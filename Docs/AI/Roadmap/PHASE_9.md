# Phase 9 - Presentation and pitch polish

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_9 handoffs](../Archive/Phase-Handoffs/PHASE_9.md).

## Phase 9 - Presentation and pitch polish

Every item remains a separate micro-milestone. Phase 9 starts only after milestone 8.28 establishes URP as the accepted rendering baseline, milestone 8.36 closes under [its user-approved acceptance amendment](PHASE_8.md), and independent reviews 8.28a and 8.36a dispose of every gate-blocking finding.

Milestone 8.19's designated roadmap audit recommends **keeping 9.1-9.16a unchanged**. The capability
matrix changes which rule events and prompts presentation must consume, but 8.20-8.24 and the primitive
online HUD must prove those typed projections before production presentation is scoped. Board, hero,
minion, camera, representation switching, five hero-animation families, shared minion animation, event
queue, final HUD, readability, audio, the final gate, and its fresh review each have distinct asset,
lifecycle, visual, accessibility, or evidence boundaries; merging them would obscure a failed visual or
runtime path, while splitting them now would guess at an asset pipeline and playtest findings not yet
known. Reassess exact asset counts and acceptance at the 8.36a gate. Current planning range is **25-50
focused engineering days**, excluding asset procurement/licensing, external feedback, approval latency,
and corrective work.

Each visible milestone runs focused Play Mode checks for the affected presentation path plus one targeted visual inspection. The disposable migration-check build from milestone 8.28 and invited-tester online build from milestone 8.36 are not retained as pitch deliverables or publicly distributed; the first retained private pitch player build remains milestone 10.1. Complete unfiltered suites wait for milestone 9.16 unless a T3 risk trigger applies earlier.

### [ ] 9.1 Production board environment

### [ ] 9.2 Six hero miniatures and Red/Blue team treatments

### [ ] 9.3 Minion miniatures

Production minion miniatures preserve the milestone 4.4 atlas-derived circular minion tokens as each unit's 2D representation rather than replacing or redrawing that source.

### [ ] 9.4 Bounded pan, orbit, zoom, and reset camera

### [ ] 9.5 Automatic 3D miniature / 2D token switching

Top-down switching uses the reviewed milestone 4.4 Steam/orange-to-Red and Crystal/light-blue-to-Blue Heavy/Melee/Ranged token mapping from `Docs/Guards of Atlantis II - Tabletopia Cards/extracted/text-assets/g713916_s799373.sprite0__45.jpg`, including the correction for the manifest's mislabeled Crystal melee sprite, while the same authoritative minion instance continues to own both its 3D and 2D presentation.

### [ ] 9.6 Six-hero idle animation coverage

### [ ] 9.7 Six-hero movement animation coverage

### [ ] 9.8 Six-hero attack animation coverage

### [ ] 9.9 Six-hero hit/defeat animation coverage

### [ ] 9.10 Six-hero respawn animation coverage

### [ ] 9.11 Shared minion animation set

### [ ] 9.12 Rule-event presentation queue, skip, and speed controls

### [ ] 9.13 Final pitch HUD and contextual prompts

Observable outcome: The polished HUD keeps the complete 3v3 match state and active resolver readable, including a compact read-only four-turn history for all six independently owned heroes.

Acceptance checks:

- The round-history treatment groups three Red and three Blue heroes and shows each hero's authoritative revealed card or empty-hand Pass in turns 1-4 without relying on color alone.
- Director mode can inspect the complete history; private online clients receive only the authorized shared played/revealed projection and never another player's hidden hand or pre-reveal card identity.
- History rebuilds identically after snapshot restore, Undo, restart, reconnect, and resynchronization without owning or mutating match state.
- The history, active resolver, six hands, contextual prompts, round/turn, Lives, Waves, hero levels, and coins remain readable at the supported desktop resolutions.

Non-goals: No editable history, replay timeline, analytics, telemetry, or public match-history service.

### [ ] 9.14 Team symbols, outlines, readability, and UI scaling baseline

### [ ] 9.15 Minimal sound-effects pass

### [ ] 9.16 Complete presentation regression and visual-readability sweep

Observable outcome: The finished pitch presentation remains readable and synchronized with authoritative rules across the complete primitive-to-polished match flow.

Acceptance checks:

- A targeted visual sweep covers the board, all six team/hero-grouped hands, played/revealed cards, prompts, event log, six-hero and team differentiation, camera bounds, 3D/2D switching, animations, and affected audio cues.
- Skipping or accelerating presentation produces the same authoritative state and ordered rules events.
- Common reference desktop resolutions preserve readable cards, values, prompts, and team identity.
- At the exact final revision, six local client processes use the milestone 8.35c playtest-build topology to occupy six distinct seats and complete a full match through the polished player UI. The run includes disconnect/reconnect and resynchronization and proves host/client convergence, owner-private hand and pre-reveal privacy, rejection of cross-seat control, correct control ownership after reconnect, and removal of stale actionable prompts.
- The recorded gate evidence identifies the source, build, content/schema, provider/SDK/configuration, topology, and test-recipe revisions. The milestone repeats the real-internet playtest only when the affectedness or failure triggers in [the post-polish multiplayer regression gate](../Testing/FUTURE_GATES.md#post-polish-multiplayer-regression-gate) require it.
- T3 pre-build gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No new networking feature, provider migration, public matchmaking, account system, public distribution, production-service reliability claim, or unconditional repeat of the invited real-internet playtest.

### [!] 9.16a Independently review pre-build engineering readiness

Blocked by: Milestone 9.16 must pass its presentation and pre-build T3 gate.

Observable outcome: A fresh, narrowly scoped analysis-only review determines whether the unchanged pitch revision is technically safe to package and demonstrate on a clean Windows machine.

Acceptance checks:

- The review checks runtime warnings/errors, repeated-match resource and memory behavior, frame-time or allocation spikes, unsupported/editor-only dependencies, build-setting assumptions, failure handling, and diagnostic usefulness using the actual final revision and existing gate evidence.
- It checks for dead or unsafe debug paths, fragile scene/content references, and presentation/runtime coupling likely to fail outside the development Editor.
- Findings follow the common independent-review evidence contract; purely aesthetic or broad maintainability preferences do not block the build without a concrete reliability consequence.
- Any required correction becomes a separately approved milestone and invalidates/re-runs only the applicable evidence under `TESTING.md`; the review itself changes no code, content, scene, configuration, or build.
- The review records an explicit proceed/block recommendation for milestone 10.1.

Non-goals: No build creation, visual redesign, general refactor, new feature, installer/updater work, production optimization program, or public-release readiness assessment.

Hard gate: The first retained private pitch player build does not begin until milestone 9.16a recommends proceeding and every gate-blocking corrective milestone is complete. The disposable render-pipeline smoke from milestone 8.28 and invited-tester online build from milestone 8.36 are pre-production evidence only.
