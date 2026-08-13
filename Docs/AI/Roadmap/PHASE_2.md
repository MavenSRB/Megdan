# Phase 2 - Two-hero combat and Tie Breaker state

Canonical active roadmap shard. Return to [ROADMAP.md](../ROADMAP.md). Historical completion evidence: [PHASE_2 handoffs](../Archive/Phase-Handoffs/PHASE_2.md).

## Phase 2 - Two-hero combat and Tie Breaker state

### [x] 2.1 Introduce teams, players, heroes, and stable IDs

Observable outcome: The model supports collections of teams, players, and heroes while the test configuration creates one Red and one Blue hero.

Acceptance checks:

- No rule API assumes exactly one hero per team.
- Team, player, and hero IDs are nonempty, unique, and resolve through collections.
- The existing primitive hero ID remains stable, and Phase 1 card-instance owner references still resolve after the state is expanded to Red/Blue teams and players.

Non-goals: No 3v3 UI or multiplayer behavior.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-21-introduce-teams-players-heroes-and-stable-ids-completion-evidence).

### [x] 2.2 Extend the test cards with Attack and Defense action offers

Observable outcome: The Phase 1 placeholder definitions gain non-Ranged Attack and Defense-reaction action definitions, from which the authority derives typed runtime offers owned by the correct test heroes.

Acceptance checks:

- Test-card Attack and Defense values are typed printed values on action definitions and are queried through runtime offers, not UI or scene constants.
- The Attack card has a printed Primary, non-Ranged Attack definition that derives the eligible runtime offer.
- A defending card definition declares an eligible Defense reaction from hand and discard as its cost/state transition, without executing that reaction yet or pretending it is a normal turn action.
- Every card used to defend is associated with exactly one hero's hand.
- The minimal card-state facets support in-hand and discarded locations without pretending to implement the complete turn lifecycle.
- Invalid Attack/Defense values, missing owners, and cross-owner card use are rejected by focused tests.

Non-goals: No Wasp data or text, ranged attacks, card tiers/colors, initiative, items, card effects, Block, turns, or animations.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-22-extend-the-test-cards-with-attack-and-defense-action-offers-completion-evidence).

### [x] 2.3 Initiate a basic adjacent hero Attack

Observable outcome: One test hero can select its non-Ranged Attack offer and target the adjacent enemy test hero through the shared command and resumable-resolution boundary.

Acceptance checks:

- Only an adjacent enemy hero is a legal target.
- The attacker cannot target itself, a friendly hero, a non-adjacent hero, an empty space, terrain/token, or another non-unit obstacle; heroes/minions remain obstacles for occupancy without ceasing to be unit targets.
- The initial Attack Total comes through the value-query service and equals the selected offer's printed Attack contribution when no modifiers exist.
- Validation failure does not mutate state or consume a card.
- The resolver opens an explicit Defense response choice owned by the targeted hero and emits an ordered Attack-started event for presentation.

Non-goals: No ranged targeting, minions, items, modifiers, Before/After Attack text, Wasp effects, Defense-card selection/resolution, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-23-initiate-a-basic-adjacent-hero-attack-completion-evidence).

### [x] 2.3a Initiate a basic Ranged hero Attack

Observable outcome: One test hero can select a synthetic Ranged Attack offer and target the enemy test hero within the offer's typed printed Range through the same shared target-query, command, and resumable-resolution boundary introduced by milestone 2.3.

Acceptance checks:

