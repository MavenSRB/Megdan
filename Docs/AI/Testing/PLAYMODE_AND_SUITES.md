# Play Mode, Complete Suites, and Test Ownership

Canonical active shard. Return to [../TESTING.md](../TESTING.md). The raw `Start-Process` blocks in this shard are test-filter and historical-output references only; write `tmp/unity-validation-request.json` and invoke the fixed launcher per [POLICY.md](POLICY.md).

## Placeholder-grid focused Play Mode test

```powershell
$minimalGridPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\minimal-grid-play-tests.log"'
)

$minimalGridPlayProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $minimalGridPlayArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($minimalGridPlayProcess.ExitCode -ne 0) {
    throw "Minimal-grid Play Mode test failed with exit code $($minimalGridPlayProcess.ExitCode)."
}
```

Expected result: all discovered cases pass after loading the complete 292-space selected map with twelve configured minions, two configured heroes, one fixed camera, one composition-owned match session, passive feature views, and no legacy controller. The cases cover the configured director hands/commitments and Initiative path, contextual Hold and Movement Stop, passive-board-callback/presenter destination input, authoritative Movement mutation and cleared highlights, unavailable Attack rejection without mutation, configured legal-minion Attack cue and stable pointer intent, accepted Defeat/reward/removal completion with rendered-minion removal, Undo/restart/snapshot restore, diagnostic-log projection, two complete uninterrupted rounds, HUD pointer blocking, and exact generated-highlight counts across scene reload.

## Complete gate suites

Run both unfiltered suites only for T3 validation: a final implementation milestone of a phase, an explicit hard gate, shared test/package/assembly infrastructure, the pre-build regression gate, or a change promoted because its blast radius cannot be enumerated. Resolve focused failures before spending time on these suites. These commands intentionally omit `-testFilter`.

### Known-good complete-suite baseline

The most recent green unfiltered totals, so a task can tell a regression it introduced from a pre-existing failure without re-running anything first. This is a current reference point, not historical evidence; replace it whenever a milestone legitimately changes a total, and state in that milestone's evidence why the total moved.

| Suite | Passed | Failed | Skipped | Inconclusive | Revision |
| --- | ---: | ---: | ---: | ---: | --- |
| Complete Edit Mode | 830 | 0 | 0 | 0 | `6.5ae` |
| Complete Play Mode | 28 | 0 | 0 | 0 | `6.5ae` |

An earlier complete Edit Mode run, at the revision the superseded `6.1ad` row recorded, is what caught the stale `CardAggregateInvarianceTests` digests, which no focused filter had selected since 6.1f. Any milestone that changes `AuthoritativeSnapshotSerializer.CurrentSchemaVersion` must run both digest-pinning fixtures - `CardAggregateInvarianceTests` and `SnapshotCompatibilityCorpusTests` - because a schema change invalidates every pinned digest by construction. `CardAggregateInvarianceTests` sits in the dotnet harness exclude list where the Linux harness cannot reach it; `SnapshotCompatibilityCorpusTests` runs in the harness but must still be selected deliberately on the Windows tier, since neither is picked up by a filter built from the types a schema change touches.

A total that drops without a milestone deleting or merging fixtures means coverage was lost, not that the suite got faster. A total that rises is expected whenever a milestone adds fixtures.

