# Guards Digital - Session Brief

Last updated: 2026-09-20

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; all nineteen heroes dealt; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v43 persists the current state; wire protocol is `guards-private-online/19`. Cleanup, corrections and packaging through 8.36h are complete, and 8.37/8.37b/8.37a deal Tali the Spiritcaller, give her runtime-integer decisions controls a player can answer, and compose her last deferred face. **No dealt hero now carries an engine deferral**: every printed text in the build runs, and the only parked faces are the seventeen p.20 Handicap cards p.6 returns to the box at setup. All nineteen are registered and pass the permanent player-path gate; the [implementation inventory](Project/CURRENT_IMPLEMENTATION.md) names each dealing and its milestone. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail. The current 8.36h candidate has one `PLAY - Host or Join.bat` launcher; older Play/Host packages remain preserved. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.69f**.
- Active authorized milestone: **none**.
- Next proposed milestone: **8.69g**.

On 2026-09-20 the user explicitly authorized and completed
**[8.35z86](Roadmap/PHASE_8_CLEANUP.md)**: two unlabeled, client-local hero-pick filter rows provide
one optional star value and AND-combined roles, with same-family CLEAR. All nineteen profiles carry
verified metadata; selection authority, bot assignment, schema v43, replay and wire `/19` are
unchanged.

On 2026-09-18 the user authorized the **[decision window](Roadmap/PHASE_8_DECISION_WINDOW.md)** ("Ok, get on it then"); **8.46 is complete** (8.46a-8.46g): every card-text decision, tied initiative, defense, removal prevention, card chooser and the level-up upgrade open in one two-state window whose controls name their consequence, a direction is six arrows on the board, and a choice of exactly one resolves in one press.

