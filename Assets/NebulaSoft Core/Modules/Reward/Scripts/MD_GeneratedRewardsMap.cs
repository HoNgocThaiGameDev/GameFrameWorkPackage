// Reward mappings are cached from RegisterReward attributes; no game types belong in core.
using System;
using System.Collections.Generic;

namespace NebulaSoft
{
    public static class RewardsMap
    {
        public static Dictionary<Type, Type> ViewMap { get; } = new Dictionary<Type, Type>();
        public static Dictionary<Type, Type> GetMap() => new Dictionary<Type, Type>(ViewMap);
    }
}
