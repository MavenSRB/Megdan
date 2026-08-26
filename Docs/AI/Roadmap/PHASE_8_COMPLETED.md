# Phase 8 - Completed milestone definitions

Canonical active roadmap shard holding Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-08-22 during milestone 8.8a's required warning-threshold ownership review, extended on the same date during milestone 8.11a's, extended again on 2026-08-26 with milestones 8.11 through 8.11c when milestone 8.25a's audit added two proposed corrective blocks and took the active shard past its hard limit, and extended once more on 2026-08-27 with milestones 8.12 through 8.15a during milestone 8.25d's review, under the standing growth pattern [PHASE_6_COMPLETED.md](PHASE_6_COMPLETED.md) established on 2026-08-14: when a phase shard reaches its 85% warning threshold, its completed self-contained definition sections move here verbatim during that same handoff, leaving a routing summary and stable link behind. Definitions remain active authority under the same `Roadmap/` family threshold - this is sharding, not archiving. Completed milestone evidence stays in commit bodies indexed by phase-handoff rows per [DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md).

The active shard keeps the milestones the current route reads against - the most recently completed slices and everything still open - so a task planning the next milestone still finds its immediate predecessors there.

### [x] 8.1 Inventory and verify Wasp's complete card set

Observable outcome: Wasp's cards, tiers, values, action types, text, item possibilities, and Ultimate are traceable to source images.

Acceptance checks:

- Every required face is present and readable.
- Structured data is manually checked against the images.
- Missing or ambiguous content is recorded in `Rules/HERO_WASP.md`, the shard milestone 6.1y made the owner of Wasp's inventory; anything not scoped to Wasp alone stays in `RULES_LEDGER.md`.

Non-goals: No other hero inventory or effect implementation.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-81-inventory-and-verify-wasps-complete-card-set-completion-evidence).

### [x] 8.2 Map Wasp cards to capabilities and re-baseline the remaining Phase 8 milestones

Observable outcome: Every verified Wasp action maps to required contexts, ordered nodes, typed bindings/outcomes, timing, state transitions, and reusable or custom capabilities before effect implementation.

Acceptance checks:

- The matrix covers every Wasp card face, tier, item relationship, and Ultimate from milestone 8.1.
- Existing core capabilities are distinguished from Wasp-specific refinements, new reusable primitives, and genuinely unique handlers.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and tests.
- The designated roadmap audit applies the verified inventory and capability matrix to the remaining Wasp milestones through 8.16. It recommends justified keep/merge/split/defer/remove decisions, records an updated remaining-effort range, and keeps dependencies ahead of the first Wasp card that needs them.
- Approved remaining Wasp milestones through 8.16 are reordered or split so dependencies precede the first Wasp card that needs them.
- If the Phase 8 endpoint changes, update the T3 gate list in `Testing/POLICY.md` (routed from the `TESTING.md` index) and this roadmap in the same re-baseline.
- New reusable contracts update `CARD_EFFECT_CAPABILITIES.md` before implementation.
- Any schema change is explicit and migration-tested rather than hidden inside a card milestone.

Non-goals: No effect implementation or other-hero inventory.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-82-map-wasp-cards-to-capabilities-and-re-baseline-the-remaining-phase-8-milestones-completion-evidence).

### [x] 8.3 Add verified Wasp definitions and lifecycle data

Observable outcome: Verified Wasp definitions/instances use the existing lifecycle and turn foundation without placeholder authority.

Acceptance checks:

- Wasp values and text are source-verified structured data, including the p.9 Basic/non-Basic/Ultimate supertype and the p.15 same-colour upgrade pairs the 8.1 inventory records.
- Definition revisions and upgrade/replacement relationships are explicit.
- Card images remain presentation/audit assets only.
- Synthetic test values are not used as Wasp authority.
- Every definition carries its transcribed source text as an inert `CardSourceContent` value or states its explicit absence, and no rule reads it (closes F-CARD-08; `CardDefinitionTests.EveryDefinitionStatesItsSourceContentOrAnExplicitAbsence_AndNoRuleReadsIt` observed failing pre-fix). Wasp action definitions attach no `EffectRoot` here: each card's effect composition lands at its 8.5-8.13 slice under a content-revision advance with its own compatibility-corpus decision, so no half-implemented text can run in between.
- Production composition sites take their capability catalog as a required parameter rather than defaulting to a synthetic one, with the catalog-identity naming decision recorded beside the `RISK-COMPAT-001` row (closes F-CARD-05; `EffectSeamConformanceTests.NoProductionCompositionSite_NamesACapabilityCatalogItWasNotGiven` observed failing pre-fix), and the false serializer comment the 7.9a review recorded is corrected in the same edit.
- The effect-definition validator rejects a node reachable through two parents (closes F-CARD-06; `EffectDefinitionValidatorTests.ANodeReachableThroughTwoParents_IsRejected` observed failing pre-fix).
- Crackling Dagger's verified definition and values ship as inventory data; its behavior is deferred by the 2026-08-21 user decision with the named trigger "first odd-player-count configuration", because the six-player pitch returns the Handicap card to the box at setup (p.5).
- Opportunistic in the same milestone per F-TEST-03: the `AnUnavailableAttack_StillOffersTheClearThatReplacesIt` vacuity escape gains its `Is.Not.Empty` guard.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-83-add-verified-wasp-definitions-and-lifecycle-data-completion-evidence).

### [x] 8.4 Load both Wasp hands into the director turn loop

Observable outcome: Red and Blue Wasp hands replace the synthetic hands while using the already-proven play/reveal/Initiative/turn controller.

Acceptance checks:

- Both hands hold the verified p.5 starting five (Magnetic Dagger, Static Barrier, Shock, Lift Up, Stop Projectiles) with the remaining thirteen deck cards reachable only through p.15's Level Up; the Handicap card is not instantiated.
- Commit, reveal, Initiative order, Hold, Fast Travel, secondary Movement, and secondary Defense reactions run entirely from verified printed values.
- A primary action whose card text is not yet composed (its slice milestone has not landed) is offered greyed with a decided availability and refused atomically at the start, exactly as the 7.8a1 contract draws a proven-empty action - never run without its text, because silently dropping card text is a rules violation; p.12's Hold keeps every card spendable meanwhile.
- `PlaceholderMovementHand` and `PlaceholderMovementHandRestoreState` retire here, per the retirement owner this re-baseline names in the temporary-structure register; the synthetic card definitions stop standing in for verified content in the configured session and remain visibly test-only fixtures.
- The expected schema move for verified hand/setup persistence is explicit, with the migrate-or-reject decision, corpus re-pin, and the full pinned-constant ceremony in this milestone's evidence rather than hidden inside a later card slice.
- The player-build smoke gate's `heroes=2 cards=36` markers are re-verified against the Wasp mirror (two eighteen-card decks compose exactly 36 instances); any moved marker is updated in `Testing/FUTURE_GATES.md` in the same milestone.

Milestones 8.5 through 8.14 were assigned their definitions by milestone 8.2's designated re-baseline (2026-08-21, user approved) from the verified 8.1 inventory and the capability matrix in `Rules/HERO_WASP.md`. The original placeholder titles dissolved against the evidence: Wasp has no Movement-primary card, no token or marker of her own, and no resource effect, while the three general card-text seams the 7.9a review identified (F-CARD-01, F-CARD-02, F-CARD-03) land as the general milestones 8.5, 8.7, and 8.12 ahead of the Wasp slices that consume them. Wasp's nineteen faces complete in five slices grouped by shared machinery: telekinetics at 8.6, kinetics at 8.8, basics at 8.9, Attacks at 8.10, Defenses at 8.11, the Ultimate at 8.13, and progression at 8.14.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-84-load-both-wasp-hands-into-the-director-turn-loop-completion-evidence).

### [x] 8.5 Open the general card-text seam on Attack and Defense actions

Observable outcome: a printed non-Skill action can declare an `EffectRoot` and a typed target policy, and its text runs at the p.14 checkpoint its keyword names, proven on synthetic definitions before any Wasp card consumes it.

Acceptance checks:

- `ActionDefinition` accepts a declared `EffectRoot` on printed Attack and Defense actions, and the `CardTextApplication` policy every printed Primary declares gains its rules consumer; text runs at the existing 5.8 checkpoints in p.14's printed order - Before-the-attack at step 1 (even when the attack later fails, per p.17), Defense-action text at step 4, After-the-attack at step 7, after the target defends or is removed (closes F-CARD-01; `ConfiguredCardMatrixTests.APrintedAttackWhoseTextApplies_RunsItsEffectRootInsideItsAttackTimingOrder` observed failing pre-fix).
- A printed Attack can declare a typed target policy narrowing or replacing the default: a hero-only filter (Crackling Dagger's shape) and a text-declared ranged targeting specification (p.13 defines ranged targeting by card text), both typed data rather than runtime text parsing.
- `EffectCapabilityContext` carries the enclosing action's kind, and the four sites that hardcode `RuleActivity.Skill` for immunity read it instead (closes F-BOARD-03; `ConfiguredImmunitySkillTests.CardTextOperationsAskTheEnclosingActionsActivity` observed failing pre-fix).
- A `Conditional` node publishes its predicate result as typed facts and routes a non-Applied result through `ApplyCapabilityResult` rather than silently reporting false (closes F-CARD-07; `EffectNodePolicyTests.AConditionalPredicate_PublishesItsFactsAndCannotSilentlyReportFalse` observed failing pre-fix).

Non-goals: no Wasp card composition (8.8-8.11 own the consumers), no Block resolution mode (8.11), no trigger-window execution (8.12).

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-85-open-the-general-card-text-seam-on-attack-and-defense-actions-completion-evidence).

