# Work completion index

Return to [archive index](README.md). Post-phase work records live here after completion; current plans live in [current work](../Roadmap/CURRENT_WORK.md). Preserve request, acceptance and outcome once here; exact validation evidence lives in the linked commit body. Tags are immutable local work/ID tags.

| ID | Outcome | Commit |
| --- | --- | --- |
| W-001 | Adopted iteration work records, PvE design routing and change-based build validation. | `work/W-001` |
| W-002 | Side-column inspection/pip/rail spell previews, bounded top-tray placement and unclipped player names. | `work/W-002` |
| W-003 | Private Windows package with W-002 fixes; one build, exact-artifact smokes and verified ZIP. | `work/W-003` |
| W-005 | Restored both whole blue side zones on the alternate map; revision-2 identity, movement/travel and rendered-map regressions. | `work/W-005` |
| W-006 | Simplified tied Initiative to candidate card faces with per-player ping accents and hero-token preference markers. | `work/W-006` |
| W-007 | Retained `WHO ACTS FIRST?` and centred each selector's hero token above their highlighted card. | `work/W-007` |
| W-008 | Published prepared runtime card faces/shine maps, card backs and spell-card artwork through Git LFS to private Gitea. | `work/W-008` |
| W-009 | Reused the typed small-spell hover preview on Gydion's level-up candidates. | `work/W-009` |
| W-010 | Corrected Gydion setup so all twenty-two spells start faceup in the removed/discarded spell pile. | `work/W-010` |
| W-011 | Added a background-free, pointer-transparent large red `10` through `1` overlay for the final seconds of allowance or bank. | `work/W-011` |
| W-012 | Individually anchored live white value modifiers on all 600 card faces, with shared rendering and level-up exclusion. | `work/W-012` |
| W-013 | Current-source private Windows package, including live card modifiers; verified exact artifact and ZIP. | `work/W-013` |
| W-014 | Removed the breathing gold perimeter from awaited cards while preserving interaction and standing wait cues. | `work/W-014` |
| W-015 | Retained resolved attack bonuses on spotlight cards and thickened shared white modifier numerals. | `work/W-015` |
| W-016 | Kept hand and right-side unavailable cards fully bright, preserving inspection and command restrictions. | `work/W-016` |
| W-017 | Suppressed generic small text tooltips while preserving full-card previews and defense modifier breakdowns. | `work/W-017` |
| W-018 | Kept same-identity last-minion death and Lane Push movement in causal presentation order. | `work/W-018` |
| W-019 | Added independent Experimental modifier-display controls in a dedicated popup and continuous dormant-to-active source-card reminders. | `work/W-019` |
| W-020 | Recorded the approved first PvE slice (PVE-D01 to PVE-D23) and routed the W-021 to W-024 implementation sequence. | `work/W-020` |
| W-021 | Separate PvE executable skeleton: isolated assemblies, validated content, static encounter composed into the existing match scene, existing mode unchanged. | `work/W-021` |
| W-022 | PvE rules: restore-stable mode rules and additive engine hooks, enemy controller, encounter session, seeded plans and loss; existing mode unchanged. | `work/W-022` |
| W-023 | PvE presentation: encounter session behind the HUD, paced enemies, plan strips, placement, level and prompt buttons, loss banner and an enemy card editor. | `work/W-023` |
| W-026 | PvE playtest fixes: the HUD plays Blue only, enemy ties read as enemy decisions, paced steps wait for animations; an enemy-turn stall and colliding enemy card faces fixed. | `work/W-026` |

### [x] W-026 PvE playtest fixes

Approved: the user's 2026-10-11 playtest report on the first W-024 candidate package ("sometimes I play as the bot and sometimes it plays automatically", "friendly creeps are counted as enemy", find other defects from W-021 to W-023). The user kept the player settling enemy ties (PVE-D10/D12/D20) but asked that it read as a choice made for an enemy, and deferred hero token artwork: the private source corpus is not in this checkout.
Outcome: a default-off Director player side, set only by the external composition, makes the existing HUD play Blue:
- the tray follows only Blue heroes and cannot be pointed at an enemy;
- while an enemy owns the moment there is no turn control and no decision window;
- board outlines read from Blue.

