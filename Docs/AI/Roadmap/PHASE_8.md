# Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

## Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work composes proven core actions, keywords, effect operations, queries, modifiers, control flow, and system transitions; it does not defer their first general implementation until a card requires them. The mirrored Wasp setup identities introduced earlier remain an intermediate configuration for the first complete hero gate, not the final pitch roster. After that gate, five additional user-approved heroes enter through source inventory and capability-delta mapping before the final six-distinct-hero 3v3 integration. Once the complete primitive match and URP baseline are proven, a separate private-online track moves the same authority behind a host/session boundary and reaches an invite-only six-human internet playtest before production presentation begins.

The completed definitions of milestones **8.1** through **8.10** - Wasp's source inventory and capability matrix, her verified definitions and both dealt hands, the general card-text seam and the two tray surfaces built on it, her five telekinetic Skills, the declared modifier vocabulary's consumers, the current-card rail, her two kinetic Skills and two basic cards, the tray-faced upgrade grid, the refused empty Clear, and her five printed Attacks - moved verbatim to [PHASE_8_COMPLETED.md](PHASE_8_COMPLETED.md), 8.1 through 8.7a on 2026-08-22 during milestone 8.8a's required warning-threshold ownership review and 8.8 through 8.10 on the same date during milestone 8.11a's; milestones **8.11** through **8.11c** - her Defense cards, the official map underlay and its interaction overlay, the verified card-table visuals, and identity-named effect-choice candidates answered on the board - followed on 2026-08-26 during milestone 8.25a's audit, when two proposed corrective blocks took this shard past its hard limit; and milestones **8.12** through **8.15a** - the Active-effect and continuous trigger windows, High Voltage, her progression and Ultimate unlock, her completed card behaviour and the faced card choices - followed on 2026-08-27 during milestone 8.25d's. Milestones **8.11d** and **8.11e** moved verbatim to [PHASE_8_COMPLETED_2.md](PHASE_8_COMPLETED_2.md) on 2026-08-28 during milestone 8.25f2a's warning-threshold review. They remain active authority under the same `Roadmap/` threshold; this is sharding, not archiving, and their per-milestone evidence is still addressed by SHA from the [Phase 8 archive](../Archive/Phase-Handoffs/PHASE_8.md). This shard keeps 8.16 onward, which is what the next milestone reads against.


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

### [x] 8.25 Replace the staged 1v1 setup with the official six-player Quick Game configuration

Observable outcome: Configuration data places three different Red heroes and three different Blue heroes with their own hands and components using the official six-player Quick Game baseline.

Acceptance checks:

- All six stable player/hero identities, ownership relationships, hero spawn positions, hands, dashboards, and hero-owned components resolve without assuming one hero per team.
- Every authoritative turn slot contains exactly six ownership-validated assignments—one for each configured `HeroId`—and snapshot round-trip preserves their deterministic order and committed-card-or-empty-hand-Pass facts; controlling `PlayerId` and team resolve through the participant graph.
- Setup uses three Wave counters, five Life counters per team, the normal Tie Breaker flip, and the verified official board/minion setup.
- Wasp appears once; all six heroes are different and assigned to exactly one team/player.
- DEV-001 and DEV-002 are marked retired; only approved director/debug deviations remain.
- Snapshots round-trip the complete six-player setup deterministically.
- `RISK-SCALE-001`'s payload half is measured rather than computed: the six-player decoded payload is recorded against the staged 1v1 reference of 6,633 bytes and compared with the standing thresholds in `Testing/FUTURE_GATES.md`. This milestone is the first at which 108 card instances and 108 definitions exist, and the risk's own figure for them is arithmetic on measured unit costs rather than a measurement.
- The player-build smoke gate's pinned `heroes=2 cards=36` markers move to the six-player values in `Testing/FUTURE_GATES.md` in this milestone - the marker line this configuration breaks now has an owner, per the 7.9a documentation correction the 8.2 re-baseline applied.

Non-goals: No networking, hidden per-player devices, AI, matchmaking, or balance changes. No reassessment of `RISK-SCALE-001`'s ordering half, which needs a round boundary this milestone does not reach.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825-replace-the-staged-1v1-setup-with-the-official-six-player-quick-game-configuration-completion-evidence).

### [x] 8.25a Answer an after-action repeat that outlives its actor's Initiative

Proposed by milestone 8.25's independent review, which found it with an offline `DeterministicMatchLoop` run rather than in an active suite. It is a rules/timing correction to milestone 8.23's after-action repeat, is reachable on a board that shipped at 8.23, and 8.25's own acceptance checks name no match loop, so it is scheduled separately rather than folded in.

Observable outcome: a decision the projection offers is answerable. A pending card-action resolution whose actor is not the hero Initiative has currently selected is either not offered, or accepted when answered.

Acceptance checks:

- The demonstrated case is a regression fixture before it is a fix: on the staged Wasp-versus-Tigerclaw board, `DeterministicMatchLoop.Play(session, seed: 1, 4000)` reaches Tigerclaw's Cloak and Daggers repeating Blend Into Shadows and is refused `TurnCommandRejected (SkillCommandRejected)` at command 1047 while `SelectedActor` names the other hero; the same shape appears as a Movement refusal on the official six-player board. Both are observed failing at the pre-fix revision and passing after.
- Every command the projection offers for a pending resolution is accepted by the rules, or the projection does not offer it. Stated as a property over a seeded sweep rather than over one seed.
- `TurnResolutionAuthority` no longer discards the backend's `CardActionResolutionRejectionReason`: a refused card-action command reports the reason its backend gave, because the refusal above took a per-destination retry to diagnose.
- Snapshot restore, replay and retained-prefix Undo reproduce the corrected sequence; the compatibility corpus records whether the schema moved.

Non-goals: No change to which abilities may repeat, to rulebook p.10's repeat rule, or to Initiative order. No six-hero match-loop gate, which milestone 8.27 owns.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825a-answer-an-after-action-repeat-that-outlives-its-actors-initiative-completion-evidence).

### [x] 8.25b Name the configuration types after what they now compose

Proposed by milestone 8.25's independent review. `PlaceholderOneVsOneTestConfiguration`, `PlaceholderOneVsOneParticipants` and `PlaceholderConfigurationRestoreState` are neither placeholders nor one-versus-one since 8.25: the first composes rulebook p.5's six official seats and is the production configuration a player loads. Renaming them is mechanical and touches roughly 250 call sites, so it was deliberately not bundled into a milestone that changed what they mean.

Observable outcome: every configuration type is named after the thing it is, and the temporary-structure register carries no row for a structure that is no longer temporary.

Acceptance checks:

- The three types are renamed with their `.cs` and `.cs.meta` files moved together so Unity GUIDs survive; no behavior, content, digest or schema changes, and the compatibility corpus is byte-identical.
- The staged one-hero-per-team arms keep names that say they are staged fixtures.
- `TECHNICAL_RISKS.md`'s temporary-structure register retires the row these types carry, or restates what is still temporary about them with a live owner.
- Complete unfiltered Edit Mode and Play Mode suites pass, because a rename of this breadth cannot be shown affected-only.

Non-goals: No behavior change, no content change, no new configuration, and no change to the staged boards themselves.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825b-name-the-configuration-types-after-what-they-now-compose-completion-evidence).

