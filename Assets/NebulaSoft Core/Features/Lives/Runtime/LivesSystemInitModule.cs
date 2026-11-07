using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Lives System", core: false)]
    public class LivesSystemInitModule : InitModule
    {
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule), typeof(CurrencyInitModule), typeof(TweenInitModule) };
        public override void Shutdown() => LivesSystem.Shutdown();

        public override string ModuleName => "Lives System";

        [SerializeField] LivesData livesData;

        public override void CreateComponent()
        {
            if (livesData == null)
            {
                Debug.LogError("LivesData is not assigned in Project Init Settings", this);

                return;
            }

            LivesSystem.Init(livesData);
        }
    }
}