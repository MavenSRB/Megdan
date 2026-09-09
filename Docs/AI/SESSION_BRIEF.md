# Guards Digital - Session Brief

Last updated: 2026-09-09

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; eleven playable heroes; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v41 persists the current state; wire protocol is `guards-private-online/12`. The approved cleanup and final private packaging are complete; evidence is indexed in [Phase 8 handoffs](Archive/Phase-Handoffs/PHASE_8.md). The user accepted a clean completed internet 2v2 on the previous build as closing [8.36](Roadmap/PHASE_8.md); its amendment preserves unverified cases and the separate review. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail; the packaged picker produces her matching hand and tray. The corrected ZIP with Play/Host launchers is local under Builds. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.36g1**.
- Active authorized milestone: **8.36h**.
- Next proposed milestone: **8.36h**.

The cleanup and 8.35z83-8.35z85 fixes are complete, and the user-accepted 8.36 gate remains closed. The independent 8.36a review is integrated. On 2026-09-09 the user approved **8.36b-8.36d**, sequentially: authenticated departure, bounded transport, attributable diagnostics. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the outcomes and closing checks. [Product](Project/PRODUCT.md) now limits the project to the user and friends: production presentation and publisher-pitch work are parked. More heroes remain desired future work, not authorized implementation in this correction batch. No repeat internet playtest, remote publication or unrelated refactor is authorized.

Card highlights and CLEAR/FAST TRAVEL placement passed resource, Unity and eleven-hero visual checks. Transparent aura and adaptive ring order passed exact-player rendering checks, including the turn pulse's minimum size. Lobby bots now use the measured owner-scoped tactical policy: useful Fast Travel, battle entry, typed Skill choices and bounded public-card knowledge while hidden hands/decks remain absent. The prior Tier-2 static policy remains the comparison control. The verified package is still the pre-g1 `Builds/Playtest-8.35z85-fixes.zip`; replacement packaging follows only at 8.36h. Additional heroes remain on hold. Aura order is token, purple effect ring, active-turn ring; without an effect, the turn ring sits beside the token.

## Durable boundaries

The user additionally approved **8.36e**, after 8.36b-8.36d: visible hero selection, no unpicked hero names, hover overview cards from Docs, and a logo/backdrop instead of the pre-match map. Its bounded closing checks live in Phase 8; this does not authorize the parked presentation program or new heroes.

8.36e also includes the reported disappearing lobby buttons, with state-transition visibility, reachability and action checks, and random bot hero picks from the currently legal choices instead of the first available hero.

**8.36f** is also approved after 8.36e: verify and correct the reported active-Trailblazer resolver failure and clipped internal-error feedback. Treat the supplied diagnosis as a report until reproduced against code and printed-card authority; Phase 8 owns closing checks. The additional player screenshot confirms a clipped banner exposing an InvalidOperationException tied to the active Silverarrow Trailblazer effect; it does not by itself prove the resolver cause.

8.36f also includes the reported Watch How I Soar minion surviving its chosen Defeat branch, through the actual input/confirmation path. **8.36g** delivered measured smarter lobby tactics, including useful Fast Travel and reaching battle, with bounded work and no private-information advantage. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns all current outcomes.

**8.36f1** is approved between f and g: readable current-hand comparison during leveling, unclipped upgrade choices, and centered "GAINS:" plus actual reward icon(s). The supplied level-up screenshot is the reported failure reference; validate the full screen at user and supported short viewports.

**8.36f2** is approved after f1: attack zoom/strike without the attacker shrinking away, followed by the authoritative defender outcome. The user additionally approved **8.36f3**, an added-roster card-by-card audit and necessary fixes, after f2 and before g. Reconcile the exact configured roster and source cards, exercise actual player-input paths, and preserve explicit unresolved rule questions. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the checks and limits.

- Rules/source hierarchy and deviations: [RULES_LEDGER.md](RULES_LEDGER.md). Cleanup does not change rules, content, or compatibility identities.
- The standing new-build/Play-and-Host-ZIP request is mapped to **8.36h**, after the correction, audit and bot work; the private-game corrections shard owns its closing checks.
- On 2026-09-09 the user amended the remaining sequence: **8.36f3 → 8.36g → 8.36g1 → 8.36h**, without another approval stop between them. Bots actively pursue wins through productive minion/hero defeats and team objectives. **8.36g1 is complete:** one shared client hosts and plays through an owner-scoped loopback seat, the hosting player alone gets deliberate local Director debug access, and other players Join. No separate seatless host process is required in the new package. **8.36h packaging is now active** under the private-game corrections shard.
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
