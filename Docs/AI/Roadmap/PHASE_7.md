# Phase 7 - Progression, victory, and the complete synthetic core loop

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_7 handoffs](../Archive/Phase-Handoffs/PHASE_7.md).

## Phase 7 - Progression, victory, and the complete synthetic core loop

### Phase-wide durability rule

Every Phase 7 milestone, including milestones 7.1-7.7, that adds authoritative state or changes the durable meaning, identity, lifecycle, or continuation of existing state must close that persistence boundary in the same milestone; a later integration gate is not substitute evidence. Its focused acceptance evidence must reach the new state and every new pending continuation, then prove:

- deterministic snapshot round-trip plus restore-and-continue through the same public command path, with equivalent authoritative state, ordered events, accepted history, and reconstructed projections;
- fresh deterministic replay and retained-prefix Undo/restart reconstruction, including truncation before and after the new state transition, without duplicated, lost, or projection-only state;
- explicit classification of every added aggregate field under the existing rollback contract, plus refused/faulted-command coverage proving rollback preserves the new state and both event streams atomically;
- projection reconstruction from restored and replayed authority rather than serialized or presentation-owned projection state; and
- explicit compatibility handling in that milestone: every incompatible snapshot schema, stable identity, or content-revision change receives a typed migrate-or-reject decision and corresponding compatibility-corpus update, while a compatible change proves the maintained corpus still restores and round-trips with unchanged meaning.

### [x] 7.1 Add synthetic card colors, tiers, and upgrade relationships

Observable outcome: Synthetic cards expose authoritative typed `CardColor` and Tier data for rules queries, while synthetic non-Basic cards model Tier I-III same-color upgrade choices without mutating definitions or using Wasp data.

Acceptance checks:

- Basic cards have no Tier and are excluded from lowest-Tier and upgrade-candidate queries.
- Every definition explicitly represents its applicable typed `CardColor` or verified absence; card color is distinct from Red/Blue team identity and any runtime or UI slot, and is never inferred from art, tint, display name, hero identity, or UI grouping.
- Pure context-aware queries derive deterministic card candidates by color together with owner/controller, location, lifecycle state, and rules-facing information availability for later choices, costs, conditions, counts, and effects.
- Color-filtered card queries compose with typed pending choices and lifecycle mutations such as `DiscardCard`; the mutation cannot guess color eligibility, ownership, or chooser policy.
- Every non-Basic definition has an explicit Tier and stable same-color next-Tier candidates where applicable; color constrains progression, while valid upgrade relationships remain explicit rather than being inferred from color alone.
- Hand, deck, and set-aside instances retain ownership and definition revision through upgrade transitions.
- Each synthetic hero has authoritative current-Level state initialized to Level 1.
- Typed queries can identify a hero's current lowest non-Basic card Tier for later Life and leveling rules.
- Focused tests cover same-color upgrade candidates, color-filtered legal-card candidates, team/color separation, state and ownership filtering, information suppression, and deterministic empty-candidate behavior.

Non-goals: No coin spending, level purchase, item bonus, Ultimate, Wasp definition, verified hero-specific color effect, or production card-color presentation.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-71-add-synthetic-card-colors-tiers-and-upgrade-relationships-completion-evidence).

### [x] 7.1a Reach card state from an effect definition

Observable outcome: A synthetic Skill requires another hero to discard one card of their own choosing; the choice is owned by that hero, declines without failing the action when they have none, and survives snapshot, replay, and retained-prefix Undo.

Context: milestone 7.1 built the color-restricted discard out of the three pieces the card-system contract names - the characteristic query, the effect frame's own typed pending choice, and the in-hand discard mutation - and found it unreachable from a card. The effect frame carries card definitions but not card instances once a Skill has started, so the choice-answer, repeated-skill and option-feasibility paths cannot re-derive card candidates, and neither can snapshot restore. No configured card needed it, so 7.1 recorded the gap rather than widening the resolver in the same milestone that moved the snapshot schema. The approved hero's own starting content settles the schedule: its Tier I card of one color, and two of the four cards that color upgrades into, each require an effect to make another hero discard a card of their own choosing under an `if able` policy, with the Tier III variant offering defeat as the alternative to discarding. No card text, value, or identity is reproduced here or anywhere in this repository; the hero's inventory remains milestone 8.1's, and the requirement is recorded in general terms because the extracted corpus is private. Milestone 7.9 requires proof that no source-reviewed general keyword or reusable core capability is deferred for first implementation to a verified hero milestone, so this must precede it. It is scheduled as 7.1a because that is where the gap was found, and it is not a prerequisite for 7.2 through 7.8: their card movement runs through milestone 6.11's system-transition seam rather than through the effect frame. Compatibility is expected to be a registration cause rather than a schema one - registering a capability moves every payload's registration list, as at 6.4 and 6.8k, while `EffectValueKind.CardInstance` already serializes in both directions and candidates are re-derived rather than written - but the phase-wide durability rule governs the outcome either way.

Acceptance checks:

- Card instances reach every point at which a capability runs or is re-asked: Skill start, repeated-Skill start, the option-feasibility walk, the choice-answer path, and snapshot restore's candidate re-derivation. A capability that reads cards answers identically at all five, and no path silently substitutes an empty set for cards it was not given.
- One registered card-state capability derives its candidates through `CardCharacteristicQuery` and commits through the existing in-hand discard mutation. The mutation still decides no color eligibility, ownership, or chooser policy, and no second candidate derivation exists beside the query's.
- The chooser may be a hero other than the actor. The pending choice records whose decision it is, an answer submitted for any other hero is refused atomically with the choice still standing, and the candidates are derived in the chooser's own information context rather than the actor's.
- `if able` is honored at the node: a chooser with no legal card declines without failing the enclosing action, and that outcome is distinguishable from a chooser who declines an optional step they could have taken.
- Candidates are re-derived rather than persisted, and re-read when the answer arrives rather than trusted from the offer: a card that left the chooser's hand between the offer and the answer is refused.
- The pending choice, its chooser, and its accepted value reconstruct equivalently through snapshot round-trip, restore-and-continue through the same public command path, fresh deterministic replay, and retained-prefix Undo truncated both before and after the discard, with every added aggregate field classified under the existing rollback contract and refused-command coverage proving the discard is atomic.
- No orphan composition remains: the milestone 7.1 color-restricted discard either gains this capability as its production consumer or is folded into it.
- Focused tests cover the five reach points, the opposing chooser, the wrong-hero refusal, the empty-candidate decline, the stale answer, and the persistence round trip.

Non-goals: No verified hero definition, card text, or values; the hero inventory remains milestone 8.1's. No card-state mutation beyond the in-hand discard - retrieve, swap, set aside, and upgrade from an effect stay with their owning milestones. No configured showcase card gains new printed text, so the ten-card matrix milestone 7.9 gates is untouched and the capability is proved on a fixture Skill card, as milestones 6.2, 6.4, and 6.7 proved theirs before any configured card exposed them. No per-player information filtering or hidden-hand presentation; the online track owns that, and this milestone requires only that the choice records whose it is. No change to the leveling, item, or system-transition seams.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-71a-reach-card-state-from-an-effect-definition-completion-evidence).

