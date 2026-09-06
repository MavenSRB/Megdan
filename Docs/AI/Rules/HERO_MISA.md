# Guards Digital - Misa Rules Shard

<!-- operative-claim:F-DOC-023 status:Unresolved -->

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Misa the Samurai is the eighth hero this project inventoried, composed, registered, dealt and player-path
gated, and the second from outside the pitch line-up milestone 8.17 approved. This shard owns her content
inventory, source-verification status, and any deviation or open question scoped to Misa alone. A rule
system that spans heroes stays in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a
deviation or open question affecting more than one hero stays in
[RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. Nothing in the source assigns her a
pronoun; this shard follows the project's convention for the dashboards' unnamed heroes and uses "she".

## Misa content ledger

The pitch uses Misa's complete progression. Its implementation-grade inventory contains the same fields
the seven shipped hero shards enumerate: stable definition and action identities, content revision, colour
and Tier, upgrade relationships, printed values, exact rules text, ordered steps and choice owners, typed
effect-node bindings, capability identities, item contributions, Active-effect versus continuous-source
classification, and the source-linked scenarios each row rests on.

Her identity is pinned at content revision `misa-8-35z38-rev-1`, measured rather than predicted by
`VerifiedContentIdentityTests` on a complete unfiltered run. That is her eighth-pin entry; the seven pins
above it came back byte-identical in the same run, which is what attributes the movement to her content
and to nothing else.

The revision is **re-based rather than incremented**, on the reading milestone 8.35z34 settled one
milestone earlier for Dodger: her earlier revision was minted under the placeholder id `8.35x`, which is a
claim rather than a milestone. Nothing read the old string - no pin named her, no artwork row named her,
and no configuration dealt her - which is exactly what makes the re-base free here and would not make it
free again.

## Misa source faces and components

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Misa/misa*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word transcribed below |
| Action-card backs | `active-hero-cards/Misa/misa*__*__back.png` | 19 | 372x520 | Yes - all nineteen byte-identical |
| Hero dashboard front | `active-hero-cards/_dashboards/Misa__front.png` | 1 | 850x445 | Yes - title, four-turn track, level track with its coin costs, and the item legend row all read |
| Hero dashboard back | `active-hero-cards/_dashboards/Misa__back.png` | 1 | - | No - blank Tabletopia placeholder shared by eleven heroes; answered by ledger question 14 |
| Hero profile card front | `active-hero-cards/_selection-cards/Misa__front.png` | 1 | 372x260 | Partly - name, complexity stars and roles read; the stat bars do not resolve to exact pips |
| Hero profile card back | `active-hero-cards/_selection-cards/Misa__back.png` | 1 | 372x260 | Yes - the common game-logo back |

The **artwork catalog** addresses her eighteen dealt fronts by index, and the mapping was verified
face by face rather than assumed: file index N is the Nth dealt definition in the table order below, and
index 19 is the Handicap card the box takes. Six independently sampled faces spanning every colour block
confirmed it before a row was written.

## Misa deck composition

Nineteen printed faces make up the eighteen-card deck plus the Handicap card setup returns to the box. The
colour sets have the standard shape exactly: one Gold basic, one Silver basic, one Tier I with two Tier II
and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (three Tier I cards plus the two untiered basics) | Watch How I Soar, Swoop In, Challenge Accepted, Sudden Breeze, Focus |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Power Overwhelming |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup, used only for an odd player count (p.20) | Leaf on the Wind |

## Misa printed values

| Card | Init | Sec Def | Sec Mov | Primary | Colour/Tier | Item |
| --- | --- | --- | --- | --- | --- | --- |
| Watch How I Soar | 12 | 2 | 1 | Skill, Range 4 | Gold Basic | - |
| Leaf on the Wind | 11 | 1 | - | Skill, Range 4 | Gold Basic (Handicap) | - |
| Swoop In | 6 | 3 | - | Skill, Radius 3 | Silver Basic | - |
| Challenge Accepted | 9 | 6 | 3 | Attack 5, non-ranged | Red I | - |
| Matter of Honor | 9 | 7 | 3 | Attack 6, non-ranged | Red II | +1 Initiative |
| Power Shot | 9 | 4 | 3 | Attack 3, Ranged, Range 2 | Red II | +1 Defense |
| Worthy Opponent | 10 | 7 | 3 | Attack 6, non-ranged | Red III | +1 Movement |
| Thunder Shot | 10 | 4 | 3 | Attack 3, Ranged, Range 2 | Red III | +1 Defense |
| Sudden Breeze | 10 | 5 | 1 | Skill | Blue I | - |
| Gust of Wind | 10 | 6 | 1 | Skill | Blue II | +1 Attack |
| Dash and Slash | 10 | 6 | 1 | Skill | Blue II | +1 Defense |
| Crushing Squall | 11 | 6 | 1 | Skill | Blue III | +1 Radius |
| Death from Above | 11 | 6 | 1 | Skill | Blue III | +1 Range |
| Focus | 3 | 3 | 2 | Skill | Green I | - |
| Discipline | 2 | 3 | 2 | Skill | Green II | +1 Initiative |
| Living Tornado | 2 | 3 | 2 | Skill, Radius 3 | Green II | +1 Attack |
| Mastery | 2 | 4 | 2 | Skill | Green III | +1 Initiative |
| Storm Spirit | 2 | 4 | 2 | Skill, Radius 3 | Green III | +1 Attack |
| Power Overwhelming | - | - | - | Ultimate, no action | Purple IV | - |

`MisaVerifiedContentTests.EveryPrintedFace_MatchesTheSourceInventory` holds this table against the
definitions face by face; the table is the source and the definition is what a mismatch corrects.

## Misa exact rules text

Transcribed verbatim; `MisaVerifiedContentTests.EveryPrintedSentence_IsTranscribedExactlyAsTheFaceCarriesIt`
pins every one. **She prints no reminder text anywhere in the set** - not one parenthesised italic line,
which is a first for a verified hero and which means there is no clarification here to keep apart from a
rule.

| Card | Printed text |
| --- | --- |
| Watch How I Soar | "Choose one — • Place yourself into a space at maximum range. • Defeat a minion adjacent to you." |
| Leaf on the Wind | "Place yourself into a space at maximum range." |
| Swoop In | "Place yourself into a space in radius adjacent to two or more enemy units. If you do, you may retrieve a discarded card." |
| Challenge Accepted | "Target a unit adjacent to you. After the attack: This turn: Gain +2 [Defense] Defense." |
| Matter of Honor | "…Gain +3 [Defense] Defense." |
| Worthy Opponent | "…Gain +5 [Defense] Defense." |
| Power Shot | "Target a unit in range. After the attack: If the target was at maximum range, you may move it 1 space, to a space farther away from you." |
| Thunder Shot | "Target a unit in range. After the attack: If the target is not adjacent to you, you may move it 1 space, to a space farther away from you." |
| Sudden Breeze | "Move up to 3 spaces in a straight line, ignoring obstacles; you may place an enemy unit you moved through into a space adjacent to you." |
| Gust of Wind / Crushing Squall | the same sentence at 4 and 5 spaces |
| Dash and Slash | "Move up to 4 spaces in a straight line, ignoring obstacles; an enemy hero you moved through discards a card, if able." |
| Death from Above | the same sentence at 5 spaces |
| Focus / Discipline / Mastery | "Next turn: Before you perform a primary action, you may move 1 / up to 2 / up to 3 spaces." |
| Living Tornado | "Swap two units at maximum radius." |
| Storm Spirit | "Swap two units in radius and at equal distance from you." |
| Power Overwhelming | "Whenever you choose one, you may choose two different options instead, in any order. Each time after you place yourself, an enemy hero adjacent to you discards a card, if able." |

## Misa upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is one Level Up's choice, and the
card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Challenge Accepted | Matter of Honor (+1 Initiative) / Power Shot (+1 Defense) | Worthy Opponent (+1 Movement) / Thunder Shot (+1 Defense) |
| Blue | Sudden Breeze | Gust of Wind (+1 Attack) / Dash and Slash (+1 Defense) | Crushing Squall (+1 Radius) / Death from Above (+1 Range) |
| Green | Focus | Discipline (+1 Initiative) / Living Tornado (+1 Attack) | Mastery (+1 Initiative) / Storm Spirit (+1 Attack) |

Her twelve item icons cover all six of p.16's icons - Initiative x3, Defense x3, Attack x3, Movement x1,
Range x1, Radius x1 - and her dashboard's own legend row reads
`ATTACK | DEFENSE | INITIATIVE | RANGE MOVEMENT RADIUS`, a fourth independent source for that grouping
after Wasp's, Arien's and Dodger's.

**Two of her items are minted by cards that print neither icon.** Crushing Squall becomes a +1 Radius item
and Death from Above a +1 Range item, so the Blue Tier III Level Up mints an item whose reach is entirely
outside the Blue set. That is ordinary - an item icon says what a card becomes when set aside, not what it
uses - and it is recorded rather than left for a reader to re-derive. Under RULING 40's item note (p.16 is
general, with no primary/secondary split), a +1 Movement item raises her primary Movement values too; she
prints no Movement primary, so for her it reaches only the sixteen secondary Movement icons.

