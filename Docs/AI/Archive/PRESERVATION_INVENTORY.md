# Before/After Preservation Inventory

This inventory accounts for the governed categories required by milestone 3.11a. `PASS` means exact current material is present in an active canonical destination or exact historical material is present in a hashed lossless archive destination. The immutable baseline and migration manifest remain the detailed evidence.

## Category coverage

| Category ID | Before evidence | After evidence | Result |
| --- | --- | --- | --- |
| `milestone-definitions` | 147 milestone ID/status headings in pre-governance `ROADMAP.md` | 150 definitions in indexed `Roadmap/*.md`; the user-approved amendment adds corrective milestones 4.4f-4.4h while preserving every baseline definition | `PASS` |
| `milestone-profiles` | 35 exact `Required Codex profile` declarations | 40 exact declarations; milestones 4.4e, 4.4f-4.4h, and 5.1 carry required profiles and the next route/config agree on 4.4f, GPT-5.6 Sol, high | `PASS` |
| `acceptance-checks` | 110 exact `Acceptance checks` blocks | 113 exact blocks in indexed roadmap shards; the three additions belong only to corrective milestones 4.4f-4.4h | `PASS` |
| `non-goals` | 77 exact milestone `Non-goals` declarations | 80 exact declarations in indexed roadmap shards; the three additions belong only to corrective milestones 4.4f-4.4h | `PASS` |
| `rule-authority-and-status` | Baseline `RULES_LEDGER.md`, 52,554 bytes, SHA-256 `13bc55a1c65ff315ac6af441cdb9a67373b4de236a984bf46ec0b6ba7c1d7c3a` | Active ledger is byte-identical | `PASS` |
| `deviations-and-open-questions` | Approved-deviation and open-question sections in the baseline ledger | Same byte-identical active sections | `PASS` |
| `card-contracts` | 17 level-two sections in baseline `CARD_SYSTEM_ARCHITECTURE.md` | 17 exact hashed sections in three indexed `Card-System/*.md` shards | `PASS` |
| `effect-contracts` | 21 level-two sections in baseline `CARD_EFFECT_CAPABILITIES.md` | Every baseline capability category remains active across the compact index and indexed `Card-System/BOARD_CAPABILITIES.md`, `STATE_CAPABILITIES.md`, and `TIMING_AND_TRANSITIONS.md` shards; moved section bodies retain their precise contracts and later authorized implementation-status additions | `PASS` |
| `test-commands-and-gates` | 56 level-two sections and 35 exact PowerShell command fences | 55 exact current sections in indexed testing shards; one superseded snapshot-Undo section is a hashed verbatim archive; all commands remain recoverable | `PASS` |
| `historical-results` | 51 roadmap completion-note bodies | 51 hashed verbatim records in indexed phase-handoff archives, with active backlinks | `PASS` |
| `rights-and-privacy` | Exact rights/distribution, product-horizon, and current-non-goal sections in baseline project context plus matching process boundaries | Exact hashed product/workflow shard destinations; compact startup routing preserves private/no-distribution boundaries | `PASS` |
| `active-leaf-measurements` | Seven governed active files measured below | The 2026-08-12 routing correction leaves all current index and subject leaves below warning and hard thresholds; affected measurements are recorded below and the validator checks the complete active corpus | `PASS` |

## Original governed-file measurements

| File | Before words | Before bytes | After index/leaf words | After index/leaf bytes |
| --- | ---: | ---: | ---: | ---: |
| `SESSION_BRIEF.md` | 2,078 | 15,737 | 697 | 5,837 |
| `PROJECT_CONTEXT.md` | 6,434 | 47,837 | 67 | 694 |
| `ROADMAP.md` | 30,125 | 227,731 | 845 | 6,289 |
| `TESTING.md` | 12,030 | 113,515 | 116 | 1,054 |
| `RULES_LEDGER.md` | 6,780 | 52,554 | 6,780 | 52,554 |
| `CARD_SYSTEM_ARCHITECTURE.md` | 6,205 | 45,489 | 63 | 607 |
| `CARD_EFFECT_CAPABILITIES.md` | 5,895 | 44,237 | 5,895 | 44,237 |

Word counting uses the validator's stable Unicode token expression. Byte values are exact file lengths at consolidation review.

## Consolidation active-leaf measurements

