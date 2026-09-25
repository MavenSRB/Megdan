# Guards Digital - Session Brief

Last updated: 2026-09-25

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5; all twenty-three heroes dealt; default six-player Quick Game, with 2v2/3v3 and Quick/Long formats selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v45 persists turn-slot marker positions; wire protocol is `guards-private-online/22`. Cleanup, corrections and packaging through 8.36h are complete, and 8.37/8.37b/8.37a deal Tali the Spiritcaller, give her runtime-integer decisions controls a player can answer, and compose her last deferred face. **No dealt hero now carries an engine deferral**: every printed text in the build runs, and the only parked faces are the p.20 Handicap cards p.6 returns to the box at setup. All twenty-three heroes pass the permanent player-path gate. The [implementation inventory](Project/CURRENT_IMPLEMENTATION.md) names each dealing and its milestone. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. Whisper's prepared neutral portrait now appears on both the board and hero rail. The current 8.36h candidate has one `PLAY - Host or Join.bat` launcher; older Play/Host packages remain preserved. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **8.93d**.
- Active authorized milestone: **8.93e**.
- Next proposed milestone: **8.93e**.

**Snorri** is authorized through [8.93g](Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md); [his shard](Rules/HERO_SNORRI.md) owns rules. **8.93-8.93d complete**: inventory, chosen drag/drop retained runes (v45, wire /22), staged Gold/Red, Blue and Green. Copied cards share the rune; Grace swaps from owned resting zones. Active 8.93e: Ultimate; 8.93f: tray/Initiative glyphs; 8.93g: dealt replay/Undo/player gate. [Trinkets](Roadmap/PHASE_8_TWENTY_THIRD_HERO.md) remains gated; twenty-three heroes dealt. Bot diagnosis 8.35z71 queued. No package or publication authorized.

On 2026-09-20 the user authorized and completed **[8.35z86](Roadmap/PHASE_8_CLEANUP.md)**: client-local hero-pick filter rows (one star value, AND-combined roles, same-family CLEAR); selection authority, bots, schema and wire unchanged.

On 2026-09-18 the user authorized the **[decision window](Roadmap/PHASE_8_DECISION_WINDOW.md)** ("Ok, get on it then"); **8.46 is complete** (8.46a-8.46g): every card-text decision, tied initiative, defense, removal prevention, card chooser and the level-up upgrade open in one two-state window whose controls name their consequence, a direction is six arrows on the board, and a choice of exactly one resolves in one press.

On 2026-09-18 the user selected **Widget** (Razzle withdrawn): the SEVENTEENTH hero's **8.43-8.43f** ([dealing](Roadmap/PHASE_8_SEVENTEENTH_HERO.md), [shard](Rules/HERO_WIDGET.md)) is COMPLETE and gated (WI-Q1-WI-Q3 resolved; his build-smoke exception approved in his shard). **[8.42c](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md)** is complete: status narration names the hero a public decision waits on, from `DecisionOwner`.

On 2026-09-18 the user authorized two packages, both complete: **[8.44](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.44-seventeen-hero`, seventeen heroes) and **[8.45](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.45-undo-corrections`, the same heroes plus 8.42d's visible seat rewind and 8.42e's interaction wall, content digest unchanged). Neither authorizes upload or public distribution.

The 2026-09-18 invited playtest drove **[8.47](Roadmap/PHASE_8.md)**-**[8.53](Roadmap/PHASE_8.md)** and **[8.56](Roadmap/PHASE_8.md)**, all complete: the phase-banner package, Bounce's borrowed repeat, lives paid by Tier with a worth badge, the banner speaking only when a player is up, press provenance in the log, END TURN answering the pointer alone, and the victory screen (`MatchScoreboard`, snapshot v43, wire /17) packaged as `Builds/Playtest-8.53-victory-screen`.

