using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NebulaSoft
{
    public sealed class RewardProgressConfirmPopupAnimation : MonoBehaviour
    {
        [SerializeField] private Button tapToClaimButton;
        [SerializeField] private RectTransform safeBox;
        [SerializeField] private RectTransform door;
        [SerializeField] private RectTransform rewardsContainer;
        [SerializeField] private RectTransform rewardShine;
        [SerializeField] private TMP_Text claimText;
        [SerializeField] private GameObject rewardPreviewPrefab;
        [SerializeField] private Sprite fallbackCoinSprite;

        [SerializeField] private int tapsToOpen = 3;
        [SerializeField] private float shakeDuration = 0.18f;
        [SerializeField] private float shakeDistance = 14f;
        [SerializeField] private float shakeScale = 1.08f;
        [SerializeField] private float doorOpenDuration = 0.12f;
        [SerializeField] private float doorOpenAngle = -180f;
        [SerializeField] private float rewardRevealDelay = 2f;

        private Vector2 initialSafeBoxPosition;
        private Vector3 initialSafeBoxScale;
        private Quaternion initialDoorRotation;
        private GameObject spawnedRewardPreview;
        private int tapCount;
        private bool isAnimating;
        private bool rewardRevealed;
        private bool initialStateCached;

        private void Awake()
        {
            CacheInitialState();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
                return;

            CacheInitialState();
            ResetSequence();

            if (tapToClaimButton != null)
                tapToClaimButton.onClick.AddListener(OnTapToClaim);
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
                return;

            StopAllCoroutines();

            if (tapToClaimButton != null)
                tapToClaimButton.onClick.RemoveListener(OnTapToClaim);
        }

        public void Show()
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
            else
                ResetSequence();

            AudioClips audioClips = AudioController.AudioClips;
            if (audioClips != null && audioClips.brightest_star != null)
                AudioController.PlaySound(audioClips.brightest_star);
        }

        private void OnTapToClaim()
        {
            if (isAnimating)
                return;

            if (rewardRevealed)
            {
                ClaimAccumulatedCoins();
                gameObject.SetActive(false);
                return;
            }

            tapCount++;

            if (tapCount >= tapsToOpen)
                StartCoroutine(OpenSafe());
            else
                StartCoroutine(ShakeSafe());
        }

        private void ClaimAccumulatedCoins()
        {
            int amount = CoinSafeProgress.Amount;
            if (amount <= 0)
                return;

            CurrencyController.Add(CurrencyType.Coins, amount, "CoinSafeClaim");
            CoinSafeProgress.ResetAccumulatedCoins();
        }

        private IEnumerator ShakeSafe()
        {
            isAnimating = true;
            yield return AnimateSafeShake();
            isAnimating = false;
        }

        private IEnumerator OpenSafe()
        {
            isAnimating = true;
            yield return AnimateSafeShake();

            if (door != null)
            {
                float elapsed = 0f;
                float duration = Mathf.Max(0.01f, doorOpenDuration);

                while (elapsed < duration)
                {
                    float progress = Mathf.Clamp01(elapsed / duration);
                    float easedProgress = progress * progress * (3f - 2f * progress);
                    door.localRotation = initialDoorRotation *
                        Quaternion.Euler(0f, doorOpenAngle * easedProgress, 0f);
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                door.localRotation = initialDoorRotation *
                    Quaternion.Euler(0f, doorOpenAngle, 0f);
            }

            if (rewardRevealDelay > 0f)
                yield return new WaitForSecondsRealtime(rewardRevealDelay);

            if (door != null)
                door.gameObject.SetActive(false);

            if (safeBox != null)
                safeBox.gameObject.SetActive(false);

            ShowRewardPreview();
            rewardRevealed = true;

            if (claimText != null)
            {
                claimText.gameObject.SetActive(true);
                claimText.text = "Tap to Claim";
            }

            if (tapToClaimButton != null)
                tapToClaimButton.gameObject.SetActive(true);

            isAnimating = false;
        }

        private IEnumerator AnimateSafeShake()
        {
            if (safeBox == null)
                yield break;

            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, shakeDuration);

            while (elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                float damping = 1f - progress;
                float offset = Mathf.Sin(progress * Mathf.PI * 4f) * shakeDistance * damping;
                float scale = Mathf.Lerp(1f, shakeScale, Mathf.Sin(progress * Mathf.PI));

                safeBox.anchoredPosition = initialSafeBoxPosition + Vector2.right * offset;
                safeBox.localScale = initialSafeBoxScale * scale;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            safeBox.anchoredPosition = initialSafeBoxPosition;
            safeBox.localScale = initialSafeBoxScale;
        }

        private void ShowRewardPreview()
        {
            if (rewardsContainer == null || rewardPreviewPrefab == null)
                return;

            rewardsContainer.gameObject.SetActive(true);

            if (spawnedRewardPreview != null)
                Destroy(spawnedRewardPreview);

            spawnedRewardPreview = Instantiate(rewardPreviewPrefab, rewardsContainer);
            spawnedRewardPreview.transform.localScale = Vector3.one;

            Transform builtInShine = spawnedRewardPreview.transform.Find("Shines Holder");
            if (builtInShine != null)
                builtInShine.gameObject.SetActive(false);

            if (rewardShine != null)
                rewardShine.gameObject.SetActive(true);

            UIRewardPreviewBehavior rewardPreview = spawnedRewardPreview.GetComponent<UIRewardPreviewBehavior>();
            if (rewardPreview != null)
            {
                int amount = CoinSafeProgress.Amount;
                rewardPreview.Init(new RewardPreview(ResolveCoinIcon(), "x" + amount), fallbackCoinSprite);
            }
        }

        private Sprite ResolveCoinIcon()
        {
            if (CurrencyController.Currencies != null && CurrencyController.Currencies.Length > 0)
            {
                Currency coinCurrency = CurrencyController.GetCurrency(CurrencyType.Coins);
                if (coinCurrency != null && coinCurrency.Icon != null)
                    return coinCurrency.Icon;
            }

            return fallbackCoinSprite;
        }

        private void ResetSequence()
        {
            StopAllCoroutines();
            tapCount = 0;
            isAnimating = false;
            rewardRevealed = false;

            if (spawnedRewardPreview != null)
            {
                Destroy(spawnedRewardPreview);
                spawnedRewardPreview = null;
            }

            if (safeBox != null)
            {
                safeBox.gameObject.SetActive(true);
                safeBox.anchoredPosition = initialSafeBoxPosition;
                safeBox.localScale = initialSafeBoxScale;
            }

            if (door != null)
            {
                door.gameObject.SetActive(true);
                door.localRotation = initialDoorRotation;
            }

            if (rewardsContainer != null)
                rewardsContainer.gameObject.SetActive(false);

            if (rewardShine != null)
                rewardShine.gameObject.SetActive(false);

            if (claimText != null)
            {
                claimText.gameObject.SetActive(true);
                claimText.text = "Tap to Claim";
            }

            if (tapToClaimButton != null)
                tapToClaimButton.gameObject.SetActive(true);
        }

        private void CacheInitialState()
        {
            if (initialStateCached)
                return;

            if (safeBox != null)
            {
                initialSafeBoxPosition = safeBox.anchoredPosition;
                initialSafeBoxScale = safeBox.localScale;
            }

            if (door != null)
            {
                initialDoorRotation = door.localRotation;
            }

            initialStateCached = true;
        }
    }
}
