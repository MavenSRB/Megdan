# Guards Digital - Whisper Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Whisper the Outcast is the eleventh hero this project inventoried, composed, registered, dealt and
player-path gated, the fifth from outside the pitch line-up milestone 8.17 approved, and **the first from
the Devoted Hero pack** rather than from the base box. This shard owns her content inventory,
source-verification status, and any deviation or open question scoped to Whisper alone. A rule system that
spans heroes stays in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger), and a deviation or
open question affecting more than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than
being copied here. Nothing in the source assigns her a pronoun; this shard follows the project's
convention for the dashboards' unnamed heroes and uses "she".

> Milestone 9.1: Blood Frenzy's "repeat up to five times" is Whisper's to stop after any repeat under the user's ruling **W2** ([PRINTED_WORDING](PRINTED_WORDING.md)), applied at milestone 9.1; Blood Fury's plain "repeat once" stays automatic (W1).

> Milestone 9.2: Lesser Evil's and Greater Good's "defeat ... minion(s) adjacent to you" offer enemy minions only under ruling **W4** ([PRINTED_WORDING](PRINTED_WORDING.md)); they admitted either side before.

## Whisper content ledger

The pitch uses Whisper's complete progression. Its implementation-grade inventory contains the same fields
the eight shipped hero shards enumerate: stable definition and action identities, content revision, colour
and Tier, upgrade relationships, printed values, exact rules text, ordered steps and choice owners, typed
effect-node bindings, capability identities, item contributions, Active-effect versus continuous-source
classification, and the source-linked scenarios each row rests on.

Her identity is pinned at content revision `whisper-8-35z53-rev-1`, measured rather than predicted by
`VerifiedContentIdentityTests` on a complete unfiltered run. That is her eleventh-pin entry; the ten pins
above it came back byte-identical in the same run, which is what attributes the movement to her content
and to nothing else.

The revision is **re-based rather than incremented**, on the reading milestones 8.35z34 and 8.35z38
settled for Dodger and Misa: her earlier revision was minted under the placeholder id `8.35x-whisper1`,
which is a claim rather than a milestone. Nothing read the old string - no pin named her, no artwork row
named her, and no configuration dealt her - which is exactly what makes the re-base free here and would
not make it free again.

## Whisper source faces and components

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes.

**The pack-wide source ruling governs this whole section.** Under PLAN Ruling 27 the Tabletopia
extraction is the **sole** authority for a pack hero's faces. The second-printing rulebook names neither
Whisper nor any of her nineteen cards - a whole-document search returns zero hits - which is expected of
an expansion hero and is recorded here as an **absence rather than a gap**. Nothing about her content
rests on a rulebook sentence naming her; what it rests on is the pages that govern every hero.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Whisper/whisper*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word is transcribed below |
| Action-card backs | `active-hero-cards/Whisper/whisper*__*__back.png` | 19 | 372x520 | Yes - all nineteen byte-identical (SHA-256 `9C8B8FAD9B6B068D...`), and that hash is distinct from all thirty-one other heroes' deck backs, which is what p.5's symbol-match step asks for |
| Hero dashboard front | `active-hero-cards/_dashboards/Whisper__front.png` | 1 | 850x445 | Partly - title, level track and stat bars read; the item-slot strip is cropped to its legend row |
| Hero dashboard back | `active-hero-cards/_dashboards/Whisper__back.png` | 1 | 903x491 | **No** - a blank Tabletopia placeholder shared by eleven heroes. **This raises no question**: ledger question 14 already ruled that a hero's tokens and markers are defined by that hero's card faces, so a blank back is missing art rather than missing rules, and no Whisper face names a token or marker |
| Hero profile card front | `active-hero-cards/_selection-cards/Whisper__front.png` | 1 | 372x260 | Yes - name, complexity stars, roles and, unusually, **all four stat bars resolve to exact pip counts** |
| Hero profile card back | `active-hero-cards/_selection-cards/Whisper__back.png` | 1 | 372x260 | Yes - the common game-logo back, identical for all thirty-two heroes |

`cards/cards.csv` carries all 38 Whisper rows (19 fronts + 19 backs), every front at 372x520, every back
pointing at the one atlas rectangle (atlas 6, x=2618, y=1566).

