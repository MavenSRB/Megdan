# Guards Digital - State Effect Capabilities

<!-- operative-claim:F-DOC-007 status:CurrentDemonstrated -->

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

The 8.43a groundwork adds `TokenRemovalCondition.RetainedAtEndOfRound` as an explicit immutable-definition override required by Dragon Bond. The existing cleanup filter leaves those instances unchanged; ordinary Clear, Spawn clearance and Remove remain available, and controller defeat does not remove tokens. `None` and undefined conditions are rejected. No production definition uses this override until Widget's component supply is verified and registered.

`SyntheticOwnedTokenCapabilities` provides two deferred, definition-bound factory methods, PresenceHandler and SpaceHandler, for a single-piece supply. They read only the acting hero's exact owned token definition/revision, not another owner's token or a matching display name. Presence publishes a boolean; position publishes the live coordinate or is impossible, with optional trusted Range/Radius measured from the actor using ordinary item/value queries. Both publish read-only feasibility facts without creating a choice or mutation. Multi-piece supplies and unsupported reach channels are refused at declaration. This narrow vocabulary is not production registration; 8.43a still owes the source-backed component and composed operation paths, and 8.43e owns reachable registration. `OwnedTokenCapabilityTests` and the test-owned foundation claim catalog cover the current slice.

A token definition composes explicit continuous/query/trigger capabilities such as obstacle classification, terrain-like restriction, entry/exit effect, modifier source, immunity interaction, or removal condition. Token art does not define behavior.

The implemented milestone 6.3 declaration carries stable property identity, rule kind, and applicability kind. It establishes the source/lifetime contract without inventing a concrete verified modifier, trigger, or Wasp rule; those consumers arrive with the milestone and source that require them.

Entry/exit effects run from authoritative movement/displacement checkpoints. A unit that ignores obstacles still applies token effects while transiting where the rules require it.

Move, Object Push, Swap, Replace, and Remove may affect a token only when the source definition's target contract permits tokens. The stable token instance and its persistent properties move or change location with the operation; they are never reconstructed from art.

### Markers

Markers are stable reminder/effect attachments, usually assigned to heroes rather than placed on board spaces. `GiveMarkerEffect` records marker definition, owner, holder, source effect, and return condition. `ReturnMarkerEffect` detaches it and returns it to the correct supply/owner.

A marker may provide an attached continuous modifier when verified text requires it. That modifier follows the marker attachment lifetime rather than token or card Active-effect rules.

Markers are not tokens, do not occupy spaces, and do not inherit token removal or obstacle rules. All markers return to supply at end of round, and every marker held by a defeated hero is returned. Exact effects may add an earlier return condition, but cannot weaken those general cleanup checkpoints without verified overriding text.

Milestone 6.2 implements the attachment, its finite supply, its explicit return condition, and the return transition; milestone 6.8h gives the attachment a per-activation instance identity, so two attachments made by two activations of one card are two markers rather than one identity reused. The attachment record has no coordinate field at all and the board occupancy index has no marker arm, so "markers are not board occupants" is structural rather than asserted. `MarkerReturnCondition` currently declares only the general cleanup both checkpoints share; an exact effect's earlier condition is a member added with the verified text that requires it, never ahead of it. Milestone 6.12 wires both checkpoints from one shared statement of which components each takes: p.8's end-of-round stage returns every attachment declaring the general cleanup and removes every placed token, and p.13's defeat return takes only the markers the defeated hero held, leaving markers that hero gave away with their holders and leaving its tokens on the board, which is p.19's own sentence. Tokens gained the matching declaration at the same time - `TokenRemovalCondition` carries only the baseline `EndOfRoundCleanup`, because p.19 conditions the removal on "unless the card text says otherwise" and no verified card text yet says otherwise.

### Obstacles and terrain

`ObstacleQuery` uses effective characteristics. Units, terrain, tokens, and qualifying miniatures/objects are obstacles, with no more than one obstacle per space. Milestone 6.8 implements it over the board's shared occupancy index and the playable-space set rather than re-deriving obstacles per kind, and separates the transit question the Move-Through permission can answer from the endpoint question no unit may answer.

Terrain is a space classification and obstacle, not a token or unit. Effects that add/remove terrain-like properties do so through explicit continuous characteristics without replacing the underlying space ID.

Terrain is a passable obstacle distinct from off-board: the official map's 82 declared terrain cells cost a Move-Through mover nothing extra, and no unit may end a Movement or be placed there. That sentence was `SESSION_BRIEF.md`'s until milestone 7.8e moved it here, which is the section that owns it; the cell count itself is inventoried in [../Maps/README.md](../Maps/README.md).

## Card-state and progression capabilities

### Card-characteristic queries

