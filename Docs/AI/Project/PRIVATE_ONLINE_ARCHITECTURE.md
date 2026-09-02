# Minimum Private-Online Architecture Decision

Canonical decision record for milestone 8.29. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md). Reassess this record before any dependency upgrade, provider change, public distribution, non-Windows bring-up, dedicated hosting, host migration, account system, matchmaking, or scale beyond six invited players.

**2026-09-01 user-directed amendment.** Invoking this record's own reassessment clause for a provider change and dedicated hosting, the user replaced the selected transport and topology: a dedicated seatless host process over direct TCP sockets replaces the player-hosted UGS Multiplayer Services/Relay-DTLS stack for the private playtest track. All six players are clients of that host, which runs on the operator's machine now and on an operator-run Windows server later. Every other boundary in this record - protocol family and version, the admission tuple, structural owner projections, host-bound seats, snapshot resync, terminal host loss, the transport-neutral port with adapters beneath it, and the excluded scope - stands unchanged. The sections below are amended in place; the superseded UGS/Relay selection remains visible in the alternatives table as the deferred fallback.

## Decision

For the private Windows playtest track, use one dedicated seatless authoritative host process connected to six clients over direct TCP sockets:

- Unity Editor **6000.5.9f1** and URP **17.5.0** remain the project baseline. No networking package is installed; `Packages/manifest.json` and `packages-lock.json` are untouched by this track.
- The byte transport is `System.Net.Sockets` TCP inside one project-local, Unity-free adapter assembly beneath the transport-neutral session port. No relay, no third-party service, no cloud account, and no service credential exists anywhere in the stack.
- The host binds a configured address and port, loopback by default; clients address it by IP and port. A reachable address is operator-supplied - one machine today, an operator-run Windows server later.
- Invite-only admission is carried by an optional pre-shared session key: the host issues a random challenge and a client must answer with proof of the same key. The key also seals the link. **Milestone 8.32 resolved the encryption decision this record owed** for the pre-shared-key AEAD frame layer over `SslStream` with a pinned self-signed certificate: per-connection, per-direction keys are derived with HKDF-SHA256 from the session key and that connection's random challenge nonce, each frame is encrypted with AES-256-CTR keyed by its own strictly monotonic sequence and authenticated encrypt-then-MAC with HMAC-SHA256, and after a verified key proof every frame travels sealed in both directions except a plaintext Reject or Goodbye, which carry no secret and whose forgery is equivalent to the connection reset an on-path party could already send. `SslStream` was rejected because a certificate would have to be generated, distributed and pinned beside a key that already exists, and a stream-level handshake sits badly beneath the bounded frame assembler and the single-threaded pump. An unkeyed session has no key to derive from, stays plaintext, and may bind only the loopback interface - the launch options refuse a non-loopback bind without a key - so an owner-private byte can cross plaintext only where this record's on-path observer does not exist.
- Client identity is one opaque connection-scoped ordinal assigned by the host; there is no account, no durable identity, and no authentication service. Recognizing a returning player is milestone 8.34's reconnect problem and is not solved by this selection.
- Protocol family is `guards-private-online`, initial protocol version **1**. It is independent of socket internals. The application protocol serializes bounded data-transfer objects rather than CLR/Unity objects or `MatchSessionProjection`.
- The host is one serialized executor around the existing Unity-free `IMatchSession`, holds no seat, and plays no hero; all six players are clients. Host loss is terminal for the current match. There is no authority migration.

Milestone 8.31 owns the socket adapter assembly and its conformance boundary; an unexpected engine, assembly, or protocol change blocks that milestone.

## Why this is the minimum supportable choice

Authoritative gameplay is already a synchronous, deterministic, Unity-free session that needs ordered command/projection messages, not replicated scene objects, prediction, rollback netcode, or distributed ownership. Direct TCP supplies exactly ordered bounded byte delivery with nothing else attached: no third-party service, account, credential, quota, free-tier limit, outage window, data-processing agreement, or retention policy. The whole stack is project-local and inspectable, no external terms govern it beyond the Unity engine license the project already carries, and deleting the adapter assembly removes it entirely.

The costs are equally plain. The operator must supply a reachable address and port: on one machine that is loopback, and for internet play it is an operator-run Windows server with a public address, which is what replaces the relay's NAT traversal - the tester-side connectivity burden the 2026-08-28 evaluation weighed moves to the operator, and the rented host is exactly what pays for it. Peers learn the host's address and the host's operating system sees client addresses; the mitigation is that no remote endpoint is ever logged, projected, or forwarded, and connections are identified everywhere by opaque ordinals. The link was plaintext while milestone 8.31 carried only handshake and seat-claim bytes; milestone 8.32 put owner-private projections on the wire and paid the owed prerequisite in the same slice - a keyed session is sealed from the key proof on, and a session without a key is confined to loopback. A reachable port also means anyone who can reach it can attempt admission; the pre-shared session key is the admission control that keeps the session invite-only.

