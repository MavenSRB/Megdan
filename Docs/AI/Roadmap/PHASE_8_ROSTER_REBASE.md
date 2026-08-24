# Phase 8 - Six-Hero Roster Re-baseline and Shared Foundations

Canonical Phase 8 shard for milestone 8.19's audit and shared foundation milestones 8.19a, inserted
8.19a1, 8.19b-8.19c, and inserted 8.19c1. Return to [PHASE_8.md](PHASE_8.md).

## Milestone 8.19 audit outcome

Keep the deterministic 3v3, URP, private-online, and independent-review gates at 8.25-8.36a; split the
hero work at the three genuinely shared risk boundaries below, then keep one complete composition
milestone per hero. The roster map is
[Rules/ROSTER_CAPABILITY_MATRIX.md](../Rules/ROSTER_CAPABILITY_MATRIX.md). It initially found one explicit
schema change, no runtime-text parser, no hero-name branch, and one isolated typed system handler: One Man
Army's Minion-Battle replacement. Preparing 8.19b then demonstrated that its current-round hero-Defeat
condition needs a second explicit schema boundary; user-approved milestone 8.19a1 isolates it. Executing
8.19c then demonstrated the same thing a third time: three of its persistent sources stop the game on a
player decision at an altitude schema v26 cannot hold - before a Defeat's removal, at the end-of-round
moment before the Minion Battle, and immediately after an applied Push - while the block's own non-goals
forbid a second snapshot revision. User-approved milestone 8.19c1, inserted 2026-08-24, isolates those
three; 8.19c keeps the sources that add no persisted state. The dependency-safe route is 8.19a, 8.19a1,
8.19b-8.19c, 8.19c1, Arien, Brogan, Sabina, Tigerclaw, Xargatha, then the unchanged six-player setup and
integration gates.

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

### [x] 8.19a Make card text and pre-action continuations fully resumable

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

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819a-make-card-text-and-pre-action-continuations-fully-resumable-completion-evidence).

### [x] 8.19a1 Persist the roster's typed turn/round rules history in schema v26

Observable outcome: Rules authority can answer whether a named hero played a printed-primary Attack card
in a named turn and whether any hero was defeated in the current round, with the latter surviving
snapshot restore, replay, Undo, and restart under explicit schema v26 semantics.

Acceptance checks:

- `target-played-attack-this-turn@1`'s foundation derives from the exact round/turn slot's committed card
  identity and that definition's `PrintedPrimary` Attack, not from which action the hero performed. It
  remains true when that hero Held, performed a Secondary action, was defeated before acting, or has not
  acted yet; a Pass, non-Attack primary, or different turn is false.
- Every completed p.13 hero Defeat appends its opaque outcome ID and typed defeated `HeroId` exactly once
  through the shared ordinary/effect-invoked consequence seam. A defended/refused/incomplete path appends
  nothing, a prevented removal remains a completed Defeat, and the record clears only when the next round
  begins.
- Snapshot schema v26 persists the ordered current-round record; v25 is rejected because empty cannot be
  recovered safely after a Defeat. Restore rejects unknown participants, duplicate outcomes, and outcomes
  absent from the cumulative awarded-outcome ledger; the compatibility corpus pins both empty and
  non-empty arms and round-trips byte-identically.
- Live, restored, replayed, retained-prefix Undo, and restarted matches converge on the same answer. The
  condition never queries diagnostic events, parses a resolution ID, or infers round scope from cumulative
  coins, Team Life, or board occupancy.
- Focused history/Defeat/round-boundary tests, both schema digest fixtures, enum decoding, aggregate
  atomicity, direct restore/replay/Undo regressions, and standing snapshot-cost guards pass at T2. No
  Unity-facing path changes, so Play Mode remains unaffected.

Non-goals: No registered roster capability handler or synthetic consumer, resource/target/value operation,
verified hero definition, removal-prevention source, presentation, scene, or general history language.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819a1-persist-the-rosters-typed-turnround-rules-history-in-schema-v26-completion-evidence).

### [x] 8.19b Add the roster's reusable derived-query, value, and state-operation vocabulary

Blocked by: Milestone 8.19a1 must supply the typed played-card and current-round hero-Defeat facts this
vocabulary consumes.

Observable outcome: The roster's shared target-set/pair, geometry, history, presence/count, value,
card-retrieval, and resource operations exist as typed reusable contracts with pure feasibility answers.