### [x] 7.2 Add generic item state and printed-icon bonuses

Observable outcome: A synthetic set-aside card can become an item in the matching dashboard slot and contribute through typed printed-value queries.

Acceptance checks:

- Item ownership, source card, slot, and order are authoritative data; multiple items may occupy the same slot.
- Each item adds `+1` only to the matching printed Initiative, Attack, Defense, Movement, Range, or Radius icon.
- Items never modify numbers in card text, and every contribution retains item provenance.

Non-goals: No leveling orchestrator, Wasp item values, or final item presentation.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-72-add-generic-item-state-and-printed-icon-bonuses-completion-evidence).


### [x] 7.2a Compose the item bonus into every implemented total, and give each hero its dashboard item slots

Observable outcome: A hero's items raise every printed icon this build computes a total for, and each hero's dashboard declares the four item slots rulebook p.16 places items under.

Context: milestone 7.2 built p.16's Item Bonus as one query answering for all six icons and composed it into the Defense total alone, on the ground that Defense was the build's only value-composition point. That was the wrong boundary rather than a missing one. Four further channels already have authoritative consumers: Initiative orders the turn, the Attack Total drives the outcome comparison, the Movement Total gates the action and its reachability, and a Ranged Attack's Range Total decides its legal targets. Each reaches its printed value through the same query, and each therefore composes the bonus by consulting the item state the round already owns rather than by any new derivation; the attack and defense backend already holds it, so two of the four are a binding away and the other two extend the same binding. Radius is the single channel with no current total or printed-value consumer. It is scheduled immediately afterward as 7.2b because adding its spatial query and affected-set boundary carries independent implementation and validation risk from this milestone's snapshot-schema and dashboard work. Milestone 7.2 also left the dashboard unmodelled, recording which slot an item occupies without anything that states which slots a hero has. p.16 states that an Item icon "matches the icon on one of four slots on the top of your hero dashboard", which is content this build can state now for its synthetic heroes; deferring it to milestone 8.1 would leave the placement unable to refuse an item its owner has nowhere to put. The user approved both parts on 2026-08-19, and chose that the slots ride the snapshot rather than being rebound from configuration, so that a restore refuses a build whose dashboards differ the way it already refuses one whose card content differs.

That approval also settled a rules question the page's own scan could not, and it retires a decision milestone 7.2 recorded. The four slots are Attack, Defense, Initiative, and a fourth carrying the Range, Movement and Radius icons together; the page confirms the first and third itself, magnifying the Attack slot as "the first item slot" and placing an Initiative item "under the third slot", while the dashboard's label strip carries six icons above four tucked-card positions. **A slot is therefore not an icon.** Milestone 7.2 recorded that "the slot is named by its icon rather than by a position index, because that is what p.16 matches on", which is true of the first three slots and false of the fourth, where three icons share one. The consequence is concrete rather than cosmetic: an item under the fourth slot may be a +1 Range, a +1 Movement or a +1 Radius item, so an item has to record both where it sits and what it gives, and the bonus has to read the icon. Phase 7's phase-wide durability rule governs the outcome.

Acceptance checks:

- Every value total this build computes composes the item bonus through milestone 7.2's single query: Initiative, Attack, Defense, Movement, and a Ranged Attack's Range. No consumer derives the bonus itself, and no second composition exists beside the query's.
- Each hero declares rulebook p.16's four dashboard item slots as authoritative content: Attack, Defense, Initiative, and a fourth accepting the Range, Movement and Radius icons together. A slot is a distinct thing from an icon, declares which icons it accepts, and is never interchangeable with one; nothing may derive a slot from an icon by identity.
- An item records both the slot it occupies and the icon it supplies, and the Item Bonus reads the icon. Two items under the same fourth slot supplying different icons raise different totals, which is the case milestone 7.2's slot-as-icon model could not represent.
- The four slots survive snapshot round-trip, restore-and-continue, replay, and retained-prefix Undo, with the migrate-or-reject decision and compatibility-corpus update their schema change requires.
- A set-aside card whose Item icon no slot on its owner's dashboard accepts is refused, distinguishably from a card that prints no Item icon at all, and neither refusal moves the card or records an item.
- Setup validation refuses content in which a hero can reach a Tier II or Tier III card whose Item icon no slot on that hero's dashboard accepts, so the p.16 correspondence is a checked property of the content rather than an assumption.
- Focused tests cover each composed channel with and without a matching item, a card that prints no icon of a channel gaining nothing there, the four-slot declaration, two differently-iconed items sharing the fourth slot, the unaccepted-icon refusal, and the persistence round trip.

Non-goals: No leveling orchestrator, Wasp dashboards, Wasp item values, or item/dashboard presentation. Printed Radius and its consumer are isolated in milestone 7.2b rather than silently folded into this snapshot-schema milestone.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-72a-compose-the-item-bonus-into-every-implemented-total-and-give-each-hero-its-dashboard-item-slots-completion-evidence).

### [x] 7.2b Add the missing Radius total and make Radius items observable

Observable outcome: A synthetic card's printed Radius determines its inclusive rules-distance scope, and each matching Radius item expands that scope by one through the same Item Bonus query as every other printed icon.

Context: milestone 7.2 deliberately made `ItemBonusQuery` answer all six p.16 channels, including Radius, but this build has no printed Radius value or Radius Total for that answer to reach. Storing a Radius item without a consumer would make a valid item mechanically inert. Fixed integers already used by synthetic effect text are not a substitute: p.16 modifies a printed Radius icon and never a number in card text. This milestone therefore gives Radius its missing value-consumption boundary after 7.2a closes dashboard validity, and before leveling can create real items. It also closes the query side needed by cards that choose or automatically affect units in Radius, while leaving each verified card's concrete mutation and timing to its source-reviewed content milestone.

Acceptance checks:

- A card definition can declare a typed printed Radius value, and one authoritative Radius Total composes that printed contribution with matching items exclusively through milestone 7.2's `ItemBonusQuery`; every contribution retains its printed or item provenance.
- A card with no printed Radius icon gains no Radius from an item, and a fixed Radius number written in effect text remains unchanged by items.
- A named `RadiusQuery` derives the inclusive disk from the current Radius Total using rulebook hex distance, not movement reachability, path length, occupancy, or invented line of sight.
- Radius-scoped object queries explicitly distinguish targeting from presence. A chosen or automatically affected unit is a target and is filtered by the applicable immunity; a presence check over the same spaces still counts an immune unit.
- The Radius query can return the complete deterministic eligible set needed by an effect that affects every unit in Radius, without making query enumeration order into mutation or simultaneous-resolution order. Each consuming effect remains responsible for its sourced timing and commit policy.
- At least one synthetic printed-Radius consumer uses the shared total end to end. Focused tests prove the boundary case in which a matching item admits a space or eligible object exactly one step beyond the unmodified Radius, while a nonmatching item, absent printed icon, and card-text integer do not.
- Any definition revision, registration, or persisted continuation change receives the phase-wide compatibility, snapshot, replay, Undo, rollback, and projection evidence applicable to what the implementation actually changes.

