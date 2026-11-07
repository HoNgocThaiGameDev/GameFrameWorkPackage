using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class DailyRewardClaimButtonView : MonoBehaviour
    {
        [SerializeField] Button claimButton;
        [SerializeField] Image claimButtonImage;
        [SerializeField] TextMeshProUGUI claimLabel;
        [SerializeField] Image lockedBadge;
        [SerializeField] Sprite claimSprite;
        [SerializeField] Sprite claimedSprite;

        private Action claimAction;

        private void Awake()
        {
            if (claimButton == null)
                return;

            claimButton.onClick.RemoveListener(HandleClaim);
            claimButton.onClick.AddListener(HandleClaim);
            UIAudioFeedback.RegisterButton(claimButton);
        }

        private void OnDestroy()
        {
            if (claimButton != null)
                claimButton.onClick.RemoveListener(HandleClaim);
        }

        public void Bind(DailyRewardCellState state, Action onClaim)
        {
            bool isCurrent = state == DailyRewardCellState.Current;
            bool isLocked = state == DailyRewardCellState.Locked;
            claimAction = isCurrent ? onClaim : null;

            if (claimButton != null)
            {
                claimButton.gameObject.SetActive(!isLocked);
                claimButton.interactable = isCurrent;
            }

            if (claimButtonImage != null)
                claimButtonImage.sprite = isCurrent ? claimSprite : claimedSprite;

            if (claimLabel != null)
            {
                claimLabel.gameObject.SetActive(!isLocked);
                claimLabel.text = isCurrent ? "CLAIM" : "CLAIMED";
            }

            if (lockedBadge != null)
                lockedBadge.gameObject.SetActive(isLocked);
        }

        private void HandleClaim()
        {
            claimAction?.Invoke();
        }
    }
}