On 2026-09-18 the user selected **Widget** after withdrawing Razzle. The authorized bounded sequence **8.43 -> 8.43a -> 8.43b -> 8.43c -> 8.43d -> 8.43e -> 8.43f** delivers the seventeenth fully playable, selectable, artworked hero and his permanent player-path gate. [Seventeenth-hero dealing](Roadmap/PHASE_8_SEVENTEENTH_HERO.md) owns acceptance; [Widget's shard](Rules/HERO_WIDGET.md) owns source facts and unresolved rules. No other hero, package or remote publication is authorized. The cleanup route below is superseded until this sequence ends.

**Widget's sequence is complete**: his complete printed set, private artwork, Pyro and permanent player-path gate are integrated. WI-Q1-WI-Q3 are resolved. FINISH MOVEMENT preserves optional follow-ups; Pyro's artwork has no name overlay.

**[8.42c](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md) is complete**: status narration now names the hero a standing public decision waits on, read from the authoritative `DecisionOwner` rather than guessed from the acting hero, while a responder keeps their own actionable prompt and no foreign hand or candidate is exposed. Ignatia was explicitly withdrawn; no hero follows Widget. Widget's local build-smoke exception is approved in his rules shard.

On 2026-09-18 the user authorized two packages, both complete: **[8.44](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.44-seventeen-hero`, seventeen heroes) and **[8.45](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.45-undo-corrections`, the same heroes plus 8.42d's visible seat rewind and 8.42e's interaction wall, content digest unchanged). Neither authorizes upload or public distribution.

The evening's invited playtest drove a run of small milestones, all complete: **[8.47](Roadmap/PHASE_8.md)** cut `Builds/Playtest-8.45a-phase-banner` (8.45 plus the transient phase banner); **[8.48](Roadmap/PHASE_8.md)** made Bounce's "may repeat once" hold when Reload! borrows it (`swift-8-48-rev-1`); **[8.50](Roadmap/PHASE_8.md)** made the banner speak only when a player is up; **[8.51](Roadmap/PHASE_8.md)** answered "Silverarrows blue got resolved without player input" with a `CardHeld` log row and an `END_TURN_PRESS` line naming what pressed END TURN.

The playtest's "Lives are not being counted correctly" report became **[8.49](Roadmap/PHASE_8.md) is complete**: p.13's lowest-Tier reading now covers the cards a hero is playing with, so a Defeat costs and pays 1, 2 or 3 by Tier, and each rail shows the hero's worth in a half-coin, half-heart badge beside its level.

From the same night's "Silverarrow" report, once 8.51 had named the press: **[8.52](Roadmap/PHASE_8.md) is complete** - the drawn END TURN control no longer answers Enter or Space, only the pointer.

**[8.53](Roadmap/PHASE_8.md) is complete** (`87dacf6c`): when a match ends the whole HUD gives way to the victory screen - winner, cause, the winners' block above the defeated with every hero's token, level, worth and the columns Kills, Deaths, Attacks at heroes, Times defended, Minions Killed, Coins earned, a solo winner split above her crew; the numbers are the authority's `MatchScoreboard` (snapshot **v43**, wire `guards-private-online/17`). **[8.56](Roadmap/PHASE_8.md) is complete**: `Builds/Playtest-8.53-victory-screen` (+ zip) carries 8.46-8.53; 8.54 and 8.55 remain queued.

The EIGHTEENTH hero, **Rowenna the Vanguard**, is complete and gated: [8.57-8.57h](Roadmap/PHASE_8_EIGHTEENTH_HERO.md) and [her shard](Rules/HERO_ROWENNA.md) own the detail (RO-03 settled by the user as the ribbon colour). The same day's **8.58** (the decision window per the mockup), **8.59** (the line picker's floor), **8.54** (Bounce through Bullet Time), **8.55** (the "6+1" badge) and **[8.60](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.55-rowenna`, cut at the 8.55 tag, digest `36da22a7...`) are complete.

- 2026-09-19 (later): the user authorized two HUD corrections from playtest screenshots as roadmap amendments - **[8.62](Roadmap/PHASE_8.md)** (targeting cues read as blood-red light) and **[8.63](Roadmap/PHASE_8.md)** (the decision window above every other panel) and **[8.64](Roadmap/PHASE_8.md)** (the victory screen drops its column legend); all three COMPLETE 2026-09-20 on the user's approval of their photographs (`c9c615ec`, `4feec1c4`, `de886e51`; the 8.62 cue settled as solid corner marks by side with a full hex on hover, not a ring).

The NINETEENTH hero, **Min the Dragonmonk**, is complete: **[8.61-8.61k](Roadmap/PHASE_8_NINETEENTH_HERO.md)** ([her shard](Rules/HERO_MIN.md); wire `/18` at 8.61e; four MI questions settled by the user), the Silverarrow follow-ups **[8.66](Roadmap/PHASE_8.md)** and **[8.67](Roadmap/PHASE_8.md)**, and the packages **[8.65](Roadmap/PHASE_8.md)** and **[8.68](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.66-silverarrow`, the current playtest package).

On 2026-09-21 the user opened the TWENTIETH hero ("ok go for Byrnn, keep in mind he needs to be correctly implemented in new hero pick screen") and chose **Brynn the Seeker**: **[8.69](Roadmap/PHASE_8_TWENTIETH_HERO.md)** (inventory; [his shard](Rules/HERO_BRYNN.md)) **8.69a-8.69e** (the obstacle-adjacency query and Over the Top; the split attacks; Decoy and the guides; the tracking swaps; the traps and grit - every vocabulary block, the wave landed as `2b246051`, `ebd62b5a`, `9c772092`, `5625664d`) and **8.69f** (`13223e5f`, the set composed under one revision) **are complete**; **8.69g** deals him and **8.69h** gates him through a fully selectable, artworked hero whose picker row carries three stars and Tactician/Damager/Durable for the 8.35z86 filters. His set is built on one predicate, "adjacent to 3 or more obstacles", which his Ultimate makes true of every enemy hero while he acts; BR-Q13 was settled by the user 2026-09-21 (the water ring counts); BR-Q4 waits on the user. No other hero or package is authorized.

The 2026-09-17 refinements **8.42 -> 8.42a -> 8.42b -> 8.42d** are complete ([playtest refinements](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md) owns them): Shotgun's discard excludes the attacked unit, the badge shows card-plus-item Initiative, trays stay on their own hero, and the seat UNDO square survives a standing prompt. The user's 2026-09-18 approval of 8.42a's disposable build-and-launch check included no package. Return to proposed cleanup 8.35z71.

