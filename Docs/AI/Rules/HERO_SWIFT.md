# Guards Digital - Swift Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Swift the Sharpshooter is the thirteenth hero this project has inventoried, and the first taken from
outside the twelve-hero package arc milestone 8.37 closed. This shard owns her content inventory,
source-verification status, capability contract map, and any deviation or open question scoped to Swift
alone. A rule system that spans heroes stays in the
[Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or open question affecting more
than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. Nothing in
the source assigns her a pronoun; this shard follows the project's convention for the dashboards' unnamed
heroes and uses "she".

## Swift content ledger

Her printed set is inventoried at milestone 8.38. Composition, registration, dealing and the permanent
player-path gate are milestones 8.38d, 8.38e and 8.38f in the
[thirteenth-hero shard](../Roadmap/PHASE_8_THIRTEENTH_HERO.md); the vocabulary her text names as owed is
built at 8.38a-8.38c. **Nothing in this shard authorizes implementation.** An inventory row records what
the printed face says and which contract would carry it, and that is all it does.

## Source authority

**SW-01. The second-printing rulebook in this repository does not name her, so the extraction is sole
authority for these faces.** The check was run rather than assumed: `pdftotext` over the local
second-printing PDF returns zero hits for `Swift`, zero for `Sharpshooter`, and zero for all nineteen of
her card names. Its only two hits for the substring `Snipe` are p.21's **Sniper** role description, which
is a role and not a card. The rulebook's worked examples name Wasp, Xargatha, Arien, Brogan, Dodger,
Sabina and Tigerclaw and no one else, so her absence is that book's scope rather than evidence about her
printing.

This is the same conclusion Ursafar's and Tali's shards reach and a different reason for it. Theirs is
the pack-wide ruling that the Devoted Hero pack has no second source at all; hers is that the one second
source this project holds is silent about her. The consequence is identical: **every string below is what
our scan prints, and nothing is reconciled against a printing we do not hold.**

[Source provenance](../RULES_LEDGER.md) still governs the rest. The extracted cards were verified
against physical second-printing cards on 2026-09-18 and match the print on every rules-bearing word
checked, so the cards and the rulebook are the SAME edition: a disagreement between them is a rulebook
gap or a typesetting difference to be weighed on its merits, not edition skew, and a designer answer
that contradicts the print reflects the third printing in preparation rather than this edition.

## Identity

| Field | Value |
| --- | --- |
| Content set id | `swift` |
| Display name | `Swift the Sharpshooter` (dashboard); `Swift` on the profile card |
| Complexity | **three stars**, p.21's highest band - the joint-highest this project has inventoried, with Tali |
| Roles | Sniper, Farming, Tactician |
| Stat profile | Attack 5, Defense 2, Initiative 4, Movement 7 |
| Standee element | `8597511` / `Swift`, `VerticalTokens`, read from the bundled manifest |
| Selection card | `randomizers-tt9__7723691__front.png`, matched to `_selection-cards/Swift__front.png` by SHA-256 |

**SW-02. The stat bars were measured, and the gold frame limits what the measurement can separate.** The
pip pitch was calibrated on Defense, the bar with the fewest pips, at 7 px, and the other three
cross-checked against it on a scanline that shows the 1 px separators. What the technique cannot do here
is split a base section from p.21's brighter conditional section: her frame is gold and every filled pip
classifies warm, which is exactly the failure Wasp's shard recorded for the same reason. So the four
numbers above are **totals**, and this shard does not claim how many of Movement's seven are conditional
on level-up choices or card texts.

**SW-03. She owns no token and no marker.** No face names, places, spawns, returns or reclaims one, and
under the standing 2026-08-23 ruling a hero's components are defined by that hero's card faces. Her
standee is a piece, not a rules component. Where Tali's set reaches
`SyntheticComponentDefinitions`' token list, Swift's contributes nothing to it.

**SW-04. She prints no Movement primary and no Defense primary.** Every one of her eighteen dealt faces
is a Basic Skill, a Ranged Attack, a Skill or the Ultimate. Her mobility is entirely card text - the Blue
line's placements and Bounce's straight-line walk - which is a different thing from a Movement action and
is why none of it reads a Movement value.

## Deck composition

Nineteen printed faces make up p.5's eighteen-card deck plus the Handicap card that setup returns to the
box. The colour sets have the standard shape without exception: one Gold basic, one Silver basic, one
Tier I with two Tier II and two Tier III in each of Red, Blue and Green, one Purple Tier IV Ultimate, and
one Gold `H`.

| Membership | Cards |
| --- | --- |
| Starting hand (p.5) | Reload!, Bounce, Snipe, Steam Jump, Suppress |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Bullet Time |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.5), used only for an odd player count (p.20) | Backup |