**One coverage anomaly, recorded rather than treated as a gap.** `whisper18` (Grim Reaper, the Ultimate)
carries `element_id` **7571129**, outside the contiguous `73872xx` band its eighteen siblings sit in,
while its `public_element_id` **6317966** *is* in sequence with them; its file is also the smallest front
in the set. The reading is that this face was re-uploaded to Tabletopia after the rest of the deck. The
face itself is complete and legible, so nothing is missing - it is worth knowing only because a future
re-extraction diff will show this row moving independently of the others.

The **artwork catalog** addresses her eighteen dealt fronts by index. The mapping follows the EXTRACTION
order rather than the table order below, which matters exactly once: the Handicap card is `whisper19`
while it is the second row of the printed-value table, and the catalog's own source-path regex refuses
that ordinal outright.

## Whisper deck composition

Nineteen printed faces make up p.5's eighteen-card deck plus the p.20 Handicap card that setup returns to
the box. The colour sets have the standard shape without exception: one Gold basic, one Silver basic, one
Tier I with two Tier II and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (p.6 step 11 - the three Tier I cards plus the two untiered basic cards) | Swift Justice, Death Seeker, Crimson Trail, Seeds of Fear, Shadow Step |
| Deck, reachable only through p.15's Level Up and p.16's unlock | the six Tier II cards, the six Tier III cards, and Grim Reaper |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup (p.6), used only for an odd player count (p.20) | Vile Accusation |

## Whisper printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the face prints no
such icon, so that secondary action is unavailable from it. Every card additionally offers p.7's and
p.12-13's iconless secondary actions - Hold, Fast Travel, and the Clear that replaces an Attack. Card text
applies only when the primary action is performed (p.9).

This table is in the same order and uses the same names as the nineteen-row literal in
`WhisperVerifiedContentTests.SourceInventory`, which is what fails if a number drifts.

| Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Swift Justice | Gold | basic | 12 | 2 | 1 | Basic Attack - Ranged | Attack 2 | Range 3 | - |
| Vile Accusation | Gold | Handicap | 11 | 1 | 1 | Basic Attack - Ranged | Attack 1 | Range 2 | - |
| Death Seeker | Silver | basic | 13 | 2 | - | Basic Skill | - | Radius 3 | - |
| Crimson Trail | Red | I | 9 | 4 | 4 | Attack | Attack 3 | Radius 2 | - |
| Sanguine Path | Red | II | 9 | 4 | 4 | Attack | Attack 3 | Radius 2 | Defense |
| Blood Fury | Red | II | 9 | 6 | 4 | Attack | Attack 6 | Radius 5 | Initiative |
| Blood Pilgrimage | Red | III | 10 | 5 | 4 | Attack | Attack 4 | Radius 2 | Movement |
| Blood Frenzy | Red | III | 10 | 7 | 4 | Attack | Attack 7 | Radius 5 | Range |
| Seeds of Fear | Blue | I | 10 | 5 | 2 | Skill | - | Radius 2 | - |
| Sprouts of Panic | Blue | II | 10 | 5 | 2 | Skill | - | Radius 3 | Initiative |
| Lesser Evil | Blue | II | 10 | 5 | 2 | Skill - Ranged | - | Range 3 | Attack |
| Blooming Nightmare | Blue | III | 11 | 6 | 2 | Skill | - | Radius 3 | Initiative |
| Greater Good | Blue | III | 11 | 6 | 2 | Skill - Ranged | - | Range 3 | Attack |
| Shadow Step | Green | I | 3 | 3 | 2 | Skill - Ranged | - | Range 3 | - |
| Shadow Walk | Green | II | 2 | 3 | 2 | Skill - Ranged | - | Range 4 | Attack |
| Cruel Twist | Green | II | 2 | 3 | 2 | Skill - Ranged | - | Range 4 | Defense |
| Creeping Shadow | Green | III | 2 | 4 | 2 | Skill - Ranged | - | Range 4 | Radius |
| Sealed Fate | Green | III | 2 | 4 | 2 | Skill - Ranged | - | Range 4 | Defense |
| Grim Reaper | Purple | IV | - | - | - | Ultimate | - | - | - |

