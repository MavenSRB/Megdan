# Guards Digital - Takahide Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The thirtieth-hero sequence](../Roadmap/PHASE_9_THIRTIETH_HERO.md) owns acceptance and status.

## Source and rule authority

Takahide the Warlord is the selected thirtieth hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Takahide/` holds **twenty-one** byte-distinct fronts and twenty-one backs sharing one MD5 (`7566e55107c3d3f31ecb4e786f0309ad`, a fan over a sheathed sword). All twenty-one fronts, the selection profile and the dashboard were read at native size, and every text box at 2x, on 2026-10-01. The profile (SHA-256 `8ff7a6df0be5b01e9b70e22deeeb3e90fd2581979a89d4f661279733e29c634d`) shows **four stars** and **Durable, Sniper, Tactician, Damager, Farming**. The dashboard (SHA-256 `1b59e70834f01068877354238eec7025d03a00636b307bb4dd92a2fb1cc472ed`) gives the title "Takahide the Warlord" and four ordinary turn slots. These are source references, not distribution permission. Montages and contact sheets are preserved in `tmp/9-32-takahide-inventory/`.

TK-01, stat bars: a centre-scanline pixel reading on the profile (pip pitch 7 px, the bar geometry of earlier inventories) gives Attack **5** plus **2** brighter pips, Initiative **3** plus **2**, Defense **4** plus **3**, Movement **1** plus **4** (p.21: brighter sections depend on level-up choices and card text). The dashboard's bottom bars agree by eye.

TK-02, manifest (`extracted/text-assets/manifest-bundled__42.json`): standee **`Taka`** element 8596254 (publicElementId 5917489, VerticalTokens, `Image_1_Atlas` sprites (3543,1096) and (0,1577), each 302x302). No token element: Takahide uses no tokens or markers. The deck is the random-element group "Takahide the Warlord" (groupId 8170785) naming the twenty-one card elements. 9.32h confirms the standee crops against the profile portrait before import.

TK-03, rulebook: the local second-printing text names no Takahide, Warlord or card title. General authority is p.4 (Hidden Information: cards in the deck, in hand and facedown are hidden; unresolved, resolved and discarded cards may be asked about), p.6 (setup: the starting hand is the Tier I cards and the basic cards; the rest return to the deck; "You never draw cards from your deck"; the Handicap card returns to the box), p.7 (Resolve Your Card: "the player with the highest Initiative on their unresolved card is next to act", checked after each resolution), p.8 (End of Round: Retrieve Cards, then level up), p.9 (card types by primary action), p.10 (Making Choices, Picking Targets, Card States, Discard, Retrieve, **Swap a Card**, **Facedown**), p.11 (Movement in card text), p.12-14 (Attack, After the attack), p.15-16 (Leveling Up, the Ultimate), p.17 (Active effects, This turn, Empty space), p.18 (Obstacle, Ignoring Obstacles, Place, Push), p.19 (Swap of units and tokens, Terrain) and p.20 (Handicap cards). Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

TK-04, corner glyphs: the corner letters (D, S, R, B, G, P) do not track colour (Blue Proven Warrior prints `G`, Green Come to Aid prints `B`), as Mrak's MR-04 found; colour, tier and upgrade line are read from frame, ribbon and primary-icon colour. Sting like a Bee and Strike like a Tiger print a `>` glyph where the other basic cards print the plain gear: they are the two gold cards whose text says they start in the deck. The glyph states no rule.

## Printed inventory

File stems below are under the Takahide source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons; A/R/r are the primary action's Attack, Range and Radius icons. A missing icon is absent, never zero. Items are the independent bottom glyphs, verified by montage against Mrak's six recorded item icons; Range and Radius glyphs were verified the same way.

| Stem | Face / colour / tier | Init | D | M | Primary / icon | Item |
| --- | --- | ---: | ---: | ---: | --- | --- |
| takahide__7653240 | Sting like a Bee / Gold / Basic | 7 | 1 | 1 | Basic Attack - Ranged / A 5, R 3 | - |
| takahide2__7570661 | Float like a Butterfly / Gold / Basic | 8 | 8 | - | Basic Movement 5 | - |
| takahide3__7570663 | Strike like a Tiger / Gold / Basic | 9 | 3 | 3 | Basic Attack / A 7 | - |
| takahide4__7653243 | Bushido / Silver / Basic | 5 | 6 | - | Basic Skill | - |
| takahide5__7570684 | Set an Example / Red / I | 11 | 1 | 1 | Attack / A 2, r 3 | - |
| takahide6__7570665 | Lead from the Front / Red / II | 11 | 2 | 1 | Attack / A 3, r 4 | Initiative |
| takahide7__7570667 | Spinning Blade / Red / II | 11 | 2 | 1 | Attack / A 3, r 4 | Defense |
| takahide8__7570674 | Hold my Saké / Red / III | 12 | 2 | 1 | Attack / A 3, r 4 | Movement |
| takahide9__7570670 | Blade Helix / Red / III | 12 | 2 | 1 | Attack / A 3, r 1 | Range |
| takahide10__7691539 | Proven Warrior / Blue / I | 9 | 4 | 2 | Skill / r 3 | - |
| takahide11__7570672 | Chosen Champion / Blue / II | 10 | 4 | 2 | Skill / r 4 | Attack |
| takahide12__7721738 | Calculated Risk / Blue / II | 10 | 4 | 2 | Skill / r 4 | Initiative |
| takahide13__7570668 | The Right Hand / Blue / III | 10 | 5 | 2 | Skill / r 4 | Attack |
| takahide14__7721739 | Tactical Gambit / Blue / III | 10 | 5 | 2 | Skill / r 4 | Initiative |
| takahide15__7653282 | Come to Aid / Green / I | 4 | 3 | 2 | Skill - Ranged / R 3 | - |
| takahide16__7653283 | Bring the Relief / Green / II | 3 | 4 | 2 | Skill - Ranged / R 4 | Attack |
| takahide17__7570669 | Pledge of Allegiance / Green / II | 3 | 4 | 2 | Skill - Ranged / R 4 | Defense |
| takahide18__7653285 | Commit Reserves / Green / III | 3 | 4 | 2 | Skill - Ranged / R 4 | Radius |
| takahide19__7570676 | Loyal Retainer / Green / III | 3 | 4 | 2 | Skill - Ranged / R 4 | Defense |
| takahide20__7583907 | Ready for War / Purple / IV | - | - | - | Ultimate | - |
| takahide21__7570673 | Shameful Display / Silver / Handicap | 5 | - | - | Basic Defense, "!" in place of a value | - |

Items: Attack x3, Defense x3, Initiative x3, Movement x1, Range x1, Radius x1. Upgrade lines: Red Set an Example -> Lead from the Front or Spinning Blade -> Hold my Saké or Blade Helix; Blue Proven Warrior -> Chosen Champion or Calculated Risk -> The Right Hand or Tactical Gambit; Green Come to Aid -> Bring the Relief or Pledge of Allegiance -> Commit Reserves or Loyal Retainer. p.15's level-up list is validated whole at dealing (the Snorri lesson: a Tier II names both Tier III faces of its colour).

**Deck shape.** Twenty cards are dealt and the twenty-first, Shameful Display, is the p.20 Handicap and stays boxed at setup. He has **three gold cards**, not one. By their own text Float like a Butterfly starts in the hand, and Sting like a Bee and Strike like a Tiger start in the deck. The starting hand is therefore Float like a Butterfly, Bushido and the three Tier I cards: five cards, as p.6 and p.10 expect. The deck holds the other two gold cards, the ten Tier II and III cards and the Ultimate.

### Printed clauses

Quoted from the fronts; each implementation test uses the front, not this routing copy. Reminder text is in parentheses as printed.

- **Sting like a Bee:** "Target a unit at maximum range. After the attack: Swap this card with a different gold card in your deck. (This card starts the game in your deck)."
- **Float like a Butterfly:** "Swap this card with a different gold card in your deck. (This card starts the game in your hand.)"
- **Strike like a Tiger:** "Target a unit adjacent to you. After the attack: Swap this card with a different gold card in your deck. (This card starts the game in your deck)."
- **Bushido:** "Swap your gold card with a different gold card in your deck; if you swap a resolved or discarded gold card this way, the new card is facedown."
- **Set an Example / Lead from the Front:** "Target a unit adjacent to you. After the attack: A friendly hero in radius may swap their unresolved card with a card in their hand."
- **Hold my Saké:** the same, ending "with a card in their hand, or in their discard."
- **Spinning Blade:** "Target a unit adjacent to you. After the attack: **This turn:** Empty spaces adjacent to you count as obstacles for the enemy units." **Blade Helix:** the same with "Empty spaces in radius".
- **Proven Warrior / Chosen Champion:** "Choose a card in the discard of a friendly hero in radius. An enemy hero in radius discards a card of the same color, if able." **The Right Hand:** the same with "Up to two enemy heroes in radius discard a card of the same color, if able."
- **Calculated Risk:** "A friendly hero in radius may discard an attack card. If that hero has a card in the discard, that hero may move up to 2 spaces." **Tactical Gambit** adds ", ignoring obstacles".
- **Come to Aid / Bring the Relief:** "A friendly hero in range may discard a card. If that hero has a card in the discard you may move up to 3 [4] spaces." **Commit Reserves** adds ", ignoring obstacles" to the 4-space move.
- **Pledge of Allegiance / Loyal Retainer:** "A friendly hero in range may discard a card. If that hero has a card in the discard, both you and that hero gain 1 coin [2 coins] and you may retrieve a discarded card."
- **Ready for War:** "Return your silver card to your deck and take two gold cards from your deck into your hand. (You now have a total hand size of 6 cards.)"
- **Shameful Display (boxed Handicap):** Basic Defense, "You are defeated."

## Readings the rulebook settles

- **A swap exchanges every state** (p.10 Swap a Card: "When two cards are swapped, all their card states are swapped"). The gold card that comes out of the deck takes the card's location, played and resolution states, and the card it replaces takes the deck's. After Sting, Strike or Float swaps during its own action, the gold card from the deck is the played card in that turn slot and is placed resolved and faceup when the action completes (p.7); the card that started the action is in the deck. Hold my Saké's discard swap follows p.10's own example: the card from the discard becomes played and unresolved, and the old unresolved card takes the discard's states, so both cards land in shapes the build already knows.
- **"Swap this card with a different gold card in your deck"** is mandatory (no "may") and Takahide chooses which deck gold (p.10 Making Choices). With no gold card in the deck, as after Ready for War, it does nothing.
- **Bushido's facedown card** is p.10's Facedown state: a resolved or discarded facedown card "loses its type, color and actions, until it changes state to in hand or becomes faceup", is hidden information (p.4), and turning facedown cancels its Active effect. A gold card in hand swaps with an ordinary faceup hand card. Exactly one gold card is outside the deck whenever Bushido can be played, so "your gold card" names one card. End-of-round retrieval returns a facedown card to the hand, and with it its face.
- **Swapping a friendly hero's unresolved card changes when that hero acts.** p.7 names the next actor by "the highest Initiative on their unresolved card", asked after each resolution, so the swapped-in card's Initiative is the one the order reads. This is the shape Hanu's Hurry up! already proved (8.71a). The swapped-in card was revealed with the others, so it is faceup.
- **Who chooses** (p.10 Making Choices): Takahide chooses the friendly hero, the discard pile card (Proven Warrior's family) and the deck gold; the friendly hero decides whether to swap, which hand or discard card, whether to discard and what, and whether to move ("that hero may move"), as Rowenna's RO-Q2 and her friendly retrieval read the same shape.
- **"A friendly hero"** never includes Takahide (p.10 Picking Targets: an action affects you only when it says so; Brogan's War Drummer, Wuk's Tree of Plenty). Proven Warrior's family therefore chooses from a teammate's discard pile, never his own. Discarded cards are open information (p.10), so the choice reveals nothing. A facedown discarded card has no colour (p.10) and is not a choice.
- **Same-colour discards** reuse Tali's Ice Blast shape: a colour value read from a card, then the colour-restricted discard, "if able". The Right Hand's "Up to two enemy heroes" follows **W3**: every such hero present is hit, up to two, the player choosing which two when more stand there.
- **"An attack card"** is a card whose primary action is an Attack (p.9), as Brogan's Shield Bash reads it. A Defense/Skill or Skill card is not one.
- **"If that hero has a card in the discard"** is a state check read after the optional discard, not "if they do": a teammate who declines but already has a discarded card satisfies it. Cards are the definition; the faces print "if they do" elsewhere and do not here.
- **Moves.** "That hero may move up to 2 spaces" is a card-text move of the teammate, their decision (RO-Q2), not a Movement action (p.11); "you may move up to N spaces" is Takahide's own card-text move; "ignoring obstacles" is p.18's. Under **W5** a move of 0 is a move.
- **Sting like a Bee's "at maximum range"** is Silverarrow's reading (RULING 1): exactly the printed Range plus Range items.
- **Coins and retrieval:** "both you and that hero gain N coins" is two grants; "you may retrieve a discarded card" is Takahide's own (p.10: you never retrieve another hero's cards).
- **Blade Helix's Radius icon is 1**, so "in radius" is 1 plus Radius items (R1); Spinning Blade's printed Radius 4 is read by nothing in its text.
- **Ready for War timing:** p.16 unlocks the Ultimate at a level-up, which p.8 performs after Retrieve Cards, so at that moment the silver card and one gold card are in the hand and the other two golds are in the deck.
- **Shameful Display** is a p.20 Handicap and returns to the box at setup (p.6), as every hero's Handicap does in this build.

## Capability mapping

Existing contracts to reuse (each block re-probes before claiming reuse): the turn-slot identity swap `RoundResolutionAuthority.TrySwapResolvedWithHand` and `CardLifecycleAuthority.TrySwapResolvedWithHand` / `TrySwapItemWithRestingCard` (Snorri's Ancestral Boon and Grace, 8.93); the card taken out from under its own action (`IsResolvingCardInTurnArea`, `TryRetrieveResolvingCard`, `WasRetrievedDuringItsOwnAction`, Brynn 8.69b); the Initiative the order reads from an unresolved card (`TrySetPrintedInitiativeOverride`, Hanu 8.71a); `CardInstance.CreateInDeck` for a card p.6 leaves in the deck; the projection's fail-closed rule for a facedown card (9.30b); `PrintedAttackTargetPolicy`'s maximum-range band (Silverarrow 8.35z30); `DefendedCardColorV1` + `ColorRestrictedDiscard` (Tali 8.37); W3's `IfAble` per-hero discard; `gain-resource@1` with a recipient (Rowenna 8.57a, Brogan's War Drummer); `retrieve-card@1`; card-text moves with a chooser (RO-Q2) and `moveIgnoresObstacles`; Wasp's `ConditionalObstacleOverlay` as the precedent for spaces an Active effect makes obstacles; `UltimateUnlockQuery` and `TryUnlockUltimateFromDeck`.

New contracts, each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| Basic cards that start in the deck; the swap of a resolving or resting card with a deck card, both cards' full states and the turn slot's identity in one operation | three golds, Bushido | 9.32a (TK-Q1, TK-Q2) |
| p.10's facedown resolved and discarded card: no type, colour or actions; hidden from other seats; Active effect cancelled; retrieved to a fresh hand | Bushido | 9.32a |
| Swap of a friendly hero's unresolved card with a hand card, or a hand or discard card; the turn order re-read; the decision on that hero's seat | Set an Example, Lead from the Front, Hold my Saké | 9.32b |
| Spaces that are obstacles for the enemy units only, this turn, around Takahide's live space; every obstacle reader names the moving or placed piece | Spinning Blade, Blade Helix | 9.32c (TK-Q3) |
| A card chosen from a teammate's discard pile as a colour value | Proven Warrior, Chosen Champion, The Right Hand | 9.32e |
| A discard filtered by card type ("an attack card") on a teammate's hand | Calculated Risk, Tactical Gambit | 9.32e |
| A teammate's optional discard followed by a discard-pile state check gating a move, coins to two heroes and a retrieval | the five Greens, Calculated Risk, Tactical Gambit | 9.32e (TK-Q4) |
| An Ultimate whose text is a one-time card movement performed at its unlock; a six-card hand | Ready for War | 9.32f (TK-Q5) |

Expected compatibility moves: CardStateFacts already carries location, played, resolution and orientation, so a facedown resolved or discarded card and a swapped-in deck card are expected to be schema-neutral, with only the restore validators widened. Whether the facedown card's projection row or the obstacle cue moves the wire protocol is measured in its block, not assumed.

## Open questions

Each blocks only the block named. Recommendations are the agent's; the user decides.

- **TK-Q1 - when Float like a Butterfly's swap happens (blocks 9.32a).** Printed: Basic Movement 5, "Swap this card with a different gold card in your deck."
  - (A) After the Movement, including a move of 0 (W5), and only when the Movement is performed: not when Fast Travel replaces it (p.11, the RZ-Q9 reading) and never when the card is discarded to defend.
  - (B) Before the Movement.
  - *Recommended:* (A). The swap ends the card's action, as Sting's and Strike's "After the attack" swaps do, and the Movement value stays the one printed on the card that started it.
- **TK-Q2 - which card Takahide has "played" after a swap (blocks 9.32a).** p.10 swaps every state, so after Sting like a Bee swaps with Float like a Butterfly, the played card in the turn slot is Float, a Movement card, and Sting is an unplayed card in the deck.
  - (A) The swapped-in card is the played and resolved card for every later reader: Brogan's Shield Bash ("who has played an attack card this turn") sees no attack card; a reader of his resolved card reads the swapped-in face.
  - (B) Both cards count as played this turn.
  - *Recommended:* (A), p.10's "all their card states are swapped".
- **TK-Q3 - what Spinning Blade's and Blade Helix's obstacles do (blocks 9.32c).** Printed: "This turn: Empty spaces adjacent to you [in radius] count as obstacles for the enemy units."
  - (A) An enemy unit cannot move through or end its movement on those spaces (unless it ignores obstacles, and then it still cannot end there), and cannot be placed, pushed or respawned into them (a p.19 swap exchanges two occupied spaces and never reaches an empty one). The spaces are read when asked, around Takahide's current space (p.17: an Active effect's radius moves with its hero), and "empty" is read then too. Friendly units, tokens, and the Range, Radius and straight lines of an enemy's action are unchanged. The effect ends at the end of the turn.
  - (B) As (A), but the set of spaces is fixed when the card resolves.
  - (C) As (A), and the spaces also block every line or path an enemy hero's action traces.
  - *Recommended:* (A). p.18's obstacle is a rule about a piece entering a space, and "for the enemy units" names whose pieces.
- **TK-Q4 - a friendly-hero face with no teammate to choose (blocks 9.32e).** Printed: "A friendly hero in range may discard a card. ..." and "Choose a card in the discard of a friendly hero in radius. ..."
  - (A) The choice of the friendly hero (or of the discarded card) is mandatory, so with none available the face is greyed (Rule 14); Hold and the secondary actions stay available. Offered heroes are those the step can act on (6.8l).
  - (B) Offered as a do-nothing play.
  - *Recommended:* (A), as RZ-Q7 was settled; RULING 53 covers only a printed "If". The Red faces are unaffected: their swap is after-attack text, and the Attack is what is offered.
- **TK-Q5 - Ready for War (blocks 9.32f).** Printed: "Return your silver card to your deck and take two gold cards from your deck into your hand. (You now have a total hand size of 6 cards.)"
  - (A) Performed once, at the level-up that unlocks it, whether or not Takahide is on the board; nothing returns the cards afterwards, so from then on he plays from six cards (three golds and three colours), Bushido stays in the deck, and the golds' "swap with a different gold card in your deck" finds none and does nothing.
  - (B) An ongoing effect re-applied each round while he is on the board (p.16's "as long as your hero is on the board").
  - *Recommended:* (A). The text is an instruction, not a condition, and p.8's retrieval leaves it nothing to re-apply.
