# Guards Digital - Min Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

## Min content ledger

Min is the nineteenth playable hero. [The implementation series](../Roadmap/PHASE_8_NINETEENTH_HERO.md) owns acceptance. This shard records her nineteen printed definitions, the general rules they lean on, her three printed token kinds, the capability map and the open readings. Nothing of hers is composed, registered or artworked yet; each later block updates the relevant row here.

## Source authority

The curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Min/` contains nineteen fronts and nineteen backs, stored internally under the Tabletopia name `min` (`min`, `min2` .. `min19`). Every front, the selection card and the dashboard were visually inspected on 2026-09-19; the twelve item icons were montaged beside Rowenna's six verified icons, the nineteen ribbons and corners were montaged, and the profile card's stat bars were read by scanline. The dashboard names **Min the Dragonmonk**; the selection card names **Min**, shows **two stars**, and lists **Tokens, Disabler, Damager, Melee**. Her faces name three token kinds: a **Smoke bomb** (Silver, Viper Stance, Cobra Stance, Poof!, Vanish, Ruse), **Mines** printed as **blast** and **dud** (Trip Mine, Cluster Mine, Mine Field) and a **Grenade** (Death Grenade, Holy Death Grenade). No marker is named.

MI-01: text extraction of all twenty-four pages of the local second-printing rulebook finds no Min, Dragonmonk or card-name mention. The extracted cards (verified on 2026-09-18 as the printed second edition wherever physical cards could be checked - see the parent ledger's "Edition of the extraction") are the local authority for individual text. General p.10 (mandatory and optional steps, making choices, picking targets, modal actions, repeat), p.11 (movement in the card text), p.13 (after-the-attack timing, Clear), p.14 (Block: "If the Block condition is fulfilled, you successfully defend, regardless of the attack value. If not, you are defeated"; the nine-step detailed order with Defense text at step 4), p.15-16 (level-up, items, item bonus), p.17 (End of turn, Friendly, If able), p.18 (obstacles, Place with its short-supply reclaim, Push, ignoring obstacles: "any special effects of tokens are still applied") and p.19 (straight line, Swap "does not count as movement or placement", Target "whenever your action has any effect on another unit in any way", Tokens: all tokens are obstacles, "Unless the card text says otherwise, tokens are removed at the end of round", "Tokens are not removed when the hero who placed them is defeated") govern.

MI-02: all nineteen backs share one MD5 (`74fbebc36183a9d8326556faf7ae5587`); this set has ONE deck back. The 2nd-printing atlas index is `cards.csv` atlas 5. The selection front is byte-identical to `cards/hero-selection/randomizers-tt8__7729915__front.png` (MD5 `f9b5947110f993e5c5cedb029f3a89dd`).

MI-03: every corner letter matches its ribbon (D, S, R x5, B x5, G x5, P, D-H); there is no printed colour conflict in this set.

MI-04: stat bars read Attack **4** plus **4** brighter conditional pips, Initiative **8**, Defense **3** plus **5** brighter pips, Movement **6** (p.21: brighter sections depend on level-up choices and card text). The dashboard's bottom bars agree.

MI-05: absent icons. Smoke Bomb prints only Defense 1 (no Movement). Poof!, Vanish and Ruse print only Movement 3 and **no Defense value at all**: they are Block cards (the exclamation shield) whose only defense is the Block, Tigerclaw's Dodge precedent. Crane, Tiger and Dragon Stance print no Range or Radius; Inner Strength and Perfect Self print none. Fast as Lightning and Chop Down print Range 1 with the Ranged band, so their reach is adjacency under the Ranged subtype. A missing icon is absent, never zero.

MI-06: Tabletopia manifest elements (`extracted/text-assets/manifest-bundled__42.json`): standee **`Min`** element 8597508 (publicElementId 714735, VerticalTokens/Standee, `Image_2_Atlas` sprites (3296,0) and (3600,0), 302x302); tokens **`Smoke_Bomb`** 1421883 (Tokens/Hexagonal, `Image_0_Atlas` (349,2526) 335x291), **`Mine_Dud`** 1421877 (`Image_0_Atlas` (0,2526) 335x291) and an older pair **`Mine_Dud`** 1134878 / **`Mine_Blast`** 1134879 (`Image_3_Atlas`, 424x369, two sprites each, sharing one preview). The grenade's face is the misnamed element 1421877 (MI-Q6); the setup layout is not in the extraction, so the physical token counts came from the user (MI-Q1).

## Printed inventory

All file stems below are under the Min source directory and end in `__front.png`. D/M are Defense/Movement ribbon icons; A is Attack, R Range, r Radius. A missing icon is absent, never zero. Item icons are independent of card values.

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| min__7582886 | Fast as Lightning / Gold / Basic | 13 | 1 | 1 | Basic Attack - Ranged / A 2, R 1 | - |
| min2__7348748 | Smoke Bomb / Silver / Basic | 12 | 1 | - | Basic Skill / r 3 | - |
| min3__7721789 | Crane Stance / Red / I | 9 | 5 | 5 | Attack / A 3 | - |
| min4__7721790 | Tiger Stance / Red / II | 9 | 5 | 5 | Attack / A 4 | Initiative |
| min5__7721791 | Viper Stance / Red / II | 9 | 5 | 5 | Attack / A 5, r 4 | Defense |
| min6__7721792 | Dragon Stance / Red / III | 10 | 6 | 5 | Attack / A 4 | Movement |
| min7__7721793 | Cobra Stance / Red / III | 10 | 6 | 5 | Attack / A 5, r 4 | Defense |
| min8__7348762 | Trip Mine / Blue / I | 10 | 4 | 2 | Skill / r 3 | - |
| min9__7582385 | Cluster Mine / Blue / II | 11 | 4 | 2 | Skill / r 3 | Attack |
| min10__7571085 | Death Grenade / Blue / II | 11 | 4 | 2 | Skill / r 2 | Initiative |
| min11__7582386 | Mine Field / Blue / III | 11 | 5 | 2 | Skill / r 3 | Attack |
| min12__7571084 | Holy Death Grenade / Blue / III | 11 | 5 | 2 | Skill / r 2 | Initiative |
| min13__7582935 | Poof! / Green / I | 2 | - | 3 | Block Defense - Ranged / R 3 | - |
| min14__7582936 | Vanish / Green / II | 2 | - | 3 | Block Defense - Ranged / R 4 | Defense |
| min15__7348761 | Inner Strength / Green / II | 2 | 2 | 3 | Skill | Attack |
| min16__7602331 | Ruse / Green / III | 1 | - | 3 | Block Defense - Ranged / R 4 | Radius |
| min17__7348755 | Perfect Self / Green / III | 1 | 3 | 3 | Skill | Range |
| min18__7543864 | Flurry of Blows / Purple / IV | - | - | - | Ultimate | - |
| min19__7348760 | Chop Down / Gold / Handicap | 12 | 1 | 1 | Basic Attack - Ranged / A 1, R 1 | - |

### Exact printed rules

Token glyphs in the text are written in brackets.

- **Fast as Lightning:** Target a unit in range. After the attack: Apply the "After the attack" text of your resolved or discarded red card. (If it has radius, use that card's value).
- **Smoke Bomb:** Place a [Smoke bomb] smoke bomb token in radius; enemy heroes cannot target you or another unit if the smoke bomb token is on a straight line between that enemy hero and their target.
- **Crane Stance:** Target a unit adjacent to you. After the attack: Push an enemy unit adjacent to you up to 3 spaces.
- **Tiger Stance:** Target a unit adjacent to you. After the attack: You may move 1 space to a space adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces.
- **Viper Stance:** Target a unit adjacent to you. After the attack: You may swap with a [Smoke bomb] Smoke bomb in radius.
- **Dragon Stance:** Target a unit adjacent to you. After the attack: You may move 1 or 2 spaces to a space adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces.
- **Cobra Stance:** Target a unit adjacent to you. After the attack: You may swap with a [Smoke bomb] Smoke bomb in radius; if you do, you may place the Smoke bomb into a space in radius.
- **Trip Mine:** Place 2 mine tokens, 1 [Mine] blast and 1 [Mine] dud, facedown in radius; units may move through them. After an enemy hero moves through a mine, remove it; for every blast removed, that hero discards a card, if able.
- **Cluster Mine:** Place 3 Mine tokens, 1 [Mine] blast and 2 [Mine] duds, facedown in radius; units may move through them; remove mine tokens after an enemy hero moves through them; for every blast removed, that hero discards a card, if able.
- **Death Grenade:** Place a [Grenade] Grenade token into a space in radius. End of turn: Up to 1 enemy hero adjacent to that token discards a card, or is defeated. Remove the Grenade token.
- **Mine Field:** Place 3 Mine tokens, 2 [Mine] blasts and 1 [Mine] dud, facedown in radius; units may move through them; remove mine tokens after an enemy hero moves through them; for every blast removed, that hero discards a card, if able.
- **Holy Death Grenade:** Place a [Grenade] Grenade token into a space in radius. End of turn: Up to 2 enemy heroes adjacent to that token discard a card, or are defeated. Remove the Grenade token.
- **Poof! / Vanish:** Swap with a [Smoke bomb] Smoke bomb token in range; if you do, block the attack.
- **Inner Strength:** This round: Double your item bonuses.
- **Ruse:** Swap with a [Smoke bomb] Smoke bomb in range; if you do, block the attack. You may place the Smoke bomb into a space in range.
- **Perfect Self:** Choose one or both - (1) This round: Double your item bonuses. (2) Take a Tier II card from your deck and add it to your dashboard as a permanent item.
- **Flurry of Blows (Ultimate):** Each time after you perform an attack action you may repeat it once on a different target.
- **Chop Down (Handicap):** Target a hero adjacent to you. After the attack: Apply the "After the attack" text of your resolved or discarded red card.

## Setup and progression

Starting hand: Fast as Lightning, Smoke Bomb, Crane Stance, Trip Mine, Poof!. Deck: twelve Tier II/III faces plus Flurry of Blows. Each colored Tier I upgrades to either face of its Tier II, then either Tier III (Red to Tiger or Viper Stance then Dragon or Cobra Stance; Blue to Cluster Mine or Death Grenade then Mine Field or Holy Death Grenade; Green to Vanish or Inner Strength then Ruse or Perfect Self); the unchosen face becomes its printed item (Initiative x3, Defense x3, Attack x3, Movement, Radius, Range). Because p.15 replaces the lower Tier, Inner Strength and Perfect Self never coexist in one deck, so their two "Double" sentences never stack. Chop Down remains the established odd-player Handicap deferral (p.20). Her tokens are placed by her cards only; p.19 removes every token at the end of the round unless its text says otherwise, and the Grenade's own text removes it at the end of its turn.

## Capability contract map

These routes identify the composed contracts. Registration at the dealing block makes each reachable from dealt content; the gate block separately owns complete configured-player coverage.

| Printed clause | Existing foundation / required delta | Owner |
| --- | --- | --- |
| The Smoke bomb token; place it in radius (Silver) or re-place it in radius (Cobra) or range (Ruse) | `TokenDefinition` (Widget's Pyro, Tali's Ice: supply per MI-Q1, `EndOfRoundCleanup`), `SyntheticTokenCapabilities.PlaceHandler` for supply Place and identity-preserving relocation, `SyntheticOwnedTokenCapabilities` presence/space facts | 8.61a |
| Enemy heroes cannot target you or another unit if the token is on a straight line between them (Silver) | NEW token property: a targeting screen read at the one seam every selector, push, swap and attack-target query already reaches (`TargetQuery.IsPrevented`), measured with `StraightLineQuery` - MI-Q2 | 8.61a |
| Swap with a Smoke bomb in radius (Viper, Cobra) or in range (Poof!, Vanish, Ruse) | Widget's `SwapActor` over the owned-token space fact on the card's own channel (`synthetic-swap-actor`) | 8.61a; faces 8.61b, 8.61c |
| Swap ...; if you do, block the attack (Poof!, Vanish, Ruse) | NEW `PrintedBlockCondition` member (an owned Smoke bomb within the card's Range of the defender - the mandatory swap's own precondition, so `DefenseBlockQuery` and the defense preview agree by construction, 8.40g's law); the swap as `CardTextTiming.DefenseAction` text at p.14 step 4; no Defense value, so a failed Block is a defeat - MI-Q8 | 8.61b |
| Push an enemy unit adjacent to you up to 3 spaces (Crane, Tiger, Dragon) | `synthetic-object-push@3`, subject chosen by Min among adjacent enemy units | 8.61c |
| You may move 1 space / 1 or 2 spaces to a space adjacent to the target (Tiger, Dragon) | `synthetic-direct-move@3` with a NEW destination policy naming the attack's target (or a Place-qualifier twin); a removed target is p.10's stop; a card-text move under p.11 | 8.61c |
| Place N mine tokens, blasts and duds, facedown in radius; units may move through them (Blues) | NEW: two `TokenDefinition`s in one `SupplyGroupId` (Tali's RULING 44 shape) plus a projection-only facedown definition; NEW traversable-obstacle property read by `MovementReachabilityQuery` and the push path; per-seat masking under 8.40f's projection-filter rule, `NetworkTokenRow` keeping its shape - MI-Q1, MI-Q3 | 8.61d |
| After an enemy hero moves through a mine, remove it; for every blast removed, that hero discards a card, if able | NEW trap seam over the board authority's displacement commits (step, route, push, direct, coupled and batch moves), then removal, public reveal and `synthetic-discard-chosen-card@1` (IfAble, chooser = that hero) as a nested interrupt (Xargatha's after-push interrupt) - MI-Q3 | 8.61e |
| Place a Grenade token in radius; End of turn: up to N adjacent enemy heroes discard a card, or are defeated; remove the token (Death Grenade, Holy Death Grenade) | `TokenDefinition` + `PlaceHandler` (Radius); Silverarrow's `CreateEndOfTurnDiscard` End-of-turn Active effect anchored on the placed instance (Widget's `enemy-unit-beside-anchor`); `DiscardChosenCardV2` escalating to `synthetic-defeat-hero@1` (Widget's Breath); `synthetic-reclaim-token` - MI-Q4 | 8.61f |
| Apply the "After the attack" text of your resolved or discarded red card, with its radius (Fast as Lightning; Chop Down parked) | NEW borrowed-rider capability over the owned Red card when resolved or discarded (`BorrowedActionPool` precedent), running that card's `AfterTheAttack` body in this attack's frame with the red card's Radius - MI-Q7 | 8.61g |
| This round: Double your item bonuses (Inner Strength, Perfect Self) | ThisRound Active effect + NEW `RuleModificationKind` item-bonus multiplier read by `ItemBonusQuery` at its eight call sites, the 8.55 badge included | 8.61h |
| Take a Tier II card from your deck and add it to your dashboard as a permanent item; Choose one or both (Perfect Self) | NEW deck-card choice + `CardLifecycleAuthority.TrySetAsideFromDeck` + `HeroItemState.TryPlaceSetAsideCard` (the p.15/16 path); `Choose` min 1 / max 2 - MI-Q5 | 8.61h |
| Each time after you perform an attack action you may repeat it once on a different target (Flurry of Blows) | Tigerclaw's Cloak and Daggers (`TriggeredAbility`, `RepeatAction` max 1, `RepeatTargetBinding.DifferentTarget`) under a NEW `UltimateTriggerCondition` for any attack action, the repeated instance excluded - MI-Q9 | 8.61h |
| Printed set, upgrades, items, registration and artwork | `MinCardContent` composes nineteen definitions, five starting cards, thirteen deck cards and the boxed Handicap; only the Handicap is deferred; three token arts - MI-Q6 | 8.61i-8.61j; gate 8.61k |

## Integration identity and private artwork

Not yet recorded: the dealing block records schema/wire (v43 and `/17` at the time of writing), the content identity pin, the selection front's SHA-256, the standee and the three token arts.

## Open verification points

- **MI-Q1 (user, settled 2026-09-19):** the physical token counts are one Smoke bomb, two blasts, two duds and one Grenade ("you are right"); p.18's short-supply reclaim covers any shortfall.
- **MI-Q2:** the screen. "On a straight line between" is read with p.19's straight line: the token's space lies strictly between the enemy hero and the target on one of the six axes; adjacent pairs are never screened. Only enemy heroes' targeting is prevented, and p.19's target definition makes that every attack, push, swap and card-text choice naming the unit; presence checks, the Minion Battle, targeting the token itself or a space, and Min's own team are unaffected.
- **MI-Q3:** "moves through". A Movement action's route, a card-text move and a push that carries an enemy hero over the mine's space all count (p.18 defines a push as moving the unit); Swap, Place, Fast Travel and respawn do not (p.19). No unit ends its movement on a mine (p.18). When several routes are legal the mover's own steps decide; a one-click destination takes a route that avoids mines when an equal-cost route exists - 8.61e measures and records this reading.
- **MI-Q4:** "Up to N enemy heroes adjacent to that token": Min chooses which and how many (p.10, you make all the choices unless the text says otherwise); each chosen hero discards a card of their own choice or, unable, is defeated (ledger question 13's reading, Widget's Breath); "that token" is the instance the card placed, and if it is gone by the end of the turn nothing happens. Superseded in part on 2026-09-28 by the user's ruling **W3** ([PRINTED_WORDING](PRINTED_WORDING.md)): every enemy hero adjacent to the token is hit, up to N; Min chooses which when more stand there, never whether. Applied at milestone 9.1.
- **MI-Q5 (user, settled 2026-09-19):** Perfect Self's second bullet takes any Tier II card in the deck zone, including a Tier II returned there by a Tier III upgrade (p.15 returns the upgraded card to the deck). The later level-up the question imagined is UNREACHABLE (the user: "since perfect self is tier 3, the only case we have is 'color already at tier 3'"): p.15 upgrades the lowest-Tier card first and "You must first upgrade all your non-basic cards to Tier II, before upgrading your Tier II cards to Tier III", so while Perfect Self is in hand every colour is at Tier II or III, the only Tier II cards in the deck are the ones Tier III upgrades returned, and those colours never level up again. 8.61h's one-candidate level-up arm is a defensive widening no legal match reaches.
- **MI-Q6 (user, settled 2026-09-19):** the Grenade's face IS in the extraction - the element numbered 1421877 is NAMED `Mine_Dud` in the manifest but draws a fiery skull-bomb hex (`Image_0_Atlas` (0,2526) 335x291), which the user identified as the grenade token; the two older mine elements share one facedown danger-sign back (`Image_3_Atlas` (2677,562)) and carry the blast (a green landmine, (3103,562)) and dud (a bare grass patch, (3543,562)) faces.
- **MI-Q7:** Fast as Lightning binds "the target" to this attack's target and "in radius" to the red card's Radius with items; a red card still in hand or unresolved offers nothing (p.10 impossible step, the attack itself unaffected).
- **MI-Q8:** without a Smoke bomb in the card's range the Block fails and p.14 defeats her; with one, the swap is mandatory and the attack is blocked; the attacker's "After the attack" text still applies, from the new positions.
- **MI-Q9:** Flurry of Blows triggers after every attack action of hers, basic or Red, once per performance; the repeated instance never re-triggers (p.10: "You cannot repeat a repeated action"); the repeat excludes the first target and her after-attack riders apply on each instance.
- **MI-Q13k (user, settled 2026-09-20):** the Smoke bomb is removed at the end of the round like every token ("Rules say that all tokens are removed at the end of round."); it prints no exception. With one turn per round the bomb exists only in the round her Silver resolved in, so Viper Stance's and Cobra Stance's "you may swap with a Smoke bomb in radius" (and Cobra's gated re-place, and the Silver's own relocate branch) are reachable by no sequence of legal play, while every bomb-consuming defence (Poof!, Vanish, Ruse - played from hand on another seat's turn in the same round) works. 8.61k's journeys assert the measured reading; the token keeps `EndOfRoundCleanup`; the printed `may` stays declared and unpressed.
