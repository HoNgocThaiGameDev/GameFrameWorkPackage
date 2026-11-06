# Ads module

Module API version: **1.0.0**. Implemented in the original GameFrameWorkPackage, using its existing InitModule system without a DI container. **Ads Kit 2.3.0** and **Google Mobile Ads 11.0.0** are installed, with **EDM4U 1.2.187** and the bundled AdMob adapter. The selected provider is Ads Kit in forced test mode, using Google's test app/ad-unit IDs.

## Boundaries and lifecycle

```text
Project Init Settings → AdsInitModule → selects AdsModuleSettings
Save module → SaveController loads the existing save
SDKInitializer → existing consent tasks finish → MonetizationSDKBehavior
AdsManager compatibility facade → application-owned AdsModule
AdsModule → AdsService → IAdsProvider → Dummy / optional Ads Kit
          → AdsSaveBridge → existing SaveController and AdSave
SDKs prefab → AdsGameBridge → game UI, analytics, Firebase entitlement sync
```

`Contracts` is a separate assembly with no Unity or SDK reference. `Runtime/AdsService` depends only on Contracts, owns request state and policies, and receives its clock/provider through its constructor. `Configuration` translates Unity ScriptableObjects into pure placement definitions. Unity lifetime and provider factories are composed at the application boundary; no VContainer installation is required.

`AdsInitModule` registers settings in Project Init Settings. Native initialization still starts from the existing monetization behavior after `SDKInitializer` receives consent. `AdsModule` belongs to the persistent Initializer object, ticks callbacks on Unity's main thread, and disposes subscriptions/native views when destroyed. Construct the service and call its API and Tick on the main thread. Provider callbacks from worker threads are queued until Tick.

## Configuration

Assets are under `Assets/Project Files/Data/Ads`:

| Asset | Role | Current setting |
| --- | --- | --- |
| AdsModuleSettings | Provider, catalog, defaults | Enabled, Ads Kit |
| AdsCatalog | Placement list | Three placements |
| ADP_MenuBanner | `menu_banner`, Banner | Enabled |
| ADP_LevelTransition | `level_transition`, Interstitial | Disabled |
| ADP_Rewarded | `rewarded`, Rewarded | Disabled |
| DummyAdsProvider | Editor UI provider configuration | Bottom banner |
| AdsKitProvider | Framework-to-SDK placement mapping | All three placements |
| AdsKitAdMobConsent | Consent provider factory | Editor simulation / native AdMob UMP |

The initial format selection preserves the existing framework configuration. To exercise fullscreen ads, enable the corresponding placement. Each placement has a stable ID, format, enabled flag, preload flag, and cooldown. Reference placement assets from gameplay/UI instead of duplicating SDK IDs in scripts. Add assets through **Create → NebulaSoft → Ads** and include them in the catalog. Validation rejects missing/duplicate IDs, wrong default formats, and invalid provider mappings.

Vendor settings live at `Assets/Resources/AdsKitSettings.asset`, banner options at `Assets/Resources/DefaultBannerAdOptions.asset`, and Google settings at `Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset`. Open them through **Window → Voxel Busters → Ads Kit → Open Settings**. Auto Init On Start is off; the module owns initialization. SDK auto-load is off; enabled module placements control preload. Dummy remains available by selecting its provider asset.

For Editor ad simulation, use an Android or iOS build target: Ads Kit selects platform-specific unit IDs from the active target. Android is configured in this project. Force Test Mode stays on until a new game's own app/ad-unit IDs are supplied and native behavior is verified. Both platform app IDs and all three unit IDs currently belong to Google's test inventory.

Project Init Settings and the legacy Monetization Settings Ads subasset point to the same AdsModuleSettings. Legacy banner/interstitial/rewarded provider dropdowns are ignored when the module settings are linked. Existing compatibility loading presentation, first-launch delays, remote flags, and reward icon stay in AdsSettings.

## Gameplay API

Pass `IAdsService` into a game controller/presenter from the composition root (`AdsManager.Service` is the existing framework access point). Controllers need no vendor types or new singleton:

```csharp
using NebulaSoft.Ads;

public sealed class ContinuePresenter
{
    private readonly IAdsService ads;
    private readonly AdPlacement continuePlacement;

    public ContinuePresenter(IAdsService ads, AdPlacement continuePlacement)
    {
        this.ads = ads;
        this.continuePlacement = continuePlacement;
    }

    public async System.Threading.Tasks.Task<bool> TryContinueAsync()
    {
        var result = await ads.ShowAsync(continuePlacement);
        return result.RewardGranted;
    }
}
```

Only grant the gameplay reward when `RewardGranted` is true. Closing a rewarded ad without a reward never succeeds. Reward events and completion callbacks are deduplicated by request ID; old content callbacks cannot reward a newer request. Fullscreen formats share one lock. `Busy`, `NotReady`, `Disabled`, `Cancelled`, `Failed`, and `Disposed` are explicit results, not successful ad completions. Cancellation stops the caller's wait, but the lock remains until the native ad closes; cancellation cannot dismiss the SDK's ad.

Cooldown/preload in AdPlacement apply to the service API. Legacy AdsManager calls also retain their existing first-launch/interstitial delays and remote flags. A new game's placement eligibility rules belong in its presenter or policy layer rather than in a provider.