`CardCharacteristicQuery` derives eligible card instances from authoritative definitions and current instance state. Its typed predicates may include `CardColor`, Basic/non-Basic/Ultimate classification, tier, owner/controller, location, lifecycle facts, and rules-facing information availability. `CardColor` is distinct from team identity, presentation color, and any runtime or UI slot, and it is never parsed from artwork, names, or UI styling.

A color-restricted choice, condition, count, cost, or effect composes this pure query with the applicable control-flow or mutation capability. For example, a verified effect that requires an opponent to discard a card of a specified color first derives the opponent-owned legal candidates in the source-allowed state, then records the correct chooser and pending choice when needed, and finally invokes `DiscardCard` on the accepted candidate. `DiscardCard` does not independently guess color eligibility, ownership, visibility, or chooser. If no candidate exists, optionality and failure behavior come from the verified effect contract.

Queries must respect context-sensitive information suppression. A color unavailable to the acting rules context because of face orientation or another explicit state cannot satisfy a color predicate merely because presentation or immutable content data can still inspect the underlying definition. Identical authoritative state and query context produce the same ordered candidate set.

Milestone 7.1 implements all three paragraphs above. The query narrows on color, tier, classification, owner, location, played state, and resolution status, treats every omitted predicate as "do not narrow", and orders its answer by instance ID so the result is independent of the order a caller supplied its collections in; an empty result is an ordinary answer. A facedown card satisfies no color, tier, or classification predicate while still answering questions of location and ownership, and revealing restores the reading, so the suppression is about orientation rather than about having been played. It also answers the lowest non-Basic tier over whatever scope its criteria state, because rulebook p.15 asks that question of a hand and milestone 7.4 will ask it of a hero mid-round. `ColorRestrictedDiscard` is the worked example of the composition above, built from this query, the effect frame's own typed pending-choice shape, and the discard mutation; it re-derives the candidate set when the answer arrives rather than trusting the offer.

Milestone 7.1a makes it reachable from an effect definition and gives it its production consumer. The effect frame carries card state as a required `EffectCardState`, and the absence of card state is a named value that throws when read rather than an empty view: a capability handed no cards would otherwise report an empty candidate set, which is indistinguishable from the ordinary legal answer that the chooser holds nothing eligible, and the two mean opposite things to the enclosing node. Every point at which a capability runs or is re-asked supplies it - Skill start, repeated-Skill start, the option-feasibility walk, the choice-answer path, and snapshot restore's candidate re-derivation - so a capability that reads cards answers identically at all five. A choice may be owned by a hero other than the actor: the handler names the exact chooser when its own typed input already identifies them, the pending choice records whose decision it is, and an answer submitted for any other hero is refused as a wrong chooser, distinctly from an invalid choice, with the choice still standing. `if able` is honored at the node rather than by failing the action, and a chooser who was never asked stays distinguishable from one who was asked and declined without a fifth result kind, because only the second leaves a choice in the recorded stream. Candidates are never persisted; they are derived when the choice opens, again when the answer arrives, and again on restore, so a card that left the chooser's hand in between is refused. The first capability built on this is proved on a fixture Skill card, as milestones 6.2, 6.4 and 6.7 proved theirs before any configured card exposed them.

Reusable card-state transitions include:

- `PlayCard`, `RevealCard`, `ResolveCard`, and `ResolveWithoutAction`.
- `DiscardCard` from the source-allowed state, including exactly one in-hand Defense card.
- `RetrieveCard` for the correct owner and allowed states.
- `SwapCardStates` as the rulebook-defined state exchange, not two arbitrary list moves.
- `TurnCardFacedown`/`TurnCardFaceup` with the correct resolved/discarded information suppression and Active-effect cancellation.
- `UpgradeCard`, `SetAsideAsItem`, and `UnlockUltimate` using immutable definitions and explicit instance transitions.

Milestone 8.19b adds the bounded `retrieve-card@1` handler over the existing public lifecycle authority. Its declaration names the owner, the exact chooser, and either any owned clean discard or an owned Basic Skill in its allowed resolved/discarded resting shape. Candidates are canonically ordered and rederived for feasibility, when the answer arrives, and after restore; malformed lifecycle combinations, cross-owner cards, an unknown chooser, or a stale choice mutate nothing. The handler delegates the accepted transition to `CardLifecycleAuthority.TryRetrieveEligibleCard` rather than duplicating card-state mutation.

Card-state effects retain orthogonal location, played, resolution, and orientation facts. Every transition validates owner, source state, and definition revision.

Garrus's resolved-card discard vocabulary (`discard-resolved-card@1`, milestone 8.41a) selects one actor-owned resolved card still in the turn area, revalidates that selection, and moves it to the discard pile while retaining played/resolved history and orientation. Its source Active effects cancel immediately at `SourceCardStateChanged`; continuous Ultimates are unaffected. Both ordinary round retrieval and `retrieve-card@1` admit this played-discard resting state, while unresolved played discards remain invalid. The choice and state round-trip in the existing schema. This contract remains explicitly deferred until Garrus's dealing milestone registers a real consumer; Battle Fury's 8.41c trigger is owned by [timing](TIMING_AND_TRANSITIONS.md#control-flow-and-action-composition).

