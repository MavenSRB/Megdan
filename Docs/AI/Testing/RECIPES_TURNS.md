# Turn, Round, Snapshot, and Replay Test Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md). The raw `Start-Process` blocks in this shard are test-filter and historical-output references only; write `tmp/unity-validation-request.json` and invoke the fixed launcher per [POLICY.md](POLICY.md).

## Card definition/instance focused Edit Mode tests

```powershell
$cardDefinitionInstanceEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-10-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-1-10-edit-tests.log"'
)

$cardDefinitionInstanceEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardDefinitionInstanceEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardDefinitionInstanceEditProcess.ExitCode -ne 0) {
    throw "Card definition/instance Edit Mode tests failed with exit code $($cardDefinitionInstanceEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering stable card definition IDs, content revisions, display names, definition references, validated typed Initiative and synthetic-fixture authority, unequal and tied synthetic Initiative fixtures, distinct card instance IDs, primitive-hero ownership, initial in-hand runtime state, multiple instances sharing one immutable definition, orthogonal state facts, and rejection of blank, duplicate, unresolved, stale, or invalid initial card data.

## Card turn-lifecycle foundation focused Edit Mode tests

```powershell
$cardLifecycleEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-1-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-1-focused.log"'
)

$cardLifecycleEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $cardLifecycleEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($cardLifecycleEditProcess.ExitCode -ne 0) {
    throw "Card lifecycle Edit Mode tests failed with exit code $($cardLifecycleEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering legal Facedown play, reveal, post-action resolution, and Defense discard; rejection without mutation for null, repeated, and out-of-order attempts; orthogonal resolved/discarded/orientation facts; positive typed Initiative validation; unequal and cross-hero tie fixtures; and explicit synthetic-test-fixture authority.

## Match-participant focused Edit Mode tests

```powershell
$matchParticipantArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.HeroOccupancyTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-1-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-1-edit-tests.log"'
)

$matchParticipantProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $matchParticipantArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($matchParticipantProcess.ExitCode -ne 0) {
    throw "Match-participant Edit Mode tests failed with exit code $($matchParticipantProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering nonempty unique team/player/hero IDs, collection-backed references, one hero per player, plural multi-hero team queries, one Red and one Blue test hero, preservation of the stable `primitive-hero` ID, globally unique card instances in separately owned Red/Blue synthetic Movement hands, and the directly affected Phase 1 card/offer/hero identity regressions.

## Tie Breaker setup focused Edit Mode tests

```powershell
$tieBreakerSetupEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TieBreakerSetupTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6-focused-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6-focused.log"'
)

$tieBreakerSetupEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $tieBreakerSetupEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($tieBreakerSetupEditProcess.ExitCode -ne 0) {
    throw "Tie Breaker setup Edit Mode tests failed with exit code $($tieBreakerSetupEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering a normal setup result that is always Red or Blue; exact deterministic internal Red and Blue overrides; undefined-side rejection; immutable value equality suitable for later snapshot reconstruction; authoritative state carried by the synthetic configuration; explicit Tie Breaker selection in unrelated tests; and directly affected participant, offer/value, and Attack-resolution regressions.

## Visible Tie Breaker indicator focused tests

```powershell
$tieBreakerIndicatorEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.MinimalHexGridSceneTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6a-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-2-6a-focused-edit.log"'
)

$tieBreakerIndicatorEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $tieBreakerIndicatorEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($tieBreakerIndicatorEditProcess.ExitCode -ne 0) {
    throw "Visible Tie Breaker indicator Edit Mode tests failed with exit code $($tieBreakerIndicatorEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering deterministic Red and Blue authoritative configurations; explicit `[R] RED` and `[B] BLUE` labels plus distinct side tinting; top-right geometry; pointer blocking over the indicator; unchanged card-driven presentation behavior; and the saved-scene controller path. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command to cover the live scene. Milestone 2.6a corrective T3 evidence passed focused Edit Mode 21/21, focused Play Mode 7/7, complete unfiltered Edit Mode 163/163, and complete unfiltered Play Mode 8/8, all with zero failed, skipped, or inconclusive cases. Readiness-gated Game-view inspection confirmed the solid Red `TIE BREAKER COIN [R] RED` indicator is readable at the top-right without overlapping the board or bottom action HUD.

## Director-mode hands focused tests

```powershell
$directorHandsEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-2-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-2-focused-edit.log"'
)

$directorHandsEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $directorHandsEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($directorHandsEditProcess.ExitCode -ne 0) {
    throw "Director-mode hands Edit Mode tests failed with exit code $($directorHandsEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly two explicit `[R] RED HERO` and `[B] BLUE HERO` hand labels, unique authoritative owner-bound instances for both synthetic hands, each visible card button's definition-backed Initiative label, director selection of either side's card, and selection without a card lifecycle transition or action resolution. A discarded card is no longer projected in its owner's hand or selectable as a hand card. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command to cover the saved-scene projection and the Blue-hand selection path.

## Turn-card commitment and reveal focused tests

```powershell
$turnCardCommitmentEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-3-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-3-focused-edit.log"'
)

$turnCardCommitmentEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $turnCardCommitmentEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($turnCardCommitmentEditProcess.ExitCode -ne 0) {
    throw "Turn-card commitment Edit Mode tests failed with exit code $($turnCardCommitmentEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering exactly one eligible own-hand commitment per Red/Blue side; Facedown played/unresolved mutation; no disclosed card ID before reveal; deterministic Red/Blue reveal events and authoritative Faceup state only after both sides respond; Pass only with no eligible card; missing, cross-side, ineligible, repeated, stale-state, and illegal-pass rejection without further mutation; deterministic replay; generic director commitment/pass labels; definition-backed revealed name/Initiative labels; and unchanged card-action/controller regressions. Run `MinimalHexGridPlayModeTests` with the existing placeholder-grid focused Play Mode command for the saved-scene joint-reveal path.

## Initiative and tied-Initiative focused Edit Mode tests

```powershell
$initiativeEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-4-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-4-focused-edit.log"'
)

$initiativeEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $initiativeEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($initiativeEditProcess.ExitCode -ne 0) {
    throw "Initiative Edit Mode tests failed with exit code $($initiativeEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering card-wide typed printed-Initiative contributions; current highest-unresolved re-query after every resolution; Red and Blue cross-team Tie Breaker selection with a recorded flip deferred until the selected card is authoritatively resolved; same-team pending choices without a flip; multi-hero ties with more than two cards; stale-choice rejection without mutation; deterministic candidate ordering; and unchanged printed action values, lifecycle, commitment, participant ownership, card definitions, and setup-side behavior. Milestone 3.4 is T2 because it introduces shared Initiative/pending-choice/turn-boundary infrastructure. Play Mode is omitted because no Unity-facing or presentation path changes.

## Hold turn-resolution focused Edit Mode tests

```powershell
$holdTurnEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-5-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-5-affected.log"'
)

$holdTurnEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $holdTurnEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($holdTurnEditProcess.ExitCode -ne 0) {
    throw "Hold turn Edit Mode tests failed with exit code $($holdTurnEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering a general-rule Hold offer for any current selected face-up unresolved played card; exactly one normal Hold per selected card; Initiative-ordered resolution of both synthetic cards; resolve-before-deferred-flip-before-re-query ordering; same-team choices without a flip; rejection without mutation for wrong, repeated, or post-turn Hold attempts; defined semantic event payloads; and exactly one turn-only-effect cancellation checkpoint immediately before turn end. Milestone 3.5 is T2 because it introduces shared turn/timing infrastructure.

## In-turn Movement resolution focused Edit Mode tests

```powershell
$turnMovementEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;GuardsDigital.Tests.EditMode.MatchParticipantsTests;GuardsDigital.Tests.EditMode.TieBreakerSetupTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.MovementSessionTests;GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6-affected.log"'
)

$turnMovementEditProcess = Start-Process `
    -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe' `
    -ArgumentList $turnMovementEditArguments `
    -Wait `
    -PassThru `
    -WindowStyle Hidden

if ($turnMovementEditProcess.ExitCode -ne 0) {
    throw "In-turn Movement Edit Mode tests failed with exit code $($turnMovementEditProcess.ExitCode)."
}
```

Expected result: all discovered cases pass, covering offers derived only from the current face-up unresolved played card; delegation to the Phase 1 allowance, cumulative waypoint route, obstacle, adjacent-step, pending-choice, and command/event authorities; retained unresolved card/actor state during partial Movement; rejection without mutation for wrong actor, stale choice, or another normal action while Movement is pending; exact source-card resolution on Stop or allowance exhaustion; and resolve-before-deferred-flip-before-current-Initiative-re-query ordering. Milestone 3.6 is T2 because it composes shared turn and resolver infrastructure.

