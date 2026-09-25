using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Passage clauses apply before the Movement they modify.</summary>
    public static class SnorriBlueMovementEffectContent
    {
        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        public static EffectDefinition For(CardDefinitionId card, string revision)
        {
            var hidden = card.Equals(SnorriBlueMovementFaces.HiddenPassage);
            var deep = card.Equals(SnorriBlueMovementFaces.DeepPassage);
            if (!card.Equals(SnorriBlueMovementFaces.SafePassage) && !hidden && !deep)
                throw new ArgumentException("Not a Passage Movement face.", nameof(card));

            var children = new List<EffectNodeId> { Id("rune-one-obstacle-gate") };
            var nodes = new List<EffectNodeDefinition>
            {
                Gate("rune-one-obstacle-gate", 1, "ignore-obstacles"),
                Create("ignore-obstacles",
                    SnorriBlueActiveEffectContent.ObstaclePermissionFor(card))
            };
            if (hidden || deep)
            {
                children.Add(Id("rune-three-immunity-gate"));
                nodes.Add(Gate("rune-three-immunity-gate", 3, "enemy-immunity"));
                nodes.Add(Create("enemy-immunity",
                    SnorriBlueActiveEffectContent.EnemyImmunityFor(card)));
            }
            if (deep)
            {
                children.Add(Id("rune-four-movement-gate"));
                nodes.Add(Gate("rune-four-movement-gate", 4, "gain-two-movement"));
                nodes.Add(Create("gain-two-movement",
                    SnorriBlueActiveEffectContent.MovementRaiseFor(card)));
            }
            nodes.Insert(0, new EffectNodeDefinition(
                Id("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children));
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-movement-text"),
                revision, Id("root"), nodes);
        }

        private static EffectNodeDefinition Gate(string id, int rune, string child) =>
            new EffectNodeDefinition(
                Id(id), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SnorriComponentContent.ActiveRuneV1,
                childNodeIds: new[] { Id(child) },
                inputs: new[]
                {
                    EffectInputBinding.FromLiteral("rune", EffectValue.Integer(rune))
                },
                outputs: new[]
                {
                    new EffectOutputBinding("active", EffectValueKind.Boolean)
                });

        private static EffectNodeDefinition Create(
            string id, EffectCapabilityReference capability) =>
            new EffectNodeDefinition(
                Id(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                capability,
                outputs: new[]
                {
                    new EffectOutputBinding(
                        ActiveEffectCreationHandler.AffectedOutputName,
                        EffectValueKind.Hero)
                });
    }
}
