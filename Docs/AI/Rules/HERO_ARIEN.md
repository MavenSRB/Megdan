# Guards Digital - Arien Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Arien the Tidemaster holds a Blue roster slot on the pitch line-up milestone 8.17 approved. This shard owns
his content inventory, his source-verification status, and any deviation or open question scoped to Arien
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. The rulebook
refers to Arien as "he"; this shard follows the source.

## Arien content ledger

The pitch requires Arien's complete progression. Before verified Arien definitions or effects are implemented, the inventory must be implementation-grade, containing at least:

- Stable definition ID and content revision.
- Display name, source image path/face, edition, and starting hand/deck/set-aside membership.
- Hero/content-set ID, typed `CardColor`, nullable tier, and explicit same-color upgrade/replacement relationships.
- Basic/non-basic/Ultimate/Handicap classification as applicable.
- Stable action-definition IDs; derived Primary/Secondary/reaction/replacement offer contexts; subtype; and text-application policy.
- Initiative, Attack, Defense, Movement, Range, and Radius values where present.
- Exact rules text transcription.
- Ordered mandatory/optional steps; choice owner; modal/repeat behavior.
- Structured triggers, conditions, targets versus presence checks, choices, values, effects, batch/simultaneous behavior, timing, duration, and cancellation.
- Stable effect-node IDs, capability ID/version, typed input/result bindings, operation-specific policies, outcome facts, and systemic checkpoints.
- Reused capability IDs and any controlled custom-handler ID.
- Item contribution and destination when set aside; Active-effect versus passive/continuous-source classification; Ultimate persistence requirements.
- Required source-linked scenarios and interaction/ordering tests.
- Source verification status and reviewer/date.
- Any official correction or clarification.

Current status: **source-verified at 8.18a (2026-08-23)**. Every printed value, icon and word on the
nineteen faces below is transcribed here, read twice - once from the whole face and again from
icon-region and text-region enlargements built for the purpose. Nothing is mapped onto a capability yet:
milestone 8.19 produces the roster-wide capability matrix and assigns the implementation slices, and the
fields above that name capability IDs, typed bindings, effect-node IDs and source-linked tests stay owed
until it does. No Arien definition, effect, or test exists in the build.

