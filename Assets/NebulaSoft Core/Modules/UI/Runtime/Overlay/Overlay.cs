using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [StaticUnload]
    public class Overlay
    {
        private static Overlay instance;

        private IOverlayPanel panel;

        private GameObject tempCanvasObject;
        private GameObject parentObject;

        public Overlay(GameObject parentObject)
        {
            this.parentObject = parentObject;

            panel = FindOverlayPanel(parentObject.transform);
            
            if(panel == null)
                panel = CreateDummyOverlay(parentObject.transform);

            panel.Init();

            panel.SetState(false);
            panel.SetLoadingState(false);
        }

        public static void Show(float duration, SimpleCallback onCompleted, bool showLoadingAnimation = false)
        {
            if (instance == null) return;

            IOverlayPanel panel = instance.panel;
            if (panel == null) return;
            if (panel.IsActive) return;

            panel.SetState(true);
            panel.Show(duration, onCompleted);

            if(showLoadingAnimation)
                panel.SetLoadingState(true);
        }

        public static void Hide(float duration, SimpleCallback onCompleted = null)
        {
            if (instance == null) return;

            IOverlayPanel panel = instance.panel;
            if (panel == null) return;
            if (!panel.IsActive) return;

            panel.Hide(duration, () =>
            {
                panel.SetState(false);
                panel.SetLoadingState(false);

                onCompleted?.Invoke();
            });
        }

        public static void Clear()
        {
            if (instance == null) return;

            IOverlayPanel panel = instance.panel;
            if (panel == null) return;

            if (panel != null)
            {
                panel.Clear();
                panel = null;
            }
        }

        private static IOverlayPanel FindOverlayPanel(Transform parentTransform)
        {
            foreach (Transform child in parentTransform)
            {
                Component component = child.GetComponent(typeof(IOverlayPanel));
                if (component != null)
                    return (IOverlayPanel)component;
            }

            return null;
        }

        private IOverlayPanel CreateDummyOverlay(Transform parentTransform)
        {
            tempCanvasObject = new GameObject("[TEMP OVERLAY]");
            tempCanvasObject.transform.SetParent(parentTransform);
            tempCanvasObject.transform.ResetLocal();
            tempCanvasObject.layer = LayerMask.NameToLayer("UI");

            RectTransform rt = tempCanvasObject.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = Vector2.zero;

            Canvas overlayCanvas = tempCanvasObject.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 999;

            tempCanvasObject.AddComponent<GraphicRaycaster>();

            DummyOverlayPanel dummy = tempCanvasObject.AddComponent<DummyOverlayPanel>();

            return dummy;
        }

        public static void Bind(Overlay overlay) => instance = overlay;
        public static void Unbind() => instance = null;

        private static void UnloadStatic()
        {
            instance = null;
        }
    }
}
