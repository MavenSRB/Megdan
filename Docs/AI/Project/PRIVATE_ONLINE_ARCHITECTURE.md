# Minimum Private-Online Architecture Decision

Canonical decision record for milestone 8.29. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md). Reassess this record before any dependency upgrade, provider change, public distribution, non-Windows bring-up, dedicated hosting, host migration, account system, matchmaking, or scale beyond six invited players.

## Decision

For the private Windows playtest track, use one player-hosted authoritative process connected to five clients through Unity Gaming Services (UGS) Multiplayer Services sessions and Relay:

- Unity Editor **6000.5.7f1** and URP **17.5.0** remain the project baseline.
- Select released `com.unity.services.multiplayer` **1.2.0** for private session/join-code grouping, Relay allocation, and service lifecycle.
- Select released `com.unity.transport` **2.6.0** as the byte transport, using Relay **DTLS** and reliable sequenced delivery for protocol messages. Do not add Netcode for GameObjects or Netcode for Entities.
- Use the Authentication package version resolved by Multiplayer Services 1.2.0 and pinned in `Packages/packages-lock.json` when milestone 8.31 installs the stack. Sign in anonymously; the UGS player ID is service identity, not gameplay authority or a durable account.
- Use one UGS development environment for the private track. Do not enable Matchmaker, Friends, Vivox, Cloud Code, Cloud Save, Analytics, telemetry, or Distributed Authority.
- Protocol family is `guards-private-online`, initial protocol version **1**. It is independent of UTP wire internals and UGS session metadata. The application protocol serializes bounded data-transfer objects rather than CLR/Unity objects or `MatchSessionProjection`.
- The host is one serialized executor around the existing Unity-free `IMatchSession`. Host loss is terminal for the current match. There is no authority migration and no dedicated server.

The selected packages are not installed by this decision milestone. Milestone 8.31 owns the manifest/lock change and must verify that the resolved dependency graph contains only released versions compatible with the pinned editor. The lockfile becomes the exact transitive dependency record; an unexpected version or package blocks that milestone.

## Why this is the minimum supportable choice

Unity documents the unified Multiplayer Services package as the Unity 6 route replacing standalone Lobby and Relay SDKs. Its sessions provide join codes and can use Relay, while Relay supplies a public endpoint that traverses NAT/firewalls and prevents peers from learning one another's IP addresses. Unity Transport is the lower-level byte transport and can be used without a high-level GameObject synchronization framework. That fits this project: authoritative gameplay is already a synchronous, deterministic, Unity-free session and needs ordered command/projection messages, not replicated scene objects, prediction, rollback netcode, or distributed ownership.

The private six-player scale is within the currently published UGS free allowances: Relay's first 50 average monthly CCU and up to 150 GiB monthly bandwidth are free, and Lobby includes 10 GiB monthly per regional group. UGS is pay-as-you-go beyond those allowances; without payment details, exceeding a free limit can block project services. The private test owner must therefore check usage before each external rehearsal and must not treat the free tier as an availability guarantee.

UGS adds operational and privacy obligations. Relay currently retains personal data for 30 days. Authentication creates a UGS player ID and retains its personal data until deletion; Unity describes the developer as controller and Unity as processor under GDPR, requires the game's own privacy notice, and applies its DPA. The private build therefore collects no display name, email, gameplay analytics, chat, or free-form lobby metadata. Logs use local opaque seat/session correlations, never access tokens, IP addresses, another seat's hand, or pre-reveal commitment identity. Anonymous credentials are cached locally and are not recoverable after their token is lost; this is acceptable for a disposable private playtest but is not an account solution.

Package use and private player distribution remain subject to each installed package's included license and the valid Unity engine license; UGS use is additionally governed by Unity's Terms of Service and Unity Cloud Additional Terms. Milestone 8.31 must inspect and retain the resolved packages' license/notices in their normal package locations and stop on any term incompatible with the private build. No decision here authorizes public distribution, sublicensing a standalone SDK, removal of notices, or use outside a Unity game.

## Alternatives considered