## Visible in-turn Movement corrective focused tests

```powershell
$visibleTurnMovementEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.DirectorHandPresentationTests;GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-edit-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-edit.log"'
)

$visibleTurnMovementPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-6a-focused-play.log"'
)
```

Run each argument set through the standard `Start-Process -Wait -PassThru` Unity invocation used above. Expected result: all discovered cases pass, covering automatic current-actor/card focus after joint reveal; played-card Movement-only offers; pointer destination and Stop submission through the turn authority; partial waypoint retention and refreshed highlights; exact source-card resolution followed by visible next-actor focus; and stale input preserving the actor, card, pending choice, occupancy, and highlights. Corrective milestone 3.6a is T2 because it changes the validation controller and saved-scene presentation boundary.

## In-turn Attack and Defense focused tests

```powershell
$turnAttackEditArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'EditMode',
    '-testFilter', 'GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;GuardsDigital.Tests.EditMode.MatchFeaturePresenterTests;GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;GuardsDigital.Tests.EditMode.ConfiguredPointerSelectionTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-affected-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-affected.log"'
)

$turnAttackPlayArguments = @(
    '-batchmode',
    '-runTests',
    '-projectPath', '"C:\Projects\Guards Digital\Guards 0.1"',
    '-testPlatform', 'PlayMode',
    '-testFilter', 'GuardsDigital.Tests.PlayMode.MinimalHexGridPlayModeTests',
    '-testResults', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-focused-play-results.xml"',
    '-logFile', '"C:\Projects\Guards Digital\Guards 0.1\tmp\milestone-3-7-focused-play.log"'
)
```

Run each argument set through the standard `Start-Process -Wait -PassThru` Unity invocation used above. Expected result: all discovered cases pass, covering played-card Attack offer revalidation; target, Defense, comparison, and outcome continuation inside the same turn frame; the defender's reaction leaving its played turn card unresolved and actionable; exact attacking-card resolution; resolve-before-deferred-flip-before-current-Initiative-re-query ordering; stale/forged input preservation; visible target/Defense/outcome/next-actor projection; and the saved-scene pointer path. Milestone 3.7 is T2 because it composes the shared turn/resolver/pending-choice boundary and changes the validation controller.

## Five-card synthetic hands focused tests