The EIGHTEENTH hero, **Rowenna the Vanguard**, is complete and gated: [8.57-8.57h](Roadmap/PHASE_8_EIGHTEENTH_HERO.md) and [her shard](Rules/HERO_ROWENNA.md) own the detail (RO-03 settled by the user as the ribbon colour). The same day's **8.58** (the decision window per the mockup), **8.59** (the line picker's floor), **8.54** (Bounce through Bullet Time), **8.55** (the "6+1" badge) and **[8.60](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.55-rowenna`, cut at the 8.55 tag, digest `36da22a7...`) are complete.

- 2026-09-19 (later): the user authorized two HUD corrections from playtest screenshots as roadmap amendments - **[8.62](Roadmap/PHASE_8.md)** (targeting cues read as blood-red light) and **[8.63](Roadmap/PHASE_8.md)** (the decision window above every other panel) and **[8.64](Roadmap/PHASE_8.md)** (the victory screen drops its column legend); all three COMPLETE 2026-09-20 on the user's approval of their photographs (`c9c615ec`, `4feec1c4`, `de886e51`; the 8.62 cue settled as solid corner marks by side with a full hex on hover, not a ring).

The NINETEENTH hero, **Min the Dragonmonk**, is complete: **[8.61-8.61k](Roadmap/PHASE_8_NINETEENTH_HERO.md)** ([her shard](Rules/HERO_MIN.md); wire `/18` at 8.61e; four MI questions settled by the user), the Silverarrow follow-ups **[8.66](Roadmap/PHASE_8.md)** and **[8.67](Roadmap/PHASE_8.md)**, and the packages **[8.65](Roadmap/PHASE_8.md)** and **[8.68](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.66-silverarrow`).

The TWENTIETH hero, **Brynn the Seeker** (opened 2026-09-21), is COMPLETE the same day: **[8.69-8.69h](Roadmap/PHASE_8_TWENTIETH_HERO.md)** ([his shard](Rules/HERO_BRYNN.md)) - dealt with his picker row (three stars; Tactician, Damager, Durable) proven on the rendered lobby and gated at Play Mode 744 with zero skips; BR-Q4, BR-Q13 and BR-Q19h were settled by the user, the last built as **[8.70](Roadmap/PHASE_8.md)** (an empty modal bullet greyed at the choice for every face sharing the contract).

On 2026-09-21 the user opened the TWENTY-FIRST hero and chose **Hanu the Trickster**: **[8.71-8.71i](Roadmap/PHASE_8_TWENTY_FIRST_HERO.md)** ([his shard](Rules/HERO_HANU.md)) are COMPLETE the same day (picker row three stars; Tactician, Sniper, Pusher; schema v44 and wire `/20` since 8.71a), and HA-Q15i was built as **[8.72](Roadmap/PHASE_8.md)** (the deciding seat's HUD draws the delegated card's controls). His set carries four mechanisms no earlier hero had: a rewritten Initiative, a delegated next action, an end-of-turn swap-back behind immunity, and a coupled move whose distance a teammate chooses. **[8.73](Roadmap/PHASE_8.md)** cut `Builds/Playtest-8.71-hanu` (stamp `...5040-5475f9c5`, at `22fdef2f`), the current playtest package.

The current playtest package is **[8.94](Roadmap/PHASE_8.md)**'s `Builds/Playtest-8.92-trinkets` (Windows only, cut at 8.92g before any Snorri work, protocol `/21`; earlier packages cannot join it); [the 2026-09-22 package history](Archive/SESSION_BRIEF_PACKAGE_HISTORY_2026_09_24.md) preserves the route to them.

