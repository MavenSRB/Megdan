# Phase 8 - Nineteenth-hero dealing: Min the Dragonmonk

Return to [roadmap](../ROADMAP.md). Current authorization and next route live in [the brief](../SESSION_BRIEF.md).

## Authorization and scope

On 2026-09-19 the user opened the nineteenth hero ("Time for new hero") and, offered the fourteen undealt heroes, chose Min ("Min"). This authorizes **8.61 -> 8.61a -> 8.61b -> 8.61c -> 8.61d -> 8.61e -> 8.61f -> 8.61g -> 8.61h -> 8.61i -> 8.61j -> 8.61k**, through a fully selectable, artworked hero with working printed cards and the permanent player-path gate. Execute with separate validation and local commits; 8.61a, 8.61d, 8.61f, 8.61g and 8.61h touch disjoint seams and may run as a parallel wave in separate worktrees, 8.61b and 8.61c follow 8.61a, 8.61e follows 8.61d, and 8.61i-8.61k are sequential. Necessary corrections demonstrated by those checks belong to the block that found them.

[Min's rules shard](../Rules/HERO_MIN.md) owns source facts and the capability map. Eighteen faces are dealt; Chop Down is the boxed p.20 Handicap. Inspect existing contracts before adding vocabulary. New contracts stay outside production registration until reachable from dealt content; reconcile each new or widened shared-foundation claim with the test-owned claim catalog. MI-Q1 (token counts), MI-Q5 (the permanent item's effect on a later level-up) and MI-Q6 (grenade art) are carried as open user questions and do not block the series.

### [x] 8.61 Inventory Min and map the implementation sequence

Outcome: source-backed identity, nineteen-face inventory, exact text/values/items/upgrades, the three printed token kinds, capability map and outstanding dependencies are recorded.

Acceptance: inspect all fronts, profile and dashboard; reconcile front/back counts and hashes; search the entire local rulebook for hero-specific authority and inspect relevant general-rule pages; distinguish printed facts, verified general rules, implementation predictions and unresolved interpretations; find every token element in the Tabletopia manifest. Record the complete bounded sequence. T0 documentation validation only; no Unity.

Non-goals: executable content or mechanics; adopting unverified community rulings.

Completed 2026-09-19. All nineteen fronts, the selection card and the dashboard were read; the twelve item icons were montaged beside Rowenna's six verified icons, the ribbons and corners were montaged (every corner letter matches its ribbon), and the profile bars were read by scanline (Attack 4 + 4, Initiative 8, Defense 3 + 5, Movement 6). The nineteen backs share one hash; the selection front is byte-identical to `randomizers-tt8__7729915__front.png`. The rulebook names her nowhere; p.10, p.11, p.13, p.14, p.16, p.18 and p.19 govern. The manifest holds her standee (`Min`, 8597508), a `Smoke_Bomb` element and two `Mine_Dud` / one `Mine_Blast` elements, and no grenade. The capability map predicts nine new pieces (the Smoke bomb's targeting screen, the vanishing Block condition, a destination policy naming the attack's target, facedown mine definitions with per-seat masking and a traversable property, the mine trap over every displacement, a token-anchored end-of-turn grenade, the borrowed after-attack rider, an item-bonus multiplier with a deck-card permanent item, and an after-attack-action Ultimate trigger) over the existing token, place, swap, push, direct-move, discard-or-defeat, active-effect and repeat contracts. MI-Q1, MI-Q5 and MI-Q6 are held for the user.

Historical evidence: [8.61 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-861-min-inventory-completion-evidence).

### [x] 8.61a Support the Smoke bomb

Outcome: Min's Smoke bomb exists as content - placed from the supply or relocated into a space in radius or range, swapped with by Min, re-placed after a swap, removed at the end of the round - and while it stands no enemy hero can target a unit when the token lies on a straight line between them.