Enemy ties carry an `ENEMY DECISION` banner naming the enemy and edge its plan strip. Paced enemy steps wait, at most three seconds, for the board's animations. Random-play checks found and the item fixes:
- an enemy turn that stalled behind a Blue hero's Removal prevention;
- enemy card copies sharing one face, which made inspecting that enemy throw.

Acceptance: T2 - offline control-boundary regression (a 500-game sweep run once, seven seeded cases kept), Unity Edit Mode presenter perspective regression with a no-side baseline and every match-scene presenter refreshed, affected existing presenter suites and the existing-mode golden, PvE Play Mode journey; living-document validation and diff review.

Historical evidence: completion commit addressed by immutable `work/W-026`; exact checks, observed failures, results and limitations live in its body.
### [x] W-023 PvE presentation

Approved: the player-facing PvE journey (third step of the approved W-021 to W-024 sequence), plus the user's 2026-10-10 request for editable enemy cards; the user directed that it finishes once it works, without usability or balance testing.
Outcome: an optional session decorator lets the external composition put `PveEncounterSession` behind the existing HUD, Undo and Restart. `PveEncounterView` paces enemy steps and shows:
- round, turn and seed;
- placement, granted-level and tie-prompt buttons;
- a plan strip beside each standing enemy with its cards on hover;
- a loss banner with Restart and New setup.

It also hides the HUD's enemy rail and scales a Blue rail of more than three heroes. The setup screen gains an enemy card editor that validates and saves the content JSON. The passive test player moved into the PvE assembly for reuse by the smoke.
Acceptance: T2 focused Unity Play Mode and Edit Mode checks plus the PvE harness; existing-mode Play Mode coordination checks; living-document validation and diff review. Full Unity suites are recorded in the W-024 package gate.

Historical evidence: completion commit addressed by immutable `work/W-023`; exact checks, observed failures, results and limitations live in its body.
### [x] W-022 PvE rules and engine hooks

Approved: implement PVE-D03 to PVE-D23 over additive default-off engine hooks (second step of the approved W-021 to W-024 sequence).
Outcome: `MatchModeRules` travel on the initial state and are bound after every rebuild and restore (snapshot restore, rollback, checkpoint Undo), never serialized. They switch off Lane Push, Level Up and Pity Coins; make every coin gain add nothing; send Blue respawns to the placement spaces; add round-start placement and per-hero granted-level transitions on the existing commands; and apply discard-or-defeat to enemies. The PvE layer derives seeded plans and drives enemies one command at a time (first applicable option, movement toward targets, weakest sufficient purple Defense, never respawning, end-of-round advance). Player-decided ties become prompts. A session guards player commands, groups Undo back to the last player command and detects the loss. Placeholder enemies now hunt heroes first.
Acceptance: T2 focused harness and Unity Edit Mode checks (encounter flow, determinism, Undo, restart, guard, loss, hook behavior, restore and checkpoint-Undo survival, discard policy), the existing-mode golden and affected existing suites, a full dotnet harness run, living-document validation and diff review. Pre-existing stale protocol-version expectations were recorded in the backlog.

Historical evidence: completion commit addressed by immutable `work/W-022`; exact checks, observed failures, results and limitations live in its body.
### [x] W-021 PvE skeleton executable and static encounter

Approved: a separate PvE executable that composes a static encounter on the existing match scene, with existing-mode behavior unchanged (first step of the approved W-021 to W-024 sequence).
Outcome: PvE code lives in `Assets/GuardsPve` (engine-free rules assembly, presentation assembly, editor build entry, launcher scene, non-imported content data) behind the `UNITY_EDITOR || GUARDS_PVE` define constraint. Placeholder enemy and approved-layout content load from JSON and are validated with readable messages. The composer builds the encounter through public APIs: chosen heroes on the placement spaces, enemies as Red heroes holding synthetic orange/purple cards, round-2 enemies off the board, creeps as Blue minions and a seed-derived Tie Breaker. The launcher's setup screen composes it and hands it to the existing match scene through a one-shot external composition, which leaves off-board heroes undrawn. Additive shared changes: a validated configuration-from-hands factory, the external-composition one-shot and further per-side seat colours. An existing-mode snapshot golden was pinned before any shared change.
Acceptance: T2 focused dotnet and Unity checks with build gates: golden, colour, conformance and PvE Edit Mode tests; PvE launcher Play Mode journey; existing player smoke; PvE build smoke with isolation, content-data and assembly checks; living-document validation and diff review. A pre-existing unrelated test failure was recorded in the backlog.

