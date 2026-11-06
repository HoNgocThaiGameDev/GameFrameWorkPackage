#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;
using Object = UnityEngine.Object;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Listener)")]
    [Tooltip("Listener action for all ad callbacks. Keep this action active to receive events. This action only fires events.")]
    public class AdsManagerAdEventListener : AdsManagerListenerActionBase
    {
        [ActionSection("Events")]
        public FsmEvent onLoadAdComplete;
        public FsmEvent onLoadAdFail;
        public FsmEvent onShowAdStart;
        public FsmEvent onShowAdClick;
        public FsmEvent onShowAdComplete;
        public FsmEvent onShowAdFail;
        public FsmEvent onAdImpressionRecorded;
        public FsmEvent onAdPaid;
        public FsmEvent onAdReward;

        public override void Reset()
        {
            base.Reset();
            onLoadAdComplete = null;
            onLoadAdFail = null;
            onShowAdStart = null;
            onShowAdClick = null;
            onShowAdComplete = null;
            onShowAdFail = null;
            onAdImpressionRecorded = null;
            onAdPaid = null;
            onAdReward = null;
        }

        public override void OnLoadAdComplete(string placementId, LoadAdResult result)
        {
            SendEvent(onLoadAdComplete, () => CreateLoadAdCompleteEventData(onLoadAdComplete, placementId, result));
        }

        public override void OnLoadAdFail(string placementId, Error error)
        {
            SendEvent(onLoadAdFail, () => CreateLoadAdFailEventData(onLoadAdFail, placementId, error));
        }

        public override void OnShowAdStart(string placementId)
        {
            SendEvent(onShowAdStart, () => CreateShowAdStartEventData(onShowAdStart, placementId));
        }

        public override void OnShowAdClick(string placementId)
        {
            SendEvent(onShowAdClick, () => CreateShowAdClickEventData(onShowAdClick, placementId));
        }

        public override void OnShowAdComplete(string placementId, ShowAdResult result)
        {
            SendEvent(onShowAdComplete, () => CreateShowAdCompleteEventData(onShowAdComplete, placementId, result));
        }

        public override void OnShowAdFail(string placementId, Error error)
        {
            SendEvent(onShowAdFail, () => CreateShowAdFailEventData(onShowAdFail, placementId, error));
        }

        public override void OnAdImpressionRecorded(string placementId)
        {
            SendEvent(onAdImpressionRecorded, () => CreateAdImpressionRecordedEventData(onAdImpressionRecorded, placementId));
        }

        public override void OnAdPaid(string placementId, AdTransaction transaction)
        {
            SendEvent(onAdPaid, () => CreateAdPaidEventData(onAdPaid, placementId, transaction));
        }

        public override void OnAdReward(string placementId, AdReward reward)
        {
            SendEvent(onAdReward, () => CreateAdRewardEventData(onAdReward, placementId, reward));
        }

        private void SendEvent(FsmEvent callbackEvent, Func<AdsManagerEventDataWrapper> wrapperFactory)
        {
            if (callbackEvent == null)
            {
                return;
            }

            AdsManagerEventDataWrapper wrapper = wrapperFactory != null ? wrapperFactory() : null;
            if (wrapper != null)
            {
                Fsm.EventData.ObjectData = wrapper;
            }

            Fsm.Event(callbackEvent);

            if (wrapper != null && Fsm.EventData.ObjectData == wrapper)
            {
                Fsm.EventData.ObjectData = null;
                Object.Destroy(wrapper);
            }
        }

        private AdsManagerOnLoadAdCompleteEventDataWrapper CreateLoadAdCompleteEventData(FsmEvent callbackEvent, string placementId, LoadAdResult result)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnLoadAdCompleteEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.result = result;
            return wrapper;
        }

        private AdsManagerOnLoadAdFailEventDataWrapper CreateLoadAdFailEventData(FsmEvent callbackEvent, string placementId, Error error)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnLoadAdFailEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.error = error;
            return wrapper;
        }

        private AdsManagerOnShowAdStartEventDataWrapper CreateShowAdStartEventData(FsmEvent callbackEvent, string placementId)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdStartEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            return wrapper;
        }

        private AdsManagerOnShowAdClickEventDataWrapper CreateShowAdClickEventData(FsmEvent callbackEvent, string placementId)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdClickEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            return wrapper;
        }

        private AdsManagerOnShowAdCompleteEventDataWrapper CreateShowAdCompleteEventData(FsmEvent callbackEvent, string placementId, ShowAdResult result)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdCompleteEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.result = result;
            return wrapper;
        }

        private AdsManagerOnShowAdFailEventDataWrapper CreateShowAdFailEventData(FsmEvent callbackEvent, string placementId, Error error)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdFailEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.error = error;
            return wrapper;
        }

        private AdsManagerOnAdImpressionRecordedEventDataWrapper CreateAdImpressionRecordedEventData(FsmEvent callbackEvent, string placementId)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnAdImpressionRecordedEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            return wrapper;
        }

        private AdsManagerOnAdPaidEventDataWrapper CreateAdPaidEventData(FsmEvent callbackEvent, string placementId, AdTransaction transaction)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnAdPaidEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.transaction = transaction;
            return wrapper;
        }

        private AdsManagerOnAdRewardEventDataWrapper CreateAdRewardEventData(FsmEvent callbackEvent, string placementId, AdReward reward)
        {
            if (!ShouldCreatePayload(callbackEvent))
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnAdRewardEventDataWrapper>();
            wrapper.placement = SafeString(placementId);
            wrapper.reward = reward;
            return wrapper;
        }

        private bool ShouldCreatePayload(FsmEvent callbackEvent)
        {
            if (callbackEvent == null)
            {
                return false;
            }

            if (HasTransition(State != null ? State.Transitions : null, callbackEvent))
            {
                return true;
            }

            return HasTransition(Fsm != null ? Fsm.GlobalTransitions : null, callbackEvent);
        }

        private static bool HasTransition(FsmTransition[] transitions, FsmEvent callbackEvent)
        {
            if (transitions == null)
            {
                return false;
            }

            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null && transitions[i].FsmEvent == callbackEvent)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
