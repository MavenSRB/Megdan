# Guards Digital - Session Brief

Last updated: 2026-10-12

## Purpose and authority

Read once at startup. This brief alone owns current authorization and next routing. [Workflow](ROADMAP.md) owns execution; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; 32 configured heroes, private host/join play, 2v2/3v3 and host-selected Waves/Lives. Snapshot schema v59 persists the fixed board partition; wire protocol /52 carries immutable Experimental modifier-display controls, their visible live card projections, and resolved animation attack totals. See [current implementation](Project/CURRENT_IMPLEMENTATION.md) for precise behavior and limits.

Current private Windows package: **W050 Unified**, `Builds/Playtest-W050-unified.zip`. `GuardsDigital.exe` opens the shared Host/Join menu; Map selects PvP or PvE. Includes W047-W049, full tiebreaker clearance above enemy trays, and wave one with 3 Melee, 3 Ranged and 1 Heavy (PVE-D41); wave two unchanged. Package identity and evidence: [build record](Project/BUILDS.md). Older packages remain preserved.

## Current authorization and next route

Matching Mac package: `Builds/Playtest-W050-unified-mac.zip` (W051), universal Intel/Apple Silicon, both PvP and PvE, same W050 build stamp/content. Build/bundle/archive checks passed on Windows; native Mac launch, rendering and cross-platform networking remain unverified. See [build record](Project/BUILDS.md).

- Last completed work: **W-054**.
- Active authorized work: **None**.
- Next proposed work: **None**.

W054 repairs the unseated hosting process's live spectator view: the host redraw signature now follows the local authority version, so remote commands refresh hero positions and every other projection-driven board/HUD surface. The affected private-lobby Play Mode fixture and targeted visual review passed. Source-only; W050/W051 packages remain unchanged.

W048 saved PvE logs and W049 generated enemy faces/board-gated turns are included in W050. Unspawned enemies have no commitments or turn assignments and enter the turn roster on spawning. Generated faces retain portrait proportions through the existing artwork importer; original card dimensions and preview scaling are unchanged. W046 package/main remain preserved; await the next user request.

W052 separates Turn timer from Card Modifiers in Experimental and makes keyed-host address discovery fall back from local UPnP to bounded HTTPS public-IPv4 lookups. Responses are accepted only as exact public IPv4 values; failure remains an explicitly labelled LAN/unknown result. This does not open or verify the selected port. Source-only; the W050/W051 packages and local main remain unchanged.

W047 implements PVE-D39 on `codex/pve-ui-cleanup`: board-click initial placement, seeded automatic enemy choices, removal of manual level grants and redundant PvE/control panels, owned tray/token selection with automatic awaited-seat switching, header-free enemy trays with larger numbers, and the shared Blue-only end screen. Focused rules/network/real-input UI checks and visual review passed. W050 includes these changes; local `main` remains at `815a9f82`. W047 itself was source-only; no main merge or remote push followed.

W045 unifies PvP/PvE on the existing Take Seats and hero-pick screens (PVE-D38). PvE has exactly three Blue seats; a player may claim one, two or all three. Each owned seat retains its own authenticated client/projection; W047 replaces Control buttons with tray/token clicks and automatic awaited-seat selection. Join inherits the host's mode and seed. PvE hides Experimental and always enables teammate coordination; PvP settings remain unchanged. Solo/two/three-process PvE and host/two-joiner PvP passed against the W046 artifact; focused Editor reconnect passed, but no new internet or packaged reconnect claim. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

PvE now defaults to PvP's TCP 45677 for direct internet play with a session key and a reachable host port. Join automatically adopts the host seed (PVE-D33); only address, port and key need sharing, with matching build/data. Keyed local Host/Join/reconnect and rejection boundaries are verified; external internet reachability is not. These source changes are not in W035. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Melee's Idle is now Shield Up, Initiative 9: "This unit is immune this turn." It uses the existing Immune rule from Skill resolution until the This Turn expiry (PVE-D34), including restore/Undo support and shared card-face wording. Ranged's movement card is Take Position (Initiative 8); its former Idle is Aim (Initiative 1): "Next turn, have +1 Range." The shared purple source-card outline lasts while Aim is pending/active (PVE-D36). Heavy now has Hurl Attack 2 / Range 2 with one different-target repeat, Hook instead of Dozing Off, and Thick Hide's adjacent Attack 1 counter after the incoming attack and its after-attack text finish (PVE-D37). See [PvE implementation](Project/PVE_IMPLEMENTATION.md). Included in W045; W035 remains unchanged.

The enemy rail box now fits its live trays, hides when empty and scrolls only beyond the available height. This UI change is not in the preserved W035 package. Details: [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Enemy orange cards now shuffle each round instead of using preset orders (PVE-D32). All four slots remain public upfront; Defense stays separate and seed/round/enemy identity preserves Undo and co-op agreement. This source change is not included in the preserved W035 ZIP. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

All weaker enemy attacks have movement fallback after both attack options (PVE-D31/D35/D37): Slash Move 2 toward creeps, Shoot Move 1 toward heroes stopping at 2, Hurl Move 1 as close as possible toward the closest Blue unit (hero or creep), stopping adjacent. Ranged Take Position also stops at range 2; both Ranged attacks retain printed Range 3 (effective Range 4 during Aim). Stronger cards and other values are unchanged. These source changes are not in W035.

Melee/Ranged now count as both heroes and minions for targeting/effects; Heavy counts as hero only, and combined effects count each enemy once (amended PVE-D30). The shared engine preserves each enemy's Hero identity, combat and Defense, with guarded minion effect outputs, removal/replacement and restore/Undo support. Category work changed no card definitions. See [PvE implementation](Project/PVE_IMPLEMENTATION.md); no further implementation is authorized.

Melee, Ranged and Heavy enemies use official red tokens and a compact scrollable left rail: token, four prefilled public turn slots and Heavy's two Defense cards. Only enemies currently on the board have rows; removal hides them and spawning reveals them. No hero-sized plaque or text above map tokens; enemy token/rail hover links both ways in solo and co-op. Each enemy hand has exactly two printed attacks, one movement card and one fourth card (Melee Shield Up; Ranged Aim; Heavy Hook). Melee/Ranged have no defenses. Melee prioritizes Blue creeps; Ranged prioritizes heroes. See [PvE implementation](Project/PVE_IMPLEMENTATION.md) and [PvE design](Project/PVE_DESIGN.md). W035 PvE includes the compact rail and original Idle hands, not the new Skills; W024 remains preserved as an older package. W045 supersedes W013 for multiplayer. Existing-mode behavior, snapshot v59 and wire /52 stay unchanged. Prior delivery and the duplicate historical W-020 identifier are indexed in [work history](Archive/WORK_LOG.md). The revised compact player-name banner is visually approved (W-004). W-044's fourteen artwork-free private faces are saved in `tmp/w044-enemy-faces/final/`, with an HTML gallery and generation prompts. Twelve orange action faces and two purple Defense faces retain primary icons, omit secondary icons and follow the approved Hurl template. W049 integrates unchanged copies as private runtime assets; they are not native builder exports and the preserved W046 package does not include them.

Phase 9 is closed as a development container. Current direction: playtest, iterate and repair the existing private game, add large maps as a planned expansion, and design PvE with the user. Large-map work retains the existing 9.62 plan and its setup/rules prerequisites in the backlog; it is not cancelled or automatically started. The first PvE slice is playable; later PvE questions stay open in its design owner. Neither the former 9.62 route nor paused 9.80 is automatically next.

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
