# Phase 8 - Completed milestone definitions, second shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-08-28 during milestone 8.25f2a's required warning-threshold ownership review. Milestones 8.11d and 8.11e moved here verbatim after the main Phase 8 shard reached its warning threshold; milestone 8.29a followed during the 2026-08-29 ownership review, and milestones 8.16 through 8.17 followed during milestone 8.30's on the same date. Milestones 8.18a through 8.24 followed on 2026-09-03 during milestone 8.35a1's, placed in sequence after milestone 8.17 so that the five 8.18 slices again sit below the split contract they read "above". Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

### [x] 8.11d Replace piece-target triangles with fitted rings and washes

Blocked by: milestone 8.11c, so every identity-named effect-choice candidate first has the same direct board surface whose shared cue this milestone changes.

Observable outcome: every projected hero, minion, or token target is identified by one static identity-keyed cue: a board-facing ring fitted to its base plus an exact-footprint translucent wash over its visible art, making the target immediate while keeping the art legible and reserving one compatible hover treatment for a future pointer-preview surface.

Acceptance checks:

- One shared `PieceTargetCueViewModel` projection replaces the triangle for heroes, minions, and tokens across Attack targeting, Minion Battle choices, effect choices, correction subjects, Clear, and every other existing consumer. It carries one stable board-object identity, cue state, and typed consequence intent; presentation derives no candidate, allegiance, distance, Range, immunity, outcome, or other legality.
- `Eligible` is one thick ring made from exactly eight separated segments. `Selected` is two complete rings. The reserved future `Hovered` treatment is one brighter solid ring with four short connected outward ticks; this milestone constructs and tests it but does not manufacture hover where the direct-click flow projects none. Shape, thickness, count, and luminance distinguish all three without colour or animation alone.
- `Attack` remains typed distinctly from `Removal`, but both use the same blood red because each declares a hostile consequence; `Affect` uses dark green. These intents describe the invoked operation rather than its guaranteed outcome, so prevention never rewrites the cue. Presentation reads the typed intent and never infers it from target kind, card text, display name, or colour.
- The settled consequence colours are green RGB `27/129/71` and blood red RGB `194/10/24`, over one light-neutral keyline at RGB `248/247/242`. The exact-footprint wash repeats the consequence colour at alpha `0.18` for `Eligible`, `0.23` for `Selected`, and the existing bounded-overlay maximum `0.28` for the reserved `Hovered` treatment. Assistive reading names state and consequence, while geometry keeps the distinction independent of colour.
- The cue measures each current renderer through its local circular contour or local convex base contour and transforms that contour into the board plane, rather than fitting a yaw-inflated world AABB or one fixed hero/minion/token radius. The ring's inner keyline meets that contour with no air gap; the wash fills the same contour just above the actual art; and hover ticks connect to the solid ring. The bounded tint leaves official art and labels legible and remains distinct from selectable-destination hex fills.
- Exactly one retained cue instance, mesh, and renderer follows each projected piece view. It adds no collider or input surface and changes no existing piece collider, selector, or hit area. Accepted or refused commands, pointer exit, selection changes, movement, removal, Defeat/respawn, pool reuse, Undo, restart, snapshot restore, replay, and presentation rebuild clear or reconstruct it from the current projection without stale, duplicated, orphaned, or growing native resources.
- Existing signed Defense-contributor badges, current-card/Active indicators, destination highlights, spawn markers, team treatments, and card-hover surfaces retain their meanings and visual priority. A target ring remains readable against representative light, dark, and highly detailed regions of the official board for both team treatments and every configured unit/token footprint.
- Focused presentation tests cover eligible, selected, cleared, moved, removed, restored, and rebuilt cues on a hero, minion, and token; exact circular and convex fitting; wash height, palette, opacity, blending, sorting, and retained resources; typed Attack/Removal/Affect without name or text inference; the reserved hover construction; destination-highlight coexistence; unchanged pointer surfaces; and absence of the former triangle glyph. Directly affected Play Mode paths cover Attack, Minion Battle, and identity-named effect-choice targeting without requiring a currently unreachable hover transition. T2 validation includes one targeted 2048-by-1152 Game View inspection.

