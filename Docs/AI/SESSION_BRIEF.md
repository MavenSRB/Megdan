# Guards Digital - Session Brief

Last updated: 2026-10-12

## Purpose and authority

Read once at startup. This brief alone owns current authorization and next routing. [Workflow](ROADMAP.md) owns execution; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; 32 configured heroes, private host/join play, 2v2/3v3 and host-selected Waves/Lives. Snapshot schema v59 persists the fixed board partition; wire protocol /52 carries immutable Experimental modifier-display controls, their visible live card projections, and resolved animation attack totals. See [current implementation](Project/CURRENT_IMPLEMENTATION.md) for precise behavior and limits.

Current private existing-mode package: **W013**, `Builds/Playtest-W013-live-card-values.zip`. Current private PvE test package: **W035 PvE**, `Builds/Playtest-W035-pve-coop.zip` (separate executable). Package identity and evidence: [build record](Project/BUILDS.md). Older packages remain preserved.

## Current authorization and next route

- Last completed work: **W-042**.
- Active authorized work: **none**.
- Next proposed work: **none**.

PvE now defaults to PvP's TCP 45677 for direct internet play with a session key and a reachable host port. Join automatically adopts the host seed (PVE-D33); only address, port and key need sharing, with matching build/data. Keyed local Host/Join/reconnect and rejection boundaries are verified; external internet reachability is not. These source changes are not in W035. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Melee's Idle is now Shield Up, Initiative 9: "This unit is immune this turn." It uses the existing Immune rule from Skill resolution until the This Turn expiry (PVE-D34), including restore/Undo support and shared card-face wording. Ranged's movement card is Take Position (Initiative 8); its former Idle is Aim (Initiative 1): "Next turn, have +1 Range." The shared purple source-card outline lasts while Aim is pending/active (PVE-D36). Heavy keeps Idle. Source only; W035 has not been rebuilt.

The enemy rail box now fits its live trays, hides when empty and scrolls only beyond the available height. This UI change is not in the preserved W035 package. Details: [PvE implementation](Project/PVE_IMPLEMENTATION.md).

Enemy orange cards now shuffle each round instead of using preset orders (PVE-D32). All four slots remain public upfront; Defense stays separate and seed/round/enemy identity preserves Undo and co-op agreement. This source change is not included in the preserved W035 ZIP. See [PvE implementation](Project/PVE_IMPLEMENTATION.md).

All weaker enemy attacks have movement fallback after both attack options (PVE-D31/D35): Slash Move 2 toward creeps, Shoot Move 1 toward heroes stopping at 2, Hurl Move 1 toward heroes stopping at 2. Ranged Take Position also stops at range 2; both Ranged attacks retain printed Range 3 (effective Range 4 during Aim). Stronger cards and other values are unchanged. These source changes are not in W035.

Melee/Ranged now count as both heroes and minions for targeting/effects; Heavy counts as hero only, and combined effects count each enemy once (amended PVE-D30). The shared engine preserves each enemy's Hero identity, combat and Defense, with guarded minion effect outputs, removal/replacement and restore/Undo support. Category work changed no card definitions. See [PvE implementation](Project/PVE_IMPLEMENTATION.md); no further implementation is authorized.

Melee, Ranged and Heavy enemies use official red tokens and a compact scrollable left rail: token, four prefilled public turn slots and Heavy's two Defense cards. Only enemies currently on the board have rows; removal hides them and spawning reveals them. No hero-sized plaque or text above map tokens; enemy token/rail hover links both ways in solo and co-op. Each enemy hand has exactly two attacks, one movement card and one fourth card (Melee Shield Up; Ranged Aim; Heavy Idle). Melee/Ranged have no defenses. Melee prioritizes Blue creeps; Ranged prioritizes heroes. See [PvE implementation](Project/PVE_IMPLEMENTATION.md) and PVE-D24/D25/D26/D29/D34/D36 in [PvE design](Project/PVE_DESIGN.md). W035 PvE includes the compact rail and original Idle hands, not the new Skills; W024 remains preserved as an older package. W013 remains the current multiplayer package. Existing-mode behavior, snapshot v59 and wire /52 stay unchanged. Prior delivery and the duplicate historical W-020 identifier are indexed in [work history](Archive/WORK_LOG.md). The revised compact player-name banner is visually approved (W-004); no approval gate remains in [current work](Roadmap/CURRENT_WORK.md).

Phase 9 is closed as a development container. Current direction: playtest, iterate and repair the existing private game, add large maps as a planned expansion, and design PvE with the user. Large-map work retains the existing 9.62 plan and its setup/rules prerequisites in the backlog; it is not cancelled or automatically started. The first PvE slice is playable; later PvE questions stay open in its design owner. Neither the former 9.62 route nor paused 9.80 is automatically next.

The playable PvE Blue team is exactly three chosen heroes (PVE-D27); all five placement spaces remain available. Generic composition supports smaller deterministic test fixtures.

PvE Host/Join supports a playing host and two joiners, one Blue hero each (PVE-D28). Host controls enemies/shared choices; hero decisions and private cards remain owner-scoped. Keyed TCP, reserved-seat reconnect and terminal host loss reuse the private session stack. Solo remains available. W035 adds exact-package solo and three-process loopback validation plus board captures and extracted-ZIP launch; no new internet/NAT or packaged reconnect claim.

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
