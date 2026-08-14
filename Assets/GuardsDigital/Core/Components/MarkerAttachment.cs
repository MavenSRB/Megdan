using System;
using GuardsDigital.Core.Cards;
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
            ActionDefinitionId sourceActionDefinitionId)
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

            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            OwnerHeroId = ownerHeroId;
            HolderHeroId = holderHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceActionDefinitionId = sourceActionDefinitionId;
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
        /// The explicit return state milestone 6.2's second acceptance check requires, read from the
        /// trusted definition rather than recorded per attachment.
        /// </summary>
        public MarkerReturnCondition ReturnCondition => Definition.ReturnCondition;

        public override string ToString() =>
            $"{Id} ({Definition.Reference}) held by {HolderHeroId}";
    }
}