Acceptance: token definition with MI-Q1's supply and p.19's end-of-round removal; supply Place and identity-preserving relocation in radius (Smoke Bomb, Cobra Stance) and in range (Ruse); Min's swap with the token in radius and in range; the screen read at the one targeting seam for attack targets, card-text selections, pushes and swaps by enemy heroes only, never for presence checks, Min's own team, the token itself, spaces or the Minion Battle; adjacent pairs never screened; all six axes measured with the token strictly between; Clear removing it; a screened offer through snapshot, replay and Undo; the Presentation reading the same legal set as the authority; narrow foundation claims. Compose Smoke Bomb. T2 minimum.

Non-goals: the vanishing Block, the Stances, mines, the grenade, dealing.

Completed 2026-09-19 (`ad185a39`). No new handler type: the token composes on `TokenDefinition` / `TokenPropertyDefinition` with `TokenPropertyAffectedObject.TheTokenItself` (Tali's shape), `SyntheticOwnedTokenCapabilities.PresenceHandler` / `SpaceHandler` and `SyntheticTokenCapabilities.PlaceHandler` (Widget's Pyro), `SyntheticPlacementCapabilities.PlaceInRadiusV1` / `PlaceInRangeV1` and `SyntheticExchangeCapabilities.SwapWithActorV1` for the sub-graphs, and `RuleModificationQuery.FromPlacedTokens` derives the screen unchanged. Deltas: `RuleModificationKind.EnemyHeroesCannotTargetThroughThisTokensSpace` (14) with `RuleModification.ForEnemyHeroTargetingScreen()` (no amount, channel, activity or new property); `StraightLineQuery.LiesStrictlyBetween` (p.19's file as strict betweenness, so adjacent pairs and endpoints fall out of the direction arm); `RuleModificationSet.TargetingScreens` beside `ObstacleOverlays`, consulted by `TargetQuery.IsPrevented` before immunity and only once a screen stands, so every existing call answers as before. Content, all deferred: token `min-smoke-bomb-token` (supply 1 under MI-Q1, `EndOfRoundCleanup`, one screen property), capabilities `min-smoke-bomb-present` / `-space` / `-in-range` / `-in-radius`, `min-place-smoke-bomb-in-radius` / `-in-range` (`MinComponentContent.SmokeBombDeferredHandlers()`, the registered list empty and asserted from both sides); `MinEffectContent.SmokeBomb()` (a presence-conditional relocate-or-supply Place) and the reusable sub-graphs `SwapWithSmokeBombInRadius` / `InRange` and `PlaceSmokeBombInRadius` / `InRange` for 8.61b and 8.61c; `MinCardContent`'s Smoke Bomb face under the placeholder `min-8-61a-prep`. Readings measured: Min's defeat leaves token and screen standing; a presence predicate, the token itself, an acting minion, the controller's own side and an actorless resolution are untouched; the screen holds on all six axes at distances 2 and 3 and never for adjacent pairs. `MinSmokeBombTests` 45 cases; claim `targeting.token-screened-straight-line` registered narrow; harness 3676/3676; focused Edit Mode 120/120; schema v43 and wire /17 unmoved; no pin moved.

Historical evidence: [8.61a completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-861a-min-smoke-bomb-completion-evidence).

### [ ] 8.61b Support the vanishing Block

Outcome: Poof!, Vanish and Ruse block an attack exactly when Min swaps with her Smoke bomb within the card's range at p.14 step 4; Ruse may then re-place the bomb in range; a Block card printing no Defense value is defeated when its Block fails.

Acceptance: the new `PrintedBlockCondition` member; the defense preview and the resolver agreeing by construction; the swap performed as Defense text before the Defense total; the attacker's after-attack text applying from the new positions; failure with no Defense value read as p.14's defeat; Ranged and non-Ranged attacks alike; the bomb out of range, absent or reclaimed; Undo and restore across the defense pause; the decision window's Block wording. Compose Poof!, Vanish and Ruse. T2 minimum.

Non-goals: the Stances, mines, the grenade, dealing.

### [ ] 8.61c Support the Stances

Outcome: the five Red faces' after-attack riders - push an adjacent enemy unit up to 3 spaces (Crane, Tiger, Dragon); an optional move of 1 (Tiger) or 1 or 2 (Dragon) spaces ending adjacent to the attack's target, taken before the push; an optional swap with the Smoke bomb in radius (Viper, Cobra) and Cobra's optional re-place in radius.

Acceptance: the push subject chosen among adjacent enemy units (the target included when it survived, never when it was removed), "up to 3" under the existing push contract, straight line away, obstacles and immunity; the move's destinations adjacent to the target at distance 1 or 1-2 with no referent once the target was removed (p.10's stop), a p.11 card-text move that never fast-travels; the swap's radius read from the card with items; each rider through snapshot, replay and Undo; the scoreboard's Attacks at heroes. Compose the five Reds. T2 minimum.

