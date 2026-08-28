# Phase 8 - Hands-on corrections before private online work

Canonical active roadmap shard for player-reported UI and rules-offer defects found after milestone 8.29a and before private-online implementation resumes. Return to [ROADMAP.md](../ROADMAP.md) or the surrounding [Phase 8 route](PHASE_8.md).

Documentation of these corrections is approved; implementation is not. They remain separately approvable micro-milestones because their implementation and closing checks have independent boundaries.

### [ ] 8.29b Stabilize hero selection and recompose Level Up

Proposed at the user's direction on 2026-08-29 after hands-on inspection of the completed 8.29a plaques and the existing Level Up prompt. Documentation of this correction is approved; implementation is not yet approved, and additional newly reported presentation defects may amend this definition or be split into a separate corrective milestone before implementation begins.

Observable outcome: Selecting a different hero never moves any of the six side plaques, and p.15's six-card Level Up decision becomes a contained, legible comparison surface that fits the current enlarged-card HUD instead of expanding the obsolete two-row tray across nearly the whole play area.

Acceptance checks:

- Every hero plaque reserves one identical top-outline width whether viewed or unviewed. Selection changes only the reserved outline's colour; after layout settles, changing the viewed hero leaves the `x`, `y`, `width`, and `height` of all six plaque bounds unchanged.
- The six Level Up options form three visually explicit source-card pairs in Blue, Green, Red reading order, with each pair's two projected candidates kept in the order supplied by the pause. Presentation groups the existing options but invents no eligibility, pairing, command, or result.
- All six candidates remain complete `CardFootprint` faces and fit in one full-width comparison band at the 2560x1440 reference Game View and the validated 2048x1152 desktop view. The prompt exposes no horizontal scrollbar, clips no card or option fact, and does not shrink, crop, or substitute a compact card design.
- Opening or closing Level Up does not relocate either hero column or recompute the board safe surface around the legacy near-full-height context. A deliberately short supported viewport may use bounded vertical scrolling without horizontal travel or card resizing.
- Hero, level, paid and remaining coins, returned card, taken card, set-aside sibling, gained item, tooltips, assistive readings, pointer highlighting, keyboard order, and each option's exact projected invocation retain their existing meanings and remain reachable. The ordinary hand and unrelated match commands remain unavailable while the mandatory decision stands.
- Focused Edit Mode presentation tests and focused Play Mode live-geometry/input tests pass, followed by targeted non-batch Level Up inspection at 2560x1440 and 2048x1152.

Closing checks: `ChangingTheViewedHero_ChangesOnlyItsReservedOutlineWithoutMovingAnyPlaque` is recorded failing at the pre-fix revision where the selected plaque alone gains one unit of top border and passing after the outline width is permanently reserved. `TheSixUpgradeChoices_AreThreeContainedPairsInOneFullWidthComparisonBand` is recorded failing against the legacy two-row/tall-context composition and passing after the Level Up recomposition; the live Game View check closes containment and standing-HUD stability at both validated desktop sizes.

Non-goals: No rule, progression, coin, item, option-projection, command, serialization, board-state, card-face, ordinary-hand, networking, animation, or imported-art change; no general production-HUD or accessibility redesign.

### [ ] 8.29c Restore the Director Undo player path

Proposed at the user's direction on 2026-08-29 after hands-on play found that the visible Director Undo control no longer undoes. Documentation of this correction is approved; implementation is not yet approved. The authoritative retained-prefix operation remains separately covered, so this milestone owns the real pointer-to-authority-to-refresh journey and must not assume whether the defect is hit routing, enablement, callback wiring, rejection, or refresh until its pre-fix reproduction identifies that boundary.

Observable outcome: When accepted command history exists, a player can open the Director drawer, activate Undo through the rendered HUD, and see exactly the latest accepted command removed with every standing surface refreshed to the reconstructed retained-prefix state.

Acceptance checks:

- A focused live-scene reproduction reaches the Director drawer and activates Undo with a real pointer event rather than invoking the runtime or view callback directly. The control remains reachable with three hero plaques in its rail and while the diagnostics/event-log overlay has been opened and closed.
- Undo is enabled exactly when the live session has an undoable accepted-command prefix. One activation removes exactly one accepted command, reconstructs the same authoritative snapshot as replaying the retained prefix, and does not duplicate, skip, or partially apply presentation work.
- Board units and spaces, hero plaques, card tray, status/action region, pending decision, diagnostics, and every other standing projection refresh from the accepted Undo result without requiring a hero reselection, drawer reopen, or second click.
- An unavailable or rejected Undo is not presented as a successful silent click: the control's disabled state or bounded Director status explains the current authority result without exposing hidden state. Restart and event-log controls retain their independent behavior.
- Restore, replay, restart, and direct authoritative Undo semantics remain unchanged unless the failing reproduction demonstrates a defect in that shared authority boundary; any such expansion requires the milestone definition to be amended before implementation.
- Focused Edit Mode presenter/session regressions and a focused Play Mode pointer-and-refresh journey pass, followed by targeted non-batch inspection at 2560x1440 and 2048x1152.

Closing check: `ClickingDirectorUndo_UndoesExactlyOneAcceptedCommandAndRefreshesTheLiveHud` is recorded failing at the pre-fix revision through the real rendered control and passing after correction, while its retained-prefix snapshot oracle remains identical to direct session replay.

