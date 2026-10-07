# Private-online implementation track before 8.36b

Preserved losslessly on 2026-09-09 from [the architecture decision](../Project/PRIVATE_ONLINE_ARCHITECTURE.md). This historical sequence and evidence narrative is not current authorization; [the session brief](../SESSION_BRIEF.md) owns that route.

## Preserved transport source links

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

The exact commands, filters, topology, and pass criteria are owned by [../Testing/PRIVATE_ONLINE.md](../Testing/PRIVATE_ONLINE.md). Milestones 8.31 through 8.35b are complete: the socket adapter and dedicated seatless host, keyed sealed sessions, the gameplay envelope, the bounded command DTO mapped to offered instances, and each seat's own owner-scoped projection on the wire - complete with pending-choice candidate detail and previews, the three resolution envelopes, offer rows, team-agreement statements, and completed-round history - converge under deterministic fault injection. Milestone 8.34 closed this record's reconnect and host-loss rows: a lost connection reserves its seat and pauses authoritative progress, an owner-private reconnect capability plus a rotated connection epoch is the only way back, the retired connection's frames are discarded, a consumed submission identity is answered from its retained result, eligibility ends with the session, and host loss stays terminal. Milestone 8.35a added the primitive address/port/key lobby, six seat claims, table-public readiness, host-derived transition, and bounded compatibility/refusal feedback. Milestone 8.35b hands that transition to the existing owner-scoped HUD through the shared read/client ports: each accepted projection or actionability transition produces an immutable presentation view, a command or agreement submission locks every control until its correlated complete projection arrives, latency and recovery states are explicit, a reconnect grant stays disabled until fresh state arrives, public team-agreement facts remain visible without unauthorized commands, and Leave or reattachment retains neither stale input nor another seat's home hand. Milestone 8.35c then ran this record's exact intended topology as processes: one disposable Win64 build, one dedicated seatless host process and six client processes on the six canonical seats, entering through the primitive lobby, playing a complete match, forcing both team-agreement flows and a mid-match reserved-seat reconnect, and proving terminal host loss in a second cohort - with host and all six clients converging on one authoritative outcome and every process self-exiting. That rehearsal is same-machine loopback evidence only and proves nothing about relay or NAT traversal, external latency, multi-network connectivity, or real-internet reliability. Milestone 8.35f then added the one thing a seat may do that is not a command: a bounded rewind of its own turn, under the same envelope, sequence and idempotency discipline, republished to every seat because a step back changes the shared board. Milestone 8.35g then held a finished turn for its own player: the eleventh pending family crosses the projection detail as a carried row rather than a derived one, the release is a recorded command,. The shortest next integration outcome is proposed milestone 8.36's invited two-network six-human match; this record does not authorize it.