**All nineteen faces share one deck back.** The extraction gives every one of them the same back
rectangle, so she is not among the nine heroes the corpus records with two distinct deck-back hashes.

## Printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the face prints
no such icon, so that secondary action is unavailable from it.

| Internal | Element | Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `swift` | 7578960 | Reload! | Gold `D` | basic | 6 | 1 | 1 | Basic Skill | - | - | - |
| `swift19` | 7505633 | Backup | Gold `D` | `H` | 6 | 1 | 1 | Basic Skill | - | - | - |
| `swift2` | 7579012 | Bounce | Silver `S` | basic | 12 | 2 | - | Basic Skill | - | - | - |
| `swift3` | 7572613 | Snipe | Red `R` | I | 8 | 4 | 4 | Attack - Ranged | Attack 5 | Range 4 | - |
| `swift4` | 7572612 | Prepared Shot | Red `R` | II | 8 | 5 | 4 | Attack - Ranged | Attack 5 | Range 4 | Defense |
| `swift5` | 7505640 | Shotgun | Red `R` | II | 8 | 6 | 4 | Attack - Ranged | Attack 4 | Range 2 | Initiative |
| `swift6` | 7572609 | Killshot | Red `R` | III | 9 | 5 | 4 | Attack - Ranged | Attack 6 | Range 5 | Defense |
| `swift7` | 7505632 | Super-Shotgun | Red `R` | III | 9 | 6 | 4 | Attack - Ranged | Attack 5 | Range 2 | Radius |
| `swift8` | 7578961 | Steam Jump | Blue `B` | I | 9 | 3 | 2 | Skill | - | Radius 3 | - |
| `swift9` | 7578113 | Assault Jump | Blue `B` | II | 10 | 4 | 2 | Skill | - | Radius 4 | Attack |
| `swift10` | 7505638 | Delayed Jump | Blue `B` | II | 10 | 4 | 1 | Skill | - | Radius 2 | Defense |
| `swift11` | 7578114 | Drop Trooper | Blue `B` | III | 10 | 4 | 2 | Skill | - | Radius 4 | Attack |
| `swift12` | 7505635 | Mobile Scout | Blue `B` | III | 10 | 4 | 1 | Skill | - | Radius 2 | Movement |
| `swift13` | 7572606 | Suppress | Green `G` | I | 4 | 2 | 2 | Skill | - | Radius 3 | - |
| `swift14` | 7572605 | Pin Down | Green `G` | II | 4 | 2 | 2 | Skill | - | Radius 4 | Attack |
| `swift15` | 7579992 | Mark for Death | Green `G` | II | 4 | 2 | 2 | Skill | - | Radius 4 | Initiative |
| `swift16` | 7572608 | Killing Ground | Green `G` | III | 3 | 3 | 2 | Skill | - | Radius 4 | Range |
| `swift17` | 7579993 | Hunting Season | Green `G` | III | 3 | 3 | 2 | Skill | - | Radius 4 | Initiative |
| `swift18` | 7702879 | Bullet Time | Purple `P` | IV | - | - | - | Ultimate | - | - | - |

