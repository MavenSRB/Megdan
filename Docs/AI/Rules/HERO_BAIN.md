# Guards Digital - Bain Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Bain the Bounty Hunter is the fifteenth hero this project has inventoried, and the third taken from
outside the twelve-hero package arc milestone 8.37 closed. This shard owns his content inventory,
source-verification status, capability contract map, and any deviation or open question scoped to Bain
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. **The
pronoun convention, stated because his case is neither of the two this project already has**: the
rulebook names him but assigns him no pronoun - its one mention is a first-person flavour quote (BA-01)
- so this shard does not follow the source the way Tigerclaw's does, and it does not fall back to the
convention Misa's, Whisper's, Swift's and Cutter's shards use for the dashboards' unnamed heroes,
because his dashboard and both standee faces show a man. It uses "he".

## Bain content ledger

His printed set is inventoried at 8.40, composed at 8.40i and dealt at 8.40j; the permanent player-path
gate is 8.40k, in the
[fifteenth-hero shard](../Roadmap/PHASE_8_FIFTEENTH_HERO.md). **Nothing here authorizes
implementation.** A row records what the face says and which contract carries it, and that is all.

## Source authority

**BA-01. The second-printing rulebook in this repository DOES name him, and that is the first time
this has happened for a hero outside the package arc - but it names him in flavour text and nowhere
else, so the extraction is still sole authority for these faces.** The check was run rather than
assumed: `pdftotext` over the local second-printing PDF returns **exactly one hit** for `Bain`, **one**
for `Bounty Hunter` - the same line - and **zero** for all nineteen card names. The line is printed
page 2's opening fiction, attributed "- Bain the Bounty Hunter", and its whole content is a hero
speaking in the first person about mercenary motive. It carries no rule, no value, no card, and no
pronoun.

This is a different conclusion from Swift's, Ursafar's, Tali's and Cutter's shards, and the difference
is worth stating precisely: theirs is an absence, his is a presence that says nothing. The consequence
is nevertheless identical - **every string below is what our scan prints, and nothing is reconciled
against a printing we do not hold.**

[Source provenance](../RULES_LEDGER.md) still governs the rest. The extracted cards were verified
against physical second-printing cards on 2026-09-18 and match the print on every rules-bearing word
checked, so the cards and the rulebook are the SAME edition: a disagreement between them is a rulebook
gap or a typesetting difference to be weighed on its merits, not edition skew, and a designer answer
that contradicts the print reflects the third printing in preparation rather than this edition.

**BA-02. His marker is a rulebook keyword rather than a new idea, and p.18, p.8 and p.13 already say
its whole lifetime.** p.18's Markers keyword says markers are reminders "usually given to other heroes
as part of the card's effect" and "usually not placed on the board"; p.8's end-of-round step returns
every marker to the supply; and p.13's Defeat list returns a defeated hero's. Those three sentences are
exactly `MarkerReturnCondition.GeneralCleanup`, which Tigerclaw's Poison marker already declares. So
nothing about WHEN the bounty marker comes back is new; what is new is every rule his cards attach to
holding one.

**BA-03. His set prints TWO different line-clearance sentences on purpose, and the difference is the
most consequential source fact in the inventory.** Get over here! prints "with **no obstacles** between
you and the target"; Light Crossbow, Heavy Crossbow and Arbalest print "with **no units, or terrain**,
between you" - **three of his five Reds, not five**, corrected at 8.40d: Hand Crossbow and
Hunter-Seeker print a marker modal and no line clause, as the transcription above has them. p.18's
Obstacle keyword makes the first list strictly wider than the second - "Units, terrain (including
printed on the board), tokens, as well as any objects represented by miniatures, are obstacles" - so a
token blocks the pull and does not block a crossbow. One deck printing both halves is as strong a
signal as this corpus gives that the narrower wording is deliberate, and the mapping below treats them
as two contracts.

## Set shape

Nineteen faces, eighteen of them dealt. The shape matches Swift's and Cutter's exactly: one Gold `D`
basic, one Silver `S` basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and
Green, one Purple Tier IV Ultimate, and one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.5) | Dead or Alive, Get over here!, Light Crossbow, A Game of Chance, Close Call |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and A Complicated Profession |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.5), used only for an odd player count (p.20) | Side Quest |

**BA-04. The nineteen faces do NOT share one deck back, and he is one of the heroes the corpus records
with two distinct back hashes.** Measured rather than assumed: **fifteen** backs hash alike
(`03F43CD7...`) and **four** hash alike to a different value (`AFB95228...`). The four are **Dead or
Alive, Get over here!, Vantage Point and High Ground**. Swift's shard states the opposite fact about her
own set and Cutter's records a fourteen/five split, so the sentence is per-hero and must be measured for
each - which is what the corpus-reading note already warned. The split is corroborated independently by
`cards.csv`, whose source-atlas coordinates put exactly those four backs on a different sheet.

## Printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the face prints
no such icon, so that secondary action is unavailable from it.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `bain` | 7478189 | Dead or Alive | Gold `D` | basic | 11 | 2 | 1 | Basic Attack | Attack 4 | Radius 4 | - |
| `bain19` | 7348641 | Side Quest | Gold `D` | `H` | 10 | 1 | 1 | Basic Skill | - | Radius 4 | - |
| `bain2` | 7624924 | Get over here! | Silver `S` | basic | 13 | 2 | - | Basic Skill - Ranged | - | Range 4 | - |
| `bain3` | 7348633 | Light Crossbow | Red `R` | I | 8 | 5 | 4 | Attack - Ranged | Attack 5 | Range 2 | - |
| `bain4` | 7348636 | Heavy Crossbow | Red `R` | II | 8 | 5 | 4 | Attack - Ranged | Attack 5 | Range 3 | Defense |
| `bain5` | 7348629 | Hand Crossbow | Red `R` | II | 8 | 6 | 4 | Attack - Ranged | Attack 4 | Range 3 | Initiative |
| `bain6` | 7348637 | Arbalest | Red `R` | III | 9 | 6 | 4 | Attack - Ranged | Attack 6 | Range 4 | Defense |
| `bain7` | 7348635 | Hunter-Seeker | Red `R` | III | 9 | 7 | 4 | Attack - Ranged | Attack 5 | Range 3 | Movement |
| `bain8` | 7348639 | A Game of Chance | Blue `B` | I | 9 | 4 | 3 | Skill | - | Radius 2 | - |
| `bain9` | 7348626 | Dead Man's Hand | Blue `B` | II | 10 | 4 | 3 | Skill | - | Radius 3 | Defense |
| `bain10` | 7348631 | Drinking Buddies | Blue `B` | II | 10 | 4 | 3 | Skill | - | Radius 3 | Attack |
| `bain11` | 7348638 | We're Not Done Yet! | Blue `B` | III | 10 | 5 | 3 | Skill | - | Radius 3 | Range |
| `bain12` | 7348634 | Another One! | Blue `B` | III | 10 | 5 | 3 | Skill | - | Radius 3 | Attack |
| `bain13` | 7348642 | Close Call | Green `G` | I | 4 | - | 2 | Defense (Block) | - | - | - |
| `bain14` | 7348640 | Narrow Escape | Green `G` | II | 4 | - | 2 | Defense (Block) | - | - | Initiative |
| `bain15` | 7348643 | Vantage Point | Green `G` | II | 4 | 3 | - | Movement | Movement 2+ | - | Attack |
| `bain16` | 7348632 | Perfect Getaway | Green `G` | III | 3 | - | 2 | Defense (Block) | - | - | Initiative |
| `bain17` | 7348644 | High Ground | Green `G` | III | 3 | 4 | - | Movement | Movement 2+ | - | Radius |
| `bain18` | 7348630 | A Complicated Profession | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

**BA-05. SIX of his faces print exactly one secondary icon, and all six draw it in the SAME slot while
carrying three of one glyph and three of the other.** This is milestone 8.35z20's trap at the roster's
largest scale: Swift's Bounce and Cutter's Grappling Bolt had one instance each, and Bain has six.
Get over here!, Close Call, Narrow Escape, Vantage Point, Perfect Getaway and High Ground
each print one icon, and **every one of the six sits in the LOWER band - the boot's position on the
twelve faces that print both**. The glyphs are:

| Face | Glyph at the shared crop | Recorded value |
| --- | --- | --- |
| Get over here! | riveted shield | **Defense 2**, no secondary Movement |
| Close Call | heeled boot | **Movement 2**, no secondary Defense |
| Narrow Escape | heeled boot | **Movement 2**, no secondary Defense |
| Vantage Point | riveted shield | **Defense 3**, no secondary Movement |
| Perfect Getaway | heeled boot | **Movement 2**, no secondary Defense |
| High Ground | riveted shield | **Defense 4**, no secondary Movement |

Resolved by GLYPH SHAPE at one shared crop rectangle, in a single image beside Dead or Alive's
`Defense 2 / Movement 1` and Hunter-Seeker's `Defense 7 / Movement 4` shield-over-boot pairs, which fix
the two reference heights. **Three shields and three boots in the same slot is the cleanest available
proof that the slot carries no information at all**, and it is recorded here so no later reader
re-opens the question a positional reading would have got half right and half wrong.

**BA-06. Every item icon was identified against a face whose channel this project already declares in
code**, never by eye, on the standing rule that Initiative's hourglass and Movement's boot are the easy
pair to swap and that Range's arrow and Radius's circling arrows are the other. The six references used
were Silverarrow's Opportunity Shot (Defense), Long Shot (Initiative), Clear Shot (Radius), Warning
Shot (Attack), Treetop Sentinel (Range) and Lead Astray (Movement). His twelve item-bearing faces
resolve to **three Defense, three Initiative, three Attack, one Movement, one Range and one Radius**,
with none left ambiguous, and **all six p.16 channels appear in one hero's set** - worth naming because
a reader checking for a missing channel finds none.

**BA-07. Dead or Alive's lower-right glyph is a RADIUS, not a Range, and this was the one printed value
the pre-inventory read could not settle.** The face is a non-Ranged `BASIC ATTACK` whose printed target
clause is "Target a unit adjacent to you", so a reach icon reads oddly at first; its text then says "an
enemy hero **in radius**", which the icon has to match. Resolved at crop resolution against Get over
here!'s known Range 4 arrow on the same hero: the two glyphs are unmistakably different, and Dead or
Alive's is the circling-arrows Radius drawn identically to Side Quest's Radius 4 and Drinking Buddies'
Radius 3. **Radius 4.**

**BA-08. All five of his Green faces print no Range and no Radius icon at all.** Close Call, Narrow
Escape, Vantage Point, Perfect Getaway and High Ground print a bare `DEFENSE` or `MOVEMENT` band with
no reach glyph, because none of their printed clauses measures a distance - each asks only whether
*some* hero in play holds the marker. This is Cutter's CU-06 in a different colour and it is recorded
for the same reason: a reader expecting a reach on every face would look for a missing icon.

**BA-09. His two Movement faces print a plus after the value** - `Movement 2+` on both Vantage Point
and High Ground - which is p.14's notation for a value the card text can raise, the same notation
Tigerclaw's Backstab and Cutter's three lunges use on the Attack channel. The printed text is what says
where the increment comes from: `+1` and `+2` respectively, and only while a hero in play holds a bounty
marker.

