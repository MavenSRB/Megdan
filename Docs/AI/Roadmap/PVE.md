# PvE delivery plan

Return to [workflow](../ROADMAP.md). Decisions live in [PvE design](../Project/PVE_DESIGN.md); authorization lives in the [brief](../SESSION_BRIEF.md).

The user selected PvE design as a development track. The first slice's rules were approved on 2026-10-10 (W-020), and its implementation was approved as one serial sequence, W-021 to W-024, recorded in [current work](CURRENT_WORK.md).

| Stage | Outcome | Prerequisite and closing check |
| --- | --- | --- |
| Design | Agree the experience and smallest encounter | Complete at W-020: PVE-D01 to PVE-D23 approved; open questions recorded. |
| First playable encounter | A separate test executable: one player directs up to five heroes against card-driven enemies defending three creeps | Approved sequence: W-021 skeleton executable and static encounter, W-022 rules and engine hooks, W-023 presentation, W-024 package. Each closes with focused checks; existing-mode identity gates at W-021, W-022 and W-024. |
| Playtest and revise | Evaluate the intended experience | Playtest evidence; triage problems and select the next bounded outcome. |
| Expand | Add content/systems justified by the played slice | Separate scope and acceptance; assess shared engine, persistence and multiplayer impact. |

Allocate work IDs when selected; detail the next playable slice instead of the entire mode. Existing bots do not establish PvE completion or select its enemy design.
