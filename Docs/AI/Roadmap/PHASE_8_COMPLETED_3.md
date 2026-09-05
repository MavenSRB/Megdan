# Phase 8 - Completed milestone definitions, third shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-04 during milestone 8.35o's required warning-threshold ownership review. Milestone 8.30 moved here verbatim, the oldest definition the active shard still held, because both existing completed shards (`PHASE_8_COMPLETED.md`, `PHASE_8_COMPLETED_2.md`) were themselves within a few hundred words of their own warning line and could not absorb it without becoming the next ownership review. Milestones 8.30a through 8.30d followed on the same date during milestone 8.35p's review, which is the rest of the same contiguous run: the owner-scoped projections and command authorization every later seat contract reads against, the seat-scoped HUD prompt flow, its complete six-seat validation, and the printed-icon lighting that finished the action surfaces. Keeping the run together is the point - 8.30a onward read 8.30 by id, and splitting them across shards would have left each half pointing at the other. Milestones 8.31 through 8.35a1 followed on 2026-09-05 during milestone 8.35u's required warning-threshold ownership review, continuing the same contiguous run one step further: the direct-TCP host and six player seats with their two correctives, the owner-scoped network authorization, the disconnect/reconnect/host-loss policy, the primitive host/join lobby, and the unkeyed-bind refusal that corrected it. They move for the same reason their parents did - the active shard could not hold another milestone definition without crossing its own warning line, and this shard is the only one of the three with room. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

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

### [x] 8.31 Establish the private host and six player seats

Blocked by: Milestone 8.30n must correct and validate public teammate agreement for tied Initiative and Minion Battle removal after milestones 8.30c, 8.30f-8.30i, 8.30i1-8.30i3, and 8.30j-8.30m.

**2026-09-01 transport and topology amendment, user-directed.** The user replaced milestone 8.29's player-hosted UGS Multiplayer Services/Relay-DTLS selection with direct TCP sockets and a dedicated seatless host, invoking the reassessment clause [the amended architecture record](../Project/PRIVATE_ONLINE_ARCHITECTURE.md) opens with; that record is the canonical statement. No Unity cloud account, package installation, anonymous authentication, or join-code service remains in scope. The host is a seventh process holding the authoritative session and no seat; all six players are clients addressing it by IP and port, on one machine now and on an operator-run Windows host later. Invite-only admission is carried by an optional pre-shared session key, and the link is plaintext while no owner-private byte crosses it; the encryption decision is owed at milestone 8.32 before owner projections reach the wire. Protocol family `guards-private-online` version 1, the admission tuple, structural owner projections, host-bound seats, terminal host loss, and the transport-neutral port with its adapter beneath it stand unchanged. Milestones 8.32-8.36a keep their definitions: each names the selected architecture rather than a provider, so the substitution reaches them through that record without rewriting them.

Observable outcome: One dedicated seatless host process listens on a configured address and port, and six compatible Windows client processes each occupy exactly one stable player/hero seat, with duplicate claims, unauthorized seat changes, incompatible build/schema/content revisions, a wrong session key, and a full session rejected by named reason before any match state is shared.

Acceptance checks:

