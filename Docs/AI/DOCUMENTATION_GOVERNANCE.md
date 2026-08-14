# Guards Digital - Living-Documentation Governance

## Purpose and invariants

This contract keeps routine context bounded without limiting the complete authoritative corpus. Current authority may be split across indexed subject shards, and completed history may move to indexed archives, but neither operation may delete, weaken, paraphrase, or silently change rules, sources, deviations, open questions, milestone acceptance/non-goal checks, exact active test recipes, validation evidence, rights/privacy boundaries, or Git history.

Every fact has exactly one canonical active home. Other active documents use a short routing summary and a stable link. Historical evidence has exactly one lossless archive destination. The pre-governance baseline is preservation evidence, not current authority.

## Ownership matrix

| Information category | One canonical home | Permitted noncanonical content |
| --- | --- | --- |
| Current implementation inventory | [`Project/CURRENT_IMPLEMENTATION.md`](Project/CURRENT_IMPLEMENTATION.md) plus its indexed [`Project/`](Project/) subject shards, currently [`Project/FOUNDATION_CORRECTIONS.md`](Project/FOUNDATION_CORRECTIONS.md), which [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) also indexes as a peer subject shard | Compact startup status and one stable link from [`SESSION_BRIEF.md`](SESSION_BRIEF.md); no duplicated chronology or test totals |
| Current authorization and exact next milestone | [`SESSION_BRIEF.md`](SESSION_BRIEF.md) | One stable link; no duplicated route markers |
| Roadmap sequencing, milestone status, exact acceptance checks, and non-goals | [`ROADMAP.md`](ROADMAP.md) plus its indexed [`Roadmap/`](Roadmap/) phase shards | Status/routing summaries that link to the exact milestone |
| Durable product scope, architecture, platform, presentation, terminology, and workflow | [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) plus its indexed [`Project/`](Project/) subject shards | Minimal dependency-routing summaries |
| Rule/source authority, verification and implementation status, approved deviations, and open questions | [`RULES_LEDGER.md`](RULES_LEDGER.md) plus its indexed [`Rules/`](Rules/) shards, currently [`Rules/CARD_COVERAGE.md`](Rules/CARD_COVERAGE.md) | Links from architecture or milestone definitions |
| Durable card/action engine contracts and extension boundaries | [`CARD_SYSTEM_ARCHITECTURE.md`](CARD_SYSTEM_ARCHITECTURE.md) plus its three indexed [`Card-System/`](Card-System/) shards: core model, resolution/effects, validation/extension | Links and narrowly necessary dependency summaries |
| Durable effect, keyword, query, modifier, timing, token/marker, and transition capabilities | [`CARD_EFFECT_CAPABILITIES.md`](CARD_EFFECT_CAPABILITIES.md) plus its three indexed [`Card-System/`](Card-System/) capability shards: timing/transitions, board, state - the directory is owned jointly by the two indexes, three shards each | Links and narrowly necessary dependency summaries |
| Validation policy, launcher resilience, active/future exact recipes, gate definitions, and test ownership | [`TESTING.md`](TESTING.md) plus its indexed [`Testing/`](Testing/) shards | Exact command link or short gate route |
| Active technical risks | [`TECHNICAL_RISKS.md`](TECHNICAL_RISKS.md) | Milestone links to owning risk IDs |
| Official map data contract, reviewed inventory, and generated-resource rule | [`Maps/README.md`](Maps/README.md) | Links from the rules ledger's official-map row and the session brief's routing table |
| Completed milestone narratives, historical run/failure evidence, superseded recipes, and resolved risks | [`Archive/README.md`](Archive/README.md) and its indexed lossless destinations | Short backlink only; never a copied body |
| Consolidation provenance (frozen; not maintained) | [`Archive/MIGRATION_MANIFEST.md`](Archive/MIGRATION_MANIFEST.md), [`Archive/PRESERVATION_INVENTORY.md`](Archive/PRESERVATION_INVENTORY.md), and [`Archive/Pre-Governance/SHA256SUMS.md`](Archive/Pre-Governance/SHA256SUMS.md) | Stable links only |

## Active leaf thresholds

Thresholds apply per active leaf, never to the aggregate corpus. Word count uses the validator's stable Unicode token rule; byte count is the file's exact length. At or above 85% of either hard limit, the validator warns and the same handoff must perform an ownership review. Exceeding either hard limit blocks handoff.

| Active leaf family | Hard words | Hard bytes |
| --- | ---: | ---: |
| `SESSION_BRIEF.md` | 2,000 | 20 KiB |
| `PROJECT_CONTEXT.md` and each `Project/*.md` shard | 4,500 | 40 KiB |
| `ROADMAP.md` and each `Roadmap/*.md` shard | 15,000 | 128 KiB |
| `TESTING.md` and each `Testing/*.md` shard | 7,000 | 80 KiB |
| `CARD_SYSTEM_ARCHITECTURE.md`, `CARD_EFFECT_CAPABILITIES.md`, and each `Card-System/*.md` shard | 7,000 | 64 KiB |
| `RULES_LEDGER.md` and any future rules-ledger shard | 9,000 | 80 KiB |
| `DOCUMENTATION_GOVERNANCE.md` and `TECHNICAL_RISKS.md` | 4,500 | 40 KiB |

