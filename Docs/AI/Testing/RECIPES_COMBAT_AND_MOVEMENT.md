# Combat and Card-Movement Test Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

## Fast Travel replacement T2 tests

Write `tmp/unity-validation-request.json` and invoke the fixed launcher specified in [POLICY.md](POLICY.md). Run these requests serially.

Final affected Edit Mode request:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.CardActionStrategyRegistryTests;GuardsDigital.Tests.EditMode.Phase4ArchitectureConformanceTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests;GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.ApplicationArchitectureBoundaryTests;GuardsDigital.Tests.EditMode.ConfiguredMatchCutoverArchitectureTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests",
  "results": "tmp/milestone-5.2-final-affected-edit-results.xml",
  "log": "tmp/milestone-5.2-final-affected-edit.log"
}
```

Configured saved-scene Play Mode request:

```json
{
  "platform": "PlayMode",
  "filter": "GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests",
  "results": "tmp/milestone-5.2-configured-play-results.xml",
  "log": "tmp/milestone-5.2-configured-play.log"
}
```

Expected result: every discovered case passes with no skipped or inconclusive case. Edit Mode covers Fast Travel rules/query/backend/event/session/replay/Undo plus affected turn, round, snapshot, presenter, architecture, and configured-scene source contracts. Play Mode proves the configured passive offer/highlight/pointer/session path and the directly affected scene regressions. Unfiltered suites are intentionally deferred to milestone 5.8 unless an escalation trigger appears.

## Action-offer and printed-value focused Edit Mode tests

```powershell
$actionOfferValueEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-2-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-2-focused.log"'
)

$actionOfferValueEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $actionOfferValueEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($actionOfferValueEditProcess.ExitCode -ne 0) {
    throw "Action-offer and printed-value Edit Mode tests failed with exit code $($actionOfferValueEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering ordered immutable action definitions; printed Primary/Secondary Movement plus printed Primary non-Ranged and Ranged Attack offers; Hand-to-Discard-Pile Defense reaction declarations; actor/source/window binding; typed Movement, Attack, Defense, and Range printed-value contributions/totals; discarded/cross-owner/missing-owner rejection; invalid value/channel/range/origin/transition rejection; unresolved offer references; and the directly affected card definition/instance regressions.

## Basic adjacent hero Attack focused Edit Mode tests

```powershell
$attackInitiationEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3-all-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3-all-affected.log"'
)

$attackInitiationEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $attackInitiationEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($attackInitiationEditProcess.ExitCode -ne 0) {
    throw "Basic adjacent hero Attack Edit Mode tests failed with exit code $($attackInitiationEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering only-adjacent-enemy hero target projection; self/friendly/non-adjacent/empty/terrain/token/non-unit-obstacle exclusion; stable target commands; stale-board, forged-offer, no-target, and overlapping-resolution rejection without card mutation; typed printed Attack contribution/total; ordered Attack-start/value/Defense-request events; defender-owned eligible reaction projection; exact pending-choice restoration; deterministic replay; visible Attack offer, exact legal-target highlighting, highlight clearing after acceptance, and pointer target submission; and directly affected Movement, offer/value, participant, occupancy, resolver, event-consumer, controller, and scene regressions. The milestone 2.3 presentation correction is T2: its initial final affected Edit Mode filter passed 71/71, the target-highlight regression filter passed 43/43 (`CardDrivenMovementPresentationTests`, `AttackActionResolutionAuthorityTests`, `CardActionResolutionAuthorityTests`, and `HeroSelectionControllerTests`), focused Play Mode passed 6/6 (`MinimalHexGridPlayModeTests`), and targeted visual inspection confirmed both a readable `PRIMARY ATTACK 3` control and a strong yellow legal-target highlight. Complete unfiltered suites remain unnecessary because this is not a T3 gate and the affected consumers are enumerated.

## Basic Ranged hero Attack focused Edit Mode tests

```powershell
$rangedAttackEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3a-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-3a-final-affected.log"'
)

$rangedAttackEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $rangedAttackEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($rangedAttackEditProcess.ExitCode -ne 0) {
    throw "Basic Ranged hero Attack Edit Mode tests failed with exit code $($rangedAttackEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering typed Ranged Attack/Range definition shape and offer-derived totals; Range 1 adjacency; inclusive maximum and beyond-Range rejection; explicit axial distance through occupied/obstacle spaces without line of sight; self/friendly/non-unit exclusions; forged/stale/invalid attempts without mutation; shared target-command, ordered Attack events, defender-owned response, restoration, and deterministic boundaries for both range classifications; visible Ranged offer/status/highlight/pointer integration; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.3a T2 evidence passed final focused Edit Mode 36/36, final affected Edit Mode 114/114, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero skipped or inconclusive cases. Targeted visual inspection confirmed `PRIMARY RANGED ATTACK 2 RANGE 2`, its status text, and the strong yellow legal-target highlight render without clipping or overlap. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and the affected consumers are enumerated.

## Basic Defense response focused Edit Mode tests

```powershell
$defenseResponseEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-4-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-4-final-affected.log"'
)

$defenseResponseEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $defenseResponseEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($defenseResponseEditProcess.ExitCode -ne 0) {
    throw "Basic Defense response Edit Mode tests failed with exit code $($defenseResponseEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly one currently legal defender-owned in-hand Defense selection or decline; Hand-to-Discard-Pile mutation with other state facts preserved; distinct reaction invocation context; printed Defense contribution/total only on discard; no total on decline or no legal card; missing, multiple, cross-owner, attacker-selected, discarded, stale, forged, and replayed answer rejection without mutation; restoration at both response and comparison checkpoints; ordered discard/total-or-decline/completion events; visible legal Defense/Decline controls and comparison-pending status; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.4 T2 evidence passed authority-focused Edit Mode 17/17, final affected Edit Mode 120/120, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases. Targeted visual inspection confirmed readable Defense controls/status, removal of the discarded choice, and clean advancement to the milestone 2.5 comparison checkpoint. Complete unfiltered suites were intentionally omitted because this is not a T3 gate and all affected consumers are enumerated.

## Basic Attack outcome focused Edit Mode tests

```powershell
$attackOutcomeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-5-final-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-5-final-affected.log"'
)

$attackOutcomeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $attackOutcomeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($attackOutcomeEditProcess.ExitCode -ne 0) {
    throw "Basic Attack outcome Edit Mode tests failed with exit code $($attackOutcomeEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering equal and greater Defense as successfully defended; lower Defense, decline, and no legal Defense as attack successful; stable same-resolution continuation; comparison revalidation and stale-state rejection without closing the Attack; typed outcome/completion events with actor, target, totals, and Defense source; deterministic replay; visible result text; pending-Attack closure; released card/actor selection; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.5 T2 evidence passed authority-focused Edit Mode 23/23, final affected Edit Mode 126/126, and focused Play Mode 6/6 (`MinimalHexGridPlayModeTests`), all with zero failed, skipped, or inconclusive cases. Targeted visual inspection confirmed `Attack 3 vs Defense 3: successfully defended` is readable after the discarded card leaves the hand and the Attack closes. Complete unfiltered suites were intentionally omitted because milestone 2.5 is not the Phase 2 T3 gate and all affected consumers are enumerated.

## Card-foundation authority hardening focused Edit Mode tests

```powershell
$cardFoundationHardeningArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\card-foundation-hardening-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\card-foundation-hardening.log"'
)

$cardFoundationHardeningProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardFoundationHardeningArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardFoundationHardeningProcess.ExitCode -ne 0) {
    throw "Card-foundation hardening Edit Mode tests failed with exit code $($cardFoundationHardeningProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering externally immutable definition/value/choice/continuation collections, strict origin/policy validation, timing-window-scoped offer projection, authoritative forged/stale offer rejection, structured missing-actor rejection, exact pending Movement continuation data, restoration through a fresh authority, deterministic choice resumption, and the directly affected definition, value, movement, reachability, and adjacent-move regressions.

## Card-resolution Movement focused Edit Mode tests

```powershell
$oneClickCardMovementArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-12b-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-12b-edit.log"'
)

$oneClickCardMovementProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $oneClickCardMovementArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($oneClickCardMovementProcess.ExitCode -ne 0) {
    throw "One-click card-resolution Movement Edit Mode tests failed with exit code $($oneClickCardMovementProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering immutable and structurally valid destination/route/cost candidates, typed Primary/Secondary allowance, partial segments that continue inside one resolution, cumulative waypoint routes, Stop with unused allowance, automatic exhaustion, ordered internal step/choice events, obstacle exclusion and legal detours, stale/invalid/changed-board rejection without unintended partial actor movement, exact restored choices after zero or more committed steps, deterministic replay, and the directly affected card, movement, reachability, adjacent-authority, and controller regressions.

## Visible card-driven Movement focused Edit Mode tests

```powershell
$visibleCardMovementArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDrivenMovementPresentationTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.HeroSelectionControllerTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-13-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-13-focused-edit.log"'
)

$visibleCardMovementProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $visibleCardMovementArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($visibleCardMovementProcess.ExitCode -ne 0) {
    throw "Visible card-driven Movement Edit Mode tests failed with exit code $($visibleCardMovementProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering two visibly test-only hand cards; separate card/action selection; typed Primary Movement 3 and Secondary Movement 1 allowances; explicit Primary/Secondary text policy; exact pending destination/route/cost projection; partial waypoint continuation with refreshed highlights; zero and explicit early Stop; stale UI-choice rejection; blocked destinations and legal obstacle detours; pointer-submitted destination commands; retained selection while pending; cleared selection after completion; and saved-scene card-driven wiring.
