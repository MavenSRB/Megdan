# Guards Digital - Cutter Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Cutter the Sky Pirate is the fourteenth hero this project has inventoried, and the second taken from
outside the twelve-hero package arc milestone 8.37 closed. This shard owns her content inventory,
source-verification status, capability contract map, and any deviation or open question scoped to Cutter
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. Nothing in
the source assigns her a pronoun; this shard follows the project's convention for the dashboards'
unnamed heroes and uses "she", as Misa's, Silverarrow's, Swift's and Whisper's shards each do for
the same reason.

## Cutter content ledger

Her printed set is inventoried at milestone 8.39. Composition, registration, dealing and the permanent
player-path gate are later milestones in the
[fourteenth-hero shard](../Roadmap/PHASE_8_FOURTEENTH_HERO.md); the vocabulary the text names as owed is
built by the lettered build blocks between them. **Nothing in this shard authorizes implementation.** An
inventory row records what the printed face says and which contract would carry it, and that is all it
does.

## Source authority

**CU-01. The second-printing rulebook in this repository does not name her, so the extraction is
sole authority for these faces.** The check was run rather than assumed: `pdftotext` over the local
second-printing PDF returns zero hits for `Cutter`, zero for `Sky Pirate`, and zero for all nineteen of
the card names. The rulebook's worked examples name Wasp, Xargatha, Arien, Brogan, Dodger, Sabina and
Tigerclaw and no one else, so this absence is that book's scope rather than evidence about the printing.

This is the same conclusion Swift's, Ursafar's and Tali's shards reach. Ursafar's and Tali's reason is
the pack-wide ruling that the Devoted Hero pack has no second source at all; Cutter's is Swift's - that
the one second source this project holds is silent about them. The consequence is identical: **every
string below is what our scan prints, and nothing is reconciled against a printing we do not hold.**

[Source provenance](../RULES_LEDGER.md) still governs the rest. The extracted cards are third-printing
playtest and the rulebook is second printing, so a disagreement between them is edition skew rather than
an extraction error, and a designer answer may carry a third-printing rules change.

**CU-02. A Fistful of Coins prints a victory sentence the second-printing rulebook does not contain, and
this is the most consequential source fact in the set.** The rulebook describes exactly three ways a
match ends, and every one of them awards the game to a TEAM: p.8's final Wave counter ("the team who won
this final push wins the game"), p.8's Throne capture ("the team who won the push wins the game"), and
p.13's Life counters ("the team with no Life counters left, loses the game"). It is emphatic about it -
the tie-break paragraph says that if both teams would win simultaneously the match **continues** "until
only one team wins the game". The printed face reads "If you have 13 or more coins, **you alone win the
game**", which names a single hero rather than a team, and no sentence in the book we hold describes
that ending.

Recorded as a source fact, not resolved here. It is edition skew by the standing provenance rule - a
third-printing playtest text ahead of the second-printing book - and the inventory's job is to state
that the printed text says what it says. What it costs to implement is a roadmap question and is
scoped as milestone 8.39g in the fourteenth-hero shard; what it means for the existing team-only
`MatchOutcome` contract was put to the user and settled as **CU-D1** below.

## Set shape

Nineteen faces, eighteen of them dealt. The shape matches Swift's exactly: one Gold `D` basic, one
Silver `S` basic, one Tier I with two Tier II and two Tier III in each of Red, Blue and Green, one
Purple Tier IV Ultimate, and one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.5) | Walk the Plank, Grappling Bolt, Daring Strike, Bombardment, Brace for Impact |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Legend of the Skies |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.5), used only for an odd player count (p.20) | Few More Steps |

**CU-03. The nineteen faces do NOT share one deck back, and she is one of the nine the corpus
records with two distinct back hashes.** Measured rather than assumed: fourteen backs hash alike and
five hash alike to a different value. The five are Daring Strike, Bold Thrust, Fearless Lunge, Legend of
the Skies and Few More Steps. Swift's shard states the opposite fact about her own set, so the sentence
is per-hero and must be measured for each - which is what the corpus-reading note already warned.

## Printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the face prints
no such icon, so that secondary action is unavailable from it.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `cutter` | 7624856 | Walk the Plank | Gold `D` | basic | 12 | 2 | 1 | Basic Skill | - | - | - |
| `cutter19` | 7624853 | Few More Steps | Gold `D` | `H` | 12 | 1 | 1 | Basic Skill | - | - | - |
| `cutter2` | 7632642 | Grappling Bolt | Silver `S` | basic | 5 | 2 | - | Basic Skill - Ranged | - | Range 5 | - |
| `cutter3` | 7624850 | Daring Strike | Red `R` | I | 8 | 6 | 3 | Attack | Attack 4+ | - | - |
| `cutter4` | 7624851 | Bold Thrust | Red `R` | II | 9 | 6 | 3 | Attack | Attack 5+ | - | Initiative |
| `cutter5` | 7624973 | Evasive Shot | Red `R` | II | 9 | 4 | 3 | Attack - Ranged | Attack 4 | Range 2 | Defense |
| `cutter6` | 7624852 | Fearless Lunge | Red `R` | III | 9 | 7 | 3 | Attack | Attack 5+ | - | Movement |
| `cutter7` | 7624974 | Tumble Shot | Red `R` | III | 9 | 5 | 3 | Attack - Ranged | Attack 4 | Range 2 | Radius |
| `cutter8` | 7718212 | Bombardment | Blue `B` | I | 10 | 5 | 3 | Skill | - | Radius 3 | - |
| `cutter9` | 7718593 | Barrage | Blue `B` | II | 10 | 5 | 3 | Skill | - | Radius 4 | Initiative |
| `cutter10` | 7718592 | X Marks the Spot | Blue `B` | II | 10 | 5 | 3 | Skill | - | Radius 3 | Attack |
| `cutter11` | 7718204 | Broadside | Blue `B` | III | 11 | 6 | 3 | Skill | - | Radius 4 | Initiative |
| `cutter12` | 7718206 | A Fistful of Coins | Blue `B` | III | 11 | 6 | 3 | Skill | - | Radius 3 | Attack |
| `cutter13` | 7718211 | Brace for Impact | Green `G` | I | 3 | 3 | 2 | Skill | - | - | - |
| `cutter14` | 7718210 | Ramming Speed | Green `G` | II | 3 | 3 | 2 | Skill | - | - | Attack |
| `cutter15` | 7718209 | Outmaneuver | Green `G` | II | 3 | 3 | 2 | Skill | - | Radius 3 | Defense |
| `cutter16` | 7718208 | Crashland | Green `G` | III | 2 | 4 | 2 | Skill | - | - | Range |
| `cutter17` | 7718207 | Outsmart | Green `G` | III | 2 | 4 | 2 | Skill | - | Radius 3 | Defense |
| `cutter18` | 7624846 | Legend of the Skies | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

**CU-04. Grappling Bolt is her one single-secondary face, and its glyph is a SHIELD sitting in the
boot's slot.** This is milestone 8.35z20's trap in the same purest form Swift's Bounce takes. Sixteen of
her faces print both secondaries with the riveted shield above the heeled boot, so a positional reader
learns "upper is Defense, lower is Movement" from every one of them - and then meets a card whose only
icon is drawn in the lower band and is a shield. The recorded value is **Defense 2, no secondary
Movement**, resolved by glyph shape at a shared crop rectangle and cross-checked in one image against
Walk the Plank and Few More Steps, whose shield-over-boot pairs sit at the two reference heights. Nothing
about the slot carries information on a one-icon card.

**CU-05. Every item icon was identified against a face whose channel this project already declares in
code**, never by eye, on the standing rule that Initiative's angular glyph and Movement's boot are the
easy pair to swap. The six references used were Silverarrow's Opportunity Shot (Defense), Long Shot
(Initiative), Clear Shot (Radius), Warning Shot (Attack), Treetop Sentinel (Range) and Lead Astray
(Movement). Her twelve item-bearing faces resolve to four Defense, three Attack, three Initiative, one
Radius, one Range and one Movement, with no candidate left ambiguous.

**CU-06. Three of her five Green faces print no Range and no Radius icon at all.** Brace for Impact,
Ramming Speed and Crashland print a bare `Skill` band with no reach glyph in the lower-right corner,
because their printed text carries its own distance and names its destination by adjacency rather than by
a measured disk. Outmaneuver and Outsmart, the other two Greens, do print Radius 3. Recorded because a
reader who expected a reach on every Skill would look for a missing icon.

