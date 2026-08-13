# Guards Digital - Agent Entry Point

## Startup and targeted context loading

At the start of each new agent task or run, read `Docs/AI/SESSION_BRIEF.md` once before the first plan or workspace change. Treat it as loaded for the rest of that task; do not reread it merely because the user sends a follow-up. Use `Docs/AI/DOCUMENTATION_GOVERNANCE.md` when ownership, archive, threshold, or risk routing is implicated.

Do not automatically read the full continuity documents. Use `rg` to find the required headings or terms, then read only the relevant section and enough surrounding context to preserve its dependencies:

1. For roadmap work, read `Docs/AI/ROADMAP.md`'s `Execution rule`, follow its phase-shard index, and read the exact current or proposed milestone section. Read adjacent milestone sections only when the selected milestone names them as prerequisites.
2. Follow `Docs/AI/PROJECT_CONTEXT.md` to the relevant subject shard when the task changes or depends on the detailed current implementation, product scope, architecture, platform, presentation direction, terminology, or development workflow beyond the session brief.
3. For game-rule work, read `Docs/AI/RULES_LEDGER.md`'s `Authority hierarchy`, the matching rule-system or capability entries, and any linked open question or deviation. Read the relevant pages of `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` when the task depends on a game rule.
4. For card, action, effect, combat, turn, progression, snapshot, or rules-authority work, follow `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` to the relevant contract shard. For effect, keyword, target/query, modifier, token, marker, board-object, timing, or system-transition work, also follow `Docs/AI/CARD_EFFECT_CAPABILITIES.md` to the relevant capability section or indexed shard.
5. Before running or changing automated tests, follow `Docs/AI/TESTING.md` to the applicable policy and exact recipe shard.

Expand to a full-document read only when the task is genuinely cross-cutting, targeted sections reveal an unresolved dependency, or the living documents may conflict. Reread a loaded section only when it changed, the task expands into a new area, its exact contents are unavailable after a context reset, or conflict verification requires it. A delegated agent may rely on applicable inherited context; otherwise it reads only the missing relevant sections once.

## Living-document governance

`Docs/AI/DOCUMENTATION_GOVERNANCE.md` is the permanent ownership, routing-threshold, archive, migration, preservation, and active-risk contract. Its thresholds limit one active leaf, never the total corpus, and never permit deletion, lossy compression, weakened precision, or reliance on Git history alone.

