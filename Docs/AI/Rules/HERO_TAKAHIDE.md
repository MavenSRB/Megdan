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

Expected compatibility moves (9.32's survey): CardStateFacts already carries location, played, resolution and orientation, so the card states were expected to be schema-neutral, and the wire was to be measured per block. 9.32a measured otherwise for the turn slot; see below.

**Built by 9.32a** (engine only, proven on `SyntheticGoldSwapHand`; registered with the dealing at 9.32h):

- `CardLifecycleAuthority.TrySwapWithDeckCard` is p.10's swap with the deck: the deck card takes every state of the outgoing card (a hand card, the card resolving its own action, or a faceup resolved or discarded card, `IsDeckSwapSubject`), the outgoing card takes `InitialInDeck`, and Bushido's flag turns the incoming card facedown only where the outgoing one was resolved or discarded. `IsFacedownRestingCard` names p.10's facedown resolved or discarded card for every reader.
- `RoundResolutionAuthority.TrySwapWithDeckCard` (`IDeckCardSwap` on `EffectRosterState`) does it as one operation (lesson 28): the cards, the turn slot of this round that names a played outgoing card, and, for the resolving card, the Initiative's selected card (`TryReplaceSelectedCard`), so the action completes and resolves the swapped-in card (TK-Q2). The Movement completion after text (Float, TK-Q1) follows the selected card when its held card went to the deck.
- `swap-this-card-with-deck-card@1` and `swap-owned-color-card-with-deck-card@1` (`SyntheticDeckSwapCapabilities`): the actor chooses among his deck cards of the named colour; the choice is projected to his seat alone; the outgoing card's Active effects end (p.17).
- Lesson 29's readers: `PlayedAttackCardThisTurnQuery`, the borrowed primary (`BorrowedPrimaryAction.InPool`) and borrowed card text refuse a facedown resting card; readers that only count cards in a location (a discard pile's presence) count it, as p.10 still makes it a card there.
- **The slot records the facedown fact** (`TurnCardAssignment.IsFacedown`). A card instance id names its definition, the turn slots reach every seat and the wire, and the completed-round history keeps them after the card has returned to the hand, where its own state no longer says it was facedown. `FacedownSlotMask` therefore shows every seat but the owner's a withheld assignment (`TurnCardAssignment.Withheld`: a card is there, not which). Snapshot schema v53 -> v54 (each slot assignment ends with the flag); wire `/33` -> `/34` (each slot row, current and completed, ends with it).

**Built by 9.32b** (engine only, proven on `SyntheticUnresolvedSwapHand`; registered with the dealing at 9.32h):

