# Phase 8 - Completed milestone definitions, tenth shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and
self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md).
Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-19 during the eighteenth hero's series, when `PHASE_8.md` stood at 14,653 words against its
15,000-word hard limit with two authorized blocks (8.58, 8.59) and the Rowenna ceremonies still to write; the
contiguous run moved here is 8.35z54 through 8.37b. `PHASE_8_COMPLETED_9.md` stood at 8,057 words and would
have crossed its own 12,750-word warning line, so a tenth shard was opened.
Extended 2026-09-21 during milestone 8.70's ceremony, when `PHASE_8.md` crossed the hard limit at 15,577
words: the contiguous run 8.44 through 8.56 moved here after 8.37b.
Extended 2026-09-22 during milestone 8.80's recording, when `PHASE_8.md` crossed the hard limit again at 15,197
words: milestones 8.58 and 8.59 moved here after 8.56.
Extended again 2026-09-22 night during milestone 8.82's recording, at 15,037 words: milestones 8.60, 8.62,
8.63 and 8.64 moved here after 8.59.

## Completed definitions

### [x] 8.35z54 Repair the private lobby the live playtest broke, and rebuild its flow to the user's spec

The 8.35z51 ten-hero package failed in a real multiplayer session, and every automated tier was green when it shipped. Three of the user's five findings are root-caused and fixed here; the other two are answered by the acceptance this milestone's own instrumentation made possible.

Observable outcome: a player joins a host naming no seat, sees the open seats, takes one, readies, picks a hero in rulebook p.22's alternating order, and plays the match that line-up composed - offered their own seat's decisions and nobody else's.

**THE HEADLINE DEFECT WAS ONE UNCALLED METHOD.** `EvaluateMatchStart` recomposed the match for the chosen line-up and never published it, so every seat kept the projection of the PROVISIONAL match issued at seat grant and every command each of the six offered came back refused by name, with the host version stuck at 0. `RepublishProjections()` has existed since 8.33 for exactly this class. The control run - nobody picks, provisional match stands - was correct throughout, which is why `SeatProjection` and `AuthorizedCommands` were never the culprits: the seat IDENTITY was.

**THE SECOND DEFECT IS WHY THE FIRST WAS INVISIBLE FROM THE SURFACE.** `PrivateLobbyDriver.Signature()` was the whole redraw condition and named each roster row's seat, occupancy and readiness - not the hero and not the chosen flag, which are exactly the two fields a pick moves. An accepted, broadcast, applied pick left the string character-identical and nothing redrew. It is now `PrivateLobbyModel.RosterSignature`, stated once beside the rows it draws.

**THE THIRD IS THE REUSE FAMILY IN A NEW PLACE.** `Compose` subscribes the board to the HUD's `PlayfieldLayoutChanged` and waits, but the HUD is deliberately kept across a re-composition, so on a second composition it has already laid out and the rebound board waits for an event that never comes. Only a joiner recomposes at the transition, which is precisely the "player side" the finding named.

**THE FLOW IS THE USER'S SPEC READ LITERALLY.** The pre-join seat picker is removed; the cycling SETUP button - which their own host log shows them pressing six times without ever reaching 2v2 - is replaced by named 2V2 / 3V3 / SHORT GAME / LONG GAME controls; FILL WITH BOTS makes its bots PICK, in turn, through the same presenter press a player makes; and the host gets a lobby UNDO bounded by the one-way transition. Readiness OPENS the picking and the LAST PICK starts the match, which is the split this milestone owes: one rejected design attempt let an untouched selection start on readiness, and it composed a match before anyone had picked.

Acceptance checks:

- A picked line-up composes, publishes, and every seat's own commitment is accepted.
- A joiner's accepted pick redraws on the picker's client and on the host.
- A joiner's board receives the HUD's layout at the transition composition.
- Wire `guards-private-online/10` and schema v40 do not move: everything the flow needed was already published.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: no schema or wire movement; no matchmaking, accounts or public service.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z54-repair-the-private-lobby-the-live-playtest-broke-and-rebuild-its-flow-to-the-users-spec-completion-evidence).

### [x] 8.35z55 Derive the match-log tray's width and x from the narration bar

The 2026-09-08 user directive, with a screenshot: "make it as wide as naration part on the top is."

Observable outcome: the match-log drop-down is exactly the narration bar's width and horizontal position, at every viewport the bar is defined at.

`CalculateMatchLogDropdownRect` no longer carries its own width literal centred by an independent clamp; it reads the bar's own `xMin` and `width`, so the two cannot drift. The tray inherits the hero-rail clearance the bar already guarantees by construction rather than re-deriving a margin clamp of its own. Re-pinned at 2560x1440: `(830, 96, 900, 320)`, was `(960, 96, 640, 320)`.

Acceptance checks:

- The reference-viewport rect is re-pinned and asserted equal to the bar's x and width.
- The existing eight-viewport sweep additionally asserts the alignment and hero-rail non-overlap, because a fixed 640 becoming a viewport-dependent width could newly reach a rail at a size nobody eyeballed.

Non-goals: no state-bearing change; height and vertical placement unmoved.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z55-derive-the-match-log-trays-width-and-x-from-the-narration-bar-completion-evidence).

### [x] 8.35z56 Sequence the attack spotlight's zoom, a beat, then the attack cue

The 2026-09-08 user directive: "wait for the camera to zoom in on the attack, before doing the animation... Zoom in, a small pause, then smack."

Observable outcome: the camera lands, a beat passes, and then the attack plays - rather than the animation stepping on the camera's own arrival.

The attack cue used to release the instant the zoom crossed from Entering into Held, a measured gap of one simulation frame. The one shared deferral predicate becomes a function of (phase, elapsed-in-phase): HELD defers for its own opening `PostZoomBeatSeconds` (0.3s, the midpoint of the user's stated band). No second driver, no coroutine, no new phase - the one-conductor law is unbroken.

**THE RANGED ASYMMETRY THE BEAT OPENED IS CLOSED IN THE SAME MILESTONE.** The ranged tracer gated on the bare HELD phase, so on a ranged pair the dash crossed the ground a beat BEFORE the attacker's jab - the same cue's other half, and precisely the two events the beat exists to collapse into one. The view model now carries `ElapsedInPhase` and answers with the same predicate the cue layer and the conductor read.

**AND THE DEFECT THAT FIX WOULD HAVE SHIPPED, CAUGHT BY ITS OWN NEW FIXTURE.** `Tick` re-rendered only when phase or framing weight changed, and both are constant across the beat - so the surface would have held a model saying "still deferring" forever and the ranged tracer would never have drawn at all. The deferral answer is now part of what counts as a visible change.

Acceptance checks:

- The cue does not play while the camera travels, does not play on the tick it arrives, and plays one beat after.
- The surface's gate and the cue layer's gate are the same call, asserted equal at both moments.

Non-goals: no schema, wire or content movement; `MaximumHeldSeconds` untouched.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z56-sequence-the-attack-spotlights-zoom-a-beat-then-the-attack-cue-completion-evidence).

### [x] 8.35z57 Make every joiner and spawned bot inherit the host's chosen format

The 2026-09-08 playtest's third report on the rebuilt lobby: "Nothing is working, lobby has the the same issue as before, can play with all heroes. Hero trays didnt change" - and, on the same shipped z56 build, "5 test driver(s) failed to join in time" on a table the tester had set to 3v3.

Observable outcome: a player who types only an address, and a bot the host spawns, both join whatever printed setup the host chose - and the line-up the table drafts is the line-up the match fields, on every seat's board, tray and authority.

A joiner cannot know the host's team size and game length before it dials, so it composed `MatchFormat.Default` (3v3 Quick) at Awake and derived its admission tuple from that; any host playing anything else refused it `MatchFormatMismatch`, and FILL WITH BOTS spawned its children on the same default so they were refused identically. **The format id carries the game LENGTH as well as the team size**, which is why this was never confined to 2v2 and why "3v3 is the default, so format cannot be the cause" was wrong: the tester's own shipped-build host log reads `LOBBY_FORMAT format=3v3-long`, with 23 `MatchFormatMismatch` rejections, ONE seat ever claimed and no `SEATS_COMPLETE` at all. The "failed to join in time" banner was the secondary symptom - refused bots exit, their seats stay open, and the join grace expires - not a separate timeout defect.

The host already publishes its own format id in the plaintext refusal, which is exactly the "told the fact it can act on" its reason enum intends, so a refused joiner reads it, recomposes this scene onto that format - rebuilding the runtime and, with it, the admission tuple derived from the composition - and re-dials ONCE. A bot needs no recovery at all: the host hands it `-guardsMatchFormat` with the format it actually composed, so it is never refused. No wire change and no schema change.

**THE GATE THAT WOULD HAVE CAUGHT THIS IS ENSHRINED HERE.** `AuditProjection` already proved a seat's authority is scoped to its own hero; nothing proved the hero it is scoped ABOUT is the one the seat DRAFTED rather than the printed default - internally identical states, which is the degeneracy that let this pass every green tier twice. The Smoke cohort now drafts the LAST available hero, so the draft is non-default by construction, and fails the run unless that hero is what the seat's online projection fields, its recomposed board seats, and its tray draws. The runner asserts the full seat count for the chosen format is claimed, so "bots join and draft" is a first-class criterion, and `-MatchFormat` is given to the HOST ONLY so joiners and bots must inherit it over the wire.

Acceptance checks:

- A joiner and every spawned bot seat on a host playing a non-default format, at both `3v3-long` and `2v2-quick`, through the shipped lobby controls over real sockets.
- The composed match equals the DRAFTED line-up - not merely a valid set of the right size - on the board, the tray and the seat's own authority, per seat.
- Whisper resolves her own prepared token rather than the primitive prism.

Non-goals: no wire, schema or content movement; the in-match bot spin and the parked placement prompts stay their own correctives.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z57-make-every-joiner-and-spawned-bot-inherit-the-hosts-chosen-format-completion-evidence).

### [x] 8.35z58 One button treatment everywhere and the hand stays visible while leveling

The user on the shipped build - "i saw the buttons in the latest build, they were the old Buttons" - and a directive on the same seam, "when leveling. Show curent hand (all 5 cards) above the offer."

Observable outcome: every named control draws as a raised chip from one shared definition, and a seat levelling a hero sees its own hand above the upgrade offer.

The 8.35z8 pass HAD applied and read as nothing, sitting ~0.08-0.11 brighter than the panels around it. `MatchHudTheme` raises the idle chip ~50%, promotes the lobby's own edge tone to one shared border, and takes the radius const-to-const to the band radius the user accepted live. `BuildHeroOption`, added at 8.35z37 after that pass, now routes through the same chrome.

