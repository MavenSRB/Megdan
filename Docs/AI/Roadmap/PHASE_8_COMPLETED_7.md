# Phase 8 - Completed milestone definitions, seventh shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md). Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-07 during the Ursafar dealing's required warning-threshold ownership review, and it is opened rather than cascaded into for the reason five earlier reviews each recorded and each then repeated: no existing completed shard could take a run without landing near its own line. `PHASE_8_COMPLETED_5.md` had just absorbed 8.35z41 and 8.35z42 in the same review and stands at 12,709 against its own 12,750 warning line; `PHASE_8_COMPLETED_6.md` has 674 words of room, `PHASE_8_COMPLETED_3.md` roughly 530 and `PHASE_8_COMPLETED_4.md` roughly 490, none of them enough for a contiguous run and every one of them close enough that the move would have to be made again within the day. Milestones **8.35z39** and **8.35z40** moved here verbatim: the breathing awaited outlines with the board's marks drawn as light, and the match log unrolled from the top strip newest-first with the healthy-connection announcement retired. They are kept together because they are one presentation pass - integration batch A landed them in a single wave and reconciled them against each other, and 8.35z47 later corrected both in one milestone, so a reader arriving at either needs the other beside it. The move is sized for BOTH definitions this session lands - the shared gate-derivation and offer-feasibility correctives at 8.35z49, and the tenth hero's dealing at 8.35z50 - because the immediately preceding move in this same review was sized for two, fitted one, and would otherwise have forced a second review mid-ceremony, which is the exact failure the 8.35z22 and 8.35z23 reviews both recorded. Definitions remain active authority under the same `Roadmap/` family threshold; this is sharding, not archiving, and completion evidence remains in commit bodies indexed by the Phase 8 handoff archive.

Milestones **8.35z45** and **8.35z46** followed them here on 2026-09-08 during the polish wave's own required review, **proactive** for the sixth time in this phase: `PHASE_8.md` stood at 14,234 words against its 15,000 limit with FIVE definitions due in one integration pass (8.35z58 through 8.35z62), which would have carried it to roughly 15,272 and past the limit mid-ceremony. This shard was the destination on its own opening arithmetic - at 3,246 words it takes the 1,686-word pair to 4,932, roughly a third of its line, the largest margin any completed shard has had for a run in this phase - and the pair is contiguous and self-contained: the attack-sequence bullet narrowed to the subjects its own sentence names, and the ninth hero's dealing. `PHASE_8.md` keeps 8.35z47 onward and lands the five new definitions at 13,755 words, 1,245 clear of its limit.

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
### [x] 8.35z43 Roll a refused restore back with the graphs it displaced, not by re-reading them

Discharges the corrective debt milestone 8.35z23 recorded against `RISK-STATE-002`, and carries four smaller structural hardenings that were queued behind it.

Observable outcome: a restore the reader refuses leaves the session refused-and-unchanged rather than half-restored, on the undo and restart paths as well as the restore path; a practice-bot seat driver that is asked to drive a seat it does not own refuses; and a completed hero selection with no composer fails loudly instead of quietly composing nothing.

**The rollback ran the deserializer on the failure path, which is where it is least safe.** `MatchSession.RestoreSnapshot` caught a failed restore and rolled back by RE-DESERIALIZING the pre-image - and a failed restore is exactly the situation where the state being described may be one this build cannot read. The second throw escaped uncaught, after the graphs had already been replaced, leaving a live session holding a half-finished restore where its own contract promises "refused, unchanged".

**The fix needed no new type, and the reason it is available here is structural.** The aggregate already carried the capture/adopt pair the replay path uses, and that pair IS a rollback that cannot fail: a restore REPLACES the graphs and never mutates the ones it displaces, so the old graphs are still intact and reinstalling them is an assignment. A command, by contrast, mutates its graphs in place, and for that a serialized pre-image really is all that is left - which is why the aggregate's own rollback stays exactly as it was for command execution. The same two lines extend to reconstruction (undo and restart), whose catch had the identical fragility; hardening one and leaving its twin would have been half a fix. A second property fell out unlooked for: the old rollback replaced the live authorities with deserialized copies even on a REFUSAL, so "refused, unchanged" silently swapped every authority object. It is now literally untouched.

**The recorded batch-push finding was WRONG, and the gap was coverage rather than behaviour.** Milestone 8.35z34's site note claimed a batched push could re-enter execution without consulting the answers already given. It has consulted them since milestone 8.8, blamed to the commit that introduced it; what was missing was a test saying so. Three cases now say it, and the stale note is corrected rather than left to mislead the next reader.

