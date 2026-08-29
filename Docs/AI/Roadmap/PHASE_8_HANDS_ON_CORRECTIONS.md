# Phase 8 - Hands-on corrections before private online work

Canonical active roadmap shard for player-reported UI and rules-offer defects found after milestone 8.29a and before private-online implementation resumes. Return to [ROADMAP.md](../ROADMAP.md) or the surrounding [Phase 8 route](PHASE_8.md).

These corrections remain separately approvable micro-milestones because their implementation and closing checks have independent boundaries. Milestones 8.29b, 8.29b1 and correctives 8.29b1a-8.29b1b, 8.29d and 8.29e are complete; 8.29c is deferred and 8.29f remains proposed.

### [x] 8.29b Stabilize hero selection and recompose Level Up

Approved and completed at the user's direction on 2026-08-29 after hands-on inspection of the completed 8.29a plaques and the existing Level Up prompt.

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

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829b-stabilize-hero-selection-and-recompose-level-up-completion-evidence).

### [x] 8.29b1 Select printed actions directly on the resolving card

Approved and completed at the user's direction on 2026-08-29 after the completed enlarged-card HUD still presented ordinary action selection as a row of debug-like text buttons. The already emphasized, fully exposed resolving card is now the ordinary printed-action surface, with luminous boxes over the exact printed regions and compact replacement chips attached to the action they replace. Corrective 8.29b1a closed the stale rail guards exposed during affected validation; the final 134 Edit Mode, 165 Play Mode, focused and two-resolution non-batch checks are green.

Observable outcome: During ordinary card resolution, a player starts each legal printed Primary or Secondary action by clicking or focusing the corresponding highlighted region on the resolving card itself; the separate action-button row is absent, while non-card controls such as End Turn remain in the contextual dock.

Acceptance checks:

- The action-selection projection identifies each available and unavailable printed action by source card instance, action definition, invocation context and exact typed invocation where one exists. The card view binds `PrintedPrimary` to the fixed primary-action band and each ordered Secondary action to its matching fixed icon/value region; it never reads artwork pixels, parses printed text, switches on a card or hero name, or recomputes legality.
- Only the fully exposed card currently resolving receives action regions, and only while ordinary action selection stands. Before reveal, the whole available card retains its existing commitment selection; played history, discarded cards, previews, Defense choices, effect-card choices and Level Up candidates gain no action hotspots.
- Every legal printed region carries a contained luminous outline visible without hover, becomes brighter on pointer hover or keyboard focus, and includes action kind, value, Primary/Secondary context and card-text applicability in its assistive reading. Shape and words preserve meaning without colour alone; the overlay changes no `CardFootprint` geometry and neither crops nor modifies prepared artwork or the generated fallback.
- Activating a legal region forwards exactly its standing projected invocation once through the existing presenter/session path. An unavailable printed action remains visibly located on the card with a subdued inert region and no command; stale, forged, mismatched-card and mismatched-region activations execute nothing.
- A projected Clear offer appears as a compact replacement chip attached to the exact Attack region it replaces, and a projected Fast Travel offer appears on the exact Movement region it replaces. Each chip carries only its existing typed offer and availability; presentation derives no replacement eligibility, destinations, candidates or result. Activating the printed region still means the printed action rather than silently choosing its replacement.
- End Turn remains the one external ordinary turn-ending control because Hold and Movement Stop are presentation meanings rather than printed card regions. Continue End of Round, initiative ties, mandatory decisions, Director controls and headless access retain their existing surfaces, and no duplicate ordinary printed-action button remains in the dock.
- Pointer and keyboard order follow the card's visible action anatomy, with replacement chips immediately after the region they qualify. Focused Edit Mode projection/view tests and focused Play Mode pointer journeys cover Primary, Secondary, unavailable, Clear and Fast Travel paths, followed by targeted non-batch Game View inspection at 2560x1440 and 2048x1152.