**Her branches divide her set along the RULINGS rather than along a role, and that is what makes both of
them matter to the gate.** Every ruling the user settled for her is observable on exactly one branch above
Tier I:

- The **first** branch carries RULING 31's escalating free move (Discipline's two spaces, Mastery's three)
  and RULING 29's place-the-unit-you-moved-through rider (Gust of Wind, Crushing Squall).
- The **second** carries RULING 32's whole tense pair - which exists as a pair and cannot be read apart -
  RULING 29's discard rider (Dash and Slash, Death from Above), and both Swap Skills.

So a first-branch Misa has no ranged Attack above Tier I and no Swap at all, and a second-branch Misa
never raises her Defense past Challenge Accepted's +2. Both branches therefore carry their own staged
configurations, and every journey names the branch it needs.

## Misa hero profile

| Field | Printed |
| --- | --- |
| Hero name | Misa on the profile card; **Misa the Samurai** on the dashboard |
| Complexity rating | **two stars** |
| Roles | **Tactician, Durable, Damager** |
| Stat profile | four bars on a 1-8 scale, **not resolvable to exact pips at this resolution** |
| Level track | the standard eight levels with coin costs 1, 1, 1, 2, 2, 2, 3, 3 |
| Pronoun | not stated in any source; this shard uses "she" by project convention |