**Death Seeker's icon row was re-read seven times and is transcribed as printed.** It prints ONE secondary
icon and it is the Defense shield, sitting in the slot a two-icon card gives the Movement boot. The face
prints no Movement secondary at all, which is why its Sec Mov column is a dash and not a zero - and it is
a real constraint on play, because the card sits in her opening hand.

**Grim Reaper's whole row is dashes, and that is a statement rather than an omission.** It prints no
Initiative, no icons and no action at all - the same shape Xargatha's Metamorphosis has - which is what
makes it a card with no commitment step: p.16 turns it faceup in the hero's area and its rule fires from
there.

## Whisper exact rules text

Transcribed verbatim from the faces. Every sentence below is what `CardSourceContent.Transcribed` carries
in `WhisperCardContent`.

| Card | Printed text |
| --- | --- |
| Swift Justice | Choose one â€” â€¢ Target a hero in range with an empty discard; After the attack: If able, that hero performs a movement action on the card they defended with, moving full distance in a straight line. â€¢ Target a unit adjacent to you. |
| Vile Accusation | Choose one â€” â€¢ Target a hero in range with an empty discard; After the attack: If able, that hero performs a movement action on the card they defended with, moving full distance in a straight line. â€¢ Target a hero adjacent to you. |
| Death Seeker | If an enemy hero in radius has a card in the discard, choose one â€” â€¢ This turn: You are immune to enemy actions. â€¢ You may retrieve a discarded card. |
| Crimson Trail | Before the attack: You may move up to 1 space if there is an empty minion spawn point in radius in the battle zone. Target a unit adjacent to you. |
| Sanguine Path | Before the attack: You may move up to 1 space for every empty minion spawn point in radius in the battle zone up to a maximum of 2 spaces. Target a unit adjacent to you. |
| Blood Fury | Target a unit adjacent to you. After the attack: If an enemy hero in radius has a card in the discard, repeat once on a different target. |
| Blood Pilgrimage | Before the attack: You may move up to 1 space for every empty minion spawn point in radius in the battle zone up to a maximum of 3 spaces. Target a unit adjacent to you. |
| Blood Frenzy | Target a unit adjacent to you. After the attack: If an enemy hero in radius has a card in the discard, repeat up to five times on different targets. |
| Seeds of Fear | Target an enemy unit in radius occupying a spawn point. Move that unit 1 space. |
| Sprouts of Panic | Target an enemy unit in radius occupying a spawn point. Move that unit up to 2 spaces. |
| Lesser Evil | An enemy hero in range chooses one â€” â€¢ That hero discards a card, if able. â€¢ You may defeat a minion adjacent to you. ( Any option can be chosen, even if it would have no effect ). |
| Blooming Nightmare | Target an enemy unit in radius occupying a spawn point. Move that unit up to 3 spaces. |
| Greater Good | An enemy hero in range chooses one â€” â€¢ That hero discards a card, or is defeated. â€¢ You defeat up to 3 minions adjacent to you. |
| Shadow Step | Place yourself into an empty minion spawn point in range in the battle zone. |
| Shadow Walk | Place yourself into an empty minion spawn point in range in the battle zone. |
| Cruel Twist | Swap with a unit adjacent to you, or with an enemy hero in range with a card in the discard. You may then move up to 2 spaces. |
| Creeping Shadow | Place yourself into an empty minion spawn point in range in the battle zone, or into a space in range adjacent to such spawn point. |
| Sealed Fate | Swap with a unit adjacent to you, or with an enemy hero in range with a card in the discard. You may then move up to 3 spaces. |
| Grim Reaper | When performing basic actions you may choose one, or both. |

**Shadow Step and Shadow Walk print byte-for-byte identical text.** The Tier II printing buys one point of
Range, one of Initiative and an Attack item icon, and nothing else - the same "identical sentence,
different icon" pair Wasp's Shock and Electrocute have.

**Vile Accusation is Swift Justice narrowed in exactly one place.** Its second bullet says "a hero" where
the Gold card says "a unit", and every value is lower. That is the whole of what p.20's handicap does to
this face.

