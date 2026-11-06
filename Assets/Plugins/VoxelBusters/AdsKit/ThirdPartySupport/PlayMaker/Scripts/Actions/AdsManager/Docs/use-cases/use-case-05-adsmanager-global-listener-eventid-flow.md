# AdsManager Global Listener Wrapper Flow

## Goal
Use one global listener FSM to receive ad callbacks, then read payload from `Fsm.EventData.ObjectData` with the matching EventData action.

## Prerequisite
- `AdsManagerAdEventListener` state is active while callbacks are expected.
- EventData action is called immediately after its callback event.

## Actions Required
| Action | Type | Purpose |
|---|---|---|
| `AdsManagerAdEventListener` | `listener` | Registers ad callback listener and emits PlayMaker events with wrapper payload in `Fsm.EventData.ObjectData`. |
| `AdsManagerOnLoadAdCompleteEventData` | `result-reader` | Read load success payload from current event data. |
| `AdsManagerOnLoadAdFailEventData` | `result-reader` | Read load failure payload from current event data. |
| `AdsManagerOnShowAdStartEventData` | `result-reader` | Read show start payload from current event data. |
| `AdsManagerOnShowAdClickEventData` | `result-reader` | Read show click payload from current event data. |
| `AdsManagerOnShowAdCompleteEventData` | `result-reader` | Read show complete payload from current event data. |
| `AdsManagerOnShowAdFailEventData` | `result-reader` | Read show fail payload from current event data. |
| `AdsManagerOnAdImpressionRecordedEventData` | `result-reader` | Read impression payload from current event data. |
| `AdsManagerOnAdPaidEventData` | `result-reader` | Read paid payload from current event data. |
| `AdsManagerOnAdRewardEventData` | `result-reader` | Read reward payload from current event data. |

## Implementation Steps
| State | Action | Inputs | Outputs | Events | Next |
|---|---|---|---|---|---|
| S1 Listen | `AdsManagerAdEventListener` | `placementFilter` (optional) | - | `onLoadAdComplete`, `onLoadAdFail`, `onShowAdStart`, `onShowAdClick`, `onShowAdComplete`, `onShowAdFail`, `onAdImpressionRecorded`, `onAdPaid`, `onAdReward` | Matching EventData state |
| S2 LoadCompleteData | `AdsManagerOnLoadAdCompleteEventData` | - | placement + load outputs | - | S1 |
| S3 LoadFailData | `AdsManagerOnLoadAdFailEventData` | - | placement + error outputs | - | S1 |
| S4 ShowStartData | `AdsManagerOnShowAdStartEventData` | - | `placement` | - | S1 |
| S5 ShowClickData | `AdsManagerOnShowAdClickEventData` | - | `placement` | - | S1 |
| S6 ShowCompleteData | `AdsManagerOnShowAdCompleteEventData` | - | placement + show outputs | - | S1 |
| S7 ShowFailData | `AdsManagerOnShowAdFailEventData` | - | placement + error outputs | - | S1 |
| S8 ImpressionData | `AdsManagerOnAdImpressionRecordedEventData` | - | `placement` | - | S1 |
| S9 PaidData | `AdsManagerOnAdPaidEventData` | - | placement + paid outputs | - | S1 |
| S10 RewardData | `AdsManagerOnAdRewardEventData` | - | placement + reward outputs | - | S1 |

## Flow Diagram
```text
S1 Listen
  -> onLoadAdComplete     -> S2 LoadCompleteData -> S1
  -> onLoadAdFail         -> S3 LoadFailData     -> S1
  -> onShowAdStart        -> S4 ShowStartData    -> S1
  -> onShowAdClick        -> S5 ShowClickData    -> S1
  -> onShowAdComplete     -> S6 ShowCompleteData -> S1
  -> onShowAdFail         -> S7 ShowFailData     -> S1
  -> onAdImpressionRecorded -> S8 ImpressionData -> S1
  -> onAdPaid             -> S9 PaidData         -> S1
  -> onAdReward           -> S10 RewardData      -> S1
```

## Common Issues
- Calling EventData action in a state that was not entered by listener callback event.
- Using a callback event without a transition in listener FSM (wrapper payload is not created).
- Using too broad listener scope without `placementFilter` in large projects.

## Best Practices
- Keep listener in a dedicated always-on FSM for centralized callback routing.
- Route each callback event directly to its matching EventData state.
- Treat wrapper payload as single-use; EventData actions destroy wrapper on read.
- Process callback states immediately and branch to business logic in the same path.
