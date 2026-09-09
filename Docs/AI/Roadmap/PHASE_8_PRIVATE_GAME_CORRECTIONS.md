# Phase 8 - Private-game corrections

Canonical outcomes approved after the independent review. Return to [ROADMAP.md](../ROADMAP.md) and [Phase 8](PHASE_8.md). Completion evidence is indexed in the [Phase 8 handoff](../Archive/Phase-Handoffs/PHASE_8.md).

## Authorized private-game reliability corrections

The user approved **8.36b-8.36d** on 2026-09-09 as a bounded sequence, executed in order with separate validation and commits. The project is now for the user and friends only; production presentation and publisher-pitch work are parked. More heroes are desired but are not part of this correction sequence. The proposed outcomes and closing checks below are approved; historical proposal wording does not withhold that authorization.

### [x] 8.36b Authenticate keyed-session seat forfeiture

Origin: [8.36a F-ONLINE-01](PHASE_8_PRIVATE_ONLINE_REVIEW.md); approved on 2026-09-09. Dependency: 8.36a, complete. Outcome: an unauthenticated leave cannot release a protected seat; authenticated deliberate leave still releases it. Acceptance: the review's exact `KeyedPlaintextGoodbyePreservesReservationAndCapability` check fails before correction and passes afterward, with authenticated leave, tamper/replay, reconnect and directly affected adapter regressions; record the protocol compatibility assessment. Non-goals: accounts, rules changes, hosting redesign or another playtest. Proposed Phase 9 blocker.

Historical evidence: [8.36b handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836b-authenticate-keyed-session-seat-forfeiture-completion-evidence).

### [x] 8.36c Keep transport service bounded under backpressure and ingress

Origin: [8.36a F-ONLINE-02 and F-ONLINE-03](PHASE_8_PRIVATE_ONLINE_REVIEW.md); approved on 2026-09-09. Dependency: after 8.36b to validate the final departure contract together. Outcome: a non-reading or flooding peer cannot monopolize host control or unbounded adapter resources. Acceptance: the review's exact `NonReadingPeerCannotBlockHostPumpOrDispose` and `TransportIngressRemainsBoundedBeforeAdmission` checks fail before correction and pass afterward; preserve ordered authorization, reserved-seat pause, reconnect and healthy-peer service. Select testing breadth from the affected transport and build paths. Non-goals: production service, host migration, gameplay timeouts or rules changes. F-ONLINE-02 is a proposed Phase 9 blocker; F-ONLINE-03 shares the correction's transport boundary.

Historical evidence: [8.36c handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836c-bounded-private-transport-completion-evidence).

### [x] 8.36d Preserve attributable private-online diagnostic bundles

Origin: [8.36a F-ONLINE-04](PHASE_8_PRIVATE_ONLINE_REVIEW.md); approved on 2026-09-09. Dependency: 8.36c, before the next captured online run. Outcome: ordinary-lobby evidence identifies its exact run and build and cannot silently reuse another cohort's logs. Acceptance: `OnlineEvidenceBundleRejectsMixedRunIdentity` fails on mixed-run/build evidence and passes a coherent bundle, including the declared participants' terminal outcome/version; retain privacy and distinct output directories. Non-goals: telemetry, upload, repeat playtest or recovering the accepted old-build internet session. Not a reopening of 8.36 or independently a Phase 9 blocker.

Historical evidence: [8.36d handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836d-attributable-private-diagnostics-completion-evidence).

### [x] 8.36e Clarify the private lobby and hero picker

User-approved on 2026-09-09, to follow 8.36b-8.36d. Outcome: clicking a hero visibly marks the selection; lobby seats show hero names only after an actual pick, never placeholder assignments; hovering a hero shows its source overview card from the Docs hero-selection-card assets; the pre-match screen hides the map behind a clean backdrop with the existing game logo above the picker and reveals the board only when the match loads. Dependency: complete 8.36d and inspect the supplied overview/logo resources. Closing checks: focused selection/unpicked-seat/hover/state-transition regressions, actual pointer-driven configured lobby-to-match Play Mode coverage, and targeted visual inspection at supported viewports. Preserve authoritative pick restrictions, readiness, bot selection and reconnect. No new heroes, invented art, public distribution or general presentation overhaul.

The user also included disappearing lobby buttons in this outcome. Exact reported reproduction: Host then Leave, or Join then Leave, returns to NOT CONNECTED with only Host/Join buttons and an incomplete setup screen. Test both paths and repeated retries without restarting; restore the full applicable setup controls. Also verify controls across pick, ready, reconnect and return transitions remain visible, reachable and functional without exposing unauthorized actions.

Also approved: bots randomly select from their currently legal hero choices instead of always selecting the first available. Preserve authoritative pick order and all selection restrictions; inject/record the selection input at the bot boundary so focused tests can prove varied choices, legal-only selection, reproducibility and no added nondeterminism in rules or replay. This changes draft choice only, not gameplay policy or hero content.

