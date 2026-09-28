# Phase 8 - Twenty-sixth hero: Wuk the Grove Keeper

Return to [roadmap](../ROADMAP.md). [The session brief](../SESSION_BRIEF.md) owns the active and next route.

## Authorization and scope

On 2026-09-28, while Mrak's 8.112g was still in flight, the user named the next hero: "so when the other agent finishes Mrak, you get to start working on Wuk ... ask me any question you have so you can keep going and work while i sleep." The user answered WU-Q1 to WU-Q4 the same night, before 8.114 began. This authorizes the bounded sequence **8.114 -> 8.114a -> 8.114b -> 8.114c -> 8.114d -> 8.114e -> 8.114b1 -> 8.114c1 -> 8.114f -> 8.114g** (8.114b1 and 8.114c1 split from their blocks and reordered by the amendments recorded there) through a fully selectable, artworked Wuk, every dealt printed face and the permanent player-path gate, starting after 8.112h (`6e29c555`). Execute one milestone at a time with its own validation and local commit. A reading outside the settled facts stops its block for the user; other blocks do not wait on it. No other hero, package, remote publication or rule invention is authorized.

[Wuk's rules shard](../Rules/HERO_WUK.md) owns source facts, inventory, capability mapping and questions WU-Q1 to WU-Q4. Eighteen ordinary fronts are dealt; the nineteenth, Makeshift Weapon, is a p.20 Handicap and stays boxed at setup.

## Carried lessons

[Mrak's list](PHASE_8_TWENTY_FIFTH_HERO.md#carried-lessons) binds here unchanged: icons by shape, the artwork contact sheet, p.15 and closed vocabularies checked before dealing, offer and answer agree, no auto-confirm skipping a DONE, rendered pieces face the camera, the deciding seat draws its controls, journeys assert the board changed, tiers stated from their result files, mappings checked against print. Mrak's own series added these, each an acceptance item of the block named:

11. **Only the full harness sees the corpus sweeps** (8.112c): a new verified content class publishes `AllDefinitions`, joins the whole-corpus walks, and every new rule modification has a consumption case. Every block with a new content class runs the full harness; no block reuses an earlier full total after adding one.
12. **A static read across partial files is initialized in Unity's order, not the harness's** (8.112f). Cross-file definitions are lazy; the Unity tier, not the harness, proves them.
13. **A new public property on a content definition moves every hero's identity digest** (8.112f, 8.38d). The isolation probe proves the cause before any pin moves; revisions follow reachable semantics only.
14. **Placement-carrying faces own their reclaim node** (8.112g removed an unprinted standalone reclaim). No Tree capability is registered that no printed face names.
15. **A rail layout the shine tooling has not seen is traced from a template and composited on every front** (8.112g). 8.114f composites every overlay before claiming coverage.
16. **Journey lookups match whole names** (8.112h: "ROLLING STONE" inside "STROLLING STONE"; Wuk's "Mighty Throw" / "Monstrous Throw" and the two Dominance faces are the same trap).

### [x] 8.114 Inventory Wuk and map the implementation sequence

Outcome: verify the nineteen fronts, profile, dashboard, Tree and standee manifest pieces and relevant rulebook pages; record printed values, exact clauses, existing-capability matches, new contracts and the user's rulings. No executable content is registered.

Acceptance: all nineteen distinct fronts and both back variants accounted for; items, Range/Radius and ribbon glyphs verified by montage against recorded references; star/role profile and stat bars recorded; every novel effect probed against actual engine seams; WU-Q1-WU-Q4 recorded as user-settled. T0 documentation validation.

Non-goals: gameplay code, art import or a package.

Completed 2026-09-28. The nineteen fronts, both back variants, profile (three stars; Tokens, Pusher, Durable, Sniper, Tactician, Healer), stat bars, dashboard, the standee (element 8597495, filed under the name `Hanu`) and the three `Sapling` Tree faces are inventoried in [his rules shard](../Rules/HERO_WUK.md); items, Range/Radius and ribbon glyphs were verified by montage against Wasp's recorded icons. The probe found retained tokens, reclaim, up-to-N radius placement, removal, adjacency-to-kind selection, one-or-both and up-to-two-times modals, delegated retrieval, swaps, token-or-unit placement, moved-through spaces and the Minion-Battle rule catalog already in the engine; the new contracts are the Tree bullet, straight-line-conditional trampling, a battle-time minion exclusion and an any-card after-resolve trigger. WU-Q1-WU-Q4 were settled by the user before the block began. T0; no executable content or Unity run.

Historical evidence: [8.114 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8114-wuk-series-evidence).

### [x] 8.114a Implement the Tree token, Mystic Saplings and the Gold Tree bullet

Outcome: a Tree token definition (obstacle, supply 3 per WU-Q1, p.18 reclaim, retained at every round end per WU-Q2, Clear), Mystic Saplings placing up to three in Radius 3 (each face owning its reclaim node), and Tree Slam's and Makeshift Weapon's "Remove a Tree token adjacent to you. Target a unit (hero) in range" bullet beside their adjacent bullet.

Acceptance: a configured round places 0-3 Trees, reclaims when short, keeps every Tree across the round end, removes one for the Gold bullet and resolves the Attack at Range 2; the Tree bullet is not offered with no adjacent Tree and the adjacent bullet still is; a paused placement restores and replays; offer and answer agree. Unregistered until 8.114f.

Completed 2026-09-28. `WukComponentContent` defines the three-Tree `wuk-tree` token (bare obstacle, `RetainedAtEndOfRound` per WU-Q2) and binds the milestone 8.37 hero-token Place and short-supply handlers to it (`wuk-place-tree-in-radius@1`, `wuk-reclaim-tree@1`). `WukCardContent` composes Mystic Saplings as three optional [reclaim, place] pairs in its printed Radius 3 (Ignatia's 8.113 shape) and Tree Slam as p.10's modal bound by card text before the attack (Brogan's Throwing Axe shape): an adjacent enemy minion, or a Tree beside Wuk removed by `synthetic-remove@2` and then an enemy unit in the printed Range 2. `synthetic-choose-target@4` gains an optional `tokenKind` literal that admits tokens of that kind alone, judged as the printed-icon family judges it; unbound, every earlier selection keeps its candidates. Makeshift Weapon, the boxed Handicap, keeps every Handicap's deferred text (p.20) and joins the set at 8.114e. Twelve configured-round tests cover supply and retention, placement reach and emptiness, declining, both short-supply branches, restore/replay of a placement pause and of the Tree bullet's Defense, the greyed Tree bullet, the minion-only adjacent bullet, Tree-only removal beside a minion and another hero's token (failing with the new filter disabled), and the face withheld when the Tree bullet could name nobody in Range. Unregistered until 8.114f. T2; evidence in the milestone commit.

Historical evidence: [8.114a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8114-wuk-series-evidence).

### [x] 8.114b Implement the Red Tree Attacks

Outcome: Nature's Protector, Guardian and Champion ("a unit in range adjacent to a Tree token" as a bullet narrowing; Champion's one-or-both on different targets).

Acceptance: configured rounds prove both bullets' boundaries, the printed Ranges, Champion's two sequences and different-target rule, the greyed bullet that names nobody, and restore of a paused target question. Unregistered until 8.114f.

Amended 2026-09-28 before implementation finished, recording what the probe found: the trampling moves need a Movement action whose walk may ignore obstacles only on a straight line and consequences over the units it crossed - an engine seam no earlier face has - so they are their own step, 8.114b1, inside the approved sequence rather than a rider on the Attacks.

Completed 2026-09-28. `WukRedFaces` composes the three Nature's Attacks as p.10's modal over two `attack-sequence@1` bullets (Brynn's Familiar Ground and Bain's Hunter-Seeker shape): "a hero adjacent to you" and "a unit in range adjacent to a Tree token", Champion taking one or both on different targets. The second bullet is `AttackSequenceSubjectNarrowing.AdjacentToATokenTheActorOwns` (value 7), milestone 8.88's ownership reading of "a Tree token", evaluated per candidate at the bullet's own sequence start. Five configured-round tests cover both bullets, a rock of another hero's excluded, Protector's Range 2 against Guardian's 3, Champion's two sequences with the first target excluded from the second, and restore of the target question; two of them failed with the narrowing admitting everyone. Unregistered until 8.114f. T2; evidence in the milestone commit.

Historical evidence: [8.114b completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8114-wuk-series-evidence).

### [x] 8.114c Implement the Blue retrieval faces

Outcome: Gifts of Nature, Tree of Plenty and Abundance: remove a Tree in radius, then retrieval for Wuk and/or a friendly hero in radius who decides.

Acceptance: the removal is mandatory (no Tree: not offered, p.10); each retrieval form, the friendly hero's own choice, and Abundance's both. Unregistered until 8.114f.

Amended 2026-09-28 before implementation finished, recording what the probe found: Dominance's exclusions are chosen at the pre-battle moment but read when the battle is planned, on a later command a snapshot can separate, and nothing persisted can hold which minions were chosen - a snapshot schema move, so it is its own step, 8.114c1, inside the approved sequence.

Completed 2026-09-28. `WukBlueFaces` composes the three retrieval faces: the printed-Radius object selection narrowed to Wuk's Trees (8.88's `tokenKind` on the object arm) and `synthetic-remove@2`, then Rock Solid's optional self-retrieval and Silverarrow's Blessing composition for "a friendly hero in radius may retrieve" (the hero chosen on Wuk's side, never Wuk, retrieving from their own pile on their own choice); Tree of Plenty takes one bullet and Abundance one or both. The block found one defect in its own composition and one guard against the class: the Tree selection first left the object arm's required `includeSelf` unbound, which the offer walk reads as undecidable and therefore performable - Gifts of Nature was offered with no Tree in reach and ended having done nothing (carried lesson 4). `WukCompositionTests` now asserts that every node of every composed Wuk face binds every required parameter of its capability; it and the offer test failed with the binding removed. Unregistered until 8.114f. T2; evidence in the milestone commit.

Historical evidence: [8.114c completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8114-wuk-series-evidence).

### [x] 8.114d Implement the throws and the Tree swaps

Outcome: Toss Away, Mighty Throw and Monstrous Throw (place a token of any owner, or an enemy unit, adjacent to Wuk into an empty space in Range; Monstrous repeats once, the same subject allowed) and Into the Canopy and Treetop Ride (swap Wuk, or a friendly unit in radius, with a Tree in radius; Treetop up to two times, either bullet each time).

Acceptance: every subject kind (own Tree, another hero's token, enemy hero, enemy minion, immune enemy refused), Range and emptiness boundaries, a minion thrown out of its Battle Zone returned, the repeat, both swap forms and the two-times modal, each with restore. Unregistered until 8.114f.

Completed 2026-09-28. `WukGreenFaces` composes the throws as Mrak's Boulder subject (`synthetic-choose-target@4` at distance one, "an enemy unit or a token") consumed by Arien's `synthetic-place-in-range@1` from Wuk, Monstrous Throw adding p.10's repeat with the same subject allowed; and the swaps as p.10's modal over "swap with a Tree token in radius" (Trinkets' swap-with-actor over the Tree-narrowed Radius object selection) and "swap a friendly unit in radius with a Tree token in radius", Treetop Ride's two performances in Rowenna's Grand Melee shape. The block found one offer defect in its own first composition: with the friendly unit and the Tree chosen by two selections, the offer walk judged the bullet by the first selection alone and offered it with a friendly unit and no Tree - either order leaves the same hole, because a selection is narrowed only by a consumer it can decide. `WukTreeSwapContent`'s `wuk-choose-friendly-unit-and-tree@1` chooses both pieces in one question (both halves asked of the printed-icon family's one derivation), impossible when either half is empty, feeding `synthetic-swap@2`; the bullet now greys correctly. Twelve configured-round tests cover every subject kind including a friendly unit refused, both printed Ranges, a thrown minion's destination, the repeat on the same minion, restore of a destination pause, both swap forms, the friendly swap greyed with no friendly unit or no Tree, the face withheld with no Tree, the same bullet twice and stopping after one. The immune-enemy and out-of-Battle-Zone rows are p.19's shared rules the Place and selection already apply, not re-proved here. Unregistered until 8.114f. T2; evidence in the milestone commit.

Historical evidence: [8.114d completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8114-wuk-series-evidence).

### [ ] 8.114e Implement March of Nature and reconcile the set

Outcome: an after-you-resolve-a-card trigger for any card Wuk resolves, optionally placing one Tree in Radius 3 with the reclaim; the set reconciled (starting hand, deck, dealt definitions, p.15 upgrade list naming both Tier III successors per Tier II, every integer parameter's declared vocabulary).

Acceptance: the trigger fires once per resolved card of every colour and not for a card only discarded or used to defend, is declinable, reclaims when short, and restores from its pause; the reconciled set asserts every face's printed values against the inventory. Unregistered until 8.114f.

### [!] 8.114b1 Implement the trampling moves

Outcome: Trample and Angry Stampede: a Movement primary whose straight-line walk may ignore obstacles, then each enemy hero moved through discards or is defeated (immunity respected) and up to one (two) minions of any side moved through are defeated (WU-Q4).

Acceptance: configured rounds prove straight versus curved walks, passing through units and tokens but never ending on one, the discard-or-defeat answered on the addressed seat, immune heroes skipped, zero/one/two minion defeats including a friendly one, Movement items applied, and restore of each pause. Unregistered until 8.114f.

Paused 2026-09-28 on WU-Q5 ([his shard](../Rules/HERO_WUK.md#open-questions)): the two buildable readings differ in what counts as his Movement action, which is the user's call. 8.114c-8.114e do not depend on it and proceed; 8.114f cannot deal him until this step is complete.

### [ ] 8.114c1 Implement Claim and Assert Dominance

Outcome: a This round effect whose pre-battle trigger lets Wuk choose up to one (two) enemy minions adjacent to him at that moment, regardless of immunity (WU-Q3); the chosen minions do not count towards the minion total when the battle is planned. The choice is persisted until the battle is planned (snapshot schema move).

Acceptance: a minion battle whose result changes by the exclusion, a minion that moved away counting, the choice drawn for Wuk's seat, immune minions excludable, the exclusion gone after the round, snapshot restore between the choice and the battle, and the schema migration's corpus evidence. T3 (schema). Unregistered until 8.114f.

Reordered 2026-09-28 within the approved sequence: 8.114b1 and 8.114c1 both need state persisted across a snapshot that nothing persists today, so they run together after 8.114e, where one snapshot schema move can serve both once WU-Q5 is answered.

### [ ] 8.114f Deal Wuk and present his artwork

Outcome: Wuk joins the roster (three stars; Tokens, Pusher, Durable, Sniper, Tactician, Healer) with eighteen dealt faces and Makeshift Weapon boxed; staged configurations whose setup and engagement arms land together; card fronts, selection art, the standee (element 8597495) and the Tree token faces prepared privately; shine sprites for any new rail layout.

Acceptance: the contact sheet of every front beside its mapped title is inspected; the stat bars re-checked against the dashboard; the Tree token's rendered face normal asserted against the camera and a captured frame inspected; complete harness and Unity Edit Mode; every moved pin carries its measured cause.

### [ ] 8.114g Gate Wuk through the complete player path

Outcome: the permanent player-path gate maps every shape derived from Wuk's content to a real-input journey; the staged boards are measured before journeys are written; network-seat tests cover every decision addressed to a seat other than Wuk's.

Acceptance: every journey asserts a board, hand or battle change; complete harness, Edit Mode and Play Mode with zero unexplained skips; no package.
