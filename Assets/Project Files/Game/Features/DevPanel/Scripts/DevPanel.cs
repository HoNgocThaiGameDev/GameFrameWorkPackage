using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NebulaSoft
{
    [RequireComponent(typeof(Canvas))]
    public class DevPanel : MonoBehaviour
    {
        [SerializeField] RectTransform safeAreaRectTransform;

        [Space]
        [SerializeField] RectTransform buttonsContainer;
        [SerializeField] GameObject panelToggleButton;

        private Canvas canvas;
        private CanvasGroup toggleCanvasGroup;

        private bool isButtonsDisplayed;

        private TweenCase[] tweenCases;

        private bool isHidden = false;

        private float lastClickTime = 0;
        private bool isPointerDown;

        private void Awake()
        {
            canvas = GetComponent<Canvas>();

            toggleCanvasGroup = panelToggleButton.GetOrAddComponent<CanvasGroup>();
            toggleCanvasGroup.alpha = isHidden ? 0.0f : 1.0f;

            panelToggleButton.AddEvent(EventTriggerType.PointerDown, (data) => OnToggleButtonPointerDown());
            panelToggleButton.AddEvent(EventTriggerType.PointerUp, (data) => OnToggleButtonPointerUp());

            gameObject.SetActive(DevPanelEnabler.IsActive);

            NotchSaveArea.RegisterRectTransform(safeAreaRectTransform);
        }

        private void OnToggleButtonPointerUp()
        {
            if(!isPointerDown) return;

            isPointerDown = false;

            if (isButtonsDisplayed)
            {
                buttonsContainer.gameObject.SetActive(false);
            }
            else
            {
                buttonsContainer.gameObject.SetActive(true);

                tweenCases = new TweenCase[buttonsContainer.childCount];

                float delay = 0.0f;
                for (int i = 0; i < tweenCases.Length; i++)
                {
                    Transform button = buttonsContainer.GetChild(i);

                    if (button.gameObject.activeSelf)
                    {
                        button.localScale = Vector3.zero;
                        tweenCases[i] = button.DOScale(Vector3.one, 0.1f, delay: delay).SetEasing(Ease.Type.CircOut);

                        delay += 0.04f;
                    }
                }
            }

            isButtonsDisplayed = !isButtonsDisplayed;
        }

        private void OnToggleButtonPointerDown()
        {
            isPointerDown = true;

            lastClickTime = Time.time + 1.0f;
        }

        private void Update()
        {
            if (!isPointerDown) return;

            if (Time.time > lastClickTime)
            {
                isHidden = !isHidden;

                toggleCanvasGroup.alpha = isHidden ? 0.0f : 1.0f;

                lastClickTime = float.MaxValue;
                isPointerDown = false;
            }
        }

        private void OnEnable()
        {
            canvas.enabled = true;

            DisablePanel();

            DevPanelEnabler.StateChanged += OnDevPanelStateChanged;
        }

        private void OnDisable()
        {
            canvas.enabled = false;

            DevPanelEnabler.StateChanged -= OnDevPanelStateChanged;

            DisablePanel();
        }

        private void OnDevPanelStateChanged(bool value)
        {
            gameObject.SetActive(value);
        }

        public void DisablePanel()
        {
            isButtonsDisplayed = false;

            buttonsContainer.gameObject.SetActive(false);

            if (!tweenCases.IsNullOrEmpty())
            {
                foreach (TweenCase tweenCase in tweenCases)
                {
                    tweenCase?.Kill();
                }
            }
        }
    }
}
