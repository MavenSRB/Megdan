# Phase 0 - Shared context and project shell

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_0 handoffs](../Archive/Phase-Handoffs/PHASE_0.md).

## Phase 0 - Shared context and project shell

### [x] 0.1 Establish living documentation

Observable outcome: Future tasks have a stable entry point, agreed product context, a proposed roadmap, and rule traceability structure.

Artifacts:

- `AGENTS.md`
- `Docs/AI/PROJECT_CONTEXT.md`
- `Docs/AI/ROADMAP.md`
- `Docs/AI/RULES_LEDGER.md`

Non-goals: No Unity project, code, assets, or builds.

Historical evidence: exempt - milestone 0.1 created the documentation system itself and predates the phase-handoff archive; an invented record would be a worse failure than this admitted gap (stated at 6.1z).

### [x] 0.2 Create the empty Unity project

Observable outcome: An empty 3D Unity project opens in installed editor `6000.5.7f1` and contains no gameplay.

Acceptance checks:

- Exact editor version is recorded by Unity project metadata.
- Project opens without compilation errors.
- Windows is the active reference build platform.
- Input System choice is recorded.
- A clean version-control ignore policy is present if version control has been authorized.

Non-goals: No board, camera system, game state, card data, or production packages.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-02-create-the-empty-unity-project-completion-evidence).

### [x] 0.3 Add the automated test shell

Observable outcome: One trivial Edit Mode test and one minimal Play Mode smoke test can be run independently.

Acceptance checks:

- Focused test commands are documented.
- Tests do not require a full build or a complete game scene.

Non-goals: No game-rule tests yet.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-03-add-the-automated-test-shell-completion-evidence).

Repository note (2026-08-09): Git was authorized after milestone 0.3. The completed 0.1-0.3 baseline is captured together in the initial local repository commit; later roadmap milestones receive individual completion commits.

### [x] 0.4 Establish the card-system architecture contract

Observable outcome: Future card work has a rulebook-grounded implementation contract that separates immutable content from runtime state and defines extensible action, choice, effect, targeting, timing, event, validation, and versioning boundaries.

Artifacts:

- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`
- Aligned card sequencing in `Docs/AI/ROADMAP.md`
- Aligned continuity, source coverage, and milestone-test guidance in `AGENTS.md`, `Docs/AI/PROJECT_CONTEXT.md`, `Docs/AI/RULES_LEDGER.md`, and `Docs/AI/TESTING.md`

Acceptance checks:

- The contract accounts for Primary, Secondary, replacement, and reaction contexts.
- The contract accounts for ordered mandatory/optional steps and serializable pending choices.
- The contract accounts for overlapping card-state facts, typed targets/values, staged and simultaneous effects, Active effects, continuous rules sources, and choice-aware timing.
- The contract defines a controlled extension path for ordinary and genuinely unique future cards.
- Early card and combat milestones prove the architecture in small slices before verified Wasp content is added.

Non-goals: No card code, Wasp inventory, corpus-wide effect taxonomy, Unity content, or gameplay implementation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-04-establish-the-card-system-architecture-contract-completion-evidence).

### [x] 0.5 Establish the reusable card-effect capability catalog

Observable outcome: Future cards can reference a source-traceable catalog of typed mutation, query, modifier, control-flow, timing, persistent-source, and system-transition capabilities instead of adding ad hoc effect logic.

Artifacts:

- `Docs/AI/CARD_EFFECT_CAPABILITIES.md`
- Aligned effect gates in `AGENTS.md`, `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`, `Docs/AI/PROJECT_CONTEXT.md`, `Docs/AI/ROADMAP.md`, and `Docs/AI/RULES_LEDGER.md`

Acceptance checks:

- The catalog defines a mandatory registration contract covering roles, parameters, choice ownership, optionality/failure, atomicity, state changes, checkpoints, events, continuation data, validation, and tests; concrete schemas are instantiated only when a verified milestone registers that capability/version.
- Stable effect-node IDs and typed input/outcome bindings preserve actual results for later conditional steps and pending-resolution serialization.
- Object Push, source-relative direct Move, Movement action, Place, Spawn, Respawn, Swap, Replace, Remove, Defeat, Clear, tokens, and markers have distinct contracts.
- Rulebook query/modifier concepts including adjacency, Range/Radius, direction/straight line, distance ordering, zones, targeting versus presence, immunity, `Counts As`, obstacles, and effective type have explicit owners.
- Card-state, value/resource, timing, Active-effect, continuous-source, control-flow, and system-transition capabilities are covered.
- Card text such as `move a unit towards you` is represented by direct Move plus source-relative destination constraints rather than an invented Pull keyword/capability.
- New verified effects have a documented extension and source-linked testing process.

Non-goals: No effect handlers, Wasp inventory, card corpus taxonomy, Unity content, or gameplay implementation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-05-establish-the-reusable-card-effect-capability-catalog-completion-evidence).

### [x] 0.6 Re-sequence development around the core match loop

Observable outcome: The roadmap proves a deterministic match with synthetic test cards before the complete Wasp set is inventoried or implemented.

Acceptance checks:

- Configurable multi-space movement is followed immediately by obstacle blocking in milestones 1.8-1.9.
- Test cards prove Tie Breaker state, Initiative ordering, play/reveal, turns, and the four-turn round structure in Phases 2-3.
- The full official map and generic core systems for combat, minions, Lane Push, end-of-round ordering, economy, progression, respawn, and victory are completed in Phases 4-7.
- A deterministic test-card core match is a hard gate before the complete Wasp card inventory or Wasp-specific card implementation begins in Phase 8.
- The Wasp capability matrix identifies Wasp-specific gaps and refinements; it no longer owns or delays general core-rule capabilities.

Non-goals: No gameplay code, Wasp inventory, Unity content, or implementation milestone is authorized by this planning change.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-06-re-sequence-development-around-the-core-match-loop-completion-evidence).

### [x] 0.7 Adopt proportional risk-tiered validation

Observable outcome: Every milestone uses the smallest test set that proves its behavior and protects affected contracts, while complete Unity suites remain mandatory at integration gates and for broad-risk changes.

Acceptance checks:

- Documentation/source-only changes use diff, reference, milestone-ID/status, traceability, and authority-consistency checks without launching Unity.
- Every executable behavior change has focused automated tests plus directly affected regressions; every bug fix adds a regression test.
- Play Mode and visual checks run only when a Unity-facing path or visible output changes.
- Unfiltered Edit Mode and Play Mode suites run at implementation-phase endpoints, explicit hard gates, before the first player build, after shared test/package/assembly infrastructure changes, or when affected consumers cannot be confidently enumerated.
- Rule-dependent work verifies its authority first and records source-linked scenarios; unresolved ambiguity blocks completion.
- Completion evidence records the chosen tier, exact checks/results, and why any unaffected suite was omitted.

Non-goals: No gameplay, test-runner code, Unity content, source transcription, or build is added by this documentation-only policy milestone.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_0.md#milestone-07-adopt-proportional-risk-tiered-validation-completion-evidence).
