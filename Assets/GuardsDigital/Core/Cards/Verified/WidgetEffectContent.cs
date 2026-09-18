using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Placement;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Source-backed Widget clauses; these factories do not deal cards or register handlers.</summary>
    public static class WidgetEffectContent
    {
        public static EffectDefinition DragonBond(CardDefinitionId card, string revision)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Choose("root", "place-pyro", "move-both"),
                new EffectNodeDefinition(Id("place-pyro"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    WidgetComponentContent.PyroPresentV1, childNodeIds: new[] { Id("relocate"), Id("from-supply") },
                    outputs: new[] { Output("present", EffectValueKind.Boolean) }),
                Sequence("relocate", "placed-pyro", "place-existing"),
                Pyro("placed-pyro"),
                new EffectNodeDefinition(Id("place-existing"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticPlacementCapabilities.PlaceInRadiusV1, inputs: new[]
                    {
                        From("subject", "placed-pyro", "space"),
                        EffectInputBinding.FromClosedVocabulary(SyntheticPlacementCapabilities.EligiblePieceParameterName, PlaceEligiblePieceKind.Token),
                        EffectInputBinding.FromClosedVocabulary(SyntheticPlacementCapabilities.DestinationAnchorParameterName, PlaceDestinationAnchor.ActingHero),
                        EffectInputBinding.FromClosedVocabulary(SyntheticPlacementCapabilities.SpawnPointExclusionParameterName, PlaceSpawnPointExclusion.None)
                    }, outputs: new[] { Output("placed", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(Id("from-supply"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    WidgetComponentContent.PlacePyroV1, outputs: new[] { Output("placed", EffectValueKind.Coordinate) }),
                Sequence("move-both", "require-pyro", "order"),
                Pyro("require-pyro"),
                Choose("order", "hero-first", "pyro-first"),
                Sequence("hero-first", "hero-a", "move-hero-a", "pyro-a", "move-pyro-a"),
                Actor("hero-a"), Move("move-hero-a", "hero-a", 2),
                Pyro("pyro-a"), Move("move-pyro-a", "pyro-a", 2),
                Sequence("pyro-first", "pyro-b", "move-pyro-b", "hero-b", "move-hero-b"),
                Pyro("pyro-b"), Move("move-pyro-b", "pyro-b", 2),
                Actor("hero-b"), Move("move-hero-b", "hero-b", 2)
            };
            return Definition(card, revision, "skill", nodes);
        }

        public static EffectDefinition MovePyroAfterAttack(CardDefinitionId card, string revision, int distance) =>
            Definition(card, revision, "after-attack", new[]
            {
                Sequence("root", "pyro", "move-pyro"), Pyro("pyro"), Move("move-pyro", "pyro", distance)
            });

        public static EffectDefinition SwapWithPyro(CardDefinitionId card, string revision, string timing) =>
            Definition(card, revision, timing, new[]
            {
                May("root", "exchange"), Sequence("exchange", "pyro", "swap"),
                Pyro("pyro", WidgetComponentContent.PyroInRadiusV1), SwapActor("swap", "pyro")
            });

        public static EffectDefinition AllAboard(CardDefinitionId card, string revision, bool safeLanding)
        {
            var nodes = new List<EffectNodeDefinition>
            {
                Sequence("root", safeLanding ? new[] { "may-move", "pyro", "partner" } : new[] { "pyro", "partner" }),
                // Safe Landing intentionally reads Pyro's live range again after its optional move.
                Pyro("pyro", WidgetComponentContent.PyroInRangeV1),
                Choose("partner", "self", "friendly"),
                SwapActor("self", "pyro"),
                Sequence("friendly", "hero", "swap-friend"),
                new EffectNodeDefinition(Id("hero"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RangeHeroTargetV1, inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary("relation", SourceTeamRelation.AlliedWithSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                        EffectInputBinding.FromLiteral(SyntheticTargetingCapabilities.ExcludeTheAttackerParameterName, EffectValue.Boolean(false)),
                        EffectInputBinding.FromLiteral(EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(Id("swap-friend")))
                    }, outputs: new[] { Output("chosenHero", EffectValueKind.Hero), Output("chosenHeroSpace", EffectValueKind.Coordinate) }),
                new EffectNodeDefinition(Id("swap-friend"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticExchangeCapabilities.SwapV2, inputs: new[]
                    {
                        From("first", "pyro", "space"), From("second", "hero", "chosenHeroSpace"),
                        EffectInputBinding.FromClosedVocabulary(SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.GamePiece)
                    }, outputs: new[] { Output("firstAt", EffectValueKind.Coordinate), Output("secondAt", EffectValueKind.Coordinate) })
            };
            if (safeLanding)
            {
                nodes.Add(May("may-move", "advance"));
                nodes.Add(Sequence("advance", "pyro-before", "move-pyro"));
                nodes.Add(Pyro("pyro-before", WidgetComponentContent.PyroInRangeV1));
                nodes.Add(Move("move-pyro", "pyro-before", 1, EffectFailurePolicy.Mandatory));
            }
            return Definition(card, revision, "skill", nodes);
        }

        private static EffectDefinition Definition(CardDefinitionId card, string revision, string suffix,
            IEnumerable<EffectNodeDefinition> nodes) => new EffectDefinition(
                new EffectDefinitionId(card.Value + "-" + suffix), revision, Id("root"), nodes);
        private static EffectNodeDefinition Sequence(string id, params string[] children) => new EffectNodeDefinition(
            Id(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory, childNodeIds: System.Array.ConvertAll(children, Id));
        private static EffectNodeDefinition Choose(string id, params string[] children) => new EffectNodeDefinition(
            Id(id), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory, childNodeIds: System.Array.ConvertAll(children, Id),
            chooser: EffectChooserRole.Actor, minimumSelections: 1, maximumSelections: 1);
        private static EffectNodeDefinition May(string id, string child) => new EffectNodeDefinition(
            Id(id), EffectNodeKind.Optional, EffectFailurePolicy.May, childNodeIds: new[] { Id(child) }, chooser: EffectChooserRole.Actor);
        private static EffectNodeDefinition Pyro(string id, EffectCapabilityReference reference = default) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                reference.Equals(default(EffectCapabilityReference)) ? WidgetComponentContent.PyroSpaceV1 : reference,
                outputs: new[] { Output("space", EffectValueKind.Coordinate) });
        private static EffectNodeDefinition Actor(string id) => new EffectNodeDefinition(Id(id), EffectNodeKind.Capability,
            EffectFailurePolicy.Mandatory, SyntheticPlacementCapabilities.ActorSpaceV1,
            outputs: new[] { Output("space", EffectValueKind.Coordinate) });
        private static EffectNodeDefinition Move(string id, string from, int distance, EffectFailurePolicy policy = EffectFailurePolicy.UpTo) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Capability, policy, SyntheticDisplacementCapabilities.DirectMoveV3,
                inputs: new[]
                {
                    From("mover", from, "space"), EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                    EffectInputBinding.FromClosedVocabulary(SyntheticDisplacementCapabilities.DestinationPolicyParameterName, DirectMoveDestinationPolicy.Unrestricted)
                }, outputs: new[] { Output("moved", EffectValueKind.Coordinate) });
        private static EffectNodeDefinition SwapActor(string id, string pyro) => new EffectNodeDefinition(Id(id), EffectNodeKind.Capability,
            EffectFailurePolicy.Mandatory, SyntheticExchangeCapabilities.SwapWithActorV1,
            inputs: new[]
            {
                From("subject", pyro, "space"),
                EffectInputBinding.FromClosedVocabulary(SyntheticExchangeCapabilities.EligiblePieceParameterName, SwapEligiblePieceKind.GamePiece)
            });
        private static EffectInputBinding From(string name, string node, string output) =>
            EffectInputBinding.FromNodeOutput(name, Id(node), output, EffectValueKind.Coordinate);
        private static EffectOutputBinding Output(string name, EffectValueKind kind) => new EffectOutputBinding(name, kind);
        private static EffectNodeId Id(string id) => new EffectNodeId(id);
    }
}
