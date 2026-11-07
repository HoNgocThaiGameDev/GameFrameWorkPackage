# Changelog

## 1.4.0 — 2026-11-07

- Introduce SDK-neutral Cloud Auth/document/transaction ports and isolate Firebase calls in the NebulaSoft.Firebase assembly. Share dependency initialization across Analytics/Auth/Firestore and bound Cloud startup with retry/shutdown isolation.
- Move profile, name registry, IAP history and entitlement orchestration onto neutral cloud snapshots, timestamps and error codes while preserving paths, schema, merge intent and transaction checks.
- Remove the unused native-handle compatibility API and centralize Firebase feature defines and provider creation in the adapter. Remove six standalone 13.4.0 native archives from Assets so platform dependencies come from the selected SDK installation.
- Add repeatable managed compatibility checks for multiple SDK releases, including Analytics-only and Auth/Firestore-only configurations, and document the SDK upgrade boundary and device validation limits.
- Add asset-based Analytics provider factories without changing legacy backend mappings or settings GUIDs. Configure startup queue capacity and opt into Ads, IAP and UI bridges independently.
- Discard buffered Analytics events and clear known user properties at identity boundaries; block delivery when applying user context fails to prevent attribution to the previous account.
- Expose local Analytics delivery diagnostics for overflow, identity/collection/shutdown discards, rejected events, send failures and failed initialization attempts. Document extension contracts and regression scenarios.
- Bound Analytics provider initialization with configurable timeouts, unblock cancellation even for unresponsive providers, isolate late initialization callbacks and release owned services through InitModule shutdown.
- Clear Remote Config snapshots between bootstrap sessions and on shutdown. Reject malformed, duplicate or negative currency defaults before bundled-key fallback.
- Expand Analytics and Remote Config integration documentation with lifecycle, provider ownership, queue behavior, typed schemas, fallback policy and regression coverage.

- Add versioned native JSON saves with legacy binary loading, per-model migration steps, constructor defaults, unknown-field preservation, backups and blocked writes after unrecoverable corruption.
- Clean up the Save Migration test fixture's registry entries so repeated EditMode runs in the same Unity session can register them again.
- Add opt-in extensible cloud models to the existing Firebase progress flow, transactional version checks, merge writes and Local/Cloud comparison.
- Support data-configured currencies, checked spending and atomic currency batches. Defer currency observers until IAP checkpoints are persisted.
- Fix per-page UI close callbacks, stable skin selection IDs and dirty tracking, pool capacity/lifetime, audio throttling/voice limits and per-key remote-config fallback.
- Add startup dependency validation, bounded SDK tasks, failure reporting, retry checkpoints and shutdown hooks.
- Move retention runtime features into core Features with preserved script GUIDs. Add shared clocks and isolate Save/Pool runtime assemblies from composition/editor integrations.
- Normalize module Runtime/Editor folders, collect framework-owned graphics under core, and group reference-game feature presentation under Game/Features and Game/UI. Preserve moved asset GUIDs and existing resource keys.
- Isolate Tween runtime/editor assemblies, keep its InitModule in composition, update path-based tools/tests, and consolidate documentation generators in tools/Documentation.


Validation on Unity 6000.6.8f1 passed all 244 EditMode tests in two consecutive runs in the same Editor session, including 39 Analytics tests and 19 Cloud tests. All 10 standalone audit assertions passed. Firebase compatibility checks passed all 18 managed compilations across SDK 13.4.0 and 14.1.0 and three feature configurations. Game and Editor compiled against the adapter with zero Firebase SDK references. Device builds, profiling and Firebase sandbox verification remain with the project owner.

## 1.3.0 — 2026-11-07

- Sync the template with Unity 6000.6.8f1 and the current project assets. Update the debug console package to v1.9.0 and adapt editor, URP, uGUI, UIEffect and UniTask calls to Unity 6.6.
- Store named save entries by string ID with lazy migration for legacy hash entries. Persist IAP reward checkpoints so a redelivered transaction resumes at the failed reward and publishes completion once after saving.
- Release the IAP purchase lock on deferred orders, retry analytics initialization on resume, and forward AdsKit impression callbacks to the framework analytics bridge.
- Fix player script imports, sound gain updates, popup wait completion, remote config parsing, Daily Reward repeat claims and redundant writes, and UMP consent refresh and age tagging.
- Move game-specific IAP and rewarded ad behavior to the game layer, resolve reward views from their registered types, and remove two stale asset references.

Validation on Unity 6000.6.8f1 covered 135 EditMode tests and Android player script compilation. The standalone audit passed all 10 assertions.
