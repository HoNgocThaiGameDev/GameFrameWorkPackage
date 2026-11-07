#pragma warning disable CS0162

using UnityEngine;
using System.Collections;

#if MODULE_IDFA
using Unity.Advertisement.IosSupport;
#endif

namespace NebulaSoft
{
    public sealed class ATTTaskBehavior : MonoBehaviour, ISDKTaskBehavior
    {
        [SerializeField] bool isATTEnabled = true;

        private SDKInitializer initializer;

        private LoadingTask task;
        public LoadingTask Task => task;

        public void Init(SDKInitializer initializer)
        {
            this.initializer = initializer;

            task = new ATTLoadingTask(initializer, this);
        }

        public sealed class ATTLoadingTask : LoadingTask
        {
            private SDKInitializer initializer;
            private ATTTaskBehavior behavior;

            public ATTLoadingTask(SDKInitializer initializer, ATTTaskBehavior behavior) : base()
            {
                this.initializer = initializer;
                this.behavior = behavior;
            }

            public override void OnTaskActivated()
            {
                if(!behavior.isATTEnabled)
                {
                    Debug.Log("[Loading]: ATT is disabled, skipping task.");

                    CompleteTask(CompleteStatus.Skipped);

                    return;
                }

#if !MODULE_IDFA
                Debug.Log("[Loading] ATT package has not been imported. Skipping task.");

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if !UNITY_IOS
                Debug.Log("[Loading] ATT is not supported on this platform, skipping task.");

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if UNITY_EDITOR
                Debug.Log("[Loading] ATT is not supported in the Unity Editor. Skipping task.");

                CompleteTask(CompleteStatus.Skipped);

                return;
#endif

#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
                ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();
                if (status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                {
                    Debug.Log($"[Loading]: IDFA status - {status} (already set, skipping request).");

                    CompleteTask(CompleteStatus.Completed);

                    return;
                }
                
                Debug.Log("[Loading]: Requesting IDFA..");

                ATTrackingStatusBinding.RequestAuthorizationTracking();
                
                initializer.StartCoroutine(CheckStatusCoroutine());

                return;
#else
                Debug.Log("[Loading]: Something went wrong with ATT task, skipping.");

                CompleteTask(CompleteStatus.Skipped);
#endif
            }

#if UNITY_IOS && MODULE_IDFA && !UNITY_EDITOR
            private IEnumerator CheckStatusCoroutine()
            {
                while (!IsFinished)
                {
                    ATTrackingStatusBinding.AuthorizationTrackingStatus status = ATTrackingStatusBinding.GetAuthorizationTrackingStatus();

                    Debug.Log($"[Loading]: IDFA status - {status}");

                    if (status != ATTrackingStatusBinding.AuthorizationTrackingStatus.NOT_DETERMINED)
                    {
                        ConsentData.SetATTStatus((NebulaSoft.AuthorizationTrackingStatus)status);

                        CompleteTask(CompleteStatus.Completed);

                        yield break;
                    }

                    yield return new WaitForSecondsRealtime(0.5f);
                }
            }
#endif
        }
    }
}
