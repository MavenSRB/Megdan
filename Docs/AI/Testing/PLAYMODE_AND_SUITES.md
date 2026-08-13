# Play Mode, Complete Suites, and Test Ownership

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

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

## Play Mode shell test

```powershell
$playModeArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.TestShellPlayModeTests.TestRunner_CanEnterPlayModeAndAdvanceOneFrame',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\play-mode-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\play-mode-tests.log"'
)

$playModeProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $playModeArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($playModeProcess.ExitCode -ne 0) {
    throw "Play Mode tests failed with exit code $($playModeProcess.ExitCode)."
}
```

Expected result: the shell fixture discovers at least one test, all discovered cases pass after entering Play Mode and advancing one frame, and the process exits with code `0`.

## Complete gate suites

Run both unfiltered suites only for T3 validation: a final implementation milestone of a phase, an explicit hard gate, shared test/package/assembly infrastructure, the pre-build regression gate, or a change promoted because its blast radius cannot be enumerated. Resolve focused failures before spending time on these suites. These commands intentionally omit `-testFilter`.

### Known-good complete-suite baseline

The most recent green unfiltered totals, so a task can tell a regression it introduced from a pre-existing failure without re-running anything first. This is a current reference point, not historical evidence; replace it whenever a milestone legitimately changes a total, and state in that milestone's evidence why the total moved.

| Suite | Passed | Failed | Skipped | Inconclusive | Revision |
| --- | ---: | ---: | ---: | ---: | --- |
| Complete Edit Mode | 331 | 0 | 0 | 0 | `6.1d` first commit |
| Complete Play Mode | 17 | 0 | 0 | 0 | `6.1d` first commit |

A total that drops without a milestone deleting or merging fixtures means coverage was lost, not that the suite got faster. A total that rises is expected whenever a milestone adds fixtures.

Milestone 6.1d's first commit moved the Edit Mode total from 328 to 331: `+2` for the new
`RuntimeShaderResolutionTests`, and `+1` for the Editor-assembly case added to
`ApplicationArchitectureBoundaryTests`. Play Mode is unchanged at 17; that commit added no Play Mode
fixture.

Milestone 6.1c moved the Edit Mode total from 354 to 328, and the drop is fully accounted for:
`-11` for the deleted `HeroSelectionControllerTests`, `-20` for the deleted
`CardDrivenMovementPresentationTests` (20 methods / 21 executed cases, less the one case relocated
verbatim into `ActionOfferAndPrintedValueTests` because it never touched the drill), `-1` for the
retired legacy-drill highlight teardown case, `+4` for the new `ConfiguredPointerSelectionTests`,
`+1` for the fourth `DirectorHandPresentationTests` case added when that fixture was rewritten
against `DirectorHandsPresenter`, and `+1` for the new pointer-adapter conformance case in
`ConfiguredMatchCutoverArchitectureTests`. Play Mode is unchanged at 17: 6.1c removed and added
assertions inside existing Play Mode tests but added or deleted no test method.

The Play Mode figure was recorded as `15` when this table was created and was corrected at 6.1g to `17`. Milestone 6.1g added no Play Mode fixture, and `Assets/Tests/PlayMode` declares exactly seventeen `[Test]`/`[UnityTest]` methods, all seventeen of which the 6.1g run discovered and passed - so seventeen was already the true total at `df965cd`. The original entry understated it. An understated baseline is worse than none: the next task to run both suites would have seen two fixtures apparently appear from nowhere and gone looking for a regression that never existed. The Edit Mode figure was recorded correctly and is unchanged.

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
