# Guards Digital - Session Brief

Last updated: 2026-10-21

## Purpose and authority

Read once at startup. This brief alone owns current authorization and next routing. [Workflow](ROADMAP.md) owns execution; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; 32 configured heroes, private host/join play, 2v2 through 5v5, host-selected Waves/Lives and four maps including two large two-lane boards. Snapshot schema v60 persists lane identity/allocation; wire protocol /54 carries live per-lane Wave pools. See [current implementation](Project/CURRENT_IMPLEMENTATION.md) for precise behavior and limits.

Current private Windows package: **W090 Main Fixes**, `Builds/Playtest-W090-main-fixes.zip`. It includes executable `main` through W-082: lane-scoped Heavy immunity, one-Life Undo restoration and Shift+hover compact trays, plus W079. Stamp `1.0-W090-main-fixes+20261021172601-1966d1b3`; SHA-256 `995faffb3ba65f73156e5e62ab1c8152ea176d4f374314b0ef15cdc93401bdbd`. Identity and evidence: [build record](Project/BUILDS.md). Older packages remain preserved.

## Current authorization and next route

Mac remains W075 (`Builds/Playtest-W075-large-lobby-fix-mac.zip`), universal Intel/Apple Silicon, PvP/PvE; SHA-256 `e71e239f890d34c7a957e41584f588126f4f0825fb1f696fd78fddda9a4fed3b`. It lacks later fixes and cannot join W090. Build/bundle/archive checks passed on Windows; native Mac launch, rendering and cross-platform networking remain unverified. See [build record](Project/BUILDS.md).

- Last completed work: **W-098**.
- Active authorized work: **none**.
- Next proposed work: **none**.

[Smart Start dossier](Project/SMART_START.md): `codex/smart-start-preparation`, documentation only; no implementation or submission authorization.

W072 added both saved 900x900 two-lane maps for 4v4/5v5. It preserves one shared initiative order, gives each lane its own active Battle Zone and push progression, and ends the whole match when either lane reaches the opposing Throne. Green's snow, water and forest centers and lava's bridge center are separate playable off-lane Zones, never terrain or lane-push spaces. Hosts choose official per-lane or shared-total Waves. Five-player teams retain one full rail and four thin token/card/current-card rails. Source-only; no package or publication.

W076 corrects those compact rails: current-card markers retain their established card proportions and the empty Active-label strip no longer leaves dead space below the turn-card row. W082 replaces ordinary hover expansion with explicit Shift+hover and swaps only that hero's rail instead of rebuilding both columns; pointer leave restores the compact form. Violet active-source outlines and click selection are preserved. Focused structure and both-large-map/two-viewport interaction, geometry, identity stability, boundary and rendered checks passed. Included in W090 Windows; W079 and W075 Mac remain unchanged.