### [x] 8.25c Scale the HUD with the viewport and identify each rail with its hero portrait

Proposed at the user's direction on 2026-08-26, after milestone 8.25's six-player HUD was seen on a 2560-wide screen, and amended at the user's direction the same day to replace each rail header's circled initial with that hero's picture while the rail layout is already being re-derived. `ConfiguredMatchHudRenderer.Build` sets `PanelScaleMode.ConstantPixelSize` with `scale = 1`, so every layout number is a literal device pixel: the rail is 192 and a card 186x420 on a 1280-wide screen and on a 2560-wide one alike, and the HUD is half its designed share of the second. The committed panel-settings asset already carries a `1200x800` reference resolution, inert under that mode.

**Most of the layout pressure milestone 8.25 fought is a symptom of this.** Three rails a side not stacking, the tray band being too narrow for rulebook p.6's five-card row below 1392 wide, and the 230-line clip 8.25 accepted where the tray covers the third rail are all one fixed-pixel layout serving viewports from 778 to 2560 wide. A scaled panel makes each region the same fraction of every screen, so those three either dissolve or change shape; this milestone re-derives them rather than assuming either.

Observable outcome: the player-facing HUD occupies the same share of the screen at every supported resolution, every pointer decision it makes stays correct at a panel scale other than 1, and each hero rail identifies its hero with a recognizable hero-specific portrait instead of a letter inside a circle.

Acceptance checks:

- **The pointer boundary is converted before anything else, and its check comes first.** `ConfiguredMatchHudRenderer.BlocksPointerAt` takes a screen position, flips it with `Screen.height - y`, and compares it against rects computed from `Screen.width`/`Screen.height`; that is an identity mapping only while the scale is 1, and every board click in the game passes through it. A wrong conversion throws nothing and blocks the pointer in the wrong places, so the closing check is observed failing at a scale other than 1 before the conversion exists and passing after.
- The panel declares one scale mode, one reference resolution and one match mode in one place, and the committed asset and the code agree rather than the code overriding the asset as it does today.
- `CardFootprint`, `HeroRailWidth`, the hero-rail column height and the standing rule that both rails together stay under half the narrowest validated viewport are **restated in reference units** rather than re-measured in device pixels; each keeps the meaning its own milestone gave it.
- All six configured hero rails replace the circular initial badge with a distinct portrait selected from an explicitly approved hero-image source. The portrait is chosen from the rail's projected hero identity rather than from its display-name text, and neither rules nor control ownership infer identity from the image.
- Each portrait stays legible, unclipped and aspect-correct at every validated viewport. The written hero name remains beside it, focus/readability does not rely on the portrait or team colour alone, and the six-hero layout has no configured fallback to the former letter badge.
- The permanent hero player-path completion gate still measures every verified face of every configured hero against one shared footprint, now at the reference resolution, and no hero's content newly fails or newly passes because of the change.
- The exact-rect expectations at the validated viewports become reference-space expectations, and at least two device resolutions either side of the reference produce the same layout in reference units.
- Milestone 8.25's two accepted geometry facts are re-derived: whether three rails still stack a side, and whether the tray still covers the third rail. If scaling removes that clip, the milestone records it and the clip goes rather than being preserved for consistency.
- The board's framing band `CalculatePlayfieldRect` feeds the fixed camera converts out of reference units before it reaches the camera, so milestone 8.5a's framing is unchanged in world terms.
- Complete unfiltered Edit Mode and Play Mode suites pass, because a change to the panel's coordinate space cannot be shown affected-only.

Non-goals: No broader production-art pipeline or new HUD content beyond the six rail portraits, and no change to any rule, command or projection. No render-pipeline work - this is independent of milestone 8.28 and is cheaper before it, because a scaled layout is what production presentation is authored against. Private or excluded source imagery is not added, published or distributed without the separate authorization and rights already required by the project boundary.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825c-scale-the-hud-with-the-viewport-and-identify-each-rail-with-its-hero-portrait-completion-evidence).

### [x] 8.25d Let a card-text Attack repeat say that it is one

Proposed at the user's direction on 2026-08-26, after milestone 8.25a made a repeated Attack resolution able to carry `ActionInvocationContext.Repeated` and found that the path Xargatha's cards actually use still does not. 8.25a admitted the context on the resolution and left this alone deliberately, because what it changes is which targets two verified cards offer - that milestone's stated non-goal.

`AttackDefenseCardActionStrategyBackend.StartRepeatedAttack` builds the second instance with `completed.InvocationContext`, which is the printed one the first instance ran under, so a card-text Attack repeat reports itself as printed everywhere downstream. Two consequences are live. Xargatha's Cleave and Rapid Thrusts print "May repeat once on a different enemy hero", and the narrowing half of that clause is gated on `resolution.InvocationContext == ActionInvocationContext.Repeated`, so it has never fired: her repeat excludes the unit she just hit - that half travels separately as `ExcludedTarget` - and still admits minions. And the repeated instance's `ActionEnded` checkpoint reports a printed context, so `PendingAfterActionRepeat.For` and `TriggeredEffectResolutionAuthority`'s own guard both read `WasRepeatedInstance` as false, which is what rulebook p.10's "You cannot repeat a repeated action!" is enforced by.

Observable outcome: a repeated Attack instance is one the rules can tell is repeated, and every clause a card prints about its own repeat applies to it.

Acceptance checks:

- `StartRepeatedAttack`'s second instance carries `ActionInvocationContext.Repeated`. The exact set of content this moves is **Wasp's Thunder Boomerang and Xargatha's Cleave and Rapid Thrusts**, which are the only verified Attack texts declaring a `RepeatAction` node; Sabina's and Arien's repeats are Skill and Movement repeats and reach different backends, and Tigerclaw's Cloak and Daggers is the after-action path milestone 8.25a already corrected.
- Xargatha's repeat offers enemy heroes only. Observed failing at the pre-fix revision on a board where her repeat has both an eligible enemy hero and an eligible minion, and passing after - the minion is offered before and not after, and the exclusion on the unit the first instance took is unchanged in both.
- **p.10's bound on a card-text Attack repeat is re-derived rather than assumed.** The repeated instance runs its own page, including the step-7 rider that asked for the repeat, and the Attack path carries no `RepeatContinuation`, so whether a second request is possible today is a measurement this milestone owes before it changes what the checkpoint reports. If a chain is reachable, the milestone states where the bound lives; if it is not, it states what stops it.
- No hero's player-path gate row newly passes or newly fails, and no configured hero's journeys change except where an offered target set legitimately narrows; the two heroes above are re-run at their own tiers.
- The compatibility corpus records whether any fixture holds an Attack resolution mid-repeat. Milestone 8.11 recorded that none did - "no corpus payload held a paused Thunder repeat" - so the expectation is that no digest and no schema version moves, and the expectation is checked rather than trusted.
- Complete unfiltered Edit Mode and Play Mode suites pass, because a change to what an action reports at its own completion checkpoint is read by triggers, and cannot be shown affected-only.

