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

His printed set is inventoried at milestone 8.40. Composition, registration, dealing and the permanent
player-path gate are later milestones in the
[fifteenth-hero shard](../Roadmap/PHASE_8_FIFTEENTH_HERO.md); the vocabulary the text names as owed is
built by the lettered build blocks between them. **Nothing in this shard authorizes implementation.** An
inventory row records what the printed face says and which contract would carry it, and that is all it
does.

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

[Source provenance](../RULES_LEDGER.md) still governs the rest. The extracted cards are third-printing
playtest and the rulebook is second printing, so a disagreement between them is edition skew rather than
an extraction error, and a designer answer may carry a third-printing rules change.

**BA-02. His marker is a rulebook keyword rather than a new idea, and p.18, p.8 and p.13 already say
its whole lifetime.** p.18's Markers keyword says markers are reminders "usually given to other heroes
as part of the card's effect" and "usually not placed on the board"; p.8's end-of-round step returns
every marker to the supply; and p.13's Defeat list returns a defeated hero's. Those three sentences are
exactly `MarkerReturnCondition.GeneralCleanup`, which Tigerclaw's Poison marker already declares. So
nothing about WHEN the bounty marker comes back is new; what is new is every rule his cards attach to
holding one.

**BA-03. His set prints TWO different line-clearance sentences on purpose, and the difference is the
most consequential source fact in the inventory.** Get over here! prints "with **no obstacles** between
you and the target"; all five crossbows print "with **no units, or terrain**, between you". p.18's
Obstacle keyword makes the first list strictly wider than the second - "Units, terrain (including
printed on the board), tokens, as well as any objects represented by miniatures, are obstacles" - so a
token standing on the file blocks the pull and does not block a crossbow. One hero's own deck printing
both halves of that distinction is as strong a signal as this corpus ever gives that the narrower
wording is deliberate, and the mapping below treats them as two contracts rather than one.

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
carrying three of one glyph and three of the other.** This is milestone 8.35z20's trap at the largest
scale the roster has produced - Swift's Bounce and Cutter's Grappling Bolt each had one instance, and
Bain has six. Get over here!, Close Call, Narrow Escape, Vantage Point, Perfect Getaway and High Ground
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
with no candidate left ambiguous. **All six p.16 channels appear in one hero's set**, which is worth
naming because a reader checking for a missing channel would find none.

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
sections, Initiative 4, and Movement 5 with one brighter conditional section.** The measurement was
calibrated against Brogan's profile card rather than a remembered pitch, because his shard already
states Attack 7, Defense 8, Initiative 1 and Movement 2-with-two-brighter - which fixes the pixel pitch
and both bar origins from known values at both ends of the scale - and a luminance-against-trough scan
was used rather than a colour classifier, because his violet frame bleeds into the fill. p.21 states
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
printed. The other two differences are the reward on a wrong guess (1 coin, 2 coins, or a modal choice
between repeating once and 2 coins) and the printed Radius (2, 3, 3). **A Game of Chance alone prints
the parenthetical reminder** about the legal guess domain; the two upgrades drop it while keeping the
rule it describes, which is the ordinary behaviour of a reminder rather than a rules difference.

**BA-12. Drinking Buddies and Another One! print byte-identical first sentences and differ only in the
printed Radius and Another One!'s extra keyword sentence.** Both are Radius 3, so unlike Cutter's
Bombardment/Barrage pair the Tier difference is entirely in the added "End of turn: May repeat once."
Recorded because the composition must not collapse them.

**BA-13. "Spends 1 additional life counter when defeated" is a rule about the DEFEATED HOLDER, and the
one declaration this build already has for that sentence is about the ATTACKER.** Dead or Alive and
Side Quest both attach the cost to the hero wearing the marker. Ursafar's Tear prints the same number
for the opposite subject - "if you defeated a hero, that hero spends 1 additional Life counter" - and
`RuleModificationKind.AdditionalDefeatLifeCost`'s own remarks record that it "is declared about the
*attacker*". The family says "one" and two declarations charge two, which is already the reading the
2026-09-06 ruling settled, so a marker on a hero Ursafar defeats charges two by construction and
neither card has to say so.

