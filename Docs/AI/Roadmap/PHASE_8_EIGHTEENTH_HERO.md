# Phase 8 - Eighteenth-hero dealing: Rowenna

Return to [roadmap](../ROADMAP.md). Current authorization and next route live in [the brief](../SESSION_BRIEF.md).

## Authorization and scope

On 2026-09-19 the user ordered the eighteenth hero after the victory-screen package: "After the build start implementing Rowenna." This authorizes **8.57 -> 8.57a -> 8.57b -> 8.57c -> 8.57d -> 8.57e -> 8.57f -> 8.57g -> 8.57h**, through a fully selectable, artworked hero with working printed cards and the permanent player-path gate. Execute sequentially with separate validation and local commits. Necessary corrections demonstrated by those checks belong to the block that found them.

[Rowenna's rules shard](../Rules/HERO_ROWENNA.md) owns source facts and the capability map. Eighteen faces are dealt; Earn the Spurs is the boxed p.20 Handicap. Inspect existing contracts before adding vocabulary. New contracts stay outside production registration until reachable from dealt content; reconcile each new or widened shared-foundation claim with the test-owned claim catalog. The printed colour conflict RO-03 is carried as an open user question and does not block the series.

### [x] 8.57 Inventory Rowenna and map the implementation sequence

Outcome: source-backed identity, nineteen-face inventory, exact text/values/items/upgrades, capability map and outstanding dependencies are recorded.

Acceptance: inspect all fronts, profile and dashboard; reconcile front/back counts and hashes; search the entire local rulebook for hero-specific authority and inspect relevant general-rule pages; distinguish printed facts, verified general rules, implementation predictions and unresolved interpretations; record RO-03 with both readings and the working assumption. Record the complete bounded sequence. T0 documentation validation only; no Unity.

Non-goals: executable content or mechanics; adopting unverified community rulings.

Completed 2026-09-19. All nineteen fronts, the selection card and the dashboard were read; the twelve item icons were montaged beside Wasp's six verified icons, the ribbons and corners were montaged, and the profile bars were read by scanline (Attack 8, Initiative 4, Defense 7, Movement 3 + 2). The nineteen backs share one hash. The rulebook names her nowhere; p.10, p.13, p.15, p.19 and p.20 govern. One printed conflict is held open for the user (RO-03: four faces whose corner letter contradicts their ribbon; the ribbon colour is assumed). The capability map predicts eight new pieces (empty-hand narrowing, a scoped assist-coin multiplier, the swap's two-qualifier narrowing, the gauntlet's self-place narrowing and opponent-owned one-space move, minion placement after Movement with "Choose up to two times", the both-parties empty-discard predicate, and the Ultimate's permanent Ranged-with-Range-2 rule) over the existing retrieve, coin, defeat, swap, place and repeat contracts.

Historical evidence: [8.57 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857-rowenna-inventory-completion-evidence).

### [x] 8.57a Support retrieve and reward for another hero, and the rider-gated repeat

Outcome: a card can let a named other hero retrieve their own discarded card or collect coins, chosen by whom the text says; "if they do" reads that rider's performed output; an after-attack repeat can be gated on it and exclude the first target.

Acceptance: both parties retrieving on Code of Chivalry and Earn the Spurs with each hero choosing their own card and able to decline; a friendly hero in radius chosen by Rowenna gaining 1 coin or retrieving on the Reds; the Paragons' repeat offered only when the rider was performed, on a different target, never a third time; no hero retrieving another's card; declined riders and empty discards; snapshot/replay continuations and narrow foundation claims. T2 minimum.

Non-goals: swaps, defeats, the Ultimate, dealing.

