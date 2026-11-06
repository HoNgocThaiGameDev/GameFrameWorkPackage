using System.Collections.Generic;
using UnityEngine;
using VoxelBusters.CoreLibrary;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;


namespace VoxelBusters.AdsKit.Adapters
{
    [AdNetwork(AdNetworkServiceId.kGoogleMobileAds)]
    public class AdMobAdapter : AdNetworkAdapter
    {
        #region Fields

        private     bool                                            m_isInitialised;

        private     Dictionary<string, BannerView>                  m_bannerViewCache                           = new Dictionary<string, BannerView>(capacity: 8);

        private Dictionary<string, InterstitialAd> m_interstitialViewCache = new Dictionary<string, InterstitialAd>(capacity: 8);
        
        private     Dictionary<string, RewardedInterstitialAd>      m_rewardedInterstitialViewCache     = new Dictionary<string, RewardedInterstitialAd>(capacity: 8);

        private     Dictionary<string, RewardedAd>                  m_rewardedViewCache                         = new Dictionary<string, RewardedAd>(capacity: 8);

        private     Dictionary<string, Callback<BannerView>>        m_bannerAdResponseCallbackCollection        = new Dictionary<string, Callback<BannerView>>(capacity: 8);

        private     Dictionary<string, Callback<InterstitialAd>>    m_interstitialAdResponseCallbackCollection  = new Dictionary<string, Callback<InterstitialAd>>(capacity: 8);

        private     Dictionary<string, Callback<RewardedAd>>        m_rewardedAdResponseCallbackCollection = new Dictionary<string, Callback<RewardedAd>>(capacity: 8);
        private     Dictionary<string, Callback<RewardedInterstitialAd>>    m_rewardedInterstitialAdResponseCallbackCollection = new Dictionary<string, Callback<RewardedInterstitialAd>>(capacity: 8);

        #endregion

        #region Static methods

        public static void SetRequestConfiguration(RequestConfiguration requestConfiguration)
        {
            Assert.IsArgNotNull(requestConfiguration, nameof(requestConfiguration));
            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        private static RequestConfiguration GetOrCreateRequestConfiguration(ApplicationPrivacyConfiguration privacyConfiguration)
        {
            var     requestConfig   = MobileAds.GetRequestConfiguration();
            if (requestConfig == null)
            {
                requestConfig       = new RequestConfiguration();
            }

            var contentRating = AdMobUtility.ConvertToNativeContentRating(privacyConfiguration.PreferredContentRating);
            requestConfig.MaxAdContentRating = contentRating;
            requestConfig.TagForUnderAgeOfConsent = AdMobUtility.ConvertToNativeUnderAgeOfConsent(privacyConfiguration.IsAgeRestrictedUser);

            return requestConfig;
        }

        #endregion

        #region Base class methods

        public override bool IsInitialised => m_isInitialised;

        public override bool IsSupported => Application.isEditor || (Application.platform == RuntimePlatform.Android) || (Application.platform == RuntimePlatform.IPhonePlayer);

        public override void Initialise(AdNetworkInitialiseProperties properties)
        {
            m_isTestMode            = properties.IsTestMode;
            SetRequestConfiguration(properties);

            MobileAds.RaiseAdEventsOnUnityMainThread = true;
            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                if (initStatus != null)
                {
                    var adapterStatusMap = initStatus.getAdapterStatusMap();
                    if (adapterStatusMap != null)
                    {
                        foreach (var item in adapterStatusMap)
                        {
                            DebugLogger.Log($"[Adapter : {item.Key}] => [State : {item.Value.InitializationState}] [Latency : {item.Value.Latency}] [Description : {item.Value.Description}]");
                        }
                    }
                    m_isInitialised = true;
                }

                AdNetworkInitialiseStateInfo stateInfo;

                if (m_isInitialised)
                {
                    stateInfo   = new AdNetworkInitialiseStateInfo(networkId: NetworkId,
                                                                status: AdNetworkInitialiseStatus.Success);
                }
                else
                {
                    stateInfo   = new AdNetworkInitialiseStateInfo(networkId: NetworkId,
                                                                status: AdNetworkInitialiseStatus.Fail,
                                                                error: AdError.InitializationError());
                }

                SendInitaliseStateChangeEvent(stateInfo);
            });
        }

