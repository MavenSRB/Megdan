# Guards Digital - State Effect Capabilities

Canonical effect-capability shard. Return to the [Card Effect Capabilities index](../CARD_EFFECT_CAPABILITIES.md).

## Tokens, markers, obstacles, and terrain

### Token instances and supply

Every placed token has a stable instance ID, token definition ID/revision, owner/controller/source links, occupied space, and persistent property source. Tokens:

- Are obstacles and are not units.
- Use an authoritative limited supply.
- Retain their properties until removed; those properties are continuous sources, not ordinary Active effects.
- Are not removed merely because the hero who placed them is defeated.
- Are normally removed at end of round unless exact text says otherwise.
- Are removed before a unit Spawns on their covered Spawn point.
- May be removed by Clear or another explicit Remove effect.

When a card requests more tokens than remain in supply, its typed token-placement capability follows the rulebook's removal/reuse choice before placement. The pending choice identifies which same-type tokens may be removed and never lets UI create extra copies.

Milestone 6.2 implements the instance, supply, occupancy, placement, and reclamation subset. A placed token holds its `TokenDefinition` object rather than a copy of its content, so a restore rebinds it to the trusted catalog and a payload can name which token this is but never what it means. Supply is derived - the catalog total minus what is on the board - so no stored count can disagree with the board. Reclamation and placement are separate nodes on the milestone 6.2a effect frame, because a stateless handler cannot distinguish a second resumption from a first, so a second decision belongs to a second node. Nothing outside `Core` can begin or end a token's lifetime; a mechanical rule enforces that against the presentation assembly.

Milestone 6.3 implements the persistent-property and first general-interaction subset. `TokenDefinition` owns immutable property declarations, and each `PlacedToken` derives stable `ContinuousRuleSourceKind.TokenProperty` sources from that trusted object. The board exposes the flattened sources only for currently placed instances: neither source-card resolution nor loss of the placing hero's occupancy cancels them, while token removal drops them without entering `ActiveEffectLifecycle`. The same stable token instance is removed by Clear or by the mandatory pre-Spawn clearance step; later movement/displacement will relocate rather than reconstruct it.

### Token property capabilities

A token definition composes explicit continuous/query/trigger capabilities such as obstacle classification, terrain-like restriction, entry/exit effect, modifier source, immunity interaction, or removal condition. Token art does not define behavior.

The implemented milestone 6.3 declaration carries stable property identity, rule kind, and applicability kind. It establishes the source/lifetime contract without inventing a concrete verified modifier, trigger, or Wasp rule; those consumers arrive with the milestone and source that require them.

Entry/exit effects run from authoritative movement/displacement checkpoints. A unit that ignores obstacles still applies token effects while transiting where the rules require it.

Move, Object Push, Swap, Replace, and Remove may affect a token only when the source definition's target contract permits tokens. The stable token instance and its persistent properties move or change location with the operation; they are never reconstructed from art.

### Markers

Markers are stable reminder/effect attachments, usually assigned to heroes rather than placed on board spaces. `GiveMarkerEffect` records marker definition, owner, holder, source effect, and return condition. `ReturnMarkerEffect` detaches it and returns it to the correct supply/owner.

A marker may provide an attached continuous modifier when verified text requires it. That modifier follows the marker attachment lifetime rather than token or card Active-effect rules.

Markers are not tokens, do not occupy spaces, and do not inherit token removal or obstacle rules. All markers return to supply at end of round, and every marker held by a defeated hero is returned. Exact effects may add an earlier return condition, but cannot weaken those general cleanup checkpoints without verified overriding text.

Milestone 6.2 implements the attachment, its finite supply, its explicit return condition, and the return transition; milestone 6.8h gives the attachment a per-activation instance identity, so two attachments made by two activations of one card are two markers rather than one identity reused. The attachment record has no coordinate field at all and the board occupancy index has no marker arm, so "markers are not board occupants" is structural rather than asserted. `MarkerReturnCondition` currently declares only the general cleanup both checkpoints share; an exact effect's earlier condition is a member added with the verified text that requires it, never ahead of it. Neither cleanup checkpoint is wired yet - milestone 6.12 owns end-of-round cleanup, and the defeat return stays with it rather than being wired alone.

### Obstacles and terrain

`ObstacleQuery` uses effective characteristics. Units, terrain, tokens, and qualifying miniatures/objects are obstacles, with no more than one obstacle per space. Milestone 6.8 implements it over the board's shared occupancy index and the playable-space set rather than re-deriving obstacles per kind, and separates the transit question the Move-Through permission can answer from the endpoint question no unit may answer.