Non-goals: no change to target candidates, legality, selection commands, occupancy, pointer hit areas, cue ownership, board registration, card or HUD presentation, camera behavior, animation, production VFX, or team palette. The static wash is part of the existing target cue, not a new animation/VFX system or a mutation of source art or team colour.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811d-replace-piece-target-triangles-with-fitted-rings-and-washes-completion-evidence).

### [x] 8.11e Make accepted commands and their resulting projections atomic

Blocked by: milestone 8.11d, whose live hero-Attack target defect supplied the concrete failure: authority accepted the target and advanced to Defense, then Defense-preview projection threw and left the public projection and prompt on the already-consumed Attack choice. This correction lands before 8.12 so every later triggered-effect continuation inherits the repaired session boundary rather than another command-specific workaround.

Observable outcome: every configured player input that the match session reports accepted publishes one complete immutable projection of that same committed authority, event history, command history, and version; if construction or validation of the resulting projection faults, the input commits nothing and the previous actionable projection remains public, so authority and the visible prompt can never split.

Acceptance checks:

- One application-session transaction owns the authoritative graphs, whole-match and current-round ordered event streams, accepted-command history, version/checkpoint bookkeeping, and resulting `MatchSessionProjection`. A domain plan is not externally accepted until its next projection has been constructed and validated; on success `MatchCommandResult.Projection` is the same object as `MatchSession.Projection`, carries the incremented version and command exactly once, and describes the state and events the command committed.
- A projection constructor, invariant, or derived-preview fault after a domain plan accepted is reported as `MatchSessionRejectionReason.EngineFault`, with the exception type in the detail, rather than escaping `IMatchSession.Execute` or being relabelled as a rules refusal. Authoritative snapshot, diagnostic snapshot/events, current-round events, accepted-command order/count, version, history availability, checkpoint behavior, and pending-choice identity all remain exactly at the pre-command value, and the same previous immutable projection remains published and usable.
- `MatchSessionTests.AcceptedCommandWhoseProjectionFaults_RollsBackAuthorityEventsHistoryVersionAndCheckpointAndReturnsEngineFault` is the closing regression. It uses a one-shot test seam to let a real command pass domain validation and then fault only while its resulting projection is built; it is observed failing at the pre-fix revision and passing afterward. Retrying the same command after the one-shot fault accepts it exactly once, its Undo returns to the pre-command projection, and a fresh replay reaches the same accepted projection. The seam creates no public constructor, runtime diagnostic mode, build-configuration bypass, or Unity-owned session path.
- Ordinary refused, stale, forged, and programming-fault commands retain their distinct existing results: a clean domain refusal moves nothing, a refusal that mutated is an `AtomicityViolation`, and an exception is an `EngineFault`. None appends a command or events, increments the version, changes a checkpoint, clears a prompt, or raises a successful presenter refresh. A later legal retry remains possible from the returned/current projection.
- The configured input inventory is explicit and exercised through the path a player actually uses, not only by calling authority methods: `DirectorHandsPresenter` plus the tray for card selection/commit confirmation and empty-hand Pass; `RoundActionPresenter` plus the HUD for tied Initiative, action/Hold, Defense-card/Block and Decline, HUD-resident effect options, Fast Travel activation, Clear selection/toggle/cancel/confirm, resolve-without-respawn, end-of-round advance, and system-transition options; `BoardInteractionPresenter` plus the configured pointer selectors for Movement destinations, hero/minion Attack targets, piece- and space-valued effect choices, displaced-minion subject/destination correction, Fast Travel destinations, and hero respawn; `MinionChoicePresenter` plus board/HUD candidate and confirm paths for Minion Battle; and `UndoRestartPresenter` for Undo and restart.
- The authoritative pending-choice transition matrix covers every currently projected player-choice family and every answer shape it offers: `PendingInitiativeChoice`; `PendingMovementChoice` continue and Stop; `PendingAttackTargetChoice` hero and minion targets; `PendingDefenseResponseChoice` numeric Defense, fulfilled Block, unfulfilled Block, and Decline together with `PendingDefensePreview`; `PendingEffectChoice` Select, Deselect, Confirm, Decline, and Stop for both ordinary Skill and Attack/Block card-text continuations; `PendingDisplacedMinionCorrection` subject then destination; `PendingMinionBattleChoice` Select, Deselect, and Confirm; `PendingHeroRespawnChoice` respawn and resolve without action; and `PendingSystemTransitionStep` for the configured Lane-Push respawn/Place and Level-Up upgrade prompts. Resolution envelopes and deterministic requests such as `PendingAttackResolution`, `PendingSkillResolution`, and Lane-Push requests are classified as companions rather than invented as extra player choices.
- `ConfiguredPendingChoiceProjectionTests.EveryConfiguredChoiceAnswerPublishesTheCurrentProjectionOrCommitsNothing` drives that matrix and asserts for each row that the old prompt either advances to the exact next authoritative choice or clears, the previous invocation cannot be submitted again, all newly projected invocations belong to the new authority, and board cues, tray/HUD controls, status text, and diagnostic rows refresh from that same projection. `ConfiguredPendingChoiceProjectionTests.EveryConfiguredChoiceFamilyAndAnswerKindHasATransitionCase` is the completeness guard: a later projected choice family or answer kind must be deliberately added to the matrix rather than silently bypassing it.
- The player-reported regression is retained end to end: a configured hero Attack target whose defender has both a legal Block and nonempty minion Defense contributions is clicked through the real Input System, piece collider, board intent handler, and presenter. The target command is accepted once, `PendingAttackTargetChoice` and its ring clear, the `PendingDefenseChoice` projection property publishes its `PendingDefenseResponseChoice` together with the preview, the status advances from Attack targeting to Defense, the Block remains value-free while numeric Defense choices carry the shared minion contributions, and no engine-fault diagnostic is emitted. `MinimalHexGridPlayModeTests.Scene_HeroAttackTargetClickWithBlockAndMinionDefenseAdvancesToDefenseWithoutEngineFault` owns this check; the pure `ConfiguredDefenseCardTests.ABlockPreview_DoesNotBorrowNearbyMinionContributionsFromNumericDefense` remains the semantic companion.
- Presentation-only inputs stay presentation-only: selecting a viewed hero when no authority choice claims the click, hover/focus cues, opening/cancelling local Clear mode before confirm, card previews, and toggling the diagnostic drawer submit no command and change no authority, event stream, history, or projection version. They may repaint from the current projection but cannot manufacture a success refresh or hide an engine fault.
- Each pending family has live, paused-snapshot restore-and-continue, fresh replay, and retained-prefix Undo evidence at the existing durability boundary; restart returns to the one initial projection with no carried pause. Accepted command identity/order, ordered events, projection after the answer, final authoritative snapshot, and history cardinality agree. No speculative overlap of mutually exclusive pending families is manufactured merely to increase the matrix.
- Projection publication is equally atomic outside ordinary `Execute`. One-shot fault regressions cover a valid `RestoreSnapshot`, retained-prefix Undo reconstruction, restart reconstruction, and final fresh-replay candidate adoption. Each operation reaches its real post-reconstruction projection boundary and faults there; aggregate state, whole-match and round events, accepted history, history availability, version, checkpoint set, and the exact prior public `Projection` all remain unchanged. Retrying after the one-shot fault succeeds once and produces the same projection as the corresponding fault-free operation. These checks close the distinct current gaps where restore mutates history/version before projection and reconstruction removes checkpoints before projection; successful restore-and-continue cases alone are not rollback evidence.
- Fault-free accepted commands still build exactly one resulting public projection, authoritative captures retain their current memoized one-per-state behavior, and private replay candidates retain their existing deferred-intermediate-projection optimization. `MatchSessionDiagnosticCostTests`, `UndoCheckpointReconstructionTests`, `CardAggregateInvarianceTests`, and the deterministic complete-loop oracle remain green, with no session-length growth, duplicate serialization, ghost checkpoint, or adopted replay candidate whose final projection could not be built.
- Schema v24, `DirectorCommandKind`, card/effect definitions, legality, and serialized pending meaning remain byte-identical. `SnapshotCompatibilityCorpusTests` and `CardAggregateInvarianceTests` therefore keep their existing digests; a schema or command-shape requirement is a scope change that stops this milestone rather than being folded in. Reassess `RISK-STATE-002` because projection-fault rollback exercises the pre-image boundary more broadly, but do not claim that separate serialized-pre-image residual retired unless its own demonstrated gap is actually removed.
- This is T3 broad-risk validation: the shared `IMatchSession.Execute` command/event/pending-choice boundary reaches every Edit Mode rules consumer and every configured Play Mode presenter/input path, and its downstream blast radius is not safely bounded by the files changed. Run the focused application/atomicity, all pending-family authority, presenter/HUD, pointer, Block/minion-preview, snapshot/replay/Undo, `CompleteCoreMatchLoopTests`, and `ConfiguredEffectLifetimeGateTests` filters first; run `MinimalHexGridPlayModeTests` for the actual input paths; then run both unfiltered Edit Mode and Play Mode suites at the final revision. No non-batch visual capture or player build is required because this milestone changes publication/recovery behavior, not rendering.