**BA-13 named the right subject and the wrong vocabulary.** 8.40a measured two FAMILIES rather than one
read twice: a marker affects its holder whoever that hero is, so asking Tear's family about the defeated
hero would charge a bounty-wearing ATTACKER's own victim. Tear is untouched and p.13's one spend
composes both, so two declarations charge two.

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
owns the cross-hero mapping and sits close enough to its own threshold that a fifteenth hero's section
would push it over; the notation below is that file's, unchanged, so the two read alike.

`A`, `S`, `T`, `C` and `X` bind the actor, source card, chosen target, chooser and enclosing action
facts. `Out(n)` binds a typed output from node `n`. `M` means an impossible mandatory step preserves
earlier applied nodes and stops the remainder, `May` may be declined, and `IfAble` silently skips only
when infeasible. **Registered** means the contract is live in the default production catalog today;
**owed** names the build milestone that mints it.

**The audit below is why this hero's series is the size it is, and it corrected the pre-authorization
read in seven places.** Most of his text lands on contracts that already exist, and two clauses that
looked new are parameters on declarations this build has carried for milestones. What is genuinely
unbuilt is concentrated in eight places, and one of them - the hidden-information guessing game - is
larger than the other seven put together. **Every owed row below names exactly one build block, and
the eight build blocks 8.40a-8.40h are exactly the blocks those rows name.**