Acceptance checks:

- Implement the matrix's `choose-target@2`, `choose-target-set@1`, `choose-target-pair@1`,
  `behind-target-line@1`, charge/through-line candidates, former-target-space, the 8.19a1 played-card and
  hero-Defeat history consumers, presence/count, and pre-consequence adjacency facts from authoritative
  state with canonical ordering.
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

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819b-add-the-rosters-reusable-derived-query-value-and-state-operation-vocabulary-completion-evidence).

### [x] 8.19c Add the roster's persistent rule sources and the Minion-Battle replacement

Blocked by: Milestone 8.19b must supply the derived queries and operations these sources consume.

Observable outcome: Trusted definitions can express every roster-wide persistent rule that adds no
persisted state, and the one isolated Minion-Battle replacement, without hero-name control flow.

Acceptance checks:

- Active/continuous sources support multi-activity immunity qualified by the actors it protects against,
  `NextTurn`, dynamic values recounted at every derivation of the live set, value modifiers qualified to
  the action class they name, and additive effective-type overlays that preserve named intrinsic
  effective types - all ordered by p.17 and ended by their own source's cancellation.
- `after-action-trigger@2` reaches every completed basic action of its controller's, while the existing
  basic-Skill condition keeps its narrower reading and no card's trigger widens by omission.
- Every declarable value channel and effective type has a production consumer or a recorded reason, and
  each value channel the roster's sources raise is composed by exactly one query that both the live
  resolution and its re-derivations ask.
- The isolated `one-man-army-minion-battle@1` handler consumes only typed Minion-Battle facts, supplies
  Heavy strength, replaces its own effective-unit removal with `PushLost`, emits the ordinary Lane-Push
  request against its team, and emits no Defeat/reward/Life consequence. The push still resolves through
  the shared transition; the handler is registered by rule identity and contains no Brogan/display-name switch.
- Source declarations remain trusted catalog content; snapshot v26 gains no field, and any digest or
  registration change is deliberately pinned and restoration-tested.
- Synthetic scenarios prove every duration, cross-team ordering, cancellation, suppression/restoration
  and applicability boundary these sources need, over at least two hero-shaped consumers where the
  contract is genuinely shared.

Non-goals: No verified hero definition, production art, player-facing presentation, snapshot revision,
broad Minion-Battle rewrite, or the three pausing sources milestone 8.19c1 owns.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819c-add-the-rosters-persistent-rule-sources-and-the-minion-battle-replacement-completion-evidence).

### [x] 8.19c1 Add the roster's pausing rule sources in schema v27

Blocked by: Milestone 8.19c must supply the persistent-source vocabulary and the Minion-Battle
replacement these three sources stand beside.

Observable outcome: A rule source can stop the game for a decision at three altitudes the engine cannot
currently hold - before a Defeat's removal, at the end-of-round moment before the Minion Battle, and
immediately after an applied Push - and every one of them restores, replays, and resumes exactly.

Acceptance checks:

- A reusable before-removal-prevention source pauses after Defeat/reward and before removal, pays its typed
  card cost if accepted, retains the Defeat/reward, and publishes `RemovalPrevented`; restore, simultaneous
  sources, radius movement, cancellation, and immediate Lane-Push consequences are covered, and both the
  ordinary p.14 Defeat and the effect-invoked one honour it or neither does.
- An Active-effect source declaring `EndOfRoundBeforeMinionBattle` executes before the battle is planned,
  may pause on its own chooser, and resumes into the same battle; the round's persisted cursor, its
  stage outcomes and its completed-round record are unchanged in shape.
- An `after-push-trigger@1` source runs after a Push has applied to a named object, may pause, and returns
  the enclosing effect to the exact node it left; a Push that moved nothing reaches no trigger.
- Snapshot schema v27 persists each pause explicitly; v26 is rejected clearly because a stopped decision
  cannot be recovered from a payload that never held it. Positive, boundary, malformed, and tamper cases
  are pinned, the compatibility corpus is renamed and re-measured, and the byte delta is measured at the
  pre-change revision.
- Restore, replay, Undo, restart, stale-choice rejection, projection rebuild, and event/checkpoint order
  are deterministic at all three altitudes; rejection remains atomic.

