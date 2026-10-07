# Phase 9 - Rules fidelity and hero intake hardening

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_9 handoffs](../Archive/Phase-Handoffs/PHASE_9.md).

## Experimental features and map preparation

Definitions moved losslessly to [the dedicated shard](PHASE_9_EXPERIMENTS_AND_MAPS.md); headings below preserve existing links.

### [x] 9.55 Record the experimental timer and header design

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-955-record-the-experimental-timer-and-header-design).


Historical evidence: [9.55 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-955-experimental-timer-design-evidence).

### 9.56 Implement and gate the experimental timer and header

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-956-implement-and-gate-the-experimental-timer-and-header).

### [x] 9.57 Record the experimental teammate coordination design

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-957-record-the-experimental-teammate-coordination-design).


Historical evidence: [9.57 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-957-teammate-coordination-design-evidence).

### 9.58 Implement and gate experimental teammate coordination

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-958-implement-and-gate-experimental-teammate-coordination).

### [x] 9.59 Prepare the experimental features for implementation

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-959-prepare-the-experimental-features-for-implementation).


Historical evidence: [9.59 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-959-experimental-preparation-evidence).

### [x] 9.60 Preserve the future map references

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-960-preserve-the-future-map-references).


Historical evidence: [9.60 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960-alternate-map-reference-evidence).

### [x] 9.60a Replace tower markers in private map artwork

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-960a-replace-tower-markers-in-private-map-artwork).


Historical evidence: [9.60a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960a-map-spawn-artwork-evidence).

### [x] 9.60b Clean and centre the replacement map glyphs

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-960b-clean-and-centre-the-replacement-map-glyphs).


Historical evidence: [9.60b completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960b-map-artwork-cleanup-evidence).

### [x] 9.60c Align replacement glyphs with their lanes

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-960c-align-replacement-glyphs-with-their-lanes).


Historical evidence: [9.60c completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-960c-map-glyph-alignment-evidence).

### [x] 9.61 Implement the alternate map for 2v2 and 3v3

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-961-implement-the-alternate-map-for-2v2-and-3v3).

Historical evidence: [9.61 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-961-completion).

### [ ] 9.62 Implement the two-lane maps for 4v4 and 5v5

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#962-implement-the-two-lane-maps-for-4v4-and-5v5).

### [x] 9.66 Disarm an END TURN press when its actor's turn has passed

Authorized 2026-10-07 by the user's playtest report: "End turn gets pressed automatically now.
After movment, after using skill." Outcome: a joined seat's asynchronous one-press END TURN
continuation is discarded as soon as a projection no longer concerns the hero whose press armed it,
even while that seat's input is disabled because another hero is acting. The stale continuation must
not survive into that hero's later turn and must not submit Hold, Stop Movement, an effect stop, or
Release Turn after a later Movement or Skill update. Preserve the existing one-press composition
across the original press's own network round trips, including a temporary input-disabled projection
that still concerns the same actor. No rules, command, wire, snapshot, bot or ordinary action-flow
change. Closing check: a deterministic joined-client regression reproduces the press crossing to an
input-disabled other-actor projection and then returning to the original actor; it fails before the
fix by submitting a command without a new press and passes after. Run the focused presenter fixture,
the directly affected END TURN input/provenance fixtures, and focused Play Mode for the live control.
T2.

Result: the joined-seat continuation now checks its actor boundary before the temporary network input
gate. Once the authoritative projection concerns another hero, the old press is discarded even though
that seat cannot act; a later turn for the same hero therefore has no press left to resume after a
Movement or Skill update. The original one-press END TURN path still resumes while an input-disabled
projection concerns the same hero. The regression failed before the fix by submitting three commands
where the one old press owned only two (`Expected: 2; But was: 3`) and passed after. Focused Unity
Edit Mode passed 122/122 across `MatchFeaturePresenterTests` and
`EndTurnPressProvenanceTests`; focused live-control Play Mode passed 1/1. No rule, command, wire,
snapshot, bot or package changed.

Completed 2026-10-07.

Historical evidence: [9.66 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-966-end-turn-continuation-boundary-evidence).

### [x] 9.68 Remove redundant tooltips from available printed actions

Authorized 2026-10-07 by the user's direction to merge the remaining completed agent work. Outcome:
an available printed-action hotspot carries no tooltip because the complete card face already prints
its rules, and it stops runtime-tooltip ancestor lookup before the resolving card's fallback label.
An unavailable action retains its concise reason tooltip. Preserve the whole-card hover face, action
highlighting, pointer reachability and exact typed invocation. No rules, command, content, wire,
snapshot or artwork change. Closing checks: focused lookup and hand-presentation regressions, plus a
live-panel hover regression proving the runtime popup remains hidden over an available action while
the hotspot remains clickable. T2.

Result: available printed-action regions now carry an explicit runtime-tooltip barrier and no local
tooltip, so neither the action's assistive label nor the resolving card's ancestor label opens over
rules already visible on the card face. Unavailable regions retain their reason. The typed invocation,
shine treatment, whole-card hover behavior and pointer click path are unchanged. Focused Unity Edit
Mode passed 146/146 across `MatchHudPresentationTests` and `RuntimeTooltipLayerTests`; the
focused live resolving-card Play Mode journey passed 1/1, including the delayed popup absence and
the accepted action click. No rule, command, content, wire, snapshot, artwork or package changed.

Completed 2026-10-07.

Historical evidence: [9.68 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-968-printed-action-tooltip-completion).

### [x] 9.69 Add an experimental Minion Battle removal cap

