# Future Validation Gates and Recipes

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

## Official board-map specification validation

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-BoardMapSpecification.ps1
```

Expected result: the validator exits with code `0` and reports that reusable invariants and synthetic map-local identity isolation pass, followed by 292 cells, 210 playable spaces, 82 terrain cells, seven zones, 809 physical connections, 530 playable connections, and 40 spawn points split evenly between Red and Blue. The reusable layer verifies map-local `SpaceId`, `ZoneId`, and `SpawnPointId` contracts, immutable `(MapId, revision)` selection, selected-map-local coordinate queries, references, zone adjacency, ordered-Lane structure, and structural invariants without official-map constants. Bounded in-memory fixtures reuse all three child IDs and their coordinate under different synthetic `MapId` values. The official layer separately verifies every reviewed count/location and the zone/space/spawn/Lane digest, including all rules-relevant zone metadata.

## Runtime performance baseline

Regression evidence, not a production performance target. Capture in a development Editor with the Unity Profiler or `ProfilerRecorder` over the complete 292-cell map and a representative two-round director flow: steady-state frames, complete map rebuilds, snapshot-only captures, retained-prefix Undo replay, and the accepted-command refresh metric timing the configured presenter submission through accepted `MatchSession.Execute`. At milestone 7.9, extend that accepted-command capture across fixed reported command-history lengths from empty/short through the representative complete synthetic match; record refresh time, managed allocation per sample, and their growth against history length. `Phase4RuntimeHygieneTests` owns the repeatable resource-lifecycle, unchanged-HUD projection/allocation, supported-object-discovery, and malformed rule-event-log envelope assertions that accompany a capture.

Current reference figures, recorded at runtime baseline revision `7ad51a7` (milestones 4.4d and 4.4h; the full captures and their environment are archived), with noise-tolerant investigation thresholds:

| Metric | Baseline | Threshold |
| --- | ---: | ---: |
| Max steady-state managed allocation | 1,418 B/frame | 64 KiB/frame |
| p95 main-thread frame time | 0.170 ms | 10 ms |
| Average map rebuild | 1.849 ms | 25 ms |
| Owned map native resources | 11 | zero growth |
| Average snapshot-only capture | 0.183 ms | 5 ms |
| Undo replay | 28.203 ms | 250 ms |
| Accepted-command refresh, 44 samples | 1.254 ms focused, 0.401 ms complete-suite | 25 ms |
| Accepted-command refresh, complete-match history | staged 1v1 at 7.9b: 0.217 ms .NET, 0.359 ms Mono at 501 commands; **current official 3v3 at 8.29d: 0.202 ms .NET, 0.622 ms Mono at 768 commands** | 25 ms |
| Accepted-command refresh managed allocation | staged 1v1 at 7.9b: 82,296 B empty, 667,952 B worst; **current official 3v3 at 8.29d: 125,496 B empty, 745,304 B worst; .NET only** | 2 MiB/sample |
| Accepted-command allocation growth across a complete match | superseded at 7.9b: +105,584 B median under full-history copying; **.NET only** | 256 KiB (superseded) |
| Accepted-command allocation growth, retained history shared | staged 1v1 at 7.9b: +21,068 B median; **current official 3v3 at 8.29d: +15,900 B median, history 0-5 against 750-755; .NET only** | 64 KiB |
| Authoritative captured-byte growth, completed-round cache retained | staged 1v1 at 7.9d: +700 B; **official 3v3 at 8.27: +5,271 B; deliberate fresh-cache violation: +17,452 B** | 12 KiB |
| Undo replay, complete-match history | superseded at 7.9b: 429 ms Mono, 171 ms .NET, full-prefix replay of 502 commands | 600 ms (superseded) |
| Undo, retained-point reconstruction | staged 1v1 at 7.9b: 30.9 ms Mono, 56.4 ms .NET at 502 commands; **current official 3v3 at 8.29d: 70.417 ms Mono, 62.725 ms .NET at 768 commands** | 150 ms |
| Six-player decoded snapshot payload | **24,733 B measured at 8.25**, against 10,988 B for the staged 1v1 setup re-measured at the same revision (the mirror is 10,064 B); the 6,633 B figure this row carried is an older schema's | reference only |
| End-of-round leveling stage, six heroes | **current seed 21 at 8.29d: all six serialized choosers observed across 30 stage commands; worst step 1.401 ms Mono, 1.953 ms .NET** | 25 ms |

The complete-match rows are measured by `CompleteMatchRuntimeCaptureTests`, which replays fixed reported seed 21's 768-command, 12-round official six-player match to each sampled history length and measures the one command after it. Milestone 8.29d changed the legal action set and therefore re-measured the deterministic seed and current figures without changing any standing threshold. Milestone 7.9 measured the staged 1v1 first; 7.9b/7.9d closed its growth gaps, and 8.27 closed F-CORE-10 by moving the same capture to the official six-player configuration and deterministic driver the gate proves.

The allocation threshold was **64 KiB/sample and had never been measured**: that figure was the per-*frame* steady-state number from the row above it, reused for a per-*command* metric nothing had sampled. Milestone 7.9 re-baselined it to 1 MiB, roughly 40% above the staged 1v1 maximum, with user approval on 2026-08-20. Milestone 8.27's official 3v3 fixture then measures 1,367,600 B at the expensive sixth commitment. That is scale, not a returned copying mechanism: median early-to-late allocation grows only 15,076 B against the unchanged 64 KiB growth line. The absolute line is therefore re-derived at 2 MiB, about 53% above the measured official maximum, preserving the prior headroom rationale rather than silently widening it.

The growth rows are `RISK-PERF-001`'s own prediction, measured at 7.9 and closed at 7.9b/7.9d; the superseded figures remain because the correction is measured against them. At official 3v3 scale, retained-history allocation grows **+15,076 B** and captured serialization grows **19,006 -> 24,277 B (+5,271)**. The latter crosses the old staged 4 KiB line because six heroes' bounded current cards, levels, items, and in-flight round state are larger. A deliberate regression replacing the aggregate's retained `CompletedRoundsSerializationCache` with a fresh cache per capture measures **19,006 -> 36,458 B (+17,452)**. The re-derived 12 KiB line is more than twice the legitimate residual and about seven tenths of the reintroduced unbounded mechanism, so it preserves discrimination on both sides. Refresh time remains 0.324 ms .NET and 0.369 ms Mono against 25 ms.

`F-ER2-08`'s full-prefix Undo was the row that grew most, the one place a measured threshold was widened rather than corrected, and is closed at 7.9b; both superseded figures are kept above. The 250 ms line came from 28.203 ms over the two-round director flow at milestone 4.4d; the complete 20-round match retains 502 commands, a full-prefix Undo replayed all of them at **429 ms on Unity's Mono and 171 ms on .NET**, and milestone 7.9 re-baselined to 600 ms against the slower runtime with the fix separately approved on 2026-08-20. Milestone 7.9b is that fix: the session retains the aggregate's constant-cost pre-image every 32 accepted commands, and Undo reconstructs from the latest retained point at or before the surviving prefix, so the replayed segment is bounded by the interval however long the match is. Measured at complete-match length it is **30.9 ms on Mono and 56.4 ms on .NET** - a complete-match Undo back at what the two-round Undo cost at 4.4d - and the 150 ms line sits roughly 2.7x above the slower tier while every full-prefix replay of this match measures above it, so it fails if the reconstruction ever replays the whole prefix again. `UndoCheckpointReconstructionTests` holds the retained point to being a derived cache: reconstructions above, exactly on, and below a retained point are each byte-identical to a fresh full-prefix replay, and the complete-loop gate still proves the undone match replays back into the same ending.

**Two of these rows are measurable on one tier only.** `GC.GetAllocatedBytesForCurrentThread` returns a constant zero on Unity's Mono, so every allocation figure above is the cross-platform harness's. `CompleteMatchRuntimeCaptureTests` detects that rather than assuming it, records `runtimeReportsAllocation=False`, and asserts that a runtime reporting nothing reports nothing everywhere - so the Windows tiers record an honest absence instead of passing an allocation check vacuously. Time is reported on every runtime and is asserted on every tier.

A threshold crossing requires investigation and an approved fix, a rationale-preserving re-baseline, or explicit user acceptance; it never authorizes speculative optimization. Milestone 7.9 performed that measurement - Undo replay and refresh time crossed nothing, the allocation line was re-baselined for the reason recorded above, and the growth term crossed - and milestone 7.9b was the approved fix it scheduled, closing both crossings with the figures and tightened lines recorded above.

The last two rows were recorded with no 3v3 baseline so `RISK-SCALE-001` would be measured rather than reasoned about. The payload row remains reference-only. Milestone 8.27 closes the other half: the official seed-5 match observes all six heroes as serialized one-at-a-time Level-Up choosers across 31 stage commands, with a worst authoritative step of **2.633 ms on Unity Mono and 0.800 ms on .NET** against 25 ms. The independent outcomes remain rule-equivalent to p.15's simultaneous instruction, while the one-resume-location continuation deliberately makes up to five director seats wait. At director scale that residual UX is accepted: one controller answers every seat, the observed cost is bounded, and concurrent pauses would require a new durable-continuation architecture rather than a local optimization. `RISK-SCALE-001` is retired with those measured bounds and explicit acceptance.

**Milestone 8.25 measured the payload half**, because it is the first milestone at which 108 card instances and 108 definitions exist. The official six-player setup's decoded payload is **24,733 B**, against **10,988 B** for the staged Wasp-versus-Arien 1v1 setup and **10,064 B** for the verified mirror, all three captured at the same source revision from `MatchSession.CaptureSnapshot` at setup - before any command - and decoded from base64. The serializer is pure Core, so the figures are the same on either runtime.

**Two things about that measurement are worth keeping.** First, the **6,633 B reference this row carried was an older schema's** and had not been re-measured since; the payload has roughly grown by two thirds at 1v1 scale over the schema versions between. A comparison against it would have reported the roster's cost and the schema's growth as one number, which is why the 1v1 reference is re-measured here rather than quoted. Second, `RISK-SCALE-001` predicted ~16.6 KB by arithmetic on measured unit costs - 93 B/instance from 7.5 and ~42 B/definition from 7.3 applied to the old base. Against the re-measured base the same arithmetic gives ~20.7 KB, and the real figure is 24,733 B, so the per-unit costs have grown too and the risk's own note that this half is "arithmetic on measured unit costs rather than a measurement" is exactly why it was scheduled to be measured. **Nothing crosses a threshold**: this row is reference-only, and the line that actually bounds it - average snapshot-only capture, 5 ms - is measured on the loaded configuration by `Phase4RuntimeBaselinePlayModeTests`, which composes the six-player match from milestone 8.25 onward.

Completed milestones' gate recipes and their recorded run evidence moved verbatim to the [testing archive](../Archive/TESTING_HISTORY.md#completed-gate-recipes-and-run-evidence-routed-2026-08-14-milestone-61ag) on 2026-08-14.

## Invited six-human internet playtest protocol (milestone 8.36)

Recorded at milestone `8.36`, whose deliverable is six invited people completing one private 3v3 over the internet across at least two external networks. Milestone `8.35c` proved the same topology on one machine, and a same-machine rehearsal establishes no relay, NAT traversal, external latency, multi-network or real-internet claim; obtaining exactly those is why this protocol exists. What it produces is validation evidence, not a retained pitch deliverable, and it authorizes no public distribution.

**Preconditions.** Every check in the `8.36` row of [PRIVATE_ONLINE.md](PRIVATE_ONLINE.md) is green at the one revision the build comes from: the final focused `8.32`-`8.35b` harness, Edit Mode and Play Mode filters; `Tools/Invoke-PrivateOnlineRehearsal.ps1 -Topology SixClient -FaultPlan Full` against a freshly built player; the [player-build](#player-build-smoke-gate) and [board visual](#board-visual-smoke-gate) smoke gates; and the complete unfiltered harness together with both complete unfiltered Unity suites. A change freeze then runs to the end of the milestone: crashes, desynchronization, latency symptoms, confusing flows, unreadable state and tester feedback are recorded and triaged, while no rule and no code changes. A gate-blocking defect ends the playtest and becomes a separately approved corrective milestone, after which the build, the gates and this protocol all begin again at the corrected revision.

**Build and identity.** One disposable private Win64 player is built fresh at that revision - `-ReuseBuild` and any cached artifact are never evidence - and every process in the session, the operator's host and all six clients, runs that same copy. Build identity is part of the admission tuple, so a differently stamped build is refused before any private byte is sent and six admitted clients are themselves proof that all seven processes agree on it. Record with the evidence: the source revision, protocol `guards-private-online/2`, snapshot schema 34, map `official-single-lane` with its pinned revision, the registered content digest, and the per-build stamp the build writes as `GUARDS_BUILD: STAMP`. Keep the build log, because the ordinary lobby path does not restate the tuple the way the rehearsal host does.

**Distribution boundary.** The exact build is distributed privately to the six invited testers and to nobody else, and who received it is recorded. No public link, store listing, mirror or onward redistribution; the package and the returned logs stay private material under the standing repository boundary. Distribution is the user's own action and no automated step performs it.

**Host runbook.** The host is one dedicated seatless process the operator runs. It holds authority, plays no hero, and all six players are clients of it.

1. Forward the chosen TCP port to the host machine and allow it inbound in the Windows firewall. The protocol's default is 45677; any free port may be used instead.
2. Choose a session key of real length - random, at least twenty characters - and issue it to the six testers over a private channel that is not this game. It is both the invitation and the seal, because the frame keys are derived from it, so a guessed key reads the whole session. Hosting anywhere but loopback without one is refused rather than allowed, but the build cannot judge a weak key: its strength is the seal's strength and that part stays the operator's.
3. Start the host with `GuardsDigitalSmoke.exe -guardsPrivateLobby -logFile host.log` - the package's `HOST - Start Server.bat` runs exactly this - type the interface to bind (`0.0.0.0` for every interface, or the machine's own address), the port and the session key into the entry form, and press `HOST`. Do not pick a seat: pressing `HOST` without one is what makes the process seatless.
4. Confirm the log line `GUARDS_ONLINE: LOBBY_HOSTING port=<port> keyed=true` before anyone is invited in. Since milestone `8.35a1` the build refuses this misconfiguration itself - an unkeyed session may bind only loopback, from the lobby exactly as from the command line - so a public host that started at all is a keyed one. The line stays in the runbook as the cheap confirmation that what started is what was intended; `keyed=false` on a public bind is no longer reachable, and on loopback it simply says the session is local and unsealed.
5. Leave the host running for the whole match. Every admission, seat claim, readiness change, submission, agreement, resync, reservation and reclaim is written here with its authoritative version, and this log is what every client log is compared against.

The `-guardsPrivateHost` command-line role is the rehearsal's host and is deliberately not the one used here: it stops the session five seconds after the sixth seat is claimed if nothing else happens and fails at its own elapsed timeout, both correct for an automated run and fatal for people who talk between turns.

**Operator trust, stated to testers.** The architecture record's threat model requires this in the playtest instructions, so it is tester-facing text rather than a note: the machine hosting the match holds every card and decides every rule, whoever runs it can read anything including another player's hand and could change what the game does, and nothing in this build prevents that. The session key is an invitation and a seal against outsiders, not protection from the host. This is accepted only because six invited people already trust the operator; it is not a competitive service.

**Tester instructions.** `Tools/PlaytestTesterInstructions.txt` is the tester-facing text, ships inside the package, and carries no real address, port or key. In outline: launch `GuardsDigitalSmoke.exe -guardsPrivateLobby -logFile <name>.log`, because a normally launched build shows no lobby at all - the package ships `PLAY - Join Match.bat` and `HOST - Start Server.bat`, double-click wrappers around exactly that line that name the log for the tester; type the operator's address, port and key; pick the assigned seat row - seats are `red-player-01` to `red-player-03` and `blue-player-01` to `blue-player-03`, are assigned in advance so two testers never race for one, and cannot be changed without leaving and rejoining; press `JOIN`, then `READY`. The match begins by itself when all six seats are claimed and ready; no one presses start. If the connection drops, the seat is held and the whole match pauses until that player returns, so nobody else can act meanwhile; the lobby reappears over the board offering `RECONNECT` and `LEAVE`, and `RECONNECT` is the way back into the same seat. At the end, each tester returns their log file privately. At least two of the six must be on different external networks, and which tester was on which network is recorded.

**Required live scenarios.** The session must produce, and the evidence must name: six stable seats through host/join with private hands and every normal player and team-owned choice; complete turns and rounds to a finished match; at least one same-team tied-Initiative disagreement followed by a revised unanimous preference; at least one confirmable Minion Battle removal changed after partial agreement and then unanimously confirmed - both through milestone `8.30n`'s public unanimous-agreement flow and never through a first-click, captain, timer or automatic fallback; the approved bounded latency, disconnect and reconnect cases including at least one deliberate mid-match disconnect and reconnect; and explicit failure handling if the host or a client is lost terminally, where host loss is terminal for that match and no seat inherits authority.

**Evidence, convergence and privacy review.** Assemble the host log and all six client logs under `tmp/milestone-8-36-playtest/` with the build log and the recorded identity tuple. The host log must show six distinct seat claims, the match start, and the authoritative versions its accepted submissions and agreements moved. Each client log must show its own seat, the version its HUD attached at, each reconnect it made, and one `GUARDS_ONLINE: MATCH_OUTCOME seat=<seat> version=<n> cause=<cause> winner=<team>` line; the host writes the same line without a seat. Convergence is the comparison of those seven lines: every cause and winner identical, each seat naming itself. Then review the whole bundle - no log may carry an `ip:port`, the session key, or a degradation pattern:

```powershell
$logs = Get-ChildItem tmp/milestone-8-36-playtest -Filter *.log
foreach ($log in $logs) {
    $text = Get-Content -LiteralPath $log.FullName -Raw
    [regex]::Matches($text, '\b(\d{1,3}\.){3}\d{1,3}:\d+') | ForEach-Object { "ENDPOINT $($log.Name): $($_.Value)" }
    if ($text.IndexOf($sessionKey, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { "KEY $($log.Name)" }
    foreach ($p in @('Shader.*not found','Failed to load','is missing','NullReferenceException')) {
        [regex]::Matches($text, $p) | ForEach-Object { "DEGRADED $($log.Name): $p" }
    }
    [regex]::Matches($text, 'MATCH_OUTCOME[^\r\n]*') | ForEach-Object { "$($log.Name): $($_.Value)" }
}
```

Any output but the outcome lines is a finding. The diagnostic vocabulary is allow-listed by the architecture record - protocol version, opaque session correlation, own seat, own submission ID, projection version, message kind and size, bounded rejection code, recovery transition, and the public outcome - so a line outside it is a leak to report rather than a curiosity. Record tester observations and feedback alongside the logs and triage them without changing anything.

**What this proves and does not.** It proves that six people on separate machines and at least two external networks reached one authoritative ending over the selected direct-socket transport with owner-private state confined to its own seat. It does not measure performance, does not establish reliability under adverse networks, does not exercise NAT shapes other than the ones those six testers happened to have, and says nothing about a public service, accounts, matchmaking or anti-cheat.

**Completion.** Both complete unfiltered Unity suites and the complete harness must be green at the applicable final revision; the pre-playtest runs stand as that evidence when no code changed afterwards, and any corrective milestone re-baselines them. The repeat rule for later revisions is the [post-polish multiplayer regression gate](#post-polish-multiplayer-regression-gate) below.

## Post-polish multiplayer regression gate

Milestone `9.16` owns the final-revision multiplayer regression after presentation polish. It reuses the exact six-client local process/build topology selected at milestone `8.29` and proven at milestone `8.35c`; milestone `8.29` must add the executable command and bounded fault recipe here once those details exist.

At the exact `9.16` source and executable-content revision, build the disposable private Win64 validation player, start six local clients in six distinct seats, and complete a full match through the polished player UI. Exercise the approved disconnect/reconnect and resynchronization cases. Evidence records the source, build, content/schema, provider/SDK/configuration, topology, and recipe revisions and proves all of the following:

- every client converges on the host's authoritative public outcome;
- no client receives another seat's hidden hand or pre-reveal commitment identity;
- cross-seat hero commands and team choices are rejected without partial mutation;
- reconnect and resynchronization restore the same seat and control ownership without transfer, duplicated input handlers, stale selection, or stale actionable prompts; and
- privacy-reviewed host/client diagnostics disclose no owner-private state.

Repeat the milestone `8.36` real-internet playtest only when affectedness or observed failure makes its earlier evidence stale. A repeat is required when a change since the accepted `8.36` revision touches the provider or SDK version/configuration, relay/NAT/lobby/invite or connection establishment, transport delivery, network serialization/version compatibility, timeout/latency/disconnect/reconnect/host-loss behavior, or another path whose correctness depends on separate networks or real internet conditions. It is also required when the local six-client gate or its diagnostics exposes a connectivity, synchronization, privacy, or ownership failure that cannot be closed confidently without multi-network evidence. Presentation-only changes with unchanged online/session/projection behavior, and failures reproduced and closed wholly within the local topology, do not by themselves require another invited playtest.

The local gate does not establish relay/NAT traversal, external-latency, multi-network, or internet-reliability claims. Any required internet repeat uses a separately identified disposable private build and the milestone `8.36` privacy/distribution boundaries; it is validation evidence, not the retained pitch deliverable.

## Declarative player-path journeys - not scheduled

Recorded 2026-08-25, for the batched hero intake [Roadmap/POST_PITCH.md](../Roadmap/POST_PITCH.md) declares. It is not worth building for Tigerclaw and Xargatha; it is the change that decides whether a hero costs a week or a day once the roster grows past this one.

The permanent gate above is satisfied today by roughly fifteen hand-written `[UnityTest]` journeys per hero, and they are the largest constant per-hero cost - the only term that has not decayed across Arien, Brogan and Sabina. The reason it is compressible is that the gate's own per-decision requirements are **identical for every decision in every journey**: the old prompt advances or clears, exactly one projected invocation is accepted, authoritative state and immutable projection agree, the next visible HUD/board state expresses that projection, a stale repeat cannot act, no `EngineFault` is emitted, and no dead, anonymous, duplicated, orphaned or unreachable control remains. Only three things vary between one decision and the next: which scene arm it starts from, which control is addressed and by which input route, and what the board is expected to look like afterwards.

The proposal is to express those three as data - a per-hero table of `(scene arm, ordered steps, expected observations)`, where a step names a control locator and an input kind - and to let one shared runner apply the seven proofs at every step. `WaspUiPlayerDriver` already owns the click mechanics, and milestone 8.20c's `StandardCardFootprintCheck` is the precedent that this works under the gate: it is hero-neutral, enumerates the hero's own `AllDefinitions`, and collects its failures rather than stopping at the first.

Three properties must survive, and they are what makes this worth designing rather than just writing:

- **The gate must stay un-fakeable.** Its strength today is that a row's shapes are derived from printed content, that `PlayerPathRow.Prove(shape, journey)` makes "declared but unproved" unrepresentable, that journeys resolve by reflection so a rename fails, and that an orphan journey fails too. A declarative journey must therefore still surface as one named `[UnityTest]` the matrix binds to by name - generated through a test-case source, not replaced by a table row that asserts nothing.
- **A step that finds no control must fail rather than skip.** That is the standard failure of data-driven UI suites and it would silently hollow out every row at once.
- **Genuinely bespoke journeys stay hand-written.** The pausing path that must continue across snapshot restore and retained-prefix Undo, the restart and presentation rebuild, and the targeted non-batch Game View inspection are one-offs the format should not try to express. Compressing the repetitive fifteen is the whole prize; compressing the last three would cost more than it saves.
- **Every step must name the node it may answer, and a runner that auto-confirms is wrong.** Milestone 8.22 hit this by hand: an unqualified "press whatever confirm is standing" pressed the *next* node's control, because Troop Movement's printed repeat carries a confirm beside its decline, so a blind confirm after the destination click accepted the repeat instead of leaving it to be answered. `ConfirmIfStillStanding` now names the node it is allowed to confirm. A data-driven runner is exactly the shape that would do this on every repeat card in the roster at once, and it would pass - the journey reaches its end and the board looks plausible, while the decision the row exists to prove was never put to anybody.

## Player-build smoke gate

Added at milestone 6.1d. An Editor suite cannot observe a player that ships with an unresolved shader, a missing resource, or a type the build stripped: every Edit Mode and Play Mode test runs inside the Editor, where those assets are always present. This gate builds the real artifact, runs it, and requires the running player to say what it reached.

Run from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-PlayerBuildSmoke.ps1
```

The script builds through the fixed Editor entry point `GuardsDigital.EditorTools.PlayerBuildEntryPoint.BuildWindowsSmokePlayer`, which targets Windows 64-bit standalone from the enabled scenes and writes to the gitignored `Builds/Win64Smoke/`. It then launches the player with `-batchmode -nographics -guardsSmokeTest`. That flag is the only thing that makes `MatchSceneCompositionRoot` attach `PlayerSmokeTestDriver`; a normally launched build constructs no test hook at all.

Expected result: exit code `0` and `GATE PASSED`. The player log must contain, in order:

```
GUARDS_SMOKE: BEGIN
GUARDS_SMOKE: SCENE Milestone1_2_HexGrid
GUARDS_SMOKE: COMPOSED heroes=6 cards=108 version=0
GUARDS_SMOKE: COMMAND_ACCEPTED kind=CommitCard version=1 commitment=CardCommittedFacedown
GUARDS_SMOKE: PAUSE_ANSWERED transition=... step=... options=...
GUARDS_SMOKE: ROUNDS_PLAYED rounds=2 ...
GUARDS_SMOKE: PASS
```

`COMPOSED` proves the map resource loaded and the configured match composed inside the player. `COMMAND_ACCEPTED` proves a real typed `DirectorCommand` went through `IMatchSession` and advanced the authoritative projection - not that the scene merely opened. Milestone 8.4 re-verified both pinned numbers against the content change that could have moved them: the configured session stopped dealing synthetic cards and dealt two mirrored Wasp seats instead, and `heroes=2 cards=36` was unchanged because p.5 deals eighteen cards per seat either way - five in hand and thirteen in the deck - so the marker line moved nowhere and that gate needed no edit beyond saying so.

**Milestone 8.25 is the change that moved it**, which is what the 7.9a documentation correction predicted when it gave this line an owner. The configuration a player loads is now rulebook p.5's six-player Quick Game setup, so the same per-seat arithmetic reads `heroes=6 cards=108`: six printed sets of eighteen, one per seat, no two seats sharing one. The `version=0` marker is unchanged, because it is the projection version before any command and not a content figure.

**Milestone 8.27 closes the inherited player-launch block.** The disposable Windows player built with exit code 0, launched successfully under the same local policy, reported `COMPOSED heroes=6 cards=108 version=0`, accepted a real commitment, answered all six second-round Level-Up pauses, reported `ROUNDS_PLAYED rounds=2`, emitted `PASS`, and exited 0. The prior 8.25 Application Control refusal remains historical environment evidence, not a current limitation.

Milestone 7.9 added the last two required markers, because one accepted command proves the stack survived the build and does not prove the built player can be played. `PAUSE_ANSWERED` and `ROUNDS_PLAYED` come from a smoke path that plays two whole rounds, answering whichever decision the projection is waiting on with that decision's own first projected option - deterministic without a seed, so one build always writes one log. Two rounds rather than one because milestone 7.6 established that an ordinary end of round only stops on p.15's Upgrade a Card from the second round onward, and a built player that reaches a pause it cannot answer completes nothing. The path never derives legality: every branch reads projected invocations or published offers, exactly as the HUD does.

The gate fails on any of: a build result other than `Succeeded`; a missing executable; a player that does not exit within the timeout; a non-zero player exit code; a missing pass marker; or a log matching a degradation pattern such as an unresolved shader, a failed load, or a `NullReferenceException`. The marker check is independent of the exit code on purpose - a player that degrades silently still exits zero, which is the exact failure this gate exists to catch.

Required at: milestone `6.1d`, where it was first proven; the Phase 6 gate `6.13`; the Phase 7 gate `7.9`; the Phase 8 T3 gates `8.16`, `8.27`, `8.28`, and `8.36`, added by the 8.2 re-baseline applying the 7.9a correction, because 8.28 changes the render pipeline and 7.9's own recorded conclusion argues this list should be longer rather than shorter; the pre-build regression gate; and milestones `10.1` and `10.2`, which already depend on a launchable build. Rerun it whenever the always-included shader list, the enabled scene list, the render pipeline, `Assets/Resources`, an assembly definition, or a stripping setting changes.

Milestone 7.9 is the reason `7.9` is on that list, and the run is the argument for the list being longer rather than shorter. The gate had not been run since milestone 7.8a replaced the HUD's immediate-mode drawing with a runtime UI Toolkit panel, because 7.8a is not a milestone this gate is required at. The player it built logged 71 warnings and 17 `NullReferenceException`s out of `UITKTextHandle.ShapeText`. The player states the cause itself: the ICU data its text shaper needs ships only when the **build contains at least one `PanelSettings` asset**, and a HUD that created its settings entirely at runtime gave the build none. Present with a graphics device as well as under `-nographics`, so it was a defect in the artifact and not a headless artefact, and invisible to every Editor tier because the Editor has the data loaded either way. Fixed by committing `Assets/Resources/UI/GuardsRuntimePanelSettings.asset` and copying the panel from it; the gate then reported zero of both. The asset's own `m_ICUDataAsset` field is empty on disk and is not what carries the data - its presence in the build is - so do not "fix" that field.

Do not run this gate as a substitute for the Unity test suites. It proves the artifact runs; it does not prove the rules are right.

## Board visual smoke gate

Added at milestone 8.31b, because the player-build smoke gate above proves a player *runs* and never looks at what it *draws*. Every other visual check in this project is an Editor capture, and the Editor holds every shader variant and asset a build may strip. In that gap, every hex tile over the official board artwork rendered opaque white in every built player - hiding the entire playfield - while both complete suites, the smoke gate and every inspected capture stayed green. A human saw it in a screenshot; nothing else could.

Run from the project root:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Invoke-BoardVisualSmoke.ps1
```

The script builds through the same fixed entry point, then launches the player **windowed** - not `-nographics`, since a player that draws nothing cannot be photographed - with `-guardsBoardCapture <path>`. That flag is the only thing that attaches `BoardVisualCaptureDriver`, which renders the board camera into a fixed 1920x1080 texture, writes it as PNG, reports whether the official artwork was underneath, and quits. Rendering through the camera rather than grabbing the screen is deliberate: the window is hidden, so there is no swapchain to capture from, and the picture then holds the board with none of the overlay HUD across it.

The script then reads the capture and judges a central block of it: it fails when over half the block is flat near-white, when over half is flat near-black, or when it holds fewer than twelve distinct colours. A drawn board reads as many colours; one flat colour is an opaque overlay, an unrendered frame, or a missing board.

Expected result: exit code `0` and `GATE PASSED`, with a reported near-white fraction at or near zero. Milestone 8.31b recorded both sides of the transition against the same build path: **86.9% near-white over 147 distinct colours before the fix, and 0.0% over 327 after it, on two consecutive fresh builds**.

Two consecutive builds matter here and are not ceremony. The first attempted fix - shipping a transparent template material so the build would retain the blended variant - passed this gate once and then failed the next build identically, which is how variant retention was found to be an unreliable thing to depend on. The fix that holds removes the dependency instead: a fully transparent fill is not rendered at all. Run this gate twice across separate builds whenever a change depends on what a shader variant does.

Required at: any change to a board overlay, highlight, rim or underlay material; any change to how a material's surface state is set; any always-included shader, render pipeline, or stripping-setting change; and alongside the player-build smoke gate at the Phase 8 online gates. Its cost is one build plus a few seconds.

This gate cannot see a rule, a layout, or a colour choice. It answers exactly one question the Editor cannot: does the built player draw a board at all.