- The host publishes one immutable admission tuple - protocol `guards-private-online/1`, build identity, snapshot schema 34, map `official-single-lane` at its pinned revision, and the registered content digest - and refuses a client whose tuple differs, naming the exact mismatching element, before that client learns anything else about the session.
- When the host holds a pre-shared session key, admission requires the client to answer the host's random challenge with proof of the same key, a wrong or missing answer is rejected by named reason, and the host binds to the loopback interface unless an operator names another address explicitly.
- Each admitted client claims exactly one seat from the authoritative six-seat binding derived through `MatchParticipants`. A claim naming a held seat, a claim naming an unknown seat, a second claim from an already-seated connection, and a compatible connection arriving when no seat remains open each receive their own bounded rejection reason and leave the host's seat map unchanged.
- Seat ownership is host state bound to one live connection: no frame field establishes, changes, or transfers a seat, and a disconnected claimed seat stays reserved and unclaimable for the rest of the session rather than being released to whoever asks next; restoring it to its player is milestone 8.34's reconnect work.
- Every frame is length-prefixed and bounded: an unknown magic, protocol version, or message kind, a truncated frame, a declared length past the configured maximum, or a handshake that misses its deadline is refused before allocation-heavy decode and disconnects only that connection.
- Every socket type stays inside the adapter assembly beneath the transport-neutral session port; the port, the authority, and the application boundary name none, and the conformance checks fail if one appears.
- One rehearsal run builds the disposable Win64 player and starts one host and six client processes locally, proving six distinct seats claimed, a duplicate claim refused, an incompatible client refused, and every process exiting on its own inside a bounded time.
- Host and client diagnostics carry protocol/build/schema/map/content identity, an opaque connection ordinal, the seat, and the rejection reason, and never a remote address, a hand, or another seat's state.

Non-goals: No gameplay over the wire - no command codec, no seat projection on the wire, no state synchronization, and no snapshot, Undo, or restart transfer (milestones 8.32-8.33); no reconnect, resynchronization, heartbeat, or host-loss recovery beyond immediate termination (8.34); no lobby, readiness, menu, or HUD surface (8.35a-8.35b); no encryption (decided at 8.32); no NAT traversal, relay, account system, matchmaking, telemetry, package installation, public distribution, or non-Windows host.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-831-establish-the-private-host-and-six-player-seats-completion-evidence).

### [x] 8.31a Keep private-online processes running without window focus

Blocked by: Completed milestone 8.31's first manual windowed run exposed the defect: the checked-in player pauses its whole loop on focus loss (`runInBackground: 0`), so of seven visible processes only the focused one pumps its session and the handshake silently stalls while unfocused windows freeze before their first rendered frame.

Observable outcome: A process launched with a private-online host or client flag keeps pumping its session while its window is hidden or unfocused, so a windowed host plus six windowed clients on one desktop reach `SEATS_COMPLETE claimed=6/6` exactly as the batch rehearsal does, while a normally launched build keeps the existing pause-on-focus-loss behavior.

Acceptance checks:

- Configuring either private-online driver enables background execution for that one process; no other launch path changes the player's focus behavior.
- The closing check `PrivateOnlineBackgroundExecutionTests` records its failing-then-passing transition for both drivers, and one windowed non-batch hidden host-plus-six-client run reaches `SEATS_COMPLETE claimed=6/6` with every process self-exiting.

Non-goals: No rehearsal-script topology change, no project-wide `runInBackground` setting change, no reconnect, heartbeat, lobby, or gameplay networking.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-831a-keep-private-online-processes-running-without-window-focus-completion-evidence).

### [x] 8.31b Draw the board's blended overlays correctly in a built player

Blocked by: Corrective 8.31a's first windowed run exposed the defect: in a built player every hex tile over the official artwork renders as opaque white instead of transparent, hiding the whole playfield, while the same scene is correct in the Editor. Every board overlay is a runtime-created URP Lit material blended by keyword, so nothing in the build data declares a transparent Lit material and its variant does not survive the build.

Observable outcome: A built player draws the official board artwork with its playfield visible rather than covered, and a build-time visual check looks at the running player's own pixels rather than assuming them.

Acceptance checks:

- A board fill that is fully transparent draws nothing by not being rendered at all, so no stripped shader variant can turn it into an opaque cover; spaces stay pickable, because a space is chosen by where the pointer ray meets the board plane and never by what is drawn on it.
- Board materials that must genuinely blend derive their surface state from template material assets that ship under `Resources`, so the configuration is present in build data rather than only asserted at runtime, and no board source blends a material it built from a found shader.
- A named build-time visual gate builds the disposable Win64 player, launches it, captures the running player's own frame, and fails when the playfield is one flat colour; it records the defect's failing observation and the fixed passing observation across separate builds.
- The Editor keeps its current board appearance: existing artwork registration, overlay alpha, rim, highlight and resource-lifetime checks pass unchanged.

