# Phase 9 - The twenty-eighth hero: Razzle

Status: [route](../SESSION_BRIEF.md). Rules: [HERO_RAZZLE.md](../Rules/HERO_RAZZLE.md) (created by 9.21).

Authorized 2026-09-30 by the user ("also, implement razzle, ask any questions you might have now"). Razzle was surveyed at Hanu's selection (four stars, Tactician and Melee, no tokens) and withdrawn once on 2026-09-18 because every one of his faces spawns, moves or removes extra bodies of himself: Stunt Doubles "Spawn up to 3 more of you in radius; each of you is the same hero, except when actions are performed. If defeated, remove all of you", Crowd Control "+2 Defense for each other you in radius" / "remove all other you in play", Phantom Strike "you may remove one of you", Alleyoop "Move one other you 1 space", Tightrope "you may move another one of you 1 space", Twin Strike "a different you may repeat it once, targeting a different unit". One hero, one hand, one level, many bodies is a new identity shape for the engine, the snapshot, the wire and the board presentation, and this shard owns its sequence. The user's answers to RZ-Q1 and later are recorded here and in his shard before the blocks that depend on them start. On 2026-09-30 the user settled RZ-Q1-RZ-Q12 one by one, each as recommended ("Give them one by one with suggestions"), and had already authorized the dealing ("implement razzle"): the bounded sequence **9.21a -> 9.21k** is authorized, executed one block at a time with separate validation and commits; 9.21i's captures still need the user's approval before it lands.

Eighteen ordinary fronts are dealt; the nineteenth, Stunted Doubles, is a p.20 Handicap and stays boxed at setup. The blocks below are mapped by 9.21. Each starts only after the RZ questions it names are answered, in the order listed.

## Carried lessons

