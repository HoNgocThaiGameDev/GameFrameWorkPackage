using System;

namespace NebulaSoft
{
    public static class RewardsUtils
    {
        public static Type GetViewTypeFor(Type rewardType)
        {
            RewardsMap.ViewMap.TryGetValue(rewardType, out Type viewType);
            if (viewType == null)
            {
                var attributes = rewardType.GetCustomAttributes(typeof(RegisterRewardAttribute), false);
                if (attributes.Length > 0)
                {
                    viewType = ((RegisterRewardAttribute)attributes[0]).ViewType;
                    RewardsMap.ViewMap[rewardType] = viewType;
                }
            }

            return viewType;
        }
    }
}
