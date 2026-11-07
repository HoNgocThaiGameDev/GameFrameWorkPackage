using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    [System.Serializable]
    public class CurrencyUIPanelSimple : MonoBehaviour
    {
        [SerializeField] CurrencyType currencyType;

        [Space]
        [SerializeField] bool updateOnChange = true;
        [SerializeField] bool useFormattedAmount = true;

        [Space]
        [SerializeField] TextMeshProUGUI text;
        [SerializeField] Image icon;
        [SerializeField] Button addButton;

        public string Text { get => text.text; set => text.text = value; }
        public Sprite Icon { get => icon.sprite; set => icon.sprite = value; }

        public Image Image => icon;
        public Button AddButton => addButton;

        private Currency currency;
        public Currency Currency => currency;
        
        private RectTransform rectTransformRef;
        public RectTransform RectTransform => rectTransformRef;

        private bool isInitialized;

        private void Awake()
        {
            rectTransformRef = GetComponent<RectTransform>();

            Init();
        }

        public void Init()
        {
            if (isInitialized) return;

            currency = CurrencyController.GetCurrency(currencyType);

            icon.sprite = currency.Icon;

            isInitialized = true;

            Redraw();
            Activate();
        }

        public void Init(CurrencyType currencyType)
        {
            if(isInitialized)
            {
                isInitialized = false;

                Disable();
            }

            this.currencyType = currencyType;

            Init();
        }

        public void Redraw()
        {
            text.text = useFormattedAmount ? currency.AmountFormatted : currency.Amount.ToString();
        }

        public void SetAmount(int amount, bool format = true)
        {
            text.text = format ? CurrencyHelper.Format(amount) : amount.ToString();
        }

        public void Activate()
        {
            if(updateOnChange)
            {
                currency.OnCurrencyChanged += OnCurrencyAmountChanged;
            }
        }

        public void Disable()
        {
            if(updateOnChange)
            {
                currency.OnCurrencyChanged -= OnCurrencyAmountChanged;
            }
        }

        private void OnCurrencyAmountChanged(Currency currency, int amountDifference)
        {
            text.text = useFormattedAmount ? currency.AmountFormatted : currency.Amount.ToString();
        }
    }
}