# Private-Online Validation Recipes

Canonical future validation record selected by milestone 8.29. Return to [../TESTING.md](../TESTING.md). Every Unity invocation uses the parameterless launcher and fresh-artifact rules in [POLICY.md](POLICY.md); the filters below become executable only when their owning milestone adds the named fixture. A milestone may add a narrower failing regression, but may not replace its listed contract checks or applicable tier.

## Shared command forms

Unity-free focused fixtures use:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore --filter "FullyQualifiedName~<fixture>"
```

Unity focused fixtures use `tmp/unity-validation-request.json` with the exact `platform` and ordered `filters` selected below, milestone-specific result/log paths beneath `tmp`, followed by the fixed launcher. Compatible filters on one platform and batch-mode boundary use one grouped request; Edit/Play, batch/non-batch, and different process-global setup remain separate serial invocations:

```powershell
& 'C:\Projects\Guards Digital\Guards 0.1\Tools\Invoke-UnityValidation.ps1'
```

The exact local multi-process runner selected for the disposable player build is `Tools/Invoke-PrivateOnlineRehearsal.ps1`. Milestone 8.31 creates it with a loopback smoke mode; 8.35c extends the same entry point to `-Topology SixClient -FaultPlan Full`. It must start hidden processes, use only workspace outputs, record exact build/protocol/schema/map/content identities, bound execution, stop only its owned PIDs, and fail on missing/non-green convergence, privacy, or process-exit evidence.

## Milestone matrix

| Milestone | Exact focused validation | Tier and directly affected evidence |
| --- | --- | --- |
| 8.30 | Harness filters `TransportNeutralMatchSessionTests` and `OnlineArchitectureConformanceTests` | T2 because the shared session boundary changes; directly affected `MatchSessionTests`, deterministic replay, restore, Undo, and official complete-loop fixtures. Unity Edit Mode runs the same filters if their production assemblies remain Unity-facing. |
| 8.30a | Harness filters `OwnerScopedProjectionTests`, `SeatAuthorizedSubmissionTests`, and `PendingDecisionTransportContractTests` | T2; all ten pending families, forged/cross-seat identity, duplicate/replay/stale/version cases, snapshot restore and session replacement. No Play Mode because no Unity surface changes. |
| 8.30b | Edit Mode filter `SeatScopedMatchHudTests`; Play Mode filter `SeatScopedMatchHudPlayModeTests` | T2 plus targeted non-batch Game View inspection at 2560x1440 and the supported short desktop viewport. Existing HUD input, pending-decision, card-footprint, rebuild, restore and resource-lifetime regressions are directly affected. |
| 8.30c | Harness filter `LocalSixSeatPlayerExperienceTests`; Edit Mode filters `LocalSixSeatPlayerExperienceTests`, `SeatScopedMatchHudTests`, `StatusDerivationConformanceTests`, `DirectorHandPresentationTests`, `MatchFeaturePresenterTests`, and `MatchHudPresentationTests`; Play Mode filter `SeatScopedMatchHudPlayModeTests`, plus targeted non-batch `SeatScene_RecomposesAllSixPerspectivesWithoutLeakingStateHandlersOrResources` | **T3 local privacy/player gate:** three complete records run only through all six seat clients and cover all ten pending families, both team-owned sides, every victory cause, privacy, cross-seat/stale rejection, snapshot replacement and ended state. Recompose every seat across 2560x1440, 2048x1152 and 2560x540 without leaked selection, handlers or resources, inspect those three captures, then run both complete unfiltered Unity suites. This is the reusable pre-network regression gate and proves no network claim. |
| 8.31 | Edit Mode filters `PrivateOnlinePackageConformanceTests` and `RelaySessionAdapterTests`; runner `Tools/Invoke-PrivateOnlineRehearsal.ps1 -Topology LoopbackSmoke` | T3 package/project-setting trigger: package resolution/import, protocol-1 byte loopback, anonymous-auth/session/join-code bounded service probe when credentials/service are available, disposable Win64 build-and-launch smoke, both complete suites. An unavailable service is not a pass; record an outage and resume the same milestone. |
| 8.32 | Harness filters `NetworkSubmissionCodecTests` and `NetworkAuthorizationBoundaryTests`; Edit Mode filter `NetworkSessionAdapterTests` | T2 minimum, promoted to T3 if package/settings/build surfaces change. Repeat all 8.30a privacy/authorization cases through encoded bytes and a hostile-client adapter. |
| 8.33 | Harness filters `PrivateOnlineSynchronizationTests`, `PrivateOnlineCompatibilityTests`, and `PrivateOnlineFaultInjectionTests`; Edit Mode filter `NetworkSynchronizationAdapterTests` | T2 minimum; lost/delayed/reordered/duplicate/stale/malformed/unknown/oversized messages, projection convergence, full resync, bounded idempotency retention, protocol/schema/map/content/build mismatch. Repeat complete suites if serialization/shared command infrastructure triggers T3 under policy. |
| 8.34 | Harness filters `PrivateOnlineReconnectTests` and `PendingDecisionReconnectTests`; Edit Mode filter `RelayReconnectAdapterTests`; Play Mode filter `ReconnectPlayerExperiencePlayModeTests` | T3 reconnect gate: every pending family at disconnect-before-answer, after-send-before-response, duplicate-after-reconnect, stale-after-resync, and successful resume; reserved ownership, rotated connection epoch, terminal host loss, HUD clears stale controls, no private leakage. Both complete suites. |
| 8.35a | Edit Mode filter `PrivateLobbyTests`; Play Mode filter `PrivateLobbyPlayModeTests` | T2 plus targeted non-batch visual/input inspection at supported desktop viewports; duplicate/full/incompatible/unauthorized/host-loss/join-failure/reconnect feedback. |
| 8.35b | Edit Mode filter `OnlineMatchHudTests`; Play Mode filter `OnlineMatchHudPlayModeTests` | T3 network/UI integration: lobby-to-match, all prompt families, waiting/stale/resync/disconnect/reconnect/terminal flows, equivalent local/network projection meaning, resource lifetime, both complete suites. |
| 8.35c | `Tools/Invoke-PrivateOnlineRehearsal.ps1 -Topology SixClient -FaultPlan Full` against one disposable Win64 build | T3 local rehearsal: one host plus five client processes, full match, selected fault plan, reconnect/resync, terminal host loss probe, projection/outcome convergence, privacy-reviewed logs. Repeat focused 8.32-8.35b checks invalidated by fixes and both complete suites at the final candidate. Local success is not internet evidence. |
| 8.36 | Repeat final focused 8.32-8.35b filters; `Tools/Invoke-PrivateOnlineRehearsal.ps1 -Topology SixClient -FaultPlan Full`; then the recorded two-network invited playtest protocol in [FUTURE_GATES.md](FUTURE_GATES.md) | T3 online hard gate: exact disposable build, six humans, two external networks, Relay-DTLS, full match, approved latency/disconnect/reconnect cases, host/client convergence, privacy-reviewed diagnostic bundle, both complete unfiltered suites at final revision. |
| 8.36a | T0 source/log/build/test evidence review | No implementation or rerun without a concrete evidence gap; living-document validator and `git diff --check`. |

## Fault-plan constants

The deterministic local fault injector operates above UTP and below the protocol codec so it is service-independent and replayable. `Full` means: drop one outbound projection, delay one within the bounded window, deliver two duplicates, reorder two messages, replay one completed submission, submit stale/current/future projection versions, use an unknown kind/discriminator, truncate a frame, declare a mismatched length, exceed every configured size limit by one byte, forge every stable identity field, disconnect/reconnect each pending family at the five required cut points, expire reconnect eligibility, and terminate the host. Each case records its fixed seed and smallest message prefix.

No fixture may log another seat's owner-private fields to prove absence. Privacy checks deserialize each captured client byte stream and inspect the typed field inventory; diagnostics checks compare against an explicit allow-list.