Non-goals: No change to any printed text, to which abilities may repeat, or to the after-action repeat path milestone 8.25a owns. No new repeat binding, and no change to `RepeatTargetBinding`'s members.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825d-let-a-card-text-attack-repeat-say-that-it-is-one-completion-evidence).

### [x] 8.25e Perform the repeat that was offered, or stop offering it

Proposed at the user's direction on 2026-08-26, found while auditing milestone 8.25a. It is the same family as that milestone and is deliberately not folded into it: 8.25a corrected *when* an owed repeat runs, and this is about *which actions can be repeated at all*, which 8.25a's block names as a non-goal.

Rulebook p.9 makes every action performed on a Gold or Silver card a basic action, and p.12's Hold, p.11's Fast Travel and p.13's Clear are all performed on one. All three raise the `ActionEnded` checkpoint under their own `ActionKind`, so `PendingAfterActionRepeat.For` holds a subject for them and Tigerclaw's Cloak and Daggers - "After you perform a basic action you may repeat it once" - offers its decision after each. **Taking it does nothing.** `CardActionResolutionAuthority.StartOwedAfterActionRepeat` switches on Attack, Skill and Movement and returns "nothing happened" for the rest, and its comment for that arm says their second instance "is started there", in the turn authority - where nothing starts it. Every consumer of an owed repeat is `TurnResolutionAuthority.TryStartOwedAfterActionRepeat`, which delegates straight back into that switch.

**Why no gate caught it.** The property milestone 8.25a closed on is that every command the projection offers is accepted and no decision stalls. This command *is* accepted; it simply has no effect, so a seeded match-loop sweep of any length passes over it in silence. That is the reusable finding: an offered decision whose answer changes nothing is invisible to a driver that only checks acceptance.

**The audit that produced this block found the same shape on three more arms of the same switch, and this milestone owns all four.** `StartOwedAfterActionRepeat` also reports "nothing happened" when the second instance is attempted and the backend refuses it - `repeatedAttack`, `repeatedSkill` and `repeatedMovement` each fall through to it on `Accepted == false`. The commonest cause is a board with nothing left the card admits, which the code and two comments call "p.10 offering a repeat rather than owing one". **It is reachable on shipped content through the real surface**: driving Tigerclaw's Cloak and Daggers after Blink Strike on `VerifiedTierThreeEngagedStagedTigerclaw`, taking the repeat returns the game to "choose an action or End Turn" with nothing having happened, because the card forbids repeating onto the unit the first instance crossed and that staging has one. Observed in Play Mode during milestone 8.25a's audit.

**The root cause is one predicate, and it is the same for all four.** `TriggeredEffectResolutionAuthority`'s `RepeatIsUnavailable` is `!TriggeringBasicAction.IsDefined || TriggeringBasicAction.WasRepeatedInstance` - a fact about the action that *ended*. Whether the repeat can be performed is a fact about the dispatcher's kind coverage and about the board as it now stands. So the offer admits strictly more than the answer can perform, in two independent ways, and the ability has no way to know it.

Observable outcome: taking a repeat performs the action a second time - or the ability does not offer a repeat it cannot perform, and the page's reason for that is written down. The four arms are one question, not four.

Acceptance checks:

- **The milestone decides which of the two outcomes is correct against the printed page, and records the reading before it builds either.** p.9's supertype says all three are basic; whether p.10's repeat is performable on them - a second Hold on a card already resolved, a second Fast Travel from a Throne the hero now stands in, a Clear with nothing left adjacent - is a rules question, and `RULES_LEDGER.md` owns the answer if the page is not decisive. A repeat that cannot happen is p.10 offering rather than owing one, which the ability already skips in silence; what is refused here is offering it and then dropping it.
- Observed failing before and passing after, per arm that survives that reading: on a staged board with the Ultimate unlocked, the decision is offered after the action, taking it is accepted, and the second instance is visible in the authoritative state rather than absent. The Blink Strike case above is the one already observed and is the cheapest to write first.
- The `default:` arm's comment no longer describes behaviour that does not exist, and the two "simply does not happen ... p.10 offers a repeat rather than owing one" comments either carry the reading this milestone records or go. Whatever it decides, the code and the sentences beside it agree.
- **The three general-rule paths owe the hold milestone 8.25a gave the other three.** `TryPerformHold`, `TryPerformFastTravel` and the Clear path all resolve their source card before the checkpoint runs, so an instance started from their decision would have no unresolved card and no selected actor to run on - the exact state 8.25a removed for Attack, Skill and Movement.
- A driver-level check exists for the family this escaped: a decision the projection offers and the rules accept changes the authoritative state, or the milestone states why this one does not. One shape, stated once, rather than a case per kind.
- Complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No change to Attack, Skill or Movement repeats, to the after-action hold milestone 8.25a built, or to `RepeatTargetBinding`. No new printed content, and no reinterpretation of which cards are basic - p.9's supertype is settled and this milestone reads it rather than revisiting it.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825e-perform-the-repeat-that-was-offered-or-stop-offering-it-completion-evidence).

Milestones **8.25e1-8.25e7** are defined in the [stabilization and corrective-route shard](PHASE_8_STABILIZATION.md). Completed review 8.25e5c found one missed Constrict ledger field; 8.25e5d corrected it and 8.25e5e freshly verified it. Later Siren's Call corrections 8.25e5f-8.25e5f1 were independently verified at 8.25e5g, reclosing F-SOURCE-01. Completed milestone 8.25e7 closed the surviving 8.16a contract and compatibility findings without reviving the obsolete 8.16b bundle.

### [x] 8.25f Replace generated complete card faces with the verified private faces

Proposed at the user's direction on 2026-08-26. The six configured heroes already have 108 verified definitions and one shared `CardFootprint`, but every complete face the UI draws is still reconstructed from typed content by `ConfiguredCardFaceView.Build`. The private Tabletopia corpus has nineteen action-card fronts for each configured hero: the eighteen-card deck plus the p.20 Handicap card that setup returns to the box. This milestone uses the former 108 deck faces as presentation without making private source material tracked authority or making the rules read pixels.

Observable outcome: when the verified private artwork is prepared locally, every complete identity-known card face for Wasp, Xargatha, Tigerclaw, Arien, Brogan and Sabina shows that definition's real front image at the shared footprint; when private artwork is absent, the same surface shows the current generated face and remains fully playable.

Acceptance checks:

- One explicit presentation catalog maps every configured `CardDefinitionReference` to exactly one of the six heroes' eighteen playable `__front.png` sources, and no source maps to two definitions. A validation check proves the configured 108-definition set and the catalog are bijective, rejects an omitted, duplicate, unknown-hero, dashboard, back-face or revision-mismatched entry, and reports the stable definition and source path that failed.
- A project-local preparation command reads only the existing private corpus and writes Unity-ready images beneath ignored `Assets/Resources/Private/`; it neither mutates the corpus nor stages, embeds, uploads or distributes source or prepared pixels. It fails with a useful inventory error before writing a partial prepared set, and a clean tracked checkout contains no private image while retaining a working generated fallback.
- The immutable projected card identity selects the catalog entry and the prepared resource. Rules, ownership, legality, printed values, card text, action offers, lifecycle, accessibility labels and command payloads continue to come from the existing typed projection; no runtime filename parsing, display-name matching, hero-name branch or image analysis becomes authority.
- The tray, legal Defense responses, card-valued and upgrade choices, current-card or later full-card surfaces, and the shared full-face preview used by revealed turn slots, discard entries and the unlocked Ultimate all go through one artwork-aware complete-face builder. No second artwork lookup or independently styled complete face appears on one of those surfaces.
- A loaded front is aspect-correct, uncropped and clipped to the existing complete-face boundary at `CardFootprint.StandardWidth` by `CardFootprint.StandardHeight`. It does not change the outer footprint, tray wrapping, scroll ownership, pointer/focus surface or keyboard order. Existing selected, unavailable, hover, focus, lifecycle and team/owner treatments remain readable without obscuring meaningful printed content.
- The current generated construction remains the per-card fallback when no prepared resource exists or a test deliberately supplies none. A missing or unloadable resource cannot produce an empty control, change its size, remove its assistive reading or make an otherwise legal command unreachable; development diagnostics identify the stable missing mapping without exposing a private absolute path in player-facing UI.
- Facedown turn commitments remain the neutral identity-free card back and `FACEDOWN`: that projection intentionally carries no card identity, so presentation does not select a real front or leak the committed card. Compact rail lifecycle icons keep their existing non-face meaning; the complete face they reveal on hover uses the real front when available.
- Focused Edit Mode tests cover the 108-entry bijection, excluded dashboard/back faces, identity-to-resource lookup, all complete-face consumers, one shared footprint, state overlays, absent-art fallback and the facedown information boundary. Directly affected Play Mode coverage exercises an ordinary hand, Defense choice, upgrade choice and rail preview with an injected representative texture and with no texture, so the tracked suite does not depend on private files.
- T2 validation includes running the private preparation inventory against all six configured heroes and one targeted 2048-by-1152 Game View inspection showing representative cards from all six heroes without clipping or interaction regression. The completion evidence records that private pixels remained ignored and unstaged; no compatibility digest or schema version moves because presentation resources are not authoritative state.

Non-goals: No card-rule, definition, printed-text, action, command, projection, persistence, replay or network change. No OCR, runtime reading of artwork, mutation of source scans, public asset package, distribution or authorization expansion. No redesign of compact rail lifecycle icons, neutral facedown commitments, hero/dashboard faces, card backs, items, or the fixed card footprint, and no removal of the generated clean-checkout fallback.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825f-replace-generated-complete-card-faces-with-the-verified-private-faces-completion-evidence).

### [x] 8.25f1 Remove the generated shell from prepared card fronts

Authorized directly by the user's 2026-08-27 correction after inspecting the ordinary tray produced by 8.25f. The prepared 372-by-520 front was being drawn inside the older 186-by-420 generated-face box, while Unity's default non-power-of-two import could resample that source away from its original dimensions. The result was a second coloured frame, a lifecycle strip below the printed face, and visibly distorted artwork.

Observable outcome: whenever a prepared configured front exists, the complete-face surface is only that uncropped front at the source's 372:520 proportions - 186 by 260 reference units - with no generated frame, coloured plate, padding, border or lifecycle band around it. When a prepared front is genuinely unavailable, the existing readable 186-by-420 generated fallback remains playable.

Acceptance checks:

- One scoped Editor importer preserves every PNG beneath ignored `Assets/Resources/Private/Cards/` at its native non-power-of-two dimensions, disables mipmaps that can bleed the printed edge, and changes no other asset folder. The six-hero private inspection proves each loaded representative is still 372 by 520 in Unity rather than a power-of-two resample.
- The shared complete-face builder chooses its arm before constructing the visible face. Its artwork arm pins 186 by 260, uses an `Image` with aspect-preserving scale-to-fit, draws no synthetic background, border, padding, generated frame or generated lifecycle band, and leaves the source image whole and uncropped. Its fallback arm retains the existing 186-by-420 generated construction.
- Every complete-face consumer still uses that one builder. The artwork image adds no pointer or keyboard stop; the existing outer card control remains the hit/focus surface. Selection is a conditional overlay that does not resize the image, unavailable cards retain their typed disabled treatment, Active source remains available through the existing tray-slot treatment, and the full typed lifecycle/state reading remains in the tooltip without placing a band over or around the printed face.
- Focused Edit Mode coverage distinguishes the native-aspect artwork footprint from the generated fallback footprint, proves an unselected prepared card has no synthetic frame, proves selection does not add layout size, and checks all complete-face consumers. The permanent hero conformance check validates the prepared image arm by source aspect and bounds, while retaining its full generated-text fit checks for fallback.
- Focused Play Mode crosses an ordinary hand, selected card, Defense choice, upgrade choice and rail preview through both injected-art and absent-art arms. One targeted non-batch 2048-by-1152 capture inspects the ordinary production tray with real prepared cards rather than a presentation-only gallery.

Non-goals: No card rule, definition, printed text, lifecycle, action, command, projection, schema, replay, networking or private-source change. No public asset distribution. No removal of the generated clean-checkout fallback, neutral facedown back, compact rail icons, decision-control affordances, selected/Active/unavailable meaning, or source-image border printed in the artwork itself.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825f1-remove-the-generated-shell-from-prepared-card-fronts-completion-evidence).

### [x] 8.25f2 Enlarge complete card faces to a readable source-ratio footprint

Authorized directly through the user's 2026-08-27 and 2026-08-28 live inspection sequence after 8.25f1. The source-ratio enlargement exposed the ordinary side-by-side tray's cost to board space; the approved outcome therefore includes the physical-hand composition, the reclaimed map surface and the interaction fixes required to keep both playable.

Observable outcome: every complete card face is approximately 300.46 by 420 reference units. Prepared artwork keeps the exact 372:520 source ratio with no generated shell, crop or distortion, and the generated clean-checkout fallback uses the same outer footprint. Five ordinary cards overlap like a physical hand at the existing bottom-left anchor; each remains clickable and reveals its complete face on hover/focus. Board artwork fills the screen while every playable hex stays clear of the hand, rails and status/action lane.

Acceptance checks:

- `CardFootprint` derives the shared width from the verified 372:520 source ratio and the existing 420-unit readable height. Prepared and generated arms pin that same minimum, preferred and maximum size with no flex growth or shrink, and all complete-face consumers continue through the shared builder.
- The ordinary hand occupies the panel's bottom-left x=0..732 footprint with five stable ordered, overlapping faces and no tray frame or scrollbar. Every resting card retains a real exposed pointer target; hover/focus reveals exactly one complete face above all HUD siblings without moving the row, blocking the board, obscuring the action dock or changing the click command. The resolving card is stable-sorted rightmost, and unavailable overlapping cards use one opaque local veil rather than stacking translucent faces.
- Only the painted compact hand blocks board input. The transparent remainder of its full-width layout reservation leaves board pieces - including the lower Blue heroes - clickable. Rail and hand hover previews share the one HUD document, are pointer-transparent and render above status, rail and tray content.
- The Red rail's third hero is followed by `DIRECTOR`, then one fixed 720-by-128 round/current-card/status dock sized for the largest routine revealed-action state. Awaiting-card, selected-card confirmation and revealed-action states retain that exact footprint instead of resizing as the player clicks. Ordinary action and stop controls occupy one hard no-wrap row with no redundant `ACTIONS` heading; the whole row remains visible while a hand card is hovered. Both three-hero rail stacks remain clear of the cards.
- The camera renders the decorative board artwork over the full screen. An exact fit places all 210 complete playable hexes in the union of the broad upper safe arm and the reclaimed lower-right arm, preserving the declared visible gap from actual HUD content while allowing artwork and decorative terrain behind it. Repeated rebuilds reuse an unchanged solved frame and remain under the recorded Phase 4 map-rebuild threshold.
- Ordinary tray scrollers stay hidden. Mandatory multi-card contexts retain the full-width bounded tall tray and vertical `Auto` overflow without horizontal scrolling or card shrink. Prepared and fallback identity remains available to pointer drivers through the outer control's typed tooltip even when the face itself is a single image.
- Focused Edit Mode covers the shared footprint, artwork/fallback consumers, overlap arithmetic, state treatments and safe-layout geometry. Directly affected Play Mode covers live five-card reachability and hover, resolving/unavailable overlap, the invariant status-dock footprint across awaiting/confirmation/action states, action-strip persistence, topmost rail previews, Blue-hero board clicks, all playable-hex containment, absent scrollbars, short mandatory context and the Phase 4 timing threshold. Targeted non-batch captures inspect the production composition with real prepared cards and the three stable ordinary dock states.

Non-goals: No card rule, definition, printed text, lifecycle, action, command, projection, schema, replay, networking, private source or import-contract change. No return of the generated shell, crop or stretch; no removal of the clean-checkout fallback; no redesign of contextual decision meanings, rail icons, facedown back or state meanings beyond the accepted layout, pointer and preview behavior.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825f2-enlarge-complete-card-faces-to-a-readable-source-ratio-footprint-completion-evidence).

### [x] 8.25f2a Correct the stale pre-L-shaped fallback framing assertion

Authorized by the user on 2026-08-28 after milestone 8.25e4's complete Edit Mode run exposed the stable stale assertion left behind by 8.25f2. This correction is completed separately before 8.25e4 resumes.

Observable outcome: `OfficialBoardArtworkTests` states the current no-HUD fallback contract introduced by 8.25f2: when no `BoardCameraSafeLayout` has been supplied, the framing camera centers the complete reviewed map in the whole viewport while retaining the official board basis and containing every reviewed space. The configured scene's L-shaped safe-layout fit remains unchanged and independently covered.

Acceptance checks:

- The pre-correction `OfficialBoardArtworkTests.TheFramingCamera_HoldsEveryReviewedSpaceInTheBandLeftByTheTray` is recorded failing at revision `a7a756ef` in both the 8.25e4 complete Edit Mode run and an isolated rerun: its left and right margins were both `0.220409393f`, disproving only the retired strict right-shift claim.
- The replacement named check `OfficialBoardArtworkTests.TheFallbackFramingCamera_CentresEveryReviewedSpaceInsideTheViewport` exercises the fallback without calling `ConfigureCameraViewport`, proves the official board basis and straight-down camera orientation, contains every reviewed space, and proves symmetric horizontal margins within floating-point tolerance.
- Focused Edit Mode runs the replacement check, directly affected Edit Mode runs the whole `OfficialBoardArtworkTests` fixture, and the complete unfiltered Edit Mode suite passes at the corrective revision. This Edit-test-only correction does not require Play Mode; milestone 8.25e4 still owes both complete suites at its later T3 candidate.

Non-goals: No production camera, HUD, safe-layout, artwork, map, rule, command, snapshot, replay, schema, content or private-source change. No weakening or removal of full-map containment, board-basis orientation, the configured L-shaped fit, or its live-scene coverage.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-825f2a-correct-the-stale-pre-l-shaped-fallback-framing-assertion-completion-evidence).

### [ ] 8.26 Integrate six-hero director control and team interactions

Prerequisites complete: milestone 8.25e7, decision-family closure 8.25e4, Siren's Call corrections 8.25e5f-8.25e5f1, fresh source closure 8.25e5g, art-only 8.25f-8.25f2, and test correction 8.25f2a.

Observable outcome: One director can play and resolve a complete turn for all six independently owned heroes while the rules preserve normal 3v3 ownership, Initiative, target, assist, reaction, and team relationships.

Acceptance checks:

- The director can access each hero's correct hand; for every hero with a nonempty hand the director must choose exactly one owned card, while an empty hand records a Pass. The complete turn reveals only after all six heroes have responses, then resolves every played card in Initiative order without transferring ownership.
- Friendly/enemy queries, tied Initiative decisions, assists, reactions, hero-specific pending choices, and actor switching use authoritative team/player/hero relationships.
- Played/revealed cards and the active resolver remain readable without requiring presentation to own turn legality.
- A read-only primitive round-history matrix groups all three Red and all three Blue heroes and shows each hero's authoritative revealed card or empty-hand Pass for turns 1-4; current-turn Facedown commitments remain hidden until the joint reveal.
- Every displayed history entry resolves through its stable assignment and card definition, and played or Defense-discarded cards cannot reappear merely because presentation rebuilds.
- Undo, restart, snapshot restore, and the event log reproduce the same six-hero turn.

Non-goals: No hidden-information enforcement, table-talk enforcement, networking, AI, or six separate input devices.

### [!] 8.27 Complete the deterministic six-hero 3v3 pitch match

Observable outcome: Wasp and the five other distinct verified heroes can start from the official six-player Quick Game setup, play repeated turns and rounds, and end through every victory path with primitive visuals and director/debug UI.

Acceptance checks:

- Every implemented hero rule links to an authoritative source and focused tests; unresolved source or capability entries keep the gate incomplete.
- The complete six-hero roster exercises legal team interactions, combat, minions, Lane Push, progression, defeat/respawn, and victory without card-name/hero-name branches outside isolated approved handlers.
- The official six-player setup and only the remaining approved director/debug deviations are represented.
- Automated configuration and integration checks prove that staged one-hero-per-team, mirrored-definition, fixed-two-response, and controller-as-game-entity assumptions are absent: six distinct heroes map to six distinct controlling player/seats, exactly three heroes belong to each team, and director tooling remains outside match authority.
- Undo, restart, snapshots, event log, and Tie Breaker override remain deterministic across the complete 3v3 loop.
- A broader source-linked six-hero integration suite passes.
- Close 8.16a's F-CORE-10 on `CompleteMatchRuntimeCaptureTests.MeasuresTheOfficialSixHeroCompleteMatch`: observe the check failing at the pre-fix revision because `CreateCompleteMatchSession` still composes the internal two-seat synthetic configuration, then passing after the capture uses the same official six-player configuration and deterministic full-match driver this gate proves. Re-derive the time, allocation, captured-byte-growth, and Undo lines from that run; retain the prior figures beside the new baseline and disposition every crossing rather than silently widening a threshold.
- `RISK-SCALE-001` is reassessed in full and then retired with its recorded 3v3 bound or its residuals explicitly accepted. This is the first milestone whose acceptance checks reach repeated rounds and progression with six heroes, so it is the first at which rulebook p.15's simultaneous Level Up can be observed as the serialized one-question-at-a-time step the continuation contract makes it, and the first at which the leveling stage's per-round derivation can be sampled against the standing thresholds rather than reasoned about.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Hard gate: The networking architecture decision does not begin until the deterministic 3v3 match, URP migration, and independent readiness review at milestone 8.28a are reviewed and approved. Production visual work remains blocked through milestone 8.36a.

