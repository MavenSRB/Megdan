# Phase 8 - Completed milestone definitions, third shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-04 during milestone 8.35o's required warning-threshold ownership review. Milestone 8.30 moved here verbatim, the oldest definition the active shard still held, because both existing completed shards (`PHASE_8_COMPLETED.md`, `PHASE_8_COMPLETED_2.md`) were themselves within a few hundred words of their own warning line and could not absorb it without becoming the next ownership review. Milestones 8.30a through 8.30d followed on the same date during milestone 8.35p's review, which is the rest of the same contiguous run: the owner-scoped projections and command authorization every later seat contract reads against, the seat-scoped HUD prompt flow, its complete six-seat validation, and the printed-icon lighting that finished the action surfaces. Keeping the run together is the point - 8.30a onward read 8.30 by id, and splitting them across shards would have left each half pointing at the other. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

### [x] 8.30 Move match authority behind a transport-neutral session boundary

Observable outcome: The existing local director adapter and a future network host can submit the same typed commands to, and consume the same ordered events and snapshots from, one authoritative match session without moving rules into Unity views or transport callbacks.

Implementation boundary: [the private-online architecture record](../Project/PRIVATE_ONLINE_ARCHITECTURE.md#re-baselined-implementation-track) scopes this milestone to the Unity-free serialized host/session port and its protocol-neutral result stream, with no UGS, package, or transport work, and [the private-online validation shard](../Testing/PRIVATE_ONLINE.md#milestone-matrix) names its exact filters and tier. The acceptance checks below were written from those two user-approved records when the milestone was authorized, and add no scope to them.

Acceptance checks:

- One Unity-free port fronts the whole authority-mutating surface - typed command, Undo, restart, snapshot restore - plus the snapshot read and the current projection, so a consumer needs no second reference to the session behind it.
- The port executes submissions serially: concurrent submissions queue rather than interleave, and a submission made from inside another - the shape a transport callback takes - is refused before the authority is reached.
- Each accepted submission appends exactly one record to one ordered result stream carrying its sequence, the authoritative version it published, and the ordered rule events it produced; two independent consumers reading from their own cursors receive identical ordered records.
- A refused submission advances neither the authoritative version nor the stream, and a host refusal is reported distinctly from the authority's own rules refusal.
- A consumer whose cursor has fallen outside the retained window is told to resynchronize and handed no records, rather than handed a stream with a gap in it.
- The same typed commands reach the same authoritative state, snapshot and rule-event log whether submitted through the port or executed directly against the session.
- The existing local director adapter submits through the port: the configured scene composes the host, its presenters and headless driver hold the port's client, and no production code outside the application boundary constructs a match session.
- Nothing on the port names a Unity, UGS, transport or socket type, and the submission payload carries no caller identity that milestone 8.30a's trusted seat binding could be claimed by.

Closing check: `MatchAuthority_IsConstructedOnlyInsideTheApplicationBoundary`, observed failing at the pre-fix working tree - where the configured scene runtime constructed the authority itself - and passing once that composition moved behind the port.

Non-goals: No owner-scoped projection, seat binding, submission identity, expected-version or idempotency check, which are milestone 8.30a's; no seat-scoped HUD, which is 8.30b's; no network SDK, package installation, transport envelope, codec, host/join flow, relay, reconnect policy, production security claim, or UI work.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830-move-match-authority-behind-a-transport-neutral-session-boundary-completion-evidence).

### [x] 8.30a Add locally testable owner-scoped projections and command authorization

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

Resolution (`F-ER2-07`): the per-viewer seam exists. `SeatProjection` is a separate read model built per seat, in which another seat's hand and deck are absent rather than filtered, and it carries no accepted-command history, snapshot, diagnostic log or Director control. `MatchSessionProjection` is unchanged and stays the Director's own full-trust view; the cost this note warned about is now paid once per seat-visible field rather than by every consumer.

Rules note: the Director's surface asks each hero to commit in turn, which is one controller serializing one question. The seat surface authorizes every seat the authority is still awaiting at the same time, which is rulebook p.7's simultaneous facedown choice; the commitment authority already tracked each hero independently, so nothing about the rule changed.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830a-add-locally-testable-owner-scoped-projections-and-command-authorization-completion-evidence).

### [x] 8.30b Seat-scope and complete the player HUD prompt flow

Re-scoped by the 8.2 re-baseline (2026-08-21, user approved, applying the 7.9a recommendation): milestone 7.8a already delivered the player-facing runtime UI Toolkit match HUD this block's original "build" wording restated. What remains is scoping that proven surface to one authorized seat and completing the states a multi-seat session adds.

Blocked by: Milestone 8.30a must prove the local owner-scoped projection and authorization contract.

Amendment, 2026-08-29, user-approved: this milestone additionally moves End Turn to the bottom right of the panel as a larger control of its own, and moves the contextual narration into the top status bar, which is expanded to report what the match is doing at every step. It widens the primitive-layout check past this block's "no production styling" non-goal for those two surfaces only; every other non-goal stands, and the measured HUD geometry milestones 8.25c and 8.25f2 pin is re-measured rather than assumed by it.