Acceptance checks:

- A measured contrast floor against the darkest HUD panel, one-source radius equality, and the literal 100ms press.
- The preview stands above the offer in document order, shows the seat's own cards unpadded, and carries no `Button` in its subtree.

Non-goals: no schema, wire or content movement; the ordinary hand card and the invisible action hotspot stay outside the treatment.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z58-one-button-treatment-everywhere-and-the-hand-stays-visible-while-leveling-completion-evidence).

### [x] 8.35z59 The HUD panel is never smaller than its reference in either dimension

A live ultrawide playtest: the rail plaques overflowed into the tray band and the hand was drawn over them.

Observable outcome: at every supported aspect, three rulebook p.6 rails a side stack fully and the board stays clear of the round dock.

UI Toolkit lerps `MatchWidthOrHeight`'s normalized axes LINEARLY, so at `Match` 0.5 an aspect wider than 16:9 makes the panel SHORTER than the reference - the opposite of what this file's own comment claimed - and the rail column, the only vertical "whatever is left" region, absorbed it. Following the height alone opened the mirror case below 16:9, so the MODE is the answer: `Expand` keeps neither dimension under the reference, with the board bounded against the dock.

Acceptance checks:

- An eleven-resolution matrix gates the ultrawide rail stack and the narrow-aspect board/dock separation offline.
- 2560x1440 and 1920x1080 are pixel-identical across the change, pinned by test.

Non-goals: no schema, wire or content movement; a composed HUD still does not re-lay-out on a mid-session resolution change.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z59-the-hud-panel-is-never-smaller-than-its-reference-in-either-dimension-completion-evidence).

### [x] 8.35z60 The top strip counts down the Waves that remain on every seat

The 2026-09-08 directive: "Waves up top should show waves remaining until the end of the game." The strip read "WAVES 3", the printed total, which never moved.

Observable outcome: the strip reads "WAVES REMAINING N" and N falls as the table pushes, on the host's screen and every joiner's.

Rulebook p.8's shared pool already stood on the lane-push authority, so the in-process half is a contract gap costing no wire bytes. The networked half is real growth, taken here because every real playtest is networked and a host-only readout would leave this correct for one seat: protocol `guards-private-online/11` carries the pool as one `Int32` beside the per-team Life counters, carried rather than derived because a seat holds no rule material saying how many pushes a match has spent.

Acceptance checks:

- A joiner's strip shows the host's live count, proven in the live two-format lobby gate.
- The wire-exclusion floor is RESTORED to 7 rather than left raised.

Non-goals: no schema movement (v36/v38 unmoved) and no content movement.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z60-the-top-strip-counts-down-the-waves-that-remain-on-every-seat-completion-evidence).

### [x] 8.35z61 Explain the skipped card and narrate the match to disk

A playtester watched Brogan's committed card come up and nothing happen, with no line saying why - and every previous report lacked a file that could say what went wrong.

Observable outcome: the seat-public log names the hero whose card resolved with no action, and the host writes a timestamped narration to disk the match can be reconstructed from.

Rulebook p.13's rule was implemented but SILENT, which reads as a bug to the person watching. The narration owns one sentence per kind with a case requiring every kind to have one, its header names the build, map and every seat, and it flushes as it goes because a crash is when it matters most. It attaches on BOTH host paths - the 2026-09-06 playtest was hosted through the lobby.

Acceptance checks:

- Every line of p.13's list reaches the match log or is a named exception.
- A driven match writes the file with rows, run-proven rather than compile-proven.

Non-goals: no schema, wire or content movement. CARRIED GAP: p.13's coin award still has no row.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z61-explain-the-skipped-card-and-narrate-the-match-to-disk-completion-evidence).

### [x] 8.35z62 The transition redraws every surface and a refused format press says so

The shipped 8.35z57 build was still broken in the real UI flow: a 2V2 press produced a six-seat table with no `LOBBY_FORMAT` line at all, and after the transition the rails still drew the printed demo six while the board spawned the picked heroes.

Observable outcome: a format press either changes the setup or says why it cannot, and the one-way transition redraws every match surface so the trays name the heroes the table drafted.

Both are one shape - A SILENT EARLY RETURN. `ConfiguredMatchHudRenderer.Build()` was once-only for its whole body, right for the panel and wrong for the surfaces on it: the runtime builds a NEW view per surface on every composition, so the new roots were never added and the previous composition's views stayed on screen. It splits into `EnsurePanelBuilt` (once) and `ComposeSurfaces` (every time). The format guard is correct and kept, and now names its refusal.

**THE LAW THIS ADDS: acceptance must drive the same path the user's finger does.** The z57 gate passed and the session broke anyway, because the gate drives launch arguments while the user drives a panel button.

Acceptance checks:

- A rendered-tier case asserts the rails ON SCREEN name the picked heroes; it failed with the demo six before the fix.
- A format press made while hosting draws a bounded sentence and leaves the composed setup unmoved.

Non-goals: no schema or wire movement; protocol /11 is unmoved from 8.35z60.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z62-the-transition-redraws-every-surface-and-a-refused-format-press-says-so-completion-evidence).


### [x] 8.35z63 Show a live effect on the board and let its aura travel with the hero

The user's directive: "when a hero has an active effect, we need to show it. Same purple color we use in hero tray to show active cards should higlight the effect target... If it is an active effect in radius, we need to paint the hexes in that radius that same color."

Observable outcome: a hero carrying an Active effect wears a violet ring on its board token, an effect with a radius tints the hexes of its disk the same violet, and the disk travels with its carrier's move rather than jumping at the end of it.

ONE COLOUR, already in the theme: `MatchHudTheme.ActiveEffectAccent` - milestone 8.5b's violet, worn by a card sourcing a live Active effect in the tray ever since - is now what the board draws too, with no second purple minted and no literal promoted. The two consumers differ only in alpha and by the board's own established idioms: a thin mark is added light, a whole-hex area is a tint so the artwork reads through it. DERIVED, NOT WIDENED - `ActiveEffectVisibility` reads only what `IMatchReadModel` already carries, so there is zero schema, wire or Core movement and every seat sees the same marks. The disk is centred on its carrier's CURRENT hex every time the board rebinds, which is rulebook p.17's "an Active effect's Radius moves with its hero" for free, because nothing is stored.

Acceptance checks:

- All 61 registered definitions across the eleven heroes resolve to a treatment - 40 ring, 18 disk, 3 both, 0 silent - walked from the catalog so a scope this layer cannot place becomes a red test rather than a hero rendering nothing.
- Every disk's exact hex set is proven offline against `RadiusQuery` rather than by hand, in the Unity-free harness.

Non-goals: no schema, wire or content movement; nothing animates on its own and no second animation authority is introduced. Sabina ships no Active effect at all, which is her printed set rather than a gap.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-835z63-show-a-live-effect-on-the-board-and-let-its-aura-travel-with-the-hero-completion-evidence).

### Queued corrective debt recorded at 8.35z23

Recorded here rather than in [TECHNICAL_RISKS.md](../TECHNICAL_RISKS.md) for a measured reason: that register stands at 4,491 words against a 4,500-word HARD limit, so the reassessment this finding deserves cannot be written there without an ownership review of the whole register, which is a milestone of its own and not this corrective's to spend. The finding belongs to `RISK-STATE-002`, whose reassessment chain should absorb it at that review.

- **A standing space-choice prompt can offer a Confirm that does nothing.** Recorded by milestone 8.35z48 from three instrumented Play Mode runs on `MisaProgressedUiJourneyPlayModeTests`' parked placement, and it is the true cause of the anomaly milestone 8.35z38 spent seven runs on. On that progressed staging the board click reaches the model and is accepted - the offered destination ring narrows from the whole lit set to exactly the space clicked - and the prompt then stands at `YOUR SPACE CHOICE - Choose 1 of 22 spaces` offering a Confirm labelled DONE which, pressed four times running through the shown control, changes nothing: same status, same single offered destination, same Confirm, the piece unmoved. `UiPlayer.DescribeBoardRay` rules the pointer chain out by measurement ("the ray passes through no hero's pick volume"), and a PROJECTED COMMAND behaves identically, which no input change can explain. Dash and Slash's parked walk on the same staging carries the same signature, so the two parked journeys are very likely one defect. **An offered control that does nothing is the class this project has fixed repeatedly (8.35z8's did-nothing report, 8.35z14's named blocker, 8.35z15's withheld repeat), and it belongs to effect resolution rather than to presentation input** - which is why 8.35z48, a presentation-input corrective, located it and stopped. Reproduction is now one filtered Play Mode run with the journey's own instrumentation. Proposed as its own corrective; not scheduled here.
- **The refused-restore rollback can fail on the recovery path.** `MatchSession.RestoreSnapshot` catches a failed restore and rolls the aggregate back by RE-DESERIALIZING the pre-image. When the state that failed is one the reader refuses, the rollback throws a second time, uncaught, and the session is left damaged where the session's own contract promises "restore refused, unchanged" - which is why milestone 8.35z22's reporting run logged a throw rather than a refusal. This is a mechanism `RISK-STATE-002` does not currently name: that row records the rollback as LOSSY for unpersisted state, not as ABLE TO FAIL. Milestone 8.35z23 removed the two reachable causes rather than the mechanism, so no repro remains to drive a hardening and none was invented; the candidates are carrying the pre-image as an object, or making a rollback failure non-throwing. Proposed as its own corrective; not scheduled here. **Discharged by milestone 8.35z43**, which took the first of the two candidates named above: the pre-image is carried as an OBJECT through the aggregate's existing capture/adopt pair, so the rollback is an assignment that cannot fail, and the same two lines harden the undo and restart reconstruction path whose catch carried the identical fragility. The reassessment of `RISK-STATE-002` itself still waits on that register's ownership review.
### [x] 8.36 Validate a private six-human internet 3v3 and run the friends playtest

User-approved acceptance amendment (2026-09-08): close this gate on the user's report of a clean, completed internet 2v2 on the old build the previous night. This replaces the original six-human 3v3 and additional live-case requirements for this closure; it does not assert those checks passed. Exact old-build identity, participant composition, diagnostic bundle and deliberate reconnect/latency coverage remain unverified. The later fixes build has local evidence only. No rules, implementation or publication authority changes; 8.36a remains a separate review.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836-user-accepted-internet-playtest-gate-completion-evidence).

Original gate definition retained below for traceability; the acceptance amendment above governs this closure.