Milestone 6.5ae moved the baseline to 830 Edit Mode, deleting no fixture, and left Play Mode at 28. Edit Mode rose by nine: five in `EffectOptionFeasibilityTests`, three in `ConfiguredDirectMoveSpawnSkillTests`, and one in `MatchFeaturePresenterTests`. Play Mode is unchanged in total but **not** in composition, which is the case this section warns about: `DirectMoveSpawnSkillPlayModeTests` still runs two cases, but its first is now the stop surface rather than the silent stop, and one assertion moved out of its second case, so the saved-scene evidence changed while the number did not. Milestone 6.5ad had moved the baseline to 821 Edit Mode, deleting no fixture, and left Play Mode at 28 in both total and composition, which is the expected result for a change that adds no Unity-facing path. Edit Mode rose by thirty-four over the row this replaces, and only sixteen of those are 6.5ad's: five in the new `AdmittedDistancesTests`, four in the new `OptionalDistanceConformanceTests`, five in `DisplacementCapabilityTests`, and two in `ConfiguredDirectMoveSpawnSkillTests`. The other eighteen are milestone 6.5ac's, whose complete run measured 805 Edit Mode and 28 Play Mode and never reached this row - which is what a stale baseline costs: the next milestone cannot tell its own delta from an unrecorded one without re-deriving it. Milestone 6.5b had moved the baseline to 787 Edit Mode and 28 Play Mode, deleting no fixture. Edit Mode rose by twenty: seventeen in the new `ConfiguredDirectMoveSpawnSkillTests`, two in `MatchFeaturePresenterTests` for the converted card's two surfaces, and one added by corrective milestone 6.5ab, whose T2 tier never ran an unfiltered suite and so never reached this row. Play Mode rose by three: two in the new `DirectMoveSpawnSkillPlayModeTests` and one likewise carried over from 6.5ab. Milestone 6.5aa had moved it to 767 Edit Mode and 25 Play Mode by adding shared-immunity, effective Minion-Battle, Hero Respawn, snapshot/replay, control-hover, and configured board-highlight fixtures; it deleted no fixture. Milestone 6.2a moved Edit Mode 392 to 440 by adding fixtures and deleting none: the effect model, resolver, policy, catalog, seam-conformance, Skill dispatch and snapshot-tampering fixtures. Play Mode is unchanged at 19 in both total and composition, the expected result for a change that adds no Unity-facing path. Milestone 6.1ap had moved it 382 to 392 by adding `ResolverEmittedCheckpointConformanceTests` (4) and `TurnTimingCheckpointOrderTests` (6).

The 6.1u measurement behind the previous row is retained because it explains a case where the totals moved without an unfiltered run: Edit Mode went 378 to 382 as 6.1a and 6.1ao added cases and 6.1u deleted four - three `AdjacentDestinationQueryTests` and one `TestShellEditModeTests` - while adding two. Play Mode read 19 before and after but not the same 19: 6.1a added a `MinimalHexGridPlayModeTests` case, taking that fixture from 14 to 15, and 6.1u deleted `TestShellPlayModeTests`. Compare compositions, not just totals, when a number looks unchanged.

```powershell
$completeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-edit-tests.log"'
)

$completeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $completeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($completeEditProcess.ExitCode -ne 0) {
    throw "Complete Edit Mode suite failed with exit code $($completeEditProcess.ExitCode)."
}

$completePlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\complete-play-tests.log"'
)

$completePlayProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $completePlayArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($completePlayProcess.ExitCode -ne 0) {
    throw "Complete Play Mode suite failed with exit code $($completePlayProcess.ExitCode)."
}
```

Expected result at a T3 gate: every currently discovered Edit Mode and Play Mode test passes, each process exits with code `0`, and fresh complete NUnit XML files record the gate result with zero unexpected skipped or inconclusive tests.

## Compile probes

Some contracts are enforced by the compiler, not by a test. A test can assert that correct code behaves correctly; it cannot assert that incorrect code fails to build. When a milestone's closing check is "this must not compile", the evidence is a probe run, not a fixture.

Probe sources live in `Tools/CompileProbes/`, outside `Assets/`, so Unity never imports them. The procedure and the standing probe set are documented in `Tools/CompileProbes/README.md`. In outline:

1. Copy the probe into the assembly whose contract it tests - for a `GuardsDigital.Core` `internal` member that means `Assets/GuardsDigital/Core/__CompileProbe.cs`, because Core declares no `InternalsVisibleTo`. Do not add one to make a probe convenient.
2. Write `tmp/unity-validation-request.json` with any focused Edit Mode filter and run the fixed launcher. The probe is answered at compile time, before test discovery, so the filter's own result is irrelevant.
3. Record the revision, exact command, and compiler output. A probe that is *expected to compile* needs stronger evidence than a silent log: confirm the probe type is present inside the built assembly under `Library/ScriptAssemblies/`, which proves it was compiled rather than skipped.
4. Delete the probe and its generated `.meta`, then re-run a focused filter to confirm the tree is clean again.

A launcher exit code of `1` is the expected, correct result for a probe that must fail to compile. Do not treat it as a broken run.

Never leave a probe in `Assets/`.

## Test ownership

- `Assets/Tests/EditMode/` contains tests that run without entering Play Mode.
- `Assets/Tests/PlayMode/` contains tests that require Play Mode.
- Future milestones should add the narrowest relevant tests to these assemblies or create a purpose-specific test assembly when isolation requires it.
- T0 non-code validation is recorded in the milestone handoff rather than represented by an empty or unrelated Unity test run.