Non-goals: No verified hero definition, production art, player-facing presentation, general expression
language, or new derived query, value, or state operation.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819c1-add-the-rosters-pausing-rule-sources-in-schema-v27-completion-evidence).

## First complete hero consumers

### [x] 8.20 Complete and verify additional pitch hero slot A - Arien

Prerequisites satisfied: Milestones 8.19a, 8.19a1, 8.19b-8.19c, and 8.19c1 completed the shared roster
foundations.

Observable outcome: Arien the Tidemaster's complete verified set is defined, dealt, resolved through the
ordinary player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested;
the staged one-hero-per-team match replaces its Blue mirrored Wasp with Arien and stands him on the map as
the official token at the configured Blue hero spawn.

Acceptance checks:

- Every Arien face and component maps exactly as the roster matrix records; all Before/After-Attack,
  behind-target, discard-or-Defeat, minion-defense suppression, This-Turn/This-Round restriction/immunity,
  Place, Swap, Repeat, and Living Tsunami paths have source-linked scenarios.
- The four Active effects obey moving-radius applicability, attacker exception, action-start Movement
  limit, end-turn/end-round cancellation, restore, and p.17 order; Living Tsunami is a continuous Ultimate
  and remains separate from those cancellation rules.
- The private-source preparation route produces Arien's official circular Blue hero token under the same
  ignored-resource, content-identity, team-rim, and generated-prism fallback contract as Wasp's token. The
  staged configuration binds that content identity to the Blue participant and places Arien at the
  configured Blue hero spawn, so the scene a player loads composes him there.
- Focused capability consumers and directly affected Wasp/core regressions pass at the applicable tier.

Non-goals: No other new hero, six-player setup, networking, or production art. This staged Arien spawn does
not add a second Blue hero or retire the one-hero-per-team development deviation; milestone 8.25 still owns
the official six-player configuration and all six official hero spawn assignments. The permanent hero
player-path gate is milestone 8.20a's, not this one's.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-820-complete-and-verify-additional-pitch-hero-slot-a---arien-completion-evidence).

### [x] 8.20a Pass the permanent hero player-path gate for Arien

Prerequisite satisfied: Milestone 8.20 defined, dealt, resolved and stood Arien on the map.

Observable outcome: Arien passes the permanent hero player-path completion gate in `Testing/POLICY.md`, so
every printed face and the hero piece itself are proved through the input route a player actually uses.

Acceptance checks:

- The gate's mechanical completeness matrix has a guarded row for every Arien face/component and every
  distinct interaction shape, and fails when one is absent. Its configured journeys use real panel-picked
  pointer events or camera rays from card commit through the resulting visible HUD/board state, with no
  Arien-name branch and no direct presenter or session submission standing in for player input.
- The staged hero token is proved as the visible, selectable, movable, defeatable, respawnable board piece:
  focused Edit Mode and Play Mode journeys cover initial spawn, pointer selection, Movement, Defeat/respawn,
  snapshot restore, replay, Undo, restart, and presentation rebuild without stale or duplicate hero views.
- At least one pausing path first made reachable by Arien continues after snapshot restore and retained-prefix
  Undo through the same visible player surface, and one targeted non-batch Game View inspection covers the
  visible additions at the validated viewport.

Non-goals: No new Arien content, no other hero, no six-player setup, networking, or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-820a-pass-the-permanent-hero-player-path-gate-for-arien-completion-evidence).

### [x] 8.20b Pass the permanent hero player-path gate for Wasp

Prerequisite satisfied: Milestone 8.20a established the permanent gate's guarded matrix and real-input
evidence shape on the staged Wasp-versus-Arien configuration. `HeroPlayerPathMatrix` is hero-neutral
machinery - the shape derivation, the required component shapes and the guard assertions - so Wasp's half is
a declared table and a fixture calling it, not a second copy of the rules.

Observable outcome: Wasp passes the same permanent hero player-path completion gate as Arien, with no
grandfathered exception for being the first verified hero; every printed face, hero-owned component and the
hero piece itself are proved through the input route a player actually uses.

Acceptance checks:

- The gate's mechanical completeness matrix has a guarded row for every Wasp face/component and every
  distinct interaction shape, and fails when one is absent. Existing Wasp player journeys count only where
  they start from a configured saved scene, use real panel-picked pointer events or camera rays, and prove the
  resulting visible HUD/board state; direct presenter or session submission cannot fill a missing row.