- A synthetic test-card definition declares a printed Primary Ranged Attack with typed Attack and Range values, and the authority derives both through the selected runtime offer rather than UI or scene constants.
- An enemy hero is a legal target when its rulebook hex distance from the attacker is no greater than the current Range Total; Range 1 includes adjacent targets and the printed Range is the complete Range Total when no modifiers exist.
- Rulebook distance and Range are queried explicitly rather than inferred from movement paths. Occupied spaces and other obstacles count normally and do not create an invented line-of-sight restriction.
- The attacker cannot target itself, a friendly hero, an out-of-range hero, an empty space, terrain/token, or another non-unit obstacle.
- Accepted Ranged and non-Ranged hero targets enter the same pending Attack/Defense response flow and emit the same ordered Attack-started event shape; range classification changes target eligibility, not resolution ownership or presentation authority.
- Invalid, forged, stale, or out-of-range attempts do not mutate state, consume a card, or open a Defense response.
- Focused tests cover Range 1 adjacency, a target exactly at maximum Range, a target beyond Range, and an in-range target across occupied or obstacle spaces.

Non-goals: No minion targets, Range modifiers from card text/items/Active effects, Before/After Attack text, Defense-card selection/resolution, Block, full-map combat integration, Wasp data or effects, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-23a-initiate-a-basic-ranged-hero-attack-completion-evidence).

### [x] 2.4 Defend with one test card or decline

Observable outcome: The targeted hero answers the pending Defense reaction by discarding exactly one legal card from its own hand for its Defense value or declining, after which the Attack resolution resumes.

Acceptance checks:

- A Defense card can be chosen only from the targeted hero's current hand.
- Exactly one chosen card moves from its hand location to its discard location while retaining the correct orthogonal state facts.
- A discarded card and an attacker's selected card cannot be reused as the Defense card.
- Declining, having no card, or lacking a legal card produces no Defense Total.
- With no modifiers, the value-query service reports only the discarded card's printed Defense contribution and the Defense Total equals it.
- Defense does not count as the defender's normal turn action, and its action context remains distinct from a played Primary/Secondary action.
- Invalid or stale response commands leave the card and pending Attack unchanged.
- Focused tests reject cross-owner, already discarded, missing, and multiple-card Defense attempts.

Non-goals: No primary Defense card text, items, minion modifiers, Block, Defense/Skill behavior, defeat consequences, turns, or animation.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-24-defend-with-one-test-card-or-decline-completion-evidence).

### [x] 2.5 Compare Attack and Defense and report the test outcome

Observable outcome: The authority completes the basic test Attack/Defense sequence and reports either successfully defended or attack successful.

Acceptance checks:

- Defense Total equal to or greater than Attack Total successfully defends.
- Defense Total below Attack Total results in a successful attack.
- Declining or being unable to defend results in a successful attack.
- Resolution resumes the same pending Attack, closes its Defense window, and emits reproducible ordered events.
- The presentation clearly identifies the result without inventing Life, coin, respawn, or other deferred consequences.

Non-goals: A successful test attack does not yet implement full hero defeat, removal, unresolved-card resolution, active-effect cancellation, markers, coins, assists, Life spending, victory, or respawn.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-25-compare-attack-and-defense-and-report-the-test-outcome-completion-evidence).

### [x] 2.6 Add Tie Breaker setup state

Observable outcome: Normal setup chooses the initial Tie Breaker side; internal tests can explicitly select Red or Blue.

Acceptance checks:

- The result is stored in authoritative state and included in later snapshot history.
- No general gameplay RNG subsystem is introduced.
- T3 phase gate: both complete unfiltered Edit Mode and Play Mode suites pass after the Phase 2 focused scenarios.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-26-add-tie-breaker-setup-state-completion-evidence).

### [x] 2.6a Display the authoritative Tie Breaker side

Observable outcome: The validation HUD shows a top-right Tie Breaker coin indicator that clearly identifies whether the current authoritative side is Red or Blue.

Acceptance checks:

- The indicator derives from `TieBreakerState`; presentation does not choose, cache independently, or mutate the side.
- Red and Blue states are visibly distinct through explicit text/symbol treatment rather than color alone.
- Focused Edit Mode and Play Mode tests cover both deterministic sides and the saved-scene presentation path.
- One targeted visual inspection confirms the top-right indicator is readable and does not overlap existing content.
- T3 corrective gate: both complete unfiltered Edit Mode and Play Mode suites pass because this changes the already-passed Phase 2 presentation boundary.