See [owning definition](PHASE_9_EXPERIMENTS_AND_MAPS.md#x-969-add-an-experimental-minion-battle-removal-cap).

Historical evidence: [9.69 completion](../Archive/Phase-Handoffs/PHASE_9.md#milestone-969-minion-battle-removal-cap-completion).

## Phase 9 - Rules fidelity and hero intake hardening

On 2026-09-28 the user closed Phase 8 and started Phase 9 with this work ("Stop with phas 8. Lets start phase 9 from this point forward"), replacing the earlier presentation-and-pitch plan, which is retired to [the archive](../Archive/PHASE_9_PRESENTATION_PLAN_RETIRED.md) and authorizes nothing.

Authorized 2026-09-28 by the user after reviewing the playtest record of Whisper, Ursafar, Swift, Trinkets and Tali ("Write it up and start doing it. Also make sure to leave proper stuff so the next heroes ... dont suffer from same issues"). The measured cause families: an effect that asks and then does nothing (8.82, 8.97, 8.48, 8.96); an offer that disagrees with its resolution (8.104, 8.97); a printed word composed as a different rule (8.42, 8.83, 8.115); and evidence taken where no player stands (8.80, 8.102). Bounded sequence 9.1 -> 9.2 -> 9.3 -> 9.4 -> 9.5, executed one at a time with separate validation and commits. Every finding is a failing-then-passing regression or an allow-list row quoting the clause that justifies it; a wording with two honest readings is recorded as an open question for the user and not decided here. Card text stays inert to rules: only tests read `CardSourceContent.PrintedText`.

Milestones 9.1 through 9.12 are complete; their definitions live in [the first completed shard](PHASE_9_COMPLETED.md).

Milestones 9.13 through 9.13f are complete; their definitions live in [the completed expected-outcome shard](PHASE_9_EXPECTED_OUTCOMES_COMPLETED.md). The routing headings below preserve historical archive anchors.

#### [x] 9.13 Independent expected-outcome corpus: the format, the executor and the three-hero pilot

#### [x] 9.13a Tables for Wasp, Arien, Brogan, Sabina and Tigerclaw

#### [x] 9.13b Tables for Xargatha, Dodger, Misa, Silverarrow and Ursafar

#### [x] 9.13c Tables for Whisper, Tali, Swift, Cutter and Bain

#### [x] 9.13d Tables for Garrus, Widget, Rowenna, Min and Brynn

#### [x] 9.13e Tables for Hanu, Ignatia, Trinkets and Snorri, and the rulebook ledger invariants

#### [x] 9.13f Close recorded supplemental outcome coverage

### [x] 9.41 Readable faded-card hover

Authorized 2026-10-03 after 9.13f: faded cards display at full brightness on hover; remove the redundant separate card-text tooltip, preserving printed face text and action eligibility. T2: faded/readable/restored states, unchanged unavailable-card input, affected card-hover regressions and focused Unity player-path/visual checks. No level-up or network correction is implied.

Result: the ordinary hand's shared full-face hover replacement explicitly renders at full opacity and omits the resting unavailable veil, while the hidden anchor retains its projected unavailable or resolution-dimmed state and returns unchanged on exit. Tray controls keep only `AssistiveLabel` as their tooltip; the complete face remains the sole rendering of printed rules text. The unavailable group stays enabled and focusable for inspection but carries no selection or drag command. Focused Unity evidence: Edit Mode 2/2 (`UnavailableCards_RetainDimmingAndRebuildWithoutDuplicatesCommandsOrHiddenIdentityLeaks`, `TheResolvingCard_ShinesWhileTheRestOfTheHandDims_UntilItResolvesAndReleases`) and Play Mode 1/1 (`Scene_OverlappedHandHoverRevealDoesNotStealTheCardClick`), with zero skips or inconclusive cases.

Completed 2026-10-03.

Historical evidence: [9.41 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-941-readable-faded-card-hover-evidence).

### [x] 9.42 Preserve the standing joined-seat level-up window

Authorized 2026-10-03: "Yes, reproduce and fix it." Joining players reported that the level-up window briefly appeared and then disappeared without input on current and older builds; reconnect restored it. The initial spotlight/display hypothesis was rejected after the user's timing objection: a strengthened real minion-defeat journey confirmed that the bounded local attack shot and the later round-end Level Up do not naturally overlap. The reproduced cause is joined-client state instead. A same-version temporary network/input pause deliberately strips private answer detail and commands; `DecisionWindowPresenter` retained Tied Initiative across that state but rebuilt an owning seat's already-drawn Level Up as `null`. It now retains that Level Up only while the public owner and authority version still name the same decision and the joined read model is non-actionable; the existing interaction wall keeps it unanswerable until private detail returns. Other seats still receive no window, authority advancement still clears it, and resynchronization restores controls without requiring reconnect. No rules, wire or network-authority change.

Closing evidence: `LevelUpWindowSurvivesJoinedSeatConnectionPause` failed pre-fix with the real owner-scoped Level Up replaced by `null`, then passed; the complete `DecisionWindowPresenterTests` fixture passed 24/24. The real Unity Level Up journey `InitiativeBadgeShowsEarnedBonusAndKeepsItsFootprint` passed 1/1 and produced its three expanded-window captures at 1280x720, 1920x1080 and 2560x1440. T2.

Completed 2026-10-03.

Historical evidence: [9.42 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-942-standing-joined-seat-level-up-window-evidence).

### [x] 9.43 Show the attack before its after-attack presentation

Authorized 2026-10-03: attack animations should happen before "after the attack" text happens. Inspect the presenter/conductor ordering; show the attack before its consequent prompts and visual effects, without delaying authoritative rules/network progression or changing printed timing. Cover a choice-bearing after-Attack branch, an automatic consequence, interruption/reconnect and queued attacks. T2; ordered presentation regression and focused rendered player-path/visual evidence. Keep distinct from 9.42's stale visibility correction.

Result: the public Attack row now reaches Presentation at the first authoritative resolved-attack boundary instead of waiting for the action's later lifecycle completion: hero attacks publish at their final success/defended outcome, while minion attacks publish at `AttackStarted` because p.13 fixes that non-defending arm as successful and emits no later outcome fact. The spotlight refreshes before the decision window. A newly authoritative after-Attack model is cached but drawn as absent through camera framing, the post-zoom beat and any earlier queued kinetic scenes; a kinetic scene-start notification releases it only after the framed attack's lunge has genuinely entered playback. Interruption releases the standing question, first-sight/reconnect still primes old attacks without replay, and authority, commands, printed timing, snapshot and wire format are unchanged.

Closing evidence: the new decision regression first failed with the after-Attack model already drawn, and the real Wasp/Shock minion path then demonstrated the missing Attack row while its effect choice stood; both passed after the two-arm timing correction. Focused Edit Mode passed 3/3 and the focused rendered Play Mode journey passed 1/1. Directly affected `MatchLogChannelTests`, `AttackSpotlightTests`, `KineticCueTests`, `KineticConductorTests`, `MatchFeaturePresenterTests` and `DecisionWindowPresenterTests` passed 230/230, including automatic smack pacing, queued/repeated attacks, first-sight/reconnect, projection loss, rewind and interruption; the complete real-scene `WavePresentationPlayModeTests` passed 5/5. The batch-capable visual capture `tmp/943-captures/attack-before-after-attack-question.png` was inspected at the lunge boundary. T2.

Completed 2026-10-03.

Historical evidence: [9.43 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-943-attack-before-after-attack-presentation-evidence).

### [x] 9.44 Implement hero-hover Defense contributors

Mockup approved for implementation 2026-10-03: "Implement this, and all the other stuff previous agent was working on as well." Preserve the hovered hero highlight and highlight only minions contributing to that hero's Defense: green positive and blood-red negative, without numeric labels on the minions. The user's latest correction requires a very small shield near the hero token's bottom edge containing only the centered signed modifier (illustratively "-1"), with no "DEF" text. Mockup uses an illustrative net minion modifier, not an invented complete Defense total before a card is chosen. Implementation consumes the immutable attack-target contribution previews: board and linked rail hover tint only their stable minion identities, each hovered hero body carries the summed projected minion modifier in the number-only shield, and hover exit clears every added treatment without a command. No Defense rule is recalculated in presentation.

Completed 2026-10-03.

Historical evidence: [9.44 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-944-hero-hover-defense-contributors-evidence).

## Queued by 9.13, not authorized

On 2026-10-02 the user authorized a housekeeping sequence - **9.14 -> 9.26 -> the documentation closure of 8.35z70 ([cleanup shard](PHASE_8_CLEANUP.md)) with the session-brief trim** - by quoting the agent's recommendation back as the instruction ("A small housekeeping pass when convenient: 9.14, Mortimer's card name, 9.26, the slow bot test case, closing the stale Tali entry, and trimming the brief"). Executed one at a time with separate validation and commits; nothing else is authorized by it - no other queued item, no parked item, no package.

### [x] 9.14 Print Mortimer's Tier III Attack as BRAAAAAAAAINS...! (eight a's)

Queued 2026-09-29 by 9.13's content finding: the card prints eight a's, the content display name seven. Outcome: the display name corrected to the printed spelling and Mortimer's pinned semantic identity digest re-measured and re-pinned with the reason recorded; no identity id or rule changes. Closing check: `VerifiedContentIdentityTests` green at the new pin, the card-name resolver in `ExpectedOutcomeExecutor` no longer needing its spelling bridge, harness and Edit Mode green. T1.

Result: the card prints eight a's - counted on `mortimer7__7703468__front.png` beside the Tier II `Braains...!` at the same scale - and the shard's own inventory had carried the seven-a spelling, so it is corrected with the content. `MortimerAttackFaces` now names the face `Braaaaaaaains...!`; the identity id `mortimer-braaaaaaains`, the content revision `mortimer-9-8-rev-1`, every printed value and every reading are as 9.8f dealt them, and Mortimer's pinned digest moved (`057a98dc...` -> `5a8a7b9d...`) because the display name feeds it, with that reason recorded beside the pin. The expected-outcome executor's `PrintedSpellings` bridge is retired: a reader's spelling the content does not print is now an unresolved card by design, and Mortimer's three eight-a tables resolve without it. The player-path gate row, the leash sweep's reasons, the disposition note and the tables guide read the printed spelling. No rule, snapshot or wire change.

Completed 2026-10-02.

Historical evidence: [9.14 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-914-printed-spelling-evidence).

### [x] 9.15 The match log records triggered-effect Defeats and card-text Removals

Queued 2026-09-29 by 9.13's diagnostic finding: `RuleEventLogProjection` unpacks card-action events for Movement, Attack, Fast Travel, Clear and Skill but not `TriggeredEffectProgressed`, so a Defeat inside a triggered effect (Wasp's High Voltage) logs no `MinionDefeated`; and Ignatia's card-text Removal emits no `MinionRemovalCompleted` at all. Outcome: both reach the seat-public match log and rule events like their card-action siblings, with regressions failing on the base; the executor's scoreboard fallback is then retired. Closing check: the regressions, the event-channel fixtures and the wire log round trips green; wire protocol unmoved unless a new row is required, which is recorded. T2.

Result: two gaps, one path. (1) `RuleEventLogProjection` unpacked card-action facts for the five action kinds and dropped `TurnResolutionEventKind.TriggeredEffectProgressed`, so everything a triggered effect did - Wasp's High Voltage's Defeat, its reward, its removal - reached the coin ledger and the scoreboard but never the rule-event log or the seat-public rows; the triggered kind now unpacks beside its siblings. (2) p.19's Remove (`remove@2`, Ignatia's Abrupt Combustion among its faces) took the piece to the supply and published only the space and the Lane-Push cause; the handler now reports the minion on its result (`EffectExecutionResult.RemovedMinions`, a `RemovedMinion`, on 9.19's terms for a discard and never for a prevented removal or a token), the node event carries it, and `CardActionResolutionEvent.RemovedMinionFacts` mints one `MinionRemovalCompleted` fact per minion with the typed `RemovalCompleted` kind, the minion on the target field and no Defeat outcome, so `MatchLogProjection` derives the same `MinionRemoved` row a Defeat's removal has always had; `CardTextNodeFacts` (reveals, discards, removals, in that order) is the one helper the four altitudes call. No new row kind, so the wire stays at /29 and schema v50. The expected-outcome executor's scoreboard fallback - reading a hero's Deaths and the team's minion kills where the log was silent - is retired: a Defeat is read from the log alone, so a Defeat the log does not record is a defect a table surfaces. Regressions, each comparing by name where it must compile on the base: `ImmediateLanePushTests.ACardTextRemove_WritesTheMinionRemovedRowForEverySeat` (a session's p.19 Remove of the last Battle Zone minion writes the row naming the minion and the acting hero, and no Defeat row) and `ConfiguredHighVoltageTests.HighVoltagesDefeat_ReachesTheSeatPublicLog_LikeASkillsWould` (the triggered Defeat, its removal and its coin award derived from the turn's events), both failing on the base; `CardTextRemovalLogTests` (4) proves the node-to-row translation and the fact's fields; the expected-outcome tables and the journey audit green without the fallback.

Completed 2026-10-01.

Historical evidence: [9.15 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-915-triggered-defeat-and-card-text-removal-rows-evidence).

## Live-play corrective, authorized 2026-09-29

### [x] 9.17 Charged Runestone asks for its target when a rune is active

Authorized 2026-09-29 by the user from the live playtest ("THIS CARD DOES NOT OFFER A TARGET SELECTION ... CHECK LOGS OF CURRENT GAME"). Evidence, host log `Builds/Playtest-9.12-bugnet/Logs/launches/e0bb88ebb1154e5aa4c4c8078994db54/match-20260929-192202.log`: Snorri played Inscribe the Runes at round 1 turn 1 (v23, `SkillUsed`), then Charged Runestone at round 2 turn 1 (v147): `SubmissionAccepted` and `SkillUsed` share the version, no question stood in between, and no discard followed, while Bain stood at (-1, 2) beside Snorri at (-2, 1). The face is a Mandatory `Conditional` over `AnySupportRuneV1` with mask 1-2-4 around a Mandatory Radius-hero target and three `IfAble` colour discards. Outcome: with a rune active under the current turn slot in any round, the Skill asks for its enemy-hero target in Radius and then applies the printed colour discard if able; the cause is found in the engine (the rune-active reading across rounds, the anticipated rune reading, or an "if able" tail closing a mandatory target) and fixed generically, never per card, with a regression that reproduces the logged round-2 state and fails on the base. If the log's state turns out to be a legal do-nothing play (no rune active by the printed words, or no enemy hero in Radius), the milestone records that with the exact reading and closes without a code change. Closing check: the regression failing then passing; Snorri's printed-set and journey fixtures green; harness green; Unity Edit Mode for the affected fixtures. T2.

Result: no engine defect. The host log only fits rune 3 below slot 1 (Rune Sigils repeated on a second hero at round 2 turn 2, so rune 4 sat below slot 2, and Rune Mastery was locked at Level 2). Charged Runestone prints clauses for three glyphs only - green (rune 4), silver (rune 2), blue (rune 1) - and is composed as a Mandatory `Conditional` over `AnySupportRuneV1` with mask 1-2-4, so under rune 3 the gate is false and RULING 53 keeps the card offered as a legal do-nothing play: submission and `SkillUsed` at one version, no question, no discard, exactly the log. `SnorriRoundTwoRuneTests` (11 cases) replays the logged line-up and positions through a whole round 1 with the runes inscribed and proves at round 2 turn 1 that `TurnIndex` resets to 1, the four runes keep their slots, runes 1 and 2 ask for Bain as the target, rune 3 asks nothing, a printed rune with no enemy in Radius greys the face while rune 3 keeps it offered, and Inscribed Runebomb (mask 1-2-3-4, "choose one active rune") asks under every rune. Ancestral Boon (mask 2-3) and Ancestral Grace (mask 1-2-3) share the shape. What the player saw was the card resolving at once with the tray showing rune 3. Open for the user (OQ-9.17): whether "If a rune is active" means any rune (a target question under rune 3 with nothing to apply) or one of the printed glyphs (as built); nothing is discarded under rune 3 either way. Worth considering separately: narrating a false printed gate ("no printed rune is active") instead of resolving silently.

Completed 2026-09-29.

Historical evidence: [9.17 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-917-charged-runestone-evidence).

Withdrawn 2026-09-29 by 9.17a: the rune-3 reading rested on Charged Runestone binding blue to rune 1; the anvil (rune 3) is its blue clause, and the fixture now proves the corrected binding.

## Turret piece, authorized 2026-09-29

### [x] 9.16 Trinkets' Turret piece reads as a turret

Authorized 2026-09-29 by the user from the playtest build, with a screenshot: "This is very ugly, this is supposed to be trinkets turret, make this nicer." The prepared art (`Resources/Private/Tokens/trinkets-turret.png`, a clean 302x302 cutout of the Koboldturret standee sprite) is drawn by `PlaceholderObstacleView` through the neutral standee route, so on the board it is a small dark shape inside a white ring. Outcome: the Turret is drawn as a board piece that reads as a turret at play scale - the sprite cropped to its silhouette and filling the piece, no white ring, on the same footprint and perimeter treatment the hex tokens (Mrak's rock, Wuk's Trees) use, still facing the camera as 8.102 settled, still an obstacle with no rules, wire or snapshot change. Candidate treatments are captured with the board visual capture driver and the user picks; nothing lands before that pick. Closing check: the chosen treatment's view regression (footprint, face normal toward the camera, no ring) failing on the base and passing after; the obstacle view fixtures and the Rapid Redeployment Play Mode journey green; a capture of the chosen treatment approved by the user. T2 with a visual inspection.

Result: three defects, all in presentation. The white ring was baked into the prepared file, which had gone through the hero-standee route (`Prepare-HeroTokens.ps1 -Companion -Neutral`) that whitens the ring and background and clips the figure; the black square was the Quad's opaque URP Unlit material drawing the file's transparent corners; and the figure was drawn rolled, because `LookRotation` took world-up under a top-down camera. The Turret is now treatment A as the user picked: `PreparedObstacleCutout` cuts the figure out of the prepared texture at runtime (GPU read-back, background and rim removed, cropped to the figure, cached per texture - the private art is untouched), draws it on the shipped transparent Unlit material at hex-token width with no ring or base, rolls it by the camera's own up while keeping 8.102's camera-facing normal, and outlines it with one thin black line whose single width lives in `BoardPieceOutline` (0.05 of the hex radius, about 1.5 px at the default match camera) - the width 9.18 shares. Regression `PlaceholderObstacleViewTests.PreparedTurret_IsTheOutlinedCutFigureOnTheTokenFootprint` (footprint and centring, face normal, upright roll, alpha honoured, no ring, outline present) fails on the base ("Expected 1.1778, But was 0.86") and passes after. The capture harness `BoardPieceCapturePlayModeTests` stays as the piece capture route. Captures `tmp/9-16-turret-final(.-close).png` beside Mrak's rock and a Wuk Tree; the user picked A with the outline from the four-treatment sheets.

Completed 2026-09-29.

Historical evidence: [9.16 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-916-turret-piece-evidence).

### [x] 9.18 Every token piece carries a thin black outline

Authorized 2026-09-29 by the user on the 9.16 captures: "i think all of our tokens need some sort of thin black outline to pop out more." Outcome: every board piece drawn from prepared token or obstacle art (Mrak's rock, Wuk's Trees, Mortimer's Zombies, Tali's Ice and Totem, Ignatia's magma and pyro, Trinkets' Barriers and Turret, Snorri's runes where they stand on the board, and any other token kind the catalog draws) gets one thin black outline around its figure, the same thickness at play scale for every kind, applied by the shared piece builder rather than baked into the private art; hero standees and cards are untouched. A capture sheet of every token kind before and after, at play scale and close, goes to the user before it lands. Closing check: one view regression proving the outline is drawn for each token kind from the catalog (content-derived, no kind hand-listed) failing on the base and passing after; the token and obstacle view fixtures, the board visual smoke and the affected Play Mode journeys green; the user's approval of the sheet. T2 with a visual inspection.

Result: `PlacedTokenView`, which draws every placed token, now stands each one on a black hex one outline width wider than the token on every side, its top just under the token's top so only the rim shows; the width is `BoardPieceOutline`'s one value (0.05 of the hex radius, about 1.5 px at the default match camera), the same the Turret uses. Kinds covered are read from `MatchContentCatalogs.ForThisBuild().Components`: ignatia-magma, min-grenade, min-mine-blast, min-mine-dud, min-mine-facedown, min-smoke-bomb-token, mortimer-zombie, mrak-rock, synthetic-hazard-token, synthetic-ward-token, tali-ice, tali-totem-ancestral, tali-totem-venerated, trinkets-barrier, widget-pyro, wuk-tree; the Turret is an obstacle covered by 9.16; Snorri's runes never stand on the board. Regression `PlacedTokenViewTests.EveryCatalogTokenKind_IsDrawnWithTheThinBlackOutline` walks the catalog and fails on the base ("ignatia-magma is drawn with no outline"). The user approved the before/after sheet (`tmp/9-18-tokens-before-after-close-sheet.png`) on 2026-09-30. Noted, not changed: the staged capture board's generated captions read mirrored, before and after.

Completed 2026-09-30.

Historical evidence: [9.18 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-918-token-outline-evidence).

### [x] 9.17a The Runestone's target and discard are visible when they happen

Reopened 2026-09-29 by the user after 9.17: "UI showed correct rune tho, he should have discarded blue card, it happened once more in this game as well, later down the line." The tray maps each rune by its `TurnSlotIndex`, so the blue-glyph rune shown under slot 1 at round 2 turn 1 was the engine's active rune 1, and 9.17's rune-3 inference is withdrawn. With rune 1 active, Bain the only enemy in Radius, and Bain likely holding A Game of Chance (blue), the printed play is: target Bain, Bain discards a blue card. The log shows the Skill completing inside its start command (v147) and no discard row; the second play at round 4 turn 2 (v502-v520) ended in the card's Movement, as a player does when the Skill seems to do nothing. Outcome: find which of these the engine does and correct what is wrong - (1) a mandatory target with one candidate answered without asking, so the player never sees a target selection; (2) a card-text discard by an enemy hero applied with no narration and no match-log row (the 9.15 shape), so a discard that did happen looked like nothing; (3) the blue discard not applied although a blue card was held (a colour or "if able" defect); (4) something else in the networked seat flow the local session does not show. Every defect is fixed generically with a failing-then-passing regression; the log's Bain hand is reconstructed from the printed starting hand and the logged plays, discards and retrievals to settle (3). Closing check: the regressions; a seat-level assertion that the target selection is offered (or, if single-candidate auto-answer is deliberate, that the narration names the target and the discard); Snorri's and the affected fixtures green; harness green; Unity Edit Mode for the affected fixtures. T2.

Result: content, not the engine. Snorri's glyphs are numbered by their order on Rune Sigils - 1 winged, 2 axe, 3 anvil, 4 hook - and Charged Runestone prints hook = green, axe = silver, anvil = blue; the content bound blue to rune 1, so under the anvil the gate (mask 1-2-4) was false and the Skill ended inside its start with no target and no discard, at v147 and again at round 3 turn 1 (v317, the same anvil under slot 1 until the re-inscription at round 3 turn 4). Inscribed Runebomb's rune-to-colour map had gold and blue swapped the same way. Fixed in `SnorriGreenEffectContent` (Runestone's third clause on rune 3, mask 2-3-4; Runebomb 1 gold, 2 silver, 3 blue, 4 green) with Snorri's revision moved to `snorri-9-17a-rev-1` (the 8.113 precedent), his artwork catalog rows and identity pin re-measured; every other rune-gated face re-read against its printed glyphs already matched. `SnorriRoundTwoRuneTests` (9 cases) replays the logged match - inscription at round 1 turn 1, the round run out, the play at round 2 turn 1 with Bain at (-1, 2) holding the log-reconstructed hand (Light Crossbow, Close Call, Drinking Buddies) - and fails on the base with "with the anvil rune active, Charged Runestone asks for the enemy hero in Radius Expected: some item equal to hero-bain But was: <empty>" and Runebomb's "Expected: all items equal to Gold But was: < Blue >"; two older fixtures that had the swap built in were corrected. Facts settled on the way: a single-candidate mandatory target still pauses (`PrintedIconTargetHandler` always asks); Bain held a blue card; the second play at round 4 turn 2 was pressable and the player chose the card's Movement. Not changed, queued as 9.19: a card-text discard by another hero writes no match-log row and no narration, so even a working discard is invisible at the table (a new `MatchLogEntryKind` moves the wire to /26). 9.17's rune-3 reading is withdrawn.

Completed 2026-09-29.

Historical evidence: [9.17a completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-917a-runestone-binding-evidence).

### [x] 9.19 A card-text discard is narrated and logged

Queued 2026-09-29 by 9.17a: an enemy hero's card-text discard (Charged Runestone, the Seismic faces, Trample) produces no match-log row and no narration line - only the Defense discard has a row kind - so a discard that happens is invisible at the table. Outcome: every card-text discard writes a seat-public match-log row naming the hero and the card's colour, and the status strip narrates it; a new `MatchLogEntryKind` moves the wire to `/26` (`NetworkMatchLog` rejects unknown kinds), recorded with the move. Closing check: a regression on the event channel and the wire log round trip failing on the base; the narration fixture; harness and the private-online fixtures green. T2.

Result: the discard rides the same path as 8.40f's reveal, end to end. A handler that sends a card out of a hand reports it on its result (`EffectExecutionResult.DiscardedCards`, a `DiscardedCard` naming the owner and the card, reported by `discard-chosen-card@1/@2` and `discard-owned-card@1`); the node event carries it (`EffectResolutionEvent.DiscardedCards`); `CardActionResolutionEvent.DiscardedCardFacts` mints one `CardActionResolutionEventKind.CardTextDiscardedCard` fact per card with the owner on the target field and the card on `DiscardedCardInstanceId`, and the four altitudes that published reveals now call `RevealedAndDiscardedCardFacts`, so no altitude publishes one and not the other; `RuleEventLogProjection` writes `discardedCard=` as a reference; `MatchLogProjection` derives `MatchLogEntryKind.CardDiscardedByCardText` (the thirty-fifth kind) with the subject the card's OWNER, never the acting hero; `GameplayNarrationLog` says "discarded ... from their hand" and `MatchLogPresentation` prints "DISCARDED <card>" in every seat, the row being what makes the card nameable. The wire moves `/28` -> `/29` (the closed kind vocabulary widened; `NetworkMatchLog` validates kinds with `Enum.IsDefined`, so a /28 peer is refused at the header rather than going dark), recorded in `PrivateSessionProtocol`'s notes; schema v50 unmoved. `SnorriRoundTwoRuneTests.RoundTwoTurnOne_ChargedRunestone_LogsBainsDiscardForEverySeat` drives Charged Runestone under the anvil rune and reads the row (owner Bain, his Blue, round 2, narrated) - it compares the kind by name so it compiles on the base and fails there with no such row; `CardTextDiscardLogTests` (5) proves the translation node -> fact -> log line -> row, both rows reveal-first for a node that did both, no row for a node that discarded nothing, and the fact's fields; the narration walk, the panel's closed-vocabulary walk and the kinetic-cue walk each gain the kind; nine wire-version pins move to 29.

Completed 2026-10-01.

Historical evidence: [9.19 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-919-card-text-discard-row-evidence).

## Rune marks, authorized 2026-09-30

### [x] 9.20 Snorri's rune marks are centred on their turn slots and show only their glyph

Authorized 2026-09-30 by the user after 9.17a: "can you make his rune marks a bit nicer? center them on card slots, and just have like a picture of hammer sigil anvil hook, or whatever the symbols are?" Outcome: each rune marker on Snorri's tray is drawn centred on the turn slot it is attached to (`TurnSlotIndex`), as a clean picture of its printed glyph alone - winged, axe, anvil, hook, the same four glyphs the cards print and 9.17a's binding follows - with a transparent background and the shared thin outline where it helps legibility, the active rune still distinguished as today; the dashboard/drag interaction, the marker authority, the wire and the snapshot are untouched. A before/after capture of the tray at play scale and close goes to the user before it lands. Closing check: a view regression proving each marker's centre equals its slot's centre and that the drawn image is the glyph resource for its ordinal, failing on the base and passing after; the rune tray and rune drag Play Mode fixtures and Snorri's journeys green; the user's approval of the capture. T2 with a visual inspection.

Result: each tray rune sat in a fixed 30x24 box 12 units above its slot's top edge, straddling it, and drew the whole prepared 27x22 tile - the printed glyph on opaque cream card stock - which Unity's default import had rounded to 32x16, so every rune was squashed on the tray and in the drag pop-up. Now `PrintedGlyphCutout` cuts the glyph off the card stock at runtime (GPU read-back shared with `PreparedObstacleCutout`, brightness keying, neighbouring-glyph slivers dropped, cropped, redrawn as a pale ivory picture on transparency with `BoardPieceOutline`'s thin line, cached per tile; no private art or Prepare script edited); `HeroOverviewsUnityView` centres each rune on the turn slot its `TurnSlotIndex` names at one shared scale (0.75 UI units per source pixel after the user's "a tad bit smaller", so the widest glyph draws about 19 units on the 24-unit mini card with a clear margin), the live-turn rune over the resolving card draws the same glyph at the same size, and markers no longer take the pointer; `ConfiguredCardArtImporter` (version 2) imports `Resources/Private/Markers/` without power-of-two resizing as it already did cards. Regression `SnorriRuneTrayPlayModeTests.InscribedRunes_AreTheirPrintedGlyphsCentredOnTheirTraySlots` (centre-on-slot, glyph-per-ordinal, fit with two units clear on every side, header matches the tray) fails on the base ("rune 1 is centred at (2443.33, 246.67), turn slot 3 at (2442.67, 266.67); rune 1 draws 'snorri-rune-1', not 'snorri-rune-1 (glyph)'") and on the first size ("rune 3 draws 29.5 x 17.4 on a 24.0 x 34.7 card, leaving under 2 each side") and passes after. The user approved the treatment and the size. The user then asked for the axe centred horizontally: rune boxes sized to their glyph snapped to whole screen pixels and drew runes 1 and 2 half a pixel right of centre, so each rune now covers its whole card and draws the glyph centred inside it (d6762a32), and the regression measures each rune's ink centre within 0.5 panel units of its slot (fails on c4f90c34 with "rune 2's ink is centred 0.67 across turn slot 1"). Left as found: the prepared axe tile crops its handle two pixel rows short, and the drag pop-up still shows the printed tiles (at the correct aspect now).

Completed 2026-09-30.

Historical evidence: [9.20 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-920-rune-marks-evidence).

## Adjacent targets, authorized 2026-09-30

### [x] 9.22 Get over here! and Grappling Bolt offer an already-adjacent target

Authorized 2026-09-30 by the user while settling Razzle's RZ-Q9: "If 0 move is still a move then we were wrong on some hook cards. We should offer adjacent targets as well. I remember we talked about it and I said no. But with that rule, they should be offered." Ruling W9 supersedes W6 ([PRINTED_WORDING](../Rules/PRINTED_WORDING.md)). Outcome: Bain's Get over here! offers a unit or token in a straight line and in range with no obstacles between even when it is already adjacent (the pull moves it 0 spaces and the card resolves), and Cutter's Grappling Bolt offers an obstacle she already stands beside; each face is greyed only when no legal target exists at all. The 9.6 narrowing that excluded the do-nothing target is removed from the shared selection contract, never per card; any other face 9.6 narrowed under W6's reasoning is found by the sweep and listed. Closing check: regressions on both faces failing on the base (the adjacent target absent from the offer) and passing after; the 9.6 fixtures that pinned W6 corrected; `OfferOutcomeTests`, `BeforeActionStepOfferTests`, the expected-outcome tables, Bain's and Cutter's printed-set fixtures and journeys green; harness green; Unity Edit and Play Mode for the affected fixtures. A bounded corrective run beside 9.21a in its own worktree. T2.

Result: the 9.6 narrowing lived in the shared selection consumer: `synthetic-direct-move@3` reported a move to a space the mover already stands on as impossible, so `EffectSelectionEligibilityQuery` dropped the adjacent target and `EffectOptionFeasibilityQuery` greyed the card when it was the only one. A new shared `IEffectZeroSpaceMoveCapabilityHandler` lets the Move answer that its sentence names a destination the mover already occupies; the eligibility query keeps such a target and the offer check counts the step as a legal play. Resolution is unchanged: the "if able" move has nowhere to go, is skipped, and the card resolves. No content moved, so no revision or identity pin moved. Regressions failing on the base and passing after: `BainPrintedSetTests.ThePull_OffersATargetAlreadyAdjacent_AndPullsItNowhere` ("Expected: some item equal to (0, -1) But was: < (2, 0) >"), `BainPrintedSetTests.ThePull_WithOnlyAnAdjacentTarget_IsNotGreyed` ("Missing: < (0, -1) >") and `CutterPrintedSetTests.GrapplingBolt_OffersAnObstacleSheAlreadyStandsBeside_AndMovesHerNowhere` ("Expected: True But was: False"); the 9.6-era pins were put back to their pre-9.6 assertions (`ThePull_DoesNotOfferATargetAlreadyAdjacent` replaced; `BainUiJourneyPlayModeTests`, `CutterUiJourneyPlayModeTests` and `CutterProgressedUiJourneyPlayModeTests` restored; comments in `CutterPrintedSetTests`, `VerifiedContentIdentityTests`, `XargathaPrintedSetTests` and both shards). Sweep: of 188 selections that name the step they feed, only Get over here! (`pull-move`) and Grappling Bolt (`grapple-move`) feed a Move to a named space; an instrumented run of the offer sweeps, every printed-set suite, the gates and the random soak fired the new reading only for those two. Kept as they are, not W6's case and open to a ruling if the user wants one: Siren's Call greyed when no space beside Xargatha is free; Cold Snap and Snowstorm greyed when no enemy in radius can step; "if able" and "may" discards and retrieves, which are not moves.

Completed 2026-09-30.

Historical evidence: [9.22 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-922-adjacent-target-evidence).

## Package, authorized 2026-09-30

### [x] 9.24 Package the 2026-09-30 build without Razzle

Authorized 2026-09-30 by the user ("Can you gut out the build with all the other stuff, but no razzle?"). Outcome: a Windows playtest package `Builds/Playtest-9.24-roster27` cut from main after 9.21g, carrying everything landed since 9.12 - Trinkets' Turret redrawn (9.16), the thin outline on every token (9.18), Snorri's rune marks (9.20), Tali's pieces without captions and every token larger (9.25), Charged Runestone's and Runebomb's rune bindings (9.17a), the adjacent-target ruling W9 (9.22), 9.13's two engine fixes and expected-outcome corpus, and Razzle's engine groundwork (9.21a-9.21g: bodies, the acting-body choice, snapshot v50, wire /27) with his content unregistered, so the roster stays at twenty-seven heroes. Recipe as 9.12: version stamp first, the build-and-launch smoke, copy without logs or debug information, launcher, read-me, manifest and identity, ZIP inspected, packaged executable launched from its own folder, content digest measured. Closing check: the T3 pre-build gate (full harness, full Edit Mode, full Play Mode) green on the packaged revision at the root; package folder, ZIP and archive row recorded. T3.

Result: `Builds/Playtest-9.24-roster27` and its ZIP (88,326,342 bytes, 185 entries, no log or DoNotShip content, size-matched to the folder), cut from main `0cbbb652` (the stamp commit over 9.25's `4dfba7a8`) with stamp `1.0-9.24-roster27+20260930174509-e2ca6150`, protocol `/27`, schema 50, default content digest `57cb64d9...` and 27 heroes. Cut at 19:46 on 2026-09-30. The T3 pre-build gate ran at the root on `4dfba7a8`, whose only difference from the packaged tree is the version stamp: offline harness 5487/5487, Unity Edit Mode 6529 passed with the 37 established Explicit diagnostics skipped and zero failures, Unity Play Mode 985/985; then the build-and-launch smoke (GATE PASSED), the copy without logs or debug information, the launcher, read-me, manifest and identity, the ZIP inspection and the packaged executable launched from its own folder (alive after 30 s, log written). 9.25 landed before the gate and is in the package. Not re-run: the six-client network rehearsal (the wire moved to /27 under 9.21b's round-trip and completeness checks; the lobby is unchanged since 8.110). No macOS package. Nothing is published.

Completed 2026-09-30.

Historical evidence: [9.24 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-924-package-evidence).

### [x] 9.25 Tali's pieces lose their captions and every token grows

Authorized 2026-09-30 by the user on the playtest board's Ice and Totem pieces: "can you remove totem and ice text from these ones as well" and "also, make them all a bit bigger." Outcome: Tali's Ice and both Totems wear no caption over their prepared faces, on the terms Pyro, Min's, Ignatia's, Mrak's, Wuk's and Mortimer's pieces already do (the prepared face says what the piece is; a kind drawn without its face keeps its name), and every placed token's piece, the Turret's figure with it, takes a larger share of its space while its outline still ends inside the space's corners so the space's edge and a destination highlight stay visible. Closing check: a view regression proving Tali's three kinds are drawn with no caption, failing on the base and passing after; a content-derived regression proving every catalog kind's outline stays inside its space; the token and obstacle view fixtures and the token capture fixture green; the capture sheet of every token kind at play scale sent to the user. T2 with a visual inspection.

Result: the text was never in the art. Tali's three prepared faces are clean; "ICE" and "TOTEM" were the caption `PlacedTokenView` draws over any kind with no `PlacedTokenArtwork` entry, so her three kinds now carry empty entries on the terms Pyro, Min's, Ignatia's, Mrak's, Wuk's and Mortimer's pieces do, and a kind drawn without its face keeps its name. Both Totems are one physical piece with one face on the table too; the card that placed the piece says which rule it carries. `PlacedTokenView.DiscRadiusFraction` 0.68 -> 0.78, the Turret's figure following through `PlaceholderObstacleView.FaceWidth`; the outline keeps its width and the piece ends inside the space's corners with a tenth of the radius to spare. Regression `PlacedTokenViewTests.TalisIceAndTotems_WearNoCaptionOverTheirPreparedFaces` fails on the base ("tali-ice must wear no caption over its prepared face. Expected: <empty> But was: ICE"); `EveryCatalogTokenKind_LeavesTheSpacesEdgeVisibleAroundItsOutline` walks the catalog. The capture sheet (`tmp/9-25-tokens-capture-close.png`, with the Turret) went to the user on 2026-09-30. Noted, unchanged: the staged capture board's generated captions still read mirrored (9.18's note), and Pyro's face is still its white standee disc.

Completed 2026-09-30.

Historical evidence: [9.25 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-925-token-captions-and-size-evidence).

### [x] 9.23 Min's stance subjects declare what they publish

Queued 2026-09-30 by 9.21f: `choose-target@2` always publishes the chosen unit's identity, and snapshot load refuses an outcome the node did not declare (Magic Trick's restore first failed exactly so and now declares all three outputs). Min's Crane, Tiger and Dragon Stance subjects declare only `chosenSpace`, so restoring a match paused mid-push on those faces is expected to be refused. Outcome: a regression restoring each stance at its push, failing on the base with the refused outcome, then the declarations corrected (content only; the identity pin re-measured if the declaration feeds it, with the reason recorded) or, if the restore actually holds, the reason recorded and the item closed. Closing check: the three regressions, Min's printed-set and journey fixtures, harness and Edit Mode green. T1.

Result: the refusal was real for all three. `choose-target@2` publishes the identity of the unit it named (`chosenHero` or `chosenMinion`) whether or not the node declares it and publishes `chosenSpace` only when declared; the resolution records every published outcome; and the snapshot reader refuses an outcome the node did not declare - so a match captured at the push of Crane, Tiger or Dragon Stance, whose shared subject node (`PushAnAdjacentEnemyUnit`) declared only `chosenSpace`, could not be restored: "Snapshot effect outcome declares output 'chosenHero' that node 'crane-subject' does not publish at that type", the same for `tiger-push-subject` and `dragon-push-subject`. The subject node now declares all three outputs on Magic Trick's terms (9.21f); the printed text, the push and every other node are unchanged, and `min-9-1-rev-1` stands because no printed value or reading moved - the identity pins the declaration feeds are re-measured with this reason. `MinStanceRestoreTests` stages Min (Blue) beside Wasp and one of his Red minions on the official map, commits the stance against Wasp's lower-Initiative card, attacks the minion, names Wasp as the push's subject, captures at the distance question, restores, and pushes him one space: the three cases fail on the base with the refusal above and pass after.

Completed 2026-10-01.

Historical evidence: [9.23 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-923-stance-declarations-evidence).

### [x] 9.26 The bot tier-two seed sweep fits Unity's per-case budget

Queued 2026-09-30 by 9.21k: `BotTierTwoTests.TierTwoIsNotWorseThanTierOne_OnSeedsItWasNotTunedOn` took 197 s in the full Edit Mode run of the twenty-eight-hero roster, over the 180 s per-case budget the launcher enforces, and passed only because the budget is applied at the case's next yield. No bot code changed in the Razzle sequence; the sweep grew with the roster. Outcome: the case split by seed group or its line-up sample bounded so every case stays under 120 s, with the same seeds covered and the tier-two-not-worse claim unchanged; the split named in `Docs/AI/Testing` where the bot sweep is described. Closing check: the split cases green in Edit Mode with their durations recorded, and the full Edit Mode run green. T1.

Result: the measurement and the claim are split, and neither moved. `BotTierTwoTests` measures each gate seed (101-104, both sides) in its own `TierTwoIsMeasuredAgainstTierOne_OnOneSeedItWasNotTunedOn(seed)` case (`TestCaseSource` over the unchanged `SuiteGateSeeds`, `Order(1)`), keeps the four reports for the run in a fixture-static cache emptied by `OneTimeSetUp`, and `TierTwoIsNotWorseThanTierOne_OnSeedsItWasNotTunedOn` (`Order(2)`) asserts over `BotMatchupReport.Merge` of them exactly what it asserted before - at least four decided, at least a quarter won - plus that the merge saw all eight matches; a seed no earlier case measured is measured by the gate case itself, so a focused run of the gate alone still makes the whole claim at the old cost. The per-seed cases assert only that both sides were played. No seed, side, floor or bot policy changed; the merge reproduces `Measure`'s counts, side split, decision-weighted means and rows, and the merged gate read 6W 2L (75%) in Unity and offline alike, the same eight matches as before. Measured in Unity Edit Mode: seeds 101-104 took 41.7/61.0/43.9/45.5 s focused and 39.6/59.3/44.3/45.3 s inside the full run, the gate case under 5 ms (the offline harness 20-27 s per seed). The full run's slowest case is now `BotBrainTests.TheMovedGreedyPassStillBeatsRandomLegal` at 164 s - inside the budget, outside this milestone, and the next candidate if the roster grows again. The split is named in [the suites shard](../Testing/PLAYMODE_AND_SUITES.md). One trap recorded for the next reader: a fixture-name `-testFilter` is an explicit selection, so it also runs the fixture's `[Explicit]` instruments (here the 72-match held-out measurement); name the cases when focusing on this fixture.


Completed 2026-10-02.

Historical evidence: [9.26 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-926-bot-gate-split-evidence).

### [x] 9.27 Package the 2026-10-01 build with Razzle

Authorized 2026-10-01 by the user ("do it", on the report that Razzle had landed and the next build was his call). Outcome: a Windows playtest package `Builds/Playtest-9.27-razzle` cut from main after 9.21k, carrying everything in Playtest-9.24-roster27 plus Razzle the Ringmaster dealt and gated (9.21h-9.21k: the different-body repeat, one standee per body with the acting-body pick and cues, his eighteen faces and art, the player-path gate) and the landing's test corrections, so the roster is twenty-eight heroes, protocol /28, schema 50. Recipe as 9.24: version stamp first, the build-and-launch smoke, copy without logs or debug information, launcher, read-me, manifest and identity, ZIP inspected, packaged executable launched from its own folder, content digest measured. Closing check: the T3 pre-build gate green on the packaged revision - the full harness at the root, and the full Edit Mode and Play Mode runs reused from the 9.21k landing where the code is identical, with the reason recorded; package folder, ZIP and archive row recorded. T3.

Result: `Builds/Playtest-9.27-razzle` and its ZIP (90,013,616 bytes, 185 entries, no log or DoNotShip content, size-matched to the folder), cut from main `17c2072f` (the stamp commit over 9.21k's `c65c215f` and the 9.27 record) with stamp `1.0-9.27-razzle+20260930224123-a5f67598`, protocol `/28`, schema 50, default content digest `90515b3c...` (measured from the lobby fixture's 3v3-quick identity rows on the packaged tree) and 28 heroes. Cut at 00:43 on 2026-10-01. The T3 gate: offline harness 5513/5513 at the root on `17c2072f`'s tree (the record commit, code identical); Unity Edit Mode 6561 passed with the 37 established Explicit diagnostics skipped and zero failures, reused from the 9.21k landing on `71b3f8ff`, whose Edit-compiled code equals this tree's (the later commits are Play Mode tests, documents and the stamp); Unity Play Mode 1016/1016, reused from the 9.21k landing on `5f99960d`, code identical to this tree; the lobby fixture 31/31 on the packaged tree; then the build-and-launch smoke (GATE PASSED), the copy without logs or debug information, the launcher, read-me, manifest and identity, the ZIP inspection and the packaged executable launched from its own folder (alive after 30 s, log written). Not re-run: the six-client network rehearsal (the wire moved to /28 under 9.21j's round-trip and completeness checks; the lobby is unchanged since 8.110). No macOS package. Nothing is published.

Completed 2026-10-01.

Historical evidence: [9.27 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-927-package-evidence).

## Turn-release cue, authorized 2026-10-01

### [x] 9.28 A turn waiting on its own END TURN says so, and every banner stays up longer

Authorized 2026-10-01 by the user, on 8.35g's release pause (the strip's "TURN COMPLETE, AWAITING RELEASE"): "when the player seat reach that state, their end turn button should start flashing, and big blood red lights should say "end turn", same way they say "commit a card"...etc. Also, all those warnings should linger little bit longer." Outcome: when the viewer's own turn stands at its release (`PendingTurnRelease` naming the viewer's hero), the phase banner announces `END TURN` once per turn in 8.47's look, to that seat only (never another seat or the Director); it yields to an expanded decision window as the other banners do, is never raised for a release the seat has already pressed END TURN for (over the wire the press passes through the pause), and is taken down the moment the release no longer stands. The END TURN control flashes blood red outside its face for as long as it releases the turn, so its words stay readable. Every phase banner holds one second longer (hold 1.3 s to 2.3 s, total 1.9 s to 2.9 s). No rule, command, snapshot or wire change. Closing check: detector and presenter regressions for the release banner (viewer only, once per turn, never raised mid-press, taken down when released) and a view regression for the flash (drawn exactly while the control releases the turn), each failing on the base and passing after; `PhaseBannerTests`, `HudButtonTreatmentTests`, the END TURN presentation fixtures and `PhaseBannerPlayModeTests` green in Unity; a capture of the banner and the flashing control sent to the user. T2 with a visual inspection.

