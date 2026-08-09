# Guards Digital - Project Context

Last updated: 2026-08-09

## Current status

Milestone 1.12 is complete: pure rules code can start one selected placeholder Movement offer in its explicit printed Primary or printed Secondary context, record a serializable pending resolution and movement choice, accept or reject follow-up step/stop commands, and resume through the existing movement session and adjacent-move authority until the action completes. The shell preserves source-card instance, action-offer, actor, invocation-context, current-step, chooser, and legal-decision data without Unity callbacks, delegates, coroutines, or UI-owned continuation state. No visible card hand, card play/reveal, card lifecycle transitions, real Wasp data/images, modal/repeat behavior, reactions, timing effects, turns, Unity presentation, animation, or broader gameplay resolution has been implemented.

Documentation milestone 0.4 is also complete: the general card/action rules on printed rulebook pages 7-19 have been reviewed and converted into the durable implementation contract in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`. This specifies future card code but implements none of it.

Documentation milestone 0.5 is complete: `Docs/AI/CARD_EFFECT_CAPABILITIES.md` now defines effect-level contracts for the rulebook's mutation operations, queries, modifiers, control flow, timing, persistent sources, and system transitions, including explicit Object Push, source-relative direct Move, Place, Spawn, token, marker, Swap, Replace, Remove, Defeat, and keyword-query architecture. This also implements no gameplay.

Documentation milestone 0.6 is complete: the roadmap now proves Tie Breaker state, Initiative, turns, rounds, and the complete general match loop with synthetic test cards before the complete Wasp card inventory or Wasp-specific card implementation begins. This sequencing change also implements no gameplay.

Documentation milestone 0.7 is complete: `Docs/AI/TESTING.md` now defines risk-tiered validation. Every executable milestone keeps focused coverage, while Play Mode, affected/full suites, visual checks, source validation, and build smoke tests run only where the changed boundary or risk justifies them. This policy change also implements no gameplay.

Milestone 1.13, selecting a test card and performing visible card-driven movement, is proposed but not authorized. Every later milestone has the same approval gate.

## Mission

Create a polished private digital prototype of *Guards of Atlantis II* that can be demonstrated to its publisher as the basis for a possible licensed, cross-platform adaptation.

The prototype is a narrow 1v1 director-mode implementation used to prove the game loop and presentation. It is not the intended final player count. The eventual primary mode is online 3v3 with six human players, one hero per player.

## Rights and distribution boundary

- The user does not currently have authorization to publicly distribute an adaptation.
- The prototype is private, noncommercial, and intended for a direct publisher pitch.
- "Guards Digital" is a working title. Final naming is gated on authorization and publisher agreement.
- Do not create a public download, public repository, marketing campaign, monetization, telemetry, or store listing before authorization.
- The initial pitch deliverable is a private Windows standalone build plus a short recorded walkthrough as backup.
- Source code and builds for other platforms remain internal unless specifically requested.

This is a project boundary, not a legal determination.

## Product horizons

### Horizon 1 - Private pitch prototype

A complete but intentionally simplified 1v1 match:

- One Red Wasp and one Blue Wasp.
- One human controls both sides in director mode.
- Both hands are visible to the controller.
- The controller selects one card for each side, then resolves the turn.
- Full official single-lane board structure and standard minion setup.
- Four-player Quick Game setup values: three Wave counters and four Life counters per team.
- Full Wasp progression, including relevant card tiers, items, and Ultimate access.
- Complete applicable game loop: cards, initiative, actions, combat, defense, minions, rounds, lane pushing, economy, leveling, respawn, and victory conditions.
- No opponent AI, pass-and-play, networking, accounts, matchmaking, progression meta, or public services.

### Horizon 2 - Licensed product direction

Post-authorization only:

- Online 3v3 as the primary mode.
- Six humans, each controlling one hero.
- Cross-platform release, developed and validated in phases.
- Larger hero roster, eventually expected to approach roughly 40 heroes.
- Hidden information, proper per-player control, team interactions, reconnect behavior, matchmaking, accounts, and platform services are separate future work.
- No AI-player requirement is currently planned.

## Approved prototype deviations

Everything not listed here must behave exactly like the authoritative physical game sources.

1. One hero per team instead of the supported four- or six-player configurations.
2. Wasp is mirrored across opposing teams.
3. One controller sees and operates both teams in director mode.
4. Internal debug tools may expose hidden state, undo commands, restart matches, import/export snapshots, change animation speed, and override the initial Tie Breaker side.

These deviations are detailed and tracked in `RULES_LEDGER.md`.

## Rules fidelity

- Card images are presentation assets only.
- All rules-relevant card values and effects must be transcribed into structured data and verified against the source images.
- Never parse card pixels or printed text at runtime.
- Do not add convenience rules, balance changes, or digital-only behavior without explicit approval.
- When a source is ambiguous, follow the authority hierarchy in `RULES_LEDGER.md` and record any temporary interpretation.
- The game has no randomness after setup. The only random setup result is the starting side of the Tie Breaker coin.
- Normal play randomizes the initial Tie Breaker side. Internal tests may explicitly choose Red or Blue.

## Technology and platforms

- Engine: Unity.
- Installed and selected editor: Unity `6000.5.7f1`.
- Reference development platform: Windows desktop and Unity Editor.
- Active reference build target: Windows 64-bit standalone (`Win64`).
- Input: Unity Input System package `1.19.0`, with **Input System Package (New)** as the only active input handler. The legacy Input Manager is not selected for gameplay input.
- Automated testing: Unity Test Framework `1.7.0`, with independent Edit Mode and Play Mode assemblies. Validation tiers, focused commands, and full-suite gates are documented in `Docs/AI/TESTING.md`.
- Version control: Local Git repository on branch `main`, with root `.gitignore` and `.gitattributes` policies. Unity-generated data, local validation output, the private rulebook, and the extracted card corpus are ignored. Completed roadmap milestones require a local commit after acceptance and continuity updates. No remote is configured.
- Platform rollout direction: Windows first, then macOS/Linux, then touch devices, with closed platforms considered later.
- Use Unity's Input System and keep interactions adaptable across mouse and touch.
- Do not choose a networking provider or SDK during the pitch phase.

## Core architecture constraints

### Map progression

- Phases 1 through 3 use an expanded temporary test grid only for foundational movement, obstacles, cards, two-hero combat, Initiative, turns, and rounds.
- Phase 4 replaces that grid in the main validation scene with the complete official multi-zone single-lane map.
- The full map model includes every official space, terrain classification, zone, zone boundary, zone adjacency, spawn point, Throne, starting Battle Zone, and ordered lane relationship.
- Fast Travel cannot begin until same-zone, adjacent-zone, and non-adjacent-zone queries are verified on the full map.
- Lane pushing cannot begin until the full ordered lane, movable Battle Zone, Thrones, and per-zone minion spawn points are verified.
- Do not use a reduced or invented map as evidence that Fast Travel or lane pushing is correct.

### Authoritative state

- Rules state is plain, deterministic game data, not Unity scene state.
- Logical hexes use integer axial `(q, r)` coordinates; generic adjacency is the six standard axial offsets exposed as named directions.
- The model uses collections and stable IDs for teams, players, heroes, cards, units, spaces, zones, and effects.
- The model must not assume exactly one hero per team even though the pitch configuration creates only one.
- Authoritative state must be serializable and schema-versioned.

### Commands and events

- The UI submits commands such as play card, choose action, select target, or confirm movement.
- A rules authority validates commands and produces ordered state-change events.
- Multi-step actions pause on authoritative, data-shaped pending choices and resume through follow-up commands; UI callbacks never own the continuation.
- The pitch authority runs locally in-process.
- A future online build can move authority to a host/server without rewriting card rules.
- Randomness must not be generalized beyond the initial Tie Breaker setup decision.

### Undo and diagnostics

- Director-mode Undo restores the authoritative snapshot before the last committed command.
- Undo also cancels and rebuilds presentation from the restored state.
- Full-match undo history is acceptable for the small internal prototype.
- Undo, hidden-state inspection, and Tie Breaker override are internal tools and unavailable in competitive online matches.
- A player-facing save system is not part of the pitch.
- Planned internal tools include restart, snapshot import/export, event log, and animation-speed control.

### Card system

- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` is the required card/resolver contract. `Docs/AI/CARD_EFFECT_CAPABILITIES.md` is its mandatory effect/keyword companion for all mutation, query, modifier, token, marker, timing, and system-transition work.
- The architecture must support the future hero catalog without claiming that unknown cards require zero extensions.
- Immutable, revisioned `CardDefinition`-equivalent content is separate from owned runtime `CardInstance`-equivalent state. Instances refer to definitions; definitions are never mutated into match state or upgrades.
- Runtime card lifecycle is represented through orthogonal facts such as location, played status, resolution status, and face orientation because the rulebook permits overlapping states.
- A card and an action are not the same selection. Immutable action definitions hold structured printed/general rules content; runtime action offers bind a definition to the current actor, source card, state, and invocation context. Offers explicitly distinguish printed Primary, printed Secondary, general-rule Secondary, replacement, reaction, repeated, and card-text-granted use, including whether card text applies.
- Printed icon values are queried through typed value/contribution services so later items, card text, minions, and active effects can modify totals without replacing early card models.
- Ordered action execution is deterministic and resumable. Mandatory/optional steps, target and modal choices, repeats, nested Defense, timing windows, and active effects use serializable rules data rather than delegates, coroutines, or UI-owned state.
- Targeting, presence checks, value calculation, card-state transitions, timing, and active-effect lifetime are reusable rule services rather than card-specific UI logic.
- Passive/continuous rules sources, including Ultimates and persistent token properties, remain distinct from ordinary cancelable Active effects.
- Common effects compose small typed definitions/handlers. A genuinely unique verified effect may register one isolated pure-code handler, but it must use the same validation, pending-choice, event, timing, state, and serialization boundaries.
- Each reusable effect/query has a stable versioned capability ID and typed contract covering source/actor/chooser/target roles, optionality, atomicity, state changes, checkpoints, events, continuation data, validation, and tests.
- Every effect node has its own stable node ID and typed input/output bindings. Serializable outcomes record actual targets, path/distance, stopping reason, defeat/removal/prevention, resource changes, and other facts so later text can branch on real results without callbacks or a universal expression language.
- Object Push, direct Move, Place, Spawn, Respawn, Swap, Replace, Remove, and Defeat remain distinct public capabilities even when they share spatial helpers. Tokens and markers have distinct instance, supply, occupancy/attachment, and lifetime rules.
- Card text such as `move a unit towards you` uses direct Move plus source-relative `Closer` destination constraints, not a separate Pull capability.
- Rules objects preserve both intrinsic representation kind and derived effective type. Each capability declares which it queries, including the rulebook exception that after-action Battle-Zone correction applies to minion miniatures rather than objects that merely count as minions.
- System consequences use typed operation facts/checkpoints: only Defeat/Remove can immediately advance the Lane, while Move/Object Push/Swap cannot; displaced-minion correction runs after the whole action.
- Never branch rules on card display name, hero display name, image filename, or printed text. Do not build a runtime natural-language parser or speculative universal scripting language.
- Phase 1 proves this foundation with placeholder definitions/instances, explicit Movement offers and typed values, command/events, a resumable choice shell, and visible card-driven movement.
- Phase 2 proves extension by opening a basic Attack, pausing for the target hero's Defense reaction, discarding one owned card or declining, and resuming the unmodified comparison through the same authority.
- Phase 3 proves generic card lifecycle, director hands, play/reveal, Initiative and tied Initiative, Hold, Movement and Attack inside turns, the four-turn round skeleton, snapshots, Undo/restart, and event logging with synthetic definitions.
- Phases 4 through 7 prove the official map, core full-map actions and combat, minions, tokens, Lane Push, exact end-of-round order, defeat/respawn, progression, victory, and a deterministic complete match with synthetic definitions.
- The complete Wasp card inventory and Wasp-specific card implementation wait until Phase 8, after milestone 7.9 proves the synthetic core match. Milestone 8.2 may re-baseline Wasp-specific work and add verified capability deltas, but it does not gate or redesign the already-proven general core loop.
- Complete Wasp verification remains a later integration gate; an earlier action-category milestone cannot claim the whole hero is complete.
- Add reusable primitives only when a verified rule or card requires them. Later discoveries may require a localized capability, handler, or explicit schema migration, but must not silently rewrite ownership, action context, UI authority, or continuation semantics.