| Option | Relay/NAT and hosting | Fit and cost | License/privacy/operations | Disposition |
| --- | --- | --- | --- | --- |
| Direct UTP host | Requires a reachable host address/port; exposes host addressing and leaves NAT/firewall setup to testers | Fewest packages, but external connectivity becomes tester-operated and fragile | No relay processor, but exposes network coordinates and creates support burden | Rejected: does not meet the minimum internet join/privacy outcome. Retain only as a local diagnostic adapter if useful. |
| UGS Multiplayer Services + Relay + UTP | Managed Relay handles NAT/firewalls and hides peer IPs; player-hosted authority means no dedicated-server bill or operations | Released Unity 6 packages, free allowance comfortably covers the bounded private test, and low-level messages preserve the existing application boundary | UGS terms/DPA/privacy notice and service availability apply; usage and dashboard environment require operator checks | **Selected.** Smallest first-party path with the required join and privacy properties. |
| UGS + Netcode for GameObjects | Same Relay/session services | NGO adds GameObject/MonoBehaviour state synchronization that the existing plain-C# authority does not need | More package and lifecycle surface; encourages a second state model | Rejected for this track. Reassess only if a later approved feature requires replicated Unity objects. |
| Photon Fusion Cloud | Managed punch-through/relay; Host Mode still loses the session with the host unless migration is built | Free Fusion tier supports 100 CCU for one commercial app, but Fusion is an opinionated state-sync stack and adds a second vendor | Separate SDK, cloud account, license, privacy and removal path; dedicated authority needs separately hosted server operations | Rejected: capable but materially larger than command/projection transport for this architecture. |
| Epic Online Services | Free game services and P2P facilities, engine/store independent | No hosting fee, but the native/C# SDK and Unity integration would be owned locally | Separate developer agreement, credentials, SDK update and privacy integration; weaker project-local Unity support path | Rejected for the minimum track; viable fallback if UGS proves unavailable before implementation. |
| Steam Networking/SDR | Relay hides IPs and authenticates Steam users | Strong Windows networking, but ordinary use binds the playtest to Steam identity/ownership and a Steam distribution path; non-Steam use needs custom signaling/coordinator work | Steamworks onboarding, platform SDK, app identity and distribution obligations | Rejected: conflicts with the store-independent private build and no-public-distribution boundary. |
| Dedicated server/managed hosting | Strongest host-tampering and host-loss boundary | Adds deployment, monitoring, credentials, cost and server build operations before the pitch | Production-like operations and data responsibilities | Deferred beyond the private pitch; not justified for six invited players. |

Evidence was checked on **2026-08-28** against Unity's released-package manuals and current Multiplayer Services, Relay, Authentication, pricing, and privacy documentation; Photon, Epic, and Steam first-party documentation supplied the comparison. Links are collected under [Sources](#sources). Current commercial terms and service limits remain external facts and must be rechecked at milestone 8.31 and before milestone 8.36.

## Authority, ownership, and wire boundary

The host process is trusted for rules resolution, seat assignment, ordered execution, version publication, snapshots, and recovery. UGS services are trusted only for authenticated service identity, session membership, join-code discovery, and relayed delivery. Every client, every received byte sequence, and every gameplay claim inside it are untrusted.

At lobby admission, the host binds one authenticated UGS player ID and one live transport connection to one immutable `OnlineSeatId`. Before the match starts, that seat is assigned exactly one existing `PlayerId`, hero, and team through the authoritative participant graph. The binding is host state outside `DirectorCommand`; payload `HeroId`, `PlayerId`, `TeamSide`, seat, or chooser fields never establish caller identity. Reconnect may replace the connection for the same authenticated service identity and session-held reconnect capability, but never transfers a seat. The join code is an invitation locator, not authorization to control a seat.

There are three information classes:

- Public: map, units, lives, wave/round/turn state, revealed cards/commitments, public events, connection/ready state without service identifiers.
- Team-shared: only state whose rules or an approved team-owned choice makes it visible to that team.
- Owner-private: the seat's hand, unrevealed commitment identity, owner-only offered commands, and reconnect capability. It is absent from every other seat's serialized projection.

The full director projection, accepted-command history, Undo/restart controls, snapshots, diagnostic bundle, UGS identifiers, access/session tokens, and host-only fault detail are never client projection fields. The host may keep privacy-reviewed local diagnostics. Client logs contain only protocol version, opaque session correlation, own seat, own submission ID, projection version, message kind/size, bounded rejection code, and recovery transition.

## Protocol and compatibility contract

Every application message has a bounded header: protocol family/version, session ID, message kind, payload length, sender connection epoch, monotonic per-direction sequence, and correlation ID. Client submissions additionally carry a random unique submission ID and expected owner-projection version. The gameplay payload is one discriminated, bounded command DTO mapped by the host to an offered typed command; it is never an arbitrary serialized `DirectorCommand` object graph.

Admission succeeds only when these values match the host: protocol `guards-private-online/1`, build identity, snapshot schema **34**, map `official-single-lane` plus its current pinned revision, and every registered content revision/digest. The host also publishes the immutable six-seat roster and assignments. A mismatch is rejected before private projection or gameplay bytes are sent. Unknown message kinds, discriminators, fields whose forward handling is not explicitly declared, or unsupported schema/content revisions reject; they do not downgrade, rebind, or best-effort deserialize.

