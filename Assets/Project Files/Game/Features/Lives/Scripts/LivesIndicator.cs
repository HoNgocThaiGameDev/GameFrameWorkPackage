using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class LivesIndicator : MonoBehaviour
    {
        [Space]
        [SerializeField] TextMeshProUGUI livesCountText;
        [SerializeField] Image infinityImage;
        [SerializeField] TextMeshProUGUI durationText;

        [Space]
        [SerializeField] Button addButton;

        [Space]
        [CachedTarget("durationText")]
        [SerializeField] CachedRectTransform defaultTransformData;
        [CachedTarget("durationText")]
        [SerializeField] CachedRectTransform fullTransformData;

        private CanvasGroup canvasGroup;

        private TweenCase canvasGroupTweenCase;

        private bool addButonEnabled;

        private RectTransform durationRectTransform;

        private void Awake()
        {
            canvasGroup = gameObject.GetOrSetComponent<CanvasGroup>();
            durationRectTransform = durationText.rectTransform;

            addButonEnabled = false;
        }

        private void Start()
        {
            if (UIAddLivesPanel.Exists())
            {
                if(addButton != null)
                {
                    addButton.gameObject.SetActive(true);
                    addButton.onClick.AddListener(OnAddButtonClicked);

                    addButonEnabled = true;
                }
            }
            else
            {
                if(addButton != null)
                {
                    addButton.gameObject.SetActive(false);
                }
            }

            OnStatusChanged(LivesSystem.Status);
        }

        private void OnEnable()
        {
            LivesSystem.StatusChanged += OnStatusChanged;
        }

        private void OnDisable()
        {
            LivesSystem.StatusChanged -= OnStatusChanged;
        }

        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroupTweenCase.KillActive();

                canvasGroup.alpha = 0;

                canvasGroupTweenCase = canvasGroup.DOFade(1, 0.3f);
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroupTweenCase.KillActive();
                canvasGroupTweenCase = canvasGroup.DOFade(0, 0.3f);
            }
        }

        private void OnStatusChanged(LivesStatus status)
        {
            if(status.InfiniteMode)
            {
                infinityImage.gameObject.SetActive(true);
                livesCountText.gameObject.SetActive(false);

                durationText.text = LivesSystem.GetFormatedTime(status.InfiniteModeTime);

                if(addButonEnabled)
                    addButton.gameObject.SetActive(false);
            }
            else
            {
                infinityImage.gameObject.SetActive(false);
                durationText.gameObject.SetActive(true);

                livesCountText.gameObject.SetActive(true);
                livesCountText.text = status.LivesCount.ToString();

                if (status.NewLifeTimerEnabled)
                {
                    durationText.text = LivesSystem.GetFormatedTime(status.NewLifeTime);

                    if (addButonEnabled)
                        addButton.gameObject.SetActive(true);
                }
                else
                {
                    durationText.text = LivesSystem.TEXT_FULL;

                    if (addButonEnabled)
                        addButton.gameObject.SetActive(false);
                }
            }

            if(addButonEnabled && addButton.gameObject.activeSelf)
            {
                defaultTransformData.Apply(durationRectTransform);
            }
            else
            {
                fullTransformData.Apply(durationRectTransform);
            }
        }

        private void OnAddButtonClicked()
        {
            UIAddLivesPanel.Show();
        }
    }
}