If irreducible precise current material cannot fit, create an indexed authoritative shard with the same family threshold or obtain an explicit governance amendment. Never abbreviate exact material or silently relax the validator. A validator cannot become passable merely by deleting content: the code-truth checks compare documentation against the source, `duplicate-history` reads the archives, and both fail on content that was removed rather than routed.

The current-implementation inventory is maintained by subject, not chronology: replace or consolidate affected statements when behavior changes; detailed completed outcome and validation evidence is written once in the milestone's commit message body with one archive row, per the history workflow below. Top-level startup and capability indexes remain compact routers; moving precise active authority to an indexed shard is the normal response to sustained growth, not compression.

## Active risk lifecycle

`TECHNICAL_RISKS.md` is the only active register. Every retained entry records a concrete failure mode/impact, evidence and confidence, trigger or detection method, earliest affected milestone, planned retirement or reassessment milestone, and approved mitigation or explicit interim acceptance. Speculation without a concrete trigger is not an active risk.

At the owning milestone or phase gate, reassess the entry. A resolved risk moves losslessly to `Archive/RESOLVED_TECHNICAL_RISKS.md`, the active row becomes a short archive link or is removed under a manifest entry, and the archive index is updated. An accepted residual risk remains active with its decision and next reassessment point.

## History and archive workflow

New milestone outcome and validation evidence is written **once, in the milestone's commit message body**: the validation tier, final revision, exact filters and results, each finding's named closing check and its observed failing-then-passing transition, and any material limitation. `Archive/Phase-Handoffs/PHASE_<N>.md` receives one row per milestone - ID, one-line outcome, commit SHA - and the active milestone definition keeps only its stable `Historical evidence` backlink.

This replaced the per-handoff narrative append on 2026-08-13. Evidence stays recoverable and immutable in Git, addressed by SHA, instead of growing a 100 KB file that every handoff had to edit and every threshold check had to measure. Existing archive narratives are not rewritten; they remain valid history under the append-only rule.

Superseded command recipes, resolved-risk records, and other historical evidence still move verbatim to their indexed archive.

Archive files are version-controlled, append-only historical evidence. Corrections are explicit addenda; prior text is not rewritten. `Archive/Pre-Governance/` is immutable. Archive indexes link back to the active canonical definition or policy, and active definitions link to their historical evidence when it exists.

## Retired migration bookkeeping

`Archive/MIGRATION_MANIFEST.md`, `Archive/PRESERVATION_INVENTORY.md`, and `Archive/Pre-Governance/` are **frozen historical records** as of 2026-08-13. They document one completed consolidation. Do not update them at handoffs, do not add rows for later edits, and do not treat them as current authority.

Their per-handoff validator checks — baseline hashes, manifest coverage and destination/source section hashes, verbatim-hash equality, preservation-category coverage, and structural baseline floors — were removed by explicitly approved governance amendment on the same date. The migration they verified is finished, Git history is its record, and re-proving it on every handoff cost a manifest hash recomputation for each edit to a tracked section while catching no defect after the migration closed.

What replaced them is narrower and enforces the rule directly rather than a property of one migration:

- `duplicate-history` reads the phase-handoff archives themselves and fails when an archived body is copied verbatim into an active document, which is the one-canonical-home invariant stated as a check.
- The code-truth checks (`undeclared-contract-type`, `incoherent-status`, `unregistered-temporary-structure`, `unreachable-component`) compare documentation against the source, which is the drift the manifest machinery could never detect.

Preservation itself is unchanged: history still moves losslessly to its indexed archive, archives remain append-only, and `Archive/Pre-Governance/` remains immutable. What is retired is the obligation to re-verify a closed migration on every handoff.

## Required handoff and phase-gate review

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1` at every milestone handoff and phase-gate audit, followed by `git diff --check`. `SESSION_BRIEF.md` alone carries a `Last updated:` stamp, kept true at each handoff; other active leaves carry none, because an unenforced stamp drifts into a false claim. Inspect any warning-threshold leaf during that same handoff. Archive duplicated or completed material losslessly, shard irreducible current material authoritatively, and update the affected indexes. The frozen migration records are not part of a handoff.

At each phase gate, review the ownership matrix, active risk register, threshold measurements, archive backlinks, and whether subject routing still matches how future tasks retrieve authority. Governance maintenance must not change gameplay scope, rule meaning, milestone status, validation evidence, or rights/privacy boundaries without the separate authority required for that change.
