# Before/After Preservation Inventory

This inventory accounts for the governed categories required by milestone 3.11a. `PASS` means exact current material is present in an active canonical destination or exact historical material is present in a hashed lossless archive destination. The immutable baseline and migration manifest remain the detailed evidence.

## Category coverage

| Category ID | Before evidence | After evidence | Result |
| --- | --- | --- | --- |
| `milestone-definitions` | 147 milestone ID/status headings in pre-governance `ROADMAP.md` | 147 definitions in indexed `Roadmap/*.md`; 3.11a changed to complete and 4.3 changed from blocked to proposed for this handoff | `PASS` |
| `milestone-profiles` | 35 exact `Required Codex profile` declarations | 36 exact declarations; milestone 4.4e now carries its required profile and the next route/config agree on 4.4e, GPT-5.6 Sol, high | `PASS` |
| `acceptance-checks` | 110 exact `Acceptance checks` blocks | 110 exact blocks in indexed roadmap shards | `PASS` |
| `non-goals` | 77 exact milestone `Non-goals` declarations | 77 exact declarations in indexed roadmap shards | `PASS` |
| `rule-authority-and-status` | Baseline `RULES_LEDGER.md`, 52,554 bytes, SHA-256 `13bc55a1c65ff315ac6af441cdb9a67373b4de236a984bf46ec0b6ba7c1d7c3a` | Active ledger is byte-identical | `PASS` |
| `deviations-and-open-questions` | Approved-deviation and open-question sections in the baseline ledger | Same byte-identical active sections | `PASS` |
| `card-contracts` | 17 level-two sections in baseline `CARD_SYSTEM_ARCHITECTURE.md` | 17 exact hashed sections in three indexed `Card-System/*.md` shards | `PASS` |
| `effect-contracts` | 21 level-two sections in baseline `CARD_EFFECT_CAPABILITIES.md` | Active capability catalog is byte-identical | `PASS` |
| `test-commands-and-gates` | 56 level-two sections and 35 exact PowerShell command fences | 55 exact current sections in indexed testing shards; one superseded snapshot-Undo section is a hashed verbatim archive; all commands remain recoverable | `PASS` |
| `historical-results` | 51 roadmap completion-note bodies | 51 hashed verbatim records in indexed phase-handoff archives, with active backlinks | `PASS` |
| `rights-and-privacy` | Exact rights/distribution, product-horizon, and current-non-goal sections in baseline project context plus matching process boundaries | Exact hashed product/workflow shard destinations; compact startup routing preserves private/no-distribution boundaries | `PASS` |
| `active-leaf-measurements` | Seven governed active files measured below | All current index and subject leaves measured below and within hard thresholds | `PASS` |

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

## New governed active-leaf measurements

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
