#pragma warning disable CS0618 // Suppress 'RenderTargetHandle is obsolete' in this file
#pragma warning disable CS0809
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Experimental.Rendering; // GraphicsFormat

#if UNITY_2023_1_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;
#endif

// NOTE:
// This feature supports both Unity 2022.3 (Execute path)
// and Unity 6000.x (RenderGraph path).
// Do NOT remove Execute(), depth attachment, or UV flip logic.

public class NebulaSoftOutlineFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class OutlineSettings
    {
        [Tooltip("Only objects on these layers will be rendered into the outline mask.")]
        public LayerMask outlinedLayer = ~0;

        // Renamed to avoid Unity Inspector "Type mismatch" from old serialized data.
        [Tooltip("Material using the Hidden/NebulaSoft/OutlineCompositeURP shader.")]
        public Material compositeMaterialAsset;

        [Tooltip("When to run the outline pass.")]
        public RenderPassEvent renderEvent = RenderPassEvent.AfterRenderingOpaques;
    }

    public OutlineSettings settings = new OutlineSettings();

    OutlinePass pass;
    Material compositeMaterialInstance;

    // =========================
    // Material instance handling
    // =========================
    void EnsureMaterialInstance()
    {
        if (settings.compositeMaterialAsset == null)
        {
            if (compositeMaterialInstance != null)
            {
                DestroyImmediate(compositeMaterialInstance);
                compositeMaterialInstance = null;
            }
            return;
        }

        if (compositeMaterialInstance == null)
        {
            compositeMaterialInstance = Instantiate(settings.compositeMaterialAsset);
            compositeMaterialInstance.name = settings.compositeMaterialAsset.name + " (Instance)";
            return;
        }

        if (compositeMaterialInstance.shader != settings.compositeMaterialAsset.shader)
        {
            DestroyImmediate(compositeMaterialInstance);
            compositeMaterialInstance = Instantiate(settings.compositeMaterialAsset);
            compositeMaterialInstance.name = settings.compositeMaterialAsset.name + " (Instance)";
        }
    }

    public override void Create()
    {
        EnsureMaterialInstance();
        pass = new OutlinePass(settings, () => compositeMaterialInstance);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        EnsureMaterialInstance();
        if (pass != null)
            pass.SetSettings(settings);
    }
