# Phase 8 - Playtest refinements

On 2026-09-17 the user reported Super-Shotgun making the attacked hero discard, requested the revealed-card Initiative badge include the bonus, and approved fixing both Shotgun and Super-Shotgun. They then requested that clicking another player's token must not show that player's cards. The approved sequence is **8.42 -> 8.42a -> 8.42b**, with separate validation and local commits. Return to [ROADMAP.md](../ROADMAP.md); [SESSION_BRIEF.md](../SESSION_BRIEF.md) owns the live route.

## Lobby front door, approved 2026-09-27

On 2026-09-27 the user approved the sequence **8.107 -> 8.108** ("fresh each time, router is fine, go ahead"; "Nah, lobby only"). It interrupts Snorri's 8.93g, which stays the active milestone and resumes afterwards. Each step gets its own validation and local commit; the work is built in a separate worktree beside the concurrent Snorri tree.

### [x] 8.107 Split the lobby entry into HOST and JOIN menus

Outcome: the first screen shows only `HOST` and `JOIN`. `HOST` opens a host menu offering the existing 2V2/3V3 and SHORT/LONG choices, `PORT`, a `SESSION ID` pre-filled with a fresh random value on every launch, and `HOST`/`CANCEL`. `JOIN` opens a join menu with `HOST IP`, `PORT`, `SESSION ID` and `JOIN`/`CANCEL`. `CANCEL` returns to the first screen and keeps the typed values. The join IP, port and session ID and the host port are remembered between launches; the host session ID is not. Pasted IP and port values lose surrounding or embedded whitespace and invisible characters before they are read, and a session ID loses surrounding whitespace. Once hosting starts, the host's lobby shows the session ID and the internet IP. The internet IP comes from the local router over UPnP; if the router gives no public address, the screen shows the LAN IPv4 labelled as such. A host whose own client never took a seat gets `DIRECTOR UNDO`, `DIRECTOR RESTART` and `LEAVE` on that screen after the match starts. These drive the same authority undo/restart and republish-to-every-client path that a playing host's `DIRECTOR` drawer already uses.

Unchanged: a cleared session ID still hosts loopback-only (8.100). No rule, authority, snapshot or wire change. No outside "what is my IP" service is contacted.

Acceptance: Edit Mode tests cover the endpoint and session-ID cleaning (spaces, tabs, non-breaking and zero-width characters, empty values); the generated session ID's shape and freshness; the remembered-field store's round trip and the host ID not persisting; the page flow on the view (landing shows exactly HOST and JOIN, each menu shows exactly its fields and buttons, CANCEL keeps values, leaving a session returns to the landing); the host invitation line in its keyed, unkeyed, probing, router-found and LAN-fallback forms; the UPnP response parsers; and the non-playing host's model offering the Director controls only after the match starts and never to a seated host. The existing lobby Play Mode journeys are updated to walk through the menus, and at least one real-socket journey hosts through the menu with a pasted, space-padded port. T2.

Completed 2026-09-27. The view keeps the page as its own state and returns to the landing whenever a session ends. The session ID's random bytes are drawn in the Network assembly (`PrivateSessionIdSource`), because the Application boundary may draw no unrecorded entropy. The host console is `PrivateLobbyModel.IsHostConsole`: the host's own client is still `Admitted` after the match starts, because `MatchStarted` goes only to seated connections. No rule, authority, snapshot or wire change.

Historical evidence: [8.107 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8107-lobby-host-and-join-menus-evidence).

### [x] 8.108 Player names in the lobby

Outcome: both menus carry a `NAME` field, remembered between launches. The name travels with the join and appears in every lobby seat row in place of the seat identifier. An empty name keeps today's `RED-1` style label. The host cleans every name it receives: trimmed, control and invisible characters removed, at most 16 characters. Duplicate names are allowed. Bot seats keep their seat label. Names are lobby-only: the match HUD, match log, victory screen and snapshots do not change.

This changes the wire (the join message and the roster row gain a name), so the private-online protocol version moves from `/23` to `/24`.