### [x] 8.5a Show verified card words and action values in the card tray

Observable outcome: every configured Wasp card in the player-facing tray shows its verified printed words and separates Initiative, primary-action values, spatial values, and secondary-action values according to the [approved card-tray mockup](../References/MILESTONE_8_5A_CARD_TRAY_MOCKUP.png), without changing what any card does.

Acceptance checks:

- Each tray card presents its name, full `CardSourceContent` transcription, primary action kind, and lifecycle label (`PLAYED · TURN <n>`, `AVAILABLE`, or `DISCARDED`) without truncation at the supported Windows validation resolution; a source face that explicitly prints no text receives an explicit empty-text presentation rather than invented copy.
- Initiative occupies its own upper-left flag. Printed values belonging to alternate actions remain in a distinct secondary-value region, a numeric value belonging to the primary action is attached to the primary-action band, and printed Radius or Range occupies a distinct spatial badge at the lower-left of the rules panel. The grouping is derived from typed action definitions and value channels, never from card names or parsed printed text.
- The tray remains colour-coded from projected `CardColor`, and every meaning stays available without colour alone through labels, borders, selection, availability, and assistive text. **User-approved amendment, 2026-08-21:** the colour's name is no longer printed on the card face - a card wearing its colour and also spelling it says the same thing twice in the space the printed text needs - and is carried by each card's hover and assistive reading instead, on the terms milestone 7.8a already set for a hero rail's turn slot.
- Played-first/available/discarded ordering, the available-card lift, viewed-hero selection, commitment forwarding, unavailable-card refusal, pointer blocking, and keyboard order remain unchanged; the passive presenter reads projected definitions and submits no new authority.
- The five-card Wasp starting hand fits as one readable row with no clipped card, overlapping label, or rules text, and a targeted visual inspection compares the running HUD with the approved mockup.
- **User-approved amendment, 2026-08-21:** the board framing reserves a share of the viewport width along the left edge, where the tray stands, so the map sits right of centre and the cards get a band of their own. The map is framed into what is left rather than centred in the whole viewport, it widens only if it no longer fits, and every space stays inside the camera's view. The reserve was set by eye to where the user wanted the board rather than to the value that would clear the tray completely, so a little of the map is still behind the cards' corner.
- **User-approved amendment, 2026-08-21:** a hero rail's occupied turn slot reveals the card played on that turn when the pointer rests on it, drawn by the same builder the tray draws a card with, and names that card in its assistive label. An empty slot reveals nothing, a redrawn rail drops the reveal, and the slot gains no command, no focus stop, and no keyboard-order change.
- T2 validation: focused `MatchHudPresentationTests`, the directly affected `MinimalHexGridPlayModeTests` HUD cases, and one targeted Game View inspection pass at the supported validation resolution.

Non-goals: no card-effect composition, rule or legality change, source-image artwork, production illustration, animation, responsive mobile layout, six-seat HUD work, or Phase 9 presentation polish.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-85a-show-verified-card-words-and-action-values-in-the-card-tray-completion-evidence).

### [x] 8.5b Highlight tray cards whose Active effects are live

Its one prerequisite is discharged: milestone 8.5a established the card-tray layout this indicator extends.

Observable outcome: a tray card that is the source of at least one currently active Active-effect instance receives a persistent outer outline and an explicit `ACTIVE` label until the authoritative effect ends.

Acceptance checks:

- `MatchSessionProjection` publishes an immutable derived fact identifying each source `CardInstanceId` whose authoritative lifecycle contains an `ActiveEffectPhase.Active` instance. Presentation does not inspect aggregate state, parse printed text, infer from a card definition that an effect might exist, or reconstruct the answer from events.
- `TrayCardViewModel` derives one active-source state from that projected fact. The runtime card draws a distinct outer outline plus a visible and assistive `ACTIVE` label; the indicator remains distinguishable from card colour, commitment selection, availability, and the played/available/discarded lifecycle label, including when two states apply together.
- Dormant and ended instances do not highlight. Creation/activation adds the indicator on the matching source card, and cancellation or expiry removes it on the same projection refresh; multiple active instances from one source still produce one unambiguous card-level indicator.
- Snapshot restore, deterministic replay, retained-prefix Undo, restart, and ordinary live play derive the same highlighted source cards from the lifecycle they already preserve. No presentation-owned effect lifetime or new serialized answer is introduced.
- A synthetic Active-effect producer proves the complete state transition before Wasp's persistent cards are composed at 8.9, and focused presentation coverage proves a similarly named card or a card whose definition can create an effect is not highlighted without a live matching instance.
- T2 validation: focused Active-effect projection and `MatchHudPresentationTests`, the directly affected `MinimalHexGridPlayModeTests` HUD case, and one targeted Game View inspection with active, selected-plus-active, and ended states.

- **User-approved correction, 2026-08-21:** the card the current turn is being resolved from is drawn at full strength. It still carries no commitment command and gains no focus stop, but the shared unavailable treatment was dimming it as though it were spent, and the card in play is the opposite of unavailable to look at.

Non-goals: no Wasp effect composition, Active-effect rule or duration change, triggered-effect execution, continuous Ultimate or token-property indicator, card interaction change, animation, production VFX, or general status-effect panel.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-85b-highlight-tray-cards-whose-active-effects-are-live-completion-evidence).

### [x] 8.6 Complete the Wasp telekinetic skills

Its one prerequisite is discharged: milestone 8.4 loaded the verified hands milestone 8.3's definitions supply.

Observable outcome: Lift Up, Control Gravity, Center of Mass, Telekinesis, and Mass Telekinesis resolve their complete printed text in the configured Wasp mirror - the first five verified Wasp primaries in play.

Acceptance checks:

- The straight-line query implements p.19 exactly - a file of spaces, a single space counts, adjacent units are always in a straight line - and its complement filters Telekinesis and Mass Telekinesis targeting (`in range, which is not in a straight line`).
- The three orbit skills compose the existing token-widened Direct Move (6.8k) with a new equidistant destination policy: one space, neither closer nor farther under the obstacle-inclusive distance p.17/p.19 define, radius read from the card and anchored to the actor.
- Telekinesis places from the board through the existing `PlaceOperation` under a new declaration - p.18's Place keyword takes a piece "from the board or from the supply" - with unit-or-token eligibility, immunity filtering units while a token reports the named absence, and destination spaces empty and adjacent to the actor.
- `May repeat once on the same target` binds the repeated instance's target to the first instance's, overriding p.10's different-choices default from card text; Center of Mass's `May repeat up to two times` is the first verified repeat count above one, discharging the explicit-maximum and repeat-ancestry triggers 6.7 recorded, under a versioned `RepeatAction` revision.
- Any continuation state the bounded repeat count adds is an explicit schema decision with the full ceremony, not an incidental field.

Non-goals: no discard riders (Kinetic Repulse and Kinetic Blast are 8.8's), no Attack or Defense text.

- **Correction taken during implementation, 2026-08-21:** `on the same target` binds the **piece**, not the space it was standing on. Every other board subject in this engine is named by its coordinate, which p.19's one-obstacle-per-space rule makes unambiguous - but only while nothing has moved, and a repeat is separated from the instance that chose the target by a whole action boundary whose commonest content is moving that very target. A coordinate would have named whatever stood where the target used to be, which the first orbit fixture demonstrated immediately.
- **Where the count above one and the straight-line complement are observed in a configured match:** on this milestone's two synthetic fixture Skills. Center of Mass and both Telekineses are Tier II and III cards that p.6's setup leaves in the deck, so no configured match can play one until p.15's Level Up puts it in a hand - milestone 8.14's business. Wasp's own configured evidence is Lift Up, which is in her printed starting five.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-86-complete-the-wasp-telekinetic-skills-completion-evidence).

### [x] 8.7 Consume the declared modifier vocabulary

Observable outcome: every declarable `RuleModification` member has a production consumer or a by-name reasoned exclusion, and the two consumption families Wasp's persistent texts need exist as general capabilities proven on synthetic sources.

Acceptance checks:

- A conformance rule enumerates the declarable modifier space - six value channels, nine restriction activities, the permission and immunity families - and fails on any member that is neither consumed by a production seam nor excluded by name with its reason, per the FrameShapeStubs precedent (closes F-CARD-03; `ModificationSeamConformanceTests.EveryDeclarableModification_HasAProductionConsumer` observed failing pre-fix). The value channels no verified card yet modifies from text stay excluded by name rather than gaining speculative consumers.
- A restriction can qualify its restricted actors - the affected object itself, or heroes standing in a declared team relation to the source - and the Swap and Place capabilities consume it at target/destination derivation, which is the shape Magnetic Dagger's text requires (`cannot be swapped or placed by themselves, or by enemy heroes`).
- A declared conditional obstacle overlay makes spaces count as obstacles relative to a radius anchored at the source hero's current space (p.17: an Active effect's Radius moves with its hero), conditioned on the acting hero's team and position, and the one obstacle query that owns p.18's classification consumes it for movement, push, and place legality alike - Static Barrier's shape, proven synthetically here.
- `HasStandingHeavyImmunity` consumes `FindEffectiveMinionsInside` instead of deriving Battle-Zone membership a second way (closes F-BOARD-04; the `UnitKindQueryConformanceTests` link assertion observed failing pre-fix).

