using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75e structural probe for the MAGMA line - the half of Ignatia's Green shard that
    /// milestone 8.75e first reported as deferred vocabulary and this slice composes. It builds a catalog
    /// of the base synthetic capabilities plus her magma handlers and her deferred coin predicate, then
    /// runs <see cref="EffectDefinitionValidator"/> over each composed magma body, so a mistyped binding,
    /// an unreachable node, or an unknown capability fails offline rather than only at a Unity launch.
    ///
    /// <para>Runtime behaviour - the Move actually walked, the wake tokens actually laid, the radius
    /// scatter actually placed - stays Unity-gated; this pins that the three faces compose to definitions
    /// the build can load, that each is branched on the Tie Breaker coin, and that the magma token the
    /// whole line drops is the impassable obstacle the printed faces describe.</para>
    /// </summary>
    public sealed class IgnatiaMagmaBodyProbeTests
    {
        [Test]
        public void TheThreeMagmaFacesComposeToBodiesTheBuildCanLoad()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaMagmaContent.Handlers().Concat(IgnatiaComponentContent.DeferredHandlers()));

            foreach (var cardId in new[]
            {
                IgnatiaCardContent.PathOfAshes,
                IgnatiaCardContent.PathOfCinders,
                IgnatiaCardContent.PathOfFlames
            })
            {
                var body = IgnatiaCardContent.Definition(cardId).ActionDefinitions
                    .Single(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .EffectRoot;
                Assert.That(body, Is.Not.Null, $"'{cardId.Value}' composes a magma body.");
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

        [Test]
        public void TheMagmaSubsystemShipsHandlersAndAnImpassableToken()
        {
            Assert.That(IgnatiaMagmaContent.Handlers().Any(), Is.True,
                "the magma line registers at least its radius and wake placements.");

            // p.18: a magma token is an impassable obstacle, which on this board is the ABSENCE of Min's
            // move-through clause - ObstacleQuery.DeclaresMayBeMovedThrough is the one reader of it, and
            // a token that declares it not is one a unit cannot cross.
            Assert.That(
                ObstacleQuery.DeclaresMayBeMovedThrough(IgnatiaMagmaContent.MagmaTokenDefinition),
                Is.False,
                "the magma token is impassable - it declares no 'may be moved through' property.");
        }
    }
}
