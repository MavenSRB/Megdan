# Guards Digital - Rules Ledger

## Purpose

This ledger prevents silent rule drift. It connects each game system to its source, approved deviations, implementation status, and focused tests.

<!-- operative-claim:F-DOC-003 status:CurrentDemonstrated -->

The configured match is rulebook p.5's official six-player Quick Game setup: three distinct Red hero sets and three distinct Blue hero sets, three Waves, five Lives per team, twelve minions, the normal Tie Breaker flip, and 108 playable definitions/instances. All six heroes' verified definitions and current rule systems are implemented where the rule-system and hero shards say implemented; named staged synthetic or one-hero fixtures remain test fixtures only. No unstated row is promoted merely by this summary.

## Authority hierarchy

When sources conflict or a rule is ambiguous, use this order:

1. Published second-printing errata or another published official ruling that clearly supersedes the source.
2. Exact card text.
3. *Guards of Atlantis II* second-printing rulebook.
4. Direct publisher or designer clarification.
5. A temporary interpretation explicitly approved by the user and recorded below.

Card text overrides conflicting general rules unless a higher official correction specifically changes that card.

Do not use an unofficial forum answer, convenience behavior, or remembered tabletop practice as authority without verification.

## Primary local sources

- Rulebook: `Docs/Guards of Atlantis II - 2nd Printing_web.pdf`
- Card references: `Docs/Guards of Atlantis II - Tabletopia Cards/`
- Active hero-card extraction: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/`
- Wasp references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Wasp/`
- Arien references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Arien/`
- Brogan references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Brogan/`
- Sabina references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Sabina/`
- Tigerclaw references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Tigerclaw/`
- Xargatha references: `Docs/Guards of Atlantis II - Tabletopia Cards/active-hero-cards/Xargatha/`

Milestone 8.1 completed the inventory and verification pass for **Wasp's faces**, recorded in [Rules/HERO_WASP.md](Rules/HERO_WASP.md#wasp-content-ledger); milestones 8.18a-8.18e did the same for **Arien**, **Brogan**, **Sabina**, **Tigerclaw**, and **Xargatha** in the [Arien](Rules/HERO_ARIEN.md#arien-content-ledger), [Brogan](Rules/HERO_BROGAN.md#brogan-content-ledger), [Sabina](Rules/HERO_SABINA.md#sabina-content-ledger), [Tigerclaw](Rules/HERO_TIGERCLAW.md#tigerclaw-content-ledger), and [Xargatha](Rules/HERO_XARGATHA.md#xargatha-content-ledger) shards. Milestones 8.25e5-8.25e5e independently reread, corrected, and hash-pinned the 114 fronts and then closed F-SOURCE-01. The user's later runtime report disproved one result those reviews had declared clean: direct inspection of `xargatha2__7632831__front.png` shows Siren's Call's left ribbon prints Defense 3 and no Movement, while the ledger and production had transposed it to Defense absent / Movement 3. Milestone 8.25e5f corrected those values, and milestone 8.25e5f1 corrected the separately demonstrated effect defect so its printed `if able` and `up to 3 spaces` remain distinct typed clauses and offer legal positive paths of one through three ending adjacent. Milestone 8.25e5g then independently reread the entire unchanged face before tracked comparison, reconciled every field and effect clause, and confirmed the 114-front manifest byte-identical; the other 113 results therefore carry forward and F-SOURCE-01 is closed. A later higher-authority source still supersedes these readings. For every hero outside those six the extraction's presence still does not prove completeness, correctness, or edition, and it proves distribution rights for no hero at all.