**SW-05. Bounce is her one single-secondary face, and its glyph is a SHIELD sitting in the boot's slot.**
This is milestone 8.35z20's trap in its purest form. Sixteen of her faces print both secondaries with the
riveted shield above the heeled boot, so a positional reader learns "upper is Defense, lower is
Movement" from every one of them - and then meets a card whose only icon is drawn in the lower band and
is a shield. The recorded value is **Defense 2, no secondary Movement**, resolved by glyph shape at a
shared crop rectangle and cross-checked against Silverarrow's Trailblazer, the governed reference face
for the same situation. Nothing about the slot carries information on a one-icon card.

**SW-06. Every item icon was identified against a face whose channel this project already declares in
code**, never by eye, on the standing rule that Initiative's hourglass and Movement's boot are the easy
pair to swap. The six references used were Silverarrow's Opportunity Shot (Defense), Long Shot
(Initiative), Clear Shot (Radius), Warning Shot (Attack), Treetop Sentinel (Range) and Lead Astray
(Movement). Her twelve item-bearing faces resolve to four Defense-or-Attack pairs, three Initiative, one
Radius, one Range and one Movement, with no candidate left ambiguous.

## Exact rules text

Transcribed verbatim; printed line breaks are joined with a single space. **Bold** marks the keywords as
the cards print them.

| Card | Printed text |
| --- | --- |
| Reload! | **Choose one —** • Perform the primary action of your rightmost resolved card. • Defeat a minion adjacent to you. |
| Backup | Perform the primary action of your rightmost resolved card. If you perform an attack this way, you cannot target minions. |
| Bounce | Move 2 spaces in a straight line, ignoring obstacles; if this card is already resolved as you perform this action, may repeat once. |
| Snipe | Target a unit at maximum range, and in a straight line. |
| Prepared Shot | Target a unit in range, in a straight line, and not adjacent to you. |
| Shotgun | Target a unit in range. Before the attack, an enemy hero adjacent to the target discards a card, if able. |
| Killshot | Target a unit in range, in a straight line, and not adjacent to you. |
| Super-Shotgun | Target a unit in range. Before the attack, an enemy hero adjacent to the target discards a card, or is defeated. |
| Steam Jump | Place yourself into a space in a straight line in radius. Push an enemy unit adjacent to you up to 1 space. |
| Assault Jump | Place yourself into a space in a straight line in radius. Push an enemy unit adjacent to you up to 2 spaces. |
| Delayed Jump | **End of turn:** Place yourself into a space in radius not in a straight line from you. |
| Drop Trooper | Place yourself into a space in a straight line in radius. Push up to two enemy units adjacent to you up to 2 spaces. |
| Mobile Scout | **End of turn:** Place yourself into a space in radius not in a straight line from you. You may then fast travel once, if able. |
| Suppress | An enemy hero in radius, who is not adjacent to terrain discards a card, if able. |
| Pin Down | An enemy hero in radius, who is not adjacent to terrain, discards a card, if able. |
| Mark for Death | Move an enemy minion in radius up to 3 spaces to a space in radius. **Next turn:** the first time an enemy minion in radius is defeated, gain 1 coin. |
| Killing Ground | An enemy hero in radius, who is not adjacent to terrain, discards a card, or is defeated. |
| Hunting Season | Move up to two enemy minions in radius up to 3 spaces to spaces in radius. **Next turn:** The first two times an enemy minion in radius is defeated, gain 1 coin. |
| Bullet Time | After you resolve a basic card you may perform the primary action on that card; you cannot target the same enemy hero twice in the same turn this way. |

**SW-07. Suppress prints no comma after "terrain" and its two siblings do.** Pin Down and Killing Ground
both read "who is not adjacent to terrain, discards a card"; Suppress reads "who is not adjacent to
terrain discards a card". Recorded as printed. It changes no reading - the relative clause is
restrictive in all three, which is the only reading that leaves "who is not adjacent to terrain" doing
work - and it is recorded because a transcription that silently regularised it would be asserting a
correction this project has no second source to make.

