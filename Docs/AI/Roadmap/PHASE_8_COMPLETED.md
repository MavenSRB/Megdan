# Phase 8 - Completed milestone definitions

Canonical active roadmap shard holding Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-08-22 during milestone 8.8a's required warning-threshold ownership review, under the standing growth pattern [PHASE_6_COMPLETED.md](PHASE_6_COMPLETED.md) established on 2026-08-14: when a phase shard reaches its 85% warning threshold, its completed self-contained definition sections move here verbatim during that same handoff, leaving a routing summary and stable link behind. Definitions remain active authority under the same `Roadmap/` family threshold - this is sharding, not archiving. Completed milestone evidence stays in commit bodies indexed by phase-handoff rows per [DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md).

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
