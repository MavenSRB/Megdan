# Guards Digital - Rules Ledger

Last updated: 2026-08-14

## Purpose

This ledger prevents silent rule drift. It connects each game system to its source, approved deviations, implementation status, and focused tests.

The project contains the completed Unity shell, automated test infrastructure, generic hex-coordinate foundation, placeholder coordinate-to-XZ presentation, authoritative hero/minion/token occupancy, typed command/event card actions, collection-backed participants, visible restorable Movement/Attack/Defense resolution, Tie Breaker state, repeated four-turn synthetic rounds with resumable end-of-round progression, deterministic schema-versioned snapshot capture/restore, accepted-command-only director Undo/restart, the complete official map with validated ordered Lane, and the exact-map configured mirrored-Wasp 1v1 Quick Game setup. Milestones through 6.3 are complete. All rule systems remain unimplemented unless stated otherwise.

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

The card extraction has not yet completed an implementation-grade inventory and verification pass. Its presence does not prove completeness, correctness, edition, or distribution rights.

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

Hero-scoped rule authority is maintained one shard per approved hero under [Rules/](Rules/), named `Rules/HERO_<NAME>.md`. Each owns that hero's content inventory, capability mapping, source-verification status, and any deviation or open question scoped to that hero alone. Wasp's is [Rules/HERO_WASP.md](Rules/HERO_WASP.md#wasp-content-ledger), and it records the implementation-grade inventory milestone 8.1 must produce before any verified Wasp definition or effect is implemented; the five heroes joining Wasp are selected at milestone 8.17 and take their shards then.

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

1. Is the extracted Wasp set complete, readable, and from the correct edition?
2. What published second-printing errata or official FAQ currently applies?
3. Retired on 2026-08-14 by user decision: answered. The user-supplied official axial-coordinate diagram verified every official single-lane board coordinate, zone boundary, terrain space, and spawn point, as the official board inventory row above records.
4. Retired for the final pitch on 2026-08-10: mirrored-Wasp interactions remain relevant only to the intermediate DEV-002 test gate and cannot become final-pitch authority.
5. Retired on 2026-08-10: the final pitch no longer uses the four-player 1v1 economy; it uses the official six-player Quick Game baseline without rebalancing.
6. Which five additional heroes will join Wasp in the six-hero pitch roster? Selection requires explicit user approval and implementation-grade source availability before inventory begins.
7. Retired for the current synthetic slice on 2026-08-16 by explicit user decision. Rulebook p.17's replacement doctrine and the published Brogan/Warlord wording establish that modified minion identity participates in Minion Battle, but no located official source defines removal consequences for a hero in that set. The approved interpretation is recorded rather than presented as sourced text: the hero counts, may be selected under the exact-count/Heavy-last removal contract, leaves the board by Remove rather than Defeat with no coins or Life change, and receives the ordinary p.7 optional Hero Respawn choice at its next action opportunity. A later official clarification supersedes this interpretation under the authority hierarchy. The unit-type question ledger in [Rules/UNIT_TYPE_QUESTIONS.md](Rules/UNIT_TYPE_QUESTIONS.md#unit-type-question-ledger) carries the implemented record.
8. Recorded at 6.8g: what is a defeated object that counts as a minion worth? p.13 prices melee/ranged at 2 coins, heavy at 4, and heroes by level; no clause prices a counts-as defeat, so defeat rewards keep reading intrinsic identity until settled.
9. Recorded at 6.4a: when one action displaces several of one team's minions outside the Battle Zone, in what order are they corrected? p.18 orders nothing within a team, and order matters because one correction can free or block another's shortest path. Implemented per the 2026-08-16 user decision as the owning team's choice - a subject-selection stage before each route or Place choice - until settled by source or official ruling.
10. Recorded at 6.4a: where does a displaced minion go when the Battle Zone has no empty space for the fallback Place? p.18 places it into the Battle Zone and no clause covers a full zone. Implemented as a typed left-in-place outcome - the minion stays where it ended up and the correction records that it could not act - until settled.
11. Recorded at 6.5b, answered on 2026-08-16 by explicit user decision: may a card's `Choose one` offer an option the actor cannot perform at all? Hands-on validation found that it does - both of Red Bombard's modes are unperformable from the saved scene's starting position, and choosing either spends the card in silence. The local rulebook was searched on 2026-08-16 with `pdftotext`, which reads the second-printing PDF here even though page rendering does not - the earlier claim that it could not be searched was wrong and is corrected here rather than left standing. That search found no clause either way: page 10 says only to choose a bullet point and apply its text, and no page states whether an option the actor cannot perform is offerable. The approved interpretation is therefore still recorded as an interpretation rather than presented as sourced text: an option the actor cannot perform is refused rather than offered, and the player is told that it cannot be done rather than left to discover it by spending a card. A later official clarification supersedes this under the authority hierarchy. Corrective milestone 6.5ac implements it: the option is projected greyed and unselectable, with no reason text by the same decision's refinement, and where no option of a choice can be performed the node reported itself impossible rather than pausing, so in that one case the player was not told before the action stopped - recorded in 6.5ac's completion evidence with the reason. Corrective milestone 6.5ae closed it on the user's 2026-08-16 decision that End Turn is the one legal command there, offered rather than performed: a mandatory choice with nothing performable now pauses with every option greyed and offers that stop, and the player clicks it. The decision is a decision about presentation rather than about the rule, so p.10's stop is unchanged in what it does. A further search on 2026-08-16, prompted by the user asking what the rules already do about a card that can achieve nothing, found the clause the earlier search had missed, and it supports rather than contradicts the interpretation: **p.12's Hold action**. Hold has no icon, can be performed with any Action card, and means doing nothing that turn; the page names an impossible or disadvantageous action as the common reason to perform one, and states that because any card can Hold, a player is never forced to perform an action. That does not settle whether an unperformable option may be *offered inside a choice*, which is what this question asks and which remains an approved interpretation. What it does settle is the surrounding expectation: the rules assume a player can see that an action is impossible before choosing it, which is why corrective milestone 6.5af judges a Skill when its action is offered rather than after it is started.

## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