**Edition of the extraction (verified 2026-09-18).** The card extraction is Tabletopia setup 799373 as downloaded on 2026-07-29. On 2026-09-18 the user checked five physical second-printing cards against it (Ursafar's Angry Roar and Unbound Fury, Wasp's Static Barrier and Crackling Dagger, Tigerclaw's Cloak and Daggers): every rules-bearing word matches, and only commas differ, in either direction. The extraction is therefore treated as the printed second edition for every card it could be checked against and as the working assumption for the rest, the same edition as the rulebook; a card-versus-rulebook disagreement is weighed on its merits (rulebook gap, stale illustration, typesetting) rather than attributed to edition skew. Tabletopia's later setups (bundle s804629, 2026-09-18) carry newer texts on seven decks, catalogued in `Docs/Guards of Atlantis II - Tabletopia Cards/CARD-DIFF-s804629-vs-s799373.md`; they show where the third printing is heading and are authority for no face. The 2026-09-06 reading that called the extraction third-printing playtest material is superseded by this paragraph.

`Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md` record project design decisions derived from these sources. They are mandatory implementation guidance but are not game-rule authorities and never replace source verification.

## Prototype setup baseline

The final private pitch uses the standard six-player Quick Game setup in both director and private-online validation modes, with the following prototype-only control and tooling boundaries:

- Single-lane board.
- Standard official zones, terrain, Thrones, spawn points, and minion placement.
- Three Wave counters.
- Five Life counters per team.
- Three Red heroes and three Blue heroes, all different: Red Wasp, Xargatha and Tigerclaw; Blue Arien, Brogan and Sabina. Their 114-front source inventory is independently verified and F-SOURCE-01 is closed.
- Director mode lets one controller operate all six heroes and inspect their hands; the private-online playtest instead preserves one-player/one-hero control and owner-private hands.
- Normal setup flips the Tie Breaker coin; internal tests may explicitly set its initial side.

Milestone 8.25 made that setup the configuration a player loads. Earlier synthetic, mirrored-Wasp and one-hero-per-team configurations remain staged development fixtures asked for by name, not the final pitch setup. Do not rebalance Life, Waves, minions, card values, economy, or any hero. Any later balance adjustment requires explicit approval and a new deviation entry.

## Opt-in digital variants, not implemented

The user approved the [experimental turn-timer design](Project/EXPERIMENTAL_FEATURES.md) on 2026-10-05: configured decision allowances and team banks, legal timeout End Turn/Undo, automatic answers and voting fallbacks. This is a proposed opt-in digital rules variant, not an official board-game interpretation or shipped deviation. Experiments reset off for each new hosted game. Only documentation is authorized by 9.55; 9.56 queues implementation and its exact validation gate. Standard rules and existing Undo limits remain unchanged.

The user also approved the [Teammate coordination design](Project/EXPERIMENTAL_FEATURES.md#teammate-coordination): a separate per-match host toggle permits allied hand inspection and right-click hex/card suggestions. This proposed digital visibility/communication variant grants no teammate gameplay control and reveals no enemy private information. 9.57 records the design only; 9.58 queues implementation. Standard option-off behavior remains unchanged.

## Approved deviations

| ID | Deviation | Reason | Impact | Removal or review trigger |
| --- | --- | --- | --- | --- |
| DEV-001 | **Retired at 8.25.** Staged development fixtures use one hero per team before the final pitch setup | Proved the complete core loop and individual hero content before six-hero integration | Material balance and board-population change; removed team coordination | Retired 2026-08-26: the configuration a player loads places three Red and three Blue heroes at the official six-player pitch-setup gate. The one-hero-per-team boards survive only as named fixtures whose subject is one printed set |
| DEV-002 | **Retired at 8.25.** Staged Wasp integration mirrors Wasp across both teams | Proved the first complete verified hero before the other five were selected and implemented | Mirror interactions are not legal final-pitch roster evidence | Retired 2026-08-26: six unique heroes replace the mirror in the loaded configuration, Wasp appears once, and the mirror remains a named fixture for the cases that measure one seat against its own reflection |
| DEV-003 | Director mode lets one controller see and control all six pitch hands | Enables private development and demonstration without AI or requiring six participants | Removes hidden information, table talk, and independent player decisions in director mode only | Keep as an internal/demo mode; milestone 8.30a proved the per-seat hidden-information half locally, and milestone 8.36 must still prove per-player hidden control over the invite-only online 3v3 path |
| DEV-004 | Internal Undo, restart, snapshot tools, event log, animation speed, hidden-state inspection, and the milestone 8.35z17 Director sandbox that composes an arbitrary staged board - heroes placed, minions spawned, removed or relocated, a chosen hand, a Level and a purse, Wave and Life counters - and then plays it under the ordinary authoritative rules | Reproducible testing and demonstrations | Allows actions unavailable in competitive play, and a staged board deliberately bypasses p.6's setup restrictions. Placed tokens and markers, and the mid-round cursor, are refused rather than staged, so no board object gains an origin no rule authored and no history is asserted that did not happen | Keep development-build only; never expose in real online matches. Milestone 8.35z18 enforces that in the launch parser rather than by convention: the sandbox flag is refused beside the host, client and lobby roles, so the one route by which a staged board could reach a real online match - a host composing one before its seats join - does not parse |
| DEV-008 | In rulebook pp.5-6's four-player setup, each of the two heroes on a team keeps the spawn point it already stands on in the six-player board, rather than the printed setup naming which of a Throne's three marked points a two-hero team occupies | p.6 step 9 says only to place the miniatures on the marked spaces of their own Throne Zone, and does not say which two of the three a four-player team takes. Answered the way milestone 8.25 answered the identical question about which dashboard goes to which player: the points are interchangeable at setup | None observable. A draw between interchangeable spawn points changes nothing any rule, projection or snapshot can distinguish, and every printed format shares p.6 step 10's same twelve minion placements | Revisit if an official ruling, an errata, or any future card distinguishes the three marked Throne spawn points from one another |
| DEV-005 | Internal override for initial Tie Breaker side | Deterministic focused tests | Bypasses the setup coin flip only when explicitly selected | Normal match flow must still flip randomly; online authority owns setup result |
| DEV-007 | A Clear with no token adjacent to the acting hero is offered greyed and refuses a start, rather than being performed to remove nothing | p.13 permits removing "any number of tokens adjacent to you" and zero is a number, so performing one that removes nothing is legal by the letter; this build refuses an action it can prove will do nothing, which is the rule milestone 6.5af began for an inert Skill and 7.8a1 continued for a targetless Attack, and p.12's Hold is what makes the refusal cost the player only an action they were never forced to take | The hero spends the turn on Hold, a printed secondary or Fast Travel instead; the only fact lost is that the turn was spent on a Clear rather than a Hold, which milestone 8.9's `PerformedBasicAction` does distinguish. Clearing none stays legal wherever a token *is* adjacent, so p.13's "any number" is unchanged where it can mean anything | Revisit when any card in a configured match reads "performs a Clear" as a trigger, or when an official ruling states that performing a Clear which removes nothing has an effect |
| DEV-006 | **Retired at 8.13 for configured content.** A printed primary whose text has no typed composition is greyed and refuses a start, naming what owes it | Milestone 8.4 dealt verified content before later slices composed its text; performing values while silently dropping the sentence would violate the rules | Every face the configured Wasp mirror instantiates is composed through High Voltage. The guard remains live for future content; only uninstantiated Handicap card Crackling Dagger retains its named odd-player-count deferral | Retired 2026-08-23 for the current configuration; reopen only when configured content carries outstanding composition |

## Status vocabulary

- **Not inventoried:** Required source material has not been checked for completeness.
- **Source reviewed:** The named local source section has been inspected; this does not prove card-corpus completeness or that all applicable errata have been found.
- **Source verified:** Rule or card data has been checked against the authority hierarchy.
- **Architecture specified:** Digital boundaries needed to represent the rule are documented but not implemented.
- **Specified:** Exact digital behavior and edge cases are documented but not implemented.
- **Implemented:** Authoritative rule behavior exists.
- **Focused tests passing:** Narrow automated scenarios cover the implemented behavior.
- **Milestone verified:** The applicable focused, regression, source, presentation, and gate checks from `TESTING.md` pass for the approved milestone.
- **Blocked:** A missing source or decision prevents faithful implementation.

## Rule-system ledger

The per-system rule status table - primary source, current status, and required verification notes - is maintained in [Rules/RULE_SYSTEMS.md](Rules/RULE_SYSTEMS.md#rule-system-ledger). The systems that put something on the board, move it, or take it off keep their rows in [Rules/BOARD_OBJECT_SYSTEMS.md](Rules/BOARD_OBJECT_SYSTEMS.md#board-and-object-rule-systems), and the intrinsic-versus-effective unit-type declaration in [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md#unit-type-question-ledger); both were split out at milestone 7.8e.

## Printed wording readings

How printed words ("may", "up to", "if able", "repeat", targeting and condition words) become composed rules for every hero, with the user's wording rulings W1-W3, is maintained in [Rules/PRINTED_WORDING.md](Rules/PRINTED_WORDING.md).

## Card architecture coverage

The rule-pressure to architecture-seam map, with the milestone that proves each seam, is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#card-architecture-coverage).

## Effect capability coverage

The capability-family catalog - primary general source, architecture status, and planned proving or integration milestones - is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#effect-capability-coverage).

## Hero content ledgers

[Garrus the Gladiator](Rules/HERO_GARRUS.md#garrus-content-ledger) is the selected sixteenth hero; his source inventory and capability mapping are owned by that shard, with implementation tracked in [the sixteenth-hero series](Roadmap/PHASE_8_SIXTEENTH_HERO.md). This inventory alone makes no implemented-content claim.

[Widget and Pyro](Rules/HERO_WIDGET.md#widget-content-ledger) are selected for the seventeenth hero. That shard owns the nineteen-face inventory, Pyro's printed token properties and outstanding interpretation questions; [the seventeenth-hero series](Roadmap/PHASE_8_SEVENTEENTH_HERO.md) owns implementation. An inventory does not make Widget playable.

[Rowenna the Vanguard](Rules/HERO_ROWENNA.md#rowenna-content-ledger) is selected for the eighteenth hero. That shard owns the nineteen-face inventory, the stat bars, the printed colour-letter conflict RO-03 and the open readings; [the eighteenth-hero series](Roadmap/PHASE_8_EIGHTEENTH_HERO.md) owns implementation. An inventory does not make Rowenna playable.

[Min the Dragonmonk](Rules/HERO_MIN.md#min-content-ledger) is selected for the nineteenth hero. That shard owns the nineteen-face inventory, the stat bars, her three printed token kinds and the open readings; [the nineteenth-hero series](Roadmap/PHASE_8_NINETEENTH_HERO.md) owns implementation. An inventory does not make Min playable.

[Brynn the Seeker](Rules/HERO_BRYNN.md#brynn-content-ledger) is selected for the twentieth hero. That shard owns the nineteen-face inventory, the stat bars, the one printed predicate his set is built on and the open readings; [the twentieth-hero series](Roadmap/PHASE_8_TWENTIETH_HERO.md) owns implementation. An inventory does not make Brynn playable.

[Hanu the Trickster](Rules/HERO_HANU.md#hanu-content-ledger) is selected for the twenty-first hero. That shard owns the nineteen-face inventory, the stat bars, the four printed mechanisms no earlier hero carried and the open readings; [the twenty-first-hero series](Roadmap/PHASE_8_TWENTY_FIRST_HERO.md) owns implementation. An inventory does not make Hanu playable.

[Ignatia the Mad](Rules/HERO_IGNATIA.md#ignatia-content-ledger) is selected for the twenty-second hero. That shard owns the nineteen-face inventory, the stat bars, the coin-selected two-text mechanism no earlier hero carried, her magma tokens and the open readings; [the twenty-second-hero series](Roadmap/PHASE_8_TWENTY_SECOND_HERO.md) owns implementation. An inventory does not make Ignatia playable.

[Trinkets the Scavenger](Rules/HERO_TRINKETS.md) is selected for the twenty-third hero. His shard owns the nineteen-face inventory, the user's one-Turret and three-Barrier rulings, the supplied designer timing answer, and the capability map; [the twenty-third-hero series](Roadmap/PHASE_8_TWENTY_THIRD_HERO.md) owns implementation. An inventory does not make Trinkets playable.

[Snorri the Runescribe](Rules/HERO_SNORRI.md) is selected for the twenty-fourth hero. His shard owns the nineteen-face inventory, persistent turn-slot rune exception, component identities, capability map and bounded questions; [the twenty-fourth-hero series](Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md) owns implementation. An inventory does not make Snorri playable.

[Mrak the Rockshaper](Rules/HERO_MRAK.md) is selected for the twenty-fifth hero. His shard owns the nineteen-face inventory, the rock token and printed secondary Attack readings, the capability map and questions MR-Q1 to MR-Q4; [the twenty-fifth-hero series](Roadmap/PHASE_8_TWENTY_FIFTH_HERO.md) owns implementation. An inventory does not make Mrak playable.

[Wuk the Grove Keeper](Rules/HERO_WUK.md) is selected for the twenty-sixth hero. His shard owns the nineteen-face inventory, the Tree token readings, the capability map and the user-settled questions WU-Q1 to WU-Q4; [the twenty-sixth-hero series](Roadmap/PHASE_8_TWENTY_SIXTH_HERO.md) owns implementation. An inventory does not make Wuk playable.

[Mortimer the Awakener](Rules/HERO_MORTIMER.md) is selected for the twenty-seventh hero. His shard owns the nineteen-face inventory, the Zombie token readings, the capability map and the user-settled questions MO-Q1 to MO-Q4; [the twenty-seventh-hero series](Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md) owns implementation. An inventory does not make Mortimer playable.
[Razzle](Rules/HERO_RAZZLE.md) is selected for the twenty-eighth hero. His shard owns the inventory 9.21 verifies, the many-bodies rulebook intersections and the RZ-Q questions the user settles. [His build record](Rules/HERO_RAZZLE_BUILD.md) owns what each 9.21 block built and left open.
[Gydion the Archwizard](Rules/HERO_GYDION.md) is selected for the twenty-ninth hero. His shard owns the nineteen fronts and twenty-two spells 9.30 verifies, the spellbook readings, the designer's two answers and the GY-Q questions the user settles; [the twenty-ninth-hero series](Roadmap/PHASE_9_TWENTY_NINTH_HERO.md) owns implementation. An inventory does not make Gydion playable.
[Takahide the Warlord](Rules/HERO_TAKAHIDE.md) is selected for the thirtieth hero. His shard owns the twenty-one fronts 9.32 verifies, the three-gold-card and card-swap readings and the TK-Q questions the user settles; [the thirtieth-hero series](Roadmap/PHASE_9_THIRTIETH_HERO.md) owns implementation. An inventory does not make Takahide playable.

[Emmitt the Traveller](Rules/HERO_EMMITT.md) and [NebKher the Harbinger](Rules/HERO_NEBKHER.md) are the user-authorized thirty-first and thirty-second heroes. Their shards own the nineteen-front inventories, the three Glitch and three Illusion token supplies, and the user's EM-Q1-EM-Q2 and NB-Q1-NB-Q4 readings; [the final-two-hero sequence](Roadmap/PHASE_9_FINAL_TWO_HEROES.md) owns implementation. Source inventory alone does not make either hero playable.

Hero-scoped rule authority is maintained one shard per approved hero under [Rules/](Rules/), named `Rules/HERO_<NAME>.md`. Each owns that hero's inventory, verification status, deviations, and questions. Wasp, [Arien](Rules/HERO_ARIEN.md#arien-content-ledger), [Brogan](Rules/HERO_BROGAN.md#brogan-content-ledger), [Sabina](Rules/HERO_SABINA.md#sabina-content-ledger), [Tigerclaw](Rules/HERO_TIGERCLAW.md#tigerclaw-content-ledger), [Xargatha](Rules/HERO_XARGATHA.md#xargatha-content-ledger), [Dodger](Rules/HERO_DODGER.md#dodger-content-ledger), [Misa](Rules/HERO_MISA.md#misa-content-ledger), [Silverarrow](Rules/HERO_SILVERARROW.md#silverarrow-content-ledger), [Ursafar](Rules/HERO_URSAFAR.md#ursafar-content-ledger), [Whisper](Rules/HERO_WHISPER.md#whisper-content-ledger), and [Tali](Rules/HERO_TALI.md#tali-content-ledger) are inventoried, capability-mapped, composed, registered, dealt, and player-path gated. Dodger joined that list at milestone 8.35z34, Misa at 8.35z38, Silverarrow at 8.35z46, Ursafar at 8.35z50, Whisper at 8.35z53, and Tali at 8.37, and all six are from outside the pitch line-up milestone 8.17 approved. **The six-hero content wave is fully dealt as of 8.37**, so no hero this project has composed is still waiting on a dealing milestone - with the one qualification milestone 8.35z38 measured, that a contract SHARED between a dealt hero and an undealt one registers with the dealt one. Milestone 8.37a then composed Blizzard's End-of-turn repeat, the one face that had remained, so no hero carries an engine deferral at all. Wasp, Arien, and Brogan also carry clean independent source review; the other three carry the explicit 8.25e5 findings above rather than an unqualified verification claim. Their shared typed history, target/set/line, value, Defense, retrieval, resource, persistent-source, and pausing-source contracts are live in the production catalogs. [Rules/ROSTER_CAPABILITY_MATRIX.md](Rules/ROSTER_CAPABILITY_MATRIX.md) owns the exact cross-hero mapping. **[Swift](Rules/HERO_SWIFT.md#swift-content-ledger) completed all six at milestone 8.38f** and is inventoried, capability-mapped, composed, registered, dealt and player-path gated - the THIRTEENTH hero and the first after the package arc closed. She is the one hero this ledger had to say those apart for, because her route was a lettered series rather than the single slice every hero before her arrived through: 8.38 inventoried, 8.38a-8.38c2 built her vocabulary, 8.38d composed, 8.38e dealt and artworked, and 8.38f gated. Her shard owns her own capability contract map rather than the matrix, and says why. The [thirteenth-hero shard](Roadmap/PHASE_8_THIRTEENTH_HERO.md) owns her completed route. **[Cutter](Rules/HERO_CUTTER.md#cutter-content-ledger) completed all six at milestone 8.39j** - the FOURTEENTH hero, named by the user on 2026-09-11 while Swift was still in flight, and inventoried, capability-mapped, composed, registered, dealt and player-path gated across thirteen blocks, 8.39 through 8.39j. Her shard owns her own capability contract map for the reason Swift's does, and records one decision the user settled rather than the shard: A Fistful of Coins prints a SOLO victory sentence this engine had no shape for, and 8.39g implemented it faithfully. The [fourteenth-hero shard](Roadmap/PHASE_8_FOURTEENTH_HERO.md) owns her completed route. **[Bain](Rules/HERO_BAIN.md#bain-content-ledger) is inventoried and capability-mapped at milestone 8.40 on the same terms** - the FIFTEENTH hero, named by the user on 2026-09-13, and nothing else yet. His shard owns his own capability contract map for the reason Swift's and Cutter's do, and it is the first hero shard whose second-source check found the rulebook NAMING the hero - once, in a p.2 flavour quote carrying no rule, so the extraction is still sole authority. The [fifteenth-hero shard](Roadmap/PHASE_8_FIFTEENTH_HERO.md) owns his route.

Milestone 8.36f3's card-by-card reconciliation and closing dispositions for the five added dealt
sets are indexed in [Rules/ADDED_ROSTER_AUDIT.md](Rules/ADDED_ROSTER_AUDIT.md).

Rule systems that span heroes stay in the `Rule-system ledger` above, and a deviation or open question that affects more than one hero stays in this document rather than being copied into each hero's shard.

## Rule implementation template

Add or expand an entry before implementing a rule:

```markdown
### RULE-XXX - Short name

- Source:
- Exact behavior:
- Timing:
- Valid targets/choices:
- State changes:
- Interactions and edge cases:
- Approved deviations: None / DEV-XXX
- Implementation status:
- Focused tests:
- Open questions:
```

## Open source and rules questions

The numbered register of open and answered source-and-rules questions is maintained in [Rules/OPEN_QUESTIONS.md](Rules/OPEN_QUESTIONS.md#open-source-and-rules-questions), split out at milestone 8.35z53 for the reason milestone 7.8e split the rule-system tables: the register is precise active authority and grows one row per answered question, so it is moved to an indexed shard rather than compressed. They do not block documentation, and become prerequisites only when their roadmap milestone is approved.


## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