| Active leaf | Words | Bytes | Family hard limit |
| --- | ---: | ---: | --- |
| `DOCUMENTATION_GOVERNANCE.md` | 994 | 8,131 | 4,500 / 40 KiB |
| `TECHNICAL_RISKS.md` | 272 | 2,404 | 4,500 / 40 KiB |
| `Project/PRODUCT.md` | 987 | 7,213 | 4,500 / 40 KiB |
| `Project/ARCHITECTURE.md` | 1,790 | 13,659 | 4,500 / 40 KiB |
| `Project/PRESENTATION.md` | 582 | 4,109 | 4,500 / 40 KiB |
| `Project/WORKFLOW_AND_TERMS.md` | 1,373 | 10,752 | 4,500 / 40 KiB |
| `Roadmap/PHASE_0.md` | 978 | 8,202 | 15,000 / 128 KiB |
| `Roadmap/PHASE_1.md` | 2,353 | 19,187 | 15,000 / 128 KiB |
| `Roadmap/PHASE_2.md` | 1,887 | 14,244 | 15,000 / 128 KiB |
| `Roadmap/PHASE_3.md` | 4,795 | 38,067 | 15,000 / 128 KiB |
| `Roadmap/PHASE_4.md` | 2,641 | 21,500 | 15,000 / 128 KiB |
| `Roadmap/PHASE_5.md` | 1,011 | 7,559 | 15,000 / 128 KiB |
| `Roadmap/PHASE_6.md` | 738 | 5,389 | 15,000 / 128 KiB |
| `Roadmap/PHASE_7.md` | 1,548 | 11,548 | 15,000 / 128 KiB |
| `Roadmap/PHASE_8.md` | 3,773 | 29,149 | 15,000 / 128 KiB |
| `Roadmap/PHASE_9.md` | 763 | 5,947 | 15,000 / 128 KiB |
| `Roadmap/PHASE_10.md` | 217 | 1,717 | 15,000 / 128 KiB |
| `Roadmap/POST_PITCH.md` | 106 | 949 | 15,000 / 128 KiB |
| `Testing/POLICY.md` | 3,659 | 27,554 | 7,000 / 80 KiB |
| `Testing/FUTURE_GATES.md` | 1,418 | 13,093 | 7,000 / 80 KiB |
| `Testing/RECIPES_FOUNDATION.md` | 897 | 10,948 | 7,000 / 80 KiB |
| `Testing/RECIPES_TURNS.md` | 4,255 | 41,613 | 7,000 / 80 KiB |
| `Testing/RECIPES_COMBAT_AND_MOVEMENT.md` | 1,536 | 17,953 | 7,000 / 80 KiB |
| `Testing/PLAYMODE_AND_SUITES.md` | 529 | 4,974 | 7,000 / 80 KiB |
| `Card-System/CORE_MODEL.md` | 1,928 | 14,327 | 7,000 / 64 KiB |
| `Card-System/RESOLUTION_AND_EFFECTS.md` | 3,529 | 25,616 | 7,000 / 64 KiB |
| `Card-System/VALIDATION_AND_EXTENSION.md` | 774 | 5,885 | 7,000 / 64 KiB |

No active leaf is at the 85% warning threshold. Archive files are excluded from active-leaf thresholds and routine startup loading.

## 2026-08-12 routing correction measurements

| Active leaf | Words | Bytes | Family hard limit |
| --- | ---: | ---: | --- |
| `SESSION_BRIEF.md` | 526 | 4,483 | 2,000 / 20 KiB |
| `Project/CURRENT_IMPLEMENTATION.md` | 1,622 | 12,569 | 4,500 / 40 KiB |
| `CARD_EFFECT_CAPABILITIES.md` | 2,368 | 18,529 | 7,000 / 64 KiB |
| `Card-System/BOARD_CAPABILITIES.md` | 1,858 | 13,460 | 7,000 / 64 KiB |
| `Card-System/STATE_CAPABILITIES.md` | 1,360 | 10,245 | 7,000 / 64 KiB |
| `Card-System/TIMING_AND_TRANSITIONS.md` | 917 | 6,711 | 7,000 / 64 KiB |

This correction changes routing and ownership only. It preserves current implementation facts and capability contracts, introduces no gameplay or milestone-status change, and leaves archives outside active-leaf thresholds.

## 2026-08-13 addendum: retirement of the `milestone-profiles` category

This addendum is an explicit correction under the append-only archive rule. The `milestone-profiles` row above is preserved verbatim as historical evidence of the category as it stood on 2026-08-12; it is no longer an active preservation requirement.

The user explicitly retired the per-milestone model/profile contract on 2026-08-13. The following were removed together, so that no document continued to assert a contract the validator no longer enforced:

| Retired artifact | Location | Disposition |
| --- | --- | --- |
| 60 `Required Codex profile:` declarations | `Roadmap/PHASE_2.md` through `PHASE_6.md` | Removed from active shards; every baseline declaration remains recoverable in immutable `Archive/Pre-Governance/ROADMAP.md` |
| `Required Codex profile for next milestone` route field | `SESSION_BRIEF.md` | Removed |
| Model-selection section and three-location sync rule | `AGENTS.md` | Removed; a non-reintroduction note replaces it |
| Permitted-model paragraph and profile execution steps | `ROADMAP.md` | Removed; the execution rule renumbers from nine steps to eight |
| Profile regex, config agreement check, required route marker, `milestone-profiles` required category, and `profiles` structural baseline floor | `Tools/Validate-LivingDocumentation.ps1` | Removed with their self-test fixtures |
| Milestone-coupling comment | `.codex/config.toml` | Removed; the file remains a plain local Codex default |

The `profiles` structural baseline floor is retired rather than lowered. Retiring it is what permits the active count to fall from 60 to 0 without a validation failure; no other structural floor changed, and `milestones`, `acceptance`, and `non-goals` floors continue to be enforced against the immutable baseline. Milestone definitions, acceptance checks, non-goals, rules, contracts, test commands, historical evidence, and rights/privacy boundaries are unaffected by this retirement.