Historical evidence: completion commit addressed by immutable `work/W-021`; exact checks, observed failures, results and limitations live in its body.
### [x] W-020 Record the approved PvE decisions

Approved: record the first-slice PvE decisions agreed with the user in the 2026-10-10 design interview of Vorty's draft, and route the approved implementation sequence W-021 to W-024.
Outcome: the PvE design owner holds PVE-D01 to PVE-D23 (isolation, participation, map, objective, round loop, enemy identity and cards, movement, defense, ties, defeat, spawning, seeded plans, granted levels, data-driven content, presentation, placement data, hero respawn, forced discard, enemy ties, lost cards, coin grants, pre-battle triggers) with exact behavior, departures, examples and closing checks; open questions and the deferred level-lowering control are recorded; the PvE plan, backlog and brief route the sequence. A found restore defect in an existing experiment is recorded in the backlog.
Acceptance: T0 living-document validation and diff review. No code, Unity launch or build.

Historical evidence: completion commit addressed by immutable `work/W-020`; exact checks, results and limitations live in its body.
### [x] W-019 Experimental modifier displays and Active-effect source reminders

Approved: add seven independent default-off per-match controls in a dedicated Experimental popup. Hero shields always retain net minion Defense and may independently add items, Active effects, and card/continuous effects; card faces independently expose items, minions, Active effects, and card/continuous effects. A source card's violet Active-effect reminder begins when its first standing effect is created, remains through dormant and active phases, and ends with its final sourced instance; hero/radius and value treatment remain mechanically active-only.
Outcome: host settings, experimental snapshot identity, private wire /52, sparse card values, hero-shield rows, spotlight capture, staged sandbox recomposition, seat projection, and shared presentation now follow those contracts. The Experimental launcher opens a centered, scrollable popup with a dimmed backdrop and Close action, leaving the host form compact. Authoritative gameplay totals, effect activation, rules, Core snapshot v59, and W013 package are unchanged.
Acceptance: T2 focused dotnet and Unity regressions cover independent/default-off categories, wire/snapshot equality and rejection, popup binding, card/shield/spotlight projection, standing-versus-active effect visibility, creation/activation/expiry/restore/private-seat paths, and accessibility wording. Final Unity evidence passed 132/132 popup/settings Edit Mode, 3/3 affected Play Mode, 3/3 non-batch visual paths, 182/182 standing-source Edit Mode, 1/1 standing-source Play Mode, 1/1 dormant-source visual, and 151/151 final presentation checks. Popup, `+1` card overlays, shield rows, and dormant-source capture were rendered; the popup/card/shield captures were visually inspected. Living-document validation and diff review close the record. No build or remote publication.

Historical evidence: completion commit addressed by immutable `work/W-019`; exact checks, corrected failures, results and limitations live in its body.
### [x] W-018 Keep a last-minion kill at its original space

Approved from the user's report that the prior correction still pushed the wave first and then struck the respawned minion in a different zone.
Outcome: when Lane Push removes the attacked last minion and immediately reuses that miniature identity at a new spawn, the real respawn stays hidden behind a death echo at the old space. All other pushed-wave relocation waits on the same echo. The framed strike is latched when its kinetic scene starts, closing the transient-lunge race; Undo/restore cancels the held death and respawn. Authoritative Attack and Lane Push timing, rules, commands, wire /51, snapshot v59 and content are unchanged. No new build.
Acceptance: T2 same-identity death/respawn and Undo regressions; affected defeat, kinetic, conductor, spotlight, minion-view and pointer checks; rendered real-scene last-minion Attack, after-Attack completion, Lane Push respawn, old-space strike and release; documentation validation and diff review.

