# Guards Digital - Session Brief

Last updated: 2026-09-13

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; fourteen playable heroes - the whole package arc plus Swift the Sharpshooter and Cutter the Sky Pirate, every composed hero dealt; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v42 persists the current state; wire protocol is `guards-private-online/14`. Cleanup, corrections and packaging through 8.36h are complete, and 8.37/8.37b/8.37a deal Tali the Spiritcaller, give her runtime-integer decisions controls a player can answer, and compose her last deferred face. **No dealt hero now carries an engine deferral**: every printed text in the build runs, and the only parked faces are the fourteen p.20 Handicap cards p.6 returns to the box at setup. Milestone 8.38's series then dealt and gated her, and the 8.39 series has dealt and gated a FOURTEENTH, Cutter the Sky Pirate; **every hero in the build now passes the permanent player-path gate**. The user accepted a clean completed internet 2v2 on the previous build as closing [8.36](Roadmap/PHASE_8.md); its amendment preserves unverified cases and the separate review. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail. The current 8.36h candidate has one `PLAY - Host or Join.bat` launcher; older Play/Host packages remain preserved. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.40a**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.40b**.

On 2026-09-13 the user named the FIFTEENTH hero and asked for him on the terms the two series before him were given: "make other agents and have them implement Bain ... Keep pushing them until hero is fully implemented and tested", and "fully implemented, pickable, real cards, pick card on pick screen, all the cards work, the whole thing." [Fifteenth-hero dealing](Roadmap/PHASE_8_FIFTEENTH_HERO.md) owns that series as a user-approved roadmap amendment, and [his shard](Rules/HERO_BAIN.md) carries the inventory it is sized against. **Milestone 8.40 inventoried him and did nothing else** - he is not composed, registered, dealt or gated, so the build still deals fourteen heroes - and twelve blocks follow, **8.40a** through **8.40k**. One is much larger than the rest: three of his Blue faces have an enemy hero secretly choose a card from their own hand while Bain guesses its colour and then reveals it, which is the first hidden choice and the first public reveal this build has ever needed, and his shard records the reading that its legal guesses are derived from PUBLIC information only. This re-opens the ROSTER for Bain alone; the 2026-09-07 brakes direction still governs the parked presentation program, the card-authoring workbench, tier-3 bots and every other parked item.

On 2026-09-11 the user re-opened the throttle that brakes direction had closed and named **Swift** as the next hero; [thirteenth-hero dealing](Roadmap/PHASE_8_THIRTEENTH_HERO.md) owns that COMPLETE series and all of its detail, and the prior next-proposed entry, [8.35z74](Roadmap/PHASE_8_CLEANUP.md#x-835z74-repair-explicitly-parked-player-path-coverage), closed the same day as discharged by 8.36f3 and 8.37a rather than executed.

On 2026-09-10 the user requested documentation only for future four-round lobby-bot planning. [8.36i-8.36k](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md#future-four-round-lobby-bot-planning-parked-not-authorized) park a bounded turn-planning prototype, revisable four-round card strategy, and actual one-human/five-bot modest-hardware validation. Do not start them; the user explicitly does not want this implemented now. This does not change the current/next route or authorize a new build.

The cleanup and 8.35z83-8.35z85 fixes are complete, and the user-accepted 8.36 gate remains closed. The independent 8.36a review is integrated. On 2026-09-09 the user approved **8.36b-8.36d**, sequentially: authenticated departure, bounded transport, attributable diagnostics. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the outcomes and closing checks. [Product](Project/PRODUCT.md) now limits the project to the user and friends: production presentation and publisher-pitch work are parked. More heroes remain desired future work, not authorized implementation in this correction batch. No repeat internet playtest, remote publication or unrelated refactor is authorized.

Card highlights and CLEAR/FAST TRAVEL placement passed resource, Unity and eleven-hero visual checks. Lobby bots use the measured owner-scoped tactical policy while hidden hands/decks remain absent. `Builds/Playtest-8.36h-host-and-play.zip` is the current completed local package; the user confirmed the outstanding ordinary-window package check worked on 2026-09-10. Additional heroes remain on hold.

On 2026-09-11 the user also named **Cutter the Sky Pirate** next, on the same terms; [fourteenth-hero dealing](Roadmap/PHASE_8_FOURTEENTH_HERO.md) owns that COMPLETE thirteen-block series and [her shard](Rules/HERO_CUTTER.md) its inventory. Nearly every block predicted the wrong contract and was corrected by the probe it ordered, and her seventeen-journey gate found four shipped defects nothing offline could see. One decision is why it ran that long: her printed "you alone win the game" was implemented FAITHFULLY on the user's 2026-09-11 answer, moving the snapshot schema to **v42** and the wire to **/14**. **The user has asked for a NEW PLAYTEST PACKAGE, which still owes a written milestone before it is one.**

## Durable boundaries

The user additionally approved **8.36e**, after 8.36b-8.36d: visible hero selection, no unpicked hero names, hover overview cards from Docs, and a logo/backdrop instead of the pre-match map. Its bounded closing checks live in Phase 8; this does not authorize the parked presentation program or new heroes.

8.36e also includes the reported disappearing lobby buttons, with state-transition visibility, reachability and action checks, and random bot hero picks from the currently legal choices instead of the first available hero.

**8.36f** is also approved after 8.36e: verify and correct the reported active-Trailblazer resolver failure and clipped internal-error feedback. Treat the supplied diagnosis as a report until reproduced against code and printed-card authority; Phase 8 owns closing checks. The additional player screenshot confirms a clipped banner exposing an InvalidOperationException tied to the active Silverarrow Trailblazer effect; it does not by itself prove the resolver cause.

8.36f also includes the reported Watch How I Soar minion surviving its chosen Defeat branch, through the actual input/confirmation path. **8.36g** delivered measured smarter lobby tactics, including useful Fast Travel and reaching battle, with bounded work and no private-information advantage. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns all current outcomes.

**8.36f1** is approved between f and g: readable current-hand comparison during leveling, unclipped upgrade choices, and centered "GAINS:" plus actual reward icon(s). The supplied level-up screenshot is the reported failure reference; validate the full screen at user and supported short viewports.

**8.36f2** is approved after f1: attack zoom/strike without the attacker shrinking away, followed by the authoritative defender outcome. The user additionally approved **8.36f3**, an added-roster card-by-card audit and necessary fixes, after f2 and before g. Reconcile the exact configured roster and source cards, exercise actual player-input paths, and preserve explicit unresolved rule questions. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the checks and limits.

- Rules/source hierarchy and deviations: [RULES_LEDGER.md](RULES_LEDGER.md). Cleanup does not change rules, content, or compatibility identities.
- The standing new-build/Play-and-Host-ZIP request is mapped to **8.36h**, after the correction, audit and bot work; the private-game corrections shard owns its closing checks.
- On 2026-09-09 the user amended the remaining sequence: **8.36f3 â†’ 8.36g â†’ 8.36g1 â†’ 8.36h**, without another approval stop between them. **The sequence is complete:** one shared client hosts and plays through an owner-scoped loopback seat, the hosting player alone gets deliberate local Director debug access, and other players Join. The private-game corrections shard links the completion evidence.
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