Use the milestone 3.7 directly affected Edit Mode filter above, adding `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `TurnCardCommitmentAuthorityTests`, `InitiativeResolutionAuthorityTests`, `DirectorHandPresentationTests`, and `MinimalHexGridSceneTests`; write results to `tmp/milestone-3-7b-final-affected-edit-results.xml`. Run `MinimalHexGridPlayModeTests` with the existing saved-scene Play Mode command and write results to `tmp/milestone-3-7b-final-play-results.xml`.

Expected result: all ten synthetic definition/value profiles match the approved matrix; each hero owns five globally unique in-hand instances; every slot can commit and exposes its Primary Attack, Secondary Movement, and general Hold when revealed; the Initiative 11 cross-team tie remains; Defense discard removes only the chosen instance from later commitment/reaction choices; representative non-Ranged and Ranged turn actions resolve; and both five-card hands remain usable in the bounded director HUD. Milestone 3.7b is T2 because executable card data and its shared controller/presentation consumers change.

## Four-turn round skeleton focused Edit Mode tests

Run `RoundResolutionAuthorityTests` alone and write its results/log to `tmp/milestone-3-8-focused-results.xml` and `tmp/milestone-3-8-focused.log`. Then run this directly affected Edit Mode filter and write its outputs to `tmp/milestone-3-8-affected-results.xml` and `tmp/milestone-3-8-affected.log`:

```text
GuardsDigital.Tests.EditMode.RoundResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.TurnResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.InitiativeResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.TurnCardCommitmentAuthorityTests;
GuardsDigital.Tests.EditMode.CardLifecycleAuthorityTests;
GuardsDigital.Tests.EditMode.ActionOfferAndPrintedValueTests;
GuardsDigital.Tests.EditMode.CardDefinitionInstanceTests;
GuardsDigital.Tests.EditMode.MatchParticipantsTests;
GuardsDigital.Tests.EditMode.TieBreakerSetupTests;
GuardsDigital.Tests.EditMode.CardActionResolutionAuthorityTests;
GuardsDigital.Tests.EditMode.MovementSessionTests;
GuardsDigital.Tests.EditMode.MovementReachabilityQueryTests;
GuardsDigital.Tests.EditMode.AdjacentMoveAuthorityTests;
GuardsDigital.Tests.EditMode.ObstacleOccupancyTests;
GuardsDigital.Tests.EditMode.AttackActionResolutionAuthorityTests
```

Pass the semicolon-joined value above as the standard Edit Mode command's `-testFilter`. Expected result: exactly four deterministic indexed turns record immutable Red/Blue card-or-Pass slots; resolved or otherwise ineligible cards cannot replay; Pass is accepted only when no eligible card remains; Hold, Movement, Attack/Defense, pending choices, and deferred Tie Breaker state compose through successive turns; and turn 4 enters an explicit end-of-round phase without retrieving cards or running deferred subsystems. Milestone 3.8 is T2 because it adds shared round/turn/event infrastructure.

## Complete visible round controller focused tests

Run `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, and `RoundResolutionAuthorityTests` together and write their outputs to `tmp/milestone-3-8a-focused-edit-results.xml` and `tmp/milestone-3-8a-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.8 filter above and add `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-8a-affected-edit-results.xml` and `tmp/milestone-3-8a-affected-edit.log`. Run `MinimalHexGridPlayModeTests` through the saved-scene Play Mode command and write outputs to `tmp/milestone-3-8a-focused-play-results.xml` and `tmp/milestone-3-8a-focused-play.log`.

Expected result: the controller retains one authoritative round across hero-focus changes and all four indexed turns; commitment/Pass, reveal, Hold, Movement, Attack/Defense, pending answers, stale rejection, deferred Tie Breaker flips, hand depletion, completed slots, and turn advancement all route through it; and the saved scene visibly projects round/turn/phase plus explicit end-of-round while post-round commands preserve state. Milestone 3.8a is T2 because it changes the shared validation controller and saved-scene presentation boundary but is not the Phase 3 T3 gate.

## Collection-backed round assignments focused Edit Mode tests

Run `RoundResolutionAuthorityTests` alone and write its outputs to `tmp/milestone-3-8b-focused-results.xml` and `tmp/milestone-3-8b-focused.log`. Then run `RoundResolutionAuthorityTests`, `TurnResolutionAuthorityTests`, `InitiativeResolutionAuthorityTests`, `TurnCardCommitmentAuthorityTests`, `CardLifecycleAuthorityTests`, `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `CardDrivenMovementPresentationTests`, and `DirectorHandPresentationTests` together; write outputs to `tmp/milestone-3-8b-affected-results.xml` and `tmp/milestone-3-8b-affected.log`.

Expected result: commitment and Pass are owned by canonical `HeroId` rather than team side; for every configured hero, its controlling player must commit exactly one hero-owned card from a nonempty hand, only an empty hand permits Pass, off-board state does not relax that obligation, invalid in-hand lifecycle state rejects rather than authorizing Pass, and joint reveal waits for every configured hero response. Every turn slot exposes an immutable deterministically ordered collection of ownership-validated `HeroId`/committed-card-or-empty-hand-Pass assignments; controlling `PlayerId` and team derive through `MatchParticipants` and are not duplicated as assignment inputs; malformed one-player-to-one-hero relationships plus unknown, duplicate, missing, and cross-owner entries reject. The staged fixture records exactly two assignments in each of four slots, and Initiative, action, Tie Breaker, event, controller, and end-of-round behavior remains unchanged. Milestone 3.8b is T2 because it changes shared commitment/round/event state immediately before snapshot serialization. Play Mode is omitted unless implementation unexpectedly changes a Unity-facing path; the planned presentation remains a read-only projection. Complete unfiltered suites remain reserved for the Phase 3 gate at milestone 3.11 unless a risk trigger promotes the change.

## Repeated-round and end-of-round progression tests

Run `RoundResolutionAuthorityTests` and `CardLifecycleAuthorityTests` together and write their outputs to `tmp/milestone-3-8c-focused-results.xml` and `tmp/milestone-3-8c-focused.log`. Run `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `RoundResolutionAuthorityTests`, and `CardLifecycleAuthorityTests` together and write their outputs to `tmp/milestone-3-8c-focused-edit-results.xml` and `tmp/milestone-3-8c-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.8 filter above and add `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-8c-affected-edit-results.xml` and `tmp/milestone-3-8c-affected-edit.log`. Run `MinimalHexGridPlayModeTests` through the saved-scene Play Mode command and write outputs to `tmp/milestone-3-8c-focused-play-results.xml` and `tmp/milestone-3-8c-focused-play.log`.

