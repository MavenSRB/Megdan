# Guards Digital - Silverarrow Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Silverarrow the Pathfinder is the ninth hero this project inventoried, composed, registered, dealt and
player-path gated, and the third from outside the pitch line-up milestone 8.17 approved. This shard owns
her content inventory, source-verification status, and any deviation or open question scoped to
Silverarrow alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. Nothing in the
source assigns her a pronoun; this shard follows the project's convention for the dashboards' unnamed
heroes and uses "she".

## Silverarrow content ledger

Her printed set was composed at milestone 8.35z30 and dealt at milestone 8.35z46. Its implementation-grade
inventory contains the same fields the other eight dealt hero shards enumerate: stable definition and
action identities, content revision, colour and Tier, upgrade relationships, printed values, exact rules
text, ordered steps and choice owners, typed effect-node bindings, capability identities, item
contributions, Active-effect versus continuous-source classification, and the source-linked scenarios each
row rests on.

Her identity is pinned at content revision `silverarrow-8-35z46-rev-1`, measured rather than predicted by
`VerifiedContentIdentityTests` on a complete unfiltered run. That is her ninth-pin entry; the eight pins
above it came back byte-identical in the same run, which is what attributes the movement to her content
and to nothing else.

The revision is **re-based rather than incremented**, on the reading milestone 8.35z34 settled for Dodger
and milestone 8.35z38 repeated for Misa: her phase-one revision was minted under the placeholder id
`8.35x-silverarrow1`, which is a claim rather than a milestone. Nothing read the old string - no pin named
her, no artwork row named her, and no configuration dealt her - which is exactly what makes the re-base
free here and would not make it free again.

## Source authority

**RULING 27 (pack-wide).** The Tabletopia extraction is the **sole authority** for every Silverarrow
face. The local rulebook is the base game's: `pdftotext` returns zero occurrences of "Silverarrow",
"Silver Arrow" or "Pathfinder", and it illustrates none of her nineteen faces. **No face of hers can
be reproduced against a second source**, so ledger question 1 stays **open for the pack** - her set is
*consistent* with the second printing's conventions (p.9 colour letters and Tiers, the p.20 `H` card,
the p.16 Tier IV Ultimate, p.21's two-star complexity and five role icons) but consistency with a
convention is not a reproduced face, and this shard does not claim one. The printing-currency
question that once topped `ASK-ARTY.md` was closed on 2026-09-18 by physical second-printing cards
(see the ledger's source provenance): the extraction is the printed 2nd edition wherever it could be
checked, which strengthens this paragraph rather than weakening it; a designer answer still
supersedes it.

Provenance, corrected 2026-09-18: the card scans are the **printed second edition** wherever physical
cards could be checked (five of five, rules text exact), the same edition as the **second-printing**
rulebook; the 2026-09-06 "third-printing playtest" reading is superseded (see the parent ledger's
"Edition of the extraction").

## Identity

| Field | Value |
| --- | --- |
| Content set id | `silverarrow` |
| Display name | `Silverarrow the Pathfinder` (dashboard); `Silverarrow` on the profile card |
| Content revision | `silverarrow-8-35z46-rev-1` |
| Complexity | two stars, p.21's "Complex" - the first non-`Challenging` hero inventoried |
| Roles | Sniper, Damager, Disabler, Healer, Farming |
| Stat profile | Attack 2, Defense 1, Initiative 7, Movement 6 + 1 conditional |

**SL-10 is not a conflict.** Both printed faces spell the name one word with a single capital; the
extraction directory's `SilverArrow` is that extraction's identity, not this project's.

**Tokens and markers: she has none** (SL-09). No face names, places, spawns or returns one, and under
the standing 2026-08-23 ruling a hero's tokens are defined by that hero's card faces. Her blank
dashboard back is missing art, not missing rules. Where Wasp's telekinetic Skills read "a unit, or a
token", Silverarrow's three Green displacement Skills read "an enemy unit" only: the token class never
appears in her set.

**Defense 1 ties Brogan's Initiative 1 as the lowest value in the inventoried roster.** She is the
roster's most fragile hero on paper, which is consistent with a set whose damage comes from forced
discards rather than attack values.

## Deck composition

Nineteen printed faces make up p.5's eighteen-card deck plus the Handicap card that setup returns to
the box. The colour sets have the standard shape without exception: one Gold basic, one Silver basic,
one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, one Purple Tier IV
Ultimate, and one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.5) | Shoot and Scoot, Trailblazer, Snap Shot, Grasping Roots, Disorient |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Wild Hunt |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.5), used only for an odd player count (p.20) | Keep the Distance |

## Printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the face
prints no such icon, so that secondary action is unavailable from it.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `silverarrow` | 7522565 | Shoot and Scoot | Gold `D` | basic | 12 | 1 | 1 | Basic Attack - Ranged | Attack 1 | Range 2 | - |
| `silverarrow19` | 7506068 | Keep the Distance | Gold `D` | `H` | 11 | 1 | 2 | Basic Attack - Ranged | Attack 1 | Range 2 | - |
| `silverarrow2` | 7525566 | Trailblazer | Silver `S` | basic | 13 | 1 | - | Basic Skill | - | Radius 4 | - |
| `silverarrow3` | 7506058 | Snap Shot | Red `R` | I | 9 | 5 | 5 | Attack - Ranged | Attack 3 | Range 2 | - |
| `silverarrow4` | 7506062 | Opportunity Shot | Red `R` | II | 9 | 5 | 5 | Attack - Ranged | Attack 4 | Range 3 | Defense |
| `silverarrow5` | 7506051 | Long Shot | Red `R` | II | 9 | 4 | 5 | Attack - Ranged | Attack 3 | Range 3 | Initiative |
| `silverarrow6` | 7506063 | Clear Shot | Red `R` | III | 10 | 6 | 5 | Attack - Ranged | Attack 5 | Range 4 | Radius |
| `silverarrow7` | 7506055 | Rain of Arrows | Red `R` | III | 10 | 5 | 5 | Attack - Ranged | Attack 3 | Range 3 | Initiative |
| `silverarrow8` | 7510563 | Grasping Roots | Blue `B` | I | 10 | 3 | 3 | Skill | - | Radius 2 | - |
| `silverarrow9` | 7510561 | Entangling Vines | Blue `B` | II | 10 | 3 | 3 | Skill | - | Radius 3 | Defense |
| `silverarrow10` | 7512603 | Warning Shot | Blue `B` | II | 10 | 3 | 3 | Skill | - | Radius 3 | Attack |
| `silverarrow11` | 7510560 | Grappling Branches | Blue `B` | III | 11 | 4 | 3 | Skill | - | Radius 4 | Defense |
| `silverarrow12` | 7512602 | Treetop Sentinel | Blue `B` | III | 11 | 4 | 3 | Skill | - | Radius 3 | Range |
| `silverarrow13` | 7506050 | Disorient | Green `G` | I | 3 | 1 | 2 | Skill | - | - | - |
| `silverarrow14` | 7506060 | Divert Attention | Green `G` | II | 2 | 2 | 2 | Skill | - | - | Initiative |
| `silverarrow15` | 7506057 | Fae Healing | Green `G` | II | 2 | 2 | 2 | Skill | - | Radius 4 | Attack |
| `silverarrow16` | 7506052 | Lead Astray | Green `G` | III | 2 | 2 | 2 | Skill | - | - | Movement |
| `silverarrow17` | 7506066 | Nature's Blessing | Green `G` | III | 2 | 2 | 2 | Skill | - | Radius 4 | Attack |
| `silverarrow18` | 7506067 | Wild Hunt | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

**The one trap worth restating.** Trailblazer is her only one-icon card, and its single glyph is the
riveted **shield** of a Defense, not the heeled boot of a Movement. A positional read gives the
opposite answer - Wasp's Stop Projectiles (Movement only) and Static Barrier (Defense only) both draw
their single icon in the same lower band, so on a one-icon card the slot carries no information at
all. Every one of the nineteen secondary blocks was resolved by glyph SHAPE at a shared crop
rectangle against governed reference faces, and no recorded value changed in that pass.

