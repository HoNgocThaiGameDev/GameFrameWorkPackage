using UnityEngine;

namespace NebulaSoft
{
    [System.Serializable]
    public class Currency
    {
        [SerializeField] CurrencyType currencyType;
        public CurrencyType CurrencyType => currencyType;
        [SerializeField, Tooltip("Leave empty for the legacy enum/save mapping. Set a stable ID for a game-specific currency.")]
        string id;
        public string ID => string.IsNullOrWhiteSpace(id) ? currencyType.ToString() : id;
        public bool HasLegacyType => string.IsNullOrWhiteSpace(id);
        internal string SaveKey => HasLegacyType ? "currency:" + (int)currencyType : "currency:id:" + ID;

        [SerializeField] int defaultAmount = 0;
        public int DefaultAmount => defaultAmount;

        [SerializeField] Sprite icon;
        public Sprite Icon => icon;

        [SerializeField] CurrencyData data;
        public CurrencyData Data => data;

        [SerializeField] FloatingCloudCase floatingCloud;
        public FloatingCloudCase FloatingCloud => floatingCloud;

        [SerializeField] CurrencyRewardPreviewSettings previewSettings;
        public CurrencyRewardPreviewSettings PreviewSettings => previewSettings;

        public int Amount { get => save.Amount; set => save.Amount = value; }

        public string AmountFormatted => CurrencyHelper.Format(save.Amount);

        public event CurrencyCallback OnCurrencyChanged;

        private Save save;

        public void Init()
        {
            data?.Init(this);

            // Initialize preview settings if available
            previewSettings?.Init(this);
        }

        public void SetSave(Save save)
        {
            this.save = save;
        }

        public void InvokeChangeEvent(int difference)
        {
            if (OnCurrencyChanged == null) return;
            foreach (CurrencyCallback callback in OnCurrencyChanged.GetInvocationList())
            {
                if (callback.Target is UnityEngine.Object target && target == null) continue;
                try { callback(this, difference); }
                catch (System.Exception exception) { Debug.LogException(exception); }
            }
        }

        [System.Serializable]
        [SaveModel("currency", CloudSync = true)]
        public class Save : ISaveObject
        {
            [SerializeField] int amount = -1;
            public int Amount { get => amount; set => amount = value; }

            public void Flush()
            {

            }
        }

        [System.Serializable]
        public class FloatingCloudCase
        {
            [SerializeField] bool addToCloud;
            public bool AddToCloud => addToCloud;

            [SerializeField] float radius = 200;
            public float Radius => radius;

            [SerializeField] GameObject specialPrefab;
            public GameObject SpecialPrefab => specialPrefab;

            [SerializeField] AudioClip appearAudioClip;
            public AudioClip AppearAudioClip => appearAudioClip;

            [SerializeField] AudioClip collectAudioClip;
            public AudioClip CollectAudioClip => collectAudioClip;
        }
    }
}
