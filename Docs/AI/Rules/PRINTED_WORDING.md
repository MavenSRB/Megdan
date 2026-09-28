# Guards Digital - Printed Wording Readings

Canonical active shard. Return to [RULES_LEDGER.md](../RULES_LEDGER.md). [Phase 9](../Roadmap/PHASE_9.md) owns acceptance and status.

## Purpose

How a printed word becomes a composed rule, stated once for every hero. A card's composition must say what its printed words say; `PrintedWordingConformanceTests` holds every dealt face to the families below, so a new hero is checked the moment it is pickable. A face whose wording has two honest readings is raised with the user and recorded here before it is composed. It is never decided in code.

## User rulings

- **W1 (user, 2026-09-28): a plain "repeat" is automatic.** "Repeat once on a different target" with no "may" is performed whenever a legal target exists; the player does not decide. Composed `RepeatAction` / `IfAble`, with no chooser. Applies to Rowenna's Paragon of Grace and Paragon of Valor and Mrak's Epicentre (previously optional), matching Silverarrow's Rain of Arrows, Whisper's Blood Fury, Brynn's Split Throw and Snorri's Rune 4.
- **W2 (user, 2026-09-28): "repeat up to N times" lets the player stop.** The count is the player's choice (p.10), with or without a printed "may". Composed `RepeatAction` / `May` with the actor choosing. Applies to Whisper's Blood Frenzy (previously forced to repeat while able), matching "May repeat up to two times" on Wasp, Sabina, Garrus and Trinkets.
- **W3 (user, 2026-09-28): "Up to N enemy heroes ... discard a card[, or are defeated]" hits every such hero present, up to N.** The attacker does not choose to skip a hero who is there; when more than N stand there, the player chooses which N. Composed as an `IfAble` Optional (no chooser) around each hero's step. Applies to Arien's Dangerous Current, Raging Stream and Violent Torrent (already so), and to Min's Death Grenade and Holy Death Grenade and Trinkets' Self-Destruct and Emergency Protocol (previously skippable).

## Wording families

| Family | Printed words | Composed as | Milestone |
| --- | --- | --- | --- |
| Optionality | "may", "you may" | an Optional node, a `May` / `UpTo` policy, a zero-minimum selection, a `Choose`, a `RepeatAction`, or a Permission / removal-prevention modification | 9.1 |
| Distance and count | "up to N" | `UpTo`, a zero-minimum selection or a `RepeatAction` maximum; an exact number is `Mandatory` / `May` / `IfAble` at that number | 9.1 (moves: 8.83) |
| Ability | "if able" | `IfAble`, or a step p.10 skips silently when nothing can perform it | 9.1 |
| Repeat | "repeat", "may repeat", "repeat up to" | W1 and W2 | 9.1 |
| Targeting | "another", "different", "in radius", "adjacent", "enemy", "friendly" | 9.2 | 9.2 |
| Conditions | "if you do", "if", "each", "all" | 9.3 | 9.3 |
