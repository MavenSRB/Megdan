# Current approved work

Return to [workflow](../ROADMAP.md). Authorization lives in the [brief](../SESSION_BRIEF.md). Completed records move to [work history](../Archive/WORK_LOG.md).


### [ ] W-047 Approved PvE popup removal and shared player interactions

Explicitly approved follow-up, authorized after W-046 (do not interrupt its tests): remove the persistent PvE Round/Turn/seed and ownership-instruction panel. Placement must use normal respawn-style highlighted legal board hexes, without popup instructions/buttons. Automatically choose a random legal answer only for enemy decisions formerly presented to the host, retaining host authority and existing seeded deterministic replay/network contracts; never randomize player-owned choices. Victory/defeat must use the PvP end-game screen with Blue team only. Record approved PvE decisions before implementation. Closing checks: focused placement input/legality, enemy-choice determinism/restore/replay/co-op ownership, Blue-only terminal presentation and absence of obsolete popup, plus directly affected regressions under policy. No source changes yet.

Additional explicit approval: remove the manual PvE `+1 level [hero]` feature entirely, including dedicated wiring/content where appropriate, not just its button. Its current `PveEncounterView` turn-1 AwaitingCommitments button invokes `MatchModeRules.LevelGrantTransitionId(hero)`. Preserve ordinary earned level-up/progression and independently used shared transition machinery. Record the amended decision and close with manual-offer absence and normal-progression regressions; no unrelated progression changes.

Additional explicit approval: remove the `ENEMIES` heading overlapping the tiebreaker token area. Preserve enemy trays, hover and card behavior; adjust header reservation/overlay only as needed and verify the tiebreaker remains visible and unobstructed. User reference: `C:/Users/sppet/AppData/Local/Temp/codex-clipboard-259d2b2f-bb20-448d-8b83-b185796e0e10.png` (private, not publication).

Additional explicit approval: slightly enlarge numbers on compact enemy-card miniatures, without changing tray/card sizes, values or hero UI. Verify one- and two-digit values fit without clipping. Private reference: `C:/Users/sppet/AppData/Local/Temp/codex-clipboard-0f201018-de69-40d6-af7d-25d623a1df9b.png`.

Additional explicit approval: replace `Control [hero]` buttons with owned-hero tray/token clicks selecting that local seat; another player's hero uses existing inspection without ownership transfer. Preserve card clicks, pending targeting/placement and meaningful board actions over seat switching, coordination/privacy and PvP. Test solo/multiseat switching and other-player inspection. Remove the redundant control panel, including Reconnect and Leave buttons (latest clarification supersedes retaining these buttons), but retain shared reconnect/session-cleanup machinery and PvP menus. Private reference: `C:/Users/sppet/AppData/Local/Temp/codex-clipboard-82fde854-62f5-4535-b3d5-1a9b08e628a4.png`.

Additional explicit approval: in multiseat play automatically switch immediately to the owned hero whose turn/action needs input. During commitment, move to another owned hero still needing commitment after the current hero finishes. Use stable order for simultaneous eligible owned heroes, avoid frame-by-frame churn, and preserve a manual selection that can still act. Never switch authority to another player's hero; preserve pending targeting. Close with automatic action-turn and commitment switching tests.
