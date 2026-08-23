# Phase 8 - Six-Hero Roster Re-baseline and Shared Foundations

Canonical Phase 8 shard for milestone 8.19's audit and shared foundation milestones 8.19a-8.19c. Return
to [PHASE_8.md](PHASE_8.md).

## Milestone 8.19 audit outcome

Keep the deterministic 3v3, URP, private-online, and independent-review gates at 8.25-8.36a; split the
hero work at the three genuinely shared risk boundaries below, then keep one complete composition
milestone per hero. The roster map is
[Rules/ROSTER_CAPABILITY_MATRIX.md](../Rules/ROSTER_CAPABILITY_MATRIX.md). It found one explicit schema
change, no runtime-text parser, no hero-name branch, and one isolated typed system handler: One Man Army's
Minion-Battle replacement. The dependency-safe route is 8.19a-8.19c, Arien, Brogan, Sabina, Tigerclaw,
Xargatha, then the unchanged six-player setup and integration gates.

Updated focused-engineering estimate, excluding approval latency, Unity/service outages, art procurement,
external playtest scheduling, and corrective findings: shared roster foundations **10-18 days**; five hero
composition slices **17-29 days**; deterministic 3v3 plus URP and its review **10-18 days**; the 8.29
decision and private-online track **20-40 days** pending provider evidence; Phase 9 production presentation
**25-50 days** pending the private playtest and asset pipeline. The shortest path to the next hard
integration outcome, 8.27, is therefore **37-65 focused engineering days** from this re-baseline; the
remaining private pitch through 9.16a is approximately **82-155 days** before corrective contingency.

| Audited range | Recommendation and evidence |
| --- | --- |
| 8.20-8.24 | **Split shared foundations, then keep five hero completions.** The matrix exposes one cross-action schema boundary, one derived-query/value/state family, and one persistent/system family used across heroes; burying them in Arien would make later heroes retroactive dependencies. One complete milestone per hero remains the smallest end-to-end source/progression/player boundary. |
| 8.25 | **Keep.** Six distinct content sets, 108 instances/definitions, component supplies, participant ownership, and payload measurement first meet here; none belongs in an individual hero slice. |
| 8.26 | **Keep.** Six-owner turn flow and director/history projection are one integration boundary after setup and before the repeated-round gate. |
| 8.27 | **Keep as the T3 deterministic 3v3 hard gate.** The matrix increases its consumer breadth but does not change its observable match-level proof. |
| 8.28-8.28a | **Keep.** URP migration and fresh analysis-only readiness review remain independent of rules composition and of each other. |
| 8.29 | **Keep.** Provider/SDK evidence can change before execution and must re-baseline the online track only after measured 3v3/URP evidence. |
| 8.30-8.30c | **Keep the existing split.** Session boundary, privacy/authorization, seat-scoped HUD, and local six-seat gate have distinct mutation and validation surfaces. |
| 8.31-8.34 | **Keep.** Seat establishment, network authorization, synchronization/fault handling, and reconnect/host-loss each close a different trust or recovery boundary. |
| 8.35a-8.35c | **Keep.** Lobby, online HUD adapter, and exact multi-process build rehearsal isolate presentation, adapter, and topology failures before external testers. |
| 8.36-8.36a | **Keep.** The recorded external playtest and its fresh analysis-only review cannot be merged without changing the evidence under review. |

## Shared implementation foundations

### [ ] 8.19a Make card text and pre-action continuations fully resumable

Observable outcome: Printed text may pause before an action begins and inside Before-Attack,
Defense-text, and After-Attack windows while preserving one enclosing typed action and its exact target,
offer, timing, consequences, and projection semantics.

Acceptance checks:

- `attack-target@2`, `set-attack-target@1`, `before-action-trigger@1`, and
  `around-primary-action@1` compose through the existing action/effect frame; choices never move into UI,
  Unity, or a second resolver.
- A pre-action trigger resumes the exact validated offer/version, a Before-Attack branch can move then bind
  its target, and After-Attack/Defense text can pause after the correct p.14 facts without repeating or
  skipping consequences.
- Pre-consequence target position, enclosing action facts, stable node path, completed outcomes, and any
  outer pre-action frame persist explicitly in snapshot schema v25. Schema v24 is either rejected clearly
  or migrated by an explicit byte-stable migration with positive, boundary, malformed, and tamper tests.
- Restore, replay, Undo, restart, stale-choice rejection, projection rebuild, and event/checkpoint order are
  deterministic for every new pause altitude; rejection remains atomic.
- Synthetic source-linked shapes prove an Arien before-Attack move, a Brogan move-then-target Attack, a
  Tigerclaw defender rider, and an Xargatha before/after Movement choice without defining any hero card.

Non-goals: No verified Arien/Brogan/Sabina/Tigerclaw/Xargatha definition, new board operation, production
presentation, or general expression language.

### [!] 8.19b Add the roster's reusable derived-query, value, and state-operation vocabulary

Blocked by: Milestone 8.19a must establish the continuation every chooser-owned result resumes through.

Observable outcome: The roster's shared target-set/pair, geometry, history, presence/count, value,
card-retrieval, and resource operations exist as typed reusable contracts with pure feasibility answers.

Acceptance checks:

- Implement the matrix's `choose-target@2`, `choose-target-set@1`, `choose-target-pair@1`,
  `behind-target-line@1`, charge/through-line candidates, former-target-space, action/round history,
  presence/count, and pre-consequence adjacency facts from authoritative state with canonical ordering.
- Implement `retrieve-card@1`, `gain-resource@1`, and atomic `transfer-resource@1`; candidate sets are
  rederived on answer/restore, debit/credit cannot mint or go negative, and another player's card/resource
  choice uses the exact recorded chooser.
- Implement typed Attack/Range/dynamic-value contribution, minion-defense suppression,
  PrintedPrimary-only Defense restriction, and exact immunity-override declarations without sentinel
  values or action/card names.
- Every capability has definition validation, pure feasibility/execution conformance, positive/negative/
  boundary/failure/ordering tests, and applicable immunity, Counts-As, obstacle, stale-input, and atomicity
  coverage. No new snapshot field is introduced; a discovered need stops the milestone for an explicit
  schema split.
- Synthetic consumers cover at least two heroes for every genuinely shared contract, while contracts used
  by only one printed shape remain narrowly named rather than generalized speculatively.

Non-goals: No verified hero definition, Active-effect/system trigger, removal-prevention source, UI work,
or schema change.

### [!] 8.19c Add the roster's persistent, trigger, and Minion-Battle rule sources

Blocked by: Milestone 8.19b must supply the derived queries and operations these sources consume.

Observable outcome: Trusted definitions can express every roster-wide persistent/triggered rule and the
one isolated Minion-Battle replacement without hero-name control flow.

Acceptance checks:

- Active/continuous sources support multi-activity immunity, `NextTurn`, before-Minion-Battle
  End-of-Round execution, dynamic values, additive effective-type overlays that preserve named intrinsic
  effective types, and after-Push/after-action triggers through p.17 ordering.
- A reusable before-removal-prevention source pauses after Defeat/reward and before removal, pays its typed
  card cost if accepted, retains the Defeat/reward, and publishes `RemovalPrevented`; restore, simultaneous
  sources, radius movement, cancellation, and immediate Lane-Push consequences are covered.
- The isolated `one-man-army-minion-battle@1` handler consumes only typed Minion-Battle facts, supplies
  Heavy strength, replaces its own effective-unit removal with `PushLost`, emits the ordinary Lane-Push
  request against its team, and emits no Defeat/reward/Life consequence. The push still resolves through
  the shared transition; the handler is registered by rule identity and contains no Brogan/display-name switch.
- Source declarations remain trusted catalog content; snapshot v25 stores only stable references and
  runtime instances, and digest/registration changes are deliberately pinned and restoration-tested.
- Synthetic scenarios prove every duration, trigger, cross-team ordering, cancellation, suppression/
  restoration, stale choice, and game-ending boundary needed by the roster matrix.

Non-goals: No verified hero definition, production art, player-facing presentation, second snapshot
revision, or broad Minion-Battle rewrite.

## First complete hero consumers

### [!] 8.20 Complete and verify additional pitch hero slot A - Arien

Blocked by: Milestones 8.19a-8.19c must complete the shared roster foundations.

Observable outcome: Arien the Tidemaster's complete verified set is defined, dealt, resolved through the
ordinary player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested.

Acceptance checks:

- Every Arien face and component maps exactly as the roster matrix records; all Before/After-Attack,
  behind-target, discard-or-Defeat, minion-defense suppression, This-Turn/This-Round restriction/immunity,
  Place, Swap, Repeat, and Living Tsunami paths have source-linked scenarios.
- The four Active effects obey moving-radius applicability, attacker exception, action-start Movement
  limit, end-turn/end-round cancellation, restore, and p.17 order; Living Tsunami is a continuous Ultimate
  and remains separate from those cancellation rules.
- Definitions, source metadata, offer/choice projections, controller ownership, progression, snapshot,
  replay, Undo, restart, and live HUD/board journeys cover all nineteen faces without an Arien-name branch.
- Focused capability consumers and directly affected Wasp/core regressions pass at the applicable tier.

Non-goals: No other new hero, six-player setup, networking, or production art.

### [!] 8.21 Complete and verify additional pitch hero slot B - Brogan

Blocked by: Milestone 8.20 must prove the shared continuation on one complete hero.

Observable outcome: Brogan the Destroyer's complete verified set is defined, dealt, resolved through the
ordinary player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested.

Acceptance checks:

- Every Brogan face/component maps exactly as the roster matrix records; charge/retarget, modal Attack,
  former-target movement, retrieve, direct Move/Push, turn history, conditional resource gain, and all
  Active-effect paths have source-linked scenarios.
- Shield/Bolster/Fortify prove paid optional removal prevention after reward and before removal across
  cancellation, restore, competing sources, minion immunity, and immediate/end-round Lane-Push order.
- One Man Army proves ordinary and Heavy Minion-Battle strength, selected/not-selected removal, PushLost,
  round continuation, snapshot/replay/Undo, and no Defeat/reward/Life side effect through its isolated typed
  handler and no Brogan-name branch.
- Definitions, projection/player journeys, progression, complete-set interaction, and directly affected
  Arien/Wasp/core regressions pass at the applicable tier.

Non-goals: No other new hero, general Minion-Battle redesign, six-player setup, networking, or art.