**CU-07. Her three melee Red faces print a plus after the Attack value** - `Attack 4+` on Daring Strike
and `Attack 5+` on Bold Thrust and Fearless Lunge - which is the same notation Tigerclaw's Backstab and
Xargatha's Long Thrust use. The printed text is what says where the increment comes from: "+2 Attack"
on the branch that moves first, and nothing on the branch that does not.

**CU-10. Her stat profile is read, not estimated, and two of the four bars carry four conditional
sections each.** On p.21's stated 1-to-8 scale: **Attack 4 with four brighter conditional sections,
Defense 4, Initiative 6, and Movement 4 with four brighter conditional sections.** The pips resolve
cleanly against her bronze frame. The measurement was calibrated against Brogan's profile card rather
than against a remembered pitch, because his shard already states Initiative 1 and Defense 8 - the
scale's minimum and maximum - which fixes both the pixel pitch and the bar origin from known values.
p.21 states what the brighter sections mean: "Brighter colored sections of the stat bars are conditional.
They depend on your level-up choices and triggering specific card texts." Her roles are **Tactician,
Damager and Farming**, read from the profile card's three role icons and corroborated by name in the
rulebook's own p.21 role descriptions. This is selection metadata that no rule consumes.

## Exact rules text

Transcribed verbatim; printed line breaks are joined with a single space. **Bold** marks the keywords as
the cards print them.

| Card | Printed text |
| --- | --- |
| Walk the Plank | **Choose one —** • Push an enemy hero adjacent to you up to 4 spaces; if that hero is pushed into another zone, that hero discards a card, or is defeated. • Defeat a minion adjacent to you. |
| Few More Steps | Push an enemy hero adjacent to you up to 3 spaces; if that hero is pushed into another zone, that hero discards a card, or is defeated. |
| Grappling Bolt | Target an obstacle in range and in a straight line, with no obstacles between you; ignore immunity. Move in a straight line towards that obstacle until you are adjacent. |
| Daring Strike | **Choose one —** • Before the attack: Move 1 space in a straight line. Target a hero adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent to you. |
| Bold Thrust | **Choose one —** • Before the attack: Move 1 or 2 spaces in a straight line. Target a hero adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent to you. |
| Evasive Shot | Target a unit in range and in a straight line. After the attack: Move up to 2 spaces in the opposite direction. |
| Fearless Lunge | **Choose one —** • Before the attack: Move 1, 2 or 3 spaces in a straight line. Target a hero adjacent to you in the direction of the move; +2 Attack. • Target a unit adjacent to you. |
| Tumble Shot | Target a unit in range and in a straight line. After the attack: Move up to 3 spaces in the opposite direction. |
| Bombardment | An enemy hero adjacent to another enemy unit and not adjacent to you discards a card, if able. |
| Barrage | An enemy hero adjacent to another enemy unit and not adjacent to you discards a card, if able. |
| X Marks the Spot | An enemy hero in radius **chooses one —** • You place that hero in a space in radius. • You gain 2 coins. |
| Broadside | An enemy hero adjacent to another enemy unit and not adjacent to you discards a card, if able. May repeat once on a different target. |
| A Fistful of Coins | An enemy hero in radius **chooses one —** • You place that hero in a space in radius. • You gain 3 coins. If you have 13 or more coins, you alone win the game. |
| Brace for Impact | Move 3 spaces in a straight line, ignoring obstacles, to a space adjacent to an enemy hero; that hero discards a card, if able. |
| Ramming Speed | Move 3 or 4 spaces in a straight line, ignoring obstacles, to a space adjacent to an enemy hero; that hero discards a card, if able. |
| Outmaneuver | Swap with an enemy minion in radius; you may move that minion up to 2 spaces. |
| Crashland | Move 3, 4 or 5 spaces in a straight line, ignoring obstacles, to a space adjacent to an enemy hero; that hero discards a card, if able. |
| Outsmart | Swap with an enemy minion in radius; you may move that minion up to 3 spaces. |
| Legend of the Skies | The first time each turn after you perform a primary action, you may perform the primary action of a card in the previous turn slot. |

**CU-08. Bombardment, Barrage and Broadside print the SAME first sentence, and only the printed Radius
and Broadside's extra sentence separate them.** Bombardment is Radius 3, Barrage and Broadside Radius 4,
and Broadside alone adds "May repeat once on a different target." Recorded because a reader comparing the
three texts for a Tier difference would find none in the first sentence: the upgrade is entirely in the
reach icon and, at Tier III, in the repeat.