Result: `PhaseSnapshot` now carries the hero whose finished turn waits on its release (`PendingTurnRelease.ActorId`, unscoped by 8.35g), and `PhaseBannerCueDetector` announces `TURN COMPLETE` / `END TURN` (key `release:<round>:<turn>:<hero>`) once per turn when that hero is the viewer's own - never to another seat or the Director, below the Minion Battle and above the turn banner; a rewind that reaches the pause again inside the same turn is not re-announced. `PhaseBannerPresenter` takes an END TURN banner down the moment the release no longer stands. A press already on its way never raises it: over the wire the round panel refreshes first and sends the release on the push that brings the pause, and a seat waiting on the host is not published a release, so no separate guard was needed (a first draft's "withhold while composing" hook was removed as redundant). `RoundActionUnityView` rings an END TURN control whose command is `ReleaseTurn` - drawn only for the seat that owns the pause, and for the Director - with a 5-unit blood-red ring (`BoardTargetRing.BloodRedColor`, the 8.62 colour) 5 units outside its face, lit 0.4 s and dark 0.4 s on the element's own scheduler, in place of the gold sole-action outline. `PhaseBannerUnityView.HoldMs` 1300 -> 2300, so every banner stands 2.9 s. Regressions failing on the base and passing after: `PhaseBannerTests.TheViewersOwnFinishedTurn_SaysEndTurn_UntilItIsReleased` ("The seat whose finished turn waits on its own END TURN is told so. Expected: True But was: False"), `HudButtonTreatmentTests.EndTurn_ThatReleasesAFinishedTurn_FlashesBloodRedOutsideItsFace` ("A turn waiting on its own release flashes its control. Expected: not null But was: null") and `PhaseBannerTests.EveryBanner_HoldsASecondLongerThanItDidAt847` ("Expected: 2300 But was: 1300"); `PhaseBannerPlayModeTests.AFinishedTurn_SaysEndTurnAndFlashesItsControl_UntilReleased` sees the banner and a flash that really alternates on the live panel, both gone once pressed. The user approved the captures (banner, ring lit, ring dark at 1920x1080). No rule, command, snapshot or wire change.

