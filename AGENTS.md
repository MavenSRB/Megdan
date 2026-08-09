# Guards Digital - Agent Entry Point

Read these files before planning or changing anything in this workspace:

1. `Docs/AI/PROJECT_CONTEXT.md`
2. `Docs/AI/ROADMAP.md`
3. `Docs/AI/RULES_LEDGER.md`
4. `Docs/AI/CARD_SYSTEM_ARCHITECTURE.md` for any card, action, effect, combat, turn, progression, snapshot, or rules-authority work
5. `Docs/AI/CARD_EFFECT_CAPABILITIES.md` for any card effect, keyword, target/query, modifier, token, marker, board-object, timing, or system-transition work
6. `Docs/AI/TESTING.md` before running or changing automated tests
7. `Docs/Guards of Atlantis II - 2nd Printing_web.pdf` when the task depends on a game rule

## Current authorization

Roadmap milestones 0.1 through 0.6 and 1.1 through 1.3 are complete: living documentation, the Unity/test shell, the card-system and effect-capability architecture contracts, the core-match-first dependency order, hex foundations, minimal rendering, and one primitive hero exist. A local Git repository is authorized and initialized.

Milestone 1.4 is proposed but not authorized. Do not begin it or any later gameplay, presentation, card, or build milestone until the user explicitly approves that exact micro-milestone.

## Non-negotiable working rules

- Treat the second-printing rulebook, verified card data, and published official rulings as authoritative according to the hierarchy in `RULES_LEDGER.md`.
- Do not invent, simplify, rebalance, or silently reinterpret a rule. Record every approved deviation in `RULES_LEDGER.md`.
- Work on exactly one user-approved micro-milestone at a time. Satisfy its acceptance checks, update the living documents, report the result, and stop for approval.
- After every completed user-approved roadmap milestone, create a local Git commit containing that milestone and its continuity updates. Do not configure a remote or push without explicit user authorization.
- Do not continue into adjacent features merely because they appear easy or logically related.
- Use the smallest implementation that proves the current behavior, while preserving the architectural constraints in `PROJECT_CONTEXT.md`.
- Keep authoritative rules and state independent from Unity scene objects, presentation, animation, and audio.
- Preserve the definition/instance, action-offer, command/event, pending-choice, timing, and versioning contracts in `CARD_SYSTEM_ARCHITECTURE.md`, plus the typed effect/query contracts in `CARD_EFFECT_CAPABILITIES.md`. Do not add card-name/hero-name rule branches, runtime printed-text parsing, or UI-owned card resolution.
- Add focused automated tests for the behavior being changed. Add regression tests for bugs. Run the broader suite only at roadmap milestone boundaries unless risk requires more.
- Do not repeatedly retest the whole game for a narrow change.
- Preserve user-created and reference files. Do not bulk-process or reorganize the card corpus unless the current milestone requires it.
- The local rulebook and extracted card corpus are intentionally excluded from Git. Preserve them at their documented paths and never add them to a commit or distribute them without explicit authorization and appropriate rights.
- Keep the project private. Do not publish, upload, market, monetize, or distribute game assets or builds without explicit authorization from the user and the relevant rights holder.

## Continuity updates

After an approved decision or completed milestone:

- Update `PROJECT_CONTEXT.md` when product scope, constraints, terminology, or architecture changes.
- Update `ROADMAP.md` when milestone status or sequencing changes.
- Update `RULES_LEDGER.md` when a rule is verified, implemented, tested, clarified, or intentionally deviated from.
- Update `CARD_SYSTEM_ARCHITECTURE.md` when a verified rule or card changes a card-engine contract or extension boundary.
- Update `CARD_EFFECT_CAPABILITIES.md` when a verified rule or card adds or changes an effect, keyword, query, modifier, timing, or system-transition contract.
- If these files disagree, stop and resolve the conflict with the user before implementation.