- Write each current fact in its one canonical home. Noncanonical active documents use a short routing summary and stable link instead of copying a narrative.
- Write detailed completed-milestone outcome and validation evidence once, in that milestone's commit message body. The applicable `Docs/AI/Archive/Phase-Handoffs/` file receives one row - ID, one-line outcome, commit SHA. The active milestone definition keeps only its exact definition/status and a historical backlink.
- Keep `SESSION_BRIEF.md` limited to a compact current-status summary, authorization, exact next milestone, boundaries, and routing. Maintain detailed current implementation by subject in `Docs/AI/Project/CURRENT_IMPLEMENTATION.md`, replacing affected statements rather than appending milestone chronology; keep product, rules, card/effect, testing, and risk material in the canonical homes named by the ownership matrix.
- When an active leaf reaches the 85% warning threshold, review ownership in the same handoff. Move duplicated/completed history losslessly or split irreducible current material into an indexed authoritative shard. A hard-threshold failure blocks handoff unless routing is corrected or the user explicitly approves a governance amendment.
- Never shorten a current rule, contract, acceptance/non-goal check, source citation, deviation, open question, exact active test recipe, or historical pass/failure record merely to meet a threshold.
- At every milestone handoff and phase gate, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1`, then `git diff --check`; update the affected indexes and risk disposition when implicated. The frozen migration manifest and preservation inventory are not updated.

## Milestone start

Once the exact milestone is approved, begin work. Status moves from `[ ]` to `[x]` in the completion commit, together with the continuity updates.

The former pre-work transition - editing the roadmap to `[~]`, editing the session brief, and running the validator before any analysis - was retired on 2026-08-13. It cost a full document round trip and a commit before the work started, and the state it recorded is already visible: an uncommitted working tree on a known branch says what is in flight, and the approval itself is in the task. The `[~]` marker and the `Active authorized milestone` field remain available and remain validator-enforced when used; mark a milestone `[~]` when work will span sessions or be handed off mid-flight, and set the brief's active field to the same ID. At most one milestone may be `[~]`.

For a normal new milestone task, the user only needs to open a new task in this trusted project and explicitly approve the milestone by ID, for example: `I explicitly approve milestone 2.2. Implement it and stop after its required handoff.` The agent then performs the normal targeted loading, implementation, validation, continuity updates, and local commit without asking the user to repeat the stored workflow.

If the roadmap and session brief disagree, do not implement the milestone. Reconcile the stored route first.

Model and reasoning-effort selection is not a governed per-milestone artifact. The previous `Required Codex profile` contract, its `.codex/config.toml` mirroring requirement, and the corresponding validator checks were retired on 2026-08-13; see the provenance record in `Docs/AI/Archive/MIGRATION_MANIFEST.md`. Do not reintroduce per-milestone model labels into the roadmap.

## Current authorization

The detailed current implementation lives in `Docs/AI/Project/CURRENT_IMPLEMENTATION.md`; authorization and the next milestone live in `Docs/AI/SESSION_BRIEF.md`. Do not begin a proposed gameplay, presentation, card, or build milestone until the user explicitly approves that exact micro-milestone.

## Non-negotiable working rules

- Treat the second-printing rulebook, verified card data, and published official rulings as authoritative according to the hierarchy in `Docs/AI/RULES_LEDGER.md`.
- Do not invent, simplify, rebalance, or silently reinterpret a rule. Record every approved deviation in `Docs/AI/RULES_LEDGER.md`.
- Work on exactly one user-approved micro-milestone at a time. Satisfy its acceptance checks, update the living documents, report the result, and stop for approval.
- Do not begin substantive work on a milestone the user has not approved by exact ID. Record the completed status in the milestone's commit; mark `[~]` mid-flight only when work will span sessions or be handed off.
- Before declaring any approved milestone complete, perform a review-only pass from the player-experience, rules-authority, maintainability, and project-risk perspectives. Distinguish demonstrated defects from measured or plausible risks and from optional improvements. Report the review in the milestone handoff even when it finds no issues. Correct only demonstrated defects that fall within the approved milestone and are required for its acceptance; do not implement optional improvements or expand scope without separate user approval.
- Apply the recursive review-correction closure rule in `Docs/AI/ROADMAP.md`: when a separately numbered independent engineering review requires one or more corrective micro-milestones before the affected downstream gate, its proposed corrective sequence must also include a separately numbered fresh-task independent verification review after the final correction. Corrective handoff self-reviews do not satisfy that verification. If the verification requires further pre-gate corrections, repeat the correction-plus-verification cycle; keep downstream work blocked through the latest required verification. Roadmap scheduling and each milestone's execution still require their applicable explicit user approvals.
- After every completed user-approved roadmap milestone, create a local Git commit containing that milestone and its continuity updates. Do not configure a remote or push without explicit user authorization.
- Do not continue into adjacent features merely because they appear easy or logically related.
- Use the smallest implementation that proves the current behavior, while preserving the architectural constraints in `Docs/AI/PROJECT_CONTEXT.md`.
- Keep authoritative rules and state independent from Unity scene objects, presentation, animation, and audio.
- Preserve the definition/instance, action-offer, command/event, pending-choice, timing, and versioning contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`, plus the typed effect/query contracts in `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. Do not add card-name/hero-name rule branches, runtime printed-text parsing, or UI-owned card resolution.
- Follow the risk-tiered policy in `Docs/AI/TESTING.md`. Every executable behavior change gets the narrowest focused automated tests and directly affected regressions; every bug fix gets a regression test.
- Affectedness determines which tests must be run; it never permits ignoring a failure after a test has run. Every observed failure in an active automated test blocks the current handoff until it is corrected and rerun green, or the user explicitly approves a documented quarantine or separately numbered corrective sequence with replacement coverage and a verification owner. If correction is outside the approved milestone, stop and obtain that disposition rather than calling the failure unrelated, legacy, or pre-existing and proceeding. Skipped, inconclusive, missing, or stale required results are not passes.
- For Unity batch-mode validation, use a command allowance of at least 15 minutes or a yielded execution that can be polled without terminating the process. Never impose a two-minute wrapper timeout. A wrapper yield or timeout is not a test failure: if the launched Unity PID is still active, keep monitoring that same process and its designated log/XML outputs, and never start a replacement against the same project. Treat repeated 60-second licensing-channel retries as an environment startup problem; retry at most once and only after the original Unity process has exited.
- Keep all project files, requests, logs, and test results inside the workspace. Launch Unity validation only through the fixed project-local `Tools/Invoke-UnityValidation.ps1` entry point described in `Docs/AI/TESTING.md`; do not compose or request approval for ad-hoc Unity, Hub, licensing-client, or elevated PowerShell commands. The launcher owns the required outside-sandbox process context, licensing preflight, and safe Hub recovery. Agents must use its exact parameterless invocation so one narrow reusable system approval can cover future validation runs. Do not make the user keep Hub open, restart Hub, or repair licensing manually unless that automated recovery has failed.
- Documentation-only changes use documentation/source validation and do not launch Unity. Pure rules/data changes do not require Play Mode unless a Unity-facing path changed.
- Reserve complete unfiltered Edit Mode and Play Mode suites for implementation-phase endpoints, explicit hard gates, the pre-build regression gate, shared test/package/assembly changes, or risk whose consumers cannot be confidently enumerated. Do not repeatedly retest the whole game for a narrow change.
- Apply `Docs/AI/Testing/POLICY.md`'s token-efficient execution and reporting rules: run final-revision filters once unless invalidated, prove each non-obvious directly affected dependency route, avoid duplicating an owned invariant across layers, and keep the one canonical archive evidence record concise. This reduces reruns and repeated narration, never required coverage or failure accountability.
- Record the validation tier, final revision, exact tests/checks, results, material limitations, and one bounded reason for any intentionally omitted unaffected suite in the milestone handoff; keep the user-facing summary shorter unless a failure, risk decision, requested audit, or hard gate needs detail.
- Preserve user-created and reference files. Do not bulk-process or reorganize the card corpus unless the current milestone requires it.
- The local rulebook and extracted card corpus are intentionally excluded from Git. Preserve them at their documented paths and never add them to a commit or distribute them without explicit authorization and appropriate rights.
- Keep the project private. Do not publish, upload, market, monetize, or distribute game assets or builds without explicit authorization from the user and the relevant rights holder.

## Continuity updates

Two documents change at essentially every milestone. Update these:

- `Docs/AI/SESSION_BRIEF.md` - authorization, completed/next milestone, and startup routing.
- The `Docs/AI/Roadmap/PHASE_<N>.md` shard - milestone status, plus one archive row (ID, one-line outcome, commit SHA) in the matching `Docs/AI/Archive/Phase-Handoffs/` file. Detailed evidence goes in the commit message body, not the archive.

Update the rest **only when that milestone actually changed their subject**, not as a routine checklist:

- `Docs/AI/Project/CURRENT_IMPLEMENTATION.md` when implemented behavior changed. `Docs/AI/Project/*.md` when product scope, constraints, architecture, presentation, or terminology changed.
- `Docs/AI/RULES_LEDGER.md` when a rule was verified, implemented, tested, clarified, or intentionally deviated from.
- `Docs/AI/Card-System/*.md` when a card-engine contract or extension boundary changed; `Docs/AI/CARD_EFFECT_CAPABILITIES.md` when an effect, keyword, query, modifier, timing, or system-transition contract changed.
- `Docs/AI/TECHNICAL_RISKS.md` when a risk was raised, reassessed, retired, or a temporary structure gained or lost its retirement owner.

A milestone that touched none of a document's subject writes nothing to it; an unchanged document is the correct outcome, not a missed step. If any of these disagree with each other, stop and resolve the conflict with the user before implementation.
