# Guards Digital - Agent Entry Point

## Startup and targeted context loading

At the start of each new Codex task or run, read `Docs/AI/SESSION_BRIEF.md` once before the first plan or workspace change. Treat it as loaded for the rest of that task; do not reread it merely because the user sends a follow-up. Use `Docs/AI/DOCUMENTATION_GOVERNANCE.md` when ownership, archive, threshold, or risk routing is implicated.

Do not automatically read the full continuity documents. Use `rg` to find the required headings or terms, then read only the relevant section and enough surrounding context to preserve its dependencies:

1. For roadmap work, read `Docs/AI/ROADMAP.md`'s `Execution rule`, follow its phase-shard index, and read the exact current or proposed milestone section. Read adjacent milestone sections only when the selected milestone names them as prerequisites.
2. Follow `Docs/AI/PROJECT_CONTEXT.md` to the relevant subject shard when the task changes or depends on product scope, architecture, platform, presentation direction, terminology, or development workflow beyond the session brief.
3. For game-rule work, read `Docs/AI/RULES_LEDGER.md`'s `Authority hierarchy`, the matching rule-system or capability entries, and any linked open question or deviation. Read the relevant pages of `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` when the task depends on a game rule.
4. For card, action, effect, combat, turn, progression, snapshot, or rules-authority work, follow `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` to the relevant contract shard. For effect, keyword, target/query, modifier, token, marker, board-object, timing, or system-transition work, also read the relevant capability sections of `Docs/AI/CARD_EFFECT_CAPABILITIES.md`.
5. Before running or changing automated tests, follow `Docs/AI/TESTING.md` to the applicable policy and exact recipe shard.

Expand to a full-document read only when the task is genuinely cross-cutting, targeted sections reveal an unresolved dependency, or the living documents may conflict. Reread a loaded section only when it changed, the task expands into a new area, its exact contents are unavailable after a context reset, or conflict verification requires it. A delegated agent may rely on applicable inherited context; otherwise it reads only the missing relevant sections once.

## Living-document governance

`Docs/AI/DOCUMENTATION_GOVERNANCE.md` is the permanent ownership, routing-threshold, archive, migration, preservation, and active-risk contract. Its thresholds limit one active leaf, never the total corpus, and never permit deletion, lossy compression, weakened precision, or reliance on Git history alone.

- Write each current fact in its one canonical home. Noncanonical active documents use a short routing summary and stable link instead of copying a narrative.
- Write detailed completed-milestone outcome and validation evidence once in the applicable indexed `Docs/AI/Archive/Phase-Handoffs/` file. The active milestone definition keeps only its exact definition/status/profile and a historical backlink.
- Keep `SESSION_BRIEF.md` limited to current implementation, authorization, exact next milestone/profile, boundaries, and routing; keep product, rules, card/effect, testing, and risk material in the canonical homes named by the ownership matrix.
- When an active leaf reaches the 85% warning threshold, review ownership in the same handoff. Move duplicated/completed history losslessly or split irreducible current material into an indexed authoritative shard. A hard-threshold failure blocks handoff unless routing is corrected or the user explicitly approves a governance amendment.
- Never shorten a current rule, contract, acceptance/non-goal check, source citation, deviation, open question, exact active test recipe, or historical pass/failure record merely to meet a threshold.
- At every milestone handoff and phase gate, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1`, then `git diff --check`; update indexes, manifest, preservation inventory, and risk disposition when implicated.

## Milestone model selection

The exact current or proposed roadmap milestone declares its `Required Codex profile`. The project-local `.codex/config.toml` must mirror that profile as the default for the next new Codex task. State the active milestone, model, and reasoning effort in the first milestone-work update.

At every milestone handoff, assign the next milestone's profile and update all three locations before the handoff commit:

1. The next milestone's `Required Codex profile` in `Docs/AI/ROADMAP.md`.
2. The next milestone and profile in `Docs/AI/SESSION_BRIEF.md`.
3. `model` and `model_reasoning_effort` in `.codex/config.toml`, including its milestone comment.

Do not pre-label the entire roadmap with model names that may become stale. Use GPT-5.6 Terra with medium reasoning for bounded routine work, GPT-5.6 Sol with medium reasoning for rules, architecture, authority, or multi-system work, and GPT-5.6 Sol with high reasoning for explicit integration or hard gates. A declared profile is an execution requirement, not authorization to begin the milestone.

For a normal new milestone task, the user only needs to open a new task in this trusted project and explicitly approve the milestone by ID, for example: `I explicitly approve milestone 2.2. Implement it and stop after its required handoff.` The agent then performs the normal targeted loading, implementation, validation, continuity updates, and local commit without asking the user to repeat the stored workflow.

If the roadmap, session brief, and project config disagree, do not implement the milestone. Reconcile the stored profile first. Project configuration supplies the default for a new task but cannot change an already-open task; if the active task uses a different profile, state the exact required `/model` and `/reasoning` change and wait for the user before implementation.

## Current authorization

The current implementation, authorization, next milestone, and required Codex profile live in `Docs/AI/SESSION_BRIEF.md`. Do not begin a proposed gameplay, presentation, card, or build milestone until the user explicitly approves that exact micro-milestone.

## Non-negotiable working rules

- Treat the second-printing rulebook, verified card data, and published official rulings as authoritative according to the hierarchy in `Docs/AI/RULES_LEDGER.md`.
- Do not invent, simplify, rebalance, or silently reinterpret a rule. Record every approved deviation in `Docs/AI/RULES_LEDGER.md`.
- Work on exactly one user-approved micro-milestone at a time. Satisfy its acceptance checks, update the living documents, report the result, and stop for approval.
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
- Record the validation tier, exact tests/checks, results, and any intentionally omitted unaffected suite in the milestone handoff.
- Preserve user-created and reference files. Do not bulk-process or reorganize the card corpus unless the current milestone requires it.
- The local rulebook and extracted card corpus are intentionally excluded from Git. Preserve them at their documented paths and never add them to a commit or distribute them without explicit authorization and appropriate rights.
- Keep the project private. Do not publish, upload, market, monetize, or distribute game assets or builds without explicit authorization from the user and the relevant rights holder.

## Continuity updates

After an approved decision or completed milestone:

- Update `Docs/AI/SESSION_BRIEF.md` and `.codex/config.toml` when current implementation status, authorization, the next milestone, its required Codex profile, or startup routing changes.
- Update the subject shard indexed by `Docs/AI/PROJECT_CONTEXT.md` when product scope, constraints, terminology, or architecture changes.
- Update the phase shard indexed by `Docs/AI/ROADMAP.md` when milestone status or sequencing changes, and append completed evidence once to the matching phase-handoff archive.
- Update `Docs/AI/RULES_LEDGER.md` when a rule is verified, implemented, tested, clarified, or intentionally deviated from.
- Update the contract shard indexed by `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` when a verified rule or card changes a card-engine contract or extension boundary.
- Update `Docs/AI/CARD_EFFECT_CAPABILITIES.md` when a verified rule or card adds or changes an effect, keyword, query, modifier, timing, or system-transition contract.
- If these files disagree, stop and resolve the conflict with the user before implementation.