### [!] 8.28 Render-pipeline migration gate

Observable outcome: The complete primitive six-hero match is ported from Unity's Built-In Render Pipeline to URP, with its existing director/debug presentation and Win64 reference path intact, before production visual assets are introduced.

Planning rationale: Unity deprecated the Built-In Render Pipeline in Unity 6.5, guarantees its availability through at least Unity 6.7 LTS, and has not announced a final removal version. This project can retain Built-In rendering through the primitive-rule phases, but must not begin production presentation work on a pipeline Unity has directed new projects away from. [Unity's 2026 render-pipeline strategy](https://unity.com/topics/render-pipelines-strategy-for-2026).

Acceptance checks:

- URP is the active render pipeline for every configured quality level; the project no longer relies on a Built-In-only default pipeline setting.
- The existing primitive board, miniatures/tokens, card/HUD panels, highlighting, pointer selection, fixed camera, and event-driven presentation remain readable and functional after conversion.
- Materials, shaders, lighting, and post-processing used by the retained primitive presentation are URP-compatible; no Built-In-only visual dependency is carried into Phase 9.
- The authoritative rules assemblies and their deterministic state/event results are unchanged; the migration does not alter game rules, command handling, or timing.
- Focused Play Mode coverage for the affected presentation paths, a targeted visual inspection, a disposable internal Win64 development-build launch smoke, and both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No production board art, hero/minion production models, animation coverage, final lighting/art direction, new game rules, or player-facing build.

Hard gate: The private-online track uses this accepted URP baseline after milestone 8.28a independently reviews its six-hero and networking-readiness implications. Phase 9 production visual milestones do not begin until milestone 8.36a is also reviewed and approved.

### [!] 8.28a Independently review six-hero, URP, and networking readiness

Blocked by: Milestones 8.27 and 8.28 must pass the deterministic six-hero and URP migration gates.

Observable outcome: A fresh analysis-only review converts measured six-hero match, projection, replay/snapshot, and URP behavior into explicit technical constraints for the private-online architecture decision.

Acceptance checks:

- The review measures or verifies representative command/event volume, immutable projection and snapshot size, restore/replay cost, full-match allocation/resource behavior, and frame-time characteristics for the complete primitive 3v3 configuration.
- It inspects sources of nondeterminism, Unity-thread or scene coupling, host-authority suitability, stable identity/version boundaries, pending-choice transport needs, and current public/private information ownership.
- It distinguishes actual scaling or confidentiality blockers from concerns that the later networking milestones already own, and does not select a provider or invent a transport design prematurely.
- Findings follow the common independent-review evidence contract and supply concrete constraints and risks to milestone 8.29's source-backed architecture comparison.
- Any prerequisite correction is proposed as a separate milestone; no networking, optimization, rendering, rules, or projection implementation occurs here.

Non-goals: No provider/SDK selection, network prototype, public service, production rendering work, speculative scale target beyond the six invited players, or implementation fix.

Hard gate: Milestone 8.29 does not begin until milestone 8.28a and every networking-readiness blocker are reviewed and given an approved disposition.

### [x] 8.28b Establish a seeded, replayable randomness seam

Scheduled by the user-approved 2026-08-13 ER2 roadmap amendment; findings and evidence live in the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2). It touches only Core setup, so it may be approved and executed at any earlier point; it must complete before 8.29 begins the online track. The 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation) resequences it directly after 8.2 in execution order - it is already unblocked, its closing determinism oracle exists since 7.9, and it is a schema/corpus change cheapest while the corpus is eight fixtures rather than after 8.25 - so it is the route's next proposal ahead of 8.3.

Observable outcome: The one Core randomness source - `TieBreakerSetupAuthority.CreateNormalSetupState`, which draws non-seeded `RandomNumberGenerator` output outside the command history - becomes a seeded seam whose draw is recorded in authoritative setup state, so deterministic replay and a future online authority reproduce normal setup exactly.

Acceptance checks:

- Normal setup's coin flip derives from a recorded seed or recorded outcome carried in immutable setup state and the snapshot, not from an unrecorded draw.
- Replay of a normal-setup match reproduces the identical Tie Breaker side from recorded state alone.
- The DEV-005 deterministic internal override is unchanged, and normal match flow still flips randomly from the player's perspective per the rulebook.
- Future randomness consumers have one seam to join; no second unrecorded randomness source is introduced.

Closing check for `F-ER2-04`: a determinism fixture replaying normal setup from recorded state and matching the flip result, observed failing at the pre-fix revision (the draw is unrecorded) and passing at the post-fix revision.

Non-goals: No networking, no new randomness consumers, no rulebook deviation, no change to the tie-breaker's gameplay meaning.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-828b-establish-a-seeded-replayable-randomness-seam-completion-evidence).

### [!] 8.29 Select the minimum private-online architecture and re-baseline the track

Observable outcome: A source-backed architecture decision defines the smallest supportable way for six invited Windows players on the internet to join one private 3v3 match, with one authoritative host/session and one independently owned hero per player.

Acceptance checks:

- Current Unity-supported transport, relay/NAT traversal, hosting, cost, licensing, privacy, and operational constraints are compared before a provider or SDK is selected.
- The trust boundary, host authority, seat/hero ownership, client-visible projections, version compatibility, join flow, disconnect policy, and failure model are explicit, including duplicate, replayed, stale, malformed, oversized, unauthorized, and out-of-order inputs plus disconnects during each pending-choice family.
- A lightweight threat model identifies trusted and untrusted processes, per-seat command authority, public/team/owner-private information, diagnostic-log sensitivity, host-tampering limitations, and the required safe rejection or terminal behavior. This is scoped to six invited players and does not claim production anti-cheat or public-service security.
- The selected Unity editor, packages/SDKs, transport/service versions, licenses, private-distribution constraints, deterministic/authoritative-code reach, removal/upgrade path, and build compatibility are pinned in a version-controlled dependency decision record. Upgrades that can affect authority, serialization, networking, rendering, or builds require an explicit compatibility assessment and applicable gate rerun.
- The track remains private and invite-only; accounts, public matchmaking, public server browsing, telemetry, monetization, and public distribution remain out of scope.
- Milestones 8.30-8.36, including the lettered local-UI and rehearsal slices, receive evidence-based implementation boundaries and validation commands before the first implementation milestone is authorized.

Planning note: This scope amendment adds a major cross-system track rather than one playtest task. The provisional planning range is thirteen implementation/validation micro-milestones and roughly 4-8 focused engineering weeks, excluding approval latency, external-service outages, and corrective work discovered by local or real-internet testing. The added milestone boundaries isolate session/projection, player-UI, transport, and internet defects; they do not by themselves expand the intended product scope. Milestone 8.29 must replace this estimate with evidence from the selected architecture.

### [!] 8.30 Move match authority behind a transport-neutral session boundary

