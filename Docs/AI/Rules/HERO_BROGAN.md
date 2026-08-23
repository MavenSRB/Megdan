# Guards Digital - Brogan Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Brogan the Destroyer holds a Blue roster slot on the pitch line-up milestone 8.17 approved. This shard owns
his content inventory, his source-verification status, and any deviation or open question scoped to Brogan
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. The rulebook
refers to Brogan as "he"; this shard follows the source.

## Brogan content ledger

The pitch requires Brogan's complete progression. Before verified Brogan definitions or effects are implemented, the inventory must be implementation-grade, containing at least:

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

Current status: **source-verified at 8.18b (2026-08-23)**. Every printed value, icon and word on the
nineteen faces below is transcribed here, read twice - once from the whole face and again from
icon-region and text-region enlargements built for the purpose. Nothing is mapped onto a capability yet:
milestone 8.19 produces the roster-wide capability matrix and assigns the implementation slices, and the
fields above that name capability IDs, typed bindings, effect-node IDs and source-linked tests stay owed
until it does. No Brogan definition, effect, or test exists in the build.

## Brogan source faces

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes; the internal names and element IDs below are that extraction's identity for a face, not this project's.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Brogan/brogan*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Brogan/brogan*__*__back.png` | 19 | 372x520 | Yes - all nineteen are byte-identical, and the deck emblem they carry is distinct from all thirty-one other heroes' backs, which is what p.5's symbol-match step asks for |
| Hero dashboard front | `active-hero-cards/_dashboards/Brogan__front.png` | 1 | 850x445 | Yes - title, the four turn slots, the discard strip, the level track and the four-slot item legend all read |
| Hero dashboard back | `active-hero-cards/_dashboards/Brogan__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder shared by ten other heroes; settled by [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions), not by this face |
| Hero profile card front | `active-hero-cards/_selection-cards/Brogan__front.png` | 1 | 372x260 | Yes - name, complexity star, all four role icons and all four stat bars resolve |
| Hero profile card back | `active-hero-cards/_selection-cards/Brogan__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |

## Brogan deck composition

Nineteen printed faces make up p.6's eighteen-card deck plus the Handicap card that setup returns to the box. The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Onslaught, Bulwark, Mad Dash, Brutal Jab, Shield |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and One Man Army |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6 step 11), used only for an odd player count (p.20) | Leap Forward |

## Brogan printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no such icon, so that secondary action is unavailable from it. Card text applies only when the primary action is performed (p.9).

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `brogan` | 7379421 | Onslaught | Gold `D` | basic | 11 | 3 | 1 | Basic Attack | Attack 4 | - | - |
| `brogan19` | 7379430 | Leap Forward | Gold `D` | `H` | 10 | 3 | 1 | Basic Attack | Attack 3 | - | - |
| `brogan2` | 7600999 | Bulwark | Silver `S` | basic | 12 | 4 | - | Basic Skill | - | Radius 4 | - |
| `brogan3` | 7694715 | Mad Dash | Red `R` | I | 7 | 7 | 3 | Attack | Attack 6 | - | - |
| `brogan4` | 7694716 | Bullrush | Red `R` | II | 7 | 8 | 3 | Attack | Attack 6 | - | Initiative |
| `brogan5` | 7694717 | Throwing Axe | Red `R` | II | 7 | 7 | 4 | Attack - Ranged | Attack 3 | Range 2 | Defense |
| `brogan6` | 7379417 | Furious Charge | Red `R` | III | 8 | 8 | 3 | Attack | Attack 7 | - | Movement |
| `brogan7` | 7694822 | Throwing Spear | Red `R` | III | 8 | 7 | 4 | Attack - Ranged | Attack 4 | Range 2 | Radius |
| `brogan8` | 7718725 | Brutal Jab | Blue `B` | I | 8 | 5 | 2 | Skill | - | - | - |
| `brogan9` | 7694719 | Mighty Punch | Blue `B` | II | 9 | 6 | 2 | Skill | - | - | Initiative |
| `brogan10` | 7694712 | Shield Bash | Blue `B` | II | 9 | 6 | 2 | Skill | - | - | Attack |
| `brogan11` | 7718723 | Savage Kick | Blue `B` | III | 9 | 6 | 2 | Skill | - | - | Initiative |
| `brogan12` | 7718722 | Counterattack | Blue `B` | III | 9 | 6 | 2 | Skill | - | - | Range |
| `brogan13` | 7718726 | Shield | Green `G` | I | 6 | 4 | 2 | Skill | - | Radius 1 | - |
| `brogan14` | 7702707 | Bolster | Green `G` | II | 5 | 5 | 2 | Skill | - | Radius 2 | Attack |
| `brogan15` | 7451449 | War Drummer | Green `G` | II | 5 | 5 | 2 | Skill - Ranged | - | Range 4 | Defense |
| `brogan16` | 7718728 | Fortify | Green `G` | III | 5 | 5 | 2 | Skill | - | Radius 2 | Attack |
| `brogan17` | 7718729 | Master Skald | Green `G` | III | 5 | 5 | 2 | Skill - Ranged | - | Range 4 | Defense |
| `brogan18` | 8405023 | One Man Army | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception here: the two Gold cards and the one Silver card are basic and every other card is non-basic, so **Brogan's only basic skill action is Bulwark's primary** and his only basic attack is Onslaught's, or Leap Forward's when handicapped.

Every card also offers p.7's iconless Hold. The other two iconless secondary actions are conditional on what the card already prints: p.11's Fast Travel replaces *an entire Movement action*, so it is offered by the seventeen cards printing a Movement icon and by neither Bulwark nor One Man Army; p.13's Clear replaces *an entire Attack action*, so it is offered only by the seven cards whose primary is an Attack, Brogan printing no Attack secondary icon anywhere.

**Brogan prints no Defense primary at all**, and no Block or combined Defense/Skill card. Every one of his eighteen tiered and basic cards carries a secondary Defense value, so he always has something to discard in response to an attack, but no card of his ever applies text while defending - all of his text sits on Attack and Skill primaries. His Blue and Green colours are Skills throughout; only Red carries Attacks.

## Brogan exact rules text

Transcribed verbatim from the faces; the printed line breaks are typographic and are joined with a single space here. **Bold** marks p.17's Active-effect keyword as the card prints it. Parenthesised italics are p.9's reminder text. Bullet points are p.10's modal actions and are reproduced as printed.

| Card | Printed text |
| --- | --- |
| Onslaught | Target a unit adjacent to you. After the attack: Move into the space it occupied. |
| Leap Forward | Target a hero adjacent to you. After the attack: Move into the space it occupied. |
| Bulwark | You may retrieve a discarded card. **This turn:** You and friendly units in radius cannot be moved, pushed, swapped or placed by enemy heroes. |
| Mad Dash | Before the attack: Move 2 spaces in a straight line to a space adjacent to an enemy unit, then target that unit. *( If you cannot make this move, you cannot attack. )* |
| Bullrush | Before the attack: Move 2 or 3 spaces in a straight line to a space adjacent to an enemy unit, then target that unit. |
| Throwing Axe | **Choose one —** • Target a unit adjacent to you. • You may discard a card; if you do, target a unit in range. |
| Furious Charge | Before the attack: Move 2, 3, or 4 spaces in a straight line to a space adjacent to an enemy unit, then target that unit. |
| Throwing Spear | **Choose one —** • Target a unit adjacent to you. • You may discard a card. If you have a card in the discard, target a unit in range. |
| Brutal Jab | You may move 1 space. Push an enemy unit or a token adjacent to you up to 1 space. |
| Mighty Punch | Move up to 1 space. Push an enemy unit, or a token, adjacent to you up to 2 spaces. |
| Shield Bash | An enemy hero adjacent to you, who has played an attack card this turn discards a card, if able. |
| Savage Kick | Move up to 2 spaces. Push an enemy unit or a token adjacent to you up to 2 spaces. |
| Counterattack | An enemy hero adjacent to you who has played an attack card this turn discards a card, or is defeated. |
| Shield | **This round:** When any friendly minion in radius is defeated you may discard a silver card. If you do, the minion is not removed. *( The enemy hero still gains the coins for defeating the minion. )* |
| Bolster | **This round:** When any friendly minion in radius is defeated, you may discard a silver card; if you do, the minion is not removed. |
| War Drummer | A friendly hero in range gains 1 coin; if any hero was defeated this round, that friendly hero gains 3 coins, instead. |
| Fortify | **This round:** When any friendly minion in radius is defeated you may discard a basic card. If you do, the minion is not removed. |
| Master Skald | A friendly hero in range gains 2 coins; if any hero was defeated this round, that friendly hero gains 4 coins instead. |
| One Man Army | During minion battle you count as a heavy minion; if you would be removed, lose the push instead. |

Four cards carry an Active-effect keyword by p.19: Bulwark prints **This turn:** and Shield, Bolster and Fortify print **This round:**. One Man Army is p.16's permanent Ultimate ability rather than an Active effect. Every other card resolves immediately.

Timing inside p.14's nine-step Attack/Defense order is printed rather than inferred. Mad Dash, Bullrush and Furious Charge print **Before the attack:**, which is step 1 and is where their movement-then-target sentence resolves; Onslaught and Leap Forward print **After the attack:**, which is step 7 and therefore runs after the defending unit has already been removed at step 6, which is what makes "move into the space it occupied" reachable. Neither modal Ranged Attack carries a timing keyword: its chosen bullet is the targeting sentence itself and resolves as part of picking a target.

## Brogan upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair below is the choice one Level Up offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Mad Dash | Bullrush (+1 Initiative) / Throwing Axe (+1 Defense) | Furious Charge (+1 Movement) / Throwing Spear (+1 Radius) |
| Blue | Brutal Jab | Mighty Punch (+1 Initiative) / Shield Bash (+1 Attack) | Savage Kick (+1 Initiative) / Counterattack (+1 Range) |
| Green | Shield | Bolster (+1 Attack) / War Drummer (+1 Defense) | Fortify (+1 Attack) / Master Skald (+1 Defense) |

Brogan's twelve item icons cover all six of p.16's icons - three Attack, three Defense, three Initiative, and one each of Movement, Range and Radius - so they reach all four dashboard slots, the fourth of which takes Range, Movement and Radius together. Because p.16 gives an item no effect on a card that prints no matching icon, the reach of each item is fixed by the value table above:

| Item | Slot | Brogan cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - the cards with an Attack primary, six of them once Leap Forward is boxed |
| +1 Defense | 2 | 18 - every card but One Man Army, all as secondary values |
| +1 Initiative | 3 | 18 - all but One Man Army |
| +1 Movement | 4 | 17 - all but Bulwark and One Man Army |
| +1 Range | 4 | 4 - Throwing Axe, Throwing Spear, War Drummer, Master Skald |
| +1 Radius | 4 | 4 - Bulwark, Shield, Bolster, Fortify |

Brogan's Ultimate prints neither a Range nor a Radius icon, so ledger question 12's approved interpretation - that an item's +1 reaches a matching printed icon on an Ultimate - has no Brogan instance to apply to, as it had none for Arien.

## Brogan hero profile

| Field | Printed |
| --- | --- |
| Hero name | Brogan on the profile card; **Brogan the Destroyer** on the dashboard and in every rulebook reference |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Durable, Disabler, Pusher, Farming |
| Stat profile | Attack 7, Defense 8, Initiative 1, Movement 2 on p.21's stated 1-to-8 scale, with Movement carrying two brighter sections and the other three carrying none |
| Level track | the generic 1-8 track with the p.13 Life cost 1/1/1/2/2/2/3/3, identical on every hero's dashboard and therefore not Brogan-scoped data |

The stat profile is read, not estimated, by the same pixel method that resolved Arien's; Brogan's amber frame separates from the bar fill as cleanly as Arien's blue one. p.21 states the scale in words and states that "Brighter colored sections of the stat bars are conditional. They depend on your level-up choices and triggering specific card texts", which is what Brogan's two brighter Movement sections are: his Red Tier II and Tier III pairs are where a Movement 4 secondary and the +1 Movement item both come from. Defense 8 is the maximum the scale offers and is the only maximum in the verified roster so far. This is selection metadata that no rule consumes.

## Brogan source observations

- The second-printing rulebook illustrates **three** of Brogan's nineteen action-card faces and all three reproduce the extracted faces exactly: Bolster II and Onslaught on p.12, and Mad Dash I on p.13, each matching name, colour letter, Tier, Initiative, secondary values, band text, primary value, Radius icon where present and card text. Brogan additionally appears without a card illustration in the p.14 Attack/Defense worked examples, which corroborate Mad Dash's Attack 6 and its move-first requirement, and in p.5's setup step 7 as a Blue-team dashboard.
- **Brogan's Green trio prints the colour-versus-supertype distinction on the cards themselves, which is independent printed corroboration of the 2026-08-23 ruling recorded as Arien's A-03.** Shield I and Bolster II each say "you may discard **a silver card**"; Fortify III says "you may discard **a basic card**". Under the ruling - a card text naming a colour names that colour, while Basic spans Gold and Silver - the Tier III card is a genuine upgrade because it accepts either of Brogan's two Gold cards as well as his Silver one, and the three otherwise identical sentences would be indistinguishable under any reading that treated the two words as synonyms. The general rule is stated once in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger)'s card upgrades row.
- **Brogan is the first verified hero with modal card text.** Throwing Axe and Throwing Spear print p.10's bullet-point form under a bold "Choose one —" heading; p.10 resolves it as "choose a bullet point and apply its text, ignoring the rest". Their two bullets differ between Tiers in a way that matters: Throwing Axe's second bullet reads "You may discard a card; if you do, target a unit in range", so the range option costs a card, while Throwing Spear's reads "You may discard a card. If you have a card in the discard, target a unit in range", so a card already sitting in the discard pays for it and the discard itself becomes optional. Choosing a bullet whose condition cannot be met stops the action at that step by p.10's mandatory-step rule, which is the case [ledger question 11](../RULES_LEDGER.md#open-source-and-rules-questions) answered on 2026-08-16.
- Mad Dash prints the reminder text *( If you cannot make this move, you cannot attack. )* and its two higher-Tier siblings, Bullrush and Furious Charge, print none. Nothing turns on it: p.10's mandatory-step rule reaches the same result for all three, and reminder text is clarification only by p.9.
- Bolster is the only one of the three minion-protection cards that prints a comma after "defeated" and a semicolon before "if you do"; Shield and Fortify print neither and use a full stop. The transcription keeps each face's own punctuation. The rulebook's Bolster illustration on p.12 is word-identical to the face, and no punctuation claim is made about it, because that page's text layer interleaves the card's lines with the surrounding example and cannot be trusted for punctuation.
- Shield's reminder text - *( The enemy hero still gains the coins for defeating the minion. )* - is corroborated by the rulebook's own p.12 Bolster example, which notes that although the effect can prevent the removal, the attacking hero would still gain the coins for defeating the minion.
- **Bulwark restricts four activities where the only comparable verified card restricts two.** Wasp's Magnetic Dagger declares Swap and Place against enemy heroes; Bulwark declares moved, pushed, swapped and placed, and scopes the protection to Brogan and friendly units in radius rather than to enemies. Bulwark also carries a second, unrelated clause ahead of the keyword - "You may retrieve a discarded card" - which p.10 settles without ambiguity: retrieving "means to take one of **your** cards back into your hand" and "You can never retrieve another hero's cards", so the card is Brogan's own and no chooser question arises.
- Shield Bash and Counterattack condition on an adjacent enemy hero "who has played an attack card this turn". p.9 makes a card with an Attack primary an Attack card, and p.10 makes a card played the moment it is placed facedown, so the condition reads the identity of the card that hero played this turn and does not require them to have performed the Attack action, nor to have resolved it yet. Counterattack prints the "discards a card, or is defeated" construction that [ledger question 13](../RULES_LEDGER.md#open-source-and-rules-questions) answered from source, making Brogan the third verified hero to print it after Wasp's Electroblast and Arien's three Red attacks.
- War Drummer and Master Skald are the first verified cards that grant coins, and the first that condition on a round-scoped history - "if any hero was defeated this round" - rather than on board state. They give the coins to a friendly hero in range; p.10's Picking Targets note that an action does not affect Brogan himself unless the text says so is what decides whether he can name himself, and the text does not say so.
- No Brogan card places, names or targets a token of Brogan's own. Three cards push "an enemy unit or a token" that is already on the board, which is a target-class question rather than a token-supply one.
- No published errata or official ruling affecting any Brogan face was located; none of the local sources contains one.

## Brogan open questions

Scoped to Brogan alone and therefore resident here. Anything broader stays in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions). All three were raised at 8.18b and all three are answered, B-02 by explicit user decision on 2026-08-23, so nothing in this set blocks Brogan's capability mapping or implementation. The wider question B-02 opened - whether a counts-as-minion hero carrying no clause of his own should inherit Brogan's answer - was answered the same day and is recorded at [ledger question 7](../RULES_LEDGER.md#open-source-and-rules-questions): it does not arise, because such a hero states the consequence on his own card.

- **B-01 - does Brogan use his own tokens or markers? Answered: no.** His dashboard back is the same blank Tabletopia placeholder every hero in this extraction has, and [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions) settled the general question on 2026-08-23: a hero's tokens and markers are defined by that hero's card faces, so a set whose faces name none has none and the blank back is missing art rather than missing rules. Brogan's faces name none, and his profile card carries none of p.22's `Tokens` role icon. No Brogan definition claims a token or marker supply.
- **B-02 - what does One Man Army's "lose the push instead" mean? ANSWERED on 2026-08-23 by explicit user decision: exactly as though Brogan's team had lost its last minion.** The Ultimate reads "During minion battle you count as a heavy minion; if you would be removed, lose the push instead." The first clause is well grounded: p.8's Minion Battle counts minions in the Battle Zone and requires the team with fewer to remove the difference, Heavy last, so Brogan adds to his team's count and, being heavy, is only ever reached after every other friendly minion in the Battle Zone has already gone. The second clause needed the ruling, because p.8 pushes the Lane when a team "loses their last minion" and the team with minions remaining wins that push - so an unremoved Brogan still counts as one, and p.8 alone would trigger no push at all. **The approved reading: the card substitutes the push for the removal, and that push is p.8's ordinary Lane Push run in full** - a Wave counter is flipped face-down and the game ends immediately if it was the last one, the next Zone towards Brogan's team's Throne becomes the new Battle Zone with the same Throne-Zone victory rule, and every minion in the previous Battle Zone is removed and respawned at the matching Spawn points in the new one. **Brogan himself is not removed**, which is the whole of what the clause buys him: he stays on the space he occupied, now outside the Battle Zone, and the counts-as clause is scoped to Minion Battle by its own first word, so the push's remove-all-minions step does not reach him. It is an approved interpretation rather than sourced text; a later official ruling supersedes it. **This card is also the published source text that [ledger question 7](../RULES_LEDGER.md#open-source-and-rules-questions) recorded as missing.** That question was retired for the current synthetic slice on 2026-08-16 on an approved interpretation, noting that the Brogan wording establishes that a modified minion identity participates in Minion Battle but that no located official source defined the removal consequences for a hero in that set. Brogan's own Ultimate defines them for Brogan, and defines them differently from the interpretation: he is never removed at all. Whether that changes the retired question's disposition for a hero *without* such a clause is a ledger decision, not a Brogan one.
- **B-03 - edition confirmation. Answered for Brogan.** Every value and word on the nineteen faces is consistent with the second printing: the three exact rulebook reproductions above, the p.9 colour-letter and Tier conventions, the p.20 `H` Handicap card and the p.16 Tier IV Ultimate. **Unlike Wasp and Arien, no wording difference of any kind was located between the illustrated faces and the rulebook**, so Brogan's edition answer carries no caveat. This answers the ledger's first open question for Brogan and establishes the edition of no other hero's faces in the same extraction.