**CU-09. Few More Steps is the more restrictive half of a pair with Walk the Plank.** It pushes up to 3
where Walk the Plank pushes up to 4, prints Defense 1 where Walk the Plank prints Defense 2, and drops
the "Choose one" and its "Defeat a minion adjacent to you" branch entirely. That is the same relationship
Swift's Backup has to Reload!, Tigerclaw's Crackling Dagger to Magnetic Dagger, and Silverarrow's Keep
the Distance to Shoot and Scoot.

**CU-11. Her two "chooses one" faces put the choice in the ENEMY's hands, which is Whisper's seam and not
a new one.** Whisper's Lesser Evil and Greater Good already print "An enemy hero in range chooses one —",
and her shard records that the opponent choosing the bullet is what made those two faces new. X Marks the
Spot and A Fistful of Coins print the same construction with `in radius` instead of `in range`. Recorded
here so the mapping below can cite the existing contract rather than propose one.

## Upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is the choice one Level Up
offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Daring Strike | Bold Thrust (+1 Initiative) / Evasive Shot (+1 Defense) | Fearless Lunge (+1 Movement) / Tumble Shot (+1 Radius) |
| Blue | Bombardment | Barrage (+1 Initiative) / X Marks the Spot (+1 Attack) | Broadside (+1 Initiative) / A Fistful of Coins (+1 Attack) |
| Green | Brace for Impact | Ramming Speed (+1 Attack) / Outmaneuver (+1 Defense) | Crashland (+1 Range) / Outsmart (+1 Defense) |

The Tier III lineage follows the printed text rather than the table order: Bold Thrust's lunge continues
into Fearless Lunge, Evasive Shot's ranged retreat into Tumble Shot, Barrage's discard into Broadside, X
Marks the Spot's enemy-chosen modal into A Fistful of Coins, Ramming Speed's obstacle-ignoring charge into
Crashland, and Outmaneuver's minion swap into Outsmart.

**CU-12. Tumble Shot is the only face in her set whose item is a Radius, and it is on a Red card that
prints Range.** Her Red line prints Range, not Radius, so the +1 Radius that Tumble Shot becomes when it
is not taken can only ever help her Blue and Green Skills. The mirror also holds: Crashland's +1 Range is
on a Green card that prints no reach at all, and can only help her Red and Silver faces. Both are p.16's
ordinary behaviour - an item raises a value wherever the hero prints it - and both are worth naming
because a reader checking the item against its own card would find nothing for it to raise there.

## Capability contract map

**This section is Cutter's, not the matrix's.** [ROSTER_CAPABILITY_MATRIX.md](ROSTER_CAPABILITY_MATRIX.md)
owns the cross-hero mapping and sits close enough to its own threshold that a fourteenth hero's section
would push it over; the notation below is that file's, unchanged, so the two read alike.

`A`, `S`, `T`, `C` and `X` bind the actor, source card, chosen target, chooser and enclosing action
facts. `Out(n)` binds a typed output from node `n`. `M` means an impossible mandatory step preserves
earlier applied nodes and stops the remainder, `May` may be declined, and `IfAble` silently skips only
when infeasible. **Registered** means the contract is live in the default production catalog today;
**owed** names the build milestone that mints it.

**The audit below is why this hero's series is the size it is.** Most of her text lands on contracts that
already exist, and several of the pieces that look new are parameters on declarations this build has
carried for milestones. What is genuinely unbuilt is concentrated in four places, and one of them - the
victory sentence - is larger than everything else in the set put together.