Non-goals: No Wasp or other verified hero content, no invention of a card-specific effect or ordering rule, no general simultaneous-batch control node, and no production Radius presentation. A later source-reviewed card may consume the complete affected set, but may not reimplement Radius geometry or item composition.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-72b-add-the-missing-radius-total-and-make-radius-items-observable-completion-evidence).

### [x] 7.2c Bind Red Needle's Place to a printed Radius icon

Observable outcome: The configured Red Needle card prints rulebook p.9's Radius icon, and the Place branch of its Skill offers exactly the spaces that printed value scopes, so a matching item widens where the acting hero may be placed.

Context: milestone 6.5a wrote Red Needle's Skill as `Choose one: choose another adjacent unit and Push it 3 spaces; or Place your hero in a different empty space within Radius 4`, and implemented that bound as a literal integer input to the Place capability. p.9 states that "if the card text mentions Range/Radius, it refers to this value" - the value on the printed Range/Radius icon - so a card whose text names a Radius while printing no Radius icon states a bound the page does not give it, and p.16 cannot raise it because there is no icon to raise. Milestone 7.2b built everything needed to state it correctly: a primary action may print a Radius, one query composes that printed value with matching items, and one named query derives the disk. The user approved the correction on 2026-08-19 and chose this card over the configured direct-Move card, on two grounds recorded here rather than inferred: this is the card whose own text already claims a Radius, and its showcase mode map does not change, where replacing a branch of the direct-Move card would have removed the build's only configured consumer of an operation milestone 7.9's matrix gate expects to find. The acting hero remains the placed subject, so no new targeting shape appears and no immunity question is added. Phase 7's phase-wide durability rule governs the outcome.

Acceptance checks:

- Red Needle's Skill action prints exactly one Radius icon, valued at the 4 its text already names, and everything else the card prints is unchanged: identity, Initiative 13, Secondary Movement 2, the Defense 1 reaction, ownership, and its hand slot.
- The Place branch's bound is the composed Radius Total rather than a number the definition carries. The total is published as a fact by a capability and bound through the ordinary node-output binding, as the actor's-space capability already publishes the actor's coordinate, so the Place reads the same total any other consumer would and no second derivation appears beside milestone 7.2b's.
- A matching Radius item widens that branch's offered destinations by exactly one ring, while the Push branch's fixed 3 spaces stays card text and does not move.
- A definition that asks for a card's Radius where the card prints none fails loudly rather than bounding anything at zero.
- Only Red Needle's content revision advances; the other nine configured cards keep theirs, and the compatibility-corpus decision the change requires is recorded with a byte-level delta measured at the pre-change revision.
- Focused tests cover the printed icon, the item-widened destination set at its boundary, the unchanged Push distance, the absent-icon refusal, and the snapshot, replay and retained-prefix Undo evidence applicable to what actually changes.

Non-goals: No change to the Push branch, to which modes this card carries, to any other configured card, to the Place operation itself, or to the ten-card matrix milestone 7.9 gates beyond this one bound's provenance. No new targeting shape, since the acting hero remains the subject; no Wasp content; no Radius presentation.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-72c-bind-red-needles-place-to-a-printed-radius-icon-completion-evidence).

### [x] 7.3 Add Ultimate continuous-source behavior

Observable outcome: A synthetic Ultimate can be unlocked as persistent state and supplies its rule only while its hero is on the board.

Acceptance checks:

- The Ultimate remains distinct from ordinary cancelable Active effects and ordinary hand/deck cards.
- Defeat/off-board state suppresses its applicability without deleting the unlocked source; legal respawn restores applicability.
- Unlock eligibility can require completion of the Tier III upgrade state without hard-coding a hero name.

Non-goals: No Wasp Ultimate text or final visual treatment.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-73-add-ultimate-continuous-source-behavior-completion-evidence).

### [x] 7.4 Resolve hero defeat, Lives, and respawn

Observable outcome: A successful synthetic Hero Attack resolves the complete general defeat consequences and later legal respawn.

Acceptance checks:

- Defeat, rewards, removal/prevention, unresolved-card resolution, effect cancellation, marker return, Life spending, and victory checks occur at verified checkpoints.
- The attacker gains coins equal to the defeated hero's current Level; eligible friendly heroes gain assist coins from the defeated hero's lowest card Tier.
- Life spending reads the defeated hero's current lowest non-Basic card Tier through the milestone 7.1 query.
- Defeating a token's source hero does not remove that token; markers held by the defeated hero return as required.
- Optional before-action respawn and resolving without action after declining are supported.
- Spawn-point and nearest-empty fallback use the full Phase 4 map.
- The Defense exception remains distinct.

ER2 note (`F-ER2-09`): when this milestone supplies verified reward derivation, relocate the hard-coded Melee/Ranged/Heavy coin literals and the standard-minion Defense modifier table out of the combat backend and defense query into configured content carrying their source citations; see the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2).

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-74-resolve-hero-defeat-lives-and-respawn-completion-evidence).

### [x] 7.5 Resolve mandatory end-of-round leveling and upgrades

Observable outcome: Every eligible synthetic hero buys as many consecutive levels as it can afford and completes the required upgrade, item, or Ultimate outcome for each level.

Acceptance checks:

- Level purchases are mandatory, spend the configured cost in order, and cannot skip a level.
- Each ordinary purchase upgrades one chosen lowest-Tier non-Basic card; tied choices and the two same-color next-Tier candidates use authoritative pending choices.
- The chosen upgrade enters the hand, the replaced card returns to the deck, and the unchosen candidate becomes the matching item using milestone 7.2.
- After all non-Basic cards reach Tier III, the next eligible level unlocks the milestone 7.3 Ultimate and no further levels are available.
- Multi-level and multi-hero ordering uses milestone 6.11's typed, serializable intra-stage system-transition continuation shape, with stable hero/level/choice identity and exact resume location; restore, replay, Undo, rejection, and stale input cannot repeat purchases, costs, upgrades, items, or Ultimate unlocks.
- Leveling adds no progression-specific callback, presentation-owned continuation, or Wasp-specific branch.

Non-goals: No Wasp costs, card identities, item art, or Ultimate text.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-75-resolve-mandatory-end-of-round-leveling-and-upgrades-completion-evidence).

### [x] 7.6 Resolve Pity Coin ordering

Observable outcome: After all leveling finishes, each hero that did not level this round receives exactly one Pity Coin.

Acceptance checks:

- Eligibility is tracked authoritatively for the current round and is independent per hero.
- A hero that bought one or more levels receives no Pity Coin.
- A Pity Coin that raises a hero to a level threshold cannot be spent until a later round.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-76-resolve-pity-coin-ordering-completion-evidence).