**SW-08. Mark for Death's second sentence begins "the" and Hunting Season's begins "The".** Same
observation, same disposition: transcribed as printed, no reading depends on it.

## Upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is the choice one Level Up
offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Snipe | Prepared Shot (+1 Defense) / Shotgun (+1 Initiative) | Killshot (+1 Defense) / Super-Shotgun (+1 Radius) |
| Blue | Steam Jump | Assault Jump (+1 Attack) / Delayed Jump (+1 Defense) | Drop Trooper (+1 Attack) / Mobile Scout (+1 Movement) |
| Green | Suppress | Pin Down (+1 Attack) / Mark for Death (+1 Initiative) | Killing Ground (+1 Range) / Hunting Season (+1 Initiative) |

**SW-09. Super-Shotgun is the only face in her set whose item is a Radius, and it is on a Red card.** Her
Red line prints Range, not Radius, so the +1 Radius that Super-Shotgun becomes when it is not taken can
only ever help her Blue and Green Skills. That is p.16's ordinary behaviour - an item raises a value
wherever the hero prints it - and it is worth naming because a reader checking the item against its own
card would find nothing for it to raise there.

## Capability contract map

**This section is Swift's, not the matrix's.** [ROSTER_CAPABILITY_MATRIX.md](ROSTER_CAPABILITY_MATRIX.md)
owns the cross-hero mapping and sits close enough to its own threshold that a thirteenth hero's section
would push it over; the notation below is that file's, unchanged, so the two read alike.

`A`, `S`, `T`, `C` and `X` bind the actor, source card, chosen target, chooser and enclosing action
facts. `Out(n)` binds a typed output from node `n`. `M` means an impossible mandatory step preserves
earlier applied nodes and stops the remainder, `May` may be declined, and `IfAble` silently skips only
when infeasible. **Registered** means the contract is live in the default production catalog today;
**owed** names the build milestone that mints it.