        public override AdPlacementState GetPlacementState(string placement)
        {
            return AdPlacementState.Unknown;
        }

        public override void LoadAd(string placement, AdContentOptions options)
        {
            var     placementMeta   = GetAdPlacementMeta(placement);
            var     adMeta          = GetAdMetaWithPlacement(placement);
            string  adUnitId        = adMeta.GetAdUnitIdForActiveOrSimulationPlatform(m_isTestMode);
            switch (placementMeta.AdType)
            {
                case AdType.Banner:
                    RequestBannerAd(adUnitId, options as BannerAdOptions);
                    break;

                case AdType.Interstitial:
                    RequestInterstitialAd(adUnitId);
                    break;

                case AdType.RewardedInterstitial:
                    RequestRewardedInterstitialAd(adUnitId);
                    break;

                case AdType.RewardedVideo:
                    RequestRewardedAd(adUnitId);
                    break;

                default:
                    throw VBException.SwitchCaseNotImplemented(placementMeta.AdType);
            }
        }

        public override void ShowAd(string placement)
        {
            var     placementMeta   = GetAdPlacementMeta(placement);
            var     adMeta          = GetAdMetaWithPlacement(placement);
            string  adUnitId        = adMeta.GetAdUnitIdForActiveOrSimulationPlatform(m_isTestMode);
            switch (placementMeta.AdType)
            {
                case AdType.Banner:
                    ShowBannerAd(adUnitId);
                    break;

                case AdType.Interstitial:
                    ShowInterstitialAd(adUnitId);
                    break;

                case AdType.RewardedInterstitial:
                    ShowRewardedInterstitialVideoAd(adUnitId);
                    break;

                case AdType.RewardedVideo:
                    ShowRewardedVideoAd(adUnitId);
                    break;

                default:
                    throw VBException.SwitchCaseNotImplemented(placementMeta.AdType);
            }
        }

        public override void HideBanner(string placement, bool destroy = false)
        {
            var     adMeta          = GetAdMetaWithPlacement(placement);
            string  adUnitId        = adMeta.GetAdUnitIdForActiveOrSimulationPlatform(m_isTestMode);
            if (m_bannerViewCache.TryGetValue(adUnitId, out BannerView adView))
            {
                adView.Hide();

                if (destroy)
                {
                    m_bannerViewCache.Remove(adUnitId);
                    HandleAdShowComplete(adUnitId);
                    adView.Destroy();
                }
            }
        }

        public override void SetPaused(bool pauseStatus)
        {
            MobileAds.SetiOSAppPauseOnBackground(pauseStatus);
        }

        public override void SetPrivacyConfiguration(ApplicationPrivacyConfiguration config)
        {
            var     requestConfiguration                    = GetOrCreateRequestConfiguration(config);
            SetRequestConfiguration(requestConfiguration);
        }

        public override void SetUser(User user)
        { }

        public override void SetUserSettings(UserSettings settings)
        {
            if (settings.Muted != null)
            {
                MobileAds.SetApplicationMuted(settings.Muted.Value);
            }
            if (settings.Volume != null)
            {
                MobileAds.SetApplicationVolume(settings.Volume.Value);
            }
        }

        public override void SetOrientation(ScreenOrientation orientation)
        { }


        public override void LaunchAdDebugger(EventCallback<bool> callback = null)
        {
            if (IsConfigValidForAdDebuggerUsage())
            {
                MobileAds.OpenAdInspector((AdInspectorError error) =>
                {
                    if (error != null)
                    {
                        DebugLogger.LogError(AdsKitSettings.Domain, error.GetMessage());
                        CallbackDispatcher.InvokeOnMainThread(() => callback(false, AdError.Unknown(error.GetMessage())));
                    }
                    else
                    {
                        CallbackDispatcher.InvokeOnMainThread(() => callback(true, null));
                    }
                });
            }
            else
            {
                CallbackDispatcher.InvokeOnMainThread(() => callback(false, AdError.NotTestMode()));
            }
        }