- Source-backed engine scenarios and the retained real-input journeys together cover every offered Primary
  and Secondary action shape, card/minion/hero/token/space/integer and confirm/decline choice as applicable,
  progression/items/High Voltage, and the official token's initial spawn, selection, Movement, Defeat/
  respawn, snapshot restore, replay, Undo, restart and presentation rebuild without stale or duplicate views.
- At least one Wasp-specific pausing card-text or Ultimate path continues after snapshot restore and
  retained-prefix Undo through the same visible player surface. Every submitted decision advances or clears
  its old prompt/cues, accepts exactly one current invocation, renders the resulting projection, rejects a
  stale repeat input, and emits no `EngineFault`.
- One targeted non-batch Game View inspection covers the complete Wasp player surface at the validated
  viewport. Any acceptance defect the gate demonstrates is corrected and its focused regression retained;
  existing green journeys are reused rather than rewritten merely to give the milestone new code.

Non-goals: No new Wasp face or component, rule reinterpretation, balance change, other hero, six-player
setup, networking, or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-820b-pass-the-permanent-hero-player-path-gate-for-wasp-completion-evidence).

### [x] 8.20c Standardize every hero card footprint

Prerequisite satisfied: Milestone 8.20b completed the permanent player-path gate for both currently
configured heroes, providing the real Wasp/Arien surfaces and journeys against which this shared
presentation correction is proved.

Observable outcome: Every complete hero-card face has one shared fixed outer width and height, independent
of hero, printed content, current hand, interaction state, or the surface displaying it; Wasp and Arien no
longer resize the tray or each other, and the permanent hero gate prevents every later hero from reintroducing
content-sized cards.

Acceptance checks:

- One shared card-footprint contract supplies the exact width and height used by the ordinary tray, Defense
  responses, card-valued effect and discard choices, Level Up/upgrade choices, complete-face previews, and
  any later full-card surface. Padding around a face may differ by context, but its outer face dimensions do
  not; title, values, action, rules text, and history wrap or fit inside that footprint without changing it.
- A focused regression demonstrates the current failure before the correction and passes afterward: switching
  between Wasp and Arien through the configured map pointer keeps every resolved card width and height exact,
  does not resize or reflow the tray/board viewport, and selection, active, unavailable, hover, and focus
  treatments do not alter the footprint.
- Every complete Wasp and Arien face remains legible without clipped, overlapped, or truncated meaningful
  content at the validated desktop viewport. At a short supported desktop viewport, the containing region's
  bounded scrolling keeps every card/control reachable rather than resizing individual cards.
- The permanent hero player-path completion gate mechanically checks the shared footprint and complete-face
  fit for every verified face of every newly configured hero, including heroes outside the mapped pitch
  roster, so content-driven resizing or overflow blocks that hero's completion.
- T2 focused Edit Mode and real-input Play Mode coverage proves the shared layout and both configured heroes;
  one targeted non-batch Game View inspection covers Wasp and Arien at the validated viewport and the bounded
  short-viewport fallback.

Non-goals: No card wording, rules, balance, hero content, production art, broad HUD redesign, responsive
card scaling, six-player setup, or networking.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-820c-standardize-every-hero-card-footprint-completion-evidence).

### [x] 8.20d Name every standing decision in the status line

Prerequisite satisfied: Milestones 8.20a and 8.20b drove both configured heroes through the real player
path, which is what surfaced this: milestone 8.20b's Wasp journey observed a discard chooser presented as
though it were an action.

Observable outcome: Whatever the match is currently asking, the status line says which decision it is and
the region holding its controls is labelled for that decision, so no pause is presented as though the
player were choosing an action.

Acceptance checks:

- The status-line derivation names the standing effect choice. Today it runs two phase checks and six
  pending-family branches - Initiative, Defense, Respawn, Attack target, Movement, displaced-minion
  correction - and has no `PendingEffectChoice` branch at all, so every effect choice since milestone
  8.15 falls through to "Initiative N: choose an action or End Turn" while its controls sit under a row
  headed `ACTIONS`. The cause is a pending family the derivation never knew about, not a generic
  heading, and the fix is that family plus the heading the controls land under.
- The words come from the projected pause, as every other prompt's do: presentation composes them from
  typed facts and never decodes an option identity or a resolution ID to decide what to say.
