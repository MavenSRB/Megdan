# Phase 8 - Completed milestone definitions, fifth shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-06 during milestone 8.35z23's required warning-threshold ownership review, and it is the first review since the fourth shard opened at which **no existing completed shard could absorb a move at all**. The active shard stood at 12,360 words against its own 12,750 warning line, with two milestone definitions due in a single integration pass; `PHASE_8_COMPLETED_3.md` sat at 12,216 and `PHASE_8_COMPLETED_4.md` at 12,262, each within roughly 500 words of the same line, and `PHASE_8_COMPLETED.md` and `PHASE_8_COMPLETED_2.md` have been within a few hundred words of theirs since 8.35j's review. Cascading into any of them would have made this review the next one. Milestones **8.35z8** through **8.35z12** moved here verbatim, the oldest definitions the active shard still held in full and one contiguous run: the shared treatment every named control now carries with its did-nothing report (8.35z8), the awaited shine sweep reaching its hosts (8.35z9), the match-log z-order and the HUD geometry corrected around it (8.35z10), the motion language the whole match reads against (8.35z11), and the attack spotlight built on that one conductor (8.35z12). The run is kept whole deliberately: 8.35z12 composes 8.35z11's conductor and reads it by id, and the wave-one integration that landed them reconciled the two against each other, so splitting them across shards would leave each half pointing at the other. The move is sized for both definitions this integration lands rather than for one, because the previous review sized for two, came up 86 words short, and had to be carried twice. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive. Milestones **8.35z13** through **8.35z18** followed on 2026-09-06 during milestone 8.35z25's required ownership review, the same contiguous run carried one step further: the greedy-legal seat drivers that made the practice bots fight (8.35z13), the named blocker in a mid-resolution fizzle that took the wire to `/8` (8.35z14), the card-text repeat withheld when it has no legal subject (8.35z15), rulebook pp.5-6's printed 2v2 setup and both game lengths that took the wire to `/9` (8.35z16), the Director sandbox (8.35z17), and the launch flag that reaches it (8.35z18). That review was **proactive**: the active shard sat at 11,775 words with three milestone definitions due in one integration pass, and this move was sized for all three rather than for the one that triggered it.
### [x] 8.35z8 Give every named control one shared treatment, and say when a step applied nothing

User-directed, the EIGHTEENTH amendment: "make these buttons nicer... they need clicking animations, they need to sit nicer in the trays... they look pasted over".

Observable outcome: every named control on screen - in the match and in the lobby - shares one chrome and one press-and-hover motion, and a resolution step that applied nothing says so and waits for an acknowledgement instead of passing in silence.

Design: `MatchHudTheme` gains the button chrome constants and `Lighten`/`Darken`; `HudElements.ApplyButtonMotion` wires the pointer events to a USS `scale` transition - idle 1, hover 1.035 over ~120 ms, pressed 0.94 over ~80 ms - gated on `enabledInHierarchy` so a disabled control never animates, with the background tint eased toward the control's OWN colour captured lazily on first hover, so a caller that recolours a button after construction still eases toward its own colour. `HudElements.Control` carries it to eleven existing call sites at once; `PewterBanner` and the card-faced buttons take scale-only motion layered over their established look rather than a chrome change. `PrivateLobbyViews`' HOST/JOIN/FILL/LEAVE and the seat PICK button drew NOTHING but Unity's stock button skin before this - no themed background, no border - which is very likely the largest single contributor to "pasted over".

The fizzle notice is the SIXTH amendment's deferred part b. Verified before designing rather than assumed: `EffectExecutionResult.ImpossibleMandatory()` is a parameterless factory, the two sites producing it record only a node id and bare kind, and milestone 8.35l's blocking-source channel is threaded through the OFFER-time feasibility walk alone. So acting-player scoping IS derivable - a seat is one hero, and the log's `SubjectHeroId` states whose resolution stopped - while the blocking hero and card are NOT derivable from any fact presentation can read today. That gap is RECORDED as open Core work rather than filled with an invented reason. `FizzleNoticeStream` mirrors `KineticCueStream`'s three rules (emit once, prime silently on first sight, re-prime on a rewind) with a latch that persists until acknowledged, and the notice draws as an overlay on the contextual region with an OK button built through the shared `Control`.

Acceptance checks:

- Every named control kind carries the shared chrome and motion - END TURN, the seat UNDO, an agreement choice, a Defense option, the Director's UNDO/RESET/EVENT LOG, and the lobby's HOST/JOIN and seat PICK - while a disabled control still never animates.
- A new `BlockedAction` row naming this viewer's hero latches one notice; it survives repeated observation until acknowledged; a rewind drops it silently; and an undefined viewer is the Director, who owns every hero's fizzle - the same reading `OwesACommitment`'s own undefined-viewer branch gives.
- The notice names the card and, honestly, not the blocker.