Acceptance: codec round-trip and bounds tests for the new fields, host-side cleaning of hostile names, roster/model rows showing the name with the seat-label fallback, the redraw signature noticing a name, the protocol-version pins moved, and the affected lobby and wire Edit/Play Mode fixtures green. T2.

Completed 2026-09-27. The name rides the seat claim rather than the Hello, because a keyed session seals every frame after the key proof and the Hello carries that proof in the clear. The host keeps it on the seat hold: an away seat keeps it for its returning player, and it is cleared whenever the seat opens. One NAME field serves both menus. `PrivateLobbyLabels.SeatName` falls back to the seat identifier. Wire `guards-private-online/24`; schema v46 unchanged. During this milestone the user also asked for the entry panel to fit its content and be centred ("Why is that box for host/join that big"): it is now 540 wide on the landing and 720 on the menus, with its contents centred; a session's lobby keeps the 72% panel.

Historical evidence: [8.108 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8108-lobby-player-names-evidence).

## Approved playtest corrections ahead of Snorri

On 2026-09-26 the user approved investigating and fixing these reports in order: **8.95 -> 8.96 -> 8.97**, before resuming Snorri at 8.93e. Each needs its own failing reproduction, correction if confirmed, green affected checks and local commit before it can be closed. The private host-game log under the 8.92 playtest build is available as evidence; preserve it.

### [x] 8.100 Host without entering this machine's address

Authorized 2026-09-26 after 8.99. Outcome: the ordinary lobby's HOST control does not parse or bind the address a player would give a joiner. The field is explicitly labelled `JOIN ADDRESS`; HOST derives its bind locally, using loopback for an unkeyed local-only session and the IPv4 wildcard for a keyed invited session, while its own player client still dials loopback. JOIN continues to validate and dial the entered numeric address and port.

Acceptance: record a failing-then-passing ordinary-lobby journey in which an intentionally unusable join address cannot prevent HOST; prove an unkeyed host remains loopback-only, a keyed host selects the wildcard bind, the hosting player reaches its ordinary claim-seat surface through loopback, and JOIN still rejects or dials the typed address as before. Update the private-online record and visible label. Run the focused lobby Edit/Play Mode contract at T2.

Non-goals: discovering or displaying the machine's LAN/public address, router configuration, NAT traversal, Relay, DNS/hostnames, changing the standalone command-line host contract, weakening the session-key requirement for a remotely reachable listener, protocol/schema/content identity, or packaging. Snorri 8.93e's in-progress work remains preserved and resumes after the separately requested 8.101 package.

Completed 2026-09-26. HOST now validates only the port, selects loopback without a key or the IPv4 wildcard with one, and brings its own owner-scoped client in through loopback. The same field is visibly `JOIN ADDRESS`, and JOIN still parses it normally. Protocol, schema and content identity are unchanged.

Historical evidence: [8.100 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-8100-automatic-lobby-host-bind-evidence).

### [x] 8.99 Restore an away player through the real lobby

Authorized 2026-09-26 after the power-loss and alt-tab reports were separated. Outcome: a player whose process or connection dies can relaunch the ordinary seatless lobby client and reclaim the seat the host kept `AWAY`, including after the match has started. The locally retained owner capability selects only that player's reserved seat; no session key, open-seat claim, other local capability or host guess transfers ownership.

Acceptance: preserve the September 25 host log as the failing field observation (the returning connection was admitted but sent no reclaim); add a failing-then-passing fresh-client regression that begins seatless, reads a started roster with its reserved seat, uses the file-store contract rather than copying client state, records `SeatReclaimed`, rotates the connection epoch and receives a fresh owner projection; cover no capability, wrong capability, multiple locally retained reserved seats and ordinary open-seat joining. Exercise the ordinary lobby/socket player path at the applicable focused Edit and Play Mode tier. Record focus changes only if required to reproduce a remaining disconnect; repairing general focus/input behavior is outside this demonstrated recovery defect.