#endif

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        CameraData cd = renderingData.cameraData;

        if (cd.isPreviewCamera) return;
        if (cd.cameraType != CameraType.Game) return;
        if (cd.camera == null) return;

        EnsureMaterialInstance();
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.DisposePass();

        if (compositeMaterialInstance != null)
        {
            DestroyImmediate(compositeMaterialInstance);
            compositeMaterialInstance = null;
        }
    }

    // ============================================================
    // PASS
    // ============================================================
    class OutlinePass : ScriptableRenderPass
    {
        static readonly ShaderTagId ShaderTag = new ShaderTagId("NebulaSoftOutlineMask");

        OutlineSettings settings;
        FilteringSettings filtering;

        readonly System.Func<Material> getMaterial;

#if !UNITY_6000_6_OR_NEWER
        RTHandle maskRT;
        RTHandle sceneCopyRT;
#endif

        public OutlinePass(OutlineSettings settings, System.Func<Material> getMaterial)
        {
            this.settings = settings;
            this.getMaterial = getMaterial;

            filtering = new FilteringSettings(RenderQueueRange.opaque, settings.outlinedLayer);
            renderPassEvent = settings.renderEvent;

            ConfigureInput(ScriptableRenderPassInput.Color);
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void SetSettings(OutlineSettings newSettings)
        {
            settings = newSettings;
            filtering = new FilteringSettings(RenderQueueRange.opaque, settings.outlinedLayer);
            renderPassEvent = settings.renderEvent;
        }

        // ===========================
        // Classic path (Execute)
        // ===========================

#if !UNITY_6000_6_OR_NEWER
        [System.Obsolete("OnCameraSetup is obsolete but required for compatibility path")]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var camDesc = renderingData.cameraData.cameraTargetDescriptor;

            // Mask (R8)
            var maskDesc = camDesc;
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = 1;
            maskDesc.graphicsFormat = GraphicsFormat.R8_UNorm;

            RenderingUtils.ReAllocateIfNeeded(
                ref maskRT, maskDesc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_NebulaSoftOutlineMask");

            // Scene copy
            var sceneDesc = camDesc;
            sceneDesc.depthBufferBits = 0;

            RenderingUtils.ReAllocateIfNeeded(
                ref sceneCopyRT, sceneDesc, FilterMode.Bilinear, TextureWrapMode.Clamp,
                name: "_NebulaSoftOutlineSceneCopy");
        }

        [System.Obsolete("Execute is obsolete but required for compatibility path")]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var mat = getMaterial?.Invoke();
            if (mat == null)
                return;

            var renderer = renderingData.cameraData.renderer;
            RTHandle cameraColor = renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("NebulaSoft Outline (Execute)");

            // 1) Mask
            CoreUtils.SetRenderTarget(cmd, maskRT);
            CoreUtils.ClearRenderTarget(cmd, ClearFlag.Color, Color.black);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var drawSettings = CreateDrawingSettings(
                ShaderTag, ref renderingData, SortingCriteria.CommonOpaque);

            context.DrawRenderers(
                renderingData.cullResults, ref drawSettings, ref filtering);

            // 2) Copy scene
            Blitter.BlitCameraTexture(cmd, cameraColor, sceneCopyRT);

            // 3) Composite
            cmd.SetGlobalTexture("_SceneTex", sceneCopyRT);
            cmd.SetGlobalTexture("_OutlineMask", maskRT);

            Blitter.BlitCameraTexture(cmd, sceneCopyRT, cameraColor, mat, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#endif

        public void DisposePass()
        {
#if !UNITY_6000_6_OR_NEWER
            maskRT?.Release();
            maskRT = null;

            sceneCopyRT?.Release();
            sceneCopyRT = null;
#endif
        }

#if UNITY_2023_1_OR_NEWER
        // ===========================
        // RenderGraph path
        // ===========================
        class RGMaskPassData { public RendererListHandle rendererList; }
        class RGCopyPassData { public TextureHandle cameraColor; }
        class RGCompositePassData
        {
            public Material material;
            public TextureHandle sceneCopy;
            public TextureHandle mask;
        }

        public override void RecordRenderGraph(RenderGraph rg, ContextContainer frameData)
        {
            var mat = getMaterial?.Invoke();
            if (mat == null)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            var renderingData = frameData.Get<UniversalRenderingData>();
            var resources = frameData.Get<UniversalResourceData>();

            TextureHandle cameraColor = resources.activeColorTexture;
            var camDesc = cameraData.cameraTargetDescriptor;

            // Mask
            var maskDesc = camDesc;
            maskDesc.depthBufferBits = 0;
            maskDesc.msaaSamples = 1;
            maskDesc.graphicsFormat = GraphicsFormat.R8_UNorm;

            TextureHandle mask = rg.CreateTexture(new TextureDesc(maskDesc)
            {
                name = "_NebulaSoftOutlineMask",
                clearBuffer = true,
                clearColor = Color.black
            });

            // Scene copy
            var sceneDesc = camDesc;
            sceneDesc.depthBufferBits = 0;

            TextureHandle sceneCopy = rg.CreateTexture(new TextureDesc(sceneDesc)
            {
                name = "_NebulaSoftOutlineSceneCopy"
            });

            var rlDesc = new RendererListDesc(
                ShaderTag, renderingData.cullResults, cameraData.camera)
            {
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderQueueRange = RenderQueueRange.opaque,
                layerMask = filtering.layerMask
            };

            RendererListHandle rl = rg.CreateRendererList(rlDesc);

            using (var builder = rg.AddRasterRenderPass<RGMaskPassData>(
                "NebulaSoft Outline - Mask", out var pass))
            {
                pass.rendererList = rl;
                builder.SetRenderAttachment(mask, 0);
                builder.UseRendererList(rl);

                builder.SetRenderFunc((RGMaskPassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.DrawRendererList(d.rendererList);
                });
            }

            using (var builder = rg.AddRasterRenderPass<RGCopyPassData>(
                "NebulaSoft Outline - Copy", out var pass))
            {
                pass.cameraColor = cameraColor;
                builder.SetRenderAttachment(sceneCopy, 0);
                builder.UseTexture(cameraColor, AccessFlags.Read);

                builder.SetRenderFunc((RGCopyPassData d, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(
                        ctx.cmd, (RTHandle)d.cameraColor,
                        new Vector4(1, 1, 0, 0), 0f, false);
                });
            }

            using (var builder = rg.AddRasterRenderPass<RGCompositePassData>(
                "NebulaSoft Outline - Composite", out var pass))
            {
                pass.material = mat;
                pass.sceneCopy = sceneCopy;
                pass.mask = mask;

                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(cameraColor, 0);
                builder.UseTexture(sceneCopy, AccessFlags.Read);
                builder.UseTexture(mask, AccessFlags.Read);

                builder.SetRenderFunc((RGCompositePassData d, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalTexture("_SceneTex", d.sceneCopy);
                    ctx.cmd.SetGlobalTexture("_OutlineMask", d.mask);

                    Blitter.BlitTexture(
                        ctx.cmd, (RTHandle)d.sceneCopy,
                        new Vector4(1, 1, 0, 0), d.material, 0);
                });
            }
        }
#endif
    }
}
