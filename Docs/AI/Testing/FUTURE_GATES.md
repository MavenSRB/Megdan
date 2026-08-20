# Future Validation Gates and Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

## Official board-map specification validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

Expected result: the validator exits with code `0` and reports that reusable invariants and synthetic map-local identity isolation pass, followed by 292 cells, 210 playable spaces, 82 terrain cells, seven zones, 809 physical connections, 530 playable connections, and 40 spawn points split evenly between Red and Blue. The reusable layer verifies map-local `SpaceId`, `ZoneId`, and `SpawnPointId` contracts, immutable `(MapId, revision)` selection, selected-map-local coordinate queries, references, zone adjacency, ordered-Lane structure, and structural invariants without official-map constants. Bounded in-memory fixtures reuse all three child IDs and their coordinate under different synthetic `MapId` values. The official layer separately verifies every reviewed count/location and the zone/space/spawn/Lane digest, including all rules-relevant zone metadata.

## Runtime performance baseline

Regression evidence, not a production performance target. Capture in a development Editor with the Unity Profiler or `ProfilerRecorder` over the complete 292-cell map and a representative two-round director flow: steady-state frames, complete map rebuilds, snapshot-only captures, retained-prefix Undo replay, and the accepted-command refresh metric timing the configured presenter submission through accepted `MatchSession.Execute`. At milestone 7.9, extend that accepted-command capture across fixed reported command-history lengths from empty/short through the representative complete synthetic match; record refresh time, managed allocation per sample, and their growth against history length. `Phase4RuntimeHygieneTests` owns the repeatable resource-lifecycle, unchanged-HUD projection/allocation, supported-object-discovery, and malformed rule-event-log envelope assertions that accompany a capture.

Current reference figures, recorded at runtime baseline revision `7ad51a7` (milestones 4.4d and 4.4h; the full captures and their environment are archived), with noise-tolerant investigation thresholds:

| Metric | Baseline | Threshold |
| --- | ---: | ---: |
| Max steady-state managed allocation | 1,418 B/frame | 64 KiB/frame |
| p95 main-thread frame time | 0.170 ms | 10 ms |
| Average map rebuild | 1.849 ms | 25 ms |
| Owned map native resources | 11 | zero growth |
| Average snapshot-only capture | 0.183 ms | 5 ms |
| Undo replay | 28.203 ms | 250 ms |
| Accepted-command refresh, 44 samples | 1.254 ms focused, 0.401 ms complete-suite | 25 ms |
| Accepted-command refresh, complete-match history | 0.217 ms .NET, 0.359 ms Mono, at 501 commands | 25 ms |
| Accepted-command refresh managed allocation | 82,296 B at an empty history; 667,952 B worst sample; **.NET only** | 1 MiB/sample |
| Accepted-command allocation growth across a complete match | superseded at 7.9b: +105,584 B median under full-history copying; **.NET only** | 256 KiB (superseded) |
| Accepted-command allocation growth, retained history shared (7.9b) | +21,068 B median residual, one command kind, history 0-13 against 487-496; **.NET only** | 64 KiB |
| Undo replay, complete-match history | superseded at 7.9b: 429 ms Mono, 171 ms .NET, full-prefix replay of 502 commands | 600 ms (superseded) |
| Undo, retained-point reconstruction (7.9b) | 30.9 ms Mono, 56.4 ms .NET, at 502 commands | 150 ms |
| Six-player decoded snapshot payload | Unmeasured at 3v3 scale; 6,633 B at the staged 1v1 setup | reference only |
| End-of-round leveling stage, six heroes | Unmeasured at 3v3 scale | 25 ms |

The complete-match rows are measured by `CompleteMatchRuntimeCaptureTests`, which replays a fixed reported seed's complete match to each sampled history length and measures the one command after it. Milestone 7.9 measured them first, and two crossed; milestone 7.9b closed both crossings, so those two rows appear twice - the superseded figure kept beside the current one, because the correction is measured against it. The refresh and absolute-allocation figures are 7.9b's re-measurement of the same rows.

The allocation threshold was **64 KiB/sample and had never been measured**: that figure was the per-*frame* steady-state number from the row above it, reused for a per-*command* metric nothing had sampled. The first measurement puts an accepted command between 101 KB and 736 KB at every history length including an empty one, so the declared number was never a description of this build and no regression put it there. It is re-baselined to 1 MiB, roughly 40% above the worst measured sample. The user was shown the measurement and approved this disposition on 2026-08-20.

