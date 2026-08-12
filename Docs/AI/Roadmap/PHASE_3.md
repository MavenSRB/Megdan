# Phase 3 - Test-card Initiative, turns, and rounds

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_3 handoffs](../Archive/Phase-Handoffs/PHASE_3.md).

## Phase 3 - Test-card Initiative, turns, and rounds

### [x] 3.1 Extend the test-card foundation with turn lifecycle

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Synthetic card instances can represent and transition through the lifecycle facts needed to play a turn without using Wasp data.

Acceptance checks:

- Location, played status, unresolved/resolved status, and face orientation remain orthogonal where the rulebook permits overlap.
- Pure-rule tests cover playing a card Facedown, revealing it, resolving it after an action, and discarding a Defense card from hand.
- Synthetic definitions include validated typed Initiative values, with fixtures for both unequal values and ties.
- Resolved status, discard location, and face orientation remain separate facts rather than mutually exclusive states.
- Test definitions are explicitly synthetic fixtures and cannot become authority for Wasp values or text.

Non-goals: No Wasp inventory, printed card effects, upgrades, items, or end-of-round subsystems.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-31-extend-the-test-card-foundation-with-turn-lifecycle-completion-evidence).

### [x] 3.2 Display both director-mode test hands

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Separate Red and Blue synthetic hands are visible and independently selectable by the director-mode controller.

Acceptance checks:

- Every displayed card resolves to one authoritative owned card instance.
- Team identity is not conveyed by color alone.
- Selecting a card does not play it or resolve an action.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-32-display-both-director-mode-test-hands-completion-evidence).

### [x] 3.3 Play and reveal one test card per side

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The controller selects and confirms one card for each side, both cards enter the correct played/unresolved state, and they reveal together.

Acceptance checks:

- Each side can commit only an eligible card from its own hand.
- Neither side's selection is revealed early.
- Reveal changes authoritative card state; it is not a presentation-only flip.
- Passing is represented when a side has no card available.

Non-goals: No Initiative ordering or action execution.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-33-play-and-reveal-one-test-card-per-side-completion-evidence).

### [x] 3.4 Resolve Initiative and tied Initiative

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The authority identifies the next unresolved actor from current Initiative values and applies the Tie Breaker rules without depending on Wasp cards.

Acceptance checks:

- Initiative comes through the typed value/contribution query rather than a turn-controller field or UI value.
- The highest current Initiative among unresolved cards is queried again after every resolution rather than cached as a complete order.
- A cross-team tie uses the current Tie Breaker side and records a deferred flip that occurs only after the selected tied resolver finishes.
- A same-team tie creates an authoritative pending choice for that team/controller and never flips the Tie Breaker.
- Synthetic multi-hero tests cover more than two tied cards even though the current staged fixture uses one hero per team.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-34-resolve-initiative-and-tied-initiative-completion-evidence).

### [x] 3.5 Resolve Hold and complete one turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Both unresolved test cards can perform Hold in Initiative order, become resolved, and end the turn.

Acceptance checks:

- Each hero performs no more than one normal action from its played card.
- A deferred cross-team Tie Breaker flip occurs only after the selected tied card resolves.
- The next actor is re-queried from current unresolved state.
- Turn-only effects have an explicit cancellation checkpoint even though no concrete effect is required yet.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-35-resolve-hold-and-complete-one-turn-completion-evidence).

### [x] 3.6 Resolve test-card Movement within a turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A hero can choose its synthetic Movement offer during its Initiative position, complete the Phase 1 movement flow, resolve its card, and yield to the next actor.

Acceptance checks:

- Movement reuses the allowance, chosen-route, obstacle, command/event, and pending-choice authority from Phase 1.
- The turn controller does not mutate occupancy itself.
- Completing Movement resolves exactly the acting card and continues Initiative order.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-36-resolve-test-card-movement-within-a-turn-completion-evidence).