Non-goals: No change to which accepted commands are undoable, no multi-step history browser, redo, confirmation dialog, networking, rule, snapshot-schema, event-log redesign, hero-plaque redesign, or general production-HUD/accessibility work.

### [ ] 8.29d Grey attacks whose mandatory card text cannot establish a target

Proposed at the user's direction on 2026-08-29 after Brogan's Red charge Attack remained actionable without a valid charge target; code inspection confirmed that target-by-card-text Attacks deliberately bypass the availability probe. Documentation of this correction is approved; implementation is not yet approved. The correction is shared and source-driven: Brogan and Tigerclaw are required examples, not card-name exceptions.

Observable outcome: An Attack whose mandatory ordered card text must first move, choose, or otherwise bind its eventual Attack target is offered only when at least one complete legal text-to-target continuation currently exists.

Acceptance checks:

- The action-offer authority uses typed effect/query feasibility to evaluate every configured Attack whose target is bound by card text. It removes the existing unconditional availability bypass and does not parse printed text or branch on a card or hero identity.
- Brogan's mandatory straight-line charge Attacks are unavailable when no legal move endpoint has the required enemy target and become available when any legal endpoint-target continuation exists. Tigerclaw's analogous move-then-target Attack receives the same empty/non-empty boundary coverage.
- Modal or multi-branch target-binding text remains available when any legal branch can complete its mandatory target binding and is unavailable only when none can. An optional step is not mistaken for a mandatory prerequisite.
- Once an offered Attack is chosen, the normal command remains authoritative: intervening state changes, restore, replay, and Undo rederive feasibility, and a stale or now-impossible continuation rejects or stops atomically under the existing resolution contract without partial mutation.
- Clear and every other independently legal turn action retain their own availability. No Attack value, range, movement distance, targeting relation, timing, or card wording changes.
- Focused Core Edit Mode regressions cover the shared availability boundary and the configured Brogan/Tigerclaw examples; a focused Play Mode check proves the unavailable action is visibly disabled and cannot be started through the rendered card.

Closing check: `AnAttackWhoseMandatoryTextMustBindItsTarget_IsUnavailableWhenNoBindingCanBeCompleted` is recorded failing at the pre-fix revision because the target-by-card-text policy returns available without probing its mandatory text, then passing with empty and non-empty legal-continuation fixtures. Configured Brogan and Tigerclaw rows close the player-facing regression without introducing content-specific authority.

Non-goals: No card wording, rule interpretation, balancing, Attack-resolution order, Defense, Defeat, board geometry, generic movement legality, card-content redesign, hero-specific rule branch, networking, animation, or broader action-menu redesign.

### [ ] 8.29e Stop offering impossible timed optional effects

Proposed at the user's direction on 2026-08-29 after before/after-Attack effects offered choices with no valid target, with Arien observed as an additional example. Code inspection confirmed that an ordinary optional effect node pauses for confirm-or-decline before consulting the existing pure feasibility probe. Documentation of this correction is approved; implementation is not yet approved.

Observable outcome: Optional before-Attack, after-Attack, and before-action card-text branches never pause or expose an enabled choice when their governed effect has no legal candidate or endpoint, while the underlying action continues whenever its mandatory requirements remain legal.

Acceptance checks:

- The shared optional-control resolution path consults typed, side-effect-free effect feasibility before creating a confirm-or-decline pause. A provably impossible optional branch resolves as declined/skipped without a player prompt; a performable branch preserves its existing prompt, choices, timing, and result.
- Arien's configured before-Attack movement and adjacent-unit after-Attack displacement examples receive explicit no-candidate and candidate-present regressions. The Attack itself remains available when its ordinary target is legal even if the optional branch is not.
- A finite source-linked matrix covers every configured `BeforeTheAttack`, `AfterTheAttack`, and before-action trigger root in the six-hero roster that can offer an optional target, unit, space, repeat, or endpoint. The matrix proves the empty/non-empty boundary and records any surface whose existing capability-level anticipation already satisfies it.
- Mandatory text, `if able`, `up to`, Repeat, Choose, and nested optional semantics retain their rulebook distinctions and ordered timing. The change neither suppresses a legal choice nor turns optional text into an action prerequisite.
- Pending-decision projections, exact command objects, HUD controls, board cues, deterministic drivers, restore, replay, and Undo agree on whether the optional branch exists. Answers are still revalidated at acceptance and impossible/stale answers cannot mutate state.
- Focused Core Edit Mode effect-session and configured-content regressions pass, plus a focused Play Mode journey showing that Arien's impossible timed option creates no dead prompt while an otherwise legal Attack completes.

Closing check: `AnOptionalStepWhoseBranchCannotApply_CompletesWithoutOfferingAConfirmCommand` is recorded failing at the pre-fix revision where the optional node always pauses and passing after the feasibility gate. `ConfiguredTimedOptionalEffects_OfferOnlyPerformableBranches` closes the roster matrix, including Arien's observed before/after-Attack surfaces.

Non-goals: No change to printed card text, Attack legality unrelated to optional text, rules timing, target queries, movement/displacement legality, triggered-effect limits, card balance, hero-specific rule branch, networking, animation, or general prompt redesign.