**BA-10. His stat profile is read, not estimated, and two of the four bars carry brighter conditional
sections.** On p.21's stated 1-to-8 scale: **Attack 5, Defense 4 with two brighter conditional
sections, Initiative 4, and Movement 5 with one brighter conditional section.** Calibrated against
Brogan's recorded Attack 7, Defense 8, Initiative 1 and Movement 2-with-two-brighter, which fixes the
pixel pitch and both bar origins from known values at each end of the scale, and read by a
luminance-against-trough scan rather than a colour classifier because his violet frame bleeds into the
fill. p.21 states
what the brighter sections mean: "Brighter colored sections of the stat bars are conditional. They
depend on your level-up choices and triggering specific card texts." **He is a TWO-star hero on p.21's
complexity scale**, which makes him the simplest hero dealt since the package arc - Swift, Tali and
Cutter are all three-star - and his roles are **Tactician, Sniper, Healer, Farming and Durable**, five
where Cutter prints three, read from the profile card's five role icons and corroborated by name in
p.21's own role descriptions. p.21's Healer paragraph - "Healers help their allies by letting them
retrieve" - is Drinking Buddies in as many words. This is selection metadata that no rule consumes.

## Exact rules text

Transcribed verbatim; printed line breaks are joined with a single space. **Bold** marks the keywords
as the cards print them. `[bounty]` is the inline wanted-poster glyph and `[life]` the inline Life
counter glyph, rendered the way Tigerclaw's shard renders `[Poison]`. *Italics* mark the parenthetical
reminder text the cards print in italic.