**Durable is the role her printed set spends the most ink on**, and it is one sentence repeated with one
number: her three Red melee faces print "This turn: Gain +2 / +3 / +5 Defense" and differ in nothing else.

## Misa settled rulings

Every one of these is a user ruling from the 2026-09-06 roster-expansion session, recorded here at her
ceremony and carried in `RULES_LEDGER.md`.

| Ruling | Answer | Where it is implemented |
| --- | --- | --- |
| **RULING 1** (M-01) | "at maximum range/radius" = EXACTLY the printed number | `PlaceDestinationScope.EmptySpacesAtExactlyRadius`; `synthetic-place-at-maximum-range@1`; `choose-unit-pair-at-maximum-radius@1`; `target-was-at-maximum-range@1` |
| **RULING 28** (M-02) | The Ultimate's first clause reaches her own cards' choose-one nodes; on a two-bullet modal "two different options" = BOTH | `RuleModificationKind.ChoiceCardinality`, `ChoiceCardinalityQuery`, and the Choose seam in `EffectResolutionSession` |
| **RULING 29** (M-03) | The Blues' straight line passes through ANY obstacle including the water ring, and must end on a playable space | ALREADY the movement rule's behaviour since milestone 6.8e - **measured, not implemented**. `choose-line-destination@1` is the new chooser |
| **RULING 30** (M-04) | The Ultimate's discard trigger fires ONLY on her own self-placements; a Swap never does (p.18) | `UltimateTriggerCondition.AfterControllerPlacesSelf` + `SelfPlacementQuery` |
| **RULING 31** (M-05) | The Next-turn free move fires ONCE; a repeated instance does not re-arm it | `TimingCheckpoint.BeforePrimaryAction` + the active-effect arm of `BeforeActionTriggerQuery` |
| **RULING 32** (M-06) | The tense difference is deliberate: "was at maximum range" is judged at targeting time, "is not adjacent" at the after-attack step | `target-was-at-maximum-range@1` vs `target-is-not-adjacent@1` - two capability ids, one handler |
| **M-07** | Edition: **by convention only**, and the weakest edition answer of the roster | Asserted as the deck-shape case rather than as a claim about the rulebook |

**RULING 28 has exactly one trigger in her whole set**, and in a handicapped game it has none. Watch How I
Soar is the only modal among her nineteen faces, and p.20 substitutes Leaf on the Wind, which prints no
"Choose one" at all. The ruling records that as printed rather than as a defect.

## What her set is the first verified content to do

