# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).

W047 is committed as `acfcf245`; W048 is complete at `work/W-048`. Continue the approved sequence without altering W046 package/main.

### [~] W-049 Integrate generated enemy faces and investigate unspawned turns

User clarification 2026-10-12: enemies that have not spawned must be excluded from engine turn participation entirely, not automatically pass or resolve cards. They become required participants only when spawned; preserve scheduled wave spawns. Verify no pre-spawn commitments, actions, or presentation delays, plus correct participation after spawning. This replaces the proposed automatic-pass workaround.

Approved user request 2026-10-12: replace synthetic enemy faces with the fourteen W044 generated private faces. Reuse the existing prepared-card presentation path without changing cards' rules/values; inspect the final assets and map every face to its exact definition. Also investigate the report that unspawned enemies appear to resolve turns in the background, distinguishing rule execution from no-op bookkeeping and presentation delay; report evidence and correct a demonstrated in-scope defect, without inventing spawn rules. Closing checks: exact face mapping, live preview/render and glow compatibility, focused off-board/spawn/turn-order behavior and affected regressions. Dependency: W048. Reference: `C:/Users/sppet/AppData/Local/Temp/codex-clipboard-5d6cdda3-3513-4721-9bfc-98c9bc1f357b.png`. Private generated artwork remains excluded from publication unless separately authorized.
