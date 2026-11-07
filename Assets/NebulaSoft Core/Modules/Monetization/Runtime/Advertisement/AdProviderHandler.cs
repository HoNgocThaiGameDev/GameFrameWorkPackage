using System.Threading.Tasks;
using UnityEngine;

namespace NebulaSoft
{
    /// <summary>
    /// Abstract base class for managing ad providers. Specific ad providers like AdMob, UnityAds, etc. should inherit from this class.
    /// </summary>
    public abstract class AdProviderHandler
    {
        protected const int RETRY_ATTEMPT_DEFAULT_VALUE = 1;
        protected const int MAX_RETRY_ATTEMPTS = 5; // Max retry attempts for loading ads

        // Retry counters for interstitial and rewarded ads
        protected int interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;
        protected int rewardedRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

        // Represents the type of ad provider (e.g., AdMob, UnityAds, etc.)
        protected AdProvider providerType;
        public AdProvider ProviderType => providerType;

        // References to monetization and ads settings, which are shared across ad providers.
        protected MonetizationSettings monetizationSettings;
        protected AdsSettings adsSettings;

        /// <summary>
        /// Returns whether the ad provider has been initialized.
        /// </summary>
        public bool IsInitialized { get; private set; }

        // Constructor for setting the provider type.
        public AdProviderHandler(AdProvider providerType)
        {
            this.providerType = providerType;
        }

        /// <summary>
        /// Links the monetization settings to this handler, allowing access to ads settings.
        /// </summary>
        public void LinkSettings(MonetizationSettings monetizationSettings)
        {
            this.monetizationSettings = monetizationSettings;

            adsSettings = monetizationSettings.AdsSettings;
        }

        /// <summary>
        /// Asynchronous method to initialize the ad provider.
        /// </summary>
        public async Task<bool> InitAsync()
        {
            // Check if already initialized to avoid re-initialization.
            if (IsInitialized)
                return true; // Already initialized

            // Calls the abstract method that subclasses must implement to handle specific initialization logic.
            bool initResult = await InitProviderAsync();

            if (initResult)
            {
                // Mark the provider as initialized and notify the AdsManager.
                IsInitialized = true;

                AdsManager.OnProviderInitialized(providerType);

                // Optional verbose logging for debugging purposes.
                if (Monetization.VerboseLogging)
                    Debug.Log(string.Format("[AdsManager]: {0} is initialized!", providerType));

                return true;
            }

            // If initialization fails, return false.
            return false;
        }

        /// <summary>
        /// Abstract method for provider-specific asynchronous initialization logic.
        /// Subclasses must implement this method to perform their SDK initialization.
        /// </summary>
        protected abstract Task<bool> InitProviderAsync();

        // Abstract methods for showing, hiding, and destroying banners.
        // Subclasses must implement these methods based on the provider's SDK capabilities.
        public abstract void ShowBanner();
        public abstract void HideBanner();
        public abstract void DestroyBanner();

        // Abstract methods for interstitial ads: requesting, showing, and checking if loaded.
        public abstract void RequestInterstitial();
        public abstract void ShowInterstitial(AdvertisementCallback callback);
        public abstract bool IsInterstitialLoaded();

        // Abstract methods for rewarded video ads: requesting, showing, and checking if loaded.
        public abstract void RequestRewardedVideo();
        public abstract void ShowRewardedVideo(AdvertisementCallback callback);
        public abstract bool IsRewardedVideoLoaded();

        // A delegate for handling the result of ad displays (used for interstitials and rewarded videos).
        public delegate void AdvertisementCallback(bool result);

        /// <summary>
        /// Helper function to handle ad load failures and retry
        /// </summary>
        protected void HandleAdLoadFailure(AdType adType, string errorMessage, ref int retryAttempt, SimpleCallback retryAction)
        {
            if (Monetization.VerboseLogging)
                Debug.LogError($"[AdsManager]: {adType} failed to load with error: {errorMessage}");

            retryAttempt++;
            if (retryAttempt <= MAX_RETRY_ATTEMPTS)
            {
                float retryDelay = Mathf.Pow(2, retryAttempt);
                Tween.DelayedCall(retryDelay, retryAction, true, UpdateMethod.Update);
            }
            else
            {
                Debug.LogError($"[AdsManager]: {adType} failed after {MAX_RETRY_ATTEMPTS} retries.");
            }
        }
    }
}