using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75e structural probe for the Green shard. It builds a catalog of the base synthetic
    /// capabilities plus Ignatia's deferred coin predicate, then runs
    /// <see cref="EffectDefinitionValidator"/> over each COMPOSED Green body - so a mistyped binding,
    /// unreachable node, or unknown capability fails offline rather than only at a Unity launch. Runtime
    /// behaviour (the swap actually exchanging the right pair, the optional space actually walked) stays
    /// Unity-gated; this pins that the composition is one the build can load.
    ///
    /// <para>Only the SWAP line composes a body in this slice. The MAGMA line - Path of Ashes, Path of
    /// Cinders, Path of Flames - keeps its awaited placeholder because it needs a magma token definition
    /// and its placement, and a place-along-a-move's-wake capability, that this build does not carry; the
    /// second test pins that those three faces compose no body yet, so the deferral is a fact the probe
    /// asserts rather than an omission a reader has to infer.</para>
    /// </summary>
    public sealed class IgnatiaGreenBodyProbeTests
    {
        [Test]
        public void TheSwapLineComposesToBodiesTheBuildCanLoad()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaComponentContent.DeferredHandlers());

            foreach (var cardId in new[]
            {
                IgnatiaCardContent.UnstablePortal,
                IgnatiaCardContent.ChaosGate
            })
            {
                var body = IgnatiaCardContent.Definition(cardId).ActionDefinitions
                    .Single(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .EffectRoot;
                Assert.That(body, Is.Not.Null, $"'{cardId.Value}' composes a body.");
                Assert.That(body.Root.Kind, Is.EqualTo(EffectNodeKind.Conditional),
                    "its root is the coin Conditional.");
                Assert.That(body.Root.Capability, Is.EqualTo(IgnatiaComponentContent.CoinSunV1),
                    "branched on the coin predicate.");
                Assert.That(
                    EffectDefinitionValidator.Validate(body, catalog),
                    Is.Empty,
                    $"'{cardId.Value}' must compose to a definition this build can load.");
            }
        }

        // The magma line's own structural validation lives in IgnatiaMagmaBodyProbeTests, which
        // builds a catalog including the magma placement handlers. The earlier deferral assertion
        // here was discharged when milestone 8.75e composed those three faces.
    }
}
