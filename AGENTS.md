# Guards Digital - Agent Entry Point

## Startup and targeted context loading

At the start of each new agent task or run, read `Docs/AI/SESSION_BRIEF.md` once before the first plan or workspace change. Treat it as loaded for the rest of that task; reread it only after a context reset that lost its exact contents or when the file changed. Use `Docs/AI/DOCUMENTATION_GOVERNANCE.md` when ownership, archive, threshold, or risk routing is implicated.

Load only the authority needed for the task:

1. For roadmap work, read `Docs/AI/ROADMAP.md`'s `Execution rule`, follow its phase-shard index, and read the exact current or proposed milestone. Read an adjacent milestone only when the selected definition names it as a prerequisite.
2. Follow `Docs/AI/PROJECT_CONTEXT.md` to the relevant subject shard when the task depends on implementation, product scope, architecture, platforms, presentation, terminology, or workflow beyond the brief.
3. For game-rule work, read `Docs/AI/RULES_LEDGER.md`'s `Authority hierarchy`, the matching rule-system or capability entries, and linked open questions or deviations. Read the relevant pages of `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` when the rule itself is implicated.
4. For card, action, effect, combat, turn, progression, snapshot, or rules-authority work, follow `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` to the relevant contract shard. For effects, keywords, targets/queries, modifiers, tokens/markers, board objects, timing, or transitions, also follow `Docs/AI/CARD_EFFECT_CAPABILITIES.md`.
5. Before running or changing automated tests, follow `Docs/AI/TESTING.md` to the applicable policy and exact recipe shard.

Expand to a full-document read only for genuinely cross-cutting work, a named dependency, or a suspected conflict. A delegated agent may rely on applicable inherited context; otherwise it loads only the missing targeted sections.

## Governing authority and milestone workflow

`Docs/AI/DOCUMENTATION_GOVERNANCE.md` owns the canonical-home matrix, leaf thresholds, archive workflow, and active-risk lifecycle. Preserve precise current rules, contracts, acceptance/non-goal checks, source citations, deviations, open questions, active recipes, validation evidence, and rights/privacy boundaries. Route duplicated or completed history losslessly; do not weaken it to satisfy a threshold.

`Docs/AI/ROADMAP.md` owns milestone execution and review closure. `Docs/AI/SESSION_BRIEF.md` alone owns the current/active/next route. Once the user explicitly approves an exact milestone ID:

1. Implement only its observable outcome and respect its non-goals.
2. Run the validation selected by `Docs/AI/TESTING.md`.
3. Perform the required review-only pass from the player-experience, rules-authority, maintainability, and project-risk perspectives. Separate demonstrated defects from measured or plausible risks and optional improvements; correct only in-scope acceptance defects, and report the review at handoff even when it finds none.
4. Update only the continuity documents whose subject changed.
5. Mark the milestone complete and create its local commit. The commit body records the validation tier, final revision, exact checks/results, material limitations, and bounded reason for any intentionally omitted unaffected suite; one phase-archive row indexes that evidence.
6. Report the result and next proposal, then stop.

Use `[~]` and the brief's matching active field only when work spans tasks or is handed off mid-flight; at most one milestone may be in progress. If the roadmap and brief disagree, reconcile the route before implementation. Model or reasoning-effort labels are not governed milestone data; do not reintroduce them.

## Non-negotiable working rules

- Treat the second-printing rulebook, verified card data, and published official rulings as authoritative under `Docs/AI/RULES_LEDGER.md`. Never invent, simplify, rebalance, or silently reinterpret a rule; record every approved deviation there.
- Work on exactly one user-approved micro-milestone at a time. Do not continue into adjacent features merely because they are easy or related.
- Each actionable independent-review finding names a closing check and closes only on its recorded failing-then-passing transition. Schedule a separately numbered fresh-task verification only when a finding cannot be closed mechanically because it concerns rule interpretation, architectural direction, or player experience; follow `Docs/AI/ROADMAP.md`.
- After every completed approved roadmap milestone, create the local Git commit containing the milestone and continuity updates. Do not configure a remote, push, publish, or distribute anything without the user's applicable explicit authorization.
- Use the smallest implementation that proves the behavior while preserving `Docs/AI/PROJECT_CONTEXT.md`'s architecture. Authoritative rules and state remain independent of Unity scene objects, presentation, animation, and audio.
- Preserve the definition/instance, action-offer, command/event, pending-choice, timing, versioning, snapshot, and replay contracts in `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md`, plus the typed effect/query contracts in `Docs/AI/CARD_EFFECT_CAPABILITIES.md`. Do not add card-name/hero-name rule branches, runtime printed-text parsing, or UI-owned card resolution.
- Follow `Docs/AI/Testing/POLICY.md` for risk tiers, affectedness, pass criteria, evidence, and suite breadth. Every executable behavior change receives the narrowest focused automated tests and directly affected regressions; every bug fix receives a regression test.
- Every failure observed in an active automated test blocks handoff until corrected and rerun green, or the user explicitly approves the policy's documented quarantine or corrective-sequence disposition. Correct only within authorized scope; otherwise stop for that disposition. Skipped, inconclusive, missing, or stale required results are not passes.
- Launch Unity validation only through the exact parameterless project-local `Tools/Invoke-UnityValidation.ps1` route in `Docs/AI/Testing/POLICY.md`. Keep requests, logs, and results inside the workspace; allow at least 15 minutes or yield and monitor the same live PID. Never start a replacement against the project while that PID runs, and use only the launcher's bounded recovery path for licensing failures.
- Documentation-only changes use documentation/source validation and do not launch Unity. Pure rules/data changes do not require Play Mode unless a Unity-facing path changed. Complete unfiltered suites remain reserved for the gates and risk triggers defined in testing policy.
- Preserve user-created and reference files. Do not bulk-process or reorganize the card corpus unless the approved milestone requires it.
- The tracked source and rulebook are the only currently authorized publication. The extracted card corpus, map references, builds, and other excluded or untracked material remain private and must not be added, uploaded, published, marketed, monetized, or distributed without separate explicit authorization and applicable rights.

## Continuity updates

At essentially every milestone, update:

- `Docs/AI/SESSION_BRIEF.md` for the current/active/next route and compact startup status.
- The exact `Docs/AI/Roadmap/PHASE_<N>.md` shard for status, plus one ID/outcome/commit row in `Docs/AI/Archive/Phase-Handoffs/PHASE_<N>.md`. Detailed evidence lives in the referenced commit body.

Update other authorities only when their subject changed:

- `Docs/AI/Project/CURRENT_IMPLEMENTATION.md` for implemented behavior; the matching `Docs/AI/Project/*.md` shard for product scope, constraints, architecture, platforms, presentation, terminology, or workflow changes.
- `Docs/AI/RULES_LEDGER.md` for rule verification, implementation, testing, clarification, deviation, or open-question changes.
- `Docs/AI/Card-System/*.md` and `Docs/AI/CARD_EFFECT_CAPABILITIES.md` for changed card/effect contracts or capabilities.
- `Docs/AI/TESTING.md` or its shards for changed policy, recipes, gates, or test ownership.
- `Docs/AI/TECHNICAL_RISKS.md` for raised, reassessed, or retired risks and for temporary structures that gain or lose a retirement owner.

An unchanged subject document is the correct outcome. Frozen migration and preservation records are never routine handoff updates. At every handoff and phase gate, run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1`, then `git diff --check`; resolve any cross-authority conflict before implementation or handoff.
