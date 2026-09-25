using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Four distinct rune markers. Ordinals follow the glyphs on Rune Sigils; the player chooses
    /// which one belongs below each numbered turn slot. Not registered until Snorri is dealt.
    /// </summary>
    public static class SnorriComponentContent
    {
        public static readonly MarkerDefinition[] Runes = Enumerable.Range(1, 4)
            .Select(ordinal => new MarkerDefinition(
                new MarkerDefinitionId("snorri-rune-" + ordinal), 1, "Rune " + ordinal, 1,
                MarkerReturnCondition.RetainedAtRoundEndAndDefeat))
            .ToArray();

        public static EffectCapabilityReference InscribeRunesV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("snorri-inscribe-runes"), 1);

        public static IEnumerable<MarkerDefinition> Markers() => Runes;

        /// <summary>
        /// A dashboard rune is active by the live turn number, not the card currently in its
        /// slot or the zone of the Silver card that originally gave the physical marker.
        /// </summary>
        public static MarkerAttachment ActiveRune(
            MarkerAttachmentRegister markers, HeroId holderHeroId, int turnIndex)
        {
            if (markers == null) throw new ArgumentNullException(nameof(markers));
            if (!holderHeroId.IsDefined)
                throw new ArgumentException("A rune query requires its hero.", nameof(holderHeroId));
            if (turnIndex < 1 || turnIndex > Core.Cards.RoundResolutionAuthority.TurnsPerRound)
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            return markers.HeldBy(holderHeroId).SingleOrDefault(marker =>
                marker.TurnSlotIndex == turnIndex &&
                Runes.Any(rune => rune.Reference.Equals(marker.Definition.Reference)));
        }

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[] { new InscribeRunesHandler() };

        /// <summary>
        /// A sixteen-pair offer yields four glyph-to-slot selections. Only a complete permutation
        /// replaces the four marker attachments, atomically, so UI drag positions never own rules.
        /// </summary>
        public sealed class InscribeRunesHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => InscribeRunesV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                CanInscribe(context)
                    ? EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor,
                        Enumerable.Range(1, 16).Select(EffectValue.Integer),
                        4, 4,
                        valueVocabulary: EffectChoiceValueVocabulary.RuneTurnSlot))
                    : EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (!CanInscribe(context) || selected == null || selected.Count != 4 ||
                    selected.Any(value => value.Kind != EffectValueKind.Integer ||
                        value.IntegerValue < 1 || value.IntegerValue > 16))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var decoded = selected.Select(value => new
                {
                    Rune = ((value.IntegerValue - 1) / 4) + 1,
                    Slot = ((value.IntegerValue - 1) % 4) + 1
                }).ToArray();
                if (decoded.Select(value => value.Rune).Distinct().Count() != 4 ||
                    decoded.Select(value => value.Slot).Distinct().Count() != 4)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var components = context.Components;
                var placements = decoded.Select(pair =>
                {
                    var definition = components.Catalog.LoadMarker(Runes[pair.Rune - 1].Reference);
                    var held = components.Markers.OfDefinition(definition.Reference).SingleOrDefault();
                    var identity = held?.Id ?? MarkerInstanceIdentity.Mint(
                        context, context.ActorId, definition.Reference);
                    return new MarkerAttachment(
                        identity, definition, context.ActorId, context.ActorId,
                        held?.SourceCardInstanceId ?? context.SourceCardInstanceId,
                        held?.SourceActionDefinitionId ?? context.ActionDefinitionId,
                        pair.Slot);
                }).ToArray();

                components.Markers.SetTurnSlotMarkers(context.ActorId, placements);
                return EffectExecutionResult.Applied();
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanInscribe(context)
                    ? EffectExecutionResultKind.NeedsChoice
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static bool CanInscribe(EffectCapabilityContext context)
            {
                var components = context?.Components;
                if (components == null || !components.IsLive || !context.ActorId.IsDefined)
                {
                    return false;
                }

                foreach (var definition in Runes)
                {
                    var held = components.Markers.OfDefinition(definition.Reference).ToArray();
                    if (held.Length > 1 ||
                        (held.Length == 1 && !held[0].HolderHeroId.Equals(context.ActorId)) ||
                        (held.Length == 0 &&
                            components.Catalog.CountAvailableMarkers(
                                definition.Reference, components.Markers.Attachments) < 1))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