The host is the sole projection version allocator. Each accepted command increments the authoritative version once, then produces one owner-scoped projection per connected seat from the same committed state. A client may display only its latest complete projection. Deltas are optional later optimization; protocol 1 correctness uses complete bounded projections. A reconnect/resync sends a fresh complete projection after compatibility and seat reauthorization, never a client snapshot.

Upgrade rule: any editor, direct or transitive package, UGS service mode, protocol, serializer, schema/map/content identity, authority, or build-target change requires a written compatibility assessment. If wire meaning changes, increment the protocol version and reject the prior version unless an explicit migration is approved and tested. Re-run package/import and Win64 smoke for dependency changes, protocol fault/compatibility tests for wire changes, projection/privacy and replay/restore checks for authority/serialization changes, and the full 8.36 gate before using a changed stack for invited testing.

Removal path: all UGS and UTP types remain in Unity adapters beneath a transport-neutral application protocol port. Removing UGS deletes the service/session/Relay adapter and package graph while retaining local seat/session, projection, authorization, codec, fault, and deterministic host tests. A replacement transport must reproduce authenticated connection context and ordered bounded byte delivery; it cannot enter Core or become rules authority.

## Failure and disconnect policy

All input is length-checked before allocation-heavy decode and semantically validated before mutation. One malformed input rejects that submission; repeated malformed/oversized or protocol-invalid input disconnects that client. No rejection advances state or projection version.

| Condition | Required result |
| --- | --- |
| Duplicate or replayed submission ID | Return the cached result for an identical already-completed submission when retained; otherwise reject `DuplicateSubmission`. Never execute twice. |
| Stale expected projection version | Reject `StaleProjection`, send no mutation, and offer one bounded current-projection resync. |
| Future version or out-of-order client sequence | Buffer nothing for gameplay; reject and require resync. Reliable transport ordering is not authorization. |
| Malformed, unknown, or oversized message | Reject before session mutation; disconnect on protocol violation or repetition. |
| Command not in the seat's current offered surface | Reject `UnauthorizedCommand` even if it is otherwise rules-legal. |
| Payload identity conflicts with trusted seat ownership | Reject `OwnershipMismatch`, record privacy-safe host diagnostic, and disconnect on repeated attempts. |
| Service/session/build/schema/map/content/protocol mismatch | Refuse lobby admission or reconnect before any private projection. |
| Client disconnect outside a pending choice | Keep the seat reserved and pause new authoritative progress until reconnect or host termination; no AI/substitute takes control. |
| Client disconnect while it owns any of the ten pending-decision families | Preserve the exact pending family and ordered answers, pause, then reproject it after authorized reconnect. No default answer or timeout mutation. |
| Disconnect during team-owned choice | Preserve the authoritative designated chooser/team contract; no other seat inherits control unless existing rules authority explicitly offers that answer to it. |
| Reconnect within the live host session | Reauthenticate the same service identity, rotate connection epoch, discard old connection input, send full compatible owner projection, then resume. |
| Host disconnect/process loss/Relay allocation loss | Terminal `HostLost`; clients clear actionable UI. No host migration, partial restore, or continuation claim. A new match may be created explicitly. |
| Relay/UGS unavailable before play | Bounded join/host failure with retry; director/local modes remain usable and no private match is claimed. |

The ten current pending families must each have deterministic disconnect-before-answer, disconnect-after-send-before-response, duplicate-after-reconnect, stale-answer-after-resync, and successful-reconnect cases by milestone 8.34. The host retains one bounded idempotency window sufficient for reconnect/retry and rejects submissions older than it; exact size is chosen from measured projection and retry traffic in 8.33, recorded as a protocol constant, and covered by a growth check.

## Lightweight threat model and limitations

Protected assets are authoritative match integrity, per-seat control, owner-private/team information, compatibility, and useful privacy-safe diagnostics. Relevant adversarial or faulty actions are forged seat/hero claims, guessed join codes, captured/replayed submissions, stale clients, malformed/oversized payloads, message delay/reordering/duplication, abusive reconnects, private-data logging, and a modified client inspecting every byte it receives.

Controls are structural owner projections, host-side seat binding, offer-surface admission plus final rules validation, protocol/version pins, bounded codecs, sequence/version/idempotency checks, DTLS Relay transport, opaque correlations, log allow-listing, and terminal host-loss behavior. Tests deliberately violate each control.

The player host can tamper with its process, inspect all authoritative state, refuse or fabricate delivery, or alter a locally built binary. Relay, DTLS, authentication, and client authorization do not make a player-hosted authority cheat-proof. This is accepted only for six invited trusted testers and must be stated in playtest instructions. Production competitive security, anti-cheat, secure secret storage, DDoS guarantees, moderation, accounts, and malicious-host detection are out of scope and require a dedicated/server-authoritative reassessment.