| Card | Printed text |
| --- | --- |
| Dead or Alive | Target a unit adjacent to you. After the attack: You may give an enemy hero in radius a [bounty] bounty marker; a hero with a bounty marker spends 1 additional [life] life counter when defeated. |
| Side Quest | If you are adjacent to an enemy unit, give a hero in radius a [bounty] bounty marker. The hero with a bounty marker spends 1 additional [life] life counter when defeated. |
| Get over here! | Target a unit or a token in a straight line and in range with no obstacles between you and the target; move the target towards you in a straight line, until you are adjacent. |
| Light Crossbow | Target a unit in a straight line and in range with no units, or terrain, between you. |
| Heavy Crossbow | Target a unit in a straight line and in range with no units, or terrain, between you. |
| Hand Crossbow | **Choose one —** • Target a hero in range with a [bounty] bounty marker. • Target a unit adjacent to you. |
| Arbalest | Target a unit in a straight line and in range with no units, or terrain, between you. |
| Hunter-Seeker | **Choose one, or both,** on different targets — • Target a hero in range with a [bounty] bounty marker. • Target a unit adjacent to you. |
| A Game of Chance | An enemy hero in radius with two or more cards in hand chooses one of those cards; Guess the card's color, then reveal the card; If you guessed correctly, discard that card; otherwise you gain 1 coin. *(You can only guess colors that could be in that player's hand)*. |
| Dead Man's Hand | An enemy hero in radius with two or more cards in hand chooses one of those cards; Guess that card's color, then reveal the card; If you guessed correctly, discard that card; otherwise you gain 2 coins. |
| Drinking Buddies | You may have a hero in radius retrieve a discarded card. If they do, you may also retrieve a discarded card. |
| We're Not Done Yet! | An enemy hero in radius with two or more cards in hand chooses one of those cards; Guess that card's color, then reveal the card; If you guessed correctly, discard that card; otherwise may repeat once or gain 2 coins. |
| Another One! | You may have a hero in radius retrieve a discarded card. If they do, you may also retrieve a discarded card. **End of turn:** May repeat once. |
| Close Call | If a hero in play has a [bounty] bounty marker, block the attack and that hero gives the marker to you. *(The marker's effect is applied to you)*. |
| Narrow Escape | If a hero in play has a [bounty] bounty marker, block the attack and retrieve the marker. |
| Vantage Point | Ignore obstacles. If a hero in play has a [bounty] bounty marker, +1 [movement] movement. |
| Perfect Getaway | If a hero in play has a [bounty] bounty marker, block the attack. |
| High Ground | Ignore obstacles. If a hero in play has a [bounty] bounty marker, +2 [movement] movement. |
| A Complicated Profession | After you give a hero the [bounty] bounty marker that hero discards a card, if able. |

**BA-11. The three guessing faces differ from one another in exactly three places, and one of them is
a word that reads like a typo and is not.** A Game of Chance prints "Guess **the** card's color"; Dead
Man's Hand and We're Not Done Yet! print "Guess **that** card's color". The transcription keeps each as
printed. The other two are the wrong-guess reward (1 coin, 2 coins, or a modal choice between repeating
once and 2 coins) and the Radius (2, 3, 3). **A Game of Chance alone prints the parenthetical
reminder** about the legal guess domain; the upgrades drop it and keep the rule, which is what a
reminder is rather than a rules difference.

**BA-12. Drinking Buddies and Another One! print byte-identical first sentences and differ only in
Another One!'s extra keyword sentence.** Both are Radius 3, so unlike Cutter's Bombardment/Barrage pair
the Tier difference is entirely the added "End of turn: May repeat once." Recorded because the
composition must not collapse them.

**BA-13. "Spends 1 additional life counter when defeated" is a rule about the DEFEATED HOLDER, and the
one declaration this build already has for that sentence is about the ATTACKER.** Dead or Alive and
Side Quest both attach the cost to the hero wearing the marker. Ursafar's Tear prints the same number
for the opposite subject - "if you defeated a hero, that hero spends 1 additional Life counter" - and
`RuleModificationKind.AdditionalDefeatLifeCost`'s own remarks record that it "is declared about the
*attacker*". **This row named the right subject and the wrong vocabulary.** 8.40a measured two
FAMILIES rather than one read twice: a marker affects its holder whoever that hero is, so asking Tear's
family about the defeated hero would charge a bounty-wearing ATTACKER's own victim. Tear is untouched
and p.13's one spend composes both, so two declarations charge two - the 2026-09-06 ruling's own
reading, which makes a marker on a hero Ursafar defeats cost two without either card saying so.

**BA-14. Both marker-giving faces make the give conditional in different ways, and neither condition is
about the recipient.** Dead or Alive's is p.10's `may` and sits inside an "After the attack:" clause;
Side Quest's is mandatory but gated on "If you are adjacent to an enemy unit", a fact about Bain rather
than about the hero receiving the marker. Recorded because a reader who saw only one of them would
generalise the wrong softener.

## Upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is the choice one Level Up
offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Light Crossbow | Heavy Crossbow (+1 Defense) / Hand Crossbow (+1 Initiative) | Arbalest (+1 Defense) / Hunter-Seeker (+1 Movement) |
| Blue | A Game of Chance | Dead Man's Hand (+1 Defense) / Drinking Buddies (+1 Attack) | We're Not Done Yet! (+1 Range) / Another One! (+1 Attack) |
| Green | Close Call | Narrow Escape (+1 Initiative) / Vantage Point (+1 Attack) | Perfect Getaway (+1 Initiative) / High Ground (+1 Radius) |

The Tier III lineage follows the printed text rather than the table order: Heavy Crossbow's plain
clear-line shot continues into Arbalest, Hand Crossbow's marker modal into Hunter-Seeker's
one-or-both, Dead Man's Hand's guess into We're Not Done Yet!, Drinking Buddies' shared retrieval into
Another One!, Narrow Escape's blocking Green into Perfect Getaway, and Vantage Point's moving Green
into High Ground.

**BA-15. Two of his items can only ever raise a value printed on a DIFFERENT colour of his own deck,
and they are exact mirrors of each other.** We're Not Done Yet!'s `+1 Range` is on a Blue card that
prints Radius, so it can only help his Red and Silver faces; High Ground's `+1 Radius` is on a Green
card that prints no reach at all, so it can only help his Gold and Blue faces. Both are p.16's ordinary
behaviour - an item raises a value wherever the hero prints it - and both are worth naming because a
reader checking an item against its own card would find nothing there for it to raise. Cutter's CU-12
records the same pair of facts about Tumble Shot and Crashland.

## Capability contract map

**This section is Bain's, not the matrix's.** [ROSTER_CAPABILITY_MATRIX.md](ROSTER_CAPABILITY_MATRIX.md)
owns the cross-hero mapping and is too close to its own threshold to hold a fifteenth section; the
notation below is that file's, so the two read alike.

`A`, `S`, `T`, `C` and `X` bind the actor, source card, chosen target, chooser and enclosing action
facts. `Out(n)` binds a typed output from node `n`. `M` means an impossible mandatory step preserves
earlier applied nodes and stops the remainder, `May` may be declined, and `IfAble` silently skips only
when infeasible. **Registered** means the contract is live in the default production catalog today;
**owed** names the build milestone that mints it.

**The audit below is why this series is the size it is, and it corrected the pre-authorization read in
seven places.** Most of his text lands on contracts that exist, and two clauses that looked new are
parameters on long-carried declarations. What was unbuilt fell in eight places, one of them - the
guessing game - larger than the other seven together, and blocks 8.40a-8.40h are exactly those rows.

| Faces | Contract | State |
| --- | --- | --- |
| Dead or Alive, Side Quest, Hand Crossbow, Hunter-Seeker, the five Greens, the Ultimate | the bounty marker itself - a `MarkerDefinition` with `MarkerReturnCondition.GeneralCleanup` and a `MarkerGivingHandler` bound to it | **registered in mechanism; hero content.** `SyntheticComponentCapabilities.MarkerGivingHandler` is a public constructor taking a reference and a definition, and `TigerclawComponentContent` registers two instances of it from his own file. Bain's is a `BainComponentContent` beside it, registering at his dealing on milestone 7.9's gate. The handler TYPE is proved, so it owes no showcase consumer. |
| Dead or Alive, Side Quest | `give -> synthetic-give-marker@<his id> (holder = Out of the radius selection)` | registered - the optional `holder` parameter (Hero, not required) is exactly "a card whose own sentence bounds the recipient binds the hero its selection chose", which is Tigerclaw's shape. The selections themselves are `synthetic-radius-hero-target@1` with `relation` bound. |
| Dead or Alive, Side Quest | "a hero with a bounty marker spends 1 additional [life] life counter when defeated" - the cost charged to the DEFEATED HOLDER | **BUILT at 8.40a, and NOT the way this row predicted.** The row said only the READ was missing; BA-13 records what the probe measured instead. The marker declares a TWELFTH family, `AdditionalDefeatLifeCostWhenDefeated`, and p.13's one spend site reads the eleventh about the attacker and the twelfth about the defeated hero, composing both into one number. See BA-Q4. |
| Side Quest | "If you are adjacent to an enemy unit" - a gate on the ACTOR's own adjacency | registered (`presence-count@1`, the radius-0 form Wasp's and Sabina's faces already ask, read through a `Conditional`) |
| Close Call | "that hero gives the marker to you" - the marker MOVES from its current holder to the defender | **BUILT at 8.40b** as `MarkerTransferHandler`: one attachment of a declared definition moves as ONE operation, keeping its identity, publishing both heroes, touching no supply. Its own capability rather than an arm of the give, because a return-then-give would mint a second identity and re-run what watches a give - the Ultimate. |
| Narrow Escape | "retrieve the marker" - return whichever bounty marker is on the board, whatever card gave it | **BUILT at 8.40b.** `synthetic-return-marker@1` returns what **this source card gave** - `Markers.GivenBy(context.SourceCardInstanceId)` - and Narrow Escape gives none, so `ReturnMarkerHandler` gained a second, definition-bound reading: the one attachment of that definition wherever it sits. The registered instance is byte-identical. |
| Dead or Alive | giving while another hero already holds it - see BA-D1 | **BUILT at 8.40b** as an optional default-off `movesWhenSupplyIsEmpty` parameter on the give: with the supply empty and one copy out, the give MOVES it, identity kept. Earlier nodes bind nothing. |
| Close Call, Narrow Escape, Vantage Point, Perfect Getaway, High Ground | "If a hero in play has a [bounty] bounty marker" - a predicate that is TRUE when any hero holds it and PUBLISHES which hero | **BUILT at 8.40c, on the shape this row predicted.** The sweep held: across 171 registered capabilities the only marker-named parameter was the giving handler's `holder`. `bain-bounty-marker-held@1` is a NEW handler type on `MarkerGivingHandler`'s terms - definition bound at construction, deferred beside the give, owing a showcase consumer at 8.40j - publishing `present` and the `holder` Close Call's transfer reads. `hero-condition@1` is untouched. A `Conditional`'s own branch cannot bind that `Conditional`'s output - but **no face of his needs to** (8.40i): the transfer's `holder` is where the marker goes and its source is derived, so all five Greens branch straight off the predicate. |
| Hand Crossbow | bullet 1: "Target a hero in range with a [bounty] bounty marker" - the target set narrowed to marker holders | **BUILT at 8.40c** as the seventh optional narrowing, `holdsTheActorsMarker`, beside `excludeTarget`, `playedAttackThisTurn`, `adjacentToFriendlyMinion`, `adjacentToAnotherEnemyUnit`, `excludeAdjacent` and `continuedLineFrom` - the first about a COMPONENT. Default-off, and the forty-four `choose-target@2` nodes the corpus composes bind none of it, asserted over those declarations themselves. No digest moved. |
| Hunter-Seeker | the same clause on a p.14 attack-sequence BULLET | **BUILT at 8.40c** as `AttackSequenceSubjectNarrowing.HoldsTheActorsMarker`, a fourth member valued 3, with `None`, `HasOneOrMoreCardsInTheDiscard` and `HasAnEmptyDiscard` keeping their values and their behaviour; an `EffectCapabilityReference` here stays REFUSED, because a persisted bullet naming a capability id would be "a payload choosing which code runs". It is evaluated where a bullet's target set is derived, off the marker register that backend has held since 6.12 - no widened signature. |
| Hand Crossbow | modal shape: `Choose one` over two bullets, each binding its own Attack target | **CORRECTED at 8.40i: `attack-sequence@1` bullets rather than Dodger's `set-attack-target@1` arms.** The marker narrowing lives on `choose-target@2`, whose reach is a distance a definition writes down, and on the bullet, whose reach is the printed Range with p.16's items on it. This face prints "in range", and BA-15's `+1 Range` item exists to raise exactly that - so a written 3 would raise everything except the face it was printed for. |
| Hunter-Seeker | modal shape: `Choose one, or both, on different targets` | registered - Dodger's Middlefinger of Death prints the identical construction and composes as an `EffectNodeKind.Choose` with `minimumSelections: 1, maximumSelections: 2` over two bullet nodes, with `distinctTargets` bound true. See BA-D4. |
| Hand Crossbow, Hunter-Seeker | bullet 2: "Target a unit adjacent to you" | registered (`AttackTargetReach.AdjacentOnly` on the bullet's policy) |
| Light Crossbow, Heavy Crossbow, Arbalest | "in a straight line and in range" | registered as of milestone 8.38a - `AttackTargetLine.InStraightLine`, built for Swift's Red line, reached through `PrintedAttackTargetPolicy.RestrictedToTheStraightLine()` |
| Light Crossbow, Heavy Crossbow, Arbalest | "with no units, or terrain, between you" - a line blocked by units and terrain and NOT by tokens | **BUILT at 8.40d**, and this row named two faces too many - the modals print no line clause. `AttackTargetLine` gained a fourth member, `InStraightLineClearOfUnitsAndTerrain`, reached through `PrintedAttackTargetPolicy.RestrictedToTheStraightLineClearOfUnitsAndTerrain()` and expressible on an `attack-sequence@1` bullet. The walk is p.19's file with both endpoints excluded; terrain is `TerrainPresenceQuery`'s question and a unit is the board's, so p.18's wider obstacle list is never consulted. See BA-03. |
| Get over here! | "a unit **or a token** in a straight line and in range with no obstacles between you and the target" | **BUILT at 8.40e, and this row predicted the contract correctly** - `synthetic-range-obstacle-target@1` already walked p.19's six directions to the printed Range Total, stopped at each file's first obstacle, and blocked on exactly p.18's full list. It gained two optional things: `excludeTerrain`, which narrows what may be NAMED and never what BLOCKS, so a file whose first obstacle is terrain offers nothing rather than what stands behind it; and `actorApproachSpace`, the actor-side twin of `obstacleApproachSpace`. Both approach outputs are now published only for a node that declared one. |
| Get over here! | "move the target towards you in a straight line, until you are adjacent" | **BUILT at 8.40e, and the MOVE half needed no change at all.** `synthetic-direct-move@3`'s subject is a typed coordinate naming "a hero, a minion, or a token", and `DirectMoveDestinationPolicy.NamedSpace` takes the space the selection published and opens no choice; a moved token keeps its identity, owner and controller. A target already adjacent is published its own space, and an `IfAble` Move then takes no step. `DirectMoveDestinationPolicy.TowardActor` is NOT the contract: a per-step filter bounded by the node's own `distance`, no straight line, no adjacency stop, bound by no card. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "An enemy hero in radius **with two or more cards in hand**" - a selection narrowed by hand SIZE | **BUILT at 8.40f** as the count form of `hasCardInDiscard`: an optional `minimumCardsInHand` integer on the printed-Radius hero selection, unbound everywhere else, with `presence-count@1`'s declared threshold as the precedent. It discloses nothing - `SeatHandSummary` has published every hero's hand COUNT to every seat since 8.30a - and both readings ask one `HeroHandQuery`. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "chooses one of those cards" - a card chosen and KEPT, hidden from every seat but the chooser's until the reveal | **BUILT at 8.40f** as `hidden-card-choice@1`, deferred until his dealing: the named hero picks one of their own hand cards and the card does not move. The OFFER half needed nothing, because `ChooserHeroId` has directed a pause to a named hero since 7.1a. The ANSWER was the work, and it is an application-layer projection arm rather than a visibility field on every request - `EffectChoiceRequest` gained none. See BA-D3. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "then reveal the card" - a public reveal naming one card | **BUILT at 8.40f** as `reveal-chosen-card@1` plus a new `MatchLogEntryKind.CardRevealedByCardText`, which takes the wire to `/15`; `IsPublic`'s hand arm then makes the card nameable in every seat, unchanged. Reusing p.7's `CardRevealed` was measured and refused: its consumers read it as a turn-card flip, and the channel's ledger asserts one producer. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "Guess the card's color" restricted to "colors that could be in that player's hand" | **BUILT at 8.40f** as an optional `restrictToColorsOwnedBy` (Hero) parameter on `name-color@1` rather than a sibling capability, which would have moved the registered count and every pinned digest. Unbound for Tali, whose six-colour offer is byte-identical. The domain is the printed deck minus what the OPEN zones account for, minus Purple. See BA-D2. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | the comparison, then `discard that card` | **BUILT at 8.40f.** `reveal-chosen-card@1` publishes the revealed colour and the verdict on ONE node, on `defended-card-color@1`'s argument, so this build gains no integer comparator; the discard took an optional `restrictToCard` mirroring `retrieve-card@1`'s, which discards the named card without opening a prompt. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | `otherwise you gain 1/2 coins` | registered (`gain-resource@1`, `recipient` and `amount` both required) |
| We're Not Done Yet! | `otherwise may repeat once **or** gain 2 coins` - a modal on the FAILURE branch | **BUILT at 8.40f**, which shipped it rather than splitting it: it mints no capability, adds no parameter and persists no field - it is a `Choose` over a `RepeatAction` and a coin grant. A repeat inside a Choose branch was probed legal before the modal was costed, and a bullet whose second instance cannot run is greyed. 8.40f1 is discharged. See BA-Q2. |
| Drinking Buddies, Another One! | "You may have a hero in radius retrieve a discarded card" - ANOTHER hero retrieves, from their own discard, choosing from their own seat | registered - **and this is the row the pre-authorization read got most wrong.** `retrieve-card@1` takes `owner` (Hero), `chooser` (Hero), `kind` and an optional `restrictToCard`; it builds candidates from `owner`'s pile and issues the prompt to `chooser` through `EffectChoiceRequest`'s explicit `chooserHeroId`. Silverarrow's Fae Healing and Nature's Blessing and Tali's Guardian Spirit and Warrior Spirit already bind BOTH to a selection's `chosenHero`. Nothing is owed. |
| Drinking Buddies, Another One! | "If they do, you may also retrieve a discarded card" - a gate on the first retrieval's outcome | **CORRECTED at 8.40i: the right contract over a fact the capability did not publish.** `synthetic-is-flagged@1`'s `flag` is a BOOLEAN and the retrieval published a card and a hero. Silverarrow's "if they do" needs none - its consequence is about the retriever - and Bain's is about HIMSELF, so `retrieve-card@1` gained `didRetrieve` on `discard-owned-card@1`'s `didDiscard` precedent; an added OUTPUT moves no digest. The second retrieval binds `owner` and `chooser` to the actor. |
| Another One! | "**End of turn:** May repeat once" | **registered in mechanism; hero content, and the cost is stated rather than assumed.** There is no reusable end-of-turn repeat contract. Tali's Blizzard clause is a hero-specific `EffectCapabilityId`, a hero-specific `ActiveEffectDefinition` at `ActiveEffectDurationKind.EndOfTurn`, an `ActiveEffectCreationHandler` registration, and a hand-written duplicate of the node graph inside a triggered effect - because `EffectNodeKind.RepeatAction` re-runs the enclosing ACTION and there is no enclosing action at `TimingCheckpoint.TurnEnded`. Another One! pays the same four costs at composition. |
| Close Call, Narrow Escape, Perfect Getaway | a Block whose condition is a fact about the BOARD rather than about the attack | **BUILT at 8.40g**, as a third `PrintedBlockCondition` member rather than a second axis: an axis means a new public property on `ActionDefinition`, which 8.38d measured as every dealt hero's digest, while a member on an enum an existing property carries moves nothing - and the cross product is a sentence no face prints. `AHeroInPlayHoldsTheActorsMarker` is answered from the board BEFORE the attack is named, through `HeldMarkerQuery`'s actor-derived reading, so BA-D5's "his own" means here what it means in 8.40c's predicate. `DefenseBlockQuery.Evaluate` took the register as a REQUIRED parameter - its two callers, the resolver and the DEFENCE PREVIEW, read one object, and an absent register refuses loudly. The pairing guard is untouched both ways. |
| Close Call, Narrow Escape | Defense card text that runs when the Block succeeds | **SETTLED at 8.40g:** p.14 step 4, where every Defense text runs - and step 4 is not conditional on step 6, so the face gates itself by reading its own condition again, which is how Wasp's two shipped "if you do" riders already work |
| Vantage Point, High Ground | a Movement primary action that carries card text | registered - Ursafar's Cold Ire and Eyes of Flame are `ActionKind.Movement` with `CardTextApplicationPolicy.Applies` and a declared text timing, and their own remarks say why the timing is the card's word: a raise "must be read before it is spent" |
| Vantage Point, High Ground | "Ignore obstacles" | registered - `RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement)`, which Silverarrow's Trailblazer already declares and `ObstacleQuery` already reads. The definition and its capability id are hero content on the proved `ActiveEffectCreationHandler` type. |
| Vantage Point, High Ground | "+1 / +2 [movement] movement" while a hero holds the marker | registered - **and NOT the way the pre-authorization read predicted.** It is not a `RosterValueQueryKind`: `RosterValueContributionDefinition`'s constructor **refuses the Movement channel outright** ("Roster card-text contributions modify Attack, Range or Defense") and additionally binds the Defense channel one-to-one to a single query, so the combination cannot be constructed at all. The registered shape is Ursafar's - an Active effect declaring `RuleModification.ForValue(PrintedValueChannel.Movement, amount)` - created by a node the marker predicate gates. See BA-Q3. |
| A Complicated Profession | `trigger -> "After you give a hero the bounty marker"` | **BUILT at 8.40h, CHEAPER than predicted.** `after-push-trigger@1`'s shape, not its HOME: a twentieth `TimingCheckpoint`, `MarkerGivenToHero`, carried in `TriggerCheckpoints` beside a seventh `UltimateTriggerCondition`, `AfterControllerGivesMarker`. No property joined `UltimateRuleDefinition`, so the cost stated above - `GrantedTargetExclusion` moved ALL twelve digests - went unpaid: the probe measured every pin byte-identical. No subject enum either; the sentence narrows the giver, not the recipient. |
| A Complicated Profession | `-> synthetic-discard-chosen-card@1 (chooser = the hero who received it; IfAble)` | registered - revision 1 is the `if able` arm that declines silently, which is what the printed words ask |
| A Complicated Profession | fires only when BAIN gives it, never when Close Call's transfer moves it | **BUILT at 8.40h**, and structural: only the give REPORTS a recipient, so a transfer and a return reach no query at all, and a giver who is not the controller is refused by one. See BA-D5. |

## Decisions and open questions

**BA-D1. The supply is ONE bounty marker, and giving it while another hero holds it MOVES it.** Two
printed facts point the same way: the Ultimate says "**the** bounty marker" with a definite article,
and all five Green faces ask whether "**a** hero in play has a bounty marker" in the singular and then
Close Call says "that hero gives **the** marker to you". Tigerclaw's Poison declares a supply of six,
which is the alternative and is rejected here: his two strengths are two definitions of one physical
stack and every hero on the board can wear one, while nothing in Bain's set ever puts two out. The
consequence the build has to implement is the second half: with a supply of one, `MarkerGivingHandler`
reports impossible when the marker is already held, and Dead or Alive would silently stop offering its
own printed clause from the second turn onwards. At a physical table the marker would simply be taken
off the first hero and pinned on the second. **Recorded for the user to overrule**; if the supply were
six instead, the transfer at 8.40b would still be owed for Close Call and the movement reading would
become unreachable rather than wrong. 8.40a implements the count as
`BainComponentContent.BountySupplyCount`; 8.40b built the move.

**BA-D2. The guess domain is the set of p.9 colours that PUBLIC information cannot rule out of the
chooser's hand.** A Game of Chance's parenthetical says "You can only guess colors that could be in
that player's hand", and `name-color@1` offers all six unconditionally. Three constraints follow from
the rules rather than from preference: p.9's Purple is the Ultimate, which sits in the Ultimate area
and never enters a hand, so it is never a legal guess; a hero's printed deck is public knowledge, so
the colours they own are known; and every zone but the hand and deck is visible, so a colour fully
accounted for in the discard pile, the turn area, the item slots and the boxed Handicap cannot be in
hand. **The domain is therefore derived from what every seat can see, never from the hand itself** -
deriving it from the hand would leak the very information the clause is protecting, which is the
sharpest instance in this set of the standing rule that an authorization filter is not a projection
filter.

**BA-D3. The chosen card is hidden from every seat but the chooser's until the reveal, and the reveal
is permanent.** The hiding half is the printed sequence: the guess is named BEFORE the card is turned
over, so a chooser's answer that any other seat could read would make the guess free. The permanence
half is less obvious and is taken deliberately: after a wrong guess the card goes back to a hand that
every other player has now seen it in, and `SeatProjection.IsPublic` already takes exactly that reading
for a different sentence - a retrieved card "remains known at the physical table after p.8 returns it",
with the bounded public log as the memory. Bain's reveal is the same fact, so it should use the same
memory rather than a second one.

**BA-D4. Hunter-Seeker follows Dodger's D-06 ruling and Dodger's composition.** "Choose one, or both,
on different targets" is Middlefinger of Death's printed construction word for word, and that ruling
reads it as two complete p.14 orders, each defending separately, with `distinctTargets` bound true.
Nothing on Bain's face asks for a different reading.

**BA-D5. "A hero in play" means any hero on the board, Bain included, holding BAIN's own marker - and
the Ultimate fires only on a give he performs.** The Green faces ask about the board rather than about
a side, so a marker Close Call has just pulled onto Bain himself still satisfies Vantage Point's
condition on a later turn of the same round, which is what "(The marker's effect is applied to you)"
tells the reader to expect. The Ultimate's clause is the mirror: it prints "After **you** give a hero
the bounty marker", so Close Call's transfer - where another hero gives it to Bain - is not a give he
performed and must not fire it. This is why 8.40b's transfer is its own operation rather than a
return followed by a give. 8.40c reads "his own" as the attachment's `OwnerHeroId`.

**BA-Q1. Drinking Buddies' and Another One!'s "a hero in radius": does it include Bain, and does it
include enemies? SETTLED at 8.40i: every hero EXCEPT Bain, enemies included.** "A hero" carries no
side-word, which on this set's own conventions means every hero - RULING 8's structure on a different
card. The second sentence's "**you may also** retrieve" then means nothing unless the first retrieval
was somebody else's, so the selection excludes the actor; p.21's Healer role - "Healers help their
allies by letting them retrieve" - is what the first half is for. Proven where the readings differ.

**BA-Q2. What does We're Not Done Yet!'s repeat re-ask, and of whom? SETTLED at 8.40f: everything, of
anybody.** The printed sentence names no hero and no exclusion, so a second run re-asks every question
the first asked - it MAY pick a different enemy hero, it need not, and the now-revealed card is not
excluded from a second choice by the same hero. Each alternative would be a rule no printed word
carries, and 8.39d1's per-selection exclusion is a parameter a card asks for rather than a property
every repeat has. Each reading is proven on a board where the alternatives differ; 8.35z15's rule
covers only the degenerate case.

**BA-Q3. What is the scope of Vantage Point's and High Ground's "+N movement"? SETTLED at 8.40i: the
TURN's, so a second Movement action in the same turn is raised too - Ursafar's answer inherited.** The
reason is structural: `RuleValueApplicability` has nothing scoped to one ACTION and
`ActiveEffectDurationKind` names no such window, so the turn is the shortest scope this build can
express for a raise. Both faces are Movement primaries and a hero performs one primary per turn, so
what this reaches is a secondary boot or an action a text granted. Proven off the composed effect.

**BA-Q4. Does the bounty marker's defeat cost apply to a hero defeated by something other than an
Attack?** p.13's Defeat is reached from card effects as well as from p.14's step 6, and the one site
that composes the additional cost today is inside the Attack backend. The printed sentence says "when
defeated" with no cause attached, so the natural answer is that the cost is charged wherever p.13's
spend is computed.

**SETTLED at 8.40a: YES, and it cost nothing.** Counted rather than assumed:
`HeroDefeatRewardSchedule.LifeCounters` and `TeamLifeState.TrySpend` each have one production caller,
both inside `ResolveHeroDefeat`, whose two entrances are p.14's step 6 and 8.22's
`ResolveEffectInvokedHeroDefeat` - through which every card-text Defeat arrives, from an Attack's text,
a Skill or a trigger's frame. p.13's list has been one chain since 6.6b, so no Minion Battle or
end-of-round path exists. A Skill-caused Defeat is asserted.

## Components and prepared artwork sources

Every row below is measured from the private corpus rather than guessed, and each names how.

| Component | Source |
| --- | --- |
| Selection card (the hover pick card) | `cards/hero-selection/randomizers-tt14__7723683__front.png`, matched to `_selection-cards/Bain__front.png` by SHA-256 (`5388D4AFC311CC06292F13487F430F5554EBFB1064DFB4F1DD3CE27694E158B4`) |
| Dashboard | `active-hero-cards/_dashboards/Bain__front.png`, 850x445, printing "Bain the Bounty Hunter" |
| Card fronts | the eighteen dealt fronts in `active-hero-cards/Bain/`, 372x520 |
| Standee element | `8596242` / `BountyHunter`, `VerticalTokens` - **BA-16** |
| Standee faces | two 302x302 faces in `Image_0_Atlas` at (1563,3007) and (1867,3007) |
| Bounty marker | `1421888` / `BountyMarker`, `Tokens`, one 708x714 face in `Image_17_Atlas` at (3214,0) - **BA-17** |

**BA-16. His standee is in the bundled manifest under an epithet rather than his name, and this is the
fourth time that has happened.** The `VerticalTokens` list contains no entry named `Bain`. It contains
`BountyHunter` (id 8596242), and the identification is visual and unambiguous rather than inferred from
the name: the cropped 302x302 faces show the same wide maroon brim, the same round goggle lenses pushed
up over the eyes, the same long dark hair and the same dark coat as his dashboard art and his profile
card. The name is exactly the kind this corpus uses - Whisper's is `Outcast`, Tali's is `Spiritcaller`,
Cutter's is `Airship_Captain`, and "Bounty Hunter" is his printed epithet outright, which makes his the
least disguised of the four.

His element has **two** 302x302 faces, one ringed blue and one ringed amber, the same shape every other
hero's has, so `Prepare-HeroTokens.ps1`'s `$declaredFaceSize = 302` two-face invariant holds for him and
needs no widening. Measured through the manifest's own object model with `ConvertFrom-Json`; a naive
text split of the minified JSON on `},{` cuts the `sprites` array itself and reports one face where
there are two.

**BA-17. The corpus holds real artwork for the bounty marker, and it is the second marker component
this project has found.** `BountyMarker` (1421888) sits immediately beside Tigerclaw's `PoisonMarker`
(1421887): same `Tokens` element type, same `Horizontal` token type, same 708x714 face in the same
atlas. Its face is a **"WANTED" poster nailed to a board inside a riveted steel ring**, which the
inline `[bounty]` glyph on his cards is a small rendering of. Recorded as a source fact rather than as
work: this build renders a held marker on the hero rail rather than on a hex, so the dealing owes a rail
caption and not a board sprite - but the art exists if the presentation ever wants it. **8.40j kept the
generated treatment**: a rail row carries no image, so that face has nothing to be prepared into, and
Tigerclaw's is unprepared for the same reason.

**BA-18. A fixture composing its catalog from a DEFERRED list is a red only a dealing can see, and it
has two faces.** Six offline fixtures concatenated one of his three deferred lists; emptying those
lists at 8.40j made three throw `Capability 'bain-bounty-marker-held' is not registered` and two throw
`Capability references must be unique` for handing the now-registered handlers back as content. One
mistake read from either side - naming the list that HOLDS a contract rather than the list that HAS it
- and `Handlers()` is the fix, meaning the same set before the dealing and after.