Completed 2026-10-01.

Historical evidence: [9.28 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-928-end-turn-cue-evidence).

## Minion Battle picks, authorized 2026-10-01

### [x] 9.29 Each teammate picks their own Minion Battle removals, and matching picks remove them

Authorized 2026-10-01 by the user, from the 2026-09-30 playtest: "After minion battle removal, when team is supposed to agree what minon to remove. That UI implementation sucks. Player are struggling with it a lot." and "they now share the selection, and that is confusing for people. Stuff is changing on the screen and they dont know why." The user settled the design the same day. Each player picks their own minions. "Matching picks remove, but you need to see what other people picked as well ... not full hex outline when minion is selectable, and full outline when a teammate or you have selected it". A teammate's pick also carries that teammate's small portrait. The milestone runs now, ahead of 9.19's sequence.

Measured cause, from the host log `Builds/Playtest-9.24-roster27/Logs/launches/3e51301cecd1419faf4c7125d3179a16/match-20260930-212556.log` (Red, end of round 1): one minion took 48 s, 35 board selections from three seats and four stale refusals. Every other contested removal that night took 14-46 s. Three things caused it:
- All three seats toggle one shared authoritative selection.
- A full selection offers no Select, so a click on another minion does nothing.
- Every click clears all three agreements.

Outcome:
- On a seat-served match the choosing-team seats no longer receive the Minion Battle's Select, Deselect or Confirm.
- Each seat states its own pick set through the host agreement coordinator. Like 8.30n's Initiative preference, it is a team-scoped statement and neither snapshot nor replay state.
- A pick on a full set replaces that seat's oldest pick.
- Each set follows the authority's non-Heavy-before-Heavy order.
- When every choosing-team seat holds the same complete set, the host submits the existing Select commands and Confirm at once, so no button remains.
- The opposing side sees neither picks nor cues, only that the other side is choosing.
- On the board, a minion the viewer may pick keeps its corner marks. Every minion the viewer or a teammate picked wears the full hex outline, plus the picking teammate's small token (the viewer's own picks carry none).
- The docked bar counts the viewer's picks and names a teammate who has not finished or picked differently.
- Bot seats follow a human teammate's complete set and never block.
- Director mode keeps its direct controls.
- p.8 is unchanged. The wire protocol moves.