Non-goals: no rules, schema or wire change; the printed-action-region hotspot and `ReplacementRibbon` keep their own visual identity; naming the blocker is deferred to Core work this block records rather than performs.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z8-give-every-named-control-one-shared-treatment-and-say-when-a-step-applied-nothing-completion-evidence).

### [x] 8.35z9 Make the awaited shine sweep reach its hosts, and keep gold for what is genuinely awaited

Three findings from the user's own live testing of the 8.35z4-fix package, taken together because they are one question - what the HUD's gold and kinetic language says, and to whom.

Observable outcome: the slow circling light actually travels the gold on a lit tray and a shining card instead of appearing nowhere; a player nobody is waiting on wears no gold at all, while the hero whose turn is under way still does; and the floating damage and block figures are gone from the board entirely.

Design:

- **The sweep found nothing, and had not since it shipped.** `CollectSweepHosts` asked for `Image` elements NAMED `gd-card-shine` - milestone 8.30d's prepared printed-artwork overlay, a different family altogether and unrelated to awaitedness. The awaited gold is a `VisualElement` named `gd-card-awaited-shine` on a card, and on a hero rail it is not an element at all: the rail's own border and background are painted. Wrong name AND wrong type, so the query matched neither, returned empty, and the caller returned early - silently, because an empty host set is legitimate on a HUD with no shine. Fixed with ONE shared marker, `MatchHudTheme.AwaitedShineClass`; a class rather than a name, because these are different element types on different surfaces and a class is the one thing all of them can carry. The sweep's own doc comment, which claimed it travelled the shine of heroes being waited on, becomes true for the first time.
- **The gold leaked.** During another hero's release pause the viewer's own committed card still wore the gold that means act now. Diagnosed rather than assumed: not legacy styling but the predicate. `isResolvingThisCard` is true from a card's REVEAL until it finishes resolving, and every hero reveals at the start of the round while turns run one at a time, so a card claimed the gold through as many as five other heroes' turns. It now additionally requires the turn to be under way for this hero - `IsTurnActive` and `SelectedActor` - which is the same pair the tray-light arm uses, so tray and card now agree by construction. Every other gold treatment was audited and left alone as already correctly gated.
- **The floating numbers are retracted**, on the user's own call after live testing: the figure cues, their printed-colour tinting and their impact timing are removed, keeping the lunge, flash, defeat fade, pulse and sweep. This deliberately retracts part of milestone 8.35z4, whose fourth defect was those figures appearing before their own cause; the timing fix was correct and is not being reverted as wrong - the feature above it is withdrawn. A replacement attack presentation is the user's own separate future design.

Acceptance checks:

- A composed HUD showing an awaited card AND a lit tray yields a NON-EMPTY sweep host set, containing one host from each surface and staying inside the recorded concurrent bound - the guard whose absence let an empty list ship four times.
- A hero the match is not waiting on has no awaited answer on any tray card AND nothing on that hero's whole surface carries the shared marker, while the acting hero's card still shines - which is what says the grammar was narrowed rather than removed.
- Every lunge, flash, slide, defeat, respawn, hitch, curve and sweep case stays green; only the five figure cases and the fade curve they alone used are removed.

Non-goals: no Core, Application or wire change; no new animation; the pewter banner's own resting glow and the hover states are that control's established identity and are not the awaited language.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z9-make-the-awaited-shine-sweep-reach-its-hosts-and-keep-gold-for-what-is-genuinely-awaited-completion-evidence).

### [x] 8.35z10 Fix the match-log z-order, retire the Tie Breaker disc for a rail banner, centre the narration bar, correct the Fast Travel ribbon's anchor, and stamp the build identity

User-directed presentation batch from 8.35z9 live testing (2026-09-05), plus a coordinator-directed small QoL item folded in the same session.

Observable outcome: the match-log drop-down always paints above both hero rail columns; the narration bar stands centred at the strip's own top, keeping its gold treatment, typography and log chevron, with the drop-down unrolling centred beneath it; the Tie Breaker disc is retired and its fact reads as a slim team-coloured `TIEBREAKER` banner above the HOLDING team's own rail column, easing its position between the two columns on handover; the Secondary Fast Travel ribbon touches the printed boot/value ink it names instead of floating over the artwork beside a padded highlight ring; and the running build's own identity stamp is visible in the OS window title and a corner of the lobby screen.

Design:

- **Z-order.** `ConfiguredMatchHudRenderer.Build()` added the match-log drop-down's panel to `root` BEFORE the hero rail column, which is UI Toolkit's paint order - so an open drop-down whose bounds reached a rail painted entirely behind it, clipping its entries (the user's screenshot). Moved to after the rail `root.Add`, unchanged geometry.
- **Top-strip redesign.** `CalculateNarrationBarRect` replaces the retired full-width narration lane: a centred, width-capped bar (`NarrationBarPreferredWidth` 900, bounded against `CalculateHeroRailWidth` so it is clear of both rails by construction at any viewport). `MatchStatusBarUnityView`'s `narrationLane` is now absolutely positioned and carries its own bordered chrome mirroring the strip's two gold states, rather than a lane painted by `Root` showing through. `GetMatchLogDropdownRect` centres under this bar instead of left-aligning under the whole strip. `CalculateTieBreakerRailBannerRect` states the slim banner's rectangle above either side's own rail column; `CalculateHeroRailRect` reserves `TieBreakerRailBannerHeight` (28) plus a margin on both columns always, whichever currently holds it, so a handover moves no column's height. The one live `gd-tie-breaker-rail-banner` element eases its own `left` between the two targets on a USS transition (350ms, `EaseInOutSine`, inside the TWENTIETH amendment's 200-450ms band) rather than jumping; `CalculateTieBreakerIndicatorRect`/`TieBreakerDiameter`/the disc are removed outright.
- **Card-face overlay alignment.** Measured against the verified 372x520 scans with a System.Drawing probe (`Assets/Resources/Private/Cards/arien/liquid-leap.png`'s Movement badge): the Secondary Fast Travel ribbon anchored to `PreparedActionSourceBounds`' padded 80x82 highlight ring, ~15-20 reference units clear of the actual printed boot/value ink the ring's own comment already names (`x=21..69, y=203..249`). `BuildReplacementChip` now anchors that one branch (Movement, Secondary, prepared artwork) to the tighter ink rect directly; the Primary title-tab placements (CLEAR above Attack, FAST TRAVEL above a Primary Movement) were measured against the same scans and found already correct within a few reference units, so neither moved. The ring/glow geometry itself is untouched.
- **Build identity stamp.** Coordinator-directed (PLAN.md's "SMALL QoL ITEM"): `BuildIdentityStamp.Current` is the one formula `Application.version + "+" + Application.unityVersion` now lives in - `ConfiguredMatchSceneRuntime`'s admission-tuple `BuildIdentity` reads it rather than carrying its own copy. A `[RuntimeInitializeOnLoadMethod]` stamps the OS window title on Windows standalone players (a defensive best-effort P/Invoke to `user32.dll`, a no-op in the Editor and on any other platform); the lobby screen carries a quiet corner watermark of the same string.

Acceptance checks:

- A structural regression proves the drop-down panel is added to `root` AFTER the hero rail column, so it paints above it regardless of geometry.
- Offline geometry sweep at the reference panel and at least one narrow/short viewport: the narration bar and both rail banner slots stay inside the viewport, clear of both rails and of End Turn, and each rail banner shares its own rail's exact x/width; corroborated live in Play Mode at the reference and the existing short (2560x540) viewport.
- The Fast Travel ribbon's Secondary anchor is proven against the printed ink rect, not the caller's padded bounds, with a regression guarding against silently falling back to the old anchor; the Primary CLEAR/FAST TRAVEL placements are unchanged.
- The lobby screen renders the build-identity line, and it equals `BuildIdentityStamp.Current`.
- Complete unfiltered Edit Mode and the affected Play Mode HUD fixtures pass at the closing revision.

Non-goals: no Core, Application or wire change; no rules change; the commitment-phase hand-shine rule (pending the user's "picking" vs "after" verdict) and the everything-build package rebuild are explicitly out of scope.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z10-fix-the-match-log-z-order-retire-the-tie-breaker-disc-for-a-rail-banner-centre-the-narration-bar-correct-the-fast-travel-ribbons-anchor-and-stamp-the-build-identity-completion-evidence).

### [x] 8.35z11 Give the whole match a motion language: a conductor, an easing layer, and card flight

PLAN.md's TWENTIETH AMENDMENT (user, 2026-09-05), on milestone 8.35y's cue engine. First of the four wave-one integration milestones.

Observable outcome: nothing on the surface teleports. A committed card arcs from the hand to its facedown slot, a reveal flips it, a retrieved card flies back at round end, coins fly to the tray they join, and a Level pops on the rail; events play one MOMENT at a time with a deliberate beat rather than as simultaneous soup; commitment reveals are staged by the revealed card's printed Initiative; and every panel eases into existence instead of popping.

Design:

- **Two derivations, and the finding that forced the second.** A facedown commitment is excluded from the seat-public match log BY CONTRACT - `MatchLogEntry` names it in its own exclusion list, and that exclusion is why every seat's log is the same log - so principle 1's first example had no source in the stream 8.35y derives from. `KineticProjectionWatch` derives the arc, the coins and the round-end retrieval from the PROJECTION, where the fact legitimately lives, and `KineticCue` REFUSES to carry a card identity for a commit: the privacy rule as a constructor invariant rather than a convention.
- **The conductor.** `KineticScene` is one moment, not one cue - an attack's lunge and its flash play together; six reveals are six moments and are staged. `KineticConductor` holds no session and no projection, asserted by reflection, so it cannot delay a command, a prompt or a frame of the board. Queueing means playing a cue against a later projection than it was derived from, which is harmless for every kind but a SLIDE; `KineticRouteFreshness` is that check, and a stale slide travels nothing rather than drawing a piece on a path it never took.
- **Staged in initiative order, not in emission order.** `TurnCardCommitmentAuthority` walks `expectedHeroIds` and `RoundTurnSlot` sorts that list by identifier, so the log's own reveal order is an ordinal sort by hero id - stable and meaningless to watch. Reveals are re-ranked by the revealed card's printed Initiative, highest first, ties by arrival. It decides nothing: the authority reads a total item bonuses can raise.
- **The easing layer and the bridge.** Every curve and duration sits in one enumerable place with an audit that samples each curve against the identity line rather than reading source for the word "linear". `KineticSpaceBridge` converts between panel points and board world units through the same mapping every board click passes through; coins are won on the board and land in a tray, which is the journey that genuinely crosses the two.

Acceptance checks:

- Each of the five new cue kinds is derived from its named source, and a commit cue carrying a card identity is refused by construction.
- The conductor plays scenes in order with a beat, compresses under backpressure, drops the OLDEST past its queue bound, and holds nothing authoritative (asserted structurally, by reflection).
- A reveal batch is re-ranked by printed Initiative and stays inside the span it already occupied; a card whose Initiative cannot be read sorts last rather than being guessed.
- The easing audit proves every shipped curve departs from the identity line, rests at both ends, and sits in its declared duration band; the interaction band agrees with 8.35x's shipped button constants.
- A composed HUD yields a resolvable, laid-out flight anchor set for every hero - the 8.35z9 zero-hosts failure mode, guarded in Play Mode.
- Complete unfiltered Edit Mode and Play Mode pass at the wave's closing revision, and the Phase 4 p95 frame gate is unmoved.

Non-goals: no Core, Application or wire change; no schema move; exit easing, idle "breathing" and a per-hex route stay out of scope and are recorded as named gaps.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z11-give-the-whole-match-a-motion-language-a-conductor-an-easing-layer-and-card-flight-completion-evidence).

### [x] 8.35z12 Frame every attack in a spotlight, ranged pairs included, under one conductor

PLAN.md's NINETEENTH AMENDMENT (user, 2026-09-05). Second of the four wave-one integration milestones, and the one that reconciles the two conductors this wave brought in.

Observable outcome: a resolved attack loses the HUD, zooms and tilts to the two combatants, stands both cards beside their own pieces, plays the smack framed with both faces up, and waits on a CONTINUE the viewer presses. The match never waits for it: six clients watch their own story at their own pace over one authoritative board.

Design:

- **The zoom is clamped BOTH ways, and the ceiling's derivation is the part worth reading.** The first version's only ceiling was the resting board frame, so a cross-board ranged attack computed a wider frame, took the board's, and opened the shot out until it WAS the board view - with the HUD hidden and nothing gained. The obvious bound is the board (32.909 units between the official map's farthest playable spaces) and it is the WRONG one, because the spotlight frames an ATTACK and an attack's separation is bounded by the attacker's printed Range. The largest printed Range in the six-hero corpus is 4, so the framed maximum is eight hexes - that printed maximum with its dynamic headroom doubled - and a wider pair crops rather than widening.
- **The tilt tapers**, sixteen degrees at melee to the amendment's ten-degree floor at the framed maximum, because the tilt reveals `depth * sin t` of a piece's side - which reads when the piece fills the frame and is invisible against eight hexes of board, while costing framing the whole time since the foreshortening divides the solve by `cos t`.
- **Token depth is free under this camera.** Raising a piece's face under an ORTHOGRAPHIC top-down camera moves it zero pixels and the wall beneath projects to a line of zero width, so the extrusion is invisible from the board view and appears only when a camera tilts. That is why it is permanent rather than switched on, and why it cannot move the Phase 4 band.
- **Each card stands beside its own piece**, on the side away from the other, flipping inward when the outer side would leave the panel - with on-screen, mutually disjoint, and clear of BOTH pieces enforced in that order rather than hoped for.
- **ONE CONDUCTOR.** This milestone's `IConductedScene` and 8.35z11's `KineticConductor` are the same seam designed twice. 8.35z11's survives; this milestone's stage becomes a scene it grants, and the hold valve is reconciled by a RULE rather than a special case - the valve is short when the chrome is hidden, because a paused board is merely slow while a hidden HUD is a client the player cannot use.

Acceptance checks:

- The framing zooms in and never out, fits a pair at the framed maximum whole on either axis, and crops rather than widening beyond it; a test reads the largest printed Range out of the verified content so a future hero printing a longer one fails here instead of silently cropping a legal shot.
- Both card faces are captured into the scene at build time and survive the cards moving on, since the continue gates the viewer and not the match.
- A ranged attack draws a jab that shortens with distance, a bounded-speed tracer and an impact ring, on the HELD phase where the deferred cues are released - and the measured SEPARATION drives the treatment, not the rules category.
- The registered conducted scene is granted the stage by 8.35z11's conductor and nothing starts behind one that holds it; the chrome-hidden hold valve is one number on both sides of the reconciliation.
- One spotlight plays end to end on the real composed HUD - chrome hidden, camera driven, CONTINUE armed, board and panels restored - proven in Play Mode.
- Complete unfiltered Edit Mode and Play Mode pass at the wave's closing revision, and the Phase 4 p95 frame gate is unmoved.

Non-goals: no Core, Application or wire change; minion pieces gain no depth (the amendment names the flat token discs); a ranged Play Mode journey needs a staged ranged scene arm that does not exist and stays owed.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z12-frame-every-attack-in-a-spotlight-ranged-pairs-included-under-one-conductor-completion-evidence).
### [x] 8.35z13 Make the practice bots fight: greedy-legal seat drivers instead of random-legal