The growth rows are `RISK-PERF-001`'s own prediction, measured at 7.9 and closed at 7.9b; the superseded row is kept because its figure is what the correction is measured against. Milestone 7.9 held one command kind fixed and measured median allocation rising about 210 bytes per command of retained history, +105,584 B across a 502-command match - a crossing under the old line, which is what scheduled the corrective milestone. Milestone 7.9b removed every mechanism behind it: the projection shares the append-only logs' constant-time snapshots for the accepted-command history and both event streams instead of copying them, and the per-command pre-image serialization writes current state only, sharing each completed round's serialized fragment from the round it froze through `CompletedRoundsSerializationCache`. The same sampler now measures **+21,068 B** between the same two windows, and the residual is state size rather than history length - items held, levels bought, and the mix of the two `CommitCard` shapes the median straddles - so the 64 KiB line fails on the copying mechanism returning, not on a late match being late. Refresh *time* still crosses nothing: 0.217 ms against 25 ms.

`F-ER2-08`'s full-prefix Undo was the row that grew most, the one place a measured threshold was widened rather than corrected, and is closed at 7.9b; both superseded figures are kept above. The 250 ms line came from 28.203 ms over the two-round director flow at milestone 4.4d; the complete 20-round match retains 502 commands, a full-prefix Undo replayed all of them at **429 ms on Unity's Mono and 171 ms on .NET**, and milestone 7.9 re-baselined to 600 ms against the slower runtime with the fix separately approved on 2026-08-20. Milestone 7.9b is that fix: the session retains the aggregate's constant-cost pre-image every 32 accepted commands, and Undo reconstructs from the latest retained point at or before the surviving prefix, so the replayed segment is bounded by the interval however long the match is. Measured at complete-match length it is **30.9 ms on Mono and 56.4 ms on .NET** - a complete-match Undo back at what the two-round Undo cost at 4.4d - and the 150 ms line sits roughly 2.7x above the slower tier while every full-prefix replay of this match measures above it, so it fails if the reconstruction ever replays the whole prefix again. `UndoCheckpointReconstructionTests` holds the retained point to being a derived cache: reconstructions above, exactly on, and below a retained point are each byte-identical to a fresh full-prefix replay, and the complete-loop gate still proves the undone match replays back into the same ending.

**Two of these rows are measurable on one tier only.** `GC.GetAllocatedBytesForCurrentThread` returns a constant zero on Unity's Mono, so every allocation figure above is the cross-platform harness's. `CompleteMatchRuntimeCaptureTests` detects that rather than assuming it, records `runtimeReportsAllocation=False`, and asserts that a runtime reporting nothing reports nothing everywhere - so the Windows tiers record an honest absence instead of passing an allocation check vacuously. Time is reported on every runtime and is asserted on every tier.

A threshold crossing requires investigation and an approved fix, a rationale-preserving re-baseline, or explicit user acceptance; it never authorizes speculative optimization. Milestone 7.9 performed that measurement - Undo replay and refresh time crossed nothing, the allocation line was re-baselined for the reason recorded above, and the growth term crossed - and milestone 7.9b was the approved fix it scheduled, closing both crossings with the figures and tightened lines recorded above.

The last two rows carry no baseline because nothing has run at 3v3 scale, and they are recorded here so `RISK-SCALE-001` has something to be measured against rather than reasoned about. The payload row is a reference figure rather than a threshold of its own: what it costs is already bounded by the snapshot-only capture row above, since the payload is serialized once per accepted command and twice per refused one. Milestone 8.25 records the six-player payload against the 1v1 reference, because it is the first milestone at which 108 card instances and 108 definitions exist. Milestone 8.27 samples the leveling stage, because it is the first whose acceptance checks reach repeated rounds and progression with six heroes; milestone 8.26 is scoped to one complete six-hero turn and names neither figure, so it measures neither.

Completed milestones' gate recipes and their recorded run evidence moved verbatim to the [testing archive](../Archive/TESTING_HISTORY.md#completed-gate-recipes-and-run-evidence-routed-2026-08-14-milestone-61ag) on 2026-08-14.

## Post-polish multiplayer regression gate

Milestone `9.16` owns the final-revision multiplayer regression after presentation polish. It reuses the exact six-client local process/build topology selected at milestone `8.29` and proven at milestone `8.35c`; milestone `8.29` must add the executable command and bounded fault recipe here once those details exist.

At the exact `9.16` source and executable-content revision, build the disposable private Win64 validation player, start six local clients in six distinct seats, and complete a full match through the polished player UI. Exercise the approved disconnect/reconnect and resynchronization cases. Evidence records the source, build, content/schema, provider/SDK/configuration, topology, and recipe revisions and proves all of the following:

- every client converges on the host's authoritative public outcome;
- no client receives another seat's hidden hand or pre-reveal commitment identity;
- cross-seat hero commands and team choices are rejected without partial mutation;
- reconnect and resynchronization restore the same seat and control ownership without transfer, duplicated input handlers, stale selection, or stale actionable prompts; and
- privacy-reviewed host/client diagnostics disclose no owner-private state.