Non-goals: no game-rule, verified-card, candidate, legality, action-offer, consequence, command, event, pending-choice, snapshot-schema, or content-revision change; no card/hero/display-name branch or runtime text parsing; no new prompt, error dialog, control, cue, layout, board, HUD, or other UI redesign; no public fault-injection/configuration surface, broad conversion of all in-place authorities to a new state model, replay/checkpoint redesign, networking work, or build artifact; no speculative combinations of pending states the configured authority cannot reach; and no Active-effect/continuous-trigger execution or other part of milestone 8.12.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-811e-make-accepted-commands-and-their-resulting-projections-atomic-completion-evidence).

### [x] 8.16 Complete the deterministic Wasp mirror integration gate

Observable outcome: A Wasp mirror match can start, play repeated turns/rounds, and end through every victory path with primitive visuals and debug UI.

Acceptance checks:

- Every implemented Wasp rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader source-linked Wasp integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-816-complete-the-deterministic-wasp-mirror-integration-gate-completion-evidence).

### [x] 8.16a Independently review Wasp-driven card-engine extensibility

Blocked by: Milestone 8.16 must pass its source-linked Wasp integration gate.

Observable outcome: A fresh analysis-only review identifies what the first complete verified hero revealed about the generic card/effect architecture before five additional heroes multiply its extension costs.