Milestone 7.1 implements the three resting-place moves rulebook p.15's Upgrade a Card step names - return to deck, take from deck into hand, and set aside facedown - as transitions rather than as a purchase: none of them spends a coin, reads a Level, chooses which card moves, or orders anything, all of which are milestone 7.5's. Each admits an unplayed card in one exact source location, and the instance's owner and definition revision are unchanged by construction because only its state facts move. The general in-hand discard is the same milestone's rename of the Defense-specific one; its behavior never was Defense-specific, and the reaction that calls it decides the card is a legal Defense before it arrives.

Milestone 3.5 deliberately uses `ResolveCard` after the performed Hold action. It does not alias Hold to `ResolveWithoutAction`; the latter remains reserved for the separately timed defeat and declined-respawn paths.

Milestone 3.7 uses the same `ResolveCard` transition only after the selected played card's outer Attack has resumed from Defense and closed. `DiscardCard` for the defender remains an independently validated reaction cost and never resolves or consumes that defender's played turn card.

## Characteristics, immunity, permissions, and restrictions

Milestone 6.8 implements the general contracts these four subjects share. One immutable `RuleModification` declares a value modifier, restriction, permission, immunity, or `Counts As`; `AppliedRuleModification` binds it to the rules object it affects and the live source supplying it; and `RuleModificationSet` answers the questions below in one canonical order. Declarations are definition content on `ActiveEffectDefinition` and `TokenPropertyDefinition`, so a restored effect or token rebinds to the trusted catalog and produces the same modifications again; no snapshot field carries a rule. `RuleModificationQuery` derives the live set from Active-effect instances in their active phase plus the properties of currently placed tokens, and rebuilds it per command rather than caching it. Ordering follows p.17: the revealed Tie-Breaker side first, then the source's creation sequence, then its stable identity, with no flip.

### Counts As

`CountsAsModifier` replaces all applicable rules and qualities of the object with the specified effective type(s) for its duration. Type-based targeting, occupancy, immunity, rewards, Minion-Battle membership, and other rules queries use those effective characteristics. A capability that cites intrinsic miniatures/representation explicitly declares `IntrinsicKindQuery`; for example, the p.18 Battle-Zone correction applies only to minion miniatures and excludes objects that merely count as minions. Under the approved 6.5aa interpretation, a counts-as-minion hero is also a Minion-Battle removal candidate; removal leaves it off board without Defeat/reward/Life consequences and the ordinary p.7 Hero Respawn choice follows at its next action opportunity.

`EffectiveUnitKindQuery` implements the replacement and `IntrinsicKindQuery` the representation, and the second deliberately accepts no modification set at all, so a rule that cites miniatures cannot consult a `Counts As` declaration even by accident. A hero counting as a minion therefore stops satisfying a hero-typed query while remaining a hero for identity, ownership, occupancy kind, supply, and the p.18 correction.

Milestone 8.19c adds the additive form beside the replacement. An **effective-type overlay** declares a type the object counts as *in addition to* whatever it already counts as, which is what a card printing "count as both" says; the replacement is resolved first and the overlay is added to whichever answer stands. `EffectiveUnitKind.Terrain` is the first member only an overlay may supply - the `Counts As` factory refuses it - because p.17 replaces all rules and qualities and p.18's terrain has none a unit could be run by. Intrinsic identity, occupancy, ownership, team and targetability are untouched, and `TerrainPresenceQuery` answers the map's printed terrain and an overlaid object as one question so a card asking "adjacent to terrain" cannot see one source and miss the other.

### Immunity

`ImmunityModifier` declares immune object, scope (action/effect/type/source), duration, and exceptions. Immunity prevents targeting and being affected but does not remove the object from presence checks. A hero is never immune to their own actions.

Corrective milestone 6.5aa completes that split through `ImmunityQuery`: Attack, direct Move, Object Push, Place, and Swap ask the same exact-activity decision both while deriving candidates and immediately before mutation. The query composes declared immunity with the p.13 standing Heavy rule, including effective-minion accompaniment, while presence queries consult no immunity and still count the object. Milestone 8.19b adds `ImmunityOverrideDeclaration` for the exact printed Heavy exception only. It names `DirectMove` or `Swap`, bypasses standing Heavy immunity for that operation alone, and never overrides an independently declared immunity; target, Radius, pair, and final-operation consumers carry the same declaration. An immunity naming one activity leaves every other activity unaffected, and the self exception is answered before any declaration is read. Milestone 8.19c lets one declaration name several activities and the actor classes it protects against, which is one printed sentence rather than four rules. The actor classes are read by the same resolution a restriction's actors get, so a qualification means one thing on either family; the directed question belongs to `ImmunityQuery`, which holds the participants a team relation resolves against, while the set itself answers only unqualified declarations. p.18's fixed self exception still precedes every scope.

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

