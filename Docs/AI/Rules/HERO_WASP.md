# Guards Digital - Wasp Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Hero-scoped rule authority lives one shard per approved hero, named `Rules/HERO_<NAME>.md`. A hero shard
owns that hero's content inventory, its mapping onto effect capabilities, its source-verification status,
and any deviation or open question scoped to that hero alone. Nothing else belongs here.

The boundary against the ledger is deliberate and runs the other way for anything shared. A rule system
that spans heroes stays in the ledger's [Rule-system ledger](../Rules/RULE_SYSTEMS.md#rule-system-ledger),
and a deviation or open question that affects more than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md)
rather than being copied into each hero's shard - a rule restated per hero is a rule that will disagree
with itself as soon as one copy is edited. A shard exists only for a hero with an approved roster slot;
the five heroes joining Wasp are selected at milestone 8.17, and each gets its shard then, not before.
## Wasp content ledger

The pitch requires Wasp's complete progression. Before verified Wasp definitions or effects are implemented, create an implementation-grade inventory containing at least:

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

Current status: **source-verified from the card faces at milestone 8.1 (2026-08-21)**. Everything below is read from the extracted faces named under [Wasp source faces](#wasp-source-faces) and cross-checked against the second-printing rulebook. The fields listed above that milestone 8.1 does not fill - stable definition, action-definition and effect-node IDs, content revision, capability ID/version, typed input/result bindings, operation policies, outcome facts, systemic checkpoints, reused and custom-handler IDs, and the required source-linked scenarios and interaction tests - are milestone 8.2's capability matrix and milestone 8.3's definitions. They are named here as owed rather than left implied. Other heroes remain outside the pitch inventory.

## Wasp source faces

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes; the internal names and element IDs below are that extraction's identity for a face, not this project's, and milestone 8.3 assigns the project's own IDs.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Wasp/wasp*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Wasp/wasp*__*__back.png` | 19 | 372x520 | Yes - all nineteen are byte-identical, and the deck emblem they carry is distinct from all thirty-one other heroes' backs and is the device Wasp's dashboard and profile-card art also show, which is what p.5's symbol-match step asks for |
| Hero dashboard front | `active-hero-cards/_dashboards/Wasp__front.png` | 1 | 850x445 | Partly - title, level track and stat bars read; the item-slot strip is cropped to its legend row |
| Hero dashboard back | `active-hero-cards/_dashboards/Wasp__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder shared by eleven heroes; see open question W-01 |
| Hero profile card front | `active-hero-cards/_selection-cards/Wasp__front.png` | 1 | 372x260 | Partly - name, complexity star and roles read; the stat bars do not resolve to exact values (W-02) |
| Hero profile card back | `active-hero-cards/_selection-cards/Wasp__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |

## Wasp deck composition

Nineteen printed faces make up p.5's eighteen-card deck plus the Handicap card that setup returns to the box. The colour sets have the standard shape: one Gold basic, one Silver basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.5 - the three Tier I cards plus the two untiered basic cards) | Magnetic Dagger, Static Barrier, Shock, Lift Up, Stop Projectiles |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and High Voltage |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.5), used only for an odd player count (p.20) | Crackling Dagger |

## Wasp printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no such icon, so that secondary action is unavailable from it. Every card additionally offers p.7's and p.12-13's iconless secondary actions - Hold, Fast Travel, and the Clear that replaces an Attack. Card text applies only when the primary action is performed (p.9).

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `wasp` | 7540381 | Magnetic Dagger | Gold `D` | basic | 12 | 2 | 1 | Basic Attack | Attack 3 | Radius 3 | - |
| `wasp19` | 7679535 | Crackling Dagger | Gold `D` | `H` | 11 | 1 | 1 | Basic Attack | Attack 2 | Radius 3 | - |
| `wasp2` | 7679533 | Static Barrier | Silver `S` | basic | 13 | 2 | - | Basic Skill | - | Radius 2 | - |
| `wasp3` | 7541335 | Shock | Red `R` | I | 8 | 6 | 4 | Attack | Attack 5 | Radius 2 | - |
| `wasp4` | 7541336 | Electrocute | Red `R` | II | 9 | 6 | 4 | Attack | Attack 5 | Radius 3 | Initiative |
| `wasp5` | 7541337 | Charged Boomerang | Red `R` | II | 9 | 3 | 4 | Attack - Ranged | Attack 3 | Range 3 | Defense |
| `wasp6` | 7541339 | Electroblast | Red `R` | III | 9 | 7 | 4 | Attack | Attack 6 | Radius 3 | Initiative |
| `wasp7` | 7598094 | Thunder Boomerang | Red `R` | III | 9 | 4 | 4 | Attack - Ranged | Attack 4 | Range 3 | Movement |
| `wasp8` | 7718510 | Lift Up | Blue `B` | I | 10 | 5 | 3 | Skill | - | Radius 2 | - |
| `wasp9` | 7718509 | Control Gravity | Blue `B` | II | 10 | 5 | 3 | Skill | - | Radius 3 | Defense |
| `wasp10` | 7718513 | Kinetic Repulse | Blue `B` | II | 10 | 5 | 3 | Skill | - | - | Attack |
| `wasp11` | 7718512 | Center of Mass | Blue `B` | III | 11 | 6 | 3 | Skill | - | Radius 3 | Defense |
| `wasp12` | 7718511 | Kinetic Blast | Blue `B` | III | 11 | 6 | 3 | Skill | - | - | Range |
| `wasp13` | 7718506 | Stop Projectiles | Green `G` | I | 3 | - | 2 | Defense - Ranged | Block (`!`) | - | - |
| `wasp14` | 7718505 | Deflect Projectiles | Green `G` | II | 3 | - | 2 | Defense - Ranged | Block (`!`) | Range 3 | Initiative |
| `wasp15` | 7718504 | Telekinesis | Green `G` | II | 3 | 3 | 2 | Skill - Ranged | - | Range 3 | Attack |
| `wasp16` | 7718507 | Reflect Projectiles | Green `G` | III | 2 | - | 2 | Defense - Ranged | Block (`!`) | Range 3 | Radius |
| `wasp17` | 7718508 | Mass Telekinesis | Green `G` | III | 2 | 4 | 2 | Skill - Ranged | - | Range 3 | Attack |
| `wasp18` | 7542128 | High Voltage | Purple `P` | IV | - | - | - | Ultimate | - | Radius 3 | - |

Supertype follows p.9 without exception here: the two Gold cards and the one Silver card are basic and every other card is non-basic, so **Wasp's only basic skill action is Static Barrier's primary** and her only basic attack is Magnetic Dagger's (Crackling Dagger's when handicapped). High Voltage's trigger reads that one action.

