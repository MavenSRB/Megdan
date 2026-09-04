# Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

## Phase 8 - Verified hero content, deterministic 3v3, and private online validation

Phase 8 begins only after the synthetic core match gate. Wasp-specific card work composes proven core actions, keywords, effect operations, queries, modifiers, control flow, and system transitions; it does not defer their first general implementation until a card requires them. The mirrored Wasp setup identities introduced earlier remain an intermediate configuration for the first complete hero gate, not the final pitch roster. After that gate, five additional user-approved heroes enter through source inventory and capability-delta mapping before the final six-distinct-hero 3v3 integration. Once the complete primitive match and URP baseline are proven, a separate private-online track moves the same authority behind a host/session boundary and reaches an invite-only six-human internet playtest before production presentation begins.

The completed definitions of milestones **8.1** through **8.10** - Wasp's source inventory and capability matrix, her verified definitions and both dealt hands, the general card-text seam and the two tray surfaces built on it, her five telekinetic Skills, the declared modifier vocabulary's consumers, the current-card rail, her two kinetic Skills and two basic cards, the tray-faced upgrade grid, the refused empty Clear, and her five printed Attacks - moved verbatim to [PHASE_8_COMPLETED.md](PHASE_8_COMPLETED.md), 8.1 through 8.7a on 2026-08-22 during milestone 8.8a's required warning-threshold ownership review and 8.8 through 8.10 on the same date during milestone 8.11a's; milestones **8.11** through **8.11c** - her Defense cards, the official map underlay and its interaction overlay, the verified card-table visuals, and identity-named effect-choice candidates answered on the board - followed on 2026-08-26 during milestone 8.25a's audit, when two proposed corrective blocks took this shard past its hard limit; and milestones **8.12** through **8.15a** - the Active-effect and continuous trigger windows, High Voltage, her progression and Ultimate unlock, her completed card behaviour and the faced card choices - followed on 2026-08-27 during milestone 8.25d's. Milestones **8.11d** and **8.11e** moved verbatim to [PHASE_8_COMPLETED_2.md](PHASE_8_COMPLETED_2.md) on 2026-08-28 during milestone 8.25f2a's warning-threshold review, and milestones **8.16** through **8.17** - the deterministic Wasp mirror integration gate, its independent card-engine extensibility review, the board-answered Lane-Push respawn placement, and the user's selection of the five additional pitch heroes - followed to the same shard on 2026-08-29 during milestone 8.30's. Milestones **8.18a** through **8.24** - the five hero inventory slices, the capability mapping and 3v3 re-baseline, the Sabina, Tigerclaw and Xargatha completions, and the rail-marker corrective between them - followed to that shard on 2026-09-03 during milestone 8.35a1's required warning-threshold ownership review, which is also where the five 8.18 slices rejoin the split contract they read "above". The 2026-08-28 amendment merging **8.26** into **8.27**, and milestones **8.27** through **8.28b** - the deterministic six-hero 3v3 pitch match gate, the render-pipeline migration to URP, the independent six-hero/URP/networking readiness review, the Unity-free harness restoration that followed it, and the seeded replayable randomness seam - followed to that shard on 2026-09-03 during milestone 8.35f's required warning-threshold ownership review; they are the run between the card-face work and the private-online track's own selection, and 8.29 onward still reads them by id. Milestones **8.25** through **8.25f2a** - the official six-player Quick Game configuration, the after-action repeat pair, the configuration rename, the viewport-scaled HUD and its portrait rails, the card-text repeat declaration, and the verified private card faces with their framing corrections - followed to that shard on 2026-09-04 during milestone 8.35h's required warning-threshold ownership review, which leaves the private-online track standing alone here. They remain active authority under the same `Roadmap/` threshold; this is sharding, not archiving, and their per-milestone evidence is still addressed by SHA from the [Phase 8 archive](../Archive/Phase-Handoffs/PHASE_8.md). This shard keeps 8.29 onward, which is what the next milestone reads against.


### [x] 8.29 Select the minimum private-online architecture and re-baseline the track

Observable outcome: A source-backed architecture decision defines the smallest supportable way for six invited Windows players on the internet to join one private 3v3 match, with one authoritative host/session and one independently owned hero per player.

