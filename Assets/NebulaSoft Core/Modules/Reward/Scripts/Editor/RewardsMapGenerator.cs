using System.Reflection;
using UnityEditor;

namespace NebulaSoft
{
    // Editor prewarm only. Never generates game-specific source inside the core package.
    [InitializeOnLoad]
    public static class RewardsMapGenerator
    {
        static RewardsMapGenerator() => Generate();

        public static void Generate()
        {
            RewardsMap.ViewMap.Clear();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Reward>())
            {
                if (type.IsAbstract) continue;
                var attribute = type.GetCustomAttribute<RegisterRewardAttribute>();
                if (attribute?.ViewType != null) RewardsMap.ViewMap[type] = attribute.ViewType;
            }
        }
    }
}
