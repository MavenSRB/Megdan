# Guards Digital - Sabina Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Sabina holds a Blue roster slot on the pitch line-up milestone 8.17 approved. This shard owns her content
inventory, her source-verification status, and any deviation or open question scoped to Sabina alone. A
rule system that spans heroes stays in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a
deviation or open question affecting more than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md)
rather than being copied here. **Her epithet was in conflict between two local sources; the user answered
S-01 on 2026-08-25 by selecting the rulebook's `Sabina the Gunslinger`**, so that is her display label and
the dashboard's `SABINA THE COMMANDER` is recorded below as the source disagreement it is. The stable
content identity is `sabina` and no rule or capability branches on either epithet.

## Sabina content ledger

The pitch requires Sabina's complete progression. Before verified Sabina definitions or effects are implemented, the inventory must be implementation-grade, containing at least:

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

Current status: **source-verified at 8.18c, capability-mapped at 8.19, and composed, dealt and player-path gated at 8.22**; the display-name field was unblocked on 2026-08-25 when the user answered S-01 in favour of the rulebook's `Sabina the Gunslinger`. Every printed value, icon and word on the nineteen faces below is transcribed here, read twice - once from the whole face and again from icon-region and text-region enlargements built for the purpose. The roster-wide [capability matrix](ROSTER_CAPABILITY_MATRIX.md#sabina) now assigns every face/component, typed binding, policy, checkpoint, and source-linked scenario family without using the disputed epithet. Milestone 8.22 turned that inventory into verified content at revision `sabina-8-22-rev-1` - three files beside Wasp's, Arien's and Brogan's, one fewer than theirs because she prints no Active effect anywhere - and into a staged match that deals her the Blue seat at p.6's setup and at both p.15 stages. Every dealt face is played through the ordinary pipeline and proved on the permanent player-path gate; Bang is the one face no player reaches, because p.6 returns the Handicap card to the box.

