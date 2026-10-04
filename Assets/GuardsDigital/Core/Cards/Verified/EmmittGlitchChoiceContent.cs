using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Emmitt's two source-owned choices before a printed Glitch Swap.</summary>
    public static class EmmittGlitchChoiceContent
    {
        public const string ChooserParameter = "chooser";
        public const string ChosenHeroOutput = "chosenHero";
        public const string ChosenTokenSpaceOutput = "chosenTokenSpace";

        public static EffectCapabilityReference EnemyHeroInPlayV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-enemy-hero-in-play"), 1);

        public static EffectCapabilityReference ChooseGlitchByHeroV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-choose-glitch-by-hero"), 1);

        public static IEnumerable<IEffectCapabilityHandler> Handlers() => new IEffectCapabilityHandler[]
        {
            new EnemyHeroInPlayHandler(), new ChooseGlitchByHeroHandler()
        };

        private sealed class EnemyHeroInPlayHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => EnemyHeroInPlayV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(Array.Empty<EffectCapabilityParameter>(),
                    new[] { new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Length > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero), 1, 1));
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                return selected?.Count == 1 && selected[0].Kind == EffectValueKind.Hero &&
                    candidates.Contains(selected[0].HeroId)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(ChosenHeroOutput, selected[0])
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            private static HeroId[] Candidates(EffectCapabilityContext context)
            {
                if (context.Participants == null || context.Components?.Tokens == null)
                    return Array.Empty<HeroId>();
                var actorSide = context.Participants.GetTeamForHero(context.ActorId).Side;
                return context.Participants.Heroes
                    .Where(hero => context.Participants.GetTeamForHero(hero.Id).Side != actorSide &&
                        context.Components.Tokens.IsHeroOnBoard(hero.Id))
                    .Select(hero => hero.Id).OrderBy(id => id.Value, StringComparer.Ordinal).ToArray();
            }
        }

        private sealed class ChooseGlitchByHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseGlitchByHeroV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(ChooserParameter, EffectValueKind.Hero) },
                    new[]
                    {
                        new EffectOutputBinding(ChosenTokenSpaceOutput, EffectValueKind.Coordinate)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Chooser(context).IsDefined && Candidates(context).Length > 0
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var chooser = Chooser(context);
                var candidates = Candidates(context);
                return !chooser.IsDefined || candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None, candidates.Select(EffectValue.Space), 1, 1, chooser));
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                return Chooser(context).IsDefined && selected?.Count == 1 &&
                    selected[0].Kind == EffectValueKind.Coordinate &&
                    candidates.Contains(selected[0].Coordinate)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(ChosenTokenSpaceOutput, selected[0])
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            private static HeroId Chooser(EffectCapabilityContext context)
            {
                var value = context.Input(ChooserParameter);
                return value.Kind == EffectValueKind.Hero && context.Participants != null &&
                    context.Participants.Heroes.Any(hero => hero.Id.Equals(value.HeroId)) &&
                    context.Components?.Tokens?.IsHeroOnBoard(value.HeroId) == true
                    ? value.HeroId : default;
            }

            private static GuardsDigital.Core.Board.HexCoordinate[] Candidates(
                EffectCapabilityContext context) =>
                context.Components?.Tokens?.PlacedTokens
                    .Where(token => token.Definition.Reference.Equals(EmmittComponentContent.Glitch.Reference) &&
                        token.OwnerHeroId.Equals(context.ActorId))
                    .Select(token => token.Coordinate)
                    .ToArray() ?? Array.Empty<GuardsDigital.Core.Board.HexCoordinate>();
        }
    }
}