Acceptance checks:

- The review traces every implemented Wasp capability to its source, typed contract, handler or composition, serialized continuation/state, ordered events, and focused/integration evidence.
- It searches for card-name or hero-name branching, runtime printed-text parsing, untyped universal payloads, Wasp-specific behavior disguised as a generic capability, duplicated patterns that now justify a shared capability, and speculative abstractions supported by only one exceptional card.
- It evaluates effect/query/modifier/continuous-source performance and test maintainability using actual Wasp scenarios rather than synthetic assumptions alone.
- Findings follow the common independent-review evidence contract and distinguish legitimate isolated custom handlers from leakage that would make the next five heroes increasingly expensive.
- Any correction is proposed separately; the review changes no rules, content, runtime implementation, card values, or source interpretation.

Non-goals: No selection, inventory, or implementation of another hero; no Wasp rebalance; no generic framework merely to eliminate every custom handler.

Hard gate: Additional pitch-hero inventory and implementation do not begin until milestone 8.16a and every gate-blocking corrective milestone are reviewed and approved. Production visual work remains blocked until the six-hero 3v3 gate at milestone 8.27, the render-pipeline migration gate at milestone 8.28 with its independent review at 8.28a, and the private online playtest gate at milestone 8.36 with its independent review at 8.36a are reviewed and approved, matching the sibling gate statements.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-816a-independently-review-wasp-driven-card-engine-extensibility-completion-evidence).

**8.16b disposition - closed without implementation, 2026-08-27.** The user-approved roadmap reassessment withdraws the never-scheduled 8.16a corrective bundle rather than marking unperformed work complete. Later evidence changed its boundaries: the permanent Wasp player-path matrix supersedes F-TEST-08's incidental mirror-seed coverage; real Brogan and Sabina declarations exercise `SourceTeamRelation.AlliedWithSource`, while the remaining relation-vocabulary disposition joins the contract correction below; and Xargatha's Cleave now proves a live repeated-Attack exclusion survives restore, though the compatibility corpus still lacks that arm. F-CORE-10's mis-aimed complete-match capture belongs to milestone 8.27, which is the existing official-six-player full-match and scale gate. The surviving F-CARD-10, F-CARD-11, F-CARD-13, F-TEST-07, and residual F-TEST-09 work is re-baselined at milestone 8.25e7. This disposition closes the obsolete proposal ID, not any surviving finding, and changes no code, rule, content, schema, test, or validation evidence.