- `CardLifecycleAuthority.TrySwapUnresolvedWithRestingCard`: a waiting unresolved card and a fresh hand card or a faceup discarded card (`IsUnresolvedSwapPartner`) exchange states exactly, so Hold my Saké's discard partner becomes played and unresolved and the old card takes the discard's shape (p.10's own example). An Initiative rewrite on the old card ends with its state.
- `RoundResolutionAuthority.TrySwapUnresolvedCard` (`IUnresolvedCardSwap`): the owner's card in this turn's slot, never the acting hero's, exchanged as one operation with the slot and the turn's revealed record (`TurnCardCommitmentAuthority.TryReplaceRevealedCard`). The order needs nothing: the Initiative authority reads unresolved cards live, so the swapped-in Initiative decides when the teammate acts (p.7). The revealed record now also follows 9.32a's resolving swap, which had left it on the card that went to the deck (regression in `TakahideGoldSwapTests`).
- `swap-unresolved-card@1` (`SyntheticUnresolvedSwapCapabilities`, `fromDiscard`): asked of the owner's seat.

**Built by 9.32c** (engine only, proven on `SyntheticEnemyObstacleHand`; registered with the dealing at 9.32h):

- `ObstacleOverlayArm.EmptySpacesInsideForUnits` and `ConditionalObstacleOverlay.DeclareForUnits(relation)` (`UnitRelation`): an Active-effect overlay whose condition is the PIECE entering a space, not the acting hero (Wasp's arms). Its disk is the effect's `DeclaredRadius` around the controller's current space: `WrittenInText(1)` for "adjacent to you", the printed icon with Radius items for "in radius" (R1). The derivation (`RuleModificationQuery.BarredUnits`) binds the units it bars: every hero of that side, and every minion of that side on the board.
- `RuleModificationSet.AnyOverlayMakesAnObstacleFor(space, piece)` answers only for an empty space; `AnyOverlayMakesAnObstacleOf(space)`, the question with no piece, never sees a unit overlay. `ObstacleQuery.BlocksTransit` (already naming the mover), and new piece-naming `BlocksEndOfMovement`, `BlocksEndAfterVacating` and `BlocksPlacement` overloads, carry it to every reader that names the moving or placed piece (lesson 30's census of about 25 call sites): the Movement walk and its reachability, pushes, straight-line crossings, coupled moves, every Place (the board placement, the Place capability, Razzle's body spawn) and the hero respawn (`HeroRespawnRules.CreateChoice` with the turn's modifications). Questions naming no piece (presence, adjacency to an obstacle, lines, ranges) are unchanged (TK-Q3). Bots read movement and placement through the same queries.
- Readings recorded here: p.18's displaced-minion correction keeps asking with no modifications (milestone 8.7: it is bookkeeping, not an action), so it is not barred; a card-text minion respawn into a barred space in the same turn (Necromastery, Create Undead, cast by an enemy of Takahide) is not refused, because a minion off the board is not in the bound set. 9.32d checks whether any dealt face can reach that case. Composed as Takahide's friend selection naming the swap as its consumer (only a teammate with an unresolved card and a partner is offered, 6.8l), then the teammate's `May` (`BoundHero` chooser), then the swap.

**Built by 9.32d** (`TakahideCardContent`, `TakahideRedFaces`; registered with the dealing at 9.32h): the three gold cards, Bushido and the five Red faces composed on 9.32a-9.32c, every value read against the fronts in `TakahideBasicAndRedFacesTests` (Initiative, Defense, Movement, Attack, Range, Radius, item, tier, the upgrade lines). Sting like a Bee targets at exactly its printed Range with Range items (`AtExactlyThePrintedRange`, RULING 1). Each gold's swap is `IfAble` (no "may"; nothing to do with no deck gold); Bushido's is its mandatory Skill. The three friend-swap faces pick the teammate through `radius-hero-target@1` (allied, never himself, the swap as consumer) at the printed Radius with items (R1), then the teammate's `May` and card. Spinning Blade's disk is `WrittenInText(1)` ("adjacent to you"; its printed Radius 4 is read by nothing); Blade Helix's is its printed Radius 1 with items. The new-hero checklist's reading pass (step 1) found every face inside W1-W9 and R1; `PrintedWordingConformanceTests` reads dealt heroes, so it holds these faces from 9.32h.

**Built by 9.32e** (`TakahideBlueGreenFaces`; registered with the dealing at 9.32h): the five Blue and five Green faces, every value read against the fronts in `TakahideBlueAndGreenFacesTests`.

- `choose-discarded-card@1` (`SyntheticDiscardedCardChoiceCapabilities`): Takahide chooses a faceup card at rest in a named hero's discard pile and its colour is published. A facedown card is no choice. Nothing to choose is impossible, so the teammate selection that names it as consumer offers only teammates with a choosable card (6.8l), and with none the Blue face is greyed (TK-Q4).
- `attackCardsOnly` on `synthetic-discard-chosen-card@1/@2`, read through `CardCharacteristicCriteria.PrintedPrimaryKind` (a card with an Attack printed primary, p.9; unbound narrows nothing).
- Proven Warrior and Chosen Champion: the friendly hero, the card, then "An enemy hero in radius discards ..., if able" composed as Gydion's Cloud Kill prints and composes the same sentence (Takahide picks the enemy hero; the discard is "if able"). The Right Hand's "Up to two enemy heroes" is W3's nested `IfAble` passes, the second excluding the first hero, so zero enemies in radius still lets the face resolve.
- Calculated Risk, Tactical Gambit and the Greens: every friendly hero in reach is offered (the discard is theirs to decline, and the check reads the pile whatever they decided). The discard is a `May` on the teammate's seat; `owned-card-present@1` reads their pile after it, so a teammate who declines with a card already there satisfies the check, a facedown card counts (lesson 29), and an empty pile does nothing (RULING 53). The moves are card-text `UpTo` moves (`synthetic-direct-move@3`), the teammate's with their own chooser (RO-Q2) and Takahide's own, `movesThroughObstacles` for "ignoring obstacles" (never ending on one, p.18). A teammate walled in is still offered a move of 0 (W5), the shape every "up to" move in the build has.
- Pledge of Allegiance and Loyal Retainer: two `gain-resource@1` grants (Takahide and the teammate) and Takahide's optional `retrieve-card@1` of his own discarded card.

**Built by 9.32f** (`TakahideUltimate`, `UltimateUnlockCardMovement`; registered with the dealing at 9.32h):

- Ready for War is `UltimateRuleDefinition.UnlockCardMovementAbility`: its whole content is a typed movement (silver from hand to deck; up to two golds from deck to hand), exclusive with every permanent-ability shape. `HeroLevelUpStepRules` performs it inside the Ultimate item that p.16's unlock resolves, so it happens once, whether or not he is on the board, beside any other hero's open Upgrade step (8.35u), and never again (TK-Q5). The reminder text composes to nothing.
- Exactly one gold is outside his deck whenever the unlock can happen, so the two taken are the two there; the stable-id order would only decide a case his cards cannot reach. After the unlock Bushido stays in the deck and a gold's swap finds no deck gold and does nothing (9.32d's `IfAble`).
- `UltimateRuleDefinition` gained a public property, so all twenty-nine identity pins moved with their revisions unchanged; with the property excluded from the walk every pin measured byte-identical first (`VerifiedContentIdentityTests`' 9.32f note).
- Lesson 31's census: no five-card limit in Core, Application (the bots score offered commands), Network (hand rows are lists) or setup beyond the five-card starting hand. Presentation's `OrdinaryHandCardCount` (the HUD hand stack) is 9.32g's; the level-up window's "show all 5 cards" never meets six, because the unlock ends leveling.

**Built by 9.32g** (presentation to the approved mockups below; his portrait, card art, card back and decision labels join at 9.32h):

- B: a facedown gold is a gold on every seat. A withheld slot row carries `TurnCardAssignment.KnownColor` (read off the definition the host holds, only on a withheld row) and the hand summary carries `SeatHandSummary.FacedownDiscardColors`; wire /35 refuses a colour on a named row and Purple. Every seat draws the same hatched gold mini (`FacedownHatch`) in the slot and the pile with no caption; his own hover is the face, dimmed and hatched, every other seat's the card back (`Private/CardBacks/<heroId>` where the private checkout has it, the flat card-back colour otherwise).
- A: six cards fit the five-card footprint (`DirectorHandsUnityView.PeekSlotFootprintFor`, `UnavailableSlotFootprintFor`); five or fewer step as before, so the HUD's reservation (`OrdinaryHandCardCount`) stays five.
- C: Bushido's candidates are captioned FACEDOWN exactly where the new card arrives facedown (`SwapChoicePresentation.ArrivesFacedown`: the node's colour and `facedownWhenResting` literals and the chooser's card of that colour resolved or discarded).
- D: the three friend-swap faces lost the separate confirm: the swap node itself is `May`, so the teammate answers one window on their own seat - their unresolved card captioned with its Initiative, their cards captioned by Initiative (and "FROM YOUR DISCARD" for Hold my Saké), KEEP MY CARD as the decline. Content revision `takahide-9-32g-rev-1`.
- E: a disk whose overlay bars the enemy's units (`ActiveEffectAuraViewModel.BarsEnemyUnits`, `ActiveEffectVisibilityModel.BarredSpaces`) marks its EMPTY playable spaces with a red hatched hex and a barred sign (`HexTileView.SetEnemyObstacleCue`, a generated `EnemyObstacleCueTexture`); occupied spaces stay unmarked; every seat sees it.
- F: the swap handlers report a `CardSwapFact` (a kept card through `IEffectDeclineReport` when the teammate declines) and Ready for War's unlock its moves (`HeroLevelUpOutcome.CardMoves`), each a `CardMoveFact`: the card's definition, its zones, its facedown flags and the colour the text fixes. They reach the log as `CardSwappedByCardText`, `CardKeptByCardText` and `UltimateMovedCards` (a row only where an unlock moved cards). `CardMoveMask` in the seat projection unnames, on every seat but the owner's, each move no seat saw (a hand or deck card at both ends, a card arriving facedown), and no move card rides `SubjectCardInstanceId`, so the log never makes a hand card public. An attack whose card starts the barring effect adds "<HERO>: EMPTY SPACES ADJACENT (or IN RADIUS) ARE OBSTACLES FOR THE ENEMY THIS TURN".
- Privacy found in review: the log's memory of a retrieved card (p.8) named a hand card for as long as any row named it, so a gold the table saw go into the deck and come back unseen (Bushido's gold in hand for one of two deck golds) would have been named to every seat. `RetrievedCardMemory` ends that memory at a later row moving the card into the deck. Ready for War's golds are withheld the same way, although a seat that tracked every gold could know them: where the table's knowledge depends on counting, the projection withholds.
- Two things differ from the mockups; **the user accepted both on 2026-10-02 (user ruling)**: another seat reads Ready for War's row as "THE SILVER CARD TO THE DECK, TWO GOLD CARDS TO HAND" (p.8 retrieves every card before leveling, so Bushido is in his hand, and F's rule names no hand card to another seat); and while the gold choice or the teammate's swap is open, every other seat reads the existing public wait, "TAKAHIDE — RESOLVING A CARD EFFECT", not C's and D's notes ("CHOOSING A GOLD CARD", "DECIDING ON A SWAP"), because the decision owner every seat holds names only the decision's family.

**Built by 9.32h** (the dealing; `TakahideCardContent.Set.cs`, `TakahideProgressedHands`):

- Roster row `hero-takahide`, "Takahide the Warlord", four stars, Durable, Sniper, Tactician, Damager and Farming. Twenty fronts dealt; Float like a Butterfly, Bushido, Set an Example, Proven Warrior and Come to Aid in hand; the other fifteen, Sting like a Bee and Strike like a Tiger among them, in the deck. Shameful Display is boxed (p.20); its "!" is p.14's Block icon and no Block condition this build declares says "You are defeated", so its definition carries the transcription and no composed action. Staged boards: Wasp against him with his starting hand, and on Tier III as Ready for War leaves him (all three golds and a Tier III per colour, the Ultimate faceup, Bushido in the deck).
- Registration: the deck swap, the unresolved swap and the discard choice join `SyntheticEffectCapabilities`; Spinning Blade's and Blade Helix's creations join `MatchContentCatalogs` (three new handler types, 143 -> 146; six capabilities). His faces raise no p.10 confirm-or-decline, so he declares no decision labels. Mockup C's eyebrow and question come from the swap node's colour literal (`SwapChoicePresentation.DeckSwapEyebrow`, `DeckSwapQuestion`).
- Art: twenty catalog rows (`takahide-9-32h-rev-1`), `Prepare-CardArtwork` (`Takahide`), `Prepare-HeroSelectionArtwork` (`randomizers-tt26__7723697`, SHA-256 identical to the selection front), `Prepare-HeroTokens -Hero takahide` (standee `Taka` 8596254) and the new `Prepare-CardBacks` (`Private/CardBacks/hero-takahide`, the importer now keeps that folder's 372x520).
- Owed items closed: a teammate's network seat answers Set an Example's swap (9.32b); the wire completeness walk reaches a facedown slot, its withheld row and known colour, a facedown discard's colour and a log row's card moves (9.32a, 9.32g), with a "facedown gold fixture" walk and the overlay. Thirty heroes rotate the line-ups away from a Clear with an adjacent token, so 9.30i's removed Clear declarations return.

## Settled questions

On 2026-10-01 the user settled all five one by one, each shown with its card faces, and chose (A), the recommendation, every time. Each names the block that builds it.

- **TK-Q1 - when Float like a Butterfly's swap happens (blocks 9.32a).** Printed: Basic Movement 5, "Swap this card with a different gold card in your deck."
  - (A) After the Movement, including a move of 0 (W5), and only when the Movement is performed: not when Fast Travel replaces it (p.11, the RZ-Q9 reading) and never when the card is discarded to defend.
  - (B) Before the Movement.
  - *Recommended:* (A). The swap ends the card's action, as Sting's and Strike's "After the attack" swaps do, and the Movement value stays the one printed on the card that started it.
  - **User, 2026-10-01: (A).**
- **TK-Q2 - which card Takahide has "played" after a swap (blocks 9.32a).** p.10 swaps every state, so after Sting like a Bee swaps with Float like a Butterfly, the played card in the turn slot is Float, a Movement card, and Sting is an unplayed card in the deck.
  - (A) The swapped-in card is the played and resolved card for every later reader: Brogan's Shield Bash ("who has played an attack card this turn") sees no attack card; a reader of his resolved card reads the swapped-in face.
  - (B) Both cards count as played this turn.
  - *Recommended:* (A), p.10's "all their card states are swapped".
  - **User, 2026-10-01: (A).**
- **TK-Q3 - what Spinning Blade's and Blade Helix's obstacles do (blocks 9.32c).** Printed: "This turn: Empty spaces adjacent to you [in radius] count as obstacles for the enemy units."
  - (A) An enemy unit cannot move through or end its movement on those spaces (unless it ignores obstacles, and then it still cannot end there), and cannot be placed, pushed or respawned into them (a p.19 swap exchanges two occupied spaces and never reaches an empty one). The spaces are read when asked, around Takahide's current space (p.17: an Active effect's radius moves with its hero), and "empty" is read then too. Friendly units, tokens, and the Range, Radius and straight lines of an enemy's action are unchanged. The effect ends at the end of the turn.
  - (B) As (A), but the set of spaces is fixed when the card resolves.
  - (C) As (A), and the spaces also block every line or path an enemy hero's action traces.
  - *Recommended:* (A). p.18's obstacle is a rule about a piece entering a space, and "for the enemy units" names whose pieces.
  - **User, 2026-10-01: (A).**
- **TK-Q4 - a friendly-hero face with no teammate to choose (blocks 9.32e).** Printed: "A friendly hero in range may discard a card. ..." and "Choose a card in the discard of a friendly hero in radius. ..."
  - (A) The choice of the friendly hero (or of the discarded card) is mandatory, so with none available the face is greyed (Rule 14); Hold and the secondary actions stay available. Offered heroes are those the step can act on (6.8l).
  - (B) Offered as a do-nothing play.
  - *Recommended:* (A), as RZ-Q7 was settled; RULING 53 covers only a printed "If". The Red faces are unaffected: their swap is after-attack text, and the Attack is what is offered.
  - **User, 2026-10-01: (A).**
- **TK-Q5 - Ready for War (blocks 9.32f).** Printed: "Return your silver card to your deck and take two gold cards from your deck into your hand. (You now have a total hand size of 6 cards.)"
  - (A) Performed once, at the level-up that unlocks it, whether or not Takahide is on the board; nothing returns the cards afterwards, so from then on he plays from six cards (three golds and three colours), Bushido stays in the deck, and the golds' "swap with a different gold card in your deck" finds none and does nothing.
  - (B) An ongoing effect re-applied each round while he is on the board (p.16's "as long as your hero is on the board").
  - *Recommended:* (A). The text is an instruction, not a condition, and p.8's retrieval leaves it nothing to re-apply.
  - **User, 2026-10-01: (A).**

## Settled presentation (9.32g)

On 2026-10-02 the user approved all six 9.32g mockups, composited over real 1920x1080 captures (`tmp/9-32g-mockups/APPROVE-A..F`, generator `mockups.py`), with one amendment to B. 9.32g builds to them:

- **A - six-card hand:** six cards in today's five-card footprint; each covered card shows a narrower column (63 px at 1920x1080 instead of 79) that still shows Initiative, Defense and Movement.
- **B - facedown gold:** in a turn slot or the discard pile, EVERY seat's mini shows the same hatched gold, with no caption: a card Bushido turned facedown can only be a gold, so its colour is no secret (user, 2026-10-02, amending the first mockup's card back for other seats). Only the mouse-over differs: his seat sees the face, dimmed and hatched; every other seat sees the Takahide card back. The hatch on a mini is a bolder diagonal hatch with fewer, thicker strokes; the full-size hover keeps the fine hatch (user, 2026-10-02: "a bolder diagonal hatch with fewer, thicker strokes. approved.", corrective 9.36). Bushido itself resolves faceup and draws as any faceup card. *Owed to it:* another seat's projection withholds the facedown card's identity (9.32a) yet must carry the fact that it is a gold - the colour the swap's own text fixes, never read from the hidden definition for that seat - so the withheld slot and pile rows gain that one public fact.
- **C - deck-gold choice:** the spell-choice window's shape: the swapping card on the left, the deck golds as faces to pick; for Bushido each candidate carries a FACEDOWN tag when the new card arrives facedown. Other seats read "TAKAHIDE - CHOOSING A GOLD CARD" with no faces.
- **D - teammate swap:** one window on the teammate's own seat: their unresolved card, their hand (and Hold my Saké's discard row) to pick from, and KEEP MY CARD to decline. Other seats read "<HERO> - DECIDING ON A SWAP".
- **E - enemy-only obstacles:** a red hatched hex with a barred sign on each empty space of the effect's disk around him, the same for every seat, for the rest of the turn; occupied spaces unmarked.
- **F - log rows:** a card is named only where every seat has seen it; a hand, deck or facedown card is never named to another seat; no pronouns.
