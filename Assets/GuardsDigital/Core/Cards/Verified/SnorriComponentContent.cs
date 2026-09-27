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
        public static EffectCapabilityReference ActiveRuneV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("snorri-active-rune"), 1);
        public static EffectCapabilityReference AnySupportRuneV1 { get; } =
            new EffectCapabilityReference(
                new EffectCapabilityId("snorri-any-support-rune"), 1);

        public static IEnumerable<MarkerDefinition> Markers() => Runes;

        /// <summary>
        /// A physical rune is active for every player's card by the live turn number, not by
        /// the acting hero or the Silver card's zone. Its slot and marker identity are global.
        /// </summary>
        public static MarkerAttachment ActiveRune(
            MarkerAttachmentRegister markers, HeroId queryingHeroId, int turnIndex)
        {
            if (markers == null) throw new ArgumentNullException(nameof(markers));
            if (!queryingHeroId.IsDefined)
                throw new ArgumentException("A rune query requires its acting hero.", nameof(queryingHeroId));
            if (turnIndex < 1 || turnIndex > Core.Cards.RoundResolutionAuthority.TurnsPerRound)
                throw new ArgumentOutOfRangeException(nameof(turnIndex));
            return markers.Attachments.SingleOrDefault(marker =>
                marker.TurnSlotIndex == turnIndex &&
                Runes.Any(rune => rune.Reference.Equals(marker.Definition.Reference)));
        }

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new InscribeRunesHandler(), new ActiveRuneHandler(),
                new AnySupportRuneHandler()
            };

        public static bool IsRuneActive(MarkerAttachmentRegister markers, int turnIndex, int ordinal) =>
            ordinal >= 1 && ordinal <= Runes.Length &&
            markers.Attachments.Any(marker => marker.IsActiveInTurn(turnIndex) &&
                marker.Definition.Reference.Equals(Runes[ordinal - 1].Reference));

        /// <summary>
        /// Milestone 8.93f. What an offer may conclude from a rune gate before the action starts:
        /// the live reading, so an Attack reachable only through an inactive rune's step is not
        /// offered and then spent doing nothing. It is withheld (null - undecided, the walk's
        /// conservative path) while the acting hero has Rune Mastery unlocked, because that
        /// Ultimate chooses an inactive rune before every action and so can make any gate true.
        /// </summary>
        private static IReadOnlyList<EffectOutcomeFact> AnticipatedRuneReading(
            IEffectCapabilityHandler predicate, EffectCapabilityContext context)
        {
            if (context == null || !context.CardState.IsAvailable ||
                context.Components?.Markers == null)
            {
                return null;
            }

            var masteryCanChoose =
                context.Components.Markers.Attachments.Any(marker =>
                    marker.HolderHeroId.Equals(context.ActorId) &&
                    Runes.Any(rune => rune.Reference.Equals(marker.Definition.Reference)) &&
                    !marker.IsActiveInTurn(context.TurnIndex)) &&
                GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery
                    .Derive(context.CardState.Definitions, context.CardState.Instances).Ultimates
                    .Any(ultimate => ultimate.OwnerId.Equals(context.ActorId) &&
                        ultimate.SourceDefinitionId.Equals(SnorriUltimateContent.RuneMastery));
            if (masteryCanChoose)
            {
                return null;
            }

            var reading = predicate.Execute(context);
            return reading.Kind == EffectExecutionResultKind.Applied ? reading.Outcomes : null;
        }

        /// <summary>The support target is chosen once, before its individually gated clauses.</summary>
        public sealed class AnySupportRuneHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => AnySupportRuneV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("mask", EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.All<SnorriRuneMask>()) },
                    new[] { new EffectOutputBinding("active", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context?.Components == null || !context.Components.IsLive ||
                    !context.ActorId.IsDefined || context.TurnIndex < 1 ||
                    context.TurnIndex > Core.Cards.RoundResolutionAuthority.TurnsPerRound)
                    return EffectExecutionResult.ImpossibleMandatory();
                var mask = context.Input("mask");
                if (mask.Kind != EffectValueKind.Integer || mask.IntegerValue < 1 ||
                    mask.IntegerValue > 15)
                    return EffectExecutionResult.ImpossibleMandatory();
                var active = Enumerable.Range(1, Runes.Length).Any(ordinal =>
                        (mask.IntegerValue & (1 << (ordinal - 1))) != 0 &&
                        IsRuneActive(context.Components.Markers, context.TurnIndex, ordinal));
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("active", EffectValue.Boolean(active))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("A rune predicate never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Execute(context).Kind;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                AnticipatedRuneReading(this, context);
        }

        /// <summary>
        /// Pure live-turn predicate for rune-gated card text. A declaration names an ordinal,
        /// while the marker register and current turn remain the sole authority for its truth.
        /// </summary>
        public sealed class ActiveRuneHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => ActiveRuneV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("rune", EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.All<SnorriRuneGlyph>()) },
                    new[] { new EffectOutputBinding("active", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context?.Components == null || !context.Components.IsLive ||
                    !context.ActorId.IsDefined || context.TurnIndex < 1 ||
                    context.TurnIndex > Core.Cards.RoundResolutionAuthority.TurnsPerRound ||
                    !context.Inputs.TryGetValue("rune", out var value) ||
                    value.Kind != EffectValueKind.Integer || value.IntegerValue < 1 ||
                    value.IntegerValue > Runes.Length)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("active", EffectValue.Boolean(
                        IsRuneActive(context.Components.Markers, context.TurnIndex,
                            value.IntegerValue)))
                });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("An active-rune predicate never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Execute(context).Kind;

            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                AnticipatedRuneReading(this, context);
        }

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
                        pair.Slot, held?.ActiveUntilHolderNextAction ?? false);
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
                if (components == null || !components.IsLive || !context.ActorId.IsDefined ||
                    !context.CardState.IsAvailable ||
                    !context.SourceDefinitionReference.Equals(SnorriSilverFace.Silver.Reference) ||
                    !context.CardState.Instances.Any(card =>
                        card.Id.Equals(context.SourceCardInstanceId) &&
                        card.OwnerId.Equals(context.ActorId) &&
                        card.DefinitionReference.Equals(SnorriSilverFace.Silver.Reference)))
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

    /// <summary>The four printed rune glyphs, by their visible order on Rune Sigils.</summary>
    public enum SnorriRuneGlyph
    {
        Rune1 = 1,
        Rune2 = 2,
        Rune3 = 3,
        Rune4 = 4
    }

    /// <summary>
    /// A non-empty set of printed rune glyphs, bit N-1 for Rune N. Every combination is a declared
    /// member so the closed-vocabulary check can reject any literal outside 1-15 at content load.
    /// </summary>
    public enum SnorriRuneMask
    {
        Runes1 = 1,
        Runes2 = 2,
        Runes12 = 3,
        Runes3 = 4,
        Runes13 = 5,
        Runes23 = 6,
        Runes123 = 7,
        Runes4 = 8,
        Runes14 = 9,
        Runes24 = 10,
        Runes124 = 11,
        Runes34 = 12,
        Runes134 = 13,
        Runes234 = 14,
        Runes1234 = 15
    }
}