Expected result: one authoritative resumable cursor records the ordered Minion Battle, token/marker cleanup, requested Lane Push, retrieval/cancellation, Level Up/upgrade, and Pity Coin checkpoints. Explicit fixture capability facts produce typed `NotApplicable`; an available stage without a handler rejects; a paused handler preserves its cursor. The populated retrieval stage returns only owner-validated resolved played and Defense-discarded synthetic cards through lifecycle events and emits the Active-effect cancellation checkpoint. Completion freezes immutable slots and terminal outcomes, resets round-local state, retains Tie Breaker, and begins the next commitment. The saved scene completes at least two deterministic rounds with Hold, Movement, Attack/Defense, Defense-driven discard/retrieval, and legal card reuse. Milestone 3.8c is T2 because it changes shared round/event state and the validation controller, but it is not the Phase 3 T3 gate.

## End-of-round Minion Battle and continuation tests

For milestone 6.1, run `MinionBattleResolutionTests` and write outputs to `tmp/milestone-6-1-focused-results.xml` and `tmp/milestone-6-1-focused.log`. Run `SnapshotCompatibilityCorpusTests` and write outputs to `tmp/milestone-6-1-compatibility-results.xml` and `tmp/milestone-6-1-compatibility.log`. For directly affected Edit Mode coverage, run `MinionBattleResolutionTests`, `RoundResolutionAuthorityTests`, `MatchSessionTests`, `DirectorCommandReplayTests`, `AuthoritativeSnapshotSerializerTests`, `SnapshotCompatibilityCorpusTests`, `MinionAttackResolutionTests`, `MatchFeaturePresenterTests`, `MatchFeaturePresenterTests`, and `ActiveEffectLifecycleTests`; write outputs to `tmp/milestone-6-1-affected-results.xml` and `tmp/milestone-6-1-affected.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-6-1-focused-play-results.xml` and `tmp/milestone-6-1-focused-play.log`. Because schema v6 adds durable round-owned pending-choice and Lane-Push request meaning, milestone 6.1 is T3: also run both unfiltered suites and write `tmp/milestone-6-1-complete-edit-results.xml`, `tmp/milestone-6-1-complete-edit.log`, `tmp/milestone-6-1-complete-play-results.xml`, and `tmp/milestone-6-1-complete-play.log`.

For milestone 6.1a's player-facing prompt, run `MinionChoicePresentationTests`, `MatchFeaturePresenterTests`, `Phase4ArchitectureConformanceTests`, `StandardMinionViewTests`, `ConfiguredMatchCutoverArchitectureTests`, `MinimalHexGridSceneTests`, `ApplicationArchitectureBoundaryTests`, `ConfiguredPointerSelectionTests`, `DirectorHandPresentationTests`, `MinionBattleResolutionTests`, and `MatchSessionDiagnosticCostTests`; write outputs to `tmp/milestone-6-1a-focused-edit-results.xml` and `tmp/milestone-6-1a-focused-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-6-1a-configured-play-results.xml` and `tmp/milestone-6-1a-configured-play.log`. Because the prompt is a visible result, repeat that same filter once with `"batchMode": false` to `tmp/milestone-6-1a-visual-play-results.xml` and `tmp/milestone-6-1a-visual-play.log`; the removal case then captures `tmp/milestone-6-1a-minion-removal-prompt.png` for the required targeted Game View inspection. Milestone 6.1a is T2: it changes only the Unity-facing presentation adapters over the unchanged 6.1 authority, so the unfiltered suites are not required.

Expected result for 6.1a: the prompt names the chooser and required count, offers exactly the candidates the pending choice currently makes legal, withholds a control from a Heavy the authority has not yet offered, forwards only the projected select/deselect/confirm invocations, and exposes confirmation only at the exact required count. A refused or stale control changes no authoritative state and rebuilds the visible selection from the returned projection; restore, replay, and Undo reconstruct an equivalent prompt that drives the same continuation. The board marks selected and pointed-at candidates and clears them when the choice completes, and the configured scene renders and forwards all of it through the passive views without touching the session. Milestone 6.1ao reuses this same pair of runs: both cue kinds must draw the identical marker at the declared choice character size and differ only in color, with the backing proportions, orientation correction, and cue lifetime unchanged.