### [x] 3.6a Connect in-turn Movement to the validation controller

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: After both synthetic cards reveal, the validation controller starts Initiative, exposes the current actor's played-card Movement offers, accepts destination/Stop input through the turn authority, moves the authoritative hero occupancy, resolves the acting card, and visibly advances to the next actor.

Acceptance checks:

- Presentation derives the current actor/card and Movement offers from authoritative turn state rather than restoring the standalone in-hand flow.
- Destination and Stop input resume the same turn-owned Movement pending choice; presentation never mutates occupancy or card state.
- Partial waypoint Movement visibly retains the same actor and refreshes legal destinations; completion updates the hero view and next-actor prompt from authoritative state.
- Focused Edit Mode and Play Mode tests cover the saved-scene reveal-to-Movement-to-next-actor path and invalid/stale input preservation.

Non-goals: No Attack/Defense turn integration, automated action choice, new Movement rules, animation, sound, round shell, or Wasp content.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-36a-connect-in-turn-movement-to-the-validation-controller-completion-evidence).

### [x] 3.7 Resolve test-card Attack and Defense within a turn

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A hero can perform the Phase 2 test Attack during its Initiative position, pause for the enemy Defense reaction, resume, resolve the attacking card, and continue the turn.

Acceptance checks:

- The Defense reaction does not consume the defender's normal action.
- The nested response resumes the same pending Attack and turn frame.
- Attack completion resolves only the attacker's played card.
- The next unresolved actor is selected from current state after the reaction closes.

Non-goals: No Attack consequences, defeat/removal, automated action choice, new Attack/Defense rules, animation, sound, round shell, snapshots, or Wasp content.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-37-resolve-test-card-attack-and-defense-within-a-turn-completion-evidence).

### [x] 3.7b Expand both synthetic heroes to five-card playable hands

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Each current test hero owns five distinct synthetic cards and can commit any eligible card, then perform its supported Primary Attack, Secondary Movement, or general Hold during its Initiative position.

Synthetic fixture matrix (test data only; these names and values are not official content or balance claims):

| Owner | Card | Initiative | Primary action | Secondary Movement | Defense reaction |
| --- | --- | ---: | --- | ---: | ---: |
| Red `primitive-hero` | Red Needle | 13 | non-Ranged Attack 2 | 1 | 1 |
| Red `primitive-hero` | Red Bulwark | 11 | non-Ranged Attack 3 | 1 | 3 |
| Red `primitive-hero` | Red Lancer | 9 | Ranged Attack 2, Range 3 | 2 | 1 |
| Red `primitive-hero` | Red Pursuit | 7 | non-Ranged Attack 4 | 2 | 2 |
| Red `primitive-hero` | Red Bombard | 5 | Ranged Attack 3, Range 2 | 1 | 2 |
| Blue `blue-test-hero` | Blue Feint | 12 | non-Ranged Attack 2 | 2 | 2 |
| Blue `blue-test-hero` | Blue Sentinel | 11 | non-Ranged Attack 3 | 1 | 4 |
| Blue `blue-test-hero` | Blue Scout Shot | 8 | Ranged Attack 2, Range 2 | 3 | 1 |
| Blue `blue-test-hero` | Blue Hammer | 6 | non-Ranged Attack 5 | 1 | 1 |
| Blue `blue-test-hero` | Blue Longbow | 4 | Ranged Attack 3, Range 3 | 2 | 2 |

Acceptance checks:

- Each hand contains exactly five immutable definitions and five globally unique owned instances, with no fixed two-card construction or indexing assumption in production code.
- Every synthetic definition has exactly one printed Primary Attack, one printed Secondary Movement, one in-hand Defense reaction, and the general-rule Hold derived separately; the fixture no longer assigns both Primary Movement and Primary Attack to every card.
- All ten definition/value profiles match the approved fixture matrix, remain explicitly marked synthetic, and preserve the Initiative 11 cross-team tie fixture.
- Commitment, reveal, Initiative, Movement, Attack/Defense, lifecycle, and ownership authorities accept any eligible hand slot and continue to reject stale, cross-owner, discarded, played, or resolved cards without partial mutation.
- Discarding a card for Defense removes only that instance from later normal commitment and Defense choices; the reaction still does not consume the defender's played-card action.
- The director HUD presents both five-card hands and all legal Defense choices legibly without overlap, using wrapping, scrolling, or another bounded layout that remains usable at the validation resolution.
- Focused pure, presentation, and saved-scene tests cover five-card construction, unique identities, the value/action matrix, cross-team Initiative tie behavior, selection from every slot, Defense-driven hand depletion, and representative non-Ranged/Ranged turn resolution.