[Mortimer's list](PHASE_9_TWENTY_SEVENTH_HERO.md#carried-lessons) binds here unchanged: Wuk's lessons 1-17, lesson 18 (every offer is checked against its outcome) and lesson 19 (rulings can live in tests). Four more bind:

20. **A hero is not a space.** The engine assumes one space per hero in named places: `AdjacentMoveAuthority`'s unique-hero guards, `TryGetHeroCoordinate`, `AttackTargetQuery`'s `Single`/`SingleOrDefault`, `DefenseValueQuery`'s exactly-one-space throw, `HeroDefeatResolution.RemovedHero`, the snapshot's hero rows ordered by `HeroId`, `NetworkHeroRow`, and `HeroViewFor`. 9.21a lists every such site by grep before changing any, and each is either body-aware or proven to read the acting body.
21. **Every other hero is a consumer of the new shape.** A many-bodied Razzle changes what 27 dealt heroes' selections see. 9.21c sweeps the roster's hero selections and presence checks, as `RosterMoveLeashSweepTests` swept moves, rather than trusting that no face is affected.
22. **Every copy carries the body** (9.11). A body field added to occupancy rows, attack targets or Defeat facts is proven through the reflection completeness checks on the projection clones, `SeatProjectionFactory` and the wire codecs, not by a hand-picked round trip.
23. **The acting body is part of every offer.** An offer judged from one body and answered from another is lesson 18's defect. Offer tests stage at least two bodies whose answers differ.

### [x] 9.21 Inventory Razzle and map the implementation sequence

Outcome: verify all nineteen fronts, the profile, dashboard and standee evidence in the Tabletopia corpus; record printed values and exact text, the rulebook intersections (units, heroes, Defeat, Defense, Radius and Range measured from a body, Swap, spawn, respawn, immunity, minion battle presence, throne and lane objectives, Fast Travel and Hold), the existing-capability matches and the new contracts (a hero with several board bodies; an acting body chosen per action; "you", "other you", "a different you" as selections; "if defeated, remove all of you"; the Ultimate's repeat by a different body), the presentation needs (several standees for one hero, the acting body's cue, the pick), and every unresolved interpretation as RZ-Qn with the block it blocks. No executable content is registered. Acceptance: nineteen distinct fronts and one shared back accounted for; selection roles and complexity, the dashboard, items and stat bars inspected; the capability map probes the actual engine seams (`HeroId`-keyed occupancy, `RuleObject.ForHero`, targeting `SourceTeamRelation`, Defeat and respawn, `SeatProjectionFactory`, `PlacedTokenView`/hero standee views) and names the schema and wire moves it expects; every open question has an owner and blocks only its dependent block. T0 documentation validation. Non-goals: gameplay code, assets in the build, a package.
Completed 2026-09-30. Nineteen byte-distinct fronts and one shared back verified; the profile (four stars, Tactician and Melee) and dashboard (Attack 3, Defense 1, Initiative 8, Movement 5; items Attack x3, Defense x3, Initiative x3, Movement, Range, Radius); one standee element in the corpus, so the body count is RZ-Q1; the one-space-per-hero seams named by file (`AdjacentMoveAuthority`, `DefenseValueQuery`, `AttackTargetQuery`, `HeroDefeatResolution`, the snapshot hero rows, `NetworkHeroRow`, `PrimitiveHeroView`); snapshot v48 -> v49 and wire /25 -> /26 expected at 9.21a. RZ-Q1-RZ-Q12 recorded in [his shard](../Rules/HERO_RAZZLE.md) with recommendations; blocks 9.21a-9.21k mapped below, each starting only once the questions it names are answered.
Historical evidence: [9.21 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-921-razzle-inventory-evidence).

### [x] 9.21a Build the many-bodies model in the engine

Outcome: a hero may stand on several spaces as one hero. The contract has these parts:
- A body identity on occupancy rows and board occupants, and a per-hero body supply (RZ-Q1).
- The unique-hero guards relaxed only for a hero that declares bodies.
- An Attack that targets a body, and Defense measured from the attacked body.
- One Defeat that removes every body, with one reward and one Life spend (RZ-Q3).
- Respawn as one body when none stands.

Every one-space-per-hero site named in lesson 20 is made body-aware or proven to read one body.

Acceptance:
- A staged match with two Razzle bodies: an enemy attacks either body; minion modifiers read that body; a failed defense defeats him once and clears both; he respawns as one.
- Every existing hero's behaviour is unchanged (harness, Edit Mode, corpus digests moving only for the schema).
- Restore and replay with two bodies.
- 9.11's completeness checks carry the new fields.
- Snapshot v48 -> v49 and wire `/25` -> `/26` recorded with cause.

Tier T2. Non-goals: spawning from a card (9.21d), the acting-body pick (9.21b), other heroes' selection counting (9.21c), presentation.

Completed 2026-09-30 (`36bdcfe0`). `HeroOccupancy.Body` is an ordinal from 1 and the two-argument constructor means body 1, so every existing hero keeps its identity; `HeroBodySupply(hero, bodies)` declares a many-bodied hero (four for Razzle, RZ-Q1) and `AdjacentMoveAuthority` relaxes its one-space guard only for a declared hero, refusing a body above the supply or twice. `AttackTarget.HeroBody` and `DirectorCommand.HeroBody` (0 = the hero only, accepted only when exactly one body is legal) name a body; `IsSameUnit` compares at hero level (RZ-Q2). The acting-body seam: `TryGetHeroCoordinate` answers the only body or the acting body (body 1 until 9.21b), throwing by name when several stand and body 1 does not; `IsHeroOnBoard`, `GetHeroBodies`, `TryGetHeroBodyCoordinate` and `TryGetSoleHeroCoordinate(hero, reading)` are the explicit reads. Seams changed per the shard's site table: any body counts as present (respawn, the defeated-actor check, Clear, six trigger/modification presence reads, the resolved-card discard, Brynn's split target, the counts-as-minion hero); the attacked body drives removal-before-Defense, the pre-consequence space, the Defeat's lane-push space, `DefenseValueQuery` (hero-only overloads refuse a many-bodied defender) and the Defense and target previews keyed by (hero, body); every body is offered by `AttackTargetQuery` (`FindLegalEnemyHeroBodies`, one target per body, the hero named once) and removed by `CommitValidatedHeroRemoval` (`HeroDefeatResolution.RemovedBodies`, RZ-Q3); presence, Battle Zone, roster vocabulary, auras, adjacency counts and hero lists count once per hero; three Active-effect radius anchors, the bounty controller and the Defense-moved defender refuse loudly. Respawn is refused while any body stands and returns body 1. Snapshot v48 -> v49 (hero rows carry the body, the board ends with the declared supplies, a persisted hero Attack target names its body; v48 rejected; sixteen corpus captures re-pinned, setup 16,045 -> 16,057 bytes, nine aggregate digests re-pinned with the equality groups intact, twelve bare pins and the six pinned documents moved); wire /25 -> /26 (`NetworkHeroRow.Body`, the Attack-target and Defense-preview rows' `HeroBody`, the command codec; six protocol pins moved; 9.11's completeness walks extended and seven formerly unreached wire properties now compared). Content digest unmoved: nothing registered. `HeroBodiesTests` (10) on the staged `HeroBodiesBoard` (the configured mirror with a four-body Blue hero) proves the guards, moves, removal, respawn, restore, the target query and Defense refusal, an Attack on either body with previews from that body, atomic refusal of a hero-only answer, one Defeat removing both bodies, restore/replay/Undo to identical bytes and the wire with both bodies. Open for later blocks: `BoardObjectReference` names a hero, not a body (9.21c); events and log rows name the hero; Presentation keeps one view per hero (9.21i); the acting body is not persisted (9.21b).