Third of the four wave-one integration milestones, on milestone 8.35q's spawned test drivers.

Observable outcome: a bot-filled seat attacks when an Attack is offered, prefers the target it is closest to killing, walks toward the fight rather than at random, and spends a Defense card when declining would kill it - so a solo playtest shows a match that progresses instead of six seats milling about.

Design: three deterministic preferences layer over the random picker in `PrivateSeatDriver`, each reading only facts already published on the seat's own `IMatchReadModel` and each falling back to random-legal the instant its own facts are absent. Preferring to START an Attack is safe unconditionally because `TurnResolutionAuthority.CanStartAttack` already refuses to offer one with no legal target. Movement prefers the lowest hex distance to the nearest enemy hero or minion, minion position standing in for the lane battle because the live Battle Zone id is Director-only board state a seat never receives. Every preference is a filter computed BEFORE the random tiebreak, so per-seed determinism is preserved.

**A finding recorded as content, not as a rule (user, design-side).** In the current content a Decline is unconditionally `AttackSuccessful`. That is CONTENT-DEPENDENT and must never be promoted to an invariant: unimplemented heroes may carry decline-triggered saves, and removal prevention already exists today as a post-hit save. No code change was owed because the bot reads the per-state preview rather than baking the assumption in, and that guard is the load-bearing line.