Non-goals: No board artwork change, no camera or registration change, no new overlay treatment, no project-wide render-pipeline or stripping-setting change, and no networking change. **Known residual:** this corrective proves the playfield is not covered; it does not prove that a partly blended overlay - a highlight at its overlay alpha - reaches a built player blended rather than opaque, because shipping a transparent template alone was observed to retain that variant on one build and not on the next. The templates are kept as the best available retention, and a later slice owns proving highlight fidelity inside a player, most likely through a preloaded shader-variant collection.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-831b-draw-the-boards-blended-overlays-correctly-in-a-built-player-completion-evidence).

### [x] 8.32 Enforce owner-scoped projections and command authorization across the network boundary

Blocked by: Milestone 8.31 must establish the private host and six network seats.

Observable outcome: Each network client receives the already-proven seat-scoped projection and can submit commands only for its owned hero or an explicitly team-owned choice; untrusted transport input cannot bypass host-side revalidation.

Acceptance checks:

- The host constructs each submission's trusted caller context from the authenticated connection-to-seat binding established by milestone 8.31, attaches a unique command identity and expected projection version outside the untrusted gameplay-command payload, and derives player/hero/team ownership through authoritative `MatchParticipants`. Payload fields, including any `HeroId`, `TeamSide`, or forged identity/version claim, never establish or override the caller's identity, seat, command identity, or expected version.
- Automated negative cases cover cross-seat hero commands, unauthorized team choices, hidden-hand/pre-reveal leakage, replayed or duplicated command identities, stale expected projection versions, mismatches between payload targets and ownership derived through `MatchParticipants`, malformed/oversized envelopes, and forged payload identities or versions without partial host mutation.
- Diagnostic output needed for support does not expose another seat's owner-private hand or pre-reveal commitment identity.
- Network serialization and delivery preserve the local projection's absence of unauthorized fields rather than sending complete director state and relying on client-side hiding.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-832-enforce-owner-scoped-projections-and-command-authorization-across-the-network-boundary-completion-evidence).

### [x] 8.34 Add disconnect, reconnect, resynchronization, and host-loss policy

Blocked by: Milestone 8.33 must prove authorized synchronization under fault injection.

Observable outcome: A temporarily disconnected player can return to the same compatible private session and rebuild the correct authorized projection; stale commands, unrecoverable host loss, and version mismatch fail explicitly without corrupting the match.

Acceptance checks:

- Fixed scenarios disconnect and reconnect during commitment, joint reveal, incomplete and disagreed same-team Initiative agreement, partial Minion Battle removal agreement, Movement continuation, Defense reaction, and end-of-round progression, then prove convergence or the documented terminal result. A returning eligible player receives the host's current public statements and standing proposal and may revise only their own; reconnect never converts partial or conflicting statements into agreement.
- Reconnect to stale, future, incompatible, or unauthorized session state cannot transfer seat control, reveal private information, or partially replace authoritative state.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-834-add-disconnect-reconnect-resynchronization-and-host-loss-policy-completion-evidence).

### [x] 8.35a Add the primitive private host/join lobby

Blocked by: Milestones 8.31-8.34 must establish seats, authorized synchronization, and reconnect/host-loss policy.

Observable outcome: Without production styling, invited players can host or join by the selected private invitation mechanism, identify their seat and team, observe compatibility and connection state, become ready, enter the match, leave, and begin an authorized reconnect.

Acceptance checks:

- The lobby exposes exactly the selected architecture's minimum host/join and invitation flow, six stable seats, team/hero identity, readiness, compatibility/version rejection, and transition-to-match state.
- Duplicate claims, unauthorized seat changes, full/incompatible sessions, host loss, join failure, and reconnect eligibility receive explicit bounded feedback without exposing another player's private match state or sensitive diagnostic data.
- Team identity and status never rely on color alone, and all required controls remain reachable at the supported desktop validation resolutions.
- Lobby state remains presentation-only over the session contracts and cannot assign authoritative match ownership by mutating Unity views.

