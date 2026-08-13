# Testing Policy and Evidence Contract

Canonical active shard. Return to [../TESTING.md](../TESTING.md).

## Scope

This shard is the current testing policy and evidence contract. It states what must be validated and how evidence is recorded. It is not a record of what each past milestone covered.

Automated coverage currently spans the pure hex/board foundation, card definitions and owned instances, typed actions/offers/values, Movement including segmented waypoint routes and Fast Travel, Attack and Defense including minion contributions, timing and Active-effect lifecycles, Defeat-reward accounting, the end-of-round Minion Battle, schema-versioned snapshots with a versioned compatibility corpus, deterministic replay and retained-prefix Undo, the configured passive presentation and saved-scene paths, and mechanical architecture-boundary conformance. Verified hero card content, networking, and player-build validation are not yet covered; milestone 6.1d adds the first player-build smoke gate.

The milestone-by-milestone record of what each completed milestone added, including its tier decision and rationale, is historical evidence rather than current policy. It is preserved verbatim in [Archive/TESTING_HISTORY.md](../Archive/TESTING_HISTORY.md#milestone-coverage-narrative-through-61b). Do not reconstruct that chronology in this shard.

Run commands from PowerShell. Each focused Unity command opens the project, runs only its named test fixture or case, writes NUnit XML plus a Unity log under `tmp/`, and exits without producing a player build. Scene-dependent filters load the saved validation scene.

## Risk-tiered validation policy

Choose the highest applicable tier from the behavior and boundaries changed, not from diff size or file extension. A broad suite never replaces focused evidence.

| Tier | Applies when | Required evidence |
| --- | --- | --- |
| T0 - Documentation/source | Only Markdown, roadmap status/sequencing, source traceability, or other non-executable documentation changes | Review the diff; run `git diff --check`; verify paths/links, milestone IDs/status, cross-document references, and changed rule claims against their authority and `RULES_LEDGER.md`. Do not launch Unity. |
| T1 - Local behavior | One bounded pure-C# rule/query/validator or executable content definition changes without a Unity-facing path | Focused Edit Mode tests for the behavior and directly impacted regressions; cover valid, invalid/stale, no-op, and boundary cases as applicable. No Play Mode. |
| T2 - Boundary/integration | Shared capability/resolver/command-event/snapshot interaction changes, or a Unity scene/input/UI/presentation adapter changes | T1 evidence as applicable, dependent Edit Mode fixtures, and determinism/round-trip/ordering/source-linked scenarios where relevant. Run focused Play Mode only for affected Unity-facing paths; visually inspect only changed output. |
| T3 - Gate/broad risk | Final implementation milestone of a phase, explicit hard gate, shared package/assembly/test infrastructure change, snapshot-schema migration, cross-system loop gate, pre-build regression gate, or blast radius that cannot be enumerated | T2 evidence as applicable, then both unfiltered Edit Mode and Play Mode suites. Add gate-specific end-to-end, determinism, visual, source, and build-smoke evidence where relevant. |

During implementation, run the smallest focused filter first. Before completion, run new tests and directly affected regressions, then the selected tier's required breadth. Promote to a higher tier whenever affected consumers cannot be listed confidently. Executable JSON, ScriptableObject, card, or map data is not T0 merely because no C# changed.

For pure Core/CardState/Application changes, a green full run of the [cross-platform dotnet harness](DOTNET_HARNESS.md) plus the applicable focused filters satisfies the Edit Mode portion of T1/T2 evidence on any OS. Any change touching Presentation, assets, scenes, assemblies, or another Unity-facing path still requires the applicable Unity tier through the Windows launcher before its milestone closes; until that run completes, the milestone records it as Windows-pending rather than claiming it.

Affectedness selects the required test breadth; it does not dispose of a failure after execution. Every failure observed in an active automated fixture blocks the current milestone handoff, including failures believed to be pre-existing, legacy, or outside the changed code. Correct and rerun it within authorized scope, or stop for explicit user approval of either (a) a documented quarantine with reason, replacement coverage, owner, and removal milestone, or (b) a separately numbered corrective sequence with required validation and a verification owner. A required result that is skipped, inconclusive, missing, stale, or from a superseded build is not green evidence. Each handoff accounts for every executed result; it may explain causality and scheduling, but it may not omit or relabel a red active result as unaffected.

Unity test processes for this project run serially. Never open parallel batchmode test instances against the same project.

### Unity process and licensing resilience

#### Required agent entry point

All agent-started Unity test validation uses the fixed, parameterless project-local launcher `Tools/Invoke-UnityValidation.ps1`. Do not issue direct `Start-Process` commands for Unity, Unity Hub, or the licensing client, and do not vary the launcher invocation between runs. This keeps every configurable input and output in the project while allowing one narrow reusable system approval for the process boundary Unity requires.

Before invoking it, write `tmp/unity-validation-request.json` with this shape:

```json
{
  "platform": "EditMode",
  "filter": "GuardsDigital.Tests.EditMode.ExampleTests",
  "results": "tmp/example-results.xml",
  "log": "tmp/example.log"
}
```

`platform` must be `EditMode` or `PlayMode`. `filter` may be an empty string for an unfiltered suite. Both output paths must be relative paths beneath the project's `tmp` directory. Invoke the launcher with this exact command and request a persistent approval for this exact project-local entry point if no matching approval exists:

```powershell
& 'C:\Projects\Guards Digital\Guards 0.1\Tools\Invoke-UnityValidation.ps1'
```

The optional request field `"batchMode": false` omits Unity's `-batchmode` argument only when an exact milestone acceptance check requires a targeted non-batch Editor transition. Omit the field, or set it to `true`, for every ordinary focused, affected, and complete automated run. The same fixed launcher, serial-process rule, filter, project-local outputs, licensing preflight, and log/XML pass criteria still apply; this flag is not a route for ad-hoc interactive validation.

The older command blocks below remain test-filter and historical-output references only. Agents translate the applicable values into the request file and must not execute those direct `Start-Process` examples.

The PowerShell examples below intentionally use `Start-Process -Wait -PassThru`; the calling command runner must allow at least 15 minutes for Unity, or yield the still-running command and poll it without terminating the process. Do not wrap a Unity run in a two-minute timeout. Unity licensing reconnects wait in 60-second increments, so a healthy test invocation can outlive a short command wrapper before test discovery begins.

Every Unity Editor, Unity licensing-client, and Unity Hub process used by an agent for validation must be launched outside the workspace sandbox with the tool's required elevated/normal-user process context. A sandboxed Editor can open the project while its child licensing client cannot access the user's persistent license data or create its named-pipe channel; Unity then misleadingly treats every package as unlicensed. Merely seeing an open Hub window or a live `Unity.Licensing.Client` process is not a licensing preflight.

Before the first Unity validation sequence in a task, the agent performs this preflight itself:

1. Check for active `Unity` processes. If another Unity process is using this project, monitor it and its designated outputs; do not restart Hub, licensing, or the Editor and do not start another Unity process.
2. When no Unity process is active, check for Unity Hub and `Unity.Licensing.Client`. On Windows, the installed Hub executable reports the PowerShell process name `Unity Hub` (with a space), so use `Get-Process -Name 'Unity Hub' -ErrorAction SilentlyContinue`; do not use the nonexistent `UnityHub` process name. If Hub is absent, or if the current task already observed a refused/dropped licensing channel, fully restart Hub and its licensing client in the elevated/normal-user context, then wait until both `Get-Process -Name 'Unity Hub'` and `Get-Process -Name 'Unity.Licensing.Client'` return live processes. Restarting these support processes is allowed only while no Unity process is active.
3. Launch the focused Unity test in the same elevated/normal-user context. Monitor its designated log early enough to distinguish licensing startup from compilation or test execution.
4. Continue normally when the log resolves entitlements and registers a nonzero package set. For the current manifest, the healthy baseline is `Registered 39 packages`; future intentional package changes may change that count.

The agent, not the user, owns this preflight and safe recovery. Do not ask the user to leave Hub open or restart it unless the automated restart cannot produce a live Hub/licensing-client pair, sign-in or license activation requires user interaction, or the one permitted post-recovery Unity retry still fails.

If the command runner yields or reaches its own observation limit, first check the PID returned by the original launch and the run's designated log/XML files. While that Unity PID remains active, continue waiting on it; do not launch the same or another test process against this project. A command-runner timeout or yield is not Unity's exit code and is not test evidence.

One initial `Channel LicenseClient-<user> doesn't exist` message is normal when Unity launches its licensing client. `Code 10` client-signature validation or `Access token is unavailable` is also non-decisive by itself because successful local runs can recover immediately afterward. Classify startup as a licensing environment failure when the log shows repeated 60-second initialization/channel timeouts, refused or lost-client reconnect failures, `Registered 0 packages`, packages rejected as unlicensed, or missing headless entitlement and the process exits without valid fresh NUnit XML.

After confirming the failed Unity process has exited, perform the preflight's Hub/licensing restart and allow one serial Unity retry. Do not count a retry made without that recovery as the permitted retry. If the recovered retry shows the same licensing cycle, stop test retries, preserve the logs, and ask the user to verify sign-in/license state or repair the Hub/Editor installation. Never kill a live Unity test merely because no XML exists yet; Unity writes the result after test execution begins.

### Minimum by change type

| Change type | Minimum validation |
| --- | --- |
| Pure core model, rules authority, card definitions/effects/queries, or validators | T1 focused Edit Mode |
| Scene, prefab, Input System, controller, UI, or presentation hookup | T2 with relevant focused Edit Mode and Play Mode |
| Visible result | Applicable automated checks plus one targeted visual inspection of the affected view |
| Official map or card transcription | Source/manual verification plus schema/content validation; add source-linked focused scenarios when executable |
| Bug fix | An automated regression that reproduces the failure when practical, plus the tier required by the affected area |
| Player build/package | Required regression gate, then build-and-launch smoke; clean-machine smoke only at milestone 10.2 |

### Escalation triggers

Use at least T2, and T3 when the change crosses Edit/Play boundaries, for changes to:

- Authoritative state schema, snapshot serialization, migrations, Undo/restore, or deterministic replay.
- Shared command/event, resolver, pending-choice, timing, turn/round, or capability-registration infrastructure.
- Assembly definitions, packages, project settings, test infrastructure, scene bootstrap, or broadly reused saved-scene foundations.
- A refactor with unexpectedly broad diffs, an unexplained failure outside the focused fixture, or a bug with uncertain blast radius.

The tier may escalate during implementation; it is never lowered to avoid a failing relevant test.

### Explicit integration gates

The current T3 phase/hard gates are milestones `1.13`, `2.6`, `3.11`, `4.4d`, `5.8`, `6.13`, `7.9`, `8.16`, `8.27`, `8.28`, `8.36`, and `9.16`. Milestone 4.4 proves the complete configured Phase 4 setup with focused and directly affected evidence; the complete Phase 4 suites run at 4.4d after the behavior-preserving application/session, action-strategy, and MVP refactors. A corrective milestone that changes an already-passed gate boundary, such as `1.13a`, re-runs that gate's complete suites. Milestones `8.2`, `8.19`, and `8.29` must update this list if they re-baseline the Wasp, six-hero 3v3, render-pipeline migration, or private-online endpoints.

Milestone `8.28` performs a disposable internal Win64 development-build launch smoke after the URP migration. Milestone `8.36` produces a separately identified disposable private Win64 build for the invited six-human internet gate. Those artifacts are validation/playtest-only and are not publicly distributed or retained as the pitch deliverable; milestone `10.1` remains the first retained private pitch player build.

Milestone `7.9` also performs an earlier disposable Win64 development-build smoke of the complete synthetic core. It launches outside the Editor, completes a deterministic scripted match path, writes usable diagnostics, and exits cleanly. It detects Editor-only API, stripping, assembly, initialization, serialization, or content-inclusion failures without becoming a retained or distributed build.

Milestone `8.29` must add the exact focused local owner-projection/player-UI, network/session, and multi-client integration commands once the provider, SDK, process topology, and test harness are selected. Milestone `8.30c` first validates all six player perspectives against the in-process local session; milestone `8.35c` then runs the exact intended six-client playtest-build topology locally with the approved fault/reconnect cases. Milestone `8.36` finally runs the focused checks, six-client integration path, recorded multi-network private playtest, and both unfiltered Unity suites at the applicable final revision. Neither an in-process local run nor a successful local multi-process rehearsal is evidence that relay/NAT traversal, external latency handling, multi-network connectivity, or internet failure behavior works.

Milestone `10.1` reuses the unchanged `9.16` result and adds build-and-launch smoke; rerun T3 if code or executable content changed after `9.16`. Milestone `10.2` runs the clean-machine private smoke test without redundantly rerunning Unity Test Framework against an unchanged build.

Risk triggers can require an earlier T2/T3 run. Passing an earlier gate never excuses a later gate after intervening changes.

## Rule and content verification

Tests prove that code matches the selected interpretation; they do not establish that the interpretation is authoritative. Before a rule-dependent milestone, verify the exact rulebook, card, or official-ruling source and update `RULES_LEDGER.md`, then add source-linked positive, negative, boundary, failure/prevention, and ordering scenarios relevant to that slice. Do not encode an unresolved ambiguity as a test expectation; block completion instead.

Rules tests use the smallest deterministic authoritative state that exposes the behavior. They assert accepted/rejected commands, exact state changes, ordered events, pending choices, and relevant checkpoints. Add serialization and repeatability cases when applicable. Tests explicitly choose Tie Breaker state unless setup-side selection itself is under test; do not introduce generalized gameplay randomness.

Verified real cards require definition/schema validation plus a scenario for every behaviorally distinct legal invocation, capability-handler conformance, and all relevant target, state, timing, modifier, replacement, repeat, Active-effect, and continuous-source interactions. Synthetic fixtures remain visibly test-only.

## Regression ownership

- For a bug fix, reproduce the failure in an automated regression test when practical, then keep the test permanently.
- Changing a shared capability runs its conformance fixture and every known directly dependent scenario; once Wasp exists, include its source-linked consumer scenarios.
- A complete suite does not compensate for a missing focused regression.

## Token-efficient execution and reporting

Validation scope follows risk and affectedness, not the desire to accumulate evidence. Preserve the required focused, regression, Play Mode, visual, compatibility, replay, and gate coverage, while applying these limits:

- Run each required filter once at the final candidate revision. Rerun only a failed filter, a filter invalidated by a later code/content/test change, or a broader tier newly triggered by evidence. Do not rerun an unchanged green filter merely to produce a newer timestamp or repeat another layer's proof.
- Count a consumer as directly affected only when the changed contract, state, data, serialized form, scene/presentation path, or shared infrastructure can reach it. Document the dependency route when inclusion is not obvious. Mere conceptual similarity, historical co-selection, or membership in a broad subsystem is insufficient.
- Prefer one test at the lowest layer that owns an invariant. Add higher-layer coverage only for behavior unique to that boundary: serialization/rebinding, command forwarding, scene wiring, Unity lifecycle, rendering, input, or an end-to-end player path. Do not duplicate exhaustive rule permutations through presenter, scene, and Play Mode layers.
- Reuse earlier evidence only when it is from the same revision or the intervening diff provably cannot affect the tested surface, and only when the applicable tier or gate permits reuse. Identify the prior revision/result and the bounded unaffected diff; never use reuse to bypass a required current-revision gate.
- Keep milestone handoff evidence concise and canonical. Record final required filters/checks, totals, revision, failures that affected runtime behavior or validation conclusions, and their dispositions. Aggregate repeated attempts with the same root cause. Summarize test-authoring, compilation, assertion-shape, launcher, or environment iterations by root cause and final disposition unless their exact sequence is needed to explain residual risk, a quarantine, or why evidence is invalid.
- Link to the one archive handoff record instead of repeating validation narratives in active documents or the user-facing report. The user-facing result normally states the tier, final pass totals, material limitations, and archive link; expand only for a failure, risk decision, requested audit, or hard gate.

These limits reduce execution and reporting cost; they do not authorize fewer rule-authority scenarios, weaker assertions, ignored failures, stale required results, or omission of a regression that reproduces a demonstrated defect.

## Evidence and pass criteria

A Unity run counts only when the process exits successfully, a fresh NUnit XML exists, the requested filter discovers at least one test, the XML reports zero failures/errors and no unexpected skipped/inconclusive tests, and the log contains no compile/import exception.

The canonical archive completion record states the tier, final revision, exact focused filters, result totals, directly affected regressions, any complete-suite result, and source/visual/manual/build checks. It identifies any required failure or material limitation and its disposition, and records an unaffected suite's omission with one bounded affectedness reason rather than a repeated coverage narrative. Exact totals in historical `ROADMAP.md` completion notes are evidence from those runs, not permanent expected counts.

## Living-document governance validation

Milestone 3.11a is documentation/tooling-only T0 work. It must add and run:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1
```

The validator must fail when an active leaf file exceeds its approved hard word/byte routing threshold, and warn at or above 85 percent. Thresholds are per file, not an aggregate documentation cap: exact current content may move to indexed authoritative shards, and completed history may move losslessly to an indexed archive. It must also verify required headings; exactly one current/active/next milestone route; exact agreement between `Active authorized milestone` and the sole roadmap `[~]` item; no orphaned or multiple `[~]` items; `ROADMAP.md`/`SESSION_BRIEF.md` agreement; indexed active shards and archive backlinks; mechanically checkable internal paths/anchors; and the absence of archived handoff bodies duplicated verbatim in active documents. The implementation must use stable text/heading rules rather than heuristic semantic rewriting and must not become passable merely by deleting an oversized section. Its own fixtures must cover valid idle and in-progress routes, warning threshold, hard-threshold failure, missing route, an active milestone left proposed, an orphaned or multiple `[~]` state, broken archive/shard link, and a duplicated archived body.

The migration-manifest, baseline-hash, and preservation-inventory checks were retired on 2026-08-13; see [DOCUMENTATION_GOVERNANCE.md](../DOCUMENTATION_GOVERNANCE.md#retired-migration-bookkeeping).

The validator must additionally enforce four code-truth checks, because document-to-document consistency alone cannot detect documentation that has drifted from the source it describes:

1. **Documented-type reality.** Every type name written as a current contract in the card-system and effect-capability documents must resolve to a declaration under `Assets/`, or be explicitly marked as planned. A contract that names no implementing type is a documentation defect, not a design.
2. **Status coherence.** The completed milestone in `SESSION_BRIEF.md`, the current-implementation summary in `Project/CURRENT_IMPLEMENTATION.md`, and the highest complete roadmap milestone must name the same ID.
3. **Temporary-structure registration.** Every placeholder, legacy, or deliberately temporary structure present in `Assets/` must have a row in the temporary-structure register of `TECHNICAL_RISKS.md` naming its retirement owner.
4. **Component reachability.** Every concrete `MonoBehaviour`/`ScriptableObject` under `Assets/` must be authored into a scene, prefab, or asset by script GUID, or constructed by production code through `AddComponent`/`CreateInstance`, or carry a retirement owner in the temporary-structure register. A component Unity cannot construct cannot run, whatever references it. Name-based heuristics cannot express this: nothing about the name `HeroSelectionController` says "temporary", which is why 2,929 lines of it survived reviews `4.4i` and `5.8c` untouched.

Its fixtures must cover a documented type with no implementation, a documented type explicitly marked planned, an incoherent status triple, an unregistered temporary structure, a component in no scene that is never constructed, a component authored into a scene, a component constructed by each supported call form, an unreachable component that carries a retirement owner, and an abstract component base. The validator must also fail loudly when it finds no declared types, no declared components, no authored script references, or no archived bodies, since each of those would make a check pass vacuously.

Manual T0 review complements the mechanical check. First preserve every governed source document unchanged under `Docs/AI/Archive/Pre-Governance/` and record its SHA-256 digest. Then compare every moved or rewritten section against that baseline and its migration-manifest entry. Confirm that milestone IDs/statuses/profiles and exact acceptance/non-goal checks; rule entries/citations/statuses/deviations/open questions; card/effect contracts; active exact recipes and gate definitions; historical results/failure evidence; and rights/privacy boundaries all remain exact in a canonical active shard or lossless archive as appropriate. Only non-authoritative overview and routing prose may be summarized. Run `git diff --check`, inspect all active/archive indexes and backlinks, verify the preservation inventory, and record before/after word/byte measurements for every governed active leaf file. Do not launch Unity.

After 3.11a, run the validator at every milestone handoff and phase-gate audit. A warning threshold requires the same handoff to review ownership: archive duplicated/completed material losslessly or shard irreducible current material authoritatively. A hard-threshold failure blocks handoff unless routing is corrected or the user explicitly approves a governance amendment and corresponding validator update; deleting or weakening precise content is never a valid correction.

## Independent engineering review evidence

Milestones `4.4e`, `4.4i`, `5.8c`, `7.9a`, `8.16a`, `8.28a`, `8.36a`, and `9.16a` are analysis-only review gates. Their review record and any approved roadmap/continuity edits are T0 changes: review the diff, run `git diff --check`, validate milestone IDs/statuses/dependencies and cross-document routes, and, after milestone 3.11a, run the living-document validator. Do not rerun both complete Unity suites merely because a review follows a T3 gate; use the preceding gate's exact revision and fresh evidence unless the review identifies a concrete evidence gap or the implementation changed.

The reviewer may run focused existing tests, source checks, profiler captures, log inspection, snapshot/replay comparisons, build inspection, or other non-mutating diagnostics needed to test a specific concern. Record the exact command or capture, revision, result, and why it was needed. A diagnostic failure is a finding, not permission to repair it during the review. If reproducing or isolating a problem requires new test code, instrumentation, fixtures, content, scenes, packages, or configuration, propose a separately approved corrective/diagnostic milestone instead.

Every review report records the inspected surfaces and preserved strengths, then classifies findings under the roadmap's independent-review categories. An actionable finding includes evidence, impact, severity/timing, the smallest correction, and **the exact named check that will close it**. A stylistic preference alone cannot block a gate. The review completes only when every gate-blocking finding has an explicit proposed disposition and a named closing check, and the affected future milestone sequence has been re-evaluated; implementation remains separately authorized.

Closure evidence is the check's observed transition, not a summary. Record the pre-fix revision at which the named check was seen to fail, the post-fix revision at which it passes, and the exact command for each. A check that only ever passed is not closure evidence, because a vacuously passing check is indistinguishable from a working one. Per `ROADMAP.md`, a separately numbered fresh-task independent verification is scheduled only when a finding's closure cannot be expressed as a check; a corrective milestone's handoff self-review never satisfies that verification.

## Cross-cutting technical-risk controls

Architecture-conformance checks use deterministic assembly-reference or source rules rather than heuristic semantic judgments. Their fixtures must prove both allowed and forbidden dependency examples. A failure blocks the owning gate; weakening an enforced boundary requires an explicit architecture amendment rather than an allow-list added only to make the check pass.

The deterministic replay oracle compares accepted command identity/order, rejected results, ordered semantic events, immutable projection after each accepted command, final authoritative snapshot, and completed history across live, fresh replay, retained-prefix Undo replay, and snapshot restore plus continuation. Property/invariant scenarios are bounded and print the fixed seed and smallest reproducible command prefix on failure. At minimum they protect atomic rejection, ownership, occupancy, live pending-choice identities, complete-set reveal privacy, and replay history cardinality.

The snapshot compatibility corpus is small, version controlled, and representative rather than a dump of routine test output. Every fixture declares its schema and referenced content revisions plus whether it must round-trip, migrate, or reject. A durable schema/content-identity change updates the decision and fixtures in the same milestone; silently accepting changed meaning is a failure.

Performance thresholds are recorded with capture environment, duration/sample count, baseline revision, tolerated noise, and metric ownership. They cover steady-state managed allocation, owned-resource growth, map rebuild, snapshot/projection, and replay at the applicable milestone scale. A threshold crossing fails the focused check or gate until investigated and explicitly fixed, re-baselined with rationale, or accepted by the user; it does not by itself prescribe pooling, DOTS/ECS, or another optimization.

Networking fault and authorization tests remain deterministic where possible and record selected transport/service versions. They cover unauthorized, duplicate, replayed, stale, malformed, oversized, lost, delayed, reordered, and incompatible inputs plus disconnect/reconnect at each pending-choice family. Expected outcomes are host convergence, one documented recoverable state, or one documented terminal state, never partial authoritative replacement or owner-private leakage. Diagnostic fixtures assert that logs correlate session/content/schema, command, projection, and recovery identities without including another seat's hidden hand or pre-reveal commitment.
