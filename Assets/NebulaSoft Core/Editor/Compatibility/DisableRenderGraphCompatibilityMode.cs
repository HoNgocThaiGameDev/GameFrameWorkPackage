#if UNITY_6000_0_OR_NEWER && !UNITY_6000_1_OR_NEWER
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using NebulaSoft;

namespace NebulaSoft
{
    [InitializeOnLoad]
    public static class DisableRenderGraphCompatibilityMode
    {
        static DisableRenderGraphCompatibilityMode()
        {
            try
            {
                // Works only if URP is active and RenderGraphSettings exists
                var settings = GraphicsSettings.GetRenderPipelineSettings<RenderGraphSettings>();
                if (settings == null)
                    return;

                if(!settings.enableRenderCompatibilityMode)
                    return;

                settings.enableRenderCompatibilityMode = false; 

                // Persist the change
                EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Editor] Failed to disable Render Graph Compatibility Mode (Unity 6000.0):\n" + e.Message);
            }
        }
    }
}
#endif