Non-goals: no Wasp card composition (8.9 owns the configured consumers); no new value-channel consumers without a verified card.

- **The declarable space grew while this milestone enumerated it, 2026-08-21.** The check says nine restriction activities and the rule now reports eleven, because Magnetic Dagger's text is not expressible without them: p.19's Swap and p.18's Place joined `RuleActivity` as effect families beside milestone 6.4's `ObjectPush`, which was already a family rather than an action kind. The vocabulary is enumerated from the enums rather than from a list precisely so that growth fails the rule until someone decides what the new member means, and this is that decision recorded rather than a count kept true by not looking.
- **The overlay conditions on the acting hero at derivation, not at each ask.** `RuleModificationQuery.Build` gained a required acting object, so the live set *is* the rules in force for the action being resolved and `ObstacleQuery` needs to know nothing about teams or Active effects. Reading the actor's position once is not an approximation: an overlay declaring both arms is a boundary its subject cannot cross, so the hero stands on the same side of it at every step of its own action. A moment with nobody acting writes `RuleObject.None`, and every overlay then applies to nobody.
- **p.18's after-action correction asks with no modifications in effect**, recorded rather than defaulted: the correction is bookkeeping the rules carry out rather than something a hero performs, so a conditional overlay has nobody to condition on - the same treatment its shortest-path arm already gives the Move-Through permission. Planner and commit ask identically, so the two cannot disagree about one state.
- **The two new declaration shapes are proven on test-local definitions rather than on new registered capabilities**, for the reason this milestone's own non-goal gives: 8.9 owns the configured consumers, and a synthetic showcase card declaring a barrier would be content invented to satisfy a rule. What is proven here is the whole live path - an Active-effect definition, the lifecycle, the derivation, and all three obstacle consumers - which is what 8.9 fills Wasp's numbers into.
- **F-BOARD-04's fix replaced 6.8g's direct immunity-to-effective-type link with the two-link chain** the Minion Battle already uses. Nothing weakened: the shared Battle-Zone query is separately asserted to ask the effective-type question. One reading moved with the derivation and is recorded in the code - the old local walk asked the effective question of minion miniatures too, so a miniature counting as a hero stopped accompanying a Heavy, while the shared set takes every miniature in the zone as intrinsic. No source declares `CountsAs Hero`, so nothing observable changed; the answer is now wrong in one place instead of disagreeing in two, which is the whole of the finding.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-87-consume-the-declared-modifier-vocabulary-completion-evidence).

### [x] 8.7a Separate the current unresolved card from resolved turn history

Observable outcome: each hero rail keeps a committed or revealed unresolved card in one dedicated current-card marker and adds it to the numbered turn history only after its authoritative card state is resolved, following the [approved current-card rail mockup](../References/MILESTONE_8_7A_CURRENT_CARD_RAIL_MOCKUP.png).

Acceptance checks:

- The four numbered rail slots are presented as resolved-turn history: an assignment's card appears in its turn slot only when that exact instance has `CardResolutionStatus.Resolved`. Commitment and immutable round assignment history remain unchanged; presentation delays only the history slot's visible occupation and never remembers resolution itself.
- The current unresolved card occupies the hero header's upper-right marker while its exact instance is committed or selected for resolution. The compact marker moves `LVL <n>` above the hero name, shows the revealed card's projected colour and Initiative with an explicit `RESOLVING` label, and clears when that card resolves.
- The shared complete card face moves its title up into the otherwise unused header space beside the Initiative marker. The title remains visually centred against the card's full width rather than centred only in the narrower space remaining to the marker's right, and long titles wrap deliberately when needed without colliding with the marker or pushing the value and action bands out of alignment. The tray, unresolved-card hover, and resolved-history hover all use that same layout.
- A facedown commitment uses a neutral card-back marker and explicit `FACEDOWN` label. It exposes no card name, colour, Initiative, action values, printed words, or full-card hover until joint reveal makes those facts public; presentation never uses the director's complete definition list to bypass that information boundary.
- Resting the pointer on a revealed unresolved marker shows the same complete passive card view the tray and occupied history-slot hover use. Facedown and empty markers reveal nothing; redraw, restore, replay, retained-prefix Undo, restart, resolution completion, or a change of viewed hero drops any stale hover without submitting a command or adding a focus stop.
- A resolved history slot whose source card owns a live Active effect uses the same outer outline and explicit `ACTIVE` label milestone 8.5b gives that card in the tray. The indicator is derived from the same projected active-source fact and disappears from both surfaces on the same refresh when the effect ends.
- Card colour, Initiative, `FACEDOWN`, `RESOLVING`, resolved turn number, and `ACTIVE` remain available through visible and assistive labels without colour or pointer hover alone. The compact rail retains its item and discard readings and does not cover the board beyond its existing reserved region.
- Focused checks cover facedown, revealed unresolved, selected-and-paused resolution, resolved without an Active effect, resolved with an Active effect, ended effect, Pass, restore/replay/Undo/restart, and two heroes at different lifecycle states without stale cross-rail content.
- T2 validation: focused `MatchHudPresentationTests`, the directly affected `MinimalHexGridPlayModeTests` rail/hover cases, and one targeted Game View inspection against the approved mockup.

Non-goals: no change to commitment, joint reveal, Initiative order, action resolution, card-state transitions, round assignment history, Active-effect lifetime, owner-scoped online projections, animation, production VFX, or general history screen.

- **The tray had to be asked the same question, 2026-08-21.** One turn is resolved hero by hero in Initiative order, so a real position exists with the first hero's card resolved and the second's not. The rail derives its split from the instance's own `CardResolutionStatus`; the tray's milestone 8.5b "card in play" reading asked the commitment status alone, and would have drawn that card at full strength with a `RESOLVING` lifecycle line at the same moment the rail moved it into numbered history. Both surfaces now ask the card, so one card cannot be in two states. `TheCardBeingResolved_IsDrawnAtFullStrengthWhileItsTurnRuns` moved onto a revealed-unresolved fixture for the same reason - the live-Active-effect fixture's Skill has already completed, so its card is not the card in play.
- **Four user decisions taken during the Game View review, 2026-08-21.** The revealed marker carries **no `RESOLVING` caption**, against this milestone's own written check: the card's printed colour and Initiative sitting in the header's inner corner already say it is the card in play, and the word is kept in the marker's hover and assistive reading and on the revealed face's own lifecycle band - the same treatment milestone 8.5a gives a card's printed colour. The facedown marker keeps `FACEDOWN`, because a neutral back with nothing on it says nothing. The **`RED HERO` / `BLUE HERO` line came off the rail**, for the same reason and to the same place, onto the hero disc's own reading. The **whole Blue rail is now the Red rail mirrored** rather than repeated, so both read outward from the board and each marker sits on its rail's inner corner; the four numbered slots and the `DISCARD` row are the two exceptions, because 1-2-3-4 is a sequence and a label with the number it counts reads left to right on either edge.
- **The title's reservation is measured from the band's edge, not from inside its inset.** The card's name is centred by reserving the Initiative banner's width symmetrically, which is the only arrangement that keeps the title concentric with the card *and* clear of the banner. Reserving a full banner width inside the band's own six-pixel inset took twelve pixels too many and broke `PROJECTILES` mid-word - the exact failure the pre-8.7a comment describes for the arrangement before it - so the reservation is the banner less that inset, and the title dropped half a point.
- **A Pass is unreachable in ordinary configured play**, so the fixture that proves one trims a hand at setup: four turns are spent from a hand of five and the end of round retrieves what was played, so no configured hand ever empties. `CreateSessionWithAPassingHero` restores the configuration with four of the Blue hero's dealt instances removed and changes nothing else. `ConfiguredHand` re-derives its dealt/deck split from the upgrade closure when it restores, so that split no longer matches where each card is; nothing a match reads uses it, because the session is composed from the instances and their own states.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-87a-separate-the-current-unresolved-card-from-resolved-turn-history-completion-evidence).

### [x] 8.8 Complete the Wasp kinetic skills

Milestone 8.7a completed the approved current-card rail insertion. Milestone 8.7 supplied the consumption regime, and milestones 8.4 and 8.5 supplied the hands and published predicate facts.

Observable outcome: Kinetic Repulse and Kinetic Blast resolve their complete printed text, including the stopped-hero discard rider.