- A conformance rule fails when a projected pending-decision family has no status derivation, so a
  seventh family cannot arrive unnamed the way the sixth did. It is the rule rather than the fix that
  keeps this closed: milestone 8.21's removal-prevention pause would otherwise inherit exactly this
  defect as the next unnamed family, and its own acceptance checks specify that prompt's contents
  without specifying its label.
- Every effect-choice shape the configured content can reach is covered - p.10's discard chooser, the
  printed-number choice, the card-valued option forms, and the confirm-or-decline `may` - at the
  applicable tier, with one targeted non-batch inspection showing the corrected wording and heading.
- No pause loses an affordance it had: the controls, their order, their invocations, and the refusal of
  an option the prompt did not project are unchanged, and the milestone 5.1c bound still holds at the
  validated and short viewports.

Non-goals: No new decision family, rules change, projection field beyond what naming requires, card
footprint work, broad HUD redesign, hero content, six-player setup, networking, or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-820d-name-every-standing-decision-in-the-status-line-completion-evidence).

### [ ] 8.21 Complete and verify additional pitch hero slot B - Brogan

Prerequisite satisfied: Milestone 8.20d named the standing effect choice in the status line and gave its
controls a heading of their own - discharged - so this milestone's removal-prevention prompt arrives as a
family the status derivation's own conformance rule requires to be named rather than inheriting that defect
by construction. Milestone 8.20c established the standard card footprint the gate now measures every newly
configured hero's faces against, so Brogan's set must fit it without resizing or clipping; milestones 8.20
and 8.20a proved the shared continuation and player-path gate on Arien, and milestone 8.20b closed the same
permanent gate for Wasp with no grandfathered exception.

Observable outcome: Brogan the Destroyer's complete verified set is defined, dealt, resolved through the
ordinary player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested.

Acceptance checks:

- Every Brogan face/component maps exactly as the roster matrix records; charge/retarget, modal Attack,
  former-target movement, retrieve, direct Move/Push, turn history, conditional resource gain, and all
  Active-effect paths have source-linked scenarios.
- Shield/Bolster/Fortify prove paid optional removal prevention after reward and before removal across
  cancellation, restore, competing sources, minion immunity, and immediate/end-round Lane-Push order.
- That decision reaches the player through the one immutable projection and the one contextual region,
  because Brogan's cards are the first content that can open it. The standing decision and its currently
  affordable options are projected fields, the pause mints the typed invocation each option is answered
  with as every other pending family does, and presentation composes its words from those facts and
  refuses an option it did not project. Each payable card is drawn as its whole printed face by the
  tray's shared standalone builder at the one fixed width, per the user's milestone 8.15a direction, with
  the card's name in hover and assistive reading; the minion the price would keep is cued on the board
  rather than named in the prompt. Declining is always drawn. While it stands the tray is hidden, no End
  Turn or end-of-round advance is drawn beside it, and the milestone 5.1c bound holds at the validated
  and short viewports.
- A prompt-reachability case pins that every offer the authority makes here produces exactly one control,
  and a live journey answers the decision by pointer at both altitudes it can be reached from - rulebook
  p.14's Attack and an effect-invoked Defeat. Milestone 8.19c1 left this deliberately unbuilt under its
  presentation non-goal and recorded that no configured content could reach the pause; these two checks
  are what keep that true, because `HasPendingResolution` refuses every other turn command while the
  decision stands. That journey also Undoes across the answered decision, which is the one thing
  milestone 8.19c1's own coverage could not reach: the answer is an ordinary recorded command and
  `DirectorCommand` already compares its payload, so retained-prefix Undo follows from the general
  machinery rather than from a measurement until a match can actually open the pause.
- One Man Army proves ordinary and Heavy Minion-Battle strength, selected/not-selected removal, PushLost,
  round continuation, snapshot/replay/Undo, and no Defeat/reward/Life side effect through its isolated typed
  handler and no Brogan-name branch.
- The permanent hero player-path gate has a complete guarded row for every Brogan face/component and distinct
  interaction shape, exercised through real configured player input and its resulting visible HUD/board state;
  definitions, progression, complete-set interaction, and directly affected Arien/Wasp/core regressions pass
  at the applicable tier.

Non-goals: No other new hero, general Minion-Battle redesign, six-player setup, networking, or art.
