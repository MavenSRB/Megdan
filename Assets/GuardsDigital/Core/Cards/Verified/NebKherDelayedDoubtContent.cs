using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// A chosen color is represented by which typed, dormant effect is created. No color is
    /// hidden in text or an unversioned instance payload; the effect's referenced body carries
    /// the exact chosen color into the next reveal checkpoint.
    /// </summary>
    public static class NebKherDelayedDoubtContent
    {
        public static readonly CardColor[] NameableColors =
        {
            CardColor.Gold, CardColor.Silver, CardColor.Red,
            CardColor.Green, CardColor.Blue, CardColor.Purple
        };

        private static readonly CardDefinitionId[] Cards =
        {
            NebKherCardContent.ImbueDoubt,
            NebKherCardContent.TimeToReconsider,
            NebKherCardContent.AnIllusionOfChoice
        };

        private static string Key(CardDefinitionId card, CardColor color) =>
            card.Value + "-" + color.ToString().ToLowerInvariant();

        public static EffectCapabilityReference CreateReference(
            CardDefinitionId card, CardColor color) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(Key(card, color) + "-defer"), 1);

        public static EffectDefinition TriggeredBody(CardDefinitionId card, CardColor color)
        {
            var prefix = Key(card, color) + "-reveal";
            EffectNodeId N(string suffix) => new EffectNodeId(prefix + "-" + suffix);
            var root = N("root");
            var nodes = new List<EffectNodeDefinition>();
            var two = card.Equals(NebKherCardContent.AnIllusionOfChoice);
            var passes = two ? 2 : 1;
            // W3: "up to two enemy heroes" is not permission to decline. Nest pass two
            // after pass one's target so its exclusion reads a guaranteed output.
            for (var index = 1; index <= passes; index++)
            {
                var optional = N("optional-" + index);
                var body = N("body-" + index);
                var target = N("target-" + index);
                var discard = N("discard-" + index);
                if (two && index == 2)
                {
                    nodes.Add(new EffectNodeDefinition(optional, EffectNodeKind.Optional,
                        EffectFailurePolicy.IfAble, childNodeIds: new[] { body }));
                }
                nodes.Add(new EffectNodeDefinition(body, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory,
                    childNodeIds: two && index == 1
                        ? new[] { target, discard, N("optional-2") }
                        : new[] { target, discard }));
                var targetInputs = new List<EffectInputBinding>
                {
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                    EffectInputBinding.FromClosedVocabulary(
                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                        SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(
                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                        EffectValue.Boolean(false)),
                    EffectInputBinding.FromLiteral(
                        EffectSelectionDeclaration.ConsumerParameterName,
                        EffectValue.Node(discard))
                };
                if (index == 2)
                    targetInputs.Add(EffectInputBinding.FromNodeOutput(
                        SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                        N("target-1"),
                        SyntheticTargetingCapabilities.ChosenHeroOutputName,
                        EffectValueKind.Hero));
                nodes.Add(new EffectNodeDefinition(target, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                    inputs: targetInputs,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero)
                    }));
                nodes.Add(new EffectNodeDefinition(discard, EffectNodeKind.Capability,
                    EffectFailurePolicy.IfAble,
                    SyntheticCardStateCapabilities.DiscardChosenCardV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter,
                            target,
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticCardStateCapabilities.ColorParameter, color)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance)
                    }));
            }
            if (two)
            {
                nodes.Insert(0, new EffectNodeDefinition(root, EffectNodeKind.Optional,
                    EffectFailurePolicy.IfAble, childNodeIds: new[] { N("body-1") }));
            }
            else
                nodes.Insert(0, new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { N("body-1") }));
            return new EffectDefinition(new EffectDefinitionId(prefix),
                NebKherCardContent.ContentRevision, root, nodes);
        }

        public static ActiveEffectDefinition DelayedEffect(CardDefinitionId card, CardColor color) =>
            new ActiveEffectDefinition(
                Key(card, color) + "-active",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.ControllerHeroDefeated },
                true, "DelayedColorDiscard", Key(card, color) + "-delayed",
                new[] { TimingCheckpoint.CardsPlayedAndRevealed },
                triggeredEffect: TriggeredBody(card, color).Reference);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            Cards.SelectMany(card => NameableColors.Select(color =>
                (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                    CreateReference(card, color), DelayedEffect(card, color))));

        public static IEnumerable<EffectDefinition> TriggeredEffects() =>
            Cards.SelectMany(card => NameableColors.Select(color =>
                TriggeredBody(card, color)));

        public static EffectDefinition ChooseColor(CardDefinitionId card)
        {
            var root = new EffectNodeId(card.Value + "-color");
            var branches = NameableColors.Select(color =>
                new EffectNodeId(Key(card, color) + "-create")).ToArray();
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                NebKherCardContent.ContentRevision, root,
                new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Choose,
                        EffectFailurePolicy.Mandatory, childNodeIds: branches,
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1, maximumSelections: 1)
                }.Concat(NameableColors.Select((color, index) =>
                    new EffectNodeDefinition(branches[index], EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, CreateReference(card, color),
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName,
                                EffectValueKind.Hero)
                        }))));
        }
    }
}
