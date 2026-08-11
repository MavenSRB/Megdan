# Guards Digital - Living-Documentation Governance

## Purpose and invariants

This contract keeps routine context bounded without limiting the complete authoritative corpus. Current authority may be split across indexed subject shards, and completed history may move to indexed archives, but neither operation may delete, weaken, paraphrase, or silently change rules, sources, deviations, open questions, milestone acceptance/non-goal checks, exact active test recipes, validation evidence, rights/privacy boundaries, or Git history.

Every fact has exactly one canonical active home. Other active documents use a short routing summary and a stable link. Historical evidence has exactly one lossless archive destination. The pre-governance baseline is preservation evidence, not current authority.

## Ownership matrix

| Information category | One canonical home | Permitted noncanonical content |
| --- | --- | --- |
| Current implementation, authorization, exact next milestone, and next profile | [`SESSION_BRIEF.md`](SESSION_BRIEF.md) | One stable link; no duplicated chronology or test totals |
| Roadmap sequencing, milestone status/profile, exact acceptance checks, and non-goals | [`ROADMAP.md`](ROADMAP.md) plus its indexed [`Roadmap/`](Roadmap/) phase shards | Status/routing summaries that link to the exact milestone |
| Durable product scope, architecture, platform, presentation, terminology, and workflow | [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md) plus its indexed [`Project/`](Project/) subject shards | Minimal dependency-routing summaries |
| Rule/source authority, verification and implementation status, approved deviations, and open questions | [`RULES_LEDGER.md`](RULES_LEDGER.md) | Links from architecture or milestone definitions |
| Durable card/action engine contracts and extension boundaries | [`CARD_SYSTEM_ARCHITECTURE.md`](CARD_SYSTEM_ARCHITECTURE.md) plus its indexed [`Card-System/`](Card-System/) shards | Links and narrowly necessary dependency summaries |
| Durable effect, keyword, query, modifier, timing, token/marker, and transition capabilities | [`CARD_EFFECT_CAPABILITIES.md`](CARD_EFFECT_CAPABILITIES.md) | Links and narrowly necessary dependency summaries |
| Validation policy, launcher resilience, active/future exact recipes, gate definitions, and test ownership | [`TESTING.md`](TESTING.md) plus its indexed [`Testing/`](Testing/) shards | Exact command link or short gate route |
| Active technical risks | [`TECHNICAL_RISKS.md`](TECHNICAL_RISKS.md) | Milestone links to owning risk IDs |
| Completed milestone narratives, historical run/failure evidence, superseded recipes, and resolved risks | [`Archive/README.md`](Archive/README.md) and its indexed lossless destinations | Short backlink only; never a copied body |
| Consolidation provenance and preservation evidence | [`Archive/MIGRATION_MANIFEST.md`](Archive/MIGRATION_MANIFEST.md), [`Archive/PRESERVATION_INVENTORY.md`](Archive/PRESERVATION_INVENTORY.md), and [`Archive/Pre-Governance/SHA256SUMS.md`](Archive/Pre-Governance/SHA256SUMS.md) | Stable links only |

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

If irreducible precise current material cannot fit, create an indexed authoritative shard with the same family threshold or obtain an explicit governance amendment. Never abbreviate exact material or silently relax the validator. A validator cannot become passable merely by deleting content: preservation inventory, manifest coverage, baseline hashes, destination checks, and exact category fingerprints must also pass.

## Active risk lifecycle

`TECHNICAL_RISKS.md` is the only active register. Every retained entry records a concrete failure mode/impact, evidence and confidence, trigger or detection method, earliest affected milestone, planned retirement or reassessment milestone, and approved mitigation or explicit interim acceptance. Speculation without a concrete trigger is not an active risk.

At the owning milestone or phase gate, reassess the entry. A resolved risk moves losslessly to `Archive/RESOLVED_TECHNICAL_RISKS.md`, the active row becomes a short archive link or is removed under a manifest entry, and the archive index is updated. An accepted residual risk remains active with its decision and next reassessment point.

## History and archive workflow

New detailed milestone outcome and validation evidence is written once, directly to the applicable `Archive/Phase-Handoffs/PHASE_<N>.md` file. The active milestone definition receives only a stable `Historical evidence` backlink. Superseded command recipes, resolved-risk records, and other historical evidence move verbatim to their indexed archive; exact hashes are recorded when the migration manifest classifies a move as `verbatim archive`.

Archive files are version-controlled, append-only historical evidence. Corrections are explicit addenda; prior text is not rewritten. `Archive/Pre-Governance/` is immutable. Archive indexes link back to the active canonical definition or policy, and active definitions link to their historical evidence when it exists.

## Manifest and preservation rules

Every removed, relocated, or rewritten governed section is listed in `Archive/MIGRATION_MANIFEST.md` by original path and heading, destination anchor, category treatment, and applicable integrity hashes. Treatments are:

- `verbatim archive`: exact source section text is recoverable at the destination and source/destination SHA-256 values match.
- `authoritative shard`: precise current material moves to an indexed canonical leaf; structural wrapper text may differ, but the section body remains exact unless separately mapped.
- `non-authoritative routing-summary replacement`: replaceable overview or routing prose is rewritten and points to the canonical destination. This treatment cannot be the sole destination for authority, rules, sources, deviations, open questions, acceptance/non-goal checks, exact active recipes, historical pass/failure evidence, or rights/privacy boundaries.

The preservation inventory accounts for milestone IDs/statuses/profiles and acceptance/non-goal checks; rule entries/citations/statuses/deviations/open questions; card/effect contract entries; test commands and gates; historical pass/failure evidence; and rights/privacy boundaries. Manual review complements the validator and compares the baseline, manifest, active leaves, and archives for semantic preservation.

## Required handoff and phase-gate review

Run `powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tools\Validate-LivingDocumentation.ps1` at every milestone handoff and phase-gate audit, followed by `git diff --check`. Inspect any warning-threshold leaf during that same handoff. Archive duplicated or completed material losslessly; shard irreducible current material authoritatively; update indexes, manifest, and preservation inventory.

At each phase gate, review the ownership matrix, active risk register, threshold measurements, archive backlinks, and whether subject routing still matches how future tasks retrieve authority. Governance maintenance must not change gameplay scope, rule meaning, milestone status, validation evidence, or rights/privacy boundaries without the separate authority required for that change.