Non-goals: host migration, automatic seat release, weakening owner authentication, changing protocol/schema/content identity, packaging, or claiming that alt-tab itself caused the recorded power-loss disconnect. Snorri 8.93e's in-progress work is preserved and resumes after this correction.

Completed 2026-09-26. The Welcome roster now lets a seatless relaunched process select the single reserved seat for which its machine retained the owner capability, then continue through the existing authenticated reclaim and fresh-projection path. No capability remains an ordinary seatless join; multiple matching reserved seats remain unselected rather than guessing an owner. Protocol, schema and content identity are unchanged.

Historical evidence: [8.99 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-899-away-seat-reclaim-evidence).

### [x] 8.95 Misa's next-turn Green Movement offer

Reported 2026-09-25: "Misas green does not offer movment" on the **next turn**, when it should. Focus, Discipline and Mastery create the correct Next Turn effect; the turn authority intercepted Attack and Movement starts but omitted Skill starts and could not resume a held Skill. Fixed at the shared pre-action seam. The host log shows Discipline's Skill on round 2 turn 1 and a next-turn Skill with no logged Misa move; the log alone cannot prove a missing offer. Player-input journeys now reach all three tier-specific free-step choices before a next-turn Skill and Discipline's step changes the board before its held Skill resumes. Existing source-linked tests retain once-only, round-boundary, and pre-action snapshot contracts. Status: complete; no content or schema revision.

Historical evidence: [8.95 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-895-misa-next-turn-movement-evidence).

### [x] 8.96 Tali Totem art and save/removal, with Ice Wall audit

Reported 2026-09-25 with a playtest photo: Tali's Totem appeared without art; after a save, the minion survived but the Totem remained. Both were confirmed. The preparation script named Totem PNGs differently from the authoritative token IDs, so Unity loaded a fallback disc; corrected mappings and prepared the private resources without altering the source atlas. The mandatory-save query stopped minion removal without consuming the source; the removal stage now reclaims exactly that Totem in canonical order. No removal choice should appear: the printed save is mandatory. All five Green cards have face-by-face count/role/optionality checks. The Ice role, used by Glacial Barrier, Wall of Frost and Pack Ice, loaded its prepared art and its initiative aura passed; no separate Ice defect was found. Full Attack/Defeat, token-source, negative, clicked-placement, and artwork regressions pass. A live non-batch Game View inspection was unavailable because the computer-use helper failed to initialize; automated renderer checks and the inspected prepared face substitute only for the claims stated here. Status: complete; old playtest package remains unchanged.

Historical evidence: [8.96 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-896-tali-green-art-and-totem-payment-evidence).

### [x] 8.97 Ursafar Angry Roar candidate and borrowed action

Reported 2026-09-26: Ursafar's Silver (Angry Roar) offered a card choice that appeared to include a red and a gold card although only the gold had an Active effect; after selection it appeared to fizzle, leaving END TURN. Its printed condition is "If enraged, perform the primary action on one of your active cards with an active effect." The user additionally requested an audit of every other Ursafar card for similar bugs. Closing checks: failing-then-passing candidate and action-result paths, no inactive card offered, the chosen primary performed with its ordinary targets/choices, all-face affectedness audit, and restore/Undo parity. Status: complete.

The reproduced corrections are narrowly separate. The active-card pool treated a dormant effect as live although the authoritative active-card tray requires the Active phase; its phase check now agrees with the tray. A borrowed Attack that binds its target in before-attack card text was discarded by a pre-action target probe before its printed optional move could create a target; that text-bound borrow now starts at step 1 and lets its own mandatory binding decide whether it reaches anyone. A failing-then-passing player-input journey stages an active Gold source, moves Ursafar out of contact, borrows Gold with Angry Roar, walks back into contact, and resolves the Attack. The static all-face audit checks every Ursafar definition with a dormant and then an activated source; the 26-test configured journey cohort covers his Gold, Silver, Red, Blue, Green, Ultimate, restore and Undo paths. Red is a valid Angry Roar candidate whenever its own Active effect is live; color alone does not exclude it. The preserved host log has no Angry Roar invocation, so the reported Red card's exact phase cannot be reconstructed from that log.

