# Phase 4 - Full official multi-zone board

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_4 handoffs](../Archive/Phase-Handoffs/PHASE_4.md).

## Phase 4 - Full official multi-zone board

### [x] 4.1 Inventory the complete single-lane board reference

Observable outcome: Every part of the complete official single-lane board - including all spaces, connections, terrain, zones, spawn points, Thrones, and the starting Battle Zone - has a reviewed data specification.

Acceptance checks:

- Every logical space has a stable ID and coordinate.
- Every zone boundary, zone membership, spawn point, and Throne is checked against the authoritative visual reference.
- The inventory covers the full map rather than a reduced movement-test area.

Non-goals: No production board art or models.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-41-inventory-the-complete-single-lane-board-reference-completion-evidence).

### [x] 4.1a Harden map-scoped identity and validation contracts

Observable outcome: The map specification and source-validation boundary can admit additional future map definitions without weakening the reviewed `official-single-lane` evidence or relying on globally unique child IDs.

Acceptance checks:

- `SpaceId`, `ZoneId`, and `SpawnPointId` are explicitly map-local; any persisted, cross-map, catalog, or external reference composes the stable `MapId` with the applicable child ID.
- The selected map is identified by an immutable `MapId` plus content revision, and coordinate-based rule/query APIs are explicitly local to that one selected match map rather than globally qualified individually.
- Reusable schema/invariant validation is separated from `official-single-lane` source-review checks; the reusable layer contains no official-map IDs, counts, or digest.
- Official-map validation still enforces every reviewed count/location and protects all rules-relevant zone metadata, including zone identity, kind, team, Throne status, and starting-Battle-Zone status.
- A bounded synthetic validation fixture may reuse coordinates and child IDs under a different synthetic `MapId` to prove scoping, but it is not retained or presented as another playable map.
- T0 documentation/source validation passes; no Unity suite is required because no Unity runtime or presentation path changes.

Non-goals: No authoritative map loader, snapshot schema change, Unity scene geometry, second playable map, zone adjacency, ordered Lane relationship, or production art.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-41a-harden-map-scoped-identity-and-validation-contracts-completion-evidence).

### [x] 4.2 Load the selected map definition into authoritative state

Observable outcome: A match loads one selected map definition through a reusable boundary, and authoritative state plus snapshots retain and validate that map's stable identity and content revision.

Acceptance checks:

- A reusable loader/catalog boundary resolves a requested `MapId` and content revision without a singleton instance, fixed official-map resource path in rules code, or map-name branches.
- The loaded immutable map definition owns its spaces, terrain, zones, and spawn points; existing coordinate-based movement/query authorities operate within that selected definition.
- The authoritative match/board exposes its immutable selected-map reference rather than requiring every internal coordinate value to repeat `MapId`.
- The versioned authoritative snapshot serializes `MapId` and map content revision, resolves them through the supplied current map definitions, and rejects missing or mismatched map content with a typed reason.
- Focused pure tests use bounded synthetic definitions that reuse coordinates and map-local child IDs to prove isolation, selection, and snapshot mismatch rejection without adding another playable map.
- Existing movement, Attack, round, snapshot, Undo, restart, and event-log regressions remain green with the official map definition selected.

Non-goals: No Unity scene replacement, second playable map, zone adjacency queries, Fast Travel, lane pushing, setup placement, or production art.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-42-load-the-selected-map-definition-into-authoritative-state-completion-evidence).

### [x] 4.2a Upgrade the test grid to the full official placeholder map

Observable outcome: The temporary Phases 1-3 test grid is replaced in the main validation scene by placeholder geometry driven by the selected complete official multi-zone single-lane map definition.

Acceptance checks:

- The scene consumes the selected loaded map definition rather than rebuilding topology from a radius or a hard-coded official-map coordinate list.
- Logical adjacency matches the reviewed board specification.
- Terrain and spawn-point classifications are inspectable.
- Every official zone is visibly distinguishable in debug presentation without implying final art.
- The complete map can be framed and inspected under the current fixed-camera test setup.
- Focused Edit Mode and saved-scene Play Mode checks prove presentation rebuilds from the selected map and does not own authoritative topology or map selection.