Observable outcome: The proven 7.8a HUD, driven by one selected seat's authorized projection and typed invocations alone, can complete the full six-hero match through the local session.

Acceptance checks:

- The HUD shows the selected seat's own hand and commitments, authorized shared reveal/history, active resolver, round/turn, Lives, Waves, hero levels, coins, and connection-independent session status without querying rules objects or director-only state.
- Every action, target, Defense, tied-Initiative, team-owned, optional/mandatory continuation, and completed-match state required by the complete pitch roster has a readable contextual prompt and forwards the exact projected typed invocation.
- Waiting, rejected-input, stale-view, session-replacement, recoverable-error, and terminal states are explicit; presentation never guesses legality or silently leaves a control enabled after its offer expires.
- Rebuilding after snapshot restore or local session replacement produces the same authorized HUD and prompt state without retaining stale callbacks, selections, hidden data, or presentation-owned continuation state.
- The development-only seat switcher is visibly non-player-facing, cannot mutate seat ownership, and is unavailable from private-online client projections.
- The primitive layout keeps all required cards, prompts, and state reachable at the supported desktop validation resolutions without introducing the deferred production HUD, general settings suite, or accessibility suite.

Non-goals: No transport, lobby, reconnect implementation, production styling, animation overhaul, main menu, account UI, tutorial, or Phase 9 presentation work.

Closing check: `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation`, observed failing at the pre-fix working tree - where the status derivation named eight of the ten declared pending families, so a standing Minion Battle or paused system transition reached the player under the round's own phase line - and passing once both branches were added.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830b-seat-scope-and-complete-the-player-hud-prompt-flow-completion-evidence).

### [x] 8.30c Validate the complete six-seat player experience against the local session

Blocked by: Milestone 8.30b must provide the primitive local player HUD and prompt flow.

Observable outcome: A complete deterministic 3v3 match can be played locally while exercising all six seat perspectives, establishing that session, privacy, HUD, and prompt defects are isolated before networking begins.

Acceptance checks:

- A recorded local scenario completes a full match while switching among all six development seats and covers every roster-required player prompt family, team-owned choice, reveal boundary, and victory path.
- Automated projection and presentation checks cover each seat's private/public visibility, cross-seat rejection, stale controls, snapshot/session rebuild, completed-match state, and supported validation resolutions.
- Repeated seat switching and session replacement do not leak hidden information, transfer control, duplicate input handlers, retain stale selections, or grow presentation-owned resources monotonically.
- Findings are classified as authoritative-session, projection/authorization, HUD/prompt, or presentation-lifecycle defects and every blocking defect is corrected and rerun green before transport work is authorized.
- Validation records explicitly state that this in-process local gate proves no claim about serialization envelopes, network faults, relay/NAT traversal, latency, disconnects, or real-internet behavior.

Non-goals: No network process, provider integration, lobby, reconnect, public distribution, production visuals, or invited playtest.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830c-validate-the-complete-six-seat-player-experience-against-the-local-session-completion-evidence).

### [x] 8.30d Light the chosen action's printed icons and integrate the replacement, End Turn, and narration surfaces

User-approved roadmap amendment (2026-08-30), following 8.30b's precedent: widens the block's "no production styling" non-goal for exactly five surfaces — the printed-action offer treatment, the Defense-prompt cards, the two replacement controls, the End Turn control, and the top status bar's narration lane. Every other non-goal stands. The user approved starting this before 8.30c, so the six-seat gate validates the finished surfaces; 8.31 remains gated on 8.30c alone.

Blocked by: Milestone 8.30b must provide the seat-scoped player HUD and prompt flow.

Observable outcome: While the player is choosing, the choice itself is lit and unmissable: each offered printed action's own icon shines (silhouette rim plus radiance, steady, brighter under pointer and focus), a Defense offer shines exactly the shield it would defend with, Fast Travel and Clear hang as card-gold ribbon tabs off the Movement and Attack they respectively replace, End Turn is a neutral pewter banner that shines fully when it is the only offer left, and the status bar narrates the match in the player's own words — lit warm when the sentence is addressed to the viewing seat.

Acceptance checks:

- A geometry pass over all six heroes' prepared fronts verifies the shared badge rects per icon slot (rail shield, rail boot, primary boot/swords/book/shield, ranged and Block variants); shine sprites trace each badge's outer silhouette from the highest-contrast source card for that slot, are alignment-checked against every hero's fronts, and live with the private card resources.
- Every offered printed action carries the shine at its kind icon's rect; unavailable regions carry none; the panel box and ring treatments are retired. Overlays are picking-ignored: hit surfaces, focus order, and the UiPlayer click drivers are unchanged.
- The Defense prompt shines the rail shield for a secondary Defense and the primary-slot shield for a Duelist or Block, placement typed by the presenter; `DECLINE DEFENSE` stays untreated.
- Fast Travel's ribbon attaches to the Movement region it replaces and Clear's to the Attack region it replaces, text-first, muted when unavailable, shining under pointer and focus.
- End Turn keeps its 8.30b placement, hide rules, and invocation, restyled as the pewter banner with its two glow states.
- The narration keeps its single derivation and `EveryDeclaredPendingFamilyIsNamedByTheStatusDerivation`, reworded to name who acts in game terms without engine vocabulary, and the lane visually distinguishes the viewing seat's own move from every other state.
- Generated-fallback faces state the same facts through their typed chips lighting up; a missing sprite degrades to the prior treatment with a one-time diagnostic.
- The measured HUD geometry milestones 8.25c and 8.25f2 pin is re-measured rather than assumed; Game View inspection at the supported validation resolutions confirms every lit state is obvious at real card size, with archived screenshots and user sign-off on intensity.

