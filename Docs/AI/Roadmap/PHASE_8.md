# Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

## Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work composes proven core actions, keywords, effect operations, queries, modifiers, control flow, and system transitions; it does not defer their first general implementation until a card requires them. The mirrored Wasp setup identities introduced earlier remain an intermediate configuration for the first complete hero gate, not the final pitch roster. After that gate, five additional user-approved heroes enter through source inventory and capability-delta mapping before the final six-distinct-hero 3v3 integration. Once the complete primitive match and URP baseline are proven, a separate private-online track moves the same authority behind a host/session boundary and reaches an invite-only six-human internet playtest before production presentation begins.

### [ ] 8.1 Inventory and verify Wasp's complete card set

Observable outcome: Wasp's cards, tiers, values, action types, text, item possibilities, and Ultimate are traceable to source images.

Acceptance checks:

- Every required face is present and readable.
- Structured data is manually checked against the images.
- Missing or ambiguous content is recorded in `Rules/HERO_WASP.md`, the shard milestone 6.1y made the owner of Wasp's inventory; anything not scoped to Wasp alone stays in `RULES_LEDGER.md`.

Non-goals: No other hero inventory or effect implementation.

### [ ] 8.2 Map Wasp cards to capabilities and re-baseline the remaining Phase 8 milestones

Observable outcome: Every verified Wasp action maps to required contexts, ordered nodes, typed bindings/outcomes, timing, state transitions, and reusable or custom capabilities before effect implementation.

Acceptance checks:

- The matrix covers every Wasp card face, tier, item relationship, and Ultimate from milestone 8.1.
- Existing core capabilities are distinguished from Wasp-specific refinements, new reusable primitives, and genuinely unique handlers.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and tests.
- The designated roadmap audit applies the verified inventory and capability matrix to the remaining Wasp milestones through 8.16. It recommends justified keep/merge/split/defer/remove decisions, records an updated remaining-effort range, and keeps dependencies ahead of the first Wasp card that needs them.
- Approved remaining Wasp milestones through 8.16 are reordered or split so dependencies precede the first Wasp card that needs them.
- If the Phase 8 endpoint changes, update the T3 gate in `TESTING.md` and this roadmap in the same re-baseline.
- New reusable contracts update `CARD_EFFECT_CAPABILITIES.md` before implementation.
- Any schema change is explicit and migration-tested rather than hidden inside a card milestone.

Non-goals: No effect implementation or other-hero inventory.

### [ ] 8.3 Add verified Wasp definitions and lifecycle data

Observable outcome: Verified Wasp definitions/instances use the existing lifecycle and turn foundation without placeholder authority.

Acceptance checks:

- Wasp values and text are source-verified structured data.
- Definition revisions and upgrade/replacement relationships are explicit.
- Card images remain presentation/audit assets only.
- Synthetic test values are not used as Wasp authority.

### [ ] 8.4 Load both Wasp hands into the director turn loop

Observable outcome: Red and Blue Wasp hands replace the synthetic hands while using the already-proven play/reveal/Initiative/turn controller.

### [!] 8.5 Implement exact Wasp Movement behavior

### [!] 8.6 Integrate verified Wasp spatial effects with the core capabilities

Maps verified Wasp text onto the already-proven direct Move, Object Push, Place, Spawn, board-object Swap/Replace, Remove, and specialized Respawn contracts. This milestone composes source-verified definitions, policies, queries, timing, and choices; it does not postpone first implementation of the general operations until a card requires them. A genuinely new source-required variant receives an explicit versioned capability or controlled custom handler and focused source-linked tests rather than changing established semantics or adding card-name branches.

### [!] 8.7 Implement verified Wasp token, marker, obstacle, and persistent effects

### [!] 8.8 Implement verified Wasp card-state and resource effects

### [!] 8.9 Integrate verified Wasp modifiers, restrictions, permissions, and characteristics with the core capabilities

### [!] 8.10 Integrate Wasp Skills and structured control flow with the core capabilities

### [!] 8.11 Implement Wasp Attack, range, and minion interactions

### [!] 8.12 Implement complete Wasp Defense behavior

### [!] 8.13 Implement Wasp timing, Active-effect, and continuous behavior

### [!] 8.14 Integrate Wasp leveling, upgrades, items, and Ultimate

Every milestone from 8.5 onward remains blocked until milestone 8.2 re-baselines the exact dependency order and smallest proving slices.

### [!] 8.15 Complete and verify every Wasp card behavior

Observable outcome: Every Wasp definition and legal action at each pitch-relevant tier passes source-linked focused scenarios after its re-baselined dependencies are available.

Acceptance checks:

- Every offer/context from the milestone 8.2 matrix is implemented; a blocked entry keeps this milestone incomplete.
- No placeholder value, display-name branch, runtime text parsing, or UI-owned rule path remains.
- Interaction tests cover each required timing, target, modifier, replacement, repeat, state change, Active effect, and continuous source.
- Any unresolved source question prevents completion rather than being guessed.

### [!] 8.16 Complete the deterministic Wasp mirror integration gate

Observable outcome: A Wasp mirror match can start, play repeated turns/rounds, and end through every victory path with primitive visuals and debug UI.

Acceptance checks:

- Every implemented Wasp rule links to an authoritative source and focused tests.
- Approved deviations are the only known departures.
- Undo, restart, snapshots, event log, and Tie Breaker override work internally.
- A broader source-linked Wasp integration suite passes.
- T3 hard gate: both complete unfiltered Edit Mode and Play Mode suites pass.

### [!] 8.16a Independently review Wasp-driven card-engine extensibility

Blocked by: Milestone 8.16 must pass its source-linked Wasp integration gate.

Observable outcome: A fresh analysis-only review identifies what the first complete verified hero revealed about the generic card/effect architecture before five additional heroes multiply its extension costs.

Acceptance checks:

- The review traces every implemented Wasp capability to its source, typed contract, handler or composition, serialized continuation/state, ordered events, and focused/integration evidence.
- It searches for card-name or hero-name branching, runtime printed-text parsing, untyped universal payloads, Wasp-specific behavior disguised as a generic capability, duplicated patterns that now justify a shared capability, and speculative abstractions supported by only one exceptional card.
- It evaluates effect/query/modifier/continuous-source performance and test maintainability using actual Wasp scenarios rather than synthetic assumptions alone.
- Findings follow the common independent-review evidence contract and distinguish legitimate isolated custom handlers from leakage that would make the next five heroes increasingly expensive.
- Any correction is proposed separately; the review changes no rules, content, runtime implementation, card values, or source interpretation.

Non-goals: No selection, inventory, or implementation of another hero; no Wasp rebalance; no generic framework merely to eliminate every custom handler.

Hard gate: Additional pitch-hero inventory and implementation do not begin until milestone 8.16a and every gate-blocking corrective milestone are reviewed and approved. Production visual work remains blocked until the six-hero 3v3 gate at milestone 8.27, the render-pipeline migration gate at milestone 8.28, and the private online playtest gate at milestone 8.36 are reviewed and approved.

### [!] 8.17 Select the five additional pitch heroes

Observable outcome: The user approves five specific heroes that join Wasp as six different heroes divided into three Red and three Blue roster slots for the private pitch.

Acceptance checks:

- The proposed roster contains no duplicate hero and identifies each hero's intended team slot without treating team assignment as a card-rule branch.
- Implementation-grade card, dashboard, token/marker, miniature/presentation, and applicable official-ruling sources are confirmed available or explicitly recorded as blockers.
- Selection considers effect-family and presentation breadth so the roster demonstrates the reusable architecture without claiming equal implementation cost for every hero.
- The exact five-hero choice receives explicit user approval before inventory begins.

Non-goals: No card transcription, effect implementation, production art, balance adjustment, or seventh pitch hero.

### [!] 8.18 Inventory and verify the five additional hero sets

Observable outcome: Every selected hero's cards, tiers, values, action types, text, item possibilities, Ultimate, tokens/markers, and other hero-owned components are traceable to source material.

Acceptance checks:

- Every required face and component is present and readable for all five heroes.
- Structured data is manually checked against source images and applicable official rulings.
- Missing, ambiguous, or conflicting content is recorded in that hero's own `Rules/HERO_<NAME>.md` shard, per the contract milestone 6.1y established, and blocks the affected hero rather than being guessed; content affecting more than one hero stays in `RULES_LEDGER.md`.
- No implementation begins from unverified filenames, OCR, display text, or assumed cross-hero similarity.

### [!] 8.19 Map the five heroes to capabilities and re-baseline the 3v3 pitch work

Observable outcome: Every verified action and hero-owned component maps to existing or new typed capabilities, and the remaining hero, 3v3 integration, and presentation milestones are re-baselined from evidence.

Acceptance checks:

- A roster-wide matrix distinguishes reused core/Wasp capabilities, reusable refinements, new primitives, and genuinely unique isolated handlers for every verified hero face and component.
- Every effect node maps to a capability ID/version, typed bindings, policy, failure/atomicity rule, checkpoints, and source-linked tests.
- The designated roadmap audit applies the matrix to milestones 8.20-8.36 and Phase 9, recommending justified keep/merge/split/defer/remove decisions and recording an updated remaining-effort range.
- Approved implementation slices are dependency ordered; a shared capability precedes the first hero that needs it, and no hero-name rule branch or runtime text parsing is scheduled.
- Any schema change is explicit and migration-tested rather than hidden inside a hero milestone.