Non-goals: No second playable map, polished environment, Fast Travel, lane pushing, or production camera controls.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-42a-upgrade-the-test-grid-to-the-full-official-placeholder-map-completion-evidence).

### [x] 4.3 Define and validate zones and zone adjacency

Observable outcome: Every board space belongs to the correct official zone, and authoritative queries can distinguish the same zone, an adjacent zone, and a non-adjacent zone.

Acceptance checks:

- Red Throne, Blue Throne, the starting Battle Zone, and every intermediate zone have stable IDs.
- Every playable space has the correct zone membership.
- Zone adjacency follows the rulebook definition and is verified against the complete map.
- Focused tests cover same-zone, adjacent-zone, and non-adjacent-zone pairs in both directions.
- The ordered lane relationship between the two Thrones is represented for later lane movement.

Non-goals: No Fast Travel validation, current-Battle-Zone changes, lane pushing, minion respawn, or victory logic.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-43-define-and-validate-zones-and-zone-adjacency-completion-evidence).

### [x] 4.4 Place the 1v1 Quick Game setup

Observable outcome: Red Wasp, Blue Wasp, standard minions, Lives, Waves, and Tie Breaker state are placed from configuration data while their hands still use synthetic test cards.

Acceptance checks:

- The setup configuration declares its required `MapId` and map content revision.
- Setup creation requires the exact selected map definition and rejects an unavailable or mismatched map identity/revision.
- Every configured occupied space and spawn point resolves within that selected map; map-local IDs are never resolved against another map.
- Three Wave counters and four Life counters per team.
- Standard single-lane minions are created from configuration with stable identity, team, type, and occupied-space data.
- The visible standard minions use the circular artwork in the private reference atlas `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg`: Steam/orange maps to project Red and Crystal/light-blue maps to project Blue; within each team, the round riveted/broad spiked face is Heavy, the narrow helmet with cheek guards/armored shoulders is Melee, and the two-pronged helmet/narrow crystal crest is Ranged. The source manifest incorrectly labels the Crystal melee sprite `Melee_Steam`; implementation must use this reviewed mapping, select art from authoritative team/type data, and never infer rules identity from the image or manifest label alone.
- The initial Tie Breaker result comes from milestone 2.6.
- Only the approved player-count, mirrored-hero, and director-mode deviations are present.
- Source/data validation, focused setup-scene inspection, and directly affected Edit Mode and Play Mode suites pass. The complete unfiltered Phase 4 gate moves to milestone 4.4d so it validates the same configured setup after the behavior-preserving architecture refactor.

Non-goals: No complete Wasp card inventory or Wasp-specific behavior.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44-place-the-1v1-quick-game-setup-completion-evidence).

### [x] 4.4a Establish the transport-neutral match application service

Observable outcome: One pure C# application-service boundary owns the configured authoritative match and accepts typed gameplay commands for both live director input and deterministic replay, while Unity components no longer coordinate low-level rules authorities directly.

Acceptance checks:

- A `GuardsDigital.Application`-equivalent assembly has no Unity dependency and exposes one explicit match-session port over immutable initial scenario inputs, current authoritative state, typed commands/choices, accepted or rejected results, ordered events, and a monotonically changing immutable projection/version.
- The session composes the existing participant, selected-map, board, card-action, turn/round, Tie Breaker, snapshot, command-replay history, and diagnostic-event boundaries without duplicating their rules or moving authority into presentation.
- Live director commands, milestone 3.10a replay, Undo, and restart use the same command dispatcher and public rules-authority paths. Rejected commands expose no partial state or projection and never enter accepted history.
- Presentation and future transports depend on the session port rather than concrete Movement, Attack/Defense, Initiative, turn, round, snapshot, or history implementations.
- Focused equivalence tests cover every currently supported command family and prove unchanged serialized authoritative state, ordered events, pending choices, Undo/restart results, and event-log projection.
- One deterministic replay oracle compares accepted-command identity/order, rejection results, ordered semantic events, immutable projection after each accepted command, final authoritative snapshot, and completed history across live execution, fresh replay, retained-prefix Undo replay, and snapshot restore plus continuation.
- Bounded invariant/property fixtures use reported fixed seeds and prove at minimum that rejected commands mutate no state/history/events/version, ownership and occupancy remain valid, pending choices reference live stable identities, reveal never exposes an incomplete commitment set, and replay appends no extra accepted command.

