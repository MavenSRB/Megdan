# Phase 8 - Completed milestone definitions, fifth shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-06 during milestone 8.35z23's required warning-threshold ownership review, and it is the first review since the fourth shard opened at which **no existing completed shard could absorb a move at all**. The active shard stood at 12,360 words against its own 12,750 warning line, with two milestone definitions due in a single integration pass; `PHASE_8_COMPLETED_3.md` sat at 12,216 and `PHASE_8_COMPLETED_4.md` at 12,262, each within roughly 500 words of the same line, and `PHASE_8_COMPLETED.md` and `PHASE_8_COMPLETED_2.md` have been within a few hundred words of theirs since 8.35j's review. Cascading into any of them would have made this review the next one. Milestones **8.35z8** through **8.35z12** moved here verbatim, the oldest definitions the active shard still held in full and one contiguous run: the shared treatment every named control now carries with its did-nothing report (8.35z8), the awaited shine sweep reaching its hosts (8.35z9), the match-log z-order and the HUD geometry corrected around it (8.35z10), the motion language the whole match reads against (8.35z11), and the attack spotlight built on that one conductor (8.35z12). The run is kept whole deliberately: 8.35z12 composes 8.35z11's conductor and reads it by id, and the wave-one integration that landed them reconciled the two against each other, so splitting them across shards would leave each half pointing at the other. The move is sized for both definitions this integration lands rather than for one, because the previous review sized for two, came up 86 words short, and had to be carried twice. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.
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