### [x] 7.7 Integrate and verify all three victory paths

Observable outcome: Final Wave, Throne capture, and loss of the final Life all end the match through one authoritative game-ending contract.

Acceptance checks:

- Final-Wave and Throne outcomes reuse milestone 6.10; final-Life loss reuses milestone 7.4.
- Each path records the winning team and exact reason, stops pending resolution, and prevents later rule steps.
- Focused scenarios prove each path independently and reject duplicate or conflicting winners.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-77-integrate-and-verify-all-three-victory-paths-completion-evidence).

### [x] 7.8 Integrate the exact end-of-round sequence

Observable outcome: The milestone 3.8c end-of-round state machine has every required handler populated, executes the completed systems in source order, and arrives at a valid next round or game-ended state without any synthetic unavailable-stage declarations.

Acceptance checks:

- The sequence runs Minion Battle; token removal/marker return; requested Lane Push; card retrieval/cancellation; mandatory leveling/upgrades; and Pity Coin collection at their exact checkpoints.
- Applicable End of Round effects and immediate victory checks occur at rules boundaries rather than presentation timing.
- A game-ending result stops later processing immediately.
- Focused scenarios cover no Lane Push, one Lane Push, level/no-level players, retrieved effects, Pity Coin eligibility, and a game-ending Push.
- The same state and choices produce the same state and ordered events.
- A focused two-round regression proves that completing a round removes that round's events from the current-round projection while retaining them in the whole-match event log and completed-round history; events accepted in the new round then append to a fresh current-round sequence. Restore, replay, and retained-prefix Undo reproduce the same boundary.
- Integration preserves the established resumable cursor, completed-round history, round-local reset, snapshot, Undo, and event-log contracts rather than introducing a second round-transition path.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-78-integrate-the-exact-end-of-round-sequence-completion-evidence).

### [x] 7.8a Add card-colour controls and compact hero overview overlays

Observable outcome: The configured match keeps the board dominant while a hero selected on the map opens that hero's colour-coded, current-round card tray; compact Red-left and Blue-right overviews show hero identity, item bonuses, four played-card colour slots, and discard state; one side `DIRECTOR` drawer contains the existing Undo, Reset, and Event Log tools; and every existing player-facing action, response, target, and mandatory choice is rehoused in the same coherent visual system with at most one usable interaction context on screen.

Approved visual reference: [milestone 7.8a UI mockup](../References/MILESTONE_7_8A_UI_MOCKUP.html). Its relative composition, compactness, ordering, labels intentionally omitted from the four overview slots, raised available-card treatment, and Director drawer are acceptance evidence; placeholder hero/card names, values, board geometry, and exact pixels are illustrative rather than content authority. The mockup specifies the standing match HUD; every contextual surface below inherits its typography, restrained chrome, spacing, colour treatment, focus/selection/disabled states, and board-dominant composition without treating the mockup's exact pixels as authority.

Player-facing transition context: no view currently draws a paused transition. That has been true since milestone 6.11 built rulebook p.8's Lane-Push respawn Place, which needed a blocked Spawn point to reach and so was never drawn; milestone 7.6 turned it from rare into ordinary, because p.8's Pity Coin leaves every hero able to afford the first level and p.15's Upgrade a Card then stops the end of round on a real choice from the second round onward. Milestone 7.6 gave the scene runtime a director command surface so the configured scene could pass its own end of round, and that surface names an option rather than choosing one; what is missing is the projection a person can see and pick from. This must precede milestone 7.9, whose acceptance checks require leveling to work inside a complete match and require a Win64 development build to complete a deterministic synthetic-match smoke path outside the Editor: a built player that reaches an end of round it cannot answer completes nothing. The final pitch treatment of contextual prompts remains milestone 9.13's.

Acceptance checks:

- Selecting a currently rendered hero through that hero's existing stable-identity map pointer changes only the director's local viewed hero and opens that hero's tray. It submits no match command, changes no authoritative state, and adds no second hero-selection box or clickable side overview.
- Every selectable card button in the viewed hero's tray uses its projected authoritative `CardColor` for the button surface while retaining readable card name and action/value text, its existing enabled/disabled and selected/focused states, and the exact typed invocation it already forwards. Presentation never infers colour from team, hero, hand position, display name, art, or action type.
- The tray sorts current-round cards left to right as already played cards in ascending turn order, then currently available cards, then discarded cards at the far right. Played cards say only `PLAYED · TURN <n>` and never name a round; discarded cards say `DISCARDED`; available cards say `AVAILABLE` and sit visibly higher than the other groups without changing hit targets, keyboard order, or legality.
- Played order comes from the authoritative current-round assignment history and discarded/available grouping comes from projected card lifecycle state. Retrieval, Defense discard, other implemented discard effects, and a round reset are not reinterpreted by the presenter.
- Red's overview is a small overlay aligned to the left screen edge and Blue's is its right-edge counterpart. Both remain clear of the configured card-selection and contextual-prompt controls, keep the board as the dominant surface, and remain usable at the project's validated desktop Game View and short-viewport fallback.
- Each overview shows the projected hero name and configured hero icon without using either to infer rules identity. The current configured one-hero-per-team scene draws one overview on each side, and the layout is a repeated hero component rather than a singleton tied to Wasp or synthetic display names.
- Item bonuses appear above the card slots as compact signed values paired with their printed-value symbols, such as `+1` beside Attack. Only authoritative current item bonuses are shown; presentation does not recalculate item ownership, slot validity, or totals, and no full item-name labels are added to the compact surface.
- Four fixed, left-to-right slots represent that hero's current-round turns. An occupied slot uses the played card's authoritative colour; an unplayed slot stays visibly empty. The slots contain no visible turn numbers, colour names, card names, or `ROUND CARDS` heading, while hover/focus and assistive labels may expose the turn ordinal and colour without changing the compact layout.
- The fifth slot is visually distinct and shows the hero's authoritative discard-pile state, including a compact discard label and count. It updates from the projection rather than from presenter-maintained card movement.
- A small side `DIRECTOR` button toggles a compact local drawer containing `UNDO`, `RESET`, and `EVENT LOG`. `UNDO` forwards the existing Undo operation, `RESET` is presentation terminology for the existing match Restart operation and requires its existing confirmation/safety behavior, and `EVENT LOG` toggles the existing collapsible diagnostic log; opening or closing the drawer changes no match state.
- One shared contextual interaction region immediately above, or temporarily in place of, the ordinary card tray presents the match's current decision. It covers commitment/Initiative choices; selected-card action offers including Movement, Attack, Skill, Hold/End Turn, Fast Travel, and Clear; Defense card or Decline responses and their contribution preview; effect modes, targets, selected subsets, Confirm and Cancel; Minion Battle removal; displaced-minion correction; Hero Respawn; and paused system-transition choices including leveling and Lane-Push respawn.
- While a mandatory response or pending choice stands, ordinary hand/card-selection and incompatible action controls are hidden or visibly inert and cannot be submitted. The authoritative projection decides what decision stands and which invocations exist; presentation does not establish a parallel priority order or infer that one control family supersedes another.
- Spatial decisions remain direct board interactions. The contextual region states what the player is choosing and exposes only projected non-spatial controls, while Movement, Fast Travel, Attack targets, effect targets, Place, Push, correction, Respawn, token, marker, and Minion Battle cues continue to resolve through their stable projected board identities and existing highlights.
- Defense retains its complete card name/printed value, Decline option, opaque Attack/Defense/Result preview, and supporting/hindering minion cues. The new style may change composition and appearance but cannot hide, clip, recompute, or weaken information the existing Defense surface already guarantees.
- Performable, selected, focused, hovered, disabled/unperformable, confirmable, and stale/refused states use one consistent visual vocabulary across cards, action offers, modal options, board-linked controls, and transition choices. Colour is paired with text, symbol, shape, or state treatment wherever the existing accessibility baseline forbids colour-only meaning.
- The round view model projects a paused system-transition step's chooser, its options, and the exact typed invocations the continuation already carries. Presentation adds no option, removes none, reorders none by a rule of its own, and chooses none.
- A leveling prompt identifies the projected hero, level gained, paid cost, remaining coins, and what decision remains. Each p.15 upgrade option names the card returned to the deck and the next-Tier card taken into the hand, plus the sibling card set aside and the printed-value symbol whose matching dashboard slot makes it an item; an Ultimate purchase identifies the unlocked Ultimate and that it replaces the ordinary upgrade/item outcome. These are application-projected facts from authoritative progression/card state and outcomes, not calculations or identity parsing performed by Unity presentation.
- Each non-leveling transition option is likewise labelled from authoritative projected content: p.8's Lane-Push respawn Place names the destination space. No label is produced by parsing an option or item identity, and none is inferred from display name, art, hero identity, or team.
- Answering forwards the existing `AnswerSystemTransitionStep` command carrying the invocation the continuation projected, and nothing else. A stale, forged, or refused option is rejected atomically with the pause still standing and the surface redrawn from current authority.
- The contextual End Turn control stays suppressed while a transition is paused, because the pause projects no stop; the existing all-options-refused rule is not reinterpreted.
- Milestone 7.6's scene-runtime command surface stops being the only way to answer. It is either replaced by the projected invocations or retained explicitly as a director/debug path, and the choice between those is recorded.
- Completed-round history, the current-round reset, card retrieval, discard, upgrade, restore, deterministic replay, and retained-prefix Undo all redraw the same colours, bonuses, slots, and discard count from current authority; none of this overview state is serialized or owned by Unity views.
- Focused presentation tests cover map-selected hero switching, Director drawer forwarding, all five card colours, team/card-colour separation, turn-order sorting, raised available cards, discarded-card placement, zero and multiple item bonuses, empty and four-card histories, single-context exclusivity, every existing contextual-control family named above, both paused transition kinds, an option the authority refuses, suppression of incompatible controls and End Turn, round reset, restore/replay/Undo reconstruction, and layout at the validated desktop and compact fallback sizes. Saved-scene Play Mode tests drive representative card/action/board-target, Defense, effect-choice, Minion-Battle, correction/Respawn, and leveling flows entirely through the restyled player-facing surfaces. A visible non-batch Game View sweep confirms readable contrast, no overlap, no clipped guaranteed information, and the approved reference composition across the standing HUD and contextual states.

Non-goals: No rules, progression, continuation, card-lifecycle, item, history, discard, action-offer, targeting, response, or pending-choice behavior changes; no new pending-choice kind, command, or second answer path beside projected invocations; no new hero-selection control outside the map; no camera controls; no production card art; no Wasp-specific card data; no animation, audio, tooltip card preview, final pitch polish, six-hero HUD, or private-hand networking work.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-78a-add-card-colour-controls-and-compact-hero-overview-overlays-completion-evidence).

### [x] 7.8a1 Decide an Attack offer's availability the way a Skill's is decided

Observable outcome: An Attack the rules can prove has no legal target is projected unavailable and drawn as the same disabled control an unstartable Skill is, instead of being drawn as an action that could be taken.

Context: milestone 6.5af gave `ActionOffer.IsAvailable` its meaning and produced the first `false`, for a Skill whose effect can apply nothing. `TurnResolutionAuthority.GetTurnActionOffers` still stamps only a Skill, so an Attack with nothing in range is offered enabled. Measured at milestone 7.8a: `StartAttackResolution` builds the target set, finds it empty, unwinds its own pending state and rejects as `TargetUnavailable`, leaving the turn active and the offers intact - so the control is dead rather than dangerous. 6.5af's own reason applies unchanged all the same: a player reads an enabled control as an action they may take, and this one is not. Found by the user in the milestone 7.8a HUD, where every offer is drawn from the projected flag.

Acceptance checks:

- The offer set and command resolution consult one statement of the judgement, so the two cannot disagree. Both reach the same `FindLegalAttackTargets`, and no second reading of Range, allegiance, adjacency, or immunity is introduced anywhere.
- An Attack whose legal-target set is empty is projected `IsAvailable = false`; an Attack with at least one legal target is unchanged. Every non-Attack offer is unchanged, and a Skill keeps the answer its own existing judgement gives.
- The judgement answers `true` wherever it cannot be asked at all, exactly as the Skill one does, so an offer is refused for being proven empty and never for being unjudgeable.
- The start command still refuses an Attack with no legal target, and still refuses it atomically. The rule does not move to the offer set: the offer becomes the earlier of two agreeing answers rather than the only one.
- Presentation changes nothing. The greyed control is milestone 6.5af's existing `UnavailableActionViewModel` path, and the round presenter's existing refusal of a start naming an unavailable action already covers a command surviving on an older model.
- Focused scenarios cover a non-Ranged Attack with an adjacent enemy and with none; a Ranged Attack in Range and out of it; a Ranged Attack whose availability changes with a p.16 Range item; an Attack whose only candidate is immune; and a Skill offer plus a Movement offer proving both are untouched.
- The same state produces the same offer set, and restore, replay, and retained-prefix Undo project the same availability.

Non-goals: No change to what a legal Attack target is; no new rejection reason; no change to Movement, Fast Travel, Clear, or Hold availability; no presentation change beyond what the projected flag already drives; no schema, wire, or command change.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-78a1-decide-an-attack-offers-availability-the-way-a-skills-is-decided-completion-evidence).

### [x] 7.8d Rename the coin ledger for what it now is

Observable outcome: The type that owns every hero's coin balance is named for the balances rather than for one of the three separate rules that move them, and nothing else changes.

Context: `DefeatRewardAccounting` was named when rulebook p.13's Defeat was the only rule that touched a coin. It now carries three operations and only one concerns a Defeat: the Defeat and assist award, milestone 7.5's p.15 level-up spend, and milestone 7.6's p.8 Pity Coin grant. All three live there because a second writer to a balance would be a second answer to what a hero can afford, so the type is right and the name is not. Milestone 6.13's naming audit is the precedent - a qualifier that has stopped describing its type is corrected in a pass of its own - and both 7.5 and 7.6 deliberately deferred it rather than bury a mechanical rename across every call site inside a progression milestone. It should precede milestone 7.9a's independent review so the review reads the final names.

