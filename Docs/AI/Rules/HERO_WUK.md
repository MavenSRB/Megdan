# Guards Digital - Wuk Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-sixth-hero sequence](../Roadmap/PHASE_8_TWENTY_SIXTH_HERO.md) owns acceptance and status.

## Source and rule authority

Wuk the Grove Keeper is the selected twenty-sixth hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Wuk/` holds nineteen byte-distinct fronts and nineteen backs in two byte variants of one visible back (14 x MD5 `04aa58d6856d23b22fd0e77a56a7ac8c` on atlas 13, 5 x MD5 `b34aaf86d6504f4de628963abbde698e` on atlas 14). All nineteen fronts, the selection profile and the dashboard were read at full resolution on 2026-09-28. The profile (SHA-256 `3231da35f15271649fd80fea08a64a1dab869726b9e720d3948b4aa125785f23`) shows **three stars** and **Tokens, Pusher, Durable, Sniper, Tactician, Healer**. The dashboard (SHA-256 `e7f3fad9b0d6ce13cc15c68acf2d46d6f5171fdb3c6a4622f0f11319c49ad0ff`) gives the title "Wuk the Grove Keeper" (the manifest's group name is "Wuk the Grovekeeper"; the printed dashboard is displayed) and four ordinary turn slots. These are source references, not distribution permission.

WU-01, stat bars: a centre-scanline pixel reading at 7 px per pip, calibrated on Hanu's and Mrak's recorded bars and checked by enlargement, gives Attack **5**, Initiative **1**, Defense **8**, Movement **3** plus **1** brighter pip (p.21). The Movement split is the least certain reading; 8.114f re-checked it on an enlargement of the dashboard's bottom bars and it holds: three pips and one brighter.

WU-02, manifest (`extracted/text-assets/manifest-bundled__42.json`): Wuk's standee is element **8597495**, which the manifest names **`Hanu`** (publicElementId 1024393, VerticalTokens, two 302x302 faces in `Image_1_Atlas`); Hanu's own standee is 8597492, and [Hanu's shard](HERO_HANU.md) already recorded that the second `Hanu`-named element draws another hero. A crop of 8597495 matches Wuk's profile portrait and the standee the user photographed in Tabletopia on 2026-09-28. The Tree token is three hexagonal elements, **`Sapling`** 1421886, **`Sapling 2`** 1421885 and **`Sapling 3`** 1421884 (`Image_0_Atlas`, y 2526, x 1372 / 1035 / 686, 335x291), three distinct tree paintings. Element names set neither gameplay kind nor supply.

WU-03, rulebook: the local second-printing text names no Wuk, Grove Keeper or card title. General authority is p.9-10 (Range/Radius icons, modal choices, repeat, mandatory and optional steps), p.8 (minion battle), p.11 (movement in card text), p.12-14 (Attack actions, Defense), p.15 (level-up), p.17 (Active effects), p.18-19 (Ignoring Obstacles, Immune, Obstacle, Place and short supply, Remove, Straight Line, Swap, Target, Tokens) and p.20 (Handicap). Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

## Printed inventory

File stems below are under the Wuk source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons; A/R/r are the primary action's Attack, Range and Radius icons. A missing icon is absent, never zero. Ribbon glyphs, Range/Radius icons and items were verified by shape in montages beside Wasp's six recorded item icons and her Range and Radius faces, never by slot position.

| Stem | Face / colour / tier | Init | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| wuk__7721594 | Tree Slam / Gold / - | 11 | 3 | 1 | Basic Attack - Ranged / A 4, R 2 | - |
| wuk2__7721586 | Mystic Saplings / Silver / - | 4 | 4 | - | Basic Skill / r 3 | - |
| wuk3__7721582 | Nature's Protector / Red / I | 7 | 7 | 4 | Attack - Ranged / A 5, R 2 | - |
| wuk4__7721585 | Nature's Guardian / Red / II | 7 | 8 | 4 | Attack - Ranged / A 5, R 3 | Initiative |
| wuk5__7721580 | Trample / Red / II | 7 | 7 | - | Movement / 4 | Defense |
| wuk6__7721592 | Nature's Champion / Red / III | 8 | 8 | 4 | Attack - Ranged / A 6, R 3 | Initiative |
| wuk7__7721589 | Angry Stampede / Red / III | 8 | 7 | - | Movement / 4 | Radius |
| wuk8__7721587 | Gifts of Nature / Blue / I | 8 | 5 | 2 | Skill / r 3 | - |
| wuk9__7721588 | Tree of Plenty / Blue / II | 9 | 6 | 2 | Skill / r 4 | Attack |
| wuk10__7721579 | Claim Dominance / Blue / II | 9 | 6 | 2 | Skill / - | Initiative |
| wuk11__7721581 | Abundance / Blue / III | 9 | 6 | 2 | Skill / r 4 | Range |
| wuk12__7721591 | Assert Dominance / Blue / III | 9 | 6 | 2 | Skill / - | Movement |
| wuk13__7721584 | Toss Away / Green / I | 6 | 4 | 2 | Skill - Ranged / R 3 | - |
| wuk14__7721596 | Mighty Throw / Green / II | 5 | 4 | 2 | Skill - Ranged / R 4 | Attack |
| wuk15__7721590 | Into the Canopy / Green / II | 5 | 4 | 2 | Skill / r 2 | Defense |
| wuk16__7721597 | Monstrous Throw / Green / III | 5 | 5 | 2 | Skill - Ranged / R 4 | Attack |
| wuk17__7721595 | Treetop Ride / Green / III | 5 | 5 | 2 | Skill / r 2 | Defense |
| wuk18__7721593 | March of Nature / Purple / IV | - | - | - | Ultimate / r 3 | - |
| wuk19__7721583 | Makeshift Weapon / Gold / Handicap | 10 | 2 | 1 | Basic Attack - Ranged / A 3, R 2 | - |

Items: Attack x3, Defense x3, Initiative x3, Movement x1, Range x1, Radius x1. Trample and Angry Stampede print a Defense icon and no Movement icon on the ribbon; their Movement is the primary action. Upgrade lines: Red Nature's Protector -> Nature's Guardian or Trample -> Nature's Champion or Angry Stampede; Blue Gifts of Nature -> Tree of Plenty or Claim Dominance -> Abundance or Assert Dominance; Green Toss Away -> Mighty Throw or Into the Canopy -> Monstrous Throw or Treetop Ride. p.15's level-up list is validated as a whole at dealing (a Tier II names both Tier III faces of its colour).

### Printed clauses

Quoted from the fronts; each implementation test uses the front, not this routing copy.

- **Tree Slam:** "Choose one - Target a minion adjacent to you. / Remove a Tree token adjacent to you. Target a unit in range."
- **Mystic Saplings:** "Place up to 3 Tree tokens in radius; Tree tokens are not removed at the end of round."
- **Nature's Protector / Nature's Guardian:** "Choose one - Target a hero adjacent to you. / Target a unit in range adjacent to a Tree token."
- **Nature's Champion:** "Choose one, or both, on different targets -" the same two bullets.
- **Trample:** "If you move in a straight line: You may ignore obstacles; each enemy hero you moved through discards a card, or is defeated; defeat up to one minion you moved through." **Angry Stampede:** the same with "defeat up to two minions".
- **Gifts of Nature:** "Remove a Tree token in radius. You may retrieve a discarded card."
- **Tree of Plenty:** "Remove a Tree token in radius. Choose one - You may retrieve a discarded card. / A friendly hero in radius may retrieve a discarded card." **Abundance:** the same with "Choose one or both".
- **Claim Dominance:** "This round: Up to 1 enemy minion adjacent to you does not count towards the minion total during minion battle, regardless of immunity." **Assert Dominance:** "Up to 2 enemy minions ... do not count".
- **Toss Away / Mighty Throw:** "Place a token, or an enemy unit, adjacent to you into a space in range." **Monstrous Throw** adds "May repeat once."
- **Into the Canopy:** "Choose one - Swap with a Tree token in radius. / Swap a friendly unit in radius with a Tree token in radius." **Treetop Ride:** "Choose up to two times -" the same bullets.
- **March of Nature:** "Each time after you resolve a card, you may place a Tree token in radius."
- **Makeshift Weapon (boxed Handicap):** "Choose one - Target a hero adjacent to you. / Remove a Tree token adjacent to you. Target a hero in range."

## Readings the rulebook settles

- **Tree tokens** are tokens: obstacles (p.19), never immune, taken from a finite supply with p.18's reclaim-from-board rule when short. Mystic Saplings' second sentence replaces p.8's end-of-round removal for every Tree token (WU-Q2).
- **"Remove a Tree token"** is a mandatory step (p.10). With no Tree in reach it cannot be performed, so the action stops: the Tree bullet of Tree Slam, Makeshift Weapon and the three Blue retrieval faces does nothing and is not offered (the 8.35z15 offer rule). The Attack's Range still measures from Wuk.
- **"Choose one, or both, on different targets"** (Nature's Champion) is Dodger's D-06 and Bain's composition: two complete p.14 sequences, the second on a different unit. **"Choose one or both"** (Abundance) is the retrieval pair taken in either or both forms; "you" and "a friendly hero" are different heroes by construction.
- **"Choose up to two times"** (Treetop Ride) is Rowenna's Grand Melee and Brynn's BR-Q4 reading: up to two performances, each choosing either bullet, the same bullet allowed twice. Each swap re-reads the board.
- **Trample and Angry Stampede** are Movement primaries whose text modifies the movement (p.11). Moving in a straight line (p.19) unlocks ignoring obstacles, so the path may pass through units and tokens but never ends on one (p.18). A hero Wuk moved through is affected by his action and so is targeted (p.19): an immune enemy hero is not asked to discard. "Discards a card, or is defeated" is the defender's choice with the defeat branch when no card is held (Seismic, Grid). A curved move performs none of the clause. Built at 8.114b1 under WU-Q5 (A): the walk may be committed in legs; while it is one straight line from where it started, a leg may continue along its file through obstacles, and once it has crossed one only that continuation is offered. The trampled units are read from the persisted walk after it ends. p.13 pays coins for enemy minions only, so defeating one of his own minions pays nothing.
- **Claim and Assert Dominance** create a This round active effect (p.17) on Wuk; the minion total is p.8's count at the end of the round. "Regardless of immunity" states that the chosen minions' immunity does not shield them from this rule.
- **The throws** are p.18's Place: the chosen token or enemy unit is taken from its space adjacent to Wuk and put into an empty space in Range of Wuk. An enemy unit is targeted (immunity applies); a token of any owner may be chosen (tokens are never immune). It is not movement. A minion placed outside its Battle Zone is returned by p.19's rule. Monstrous Throw's "May repeat once" has no "different target" wording, so p.10's repeat allows the same subject.
- **The swaps** are p.19's Swap: neither movement nor placement. "A friendly unit" is read as the build's other friendly-unit selections read it.
- **March of Nature** fires after each card Wuk resolves (any colour, Gold and Silver included), optionally placing one Tree in Radius 3, with the supply's reclaim. It is a Place, so a Tree placed after a card's resolution is not part of that card.

## Capability mapping

Existing contracts to reuse (each block re-probes before claiming reuse): token definitions with supply, reclaim (`synthetic-reclaim-token@1`, Mrak's and Ignatia's short-supply steps) and `TokenRemovalCondition.RetainedAtEndOfRound` (Widget); up-to-N placement in radius (Ignatia's magma, Min's Smoke bomb); `synthetic-remove@2`; the 8.88 composable selection's adjacency-to-token-kind arm; `retrieve-card@1` with Silverarrow's Blessing shape for "a friendly hero ... may retrieve"; Dodger/Bain's one-or-both Attack modal and Brynn/Rowenna's up-to-two-times modal; `synthetic-swap@2` / `choose-swap-subject@1`; `synthetic-place-in-range@1` and Mrak's Boulder "a token, or an enemy unit" kind union; p.10 repeats; discard-or-defeat; `MoveThroughTrapQuery.SpacesMovedThrough` (Min's Mines) and the obstacle-ignoring straight-line move (Mrak's Rolling Stone); the Minion-Battle rule catalog (`MinionBattleRuleDeclaration`, Garrus's One Man Army handler) and `PendingMinionBattleChoice`; `UltimateTriggerCondition.AfterControllerResolvesBasicCard` and Mrak's after-tokens-placed interrupt as the trigger precedents.

New contracts, each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| Tree token (3, obstacle, retained at round end, reclaim) and Mystic Saplings | Silver, every Tree face | 8.114a |
| Remove an adjacent Tree, then target in Range, as one modal bullet | Tree Slam, Makeshift Weapon | 8.114a |
| Straight-line-conditional obstacle ignoring and moved-through consequences on a Movement primary | Trample, Angry Stampede | 8.114b1 |
| Remove a Tree in radius, then retrieve for self and/or a friendly hero | three Blue retrieval faces | 8.114c |
| Enemy minions adjacent at minion battle excluded from the total, chosen then (WU-Q3) | Claim, Assert Dominance | 8.114b1 |
| Place a token or an enemy unit adjacent into a space in Range | three throws | 8.114d |
| After-you-resolve-a-card trigger for any card | Ultimate | 8.114e |

## Open questions

Each blocks only the block named. WU-Q1 to WU-Q6 are settled by the user.

- **WU-Q1 (user, settled 2026-09-28):** Wuk's Tree supply is **three**, with p.18's short-supply step taking standing Trees back when a placement needs more.
- **WU-Q2 (user, settled 2026-09-28):** Mystic Saplings' "Tree tokens are not removed at the end of round" is a standing property of **every** Tree token, whichever card placed it (the Ultimate's included) - the reading Snorri's identical rune-marker sentence has.
- **WU-Q3 (user, settled 2026-09-28):** Claim and Assert Dominance choose nothing on resolution. At the round's minion battle, Wuk chooses up to one (two) enemy minions **adjacent to him at that moment**; those do not count. A minion that is no longer adjacent counts.
- **WU-Q4 (user, settled 2026-09-28):** Trample's and Angry Stampede's "defeat up to one (two) minion(s) you moved through" reads as printed: **any** minion, friendly included; the "up to" makes it Wuk's choice. **Superseded 2026-09-28 by W4** ([PRINTED_WORDING](PRINTED_WORDING.md)): the rulebook forbids defeating a friendly unit with no exceptions, so only enemy minions he moved through are offered; the "up to" remains his choice. Applied at milestone 9.2.
- **WU-Q5 (user, settled 2026-09-28, "I think its A"): reading (A).** Raised the same day: how Trample and Angry Stampede are built. The engine probe found that a Movement walk's route is persisted only while the walk is under way and is discarded when it completes, and that nothing persisted between the walk's end and its "after" consequences can hold which spaces were crossed. Two readings are buildable. **(A) The Movement itself tramples:** a new straight-line obstacle permission on the Movement walk, with the crossed units' consequences run at the walk's completion; faithful to "if you move in a straight line", Movement items and every Movement-action rule apply unchanged, but the completed walk must be persisted, a snapshot schema move (v46 to v47). **(B) The text performs the trample before the Movement:** "move in a straight line, ignoring obstacles" is an optional card-text move up to the Movement value, with the consequences in the same frame, after which the Movement action has nothing left to walk; no schema move, but the trample is a card-text move rather than the Movement action - the reading the user's 2026-09-06 rider gave Whisper's forced moves - so effects that watch Movement actions do not see it. Recommended: (A).
- **WU-Q6 (user, settled 2026-09-28, "Yes"): yes, the excluded minion may be removed.** Raised the same day: Claim and Assert Dominance say the chosen minions "do not count towards the minion total". When the enemy team then loses the battle, may it pick an excluded minion as one of the minions it removes? The literal reading is yes - the sentence changes the count, not which minions stand in the Battle Zone - and it is the recommended one. The other reading takes the excluded minions out of the battle entirely.
