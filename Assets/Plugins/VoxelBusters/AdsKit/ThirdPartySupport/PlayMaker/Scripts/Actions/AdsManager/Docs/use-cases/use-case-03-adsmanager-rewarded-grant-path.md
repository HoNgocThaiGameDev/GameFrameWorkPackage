# AdsManager Rewarded Grant Path

## Goal
Show a rewarded ad and grant the reward only when the rewarded callback is received.

## Prerequisite
- Initialisation flow is complete.
- Rewarded placement exists and is ready to show.

## Actions Required
| Action | Type | Purpose |
|---|---|---|
| `AdsManagerShowAd` | `async` | Show rewarded ad and emit lifecycle callbacks. |
| `AdsManagerGetAdReward` | `result-reader` | Read reward payload from current callback event data. |
| `AdsManagerGetShowAdError` | `result-reader` | Read failure reason on show failure path. |
| `AdsManagerGetShowAdSuccessResult` | `result-reader` | Read completion data on finish path (optional analytics). |

## Implementation Steps
| State | Action | Inputs | Outputs | Events | Next |
|---|---|---|---|---|---|
| S1 ShowRewarded | `AdsManagerShowAd` | `rewardedPlacement` | - | `adRewardEvent`, `adFinishEvent`, `adFailEvent` | S2 on reward, S3 on finish, S4 on fail |
| S2 ReadReward | `AdsManagerGetAdReward` | - (reads current event data) | Reward variables | - | SGrant |
| S3 ReadFinishOptional | `AdsManagerGetShowAdSuccessResult` | - (reads current event data) | Optional completion outputs | - | SDone |
| S4 ReadShowError | `AdsManagerGetShowAdError` | - (reads current event data) | Show error outputs | - | SFail |

## Flow Diagram
```text
S1 ShowRewarded
  -> adRewardEvent -> S2 ReadReward -> SGrant
  -> adFinishEvent -> S3 ReadFinishOptional -> SDone
  -> adFailEvent   -> S4 ReadShowError      -> SFail
```

## Common Issues
- Granting reward on `adFinishEvent` instead of `adRewardEvent`.
- Calling `AdsManagerGetAdReward` in a state that was not entered by `adRewardEvent`.
- Missing failure path for `adFailEvent`.

## Best Practices
- Trigger reward grant logic only from `adRewardEvent` + `AdsManagerGetAdReward`.
- Keep finish and fail logic separate from grant logic.
- Store reward payload fields for game economy audit/debug.
