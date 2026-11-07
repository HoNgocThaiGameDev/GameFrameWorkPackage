using UnityEngine;

#if UNITY_IOS && MODULE_IDFA
using Unity.Advertisement.IosSupport;
#endif

namespace NebulaSoft
{
    public sealed class IDFALoadingTask : LoadingTask
    {
        private TweenCase checkTweenCase;
        private SDKInitializer initializer;

        public IDFALoadingTask(SDKInitializer initializer) : base()
        {
            this.initializer = initializer;
        }

        public override void OnTaskActivated()
        {
#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
            if (AdsManager.IsIDFADetermined())
            {
                CompleteTask(CompleteStatus.Completed);
            }

            if (Monetization.VerboseLogging)
                Debug.Log("[Ads Manager]: Requesting IDFA..");

            ATTrackingStatusBinding.RequestAuthorizationTracking();

            CheckStatus();
#else
            CompleteTask(CompleteStatus.Skipped);
#endif
        }

        private void CheckStatus()
        {
#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
            checkTweenCase.KillActive();

            ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

            if (Monetization.VerboseLogging)
                Debug.Log($"[Ads Manager]: IDFA status - {status}");

            if (status == ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
            {
                checkTweenCase = Tween.DelayedCall(0.3f, CheckStatus, unscaledTime: true);
            }
            else
            {
                initializer.

                CompleteTask(CompleteStatus.Completed);
            }
#endif
        }
    }
}