## Exact rules text

Transcribed verbatim; printed line breaks are joined with a single space. **Bold** marks the keywords
as the cards print them.

| Card | Printed text |
| --- | --- |
| Shoot and Scoot | Target a unit at maximum range. After the attack: If able, you may fast travel to an adjacent zone. |
| Keep the Distance | Target a hero at maximum range. After the attack: If able, you may fast travel to an adjacent zone. |
| Trailblazer | You may fast travel, if able. **This round:** You and friendly heroes in radius may ignore obstacles while performing movement actions. |
| Snap Shot | **Choose one —** • Target a unit in range, which is not adjacent to any other unit. • Target a unit adjacent to you. |
| Opportunity Shot | **Choose one —** • Target a unit in range, which is not adjacent to any other unit. • Target a unit adjacent to you. |
| Long Shot | Target a unit at maximum range. |
| Clear Shot | **Choose one —** • Target a unit in range, which is not adjacent to any other unit. • Target a unit adjacent to you. |
| Rain of Arrows | Target a unit at maximum range. If you target a hero, repeat once on a different hero; if you do, repeat once on a minion. |
| Grasping Roots | **This turn:** Enemy heroes in radius cannot fast travel, or move more than 1 space with a movement action. |
| Entangling Vines | **This turn:** Enemy heroes in radius cannot fast travel, or move more than 1 space with a movement action. |
| Warning Shot | **End of turn:** An enemy hero in radius discards a card, if able. |
| Grappling Branches | **This turn:** Enemy heroes in radius cannot fast travel, or move more than 1 space with a movement action. |
| Treetop Sentinel | **End of turn:** An enemy hero in radius discards a card, or is defeated. |
| Disorient | Move an enemy unit adjacent to you 1 space; if you do, you may move 1 space. |
| Divert Attention | Move an enemy unit adjacent to you up to 2 spaces; if you do, move up to that number of spaces in a straight line. |
| Fae Healing | A hero in radius may retrieve a discarded card; if they do, that hero gains 1 coin. |
| Lead Astray | Move an enemy unit adjacent to you up to 3 spaces; if you do, move up to that number of spaces in a straight line. |
| Nature's Blessing | A hero in radius may retrieve a discarded card; if they do, that hero gains 2 coins. |
| Wild Hunt | Each time before you perform an action you may move 2 spaces in a straight line. |

## Upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is the choice one Level Up
offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Snap Shot | Opportunity Shot (+1 Defense) / Long Shot (+1 Initiative) | Clear Shot (+1 Radius) / Rain of Arrows (+1 Initiative) |
| Blue | Grasping Roots | Entangling Vines (+1 Defense) / Warning Shot (+1 Attack) | Grappling Branches (+1 Defense) / Treetop Sentinel (+1 Range) |
| Green | Disorient | Divert Attention (+1 Initiative) / Fae Healing (+1 Attack) | Lead Astray (+1 Movement) / Nature's Blessing (+1 Attack) |

| Item | Slot | Count | Silverarrow cards carrying that icon |
| --- | --- | --- | --- |
| +1 Attack | 1 | 7 | the seven cards with an Attack primary, six once Keep the Distance is boxed |
| +1 Defense | 2 | 18 | every card except Wild Hunt |
| +1 Initiative | 3 | 18 | every card except Wild Hunt |
| +1 Movement | 4 | 17 | every card except Trailblazer and Wild Hunt |
| +1 Range | 4 | 7 | both basics' Gold attacks and all five Red Attacks |
| +1 Radius | 4 | 8 | Trailblazer, the five Blue Skills, Fae Healing, Nature's Blessing |

## Three structural facts

- **No Block and no Defense primary anywhere.** The whole block-evaluation family milestone 8.11 built
  is not exercised by her set.
- **Every one of her seven Attacks is `Attack - Ranged`.** She has no melee Attack primary at all. Two
  reach an adjacent unit, but only through a modal bullet on a card whose band still reads Ranged,
  which is what makes RULING 39 material.