Original dependency: Milestone 8.35c must pass the local six-client playtest-build rehearsal.

Observable outcome: Six humans on separate clients complete an invite-only full 3v3 match over the internet using a disposable private Win64 playtest build and primitive UI before production presentation begins.

Acceptance checks:

- The exact build and content/schema revision are recorded and privately distributed only to the invited testers.
- The session proves host/join, six stable seats, private hands, all normal player and team-owned choices, complete turns/rounds, reconnect/resync, match completion, and explicit failure handling across at least two external networks. At least one same-team tied Initiative and one confirmable Minion Battle removal are resolved through milestone 8.30n's public unanimous-agreement flows rather than a first-click, captain, timer, or automatic fallback.
- Host and clients converge on the same authoritative public outcome; owner-private projections and command authorization show no cross-player leakage or control transfer.
- The playtest includes the approved bounded latency/disconnect/reconnect cases and preserves a privacy-reviewed diagnostic bundle sufficient to correlate failures without exposing another player's hidden hand.
- Crashes, desynchronization, latency symptoms, confusing flows, unreadable state, and tester feedback are recorded and triaged without changing rules or code during the milestone.
- Any gate-blocking defect becomes a separately approved corrective milestone; the reviewed findings re-baseline Phase 9 HUD, prompt, feedback, and readability work.
- T3 online hard gate: focused network/session tests, a multi-client integration run, the private internet playtest, and both complete unfiltered Edit Mode and Play Mode suites pass at the applicable final revision.

Non-goals: No public release, dedicated production service, accounts, matchmaking, anti-cheat certification, competitive operations, or production presentation.

### [x] 8.36a Independently review the private-online implementation and playtest evidence

Dependency satisfied by the user-accepted 8.36 closure above. The user-authorized fresh-task review is complete; [findings and dispositions](PHASE_8_PRIVATE_ONLINE_REVIEW.md) remain open, with no corrective execution authorized. Review the available evidence and its explicit limitations; do not treat the acceptance amendment as proof of the original six-human/reconnect cases.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836a-independent-private-online-review-completion-evidence).

Observable outcome: A fresh analysis-only review correlates observed playtest failures and friction with the networking, authorization, synchronization, reconnect, diagnostics, and primitive presentation implementation before production polish begins.

Acceptance checks:

- The review examines actual session logs, test/build identity, tester observations, failure handling, host/client convergence evidence, reconnect/resync behavior, latency-sensitive choices, and relevant source paths.
- It checks private-information leakage, command authorization, stale/version-mismatched input, host-loss policy, diagnostic sufficiency, runtime stability, and whether symptoms indicate correctness, architecture, performance, operational, or presentation causes.
- Findings follow the common independent-review evidence contract; uncertain causes remain explicitly uncertain rather than being converted into unsupported fixes.
- Gate-blocking defects and later presentation/UX findings are separated into appropriately scoped proposed corrective or Phase 9 milestones.
- No code, rules, networking configuration, build, or presentation implementation changes during the review.

Non-goals: No second playtest, production hosting/service design, accounts, matchmaking, telemetry, public distribution, or bundled fix implementation.

Hard gate: Phase 9 does not begin until milestone 8.36a and every gate-blocking corrective milestone are reviewed and approved.

## Authorized private-game reliability corrections

The approved 8.36b-8.36g outcomes and closing checks live in [Private-game corrections](PHASE_8_PRIVATE_GAME_CORRECTIONS.md). The session brief owns the exact active route.

## Twelfth-hero dealing and the last deferred face

### [x] 8.37 Deal, register and player-path gate Tali the Spiritcaller as the twelfth hero

The twelfth hero this build deals, the second from the **Devoted Hero pack**, and the one that closes the package arc: with her row in the selector, every hero this build can compose is one a player can pick. Her dealing-prep branch was written offline at `17965187` and is squash-merged here; unlike the five dealings before it, this one also lands the **composition** milestone 8.35z32 deliberately left owed, because that milestone shipped only her Green line - five of nineteen faces - with the other thirteen carrying `PrintedTextComposition.DeferredUntil`.

Observable outcome: a player can pick Tali the Spiritcaller in the hero selector, see her printed card fronts and her own pick card, and play her set. She is the first verified hero whose faces place **tokens** on the board, the first to print a condition a card asks about **itself**, and the first above one star on p.21's complexity scale.

**TWELVE OF THIRTEEN DEFERRED FACES ARE COMPOSED HERE, and one predicate is why the number is twelve rather than five.** `synthetic-source-card-in-discard@1` - a predicate over the SOURCE card's own lifecycle state rather than over a hero's condition - closes seven faces at once: the three Winter Attacks read it for a dynamic +3, Spirit Wolf and Spirit Bear gate a modal bullet on it, and Guardian Spirit and Warrior Spirit pair it with `retrieve-card@1`. It is negatable because her own faces print both directions of the same question and a `Conditional` is structurally a then-branch with an optional else. **Blizzard alone stays deferred, and structurally rather than by budget**: its second, independent "End of turn: May repeat once" clause needs a repeat that re-invokes a full primary action from a timing trigger, and `ActionDefinition`'s constructor refuses an awaited `PrintedTextComposition` beside a non-null `EffectRoot`, so the face cannot ship half-composed. Milestone 8.37a owns it. Ice Shard stays parked on the standing p.20 Handicap deferral, exactly as every other hero's Handicap card does.

**ICE BLAST'S OWN DEBT WAS SMALLER THAN PHASE ONE COSTED IT, because a later dealing had already paid half.** Phase one recorded that nothing in this engine captures the colour a defender discarded at p.14 step 3. That stopped being true at milestone 8.35z53: `EnclosingAttackFacts.DefenseCardInstanceId` exists, added for Whisper's own `defended-card-forced-move@1`. The actual debt was one reader, `defended-card-color@1`, publishing `defended` and `color` as one node with two outputs so a `Conditional` and a sibling read the same answer - not the capture-and-carry plumbing phase one designed for. **Probe the current engine rather than trusting a prior phase's inventory** is the reusable half of this.

**THE REGISTRATION IS SIX NEW HANDLER TYPES PLUS HER OWN SIX TOKEN HANDLERS, and the arithmetic is stated rather than implied.** `synthetic-source-card-in-discard@1`, `friendly-hero-in-play@1`, `name-color@1`, `synthetic-choose-direction@1`, `synthetic-batch-direct-move@1` and `defended-card-color@1` are new TYPES; her six `tali-place-*`/`tali-reclaim-for-*` handlers are content of hers and register from her own file, the way Tigerclaw's Poison marker does. `hero-condition@1` and `borrow-primary-action@1` are found already registered - milestone 8.35z50 moved both across with Ursafar on [ledger row 25](../RULES_LEDGER.md#open-source-and-rules-questions)'s rule - so her dealing adds nothing for them. Her three token definitions join the one registered component set, which no hero before her has needed. She declares no Active effect at all, which is an explicit empty contribution: her only Active-effect keyword is Blizzard's `End of turn:`, and Blizzard is the deferred face.

**A STATIC-INITIALIZATION CYCLE, FOUND BY THE FIRST RUN AFTER THE FLIP AND NOT BY THE MERGE.** Registering her token handlers means `MatchContentCatalogs` reaches `TaliEffectContent` before anything reaches `TaliCardContent`, and `CompositionOwed`'s initializer keys a dictionary on that class's ids while that class's own initializer composes effects from this one. Entered from the new side, the CLR hands out a half-initialized capability id and 1,423 cases fail on one `NullReferenceException`. Making the dictionary lazy removes the edge in both directions; it is the only member of the type whose initializer reaches across.

Acceptance checks:

- Tali is pickable in the live hero selector, her pick card renders beside the other eleven, and a configured match composes her hand without throwing.
- Her nineteen faces and her token components pass the content-derived player-path gate under Unity, with Ice Shard and Blizzard declared unreachable and every other face journeyed.
- Her eighteen dealt fronts, her shine sprites and her two team tokens are prepared from the real Tabletopia corpus rather than generated placeholders, and the artwork bijection is re-pinned from measurement.
- The registered-handler count, every corpus digest and both scenario-digest families are re-pinned from measurement, and the schema version does not move.
- Her identity pin is minted from a complete unfiltered run and the eleven prior pins are byte-identical on it.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: Blizzard's End-of-turn repeat, which 8.37a owns; the controls her two runtime-integer decisions need, which 8.37b owns; Ice Shard, which stays on the standing p.20 Handicap deferral; no schema or wire movement; no new playtest package.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-837-deal-register-and-player-path-gate-tali-the-spiritcaller-as-the-twelfth-hero-completion-evidence).

### [x] 8.37a Compose Blizzard's End-of-turn repeat and close Tali's last deferred face

The one face milestone 8.37 could not ship, taken on its own terms rather than folded into a dealing that was already the largest of the six.

Observable outcome: Blizzard's whole printed text runs - the free direction and the simultaneous batch Move its two siblings already have, and then the "End of turn: May repeat once" clause that neither of them prints - so no dealt hero in this build has a face whose printed text is marked absent.

**WHAT IS GENUINELY NEW IS RE-INVOKING AN ACTION FROM A TIMING TRIGGER, not the checkpoint.** Silverarrow's End-of-turn discard proves the checkpoint half: a definition can declare an effect that runs at the turn's end. What it does not prove is running a full **primary action** there rather than a bounded effect, which is what "May repeat once" asks for. The optional half is p.10's ordinary May policy and needs nothing new.

**AND THE ANSWER IS THAT IT IS NOT A p.10 REPEAT AT ALL, which is a reading rather than a workaround and is recorded as one.** `EffectNodeKind.RepeatAction` is card-text machinery: it re-runs the enclosing ACTION from its own start, its bound is read off `CardTextRepeatChain`, and p.10's "you cannot repeat a repeated action" is enforced against it inside that action's resolution. There is no enclosing action at `TimingCheckpoint.TurnEnded` - the turn is over - so the node cannot express this clause. What ships instead is an End-of-turn Active effect on exactly Silverarrow's shape, whose triggered effect re-runs the SAME two nodes the printed Skill performs, once, under the card's own "May". **The consequence is named rather than left to be discovered: a card that forbids repeating an action does not read this, because nothing here is spelled as a repeat to the engine** - the same seam milestone 8.35z27's modal Attack chose deliberately in the other direction.

