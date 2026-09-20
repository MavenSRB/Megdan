# Phase 8 - Twentieth-hero dealing: Brynn the Seeker

Return to [roadmap](../ROADMAP.md). Current authorization and next route live in [the brief](../SESSION_BRIEF.md).

## Authorization and scope

On 2026-09-21 the user opened the twentieth hero ("ok go for Byrnn, keep in mind he needs to be correctly implemented in new hero pick screen"), choosing Brynn from the thirteen undealt heroes and naming one requirement beyond the standing terms: his row in the 8.35z86 hero picker carries the verified profile facts (three stars; Tactician, Damager, Durable) so the star and role filters find him. This authorizes **8.69 -> 8.69a -> 8.69b -> 8.69c -> 8.69d -> 8.69e -> 8.69f -> 8.69g -> 8.69h**, through a fully selectable, artworked hero with working printed cards and the permanent player-path gate. Execute with separate validation and local commits; 8.69a comes first because every later block reads the predicate it builds, 8.69b, 8.69c, 8.69d and 8.69e then touch disjoint faces and may run as a parallel wave in separate worktrees, and 8.69f-8.69h are sequential. Necessary corrections demonstrated by those checks belong to the block that found them.

[Brynn's rules shard](../Rules/HERO_BRYNN.md) owns source facts and the capability map. Eighteen faces are dealt; Home Turf is the boxed p.20 Handicap. Inspect existing contracts before adding vocabulary. New contracts stay outside production registration until reachable from dealt content; reconcile each new or widened shared-foundation claim with the test-owned claim catalog. BR-Q4 (Decoy's two performances) is carried as an open user question and does not block the series; BR-Q13 (the board's water ring as obstacles) was settled by the user on 2026-09-21 as the printed reading.

### [x] 8.69 Inventory Brynn and map the implementation sequence

Outcome: source-backed identity, nineteen-face inventory, exact text/values/items/upgrades, the one printed predicate, capability map and outstanding dependencies are recorded.

Acceptance: inspect all fronts, profile and dashboard; reconcile front/back counts and hashes; search the entire local rulebook for hero-specific authority and inspect relevant general-rule pages; distinguish printed facts, verified general rules, implementation predictions and unresolved interpretations; find the standee in the Tabletopia manifest. Record the complete bounded sequence. T0 documentation validation only; no Unity.

Non-goals: executable content or mechanics; adopting unverified community rulings.

Completed 2026-09-21. All nineteen fronts, the selection card and the dashboard were read; the twelve item icons were montaged beside Rowenna's six verified icons (Initiative x3, Defense x3, Attack x3, Radius, Range, Movement), the ribbons and corners were montaged (every corner letter matches its ribbon), and the profile bars were read by luminance scanline (Attack 4 + 4, Initiative 7, Defense 4, Movement 5 + 1). The nineteen backs share one hash; the selection front is byte-identical to `randomizers-tt23__7723707__front.png`. The rulebook names him nowhere; p.9, p.10, p.11, p.14, p.17, p.18 and p.19 govern, p.18's Obstacle and p.19's Terrain sentences most of all. The manifest holds his standee (`Brynn`, 8629140) and no token; its card group says "the Tracker" where the dashboard prints "the Seeker". The Handicap's band prints "Basic Skill - Ranged" over an Attack (BR-05). The capability map predicts eight new pieces (the obstacle-adjacency query, the Ultimate's counts-as overlay with an any-action applicability, an attack-target narrowing member, a value-query member, a radius-hero narrowing parameter, a presence predicate, a repeat binding for "a different unit adjacent to you", and an attack-card filter on retrieval) over the existing modal, direct-move, swap, discard-or-defeat, repeat and retrieve contracts. BR-Q4 and BR-Q13 are held for the user.

Historical evidence: [8.69 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-869-brynn-inventory-completion-evidence).

### [ ] 8.69a Support obstacle adjacency and Over the Top

Outcome: "adjacent to 3 or more obstacles" exists as one query every consumer asks; Over the Top makes every enemy hero satisfy it while Brynn acts; Familiar Ground's second bullet, and the +2 of High Ground and Elevated Ambush, read it.

Acceptance: the query over the six neighbours with units (Brynn included), terrain, the unplayable ring, tokens and overlays counted, a threshold never a count, immune units counted (presence); the counts-as overlay applied only while the controller performs an action and never for minions or while another hero acts; the attack-target narrowing member and the value-query member; the optional Boolean narrowing on the radius hero target and the deferred presence predicate for the later blocks, each with a test; the +2 computed at the Attack total; the Ultimate through snapshot, replay and Undo; the Presentation's legal set equal to the authority's. Compose Familiar Ground, High Ground, Elevated Ambush and Over the Top. T2 minimum.

Non-goals: the split attacks, Decoy, the guides, the swaps, the traps, dealing.

### [ ] 8.69b Support the split attacks and Peak Precision

Outcome: Split Attack may repeat once on a different unit adjacent to Brynn, Split Throw repeats once on a different target unit, both only when the first target was a hero adjacent to 3 or more obstacles; Peak Precision adds +2 and may retrieve itself after the attack under the same condition.

Acceptance: the gate read on the first instance's target; the new repeat binding limited to units adjacent to Brynn; Split Throw's mandatory repeat and its impossible step; no repeat of a repeated instance; the retrieval from the played state at the after-the-attack step with the action still completing, refused when the condition failed; the Ultimate satisfying the gate; Undo and restore across the repeat and retrieval pauses. Compose Split Attack, Split Throw and Peak Precision. T2 minimum.

Non-goals: Decoy, the guides, the swaps, the traps, dealing.

### [ ] 8.69c Support Decoy and the guides

Outcome: Decoy moves up to two different enemy units one space each, minions freely and heroes only when adjacent to 3 or more obstacles; Mountain Guide and Expedition Leader move a friendly unit adjacent to Brynn up to N spaces and, when an enemy hero in radius is adjacent to 3 or more obstacles, a different friendly unit in radius up to N spaces.

Acceptance: two performances each choosing either bullet, the second excluding the first's unit, zero legal (BR-Q4); Brynn choosing unit and destination under p.11 (no obstacle transit, no ending on an obstacle, a unit with no empty neighbour not offered); the guides' optional first move and mandatory second with "up to" allowing zero; "different" excluding the first unit whether or not it moved; the presence checked not targeted; the Ultimate satisfying the presence; Undo and restore across the pauses. Compose Decoy, Mountain Guide and Expedition Leader. T2 minimum.

Non-goals: the split attacks, the swaps, the traps, dealing.

### [ ] 8.69d Support the tracking swaps

Outcome: Tread Lightly, Cover Tracks and Hide Traces swap Brynn with a unit adjacent to him or with an enemy hero in radius adjacent to 3 or more obstacles; Cover Tracks may then move him 1 space and Hide Traces moves him up to 2.

Acceptance: one choice over the union of the two reaches; friendly and enemy, hero and minion in the adjacent arm; immunity honoured for the swap subject (p.19: a swap targets); the swap neither movement nor placement; the trailing moves as Brynn's own card-text moves, optional for Cover Tracks and "up to" for Hide Traces; the Ultimate widening the radius arm to every enemy hero in radius; Undo and restore across the pauses. Compose Tread Lightly, Cover Tracks and Hide Traces. T2 minimum.

Non-goals: the split attacks, Decoy, the guides, the traps, dealing.

### [ ] 8.69e Support the traps and the grit

Outcome: Bear Trap, Log Trap and Deadfall Trap make one enemy hero - adjacent, or in radius and adjacent to 3 or more obstacles - discard a card, Deadfall defeating a hero who cannot; True Grit and Die Hard may retrieve a discarded attack card and, when an enemy hero in radius is adjacent to 3 or more obstacles, move Brynn up to 3 or 4 spaces.

Acceptance: the modal's one bullet; the chosen hero picking the card; "if able" as nothing on an empty hand; the Deadfall escalation defeating with p.13's coins; the attack-card filter admitting Familiar Ground and the Reds and refusing Blues and Greens; the retrieval optional and the conditional move mandatory with zero allowed; the Ultimate satisfying the presence; Undo and restore across the pauses. Compose Bear Trap, Log Trap, Deadfall Trap, True Grit and Die Hard. T2 minimum.

Non-goals: the split attacks, Decoy, the guides, the swaps, dealing.

### [ ] 8.69f Compose Brynn's printed set

Outcome: all nineteen definitions with progression, items, starting hand, deck, the Ultimate on no ladder and the boxed Handicap compose under one content revision, with the set facts and the deferred ids asserted.

Acceptance: every face's values, icons and text as the shard prints them; `DealtDefinitions` stated; the dealt-corpus composition validator green for all eighteen dealt faces; the deferral list equal to the series' declared ids; corner/ribbon agreement; no registration. T2 minimum.

Non-goals: dealing, artwork registration or player-gate completion claims.

### [ ] 8.69g Deal, register and artwork Brynn

Outcome: Brynn is the twentieth legal pick, with profile, real card fronts, board visuals for his standee, hand/deck/upgrades, reachable catalog registrations, and a roster row the 8.35z86 picker filters by his three stars and his three printed roles.

Acceptance: all configured/staged/engaged/progressed adapters with both scene-composition arms, selection/definition/artwork bijections, correct source visual identities (standee `Brynn` 8629140, pick card `randomizers-tt23__7723707__front.png`), private-resource boundaries, content identity and snapshot compatibility decision preserving existing hero identities; every deferred id registered and every new handler type reaching a showcase consumer; the roster row's `HeroComplexity.VeryComplex` and `Tactician | Damager | Durable` asserted by the metadata coverage and shown at the RENDERED tier - a three-star filter and each of his three roles keep his button visible and a role he lacks hides it, in an inspected capture; scene/picker/artwork/footprint checks; lobby bots holding his set. Apply the highest tier triggered by catalog/configuration changes and the affected local build-and-launch smoke gate.

Non-goals: package creation/upload or final player-gate claims.

### [ ] 8.69h Complete Brynn's permanent player-path gate

Outcome: mechanically complete coverage of every playable face and distinct interaction through configured real-input journeys.

Acceptance: pick/spawn/identity through the filtered picker, actual pointer/board input, commit/reveal, all primaries/secondaries, both Familiar Ground bullets, the +2 seen in the Attack total, each split repeat and its refusal, Peak Precision's retrieval, Decoy's two performances, both guide sentences, each swap arm and trailing move, each trap bullet and Deadfall's defeat, the grit's retrieval and move, Over the Top widening every clause, progression/earned items, round persistence, defeat/respawn, stale input, no EngineFault, projection agreement, snapshot/restore/retained-prefix Undo on Brynn-specific pauses, whole-face footprint. Complete Play Mode with zero skips.

Non-goals: new scope beyond Brynn's faces.
