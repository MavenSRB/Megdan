# Guards Digital - Session Brief

Last updated: 2026-10-05

## Purpose and authority

Read once at startup. `AGENTS.md` owns process; [governance](DOCUMENTATION_GOVERNANCE.md) owns subject routing and preservation. This brief alone owns current authorization and the exact next route.

## Current implementation

Unity 6000.5.9f1 / URP 17.5. Thirty heroes pass permanent player-path gates; Emmitt is the thirty-first playable hero and his gate is active at 9.50. The default six-player Quick Game and 2v2/3v3 Quick/Long formats are selectable. Rules, typed choices, replay, Undo, snapshots, and private host-authoritative TCP play are implemented. Snapshot schema v57 persists current-turn discard history (9.49); v56's two-card reveal choice and v55's movement/turn history remain. Wire protocol is `guards-private-online/36`. No dealt hero carries an engine deferral; only p.20 Handicap cards are boxed at setup. The [implementation inventory](Project/CURRENT_IMPLEMENTATION.md) and [Phase 9 archive](Archive/Phase-Handoffs/PHASE_9.md) retain earlier detail. Explicitly parked coverage is not passing coverage.

FILL WITH BOTS uses host-owned in-process seats. Built-player 2v2/3v3 human-plus-bot paths, END TURN/UNDO, and new-hero card faces have been checked. The current `Playtest-9.37-takahide` package has one `PLAY - Host or Join.bat` launcher; older Play/Host packages remain preserved. Private logs and extracted art remain private evidence/resources.

## Current authorization and next route

- Current completed milestone: **9.49**.
- Active authorized milestone: **9.50**.
- Approved sequence: **9.45 -> 9.46 -> 9.47 -> 9.48 -> 9.49 -> 9.50 -> 9.51 -> 9.52 -> 9.53 -> 9.54**, authorized 2026-10-04 when the user directed the last two unimplemented heroes, Emmitt the Traveller and NebKher the Harbinger, through full implementation and player-path gates. [The owning roadmap](Roadmap/PHASE_9_FINAL_TWO_HEROES.md) maps the separate outcomes, checks and dependencies. E1-E3 and N1-N4 were settled by the user after viewing the fronts; each token supply is three. EM-Q3 specifies two empty hexes between Glitches, a minimum hex distance of 3. Execute serially with separate validation and commits.
- This sequence uses one implementation agent.
- Next proposed milestone: **9.50**.
- Next route: **9.50** prove all Emmitt fronts on the permanent real-control player path, then continue the approved NebKher sequence; stop after 9.54 or at a genuinely new decision outside scope.
- 9.49 dealt all eighteen Emmitt fronts, boxed Tear in Time, and prepared private card, selection, standee and shine art. Schema v57 persists current-turn discard history. T3 passed: offline 6,670/6,670; Unity Edit 7,770 passed with 37 explicit diagnostic skips; Unity Play 1,097/1,097; Windows player-build smoke green. The [roadmap](Roadmap/PHASE_9_FINAL_TWO_HEROES.md) and local milestone commit own detail. Preserve untracked user files.
- Corrective route: the user-directed `RISK-PLAYMODE-001` repair closed within 9.48. The final unfiltered Play Mode gate passed 1096/1096 after failing-then-passing focused checks; no quarantine was used.

The A-E sequence and its supplemental **9.13f** coverage are complete. Publisher-pitch work and unconfigured variants remain parked.

**[8.111](Roadmap/PHASE_8.md)** is complete: Windows package `Builds/Playtest-8.110-lobby`. **8.110** boxed the setup choices. **8.108** (lobby player names, wire /24), **8.107** (HOST/JOIN menus) and **8.93g** are complete. 8.35z87 stays parked; 8.35z71/8.35z72 closed unimplemented (2026-09-27).

On 2026-09-27 the user cleared the next hero; the agent chose **Mrak the Rockshaper**: [8.112-8.112h](Roadmap/PHASE_8_TWENTY_FIFTH_HERO.md) ([shard](Rules/HERO_MRAK.md), MR-Q1-MR-Q6 settled by the user) are complete - dealt, artworked, pickable and gated. The same-day corrective **[8.113](Roadmap/PHASE_8.md)** set Ignatia's magma supply to four with p.18's short-supply reclaim.

