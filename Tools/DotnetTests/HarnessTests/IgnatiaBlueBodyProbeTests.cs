using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75d structural probe for the Blue PUSH line. It builds a catalog of the base
    /// synthetic capabilities plus Ignatia's deferred coin predicate, then runs
    /// <see cref="EffectDefinitionValidator"/> over each composed body - so a mistyped binding,
    /// unreachable node, or unknown capability fails offline rather than only at a Unity launch.
    /// Runtime behaviour (the hero actually moving the right distance in a straight line) stays
    /// Unity-gated; this pins that the composition is one the build can load.
    /// </summary>
    public sealed class IgnatiaBlueBodyProbeTests
    {
        [Test]
        public void ThePushLineComposesToBodiesTheBuildCanLoad()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaComponentContent.DeferredHandlers());

            foreach (var cardId in new[]
            {
                IgnatiaCardContent.SearingHeat,
                IgnatiaCardContent.ScorchingBlaze
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
    }
}