**THE GATE THE CLAUSE PRINTS NEEDED A RULING, AND IT IS OPEN QUESTION 34.** "If Blizzard is in your discard" is asked at the turn's end, when the card has been played and resolved - and this engine keeps a played card in `CardLocation.TurnArea` for the round, reserving `CardLocation.DiscardPile` for a card SPENT as a p.14 Defense or taken by a discard clause. On the narrow reading the condition is false by construction and the whole second sentence is dead text on every board. The wide reading is implemented and **opt-in at its one call site** - `SyntheticConditionCapabilities.CountResolvedPlayedParameter`, bound true by Blizzard's trigger and by nothing else, with an Edit Mode case asserting no printed face of hers binds it - so every earlier reading of the same predicate keeps the narrow one it wants.

**THE MILESTONE ALSO UN-PARKS BOTH JOURNEYS THE DEALING SHIPPED PARKED, and one of them found a shipped engine defect.** `GuardianSpirit_...` was `[Explicit]` for a three-turn arc the round could not hold; the diagnosis was wrong in a way worth recording - **playing a card never fills a discard at all**, so the arc could not have produced a candidate however many turns it was given - and the fix is one round with the enemy's Attack taking a card out of her hand as a Defense. `ReignOfWinter_...` self-skipped when p.8's waves stood no minion its subject could address; staging one beside her through `NextComposedSandboxStaging` is what the old remarks called a new staging and is in fact a parameter. Driving it then **threw**: `ArgumentException: A turn card-action event requires a defined card-action event`. p.13 gives a minion target no steps 3 to 5, so p.14's step-1 text resumes the whole rest of the page inside its own produced stream, and `TurnResolutionAuthority.MaterializeSteps` walked that produced stream with a smaller loop that knew neither a card-text step nor an `ActionEnded`. The same gap would also have dropped the `PerformedBasicAction` this very Ultimate reads. Only a face printing text at BOTH of an attacker's own moments can reach it, and only against a minion; Ice Blast is the first dealt content of that shape to be pointed at one.

Acceptance checks:

- Blizzard carries no `PrintedTextComposition.DeferredUntil`, and `TaliEffectContent.CompositionOwed` names only Ice Shard.
- A Play Mode journey drives the repeat: the direction is chosen, the batch resolves, the turn ends, the repeat is offered once, and declining it ends the turn cleanly.
- Her content revision and identity pin move together and are re-measured from a complete unfiltered run; no other hero's pin moves.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: Ice Shard, which stays on the standing p.20 Handicap deferral; no other hero's deferred text; no schema or wire movement.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-837a-compose-blizzards-end-of-turn-repeat-and-close-talis-last-deferred-face-completion-evidence).

### [x] 8.37b Give a named colour and a chosen direction a control a player can actually answer, and draw a placed token as its own piece

The user's own direction, on reading milestone 8.37's plan: "tali will need UI elements to ask other player for color choice", "the other player will need UI element to answe", and "make sure you use proper Token art". All three were real, and none of them was visible from the rules tier - the gate table, the journeys and the harness all pass on content that reaches a player as unanswerable buttons.

Observable outcome: the ally Commune with Spirits asks is shown six named, colour-tinted controls and answers on their own seat; the acting seat is told whose decision it is waiting on rather than being addressed as if the decision were theirs; Cold Snap's six directions read as directions; and Tali's Ice and her two Totems stand on the board as their own printed pieces.

**THE DEFECT IS OLDER THAN TALI AND SHE IS THE FIRST FACE THAT MAKES IT VISIBLE.** An `EffectValueKind.Integer` candidate has meant a printed NUMBER every time one has appeared - the kinetic "3 or 4 spaces" distance - so `RoundActionPresenter` labels one with its own digits and `SubjectOf` calls the decision a number. Her `name-color@1` offers six `CardColor` members and her `synthetic-choose-direction@1` offers six `HexDirection` members, and both reached a player as the buttons "0" through "5". Inferring the meaning from the values is what this build refuses elsewhere - `EffectChoiceCandidatePreview.ConsequenceIntent` exists so presentation can tell a removal from every other choice "without reading a capability name, card text, display name, target kind, or colour" - so the meaning is DECLARED: `EffectChoiceValueVocabulary` rides from the handler that opened the choice, through the request and the pause, to whoever draws it.

**THE WIRE MOVES AND THE SCHEMA DOES NOT, and the asymmetry is the whole design.** A joined seat draws its own controls, and Commune's decision belongs by construction to a hero who is not the acting seat - so on any real table the seat that must answer is a REMOTE one and the declaration has to cross: `guards-private-online/12` becomes `/13`. It does NOT cross a snapshot, because restore re-asks the registered handler for its candidate set and the handler re-declares the vocabulary on the way back out. Nothing persists it, so schema stays at v41 and no corpus digest, pinned constant or bare version assertion moves.

**THE ACTING SEAT WAS BEING ADDRESSED AS THE CHOOSER, and Whisper opened that gap first.** A pending choice reaches a seat's projection whether or not that seat owns it - authorization filter is not projection filter - and until Tali the two agreed in practice, because every effect choice belonged to the hero playing the card. `NarrationLabels.Own` would tell Tali's player "YOUR NAME A COLOUR" about a control they do not have. A decision owned by somebody else is now narrated as theirs, which also closes the same gap Whisper's bound-hero modal left at 8.35z53.

**THE TOKEN ART IS ONE PHYSICAL PIECE WITH TWO SIDES, and the first crop got it wrong.** `PlacedTokenView` has drawn every token as one flat disc since 6.2d and said why: "no catalog lookup and no per-definition art mapping". That was right while the only tokens were milestone 6.2's synthetic hazard and ward. The corpus names her three elements `Ice/Totem`, and opening both sprites explains the slash - the first rectangle, shared across all three, is a carved totem; the second, which differs per element, is a frozen hex printed with the `-1` hourglass that is exactly her Ice's own Initiative aura. One token, Ice on one side and Totem on the other, which is the same fact RULING 44 states from the rules side. The first pass read the shared sprite as a card back and wrote three ice faces under three names, two of them for totems; the tool now asserts the shape it depends on before it crops.

Acceptance checks:

- A colour choice renders six named controls tinted from the same palette the rest of the HUD draws those colours with, and a direction choice renders six named directions; neither renders a digit.
- The controls open on the CHOOSER's seat, and the acting seat's narration names the hero it is waiting on rather than addressing the viewer.
- A candidate outside its choice's declared vocabulary is a fault rather than a digit, and a node output bound into a closed vocabulary is refused at runtime when the produced value is outside it.
- Ice and both Totems draw their prepared faces; a checkout without the private corpus draws the generated disc exactly as before, and a pooled view re-bound from a token with a face to one without clears it.
- The wire moves to `/13` and the schema does not: no corpus digest, pinned constant or bare version assertion moves.
- Complete unfiltered dotnet harness, Edit Mode and Play Mode pass at the final revision.

Non-goals: no new hero content; no change to which decisions exist, only to how they are asked and answered; Blizzard stays with 8.37a; no second playtest package.

Historical evidence: [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-837b-give-a-named-colour-and-a-chosen-direction-a-control-a-player-can-actually-answer-and-draw-a-placed-token-as-its-own-piece-completion-evidence).

### [x] 8.44 Package the seventeen-hero private playtest build

Authorized 2026-09-18, immediately after 8.42c, in the user's own words: "After that carve out the new build." This converts the standing new-playtest-package request - recorded as owing a written milestone since the fourteenth-hero series - into a written milestone, and supersedes that note. Execute after 8.42c, not in place of any other work.

Outcome: a separately named current Windows build and matching ZIP under `Builds/`, carrying all seventeen dealt heroes, the 8.42 playtest refinements and 8.42c's awaited-responder narration, with the existing single `PLAY - Host or Join.bat` launcher, in-client Host/Join, a playtester README, and a build identity naming the source commit, Unity version, wire protocol, snapshot schema and configured content digest.

Acceptance: a packaging milestone is a pre-build regression gate and therefore a T3 trigger in its own right, so both unfiltered Edit Mode and Play Mode suites run at the exact packaged revision, with every skip and every established `[Explicit]` diagnostic enumerated rather than absorbed into a total. Private-resource checks must confirm that no extracted card art, map, rulebook, private log, build or worktree enters Git; the package itself legitimately carries prepared artwork resources, which is the existing local-artifact boundary and not a distribution. Run the required build-and-launch smoke through the actual packaged executable rather than a rehearsal script, and inspect the ZIP contents, the launcher and the identity file. Record measured suite totals and the executable and managed-assembly hashes in the completion evidence. Keep every prior package and all private evidence.

Non-goals: upload, remote operation, or public distribution of any kind; new heroes; a repeat internet playtest; the parked presentation program, tier-3 bots or the card-authoring workbench; and any rule, schema, wire or content-identity change.

Historical evidence: [8.44 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-844-seventeen-hero-package-completion-evidence).

### [x] 8.45 Package the undo-correction playtest build

Authorized 2026-09-18, in the user's own words: "Ok, go make a build now please." It carries the two undo corrections made after 8.44 - 8.42d's visible seat rewind and 8.42e's interaction wall - to the playtesters who reported the first of them. No new hero, content or protocol work is authorized by it.

Outcome: a separately named current Windows build and matching ZIP under `Builds/`, carrying all seventeen dealt heroes plus 8.42d and 8.42e, with the existing single `PLAY - Host or Join.bat` launcher, in-client Host/Join, a playtester README, and a build identity naming the source commit, Unity version, wire protocol, snapshot schema and configured content digest.

Acceptance: a packaging milestone is a pre-build regression gate and therefore a T3 trigger in its own right, so both unfiltered Edit Mode and Play Mode suites plus the complete offline harness run at the exact packaged revision, with every skip and every established `[Explicit]` diagnostic enumerated rather than absorbed into a total. The configured content digest must be MEASURED and compared against 8.44's rather than assumed unchanged; neither correction touched content, so a moved digest would mean something unintended shipped. Private-resource checks must confirm that no extracted card art, map, rulebook, private log, build or worktree enters Git. Run the required build-and-launch smoke through the actual packaged executable rather than a rehearsal script, and inspect the ZIP contents, the launcher and the identity file. Record measured suite totals and the executable and managed-assembly hashes in the completion evidence. Keep every prior package and all private evidence.

Non-goals: upload, remote operation, or public distribution of any kind; new heroes; a repeat internet playtest; the parked presentation program, tier-3 bots or the card-authoring workbench; and any rule, schema, wire or content-identity change.

Historical evidence: [8.45 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-845-undo-correction-package-completion-evidence).

### [x] 8.47 Announce each phase change with a banner