Non-goals: No animation loop, no board-surface changes, no invented action glyphs, no other HUD styling, no Phase 9 presentation suite.

2026-08-30 corrective, user-directed after live review of the completed milestone: an offered primary lights the card's own printed lines (the type ribbon's box and the rules-text box, one traced ring overlay per card) instead of its icon; secondaries keep their badge shine, re-derived through per-family template masks; the replacement ribbons follow the card's printed colour; card faces clip to the printed corner radius; the peek preview stands in for the card it enlarges; and Confirm and Pass are pewter banners in End Turn's corner while the hand-status bar and the dock's reserved empty frame are retired. The Defense-prompt shield shine and every other acceptance stand as written.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-830d-light-the-chosen-actions-printed-icons-and-integrate-the-replacement-end-turn-and-narration-surfaces-completion-evidence).

**2026-08-30 route amendment, user-directed.** The former proposed milestone 8.30e was removed from the active route. Keep milestone 8.30d's completed scan-backed highlighting treatment as implemented; no digital-front rebuild, user-traced template-card dependency, or bulk 108-front composition/sign-off work remains scheduled. At that amendment 8.30c was next and 8.31 was gated on it alone. The later late-HUD amendment below schedules new corrections after the already-started 8.30c and before 8.31. The never-started definition is preserved in [roadmap history](../Archive/ROADMAP_HISTORY.md#removed-proposed-milestone-830e-compose-the-card-fronts-digitally-from-the-verified-content).

**2026-08-30 late-HUD amendment, user-directed.** Proposed milestones **8.30f**, **8.30g**, and **8.30h** are defined in [the late HUD and card-interaction correction shard](PHASE_8_LATE_HUD_CORRECTIONS.md). Milestone 8.30c was already being implemented when they were defined, so it completes first. Milestone 8.30f keeps End Turn bottom-right, moves only commitment confirmation below `DIRECTOR`, strengthens fixed action highlights, centres/enlarges compact numbers, and reorders a Defense discard; 8.30g adds immediate facedown commitment by dragging an eligible hand card onto the map while preserving click-then-confirm; 8.30h separately iterates Movement and Attack colours while lighting all six printed corner marks of every projected choice hex. Each correction refreshes the 8.30c checks it affects. The route is 8.30c, 8.30f, 8.30g, 8.30h, then 8.31. The removed 8.30e stays removed. Further 2026-08-31 amendments add **8.30i-8.30m** after 8.30h: completed 8.30i moves minion Defense contributions and the Defense hover breakdown into the corrected shine language and previews authority-projected contributions when a legal hero is hovered during Attack targeting; completed corrective **8.30i1** replaces the prepared hero faces' coloured interior fields with white while preserving portraits and team rings on both board and rail; completed corrective **8.30i2** removes generic effect-choice confirm/undo buttons, auto-completes a filled selection, and keeps spatial selection and deselection on the map; completed corrective **8.30i3** keeps the ordinary hand visible as non-actionable context beneath facedown-style `ACCEPT`/`DECLINE` banners during an optional step; completed **8.30j** makes p.15's Upgrade-a-Card choice lose its comparison box, pair captions and pair/option outlines while retaining six ordered card controls and no `GAINS` prefix; completed **8.30k** replaces hero-plaque and Upgrade-a-Card item abbreviations with shared printed symbols and places each solid upgrade box over its candidate's bottom item position; completed corrective **8.30k1** restarts Hub/licensing before every fixed-launcher validation; non-resolving unavailable cards leave the bottom playable hand; and every rendered card receives one shared thick black exterior outline. A 2026-09-01 user-directed amendment places 8.30l's unavailable-card group on the right side, renders it like the current hand, and retains the existing unavailable dimming. Further 2026-09-01 amendments add **8.30n** so tied candidate owners publicly state bounded preferences and unanimously agree who acts first, and all three members of a Minion Battle's losing team publicly agree to its exact selected removal set before the existing authoritative confirmation is submitted. The final affected six-seat evidence refresh moves to 8.30n, and the route ends 8.30h, 8.30i, 8.30i1, 8.30i2, 8.30i3, 8.30j, 8.30k, 8.30k1, 8.30l, 8.30m, 8.30n, then 8.31.

**2026-09-01 completion amendment.** Milestone **8.30n** is complete: tied Initiative candidate owners and all three Minion Battle choosing-team seats now use bounded public unanimous agreement before the existing final rules command is submitted. The remaining route is **8.31**; the late-HUD shard owns the exact checks.