Completed 2026-09-19 (`686af16e`). No new capability and no new handler type: `retrieve-card@1` (owner = chooser = the retrieving hero), `synthetic-radius-hero-target@1` (AlliedWithSource, includeSelf unset, the card's own Radius), `gain-resource@1`, `synthetic-is-flagged@1` over a Conditional, `synthetic-attack-took-a-hero@1` and Thunder Boomerang's `RepeatTargetBinding.DifferentTarget` declaration compose all five faces. Two optional OUTPUTS were added - `attackTargetHero` on the took-a-hero predicate (8.40c forbids a branch binding its own Conditional's output) and `didGrant` on `gain-resource@1` (8.40i's `didRetrieve` for coins) - published only when declared, so no digest moved. `RowennaEffectContent` / `RowennaCardContent` carry six faces under the placeholder revision `rowenna-8-57a-prep`, registered nowhere. Readings settled: RO-Q7 (step 1 already knows the Attack's target), RO-Q7b (nobody in radius = p.10 stop, no repeat, the Attack not greyed), RO-Q7c (an empty-discard hero is not offered), RO-Q7d (each "may retrieve" is its hero's own decline on that hero's prompt). `RowennaRewardTests` 25 cases; harness 3450/3450; focused Edit Mode 111/111 over eleven fixtures, no pin moved. Noted for 8.57b: `EffectChooserRole.BoundHero` reaches only `Choose` nodes today, and the unit selector arm has no `includeSelf`.

Historical evidence: [8.57a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857a-rowenna-reward-completion-evidence).

### [x] 8.57b Support the vanguard's swap and the gauntlet

Outcome: Rowenna swaps with a friendly unit in range that is adjacent to an enemy hero or has a card in the discard; Throw the Gauntlet places her adjacent to an enemy hero in range, hands that hero an optional one-space move, and pays her 2 coins only when they took it.

Acceptance: candidate narrowing on both qualifiers with minions qualifying by adjacency only; Defend the Innocent's own retrieve; self-place destinations in range and adjacent to an enemy hero in range with no destination inventing range; the enemy hero as the mover's decision owner across seats and the wire; the conditional coins; immunity, occupied destinations and stale answers. T2 minimum.

Non-goals: defeats, minion placement, dealing.

Completed 2026-09-19 (`71307821`). The three Blues compose on the printed-icon UNIT selection `synthetic-range-unit-target@1` (AlliedWithSource; the unit arm already excludes the actor, so 8.57a's `includeSelf` note is answered by measurement) with ONE new optional parameter `adjacentToAnEnemyHeroOrHoldingADiscard` (one parameter because the printed "or" joins two qualifiers inside one reach - Whisper's `choose-swap-subject@1` joins two REACHES and was the wrong contract, RO-Q8a), `synthetic-swap-actor@1` as the selection's consumer, and `retrieve-card@1` for Defend the Innocent. Throw the Gauntlet asks the hero first, then the space (RO-Q8): `synthetic-range-hero-target@1` with the Place as its consumer, a NEW deferred declaration `synthetic-place-in-range-beside-a-named-hero@1` over the existing `PlaceHandler` (`PlaceDestinationQualifier.AdjacentToTheNamedSpace`, required `besideSpace`), then `synthetic-direct-move@3` as a `May` Capability node with a new optional `chooser` (the enemy hero rides to the pause as `EffectChoiceRequest.ChooserHeroId`, the field 8.35r already routes; the actor is refused as the wrong chooser - RO-Q8c; a hemmed-in hero is never asked - RO-Q8d) and a new optional `didMove` output gating `gain-resource@1`'s 2 coins. No handler type; no digest moved; `SyntheticPlacementCapabilities.DeferredHandlers` holds exactly the one id until 8.57g. Two neighbouring fixtures corrected (the reward fixture's registered-catalog assertion split; `PlacementLineQualifierTests`' empty-deferred assertion narrowed). `RowennaSwapAndGauntletTests` 28 cases; harness 3479/3479; focused Edit Mode 138/138 over thirteen fixtures. The `Optional`/`BoundHero` gap stays open and unneeded.

Historical evidence: [8.57b completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857b-rowenna-swap-and-gauntlet-completion-evidence).

### [x] 8.57c Support surrender

Outcome: an effect-invoked Defeat narrowed to an adjacent enemy hero with no cards in hand, and a Defeat whose assist coins are tripled for Rowenna's team-mates.

Acceptance: hand means hand (played, resolved and discarded cards do not count); the ordinary p.13 Defeat consequences (Life counters by lowest Tier, Defeat coins by level to Rowenna, assist coins by Tier to her team-mates) and Glorious Triumph's tripled assist coins at the one spend site through both entrances; 8.53's scoreboard credits the Kill to Rowenna; immunity and a hero who draws a card before resolution. T2 minimum.

Non-goals: minion placement, the Ultimate, dealing.

Completed 2026-09-19 (`d792482d`). Both faces compose on `choose-target@2` (origin Actor, reach 1 as a LITERAL - neither face prints a Range or Radius icon, Silverarrow's 8.35z46 lesson - unitKind Hero, relation OpposingSource) and `synthetic-defeat-hero@1` (8.22's effect-invoked p.13 Defeat). Deltas, all optional: `choose-target@2` gains `requiresEmptyHand` (a BOOLEAN, because an unbound optional Integer reads as zero and a `maximumCardsInHand` of 0 could not be told from no narrowing; named beside 8.40f's `minimumCardsInHand`, declared on the roster arm only; `HeroHandQuery.IsEmpty` beside `HasAtLeast`); `synthetic-defeat-hero@1` gains `assistCoinMultiplier` (input and published output - it rides with the REQUEST, since the clause is true of one Defeat and a modification set derived at the command boundary cannot say "this one"); `RequestedHeroDefeatQuery` returns `RequestedHeroDefeat` (hero + multiplier) threaded to `ResolveHeroDefeat`'s eighth argument, p.14's step 6 passing `UnmultipliedAssistCoins`; `HeroDefeatRewardSchedule.AssistCoins(tier, multiplier)` is one grant, not a second payment. No new capability, handler type or deferred id. Readings: RO-Q3 confirmed end to end (assist I/II/III -> 3/6/9; Defeat coins and Life counters unmoved), RO-Q9 (hand means hand), RO-Q9a (the clause narrows who may be chosen; nobody qualifying greys the Skill), RO-Q9b (a hero who gains a card between offer and answer is p.10's impossible step, not a refusal), RO-Q9c (RO-03 changes nothing composed). Four neighbouring fixtures corrected. `RowennaSurrenderTests` 30 cases; harness 3509/3509; focused Edit Mode 308/308 over 27 fixtures; no pin moved.

Historical evidence: [8.57c completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857c-rowenna-surrender-completion-evidence).

### [x] 8.57d Support close quarters and the opening shots

Outcome: after a Movement action ending adjacent to an enemy hero, Rowenna may place a friendly minion in radius adjacent to that hero or an enemy minion in radius adjacent to herself, once or up to twice; and an enemy hero in radius with an empty discard, while hers is empty too, discards a card, once or with one repeat.

Acceptance: the adjacency predicate read after the Movement action (not after a card-text move); one named enemy hero when several are adjacent; both bullets' destination sets, occupied and out-of-radius spaces, minion identity preserved; Grand Melee's two choices in any combination; both-parties empty-discard predicate on each party's discard alone; the discard chosen by that hero, "if able" on an empty hand; Opening Volley's repeat against another eligible hero. T2 minimum.

Non-goals: the Ultimate, dealing.

Completed 2026-09-19 (`b9a92e19`). No new capability, handler type or handler code. The Greens compose on `CardTextTiming.AfterTheMovement` (8.35z31's timing, Ursafar's seam - a zero-space walk still applies the text, a card-text move never reaches it), `presence-count@1` under a Conditional for "if you are adjacent to an enemy hero" (a presence, naming nobody - RO-Q4), a `Choose` modal (min 1 / max 1) under `Optional` for "you may" and, for Grand Melee, TWO SIBLING SUBTREES for "Choose up to two times" (there is no loop node and `RepeatAction` would repeat the Movement; Wasp's Kinetic Push precedent; each performance carried out before the next is offered), `synthetic-radius-minion-target@1` for both bullets' subjects (the card's own Radius; activity Place), `choose-target@2` with reach 1 WRITTEN for "that enemy hero" (a landmark, nothing is done to him; the minion is named BEFORE the hero so 8.35l's first-step walk greys the bullet correctly), `synthetic-place@2` for bullet 2 (Telekinesis word for word), `synthetic-discard-chosen-card@1` (IfAble) and a May `RepeatAction` (max 1, Unbound) for Opening Volley. Deltas: the deferred declaration `synthetic-place-in-radius-beside-a-named-hero@1` (the printed-RADIUS twin of 8.57b's Range declaration over the same `PlaceHandler` - a separate id because one declaration spanning both would let a Range item widen a Radius card; the first arm whose subject is not the actor, so immunity and 8.7's place restriction are asked about the minion; the Radius bound re-read is provably non-binding) and the optional Boolean `bothDiscardPilesEmpty` on `synthetic-radius-hero-target@1` (the actor's half empties the whole offer; each candidate's own pile removes only that candidate). Readings: RO-Q10 (Grand Melee's second performance of bullet 1 names its OWN "that enemy hero"); a declared boundary measured rather than assumed - neither bullet-1 selection can declare a consumer (`choose-target@2` implements no selection-consumer interface; the Place's `besideSpace` is unpublished when the minion is offered), so a hero with no empty neighbour IS offered and naming him stops the bullet at the Place. Four fixtures re-measured (`choose-target@2` census 59 -> 63; composed bodies 12 -> 17; the deferral list now her two). `RowennaCloseQuartersTests` 42 cases; harness 3551/3551; focused Edit Mode 137/137; no pin moved. Seventeen of nineteen faces are composed; 8.57e owes the Ultimate and 8.57f the boxed Handicap and the set.

Historical evidence: [8.57d completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857d-rowenna-close-quarters-completion-evidence).

### [x] 8.57e Support Fabled Lance

Outcome: Rowenna's Attack actions are Ranged, target a unit in range and read a printed Range of 2 once her Ultimate is unlocked.

Acceptance: every Attack face targeting at Range Total 2 plus Range items; Ranged Block conditions answering her and non-Ranged ones no longer; the attack rail and defense preview reading the same range; the composed Range Total on the wire; no change before unlock or for any other hero. T2 minimum.

Non-goals: dealing.

Completed 2026-09-19 (`20f5576f`, rebased from `262e1089` onto the morning's docs commits). One family for one sentence: `RuleModificationKind.AttackActionsCountAsRangedAtPrintedRange` (13) with `RuleModification.ForAttackActionsCountAsRangedAtPrintedRange(int)` carrying the printed Range in `Amount` (no new property, no digest moved). Not a `ValueModifier` (p.16 raises an icon, never a total, and her Attack faces print no Range icon - a +2 contribution would leave her Range item matching nothing) and not `CountsAs` (p.17 replaces a unit type): the override is a `PrintedIcon` contribution injected in `RangeValueQuery.Calculate` before `ItemBonusQuery`, so Paragon of Grace's Range item makes her reach 3 exactly once. One query, `RangedAttackOverlayQuery`, read at three seams: the backend's `FindLegalAttackTargets` (ordinary and modal-bullet paths, and `CanFindLegalTarget`'s feasibility through the same method), `RangeValueQuery`, and `DefenseBlockQuery.Evaluate` (now taking `attackerId` + `modifications`, so the resolver and `MatchSession.CreateDefensePreview` reach one verdict by construction). Readings: the Ultimate replaces the REACH only (`AdjacentOnly` -> in range; unit kinds, p.19's line and 8.19a's binding stay printed); Ledger Q12's item reading composes once (2 + one item = 3, never 4); two printed-icon declarations take the larger. `RowennaEffectContent.FabledLanceRule()` on `UltimateRuleDefinition.PermanentAbility` (new optional `printedValues`); `RowennaCardContent` gains Fabled Lance (Tier IV, Range 2, no action definitions - `RowennaPrimaryKind.None`). Eighteen of nineteen faces composed; nothing registered; deferral list unchanged; schema and wire unmoved. `RowennaFabledLanceTests` 18 cases; harness 3569/3569; focused Edit Mode 376/376 over 24 fixtures. OWED TO 8.57g: Presentation reads `ActionDefinition.AttackRange` off the card at four sites (`AttackSpotlightPresenter` ~469, `DecisionWindowPresenter` ~623, `HeroOverviewPresentation` ~1454, `RoundActionPresenter` ~2039 via `offer.AttackRange`, which deliberately keeps the printed classification because offer equality guards every authorization path) - the projection route is `UltimateUnlockQuery.Derive(projection.Definitions, projection.Cards)` -> `RangedAttackOverlayQuery`, proven reachable from published state.

Historical evidence: [8.57e completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-857e-rowenna-fabled-lance-completion-evidence).

### [ ] 8.57f Compose Rowenna's eighteen playable faces

Outcome: nineteen exact printed definitions, eighteen fully executable faces, starting hand and upgrade/item paths.

Acceptance: literal source-linked rows for all values/text/items, absent icons remain absent; composition validation including deferred contracts; every distinct legal behavior and negative; all rider orders; exact source-card state and target exclusions; source corpus sweep; no engine deferral on dealt faces and only the established odd-player Handicap deferral. Focused and directly affected T2 checks.

Non-goals: dealing, artwork registration or player-gate completion claims.

### [ ] 8.57g Deal, register and artwork Rowenna

Outcome: Rowenna is the eighteenth legal pick, with profile, real card fronts, board visuals, hand/deck/upgrades and reachable catalog registrations.

Acceptance: all configured/staged/engaged/progressed adapters, selection/definition/artwork bijections, correct source visual identities, private-resource boundaries, content identity and snapshot compatibility decision preserving existing hero identities; scene/picker/artwork/footprint checks. Apply the highest tier triggered by catalog/configuration changes and the affected local build-and-launch smoke gate.

Non-goals: package creation/upload or final player-gate claims.

### [ ] 8.57h Complete Rowenna's permanent player-path gate

Outcome: mechanically complete coverage of every playable face and distinct interaction through configured real-input journeys.

Acceptance: pick/spawn/identity, actual pointer/board input, commit/reveal, all primaries/secondaries, both-party retrieves, other-hero coins and retrieves, the swap's two qualifiers, the gauntlet's opponent move across seats, both surrenders, both placement bullets, the opening shots, progression/earned items/Ultimate at Range 2, round persistence, defeat/respawn, stale input, no EngineFault, projection agreement, snapshot/restore/retained-prefix Undo on Rowenna-specific pauses, whole-face footprint. Complete Play Mode with zero skips.

Non-goals: new scope beyond Rowenna's faces.
