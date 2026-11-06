# Changelog

## 1.3.0 — 2026-11-07

- Sync the template with Unity 6000.6.8f1 and the current project assets. Update the debug console package to v1.9.0 and adapt editor, URP, uGUI, UIEffect and UniTask calls to Unity 6.6.
- Store named save entries by string ID with lazy migration for legacy hash entries. Persist IAP reward checkpoints so a redelivered transaction resumes at the failed reward and publishes completion once after saving.
- Release the IAP purchase lock on deferred orders, retry analytics initialization on resume, and forward AdsKit impression callbacks to the framework analytics bridge.
- Fix player script imports, sound gain updates, popup wait completion, remote config parsing, Daily Reward repeat claims and redundant writes, and UMP consent refresh and age tagging.
- Move game-specific IAP and rewarded ad behavior to the game layer, resolve reward views from their registered types, and remove two stale asset references.

Validation on Unity 6000.6.8f1 covered 135 EditMode tests and Android player script compilation. The standalone audit passed all 10 assertions.
