using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75b. Equilibrium composes to a body that creates the round-scoped free-coin-choice
    /// marker, and it validates against a catalog holding Ignatia's deferred handlers (which now include
    /// the marker's creation handler). The behaviour it enables - the coin predicate asking the actor
    /// while the marker stands - is Unity-gated; this pins that the body loads and the handler is wired.
    /// </summary>
    public sealed class IgnatiaEquilibriumProbeTests
    {
        [Test]
        public void EquilibriumComposesTheRoundMarkerAndValidates()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaComponentContent.DeferredHandlers());

            var body = IgnatiaCardContent.Definition(IgnatiaCardContent.Equilibrium).ActionDefinitions
                .Single(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot;
            Assert.That(body, Is.Not.Null, "Equilibrium composes a body.");
            Assert.That(EffectDefinitionValidator.Validate(body, catalog), Is.Empty,
                "Equilibrium must compose to a definition this build can load.");

            Assert.That(
                IgnatiaComponentContent.DeferredHandlers()
                    .Any(h => h.Reference.Equals(IgnatiaComponentContent.EquilibriumV1)),
                Is.True,
                "the round-marker creation handler is carried for registration.");
            Assert.That(IgnatiaComponentContent.FreeCoinChoiceDefinition.EffectKind,
                Is.EqualTo(IgnatiaComponentContent.FreeCoinChoiceKind));
        }
    }
}
