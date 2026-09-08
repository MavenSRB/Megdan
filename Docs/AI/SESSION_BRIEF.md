# Guards Digital - Session Brief

Last updated: 2026-09-08

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; eleven playable heroes; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v40 persists the current state; wire protocol is `guards-private-online/11`. The latest pre-cleanup implementation is 8.35z63 (active-effect board markers); integration evidence is in commit `0847c44e`, build identity in `49dc63a1`. Those records include explicitly skipped coverage; the live internet gate is outstanding.

The user reports FILL WITH BOTS timing out in the built player. Final examples also include END TURN/UNDO button treatment and real artwork for newly playable heroes (8.35z80/8.35z81). Finish with the bot path and a new private build showing these changes. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.35z65**.
- Active authorized milestone: **None**.
- Next proposed milestone: **8.35z66**.

The user's 2026-09-08 "All yours now" authorizes the five agreed cleanup passes plus the bot fix last: **8.35z64-8.35z68**, then the added **8.35z80-8.35z81**, and finally **8.35z69**, sequentially with local commits. [The cleanup shard](Roadmap/PHASE_8_CLEANUP.md) owns outcomes, checks, and all carried-forward work. Further hero/content/feature work is recorded but not authorized. Previously authorized **8.36** is paused pending cleanup and human participation; its readiness evidence and **8.36a** review remain in [Phase 8](Roadmap/PHASE_8.md).

## Durable boundaries

- Rules/source hierarchy and deviations: [RULES_LEDGER.md](RULES_LEDGER.md). Cleanup does not change rules, content, or compatibility identities.
- Preserve pure rules/state, commands/choices, host authorization/privacy, snapshots, and replay. [Architecture](Project/ARCHITECTURE.md).
- Windows is the reference target; macOS/iOS are declared rather than validated. [Product](Project/PRODUCT.md).
- Source/rulebook permission does not cover extracted cards, maps, builds, private logs, or worktrees. No remote, upload, or distribution is authorized.

## Targeted routing

| Need | Read |
| --- | --- |
| Cleanup and carried-forward queue | [Roadmap/PHASE_8_CLEANUP.md](Roadmap/PHASE_8_CLEANUP.md) |
| Milestone execution / other phases | [ROADMAP.md](ROADMAP.md) |
| Current behavior inventory | [Project/CURRENT_IMPLEMENTATION.md](Project/CURRENT_IMPLEMENTATION.md) |
| UI implementation / direction | [Project/MATCH_HUD.md](Project/MATCH_HUD.md), [Project/PRESENTATION.md](Project/PRESENTATION.md) |
| Online host, protocol, trust | [Project/PRIVATE_ONLINE_ARCHITECTURE.md](Project/PRIVATE_ONLINE_ARCHITECTURE.md) |
| Other implementation subjects | [PROJECT_CONTEXT.md](PROJECT_CONTEXT.md) |
| Rules, hero evidence, deviations | [RULES_LEDGER.md](RULES_LEDGER.md) |
| Official map contract | [Maps/README.md](Maps/README.md) |
| Card/action contracts | [CARD_SYSTEM_ARCHITECTURE.md](CARD_SYSTEM_ARCHITECTURE.md) |
| Effect/query/timing contracts | [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) |
| Validation tier / recipe | [TESTING.md](TESTING.md) |
| Active risks | [TECHNICAL_RISKS.md](TECHNICAL_RISKS.md) |
| Ownership and archives | [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md) |

Previous startup narrative is preserved in [the pre-cleanup record](Archive/SESSION_BRIEF_BEFORE_CLEANUP.md); load history only for a specific question.
