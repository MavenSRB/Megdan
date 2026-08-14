# Combat and Card-Movement Test Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md). The raw `Start-Process` blocks in this shard are test-filter and historical-output references only; write `tmp/unity-validation-request.json` and invoke the fixed launcher per [POLICY.md](POLICY.md).

## Minion obstacle and Defense contribution T2 tests

Write `tmp/unity-validation-request.json` and invoke the fixed launcher specified in [POLICY.md](POLICY.md). Run these requests serially.

Focused Edit Mode request:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.MinionObstacleAndDefenseTests",
  "results": "tmp/milestone-5-3-focused-results.xml",
  "log": "tmp/milestone-5-3-focused.log"
}
```

Directly affected Edit Mode request:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests;GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests",
  "results": "tmp/milestone-5-3-regression-results.xml",
  "log": "tmp/milestone-5-3-regression.log"
}
```

Configured saved-scene Play Mode request:

```json
{
  "platform": "PlayMode",
  "filter": "GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests",
  "results": "tmp/milestone-5-3-play-results.xml",
  "log": "tmp/milestone-5-3-play.log"
}
```

Expected result: every discovered case passes with no skipped or inconclusive case. Edit Mode covers authoritative minion identity/type/space reuse, one-obstacle occupancy, Movement blocking, exact Defense modifier boundaries and provenance, restore/snapshot determinism, and affected consumers. Play Mode proves the configured scene remains operational with minions bound into the board. Unfiltered suites are intentionally deferred to milestone 5.8 unless an escalation trigger appears.

## Explicit Fast Travel selection T2 tests

Write `tmp/unity-validation-request.json` and invoke the fixed launcher specified in [POLICY.md](POLICY.md). Run these requests serially.

Final affected Edit Mode request:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ApplicationArchitectureBoundaryTests;GuardsDigital.Tests.EditMode.ConfiguredMatchCutoverArchitectureTests;GuardsDigital.Tests.EditMode.Phase4ArchitectureConformanceTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.FastTravelActionTests",
  "results": "tmp/milestone-5.2a-final-affected-edit-results.xml",
  "log": "tmp/milestone-5.2a-final-affected-edit.log"
}
```

Configured saved-scene Play Mode request:

```json
{
  "platform": "PlayMode",
  "filter": "GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests",
  "results": "tmp/milestone-5.2a-configured-play-results.xml",
  "log": "tmp/milestone-5.2a-configured-play.log"
}
```

Expected result: every discovered case passes with no skipped or inconclusive case. Edit Mode covers explicit selection state, button visibility, destination suppression/activation, passive-view intent forwarding, unchanged Fast Travel authority/session behavior, and the directly affected architecture/saved-scene source contracts. Play Mode proves the configured passive button/highlight/pointer/session path. Unfiltered suites are intentionally deferred to milestone 5.8 unless an escalation trigger appears.

## Fast Travel replacement T2 tests

Write `tmp/unity-validation-request.json` and invoke the fixed launcher specified in [POLICY.md](POLICY.md). Run these requests serially.

Final affected Edit Mode request:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.CardActionDispatcherCompositionTests;GuardsDigital.Tests.EditMode.Phase4ArchitectureConformanceTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests;GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.ApplicationArchitectureBoundaryTests;GuardsDigital.Tests.EditMode.ConfiguredMatchCutoverArchitectureTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests",
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests',
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

Expected result: all discovered cases pass, covering only-adjacent-enemy hero target projection; self/friendly/non-adjacent/empty/terrain/token/non-unit-obstacle exclusion; stable target commands; stale-board, forged-offer, no-target, and overlapping-resolution rejection without card mutation; typed printed Attack contribution/total; ordered Attack-start/value/Defense-request events; defender-owned eligible reaction projection; exact pending-choice restoration; deterministic replay; visible Attack offer, exact legal-target highlighting, highlight clearing after acceptance, and pointer target submission; and directly affected Movement, offer/value, participant, occupancy, resolver, event-consumer, controller, and scene regressions. The milestone 2.3 presentation correction is T2.

## Basic Ranged hero Attack focused Edit Mode tests

```powershell
$rangedAttackEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
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

