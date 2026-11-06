#if PLAYMAKER
using HutongGames.PlayMaker;
using TooltipAttribute = HutongGames.PlayMaker.TooltipAttribute;

namespace VoxelBusters.AdsKit.PlayMaker
{
    [ActionCategory("VoxelBusters - AdsKit - Ads Manager (Load, show, hide ads)")]
    [Tooltip("Reads ShowAd error data from current PlayMaker event payload (Fsm.EventData).")]
    public class AdsManagerGetShowAdError : FsmStateAction
    {
        [ActionSection("Results")]
        [UIHint(UIHint.Variable)]
        public FsmString errorDomain;

        [UIHint(UIHint.Variable)]
        public FsmInt errorCode;

        [UIHint(UIHint.Variable)]
        public FsmString errorDescription;

        public override void Reset()
        {
            errorDomain = new FsmString { UseVariable = true };
            errorCode = new FsmInt { UseVariable = true };
            errorDescription = new FsmString { UseVariable = true };
        }

        public override void OnEnter()
        {
            var wrapper = Fsm.EventData.ObjectData as AdsManagerOnShowAdFailEventDataWrapper;
            var error = wrapper != null ? wrapper.error : null;

            if (!errorDomain.IsNone)
            {
                errorDomain.Value = error != null ? (error.Domain ?? string.Empty) : string.Empty;
            }

            if (!errorCode.IsNone)
            {
                errorCode.Value = error != null ? error.Code : 0;
            }

            if (!errorDescription.IsNone)
            {
                errorDescription.Value = error != null ? (error.Description ?? string.Empty) : string.Empty;
            }

            Finish();
        }
    }
}
#endif
