#pragma warning disable 0649

using UnityEngine;

namespace NebulaSoft
{
    [RegisterModule("Tween", core: true, order: 800)]
    public class TweenInitModule : InitModule
    {
        public override string ModuleName => "Tween";

        [SerializeField] CustomEasingFunction[] customEasingFunctions;

        [Space]
        [SerializeField] int tweensUpdateCount = 300;
        [SerializeField] int tweensFixedUpdateCount = 30;
        [SerializeField] int tweensLateUpdateCount = 0;

        [Space]
        [SerializeField] bool verboseLogging;

        public override void CreateComponent()
        {
            Tween tween = Initializer.GameObject.AddComponent<Tween>();
            tween.Init(tweensUpdateCount, tweensFixedUpdateCount, tweensLateUpdateCount, verboseLogging);

            Ease.Init(customEasingFunctions);
        }
    }
}