## Presentation direction

- The Unity scene is 3D from the beginning, with the board on the XZ plane.
- Early implementation uses primitive meshes and a fixed camera.
- The minimal-grid validation scene centers its fixed camera directly above the map and looks straight down.
- Pointy-top presentation maps `q` eastward and decreasing `r` northward; the same outer-radius value defines both the regular hex mesh and its center spacing.
- Placeholder hexes use one light-gray fill with a darker gray perimeter for separation; this styling is non-semantic and the logical origin has no unique treatment.
- The polished pitch uses a stylized tabletop presentation.
- Heroes and minions appear as 3D miniatures on clearly marked team bases.
- Miniatures may briefly animate when acting.
- When the camera approaches top-down, units automatically crossfade to readable 2D tokens.
- Separate enter and exit thresholds prevent visual flicker near the switching angle.
- One logical unit owns interchangeable 3D-miniature and 2D-token representations.

### Camera

- Board-focused rather than free flight.
- Bounded pan, orbit, and zoom.
- Supports angled tabletop and top-down views.
- Includes a whole-board reset/focus control.
- Cannot travel beneath or far outside the board.

### Animation and audio

- Rules resolve independently of animations.
- The presentation layer consumes ordered rule events and animates them.
- Skipping or accelerating animation never changes game state.
- Target Wasp animation set: idle, movement, attack, hit/defeat, and respawn.
- Minions use a smaller shared animation set.
- The pitch receives a small sound-effects pass for UI confirmation, card reveal, movement, attack, hit/defeat, minion battle, and lane push.
- Music, voice acting, announcer systems, and elaborate adaptive audio are out of scope.