Authorized 2026-09-18, in the user's own words, three hours before an invited playtest: "Can you squeeze the letters into a build for tonight somehow?" - the "letters" being the transient phase banner they had specified minutes earlier: "text over the screen to let the players know game state changed. It pops up, then fades away after like a second or two", in blood red with a black outline on a shaded box, big, centre screen; round start bundled into the round's first commit banner; no banner at a turn's end; a banner for the Minion Battle. Cut as an interim package on top of 8.45, ahead of the decision window (8.46), which was mid-build on its own branch.

Outcome: `PhaseBannerPresenter`, a seat-scoped presenter whose `PhaseBannerCueDetector` decides from one projection snapshot to the next which transition just happened for this viewer - commitment begins (`ROUND 2 · TURN 1` over `COMMIT A CARD`), a hero's card begins resolving (`YOUR TURN` over `RESOLVE YOUR CARD` on the acting seat, `SABINA'S TURN` elsewhere), the end-of-round Minion Battle (`END OF ROUND 2` over `MINION BATTLE`) - and `PhaseBannerUnityView`, which attaches to the HUD root lazily, never takes the pointer, fades in, holds, fades out and detaches, every size a reference unit under `MatchHudPanelScaling`. The first observation primes and announces nothing, so a joining or reconnecting client is never told a state it did not watch change; every key shows once; a refresh carrying a whole turn's change announces where the game is now. Package `Builds/Playtest-8.45a-phase-banner` and its ZIP, stamped `1.0-8.45a-phase-banner`.

Acceptance: the detector proven on plain snapshots for every moment, both viewer readings, priming, repriming on a rebind, and the no-turn-end rule; the view proven to draw the words in the asked colours at the asked size and to take no pointer; a Play Mode case seeing the banner land centred over the HUD and take itself down; the presenter roster and the shared button treatment fixtures still green; the complete Play Mode suite, the complete Edit Mode suite and the complete offline harness green at the packaged revision, with every established skip enumerated; the built player reaching a configured match, and the packaged exe launching from its own folder. The source diff against 8.45 must touch only Presentation, tests, the harness exclusion list and the version stamp, which is what carries the configured content digest unchanged.

Non-goals: a banner at a turn's end; sequencing through the kinetic conductor ahead of the decision window (owed when 8.46 lands, so the two never draw over each other); upload or public distribution; any rule, schema, wire or content change.

Historical evidence: [8.47 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-847-phase-banner-completion-evidence).

### [x] 8.48 Bounce's repeat holds through Reload!

Authorized 2026-09-18 by the evening playtest's own report, in the user's words: "swifts gold didnt let me play her silver twice." Reload! (her Gold basic) borrows the primary action of her rightmost resolved card; Bounce (her Silver basic) prints "if this card is already resolved as you perform this action, may repeat once" - and the repeat never came.

Outcome: Bounce's `bounce-resolved` gate binds `countResolvedPlayed` true, the wide reading Blizzard's "in your discard" opted into at 8.37a: a card resolved this round sits in the turn area until the round ends, not in the discard pile, and Reload!'s pool names precisely such a card, so on the narrow reading the clause could never hold through Reload!. A borrow through Reload! now offers the repeat; Bounce's own turn still does not, because the card is mid-resolution. Swift's content revision moves to `swift-8-48-rev-1` with the artwork catalog and the identity pin.

Acceptance: `BounceBorrowedRepeatTests` drives a turn whose first slot holds Bounce resolved and whose second plays Reload!, takes the borrow arm, and asserts the repeat is offered and taken (the silver played twice); its twin drives Bounce's own turn and asserts no repeat. Complete offline harness, Edit Mode and Play Mode green.

Non-goals: Bullet Time's granted performance, which runs while Bounce is still mid-resolution in the engine's state and so offers no repeat - whether p.16's "when a basic card resolves" should count that card as already resolved is a ruling for the user, recorded in [HERO_SWIFT.md](../Rules/HERO_SWIFT.md).

Historical evidence: [8.48 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-848-bounce-repeat-through-reload-completion-evidence).

### [x] 8.49 A Defeat costs what the hero is worth

Authorized 2026-09-18 by the evening playtest's own report, in the user's words: "Lives are not being counted correctly. It just takes -1 from pool. It should take more and give more assit gold based on hero lvl, it is said so in the rulebook. And we need to show that on the map, next to hero level, show like half a coin and half a heart and a number in it, 1 2 or 3 depending on hero tier."

Outcome: rulebook p.13's "that hero's lowest card Tier" is read over the cards the hero is playing with - hand, turn area and discard pile - through one query, `HeroDefeatCostQuery`, that both the combat backend and the hero rail use. The whole-pool reading milestone 7.4 shipped also counted the Tier I cards p.15's upgrades return to the deck face up, so every hero read as Tier I at every Level and every Defeat cost one Life counter and paid one assist coin; p.13's table (Level 1-3 -> 1, 4-6 -> 2, 7-8 -> 3) now holds on a real progressed board. Beside each rail's level, a half-coin half-heart badge carries that number, with a hover that says what it pays and what it costs.

Acceptance: `HeroDefeatCostQueryTests` measures the defect (the returned Tier I card wins on the old reading) and the fix; `DefeatCostByTierTests` defeats a Tier II and a Tier III progressed hero on the staged boards and asserts two and three counters spent; `HeroRailPortraitTests` pins the badge beside the level. Complete offline harness, Edit Mode and Play Mode green, with every seeded walk the changed cost re-derives.

Non-goals: the killer's own coins, which p.13 already prices by Level; a badge on the board piece itself.

Historical evidence: [8.49 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-849-defeat-cost-by-tier-completion-evidence).

### [x] 8.50 The banner speaks only when you are up

Authorized 2026-09-18 after the evening playtest, in the user's words: "I dont think i want those banner letters we implemented for all the players. Just when a player is up. So only COMMIT A CARD and YOUR TURN + MINNION BATTLE i think." - confirmed after the session ("Yes, to it for point 1").

Outcome: `PhaseBannerCueDetector` announces a seat's own commitment, its own turn and the Minion Battle, and nothing else: the other seats' "<NAME>'S TURN" line of 8.47 is retired, and the Director, who is up for nobody, hears no turn banner at all.

Acceptance: `PhaseBannerTests` proves another hero's turn is silent while the viewer's next own turn is still announced, and that the Director still sees each commitment and the battle; `PhaseBannerPlayModeTests` unchanged and green.

Non-goals: any change to the letters' look, timing or the window it yields to.

Historical evidence: [8.50 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-850-banner-only-when-up-completion-evidence).

### [x] 8.51 Say when a seat held its card, and how END TURN was pressed

Authorized 2026-09-18 after the evening playtest, in the user's words: "It looks like Silverarrows blue got resolved without player input, like he clicked end turn but he didnt" - and, once the host log had shown that the player's own client sent one END TURN press composition (a Hold, then the release one round trip later) 1.1 s into his turn while nothing on disk could say what pressed it: "close this gap, we need to know for sure." The player had been alt-tabbing; a click landing on the live control and Enter or Space reaching a focused one were both possible and both unrecorded.

Outcome: three records that did not exist. `MatchLogEntryKind.CardHeld` - rulebook p.12's Hold performed by the seat's own command, derived from the `Turn.HoldActionPerformed` event the engine has always recorded - reaches the host's narration file as "<hero> HELD <card> - resolved with NO ACTION by the seat's own END TURN press" and the in-game tray as "<HERO> HELD <CARD> — NO ACTION"; the wire moves to `guards-private-online/16` (a version 15 peer refuses the frame; no bytes grew, schema v42 unmoved). `EndTurnPressProvenance` watches the drawn END TURN control ahead of the control's own handlers and, at every press, the runtime writes `GUARDS_INPUT: END_TURN_PRESS source=pointer|keyboard|pointer+keyboard|driver button=… key=… focused=… at=… version=… actor=… progressed=…` to the player's own log - UI Toolkit's Button answers a navigation submit exactly like a click, and the key that did it is named. `GameplayNarrationRecorder` no longer reports a numbering gap as a loss: a row keeps its rule-event sequence and most rule events derive no row, so a gap is a loss only when the window's oldest retained row was never written (the 2026-09-18 file carried 218 such reports, none real).

Acceptance: `MatchLogChannelTests` ties the Hold row one-for-one to its event on a driven walk and pins the deterministic line; `GameplayNarrationLogTests` pins the sentence and the corrected gap rule; `MatchFeaturePresenterTests`' closed-vocabulary walk and `MatchFormatAdmissionTests`' version ratchet are swept in the same change; `EndTurnPressProvenanceTests` covers every source, the consumed stamps, the view's driver reading and the marker; `EndTurnPressProvenancePlayModeTests` clicks the drawn control on the live panel and then submits it from the keyboard, reading `source=pointer` and then `source=keyboard key=Space`. Complete offline harness, the affected Edit Mode fixtures and the affected Play Mode fixtures green.

Non-goals: deciding what pressed END TURN on 2026-09-18 - no log from that night can; refusing keyboard submits on END TURN or any other change to how a press behaves; a package, since the record reaches players only in the next build they run; any rule, content or schema change.

Historical evidence: [8.51 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-851-hold-row-and-press-provenance-completion-evidence).

### [x] 8.52 END TURN answers the pointer and nothing else

Authorized 2026-09-19 by the user, relayed from the session that built 8.51: "have it make space and enter not end a turn." 8.51's provenance line proved how an alt-tabbing player ended Silverarrow's turn on 2026-09-18: a UI Toolkit Button presses itself on a `NavigationSubmitEvent`, and InputForUI maps Enter and Space to it for whichever control holds focus.

Outcome: the drawn END TURN control - live or greyed - is unfocusable, so the keyboard's navigation can never land on it, and a trickle-down submit handler stops any `NavigationSubmitEvent` before the Button's own handler runs. A mouse click and 8.51's press-provenance log line are exactly as they were.

Acceptance: `EndTurnPressProvenancePlayModeTests` presses the live control with the pointer (one press, `source=pointer`), then focuses it and dispatches a Space key and a navigation submit, and asserts the control is unfocusable, focus stayed elsewhere, and the press count is still one; `EndTurnPressProvenanceTests` unchanged. Focused Edit and Play Mode green; complete offline harness green.

Non-goals: any other control's keyboard behaviour; the tray's Confirm and Pass banners, which a keyboard may still submit.

Historical evidence: [8.52 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-852-end-turn-keyboard-completion-evidence).

### [x] 8.53 The victory screen

Authorized 2026-09-19 by the user from a mockup drawn over the 18 September playtest's own numbers: "I like this, implement it before the build. Everything else should disappear and this pops up." With two amendments in the same breath: no "Cards played" column, "Minions Killed" as the wording, and a "Times defended" column.

Outcome: when the match ends, the whole HUD gives way to one screen: the winner in big letters ("BLUE WINS", or "CUTTER WINS" for her solo victory), the win condition under it in words (the last Wave pushed, the Throne captured, the last Life counter spent, A Fistful of Coins), then a scoreboard - the winning team's block above the losing team's, each hero as its own token with name, level and worth, and the columns Kills, Deaths, Attacks at heroes, Times defended, Minions Killed, Coins earned. A Cutter solo win splits her out above her own crew, who show the same numbers with no share of the win. The numbers are the authority's: a per-hero match scoreboard accumulated where the log rows are emitted, carried in the snapshot and on the wire, so every seat sees the same board and a restored match keeps it.

Acceptance: the accumulator proven on the defeat, attack, defense, minion-defeat and coin facts and across snapshot restore and replay; the screen proven to show on every ending cause with the strip, rails, tray and window hidden beneath it; a Play Mode journey to each ending; no controls of its own, leaving staying the lobby surface's.

Non-goals: per-round breakdowns; a replay viewer; changing what ends the game.

Completed 2026-09-19 in two halves on one branch. The authority: `MatchScoreboard` / `HeroScoreboardEntry` (Kills, Deaths, Attacks at heroes, Times defended = attacks on the hero that were successfully defended, Minions Killed by attack or card text, Coins earned = collected over the match, not held; Level stays the progression's) owned by `RoundResolutionAuthority`, folded at `MatchAggregate`'s commit boundary and by `HeroCoinLedger.BindScoreboard`; snapshot v42 -> **v43** (+82 bytes on all sixteen corpus captures, v42 rejected rather than migrated); wire `/16` -> **`/17`** (`NetworkScoreboardRow`, four pins moved); `IMatchReadModel.Scoreboard`; `MatchScoreboardTests` (9). The screen: `VictoryScreenPresenter.Derive` over `Outcome.HasEnded`, `VictoryScreenUnityView` (`gd-victory-screen`) composed last by the HUD renderer, which hides every other root child while the screen stands and writes their display back if it ever comes down; `VictoryCauseLabels` shared with the status line; the 8.49 worth badge shared through `HudElements.WorthBadge`; the best value per column marked, Deaths never. No controls: the lobby's LEAVE stays reachable. `VictoryScreenPresenterTests` (15 cases), `VictoryScreenUiJourneyPlayModeTests` (the last Life counter through `UrsafarAuditStaging`), the Cutter solo journey extended. Suites at `87dacf6c`: complete Edit Mode 4396 / 4360 passed / 0 failed / 36 established skips, complete Play Mode 629/629 with zero skips, harness 3425/3425 with zero skips.

Historical evidence: [8.53 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-853-victory-screen-completion-evidence).

### [x] 8.54 Bounce's repeat through Bullet Time

Ruled by the user 2026-09-19: "Bounce through Bullet Time counts as already resolved. Bounce moves 2, the card resolves, the Ultimate grants the primary on the resolved card, and that performance offers the repeat. Three moves of 2." The Ultimate's granted performance must see Bounce as resolved; `GrantedTargetExclusionTests.BulletTime_PerformsThePrimaryOfTheBasicCardItJustResolved` becomes the witness of three moves and HERO_SWIFT.md closes SW-Q3.

Completed 2026-09-19 (`2f700803`, rebased onto main after 8.57h). THE CAUSE WAS AN ORDERING, NOT A READING: 8.25e commits a card's `CardResolutionStatus` below the `ActionEnded` checkpoint on purpose (an action the turn holds open still has an unresolved card and a selected actor to run on), and Bullet Time grants its instance AT that checkpoint - so p.16 had resolved the card while the state had not yet said so, and Bounce's gate (`synthetic-source-card-in-discard@1` under 8.48's `countResolvedPlayed`) read the state: measured `TurnArea / HasBeenPlayed / Unresolved` at the granted performance. The predicate now also asks WHICH PERFORMANCE IS RUNNING: `BasicActionQuery.IsPerformedOffACardAlreadyResolved` (`PerformedBasicAction.cs`), an exhaustive switch beside 8.38c2's `IsCardResolution` and deliberately not its negation - `TextGranted` true (every printed borrow or grant names an already-resolved card), `Repeated` false (8.25a/8.25e run p.10's repeat while the card is unresolved), `Reaction` false, an unclassified context throws; `EffectCapabilityContext.InvocationContext` (a new optional property, default `None`) is passed by `EffectResolutionSession.BuildCapabilityContext` and `AuthoritativeSnapshotSerializer.DeriveCapabilityCandidates`; `SourceCardInDiscardHandler` widens only inside the opt-in. No schema or wire move (`PendingResolution.InvocationContext` was already persisted): v43 and `/17` untouched; `VerifiedContentIdentityTests` byte-identical, no digest moved. Blast radius named: `countResolvedPlayed` has exactly two bindings - Bounce's and Tali's `blizzard-repeat-gate`, whose frame runs under `PrintedPrimary` with the card genuinely `Resolved` at `TurnEnded`, so its answer cannot move; the four other `synthetic-source-card-in-discard@1` sites bind only `negate` or nothing; `BorrowedPrimaryActionQuery.EligibleCards`, `ResolvedOrderFor`, `CountsAsActive`, `CardLifecycleAuthority` and `SameEnemyHeroOncePerTurn` untouched. Invariants measured: Bounce's own turn two moves and no repeat; Reload!'s borrow exactly one repeat; the grant's repeat offered once - three moves of 2 taken, two declined; negative control: reverting only the widening fails four of the five new turn-level cases and leaves 8.48's fixture green. Tests: the witness at three moves with `bounce-repeat` in the trace, the repeat offered and declinable, plus its refusal twin; `BounceBulletTimeRepeatTests` (8: the ruling through a turn, declined, her own performance still offered nothing, the repeat's question equal to the straight-line spaces two away from where the grant left her, a board pared to one file, and on a composed `MatchSession` snapshot/restore paused inside the grant inside the trigger, retained-prefix Undo with byte-identical replay, a staged seeded walk that takes the repeat and plays on); `BasicCardResolutionTriggerTests` +3; the Swift Bullet Time journey answers the repeat in the decision window through `UiPlayer.ClickEffectAnswer` and measures three legs of exactly two spaces, its twin refuses and measures two; `ConfiguredMatchFixtures.CreateEngagedStagedSwiftTierThreeSession`. FINDING: a board where the repeat has no legal space cannot be staged - every performance is an exact two-space walk down a file whose reverse is complete by construction, so the repeat's question is never empty (proved instead on the one-file board: the offer is exactly one space, the one the grant came from). Gate table shapes unchanged; Bounce's `RepeatDecision` row had declared the Bullet Time journey as its proof and NO journey had ever exercised it - now two do. On the branch: probe 0 errors; focused Edit Mode 41/41 (identity 2/2, corpus 3/3); focused Play Mode 24/24; complete Play Mode 632/632 with zero skips (631 + 1). The branch's own complete harness and complete Edit Mode attempts starved under two Editors and two harnesses at once and were re-run after landing: harness 3631/3631 on main with the block landed (3619 + 12 new cases), 13 m; complete Edit Mode 4608 / 4572 / 0 / 36 established skips, on main with 8.54 and 8.55 landed.

Historical evidence: [8.54 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-854-bounce-through-bullet-time-completion-evidence).

### [x] 8.55 The initiative badge reads card plus item

Asked by the user 2026-09-18 with a screenshot: the hero rail's revealed-card initiative badge must read "6+1" - the printed Initiative and what the item adds - rather than the summed 7 that 8.42a draws.

Completed 2026-09-19 (`215135f8`, rebased onto main after 8.54). `HeroCurrentCardViewModel` (`HeroOverviewPresentation.cs`) carried one combined `int? initiativeWithItems`; it now carries the two numbers as data - `CardInitiative` (printed) and `ItemInitiativeBonus` (what p.16 adds), plus `CardInitiativeLabel` - and composes `InitiativeLabel` from them: `6+1` with one item, `6` with none, empty facedown; a facedown commitment handed either number throws, so the no-leak boundary is the type's, not a caller's discipline. The caller, `CurrentRoundCards.CurrentCard`, already read both halves and handed over their sum; it now names them separately - the printed total from `PrintedValueQuery.CalculatePrintedInitiative`, the addend from a new `ItemBonusQuery.ProjectedItemBonus`, where p.16's matching-icon condition already lived (`TotalWithProjectedItems` now delegates to it), so nothing subtracts a printed total back out of a sum. The assistive reading is `RESOLVING - Initiative: Card 6 + item 1` (plainly `Card 6` with none). Geometry measured live at 2560x1440, 1920x1080, 1280x720 and 2560x540: the marker keeps its 46x52 footprint exactly and only the type gives, by one rule (`HeroOverviewsUnityView.InitiativeBadgeFontSizeFor`: 20 for one or two characters as since 8.7a, 18 for three, 14 for four) - at 20 a three-character reading drew 40 of the 42 units inside the border and four characters drew 50. Two live findings: the staged card is Initiative 11, so the real reading is `11+1` - four characters is the ordinary case, not a defensive one; and 8.35z11's motion re-eases the rail whenever it re-enters, so the marker measures 31/13/6 units of its 46 a few frames after a viewport change - the first complete run photographed a squeezed badge, and the fixture now waits for the marker to BE its footprint before it reads and photographs. Captures for the user in `tmp/855-captures/` (the 1920x1080 rail and a 6x zoom reading `11+1` centred with margins both sides). Tests: `MatchHudPresentationTests` (base-only, +1, +2, facedown, the assistive text, the throwing facedown boundary); `HeroTrayRefinementTests` asserts the badge on the live rail after reveal, the plain reading, and the reading surviving a rebuilt presentation, and takes both captures from one photograph. The two-item reading `6+2` is proven by composition and by the live width measurement, not by a match that earned two Initiative items (no short fixture reaches that). No schema, wire or content-revision move; `MATCH_HUD.md`'s rail paragraph corrected here (it said a one- or two-digit label absolutely fills the fixed marker). On the branch: probe 0 errors; harness 3619/3619; focused Edit Mode 153/153; focused Play Mode 4/4; complete Play Mode 631/631 with zero skips; complete Edit Mode 4596 / 4560 / 0 / 36 established skips. On main with 8.54 and 8.55 landed: harness 3631/3631; complete Edit Mode 4608 / 4572 / 0 / 36 established skips; the combined complete Play Mode is the gate of 8.60.

Historical evidence: [8.55 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-855-initiative-badge-completion-evidence).

### [x] 8.56 Package the victory-screen playtest build

Authorized 2026-09-19 by the user: "Cut out the build after that end" (said of 8.46's landing) and, once the victory screen was ordered, "implement it before the build." The build carries 8.46 through 8.53: the decision window with the level-up screen inside it, Bounce's repeat through Reload!, defeat costs and assist coins by Tier with the worth badge on the rail, the banner only when you are up, the END TURN keyboard fix and the victory screen.

Outcome: `Builds/Playtest-8.53-victory-screen` and its zip, built from main at the 8.53 tag by the 8.44 recipe (bundleVersion `1.0-8.53-victory-screen`, `Invoke-PlayerBuildSmoke.ps1`, the launcher batch, READ ME FIRST, PACKAGE MANIFEST, BUILD IDENTITY), launched once from its own folder.

Acceptance: T3 - the complete Edit Mode suite, the complete Play Mode suite and the harness green on the packaged commit before the build is cut; the packaged exe launches and reaches the lobby; the identity stamp names the commit.

Non-goals: new gameplay; the 8.54 and 8.55 fixes, which wait behind the package.

Completed 2026-09-19. `Builds/Playtest-8.53-victory-screen` and `Builds/Playtest-8.53-victory-screen.zip` (186 entries, no logs, no debug folder, 67 MB), built from main at `11e8cf3d` (tag `milestone/8.53`) with bundleVersion `1.0-8.53-victory-screen`, stamp `1.0-8.53-victory-screen+20260919024107-789e12b0`; protocol `guards-private-online/17`, schema 43, configured content digest `ff41228b...` re-measured and unchanged. The gate: complete Edit Mode 4396 / 4360 passed / 0 failed / 36 established skips, complete Play Mode 629/629 with zero skips, harness 3425/3425, the build-and-launch smoke, and the packaged exe launched once from its own folder to `LOBBY_BEGIN`. Manifest, READ ME FIRST (what is new: the decision window, the victory screen, defeat costs, the banner, Bounce, END TURN; known problems updated) and BUILD IDENTITY written by hand per the 8.44 recipe.

Historical evidence: [8.56 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-856-victory-screen-package-completion-evidence).

### [x] 8.58 The decision window shows the card and names its choices

Asked by the user 2026-09-19 morning, on three screenshots of the packaged build: "This sucks. We said we will show the card, and have nice labels on the buttons? This box is a waste of space like this." and "Old buttons here instead of new ones." The 2026-09-18 review settled exactly that - "the card(s) in question, one button per choice", "card + one button per printed bullet", "two buttons whose labels name the consequence, never ACCEPT/DECLINE" - and 8.46 shipped the mechanics without it: a modal read `MODE #1 RELOAD-BORROW-ARM`, an optional step read "DO IT?" over pewter ACCEPT / DECLINE banners, no card face, and the expanded window an inset of the whole playfield.

