# Guards Digital - Card-System Architecture Index

The card-system contract is authoritative across the indexed active shards below. Read only the shard routed by the task, plus any dependency it names.

## Canonical active shards

- [Core model](Card-System/CORE_MODEL.md)
- [Resolution and effect contracts](Card-System/RESOLUTION_AND_EFFECTS.md)
- [Ordinary Skill consequences](Card-System/SKILL_CONSEQUENCES.md)
- [Validation and extension contracts](Card-System/VALIDATION_AND_EXTENSION.md)

Effect/query capability authority remains in the [CARD_EFFECT_CAPABILITIES.md](CARD_EFFECT_CAPABILITIES.md) index and its routed shards. Rule verification status remains in [RULES_LEDGER.md](RULES_LEDGER.md).