## Alternatives considered

| Option | Relay/NAT and hosting | Fit and cost | License/privacy/operations | Disposition |
| --- | --- | --- | --- | --- |
| Direct host address (raw TCP) | Requires a reachable host address/port; NAT/firewall setup is operator-owned on the dedicated host rather than tester-owned | Fewest moving parts: no package, service, account, or quota, and the adapter assembly is deletable whole | No relay processor or third-party terms, but peers see the host address and the link is plaintext until 8.32's encryption decision | **Selected (2026-09-01 user-directed amendment).** The operator-run dedicated host absorbs the connectivity burden the 2026-08-28 evaluation assigned to testers. |
| UGS Multiplayer Services + Relay + UTP | Managed Relay handles NAT/firewalls and hides peer IPs; player-hosted authority means no dedicated-server bill or operations | Released Unity 6 packages, free allowance comfortably covers the bounded private test, and low-level messages preserve the existing application boundary | UGS terms/DPA/privacy notice and service availability apply; usage and dashboard environment require operator checks | Selected on 2026-08-28; deferred by the 2026-09-01 amendment to the fallback if operator-side reachability fails. Nothing in the codebase depends on it. |
| UGS + Netcode for GameObjects | Same Relay/session services | NGO adds GameObject/MonoBehaviour state synchronization that the existing plain-C# authority does not need | More package and lifecycle surface; encourages a second state model | Rejected for this track. Reassess only if a later approved feature requires replicated Unity objects. |
| Photon Fusion Cloud | Managed punch-through/relay; Host Mode still loses the session with the host unless migration is built | Free Fusion tier supports 100 CCU for one commercial app, but Fusion is an opinionated state-sync stack and adds a second vendor | Separate SDK, cloud account, license, privacy and removal path; dedicated authority needs separately hosted server operations | Rejected: capable but materially larger than command/projection transport for this architecture. |
| Epic Online Services | Free game services and P2P facilities, engine/store independent | No hosting fee, but the native/C# SDK and Unity integration would be owned locally | Separate developer agreement, credentials, SDK update and privacy integration; weaker project-local Unity support path | Rejected for the minimum track; viable fallback if UGS proves unavailable before implementation. |
| Steam Networking/SDR | Relay hides IPs and authenticates Steam users | Strong Windows networking, but ordinary use binds the playtest to Steam identity/ownership and a Steam distribution path; non-Steam use needs custom signaling/coordinator work | Steamworks onboarding, platform SDK, app identity and distribution obligations | Rejected: conflicts with the store-independent private build and no-public-distribution boundary. |
| Dedicated server/managed hosting | Strongest host-tampering and host-loss boundary | Adds deployment, monitoring, credentials, cost and server build operations before the pitch | Production-like operations and data responsibilities | Partially adopted by the 2026-09-01 amendment: a self-operated dedicated seatless authority process with no managed hosting service. A rented Windows host is a deployment target, not a service dependency; managed hosting stays deferred. |

Evidence was checked on **2026-08-28** against Unity's released-package manuals and current Multiplayer Services, Relay, Authentication, pricing, and privacy documentation; Photon, Epic, and Steam first-party documentation supplied the comparison. Links are collected under [Sources](#sources) and remain the evidence trail for the deferred fallback. The direct-socket amendment was recorded on **2026-09-01**; it depends on no external commercial terms, so the UGS terms/limits rechecks formerly scheduled for 8.31 and before 8.36 lapse with the deferral and return only if the fallback is adopted.

## Authority, ownership, and wire boundary

The host process is trusted for rules resolution, seat assignment, ordered execution, version publication, snapshots, and recovery. Nothing else is trusted: there is no service tier, and every client, every received byte sequence, and every gameplay claim inside it are untrusted.

At admission, the host binds one live transport connection, identified by an opaque host-assigned connection ordinal, to one immutable `OnlineSeatId`. Before the match starts, that seat is assigned exactly one existing `PlayerId`, hero, and team through the authoritative participant graph. The binding is host state outside `DirectorCommand`; payload `HeroId`, `PlayerId`, `TeamSide`, seat, or chooser fields never establish caller identity. Reconnect may replace the connection under the session-held reconnect capability milestone 8.34 defines, but never transfers a seat. The host address is a locator and the session key is an invitation gate; neither is authorization to control a seat.

There are three information classes:

- Public: map, units, lives, wave/round/turn state, revealed cards/commitments, public events, connection/ready state without service identifiers.
- Team-shared: only state whose rules or an approved team-owned choice makes it visible to that team.
- Owner-private: the seat's hand, unrevealed commitment identity, owner-only offered commands, and reconnect capability. It is absent from every other seat's serialized projection.