Observable outcome: The existing local director adapter and a future network host can submit the same typed commands to, and consume the same ordered events and snapshots from, one authoritative match session without moving rules into Unity views or transport callbacks.

### [!] 8.30a Add locally testable owner-scoped projections and command authorization

Blocked by: Milestone 8.30 must establish the transport-neutral session boundary.

Observable outcome: Before any real transport is introduced, the local session can expose six distinct seat-scoped client projections and authorize each submitted command against its stable player/hero ownership.

Acceptance checks:

- A development-only local seat harness can select each of the six configured seats outside match authority and consume the same owner-scoped projection contract intended for network clients.
- Each seat receives its own hand and pre-reveal commitment state plus only the authorized public or team-shared state; another seat's hidden hand or commitment identity is absent rather than merely concealed by a view.
- Every player submission reaches the authoritative session as a gameplay-command payload plus trusted connection-or-local-seat context, a unique command identity, and the expected projection version supplied outside payload claims. The session derives the submitting `PlayerId`, owned hero, and team through that trusted seat binding and authoritative `MatchParticipants`; a `HeroId`, `TeamSide`, or other stable identity carried by `DirectorCommand` identifies the requested action or choice and is never accepted as caller identity.
- The authoritative session rejects cross-seat hero commands, unauthorized team choices, stale expected projection versions, replayed or duplicated command identities, mismatches between payload targets and ownership derived through `MatchParticipants`, and forged payload identities without partial mutation.
- Snapshot restore and session replacement rebuild every seat's authorized projection deterministically without transferring ownership or revealing another seat's private state.
- Director-only Undo, restart, hidden-state inspection, and Tie Breaker override remain outside the player projection and command surface.

Non-goals: No network SDK, transport envelope, host/join flow, relay/NAT traversal, reconnect policy, production security claim, or polished UI.

ER2 note (`F-ER2-07`): `MatchSessionProjection` is currently one global read model exposing every owner's cards and full state; this milestone is the recorded owner of the per-viewer seam, and every projection property added before it raises its cost.

### [!] 8.30b Seat-scope and complete the player HUD prompt flow

Re-scoped by the 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation): milestone 7.8a already delivered the player-facing runtime UI Toolkit match HUD this block's original "build" wording restated. What remains is scoping that proven surface to one authorized seat and completing the states a multi-seat session adds.

Blocked by: Milestone 8.30a must prove the local owner-scoped projection and authorization contract.

Observable outcome: The proven 7.8a HUD, driven by one selected seat's authorized projection and typed invocations alone, can complete the full six-hero match through the local session.

Acceptance checks:

- The HUD shows the selected seat's own hand and commitments, authorized shared reveal/history, active resolver, round/turn, Lives, Waves, hero levels, coins, and connection-independent session status without querying rules objects or director-only state.
- Every action, target, Defense, tied-Initiative, team-owned, optional/mandatory continuation, and completed-match state required by the complete pitch roster has a readable contextual prompt and forwards the exact projected typed invocation.
- Waiting, rejected-input, stale-view, session-replacement, recoverable-error, and terminal states are explicit; presentation never guesses legality or silently leaves a control enabled after its offer expires.
- Rebuilding after snapshot restore or local session replacement produces the same authorized HUD and prompt state without retaining stale callbacks, selections, hidden data, or presentation-owned continuation state.
- The development-only seat switcher is visibly non-player-facing, cannot mutate seat ownership, and is unavailable from private-online client projections.
- The primitive layout keeps all required cards, prompts, and state reachable at the supported desktop validation resolutions without introducing the deferred production HUD, general settings suite, or accessibility suite.

Non-goals: No transport, lobby, reconnect implementation, production styling, animation overhaul, main menu, account UI, tutorial, or Phase 9 presentation work.

### [!] 8.30c Validate the complete six-seat player experience against the local session

Blocked by: Milestone 8.30b must provide the primitive local player HUD and prompt flow.

Observable outcome: A complete deterministic 3v3 match can be played locally while exercising all six seat perspectives, establishing that session, privacy, HUD, and prompt defects are isolated before networking begins.

Acceptance checks:

- A recorded local scenario completes a full match while switching among all six development seats and covers every roster-required player prompt family, team-owned choice, reveal boundary, and victory path.
- Automated projection and presentation checks cover each seat's private/public visibility, cross-seat rejection, stale controls, snapshot/session rebuild, completed-match state, and supported validation resolutions.
- Repeated seat switching and session replacement do not leak hidden information, transfer control, duplicate input handlers, retain stale selections, or grow presentation-owned resources monotonically.
- Findings are classified as authoritative-session, projection/authorization, HUD/prompt, or presentation-lifecycle defects and every blocking defect is corrected and rerun green before transport work is authorized.
- Validation records explicitly state that this in-process local gate proves no claim about serialization envelopes, network faults, relay/NAT traversal, latency, disconnects, or real-internet behavior.

Non-goals: No network process, provider integration, lobby, reconnect, public distribution, production visuals, or invited playtest.

### [!] 8.31 Establish the private host and six player seats

Blocked by: Milestone 8.30c must pass the complete local six-seat player-experience gate.

Observable outcome: One host creates an invite-only session and six compatible Windows clients can occupy exactly one stable player/hero seat each, with duplicates, unauthorized seat changes, and incompatible content/schema revisions rejected.

### [!] 8.32 Enforce owner-scoped projections and command authorization across the network boundary

Blocked by: Milestone 8.31 must establish the private host and six network seats.

Observable outcome: Each network client receives the already-proven seat-scoped projection and can submit commands only for its owned hero or an explicitly team-owned choice; untrusted transport input cannot bypass host-side revalidation.

Acceptance checks:

- The host constructs each submission's trusted caller context from the authenticated connection-to-seat binding established by milestone 8.31, attaches a unique command identity and expected projection version outside the untrusted gameplay-command payload, and derives player/hero/team ownership through authoritative `MatchParticipants`. Payload fields, including any `HeroId`, `TeamSide`, or forged identity/version claim, never establish or override the caller's identity, seat, command identity, or expected version.
- Automated negative cases cover cross-seat hero commands, unauthorized team choices, hidden-hand/pre-reveal leakage, replayed or duplicated command identities, stale expected projection versions, mismatches between payload targets and ownership derived through `MatchParticipants`, malformed/oversized envelopes, and forged payload identities or versions without partial host mutation.
- Diagnostic output needed for support does not expose another seat's owner-private hand or pre-reveal commitment identity.
- Network serialization and delivery preserve the local projection's absence of unauthorized fields rather than sending complete director state and relying on client-side hiding.

### [!] 8.33 Synchronize commands, events, pending choices, and snapshots

Blocked by: Milestone 8.32 must enforce owner-scoped projections and command authorization across the network boundary.

Observable outcome: All clients reproduce the host's public match state and their authorized private projection across full turns, nested Defense reactions, tied Initiative choices, and other pending resolutions without client-owned rules authority.

Acceptance checks:

- Repeatable fault-injection scenarios exercise permitted delay/jitter, duplication, loss, reordering, stale messages, corrupt/incompatible snapshots, and interrupted synchronization; clients either converge on the host's authorized version or enter one explicit recoverable/terminal failure state.
- Host and client diagnostics correlate session/content/schema version, command identity, authoritative projection version, and recovery decision without logging prohibited private information.