Acceptance checks:

- The accounting type and its restore-state types are renamed to name the coin ledger. The awarded-outcome record that stops one Defeat paying twice keeps its Defeat vocabulary, because that part is genuinely about a Defeat.
- Every production call site, test, and living-document reference to the old names is updated in the same milestone. No alias, shim, obsolete forwarder, or partial rename is left behind.
- No wire format changes. The snapshot section is positional, so `CurrentSchemaVersion` does not move, the compatibility corpus keeps its `-v21` fixture names, and all eight pinned digests are unchanged - asserted as evidence that this is a rename rather than re-pinned as a consequence.
- Both aggregate-invariance digest families are unchanged for the same reason, and all three equality groups still hold.
- The cross-platform harness and both complete Unity suites pass with the same totals as the preceding milestone, since no test is added or removed.

Non-goals: No behaviour change of any kind; no new coin source or sink; no change to who may spend, grant, or award; no schema move; no re-pinning; no renaming of the Defeat outcome, removal, or reward-schedule types that are still about a Defeat.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-78d-rename-the-coin-ledger-for-what-it-now-is-completion-evidence).

### [x] 7.8e Shard the three living documents that sit at their thresholds

Observable outcome: `SESSION_BRIEF.md`, `Project/CURRENT_IMPLEMENTATION.md`, and `Rules/RULE_SYSTEMS.md` are each comfortably under their thresholds because authority moved to indexed shards, not because prose was compressed again.

Context: all three have warned at 85% on consecutive milestones, and each has been brought back under its line by compression rather than by routing - milestone 7.6 spent a substantial part of its handoff trimming construction history and restatements out of them, which is effort spent on the documents rather than on the work. `DOCUMENTATION_GOVERNANCE.md` already names the remedy: "moving precise active authority to an indexed shard is the normal response to sustained growth, not compression", and it forbids the alternative, since "a validator cannot become passable merely by deleting content". This is scheduled as its own milestone because the next one to touch these subjects would otherwise pay the same cost again.

Acceptance checks:

- Each of the three documents ends below 75% of both its word and byte thresholds, and the handoff records the remaining headroom for each.
- Every statement that moves is relocated verbatim or replaced by a more precise statement in its new home. Nothing is deleted to fit, and the `duplicate-history`, code-truth, and archive checks still pass.
- Each new shard is indexed by the family index that owns its subject, carries that family's threshold, is reachable from the brief's targeted routing table, and states what it owns at its head.
- The brief keeps only current authorization, the exact next-milestone route, and a compact implementation summary, which is what it already claims to be.
- No governed document is left above its warning threshold, and the validator reports zero warnings.
- The living-document validator and `Validate-BoardMapSpecification.ps1` both pass; no Unity validation is launched, because no source, test, scene, or asset changes.

Non-goals: No content deletion or weakening; no threshold change or governance amendment; no rule, behaviour, or test change; no archive rewriting beyond append-only routing; no renumbering or rescoping of any milestone.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-78e-shard-the-three-living-documents-that-sit-at-their-thresholds-completion-evidence).

### [x] 7.9 Complete a deterministic test-card core match

Observable outcome: The configured ten-card mechanics-showcase hands can start a match, play legal Movement/Hold/Attack/Defense/Skill turns, exercise every source-reviewed reusable core operation and delayed lifetime, complete rounds, resolve minions and Lane Pushes, progress, and end through every victory path.

Acceptance checks:

- Every general core rule used by the loop links to its authoritative source and focused tests.
- The exact configured primary-action matrix is Red Needle Push/Place Skill, Red Bulwark modifier/restriction/permission Skill, Red Lancer Ranged Attack, Red Pursuit non-Ranged Attack, Red Bombard Direct-Move/Spawn Skill, Blue Feint immunity/effective-type timing plus place-token and give/return marker Skill, Blue Sentinel Swap/Replace/Remove Skill, Blue Scout Shot Ranged Attack, Blue Hammer non-Ranged Attack, and Blue Longbow bounded-Repeat Skill. All ten retain their stable definition/instance identities, approved Initiative values, printed Secondary Movement, Defense reactions, ownership, and five-card-per-hero shape under explicit current content revisions. Blue Feint's last three modes are milestone 6.2's component capabilities, which the configured scene reached through a sixth card between milestones 6.2d and the five-card scene; the gate proves them from the hand rather than from a fixture, and proves that Feint's placement is what supplies the Hazard Blue Sentinel's Replace and Remove take within the same round, which is why the placement is not on Sentinel itself.
- General Hold remains derived from a selected unresolved card, Fast Travel remains a replacement only for an eligible Movement offer, and Clear remains a replacement only for an eligible Attack; none becomes invented printed text on a showcase Skill. At least one non-Ranged and one Ranged printed Attack remains configured for each team.
- Initiative, Tie Breaker flips, turns, rounds, immediate/end-of-round Lane Pushes, retrieval, leveling, respawn, and victory work together.
- Synthetic effect scenarios exercise direct Move, Object Push, Place, Spawn, board-object Swap/Replace, standalone Remove, Defeat, and Clear through the same command, timing, pending-choice, snapshot, replay, Undo/restart, and event-log boundaries used by the match; Object Push and Lane Push remain observably distinct.
- Synthetic Skill, ordered/optional/modal/Repeat control flow, modifiers, restrictions, permissions, immunity, and effective-characteristic scenarios prove that no source-reviewed general keyword or reusable core capability is deferred for first implementation to a verified hero milestone.
- The actor-affecting modifier/restriction/permission sources created by Red Bulwark's Skill exercise `This Turn`, `This Round`, and `Next Turn`, while the actor-affecting immunity/effective-type sources created by Blue Feint's Skill exercise `This Round` and `Next Turn after Playing Cards`; together they prove all four lifetime forms through creation, dormant/active applicability, triggering, expiration, source-card cancellation/retrieval, defeat/off-board interaction where declared, simultaneous ordering, snapshot/restore, replay, and retained-prefix Undo. The gate proves the card remains source identity, the hero remains the affected rules object, and the lifetimes are authoritative rather than presenter timers or one generic delayed flag.
- Synthetic Phase 7 colors, tiers, upgrade relationships, items, and Ultimate sources layer onto the same showcase definitions/instances without replacing their primary-action identities or inferring progression from display names, art, team, or hand slots.
- Snapshots, Undo, restart, and the event log remain deterministic across the complete loop.
- The deterministic replay oracle covers representative complete matches and bounded multi-round generated/soak scenarios with fixed reported seeds; live, replayed, restored-and-continued, and Undo-truncated paths produce matching command/event/projection/snapshot/history evidence.
- The complete-match runtime capture samples accepted-command projection refresh time and managed allocation at fixed reported command-history lengths, including an empty/short history and the complete-match history. It records the samples and growth against history length, and compares the complete-match result with the standing 25 ms refresh-time and 64 KiB managed-allocation investigation thresholds in `Testing/FUTURE_GATES.md`.
- The maintained snapshot compatibility corpus covers the complete synthetic-loop state, and every schema/content incompatibility remains an explicit typed migration or rejection decision.
- A disposable Win64 development build of the exact gate revision launches outside the Editor, starts and completes a deterministic synthetic-match smoke path, writes usable diagnostics, and exits cleanly. The artifact is validation-only and is neither retained as the pitch build nor distributed publicly.
- Synthetic definitions are visibly test-only and contain no claim about Wasp values or effects.
- The extra Focus/Cache fixture-only cards either retain a documented distinct isolation role for a lower-level effect/component contract or are retired with their dependent tests migrated to the configured showcase; they do not enlarge either normal hand beyond five or remain as unexplained duplicate coverage.
- A broader deterministic core-loop suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