Repeat the milestone `8.36` real-internet playtest only when affectedness or observed failure makes its earlier evidence stale. A repeat is required when a change since the accepted `8.36` revision touches the provider or SDK version/configuration, relay/NAT/lobby/invite or connection establishment, transport delivery, network serialization/version compatibility, timeout/latency/disconnect/reconnect/host-loss behavior, or another path whose correctness depends on separate networks or real internet conditions. It is also required when the local six-client gate or its diagnostics exposes a connectivity, synchronization, privacy, or ownership failure that cannot be closed confidently without multi-network evidence. Presentation-only changes with unchanged online/session/projection behavior, and failures reproduced and closed wholly within the local topology, do not by themselves require another invited playtest.

The local gate does not establish relay/NAT traversal, external-latency, multi-network, or internet-reliability claims. Any required internet repeat uses a separately identified disposable private build and the milestone `8.36` privacy/distribution boundaries; it is validation evidence, not the retained pitch deliverable.

## Player-build smoke gate

Added at milestone 6.1d. An Editor suite cannot observe a player that ships with an unresolved shader, a missing resource, or a type the build stripped: every Edit Mode and Play Mode test runs inside the Editor, where those assets are always present. This gate builds the real artifact, runs it, and requires the running player to say what it reached.

Run from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-PlayerBuildSmoke.ps1
```

The script builds through the fixed Editor entry point `GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer`, which targets Windows 64-bit standalone from the enabled scenes and writes to the gitignored `Builds/Win64Smoke/`. It then launches the player with `-batchmode -nographics -guardsSmokeTest`. That flag is the only thing that makes `MatchSceneCompositionRoot` attach `PlayerSmokeTestDriver`; a normally launched build constructs no test hook at all.

Expected result: exit code `0` and `GATE PASSED`. The player log must contain, in order:

```
GUARDS_SMOKE: BEGIN
GUARDS_SMOKE: SCENE Milestone1_2_HexGrid
GUARDS_SMOKE: COMPOSED heroes=2 cards=36 version=0
GUARDS_SMOKE: COMMAND_ACCEPTED kind=CommitCard version=1 commitment=CardCommittedFacedown
GUARDS_SMOKE: PAUSE_ANSWERED transition=... step=... options=...
GUARDS_SMOKE: ROUNDS_PLAYED rounds=2 ...
GUARDS_SMOKE: PASS
```

`COMPOSED` proves the map resource loaded and the configured match composed inside the player. `COMMAND_ACCEPTED` proves a real typed `DirectorCommand` went through `IMatchSession` and advanced the authoritative projection - not that the scene merely opened.

Milestone 7.9 added the last two required markers, because one accepted command proves the stack survived the build and does not prove the built player can be played. `PAUSE_ANSWERED` and `ROUNDS_PLAYED` come from a smoke path that plays two whole rounds, answering whichever decision the projection is waiting on with that decision's own first projected option - deterministic without a seed, so one build always writes one log. Two rounds rather than one because milestone 7.6 established that an ordinary end of round only stops on p.15's Upgrade a Card from the second round onward, and a built player that reaches a pause it cannot answer completes nothing. The path never derives legality: every branch reads projected invocations or published offers, exactly as the HUD does.

The gate fails on any of: a build result other than `Succeeded`; a missing executable; a player that does not exit within the timeout; a non-zero player exit code; a missing pass marker; or a log matching a degradation pattern such as an unresolved shader, a failed load, or a `NullReferenceException`. The marker check is independent of the exit code on purpose - a player that degrades silently still exits zero, which is the exact failure this gate exists to catch.

Required at: milestone `6.1d`, where it was first proven; the Phase 6 gate `6.13`; the Phase 7 gate `7.9`; the pre-build regression gate; and milestones `10.1` and `10.2`, which already depend on a launchable build. Rerun it whenever the always-included shader list, the enabled scene list, the render pipeline, `Assets/Resources`, an assembly definition, or a stripping setting changes.

Milestone 7.9 is the reason `7.9` is on that list, and the run is the argument for the list being longer rather than shorter. The gate had not been run since milestone 7.8a replaced the HUD's immediate-mode drawing with a runtime UI Toolkit panel, because 7.8a is not a milestone this gate is required at. The player it built logged 71 warnings and 17 `NullReferenceException`s out of `UITKTextHandle.ShapeText`. The player states the cause itself: the ICU data its text shaper needs ships only when the **build contains at least one `PanelSettings` asset**, and a HUD that created its settings entirely at runtime gave the build none. Present with a graphics device as well as under `-nographics`, so it was a defect in the artifact and not a headless artefact, and invisible to every Editor tier because the Editor has the data loaded either way. Fixed by committing `Assets/Resources/UI/GuardsRuntimePanelSettings.asset` and copying the panel from it; the gate then reported zero of both. The asset's own `m_ICUDataAsset` field is empty on disk and is not what carries the data - its presence in the build is - so do not "fix" that field.

Do not run this gate as a substitute for the Unity test suites. It proves the artifact runs; it does not prove the rules are right.