### [!] 8.34 Add disconnect, reconnect, resynchronization, and host-loss policy

Blocked by: Milestone 8.33 must prove authorized synchronization under fault injection.

Observable outcome: A temporarily disconnected player can return to the same compatible private session and rebuild the correct authorized projection; stale commands, unrecoverable host loss, and version mismatch fail explicitly without corrupting the match.

Acceptance checks:

- Fixed scenarios disconnect and reconnect during commitment, joint reveal, same-team Initiative choice, Movement continuation, Defense reaction, and end-of-round progression, then prove convergence or the documented terminal result.
- Reconnect to stale, future, incompatible, or unauthorized session state cannot transfer seat control, reveal private information, or partially replace authoritative state.

### [!] 8.35a Add the primitive private host/join lobby

Blocked by: Milestones 8.31-8.34 must establish seats, authorized synchronization, and reconnect/host-loss policy.

Observable outcome: Without production styling, invited players can host or join by the selected private invitation mechanism, identify their seat and team, observe compatibility and connection state, become ready, enter the match, leave, and begin an authorized reconnect.

Acceptance checks:

- The lobby exposes exactly the selected architecture's minimum host/join and invitation flow, six stable seats, team/hero identity, readiness, compatibility/version rejection, and transition-to-match state.
- Duplicate claims, unauthorized seat changes, full/incompatible sessions, host loss, join failure, and reconnect eligibility receive explicit bounded feedback without exposing another player's private match state or sensitive diagnostic data.
- Team identity and status never rely on color alone, and all required controls remain reachable at the supported desktop validation resolutions.
- Lobby state remains presentation-only over the session contracts and cannot assign authoritative match ownership by mutating Unity views.

Non-goals: No account system, friends list, public matchmaking, server browser, text/voice chat, spectator mode, ranking, moderation, general main menu, or production styling.

### [!] 8.35b Integrate the proven player HUD with online match flow

Blocked by: Milestone 8.35a must establish the primitive private lobby.

Observable outcome: The locally proven player HUD operates over the network client/session adapter with clear waiting, synchronization, latency, disconnect, reconnect, rejection, host-loss, and terminal feedback while preserving the same authoritative invocations and visibility boundaries.

Acceptance checks:

- Invited clients enter the existing owner-scoped HUD from the lobby, see only their own hand and authorized shared state, answer every legal prompt, and return to an explicit recoverable or terminal state when the session policy requires it.
- Network waiting, stale-view rejection, resynchronization, temporary disconnect, incompatible restore, reconnect success/failure, and host loss cannot leave stale actionable controls or presentation-owned continuation state.
- Equivalent local and network projections produce equivalent player HUD and prompt meanings; transport metadata does not become gameplay authority or fork a second rules-facing UI path.
- Reconnect and resynchronization rebuild the HUD without hidden-information leakage, seat-control transfer, duplicated input handlers, stale selections, or monotonic presentation-resource growth.

Non-goals: No production HUD redesign, public-service UX, matchmaking, account recovery, telemetry dashboard, tutorial, chat, or Phase 9 polish.

### [!] 8.35c Pass a local six-client playtest-build rehearsal

Blocked by: Milestone 8.35b must complete primitive online match-flow UI integration.

Observable outcome: The exact intended private-playtest topology and disposable Win64 build path complete a six-client match locally, including bounded fault and reconnect scenarios, before friends are asked to test over the internet.

Acceptance checks:

- Six local client processes occupy six distinct seats through the primitive lobby and complete a full match using the same build, content/schema revision, host/session boundary, owner-scoped projections, and UI intended for milestone 8.36.
- The rehearsal exercises the approved local fault-injection, disconnect/reconnect, stale/version rejection, host-loss, and resynchronization cases and records host/client convergence plus privacy-reviewed diagnostics.
- No client receives another seat's hidden hand, controls another hero, retains stale actionable prompts after resynchronization, or diverges from the host's authoritative public outcome.
- Every observed blocking rules/session, projection/privacy, HUD/prompt, transport/synchronization, build, or diagnostic defect is corrected and rerun green before the internet playtest is authorized.
- The evidence explicitly states that a same-machine or local-network rehearsal does not prove relay/NAT traversal, external latency behavior, multi-network connectivity, or real-internet reliability.

Non-goals: No invited external tester, public distribution, production service, account system, matchmaking, production presentation, or substitution for milestone 8.36.

### [!] 8.36 Validate a private six-human internet 3v3 and run the friends playtest

Blocked by: Milestone 8.35c must pass the local six-client playtest-build rehearsal.

Observable outcome: Six humans on separate clients complete an invite-only full 3v3 match over the internet using a disposable private Win64 playtest build and primitive UI before production presentation begins.

Acceptance checks:

- The exact build and content/schema revision are recorded and privately distributed only to the invited testers.
- The session proves host/join, six stable seats, private hands, all normal player and team-owned choices, complete turns/rounds, reconnect/resync, match completion, and explicit failure handling across at least two external networks.
- Host and clients converge on the same authoritative public outcome; owner-private projections and command authorization show no cross-player leakage or control transfer.
- The playtest includes the approved bounded latency/disconnect/reconnect cases and preserves a privacy-reviewed diagnostic bundle sufficient to correlate failures without exposing another player's hidden hand.
- Crashes, desynchronization, latency symptoms, confusing flows, unreadable state, and tester feedback are recorded and triaged without changing rules or code during the milestone.
- Any gate-blocking defect becomes a separately approved corrective milestone; the reviewed findings re-baseline Phase 9 HUD, prompt, feedback, and readability work.
- T3 online hard gate: focused network/session tests, a multi-client integration run, the private internet playtest, and both complete unfiltered Edit Mode and Play Mode suites pass at the applicable final revision.

Non-goals: No public release, dedicated production service, accounts, matchmaking, anti-cheat certification, competitive operations, or production presentation.

### [!] 8.36a Independently review the private-online implementation and playtest evidence

Blocked by: Milestone 8.36 must complete the invited six-human internet gate and record its unmodified playtest evidence.

Observable outcome: A fresh analysis-only review correlates observed playtest failures and friction with the networking, authorization, synchronization, reconnect, diagnostics, and primitive presentation implementation before production polish begins.

Acceptance checks:

- The review examines actual session logs, test/build identity, tester observations, failure handling, host/client convergence evidence, reconnect/resync behavior, latency-sensitive choices, and relevant source paths.
- It checks private-information leakage, command authorization, stale/version-mismatched input, host-loss policy, diagnostic sufficiency, runtime stability, and whether symptoms indicate correctness, architecture, performance, operational, or presentation causes.
- Findings follow the common independent-review evidence contract; uncertain causes remain explicitly uncertain rather than being converted into unsupported fixes.
- Gate-blocking defects and later presentation/UX findings are separated into appropriately scoped proposed corrective or Phase 9 milestones.
- No code, rules, networking configuration, build, or presentation implementation changes during the review.

Non-goals: No second playtest, production hosting/service design, accounts, matchmaking, telemetry, public distribution, or bundled fix implementation.

Hard gate: Phase 9 does not begin until milestone 8.36a and every gate-blocking corrective milestone are reviewed and approved.
