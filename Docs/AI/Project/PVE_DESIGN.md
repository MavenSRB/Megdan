# PvE design

Canonical owner for original PvE product and rule decisions. Return to [project index](../PROJECT_CONTEXT.md); delivery stages live in [the PvE plan](../Roadmap/PVE.md).

## Approved direction

Design PvE alongside iteration, testing and repairs to the existing private game. Develop the design with the user. No mechanics are approved or claimed implemented by this document.

## Decisions needed for the first playable slice

| Topic | Status | Decision needed |
| --- | --- | --- |
| Experience | Open | Challenge, cooperation and session length. |
| Participation | Open | Player/hero count, solo/cooperative and local/online expectations. |
| Encounter | Open | Minimal scenario, setup, victory, defeat and termination. |
| Enemies | Open | Units, legal actions, decision timing, behavior and allowed information. |
| Existing rules | Open | Reused or intentionally changed movement, combat, cards, progression and turns. |
| Progression | Open | Within-encounter content and any between-encounter persistence. |
| Uncertainty | Open | Whether randomness exists; if approved, seed/state and replay contract. |
| Acceptance | Open | Concrete first-slice player journey, outcomes, exclusions and playtest questions. |

## Rule authority and decision records

Existing-mode and reused tabletop behavior remain governed by [RULES_LEDGER.md](../RULES_LEDGER.md). Original PvE behavior requires user-approved decisions here. Record a stable PVE-Dnn ID, Proposed/Approved/Superseded status, approval reference, exact behavior, mode applicability, departures from reused rules, examples and closing checks. Preserve superseded decisions with successor links. A proposal or test expectation is not rule authority.

PvE approval never silently changes competitive play. Ambiguous reused printed rules use the ledger hierarchy. Do not guess unresolved mechanics or prebuild a speculative framework.

## Engineering boundaries

Keep rules/state independent of Unity and presentation. Enemies submit legal authoritative actions; UI and AI do not own resolution. Identify shared versus mode-specific changes before implementation, including pending choices, host/seat information, identities, snapshots, Undo and replay. Approved nondeterminism must be explicit, recorded and replayable. Test changed contracts and affected consumers while preserving existing-mode behavior.
