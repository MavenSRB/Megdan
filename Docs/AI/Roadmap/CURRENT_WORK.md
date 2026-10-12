# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

## W-053 - PvE seat Undo parity

- **Status:** [~] In progress.
- **Request:** Make Undo available in PvE under the same player-facing rules as PvP.
- **Observable result:** A Blue PvE seat sees and can use the existing per-turn Undo control whenever the same turn-floor, attack, interaction and other-seat boundaries would permit it in PvP. Rewinding one player action also removes any automatic encounter commands that followed it; commitments retain the existing withdrawal behavior.
- **Affected area:** Seat-scoped Undo authorization and interaction ownership in asymmetric matches, the PvE encounter session, PvE co-op projections/wire behavior, and focused documentation.
- **Closing checks:** Focused EditMode coverage proves availability/refusal boundaries, commitment withdrawal, player-action rewind across automatic enemy commands, and PvP regression; affected PvE co-op wire/presentation checks pass. Living-document validation and `git diff --check` pass.