Decision inputs: [Private-online decision constraints](../Project/ARCHITECTURE.md#private-online-decision-constraints) and [`RISK-ONLINE-001`](../TECHNICAL_RISKS.md#active-risks) carry milestone 8.28a's reviewed host-authority, projection-privacy, submission-identity, compatibility, payload, and recovery boundaries.

Acceptance checks:

- Current Unity-supported transport, relay/NAT traversal, hosting, cost, licensing, privacy, and operational constraints are compared before a provider or SDK is selected.
- The trust boundary, host authority, seat/hero ownership, client-visible projections, version compatibility, join flow, disconnect policy, and failure model are explicit, including duplicate, replayed, stale, malformed, oversized, unauthorized, and out-of-order inputs plus disconnects during each pending-choice family.
- A lightweight threat model identifies trusted and untrusted processes, per-seat command authority, public/team/owner-private information, diagnostic-log sensitivity, host-tampering limitations, and the required safe rejection or terminal behavior. This is scoped to six invited players and does not claim production anti-cheat or public-service security.
- The selected Unity editor, packages/SDKs, transport/service versions, licenses, private-distribution constraints, deterministic/authoritative-code reach, removal/upgrade path, and build compatibility are pinned in a version-controlled dependency decision record. Upgrades that can affect authority, serialization, networking, rendering, or builds require an explicit compatibility assessment and applicable gate rerun.
- The track remains private and invite-only; accounts, public matchmaking, public server browsing, telemetry, monetization, and public distribution remain out of scope.
- Milestones 8.30-8.36, including the lettered local-UI and rehearsal slices, receive evidence-based implementation boundaries and validation commands before the first implementation milestone is authorized.

Decision and re-baseline: [the private-online architecture record](../Project/PRIVATE_ONLINE_ARCHITECTURE.md) initially selected a player-hosted UGS/Relay authority under `guards-private-online/1`. The 2026-09-01 amendment replaced that topology with a dedicated seatless host over direct TCP while retaining owner projections, host-bound seats, snapshot resync, terminal host loss, trust/removal boundaries, and excluded public-service scope. Milestone 8.35b's compatibility assessment advances the current wire to `guards-private-online/2` for a rules-bearing selected-actor field and rejects version 1 without migration. Exact gates remain in [the private-online validation shard](../Testing/PRIVATE_ONLINE.md).

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829-select-the-minimum-private-online-architecture-and-re-baseline-the-track-completion-evidence).

### Completed correction 8.29a

Milestone **8.29a** is complete. Its exact definition remains active authority in [the additional completed Phase 8 definitions shard](PHASE_8_COMPLETED_2.md#x-829a-recompose-and-contain-the-side-hero-trays), and its completion evidence is indexed by the [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-829a-recompose-and-contain-the-side-hero-trays-completion-evidence).

### Completed correction 8.29b and remaining hands-on route before 8.30

Milestones **8.29b**, **8.29b1**, **8.29b1a-8.29b1b**, **8.29d**, **8.29e** and **8.29f** are complete; only deferred **8.29c** remains. Their exact observable outcomes, acceptance checks, closing checks, and non-goals are authoritative in [the Phase 8 hands-on correction shard](PHASE_8_HANDS_ON_CORRECTIONS.md). Milestone 8.30 followed that sequence.

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

### [x] 8.33 Synchronize commands, events, pending choices, and snapshots

Blocked by: Milestone 8.32 must enforce owner-scoped projections and command authorization across the network boundary.

Observable outcome: All clients reproduce the host's public match state and their authorized private projection across full turns, nested Defense reactions, tied Initiative choices, and other pending resolutions without client-owned rules authority.

Acceptance checks:

- Repeatable fault-injection scenarios exercise permitted delay/jitter, duplication, loss, reordering, stale messages, corrupt/incompatible snapshots, and interrupted synchronization; clients either converge on the host's authorized version or enter one explicit recoverable/terminal failure state.
- Same-team tied-Initiative and Minion Battle removal agreement synchronizes each eligible player's latest public statement, incomplete/disagreed/agreed status, the exact standing proposal, and the host's exactly-once family-specific authoritative choice. Delayed, duplicated, reordered, stale, forged, or conflicting statements cannot manufacture agreement, submit twice, apply to a changed removal set, or diverge any client from the host.
- Host and client diagnostics correlate session/content/schema version, command identity, authoritative projection version, and recovery decision without logging prohibited private information.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-833-synchronize-commands-events-pending-choices-and-snapshots-completion-evidence).

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

### [x] 8.35f Give each seat a bounded undo inside its own turn's card resolution