On 2026-09-28 the user named **Wuk the Grove Keeper** (WU-Q1-WU-Q4 settled); [8.114-8.114g](Roadmap/PHASE_8_TWENTY_SIXTH_HERO.md) is complete ([his shard](Rules/HERO_WUK.md); schema v47). **[8.116](Roadmap/PHASE_8.md)** then made Whisper's forced move fire Widget's Dragon Knight right after the move (Ruling 16c). **8.117** (every gate walks its Active effects' trigger bodies) and **8.118** (Defeats held behind a removal-prevention question are settled by its answer; schema v48) are complete. The user then closed Phase 8 and started **[Phase 9](Roadmap/PHASE_9.md)**, the printed-wording audit **9.1 -> 9.2 -> 9.3 -> 9.4 -> 9.5**; the earlier presentation plan is retired. **9.1** (optionality, W1-W3), **9.2** (targeting, W4, R1) **9.3** (conditions, W5), **9.4** (journeys read outcomes) and **9.5** (the new-hero checklist) are complete; the user then authorized **9.6 -> 9.7** (offer against outcome) ([PRINTED_WORDING](Rules/PRINTED_WORDING.md)). **9.6** and **9.7** are complete: offers checked against outcomes both ways; restrictions refused at start. **[8.115](Roadmap/PHASE_8.md)** is complete: Whisper's forced move is a full-distance Movement action (Rulings 16a-16b, [her shard](Rules/HERO_WHISPER.md)). W8 (2026-09-29, Boulder Rush) was ruled during 9.8f.

On 2026-10-01 the user chose **Gydion the Archwizard** as the TWENTY-NINTH hero: **9.30-9.30j** and the user-directed **9.30h1** are complete - dealt, artworked, pickable and gated ([roadmap](Roadmap/PHASE_9_TWENTY_NINTH_HERO.md), [shard](Rules/HERO_GYDION.md); schema v53, wire /33), built by agents as Razzle was; GY-Q1-GY-Q12 settled by the user.

Asked for an unnamed new hero the same day, the agent chose **Takahide the Warlord** (THIRTIETH) and recorded **9.32 -> 9.32a .. 9.32i** ([roadmap](Roadmap/PHASE_9_THIRTIETH_HERO.md), [shard](Rules/HERO_TAKAHIDE.md)). **9.32-9.32i** are complete (inventory; deck swaps and facedown cards, schema v54; teammate swaps; enemy-only obstacles; every face and the Ultimate; his presentation, wire /35; dealt with his art; gated); the user settled TK-Q1-TK-Q5 as recommended. Corrective **[9.36](Roadmap/PHASE_9.md)** (rail hover, facedown hatch) is complete; corrective **[9.38](Roadmap/PHASE_9.md)** (Fight as One's borrow offer) is complete. Housekeeping sequence authorized 2026-10-02: **9.14 -> 9.26 -> 8.35z70 closure with the brief trim**. On 2026-10-03 the user ruled W10 (a decline ends an "up to N") and gave the control-label directive; sequence **[9.39 -> 9.40](Roadmap/PHASE_9.md)** complete (W10 built; every choice control labelled).

After that playtest the user reported three defects; correctives **[9.33 -> 9.35](Roadmap/PHASE_9.md)** (terrain lit, spell spotlight, "up to" line moves offer 0) are complete.

On 2026-09-29 the user named **Mortimer** next and authorized [9.8-9.8g](Roadmap/PHASE_9_TWENTY_SEVENTH_HERO.md) ([his shard](Rules/HERO_MORTIMER.md)); 9.8-9.8g are complete: dealt, artworked, pickable and gated.

On 2026-09-29 the user authorized the **[bug net 9.9 -> 9.12](Roadmap/PHASE_9.md)** (complete; `Builds/Playtest-9.12-bugnet`) and then **9.13** (expected-outcome corpus; 9.13a-e and 9.14 queued; OQ-9.13 answered as W10 at 9.39), **9.15-9.20** and **9.22-9.29**, all complete except the queued 9.26 - [Phase 9](Roadmap/PHASE_9.md) owns each result. On 2026-09-30 the user authorized **Razzle** as the TWENTY-EIGHTH hero: [9.21-9.21k](Roadmap/PHASE_9_TWENTY_EIGHTH_HERO.md) are complete (RZ-Q1-RZ-Q14 settled by the user; schema v50). The same day the user reversed W6 as **W9** (an already-adjacent target is offered), built at 9.22.

**Snorri** ([8.93-8.93g](Roadmap/PHASE_8_TWENTY_FOURTH_HERO.md), [shard](Rules/HERO_SNORRI.md)) is complete. No publication authorized.

On 2026-09-18 the user selected **Widget** (Razzle withdrawn): the SEVENTEENTH hero's **8.43-8.43f** ([dealing](Roadmap/PHASE_8_SEVENTEENTH_HERO.md), [shard](Rules/HERO_WIDGET.md)) is COMPLETE and gated (WI-Q1-WI-Q3 resolved; his build-smoke exception approved in his shard). **[8.42c](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md)** is complete: status narration names the hero a public decision waits on, from `DecisionOwner`.

The EIGHTEENTH hero, **Rowenna the Vanguard**, is complete and gated: [8.57-8.57h](Roadmap/PHASE_8_EIGHTEENTH_HERO.md) and [her shard](Rules/HERO_ROWENNA.md) own the detail (RO-03 settled by the user as the ribbon colour). The same day's **8.58** (the decision window per the mockup), **8.59** (the line picker's floor), **8.54** (Bounce through Bullet Time), **8.55** (the "6+1" badge) and **[8.60](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.55-rowenna`, cut at the 8.55 tag, digest `36da22a7...`) are complete.

The NINETEENTH hero, **Min the Dragonmonk**, is complete: **[8.61-8.61k](Roadmap/PHASE_8_NINETEENTH_HERO.md)** ([her shard](Rules/HERO_MIN.md); wire `/18` at 8.61e; four MI questions settled by the user), the Silverarrow follow-ups **[8.66](Roadmap/PHASE_8.md)** and **[8.67](Roadmap/PHASE_8.md)**, and the packages **[8.65](Roadmap/PHASE_8.md)** and **[8.68](Roadmap/PHASE_8.md)** (`Builds/Playtest-8.66-silverarrow`).

The TWENTIETH hero, **Brynn the Seeker**, is COMPLETE: **[8.69-8.69h](Roadmap/PHASE_8_TWENTIETH_HERO.md)** and **[8.70](Roadmap/PHASE_8.md)** ([his shard](Rules/HERO_BRYNN.md) owns BR-Q4, BR-Q13 and BR-Q19h and the Play Mode 744 gate).

On 2026-09-21 the user chose **Hanu the Trickster** as the TWENTY-FIRST hero: **[8.71-8.71i](Roadmap/PHASE_8_TWENTY_FIRST_HERO.md)** and **[8.72](Roadmap/PHASE_8.md)** are COMPLETE ([his shard](Rules/HERO_HANU.md) owns the four new mechanisms and HA-Q answers; schema v44, wire `/20`); **[8.73](Roadmap/PHASE_8.md)** cut `Builds/Playtest-8.71-hanu`.

Current package: [9.37](Roadmap/PHASE_9.md)'s Windows `Builds/Playtest-9.37-takahide` (thirty heroes, protocol `/35`, schema 54, stamp `...73d9864f`); earlier packages, `Playtest-9.31-gydion` among them, stay preserved and cannot join. [Package history](Archive/SESSION_BRIEF_PACKAGE_HISTORY_2026_09_24.md) owns older routes.

On 2026-09-22 the user opened the TWENTY-SECOND hero, **Ignatia**: [8.75-8.75e](Roadmap/PHASE_8_TWENTY_SECOND_HERO.md) ([her shard](Rules/HERO_IGNATIA.md)) and the playtest follow-ups [8.80-8.91](Roadmap/PHASE_8.md) are complete. **8.105 supersedes the historical coin reading: live play corrected it to `☀ = Blue`, `☾ = Red`.**

The 2026-09-17 refinements **8.42 -> 8.42a -> 8.42b -> 8.42d** are complete ([playtest refinements](Roadmap/PHASE_8_PLAYTEST_REFINEMENTS.md) owns them).

On 2026-09-17 the user selected **Garrus the Gladiator** as the SIXTEENTH hero: [8.41-8.41f](Roadmap/PHASE_8_SIXTEENTH_HERO.md) is COMPLETE and [his shard](Rules/HERO_GARRUS.md) records GA-Q1 ("I think yes, he can move 0") as a user-approved interpretation.

On 2026-09-13 the user named the FIFTEENTH hero, Bain the Bounty Hunter: [8.40-8.40k](Roadmap/PHASE_8_FIFTEENTH_HERO.md) is COMPLETE (wire `/15` at 8.40f; the gate's two recorded gaps live there). The 2026-09-07 brakes direction still governs every parked item.

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
