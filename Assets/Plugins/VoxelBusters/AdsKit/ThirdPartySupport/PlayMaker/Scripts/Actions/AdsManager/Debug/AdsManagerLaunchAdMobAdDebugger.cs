#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.AdsKit.Adapters;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Ad network debuggers)")]
    [Tooltip("Launches AdMob ad debugger. Requires AdMob adapter and test mode configuration.")]
    public class AdsManagerLaunchAdMobAdDebugger : FsmStateAction
    {
        [ActionSection("Events")]
        public FsmEvent successEvent;

        public FsmEvent failureEvent;

        private bool m_hasCompleted;

        public override void Reset()
        {
            successEvent = null;
            failureEvent = null;
            m_hasCompleted = false;
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            m_hasCompleted = false;

            AdNetworkAdapter adapter = AdsManager.AdMob;
            if (adapter == null)
            {
                HandleCallback(false, new Error("AdMob adapter is not available."));
                return;
            }

            adapter.LaunchAdDebugger(HandleCallback);
#else
            Debug.LogWarning("AdsManagerLaunchAdMobAdDebugger: Ads Kit is not enabled.");
            Fsm.Event(failureEvent);
            Finish();
#endif
        }

#if ENABLE_VOXELBUSTERS_ADS_KIT
        private void HandleCallback(bool isSuccess, Error error)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            Fsm.Event(isSuccess ? successEvent : failureEvent);
            Finish();
        }
#endif
    }
}
#endif