Acceptance checks:

- Each preference has a positive case and a fallback case, and a same-seed-twice case proves seeded determinism survives.
- Commitments, leveling picks and team-agreement convergence stay random/untouched, as durable non-goals rather than oversights.
- A measured offline comparison records the change in attackable turns converted into an Attack.
- The SixClient Solo rehearsal completes with bot-filled seats at the wave's closing revision.

Non-goals: no search, no evaluation function, no difficulty setting, no rules or wire change; commitment quality stays out of scope by design.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z13-make-the-practice-bots-fight-greedy-legal-seat-drivers-instead-of-random-legal-completion-evidence).

### [x] 8.35z14 Name the blocker in a mid-resolution fizzle, and advance the wire to /8

The SIXTH amendment's remaining known-problems entry. Last of the four wave-one integration milestones.

Observable outcome: when a mandatory effect step cannot be performed and the action fizzles mid-resolution, the notice names WHO blocked it and with WHAT - the same "blocked by HERO's CARD" vocabulary milestone 8.35l's offer-time tooltip already uses - instead of naming only the acting hero and card.

Design: `EffectOptionFeasibilityQuery`'s two-step refusal question (the refusing node's own explaining handler, else the consumer that narrowed a selection to nothing) is extracted and reused by `EffectResolutionSession` at both sites that produce `ImpossibleMandatory`. The answer rides the existing `EffectResolutionEvent` as a new `Blocking` property and deliberately NOT `EffectNodeOutcome`/`PendingResolution`: that record persists into a snapshot only while a resolution is genuinely paused, and `ImpossibleMandatory` always completes the whole resolution - so **no snapshot payload and no schema is touched, and the schema stays v36**. The blocking source is always a standing PUBLIC Active effect (rulebook p.17), so naming it reveals nothing a viewer's projection does not already carry. `NetworkMatchLogRow` gained two optional fields appended after `amount`, which is a wire-format change: `guards-private-online` 7 -> 8, riding the existing BlockedAction row rather than a new message kind, on the same reasoning 8.35r and 8.35z3 record.