Non-goals: mines, the grenade, the borrowed rider, dealing.

### [ ] 8.61d Support facedown Mines

Outcome: Min places blast and dud mine tokens facedown in radius; they are obstacles units may move through; every other seat sees only a facedown mine until it is removed.

Acceptance: two token definitions sharing one supply group with MI-Q1's counts; p.18's short-supply reclaim across the group; Trip Mine 1 + 1, Cluster Mine 1 + 2, Mine Field 2 + 1 as mandatory placements whose spaces Min orders; fewer empty spaces than tokens placing as many as possible; traversable for movement routes, direct moves and pushes but never an endpoint; end-of-round removal, Clear and the spawn-point rule; per-seat masking of the definition to a facedown mine for every seat but the controller's (bots included, the Director unmasked), the wire rows keeping their shape, no hidden kind reaching another seat's projection or log; snapshot, replay and restore keeping the real kinds. Compose the three Blues' placements. T2 minimum; T3 if the projection codec moves.

Non-goals: the trap, the grenade, dealing.

### [ ] 8.61e Support the mine trap

Outcome: after an enemy hero moves through a mine - by a Movement action, a card-text move or a push - the mine is removed and revealed, and for every blast removed that hero discards a card of their choice, if able.

Acceptance: detection over every displacement that moves an enemy hero (step commits, one-click routes, direct moves, pushes, coupled and batch moves), never Swap, Place, Fast Travel or respawn; several mines in one move; the trap applied after the whole move completes, inside an Attack when a push carried the hero; the discard chosen by that hero on that seat with the acting seat refused; "if able" on an empty hand; the public reveal (a log row - wire `/18` only if a kind is minted, stated either way); MI-Q3's route reading measured and recorded; Min's own moves, friendly heroes and minions never triggering; snapshot, replay, Undo and restore through the discard pause; the Presentation showing the removal and the reveal. T2 minimum; T3 if the wire moves.

Non-goals: the grenade, dealing.

### [ ] 8.61f Support the Grenade

Outcome: Death Grenade and Holy Death Grenade place a Grenade token in radius; at the end of that turn up to one (two) enemy heroes adjacent to it, chosen by Min, discard a card or are defeated, and the token is removed.

Acceptance: token definition with MI-Q1's count, placed from the supply or relocated; an End of turn Active effect bound to the placed instance; "up to" as Min's optional choice of which and how many; each hero's own discard choice and the defeat on inability through p.13's Defeat with its rewards and the scoreboard; a token gone before the end of the turn doing nothing; the grenade standing as an obstacle for the turn; ordering beside other end-of-turn effects; snapshot, replay and restore through the end-of-turn pauses. Compose both Blues. T2 minimum.

Non-goals: dealing.

### [ ] 8.61g Support the borrowed after-attack text

Outcome: Fast as Lightning's attack applies the "After the attack" text of Min's resolved or discarded Red card as its own rider, with that card's radius.

