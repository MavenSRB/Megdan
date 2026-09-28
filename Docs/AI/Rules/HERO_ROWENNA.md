# Guards Digital - Rowenna Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

> Milestone 9.1: The Paragons' "repeat once on a different target" is automatic under the user's ruling **W1** ([PRINTED_WORDING](PRINTED_WORDING.md)), applied at milestone 9.1; it was composed declinable at 8.57a.

## Rowenna content ledger

Rowenna is the eighteenth playable hero. [The implementation series](../Roadmap/PHASE_8_EIGHTEENTH_HERO.md) owns acceptance. This shard records her nineteen printed definitions, the general rules they lean on, the capability map and the open readings. Nothing of hers is composed, registered or artworked yet; each later block updates the relevant row here.

## Source authority

The curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Rowenna/` contains nineteen fronts and nineteen backs, stored internally under the Tabletopia name `knight` (the directory README records the normalization). Every front, the selection card and the dashboard were visually inspected on 2026-09-19; the twelve item icons were montaged beside Wasp's six verified icons, the nineteen ribbons and corners were montaged, and the profile card's stat bars were read by scanline. The dashboard names **Rowenna the Vanguard**; the selection card names **Rowenna**, shows **two stars**, and lists **Melee, Durable, Healer, Farming, Pusher, Tactician**. No token, marker or component is named by the set.

RO-01: text extraction of all twenty-four pages of the local second-printing rulebook finds no Rowenna or Vanguard mention. The extracted cards (verified on 2026-09-18 as the printed second edition wherever physical cards could be checked - see the parent ledger's "Edition of the extraction") are the local authority for individual text. General p.9 anatomy, p.10 card states (discard from hand unless the text says otherwise; retrieve is always your own card; "you can never retrieve another hero's cards"), p.13 Defeat rewards (Defeat coins = level to the defeating hero, assist coins and Life counters = the defeated hero's lowest card Tier), p.15 level-up ("the same color as the chosen card but one Tier higher"), p.18 Place, p.19 Swap ("does not count as movement or placement"), p.20 Handicap and p.21 roles govern.

RO-02: all nineteen backs share one MD5 (`b853742139055da943e366b5cb62af6c`); this set has ONE deck back. The 2nd-printing atlas index is `cards.csv` atlas 9.

RO-03: **printed conflict, unresolved.** Four faces print a colour letter that contradicts their own ribbon, band and frame: Accept Surrender (II) and Glorious Triumph (III) carry blue ribbons and blue "Skill" bands but a corner **G**; Melee (II) and Grand Melee (III) carry green ribbons and green "Movement" bands but a corner **B**. Every other face's letter matches its ribbon. Either reading gives each colour one Tier I, two Tier II and two Tier III faces, so the deck shape does not decide it; the level-up line does, because p.15 upgrades by colour. This shard records the RIBBON colour as the working assumption (Melee's text is Close Quarters' text; Glorious Triumph is Accept Surrender's Tier III) and holds RO-Q1 for the user's physical cards.

RO-04: stat bars read Attack **8**, Initiative **4**, Defense **7**, Movement **3** plus **2** brighter conditional pips (p.21: brighter sections depend on level-up choices and card text). The dashboard's bottom bars agree.

RO-05: Melee and Grand Melee print no Movement ribbon icon at all (only Defense 4); Close Quarters and Throw the Gauntlet print only Defense. A missing icon is absent, never zero.

## Printed inventory

All file stems below are under the Rowenna source directory and end in `__front.png`. D/M are Defense/Movement ribbon icons; A is Attack, R Range, r Radius. A missing icon is absent, never zero. Item icons are independent of card values.

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| rowenna__7438963 | Code of Chivalry / Gold / Basic | 11 | 2 | 1 | Basic Attack / A 4 | - |
| rowenna2__7645264 | Throw the Gauntlet / Silver / Basic | 5 | 3 | - | Basic Skill - Ranged / R 4 | - |
| rowenna3__7550273 | Token of Gratitude / Red / I | 8 | 6 | 4 | Attack / A 5, r 1 | - |
| rowenna4__7550274 | Fair Share / Red / II | 8 | 6 | 4 | Attack / A 6, r 3 | Initiative |
| rowenna5__7550276 | Feat of Bravery / Red / II | 8 | 7 | 4 | Attack / A 7, r 4 | Defense |
| rowenna6__7550277 | Paragon of Grace / Red / III | 9 | 7 | 4 | Attack / A 6, r 3 | Range |
| rowenna7__7550278 | Paragon of Valor / Red / III | 9 | 8 | 4 | Attack / A 7, r 4 | Defense |
| rowenna8__7550331 | Stand Guard / Blue / I | 9 | 6 | 2 | Skill - Ranged / R 3 | - |
| rowenna9__7550332 | Protect the Weak / Blue / II | 10 | 6 | 2 | Skill - Ranged / R 4 | Attack |
| rowenna10__7605650 | Accept Surrender / Blue (corner G, RO-03) / II | 10 | 6 | 2 | Skill | Defense |
| rowenna11__7550335 | Defend the Innocent / Blue / III | 10 | 7 | 2 | Skill - Ranged / R 4 | Radius |
| rowenna12__7624923 | Glorious Triumph / Blue (corner G, RO-03) / III | 10 | 7 | 2 | Skill | Movement |
| rowenna13__7727061 | Close Quarters / Green / I | 4 | 3 | - | Movement / 2, r 2 | - |
| rowenna14__7727060 | Melee / Green (corner B, RO-03) / II | 4 | 4 | - | Movement / 2, r 3 | Attack |
| rowenna15__7727059 | Opening Shots / Green / II | 4 | 4 | 2 | Skill / r 3 | Initiative |
| rowenna16__7727058 | Grand Melee / Green (corner B, RO-03) / III | 3 | 4 | - | Movement / 2, r 3 | Attack |
| rowenna17__7727057 | Opening Volley / Green / III | 3 | 4 | 2 | Skill / r 3 | Initiative |
| rowenna18__7727056 | Fabled Lance / Purple / IV | - | - | - | Ultimate / R 2 | - |
| rowenna19__7439033 | Earn the Spurs / Gold / Handicap | 10 | 1 | 1 | Basic Attack / A 4 | - |

### Exact printed rules

- **Code of Chivalry:** Target a unit adjacent to you. Before the attack: If you target a hero, both you and the target may retrieve a discarded card.
- **Throw the Gauntlet:** Place yourself into a space in range adjacent to an enemy hero in range; that hero may move 1 space; if they do, gain 2 coins.
- **Token of Gratitude / Fair Share:** Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 coin.
- **Feat of Bravery:** Target a unit adjacent to you. After the attack, a friendly hero in radius may retrieve a discarded card.
- **Paragon of Grace:** Target a unit adjacent to you. After the attack: A friendly hero in radius gains 1 coin. If they do, repeat once on a different target.
- **Paragon of Valor:** Target a unit adjacent to you. After the attack, a friendly hero in radius may retrieve a discarded card; If they do, repeat once on a different target.
- **Stand Guard / Protect the Weak:** Swap with a friendly unit in range which is adjacent to an enemy hero, or has a card in the discard.
- **Accept Surrender:** Defeat an enemy hero adjacent to you with no cards in hand.
- **Defend the Innocent:** Swap with a friendly unit in range which is adjacent to an enemy hero, or has a card in the discard; you may retrieve a discarded card.
- **Glorious Triumph:** Defeat an enemy hero adjacent to you with no cards in hand; your friendly heroes gain triple assist coins.
- **Close Quarters / Melee:** After movement, if you are adjacent to an enemy hero, you may Choose one - (1) Place a friendly minion in radius into a space adjacent to that enemy hero. (2) Place an enemy minion in radius into a space adjacent to you.
- **Grand Melee:** After movement, if you are adjacent to an enemy hero, Choose up to two times - (the same two bullets).
- **Opening Shots:** If both you and an enemy hero in radius have no cards in the discard, that hero discards a card, if able.
- **Opening Volley:** If both you and an enemy hero in radius have no cards in the discard, that hero discards a card, if able. May repeat once.
- **Fabled Lance (Ultimate):** All your attack actions gain the "Ranged" subtype, target a unit in range, and count as having a printed Range value of 2.
- **Earn the Spurs (Handicap):** Target a hero adjacent to you. Before the attack: Both you and the target may retrieve a discarded card.

## Setup and progression

Starting hand: Code of Chivalry, Throw the Gauntlet, Token of Gratitude, Stand Guard, Close Quarters. Deck: twelve Tier II/III faces plus Fabled Lance. Each colored Tier I upgrades to either face of its Tier II, then either Tier III (under RO-03's working assumption: Red to Fair Share or Feat of Bravery then Paragon of Grace or Valor; Blue to Protect the Weak or Accept Surrender then Defend the Innocent or Glorious Triumph; Green to Melee or Opening Shots then Grand Melee or Opening Volley); the unchosen face becomes its printed item. Earn the Spurs remains the established odd-player Handicap deferral (p.20).

## Capability contract map

These routes identify the composed contracts. Registration at the dealing block makes each reachable from dealt content; the gate block separately owns complete configured-player coverage.

| Printed clause | Existing foundation / required delta | Owner |
| --- | --- | --- |
| Both you and the target may retrieve (Gold, Handicap) | `retrieve-card@1` takes `owner` and `chooser`; two nodes, each hero choosing their own card, the target's node bound through the node `chooser` (Cutter/Whisper precedent); p.10 forbids retrieving another hero's card | 8.57a |
| A friendly hero in radius gains 1 coin / may retrieve (Reds) | `gain-resource@1` takes `recipient`; the friendly hero is Rowenna's choice through the radius hero selector's friendly narrowing; the retrieve is owner = chooser = that hero | 8.57a |
| If they do, repeat once on a different target (Paragons) | Wasp's "if you do" conditional over the rider's performed output, then Cutter Broadside's different-target repeat (8.39d1) | 8.57a |
| Swap with a friendly unit in range which is adjacent to an enemy hero, or has a card in the discard (Blues) | `choose-swap-subject@1` (Whisper's Cruel Twist); NEW candidate narrowing: friendly unit AND (adjacent to an enemy hero OR a hero with a discarded card); minions never qualify by discard | 8.57b |
| Place yourself in range adjacent to an enemy hero in range; that hero may move 1 space; if they do, gain 2 coins (Silver) | Arien/Misa/Swift self-place with a NEW destination narrowing; a one-space move node whose `chooser` is the enemy hero (the mover, not the actor - RO-Q2); `gain-resource@1` gated on the move's performed output | 8.57b |
| Defeat an enemy hero adjacent to you with no cards in hand (Accept Surrender, Glorious Triumph) | `synthetic-defeat-hero` (Whisper modal, Cutter plank) with a NEW empty-hand narrowing (8.40f's `minimumCardsInHand` has no complement) | 8.57c |
| Your friendly heroes gain triple assist coins (Glorious Triumph) | NEW scoped multiplier on `HeroDefeatRewardSchedule.AssistCoins` for that Defeat only, at the one p.13 spend site (`ResolveHeroDefeat`, both entrances) - RO-Q3 | 8.57c |
| After movement, if adjacent to an enemy hero, place a friendly minion adjacent to that hero / an enemy minion adjacent to you (Greens) | After-Movement rider with the adjacency predicate; Wasp Telekinesis' place-a-unit-adjacent shape over `SyntheticPlacementCapabilities`; Widget Dragon Bond's `Choose one`; Grand Melee's "Choose up to two times" = Garrus/Sabina/Wasp "May repeat up to two times" over the modal - RO-Q4 | 8.57d |
| If both you and an enemy hero in radius have no cards in the discard, that hero discards (Opening Shots/Volley) | NEW negative discard-presence predicate on both parties (positive form: `enemy-hero-with-discard-present@1`, `owned-card-present@1`); `discard-owned-card@1` chosen by that hero; Volley's "May repeat once" is p.10's repeat | 8.57d |
| All your attack actions gain the Ranged subtype, target a unit in range, count as printed Range 2 (Fabled Lance) | Ultimate `PermanentDeclaration` (Reign of Winter precedent) of a NEW rule read by `AttackTargetQuery` (`AttackRangeKind`), `PrintedBlockCondition.AttackIsRanged` and the Range Total (her Range items then count) - RO-Q5 | 8.57e |
| Printed set, upgrades, items, registration and artwork | `RowennaCardContent` composes nineteen definitions, five starting cards, thirteen deck cards and the boxed Handicap; only the Handicap is deferred | 8.57f-8.57g; gate 8.57h |

## Integration identity and private artwork

Not yet recorded: the dealing block records schema/wire (v43 and `/17` at the time of writing), the content identity pin, the selection front's corpus twin and hash, and the standee/token art element.

## Open verification points

- **RO-Q1 (user):** RO-03's colour letters. Which colour do the physical Accept Surrender, Glorious Triumph, Melee and Grand Melee cards carry? The ribbon colour is assumed until answered.
- **RO-Q2:** Throw the Gauntlet's "that hero may move 1 space" is the enemy hero's decision (may = theirs to decline), a card-text move rather than a Movement action (p.19), and Rowenna's 2 coins follow only when the move was performed.
- **RO-Q3:** Glorious Triumph's "your friendly heroes" are Rowenna's team-mates; she collects the Defeat coins (level) as the defeating hero, and only the assist coins are tripled; Life counters are unchanged.
- **RO-Q4:** Close Quarters' "that enemy hero" is the one named by the predicate, chosen once when several are adjacent; the second bullet needs no hero. Grand Melee may take the same bullet twice.
- **RO-Q5:** Fabled Lance makes every Attack Ranged for Block conditions (Reflect Projectiles-style Ranged Blocks now answer her) and reads a Range Total of 2 plus items; Radius on her Attack faces is unaffected.
- **RO-Q6:** Earn the Spurs is parked (p.20).
