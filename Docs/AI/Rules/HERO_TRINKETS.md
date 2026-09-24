# Guards Digital - Trinkets Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The implementation sequence](../Roadmap/PHASE_8_TWENTY_THIRD_HERO.md) owns acceptance and status.

## Source and settled authority

Trinkets the Scavenger is the approved twenty-third hero. The curated Tabletopia setup 799373 contains nineteen fronts and nineteen backs under `active-hero-cards/Trinkets/`; all nineteen fronts, the profile and dashboard were visually read on 2026-09-24. The enlarged profile shows **three stars** and **Sniper, Damager, Pusher, Tokens**. The dashboard gives the full title. The nineteen backs share one MD5 (`2a37aa67777c6d3ac6622ba54212de82`). The profile front SHA-256 is `02590861929e0f2a19d3a0c14849f263a7aaf4fc652d049edd08e0f34959971d`. These are source references, not distribution permission.

The user's 2026-09-24 ruling: the supply has **three Barrier tokens**. The Turret is the **single provided** piece and **an obstacle only**: it is not terrain, a token, a unit, a minion or a hero. The user's later explicit "No, obstacle only" supersedes the earlier "counts as terrain" description; the distinction is preserved here because it changes terrain predicates. It does only what Trinkets' cards instruct. Its obstacle property persists when Trinkets' cards change zones, between rounds and after Trinkets' defeat. Placing it again repositions the same piece. A target required to be in Range of Trinkets and the Turret must satisfy both ranges independently. The Turret does not enter p.19's ordinary-token cleanup at the end of a round or on the placing hero's defeat.

The user's further 2026-09-24 clarification: Trinkets is the actor and targeting source of every card. The Turret adds only the printed geometric restriction; it never replaces Trinkets as the actor. Thus Arien's Spell Break can prevent nearby Trinkets from using Disruptor Jolt or Self-Destruct even with a distant Turret, and Min's Smoke Bomb between Trinkets and the target can interrupt Trinkets' Attack regardless of the Turret's position. Tests for the affected cards must distinguish those positions.

The user also supplied a screenshot of a direct designer answer dated 2026-09-21 to the pre-Defense timing question. When a friendly hero attacks an enemy hero in the active Disruptor Jolt or Pulse Radius and the defender's sole hand card has a Defense primary, Trinkets' discard effect happens first, before p.14 step 3, so that card cannot be used to defend. This direct designer clarification is source authority under the ledger hierarchy. The implementation must test the exact case and retain the Defense window only for cards still eligible afterward.

The local second-printing rulebook pp.10, 13-14 and 17-19 governs modal choices, repeat, Attack order, timed active effects, obstacles, placement, Swap, target, tokens and independent Range/Radius icons. The extracted cards are the source for the individual printed clauses under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy). `Ramshackle Turret` is a p.20 Handicap and is boxed at setup, not dealt.

The Tabletopia manifest names a `Kobold` standee (element 8597497), a `Koboldturret` standee (8597498), and `Wall_Barrier` token art (1421882). Their imagery needs crop confirmation before 8.92f imports it; the names alone do not set the gameplay kind or supply. All nineteen card backs share the one MD5 above. `Trinkets__front.png` is the profile; `Trinkets__front.png` under `_dashboards` is the dashboard. The profile and dashboard bars agree at Attack **5**, Initiative **7**, Defense **4**, Movement **8+1** (the brighter last pip is the Movement item), by comparison with the already-counted Hanu bars.

## Printed inventory

The file stems below are under `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Trinkets/` and end in `__front.png`. D and M are printed Defense and Movement; A, R and r are printed Attack, Range and Radius. A missing printed icon is absent, never zero. Item icons are independent of card values.

| Stem | Face / colour / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | ---: | ---: | ---: | --- | --- |
| trinkets__7352735 | Rapid Redeployment / Gold / Basic | 12 | 1 | 4 | Basic Skill | - |
| trinkets2__7352737 | Salvage Parts / Silver / Basic | 5 | 2 | - | Basic Skill | - |
| trinkets3__7426589 | Makeshift Minigun / Red / I | 8 | 5 | 4 | Ranged Attack / A 3+, R 2 | - |
| trinkets4__7426588 | Gatling Gun / Red / II | 9 | 6 | 4 | Ranged Attack / A 3+, R 3 | Defense |
| trinkets5__7352733 | Steam Discharge / Red / II | 9 | 6 | 4 | Ranged Attack / A 4, R 4 | Initiative |
| trinkets6__7426587 | Supercharged Cannon / Red / III | 9 | 6 | 4 | Ranged Attack / A 4+, R 3 | Defense |
| trinkets7__7352739 | Flame Belcher / Red / III | 9 | 6 | 4 | Ranged Attack / A 5, R 4 | Radius |
| trinkets8__7562521 | Disruptor Jolt / Blue / I | 10 | 3 | 3 | Skill / r 3 | - |
| trinkets9__7562522 | Disruptor Pulse / Blue / II | 10 | 4 | 3 | Skill / r 4 | Attack |
| trinkets10__7562527 | Deployable Barrier / Blue / II | 10 | 4 | 3 | Skill / r 3 | Initiative |
| trinkets11__7562526 | Disruptor Grid / Blue / III | 11 | 4 | 3 | Skill / r 4 | Movement |
| trinkets12__7562525 | Deployable Bastion / Blue / III | 11 | 4 | 3 | Skill / r 3 | Initiative |
| trinkets13__7427033 | Early Prototype / Green / I | 3 | 2 | 2 | Skill / r 2 | - |
| trinkets14__7427034 | Updated Design / Green / II | 3 | 2 | 2 | Skill / r 2 | Attack |
| trinkets15__7427037 | Self-Destruct / Green / II | 3 | 2 | 2 | Skill / r 3 | Defense |
| trinkets16__7427039 | Perfected Design / Green / III | 2 | 3 | 2 | Skill / r 2 | Attack |
| trinkets17__7427040 | Emergency Protocol / Green / III | 2 | 3 | 2 | Skill / r 3 | Range |
| trinkets18__7544261 | Unlimited Firepower / Purple / IV | - | - | - | Ultimate | - |
| trinkets19__7352742 | Ramshackle Turret / Gold / Handicap | 12 | 1 | 4 | Basic Skill | - |

