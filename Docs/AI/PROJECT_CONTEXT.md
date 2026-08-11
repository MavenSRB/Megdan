# Guards Digital - Project Context

Last updated: 2026-08-11

## Current status

Milestones 3.8 through 3.11 and milestones 4.1-4.2a are complete: a pure authoritative round state composes exactly four indexed commitment/reveal and Initiative/action turns, and the saved validation controller retains that authority through repeated rounds. Commitment is keyed by canonical `HeroId`, and each turn records an immutable deterministic collection of hero/card-or-empty-hand-Pass assignments while player/team derive through the participant graph. After turn 4, one resumable cursor advances through the stable end-of-round checkpoints, retrieves eligible synthetic cards, freezes completed history, resets round-local state, and carries match-wide Tie Breaker state. A deterministic schema-versioned snapshot serializes/restores that participant, round, cursor, selected-map reference, board occupancy, card, definition-reference, Tie Breaker/Initiative, and pending-resolution state. Existing resolver validators re-derive pending Movement and Attack/Defense choices on load; schema, card-definition, missing-map, and map-revision failures reject distinctly. Director Undo records the exact pre-command snapshot only after an authoritative command is accepted; it pops and restores that boundary without adding history, while restart restores the initial scenario and clears history. A versioned controller envelope preserves the immutable read-only rule-event projection beside the authoritative payload, so restore, Undo, and restart rebuild the same ordered diagnostic log without allowing it to mutate or continue rules resolution. The validation HUD provides compact `UNDO`, `RESTART`, and collapsible `EVENT LOG` controls; each log row shows sequence, semantic type, stable references, and diagnostic values while Facedown identity remains hidden until reveal. The Phase 3 gate passed both complete Unity suites. The reviewed `official-single-lane` revision 1 specification inventories all 292 map cells, seven zones, standard axial connections, terrain, Thrones, the starting Battle Zone, and 40 symmetric spawn points. Reusable immutable content/catalog models load that selected map into authoritative board state, whose internal coordinate queries remain selected-map-local and whose snapshots carry only stable map identity/revision. The saved validation scene rebuilds its complete placeholder geometry from that selected definition: all seven zones and terrain are distinguishable, all eight team/type spawn markers are inspectable, the two synthetic hero views resolve stable team Hero-spawn references through the selected definition, and the fixed top-down camera frames the complete board. Presentation owns neither topology nor map selection. Later Phase 6-7 milestones populate the same checkpoint identities, with 7.8 remaining the complete exact-sequence integration gate. Zone adjacency, end-of-round subsystems, Active effects, and victory remain unimplemented; milestone 4.3 is next and unauthorized.

Milestone 3.7b is complete: each current synthetic hero now owns five distinct test-only cards matching the approved value/action matrix. Every definition has exactly one printed Primary Attack, one printed Secondary Movement, and one in-hand Defense reaction, while Hold remains general-rule derived. The ten definition and instance identities are globally unique, retain the Initiative 11 cross-team tie, and flow through commitment, reveal, Initiative, Movement, Attack/Defense, lifecycle, ownership, and director presentation without fixed two-card assumptions. The HUD gives each five-card hand a dedicated bounded row and keeps all five legal Defense choices usable. Official hero content, card-text effects, consequences, and snapshots remain unimplemented.

Milestone 3.7 is complete: the selected face-up unresolved turn card can perform its existing synthetic printed hero Attack through the same turn frame. The turn authority delegates target, typed Attack/Defense values, defender-owned one-card discard or decline, comparison, and semantic events to the Phase 2 resolver; the nested Defense reaction never replaces the attacking turn actor or consumes the defender's normal played-card action. Only completed Attack resolution changes the attacker's selected card to resolved, after which any deferred Tie Breaker flip is consumed and Initiative is re-queried from current state. The validation controller exposes both Movement and Attack offers for the current actor/card, visibly retains the attacker through the Defense prompt, reports the typed outcome, and focuses the next unresolved actor without owning card or combat state. Attack consequences, defeat/removal, snapshots, and broader content remain unimplemented.