Non-goals: No official hero identity, official card name/value/text/art, Skill action, card-text effect, modifier, marker, token, Push, Place, minion target, Attack consequence, defeat/removal, balancing, card retrieval, round advancement, snapshot, animation, sound, or Wasp content.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-37b-expand-both-synthetic-heroes-to-five-card-playable-hands-completion-evidence).

### [x] 3.8 Complete the four-turn round skeleton

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Four turns advance, cards occupy the correct turn slots, and the round reaches an explicit end-of-round phase.

Acceptance checks:

- A card cannot be replayed from the wrong state.
- Passing behavior is represented when no card remains.
- Turn and round indices are authoritative and deterministic.
- End-of-round entry is explicit but does not yet resolve its subsystems.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-38-complete-the-four-turn-round-skeleton-completion-evidence).

### [x] 3.8a Connect the complete round to the validation controller

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: The saved validation scene uses one authoritative round instance to play all four indexed turns from commitment through action resolution and visibly reaches the explicit end-of-round phase.

Acceptance checks:

- The validation controller creates and retains one `RoundResolutionAuthority`; it does not replace the round with an isolated commitment, Initiative, or turn authority after reveal.
- Red and Blue commitment, legal Pass, joint reveal, current-actor selection, Hold, Movement, Attack/Defense, and every pending answer route through the round's current turn without presentation-owned state mutation.
- After both revealed cards resolve, the completed immutable slot remains recorded and the controller visibly advances to the next indexed turn with only currently eligible hand cards; a Defense-discarded or previously played card cannot reappear as a legal commitment or reaction.
- Tie Breaker state carries visibly and authoritatively between turns, including a deferred flip completed during one turn before the next turn begins.
- After turn 4, the scene clearly displays the explicit end-of-round phase and rejects further commitment or turn commands without mutating round, card, occupancy, or event state.
- Focused Edit Mode and saved-scene Play Mode scenarios exercise one uninterrupted four-turn round containing representative Hold, Movement, Attack/Defense, Defense-driven hand depletion, and stale/invalid input preservation.

Non-goals: No card retrieval, next-round start, end-of-round subsystem resolution, snapshots, Undo/restart, event-log panel, automated action choice, Attack consequences, animation, sound, official content, or production UI.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-38a-connect-the-complete-round-to-the-validation-controller-completion-evidence).

### [x] 3.8b Generalize turn commitments and round slots for hero ownership

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: For every configured hero, its controlling player must commit exactly one hero-owned card when that hero's hand is nonempty and may Pass only when it is empty; after every hero has a response, each indexed turn slot stores the resulting immutable ordered hero-assignment collection without assuming exactly one hero per team.

Acceptance checks:

- Replace side-owned commitment responses with hero-owned responses keyed by canonical `HeroId`. For each configured hero, a nonempty authoritative hand requires exactly one owned card commitment and an empty hand requires an explicit Pass; hero board presence never permits Pass, and an invalid in-hand lifecycle state rejects as an authority error rather than being treated as an empty hand.
- Joint reveal occurs only after every configured hero has independently received a commitment or Pass response from its controlling player. Commitment and assignment construction validate card ownership and the participant graph, reject unknown or duplicate hero/card identities and responses, and require exactly one response and assignment for every expected hero.
- Replace fixed Red-card/Red-pass and Blue-card/Blue-pass slot members with immutable entries carrying canonical `HeroId` plus exactly one committed card ID or explicit empty-hand Pass outcome. Resolve `PlayerId` and team through `MatchParticipants`; do not duplicate them as independently supplied assignment facts, and reject a broken one-player-to-one-hero mapping.
- The current synthetic 1v1 fixture produces exactly two deterministic responses and assignments per turn while preserving existing reveal, Initiative, action, Tie Breaker, slot-history, and end-of-round behavior.
- Slot and round events expose the collection without mutable aliases or Red/Blue positional assumptions, so milestone 3.9 can serialize it without a known six-player schema replacement.
- Focused and directly affected Edit Mode tests cover mandatory commitment from a nonempty hand, Pass from an empty hand, illegal Pass while any owned card remains in hand, the same requirement for an off-board hero, invalid in-hand state rejection, wait-for-every-player reveal, deterministic ordering, immutability, malformed assignment rejection, four-turn history, and unchanged visible-round controller projections.

Non-goals: No six-player setup, additional heroes or cards, 3v3 turn execution, snapshot serialization, history UI, event-log UI, networking, or production presentation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-38b-generalize-turn-commitments-and-round-slots-for-hero-ownership-completion-evidence).

### [x] 3.8c Scaffold end-of-round progression and repeated synthetic rounds

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: After the fourth turn, the synthetic round advances through an explicit resumable end-of-round stage sequence, retrieves eligible cards, preserves the completed round, and starts turn 1 of the next round so the validation scene can play at least two uninterrupted rounds.

Acceptance checks:

- Add one authoritative end-of-round stage cursor whose ordered checkpoint identities match the verified sequence later completed by milestones 6.1-6.13 and 7.5-7.8; pending choices or a game-ending result can pause or stop the cursor without presentation-owned continuation.
- The current visibly test-only fixture declares which later match subsystems are unavailable. A stage may report a typed `NotApplicable` outcome only from authoritative fixture capability/state facts; a missing handler for a subsystem declared available is an authority error, never a silent skip.
- Implement the reusable owner-validated retrieval subset needed by the current fixture: every eligible resolved or Defense-discarded synthetic card returns to its owning hero's hand through explicit lifecycle events, ineligible cards do not move, and the retrieval checkpoint exposes the cancellation boundary even though the fixture has no concrete Active effects.
- After every applicable stage completes, freeze the four indexed turn slots as an immutable completed-round record, increment the authoritative round index, create fresh empty turn slots, retain match-wide state including the current Tie Breaker, and enter turn 1 commitment without reconstructing state from presentation.
- The saved validation scene continues from its visible end-of-round state into the next round and can complete at least two uninterrupted rounds containing representative Hold, Movement, Attack/Defense, legal card reuse after retrieval, and deterministic Tie Breaker carryover.
- Focused pure and saved-scene tests cover stage order, resumable cursor state, explicit unavailable/not-applicable outcomes, missing-required-handler rejection, owner-safe retrieval, prior-round immutability, round-local reset, match-state carryover, and two-round deterministic replay.

Non-goals: No Minion Battle behavior, token/marker cleanup behavior, Lane Push resolution, concrete End of Round effects, leveling/upgrades, Pity Coins, victory implementation, full-map setup, snapshots, Undo/restart, event-log UI, official content, networking, or production presentation. Later milestones populate the established checkpoints rather than redesigning the round boundary.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-38c-scaffold-end-of-round-progression-and-repeated-synthetic-rounds-completion-evidence).

### [x] 3.9 Serialize and restore authoritative snapshots

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: A versioned snapshot round-trips without changing logical state.

Acceptance checks:

- Teams, players, heroes and their enforced one-to-one player/hero relationship, Tie Breaker, current and completed round state including every collection-backed hero card-or-empty-hand-Pass assignment, the end-of-round stage cursor/outcomes, board occupancy, card instances/state facets, and definition references survive round-trip tests.
- A state paused on a pending card choice or Defense reaction resumes to the same result and ordered events.
- Snapshot schema version and card-definition content revision are checked independently; mismatches are rejected clearly until a migration exists.
- Presentation can rebuild from restored state.