## Whisper upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair below is the choice one Level Up
offers, and the card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Crimson Trail | Sanguine Path (+1 Defense) / Blood Fury (+1 Initiative) | Blood Pilgrimage (+1 Movement) / Blood Frenzy (+1 Range) |
| Blue | Seeds of Fear | Sprouts of Panic (+1 Initiative) / Lesser Evil (+1 Attack) | Blooming Nightmare (+1 Initiative) / Greater Good (+1 Attack) |
| Green | Shadow Step | Shadow Walk (+1 Attack) / Cruel Twist (+1 Defense) | Creeping Shadow (+1 Radius) / Sealed Fate (+1 Defense) |

Her twelve item icons cover all six of p.16's icons, so they reach all four dashboard slots. The
distribution is lopsided - three Attack, three Defense, three Initiative, but only one each of Movement,
Range and Radius.

| Item | Slot | Whisper cards carrying that icon (dealt set of 18 in brackets) |
| --- | --- | --- |
| +1 Attack | 1 | 7 [6] - the cards with an Attack primary |
| +1 Defense | 2 | 18 [17] - all but Grim Reaper |
| +1 Initiative | 3 | 18 [17] - all but Grim Reaper |
| +1 Movement | 4 | 17 [16] - all but Death Seeker and Grim Reaper |
| +1 Range | 4 | 9 [8] - both Gold cards, Lesser Evil, Greater Good, and all five Green cards |
| +1 Radius | 4 | 9 [9] - Death Seeker, all five Red cards, Seeds of Fear, Sprouts of Panic, Blooming Nightmare |

**Two items can never be used by the line that grants them, and it is worth stating.** +1 Range comes only
from the Red Tier III pair, and Red is the one colour whose cards print no Range at all; +1 Radius comes
only from the Green Tier III pair, and Green is the one colour whose cards print no Radius at all.

The two upgrade branches a staged fixture can deal are recorded in `WhisperProgressedHands`, and the split
is content rather than convenience: the FIRST branch (Sanguine Path / Sprouts of Panic / Shadow Walk and
their successors) is the seat that moves, and the SECOND (Blood Fury / Lesser Evil / Cruel Twist and
theirs) is the seat that hands decisions away and exchanges places.

## Whisper hero profile

| Field | Printed |
| --- | --- |
| Hero name | **Whisper** on the profile card; **WHISPER THE OUTCAST** on the dashboard. The two agree - there is no Sabina-style epithet conflict, and the rulebook names neither |
| Complexity rating | **two stars** - p.21's "Complex" |
| Roles | **Damager, Durable, Tactician, Pusher** - four roles |
| Stat profile | four bars on p.21's 1-8 scale: **Attack 7, Defense 4, Initiative 7, Movement 3 with a brighter conditional section of 2 (up to 5)** |
| Level track | the generic 1-8 track with the p.13 Life cost 1/1/1/2/2/2/3/3, identical on every hero's dashboard and therefore not Whisper-scoped data |
| Dashboard item legend | the standard six-icon row, Range / Movement / Radius grouped together at the right, corroborating the slot-4 grouping |

**All four stat bars resolve exactly, which is unusual and is corroborated by the deck.** Attack 7 matches
Blood Frenzy's printed Attack 7, the highest value on any Whisper face; the +2 conditional Movement
matches the card texts p.21 says brighter sections depend on - the Red line's "you may move up to 1/2/3
spaces" and the Green swaps' "you may then move up to 2/3 spaces". The measurement was calibrated against
Sabina's already-verified profile card rather than estimated, and Sabina's own Attack bar spans exactly
the same pixel range as Whisper's, which is an exact-match cross-check.

## Whisper settled rulings

Rulings that span heroes live in [RULES_LEDGER.md](../RULES_LEDGER.md); what follows is the Whisper-scoped
reading of each, and the two designer-tier answers her thread produced.

- **The pack-wide source ruling (PLAN Ruling 27).** The Tabletopia extraction is the sole authority for a
  pack hero's faces. The rulebook naming neither her nor her cards is an absence, not a gap.
- **Ruling 16 - who steers the forced move.** Swift Justice's step-7 rider makes the DEFENDING hero
  perform a move, and that hero's own seat steers it. The basis is a designer answer of 2026-08-19: there
  is no functional distinction between "Player" and "Hero" as far as the rules text is concerned. It is a
  direct Move and not a Movement action, so no Fast Travel substitutes and no movement-action restriction
  or buff touches it; the Movement VALUE is the defended card's own, through the ordinary value pipeline,
  so p.16's +1 Movement item raises it.