Historical evidence: completion commit addressed by immutable `work/W-018`; exact checks, observed failures/corrections, results and limitations live in its body.

### [x] W-017 Remove small text hover popups

Approved: remove all small text tooltips, including unavailable-action explanations; explicitly preserve defense modifier breakdowns and full card previews.
Outcome: the shared HUD tooltip lookup respects an ancestor-wide suppression barrier without erasing labels or changing input, gameplay or either defense breakdown surface. No new build.
Acceptance: T2 tooltip suppression regressions, preserved typed defense rows and totals, live enabled/disabled/nested hover checks, full-card preview and click, defense modifier and defense-card breakdown journeys, targeted non-batch visual inspection and documentation validation.

Historical evidence: completion commit addressed by immutable `work/W-017`; exact checks, initial failure/recovery, results and limitations live in its body.

### [x] W-016 Keep hand and right-side cards fully bright

Approved: never dim player hand cards; also remove dimming from unavailable cards on the right.
Outcome: shared hero-card faces ignore legacy resolution dimming; the right-side fan and hover previews have no dark veil. Commands, click/drag guards, inspection, layout and spellbook availability treatment are unchanged. No new build.
Acceptance: focused renderer/hand/spell regressions, actual resolving-hand brightness, right-side brightness and input refusal across viewport/refresh/session replacement, plus visual inspection and documentation validation.

Historical evidence: completion commit addressed by immutable `work/W-016`; exact checks, red-to-green transitions and limitations live in its body.

### [x] W-015 Resolved spotlight attack values and heavier modifiers

Approved: fix Cutter's missing movement bonus during the post-resolution attack animation and make white modifier numbers slightly thicker.
Outcome: animation hero/spell faces use their own recorded resolved Attack total; Cutter's completed Daring Strike retains +2. Shared white numerals gain modest stroke weight with dark contrast, without changing individual anchors, fitting or level-up exclusion. Public log transport is wire /51; snapshot v59 and gameplay rules are unchanged.
Scope and acceptance: T2 focused log/wire/copy/spotlight regressions, real Cutter lunge and animation exit checks, multi-digit render bounds and visual inspection. No new package or breakdown UI. W013 is unchanged. Exact failing-to-passing checks, captures, limitations and review evidence live in `work/W-015`.

Historical evidence: completion commit addressed by immutable `work/W-015`; exact checks and limitations live in its body.

### [x] W-014 Remove the breathing gold card outline

Approved: remove the breathing gold treatment shown around awaited hand cards.
Outcome: awaited/eligible card semantics, commands, assistive text, selection outlines and hand-resolution dimming are unchanged, but card faces no longer draw or join the animated gold perimeter. The standing rail/header wait cues remain; the breathing-host cap now reflects the six-rail bound.
Scope: shared card-face presentation and focused regression coverage only. No gameplay, rules, commands, card art, package or unrelated wait cue changed.
Closing checks: 163/163 focused Edit Mode checks and 1/1 real-input Play Mode tray journey; `tmp/w014-awaited-cards.png` was freshly captured at the live commitment prompt. Direct local-file preview repeatedly failed in the environment ACL helper, so independent visual inspection of that capture remains an explicit tooling limitation. Living-document validation and diff review passed; no unrelated suites.

Historical evidence: completion commit addressed by immutable `work/W-014`; exact checks, initial compiler correction and limitations live in its body.

### [x] W-013 Package current implementation as-is

Approved: explicit as-is build request; package source 482322ea including the current banner candidate, without adding proposed Shift breakdowns, decision summaries or diagrams. This authorizes packaging the candidate, not separate W-004 visual approval.
Scope: private Windows package/version only; preserve older packages and unrelated user files. Reuse unaffected focused source evidence; one build through the mandatory launcher; exact-artifact player, host-plus-two-client, board-render and packaged-launch smokes; manifest/file/ZIP integrity; documentation validation and local commit.
Outcome: W013 ZIP and extracted folder are available locally, with 184 files verified. No remote publication, gameplay changes or extra UI features.