On 2026-09-17 the user selected **Garrus the Gladiator** as the SIXTEENTH hero. [Sixteenth-hero dealing](Roadmap/PHASE_8_SIXTEENTH_HERO.md) owns that COMPLETE sequence (**8.41**-**8.41f**) and [his rules shard](Rules/HERO_GARRUS.md) the nineteen-face inventory; both preserve the user's 2026-09-17 GA-Q1 answer ("I think yes, he can move 0") as a recorded user-approved interpretation, not a claimed official ruling. That authorization is exhausted.

On 2026-09-13 the user named the FIFTEENTH hero, Bain the Bounty Hunter; [fifteenth-hero dealing](Roadmap/PHASE_8_FIFTEENTH_HERO.md) owns that COMPLETE series (**8.40**-**8.40k**): the gate found no shipped defect but raised two player-facing gaps recorded there, and **8.40f** took the wire to `/15`. The 2026-09-07 brakes direction still governs the parked presentation program, the card-authoring workbench, tier-3 bots and every other parked item.

On 2026-09-11 the user re-opened the throttle that brakes direction had closed and named **Swift** as the next hero; [thirteenth-hero dealing](Roadmap/PHASE_8_THIRTEENTH_HERO.md) owns that COMPLETE series and all of its detail, and the prior next-proposed entry, [8.35z74](Roadmap/PHASE_8_CLEANUP.md#x-835z74-repair-explicitly-parked-player-path-coverage), closed the same day as discharged by 8.36f3 and 8.37a rather than executed.

On 2026-09-10 the user asked for documentation only of future four-round lobby-bot planning: [8.36i-8.36k](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md#future-four-round-lobby-bot-planning-parked-not-authorized) are parked and must not be started; nothing there changes the route or authorizes a build.

The cleanup and 8.35z83-8.35z85 fixes are complete; the user-accepted 8.36 gate remains closed; the independent 8.36a review is integrated; **8.36b-8.36d** (authenticated departure, bounded transport, attributable diagnostics) are complete under [private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md). [Product](Project/PRODUCT.md) limits the project to the user and friends: production presentation and publisher-pitch work are parked; no repeat internet playtest, remote publication or unrelated refactor is authorized.

Card highlights and CLEAR/FAST TRAVEL placement passed resource, Unity and eleven-hero visual checks. Lobby bots use the measured owner-scoped tactical policy while hidden hands/decks remain absent. `Builds/Playtest-8.44-seventeen-hero.zip` is the current completed local package; the user confirmed the outstanding ordinary-window package check worked on 2026-09-10. Additional heroes remain on hold.

On 2026-09-11 the user also named **Cutter the Sky Pirate**; [fourteenth-hero dealing](Roadmap/PHASE_8_FOURTEENTH_HERO.md) owns that COMPLETE thirteen-block series and [her shard](Rules/HERO_CUTTER.md) its inventory. Her printed "you alone win the game" was implemented FAITHFULLY on the user's 2026-09-11 answer, moving the snapshot schema to **v42** and the wire to **/14**.

## Durable boundaries

The user additionally approved **8.36e**, after 8.36b-8.36d: visible hero selection, no unpicked hero names, hover overview cards from Docs, and a logo/backdrop instead of the pre-match map. Its bounded closing checks live in Phase 8; this does not authorize the parked presentation program or new heroes.

8.36e also includes the reported disappearing lobby buttons, with state-transition visibility, reachability and action checks, and random bot hero picks from the currently legal choices instead of the first available hero.

**8.36f**, **8.36f1**, **8.36f2** and **8.36f3** were approved after 8.36e, in that order, and are complete: the reported Trailblazer resolver failure and clipped error feedback, the Watch How I Soar Defeat branch, the readable level-up comparison, the attack zoom without the attacker shrinking, and the added-roster card-by-card audit. [Private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md) owns the checks and limits.

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