**The bot evaluation screening retuned nothing, and that is the honest result.** Three signals measure as inert and one is live with the incumbent weight already jointly best; the one apparent outlier is one match in forty, which is noise. A wrinkle is recorded rather than smoothed over: the default configuration no longer reproduces the recorded phase-A invariance figure, so the standing suite pins the MECHANISM - the decision-change counts - rather than a replay of a number that has moved.

Acceptance checks:

- A restore whose pre-image cannot be read is refused with a typed refusal, leaves the published projection instance and the full transaction state untouched, and a subsequent restore of a readable snapshot succeeds - which a damaged session would fail.
- The seam that produces an unreadable pre-image is proved still to defeat a deserializing rollback, so the case above cannot pass vacuously.
- The undo and restart reconstruction path is hardened by the same pair and asserted on the same terms.
- A practice-bot seat driver refuses a seat it does not own, drives one it does, is absent by default, and does not throw on a per-frame update in any of its three states.
- A completed hero selection with no composer throws naming the discarded line-up seat by seat.
- The complete unfiltered dotnet harness passes at the final revision.

Non-goals: no bot weight is retuned and no evaluation signal is added or removed; no change to the aggregate's command-execution rollback, which is a different mechanism; and no reassessment of `RISK-STATE-002` itself, which needs the register's own ownership review.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z43-roll-a-refused-restore-back-with-the-graphs-it-displaced-not-by-re-reading-them-completion-evidence).

### [x] 8.35z44 Sweep the stale placeholder ids, track the shared compile probe, and drop Sabina's phantom clause

The pre-package housekeeping batch: three items the user approved together before the twelve-hero build.

Observable outcome: no shipped comment names a milestone id that was never assigned; the compile probe every worker had been copying privately is a tracked repo artefact that works from any worktree; and the one card clause the roster carried that no printed face supports is gone.

**Blame names the AUTHOR of a comment, not its REFERENT, and five tokens turned on it.** Fifty-eight placeholder tokens were found, of which fifty-five are renamed to the ids their work actually landed under and three are deliberately left. Renaming by authorship alone would have filed five of them under the wrong milestone: four sit in a motion-language file but read "the button pass milestone 8.35x already animates" and name the button system, and one blames to the milestone that merely re-anchored the line while its own sentence says which milestone introduced the rectangle. The discriminator is mechanical and worth reusing - read the sentence, not the blame; where the token is followed by a self-description, the referent is whatever that description names.

**The three left alone are the token quoted AS DATA, and renaming them would make them false.** Each justifies why a hero's content revision was re-based rather than incremented, on the grounds that the placeholder is a claim rather than a milestone and no milestone existed to inherit. The token is the subject of the sentence rather than a provenance prefix. There is an objective discriminator that holds across all fifty-eight: the three quoted ones are marked up as code, and all fifty-five others appear bare.

**The probe is tracked because tracking is what makes it correct.** Seven private copies of it existed across worktrees, and the one the notes called shared no longer worked on any current tree. Its repository root now derives from the probe file's own location rather than from a relative path that silently resolved to the main tree wherever it was copied - so a worktree worker can no longer compile someone else's sources and believe the answer. That fix only works because every worktree inherits its own copy of a tracked file.

**Sabina's phantom sat on a SHARED helper, so it was three cards and not one.** The clause was declared once on the helper that builds Troop Movement, Marching Orders and Path to Victory, and none of the three prints it; the helper's own documentation already contradicted the declaration. Inertness was measured rather than argued: with the declaration restored the live offer surface is unchanged while both structural guards fail. The behavioural case added here therefore guards the ENGINE against growing the route that would give the clause teeth - it would not catch the clause coming back, and saying so plainly is part of the record.

Acceptance checks:

- Every renamed token is verified byte-identical apart from the token itself by reverse substitution, and no executable line moves.
- The three tokens quoted as data are left, and the reason is recorded where a later sweep will read it.
- The tracked compile probe runs clean from a worktree and from the main checkout, compiles the whole production and test surface, and is proved able to fail by a negative control.
- Sabina's repeat offers every legal minion again, including the one it just moved, on a board staging more than one - the smallest board on which the question has a visible answer.
- The identity oracle reports exactly one moved pin and the other seven byte-identical.
- The complete unfiltered dotnet harness passes at the final revision.

