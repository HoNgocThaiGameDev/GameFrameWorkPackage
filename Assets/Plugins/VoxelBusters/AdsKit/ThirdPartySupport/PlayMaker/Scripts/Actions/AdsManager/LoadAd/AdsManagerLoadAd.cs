#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Loads an ad for the given placement. On SUCCESS use AdsManagerGetLoadAdSuccessResult. On FAILURE use AdsManagerGetLoadAdError.")]
    public class AdsManagerLoadAd : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("Placement identifier configured in Ads Kit settings.")]
        public FsmString placement;

        [ObjectType(typeof(AdContentOptionsAsset))]
        [Tooltip("Optional AdContentOptionsAsset. Leave empty to use default settings.")]
        public FsmObject adContentOptionsAsset;

        [ActionSection("Events")]
        [Tooltip("Event sent when load succeeds.")]
        public FsmEvent successEvent;

        [Tooltip("Event sent when load fails.")]
        public FsmEvent failureEvent;

        private bool m_hasCompleted;
        private IAsyncOperationHandle<LoadAdResult> m_operation;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = false };
            adContentOptionsAsset = new FsmObject { UseVariable = true };
            successEvent = null;
            failureEvent = null;
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
                HandleFailure(placementId, new Error("Placement is null or empty."));
                return;
            }

            AdContentOptions options = null;
            if (!adContentOptionsAsset.IsNone && adContentOptionsAsset.Value != null)
            {
                AdContentOptionsAsset optionsAsset = adContentOptionsAsset.Value as AdContentOptionsAsset;
                if (optionsAsset == null)
                {
                    HandleFailure(placementId, new Error("adContentOptionsAsset is not a valid AdContentOptionsAsset."));
                    return;
                }
                options = optionsAsset.GetRawData();
            }

            try
            {
                m_operation = AdsManager.LoadAd(placementId, options);
            }
            catch (Exception ex)
            {
                HandleFailure(placementId, new Error($"LoadAd exception: {ex.Message}"));
                return;
            }

            if (m_operation == null)
            {
                HandleFailure(placementId, new Error("LoadAd request returned null."));
                return;
            }

            m_operation.OnComplete += HandleComplete;
#else
            Debug.LogWarning("AdsManagerLoadAd: Ads Kit is not enabled.");
            Fsm.Event(failureEvent);
            Finish();
#endif
        }

        public override void OnExit()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            if (m_operation != null)
            {
                m_operation.OnComplete -= HandleComplete;
                m_operation = null;
            }
#endif
        }

#if ENABLE_VOXELBUSTERS_ADS_KIT
        private void HandleComplete(IAsyncOperationHandle<LoadAdResult> operation)
        {
            if (m_hasCompleted)
            {
                return;
            }
            if (m_operation != null)
            {
                m_operation.OnComplete -= HandleComplete;
            }

            string placementId = placement.Value;

            if (operation.Error == null && operation.Result != null)
            {
                m_hasCompleted = true;
                SendEvent(successEvent, CreateLoadAdCompleteEventData(placementId, operation.Result));
            }
            else
            {
                HandleFailure(placementId, operation.Error ?? new Error("LoadAd completed without a result."));
                return;
            }

            Finish();
        }

        private void HandleFailure(string placementId, Error error)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            Debug.LogError($"AdsManagerLoadAd: {error?.Description ?? "Unknown error"}");
            SendEvent(failureEvent, CreateLoadAdFailEventData(placementId, error));
            Finish();
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

        private AdsManagerOnLoadAdCompleteEventDataWrapper CreateLoadAdCompleteEventData(string placementId, LoadAdResult result)
        {
            if (successEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnLoadAdCompleteEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            wrapper.result = result;
            return wrapper;
        }

        private AdsManagerOnLoadAdFailEventDataWrapper CreateLoadAdFailEventData(string placementId, Error error)
        {
            if (failureEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnLoadAdFailEventDataWrapper>();
            wrapper.placement = placementId ?? string.Empty;
            wrapper.error = error;
            return wrapper;
        }
#endif
    }
}
#endif