- **Ruling 16a - full distance or not at all (user, 2026-09-28).** "If able" governs the whole sentence:
  the hero moves exactly the defended card's Movement value (items included) in one straight line, and a
  direction that an obstacle or the board edge stops short is not offered. When no direction reaches the
  full distance the hero does not move. This supersedes the "as far as possible when blocked" reading the
  shipped `defended-card-forced-move@1` attributes to 2026-09-06, which no rules document recorded as a
  user ruling. Built by milestone 8.115.
- **Ruling 16b - it is a Movement action (user, 2026-09-28).** "For whispers gold its movement action that
  forces you to move as specified. Thats all." The card prints "performs a movement action", so the forced
  move IS a Movement action of the defending hero, constrained to Ruling 16a's full distance in one
  straight line: Movement-action restrictions and raises and after-Movement-action triggers apply to it.
  This supersedes Ruling 16's "direct Move and not a Movement action"; who steers it (the defending
  hero's seat) is unchanged. Built by milestone 8.115. The user added (2026-09-28): the defender's own after-Movement-action triggers DO fire off it ("For Widget, yes it should" - Dragon Knight); built by milestone 8.116.
- **Ruling 16c - the trigger fires right after the forced move (user, 2026-09-28).** Asked when Widget's
  Dragon Knight lets him perform his face-up Skill's primary after Swift Justice forces him to move, the user
  chose "Right after the move": immediately after the forced Movement action, still inside Whisper's Attack
  and before Whisper's own after-action effects, reading "each time after you perform a movement action"
  literally.
- **Ruling 17 - the two-tier modal rule.** An option of a modal is refused only when a mandatory step of
  its branch cannot be taken; being able to apply nothing is not a reason. Lesser Evil's and Greater
  Good's opponent-chosen modals are the canonical case - the bullet that does nothing to the enemy is the
  escape hatch the decision exists to offer - and the designer's own reading of Lesser Evil's parenthesis
  is what makes it printed behaviour rather than a new rule: "It holds no rules text. The card would
  function in exactly the same way if you remove the text in italics."
- **Ruling 18 - printed reading order.** Her three Red step-1 movers print the move BEFORE "Target a unit
  adjacent to you", so the target is named from the space the move left her in.
- **Ruling 19 and its rider - "or both" on a modal Attack.** Grim Reaper's raise turns Swift Justice's
  modal into two complete rulebook p.14 orders inside one action. Nothing constrains the two sequences to
  different units, because the face prints no "(on different targets)" - so both bullets may point at one
  unit provided it still satisfies each bullet's own sentence when that bullet resolves.
- **Ruling 33 - the scope of "immune to enemy actions".** All enemy HERO actions: attack, skill, forced
  move, push, swap, place, discard-forcing. System events are not anybody's action and still reach her -
  the end-of-round Minion Battle, p.8's Lane Push and the round's own cleanup have no acting hero to be
  opposed to. p.18's own exceptions need no clause: "you are never immune to your own actions" and
  "effects that check for unit presence will count immune units" are the page's rules rather than this
  card's.
- **Ruling 34 - "on different targets" is pairwise distinct.** No unit is attacked twice anywhere in a
  chain. See the open questions below for the measured shortfall.
- **Ruling 42, superseded by RULING 53 on 2026-09-08.** Death Seeker's gate is false for every seat at
  p.6's setup. Ruling 42 greyed it; RULING 53 offers it as a legal do-nothing play ("it's a Hold"), and
  her journey `DeathSeeker_IsOfferedOnTheFirstTurnOfRoundOne_AndResolvesAsALegalDoNothingPlay` asserts
  both halves. The shard carried the superseded wording until 9.6 found the gap.
- **The reminder-text answer (designer, 2026-08-19).** Italic parenthetical text holds no rules weight
  anywhere and is printed only on the lowest-tier card of a branch. So Greater Good, which omits Lesser
  Evil's parenthesis, behaves in exactly the same way.

## What her set is the first verified content to state

Two things.

1. **A p.14 timing keyword printed inside a modal bullet.** Swift Justice and Vile Accusation print "After
   the attack:" inside their *first* bullet only, so whether the step-7 seam opens at all is decided at
   targeting time. Brogan's two modal Ranged Attacks are the only other modal card texts in the verified
   roster and neither carries a timing keyword.