Acceptance checks:

- The engine seam attaches the blocker exactly where the refusing party could explain itself and nowhere else, proven against a handler that refuses-and-explains, one that refuses silently, and one that applies.
- The log projection carries both halves only when defined, and the notice falls back to its generic line wherever either half is absent - never a guessed blocker.
- The wire round-trips a row with and without a blocker, bare and through the full seat-projection frame, and the seeded six-seat walk's frame-size guard still holds with the new fields present.
- Both hard-coded protocol-version pins read 8 and an older peer is refused before payload interpretation.
- A real-fixture pair proves the composed notice prints the blocking hero and card for a currently-standing restriction, and the plain line for the unpinned control.
- Complete unfiltered Edit Mode and Play Mode pass at the wave's closing revision.

Non-goals: no schema move, no new message kind, no change to the match log's seat-filter identity property, and no attempt to re-open the offer-time window milestone 8.35l correctly closes ahead of this stop.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z14-name-the-blocker-in-a-mid-resolution-fizzle-and-advance-the-wire-to-8-completion-evidence).

### [x] 8.35z15 Stop offering a card-text repeat that has no legal subject

From a live user report: Xargatha resolved Cleave - "Target a unit adjacent to you. After the attack: May repeat once on a different enemy hero" - against the only enemy hero adjacent to her, who defended, and rulebook p.10's confirm-or-decline opened anyway for a repeat whose subject set was empty. The fourth instance of the class milestones 8.29d, 8.29e and 8.35z5 closed elsewhere.

Observable outcome: a repeat printed inside rulebook p.14 whose second instance has nobody legal to take is not offered at all, and one that has a legal subject is offered exactly as before.

Design: the judgement already existed and was already recorded. Milestone 8.25e extracted `AttackDefenseCardActionStrategyBackend.CanStartRepeatedAttack` and `Rules/RULE_SYSTEMS.md` row 48 has since stated the ruling for every path - "whether the second instance can start is asked of the board as it then stands, under the target binding the asking node declares, rather than of the action that ended". Only the after-action-ability path asked it; the card-text path supplied no `repeatCanBePerformed` probe at either of its two `EffectResolutionContext` sites, so `RepeatIsUnavailableFor` fell through and the `May` node paused unconditionally. Both sites now supply one. The exclusion the offer is judged against is extracted as `CardTextRepeatExclusionFor`, so the offer and the answer read one statement. **No schema move, no wire move, no content revision**: the probe is a question asked of existing state.

The gate is whether the enclosing Attack's target is known, deliberately not which of p.14's moments the text prints at. Arien's Violent Torrent prints its repeat at "Before the attack:" and a moment-shaped gate excused it - but driven, its node is reached with the target already chosen, and it opened the offer, took the confirmation and started nothing. Judging it also required telling `PlanRepeatedAttack` that the Attack occupying `pendingAttackResolution` is the subject of the question rather than an obstacle to it; without that, every Violent Torrent repeat would have been suppressed instead.

Suppression rather than milestone 8.35s's grey-with-reason vocabulary, because that grammar attaches a cause to an `ActionOffer` a player can hover, and a repeat node publishes none - its only states are a pause that stands and a pause that does not. Drawing a greyed ACCEPT nobody can answer is the "ask, then do nothing" outcome the node's own rule has forbidden since milestone 8.23.

Acceptance checks:

- Every `RepeatAction` node the dealt roster's action definitions print is named by one content-derived table, classified by the kind of action it repeats, so a seventh hero's repeat arrives as a new row rather than as a new unjudged prompt.
- Every repeat in that table that repeats an Attack has an empty-subject case driven on a real board - all four of them, each failing at the pre-fix revision.
- The reported shape is reproduced exactly: one adjacent enemy hero, targeted, successfully defending, no confirm opened, and the action reaching its own natural end with the card resolved.
- A defended Attack whose repeat still has a legal subject is offered unchanged, so the outcome is never what the suppression keys on.
- Complete unfiltered Edit Mode and Play Mode pass.

Non-goals: no schema move, no wire move, no content revision, no change to what a repeat that legitimately starts may then do, and no attempt to decide whether an instance that starts will finish - a board that changes between the offer and the instance stays rulebook p.10's own residual, as `CanStartRepeatedAttack` already documents.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z15-stop-offering-a-card-text-repeat-that-has-no-legal-subject-completion-evidence).