Non-goals: no behaviour change beyond the removed clause; no renaming of ids on branches not yet landed; and no reassessment of the roster's other repeat bindings, which the ledger row this milestone answers already measured.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z44-sweep-the-stale-placeholder-ids-track-the-shared-compile-probe-and-drop-sabinas-phantom-clause-completion-evidence).


### [x] 8.35z45 Narrow an attack sequence bullet to the subjects its own sentence names

Composes Middlefinger of Death, the last of Dodger the Warlock's faces that carried a named deferral rather than a composition, and moves the snapshot schema alone.

Observable outcome: a modal Attack whose bullet prints a condition about WHO it may target offers only the units that satisfy it, and offers nobody where none does - while the other bullet of the same card, on the same board and at the same instant, still offers the unit it may take.

**The sketched seam was refused, and the refusal is the design.** The multi-sequence contract had sized this extension point as an optional capability reference resolved through the catalog as a per-candidate predicate. `AttackSequenceBullet` is persisted, and every writer in the snapshot serializer says the same thing about what may cross: identity, and content the payload NAMES - never semantics it CARRIES. A capability id plus a version is a name the reader would have to resolve to a handler and then trust to filter a target set, which is a payload choosing which code runs. A closed enum is content the payload names: it reads back exactly as the bullet's four existing policy enums do, and a value outside the declared range is a rejected snapshot rather than a reconstructed one. Three consequences follow, and all three are the point - the rule lives in the member's NAME, so adding a member is not a content-revision event; the complement clause another hero prints becomes one member and no schema move at all; and it went on the BULLET rather than on the printed target policy every Attack in the roster declares, which could not have been widened without re-identifying every dealt hero.

**Where it is applied leaves p.13 alone.** The narrowing runs over the set the existing legal-target query already admitted, so which units, what reach and p.18's immunities are asked once and unchanged, and the card's extra sentence narrows the answer rather than replacing the question.

**The absence of card state is LOUD.** A bullet declaring a predicate that reaches the derivation without card state throws, naming the bullet and the clause. The two silent answers - admit everybody, or admit nobody - are a widened offer and a card greyed for nothing, and both look exactly like an ordinary board position.

**One derivation, four consumers.** The discard reading existed as THREE copies, one carrying a comment claiming they could not drift. Three copies cannot keep that promise; there is now one derivation and all four consumers ask it.

**The schema moved for a payload this corpus does not contain.** The field costs the compatibility corpus zero bytes and zero digests - measured, by running the corpus with the field in place and the version left alone - so the whole digest movement is the version value itself. It moved anyway because a shipped build can already produce a payload carrying a chain, and such a payload is one integer per bullet shorter: a newer reader walking it would take the next bullet's id out of the middle of this bullet's policy. That is corruption rather than a short read, and turning it into a refusal is the entire job of the version guard. A corpus that happens to hold no example of a shape is evidence about the corpus, not about the shape.

Acceptance checks:

- A bullet declaring a printed subject condition offers only candidates that satisfy it, proved on a board where the card's OTHER bullet offers a unit at the same instant - so reach and immunity are established by that bullet's own offer and the narrowing is the single degree of freedom.
- A bullet whose predicate has no satisfying candidate offers nobody, and the card's remaining bullet is unaffected.
- A declared predicate that reaches the derivation without card state throws, naming the bullet and the clause.
- The discard fact has exactly one derivation, and every consumer of it reads that one.
- The snapshot schema rejects the previous version rather than migrating it, and the version guard is asserted at every site that pins it.
- The identity oracle moves exactly one pin and leaves every other byte-identical.
- The complete unfiltered dotnet harness, and both complete unfiltered Unity suites, pass at the final revision.

Non-goals: no change to the bound-target resolve path, which is unreachable mid-chain and which milestone 8.35z27 owns; no composition of the complement clause another hero prints, which moves that hero's content revision and belongs to her dealing; and no widening of the printed target policy every Attack declares.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z45-narrow-an-attack-sequence-bullet-to-the-subjects-its-own-sentence-names-completion-evidence).

### [x] 8.35z46 Deal, register and player-path gate Silverarrow the Pathfinder as the ninth hero

The third of the six dealing milestones the content wave owes, on exactly the ceremony milestones 8.35z34 and 8.35z38 proved: register, deal, gate, journey, govern, verify.

Observable outcome: a player loads a staged match, is dealt Silverarrow's eighteen cards, and can reach every face of hers a printed sentence allows - with her contract in the build's registered set, her identity pinned, her artwork addressed, and her player-path gate passing beside the eight that came before it.

