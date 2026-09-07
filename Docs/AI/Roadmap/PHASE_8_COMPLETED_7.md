# Phase 8 - Completed milestone definitions, seventh shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-07 during the Ursafar dealing's required warning-threshold ownership review, and it is opened rather than cascaded into for the reason five earlier reviews each recorded and each then repeated: no existing completed shard could take a run without landing near its own line. `PHASE_8_COMPLETED_5.md` had just absorbed 8.35z41 and 8.35z42 in the same review and stands at 12,709 against its own 12,750 warning line; `PHASE_8_COMPLETED_6.md` has 674 words of room, `PHASE_8_COMPLETED_3.md` roughly 530 and `PHASE_8_COMPLETED_4.md` roughly 490, none of them enough for a contiguous run and every one of them close enough that the move would have to be made again within the day. Milestones **8.35z39** and **8.35z40** moved here verbatim: the breathing awaited outlines with the board's marks drawn as light, and the match log unrolled from the top strip newest-first with the healthy-connection announcement retired. They are kept together because they are one presentation pass - integration batch A landed them in a single wave and reconciled them against each other, and 8.35z47 later corrected both in one milestone, so a reader arriving at either needs the other beside it. The move is sized for BOTH definitions this session lands - the shared gate-derivation and offer-feasibility correctives at 8.35z49, and the tenth hero's dealing at 8.35z50 - because the immediately preceding move in this same review was sized for two, fitted one, and would otherwise have forced a second review mid-ceremony, which is the exact failure the 8.35z22 and 8.35z23 reviews both recorded. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

### [x] 8.35z39 Breathe the awaited outlines and draw the board's marks as light

The user's own direction, quoted in the amendment: "i dont like the little light that goes around, i want a breathing light around the whole thing (Card(s), hero tray). But light, not just yellow color", and "if you can use light for stuff marked on the map, that would be much cooler as well".

Observable outcome: an awaited card or hero tray no longer has a dot crawling round its edge - its own outline kindles and swells on a slow breath; and a marked space stops being a coloured patch painted over the board and becomes the board lit up in that shape, with the artwork underneath still readable.

**The travelling light was a 10x10 pixel dot, and knowing that decided the shape of its replacement.** It was parented into every awaited element and walked round the perimeter in percent coordinates, which is why the rail needed a child element invented for it to travel on. Retiring the dot retires that host too, so this milestone REMOVES up to eight elements from the HUD and adds none.

**The breath had to be able to go DOWN, and only a measurement said so.** The first version ran the outline from its resting gold up toward the brighter rim; it read correctly in source and the offline probe measured the swing at **3.44% of full white**, because the resting colour is already a 0.92-luminance near-white and its gap to the target is almost entirely blue channel - saturation, not brightness. It would have shipped looking identical to the static gold the user asked to replace and no test would have said a word. The breath now kindles from the gold DIMMED by a channel multiplier up to the full painted gold: a **x2.22 luminance swing with hue exactly constant**, and its top is the colour the HUD already paints, so a freshly rebuilt element is already on the breath and an element the treatment lets go of is left indistinguishable from one it never touched.

**Every mark keeps its exact geometry, footprint and colour, and only the rendering changes** - the user's own tightening. The halo becomes additive, so a mark can only ever brighten the space it names and the board's printed artwork survives underneath at full strength; the near-white core stays painted, because it is what carries the shape on the bright half of the board. Bloom was rejected on measurement rather than taste: this project's renderer carries no post-process data at all and the volume profile's Bloom sits at intensity 0, so a glow from post-processing is a render-pipeline change with a per-frame cost to justify, not a material choice.

**One core in the type is not white-biased, and the first complete Edit Mode run of this work caught it.** A tile that is both an Attack target and a Defense contributor draws a saturated core inside a blood-red halo, and it had always obtained that saturation by BORROWING the halo material. Once halos became light the borrow silently handed that core the luminous alpha - expected alpha 1.000, measured 0.620 - so the borrow becomes a declared saturated-core family of its own, and a new case pins the distinction so the next change to the light family cannot re-borrow it.

Acceptance checks:

- The awaited breath is a pure function of the existing kinetic clock read inside the one conductor's tick, adds no second driver, and hands every lit element the same intensity on the same frame.
- The breath's brightness swing and its constant hue are asserted as measurements, and the colour it recognises is stated as distance to its own segment rather than a bounding box, with the margin to every other colour the HUD paints on a border recorded.
- Every six-corner mesh, ratio, radius and declared colour is unchanged, asserted against transcribed expectations rather than values read back from the type under test.
- A luminous mark is the shipped blended template with only its blend factor changed, so no shader variant is added.
- The composite mark's core is saturated paint at full alpha while its halo is light, and the two are not the same material.
- The token preparation tool gains a team-neutral mode whose check is INVERTED - a prepared neutral face must classify as neither team - and all sixteen shipped team-baked faces regenerate byte-identically.

