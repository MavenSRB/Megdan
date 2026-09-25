using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// One marker that has left the supply and is attached to a hero.
    ///
    /// This type has no coordinate and no board concept of any kind, which is how milestone 6.2's second
    /// acceptance check - markers are attachments rather than board occupants - is made structural. There
    /// is no field a marker could occupy a space with, and <c>BoardOccupancyIndex</c> has no arm that
    /// admits one. Rulebook p.18: "Unlike tokens, markers are usually not placed on the board."
    ///
    /// Like <see cref="PlacedToken"/>, it holds the definition object rather than a copy of its content,
    /// so the return condition is read from the trusted catalog and can never be stated by a payload.
    /// </summary>
    public sealed class MarkerAttachment
    {
        public MarkerAttachment(
            MarkerInstanceId id,
            MarkerDefinition definition,
            HeroId ownerHeroId,
            HeroId holderHeroId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId sourceActionDefinitionId,
            int? turnSlotIndex = null)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("A marker attachment requires a stable instance ID.", nameof(id));
            }

            if (!holderHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "A marker attachment requires a holder; a marker attached to nobody is in the supply.",
                    nameof(holderHeroId));
            }

            if (turnSlotIndex.HasValue && (turnSlotIndex.Value < 1 ||
                turnSlotIndex.Value > RoundResolutionAuthority.TurnsPerRound))
            {
                throw new ArgumentOutOfRangeException(nameof(turnSlotIndex));
            }

            if (turnSlotIndex.HasValue && definition != null &&
                definition.ReturnCondition != MarkerReturnCondition.RetainedAtRoundEndAndDefeat)
            {
                throw new ArgumentException(
                    "A marker below a turn slot requires a printed retained-marker definition.",
                    nameof(turnSlotIndex));
            }

            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            OwnerHeroId = ownerHeroId;
            HolderHeroId = holderHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
            TurnSlotIndex = turnSlotIndex;

            // Milestone 8.23, the exact shape PlacedToken derives its own from - one source per
            // declared property, in declaration order, so the two collections address the same
            // property at the same index. The controller is the HOLDER and not the owner: p.18 gives
            // markers to other heroes, and Tigerclaw's two Green cards say whose values change - "The
            // hero with a poison marker has -1 Initiative, -1 Attack and -1 Defense".
            PropertySources = new ReadOnlyCollection<ContinuousRuleSource>(definition.Properties
                .Select(property => new ContinuousRuleSource(
                    $"marker:{id.Value}:property:{property.PropertyId}",
                    ContinuousRuleSourceKind.MarkerProperty,
                    holderHeroId,
                    property.RuleKind,
                    property.ApplicabilityKind,
                    sourceCardInstanceId))
                .ToArray());
        }

        public MarkerInstanceId Id { get; }

        /// <summary>The trusted definition object, supplied by the catalog and never by a payload.</summary>
        public MarkerDefinition Definition { get; }

        /// <summary>
        /// The hero whose content owns this marker, and therefore the supply a return puts it back into.
        /// Distinct from <see cref="HolderHeroId"/>, because rulebook p.18 describes markers as
        /// "usually given to other heroes": the two are normally different heroes.
        /// </summary>
        public HeroId OwnerHeroId { get; }

        /// <summary>The hero currently holding the marker.</summary>
        public HeroId HolderHeroId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId SourceActionDefinitionId { get; }

        /// <summary>
        /// The numbered turn slot this printed retained marker sits below; null for ordinary
        /// hero-held markers. It is a dashboard association, never a board coordinate.
        /// </summary>
        public int? TurnSlotIndex { get; }

        /// <summary>
        /// The explicit return state milestone 6.2's second acceptance check requires, read from the
        /// trusted definition rather than recorded per attachment.
        /// </summary>
        public MarkerReturnCondition ReturnCondition => Definition.ReturnCondition;

        /// <summary>
        /// Continuous rules whose lifetime is this attachment's own: they apply while this hero holds
        /// this marker and stop the moment p.8's end of round or p.13's Defeat returns it, because
        /// returning it removes the attachment these are derived from. Milestone 8.23, and the twin of
        /// <see cref="PlacedToken.PropertySources"/> - derived from trusted definition content, and
        /// deliberately outside the Active-effect lifecycle.
        /// </summary>
        public IReadOnlyList<ContinuousRuleSource> PropertySources { get; }

        public override string ToString() =>
            $"{Id} ({Definition.Reference}) held by {HolderHeroId}";
    }
}
