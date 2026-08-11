# Resolved Technical-Risk History

Lossless resolved-risk records are appended here when retired from [the active register](../TECHNICAL_RISKS.md).

## RISK-ACTION-001

| ID | Concrete failure mode and impact | Evidence / confidence | Trigger or detection | Earliest affected milestone | Retirement or reassessment | Approved mitigation or explicit acceptance |
| --- | --- | --- | --- | --- | --- | --- |
| `RISK-ACTION-001` | Movement and Attack/Defense orchestration in one card-action resolver can accumulate type switches and cross-action state, making future effects hard to isolate and test. | Direct source review after 4.2a; high confidence growth risk, no demonstrated resolution error. | Adding an action requires editing shared branching/orchestration or unrelated action regressions. | 4.4b | Retire in 4.4b; independently reassess at 4.4e. | Introduce typed strategy handlers behind the existing resolution contract while preserving pending-choice and event semantics. |

Resolution note (2026-08-11): Milestone 4.4b added stable typed `movement-v1` and `attack-defense-v1` strategy identities, explicit action-kind mappings, composition-time uniqueness validation, and generic typed registry dispatch. The complete existing Movement and nested Attack/Defense authority, turn/round, snapshot, replay, and application-session regression set remained green. A bounded synthetic Skill fixture registered and executed through the same typed registry control flow without changing it. The risk is retired because later capabilities now have one explicit registration/dispatch seam; milestone 4.4e still independently reassesses the resulting architecture.
