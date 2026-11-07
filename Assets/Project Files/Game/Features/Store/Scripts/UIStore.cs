using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft.IAPStore
{
    public class UIStore : UIPage
    {
        private const int OVERLAY_SORTING_LAYER = 210;
        private const float DEFAULT_STORE_HEIGHT_OFFSET = 300;
        private const float HIDE_ANIMATION_DURATION = 0.26f;
        private const float HIDE_SCREEN_MARGIN = 40f;

        [BoxGroup("References", "References")]
        [SerializeField] RectTransform safeAreaTransform;
        [BoxGroup("References")]
        [SerializeField] CurrencyUIPanelSimple coinsUI;

        [BoxGroup("Scroll View", "Scroll View")]
        [SerializeField] VerticalLayoutGroup layout;
        [BoxGroup("Scroll View")]
        [SerializeField] RectTransform content;

        [BoxGroup("Buttons", "Buttons")]
        [SerializeField] Button closeButton;
        
        private IStoreElement[] offersElements;

        private bool isOverlay;
        private int defaultSortingOrder;
        private CanvasGroup canvasGroup;
        private RectTransform pageRectTransform;
        private TweenCase hideMoveTweenCase;
        private Vector2 pageStartAnchoredPosition;
        private bool pageTransitionPrepared;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();

            pageRectTransform = (RectTransform)transform;

            offersElements = new IStoreElement[content.childCount];
            for (int i = 0; i < offersElements.Length; i++)
            {
                Transform child = content.GetChild(i);

                IStoreElement storeElement = child.GetComponent<IStoreElement>();
                if(storeElement == null)
                {
                    storeElement = new DefaultStoreElement((RectTransform)child);
                }

                storeElement.Init();

                offersElements[i] = storeElement;
            }

            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }

        private void OnDisable()
        {
            hideMoveTweenCase.KillActive();
            ResetPageTransform();
        }

        public override void Init()
        {
            defaultSortingOrder = canvas.sortingOrder;

            NotchSaveArea.RegisterRectTransform(safeAreaTransform);

            coinsUI.Init();
        }

        public override void PlayHideAnimation()
        {
            foreach (IStoreElement offer in offersElements)
            {
                offer.KillTweenCases();
            }

            hideMoveTweenCase.KillActive();

            canvasGroup.blocksRaycasts = false;
            PreparePageTransition();

            RectTransform parentRect = pageRectTransform.parent as RectTransform;
            float screenWidth = parentRect != null ? parentRect.rect.width : pageRectTransform.rect.width;
            Vector2 targetPosition = pageStartAnchoredPosition +
                Vector2.left * ((screenWidth + pageRectTransform.rect.width) * 0.5f + HIDE_SCREEN_MARGIN);

            hideMoveTweenCase = pageRectTransform
                .DOAnchoredPosition(targetPosition, HIDE_ANIMATION_DURATION, unscaledTime: true)
                .SetEasing(Ease.Type.CubicIn)
                .OnComplete(() =>
                {
                    ResetPageTransform();
                    canvasGroup.blocksRaycasts = true;

                    if (isOverlay)
                    {
                        canvas.sortingOrder = defaultSortingOrder;
                        isOverlay = false;
                    }

                    UIController.OnPageClosed(this);
                });
        }

        public override void PlayShowAnimation()
        {
            hideMoveTweenCase.KillActive();
            ResetPageTransform();

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;

            float height = layout.padding.top + layout.padding.bottom + DEFAULT_STORE_HEIGHT_OFFSET;

            IStoreElement[] activeOffers = offersElements.Where(x => x.IsActive).ToArray();
            for (int i = 0; i < activeOffers.Length; i++)
            {
                IStoreElement offer = activeOffers[i];

                offer.KillTweenCases();
                offer.PlayAnimation(i);

                height += offer.Height;
            }


            height += activeOffers.Length * layout.spacing;

            closeButton.transform.localScale = Vector3.zero;
            closeButton.transform.DOScale(1.0f, 0.3f, 0.2f, unscaledTime: true).SetEasing(Ease.Type.BackOut);

            content.sizeDelta = new Vector2(0, height);
            content.anchoredPosition = Vector2.zero;

            UIController.OnPageOpened(this);
        }

        private void PreparePageTransition()
        {
            pageStartAnchoredPosition = pageRectTransform.anchoredPosition;
            pageTransitionPrepared = true;
        }

        private void ResetPageTransform()
        {
            if (!pageTransitionPrepared)
                return;

            pageRectTransform.anchoredPosition = pageStartAnchoredPosition;
            pageTransitionPrepared = false;
        }

        public void Hide()
        {
            foreach(IStoreElement offer in offersElements)
            {
                offer.KillTweenCases();
            }

            UIController.HidePage<UIStore>();
        }

        private void OnCloseButtonClicked()
        {
            UIController.HidePage<UIStore>();
        }

        public void SpawnCurrencyCloud(RectTransform spawnRectTransform, CurrencyType currencyType, int amount, SimpleCallback completeCallback = null)
        {
            FloatingCloud.SpawnCurrency(currencyType.ToString(), spawnRectTransform, coinsUI.RectTransform, amount, null, completeCallback);
        }

        public static void OpenAsOverlay()
        {
            UIStore storeUI = UIController.GetPage<UIStore>();
            storeUI.isOverlay = true;

            storeUI.canvas.overrideSorting = true;
            storeUI.canvas.sortingOrder = OVERLAY_SORTING_LAYER;

            UIController.ShowPage(storeUI);
        }
    }
}
