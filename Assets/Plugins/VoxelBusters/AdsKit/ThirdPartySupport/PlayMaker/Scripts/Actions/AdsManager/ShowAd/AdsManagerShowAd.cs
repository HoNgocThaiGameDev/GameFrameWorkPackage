#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Shows an ad for the specified placement. Emits ad lifecycle events during playback (start, click, reward, finish, fail).")]
    public class AdsManagerShowAd : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("Placement identifier configured in Ads Kit settings.")]
        public FsmString placement;

        [ActionSection("Events")]
        [Tooltip("Event sent when ad start callback is received.")]
        public FsmEvent adStartEvent;

        [Tooltip("Event sent when ad click callback is received.")]
        public FsmEvent adClickEvent;

        [Tooltip("Event sent when ad reward callback is received.")]
        public FsmEvent adRewardEvent;

        [Tooltip("Event sent when ad finish callback is received.")]
        public FsmEvent adFinishEvent;

        [Tooltip("Event sent when ad fail callback is received.")]
        public FsmEvent adFailEvent;

        private bool m_hasCompleted;
        private AdContent m_operation;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = false };
            adStartEvent = null;
            adClickEvent = null;
            adRewardEvent = null;
            adFinishEvent = null;
            adFailEvent = null;
            m_hasCompleted = false;
            m_operation = null;
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            m_hasCompleted = false;

            string placementId = placement.Value;
            if (string.IsNullOrEmpty(placementId))
            {
                HandleAdFailInternal(placementId, new Error("Placement is null or empty."));
                return;
            }

            try
            {
                m_operation = AdsManager.ShowAd(placementId);
            }
            catch (Exception ex)
            {
                HandleAdFailInternal(placementId, new Error($"ShowAd exception: {ex.Message}"));
                return;
            }

            if (m_operation == null)
            {
                HandleAdFailInternal(placementId, new Error("ShowAd operation returned null. Ensure placement is ready and Ads Kit is initialised."));
                return;
            }

            m_operation.OnAdStart += HandleAdStart;
            m_operation.OnAdClick += HandleAdClick;
            m_operation.OnAdReward += HandleAdReward;
            m_operation.OnAdFinish += HandleAdFinish;
            m_operation.OnAdFail += HandleAdFail;
#else
            Debug.LogWarning("AdsManagerShowAd: Ads Kit is not enabled.");
            Fsm.Event(adFailEvent);
            Finish();
#endif
        }

        public override void OnExit()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            UnregisterOperationCallbacks();
#endif
        }

#if ENABLE_VOXELBUSTERS_ADS_KIT
        private void HandleAdStart(AdContent adContent)
        {
            string placementId = adContent != null ? adContent.Placement : placement.Value;
            SendEvent(adStartEvent, CreateShowAdStartEventData(placementId));
        }

        private void HandleAdClick(AdContent adContent)
        {
            string placementId = adContent != null ? adContent.Placement : placement.Value;
            SendEvent(adClickEvent, CreateShowAdClickEventData(placementId));
        }

        private void HandleAdReward((AdContent content, AdReward reward) data)
        {
            string placementId = data.content != null ? data.content.Placement : placement.Value;
            SendEvent(adRewardEvent, CreateAdRewardEventData(placementId, data.reward));
        }

        private void HandleAdFinish(AdContent adContent)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            UnregisterOperationCallbacks();

            string placementId = adContent != null ? adContent.Placement : placement.Value;
            SendEvent(adFinishEvent, CreateShowAdCompleteEventData(placementId, adContent != null ? adContent.Result : null));
            Finish();
        }

        private void HandleAdFail(AdContent adContent)
        {
            string placementId = adContent != null ? adContent.Placement : placement.Value;
            Error error = adContent != null ? adContent.Error : null;
            HandleAdFailInternal(placementId, error ?? new Error("ShowAd failed."));
        }

        private void HandleAdFailInternal(string placementId, Error error)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            UnregisterOperationCallbacks();

            Debug.LogError($"AdsManagerShowAd: {error?.Description ?? "Unknown error"}");
            SendEvent(adFailEvent, CreateShowAdFailEventData(placementId, error));
            Finish();
        }

        private void UnregisterOperationCallbacks()
        {
            if (m_operation == null)
            {
                return;
            }

            m_operation.OnAdStart -= HandleAdStart;
            m_operation.OnAdClick -= HandleAdClick;
            m_operation.OnAdReward -= HandleAdReward;
            m_operation.OnAdFinish -= HandleAdFinish;
            m_operation.OnAdFail -= HandleAdFail;
            m_operation = null;
        }

        private void SendEvent(FsmEvent callbackEvent, AdsManagerEventDataWrapper wrapper = null)
        {
            if (callbackEvent == null)
            {
                if (wrapper != null)
                {
                    UnityEngine.Object.Destroy(wrapper);
                }
                return;
            }

            if (wrapper != null)
            {
                Fsm.EventData.ObjectData = wrapper;
            }

            Fsm.Event(callbackEvent);

            if (wrapper != null && Fsm.EventData.ObjectData == wrapper)
            {
                Fsm.EventData.ObjectData = null;
                UnityEngine.Object.Destroy(wrapper);
            }
        }

        private AdsManagerOnAdRewardEventDataWrapper CreateAdRewardEventData(string placementId, AdReward reward)
        {
            if (adRewardEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnAdRewardEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            wrapper.reward = reward;
            return wrapper;
        }

        private AdsManagerOnShowAdStartEventDataWrapper CreateShowAdStartEventData(string placementId)
        {
            if (adStartEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdStartEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            return wrapper;
        }

        private AdsManagerOnShowAdClickEventDataWrapper CreateShowAdClickEventData(string placementId)
        {
            if (adClickEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdClickEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            return wrapper;
        }

        private AdsManagerOnShowAdCompleteEventDataWrapper CreateShowAdCompleteEventData(string placementId, ShowAdResult result)
        {
            if (adFinishEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdCompleteEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            wrapper.result = result;
            return wrapper;
        }

        private AdsManagerOnShowAdFailEventDataWrapper CreateShowAdFailEventData(string placementId, Error error)
        {
            if (adFailEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnShowAdFailEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            wrapper.error = error;
            return wrapper;
        }
#endif
    }
}
#endif