For milestone 6.1u's dead-code removal and shared resolution lookup, run `MinionBattleResolutionTests`, `RoundResolutionAuthorityTests`, `CardActionResolutionAuthorityTests`, `AttackActionResolutionAuthorityTests`, `MovementSessionTests`, `MovementReachabilityQueryTests`, `ActionOfferAndPrintedValueTests`, `MatchSessionTests`, `ConfiguredPointerSelectionTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-6-1u-focused-edit-results.xml` and `tmp/milestone-6-1u-focused-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-6-1u-configured-play-results.xml` and `tmp/milestone-6-1u-configured-play.log`, because the deletion removes a Unity-facing view method and a Play Mode fixture. Milestone 6.1u is T2 and produces no visible result, so neither the unfiltered suites nor a non-batch capture is required.

Expected result for 6.1u: nothing the deletions touched had a production caller, so every previously green case stays green with only the removed fixtures missing from the totals. Advancing end of round with a recorded Lane-Push request rejects with `UnimplementedLanePushConsumption`, leaves the cursor on `ResolveRequestedLanePush`, emits no event, and keeps the request; `MissingRequiredEndOfRoundHandler` survives for its own cause, an end-of-round stage that is available with no handler registered. One shared `ActionOfferQuery.FindActionDefinition` answers all three resolution backends, proven through a real caller because it is internal: a card carrying both a Movement and an Attack action resolves each offer to its own action definition, so starting Movement from the Attack offer rejects with `UnsupportedActionKind`.

Expected result: a configured full-map Minion Battle completes ties without effect, removes the exact difference or all remaining losing minions, auto-resolves only a forced final set, and otherwise pauses on a round-owned typed losing-team choice. Select/deselect progresses one minion at a time, never makes Heavy legal before all required earlier non-Heavy removals, rejects stale/wrong-team/duplicate/over-count/under-count/ineligible commands atomically, and confirms exactly once through ordered non-Heavy-before-Heavy removal events. Losing the last minion records but does not consume an end-of-round Lane-Push request before cleanup. Projection, schema-v6 snapshot restore, the five-fixture compatibility corpus, fresh replay, and retained-prefix Undo rebuild the exact selected continuation byte-identically. Reduced synthetic maps without a declared Battle Zone and explicit custom stage-handler fixtures retain their established unavailable/handler behavior.

## Player-facing HUD tests

For milestone 7.8a's card tray, hero overviews, `DIRECTOR` drawer, and paused-transition prompt, run `MatchHudPresentationTests`, `MatchFeaturePresenterTests`, `ConfiguredMatchCutoverArchitectureTests`, `EndOfRoundLevelUpStageTests`, `MinimalHexGridSceneTests`, `ApplicationArchitectureBoundaryTests`, `Phase4ArchitectureConformanceTests`, `MinionChoicePresentationTests`, `ClearPresentationTests`, and `DirectorHandPresentationTests`; write outputs to `tmp/milestone-7-8a-focused-edit-results.xml` and `tmp/milestone-7-8a-focused-edit.log`. Run `MinimalHexGridPlayModeTests` to `tmp/milestone-7-8a-configured-play-results.xml` and `tmp/milestone-7-8a-configured-play.log`, because the saved scene now builds a runtime UI Toolkit panel and that is the only tier a live panel exists in. Repeat the same Play Mode filter once with `"batchMode": false` to `tmp/milestone-7-8a-visual-play-results.xml` and `tmp/milestone-7-8a-visual-play.log`; it captures `tmp/milestone-7-8a-standing-hud.png`, `tmp/milestone-7-8a-director-drawer.png`, and `tmp/milestone-7-8a-leveling-prompt.png` for the required Game View inspection. Milestone 7.8a is T2: it changes the Unity-facing presentation adapters plus two read-only projections over unchanged rules, and adds no schema, command, or legality.

For milestone 7.8a1's decided Attack availability, run `AttackOfferAvailabilityTests`, `DashboardSlotsAndComposedItemTotalsTests`, `MatchHudPresentationTests`, `ClearPresentationTests`, `ActionOfferAndPrintedValueTests`, `TurnResolutionAuthorityTests`, and `AttackActionResolutionAuthorityTests`. It is nonetheless a **both-unfiltered-suites** milestone rather than a focused one, and the reason is recorded rather than inferred: `IsAvailable` is consulted by every consumer of the offer set, and the first complete Edit Mode run found ten `ClearPresentationTests` failures that no focused filter built from the changed types would have selected and that the cross-platform harness cannot see at all. Write outputs to `tmp/milestone-7-8a1-complete-edit-results.xml` and `tmp/milestone-7-8a1-complete-play-results.xml` with matching logs. No non-batch capture is required: the greyed control is milestone 6.5af's existing treatment and nothing new is drawn.