- **Her Ultimate prints neither Range nor Radius**, so she is the first hero for whom ledger question
  12's item-reaches-the-Ultimate interpretation has nothing at all to apply to.

Clear Shot's Range 4 is the longest printed reach in the verified roster (Wasp's maximum is 3), and
Trailblazer's Radius 4 the largest on any basic card (Static Barrier is Radius 2). Her Green cards
carry Initiative 2-3 while her Gold and Silver carry 11-13, the widest printed Initiative spread of
any inventoried hero.

## Settled readings, and where each one lives in code

| Ref | Reading | Where it is declared |
| --- | --- | --- |
| RULING 1 (SL-01) | "at maximum range" = **exactly** the Range Total. A unit nearer is not a legal target at all. | `AttackTargetReach.ExactlyPrintedRange`, applied in `AttackTargetQuery` |
| RULING 8 (SL-04) | "A hero in radius" = **any** hero in radius - enemies included, and herself included. The card comes from that hero's own discard and they choose it. | the hero arm's `includeSelf`, then `retrieve-card@1` with the chosen hero as owner *and* chooser |
| RULING 27 | The extraction is sole authority for pack heroes; ledger question 1 stays open. | This shard's own header |
| RULING 37 (SL-03) | "that number" = the spaces the enemy was **actually** moved. | `synthetic-direct-move@3`'s realised `distance` output bound into the line chooser's `maximumDistance` |
| RULING 38 (SL-05) | Repeats mandatory where a legal target exists, skipped where none; the minion clause fires only if the hero repeat happened; every instance inherits the full targeting sentence; the minion is a different unit. | Both clauses composed - `DifferentEnemyHero` then `DifferentEnemyMinion`, both `IfAble` - and the chain bounded by `CardTextRepeatChain` |
| RULING 39 (SL-02) | The adjacent arm of a Red modal is **still a ranged attack**. | True by construction: the action declares `Ranged` and the policy only says which units it reaches |
| RULING 47 | A card-text Move is not a Movement action, in **both** directions. | Activity-scoped permissions: Trailblazer grants `MoveThroughObstacles` `whilePerforming: Movement` |
| RULING 52 | Membership in a radius is **live** for every Active-effect class. | Her three restriction Blues inherit Arien's shape unchanged; nothing of hers samples at turn start |
| SL-06 | "End of turn:" = her turn, radius evaluated **at firing time** against her position then. | `ActiveEffectDurationKind.EndOfTurn`, triggering and expiring at `TimingCheckpoint.TurnEnded` |
| SL-07 | Trailblazer's round-long permission is continuously re-evaluated, radius live. | `ControllerAndHeroesInRadius`, membership re-derived at every ask (p.17) |
| SL-08 | Edition confirmation unavailable. | Superseded by RULING 27 above |
| SL-09 / SL-10 | Tokens: none. Spelling: no conflict. | Recorded above |

**The Red modal needed no ruling, and the argument is reusable.** "Not adjacent to any other unit"
either counts the attacker or does not, and **the union of the two printed bullets is the same set
either way** - every enemy unit within Range that no unit stands beside, plus every enemy unit
adjacent to her. So the modal is composed as ONE candidate set
(`AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou`) rather than as a choose-one over two
policies, the equality is asserted on a purpose-built board rather than argued, and RULING 39 falls
out with no rule written anywhere: there is no arm to carry a type.

**Three of her faces cost nothing at all.** Grasping Roots, Entangling Vines and Grappling Branches
print Arien's Deluge word for word, and milestone 8.20 had already built both of its declarations -
`RuleModification.ForRestriction(RuleActivity.FastTravel)` and
`RuleModification.ForActivityLimit(RuleActivity.Movement, 1)`. They differ from each other only in the
printed Radius (2 / 3 / 4).

**Three "if you do" clauses needed no predicate between them**, which is the composition's most
repeated finding. RULING 37's second Move is bounded by the first's realised distance, so nought
spaces moved offers nothing; RULING 8's coin grant binds its recipient from an output the retrieval
publishes only on success, so a declined retrieval leaves it undefined; and RULING 38's minion clause
is printed second, so a chain reaches it only from an instance the first clause produced. In each case
the step before it does the work a condition would have done.