Milestone 3.6 is complete: the pure turn authority now composes the existing Phase 1 Movement resolver for the currently selected face-up unresolved synthetic card. An explicit played-turn-card query derives printed Movement offers without changing the legacy in-hand offer path. The card-action authority retains ownership of allowance, disclosed deterministic route candidates, cumulative waypoints, obstacles, adjacent-step occupancy commits, pending choice state, and semantic events; the turn authority only opens/resumes that resolution and never mutates occupancy. Partial movement keeps the same normal action pending, while Stop or allowance exhaustion completes Movement, resolves exactly its source card, consumes any deferred Tie Breaker flip, and re-queries Initiative. Milestone 3.7 subsequently added Attack/Defense turn integration; resolve-without-action remains unimplemented.

Milestone 3.3 is complete: a pure authority accepts exactly one eligible own-hand card per Red/Blue side, commits it as played/unresolved/Facedown, represents a legal Pass only when that side has no eligible card, and reveals all commitments together only after both sides respond. Pre-reveal events and HUD labels disclose only that a side committed, not the card ID; reveal events and definition-backed HUD labels disclose the stable card, name, and Initiative only after authoritative Faceup transitions. Rejected cross-side, stale, repeated, missing, ineligible, and illegal-pass inputs do not mutate further state.

Corrective milestone 2.6a is complete: the validation HUD now shows the current authoritative Tie Breaker side in the top-right through a solid side-colored panel with explicit `TIE BREAKER COIN` and `[R] RED` or `[B] BLUE` text. Presentation reads the immutable `TieBreakerState` from the complete placeholder match configuration, never chooses or mutates it, and blocks pointer input from passing through the indicator. The Phase 2 corrective T3 gate passed both complete Unity suites. Milestone 3.2 also projects both complete synthetic test hands in separate explicit `[R] RED HERO` and `[B] BLUE HERO` groups.

Milestone 2.6 is complete: normal setup performs one localized Tie Breaker coin flip and stores the revealed Red or Blue side in immutable authoritative state carried by the synthetic match configuration. Internal deterministic tests can explicitly select Red or Blue; invalid undefined selection rejects, and tests unrelated to setup-side selection pin the side explicitly. No general gameplay RNG service, Initiative ordering, or Tie Breaker flip timing was added. The Phase 2 T3 gate passed both complete Unity suites. The Phase 3 entry audit kept milestones 3.1-3.11 unchanged and dependency ordered through one deterministic round, snapshots, Undo/restart, and the event log.

Milestone 2.5 is complete: after the targeted hero discards exactly one revalidated own-hand Defense card or declines, the same Attack advances through its restorable comparison checkpoint and revalidates its offer, target, Attack Total, and optional completed Defense facts. Defense equal to or above Attack reports successfully defended; lower Defense, decline, or no legal Defense reports attack successful. Ordered typed outcome/completion events preserve the stable resolution, actor, target, totals, and Defense source, then the Attack closes. Stale comparison state rejects without closing the pending Attack. The visible Phase 2 panel reports the exact result and releases actor/card selection without applying defeat, removal, Life, coins, respawn, or any other deferred consequence.

Milestone 2.3 is complete: the shared card-action authority can initiate the synthetic printed Primary non-Ranged Attack, project only adjacent enemy hero targets from participant relationships and authoritative occupancies, accept a stable target-choice command, calculate the initial Attack Total through typed printed-value contributions, emit ordered target-request/Attack-start/Attack-total/Defense-request events, and pause in a restorable defender-owned Defense response choice. Self, friendly, non-adjacent, unknown, empty-space, terrain/token, and non-unit-obstacle identities are never legal hero targets; rejected or stale commands preserve the same pending state and all card states. Units continue to occupy/block spaces through occupancy authority while remaining eligible typed targets. The visible Phase 2 test hand now exposes Route Drill's `Primary Attack 3`; selecting it highlights exactly the authority-projected legal enemy hero coordinates, and clicking the adjacent enemy miniature submits the target through the existing pointer/command boundary, clears the target highlight, and displays the pending Defense response. This milestone does not select or discard a Defense card, resolve/decline Defense, compare totals, mutate the attacking card, or add Ranged Attack/minions/items/modifiers/text timing.