## Sabina source faces

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes; the internal names and element IDs below are that extraction's identity for a face, not this project's.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Sabina/sabina*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Sabina/sabina*__*__back.png` | 19 | 372x520 | Yes - all nineteen are byte-identical, and the deck emblem they carry is distinct from all thirty-one other heroes' backs, which is what p.5's symbol-match step asks for |
| Hero dashboard front | `active-hero-cards/_dashboards/Sabina__front.png` | 1 | 850x445 | Yes - the four turn slots, the discard strip, the level track and the four-slot item legend all read. **The title reads `SABINA THE COMMANDER`, which the rulebook contradicts; see S-01** |
| Hero dashboard back | `active-hero-cards/_dashboards/Sabina__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder shared by ten other heroes; settled by [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions), not by this face |
| Hero profile card front | `active-hero-cards/_selection-cards/Sabina__front.png` | 1 | 372x260 | Partly - the name (`Sabina`, no epithet), complexity star and both role icons read; the Attack bar's base-versus-conditional split does not (S-03) |
| Hero profile card back | `active-hero-cards/_selection-cards/Sabina__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |

## Sabina deck composition

Nineteen printed faces make up p.6's eighteen-card deck plus the Handicap card that setup returns to the box. The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Point Blank Shot, Back to Back, Quickdraw, Listen Up, Troop Movement |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Big Sodding Gun |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6 step 11), used only for an odd player count (p.20) | Bang |

## Sabina printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no such icon, so that secondary action is unavailable from it. Card text applies only when the primary action is performed (p.9). A `+` after an Attack value is p.14's printed convention marking a value the card's own text can increase.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `sabina` | 7389127 | Point Blank Shot | Gold `D` | basic | 12 | 2 | 1 | Basic Attack - Ranged | Attack 2 | Range 1 | - |
| `sabina19` | 7389132 | Bang | Gold `D` | `H` | 11 | 1 | 1 | Basic Attack | Attack 1 | - | - |
| `sabina2` | 7389147 | Back to Back | Silver `S` | basic | 8 | 2 | - | Basic Skill | - | Radius 2 | - |
| `sabina3` | 7577220 | Quickdraw | Red `R` | I | 8 | 6 | 4 | Attack - Ranged | Attack 2+ | Range 2 | - |
| `sabina4` | 7577219 | Gunslinger | Red `R` | II | 9 | 6 | 4 | Attack - Ranged | Attack 3+ | Range 2 | Initiative |
| `sabina5` | 7685149 | Shootout | Red `R` | II | 9 | 6 | 4 | Attack - Ranged | Attack 3 | Range 2 | Defense |
| `sabina6` | 7577187 | Dead Shot | Red `R` | III | 9 | 7 | 4 | Attack - Ranged | Attack 3+ | Range 2 | Movement |
| `sabina7` | 7685151 | Bullet Hell | Red `R` | III | 9 | 7 | 4 | Attack - Ranged | Attack 3 | Range 2 | Radius |
| `sabina8` | 7596661 | Listen Up | Blue `B` | I | 9 | 3 | 3 | Skill | - | Radius 1 | - |
| `sabina9` | 7389138 | Roger Roger | Blue `B` | II | 10 | 4 | 3 | Skill | - | Radius 2 | Attack |
| `sabina10` | 7389144 | Steady Advance | Blue `B` | II | 10 | 4 | 3 | Skill | - | Radius 2 | Defense |
| `sabina11` | 7686417 | Ready and Waiting | Blue `B` | III | 10 | 4 | 3 | Skill | - | Radius 2 | Attack |
| `sabina12` | 7596660 | Unwavering Resolve | Blue `B` | III | 10 | 4 | 3 | Skill | - | Radius 2 | Defense |
| `sabina13` | 7596657 | Troop Movement | Green `G` | I | 4 | 2 | 2 | Skill | - | Radius 2 | - |
| `sabina14` | 7389128 | Marching Orders | Green `G` | II | 3 | 3 | 2 | Skill | - | Radius 3 | Attack |
| `sabina15` | 7389140 | Close Support | Green `G` | II | 3 | 3 | 2 | Skill | - | Radius 3 | Initiative |
| `sabina16` | 7596656 | Path to Victory | Green `G` | III | 3 | 3 | 2 | Skill | - | Radius 3 | Range |
| `sabina17` | 7596654 | Covering Fire | Green `G` | III | 3 | 3 | 2 | Skill | - | Radius 3 | Initiative |
| `sabina18` | 7389129 | Big Sodding Gun | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception here: the two Gold cards and the one Silver card are basic and every other card is non-basic, so **Sabina's only basic skill action is Back to Back's primary** and her only basic attack is Point Blank Shot's, or Bang's when handicapped.

Every card also offers p.7's iconless Hold. p.11's Fast Travel replaces *an entire Movement action*, so it is offered by the seventeen cards printing a Movement icon and by neither Back to Back nor Big Sodding Gun; p.13's Clear replaces *an entire Attack action*, so it is offered only by the seven cards whose primary is an Attack, Sabina printing no Attack secondary icon anywhere.

**Sabina prints no Defense primary, no Movement primary, no Block and no combined Defense/Skill card.** Every Red card is an Attack and every Blue and Green card is a Skill, with no exception in either direction. **Her Gold basic is `Basic Attack - Ranged` with a printed Range 1** - the first ranged basic attack in the verified inventory - while her Handicap card is a plain melee `Basic Attack` printing no Range icon at all, which is the asymmetry S-02 turns on.

## Sabina exact rules text

Transcribed verbatim from the faces; the printed line breaks are typographic and are joined with a single space here. Parenthesised italics are p.9's reminder text. Where a face prints an inline icon inside a sentence it is written here as the bracketed word the icon stands for.

| Card | Printed text |
| --- | --- |
| Point Blank Shot | Target a unit in range. After the attack: If the target is adjacent to you, push it 1 space. *(A unit pushed into an obstacle will stop. This is a valid push.)* |
| Bang | Target a hero adjacent to you. After the attack: Push the target 1 space. |
| Back to Back | Swap with a friendly minion in radius. |
| Quickdraw | Target a unit in range. +3 [Attack] Attack if the target played an attack card this turn. |
| Gunslinger | Target a unit in range. +3 [Attack] Attack if the target played an attack card this turn. |
| Shootout | Target a unit in range. After the attack: If the target was adjacent to you remove up to one enemy minion adjacent to you. *( You gain no coins for removing a minion, only for defeating. )* |
| Dead Shot | Target a unit in range. +4 [Attack] Attack if the target played an attack card this turn. |
| Bullet Hell | Target a unit in range. After the attack: If the target was adjacent to you remove up to two enemy minions adjacent to you. |
| Listen Up | Swap two minions in radius. |
| Roger Roger | Swap two minions in radius. |
| Steady Advance | If there are two or more friendly minions in radius you may retrieve a discarded card, then move up to 1 space. |
| Ready and Waiting | Swap two minions in radius, ignoring heavy minion immunity. |
| Unwavering Resolve | If there are two or more friendly minions in radius you may retrieve a discarded card, then move up to 2 spaces. |
| Troop Movement | Move a friendly minion in radius 1 space, to a space in radius. May repeat once. |
| Marching Orders | Move a friendly minion in radius 1 space, to a space in radius. May repeat once. |
| Close Support | An enemy hero in radius adjacent to your friendly minion discards a card, if able. |
| Path to Victory | Move a friendly minion in radius 1 space, to a space in radius. May repeat up to two times. |
| Covering Fire | An enemy hero in radius adjacent to your friendly minion discards a card, or is defeated. |
| Big Sodding Gun | Your basic attack has +2 [Range] Range and +2 [Attack] Attack. If you push an enemy hero, that hero discards a card, or is defeated. |

**No Sabina card carries a p.19 Active-effect keyword.** She is the first verified hero with neither a `This turn:` nor a `This round:` card - every non-Ultimate card resolves immediately, and Big Sodding Gun is p.16's permanent Ultimate ability.

Timing inside p.14's nine-step Attack/Defense order is printed rather than inferred. Point Blank Shot, Bang, Shootout and Bullet Hell print **After the attack:**, which is step 7 and therefore runs after the defending unit has already been removed at step 6. Quickdraw, Gunslinger and Dead Shot print no timing keyword: their clause is a conditional addition to the Attack value itself, which the printed `+` on the value marks, so it is read when the Attack Total is calculated rather than as a separate step.

## Sabina upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair below is the choice one Level Up offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Quickdraw | Gunslinger (+1 Initiative) / Shootout (+1 Defense) | Dead Shot (+1 Movement) / Bullet Hell (+1 Radius) |
| Blue | Listen Up | Roger Roger (+1 Attack) / Steady Advance (+1 Defense) | Ready and Waiting (+1 Attack) / Unwavering Resolve (+1 Defense) |
| Green | Troop Movement | Marching Orders (+1 Attack) / Close Support (+1 Initiative) | Path to Victory (+1 Range) / Covering Fire (+1 Initiative) |

Sabina's twelve item icons cover all six of p.16's icons - three Attack, three Defense, three Initiative, and one each of Movement, Range and Radius - so they reach all four dashboard slots, the fourth of which takes Range, Movement and Radius together. Because p.16 gives an item no effect on a card that prints no matching icon, the reach of each item is fixed by the value table above:

| Item | Slot | Sabina cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - the cards with an Attack primary, six of them once Bang is boxed |
| +1 Defense | 2 | 18 - every card but Big Sodding Gun, all as secondary values |
| +1 Initiative | 3 | 18 - all but Big Sodding Gun |
| +1 Movement | 4 | 17 - all but Back to Back and Big Sodding Gun |
| +1 Range | 4 | 6 - Point Blank Shot and all five Red cards printing a Range icon |
| +1 Radius | 4 | 11 - Back to Back and every Blue and Green card |

**Sabina has the widest Radius reach and the narrowest Attack reach in the verified roster so far**, which follows from her shape: eleven of her cards print a Radius icon because her whole Blue and Green kit is minion command, while only her seven Attack cards take the +1 Attack item. Her Ultimate prints neither a Range nor a Radius icon, so ledger question 12's approved interpretation - that an item's +1 reaches a matching printed icon on an Ultimate - has no Sabina instance, as it had none for Arien or Brogan.

## Sabina hero profile

| Field | Printed |
| --- | --- |
| Hero name | `Sabina` on the profile card, with no epithet; `SABINA THE COMMANDER` on the dashboard; **`Sabina the Gunslinger` in the rulebook's setup step 7** - the conflict recorded as S-01 and **answered in favour of the rulebook name on 2026-08-25** |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Tactician, Pusher - **two roles, the fewest of any verified hero** |
| Stat profile | Defense 3, Initiative 5, Movement 5 with one brighter section, on p.21's stated 1-to-8 scale. The Attack bar fills to approximately 7 but its base-versus-conditional split does not resolve (S-03) |
| Level track | the generic 1-8 track with the p.13 Life cost 1/1/1/2/2/2/3/3, identical on every hero's dashboard and therefore not Sabina-scoped data |

Three of the four bars resolve by the same pixel method that read Arien's and Brogan's. The Attack bar does not, and the reason is worth recording rather than forcing a number: p.21 states that "Brighter colored sections of the stat bars are conditional. They depend on your level-up choices and triggering specific card texts", and Sabina's Attack is exactly such a case - her printed Attack values are the lowest in the roster at 1 to 3, while three cards add +3 or +4 and the Ultimate adds +2, so much of that bar should be conditional. The rendered bar is uniformly pale rather than showing the crisp saturated-then-bright boundary her own Movement bar shows, so where the base ends cannot be read. This is p.21 selection metadata that no rule consumes.

## Sabina source observations

- **No Sabina action card is illustrated anywhere in the second-printing rulebook.** A whole-document text search for all nineteen card names returns nothing, and the only Sabina reference in the book is setup step 7 on p.5, naming her dashboard for the third Blue player in a six-player game. She is therefore the first verified hero whose faces cannot be checked against a rulebook reproduction, and her edition answer rests on convention alone; see S-04.
- **The dashboard and the rulebook give her different epithets**, and the mismatch is hers alone. The other five roster dashboards reproduce the rulebook's names exactly - `Tigerclaw the Cutpurse`, `Xargatha the Changed`, `Wasp the Warmaiden`, `Arien the Tidemaster`, `Brogan the Destroyer` - so this is not a systematic drift in the extraction. Recorded as S-01 and answered on 2026-08-25 in favour of the rulebook name; the mismatch itself remains an unexplained source disagreement.
- **Sabina is the first verified hero with no Active effect of any kind.** Wasp has three `This turn:` cards, Arien three plus a `This round:`, Brogan one plus three `This round:`; Sabina has none. Every card of hers resolves immediately.
- **Three cards print an increasable Attack value.** Quickdraw `2+`, Gunslinger `3+` and Dead Shot `3+` use the convention p.14 states beside its own worked example: "The `+` sign signifies that the value can be increased by the card text". These are the first printed `+` values in the verified inventory, and the increase is conditional on the target having played an attack card this turn.
- The condition those three read - "if the target played an attack card this turn" - is the same family as Brogan's Shield Bash and Counterattack, grounded the same way: p.9 makes a card with an Attack primary an Attack card, and p.10 makes a card played the moment it is placed facedown. Sabina applies it to the target of her own attack to raise a value; Brogan applies it to an adjacent hero to force a discard.
- **Shootout and Bullet Hell remove minions rather than defeating them**, and Shootout prints the consequence as reminder text: *( You gain no coins for removing a minion, only for defeating. )* p.19's Remove entry says the same in general terms - "Do not collect any coins" - so the reminder restates a keyword rather than creating an exception. Bullet Hell prints no reminder and removes up to two.
- **Ready and Waiting is the first verified card that overrides p.13's heavy minion immunity**, printing "ignoring heavy minion immunity" where its Tier I and Tier II siblings, Listen Up and Roger Roger, print the same swap without it. The three otherwise differ only in Radius, so the clause is the whole of what Tier III buys.
- Steady Advance and Unwavering Resolve retrieve a discarded card, gated on two or more friendly minions in radius. p.10 settles the ownership question without ambiguity: retrieving "means to take one of **your** cards back into your hand" and "You can never retrieve another hero's cards". They are the second and third verified retrieve cards, after Brogan's Bulwark.
- Covering Fire and Big Sodding Gun both print ledger question 13's "discards a card, or is defeated" construction, making Sabina the **fourth** verified hero to print it, after Wasp's Electroblast, Arien's three Red attacks and Brogan's Counterattack. Covering Fire's Tier II sibling Close Support prints `if able` in the same sentence position, which is the same Tier-scaled contrast Wasp's Shock and Electroblast print.
- Back to Back swaps Sabina herself with a friendly minion; Listen Up, Roger Roger and Ready and Waiting swap two minions with each other and do not involve her. p.19's Swap entry covers both shapes and states the swap "does not count as movement or placement".
- No Sabina card places, names or targets a token of her own, and her profile carries none of p.22's `Tokens` role, so [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions) answers the token and marker question for her without a per-hero question.
- No published errata or official ruling affecting any Sabina face was located; none of the local sources contains one.

## Sabina open questions

Scoped to Sabina alone and therefore resident here. Anything broader stays in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions). **S-01 is ANSWERED and no longer blocks the display-name field**; S-02 is deferred on a named trigger; S-03 and S-04 are recorded rather than answerable from the local sources and block nothing.

- **S-01 - which epithet is Sabina's? ANSWERED 2026-08-25 by user decision: `Sabina the Gunslinger`.** The extraction's dashboard front prints `SABINA THE COMMANDER`. The second-printing rulebook's setup step 7 on p.5 prints `Sabina the Gunslinger`, which is the name milestone 8.17's approved roster carries. The profile card prints `Sabina` alone and settles nothing. **Neither source was obviously wrong and the usual tie-breaker did not apply**: the ledger's [authority hierarchy](../RULES_LEDGER.md#authority-hierarchy) ranks a component face above the rulebook for *rules*, and an epithet is not a rule, so the hierarchy was silent here - which is why this needed a decision rather than a reading. Nothing else is in conflict - the deck emblem, the dashboard, the level track, the item legend and all nineteen faces agree that this is one hero, and the p.5 symbol-match step still works - so the roster and the inventory stood either way. The user selected the rulebook name when milestone 8.22 was authorized, on the ground that milestone 8.17's approved roster, the session brief and the roadmap already carry it, so no other document is renamed. Two facts stay recorded rather than resolved: `Gunslinger` is also the printed name of her Red Tier II card, so the display label and that card name coincide by coincidence and neither is derived from the other; and whether the dashboard's disagreement indicates a different printing is still unknown, which is part of why S-04 stays partial. **The decision governs the display label only.** The stable content identity remains `sabina`, and no rule, capability or test may branch on either epithet.
- **S-02 - does Big Sodding Gun's "+2 Range" reach the Handicap card? DEFERRED, trigger "first odd-player-count configuration".** The Ultimate reads "Your basic attack has +2 Range and +2 Attack". Sabina's Gold basic, Point Blank Shot, is `Basic Attack - Ranged` and prints a Range 1 icon, so the clause plainly makes it Range 3 and Attack 4. Her Handicap card, Bang, is a plain melee `Basic Attack` printing no Range icon and no `Ranged` in its band, and it replaces the Gold card only at an odd player count by p.20. Whether "+2 Range" gives Bang a Range where the face prints none - and if so whether that also makes it a Ranged attack for every rule that asks, including p.14's Block conditions - is unanswerable from the faces. p.16's rule that an item has no effect on a card printing no matching icon is suggestive but governs items, not card text, and the same page states that items never affect card text at all. The pitch is a six-player configuration in which Bang is returned to the box, so nothing is blocked; this is deferred on the same named trigger as Wasp's Crackling Dagger, and no definition may claim either reading before then.
- **S-03 - Sabina's exact Attack stat value.** The Attack bar on the 372x260 profile card fills to approximately 7 of p.21's 8, but unlike her Movement bar it shows no crisp boundary between the saturated base section and the brighter conditional one, so the base value cannot be read. Defense 3, Initiative 5 and Movement 5-plus-one-conditional all resolve. This is p.21 selection metadata that no rule consumes; it is recorded as unresolved rather than guessed, exactly as Wasp's W-02 was.
- **S-04 - edition confirmation. PARTIAL for Sabina, and weaker than for any other verified hero.** Every value and word on the nineteen faces is consistent with the second printing by convention: the p.9 colour-letter, Tier, supertype and Range-versus-Radius conventions, the p.14 `+` value convention, the p.20 `H` Handicap card and the p.16 Tier IV Ultimate all hold without exception. **But no Sabina card is reproduced anywhere in the rulebook**, so there is no exact-match evidence of the kind that answered the question for Wasp, Arien and Brogan, and the one component the rulebook does name disagrees with the extraction about her epithet (S-01). The ledger's first open question is therefore recorded as answered-by-convention for Sabina rather than answered outright, and this establishes the edition of no other hero's faces in the same extraction.