Historical evidence: [8.36e handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836e-private-lobby-and-picker-completion-evidence). The user reiterated that confirmed picks must update for everyone, including the next picker; unconfirmed/default names alone remain hidden. With no existing logo resource found, the approved game-title alternative supplies the wordmark.

### [x] 8.36f Correct reported card-resolution failures and feedback

User-approved on 2026-09-09 after 8.36e. The supplied screenshot reports an unsupported resolver scope for Silverarrow and friendly heroes in radius while Trailblazer is active, followed by a clipped internal-exception banner; the shared live-scope omission was reproduced and corrected under the checks below. Outcome: verified legal Trailblazer actions resolve through the shared typed capability without a host engine fault, and unexpected internal errors produce bounded player-safe feedback with private diagnostic detail retained. Dependency: verify the printed card/rules-ledger authority and reproduce the actual resolver/host path. Closing checks: failing-then-passing source-linked active-Trailblazer regression covering self/friendly/enemy and radius boundaries, directly affected shared capability consumers and host authorization/atomicity; focused banner and player-path coverage where behavior changes. No card-name branches, invented rules, rebalance or broad error-system refactor.

Additional user-approved report: Misa's Watch How I Soar offers its printed "Defeat a minion adjacent to you" branch, permits target selection, but the selected minion survives. Reproduce the actual choice/target/confirmation route, distinguish provisional selection from accepted resolution, verify printed adjacency and any applicable prevention, and correct the demonstrated fault. Add source-linked legal/invalid-target and accepted Defeat state/event tests plus a real-input player journey proving removal and subsequent prompt progression; preserve the alternate placement branch and shared effect-choice semantics. These card-resolution reports share this bounded outcome and get distinct focused regressions; no other card changes are authorized.

Validation disposition approved by the user on 2026-09-09: Windows Application Control blocks the separate .NET test runner (`0x800711C7`). For this correction batch, complete Unity Edit Mode and Play Mode suites replace the blocked harness gate; focused behavior and player-path checks remain required. The implementing agent owns this replacement coverage and records the harness as blocked, not passing. This does not authorize changing or bypassing Windows security, quarantining test failures, or weakening later milestones' validation policy.

Historical evidence: [8.36f handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836f-card-resolution-and-feedback-completion-evidence).

### [x] 8.36f1 Make the level-up comparison and rewards readable

User-approved on 2026-09-09, after 8.36f and before 8.36g. The supplied Misa level-up screenshot shows a tiny unreadable current hand and upgrade choices clipped at the bottom. Outcome: the current hand can be read/compared during leveling and complete upgrade cards/rewards remain accessible inside the viewport. The reward overlay stays centered on its card and reads "GAINS:" followed by the actual authoritative gain icon(s). Preserve whole-card selection and the existing source-ratio/hover/short-viewport card contracts; no change to upgrade or reward rules. Closing checks: focused layout/reward-projection regressions, a pointer-driven level-up journey comparing the hand and selecting an upgrade, and full-screen visual inspection at the user's viewport and the supported short viewport, with no bottom clipping or unreachable choices.

The user clarified during visual validation: comparison cards must not be see-through, particularly the rightmost face over the map. Include opaque inline and hover faces in the closing checks; in-play unavailability must not obscure read-only comparison.

Historical evidence: [8.36f1 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836f1-level-up-comparison-completion-evidence).

### [x] 8.36f2 Correct attack presentation without shrinking the attacker

User-approved on 2026-09-09, after f1 and before g. Report: the camera zooms but the attacking hero shrinks away during the animation. Outcome: a readable zoom followed by a strike/impact; the attacker retains a visible, stable token scale, and the defender remains or is removed according to the authoritative resolved outcome. Reproduce the actual camera/token animation path; distinguish presentation scaling from authoritative Defeat and preserve input/timing ownership. Closing checks: failing-then-passing animation geometry/lifetime regression, actual player-input attacks with surviving and defeated defenders, interruption/rebuild/Undo cleanup where affected, and targeted non-batch visual inspection. No combat rule, damage, targeting or new VFX-system changes.

The reported shrink was traced to HeroRemoved/HeroDefeated using the actor's `hero` reference instead of the affected `target`; HeroRemovalPrevented shares the corrected narration identity. Visual validation also exposed a defender echo fading before the queued strike and card spacing that cleared token centers but overlapped their zoomed discs. These belong to the same attack-readability outcome: retain only decorative echoes until the actual strike, and measure token clearance in projected panel units. Real removed pieces, authoritative outcomes and input ownership must remain unchanged.

The user also expressed concern after two reported failures among roughly ten tried cards from the added roster. This is not a measured roster-wide failure rate. The broader audit and necessary corrections are explicitly approved below.

Historical evidence: [8.36f2 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836f2-attack-presentation-completion-evidence).

### [x] 8.36f3 Audit added-roster cards and correct demonstrated failures

Working inventory, findings and per-card closing checks: [added-roster audit](../Rules/ADDED_ROSTER_AUDIT.md). Open rows are not passing coverage.