| Faces | Contract | State |
| --- | --- | --- |
| Dead or Alive, Side Quest, Hand Crossbow, Hunter-Seeker, the five Greens, the Ultimate | the bounty marker itself - a `MarkerDefinition` with `MarkerReturnCondition.GeneralCleanup` and a `MarkerGivingHandler` bound to it | **registered in mechanism; hero content.** `SyntheticComponentCapabilities.MarkerGivingHandler` is a public constructor taking a reference and a definition, and `TigerclawComponentContent` registers two instances of it from his own file. Bain's is a `BainComponentContent` beside it, registering at his dealing on milestone 7.9's gate. The handler TYPE is proved, so it owes no showcase consumer. |
| Dead or Alive, Side Quest | `give -> synthetic-give-marker@<his id> (holder = Out of the radius selection)` | registered - the optional `holder` parameter (Hero, not required) is exactly "a card whose own sentence bounds the recipient binds the hero its selection chose", which is Tigerclaw's shape. The selections themselves are `synthetic-radius-hero-target@1` with `relation` bound. |
| Dead or Alive, Side Quest | "a hero with a bounty marker spends 1 additional [life] life counter when defeated" - the cost charged to the DEFEATED HOLDER | **BUILT at 8.40a, and NOT the way this row predicted.** The row said only the READ was missing; BA-13 records what the probe measured instead. The marker declares a TWELFTH family, `AdditionalDefeatLifeCostWhenDefeated`, and p.13's one spend site reads the eleventh about the attacker and the twelfth about the defeated hero, composing both into one number. See BA-Q4. |
| Side Quest | "If you are adjacent to an enemy unit" - a gate on the ACTOR's own adjacency | registered (`presence-count@1`, the radius-0 form Wasp's and Sabina's faces already ask, read through a `Conditional`) |
| Close Call | "that hero gives the marker to you" - the marker MOVES from its current holder to the defender | **owed, 8.40b.** Nothing transfers a marker. `synthetic-give-marker@1` mints a NEW attachment from the supply and refuses when the supply is empty; `synthetic-return-marker@1` detaches and returns. A transfer is neither, and composing it as return-then-give would mint a new instance identity and re-run whatever watches a give - which is precisely the Ultimate. |
| Narrow Escape | "retrieve the marker" - return whichever bounty marker is on the board, whatever card gave it | **owed, 8.40b.** `synthetic-return-marker@1` returns a marker **this source card gave**: its finder is `components.Markers.GivenBy(context.SourceCardInstanceId)`. Narrow Escape gives no marker at all, so it has nothing to return by that reading, and the sentence is about the one on the board. |
| Dead or Alive | giving while another hero already holds it - see BA-D1 | **owed, 8.40b.** With a supply of one and the marker held, `MarkerGivingHandler` reads `CountAvailableMarkers(...) < 1` and reports impossible. The recorded reading is that the give MOVES it. |
| Close Call, Narrow Escape, Vantage Point, Perfect Getaway, High Ground | "If a hero in play has a [bounty] bounty marker" - a predicate that is TRUE when any hero holds it and PUBLISHES which hero | **owed, 8.40c.** No capability reads marker state as a predicate: the only marker readers in the catalog are the supply check and re-entrancy guard inside the giving handler and the finder inside the return. `hero-condition@1` is the near miss in shape - a Boolean predicate a `Conditional` names directly - but its `condition` parameter is a closed vocabulary whose one non-`None` member is `Enraged`, and it asks about the ACTOR rather than about the board. A hero output beside the Boolean is what Close Call's transfer reads. |
| Hand Crossbow | bullet 1: "Target a hero in range with a [bounty] bounty marker" - the target set narrowed to marker holders | **owed, 8.40c.** `choose-target@2` carries six optional narrowings today - `excludeTarget`, `playedAttackThisTurn`, `adjacentToFriendlyMinion`, `adjacentToAnotherEnemyUnit`, `excludeAdjacent`, `continuedLineFrom` - and none is about a component. A seventh, optional and default-off, is the shape; every declaration written before it keeps its exact candidate set, which this project has measured cannot move a digest. |
| Hunter-Seeker | the same clause on a p.14 attack-sequence BULLET | **owed, 8.40c**, and it is a fourth member on an existing enum rather than a parameter. `AttackSequenceBullet` persists its predicate as `AttackSequenceSubjectNarrowing`, a closed enum with `None`, `HasOneOrMoreCardsInTheDiscard` and `HasAnEmptyDiscard`; its own remarks record that an `EffectCapabilityReference` here was measured and REFUSED, because a persisted bullet naming a capability id would be "a payload choosing which code runs". The same remarks record that an added enum member is not a content-revision event. |
| Hand Crossbow | modal shape: `Choose one` over two bullets, each binding its own Attack target | registered - Dodger's three "Choose one" modal Attacks are this shape (`attack-sequence@1` per bullet plus `set-attack-target@1`), and the whole-node parameters are `units`, `targetReach`, `line`, `distinctTargets` with `subjectNarrowing` and `appliesAfterAttackText` optional. |
| Hunter-Seeker | modal shape: `Choose one, or both, on different targets` | registered - Dodger's Middlefinger of Death prints the identical construction and composes as an `EffectNodeKind.Choose` with `minimumSelections: 1, maximumSelections: 2` over two bullet nodes, with `distinctTargets` bound true. See BA-D4. |
| Hand Crossbow, Hunter-Seeker | bullet 2: "Target a unit adjacent to you" | registered (`AttackTargetReach.AdjacentOnly` on the bullet's policy) |
| Light Crossbow, Heavy Crossbow, Arbalest | "in a straight line and in range" | registered as of milestone 8.38a - `AttackTargetLine.InStraightLine`, built for Swift's Red line, reached through `PrintedAttackTargetPolicy.RestrictedToTheStraightLine()` |
| Light Crossbow, Heavy Crossbow, Arbalest, and both crossbow modals' ranged bullet | "with no units, or terrain, between you" - a line blocked by units and terrain and NOT by tokens | **owed, 8.40d.** `AttackTargetLine` has exactly three members and every one says only WHICH FILE a target stands on, never what stands on it; the type's own remarks record that p.9 states no line-of-sight rule at all. The near miss is `synthetic-range-obstacle-target@1`'s walk, and it is the wrong shape twice: it blocks on `board.TryGetObjectAt` - **any** board object, tokens included - as well as on terrain and overlays, and it targets the obstacle rather than something past it. See BA-03. |
| Get over here! | "a unit **or a token** in a straight line and in range with no obstacles between you and the target" | **owed, 8.40e**, and it is a widening of 8.39b's family rather than a new selection. `synthetic-range-obstacle-target@1` already walks p.19's six directions to the printed Range Total, already stops at the first obstacle on each file, and already blocks on exactly p.18's full list. What it does not do is exclude terrain from the TARGETABLE set or publish a destination on the actor's side; its two outputs are `chosenObstacleSpace` and `obstacleApproachSpace`, and the approach is one step back from the obstacle rather than one step out from the actor. |
| Get over here! | "move the target towards you in a straight line, until you are adjacent" | **owed, 8.40e**, and the MOVE half is already registered. `synthetic-direct-move@3`'s subject is a typed coordinate naming "a hero, a minion, or a token", and `DirectMoveDestinationPolicy.NamedSpace` takes a space an earlier node published and opens no choice - which is exactly how Cutter's Grappling Bolt performs the mirror-image move of the ACTOR. `DirectMoveDestinationPolicy.TowardActor` is NOT the contract: it is a per-step filter bounded by the node's own `distance`, is not constrained to a straight line, has no adjacency stop condition, and is referenced by no card content in the build. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "An enemy hero in radius **with two or more cards in hand**" - a selection narrowed by hand SIZE | **owed, 8.40f.** `owned-card-present@1` takes `hero` and `location` and answers `present` - "at least one", by `.Any(...)` - which is a presence flag rather than a threshold and a predicate rather than a narrowing. `hasCardInDiscard` on `synthetic-range-hero-target@1` is the closest NARROWING in shape and is about the discard pile and about presence. **`presence-count@1`'s optional `minimumPresent` integer is the closest THRESHOLD in shape** and counts pieces standing on the board rather than cards in a hand. Nothing in the build reads a hand CARDINALITY. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "chooses one of those cards" - a card chosen and KEPT, hidden from every seat but the chooser's until the reveal | **owed, 8.40f**, and it is the contract this series is sized around. `EffectValueKind.CardInstance` and `EffectChooserRole.BoundHero` both exist, and another hero's hand is already absent from a seat projection rather than concealed in it (`F-ER2-07`). What does not exist is a choice whose ANSWER stays private: `EffectChoiceRequest` carries `chooser`, `candidates`, the two selection bounds, `chooserHeroId` and a value vocabulary, and no visibility field at all, and every card-valued choice in the build today terminates immediately in a discard, a retrieval, a borrow or a play. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "then reveal the card" - a public reveal naming one card | **owed, 8.40f.** The nearest mechanism is real and is not reachable: `MatchLogEntryKind.CardRevealed` carries a `subjectCardInstanceId`, and `SeatProjection.IsPublic` makes a HAND card public to every seat exactly when the bounded public log names it. But the only producer is p.7's turn-card commitment flow, through `CardLifecycleAuthority.TryReveal`, which no capability handler can reach. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | "Guess the card's color" restricted to "colors that could be in that player's hand" | **owed, 8.40f.** `name-color@1` is the verb - one required `chooser` (Hero) parameter, one `namedColor` (Integer) output, drawn with `EffectChoiceValueVocabulary.CardColor` - but its candidate set is the hard-coded static list `AllNameableColors`, all six of p.9's colours, and its own remarks record that nothing reads which colours are live in anyone's hand. Bain's parenthetical is a narrowing that must be derived from PUBLIC information only. See BA-D2. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | the comparison, then `discard that card` | **owed, 8.40f** for the comparison against a hidden value; the discard itself is registered. `synthetic-discard-chosen-card@2` takes `hero` and an optional `color` over `All<CardColor>()` and outputs `discarded` plus `unableToDiscard`; here the card is already named, which is what a `restrictToCard`-shaped binding would express. |
| A Game of Chance, Dead Man's Hand, We're Not Done Yet! | `otherwise you gain 1/2 coins` | registered (`gain-resource@1`, `recipient` and `amount` both required) |
| We're Not Done Yet! | `otherwise may repeat once **or** gain 2 coins` - a modal on the FAILURE branch | **owed, 8.40f.** The modal itself is ordinary and the repeat is `EffectNodeKind.RepeatAction`, but this is the build's first `Choose one` whose bullets are reached only on one arm of a comparison, and the repeat re-runs a text that asks an ENEMY to choose - so what a second failure offers is a question the block settles rather than inherits. Split it out as 8.40f1 if the implementation measures it as a separate seam, on the precedent 8.39d1, 8.39f1, 8.39g1 and 8.39g2 set. |
| Drinking Buddies, Another One! | "You may have a hero in radius retrieve a discarded card" - ANOTHER hero retrieves, from their own discard, choosing from their own seat | registered - **and this is the row the pre-authorization read got most wrong.** `retrieve-card@1` takes `owner` (Hero), `chooser` (Hero), `kind` and an optional `restrictToCard`; it builds candidates from `owner`'s pile and issues the prompt to `chooser` through `EffectChoiceRequest`'s explicit `chooserHeroId`. Silverarrow's Fae Healing and Nature's Blessing and Tali's Guardian Spirit and Warrior Spirit already bind BOTH to a selection's `chosenHero`. Nothing is owed. |
| Drinking Buddies, Another One! | "If they do, you may also retrieve a discarded card" - a gate on the first retrieval's outcome | registered (`synthetic-is-flagged@1`, which exists to present an earlier node's published fact in the shape a `Conditional` reads, over `retrieve-card@1`'s `retrieved` output; the second retrieval is `retrieve-card@1` with `owner` and `chooser` both the actor, which is Brogan's and Misa's shape) |
| Another One! | "**End of turn:** May repeat once" | **registered in mechanism; hero content, and the cost is stated rather than assumed.** There is no reusable end-of-turn repeat contract. Tali's Blizzard clause is a hero-specific `EffectCapabilityId`, a hero-specific `ActiveEffectDefinition` at `ActiveEffectDurationKind.EndOfTurn`, an `ActiveEffectCreationHandler` registration, and a hand-written duplicate of the node graph inside a triggered effect - because `EffectNodeKind.RepeatAction` re-runs the enclosing ACTION and there is no enclosing action at `TimingCheckpoint.TurnEnded`. Another One! pays the same four costs at composition. |
| Close Call, Narrow Escape, Perfect Getaway | a Block whose condition is a fact about the BOARD rather than about the attack | **owed, 8.40g.** `PrintedBlockCondition` has two live members and both read the attacking card's own p.13 classification; the type's remarks say the answer is read "off nothing the defender holds", and `ActionDefinition` enforces the pairing both ways - a Block naming no condition throws, and a non-Block naming one throws. `DefenseBlockQuery.Evaluate` is handed the offer, the definitions and the ATTACK's reference and has no board, no components and no participants at all. Its two callers are the resolver and the DEFENCE PREVIEW, and that file's own remarks say a preview that disagreed with the resolution "would be worse than no preview at all". Bain's three Blocks also print no ranged/non-ranged clause, so the condition is unconditional in the attack and conditional in the board - which neither existing member says. |
| Close Call, Narrow Escape | Defense card text that runs when the Block succeeds | registered in shape - Arien's, Tigerclaw's and Wasp's Defense actions all carry `DefenseTextFor` card text - with the TIMING of a Block's text the thing 8.40g settles rather than inherits |
| Vantage Point, High Ground | a Movement primary action that carries card text | registered - Ursafar's Cold Ire and Eyes of Flame are `ActionKind.Movement` with `CardTextApplicationPolicy.Applies` and a declared text timing, and their own remarks say why the timing is the card's word: a raise "must be read before it is spent" |
| Vantage Point, High Ground | "Ignore obstacles" | registered - `RuleModification.ForPermission(RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement)`, which Silverarrow's Trailblazer already declares and `ObstacleQuery` already reads. The definition and its capability id are hero content on the proved `ActiveEffectCreationHandler` type. |
| Vantage Point, High Ground | "+1 / +2 [movement] movement" while a hero holds the marker | registered - **and NOT the way the pre-authorization read predicted.** It is not a `RosterValueQueryKind`: `RosterValueContributionDefinition`'s constructor **refuses the Movement channel outright** ("Roster card-text contributions modify Attack, Range or Defense") and additionally binds the Defense channel one-to-one to a single query, so the combination cannot be constructed at all. The registered shape is Ursafar's - an Active effect declaring `RuleModification.ForValue(PrintedValueChannel.Movement, amount)` - created by a node the marker predicate gates. See BA-Q3. |
| A Complicated Profession | `trigger -> "After you give a hero the bounty marker"` | **owed, 8.40h.** `TimingCheckpoint` carries nineteen members and none is a marker moment; `UltimateRuleDefinition` carries `BeforeAction` (`before-action-trigger@1`) and `AfterPush` (`after-push-trigger@1`), and the second is the exact shape this needs - a p.16 permanent ability watching a moment, with a tiny enum naming which subject makes it fire. The cost is stated in advance: `GrantedTargetExclusion` on this same type was the first change ever to move ALL twelve corpus digests, because the identity oracle walks every public instance property. |
| A Complicated Profession | `-> synthetic-discard-chosen-card@1 (chooser = the hero who received it; IfAble)` | registered - revision 1 is the `if able` arm that declines silently, which is what the printed words ask |
| A Complicated Profession | fires only when BAIN gives it, never when Close Call's transfer moves it | **owed, 8.40h** as part of the trigger's subject, and it is the reason the transfer at 8.40b must not be composed as return-then-give. See BA-D5. |

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
`BainComponentContent.BountySupplyCount`; 8.40b builds the move.

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
performed and must not fire it. This is why 8.40b's transfer must be its own operation rather than a
return followed by a give.

**BA-Q1. Drinking Buddies' and Another One!'s "a hero in radius": does it include Bain, and does it
include enemies?** The face says "a hero", not "a friendly hero" and not "an enemy hero", which on this
card set's own conventions means every hero. The natural reading of the second sentence - "**you may
also** retrieve" - is that the first hero is somebody other than Bain, or "also" says nothing; and
p.21's Healer role, which his profile card prints and which reads "Healers help their allies by letting
them retrieve", points at allies without excluding anyone. The build block records the reading it takes
and proves it on a board where the three readings differ.

**BA-Q2. What does We're Not Done Yet!'s repeat re-ask, and of whom?** The repeat is on the FAILURE
branch of a clause whose first step hands a choice to an enemy hero. Whether a second run may pick a
different enemy hero, whether it must, and whether the first chooser's now-revealed card is excluded
from the second choice are three questions the printed sentence does not answer. Milestone 8.35z15's
general rule - a card-text repeat with no legal subject is not offered - covers the degenerate case and
not these three. Settled by the block that builds it.

**BA-Q3. What is the scope of Vantage Point's and High Ground's "+N movement"?** The registered shape
for a Movement raise is Ursafar's `ActiveEffectDurationKind.ThisTurn` Active effect, which would also
raise a second Movement action performed later in the same turn - a repeat, or one a text granted. His
two faces print the bonus on the action they are, and a turn that performs two Movement actions is
reachable. The question is inherited from Ursafar rather than introduced here, so the composition
records whether it is answered the same way and proves it, rather than assuming the inheritance settles
it.

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
caption and not a board sprite - but the art exists if the presentation ever wants it.