Non-goals: No player-facing save slots or cloud storage.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-39-serialize-and-restore-authoritative-snapshots-completion-evidence).

### [x] 3.10 Add director-mode Undo and restart

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Undo restores the snapshot before the last committed command; restart restores the initial scenario.

Acceptance checks:

- Invalid commands do not create undo entries.
- Undo restores authoritative card, turn, Tie Breaker, pending-resolution, and board state plus presentation.

Non-goals: No redo requirement and no competitive availability.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-310-add-director-mode-undo-and-restart-completion-evidence).

### [x] 3.10a Replace snapshot-history Undo with deterministic command replay

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Director Undo removes the latest accepted gameplay command, reconstructs the authoritative match from its immutable initial scenario inputs, and replays the remaining accepted command prefix through the normal rules authorities; Undo history no longer stores a full authoritative snapshot per command.

Acceptance checks:

- One immutable initial-scenario description contains every input required for deterministic reconstruction, including participant/content configuration, selected map identity/revision, initial placement and card state, and the chosen initial Tie Breaker result. Any future nondeterministic result must be captured as an explicit replay input rather than generated again during replay.
- The history records typed accepted director commands and every explicit player/director choice by stable rules-object identity. Rejected commands and presentation-only interactions create no history entry.
- Replay starts from a fresh authoritative setup and resubmits the retained commands through the same public rules-authority command paths used during live play; it does not use inverse-action handlers, mutate state from presentation, or restore an earlier per-command snapshot.
- Undo removes exactly the latest accepted command and atomically rebuilds authoritative card, turn, Tie Breaker, pending-resolution, end-of-round, completed-history, and board state. Restart clears the command log and rebuilds the unchanged initial scenario.
- The read-only rule-event log is regenerated from the replayed accepted events, and presentation rebuilds hero/obstacle positions, active actor/card focus, pending highlights, Defense controls, and HUD state from the reconstructed authority.
- Focused tests compare live execution with reconstruction after every retained command prefix, including commitments/reveal, Initiative choice, Hold, multi-step Movement and Stop, Attack target and Defense response, end-of-round continuation, card retrieval, and entry into a repeated round. The resulting authoritative snapshot and event-log projection must be byte-for-byte deterministic for the same setup and command prefix.
- A replay rejection or determinism mismatch fails clearly without exposing a partially rebuilt match.
- Existing versioned snapshot capture/restore remains available as an orthogonal persistence, diagnostics, and future network-resynchronization boundary, but snapshots are not Undo entries.

Non-goals: No Redo, inverse-action framework, competitive Undo, gameplay-rule changes, snapshot removal, save-slot UI, networking, or production presentation.

Corrective sequencing amendment (2026-08-11): The user rejected snapshot-stack Undo in favor of deterministic initial-setup plus accepted-command replay. Milestone 3.10a therefore became the next required milestone after completed 4.2a; documentation-governance milestone 3.11a follows it before 4.3. Prior 3.10 and 3.11 completion notes remain historical evidence of the behavior that existed when this amendment was recorded, not the corrective target architecture. This roadmap change did not itself authorize implementation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-310a-replace-snapshot-history-undo-with-deterministic-command-replay-completion-evidence).

### [x] 3.11 Add the internal rule-event log

Required Codex profile: GPT-5.6 Sol with high reasoning.

Observable outcome: A primitive internal panel shows accepted ordered rule events and rebuilds consistently after restore, Undo, or restart.

Acceptance checks:

- Each entry shows sequence, event type, stable rules-object references, and the key value/choice needed to diagnose the event.
- Rejected commands do not appear as accepted state-change events; any rejection display is clearly separate.
- Undo removes events after the restored command boundary, restart clears to setup events, and snapshot restore reproduces the same log projection.
- The log is a read-only projection and cannot mutate or continue rules resolution.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass.

