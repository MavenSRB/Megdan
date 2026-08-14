# Future Validation Gates and Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

## Official board-map specification validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

Expected result: the validator exits with code `0` and reports that reusable invariants and synthetic map-local identity isolation pass, followed by 292 cells, 210 playable spaces, 82 terrain cells, seven zones, 809 physical connections, 530 playable connections, and 40 spawn points split evenly between Red and Blue. The reusable layer verifies map-local `SpaceId`, `ZoneId`, and `SpawnPointId` contracts, immutable `(MapId, revision)` selection, selected-map-local coordinate queries, references, zone adjacency, ordered-Lane structure, and structural invariants without official-map constants. Bounded in-memory fixtures reuse all three child IDs and their coordinate under different synthetic `MapId` values. The official layer separately verifies every reviewed count/location and the zone/space/spawn/Lane digest, including all rules-relevant zone metadata.

## Runtime performance baseline

Regression evidence, not a production performance target. Capture in a development Editor with the Unity Profiler or `ProfilerRecorder` over the complete 292-cell map and a representative two-round director flow: steady-state frames, complete map rebuilds, snapshot-only captures, retained-prefix Undo replay, and the accepted-command refresh metric timing the configured presenter submission through accepted `MatchSession.Execute`. `Phase4RuntimeHygieneTests` owns the repeatable resource-lifecycle, unchanged-HUD projection/allocation, supported-object-discovery, and malformed rule-event-log envelope assertions that accompany a capture.

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

A threshold crossing requires investigation and an approved fix, a rationale-preserving re-baseline, or explicit user acceptance; it never authorizes speculative optimization. The Undo-replay metric is re-examined at the 7.9 complete-loop baseline per `F-ER2-08`.

Completed milestones' gate recipes and their recorded run evidence moved verbatim to the [testing archive](../Archive/TESTING_HISTORY.md#completed-gate-recipes-and-run-evidence-routed-2026-08-14-milestone-61ag) on 2026-08-14.
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
GUARDS_SMOKE: COMPOSED heroes=2 cards=10 version=0
GUARDS_SMOKE: COMMAND_ACCEPTED kind=CommitCard version=1 commitment=CardCommittedFacedown
GUARDS_SMOKE: PASS
```

`COMPOSED` proves the map resource loaded and the configured match composed inside the player. `COMMAND_ACCEPTED` proves a real typed `DirectorCommand` went through `IMatchSession` and advanced the authoritative projection - not that the scene merely opened.

The gate fails on any of: a build result other than `Succeeded`; a missing executable; a player that does not exit within the timeout; a non-zero player exit code; a missing pass marker; or a log matching a degradation pattern such as an unresolved shader, a failed load, or a `NullReferenceException`. The marker check is independent of the exit code on purpose - a player that degrades silently still exits zero, which is the exact failure this gate exists to catch.

Required at: milestone `6.1d`, where it was first proven; the Phase 6 gate `6.13`; the pre-build regression gate; and milestones `10.1` and `10.2`, which already depend on a launchable build. Rerun it whenever the always-included shader list, the enabled scene list, the render pipeline, `Assets/Resources`, an assembly definition, or a stripping setting changes.

Do not run this gate as a substitute for the Unity test suites. It proves the artifact runs; it does not prove the rules are right.