        #endregion

        #region Private methods

        private void SetRequestConfiguration(AdNetworkInitialiseProperties properties)
        {
            var config = GetOrCreateRequestConfiguration(properties.PrivacyConfiguration);
            if (properties.IsTestMode)
            {
                config.TestDeviceIds.AddRange(Manager.GetTestDeviceIds());
            }
            SetRequestConfiguration(config);
        }

        private void RequestBannerAd(string adUnitId, BannerAdOptions adOptions, Callback<BannerView> callback = null)
        {
            BannerView  banner;
            if (m_bannerViewCache.TryGetValue(adUnitId, out banner))
            {
                m_bannerViewCache.Remove(adUnitId);
                banner.Destroy();
            }

            // Check whether request is being processed
            AddRequestCallbackToCollection(adUnitId, m_bannerAdResponseCallbackCollection, callback, out bool requestExists);
            if (requestExists) return;

            // Create banner request
            if(adOptions.Position.Preset.HasValue)
            {
                banner = new BannerView(adUnitId,
                                    AdMobUtility.ConvertToNativeAdSize(adOptions.Size),
                                    AdMobUtility.ConvertToNativeAdPosition(adOptions.Position.Preset.Value));
            }
            else
            {
                Vector2Int position = adOptions.Position.Absolute.Value;
                banner = new BannerView(adUnitId,
                                    AdMobUtility.ConvertToNativeAdSize(adOptions.Size),
                                    position.x,
                                    position.y
                                    );
            }

            bool bannerNewlyCreated = true;
 
            // Register for ad events.
            banner.OnBannerAdLoaded        += () =>
            {
                if(bannerNewlyCreated)
                {
                    // Hide to banner
                    banner.Hide();
                    bannerNewlyCreated = false; //We need to hide it only when the banner is first loaded as we don't show by default. For later reloads, we shouldn't hide as the ad might be shown by that time.
                }

                // Store reference
                m_bannerViewCache[adUnitId] = banner;

                // Send callback
                var     batchedCallback     = GetRequestCallback(adUnitId, m_bannerAdResponseCallbackCollection);
                batchedCallback?.Invoke(banner);

                HandleAdLoaded(adUnitId);
            };
            banner.OnBannerAdLoadFailed    += (error) =>
            {
                // Send callback
                var     batchedCallback     = GetRequestCallback(adUnitId, m_bannerAdResponseCallbackCollection);
                batchedCallback?.Invoke(null);

                HandleAdFailedToLoad(adUnitId, error);
            };
            //banner.OnAdFullScreenContentOpened  += () => { HandleAdShowStart(adUnitId); };
            //banner.OnAdFullScreenContentClosed  += () => { HandleAdShowComplete(adUnitId); };
            banner.OnAdClicked                  += () => { HandleAdClicked(adUnitId); };
            banner.OnAdPaid                     += (adValue) => { HandleAdPaid(adUnitId, adValue); };

            // Start request
            banner.LoadAd(CreateAdRequest());
        }