W078 corrects W077's rejected shortage explanation: every printed minion glyph in either large map's active Battle Zones receives its matching minion. Each team/lane now has a third Melee in reserve for defending zones; initial waves stay unchanged. Every lane zone, snapshot restoration, joined-client identity and both-map rendering are covered, including the bottom lava glyph. Included in W079 Windows; the W075 Mac package remains unchanged. Exact inventories and authority: [Maps](Maps/README.md#reviewed-two-lane-minion-spawn-inventory).

W081 preserves the experimental one-Life hero cost through Undo/rollback checkpoints. The reported second Red defeat followed Director Undo; a retained snapshot restored the official Tier cost until this fix. Included in W090 Windows; W079 and W075 Mac remain unchanged.

W062 makes unseated hosts full-information observers without player-command authority. PvE binds seated and unseated host Undo to live encounter authority and keeps its drawer pointer-reachable above the enemy overlay. Focused regressions and rendering passed; rules, snapshot 59, wire /52 and W059 packages are unchanged.

W061 retains Attack strike-time spaces through public/network coordinates, so immediate Lane Push cannot redirect delayed presentation to a same-identity adjacent-zone respawn. Exact-scene, consumer and rendered checks passed; rules, snapshot 59, wire /52 and W059 packages are unchanged.

W060 completes the latest PvE playtest repair batch: seatless-host live viewing, inert Minion Battle, pointer-safe Level Up, fresh-process reserved-seat reclaim, unresolved-plan/available-Defense token pips, host-routed Blue Initiative ties and typed category colours across enemy faces/rails/pips. Focused rules, presenter, real-loopback lobby, shared indicator and non-batch rendered checks passed. Source-only; W059 packages remain unchanged.

W053 makes the existing seat-scoped PvP Undo rules available to Blue seats in PvE, including commitment withdrawal and the turn floor plus attack, interaction and other-seat walls. A manually selected committed local seat remains selected while its Undo is available, without changing ordinary automatic awaited-seat switching. Focused engine, wire and real-input shared-lobby checks passed. Source-only; W050/W051 packages remain unchanged.

W058 keeps each public orange enemy card in its existing tray slot and dims it immediately after resolution, reusing the spent enemy-Defense treatment. Current/unresolved cards and full-card inspection stay unchanged; Undo and replay re-derive the correct state, and focused solo/co-op Play Mode checks passed. Source-only; W050/W051 packages remain unchanged.

W057 removes the final Ranged spawn from each PvE wave: current source now has 3 Melee, 2 Ranged and 1 Heavy in wave one, then 2 Heavy and 1 Ranged in wave two. Exact content/composition checks and solo/co-op six-row rail checks passed. Source-only; W050/W051 packages retain their prior roster.

W056 gives white numeric card-modifier overlays a thicker black outline without moving or resizing them. Focused EditMode/PlayMode checks and targeted Game View inspection passed; source-only, with W050/W051 unchanged.

W055 makes the large transient phase announcements and final-ten timer countdowns use the viewing player's team colour: the existing blood red for Red and dark blue for Blue. Focused EditMode/PlayMode checks and targeted Game View inspections passed; source-only, with W050/W051 unchanged.

W048 saved PvE logs and W049 generated enemy faces/board-gated turns are included in W050. Unspawned enemies have no commitments or turn assignments and enter the turn roster on spawning. Generated faces retain portrait proportions through the existing artwork importer; original card dimensions and preview scaling are unchanged. W046 package/main remain preserved.

W052 separates Turn timer from Card Modifiers in Experimental and makes keyed-host address discovery fall back from local UPnP to bounded HTTPS public-IPv4 lookups. Responses are accepted only as exact public IPv4 values; failure remains an explicitly labelled LAN/unknown result. This does not open or verify the selected port. Source-only; the W050/W051 packages and local main remain unchanged.

Earlier W045/W047 startup context is preserved in [startup history](Archive/W078_STARTUP_HISTORY.md); current behavior is owned by [PvE implementation](Project/PVE_IMPLEMENTATION.md).

PvE now defaults to PvP's TCP 45677 for direct internet play with a session key and a reachable host port. Join automatically adopts the host seed (PVE-D33); only address, port and key need sharing, with matching build/data. Keyed local Host/Join/reconnect and rejection boundaries are verified; external internet reachability is not. These source changes are not in W035. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Melee's Idle is now Shield Up, Initiative 9: "This unit is immune this turn." It uses the existing Immune rule from Skill resolution until the This Turn expiry (PVE-D34), including restore/Undo support and shared card-face wording. Ranged's movement card is Take Position (Initiative 8); its former Idle is Aim (Initiative 1): "Next turn, have +1 Range." The shared purple source-card outline lasts while Aim is pending/active (PVE-D36). Heavy now has Hurl Attack 2 / Range 2 with one different-target repeat, Hook instead of Dozing Off, and Thick Hide's adjacent Attack 1 counter after the incoming attack and its after-attack text finish (PVE-D37). See [PvE implementation](Project/PVE_IMPLEMENTATION.md). Included in W045; W035 remains unchanged.

The enemy rail box now fits its live trays, hides when empty and scrolls only beyond the available height. This UI change is not in the preserved W035 package. Details: [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Enemy orange cards now shuffle each round instead of using preset orders (PVE-D32). All four slots remain public upfront; Defense stays separate and seed/round/enemy identity preserves Undo and co-op agreement. This source change is not included in the preserved W035 ZIP. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

All weaker enemy attacks have movement fallback after both attack options (PVE-D31/D35/D37): Slash Move 2 toward creeps, Shoot Move 1 toward heroes stopping at 2, Hurl Move 1 as close as possible toward the closest Blue unit (hero or creep), stopping adjacent. Ranged Take Position also stops at range 2; both Ranged attacks retain printed Range 3 (effective Range 4 during Aim). Stronger cards and other values are unchanged. These source changes are not in W035.

Melee/Ranged now count as both heroes and minions for targeting/effects; Heavy counts as hero only, and combined effects count each enemy once (amended PVE-D30). The shared engine preserves each enemy's Hero identity, combat and Defense, with guarded minion effect outputs, removal/replacement and restore/Undo support. Category work changed no card definitions. See [PvE implementation](Project/PVE_IMPLEMENTATION.md); no further implementation is authorized.

Melee, Ranged and Heavy enemies use official red tokens and a compact scrollable left rail: token, four prefilled public turn slots and Heavy's two Defense cards. Only enemies currently on the board have rows; removal hides them and spawning reveals them. No hero-sized plaque or text above map tokens; enemy token/rail hover links both ways in solo and co-op. Each enemy hand has exactly two printed attacks, one movement card and one fourth card (Melee Shield Up; Ranged Aim; Heavy Hook). Melee/Ranged have no defenses. Melee prioritizes Blue creeps; Ranged prioritizes heroes. See [PvE implementation](Project/PVE_IMPLEMENTATION.md) and [PvE design](Project/PVE_DESIGN.md). W035 PvE includes the compact rail and original Idle hands, not the new Skills; W024 remains preserved as an older package. W045 supersedes W013 for multiplayer. Existing-mode behavior, snapshot v59 and wire /52 stay unchanged. Prior delivery and the duplicate historical W-020 identifier are indexed in [work history](Archive/WORK_LOG.md). The revised compact player-name banner is visually approved (W-004). W-044's fourteen artwork-free private faces are saved in `tmp/w044-enemy-faces/final/`, with an HTML gallery and generation prompts. Twelve orange action faces and two purple Defense faces retain primary icons, omit secondary icons and follow the approved Hurl template. W049 integrates unchanged copies as private runtime assets; they are not native builder exports and the preserved W046 package does not include them.

Phase 9 is closed as a development container. Current direction: playtest and repair the private game and design PvE with the user. The first PvE slice is playable; later PvE questions stay open in its design owner. Paused 9.80 is not automatically next.

The playable PvE Blue team is exactly three chosen heroes (PVE-D27); all five placement spaces remain available. Generic composition supports smaller deterministic test fixtures.

PVE-D38 extends PVE-D28's playing host/two joiners to one-to-three players sharing three Blue seats. Host controls enemies/shared choices; only owned heroes may be controlled, while coordination grants allied-hand inspection. Keyed TCP, reserved-seat reconnect and terminal host loss reuse the private session stack. W035's historical separate launcher remains a fixture/tool path; the unified package no longer uses it.

## Durable boundaries

- Existing rules and reused tabletop behavior: [rules ledger](RULES_LEDGER.md). Original PvE decisions: [PvE design](Project/PVE_DESIGN.md).
- Preserve authoritative state, privacy, command/choice, snapshot, Undo and replay contracts.
- Scope stays private/noncommercial for the user and friends. Remote publication is limited to tracked source, the authorized rulebook and prepared runtime artwork authorized under W-008 and W-020; raw reference corpora, source ZIPs, screenshots, builds, logs and all other excluded or untracked material remain private.
- Build requests use changed/affected checks plus exact-artifact smokes; untouched green coverage is reused under [testing policy](Testing/POLICY.md).
- Unfinished legacy items and prerequisites remain in the [shared backlog](Roadmap/BACKLOG.md); parked coverage and risks are not passes.

## Targeted routing

| Need | Read |
| --- | --- |
| Current plan / shared queue | [Current work](Roadmap/CURRENT_WORK.md), [backlog](Roadmap/BACKLOG.md) |
| Execution / historical phases | [ROADMAP.md](ROADMAP.md) |
| Current behavior inventory | [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) |
| UI implementation / direction | [Project/MATCH_HUD.md](Project/MATCH_HUD.md), [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Online host, protocol, trust | [Project/PRIVATE_ONLINE_ARCHITECTURE.md](Project/PRIVATE_ONLINE_ARCHITECTURE.md) |
| Other implementation subjects | [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) |
| Rules, hero evidence, deviations | [RULES_LEDGER.md](RULES_LEDGER.md) |
| Official map contract | [Maps/README.md](Maps/README.md), [small-map preparation](Maps/ALTERNATE_SMALL_PREPARATION.md) |
| Card/action contracts | [CARD_SYSTEM_ARCHITECTURE.md](CARD_SYSTEM_ARCHITECTURE.md) |
| Effect/query/timing contracts | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) |
| Validation tier / recipe | [TESTING.md](TESTING.md) |
| Active risks | [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) |
| Ownership and archives | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) |


Superseded startup narrative is preserved in [the transition snapshot](Archive/SESSION_BRIEF_BEFORE_ITERATION_2026_10_08.txt); read it only for a specific historical question.
