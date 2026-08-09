# Guards Digital - Rules Ledger

Last updated: 2026-08-09

## Purpose

This ledger prevents silent rule drift. It connects each game system to its source, approved deviations, implementation status, and focused tests.

The project contains the completed Unity shell, automated test infrastructure, generic hex-coordinate foundation, placeholder coordinate-to-XZ presentation, one primitive hero occupancy/rendering slice, selectable adjacent-destination highlighting, an authoritative adjacent-move command/event slice, click-to-move presentation hookup, an expanded placeholder test grid, configurable primitive one-click movement sessions with recorded adjacent step routes, generic placeholder obstacle blocking, the pure placeholder card definition/instance foundation, timing-window-scoped placeholder Movement offers/typed printed values, and an authority-revalidated and snapshot-compatible one-click destination placeholder Movement action resolution shell, plus the architecture documents from completed roadmap milestones 0.1-0.5 and the core-first roadmap sequence from milestone 0.6. Milestones 1.1-1.12 and corrective milestones 1.12a-1.12b are complete. It contains no broader gameplay resolution. All rule systems are therefore unimplemented unless stated otherwise.

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
- **Source reviewed:** The named local source section has been inspected; this does not prove card-corpus completeness or that all applicable errata have been found.
- **Source verified:** Rule or card data has been checked against the authority hierarchy.
- **Architecture specified:** Digital boundaries needed to represent the rule are documented but not implemented.
- **Specified:** Exact digital behavior and edge cases are documented but not implemented.
- **Implemented:** Authoritative rule behavior exists.
- **Focused tests passing:** Narrow automated scenarios cover the implemented behavior.
- **Milestone verified:** The applicable focused, regression, source, presentation, and gate checks from `TESTING.md` pass for the approved milestone.
- **Blocked:** A missing source or decision prevents faithful implementation.

## Rule-system ledger

Printed rulebook page numbers are used below.

