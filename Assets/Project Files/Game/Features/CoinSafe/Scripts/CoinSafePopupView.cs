using NebulaSoft;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CoinSafePopupView : MonoBehaviour
{
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text currentCoinsText;
    [SerializeField] private Image openButtonImage;
    [SerializeField] private Button openButton;
    [SerializeField] private Sprite activeButtonSprite;
    [SerializeField] private Sprite deactiveButtonSprite;
    [SerializeField] private int maxCoins = 1000;

    private void OnEnable()
    {
        if (Application.isPlaying)
            Refresh();
    }

    public void Refresh()
    {
        CacheReferences();

        int amount = CoinSafeProgress.Amount;
        int displayedAmount = Mathf.Clamp(amount, 0, maxCoins);

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = maxCoins;
            progressSlider.value = displayedAmount;
        }

        if (currentCoinsText != null)
            currentCoinsText.text = amount.ToString();

        bool canOpen = CoinSafeProgress.HasClaimableReward;
        if (openButtonImage != null)
            openButtonImage.sprite = canOpen ? activeButtonSprite : deactiveButtonSprite;

        if (openButton != null)
            openButton.interactable = canOpen;
    }

    private void CacheReferences()
    {
        if (progressSlider == null)
            progressSlider = transform.Find("Content/Progress Frame")?.GetComponent<Slider>();

        if (currentCoinsText == null)
            currentCoinsText = transform.Find("Content/Coin Amount Dialog/Current Coins Text")?.GetComponent<TMP_Text>();

        if (openButton == null)
            openButton = transform.Find("Content/Open It Button")?.GetComponent<Button>();

        if (openButtonImage == null && openButton != null)
            openButtonImage = openButton.GetComponent<Image>();
    }
}
