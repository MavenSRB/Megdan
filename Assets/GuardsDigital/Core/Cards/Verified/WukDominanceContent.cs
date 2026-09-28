using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 8.114b1. Claim Dominance and Assert Dominance: "This round: Up to 1 (2) enemy
    /// minion(s) adjacent to you does (do) not count towards the minion total during minion battle,
    /// regardless of immunity."
    ///
    /// <para><b>The shape is Xargatha's Constrict.</b> The Skill creates a This Round effect on Wuk
    /// (p.17) and chooses nothing; the effect declares p.8's moment before the Minion Battle, and what
    /// it runs there asks Wuk for up to one (two) enemy minions adjacent to him AT THAT MOMENT (WU-Q3,
    /// user, 2026-09-28). A minion that is no longer adjacent is not offered and counts.</para>
    ///
    /// <para><b>What the choice changes.</b> The chosen minions are written to the board's
    /// Minion-Battle exclusions, which p.8's count reads and nothing else does: WU-Q6 (user,
    /// 2026-09-28) keeps an excluded minion in the Battle Zone and removable. "Regardless of immunity"
    /// is why the candidates are not filtered by it - this is not targeting (p.19), and the sentence
    /// says so. The exclusions are cleared with p.8's end-of-round cleanup.</para>
    /// </summary>
    public static class WukDominanceContent
    {
        public static EffectCapabilityReference ClaimDominanceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-claim-dominance"), 1);

        public static EffectCapabilityReference AssertDominanceV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-assert-dominance"), 1);

        public static EffectCapabilityReference ExcludeAdjacentEnemyMinionsV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("wuk-exclude-adjacent-enemy-minions"), 1);

        public const string MaximumParameter = "maximum";

        public const string ExcludeNode = "dominance-exclude";

        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectDefinition ClaimTriggeredEffect => LazyClaimTriggered.Value;

        public static EffectDefinition AssertTriggeredEffect => LazyAssertTriggered.Value;

        public static ActiveEffectDefinition ClaimDefinition => LazyClaim.Value;

        public static ActiveEffectDefinition AssertDefinition => LazyAssert.Value;

        private static readonly Lazy<EffectDefinition> LazyClaimTriggered =
            new Lazy<EffectDefinition>(() => Exclusion("wuk-claim-dominance-exclusion", 1));

        private static readonly Lazy<EffectDefinition> LazyAssertTriggered =
            new Lazy<EffectDefinition>(() => Exclusion("wuk-assert-dominance-exclusion", 2));

        private static readonly Lazy<ActiveEffectDefinition> LazyClaim = new Lazy<ActiveEffectDefinition>(() =>
            Effect("wuk-claim-dominance-effect", "WukClaimDominance", "wuk-claim-dominance",
                LazyClaimTriggered.Value.Reference));

        private static readonly Lazy<ActiveEffectDefinition> LazyAssert = new Lazy<ActiveEffectDefinition>(() =>
            Effect("wuk-assert-dominance-effect", "WukAssertDominance", "wuk-assert-dominance",
                LazyAssertTriggered.Value.Reference));

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new ActiveEffectCreationHandler(ClaimDominanceV1, ClaimDefinition),
            new ActiveEffectCreationHandler(AssertDominanceV1, AssertDefinition),
            new ExcludeAdjacentEnemyMinionsHandler()
        };

        public static IReadOnlyList<EffectDefinition> TriggeredEffects() =>
            Array.AsReadOnly(new[] { ClaimTriggeredEffect, AssertTriggeredEffect });

        /// <summary>The Skill primary's text: create the effect, and nothing else.</summary>
        public static EffectDefinition SkillText(CardDefinitionId card, EffectCapabilityReference capability)
        {
            var source = new EffectNodeId("dominance-source");
            return new EffectDefinition(
                new EffectDefinitionId(card.Value + "-skill"), WukCardContent.ContentRevision, source,
                new[]
                {
                    new EffectNodeDefinition(
                        source, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory, capability,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                ActiveEffectCreationHandler.AffectedOutputName, EffectValueKind.Hero)
                        })
                });
        }

        private static ActiveEffectDefinition Effect(
            string definitionId, string effectKind, string sourceKind, EffectDefinitionReference triggered) =>
            new ActiveEffectDefinition(
                definitionId,
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                Cancellations,
                false,
                effectKind,
                sourceKind,
                new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle },
                triggeredEffect: triggered);

        private static EffectDefinition Exclusion(string effectId, int maximum)
        {
            var node = new EffectNodeId(ExcludeNode);
            return new EffectDefinition(
                new EffectDefinitionId(effectId), WukCardContent.ContentRevision, node,
                new[]
                {
                    new EffectNodeDefinition(
                        node, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        ExcludeAdjacentEnemyMinionsV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral(MaximumParameter, EffectValue.Integer(maximum))
                        })
                });
        }

        /// <summary>
        /// "Up to N enemy minions adjacent to you do not count": offers the enemy minions adjacent to
        /// the actor now, takes up to N of them (none is an answer), and writes them to the board's
        /// Minion-Battle exclusions. With none adjacent there is nothing to ask and nothing happens.
        /// </summary>
        private sealed class ExcludeAdjacentEnemyMinionsHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ExcludeAdjacentEnemyMinionsV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        MaximumParameter, EffectValueKind.Integer, true)
                },
                Array.Empty<EffectOutputBinding>());

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.Applied(Array.Empty<EffectOutcomeFact>());
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(EffectValue.Minion).ToArray(),
                    0,
                    Math.Min(Maximum(context), candidates.Count)));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = (selected ?? Array.Empty<EffectValue>()).ToArray();
                if (chosen.Length > Maximum(context) ||
                    chosen.Any(value => value.Kind != EffectValueKind.Minion) ||
                    chosen.Select(value => value.MinionId).Distinct().Count() != chosen.Length ||
                    chosen.Any(value => !candidates.Contains(value.MinionId)) ||
                    !(context.Components?.Board is IMinionBattleExclusionAuthority exclusions))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                exclusions.ExcludeFromMinionBattle(chosen.Select(value => value.MinionId));
                return EffectExecutionResult.Applied(Array.Empty<EffectOutcomeFact>());
            }

            private static int Maximum(EffectCapabilityContext context) =>
                context.Inputs.TryGetValue(MaximumParameter, out var value) ? value.IntegerValue : 0;

            /// <summary>p.19's enemy minions one space from the actor - effective team, no immunity.</summary>
            private static IReadOnlyList<MinionId> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<MinionId>();
                }

                return RosterPresenceQuery.FindUnitsWithin(
                        actorAt, 1, RosterUnitKind.Minion, SourceTeamRelation.OpposingSource,
                        context.ActorId, board, context.Participants, context.Modifications)
                    .Where(unit => unit.Kind == BoardObjectKind.Minion)
                    .Select(unit => unit.MinionId)
                    .Distinct()
                    .OrderBy(minion => minion.Value, StringComparer.Ordinal)
                    .ToArray();
            }
        }
    }
}
