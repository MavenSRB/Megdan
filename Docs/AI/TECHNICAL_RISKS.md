# Guards Digital - Active Technical-Risk Register

This is the only active technical-risk register. Its lifecycle and required fields are governed by [DOCUMENTATION_GOVERNANCE.md](DOCUMENTATION_GOVERNANCE.md). Planned mitigations name roadmap ownership; they do not authorize implementation.

## Active risks

| ID | Concrete failure mode and impact | Evidence / confidence | Trigger or detection | Earliest affected milestone | Retirement or reassessment | Approved mitigation or explicit acceptance |
| --- | --- | --- | --- | --- | --- | --- |
| `RISK-COMPAT-001` | Snapshot/content schema or stable-identity changes can silently deserialize older data into a different meaning, undermining replay, diagnostics, and future resynchronization. | Milestone 4.4d pins schema-version-2 setup, active-turn, pending-choice, and end-of-round fixtures by deterministic SHA-256 and proves explicit unsupported-schema/content-revision rejection. The independent 4.4e review confirmed byte-identical supported round trips, distinct unsupported-schema/content-revision rejection, and no current schema drift; the risk remains high impact for every future durable schema or content-identity change. | Any snapshot schema, stable ID, selected content revision, serializer envelope, or compatibility-fixture decision changes. | 4.4a | Reassessed at 4.4e; reassess at every durable schema/content-identity change and formally again at 5.8c, continuing through later network gates. | Keep the small versioned compatibility corpus current and require an explicit migrate-or-reject decision in the same milestone as every incompatible durable change. |
| `RISK-PRESENTATION-001` | Resolved at milestone 5.8a1; see the [lossless archived record and resolution evidence](Archive/RESOLVED_TECHNICAL_RISKS.md#risk-presentation-001). | - | - | - | Reassess the corrected presentation foundation at 5.8c. | - |
| `RISK-ONLINE-001` | Staged single-controller assumptions or unpinned network dependencies could leak private information, accept commands for another hero, or make reconnect/resync nondeterministic in the six-human track. | Planned online architecture has not been selected; high impact, medium confidence future risk. | Any networking dependency selection, owner-scoped projection, seat authorization, reconnect, or host-loss implementation begins. | 8.27 | Reassess at 8.28a; retire or accept residuals at 8.36a. | Explicitly retire staged assumptions in 8.27; pin and assess dependencies in 8.29; test trust, authorization, confidentiality, faults, reconnect, and diagnostics in 8.30-8.36. |

## Resolved-risk history

Resolved entries move losslessly to [Archive/RESOLVED_TECHNICAL_RISKS.md](Archive/RESOLVED_TECHNICAL_RISKS.md). `RISK-ACTION-001`, `RISK-APP-001`, and `RISK-RUNTIME-001` were retired by milestones 4.4b, 4.4c, and 4.4d respectively and are preserved there.