Expected result: all discovered cases pass, covering typed Ranged Attack/Range definition shape and offer-derived totals; Range 1 adjacency; inclusive maximum and beyond-Range rejection; explicit axial distance through occupied/obstacle spaces without line of sight; self/friendly/non-unit exclusions; forged/stale/invalid attempts without mutation; shared target-command, ordered Attack events, defender-owned response, restoration, and deterministic boundaries for both range classifications; visible Ranged offer/status/highlight/pointer integration; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.3a is T2.

## Basic Defense response focused Edit Mode tests

```powershell
$defenseResponseEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
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

Expected result: all discovered cases pass, covering exactly one currently legal defender-owned in-hand Defense selection or decline; Hand-to-Discard-Pile mutation with other state facts preserved; distinct reaction invocation context; printed Defense contribution/total only on discard; no total on decline or no legal card; missing, multiple, cross-owner, attacker-selected, discarded, stale, forged, and replayed answer rejection without mutation; restoration at both response and comparison checkpoints; ordered discard/total-or-decline/completion events; visible legal Defense/Decline controls and comparison-pending status; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.4 is T2.

## Basic Attack outcome focused Edit Mode tests

```powershell
$attackOutcomeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.HexCoordinateTests',
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

Expected result: all discovered cases pass, covering equal and greater Defense as successfully defended; lower Defense, decline, and no legal Defense as attack successful; stable same-resolution continuation; comparison revalidation and stale-state rejection without closing the Attack; typed outcome/completion events with actor, target, totals, and Defense source; deterministic replay; visible result text; pending-Attack closure; released card/actor selection; and directly affected Movement, occupancy, participant, value, resolver, controller, and scene regressions. Milestone 2.5 is T2.

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
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests',
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
    '-testFilter', 'GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
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

## Core token properties, Spawn clearance, and Clear replacement tests

Milestone 6.3 is T2 because it crosses token definition/instance, board mutation, action replacement, command/event, turn/card lifecycle, snapshot, replay, and diagnostic boundaries. Its production changes remain pure Core/Application code and add no presentation, scene, asset, or Unity-facing adapter, so no Play Mode run is required.

Run the focused cases first through the cross-platform harness:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore --filter "FullyQualifiedName~ClearActionTests|FullyQualifiedName~TokenAndMarkerComponentTests|FullyQualifiedName~MatchSessionComponentDispatchTests|FullyQualifiedName~DirectorCommandReplayTests"
```

Then run the complete dotnet harness, which must include `SnapshotCompatibilityCorpusTests` and report zero failed, skipped, or inconclusive cases:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore
```

Because `CardAggregateInvarianceTests` is the one Unity-only fixture excluded from that harness, the Windows affected run must include it and the directly affected replacement, session, timing, lifecycle, snapshot, and conformance regressions. Write `tmp/unity-validation-request.json` as follows and invoke only the parameterless launcher from [POLICY.md](POLICY.md#required-agent-entry-point):

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.ClearActionTests;GuardsDigital.Tests.EditMode.TokenAndMarkerComponentTests;GuardsDigital.Tests.EditMode.MatchSessionComponentDispatchTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests;GuardsDigital.Tests.EditMode.CardActionDispatcherCompositionTests;GuardsDigital.Tests.EditMode.Phase4ArchitectureConformanceTests;GuardsDigital.Tests.EditMode.ResolverEmittedCheckpointConformanceTests;GuardsDigital.Tests.EditMode.CardAggregateInvarianceTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests;GuardsDigital.Tests.EditMode.AppendOnlyEventLogTests;GuardsDigital.Tests.EditMode.ActiveEffectLifecycleTests;GuardsDigital.Tests.EditMode.ComponentLifetimeOwnershipConformanceTests;GuardsDigital.Tests.EditMode.EffectSeamConformanceTests;GuardsDigital.Tests.EditMode.MatchSessionSkillDispatchTests;GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.TurnTimingCheckpointOrderTests",
  "results": "tmp/milestone-6-3-affected-results.xml",
  "log": "tmp/milestone-6-3-affected.log"
}
```

Expected result: all cases pass. Coverage proves properties are trusted token-owned continuous sources rather than Active effects; source-card or source-hero absence does not end them; token removal does; Spawn-point clearance removes a covering token before the owning Spawn commit; Clear is a value-free, source-text-suppressing replacement for one current replaceable Attack; any unique adjacent-token subset including none is legal; duplicate, non-adjacent, forged, and stale input rejects before mutation; Clear emits no Attack facts; accepted execution resolves the source card and advances Initiative; and snapshot restore, retained-prefix Undo, replay, diagnostics, compatibility fixtures, and architecture/checkpoint constraints remain deterministic.

Schema remains v9. The synthetic Hazard definition advances from revision 1 to revision 2 so trusted content can own the property declaration. The compatibility-corpus and aggregate-invariance fixtures must first report the exact digest delta, then may be re-pinned only with the explicit content-revision reject decision and unchanged equality groups recorded in source and completion evidence.

## Reusable modifier, restriction, permission, immunity, and effective-characteristic tests

Milestone 6.8 is T2 because it crosses definition content, the Active-effect and continuous-source seams, value composition, targeting, action offers, snapshot registration identity, and replay. Its production changes remain pure Core/Application code and add no presentation, scene, asset, or Unity-facing adapter, so no Play Mode run is required.

Run the focused fixture first through the cross-platform harness:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore --filter "FullyQualifiedName~RuleModificationTests"
```