On 2026-09-22 the user opened the TWENTY-SECOND hero ("implement Ignatia"; "keep going and dont stop until you need something from me, or when the hero is done up to standard") and settled her four inventory questions, the coin's faces (sun = Red, moon = Blue) and her magma supply (three): **[8.75 -> 8.75a -> 8.75b -> 8.75c](Roadmap/PHASE_8_TWENTY_SECOND_HERO.md)** ([her shard](Rules/HERO_IGNATIA.md)) is AUTHORIZED, through a fully selectable, artworked hero whose picker row carries four stars and Sniper/Damager/Tokens/Tactician/Pusher and whose cards show which of their two printed texts the Tie Breaker coin makes live. **8.75-8.75c** are COMPLETE the same day: composed, the coin bound into every card-text path, Chaos Incarnate's flip-and-repeat, dealt with the first four-star, five-role picker row, and gated at Play Mode 825 with zero skips. **8.75d** (a repeat's offer excluding what its printed clause excludes, and the answer with it; Tigerclaw's Blink Strike repeat closed by the same seam) is COMPLETE; **8.75e** (no live-face badge - the user: "Drop the badge and rely on the coin strip alone") is COMPLETE; **[8.80](Roadmap/PHASE_8.md)** (the Defense window's attacker card on every network seat - the choice carries it; wire `/21`) is COMPLETE from the same playtest. **[8.82](Roadmap/PHASE_8.md)** (Swift's Mark for Death and Hunting Season leash reads her Radius icon with her items - the minion could only stand still) is COMPLETE from the same playtest; **8.81** re-cut both packages with every fix (protocol `/21`: every player switches). **[8.83](Roadmap/PHASE_8.md)** (COMPLETE 2026-09-23, on "fix those issues"): Hunting Season moves up to two minions, Sabina's leash reads her icon, and a roster sweep found and fixed three unreported move families (Swift's jump pushes, Xargatha's commands, Tigerclaw's Master Thief); **8.84** packaged it. **[8.85](Roadmap/PHASE_8.md)** (complete): a primary Movement's hit surface is the whole printed action panel - it was the small badge circle, which is why players could not select it. **[8.86](Roadmap/PHASE_8.md)** (COMPLETE): every journey asserts the board actually changed - no card defect, a dozen vacuous passes made real; **8.87** packaged both. **[8.88](Roadmap/PHASE_8.md)**, **8.89** and **8.90** (immunity ruling) are COMPLETE; 8.88a and 8.91 COMPLETE.

The 2026-09-17 refinements **8.42 -> 8.42a -> 8.42b -> 8.42d** are complete ([playtest refinements](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md) owns them).

On 2026-09-17 the user selected **Garrus the Gladiator** as the SIXTEENTH hero; [sixteenth-hero dealing](Roadmap/PHASE_8_SIXTEENTH_HERO.md) owns that COMPLETE sequence (**8.41**-**8.41f**) and [his rules shard](Rules/HERO_GARRUS.md) the inventory, both preserving the user's GA-Q1 answer ("I think yes, he can move 0") as a recorded user-approved interpretation.

On 2026-09-13 the user named the FIFTEENTH hero, Bain the Bounty Hunter; [fifteenth-hero dealing](Roadmap/PHASE_8_FIFTEENTH_HERO.md) owns that COMPLETE series (**8.40**-**8.40k**): the gate found no shipped defect but raised two player-facing gaps recorded there, and **8.40f** took the wire to `/15`. The 2026-09-07 brakes direction still governs the parked presentation program, the card-authoring workbench, tier-3 bots and every other parked item.

On 2026-09-11 the user re-opened the throttle and named **Swift**; [thirteenth-hero dealing](Roadmap/PHASE_8_THIRTEENTH_HERO.md) owns that COMPLETE series; [8.35z74](Roadmap/PHASE_8_CLEANUP.md#x-835z74-repair-explicitly-parked-player-path-coverage) closed the same day as discharged by 8.36f3 and 8.37a.

[8.36i-8.36k](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md#future-four-round-lobby-bot-planning-parked-not-authorized) (four-round lobby-bot planning) are parked and must not be started.

The cleanup and 8.35z83-8.35z85 fixes are complete; the user-accepted 8.36 gate remains closed; the independent 8.36a review is integrated; **8.36b-8.36d** (authenticated departure, bounded transport, attributable diagnostics) are complete under [private-game corrections](Roadmap/PHASE_8_PRIVATE_GAME_CORRECTIONS.md). [Product](Project/PRODUCT.md) limits the project to the user and friends: production presentation and publisher-pitch work are parked; no repeat internet playtest, remote publication or unrelated refactor is authorized.

On 2026-09-11 the user also named **Cutter the Sky Pirate**; [fourteenth-hero dealing](Roadmap/PHASE_8_FOURTEENTH_HERO.md) owns that COMPLETE series and [her shard](Rules/HERO_CUTTER.md) its inventory, including her faithfully implemented solo victory (schema **v42**, wire **/14**).

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