| System | Primary source | Current status | Required verification notes |
| --- | --- | --- | --- |
| Generic hex coordinates, adjacency, and placeholder XZ layout | Project architecture; not a game-rule interpretation | Implemented; milestone verified | Axial `(q, r)` coordinates expose six named standard neighbors; pointy-top placeholder conversion and mesh spacing are tested, but official board spaces and adjacency remain unverified and unimplemented until milestones 4.1-4.2 |
| Primitive hero occupancy and placeholder placement | Project architecture; not a game-rule interpretation | Implemented; focused tests passing | One placeholder hero has a stable hero ID and occupied axial coordinate; the scene view derives its position from that coordinate. This does not implement teams, legal setup, movement, targeting, turns, card effects, or official board spaces |
| Primitive hero selection and adjacent-empty highlighting | Project architecture; not a game-rule interpretation | Implemented; focused tests passing | A pure query derives empty adjacent destination candidates from board coordinates and current occupancies. The validation scene selects the primitive hero through Unity Input System pointer input and highlights adjacent empty hexes only. This does not implement a move command, occupancy mutation, movement events, teams, turns, or gameplay legality |
| Primitive adjacent-move command/event authority | Project architecture; not a game-rule interpretation | Implemented; focused tests passing | A pure rules authority validates stable hero ID, explicit origin, explicit destination, board membership, adjacency, and occupancy before committing a one-space occupancy update and emitting a semantic movement event. Structured rejections leave state unchanged and emit no accepted event. This does not implement pointer wiring, presentation movement, Movement action allowance, cards, turns, teams, networking, or animation |
| Primitive click-to-move presentation hookup | Project architecture; not a game-rule interpretation | Implemented; focused tests passing | Pointer input submits highlighted destination intents to the adjacent-move authority, and the hero view snaps only from updated authoritative occupancy after an accepted command. Rejected commands leave selection and presentation consistent with unchanged state. This does not implement multi-space movement, Movement action allowance, cards, turns, pathfinding, networking, or animation |
| Expanded placeholder test grid | Project architecture; not a game-rule interpretation | Implemented; milestone verified | The validation scene uses a radius-2 placeholder grid with 19 axial spaces. Focused scene tests verify distance-1, distance-2, and distance-3 pairs, an alternate two-step route pair, and coordinate-derived rendered centers while preserving primitive hero occupancy, adjacent highlighting, and click-to-move behavior. This does not implement official map data, multi-space movement, obstacles, zones, terrain, cards, turns, or gameplay legality |
| Primitive configurable movement session | Project architecture; not a game-rule interpretation | Implemented; milestone verified | A pure movement session tracks configurable allowance, remaining allowance, explicit route, and the ordered adjacent-move events accepted by the milestone 1.5 authority. The validation scene highlights all currently reachable empty destinations, accepts one clicked final destination, commits a deterministic shortest adjacent route internally through the same authority, and lets the tester stop without moving by clicking the selected hero again. This is not the full card Movement action and does not implement Fast Travel, movement from card text, movement-limiting effects, cards, turns, zones, terrain, networking, or animation |
| Generic placeholder obstacles | Rulebook pp.18-19 for empty space and one-obstacle occupancy; project architecture for temporary coordinates/art | Implemented; milestone verified | Two placeholder blockers occupy stable coordinates on the expanded validation grid. Pure obstacle occupancies are included in adjacent-move and reachability validation, cannot share coordinates with units or each other, block destinations and intermediate route spaces, and still allow legal detours when allowance permits. This is only the generic Phase 1 blocking slice; it does not implement obstacle subtypes, terrain, tokens, ignore-obstacle behavior, official map spaces, object Push, Place, Spawn, cards, turns, or obstacle entry/exit effects |
| Placeholder card definitions and instances | `CARD_SYSTEM_ARCHITECTURE.md`; rulebook pp.8-10 for general card-state pressure already source-reviewed in architecture | Implemented; milestone verified | Pure placeholder definitions have stable definition IDs, content revisions, display names, and immutable references. Runtime instances have distinct stable IDs, owner IDs, definition references, and orthogonal state facts initialized in hand. Initial setup validation rejects blank, duplicate, unresolved, stale, or invalid initial card data. This does not implement action definitions, runtime offers, printed values, real Wasp content, play/reveal, card-state transitions, turns, or card resolution |
| Placeholder action definitions, runtime offers, and typed printed values | `CARD_SYSTEM_ARCHITECTURE.md`; rulebook pp.7, 9, 11-14 source-reviewed in architecture for action/offers/value pressure | Implemented; corrective milestone verified | Pure placeholder definitions expose ordered read-only action/value data with stable local IDs, validated origins/policies, typed Movement values, and explicit default card-text/repeat/replacement policy. Repeat is an invocation context, not a static definition origin. Runtime offer queries derive only the origins applicable to the requested normal/general-rule/replacement/reaction/text-granted window for owned eligible instances and use explicit origin/context mapping. Printed value queries return read-only printed-icon contribution provenance and totals for the chosen offer. This does not implement broader action execution, card-text effects, real Wasp values, play/reveal, turns, or UI |
| One-click destination placeholder Movement action resolution shell | `CARD_SYSTEM_ARCHITECTURE.md`; rulebook pp.7, 10-11 source-reviewed in architecture for action choice/resolution pressure | Implemented; corrective milestone verified | Pure card-action resolution code re-derives a selected printed Primary/Secondary offer from current authoritative definitions and card instances before starting. Forged/stale/context-mismatched offers and missing actor occupancy reject without pending-state mutation. Pending data owns immutable reachable destination/route/cost candidates plus allowance, source/action/actor/context/current-step, and exact chooser decision facts. A destination answer re-derives the complete candidate set against current occupancy/obstacles, validates the selected route without side effects, commits every adjacent step through existing authorities, and completes the `up to` action; Stop completes at zero movement. A fresh authority validates the pending destination candidates against restored occupancy. The Phase 1 projection binds one deterministic shortest route to each destination and does not yet expose alternate/non-shortest route selection, concrete obstacle types/effects, or exact full-map Movement. This is snapshot-compatible continuation data, not snapshot import/export, and it does not implement visible hand UI, play/reveal, lifecycle transitions, Wasp text, modal choices, repeat execution, reactions, timing effects, turns, networking, presentation, or animation |
| Heroes, teams, zones, Thrones, minions, lanes, win overview | Rulebook p.3 | Not implemented | Phase 4 must model the complete official multi-zone single-lane map; reduced early grids cannot verify Fast Travel or Lane Push |
| Table talk and hidden information | Rulebook p.4 | Deferred by DEV-003 | Required for post-authorization online 3v3, not pitch interaction |
| Four-/six-player setup | Rulebook pp.5-6 | Source reviewed; not implemented | Pitch inherits four-player Quick Game values except DEV-001/002/003 |
| Minion setup and types | Rulebook p.6 and summary | Not implemented | Verify every spawn point and type against official board reference |
| Round structure | Rulebook p.7 | Source reviewed; not implemented | Milestone 3.8 proves exactly four turns per round with synthetic cards |
| Play/reveal cards | Rulebook p.7 | Source reviewed; not implemented | Milestone 3.3 proves the lifecycle with synthetic cards; director UI changes who selects, not card-state or reveal rules |
| Initiative and ties | Rulebook p.7 | Source reviewed; not implemented | Tie Breaker setup is milestone 2.6; milestone 3.4 proves Initiative and exact flip timing before later simultaneous-order systems |
| Perform one action / resolve card / turn end | Rulebook p.7 | Source reviewed; not implemented | Milestones 3.5-3.8 prove choice timing, unresolved/resolved order, turns, and the round shell before the complete Wasp set |
| Hero respawn | Rulebook p.7 | Not implemented | Include optional timing and Defense exception |
| Minion Battle | Rulebook p.8 | Source reviewed; not implemented | Difference removal, Heavy removed last, zero-effect tie |
| Remove tokens / return markers | Rulebook p.8 | Source reviewed; architecture specified; not implemented | Verify ordering relative to Active effects and Lane Push |
| Lane Push and respawn | Rulebook p.8 | Source reviewed; architecture specified; not implemented | Resolve Wave/final-Wave check, then zone/Throne check, then old-zone removal and new-zone respawn. Token-covered Spawn points remove the token; non-token obstacles use the owning team's nearest-empty choice and Tie-Breaker-side ordering. Mid-turn Push is immediate; end-of-round Push follows token/marker cleanup |
| Wave counters / Throne / Last Push | Rulebook pp.3, 8 | Source reviewed; not implemented | Test each victory path independently |
| Retrieve cards | Rulebook p.8 | Source reviewed; not implemented | Cancels applicable active effects through state changes |
| Level Up and Pity Coin ordering | Rulebook pp.8, 15-16 | Source reviewed in outline; not implemented | Leveling is mandatory before Pity Coin resolution |
| Card anatomy, classification, and static values | Rulebook pp.9, 15-16 plus verified Wasp cards | General source reviewed; architecture specified; Wasp not inventoried | Immutable revisioned definitions carry structured content and source text; images are presentation/audit references and verified Wasp values still require manual verification |
| Action availability and invocation context | Rulebook pp.7, 9, 11-14 | Placeholder Movement action definitions/offers implemented; broader architecture specified | Select card and action separately; normal-play queries derive printed Primary and printed Secondary Movement offers from static placeholder definitions and current in-hand ownership/state, while other origins require their explicit future timing window. Repeated actions preserve the original definition and use a repeated invocation context. General Hold, Fast Travel/Clear replacements, Defense reaction, repeat execution, text-granted actions, Basic/Defense/Skill context-sensitive classification, and broader execution remain later milestones |
| Ordered mandatory/optional execution and failure | Rulebook p.10 | Source reviewed; architecture specified; not implemented | Preserve written order; stop the remaining action at an impossible mandatory step; represent `may`, `up to`, and `if able` semantics explicitly |
| Decisions, modal actions, targets, and repeat | Rulebook p.10 | Source reviewed; architecture specified; not implemented | Serializable pending choices record chooser and continuation; repeat is a separate action instance, cannot recurse, and has replacement/active-effect restrictions |
| Card lifecycle and state changes | Rulebook pp.8, 10 | Source reviewed; architecture specified; not implemented | Location, played, unresolved/resolved, and face orientation are orthogonal; played/resolved and rare played/discarded combinations must work; resolved/discarded facedown suppression is distinct from newly played hidden/reveal flow; retrieve/swap and effect cancellation remain later milestones |
| Printed values and modifier contributions | Rulebook pp.9, 13-16 | Placeholder printed Movement value query implemented; broader architecture specified | Milestone 1.11 queries the selected runtime Movement offer and returns printed-icon contribution provenance plus total. Items, card text, minions, active effects, other value channels in real gameplay, and Wasp values remain later milestones |
| Movement action and direct Move | Rulebook p.11 plus cards | Generic placeholder Movement slice implemented; broader architecture specified | Phase 1 now proves typed Movement offers, a one-click destination candidate bound to an explicit deterministic adjacent route, zero/early stopping, ordered step events, and ordinary obstacle blocking. Alternate/non-shortest route selection, action-start restrictions, exact full-map Movement, and direct card-text Move remain unimplemented. Direct Move stays distinct and cannot become Fast Travel; wording such as `move a unit towards you` composes Move with a source-relative `Closer` destination constraint rather than a separate Pull capability |
| Fast Travel replacement | Rulebook p.11 | Source reviewed; architecture specified; not implemented | Blocked until Phase 4 verifies full-map zone membership/adjacency; replaces the entire Movement action, suppresses Primary card text, and needs no empty-space path |
| Skill | Rulebook p.12 plus verified Wasp cards | General source reviewed; Wasp requirements not inventoried | Add only the typed components required by verified Wasp actions |
| Hold and resolve without action | Rulebook pp.7, 12-13 | Source reviewed; architecture specified; not implemented | Hold is a general Secondary action; resolving without action after defeat/no respawn is a distinct state transition and timing path |
| Attack and Clear replacement | Rulebook p.13 | Source reviewed; architecture specified; not implemented | Separate non-Ranged/Ranged targeting and Before/After windows; Clear replaces the entire Attack action and suppresses Attack card text |
| Attacking minions | Rulebook pp.8, 13, 18 | Source reviewed; architecture specified; not implemented | No Defense reaction; rewards, Heavy immunity, immediate Lane Push only on Defeat/Remove, and after-action Battle-Zone correction require focused ordering tests |
| Attacking heroes, defeat, and systemic consequences | Rulebook pp.12-13 | Source reviewed; architecture specified; not implemented | Phase 2 reports only a basic success result; later stage defeat, reward, attempted/prevented removal, unresolved-card resolution, effect cancellation, markers, assists, Life, victory, and respawn so prevention does not erase defeat rewards |
| Defense reaction and detailed Attack order | Rulebook p.14 | Source reviewed; architecture specified; not implemented | Phase 2 proves a nested pending Defense choice, exactly-one-card discard/decline, and unmodified `Defense >= Attack`; later add text context, Block, items, minion modifiers, and full timing |
| Card upgrades, items, and Ultimate | Rulebook pp.15-16 plus Wasp cards | General source reviewed; architecture specified; Wasp not inventoried | Upgrades replace runtime card definitions/instances rather than mutating definitions; full Wasp relationships and values are required for pitch completion |
| Active effects, timing, cancellation, and continuous sources | Rulebook pp.7-10, 13-19 plus Wasp cards | General source reviewed; architecture specified; Wasp not inventoried | Serializable Active-effect instances carry source, trigger, scope, duration, order/choice, uniqueness, and cancellation; Ultimates/token properties use distinct continuous-source lifetime rules; no Tie Breaker flip for simultaneous Active-effect ordering |
| Targeting, presence checks, geometry, immunity, and object type | Rulebook pp.9-10, 13, 17-19 plus cards | General source reviewed; architecture specified; Wasp not inventoried | Typed queries distinguish target from presence, self from friendly, Range/Radius from movement path, straight line, zone, immunity, and `Counts As`; capabilities declare effective rules type versus intrinsic representation, including p.18's minion-miniature exception |
| Obstacles and occupancy | Rulebook pp.18-19 | Generic Phase 1 slice implemented; broader system not implemented | Phase 1 adds generic placeholder blocking only; never more than one obstacle in a space, including units. Ignore-obstacle behavior, concrete unit/terrain/token distinctions, terrain map data, token properties, and obstacle entry/exit effects remain later work |
| Object Push | Rulebook p.18 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Straight directly away from the pushing unit; blocked targets move as far as possible, including zero. An `up to X` multi-target Push uses one chosen distance and one simultaneous batch. Object Push is not Lane Push |
| Place | Rulebook p.18 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Moves a piece from board or supply to a different empty space and is not movement; source container, destination query, chooser, and failure policy are explicit |
| Spawn and Respawn | Rulebook pp.7-8, 19 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Spawn is supply-only into an empty source-defined destination. Hero and minion Respawn are specialized eligibility/timing contracts; spawn-point type/color applies only when the invoking rule requires it, while any unit spawning on a token-covered Spawn point removes that token first |
| Board-object Swap and Replace | Rulebook p.19 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Swap is one atomic position exchange and neither movement nor placement. Replace is one typed substitution and is distinct from action replacement and card upgrade/replacement |
| Remove, Defeat, prevention, and Clear | Rulebook pp.8, 12-14, 18-19 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Remove has no defeat reward; Defeat is staged through rewards and a preventable removal attempt; Clear replaces the entire Attack and removes eligible adjacent tokens. Only last-minion Remove/Defeat can immediately advance the Lane |
| Token instances, supply, properties, and interactions | Rulebook pp.8, 18-19 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Tokens are limited-supply obstacles, not units; stable instances carry persistent properties until removal. Cover reclamation choices, Move/Object Push/Swap/Replace/Remove, Clear, spawn-over-token, and end-round cleanup without tying lifetime to the placing hero's defeat |
| Marker attachments, properties, and return | Rulebook pp.8, 13, 18-19 plus cards | Source reviewed; architecture specified; Wasp not inventoried | Markers are non-board attachments, not tokens. Their modifiers follow attachment lifetime; all return at end of round and all held markers return when their hero is defeated unless verified overriding text applies |
| Minions outside Battle Zone | Rulebook p.18 | Source reviewed; architecture specified; not implemented | Applies only to intrinsic minion miniatures, not objects that merely count as minions; correct after the whole action by shortest empty path with team choice, or Place in a nearest empty Battle-Zone space when blocked. Tie-Breaker-side team resolves rare ordering cases first without a coin flip |
| Extended player counts | Rulebook p.20 | Reference only | Pitch does not claim official two-player setup because DEV-001 differs |
| Hero profile / selection guidance | Rulebook pp.21-22 | Reference only | Wasp selected for versatility and first-game suitability |

