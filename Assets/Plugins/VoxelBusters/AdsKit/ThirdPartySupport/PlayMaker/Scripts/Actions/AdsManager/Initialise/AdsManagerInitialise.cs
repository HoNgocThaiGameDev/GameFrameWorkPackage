#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using VoxelBusters.CoreLibrary;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Initialization and consent)")]
    [Tooltip("Initialises Ads Kit using the highest-priority consent form provider. On SUCCESS use AdsManagerGetInitialiseSuccessResult. On FAILURE use AdsManagerGetInitialiseError.")]
    public class AdsManagerInitialise : FsmStateAction
    {
        [ActionSection("Inputs")]
        [Tooltip("Optionally clears cached consent information on all discovered providers before initialise.")]
        public FsmBool resetConsentInformation;

        [ActionSection("Events")]
        [Tooltip("Event sent when initialise succeeds.")]
        public FsmEvent successEvent;

        [Tooltip("Event sent when initialise fails.")]
        public FsmEvent failureEvent;

        private bool m_hasCompleted;
        private IAsyncOperation<InitialiseResult> m_operation;

        public override void Reset()
        {
            resetConsentInformation = new FsmBool { UseVariable = false, Value = false };
            successEvent = null;
            failureEvent = null;
            m_hasCompleted = false;
            m_operation = null;
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            try
            {
                m_hasCompleted = false;

                if (!resetConsentInformation.IsNone && resetConsentInformation.Value)
                {
                    AdServices.ResetConsentInformation();
                }

                IConsentFormProvider consentFormProvider = AdServices.GetConsentFormProvider();
                if (consentFormProvider == null)
                {
                    HandleFailure(new Error("No IConsentFormProvider implementation found. Enable an ad network provider or add a custom consent provider."));
                    return;
                }

                m_operation = AdsManager.Initialise(consentFormProvider);
                if (m_operation == null)
                {
                    HandleFailure(new Error("Initialise operation returned null. Ensure Ads Kit is enabled and configured."));
                    return;
                }

                m_operation.OnComplete += HandleComplete;
            }
            catch (Exception ex)
            {
                HandleFailure(new Error($"Initialise exception: {ex.Message}"));
            }
#else
            Debug.LogWarning("AdsManagerInitialise: Ads Kit is not enabled.");
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
        private void HandleComplete(IAsyncOperation<InitialiseResult> operation)
        {
            if (m_hasCompleted)
            {
                return;
            }
            if (m_operation != null)
            {
                m_operation.OnComplete -= HandleComplete;
            }

            if (operation.Error == null && operation.Result != null)
            {
                m_hasCompleted = true;
                SendEvent(successEvent, CreateInitialiseSuccessEventData(operation.Result));
            }
            else
            {
                HandleFailure(operation.Error ?? new Error("Initialise completed without a result."));
                return;
            }

            Finish();
        }

        private void HandleFailure(Error error)
        {
            if (m_hasCompleted)
            {
                return;
            }

            m_hasCompleted = true;
            Debug.LogError($"AdsManagerInitialise: {error?.Description ?? "Unknown error"}");
            SendEvent(failureEvent, CreateInitialiseFailureEventData(error));
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

        private AdsManagerOnInitialisationCompleteEventDataWrapper CreateInitialiseSuccessEventData(InitialiseResult result)
        {
            if (successEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnInitialisationCompleteEventDataWrapper>();
            wrapper.result = result;
            return wrapper;
        }

        private AdsManagerOnInitialisationFailEventDataWrapper CreateInitialiseFailureEventData(Error error)
        {
            if (failureEvent == null)
            {
                return null;
            }

            var wrapper = ScriptableObject.CreateInstance<AdsManagerOnInitialisationFailEventDataWrapper>();
            wrapper.error = error;
            return wrapper;
        }
#endif
    }
}
#endif