Existing `AdsManager.ShowRewardBasedVideo`, interstitial calls, events and banner methods delegate to the service. `GetAdProvider` is obsolete and returns null; direct legacy native-handler access has been retired. Old AppLovin/LevelPlay/Dummy handlers were removed. Provider-specific diagnostics should use SDK tooling, outside gameplay.

## Save, UI and banner layout

`AdsSaveBridge` uses the existing `AdSave` key **advertisement_forced_ad**, preserving the permanent `-1` marker and timed disable timestamp. It marks the framework save dirty rather than creating a second save system. Permanent No Ads disables banner/interstitial; rewarded ads remain available. A temporary No Ads period expiring re-enables the banner through the compatibility bridge. IAP rewards and cloud entitlement sync continue calling existing AdsManager entitlement methods.

`AdsGameBridge` on the SDKs prefab owns SystemMessage loading/error UI, analytics, Firebase pending entitlement application and the No Ads offer popup. Providers emit lifecycle/geometry events and never reference those game screens. Connect a game's No Ads button to `AdsGameBridge.OpenNoAdsOffer` if needed.

`BannerInfo` reports actual display-pixel width/height and bottom inset. Show/hide/No Ads update `BannerChanged`; duplicate show requests do not create another view. Existing banner spacers receive the compatibility `BannerLayoutChanged` event. The Dummy view measures its RectTransform. The Ads Kit adapter reads the public network ad-view proxy size; it currently supports bottom banners with zero inset. If a network does not expose a positive size, layout stays unreserved. Other positions/native insets need an adapter geometry extension and device verification; do not substitute a guessed banner size.

## Optional Ads Kit adapter and upgrades

SDK source is isolated in `Providers/AdsKit`; the core assemblies cannot reference it. Missing SDK configuration fails validation explicitly. It does not silently fall back to Dummy.

When separately adding Ads Kit:

1. Use Ads Kit's own settings for app IDs, network credentials, ad-unit IDs, network ordering and placements. Turn **Auto Init On Start off** so the framework owns consent/init timing.
2. Create **Ads Kit Provider** configuration and map each enabled framework placement asset to a vendor placement name. Validation checks existence and matching ad format.
3. Select a consent factory. **Ads Kit AdMob Consent** uses the vendor AdMob consent implementation on devices and requires that integration. Its always-authorized provider is restricted to the Unity Editor. For another network, implement `AdsKitConsentProviderConfig`; consent configuration is confined to the adapter layer.
4. Assign the provider asset to AdsModuleSettings. The framework's Define system detects `MODULE_ADSKIT`; manual SDK imports outside the normal plugin location may require setting it explicitly.
5. The adapter uses the modern **Ads Kit 2.3.0** placement states. For the older API used by PicturePuzzle (Ready/Waiting), add **NEBULASOFT_ADSKIT_LEGACY_STATES** to scripting defines. Remove that compatibility symbol when upgrading to the modern API.
6. Verify native consent, all three formats, reward/cancel/failure callbacks, orientation, banner geometry and No Ads on devices.

Updating the SDK should require changing this adapter/configuration translation only. Gameplay references `IAdsService` and AdPlacement assets; network selection comes from initialized SDK adapters, with no hardcoded network-name switch. Vendor files are not patched. To use another SDK, implement `IAdsProvider` and an `AdsProviderConfig` factory, then select the new configuration asset. Preserve request IDs when emitting reward/close events and release native subscriptions in Dispose.

## Verification

Unity 6000.3.12f1, Android build target: **77/77** selected cases passed after SDK installation, covering AdsModuleTests, AdsDummyPlayModeTests, AdsKitPlayModeTests, IAPModuleTests, SaveModuleTests and FrameworkSceneSmokeTests. Tests are under `Assets/Tests/Editor`.

The Ads Kit integration test enters Play Mode, initializes the installed SDK with Editor consent simulation, loads banner/interstitial/rewarded through the framework provider, shows the banner and verifies No Ads hides it while retaining rewarded availability. Its temporary format definitions exercise disabled fullscreen placements without modifying the template assets. It requests Player Loop updates so SDK callback dispatch also runs when the Editor is unfocused. Editor adaptive-banner simulation may return no positive pixel size; the service then reserves no invented height. The Dummy UI test separately verifies measured banner geometry, reward/skip, interstitial close and view disposal.

The adapter also compiled separately against the modern SDK source API and the older PicturePuzzle runtime API. The installed SDK is from the official [Ads Kit 2.3.0 release](https://github.com/voxelbusters/ads-kit/releases/tag/2.3.0), with CoreLibrary updated from that package. The original CoreLibrary was preserved under `Library/AdsKitInstallBackup/Before2_3_0`. The vendor package.json still reports 2.2.0; its runtime `AdsKitSettings.Version` reports 2.3.0. No vendor source was patched.

Google Mobile Ads is from its official 11.0.0 package; EDM4U is supplied once by Ads Kit. Android Gradle dependencies were refreshed for the installed AdMob/UMP versions. Native network delivery, consent UI and device orientation/layout remain device checks; Editor simulation does not establish those behaviors.
