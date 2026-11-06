# IAP module

The IAP module configures the shared `IAPManager`. It uses the existing `IAPRewardsHolder`, `IAPButton`, reward types, and store adapters.

## Configure once

1. Open **Project Init Settings** and add **Monetization/IAP** after Save. The reference project already includes this module.
2. Assign **Assets/Project Files/Data/IAP Settings.asset**. To create another catalog use **Create > NebulaSoft > IAP Settings**.
3. Enable IAP and configure products in the catalog. The module prepares configuration during module initialization; the existing monetization SDK behavior connects after consent.

A registered module is authoritative, including when disabled or missing its settings. Projects without this module retain the legacy Monetization Settings path. The editor reports conflicting catalogs rather than selecting an arbitrary asset.

## Add a package without code

1. In IAP Settings press **+**. Enter a unique stable key, for example `coins_2500`.
2. Enter the matching Android/iOS store IDs. Select Consumable for repeatable currency/booster packs; select NonConsumable for permanent unlocks.
3. Assign a RewardsSet and configure the rewards using the existing reward editor. Preview USD price is for editor presentation, not the store charge.
4. Duplicate an existing offer prefab or use your own layout with IAPRewardsHolder and IAPButton.
5. On IAPRewardsHolder choose **Product** from the catalog dropdown. Assign its **Purchase Button**. Reward data comes from the selected catalog item; configure the existing Reward Views for your layout.
6. Assign Button, Price Text and any optional image/loading references on IAPButton. No manual OnClick binding or package-specific script is required.

Drag catalog rows to reorder. **Duplicate selected product** keeps its reward/type/preview price and clears its identity; enter a new key and store IDs before use. Removing a catalog entry does not delete its RewardsSet. Missing product references are reported on holders.

Keys are case-sensitive. Do not rename a published key: it is a save identity. Changing display name or catalog order is safe. Product assets and settings contain configuration only; runtime purchase counts are stored separately.

## Runtime API

```csharp
IAPManager.BuyProduct("coins_2500");
var item = IAPManager.GetIAPItemByKey("coins_2500");
var price = IAPManager.GetProductLocalPriceString("coins_2500");
bool owned = IAPManager.IsPurchased("premium_unlock");
IAPManager.RestorePurchases();
```

`GetIAPItem(string)` still resolves a **store ID**, not a catalog key. Existing enum overloads and events remain available for legacy-mapped products. New integrations should use `ProductPurchaseCompleted`, `ProductPurchaseFailed` and `StateChanged`; subscribe/unsubscribe with the owning component lifecycle.

The manager locks purchase requests while a purchase or restore is active. All buttons read the same state. Unknown products, unready stores and duplicate attempts return failure events. The manager grants rewards independently of offer lifetime. Disabling or destroying an offer does not cancel a store transaction.

## Compatibility and transaction behavior

The six migrated products preserve their enum values, original `iap_{EnumName}` saves, store IDs, reward references and prices. New products use `iap_{key}` saves. The module and Monetization Settings reference the same standalone catalog. The old embedded settings subasset is retained for external serialized references.

Adapters use persistent transaction deduplication. Mobile/editor purchase saves use synchronous atomic file replacement; a failed save keeps the transaction unconfirmed. Reward/presentation observers are isolated so a broken UI callback cannot prevent transaction persistence. Unknown catalog products are not acknowledged. The acknowledgement flow follows [Unity IAP purchase processing](https://docs.unity.com/en-us/iap/purchases). Restore does not re-grant consumables; all non-consumable products can restore their RestoreReward behavior.

Custom Reward implementations must complete state changes synchronously and avoid partially mutating state before throwing. A multi-reward set that throws after earlier rewards changed state is not automatically rolled back; keep reward operations reliable or idempotent. Arbitrary external side effects are not an atomic transaction with the local save. Local purchase records do not restore consumed currency after reinstall or establish server-authoritative receipt validation.

## Testing and optional SDK

Without Unity Purchasing the framework selects DummyIAPWrapper. With IAP 5, Use Fake Store also selects Dummy; the legacy Fake Store UI mode only applies to the IAP 4 adapter. It simulates completion and does not contact a store. Test data should use separate local saves.

Unity Purchasing remains optional. Existing feature symbols select the IAP 4/5 adapters; installing/configuring the SDK and testing Play/App Store purchases are separate steps. Subscription query support is retained; subscription management UI is outside this module update.

Run **IAPModuleTests** and the existing EditMode suite in Unity Test Runner. Test saves are isolated under a temporary directory.

## Verification — 2026-10-08

Unity 6000.3.12f1 compiled the current project without compiler errors. The final targeted EditMode run passed **29/29** cases: 25 IAP cases and 4 existing scene smoke cases. Coverage includes catalog add/duplicate/reorder/reload, legacy save identities, invalid configuration, repeated transactions, save/reward failure retry, isolated presentation exceptions, shared buttons, pre-initialization UI, destruction during purchase, initialization guarding and restore cleanup.

After removing the optional design bridge and its tests, the full EditMode run completed **61** cases: **57 passed, 4 failed**. All IAP and scene smoke cases passed. The remaining failures are in `QuestRotationTests`: daily/weekly quest slot counts, online-time reward type, and the quest button sprite expectation. Those tests and their data were not changed.

These are automated Editor checks, including scene structure checks, not a device gameplay pass. Real purchase/cancel/deferred/restore callbacks and optional IAP 4/5 compilation require the corresponding SDK and store sandbox; no SDK was installed or live purchase made.
