# Guards Digital - Session Brief

Last updated: 2026-09-08

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; eleven playable heroes; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v40 persists the current state; wire protocol is `guards-private-online/11`. The approved cleanup and final private packaging are complete; evidence is indexed in [Phase 8 handoffs](Archive/Phase-Handoffs/PHASE_8.md). Explicitly parked coverage is not passing coverage; the live internet gate remains outstanding.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail; the packaged picker produces her matching hand and tray. The corrected ZIP with Play/Host launchers is local under Builds. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.35z83**.
- Active authorized milestone: **8.35z84**.
- Next proposed milestone: **8.35z84**.

The original cleanup through **8.35z82** is complete. The user's subsequent 2026-09-08 reports authorize the bounded follow-up **8.35z83-8.35z85**, sequentially: complete card highlights, transparent aura and adaptive ring order, then connect lobby bots to the existing smarter policy and produce the corrected private ZIP. [The cleanup shard](Roadmap/PHASE_8_CLEANUP.md) owns exact outcomes, checks, and the preserved backlog. This does not authorize new AI research or unrelated backlog work. Previously authorized **8.36** remains paused for human participation; its readiness evidence and **8.36a** review remain in [Phase 8](Roadmap/PHASE_8.md).

Card highlights and CLEAR/FAST TRAVEL placement have passed focused resource, Unity and eleven-hero visual checks; exact-player confirmation and packaging follow at 8.35z85. Additional heroes remain on hold. Aura order is token, purple effect ring, active-turn ring; without an effect, the turn ring sits beside the token.

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
