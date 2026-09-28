# Guards Digital - Printed Wording Readings

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [Phase 9](../Roadmap/PHASE_9.md) owns acceptance and status.

## Purpose

How a printed word becomes a composed rule, stated once for every hero. A card's composition must say what its printed words say; `PrintedWordingConformanceTests` holds every dealt face to the families below, so a new hero is checked the moment it is pickable. A face whose wording has two honest readings is raised with the user and recorded here before it is composed. It is never decided in code.

## User rulings

- **W1 (user, 2026-09-28): a plain "repeat" is automatic.** "Repeat once on a different target" with no "may" is performed whenever a legal target exists; the player does not decide. Composed `RepeatAction` / `IfAble`, with no chooser. Applies to Rowenna's Paragon of Grace and Paragon of Valor and Mrak's Epicentre (previously optional), matching Silverarrow's Rain of Arrows, Whisper's Blood Fury, Brynn's Split Throw and Snorri's Rune 4.
- **W2 (user, 2026-09-28): "repeat up to N times" lets the player stop.** The count is the player's choice (p.10), with or without a printed "may". Composed `RepeatAction` / `May` with the actor choosing. Applies to Whisper's Blood Frenzy (previously forced to repeat while able), matching "May repeat up to two times" on Wasp, Sabina, Garrus and Trinkets.
- **W3 (user, 2026-09-28): "Up to N enemy heroes ... discard a card[, or are defeated]" hits every such hero present, up to N.** The attacker does not choose to skip a hero who is there; when more than N stand there, the player chooses which N. Composed as an `IfAble` Optional (no chooser) around each hero's step. Applies to Arien's Dangerous Current, Raging Stream and Violent Torrent (already so), and to Min's Death Grenade and Holy Death Grenade and Trinkets' Self-Destruct and Emergency Protocol (previously skippable).
- **W4 (rulebook, confirmed by the user 2026-09-28): "defeat a minion" is an enemy minion.** The rulebook's Defeat entry and p.13 forbid defeating a friendly unit with no exceptions, so a card that prints "Defeat a minion" without "enemy" offers enemy minions only. Applies to Whisper's Lesser Evil and Greater Good (previously either side) and replaces Wuk's WU-Q4 for Trample and Angry Stampede (previously either side), matching Misa's Watch How I Soar, Swift's Reload, Cutter's Walk the Plank and Trinkets' Rapid Redeployment.
- **W5 (user, 2026-09-28): under "up to N", moving 0 spaces is doing it.** "You may move up to 1 space to a space adjacent to an enemy hero; if you do, push that hero" - the printed "up to" only means something if 0 is a real choice, so Garrus standing beside an enemy hero may move 0 and push. Garrus's Blunt Force and Send Flying are built this way already; 9.3 records it so no later audit changes it.

## Rulebook readings the audit enforces

- **R1 (rulebook p.9 and p.16, milestone 9.2): a Radius-bounded selection reads the printed icon with its items.** The icon "determines the maximum Range/Radius of your action", "in radius" refers to it, and p.16's items raise it; a sentence that writes a number down is that number. Six selections had written the icon's number as a literal reach, so a Radius item did not widen them: Sabina's Close Support and Covering Fire, and Cutter's Bombardment, Barrage, Broadside and X Marks the Spot. They now bind `measuresPrintedRadius`, the same shape milestone 8.83 required of moves.

## Wording families

| Family | Printed words | Composed as | Milestone |
| --- | --- | --- | --- |
| Optionality | "may", "you may" | an Optional node, a `May` / `UpTo` policy, a zero-minimum selection, a `Choose`, a `RepeatAction`, or a Permission / removal-prevention modification | 9.1 |
| Distance and count | "up to N" | `UpTo`, a zero-minimum selection or a `RepeatAction` maximum; an exact number is `Mandatory` / `May` / `IfAble` at that number | 9.1 (moves: 8.83) |
| Ability | "if able" | `IfAble`, or a step p.10 skips silently when nothing can perform it | 9.1 |
| Repeat | "repeat", "may repeat", "repeat up to" | W1 and W2 | 9.1 |
| Targeting | "enemy", "friendly", "defeat a minion" (W4) | the declared `relation`; attack targets are enemies by p.13; a side named in a condition is read off that condition | 9.2 |
| Exclusion | "another", "different", "other" | an `exclude*` input, `distinctTargets` or a `Different*` repeat binding; counting and reminder uses are allow-listed with their reading | 9.2 |
| Reach | "in radius", or a Radius icon with no number in the text | `measuresPrintedRadius` (R1); a literal `reach` only where the sentence writes the number | 9.2 (moves: 8.83) |
| If you do | "if you do", "if they do" | the later step reads the earlier's output, sits after it in a mandatory Sequence, or is gated by a Conditional; W5 for "up to" moves | 9.3 |
| Value changes | "+N" / "-N" to a value, "for each" | a roster value contribution, a value modifier, or the token or marker's own property | 9.3 |
| Each | "each enemy/friendly/unit/hero/minion" | a batch, a per-subject publication or a count; every face read in a register | 9.3 |
| Conditions | "if you do", "if", "each", "all" | 9.3 | 9.3 |

## Required for a new hero

A hero's composition milestone is not complete until each step below holds. The tests enforce the mechanical half on every hero the roster deals, so a hero added to `QuickGameSetupConfiguration` without them fails the harness; the reading half is the dealing agent's.

1. **Read every face against this page before composing it.** For each printed "may", "up to", "if able", "repeat", "enemy", "friendly", "another/different/other", "in radius", "if you do", "+N ... if", "for each" and "each", compose the shape the families table names. A wording that does not fit W1-W5 or R1, or has two honest readings, goes to the user as a question before it is composed, and its answer is added here as the next W-ruling.
2. **Run `PrintedWordingConformanceTests` and `RosterMoveLeashSweepTests`.** A new face that trips a family is corrected, or listed in that family's allow-list with the clause and the reading that settles it. Never list a face to make a red test green without that reason.
3. **Register every new "each" face** in `EveryEachFace_IsReadWithTheShapeThatReachesEverySubject` with the shape that reaches every subject.
4. **Every journey that answers a card's question reads what changed** (`JourneyOutcomeAuditTests`): the board, a hand, a pile, tokens, coins or a value, and after a decline, that nothing changed.
5. **Behaviour behind a fix gets its own regression** at the Edit Mode tier (a staged board where the printed outcome is measured), failing on the old content and passing on the new.
