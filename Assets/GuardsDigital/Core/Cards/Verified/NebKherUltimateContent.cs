using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Typed, immediate result of one chosen laugh; one pass for every enemy hero in radius.</summary>
    public static class NebKherUltimateContent
    {
        private const int MaximumEnemyHeroes = 6;
        public const string RuleId = "nebkher-what-the-hell-laughter";
        public const string Text =
            "Each time you laugh diabolically as part of performing an action, all enemy " +
            "heroes in radius discard a card, or are defeated.";

        public static EffectCapabilityReference EnemiesInRadiusV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("nebkher-all-enemies-in-radius"), 1);

        private static string HeroOutput(int slot) => "hero" + slot;
        private static string HasHeroOutput(int slot) => "hasHero" + slot;

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new[] { (IEffectCapabilityHandler)new EnemiesInRadiusHandler() };

        public static EffectDefinition TriggeredEffect { get; } = BuildTriggeredEffect();

        public static CardDefinition Definition { get; } =
            new CardDefinition(NebKherCardContent.WhatTheHellAreYou,
                NebKherCardContent.ContentRevision, "What the Hell Are You?",
                default, CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.UltimateCard(),
                Array.Empty<ActionDefinition>(),
                UltimateRuleDefinition.TriggeredAbility(
                    RuleId, "NebKherLaughter", TriggeredEffect.Reference,
                    UltimateTriggerCondition.AfterControllerLaughsDiabolically,
                    new[] { TimingCheckpoint.LaughedDiabolically },
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 5) }),
                CardSourceContent.Transcribed(Text));

        private static EffectDefinition BuildTriggeredEffect()
        {
            EffectNodeId N(string suffix) => new EffectNodeId(
                NebKherCardContent.WhatTheHellAreYou.Value + "-" + suffix);
            var root = N("root");
            var heroes = N("all-enemies");
            var nodes = new List<EffectNodeDefinition>();
            var children = new List<EffectNodeId> { heroes };
            var outputs = Enumerable.Range(1, MaximumEnemyHeroes).SelectMany(slot =>
                new[]
                {
                    new EffectOutputBinding(HeroOutput(slot), EffectValueKind.Hero),
                    new EffectOutputBinding(HasHeroOutput(slot), EffectValueKind.Boolean)
                }).ToArray();
            nodes.Add(new EffectNodeDefinition(heroes, EffectNodeKind.Capability,
                EffectFailurePolicy.Mandatory, EnemiesInRadiusV1, outputs: outputs));
            for (var slot = 1; slot <= MaximumEnemyHeroes; slot++)
            {
                var gate = N("hero-" + slot);
                var body = N("hero-" + slot + "-body");
                var discard = N("hero-" + slot + "-discard");
                var unable = N("hero-" + slot + "-unable");
                var defeat = N("hero-" + slot + "-defeat");
                children.Add(gate);
                nodes.Add(new EffectNodeDefinition(gate, EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { body },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("flag", heroes,
                            HasHeroOutput(slot), EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(body, EffectNodeKind.Sequence,
                    EffectFailurePolicy.Mandatory, childNodeIds: new[] { discard, unable }));
                nodes.Add(new EffectNodeDefinition(discard, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticCardStateCapabilities.DiscardChosenCardV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticCardStateCapabilities.HeroParameter,
                            heroes, HeroOutput(slot), EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.DiscardedOutput,
                            EffectValueKind.CardInstance),
                        new EffectOutputBinding(
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(unable, EffectNodeKind.Conditional,
                    EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { defeat },
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput("flag", discard,
                            SyntheticCardStateCapabilities.UnableToDiscardOutput,
                            EffectValueKind.Boolean)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("value", EffectValueKind.Boolean)
                    }));
                nodes.Add(new EffectNodeDefinition(defeat, EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory,
                    SyntheticConsequenceCapabilities.DefeatHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SyntheticConsequenceCapabilities.HeroParameter,
                            heroes, HeroOutput(slot), EffectValueKind.Hero)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticConsequenceCapabilities.DefeatedHeroOutput,
                            EffectValueKind.Hero)
                    }));
            }
            nodes.Insert(0, new EffectNodeDefinition(root, EffectNodeKind.Sequence,
                EffectFailurePolicy.Mandatory, childNodeIds: children));
            return new EffectDefinition(new EffectDefinitionId(
                NebKherCardContent.WhatTheHellAreYou.Value + "-trigger"),
                NebKherCardContent.ContentRevision, root, nodes);
        }

        private sealed class EnemiesInRadiusHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => EnemiesInRadiusV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(outputs:
                    Enumerable.Range(1, MaximumEnemyHeroes).SelectMany(slot =>
                        new[]
                        {
                            new EffectOutputBinding(HeroOutput(slot), EffectValueKind.Hero),
                            new EffectOutputBinding(HasHeroOutput(slot), EffectValueKind.Boolean)
                        }).ToArray());
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!TryGetAnchor(context, out var board, out var at))
                    return EffectExecutionResult.ImpossibleMandatory();

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId, context.SourceDefinitionReference,
                    context.ActionDefinitionId, context.CardState.Definitions,
                    context.ActorId, context.HeroItems, context.Modifications).Total;
                var enemies = RosterPresenceQuery.FindUnitsWithin(
                        at, radius, RosterUnitKind.Hero,
                        SourceTeamRelation.OpposingSource, context.ActorId,
                        board, context.Participants, context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId))
                    .Where(target => !TargetQuery.IsPrevented(
                        target.AsRuleObject(), RuleActivity.Skill,
                        RuleObject.ForHero(context.ActorId), board, context.Participants,
                        context.Modifications, context.ImmunityOverrides))
                    .Select(target => target.HeroId).Distinct()
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Take(MaximumEnemyHeroes).ToArray();
                var facts = new List<EffectOutcomeFact>();
                for (var slot = 1; slot <= MaximumEnemyHeroes; slot++)
                {
                    var present = slot <= enemies.Length;
                    facts.Add(new EffectOutcomeFact(
                        HasHeroOutput(slot), EffectValue.Boolean(present)));
                    if (present)
                        facts.Add(new EffectOutcomeFact(
                            HeroOutput(slot), EffectValue.Hero(enemies[slot - 1])));
                }
                return EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                TryGetAnchor(context, out _, out _)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static bool TryGetAnchor(
                EffectCapabilityContext context,
                out IBoardDisplacementAuthority board,
                out GuardsDigital.Core.Board.HexCoordinate coordinate)
            {
                board = context.Components?.Board;
                coordinate = default;
                return board != null && context.Participants != null &&
                    board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out coordinate);
            }
        }
    }
}
