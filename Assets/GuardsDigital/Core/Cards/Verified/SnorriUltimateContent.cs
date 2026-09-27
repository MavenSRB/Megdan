using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Rune Mastery chooses for the current action (user ruling SN-Q3).</summary>
    public static class SnorriUltimateContent
    {
        public const string ContentRevision = SnorriCardContent.ContentRevision;
        public static CardDefinitionId RuneMastery { get; } = new CardDefinitionId("snorri-rune-mastery");
        public static EffectCapabilityReference ChooseRuneV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("snorri-choose-inactive-rune"), 1);
        public static EffectDefinition TriggeredEffect { get; } = new EffectDefinition(
            new EffectDefinitionId("snorri-rune-mastery-before-action"), ContentRevision,
            new EffectNodeId("choose-rune"), new[]
            {
                new EffectNodeDefinition(new EffectNodeId("choose-rune"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, ChooseRuneV1)
            });
        public static CardDefinition Definition { get; } = new CardDefinition(
            RuneMastery, ContentRevision, "Rune Mastery", default,
            CardContentAuthority.VerifiedOfficial, CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.BeforeActionAbility("rune-mastery-before-action", "SnorriRuneMastery",
                TriggeredEffect.Reference, new BeforeActionTrigger(ActionKind.None,
                    BeforeActionTriggerFrequency.EveryActionIncludingReactions),
                Array.Empty<PrintedValueDefinition>()),
            CardSourceContent.Transcribed("Each time you perform an action, choose one inactive rune; " +
                "that rune counts as active until your next action."));

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new[] { new ChooseRuneHandler() };

        public sealed class ChooseRuneHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseRuneV1;
            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!HasLiveMarkers(context)) return EffectExecutionResult.ImpossibleMandatory();
                context.Components.Markers.EndTemporaryActivations(context.ActorId);
                var choices = InactiveOrdinals(context);
                return choices.Length == 0 ? EffectExecutionResult.Applied() :
                    EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, choices.Select(EffectValue.Integer), 1, 1));
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context,
                IReadOnlyList<EffectValue> selected)
            {
                if (!HasLiveMarkers(context) || selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer ||
                    !InactiveOrdinals(context).Contains(selected[0].IntegerValue))
                    return EffectExecutionResult.ImpossibleMandatory();
                var reference = SnorriComponentContent.Runes[selected[0].IntegerValue - 1].Reference;
                var marker = context.Components.Markers.HeldBy(context.ActorId)
                    .Single(value => value.Definition.Reference.Equals(reference));
                context.Components.Markers.ActivateUntilHolderNextAction(marker.Id, context.TurnIndex);
                return EffectExecutionResult.Applied();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                !HasLiveMarkers(context) ? EffectExecutionResultKind.ImpossibleMandatory :
                context.Components.Markers.HeldBy(context.ActorId).Any(marker =>
                    marker.TurnSlotIndex.HasValue && marker.TurnSlotIndex != context.TurnIndex &&
                    SnorriComponentContent.Runes.Any(rune =>
                        rune.Reference.Equals(marker.Definition.Reference)))
                    ? EffectExecutionResultKind.NeedsChoice : EffectExecutionResultKind.Applied;

            private static bool HasLiveMarkers(EffectCapabilityContext context) =>
                context?.Components != null && context.Components.IsLive &&
                context.ActorId.IsDefined && context.TurnIndex >= 1 &&
                context.TurnIndex <= RoundResolutionAuthority.TurnsPerRound;

            private static int[] InactiveOrdinals(EffectCapabilityContext context) =>
                Enumerable.Range(1, SnorriComponentContent.Runes.Length).Where(ordinal =>
                    context.Components.Markers.HeldBy(context.ActorId).Any(marker =>
                        marker.TurnSlotIndex.HasValue && !marker.IsActiveInTurn(context.TurnIndex) &&
                        marker.Definition.Reference.Equals(
                            SnorriComponentContent.Runes[ordinal - 1].Reference))).ToArray();
        }
    }
}
