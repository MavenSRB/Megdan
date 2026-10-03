# Guards Digital - Emmitt Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The final-two-hero sequence](../Roadmap/PHASE_9_FINAL_TWO_HEROES.md) owns acceptance and status.

## Source and authority

Emmitt the Traveller is the thirty-first hero selected by the user's 2026-10-04 final-two instruction. The curated Tabletopia second-printing working source is `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Emmitt/`: nineteen byte-distinct fronts and nineteen byte-identical backs. All fronts, the selection profile, and dashboard were visually read on 2026-10-04. The source README distinguishes these current `timetraveler` cards from an older placeholder set in the bundle. The selection front SHA-256 is `E843CFC143EE489F7AC7DF9EE91548D09D114BDE58F342AA067D8C7659344CBE`; dashboard front SHA-256 is `D61013DF658865115A7F85E479B193FECF57C93EEBA52CF29E2717EF5BA48F0A`. They are private reference art, not permission to distribute it.

The profile prints four stars and Melee, Tactician, Durable, Disabler, Tokens. The dashboard names **Emmitt the Traveller** and has the four ordinary turn slots. The user confirmed **three Glitch tokens** on 2026-10-04. Rulebook pp.7-10 govern turn order, card states, choices and the Ultimate; pp.13-14 govern Attack and Defense; pp.17-19 govern Active effects, immunity, token supply and Swap. The [printed-wording rulings](PRINTED_WORDING.md) apply to each face. Exact card text outranks general rules under the [hierarchy](../RULES_LEDGER.md#authority-hierarchy).

## Printed inventory

File stems below are under `active-hero-cards/Emmitt/` and end in `__front.png`. `D` and `M` are ribbon Defense and Movement; `A`, `R` and `r` are primary Attack, Range and Radius. A dash means the icon is absent. Item icons are read against the established six-item glyphs. All eighteen non-Handicap fronts enter the deck; Tear in Time returns to the box at setup under p.6.

| Stem | Face / colour / tier | Init | D | M | Primary / printed values | Item |
| --- | --- | ---: | ---: | ---: | --- | --- |
| emmitt__7690881 | Reverse Time / Gold / basic | 11 | 2 | 1 | Basic Attack A4 | - |
| emmitt2__7690867 | Unstable Timeline / Silver / basic | 1 | 6 | - | Basic Defense / Skill r4 | - |
| emmitt3__7690871 | Temporal Punch / Red / I | 7 | 7 | 3 | Attack A9 | - |
| emmitt4__7690876 | Temporal Slam / Red / II | 7 | 7 | 3 | Attack A11 | Initiative |
| emmitt5__7690875 | Flashback / Red / II | 7 | 6 | 3 | Attack A5, r3 | Defense |
| emmitt6__7690874 | Temporal Judgement / Red / III | 8 | 8 | 3 | Attack A12 | Range |
| emmitt7__7690873 | Déjà Vu / Red / III | 8 | 7 | 3 | Attack A6, r3 | Defense |
| emmitt8__7690872 | Time Snare / Blue / I | 8 | 3 | 2 | Skill - Ranged R2 | - |
| emmitt9__7407314 | Time Trap / Blue / II | 9 | 4 | 2 | Skill - Ranged R3 | Defense |
| emmitt10__7407318 | Time Loop / Blue / II | 9 | 4 | 2 | Skill - Ranged R4 | Attack |
| emmitt11__7407320 | Time Bomb / Blue / III | 9 | 4 | 2 | Skill - Ranged R3 | Movement |
| emmitt12__7407322 | Time Warp / Blue / III | 9 | 4 | 2 | Skill - Ranged R4 | Radius |
| emmitt13__7475996 | Time Walk / Green / I | 6 | 2 | 2 | Skill - Ranged R3 | - |
| emmitt14__7475995 | Fast Forward / Green / II | 5 | 2 | 2 | Skill - Ranged R4 | Attack |
| emmitt15__7475994 | Time Capsule / Green / II | 5 | 2 | 2 | Skill r4 | Initiative |
| emmitt16__7475993 | Back to the Future / Green / III | 5 | 3 | 2 | Skill - Ranged R4 | Attack |
| emmitt17__7475992 | Future Proof / Green / III | 5 | 3 | 2 | Skill r4 | Initiative |
| emmitt18__7475991 | Alternative Timelines / Purple / IV | - | - | - | Ultimate | - |
| emmitt19__7407313 | Tear in Time / Gold / Handicap | 10 | 1 | 1 | Basic Attack A3 | - |

Upgrade lines: Temporal Punch to Temporal Slam or Flashback to Temporal Judgement or Déjà Vu; Time Snare to Time Trap or Time Loop to Time Bomb or Time Warp; Time Walk to Fast Forward or Time Capsule to Back to the Future or Future Proof. The twelve item glyphs comprise Attack x3, Defense x3, Initiative x3, and one each of Range, Radius and Movement.

### Exact printed clauses

- **Reverse Time:** "Target a unit adjacent to you. After the attack, **Next turn:** Heroes with lower initiative act before heroes with higher initiative; this effect ignores immunity. *(Resolve the Ties as normal.)*"
- **Unstable Timeline:** "Place 2 Glitch tokens in radius, with at least two spaces between each token; if used as a defense, place 3 tokens instead. An enemy hero in play chooses one of the Glitch tokens; you swap with that token. **End of turn:** Remove all Glitch tokens."
- **Temporal Punch / Temporal Slam / Temporal Judgement:** "Target a unit adjacent to you; when defending, the enemy hero must use the Initiative value of their card and items instead of the Defense value of their card and items." Temporal Punch additionally reminds: "*(Minion defense modifiers are applied as normal.)*"
- **Flashback / Déjà Vu:** "Target a unit adjacent to you. After the attack: You may place 3 [2] Glitch tokens in radius, with at least two spaces between each token; if you do, up to 1 enemy hero in radius swaps with a Glitch token of their choice. **End of turn:** Remove all Glitch tokens."
- **Time Snare / Time Trap:** "An enemy hero in range, who has already resolved a card this turn, discards a card, if able."
- **Time Loop:** "Swap with an enemy hero in range who has already resolved a card this turn."
- **Time Bomb:** "An enemy hero in range, who has already resolved a card this turn, discards a card, or is defeated."
- **Time Warp:** "**Choose one —** Swap with an enemy hero in range who has already resolved a card this turn. [Or:] An enemy hero in range swaps their unresolved card with their resolved card of their choice."
- **Time Walk / Fast Forward:** "Move an enemy hero in range, who remained in the same space since the last turn, 2 spaces in a straight line."
- **Time Capsule:** "You, and friendly heroes in radius, may retrieve all cards discarded this turn."
- **Back to the Future:** "**Choose one —** Place a unit in range into the space where that unit was at the start of this turn. [Or:] Move an enemy hero in range, who remained in the same space since the last turn, 2 spaces in a straight line."
- **Future Proof:** "**Choose one —** You, and friendly heroes in radius, may retrieve all cards discarded this turn. [Or:] **This turn:** Friendly heroes in radius, are immune to enemy actions."
- **Alternative Timelines:** "You may play two cards each turn. If you do, after the cards are revealed, retrieve one of your unresolved cards."
- **Tear in Time (boxed Handicap):** "Target a hero adjacent to you. After the attack, **Next Turn:** Heroes with lower initiative act before heroes with higher initiative. This effect ignores immunity."

## Settled questions, user 2026-10-04

The user saw the relevant fronts and confirmed all proposed readings. **EM-Q1:** Alternative Timelines' two cards are committed and revealed together; immediately after reveal, Emmitt may retrieve either newly played unresolved card, leaving the other as the only card whose Initiative and action are resolved that turn. **EM-Q2:** "Remained in the same space since the last turn" is a continuous position-history condition from the start of the previous turn through this action. Moving away and back fails it. These answers govern 9.46 and 9.48; the printed text and p.7 still govern all other timing.

## Contract map

Existing mechanisms: card-text straight-line Move and Place; typed token supply/short-supply, cleanup and Swap; `EffectRosterState` turn slots and card history; the p.14 Defense calculation; Active-effect `NextTurn` and `ThisTurn` windows; source-owned, seat-authorized choices; snapshot and replay. New contracts: a query over continuous unit position history, a turn-scoped resolved-card predicate for each hero, a next-turn ascending Initiative order while ordinary ties remain, substitution of the defending card's effective Initiative for its Defense value, and a second commitment that resolves to one chosen unresolved card after reveal. The card definitions consume those contracts; no card-name branch belongs in them.
