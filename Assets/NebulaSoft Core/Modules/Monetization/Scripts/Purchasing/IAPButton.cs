using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public class IAPButton : MonoBehaviour
    {
        [SerializeField] Image backImage;
        [SerializeField] Button button;
        [SerializeField] TMP_Text priceText;
        [SerializeField] GameObject loadingObject;
        [SerializeField] Sprite activeBackSprite;
        [SerializeField] Sprite unactiveBackSprite;
        private string key;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OnButtonClicked);
                UIAudioFeedback.RegisterButton(button);
            }
        }
        private void OnEnable()
        {
            IAPManager.StateChanged += UpdateState;
            UpdateState();
        }
        private void OnDisable() => IAPManager.StateChanged -= UpdateState;
        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnButtonClicked);
        }
        public void Init(ProductKeyType product) => Init(IAPManager.GetIAPItem(product)?.Key ?? IAPItem.GetLegacyKey(product));
        public void Init(string productKey)
        {
            key = productKey;
            UpdateState();
        }
        public void UpdateState() => UpdateState(IAPManager.GetProductData(key));
        public void UpdateState(ProductData product)
        {
            var item = IAPManager.GetIAPItemByKey(key);
            bool ready = IAPManager.IsEnabled && IAPManager.IsInitialized && product != null && item != null;
            bool busy = IAPManager.IsPurchaseInProgress || IAPManager.IsRestoreInProgress;
            bool owned = item != null && item.ProductType != ProductType.Consumable && IAPManager.IsPurchased(key);
            if (button != null) button.interactable = ready && !busy && !owned;
            if (loadingObject != null) loadingObject.SetActive(IAPManager.IsInitializing || (busy && IAPManager.ActivePurchaseKey == key));
            if (backImage != null)
            {
                var sprite = ready && !owned ? activeBackSprite : unactiveBackSprite;
                if (sprite != null) backImage.sprite = sprite;
            }
            if (priceText != null)
            {
                priceText.gameObject.SetActive(true);
                if (owned) priceText.text = "Owned";
                else if (ready && !string.IsNullOrWhiteSpace(product.LocalizedPriceString)) priceText.text = product.GetLocalPrice();
#if UNITY_EDITOR
                else if (item != null) priceText.text = string.Format(CultureInfo.InvariantCulture, "${0:0.00}", item.DefaultUSDPrice);
#endif
                else priceText.text = ready ? product.GetLocalPrice() : "—";
            }
        }
        private void OnButtonClicked()
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                SystemMessage.ShowMessage("No internet connection. Please try again later.");
                return;
            }
            IAPManager.BuyProduct(key);
        }
    }
}