| Faces | Contract | State |
| --- | --- | --- |
| Snipe | Target policy `reach=ExactlyPrintedRange, line=InStraightLine` | reach registered (RULING 1); line **owed, 8.38a** |
| Prepared Shot, Killshot | Target policy `reach=PrintedRange, line=InStraightLine, proximity=NotAdjacentToActor` | line and proximity **owed, 8.38a** |
| Shotgun | `shot-bystander -> choose-target@2 (A,C, enemy hero adjacent to X.target, excluding X.target itself; IfAble/One; BeforeAttack)`; `shot-discard -> synthetic-discard-chosen-card@2 (chooser=Out(shot-bystander); IfAble/One)` | registered |
| Super-Shotgun | Shotgun's two nodes plus `shot-defeat -> synthetic-defeat-hero@1 (when the discard reported none; M/One; staged Defeat)` | registered - Treetop Sentinel's escalation, ledger question 13 |
| Steam Jump, Assault Jump, Drop Trooper | `jump-destination -> place-in-radius-in-straight-line@1 (A, in printed Radius, on a file from A, empty, no Spawn point, not adjacent to an empty Spawn point; M/One)`; `jump-place -> synthetic-place@2 (piece=A, destination=Out(jump-destination); M/One)` | destination **owed, 8.38b**; place registered |
| Steam Jump, Assault Jump | `jump-push-target -> synthetic-choose-target@1 (enemy unit adjacent to A; May/One; after the placement)`; `jump-push -> synthetic-object-push@3 (source=A, target=Out(jump-push-target), distance 0..1 / 0..2)` | registered |
| Drop Trooper | `drop-push-set -> choose-target-set@1 (enemy units adjacent to A, max=2; May/One)`; `drop-push -> synthetic-object-push@3 over the set, distance 0..2; Batch` | set registered; the multi-subject push is **owed, 8.38b** |
| Delayed Jump, Mobile Scout | `late-destination -> place-in-radius-off-every-line@1 (A, in printed Radius, on no file from A, same occupancy rules; M/One; TurnEnded)`; `late-place -> synthetic-place@2` | destination **owed, 8.38b**; checkpoint registered (Warning Shot) |
| Mobile Scout | `scout-travel -> fast-travel@1 (A; May/One; after the placement)` | registered, moved across with Silverarrow at 8.35z46 |
| Suppress, Pin Down | `pin-subject -> synthetic-radius-hero-target@1 (enemy hero in printed Radius)` narrowed by `subject-adjacent-terrain-presence@1 (subject=Out(pin-subject); false)`; `pin-discard -> synthetic-discard-chosen-card@2 (chooser=Out(pin-subject); IfAble/One)` | targeting and discard registered; the SUBJECT-scoped terrain predicate is **owed, 8.38d** - see SW-11 |
| Killing Ground | Pin Down's nodes plus `pin-defeat -> synthetic-defeat-hero@1 (when the discard reported none)` | registered |
| Mark for Death | `mark-subject -> synthetic-radius-minion-target@1 (enemy minion in printed Radius)`; `mark-move -> synthetic-direct-move@1 (target=Out(mark-subject), up to 3, destination in printed Radius; M/One)`; `mark-bounty -> next-turn-defeat-bounty@1 (radius from S, count=1, reward=gain-resource@1 1 coin)` | targeting and move registered; the counted bounty is **owed, 8.38c** |
| Hunting Season | Mark for Death's nodes over `choose-target-set@1 (max=2)` with `synthetic-batch-direct-move@1`, and `count=2` on the bounty | set and batch move registered; bounty **owed, 8.38c** |
| Reload! | `reload-mode -> choose@1 (C, borrow branch or defeat branch; M/One)`; borrow arm `reload-source -> rightmost-resolved-card@1 (A)` then `borrow-primary-action@1 (card=Out(reload-source))`; defeat arm `synthetic-defeat-minion@1 (enemy minion adjacent to A)` | modal, borrow verb and defeat registered; the selector is **owed, 8.38c** |
| Backup | Reload!'s borrow arm alone, with the borrowed Attack's target policy narrowed to heroes | selector **owed, 8.38c**; deferred as a p.20 Handicap regardless |
| Bounce | `bounce-move -> synthetic-direct-move@1 (A, exactly 2, straight line, ignoring obstacles; M/One)`; `bounce-repeat -> repeat-action@2 (max=1; May/One; ActionEnded)` gated on S already being resolved | move and repeat registered; the self-referential resolved gate reuses 8.37a's opt-in reading |
| Bullet Time | Ultimate continuous source declaring `after-basic-card-resolution-trigger@1 (controller=A)`; `bullet-borrow -> borrow-primary-action@1 (card=the resolved basic card; May/One)` under a per-grant `restrict-action@2 (exception: an enemy hero already taken this turn through this grant)` | trigger condition and the per-grant restriction are **owed, 8.38c** |
| Profile, dashboard, standee | `hero-content-definition@1 (hero-content/swift, team slot, deck, profile/dashboard references; validation-only)` and ordinary participant/setup bindings | registered. No rules component supply - SW-03 |

**SW-11. SW-10's count was one contract optimistic, and composition is what found it.** The three Green
faces' "who is not adjacent to terrain" was mapped onto `adjacent-terrain-presence@1`, which Tigerclaw's
Blend Into Shadows already names. That capability exists and answers a **different question**: it takes
no subject and asks whether the ACTOR stands beside terrain, as the condition for a Place he then makes.
Hers qualifies the ENEMY her Skill may reach - a property of a candidate rather than of her own footing.
`subject-adjacent-terrain-presence@1` is minted at milestone 8.38d and declared apart rather than added
as a parameter, by the rule the effect catalog has followed since 8.20: a shared declaration gaining a
field moves every hero who already referenced it. Both read the same `TerrainPresenceQuery`, so a hero
petrified into terrain is terrain to stand beside for hers exactly as for his. **The reusable half: a
capability whose NAME matches the printed words has still to be checked for what it actually asks.**