        private void RequestInterstitialAd(string adUnitId, Callback<InterstitialAd> callback = null)
        {
            InterstitialAd cachedAdView;
            if (m_interstitialViewCache.TryGetValue(adUnitId, out cachedAdView))
            {
                cachedAdView.Destroy();
            }

            // Check whether request is being processed
            AddRequestCallbackToCollection(adUnitId, m_interstitialAdResponseCallbackCollection, callback, out bool requestExists);
            if (requestExists) return;

            // Make a request to create new instance
            InterstitialAd.Load(adUnitId, CreateAdRequest(), (newAdView, loadError) =>
            {
                var batchedCallback = GetRequestCallback(adUnitId, m_interstitialAdResponseCallbackCollection);
                if ((loadError != null) || (newAdView == null))
                {
                    // Send callback data
                    batchedCallback?.Invoke(null);

                    HandleAdFailedToLoad(adUnitId, loadError);
                }
                else
                {
                    // Store reference
                    m_interstitialViewCache[adUnitId] = newAdView;

                    // Send callback data
                    batchedCallback?.Invoke(newAdView);

                    // Set callbacks
                    newAdView.OnAdFullScreenContentOpened += () => { HandleAdShowStart(adUnitId); };
                    newAdView.OnAdFullScreenContentClosed += () => { HandleAdShowComplete(adUnitId); };
                    newAdView.OnAdFullScreenContentFailed += (error) => { HandleAdFailedToShow(adUnitId, error); };
                    newAdView.OnAdClicked += () => { HandleAdClicked(adUnitId); };
                    newAdView.OnAdPaid += (adValue) => { HandleAdPaid(adUnitId, adValue); };
                    newAdView.OnAdImpressionRecorded += () => { HandleAdImpressionRecorded(adUnitId); };

                    // Send load complete callback
                    HandleAdLoaded(adUnitId);
                }
            });
        }
        
        private void RequestRewardedInterstitialAd(string adUnitId, Callback<RewardedInterstitialAd> callback = null)
        {
            RewardedInterstitialAd  cachedAdView;
            if (m_rewardedInterstitialViewCache.TryGetValue(adUnitId, out cachedAdView))
            {
                cachedAdView.Destroy();
            }

            // Check whether request is being processed
            AddRequestCallbackToCollection(adUnitId, m_rewardedInterstitialAdResponseCallbackCollection, callback, out bool requestExists);
            if (requestExists) return;

            // Make a request to create new instance
            RewardedInterstitialAd.Load(adUnitId, CreateAdRequest(), (newAdView, loadError) =>
            {
                var     batchedCallback = GetRequestCallback(adUnitId, m_rewardedInterstitialAdResponseCallbackCollection);

                if ((loadError != null) || (newAdView == null))
                {
                    // Send callback data
                    batchedCallback?.Invoke(null);

                    HandleAdFailedToLoad(adUnitId, loadError);
                }
                else
                {
                    // Store reference
                    m_rewardedInterstitialViewCache[adUnitId]    = newAdView;
                    
                    // Send callback data
                    batchedCallback?.Invoke(newAdView);

                    // Set callbacks
                    newAdView.OnAdFullScreenContentOpened   += () => { HandleAdShowStart(adUnitId); };
                    newAdView.OnAdFullScreenContentClosed   += () => { HandleAdShowComplete(adUnitId); };
                    newAdView.OnAdFullScreenContentFailed   += (error) => { HandleAdFailedToShow(adUnitId, error); };
                    newAdView.OnAdClicked                   += () => { HandleAdClicked(adUnitId); };
                    newAdView.OnAdPaid                      += (adValue) => { HandleAdPaid(adUnitId, adValue); };
                    newAdView.OnAdImpressionRecorded        += () => { HandleAdImpressionRecorded(adUnitId); };

                    // Send load complete callback
                    HandleAdLoaded(adUnitId);
                }
            });
        }

        private void RequestRewardedAd(string adUnitId, Callback<RewardedAd> callback = null)
        {
            RewardedAd  cachedAdView;
            if (m_rewardedViewCache.TryGetValue(adUnitId, out cachedAdView))
            {
                cachedAdView.Destroy();
            }

            // Check whether request is being processed
            AddRequestCallbackToCollection(adUnitId, m_rewardedAdResponseCallbackCollection, callback, out bool requestExists);
            if (requestExists) return;

            // Make a request to create new instance
            RewardedAd.Load(adUnitId, CreateAdRequest(), (newAdView, loadError) =>
            {
                var     batchedCallback = GetRequestCallback(adUnitId, m_rewardedAdResponseCallbackCollection);

                if ((loadError != null) || (newAdView == null))
                {
                    // Send callback data
                    batchedCallback?.Invoke(null);

                    HandleAdFailedToLoad(adUnitId, loadError);
                }
                else
                {
                    // Store reference
                    m_rewardedViewCache[adUnitId]    = newAdView;
                    
                    // Send callback data
                    batchedCallback?.Invoke(newAdView);

                    // Set callbacks
                    newAdView.OnAdFullScreenContentOpened   += () => { HandleAdShowStart(adUnitId); };
                    newAdView.OnAdFullScreenContentClosed   += () => { HandleAdShowComplete(adUnitId); };
                    newAdView.OnAdFullScreenContentFailed   += (error) => { HandleAdFailedToShow(adUnitId, error); };
                    newAdView.OnAdClicked                   += () => { HandleAdClicked(adUnitId); };
                    newAdView.OnAdPaid                      += (adValue) => { HandleAdPaid(adUnitId, adValue); };
                    newAdView.OnAdImpressionRecorded        += () => { HandleAdImpressionRecorded(adUnitId); };

                    // Send load complete callback
                    HandleAdLoaded(adUnitId);
                }
            });
        }