- **Print no basic Attack at all.** Her Gold basic, her Silver basic and her Gold Handicap card are all
  Basic Skills. Any rule reading "after you perform a basic attack" can never fire for her, and any
  fixture assuming a hero's Gold card yields a basic Attack meets its first counterexample here.
  `MisaVerifiedContentTests.NoBasicCardOfHers_PrintsAnAttack` is the standing tripwire.
- **Print p.17's "ignoring obstacles" on a movement of her own** - five Blue cards.
- **Have a reach whose candidate set can legitimately be empty.** Every earlier range or radius is a disk;
  hers is a ring, so "no legal destination" is an ordinary answer the offer must grey with its blocker
  named rather than an error.
- **Carry a modification of a control-flow node** rather than of a fact about a piece - the ninth
  `RuleModificationKind`.
- **Need a relational pair constraint.** Storm Spirit's second member is admissible only relative to the
  first, which no earlier selection in this build asks.

## The p.7 consequence her three Green cards inherit

p.7's End of Round Retrieve Cards step "cancels all your Active effects, including 'Next Turn' effects."
So a Focus, Discipline or Mastery played on the round's fourth turn never takes effect at all. Settled
from source; `HERO_TIGERCLAW.md` already records it for the one earlier face that prints the keyword, and
all six of Misa's Active effects declare the cancellation.

## Two bounds on the self-place trigger, stated rather than left to be found

`after-push-trigger@1` interrupts mid-effect; the self-place trigger runs on the after-action rails
instead, because an immediate interrupt needs a new `EffectInterruptKind` member and that enum is
serialized. The trigger therefore fires:

- once per completed **action** rather than once per placement, and
- only through p.9's basic-action supertype.

**For Misa's printed set these coincide exactly**: all three self-placing faces are Basic Skills, none
places her twice, and nothing between her Place and the end of her action moves a hero. A future face that
self-places from a non-basic card, or twice in one action, needs the interrupt and its schema bump -
a **named follow-up rather than a debt**, and milestone 8.35z38 deliberately did not take it, because
per-action and per-placement firing are indistinguishable for every face she prints.

## Absences that are content

- **No rules token and no marker.** No face of hers names, places, gives or targets one, so her dealing
  adds no component content. Retired ledger question 14 settles the blank Tabletopia dashboard back as
  missing art rather than missing rules, exactly as it did for Sabina and Dodger.
- **No Defense primary, no p.14 Block, no Defense/Skill combined card** - the same shape Sabina has.
- **No face names a friendly hero.** Hers is the strongest such absence on the roster: no heal, no grant,
  no friendly selection anywhere in nineteen faces. It is why her staged configurations need no ally seat
  at all, where Brogan's needed one.
- **Neither Swap card can move Misa.** p.10's Picking Targets: both say only "two units".
- **Neither Swap can trigger her own Ultimate.** p.18: a Swap "does not count as movement or placement".

## Misa deferred faces

| Face | Why | Trigger to compose |
| --- | --- | --- |
| Leaf on the Wind | p.20's Handicap card, on the standing 2026-08-21 user decision that covers Wasp's Crackling Dagger, Arien's Ceremonial Blade, Brogan's Leap Forward, Sabina's Bang, Tigerclaw's Nimble Strike, Xargatha's Desperate Cleave and Dodger's Rusty Razor | the first odd-player-count configuration. Its composition is one call to the same helper Watch How I Soar's first bullet uses |

## Misa open questions

None scoped to Misa alone. M-07's edition answer is settled above and carried in the ledger; every other
question her inventory raised was answered by a user ruling in the 2026-09-06 session.

## What makes her set differ from the seven shipped heroes

She is the roster's first hero whose whole kit is about **distance as an exact quantity** rather than as a
bound. Every earlier hero's reaches are disks - "in range", "in radius", "adjacent" - and every one of
hers that matters is a ring or an equality: a space at exactly the printed Range, a pair on exactly the
printed Radius, a pair at one distance from her, a target that was at maximum range when it was bound.
That is one ruling (RULING 1) reaching four faces and two more (RULING 32's tenses) reaching two, and it
is what makes an empty offer an ordinary outcome for her where it is nearly impossible for anybody else.

The second difference is that her Ultimate changes **how a decision is shaped** rather than what a rule is
true of. Every other unlocked Ultimate in this build is a statement about the board or about a hero; hers
raises the ceiling on a choice, which is why it is the only one that needed a modification of a
control-flow node.