User-approved on 2026-09-09: "Yes, audit the cards. And implement the fixes as needed." Execute after f2 and before g. Scope: the recently added configured roster (Dodger, Misa, Silverarrow, Ursafar, Whisper; reconcile the user's "six" against the eleven-hero configured catalog), not new hero content. Inventory every source card and its executable branches, map verified source authority to implementation and actual player-input coverage, and exercise uncovered or suspect paths through the configured UI. Existing green cases may be reused only under the testing policy's affectedness rules, never as assumed proof of untested branches. Correct demonstrated defects using shared typed capabilities, with a failing-then-passing regression per fault and directly affected consumers, restore/replay/Undo and visible-result checks as applicable. Record per-card conclusions, coverage gaps and any unresolved source/rules questions in one indexed audit record; no invented rules or silent quarantine. Select T3 if the shared boundary or unenumerable blast radius triggers it. This audit is implementation-authorized, not a separately mandated independent engineering review. No new heroes, rebalance, unbounded AI changes or public distribution.

Historical evidence: [8.36f3 handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836f3-added-roster-card-audit-completion-evidence).

### [x] 8.36g Improve private-session bot tactics with measured evidence

User-approved on 2026-09-09 after all fixes in 8.36b-8.36f. This reopens the stronger-bot outcome parked at 8.35z78; that entry routes here rather than becoming a duplicate implementation. Outcome: improve the existing lobby bot policy's tactical usefulness, particularly reaching battle, useful legal Fast Travel, and avoiding wasted or repeated ineffective turns. First reproduce the reported decisions and compare against the existing static Tier-2 baseline. Closing checks: fixed-seed scenarios demonstrate the targeted improvements and legal forward progress, paired baseline/candidate match samples expose tradeoffs without claiming universal strength, and measured decision-work caps preserve host responsiveness. Exercise actual lobby bots, not just the practice director. Preserve owner-private information boundaries, authorized commands, deterministic rules/replay and reserved-human pause. No omniscient host-state search, new heroes, rule changes, external AI service or unbounded search; any larger architectural direction requires a separate decision.

The user's 2026-09-09 clarification makes winning the explicit tactical objective: pursue useful minion and hero defeats, team-Life damage and lane pressure; use legal Fast Travel to reach productive combat, and avoid wasting cards or taking predictably losing attacks. Coordinate commitment, movement and targeting using public state and the bot's own hand. Closing comparisons include battle arrival, minion/hero defeats, team-Life outcomes, ineffective turns and paired match outcomes against the current policy. Aggression alone is not acceptance, and a small match sample is not proof of universal strength.

Also explicitly requested: use revealed commitments/initiative and public played, discarded and retrieved cards to assess current threats and possible future cards. Preserve the distinction between known and merely possible cards; public-history inference must never read hidden hands, decks or facedown commitments. Evaluate productive Skill modes, targets and setup effects alongside attacks and movement. Closing scenarios cover reacting to a revealed threat, updating card-availability knowledge after public retrieval, uncertainty when information is hidden, and useful Skill outcomes (including retrieval/discard, removal and positioning). Keep inference and decision work bounded; no perfect-information opponent-hand simulation or unbounded multi-turn search.

Historical evidence: [8.36g handoff](../Archive/Phase-Handoffs/PHASE_8.md#milestone-836g-private-session-bot-tactics-completion-evidence).

### [~] 8.36g1 Host and play from the same client

Explicitly approved on 2026-09-09 after the card audit and bot work, before packaging. Outcome: everyone runs the same client; one player chooses Host and plays a seat while others choose Join. No separate seatless host process is required. The hosting player alone can deliberately access the Director tab for debugging; joining players remain owner-scoped, and bots retain their private seat projections even though their process owns authority. Preserve keyed TCP admission, reconnect, host-loss behavior and existing authoritative rules; no host migration or online-service change. Closing checks: actual same-client host-plus-remote-client lobby, picks and complete local gameplay; host-only Director visibility and authorization; joiners cannot invoke debug commands; host departure/rehost and client leave/rejoin recover correctly; host plus bots works with all configured sizes. Review the trust-model change explicitly: the playing host is trusted and can see or alter authoritative state through debug tools. Validate this final topology before 8.36h packaging.

### [ ] 8.36h Package the corrected private playtest build

Carries the user's standing request for a new build after the fixes and a local ZIP, amended on 2026-09-09 to use the same playing client for Host and Join. Execute after 8.36g1, not in place of any correction or audit. Outcome: a separately named current Windows build and matching ZIP under Builds, with a clear play launcher, in-client Host/Join and version identity; do not require a separate dedicated-host launcher. Closing checks: the applicable complete pre-build regression gate, private-resource checks, required build-and-launch smoke, and inspection of the ZIP contents and launch scripts. Exercise actual packaged host-and-play, joining-client and bot paths; do not infer built-player success from a rehearsal script. Keep prior packages and private evidence. No upload, remote operation, public distribution, new heroes, or repeat internet playtest is authorized.