Closing checks: `TheResolvingCardsPrintedActionRegions_ForwardOnlyTheirExactProjectedOffers` is recorded failing at the pre-fix revision where the resolving face owns no action controls and passing when its fixed typed regions forward the matching offers. `OrdinaryPrintedActions_AppearOnTheResolvingCardWithoutADuplicateDockRow` closes the live pointer, focus, replacement, containment and one-surface behavior across prepared-art and generated-fallback fixtures.

Non-goals: No rule, action availability, printed value, card-text application, replacement, targeting, command, serialization, replay, Undo, card definition, source artwork, card-footprint, ordinary commitment, Defense, Level Up, effect-choice, networking, animation or general production-HUD redesign; no hotspot derived from private image analysis and no removal of End Turn or Director/headless controls.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829b1-select-printed-actions-directly-on-the-resolving-card-completion-evidence).

### [x] 8.29b1b Align silent printed-action highlights and preserve them on hover

Approved and completed at the user's direction on 2026-08-29 after hands-on prepared-card inspection showed that 8.29b1's text-heavy regions were aligned to the generated fallback's upper bands rather than the official front's printed anatomy, and that hovering the resolving card raised a standalone preview without its action surface. Further hands-on passes rejected the intermediate crop, invented vector symbols and source-traced symbol masks. The user selected a deliberately simple temporary treatment: a glowing circle around Movement's printed boot/value badge and one large glowing box around the complete Primary Skill or Attack section.

Observable outcome: A resolving prepared front places a luminous circle around a legal printed Movement boot/value badge and a broad luminous box around a legal Primary Skill or Attack section, with no redundant action words; hovering that already exposed card never covers or removes those highlights.

Acceptance checks:

- Prepared fronts map typed Primary Skill and Attack offers to their broad lower printed-action panels, while Primary or Secondary Movement maps to the compact boot/value badge. The mapping uses shared 372-by-520 source-ratio template geometry plus invocation/action kind, never a card/hero name, runtime pixel reading, OCR, or printed-text parsing.
- The generated fallback retains its own generated Primary/Secondary anatomy rather than inheriting prepared-front coordinates. Both branches keep the exact `CardFootprint` and every region inside it at 2560x1440 and 2048x1152.
- Printed-action controls render with an empty visible label and transparent hit surface: the artwork supplies the meaning, while tooltip/assistive text retains action kind, value, context, applicability, and availability. An available prepared Movement action owns one centred circular glow around its boot/value badge, including a warm translucent inner light; an available Primary Skill or Attack owns one broad rounded glow box around its complete printed section. Each glow layers a faint wide halo, amber bloom and near-white core. These are transparent UI geometry only - no copied card pixels, replacement glyphs, texture masks or runtime image analysis. Pointer hover or keyboard focus strengthens the same stable shape without moving or resizing it. The generated fallback retains its contained luminous outline because it has no prepared artwork anatomy.
- Fast Travel and Clear keep their necessary compact labels and exact typed offers: Fast Travel remains beside the Movement circle, while Clear sits immediately above and right-aligned to the prepared Attack box rather than inside it. Activating the silent printed region still starts only its printed action.
- The already exposed resolving card does not create the ordinary compact-hand standalone hover preview. Its region objects, glow, availability, invocation, pointer behavior, and keyboard order remain live across real pointer enter/leave; hovering other hand cards retains their existing preview behavior.
- Focused Edit Mode view/projection checks and Play Mode prepared/generated pointer journeys pass, followed by targeted non-batch prepared-front inspection at 2560x1440 and 2048x1152.

Closing checks: `PreparedArtworkPrintedActionRegions_AreSilentAndUseTheOfficialTypedSlots` is recorded failing while the controls still carry action text and the fallback's upper-band coordinates, then passing with separate prepared/generated geometry. `HoveringTheResolvingCard_PreservesItsLivePrintedActionSurface` is recorded failing while the standalone preview covers the overlay and passing when the resolving card suppresses only that redundant preview.