### [x] 8.35z16 Add rulebook pp.5-6's printed 2v2 setup and both game lengths, and advance the wire to /9

The ELEVENTH amendment's first half. The user asked to choose the format when hosting; 1v1 was dropped the same day, leaving the two printed setups and the two game lengths.

Observable outcome: a build launched with `-guardsMatchFormat 2v2-quick|2v2-long|3v3-quick|3v3-long` composes exactly that printed setup, and a peer running a different one is refused by name instead of being admitted and desynchronised.

Design: the printed reading came first. p.5's setup chapter is titled "GAME SETUP (4 or 6 players)" and p.20 covers what falls outside "the recommended 4 or 6 player count described on pages 5-6", so a 2v2 is one of the two printed setups rather than a variant. Step 4 makes the Wave count a function of the game length alone (Quick 3, Long 5); step 5's table crosses length with count for Life counters (4 players 4/6, 6 players 5/8); step 7 names the four-player dashboards and builds the six-player line-up by adding a third a side. `MatchFormat` is that menu as four rows, with the Wave count derived from the length and the team shape from the count so the stored numbers cannot disagree with the derived ones; `Default` is `3v3-quick`, so every existing caller composes exactly the match it did before.

The format is its own element of the admission tuple rather than folded into the content digest, and the reason is measured: a 2v2 and a 3v3 register different printed sets so their digests already differed, but Quick and Long of the same count change no card, capability, token or marker at all - their digests were byte-identical and the two were admitted to one another's sessions before this change. Folding the length in would have falsified the digest's own stated contract, which stays true of a Quick/Long pair, and `RISK-COMPAT-001` refuses a second coarser content identity. The element is DERIVED from the composed setup rather than passed beside it, because a caller that composed one setup and declared another is exactly the failure it exists to catch. The Hello payload therefore carries one more string: `guards-private-online/8` -> `/9`. **Schema v36 does not move**, measured rather than assumed - a 2v2 payload is a v36 payload of different values, not of a different shape.

Acceptance checks:

- The printed table is transcribed as checks: four rows, both Wave counts, all four Life counts, and the four-player dashboards step 7 names.
- A composed 2v2 deals four seats, 72 definitions and 72 instances, plays out under the ordinary rules, and leaves the six-player configuration byte-identical to its pre-change measurement.
- Two peers on different game lengths are refused with `MatchFormatMismatch` - a fact a player can act on - where before they were admitted; a 2v2-vs-3v3 pair reports the same rather than a content-digest mismatch.
- `DescribeComposition` is total, so every staged mirror, one-hero and ally fixture has a defined format element and no path exists on which the check silently does not happen.
- Both protocol-version pins read 9 and a version 8 peer is refused from the bounded header.
- Complete unfiltered Edit Mode and Play Mode pass, including a look at a live 2v2 board.

Non-goals: no schema move, no lobby format control (that is the amendment's own second milestone and needs the scene composed when the lobby says so rather than at scene load), and no claim on p.20's odd-count Handicap games, its two-player variant or its 8-10-player double-lane map.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z16-add-rulebook-pp5-6s-printed-2v2-setup-and-both-game-lengths-and-advance-the-wire-to-9-completion-evidence).

### [x] 8.35z17 Stage a situation by hand and play it with the real rules: the Director sandbox

The TWELFTH amendment's first half - the user's "I want a test environment where I can spawn stuff and test it". "Sandbox" here is this staged-board test environment throughout, and is unrelated to the agent process-isolation sandbox `Testing/POLICY.md` names by the same word.

Observable outcome: an arbitrary situation - heroes placed, minions spawned, removed or relocated, a chosen hand, a Level and a purse, Wave and Life counters - is composed and then played with the ordinary authoritative rules, with Undo, restart, the event log, snapshots and replay working exactly as they already do, and a restart returning to the STAGED board rather than to a dealt one.

Design: the amendment left an open choice between staging-as-recorded-commands and a load-a-staged-description entry point, and this build closes it by measurement rather than preference. A recorded staging operation would have to be a `DirectorCommandKind` member, which `NetworkSubmissionCodecTests` and `SeatAuthorizedSubmissionTests` both quantify over - so it would either break two governed ratchets or be carried on the wire and offered to seats, which the amendment forbids outright. The description option is also strictly better: `MatchSession.RestoreSnapshot` clears history and stops recording, so a snapshot-shaped sandbox would have had no Undo at all, while composing the staging into `MatchSessionInitialState` makes every downstream mechanism the ordinary one and costs no new code, because `RebuildInitialState` is already what restart and a retained-prefix Undo re-run.

