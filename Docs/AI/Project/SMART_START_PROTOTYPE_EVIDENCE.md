# Smart Start — prototype evidence

Return to [preparation owner](SMART_START.md). Audit date: 2026-10-21. This is the application-facing evidence snapshot, not a new implementation authority. Source baseline: `1966d1b38bee65b49f7213475c20de64a7848c9c`; preparation baseline: `691a8d2c`. No game changes, external contact or publication.

## What already works

The self-funded prototype is a playable game, not just a concept or visual mock-up. The following summarizes the implementation owners rather than promising new grant deliverables.

| Capability | Existing behavior and evidence boundary |
| --- | --- |
| Team play and setup | Private Host/Join, individual hero seats, 2v2 through 5v5, 32 configured heroes, four maps, host-selected Waves/Lives. The proposed commercial focus remains 3v3. See [current implementation](CURRENT_IMPLEMENTATION.md). |
| Complete tactical match | Card commitments/reveal, initiative and turns, movement and positional choices, attack/defense and abilities, minions and lane push, defeat/respawn, coins and level-up, victory and per-hero scoreboard. See [implementation](CURRENT_IMPLEMENTATION.md), [progression](HERO_PROGRESSION.md), [combat](COMBAT_AND_CONSEQUENCES.md). |
| Large-map play | Two 4v4/5v5 boards, two independent lane battles/pushes with a shared turn order; either Throne capture ends the match. See [maps](../Maps/README.md). |
| Player interface | Board interaction, hero/card trays, card inspection, legal action/choice presentation, runtime tooltips, turn/phase feedback and optional experimental timers/overlays. See [HUD](MATCH_HUD.md) and [presentation](CONFIGURED_PRESENTATION.md). |
| Multiplayer continuity | Direct keyed TCP sessions, seat ownership and reconnect/resynchronization; compatible build/data required. Not public matchmaking, a hosted scalable service or blanket proof of internet reachability. See [implementation](CURRENT_IMPLEMENTATION.md). |
| Technical foundation | Rules independent of Unity presentation; typed commands/events, deterministic replay, versioned snapshots and bounded player Undo, plus Director diagnostics. These are implemented technical capabilities, not all end-user product features. See [architecture](ARCHITECTURE.md). |
| Experimental cooperative mode | One to three players share three Blue hero seats against Melee/Ranged/Heavy enemy behavior, with shared lobby and host authority. Separate from the grant's core competitive proposition; see [PvE implementation](PVE_IMPLEMENTATION.md). |

The latest Windows W079 package includes the large-map fixes; later W080–W082 changes are source-only. Mac W075 is older and native launch/cross-platform networking are not verified. Exact artifact identities and coverage belong to [build records](BUILDS.md). The current prototype has no audio. New identities/card presentation for six selected kits, a complete 3D world for every board-bound piece and interaction cue, one production-ready map/free camera, animation/VFX and audio remain next-phase scope. Screen-space cards/HUD remain 2D. A later AI-assisted slice would be additional self-funded baseline evidence, not retroactively funded work.

## Reproducible local match audit

Run `./Tools/Get-SmartStartMatchEvidence.ps1` from the repository root. The read-only [audit script](../../../Tools/Get-SmartStartMatchEvidence.ps1) emits JSON with relative source paths, file hashes, event timestamps, claimed-seat counts and online run identities. Raw build logs remain private and unmodified.

Scope: all 404 loose `*.log` files under `Builds`, not only filenames beginning with `match-`. Text/JSON searches found no further gameplay event records. ZIP interiors, external player machines, AppData/persistent-data fallback and absent/deleted logs are outside this inventory; it is not a lifetime total of play. Online metadata is explicitly included, including the newer W050/W059/W079 builds.

| Observed inventory | Count / interpretation |
| --- | --- |
| Timestamped gameplay logs | 67; 67 distinct SHA-256 values, no exact file duplicates |
| Logs with explicit MatchStarted | 42; all distinct start timestamps |
| Started logs with GameEnded | 16; 14 long six-seat sessions and two short four-seat sessions with local seat claims |
| Started logs without recorded GameEnded | 26; not automatically abandoned games |
| Terminal log without MatchStarted | 1; same launch and final `v520 r4 t4 #1877` outcome as the September 26 match, excluded as repeat observation |
| Long six-seat logs without recorded end | 4; three have no negative/local claims, one has five local claims; not counted as completed |
| Online metadata files / distinct run IDs | 257 / 125; BEGIN means capture creation, not proof a match started |
| Online completed run IDs | 21 after grouping host/client records by run ID; two are in Win64Smoke, leaving 19 outside that explicitly named smoke directory |

