using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class SaveProgressFoundDialogView : MonoBehaviour
    {
        [Header("Device Card")]
        [SerializeField] TextMeshProUGUI deviceLevelText;
        [SerializeField] TextMeshProUGUI deviceCoinText;
        [SerializeField] TextMeshProUGUI deviceCoinSafeAmountText;
        [SerializeField] TextMeshProUGUI deviceTimestampText;
        [SerializeField] Button deviceSelectButton;

        [Header("Server Card")]
        [SerializeField] TextMeshProUGUI serverLevelText;
        [SerializeField] TextMeshProUGUI serverCoinText;
        [SerializeField] TextMeshProUGUI serverCoinSafeAmountText;
        [SerializeField] TextMeshProUGUI serverTimestampText;
        [SerializeField] Button serverSelectButton;

        private Action deviceSelected;
        private Action serverSelected;

        public void ConfigureReferences(
            TextMeshProUGUI deviceLevel,
            TextMeshProUGUI deviceCoin,
            TextMeshProUGUI deviceTimestamp,
            Button deviceSelect,
            TextMeshProUGUI serverLevel,
            TextMeshProUGUI serverCoin,
            TextMeshProUGUI serverTimestamp,
            Button serverSelect)
        {
            deviceLevelText = deviceLevel;
            deviceCoinText = deviceCoin;
            deviceTimestampText = deviceTimestamp;
            deviceSelectButton = deviceSelect;
            serverLevelText = serverLevel;
            serverCoinText = serverCoin;
            serverTimestampText = serverTimestamp;
            serverSelectButton = serverSelect;
        }

        public void ConfigureCoinSafeReferences(
            TextMeshProUGUI deviceCoinSafeAmount,
            TextMeshProUGUI serverCoinSafeAmount)
        {
            deviceCoinSafeAmountText = deviceCoinSafeAmount;
            serverCoinSafeAmountText = serverCoinSafeAmount;
        }

        public void Init(Action onDeviceSelected, Action onServerSelected)
        {
            deviceSelected = onDeviceSelected;
            serverSelected = onServerSelected;

            if (deviceSelectButton != null)
            {
                deviceSelectButton.onClick.RemoveListener(OnDeviceSelected);
                deviceSelectButton.onClick.AddListener(OnDeviceSelected);
            }

            if (serverSelectButton != null)
            {
                serverSelectButton.onClick.RemoveListener(OnServerSelected);
                serverSelectButton.onClick.AddListener(OnServerSelected);
            }
        }

        public void Show(PlayerProgressSnapshot localProgress, PlayerProgressSnapshot cloudProgress)
        {
            ApplyCard(
                deviceLevelText,
                deviceCoinText,
                deviceCoinSafeAmountText,
                deviceTimestampText,
                localProgress,
                "Uploaded Now");

            ApplyCard(
                serverLevelText,
                serverCoinText,
                serverCoinSafeAmountText,
                serverTimestampText,
                cloudProgress,
                FormatServerTimestamp(cloudProgress));

            gameObject.SetActive(true);
        }

        public void HideImmediate()
        {
            gameObject.SetActive(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (deviceSelectButton != null)
                deviceSelectButton.interactable = interactable;

            if (serverSelectButton != null)
                serverSelectButton.interactable = interactable;
        }

        private void OnDeviceSelected()
        {
            deviceSelected?.Invoke();
        }

        private void OnServerSelected()
        {
            serverSelected?.Invoke();
        }

        private static void ApplyCard(
            TextMeshProUGUI levelText,
            TextMeshProUGUI coinText,
            TextMeshProUGUI coinSafeAmountText,
            TextMeshProUGUI timestampText,
            PlayerProgressSnapshot progress,
            string timestamp)
        {
            if (levelText != null)
                levelText.text = progress != null && progress.HasLevelProgress
                    ? progress.DisplayLevelNumber.ToString()
                    : "Not saved";

            if (coinText != null)
                coinText.text = progress != null && progress.HasCoinBalance
                    ? Mathf.Max(0, progress.CoinBalance).ToString()
                    : "--";

            if (coinSafeAmountText != null)
                coinSafeAmountText.text = progress != null && progress.HasCoinSafeAmount
                    ? Mathf.Max(0, progress.CoinSafeAmount).ToString()
                    : "--";

            if (timestampText != null)
                timestampText.text = timestamp;
        }

        private static string FormatServerTimestamp(PlayerProgressSnapshot cloudProgress)
        {
            if (cloudProgress == null || !cloudProgress.HasUpdatedAt)
                return "Last saved: --";

            DateTime localTime = cloudProgress.UpdatedAtUtc.ToLocalTime();
            return "Last saved:\n" + localTime.ToString("dd/MM/yyyy HH:mm:ss");
        }
    }
}