Outcome: every expanded card-text question shows the asking card's face beside a content-sized centred panel; every choice arm and optional step of every dealt hero carries a label from its printed text, proven by a corpus test that names each missing pair, so a seat never sees a node id; the optional-step arm uses the window's own controls.

Acceptance: presenter tests for the face, the title and the labels on each family; the corpus label test green over all dealt heroes; geometry inside the 8.46 bound at 1280x720 and 2560x1440; Play Mode journeys through Swift's Reload! modal and Bounce's repeat showing the face and the printed labels; the complete Play Mode suite green.

Also in scope, from the same review: the worth badge grows and its digit gets a legible treatment ("Hard to see this number one here"); the Minion Battle window loses its pewter verb, its repeated line and the old banner peeking out behind the docked bar ("Buttons again, when maximised it makes no sense for the window to be this big and this empty"); the tray stays drawn while the window is docked ("hand should not disappear on these choices, it makes this bar float strangely in the air"); and the defense window shows the attacker's card with its attack total and each candidate's defense and whether it holds ("This is not showing the attack card, nor our defense breakdown popup"); the victory screen's columns become a fixed grid whose last column fits the panel ("Text needs to be aligned better to columns, and coins earned is going out of the table"); and every new window is captured at three resolutions and checked before the block closes ("Just check all new windows, that seems to be where all the issues are"). The 2026-09-18 mockup is the spec.