### Printed clauses

- **Rapid Redeployment:** choose to move up to 3 spaces and place the Turret adjacent to you (it counts as an obstacle), or defeat an adjacent minion.
- **Salvage Parts:** choose to place the Turret adjacent to you (obstacle), remove it then move up to 3 spaces, or remove it then optionally retrieve a discarded card.
- **Makeshift Minigun / Gatling Gun:** target a unit in Range of both you and the Turret; if the target is in a straight line from both, gain +2 Attack.
- **Supercharged Cannon:** the same dual-Range and straight-line clause, with +3 Attack.
- **Steam Discharge:** target a unit in Range adjacent to the Turret; may repeat once on a different enemy unit.
- **Flame Belcher:** the same Turret-adjacent target, with up to two repeats on different enemy units.
- **Disruptor Jolt / Disruptor Pulse:** this turn, before any enemy hero in the Turret's Radius performs a primary action, that hero discards a card if able; if they do, deactivate the effect.
- **Disruptor Grid:** the same timing and subject; that hero discards a card or is defeated, and a discard deactivates the effect.
- **Deployable Barrier / Deployable Bastion:** place up to two / three Barrier tokens in Radius, with at least one adjacent to the Turret; you and friendly heroes gain +1 Defense for each Barrier token they are adjacent to.
- **Early Prototype:** if you are in the Turret's Radius, swap with a unit or token there, then remove the Turret.
- **Updated Design:** if you are in the Turret's Radius, swap with a unit or token there; the Turret remains.
- **Self-Destruct:** up to two enemy heroes in the Turret's Radius discard a card if able; remove the Turret.
- **Perfected Design:** if you are in the Turret's Radius, choose to swap with a unit or token there, or place yourself into a space in the Turret's Radius.
- **Emergency Protocol:** up to two enemy heroes in the Turret's Radius discard a card or are defeated; remove the Turret.
- **Unlimited Firepower:** gain +1 Range and +1 Radius.
- **Ramshackle Turret (boxed Handicap):** move up to 3 spaces and place the Turret adjacent to you; it counts as an obstacle.

The inventory paraphrases for routing; source fronts own exact punctuation and order. Every implementation test must use the printed clause and the general rules rather than treating this summary as a replacement card.

## Capability mapping and outstanding audit

The existing printed-icon selection and placement clauses from 8.88/8.88a cover dual-centre geometry, a Turret-centred Radius, unit/token unions, and adjacency to a named piece. Existing token, direct Move, card retrieval, repeat, timed active-effect, discard/defeat, Swap, item and Ultimate contracts are the starting points. The new Turret is a persistent **non-token, non-terrain obstacle** with one-instance repositioning and a position other queries can reference. Probe results at 8.92: `AdjacentMoveAuthority.ObstacleOccupancies` and `BoardOccupancyIndex` already model generic obstacle identity and blocking but populate it only at board construction/restore; the dynamic placement/removal operation and Turret source lookup do not yet exist. `SyntheticTargetingCapabilities.MeasuredFrom` and `AlsoWithinReachOf` accept a published coordinate but need a node publishing the live Turret coordinate. `SyntheticPlacementCapabilities` handles board pieces, not dynamic generic obstacles. `SyntheticExchangeCapabilities` swaps units and tokens, correctly excluding the Turret from the Green faces' subjects. `TimingCheckpoint.DefenseResponse` is raised before `DefenseResponseChoiceRequested`, but `AttackDefenseCardActionStrategyBackend` derives the Defense offers before that checkpoint; 8.92c must reconcile that ordering with the designer's pre-step-3 discard and refresh the offered Defense choices. Existing token property sources derive from placed instances, suitable for the three Barrier tokens; they must not be reused to encode the Turret as a token.
