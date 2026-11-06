#if PLAYMAKER
using System;
using UnityEngine;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using VoxelBusters.AdsKit.Adapters;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

    namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Initialization and consent)")]
    [Tooltip("Reads initialise success data from current PlayMaker event payload (Fsm.EventData).")]
    public class AdsManagerGetInitialiseSuccessResult : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("Count of subscribed ad networks.")]
        public FsmInt subscribedNetworkCount;

        [UIHint(UIHint.Variable)]
        [ArrayEditor(VariableType.String)]
        [Tooltip("Subscribed network IDs.")]
        public FsmArray subscribedNetworkIds;

        [UIHint(UIHint.Variable)]
        [ArrayEditor(VariableType.String)]
        [Tooltip("Invalid ad network IDs.")]
        public FsmArray invalidNetworkIds;

        public override void Reset()
        {
            subscribedNetworkCount = new FsmInt { UseVariable = true };
            subscribedNetworkIds = new FsmArray { UseVariable = true };
            invalidNetworkIds = new FsmArray { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnInitialisationCompleteEventDataWrapper;
            var result = wrapper != null ? wrapper.result : null;
            if (result == null)
            {
                LogWarning("AdsManagerGetInitialiseSuccessResult: No initialise success payload found in Fsm.EventData.ObjectData.");
                ApplyDefaults();
                Finish();
                return;
            }

            AdNetworkAdapter[] networks = result.SubscribedAdNetworks ?? Array.Empty<AdNetworkAdapter>();
            string[] invalid = result.InvalidNetworks ?? Array.Empty<string>();

            if (!subscribedNetworkCount.IsNone)
            {
                subscribedNetworkCount.Value = networks.Length;
            }

            if (!subscribedNetworkIds.IsNone)
            {
                object[] values = new object[networks.Length];
                for (int i = 0; i < networks.Length; i++)
                {
                    values[i] = networks[i] != null ? (networks[i].NetworkId ?? string.Empty) : string.Empty;
                }
                subscribedNetworkIds.Values = values;
            }

            if (!invalidNetworkIds.IsNone)
            {
                object[] values = new object[invalid.Length];
                for (int i = 0; i < invalid.Length; i++)
                {
                    values[i] = invalid[i] ?? string.Empty;
                }
                invalidNetworkIds.Values = values;
            }

            Finish();
        }

        private void ApplyDefaults()
        {
            if (!subscribedNetworkCount.IsNone)
            {
                subscribedNetworkCount.Value = 0;
            }

            if (!subscribedNetworkIds.IsNone)
            {
                subscribedNetworkIds.Values = Array.Empty<object>();
            }

            if (!invalidNetworkIds.IsNone)
            {
                invalidNetworkIds.Values = Array.Empty<object>();
            }
        }
    }
}
#endif