Non-goals: No final visual styling, export format, analytics, telemetry, or player-facing match history.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-311-add-the-internal-rule-event-log-completion-evidence).

### [x] 3.11a Consolidate living-document ownership, history, and routing controls

Required Codex profile: GPT-5.6 Sol with medium reasoning.

Observable outcome: Routine startup and targeted context loading stays bounded by explicit per-file routing thresholds, while the complete precise current authority remains in indexed active documents and detailed historical completion/test evidence remains losslessly preserved in indexed phase archives excluded from normal startup loading.

Acceptance checks:

- One version-controlled `Docs/AI/DOCUMENTATION_GOVERNANCE.md` ownership matrix names exactly one canonical home for current authorization/routing, roadmap sequencing, product/architecture contracts, rule authority, card/effect contracts, testing policy/active recipes, and historical evidence. A canonical home may be a compact index plus subject/phase shards; noncanonical documents use short routing summaries and stable links rather than copied narratives.
- The ownership matrix establishes one bounded active technical-risk register. Each retained risk records its concrete failure mode/impact, evidence and confidence, trigger or detection method, earliest affected milestone, planned retirement/reassessment milestone, and approved mitigation or explicit acceptance; resolved risks move to indexed history instead of accumulating indefinitely in the active register.
- `SESSION_BRIEF.md` is compacted to current implementation, authorization, next milestone/profile, boundaries, and routing; `PROJECT_CONTEXT.md` retains only current durable scope/architecture/workflow; card/effect documents retain only durable contracts; `RULES_LEDGER.md` retains source authority, rule status, open questions, and deviations.
- Before consolidation edits, `Docs/AI/Archive/Pre-Governance/` receives an immutable copy of every governed source document plus a SHA-256 inventory. This preservation baseline is not the post-migration canonical authority, but it makes the complete original text recoverable without relying on Git history.
- `ROADMAP.md` remains the compact status/sequencing index and route to exact active/unstarted milestone definitions; those definitions may move intact into indexed phase shards when needed. Detailed completed-milestone narratives and validation evidence move verbatim to indexed `Docs/AI/Archive/` phase handoff files with stable backlinks; no evidence is deleted, reduced to a summary, or left only in Git history.
- `TESTING.md` retains or indexes current policy, launcher resilience, active/future exact recipes, gate definitions, and test ownership. If that precise current material outgrows one file, it splits into indexed authoritative subject shards. Superseded command blocks and historical run totals move verbatim to the applicable indexed archive without weakening reproducibility or the fixed-launcher rule.
- The governance contract establishes measured per-file word/byte routing thresholds, with a warning at 85 percent and a handoff-blocking hard threshold. Initial hard thresholds for each active leaf file are: `SESSION_BRIEF.md` 2,000 words/20 KiB; `PROJECT_CONTEXT.md` 4,500 words/40 KiB; `ROADMAP.md` 15,000 words/128 KiB; `TESTING.md` 7,000 words/80 KiB; each card/effect architecture leaf 7,000 words/64 KiB; and each rules-ledger leaf 9,000 words/80 KiB. These are not aggregate corpus limits. Before enforcement, inventory the irreducible precise content; if one proposed threshold is inadequate, create an indexed authoritative shard or obtain an explicit governance amendment rather than abbreviating content or silently relaxing the validator.
- `Docs/AI/Archive/MIGRATION_MANIFEST.md` maps every removed, relocated, or rewritten source section by original path/heading to its destination anchor, information category, and treatment (`verbatim archive`, `authoritative shard`, or `non-authoritative routing-summary replacement`). Verbatim entries include source/destination integrity hashes. No manifest entry may use a summary as the sole destination for authority, rules, sources, deviations, open questions, milestone acceptance/non-goals, exact active recipes, or historical result/failure evidence.
- A before/after preservation inventory accounts for milestone IDs/statuses/profiles and exact acceptance/non-goal checks; rule entries, citations, statuses, deviations, and open questions; card/effect contract entries; test commands and gate definitions; historical pass/failure records; and rights/privacy boundaries. Every item remains exact in an active canonical shard or a lossless archive destination as appropriate.
- A project-local `Tools/Validate-LivingDocumentation.ps1` check enforces per-file thresholds, required headings, unique current/next milestone and profile routing, roadmap/session/config agreement, valid internal paths/anchors where mechanically checkable, archive indexes/backlinks, migration-manifest destinations and hashes, preservation-inventory coverage, and absence of copied completion-note bodies in noncanonical active documents. The check must not become passable merely by deleting an oversized section.
- `AGENTS.md` routes agents through the ownership matrix and requires ownership review at phase gates or whenever an active leaf reaches its warning threshold. Completed history is archived losslessly; irreducible current material is sharded authoritatively. Neither operation changes authority, rule meaning, milestone status, validation evidence, or Git history.
- Manual review compares the preservation baseline, manifest, inventories, active shards, and archives and confirms that no source claim, approved deviation, open question, acceptance/non-goal check, exact active test recipe, historical pass/failure evidence, or rights/privacy boundary was lost or semantically changed. The validator and `git diff --check` pass; Unity is not launched because this is documentation/tooling-only work.

