# Superseded Testing Recipes

Lossless historical recipes removed from active testing authority. Return to [TESTING.md](../TESTING.md).

## Historical snapshot-backed Director Undo and restart tests

Run `CardDrivenMovementPresentationTests` and `AuthoritativeSnapshotSerializerTests` together and write outputs to `tmp/milestone-3-10-focused-edit-results.xml` and `tmp/milestone-3-10-focused-edit.log`. For directly affected Edit Mode coverage, use the milestone 3.9 filter above and write outputs to `tmp/milestone-3-10-affected-edit-results.xml` and `tmp/milestone-3-10-affected-edit.log`. Run `MinimalHexGridPlayModeTests` and write outputs to `tmp/milestone-3-10-focused-play-results.xml` and `tmp/milestone-3-10-focused-play.log`.

Expected result: every accepted director commitment, Pass, Initiative choice, Hold, action start, pending answer, and end-of-round continuation records its exact pre-command authoritative snapshot; rejected and presentation-only inputs add no entry. Undo pops and restores that boundary without recording itself, restart restores the initial scenario and clears history, and restored card/round/Tie Breaker/pending/board state rebuilds active actor/card focus, hero/obstacle views, pending highlights, Defense controls, and the compact enabled/disabled HUD buttons. Milestone 3.10 T2 evidence passed focused Edit Mode 23/23, directly affected Edit Mode 156/156, and saved-scene Play Mode 12/12 with zero failed, skipped, or inconclusive cases and the healthy 39-package baseline. Source/layout inspection confirmed `UNDO` and `RESTART` share the existing header row without increasing the bounded panel height. Complete unfiltered suites were intentionally omitted because milestone 3.11 remains the Phase 3 T3 gate and the affected snapshot/authority/controller/presentation consumers are enumerated.

The preceding result is historical evidence for the currently implemented milestone 3.10 behavior. Corrective milestone 3.10a supersedes its Undo-history target without invalidating snapshot serialization tests.