One new composition seam. Coins and Level had no composition-time route: `RoundResolutionAuthority` built both itself, always Level 1 and always zero. The seam follows the precedent whose own doc comment makes the argument verbatim - `RoundResolutionComposition.TeamLives` travels in the composition because a starting count is a setup decision - and everything added is optional and defaults to today's behaviour, reusing the two EXISTING restore-state types so there is no new persisted vocabulary and **no schema move**. No rule changed.

Two capability items are refused with measured reasons rather than worked around. **Placed tokens and markers**: `AdjacentMoveAuthority`'s token-carrying arm is private and says why - a token reaches the board when an effect places it - and `ComponentLifetimeOwnershipConformanceTests` pins component lifetime to the capability handlers, so a staged token would be a second origin for a board object no rule authored. **The mid-round cursor**: a round index is not a setup decision but where the rules have got to, so staging one asserts a history that did not happen. The Wave and Life counters, which the composed setup already records as setup decisions, ARE staged.

Acceptance checks:

- An empty staging composes the dealt match byte for byte, which is what says the seam is inert.
- A thoroughly staged board no dealt setup would produce - heroes relocated, a minion removed, a minion spawned, a hero at Level 4 with coins, a hand of two - is driven by a seeded walk through the seats under the ordinary rules.
- An Undo over a staged board rewinds to the staged situation, and a restart returns to the staged board rather than to a dealt one.
- A staged spawn takes a spawn point no other minion holds.
- Staging is reachable by no command kind, no message kind and no wire change, so no seat can be offered it.
- Complete unfiltered Edit Mode and Play Mode pass.

Non-goals: no staged tokens or markers, no staged round cursor, no rules change, no schema move, no wire move, and no HUD surface - that is milestone 8.35z18.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z17-stage-a-situation-by-hand-and-play-it-with-the-real-rules-the-director-sandbox-completion-evidence).

### [x] 8.35z18 Reach the sandbox from a launch flag, and say on screen which board it staged

The TWELFTH amendment's second half, the HUD affordances milestone 8.35z17 deliberately left owed because they are Presentation and a worktree can neither compile-probe them cheaply nor run Play Mode.

Observable outcome: `-guardsSandbox <path>` loads a `guards-sandbox-staging/1` file and plays that board; a Director-only banner names it as a sandbox, states that it is not available in online seats, and lists what the staging actually changed. Every existing launch line composes exactly the dealt match it always did.

Design: the composition slots in beside `EngagedMirrorStaging.Restage`, because both answer the same question - what board does this scene begin from - and both answer it before any view is generated or any authority exists. From there the staged setup IS the setup, so the generated views, the board rebuild and the runtime read it without knowing a staged board from a dealt one. The composed initial state is carried to `ConfiguredMatchSceneRuntime` rather than recomposed there, because staging mutates the configuration in place and composing it twice would deal the hands twice.

**`DEV-004` is enforced in the parser rather than asserted in a comment.** That ledger row requires the affordance never be exposed in a real online match, and a staged board reaches one by exactly one route - a host composing it before its seats join - so `-guardsSandbox` is refused beside the host, client and lobby roles, where no later surface can reopen it. This is the deliberate opposite of `-guardsMatchFormat`, which is accepted beside every role: a format is the match a whole table agrees to play and is checked into the admission tuple, while a staging is one machine's private experiment that no admission tuple describes. The banner is Director-only a second, independent way, hidden with the rest of the Director tools when the lobby hands the HUD an online seat.

The banner's summary is DERIVED from the composed staging rather than recorded beside it, so it cannot describe a board other than the one on screen, and it lives engine-free in Application beside the composition - following `PrivateLobbyModel` - so the Unity-free harness covers it while the view holds nothing but strings.

Acceptance checks:

- The flag names a staging file and marks the process a sandbox; an absent flag reads as the ordinary dealt match; a flag with no path is refused.
- The flag is refused beside the host, lobby and client roles, each naming "Director-only", so no seat can be dealt a staged board.
- The sandbox and format flags still compose on the one line where both are legal.
- An empty staging is named as the dealt board rather than drawn as a blank panel a tester would read as a bug.
- A staging that round-trips through its own file format summarises identically, which is the join between the flag and the banner.
- A staged board names every kind of thing it changed, and counts agree with their nouns.
- A live Play Mode check confirms the banner actually renders, and that a staged board composed through the flag path plays, undoes and restarts to the staged origin.

Non-goals: no staging EDITOR - this milestone reaches and reports a staging, it does not author one; no change to what 8.35z17 stages or refuses; no schema move, no wire move, and no lobby surface.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z18-reach-the-sandbox-from-a-launch-flag-and-say-on-screen-which-board-it-staged-completion-evidence).