Non-goals: No rule, action offer, availability, printed value, replacement eligibility, command, card definition, source-artwork or import mutation, imported resource or derived private image asset, card footprint, ordinary commitment, Defense/Level Up/effect choice, other-card hover preview, networking, animation, or general HUD redesign; no per-card geometry table, texture mask, runtime pixel analysis, OCR or printed-text parsing.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829b1b-align-silent-printed-action-highlights-and-preserve-them-on-hover-completion-evidence).

### [x] 8.29b1a Reconcile rail geometry guards after the approved plaque changes

Approved at the user's direction on 2026-08-29 after 8.29b1 affected validation found three repeatable failures in two standing rail guards. The failures concern test measurement assumptions introduced before the approved 8.29a medallion and 8.29b reserved-outline geometry; the printed-action implementation does not own or change either rail.

Observable outcome: The standing rail guards measure the intended content geometry and the accepted post-8.29a/8.29b plaque baseline without masking later growth. No production HUD, layout, rule or action behavior changes.

Acceptance checks:

- The shared coin guard excludes the bonus strip's decorative top and bottom borders before comparing the content line with the taller of its item chips and coin, and compares the declared rail width within the panel's explicit one-pixel snapping allowance. It continues to fail if content height or width grows beyond those bounds; both Wasp coin-rail journeys pass at their validated viewports.
- The held-marker rail ceiling is rebaselined only to the measured accepted plaque geometry after 8.29a and 8.29b. The independent `ThreeUpBudget` assertion remains unchanged and continues to prove that three rails fit the standing rail rect.
- The exact three previously failing Play Mode journeys pass together, followed by the directly affected rail journeys and documentation/source validation.

Closing checks: `TheCoinBalance_MovesOnTheRailOfTheHeroTheDefeatPaidAndOnNoOther`, `Scene_KeepsEveryRailsCoinCountInsideItsRailAtBothValidatedViewports`, and `APoisonedHero_ReadsTheMarkerAndItsThreeLostValuesOnTheirOwnRail` are recorded failing in the pre-correction 8.29b1 candidate based on `f35bd432` and passing after the bounded content-line, one-edge width-snap and marker-ceiling corrections. The recovered intermediate run records the first two corrected boundaries passing before the short-viewport width assertion exposed its older fixed half-unit tolerance; the marker journey's recorded output independently preserves its 258.667-versus-288 three-up measurement.

Non-goals: No production source, visual geometry, plaque composition, card surface, rules authority, action offer, command, snapshot, content or unrelated test change; no tolerance or ceiling beyond the observed approved baseline.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829b1a-reconcile-rail-geometry-guards-after-the-approved-plaque-changes-completion-evidence).

### [ ] 8.29c Restore the Director Undo player path

Proposed at the user's direction on 2026-08-29 after hands-on play found that the visible Director Undo control no longer undoes. Documentation of this correction is approved; implementation was deferred on 2026-08-29 when the symptom could not be reproduced and the user chose to document a future recurrence more exactly. The authoritative retained-prefix operation remains separately covered, so this milestone owns the real pointer-to-authority-to-refresh journey and must not assume whether the defect is hit routing, enablement, callback wiring, rejection, or refresh until its pre-fix reproduction identifies that boundary.

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

### [x] 8.29d Grey attacks whose mandatory card text cannot establish a target

Completed on 2026-08-29 after Brogan's Red charge Attack remained actionable without a valid charge target; code inspection confirmed that target-by-card-text Attacks deliberately bypassed the availability probe. The correction is shared and source-driven: Brogan and Tigerclaw are required examples, not card-name exceptions.

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

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829d-grey-attacks-whose-mandatory-card-text-cannot-establish-a-target-completion-evidence).

### [x] 8.29e Stop offering impossible timed optional effects