Acceptance: the pool is the one owned Red card when resolved or discarded and nothing when it is in hand or unresolved (p.10's impossible step, the attack unaffected); every Red rider executed in this attack's frame with its target (Crane's push, Tiger's and Dragon's move then push, Viper's and Cobra's swap and re-place) and the red card's Radius with items; Ranged Range 1 targeting; the rider's own pauses through snapshot, replay and Undo; Flurry's repeat re-borrowing; Chop Down composed as a definition and parked. T2 minimum.

Non-goals: dealing.

### [ ] 8.61h Support doubled items, the permanent item and Flurry of Blows

Outcome: Inner Strength and Perfect Self double Min's item bonuses for the round; Perfect Self may also take a Tier II card from her deck into a permanent item; Flurry of Blows lets her repeat each attack action once on a different target.

Acceptance: an item-bonus multiplier read at every item-bonus site (attack, defense, movement, range, radius, initiative ordering, declared radius and the 8.55 badge), cleared at the round's end; Perfect Self's one-or-both modal in any order; the deck-card choice among the Tier II cards in the deck, hidden from other seats' projections and visible to hers, set aside and placed as an item with its own icon, persisted through snapshot, restore and replay; a later level-up with one candidate under MI-Q5; the Ultimate's trigger after any attack action, once, never on a repeated instance, with the different-target exclusion, the riders re-applying and the trigger pause in the window; snapshot, replay and Undo. Compose Inner Strength, Perfect Self and Flurry of Blows. T2 minimum.

Non-goals: dealing.

### [ ] 8.61i Compose Min's eighteen playable faces

Outcome: nineteen exact printed definitions, eighteen fully executable faces, starting hand and upgrade/item paths.

Acceptance: literal source-linked rows for all values/text/items, absent icons remain absent; composition validation including deferred contracts; every distinct legal behavior and negative; all rider orders; exact source-card state and target exclusions; source corpus sweep; no engine deferral on dealt faces and only the established odd-player Handicap deferral; the set under one real content revision with `DealtDefinitions` present. Focused and directly affected T2 checks.

Non-goals: dealing, artwork registration or player-gate completion claims.

### [ ] 8.61j Deal, register and artwork Min

Outcome: Min is the nineteenth legal pick, with profile, real card fronts, board visuals for her standee and her three token kinds, hand/deck/upgrades and reachable catalog registrations.

Acceptance: all configured/staged/engaged/progressed adapters with both scene-composition arms, selection/definition/artwork bijections, correct source visual identities (standee `Min`, `Smoke_Bomb`, the mine faces with one facedown face for every seat but hers, the grenade under MI-Q6), private-resource boundaries, content identity and snapshot compatibility decision preserving existing hero identities; every deferred id registered and every new handler type reaching a showcase consumer; scene/picker/artwork/footprint checks; lobby bots holding her set. Apply the highest tier triggered by catalog/configuration changes and the affected local build-and-launch smoke gate.

Non-goals: package creation/upload or final player-gate claims.

### [ ] 8.61k Complete Min's permanent player-path gate

Outcome: mechanically complete coverage of every playable face and distinct interaction through configured real-input journeys.

Acceptance: pick/spawn/identity, actual pointer/board input, commit/reveal, all primaries/secondaries, the borrowed rider, the Smoke bomb's placement and the screen greying an enemy's targets, every Stance rider, facedown mines read on another seat, the trap on a movement and on a push with the other seat's discard, the grenade's end-of-turn pauses, the vanishing Block and its failure, doubled items in the badge, the permanent item, Flurry's repeat, progression/earned items, round persistence, defeat/respawn, stale input, no EngineFault, projection agreement, snapshot/restore/retained-prefix Undo on Min-specific pauses, whole-face footprint. Complete Play Mode with zero skips.

Non-goals: new scope beyond Min's faces.