Expected result for 7.8a1: an Attack whose legal-target set is empty is projected unavailable and drawn among the greyed controls carrying no command, while an Attack with a target is unchanged and every non-Attack offer is unchanged. The offer flag equals what the start would find, in both a position with a target and one without. The start still refuses an Attack with no target, atomically, leaving no pending resolution and the turn active. A p.16 Range item makes an otherwise unavailable Ranged Attack available by exactly one space and no further, and leaves a non-Ranged Attack alone. The Clear that replaces an unavailable Attack is still offered, because p.13 gives it its own legality. Restore and retained-prefix Undo re-derive the same availability rather than carrying it on the wire.

Note that a non-batch run leaves the Editor open, so the next launcher invocation refuses until it exits; that is the serial-process rule working rather than a fault.

Expected result: every current-round card is drawn from its own projected `CardColor` with the colour's name beside it, and each of the two hands holds exactly one card of the colour that shares its team's name, which a colour derived from team could not produce. The tray sorts played cards in ascending turn order, then available, then discarded, labels a played card `PLAYED · TURN <n>` and never a round, reads the three groups from projected card lifecycle and the round's own assignment history, and drops every played entry at a round boundary. The viewed hero follows the hero the match is waiting on, a map selection pins another until the awaited hero changes, a hero this match does not have changes nothing, and no selection submits a command. Each overview carries the projected name, Level, four turn slots taking played colours with the ordinal and colour only in the assistive label, projected item counts as signed chips, and the projected discard count and colours. `UNDO`, `RESET`, and `EVENT LOG` forward the existing operations, and opening or closing the drawer changes no match state. A paused p.15 upgrade is drawn with its hero, level, paid cost, remaining coins, and one control per projected option naming the card returned, the card taken, the sibling set aside, and the item icon; a p.8 blocked-Spawn Place names its destination space. An option the prompt did not project is refused before the authority sees it, the same option twice does nothing the second time, and neither End Turn nor the end-of-round advance is drawn beside a pause. Retained-prefix Undo and snapshot restore both redraw the same colours, slots, and discard counts from current authority. Both validated viewports keep every region inside the screen with the status strip clear of the tray and the two rails clear of each other.

## Authoritative snapshot serialization and restore tests

Run `AuthoritativeSnapshotSerializerTests` and `CardDrivenMovementPresentationTests` together and write outputs to `tmp/milestone-3-9-focused-results.xml` and `tmp/milestone-3-9-focused.log`. For directly affected Edit Mode coverage, run `AuthoritativeSnapshotSerializerTests`, `RoundResolutionAuthorityTests`, `TurnResolutionAuthorityTests`, `InitiativeResolutionAuthorityTests`, `TurnCardCommitmentAuthorityTests`, `CardLifecycleAuthorityTests`, `CardDefinitionInstanceTests`, `MatchParticipantsTests`, `CardActionResolutionAuthorityTests`, `AttackActionResolutionAuthorityTests`, `CardDrivenMovementPresentationTests`, `DirectorHandPresentationTests`, `HeroSelectionControllerTests`, and `MinimalHexGridSceneTests`; write outputs to `tmp/milestone-3-9-affected-results.xml` and `tmp/milestone-3-9-affected.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-9-play-results.xml` and `tmp/milestone-3-9-play.log`.

Expected result: deterministic schema-versioned round trips preserve participants, the enforced one-player/one-hero relationship, Tie Breaker/Initiative state, current and completed collection-backed round assignments, end-of-round cursor/outcomes, board/obstacle occupancy, definition references, every card-state facet, partial commitments, and pending Movement or Attack/Defense continuations. Unchanged restored state serializes identically; schema and definition content-revision mismatches reject with distinct reasons. Resumed target and Defense commands produce the same ordered events, and the validation controller rebuilds restored hands, occupancies, pending highlights, and status from authority.

### Required selection on a schema-version change

Two fixtures pin SHA-256 digests of serialized authoritative state, and a change to
`AuthoritativeSnapshotSerializer.CurrentSchemaVersion` invalidates every one of them by construction,
because the version is a serialized field:

