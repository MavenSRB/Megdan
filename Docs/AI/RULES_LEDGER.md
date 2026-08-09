# Guards Digital - Rules Ledger

Last updated: 2026-08-09

## Purpose

This ledger prevents silent rule drift. It connects each game system to its source, approved deviations, implementation status, and focused tests.

The project contains the completed Unity shell, automated test infrastructure, generic hex-coordinate foundation, and placeholder coordinate-to-XZ presentation from milestones 0.2 through 1.2. It contains no game-rule implementation. All rule systems are therefore unimplemented unless stated otherwise.

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

## Prototype setup baseline

Use the standard four-player Quick Game setup wherever the approved 1v1 deviation does not make a rule impossible:

- Single-lane board.
- Standard official zones, terrain, Thrones, spawn points, and minion placement.
- Three Wave counters.
- Four Life counters per team.
- One Red Wasp and one Blue Wasp instead of two heroes per team.
- One director-mode controller operates both sides.
- Normal setup flips the Tie Breaker coin; internal tests may explicitly set its initial side.

Do not rebalance Life, Waves, minions, card values, economy, or Wasp merely because the player count is unofficial. Any later balance adjustment requires explicit approval and a new deviation entry.

## Approved deviations

| ID | Deviation | Reason | Impact | Removal or review trigger |
| --- | --- | --- | --- | --- |
| DEV-001 | One hero per team | Smallest complete pitch loop | Material balance and board-population change; removes team coordination | Review when expanding beyond the pitch configuration; replace with official 3v3 for the intended product |
| DEV-002 | Wasp appears on both opposing teams | Restricts pitch content to one fully implemented hero | Mirror match may produce interactions unavailable in normal unique-hero setup | Review when a second verified hero is approved |
| DEV-003 | One controller sees and controls both hands | Enables director-mode testing and demonstration without AI or networking | Removes hidden information, table talk, and independent player decisions | Replace with per-player hidden control in post-authorization online 3v3 |
| DEV-004 | Internal Undo, restart, snapshot tools, event log, animation speed, and hidden-state inspection | Reproducible testing and demonstrations | Allows actions unavailable in competitive play | Keep development-build only; never expose in real online matches |
| DEV-005 | Internal override for initial Tie Breaker side | Deterministic focused tests | Bypasses the setup coin flip only when explicitly selected | Normal match flow must still flip randomly; online authority owns setup result |

## Status vocabulary

- **Not inventoried:** Required source material has not been checked for completeness.
- **Source verified:** Rule or card data has been checked against the authority hierarchy.
- **Specified:** Digital behavior and edge cases are documented but not implemented.
- **Implemented:** Authoritative rule behavior exists.
- **Focused tests passing:** Narrow automated scenarios cover the implemented behavior.
- **Milestone verified:** Relevant broad and presentation checks pass at an approved milestone boundary.
- **Blocked:** A missing source or decision prevents faithful implementation.

## Rule-system ledger

Printed rulebook page numbers are used below.

