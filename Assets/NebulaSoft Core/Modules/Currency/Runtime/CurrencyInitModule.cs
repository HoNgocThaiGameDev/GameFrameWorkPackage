using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Currencies", false)]
    public class CurrencyInitModule : InitModule
    {
        public override System.Type[] Dependencies => new[] { typeof(SaveInitModule) };
        public override void Shutdown() => CurrencyController.Shutdown();
        public override string ModuleName => "Currencies";

        [SerializeField] CurrencyDatabase currenciesDatabase;
        public CurrencyDatabase Database => currenciesDatabase;

        public override void CreateComponent()
        {
            CurrencyController.Init(currenciesDatabase);
        }
    }
}