The 19 non-smoke completion IDs are **not 19 additional matches**. They overlap the timestamped records and include resumed/repeated final-state evidence: two separate September 26 run IDs end at version 520 with the same winner/cause. Excluding a smoke directory does not prove all remaining runs are human playtests. Newer host completion records include W050 (`custom-6p-w3-l3`, final version 657) and W079 (`custom-10p-w5-l5`, version 1291, ThroneCaptured). Their metadata has no start/end timestamps, so no duration or exact play date is inferred from filesystem modification time. Source semantics: [PrivateOnlineEvidenceFile](../../../Assets/GuardsDigital/Network/PrivateOnlineEvidenceFile.cs).

### Timed completed 3v3 cohort

Explicit descriptive filter: MatchStarted plus GameEnded, six distinct SeatClaimed IDs, at least 30 elapsed minutes. All 14 selected logs also have zero negative/local seat claims. The duration threshold separates this descriptive cohort from short technical runs; it does not prove independent human participation. Duration is first GameEnded minus MatchStarted, including any pauses. Distinct starts, chronology and separate terminal records support counting these as distinct sessions, not host/client copies.

| Start date/time (UTC) | Elapsed minutes |
| --- | ---: |
| 2026-09-17 19:37:37 | 104.12 |
| 2026-09-18 19:07:55 | 90.86 |
| 2026-09-22 19:12:46 | 95.80 |
| 2026-09-26 19:38:31 | 72.02 |
| 2026-09-26 20:58:59 | 75.88 |
| 2026-09-29 19:28:35 | 98.51 |
| 2026-09-30 21:30:57 | 90.26 |
| 2026-10-01 19:12:59 | 77.10 |
| 2026-10-01 20:44:31 | 108.74 |
| 2026-10-02 19:35:03 | 128.72 |
| 2026-10-06 19:13:00 | 71.24 |
| 2026-10-06 20:34:15 | 83.14 |
| 2026-10-08 19:10:24 | 94.58 |
| 2026-10-08 21:07:06 | 77.61 |

Totals calculated before rounding: **14 completed sessions, 21.1432 elapsed match-hours**, mean **90.61 minutes**, median **90.56**, range **71.24–128.72**. Starts cover 10 distinct UTC dates across a 22-calendar-day inclusive window (September 17–October 8). The last completion is October 9 in Serbia local time. This establishes repeated, sustained use of the working prototype over several weeks, not merely one demo run.

Applicant-reported participant context, recorded 28 October 2026: Stefan estimates that approximately 20 unique people have played the private prototype and that practically all have some current or past MOBA experience, to varying degrees. He reports that the game-development effort itself began because participants repeatedly asked to play again and requested additional places. This is founder recollection, not an identity-level result independently recoverable from the logs. No participant quotation, demographic breakdown, consent record, quantified return/referral rate or purchase-intent result is inferred from it.

Additional applicant account recorded in the 29–30 October 2026 interview: people outside Stefan's circle expressed interest in playing after seeing demo screenshots. No count, participant quotation, conversion or purchase result is independently verified. The first three player responses are preserved below; permission for application use remains unconfirmed and further responses are pending.

These logs do not establish unique people, age/MOBA background, voluntary retention, referrals or willingness to pay. Seat labels and online `human=` fields are software classifications, not verified identities. Do not multiply by six to claim verified person-hours. `SessionComplete` means filled seats, not victory. These are existing-prototype results, not testing of the future new content. The earlier provisional claim of 17 started-and-completed logs is corrected to 16; 17 terminal-bearing files include the repeated no-start observation.

## Initial recruitment channels and expressions of interest

Founder account supplied 2026-10-31: Stefan identifies the Facebook groups **game.rs** and **Društvene igre** as concrete places to recruit testers. He has already mentioned the game to some people in that context; approximately **5-6 people asked him to make sure to notify them when they can try it**. These were incidental mentions while discussing other topics, not a structured recruitment campaign or organized playtest invitation. Group names are supplied labels; exact group URLs, membership counts and posting access have not been verified.

Stefan expects considerably more interest from a deliberate invitation. Record this as his hypothesis, supported by the incidental positive responses but not a measured audience estimate. The number approached, channel-by-channel split, dates/records of individual replies and overlap with existing testers are not supplied. Do not call these people confirmed external testers, buyers or participants, describe them as independently discovering the game, or infer a conversion rate. This is separate from the P01-P03 played-prototype feedback and must not be added to it as additional completed responses.

Application relevance: the planned 12-18-new-player pilot now has two named recruitment channels and an initial pool of expressed testing interest. Scheduled sessions remain the initial route. A later invitation should track interest, actual attendance and return separately, deduplicating existing players before counting new external participants. No group outreach, posting, contact-data collection or submission is authorized by recording this account. Existing claims of earlier repeat requests and screenshot interest remain separate; overlap is unknown.

## Player feedback register

