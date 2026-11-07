using NebulaSoft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CoinSafeProgressText : MonoBehaviour
{
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private int maxCoins = 1000;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnEnable()
    {
        CacheReferences();
        CoinSafeProgress.AmountChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        CoinSafeProgress.AmountChanged -= Refresh;
    }

    public void Refresh()
    {
        int amount = CoinSafeProgress.Amount;

        if (progressText != null)
            progressText.text = $"{amount}/{maxCoins}";

        if (progressSlider != null)
            progressSlider.SetValueWithoutNotify(Mathf.Clamp(amount, 0, maxCoins));
    }

    private void CacheReferences()
    {
        if (progressText != null && progressSlider != null)
            return;

        Transform progressBackground = transform.Find("Progress Background");
        if (progressBackground == null)
            return;

        if (progressText == null)
            progressText = progressBackground.Find("Progress Text")?.GetComponent<TMP_Text>();

        if (progressSlider == null)
            progressSlider = progressBackground.GetComponent<Slider>();
    }
}