Historical evidence: [9.21a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-921a-many-bodies-model-evidence).

### [x] 9.21b Choose the acting body for each action

Outcome: when more than one body stands and he acts, a decision "which of you acts" precedes the action choice (RZ-Q4). Every actor-space read answers the acting body for the whole action: before/after text, secondary Movement, Fast Travel, Clear and Hold. A Defense is the attacked body's. The decision is projected, crosses the wire, restores, and has a bot answer.

Acceptance:
- Offers differ correctly by body (a Basic Attack offered from one body and not from the other).
- Movement and Fast Travel move the chosen body.
- Restore at the pending pick.
- Undo walls unchanged.
- The pick is absent with one body.
- Any schema or wire move is recorded.

Tier T2. Non-goals: presentation of the pick (9.21i).

Completed 2026-09-30 (`d51403a6` rebased from `ec7b59bb`, plus `d63c5f4c`). A twelfth pending family, `PendingDecisionFamilies.ActingBody` ("acting-body"): `PendingActingBodyChoice` (hero, card, the standing bodies in body order, one `ActingBodyCommand` per body) is derived like the respawn choice (`TurnResolutionAuthority.PendingActingBodyChoice` via `ActingBodyRules.CreateChoice`) when the selected actor's supply is above one, more than one body stands and no acting body is settled; while it stands `GetCommonRejection` refuses every action start and Hold (`ActingBodyChoicePending`) and the projection offers no action, Hold, Fast Travel or Clear. `DirectorCommand.ChooseActingBody` (kind 25, the body in `HeroBody`) answers it through `RoundResolutionAuthority` to `TryAnswerActingBody`, refusing a bad body atomically (`ActingBodyCommandRejected`) and emitting `ActingBodyChosen` with the body's space. `AdjacentMoveAuthority` keeps the settled body per many-bodied hero (`ActingBodyIndex`), which `TryGetHeroCoordinate` and every 9.21a acting-body read follow: before-action text, `MovementSession`, Fast Travel, Clear, the attacker and the synthetic capabilities' actor; settled bodies clear on each actor selection and on Defeat, a many-bodied actor with one body standing is settled at once (and again after a respawn), and a Defense raises no pick. Owner is the selected hero (`OwnerOfStandingDecision`); the family is delegated under Hanu's Ultimate with HA-Q3 (the reading recorded in the shard for the user's confirmation); a seat reads it as its `DecisionContract` (Director-only in `CopyCompletenessTests`). Wire /26 -> /27 (the new command kind only; six pins moved); snapshot v49 -> v50 (each supply row ends with the settled acting body, carried by `BoardRestoreState.ActingBodies`; v49 rejected; sixteen captures re-pinned at unchanged lengths, nine aggregate digests re-pinned, twelve bare pins and the six pinned documents moved). The bot scores bodies by distance to the nearest enemy plus the Attack-start weight when an enemy is adjacent (`BotPolicy.ActingBodyDelta`), ties to the seeded die. The first Unity run found two presentation pins the harness cannot see and d63c5f4c closed them: `RoundActionPresenter.FormatStatus` names the step ("YOUR ACTING BODY - CHOOSE WHICH OF YOU ACTS" to the owner and the Director, "<HERO> - CHOOSING WHICH OF THEM ACTS" to every other seat), the presenter inventory counts twelve, and `PendingActingBodyChoice` joined `IMatchReadModel` (carried to the owning seat only, rebuilt on the client from the decision contract and the hero rows - no wire row), with `AwaitedResponderNarrationTests.TheActingBodyPick_IsNamedToItsOwnerAndToEveryOtherSeat`. `HeroActingBodyTests` (9) on `HeroBodiesBoard` proves the pick stands before any action and only the owning seat answers, offers differ by body (the Basic Attack from body 1 beside Red, greyed and refused from body 2 on its spawn; Fast Travel only from body 2), Movement and Fast Travel move the chosen body, a Defense and a single standing body ask nothing, restore at and after the pick keeps the body, replay and Undo hold (the seat's rewind reaches the pick and no further), the pick crosses the wire, and the bot picks body 1 on eight seeds; mutation checks (ignoring the settled body; persisting 0) fail the offer, movement and restore cases. Open for later blocks: the standee pick and acting cue and the parked conformance arm (9.21i); a spawn during the action (9.21d); an acting body removed mid-action (9.21e); borrowed or delegated actions off his turn (9.21c); `BotEvaluation` reads a hero's first row; the six-seat rehearsal lists gain the family at 9.21j or 9.21k.