`RosterValueQueryKind.OwnedDiscardCount` supplies an Attack card-text contribution from every card currently in the acting hero's discard pile, independent of orientation and played state. It is live across discard/retrieval and composes separately from printed and item contributions. This 8.41b checkpoint adds no production card consumer; Garrus's composition and dealing remain pending.

`ModifyValueEffect` references a typed value channel and contribution source, such as Initiative, Attack, Defense, Movement, Range, or Radius. Printed icons, items, card text, minions, and Active effects remain separate contributions.

Milestone 5.3 implements the general minion Defense subset from rulebook page 14. A discarded Defense card keeps its printed-icon contribution, then the value query adds +1 for every adjacent friendly Melee/Heavy minion, -1 for every adjacent enemy Melee/Heavy minion, and -1 for every enemy Ranged minion within radius 2. Every added contribution carries the stable authoritative `MinionId`; decline or no legal Defense card still produces no Defense Total.

Milestone 5.8 makes the future combat source seams executable data rather than labels: printed-icon, minion, item-card, card-text/action, and Active-effect-instance contributions use distinct discriminators and typed source identity. `DefenseBlockResult` is a separate nonnumeric result override; a fulfilled Block never becomes a sentinel Defense amount. Milestone 8.19b populates typed card-text/action Attack and Range contributions from exact history/presence predicates, rederived in the live combat totals and on restore. It also adds a definition-owned all-or-none p.14 minion-Defense contribution policy and the exact incoming-Attack restriction that removes `PrintedPrimary` Defense offers only.

Milestone 3.4 establishes the current synthetic Initiative consumer: the printed card-wide Initiative icon is projected through the same typed contribution/result shape without inventing an action-definition source, and the next unresolved actor query recalculates from current contributions at every boundary. Later Initiative modifiers extend that contribution query rather than adding a cached turn-controller field.

Other typed state changes include gain/spend coins, spend Lives, flip/remove Wave counters, change hero level, and assign/return ownership or control where verified text permits it. Resource effects declare minimums, failure behavior, recipient, source, and emitted contribution events; they never use negative magic numbers as hidden semantics.

Milestone 5.5 implements the pure coin-gain subset for Defeat outcomes. Each outcome carries a stable outcome/target identity, defeated team, acting hero, and already-verified Defeat/Assist amounts. Accounting validates the participant graph and enemy team, awards the acting hero first and every other friendly hero in stable participant order, and emits typed operations containing source, recipient hero/team, Defeat-versus-Assist kind, positive amount, zero minimum, reject-without-mutation behavior, and before/after balances. The entire recipient batch is planned before mutation, duplicate outcomes reject, and a separately typed removed/removal-prevented result cannot reverse the reward. Milestone 7.4 owns exact hero Level/lowest-Tier derivation; spending and presentation remain with their owning milestones.

The configured match derives minion rewards, hero Level and lowest non-Basic Tier, persists balances and awarded-outcome identity, performs Level spending and upgrades, and projects each hero's balance, items, Level, and progression choices.

Milestone 8.19b adds `gain-resource@1` and `transfer-resource@1` to the same `HeroCoinLedger`. A positive card-effect grant validates the recipient and overflow before committing. Transfer names source, recipient, exact chooser, and a bounded positive amount choice; feasibility and resumption rederive balances and admitted amounts, and `TryTransfer` plans the debit and credit together before either is written. Unknown heroes, self-transfer, insufficient funds, overflow, forged/stale amounts, and wrong-chooser answers leave both balances unchanged. The live ledger already persists, so no snapshot field is added.

Milestone 8.19c gives a source-declared value modifier two things it lacked. It may be **counted from live state** - an amount per match of a named derived query, resolved wherever the live set is derived and contributing nothing at all at a count of zero - and it may be **qualified to the action class it names**, so a declaration reaches only the total that action composes. Each of p.9's remaining channels now has exactly one owning query that the live resolution and its re-derivations both ask: `AttackValueQuery` and `MovementValueQuery` are promoted beside the existing Range, Radius and Defense totals, and the Initiative total is composed where the turn order reads it.

## Contract implementation status

See [CORE_MODEL.md](CORE_MODEL.md#contract-implementation-status) for what this section is and how to maintain it.

| Contract | State | Owning milestone |
| --- | --- | --- |

No row remains. `CardCharacteristicQuery` held the only one until milestone 7.1 implemented it, which is what this section's own rule asks for: a row is removed when its type is declared, not kept as a record of when it was not.
