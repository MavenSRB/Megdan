using System;
using System.Collections.Generic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>One friendly Radius target, followed by the clauses of its active runes.</summary>
    public static class SnorriBlueSupportEffectContent
    {
        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        public static EffectDefinition ForBoon(string revision) =>
            For(SnorriBlueMovementFaces.AncestralBoon, revision);

        public static EffectDefinition For(CardDefinitionId card, string revision)
        {
            var grace = card.Equals(SnorriBlueMovementFaces.AncestralGrace);
            if (!grace && !card.Equals(SnorriBlueMovementFaces.AncestralBoon))
                throw new ArgumentException("Not an Ancestral Blue Skill.", nameof(card));
            var root = Id("root");
            var any = Id("any-support-rune");
            var body = Id("support-body");
            var friend = Id("friendly-hero");
            var swapGate = Id("rune-two-swap-gate");
            var swapMay = Id("friend-may-swap");
            var swapBody = Id("swap-body");
            var resolved = Id("choose-resolved");
            var swap = Id("swap-with-hand");
            var retrieveGate = Id("rune-three-retrieve-gate");
            var retrieveMay = Id("friend-may-retrieve");
            var retrieve = Id("retrieve-all-discarded");
            var itemGate = Id("rune-one-item-gate");
            var itemMay = Id("friend-may-swap-item");
            var itemBody = Id("item-swap-body");
            var item = Id("choose-item");
            var itemSwap = Id("swap-item-with-card");
            var nodes = new List<EffectNodeDefinition>
            {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { any }),
                    new EffectNodeDefinition(
                        any, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SnorriComponentContent.AnySupportRuneV1,
                        childNodeIds: new[] { body },
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "mask", grace ? SnorriRuneMask.Runes123 : SnorriRuneMask.Runes23)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("active", EffectValueKind.Boolean)
                        }),
                    new EffectNodeDefinition(
                        body, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: grace
                            ? new[] { friend, swapGate, retrieveGate, itemGate }
                            : new[] { friend, swapGate, retrieveGate }),
                    new EffectNodeDefinition(
                        friend, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromClosedVocabulary(
                                "activity", RuleActivity.Skill),
                            EffectInputBinding.FromClosedVocabulary(
                                SyntheticTargetingCapabilities.TargetRelationParameterName,
                                SourceTeamRelation.AlliedWithSource),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.IncludeSelfParameterName,
                                EffectValue.Boolean(false)),
                            EffectInputBinding.FromLiteral(
                                SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                EffectValue.Boolean(false))
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                                EffectValueKind.Hero)
                        }),
                    RuneGate(swapGate, 2, swapMay),
                    new EffectNodeDefinition(
                        swapMay, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { swapBody },
                        inputs: new[] { FriendInput(friend) },
                        chooser: EffectChooserRole.BoundHero),
                    new EffectNodeDefinition(
                        swapBody, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { resolved, swap }),
                    new EffectNodeDefinition(
                        resolved, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SnorriBlueSupportCapabilities.ChooseResolvedCardV1,
                        inputs: new[] { OwnerInput(friend) },
                        outputs: new[]
                        {
                            new EffectOutputBinding("resolved", EffectValueKind.CardInstance)
                        }),
                    new EffectNodeDefinition(
                        swap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SnorriBlueSupportCapabilities.SwapResolvedWithHandV1,
                        inputs: new[]
                        {
                            OwnerInput(friend),
                            EffectInputBinding.FromNodeOutput(
                                "resolved", resolved, "resolved", EffectValueKind.CardInstance)
                        },
                        outputs: new[]
                        {
                            new EffectOutputBinding("replacement", EffectValueKind.CardInstance)
                        }),
                    RuneGate(retrieveGate, 3, retrieveMay),
                    new EffectNodeDefinition(
                        retrieveMay, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { retrieve },
                        inputs: new[] { FriendInput(friend) },
                        chooser: EffectChooserRole.BoundHero),
                    new EffectNodeDefinition(
                        retrieve, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SnorriBlueSupportCapabilities.RetrieveAllDiscardedV1,
                        inputs: new[] { OwnerInput(friend) },
                        outputs: new[]
                        {
                            new EffectOutputBinding("retrievedCount", EffectValueKind.Integer)
                        })
            };
            if (grace)
            {
                nodes.Add(RuneGate(itemGate, 1, itemMay));
                nodes.Add(new EffectNodeDefinition(
                    itemMay, EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { itemBody },
                    inputs: new[] { FriendInput(friend) },
                    chooser: EffectChooserRole.BoundHero));
                nodes.Add(new EffectNodeDefinition(
                    itemBody, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { item, itemSwap }));
                nodes.Add(new EffectNodeDefinition(
                    item, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SnorriBlueSupportCapabilities.ChooseOwnedItemV1,
                    inputs: new[] { OwnerInput(friend) },
                    outputs: new[]
                    {
                        new EffectOutputBinding("item", EffectValueKind.CardInstance)
                    }));
                nodes.Add(new EffectNodeDefinition(
                    itemSwap, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SnorriBlueSupportCapabilities.SwapItemWithCardV1,
                    inputs: new[]
                    {
                        OwnerInput(friend),
                        EffectInputBinding.FromNodeOutput(
                            "item", item, "item", EffectValueKind.CardInstance)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("replacement", EffectValueKind.CardInstance)
                    }));
            }
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill-text"),
                revision, root, nodes);
        }

        private static EffectNodeDefinition RuneGate(
            EffectNodeId id, int ordinal, EffectNodeId child) =>
            new EffectNodeDefinition(
                id, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                SnorriComponentContent.ActiveRuneV1,
                childNodeIds: new[] { child },
                inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary("rune", (SnorriRuneGlyph)ordinal)
                },
                outputs: new[]
                {
                    new EffectOutputBinding("active", EffectValueKind.Boolean)
                });

        private static EffectInputBinding OwnerInput(EffectNodeId friend) =>
            EffectInputBinding.FromNodeOutput(
                "owner", friend, SyntheticTargetingCapabilities.ChosenHeroOutputName,
                EffectValueKind.Hero);

        private static EffectInputBinding FriendInput(EffectNodeId friend) =>
            EffectInputBinding.FromNodeOutput(
                EffectNodeDefinition.ChooserParameterName, friend,
                SyntheticTargetingCapabilities.ChosenHeroOutputName,
                EffectValueKind.Hero);
    }
}
