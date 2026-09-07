# Guards Digital - Dodger Rules Shard

<!-- operative-claim:F-DOC-023 status:Unresolved -->

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Dodger the Warlock is the seventh hero this project inventoried, composed, registered, dealt and
player-path gated, and the first from outside the pitch line-up milestone 8.17 approved. This shard owns
her content inventory, source-verification status, and any deviation or open question scoped to Dodger
alone. A rule system that spans heroes stays in the [Rule-system ledger](RULE_SYSTEMS.md#rule-system-ledger),
and a deviation or open question affecting more than one hero stays in
[RULES_LEDGER.md](../RULES_LEDGER.md) rather than being copied here. The rulebook refers to Dodger as
"she" in both its p.12 and p.14 worked examples; this shard follows the source.

## Dodger content ledger

The pitch uses Dodger's complete progression. Its implementation-grade inventory contains the same fields
the six shipped hero shards enumerate: stable definition and action identities, content revision, colour
and Tier, upgrade relationships, printed values, exact rules text, ordered steps and choice owners, typed
effect-node bindings, capability identities, item contributions, Active-effect versus continuous-source
classification, and the source-linked scenarios each row rests on.

Her identity is pinned at content revision `dodger-8-35z34-rev-1`, measured rather than predicted by
`VerifiedContentIdentityTests` on a complete unfiltered run. That is her seventh-pin entry and the first
added to that oracle since it was built at milestone 8.25e7; the six pins above it came back
byte-identical in the same run, which is what attributes the movement to her content and to nothing else.

## Dodger source faces and components

Every file is a hard link into the Tabletopia extraction that `active-hero-cards/README.md` describes.

| Face | Location under `Docs/Guards of Atlantis II - Tabletopia Cards/` | Files | Pixels | Readable |
| --- | --- | --- | --- | --- |
| Action-card fronts | `active-hero-cards/Dodger/dodger*__*__front.png` | 19 | 372x520 | Yes - every printed value, icon and word transcribed below |
| Action-card backs | `active-hero-cards/Dodger/dodger*__*__back.png` | 19 | 372x520 | Yes - all nineteen byte-identical; the emblem is distinct from all thirty-one other heroes' backs |
| Hero dashboard front | `active-hero-cards/_dashboards/Dodger__front.png` | 1 | 850x445 | Partly - title, level track and stat bars read; the item-slot strip is cropped to its legend row |
| Hero dashboard back | `active-hero-cards/_dashboards/Dodger__back.png` | 1 | 903x491 | No - blank Tabletopia placeholder shared by eleven heroes; answered by ledger question 14 |
| Hero profile card front | `active-hero-cards/_selection-cards/Dodger__front.png` | 1 | 372x260 | Partly - name, complexity star and roles read; the stat bars do not resolve (D-10) |
| Hero profile card back | `active-hero-cards/_selection-cards/Dodger__back.png` | 1 | 372x260 | Yes - the common game-logo back |

**Coverage is complete: nineteen distinct front hashes, no gap, no duplicate, no unreadable action face.**
`cards.csv` gives Dodger nineteen rows with contiguous public element ids 6357717-6357735. Three fronts
carry internal element ids outside the main `7435377-7435395` run - `dodger18` is `7439536`, `dodger10` is
`7722006`, `dodger12` is `7722007` - because those slots were re-uploaded in the source setup; the slot
identity is unchanged and all three read cleanly.

Her official circular token is prepared from element `8596247` (`Dodger`, a `VerticalTokens` entry) by
`Tools/Prepare-HeroTokens.ps1`, which measured each face's ring colour to a team rather than taking it from
order: the face at (2792,0) is project Blue and the face at (3096,0) is project Red.

## Dodger deck composition

Nineteen printed faces make up the eighteen-card deck plus the Handicap card setup returns to the box. The
colour sets have the standard shape exactly: one Gold basic, one Silver basic, one Tier I with two Tier II
and two Tier III in each of Red, Blue and Green, and one Purple Tier IV Ultimate.

| Membership | Cards |
| --- | --- |
| Starting hand (three Tier I cards plus the two untiered basics) | Dread Razor, Death Trap, Littlefinger of Death, Shield of Decay, Dark Ritual |
| Deck, reachable only through p.15's Level Up | the six Tier II cards, the six Tier III cards, and Tide of Darkness |
| Set aside as an item (p.16) | whichever card of an upgraded colour/Tier pair the Level Up choice does not take |
| Returned to the box at setup, used only for an odd player count (p.20) | Rusty Razor |

## Dodger printed values

`Sec Def` and `Sec Mov` are the secondary action icons on the left ribbon; a dash means the card prints no
such icon. Card text applies only when the primary action is performed (p.9).

| Card | Colour | Tier | Init | Sec Def | Sec Mov | Primary action | Primary value | Range/Radius | Item icon |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Dread Razor | Gold | basic | 12 | 1 | 1 | Basic Attack - Ranged | Attack 2 | Range 2 | - |
| Rusty Razor | Gold | Handicap | 11 | 1 | 1 | Basic Attack - Ranged | Attack 1 | Range 2 | - |
| Death Trap | Silver | basic | 7 | 1 | - | Basic Skill | - | Radius 4 | - |
| Littlefinger of Death | Red | I | 9 | 5 | 4 | Attack - Ranged | Attack 4 | Range 2 | - |
| Finger of Death | Red | II | 9 | 6 | 4 | Attack - Ranged | Attack 4 | Range 3 | Initiative |
| Burning Skull | Red | II | 9 | 5 | 4 | Attack - Ranged | Attack 3 | Range 2 | Defense |
| Middlefinger of Death | Red | III | 10 | 6 | 4 | Attack - Ranged | Attack 5 | Range 3 | Movement |
| Blazing Skull | Red | III | 10 | 5 | 4 | Attack - Ranged | Attack 4 | Range 2 | Radius |
| Shield of Decay | Blue | I | 10 | - | 3 | Defense | Defense **3+** | Radius 2 | - |
| Vampiric Shield | Blue | II | 10 | - | 3 | Defense | Defense **4+** | Radius 2 | Initiative |
| Weakness | Blue | II | 10 | 4 | 3 | Skill | - | Radius 4 | Attack |
| Aegis of Doom | Blue | III | 11 | - | 3 | Defense | Defense **4+** | Radius 2 | Initiative |
| Enfeeblement | Blue | III | 11 | 4 | 3 | Skill | - | Radius 4 | Attack |
| Dark Ritual | Green | I | 3 | 2 | 2 | Skill | - | Radius 3 | - |
| Darker Ritual | Green | II | 3 | 2 | 2 | Skill | - | Radius 3 | Defense |
| Necromancy | Green | II | 3 | 2 | 2 | Skill | - | - | Attack |
| Darkest Ritual | Green | III | 2 | 3 | 2 | Skill | - | Radius 3 | Defense |
| Necromastery | Green | III | 2 | 3 | 2 | Skill | - | Radius 1 | Range |
| Tide of Darkness | Purple | IV | - | - | - | Ultimate | - | - | - |

**Every Attack she owns is Ranged** - all five Red cards and both Gold cards print `Attack - Ranged`, a
first in the verified roster and what her Sniper role is made of. It also means every one of her attacks is
answerable by a "Block a ranged attack" card. **Death Trap prints no Movement icon at all**, which is a real
constraint on play because it sits in her opening hand, and it is why her player-path row for that face
carries no secondary-movement shape where every other dealt face of hers does.

## Dodger exact rules text

Transcribed verbatim from the faces; printed line breaks are joined with a single space. **Bold** marks
p.17's Active-effect keyword as the card prints it; parenthesised italics are p.9's reminder text.

| Card | Printed text |
| --- | --- |
| Dread Razor | **Choose one —** • Target a unit adjacent to you. • If you are adjacent to an empty spawn point in the battlezone, target a unit in range. |
| Rusty Razor | **Choose one —** • Target a hero adjacent to you. • If you are adjacent to an empty spawn point in the battlezone, target a hero in range. |
| Death Trap | An enemy hero in radius who is adjacent to an empty spawn point in the battle zone discards a card, if able. |
| Littlefinger of Death | **Choose one —** • Target a unit adjacent to you. • Target a hero in range who has one or more cards in the discard. |
| Finger of Death | **Choose one —** • Target a unit adjacent to you. • Target a hero in range who has one or more cards in the discard. |
| Burning Skull | Target a unit in range. After the attack: Move up to 1 minion adjacent to you 1 space to a space not adjacent to you. |
| Middlefinger of Death | **Choose one or both** (on different targets) **—** • Target a unit adjacent to you. • Target a hero in range who has one or more cards in the discard. |
| Blazing Skull | Target a unit in range. After the attack: Move each of up to 2 minions adjacent to you 1 space to a space not adjacent to you. |
| Shield of Decay | +2 [Defense] Defense if there are 2 or more empty spawn points in radius in the battle zone. |
| Vampiric Shield | +2 [Defense] Defense if there are 2 or more empty spawn points in radius in the battle zone. |
| Aegis of Doom | +4 [Defense] Defense if there are 2 or more empty spawn points in radius in the battle zone. |
| Weakness | **This turn:** Enemy heroes in radius have -4 [Attack] Attack. *( They can still attack, even with a negative attack value. )* |
| Enfeeblement | **This turn:** Enemy heroes in radius have -6 [Attack] Attack and cannot repeat actions. |
| Dark Ritual | If there are 2 or more empty spawn points in radius in the battle zone, gain 1 coin. |
| Darker Ritual | If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins. |
| Necromancy | Respawn a friendly minion in an empty friendly spawn point adjacent to you in the battle zone. |
| Darkest Ritual | If there are 2 or more empty spawn points in radius in the battle zone, gain 2 coins; If you have Ultimate, gain +1 [Attack] Attack item. *( Use any spare card from the box wtih the corresponding icon )* |
| Necromastery | Respawn a friendly minion in an empty friendly spawn point in radius in the battle zone. |
| Tide of Darkness | While you are performing an action, all spaces count as if they were in the battle zone and had a friendly minion spawn point. |

**Transcription notes that are printed facts, not typos of this document.** Darkest Ritual's reminder reads
`wtih`, not `with` - verified at 9x magnification - and that line ends without a full stop, where Weakness's
reminder ends with one inside its parentheses. Darkest Ritual also prints a capital `If` immediately after
its semicolon. Both Gold cards print `battlezone` as one word where all nine other spawn-point faces print
`battle zone` as two (D-12). Under the authority hierarchy the face wins; none is corrected.

## Dodger upgrade candidates and item destinations

p.15 upgrades by colour and Tier rather than by named card, so each pair is one Level Up's choice, and the
card not taken becomes the item p.16 tucks under the dashboard.

| Colour | Tier I | Tier II pair (item the untaken card becomes) | Tier III pair (item the untaken card becomes) |
| --- | --- | --- | --- |
| Red | Littlefinger of Death | Finger of Death (+1 Initiative) / Burning Skull (+1 Defense) | Middlefinger of Death (+1 Movement) / Blazing Skull (+1 Radius) |
| Blue | Shield of Decay | Vampiric Shield (+1 Initiative) / Weakness (+1 Attack) | Aegis of Doom (+1 Initiative) / Enfeeblement (+1 Attack) |
| Green | Dark Ritual | Darker Ritual (+1 Defense) / Necromancy (+1 Attack) | Darkest Ritual (+1 Defense) / Necromastery (+1 Range) |

Her twelve item icons cover all six of p.16's icons and reach all four dashboard slots; her dashboard's own
legend row reads `ATTACK | DEFENSE | INITIATIVE | RANGE MOVEMENT RADIUS`, a third independent source for
that grouping after Wasp's and Arien's. Tide of Darkness prints neither Range nor Radius, so **ledger
question 12 has no Dodger instance**, as it had none for Arien or Brogan.

**Her branches divide her kit along a line no earlier hero's do: only the second branch can bring a minion
back.** Necromancy and Necromastery are the Green siblings of Darker and Darkest Ritual, so a first-branch
Dodger holds no Respawn at all. That is content rather than a staging accident, and it is why both branches
carry their own staged configurations and why every journey driving p.19's Respawn names the second.

## Dodger hero profile

| Field | Printed |
| --- | --- |
| Hero name | Dodger on the profile card; **Dodger the Warlock** on the dashboard |
| Complexity rating | one star |
| Roles | **Damager, Sniper, Pusher, Farming, Disabler** - five, the largest role set in the verified roster |
| Stat profile | four bars on a 1-8 scale; approximately Attack 3, Defense 2 with a long brighter conditional section reaching about 5, Initiative 7, Movement 5. **Not resolvable to exact pips at this resolution** - D-10 |
| Pronoun | **her/she**, as the rulebook's own p.12 and p.14 examples use |

The Defense bar's brighter conditional section is unusually long, and p.21 defines brighter sections as
conditional on level-up choices and card texts - consistent with her three Defense primaries, whose own
texts add +2, +2 and +4.

## Dodger source observations

- **The second-printing rulebook illustrates two Dodger faces and both reproduce the extracted faces
  exactly.** Burning Skull II on printed p.12 and Vampiric Shield II on printed p.14 match name, colour,
  Tier, every printed value and the card text, with **no wording difference of any kind**. Vampiric Shield
  appears twice - as the anatomy callout for the `+` notation and in the worked example. That makes Dodger
  the second caveat-free edition after Brogan.
- **p.14's worked example settles three readings from the source rather than by interpretation.** Brogan
  attacks Dodger with Mad Dash for an Attack Total of 6; she defends with Vampiric Shield's printed 4, and
  the page says the card text applies *because Defense is the primary action*, giving +2 for the two empty
  minion spawn points within radius 2 **of her own space**. 4+2=6 against 6 defends, matching p.14's "equal
  or higher". The build reproduces this arithmetic exactly.
- **The rulebook's own count says "empty *minion* spawn points" where the card says "empty spawn points".**
  That difference is the evidence behind D-01 and is the only place the two sources use different words
  about Dodger.
- **p.12's prose corroborates the Gold basic's conditional structure rather than contradicting it.** It
  reads "the only Range 2 attack Dodger has is on her Red card", and Dread Razor also prints Range 2. The
  two agree because Dread Razor's ranged bullet is gated on adjacency to an empty spawn point, so it is not
  an attack she simply has.
- **No Dodger face names a token or a marker**, so ledger question 14 answers her dashboard back without a
  hero-scoped question.
- **No published errata or official ruling affecting any Dodger face was located.**

## Dodger settled rulings

The user settled Dodger's decision list on 2026-09-06. Each ruling below is implemented; the cross-hero
consequences live in [RULES_LEDGER.md](../RULES_LEDGER.md) rather than here.

| Id | Question | Ruling |
| --- | --- | --- |
| D-01 | What "empty spawn points" counts | **Minion** spawn points only, matching the rulebook's own worked example |
| D-02 | What Tide of Darkness's overlay covers | **Any** action she performs, a Defense action included |
| D-03 | Whether granted points satisfy "empty spawn point" | Yes, wherever the space itself is empty |
| D-04 | Darkest Ritual's semicolon | Two **independent** clauses; the item clause fires on its own condition |
| D-05 | The item grant's bound | Literal and unbounded; every performance grants another permanent +1 Attack item |
| D-06 | "Choose one or both" | **Two full p.14 sequences**, each defending separately |
| D-07 | Which minion a Respawn returns | The chosen point's type fixes it; heavies are excluded by p.19's arithmetic, not by a written rule |
| D-08 | What "-4/-6 Attack" reduces | The Attack **Total**, as a signed contribution, with **no floor at zero** |
| D-09 | What "cannot repeat actions" forbids | Only p.10's **Repeat** keyword; replacement actions remain available |

**A designer ruling settles the anchoring of the Defense clause.** The official `rules-questions-goa2`
channel, 2026-07-25, on Aegis of Doom: asked whether Dodger can be outside the battle zone and still get
the +4 as long as the empty spawns are in it, the designer answered "Sure, why not." The condition is
therefore **zone-anchored, not self-anchored** - the counted points must be in the Battle Zone; she need
not be. The build measures this rather than assuming it.

**RULING 48 supersedes the D-03 reading milestone 8.35z28 shipped.** The user settled it in one sentence -
"Respawn anywhere in reach" - and it changes what a granted point is *for*. Post-Ultimate she may respawn
into any empty space within the card's reach, because the Ultimate's whole content is that every space has
a friendly minion Spawn point. A granted point still names no type; what changed is the conclusion drawn
from that silence. p.19 answers the type question from its own side - the page's eligibility count already
says which types may return - so the type comes from that count rather than from the point, heavies stay
structurally excluded by RULING 12's derivation, and where the page admits more than one type p.10's
default hands the choice to the actor. At a **real printed** spawn point RULING 12's destination-fixes-type
still governs.

## Dodger open questions

- **D-10**: her profile card's four stat bars do not resolve to exact pips at this extraction's resolution.
  Recorded as unreadable rather than estimated. It blocks no implementation: no rule reads a profile bar.
- **D-12**: both Gold cards print `battlezone` as one word where her nine other spawn-point faces print
  `battle zone` as two. The faces are transcribed as printed; no correction is applied and none is implied.

## Dodger deferred faces

One of her nineteen faces carries a named deferral rather than a composition, and it states its trigger in
the content itself rather than here.

- **Rusty Razor** is p.20's Handicap card, parked under the standing 2026-08-21 user decision that no
  configuration of this build composes an odd player count. p.6 returns it to the box.
- **Middlefinger of Death** was parked on the **attack-sequence bullet's subject narrowing** through two
  successive narrowings rather than an inherited one - phase one on a question the user's D-06 ruling
  answered, phase two on a seam, having measured that the only shape then producing a second complete
  p.14 order was p.10's Repeat, which Enfeeblement is ruled to forbid. Milestone 8.35z27 built that seam,
  and **milestone 8.35z45 composed the face**. Its second bullet declares one closed enum member on
  `AttackSequenceBullet` - "has one or more cards in the discard" - evaluated per candidate at that
  bullet's sequence start over the set p.13's targeting already admits, so reach and p.18's immunities
  are asked once and unchanged while the printed sentence narrows the answer. It went on the BULLET and
  not on the printed target policy every Attack declares, which is what kept it from re-identifying every
  dealt hero; it moved the snapshot schema alone, v38 to v40, rejecting rather than migrating.

## What makes her set differ from the six shipped heroes

- **One board fact drives nine of nineteen faces.** Both Gold cards, the Silver basic, all three Blue
  Defense primaries and three of the five Green Skills condition on empty spawn points in the Battle Zone.
  No shipped hero has a single derived board quantity carrying this much of a deck, and the build answers
  all nine through one derivation reached three ways - a count, a Defense value, and a selection narrowing.
- **She is the first hero who is paid for the board emptying out.** Her rituals farm coins from vacated
  spawn points, her shields harden as the zone empties, and her Necromancy pair refills the very points the
  rest of her deck wants empty. That internal tension is content, not a defect.
- **Six firsts in one deck**: the first conditional printed value (`3+`), the first "choose one or both",
  the first restriction on Repeat, the first card-text item grant, the first card-text minion respawn, and
  the first negative value channel with an explicit no-floor reminder.
- **Her Ultimate is a global overlay rather than a triggered ability.** Tide of Darkness rewrites what every
  space on the board *counts as* for the duration of her own actions, which is how it turns the other nine
  faces' condition on. It starts no step and asks nobody anything, so its player-path row claims only that
  a player can see it - the shortest face row in this build.
- **At p.6's setup none of that is switched on**, and it is measured rather than assumed: every minion Spawn
  point inside the Battle Zone is occupied, so her count reads zero and p.19 admits nothing back. Her kit
  switches on as the wave dies or once the Ultimate is up.