Historical evidence: completion commit addressed by immutable `work/W-013`; exact source, artifact identity, checks and limitations live in its body.

### [x] W-012 Dynamic modifiers beside printed card values

Approved: inspect cards individually and place dynamic white signed modifiers directly right of each printed value, entirely inside the card; cover every rendering except level-up previews and leave screenshots for review.
Outcome: 578 hero fronts and 22 spells have manually inspected source-pixel anchors. Shared hero/spell faces, hands, decisions, inspection, rail/pip hovers and spell companions use current typed Core value queries, including items, active effects and contextual creep Defense. Digits fit the remaining right-edge space; zero values disappear; level-up candidates/comparison hands/companions stay clean. Unresolved target/body-dependent effects are not guessed. See the [display/value contract](../Project/CARD_VALUE_OVERLAYS.md).
Scope: read-only host/seat/wire projections and presentation; immutable printed artwork, rules and snapshot v59 remain unchanged. Wire /50 uses sparse, bounded, lossless values. No build or remote publication was requested or performed.
Closing checks: all-face anchor coverage, all-hero query coverage, six-channel item/effect refresh, targeted Defense and Undo parity, seat visibility and complete wire-copy boundary cases, late-game frame margin, level-up/presenter regressions and actual Unity bounds/white-digit captures. Friendly and enemy creep screenshots exercise real game-state calculations; multi-digit gallery captures are explicitly layout-test values. Player-experience, rules-authority, maintainability and privacy review completed. Living-document validation and diff review close the record.

Historical evidence: completion commit addressed by immutable `work/W-012`; exact checks, results, corrected failures and limitations live in its body.

### [x] W-011 Final-ten-second map countdown

Approved by the user's instruction to reuse the large red phase-message typography for a map-wide `10` through `1` countdown during the last ten seconds of turn allowance or team bank, without a background or input obstruction.
Outcome: the existing projected clock now drives one centred large blood-red outlined number for displayed seconds 10 through 1. The host and every descendant ignore picking, so hover, card selection and a real commitment command pass through it. It has no plate or background. Simultaneous allowances follow the header's shared latest expiry; mixed displayed clocks warn on the earliest final-ten deadline.
Scope: match presentation over `MatchClockState` only. No clock accounting, readiness, expiry, gameplay command, wire/snapshot contract or package changed.
Digit size: the user's follow-up enlarged the countdown from the transient banner's 150 units to 260 units while preserving the same red/outline treatment.
Closing checks: real-HUD overlay regression failed before implementation and passed afterward; focused countdown and phase-banner Edit Mode checks passed 19/19; full experimental-timer Play Mode fixture passed 2/2 including pointer transparency and command passage; non-batch 2560x1440 capture and visual review passed; living-document validation and diff review. No unrelated suites.

Historical evidence: completion commit addressed by immutable `work/W-011`; exact checks, results and limitations live in its body.

### [x] W-010 Gydion starts with all spells discarded

Approved by the user's correction that Gydion should start with all of his spells discarded.
Outcome: standard Gydion setup now creates all twenty-two spells unplayed and faceup in the existing public `RemovedFromSpellbook` zone, leaving the spellbook empty and the HUD at `SPELLBOOK 0 | 22`. Prepare Spells returns them through the standing lifecycle. Deliberate action fixtures explicitly prepare the book; the progressed Tier III fixture remains an explicitly progressed state.
Scope: superseded GY-Q1 setup state, exact setup validation, content authority and affected regressions. Casting, refill effects, public visibility, stable instances, commands, snapshot/wire schema and package remain unchanged.
Closing checks: the 2-case standard-setup regression failed against the old full book and passed after correction; Gydion dealing plus spellbook lifecycle passed 64/64 Edit Mode; affected standard/progressed UI and Prepare Spells paths passed 7/7 Play Mode; living-document validation and diff review. No broad-risk trigger or unrelated suites.

