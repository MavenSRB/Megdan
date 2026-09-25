using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Live, printed-Range target choices for the two Snorri Attacks whose reach depends on
    /// the first rune. The shared printed-icon query owns items, immunity and side checks.
    /// </summary>
    public static class SnorriAttackTargetContent
    {
        public const string TargetHeroOutput = "targetHero";
        public static EffectCapabilityReference SigilsTargetV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("snorri-sigils-target"), 1);

        public static EffectCapabilityReference RuneblasterTargetV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("snorri-runeblaster-target"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new RuneTargetHandler(SigilsTargetV1, sigils: true),
                new RuneTargetHandler(RuneblasterTargetV1, sigils: false)
            };

        private sealed class RuneTargetHandler :
            IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly bool sigils;

            public RuneTargetHandler(EffectCapabilityReference reference, bool sigils)
            {
                Reference = reference;
                this.sigils = sigils;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            "activity", EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None)),
                        new EffectCapabilityParameter(
                            SyntheticTargetingCapabilities.TargetRelationParameterName,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(SourceTeamRelation.None)),
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node, isRequired: false)
                    },
                    new[]
                    {
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenHeroOutputName,
                            EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticTargetingCapabilities.ChosenMinionOutputName,
                            EffectValueKind.Minion),
                        new EffectOutputBinding(TargetHeroOutput, EffectValueKind.Boolean)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = AnticipateCandidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var facts = AnticipateOutcomes(context, selected);
                return facts.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(facts);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public bool PublishesWithoutApplying => true;
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Targets(context)
                    .Select(target => target.Kind == BoardObjectKind.Hero
                        ? EffectValue.Hero(target.HeroId)
                        : EffectValue.Minion(target.MinionId))
                    .ToArray();

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                if (chosen == null || chosen.Count != 1 ||
                    !AnticipateCandidates(context).Contains(chosen[0]))
                    return Array.Empty<EffectOutcomeFact>();

                var hero = chosen[0].Kind == EffectValueKind.Hero;
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(
                        hero
                            ? SyntheticTargetingCapabilities.ChosenHeroOutputName
                            : SyntheticTargetingCapabilities.ChosenMinionOutputName,
                        chosen[0])
                };
                if (context.Publishes(TargetHeroOutput))
                    facts.Add(new EffectOutcomeFact(TargetHeroOutput, EffectValue.Boolean(hero)));
                return facts;
            }

            private IReadOnlyList<BoardObjectReference> Targets(EffectCapabilityContext context)
            {
                var board = context?.Components?.Board;
                if (board == null || context.TurnIndex < 1 || context.TurnIndex > 4 ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<BoardObjectReference>();

                var rune = SnorriComponentContent.ActiveRune(
                    context.Components.Markers, context.ActorId, context.TurnIndex);
                var firstRune = rune != null &&
                    rune.Definition.Reference.Equals(SnorriComponentContent.Runes[0].Reference);
                var all = SyntheticTargetingCapabilities.PrintedIconSelection(
                    context, PrintedValueChannel.Range,
                    SyntheticTargetingCapabilities.PrintedIconKinds.Units,
                    relationRequired: true);
                if (sigils)
                {
                    // The fourth glyph's repeat expressly names a different HERO in range.
                    // It widens that repeated instance independently of the first glyph's
                    // minion-only reach on the original Attack.
                    // Card-text frames themselves retain PrintedPrimary even inside a repeated
                    // Attack. Its producing clause is carried by the enclosing Attack facts.
                    if (context.EnclosingAttack.RepeatedSubject.AdmittedKind ==
                        BoardObjectKind.Hero)
                        return all.Where(target => target.Kind == BoardObjectKind.Hero).ToArray();

                    return all.Where(target =>
                    {
                        if (!board.TryGetObjectCoordinate(target, out var targetAt))
                            return false;
                        return HexDistanceQuery.Calculate(actorAt, targetAt) == 1 ||
                            (firstRune && target.Kind == BoardObjectKind.Minion);
                    }).ToArray();
                }

                return firstRune
                    ? all
                    : SyntheticTargetingCapabilities.PrintedIconSelection(
                        context.WithAdditionalInputs(new Dictionary<string, EffectValue>
                        {
                            [SyntheticTargetingCapabilities.AtMaximumReachParameterName] =
                                EffectValue.Boolean(true)
                        }),
                        PrintedValueChannel.Range,
                        SyntheticTargetingCapabilities.PrintedIconKinds.Units,
                        relationRequired: true);
            }
        }
    }
}
