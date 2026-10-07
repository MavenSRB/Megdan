# Experimental features history

Preserved losslessly from [the active experimental-features shard](../Project/EXPERIMENTAL_FEATURES.md). This archive is not current authorization; the [session brief](../SESSION_BRIEF.md) owns the active route.

## Milestone 9.58 teammate coordination closing checks

These acceptance obligations were checked at 9.58; its completion commit owns the evidence:

1. Host and joined seats in 2v2/3v3 see the same per-match option; it is independent of the timer and resets off on a new host game. Option-off preserves ordinary hand privacy, input and selection behavior.
2. Both allied board-token and tray clicks reach the intended hand and return to the player's own cards. Team eligibility is checked before projection/transmission, including hostile or stale requests; enemy private cards never reach an unauthorized client.
3. Right-click works on hexes and inspected cards through every match phase. It does not submit a gameplay choice, cast a Minion Battle vote, cancel a pending action, change a timer or trigger an existing right-click gameplay binding as a second action. Read-only suggestion handling must not wait on the current actor's decision.
4. Same-target toggle, movement to another target, simultaneous teammates and coexistence with effect/selection/white highlights remain attributable and independent. Removing the communication layer leaves the underlying cues intact. Use the actual tray token, the smaller board size, and supported viewport checks; no header growth or obscured controls.
5. Use stable player/card identities across tray rebuilds and hand changes, not visual slot indexes. Remove a card suggestion when its target leaves the inspectable hand rather than transferring it to a replacement card. Seat loss, reconnect, match exit and Director restoration must not leave ghost markers, stale hand visibility or a suggestion attached to the wrong target. These presentation messages cannot block authoritative resolution or alter gameplay outcomes.

The reviewed mockups illustrate layout, not exact new artwork. The written right-click and existing-token amendments supersede their earlier hover/invented-badge versions. No gameplay implementation, assets or private reference images are added by this documentation milestone.
