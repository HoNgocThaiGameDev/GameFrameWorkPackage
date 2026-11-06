# AdsManager Banner Hide Or Destroy

## Goal
Hide or destroy a banner instance for a specific placement.

## Prerequisite
- Banner placement exists.
- Banner has been loaded/shown earlier in your flow.

## Actions Required
| Action | Type | Purpose |
|---|---|---|
| `AdsManagerHideAd` | `sync` | Hide banner for placement; optionally destroy banner instance. |
| `AdsManagerShowAd` | `async` | Optional upstream action if this FSM also shows banner before hide. |

## Implementation Steps
| State | Action | Inputs | Outputs | Events | Next |
|---|---|---|---|---|---|
| S1 HideBanner | `AdsManagerHideAd` | `bannerPlacement`, `destroy` | - | `successEvent`, `failureEvent` | SDone on success, SFail on failure |

## Flow Diagram
```text
S1 HideBanner
  -> successEvent -> SDone
  -> failureEvent -> SFail
```

## Common Issues
- Calling `AdsManagerHideAd` with empty placement.
- Expecting hide/destroy behavior for non-banner placements.

## Best Practices
- Use `destroy=false` for temporary hide and `destroy=true` only when you need instance teardown.
- Keep banner placement IDs dedicated and consistent across show/hide flows.