Corrective milestone from the user's live multiplayer testing on 2026-09-03, approved the same day ahead of milestone 8.36's human gate and paired with 8.35d and 8.35e. Undo was the Director's alone - `UndoRestartPresenter` is null for a seat-scoped runtime and `SeatSubmission` deliberately could not name it - so a player who misplayed a step of their own resolution had no way back at all. The user's rule is printed: each player gets an undo, usable only while their own cards are being resolved, clickable step by step all the way back to the beginning of their turn, and never through an Attack, because once a target has been selected the defender's answer is information that cannot be un-shown.

Observable outcome: During its own turn's card resolution a seat presses one UNDO control to step the match back one accepted command at a time, as far as the state its turn began in and no further, and never into or through an Attack whose target it has chosen; every other seat sees the board step back with it, and nothing else about who may do what changes.

Acceptance checks:

- A seat resolving its own turn is published a rewind exactly while the latest accepted command stands above the floor its turn began at; each accepted press reverts exactly one command and moves the authoritative version forward; the presses run out at that turn's own beginning rather than reaching its commitments, the Initiative that selected its actor, or a previous turn.
- After a target is chosen, no sequence of presses ever takes back the target selection or anything inside rulebook p.14's action: the acting seat is published no rewind while the action resolves and is refused by name when it asks anyway, and the floor stands immediately above the settled action.
- The same claim stated over a whole run rather than one position: a seeded seat walk pressing the control at every third state that offers one never lowers the count of Attack target selections in the recorded stream, and the case fails rather than passes when it observed no Attack or took no rewind.
- Off-turn seats, teammates and opponents are published no rewind and are refused `UndoUnavailable`; the Director's own unbounded Undo, Restart and snapshots stay unreachable from every player surface, and the seat's own request carries no seat, no count and no target - the host answers all three.
- A stale expected version and a repeated submission identity rewind exactly once between them, so a double click cannot take two steps; a refused rewind moves no version, no recorded stream and no snapshot.
- Determinism holds across a rewind: the retained prefix replays to the state the rewind left, and that state's snapshot restores to itself.
- Across the wire, six clients converge on the rewound board with every other hero's hidden cards still absent from the bytes; a protocol 2 peer is refused before any payload is interpreted, because the seat record carries the new fact in the middle of it; and a rewind asked while a reserved seat holds the session is refused `SessionPaused` without moving anything and is taken after that seat returns.
- The seat HUD draws the control as a square on the status strip's top edge immediately left of END TURN, glyph only and no words, standing exactly when the host published the rewind and absent from the Director's drawer.

Non-goals: No redo. No undo of commitments, Initiative, agreements, the Minion Battle, end-of-round decisions, or anything outside the acting seat's own resolution; no Director change and no seat restart. No change to the persisted snapshot schema, to rulebook p.14, or to milestone 8.36's execution. The explicit turn release the user amended in on 2026-09-03 is milestone 8.35g's work, not this one's. The pre-existing malformed-checkpoint replay refusal this milestone's own sweep reached at a deep history - which the Director's unbounded Undo answers identically from the same state, and which no part of this milestone introduced - is recorded here and corrected separately.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835f-give-each-seat-a-bounded-undo-inside-its-own-turns-card-resolution-completion-evidence).

### [x] 8.35g Hold a finished turn until its own player releases it

User-approved amendment of 2026-09-03, added to milestone 8.35f's approval the same day once the user saw what the undo could not reach: "If user makes the wrong movement, the turn auto ends, so no undo will fix that. We need to stop auto ending turns." Today a card resolution completing is the hand-off - `TurnResolutionAuthority.AdvanceInitiative` acknowledges the resolved card and selects the next actor inside the same accepted command - so the last action of a turn is unreachable by 8.35f's rewind, whose floor and walls only exist while that turn is still being resolved. A seeded walk shows the shape exactly: at seed 3 an Attack's target selection at command 287 both resolves the attack and ends Arien's turn. This is a digital-UX affordance rather than a rules change: tabletop turn handoff is implicitly consensual, and the release makes that consent explicit online.

Observable outcome: When the acting hero's card resolution completes, the match holds still in a `turn-release` pause owned by that hero's seat instead of advancing. The seat's UNDO keeps working through the pause and reaches back into the resolution it just finished. END TURN, pressed again, is the release: it runs the end-of-turn timing and hands the turn on to the next actor, or into end of round where milestone 8.35d's continuation takes over.

Acceptance checks:

- A completed card resolution leaves the match in the release pause rather than selecting the next actor: the same round, the same turn index, and the acting hero's own seat owning the one standing decision. The pause is the eleventh declared pending family, crossing the projection and the wire like the other owned ones.
- The release is one recorded `DirectorCommand`, so replay, retained-prefix Undo, snapshots and restore see it as the ordinary stream entry it is. End-of-turn timing - the turn-only cancellation checkpoint and `TurnEnded` - fires at the release and not at resolution completion, so an undo inside the pause never crosses checkpoint state.
- Milestone 8.35f's rewind is available through the whole pause and steps back into the just-finished resolution under its unchanged floor and walls; the amended availability rule is "while this seat's resolution is active OR while its completed resolution awaits release".
- Only the seat whose hero resolved the turn may release it: a teammate's and an opponent's release are both refused without moving the version, the stream or the snapshot, and the Director releases directly from its own door.
- The pause follows every resolution completion uniformly, including one caused by an explicitly labelled END TURN command - a Movement Stop, an effect stop or a Hold - so an explicit stop needs a second press and that second press is the turn's point of no return. A hero that passed or holds an empty hand resolves no card and gets no pause. A resolution stopped by the game ending gets none either, because nothing further is asked of anyone.
- A mid-turn Lane Push standing at its own checkpoint is answered first and the release pause opens after it, so two decisions never stand at once; the same holds for a triggered effect that stopped on a choice.
- The persisted pause moves the snapshot schema to v35 with the full re-pin: every superseded version is rejected rather than migrated, the pinned-constant ledger's six documents state v35, the corpus fixtures are renamed and re-digested, and the byte delta against v34 is measured rather than computed.
- Every driver and seeded journey that ends a turn presses the release: the deterministic match loop, the player smoke driver, the private-online rehearsal driver and the Play Mode journeys. Their re-derived seeds are recorded, because one extra command per turn reroutes every seeded trajectory.
- Both complete suites pass at the final revision.

Non-goals: No rules change - rulebook p.12's turn structure is untouched and the release adds no step to it. No redo, no change to milestone 8.35f's floor or walls, no Director undo/restart change, no presentation pacing, no wire protocol change beyond the new command kind and the new pending family, and no 8.36 execution.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835g-hold-a-finished-turn-until-its-own-player-releases-it-completion-evidence).

### [x] 8.35h Isolate the HUD presenter fan-out so one broken panel degrades alone

User-approved hardening of 2026-09-03 - the user's "isolate - queue it up" - raised by the invited playtest, where Andrej's client froze whole while the authority kept accepting submissions to version 97. The diagnosis names an amplifier rather than a bug: `MatchFeatureCoordinator.RefreshAll` calls its seven presenters in a straight line, so the second one throwing silently skips the five behind it on every projection change, and one view-model defect presents as a dead client instead of one dead panel. Both milestone 8.35e view-model defects were match-ending for exactly that reason, and the next presenter to throw would do the same.

Observable outcome: One presenter throwing during a refresh leaves every other panel rendering - the board, the tray, END TURN and the seat UNDO included - and the failure is reported by name rather than swallowed, so a broken panel is visible and named instead of silently stale.

Acceptance checks:

- Each of the seven presenter refreshes runs independently: a throw in any one of them neither prevents nor corrupts the others, and the panels both before and after the failing one hold the newest projection's version.
- The failure is never silent. A composed HUD reports each failed panel by name, with its exception, to an injected failure sink; a composition supplying none raises only after every panel has had its turn, so a swallowed throw cannot be introduced by omission.
- The coordinator publishes which panels failed on the latest refresh, so a surface can draw a named failed state rather than leaving a stale panel indistinguishable from a current one.
- `TeamAgreementHudPresentation` reads a scoped seat's agreement with the tolerance its own hero-name lookup already uses: no bare indexer or `Single` over a projection-carried collection remains beside a `TryGetValue` fallback in the same method, and an agreement naming a card that seat cannot see renders a degraded label instead of throwing.
- Presentation-only. No rule, authority, projection, wire, schema or command change, and nothing any panel draws changes while nothing throws.

Closing check: `MatchFeaturePresenterTests.OneThrowingPresenter_LeavesEveryOtherPanelRenderingAndNamesTheFailure` is recorded failing at the pre-fix revision, where a throwing round presenter stops the four panels behind it from refreshing at all, and passing after the isolation with those panels holding the current version and the round panel named as the failure.

Non-goals: No presenter's own defect is fixed here and no view-model reader is rewritten beyond the one tolerance alignment named above; no retry, no fallback rendering of a failed panel's content, no diagnostics redesign, no rules, authority, projection, wire or schema change, and no other milestone of the pre-package UX batch.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835h-isolate-the-hud-presenter-fan-out-so-one-broken-panel-degrades-alone-completion-evidence).