## Pitch HUD

- The board is the primary focus.
- Red and Blue hands are simultaneously visible and clearly separated.
- Show round, turn, active resolver, Lives, Waves, hero levels, and coins.
- Use contextual prompts for actions, targets, and choices.
- Provide internal Undo, restart, animation-speed, and camera-reset controls.
- Provide a collapsible event log.
- No main menu, settings suite, tutorial, collection screen, or account UI is required for the pitch.

## Accessibility baseline

- Team identity never relies on color alone.
- Red and Blue also use distinct symbols, base markings, outlines, and labels.
- Cards and state values must remain readable at common desktop resolutions.
- UI structure must allow later scaling.
- A full accessibility settings suite is deferred.

## Development workflow

- Implement the smallest independently testable behavior.
- One approved micro-milestone per task.
- Every milestone defines one observable outcome, explicit non-goals, focused acceptance checks, and a hard stop.
- Do not start the next milestone without explicit user approval.
- The early board/card foundation proceeds in separate steps: adjacent selection, an authoritative adjacent-move command/event, presentation hookup, a larger placeholder grid, movement of multiple spaces, obstacle blocking immediately afterward, definition/instance separation, action definitions/runtime offers and typed values, resumable card-action choices, then visible card-driven movement.
- Multi-space movement currently treats the player's clicked reachable destination as the explicit choice, records the deterministic internal adjacent route used to reach it, allows zero movement by stopping before a destination click, and rejects entering or passing through generic placeholder obstacles.
- Before Phase 1 ends, the selected test action offer's typed printed-value query must be the sole source of movement allowance.
- Fast Travel, movement from card text, and movement-limiting effects remain later rule milestones rather than being folded into the early board test.
- Mechanics are completed with primitives and debug UI before production art begins.
- Select the highest applicable validation tier from `Docs/AI/TESTING.md` based on the behavior and boundaries changed, not merely diff size.
- Every executable behavior change receives focused automated coverage and directly affected regressions; every fixed bug receives a regression test.
- Prefer tiny deterministic scenarios to complete match replays.
- Run Play Mode and visual checks only for affected Unity-facing or visible paths.
- Run complete unfiltered suites only at implementation-phase endpoints, explicit hard gates, broad-risk infrastructure/schema changes, or when affected consumers cannot be confidently enumerated.
- Documentation/source-only work uses diff, reference, traceability, and consistency validation without launching Unity.
- Visually inspect only the presentation affected by the current milestone.
- Record the validation tier, exact checks/results, visual or source review, and any intentionally omitted unaffected suite in the completion note.
- After a roadmap milestone passes its acceptance checks and the living documents are updated, create a local Git commit for that completed milestone. Never push or configure a remote without separate authorization.