**SW-10. Fourteen of her nineteen faces need nothing minted.** What is owed falls on three clean lines -
two printed target narrowings, two placement destination policies plus a multi-subject push, and three
genuinely new contracts (a card selector, a counted next-turn bounty, and a card-resolution Ultimate
trigger) - which is what milestones 8.38a, 8.38b and 8.38c are drawn on. Reading her rows against what is
registered rather than against any prior slice's completion is the practice
[POST_PITCH.md](../Roadmap/POST_PITCH.md) records after three heroes in a row found sentences no planned
slice had built.

## Open questions this hero raises

**SW-Q1. Does "your rightmost resolved card" mean the turn area's committed order, or the physical
right-hand end of a player's own layout?** The engine has a committed order and no physical layout, so
the first reading is the only one it can hold, and it is what the contract map above assumes. The
question is recorded because the two readings could differ for a player who arranges their turn area
otherwise, and because Reload! and Backup are the only faces in the dealt roster that read the turn
area's order at all. Not blocking: both readings agree whenever a hero has resolved exactly one card,
which is every first turn of a round.

**SW-Q2. Does Bullet Time's "you cannot target the same enemy hero twice in the same turn this way"
count a target taken by an ordinary action earlier in the same turn?** The words "this way" scope the
prohibition to actions performed through the Ultimate, which is the reading the contract map assumes and
the narrower of the two. Recorded because the alternative reading would make an ordinary Attack
constrain a later granted one. **Implemented on the narrow reading at milestone 8.38d, and measurement
there narrowed the question further**: her only two dealt basics, Reload! and Bounce, both print SKILL
primaries, so the grant never hands back an Attack directly and the only Attack it can reach is the one
Reload!'s own text borrows from inside the granted instance. The exclusion therefore follows the grant's
whole CHAIN. This also retires milestone 8.38c2's closing reading that the clause was about several basic
cards' grants in one turn - a turn resolves one card, so there are never several.

**SW-Q3 IS SETTLED, AND THE ANSWER IS YES - through Bullet Time, measured at milestone 8.38e.** A driven
turn shows the grant handing Bounce's own primary back after Bounce resolved, which is exactly the second
performance the clause describes. **And since milestone 8.54 the REPEAT holds there too** - the user's ruling of 2026-09-19: "Bounce through Bullet Time counts as already resolved. Bounce moves 2, the card resolves, the Ultimate grants the primary on the resolved card, and that performance offers the repeat. Three moves of 2." Until 8.54 it did not: 8.25e commits a card's resolution status below the `ActionEnded` checkpoint where Bullet Time grants, so the state still read Bounce as unresolved at the granted performance's gate. The gate now also asks which performance is running (`BasicActionQuery.IsPerformedOffACardAlreadyResolved` - true for a text-granted performance, false for p.10's repeat and for a reaction), inside the `countResolvedPlayed` opt-in only; Blizzard's binding, the only other one, cannot move. The original note follows.

**AND IT WAS STILL DEAD TEXT THROUGH RELOAD!, UNTIL MILESTONE 8.48.** The 2026-09-18 playtest reported it in
one sentence - "Swift's gold didn't let me play her silver twice" - and the cause was the gate's reading, not
the borrow: Bounce's `bounce-resolved` node asked `synthetic-source-card-in-discard@1` on the NARROW reading,
true only of a card in the discard pile, while a card resolved this round sits in the turn area until the
round ends - and Reload!'s pool names precisely such a card. Blizzard met the same shape at 8.37a and opted
into the wide reading with `countResolvedPlayed`; Bounce now does the same at its one call site, so a borrow
through Reload! offers the repeat and Bounce's own turn still does not (`BounceBorrowedRepeatTests`). Open:
through BULLET TIME the granted performance runs while Bounce is still mid-resolution in the engine's state,
so the repeat is not offered there - whether p.16's "when a basic card resolves" should count that card as
already resolved is a ruling for the user.

