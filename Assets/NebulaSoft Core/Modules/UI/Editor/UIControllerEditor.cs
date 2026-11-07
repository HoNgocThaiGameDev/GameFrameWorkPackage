using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [CustomEditor(typeof(UIController))]
    public class UIControllerEditor : Editor
    {
        [InitializeOnLoadMethod]
        public static void CheckCanvasSize()
        {
#if UNITY_6000_0_OR_NEWER
            UIController uiController = FindFirstObjectByType<UIController>();
#else
            UIController uiController = FindObjectOfType<UIController>();
#endif

            if (uiController != null)
            {
                CanvasScaler canvasScaler = uiController.gameObject.GetComponent<CanvasScaler>();
                canvasScaler.matchWidthOrHeight = UIUtils.IsWideScreen(Camera.main) ? 1 : 0;
            }
        }

        [MenuItem("CONTEXT/UIController/Create Canvas")]
        public static void CreateNewCanvas(MenuCommand menuCommand)
        {
            UIController uiController = (UIController)menuCommand.context;

            // Create a custom game object
            GameObject canvasObject = new GameObject("UI Custom Canvas");
            canvasObject.transform.SetParent(uiController.transform);
            canvasObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            canvasObject.layer = LayerMask.NameToLayer("UI");

            RectTransform canvasRectTransform = canvasObject.AddComponent<RectTransform>();
            canvasRectTransform.anchorMin = new Vector2(0, 0);
            canvasRectTransform.anchorMax = new Vector2(1.0f, 1.0f);
            canvasRectTransform.sizeDelta = Vector2.zero;

            canvasObject.AddComponent<Canvas>();
            canvasObject.AddComponent<GraphicRaycaster>();

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create " + canvasObject.name);

            Selection.activeObject = canvasObject;
        }
    }
}
