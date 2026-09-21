# Guards Digital - Brynn Rules Shard

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md).

## Brynn content ledger

Brynn is the twentieth playable hero. [The implementation series](../Roadmap/PHASE_8_TWENTIETH_HERO.md) owns acceptance. This shard records his nineteen printed definitions, the general rules they lean on, the one printed predicate every non-basic face reads, the capability map and the open readings. Nothing of his is composed, registered or artworked yet; each later block updates the relevant row here.

## Source authority

The curated `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Brynn/` contains nineteen fronts and nineteen backs, stored internally under the Tabletopia name `brynn` (`brynn`, `brynn2` .. `brynn19`). Every front, the selection card and the dashboard were visually inspected on 2026-09-21; the twelve item icons were montaged beside Rowenna's six verified icons, the nineteen ribbons and corners were montaged, and the profile card's stat bars were read by luminance scanline. The dashboard names **Brynn the Seeker**; the selection card names **Brynn**, shows **three stars** (p.21 "Very Complex"), and lists **Tactician, Damager, Durable**. No token and no marker is named anywhere in his set. The one phrase his set is built around is printed on fifteen faces: **"adjacent to 3 or more obstacles"**, with the Gold's reminder "( You, other heroes, minions, tokens and terrain are obstacles )".

BR-01: text extraction of all twenty-four pages of the local second-printing rulebook finds no Brynn, Seeker or card-name mention. The extracted cards (verified on 2026-09-18 as the printed second edition wherever physical cards could be checked - see the parent ledger's "Edition of the extraction") are the local authority for individual text. General p.9 (card anatomy; "a card with an Attack primary action is referred to as an Attack card"), p.10 (mandatory and optional steps, picking targets, modal actions and "Choose ... up to", repeat: "with the same range, restrictions etc.", "You cannot repeat a repeated action", retrieve), p.11 (movement in the card text: the same rules as a Movement action, "up to" makes the distance optional), p.13 (after-the-attack timing), p.14 (the nine-step order), p.17 (Adjacent; Counts as: "all rules and qualities of that object are replaced"; Friendly never includes you), p.18 (Obstacle: "Units, terrain (including printed on the board), tokens, as well as any objects represented by miniatures, are obstacles"; Immune: presence checks count immune units) and p.19 (Swap "does not count as movement or placement"; Target: "Whenever your action has any effect on another unit in any way, you target that unit. Effects that check for unit presence ... do not target"; Terrain: "Any board space without a full grid outline counts as terrain and is an obstacle, including the water spaces surrounding the board") govern.

BR-02: all nineteen backs share one MD5 (`321a6a44ed866bc070d9e0876c9b217e`); this set has ONE deck back. The 2nd-printing atlas index is `cards.csv` atlas 8. The selection front is byte-identical to `cards/hero-selection/randomizers-tt23__7723707__front.png` (MD5 `e6176809b552fbcec7753686b971af08`, SHA-256 `1e9bfcb798492dd134a810c88213119c8dba6c03171c14c6bbfd05f6f53abad3`).

BR-03: every corner letter matches its ribbon (D, S, R x5, B x5, G x5, P, D-H); there is no printed colour conflict in this set.

BR-04: stat bars read Attack **4** plus **4** brighter conditional pips, Initiative **7**, Defense **4**, Movement **5** plus **1** brighter pip (p.21: brighter sections depend on level-up choices and card text). The dashboard's bottom bars agree.

BR-05: Home Turf, the Handicap, prints the band **"Basic Skill - Ranged"** over an Attack value of 2, a Range icon of 2 and the text "Target a hero ..." - the mirror of Familiar Ground's "Basic Attack - Ranged". It is read as a printed Attack under a mislabelled band; it is the boxed p.20 Handicap and is never dealt, so nothing runs on it.

BR-06: absent icons. Decoy prints only Defense 2 (no Movement). High Ground, Elevated Ambush and Peak Precision print no Range or Radius (adjacent targets only); Over the Top prints no values at all. Familiar Ground prints Range 3 with the Ranged band; Split Attack and Split Throw print Range 2 with the Ranged band. A missing icon is absent, never zero.

BR-07: Tabletopia manifest elements (`extracted/text-assets/manifest-bundled__42.json`): standee **`Brynn`** element 8629140 (publicElementId 5917484, VerticalTokens/Standee, `Image_2_Atlas` sprites (3588,1443) and (3284,1443), 302x302 - verified by crop as the orange-ringed and blue-ringed portraits of the lynx-eared hero). His card group is named **"Brynn the Tracker"** (group 8878585) where the dashboard prints **the Seeker**; the dashboard is the printed authority and the display name is "Brynn the Seeker". No token element is his.

## Printed inventory

All file stems below are under the Brynn source directory and end in `__front.png`. D/M are Defense/Movement ribbon icons; A is Attack, R Range, r Radius. A missing icon is absent, never zero. Item icons are independent of card values. `A 4+` is the printed plus beside a conditional bonus (Tigerclaw's Backstab shape).

| File stem | Face / color / tier | Initiative | D | M | Primary / icon | Item |
| --- | --- | --- | --- | --- | --- | --- |
| brynn__7690719 | Familiar Ground / Gold / Basic | 12 | 2 | 1 | Basic Attack - Ranged / A 2, R 3 | - |
| brynn2__7711616 | Decoy / Silver / Basic | 1 | 2 | - | Basic Skill / r 4 | - |
| brynn3__7696235 | High Ground / Red / I | 9 | 6 | 4 | Attack / A 4+ | - |
| brynn4__7696236 | Elevated Ambush / Red / II | 9 | 6 | 4 | Attack / A 5+ | Initiative |
| brynn5__7690722 | Split Attack / Red / II | 9 | 3 | 4 | Attack - Ranged / A 3, R 2 | Defense |
| brynn6__7696234 | Peak Precision / Red / III | 10 | 7 | 4 | Attack / A 5+ | Radius |
| brynn7__7690723 | Split Throw / Red / III | 10 | 4 | 4 | Attack - Ranged / A 3, R 2 | Defense |
| brynn8__7690726 | Tread Lightly / Blue / I | 10 | 5 | 3 | Skill / r 2 | - |
| brynn9__7690727 | Cover Tracks / Blue / II | 10 | 5 | 3 | Skill / r 3 | Defense |
| brynn10__7724914 | Mountain Guide / Blue / II | 10 | 5 | 3 | Skill / r 4 | Attack |
| brynn11__7690728 | Hide Traces / Blue / III | 11 | 6 | 3 | Skill / r 3 | Range |
| brynn12__7724915 | Expedition Leader / Blue / III | 11 | 6 | 3 | Skill / r 4 | Attack |
| brynn13__7715988 | Bear Trap / Green / I | 3 | 3 | 2 | Skill / r 3 | - |
| brynn14__7715987 | Log Trap / Green / II | 2 | 3 | 2 | Skill / r 4 | Initiative |
| brynn15__7418476 | True Grit / Green / II | 2 | 3 | 2 | Skill / r 4 | Attack |
| brynn16__7715986 | Deadfall Trap / Green / III | 2 | 4 | 2 | Skill / r 4 | Initiative |
| brynn17__7418486 | Die Hard / Green / III | 2 | 4 | 2 | Skill / r 4 | Movement |
| brynn18__7550498 | Over the Top / Purple / IV | - | - | - | Ultimate | - |
| brynn19__7418492 | Home Turf / Gold / Handicap | 11 | 1 | 1 | "Basic Skill - Ranged" (BR-05) / A 2, R 2 | - |

### Exact printed rules

- **Familiar Ground:** Choose one - (1) Target a unit adjacent to you. (2) Target a hero in range who is adjacent to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles ).
- **Decoy:** Choose up to two times, on different targets - (1) Move an enemy minion in radius 1 space. (2) Move an enemy hero in radius who is adjacent to 3 or more obstacles 1 space.
- **High Ground / Elevated Ambush:** Target a unit adjacent to you. If you target a hero adjacent to 3 or more obstacles, +2 [Attack] attack.
- **Split Attack:** Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, may repeat once on a different unit adjacent to you.
- **Peak Precision:** Target a unit adjacent to you. If you target a hero adjacent to 3 or more obstacles obstacles, +2 [Attack] attack and After the attack: You may retrieve this card.
- **Split Throw:** Target a unit in range. If you target a hero who is adjacent to 3 or more obstacles, repeat once on a different target unit.
- **Tread Lightly:** Swap either with a unit adjacent to you, or with an enemy hero in radius who is adjacent to 3 or more obstacles.
- **Cover Tracks:** Swap either with a unit adjacent to you, or with an enemy hero in radius who is adjacent to 3 or more obstacles. You may move 1 space.
- **Mountain Guide:** You may move a friendly unit adjacent to you up to 2 spaces. If an enemy hero in radius is adjacent to 3 or more obstacles, move a different friendly unit in radius up to 2 spaces.
- **Hide Traces:** Swap either with a unit adjacent to you, or with an enemy hero in radius who is adjacent to 3 or more obstacles. Move up to 2 spaces.
- **Expedition Leader:** You may move a friendly unit adjacent to you up to 3 spaces. If an enemy hero in radius is adjacent to 3 or more obstacles, move a different friendly unit in radius up to 3 spaces.
- **Bear Trap / Log Trap:** Choose one - (1) An enemy hero adjacent to you discards a card, if able. (2) An enemy hero in radius who is adjacent to 3 or more obstacles discards a card, if able.
- **True Grit:** You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to 3 or more obstacles, move up to 3 spaces.
- **Deadfall Trap:** Choose one - (1) An enemy hero adjacent to you discards a card, or is defeated. (2) An enemy hero in radius who is adjacent to 3 or more obstacles discards a card, or is defeated.
- **Die Hard:** You may retrieve a discarded attack card. If an enemy hero in radius is adjacent to 3 or more obstacles, move up to 4 spaces.
- **Over the Top (Ultimate):** While you are performing actions all enemy heroes in play count as adjacent to 3 or more obstacles.
- **Home Turf (Handicap):** Choose one - (1) Target a hero adjacent to you. (2) Target a hero in range who is adjacent to 3 or more obstacles. ( You, other heroes, minions, tokens and terrain are obstacles ).

Peak Precision's doubled "obstacles obstacles" is a printing slip transcribed as printed; it carries no rule.

## Setup and progression

Starting hand: Familiar Ground, Decoy, High Ground, Tread Lightly, Bear Trap. Deck: twelve Tier II/III faces plus Over the Top. Each colored Tier I upgrades to either face of its Tier II, then either Tier III (Red to Elevated Ambush or Split Attack then Peak Precision or Split Throw; Blue to Cover Tracks or Mountain Guide then Hide Traces or Expedition Leader; Green to Log Trap or True Grit then Deadfall Trap or Die Hard); the unchosen face becomes its printed item (Initiative x3, Defense x3, Attack x3, Radius, Range, Movement). Home Turf remains the established odd-player Handicap deferral (p.20). He places nothing and gives nothing; his whole set is positional.

## Capability contract map

These routes identify the composed contracts. Registration at the dealing block makes each reachable from dealt content; the gate block separately owns complete configured-player coverage.

| Printed clause | Existing foundation / required delta | Owner |
| --- | --- | --- |
| "adjacent to 3 or more obstacles" (fifteen faces) | NEW one pure query over the six neighbours of a unit's space, each answered by `ObstacleQuery.IsObstacle` (units including Brynn, terrain, the unplayable water ring, tokens, overlays) - a threshold predicate, never a count; consulted by every consumer below so the Ultimate's overlay reaches all of them - BR-Q1, BR-Q13 | 8.69a |
| While you are performing actions all enemy heroes in play count as adjacent to 3 or more obstacles (Over the Top) | `UltimateRuleDefinition.PermanentAbility` (Whisper's Grim Reaper, Xargatha's Metamorphosis) with a NEW `RuleModificationKind` counts-as overlay read inside the one query, scoped to the controller's own actions (NEW `RuleValueApplicability` for any action beside `ControllerBasicAction`) - BR-Q2 | 8.69a |
| Target a hero in range who is adjacent to 3 or more obstacles (Familiar Ground; Home Turf parked) | Bain's Hand Crossbow modal (`Choose` 1..1 over two attack-target bullets) with a NEW `AttackSequenceSubjectNarrowing` member - BR-Q3 | 8.69a |
| If you target a hero adjacent to 3 or more obstacles, +2 attack (High Ground, Elevated Ambush, Peak Precision) | `RosterValueContributionDefinition` on `PrintedValueChannel.Attack` with a NEW `RosterValueQueryKind` member (Tigerclaw's Backstab shape), read when p.14 computes the Attack total - BR-Q6 | 8.69a |
| An enemy hero in radius who is adjacent to 3 or more obstacles (Decoy, the three swaps, the three traps) | `synthetic-radius-hero-target@1` gaining an optional Boolean narrowing (8.57c's `requiresEmptyHand` precedent - a Boolean, never an optional Integer) | 8.69a |
| If an enemy hero in radius is adjacent to 3 or more obstacles (Mountain Guide, Expedition Leader, True Grit, Die Hard) | NEW deferred presence predicate (Boolean output; a presence check, so p.18's immunity does not enter and nothing is targeted) on `subject-adjacent-terrain-presence@1`'s shape; a `Conditional` over it | 8.69a |
| If you target a hero who is adjacent to 3 or more obstacles, may repeat once on a different unit adjacent to you (Split Attack) / repeat once on a different target unit (Split Throw) | `RepeatAction` (`maximumRepeats` 1) gated by a `Conditional` on the first instance's target; NEW `RepeatTargetBinding` member for "a different unit adjacent to you"; `RepeatTargetBinding.DifferentTarget` and `EffectFailurePolicy.Mandatory` for Split Throw - BR-Q5 | 8.69b |
| After the attack: You may retrieve this card (Peak Precision) | `retrieve-card@1` with `restrictToCard` bound to the resolving card, `Optional` + `May`, at `AfterTheAttack` - BR-Q7 | 8.69b |
| Choose up to two times, on different targets - move an enemy minion / a narrowed enemy hero 1 space (Decoy) | Rowenna's Grand Melee shape: two sibling optional modals over `synthetic-radius-minion-target@1` / the narrowed hero target and `synthetic-direct-move@3` (mover = the chosen unit, Brynn choosing, exact distance 1), the second sibling excluding the first's unit - BR-Q4 | 8.69c |
| You may move a friendly unit adjacent to you up to N spaces; ... move a different friendly unit in radius up to N spaces (Mountain Guide, Expedition Leader) | `synthetic-direct-move@3` over a friendly-unit selection (adjacent, then in radius excluding the first), `EffectFailurePolicy.UpTo` for the distance; the second sentence a `Conditional` on the presence predicate - BR-Q9 | 8.69c |
| Swap either with a unit adjacent to you, or with an enemy hero in radius who is adjacent to 3 or more obstacles (Tread Lightly, Cover Tracks, Hide Traces) | `synthetic-swap-actor` (Widget's SwapActor, Rowenna's Stand Guard) over ONE selection joining two reaches - Whisper's `choose-swap-subject@1` shape (two reaches, which is exactly what 8.57b found it to be) or a NEW deferred selection; then `synthetic-direct-move@3` for "You may move 1 space" (Optional) and "Move up to 2 spaces" (UpTo) - BR-Q10 | 8.69d |
| An enemy hero ... discards a card, if able / or is defeated (Bear Trap, Log Trap, Deadfall Trap) | `Choose` 1..1 over two bullets; `synthetic-discard-chosen-card@1` (IfAble, chooser = that hero) and the V2 escalation to `synthetic-defeat-hero@1` (Widget's Breath, Silverarrow) - BR-Q11 | 8.69e |
| You may retrieve a discarded attack card; if ..., move up to N spaces (True Grit, Die Hard) | `retrieve-card@1` gaining an optional attack-card filter (p.9's "Attack card"), `Optional` + `May`; a `Conditional` on the presence predicate over `synthetic-direct-move@3` (UpTo) - BR-Q8 | 8.69e |
| Printed set, upgrades, items, registration and artwork | `BrynnCardContent` composes nineteen definitions, five starting cards, thirteen deck cards and the boxed Handicap; only the Handicap is deferred; the roster row carries THREE stars and Tactician/Damager/Durable for the 8.35z86 picker filters | 8.69f-8.69g; gate 8.69h |

## Integration identity and private artwork

Recorded at 8.69g: schema v43 and wire `/19` unmoved; content revision `brynn-8-69f-rev-1`, identity pin #20 `eafde64d49552397d570d5ee6ec7fd14a915004745f88f6e7d0487116a78601f`; the pick card at BR-02's SHA-256 as `Assets/Resources/Private/HeroSelection/hero-brynn.png`; the standee pair `Heroes/brynn-red.png` / `brynn-blue.png` from element 8629140; eighteen prepared fronts and 67 shine slots under `Cards/brynn/`; no token art. Three registered handler types (`brynn-enemy-hero-in-radius-adjacent-to-obstacles@1`, `brynn-attack-target-was-adjacent-to-obstacles@1`, `brynn-tracking-swap-subject@1`); the roster row carries `VeryComplex` and `Tactician | Damager | Durable`.

## Open verification points

- **BR-Q1:** "adjacent to 3 or more obstacles" is read on the unit's own space: of its six neighbouring spaces, at least three hold an obstacle under p.18 - any unit (Brynn himself included, as the Gold's reminder says), terrain including the unplayable ring around the map (p.19), any token, and any space an overlay has made an obstacle. It is a threshold, not a count, evaluated at the moment the clause is asked (target selection, the Attack total, a presence check); nothing is targeted by asking it.
- **BR-Q2:** Over the Top's "While you are performing actions" is read as: whenever Brynn is the acting hero of an action (primary, secondary or Defense), every enemy hero in play satisfies BR-Q1 for every clause of his - attack targets, the +2, the swaps, Decoy, the guides, the traps and the grit - and never while another hero acts. Under p.17's "Counts as" nothing else about the hero changes, and minions are untouched.
- **BR-Q3:** Familiar Ground is p.10's modal: one bullet is taken and the other ignored; the second bullet's narrowing is a property of the candidate read at target selection.
- **BR-Q4 (user, settled 2026-09-21 "you are right"):** Decoy's "Choose up to two times, on different targets" is read as up to two performances, each choosing either bullet (the same bullet may be taken twice, e.g. two minions), the second never naming the first's unit, zero performances legal. Brynn chooses the unit and its one-space destination (p.10); the move follows p.11, so a unit with no empty neighbour cannot be moved and is not offered.
- **BR-Q5:** the split repeats are p.10 repeats performed after the first instance completes, gated by the FIRST instance's target having satisfied BR-Q1 when it was chosen; the repeated instance takes a different unit - adjacent to Brynn for Split Attack, in range for Split Throw - and never re-triggers; Split Throw's repeat is mandatory and is an impossible step when no different legal unit exists.
- **BR-Q6:** the +2 is a value contribution computed at the Attack total (p.14), the moment Tigerclaw's Backstab reads its own, so a Defense text that moves the defender between steps 4 and 6 can change it.
- **BR-Q7:** Peak Precision's retrieval takes the resolving card from its played state back into his hand at p.14's after-the-attack step; the action still completes; the same +2 condition gates it ("+2 attack and After the attack").
- **BR-Q8:** "a discarded attack card" is a card in Brynn's own discard whose primary action is an Attack (p.9): Familiar Ground and the five Reds. Retrieving is optional; the conditional move is mandatory once the presence holds, with "up to" allowing zero (p.11; Garrus's GA-Q1 precedent).
- **BR-Q9:** in the guides Brynn chooses which friendly unit moves and where (p.10), under p.11's movement rules; "a different friendly unit in radius" excludes the first sentence's unit whether or not that sentence was taken; the enemy hero's presence is checked, not targeted, so immunity is irrelevant and the Ultimate makes it hold for any enemy hero in radius.
- **BR-Q10:** the swap's "either ... or" is one choice over the union of two reaches - any unit adjacent to Brynn (friendly or enemy, hero or minion) and the narrowed enemy heroes in radius; p.19's swap is neither movement nor placement; the trailing move is Brynn's own card-text move.
- **BR-Q11:** the traps are p.10 modals; the chosen hero picks the card they discard (the ledger's reading); "if able" makes an empty hand nothing; "or is defeated" defeats the hero who cannot discard (Widget's Breath).
- **BR-Q12:** Home Turf is parked under p.20 and BR-05; no reading is needed until a Handicap is ever dealt.
- **BR-Q19h (user, settled 2026-09-21 "i think we should gray it out if no legal targets"; built by [8.70](../Roadmap/PHASE_8.md#870-grey-a-modal-bullet-that-has-no-legal-target)):** a p.10 modal bullet whose subject is chosen by `attack-sequence@1` is offered even when its own sentence names nobody, because that handler anticipates from its declaration rather than the board (an 8.35z27 decision). On a board where a unit stands beside Brynn and no hero in range is adjacent to 3 or more obstacles, Familiar Ground's second bullet is therefore a live control that spends the action and applies nothing; both trap bullets, both guide sentences and the swap's two reaches grey correctly. Bain's Bounty Hunter, Dodger's Middlefinger of Death and Whisper's Swift Justice share the contract, so 8.69h pinned the measurement in a journey rather than changing four heroes' shared behaviour inside a gate. The user settled it: an empty arm is greyed at the choice; 8.70 builds it for all four heroes.
- **BR-Q13 (user, settled 2026-09-21):** p.19 makes the water spaces surrounding the board terrain and obstacles, so a hero on the map's edge often satisfies BR-Q1 from the edge alone. The user confirmed the printed reading ("Yeah it counts as obstacle"); the engine already treats an unplayable coordinate as an obstacle (`ObstacleQuery.IsObstacle`) and the series builds to it, with a test stating that the edge alone can qualify a hero.