Non-goals: No gameplay-rule change, visual redesign, networking provider, client authorization/projection policy, save UI, production dependency-injection framework, service locator, or global event bus.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44a-establish-the-transport-neutral-match-application-service-completion-evidence).

### [x] 4.4b Decompose card-action resolution behind typed strategy handlers

Observable outcome: The shared card-action dispatcher preserves one deterministic resumable resolution boundary while Movement and Attack/Defense behavior live behind separately testable typed handlers rather than continuing to grow one action-kind authority.

Acceptance checks:

- One explicit registry maps supported typed action/capability identities to pure handlers without reflection, card-name/hero-name branches, runtime printed-text parsing, a service locator, or an untyped universal payload.
- The dispatcher retains cross-action invariants: one active resolution, stable resolution/step identity, authoritative offer revalidation, atomic rejection, serializable pending choice, and ordered event publication.
- Movement and Attack/Defense handlers retain their existing typed commands, results, state transitions, pending continuations, restoration validators, and deterministic event order.
- Adding a bounded synthetic handler fixture proves a new supported strategy can be registered and tested without changing dispatcher control flow or unrelated handlers.
- Before/after equivalence tests compare accepted and rejected scenarios, authoritative snapshots, event projections, turn/round integration, and deterministic replay for every currently supported action path.

Non-goals: No new action kind, card text, effect capability, Attack consequence, Fast Travel, timing system, schema migration, reflection-based plugin system, or speculative general scripting language.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44b-decompose-card-action-resolution-behind-typed-strategy-handlers-completion-evidence).

### [x] 4.4c Replace the validation god-controller with feature MVP

Observable outcome: The validation scene uses Model-View-Presenter over the match-session port: feature presenters translate user intent and immutable projections, passive Unity views render view models, and one small composition root owns scene wiring.

Acceptance checks:

- The current `HeroSelectionController` responsibilities are separated into bounded board-interaction, director-hand/commitment, round/action, Undo/restart, and diagnostic-log presenters plus passive views; no replacement type owns all of session authority, history, input, projection, and rendering.
- Presenters are plain testable C# collaborators, depend only on the match-session port and their view port, and never call one another directly. Cross-feature change flows through a new immutable session projection.
- Unity views receive immutable view models and forward user intent through explicit callbacks. They do not query or mutate rules authorities, create snapshots, decide legality, own pending continuations, or reconstruct match state.
- The round/action view exposes one contextual **End Turn** control and no separate Movement Stop button. Its immutable view model carries the exact authoritative typed invocation: Hold at ordinary unresolved-card action selection, Stop during an optional Movement continuation, and no enabled invocation during a mandatory choice. The passive view contains no Hold/Stop/action-kind/name/phase branching, and a bounded presenter test proves that a later projected typed turn-ending invocation can reuse the same control without changing the view.
- Scene-object discovery and dependency wiring are restricted to one composition root; feature views and presenters do not use global object discovery, a singleton, or a service locator.
- Snapshot restore, command-replay Undo/restart, hero/obstacle placement, pending highlights, Defense controls, hands, round/turn status, Tie Breaker, and the event log rebuild identically through the new projection path.
- Focused Edit Mode and saved-scene Play Mode tests prove behavior and visible layout equivalence across all existing interaction paths.