### [x] 8.16c Answer blocked Lane-Push respawn placement on the board

Inserted 2026-08-23 at the user's direction after the running match exposed a missed acceptance defect from milestones 8.11a and 8.15a. The reserved 8.16b proposal is now closed by the disposition above; 8.25e7 and 8.27 own its surviving work.

Observable outcome: when a non-token obstacle blocks a minion Spawn point during a Lane Push, every equally-nearest empty destination is lit on the board and the owning team answers by clicking one of those spaces; the HUD explains the mandatory placement but presents no destination button.

Acceptance checks:

- A pending `LanePushRespawnPlace` system-transition description projects every currently offered destination into the board interaction model, each paired one-to-one with the continuation's exact typed invocation. Presentation adds, removes, reorders, decodes, or reconstructs neither legality nor command identity.
- Every offered destination uses the existing selectable-space cue and is simultaneously visible before pointer movement. Clicking a lit space forwards only its paired `AnswerSystemTransitionStep` command through the configured board pointer path; an unoffered coordinate, stale projection, forged option, or repeated click executes nothing.
- The Lane Push status and explanation remain in the contextual HUD, but no `PLACE AT ...` control, coordinate control, or other destination-naming button is drawn. Card-faced Level Up options and system-transition director/headless access retain their existing behavior.
- Accepted placement, refusal, Undo, restart, snapshot restore, replay, transition completion, and presentation rebuild clear or reconstruct the destination set from the current immutable projection without stale highlights or duplicate input surfaces.
- `PromptReachabilityConformanceTests.APausedLanePushRespawnPlace_IsAnsweredOnlyOnTheBoard` is observed failing at the pre-fix revision and passing after the correction. A directly affected Play Mode journey reaches a real blocked Spawn-point pause, verifies every offered space is lit, clicks one through the camera pointer ray, and verifies the chosen minion placement and cleared cues; one targeted Game View inspection confirms the HUD contains explanation rather than destination buttons.

Non-goals: no Lane Push, Spawn, Place, obstacle, nearest-space, Tie Breaker, transition-order, snapshot, or replay rule change; no change to the offered destinations or command/wire shape; no redesign of the HUD, board cue palette, camera, director tooling, Level Up presentation, animation, or production VFX.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-816c-answer-blocked-lane-push-respawn-placement-on-the-board-completion-evidence).

### [x] 8.17 Select the five additional pitch heroes

Observable outcome: The user approves five specific heroes that join Wasp as six different heroes divided into three Red and three Blue roster slots for the private pitch.

**Approved 2026-08-23: the rulebook's own recommended first-game line-up, chosen by the user from p.6's setup step 7 over the analysis-recommended alternative.** Red: Wasp the Warmaiden, Xargatha the Changed, Tigerclaw the Cutpurse; Blue: Arien the Tidemaster, Brogan the Destroyer, Sabina the Gunslinger - the exact dashboards the printed step names for a six-player game, so the team slots come from the source rather than from invention. All five newcomers are one-star Challenging, none carries the profile cards' `Tokens` role, and every extraction set is Wasp-shaped: nineteen faces at the same readable resolution, with all thirty-two dashboard backs blank placeholders - the recorded W-01-class open question each hero shard inherits at its 8.18 slice.

Acceptance checks:

- The proposed roster contains no duplicate hero and identifies each hero's intended team slot without treating team assignment as a card-rule branch.
- Implementation-grade card, dashboard, token/marker, miniature/presentation, and applicable official-ruling sources are confirmed available or explicitly recorded as blockers.
- Selection considers effect-family and presentation breadth so the roster demonstrates the reusable architecture without claiming equal implementation cost for every hero.
- The exact five-hero choice receives explicit user approval before inventory begins.

Non-goals: No card transcription, effect implementation, production art, balance adjustment, or seventh pitch hero.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-817-select-the-five-additional-pitch-heroes-completion-evidence).