Non-goals: No account system, friends list, public matchmaking, server browser, text/voice chat, spectator mode, ranking, moderation, general main menu, or production styling.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835a-add-the-primitive-private-hostjoin-lobby-completion-evidence).

### [x] 8.35a1 Refuse an unkeyed bind off loopback from the lobby's host control

Corrective milestone for a gap milestone 8.36's readiness review found, approved by the user on 2026-09-03. An unkeyed session derives no channel keys, so its link stays plaintext, and [the architecture record](../Project/PRIVATE_ONLINE_ARCHITECTURE.md) permits an owner-private byte to cross plaintext only on the loopback interface. The command-line host role has refused an unkeyed non-loopback bind since milestone 8.32; milestone 8.35a's `HOST` control - the path an operator actually uses to host an invited playtest - applied no such rule and would have opened an unkeyed public listener.

Observable outcome: Hosting from the lobby obeys the same rule as the command line - an unkeyed session may bind only loopback, a keyed session may bind any interface - and a refused bind is named on the entry form before any listener exists.

Acceptance checks:

- Closing check `PrivateLobbyPlayModeTests.HostingOffLoopbackWithoutASessionKey_IsRefusedBeforeAnyBind`, recorded failing at the pre-fix revision, where pressing `HOST` on a non-loopback address with an empty key reached a bind attempt and reported the failed bind, and passing after, where the entry form names the missing session key and no session is opened.
- The rule is stated once and read by both surfaces, so the two cannot drift apart again; each surface keeps its own bounded wording, because a lobby names its key field rather than a command-line flag.
- No stricter behavior is introduced: an unkeyed loopback bind and a keyed non-loopback bind stay accepted, the latter proven by its own journey.

Non-goals: No new admission rule, key-strength policy, transport, encryption, diagnostic, or lobby redesign; no change to the command-line rule this mirrors or to any other launch path.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835a1-refuse-an-unkeyed-bind-off-loopback-from-the-lobbys-host-control-completion-evidence).

### [x] 8.35b Integrate the proven player HUD with online match flow

Blocked by: Milestone 8.35a must establish the primitive private lobby.

Observable outcome: The locally proven player HUD operates over the network client/session adapter with clear waiting, synchronization, latency, disconnect, reconnect, rejection, host-loss, and terminal feedback while preserving the same authoritative invocations and visibility boundaries.

Acceptance checks:

- Invited clients enter the existing owner-scoped HUD from the lobby, see only their own hand and authorized shared state, answer every legal prompt, and return to an explicit recoverable or terminal state when the session policy requires it.
- The tied-Initiative and Minion Battle prompts preserve milestone 8.30n's public statements and unanimous-agreement boundary over the network adapter: eligible players can state, revise, or withdraw only their own preference/agreement, observers see the current public proposal and status without actionable controls, and no client can submit either final rules choice directly.
- Network waiting, stale-view rejection, resynchronization, temporary disconnect, incompatible restore, reconnect success/failure, and host loss cannot leave stale actionable controls or presentation-owned continuation state.
- Equivalent local and network projections produce equivalent player HUD and prompt meanings; transport metadata does not become gameplay authority or fork a second rules-facing UI path.
- Reconnect and resynchronization rebuild the HUD without hidden-information leakage, seat-control transfer, duplicated input handlers, stale selections, or monotonic presentation-resource growth.

Non-goals: No production HUD redesign, public-service UX, matchmaking, account recovery, telemetry dashboard, tutorial, chat, or Phase 9 polish.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835b-integrate-the-proven-player-hud-with-online-match-flow-completion-evidence).

### [x] 8.35c Pass a local six-client playtest-build rehearsal

