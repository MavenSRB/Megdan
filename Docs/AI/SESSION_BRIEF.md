# Guards Digital - Session Brief

Last updated: 2026-10-10

## Purpose and authority

Read once at startup. This brief alone owns current authorization and next routing. [Workflow](ROADMAP.md) owns execution; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; 32 configured heroes, private host/join play, 2v2/3v3 and host-selected Waves/Lives. Snapshot schema v59 persists the fixed board partition; wire protocol /52 carries immutable Experimental modifier-display controls, their visible live card projections, and resolved animation attack totals. See [current implementation](Project/CURRENT_IMPLEMENTATION.md) for precise behavior and limits.

Current private package: **W013**, `Builds/Playtest-W013-live-card-values.zip`. Package identity and evidence: [build record](Project/BUILDS.md). Older packages remain preserved.

## Current authorization and next route

- Last completed work: **W-026**.
- Active authorized work: **W-024**, the PvE package step of the approved sequence W-021 to W-024, rebuilt after the W-026 playtest fixes.
- Next proposed work: **W-024**.

W-026 fixed the user's 2026-10-11 playtest report: the HUD plays Blue only and enemy ties read as enemy decisions. W-023 made the PvE encounter playable in the separate executable (paced enemies, plan strips, placement, granted levels, tie prompts, loss banner, enemy card editor). Its rules run over restore-stable mode rules and additive, default-off engine hooks (placement, granted levels, no coins, respawn spaces, discard-or-defeat), with an enemy controller and an encounter session; the existing mode stays pinned by its snapshot golden. The separate PvE executable composes the encounter into the existing match scene; see [PvE implementation](Project/PVE_IMPLEMENTATION.md). First-slice rules are PVE-D01 to PVE-D23 in [PvE design](Project/PVE_DESIGN.md). W-024 (package) remains in the approved sequence; its first candidate package was superseded by the user's 2026-10-11 playtest report, and it is rebuilt after W-026. Existing-mode behavior, snapshot v59, wire /52 and the W013 package stay unchanged; W013 does not include W-014 through W-026. W-004 remains a paused presentation-approval gate. See [current work](Roadmap/CURRENT_WORK.md).

Phase 9 is closed as a development container. Current direction: playtest, iterate and repair the existing private game, add large maps as a planned expansion, and design PvE with the user. Large-map work retains the existing 9.62 plan and its setup/rules prerequisites in the backlog; it is not cancelled or automatically started. The first PvE slice is approved and in progress; later PvE questions stay open in its design owner. Neither the former 9.62 route nor paused 9.80 is automatically next.

## Durable boundaries

- Existing rules and reused tabletop behavior: [rules ledger](RULES_LEDGER.md). Original PvE decisions: [PvE design](Project/PVE_DESIGN.md).
- Preserve authoritative state, privacy, command/choice, snapshot, Undo and replay contracts.
- Scope stays private/noncommercial for the user and friends. Remote publication is limited to tracked source, the authorized rulebook and W-008 prepared card artwork; all other excluded or untracked material remains private.
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
