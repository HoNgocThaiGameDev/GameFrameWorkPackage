# AdsManager Interstitial Load And Show By Placement

## Goal
Load an interstitial for a specific placement, show it, and branch correctly on load/show success and failure.

## Prerequisite
- Initialisation flow is complete and `AdsManagerIsInitialised` is true.
- Interstitial placement exists in Ads Kit settings.

## Actions Required
| Action | Type | Purpose |
|---|---|---|
| `AdsManagerLoadAd` | `async` | Load ad for a placement. |
| `AdsManagerGetLoadAdSuccessResult` | `result-reader` | Read load success details from current callback event data. |
| `AdsManagerGetLoadAdError` | `result-reader` | Read load error details from current callback event data. |
| `AdsManagerGetAdPlacementState` | `sync` | Inspect placement state before/after load (optional). |
| `AdsManagerShowAd` | `async` | Show ad and receive lifecycle events. |
| `AdsManagerGetShowAdSuccessResult` | `result-reader` | Read show completion result from current callback event data. |
| `AdsManagerGetShowAdError` | `result-reader` | Read show error details from current callback event data. |

## Implementation Steps
| State | Action | Inputs | Outputs | Events | Next |
|---|---|---|---|---|---|
| S1 PreCheckOptional | `AdsManagerGetAdPlacementState` | `placement` | `placementState` | - | S2 |
| S2 LoadInterstitial | `AdsManagerLoadAd` | `placement`, `adContentOptionsAsset` | - | `successEvent`, `failureEvent` | S3 on success, S4 on failure |
| S3 ReadLoadSuccess | `AdsManagerGetLoadAdSuccessResult` | - (reads current event data) | Load result variables | - | S5 |
| S4 ReadLoadFail | `AdsManagerGetLoadAdError` | - (reads current event data) | Load error variables | - | SFail |
| S5 ShowInterstitial | `AdsManagerShowAd` | `placement` | - | `adFinishEvent`, `adFailEvent` (and optional `adStartEvent`, `adClickEvent`, `adRewardEvent`) | S6 on finish, S7 on fail |
| S6 ReadShowSuccess | `AdsManagerGetShowAdSuccessResult` | - (reads current event data) | Show result variables | - | SDone |
| S7 ReadShowFail | `AdsManagerGetShowAdError` | - (reads current event data) | Show error variables | - | SFail |

## Flow Diagram
```text
S2 LoadInterstitial
  -> successEvent -> S3 ReadLoadSuccess -> S5 ShowInterstitial
    -> adFinishEvent -> S6 ReadShowSuccess -> SDone
    -> adFailEvent   -> S7 ReadShowFail    -> SFail
  -> failureEvent -> S4 ReadLoadFail -> SFail
```

## Common Issues
- Calling `AdsManagerShowAd` without a successful load path.
- Calling a getter in a state that was not entered by its matching callback event.

## Best Practices
- Keep one placement variable reused across the full load/show chain.
- Always branch `AdsManagerLoadAd` and `AdsManagerShowAd` failure events to dedicated error readers.
- Optionally inspect `AdsManagerGetAdPlacementState` before show to avoid invalid show attempts.