Acceptance checks:

- Both compose the existing 6.4 batch Push exactly as p.18 prints it: up to two enemy units adjacent to the actor, one chosen distance - fixed 3 for Repulse, chosen 3-or-4 for Blast - resolved against one pre-effect state and committed simultaneously, farthest first.
- The push outcome's stop cause is published as typed facts, the 8.5 `Conditional` consumes them per pushed hero, and a hero stopped by an obstacle discards a card, if able: the actor's card names no chooser for the card, so the stopped hero chooses it from their own hand per p.10's worked example, with `if able` honored at the node.
- The W-04 board-edge question is resolved on its recorded proposed reading before any test encodes it: p.19 makes the water surrounding the board terrain and therefore an obstacle, so a push stopped at the board edge is stopped by an obstacle; the map model's off-map-is-nothing answer is reconciled with that reading explicitly in this milestone's evidence.

Non-goals: no Attack text, no new push geometry.

**How the composition reads, recorded because 8.9-8.11 compose against the same declarations.** The `up to` in "up to 2 enemy units adjacent to you 3 spaces" is over the units and the spaces are exact, and a node carries one `EffectFailurePolicy`, so the count and the distance are declared separately from this milestone on: `AdmittedDistances.ForDeclared` takes the least distance the card itself prints beside the declared one, and the batch declares a maximum number of targets. A declaration that names a maximum makes which units are pushed the chooser's decision; one that names none is p.18's plain batch over every adjacent object, which is what every declaration before this milestone meant and still means. Both Push declarations moved to revision 3 together because both resolve through one routine, and only the shared distance reading moved to both - the count bound and the target relation are the batch's, because a named push is handed one object by a selection node that has already answered both. `TargetQuery.StandsInRelationTo` opens the first arm of p.19's relationship vocabulary and reuses `SourceTeamRelation` rather than minting a fourth team enum; the rest of that vocabulary still waits for a card.

**The batch asks two questions, and that is the general seam this milestone added.** p.18 settles the distance over "all those units", so the units are named first and the distance second, and one capability asks both because the rule is one keyword and both answers feed one simultaneous commit. `EffectCapabilityContext.AnswersAlreadyGiven` carries what the node has already been told, projected from the resolution's own accepted choices - state the frame already carried and the payload already wrote - so no new persisted field exists and a restore between the two questions re-derives the second. `Execute` reads the same answers, which is what lets the snapshot restore path re-ask a handler what it is offering and get the question the pause was written on rather than the first one.

**W-04 is answered yes, and the reconciliation has two halves, neither of which changed the model.** The official map already states p.19's reading: every space adjacent to a playable space of `official-single-lane` is itself playable or declared terrain, so a unit pushed off the play area meets the modelled water and the ordinary obstacle rule answers the card. Past that ring the classification does answer "nothing", deliberately, because milestone 6.8e separated off-map from terrain so no Move-Through permission could carry a unit into the water - so the card's own reading is stated once, on `PushPathResult.StoppedByAnObstacle`, and admits both the blocked arm and the left-the-board arm. A later official ruling supersedes the reading rather than the classification.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-88-complete-the-wasp-kinetic-skills-completion-evidence).

### [x] 8.8a Show upgrade choices as tray-style cards

Observable outcome: when end-of-round Level Up pauses on `UPGRADE A CARD`, the contextual region presents Wasp's six projected upgrade choices as the same complete portrait card faces used in the ordinary tray, arranged as two rows that each read Blue, Green, Red.

Acceptance checks:

- Each option draws the candidate replacement card through the tray's shared card-face builder, including its Initiative, name, printed values and action qualifiers, exact source text, Range or Radius badge when present, and assistive colour reading; the prompt does not introduce a second abbreviated card design or decode printed text.
- The six choices use a two-row, three-column reading and focus order: Blue, Green, Red on the first row, then Blue, Green, Red on the second. The two candidates of each colour retain their authoritative projected order, so presentation neither identifies upgrade pairs by card name nor invents choice authority.
- The whole option card is the one selectable control and forwards only its projected typed continuation invocation. Selection remains visually and assistively explicit without changing card width, text wrapping, or row height, and the existing prompt facts still state the card returned to the deck, the sibling set aside as an item, paid cost, and remaining coins without duplicating those facts onto the printed face.
- The contextual region remains the only usable interaction context while the mandatory transition is paused; the ordinary tray, End Turn, and end-of-round advance stay unavailable. The two-row body stays inside the viewport and scrolls when necessary so all six options remain pointer- and keyboard-reachable on the validated desktop view.
- Focused `MatchHudPresentationTests` cover card-face reuse, exact projected invocation forwarding, row/focus order, visible and assistive selection, and constrained-viewport reachability. T2 validation also runs the directly affected `MinimalHexGridPlayModeTests` Level Up prompt path and one targeted 2048-by-1152 Game View inspection.

Non-goals: no change to Level Up eligibility, coin payment, upgrade-pair or item authority, continuation/snapshot/replay behavior, card definitions, the ordinary tray, other contextual-choice families, or production art and animation.

**The column order is the one ordering this milestone decides, and it had to be decided somewhere.** The pause groups its options by upgraded card and orders those pairs by card instance identity, which for the configured Wasp is Blue, Red, Green and for the showcase hand is Red, Blue, Green - the same three colours in a different order for every hero, in no order a player could learn. Everything else stays the projection's: which options exist, which pair each belongs to, and which of p.15's two candidates is drawn above the other. So `MatchHudTheme.ColorReadingOrder` states Blue, Green, Red once, the grid reads its columns by it, and the check above is met by a reordering of columns rather than by the pause happening to arrive that way. A colour a hero could bring that the list does not name sorts after every colour it does, so an unexpected value is ordered rather than dropped.

**Two user decisions taken during the Game View review, 2026-08-22.** The item each option gains is drawn under its card and the other two prompt facts are not: `SETS ASIDE <card>` and `RETURNS <card>` came off the six faces, because the grid already says both - a column is one upgraded card, and the sibling is the other card in that column - and six repetitions of what the layout states were six lines of noise under cards that need the room. Both stay in each option's hover and assistive reading, which is where this HUD already keeps a fact a surface stops drawing, exactly as milestone 8.5a keeps a card's printed colour. The second decision is that **the prompt must not scroll**: two rows of whole card faces are taller than the share `CardPanelTallViewportShare` allows a one-row tray, so while - and only while - this grid stands the tray region is bounded by the height under the status strip instead, less the band described next. Nothing competes for it, because the ordinary tray is hidden behind a mandatory decision and the board's framing is computed from the ordinary bound either way, so the map does not move when the prompt opens or closes. **The `DIRECTOR` toggle moves with it**, and that is a correction the first Game View inspection forced rather than a preference: the enlarged region covered the toggle where it stands, and the toggle is the only route to Undo, which is exactly as legitimate while a mandatory decision stands as at any other moment. It relocates to the band beside the Red rail under the status strip - the one part of that corner nothing else draws in - its drawer opens downward because nothing is above it there, and `CalculateTallContextHeight` reserves the band so the toggle stays clear even at the bound.

**The item belongs to the option rather than to the card**, which is the reason it is drawn at all. p.16 files the candidate p.15 sets aside under the matching dashboard slot, and the card set aside is the one this option does *not* take - so a player choosing between two cards of one colour is also choosing which of the two items they end the round holding, and that fact is on neither printed face. It reads as `GAINS ATK +1`, the same compact channel token and signed value the hero rails already draw an item bonus with.

**The candidate is drawn as a card in the deck, and the type says so.** `TrayCardGroup` gained a fourth member, `Deck`, whose lifecycle band reads `IN DECK`. The tray never produces it - the tray draws one hero's current round - but borrowing `Available` would have written the word `AVAILABLE` on the band of a card in nobody's hand. `CurrentRoundCards.DeckCard` is the one construction, so a candidate and the card that arrives in the hand go through the same `ReadFace`.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-88a-show-upgrade-choices-as-tray-style-cards-completion-evidence).

### [x] 8.8b Refuse a Clear that can remove nothing

Added by the user-approved roadmap amendment of 2026-08-22, from the player's report that `CLEAR` reads as an ordinary choice while standing beside nothing it could take.

Observable outcome: when no token stands adjacent to the acting hero, the `CLEAR` replacement is drawn greyed and carries no command, and a start naming it is refused atomically.

Acceptance checks:

- Availability is decided by the authority that decides a turn action's, on one predicate asked both where the offer set is built and where the start is validated, so the offer and the command cannot disagree. `ActionOffer.IsAvailable` is the vehicle, as it is for milestones 6.5af, 7.8a1, 8.4 and 8.7, and the refusal names its own typed rejection reason rather than a generic one.
- **Clearing none stays legal wherever a token is adjacent.** p.13's "any number" includes zero, so what is refused is a Clear that *cannot* remove anything, never one whose chooser declines to: a hero beside one or more tokens still confirms the empty subset, and the projected invocation list still leads with it.
- The greyed offer keeps its place and its label and reads as unavailable through opacity as well as through being inert, which is the treatment milestones 6.5ac and 6.5af give every other refused control; no second design and no sentence in its place. Opening the token selection from it is refused too, so a surface cannot enter a mode the authority would not complete.
- The refusal is recorded in [RULES_LEDGER.md](../RULES_LEDGER.md) as an approved deviation with a named review trigger, because p.13 does permit performing a Clear that removes nothing. p.12's Hold is what makes refusing it costless, and no card in this build reads "performed a Clear" as a trigger; the deviation is revisited when one does.
- Focused checks cover the greyed offer with no adjacent token, the available offer with one, the empty subset still confirmable beside a token, the atomic refusal of a start naming the greyed offer, and the selection refusing to open on it. The new rejection reason is added inert first and the checks are recorded failing before the predicate is wired and passing after.

Non-goals: no change to what a Clear removes, to token adjacency, to p.13's replacement relationship with the Attack, to Fast Travel or any other replacement, to any other action's availability rule, or to the tray and contextual region beyond the greyed control.

**A greyed offer had to stop carrying a command, and the complete-loop gate is what said so.** `ClearOptionProjection` still projects the option - the control keeps its label and its place - but an unavailable one now enumerates no `SubsetInvocations` at all. Without that it still projected the empty subset, which is precisely the `PerformClear` the rules had just started refusing, and `DeterministicMatchLoop` failed on its own invariant: a command the projection offered and the rules rejected. That invariant is the reason the milestone's first acceptance check is worth having, and it caught the gap within one run.

**What the gate's `PerformClear` coverage had actually been.** With the refusal in place, no seed of the engaged scenario that reaches an ending takes a Clear at all: across seeds 1-60 exactly one run takes one, and it reaches no ending inside the budget. So every Clear the walk had ever taken was the empty one offered beside nothing - the gate was reaching the command through the one case p.13 now refuses here. Rather than drop the claim, `TheCompleteLoop_ReachesEveryPartOfThePage7TurnAndThePage8Round` gained one bounded reachability run on that seed, and the claim is stronger than it was, because the Clear it observes is offered over a real candidate.

**The three reported seeds were re-measured, by a stated rule rather than by convenience.** Taking one command out of the offered set moves every walk's trajectory, so the Life-counter and Wave seeds moved to the **lowest seed that reaches that ending inside half the budget** - 22 and 32, replacing 11 and 42 - and the Throne seed is unchanged at 1. Endings stayed plentiful either way, which is the check that this was a re-indexing rather than a loss: across seeds 1-40 the engaged scenario still reaches the Life-counter ending on 22, 25 and 33 and the Wave ending on 5, 10, 12, 14, 26, 32, 37 and 40. `UndoCheckpointReconstructionTests` and `CompleteMatchRuntimeCaptureTests` both pin the Wave seed deliberately - they measure the match the gate proves correct - so both moved with it.

**The deviation is `DEV-007`**, with its review trigger named: any card that reads "performs a Clear" as a trigger, or an official ruling that a Clear removing nothing has an effect. Milestone 8.9's `PerformedBasicAction` already carries the kind, so such a card would be able to tell a Clear from a Hold - which is exactly why the deviation is recorded rather than assumed harmless.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-88b-refuse-a-clear-that-can-remove-nothing-completion-evidence).

### [x] 8.9 Complete the Wasp basic cards

Blocked by: milestone 8.7 (restriction and overlay consumers). Milestones 8.4 and 8.5 supplied the hands and the Attack text seam.

Observable outcome: Magnetic Dagger and Static Barrier resolve their complete printed text in the configured mirror, and the performed-basic-action fact High Voltage's trigger will read exists.

Acceptance checks:

- Magnetic Dagger runs its printed Attack through the existing combat contracts and hangs its After-the-attack text on the 8.5 seam: a `This turn` Active effect whose actor-qualified restriction forbids enemy units in radius being swapped or placed by themselves or by heroes of their own team, radius anchored to Wasp's current space and consulted at the moment a swap or place is attempted, with Wasp's own teammates unrestricted.
- Static Barrier's Basic Skill creates the `This turn` conditional obstacle overlay 8.7 proved, with both arms of its text - spaces in radius count as obstacles for an enemy hero acting from outside, spaces outside count as obstacles for an enemy hero acting from inside.
- Every action performed on a Basic card records the typed basic-action fact at its completion checkpoint - p.9's supertype through 7.1's context-sensitive classification - and performing Static Barrier's primary is the one action the configured setup can record as a basic skill.
- Crackling Dagger stays data-only per the 8.3 deferral decision.

Non-goals: no rider discards (no basic card has one), no trigger execution.

- **The disk moved onto the Active-effect definition, and it is the printed icon rather than a number.** Milestone 8.7 declared its overlay's radius on the overlay; both of this milestone's cards need one, and p.9 gives a card one Range/Radius icon whose value is what its text means by "radius" - so a definition whose scope and whose overlay measured different disks would be describing two cards, and `ActiveEffectDefinition.Radius` states it once for both consumers. `DeclaredRadius` then says where the number comes from, which is the distinction milestone 7.2b exists to make representable: the printed icon, which p.16's matching item raises, or a figure the sentence writes down, which it never does. Both of Wasp's basics print the icon, both appear on the +1 Radius item list, and the composition happens at derivation through the same `ItemBonusQuery` every printed total goes through - so nothing is stored, and a hero holding the item finds the pinning field at 4 and the barrier at 3.
- **The affected scope is the first whose membership the board decides.** `ActiveEffectAffectedScopeKind.UnitsInRadius` names units rather than heroes because p.19 does - a pinned minion is as much an enemy unit as a pinned hero - and it selects by `TargetQuery.StandsInRelationTo`, the relationship arm milestone 8.8 opened, rather than by a second reading of who is whose enemy. It consults no modification set, because it *is* that set's derivation, and that costs the rules nothing: `Counts As` replaces a unit's type and both types it walks are units, so no declaration can move an object in or out of "enemy units" - only off the board, which takes it out of the walk. Uniqueness is per controller for the same reason the disk is anchored there; without that arm the first Wasp to resolve a Magnetic Dagger would have suppressed the opposing Wasp's as a duplicate.
- **The consumers are milestone 8.7's, unchanged.** This milestone declares; the Swap and Place capabilities that ask `RestrictionQuery` at target and destination derivation, and the one `ObstacleQuery` that hands the overlay's answer to movement, push and place alike, were built and proved there on definitions with no numbers in them. Nothing here reads a card name at runtime, and neither composition is more than one mandatory capability node - everything both sentences say is definition content on the Active effect, so an effect tree with branches would be stating the same rule twice in a second vocabulary.
- **p.9's supertype is recorded structurally rather than by remembering to.** `ResolutionStep.AtCheckpoint` now refuses `ActionEnded`, so the only way to reach the completion checkpoint is `AtActionEnded`, which names the card, the hero and the kind; the turn authority decides `CardClassification.Basic` from the card and publishes one `PerformedBasicAction`. A new action family inherits the rule instead of having to be told it. The kind travels with the fact because "a basic *skill*" narrows by it and a replacement action - p.7's Fast Travel, p.13's Clear - has no definition on the card to look one up on, and the fact is produced by the command that ends the action rather than stored, so replay reproduces it and Undo removes it with everything else.
- **The 7.9 deferral gate was split in two rather than widened.** It compared every registered capability against the showcase hands alone, which since milestone 8.4 is half the cards this build deals, and Wasp's two registrations failed it. A *capability* must now be on some card this build can deal, hers included; a *handler implementation* - the reusable core capability the 7.9 check actually names - must additionally be reachable from a showcase card. Wasp's two pass the second half because they run on `ActiveEffectCreationHandler`, which milestone 6.8 proved on Red Bulwark and which was lifted out of `SyntheticModificationCapabilities` unchanged so both use one copy. A handler introduced inside a hero's slice still fails, which is the deferral the gate exists to refuse.
- **What the greyed-primary fixture lost.** Static Barrier was the milestone 8.4 rule's Skill-shaped subject, chosen because a Skill with no effect root passes both other availability rules by construction. Composing its text discharges that case - every one of Wasp's Skill primaries now carries a composition - so the fixture states the release instead, and the rule's remaining subjects are the Attacks and Defenses of 8.10 and 8.11, carried by the sibling case that neutralises 7.8a1's rule with a minion beside the attacker.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-89-complete-the-wasp-basic-cards-completion-evidence).

### [x] 8.10 Complete the Wasp Attack cards

Blocked by: milestone 8.8 (discard rider family) - discharged. Milestone 8.5 supplied the text seam and the target policies, and milestone 8.6 supplied the straight-line query and its complement.

Observable outcome: Shock, Electrocute, Electroblast, Charged Boomerang, and Thunder Boomerang resolve their complete printed text, including the escalating and repeating riders.

Acceptance checks:

- Shock and Electrocute run the shared After-the-attack rider: one enemy hero in radius and not adjacent to the actor - the composed disk-minus-ring the 7.2b targeting/presence separation supports - chosen by the actor per p.10, discards a card of their own choice, if able.
- Electroblast's rider escalates per [ledger question 13](../RULES_LEDGER.md#open-source-and-rules-questions), which milestone 8.9 answered from source: the chosen hero must discard if they hold any card, and only a hero unable to discard is defeated. The second arm is how a mandatory step completes rather than a choice either player makes, so the rider is an escalation policy on the discard and not a `Choose` node - which keeps it inside 8.5's card-text pausing boundary. The defeat runs as an effect-invoked hero Defeat through the staged p.13 chain - reward by Level, Life counters, retrieval, cancellation, respawn eligibility - registered as a capability rather than duplicated, and reusable at 8.13.
- Both Boomerangs target through the 8.5 declared ranged policy composed with 8.6's straight-line complement, excluding adjacent units per the printed reminder.
- Thunder Boomerang's conditional repeat consumes the published targeted-a-hero fact, starts one repeated Attack instance through the dispatcher under a different-target exclusion binding, and each instance carries its own Defense response, rewards, and consequence chain; a mid-turn Lane Push between instances follows the p.10 sidebar - a freshly respawned minion is targetable by the repeat.
- Heavy-minion immunity, Clear replacement, and defeat rewards hold across all five cards through the existing contracts.
- Any new pending state these riders add rides the existing continuation contract; a new pending kind is an explicit schema decision with the full ceremony.

Non-goals: no Defense text (8.11), no trigger windows (8.12).

- **The nesting landed at two of rulebook p.14's three moments, and the third is a stated boundary rather than an oversight.** What the page leaves after step 4 and after step 7 is a function of the attack's own identity - the Defense total and its comparison, or the outcome and the end of the action - so one generator produces it, used both when nothing pauses and when resuming, and the paused and unpaused orders cannot drift apart. What follows step 1 is not such a function: on the arm where p.13 simply defeats a targeted minion it contains the facts of a Defeat the resolver has already computed and applied. Card text that could ask a question at step 1 is therefore still refused at configuration, and the refusal now names the moment instead of the whole Attack. Every verified face this milestone and 8.11 need prints "After the attack:" or Defense-action text, so nothing verified is excluded.
- **The continuation persists identity and never steps.** `CardActionContinuationKind.AttackCardText` carries the paused frame - the same section a paused Skill writes - the moment the page stopped at, and the attack whose remainder is owed. It is captured before the attack's own continuation, because at step 4 the attack is still pending underneath the paused text and asking about the attack first would keep the attack and lose the frame holding the page.
- **Milestone 8.4's honesty rule has no turn-action subject left in Wasp's set.** Composing Shock removed the last one, exactly as 8.9's composing Static Barrier removed the Skill one. Its remaining verified subjects are 8.11's three Defense primaries, which are performed in reaction and which no start site asks the question of yet, plus Crackling Dagger's deferral, which p.5 returns to the box before a hand exists. **Milestone 8.11 owes this rule its refusal case**, on the path it reaches a Defense by; what is left here is the release.
- **Three new declarations, one new implementation.** The printed-Radius hero selection shares the handler the three other printed-icon declarations use, so milestone 7.9's deferral gate stays green on composition rather than on an exclusion entry; the capability reaching p.13's Defeat list and the predicate reading the enclosing Attack's target are genuinely new implementations, and both are proved on showcase content - "Red/Blue Ultimatum" and "Red Reckoning" - before a verified card names them.
- **The defeat capability declares and performs nothing**, which is milestone 6.9's mid-turn Lane-Push shape: p.13's list prices a reward by Level, spends Life counters, returns markers, resolves an unresolved card and cancels Active effects, all of it authority state no capability holds and all of it already written once in the Attack resolver. Three of that resolver's event factories read the defeated hero off the Attack's own target and now take it explicitly, because a rider can defeat a hero the Attack never targeted.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-810-complete-the-wasp-attack-cards-completion-evidence).

### [x] 8.11 Complete the Wasp Defense cards

Blocked by: milestone 8.8 (discard rider family) - discharged. Milestone 8.5 supplied the Defense text seam.

Observable outcome: Stop Projectiles, Deflect Projectiles, and Reflect Projectiles resolve their complete printed text as p.14 Block cards.

Acceptance checks:

- Block evaluation lands on the reserved 5.8 `DefenseBlockResult` seam at p.14's step 4: the condition - the incoming attack is ranged, read from the attack's typed subtype facts - fulfilled means a successful defense regardless of totals, unfulfilled means the defender is defeated, and no Block ever becomes a sentinel Defense amount.
- Deflect's and Reflect's on-success riders compose the defender-anchored `RangeQuery` from the card's printed Range icon: one enemy hero in range - excluding the attacker on Deflect, any enemy hero on Reflect - chosen by the defender performing the Block per p.10, discards a card, if able.
- A Defense-primary card in the played state offers only its secondary and general-rule actions, because p.14 forbids performing a Defense action on an already played card.
- The pending-defense continuation state the Block evaluation adds is an explicit schema decision with the full ceremony.

Non-goals: no continuous sources, no Ultimate.

- **The Block verdict is a fact the resolution carries, not a Defense number.** `DefenseBlockResult` lands on the seam milestone 5.8 reserved, gains `ConditionUnfulfilled` beside `Fulfilled` and `None`, and is written on the Attack resolution state - which is why this milestone moves the schema. A Block never produces a Defense amount at all: where a Defense card would be totalled, a Block short-circuits the comparison, so no sentinel value can be mistaken for a printed one and p.14's step 5 is skipped rather than fed. Unfulfilled defeats the defender through the same staged p.13 chain 8.10 registered, not through a second copy of the page.
- **p.14's already-played rule holds twice over, and the case says so rather than claiming a mechanism.** A printed Block is available in the Reaction window and nowhere else, and it declares a Hand -> DiscardPile state transition, so a card in the turn area fails both tests independently. The knockout evidence in the handoff found this: removing the window alone left the transition covering the same assertion. The case now pins the outcome and names both rules, and the window's other half - that the Block *is* offered once an Attack is being answered - is what every Block case stands on.
- **The Block keeps its printed origin; the window and the invocation context widened instead.** Milestone 8.3's rule is that a printed primary is recorded as one, so `PrintedPrimary` stays and `ActionOfferQuery` asks the definition rather than the origin. The two sites that had encoded "a Defense is a Reaction" as an origin check - the pending defense-response choice and the completed-defense match - were taught the printed Block instead of relaxed.
- **The attacker exclusion is a parameter of the Range channel and of nothing else.** "another enemy hero" is printed only by Deflect, only on a Range selection, and the attacker it excludes is the enclosing Attack's actor - a fact about the action being answered rather than about anything the text did - so it is read from the same context p.13's subtype is, through `EnclosingAttackFacts`. Declaring it per channel keeps the Radius selections' declarations byte-identical, which the corpus measurement confirms.
- **Milestone 8.4's honesty rule got its refusal case, and its subject is synthetic on purpose.** After this milestone no verified Defense owes a composition, so a case built on Wasp's own cards would assert over nothing; the discriminating pair is one synthetic card offered when its text is composed and withheld when it is not. p.14 makes refusing costless the same way p.12 does, so a withheld Defense costs the defender nothing.
- **A new corpus fixture earned its place the moment it existed.** `paused-block-rider-v24` caught a Conditional root declaring an output its predicate never published - on this milestone's rider, and latently on 8.10's repeat root, where no other fixture looked. That is the case for adding a fixture per pausing section rather than per milestone.
- **8.10's digest re-pin had no recorded cause, and this milestone recorded it.** The aggregate-invariance fixture requires every re-pin to state its byte-level delta in the re-pinning commit; 8.10 moved all nine pairs without one. The delta was measured after the fact in a throwaway worktree at 19a6def8 - schema 22 -> 23 plus four capability registrations, 126 bytes on every carried payload and 180 on the verified-hand one - and written into the fixture beside this milestone's own.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811-complete-the-wasp-defense-cards-completion-evidence).

### [x] 8.11a Official map underlay and transparent interaction overlay

Blocked by: milestone 8.11, so the current rules-content milestone closes before this bounded presentation amendment begins.

Observable outcome: The configured match presents the official `GuardsII_Small_Board` artwork as its
private board underlay while retaining the reviewed 292-space map and every existing board interaction
as a precisely registered transparent overlay.

Acceptance checks:

- A project-local deterministic preparation route reads the private Tabletopia manifest entry named
  `GuardsII_Small_Board`, extracts its declared 1588-by-1588 sprite from the declared
  `Image_17_Atlas` source, and writes only an ignored private Unity runtime asset; it does not alter,
  move, add, or commit the source atlas, extracted corpus, or map-reference directory.
- With the private runtime asset present, the complete official artwork is rendered flat on the board's
  XZ plane beneath units, tokens, spawn markers, highlights, and board-facing cues. Base tile fills and
  debug edges are transparent or visually suppressed without removing the tile views used by existing
  selection and highlighting paths.
- The authoritative 292 spaces in `official-single-lane` revision 1 register to their corresponding
  artwork positions within one declared visual tolerance across the complete board. The check uses
  reviewed map identity and coordinates; Tabletopia's 332 magnetic centres may assist calibration but
  neither create spaces nor become runtime or rules authority.
