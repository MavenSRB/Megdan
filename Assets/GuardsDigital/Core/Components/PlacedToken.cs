using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// One token that has left the supply and is on the board.
    ///
    /// It holds the <see cref="TokenDefinition"/> object rather than a copy of its content, the same way
    /// <c>ActiveEffectInstance</c> holds an <c>ActiveEffectDefinition</c>. That is what a restore rebinds:
    /// the payload names a definition reference, the catalog supplies the object, and the reconstructed
    /// token is reference-equal to the trusted definition rather than an equivalent rebuilt from bytes.
    ///
    /// The owner, controller, and source links are records of provenance, not of lifetime. Rulebook p.19
    /// is explicit that tokens "are not removed when the hero who placed them is defeated", so nothing
    /// here may be read as a lifetime rule; a token ends only when an effect removes it.
    /// </summary>
    public sealed class PlacedToken
    {
        public PlacedToken(
            TokenInstanceId id,
            TokenDefinition definition,
            HeroId ownerHeroId,
            HeroId controllerHeroId,
            CardInstanceId sourceCardInstanceId,
            HexCoordinate coordinate)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("A placed token requires a stable instance ID.", nameof(id));
            }

            Id = id;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            OwnerHeroId = ownerHeroId;
            ControllerHeroId = controllerHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
            Coordinate = coordinate;
            PropertySources = new ReadOnlyCollection<ContinuousRuleSource>(definition.Properties
                .Select(property => new ContinuousRuleSource(
                    $"token:{id.Value}:property:{property.PropertyId}",
                    ContinuousRuleSourceKind.TokenProperty,
                    controllerHeroId,
                    property.RuleKind,
                    property.ApplicabilityKind,
                    sourceCardInstanceId))
                .ToArray());
        }

        public TokenInstanceId Id { get; }

        /// <summary>The trusted definition object, supplied by the catalog and never by a payload.</summary>
        public TokenDefinition Definition { get; }

        /// <summary>The hero whose effect placed this token. Provenance only; see the type remarks.</summary>
        public HeroId OwnerHeroId { get; }

        public HeroId ControllerHeroId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public HexCoordinate Coordinate { get; }

        /// <summary>
        /// Continuous rules whose lifetime is this token instance's board lifetime. They are derived
        /// from trusted definition content and deliberately do not enter the Active-effect lifecycle.
        /// </summary>
        public IReadOnlyList<ContinuousRuleSource> PropertySources { get; }

        /// <summary>
        /// The same token at a different space. Displacement arrives with milestones 6.4 and 6.6; this
        /// exists so that when it does, the instance moves rather than being reconstructed - which is
        /// exactly what <c>STATE_CAPABILITIES.md</c> requires of a token's persistent properties.
        /// </summary>
        public PlacedToken WithCoordinate(HexCoordinate coordinate) =>
            new PlacedToken(Id, Definition, OwnerHeroId, ControllerHeroId, SourceCardInstanceId, coordinate);

        public override string ToString() => $"{Id} ({Definition.Reference}) at {Coordinate}";
    }
}
