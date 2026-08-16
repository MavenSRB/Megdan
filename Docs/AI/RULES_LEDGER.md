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

The per-system rule status table - primary source, current status, and required verification notes - is maintained in [Rules/RULE_SYSTEMS.md](Rules/RULE_SYSTEMS.md#rule-system-ledger).

## Card architecture coverage

The rule-pressure to architecture-seam map, with the milestone that proves each seam, is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#card-architecture-coverage).

## Effect capability coverage

The capability-family catalog - primary general source, architecture status, and planned proving or integration milestones - is maintained in [Rules/CARD_COVERAGE.md](Rules/CARD_COVERAGE.md#effect-capability-coverage).

## Wasp content ledger

The pitch requires Wasp's complete progression. Before verified Wasp definitions or effects are implemented, create an implementation-grade inventory containing at least:

- Stable definition ID and content revision.
- Display name, source image path/face, edition, and starting hand/deck/set-aside membership.
- Hero/content-set ID, typed `CardColor`, nullable tier, and explicit same-color upgrade/replacement relationships.
- Basic/non-basic/Ultimate/Handicap classification as applicable.
- Stable action-definition IDs; derived Primary/Secondary/reaction/replacement offer contexts; subtype; and text-application policy.
- Initiative, Attack, Defense, Movement, Range, and Radius values where present.
- Exact rules text transcription.
- Ordered mandatory/optional steps; choice owner; modal/repeat behavior.
- Structured triggers, conditions, targets versus presence checks, choices, values, effects, batch/simultaneous behavior, timing, duration, and cancellation.
- Stable effect-node IDs, capability ID/version, typed input/result bindings, operation-specific policies, outcome facts, and systemic checkpoints.
- Reused capability IDs and any controlled custom-handler ID.
- Item contribution and destination when set aside; Active-effect versus passive/continuous-source classification; Ultimate persistence requirements.
- Required source-linked scenarios and interaction/ordering tests.
- Source verification status and reviewer/date.
- Any official correction or clarification.

Current status: **Not inventoried**. Wasp inventory is deferred until milestone 8.1, after milestone 7.9 proves the deterministic synthetic core match; its capability matrix follows in 8.2. Other heroes remain outside the pitch inventory.

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
7. Retired for the current synthetic slice on 2026-08-16 by explicit user decision. Rulebook p.17's replacement doctrine and the published Brogan/Warlord wording establish that modified minion identity participates in Minion Battle, but no located official source defines removal consequences for a hero in that set. The approved interpretation is recorded rather than presented as sourced text: the hero counts, may be selected under the exact-count/Heavy-last removal contract, leaves the board by Remove rather than Defeat with no coins or Life change, and receives the ordinary p.7 optional Hero Respawn choice at its next action opportunity. A later official clarification supersedes this interpretation under the authority hierarchy. The unit-type question ledger in [Rules/RULE_SYSTEMS.md](Rules/RULE_SYSTEMS.md#unit-type-question-ledger) carries the implemented record.
8. Recorded at 6.8g: what is a defeated object that counts as a minion worth? p.13 prices melee/ranged at 2 coins, heavy at 4, and heroes by level; no clause prices a counts-as defeat, so defeat rewards keep reading intrinsic identity until settled.
9. Recorded at 6.4a: when one action displaces several of one team's minions outside the Battle Zone, in what order are they corrected? p.18 orders nothing within a team, and order matters because one correction can free or block another's shortest path. Implemented per the 2026-08-16 user decision as the owning team's choice - a subject-selection stage before each route or Place choice - until settled by source or official ruling.
10. Recorded at 6.4a: where does a displaced minion go when the Battle Zone has no empty space for the fallback Place? p.18 places it into the Battle Zone and no clause covers a full zone. Implemented as a typed left-in-place outcome - the minion stays where it ended up and the correction records that it could not act - until settled.

## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