Historical evidence: completion commit addressed by immutable `work/W-010`; exact checks, results and limitations live in its body.

### [x] W-009 Gydion spell previews on level-up candidates

Approved by the user's instruction that Gydion's level-up candidates use the same small spell-card hover mechanism as his other card surfaces.
Outcome: every offered Gydion upgrade card carries its visibility-safe typed spell companions and shows the shared compact spell faces on hover/focus in both the composed Decision window and the retained round-action fallback. The preview stays within the level-up dialog and does not hide or replace the selectable card.
Scope: presentation and focused regression coverage only. No spell rules, level-up resolution, content, command, wire/snapshot contract or package changed.
Closing checks: real-offer regression failed before the projection fix and passed afterward; 51/51 affected Edit Mode checks and 1/1 focused Play Mode journey passed; non-batch 2560x1440 capture and visual review passed; living-document validation and diff review. No broad-risk trigger or unrelated suites.

Historical evidence: completion commit addressed by immutable `work/W-009`; exact checks, results and limitations live in its body.

### [x] W-008 Publish prepared card artwork to private Gitea

Approved by the user's 2026-10-09 instruction to push the card art to the private `maven/guards-digital` Gitea repository.
Outcome: tracked the prepared runtime card faces and shine maps, card backs and spell-card artwork with all 2,709 PNGs stored through Git LFS; retained their Unity metadata and source-name text sidecars. Boards, hero portraits, hero-selection art, markers, tokens and source-reference corpora remain excluded.
Scope: publication policy, Git LFS routing and `Assets/Resources/Private/Cards`, `CardBacks` and `Spells` only. No gameplay, rules, package or other private asset changed.
Closing checks: exact 6,579-file private-resource inventory; 2,709/2,709 staged PNGs verified as LFS pointers with zero raw PNG blobs; excluded-path audit; living-document validation and diff review. No Unity launch.

Historical evidence: completion commit addressed by immutable `work/W-008`; exact checks, results and limitations live in its body.

### [x] W-007 Tied-Initiative title and centred selector token

Approved by the user's follow-up to W-006: retain the title `WHO ACTS FIRST?` and place each selecting hero token in the middle above its chosen card.
Outcome: the title is the tied-card screen's only upper text. Each public preference retains the W-006 player-colour ping accent, while its hero token now occupies a stable centred row over the selected card. Eyebrow, explanatory and participant sentences, button captions and viewer-only selection treatment remain absent; candidate hover still uses the standing board/plaque identity channel.
Scope: W-006 tied-Initiative layout correction and focused coverage only. No Initiative rules, agreement semantics, other decision families, commands, wire/snapshot contract or package changed.
Closing checks: T2 focused decision-window Edit Mode regressions; affected real-input Play Mode journey; non-batch 2560x1440 capture with title, token/card centring and unclipped-panel geometry checks plus visual review; living-document validation and diff review. No broad-risk trigger or unrelated full suites. W-004 remains at its independent visual-approval gate.

Historical evidence: completion commit addressed by immutable `work/W-007`; exact checks, results and limitations live in its body.

### [x] W-006 Tied-Initiative card selection markers

Approved by the user's 2026-10-09 instruction to remove the upper text from the `WHO ACTS FIRST?` screen, clarified to show only what each participant selected using the pinged-hex effect, player colour and hero token above the card.
Outcome: the expanded tied-Initiative window shows only the candidate card faces. Each stated preference paints that participant's stable player accent around the selected face with the established steady-plus-pulse timing and places their small hero portrait above it; simultaneous different preferences remain visible and revising a statement moves the identity marker. The prior title, eyebrow, explanation, participant sentences, button captions and viewer-only selection outline are absent. Candidate hover still links to the standing board/plaque identity cue, and rebuilding after a click clears removed-control focus so that cue cannot stick.
Scope: tied-Initiative presentation and focused coverage only. No Initiative rules, agreement semantics, other decision families, coordination commands, wire/snapshot contract or package changed.
Closing checks: T2 focused presenter/view Edit Mode regressions; real-input seat-scoped Play Mode journey; non-batch 2560x1440 capture with token/card/panel geometry checks and visual review; living-document validation and diff review. No broad-risk trigger or unrelated full suites. W-004 remains at its independent visual-approval gate.

