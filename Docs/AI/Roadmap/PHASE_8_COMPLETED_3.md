# Phase 8 - Completed milestone definitions, third shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-04 during milestone 8.35o's required warning-threshold ownership review. Milestone 8.30 moved here verbatim, the oldest definition the active shard still held, because both existing completed shards (`PHASE_8_COMPLETED.md`, `PHASE_8_COMPLETED_2.md`) were themselves within a few hundred words of their own warning line and could not absorb it without becoming the next ownership review. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

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