## Card architecture coverage

Milestones 0.4-0.5 specify these representation and capability seams; they do not implement the rules:

| Rule pressure | Required architecture seam | Planned proving milestone(s) |
| --- | --- | --- |
| UI must not own rule mutation | Intent commands and ordered rule events | 1.5 implemented a pure adjacent-move command/event slice |
| Static print data versus mutable physical card | Versioned definition plus stable owned instance | 1.10 implemented the pure placeholder definition/instance foundation |
| One card exposes context-dependent actions | Immutable action definitions plus runtime offers selected separately from card | 1.11 implemented placeholder printed Primary/Secondary Movement definitions and offers |
| Printed icons gain distinct modifiers | Typed value query with contribution provenance | 1.11 implemented placeholder printed Movement value contributions; extended in 5.3-5.4, 5.8, 7.2-7.3, and verified Wasp slices |
| Actions pause for route/target/option/other-player choices | Serializable pending resolution and pending choice | 1.12 implemented the first placeholder Movement pending-choice shell; 1.12b binds immutable destination/route/cost candidates to one destination command |
| Defense interrupts Attack | Reaction offer nested in the same resolution | 2.3-2.5 |
| Undo/networking must preserve partial resolution | Versioned snapshot of card state and pending choice | 3.9 |
| Played/resolved/discarded/facedown facts overlap | Orthogonal lifecycle axes and validated transitions | 3.1-3.8; verified Wasp data in 8.3 |
| Real Wasp cards introduce ordered effects and dependencies | Verified Wasp capability matrix before Wasp effect work | 8.1-8.2 |
| Timed and persistent text outlives one resolver step | Serializable active-effect instances and timing windows | 5.4, 6.8, and verified Wasp slices |
| Ultimate/token rules persist without ordinary Active-effect semantics | Distinct continuous-rule sources and effective-characteristic queries | 6.3, 7.3, and verified Wasp slices |

