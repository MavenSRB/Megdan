# Guards Digital - Agent Entry Point

## Startup and targeted context loading

Read `Docs/AI/SESSION_BRIEF.md` once at startup; reuse it until it changes or context is lost. It alone owns the current/active/next route. Read only the subject owners it names; use `rg` and bounded sections before whole files. Do not load archives, private agent memories, generated manifests, or full test logs during routine startup.

`Docs/AI/ROADMAP.md` owns execution and review. A user request authorizes its bounded outcome or sequence; record it as a compact work item before implementation. Ordinary fixes use work records; milestones are reserved for substantial integration outcomes. Execute approved outcomes serially with proportionate validation and cohesive local commits. Continue through an already approved sequence without asking again; stop at its end or a decision outside scope. A queued or parked item is not authorization.

Default to one implementation agent; delegate only when the user explicitly requests it. Do not add adjacent features, hero content, or unrequested refactors. Group related edits proving the same outcome; do not create a milestone for each file, control, or mechanical step.

## Governing authority and work lifecycle

- Workflow, architecture, product, presentation, implemented behavior: `Docs/AI/PROJECT_CONTEXT.md` or the brief's direct subject link.
- Ownership, preservation, archives, risk routing: `Docs/AI/DOCUMENTATION_GOVERNANCE.md`; active register: `Docs/AI/TECHNICAL_RISKS.md`.
- Game rules: `Docs/AI/RULES_LEDGER.md` authority hierarchy, matching entries, deviations/open questions, and relevant rulebook pages. Verified card data and official rulings govern under that hierarchy. Never invent, rebalance, simplify, or silently reinterpret an existing-mode rule. Original PvE rules require explicit approved decisions in `Docs/AI/Project/PVE_DESIGN.md`; proposals there are not rules or implementation authorization.
- Cards, actions, combat, turns, progression, persistence/replay: matching `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` shard. Effects, queries, modifiers, tokens, board operations, and timing also use `Docs/AI/CARD_EFFECT_CAPABILITIES.md`.
- Before automated tests: `Docs/AI/TESTING.md`, policy, and applicable recipe. Expand only for a named dependency or demonstrated conflict.

## Non-negotiable working rules

Keep authoritative rules/state independent of Unity, presentation, animation, and audio. Preserve definition/instance, action-offer, command/event, pending-choice, timing, typed effect/query, identity/versioning, snapshot, and deterministic replay contracts. No card-name/hero-name branches, printed-text parsing, or UI-owned resolution.

Use the smallest implementation proving the approved behavior. Every executable behavior change receives focused tests and directly affected regressions; every bug fix receives a regression. Select the highest applicable testing tier. Run focused evidence before expensive integration; full suites are for explicit gates and enumerated broad-risk triggers. Reuse green evidence until a later change affects it. Documentation-only work never launches Unity. A build/package request does not trigger full suites: compare against the validated baseline, check changed behavior and affected consumers, reuse unaffected green evidence, and smoke-test the exact artifact. Broader runs need a concrete trigger under testing policy.

Every observed active-test failure requires correction and a green rerun within scope, or an explicitly approved quarantine/corrective disposition with owner and replacement coverage. Skipped, inconclusive, missing, or stale required checks are not passes. Keep explicitly parked test coverage visible in the shared backlog.

Launch Unity only through the exact parameterless `Tools/Invoke-UnityValidation.ps1` validation route in testing policy. Requests/logs/results stay in the workspace. Run serially: allow at least 15 minutes or monitor the same live PID; never replace a running instance. Use only the launcher's bounded licensing recovery. Inspect summaries/failing cases before full logs.

## Continuity updates

1. Verify acceptance at the selected tier. Perform the review-only pass for player experience, rules authority, maintainability, and project risk. Fix in-scope acceptance defects; distinguish demonstrated defects, measured/plausible risks, and optional improvements.
2. Update the brief and work record; move completed work out of current planning to the completion index. Legacy milestone rows remain historical. Update other documents only when their subject changed. Evidence lives once in the commit body: tier, tested source revision, exact checks/results, limitations, and bounded reasons for omitted unaffected suites.
3. Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1`, then `git diff --check`. Resolve authority conflicts. Mark complete and create a cohesive local commit. An immutable `work/<ID>` tag may identify that commit in the completion index without a second bookkeeping commit.
4. Report concise results, limitations, and next route. Continue remaining approved work; stop at its end or a material decision outside scope.

At most one work item is `[~]`, matching the brief's active field. `Docs/AI/Roadmap/BACKLOG.md` is the shared intake route; `Docs/AI/Roadmap/CURRENT_WORK.md` owns the current approved plan. Paused human gates stay `[!]` with prior evidence and the outstanding prerequisite. All agent tools share these records: private memory/scratch notes must not be the only location of unfinished work. Record queue items with status, outcome, dependencies, and closing checks; recording does not authorize implementation or a rule change.

Independent-review findings follow `ROADMAP.md`: each actionable finding names a closing check and records a failing-then-passing transition. Require fresh-task verification only where interpretation, architectural direction, or player experience prevents mechanical closure.

## Preservation and publication

Preserve user edits, source/reference files, `tmp/` evidence, private worktrees, precise rules/contracts, citations, deviations, open questions, and history. Do not bulk-reorganize the card corpus. Frozen migration records are not routine handoff work. Route completed history losslessly; do not weaken active authority to meet size limits.

Tracked source and rulebook are the only currently authorized publication. Extracted cards, maps, builds, private logs, and other excluded/untracked material remain private. Do not add, upload, publish, distribute, push, or configure a remote without applicable explicit authorization and rights. Local commits are required; remote operations are not.