The three Green `!` cards are p.14's Block cards: the block succeeds regardless of the attack value when its condition is met and the hero is defeated when it is not, which is why they print no Defense number and no Defense secondary icon.

## Wasp exact rules text

Transcribed verbatim from the faces; the printed line breaks are typographic and are joined with a single space here. **Bold** marks p.17's Active-effect keyword as the card prints it. Parenthesised italics are p.9's reminder text.

| Card | Printed text |
| --- | --- |
| Magnetic Dagger | Target a unit adjacent to you. After the attack: **This turn:** Enemy units in radius cannot be swapped or placed by themselves, or by enemy heroes. |
| Crackling Dagger | Target a hero adjacent to you. After the attack: **This turn:** Enemy units in radius cannot be swapped or placed by themselves, or by enemy heroes. |
| Static Barrier | **This turn:** While an enemy hero outside of radius is performing an action, spaces in radius count as obstacles. While an enemy hero in radius is performing an action, spaces outside of radius count as obstacles. |
| Shock | Target a unit adjacent to you. After the attack: An enemy hero in radius and not adjacent to you discards a card, if able. |
| Electrocute | Target a unit adjacent to you. After the attack: An enemy hero in radius and not adjacent to you discards a card, if able. |
| Charged Boomerang | Target a unit in range and not in a straight line. *( Adjacent units are in a straight line as well. )* |
| Electroblast | Target a unit adjacent to you. After the attack: An enemy hero in radius and not adjacent to you discards a card, or is defeated. |
| Thunder Boomerang | Target a unit in range and not in a straight line. After the attack: If you targeted a hero may repeat once on a different target. |
| Lift Up | Move a unit, or a token, in radius 1 space, without moving it away from you or closer to you. May repeat once on the same target. |
| Control Gravity | Move a unit, or a token, in radius 1 space, without moving it away from you or closer to you. May repeat once on the same target. |
| Kinetic Repulse | Push up to 2 enemy units adjacent to you 3 spaces; if a pushed hero is stopped by an obstacle, that hero discards a card, if able. |
| Center of Mass | Move a unit, or a token, in radius 1 space, without moving it away from you or closer to you. May repeat up to two times on the same target. |
| Kinetic Blast | Push up to 2 enemy units adjacent to you 3 or 4 spaces; if a pushed hero is stopped by an obstacle, that hero discards a card, if able. |
| Stop Projectiles | Block a ranged attack. |
| Deflect Projectiles | Block a ranged attack; if you do, an enemy hero in range, other than the attacker, discards a card, if able. |
| Telekinesis | Place a unit or a token in range, which is not in a straight line, into a space adjacent to you. |
| Reflect Projectiles | Block a ranged attack; if you do, an enemy hero in range discards a card, if able. |
| Mass Telekinesis | Place a unit or a token in range, which is not in a straight line, into a space adjacent to you. May repeat once. |
| High Voltage | Each time after you perform a basic skill, you may defeat an enemy minion in radius; an enemy hero who was adjacent to that minion discards a card, if able. |

The three cards carrying a **This turn:** keyword are Active effects by p.17; High Voltage is p.16's continuous permanent ability rather than an Active effect; every other card resolves immediately.

