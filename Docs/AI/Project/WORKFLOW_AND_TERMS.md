# Development Workflow, References, and Terminology

Canonical active shard. Return to [../PROJECT_CONTEXT.md](../PROJECT_CONTEXT.md).

## Development workflow

### Living-document ownership and growth

- [Living-document governance](../DOCUMENTATION_GOVERNANCE.md) owns the exact matrix, thresholds, archive lifecycle, and phase-gate ownership review; the migration manifest and preservation inventory are frozen historical records, not governed current subjects.
- Active continuity documents are working contracts, not chronological journals. Detailed milestone outcome and validation evidence lives once in the milestone's commit message body, with one archive row per milestone; active definitions retain exact status/checks and a stable backlink.
- The current implementation inventory is maintained by subject in [CURRENT_IMPLEMENTATION.md](CURRENT_IMPLEMENTATION.md). Replace or consolidate affected statements as behavior changes; the session brief keeps only a compact startup summary and route.
- A threshold limits one active leaf, never the complete corpus. At 85% review ownership in the same handoff; archive completed history losslessly or split irreducible current authority into indexed shards. Never weaken precise content to pass.
- Run the project-local living-document validator and `git diff --check` at every milestone handoff and phase gate.

- Implement the smallest independently testable behavior.
- One approved micro-milestone per task.
- Every milestone defines one observable outcome, explicit non-goals, focused acceptance checks, and a hard stop.
- Do not start the next milestone without explicit user approval.
- The early board/card foundation proceeds in separate steps: adjacent selection, an authoritative adjacent-move command/event, presentation hookup, a larger placeholder grid, movement of multiple spaces, obstacle blocking immediately afterward, definition/instance separation, action definitions/runtime offers and typed values, resumable card-action choices, then visible card-driven movement.
- Both the primitive scene session and pure placeholder card resolver treat the player's selected reachable destination with its projected deterministic route as the explicit choice, record every internal adjacent step, allow zero movement through Stop, and reject entering or passing through generic placeholder obstacles.
- Before Phase 1 ends, the selected test action offer's typed printed-value query must be the sole source of movement allowance.
- Fast Travel, movement from card text, and movement-limiting effects remain later rule milestones rather than being folded into the early board test.
- Mechanics are completed with primitives and debug UI before production art begins. Milestone 8.28 establishes URP as the accepted rendering baseline, and milestones 8.29-8.36 prove the invite-only online flow and full friends playtest before any production visual asset is introduced.
- Phase 5 does not begin until the post-setup Phase 4 architecture track has established the match-session port, typed action strategies, passive-view MVP, explicit Unity resource ownership, robust diagnostic-envelope parsing, warning-clean supported object discovery, and a recorded prototype performance baseline, then milestone 4.4e independently reviews that accumulated baseline and every gate-blocking finding receives an approved disposition.
- Independent engineering review milestones run in fresh tasks after selected accumulation gates; the authoritative list of analysis-only review gates is maintained in [Testing/POLICY.md](../Testing/POLICY.md). Reviews inspect actual implementation and diagnostic evidence, record preserved strengths as well as classified findings, and never implement their proposed corrections in the same milestone.
- A review finding does not authorize work. Confirmed gate blockers and other justified corrections become separately approved micro-milestones with explicit validation; subjective style preferences do not become roadmap work without a concrete readability, consistency, defect-risk, tooling, or maintenance consequence.
- [TECHNICAL_RISKS.md](../TECHNICAL_RISKS.md) is the bounded active register; it requires evidence, confidence, trigger/detection, earliest affected milestone, retirement/reassessment point, and approved mitigation or acceptance. It is not a speculative concern backlog; resolved entries move losslessly to indexed history.
- Architecture boundaries that can be stated mechanically are enforced by project-local checks: pure rules/application code remains Unity-free, presentation uses the match-session port, passive views do not own authoritative state, and forbidden name/text parsing or service-location shortcuts fail validation.
- Deterministic scenarios compare command, rejection, event, per-command projection, snapshot, and completed-history evidence across live execution, replay, Undo-prefix replay, and restore/continuation. Bounded generated scenarios always report fixed reproduction seeds.
- Durable snapshot/content compatibility uses a small versioned fixture corpus and an explicit migrate-or-reject decision for every incompatible schema or stable content-identity change; old data must never deserialize silently into a different meaning.
- Performance captures become regression evidence through noise-tolerant thresholds and retained rationale, not premature production targets. Threshold crossings require investigation and disposition; they do not authorize speculative optimization.
- External editor/package/SDK/service dependencies are pinned with purpose, version, license/private-distribution constraints, authority/determinism reach, and upgrade/removal path. Material upgrades receive an explicit compatibility assessment and the applicable gate rerun.
- Disposable player-build smokes occur at selected hard gates to detect Editor-only assumptions; they remain private validation artifacts and do not replace the retained pitch build milestone.
- Select the highest applicable validation tier from `Docs/AI/TESTING.md` based on the behavior and boundaries changed, not merely diff size.
- Every executable behavior change receives focused automated coverage and directly affected regressions; every fixed bug receives a regression test.
- Prefer tiny deterministic scenarios to complete match replays.
- Run Play Mode and visual checks only for affected Unity-facing or visible paths.
- Run complete unfiltered suites only at implementation-phase endpoints, explicit hard gates, broad-risk infrastructure/schema changes, or when affected consumers cannot be confidently enumerated.
- Run Unity validation serially with the long-running process and licensing safeguards in `Docs/AI/TESTING.md`; a command-runner timeout is not a Unity test result, and an active Unity process is monitored rather than replaced.
- Documentation/source-only work uses diff, reference, traceability, and consistency validation without launching Unity.
- Visually inspect only the presentation affected by the current milestone.
- Record the validation tier, exact checks/results, visual or source review, and any intentionally omitted unaffected suite in the completion note.
- After a roadmap milestone passes its acceptance checks and the living documents are updated, create a local Git commit for that completed milestone. Never push or configure a remote without separate authorization.

