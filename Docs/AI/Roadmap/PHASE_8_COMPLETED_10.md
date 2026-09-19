# Phase 8 - Completed milestone definitions, tenth shard

Canonical active roadmap shard holding additional Phase 8 milestone definitions that are complete and
self-contained. Return to [ROADMAP.md](../ROADMAP.md) or the active shard [PHASE_8.md](PHASE_8.md).
Historical completion evidence: [PHASE_8 handoffs](../Archive/Phase-Handoffs/PHASE_8.md).

Created 2026-09-19 during the eighteenth hero's series, when `PHASE_8.md` stood at 14,653 words against its
15,000-word hard limit with two authorized blocks (8.58, 8.59) and the Rowenna ceremonies still to write; the
contiguous run moved here is 8.35z54 through 8.37b. `PHASE_8_COMPLETED_9.md` stood at 8,057 words and would
have crossed its own 12,750-word warning line, so a tenth shard was opened.

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
