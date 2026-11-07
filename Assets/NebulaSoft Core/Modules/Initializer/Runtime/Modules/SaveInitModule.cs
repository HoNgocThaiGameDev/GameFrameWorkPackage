#pragma warning disable 0414

using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Save Controller", core: true, order: 900)]
    public class SaveInitModule : InitModule
    {
        public override string ModuleName => "Save Controller";
        public override void Shutdown() => SaveController.Shutdown();

        [SerializeField] float autoSaveDelay = 0;
        [SerializeField] bool cleanSaveStart = false;

        [Space]
        [SerializeField] string webGLPrefix = "gameName";

        public override void CreateComponent()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            BaseSaveWrapper wrapper = BaseSaveWrapper.ActiveWrapper;
            if(wrapper is WebGLSaveWrapper)
            {
                WebGLSaveWrapper webGLWrapper = (WebGLSaveWrapper)wrapper;
                webGLWrapper.Init(webGLPrefix);
            }
#endif

            SaveController.Init(autoSaveDelay, cleanSaveStart);
        }
    }
}