        private AdRequest CreateAdRequest()
        {
            return new AdRequest();
        }

        private void ShowBannerAd(string adUnitId)
        {
            BannerView  cachedAdView;
            if (m_bannerViewCache.TryGetValue(adUnitId, out cachedAdView))
            {
                cachedAdView.Show();
                HandleAdShowStart(adUnitId);
            }
            else
            {
                RequestBannerAd(adUnitId, null, (newAdView) =>
                {
                    if (newAdView != null)
                    {
                        newAdView.Show();
                        HandleAdShowStart(adUnitId);
                    }
                });
            }
        }

        private void ShowInterstitialAd(string adUnitId)
        {
            InterstitialAd cachedAdView;
            if (m_interstitialViewCache.TryGetValue(adUnitId, out cachedAdView) && cachedAdView.CanShowAd())
            {
                cachedAdView.Show();
            }
            else
            {
                RequestInterstitialAd(adUnitId, (newAdView) =>
                {
                    newAdView?.Show();
                });
            }
        }

        private void ShowRewardedInterstitialVideoAd(string adUnitId)
        {
            RewardedInterstitialAd  cachedAdView;
            if (m_rewardedInterstitialViewCache.TryGetValue(adUnitId, out cachedAdView) && cachedAdView.CanShowAd())
            {
                cachedAdView.Show((reward) => { 
                    HandleAdReward(adUnitId, reward);
                });
            }
            else
            {
                RequestRewardedInterstitialAd(adUnitId, (newAdView) =>
                {
                    newAdView?.Show((reward) => {
                        HandleAdReward(adUnitId, reward);
                     });
                });
            }
        }

        private void ShowRewardedVideoAd(string adUnitId)
        {
            RewardedAd  cachedAdView;
            if (m_rewardedViewCache.TryGetValue(adUnitId, out cachedAdView) && cachedAdView.CanShowAd())
            {
                cachedAdView.Show((reward) => { 
                    HandleAdReward(adUnitId, reward);
                });
            }
            else
            {
                RequestRewardedAd(adUnitId, (newAdView) =>
                {
                    newAdView?.Show((reward) => {
                        HandleAdReward(adUnitId, reward);
                     });
                });
            }
        }

        private void AddRequestCallbackToCollection<T>(string adUnitId,
                                                       Dictionary<string, Callback<T>> collection,
                                                       Callback<T> newCallback, out bool requestExists)
        {
            if ((requestExists = collection.TryGetValue(adUnitId, out Callback<T> batchedCallback)) == false)
            {
                collection[adUnitId]     = newCallback;
            }
            else if (newCallback != null)
            {
                batchedCallback         += newCallback;
                collection[adUnitId]     = batchedCallback;
            }
        }

        private Callback<T> GetRequestCallback<T>(string adUnitId, Dictionary<string, Callback<T>> collection)
        {
            if (collection.TryGetValue(adUnitId, out Callback<T> callback))
            {
                collection.Remove(adUnitId);
                return callback;
            }
            return null;
        }

        #endregion

        #region Event handler methods

