# AdsManager Initialise And Readiness Gate

## Goal
Initialise Ads Kit once, handle success/failure, and gate ad flows until the SDK is ready.

## Prerequisite
- Ads Kit plugin is enabled (`ENABLE_VOXELBUSTERS_ADS_KIT`).
- At least one consent form provider is available for `AdsManagerInitialise`.

## Actions Required
| Action | Type | Purpose |
|---|---|---|
| `AdsManagerSetPrivacyConfiguration` | `sync` | Apply privacy configuration before initialise (optional). |
| `AdsManagerSetUser` | `sync` | Apply user metadata before initialise (optional). |
| `AdsManagerSetPlacementMeta` | `sync` | Set placement metadata before initialise (optional). |
| `AdsManagerIsConsentFormProviderAvailable` | `sync` | Check consent provider availability (optional gate). |
| `AdsManagerInitialise` | `async` | Start initialise flow and emit success/failure event. |
| `AdsManagerGetInitialiseSuccessResult` | `result-reader` | Read subscribed/invalid network data after success. |
| `AdsManagerGetInitialiseError` | `result-reader` | Read initialise error after failure. |
| `AdsManagerIsInitialised` | `sync` | Final readiness check for downstream FSMs. |

## Implementation Steps
| State | Action | Inputs | Outputs | Events | Next |
|---|---|---|---|---|---|
| S1 ConfigureOptional | `AdsManagerSetPrivacyConfiguration`, `AdsManagerSetUser`, `AdsManagerSetPlacementMeta` | Action-specific input vars | Optional `errorMessage` for setter actions | Success/failure on setter actions (if configured) | S2 |
| S2 ConsentGate | `AdsManagerIsConsentFormProviderAvailable` | - | `isConsentProviderAvailable` | - | S3 when true, SFail when false |
| S3 Initialise | `AdsManagerInitialise` | `resetConsentInformation` | - | `successEvent`, `failureEvent` | S4 on success, S5 on failure |
| S4 ReadInitSuccess | `AdsManagerGetInitialiseSuccessResult` | - | `subscribedNetworkCount`, `subscribedNetworkIds`, `invalidNetworkIds` | - | S6 |
| S5 ReadInitError | `AdsManagerGetInitialiseError` | - | `initErrorDomain`, `initErrorCode`, `initErrorDescription` | - | SFail |
| S6 ReadyCheck | `AdsManagerIsInitialised` | - | `isInitialised` | - | SReady when true, SFail when false |

## Flow Diagram
```text
S1 ConfigureOptional
  -> S2 ConsentGate
    -> (isConsentProviderAvailable = true) S3 Initialise
      -> successEvent -> S4 ReadInitSuccess -> S6 ReadyCheck -> SReady
      -> failureEvent -> S5 ReadInitError -> SFail
    -> (isConsentProviderAvailable = false) SFail
```

## Common Issues
- `AdsManagerInitialise` fails when no consent form provider is available.
- Calling ad load/show states before readiness check.
- Ignoring setter validation failures from `AdsManagerSetUser` or `AdsManagerSetPlacementMeta`.

## Best Practices
- Run this flow once at app start and branch all ad FSM entry points through the ready gate.
- Always process both `successEvent` and `failureEvent` from `AdsManagerInitialise`.
- Store initialise error fields for telemetry/debug UI.
