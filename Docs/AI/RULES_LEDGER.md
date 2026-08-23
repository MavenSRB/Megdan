# Guards Digital - Rules Ledger

Last updated: 2026-08-20

## Purpose

This ledger prevents silent rule drift. It connects each game system to its source, approved deviations, implementation status, and focused tests.

The project contains the completed Unity shell, automated test infrastructure, generic hex-coordinate foundation, placeholder coordinate-to-XZ presentation, authoritative hero/minion/token occupancy, typed command/event card actions, collection-backed participants, visible restorable Movement/Attack/Defense resolution, Tie Breaker state, repeated four-turn synthetic rounds with resumable end-of-round progression, deterministic schema-versioned snapshot capture/restore, accepted-command-only director Undo/restart, the complete official map with validated ordered Lane, and the exact-map configured mirrored-Wasp 1v1 Quick Game setup. Milestones through 8.28b are complete. All rule systems remain unimplemented unless stated otherwise.

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

Milestone 8.1 completed the inventory and verification pass for **Wasp's faces**, recorded in [Rules/HERO_WASP.md](Rules/HERO_WASP.md#wasp-content-ledger); milestone 8.18a did the same for **Arien's** in [Rules/HERO_ARIEN.md](Rules/HERO_ARIEN.md#arien-content-ledger), and milestone 8.18b for **Brogan's** in [Rules/HERO_BROGAN.md](Rules/HERO_BROGAN.md#brogan-content-ledger). For every hero outside those three the extraction's presence still does not prove completeness, correctness, or edition, and it proves distribution rights for no hero at all.

`Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md` record project design decisions derived from these sources. They are mandatory implementation guidance but are not game-rule authorities and never replace source verification.

## Prototype setup baseline

The final private pitch uses the standard six-player Quick Game setup in both director and private-online validation modes, with the following prototype-only control and tooling boundaries:

- Single-lane board.
- Standard official zones, terrain, Thrones, spawn points, and minion placement.
- Three Wave counters.
- Five Life counters per team.
- Three Red heroes and three Blue heroes, all different: Wasp plus five later user-approved, source-verified heroes.
- Director mode lets one controller operate all six heroes and inspect their hands; the private-online playtest instead preserves one-player/one-hero control and owner-private hands.
- Normal setup flips the Tie Breaker coin; internal tests may explicitly set its initial side.

Earlier synthetic and mirrored-Wasp 1v1 configurations are staged development fixtures, not the final pitch setup. Do not rebalance Life, Waves, minions, card values, economy, or any hero. Any later balance adjustment requires explicit approval and a new deviation entry.

## Approved deviations

| ID | Deviation | Reason | Impact | Removal or review trigger |
| --- | --- | --- | --- | --- |
| DEV-001 | Staged development fixtures use one hero per team before the final pitch setup | Proves the complete core loop and individual hero content before six-hero integration | Material balance and board-population change; removes team coordination | Retire at the official six-player pitch-setup gate before final presentation |
| DEV-002 | Staged Wasp integration mirrors Wasp across both teams | Proves the first complete verified hero before the other five are selected and implemented | Mirror interactions are not legal final-pitch roster evidence | Retire when the six unique pitch heroes replace the Wasp mirror at the official six-player setup gate |
| DEV-003 | Director mode lets one controller see and control all six pitch hands | Enables private development and demonstration without AI or requiring six participants | Removes hidden information, table talk, and independent player decisions in director mode only | Keep as an internal/demo mode; milestone 8.36 must separately prove per-player hidden control in the invite-only online 3v3 path |
| DEV-004 | Internal Undo, restart, snapshot tools, event log, animation speed, and hidden-state inspection | Reproducible testing and demonstrations | Allows actions unavailable in competitive play | Keep development-build only; never expose in real online matches |
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

## Card architecture coverage

The rule-pressure to architecture-seam map, with the milestone that proves each seam, is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#card-architecture-coverage).

## Effect capability coverage

The capability-family catalog - primary general source, architecture status, and planned proving or integration milestones - is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#effect-capability-coverage).

## Hero content ledgers

Hero-scoped rule authority is maintained one shard per approved hero under [Rules/](Rules/), named `Rules/HERO_<NAME>.md`. Each owns that hero's content inventory, capability mapping, source-verification status, and any deviation or open question scoped to that hero alone. Wasp's is [Rules/HERO_WASP.md](Rules/HERO_WASP.md#wasp-content-ledger); milestone 8.1 filled it with the source-verified content inventory, milestone 8.2 added the capability matrix mapping every action onto implemented contracts, and milestone 8.3's definitions complete the remaining owed fields. Milestone 8.17 approved the five heroes joining Wasp, and each takes its shard at its own inventory milestone after the 8.2 re-baseline split 8.18 five ways: Arien's is [Rules/HERO_ARIEN.md](Rules/HERO_ARIEN.md#arien-content-ledger), written at milestone 8.18a, and Brogan's is [Rules/HERO_BROGAN.md](Rules/HERO_BROGAN.md#brogan-content-ledger), written at milestone 8.18b; both carry the source-verified inventory only - the capability matrix for all five newcomers is produced roster-wide at milestone 8.19, so an 8.18 shard states what is printed and claims no mapping.

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

These do not block documentation. They become prerequisites only when their roadmap milestone is approved.

1. Answered for Wasp on 2026-08-21 by milestone 8.1 and for Arien and Brogan on 2026-08-23 by milestones 8.18a and 8.18b; open for the other twenty-nine heroes. **Brogan**: nineteen faces in the standard colour shape, every front readable, three of them illustrated by the second-printing rulebook across two pages and all three reproducing the extracted faces exactly - **the only verified hero so far whose edition answer carries no wording caveat at all**. His dashboard back is the same blank placeholder, answered "no tokens or markers" by question 14; his profile stat bars resolve to exact values. See [Rules/HERO_BROGAN.md](Rules/HERO_BROGAN.md#brogan-source-faces). **Arien**: nineteen faces in the same standard colour shape, every front readable to the last printed word, and seven of them illustrated by the second-printing rulebook across four pages. Five of the seven reproduce the extracted faces exactly; two differ in wording without differing in meaning, and unlike Wasp's single reminder-text difference one of Arien's falls in the operative card text, so the authority hierarchy is what decides it rather than a clarification convention. His dashboard back is the same unreadable blank placeholder, raised as A-01 and answered "no tokens or markers" by question 14's general ruling; his profile stat bars do resolve to exact values, so Arien has no counterpart to W-02. See [Rules/HERO_ARIEN.md](Rules/HERO_ARIEN.md#arien-source-faces). **Wasp**: her set is complete - nineteen faces, being p.5's eighteen-card deck plus the p.20 Handicap card, in the standard colour shape - and every action-card front is readable to the last printed word. Three of the four Wasp cards the second-printing rulebook illustrates reproduce the extracted faces exactly, and the fourth differs only in reminder text, so the faces are consistent with the second printing. Two faces are not readable and were recorded as Wasp-scoped open questions rather than treated as answered: the dashboard back, which p.6 says carries a hero's tokens and markers, is a blank Tabletopia placeholder - W-01, since answered by question 14's general ruling - and the profile card's stat bars, which do not resolve to exact values and remain open as W-02. See [Rules/HERO_WASP.md](Rules/HERO_WASP.md#wasp-source-faces).
2. What published second-printing errata or official FAQ currently applies?
3. Retired on 2026-08-14 by user decision: answered. The user-supplied official axial-coordinate diagram verified every official single-lane board coordinate, zone boundary, terrain space, and spawn point, as the official board inventory row above records.
4. Retired for the final pitch on 2026-08-10: mirrored-Wasp interactions remain relevant only to the intermediate DEV-002 test gate and cannot become final-pitch authority.
5. Retired on 2026-08-10: the final pitch no longer uses the four-player 1v1 economy; it uses the official six-player Quick Game baseline without rebalancing.
6. Answered on 2026-08-23 by explicit user approval at milestone 8.17: which five additional heroes join Wasp in the six-hero pitch roster? The rulebook's own recommended first-game line-up, chosen over an analysis-recommended alternative - **Red: Wasp the Warmaiden, Xargatha the Changed, Tigerclaw the Cutpurse; Blue: Arien the Tidemaster, Brogan the Destroyer, Sabina the Gunslinger** - which setup step 7 names for a six-player game, so the team slots are printed by the source rather than invented. Implementation-grade source availability was confirmed for all five before inventory began: nineteen readable faces each, and the only unreadable component is the dashboard back that question 14 has since settled generally. Each hero takes its own shard at its own 8.18 inventory slice; Arien's is [Rules/HERO_ARIEN.md](Rules/HERO_ARIEN.md#arien-content-ledger), written at 8.18a.
7. Retired for the current synthetic slice on 2026-08-16 by explicit user decision. Rulebook p.17's replacement doctrine and the published Brogan/Warlord wording establish that modified minion identity participates in Minion Battle, but no located official source defines removal consequences for a hero in that set. The approved interpretation is recorded rather than presented as sourced text: the hero counts, may be selected under the exact-count/Heavy-last removal contract, leaves the board by Remove rather than Defeat with no coins or Life change, and receives the ordinary p.7 optional Hero Respawn choice at its next action opportunity. A later official clarification supersedes this interpretation under the authority hierarchy. The unit-type question ledger in [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md#unit-type-question-ledger) carries the implemented record. **Factual correction recorded 2026-08-23 at milestone 8.18b, without reopening the retirement:** the clause above stating that no located official source defines the removal consequences is no longer true. Brogan the Destroyer's Ultimate, One Man Army, is now source-verified and prints them - "During minion battle you count as a heavy minion; if you would be removed, lose the push instead" - so the published case is never removed at all, which is not the consequence this question's interpretation supplies. That interpretation still governs the synthetic slice, whose hero carries no such clause, and whether a counts-as-minion hero without one should inherit Brogan's answer is a decision this entry does not make. The reading of Brogan's own clause, which is separately underspecified, is [his shard's question B-02](Rules/HERO_BROGAN.md#brogan-open-questions).
8. Recorded at 6.8g: what is a defeated object that counts as a minion worth? p.13 prices melee/ranged at 2 coins, heavy at 4, and heroes by level; no clause prices a counts-as defeat, so defeat rewards keep reading intrinsic identity until settled.
9. Recorded at 6.4a: when one action displaces several of one team's minions outside the Battle Zone, in what order are they corrected? p.18 orders nothing within a team, and order matters because one correction can free or block another's shortest path. Implemented per the 2026-08-16 user decision as the owning team's choice - a subject-selection stage before each route or Place choice - until settled by source or official ruling.
10. Recorded at 6.4a, **answered on 2026-08-23 by explicit user decision**: where does a displaced minion go when the Battle Zone has no empty space for the fallback Place? p.18 sends it to "the nearest empty space of the Battle Zone" and no clause covers a zone with none. The implemented behaviour is confirmed as the ruling: a typed left-in-place outcome - the minion stays where it ended up and the correction records that it could not act. The user's assessment is that the state is unreachable in practice and the ruling exists so that the code has a defined answer rather than because the case is expected; nothing changes in the build, and a later official ruling supersedes it.
11. Recorded at 6.5b, answered on 2026-08-16 by explicit user decision: may a card's `Choose one` offer an option the actor cannot perform at all? Hands-on validation found that it does - both of Red Bombard's modes are unperformable from the saved scene's starting position, and choosing either spends the card in silence. The local rulebook was searched on 2026-08-16 with `pdftotext`, which reads the second-printing PDF here even though page rendering does not - the earlier claim that it could not be searched was wrong and is corrected here rather than left standing. That search found no clause either way: page 10 says only to choose a bullet point and apply its text, and no page states whether an option the actor cannot perform is offerable. The approved interpretation is therefore still recorded as an interpretation rather than presented as sourced text: an option the actor cannot perform is refused rather than offered, and the player is told that it cannot be done rather than left to discover it by spending a card. A later official clarification supersedes this under the authority hierarchy. Corrective milestone 6.5ac implements it: the option is projected greyed and unselectable, with no reason text by the same decision's refinement, and where no option of a choice can be performed the node reported itself impossible rather than pausing, so in that one case the player was not told before the action stopped - recorded in 6.5ac's completion evidence with the reason. Corrective milestone 6.5ae closed it on the user's 2026-08-16 decision that End Turn is the one legal command there, offered rather than performed: a mandatory choice with nothing performable now pauses with every option greyed and offers that stop, and the player clicks it. The decision is a decision about presentation rather than about the rule, so p.10's stop is unchanged in what it does. A further search on 2026-08-16, prompted by the user asking what the rules already do about a card that can achieve nothing, found the clause the earlier search had missed, and it supports rather than contradicts the interpretation: **p.12's Hold action**. Hold has no icon, can be performed with any Action card, and means doing nothing that turn; the page names an impossible or disadvantageous action as the common reason to perform one, and states that because any card can Hold, a player is never forced to perform an action. That does not settle whether an unperformable option may be *offered inside a choice*, which is what this question asks and which remains an approved interpretation. What it does settle is the surrounding expectation: the rules assume a player can see that an action is impossible before choosing it, which is why corrective milestone 6.5af judges a Skill when its action is offered rather than after it is started.
12. Recorded at 8.1, answered on 2026-08-21 by explicit user decision at milestone 8.2: an item's +1 reaches a matching printed Range or Radius icon on an Ultimate card. p.16 says an item increases the matching icon "on each of your Action cards" and p.5's setup step counts the Tier IV card among the deck's Action cards; no clause addresses the Ultimate directly, so this is recorded as an approved interpretation rather than presented as sourced text, and a later official ruling supersedes it under the authority hierarchy. Concretely: a +1 Radius item makes Wasp's High Voltage Radius 4. Milestone 8.14 implemented it, proved over Wasp's own printed Radius item; it affects any hero whose Ultimate prints such an icon, which is why it lives here rather than in a hero shard.
13. Recorded at 8.2 as the Wasp-scoped question W-05, generalised and **answered from source on 2026-08-22 at milestone 8.9**: what does a card's "discards a card, or is defeated" require - is the second arm a choice, or the consequence of being unable to take the first? It is the consequence of inability, and no choice arises. p.10's Mandatory and Optional Steps settles it: "Steps which include `you may`, `up to` and `if able` are non-mandatory, all other steps are mandatory", and this clause carries none of the three, so the step is mandatory; p.10 stops an action only at a mandatory step that "for any reason you cannot complete", and the printed second arm is what makes this one always completable. p.17's `If Able` entry supplies the contrast the wording is built on - that keyword makes the preceding clause non-mandatory, so `if able` and `or is defeated` are two different answers to the same question, "what if you cannot?", and a card carries exactly one of them. The reading is not a choice for either player: p.10's Making Choices would give an unassigned choice to the card's controller, and its worked example assigns the affected hero only the choice of *which* card, so a choice reading would make the printed words "discards a card" unreachable. It is also not an election by the affected hero, because p.13 shows what the rulebook writes when it means one - "If the attacked hero has no cards in hand, is unable to defend, **or chooses not to**, that hero is defeated" - and this clause says no such thing. It is recorded here rather than in a hero shard because the construction spans heroes: the rulebook illustrates Arien the Tidemaster's Dangerous Current printing it on p.6, and Wasp's Electroblast prints it. The general rule is stated in the [Rule-system ledger](Rules/RULE_SYSTEMS.md#rule-system-ledger)'s ordered mandatory/optional row. **Milestone 8.10 implemented Wasp's instance on that reading**: Electroblast's rider is a discard that reports whether the chosen hero had a card, and one conditional arm running p.13's Defeat list where they did not - a branch, and no question either player is asked. A later official ruling supersedes the reading.

14. Recorded per-hero at 8.1 and 8.18a as the hero-scoped questions W-01 and A-01, generalised and **answered on 2026-08-23 by explicit user decision**: when a hero's dashboard back is unreadable, how is it established whether that hero owns tokens or markers? **By the hero's card faces, which are what define a hero's tokens and markers.** A set whose faces name, place or target none owns none, and a blank Tabletopia dashboard back is missing art rather than missing rules, so it is not evidence of a hidden supply and does not block that hero. It is recorded here rather than in a hero shard because all thirty-two dashboard backs in this extraction are blank placeholders in two byte-identical groups, so the question is the same question for every hero and a per-hero copy of it would be thirty-two copies of one rule. Two corroborating signals are available and were used at 8.18a without being required by the ruling: p.22 defines the profile deck's `Tokens` role as marking exactly the heroes "capable of placing tokens", and p.19 states that all tokens are obstacles placed from a supply, so a hero owning one has something for a face to name. **Wasp, Arien and Brogan are therefore answered "no tokens or markers"**, their shards record it, and the remaining 8.18 slices apply this rule rather than re-raising it. A readable dashboard back or a later official source supersedes the reading for the hero it covers.

## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
