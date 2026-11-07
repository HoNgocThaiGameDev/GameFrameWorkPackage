using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class TutorialCanvasController : MonoBehaviour
    {
        private static TutorialCanvasController instance;

        public static readonly int POINTER_SWIPE_UP = Animator.StringToHash("Swipe Up");
        public static readonly int POINTER_SWIPE_LEFT_UP = Animator.StringToHash("Swipe Left Up");
        public static readonly int POINTER_SWIPE_DOWN = Animator.StringToHash("Swipe Down");
        public static readonly int POINTER_SHOW_PU = Animator.StringToHash("Show");
        public static readonly int POINTER_CLICK = Animator.StringToHash("Click");

        [SerializeField] CanvasGroup fadeCanvasGroup;

        [Space]
        [SerializeField] Animator pointerAnimator;

        private static Canvas tutorialCanvas;

        private static List<TransformCase> activeTransformCases;

        private static TweenCase fadeTweenCase;

        public void Init()
        {
            instance = this;

            tutorialCanvas = GetComponent<Canvas>();
            tutorialCanvas.enabled = false;

            activeTransformCases = new List<TransformCase>();
        }

        private void OnDestroy()
        {
            if (activeTransformCases != null)
            {
                foreach (TransformCase transformCase in activeTransformCases)
                {
                    transformCase?.Destroy();
                }
                activeTransformCases.Clear();
            }

            fadeTweenCase?.KillActive();
            if (instance == this)
                instance = null;
        }

        public static void ActivatePointer(Vector3 position, int animationHash)
        {
            Transform pointerTransform = instance.pointerAnimator.transform;
            pointerTransform.gameObject.SetActive(true);
            pointerTransform.SetAsLastSibling();

            tutorialCanvas.enabled = true;

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            RectTransform rectTransform = (RectTransform)instance.pointerAnimator.transform;

            Vector3 screenPoint = mainCamera.WorldToScreenPoint(position);

            RectTransform canvasRect = UIController.MainCanvas.GetComponent<RectTransform>();

            Vector2 localPos;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, mainCamera, out localPos))
            {
                rectTransform.localPosition = localPos;
            }

            instance.pointerAnimator.Play(animationHash, -1, 0);
        }

        public static void ActivateTutorialCanvas(RectTransform element, bool createDummy, bool fadeImage)
        {
            TransformCase activeTransformCase = new TransformCase(element);
            activeTransformCase.SetNewParent(tutorialCanvas.transform, createDummy);

            activeTransformCases.Add(activeTransformCase);

            tutorialCanvas.enabled = true;

            if (fadeImage)
            {
                if (!instance.fadeCanvasGroup.gameObject.activeSelf)
                {
                    fadeTweenCase.KillActive();

                    instance.fadeCanvasGroup.gameObject.SetActive(true);
                    instance.fadeCanvasGroup.alpha = 0;

                    fadeTweenCase = instance.fadeCanvasGroup.DOFade(1.0f, 0.3f);
                }
            }
        }

        public static void ResetTutorialCanvas()
        {
            foreach (TransformCase transformCase in activeTransformCases)
            {
                transformCase.Reset();
            }
            activeTransformCases.Clear();

            fadeTweenCase.KillActive();

            instance.fadeCanvasGroup.alpha = 0;
            instance.fadeCanvasGroup.gameObject.SetActive(false);

            instance.pointerAnimator.gameObject.SetActive(false);

            tutorialCanvas.enabled = false;
        }

        public static void ResetPointer()
        {
            instance.pointerAnimator.gameObject.SetActive(false);

            tutorialCanvas.enabled = false;
        }

        public static void AlignToCorner(RectTransform rectTransform, UIAnchorCorner corner, Vector2 anchoredPosition)
        {
            Vector2 anchor = Vector2.zero;
            Vector2 pivot = Vector2.zero;

            switch (corner)
            {
                case UIAnchorCorner.TopLeft:
                    anchor = new Vector2(0, 1);
                    pivot = new Vector2(0, 1);
                    break;
                case UIAnchorCorner.TopCenter:
                    anchor = new Vector2(0.5f, 1);
                    pivot = new Vector2(0.5f, 1);
                    break;
                case UIAnchorCorner.TopRight:
                    anchor = new Vector2(1, 1);
                    pivot = new Vector2(1, 1);
                    break;
                case UIAnchorCorner.MiddleLeft:
                    anchor = new Vector2(0, 0.5f);
                    pivot = new Vector2(0, 0.5f);
                    break;
                case UIAnchorCorner.MiddleCenter:
                    anchor = new Vector2(0.5f, 0.5f);
                    pivot = new Vector2(0.5f, 0.5f);
                    break;
                case UIAnchorCorner.MiddleRight:
                    anchor = new Vector2(1, 0.5f);
                    pivot = new Vector2(1, 0.5f);
                    break;
                case UIAnchorCorner.BottomLeft:
                    anchor = new Vector2(0, 0);
                    pivot = new Vector2(0, 0);
                    break;
                case UIAnchorCorner.BottomCenter:
                    anchor = new Vector2(0.5f, 0);
                    pivot = new Vector2(0.5f, 0);
                    break;
                case UIAnchorCorner.BottomRight:
                    anchor = new Vector2(1, 0);
                    pivot = new Vector2(1, 0);
                    break;
            }

            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
        }

        private class TransformCase
        {
            private RectTransform rectTransform;

            private Transform parentTransform;

            private Vector3 worldPosition;
            private Vector2 anchoredPosition;
            private Vector2 size;
            private Vector3 scale;
            private Quaternion rotation;

            private int siblingIndex;

            private GameObject dummyObject;

            private bool isActive;

            private TweenCase tweenCase;

            public TransformCase(RectTransform element)
            {
                rectTransform = element;

                siblingIndex = element.GetSiblingIndex();

                parentTransform = element.parent;

                worldPosition = element.position;
                anchoredPosition = element.anchoredPosition;
                size = element.sizeDelta;
                scale = element.localScale;
                rotation = element.localRotation;

                isActive = true;
            }

            public void SetNewParent(Transform transform, bool createDummy)
            {
                Vector3 position = rectTransform.position;
                Transform parentTransform = rectTransform.parent;

                rectTransform.SetParent(transform, true);

                if (createDummy)
                {
                    dummyObject = new GameObject("[TUTORIAL DUMMY]", typeof(RectTransform));

                    RectTransform dummyRectTransform = (RectTransform)dummyObject.transform;
                    dummyRectTransform.SetParent(parentTransform, true);
                    dummyRectTransform.SetSiblingIndex(siblingIndex);

                    Vector3 localPos = dummyRectTransform.localPosition;
                    localPos.z = 0;

                    dummyRectTransform.localPosition = localPos;
                    dummyRectTransform.anchoredPosition = anchoredPosition;
                    dummyRectTransform.sizeDelta = size;
                    dummyRectTransform.localScale = scale;
                    dummyRectTransform.localRotation = rotation;

                    dummyObject.SetActive(true);

                    if(parentTransform != null)
                    {
                        HorizontalOrVerticalLayoutGroup layoutGroup = parentTransform.GetComponent<HorizontalOrVerticalLayoutGroup>();
                        if (layoutGroup != null)
                        {
                            tweenCase = Tween.NextFrame(() =>
                            {
                                ApplyNewPosition();
                            });
                        }
                        else
                        {
                            ApplyNewPosition();
                        }
                    }
                    else
                    {
                        ApplyNewPosition();
                    }

                    void ApplyNewPosition()
                    {
                        rectTransform.position = dummyRectTransform.position;
                    }
                }

            }

            public void Reset()
            {
                if (!isActive) return;

                isActive = false;

                if (dummyObject != null)
                    GameObject.Destroy(dummyObject);

                rectTransform.SetParent(parentTransform, true);
                rectTransform.anchoredPosition = anchoredPosition;
                rectTransform.sizeDelta = size;
                rectTransform.localScale = scale;
                rectTransform.localRotation = rotation;

                rectTransform.SetSiblingIndex(siblingIndex);
            }

            public void Destroy()
            {
                tweenCase?.KillActive();

                isActive = false;
            }
        }

        public enum UIAnchorCorner
        {
            TopLeft,
            TopCenter,
            TopRight,
            MiddleLeft,
            MiddleCenter,
            MiddleRight,
            BottomLeft,
            BottomCenter,
            BottomRight
        }
    }
}