Milestone 8.18 was split five ways by the 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation): its own acceptance contract says a missing component blocks the affected hero, but one milestone made a single blocked hero hold four others, and the one-shard-per-hero contract already exists. Each slice below applies the same inventory contract to the one hero milestone 8.17 assigns to its slot: every required face and component present and readable; structured data manually checked against source images and applicable official rulings; missing, ambiguous, or conflicting content recorded in that hero's own `Rules/HERO_<NAME>.md` shard per the milestone 6.1y contract, blocking only that hero rather than being guessed, with content affecting more than one hero staying in `RULES_LEDGER.md`; and no implementation from unverified filenames, OCR, display text, or assumed cross-hero similarity.

### [x] 8.18a Inventory and verify pitch hero slot A

Slot-A hero, assigned by 8.17's approved roster: **Arien the Tidemaster** (Blue).

Observable outcome: the slot-A hero's cards, tiers, values, action types, text, item possibilities, Ultimate, tokens/markers, and other hero-owned components are traceable to source material under the shared 8.18 contract above.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-818a-inventory-and-verify-pitch-hero-slot-a-completion-evidence).

### [x] 8.18b Inventory and verify pitch hero slot B

Slot-B hero, assigned by 8.17's approved roster: **Brogan the Destroyer** (Blue).

Observable outcome: the slot-B hero's complete set is source-verified under the shared 8.18 contract above.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-818b-inventory-and-verify-pitch-hero-slot-b-completion-evidence).

### [x] 8.18c Inventory and verify pitch hero slot C

Slot-C hero, assigned by 8.17's approved roster: **Sabina the Gunslinger** (Blue). The extraction's dashboard names her `SABINA THE COMMANDER` instead; the conflict is recorded as her shard's question S-01 and changes neither the roster slot nor the hero.

Observable outcome: the slot-C hero's complete set is source-verified under the shared 8.18 contract above.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-818c-inventory-and-verify-pitch-hero-slot-c-completion-evidence).

### [x] 8.18d Inventory and verify pitch hero slot D

Slot-D hero, assigned by 8.17's approved roster: **Tigerclaw the Cutpurse** (Red).

Observable outcome: the slot-D hero's complete set is source-verified under the shared 8.18 contract above.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-818d-inventory-and-verify-pitch-hero-slot-d-completion-evidence).

### [x] 8.18e Inventory and verify pitch hero slot E

Slot-E hero, assigned by 8.17's approved roster: **Xargatha the Changed** (Red).

Observable outcome: the slot-E hero's complete set is source-verified under the shared 8.18 contract above.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-818e-inventory-and-verify-pitch-hero-slot-e-completion-evidence).

### [x] 8.19 Map the five heroes to capabilities and re-baseline the 3v3 pitch work

Observable outcome: Every verified action and hero-owned component maps to existing or new typed capabilities, and the remaining hero, 3v3 integration, and presentation milestones are re-baselined from evidence.

Acceptance checks:

- A roster-wide matrix distinguishes reused core/Wasp capabilities, reusable refinements, new primitives, and genuinely unique isolated handlers for every verified hero face and component.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and source-linked tests.
- The designated roadmap audit applies the matrix to milestones 8.20-8.36 and Phase 9, recommending justified keep/merge/split/defer/remove decisions and recording an updated remaining-effort range.
- Approved implementation slices are dependency ordered; a shared capability precedes the first hero that needs it, and no hero-name rule branch or runtime text parsing is scheduled.
- Any schema change is explicit and migration-tested rather than hidden inside a hero milestone.

Non-goals: No effect implementation or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-819-map-the-five-heroes-to-capabilities-and-re-baseline-the-3v3-pitch-work-completion-evidence).

The completed audit, effort range, retained-gate decisions, and dependency-ordered shared foundations
8.19a, inserted 8.19a1, and 8.19b-8.19c live in
[PHASE_8_ROSTER_REBASE.md](PHASE_8_ROSTER_REBASE.md).

### [x] 8.22 Complete and verify additional pitch hero slot C - Sabina

Prerequisites satisfied: Milestone 8.21 proved the unique roster system interaction and milestone 8.21a
closed the public-coin presentation gap without enlarging the side rails, so both named blocks are
discharged. S-01 is answered - the user selected the rulebook's setup-step-7 epithet, **Sabina the
Gunslinger**, on 2026-08-25 - so the display label is no longer blocked and this milestone carries it.