The Windows run must include `CardAggregateInvarianceTests`, which is the one Unity-only fixture excluded from that harness, together with the value, targeting, offer, timing, component, snapshot, and conformance regressions the contracts reach. Write `tmp/unity-validation-request.json` as follows and invoke only the parameterless launcher from [POLICY.md](POLICY.md#required-agent-entry-point):

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.RuleModificationTests;GuardsDigital.Tests.EditMode.CardAggregateInvarianceTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.ActiveEffectLifecycleTests;GuardsDigital.Tests.EditMode.MinionObstacleAndDefenseTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.TokenAndMarkerComponentTests;GuardsDigital.Tests.EditMode.MatchSessionComponentDispatchTests;GuardsDigital.Tests.EditMode.EffectCapabilityCatalogTests;GuardsDigital.Tests.EditMode.EffectSeamConformanceTests;GuardsDigital.Tests.EditMode.AuthoritativeSnapshotSerializerTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests",
  "results": "tmp/milestone-6-8-affected-results.xml",
  "log": "tmp/milestone-6-8-affected.log"
}
```

Expected result: all cases pass. Coverage proves each declaration family rejects incomplete data; an activity-scoped declaration covers itself or everything and nothing else; a narrow permission leaves unrelated restrictions standing; simultaneous modifications order by revealed Tie-Breaker side, then creation sequence, then identity, without a flip; an immune unit cannot be targeted, is still counted by presence, and is never immune to its own actions; `Counts As` replaces the effective type while intrinsic kind and miniature rules cannot see it; obstacles block transit unless the permission allows it and always block an endpoint; only active effects and placed tokens supply rules; team and global scopes expand to the heroes they name; a Defense total composes printed, minion, Active-effect, and continuous contributions with distinct source provenance; the Fast-Travel offer disappears under a restriction naming it while other offers remain; and a snapshot round-trip reproduces the identical set by rebinding definitions from the trusted catalog.

Schema remains v9. The capability registration list gains the five synthetic modification capabilities and the synthetic Hazard definition advances from revision 2 to revision 3. The compatibility-corpus and aggregate-invariance fixtures must first report the exact digest delta, then may be re-pinned only with the explicit reject decision and unchanged equality groups recorded in source and completion evidence.

## Converted Red Bulwark modifier/restriction/permission Skill tests

Milestone 6.8a is T2 because it changes configured card content every pinned digest measures, and because it adds the first passive presentation surface for a paused effect frame. That presentation change is Unity-facing, so a focused Play Mode run is required alongside Edit Mode.

Run the focused fixtures first through the cross-platform harness:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore --filter "FullyQualifiedName~ConfiguredModificationSkillTests"
```