Non-goals: docked geometry; direction on the board; the level-up window's content.

Completed 2026-09-19 (`1a1e6bb6`, the branch rebased onto main at 8.57g and fast-forwarded). THE CARD WAS MISSING FOR ONE REASON: the window read the asking card off `PendingSkillResolution`, which a network seat reports as null on purpose (8.40f's privacy arm), so every client in the playtest had no face and no card id to key a declared label on - the 125 label declarations 8.46a demanded were all there. `IMatchReadModel.ResolvingSourceCardInstanceId` now publishes which card is resolving (Skill, then Attack, then Movement frame, then the acting hero's selected card for p.14's own text); no wire, schema or protocol move. The expanded rectangle 8.46c stated (rail to rail, under the strip, above the tray) is now the BOUND and the panel is sized to its content and centred inside it (a `may` about 700 units wide, a `Choose one` about 900, a four-candidate Defense about 1600, at all three viewports); the scrim is gone (a transparent click-shield only); the body row is centred with equal side padding. One control style: `HudElements.Control` with the shared motion everywhere in the window, the leading answer told apart by a gold border, `BuildVerbBanner` deleted; a card's bullets and a `may`'s two answers stack beside the card and fill its height; the strip's own banner lane is hidden while the window owns the decision; `MODE #n <node-id>` demoted to a Director diagnostic, a seat falling back to "OPTION n", the title of a card-text question the card's own name. The docked bar is the mockup's: one row, the question on one line, the count short by construction, the verb drawn and disabled until the count is met. The hand stays drawn (blocked from answering) docked and expanded, except that the expanded level-up hides the tray and draws the hero's hand as the 8.46g preview inside the panel under the candidates (the user's call: "shown below the offer window, like it was before"). Defense: the attacker's card on its own side with its total and Range as a gold chip, each candidate its printed Defense and `HOLDS` where the authority's preview says so, the decline a quiet corner control, the old per-card hover breakdown kept (the user withdrew an inline modifiers section). The victory table is a fixed grid (1480 units of columns inside a 1472-unit block was the one arithmetic mistake behind the drift and the clipping). The worth badge is 22 units with a black-outlined digit filling the disc, no backing disc, ink-centred; the strip's team life counters are a red and a blue heart with the count inside, on the text line, no words, no dot. Nineteen journeys that pressed a label off the strip's model now press through `UiPlayer.ClickEffectAnswer`. Every family captured at 1280x720, 1920x1080 and 2560x1440 against its mockup frame (`tmp/858-captures/VERDICTS.md`), reviewed by the coordinator and by the user before the merge. Harness 3619/3619 on the rebased branch; focused Edit Mode 265/265; complete Play Mode 631 on the rebased branch - 628 green and three reds that were only Rowenna's git-ignored prepared artwork absent from that worktree (the picker's overview and a shine slot), both fixtures 28/28 once the artwork was synced there and into the main checkout; the pre-rebase complete run's one red, Whisper's Swift Justice journey, was 8.57e's defect and is fixed at 8.57g.

Historical evidence: [8.58 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-858-decision-window-shows-the-card-completion-evidence).

### [x] 8.59 Bounce offers only the spaces it can reach

Asked by the user 2026-09-19 morning on a screenshot of Swift's Silver: "why are unavailable spaces offered for this silver? It should just offer legal spaces 2 away, not this space 1 away which is illegal." Bounce's line picker is bound "up to 2" while its move is "exactly 2", so the picker offers 1-away spaces the move can never take.

Outcome: `choose-line-destination@1` takes an optional minimum distance, Bounce binds 2, and every other binding of that picker is swept against its printed text ("up to" bindings unchanged).

Acceptance: an offer test proving Bounce's candidates are exactly the reachable distance-2 line spaces and nothing 1 away, the p.11 stop when none exists, the repeat's second offer; the sweep table recorded; Swift's revision, identity pin and artwork row moved together; harness, focused Edit Mode and the Bounce journeys green.

Non-goals: any card printing "up to".