Observable outcome: Sabina's complete verified set is defined, dealt, resolved through the ordinary
player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested, with the
approved epithet displayed consistently.

Acceptance checks:

- Every Sabina face/component maps exactly as the roster matrix records; conditional Attack values,
  paired Swap/Heavy override, multi-target Remove, minion direct Move/Repeat, retrieve, discard-or-Defeat,
  and all After-Attack paths have source-linked scenarios.
- Big Sodding Gun modifies only Sabina's Basic Attack and its after-Push escalation triggers only on her
  applied Push of an enemy hero, with exact ordering, restore, repeated action, and no-card Defeat coverage.
- Up-to-two Remove proves chosen-set legality, deterministic ordered application, removal prevention,
  immediate Lane Push/game end after either target, and no Defeat reward.
- The permanent hero player-path gate in `Testing/POLICY.md` has a complete guarded row for every Sabina
  face/component and distinct interaction shape, exercised through real configured player input and its
  resulting visible HUD/board state; definitions, progression, complete-set interaction, and directly
  affected earlier-hero/Wasp/core regressions pass at the applicable tier, and no epithet string drives rules.

Non-goals: No other new hero, six-player setup, networking, or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-822-complete-and-verify-additional-pitch-hero-slot-c---sabina-completion-evidence).

### [x] 8.23 Complete and verify additional pitch hero slot D - Tigerclaw

Prerequisites satisfied: Milestone 8.22 completed the third shared-composition consumer and left the
staged-hero fixture hero-neutral, so the named block was clear.

Observable outcome: Tigerclaw the Cutpurse's complete verified set and Poison component are defined,
dealt, resolved through the ordinary player pipeline, progressed through every printed upgrade/item/
Ultimate stage, and source-tested.

Acceptance checks:

- Every Tigerclaw face/component maps exactly as the roster matrix records; move-through-target Attacks,
  before/after movement, conditional values, Defense riders/restriction, card retrieval, coin transfer,
  Poison attachment, and Repeat paths have source-linked scenarios.
- Poison's one physical supply, Tier-specific -1/-2 Initiative/Attack/Defense source, holder-defeat and
  end-round return, upgrade boundary, restore, and simultaneous modifier order are authoritative and never
  inferred from the marker art.
- Cloak and Daggers repeats every legal Basic action once, excludes the original Attack target, forbids a
  replacement/recursive repeat, and preserves action/checkpoint/progression semantics across restore and
  Undo without a Tigerclaw-name branch.
- The permanent hero player-path gate has a complete guarded row for every Tigerclaw face/component and
  distinct interaction shape, exercised through real configured player input and its resulting visible
  HUD/board state; definitions, progression, complete-set interaction, and directly affected earlier-hero/
  Wasp/core regressions pass at the applicable tier.

Non-goals: No other new hero, six-player setup, networking, or production art.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-823-complete-and-verify-additional-pitch-hero-slot-d---tigerclaw-completion-evidence).

### [x] 8.23a Draw a held marker on a rail that has room for it

Blocked by: Milestone 8.23 must attach the first verified marker.

Observable outcome: A hero holding a rulebook p.19 marker is drawn as holding it, and what that
marker does to that hero is readable from the same surface, so a rule that lowers three of a
hero's values is one the player it is applied to can see - on a hero rail compact enough that the
reading costs no other row its place.

Acceptance checks:

- Every marker a configured hero holds is drawn from the authoritative projection alone - the supply
  it came from, its printed strength, and the value channels it changes - with no hero-name branch,
  no presentation-owned accounting, and no schema change.
- The reading follows the attachment's own lifetime rather than a card's: it appears when the marker
  is given, and p.13's holder Defeat and p.8's end-of-round return remove it.
- Restore, replay, retained-prefix Undo, restart and a presentation rebuild reconstruct it from the
  projection they already preserve, with no stale reading left behind.
- The hero rail is recomposed to hold the reading rather than grown by it: the discard pile shares
  its caption's line as p.16's Ultimate row already does, the coin balance is drawn as its own
  minted disc rather than as a word beside a number, and the rail's width and laid-out height are
  measured before and after rather than asserted. Every existing rail reading - identity, Level,
  items, the current-card marker, the four resolved turns, the pile and the Ultimate - survives the
  recomposition, and the mirror rule and the pile's left-to-right exception are preserved.