Terrain is a space classification and obstacle, not a token or unit. Effects that add/remove terrain-like properties do so through explicit continuous characteristics without replacing the underlying space ID.

## Card-state and progression capabilities

### Card-characteristic queries

`CardCharacteristicQuery` derives eligible card instances from authoritative definitions and current instance state. Its typed predicates may include `CardColor`, Basic/non-Basic/Ultimate classification, tier, owner/controller, location, lifecycle facts, and rules-facing information availability. `CardColor` is distinct from team identity, presentation color, and any runtime or UI slot, and it is never parsed from artwork, names, or UI styling.

A color-restricted choice, condition, count, cost, or effect composes this pure query with the applicable control-flow or mutation capability. For example, a verified effect that requires an opponent to discard a card of a specified color first derives the opponent-owned legal candidates in the source-allowed state, then records the correct chooser and pending choice when needed, and finally invokes `DiscardCard` on the accepted candidate. `DiscardCard` does not independently guess color eligibility, ownership, visibility, or chooser. If no candidate exists, optionality and failure behavior come from the verified effect contract.

Queries must respect context-sensitive information suppression. A color unavailable to the acting rules context because of face orientation or another explicit state cannot satisfy a color predicate merely because presentation or immutable content data can still inspect the underlying definition. Identical authoritative state and query context produce the same ordered candidate set.

Reusable card-state transitions include:

- `PlayCard`, `RevealCard`, `ResolveCard`, and `ResolveWithoutAction`.
- `DiscardCard` from the source-allowed state, including exactly one in-hand Defense card.
- `RetrieveCard` for the correct owner and allowed states.
- `SwapCardStates` as the rulebook-defined state exchange, not two arbitrary list moves.
- `TurnCardFacedown`/`TurnCardFaceup` with the correct resolved/discarded information suppression and Active-effect cancellation.
- `UpgradeCard`, `SetAsideAsItem`, and `UnlockUltimate` using immutable definitions and explicit instance transitions.

Card-state effects retain orthogonal location, played, resolution, and orientation facts. Every transition validates owner, source state, and definition revision.

Milestone 3.5 deliberately uses `ResolveCard` after the performed Hold action. It does not alias Hold to `ResolveWithoutAction`; the latter remains reserved for the separately timed defeat and declined-respawn paths.

Milestone 3.7 uses the same `ResolveCard` transition only after the selected played card's outer Attack has resumed from Defense and closed. `DiscardCard` for the defender remains an independently validated reaction cost and never resolves or consumes that defender's played turn card.

## Characteristics, immunity, permissions, and restrictions

Milestone 6.8 implements the general contracts these four subjects share. One immutable `RuleModification` declares a value modifier, restriction, permission, immunity, or `Counts As`; `AppliedRuleModification` binds it to the rules object it affects and the live source supplying it; and `RuleModificationSet` answers the questions below in one canonical order. Declarations are definition content on `ActiveEffectDefinition` and `TokenPropertyDefinition`, so a restored effect or token rebinds to the trusted catalog and produces the same modifications again; no snapshot field carries a rule. `RuleModificationQuery` derives the live set from Active-effect instances in their active phase plus the properties of currently placed tokens, and rebuilds it per command rather than caching it. Ordering follows p.17: the revealed Tie-Breaker side first, then the source's creation sequence, then its stable identity, with no flip.

### Counts As

`CountsAsModifier` replaces all applicable rules and qualities of the object with the specified effective type(s) for its duration. Type-based targeting, occupancy, immunity, rewards, Minion-Battle membership, and other rules queries use those effective characteristics. A capability that cites intrinsic miniatures/representation explicitly declares `IntrinsicKindQuery`; for example, the p.18 Battle-Zone correction applies only to minion miniatures and excludes objects that merely count as minions. Under the approved 6.5aa interpretation, a counts-as-minion hero is also a Minion-Battle removal candidate; removal leaves it off board without Defeat/reward/Life consequences and the ordinary p.7 Hero Respawn choice follows at its next action opportunity.

`EffectiveUnitKindQuery` implements the replacement and `IntrinsicKindQuery` the representation, and the second deliberately accepts no modification set at all, so a rule that cites miniatures cannot consult a `Counts As` declaration even by accident. A hero counting as a minion therefore stops satisfying a hero-typed query while remaining a hero for identity, ownership, occupancy kind, supply, and the p.18 correction.

### Immunity

`ImmunityModifier` declares immune object, scope (action/effect/type/source), duration, and exceptions. Immunity prevents targeting and being affected but does not remove the object from presence checks. A hero is never immune to their own actions.

