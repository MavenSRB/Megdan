# Guards Digital - Mortimer Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [The twenty-seventh-hero sequence](../Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md) owns acceptance and status.

## Source and rule authority

Mortimer the Awakener is the selected twenty-seventh hero. The curated Tabletopia source `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Mortimer/` holds nineteen byte-distinct fronts and nineteen backs in one byte variant (MD5 `7f9f5b61ed7806f5428e34da27aeb6bf`, atlas 13). All nineteen fronts, the selection profile and the dashboard were read at full resolution on 2026-09-29. The profile (the "Mortimer" tile of `hero-selection-contact-sheet.jpg`, row 2, column 7) shows **three stars** and **Melee, Tokens, Farming, Durable, Pusher**. The dashboard is `cards/hero-dashboards/dashboards-tt-resized-033__7732135__front.png`, matched to the contact sheet's "Mortimer the Awakener" tile by image difference. It gives that title (the manifest's group name is "Mortimer the Undying"; the printed dashboard is displayed) and four ordinary turn slots. These are source references, not distribution permission.

MO-01, stat bars: reading the dashboard's bottom bars pip by pip on an enlargement gives Attack **8**, Defense **7**, Initiative **2** and Movement **4**, each of eight. No Movement pip is brighter than the others, unlike Wuk's (WU-01).

MO-02, manifest (`extracted/text-assets/manifest-bundled__42.json`):
- Standee: element **8596249**, named **`Morty`** (publicElementId 5917493, VerticalTokens, 302x302).
- Zombie token: one hexagonal element, **`Zombieeee`** 7694669 (publicElementId 5551748, preview 427x369). It has two 427x369 face sprites in `Image_3_Atlas` at y 1043, x 441 and x 870, plus a 10-pixel edge sprite at x 1299.
- The manifest states no token count, and element names set neither gameplay kind nor supply (MO-Q1 sets the supply).

MO-03, rulebook: the local second-printing text names no Mortimer, Awakener or Zombie. General authority:
- p.9-10: Range and Radius icons, modal choices, repeat, mandatory and optional steps.
- p.8: minion battle.
- p.11: movement in card text.
- p.12-14: Attack actions, "Before the attack" and "After the attack", Defense.
- p.15: level-up.
- p.16: Ultimate.
- p.18-19: Obstacle, Place and short supply, Push, Remove, Replace, Swap, Target, Tokens, Spawn points.
- p.20: Handicap.

