# Ordinary Skill Consequences

Canonical active contract. Return to [Resolution and effects](RESOLUTION_AND_EFFECTS.md).

An ordinary Skill's typed minion-Defeat request must reach the existing card-resolution Defeat authority;
recording an effect-node outcome alone is not removal. The Skill execution context pauses after the
requesting node, with that node already recorded and the persisted continuation positioned after it.
Card authority settles the reward and removal attempt before resuming later effect nodes or closing
the action. It preserves the authoritative Defeat facts and the existing after-action Lane Push boundary.

Removal prevention can keep a defeated minion on the board without undoing its Defeat reward. While a
player answers prevention, the existing pending Skill frame and prevention resolution survive snapshot
restore. The enclosing resolution identity distinguishes this pause from prevention opened by a separate
triggered effect. Answering resumes the correct owner and completes the Skill through the normal turn
boundary; it must not leave an unresolved turn or repeat the reward. The requesting node discriminates
the Defeat outcome identity within the action.

The staged request list is transient output from one execution call, not a second persistence authority.
Snapshot schema remains unchanged. Triggered effects retain their existing completion-owned Defeat
consumer and ordering, including effects that must read the target's former adjacency before removal.

`MisaSkillDefeatTests` covers ordinary accepted Defeat, selection restore, prevention payment/decline,
reward idempotency, Undo, stale confirmation and subsequent turn release. The configured Misa player
journey selects the printed Defeat bullet and its minion through real controls. Content target eligibility
still follows its verified definition and rulebook p.13; neither the UI nor this continuation adds rules.
