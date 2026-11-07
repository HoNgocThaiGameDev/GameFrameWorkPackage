using NebulaSoft;
using UnityEngine;
using UnityEngine.UI;

public sealed class ProgressPopupToggle : MonoBehaviour
{
    [SerializeField] private GameObject progressPopup;
    [SerializeField] private Button progressButton;
    [SerializeField] private GameObject rewardProgressConfirmPopupPrefab;

    private Button openRewardButton;
    private GameObject rewardProgressConfirmPopup;
    private CoinSafePopupView popupView;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        CacheReferences();

        CoinSafeProgress.AmountChanged += RefreshPopup;

        if (progressButton != null)
            progressButton.onClick.AddListener(ShowPopup);

        if (openRewardButton != null)
            openRewardButton.onClick.AddListener(ShowRewardPopup);
    }

    private void OnDisable()
    {
        if (!Application.isPlaying)
            return;

        CoinSafeProgress.AmountChanged -= RefreshPopup;

        if (progressButton != null)
            progressButton.onClick.RemoveListener(ShowPopup);

        if (openRewardButton != null)
            openRewardButton.onClick.RemoveListener(ShowRewardPopup);
    }

    public void ShowPopup()
    {
        if (progressPopup == null)
            return;

        RefreshPopup();

        var entranceAnimation = progressPopup.GetComponent<ProgressPopupEntranceAnimation>();
        if (entranceAnimation != null)
            entranceAnimation.Show();
        else
            progressPopup.SetActive(true);
    }

    public void HidePopup()
    {
        if (progressPopup == null)
            return;

        var entranceAnimation = progressPopup.GetComponent<ProgressPopupEntranceAnimation>();
        if (entranceAnimation != null)
            entranceAnimation.Hide();
        else
            progressPopup.SetActive(false);
    }

    public void SetHomeTabVisible(bool visible)
    {
        CacheReferences();
        if (progressButton == null)
            return;

        if (progressButton.gameObject.activeSelf != visible)
            progressButton.gameObject.SetActive(visible);

        if (visible)
        {
            RefreshPopup();
            return;
        }

        if (progressPopup != null && progressPopup.activeSelf)
            HidePopup();

        if (rewardProgressConfirmPopup != null && rewardProgressConfirmPopup.activeSelf)
            rewardProgressConfirmPopup.SetActive(false);
    }

    public void ShowRewardPopup()
    {
        if (!CoinSafeProgress.HasClaimableReward || rewardProgressConfirmPopupPrefab == null)
            return;

        HidePopup();

        if (rewardProgressConfirmPopup == null)
            rewardProgressConfirmPopup = Instantiate(rewardProgressConfirmPopupPrefab);

        var popupAnimation = rewardProgressConfirmPopup.GetComponent<RewardProgressConfirmPopupAnimation>();
        if (popupAnimation != null)
            popupAnimation.Show();
        else
            rewardProgressConfirmPopup.SetActive(true);
    }

    private void CacheReferences()
    {
        if (popupView == null && progressPopup != null)
            popupView = progressPopup.GetComponent<CoinSafePopupView>();

        if (openRewardButton == null && progressPopup != null)
            openRewardButton = progressPopup.transform.Find("Content/Open It Button")?.GetComponent<Button>();

        if (progressPopup != null)
            UIAudioFeedback.RegisterButtons(progressPopup.transform);
    }

    private void RefreshPopup()
    {
        if (popupView != null)
            popupView.Refresh();
    }
}