| Faces | Contract | State |
| --- | --- | --- |
| Walk the Plank, Few More Steps | `push -> synthetic-object-push@3 (A, enemy hero adjacent to A; maximumTargets=1, spaces=4/3)` | registered |
| Walk the Plank, Few More Steps | `crossing -> the push's own published zone-crossing pair` | registered as of 8.39c - **and this row named the wrong contract until that milestone measured it.** It cited `after-push-trigger@1` at `TimingCheckpoint.ObjectPushApplied`, which is how an unlocked ULTIMATE watches a push some other card performed: Sabina's Big Sodding Gun is her Purple Tier IV permanent ability, "If you push an enemy hero, that hero discards a card, or is defeated", and performs no push of its own. Cutter's clause is a consequence on the same card that does the pushing, which is **Wasp's Kinetic Repulse** shape - "Push up to 2 enemy units adjacent to you 3 spaces; if a pushed hero is stopped by an obstacle, that hero discards a card, if able" - a rider read off the push's own output pair. 8.39c adds the sibling pair beside the stop causes. |
| Walk the Plank, Few More Steps | the zone test - the pushed hero's origin and destination zones differ | registered as of **8.39c**. Judged on origin against final destination and never on the spaces between, so a push that leaves a zone and re-enters it has carried nobody anywhere; and on displacement rather than attempt, so a hero the geometry left standing has crossed nothing. |
| Walk the Plank, Few More Steps | `consequence -> discard-chosen-card@2 (chooser=pushed hero) or defeat` | registered (Whisper's Greater Good prints the same "discards a card, or is defeated") |
| Walk the Plank | `minion -> synthetic-remove@2 (A, enemy minion adjacent to A)` | registered |
| Grappling Bolt | an obstacle as the thing targeted, rather than one of p.19's units | **owed, 8.39b** - the one genuinely unbuilt contract on this face |
| Grappling Bolt | "with no obstacles between you" - a clear straight line to the target | **owed, 8.39b** (near miss: `StraightLineQuery` answers the line and `ObstacleQuery` answers what stands on it; probe whether an existing declaration already composes them before minting a predicate) |
| Grappling Bolt | "ignore immunity" | **mechanism registered; the reading is CU-Q2.** `EffectNodeDefinition.ImmunityOverrides` + `IEffectImmunityOverrideCapabilityHandler.SupportedImmunityOverrideActivities` is a built declaration system that `EffectDefinitionValidator` already enforces (`UnsupportedImmunityOverride`), and Swap and direct move both support it today. `RuleActivity.All` is documented as p.18's unqualified immunity. The cost is a new handler implementing an existing interface, not a new mechanism. |
| Grappling Bolt | "Move in a straight line towards that obstacle until you are adjacent" | **probably registered in shape** - this is `choose-charge-line@1`'s semantics (a straight-line destination adjacent to a named thing, between a minimum and a maximum, returning both). The blocker is that its `unitKind` ranges over `RosterUnitKind`, which has no obstacle member; 8.39b probes what adding one costs, on Swift's 8.38a measurement that a member on an enum the payload already writes is free. |
| Daring Strike, Bold Thrust, Fearless Lunge | `premove -> a straight-line move of min/max 1/1, 1/2, 1/3 (BeforeAttack)` | registered. `choose-charge-line@1` already separates `minimumDistance` from `maximumDistance`, so the enumerated distances these three print need nothing built. |
| Daring Strike, Bold Thrust, Fearless Lunge | "Target a hero adjacent to you **in the direction of the move**" - the target set narrowed to the heading just travelled | **owed, 8.39a**, and it is a narrowing of something that exists rather than a new geometry. `choose-charge-line@1` returns a destination and Brogan names the unit afterwards with a separate radius-1 `choose-target@2`, which is the selection this narrowing sits on - his admits a unit in any of p.19's six directions and Cutter's faces admit only the continuation of the heading, one space where his are up to six. (**This row read the charge line as returning the pair; 8.39a measured that it does not, and 8.39e settled why it must not.**) **`actor-moved-this-action@1` is NOT the near miss it looks like**: it is Tigerclaw's boolean "did the attacker move", and carries no heading at all. |
| Daring Strike, Bold Thrust, Fearless Lunge | `+2 Attack` on the moved branch only | registered (`modify-value@2`) |
| Daring Strike, Bold Thrust, Fearless Lunge | second branch: `attack-target@2` adjacency-only unit target | registered |
| Evasive Shot, Tumble Shot | Target policy `reach=PrintedRange, line=InStraightLine` | registered as of milestone 8.38a (Swift's Red line) |
| Evasive Shot, Tumble Shot | "After the attack: Move up to 2/3 spaces **in the opposite direction**" - a move along the reverse of the attack's own line | **owed, 8.39a** (near miss: `attacked-target-space@1` already binds the attacked space, which is the half that names the line) |
| Bombardment, Barrage, Broadside | `subject -> radius enemy hero selection (S radius 3/4/4)` | registered (`synthetic-radius-unit-target@1` family) |
| Bombardment, Barrage, Broadside | "adjacent to another enemy unit" - the subject narrowed by adjacency to a unit of the SUBJECT's own team | registered as of **8.39d**, and the near miss is exact: Sabina's `adjacentToFriendlyMinion` parameter narrows the same selection by adjacency measured from the ACTOR's team and restricted to minions. Cutter's clause measures from the target's team and says unit, so it is a sibling parameter on a declaration that already carries one, not a new selection. |
| Bombardment, Barrage, Broadside | "and not adjacent to you" | registered (`target-is-not-adjacent@1`) |
| Bombardment, Barrage, Broadside | `discard -> discard-chosen-card@2 (chooser=subject; IfAble)` | registered |
| Broadside | "May repeat once on a different target" - p.10's repeat with the first subject excluded | **owed, 8.39d1** (the repeat is registered and so is a different-target repeat - but all three `Different*` arms are about an ATTACK's own target, stored in `PendingAttackResolution.ExcludedTargets`, and Broadside is a Skill whose subject comes from a card-text SELECTION. 8.39d measured that these are different seams and split the work out. Milestone 8.38c2's per-grant exclusion is NOT the nearest shape either - it is turn-scoped and spans separate granted actions; the nearest is `SameTarget`'s selection-node binding, which captures the same piece and performs the opposite operation on it.) |
| X Marks the Spot, A Fistful of Coins | `chooser -> an enemy hero in radius, who answers the modal` | registered (Whisper's Lesser Evil and Greater Good) |
| X Marks the Spot, A Fistful of Coins | branch 1: `synthetic-place-in-radius@1 (subject=chooser, destination in S radius)` | registered |
| X Marks the Spot, A Fistful of Coins | branch 2: `gain-resource@1 (A, coins=2/3)` | registered |
| A Fistful of Coins | **"If you have 13 or more coins, you alone win the game."** A victory sentence whose winner is one hero rather than a team | **owed, 8.39g - the largest single item in the set** - see CU-D1 |
| Brace for Impact, Ramming Speed, Crashland | `move -> choose-charge-line@1 (A; min/max 3/3, 3/4, 3/5; unitKind=Hero)` then a direct move to `Out(destination)` | registered - **and this is the row the first audit got wrong.** It read capability NAMES and concluded that nothing narrowed a MOVE's destination. `choose-charge-line@1` is Brogan's Mad Dash, "Move 2 spaces in a straight line to a space adjacent to an enemy unit", and its schema already carries `minimumDistance`, `maximumDistance` and a `unitKind` whose domain already admits `RosterUnitKind.Hero`. The exact distance, the enumerated distances and the adjacency narrowing are all registered. |
| Brace for Impact, Ramming Speed, Crashland | "ignoring obstacles" on that charge line | registered as of **8.39e**, and it was exactly the one optional parameter this row predicted - `choose-line-destination@1` had carried `ignoresObstacles` since Misa and `choose-charge-line@1` had not, because no printed charge had needed it. Bind it `true` on all three. The permission reaches the transit predicate only, so p.18's second sentence still forbids stopping on an obstacle: these faces cross more ground, not more endpoints. |
| Brace for Impact, Ramming Speed, Crashland | `discard -> discard-chosen-card@2 (chooser=Out of a radius-1 `choose-target@2` asked AFTER the move; IfAble)` | registered - **and this row named the wrong source until 8.39e settled it.** It read "that hero" as something the charge line hands over. It does not: the charge offers a set of DESTINATIONS and collapses the qualifying units away, so on a destination beside two enemy heroes there is no hero for a consequence to read. Compose these three the way Brogan's charges already are - charge, Move, then an ordinary radius-1 `choose-target@2` measured from where she now stands, which offers both and lets the player choose. Putting the question back inside the charge node is what milestone 8.35p took out, on the user's rule that "they move as soon as they select, and then they attack". |
| Outmaneuver, Outsmart | `swap -> synthetic-swap-actor@1 + choose-swap-subject@1 (A, enemy minion in S radius)` | registered (Misa's two Swap Skills) |
| Outmaneuver, Outsmart | `then -> synthetic-direct-move@1 (subject=Out(swap), up to 2/3 spaces; May)` | registered |
| Legend of the Skies | `trigger -> UltimateTriggerCondition.AfterControllerPerformsPrimaryAction` | registered as of **8.39f1** - **and this row named the wrong contract until 8.39f measured it.** It cited Tigerclaw's `AfterControllerPerformsBasicAction`. No existing `UltimateTriggerCondition` can reach this clause, and the reason is one line: every condition is matched against a `PerformedBasicAction`, and `BasicActionQuery.ForCompletedAction` returns `None` unless the source card's classification is `Basic` - p.9's Gold and Silver alone. Cutter fires after a PRIMARY action, which every card prints, and her Tier IV is unlocked when most of what she plays is not basic. The "first time each turn" bound reuses the turn's existing consumed-key set - milestone 8.19a's spent before-action triggers and 8.38d's granted-target exclusions already share it - under 8.39f1's own prefix. Compose the Ultimate with `TimingCheckpoint.ActionEnded` and this condition; a borrowed primary DOES arm the trigger, and the bound is what stops the loop. |
| Legend of the Skies | `borrow -> borrow-primary-action@1 (pool = `BorrowedActionPool.PreviousTurnSlotCard`)` | registered as of **8.39f**. A fourth pool member rather than a second reading of 8.38c's third: Swift's names a POSITION IN AN ORDER and walks back until it finds a card still hers, Cutter's names a SLOT and takes what stands in it, so a slot holding no card of hers is an empty pool where Swift's would reach past it. **Owner-scoped, and CU-Q3 is closed by the rulebook** - see below. |

**CU-13. "That hero discards a card, or is defeated" is already settled and is not an open question
here.** Rule-system ledger question 13 settled it on Arien's identical sentence and Whisper's Greater
Good composes it unchanged: one **mandatory** step with two ways to complete, rather than a discard that
silently skips. Walk the Plank and Few More Steps print the same sentence and take the same composition.
The contrast is printed on the same card set - Bombardment's and Brace for Impact's discards print "if
able" and are the skipping kind.

## Decisions and open questions

**CU-D1 (SETTLED 2026-09-11 by the user). "You alone win the game" is implemented faithfully:
a single hero wins and everyone else loses.** This was the open question of the set, and nothing else
in it is close in size. It was put to the user with the contract cost stated, and the answer was to
build what the card prints rather than to reinterpret it as a team win or to park the clause. The
reasoning below is kept because it is what the implementing milestone has to satisfy, not because the
question is still open.

The printed face reads "You gain 3 coins. If you have 13 or more coins, you alone win the game." Three
things about the build made it a real question rather than a wording detail, and they are what the
implementing milestone has to answer:

- `MatchVictoryCause` has exactly three members and all three are the rulebook's team sentences.
  `MatchOutcome` records a `WinningTeamId` and a `LosingTeamId` together, and its own remarks say why:
  "p.8 names its winner by side and p.13 names its loser by team, and each implies the other, so an
  ending that cannot name both names neither."
- `MatchOutcomeFault.UndeclaredOutcomeTeams` exists precisely to reject an ending that does not resolve
  to exactly one winning team and one losing team. A solo victory does not - it names one hero, and in
  2v2 and 3v3 that hero has team-mates whose standing the sentence does not state.
- The second-printing rulebook contains no sentence of this kind at all (CU-02), so there is no second
  source to read it against, and the tie-break paragraph it does contain pushes the other way: when two
  teams would win at once the match continues "until only one team wins the game".

What the decision commits the build to: the sentence is printed, it is unambiguous about who wins ("you
**alone**"), and it is reachable only from a Tier III Blue card at 13 coins. The implementing milestone
owes an answer for the losing side, for the winner's team-mates, and for the two-team `MatchOutcome`
contract that all thirteen previously dealt heroes share - so it is sequenced as a block of its own and
is the one place in this series where a schema move is expected rather than avoided.

**CU-Q2 (SETTLED at 8.39b, by the rulebook rather than by a reading). "Ignore immunity" does real work,
because an obstacle may BE a unit.** This entry was opened on the premise that "the thing it targets is
not a unit". p.18's own Obstacle keyword says otherwise - "Units, terrain (including printed on the
board), tokens, as well as any objects represented by miniatures, are obstacles" - so Grappling Bolt may
target a hero or a minion, and a unit may be immune. p.18's Immune keyword is then written to be
overridden in exactly these words: "If a unit is immune, you cannot target that unit ... **unless the
card text says otherwise**." This sentence is that card text. Terrain and tokens are obstacles that are
not units and carry no immunity for the clause to reach, so it reaches precisely the arm that needs it.
Implemented as `ImmunityOverrideReason.PrintedUnqualifiedImmunity`, minted beside the Heavy-minion
reason rather than widening it, because the two sentences say different things.

**CU-Q3. CLOSED at 8.39f by the rulebook. What is "the previous turn slot" on the first turn of a round, and whose slot is it?** **Answer: there is no previous slot on turn 1, and the slot is always the borrower's own.** p.6 step 6 prints the component outright - "place your card Faceup on the current Turn slot of **your hero dashboard**" - so a turn slot is a position on a dashboard and there is no table-wide slot to read at all. The absent possessive in "a card in the previous turn slot" is therefore not a widening: "your" is already carried by "turn slot". This mattered more than it looks, because the engine pointed the other way - `RoundTurnSlot` carries one assignment for EVERY configured hero, and the card state a borrow is handed really does contain the other heroes' instances, so the table-wide reading was both available and implementable. The question as originally recorded follows.\n\n**CU-Q3 (as recorded).** Legend of the Skies borrows
"the primary action of a card in the previous turn slot". p.6's turn area holds up to four slots and the
round empties it at the boundary, so on Turn 1 there is no previous slot and the Ultimate has nothing to
offer. The natural reading is that the clause simply does not fire, exactly as milestone 8.38c decided
that a rightmost card the borrow refuses empties the pool rather than falling through. Recorded because
the wording "a card in the previous turn slot" says **a** card rather than **your** card, which is the
second half of the question: whether the slot is read across the whole table or only within this hero's
own turn area. Milestone 8.38c's pool is owner-scoped and its remarks say `RoundTurnSlot` already
refuses an assignment naming another hero's card, which points at owner-scoped here too - but it points
rather than settles, and the build milestone records the reading it takes.

**CU-Q4. "In the direction of the move" when the move is one space, and when a branch declines to
move.** The three lunge faces print "Before the attack: Move 1 space in a straight line. Target a hero
adjacent to you in the direction of the move". A one-space move fixes a heading unambiguously. What the
text does not say is whether the move may be declined while the branch is still taken - the branch reads
as a single composed step, so the natural answer is no - and whether "the direction" survives a move that
p.19 stops early. Both are settled by the milestone that builds the narrowing.

**CU-Q5. "May repeat once on a different target" - different from the first target only.** Broadside's
repeat excludes the subject already taken. The question the build settles is whether the repeat is
offered at all when no second legal subject exists; milestone 8.35z15 established the general answer for
this build - a card-text repeat with no legal subject is not offered - so this is expected to be a
citation rather than a decision, and is recorded so the milestone confirms it rather than assumes it.

## Components and prepared artwork sources

Every row below is measured from the private corpus rather than guessed, and each names how.

| Component | Source |
| --- | --- |
| Selection card (the hover pick card) | `cards/hero-selection/randomizers-tt15__7723688__front.png`, matched to `_selection-cards/Cutter__front.png` by SHA-256 |
| Dashboard | `active-hero-cards/_dashboards/Cutter__front.png`, 850x445 |
| Card fronts | the eighteen dealt fronts in `active-hero-cards/Cutter/`, 372x520 |
| Standee element | `8583041` / `Airship_Captain`, `VerticalTokens`, read from the bundled manifest - **CU-14** |
| Standee faces | two 302x302 faces in `Image_0_Atlas` at (3249,2526) and (3553,2526) |

**CU-14. Her standee is in the bundled manifest under an epithet rather than her name, and this is the
third time that has happened.** The manifest's `VerticalTokens` list contains no entry named `Cutter`.
It contains `Airship_Captain` (id 8583041), and the identification is visual and unambiguous rather than
inferred from the name alone: the cropped 302x302 face shows the same teal curly hair, the same goggles
pushed up on the forehead and the same brown captain's coat as her profile card and her dashboard art.
The name is also exactly the kind this corpus uses for her - Whisper's standee is `Outcast` and Tali's is
`Spiritcaller`, each an unclaimed hero epithet, and "Airship Captain" is what "the Sky Pirate" is.

Her element has **two** 302x302 faces, the same shape every other hero's has, so
`Prepare-HeroTokens.ps1`'s two-face invariant holds for her and needs no widening. This was measured
through the manifest's own object model rather than by text-splitting it; a naive split of the
minified JSON on `},{` cuts the `sprites` array itself and reports one face where there are two.
