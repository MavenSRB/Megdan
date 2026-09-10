using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.35z32. The two capabilities a hero's own token line needs, and the first token
    /// handlers in the engine bound to <em>verified</em> content rather than to milestone 6.2's
    /// synthetic hazard.
    ///
    /// <para><b>Why they are new capabilities and not parameters on the synthetic pair.</b> Milestone
    /// 6.2's <c>synthetic-place-token@1</c> and <c>synthetic-reclaim-token@1</c> hardcode one token
    /// definition and a radius of 1 measured from the actor, and both of those are exactly what a
    /// printed card decides: Glacial Barrier places Ice "in radius" and its Radius icon says 3, while
    /// Ancestral Totem places a Totem "in range" at 2. Changing the synthetics would have moved a
    /// registered capability every existing snapshot names.</para>
    ///
    /// <para><b>One handler per token definition, which is Tigerclaw's marker pattern reused.</b> The
    /// definition is bound to the handler rather than passed on the wire, for the reason milestone 8.23
    /// gives: a payload can then name a node but never a different token, and the registered list a
    /// snapshot pins says exactly which tokens this build can put on the board. It also avoids widening
    /// <see cref="EffectValue"/> with a token-definition arm that only content would ever supply.</para>
    ///
    /// <para><b>Registered at milestone 8.37</b>, the milestone that deals Tali. Her own six
    /// instances are content of hers and register from <c>TaliEffectContent</c>; what registers HERE
    /// is the pair of showcase instances milestone 7.9's gate asks for, because both classes are
    /// handler TYPES this build had never registered and the gate refuses a type that arrives for the
    /// first time inside a verified hero's slice. They are bound to milestone 6.2's own synthetic
    /// hazard token rather than to a new definition, so the showcase proves the TYPE without adding a
    /// component to the registered set that no card places.</para>
    /// </summary>
    public static class SyntheticTokenCapabilities
    {
        /// <summary>
        /// The space a placement put its token on, published so a following node can read it - the
        /// same output name milestone 6.2's synthetic placement publishes, because it is the same fact.
        /// </summary>
        public const string PlacedOutput = "placed";

        /// <summary>The space a p.18 short-supply reclamation took a token back from.</summary>
        public const string ReclaimedOutput = "reclaimed";

        /// <summary>
        /// Milestone 8.37's showcase instances of both handler types, over milestone 6.2's synthetic
        /// hazard. The reach channel is Radius because that is the channel the showcase Skill that
        /// names them prints, and the reclaim's supply group is the hazard's own.
        /// </summary>
        public static EffectCapabilityId ShowcasePlaceHazard { get; } =
            new EffectCapabilityId("showcase-place-hero-token");

        public static EffectCapabilityId ShowcaseReclaimHazard { get; } =
            new EffectCapabilityId("showcase-reclaim-hero-token");

        public static EffectCapabilityReference ShowcasePlaceHazardV1 =>
            new EffectCapabilityReference(ShowcasePlaceHazard, 1);

        public static EffectCapabilityReference ShowcaseReclaimHazardV1 =>
            new EffectCapabilityReference(ShowcaseReclaimHazard, 1);

        /// <summary>See the type remarks: the two showcase instances milestone 7.9's gate asks for.</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            new[]
            {
                PlaceHandler(
                    ShowcasePlaceHazardV1,
                    Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition,
                    PrintedValueChannel.Radius),
                ReclaimHandler(
                    ShowcaseReclaimHazardV1,
                    Components.Synthetic.SyntheticComponentDefinitions.HazardDefinition)
            };

        /// <summary>
        /// One placement handler for one token definition, reaching the distance that definition's
        /// placing card prints on the named channel.
        /// </summary>
        public static IEffectCapabilityHandler PlaceHandler(
            EffectCapabilityReference reference,
            TokenDefinition definition,
            PrintedValueChannel reachChannel) =>
            new PlaceHeroTokenHandler(reference, definition, reachChannel);

        /// <summary>
        /// One p.18 short-supply handler for one token definition. It offers back every token in that
        /// definition's <em>supply group</em>, which is the sentence's "tokens of the same type" read
        /// as RULING 44 reads it for a hero whose roles share one physical pile.
        /// </summary>
        public static IEffectCapabilityHandler ReclaimHandler(
            EffectCapabilityReference reference, TokenDefinition definition) =>
            new ReclaimHeroTokenHandler(reference, definition);

        /// <summary>
        /// Places one token from the supply into an empty playable space within the placing card's
        /// printed reach of the acting hero (rulebook p.18's Place).
        ///
        /// <para>The destination bound is the card's own printed icon composed with p.16's items, read
        /// through the same value queries every printed-icon selection reads - so a +1 Radius item
        /// makes Pack Ice reach 4 without this handler knowing an item exists. p.9 is what licenses
        /// that: "the value on the Range/Radius icon determines the maximum Range/Radius of your
        /// action. If the card text mentions Range/Radius, it refers to this value" - and both of her
        /// token families' texts do say "in radius" and "in range" respectively.</para>
        ///
        /// <para>It places <b>one</b> token. "Place up to 2" and "up to 3" are the composition's job -
        /// two and three optional nodes - for the same reason milestone 6.2 split p.18's short-supply
        /// step into its own node: a handler is stateless and cannot tell a second resumption from a
        /// first, so a second decision belongs to a second node.</para>
        /// </summary>
        private sealed class PlaceHeroTokenHandler : IEffectCapabilityHandler
        {
            private readonly TokenDefinition definition;
            private readonly PrintedValueChannel reachChannel;

            public PlaceHeroTokenHandler(
                EffectCapabilityReference reference,
                TokenDefinition definition,
                PrintedValueChannel reachChannel)
            {
                if (reachChannel != PrintedValueChannel.Radius && reachChannel != PrintedValueChannel.Range)
                {
                    throw new ArgumentException(
                        "A token placement reaches a printed Radius or a printed Range; p.9 gives a " +
                        "card no third reach.",
                        nameof(reachChannel));
                }

                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
                this.reachChannel = reachChannel;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(PlacedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (components.Catalog.CountAvailableTokens(
                        definition.Reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var candidates = LegalSpaces(context, components).Select(EffectValue.Space).ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (!components.Tokens.TryGetHeroCoordinate(context.ActorId, out var origin))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var coordinate = selected[0].Coordinate;

                // The operation re-derives the admitted spaces and re-asks the supply rather than
                // trusting the paused choice: a space legal when the choice opened may have been taken
                // before the answer arrived, and the last copy of the shared pile may have been placed
                // elsewhere in the meantime.
                var outcome = PlaceOperation.PlaceFromSupply(
                    Policy(context, components),
                    components.Tokens,
                    components.Catalog,
                    new PlacedToken(
                        // Deterministic under replay and unique across placements: the round and turn
                        // distinguish a token from a later one that happens to occupy the space the
                        // first was reclaimed from, and the definition distinguishes two roles of the
                        // one pile placed on the same space in the same turn.
                        new TokenInstanceId(
                            $"{context.SourceCardInstanceId.Value}:{context.NodeId.Value}" +
                            $":{definition.Id.Value}:r{context.RoundIndex}t{context.TurnIndex}" +
                            $":{coordinate.Q}:{coordinate.R}"),
                        definition,
                        context.ActorId,
                        context.ControllerId,
                        context.SourceCardInstanceId,
                        coordinate),
                    origin,
                    Array.Empty<HexCoordinate>(),
                    context.Modifications);
                if (!outcome.Accepted)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(PlacedOutput, EffectValue.Space(outcome.Destination))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                if (components.Catalog.CountAvailableTokens(
                        definition.Reference, components.Tokens.PlacedTokens) < 1)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return LegalSpaces(context, components).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// This node's Place, built from the card rather than held as a constant: the four fixed
            /// declarations are the ones p.18 fixes for every supply placement, and the reach is the
            /// one thing the printed face decides.
            /// </summary>
            private PlacePolicy Policy(EffectCapabilityContext context, MatchComponents components) =>
                new PlacePolicy(
                    PlaceSourceContainer.Supply,
                    PlaceEligiblePieceKind.Token,
                    PlaceDestinationScope.EmptySpacesWithinRadius,
                    ResolveReach(context),
                    PlaceChooserKind.ActingHero,
                    PlaceNoDestinationPolicy.Impossible,
                    // The piece comes from the supply and stands nowhere, so the only space the reach
                    // can be measured from is the acting hero's - which is also what both of her token
                    // families' texts mean by "in radius" and "in range".
                    PlaceDestinationAnchor.ActingHero);

            private int ResolveReach(EffectCapabilityContext context) =>
                (reachChannel == PrintedValueChannel.Radius
                    ? RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems)
                    : RangeValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications,
                        ActionValueApplicabilityQuery.For(
                            context.SourceDefinitionReference,
                            context.ActionDefinitionId,
                            context.CardState.Definitions))).Total;

            private IEnumerable<HexCoordinate> LegalSpaces(
                EffectCapabilityContext context, MatchComponents components) =>
                components.Tokens.TryGetHeroCoordinate(context.ActorId, out var origin)
                    ? PlaceDestinationQuery.FindDestinations(
                        Policy(context, components), components.Tokens, origin,
                        Array.Empty<HexCoordinate>(), context.Modifications)
                    : Array.Empty<HexCoordinate>();
        }

        /// <summary>
        /// Rulebook p.18's short-supply step for a hero-owned token, and only that step: "If you need
        /// to place more tokens than are available in the supply, first remove up to that number of
        /// tokens of the same type from the board, if able."
        ///
        /// <para><b>The one thing it does differently from milestone 6.2's synthetic twin</b> is what
        /// "of the same type" reaches. For a token with its own supply it is that definition's tokens,
        /// unchanged. For Tali it is her whole supply group, because RULING 44 says her three roles are
        /// one token - so a Pack Ice needing a third copy may take back a standing totem, and a Totem
        /// placement may take back standing Ice. That is not a liberty taken here: it is the mechanical
        /// consequence of one shared type, and the ruling states it as such.</para>
        ///
        /// <para>It declines rather than fails when the supply can already cover the placement, because
        /// a legal "nothing needed to happen" is <see cref="EffectExecutionResultKind.DeclinedOptional"/>
        /// and not a refusal; and it reports impossible when the pile is empty and nothing of the type
        /// stands on the board, which under the <c>if able</c> policy its node declares means the step
        /// is simply skipped.</para>
        /// </summary>
        private sealed class ReclaimHeroTokenHandler : IEffectCapabilityHandler
        {
            private readonly TokenDefinition definition;

            public ReclaimHeroTokenHandler(EffectCapabilityReference reference, TokenDefinition definition)
            {
                Reference = reference;
                this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(ReclaimedOutput, EffectValueKind.Coordinate) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                if (components.Catalog.CountAvailableTokens(
                        definition.Reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResult.DeclinedOptional();
                }

                var candidates = SameTypeTokens(components)
                    .Select(token => EffectValue.Space(token.Coordinate))
                    .ToArray();
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var components = context.Components;
                if (components == null || !components.IsLive || selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var coordinate = selected[0].Coordinate;
                var token = SameTypeTokens(components)
                    .FirstOrDefault(candidate => candidate.Coordinate.Equals(coordinate));
                if (token == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var reclaimed = components.Tokens.CommitValidatedTokenReclamation(token.Id);
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ReclaimedOutput, EffectValue.Space(reclaimed.Coordinate))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.IsLive)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                if (components.Catalog.CountAvailableTokens(
                        definition.Reference, components.Tokens.PlacedTokens) > 0)
                {
                    return EffectExecutionResultKind.DeclinedOptional;
                }

                return SameTypeTokens(components).Any()
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;
            }

            /// <summary>
            /// p.18's "tokens of the same type", in a stable board order so the offer a player sees is
            /// a fact rather than an accident of collection order.
            /// </summary>
            private IEnumerable<PlacedToken> SameTypeTokens(MatchComponents components) =>
                components.Catalog
                    .ReclaimableForPlacement(definition.Reference, components.Tokens.PlacedTokens)
                    .OrderBy(token => token.Coordinate.Q)
                    .ThenBy(token => token.Coordinate.R);
        }
    }
}