### [x] 8.35i Restore a snapshot captured while a choice is paused inside an Attack

User-approved corrective, queued after 8.35g from milestone 8.35f's own finding. 8.35f's rewind sweep reached a refused Undo at a history of 1,262 commands - `Undo replay failed without changing the match: Snapshot payload is malformed or internally inconsistent` - and the diagnosis relocated it out of the undo machinery entirely: `MatchSession.Reconstruct` replays from a retained checkpoint, and deserializing that pre-image threw. Replaying the same stream into a fresh host and round-tripping after every command named the real cause at command 1,067: **`Snapshot pending effect choice names node 'noble-other', whose capability no longer requests a choice.`** That is milestone 8.29c's documented recurrence, and it is shared authority rather than the HUD path 8.29c assumed, which is why it is corrected here rather than there.

The cause is a question asked with half of what it reads. Candidates are never written to a payload - a capability's come from asking the trusted handler again, which is what stops a payload inventing an authoritative option - so a restore rebuilds the question. Arien's Noble Blade binds its selection to `EnclosingTargetBeforeConsequences`: "another unit adjacent to the target of this Attack". The restore path rebuilt that question with no enclosing Attack at all, so the origin resolved to nobody, the handler offered no choice, and the payload was refused for describing a pause its own content "no longer" requested. The content had never changed.

Observable outcome: A match paused on a capability choice inside an Attack's card text captures and restores like any other, offering the same candidates it offered before, and the Director's Undo and a seat's rewind both step through such a state instead of refusing at it.

Acceptance checks:

- A snapshot captured while a capability choice is paused inside an Attack restores, and the restored pause names the same node, the same candidates and the same selection bounds - then takes its own published answer. Asserting only that it restores would pass a build that restored the pause and forgot what it offered, which is the same defect one layer down.
- Every state of the reproducing command stream round-trips through a snapshot, checked after each accepted command rather than at the end, because the defect was invisible until one specific intermediate state was captured. The case fails if the stream never passes through a choice paused inside an Attack, so it cannot pass by walking a match that never reaches the position.
- The enclosing Attack is rebuilt by the authority's own `EnclosingAttackOf` rather than by a second builder in the serializer - the method the live path already uses, whose own record refuses "a second place the same fact could be wrong".
- No byte moves and no version changes. The Attack is written after the frame that pauses inside it, so the frame is read as a function of it, which is the answer milestone 8.23 gave for the opposing hero the payload also names late. Snapshot schema stays v35 and the wire stays `guards-private-online/4`.
- The serializer's generic wrapper names its cause in the message rather than only chaining it, because callers surface a refusal by `Message` alone and an `InnerException` nobody reads is a cause nobody reads. Diagnostic only: the rejection reason and the refusal behaviour are unchanged.

Closing check: `AttackCardTextSnapshotRestoreTests` - both cases - recorded failing with the fix off at its one behavioural site and passing with it on.

Non-goals: No tolerance is widened and nothing is caught more broadly; a payload that really is malformed is refused exactly as before. No schema, wire, command, rules or presentation change. Milestone 8.29c is not reopened: its own HUD-path symptom remains unreproduced, and what is fixed here is the shared-authority mechanism 8.35f recorded under its name. The offer/refusal defect this milestone's scans surfaced separately - an `AnswerEffectChoice` the projection offers and the rules refuse with `AttackActionRejected: TargetUnavailable`, reproducible at `6bfe61f9` on seeds 12 and 20 of the official six-player fixture at budget 4,000 - is reported and stays unclaimed pending the user's call.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835i-restore-a-snapshot-captured-while-a-choice-is-paused-inside-an-attack-completion-evidence).

### [~] 8.36 Validate a private six-human internet 3v3 and run the friends playtest

Blocked by: Milestone 8.35c must pass the local six-client playtest-build rehearsal.

Observable outcome: Six humans on separate clients complete an invite-only full 3v3 match over the internet using a disposable private Win64 playtest build and primitive UI before production presentation begins.

Acceptance checks:

- The exact build and content/schema revision are recorded and privately distributed only to the invited testers.
- The session proves host/join, six stable seats, private hands, all normal player and team-owned choices, complete turns/rounds, reconnect/resync, match completion, and explicit failure handling across at least two external networks. At least one same-team tied Initiative and one confirmable Minion Battle removal are resolved through milestone 8.30n's public unanimous-agreement flows rather than a first-click, captain, timer, or automatic fallback.
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