- Movement, Fast Travel, Attack targeting, effect choices, correction destinations, Hero Respawn,
  Defense contributors, Minion Battle choices, Clear selections, and token/object cues retain their
  existing projected legality and typed command paths. Their visible overlays remain readable against
  the artwork and identify the same stable space or object before and after Undo, restart, snapshot
  restore, replay, and a presentation rebuild.
- Camera framing still contains the whole authoritative map in the board band left by the HUD at the
  validated 2048-by-1152 Game View and the applicable short-viewport fallback. A targeted visual
  inspection confirms that artwork, units, transparent overlays, and the standing HUD remain legible
  without z-fighting, opaque debug tiles, clipped board edges, or the card tray concealing an actionable
  cue.
- With the private runtime asset absent, the tracked project loads and plays through the existing
  generated-board fallback without a missing-reference exception, compile failure, or rules/state
  difference. The private artwork is included only in specifically authorized internal builds and is
  not added to the public source repository or otherwise distributed by this milestone.

Non-goals: No change to the official map specification, space count, topology, zones, spawn points,
rules, commands, snapshots, or replay; no adoption of Tabletopia magnetic centres as game spaces; no
new camera controls, 3D scenery, unit art, animation, HUD redesign, public build, publication-rights
determination, or distribution authorization.

**User-approved scope amendment, 2026-08-22:** board targeting was folded into this milestone. Every choice with a board surface is answered on the board, the way an Attack target always has been, and the prompt keeps only what has no board surface.

- **The registration was settled against the artwork, not against the manifest's magnetic map.** The manifest's 332 magnetic centres fit one hexagonal lattice exactly - 332 distinct indices, worst deviation 10.4% of the 2.4074-unit spacing - and they are stated in the sprite's own frame, which was confirmed by drawing all 332 onto the extracted face and finding every one on a painted hex. What they could not settle is orientation: the twelve lattice symmetries score 272 to 283 of 292, so the best is not meaningfully better than the rest. It was settled by drawing the map's seven zones over the face, where one orientation puts every zone boundary on a painted boundary - Red Throne on the drill, Blue Throne on the crystal, both jungles on the forests, terrain on the rock ring - and no other does. **The highest-scoring lattice match is not that orientation**, which is why `OfficialBoardArtwork` records the reasoning rather than only the numbers.
- **The measured registration is a similarity and nothing looser**, fitted over the 275 spaces that have a magnetic centre: mean deviation 0.031 and worst 0.076 board units, which is 0.054 world units or 5.4% of a space's outer radius, against a declared tolerance of a tenth of a space. No shear and no mirroring, both excluded by test rather than by hope, because a landmark table cannot see either.
- **The camera stands on the board's own axes, by the user's decision.** The board's hex lattice sits about fifteen degrees inside its square, so the square and the lane cannot both be square to the screen. The artwork wins: the square reads upright as it does on the table, and the lane runs corner to corner because on this board it does. `BoardChoiceCue` takes its up from the same statement, so every board-facing cue turns with it - `StandardMinionView` had that rotation written out by hand and was the one thing that stayed crooked until it read the shared construction.
- **Nothing is removed to make room for the artwork; it is suppressed.** Tile fills become one fully transparent material, debug edges and spawn rings switch their renderers off, and every view stays configured - so selection, highlighting and the presentation tests all read exactly what they read before, and a checkout without the artwork puts all of it straight back.
- **A hex you choose is lit; a piece you choose is marked.** Ranges, correction destinations, respawn spaces and empty effect-choice candidates keep the outlined region, because the answer is the space and a region only reads as a region when it is lit. Attack targets and effect candidates naming a unit or token wear the Minion Battle's own marker instead, because the answer is the thing standing there. That split is also the only thing that reads: a tint has to contrast with pale sand, mid brown, jungle and near-black rock at once and no colour does, while the marker carries its own dark backing.
- **A Block was unreachable, and milestone 8.11 shipped it that way.** The defence preview calculated a Defense value for every offer, so a Block previewed as a card worth zero and predicted the defender losing an attack it in fact stops; the prompt then looked for the offer's printed Defense icon, found none on a Block, and threw where it was drawn - so the authority asked and the screen asked nothing. p.14's condition now lives in one `DefenseBlockQuery` that both the resolver and the preview ask, because a preview that disagreed with the resolution would be worse than none. **8.11's cases all drove the authority directly and never built the prompt**, which is the gap `PromptReachabilityConformanceTests` now closes: every offer the authority makes must be answerable on the board or in the prompt, checked from the authority's own legal sets.
- **The board never offers what the rules withhold.** Marking every Battle candidate rather than only the hovered one exposed that `Candidates` is every unit in the zone while `LegalSelectUnits` is p.8's Heavy-removed-last rule applied; the board reads the latter.
- **What the artwork could not be drawn with.** `RuntimeShaderResolutionTests` requires every shader resolved by name to be declared in Graphics settings, so two attempts were made to render the face with the Standard shader: albedo and emission both carrying the texture doubled it, and blacking the albedo left the broad specular and reflection response to this scene's flat blue-grey ambient, which bleached it even with both switched off. `Unlit/Texture` is declared instead, at the file id measured from the Editor rather than guessed into project settings.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811a-official-map-underlay-and-transparent-interaction-overlay-completion-evidence).

### [x] 8.11b Replace Wasp pins and abbreviated card indicators with verified card-table visuals

Blocked by: milestone 8.11, so Wasp's complete printed Defense set and the response path it uses are available before this bounded presentation pass begins.

Observable outcome: the configured mirrored match renders each primitive Wasp pin as the official team-coloured Wasp token, a pending Defense response renders every legal Defense card as the same complete portrait card face used in the ordinary tray, and each hero rail represents resolved and discarded cards with individual mini-card icons whose complete faces appear on mouseover.

Acceptance checks:

- A project-local deterministic preparation route reads the private Tabletopia manifest entry `Wasp` (`VerticalTokens`, element `8597310`), extracts its declared 302-by-302 Red and Blue circular token faces from `Image_1_Atlas`, and writes only ignored private Unity runtime assets. It does not alter, move, add, or commit the source atlas or extracted corpus.
- Each configured mirrored Wasp view selects the matching Red or Blue face from its projected hero content and authoritative team. The token is a flat board-readable disc centred on the hero's authoritative coordinate; art never supplies hero identity, team, occupancy, selection, or legality.
- The token replaces only the primitive prism's visible mesh/material. Existing pointer selection, collider footprint, target and choice cues, movement refresh, Defeat/respawn visibility, Undo, restart, snapshot restore, replay, and presentation rebuild continue to bind the same stable `HeroId` without duplicate or stale views.
- Every legal Defense-card invocation is drawn through the tray's shared complete card-face builder, including Initiative, name, printed values and qualifiers, exact source text, Range or Radius badge when present, and assistive colour reading. The whole card is the selectable control and forwards only its projected response invocation; `DECLINE DEFENSE` remains a distinct non-card control.
- Hover and keyboard focus on a Defense card retain the authority-computed Attack/Defense/result preview, including the offer-bound `PRINTED DEFENSE` reading, without presentation recomputing totals, Block results, eligibility, or predicted outcome. Visible order and keyboard order match the projected response order, and selection changes no card width, wrapping, or row height.
- Each occupied resolved-turn slot is a compact card-shaped icon derived from that exact resolved card. The `1`, `2`, `3`, and `4` text is removed from the slots; their fixed left-to-right positions under `RESOLVED TURNS` retain sequence, while an unoccupied reserved slot remains an empty card outline with no invented card or number.
- The discard row replaces its aggregate number and colour pips with one compact card-shaped icon for each projected discarded card. It displays no numeric count: the visible icons are the count. Their deterministic display order is derived from projected card state rather than from presentation-observed discard events.
- Resting the pointer on any occupied resolved-turn or discard icon opens the same single complete card-face preview used by the tray and current-card rail. Moving between icons replaces that preview, leaving the rail or rebuilding it closes the preview, and empty slots reveal nothing. Visible colour is supplemented by an assistive label naming the exact card and lifecycle location.
- Four legal Defense cards plus Decline remain simultaneously pointer- and keyboard-reachable at the validated 2048-by-1152 Game View, with the existing bounded-scroll fallback on short desktop viewports. A targeted Game View inspection covers both team-coloured Wasp tokens, a numeric Defense face, a Block face, Decline, preview hover/focus, and board cues against the current board presentation.
- Focused `MatchHudPresentationTests` cover shared card-face reuse, exact response forwarding, Defense-preview retention, response order, constrained-viewport reachability, icon-per-card discard representation, number-free resolved/discard slots, full-face rail previews, empty slots, and refresh without stale icons. Directly affected Play Mode coverage proves both Wasp token variants across movement, selection, Defeat/respawn and rebuild, plus the configured pending-Defense and hero-rail interaction paths. T2 validation includes one targeted Game View inspection.
- With the private token assets absent, the tracked project loads and plays through the existing generated primitive hero fallback without a missing-reference exception, compile failure, or rules/state difference. The private artwork is included only in specifically authorized internal builds and is not added to the public source repository or otherwise distributed by this milestone.