## Effect capability coverage

The capability catalog is source-reviewed architecture, not an implementation claim. Phases 1 through 7 prove verified general/core slices with synthetic definitions. After the deterministic core-match gate, milestone 8.2 maps verified Wasp content to the smallest Wasp-specific slices and records any capability deltas without retroactively gating the core rules.

| Capability family | Primary general source | Architecture status | Planned proving/integration milestones |
| --- | --- | --- | --- |
| Movement action and Fast Travel | Rulebook p.11 | Placeholder Movement definitions/offers/printed values and a one-click destination Movement resolution shell implemented; Fast Travel not implemented | 1.11 implemented placeholder Movement action definitions, offers, and printed value queries. 1.12 implemented the pure resumable Movement pending-choice shell; 1.12b projects immutable destination/route/cost candidates and commits one selected route. 1.13 proves visible card-driven movement; 5.1-5.2 full-map core integration; 8.5 Wasp integration |
| Direct Move, including source-relative destinations | Verified cards | Architecture specified; not implemented | Milestone 8.2 assigns the first verified Wasp slice if required; provisionally 8.6 |
| Place, Spawn, and Respawn | Rulebook pp.7-8, 18-19 plus verified cards | Architecture specified; not implemented | 5.7, 6.6, and 7.4 core/system integration; 8.6 Wasp integration if required |
| Object Push, Swap, and Replace | Rulebook pp.18-19 plus verified cards | Architecture specified; not implemented | Milestone 8.2 assigns the first verified Wasp slice if required; provisionally 8.6 |
| Remove, Defeat, prevention, and Clear | Rulebook pp.8, 12-14, 18-19 plus verified cards | Architecture specified; not implemented | Phase 2 foundation; 5.6, 5.8, 6.1, 6.3-6.6, and 7.4 consequences |
| Tokens, markers, obstacles, and persistent properties | Rulebook pp.8, 18-19 plus verified cards | Generic obstacle foundation implemented; broader capabilities architecture specified | 1.9 implemented generic placeholder obstacle blocking only. Token supply/properties, marker attachments, concrete terrain, and optional spatial interactions remain planned for 6.2-6.3, 6.7, and verified Wasp slices assigned by 8.2 |
| Card-state, ownership, value, and resource operations | Rulebook pp.7-10, 15-16 plus verified cards | Placeholder definition/instance foundation and printed Movement value query implemented; broader architecture specified; resources/transitions not implemented | 1.10 implemented stable placeholder definitions, instances, ownership, definition references, and initial in-hand state. 1.11 implemented placeholder offer-bound Movement value queries. 1.12-1.13 and Phase 3 extend this into resolution and lifecycle; 5.5, 6.8, and 7.1-7.6 cover core integration; 8.8 Wasp integration |
| Target/presence and spatial queries | Rulebook pp.9-10, 13, 17-19 plus verified cards | Architecture specified; not implemented | Phases 1-2 foundation; 4.3 and Phase 5 core integration; verified Phase 8 Wasp slices |
| Modifiers, restrictions, permissions, immunity, and effective characteristics | Rulebook pp.9, 11-19 plus verified cards | Architecture specified; not implemented | 1.11 foundation; 5.3-5.4, 5.8, and 7.2-7.3 core integration; 8.9 and 8.11-8.14 Wasp integration |
| Timing, control flow, Active effects, and continuous sources | Rulebook pp.7-10, 13-19 plus verified cards | Architecture specified; not implemented | 1.12 and 2.3-2.5 foundation; Phase 3, 5.4, 6.4, 6.8, and 7.8 core integration; 8.10 and 8.13 Wasp integration |
| Lane/minion/end-of-round/progression system transitions | Rulebook pp.7-8, 15-16 | Architecture specified; not implemented | Phases 6-7 |

## Wasp content ledger

The pitch requires Wasp's complete progression. Before verified Wasp definitions or effects are implemented, create an implementation-grade inventory containing at least:

- Stable definition ID and content revision.
- Display name, source image path/face, edition, and starting hand/deck/set-aside membership.
- Hero/content-set ID, color/slot, nullable tier, and upgrade/replacement relationships.
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
3. What source will be used to verify every official single-lane board coordinate, zone boundary, terrain space, and spawn point?
4. Do any mirrored-Wasp interactions require an explicit ruling because the physical setup normally uses unique heroes?
5. Does the unmodified four-player Quick Game economy create a pitch-breaking issue in 1v1? Do not change it unless later playtesting demonstrates a problem and the user approves a deviation.

## Change policy

- Never delete a deviation entry after it has affected implementation; mark it retired and record the replacement behavior.
- Add a dated note when an official ruling changes an existing interpretation.
- Every implemented rule must link to focused tests before its milestone can be marked verified.
- If code and this ledger disagree, treat the discrepancy as a bug until the user approves a documented rule change.