Corrective milestone 6.5aa completes that split through `ImmunityQuery`: Attack, direct Move, Object Push, and Place ask the same exact-activity decision both while deriving candidates and immediately before mutation. The query composes declared immunity with the p.13 standing Heavy rule, including effective-minion accompaniment, while `UnitPresenceQuery` consults no immunity and still counts the object. An immunity naming one activity leaves every other activity unaffected, and the self exception is answered before any declaration is read.

### Permissions and prohibitions

Typed permission/restriction capabilities include:

- Prevent or allow a named action.
- Limit Movement distance, checked at Movement action start where required.
- Ignore obstacles or move through named obstacle types.
- Allow otherwise illegal targets or spaces.
- Prevent removal, a named outcome, card-state change, or another exact capability.

These modify capability queries. They do not inject card-name branches into executors.

A restriction names an activity and a permission names the exact grant it gives, so the two are separate queries and a narrow permission is structurally unable to disable an unrelated restriction. Milestone 6.8 implements the Fast-Travel restriction against the general-rule offer query, which removes the offer rather than rejecting the command later, and `ObstacleQuery` as the p.18 obstacle decision point that honours the Move-Through permission for transit while refusing every unit an obstacle endpoint. The Movement action does not consult that permission yet: crossing an occupied space requires a move to assert occupancy only at its endpoint, which is a change to route commitment owned by the movement and displacement milestones, and wiring it into today's one-space-at-a-time commit would offer destinations the commit path must then refuse.

## Values, resources, and ownership changes

`ModifyValueEffect` references a typed value channel and contribution source, such as Initiative, Attack, Defense, Movement, Range, or Radius. Printed icons, items, card text, minions, and Active effects remain separate contributions.

Milestone 5.3 implements the general minion Defense subset from rulebook page 14. A discarded Defense card keeps its printed-icon contribution, then the value query adds +1 for every adjacent friendly Melee/Heavy minion, -1 for every adjacent enemy Melee/Heavy minion, and -1 for every enemy Ranged minion within radius 2. Every added contribution carries the stable authoritative `MinionId`; decline or no legal Defense card still produces no Defense Total.

Milestone 5.8 makes the future combat source seams executable data rather than labels: printed-icon, minion, item-card, card-text/action, and Active-effect-instance contributions use distinct discriminators and typed source identity. Only the verified synthetic printed/minion producers are populated. `DefenseBlockResult` is a separate nonnumeric result override; a fulfilled future Block never becomes a sentinel Defense amount.

Milestone 3.4 establishes the current synthetic Initiative consumer: the printed card-wide Initiative icon is projected through the same typed contribution/result shape without inventing an action-definition source, and the next unresolved actor query recalculates from current contributions at every boundary. Later Initiative modifiers extend that contribution query rather than adding a cached turn-controller field.

Other typed state changes include gain/spend coins, spend Lives, flip/remove Wave counters, change hero level, and assign/return ownership or control where verified text permits it. Resource effects declare minimums, failure behavior, recipient, source, and emitted contribution events; they never use negative magic numbers as hidden semantics.

Milestone 5.5 implements the pure coin-gain subset for Defeat outcomes. Each outcome carries a stable outcome/target identity, defeated team, acting hero, and already-verified Defeat/Assist amounts. Accounting validates the participant graph and enemy team, awards the acting hero first and every other friendly hero in stable participant order, and emits typed operations containing source, recipient hero/team, Defeat-versus-Assist kind, positive amount, zero minimum, reject-without-mutation behavior, and before/after balances. The entire recipient batch is planned before mutation, duplicate outcomes reject, and a separately typed removed/removal-prevented result cannot reverse the reward. Milestone 7.4 owns exact hero Level/lowest-Tier derivation; spending and presentation remain with their owning milestones.

Milestone 5.6 supplies the verified minion values, integrates the acting-hero-only award with minion Attack, and persists balances plus awarded-outcome identity in snapshot schema v4. Exact hero Level/lowest-Tier derivation remains milestone 7.4; spending and presentation remain deferred.

## Contract implementation status

See [CORE_MODEL.md](CORE_MODEL.md#contract-implementation-status) for what this section is and how to maintain it.

| Contract | State | Owning milestone |
| --- | --- | --- |
| `CardCharacteristicQuery` | Specified, not implemented. Milestone 6.8 implemented the characteristic, immunity, permission, and restriction contracts its acceptance names; card-characteristic predicates are a separate subject with no consumer yet. | Named trigger: the first verified card that selects, counts, or restricts by card characteristic |
