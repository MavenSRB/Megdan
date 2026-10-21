# Smart Start — prototype evidence

Return to [preparation owner](SMART_START.md). Audit date: 2026-10-21. This is the application-facing evidence snapshot, not a new implementation authority. Source baseline: `1966d1b38bee65b49f7213475c20de64a7848c9c`; preparation baseline: `691a8d2c`. No game changes, external contact or publication.

## What already works

The self-funded prototype is a playable game, not just a concept or visual mock-up. The following summarizes the implementation owners rather than promising new grant deliverables.

| Capability | Existing behavior and evidence boundary |
| --- | --- |
| Team play and setup | Private Host/Join, individual hero seats, 2v2 through 5v5, 32 configured heroes, four maps, host-selected Waves/Lives. The proposed commercial focus remains 3v3. See [current implementation](CURRENT_IMPLEMENTATION.md). |
| Complete tactical match | Card commitments/reveal, initiative and turns, movement and positional choices, attack/defense and abilities, minions and lane push, defeat/respawn, coins and level-up, victory and per-hero scoreboard. See [implementation](CURRENT_IMPLEMENTATION.md), [progression](HERO_PROGRESSION.md), [combat](COMBAT_AND_CONSEQUENCES.md). |
| Large-map play | Two 4v4/5v5 boards, two independent lane battles/pushes with a shared turn order; either Throne capture ends the match. See [maps](../Maps/README.md). |
| Player interface | Board interaction, hero/card trays, card inspection, legal action/choice presentation, runtime tooltips, turn/phase feedback and optional experimental timers/overlays. See [HUD](MATCH_HUD.md) and [presentation](CONFIGURED_PRESENTATION.md). |
| Multiplayer continuity | Direct keyed TCP sessions, seat ownership and reconnect/resynchronization; compatible build/data required. Not public matchmaking, a hosted scalable service or blanket proof of internet reachability. See [implementation](CURRENT_IMPLEMENTATION.md). |
| Technical foundation | Rules independent of Unity presentation; typed commands/events, deterministic replay, versioned snapshots and bounded player Undo, plus Director diagnostics. These are implemented technical capabilities, not all end-user product features. See [architecture](ARCHITECTURE.md). |
| Experimental cooperative mode | One to three players share three Blue hero seats against Melee/Ranged/Heavy enemy behavior, with shared lobby and host authority. Separate from the grant's core competitive proposition; see [PvE implementation](PVE_IMPLEMENTATION.md). |

The latest Windows W079 package includes the large-map fixes; later W080–W082 changes are source-only. Mac W075 is older and native launch/cross-platform networking are not verified. Exact artifact identities and coverage belong to [build records](BUILDS.md). The current prototype has no audio. Six selected kits' new identities/card presentation, a complete board-visible 3D layer, one production-ready map/free camera, animation/VFX and audio remain next-phase scope. A later AI-assisted vertical slice would be additional self-funded baseline evidence, not retroactively funded work. Existing configured heroes remain technical evidence, not the production-ready funded presentation.

## Reproducible local match audit

Run `./Tools/Get-SmartStartMatchEvidence.ps1` from the repository root. The read-only [audit script](../../../Tools/Get-SmartStartMatchEvidence.ps1) emits JSON with relative source paths, file hashes, event timestamps, claimed-seat counts and online run identities. Raw build logs remain private and unmodified.

Scope: all 404 loose `*.log` files under `Builds`, not only filenames beginning with `match-`. Text/JSON searches found no further gameplay event records. ZIP interiors, external player machines, AppData/persistent-data fallback and absent/deleted logs are outside this inventory; it is not a lifetime total of play. Online metadata is explicitly included, including the newer W050/W059/W079 builds.

| Observed inventory | Count / interpretation |
| --- | --- |
| Timestamped gameplay logs | 67; 67 distinct SHA-256 values, no exact file duplicates |
| Logs with explicit MatchStarted | 42; all distinct start timestamps |
| Started logs with GameEnded | 16; 14 long six-seat sessions and two short four-seat sessions with local seat claims |
| Started logs without recorded GameEnded | 26; not automatically abandoned games |
| Terminal log without MatchStarted | 1; same launch and final `v520 r4 t4 #1877` outcome as the September 26 match, excluded as repeat observation |
| Long six-seat logs without recorded end | 4; three have no negative/local claims, one has five local claims; not counted as completed |
| Online metadata files / distinct run IDs | 257 / 125; BEGIN means capture creation, not proof a match started |
| Online completed run IDs | 21 after grouping host/client records by run ID; two are in Win64Smoke, leaving 19 outside that explicitly named smoke directory |

The 19 non-smoke completion IDs are **not 19 additional matches**. They overlap the timestamped records and include resumed/repeated final-state evidence: two separate September 26 run IDs end at version 520 with the same winner/cause. Excluding a smoke directory does not prove all remaining runs are human playtests. Newer host completion records include W050 (`custom-6p-w3-l3`, final version 657) and W079 (`custom-10p-w5-l5`, version 1291, ThroneCaptured). Their metadata has no start/end timestamps, so no duration or exact play date is inferred from filesystem modification time. Source semantics: [PrivateOnlineEvidenceFile](../../../Assets/GuardsDigital/Network/PrivateOnlineEvidenceFile.cs).

### Timed completed 3v3 cohort

Explicit descriptive filter: MatchStarted plus GameEnded, six distinct SeatClaimed IDs, at least 30 elapsed minutes. All 14 selected logs also have zero negative/local seat claims. The duration threshold separates this descriptive cohort from short technical runs; it does not prove independent human participation. Duration is first GameEnded minus MatchStarted, including any pauses. Distinct starts, chronology and separate terminal records support counting these as distinct sessions, not host/client copies.

| Start date/time (UTC) | Elapsed minutes |
| --- | ---: |
| 2026-09-17 19:37:37 | 104.12 |
| 2026-09-18 19:07:55 | 90.86 |
| 2026-09-22 19:12:46 | 95.80 |
| 2026-09-26 19:38:31 | 72.02 |
| 2026-09-26 20:58:59 | 75.88 |
| 2026-09-29 19:28:35 | 98.51 |
| 2026-09-30 21:30:57 | 90.26 |
| 2026-10-01 19:12:59 | 77.10 |
| 2026-10-01 20:44:31 | 108.74 |
| 2026-10-02 19:35:03 | 128.72 |
| 2026-10-06 19:13:00 | 71.24 |
| 2026-10-06 20:34:15 | 83.14 |
| 2026-10-08 19:10:24 | 94.58 |
| 2026-10-08 21:07:06 | 77.61 |

Totals calculated before rounding: **14 completed sessions, 21.1432 elapsed match-hours**, mean **90.61 minutes**, median **90.56**, range **71.24–128.72**. Starts cover 10 distinct UTC dates across a 22-calendar-day inclusive window (September 17–October 8). The last completion is October 9 in Serbia local time. This establishes repeated, sustained use of the working prototype over several weeks, not merely one demo run.

These logs do not establish unique people, age/MOBA background, voluntary retention, referrals or willingness to pay. Seat labels and online `human=` fields are software classifications, not verified identities. Do not multiply by six to claim verified person-hours. `SessionComplete` means filled seats, not victory. These are existing-prototype results, not testing of the future new content. The earlier provisional claim of 17 started-and-completed logs is corrected to 16; 17 terminal-bearing files include the repeated no-start observation.
