using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Daily Reward", core: false)]
    public sealed class DailyRewardInitModule : InitModule
    {
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule), typeof(CurrencyInitModule) };
        public override void Shutdown() => DailyRewardService.Shutdown();

        public override string ModuleName => "Daily Reward";

        [SerializeField] DailyRewardDatabase database;
        public DailyRewardDatabase Database => database;

        public override void CreateComponent()
        {
            DailyRewardService.Init(database);
        }
    }
}
