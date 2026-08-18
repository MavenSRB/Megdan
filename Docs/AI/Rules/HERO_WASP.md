# Guards Digital - Wasp Rules Shard

Canonical active shard. Return to [../RULES_LEDGER.md](../RULES_LEDGER.md).

Hero-scoped rule authority lives one shard per approved hero, named `Rules/HERO_<NAME>.md`. A hero shard
owns that hero's content inventory, its mapping onto effect capabilities, its source-verification status,
and any deviation or open question scoped to that hero alone. Nothing else belongs here.

The boundary against the ledger is deliberate and runs the other way for anything shared. A rule system
that spans heroes stays in the ledger's [Rule-system ledger](../Rules/RULE_SYSTEMS.md#rule-system-ledger),
and a deviation or open question that affects more than one hero stays in [RULES_LEDGER.md](../RULES_LEDGER.md)
rather than being copied into each hero's shard - a rule restated per hero is a rule that will disagree
with itself as soon as one copy is edited. A shard exists only for a hero with an approved roster slot;
the five heroes joining Wasp are selected at milestone 8.17, and each gets its shard then, not before.
## Wasp content ledger

The pitch requires Wasp's complete progression. Before verified Wasp definitions or effects are implemented, create an implementation-grade inventory containing at least:

- Stable definition ID and content revision.
- Display name, source image path/face, edition, and starting hand/deck/set-aside membership.
- Hero/content-set ID, typed `CardColor`, nullable tier, and explicit same-color upgrade/replacement relationships.
- Basic/non-basic/Ultimate/Handicap classification as applicable.
- Stable action-definition IDs; derived Primary/Secondary/reaction/replacement offer contexts; subtype; and text-application policy.
- Initiative, Attack, Defense, Movement, Range, and Radius values where present.
- Exact rules text transcription.
- Ordered mandatory/optional steps; choice owner; modal/repeat behavior.
- Structured triggers, conditions, targets versus presence checks, choices, values, effects, batch/simultaneous behavior, timing, duration, and cancellation.
- Stable effect-node IDs, capability ID/version, typed input/result bindings, operation-specific policies, outcome facts, and systemic checkpoints.
- Reused capability IDs and any controlled custom-handler ID.
- Item contribution and destination when set aside; Active-effect versus passive/continuous-source classification; Ultimate persistence requirements.
- Required source-linked scenarios and interaction/ordering tests.
- Source verification status and reviewer/date.
- Any official correction or clarification.

Current status: **Not inventoried**. Wasp inventory is deferred until milestone 8.1, after milestone 7.9 proves the deterministic synthetic core match; its capability matrix follows in 8.2. Other heroes remain outside the pitch inventory.

