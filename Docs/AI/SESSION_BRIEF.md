# Guards Digital - Session Brief

Last updated: 2026-09-10

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; thirteen playable heroes - the whole package arc plus Swift the Sharpshooter, every composed hero dealt; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v41 persists the current state; wire protocol is `guards-private-online/13`. Cleanup, corrections and packaging through 8.36h are complete, and 8.37/8.37b/8.37a deal Tali the Spiritcaller, give her runtime-integer decisions controls a player can answer, and compose her last deferred face. **No dealt hero now carries an engine deferral**: every printed text in the build runs, and the only parked faces are the twelve p.20 Handicap cards p.6 returns to the box at setup. Milestone 8.38's series then dealt and gated her; 8.39 inventories a FOURTEENTH hero, Cutter the Sky Pirate, at implementation grade - capability-mapped only, not composed, registered, dealt or gated. The user accepted a clean completed internet 2v2 on the previous build as closing [8.36](Roadmap/PHASE_8.md); its amendment preserves unverified cases and the separate review. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail. The current 8.36h candidate has one `PLAY - Host or Join.bat` launcher; older Play/Host packages remain preserved. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.39**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.39a**.

On 2026-09-11 the user re-opened the throttle the 2026-09-07 brakes direction closed, asked for proper documentation milestones and the next hero, and named **Swift** from the twenty undealt heroes the corpus holds. [Thirteenth-hero dealing](Roadmap/PHASE_8_THIRTEENTH_HERO.md) owns the whole series as a user-approved roadmap amendment: **8.38** has inventoried her printed set and **8.38a**, **8.38b** and **8.38c** have built the printed-target, placement and borrow-pool parts of what it named as owed; **8.38c1** and **8.38c2** have built the counted minion bounty and Bullet Time's card-resolution trigger - both split out of 8.38c during implementation, same total scope, **8.38d** has composed her eighteen dealt faces and made a permanent ability able to grant an action at all, **8.38e** has dealt, registered and artworked her and put her pick card under the cursor - she is pickable, her real fronts and token are prepared, and the composition defects only a dealing can see are closed - and **8.38f** has gated her whole printed set through real player input - fifteen journeys, no `[Explicit]` case, no skip - so **the series is COMPLETE and Swift is fully dealt and gated**. It also closed two staging defects 8.38e shipped that are invisible offline: her scenes had no arm in `SelectSetupConfiguration` and fell through to Arien's setup, dealing her cards to a seat whose identity was `arien`, and none in `EngagedMirrorStaging`, so scenes named "Engaged" stood the heroes fifteen apart. **8.38d and 8.38e were both flipped before Unity had run at all** - raised by the peer Cutter session on 2026-09-11 - so Edit Mode ran afterwards and found one red belonging to 8.38e, the artwork catalog's bijection count, which no offline tier can execute; both acceptance checks are amended in the shard rather than left reading as met, and **8.38e's Play Mode debt is moved to 8.38f**. 8.38f's gate table is already derived and offline-proved; its gate test and two draft journeys are parked at `tmp/8-38f-parked/` with the compile traps they already cost recorded in the block. Their user-named requirements are real hero art, a hover pick card on the selector, and every face proven through real player input. This re-opens the roster and nothing else: the brakes direction still governs the parked presentation program, the card-authoring workbench, tier-3 bots and every other parked item. The prior next-proposed entry, [8.35z74 Repair explicitly parked player-path coverage](Roadmap/PHASE_8_CLEANUP.md#x-835z74-repair-explicitly-parked-player-path-coverage), is closed on 2026-09-11 as discharged by 8.36f3 and 8.37a rather than executed: the inventory its own dependency clause demanded found no parked player-path coverage left, Play Mode runs 478 with no skips at all, and the 36 Edit Mode skips are the deliberate measurement instruments its block now lists. The earlier claim that it still owed Ursafar's is superseded - 8.36f3 un-parked his nine journeys with Silverarrow's and Whisper's.