Milestone 2.2 is complete: the existing synthetic definitions now carry typed non-Ranged Attack and Defense-reaction data in addition to their Phase 1 Movement definitions. One test definition declares a printed Primary Attack 3 classified as non-Ranged; both definitions declare Defense reactions with printed Defense 2/3 and an explicit Hand-to-Discard-Pile source-card cost/state transition. Runtime offers project these typed facts only in the applicable normal or reaction window and remain bound to one owned card instance and hero. Discarded and cross-owner cards are ineligible, and missing owners or invalid Attack/Defense definition shapes are rejected. The existing visible Phase 1 panel remains Movement-only. This milestone does not initiate an Attack, open a Defense response, mutate/discard a card, calculate combat totals beyond typed printed-value queries, or add Ranged Attack, Block, Wasp content, items/modifiers, turns, or animations.

Milestone 2.1 is complete: authoritative participant data now uses validated collections of teams, players, and heroes with nonempty unique stable IDs and collection-backed relationship lookups. The synthetic 1v1 configuration creates one Red player/hero and one Blue player/hero; the existing `primitive-hero` ID remains the Red hero, and the new Blue hero has its own stable identity and occupied coordinate. Each hero owns a distinct two-card copy of the Phase 1 synthetic Movement hand, with globally unique card-instance IDs whose owner references resolve through the participant collections. Team queries return collections and are tested with multiple heroes per team rather than assuming the staged fixture's one-hero team size. The validation scene visibly renders both occupancies with distinct Red/round and Blue/square primitive treatments. Clicking either miniature makes it the active director-controlled hero and projects only that hero's stable owned hand; either hero can resolve the existing placeholder Movement offers, while the other hero remains an occupied blocker through the same generic authority. An action already in progress cannot be transferred to the other hero. This does not add 3v3 UI, multiplayer behavior, Attack/Defense offers, combat, or later Phase 2 systems.

Corrective milestone 1.13a is complete: the visible Phase 1 Movement action now stays pending after any selected destination route that costs less than the remaining allowance. The same resolution accumulates every committed adjacent step, advances its choice/step ID, recalculates destination candidates from the hero's new authoritative position, and lets the tester choose another waypoint or Stop. Exhausting the allowance completes automatically; Stop completes while preserving unused allowance. Presentation retains the selected card/action and refreshes highlights between segments, then clears them only when the action completes. This permits deliberately longer cumulative routes through waypoint choices without enumerating every complete route to a destination. Action-start-only restrictions remain a single future checkpoint for the whole Movement action rather than restarting per segment. Phase 1 still does not implement card play/reveal, lifecycle transitions, real Wasp data/images, action-start restriction effects, Fast Travel, direct card-text Move, modal/repeat behavior, reactions, timing effects, turns, snapshot import/export, networking, animation, or broader gameplay resolution.

