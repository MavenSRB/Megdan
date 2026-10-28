# Experiments

Canonical designs and acceptance requirements; implementation status belongs to the owning roadmap. [9.55](../Roadmap/PHASE_9.md#x-955-record-the-experimental-timer-and-header-design) and [9.57](../Roadmap/PHASE_9.md#x-957-record-the-experimental-teammate-coordination-design) record documentation only. Implementation [9.56](../Roadmap/PHASE_9.md#956-implement-and-gate-the-experimental-timer-and-header) and [9.58](../Roadmap/PHASE_9.md#958-implement-and-gate-experimental-teammate-coordination) is authorized now in an isolated worktree, in the sequence recorded by the session brief; reconcile final hero changes before merging. Return to [project context](../PROJECT_CONTEXT.md). The [session brief](../SESSION_BRIEF.md) alone owns active work. Standard board-game rules remain governed by [the rules ledger](../RULES_LEDGER.md); this opt-in digital variant is recorded there separately.

The [implementation preparation packet](EXPERIMENTAL_PREPARATION.md) maps these decisions to existing source, proposed contracts, concrete acceptance cases and the post-hero handoff; it does not authorize runtime work.

## Experimental menu

**Experimental** opens a scrollable immutable-options popup. Map stays outside; PvP defaults off. PvE hides Experimental and forces coordination on ([PVE-D38](PVE_DESIGN.md#pve-d38-unified-lobby-and-cooperative-ownership-approved-user-2026-10-12)). **Turn timer** reveals durations; **Teammate coordination** independently enables allied card inspection/right-click suggestions. Defaults: commitment **120s**, resolution **30s**, response **30s**, upgrade **120s**, bank/wave **300s**. Allowances: 1–86400 whole seconds; bank: 0–86400. Invalid input is refused. Mockup lives remain illustrative.

| Setting | What it times |
| --- | --- |
| Card commitment time | Each player's card commitment, including simultaneous commitments. |
| Card resolution time | The acting player's complete card resolution, including action selection; not a fresh allowance for each prompt. |
| Response time | Decisions handed to another player, and other decisions outside the preceding allowances: tied initiative, post-reveal retrieval, Minion Battle voting and Lane Push placement. |
| Card upgrade time | Card-upgrade decisions; a separate setting, including simultaneous upgrades. |
| Team time bank | Shared extra thinking time for each team, reset to the configured amount each wave. |

All these durations are configured on the Experimental screen. Keep the descriptive setting names; do not use X/Y/Z in the UI or design.

## Timing and ownership

Count only while the relevant player/team can act. Pause during animations and automatic resolution. The deciding team owns the time, including friendly-player interruptions; the originating actor alone does not determine clock colour or which bank is charged.

A decision consumes its normal allowance first, then its team's remaining bank. An empty bank does not remove future normal allowances. Each new card commitment/resolution or independently opened response/upgrade gets its applicable allowance, but selecting/deselecting items, reopening a menu or moving to the next mandatory prompt inside the same resolution must not refresh that resolution's budget.

When a card resolution passes a decision to another player, pause the original remaining resolution allowance (or its bank consumption). Run the answering player's response allowance and then that team's bank if necessary. Resume the original remaining budget when the response finishes. Nested interruptions require the same ownership and pause/resume discipline; do not double-charge a paused frame. A response comprising several required steps is not an opportunity to replenish time on each UI prompt.

Several teammates using their bank concurrently consume **one second per elapsed second overall**, not one second per teammate. Each team's bank is independent. Stop charging a team once it has no outstanding bank-using decision; another team's remaining decision must not charge it. A wave reset occurs once at the authoritative wave transition.

### Undo, restoration and replay

- Ordinary player Undo and timeout-triggered Undo retain existing authorization and irreversible-information boundaries. They do not refund spent allowance or bank time or reset a deadline.
- **Director Undo restores the clocks and team banks to the earlier checkpoint along with the game state**, as explicitly requested by the user. Restore the remaining allowances, banks, decision owners, paused/resumed frames and bank/allowance phase consistently; do not restore only the visible digits. Rebase running deadlines from restored remaining durations, not an obsolete wall-clock timestamp.
- Reconnect/view rebuild must not grant new time or lose an overdue decision. Snapshot/replay handling must preserve the authoritative timing outcome and random answers without depending on fresh real-time delays during replay. The implementation must define the existing transport-pause interaction and test it rather than silently introducing disconnect immunity or forfeiture.
- Host-authoritative timing decisions and accepted timeout commands must be recorded and replayable; UI animation, colour and countdown rendering cannot own rules resolution. Runtime random choices must be recorded/reproducible, not independently rerolled by clients.

## Timeout policy

A timeout means **both the applicable allowance and available team bank are exhausted**. Revalidate the standing decision before applying an answer. The following rules resolve the discussion; no fallback may choose an illegal answer or bypass an irreversible boundary.

1. During the acting player's card resolution, end the turn when legal. If ordinary authorized Undo can reach a legal End Turn point, undo only within the current actor's turn and end it. Do not introduce Director-level rollback, undo a committed attack beyond the existing boundary, refund time, or undo an earlier player's turn.
2. If legal end-turn/Undo cannot close the current resolution, decline optional choices and complete mandatory ones using the specific rules below. Another player's response timeout uses its own answers, not rollback of the original actor's action.
3. **Random legal is the universal fallback** whenever no more specific agreed rule applies. This includes mandatory non-spatial options, colour, direction, Sun/Moon text, rune activation, borrowed-action options and numeric choices. Optional zero/stop answers still follow decline semantics.
4. Preserve previously accepted selections and effects. Fill only remaining required slots; decline optional additions. Random completion must satisfy the whole selection's cardinality, uniqueness, pairing and ordering constraints, not merely pick individually plausible buttons.
5. A depleted resolution retains its exhausted budget through subsequent prompts: resolve their mandatory answers or declines without waiting for another timer tick or replenishing the card-resolution allowance. A separately owned response still gets its applicable response allowance.

### Choice coverage matrix

This is a static source audit of the current choice families and non-pending offers, not runtime timer evidence. The inventory sources are [the pending contract](../../../Assets/GuardsDigital/Application/MatchSessionProjection.cs), [the choice-family oracle](../../../Assets/Tests/EditMode/ConfiguredPendingChoiceProjectionTests.cs), [effect-choice values](../../../Assets/GuardsDigital/Core/Rules/EffectResolution/EffectChoice.cs) and [team agreement](../../../Assets/GuardsDigital/Application/TeamAgreement.cs).

| Choice or situation | Timeout result |
| --- | --- |
| Card commitment, including a required multi-card commitment | Lowest-initiative eligible card(s), preserving accepted selections; random ties. An optional extra commitment declines. Empty-hand Pass remains its existing legal answer. |
| Ordinary action selection; Razzle acting-body selection | End the turn. Prove that the acting-body preselection can reach this outcome through legal commands; do not invent expanded Undo rights. |
| Unconfirmed Fast Travel destination or Clear subset | Abandon the uncommitted selection and end the turn. |
| Attack target while ordinary cancellation remains available | Cancel through the existing authorized route and end the turn. |
| Committed/uncancellable attack target or later mandatory target | Resolve the required target using the applicable legal selection rule; do not rewind past the lock. |
| Optional movement continuation | Stop at the current position, preserving movement already performed; settle required consequences and release the turn. |
| Optional effect, repeat, Defense, removal prevention/payment | Decline. This takes priority over card/minion selection rules. |
| Hero respawn | Decline respawn and resolve the card without acting. |
| Mandatory card discard, retrieval, swap, copy-source selection or post-reveal retrieval | Lowest-initiative eligible offered card; random ties. When offered cards have no initiative, random legal. |
| Card upgrade | Pick the lowest-initiative offered card; random ties. This supersedes the earlier two-stage ranking proposal; do not add a separate ranking policy for hidden/compound upgrade data. |
| Mandatory minion-only selection, outside Minion Battle voting | Random eligible minion; preserve accepted picks and fill only required slots under current legality. |
| Mandatory individual board choice with an acting unit (space, token or hero) | Closest eligible to the acting unit, restricted to actual legal candidates. Random legal resolves equal closest candidates or a case with no applicable agreed ranking. |
| Mixed hero/minion target list | Where neither a more specific agreed rule nor legal cancellation settles the mixed list, use the universal random-legal fallback; the audit's nearest-mixed-target suggestion was not separately adopted. |
| Shared board placement/destination choices requiring agreement, including Lane Push placement and shared equal-distance choices | Agreement/vote policy below. Existing placement/nearest-space restrictions still define eligibility. |
| Displaced-minion correction | Choose the mandatory minion under the minion rule; its destination uses the applicable board/shared-choice rule. It cannot be skipped to release an unrelated turn. |
| Minion Battle | Agreement/vote policy below, filling the required removal slots while respecting eligibility and legal removal order. |
| Tied initiative awaiting agreement | Response time; the universal random-legal fallback selects a legal tied actor if no specific timeout-voting policy applies. The suggestion to extend plurality here was not explicitly adopted. |
| Snorri rune assignment | Preserve valid accepted assignments and randomly complete a legal full permutation, one rune per slot. |
| Other mandatory effect mode, value, body/target, numeric, direction or ordered choice | Specific rules above where applicable, otherwise random legal completion. Numeric zero means decline only where the actual choice semantics permit it. |
| Mandatory choice whose candidates are all refused | Use the authority's existing Stop/impossible-step continuation if offered. An unexplained empty answer set is a defect to close, not permission to fabricate a result. |
| Finished resolution awaiting turn release | Keep the same card-resolution clock running and wait for that actor's END TURN while allowance or team bank remains; release automatically only after both expire. Automatic end-of-round advancement remains automatic and untimed. |

All thirteen audited pending families are accounted for: initiative, movement, attack target, Defense, effect, correction, Minion Battle, hero respawn, system transition (Lane Push/upgrade), removal prevention, turn release, acting body and reveal retrieval. Pregame hero selection was not included in the agreed timer scope.

### Agreement and votes

Shared board decisions covered above resolve early when all eligible deciding players agree. At timeout, use the legal option with the **most votes** (plurality, not an absolute-majority requirement). Choose randomly among tied highest-vote options. Zero votes is an all-eligible-options tie.

Minion Battle follows that same policy: most-voted eligible removals fill required slots; tied candidates are random, including zero votes. Preserve finalized selections, not every conflicting draft preference. Apply the authority's legality/removal order while deriving a complete valid result, rechecking remaining eligible candidates as required. Do not mix opponents' votes into a team-owned decision or count a player's repeated clicks as extra votes. Adding agreement surfaces for shared board choices must be scoped in the implementation, not assumed already shipped.

## Header and clock presentation

The top bar retains its **current width and height**. Do not widen it, make it thicker, displace the board, overlap controls or shrink the board to accommodate the design. At each supported viewport use the largest legible status text and life hearts that fit safely alongside the narration, clock, Undo and End Turn controls.

- Remove the **GUARDS DIGITAL** label.
- Far left, put **ROUND / TURN** on the first line and **WAVES REMAINING** with its count directly below.
- Move each team's Life heart/counter beside its corresponding team-bank clock, red on the red side and blue on the blue side. Enlarge the hearts within the same bounds; life values and rules do not change.
- Normal allowance: one larger main clock above the centre of the narration bar; smaller red and blue bank previews sit at the respective left and right ends of that narration bar, on its baseline. Bank previews do not count down while only the normal allowance is being used.
- Both teams still deciding: the main allowance clock is gold. Only one team still deciding: its colour is red or blue. When a team finishes, change colour without restarting any remaining allowance. Commitment and simultaneous upgrades are known simultaneous cases; derive colour from actual outstanding owners.
- On switching to bank time, remove the normal allowance window. The active bank takes its above-narration position; if both teams use banks, show both active banks there. Remove duplicate side previews for promoted banks; an inactive bank can remain at its side. The position/size change and team colour must make the transition clear.
- **Final user amendment: no text labels on clock windows. Show only the little clock icon and remaining time.** Remove labels such as CARD COMMITMENT, RESPONSE TIME, RED BANK, BLUE BANK and TEAM TIME BANK from the clock display. Settings keep descriptive names; existing narration still explains the current decision. Preserve assistive identification without adding visible clock labels.
- Earlier generated mockups are illustrative and predate the icon-only amendment. Their digits are samples. The written decisions here govern over inconsistent mockup text or counts.

The simultaneous display must remain truthful if independent response/upgrade starts leave different remaining allowances: do not merge budgets, silently replenish time or hide the outstanding decision. Fit any necessary representation within the existing header and validate it on the actual multi-seat path.

## NebKher prerequisite and release acceptance

**NebKher the Harbinger is being implemented under [9.51-9.54](../Roadmap/PHASE_9_FINAL_TWO_HEROES.md). His final choices must be checked against timers before a merged release containing him.** The 2026-10-05 user amendment authorizes the separate 31-hero experimental build before that merge. Its available-roster gate does not certify future content or redirect his approved sequence.

Milestone 9.56 owns these checks for the available roster; the final NebKher merge must extend them to his completed content. The roadmap owns validation status, and 9.64c owns the built-player checks:

1. **Complete choice coverage:** derive the final pending-family/offer/effect-choice inventory from source and dealt content. Map every reachable decision to allowance, owner, optionality, timeout result and continuation. Include all NebKher fronts, Illusion selections/substitution, borrowed primary/secondary/Attack/Defense and nested ownership, optional laughter and divided-board restrictions; include his Ultimate and pending states created only after continuation. Treat an unmapped decision as a failed gate, not as implicitly covered by the phrase random legal.
2. **No stranded match:** expire each decision family with an empty bank and drive accepted timeout commands through the real authority. Prove that it advances to the next legitimate owner/stage or game end within bounded work. Exercise timeout chains, partial selections, valid rune permutations, minimum counts, optional decline, no-candidate/Stop paths, constrained minion voting, legal Undo and locked attacks, acting-body preselection and turn release. A loop cap, exception, refusal or silent no-op is not passing progression evidence.
3. **Race and revalidation:** prove one outcome for a player answer arriving at the deadline; reject stale/duplicate timeout commands without stranding the successor. Re-derive legal choices after state changes; do not repeatedly retry the same invalid answer. Exercise simultaneous team expiry, teams finishing at different instants, bank exhaustion mid-response and a wave transition while work is pending.
4. **Clock correctness:** prove animation/automatic-resolution pauses, nested response pause/resume, one bank second per team per second, normal allowance with zero bank, exactly-once wave reset and no budget farming through player Undo or reopened controls. Use controllable time, not long wall-clock sleeps.
5. **Director Undo and persistence:** witness non-default allowances/banks and paused owners before and after Director Undo, restored exactly. Separately prove ordinary/timeout Undo does not refund time. Snapshot, deterministic replay, reconnect and view rebuild must retain valid timing and timeout continuations, with no duplicate charge, fresh reroll or frozen expired state.
6. **Real player path:** exercise host and joined seats, simultaneous commitments/upgrades and team votes on the final roster including NebKher. Verify timer-off preserves the standard match. Check the icon-only clock transitions, larger two-line status/lives, longest narration, Undo/End Turn and supported viewport sizes without header growth or overlap.

The host must finish timeout resolution through typed authoritative commands, not UI button automation or card-name branches. Random fallback means selection of a **legal completing answer**, not arbitrary button clicks that can deselect, loop or leave a malformed set. Any observed stranded or unhandled case blocks timer completion until corrected with a failing-then-passing regression; log its owning decision and command context for diagnosis. The progress requirement applies to each reachable decision; future content needs fresh coverage.

Related risk: [RISK-TIMER-001](../TECHNICAL_RISKS.md). Validation evidence belongs to the implementation commits, not this design record.

## Teammate coordination

Approved after mockup review; documentation outcome [9.57](../Roadmap/PHASE_9.md#x-957-record-the-experimental-teammate-coordination-design), with implementation completed at [9.58](../Roadmap/PHASE_9.md#958-implement-and-gate-experimental-teammate-coordination). This is the second independent option in the host-game **Experimental** menu. It applies to that match only, resets off for each new hosted game, and does not require Turn timer. The current shipped [hero-rail selection/privacy contract](HERO_RAIL_INTERACTION.md#identity-and-selection) governs option-off play; the enabled exception follows the contract below.

### Allied cards and right-click suggestions

- **Left-click an allied hero's board token or hero tray to inspect that ally's current hand.** Identify whose cards are displayed and provide the mockup's **My Cards** return control. Inspecting an ally never grants permission to play, commit, discard, upgrade or otherwise answer for them.
- **Right-click a board hex to mark it for teammates.** Show a white selection outline and the indicating player's hero token above the hex. This is a communication marker, not an action target or a vote.
- **Right-click a card in the inspected allied hand to suggest it to its owner.** The owner sees that card with the same white glow and the suggesting player's hero token above it. The inspecting player receives visible feedback for their own suggestion.
- Marking uses **right-click, not mouse-over**, for both hexes and cards. It is available throughout all match phases, including while another player is acting or simultaneous decisions are underway. Existing hover previews and ordinary left-click gameplay keep their own purposes.
- Right-clicking the same target again clears that player's marker; right-clicking another moves it. Maintain one board marker and one card suggestion per indicating player so both can appear together, as in the reviewed mockup. Markers persist after pointer exit; a routine phase change alone is not a reason to disable communication or erase a still-valid target.
- Suggestions are teammate communication. Opponents receive neither private allied hand data nor these markers. The host must enforce team/seat eligibility and the enabled option; hiding already-delivered private data in UI is insufficient. The option adds allied hand visibility, not enemy-hand, deck-order or other undisclosed information.

### Marker appearance and overlapping effects

Use the **existing hero-tray token asset and presentation**, including its existing portrait crop, background and player-coloured outline, as the highlight-owner marker. Reuse it rather than drawing a new token or inventing a white-disc portrait treatment. It identifies the player making the suggestion, not the owner of the suggested card or an occupant of the marked hex. The user's token-reuse correction supersedes the intermediate generated token designs.

The board marker is smaller than the card marker. Milestone 9.89 fixes the board sender medallion at 0.42 of the shared rail portrait, above the marked hex; card suggestions retain their larger treatment. Preserve the existing header and board footprint.

If an effect, legal-target cue or selection already highlights the hex, preserve it. The implemented suggestion is 9.81's player-coloured segmented outward ripple, made thicker at 9.89 without changing its animation; place every sender's smaller hero token in stable order in one centred row above it. Neither adding nor clearing a suggestion replaces, recolours or removes the underlying effect/selection. Multiple teammates marking the same target retain their separate identity badges without covering one another. Apply the same coexistence principle to card highlights.

The completed 9.58 closing checks and mockup-scope note are preserved in the [experimental features history](../Archive/EXPERIMENTAL_FEATURES_HISTORY.md#milestone-958-teammate-coordination-closing-checks). Its completion commit owns evidence; this active shard retains the implemented contract below.


## Implemented communication contract

Player colours, cursor/ping ripples and Director controls: [signal contract](PLAYER_SIGNALS.md).

**9.58 compatibility:** wire 37 adds immutable experimental settings, a separately filtered allied-hand collection, and typed teammate signals. Signals derive their sender from the admitted seat, check map identity and current hand ownership, and use per-sender revisions plus a restart generation. They never enter gameplay commands, Undo history or gameplay versioning. Reconnect receives current team-only signals; departure republishes their removal. Experimental host snapshots wrap the unchanged Core schema-58 payload with validated immutable settings and reject mismatched settings atomically. The [owning milestone](../Roadmap/PHASE_9_EXPERIMENTS_AND_MAPS.md#x-958-implement-and-gate-experimental-teammate-coordination) records validation status; timer implementation and the final hero merge remain separate gates.

## Timer candidate implementation contract

9.56 adds `MatchClock`, `MatchHost.Timer` and `TimeoutChoicePolicy`. Integer elapsed ticks enter at the host adapter; every allowance and team bank is host-owned. A team is charged for the union of its simultaneous bank intervals. Resolution and nested response frames survive pauses, while wave identity controls one reset. Options remain immutable per match. The roadmap owns gate/completion status.

Authenticated readiness reports name the authoritative version. Input remains paused through spotlight/kinetic transitions; an attack held for a defender's answer is actionable. A ready report is latched for that version. Missing visual completion has a 30-second host lease so repeated busy reports cannot freeze a match; disconnected reserved seats retain the existing whole-session pause, including lease aging. No elapsed animation interval is charged retroactively when the lease ends. Pregame selection stays untimed.
Milestone 9.73 uses the same ticks, readiness and pauses to write per-player windows to
`guards-gameplay-log/2` even with enforcement off; parent resolutions pause during nested responses.
Measurements stay outside state, replay, snapshots and wire. Five valid duration values persist
locally, including a zero bank; new Experimental toggles remain off.

The wire carries clocks, readiness, typed votes, permitted current-card inspection faces and the experimental Minion Battle cap. Votes are separate from suggestions and retain deciding-side eligibility. Unanimity resolves early; timeout applies plurality and recorded random ties. Constrained selections retain their tally through completion and restore. Timer snapshots use `guards-experimental/4:` around Core schema 59, preserving options, ticks, paused responses, ballots and timeout random state. Director Undo restores a pre-command checkpoint; other Undo cannot refund time. Imported Core snapshots invent no Undo history; replay uses accepted timeout commands without waiting or redrawing.

The 96-unit header groups simultaneous normal allowances into one gold clock showing the latest
remainder; lone allowances use team colour. Bank clocks stay separate team resources. Every clock
shows its icon, digits and tooltip.

Private built-player smoke may set `GUARDS_SMOKE_EXPERIMENTS=1` only with rehearsal Smoke mode. It exercises real host controls, joined clock/options projections, allied-hand inspection, authenticated suggestions and return to My Cards. Normal launches never enable the hook or experiments.

### Clock updates

Periodic authenticated `ClockUpdate` frames have their own per-connection sequence and current game version. They replace only clock display state: never gameplay projection/revision, hands, choices, board highlights, hover, local preparation, gameplay sequence or pending acknowledgement. Accepted timeout commands still publish normal owner-scoped projections. Full/reconnect projections include clocks; stale or duplicate updates are ignored. Authenticated `CoordinationUpdate` frames likewise carry their own sequence, game version and reset generation, and replace only team marker state. Local and remote paths repaint outlines/tokens in place, preserving cards, windows, hover and pending acknowledgements; full/reconnect projections carry the complete marker snapshot.

The 9.64a corrections also use black duration values, vertically centred clock/status text, larger hearts within the unchanged bar, and the same white card highlight and tray portrait on a hovered card's enlarged face.

Gate corrections (9.56): Defense text returning a decision to the original attacker still uses a nested Response allowance while both parent budgets remain paused. The host reads the typed Defense continuation; no card-name or identifier parsing is used. Experimental snapshot decoding rejects duplicate voters before applying Core state. Board-suggestion outlines and owner tokens paint below every HUD window, including spellbook drawers.

## Current-card inspection extension

Implemented at 9.64e/9.64g and partitioned at 9.72. Either card-information experiment lets an
authorized token or tray open read-only inspection without replacing the local hand. `HAND` is
upper-left, ordinary `UNAVAILABLE CARDS` upper-right, and Takahide's typed `OTHER GOLDS` in a
bordered box below his hand. Future upgrades, deck choices, items and controls are excluded.

Teammate coordination grants own/allied access; default-off Enemy card information grants enemy
access. Both govern spellbook and Takahide-gold exceptions. Public visibility is unchanged when off.
Permitted spellbooks remain owner-style but non-casting; 9.64h owns the shortcut and 9.64d hover.

The projection carries known hand/other/other-Gold references and anonymous counts, never instance
IDs. Top-rim and lower-rim pips read public current-card and hand sources. Before joint reveal,
non-owners receive anonymous hand and commitment counts, preventing subtraction. A facedown resting
swap stays anonymous within its colour set until authority reveals or retrieves it; that state
persists through rounds, snapshots, replay, Undo and reconnect. Wire 46 rejects earlier peers;
experimental envelope 5 and Core schema 59 are unchanged.

Collider-free unlit pips follow the token border in the top render queue while target, selection,
hover, Active-effect, damage and Defense layers remain underneath. Inspection stays above later
mandatory decisions; CLOSE and Escape remain reachable, and victory closes it before Restart.
## Minion Battle removal cap

Milestone 9.69 adds an independent per-match option in the host's Experimental menu. It is disabled
for every new hosted game. When enabled, its non-negative whole-number value caps the total effective
losing units removed by one Minion Battle after the ordinary count difference and available losing
units are derived. A value of zero therefore completes the battle without removing a unit. A cap at
or above the ordinary removal count is non-binding.

The option changes only the removal count. Existing effective-unit counting, Wuk exclusions,
counts-as-minion participants, Heavy-last selection, losing-team choice authority and stable removal
order remain unchanged. Lane Push is derived from the board after those capped removals, so a cap that
leaves a losing unit in the Battle Zone prevents the last-minion push; it never suppresses a push
after the capped result really did empty that side. Disabled play remains the official rulebook path.

The setting is immutable after hosting begins and is part of experimental snapshot and private-wire
identity. Restart, replay, Undo and restore use the same configured cap; a snapshot captured with a
different enabled state or value is refused atomically. This feature does not alter the Core snapshot
schema, hero/card content, timer, coordination permissions or map selection.

## One-Life hero base cost

Milestone 9.71 adds an independent host option, off by default. It makes every hero Defeat's base
Life cost 1 before existing additions. Tier-based assist coins and exact-Level rewards remain; tray
badges keep their Tier number but become yellow-only. W-132 binds this immutable setting at the
aggregate's single graph-install seam, covering fresh/private-online composition, restart, replay,
Undo, restore and reconnect. Disabled play remains official.
## Modifier presentation controls

W-019 adds seven independent host options, all off for every fresh hosted game. The hero-token Defense shield always includes the net standard-minion contribution, including an effective token that the typed rules currently make a minion. Its **Items**, **Active effects**, and **Card effects** options add those categories independently. The card-face group separately exposes **Items**, **Minion contribution**, **Active effects**, and **Card effects**.

“Card effects” means typed `CardTextModifier` contributions plus persistent card/continuous rule sources; it does not reclassify temporary Active effects or held items. The shield has no selected action card, so it can include only hero-wide card/continuous modifiers that apply without choosing a Defense offer. A card face may include action-specific card text when the existing Core query can establish it from current public state. A resolved action total never bypasses these filters.

These options change presentation projections only. Combat, actions, item bonus multiplication, minion adjacency, effect lifetime, Undo, replay, and rules authority remain unchanged. The selected combination is immutable match identity under private wire 52 and experimental envelope 6; Core snapshot schema 59 is unchanged. Joined clients receive the host's exact options, and restore refuses a snapshot captured with a different combination.