Printed fronts control each clause under [the authority hierarchy](../RULES_LEDGER.md#authority-hierarchy).

## Printed inventory

File stems below are under the Mortimer source directory and end in `__front.png`. D/M are the ribbon's Defense and Movement icons; A/R/r are the primary action's Attack, Range and Radius icons. A missing icon is absent, never zero. Ribbon glyphs, Range/Radius icons and items were verified by shape in montages across all nineteen fronts: the Gold, Silver and Red faces print the ring-with-two-arrows Radius icon and read "in radius"; the Blue and Green faces print the crosshair-and-arrow Range icon and read "in range".

| Stem | Face / colour / tier | Init | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| mortimer__7695015 | Knife of the Living Dead / Gold / - | 11 | 3 | 1 | Basic Attack / A 4+, r 4 | - |
| mortimer2__7695011 | Awaken! / Silver / - | 6 | 3 | - | Basic Defense / Skill / r 4 | - |
| mortimer3__7703472 | Crawling Dead / Red / I | 7 | 7 | 3 | Attack / A 6, r 4 | - |
| mortimer4__7703471 | Walking Dead / Red / II | 7 | 7 | 3 | Attack / A 7, r 4 | Defense |
| mortimer5__7703470 | Braains...! / Red / II | 7 | 7 | 3 | Attack / A 7, r 4 | Initiative |
| mortimer6__7703469 | Racing Dead / Red / III | 8 | 8 | 3 | Attack / A 7, r 4 | Range |
| mortimer7__7703468 | Braaaaaaains...! / Red / III | 8 | 8 | 3 | Attack / A 7, r 4 | Movement |
| mortimer8__7695016 | Corpse Slam / Blue / I | 9 | 6 | 3 | Skill - Ranged / R 4 | - |
| mortimer9__7695014 | Morbid Mosh / Blue / II | 9 | 6 | 3 | Skill - Ranged / R 4 | Attack |
| mortimer10__7695010 | Robbing Zombies / Blue / II | 9 | 6 | 3 | Skill - Ranged / R 4 | Defense |
| mortimer11__7695019 | Macabre Mayhem / Blue / III | 10 | 7 | 3 | Skill - Ranged / R 4 | Radius |
| mortimer12__7695021 | Stalking Scalpers / Blue / III | 10 | 7 | 3 | Skill - Ranged / R 4 | Defense |
| mortimer13__7695012 | Stage Dive / Green / I | 5 | 3 | 2 | Skill - Ranged / R 4 | - |
| mortimer14__7695017 | Crowd Drift / Green / II | 5 | 4 | 2 | Skill - Ranged / R 4 | Attack |
| mortimer15__7695028 | Gathering Horde / Green / II | 5 | 4 | 2 | Skill - Ranged / R 4 | Initiative |
| mortimer16__7695025 | Crowd Surf / Green / III | 4 | 4 | 2 | Skill - Ranged / R 4 | Attack |
| mortimer17__7695024 | Army of Darkness / Green / III | 4 | 4 | 2 | Skill - Ranged / R 4 | Initiative |
| mortimer18__7695027 | Master of Puppets / Purple / IV | - | - | - | Ultimate | - |
| mortimer19__7695026 | Shallow Graves / Gold / Handicap | 10 | 1 | 1 | Basic Attack / A 3+, r 4 | - |

Items: Attack x3, Defense x3, Initiative x3, Movement x1, Range x1, Radius x1. Awaken!'s 3 is its Basic Defense value; it prints no Movement icon. Upgrade lines (by name family, validated as a whole against p.15 at dealing):
- Red: Crawling Dead -> Walking Dead or Braains...! -> Racing Dead or Braaaaaaains...!
- Blue: Corpse Slam -> Morbid Mosh or Robbing Zombies -> Macabre Mayhem or Stalking Scalpers.
- Green: Stage Dive -> Crowd Drift or Gathering Horde -> Crowd Surf or Army of Darkness.

### Printed clauses

Quoted from the fronts; each implementation test uses the front, not this routing copy.

- **Knife of the Living Dead:** "Target a unit adjacent to you. Before the attack, Choose up to three times - Move a Zombie token in radius 1 space. / Remove a Zombie token adjacent to the target; if you do, gain +1 Attack."
- **Awaken!:** "Place up to 4 Zombie tokens into spawn points in radius, counting spaces adjacent to you as spaces with spawn points; Zombie tokens are not removed at the end of round."
- **Crawling Dead:** "Target a unit adjacent to you. After the attack, choose one - Move a Zombie token in radius 1 space. / Another enemy hero in radius, adjacent to a Zombie token, discards a card, if able."
- **Walking Dead / Racing Dead:** the same with "choose up to two / three times", and the second bullet ending "; each enemy hero can only be targeted once."
- **Braains...! / Braaaaaaains...!:** "Target a unit adjacent to you. Before the attack, choose up to two / three times - Move a Zombie token in radius 1 space. / If an enemy hero in radius is adjacent to a Zombie token, retrieve a discarded card."
- **Corpse Slam:** "Choose one - Move a Zombie token in range up to 1 space; that Zombie token may push a unit or a token adjacent to it 1 space. / Move 1 space." **Morbid Mosh / Macabre Mayhem:** "Choose up to two / three times -" the same bullets.
- **Robbing Zombies / Stalking Scalpers:** "Choose up to two / three times - Move a Zombie token in range up to 1 space. Gain 1 coin. / Move 1 space."
- **Stage Dive:** "Choose one - Move a Zombie token in range 1 space. / Swap with a Zombie token in range." **Crowd Drift / Crowd Surf:** "Choose up to two / three times -" the same bullets.
- **Gathering Horde / Army of Darkness:** "Choose up to two / three times - Move a Zombie token in range 1 space. / Once per turn: Replace an enemy minion in range adjacent to two or more Zombie tokens with a Zombie token."
- **Master of Puppets:** "Whenever you would choose up to three times, you may choose up to five times instead."
- **Shallow Graves (boxed Handicap):** "Target a hero adjacent to you. Before the attack, Choose up to two times - Move a Zombie token in radius 1 space. / Remove a Zombie token adjacent to the target; if you do, gain +1 Attack."

## Readings the rulebook settles

- **Zombie tokens are tokens.** They are obstacles (p.19), never immune, and taken from a finite supply with p.18's reclaim-from-board rule when short. Awaken!'s second sentence replaces p.8's end-of-round removal for every Zombie token (MO-Q4).
- **"Move a Zombie token in radius (range) 1 space"** moves a token standing within the printed reach of Mortimer to an adjacent empty space. The reach bounds which token, not where it goes. "Up to 1 space" admits 0 under W5 ([PRINTED_WORDING](PRINTED_WORDING.md)); "1 space" is exact (p.11). With no Zombie in reach the step cannot be performed, and p.10 stops that bullet (MO-Q3).
- **"That Zombie token may push a unit or a token adjacent to it 1 space"** is p.18's Push with the Zombie as the pusher: straight away from the token's space. "A unit" names any unit, friendly included, and Mortimer himself when adjacent. An immune unit is not pushed (p.19).
- **"Choose up to two (three) times"** is Wuk's Treetop Ride, Rowenna's Grand Melee and Brynn's BR-Q4 reading: up to that many performances, each choosing either bullet, the same bullet allowed again, and each performance reading the board the last one left. Stopping early is the player's choice. "Choose one" (Tier I) is one performance.
- **Master of Puppets** changes only faces that print "up to three times": Knife of the Living Dead, Racing Dead, Braaaaaaains...!, Macabre Mayhem, Stalking Scalpers, Crowd Surf and Army of Darkness. "Choose one" and "up to two times" are unchanged. It is a p.16 permanent Ultimate ability, and "may" lets him keep three.
- **"Remove a Zombie token adjacent to the target; if you do, gain +1 Attack"** is a Remove (p.19; the token returns to the supply) whose success gives +1 Attack to this Attack. Chosen twice or three times, each removal adds one.
- **"Another enemy hero in radius, adjacent to a Zombie token, discards a card, if able"** names an enemy hero other than this Attack's target who stands in radius of Mortimer and adjacent to a Zombie. "Each enemy hero can only be targeted once" makes the repeated bullet name a different hero each time. With nobody qualifying, the bullet cannot be performed.
- **"If an enemy hero in radius is adjacent to a Zombie token, retrieve a discarded card"** is a printed "If" gate (RULING 53: a false gate is a legal do-nothing choice); when it holds, Mortimer retrieves a card of his own.
- **"Swap with a Zombie token in range"** is p.19's Swap between Mortimer and the token: neither movement nor placement.
- **"Replace an enemy minion ... with a Zombie token"** is p.19's Replace. The minion is removed, not defeated, so no coins are paid (p.13 pays for Defeats), and a Zombie from the supply takes its space. It qualifies as a removal for p.8's Lane Push. "Once per turn" limits that bullet to one performance in Mortimer's turn, across the card's repeated choices.
- **Awaken!** places up to four Zombies. Each goes into an empty spawn point (minion or hero) within Radius 4, or an empty space adjacent to Mortimer (MO-Q2), with the supply's reclaim. It is a Place, not movement.

## Capability mapping

Probed 2026-09-29 against the build at `milestone/9.7`; each block re-probes before claiming reuse.

Existing contracts to reuse:
- **"Choose up to N times"** is Wuk's Treetop Ride, Rowenna's Grand Melee and Brynn's Decoy composition. It is N sibling `Optional` (`UpTo`, chooser Actor) wrappers, each over a one-of `Choose`, so each pass reads the board the last one left. The builders already take the pass count.
- **Moving a Zombie** is `synthetic-direct-move@3`, whose mover is whatever stands on the named space; tokens are always admitted (Wasp's Lift Up). The selection is `synthetic-radius-target@1` or `synthetic-range-target@1` with `admittedObjectKind = TokensOnly` and the Zombie `tokenKind`, as Wuk's Tree selections do. "1 space" is Mandatory and "up to 1 space" is `UpTo`.
- **Swap with a Zombie** is `synthetic-swap-actor@1` (Into the Canopy). The `ownTokensOnly` clause's own comment names this sentence.
- **The Zombie token** reuses the `TokenDefinition` supply (four), `TokenRemovalCondition.RetainedAtEndOfRound` (Wuk's Tree, Widget's Pyro), and a per-hero `ReclaimHandler` as a `May` node before each placement. The synthetic hazard's `synthetic-reclaim-token@1` / `synthetic-place-token@1` are not for this.
- **"An enemy minion in range adjacent to two or more Zombie tokens"** is `synthetic-range-minion-target@1` with `adjacentToTokenKind` and `adjacentAtLeast = 2` (8.88). Mortimer's clause was the survey case for it.
- **After-the-attack discard by another hero** follows Tali's Ice Blast rider (`choose-target@2` with `excludeTarget`, then `synthetic-discard-chosen-card@1`, `IfAble`).
- **Remove, gain resource and retrieve** are existing verbs. *Corrected at 9.8c:* the Attack-value "+1 if you do" was NOT one. Every Attack bonus the engine had was a value contribution read from the board, and nothing counted what the Attack's own text did. 9.8c builds it as one "+1 Attack" Active effect per pass that removed a Zombie.

New contracts, each owned by one block:

| Need | Faces | Block |
| --- | --- | --- |
| Zombie token definition; "onto a spawn point in radius or a space adjacent to you" placement clause, placed four times with reclaim. **Built at 9.8a:** `MortimerComponentContent.Zombie` (supply 4, `RetainedAtEndOfRound`), the family place/reclaim handlers bound to it, and the `spawnPointOrAdjacent` placement clause on the printed-icon space family (every p.19 spawn point in the map definition, minion or hero, any zone, or distance 1 from where the actor stands; the printed Radius still bounds both) | Awaken! | 9.8a |
| Five passes when Master of Puppets is unlocked: passes 4-5 behind a `Conditional` over `ultimate-unlocked@1` on the seven "up to three times" faces (`ChoiceCardinality` raises one modal's distinct-option count and is not this). **Built at 9.8b:** `MortimerRepeatedChoice.Compose`, whose gate carries an explicit read-only else-child so the Ultimate's unprinted condition never triggers RULING 53's single-child exemption, and Master of Puppets as a p.16 permanent declaration | Ultimate and seven faces | 9.8b |
| One selection clause that both excludes the Attack's target and reads adjacency to a token kind (today `choose-target@2` has the first and the printed-icon hero arm the second). **Built at 9.8c:** `excludeTheAttackTarget` on the printed-icon family beside its `adjacentToTokenKind` | Crawling, Walking, Racing Dead | 9.8c |
| "Each enemy hero can only be targeted once" across up to five passes: an exclusion set accumulated over the card's passes (the single-value `excludeHero` and one-target repeat bindings cannot express it). **Built at 9.8c:** the engine-known `distinctAcross` selection group; the resolver hands each member the answers its group has been given, read off the frame's accepted choices | Walking, Racing Dead | 9.8c |
| A push whose pusher is a named space (an optional pusher-space parameter on `synthetic-push-target@3`; `TryFindPusher` hard-codes the acting hero today). **Built at 9.8d:** `pusherSpace` on both push declarations; the Blues use `synthetic-object-push@3` with one target, bound to the moved Zombie's space | Corpse Slam, Morbid Mosh, Macabre Mayhem | 9.8d |
| Replace an enemy minion with a token: a minion-to-token arm on `ReplaceOperation` (only token-for-token exists) and its capability; not Remove-then-Place. **Built at 9.8e:** `ReplaceOperation.ReplaceMinion`, `CommitValidatedMinionReplacement`, `mortimer-replace-minion-with-zombie@1` | Gathering Horde, Army of Darkness | 9.8e |
| "Once per turn" on one bullet: a predicate over the turn-scoped keys the Ultimate triggers already spend, as that bullet's first step. **Built at 9.8e instead as** the `onceAcrossTheGroup` selection clause over 9.8c's `distinctAcross` group: the card's one resolution is the whole turn that can reach the bullet | Gathering Horde, Army of Darkness | 9.8e |

## Open questions

Each blocks only the block named. MO-Q1 to MO-Q4 are settled by the user.

- **MO-Q1 (user, settled 2026-09-29):** the Zombie supply is **four**, with p.18's short-supply step taking standing Zombies back when a placement needs more.
- **MO-Q2 (user, settled 2026-09-29):** Awaken!'s "spawn points in radius" are **any empty spawn point, minion or hero, anywhere in Radius 4** (not limited to the battle zone), plus empty spaces adjacent to Mortimer.
- **MO-Q3 (user, settled 2026-09-29):** Robbing Zombies' and Stalking Scalpers' "Gain 1 coin" needs a Zombie in range. The user pointed to p.10: the move is a mandatory step, and a step that cannot happen stops the bullet before the coin. A present Zombie may still move 0 (W5).
- **MO-Q4 (user, settled 2026-09-29):** Awaken!'s "Zombie tokens are not removed at the end of round" is a standing property of **every** Zombie token, including one placed by Gathering Horde's or Army of Darkness's Replace.
