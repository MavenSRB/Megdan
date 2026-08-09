# Guards Digital - Project Context

Last updated: 2026-08-09

## Current status

Milestone 1.2 is complete: a Unity presentation assembly converts axial coordinates to pointy-top XZ positions, and a fixed-camera scene renders an origin hex plus its six neighbors as connected flat placeholder meshes. No gameplay has been implemented.

Milestone 1.3, placing one primitive hero from authoritative occupancy data, is proposed but not authorized. Every later milestone has the same approval gate.

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
- Automated testing: Unity Test Framework `1.7.0`, with independent Edit Mode and Play Mode assemblies. Focused commands are documented in `Docs/AI/TESTING.md`.
- Version control: Local Git repository on branch `main`, with root `.gitignore` and `.gitattributes` policies. Unity-generated data, local validation output, the private rulebook, and the extracted card corpus are ignored. Completed roadmap milestones require a local commit after acceptance and continuity updates. No remote is configured.
- Platform rollout direction: Windows first, then macOS/Linux, then touch devices, with closed platforms considered later.
- Use Unity's Input System and keep interactions adaptable across mouse and touch.
- Do not choose a networking provider or SDK during the pitch phase.

## Core architecture constraints

### Authoritative state

- Rules state is plain, deterministic game data, not Unity scene state.
- Logical hexes use integer axial `(q, r)` coordinates; generic adjacency is the six standard axial offsets exposed as named directions.
- The model uses collections and stable IDs for teams, players, heroes, cards, units, spaces, zones, and effects.
- The model must not assume exactly one hero per team even though the pitch configuration creates only one.
- Authoritative state must be serializable and schema-versioned.

### Commands and events

- The UI submits commands such as play card, choose action, select target, or confirm movement.
- A rules authority validates commands and produces ordered state-change events.
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

- The architecture must grow to support the future hero catalog.
- Add reusable effect primitives only when a real card requires them.
- Represent common card behavior as composable, typed data and small rule components.
- Keep a controlled custom-code extension point for genuinely unique effects.
- Do not build a runtime natural-language parser or a speculative universal scripting language.
- The full card corpus review is deferred. New cards are classified as they enter an approved milestone, and later cards may trigger deliberate refactoring.

## Presentation direction

- The Unity scene is 3D from the beginning, with the board on the XZ plane.
- Early implementation uses primitive meshes and a fixed camera.
- Pointy-top presentation maps `q` eastward and decreasing `r` northward; the same outer-radius value defines both the regular hex mesh and its center spacing.
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
- Mechanics are completed with primitives and debug UI before production art begins.
- Add focused automated tests for the changed behavior.
- Add a regression test for every fixed bug.
- Prefer tiny deterministic scenarios to complete match replays.
- Run the broad suite at milestone boundaries, not after every narrow change.
- Visually inspect only the presentation affected by the current milestone.
- After a roadmap milestone passes its acceptance checks and the living documents are updated, create a local Git commit for that completed milestone. Never push or configure a remote without separate authorization.

## Available reference material

- `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` - primary local rulebook.
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
- **Micro-milestone:** The smallest user-approved, independently testable increment with a hard stop after acceptance.
- **Rules ledger:** The traceability record connecting rule systems to sources, implementation, tests, and deviations.

## Current explicit non-goals

- Placing a hero or introducing authoritative occupancy before milestone 1.3 approval.
- Public distribution or commercialization.
- Online play during the pitch phase.
- AI opponents.
- Pass-and-play.
- Multiple playable heroes in the pitch.
- A universal card-text parser.
- Production visuals before the primitive game loop is complete.
- Music, voice, tutorials, accounts, matchmaking, cloud saves, or telemetry.