The Windows run must add the Unity-only fixtures that harness excludes - `CardAggregateInvarianceTests` and `MatchFeaturePresenterTests` - together with the offer, value, targeting, timing, and round regressions the converted card reaches. Write `tmp/unity-validation-request.json` as follows and invoke only the parameterless launcher from [POLICY.md](POLICY.md#required-agent-entry-point):

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.ConfiguredModificationSkillTests;GuardsDigital.Tests.EditMode.CardAggregateInvarianceTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.RuleModificationTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.MinionAttackResolutionTests;GuardsDigital.Tests.EditMode.MinionObstacleAndDefenseTests;GuardsDigital.Tests.EditMode.FastTravelActionTests;GuardsDigital.Tests.EditMode.MatchSessionTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests",
  "results": "tmp/milestone-6-8a-affected-results.xml",
  "log": "tmp/milestone-6-8a-affected.log"
}
```

Then run Play Mode filtered to `GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests`, which drives the converted Skill through the configured scene's passive round view.

Expected result: all cases pass. Coverage proves the card keeps its definition/instance identity, Initiative, printed Secondary Movement, Defense reaction and hand slot while only its printed Primary Attack is replaced; the Skill offers exactly three modes in definition order under an actor-owned `Choose`; each mode instantiates one registered capability of one rule family on the action's authoritative actor with the card as source; `This Turn` `+1 Defense` composes into a later enemy Attack's Defense Total in the same turn with its Active-effect provenance intact; the `This Round` restriction withdraws only that hero's Fast-Travel offer and ends at Round End; the `Next Turn` permission stays dormant during the creating turn, permits transit through an occupied space on the next turn, never permits an obstacle endpoint, and then expires; stale and forged mode answers reject without creating an effect or resolving the card; and snapshot restore, replay, and retained-prefix Undo reproduce the identical payload across the delayed boundary. The presenter forwards only the projected invocations of a paused frame and withholds actions, Fast-Travel offers, and End Turn while it stands.

Schema remains v9. Red Bulwark's definition advances from `phase-5-1a-rev-3` to `phase-6-8a-rev-1`. The compatibility-corpus and aggregate-invariance fixtures must first report the exact digest delta, then may be re-pinned only with the explicit content-revision reject decision and unchanged equality groups recorded in source and completion evidence.

## Move-Through permission Movement consumer tests

Milestone 6.8c is T2 because it changes route commitment and the reachability query every Movement uses, which the whole card-resolution, snapshot, and replay surface sits on. It adds no Presentation, scene, asset, or assembly path, so Play Mode is run to confirm the configured scene still drives Movement and the 6.8a Skill rather than because a Unity-facing file changed.

Run the focused fixtures first through the cross-platform harness:

```powershell
dotnet test Tools/DotnetTests/GuardsDigital.DotnetTests.csproj --no-restore --filter "FullyQualifiedName~MoveThroughPermissionTests|FullyQualifiedName~ConfiguredModificationSkillTests"
```

The Windows run must add the Unity-only fixtures the harness excludes, together with the movement, reachability, resolution, and round regressions route commitment reaches:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.MoveThroughPermissionTests;GuardsDigital.Tests.EditMode.ConfiguredModificationSkillTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardAggregateInvarianceTests;GuardsDigital.Tests.EditMode.SnapshotCompatibilityCorpusTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.DirectorCommandReplayTests",
  "results": "tmp/milestone-6-8c-affected-results.xml",
  "log": "tmp/milestone-6-8c-affected.log"
}
```

Expected result: all cases pass. Coverage proves an ordinary mover is still stopped by an obstacle and everything beyond it stays unreachable; a mover holding the permission reaches past an occupied space but is never offered that space as a destination; passing through costs one movement like any space entered; nothing passes outside the playable board; a permission on another unit does nothing for this mover; a committed through-route leaves the passed-through unit in place, vacates the origin once, and narrates every space entered; a route ending on an obstacle, a through-route without the permission, a route leaving the board partway, and a route beyond the remaining allowance each reject whole without moving anything; and the configured Red Bulwark `Next Turn` mode makes the space beyond an adjacent enemy reachable on that hero's next turn, against a control run that never chooses the mode.

Schema is unchanged and no pinned digest moves: route commitment produces the same events and the same end state as the per-space walk it replaced whenever nothing occupied is crossed.
