#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Configuration setters)")]
    [Tooltip("Sets orientation for Ads Kit adapters.")]
    public class AdsManagerSetOrientation : FsmStateAction
    {
        [ActionSection("Inputs")]
        [Tooltip("Orientation to set on Ads Kit.")]
        public FsmEnum orientation;

        public override void Reset()
        {
            orientation = new FsmEnum
            {
                UseVariable = false,
                EnumType = typeof(ScreenOrientation),
                Value = ScreenOrientation.AutoRotation
            };
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            ScreenOrientation value = !orientation.IsNone
                ? (ScreenOrientation)orientation.Value
                : ScreenOrientation.AutoRotation;
            AdsManager.SetOrientation(value);
#else
            Debug.LogWarning("AdsManagerSetOrientation: Ads Kit is not enabled.");
#endif
            Finish();
        }
    }
}
#endif
