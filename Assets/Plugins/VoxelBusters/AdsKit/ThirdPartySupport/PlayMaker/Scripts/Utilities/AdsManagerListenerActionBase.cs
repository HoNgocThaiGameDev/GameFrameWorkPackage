#if PLAYMAKER
using UnityEngine;
using HutongGames.PlayMaker;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.AdsKit.PlayMaker
{
    public abstract class AdsManagerListenerActionBase : FsmStateAction, IAdLifecycleEventListener
    {
        public virtual int CallbackOrder => 10;

        public override void Reset()
        {
        }

        public override void OnEnter()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            AdsManager.RegisterListener(this);
#else
            Debug.LogWarning($"{GetType().Name}: Ads Kit is not enabled.");
            Finish();
#endif
        }

        public override void OnExit()
        {
#if ENABLE_VOXELBUSTERS_ADS_KIT
            AdsManager.UnregisterListener(this);
#endif
        }

        protected static string SafeString(string value)
        {
            return value ?? string.Empty;
        }

        protected static int SafeErrorCode(Error error)
        {
            return error != null ? error.Code : 0;
        }

        protected static string SafeErrorDescription(Error error)
        {
            return error != null ? (error.Description ?? string.Empty) : string.Empty;
        }

        protected static string SafeErrorDomain(Error error)
        {
            return error != null ? (error.Domain ?? string.Empty) : string.Empty;
        }

        public virtual void OnInitialisationComplete(InitialiseResult result)
        {
        }

        public virtual void OnInitialisationFail(Error error)
        {
        }

        public virtual void OnLoadAdComplete(string placement, LoadAdResult result)
        {
        }

        public virtual void OnLoadAdFail(string placement, Error error)
        {
        }

        public virtual void OnShowAdStart(string placement)
        {
        }

        public virtual void OnShowAdClick(string placement)
        {
        }

        public virtual void OnShowAdComplete(string placement, ShowAdResult result)
        {
        }

        public virtual void OnShowAdFail(string placement, Error error)
        {
        }

        public virtual void OnAdImpressionRecorded(string placement)
        {
        }

        public virtual void OnAdPaid(string placement, AdTransaction transaction)
        {
        }

        public virtual void OnAdReward(string placement, AdReward reward)
        {
        }
    }
}
#endif