**Her whole Green push kit shares one targeting node, and a first build routed it through the wrong
capability family.** Disorient, Divert Attention and Lead Astray all target "an enemy unit adjacent to
you" through `SilverarrowEffectContent.AdjacentEnemyUnitNode`, and none of the three prints a Radius
icon at all - correctly, since "adjacent" is a fixed distance the sentence states rather than a number
p.9's icon carries (see [rules-ledger row 27](../RULES_LEDGER.md#open-source-and-rules-questions)). A
first build bound the node through `SyntheticTargetingCapabilities.RadiusTargetV1` anyway, whose own
handler reads its search bound off the card's printed Radius channel and refuses to invent one where
none is printed - so all three offered no target on any board. The fix routes the node through
`ChooseTargetV4` instead, with the distance as a literal `range=1` input, matching how Red Needle's
Push and Blue Sentinel's Swap already name their own adjacent object; `HeroPlayerPathMatrix.
CapabilityShapes` gained the sibling entry `synthetic-choose-target` since her content is the first
dealt to name it.

## Composition state

**18 of 19 faces composed.** Parked: **Keep the Distance**, under the standing p.20 Handicap deferral
(`PrintedTextComposition.DeferredUntil("first odd-player-count configuration")`) - the same decision
Wasp's Crackling Dagger and Arien's Ceremonial Blade take. Its printed values, its hero-only narrowing
and its exact-range band all ship as inventory; only its "After the attack:" sentence is uncomposed,
and the player-path gate records it as unreachable against the content's own deferral.

**Rain of Arrows is composed in full.** Milestone 8.35z30 shipped its hero clause and parked the
minion clause on a measured seam limit; 8.35z46 closed it. Two walls stood in front of
it and only one had been measured. The known one: a repeated Attack learns which unit kind it may take
by reading its own CARD's declaration - derived deliberately, so the clause moves no snapshot field -
which cannot tell two clauses apart on one card. The second, found by probe: rulebook p.10's "You
cannot repeat a repeated action!" is enforced for card text as a single boolean about the enclosing
attack, so the minion clause would have been skipped in silence inside the very instance it exists to
fire in. Both have one cause - card text is always started with no completed repeats, so that boolean
is a SUBSTITUTE for a counter the card-text path never had - and one answer: the chain already mints
each instance's identity from the last one's, so the position is restorable state already.
`CardTextRepeatChain` reads it. No schema version moves and a card printing one clause keeps the exact
walk it had, which is every face in this build but this one.

**Inherited, not introduced**: RULING 15's turn-start sampling is withdrawn by RULING 52, which makes
membership live for every Active-effect class - so her three Blue faces are correct as shipped, exactly
as Arien's identical sentence is.

## The player-path gate

Her table adds **no interaction shape** and three capability classifications (`fast-travel`,
`choose-line-destination`, `synthetic-attack-took-a-hero`). Nineteen new faces and the surface already
had a name for every input they ask for, which is the healthiest outcome that gate has.

Two rows are worth reading twice. Her three Red modals carry **no** `ModalBulletChoice`, because the
two bullets name one set and the card composes no Choose node - a player is shown one target question.
And Rain of Arrows carries `RepeatDecision` while its repeats open **no control**: the shape is
derived from the node kind and RULING 38's repeats are `if able`, so the journey that proves it shows
the chain ADVANCING rather than a control being pressed.

## Ledger rows this hero owns

Rulings 1, 8, 27, 37, 38, 39 and 47, plus SL-06 (the `End of turn:` keyword - a new Active-effect
duration, first printed by this hero, whose whole content is *when* a one-shot piece of work happens)
and SL-07 (the round-long movement-action permission, live membership). Ledger question 12 records
that she is the first hero for whom the item-reaches-the-Ultimate interpretation has nothing to reach;
ledger question 13 is settled generally by Warning Shot and Treetop Sentinel being the
discard / discard-or-defeated pair, and raises no new question; ledger question 1 stays **open for the
pack** under RULING 27.
