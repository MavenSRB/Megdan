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

### Token property capabilities

A token definition composes explicit continuous/query/trigger capabilities such as obstacle classification, terrain-like restriction, entry/exit effect, modifier source, immunity interaction, or removal condition. Token art does not define behavior.

Entry/exit effects run from authoritative movement/displacement checkpoints. A unit that ignores obstacles still applies token effects while transiting where the rules require it.

Move, Object Push, Swap, Replace, and Remove may affect a token only when the source definition's target contract permits tokens. The stable token instance and its persistent properties move or change location with the operation; they are never reconstructed from art.

### Markers

Markers are stable reminder/effect attachments, usually assigned to heroes rather than placed on board spaces. `GiveMarkerEffect` records marker definition, owner, holder, source effect, and return condition. `ReturnMarkerEffect` detaches it and returns it to the correct supply/owner.

A marker may provide an attached continuous modifier when verified text requires it. That modifier follows the marker attachment lifetime rather than token or card Active-effect rules.

Markers are not tokens, do not occupy spaces, and do not inherit token removal or obstacle rules. All markers return to supply at end of round, and every marker held by a defeated hero is returned. Exact effects may add an earlier return condition, but cannot weaken those general cleanup checkpoints without verified overriding text.

### Obstacles and terrain

`ObstacleQuery` uses effective characteristics. Units, terrain, tokens, and qualifying miniatures/objects are obstacles, with no more than one obstacle per space.

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

### Counts As

`CountsAsModifier` replaces all applicable rules and qualities of the object with the specified effective type(s) for its duration. Type-based targeting, occupancy, immunity, rewards, and other rules queries use those effective characteristics. A capability that cites intrinsic miniatures/representation explicitly declares `IntrinsicKindQuery`; for example, the p.18 Battle-Zone correction applies only to minion miniatures and excludes objects that merely count as minions.

### Immunity

`ImmunityModifier` declares immune object, scope (action/effect/type/source), duration, and exceptions. Immunity prevents targeting and being affected but does not remove the object from presence checks. A hero is never immune to their own actions.

### Permissions and prohibitions

Typed permission/restriction capabilities include:

- Prevent or allow a named action.
- Limit Movement distance, checked at Movement action start where required.
- Ignore obstacles or move through named obstacle types.
- Allow otherwise illegal targets or spaces.
- Prevent removal, a named outcome, card-state change, or another exact capability.

These modify capability queries. They do not inject card-name branches into executors.

## Values, resources, and ownership changes

`ModifyValueEffect` references a typed value channel and contribution source, such as Initiative, Attack, Defense, Movement, Range, or Radius. Printed icons, items, card text, minions, and Active effects remain separate contributions.

Milestone 5.3 implements the general minion Defense subset from rulebook page 14. A discarded Defense card keeps its printed-icon contribution, then the value query adds +1 for every adjacent friendly Melee/Heavy minion, -1 for every adjacent enemy Melee/Heavy minion, and -1 for every enemy Ranged minion within radius 2. Every added contribution carries the stable authoritative `MinionId`; decline or no legal Defense card still produces no Defense Total.

Milestone 5.8 makes the future combat source seams executable data rather than labels: printed-icon, minion, item-card, card-text/action, and Active-effect-instance contributions use distinct discriminators and typed source identity. Only the verified synthetic printed/minion producers are populated. `DefenseBlockResult` is a separate nonnumeric result override; a fulfilled future Block never becomes a sentinel Defense amount.

Milestone 3.4 establishes the current synthetic Initiative consumer: the printed card-wide Initiative icon is projected through the same typed contribution/result shape without inventing an action-definition source, and the next unresolved actor query recalculates from current contributions at every boundary. Later Initiative modifiers extend that contribution query rather than adding a cached turn-controller field.

Other typed state changes include gain/spend coins, spend Lives, flip/remove Wave counters, change hero level, and assign/return ownership or control where verified text permits it. Resource effects declare minimums, failure behavior, recipient, source, and emitted contribution events; they never use negative magic numbers as hidden semantics.

Milestone 5.5 implements the pure coin-gain subset for Defeat outcomes. Each outcome carries a stable outcome/target identity, defeated team, acting hero, and already-verified Defeat/Assist amounts. Accounting validates the participant graph and enemy team, awards the acting hero first and every other friendly hero in stable participant order, and emits typed operations containing source, recipient hero/team, Defeat-versus-Assist kind, positive amount, zero minimum, reject-without-mutation behavior, and before/after balances. The entire recipient batch is planned before mutation, duplicate outcomes reject, and a separately typed removed/removal-prevented result cannot reverse the reward. Milestone 7.4 owns exact hero Level/lowest-Tier derivation; spending and presentation remain with their owning milestones.

Milestone 5.6 supplies the verified minion values, integrates the acting-hero-only award with minion Attack, and persists balances plus awarded-outcome identity in snapshot schema v4. Exact hero Level/lowest-Tier derivation remains milestone 7.4; spending and presentation remain deferred.
