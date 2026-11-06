# AdsManager PlayMaker Docs

This folder documents practical FSM usage for actions in `Scripts/Actions/AdsManager`.

## Feature Coverage
- Initialization flow: `AdsManagerInitialise`, `AdsManagerGetInitialiseSuccessResult`, `AdsManagerGetInitialiseError`, `AdsManagerIsInitialised`.
- Interstitial flow: `AdsManagerLoadAd` + load readers, `AdsManagerShowAd` + show readers.
- Rewarded flow: `AdsManagerShowAd` + `AdsManagerGetAdReward`.
- Banner hide flow: `AdsManagerHideAd`.
- Global listener flow: `AdsManagerAdEventListener` + `AdsManagerOn*EventData` via `Fsm.EventData.ObjectData` wrappers.

## Use Cases
See `use-cases/README.md`.