Non-goals: No effect implementation or production art.

### [!] 8.20 Complete and verify additional pitch hero slot A

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.21 Complete and verify additional pitch hero slot B

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.22 Complete and verify additional pitch hero slot C

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.23 Complete and verify additional pitch hero slot D

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.24 Complete and verify additional pitch hero slot E

The exact hero name, dependency slices, acceptance checks, and validation are assigned by milestone 8.19 before authorization.

### [!] 8.25 Replace the staged 1v1 setup with the official six-player Quick Game configuration

Observable outcome: Configuration data places three different Red heroes and three different Blue heroes with their own hands and components using the official six-player Quick Game baseline.

Acceptance checks:

- All six stable player/hero identities, ownership relationships, hero spawn positions, hands, dashboards, and hero-owned components resolve without assuming one hero per team.
- Every authoritative turn slot contains exactly six ownership-validated assignments—one for each configured `HeroId`—and snapshot round-trip preserves their deterministic order and committed-card-or-empty-hand-Pass facts; controlling `PlayerId` and team resolve through the participant graph.
- Setup uses three Wave counters, five Life counters per team, the normal Tie Breaker flip, and the verified official board/minion setup.
- Wasp appears once; all six heroes are different and assigned to exactly one team/player.
- DEV-001 and DEV-002 are marked retired; only approved director/debug deviations remain.
- Snapshots round-trip the complete six-player setup deterministically.
- `RISK-SCALE-001`'s payload half is measured rather than computed: the six-player decoded payload is recorded against the staged 1v1 reference of 6,633 bytes and compared with the standing thresholds in `Testing/FUTURE_GATES.md`. This milestone is the first at which 108 card instances and 108 definitions exist, and the risk's own figure for them is arithmetic on measured unit costs rather than a measurement.

Non-goals: No networking, hidden per-player devices, AI, matchmaking, or balance changes. No reassessment of `RISK-SCALE-001`'s ordering half, which needs a round boundary this milestone does not reach.

### [!] 8.26 Integrate six-hero director control and team interactions

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

### [ ] 8.28b Establish a seeded, replayable randomness seam

Scheduled by the user-approved 2026-08-13 ER2 roadmap amendment; findings and evidence live in the [ER2 record](../Archive/Phase-Handoffs/PHASE_6.md#2026-08-13-external-engineering-review-er2). It touches only Core setup, so it may be approved and executed at any earlier point; it must complete before 8.29 begins the online track.

Observable outcome: The one Core randomness source - `TieBreakerSetupAuthority.CreateNormalSetupState`, which draws non-seeded `RandomNumberGenerator` output outside the command history - becomes a seeded seam whose draw is recorded in authoritative setup state, so deterministic replay and a future online authority reproduce normal setup exactly.

Acceptance checks:

- Normal setup's coin flip derives from a recorded seed or recorded outcome carried in immutable setup state and the snapshot, not from an unrecorded draw.
- Replay of a normal-setup match reproduces the identical Tie Breaker side from recorded state alone.
- The DEV-005 deterministic internal override is unchanged, and normal match flow still flips randomly from the player's perspective per the rulebook.
- Future randomness consumers have one seam to join; no second unrecorded randomness source is introduced.

Closing check for `F-ER2-04`: a determinism fixture replaying normal setup from recorded state and matching the flip result, observed failing at the pre-fix revision (the draw is unrecorded) and passing at the post-fix revision.

Non-goals: No networking, no new randomness consumers, no rulebook deviation, no change to the tie-breaker's gameplay meaning.

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

### [!] 8.30b Build the primitive local player HUD and complete prompt flow

Blocked by: Milestone 8.30a must prove the local owner-scoped projection and authorization contract.

Observable outcome: A player-facing primitive HUD can complete the full six-hero match through the local session using only one selected seat's authorized projection and typed invocations.

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

Observable outcome: All clients reproduce the host's public match state and their authorized private projection across full turns, nested Defense reactions, tied Initiative choices, and other pending resolutions without client-owned rules authority.

Acceptance checks:

- Repeatable fault-injection scenarios exercise permitted delay/jitter, duplication, loss, reordering, stale messages, corrupt/incompatible snapshots, and interrupted synchronization; clients either converge on the host's authorized version or enter one explicit recoverable/terminal failure state.
- Host and client diagnostics correlate session/content/schema version, command identity, authoritative projection version, and recovery decision without logging prohibited private information.

### [!] 8.34 Add disconnect, reconnect, resynchronization, and host-loss policy

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
