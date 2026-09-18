using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Dragon Bond and WI-Q3: one persistent Pyro. Deferred until Widget is dealt.</summary>
    public static class WidgetComponentContent
    {
        public static TokenDefinition PyroDefinition { get; } = new TokenDefinition(
            new TokenDefinitionId("widget-pyro"), 1, "Pyro", 1,
            Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.RetainedAtEndOfRound);

        public static EffectCapabilityReference PyroPresentV1 => Reference("widget-pyro-present");
        public static EffectCapabilityReference PyroSpaceV1 => Reference("widget-pyro-space");
        public static EffectCapabilityReference PyroInRangeV1 => Reference("widget-pyro-in-range");
        public static EffectCapabilityReference PyroInRadiusV1 => Reference("widget-pyro-in-radius");
        public static EffectCapabilityReference PlacePyroV1 => Reference("widget-place-pyro");

        public static IEnumerable<TokenDefinition> Tokens() => new[] { PyroDefinition };
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() => Array.Empty<IEffectCapabilityHandler>();
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() => new[]
        {
            SyntheticOwnedTokenCapabilities.PresenceHandler(PyroPresentV1, PyroDefinition),
            SyntheticOwnedTokenCapabilities.SpaceHandler(PyroSpaceV1, PyroDefinition),
            SyntheticOwnedTokenCapabilities.SpaceHandler(PyroInRangeV1, PyroDefinition, PrintedValueChannel.Range),
            SyntheticOwnedTokenCapabilities.SpaceHandler(PyroInRadiusV1, PyroDefinition, PrintedValueChannel.Radius),
            SyntheticTokenCapabilities.PlaceHandler(PlacePyroV1, PyroDefinition, PrintedValueChannel.Radius)
        };

        private static EffectCapabilityReference Reference(string id) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), 1);
    }
}