Closing check: every regression below fails on the base and passes after.
- Coordinator: a seat's picks never move a teammate's; a full set swaps; the Heavy order holds; unanimity submits Select and Confirm and removes the minions; a divergent set does not; the opposing side receives no picks; a stale proposal is refused.
- Seat authorization: no Minion Battle rules command reaches a seat.
- Codec: picks round-trip.
- Presenter: the corner marks, the full outline and the teammate token, and the docked bar's wording.
- Bots: bot seats converge on a human's picks.

The six-seat rehearsal, the seat drivers and the Play Mode paths through a Minion Battle are re-pointed and green. Full dotnet harness, then complete Edit Mode and Play Mode suites: T3, because seat authorization, the wire and the whole-match drivers change. A capture of picked minions on the board goes to the user.

Result: the host agreement coordinator holds one pick set per choosing-team seat, oldest first (`TeamAgreementCoordinator.minionPicks`, legality from `MinionBattlePickRules`, which asks the authority's Heavy-last order of that seat's own set); a pick on a full set replaces the oldest pick that leaves a legal set. Seats are authorized no Minion Battle rules command (`SeatProjectionFactory`); a seat states `PickMinionBattleRemoval` / `UnpickMinionBattleRemoval` with the unit it names, and the moment every choosing seat holds the same complete set the host submits the existing Selects in that order and the existing Confirm (`TeamAgreementCoordinator.NextMinionBattleCommand`, first taking back any stray selection), so the authority's choice, legality and removal order are untouched. A team agreement projection carries each participant's `Picks` and `PicksComplete` for the choosing side only, and `ViewerHeroId`; wire `guards-private-online/30`, snapshot v50 unmoved. The board cues a pickable minion with its corner marks and every picked one with the full hex outline plus a small token for each picking teammate (`MinionBattlePickViewModel`, `MinionBattlePickBadges`); the docked bar counts the viewer's own picks, has no verb, never expands, and names a teammate unfinished or different; the other side sees "IS CHOOSING WHICH MINION TO REMOVE" and nothing else. Bot seats copy a human teammate's complete picks (`MinionBattlePickPolicy`), and a team of bots converges on the first offered set. The runtime answers a projected pick through the minion's own prompt candidate, as a click would - before that the built-player seat drivers could not pick at all. The rehearsal driver's scripted battle proves `minion-divergent` and `minion-own-picks-kept` in a built six-client match; seeded test walks keep their trajectories through `TeamAgreementTestDriver.EnumerateWalkCommands`.