## Available reference material

- `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` - primary local rulebook.
- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` - project architecture contract derived from the general card/action rules; it is not a substitute for card-specific source verification.
- `Docs/AI/CARD_EFFECT_CAPABILITIES.md` - effect and keyword capability catalog; it is not evidence that a capability or card is implemented.
- `Docs/Guards of Atlantis II - Tabletopia Cards/` - private reference extraction containing hero-card material and supporting scripts/assets.
- The extracted card corpus has not been accepted as complete or fully verified for implementation.
- Do not assume filenames, duplicates, image faces, or extraction completeness are authoritative without an explicit inventory milestone.
- Do not distribute these references.

## Shared terminology

- **Director mode:** One human can see and control both teams for testing and demonstration. This is not opponent AI and not the final multiplayer interaction model.
- **Pitch prototype:** The private, polished 1v1 Wasp-versus-Wasp build described above.
- **Authoritative state:** The validated rules state from which all visuals are derived.
- **Command:** A requested player or debug action sent to the rules authority.
- **Rule event:** An ordered, validated state change produced by the rules authority and consumed by presentation.
- **Card definition:** Immutable, versioned structured rules content for a printed card face; never live match state.
- **Card instance:** One stable owned runtime card that references a definition and carries its current lifecycle facts.
- **Action definition:** Immutable structured content for a printed or general-rule action before current match context is applied.
- **Action offer:** One context-specific legal use of a card or general rule, selected separately from the card itself.
- **Effect capability:** One versioned typed mutation, query, modifier, control-flow, timing, persistent-source, or system-transition contract referenced by structured card rules.
- **Pending choice:** Serializable authoritative input required to continue a paused action resolution.
- **Micro-milestone:** The smallest user-approved, independently testable increment with a hard stop after acceptance.
- **Rules ledger:** The traceability record connecting rule systems to sources, implementation, tests, and deviations.

## Current explicit non-goals

- Card execution, pending choices, visible card UI, turns, or Wasp content before their exact milestone approval.
- Public distribution or commercialization.
- Online play during the pitch phase.
- AI opponents.
- Pass-and-play.
- Multiple distinct hero designs beyond mirrored Wasp in the pitch.
- A universal card-text parser.
- Production visuals before the primitive game loop is complete.
- Music, voice, tutorials, accounts, matchmaking, cloud saves, or telemetry.