Non-goals: No gameplay, Unity runtime, test expectation, roadmap feature scope, rule interpretation, snapshot, build, or presentation change; no deletion or lossy summarization of authoritative or historical evidence; no aggregate cap on the documentation corpus; no external documentation service or generated website.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_3.md#milestone-311a-consolidate-living-document-ownership-history-and-routing-controls-completion-evidence).

Sequencing amendment (2026-08-11): The living documents have grown by repeating chronological completion evidence across active startup, context, roadmap, card-contract, and testing files; `ROADMAP.md` is already approximately 26,600 words and `TESTING.md` approximately 10,300. Milestone 3.11a is required immediately after 3.10a and before 4.3 so future milestone work reads smaller canonical active documents and appends evidence to one governed destination. This amendment plans the consolidation but does not perform or authorize it.

Phase-gate roadmap audit: After milestone 3.11 passes its T3 gate, audit Phases 4-7 before assigning the next milestone profile. Reassess milestone packaging and sequencing around the shortest dependency-safe path to milestone 7.9's complete deterministic synthetic match, while preserving the full-map, general-rule, snapshot, progression, and victory evidence required by that hard gate; recommend any justified keep/merge/split/defer/remove decisions and record the remaining-effort range.

Phase 4-7 audit (2026-08-10): Keep milestones 4.1-4.4, 5.1-5.8, 6.1-6.8, and 7.1-7.9 separate and in their current order. No merge is justified because each item owns a distinct source/data, rules-authority, continuation, presentation, or validation boundary; the deliberately non-executable Lane-Push prefix at 6.5 must remain separate from the live transition at 6.6. No further split is justified now that Phase 3 has proven stable snapshot, Undo, event-log, round-cursor, and pending-choice seams. Nothing can be deferred or removed without weakening milestone 7.9's required full-map Movement/combat, minion/token/Lane-Push, defeat/respawn, progression, victory, or exact end-of-round evidence. The shortest dependency-safe path is therefore the existing sequential path through the Phase 4 map/setup gate, Phase 5 general combat gate, Phase 6 Lane/end-of-round component gate, Phase 7 progression/victory integration, and milestone 7.9 hard gate. Remaining effort is 29 separately approved micro-milestones, estimated at 26-43 focused implementation days excluding approval latency, source ambiguity, and environment outages.

Roadmap sequencing correction (2026-08-12, user approved): The Phase 6 portion of that audit is superseded for unstarted work. Milestones 6.4-6.8 implement reusable direct Move, Object Push, Place, Spawn, board-object Swap/Replace, standalone Remove, Skill/control flow, modifiers, permissions, immunity, and effective characteristics with synthetic effects before the complete synthetic-core gate. Former milestone 5.7 moves behind core Push as 6.4a so after-action displaced-minion correction is integrated against a real displacement capability rather than an isolated drill; 6.4b then exposes its authority-owned route and fallback-Place choices by reusing the shared minion-choice UI interface established by 6.1a. The former Lane-Push and cleanup milestones 6.4-6.8 are renumbered 6.9-6.13 without weakening their behavior, evidence, or gate roles. This adds six net separately approved micro-milestones, estimated at 11-20 focused implementation days excluding approval latency, source ambiguity, and environment outages; it authorizes no implementation.