        private void HandleAdLoaded(string adUnitId)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     stateInfo       = new AdNetworkLoadAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   networkId: NetworkId,
                                                                   placement: placementMeta.Name,
                                                                   placementState: AdPlacementState.ReadyToShow);
            SendLoadAdStateChangeEvent(stateInfo);
        }

        private void HandleAdFailedToLoad(string adUnitId, LoadAdError error)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     castedError     = (error != null)
                ? AdError.LoadFail(error.GetMessage())
                : AdError.Unknown();
            var     stateInfo       = new AdNetworkLoadAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   networkId: NetworkId,
                                                                   placement: placementMeta.Name,
                                                                   placementState: AdPlacementState.LoadFailed,
                                                                   error: castedError);
            SendLoadAdStateChangeEvent(stateInfo);
        }

        private void HandleAdShowStart(string adUnitId)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     stateInfo       = new AdNetworkShowAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   placement: placementMeta.Name,
                                                                   networkId: NetworkId,
                                                                   state: ShowAdState.Started);
            SendShowAdStateChangeEvent(stateInfo);
        }

        private void HandleAdClicked(string adUnitId)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     stateInfo       = new AdNetworkShowAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   placement: placementMeta.Name,
                                                                   networkId: NetworkId,
                                                                   state: ShowAdState.Clicked);
            SendShowAdStateChangeEvent(stateInfo);
        }

        private void HandleAdPaid(string adUnitId, AdValue value)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     paymentInfo     = new AdTransaction(adUnitId: adUnitId,
                                                        adType: placementMeta.AdType,
                                                        placement: placementMeta.Name,
                                                        networkId: NetworkId,
                                                        currencyCode: value.CurrencyCode,
                                                        value: value.Value,
                                                        precision: AdMobUtility.ConvertToNativeAdTransactionPrecisionType(value.Precision));
            SendAdPaidEvent(paymentInfo);
        }

        private void HandleAdImpressionRecorded(string adUnitId)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     impressionInfo  = new AdNetworkAdImpressionInfo(adUnitId: adUnitId,
                                                                    adType: placementMeta.AdType,
                                                                    placement: placementMeta.Name,
                                                                    networkId: NetworkId);
            SendAdImpressionRecordedEvent(impressionInfo);
        }

        private void HandleAdShowComplete(string adUnitId)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     stateInfo       = new AdNetworkShowAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   placement: placementMeta.Name,
                                                                   networkId: NetworkId,
                                                                   state: ShowAdState.Finished,
                                                                   clicked: IsAdClicked(adUnitId),
                                                                   resultCode: ShowAdResultCode.Finished);
            SendShowAdStateChangeEvent(stateInfo);
        }

        public void HandleAdFailedToShow(string adUnitId, GoogleMobileAds.Api.AdError error)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     stateInfo       = new AdNetworkShowAdStateInfo(adUnitId: adUnitId,
                                                                   adType: placementMeta.AdType,
                                                                   placement: placementMeta.Name,
                                                                   networkId: NetworkId,
                                                                   state: ShowAdState.Failed,
                                                                   error: new Error(error.GetDomain(), error.GetCode(), error.GetMessage()));
            SendShowAdStateChangeEvent(stateInfo);
        }

        private void HandleAdReward(string adUnitId, Reward reward)
        {
            var     placementMeta   = GetAdPlacementMetaWithAdUnitId(adUnitId: adUnitId);
            var     rewardInfo  = new AdReward(adUnitId: adUnitId,
                                                                    adType: placementMeta.AdType,
                                                                    placement: placementMeta.Name,
                                                                    networkId: NetworkId,
                                                                    amount: reward.Amount,
                                                                    type: reward.Type);
            SendAdRewardEvent(rewardInfo);
        }


        #endregion

        #region Protected methods
        protected override AdViewProxy GetAdViewProxy(AdType adType, string adUnitId)
        {
            switch(adType)
            {
                case AdType.Banner:
                    if (m_bannerViewCache.TryGetValue(adUnitId, out BannerView banner))
                    {
                        return new AdViewProxy(new Vector2Int((int)banner.GetWidthInPixels(), (int)banner.GetHeightInPixels()));
                    }
                    break;
            }
            
            return null;
        }

        #endregion
    }
}
