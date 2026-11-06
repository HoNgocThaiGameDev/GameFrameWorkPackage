#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Status and state)")]
    [Tooltip("Gets the current placement state for a placement ID.")]
    public class AdsManagerGetAdPlacementState : FsmStateAction
    {
        [ActionSection("Inputs")]
        [RequiredField]
        [Tooltip("Placement identifier configured in Ads Kit settings.")]
        public FsmString placement;

        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        [Tooltip("Current placement state.")]
        public FsmEnum placementState;

        public override void Reset()
        {
            placement = new FsmString { UseVariable = false };
            placementState = new FsmEnum { UseVariable = true, EnumType = typeof(AdPlacementState), Value = AdPlacementState.Unknown };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            string placementId = placement.Value;
            AdPlacementState state = string.IsNullOrEmpty(placementId)
                ? AdPlacementState.Unknown
                : AdsManager.GetAdPlacementState(placementId);

            if (!placementState.IsNone)
            {
                placementState.Value = state;
            }
#else
            Debug.LogWarning("AdsManagerGetAdPlacementState: Ads Kit is not enabled.");
            if (!placementState.IsNone)
            {
                placementState.Value = AdPlacementState.Unknown;
            }
#endif
            Finish();
        }
    }
}
#endif