- `SnapshotCompatibilityCorpusTests` - the five versioned compatibility fixtures.
- `CardAggregateInvarianceTests` - nine scenario digest pairs covering accepted, rejected, stale,
  restored, replayed, undo-truncated, and the three defense-discard cases.

Any milestone changing the schema version must run both. Neither is optional and neither can be
inferred from a filter built by naming the types a change touches: `CardAggregateInvarianceTests`
sits in the dotnet harness exclude list, so the cross-platform harness cannot reach it, and it
names none of the types a schema change edits, so an enumerated focused filter will not select it;
`SnapshotCompatibilityCorpusTests` runs in the harness but must still be selected deliberately.
Milestone 6.1s hit exactly that gap - it re-pinned the corpus fixtures, closed as complete, and left
the invariance fixture failing on `main` until 6.1x found it with the complete Edit Mode suite.

When a digest moves, explain the delta before re-pinning. `CardAggregateInvarianceTests` carries a
"do not update to match a failing run" warning for the reason 6.1x recorded: a re-pin that cannot
account for its own bytes is indistinguishable from pinning over a regression. Decode the payloads
either side of the change and attribute every differing byte.

`SnapshotEnumReaderTests` pins no digest but belongs in the same route, because it owns the decode
boundary the whole payload passes through: it drives every enum the payload carries against its
declared members and against out-of-domain values. Run it whenever the serializer, an enum it reads,
or `SnapshotEnumReader` itself changes. Adding a member to a payload enum is exactly the case it
covers - the new member must read back, and the next value up must still be refused. The
complementary half is not a test at all: code-truth check 6 in the living-document validator fails
on any raw `(SomeEnum)reader.Read…` cast left under `Core/Snapshots/`, and it runs on a host where
no Unity fixture can.

## Deterministic command-replay Director Undo corrective tests

For milestone 3.10a, run `MatchFeaturePresenterTests` plus the new pure command-history/replay fixture and `AuthoritativeSnapshotSerializerTests` together; write outputs to `tmp/milestone-3-10a-focused-edit-results.xml` and `tmp/milestone-3-10a-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter above plus every new command-envelope, initial-scenario, replay, and event-log fixture; write outputs to `tmp/milestone-3-10a-affected-edit-results.xml` and `tmp/milestone-3-10a-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-10a-focused-play-results.xml` and `tmp/milestone-3-10a-focused-play.log`.

Expected result: accepted typed commands and explicit choices append exactly once; rejected and presentation-only inputs append nothing. Reconstructing from immutable initial scenario inputs and replaying every retained command prefix through normal authority paths produces the same serialized authoritative state and ordered event projection as live execution across commitment/reveal, Initiative, Hold, multi-step Movement/Stop, Attack target/Defense, end-of-round continuation/retrieval, and repeated-round entry. Undo truncates one command and atomically exposes the reconstructed prefix; restart clears the command log and reconstructs setup. Replay rejection or a deterministic state/event mismatch exposes no partial rebuild and reports a clear failure. No per-command authoritative snapshot is retained, while standalone snapshot capture/restore regressions remain green.

## Internal rule-event log and Phase 3 T3 gate

Run `CardDrivenMovementPresentationTests` and `AuthoritativeSnapshotSerializerTests` together and write outputs to `tmp/milestone-3-11-focused-edit-final-results.xml` and `tmp/milestone-3-11-focused-edit-final.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter and add the current controller/presentation consumers; write outputs to `tmp/milestone-3-11-affected-edit-results.xml` and `tmp/milestone-3-11-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-11-focused-play-results.xml` and `tmp/milestone-3-11-focused-play.log`. Finally run both unfiltered suites through the fixed launcher with empty filters and write outputs to `tmp/milestone-3-11-complete-edit-results.xml`, `tmp/milestone-3-11-complete-edit.log`, `tmp/milestone-3-11-complete-play-results.xml`, and `tmp/milestone-3-11-complete-play.log`.

Expected result: accepted ordered setup, commitment, Initiative, turn, action, and end-of-round events project to contiguous immutable rows containing semantic type, stable rules-object references, and diagnostic values. Facedown commitment rows do not reveal card identity; rejected commands remain outside the accepted log. Snapshot restore and Undo reproduce the same projection, restart returns to setup rows, expanding/collapsing the panel does not alter the authoritative snapshot, and expanded bounds block board input.