## Wasp upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair below is the choice one Level Up offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Shock | Electrocute (+1 Initiative) / Charged Boomerang (+1 Defense) | Electroblast (+1 Initiative) / Thunder Boomerang (+1 Movement) |
| Blue | Lift Up | Control Gravity (+1 Defense) / Kinetic Repulse (+1 Attack) | Center of Mass (+1 Defense) / Kinetic Blast (+1 Range) |
| Green | Stop Projectiles | Deflect Projectiles (+1 Initiative) / Telekinesis (+1 Attack) | Reflect Projectiles (+1 Radius) / Mass Telekinesis (+1 Attack) |

Wasp's twelve item icons cover all six of p.16's icons, so they reach all four dashboard slots, the fourth of which takes Range, Movement and Radius together. Because p.16 gives an item no effect on a card that prints no matching icon, the reach of each item is fixed by the value table above:

| Item | Slot | Wasp cards carrying that icon |
| --- | --- | --- |
| +1 Attack | 1 | 7 - the cards with an Attack primary, six of them once Crackling Dagger is boxed |
| +1 Defense | 2 | 15 - all but the three Block cards and High Voltage |
| +1 Initiative | 3 | 18 - all but High Voltage |
| +1 Movement | 4 | 17 - all but Static Barrier and High Voltage |
| +1 Range | 4 | 6 - Charged Boomerang, Thunder Boomerang, Deflect Projectiles, Reflect Projectiles, Telekinesis, Mass Telekinesis |
| +1 Radius | 4 | 10 - both Daggers, Static Barrier, Shock, Electrocute, Electroblast, Lift Up, Control Gravity, Center of Mass, High Voltage |

## Wasp hero profile

| Field | Printed |
| --- | --- |
| Hero name | Wasp on the profile card; **Wasp the Warmaiden** on the dashboard |
| Complexity rating | one star - p.21's "Challenging" |
| Roles | Disabler, Tactician, Sniper |
| Stat profile | four bars on a 1-8 scale; approximately Attack 5, Defense 4 with a brighter conditional section, Initiative 6, Movement 4-5, not resolvable to exact values at this extraction's resolution (W-02) |
| Level track | the generic 1-8 track with the p.13 Life cost 1/1/1/2/2/2/3/3, identical on every hero's dashboard and therefore not Wasp-scoped data |

## Wasp source observations

- The second-printing rulebook illustrates four Wasp cards and three reproduce the extracted faces exactly: Static Barrier and Kinetic Repulse II on p.9, and Lift Up I on p.14, each matching name, colour letter, Tier, Initiative, secondary values, band text and card text.
- The fourth, Charged Boomerang II on p.9, matches in every value and in its card text but prints a different reminder text. The rulebook's text layer reads *( Units adjacent to you are in a straight line from you. )* where the card face reads *( Adjacent units are in a straight line as well. )*. The two say the same thing, and reminder text is clarification only by p.9, so nothing turns on it; it is recorded because it is the single wording difference located between the two sources. Card text outranks the rulebook under the ledger's [authority hierarchy](../RULES_LEDGER.md#authority-hierarchy), so the face's wording is the one this project transcribes.
- Thunder Boomerang III prints "If you targeted a hero may repeat once on a different target" with no comma after "hero". The transcription above keeps the printed punctuation.
- Electroblast III is Wasp's only card whose after-the-attack consequence is a discard **or a defeat** rather than a discard alone.
- No Wasp card places a token or a marker of Wasp's own. Five cards move or place "a unit, or a token" that is already on the board, which is a target-class question rather than a token-supply one.
- No published errata or official ruling affecting any Wasp face was located; none of the local sources contains one.

## Wasp open questions

Scoped to Wasp alone and therefore resident here. Anything broader stays in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions), including the p.16 question milestone 8.1 raised about whether an item's +1 reaches an Ultimate card's printed icon, which Wasp's Radius 3 High Voltage makes concrete but which is not Wasp-scoped.

- **W-01 - does Wasp use her own tokens or markers?** p.5 states that a hero's tokens and markers are displayed on the back of the hero dashboard, and Wasp's dashboard back in this extraction is a blank Tabletopia placeholder shared with ten other heroes. The nineteen card faces name no Wasp token or marker, so the working answer is none, and milestone 8.2 may proceed on it. Confirm against a readable dashboard back before any Wasp definition claims a token supply.
- **W-02 - Wasp's exact profile stat values.** The four bars on the 372x260 profile card do not resolve to exact pip counts. This is p.21 selection metadata that no rule consumes; it is recorded as approximate rather than guessed, and needs no answer before implementation.
- **W-03 - edition confirmation.** Every value and word on the nineteen faces is consistent with the second printing: the three exact rulebook matches above, the p.9 colour-letter and Tier conventions, the p.20 `H` Handicap card, and the p.16 Tier IV Ultimate. This answers the ledger's first open question for Wasp with one caveat, the p.9 reminder-text difference recorded above. It does not establish the edition of any other hero's faces in the same extraction.