The full director projection, accepted-command history, Undo/restart controls, snapshots, diagnostic bundle, host bind/remote addresses, session-key material, and host-only fault detail are never client projection fields. The host may keep privacy-reviewed local diagnostics. Client logs contain only protocol version, opaque session correlation, own seat, own submission ID, projection version, message kind/size, bounded rejection code, and recovery transition.

## Protocol and compatibility contract

Every application message has a bounded header: protocol family/version, session ID, message kind, payload length, sender connection epoch, monotonic per-direction sequence, and correlation ID. Milestone 8.31's session frames carry that envelope's first slice - magic, protocol version, message kind, and a validated length - and milestone 8.32's gameplay messages carry the remainder: the session correlation published at admission, the sender's connection epoch (fixed at 1 until milestone 8.34 rotates it on reconnect), a strictly monotonic per-direction gameplay sequence, and the correlation a reply echoes; a wrong value is a protocol refusal that closes the connection before any seat-level answer. Client submissions additionally carry a random unique submission ID and expected owner-projection version, both outside the gameplay payload. The gameplay payload is one discriminated, bounded command DTO mapped by the host to an offered typed command; it is never an arbitrary serialized `DirectorCommand` object graph.

Admission succeeds only when these values match the host: protocol `guards-private-online/1`, build identity, snapshot schema **34**, map `official-single-lane` plus its current pinned revision, and every registered content revision/digest. The host also publishes the immutable six-seat roster and assignments. A mismatch is rejected before private projection or gameplay bytes are sent. Unknown message kinds, discriminators, fields whose forward handling is not explicitly declared, or unsupported schema/content revisions reject; they do not downgrade, rebind, or best-effort deserialize.

The host is the sole projection version allocator. Each accepted command increments the authoritative version once, then produces one owner-scoped projection per connected seat from the same committed state. A client may display only its latest complete projection. Deltas are optional later optimization; protocol 1 correctness uses complete bounded projections. A reconnect/resync sends a fresh complete projection after compatibility and seat reauthorization, never a client snapshot.

Upgrade rule: any editor, adapter-assembly, socket-usage, protocol, serializer, schema/map/content identity, authority, or build-target change requires a written compatibility assessment. If wire meaning changes, increment the protocol version and reject the prior version unless an explicit migration is approved and tested. Re-run the Win64 build smoke for adapter/build changes, protocol fault/compatibility tests for wire changes, projection/privacy and replay/restore checks for authority/serialization changes, and the full 8.36 gate before using a changed stack for invited testing.

Removal path: all socket types remain in the Unity-free adapter assembly beneath a transport-neutral application protocol port. Removing the transport deletes that assembly while retaining local seat/session, projection, authorization, codec, fault, and deterministic host tests. A replacement transport must reproduce connection context and ordered bounded byte delivery; it cannot enter Core or become rules authority.

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
| Connection lost before a seat is claimed | Close silently; no seat state exists to preserve and no reservation is created. |
| Client disconnect outside a pending choice | Keep the seat reserved and pause new authoritative progress until reconnect or host termination; no AI/substitute takes control. The reserved seat's return path is milestone 8.34's. |
| Client disconnect while it owns any of the ten pending-decision families | Preserve the exact pending family and ordered answers, pause, then reproject it after authorized reconnect. No default answer or timeout mutation. |
| Disconnect during team-owned choice | Preserve the authoritative designated chooser/team contract; no other seat inherits control unless existing rules authority explicitly offers that answer to it. |
| Reconnect within the live host session | Reauthenticate the same service identity, rotate connection epoch, discard old connection input, send full compatible owner projection, then resume. |
| Host disconnect/process loss/listener loss | Terminal `HostLost`; clients clear actionable UI. No host migration, partial restore, or continuation claim. A new match may be created explicitly. |
| Host unreachable before play (wrong address, closed port, refused connection) | Bounded join/host failure with retry; director/local modes remain usable and no private match is claimed. |

The ten current pending families must each have deterministic disconnect-before-answer, disconnect-after-send-before-response, duplicate-after-reconnect, stale-answer-after-resync, and successful-reconnect cases by milestone 8.34. The host retains one bounded idempotency window sufficient for reconnect/retry and rejects submissions older than it; 8.33 chose its exact size from measured retry traffic - worst per-seat distance 27 across the deterministic walk - and recorded it as `PrivateSessionProtocol.SubmissionIdempotencyWindowEntries` = 256 under that measurement's own growth check.

## Lightweight threat model and limitations

