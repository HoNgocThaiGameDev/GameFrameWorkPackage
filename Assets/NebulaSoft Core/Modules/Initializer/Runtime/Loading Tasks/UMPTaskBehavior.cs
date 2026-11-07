using System.Collections.Generic;
using UnityEngine;

#if MODULE_ADMOB
using GoogleMobileAds.Ump.Api;
#endif

namespace NebulaSoft
{
    public sealed class UMPTaskBehavior : MonoBehaviour, ISDKTaskBehavior
    {
        [SerializeField] bool isUMPEnabled = true;
        public bool IsUMPEnabled => isUMPEnabled;

        [ShowIf("isUMPEnabled")]
        [Tooltip("Set TagForUnderAgeOfConsent (TFUA) to indicate whether a user is under the age of consent. Consent is not requested from the user when TFUA is set to true. Mixed audience apps should set this parameter for child users to ensure consent is not requested.")]
        [SerializeField] bool umpTagForUnderAgeOfConsent = false;
        public bool UMPTagForUnderAgeOfConsent => umpTagForUnderAgeOfConsent;

        [Space]
        [ShowIf("isUMPEnabled")]
        [SerializeField] bool umpDebugMode = false;
        public bool UMPDebugMode => umpDebugMode;

        [ShowIf("isUMPEnabled")]
        [SerializeField] DebugGeography umpDebugGeography;
        public DebugGeography UMPDebugGeography => umpDebugGeography;

        [ShowIf("isUMPEnabled")]
        [SerializeField] List<string> testDevices;
        public List<string> TestDevices => testDevices;

        private SDKInitializer initializer;

        private LoadingTask task;
        public LoadingTask Task => task;

        public void Init(SDKInitializer initializer)
        {
            this.initializer = initializer;

            task = new UMPLoadingTask(initializer, this);
        }

        public sealed class UMPLoadingTask : LoadingTask
        {
            private SDKInitializer initializer;
            private UMPTaskBehavior behavior;

            public UMPLoadingTask(SDKInitializer initializer, UMPTaskBehavior behavior) : base()
            {
                this.initializer = initializer;
                this.behavior = behavior;
            }

            public override void OnTaskActivated()
            {
                if(!behavior.isUMPEnabled)
                {
                    Debug.Log("[Loading]: UMP is disabled, skipping task.");

                    CompleteTask(CompleteStatus.Skipped);

                    return;
                }

#if MODULE_ADMOB
                ConsentRequestParameters requestParameters = GetRequestParameters();

                ConsentInformation.Update(requestParameters, (FormError updateError) =>
                {
                    if (IsFinished) return;
                    if (updateError != null)
                    {
                        // UMP retains consent from the previous app session. If that
                        // state is still valid, Google allows ad requests despite this
                        // launch's refresh failure.
                        if (ConsentInformation.CanRequestAds())
                        {
                            Debug.LogWarning("[Loading]: UMP refresh failed; using the previous consent state. " + updateError.Message);

                            ConsentData.SetConsentGiven(true);
                            CompleteTask(CompleteStatus.Completed);
                        }
                        else
                        {
                            Debug.LogError("[Loading]: Failed to gather consent: " + updateError.Message);

                            CompleteTask(CompleteStatus.Failed);
                        }

                        return;
                    }

                    ConsentForm.LoadAndShowConsentFormIfRequired((FormError showError) =>
                    {
                        if (IsFinished) return;
                        if (showError != null)
                        {
                            Debug.LogError("[Loading]: Failed to show consent: " + showError.Message);

                            CompleteTask(CompleteStatus.Failed);

                            return;
                        }

                        bool canRequestAds = ConsentInformation.CanRequestAds();
                        Debug.Log($"[Loading]: UMP status={ConsentInformation.ConsentStatus}, CanRequestAds={canRequestAds}.");

                        if (canRequestAds)
                        {
                            Debug.Log("[Loading]: UMP successfully completed.");

                            ConsentData.SetConsentGiven(true);

                            CompleteTask(CompleteStatus.Completed);
                        }
                        else
                        {
                            Debug.LogWarning("[Loading]: UMP completed without permission to request ads.");

                            CompleteTask(CompleteStatus.Failed);
                        }
                    });

#if UNITY_EDITOR
                    // Workaround for AdMob UMP canvas sorting order
                    GameObject canvasObject = GameObject.Find("ConsentForm(Clone)");
                    if (canvasObject != null)
                    {
                        Canvas canvas = canvasObject.GetComponent<Canvas>();
                        if (canvas != null)
                        {
                            canvas.sortingOrder = 9999;
                        }
                    }
#endif
                });
#else
                Debug.Log("[Loading]: UMP SDK is missing, skipping task.");

                CompleteTask(CompleteStatus.Skipped);
#endif
            }

#if MODULE_ADMOB
            private ConsentRequestParameters GetRequestParameters()
            {
                ConsentRequestParameters requestParameters = new ConsentRequestParameters
                {
                    TagForUnderAgeOfConsent = behavior.UMPTagForUnderAgeOfConsent
                };

                if (behavior.UMPDebugMode)
                {
                    requestParameters.ConsentDebugSettings = new ConsentDebugSettings()
                    {
                        DebugGeography = (GoogleMobileAds.Ump.Api.DebugGeography)behavior.UMPDebugGeography,
                        TestDeviceHashedIds = behavior.TestDevices
                    };
                }

                return requestParameters;
            }
#endif
        }

        public enum DebugGeography
        {
            Disabled = 0,
            EEA = 1,
            NotEEA = 2
        }
    }
}