ER2 note (`F-ER2-08`): `Undo`/`Restart` reconstruct by full-prefix replay, so their cost grows with match length; this gate's runtime baseline must measure Undo at complete-match length and record a bound. The `RISK-PERF-001` row reopened after its 6.1o retirement is also open because projection construction copies growing command/current-round histories. Neither mechanism schedules optimization by inspection: milestone 7.9 records the pitch-scale measurements, and a separately approved corrective milestone is proposed only for a measured threshold crossing. Both crossed, and milestone 7.9b is that corrective milestone.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-79-complete-a-deterministic-test-card-core-match-completion-evidence).

### [x] 7.9b Reduce what a complete match costs to refresh and to undo

Blocked by: nothing. It runs after milestone 7.9 and before milestone 7.9a, which is where `RISK-PERF-001`'s recorded disposition places the smallest corrective milestone a measured crossing schedules.

Observable outcome: an accepted command costs the same at the end of a complete match as at the start of one, and an Undo at complete-match length costs a bounded fraction of what a full-prefix replay costs today.

Context: milestone 7.9 measured the two mechanisms that had only ever been reasoned about, and both crossed. `RISK-PERF-001` predicted that projection construction copies a growing accepted-command history; holding one command kind fixed across a 502-command match, median managed allocation rises from 281,424 B over history lengths 0-13 to 387,008 B over 487-496, about 210 bytes per command of retained history. `F-ER2-08` predicted that `Undo` and `Restart` reconstruct by full-prefix replay; that measures 429 ms on Unity's Mono at complete-match length against 28.203 ms over the two-round flow the 250 ms threshold was set from. Neither is a playability problem at 1v1 scale - an accepted command refresh is 0.4 ms on the same runtime - and neither authorized a fix by inspection, which is why nothing was optimized inside 7.9. What discharges the interim acceptance is the measurement, and the user approved scheduling this on 2026-08-19+1 rather than accepting the figures outright. `RISK-SCALE-001` owns the separate question of what six heroes do to the same two numbers, and this milestone must not pre-empt it by optimizing for a scale nothing has run at.

Acceptance checks:

- The per-accepted-command growth term is removed or bounded: one command kind's median managed allocation at complete-match history length is within the noise of its median at an empty history, measured by the existing `CompleteMatchRuntimeCaptureTests` sampler and its recorded seed.
- An Undo at complete-match history length no longer replays the whole prefix. Its cost is recorded against the 4.4d two-round figure and the 7.9 complete-match figure, and `Testing/FUTURE_GATES.md` carries the new baseline with the superseded rows kept.
- Every determinism guarantee the complete-loop gate proves still holds exactly: live, replayed, restored-and-continued, Undo-truncated and restarted paths produce matching command, event, projection, snapshot and history evidence, and the retained-prefix Undo contract is unchanged in what it means.
- The authoritative-state and immutable-projection contracts are preserved. A retained intermediate state is a derived cache that a restore, a replay and a rollback can each discard and rebuild; it is never a second answer to what the match is.
- The `RISK-PERF-001` row reopened after its 6.1o retirement, and `F-ER2-08`, are retired with their recorded bounds, or the residual is accepted explicitly with its measurement.
- T2 validation: focused runtime-capture and determinism regressions plus the complete-loop suite; both unfiltered suites only if affectedness or the risk triggers require them.

Non-goals: No change to what a command means, to the schema, to the projection's public shape, or to any rule. No 3v3-scale optimization, which is `RISK-SCALE-001`'s and milestone 8.27's. No speculative work on any figure milestone 7.9 measured as inside its threshold - refresh time, snapshot capture, and the steady-state frame metrics are not this milestone's subject.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-79b-reduce-what-a-complete-match-costs-to-refresh-and-to-undo-completion-evidence).

### [x] 7.9a Independently review the complete synthetic core

Blocked by: Milestone 7.9 must pass its deterministic synthetic-match T3 gate, and milestone 7.9b must close the two threshold crossings that gate measured, so the review reads the corrected figures rather than the crossing ones.

Observable outcome: A fresh analysis-only review determines whether the complete synthetic rules loop is correct, maintainable, deterministic, performant enough for the prototype, and architecturally ready for verified hero content.

Acceptance checks:

- The review follows complete-match paths across setup, turns, pending choices, combat, reusable direct Move/Object Push/Place/Spawn/Swap/Replace/Remove/Clear operations, minions, tokens/markers, Lane Push, progression, victory, snapshots, deterministic replay, Undo/restart, and the event log using actual source and gate evidence.
- It checks for state duplication, hidden coupling, temporary Phase 1-7 structures that outlived their purpose, rules or presentation authority leakage, persistence/replay divergence, complete-match allocation or resource-growth risks, and test suites that could provide false confidence.
- It explicitly assesses whether the generic card/effect contracts remain suitable for real verified content without claiming that unknown cards require no extension.
- It identifies any source-reviewed general keyword or reusable capability still awaiting first implementation; such a gap blocks verified hero content rather than being reassigned to Phase 8.
- Findings and preserved strengths follow the common independent-review evidence contract; fixes become separately approved corrective milestones and are not implemented here.
- The existing hard-gate roadmap audit of Phase 8 entry is completed as a distinct section of the same review record, including remaining-effort range and shortest dependency-safe path.

Non-goals: No Wasp inventory, source interpretation, card implementation, runtime fix, architecture rewrite, production optimization, or weakening of the synthetic-core gate.

Hard gate: The complete Wasp inventory and Wasp-specific implementation do not begin until milestone 7.9a and every gate-blocking corrective milestone are reviewed and approved.

Hard-gate roadmap audit: As part of milestone 7.9a, audit readiness for verified Wasp content before authorizing the next milestone. Confirm that the synthetic core loop has retired the intended architecture and integration risks, identify unresolved prerequisites that must remain ahead of Wasp implementation, recommend any justified changes to Phase 8's inventory/mapping entry sequence, and record the remaining-effort range. This audit cannot waive milestones 8.1 or 8.2 or authorize Wasp-specific implementation.

