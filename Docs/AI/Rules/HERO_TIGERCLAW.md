# Guards Digital - Tigerclaw Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Tigerclaw the Cutpurse holds a Red roster slot on the pitch line-up milestone 8.17 approved. This shard
owns his content inventory, source-verification status, and any deviation or open question scoped to
Tigerclaw alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. The rulebook
refers to Tigerclaw as "he"; this shard follows the source.

## Tigerclaw content ledger

The pitch requires Tigerclaw's complete progression. Before verified Tigerclaw definitions or effects are implemented, the inventory must be implementation-grade, containing at least:

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

Current status: **source-verified at 8.18d and capability-mapped at 8.19 (2026-08-23)**. Every printed value, icon and word on the
nineteen faces below is transcribed here, read twice - once from the whole face and again from
icon-region and text-region enlargements. The Poison marker's own extracted atlas art and manifest entry
were also inspected. The roster-wide [capability matrix](ROSTER_CAPABILITY_MATRIX.md#tigerclaw) now assigns
every face/component, typed binding, policy, checkpoint, and source-linked scenario family. No Tigerclaw
definition, effect, marker, or test exists in the build; milestones 8.19a-8.19c own the shared dependencies
and 8.23 owns composition.

## Tigerclaw source faces

Every action-card file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md`
describes; the internal names and element IDs below are that extraction's identity for a face, not this
project's. The Poison marker is not part of the curated hard-link directory, so its exact manifest and atlas
locations are recorded separately.

| Face/component | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files/elements | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Tigerclaw/tigerclaw*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Tigerclaw/tigerclaw*__*__back.png` | 19 | 372x520 | Yes - all carry the same Tigerclaw crossed-blades emblem; fifteen and four form two byte-distinct but visually identical raster groups, recorded under source observations |
| Hero dashboard front | `active-hero-cards/_dashboards/Tigerclaw__front.png` | 1 | 850x445 | Yes - title, four turn slots, discard strip, level track and four-slot item legend all read and exactly match the dashboard illustrated on pp.15-16 |
| Hero dashboard back | `active-hero-cards/_dashboards/Tigerclaw__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder; [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions) makes the card faces the marker definition, and the marker itself is present below |
| Hero profile card front | `active-hero-cards/_selection-cards/Tigerclaw__front.png` | 1 | 372x260 | Yes - name, complexity star, three role icons and all four stat bars resolve |
| Hero profile card back | `active-hero-cards/_selection-cards/Tigerclaw__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |
| Poison marker | `extracted/text-assets/manifest-bundled__42.json`, element `1421887` / `PoisonMarker`; atlas `g713916_s799373.sprite17__31.jpg`, rectangle `(2492,0,708,714)` | 1 component definition | 708x714 | Yes - the circular bottle-and-skull face reads `POISON`; the two Green faces define its rule values |

## Tigerclaw deck composition

Nineteen printed faces make up p.6's eighteen-card deck plus the Handicap card that setup returns to the box. The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Blink Strike, Blend Into Shadows, Hit and Run, Dodge, Light-Fingered |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Cloak and Daggers |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6 step 11), used only for an odd player count (p.20) | Nimble Strike |

## Tigerclaw printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no
such icon. Card text applies only when the primary action is performed (p.9). A `+` after an Attack value is
p.14's convention marking a value the card's own text can increase. A Block prints `!` instead of a Defense
number and produces no numeric Defense value.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `tigerclaw` | 7379728 | Blink Strike | Gold `D` | basic | 13 | 1 | 1 | Basic Attack | Attack 3 | - | - |
| `tigerclaw19` | 7379735 | Nimble Strike | Gold `D` | `H` | 12 | 1 | 1 | Basic Attack | Attack 2 | - | - |
| `tigerclaw2` | 7379736 | Blend Into Shadows | Silver `S` | basic | 6 | - | 2 | Basic Skill | - | Radius 2 | - |
| `tigerclaw3` | 7379734 | Hit and Run | Red `R` | I | 9 | 3 | 4 | Attack | Attack 3 | - | - |
| `tigerclaw4` | 7379720 | Combat Reflexes | Red `R` | II | 9 | 3 | 4 | Attack | Attack 4 | - | Initiative |
| `tigerclaw5` | 7379732 | Backstab | Red `R` | II | 9 | 5 | 5 | Attack | Attack 5+ | - | Defense |
| `tigerclaw6` | 7379729 | Leaping Strike | Red `R` | III | 10 | 4 | 4 | Attack | Attack 4 | - | Radius |
| `tigerclaw7` | 7379725 | Backstab with a Ballista | Red `R` | III | 10 | 6 | 5 | Attack - Ranged | Attack 5+ | Range 1 | Defense |
| `tigerclaw8` | 7379730 | Dodge | Blue `B` | I | 10 | - | 3 | Defense | Block (`!`) | - | - |
| `tigerclaw9` | 7379719 | Sidestep | Blue `B` | II | 11 | - | 3 | Defense | Block (`!`) | - | Attack |
| `tigerclaw10` | 7379721 | Parry | Blue `B` | II | 11 | - | 3 | Defense | Block (`!`) | - | Defense |
| `tigerclaw11` | 7379724 | Evade | Blue `B` | III | 11 | - | 3 | Defense | Block (`!`) | - | Attack |
| `tigerclaw12` | 7379723 | Riposte | Blue `B` | III | 11 | - | 3 | Defense | Block (`!`) | - | Range |
| `tigerclaw13` | 7723257 | Light-Fingered | Green `G` | I | 2 | 1 | 3 | Skill | - | - | - |
| `tigerclaw14` | 7723258 | Pick Pocket | Green `G` | II | 2 | 1 | 3 | Skill | - | - | Attack |
| `tigerclaw15` | 7723259 | Poisoned Dagger | Green `G` | II | 2 | 1 | 3 | Skill - Ranged | - | Range 3 | Initiative |
| `tigerclaw16` | 7723260 | Master Thief | Green `G` | III | 1 | 2 | 3 | Skill | - | - | Movement |
| `tigerclaw17` | 7723261 | Poisoned Dart | Green `G` | III | 1 | 2 | 3 | Skill - Ranged | - | Range 3 | Initiative |
| `tigerclaw18` | 7379722 | Cloak and Daggers | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception: the two Gold cards and Silver card are basic and every other card
is non-basic. Tigerclaw's only basic Skill primary is Blend Into Shadows and his only basic Attack primary is
Blink Strike, or Nimble Strike when handicapped. p.9 goes further than the primary bands: **every action on
a basic card is a basic action**, which is the exact set Cloak and Daggers can repeat.

Every non-Ultimate face prints a Movement icon, so all eighteen can offer p.11's Fast Travel replacement;
Tigerclaw is the first verified hero with no basic/tiered exception to that reach. p.13's Clear replacement is
available on the seven cards with an Attack primary, six once Nimble Strike is boxed. Every card also offers
p.12's iconless Hold.

Tigerclaw's five Blue cards are all p.14 Blocks. They print no Defense number and no Defense secondary icon:
their text applies only when each is discarded from hand as its primary Defense reaction. Every Gold, Red and
Green card instead carries an ordinary secondary Defense value whose text is ignored when it defends.

## Tigerclaw exact rules text

Transcribed verbatim from the faces; printed line breaks are typographic and are joined with one space here.
**Bold** marks p.17's Active-effect keyword. Parenthesised italics are p.9 reminder text. Inline icons are
written as bracketed words.

| Card | Printed text |
| --- | --- |
| Blink Strike | Before the attack: Move 2 spaces in a straight line through an enemy unit; target that unit. *(If you cannot make this move, you cannot attack.)* |
| Nimble Strike | Before the attack: Move 2 spaces in a straight line through an enemy hero; target that hero. |
| Blend Into Shadows | If you are adjacent to terrain, place yourself into a space in radius; if you do, **Next turn:** You are immune to enemy attack actions. |
| Hit and Run | Target a unit adjacent to you. After the attack: You may move 1 space. |
| Combat Reflexes | Before the attack: You may move 1 space. Target a unit adjacent to you. After the attack: If you did not move before the attack, you may move 1 space. |
| Backstab | Target a unit adjacent to you; If a friendly unit is adjacent to the target, +2 [Attack] Attack. *(A "friendly unit" is another hero or a minion on your team.)* |
| Leaping Strike | Before the attack: You may move 1 space. Target a unit adjacent to you. After the attack: You may move 1 space. |
| Backstab with a Ballista | Target a unit in range; If a friendly unit is adjacent to the target +2 [Attack] Attack and the target cannot perform a primary action to defend. |
| Dodge | Block a ranged attack. |
| Sidestep | Block a ranged attack. You may move 1 space. |
| Parry | Block a non-ranged attack. The attacker discards a card, if able. |
| Evade | Block a ranged attack. You may move 1 space. You may retrieve your resolved or discarded basic skill card. |
| Riposte | Block a non-ranged attack. The attacker discards a card, or is defeated. |
| Light-Fingered | You may move 1 space. Take 1 coin from an enemy hero adjacent to you; if you do, you may move 1 space. |
| Pick Pocket | Move up to 2 spaces. Take 1 coin from an enemy hero adjacent to you; if you do, you may move 1 space. |
| Poisoned Dagger | Give a hero in range a [Poison] Poison marker. The hero with a poison marker has -1 [Initiative] Initiative, -1 [Attack] Attack and -1 [Defense] Defense. |
| Master Thief | Move up to 2 spaces. Take 1 or 2 coins from an enemy hero adjacent to you; if you do, you may move up to 2 spaces. |
| Poisoned Dart | Give a hero in range a [Poison] Poison marker. The hero with a poison marker has -2 [Initiative] Initiative, -2 [Attack] Attack and -2 [Defense] Defense. |
| Cloak and Daggers | After you perform a basic action you may repeat it once; if you repeat an attack action, you cannot target the same unit. |

Blend Into Shadows is Tigerclaw's only p.17 Active effect. Its **Next turn:** protection starts immediately
after the Place completes but applies on Tigerclaw's next turn, and p.8 cancels it during end-of-round card
retrieval if that turn never comes. Cloak and Daggers is p.16's permanent Ultimate source while Tigerclaw is
on the board. The Poison marker is persistent state rather than an Active effect: p.8 returns all markers to
the supply at end of round and p.13 also returns a defeated hero's markers.

Timing inside p.14's nine-step Attack/Defense order is printed. Blink Strike, Nimble Strike, Combat Reflexes
and Leaping Strike use step 1's **Before the attack:** seam; Hit and Run, Combat Reflexes and Leaping Strike
use step 7's **After the attack:** seam after any defeated target was removed at step 6. Backstab and
Backstab with a Ballista condition the Attack value itself, so their `5+` is evaluated for the Attack total.
All five Blocks resolve their condition and rider at defender step 4.

Riposte prints ledger question 13's mandatory "discards a card, or is defeated" construction. Parry supplies
the lower-Tier `if able` contrast in the same colour pair. Backstab with a Ballista restricts only a **primary**
Defense action; p.14's ordinary secondary Defense actions remain legal. A
[publicly indexed fan FAQ](https://www.cockeyedandcurious.com/goa2/faq) attributes
the same reading, the Blink Strike mandatory-step reading, and the p.8 Poison-marker cleanup reading to
designer Arty on Discord in 2025. Because that page is not the primary Discord record, it is corroboration
rather than independent authority under the ledger's no-unverified-forum-answer rule; the exact card and
rulebook text already settle all three outcomes.

## Tigerclaw upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by name. Each untaken card below becomes the p.16 item tucked
under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Hit and Run | Combat Reflexes (+1 Initiative) / Backstab (+1 Defense) | Leaping Strike (+1 Radius) / Backstab with a Ballista (+1 Defense) |
| Blue | Dodge | Sidestep (+1 Attack) / Parry (+1 Defense) | Evade (+1 Attack) / Riposte (+1 Range) |
| Green | Light-Fingered | Pick Pocket (+1 Attack) / Poisoned Dagger (+1 Initiative) | Master Thief (+1 Movement) / Poisoned Dart (+1 Initiative) |

Tigerclaw's twelve item icons cover all six p.16 icons - three Attack, three Defense, three Initiative, and one
each of Movement, Range and Radius - and therefore all four dashboard slots. Their reach is fixed by the
printed icons rather than by the item card's own action type:

| Item | Slot | Tigerclaw cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - the cards with an Attack primary, six once Nimble Strike is boxed |
| +1 Defense | 2 | 12 - both Gold cards, all five Red cards and all five Green cards; not the five Blocks, Silver, or Ultimate |
| +1 Initiative | 3 | 18 - all but Cloak and Daggers |
| +1 Movement | 4 | 18 - all but Cloak and Daggers |
| +1 Range | 4 | 3 - Backstab with a Ballista, Poisoned Dagger and Poisoned Dart |
| +1 Radius | 4 | 1 - Blend Into Shadows |

Cloak and Daggers prints no Range or Radius icon, so ledger question 12's Ultimate-item interpretation has no
Tigerclaw instance. Tigerclaw instead has the verified roster's widest Movement-item reach: every card the
normal six-player deck can put into hand carries Movement.

## Tigerclaw hero profile

| Field | Printed |
| --- | --- |
| Hero name | `Tigerclaw` on the profile card; **Tigerclaw the Cutpurse** on the dashboard and in setup p.5 and the lore quotation on p.2 |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Melee, Disabler, Farming |
| Stat profile | Attack 3 with five brighter conditional sections, Defense 2 with three brighter conditional sections, Initiative 8, and Movement 7 with one brighter conditional section, on p.21's 1-to-8 scale |
| Level track | the generic 1-8 track with p.13's Life cost 1/1/1/2/2/2/3/3, identical on every dashboard and not Tigerclaw-scoped data |

The broad conditional sections match the face inventory without becoming rules data: two Backstab cards add
Attack, five Blocks replace numerical Defense, movement appears throughout card text, and Cloak and Daggers
can repeat a basic action. p.21 calls these bars selection metadata; no rule consumes them.

## Tigerclaw source observations

- **Edition is confirmed without a wording caveat.** Printed p.15 reproduces Light-Fingered, Pick Pocket and Poisoned Dagger and uses them for its whole Upgrade example. Printed p.16 reproduces Pick Pocket and Poisoned Dagger again, plus Cloak and Daggers and the dashboard. All four unique faces and the dashboard match the extraction exactly in every visible name, colour, Tier, value, icon and word. Setup p.5 and lore p.2 also print `Tigerclaw the Cutpurse` exactly.
- The action backs divide into two hashes: Combat Reflexes, Sidestep, Parry and Cloak and Daggers share one decoded raster, while the other fifteen share another. Pixel comparison finds low-level colour differences across the raster, but direct inspection finds the same geometry and crossed-blades emblem, and p.5 asks only for the matching hero symbol. The affected Ultimate itself exactly matches p.16, so this is a non-blocking extraction/atlas variance and not evidence for different rules text or a different hero. No printing history is inferred from it.
- Tigerclaw is the first verified hero with a readable hero-owned marker component in the extraction. The manifest names element `1421887` `PoisonMarker`, and its atlas face is a poison bottle and skull labeled `POISON`. Poisoned Dagger and Poisoned Dart both print the same marker icon and define the attached modifier at -1 and -2 respectively. Because Level Up happens after p.8 returns all markers, the two Tier values cannot coexist from Tigerclaw's own deck across an upgrade.
- A Poison marker is a marker, not p.19's token obstacle. Tigerclaw correctly lacks p.22's `Tokens` role even though his two Green alternatives attach this marker. Ledger question 14 makes these faces the component's rule definition; the blank dashboard back hides no additional rule.
- **Every non-Ultimate card prints Movement.** The normal six-player deck therefore always has a Movement action and Fast Travel route on every card that can reach hand, while three Red cards and two Blue cards also put optional movement in their primary text.
- **Every Blue card is a Block**, split cleanly by attack subtype: Dodge, Sidestep and Evade block Ranged attacks; Parry and Riposte block non-Ranged attacks. The Tier II/III pairings also scale the rider: move 1 becomes move-and-retrieve on the Ranged branch, and `if able` becomes ledger question 13's defeat escalation on the non-Ranged branch.
- Backstab with a Ballista is the verified roster's first restriction on the **origin** of a Defense action: the target cannot use a primary action to defend, but secondary Defense remains. It is also Tigerclaw's only Ranged Attack and his only Attack with a Range icon.
- Blink Strike and Nimble Strike share one move-through-then-target shape but differ in target class: the ordinary Gold takes an enemy unit, the Handicap only an enemy hero. Blink alone prints the reminder that failure to make the mandatory move means the attack cannot continue; p.10 applies the same mandatory-step rule to both.
- Light-Fingered, Pick Pocket and Master Thief are the first verified cards that take coins from an enemy hero rather than gaining them from the supply. Their `if you do` movement depends on the transfer actually occurring. The Tier III offers `1 or 2` coins and two spaces on each movement arm.
- Poisoned Dagger and Poisoned Dart say **a hero**, not an enemy hero. By p.10 the target can be another friendly or enemy hero but not Tigerclaw himself, because an action affects its own hero only when the text says so. The marker modifies Initiative, Attack and Defense but no other value.
- Cloak and Daggers applies to every action p.9 classifies as basic, not merely the printed primary: actions on Gold and Silver are basic actions. p.10's Repeat rules make the repeated instance a separate action, forbid replacing it with Clear or Fast Travel, and forbid repeating a repeat; the Ultimate adds the different-unit target restriction only when the repeated action is an Attack.
- No published errata affecting a Tigerclaw face was located. The fan-maintained public FAQ's attributed designer answers are recorded above only where the local exact text already establishes the result.

## Tigerclaw open questions

No Tigerclaw-scoped rules or source question blocks definition work. The dashboard back is covered by ledger
question 14; the actual Poison marker definition and art are present; the two action-back rasters carry the
same readable symbol; and the second-printing rulebook exactly corroborates every Tigerclaw face it reproduces.
Roster-wide capability deltas and implementation sequencing belong to milestone 8.19 rather than this shard.