T3 closure: full Unity-free 4,680/4,680, Unity Edit 5,674 passed with 37 pre-existing Explicit skips and zero failures, Unity Play 860/860 with zero skips. The broad gate exposed two stale Snorri-era Edit assertions (the eleventh value-query kind and nine older identity hashes affected by an optional marker field) and one omitted 8.95 Misa journey matrix link; all were corrected, focused-rerun green, and followed by the stated green complete gates. No other Ursafar face needed a rule or behavior change. No schema, wire, card content or package changed.

Historical evidence: [8.97 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-897-ursafar-angry-roar-and-all-card-audit-evidence).

### [x] 8.42 Exclude the attack target from shotgun scatter

Outcome: Shotgun and Super-Shotgun affect a different enemy hero adjacent to the attack target, never that target itself.

Acceptance: use the existing typed enclosing-target exclusion; reproduce the reported self-discard failure before correction; exercise both actual card definitions against hero and minion targets, with and without an eligible bystander; preserve discard ownership, Super-Shotgun's inability-to-discard Defeat, and continuation of the original attack. Focused Core and directly affected regressions, plus an actual player-input journey for the reported path (T2). Preserve deterministic replay/restore and content identity contracts.

Non-goals: shared distance-query changes, rule reinterpretation, other cards, new packaging.

Historical evidence: [8.42 completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842-shotgun-target-exclusion-completion-evidence).

### [x] 8.42a Show total revealed Initiative in the hero rail

Outcome: the revealed-card badge in each hero plaque shows printed Initiative plus Initiative items, using authoritative public counts and the shared item query. The printed card front remains unchanged. Validation also repairs Swift's artwork catalog references after 8.42's content-revision bump; that resource metadata requires the build-smoke check.

Acceptance: base-only and bonus cases, correct per-hero totals, no facedown information leak, updates across reveal/resolution/round refresh, existing hover and badge geometry preserved, and owner-private/Director projection agreement. Focused projection and HUD checks, affected Play Mode and targeted visual inspection (T2); no Initiative resolution rule or protocol/schema change.

Non-goals: other statistics, tray redesign, unrelated presentation changes, packaging.

On 2026-09-18 the user approved `Tools/Invoke-PlayerBuildSmoke.ps1` for the updated resource catalog, discharging the checkpoint's prerequisite. No package or upload is authorized.

Historical evidence: [checkpoint](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842a-initiative-badge-build-gate-checkpoint); [completion](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842a-initiative-badge-completion-evidence).

### [x] 8.42b Keep player tray navigation private and connect hero identity controls

Outcome: clicking another player's board token cannot show that player's card tray in a player seat; the seat's own tray remains selected. Clicking the player's own hero plaque selects that hero. Hovering a plaque's hero portrait shows its existing hero-picker profile card. Director-only inspection is preserved. The user explicitly added the own-plaque selection and portrait-preview requirements on 2026-09-17.

Acceptance: actual board clicks on allied and enemy tokens from a player seat keep the owner's tray, hide all foreign tray cards and preserve own card actions; own-plaque click selects the local hero; profile hover uses the correct existing artwork, stays on-screen and dismisses cleanly; private projection boundaries and reconnect/refresh remain intact; Director pinning still works. Focused presenter and affected real-input/online Play Mode checks plus visual inspection (T2).

Non-goals: removing public hero-rail history or hover information, changing game visibility rules, or disabling Director tools.

Historical evidence: [8.42b completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842b-hero-tray-interactions-completion-evidence).

### [x] 8.42c Name the awaited responder in status narration

Authorized 2026-09-18, to follow Widget's approved sequence: the user reports the narration bar fails to say when the game waits for a hero outside the current activation, for example "Sabina choosing defense card".

Outcome: status narration identifies the hero whose response is currently required and the public activity, including out-of-turn Defense and opponent-owned card choices, rather than attributing every pause to the active hero.