| System | Primary source | Current status | Required verification notes |
| --- | --- | --- | --- |
| Generic hex coordinates, adjacency, and placeholder XZ layout | Project architecture; not a game-rule interpretation | Implemented; milestone verified | Axial `(q, r)` coordinates expose six named standard neighbors; pointy-top placeholder conversion and mesh spacing are tested, but official board spaces and adjacency remain unverified and unimplemented until milestones 3.1-3.2 |
| Heroes, teams, zones, Thrones, minions, lanes, win overview | Rulebook p.3 | Not implemented | Verify digital terms and object relationships before domain modeling |
| Table talk and hidden information | Rulebook p.4 | Deferred by DEV-003 | Required for post-authorization online 3v3, not pitch interaction |
| Four-/six-player setup | Rulebook pp.5-6 | Source reviewed; not implemented | Pitch inherits four-player Quick Game values except DEV-001/002/003 |
| Minion setup and types | Rulebook p.6 and summary | Not implemented | Verify every spawn point and type against official board reference |
| Round structure | Rulebook p.7 | Source reviewed; not implemented | Exactly four turns per round |
| Play/reveal cards | Rulebook p.7 | Source reviewed; not implemented | Director UI changes who selects, not card-state or reveal rules |
| Initiative and ties | Rulebook p.7 | Source reviewed; not implemented | Tie Breaker flip timing requires focused scenarios |
| Perform one action / resolve card / turn end | Rulebook p.7 | Source reviewed; not implemented | Preserve choice timing and unresolved/resolved order |
| Hero respawn | Rulebook p.7 | Not implemented | Include optional timing and Defense exception |
| Minion Battle | Rulebook p.8 | Source reviewed; not implemented | Difference removal, Heavy removed last, zero-effect tie |
| Remove tokens / return markers | Rulebook p.8 | Source reviewed; not implemented | Verify ordering relative to active effects and Push |
| Lane Push and respawn | Rulebook p.8 | Source reviewed; not implemented | Include immediate mid-turn Push and end-of-round Push |
| Wave counters / Throne / Last Push | Rulebook pp.3, 8 | Source reviewed; not implemented | Test each victory path independently |
| Retrieve cards | Rulebook p.8 | Source reviewed; not implemented | Cancels applicable active effects through state changes |
| Level Up and Pity Coin ordering | Rulebook pp.8, 15-16 | Source reviewed in outline; not implemented | Leveling is mandatory before Pity Coin resolution |
| Action-card anatomy and values | Rulebook p.9 plus verified Wasp cards | Not inventoried | Images are presentation-only; structured data requires manual verification |
| Action execution, choices, targets, repeat | Rulebook p.10 | Not implemented | Stop-on-failure and mandatory/optional wording need focused cases |
| Card states and state changes | Rulebook p.10 | Not implemented | Hand, played, unresolved, resolved, discarded, facedown, retrieve, swap |
| Movement and movement text | Rulebook p.11 | Not implemented | Separate primary/secondary Movement and movement inside card text |
| Fast Travel | Rulebook p.11 | Not implemented | Replacement action; card text does not apply |
| Skill | Rulebook p.12 plus verified Wasp cards | Not implemented | Add only real, typed Wasp requirements initially |
| Hold | Rulebook p.12 | Not implemented | Any card may Hold; perform no action |
| Attack and Clear | Rulebook p.13 | Not implemented | Separate replacement Clear from Attack text |
| Attacking minions | Rulebook p.13 | Not implemented | Rewards and Heavy immunity require minion data |
| Attacking heroes and defeat | Rulebook p.13 | Not implemented | Coins, assists, Life spending, effect cancellation, markers |
| Defense and detailed attack order | Rulebook p.14 | Not implemented | Primary/secondary Defense, discard choice, Block, minion modifiers |
| Card upgrades, items, Ultimate | Rulebook pp.15-16 plus Wasp cards | Not inventoried | Full Wasp progression is required for pitch completion |
| Active effects and timing | Rulebook pp.17-19 plus Wasp cards | Not inventoried | Build typed triggers/durations only from verified requirements |
| Obstacles and occupancy | Rulebook pp.18-19 | Not implemented | Never more than one obstacle in a space |
| Push, swap, place, move through | Rulebook pp.18-19 plus cards | Not inventoried | Exact Wasp needs determine initial primitives |
| Minions outside Battle Zone | Rulebook p.18 | Source reviewed; not implemented | Return after the action by shortest empty path; tie order does not flip coin |
| Extended player counts | Rulebook p.20 | Reference only | Pitch does not claim official two-player setup because DEV-001 differs |
| Hero profile / selection guidance | Rulebook pp.21-22 | Reference only | Wasp selected for versatility and first-game suitability |

## Wasp content ledger

The pitch requires Wasp's complete progression. Before card implementation, create an implementation-grade inventory containing at least:

- Stable card ID.
- Display name and source image path.
- Color/slot and tier.
- Basic/non-basic/Ultimate/Handicap classification as applicable.
- Primary and secondary action types.
- Initiative, Attack, Defense, Movement, Range, and Radius values where present.
- Exact rules text transcription.
- Structured triggers, conditions, targets, choices, and effects.
- Item contribution when set aside.
- Source verification status and reviewer/date.
- Any official correction or clarification.

Current status: **Not inventoried**. Corpus-wide analysis is intentionally deferred until its roadmap milestone.

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
3. What source will be used to verify every official single-lane board coordinate, zone boundary, terrain space, and spawn point?
4. Do any mirrored-Wasp interactions require an explicit ruling because the physical setup normally uses unique heroes?
5. Does the unmodified four-player Quick Game economy create a pitch-breaking issue in 1v1? Do not change it unless later playtesting demonstrates a problem and the user approves a deviation.

## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