Historical evidence: [9.21b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-921b-acting-body-evidence).

### [x] 9.21c Make every other hero read a many-bodied hero

Outcome: the dealt roster's hero selections, counts and presence checks read Razzle per RZ-Q2. Hero-counting steps ("each enemy hero", "up to N heroes", "a different hero", repeats that cannot target the same unit) and hero-level results follow the answer. Presence checks, obstacles and Fast Travel's "no enemy heroes in the Zone" see every body.

Acceptance: a roster sweep enumerating every hero-selecting capability and its reading under the answer, with a staged two-body case per distinct shape (for example Arien's Dangerous Current's "up to N enemy heroes" under W3, a Different-target repeat, a Fast Travel block). `OfferOutcomeTests` stays green with Razzle staged. Tier T2. Non-goals: Razzle's own faces.

Completed 2026-09-30 (`81b232d7`, rebased as `a8fa4368`). `HeroBodiesRosterSweepTests` walks the 264 capabilities the dealt faces compose over 110 handler types and fails on any handler without a declared reading: 47 read no hero or unit; 8 read the acting body (actor-space, charge and line destinations, choose-direction, the Turret radius, Wuk's adjacent minions); 8 read the attacked body (Brynn's three-obstacle target, the defended card's forced move, target distance and adjacency, the attacked-target space, pre-consequence adjacency, the attack sequence); 25 offer each body as its space and count him once (the printed-icon family, the roster target/set/trio/through-line/traversed/swap-subject handlers, unit pairs, Hanu's friendly pair, `synthetic-choose-target@3/4`, range-obstacle, Wuk's friend-and-tree); 11 offer him once (behind-target-line, anchored grenade targets, Ignatia's magma and minion discards, hero-adjacent-to-minion, friendly-hero-in-play, Wuk's trampled heroes); 27 presence reads see every body and count once (presence-count, Fast Travel, obstacle placements, Brynn's radius presence, spawn points, target-survived); 110 hero-level results happen once (Active effects addressed to the hero, discards, markers, Defeat, coins and items); 20 operations on a space act on that body (push, direct/batch/coupled move, swap, place, remove); 8 are open where nobody chooses the body (Brynn's tracking swap subject, Hanu's journey partner, Ignatia's and Snorri's text-bound Attack targets, `set-attack-target@1`). Seams: `BoardObjectReference` names a body (`HeroBody`, `ForHeroBody`; equality at hero grain; `TryGetObjectAt` names the body on a many-bodied hero's space; `TryGetObjectCoordinate` reads a named body); `RosterPresenceQuery.OfferUnits`/`ResolveUnit` and `FindUnitsWithin(..., everyBody)` are the shared offer helpers, the hero-once handlers dedupe (they had listed him per space and the choice threw "Effect choice candidates must be unique"); `EnclosingAttackFacts.HeroTargetBody` feeds card text about "the target" and the defender's forced Movement, closing 9.21a's refusal in `CardActionResolutionAuthority` and Brynn's any-body read (`ObstacleAdjacencyQuery` takes a body); "different target" repeats over spaces exclude every space the excluded hero stands on. Restore-fidelity defect found by the many-bodied soak and fixed: a restored pending selection was re-asked without its node's declared outputs, so after a restore it offered the hero once instead of his bodies (`AuthoritativeSnapshotSerializer.CardAction` now passes `declaredOutputs: node.Outputs`). A repeat's bound piece and effect-choice preview occupants stay at hero grain (declared in `CopyCompletenessTests`); schema v50 and wire /27 unmoved. Staged two-body cases: Dangerous Current's "up to 1 enemy hero behind the target" names him once with two bodies in the file; the printed-icon hero arm offers him once or per space as its sentence reads; an ally's "friendly hero" offers him once and its radius pick moves body 2 only; a hostile push moves the picked body; "up to 2 units" refuses his two bodies as two; Min's Death Grenade names him once; Fast Travel is blocked by body 2 alone in the Zone; immunity shields both bodies while presence counts once; "the target" and the three-obstacle check read the attacked body; "different target" excludes his second body; Arien with four bodies reaching his own radius aura is refused atomically by name; the soak plays a line-up with Sabina on four spaces for 300 commands (an earlier probe of every line-up and position left only the self-anchored auras, now RZ-Q13). Open: text-bound Attack targets take body 1 (`set-attack-target@1`), Brynn's tracking subject, Hanu's journey partner, the unit-pair partner edge, "on the same target" repeats at hero grain (9.21h), borrowed actions by Razzle get no pick (9.21h), RZ-Q13 sites (9.21h). RZ-Q13 and RZ-Q14 were settled by the user during this block.

Historical evidence: [9.21c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-921c-roster-reading-evidence).

### [~] 9.21d Implement Stunt Doubles and the spawn

Outcome: Stunt Doubles' Basic Attack, then "Spawn up to 3 more of you in radius". The spawn takes 0-3 bodies from the supply (RZ-Q1; p.19 Spawn, no reclaim) into empty spaces within the printed Radius of the acting body, and Radius items widen it (R1). The boxed Stunted Doubles is composed the same way ("Target a hero").

Acceptance: 0-3 spawns, a short supply spawning fewer, the Radius item, restore and replay mid-spawn, offer and answer agree. Unregistered until 9.21j. Tier T2. Non-goals: Twin Strike's repeat of it (9.21h).

### [ ] 9.21e Implement the removal faces

Outcome:
- Phantom Strike ("If there is more than one of you in play, you may remove one of you"; a false "If" is RULING 53's do-nothing).
- Hit and Gone (one or more, never the last).
- Into Thin Air (any number, including all).
- Crowd Control as a Skill ("remove all other you in play", RZ-Q7).

Removal is never a Defeat. After Into Thin Air removes every body he is off the board, not defeated, with his Ultimate inactive, and respawns by p.7 (RZ-Q6).

Acceptance: each face's bounds, no coins or Life on removal, the respawn offer after Into Thin Air, Crowd Control greyed or offered per RZ-Q7, restore and replay. Tier T2. Non-goals: Crowd Control's Defense (9.21g).

### [ ] 9.21f Implement the moves and swaps of his bodies

Outcome, with the reach and direction readings of RZ-Q5 and RZ-Q8-RZ-Q10:
- Alleyoop, Group Performance and Team Spirit: swap with a friendly hero in range, then move one other body 1 / up to 2 / up to 3 spaces.
- Tightrope, High Wire and Wire Dancers: after the Movement, he may move another body 1 / up to 2 / up to 3 spaces.
- Magic Trick and Aaaand it's gone!: push a unit adjacent up to 2 / 3, then the acting body retreats the realised count in the opposite direction.
- Theatrics: swap with a minion in range.

Acceptance:
- Other bodies are never offered as the friendly hero or the pushed unit (per RZ-Q5).
- Moving 0 still triggers the Tightrope text; Fast Travel does not.
- The retreat matches a 0, 1 and 2 space push.
- `RosterMoveLeashSweepTests` lists the new moves.
- Restore and replay.

Tier T2. Non-goals: Spectacle's repeat (9.21h).

### [ ] 9.21g Implement Crowd Control's Defense and the counting faces

Outcome: Crowd Control discarded to defend gives +2 Defense for each other body in Radius 3 of the attacked body (RZ-Q3). Rummage retrieves if another body is in radius (a printed "If"). Ransack retrieves once per other body in radius, each optional.

Acceptance:
- The Defense total with 0-3 other bodies, measured from the attacked body.
- A body just outside the radius is not counted.
- The Radius item widens the count.
- Retrieve counts.
- Restore at the defend question.

Tier T2.

### [ ] 9.21h Implement the repeat by a different body

Outcome: Twin Strike (p.16 permanent): after Stunt Doubles' Basic Attack, another body may perform it again against a different unit (RZ-Q11; Cloak and Daggers' after-basic-action repeat with the acting body switched). Spectacle: "May repeat once by a different one of you, targeting a different minion" (RZ-Q12). RZ-Q13 (any body): the self-anchored radius sites 9.21a left as named refusals read any body of a many-bodied controller here, with the borrowed-action arm.

Acceptance:
- The repeat is measured from the second body.
- The first target is excluded.
- The repeated spawn is bounded by the supply.
- No repeat with one body.
- Twin Strike only after a basic attack action.
- Restore mid-repeat.

Tier T2.

### [ ] 9.21i Present several standees and the acting cue

Outcome:
- One standee view per body, each with element 8596252's team face and a quiet per-body mark.
- Hover, target cues and the Defeat fade per body.
- The pick of the acting body by clicking a standee.
- An acting cue that follows a "different you" repeat.
- Rail, overview and victory screen showing one hero.

Acceptance: Play Mode cases for two and four bodies, the pick, an attack on a body, the fade of every body on Defeat, and one rail entry. The acting-body family's surface arm in `PendingDecisionContractConformanceTests` (parked by 9.21b in `ParkedSurfaceFamilies`) is replaced by the standee pick's commands and the parking removed. Captures are approved by the user. Tier T3. Non-goals: new standee art.

### [ ] 9.21j Deal Razzle with his artwork

Outcome: `hero-razzle` joins the roster as "Razzle the Ringmaster" (four stars; Tactician, Melee) with eighteen dealt faces and Stunted Doubles boxed. Also:
- His handlers join the match catalogs.
- Private art is prepared: eighteen fronts, the selection front `randomizers-tt30__7723698`, and the standee faces.
- Staged boards: the starting hand, and Tier III with Twin Strike, each with both scene composition arms.
- Every pin moved with its cause.
- The corpus, conformance, printed-wording and offer checks cover him.

Tier T2. Non-goals: a package.

### [ ] 9.21k Gate Razzle on the player path

Outcome: the permanent player-path gate passes for him. Every face's journey reads what its answer changed: bodies spawned, removed, moved or swapped, the retrieve, the Defense total, and the repeat by a different body. The board is measured first by a headless probe.

Acceptance: the gate's checks green offline and in Play Mode. Tier T3.
