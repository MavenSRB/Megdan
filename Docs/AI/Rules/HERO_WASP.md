# Guards Digital - Wasp Rules Shard

<!-- operative-claim:F-DOC-022 status:CurrentDemonstrated -->

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

The pitch uses Wasp's complete progression. Its implementation-grade inventory contains:

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

Current status: **source-verified at 8.1, capability-mapped at 8.2, defined at 8.3, dealt at 8.4, every configured face composed through High Voltage at 8.13, her whole p.15/p.16 progression run at 8.14, every composed behavior verified through the real player pipeline at 8.15 - live HUD controls and board pointer clicks at each p.15 stage - and the deterministic mirror integration gate closed at 8.16, where the mirror plays itself from p.5's setup to each of the rulebook's three endings; only the uninstantiated Handicap face retains its named deferral**. Everything below is read from the extracted faces named under [Wasp source faces](#wasp-source-faces) and cross-checked against the second-printing rulebook; the [capability matrix](#wasp-capability-matrix) maps every action onto the implemented contracts and names each delta with its owning milestone.

The stable definitions and typed bindings live in `WaspEffectContent`; basic-card Active effects remain in `WaspActiveEffectContent`, and the Ultimate is a continuous triggered source. Wasp's eighteen playable definitions are one of six distinct configured sets and pass the permanent player-path gate. Milestone 8.25e5 independently reread all nineteen private fronts and reconciled every field without a Wasp discrepancy; milestone 8.25e5g confirmed the 114-front manifest byte-identical, so those Wasp results carry forward and F-SOURCE-01 is closed. A later higher-authority source still supersedes that result.

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

## Wasp capability matrix

Produced by milestone 8.2 (2026-08-21) against the implemented contracts in [CARD_EFFECT_CAPABILITIES.md](../CARD_EFFECT_CAPABILITIES.md) and its shards. Every secondary Defense and Movement value, Hold, Fast Travel, and Clear rides the implemented general contracts unchanged and is not repeated per row. Choice ownership follows p.10 throughout: the actor makes every choice in their own card's text, a hero told to discard chooses the card from their own hand, and `if able` is honored at the node. The milestone column is where that row's composition lands; the general seams 8.5, 8.7, and 8.12 are proven on synthetic definitions first, exactly as 6.4-6.8 were. Every row's composing milestone carries that row's focused fixtures, milestone 8.15's journeys drive them through the live HUD, and milestone 8.16's `WaspMirrorIntegrationGateTests` plays the whole set together: matches dealt from these definitions run from p.5's setup to each of the rulebook's three endings by answering whatever the projection is waiting on, so no row's evidence rests on a fixture naming the commands that reach it.

| Card / action | Composes (implemented) | New or refined (owner) |
| --- | --- | --- |
| Magnetic Dagger - Basic Attack | Printed Attack/combat contracts; Active-effect lifecycle (`This turn`); `RuleModification` restriction contract; `RadiusQuery` | Attack text seam (8.5); actor-qualified Swap/Place restriction consumption built at 8.7. **Composed at 8.9.** One node creating an Active effect scoped `UnitsInRadius` opposing the source at the printed Radius icon, declaring Swap and Place against `AffectedObjectItself` and `HeroesOpposingSource` |
| Crackling Dagger - Basic Attack | Same as Magnetic Dagger plus a hero-only target policy (8.5's declared narrowing) | Behavior deferred, named trigger "first odd-player-count configuration" (2026-08-21 decision); data ships at 8.3 |
| Static Barrier - Basic Skill | Skill effect frame (exists); Active-effect lifecycle; the one obstacle query | Conditional space-obstacle overlay declaration and consumption built at 8.7 - both arms, radius anchored to the source hero's current space, consumed by movement, push and place through the one obstacle query. **Composed at 8.9.** One node creating an Active effect whose overlay declares both printed arms at the printed Radius icon, conditioned on heroes opposing the source |
| Shock / Electrocute - Attack | Combat contracts; `RadiusQuery` targeting/presence separation (7.2b); discard machinery (7.1a: non-actor chooser, `if able`, candidates re-derived) | **Composed at 8.10, on one shared definition** - the two print the same sentence and differ only in the Radius icon the selection reads off the card. `synthetic-radius-hero-target` (p.9's disk without its innermost ring, on the side the card names) feeding 7.1a's discard at `IfAble`. It asks two questions inside p.14's step 7 and is the first card text in this build to stop an Attack at all; the selection deliberately declares no consuming node, because narrowing the offer to heroes holding a card would make the printed `if able` unreachable |
| Electroblast - Attack | As Shock, plus the staged p.13 Defeat chain (6.6b/5.5/7.4) | **Composed at 8.10.** The same rider with one conditional arm added: `DiscardChosenCardV2` reports whether the hero had a card, and `synthetic-defeat-hero` runs p.13's list through the Attack's own resolver where they did not. It is a branch and not a question - W-05 was answered from source at 8.9 as ledger question 13 - so nobody is asked anything the two lower-Tier printings do not also ask |
| Charged Boomerang - Attack, Ranged | Ranged combat values; `RangeQuery` | **Composed at 8.10**, and the one composed Wasp Attack with no rider at all: its whole printed text is the targeting sentence, declared as `AttackTargetLine.NotInStraightLine` on the action's own `PrintedAttackTargetPolicy`, so it carries no effect root and owes nothing |
| Thunder Boomerang - Attack, Ranged | As Charged Boomerang, plus `RepeatAction` (6.7) and the dispatcher's repeated-instance start | **Composed at 8.10.** Three printed clauses, three declarations: `synthetic-attack-took-a-hero` reads the enclosing Attack's own target, `May` is p.10's confirm-or-decline, and `RepeatTargetBinding.DifferentTarget` at maximum 1 excludes the unit the first instance took. The repeated instance is an ordinary Attack - its own target choice, its own Defense response, its own consequences - started through a new `StartRepeatedAttack` that rebuilds the offer from the finished instance's identity, because by then the card has been played and no offer query would admit it |
| Lift Up / Control Gravity - Skill | Token-widened Direct Move (6.8k); `RadiusQuery`; `RepeatAction` | **Composed at 8.6.** `DirectMoveDestinationPolicy.EquidistantFromActor` and a `SameTarget` repeat bound to the printed-Radius selection |
| Center of Mass - Skill | As Lift Up | **Composed at 8.6.** `MaximumRepeats` 2 - the first verified repeat count above one - discharging 6.7's deferred explicit-maximum and repeat-ancestry triggers under the schema v22 repeat continuation |
| Kinetic Repulse / Kinetic Blast - Skill | 6.4 batch Push exactly as p.18 prints it: up to two targets, one chosen distance (fixed 3 / chosen 3-or-4), one pre-effect state, farthest-first simultaneous commit; 7.1a discard machinery | **Composed at 8.8.** Both Push declarations at revision 3 carrying the printed least distance, and the batch also the count bound and p.19's first relationship arm (`SourceTeamRelation` over a target set); per-hero stop-cause facts read by one 8.5 `Conditional` per pushed hero; W-04 resolved on its proposed reading |
| Telekinesis / Mass Telekinesis - Skill, Ranged | `PlaceOperation`; `RepeatAction` (Mass, unbound - the face says only `May repeat once`) | **Composed at 8.6.** `synthetic-range-target` reading the printed Range icon with p.19's straight-line complement, feeding the Place capability's revision 2 under `PlaceEligiblePieceKind.UnitOrToken` and `PlaceDestinationAnchor.ActingHero` |
| Stop / Deflect / Reflect Projectiles - Defense, Ranged | Defense reaction flow (2.3-2.5/5.8); the reserved `DefenseBlockResult` seam (5.8); attack subtype facts; discard machinery | **Composed at 8.11**, all three on one shared Block shape. `DefenseBlockResult` gains `ConditionUnfulfilled` beside `Fulfilled` and `None`, evaluated at p.14 step 4 through `synthetic-attack-is-ranged` reading the Attack's own typed subtype facts: fulfilled defends whatever the totals and skips step 5 entirely, unfulfilled defeats the defender through 8.10's `synthetic-defeat-hero`. No Block ever produces a Defense number. Deflect and Reflect share one rider factory over the new `synthetic-range-hero-target` channel, differing only in its `excludeTheAttacker` parameter, resolved from `EnclosingAttackFacts.ActorId`. The Block keeps its `PrintedPrimary` origin per 8.3; the offer window and the invocation context widened instead. **Schema v24** |
| High Voltage - Ultimate | 7.3 continuous Ultimate source; staged Defeat chain; `RadiusQuery`; discard; Heavy-immunity targeting | **Composed at 8.13.** `AfterControllerPerformsBasicSkill` consumes 8.9's completed basic-Skill fact at 8.12's Action-Ended reader only while Wasp is on board. `May` offers one enemy minion in printed Radius, `synthetic-defeat-minion` requests shared reward/removal/Lane-Push stages, and `synthetic-hero-adjacent-to-minion` chooses against pre-removal occupancy before the chosen enemy hero's `if able` discard. Unlock remains 8.14. |
| Upgrade pairs, items, unlock | 7.5 leveling transition; 7.2 item slots and bonus composition; 7.1 card-state transitions | **Run at 8.14** over the printed deck: p.15's six verified pairs through the 7.5 transition, six items covering all six p.16 icons across all four slots, and the seventh level unlocking High Voltage where an ordinary match then reads it. p.16's item step still reads the set-aside location, whose single writer is fenced by a conformance rule; F-PROG-08's scoping was withdrawn by the 2026-08-23 amendment as unreachable and mis-scoped. Question 12's interpretation is applied - an item's +1 reaches the Ultimate's printed icon, so a +1 Radius item makes High Voltage Radius 4 |

**Delta register.** Genuinely new reusable pieces, each landing as a versioned capability or declared policy rather than a card-name branch, with **no custom handler anywhere in Wasp's set**: the non-Skill text seam with declared target policies (8.5, F-CARD-01/F-BOARD-03/F-CARD-07); actor-qualified restrictions with p.19's Swap and p.18's Place named as activities, the conditional obstacle overlay bound to its hero's current space, and the consumption conformance rule (all built at 8.7, F-CARD-03/F-BOARD-04); straight-line geometry, the orbit destination policy, the actor-anchored Place declaration with its unit-or-token kind, and the bounded repeat maximum with its same-target piece binding (all built at 8.6); the printed least distance beside the declared one, the batch's count bound and enemy-side target relation, the per-hero push stop-cause facts, and the capability that asks two questions (all built at 8.8, W-04); effect-invoked staged Defeat with the discard-else-defeated escalation (8.10); Block evaluation with defender-anchored riders (8.11); trigger-window execution (8.12); and the was-adjacent snapshot (8.13). Everything else is reuse: the batch-push, discard, place, direct-move, radius, repeat, Active-effect, Ultimate-source, item, and leveling contracts take Wasp's text as declarations.

**Timing and state.** The three `This turn` cards ride the Active-effect lifecycle with p.17's cancellation set and radius anchored to the hero's current space; High Voltage is p.16's continuous source. Its questions reuse the Skill-shaped effect continuation across restore, so 8.13 adds no pending kind and schema remains v24. Trusted capability/triggered registration and content digests changed and were deliberately re-pinned.

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

Scoped to Wasp alone and therefore resident here. Anything broader stays in [RULES_LEDGER.md](../RULES_LEDGER.md#open-source-and-rules-questions), including the p.16 Ultimate-item question milestone 8.1 raised, which the user answered on 2026-08-21 as ledger question 12's approved interpretation: an item's +1 reaches a matching printed icon on the Ultimate.

- **W-01 - does Wasp use her own tokens or markers? ANSWERED on 2026-08-23 by explicit user decision: no, and the question is no longer Wasp-scoped.** p.6's setup step 11 states that a hero's tokens and markers are displayed on the back of the hero dashboard, and Wasp's dashboard back in this extraction is a blank Tabletopia placeholder shared with ten other heroes. The nineteen card faces name no Wasp token or marker, so the working answer was none, and milestone 8.2's capability matrix proceeded on it - it maps no token supply. That working answer is now the answer: the ruling is that **a hero's tokens and markers are defined by that hero's card faces**, so a set whose faces name none has none and the blank back is missing art rather than missing rules. The general rule is [ledger question 14](../RULES_LEDGER.md#open-source-and-rules-questions), because it settles the identical question for every hero rather than for Wasp alone; Arien's A-01 is answered from the same decision. Nothing in Wasp's composed set needs revisiting.
- **W-02 - Wasp's exact profile stat values.** The four bars on the 372x260 profile card do not resolve to exact pip counts. This is p.21 selection metadata that no rule consumes; it is recorded as approximate rather than guessed, and needs no answer before implementation.
- **W-03 - edition confirmation.** Every value and word on the nineteen faces is consistent with the second printing: the three exact rulebook matches above, the p.9 colour-letter and Tier conventions, the p.20 `H` Handicap card, and the p.16 Tier IV Ultimate. This answers the ledger's first open question for Wasp with one caveat, the p.9 reminder-text difference recorded above. It does not establish the edition of any other hero's faces in the same extraction.
- **W-04 - is a push stopped at the board edge "stopped by an obstacle"? ANSWERED at 8.8 on the proposed reading: yes.** p.19's Terrain keyword makes the water surrounding the play area terrain, and p.18 lists terrain among the obstacles, so a push stopped at the edge stopped against something and Kinetic Repulse's and Kinetic Blast's rider applies. The reconciliation the milestone owed turned out to have two halves, and neither needed the model changed. **First, the official map already says it:** every space adjacent to a playable space of `official-single-lane` is itself playable or declared terrain, so on the board this project actually plays, a pushed unit meets the modelled water before it can leave the map and the ordinary obstacle rule answers the card. `TerrainObstacleTests.EverySpaceBesideAPlayableOne_IsPlayableOrDeclaredTerrain` holds the map to it. **Second, past that ring the classification does answer "nothing"**, deliberately, because milestone 6.8e separated off-map from terrain so that no Move-Through permission could carry a unit into the water; so the card's own reading is stated once, on `PushPathResult.StoppedByAnObstacle`, and admits both `BlockedByObstacle` and `LeftBoard`. The classification keeps its three answers for the permission, and the printed sentence never depends on how far a map's terrain ring was drawn. A later official ruling supersedes the reading, not the model.
- **W-05 - what does "discards a card, or is defeated" require? ANSWERED on 2026-08-22 at milestone 8.9, and no longer Wasp-scoped.** Electroblast's rider. The conclusion 8.2 proposed stands - the chosen hero must discard if they hold any card, and only a hero unable to discard is defeated - but the reasoning it recorded does not, and the question has moved. **What 8.2 recorded was the bare contrast with the `if able` its two lower-Tier siblings print, and that contrast does not discriminate**: it is equally explained by a choice reading, under which `if able` is needed on Shock because a lone instruction can be impossible and Electroblast needs none because its second arm is always available. What settles it is p.10's Mandatory and Optional Steps read with p.17's `If Able` glossary entry, and the finding that the construction is not Wasp's at all - the rulebook illustrates Arien the Tidemaster's Dangerous Current printing it on p.6. **The reading, its three located clauses and the reasons both choice readings fail are [ledger question 13](../RULES_LEDGER.md#open-source-and-rules-questions), and the general rule is in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger)'s ordered mandatory/optional row**; a rule spanning heroes does not live in a hero shard. Milestone 8.10 implements Wasp's instance: an escalation policy on the discard, not a `Choose` node.
