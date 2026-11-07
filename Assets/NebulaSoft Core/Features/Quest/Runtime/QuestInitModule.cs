using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Quest", core: false)]
    public sealed class QuestInitModule : InitModule
    {
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule), typeof(CurrencyInitModule) };
        public override void Shutdown() => QuestService.Shutdown();

        public override string ModuleName => "Quest";

        [SerializeField] QuestDatabase questDatabase;
        public QuestDatabase Database => questDatabase;

        public override void CreateComponent()
        {
            QuestService.Init(questDatabase);
        }
    }
}