Non-goals: No production HUD, UI Toolkit migration, animation, sound, networking, new gameplay, broader presentation redesign beyond the approved contextual End Turn consolidation, or generic framework beyond the current match needs.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44c-replace-the-validation-god-controller-with-feature-mvp-completion-evidence).

### [x] 4.4d Close Phase 4 runtime hygiene and architecture gate

Observable outcome: The refactored Phase 4 match runs with explicit generated-resource ownership, no known project-authored hot-path allocation churn or obsolete Unity object-discovery APIs, robust malformed-envelope rejection, and a recorded performance baseline before Phase 5 expands the action system.

Acceptance checks:

- Generated Materials, Meshes, Input Actions, and other owned Unity/native resources have explicit reuse and teardown; repeated map rebuild/scene lifecycle tests do not monotonically increase owned native-object counts.
- Unchanged HUD repaint/update paths reuse cached styles, immutable view models, labels, definitions, offers, and lookup collections rather than rebuilding project-owned LINQ arrays or GUI objects each event. A Unity Profiler or `ProfilerRecorder` capture records the managed-allocation and frame-time baseline for the complete 292-cell map and a representative two-round director flow.
- Project-authored runtime and test code remains on the supported Unity 6.5 object-discovery APIs and compiles without `CS0618` warnings; scene discovery remains confined to the composition root.
- Rule-event-log envelope parsing handles invalid Base64, truncated headers/bodies, invalid counts, unsupported versions, trailing data, and oversized/malformed inputs through the documented non-throwing or typed snapshot-rejection contract, with no partial restore.
- Project-local architecture-conformance checks fail if pure rules/application assemblies reference Unity, presentation bypasses the match-session port for low-level authorities, views reference authoritative state/snapshot implementations, forbidden singleton/service-locator/global-discovery paths reappear, or card/hero display-name branches and runtime printed-text parsing occur outside an explicitly approved isolated boundary.
- A small versioned compatibility corpus covers representative setup, active-turn, pending-choice, end-of-round, and completed-match snapshots. Current supported fixtures round-trip deterministically; intentionally unsupported schema/content revisions reject distinctly, and every future durable schema or content-identity change must record an explicit migrate-or-reject decision rather than silently changing meaning.
- The recorded performance baseline gains noise-tolerant regression thresholds for steady-state managed allocation, owned-resource growth, map rebuild, snapshot/projection, and representative replay costs. Crossing a threshold requires investigation and an approved disposition, not automatic speculative optimization; threshold changes retain the prior measurement and rationale.
- The application/session, strategy-handler, presenter/view, snapshot, command-replay, event-log, map/setup, and saved-scene focused regressions pass, followed by both complete unfiltered Edit Mode and Play Mode suites.
- Targeted visual inspection confirms the complete Phase 4 configured setup and validation controls remain readable and behaviorally unchanged.

Non-goals: No production optimization target, DOTS/ECS rewrite, production UI migration, object pooling without measured need, gameplay change, networking, build, or polished art.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44d-close-phase-4-runtime-hygiene-and-architecture-gate-completion-evidence).

### [x] 4.4e Independently review the Phase 4 architecture baseline

Observable outcome: A fresh analysis-only review determines whether the completed application-service, command-replay, action-strategy, MVP, resource-lifetime, diagnostic-envelope, and performance-baseline work is a sound foundation for expanding the general action system.

Acceptance checks:

- The review traces representative live, replay, Undo/restart, snapshot, event, pending-choice, and presentation paths through the actual implementation and checks that authority and dependency direction match the durable contracts.
- It checks whether the former controller and combined resolver responsibilities were genuinely decomposed rather than moved into replacement god objects or hidden service-location paths.
- It evaluates abstraction proportionality, generated-resource ownership, allocation/frame-time evidence, supported Unity APIs, warning state, and the ability of existing tests to detect behavioral drift.
- Findings follow the independent-review classification and evidence contract in the roadmap execution rule; any justified fix is proposed as a separate corrective micro-milestone with no runtime implementation in this review.
- The review re-evaluates the Phase 5 sequence and records whether the shortest dependency-safe path remains valid.

