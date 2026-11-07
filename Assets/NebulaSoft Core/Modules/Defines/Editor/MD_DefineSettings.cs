namespace NebulaSoft
{
    public static class DefineSettings
    {
        public static readonly RegisteredDefine[] STATIC_REGISTERED_DEFINES = new RegisteredDefine[]
        {
            // System
            new RegisteredDefine("MODULE_INPUT_SYSTEM", "UnityEngine.InputSystem.InputManager", "com.unity.inputsystem/package.json"),
            new RegisteredDefine("MODULE_TMP", "TMPro.TMP_Text", "com.unity.textmeshpro/package.json"),
            new RegisteredDefine("MODULE_CINEMACHINE", "Cinemachine.CinemachineBrain", "com.unity.cinemachine/package.json"),
            new RegisteredDefine("MODULE_IDFA", "Unity.Advertisement.IosSupport.ATTrackingStatusBinding", "com.unity.ads.ios-support/package.json"),
            new RegisteredDefine("MODULE_IAP", "UnityEngine.Purchasing.UnityPurchasing", "com.unity.purchasing/package.json"),

            // Core
            new RegisteredDefine("MODULE_MONETIZATION", "NebulaSoft.Monetization", "Modules/Monetization/Runtime/MonetizationSDKBehavior.cs"),
            new RegisteredDefine("MODULE_HAPTIC", "NebulaSoft.Haptic", "Modules/Haptic/Runtime/Haptic.cs"),
        };
    }
}