Completed 2026-10-01.

Historical evidence: [9.29 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-929-minion-battle-picks-evidence).

## Package, authorized 2026-10-01 (Gydion)

### [x] 9.31 Package the 2026-10-01 build with Gydion

Authorized 2026-10-01 by the user ("make a new build after", once Gydion was gated and the worktrees cleaned). Outcome: a Windows playtest package `Builds/Playtest-9.31-gydion` cut from main after 9.30j, carrying everything in Playtest-9.27-razzle plus 9.19 (card-text discards logged), 9.15 (triggered-effect Defeats and card-text Removals logged), 9.23 (Min's stances restore mid-push), 9.28 (a turn waiting on its own END TURN says so), 9.29 (each teammate picks their own Minion Battle removals) and Gydion the Archwizard dealt and gated (9.30-9.30j with 9.30h1; every hero's unlocked Ultimate a purple card in the items strip), so the roster is twenty-nine heroes, protocol /33, schema 53. Recipe as 9.27: version stamp first, the build-and-launch smoke, copy without logs or debug information, launcher, read-me, manifest and identity, ZIP inspected, packaged executable launched from its own folder, content digest measured. Closing check: the T3 pre-build gate green on the packaged revision - the full harness and the full Play Mode run at the root on the stamped tree, and the full Edit Mode run reused from 9.30j's landing where the code is identical, with the reason recorded; package folder, ZIP and archive row recorded. T3.