**SW-Q3. Does Bounce's "if this card is already resolved as you perform this action" ever hold?** It is
the same shape as Blizzard's "if Blizzard is in your discard", which milestone 8.37a settled as open
question 34 and implemented on the wide reading, opt-in at exactly one call site. Bounce's own text is
narrower and self-referential: it asks about the card being resolved rather than discarded, and the
moment it asks is inside an action performed FROM that card. The likely answer is that it holds when the
action is a borrowed or repeated performance rather than the card's own turn, which is what makes the
clause do work beside Reload! and Bullet Time. Settled at composition, not here.

**SW-12. Her dealing corrected twenty-seven capability BINDINGS across ten of her eighteen faces, and
none of them changed what a face says.** Milestone 8.38e ran the effect-definition validator a match
composition runs - the one tier a composition milestone cannot reach - and every violation was a guessed
parameter or output name where the capability's own schema was there to be read: Reload! passed a literal
`range` to a capability that reads the printed Range ICON (and Reload! prints none, so `choose-target@2`
with an explicit distance is the vocabulary an unprinted "adjacent to you" has, exactly as Misa's Leaf on
the Wind uses); Bounce and the two bounty faces bound `subject` where `synthetic-direct-move@3` declares
`mover`; both Shotguns bound a distance where `choose-target@2` wants `origin`, `reach` and `unitKind`,
with `RosterQueryOrigin.EnclosingTargetBeforeConsequences` being the anchor the scatter always meant; the
three Jumps omitted the required `ignoresObstacles`, which is FALSE for them because "ignoring obstacles"
is printed on the Place she makes of herself and never on what she moves of the enemy's; and the three
Green faces put `negate` on `synthetic-is-flagged@1`, which declares none. **The negation moved onto the
predicate**, which is where this build puts one - `subject-adjacent-terrain-presence@1` gained an optional
`negate` on the convention `SyntheticConditionCapabilities`' own predicates set - because a gate that
could invert its own input would let two readings of one predicate exist. No printed value, no printed
text and no ledger reading above moved; this row records that the CONTRACT MAP's bindings were wrong and
the inventory was not.

## Ledger rows this hero owns

| Row | Subject | State |
| --- | --- | --- |
| SW-01 | The second-printing rulebook is silent about her; the extraction is sole authority | Settled |
| SW-02 | Stat bars measured; base and conditional sections are not separable on a gold frame | Settled, with the limit stated |
| SW-03 | She owns no token and no marker | Settled |
| SW-04 | She prints no Movement primary and no Defense primary | Settled |
| SW-05 | Bounce's single secondary is a Defense in the Movement slot | Settled by glyph shape |
| SW-06 | Every item icon identified against a governed reference face | Settled |
| SW-07 | Suppress's missing comma, transcribed as printed | Settled |
| SW-08 | Mark for Death's lowercase "the", transcribed as printed | Settled |
| SW-09 | Her only +1 Radius item is printed on a Red card | Settled |
| SW-10 | Fourteen of nineteen faces need nothing minted | **Corrected by SW-11** |
| SW-11 | The audit mapped her Green terrain clause onto an actor-scoped predicate that asks a different question | Settled; `subject-adjacent-terrain-presence@1` minted at 8.38d |
| SW-12 | Twenty-seven capability BINDINGS across ten faces named parameters and outputs their capabilities do not declare | Settled at 8.38e; no printed reading moved |
| SW-Q1 | "Rightmost resolved card" reading | Open, non-blocking |
| SW-Q2 | Bullet Time's target exclusion scope | Open, non-blocking |
| SW-Q3 | Whether Bounce's self-referential gate ever holds | Settled at 8.38e: YES, through Bullet Time's grant; the repeat off that grant holds since 8.54 |
