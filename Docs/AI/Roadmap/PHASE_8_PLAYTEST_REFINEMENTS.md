# Phase 8 - Playtest refinements

On 2026-09-17 the user reported Super-Shotgun making the attacked hero discard, requested the revealed-card Initiative badge include the bonus, and approved fixing both Shotgun and Super-Shotgun. They then requested that clicking another player's token must not show that player's cards. The approved sequence is **8.42 -> 8.42a -> 8.42b**, with separate validation and local commits. Return to [ROADMAP.md](../ROADMAP.md); [SESSION_BRIEF.md](../SESSION_BRIEF.md) owns the live route.

## Approved playtest corrections ahead of Snorri

On 2026-09-26 the user approved investigating and fixing these reports in order: **8.95 -> 8.96 -> 8.97**, before resuming Snorri at 8.93e. Each needs its own failing reproduction, correction if confirmed, green affected checks and local commit before it can be closed. The private host-game log under the 8.92 playtest build is available as evidence; preserve it.

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
