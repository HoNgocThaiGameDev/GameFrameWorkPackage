#if PLAYMAKER
using UnityEngine;
using VoxelBusters.CoreLibrary;

namespace VoxelBusters.AdsKit.PlayMaker
{
    public abstract class AdsManagerEventDataWrapper : ScriptableObject
    {
        public string placement;

        public virtual string ToLogString()
        {
            return $"placement=\"{Safe(placement)}\"";
        }

        protected static string Safe(string value)
        {
            return value ?? string.Empty;
        }
    }

    public sealed class AdsManagerOnInitialisationCompleteEventDataWrapper : AdsManagerEventDataWrapper
    {
        public InitialiseResult result;

        public override string ToLogString()
        {
            if (result == null)
            {
                return "result=<null>";
            }

            int subscribedCount = result.SubscribedAdNetworks != null ? result.SubscribedAdNetworks.Length : 0;
            int invalidCount = result.InvalidNetworks != null ? result.InvalidNetworks.Length : 0;
            return $"subscribedNetworkCount={subscribedCount}, invalidNetworkCount={invalidCount}";
        }
    }

    public sealed class AdsManagerOnInitialisationFailEventDataWrapper : AdsManagerEventDataWrapper
    {
        public Error error;

        public override string ToLogString()
        {
            if (error == null)
            {
                return "error=<null>";
            }

            return $"errorDomain=\"{Safe(error.Domain)}\", errorCode={error.Code}, errorDescription=\"{Safe(error.Description)}\"";
        }
    }

    public sealed class AdsManagerOnLoadAdCompleteEventDataWrapper : AdsManagerEventDataWrapper
    {
        public LoadAdResult result;

        public override string ToLogString()
        {
            if (result == null)
            {
                return $"{base.ToLogString()}, result=<null>";
            }

            return $"{base.ToLogString()}, adUnitId=\"{Safe(result.AdUnitId)}\", adType={result.AdType}, placementState={result.PlacementState}, preferredAdProvider=\"{Safe(result.PreferredAdProvider)}\"";
        }
    }

    public sealed class AdsManagerOnLoadAdFailEventDataWrapper : AdsManagerEventDataWrapper
    {
        public Error error;

        public override string ToLogString()
        {
            if (error == null)
            {
                return $"{base.ToLogString()}, error=<null>";
            }

            return $"{base.ToLogString()}, errorDomain=\"{Safe(error.Domain)}\", errorCode={error.Code}, errorDescription=\"{Safe(error.Description)}\"";
        }
    }

    public sealed class AdsManagerOnShowAdStartEventDataWrapper : AdsManagerEventDataWrapper
    {
    }

    public sealed class AdsManagerOnShowAdClickEventDataWrapper : AdsManagerEventDataWrapper
    {
    }

    public sealed class AdsManagerOnShowAdCompleteEventDataWrapper : AdsManagerEventDataWrapper
    {
        public ShowAdResult result;

        public override string ToLogString()
        {
            if (result == null)
            {
                return $"{base.ToLogString()}, result=<null>";
            }

            string watchDuration = result.WatchDuration.HasValue ? result.WatchDuration.Value.ToString() : "null";
            return $"{base.ToLogString()}, adUnitId=\"{Safe(result.AdUnitId)}\", adType={result.AdType}, networkId=\"{Safe(result.NetworkId)}\", clicked={result.Clicked}, resultCode={result.ResultCode}, watchDuration={watchDuration}";
        }
    }

    public sealed class AdsManagerOnShowAdFailEventDataWrapper : AdsManagerEventDataWrapper
    {
        public Error error;

        public override string ToLogString()
        {
            if (error == null)
            {
                return $"{base.ToLogString()}, error=<null>";
            }

            return $"{base.ToLogString()}, errorDomain=\"{Safe(error.Domain)}\", errorCode={error.Code}, errorDescription=\"{Safe(error.Description)}\"";
        }
    }

    public sealed class AdsManagerOnAdImpressionRecordedEventDataWrapper : AdsManagerEventDataWrapper
    {
    }

    public sealed class AdsManagerOnAdPaidEventDataWrapper : AdsManagerEventDataWrapper
    {
        public AdTransaction transaction;

        public override string ToLogString()
        {
            if (transaction == null)
            {
                return $"{base.ToLogString()}, transaction=<null>";
            }

            return $"{base.ToLogString()}, adType={transaction.AdType}, networkId=\"{Safe(transaction.NetworkId)}\", currencyCode=\"{Safe(transaction.CurrencyCode)}\", value={transaction.Value}, precision={transaction.Precision}";
        }
    }

    public sealed class AdsManagerOnAdRewardEventDataWrapper : AdsManagerEventDataWrapper
    {
        public AdReward reward;

        public override string ToLogString()
        {
            if (reward == null)
            {
                return $"{base.ToLogString()}, reward=<null>";
            }

            return $"{base.ToLogString()}, adType={reward.AdType}, networkId=\"{Safe(reward.NetworkId)}\", amount={reward.Amount}, rewardType=\"{Safe(reward.Type)}\", isAmountAvailable={reward.IsRewardAmountAvailable()}";
        }
    }

    internal static class AdsManagerEventDataLogUtility
    {
        public static void LogIfEnabled(bool shouldLog, string actionName, AdsManagerEventDataWrapper wrapper)
        {
            if (!shouldLog)
            {
                return;
            }

            if (wrapper == null)
            {
                Debug.Log($"{actionName}: No event payload found in Fsm.EventData.ObjectData.");
                return;
            }

            Debug.Log($"{actionName}: {wrapper.ToLogString()}");
        }
    }
}
#endif