Non-goals: no change to hero or team authority, occupancy, movement, targeting, Defense eligibility or resolution, contribution totals, Block semantics, card lifecycle, discard or resolved-turn authority, snapshots or replay; no official-map underlay, 3D miniature, animation, camera change, general Phase 9 hero-art pipeline, broader HUD redesign, public build, publication-rights determination, or distribution authorization.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811b-replace-wasp-pins-and-abbreviated-card-indicators-with-verified-card-table-visuals-completion-evidence).

### [x] 8.11c Answer identity-named effect-choice candidates on the board

Blocked by: milestone 8.11a, which put every space-named candidate on the board and left this one shape behind.

Observable outcome: an effect choice that names a hero, a minion, or a token by identity is answered by clicking that piece, exactly as one that names a space already is, and its control leaves the prompt.

Acceptance checks:

- A candidate's board space is resolved from authoritative occupancy rather than from the candidate's own value kind, so a candidate carrying a `HeroId`, a `MinionId` or a `TokenInstanceId` marks and answers on the piece it names. `EffectChoiceCandidatePreview` covers space-valued candidates only today; extending the preview query is the preferred home, because the preview already exists to say what a candidate means on the board and would then supply the resulting-space reading these candidates currently lack.
- The resolution is total and asserted as such. `IBoardDisplacementAuthority.TryGetObjectAt` returns at most one object per space, so exactly one candidate resolves to any space; two is a violated invariant and fails loudly rather than falling back to a control.
- Every candidate the authority offers is marked on the piece it names or lit on the space it names, and its prompt control is withdrawn - which requires the round view model to carry whether an invocation is answerable on the board, rather than the view inferring it from the candidate's shape. `EffectChoiceDestinations`, `EffectChoiceTargets` and the generic `EffectChoices` list keep their existing meanings.
- Reflect Projectiles' rider is the live subject: it names the attacker by `HeroId`, so clicking the attacking hero answers it. Milestone 8.11a's `PromptReachabilityConformanceTests` tightens from "answerable on the board or in the prompt" to "answerable on the board" for every candidate that names an object, and the pre-fix observation is that case failing.
- Selection, targeting, correction, respawn, Minion Battle and Attack targeting keep their existing projected legality and typed command paths, and identify the same stable space or object before and after Undo, restart, snapshot restore, replay, and a presentation rebuild.

Non-goals: no change to effect-choice authority, candidate sets, legality, or command shapes; no change to the board underlay, registration, camera framing, cue vocabulary, or highlight palette; no schema move, and no new pending state.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811c-answer-identity-named-effect-choice-candidates-on-the-board-completion-evidence).

### [x] 8.12 Execute Active-effect and continuous trigger windows

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-812-execute-active-effect-and-continuous-trigger-windows-completion-evidence).

Blocked by: milestone 8.11e, so triggered continuations inherit one atomic command/reconstruction-to-projection publication boundary.

Observable outcome: an Active-effect or continuous source that declares a triggered effect runs that effect when its trigger checkpoint arrives, proven on synthetic definitions.

Acceptance checks:

- `ActiveEffectDefinition` and the continuous-source declaration can carry a typed effect reference, and the lifecycle's `Triggered` visit - today one emit site with zero production readers - executes the declared effect through the ordinary frame (closes F-CARD-02; `ActiveEffectLifecycleTests.AnEffectDeclaringATriggerWindow_RunsItsDeclaredEffectWhenTheCheckpointArrives` observed failing pre-fix).
- Simultaneous triggered effects order through p.17's tie procedure without flipping the Tie Breaker; cancellation on defeat, card-state change, and expiry follows the implemented p.17 boundaries.
- A triggered execution that pauses rides the existing continuation contract; a new pending kind is an explicit schema decision with the full ceremony.

Non-goals: no High Voltage composition (8.13), no new trigger vocabulary beyond what the synthetic proof needs.

### [x] 8.13 Complete High Voltage

Blocked by: milestones 8.9 (the basic-action fact), 8.10 (effect-invoked Defeat), and 8.12 (trigger execution).

Observable outcome: the unlocked Ultimate's continuous ability fires after each basic skill Wasp performs, exactly as printed.

Acceptance checks:

- The 7.3 continuous Ultimate source declares the triggered ability; the trigger consumes the 8.9 performed-basic-skill fact, applies only while the hero is on the board, and fires each qualifying time.
- The optional defeat targets one enemy minion in radius through the effect-invoked staged Defeat 8.10 registered, with Heavy-minion immunity filtering targeting, defeat coins per p.17's keyword, and a resulting last-minion removal recording p.8's mid-turn Lane-Push cause exactly as any other defeat.
- The rider reads adjacency against the pre-removal state - `an enemy hero who was adjacent to that minion` - with the actor choosing among the was-adjacent heroes per p.10 and the chosen hero discarding a card, if able.
- Unlock is not claimed here: 8.14 proves the p.15/p.16 path that turns the card faceup; this milestone may stage the unlocked state directly, visibly test-only.

Non-goals: no leveling or item integration.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-813-complete-high-voltage-completion-evidence).

### [x] 8.14 Integrate Wasp progression, items, and the Ultimate unlock

Blocked by: milestones 8.6, 8.8, 8.9, 8.10, 8.11, and 8.13 - every upgraded card a Level Up can put in hand must already resolve its text.

Observable outcome: the complete p.15/p.16 progression runs over Wasp's real eighteen-card deck, from the first Level Up through the Ultimate unlock, with items composing onto verified printed icons.

Acceptance checks:

- p.15's mandatory end-of-round leveling runs over the verified upgrade pairs the 8.1 inventory records, through the implemented 7.5 transition, with the set-aside card of each pair becoming the p.16 item under its verified icon and slot.
- The `SetAside` sweep keeps reading the location, and `HeroLevelUpTests.TheSetAsideLocation_HasExactlyOneWriterAndItIsPageFifteensUpgradeStep` fails the day that location gains a second writer. Scoping the sweep by the transition's own completed items was implemented, reviewed and **withdrawn by user-approved amendment on 2026-08-23**: F-PROG-08 is a growth finding with no reachable case - `CardLifecycleAuthority.TrySetAsideFromDeck` has exactly one caller and admits a card only from the deck - and a completed item can name the hero rather than the card, so the scope would not have covered the future rule the finding anticipates. F-PROG-08 stays open against the rule that first needs it.
- Item bonuses compose per icon onto verified cards, including the 2026-08-21 approved interpretation of ledger question 12: an item's +1 reaches a matching printed icon on the Ultimate, so a +1 Radius item makes High Voltage's Radius 4; the interpretation is recorded in `RULES_LEDGER.md` and superseded by any later official ruling.
- Upgrading every non-basic card to Tier III and leveling once more unlocks the Ultimate - faceup beside the dashboard, no further levels, coins still collectable - and 8.13's ability is reachable end-to-end from an ordinary match.

Non-goals: no rebalance, no second hero.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-814-integrate-wasp-progression-items-and-the-ultimate-unlock-completion-evidence).

### [x] 8.15 Complete and verify every Wasp card behavior

Observable outcome: Every Wasp definition and legal action at each pitch-relevant tier passes source-linked focused scenarios after its re-baselined dependencies are available.

Acceptance checks:

- Every offer/context from the milestone 8.2 matrix is implemented; a blocked entry keeps this milestone incomplete.
- No placeholder value, display-name branch, runtime text parsing, or UI-owned rule path remains.
- Interaction tests cover each required timing, target, modifier, replacement, repeat, state change, Active effect, and continuous source.
- Any unresolved source question prevents completion rather than being guessed.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-815-complete-and-verify-every-wasp-card-behavior-completion-evidence).

### [x] 8.15a Present card choices as faces and finish board-first selection

Inserted 2026-08-23 as a user-approved roadmap amendment, from the user's direction on 8.15's result: the discard chooser must show whole card faces, and every choice whose subject stands on the map is made on the map.

Observable outcome: a choice among cards is presented as the cards themselves, and no prompt control anywhere in the HUD names a board object or space.

Acceptance checks:

- A card-instance effect-choice candidate is drawn as its whole printed face through the shared tray builder - rulebook p.10's discard chooser included - forwarding exactly its projected invocation; the card's name stays in the control's hover and assistive reading.
- The open Clear selection marks every offered token where it stands, Eligible or Selected with the Removal intent, derived from the selection itself rather than from pointer state; a click on an offered token's space toggles it through the same board intent chain every other board answer takes; and the prompt keeps only the controls that have no board surface - confirm and cancel.
- The prompt-reachability conformance fixture covers the Clear selection: every offered candidate markable and toggleable on the board, and no candidate prompt control drawn (observed failing at the pre-fix surface).

Non-goals: no rules change - the empty-subset Clear confirm stays legal per 8.8b and the greyed unavailable offer stays refused; no schema change.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-815a-present-card-choices-as-faces-and-finish-board-first-selection-completion-evidence).