Three responses received through Stefan: P01/P02 on 2026-10-30 and P03 on 2026-10-31. This register preserves qualitative evidence for later application preparation; it is not a representative survey or a measured retention rate. Keep each respondent's words separate from founder corrections and interpretation. Future responses receive distinct IDs. Number invited and response rate are not yet recorded.

### P01 — first relayed player response

Source: player text pasted by Stefan into the preparation conversation. Name not supplied; P01 is an internal identifier. Exact prior relationship to the founders is unanswered. Permission for anonymous application use/quotation is **unconfirmed**; saving internally was explicitly authorized by Stefan. Do not transfer quotations or the respondent-specific summary into the submitted application until permission is recorded.

Verbatim response, preserving spelling and jokes:

> Koje igre - sve sem fps-a (mator sam), MOBA igrao, obrnuo.
>
> Prototip sam igrao 10ak, sve pobedio.
>
> Najvise mi se dopada kolicina stvari koje mozes da uradis pogresno.. partija deluje kao da moze da se dobije uvek bez obzira na greske. I "sah" deo igre gde trazis da jednom kartom imas vise  korisnih opcija u tom turn-u.
> Takodje je dobar feel kad ne odigras obvious ili kad tacno predis to isto kod protivnika.
>
> Nista me ne frustira, samo greske saigraca 😀
>
> Vracam se jer je fun, team game with friends, ne znam za
> solo que..
>
> Igru bi izabrao samo zbog in-house aspekta gde igras s drugarima protiv drugara, trenutno kako je.
>
> Otezalo bi mi kad bi poceo da gubim i okupljanje ljudi

**Founder correction and clarification:** Stefan explicitly corrected “sve pobedio”: this player lost approximately six games in a row and continued returning afterward. Record this as founder-reported context, not a player quotation or a log-verified score. Do not repeat the initial assistant interpretation that the player had only won. The closing losing remark and teammate complaint are potentially humorous and should not be coded as established churn causes.

| Topic | Supported interpretation and limits |
| --- | --- |
| Background / exposure | Reports previous MOBA experience and roughly ten times playing the prototype. Whether this means matches or separate sessions is not established; no precise age is inferred from the joke. |
| Valued gameplay | Multiple useful options from a card, non-obvious decisions and predicting opponents. Perceives opportunities to win despite mistakes; this is a subjective impression, not proof of balance or comeback rates. |
| Return motivation | Player says the game is fun with friends. Stefan confirms returning after approximately six consecutive losses. This is one qualitative example of continued play despite losses, not a cohort retention metric. |
| Current use case | Explicitly prefers organized friends-versus-friends play in the current version. Solo-queue interest is uncertain, not established or definitively rejected. |
| Barrier | Gathering players. Supports the existing group-formation concern. |
| Missing answers | Independent rules comprehension/onboarding; who initiated repeat sessions or referrals; specific alternative displaced; exact prior relationship; consent. No purchase intent or willingness to pay was asked or established. |

Internal application-summary candidate, pending consent: One respondent with prior MOBA experience, reporting roughly ten times playing the prototype, valued flexible card decisions and predicting opponents. Their stated reason for returning was enjoyable team play with friends, with gathering players a barrier. Stefan separately confirms that this participant continued playing after approximately six consecutive losses. The response supports an organized social-play use case; it does not establish solo-matchmaking demand or willingness to pay.

### P02 — second relayed player response

Source: player text pasted by Stefan on 2026-10-30, following the standing request to retain subsequent responses. Name and exact prior relationship to the founders not supplied. Permission for anonymous application use/quotation remains **unconfirmed**. The standalone “Da” is mapped by answer order to question 7 (ability to play without rules help), not question 10 (consent). No group-chat evidence was provided or inspected.

Verbatim response:

> Slabo igram igrice više, osim ako se šah ne računa, ali sam u životu najviše igrao mobe tj. prvenstveno dotu
>
> Možda petnaestak puta
>
> Volim kad je više heroja grupisano u jednom delu mape i bijemo se međusobno, ili bar gledamo kako kripove da rešimo
>
> Čekanje da neko odigra potez i predugo trajanje partije mi najviše smetaju
>
> Ovo valjda možeš sam da zaključiš ako išta na osnovu četa grupe
>
> Kad bi mi međusobno igrali i kad bi se rešio problem trajanja poteza i partija sigurno bih nastavio da igram
>
> Da
>
> Trajanje partije najviše sigurno