Non-goals: No gameplay or presentation change, refactor, optimization implementation, new abstraction, networking work, production performance target, or weakening of the Phase 4 evidence.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44e-independently-review-the-phase-4-architecture-baseline-completion-evidence).

Hard gate: Phase 5 does not begin until milestone 4.4e is reviewed and every finding classified as gate-blocking has an approved disposition and, when required, a completed corrective milestone.

### [x] 4.4f Complete the configured MVP cutover and highlight-resource ownership

Observable outcome: The configured saved scene routes every board intent through the feature MVP boundary, the legacy controller no longer owns configured-match orchestration/rendering, and every generated highlight resource has explicit lifetime ownership.

Acceptance checks:

- Configured hero-target and Movement-destination pointer input enters through explicit passive board-view callbacks, is translated by `BoardInteractionPresenter`, and reaches only `IMatchSession`; no saved-scene input adapter or legacy controller submits a configured command directly.
- Configured session creation, feature presenter/view composition, projection refresh, and feature HUD drawing move to bounded scene-composition ownership. Any retained unconfigured presentation drill is explicitly isolated and cannot supply configured-match input, orchestration, projection, or rendering.
- No replacement type combines session authority, command history, pointer/GUI input, all feature projections, and rendering. Feature presenters remain peer-independent and passive views remain authority-free.
- The destination-highlight Material records whether it was generated, reuses one owned instance, destroys only that instance at teardown, and never destroys a saved/assigned asset. Repeated controller/scene lifecycles return the exact owned highlight/native-resource counts to baseline.
- Configured commitment, Initiative, Hold/Movement Stop, Movement, Attack/Defense, end-of-round, Undo/restart, snapshot restore, diagnostic log, layout, and input-blocking behavior remain equivalent through focused Edit Mode and saved-scene Play Mode tests.
- Architecture/source checks fail if configured board input bypasses its presenter, configured session/feature ownership returns to the legacy controller, a replacement god object appears, or generated highlight ownership loses its teardown contract.

Non-goals: No gameplay, action-strategy refactor, production HUD redesign, UI Toolkit migration, animation, sound, networking, object pooling, or polished art.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44f-complete-the-configured-mvp-cutover-and-highlight-resource-ownership).

### [x] 4.4g Extract the production Movement and Attack/Defense strategy backends

Observable outcome: Production Movement and Attack/Defense behavior is owned by distinct typed strategy backends rather than delegating both handlers back into the combined card-action authority.

Acceptance checks:

- Concrete Movement and Attack/Defense backends independently own their handler-specific pending state, validation, restoration, mutation, and typed results; the central dispatcher does not implement either backend interface or contain their action-specific algorithms.
- The dispatcher retains only cross-action invariants: one active resolution, stable resolution/step identity, authoritative offer revalidation, atomic rejection, serializable pending-choice publication, restoration coordination, and ordered event publication.
- Production registry composition maps the stable `movement-v1` and `attack-defense-v1` identities to the distinct concrete handlers without reflection, service location, display-name/printed-text branching, or an untyped universal payload.
- Snapshot schema/version and serialized Movement/Attack continuation meaning remain unchanged; supported compatibility fixtures round-trip byte-identically and replay/Undo/snapshot continuation remain equivalent.
- Focused tests prove production composition is genuinely decomposed, each handler rejects foreign or stale continuations without mutation, and all current Movement, Attack/Defense, turn, round, application-session, snapshot, replay, and architecture regressions pass.

Non-goals: No new action kind, Fast Travel, card text, effect capability, Attack consequence, timing system, snapshot migration, reflection plugin framework, or speculative scripting language.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44g-extract-the-production-movement-and-attack-defense-strategy-backends).

### [x] 4.4h Re-close the corrected Phase 4 architecture and performance gate