Blocked by: Milestone 8.35b must complete primitive online match-flow UI integration.

Observable outcome: The exact intended private-playtest topology and disposable Win64 build path complete a six-client match locally, including bounded fault and reconnect scenarios, before friends are asked to test over the internet.

Acceptance checks:

- Six local client processes occupy six distinct seats through the primitive lobby and complete a full match using the same build, content/schema revision, host/session boundary, owner-scoped projections, and UI intended for milestone 8.36.
- The rehearsal forces at least one same-team tied-Initiative disagreement followed by a revised unanimous preference and one changed Minion Battle removal proposal after partial agreement followed by unanimous confirmation, proving public visibility, proposal invalidation, exactly-once final submission, unchanged Tie Breaker behavior, and unchanged removal ordering across six processes.
- The rehearsal exercises the approved local fault-injection, disconnect/reconnect, stale/version rejection, host-loss, and resynchronization cases and records host/client convergence plus privacy-reviewed diagnostics.
- No client receives another seat's hidden hand, controls another hero, retains stale actionable prompts after resynchronization, or diverges from the host's authoritative public outcome.
- Every observed blocking rules/session, projection/privacy, HUD/prompt, transport/synchronization, build, or diagnostic defect is corrected and rerun green before the internet playtest is authorized.
- The evidence explicitly states that a same-machine or local-network rehearsal does not prove relay/NAT traversal, external latency behavior, multi-network connectivity, or real-internet reliability.

Non-goals: No invited external tester, public distribution, production service, account system, matchmaking, production presentation, or substitution for milestone 8.36.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835c-pass-a-local-six-client-playtest-build-rehearsal-completion-evidence).

### [x] 8.35d Run the end-of-round sequence automatically and scope each hero's level up to its own player

Corrective milestone for two findings the user reported from live multiplayer testing on 2026-09-03 and approved the same day, ahead of milestone 8.36's human gate. Milestone 8.30a authorized `AdvanceEndOfRound` to every seat because the command carries no choice. Six players sharing that one control stepped rulebook p.8's stages past each other's pauses, and the five seats that did not own the standing question were each drawn a `CONTINUE END OF ROUND` control whose every press the host refused - because a seat's scoped surface reports the decision it does not own as absent, which is the privacy rule working correctly. Separately, p.15's Upgrade a Card was owned by the whole side the transition named, so a teammate could answer another player's upgrade; that is what "leveling was skipped for some players" was.

Observable outcome: In a seat-based hosted session, rulebook p.8's end-of-round sequence runs by itself in stage order the moment the phase is entered, pausing only for decisions somebody owns and resuming automatically once one is answered, and each hero's Upgrade a Card pause is answerable only by the seat that owns that hero.

Acceptance checks:

- Entering end of round with no decision standing advances every applicable stage without any seat submission, in `EndOfRoundStage` order, as ordinary accepted commands in the one recorded stream, so replay, retained-prefix Undo, snapshots and restore see the sequence they always saw.
- A stage pause of any family - the pre-Minion-Battle triggered-effect question, the Minion Battle selection, the Lane Push Place, the Level Up upgrade - halts the continuation, only the seat that owns it is authorized, and the answer resumes it automatically through to the next pause or the next round's commitments.
- A hero that can afford no level is passed over with no pause and no item of its own; this already held and is now asserted.
- Each hero's upgrade pause is answerable only by the seat owning that hero: a teammate's and an opponent's submission are both refused `UnauthorizedCommand` without moving the version, the stream or the snapshot, and the Director still answers it directly.
- No seat projection offers `AdvanceEndOfRound` in any phase and no seat surface draws `CONTINUE END OF ROUND`; the Director's own direct door runs no stage by itself and still advances one by hand.
- While a reserved seat holds the session, the continuation holds with it and resumes on the returning player's next accepted answer.
- A six-client wire regression drives a full end of round, converges every seat on the settled authoritative version rather than on the one the submission alone produced, and records no seat submitting an advance.