## Arien source faces

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes; the internal names and element IDs below are that extraction's identity for a face, not this project's.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Arien/arien*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Arien/arien*__*__back.png` | 19 | 372x520 | Yes - all nineteen are byte-identical, and the deck emblem they carry is distinct from all thirty-one other heroes' backs, which is what p.5's symbol-match step asks for |
| Hero dashboard front | `active-hero-cards/_dashboards/Arien__front.png` | 1 | 850x445 | Yes - title, the four turn slots, the discard strip, the level track and the four-slot item legend all read |
| Hero dashboard back | `active-hero-cards/_dashboards/Arien__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder shared by ten other heroes; see open question A-01 |
| Hero profile card front | `active-hero-cards/_selection-cards/Arien__front.png` | 1 | 372x260 | Yes - name, complexity star, all four role icons and all four stat bars resolve |
| Hero profile card back | `active-hero-cards/_selection-cards/Arien__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |

## Arien deck composition

Nineteen printed faces make up p.6's eighteen-card deck plus the Handicap card that setup returns to the box. The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Noble Blade, Spell Break, Dangerous Current, Aspiring Duelist, Liquid Leap |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Living Tsunami |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6 step 11), used only for an odd player count (p.20) | Ceremonial Blade |

The rulebook illustrates exactly these five starting cards together on p.6, beside the instruction that produces them, so the starting hand is read from the source rather than derived from the Tier icons alone.

## Arien printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no such icon, so that secondary action is unavailable from it. Card text applies only when the primary action is performed (p.9).

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `arien` | 7507116 | Noble Blade | Gold `D` | basic | 11 | 2 | 1 | Basic Attack | Attack 4 | - | - |
| `arien19` | 7452193 | Ceremonial Blade | Gold `D` | `H` | 10 | 1 | 1 | Basic Attack | Attack 3 | - | - |
| `arien2` | 7506070 | Spell Break | Silver `S` | basic | 13 | 3 | - | Basic Skill | - | Radius 3 | - |
| `arien3` | 7717453 | Dangerous Current | Red `R` | I | 8 | 6 | 4 | Attack | Attack 6 | - | - |
| `arien4` | 7717452 | Raging Stream | Red `R` | II | 8 | 7 | 4 | Attack | Attack 7 | - | Initiative |
| `arien5` | 7717451 | Rogue Wave | Red `R` | II | 8 | 4 | 4 | Attack - Ranged | Attack 4 | Range 2 | Defense |
| `arien6` | 7717449 | Violent Torrent | Red `R` | III | 9 | 7 | 4 | Attack | Attack 7 | - | Initiative |
| `arien7` | 7717448 | Tidal Blast | Red `R` | III | 9 | 4 | 4 | Attack - Ranged | Attack 4 | Range 2 | Movement |
| `arien8` | 7507117 | Aspiring Duelist | Blue `B` | I | 9 | - | 3 | Defense | Defense 5 | - | - |
| `arien9` | 7542898 | Expert Duelist | Blue `B` | II | 10 | - | 3 | Defense | Defense 6 | - | Initiative |
| `arien10` | 7507024 | Slippery Ground | Blue `B` | II | 10 | 6 | - | Movement | Movement 3 | - | Attack |
| `arien11` | 7542897 | Master Duelist | Blue `B` | III | 10 | - | 3 | Defense | Defense 6 | - | Range |
| `arien12` | 7507025 | Deluge | Blue `B` | III | 10 | 6 | - | Movement | Movement 3 | Radius 1 | Attack |
| `arien13` | 7452180 | Liquid Leap | Green `G` | I | 4 | 3 | 2 | Skill - Ranged | - | Range 2 | - |
| `arien14` | 7452182 | Magical Current | Green `G` | II | 4 | 3 | 2 | Skill - Ranged | - | Range 3 | Attack |
| `arien15` | 7506069 | Arcane Whirlpool | Green `G` | II | 4 | 3 | 2 | Skill - Ranged | - | Range 4 | Defense |
| `arien16` | 7452197 | Stranger Tide | Green `G` | III | 3 | 4 | 2 | Skill - Ranged | - | Range 3 | Radius |
| `arien17` | 7452183 | Ebb and Flow | Green `G` | III | 3 | 4 | 2 | Skill - Ranged | - | Range 4 | Defense |
| `arien18` | 7702299 | Living Tsunami | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception here: the two Gold cards and the one Silver card are basic and every other card is non-basic, so **Arien's only basic skill action is Spell Break's primary** and his only basic attack is Noble Blade's, or Ceremonial Blade's when handicapped.

Every card also offers p.7's iconless Hold. The other two iconless secondary actions are conditional on what the card already prints, and the condition is the printed rule rather than a convention: p.11's Fast Travel replaces *an entire Movement action*, so it is offered by the seventeen cards printing a Movement icon and by neither Spell Break nor Living Tsunami; p.13's Clear replaces *an entire Attack action*, so it is offered only by the seven cards whose primary is an Attack, Arien printing no Attack secondary icon anywhere.

Arien prints no Block card and no combined Defense/Skill card. His three Blue Defense cards carry an ordinary printed Defense value and a plain Defense primary icon, so their text is applied under p.14's rule for a card whose Primary action is Defense - it reaches the table only when the card is discarded to defend, and playing the card instead offers its Movement 3 secondary with no text at all.

## Arien exact rules text

Transcribed verbatim from the faces; the printed line breaks are typographic and are joined with a single space here. **Bold** marks p.17's Active-effect keyword as the card prints it. Parenthesised italics are p.9's reminder text.

| Card | Printed text |
| --- | --- |
| Noble Blade | Target a unit adjacent to you. Before the attack: You may move another unit that is adjacent to the target 1 space. *( "Another unit" never includes you. )* |
| Ceremonial Blade | Target a hero adjacent to you. Before the attack: You may move a minion that is adjacent to the target 1 space. |
| Spell Break | **This turn:** Enemy heroes in radius cannot perform Skill actions, except on the gold cards. *( Skill is an action type. Other action types are unaffected. )* |
| Dangerous Current | Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 2 spaces in a straight line directly behind the target discards a card, or is defeated. |
| Raging Stream | Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 3 spaces in a straight line directly behind the target discards a card, or is defeated. |
| Rogue Wave | Target a unit in range. After the attack: You may push an enemy unit adjacent to you up to 2 spaces. |
| Violent Torrent | Target a unit adjacent to you. Before the attack: Up to 1 enemy hero in any of the 5 spaces in a straight line directly behind the target discards a card, or is defeated. May repeat once on a different unit. |
| Tidal Blast | Target a unit in range. After the attack: You may push an enemy unit adjacent to you up to 3 spaces. |
| Aspiring Duelist | Ignore all minion defense modifiers. *( This includes your minions, as well as the enemy minions. )* |
| Expert Duelist | Ignore all minion defense modifiers. **This turn:** You are immune to attack actions of all enemy heroes, except this attacker. |
| Slippery Ground | **This turn:** Enemy heroes adjacent to you cannot fast travel, or move more than 1 space with a movement action. |
| Master Duelist | Ignore all minion defense modifiers. **This round:** You are immune to attack actions of all enemy heroes, except this attacker. |
| Deluge | **This turn:** Enemy heroes in radius cannot fast travel, or move more than 1 space with a movement action. |
| Liquid Leap | Place yourself into a space in range without a spawn point and not adjacent to an empty spawn point. |
| Magical Current | Place yourself into a space in range without a spawn point and not adjacent to an empty spawn point. |
| Arcane Whirlpool | Swap with an enemy minion in range. *( Swap places with the target. This is not movement. )* |
| Stranger Tide | Place yourself into a space in range without a spawn point. |
| Ebb and Flow | Swap with an enemy minion in range; if it was adjacent to you, may repeat once. |
| Living Tsunami | Once per turn, before performing an Attack action, you may move 1 space. |

Spell Break's reminder text prints the Skill action-type icon between the word "Skill" and "is"; the icon is reproduced above as the bare word, which is the only place any Arien face embeds an icon inside a sentence.

Four cards carry an Active-effect keyword by p.19: Spell Break, Expert Duelist and Slippery Ground print **This turn:** and Master Duelist prints **This round:**, which no Wasp face does - it is the first Active effect in the pitch inventory that survives a turn boundary and is cancelled only at the end of the round. Living Tsunami is p.16's permanent Ultimate ability rather than an Active effect. Every other card resolves immediately.

Timing inside p.14's nine-step Attack/Defense order is printed rather than inferred, and Arien uses three different seams where Wasp used one. Noble Blade, Ceremonial Blade, Dangerous Current, Raging Stream and Violent Torrent print **Before the attack:**, which is step 1. Rogue Wave and Tidal Blast print **After the attack:**, which is step 7 and therefore runs after the defending hero has already been removed at step 6. The three Blue Defense cards' text is defender-side and lands at step 4, ahead of the step-5 total, and their "Ignore all minion defense modifiers" clause governs the step-2 calculation the total then reads. Living Tsunami sits outside that order entirely: it fires before an Attack action is performed at all, not inside it.

## Arien upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair below is the choice one Level Up offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Dangerous Current | Raging Stream (+1 Initiative) / Rogue Wave (+1 Defense) | Violent Torrent (+1 Initiative) / Tidal Blast (+1 Movement) |
| Blue | Aspiring Duelist | Expert Duelist (+1 Initiative) / Slippery Ground (+1 Attack) | Master Duelist (+1 Range) / Deluge (+1 Attack) |
| Green | Liquid Leap | Magical Current (+1 Attack) / Arcane Whirlpool (+1 Defense) | Stranger Tide (+1 Radius) / Ebb and Flow (+1 Defense) |

Arien's twelve item icons cover all six of p.16's icons - three Attack, three Defense, three Initiative, and one each of Movement, Range and Radius - so they reach all four dashboard slots, the fourth of which takes Range, Movement and Radius together. Arien's dashboard legend row is pixel-identical to Wasp's, which is the second source for that grouping. Because p.16 gives an item no effect on a card that prints no matching icon, the reach of each item is fixed by the value table above:

| Item | Slot | Arien cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - the cards with an Attack primary, six of them once Ceremonial Blade is boxed |
| +1 Defense | 2 | 18 - every card but Living Tsunami, counting the three Blue cards' primary Defense value |
| +1 Initiative | 3 | 18 - all but Living Tsunami |
| +1 Movement | 4 | 17 - all but Spell Break and Living Tsunami, counting Slippery Ground's and Deluge's primary Movement value |
| +1 Range | 4 | 7 - Rogue Wave, Tidal Blast, Liquid Leap, Magical Current, Arcane Whirlpool, Stranger Tide, Ebb and Flow |
| +1 Radius | 4 | 2 - Spell Break and Deluge |

Arien's Ultimate prints neither a Range nor a Radius icon, so ledger question 12's approved interpretation - that an item's +1 reaches a matching printed icon on an Ultimate - has no Arien instance to apply to, and nothing in this set depends on it.

## Arien hero profile

| Field | Printed |
| --- | --- |
| Hero name | Arien on the profile card; **Arien the Tidemaster** on the dashboard and in every rulebook reference |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Tactician, Disabler, Durable, Pusher |
| Stat profile | Attack 8, Defense 5, Initiative 4, Movement 5 on p.21's stated 1-to-8 scale, with Defense and Movement each carrying one brighter sixth section |
| Level track | the generic 1-8 track with the p.13 Life cost 1/1/1/2/2/2/3/3, identical on every hero's dashboard and therefore not Arien-scoped data |

The stat profile is read, not estimated. p.21 states the scale explicitly - "Each of the four main Stats ... has a value, ranging from `Very poor` to `Exceptional` (1 to 8)" - and states what the brighter sections mean: "Brighter colored sections of the stat bars are conditional. They depend on your level-up choices and triggering specific card texts." Arien's bars resolve to discrete pips against a blue frame at this extraction's resolution, where Wasp's gold frame defeated the same measurement and left W-02 recorded as approximate. p.21 additionally uses **Arien's own profile card** as its anatomy illustration and prints the four role names in its text, so the roles are confirmed from the rulebook and not only from the icon art. This is selection metadata that no rule consumes.

## Arien source observations

- The second-printing rulebook illustrates **seven** of Arien's nineteen action-card faces, across four pages: Noble Blade, Spell Break, Dangerous Current, Aspiring Duelist and Liquid Leap on p.6; Noble Blade again and Slippery Ground on p.7; Expert Duelist, plus Slippery Ground and Aspiring Duelist again, on p.11; and Spell Break and Liquid Leap again on p.12. Five of the seven reproduce the extracted faces exactly - name, colour letter, Tier, Initiative, secondary values, band text, primary value, Range or Radius icon and card text all match.
- **Two differ, and one of the two is in card text rather than reminder text.** Aspiring Duelist's reminder reads *( This includes your minions, as well as the enemy minions. )* on the face and *( This includes your minions, as well as enemy minions. )* in the rulebook, on both pages that illustrate it. Spell Break's card text reads "cannot perform Skill actions, except on **the** gold cards" on the face and "cannot perform skill actions, except on gold cards" in the rulebook, again on both pages. Neither difference changes what the card does - one drops an article from a clarification, the other drops an article and a capital - but the second is a difference in the operative sentence, which Wasp's single reminder-text difference was not. Card text outranks the rulebook under the ledger's [authority hierarchy](../RULES_LEDGER.md#authority-hierarchy), so the face's wording is the one this project transcribes, and the rulebook's wording must not be substituted back in.
- **Deluge is not a stronger Slippery Ground by reach; it is a stronger Slippery Ground by scaling.** p.9 states that a Range or Radius value of 1 means the primary action "can only affect adjacent targets", so Deluge's printed Radius 1 covers exactly the spaces Slippery Ground's "adjacent to you" covers. What the Tier III card buys is that its geometry is an icon rather than a word, and p.16 states that items "never affect values in the card text, only the value of the Initiative, Attack, Defense, Movement, Range, or Radius icons" - so a +1 Radius item widens Deluge and can never widen Slippery Ground. The two are otherwise identical in printed values, and they differ in item icon only by which pair they sit in.
- **Stranger Tide is a strict superset of Liquid Leap and Magical Current, by dropping a clause.** All three place Arien into a space in range without a spawn point; only the Tier I and Tier II printings add "and not adjacent to an empty spawn point". The rulebook's own worked example on p.12 settles what the longer sentence excludes, naming both exclusions against a highlighted board.
- Violent Torrent prints "May repeat once on a different **unit**" where Wasp's Thunder Boomerang prints "on a different **target**". The transcriptions keep each face's own noun; whether the two clauses mean the same thing is a mapping question for milestone 8.19, not an inventory one.
- Ebb and Flow's repeat condition is printed in the past tense - "if it **was** adjacent to you" - which is what makes it readable at all, since the swap it follows has already moved the minion into Arien's former space. The condition is therefore evaluated against the pre-swap position.
- Every keyword Arien's faces use is defined in the rulebook except one. p.18 defines Immune, Place, Push and Minion Defense Modifiers (the last by pointing at p.14's three-line calculation); p.19 defines Straight Line, Swap, Spawn Point, Unit, and This Turn / This Round. **"Directly behind" is defined nowhere** - a whole-rulebook text search finds the word only inside Arien's own Dangerous Current illustration and in one unrelated idiom - and it is recorded as open question A-02 rather than assumed.
- No Arien card places, names, or targets a token or a marker. Five Wasp cards act on "a unit, or a token"; no Arien card mentions tokens at all, so his set raises no token-supply question beyond the unreadable dashboard back recorded as A-01.
- No published errata or official ruling affecting any Arien face was located; none of the local sources contains one.
- The rulebook's page numbering is worth stating once, because two earlier milestones cited it inconsistently: in the local second-printing PDF the printed page number is the PDF page index minus one. Setup steps 1-8, including step 7's recommended line-up, are on **printed p.5**; setup steps 9-11, including the deck of eighteen, the three-Tier-I-plus-two-basics starting hand, the Handicap card returned to the box, and the sentence putting a hero's tokens and markers on the dashboard back, are on **printed p.6**. Every page citation in this shard uses printed numbers.

## Arien open questions

Scoped to Arien alone and therefore resident here. Anything broader stays in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions). All four were raised at 8.18a and **A-01, A-02 and A-03 were answered by explicit user decision on 2026-08-23**, so nothing in this set blocks Arien's capability mapping or implementation.

- **A-01 - does Arien use his own tokens or markers? ANSWERED on 2026-08-23: no, and the question is no longer Arien-scoped.** The working answer this milestone recorded is the answer, and the user's ruling supplied the general reason it holds without a readable dashboard back: **a hero's tokens and markers are defined by that hero's card faces**, so a set whose faces name none has none, and the blank Tabletopia placeholder is missing art rather than missing rules. Arien's evidence is doubly clear - no Arien face names, places or targets a token or a marker at all, and his profile card carries none of p.22's `Tokens` role icon, which that page defines as marking exactly the heroes "capable of placing tokens". The general rule now lives as [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions), because it settles the identical question for every remaining hero rather than for Arien alone; Wasp's W-01 is answered from the same decision. No Arien definition claims a token or marker supply.
- **A-02 - what is "in a straight line directly behind the target"? ANSWERED on 2026-08-23 by explicit user decision, on the reading milestone 8.18a proposed.** Printed on Dangerous Current, Raging Stream and Violent Torrent, which reach 2, 3 and 5 such spaces. p.19 defines Straight Line as "a sequence of spaces arranged in a file", but the rulebook never defines "behind", and no other located face uses the word - the reading is therefore taken from the one directional phrase the rulebook does define, p.18's Push, "move it in a straight line directly away from the pushing unit". **The approved reading:** the spaces are the continuation of the file running from Arien's space through the target's space, counted outward from the target, so "behind" is measured from the attacker. Three consequences follow and are part of the approval. The file is determined by the attacker/target pair, so the card asks no direction question. The printed "Up to 1 enemy hero in any of the N spaces" is therefore a selection among whichever enemy heroes stand in them, not a choice of where to look. And a space occupied by an obstacle is still one of the N, since p.18's Nearest counts spaces with obstacles and p.9 states there are no line-of-sight rules, so nothing shortens the file. This is an approved interpretation rather than sourced text; a later official ruling supersedes it. If a second mapped hero prints "behind", it moves to the ledger, exactly as W-05 did.
- **A-03 - does Spell Break's "except on the gold cards" mean the Gold colour, or basic cards generally? ANSWERED on 2026-08-23 by explicit user decision: the Gold colour, as printed.** The user's ruling states the vocabulary the question turned on: a card text naming a colour names that colour, while **Basic** is the supertype spanning Gold *and* Silver, which is why a hero can hold both a basic Attack on Gold and a basic Skill on Silver. The two words are therefore not interchangeable, and Spell Break exempts only the Gold ones. p.9 already lists Gold(D) and Silver(S) as separate colours and p.12 states that "A Skill action is always the Primary action", so the exception can only ever reach a Gold card's primary Skill. **The consequence is live in this pitch roster and was accepted with the ruling:** a hero whose basic Skill is printed on the Silver card is stopped by Spell Break, and Wasp is exactly that hero - Static Barrier is Silver, so Arien's Spell Break shuts it down while a Gold-printed basic Skill would survive. Arien's own two Gold cards are both Basic Attacks, so the exception exempts none of his own Skills either way. The general vocabulary reading is recorded once in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger)'s card upgrades row, because it will decide any other card text that names a colour rather than a supertype.
- **A-04 - edition confirmation. Answered for Arien with one caveat.** Every value and word on the nineteen faces is consistent with the second printing: the seven rulebook illustrations above, the p.9 colour-letter and Tier conventions, the p.20 `H` Handicap card, the p.16 Tier IV Ultimate, and p.21's own use of Arien's profile card. The caveat is the pair of wording differences recorded in the source observations, one of which falls in card text. This answers the ledger's first open question for Arien and establishes the edition of no other hero's faces in the same extraction.