Observable outcome: The corrected Phase 4 foundation has direct projection-cost evidence, all 4.4e findings have completed dispositions, and the T3 gate is green at one final revision before Phase 5.

Acceptance checks:

- The performance fixture retains the prior snapshot-only measurements and separately records a noise-tolerant complete-map application-projection or representative accepted-command refresh cost that includes immutable projection construction and its authoritative/diagnostic snapshot work.
- Capture environment, sample count, baseline revision, metric ownership, prior measurement, new measurement, threshold, and threshold rationale are recorded without claiming a production target or prescribing speculative optimization.
- The corrected configured input/MVP/resource-lifetime and production strategy-decomposition architecture checks plus their focused Edit Mode and saved-scene Play Mode regressions pass.
- Snapshot compatibility, live/replay/Undo/restart/restore equivalence, pending-choice identity, event ordering, warning/API state, map/setup presentation, and generated-resource lifecycle evidence remain green.
- Both complete unfiltered Edit Mode and Play Mode suites pass at the final revision, followed by targeted visual inspection of the unchanged complete configured setup and validation controls.
- The 4.4e finding-disposition addendum links `F44E-01` through `F44E-04` to their completion/validation evidence; no gate-blocking row remains open before 5.1 is proposed.

Non-goals: No gameplay, production optimization target, pooling, DOTS/ECS, production UI migration, networking, build, or polished art.

Hard gate: Phase 5 does not begin until milestone 4.4h completes and every 4.4e finding has its required completion evidence.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44h-re-close-the-corrected-phase-4-architecture-and-performance-gate-completion-evidence).

### [x] 4.4i Independently verify the corrected Phase 4 foundation

Observable outcome: A fresh analysis-only review determines whether the 4.4f-4.4h corrections genuinely close the 4.4e findings without introducing replacement ownership, authority, lifecycle, performance-evidence, or test-quality problems before Phase 5 expands the action system.

Acceptance checks:

- The review runs in a fresh task at the final corrected Phase 4 revision and inspects actual source, tests, warnings, runtime paths, and the exact 4.4h T3 profiling/diagnostic evidence rather than relying on corrective completion notes.
- It retraces representative configured board input, live command, replay, Undo/restart, snapshot restore, event, pending-choice, application-projection, and presentation paths and verifies that authority, dependency direction, atomicity, deterministic identity, and resource ownership still match the durable contracts.
- It determines whether the configured runtime/HUD/presenter cutover and distinct Movement and Attack/Defense backends are genuine decompositions rather than replacement god objects, hidden service-location paths, cross-handler leakage, duplicated resolution routes, or test-only conformance.
- It independently rechecks the evidence and closure claims for `F44E-01` through `F44E-05`, including generated-versus-assigned Material teardown, configured saved-scene input routing, strategy-backend ownership, accepted-command/projection metric ownership, and additive documentation-governance preservation.
- Findings follow the roadmap's independent-review classification and evidence contract. Any justified runtime, test, tooling, or roadmap correction is proposed as a separately approved micro-milestone; the review itself implements no fix.
- The review re-evaluates whether 5.1 remains the shortest dependency-safe next step. The fresh 4.4h T3 evidence is reused unless source inspection identifies a concrete evidence gap or a diagnostic is required to test a specific concern.

Non-goals: No gameplay, presentation, runtime, test, scene, content, package, assembly, optimization, networking, build, or polished-art change; no automatic rerun of unchanged complete Unity suites; no reopening a closed 4.4e finding without new evidence.

Hard gate: Phase 5 does not begin until milestone 4.4i is reviewed and every finding classified as gate-blocking has an approved disposition and, when required, completed corrective evidence. If 4.4i requires any pre-Phase-5 corrective micro-milestone, the recursive review-correction closure rule automatically requires a separately numbered fresh-task independent verification after the final correction; Phase 5 remains blocked through that latest required verification.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_4.md#milestone-44i-independently-verify-the-corrected-phase-4-foundation-completion-evidence).
