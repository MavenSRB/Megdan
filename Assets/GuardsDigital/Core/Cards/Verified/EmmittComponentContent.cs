using System;
using System.Collections.Generic;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Emmitt's physical Glitch supply and its typed placement capabilities.</summary>
    public static class EmmittComponentContent
    {
        public const int MinimumGlitchHexDistance = 3;

        public static TokenDefinition Glitch { get; } = new TokenDefinition(
            new TokenDefinitionId("emmitt-glitch"), 1, "Glitch", 3,
            Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfTurnCleanup);

        public static EffectCapabilityReference PlaceGlitchV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-place-glitch"), 1);

        public static EffectCapabilityReference ReclaimGlitchV1 { get; } =
            new EffectCapabilityReference(new EffectCapabilityId("emmitt-reclaim-glitch"), 1);

        /// <summary>
        /// EM-Q3: two empty hexes between Glitches, so their hex distance is at least three.
        /// The p.18 short-supply handler precedes each Place in content.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                SyntheticTokenCapabilities.ReclaimHandler(ReclaimGlitchV1, Glitch),
                SyntheticTokenCapabilities.PlaceHandler(
                    PlaceGlitchV1, Glitch, PrintedValueChannel.Radius,
                    minimumSameKindHexDistance: MinimumGlitchHexDistance)
            };
    }
}