## Re-baselined implementation track

The dependency-safe sequence remains 8.30 through 8.36a. Estimated implementation/validation effort is **24-41 focused engineering days**, excluding approval latency, service outages, tester scheduling, and corrective milestones:

| Milestone | Boundary and exit evidence | Estimate |
| --- | --- | ---: |
| 8.30 | Unity-free serialized host/session port and protocol-neutral result stream; no UGS/package work | 2-3 d |
| 8.30a | Six local owner projections, trusted local seat envelope, version/idempotency/authorization, all ten pending families | 3-5 d |
| 8.30b | Seat-scoped player HUD with no director controls or cross-seat private state | 2-4 d |
| 8.30c | In-process six-seat full-match gate, privacy/ownership/rebuild/restore coverage | 2-3 d |
| 8.31 | Install pinned packages; anonymous auth; MPS private session/join code; Relay-DTLS UTP host plus five connections; package/build smoke | 3-5 d |
| 8.32 | Network envelope/codec and host admission enforce the already-proven local projection/authorization contract | 2-3 d |
| 8.33 | Complete projection synchronization, sequencing, compatibility, bounded codec, idempotency window, deterministic fault injection | 2-4 d |
| 8.34 | Reserved-seat reconnect/resync for all ten pending families; terminal host loss | 2-4 d |
| 8.35a | Primitive invite-code lobby, six seat claims/readiness, compatibility feedback | 1-2 d |
| 8.35b | Network adapter drives the proven HUD with waiting/rejection/recovery states | 2-3 d |
| 8.35c | Disposable Win64 six-process local rehearsal with fault/privacy diagnostics | 2-3 d |
| 8.36 | Two-network invited six-human match, evidence capture and triage | 1-2 d plus scheduling |
| 8.36a | Fresh analysis-only review and Phase 9 re-baseline | 1-2 d |

Milestones 8.30 through 8.30c are now complete locally. Three deterministic official-roster records run only through all six seat clients, cover the ten pending families, both team-owned sides and all three victory causes, and remain byte-identical across midpoint snapshot/session replacement. Every perspective is checked after every command for owner-private/public visibility and authorized control; repeated scene composition at the three supported desktop viewports retains no stale selection, duplicate handler surface or growing owned resources. This in-process evidence proves no serialized envelope, network fault, Relay/NAT traversal, latency, disconnect, reconnect or real-internet behavior.

The exact commands, filters, topology, and pass criteria are owned by [../Testing/PRIVATE_ONLINE.md](../Testing/PRIVATE_ONLINE.md). The shortest next integration outcome is 8.31: introduce the pinned package/service/Relay variables only after the local boundary they must preserve is green. No adjacent milestone is authorized by this decision.

## Sources

- [Unity Multiplayer Services SDK overview](https://docs.unity.com/en-us/relay/mirror)
- [Unity Multiplayer Services 1.2.0 released-package listing](https://docs.unity3d.com/ja/current/Manual/com.unity.services.multiplayer.html)
- [Unity Transport 2.6.0 released-package listing](https://docs.unity3d.com/ja/current/Manual/com.unity.transport.html)
- [Unity session network lifecycle and custom network handlers](https://docs.unity.com/en-us/mps-sdk/manage-session-network-connection)
- [Unity Relay servers, NAT/firewall behavior, IP privacy, and lifecycle](https://docs.unity.com/en-us/mps-sdk/networking/relay-servers)
- [Unity Relay protocols](https://docs.unity.com/relay/networking)
- [Unity Gaming Services pricing](https://unity.com/products/gaming-services/pricing)
- [Unity UGS pricing and free-limit blocking behavior](https://docs.unity.com/en-us/services/pricing-and-billing)
- [Unity Relay privacy and retention](https://docs.unity.com/mps-sdk/privacy/privacy-overview-relay)
- [Unity Authentication approaches](https://docs.unity.com/en-us/authentication/approaches-to-authentication)
- [Unity Authentication privacy](https://docs.unity.com/en-us/authentication/privacy-and-consent/overview)
- [Unity Terms of Service](https://unity.com/legal/terms-of-service)
- [Unity Cloud Additional Terms](https://unity.com/legal/cloud-terms-of-service)
- [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license)
- [Photon pricing and licensing](https://doc.photonengine.com/photon/current/pricing)
- [Photon topology comparison](https://doc.photonengine.com/photon/current/photon-products)
- [Epic Online Services licensing](https://onlineservices.epicgames.com/licensing)
- [Epic Online Services developer agreement](https://onlineservices.epicgames.com/services/terms/agreements?lang=en-US)
- [Steam Datagram Relay](https://partner.steamgames.com/doc/features/multiplayer/steamdatagramrelay)