2. **An Ultimate that changes how another card's text is read.** Grim Reaper adds no trigger, no value and
   no rule source: it raises the number of options a modal on one of her *basic* cards admits. Every
   earlier verified Ultimate states something true about the board.

## Live implementation corrections

- **8.104 - Crimson Trail's prospective step must count when offering its Attack.** The 2026-09-26
  packaged match revealed Crimson Trail at Whisper's turn but offered only its secondary Movement even
  though a legal one-space card-text step could leave her beside a target. The shared offer walk could
  enumerate a direct Move followed by an actor-origin target, but lost those prospective spaces when
  the Move was the active branch of a satisfied read-only Conditional. The walk now carries the typed
  relocation answer through Conditional, Optional and Sequence containers. The printed condition,
  destinations, adjacent-unit target rule and player rope are unchanged.

## Absences that are content

- **No Defense primary, no Movement primary, no p.14 Block, no `!` icon.**
- **No token and no marker on any face**, so her milestone adds no component content at all. Ledger
  question 14 rules that a hero's tokens are defined by that hero's card faces; hers name none.
- **Every Red card is an Attack and every Blue and Green card is a Skill, without exception.** That is the
  same shape Sabina's set has.
- **Grim Reaper prints neither Range nor Radius**, so retired ledger question 12 has no instance in her
  set.
- **Ledger question 14 has no instance in her set either** - the blank dashboard back it governs is
  missing art rather than missing rules, because no face of hers names a token or a marker.

## Whisper deferred faces

**Vile Accusation** alone, the p.20 Handicap face, under the standing 2026-08-21 decision that parks one
face of every verified hero. Its printed values and words ship as inventory and its player-path row
records the deferral against the content rather than against the row's say-so. What it would need when a
milestone deals it is one call to the Gold card's own composer with its second bullet narrowed to heroes,
which is the only place its text differs.

## Whisper open questions

- **The pairwise-distinct chain (Ruling 34) is not enforced at Blood Frenzy's maximum.** The build's
  repeat exclusion holds a SINGLE target, reset at each instance, so it excludes only the unit the
  previous instance took. At Blood Fury's printed maximum of one the two readings coincide exactly and
  that card is correct; at Blood Frenzy's five they diverge, and the third instance may re-take the
  first's target. **Blood Frenzy is the first printed face in the roster where the distinction exists at
  all** - every earlier different-target repeat has a maximum of one, and Wasp's Center of Mass reaches
  two but binds the same target. The fix is an accumulating exclusion set where a single target is held,
  and that state is persisted, so it is a schema-bumping change. Measured and pinned by
  `WhisperVerifiedContentTests.BloodFrenzy_IsTheFirstAttackRepeat_WhoseDistinctTargetMaximumIsAboveOne`
  and by `WhisperProgressedUiJourneyPlayModeTests.BloodFrenzy_ExcludesOnlyTheInstanceBeforeIt_RecordingTheSingleSlotShortfall`.
- **Swift Justice's first bullet over-offers.** "Target a hero in range **with an empty discard**" is a
  predicate about the candidate, and the printed target policy states which units, how far and which line
  - it cannot state a predicate. The consequence is measured rather than assumed: the bullet admits any
  enemy hero in range, and the step-7 rider inherits the same gap from the other side, firing on whichever
  bullet's sequence took a hero where the face prints it on bullet 1 alone. Both are one change - a
  per-bullet field on the modal-Attack contract's bullet type, which the chain persists, so it forces a
  schema bump.

## What makes her set differ from the eight shipped heroes

**Five of her faces read an enemy's DISCARD PILE, and one reads the negative of it** - which is more
discard-conditioned text than any hero before her prints. That matters more than it looks, because a
discard pile fills by exactly two routes in this build: a card spent as a Defense, and a card some text
made a hero discard. So her kit is a knife that opens only once the other player has paid for something,
and her Gold basic's first bullet is at its widest on the very first turn and narrows from then on.

**Two of her sentences hand a decision to somebody who is not the actor, and they do it in two different
ways.** Lesser Evil and Greater Good put a *modal* to an enemy hero Whisper picked; Swift Justice makes its
target *perform* a move. No hero before her does either.