Documentation milestone 0.4 is also complete: the general card/action rules on printed rulebook pages 7-19 have been reviewed and converted into the durable implementation contract in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`. This specifies future card code but implements none of it.

Documentation milestone 0.5 is complete: `Docs/AI/CARD_EFFECT_CAPABILITIES.md` now defines effect-level contracts for the rulebook's mutation operations, queries, modifiers, control flow, timing, persistent sources, and system transitions, including explicit Object Push, source-relative direct Move, Place, Spawn, token, marker, Swap, Replace, Remove, Defeat, and keyword-query architecture. This also implements no gameplay.

Documentation milestone 0.6 is complete: the roadmap now proves Tie Breaker state, Initiative, turns, rounds, and the complete general match loop with synthetic test cards before the complete Wasp card inventory or Wasp-specific card implementation begins. This sequencing change also implements no gameplay.

Documentation milestone 0.7 is complete: `Docs/AI/TESTING.md` now defines risk-tiered validation. Every executable milestone keeps focused coverage, while Play Mode, affected/full suites, visual checks, source validation, and build smoke tests run only where the changed boundary or risk justifies them. This policy change also implements no gameplay.

## Mission

Create a polished private digital prototype of *Guards of Atlantis II* that can be demonstrated to its publisher as the basis for a possible licensed, cross-platform adaptation.

The private pitch prototype is a complete 3v3 match with six different heroes: Wasp plus five later user-approved, source-verified heroes. It retains single-controller director mode for development and demonstration, and now also targets an invite-only pre-production internet playtest in which six humans each control their independently owned hero before production presentation begins.

## Rights and distribution boundary

- The user does not currently have authorization to publicly distribute an adaptation.
- The prototype is private, noncommercial, and intended for a direct publisher pitch.
- "Guards Digital" is a working title. Final naming is gated on authorization and publisher agreement.
- Do not create a public download, public repository, marketing campaign, monetization, telemetry, or store listing before authorization.
- The initial pitch deliverable is a private Windows standalone build plus a short recorded walkthrough as backup.
- Disposable online playtest builds may be shared only with specifically invited private testers and are not public releases.
- Source code and builds for other platforms remain internal unless specifically requested.

This is a project boundary, not a legal determination.

## Product horizons

### Horizon 1 - Private pitch prototype

A complete 3v3 match with both director and private online validation modes:

- Three Red heroes and three Blue heroes, all different: Wasp plus five later user-approved, source-verified heroes.
- Wasp is implemented and integrated first; a mirrored Wasp 1v1 remains an intermediate development gate, not the pitch endpoint.
- One human controls all six heroes in director mode.
- All six hero hands are available to the controller through team/hero grouping; each nonempty hero hand requires exactly one facedown commitment, each empty hand requires Pass, reveal waits for all six hero responses, and a read-only four-turn history identifies every hero's authoritative revealed card or empty-hand Pass while current Facedown commitments and private hands remain protected by the applicable projection.
- The controller selects exactly one owned card for every hero with a nonempty hand and records Pass for every empty hand, regardless of whether that hero is on the board; after all six heroes have responses, it reveals and resolves the played cards through normal Initiative order.
- Before production presentation begins, six invited humans can connect over the internet from Windows clients, each control exactly one owned hero, see only their authorized private projection, and complete a full match through one authoritative host/session.
- Full official single-lane board structure and standard minion setup.
- Six-player Quick Game setup values: three Wave counters and five Life counters per team.
- Full verified progression for all six heroes, including relevant card tiers, items, and Ultimate access.
- Complete applicable game loop: cards, initiative, actions, combat, defense, minions, rounds, lane pushing, economy, leveling, respawn, and victory conditions.
- No opponent AI, pass-and-play, accounts, public matchmaking, progression meta, or public live-service operations.

### Horizon 2 - Licensed product direction

Post-authorization only:

- Production-ready online 3v3 as the primary mode, building on the private pre-production validation track.
- Six humans, each controlling one hero.
- Cross-platform release, developed and validated in phases.
- Larger hero roster, eventually expected to approach roughly 40 heroes.
- Production-scale hosting, matchmaking, accounts, platform services, moderation, anti-cheat, and operational support remain separate future work.
- No AI-player requirement is currently planned.

## Approved prototype deviations

Everything not listed here must behave exactly like the authoritative physical game sources.

1. Before the final pitch integration, staged development configurations may use one hero per team and mirrored Wasp; both temporary deviations must be retired by the official six-player setup gate.
2. One controller sees and operates all six heroes in director mode.
3. Internal debug tools may expose hidden state, undo commands, restart matches, import/export snapshots, change animation speed, and override the initial Tie Breaker side.

Director mode is an internal/demo path. The private online playtest uses normal one-player/one-hero ownership and does not expose other players' private hands or competitive debug controls.

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
- Rendering pipeline: Built-In Render Pipeline is retained for the current primitive-rule phases. Before production presentation begins, milestone 8.28 migrates the validated primitive match to URP and makes URP the accepted rendering baseline; do not introduce Built-In-only production visual dependencies beforehand.
- Reference development platform: Windows desktop and Unity Editor.
- Active reference build target: Windows 64-bit standalone (`Win64`).
- Input: Unity Input System package `1.19.0`, with **Input System Package (New)** as the only active input handler. The legacy Input Manager is not selected for gameplay input.
- Automated testing: Unity Test Framework `1.7.0`, with independent Edit Mode and Play Mode assemblies. Validation tiers, focused commands, and full-suite gates are documented in `Docs/AI/TESTING.md`.
- Version control: Local Git repository on branch `main`, with root `.gitignore` and `.gitattributes` policies. Unity-generated data, local validation output, the private rulebook, and the extracted card corpus are ignored. Completed roadmap milestones require a local commit after acceptance and continuity updates. No remote is configured.
- Platform rollout direction: Windows first, then macOS/Linux, then touch devices, with closed platforms considered later.
- Use Unity's Input System and keep interactions adaptable across mouse and touch.
- Do not choose a networking provider or SDK before milestone 8.29 compares current support, relay/hosting, cost, licensing, privacy, and operational constraints.

## Core architecture constraints

### Map progression

- Phases 1 through 3 use an expanded temporary test grid only for foundational movement, obstacles, cards, two-hero combat, Initiative, turns, and rounds.
- Phase 4 replaces that grid in the main validation scene with the complete official multi-zone single-lane map.
- The full map model includes every official space, terrain classification, zone, zone boundary, zone adjacency, spawn point, Throne, starting Battle Zone, and ordered lane relationship.
- Map content is selected once per match by stable `MapId` plus content revision. `SpaceId`, `ZoneId`, and `SpawnPointId` are map-local; persisted, catalog, external, or cross-map references compose `MapId` with the applicable child ID. Internal coordinate-based rules and queries may remain local to the match's selected immutable map definition, while loaders, snapshots, and presentation must carry or derive that selection and must not assume one static map or make a bare coordinate globally unique.
- Fast Travel cannot begin until same-zone, adjacent-zone, and non-adjacent-zone queries are verified on the full map.
- Lane pushing cannot begin until the full ordered lane, movable Battle Zone, Thrones, and per-zone minion spawn points are verified.
- Do not use a reduced or invented map as evidence that Fast Travel or lane pushing is correct.

### Authoritative state

- Rules state is plain, deterministic game data, not Unity scene state.
- Logical hexes use integer axial `(q, r)` coordinates; generic adjacency is the six standard axial offsets exposed as named directions.
- The model uses collections and stable IDs for teams, players, heroes, cards, units, spaces, zones, and effects.
- The model must support exactly three independently owned heroes per team for the pitch without hard-coding that team size into general rule APIs.
- Authoritative state must be serializable and schema-versioned.

### Commands and events

- The UI submits commands such as play card, choose action, select target, or confirm movement.
- A rules authority validates commands and produces ordered state-change events.
- Multi-step actions pause on authoritative, data-shaped pending choices and resume through follow-up commands; UI callbacks never own the continuation.
- Director-mode authority runs locally in-process.
- Private online authority runs in one host/session process behind the same typed command, ordered-event, pending-choice, and versioned-snapshot contracts; clients never own rules resolution.
- Randomness must not be generalized beyond the initial Tie Breaker setup decision.

### Undo and diagnostics

- Director-mode Undo restores the authoritative snapshot before the last committed command.
- Undo also cancels and rebuilds presentation from the restored state.
- Full-match undo history is acceptable for the small internal prototype.
- Undo, hidden-state inspection, and Tie Breaker override are internal tools and unavailable in competitive online matches.
- Accepted director commands now record their pre-command snapshot; invalid and presentation-only inputs create no history entry. Undo pops one boundary, and restart restores the initial scenario and clears history. Redo is intentionally absent.
- A player-facing save system is not part of the pitch.
- Internal authoritative snapshot capture/restore, Undo, restart, and the collapsible read-only event log are implemented; planned tools still include animation-speed control.

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
- Phase 3 proves generic card lifecycle, director hands, play/reveal, Initiative and tied Initiative, Hold, Movement and Attack inside turns, the four-turn round skeleton, an extensible repeated-round transition with limited synthetic retrieval, snapshots, Undo/restart, and event logging with synthetic definitions.
- Phases 4 through 7 prove the official map, core full-map actions and combat, minions, tokens, Lane Push, exact end-of-round order, defeat/respawn, progression, victory, and a deterministic complete match with synthetic definitions.
- The complete Wasp card inventory and Wasp-specific card implementation wait until Phase 8, after milestone 7.9 proves the synthetic core match. Milestone 8.2 may re-baseline Wasp-specific work and add verified capability deltas, but it does not gate or redesign the already-proven general core loop.
- Complete Wasp verification remains an intermediate integration gate. Only afterward are five additional heroes selected, inventoried, capability-mapped, implemented, and integrated into the official-setup 3v3 pitch match.
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
- Standard minions use the circular artwork embedded along the top of the private reference atlas `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg` when they first become visible in milestone 4.4. Steam/orange maps to project Red and Crystal/light-blue maps to project Blue; the round riveted/broad spiked face is Heavy, the narrow helmet with cheek guards/armored shoulders is Melee, and the two-pronged helmet/narrow crystal crest is Ranged. The source manifest's `Melee_Steam` label on the Crystal melee sprite is a known metadata error. Milestones 9.3 and 9.5 retain these images as the 2D side of each minion's interchangeable presentation; authoritative team/type data selects the image, and rules identity is never inferred from art or the incorrect source label.

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
- Target animation coverage for each of the six pitch heroes: idle, movement, attack, hit/defeat, and respawn, reusing shared rigs/clips where suitable without sacrificing hero readability.
- Minions use a smaller shared animation set.
- The pitch receives a small sound-effects pass for UI confirmation, card reveal, movement, attack, hit/defeat, minion battle, and lane push.
- Music, voice acting, announcer systems, and elaborate adaptive audio are out of scope.

## Pitch HUD

- The board is the primary focus.
- Director mode exposes all six hands through clear Red/Blue team and hero grouping. Private online clients expose only the local player's hand plus authorized shared played/revealed state; both modes keep the active resolver readable without presentation owning legality.
- Show a compact read-only 6-hero-by-4-turn card history grouped as three Red and three Blue heroes; each entry derives from the authoritative `HeroId` assignment and reveals no committed card before the joint reveal.
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
- Both the primitive scene session and pure placeholder card resolver treat the player's selected reachable destination with its projected deterministic route as the explicit choice, record every internal adjacent step, allow zero movement through Stop, and reject entering or passing through generic placeholder obstacles.
- Before Phase 1 ends, the selected test action offer's typed printed-value query must be the sole source of movement allowance.
- Fast Travel, movement from card text, and movement-limiting effects remain later rule milestones rather than being folded into the early board test.
- Mechanics are completed with primitives and debug UI before production art begins. Milestone 8.28 establishes URP as the accepted rendering baseline, and milestones 8.29-8.36 prove the invite-only online flow and full friends playtest before any production visual asset is introduced.
- Select the highest applicable validation tier from `Docs/AI/TESTING.md` based on the behavior and boundaries changed, not merely diff size.
- Every executable behavior change receives focused automated coverage and directly affected regressions; every fixed bug receives a regression test.
- Prefer tiny deterministic scenarios to complete match replays.
- Run Play Mode and visual checks only for affected Unity-facing or visible paths.
- Run complete unfiltered suites only at implementation-phase endpoints, explicit hard gates, broad-risk infrastructure/schema changes, or when affected consumers cannot be confidently enumerated.
- Run Unity validation serially with the long-running process and licensing safeguards in `Docs/AI/TESTING.md`; a command-runner timeout is not a Unity test result, and an active Unity process is monitored rather than replaced.
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
- **Pitch prototype:** The private 3v3 build with Wasp and five other distinct verified heroes, retaining director mode while also proving invite-only six-human internet play before production presentation.
- **Private online playtest:** An invite-only pre-production Windows test in which six remote humans each control one hero through a host-authoritative session; it is not a public or production live service.
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

- Card execution beyond the completed standalone placeholder Movement/Attack slices and in-turn Hold/Movement/Attack actions, including Attack consequences, resolve-without-action, rounds, or Wasp content before their exact milestone approval.
- Public distribution or commercialization.
- Public online play, public server discovery, or production live-service operation.
- AI opponents.
- Pass-and-play.
- Any seventh pitch hero or hero content not selected through the post-Wasp roster milestone.
- A universal card-text parser.
- Production visuals before the primitive game loop, milestone 8.28 URP migration gate, and milestone 8.36 private online playtest gate have passed.
- Music, voice, tutorials, accounts, public matchmaking, cloud saves, or telemetry.