## Available reference material

- `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` - primary rulebook, tracked through Git LFS under the user's 2026-08-13 confirmation of relevant public-distribution permission.
- `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` - project architecture contract derived from the general card/action rules; it is not a substitute for card-specific source verification.
- `Docs/AI/CARD_EFFECT_CAPABILITIES.md` - effect and keyword capability index/catalog, routing to capability shards where needed; it is not evidence that a capability or card is implemented.
- `Docs/Guards of Atlantis II - Tabletopia Cards/` - private reference extraction containing hero-card material and supporting scripts/assets.
- The extracted card corpus has not been accepted as complete or fully verified for implementation.
- Do not assume filenames, duplicates, image faces, or extraction completeness are authoritative without an explicit inventory milestone.
- The tracked rulebook is the sole public reference authorized on 2026-08-13. Do not distribute the extracted card corpus, map references, or other excluded source material without separate explicit authorization and appropriate rights.

## Shared terminology

- **Director mode:** One human can see and control both teams for testing and demonstration. This is not opponent AI and not the final multiplayer interaction model.
- **Pitch prototype:** The private 3v3 build with Wasp and five other distinct verified heroes, retaining director mode while also proving invite-only six-human internet play before production presentation.
- **Private online playtest:** An invite-only pre-production Windows test in which six remote humans each control one hero through a host-authoritative session; it is not a public or production live service.
- **Authoritative state:** The validated rules state from which all visuals are derived.
- **Command:** A requested player or debug action sent to the rules authority.
- **Rule event:** An ordered, validated state change produced by the rules authority and consumed by presentation.
- **Card definition:** Immutable, versioned structured rules content for a printed card face; never live match state.
- **Card instance:** One stable owned runtime card that references a definition and carries its current lifecycle facts.
- **Action definition:** Immutable structured content for a printed or general-rule action before current match context is applied.
- **Action offer:** One context-specific legal use of a card or general rule, selected separately from the card itself.
- **Effect capability:** One versioned typed mutation, query, modifier, control-flow, timing, persistent-source, or system-transition contract referenced by structured card rules.
- **Pending choice:** Serializable authoritative input required to continue a paused action resolution.
- **Micro-milestone:** The smallest user-approved, independently testable increment with a hard stop after acceptance.
- **Rules ledger:** The traceability record connecting rule systems to sources, implementation, tests, and deviations.

## Current explicit non-goals

- Card execution beyond the completed synthetic Hold/Movement/Attack/Defense and repeated-round slices, including Attack consequences, resolve-without-action, concrete end-of-round systems beyond retrieval, or verified Wasp content before their exact milestone approval.
- Public distribution or commercialization.
- Public online play, public server discovery, or production live-service operation.
- AI opponents.
- Pass-and-play.
- Any seventh pitch hero or hero content not selected through the post-Wasp roster milestone.
- A universal card-text parser.
- Production visuals before the primitive game loop, milestone 8.28 URP migration gate, and milestone 8.36 private online playtest gate have passed.
- Music, voice, tutorials, accounts, public matchmaking, cloud saves, or telemetry.