Non-goals: no Bloom or post-processing; no new mark, colour or footprint; no change to which spaces are marked; no HUD element added; and no tuning of the luminous strength against the painted board, which is one number and wants the user's eye on a rendered frame.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z39-breathe-the-awaited-outlines-and-draw-the-boards-marks-as-light-completion-evidence).

### [x] 8.35z40 Unroll the match log from the top strip, newest first, and stop announcing a healthy connection

Three refinements the user asked for with a screenshot, the first quoted verbatim: "make the dropdown match the top, not to be different like this".

Observable outcome: opening the match log unrolls the top strip's own band downward instead of dropping a foreign dark box under it; the newest thing that happened is the first thing read; and the words "CONNECTED TO THE HOST." stop sitting on top of the log's header, because a healthy connection no longer says anything at all.

**The chrome is READ from the strip rather than copied, which is the difference between a fix and a coincidence.** The tray drew the shared panel background while the strip drew its own darker one - two independent styles that happened to disagree - so the correction is one shared source: the status bar publishes its own band language (background, corner radius, border width, insets, label size) and both it and the tray use it. A second literal that agrees today would drift the next time either moved. The tray also hangs FLUSH now, with the squared, unbordered top edge meeting the strip's own bottom hairline, so the seam reads as one band unrolling rather than as two bordered boxes with a gap between them.

**A plain reversal is the wrong answer to "newest first", and saying why is the whole of item 2.** Reversing the rendered lines puts every ROUND and TURN header BELOW the entries it heads. So the stream is grouped into a header plus the lines under it, the groups are reversed, and each group's body is reversed beneath its own header - which is what "with the group headers adapting correctly" means, pinned by a worked example in the fixture. The undo marker moves from last to first, because under newest-first the top is where "now" reads. The scroll position resets only on the closed-to-open transition, so an arriving entry cannot yank a reader who has scrolled back into the history.

**The reorder is proved to be a PERMUTATION, which is how milestone 8.35r's privacy boundary survives it untouched.** Same lines in, same lines out: a re-order cannot surface a fact that was not already in the list handed to it. The closed-vocabulary rendering is byte-identical - the diff ends before it - so this milestone changed reading order and chrome and nothing about what the log may say.

**A healthy connection earns no screen, and the branch that decides it is stated structurally.** The banner used to be shown for every state the moment the online HUD attached, the healthy one included. It is now hidden exactly when the feedback would have read "Connected to the host.", derived from the same condition rather than by comparing the sentence back - and the fixture sweeps the whole state space asserting the two agree, so a drift fails a test instead of silently hiding a notice. The anomalous states are placed clear of both the narration bar and the open drop-down BY CONSTRUCTION, under the tray's whole open extent, so one rectangle answers "both open and closed" instead of a state-dependent position that could be wrong in one of them.

**A shipped Play Mode test PINNED the defect**, which is the finding worth carrying: a fixture whose name is about HUD reuse and host loss asserted that the status element was visible and contained "CONNECTED". Nothing in the presentation change would have revealed it - the harness cannot see Play Mode and the compile probes only compile it. It was found by grepping every reader of the element before committing. **When a change makes something stop being drawn, grep for who asserts that it IS drawn.**

Acceptance checks:

- The tray's background, corner radius, border, insets and label size are read from the status bar's own published band language rather than restated, and its top edge is squared and unbordered against the strip.
- The reading order is newest-first with each group header above its own newest-first body, pinned by a transcribed worked example, and asserted to be a permutation of its input.
- The undo marker renders first, and the scroll offset resets only on the closed-to-open transition.
- The connection banner is hidden exactly when the feedback would be the healthy sentence, asserted across the whole state space rather than for sampled states.
- The banner's rectangle clears the narration bar and the open drop-down at every supported viewport, and stays on the panel from the shortest one any sibling fixture uses.
- The drop-down's paint order is asserted against the strip, both rails, the tie-breaker banner and the tooltip layer.

Non-goals: no change to what the log may say - the entry vocabulary, the privacy predicate, the wire and the schema are untouched; no new log content, filter or search; and no permanent connection indicator, which is left as a question for the user rather than answered by this milestone.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z40-unroll-the-match-log-from-the-top-strip-newest-first-and-stop-announcing-a-healthy-connection-completion-evidence).