Historical evidence: [Phase 7 handoff](../Archive/Phase-Handoffs/PHASE_7.md#milestone-79a-independently-review-the-complete-synthetic-core-completion-evidence).

### [~] 7.9c Close the four confirmed rules-surface defects from the 7.9a review

Blocked by: nothing; it is the gate-blocking corrective milestone 7.9a proposed, and the Phase 8 hard gate stays closed until it completes.

Observable outcome: a hero Defeat that leaves its side without an effective minion in the Battle Zone produces p.8's immediate mid-turn Lane Push exactly when the defeated hero itself counted as a minion and never otherwise; the two production system transitions cannot be begun by a raw director command; and the HUD never draws an `END TURN` control beside a mandatory Hero Respawn choice.

Context: milestone 7.9a confirmed all four defects from source, with the full evidence in its completion commit. The Attack backend mints a hero Defeat's Lane-Push qualification through the shared removal decision and `RecordAttackLanePush` then reads only the minion arm, so the recorded cause has no consumer (F-BOARD-01) - reachable with shipped content through Blue Feint's counts-as-minion mode. The qualification query itself never asks whether the removed unit counted as a minion (F-BOARD-02), which today is masked by the first defect; the two corrections land together because routing the cause without the guard would emit false pushes for ordinary hero Defeats. `TryBeginSystemTransition` accepts the two production definitions its own contract comment excludes, so a raw command runs p.15's mandatory purchase outside p.8's step and corrupts the Pity-Coin record in both directions (F-PROG-01). And the End Turn derivation's hand-maintained pending-decision list omits `PendingHeroRespawnChoice` while its sibling includes it (F-PRES-01).

Acceptance checks:

- `RecordAttackLanePush` records the hero arm's cause, and `LanePushQualificationQuery.AfterRemoval` refuses to qualify a removed unit that did not count as a minion, in one change. `ImmediateLanePushTests.HeroDefeatOfTheLastCountsAsMinion_EmitsTheMidTurnRequest` and `ImmediateLanePushTests.HeroDefeatOfAnOrdinaryHero_RecordsNoCause_EvenInAnEmptiedBattleZone` are both observed failing at the pre-fix revision and passing after.
- `TryBeginSystemTransition` refuses the Lane-Push and level-up definitions under a typed reason that says the transition is begun by its own rules, before the transition authority is asked. `SystemTransitionContinuationTests.BeginningAProductionTransitionByCommand_IsRefused` is observed failing pre-fix (the command is accepted and levels move) and passing after, and the refused command leaves every hero's Level, coin balance, and the projection's pending step unchanged.
- The round presenter and the End Turn derivation share one pending-decision predicate, so a pending kind can no longer be added to one list only. `MatchFeaturePresenterTests.RoundPresenter_WithholdsEndTurnWhileAHeroRespawnChoiceStands` is observed failing pre-fix (a non-null invocation whose execution the authority refuses) and passing after.
- Determinism evidence is unchanged: the three reported seeds and the eight-seed soak reproduce their recorded endings, all pinned corpus digests are byte-identical (no schema cause exists), and the aggregate-invariance equality groups hold.
- T2 validation: the four named fixtures, the complete-loop suite, and directly affected Lane-Push, hero-Defeat, system-transition, and presenter regressions. The corrective sequence's complete unfiltered sweep is milestone 7.9d's closing validation, by the user's recorded decision.

Non-goals: no publishing of a projected turn-ending offer (the recorded root cause of F-PRES-01's class, Phase 8-scale work); no change to the Minion Battle, the Lane-Push stages, or the continuation contract itself; no schema move; no correction of any other 7.9a finding.

### [ ] 7.9d Restore the guards verified content will lean on

Blocked by: milestone 7.9c, so the corrective sequence ends on this milestone's complete unfiltered sweep.

Observable outcome: the five guard gaps the 7.9a review recorded are closed - the cross-runtime snapshot-digest pin runs on the harness tier, the Unity tiers carry a growth assertion for 7.9b's mechanism, setup refuses a hero deck missing a named next-Tier instance, a malformed effect definition is refused before a match starts, and the risk register's schema claim is mechanically pinned.

Context: none of these is a defect today; each is a guard whose absence the review demonstrated - the digest pin discovers zero harness tests for one `UnityEngine.Application.dataPath` line (recorded failing at `587401e`), the pre-7.9b copying mechanism passed the Unity tiers' absolute 25 ms line at 2.7 ms, a hand-authored deck gap would silently skip p.15's mandatory purchase and still collect p.8's Pity Coin, `EffectDefinitionValidator` has had no production caller since 6.2b recorded F62B-05, and the `RISK-COMPAT-001` row went stale across twelve schema moves before 7.9a brought it current.

Acceptance checks:

- `CardAggregateInvarianceTests` resolves its diagnostic output directory without `UnityEngine`, leaves the harness exclude list, and its nine measured digests match across both runtimes; the pre-fix half is the zero-discovery observation recorded at `587401e`, and a cross-runtime mismatch is raised as a new confirmed defect rather than re-pinned. `Testing/DOTNET_HARNESS.md`'s exclude-list sentence drops its hand-counted integer in the same edit.
- `CompleteMatchRuntimeCaptureTests` gains a median-time ratio assertion beside its absolute refresh line, proven non-vacuous on a deliberate violator - a temporarily reintroduced per-command history copy observed failing the assertion in a scratch working tree - and passing at the closing revision; the assertion's limits against mechanisms too cheap to show in time are recorded beside it, because the Unity tiers cannot measure allocation at all.
- `HeroItemSlotSetupValidator` requires every named next-Tier candidate reachable from a hero's owned cards to exist as that hero's deck instance. `DashboardSlotsAndComposedItemTotalsTests.AHeroWhoseDeckIsMissingANamedNextTierCard_IsRefusedAtSetup` is observed failing pre-fix and passing after.
- Every configured `ActionDefinition.EffectRoot` is validated by `EffectDefinitionValidator` from the same configuration constructor that runs the two setup validators, against the catalog the match composes. `AMalformedEffectDefinition_IsRefusedBeforeTheMatchStarts` is observed failing pre-fix (configuration succeeds) and passing after.
- The `RISK-COMPAT-001` register row's schema claim is pinned through `Tools/PinnedConstants.txt`, with the `stale-pinned-constant` check observed failing on a deliberately mis-stated scratch copy of the row and the validator passing at the closing revision.
- Closing validation, by the user's recorded decision: both complete unfiltered Unity suites and the cross-platform harness green at the closing revision, with the known-good baseline table in `Testing/PLAYMODE_AND_SUITES.md` updated and the moved totals explained by the tests 7.9c and this milestone added.

Non-goals: no work on the three general card-text seams (F-CARD-01, F-CARD-02, F-CARD-03 - sequenced at milestone 8.2 from Wasp's verified text); no harness NUnit-version alignment; no schema move; no correction of any other 7.9a finding.