Acceptance: reproduce the incorrect Defense narration before fixing it; derive the awaited responder from authoritative pending-input/projection facts, preserving the separate active actor; cover defender card selection, opponent-owned discard/other applicable pending families, ordinary activation, and transition back to the attacker. Both responding and observing seats must receive accurate public waiting text with no foreign hand/selection leak. Focused presenter/projection regressions, an affected real-input Play Mode journey and targeted visual inspection (T2); snapshot refresh must rederive the same narration without new authoritative state.

Non-goals: narration-bar redesign, new rule timing, unrelated HUD changes or packaging.

Historical evidence: [8.42c completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842c-awaited-responder-narration-completion-evidence).

### [x] 8.42d Keep the seat's own rewind visible while its card text is asking

Authorized 2026-09-18, after the 8.44 package: a playtester reported that undo stops being available as soon as the attack option is selected on a card, before any target is chosen, playing Whisper. Investigation measured the host still publishing the rewind throughout, so the loss was presentational.

Outcome: the seat's UNDO square is drawn whenever the host publishes that seat's rewind, including while the acting seat's own card-text choice stands. The authority's bounds are untouched: the turn floor, the other-seat wall, and the attack wall that rises once a target is chosen still decide exactly what they decided before.

Acceptance: reproduce the missing square before correcting it, on both routings of a card-text prompt - the CONFIRM control a select raises, and a candidate whose preview names an object; draw the square from `IsSeatUndoAvailable` alone, no longer suppressed by the action region's exclusive-decision rule, which does not own this status-strip control any more than it owns END TURN since 8.35v; a seat published no rewind still draws nothing rather than a greyed square. Preserve the glyph, tooltip, square geometry, shared button chrome and motion, the strip's pointer claim, END TURN's own slot, and every other use of that rule for the hand and tray. Focused HUD-treatment and seat-scoped regressions plus the complete Edit Mode and Play Mode suites (T2); no host, projection, protocol or schema change.

Non-goals: the host's undo bounds; END TURN's cancel composition for a card-text-bound attack, which one press cannot discard and which would mislabel the control; an always-present greyed square for seats with no rewind; the action region's one-usable-surface rule for the hand and tray; packaging.

Historical evidence: [8.42d completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842d-visible-seat-rewind-completion-evidence).

### [x] 8.42e Stop the seat's rewind at any interaction with an opponent

Authorized 2026-09-18, directly after 8.42d: "As soon as you attack or interact with other player in any way, you should not be able to undo." Investigation showed 8.35f's wall covers the Attack and nothing else, so a card text that displaces an enemy piece stayed rewindable - measured five times in 2,400 commands of the seeded six-player walk, twice having moved an enemy HERO several spaces.

Outcome: a command that changed anything the acting seat's opponents own puts the undo floor above itself, so a rewind can never step back through it. The user settled both boundaries: opponents only, so a friendly buff or an ally's move stays rewindable and support heroes keep a usable rewind; and the floor rises above that one command rather than ending the turn's rewind, so what the actor does afterwards remains its own to take back.

Acceptance: reproduce the five rewindable interactions before correcting them; decide the wall by comparing what opponents own across each accepted command - enemy hero and minion positions, opposing card locations, markers their heroes hold, tokens they own, and their team life counters - rather than asking each effect to declare that it interacted, so a new card cannot opt out by omission. A rewind must re-read that state without walling itself, or one press would strand the seat. Preserve the turn floor, the other-seat wall, the target-lock attack wall, Director Undo, and every existing bound and refusal reason. Focused seat-undo regressions plus the complete offline harness, Edit Mode and Play Mode suites (T2); the watermark stays derived host state, so no projection, protocol or schema change.

Non-goals: teammates' pieces; a turn-long wall; any change to what the authority offers or to the rules of an Attack; END TURN's cancel composition; a new refusal reason - `UndoUnavailable` carries every bound, and this one gains its own detail sentence rather than falling back on the floor's generic one; packaging.

Historical evidence: [8.42e completion index](../Archive/Phase-Handoffs/PHASE_8.md#milestone-842e-interaction-undo-wall-completion-evidence).