**Rain of Arrows' second repeat clause is composed, and the costed design for it was wrong in a way that would have shipped silently.** Milestone 8.35z30 parked "if you do, repeat once on a minion" on a measured seam limit. The wall it named is real: a repeated Attack derives its admitted unit kind from its own CARD's declaration - milestone 8.24 chose that so the clause moves no snapshot field - and a derivation that asks the card cannot tell two clauses apart. The wall it missed was found by probe before a line was written: rulebook p.10's "You cannot repeat a repeated action!" is enforced for card text by milestone 8.25d as ONE BOOLEAN about the enclosing attack, so a second clause would have been skipped in silence inside the very instance it exists to fire in. Both have one cause and one answer that costs nothing: card text is always started with no completed repeats, so the bounded-chain counter every Skill repeat uses reads zero in every Attack instance, and the chain already mints each instance's identity by appending to the last one's - which is already restorable state. `CardTextRepeatChain` reads it; no schema version moves, and a card printing one clause keeps the exact walk it had, which is every face in this build but this one. Recorded as [rules-ledger row 26](../RULES_LEDGER.md#open-source-and-rules-questions) because the next face to print a chain inherits it.

**The registration is what moves the numbers, and this time the move is exactly ONE contract.** `fast-travel@1` is a handler TYPE this build had never registered, so it owed a `SyntheticShowcaseHand` consumer, which this milestone added. Her other new-to-her contract, `choose-line-destination@1`, registered already at Misa's dealing (8.35z38) and needed nothing here - her three straight-line faces reach the same registered contract Misa's five Blue Skills do, which is the ordinary case of two heroes sharing one rule rather than a second registration. Her six Active-effect creations move the count by nothing either, on the sentence three heroes running now have written: `ActiveEffectCreationHandler` is a type this build has had since milestone 6.8. So the pinned handler count moved 61 to **62** rather than by two. Every corpus digest and both scenario-digest families moved on the same cause, schema **unmoved at v40**, and the arithmetic was measured rather than assumed: the failing digests read off a complete unfiltered run are what this milestone's re-pins carry forward.

**The gate table was written from a derivation rather than checked against one, and needed no new interaction shape at all.** An offline probe printed what `HeroPlayerPathMatrix` derives for each of her nineteen faces before a row existed, and all nine gate cases passed on their first run. Three capability classifications were added and no shape - `fast-travel`, `choose-line-destination` and `synthetic-attack-took-a-hero` - which is the healthiest outcome this gate has had: nineteen new faces and the surface already had a name for every input they ask for.

**The staging fact that decides where her journeys live was measured before any was written.** The unrestaged staged board holds the two heroes fifteen spaces apart, and RULING 1 makes six of her seven Attacks reach a BAND rather than a ceiling - so on that board every Attack she prints has no legal target at all. The engaged restaging holds them exactly two apart, which is her Gold basic's printed Range. For every earlier hero the restaging was a convenience; for her it is what makes her set reachable.

**Her hero-token art had never been prepared**, on exactly the shape Misa's dealing found: declared at composition time but never written, which the piece journey would otherwise have caught as a hero wearing the generated prism fallback. `Tools/Prepare-HeroTokens.ps1 -Hero silverarrow -Check` named the gap before a journey had to; running it without `-Check` wrote both team fronts, verified against the same shared reference-ring measurement every other hero's token passes.

Acceptance checks:

- Silverarrow is dealt by a configured match, her one remaining contract is registered, and both halves of milestone 7.9's gate are asserted against each other so neither can move alone.
- The registered-handler count is 61 to 62, her identity pin is measured on a complete unfiltered run, and the other eight pins are byte-identical in that same run.
- Her eighteen artwork rows exist and the catalog's bijection holds at 162 dealt definitions across nine heroes.
- Her player-path gate passes, every row's shapes match the derivation exactly, and the eight earlier heroes' derived rows are unmoved.
- Rain of Arrows' three-instance chain is proved on a purpose-built board, and its containment - no other composed hero's card reaches more than one clause - is asserted rather than assumed.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: Keep the Distance stays parked on the standing p.20 Handicap deferral; the three other composed heroes (Ursafar, Tali, Whisper) are not dealt; and no official six-player line-up changes, which milestone 8.25 owns.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z46-deal-register-and-player-path-gate-silverarrow-the-pathfinder-as-the-ninth-hero-completion-evidence).

