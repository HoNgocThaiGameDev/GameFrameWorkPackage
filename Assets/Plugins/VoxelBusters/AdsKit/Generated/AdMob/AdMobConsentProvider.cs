using System.Collections;
using System.Collections.Generic;
using GoogleMobileAds.Ump.Api;
using UnityEngine;
using UnityEngine.EventSystems;
using VoxelBusters.CoreLibrary;

using ConsentStatus = GoogleMobileAds.Ump.Api.ConsentStatus;

namespace VoxelBusters.AdsKit.Adapters
{
    public class AdMobConsentFormProvider : IConsentFormProvider
    {
        #region IConsentFormProvider implementation

        public int Priority => 128;
        public bool? IsAgeRestrictedUser { get; set; }

        public void ShowConsentForm(bool forceShow = false, CompletionCallback<ApplicationPrivacyConfiguration> callback = null)
        {
            void callbackWrapper(ApplicationPrivacyConfiguration config, Error error)
            {
                CallbackDispatcher.InvokeOnMainThread(() => callback?.Invoke(config, error));
            }

            ShowConsentForm(debugSettings: GetConsentDebugSettingsIfRequired(), IsAgeRestrictedUser, forceShow,
                            callback: callbackWrapper);
        }

        public void ResetConsentInformation()
        {
            Debug.Log("Reset consent info...");
            ConsentInformation.Reset();
        }

        #endregion

        #region Private methods

        private void ShowConsentForm(ConsentDebugSettings debugSettings, bool? isAgeRestrictedUser = null, bool forceShow = false,
                                    CompletionCallback<ApplicationPrivacyConfiguration> callback = null)
        {
            var     tagForUnderAgeOfConsent = isAgeRestrictedUser ?? false;
            var     requestParams           = new ConsentRequestParameters()
            {
                TagForUnderAgeOfConsent     = tagForUnderAgeOfConsent,
                ConsentDebugSettings        = debugSettings,
            };

            ConsentInformation.Update(requestParams,
                                      (updateError) =>
                                      {
                                          if (updateError == null)
                                          {
                                              DebugLogger.Log("Consent Status after update : " + ConsentInformation.ConsentStatus);

                                              if ((ConsentInformation.ConsentStatus == ConsentStatus.Required) || forceShow || IsEditorAndDebugBuild())
                                              {
                                                  ShowConsentFormInternal(isAgeRestrictedUser, callback);
                                              }
                                              else
                                              {
                                                  var     config  = new ApplicationPrivacyConfiguration(usageConsent: AdMobUtility.ConvertToUnityConsentStatus(ConsentInformation.ConsentStatus), isAgeRestrictedUser: isAgeRestrictedUser);
                                                  callback?.Invoke(config, null);
                                              }
                                          }
                                          else
                                          {
                                              DebugLogger.LogError("Failed to present consent form with error: " + updateError.Message);

                                              callback?.Invoke(null, new Error(updateError.Message));
                                          }
                                      });
        }

        private void ShowConsentFormInternal(bool? isAgeRestrictedUser, CompletionCallback < ApplicationPrivacyConfiguration> callback)
        {
            ConsentForm.Load((form, loadError) =>
            {
                if (loadError == null)
                {
                    WarnIfNoEventSystem();

                    form.Show((showError) =>
                    {
                        if (showError == null)
                        {
                            var     config  = new ApplicationPrivacyConfiguration(usageConsent: AdMobUtility.ConvertToUnityConsentStatus(ConsentInformation.ConsentStatus), isAgeRestrictedUser: isAgeRestrictedUser);
                            callback?.Invoke(config, null);
                        }
                        else
                        {
                            callback?.Invoke(null, new Error(showError.Message));
                        }
                    });
                }
                else
                {
                    callback?.Invoke(null, new Error(loadError.Message));
                }
            });
        }

        private bool IsEditorAndDebugBuild() => (Application.isEditor && IsTestMode());

        private bool IsTestMode()
        {
            return AdsKitSettings.Instance.IsTestMode == true;
        }

        private ConsentDebugSettings GetConsentDebugSettingsIfRequired()
        {
            if (IsTestMode())
            {
                return new ConsentDebugSettings()
                {
                    TestDeviceHashedIds = new List<string>(AdsKitSettings.Instance.TestDevices.GetDeviceIdsForPlatform(Application.platform))
                };
            }
            else
            {
                return new ConsentDebugSettings();
            }
        }

        /// <summary>
        /// Logs a warning if no UI event system available in the current scene.
        /// </summary>
        private void WarnIfNoEventSystem()
        {
            if (EventSystem.current == null)
            {
                Debug.LogWarning("No event system available in the current scene. You need to add an UI Event System for capturing the input from the consent form.");
            }
        }

        #endregion
    }
}