Non-goals: No Initiative ordering, Tie Breaker flip timing, coin animation, production art, sound, or player control.

Historical evidence: [lossless completion record](../Archive/Phase-Handoffs/PHASE_2.md#milestone-26a-display-the-authoritative-tie-breaker-side-completion-evidence).

Phase-gate roadmap audit: After milestone 2.6 passes its T3 gate, audit Phase 3 before assigning the next milestone profile. Confirm that its lifecycle, Initiative, turn, round, snapshot, Undo/restart, and event-log boundaries reach one complete deterministic round through the shortest dependency-safe sequence; recommend any justified keep/merge/split/defer/remove decisions and record the remaining-effort range.

Phase 3 entry audit (2026-08-10): Keep milestones 3.1-3.11 as separate, unchanged micro-milestones; no merge, split, defer, or removal is justified. The shortest dependency-safe path is lifecycle/typed Initiative data (3.1), two-hand selection presentation (3.2), simultaneous play/reveal (3.3), Initiative and Tie Breaker ordering (3.4), the minimum Hold turn (3.5), existing Movement and Attack/Defense turn integrations (3.6-3.7), the four-turn round shell (3.8), then the versioned snapshot boundary (3.9), snapshot-backed Undo/restart (3.10), and the read-only event-log projection plus T3 gate (3.11). This ordering reaches one complete deterministic round before adding persistence tools, and places serialization before every tool that restores or rebuilds history. Remaining effort is 11 separately approved micro-milestones, estimated at 7-12 focused implementation days excluding approval latency and environment outages.

Approved content-readiness amendment (2026-08-10): Insert milestone 3.7b after the completed Attack/Defense turn integration and before the four-turn round shell. The current two-card mirrored synthetic fixtures cannot exercise four card selections per hero, hand depletion through Defense discards, or the rulebook-shaped five-card starting hand. Milestone 3.7b expands only synthetic data and its existing presentation/authority consumers; it does not begin verified hero content or add card-text effects. The remaining Phase 3 path is now five separately approved milestones (3.7b and 3.8-3.11), estimated at 4-7 focused implementation days excluding approval latency and environment outages.

Corrective playable-round amendment (2026-08-10): Insert milestone 3.8a after the completed pure four-turn round skeleton and before snapshot serialization. Validation after milestone 3.8 confirmed that the round authority completes four deterministic turns, while the saved-scene controller still constructs and presents only an isolated revealed-card turn flow. Milestone 3.8a closes that integration gap before snapshots, Undo/restart, and the event log depend on the visible round boundary. The remaining Phase 3 path is four separately approved milestones (3.8a and 3.9-3.11), estimated at 3-5 focused implementation days excluding approval latency and environment outages.

Approved six-hero round-history amendment (2026-08-10), rule-corrected 2026-08-10: Insert milestone 3.8b after the completed visible-round integration and before snapshot serialization. The staged commitment authority and `RoundTurnSlot` previously hard-coded one Red and one Blue response/assignment, while the authoritative rule requires each hero's controlling player to commit exactly one hero-owned card from a nonempty hand or Pass with an empty hand. The approved pitch requires three independently controlled heroes per team plus a later UI that shows which hero played which card in each of four turns. Because `MatchParticipants` enforces exactly one hero per player, `HeroId` is the canonical gameplay/assignment owner; `PlayerId` remains the distinct controller/seat identity and player/team are derived through the participant graph instead of duplicated in each assignment. Milestone 3.8b completed that generalization before snapshots freeze the shape. Milestones 8.25 and 8.26 must later populate and visibly project six assignments, and milestone 9.13 must polish that same read-only history. The remaining Phase 3 path is four separately approved milestones (3.8c and 3.9-3.11), estimated at 3-6 focused implementation days excluding approval latency and environment outages.