Protected assets are authoritative match integrity, per-seat control, owner-private/team information, compatibility, and useful privacy-safe diagnostics. Relevant adversarial or faulty actions are forged seat/hero claims, a guessed or shared session key, unsolicited connections to a reachable port, captured/replayed submissions, stale clients, malformed/oversized payloads, message delay/reordering/duplication, abusive reconnects, private-data logging, an on-path observer of the plaintext link, and a modified client inspecting every byte it receives.

Controls are structural owner projections, host-side seat binding, offer-surface admission plus final rules validation, protocol/version pins, bounded codecs on a loopback-default listener, the pre-shared session-key challenge, the sealed frame layer on every keyed session, sequence/version/idempotency checks, opaque correlations, log allow-listing, and terminal host-loss behavior. Tests deliberately violate each control. The remaining plaintext surface is bounded and named: an unkeyed session is loopback-only, the pre-proof handshake and the Reject/Goodbye close signals stay readable, and a forged plaintext close is accepted as equivalent to a reset. The session key's strength is the seal's strength - a guessed key reads the session - which is accepted for six invited trusted testers holding an operator-issued key.

The host is operator-run rather than player-run, so no player process holds rules authority - a strictly stronger boundary than the listen host this amendment replaced. The operator can still tamper with the host process, inspect all authoritative state, refuse or fabricate delivery, or alter a locally built binary; client authorization does not make the authority cheat-proof against its own operator. This is accepted only for six invited trusted testers and must be stated in playtest instructions. Production competitive security, anti-cheat, secure secret storage, DDoS guarantees, moderation, accounts, and malicious-host detection are out of scope and require a separate production reassessment.

## Re-baselined implementation track

The dependency-safe sequence remains 8.30 through 8.36a. Estimated implementation/validation effort is **24-41 focused engineering days**, excluding approval latency, service outages, tester scheduling, and corrective milestones. The 2026-09-01 transport substitution is estimate-neutral: package installation and service probes leave the plan while frame-codec and socket-adapter work enters it:

| Milestone | Boundary and exit evidence | Estimate |
| --- | --- | ---: |
| 8.30 | Unity-free serialized host/session port and protocol-neutral result stream; no UGS/package work | 2-3 d |
| 8.30a | Six local owner projections, trusted local seat envelope, version/idempotency/authorization, all ten pending families | 3-5 d |
| 8.30b | Seat-scoped player HUD with no director controls or cross-seat private state | 2-4 d |
| 8.30c | In-process six-seat full-match gate, privacy/ownership/rebuild/restore coverage | 2-3 d |
| 8.31 | Direct-TCP session adapter beneath the transport-neutral port; dedicated seatless host plus six client connections; admission tuple, session-key challenge, and seat claims; loopback rehearsal and build smoke | 3-5 d |
| 8.32 | Network envelope/codec and host admission enforce the already-proven local projection/authorization contract | 2-3 d |
| 8.33 | Complete projection synchronization, sequencing, compatibility, bounded codec, idempotency window, deterministic fault injection | 2-4 d |
| 8.34 | Reserved-seat reconnect/resync for all ten pending families; terminal host loss | 2-4 d |
| 8.35a | Primitive invite-code lobby, six seat claims/readiness, compatibility feedback | 1-2 d |
| 8.35b | Network adapter drives the proven HUD with waiting/rejection/recovery states | 2-3 d |
| 8.35c | Disposable Win64 six-process local rehearsal with fault/privacy diagnostics | 2-3 d |
| 8.36 | Two-network invited six-human match, evidence capture and triage | 1-2 d plus scheduling |
| 8.36a | Fresh analysis-only review and Phase 9 re-baseline | 1-2 d |

Milestones 8.30 through 8.30c are now complete locally. Three deterministic official-roster records run only through all six seat clients, cover the ten pending families, both team-owned sides and all three victory causes, and remain byte-identical across midpoint snapshot/session replacement. Every perspective is checked after every command for owner-private/public visibility and authorized control; repeated scene composition at the three supported desktop viewports retains no stale selection, duplicate handler surface or growing owned resources. This in-process evidence proves no serialized envelope, network fault, reachability, latency, disconnect, reconnect or real-internet behavior.

The exact commands, filters, topology, and pass criteria are owned by [../Testing/PRIVATE_ONLINE.md](../Testing/PRIVATE_ONLINE.md). Milestones 8.31 through 8.33 are complete: the socket adapter and dedicated seatless host, keyed sealed sessions, the gameplay envelope, the bounded command DTO mapped to offered instances, and each seat's own owner-scoped projection on the wire - now complete, with the presentation-fidelity fields (pending-choice candidate detail, previews, resolution envelopes, offer rows, team-agreement statements, completed-round history) carried by 8.33's codec and converging under deterministic fault injection. The shortest next integration outcome is 8.34's disconnect, reconnect, and host-loss policy. No adjacent milestone is authorized by this decision.

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
