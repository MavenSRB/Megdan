# Guards Digital - Xargatha Rules Shard

<!-- operative-claim:F-DOC-023 status:Unresolved -->

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Xargatha the Changed holds a Red roster slot on the pitch line-up milestone 8.17 approved. This shard owns
her content inventory, source-verification status, and any deviation or open question scoped to Xargatha
alone. A rule system that spans heroes stays in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger),
and a deviation or open question affecting more than one hero stays in
[RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. The rulebook refers to Xargatha as
"she"; this shard follows the source.

## Xargatha content ledger

The pitch uses Xargatha's complete progression. Its implementation-grade inventory contains:

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

Current status: **all nineteen faces are transcribed and capability-mapped; eighteen playable definitions are composed, dealt, and pass Xargatha's permanent player-path gate**. Production definitions, components, and tests exist and the Handicap remains setup-excluded. Milestone 8.25e5 independently reread all nineteen private fronts: twelve reconcile, while Cleave, Dangerous Slash, Stone Gaze, Petrifying Stare, Turn Into Statues, Constrict, and Final Embrace carry literal ledger and/or production-provenance differences. Typed rule behavior is unaffected, but five production transcriptions also make formatting notation player-visible. F-SOURCE-01 stays open through milestones 8.25e5b-8.25e5c; the hash-pinned findings are addressed from the [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825e5-independently-verify-the-six-pitch-heroes-114-private-fronts-completion-evidence).

## Xargatha source faces and components

Every action-card file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md`
describes; the internal names and element IDs below are that extraction's identity for a face, not this
project's. The ordinary standee is not part of the curated hard-link directory, so its manifest and atlas
locations are recorded separately. Tabletopia calls that object a `VerticalTokens` element, but its own
`tokenType` is `Standee`: it is the hero miniature representation, not a p.19 rules token.

| Face/component | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files/elements | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Xargatha/xargatha*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Xargatha/xargatha*__*__back.png` | 19 | 372x520 | Yes - all are byte-identical and carry the same Xargatha serpent-head emblem |
| Hero dashboard front | `active-hero-cards/_dashboards/Xargatha__front.png` | 1 | 850x445 | Yes - title, four turn slots, discard strip, level track and four-slot item legend all read |
| Hero dashboard back | `active-hero-cards/_dashboards/Xargatha__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder; [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions) makes the card faces the token/marker definition, and none names one |
| Hero profile card front | `active-hero-cards/_selection-cards/Xargatha__front.png` | 1 | 372x260 | Yes - name, complexity star, five role icons and all four stat bars resolve |
| Hero profile card back | `active-hero-cards/_selection-cards/Xargatha__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |
| Hero standee and portraits | `extracted/text-assets/manifest-bundled__42.json`, element `8597214` / public element `953670` / `Xargatha`; atlas `g713916_s799373.sprite1__30.jpg` | 1 ordinary standee definition, two team-outline standee sprites and two portraits | 515x479 standees; 302x302 portraits | Yes - the blue/orange variants depict the same Xargatha figure; no alternate form is defined |

## Xargatha deck composition

Nineteen printed faces make up p.6's eighteen-card deck plus the Handicap card that setup returns to the box.
The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and
two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Cleave, Siren's Call, Dangerous Slash, Stone Gaze, Charm |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Metamorphosis |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6 step 11), used only for an odd player count (p.20) | Desperate Cleave |

## Xargatha printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no
such icon. Card text applies only when the primary action is performed (p.9). A `+` after a printed value
signals that the card's own text can increase it; pp.13-14 illustrate the convention with Xargatha's Range
and with Attack respectively.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `xargatha` | 7679536 | Cleave | Gold `D` | basic | 11 | 2 | 1 | Basic Attack | Attack 4 | - | - |
| `xargatha19` | 7576067 | Desperate Cleave | Gold `D` | `H` | 10 | 1 | 1 | Basic Attack | Attack 3 | - | - |
| `xargatha2` | 7632831 | Siren's Call | Silver `S` | basic | 3 | - | 3 | Basic Skill - Ranged | - | Range 3 | - |
| `xargatha3` | 7679537 | Dangerous Slash | Red `R` | I | 7 | 6 | 5 | Attack | Attack 5+ | - | - |
| `xargatha4` | 7439587 | Deadly Swipe | Red `R` | II | 8 | 7 | 5 | Attack | Attack 5+ | - | Initiative |
| `xargatha5` | 7643642 | Long Thrust | Red `R` | II | 8 | 4 | 4 | Attack - Ranged | Attack 3 | Range 1+ | Defense |
| `xargatha6` | 7439588 | Lethal Spin | Red `R` | III | 8 | 7 | 5 | Attack | Attack 5+ | - | Radius |
| `xargatha7` | 7643643 | Rapid Thrusts | Red `R` | III | 8 | 4 | 4 | Attack - Ranged | Attack 4 | Range 1+ | Movement |
| `xargatha8` | 7679539 | Stone Gaze | Blue `B` | I | 9 | 5 | 3 | Skill | - | Radius 2 | - |
| `xargatha9` | 7717923 | Petrifying Stare | Blue `B` | II | 10 | 6 | 3 | Skill | - | Radius 3 | Defense |
| `xargatha10` | 7719750 | Fresh Converts | Blue `B` | II | 10 | 6 | 3 | Skill | - | - | Attack |
| `xargatha11` | 7717921 | Turn Into Statues | Blue `B` | III | 10 | 6 | 3 | Skill | - | Radius 4 | Defense |
| `xargatha12` | 7719751 | Devoted Followers | Blue `B` | III | 10 | 6 | 3 | Skill | - | - | Range |
| `xargatha13` | 7659041 | Charm | Green `G` | I | 5 | 3 | - | Movement | Movement 2 | Radius 2 | - |
| `xargatha14` | 7659042 | Control | Green `G` | II | 4 | 3 | - | Movement | Movement 2 | Radius 2 | Attack |
| `xargatha15` | 7659037 | Constrict | Green `G` | II | 4 | 3 | 2 | Skill | - | - | Initiative |
| `xargatha16` | 7659043 | Dominate | Green `G` | III | 4 | 4 | - | Movement | Movement 2 | Radius 2 | Attack |
| `xargatha17` | 7639363 | Final Embrace | Green `G` | III | 4 | 4 | 2 | Skill | - | - | Initiative |
| `xargatha18` | 7578825 | Metamorphosis | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

Supertype follows p.9 without exception: Cleave and Siren's Call are basic, as is Desperate Cleave when it
replaces the Gold card; every Tiered card is non-basic. Xargatha's only basic Skill action is Siren's Call's
primary and her only basic Attack is Cleave's, Desperate Cleave's when handicapped.

Every card also offers p.12's iconless Hold. Every one of the eighteen non-Ultimate cards prints a Movement
icon, so every one offers p.11's Fast Travel replacement; the three Green Movement primaries apply no card
text when replaced. p.13's Clear can replace only the seven Attack primaries, six in the normal deck.
Seventeen cards can be discarded to perform a Defense action - every non-Ultimate except Siren's Call.

**Xargatha prints no Defense primary and no Block.** Every Red card is an Attack, every Blue card is a Skill,
and the Green branch splits three Movement primaries from two Skills. Siren's Call is the only card in the
set with Movement but no Defense, and it is also her only Ranged Skill.

## Xargatha exact rules text

Transcribed verbatim from the faces; the printed line breaks are typographic and are joined with a single
space here. Parenthesised italics are p.9's reminder text. Where a face prints an inline icon inside a
sentence it is written here as the bracketed word the icon stands for.

| Card | Printed text |
| --- | --- |
| Cleave | Target a unit adjacent to you. After the attack: May repeat once on a different enemy hero. *(You may repeat even if the original target was a minion.)* |
| Desperate Cleave | Target a hero adjacent to you. After the attack: May repeat once on a different enemy hero. |
| Siren's Call | Target an enemy unit not adjacent to you and in range; if able, move the target up to 3 spaces to a space adjacent to you. |
| Dangerous Slash | Target a unit adjacent to you. +1 [Attack] Attack for each other enemy unit adjacent to you. *(Do not count the target when calculating the attack bonus.)* |
| Deadly Swipe | Target a unit adjacent to you. +2 [Attack] Attack for each other enemy unit adjacent to you. |
| Long Thrust | Target a unit in range; +1 [Range] Range for each enemy unit adjacent to you. |
| Lethal Spin | Target a unit adjacent to you. +3 [Attack] Attack for each other enemy unit adjacent to you. |
| Rapid Thrusts | Target a unit in range; +1 [Range] Range for each enemy unit adjacent to you. May repeat once on a different enemy hero. |
| Stone Gaze | **Next turn:** Enemy heroes in radius count as both heroes and terrain, and cannot perform movement actions. *(If you move, the radius "moves" with you.)* |
| Petrifying Stare | **Next turn:** Enemy heroes in radius count as both heroes and terrain, and cannot perform movement actions. |
| Fresh Converts | If you are adjacent to an enemy minion, you may retrieve a discarded card. |
| Turn Into Statues | **Next turn:** Enemy heroes in radius count as both heroes and terrain, and cannot perform movement actions. |
| Devoted Followers | If you are adjacent to an enemy unit, you may retrieve a discarded card. |
| Charm | You may move an enemy ranged minion in radius up to 2 spaces before or after movement. |
| Control | You may move an enemy ranged or melee minion in radius up to 2 spaces before or after movement. |
| Constrict | **End of round:** Defeat an enemy melee minion adjacent to you. *(Before the end of round minion battle.)* |
| Dominate | You may move an enemy minion in radius up to 2 spaces before or after movement; ignore heavy minion's immunity. |
| Final Embrace | **End of round:** Defeat an enemy melee or ranged minion adjacent to you. |
| Metamorphosis | Gain +1 [Movement] Movement and +1 [Initiative] Initiative for each enemy unit adjacent to you. You may move through obstacles. |

Stone Gaze, Petrifying Stare and Turn Into Statues are p.17 **Next turn:** Active effects with Radius 2, 3
and 4 respectively. They preserve both types their text names: affected enemy heroes remain heroes and also
count as terrain. The Radius is recalculated from Xargatha's current space, and the prohibition prevents a
Movement action from being performed rather than merely reducing its distance. p.11 draws the important
boundary: availability is checked when the Movement action would start, and a prohibited hero cannot start
one merely to apply primary text while moving zero spaces. It does not prohibit movement written inside
another card's text, and Swap is not movement.

Constrict and Final Embrace are **End of round:** Active effects. Constrict's reminder says their timing
plainly: before p.8's ordinary end-of-round Minion Battle. Because p.9 makes reminder text clarification
rather than a new rule, Final Embrace's identical keyword has the same timing even though its face omits the
reminder. Defeating the Battle Zone's last enemy minion there immediately runs p.8's Lane Push before the
ordinary Minion Battle continues.

Metamorphosis is p.16's permanent Ultimate source while Xargatha is on the board, not an ordinary
cancelable Active effect. Its two value contributions are recalculated from adjacent enemy-unit presence;
p.7 rechecks the highest Initiative after every card resolves, so changing adjacency can change the order
of Xargatha's unresolved card. Its Move-Through permission follows p.18: Xargatha may enter and leave
obstacle spaces while moving but may never end in one.

Cleave, Desperate Cleave and Rapid Thrusts use p.10's whole-action Repeat. Each repeated instance is a
separate action, cannot itself repeat, and cannot be replaced with Clear or Fast Travel. Cleave's first
target may be a minion, but its repeated target must be a different enemy hero. Rapid Thrusts recalculates
the enemy-adjacency Range contribution for its separate repeated instance: p.10's same Range rule and
restrictions are reapplied to the new board state. If the first attack removes an adjacent enemy minion,
the second instance may therefore lose the Range that minion supplied.

Dangerous Slash, Deadly Swipe and Lethal Spin calculate their Attack contribution from **other** adjacent
enemy units, excluding their target. Long Thrust and Rapid Thrusts instead count every adjacent enemy unit
to calculate Range. p.18 makes immunity irrelevant to a presence check, so an immune Heavy minion still
contributes to those counts and to Metamorphosis even when it cannot be targeted.

Siren's Call and the three Green Movement primaries use p.11 movement in card text, so the moved minion or
enemy unit follows path and obstacle rules and cannot Fast Travel. Charm, Control and Dominate expressly
let Xargatha choose whether that direct move happens before or after her own primary Movement, overriding
p.11's default before-Movement timing. Dominate is the only one that overrides Heavy minion immunity.

## Xargatha upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card. Each untaken card below becomes the p.16 item
tucked under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Dangerous Slash | Deadly Swipe (+1 Initiative) / Long Thrust (+1 Defense) | Lethal Spin (+1 Radius) / Rapid Thrusts (+1 Movement) |
| Blue | Stone Gaze | Petrifying Stare (+1 Defense) / Fresh Converts (+1 Attack) | Turn Into Statues (+1 Defense) / Devoted Followers (+1 Range) |
| Green | Charm | Control (+1 Attack) / Constrict (+1 Initiative) | Dominate (+1 Attack) / Final Embrace (+1 Initiative) |

Xargatha's twelve item icons cover all six p.16 icons - three Attack, three Defense, three Initiative, and
one each of Movement, Range and Radius - and therefore all four dashboard slots. Their reach is fixed by
the printed icons rather than by the item card's own action type:

| Item | Slot | Xargatha cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - both Gold cards and all five Red cards, six once Desperate Cleave is boxed |
| +1 Defense | 2 | 17 - every non-Ultimate card except Siren's Call, sixteen in the normal deck |
| +1 Initiative | 3 | 18 - every non-Ultimate card |
| +1 Movement | 4 | 18 - every non-Ultimate card |
| +1 Range | 4 | 3 - Siren's Call, Long Thrust and Rapid Thrusts |
| +1 Radius | 4 | 6 - the three petrification cards and the three Green Movement primaries |

Metamorphosis prints no card-value icon outside its rules text, so ledger question 12's approved
Range/Radius interpretation has no Xargatha instance. Its inline Movement and Initiative icons accompany
numbers in card text, which p.16 expressly excludes from item increases.

## Xargatha hero profile

| Field | Printed |
| --- | --- |
| Hero name | `Xargatha` on the profile card; **Xargatha the Changed** on the dashboard, in setup p.5 and on the publisher's component list |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Tactician, Pusher, Disabler, Durable, Sniper |
| Stat profile | Attack 5 with three brighter conditional sections, Defense 6, Initiative 3, and Movement 6, on p.21's 1-to-8 scale |
| Level track | the generic 1-8 track with p.13's Life cost 1/1/1/2/2/2/3/3, identical on every dashboard and not Xargatha-scoped data |

The Attack bar's conditional sections agree with the three adjacency-scaled melee Attacks without becoming
rules data. p.21 calls these bars selection metadata; no rule consumes them.

## Xargatha source observations

- **Edition is confirmed with no conflicting current-face wording.** Printed p.13 reproduces Long Thrust and matches the extracted face exactly in every visible name, colour, Tier, Initiative, secondary value, action/value, Range, item icon and word. The official publisher [second-printing development update](https://gamefound.com/en/projects/wolffdesigna/guardsofatlantis2new/updates/17) shows the then-tested Siren's Call rewrite, matching the extraction in substance and distinguishing it from the first-printing version without being treated as final errata. Setup p.5 and the publisher's [component list](https://wolffdesigna.com/products/guards-of-atlantis-ii-1) also print `Xargatha the Changed` exactly. No located published correction conflicts with a current face.
- All nineteen action backs are byte-identical, SHA-256 `28AB725A3DAFCD5983ADE8063292430782DEA5518B84F7347592C992013834CF`, and visibly carry the same serpent-head emblem. The dashboard back is the common blank Tabletopia placeholder; the profile back is the common logo face.
- **Xargatha owns no rules token or marker.** None of her action faces names, places, gives or targets one, and her profile carries none of p.22's `Tokens` role. Ledger question 14 therefore answers the blank dashboard back without a Xargatha-scoped question. Stone Gaze, Petrifying Stare and Turn Into Statues create state, not statue components; Metamorphosis creates no alternate figure.
- The manifest's one `Xargatha` `VerticalTokens` entry is an ordinary `Standee`, with blue and orange outline variants of the same figure. Its extraction label must not be confused with p.19's game-rule token category.
- A [publicly indexed fan FAQ](https://www.cockeyedandcurious.com/goa2/faq) attributes five relevant answers to designer Arty on Discord: a Stone Gaze subject cannot perform a Movement primary merely to apply its text at zero distance; Rapid Thrusts recalculates adjacency-derived Range between repeat instances; position changes recalculate Active effects; an immune Heavy still counts for adjacency; and a petrified hero may still Swap. The page explicitly calls itself unofficial and the primary Discord records are absent, so these answers are corroboration rather than independent authority under the ledger's no-unverified-forum-answer rule. The exact card and rulebook text already determine the recorded outcomes.
- The mechanically novel phrases - preserving two effective types at once, moving an enemy unit before or after one's own Movement, dynamic Range on a repeated Attack, end-of-round Defeat, and two adjacency-derived Ultimate values - are capability-mapping concerns for milestone 8.19, not transcription ambiguities.

## Xargatha open questions

No Xargatha-scoped source or rule question blocks the implemented definitions. The roster-wide official-errata search remains [ledger question 2](../RULES_LEDGER.md#open-source-and-rules-questions); her typed capability mapping and source-linked test matrix are complete.