Historical evidence: completion commit addressed by immutable `work/W-006`; exact checks, results and limitations live in its body.

### [x] W-005 Restore both alternate-map blue side zones

Approved by the user's 2026-10-09 instruction to fix the entire lower-left and upper-right blue regions omitted from movement. The original import incorrectly classified their 30 fully gridded spaces as blocked water. Printed rulebook p.19 and the user-confirmed zones govern the correction.
Outcome: two neutral 15-space playable off-lane zones; original lane, spawns, rocks, artwork and coordinate inventory preserved. Runtime and offline catalogs select map revision 2 and reject obsolete revision 1. Existing package remains unchanged.
Closing checks: both movement regressions failed before correction and passed afterward; focused map, movement, Fast Travel, setup, Lane Push, snapshot/Undo, revision rejection, rendered tile classification and host/join checks; map/resource and living-document validation, diff review. T2. No hero-specific behavior, new build or unrelated full suites. W-004 remains at its independent visual-approval gate.
Historical evidence: completion commit addressed by immutable `work/W-005`; exact checks, results and limitations live in its body.

### [x] W-001 Transition to iteration and PvE design

Approved by the user's 2026-10-08 request to update documents and agent workflow, including the instruction that new builds must not rerun hours of tests for untouched behavior. The user also reaffirmed large maps as planned existing-game work; the 9.62 proposal and prerequisites are carried forward, not cancelled or implicitly started.

Outcome: compact startup context, shared backlog, separate PvE design/delivery track, preserved history and rules boundaries, and change-based package validation.
Scope: governing documents and documentation validator support. No gameplay changes, PvE mechanics, builds or publication.
Closing checks: ownership and pending-work review; preserved superseded text and links; validator positive/negative cases for work routes and archive evidence; living-document validation and git diff check. No Unity.

Historical evidence: completion commit addressed by immutable `work/W-001`; exact checks/results and limitations live in its body.

### [x] W-002 Spell previews on inspection and token pips

Approved by the user's request to connect casting-card spell hover to current-card inspection and the little card pips, plus verification of top-position tray clipping.
Outcome: inspection and Shift-hover pip cards show their typed spell faces, with public removed spells dimmed. Non-hand inspection uses a side column; the player's own hand retains the above-card column. Top-tray previews remain within available space. The user's follow-up also authorizes fixing player names clipped behind hero identity panels, with focused layout regression coverage.
Scope: presentation only, reuse the visibility-safe spell model and artwork. No commands, rule, content, wire or snapshot changes; no new package requested.
Closing checks: focused preview/visibility/placement Edit Mode regressions, real-pointer inspection and Shift-pip Play Mode journeys, top-rail and short-viewport bounds, dismissal/refresh and no-command checks; targeted rendered inspection. T2; no unrelated suites.

Completed: requested preview surfaces share the typed spell column; own-hand layout remains above. Player names have a reserved non-overlapping line. Focused checks and rendered review passed; no package built.

Historical evidence: completion commit addressed by immutable `work/W-002`; exact results, initial failure/correction and limitations live in its body.

### [x] W-003 Package the completed inspection fixes

Approved by the user's request for a new build after W-002. Outcome: a private Windows package containing the completed spell-preview and player-name fixes, with a distinct build stamp and launch instructions.
Scope: package identity, build once, artifact validation and local packaging; no gameplay changes or publication. Preserve prior packages.
Closing checks: reuse W-002's 46 Edit/6 Play green checks and visual evidence; compare against 9.100; fresh player interaction, host-plus-two-client and board-render smokes on one binary; package file/ZIP hashes and packaged launch; documentation validation. No broad-risk trigger or full-suite run.

Completed: `Builds/Playtest-W003-inspection-fixes.zip`; all artifact checks passed and prior packages preserved.

Historical evidence: completion commit addressed by immutable `work/W-003`; exact artifact identity, checks and limitations live in its body.