| Topic | Supported interpretation and limits |
| --- | --- |
| Background / exposure | Currently plays few video games apart from chess; historically mostly MOBAs, especially Dota. Reports perhaps fifteen times playing the prototype; not a verified count or an established number of separate sessions. |
| Valued gameplay | Enjoys several heroes fighting in one area and dealing with creeps. Supports enjoyment of concentrated team encounters; does not by itself demonstrate card-combo preference. |
| Friction / return condition | Waiting for others' moves and long matches are the main complaints. States willingness to continue playing within the group if turn and match duration problems are resolved. Conditional future intent, not guaranteed retention or evidence that a specific timer configuration solves the issue. |
| Rules comprehension | By questionnaire order, says they could play another match without rules explanation. Self-reported confidence after repeated play, not proof that new players can onboard independently. |
| Repeat initiation / referrals | Refers the founder to the group chat instead of providing details. Do not infer who initiated sessions or invited others without supplied evidence. |
| Missing answers | Specific competing game displaced, preferred/acceptable durations, tested timer settings, exact prior founder relationship and consent. No solo-matchmaking or purchase-demand conclusion. |

Internal application-summary candidate, pending consent: A second respondent, previously primarily a Dota/MOBA player who now plays few video games, reports approximately fifteen times playing the prototype. They enjoy concentrated hero fights and creep encounters, and report confidence playing without further rules explanation. Waiting between actions and overall match length are their main barriers; their stated interest in continued group play is conditional on improvements to those durations.

Across P01 and P02, existing-group social play appears in both responses. P01 emphasizes flexible card decisions and prediction; P02 emphasizes team encounters and raises duration as a substantial barrier. These are two qualitative accounts, not a representative demand estimate. Preserve P02's criticism alongside positive evidence. Existing configurable timers remain a possible test variable, not an established fix or authorization to change rules.

### P03 — third relayed player response

Source: player text pasted by Stefan on 2026-10-31 under the standing feedback-preservation request. Name and exact prior relationship to the founders not supplied. Anonymous application-use/quotation permission remains **unconfirmed**. Preserve internally; no submission or outreach authorized.

Verbatim response:

> Singleplayer, wow, pokemoni, nekad mobe ugl sa drugarima, igrao sam sve mobe osim LoLa, brother eew. Hon was da bomb
>
> 10ak puta
>
> Najvise mi se dopalo sto mozemo da pravimo planove i dogovaramo se, a turn based je… mada mi je smor sto ne blejimo svi zajedno na voice-u vise.
>
> Nista konkretno, ui ux polish mi jedino fali. Bolji feedback pogotovu tokom tudjih turnova. Bar jedan neki “headline” koji kaze ko je sta uradio.
>
> Zbog drustva, nisam sam trazio.
>
> Ovo ne bi zamenilo ni jednu igru koju sam igram. Zabavno mi je najvise zbog drustva.
>
> Mogao bih. Terminologija je malo confusing i neki dodatni tooltipi pi pomogli za neke specificnosti tokena, actiona itd.
>
> Ne vidim nikakve probleme oko ponovnog igranja.

| Topic | Supported interpretation and limits |
| --- | --- |
| Background / exposure | Mentions single-player games, WoW, Pokemon and previous MOBA play mostly with friends, particularly fond of HoN. Reports roughly ten times playing the prototype; exact matches versus separate sessions is unresolved. |
| Valued gameplay / social experience | Enjoys planning and coordination in a turn-based game. Misses everyone being together on voice chat; record the experience without inferring the actual voice setup or proposing a feature. |
| Return motivation / initiation | Explicitly returns for the company and did not personally request play. Do not count this as unsolicited demand, a referral or solo interest. |
| Alternatives / use case | Says this would not replace any game played alone; enjoyment is primarily social. Supports a social occasion use case, not displacement of solo games or proof of independent commercial demand. |
| UI comprehension | Requests clearer feedback during other players' turns, specifically a headline stating who did what. Calls this UI/UX polish, but the concrete concern is action legibility, not a demonstrated need for premium graphics. |
| Rules confidence | Says they could play without explanation, while terminology and token/action specifics need more tooltips. Self-report after repeated play, not new-player onboarding evidence. |
| Barrier / missing answers | Reports no obstacle to playing again; keep that alongside their UI/voice concerns rather than treating those as stated churn barriers. Exact founder relationship and consent remain missing; no willingness to pay established. |

Internal application-summary candidate, pending consent: A third respondent with prior MOBA experience reports approximately ten times playing the prototype. They value planning and coordination in turn-based play, but attribute return primarily to friends and explicitly say they did not request sessions themselves. They want clearer summaries of other players' actions and better terminology/tooltips, while expressing confidence playing without further rules explanation and reporting no barrier to returning. The game would not replace their solo gaming choices.

Current three-response reading: social group play appears in all three accounts, with different reasons and barriers. P03 provides explicit counterevidence to treating every return as self-initiated demand. P01/P03 value planning options/coordination; P02 foregrounds team encounters and duration. Keep UI comprehension separate from premium visual polish and preserve negative/conditional evidence. This small, relayed sample is not a retention rate, representative market estimate or implementation authorization.