On 2026-09-10 the user requested documentation only for future four-round lobby-bot planning. [8.36i-8.36k](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md#future-four-round-lobby-bot-planning-parked-not-authorized) park a bounded turn-planning prototype, revisable four-round card strategy, and actual one-human/five-bot modest-hardware validation. Do not start them; the user explicitly does not want this implemented now. This does not change the current/next route or authorize a new build.

The cleanup and 8.35z83-8.35z85 fixes are complete, and the user-accepted 8.36 gate remains closed. The independent 8.36a review is integrated. On 2026-09-09 the user approved **8.36b-8.36d**, sequentially: authenticated departure, bounded transport, attributable diagnostics. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the outcomes and closing checks. [Product](Project/PRODUCT.md) now limits the project to the user and friends: production presentation and publisher-pitch work are parked. More heroes remain desired future work, not authorized implementation in this correction batch. No repeat internet playtest, remote publication or unrelated refactor is authorized.

Card highlights and CLEAR/FAST TRAVEL placement passed resource, Unity and eleven-hero visual checks. Lobby bots use the measured owner-scoped tactical policy while hidden hands/decks remain absent. `Builds/Playtest-8.36h-host-and-play.zip` is the current completed local package; the user confirmed the outstanding ordinary-window package check worked on 2026-09-10. Additional heroes remain on hold.

On 2026-09-11, with the thirteenth-hero series still in flight, the user named the NEXT hero to deal - **Cutter the Sky Pirate** - and asked for the same terms Swift was given: real hero art, a hover pick card on the selector, and every face proven through real player input. [Fourteenth-hero dealing](Roadmap/PHASE_8_FOURTEENTH_HERO.md) owns that series as a user-approved roadmap amendment, and [her shard](Rules/HERO_CUTTER.md) carries the inventory it is sized against. Eleven blocks, **8.39** through **8.39j**. Its sequencing clause - wait for Swift - is discharged by 8.38f, and **8.39** has inventoried her. One decision is already settled and is why the series is eleven blocks rather than nine: her Tier III Blue card **A Fistful of Coins** prints "If you have 13 or more coins, you alone win the game", which `MatchVictoryCause` has no shape for - it carries three members and every one awards the match to a TEAM - and on 2026-09-11 the user chose to implement it FAITHFULLY rather than read it as a team win or park the clause, so **8.39g** is expected to move the schema and is sequenced before composition rather than after. This re-opens the roster and nothing else.

## Durable boundaries

The user additionally approved **8.36e**, after 8.36b-8.36d: visible hero selection, no unpicked hero names, hover overview cards from Docs, and a logo/backdrop instead of the pre-match map. Its bounded closing checks live in Phase 8; this does not authorize the parked presentation program or new heroes.

8.36e also includes the reported disappearing lobby buttons, with state-transition visibility, reachability and action checks, and random bot hero picks from the currently legal choices instead of the first available hero.

**8.36f** is also approved after 8.36e: verify and correct the reported active-Trailblazer resolver failure and clipped internal-error feedback. Treat the supplied diagnosis as a report until reproduced against code and printed-card authority; Phase 8 owns closing checks. The additional player screenshot confirms a clipped banner exposing an InvalidOperationException tied to the active Silverarrow Trailblazer effect; it does not by itself prove the resolver cause.

8.36f also includes the reported Watch How I Soar minion surviving its chosen Defeat branch, through the actual input/confirmation path. **8.36g** delivered measured smarter lobby tactics, including useful Fast Travel and reaching battle, with bounded work and no private-information advantage. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns all current outcomes.

**8.36f1** is approved between f and g: readable current-hand comparison during leveling, unclipped upgrade choices, and centered "GAINS:" plus actual reward icon(s). The supplied level-up screenshot is the reported failure reference; validate the full screen at user and supported short viewports.

**8.36f2** is approved after f1: attack zoom/strike without the attacker shrinking away, followed by the authoritative defender outcome. The user additionally approved **8.36f3**, an added-roster card-by-card audit and necessary fixes, after f2 and before g. Reconcile the exact configured roster and source cards, exercise actual player-input paths, and preserve explicit unresolved rule questions. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the checks and limits.

- Rules/source hierarchy and deviations: [RULES_LEDGER.md](RULES_LEDGER.md). Cleanup does not change rules, content, or compatibility identities.
- The standing new-build/Play-and-Host-ZIP request is mapped to **8.36h**, after the correction, audit and bot work; the private-game corrections shard owns its closing checks.
- On 2026-09-09 the user amended the remaining sequence: **8.36f3 → 8.36g → 8.36g1 → 8.36h**, without another approval stop between them. **The sequence is complete:** one shared client hosts and plays through an owner-scoped loopback seat, the hosting player alone gets deliberate local Director debug access, and other players Join. The private-game corrections shard links the completion evidence.
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