Post-4.1 multi-map readiness correction (2026-08-11): The reviewed inventory is valid, but an independent audit found that the executable board/snapshot state does not yet carry selected-map identity, the data contract explicitly scopes only `SpaceId` rather than every map-owned child identity, and the validator combines reusable structural checks with `official-single-lane`-specific counts and digest checks while omitting rules-relevant zone metadata from that digest. Split the former milestone 4.2 into corrective milestone 4.1a for the general identity/validation boundary, milestone 4.2 for selected-map authoritative loading and snapshot binding, and milestone 4.2a for Unity placeholder presentation. This adds no second playable map: bounded synthetic definitions may reuse coordinates and child IDs only as automated collision/singleton tests. The revised shortest dependency-safe path contains 30 remaining separately approved micro-milestones through 7.9, estimated at 28-46 focused implementation days under the same exclusions.

Post-4.2a architecture, performance, and documentation-governance audit (2026-08-11): Preserve the verified pure rules, command/event, pending-choice, snapshot, and map boundaries, but do not carry the current validation-controller shape or duplicated living-document history into Phase 5. Milestone 3.11a first consolidates canonical active documentation after corrective Undo milestone 3.10a. The controller has accumulated fixture construction, low-level authority ownership, command translation, Undo/restart, event-log projection, selection, and immediate-mode HUD rendering, while the shared card-action authority now contains both Movement and Attack/Defense orchestration. Insert behavior-preserving milestones 4.4a-4.4d after the complete configured Phase 4 setup and before Phase 5: establish a transport-neutral match application service, decompose card actions behind typed strategy handlers, replace the god-controller with feature presenters and passive Unity views, then close the identified allocation/resource/parser debt and preserve the already-corrected supported Unity object-discovery baseline at the final Phase 4 architecture gate. Independent review milestones 4.4e, 5.8a, and 7.9a then inspect the accumulated implementation before Phase 5, Phase 6, and verified hero content respectively. This ordering gives the refactor the complete Phase 4 state shape, keeps each seam independently testable, and prevents Phase 5 effects from deepening the temporary controller/resolver concentration. The shortest dependency-safe path now contains 36 remaining separately approved micro-milestones through 7.9a, estimated at 37-65 focused implementation/review days excluding approval latency, source ambiguity, and environment outages. This amendment plans no further runtime change and authorizes none of the inserted milestones.

Scheduling addendum (2026-08-12): The independent pre-Phase-6 review called 5.8a in the dated audit above is renumbered to 5.8c. New milestone 5.8a first closes the configured minion Attack-target presentation gap, and new milestone 5.8b then adds an authority-computed mouse-hover Defense breakdown before the independent review evaluates the complete intended Phase 5 interaction surface. None of these milestones is authorized by this scheduling decision.

Technical-risk-control amendment (2026-08-11): Embed complementary prevention and detection controls in their existing owning milestones instead of adding more roadmap gates. Milestone 3.11a establishes bounded active risk ownership; 4.4a-4.4d establish executable architecture rules, deterministic replay/invariant evidence, compatibility fixtures, and performance regression thresholds; 7.9 adds complete-match soak evidence plus one disposable Win64 smoke; 8.27 explicitly retires staged participant/content assumptions; and 8.29-8.36 pin dependencies and exercise the private-online trust, authorization, confidentiality, fault, reconnect, and diagnostic model. Independent reviews retain a finding-disposition table. These additions do not authorize implementation, introduce production-scale optimization/security claims, or change the current 3.10a -> 3.11a -> 4.3 sequence.