Non-goals: No change to rulebook p.8's stages, presentation pacing or animation, Director mode, the wire protocol version, the persisted snapshot schema, or milestone 8.36's execution.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835d-run-the-end-of-round-sequence-automatically-and-scope-each-heros-level-up-to-its-own-player-completion-evidence).

### [x] 8.35e Render the Minion Battle agreement stage on every scoped seat and survive a scoped tray's absent revealed card

Corrective milestone for two client crashes recorded in the 2026-09-03 invited-playtest logs (`tmp/playtest-mitok-2026-09-03.log` seat red-player-01, `tmp/playtest-Andrej-2026-09-03.log` seat blue-player-01, and `tmp/playtest-sppet-2026-09-03.log` seat red-player-02 on the hosting PC, whose file interleaves the host control's own wire lines), reported the same day ahead of milestone 8.36's re-run. When the Minion Battle's confirm stands as milestone 8.30n's public unanimous agreement, a seat's scoped projection carries the public `TeamAgreement` and no owned `PendingMinionBattleChoice` detail - and `MinionChoicePresenter.CreateViewModel`'s public-agreement branch then constructs `MinionChoiceViewModel` with default end-of-round identities, which that constructor's own guard refuses. All three clients threw that `ArgumentException` on every render - entered from an accepted minion selection's own result refresh, from an accepted agreement choice's (`SubmitAgreement`'s) result refresh, and from every `PrivateLobbyDriver.Update` - and each throw aborts `MatchFeatureCoordinator.RefreshAll` mid-list, so every seat's surface froze at the exact stage the branch exists to draw. The authority itself kept serving throughout: the hosting PC's wire lines record accepted agreements and submissions advancing the authoritative version (92 through 97) under surfaces that could no longer draw them, so the end-of-round stall the players reported is this presentation crash's consequence, ruled the same day as not its own finding. Separately, `DirectorHandsPresenter.Refresh` resolves each hero's commitment `RevealedCardInstanceId` with `Single` over the seat's scoped card list, and all three logs record `InvalidOperationException` tray failures where the named card was absent from that scoped list - mostly at the session tail, and on the hosting PC also inside an accepted round-action submission's own result refresh, so the absence window opens while end-of-round stages are being stepped, not only at teardown.

Observable outcome: In a seat-served session, every seat renders the Minion Battle from first selection through the public unanimous agreement to the applied removal without a presentation exception - the choosing side with its selection controls and then its own agreement choices, the other side read-only - and a seat's hand tray renders every projection its client can legitimately receive.

Acceptance checks:

- A choosing-side seat at the agreement stage renders the public counts, the selected units and its own agreement choices with no candidates and no fabricated identities, and its agreement submission is accepted; the removal-prompt form with its identity guard is constructed only from an owned pending choice, and the guard itself stands unchanged.
- A non-choosing seat renders the same public stage read-only at every version of the battle, from first selection through confirmation, without an exception.
- The six-seat wire regression drives a Minion Battle from first selection through the unanimous agreement to the applied removal with every seat's full presenter set refreshed at every version it observes, and no seat records an exception.
- A commitment naming a revealed card the seat's scoped card list does not carry renders that hero's tray without the card's detail rather than failing the whole refresh - or, if investigation proves the scoped list must always carry that card, the projection is corrected and the invariant asserted; the completion records which reading held and why the playtest projections lacked the card.
- Both complete suites pass at the corrective revision.

Non-goals: No change to the Minion Battle rules or the agreement flow's authority, the wire protocol version, the persisted snapshot schema, Director-mode rendering, or milestone 8.35d's continuation work; no blanket exception isolation in the refresh loop; the end-of-round stall these crashes produced is recorded here, not as its own milestone.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835e-render-the-minion-battle-agreement-stage-on-every-scoped-seat-and-survive-a-scoped-trays-absent-revealed-card-completion-evidence).