Completed 2026-09-19 (`f6f651d5`). `choose-line-destination@1` gains an optional Integer `minimumDistance` (unbound or 0 reads as the floor of one the walk already had, so no earlier body's candidate set moves; the floor sits on the OFFER because by the time the Move reads a named space the space has been named). The sweep of every binding against printed text corrected two faces: Bounce (max 2 -> floor 2) and Silverarrow's Wild Hunt ("you may move 2 spaces" - the "may" governs whether, not how far; its Move bound no least distance, so the short walk was performed, not refused). Every "up to", enumerated ("1 or 2", "1, 2 or 3") and RULING 37 binding is unchanged; the charge-line family already binds both bounds. Revisions `swift-8-48-rev-1` -> `swift-8-59-rev-1` and `silverarrow-8-35z46-rev-1` -> `silverarrow-8-59-rev-1`, each with its eighteen catalog rows and identity pin measured on a complete run (swift `147f9266...`, silverarrow `7550446c...`); the other fifteen pins, sixteen corpus digests and both scenario-digest families byte-identical; shine sprites key on content id, local id and slot, never the revision. `BounceOfferTests` (9) and three turn-level cases on `BounceBorrowedRepeatTests`. Harness 3563/3563; focused Edit Mode 350/351 (the one red is the pre-existing `ConfiguredCardArtworkCatalogTests` case that reflects on `RowennaCardContent`'s missing `DealtDefinitions` - owned by 8.57f); focused Play Mode 43/43 with both tightened journeys.

Historical evidence: [8.59 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-859-bounce-offer-completion-evidence).

### [x] 8.60 Package the eighteen-hero playtest build

Authorized 2026-09-19 by the user ("Previews look ok, merge 8.58 and proceed"), then ordered AFTER the two open fixes when a package was cut early at 8.57h: "No need. Just finish everything and then do the build." (that package was withdrawn, its commit and tag removed). The build carries 8.57-8.57h (Rowenna the Vanguard, the eighteenth hero, dealt with her real artwork and gated), 8.58 (the decision window per the mockup, the worth badge, the strip's hearts, the victory grid), 8.59 (Bounce and Wild Hunt offer only the spaces they can reach), 8.54 (Bounce's repeat through Bullet Time) and 8.55 (the initiative badge reads card plus item).

Outcome: `Builds/Playtest-8.55-rowenna` and its zip, built from main at the later of the 8.54 and 8.55 tags by the 8.44 recipe (bundleVersion `1.0-8.55-rowenna`, `Invoke-PlayerBuildSmoke.ps1`, the launcher batch, READ ME FIRST, PACKAGE MANIFEST, BUILD IDENTITY), launched once from its own folder.

Acceptance: T3 - the complete Edit Mode suite, the complete Play Mode suite and the harness green on the packaged commit before the build is cut; the packaged exe launches and reaches the lobby; the identity stamp names the commit; Rowenna pickable in the packaged lobby with her pick card and both tokens.

Non-goals: new gameplay beyond the blocks named; a build before 8.54 and 8.55 have landed.

Completed 2026-09-19, after 8.54 and 8.55 landed - the user's order ("Just finish everything and then do the build"). `Builds/Playtest-8.55-rowenna` and `Builds/Playtest-8.55-rowenna.zip` (185 entries, no logs, no debug folder, 69 MB), built from main at `cdbf4d71` (tag `milestone/8.55`) with bundleVersion `1.0-8.55-rowenna`, stamp `1.0-8.55-rowenna+20260919163202-9d3386dd`; protocol `guards-private-online/17` and schema 43 unchanged; the configured content digest re-measured at this commit is `36da22a7...`, moved from `ff41228b...` by Rowenna's registration (her eighteen definitions and two placement capabilities), so this build refuses every older copy at the join. The gate on the packaged tree: complete Edit Mode 4608 / 4572 / 0 / 36 established skips; complete Play Mode 661/661 with zero skips; harness 3631/3631; the build-and-launch smoke (GATE PASSED); the packaged exe launched once from its own folder to `LOBBY_BEGIN`. Manifest, READ ME FIRST (what is new: Rowenna and her three cross-seat questions, the rebuilt window, the hearts, the badge, the victory grid, the Bounce and Wild Hunt offers, Bounce through Bullet Time, the "6+1" badge; known problems: Rowenna unplayed by people so far, the window checked on screenshots, the two same-day fixes) and BUILD IDENTITY written by hand per the 8.44 recipe.

Historical evidence: [8.60 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-860-eighteen-hero-package-completion-evidence).

### [x] 8.62 Targeting cues read as blood-red light

Authorized 2026-09-19 by the user, from a screenshot of an attack's targets: "can you make targeting highlights more visible, they are almost impossible to see like this.. Blood red light instead of this pink i think."

Outcome: a hero or minion that is a legal target of an attack, a removal or a card-text choice is unmistakable at every supported viewport - the piece cue reads as a strong blood-red (or, for a non-harmful effect, its own consequence colour) ring and wash rather than the faint pink tint the 18% wash produced over a portrait - with the selected and hovered states stronger still, and the tile's corner marks no weaker than before.

Acceptance: `BoardTargetRing`'s eligible, selected and hovered washes and ring widths re-tuned and re-pinned in `PrimitiveHeroViewTests` with the user's words as the rationale (the old "never exceeds the transparent-overlay cap" pin retired on the user's direction); the attack-target corner marks and their halo kept or strengthened, never weakened; before-and-after photographs of a hero and a minion as attack targets and of a hero as an effect target, at 1920x1080 and 2560x1440, written under `tmp/862-captures/` by a batch-mode Play Mode fixture that stays as the cue's permanent photograph; every journey asserting a cue state still green; the complete Play Mode suite with zero skips. The user approves the photographs before the block completes.

Non-goals: the board's team rings, hex corner geometry for movement or respawn, the attack spotlight.

Completed 2026-09-20 (`c9c615ec`, squash-merged from `2058ddd8` on `8.62-hud-cues`; the user approved the final photographs at 13:10: "That's it. This is the one."). THE USER REDIRECTED THE DESIGN ACROSS SIX PHOTOGRAPH ROUNDS, and the outcome above is superseded by their calls: the thick ring plus 40-60% wash was "coloring on the inside is a bit too much"; the original ring in solid red was "not the ring"; an always-on full hex outline was liked as a shape but pale in colour; a 2x hovered edge was "too thick"; the settled cue is the ORIGINAL six corner marks on the target's TILE, painted solid by SIDE - blood red (194,10,24) on an enemy unit, `AffectColor` green on a friendly one, full opacity over a rim of the same hue 0.55 towards black - with NOTHING on the token (no ring, no wash), and the marks closing into the whole hex edge (`OutlineCoreHalfWidth` 0.055) when the pointer is on the target, on the piece or on the sand. Colour by side rather than by intent differs from the shipped palette in exactly one reachable cue: a card-text effect naming an enemy unit (Shock's rider) was green and is now blood red. `BoardTargetCuePresentation.Treatment` is the one line that chooses; `RingOnThePiece` (pre-8.62 ring shapes, wash alphas 0, wash mesh not built) and `OutlineTheWholeHex` stay whole behind it, off; `GUARDS_CAPTURE_PHASE=ring` photographs the ring. Pins: `HexCornerHighlightTests` gains the outline case; `PrimitiveHeroViewTests` states the three zero wash alphas, the empty wash submesh, a keyline-only inner boundary and retires the overlay-cap pin on the user's direction; the template's bounded native set 20 -> 26 in `HexCornerHighlightPlayModeTests` and `MinimalHexGridPlayModeTests`; every fixture that pinned a piece cue (Arien x2, Brogan x2, Sabina engaged, Wasp engaged, minion correction, placed token, minimal grid, `StandardMinionViewTests`, `PlacedTokenViewTests`) states it through `BoardTargetCuePresentation.AsDrawn(...)` so it follows the switch; no pinned colour moved. The permanent photograph fixture `TargetingCuePhotographPlayModeTests` writes hero / minion / enemy-effect / friendly-swap targets, resting and hovered, at 1920x1080 and 2560x1440 under `tmp/862-captures/` (the Director sandbox stands the heroes adjacent; the Wasp engaged mirror gives the minion, the rider and the friendly swap). Two fixture defects the photographs exposed and fixed: the phase banner was still fading across early frames (dismissed through its own `Hide()`), and hovered captures were pixel-identical to resting ones because pointer input clears the hovered space on its own `Update` (the board's `SetHoveredCoordinate` is now set with no frame between it and the shutter). Schema and wire unmoved; complete Play Mode 666/666 with zero skips (`tmp/862-play-ship-results.xml` in the branch worktree); complete Edit Mode 4877 / 4841 / 0 / 36 established skips (`tmp/862-edit-ship2-results.xml`); harness 3898/3898 on the branch's base.

Historical evidence: [8.62 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-862-targeting-cues-completion-evidence).

### [x] 8.63 The decision window is drawn above every other panel

Authorized 2026-09-19 by the user, from a screenshot of a defense window partly covered by the defense-preview panel: "popup should be on top, not behind stuff :D".

Outcome: whenever the decision window is open, expanded or docked, nothing else in the HUD draws over it - not the defense preview, the hero overviews, the log drawer, the phase banner, the attack spotlight or a hover preview - and the window's own hover tooltip stays inside it.

Acceptance: the window's host is the topmost element of the HUD's overlay layer whenever it is shown (brought to the front on every open and on every later sibling addition, or hosted in a dedicated top layer), proved by a Play Mode assertion over the live hierarchy for each family the 8.58 capture fixture stages plus the defense window with its preview panel open; the 8.46 rule that the phase banner yields to a window opening on the same refresh preserved; before-and-after photographs of the defense window over its preview panel at 1920x1080 under `tmp/863-captures/`; the complete Play Mode suite with zero skips. The user approves the photograph before the block completes.

Non-goals: the preview panel's own layout, the window's content or controls.

Completed 2026-09-20 (`4feec1c4`, squash-merged from `ef19c060` on `8.62-hud-cues`; the user approved the photograph: "Victory screen and defense breakdown are good"). The window is topmost whenever shown, proved by `DecisionWindowTopMostPlayModeTests` over the live hierarchy for every family the 8.58 capture fixture stages plus the defence window with its preview open; the 8.46 banner-yields rule kept. THE USER'S SECOND CALL ON THE SAME SCREENSHOT ("I think spacing is a bit off here" - "I am talking about defense breakdown") found the breakdown's rows drawn on top of one another: the cause was `LayoutPreview`'s flat 240-unit `maxHeight` with every `gd-defense-contribution-row` keeping UI Toolkit's default `flex-shrink: 1` while `HudElements.Text` labels are `flex-shrink: 0`, so a breakdown taller than the cap squeezed its rows to nothing and the labels painted over the line below. The panel is content-sized now (`CalculateDefensePreviewHeight` is the same arithmetic the drawing performs, line for line), every line is `Rigid` (declared height, no shrink, no grow), the name clips at the panel's edge rather than pushing its value off the row, and `SetPreviewOrigin` lets the breakdown drawn INSIDE the window cover the same panel-space rectangle it did outside it; `AssertTheBreakdownReadsAsLines` pins the order, non-overlap and heights. Photographs under `tmp/863-captures/` (1280x720, 1920x1080, 2560x1440). Schema and wire unmoved; suites as 8.62's (one branch, three commits).

Historical evidence: [8.63 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-863-decision-window-topmost-completion-evidence).

### [x] 8.64 The victory screen drops its column legend

Authorized 2026-09-19 by the user, from a screenshot of the legend strip under the victory tables ("Level p.15's Hero Level as the match ended - Kills ... - Worth what defeating this hero pays and costs"): "Remove this from victory screen."

Outcome: the victory screen shows the winners' and the defeated blocks with their column headers and numbers and nothing explaining the columns; the space the legend took goes back to the tables.

Acceptance: `VictoryScreenModels.Legend` and its `VictoryScreenLegendEntry` rows retired from the view (and from the models if nothing else reads them), the 8.53 fixtures that asserted the legend re-stated to assert its absence, the 8.58 capture fixture's victory photographs re-taken under `tmp/864-captures/` at 1920x1080 in both variants (team win, solo win), the complete Play Mode suite with zero skips. The user approves the photograph before the block completes.

Non-goals: the columns, their numbers or their headers.

Completed 2026-09-20 (`de886e51`, squash-merged from `bc270186` on `8.62-hud-cues`; the user approved the photograph). `VictoryScreenModels.Legend` and its rows retired from the view and the models; the 8.53 fixtures that asserted the legend assert its absence; the column headers sit over their numbers in both variants (the user's "spacing" remark was first read against these columns and their alignment was checked in the same pass); `VictoryScreenPhotographPlayModeTests` writes team-win and solo-win photographs under `tmp/864-captures/` at 1920x1080. Schema and wire unmoved; suites as 8.62's.

Historical evidence: [8.64 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-864-victory-legend-dropped-completion-evidence).
