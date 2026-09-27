# Guards Digital - Mrak Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-fifth-hero sequence](../Roadmap/PHASE_8_TWENTY_FIFTH_HERO.md) owns acceptance and status.

## Source and rule authority

Mrak the Rockshaper is the selected twenty-fifth hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Mrak/` holds nineteen byte-distinct fronts and nineteen backs sharing one MD5 (`faf716cbaa343936daab90cde2e2ed2f`). All nineteen fronts, the selection profile and the dashboard were read at full resolution on 2026-09-27. The profile (SHA-256 `1f478496cbe690ef549c5edc9e29624d22f43f1178e02386a8b122d5fc42ecc9`) shows **three stars** and **Melee, Tokens, Durable, Disabler, Tactician**. The dashboard (SHA-256 `0eff7446c6f8375e6f93ce5dd7f105231bdcfd25a209bc908d9966c313c0d7f4`) gives the title "Mrak the Rockshaper" and four ordinary turn slots. These are source references, not distribution permission.

MR-01, stat bars: a centre-scanline pixel reading calibrated on Hanu's recorded bars (7 px per pip) gives Attack **8**, Initiative **1**, Defense **8**, Movement **2** plus **2** brighter pips (p.21: brighter sections depend on level-up choices and card text). The dashboard's bottom bars agree by eye.

MR-02, manifest (`extracted/text-assets/manifest-bundled__42.json`): standee **`Mrak`** element 8629814 (publicElementId 4745528, VerticalTokens, `Image_2_Atlas` sprites from (489,3048), 302x302) and one token element **`Rockkk`** 7355742 (publicElementId 4609784, Tokens/Hexagonal, `Image_0_Atlas` (3131,2233) 337x289, a second sprite beside it). The names do not set gameplay kind or supply; 8.112g confirms both crops against the printed rock glyph and the profile portrait before import. The extraction does not carry the physical supply count (MR-Q1).

MR-03, rulebook: the local second-printing text names no Mrak, Rockshaper or card title. General authority is p.9-10 (Range/Radius icons, modal choices, repeat, "if able", changing card states, discarding from other states), p.11 (movement in card text), p.12-14 (Attack actions, the non-Ranged adjacency rule, Clear, Defense discards, Attack Total), p.17 (Active effects and their cancellation), p.18-19 (Ignoring Obstacles, Obstacle, Place and short supply, Push, Straight Line, Terrain, Tokens) and p.20 (Handicap). Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

MR-04, the corner letters (D, S, R, B, G) printed at each front's top-right do not track the card colour (Blue Stomping Step and Ground Shaker print `G`; Green Rolling Stone and Strolling Stone print `B`). Colour, tier and upgrade line are read from the frame, ribbon and primary-icon colour, which agree on every face; the letters state no rule and are not used.

## Printed inventory

File stems below are under the Mrak source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons and **SA** its crossed-swords secondary Attack icon; A/R/r are the primary action's Attack, Range and Radius icons. A missing icon is absent, never zero. Items are the independent bottom glyphs, verified by montage against Trinkets' six recorded item icons; Range and Radius glyphs were verified the same way.

| Stem | Face / colour / tier | Init | D | M | SA | Primary / icon | Item |
| --- | --- | ---: | ---: | ---: | ---: | --- | --- |
| mrak__7430505 | Fissure / Gold / Basic | 11 | 3 | 1 | - | Basic Attack / A 4 | - |
| mrak2__7430510 | Stone Grip / Silver / Basic | 12 | 4 | - | - | Basic Skill - Ranged / R 3 | - |
| mrak3__7596606 | Seismic Slam / Red / I | 7 | 7 | 3 | 6 | Skill / r 3 | - |
| mrak4__7596607 | Seismic Assault / Red / II | 7 | 8 | 3 | 7 | Skill / r 4 | Initiative |
| mrak5__8176070 | Stone Carapace / Red / II | 7 | 6 | - | 7 | Movement 4 | Defense |
| mrak6__7596611 | Epicentre / Red / III | 8 | 8 | 3 | 7 | Skill / r 4 | Range |
| mrak7__8176071 | Rock Solid / Red / III | 8 | 6 | - | 7 | Movement 4 | Defense |
| mrak8__7430502 | Boulder Rush / Blue / I | 8 | 6 | 2 | - | Skill | - |
| mrak9__7430515 | Boulder Blitz / Blue / II | 9 | 7 | 2 | - | Skill | Defense |
| mrak10__7583415 | Stomping Step / Blue / II | 9 | 7 | 2 | - | Skill / r 4 | Attack |
| mrak11__7430509 | Boulderdozer / Blue / III | 9 | 7 | 2 | - | Skill | Movement |
| mrak12__7583417 | Ground Shaker / Blue / III | 9 | 7 | 2 | - | Skill / r 4 | Attack |
| mrak13__7572600 | Treacherous Ground / Green / I | 6 | 4 | 2 | - | Skill - Ranged / R 3 | - |
| mrak14__7583411 | Rockslide / Green / II | 5 | 4 | 2 | - | Skill - Ranged / R 4 | Attack |
| mrak15__7583412 | Rolling Stone / Green / II | 5 | 4 | 2 | - | Skill | Initiative |
| mrak16__7583413 | Avalanche / Green / III | 5 | 5 | 2 | - | Skill - Ranged / R 4 | Radius |
| mrak17__7583414 | Strolling Stone / Green / III | 5 | 5 | 2 | - | Skill | Initiative |
| mrak18__7659508 | Rock and a Hard Place / Purple / IV | - | - | - | - | Ultimate | - |
| mrak19__7430501 | Lesser Fissure / Gold / Handicap | 10 | 2 | 1 | - | Basic Attack / A 3 | - |

Items: Attack x3, Defense x3, Initiative x3, Movement x1, Range x1, Radius x1. Upgrade lines: Red Seismic Slam -> Seismic Assault or Stone Carapace -> Epicentre or Rock Solid; Blue Boulder Rush -> Boulder Blitz or Stomping Step -> Boulderdozer or Ground Shaker; Green Treacherous Ground -> Rockslide or Rolling Stone -> Avalanche or Strolling Stone. p.15's level-up list is validated as a whole at dealing (the Snorri lesson: a Tier II names both Tier III faces of its colour).

### Printed clauses

Quoted from the fronts; each implementation test uses the front, not this routing copy.

- **Fissure:** "Target a unit adjacent to you. After the attack: Place a rock token in each of the first three empty spaces in the straight line from you in the direction of the attack."
- **Stone Grip:** "Target an enemy hero in range; place exactly 3 rock tokens into empty spaces adjacent to the target and farthest away from you."
- **Seismic Slam / Seismic Assault:** "An enemy hero in radius adjacent to terrain, or to a rock token, discards a card, or is defeated." Seismic Slam alone adds "(The secondary Attack action is non-ranged)".
- **Epicentre:** the same clause, then "Repeat once on a different target."
- **Stone Carapace:** "This round: If you would discard a card from your hand, you may discard this card instead; you may discard this card to perform its defense action, as if it was in your hand."
- **Rock Solid:** "You may retrieve a discarded card." then Stone Carapace's This round text.
- **Boulder Rush / Boulder Blitz / Boulderdozer:** "Push a token, or an enemy unit, adjacent to you 1 or 2 spaces [1, 2 or 3 / 1, 2, 3 or 4], ignoring obstacles; you may move up to 2 [3 / 4] spaces in the direction of the push, ignoring obstacles."
- **Stomping Step:** "Move a unit in radius which is adjacent to terrain, or to a rock token, 1 space. Place a rock token in the space it occupied." **Ground Shaker** adds "May repeat once on a different target."
- **Treacherous Ground / Rockslide:** "You may move a unit in range 1 space to a space adjacent to terrain, or a rock token." **Avalanche** adds "May repeat once."
- **Rolling Stone:** "Move any number of spaces in a straight line ignoring obstacles, without moving through more than one empty space. (The starting space and the destination space do not count.)" **Strolling Stone:** the same with "more than two empty spaces" and no parenthetical.
- **Rock and a Hard Place:** "Once per turn, after you place one or more Rock tokens into one or more spaces adjacent to one or more enemy heroes, each of those heroes discards a card, if able."
- **Lesser Fissure (boxed Handicap):** Fissure's text with "Target a hero adjacent to you".

## Readings the rulebook settles

- **Rock tokens** are tokens: obstacles (p.19), removed at the end of the round and not on the placer's defeat (p.8, p.19), cleared by any hero's Clear (p.13), taken from a finite supply with p.18's reclaim-from-board rule when short. No face prints an exception, so none is added.
- **The printed secondary Attack** (SA) is the first in the roster. p.9 names only Movement and Defense secondary icons, so this is a new icon the card defines, performed under p.12-14's general Attack: no card text (p.9 "only when performing the Primary action"), non-Ranged, so only a unit adjacent to Mrak (p.12: "even if the card has a radius icon", which Seismic Slam's note restates), Attack Total = printed value + Attack items, the ordinary hero Defense and minion defeat. p.13's Clear may replace it as it replaces any Attack action.
- **Stone Carapace and Rock Solid** create This round effects on a resolved card. p.10 allows discarding from other states when the text specifies it; p.17 cancels the effect when the card changes state, so the substitution is used at most once. The first clause covers discard effects addressed to Mrak's hand; the second lets the resolved card be the Defense card, applying its own Defense value and not its Movement text. The effect also ends on defeat and at round end.
- **Push ignoring obstacles** (p.18): the pushed token or unit passes through obstacles as long as it can reach an empty space, otherwise it goes as far as possible (0 is legal). The follow move is Mrak's own, optional, a straight line along the push's direction, ignoring obstacles and never ending on one (p.18 Ignoring Obstacles). Its direction is fixed by the chosen target, so it remains available after a zero-space push; the text sets no condition on it.
- **Rolling/Strolling Stone** is one straight-line move ignoring obstacles. Every intermediate space is counted and at most one (two) may be empty; the destination must be empty (p.18). Whether "any number" admits standing still follows the settled Garrus reading (GA-Q1, a zero move is legal) rather than a new ruling.
- **Avalanche's** "May repeat once" has no "different target" wording, so p.10's repeat, "with the same range, restrictions", allows the same unit. Epicentre and Ground Shaker print "different target".
- **Rock and a Hard Place** listens for Place (p.18). A push or a Rolling Stone move of a rock is not a placement; a p.18 short-supply reclaim followed by placing is. It fires once per turn, after the placing step completes, for every enemy hero adjacent to any rock that step placed.

## Capability mapping

Existing contracts to reuse (each block re-probes before claiming reuse): token definitions with supply and end-of-round cleanup (Ignatia's magma, Tali's Ice); `SyntheticTokenCapabilities` Place with its rock-token parameter comment; the 8.88 composable selection whose adjacency-to-kind arm already names Mrak's "adjacent to terrain, or to a rock token"; `TerrainPresenceQuery`; discard-or-defeat and different-target repeats (Trinkets' Grid, Snorri's Runebomb); `ObjectPushV3` / `PushTargetV3` with `pushIgnoresObstacles`; `FindDirectMoveDestinations(moveIgnoresObstacles)`; Clear's replacement offer, generic over the replaced Attack; retrieval; the after-push trigger family as the precedent for an after-place trigger.

New contracts, each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| Rock token definition, supply (MR-Q1), obstacle, cleanup | all rock faces | 8.112a |
| Printed secondary Attack: non-Ranged, value, items, no text, Clear, bots, HUD control | six Red faces | 8.112a |
| After-place trigger fact: which placement step, which spaces, once per turn | Ultimate | 8.112a (seam), 8.112f (card) |
| Place in the first N empty spaces of a straight line in the Attack's direction | Fissure | 8.112b (MR-Q2) |
| Place exactly N tokens adjacent to a target, farthest from the actor | Stone Grip | 8.112b (MR-Q3) |
| Resolved-card discard substitution and Defense | Stone Carapace, Rock Solid | 8.112c (MR-Q4) |
| Push-then-follow along the push direction | three Boulder faces | 8.112d |
| Move a filtered unit 1 space, then place into its vacated space | Stomping Step, Ground Shaker | 8.112d |
| Straight-line move with an empty-space budget | Rolling, Strolling Stone | 8.112e |

## Open questions

Each blocks only the block named.

- **MR-Q1 (user, settled 2026-09-27):** "Mark has 3 tokens in supply" - Mrak's rock supply is **three**, with p.18's short-supply step taking standing rocks back when a placement needs more (Fissure's three and Stone Grip's exactly-three can each spend the whole pile).
- **MR-Q2 (user):** Fissure's "first three empty spaces in the straight line" - literal reading: walk outward from Mrak through the target's space and place in the first three empty spaces met, skipping occupied spaces and terrain until the board ends. The alternative stops at the first non-empty space. Blocks 8.112b.
- **MR-Q3 (user):** Stone Grip's "exactly 3 ... farthest away from you" - when fewer than three empty adjacent spaces share the greatest distance (off-line targets have two), do the next-farthest fill the rest; and when fewer than three empty adjacent spaces exist at all, is the clause unappliable (place none, p.10) or does it place as many as possible? Blocks 8.112b.
- **MR-Q4 (user):** Stone Carapace with an empty hand: when Mrak "discards a card, or is defeated", may the resolved Carapace be discarded instead to avoid defeat? Blocks 8.112c.