- The recomposed rail's measured height is recorded against a third of the hero-rail rect, so
  whether milestone 8.25's three-per-side column fits is a measurement this milestone leaves behind
  rather than a claim. This milestone deals no additional hero and lays out no second rail.
- The permanent hero player-path gate gains its marker-reading component shape, and milestone 8.23's
  declared debt for it - recorded in Tigerclaw's matrix because this build drew no held marker
  anywhere - is closed rather than restated.

Non-goals: No new marker content, no change to what a marker does, no six-player setup, and no
imported production art - a coin drawn from the panel's own vocabulary is composition rather than
art, and an imported sprite is out of scope.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-823a-draw-a-held-marker-on-a-rail-that-has-room-for-it-completion-evidence).

### [x] 8.24 Complete and verify additional pitch hero slot E - Xargatha

Blocked by: Milestone 8.23 must complete the marker/general-action Repeat consumer.

Observable outcome: Xargatha the Changed's complete verified set is defined, dealt, resolved through the
ordinary player pipeline, progressed through every printed upgrade/item/Ultimate stage, and source-tested.

Acceptance checks:

- Every Xargatha face/component maps exactly as the roster matrix records; dynamic Attack/Range,
  conditional target movement, typed Repeat targets, card retrieval, before/after primary Movement,
  Heavy override, and End-of-Round Defeat paths have source-linked scenarios.
- Next-Turn petrification preserves Hero and adds Terrain as effective types, prevents only Movement
  actions, follows Xargatha's moving radius, respects intrinsic-representation exceptions, and restores/
  cancels in exact p.17 order without materializing a statue component.
- Constrict/Final Embrace run before Minion Battle and consume immediate Lane Push/game end correctly;
  Metamorphosis recalculates Initiative/Movement from presence at each owning query and reuses Move Through.
- The permanent hero player-path gate has a complete guarded row for every Xargatha face/component and
  distinct interaction shape, exercised through real configured player input and its resulting visible
  HUD/board state; definitions, progression, complete-set interaction, and directly affected roster/Wasp/core
  regressions pass at the applicable tier without an Xargatha-name branch.

Non-goals: No six-player setup, networking, production art, alternate miniature, or statue component.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-824-complete-and-verify-additional-pitch-hero-slot-e---xargatha-completion-evidence).

### [x] 8.29a Recompose and contain the side hero trays

Approved by the user on 2026-08-29 as the bounded UI correction before transport work resumes.

Observable outcome: The six side hero trays become contained, artistic character plaques whose hero portrait and written name lead the hierarchy while every existing match fact and interaction remains available.

Acceptance checks:

- Each team column uses a wider 264-reference-unit preferred plaque at supported desktop layouts, but dynamically yields before the opposite column can meet it; both columns together remain under half the panel so the board stays dominant.
- Every rendered plaque and readable descendant stays inside its assigned side column and above the card tray at the 2560x1440 reference Game View and the supported short desktop layout, with no horizontal protrusion or clipped identity.
- The official hero-token face is presented in a portrait disc at least 48 reference units across, and the full written hero name is a bold, high-contrast label at least 15 reference units tall.
- A layered identity plate, team-coloured medallion and edge, shaped outer frame, and quieter secondary compartments replace the flat box treatment without importing new art or making colour the only carrier of identity.
- Level, coins, item bonuses, held markers, Ultimate, current card, four resolved-turn slots, discard cards, tooltips, hover previews, mirroring, selection, and pointer blocking retain their existing projected meanings and interactions.
- The focused Edit Mode presentation/scaling checks and focused Play Mode six-rail geometry/input checks pass, followed by targeted non-batch Game View inspection at 2560x1440.

Closing check: `HeroRailPortraitTests.TheSixHeroPlaques_MakePortraitAndNameThePrimaryIdentity` is recorded failing on the pre-fix flat 30-unit-disc/12-point-name rail and passing after the recomposition; the live six-rail Play Mode check closes containment against each assigned column and the tray.

Non-goals: No rule, match-state, projection, serialization, input-command, networking, board, card-face, tray, prompt-flow, animation, or imported-art change; no broader production-HUD or accessibility suite.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829a-recompose-and-contain-the-side-hero-trays-completion-evidence).