Approved and completed at the user's direction on 2026-08-29 after before/after-Attack effects offered choices with no valid target, with Arien observed as an additional example. Code inspection confirmed that an ordinary optional effect node pauses for confirm-or-decline before consulting the existing pure feasibility probe. The correction is shared and content-derived: the `Optional` control node now asks its one branch the question a `Choose` has asked each of its options since milestone 6.5ac.

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

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829e-stop-offering-impossible-timed-optional-effects-completion-evidence).

### [ ] 8.29f Close a completed Attack before another action can start

Proposed at the user's direction on 2026-08-29 after hands-on play and the accepted rule-event log demonstrated Arien's Red Tier I Dangerous Current completing its Primary Attack against a minion and then accepting the same card's Secondary Movement before the card resolved. Code inspection confirmed that Attack completion and source-card resolution are separate transitions, while a later action start relies on the selected unresolved card and absence of pending work rather than an independent one-action-spent boundary. Documentation of this correction is approved; implementation is not yet approved. Dangerous Current is the required configured reproduction, not a card-name exception.

Observable outcome: Once an Attack reaches its terminal Action Ended boundary, its played source card resolves exactly once and Initiative advances before any unrelated Primary, Secondary, Hold, Fast Travel, or Clear action can start on that card; only a rules-authorized repeat or genuinely pending action consequence may keep the card open.

Acceptance checks:

- A focused configured regression reproduces the reported Level-I Dangerous Current sequence: Arien performs its Primary Attack against a legal enemy minion, its Before-the-Attack text follows the selected zero-or-one-hero outcome, and the minion Defeat/removal path completes. The same card is then resolved, the selected actor advances, and submitting its Secondary Movement invocation is rejected atomically with no movement, second action-start event, or extra card transition.
- Every terminal Attack path reaches one shared completion invariant exactly once, including successful and defended hero Attacks, ordinary minion Defeat, removal prevented or completed, Before-the-Attack and After-the-Attack text with immediate or resumed choices, a mandatory step that stops the remaining action, and the final instance of a legal repeat. A path that is genuinely paused on Defense, card text, removal prevention, displacement correction, Lane Push, or an authorized repeat remains open and exposes no unrelated ordinary action.
- The turn/action authority rejects a stale second normal-action command after the same selected card has recorded a terminal action boundary even if presentation still holds an old invocation. The protection is shared and source-driven; it does not parse printed text, branch on Arien or Dangerous Current, or rely only on hiding a HUD control.
- Rulebook exceptions remain distinct: Defense continues to be a reaction from a card in hand and not the played card's one turn action; a card-text or continuous-source Repeat still performs only its explicitly authorized additional instance; resolving a defeated hero's unresolved card still performs no action.
- Snapshot restore, replay, retained-prefix Undo/reapply, restart, deterministic drivers, ordered semantic events, and immutable projections reconstruct the same one-action boundary. Rejected stale second-action commands do not enter accepted history or change version, pending state, occupancy, card state, or Initiative.
- Focused Core/Edit Mode lifecycle regressions pass, plus a focused Play Mode pointer journey that performs Dangerous Current's Attack and proves its Movement region is no longer actionable after completion. The event log shows one terminal Attack, one card resolution, and no second action start.

Closing checks: `ACompletedAttack_CannotStartASecondActionOnItsUnresolvedSourceCard` is recorded failing at the pre-fix revision where Dangerous Current's completed Primary Attack is followed by an accepted Secondary Movement and passing after correction. `EveryTerminalAttackPath_ResolvesItsSourceCardExactlyOnceOrNamesItsPendingContinuation` closes the shared lifecycle matrix and prevents a card-specific repair.

Non-goals: No change to Dangerous Current's wording, values, targeting, Before-the-Attack effect, minion Defeat/removal rules, action timing, Repeat semantics, Defense exception, Initiative order, card-content definitions, networking, animation, or general action-HUD design; no hero/card-name branch and no presentation-only suppression standing in for authority rejection.
