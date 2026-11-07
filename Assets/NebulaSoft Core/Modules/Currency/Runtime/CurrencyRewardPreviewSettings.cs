using System.Linq;
using UnityEngine;

namespace NebulaSoft
{
    [CreateAssetMenu(fileName = "Currency Preview Settings", menuName = "Data/Currency/Preview Settings")]
    public class CurrencyRewardPreviewSettings : ScriptableObject
    {
        public const int DEFAULT_SORING_ORDER = 10;

        [SerializeField] Sprite previewSprite;

        [InlineButton("Sort", "SortOverrides")]
        [SerializeField] PreviewOverride[] overrides;

        private Sprite defaultPreviewSprite;
        private Currency currency;

        public void Init(Currency currency)
        {
            this.currency = currency;

            defaultPreviewSprite = previewSprite;
            if (defaultPreviewSprite == null)
                defaultPreviewSprite = currency.Icon;
        }

        public Preview GetPreview(CurrencyAmount currencyAmount)
        {
            Sprite sprite = GetOverrideSprite(currencyAmount.Amount);

            return new Preview(sprite, $"{CurrencyHelper.Format(currencyAmount.Amount)}");
        }

        private Sprite GetOverrideSprite(int amount)
        {
            if (!overrides.IsNullOrEmpty())
            {
                for (int i = 0; i < overrides.Length; i++)
                {
                    if (amount < overrides[i].MaxAmount)
                    {
                        return overrides[i].Sprite;
                    }
                }

                return overrides[^1].Sprite;
            }

            return defaultPreviewSprite;
        }

        private void SortOverrides()
        {
            PreviewOverride[] orderArray = overrides.OrderBy(x => x.MaxAmount).ToArray();
            for(int i = 0; i < orderArray.Length; i++)
            {
                if (overrides[i].MaxAmount != orderArray[i].MaxAmount)
                {
                    GUI.FocusControl(null);

                    RuntimeEditorUtils.Undo(this, "Array sorting");

                    overrides = orderArray;

                    RuntimeEditorUtils.SetDirty(this);

                    return;
                }
            }
        }

        [System.Serializable]
        public class PreviewOverride
        {
            [SerializeField] int maxAmount;
            public int MaxAmount => maxAmount;

            [SerializeField] Sprite sprite;
            public Sprite Sprite => sprite;
        }

        public class Preview : IRewardPreview
        {
            private Sprite icon;
            public Sprite Icon => icon;

            private string text;
            public string Text => text;

            public int SortingOrder => DEFAULT_SORING_ORDER;

            public Preview(Sprite icon, string text)
            {
                this.icon = icon;
                this.text = text;
            }

            public void OnBehaviorInitialized(UIRewardPreviewBehavior uiBehavior)
            {

            }

            public GameObject GetCustomUIPrefab() => null;
        }
    }
}