Result: `Builds/Playtest-9.31-gydion` and its ZIP (92,727,312 bytes, 185 entries - 183 files and two empty folder entries - no log or DoNotShip content, size-matched to the folder), cut from main `ccfeac4a` (the stamp commit over 9.30j's `1a0f9379` and the 9.31 record) with stamp `1.0-9.31-gydion+20261001160724-a0b8662d`, protocol `/33`, schema 53, default content digest `91fc496c...` (the 3v3-quick identity row measured on the packaged tree) and 29 heroes. The T3 gate on the stamped tree: offline harness 5711/5711; Unity Play Mode complete 1063 of 1065 - the Phase 4 frame-cost line (16.8 ms against 10, read right after an 18-minute harness run, the recorded load sensitivity) and Wuk's TreesOutlastWuksDefeat respawn journey (DIRECTOR not yet shown, the respawn-timing family Mortimer's journey showed earlier the same day) - both green when rerun alone (18/18); Unity Edit Mode 6794 passed with the 37 established Explicit diagnostics skipped, reused from 9.30j's final run on `f011e52d`, whose Edit-compiled code equals this tree's (the later commits are documents and the version stamp); then the build-and-launch smoke (GATE PASSED), the copy without logs or debug information, the launcher, read-me, manifest and identity (which state the Play Mode result as above), the ZIP inspection and the packaged executable launched from its own folder (alive after 30 s, log written). Not re-run: the six-client network rehearsal (9.29 ran it on its own tree; the wire moved to /33 under round-trip and completeness checks). No macOS package. Nothing is published.

Completed 2026-10-01.

Historical evidence: [9.31 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-931-package-evidence).

## Live-play correctives, authorized 2026-10-01 (night)

Reported by the user after the 2026-10-01 evening playtest on `Builds/Playtest-9.31-gydion` (host log `Builds/Playtest-9.31-gydion/Logs/launches/b1dc22d9c8da4682b07a90af36dac92e/match-20261001-181555.log`). Executed one at a time with separate validation and commits, beside the Takahide sequence, whose numbers stay 9.32x.

### [x] 9.33 An offered terrain space is lit like every other target

Authorized 2026-10-01 by the user: "cutter silver does not work on terrain anymore." Measured cause: Grappling Bolt's choice is answered on the board alone (every candidate is a previewed space, so the decision window docks with no controls), and since 8.30h the board configured and drew its corner marks on playable tiles only. A terrain target was therefore offered, clickable (8.39j) and resolved correctly, but drawn as nothing. In the playtest, Cutter's last Grappling Bolt took 71 s to answer. Not the cause, each measured: the selection and its Move on the official map from the playtest's positions (every terrain file offered, Cutter reeled beside it), the seat and wire projection (terrain coordinates travel like any space), and the local click (`CutterUiJourneyPlayModeTests` clicks terrain at (-3,-2), green at 20:28). Outcome: every drawn tile carries the corner marks, so an offered terrain space is lit in the marks the board gives every empty candidate of that choice; the Active-effect aura stays on playable spaces; nothing else can name terrain, and a hover draws only on a lit candidate. No rule, command, snapshot or wire change. Closing check: the Cutter grapple journey asserts the offered terrain lit and an unoffered terrain dark, failing on the base and passing after; the complete Play Mode suite and the board-cue Edit Mode fixtures green. T2 with complete Play Mode, because every board render draws through the changed pass.

Result: `UnityMatchFeatureViews` configures the shared corner-highlight template on every drawn tile and runs the cue pass over every tile, so an offered terrain space wears the six corner marks; the Active-effect aura is still drawn on playable spaces only. Grappling Bolt's preview tags its candidates with a Movement consequence (the chosen space feeds a Move), so an offered terrain space is lit in the movement-destination marks the board gives every empty candidate of such a choice, and its pieces keep their target cues. `CutterUiJourneyPlayModeTests.GrapplingBoltReelsHerToTheFirstObstacleOnAFile_AndStopsBesideIt` now asserts the offered terrain at (-3,-2) lit and the unoffered terrain at (-3,3) dark before it clicks; on the base it failed with "An offered terrain space carries the corner marks every candidate wears. Expected: not null But was: null". No rule, command, snapshot or wire change.

Completed 2026-10-01.

Historical evidence: [9.33 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-933-terrain-target-cue-evidence).

### [x] 9.34 A spell attack's spotlight shows the spell card

Authorized 2026-10-01 by the user: "Gydion when he attacks animation shows syntetic card for spell attack, it needs to be actual spell card." Measured cause: a spell attack's Attack row names the spell card it was cast from (9.30c), and the attack spotlight drew every attacking card through the hero-card face builder, which has no spell artwork and draws a generated hero card. Outcome: when the attacking card is a spell, the spotlight carries the spell's own face (`SpellCardViewModel`) and draws it through `SpellCardFaceView`, the builder the spell choice and the SPELLBOOK drawer share, centred in the card box the layout already keeps clear of both pieces; a hero card attack is unchanged. No rule, command, snapshot or wire change. Closing check: `AttackSpotlightTests` proves a spell attack carries the spell face and no hero card (failing on the base, passing after) and a hero card attack carries no spell face; the spotlight and board-cue fixtures green. T2.

Result: `AttackSpotlightPresenter` reads the attacking card's definition and, for a spell (`Progression.IsSpell`), gives the scene `AttackSpell` (a `SpellCardViewModel`) in place of `AttackCard`, so one attack shows one face; `AttackSpotlightUnityView` draws it with `SpellCardFaceView.Build`, centred in the card box the layout placed clear of both pieces, and a hero card's host is reset to fill as before. `AttackSpotlightTests.ASpellAttack_CarriesTheSpellsOwnFace_AndNoHeroCard` (Magic Missile) and `AHeroCardAttack_CarriesNoSpellFace` (Shootout) pin the scene; `GydionSpellUiJourneyPlayModeTests.ShockingGraspHitsAnAdjacentHeroAndMovesHerOneSpace` finds a `gd-spell-face` inside the live spotlight's attacker card once the attack completes. On the base the presenter test failed with "Magic Missile is a spell, so the spotlight shows it as one. Expected: not null" and the journey with "The spotlight shows the spell the attack was cast from ... Expected: gydion-shocking-grasp But was: null". No rule, command, snapshot or wire change.

Completed 2026-10-01.

Corrective (2026-10-02): the defending seat's window also drew a spell attack's card (8.80's attacking card) as a generated hero card, and so did the source card of a spell's own text window. Both now carry the spell's own face (`DecisionWindowCardViewModel.Spell`, drawn by `SpellCardFaceView`), read through one shared `SpellbookPresentation.FaceOfCard` that the spotlight now uses too. `GydionDealingTests.ASpellAttacksDefenseWindow_ShowsTheSpellsOwnFace` and the Shocking Grasp journey's Defense-window check pin it.

Historical evidence: [9.34 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-934-spell-attack-spotlight-evidence).

### [x] 9.35 The "up to" line moves of Cutter, Misa and Silverarrow offer 0 spaces

Authorized 2026-10-01 by the user: "Cutters evasive shot should give option to move 0 spaces." Then, offered the two other faces sharing the gap: "yes, do it." Measured cause: Cutter's retreat ("After the attack: Move up to 2 [3] spaces in the opposite direction"), Misa's five Blue Skills ("Move up to N spaces in a straight line, ignoring obstacles; ...") and Silverarrow's Divert Attention and Lead Astray ("... move up to that number of spaces in a straight line") are offered by `choose-line-destination@1`, which walks 1..N and never offers standing still, as one required pick; their up-to policy only skipped an EMPTY offer. With one space in reach the hero had to take it (the gap `CutterProgressedUiJourneyPlayModeTests` recorded). Ruling W5 makes 0 a real choice under "up to", and Shocking Grasp's move already offers it as the mover's own space. Outcome: the chooser's optional `minimumDistance`, when explicitly bound to 0, adds the mover's own space beside at least one walked space (an unbound floor stays one, so every other body is unchanged; with no space in reach the offer stays empty, a step not taken); the three bodies bind it, so all nine faces offer moving 0, answered by a click on the hero herself. Moving 0 crosses nobody, so Misa's riders then do nothing; an enemy Silverarrow moved 0 still bounds her line at nought (RULING 37). Content revisions `cutter-9-6-rev-1`, `misa-8-36f-rev-1` and `silverarrow-8-66-rev-1` move to `-9-35-rev-1`, artwork rows and identity pins following. No snapshot or wire change. Closing check: handler and content regressions and a Play Mode journey answering 0 by clicking Cutter, failing on the base and passing after; the full harness and the affected Unity suites green. T3, because a registered capability and three dealt heroes' content digests move.

Result: `choose-line-destination@1` reads a `minimumDistance` bound to exactly 0 as the "up to" declaration and adds the mover's own space beside the walked spaces whenever at least one is offered; the walk still starts at one, and an unbound floor is unchanged. Cutter's retreat, Misa's Blue line and Silverarrow's misdirection line bind it, so Evasive Shot, Tumble Shot, Sudden Breeze, Gust of Wind, Dash and Slash, Crushing Squall, Death from Above, Divert Attention and Lead Astray offer moving 0, answered by a click on the hero, and the Move then takes no step. The 8.59 floor census (`BounceOfferTests`) now pins the exact-distance floors and these nine 0 floors separately. `CutterProgressedUiJourneyPlayModeTests.EvasiveShotRetreat_AnsweredWithAClickOnHerself_MovesHerZeroSpaces` answers the retreat with a click on Cutter and finds her where she stood; on the base it failed with "Moving 0 is offered as her own space ... Expected: collection containing (-3, 1) But was: < (-2, 0) >". Revisions `cutter-9-35-rev-1`, `misa-9-35-rev-1` and `silverarrow-9-35-rev-1`, with their 54 artwork rows and three identity pins. No snapshot or wire change.

Completed 2026-10-02.

Historical evidence: [9.35 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-935-up-to-line-move-evidence).

## Rail hover and facedown hatch, queued 2026-10-02

Reported by the user on the 9.32h captures (`tmp/9-32h-takahide/b-facedown-gold-other-seats-*.png` and `b-facedown-gold-his-seat-*.png`), relayed by the "Agent work status review" session.

### [x] 9.36 A rail mini's hover sits beside the rail, and a facedown mini's hatch reads at mini size

Queued 2026-10-02. Authorized 2026-10-02 by the user, as recorded, with the hatch settled: "a bolder diagonal hatch with fewer, thicker strokes. approved." - on a mini, fewer and thicker diagonal strokes; the full-size hover keeps the fine hatch. Two presentation defects, one corrective; neither moves a rule, command, snapshot or wire.

(1) Rail hover placement. `HeroOverviewsUnityView.PlacePreview` places the revealed card at `anchor.xMax + 10` (left rail) or `anchor.xMin - width - 10` (right rail), where the anchor is the hovered element. For a tray or discard mini that is the mini itself, so the preview (a face, or another seat's card back) draws over the rail, covering the owner's tray and the plaque below it, although the method's own summary says "beside the rail it belongs to". The placement dates from 8.42b, so every hero's rail mini hover does this, not only Takahide's. Outcome: a rail mini's preview is placed out from the left rail's right edge and in from the right rail's left edge, clamped to the screen top, bottom and sides as now; the hero-profile hover is unchanged.

(2) Facedown hatch legibility. `FacedownHatch` (9.32g, mockup B) strokes 1 px lines at 4-unit spacing in a dark colour at alpha 0.62; at a tray mini's size this reads as plain gold at normal viewing ("is not showing the strike-through pattern"). Outcome: the hatch is legible on a tray and discard mini at 2560x1440 and 2048x1152, with fewer, thicker, higher-contrast strokes scaled to the element (the user's ruling above: a bolder diagonal hatch, fewer and thicker strokes, not a single strike); the full-size hover on his own seat keeps a fine hatch. Every seat still sees the same hatched gold mini (mockup B as amended).

Dependencies: none; independent of 9.32i. Closing check: (1) a layout test that a rail mini's preview rect does not intersect its rail column, for both rails at 2560x1440, 2048x1152 and 2560x540, failing on the base and passing after, the hero-profile placement pinned unchanged; (2) a mesh test over `FacedownHatch.generateVisualContent` asserting stroke count and width at mini size (and the fine hatch at full size), failing on the base and passing after; the HUD and Takahide presentation fixtures green; the b captures retaken at the three viewports and inspected by eye for the hover beside the rail and a visible hatch. T2.

Result: `HeroOverviewsUnityView.PlacePreview` places a rail mini's face - a turn slot, the current card, a discard, a withheld card's back - off its rail column's edge (`RailColumnOf`), 10 units out from the left rail and in from the right, clamped to the screen as before; the hero profile and a drawer spell keep their anchors. `FacedownHatch` takes a `Weight`: a mini strokes 3-unit lines 10 apart at alpha 0.85 (five strokes on the 24x34 mini, where the fine hatch drew fourteen), and the face a hover reveals keeps 9.32g's 1-unit lines 4 apart at alpha 0.62. `MinimalHexGridPlayModeTests.RailMiniHover_SitsBesideItsRail_OnBothRails_AtTheThreeValidatedViewports` hovers another seat's card back and his facedown discard on the left rail and a resolved card on the right at 2560x1440, 2048x1152 and 2560x540, and pins the profile's 8.42b placement; on the base all nine readings failed ("the preview (x:80.00, ...) draws over its rail (x:12.00, width:264.00, ...)" at 2560x1440) while the profile held. `TakahidePresentationTests.TheMiniHatch_DrawsFewerThickerDarkerStrokes_AndTheFullFaceKeepsTheFineHatch` reads the strokes the hatch's content generator paints; at 9.32g's values it failed "thicker strokes on a mini. Expected: greater than or equal to 2.5f But was: 1.0f". `EveryMiniWearsTheBoldHatch_AndOnlyTheRevealedFaceTheFine` pins which weight each site wears on every seat of a configured match. The b captures were retaken in `tmp/9-36-takahide/`. No rule, command, snapshot or wire change.

Completed 2026-10-02.

Historical evidence: [9.36 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-936-rail-hover-and-hatch-evidence).

## Package, authorized 2026-10-02 (Takahide)

### [x] 9.37 Package the 2026-10-02 build with Takahide

Authorized 2026-10-02 by the user ("Make a build if everything is done.", relayed by the "Agent work status review" session). Outcome: a Windows playtest package `Builds/Playtest-9.37-takahide` cut from main at the 9.32i tag, carrying everything in Playtest-9.31-gydion plus 9.33 (an offered terrain space is lit), 9.34 and its corrective (a spell attack shows the spell card in the spotlight and the Defense window), 9.35 ("up to" line moves offer 0 spaces), 9.36 (a rail mini's hover beside the rail; a bolder facedown hatch on a mini) and Takahide the Warlord dealt and gated (9.32-9.32i), so the roster is thirty heroes, protocol /35, schema 54. Recipe as 9.31: version stamp first, the build-and-launch smoke, the board visual smoke and the multi-client lobby smoke (a playing host and two joiners through the real lobby, FILL WITH BOTS, picks - each joiner takes the last available hero, Takahide first), copy without logs or debug information, the one `PLAY - Host or Join.bat` launcher, read-me, manifest and identity, ZIP inspected, packaged executable launched from its own folder, content digest measured; older packages preserved. Closing check: the T3 pre-build gate green on the packaged revision - the full harness and the full Play Mode run at the root on the stamped tree, and the full Edit Mode run reused from 9.32i's landing where the Edit-compiled code is identical, with the reason recorded; package folder, ZIP and archive row recorded. No upload, publication or remote. T3.

Result: `Builds/Playtest-9.37-takahide` and its ZIP (94,744,831 bytes, 185 entries, size-matched to the folder's 183 files; no log or DoNotShip content), cut from main `42238321` (the stamp commit over 9.32i's `7c219032` and the 9.37 record) with stamp `1.0-9.37-takahide+20261002181847-73d9864f`, protocol `/35`, schema 54, default content digest `416b6167...` (the 3v3-quick identity row measured on the packaged tree) and 30 heroes. The T3 gate on the stamped tree: offline harness 5812/5812; Unity Play Mode complete 1094/1094, zero skips; Unity Edit Mode 6909 passed with the 37 established Explicit diagnostics skipped, reused from 9.32i's final run on `66b4d275`, whose Edit-compiled code equals this tree's (the later commits are documents and the version stamp); then the build-and-launch smoke (GATE PASSED), the board visual smoke (0.0% near-white over 327 colours, GATE PASSED) and the multi-client lobby smoke (a playing host and two joiners through TAKE SEAT, READY, FILL WITH BOTS and the picks, Takahide drafted as the last available hero, each seat fielding its own pick at the first commitment window; SMOKE PASSED). The two later smokes ran the build the first one made from this revision, so the shipped binaries are the ones all three smokes ran. Then the copy without logs or debug information, the one launcher, read-me, manifest and identity, the ZIP inspection and the packaged executable launched from its own folder (alive after 30 s, log written). Before the record, the completed run 9.1-9.12 moved losslessly to [the first completed shard](PHASE_9_COMPLETED.md). Not re-run: the six-client network rehearsal. Not automated: a hands-on built-player match with turns, END TURN and UNDO on Takahide's seat (his Play Mode journeys cover them on this code). No macOS package. Nothing is published.

Completed 2026-10-02.

Historical evidence: [9.37 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-937-package-evidence).

## Borrowed-skill offer, authorized 2026-10-02

### [x] 9.38 Fight as One offers only a resolved Skill that can act on a different unit

Queued 2026-10-02 from a playtester's report relayed by the user (Discord, translated): "When you play a gold you may use a skill from a played card ... a green card which says that at 2 range in a straight line an enemy hero discards a card ... it did not let me select the [enemy piece] after I chose the green as the skill." Screenshot: Widget and Pyro's seat, panel "FIERY BREATH CANCELED". Authorized 2026-10-02 by the user ("ok, 9.38", relayed by the "Agent work status review" session).

Measured headlessly (scratch probe on Widget's real faces, the offline harness): Fiery Breath's targeting is the same query direct or borrowed - an enemy hero in Widget's Range 2 (WI-04: range from Widget) and in a straight line from Pyro - and Fight as One's borrowed copy excludes the unit it attacked (WI-07), which is correct. The defect is the OFFER: Fight as One's after-attack borrow (`borrow-primary-action@1`, pool `OwnedResolvedSkills`, `excludeAttackedUnit`) lists every resolved Skill without asking whether that Skill's primary can act once the attacked unit is excluded. When the only enemy hero in Range and in Pyro's line is the one just attacked, it still offers Fiery Breath; choosing it empties the Breath's mandatory target, the card stops, and the fizzle notice (`FizzleNoticePresenter`, "FIERY BREATH CANCELED") reports it. Ruling W7 ("a borrowed primary that can do nothing does not make a card playable") and 8.35z15 (a card-text step with no legal subject is not offered) both say such a Skill should not be offered; the turn's own feasibility walk already reads `BorrowedPrimaryCanAct`, but the live offer and the attacked-unit exclusion do not. Outcome: the after-attack borrow offers a resolved Skill only when its primary can act on a unit other than the attacked one (the exclusion applied when the offer is built), and with none it asks nothing (the step is a "may" with nothing to choose). The fizzle notice stays the presentation of a genuine mid-resolution stop. Closing check: a regression on the real faces - Widget attacks the only enemy hero in Range and in Pyro's line with Fight as One, Fiery Breath is resolved - asserting the borrow does not offer Fiery Breath, failing on the base (it is offered, then canceled) and passing after; the existing `WidgetSkillReuseTests` exclusion cases and the Widget journeys green. T2.

Also for the user, not part of this item: WI-04 reads "both in range and in a straight line from Pyro" as Range from Widget and the line from Pyro; an enemy two from Pyro in line but three from Widget is not offered even by a direct Fiery Breath. On 2026-10-02 the user confirmed that reading; it is recorded beside WI-04 in Widget's shard as a user ruling (an implementation reading until then), and no code change follows from it.

Result: `borrow-primary-action@1` builds its offer from one candidate function (`ActingCandidates`): the declared pool, narrowed to the cards whose printed primary could act now with the unit the clause bars excluded, wherever the turn's judgement is supplied - an empty pool is still impossible, a pool whose cards can all do nothing is declined (the "may" asks nothing), and the chooser, the answer and the anticipation read one set. The judgement is 9.6's `BorrowedPrimaryCanAct`, now the `BorrowedPrimaryJudgement(card, actor, excludedUnit)` delegate, made under the standing modifications plus the attacked-unit `ActionTargetExclusion` the borrowed instance will resolve under; the turn binds it into `CardActionResolutionAuthority` (`BindBorrowedPrimaryJudgement`) for the two Attack card-text contexts only - the seam no offer walk reaches, where the unit the Attack took is known - so a Skill's own borrow and an Ultimate's (Dragon Knight) keep their live offer. Regression on Widget's real faces: `WidgetSkillReuseTests.FightAsOne_DoesNotOfferFieryBreath_WhenTheOnlyEnemyInRangeAndInPyrosLineIsTheAttackedHero` failed on the base ("Expected: not some item equal to breath But was: < breath >") and passes after; its positive control (a second enemy in Range and in Pyro's line: the Breath offered, only that enemy targetable) passes on both. Two existing cases that relied on the old offer (the inert Breath with Pyro off the board; the Breath whose only other hero is Skill-immune) and the Play Mode exclusion journey now assert the Skill is not offered, the journey declining the may-step and reading the enemy's unchanged hand. No rule, snapshot or wire change; the fizzle notice stays the presentation of a genuine mid-resolution stop.

Completed 2026-10-02.

Historical evidence: [9.38 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-938-borrowed-skill-offer-evidence).

## Ruling W10 and the control-label directive, authorized 2026-10-03

Authorized 2026-10-03 by the user, relayed verbatim by the "Agent work status review" session as a bounded two-milestone sequence **9.39 -> 9.40**, executed one at a time with separate validation and commits; nothing else is authorized by it. Decision A answers OQ-9.13 (recorded in the 9.13 result above): "A decline ends it. 'Up to 4' is one decision about how many. Once you say no, you are done. Fewer prompts, and it matches how a tabletop player would do it, placing the tokens they want and stopping." - recorded as ruling **W10** in [PRINTED_WORDING](../Rules/PRINTED_WORDING.md). Decision B is a standing presentation directive: "never have option X label on the button, always write what the button does."

### [x] 9.39 Declining one pass of an "up to N" repeat ends the remaining passes

Outcome: wherever a printed "up to N" (placements or "choose up to N times") composes as N sibling passes asked one at a time, a player's decline of one pass ends the sequence - no further pass is offered and everything already placed or chosen stands. A pass with no legal subject already stops without asking (8.35z15) and keeps doing so; "may repeat up to N times" (W2, a `RepeatAction`) already stops on its decline and is unchanged. The behaviour belongs to the composition, chosen once and never per card: the pass Sequence declares it, and the resolver reads the declaration on a player's decline. Affected faces are enumerated from the content, not by name branching. Closing check: for each affected face family a regression that takes fewer passes than printed, declines, and asserts no further prompt with the earlier work standing - failing on the base and passing after; the six parked reader cases the 9.13 result names executed through `ExpectedOutcomeExecutor` with their dispositions recorded; the W2 repeat fixtures green unchanged; affected hero fixtures and journeys green; no snapshot schema or wire change (a schema need stops the milestone). T2.

Result: one declaration, one reading. `EffectNodeDefinition` lets a Sequence carry `EffectFailurePolicy.UpTo` (it was mandatory by shape; `may` and `if able` still belong to its children): its children are the passes of one printed "up to N". `EffectResolutionSession.Resume`'s Decline arm, after popping the declined node, pops an enclosing `UpTo` Sequence too and records it applied (`CloseDeclinedUpToCount`) - a player's Decline alone, never the resolver's own skip of a pass with nothing to offer (8.29e, 8.35z15, the short-supply step standing aside), and only the declined pass's immediate Sequence, so a decline deeper inside a pass (Morbid Mosh's "that Zombie token may push") finishes that pass and still asks the next. Thirteen faces across six heroes declare it, each where its passes are ordered: Mortimer's Awaken! (Skill and Defense bodies) and the modal Sequences of his seven "choose up to N times" faces (`MortimerRepeatedChoice.CountPolicy`, with Master of Puppets' raised passes under a second `UpTo` Sequence behind the gate); Wuk's Mystic Saplings and Treetop Ride; Ignatia's three moon scatters (Path of Ashes, Cinders, Flames); Tali's Wall of Frost and Pack Ice; Trinkets' Deployable Barrier and Bastion (`placements`); Rowenna's Grand Melee (`choices`). Not touched, because they already stopped: Swift's and Brynn's Decoy passes (nested at their dealings), every W2 `RepeatAction` (Blood Frenzy, Center of Mass and their siblings), and Find Familiar's "remove up to three" (one multi-select). Six content revisions moved to `-9-39-rev-1` with their artwork rows and measured identity pins (every other pin byte-identical). Regressions failing on the base `9bf6f627` and passing after: `Awaken_UpToFour_StopsWhereMortimerDeclines`, `Awaken_TwoOfFourThenADecline_EndsTheCountWithBothStanding`, `Awaken_ZeroPlaced_IsALegalPlay`, `Awaken_AnEmptyPileAsksWhichStandingZombieComesBack`, `DiscardedToDefend_AwakensTextPlacesZombies`, `MysticSaplings_UpToThree_StopsWhereWukDeclines`, `MysticSaplings_AnEmptyPileAsksWhichStandingTreeComesBack`, `Treetop_DecliningTheFirstPass_AsksNoSecond`, `GrandMelee_DecliningTheFirstPerformance_AsksNoSecond`, `DecliningTheSecondBarrier_AsksNoThird_AndTheFirstStands`, `TheScatter_OneOfThreeThenADecline_AsksNoThird`, `AnEmptyPileAsksWhichStandingTokenReturnsBeforeTheNextPlacement`, `TheMoonFaceIsOneOptionalPlacementPerPrintedToken`, `PackIce_OneOfThreeThenADecline_AsksNoThird_AndTheFirstStands`; the modal cases (`UpToThree_DecliningTheSecondPass_AsksNoThird`, `UpToThree_WithMasterOfPuppetsUnlocked_DecliningTheThird_SkipsTheRaisedPasses`, `UpToThree_WithMasterOfPuppetsUnlocked_MayKeepThree`) name `CountPolicy` and could not compile on the base - Treetop Ride and Grand Melee prove the same `Optional`-pass shape there; `MorbidMosh_DecliningThePushInsideAPass_StillAsksTheNextPass` and `Awaken_TheShortSupplyStepStandsAsideWhileThePileHoldsAZombie` hold on both as the boundary. The six parked reader cases now execute: five run with every executable fact passing under W10 and the reader's own setup note recorded (`not-executable`, `execute: true`), and `wuk-mystic-saplings-short-supply-takes-from-board` is `execute: false` because its table stages one Tree of three, so the pile is never empty (the behaviour is proved by `MysticSaplings_AnEmptyPileAsksWhichStandingTreeComesBack`). Two existing cases read the old pass-by-pass asking and were re-read under W10 (`Knife_WithMasterOfPuppets_FourRemovalsOnFivePassesAddFour`, `MarchOfNature_ABasicCardResolvingFiresItToo`), as was the Ignatia Progressed journey's reclaim loop. No snapshot schema, wire or rule-event change: the continuation stack is the same frames, popped one deeper.

Completed 2026-10-03.

Historical evidence: [9.39 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-939-declined-up-to-pass-evidence).

### [x] 9.40 Every choice control says what it does; no ordinal labels

Outcome: (1) every registered card's choice nodes - modal "Choose one" arms and the optional steps that open p.10's confirm-or-decline - across all thirty heroes carry a `DecisionLabel` in the card's own printed words describing what the press performs (e.g. "PLACE PYRO", "MOVE BOTH UP TO 2"), never "OPTION #1"; (2) the ordinal fallback is unreachable for registered content: a guard walks every registered card's effect definitions and asserts every presentable choice node resolves a non-ordinal label, failing on the base for each gap and passing after; (3) the HUD's generic prompt titles ("CHOICE", "CHOOSE ONE") stay only as window titles above named controls, never as a control label. Direction controls, board-object targets and hero/minion/card names already satisfy the rule and stay. No rule, command, snapshot or wire change: labels are presentation data in the catalog, never something a rule branches on. Closing check: the guard green over the full roster; `DecisionWindowPresenterTests` and the HUD presentation fixtures green; one Play Mode journey per newly labelled window kind reading the label; a before/after capture of one previously ordinal-labelled window at 2560x1440 for the user. T2.

Result: the gap was measured before it was closed. `DecisionLabelCoverageTests` rostered twenty-three heroes; rostering all thirty made `EveryDecisionControlInDealtContent_NamesItsConsequence` fail on the base with 215 unlabelled controls - Mortimer 155 (every pass of his sixteen modal faces: the "up to" step, the two bullets, Corpse Slam's line of "that Zombie token may push"), Wuk 22 and Razzle 6 (tables written at their dealings and never joined to the catalog), Trinkets 15, Snorri 11, Mrak 6; Takahide raises no such control (asserted at his dealing) and Gydion's spells were already rostered. Four tables were authored in the cards' own words (`MortimerCardContent.Labels`, generated per pass so Master of Puppets' raised passes are covered; `TrinketsEffectContent.Labels`; `SnorriCardContent.Labels`; `MrakCardContent.Labels`) and six joined to `DecisionLabelCatalog.Declared()`; the guard is green over the full roster and the declared set equals the derived set. No rule, command, snapshot, digest or wire change: the catalog sits beside the definitions (every identity pin byte-identical). "CHOICE", "CHOOSE ONE" and the other generic words were confirmed to be window titles and section eyebrows (`EffectChoicePromptLabels.SectionLabel`, the window's title fallback), never a control's label, and `DecisionWindowPresenter.PrintedBulletLabel` is now unreachable for dealt content. Before/after, the same Play Mode journey (`MortimerUiJourneyPlayModeTests.SlamWindows_SayWhatTheirControlsDo_AndTheModalIsPhotographed`) run with the tables held back and then joined: Corpse Slam's modal read "OPTION 1" / "OPTION 2" and reads "MOVE A ZOMBIE TOKEN IN RANGE UP TO 1 SPACE" / "MOVE 1 SPACE"; Morbid Mosh's second pass read "DO IT" / "STOP HERE" and reads "MOVE A ZOMBIE TOKEN OR MOVE 1 SPACE AGAIN" / "STOP HERE"; the captures are `tmp/9-40-captures/corpse-slam-modal-before-2560x1440.png` and `-after-`. The new-hero checklist gains step 8 (join the table and roster the hero) and [PRESENTATION](../Project/PRESENTATION.md) records the directive.

Completed 2026-10-03.

Historical evidence: [9.40 completion index](../Archive/Phase-Handoffs/PHASE_9.md#milestone-940-choice-control-labels-evidence).
