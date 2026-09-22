using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75a/8.75c structural probe for Ignatia's seven ATTACK bodies - her two Gold basics
    /// and her five Reds. It builds a catalog of the base synthetic capabilities plus her minted
    /// attack-target selections and her deferred coin predicate, then runs
    /// <see cref="EffectDefinitionValidator"/> over each composed body - so a mistyped binding,
    /// unreachable node, or unknown capability fails offline rather than only at a Unity launch.
    /// Runtime behaviour (the coin actually picking the arm, the right unit being offered, the repeat
    /// firing) stays Unity-gated; this pins that the composition is one the build can load, and that
    /// each root is the coin Conditional on <c>ignatia-coin-sun@1</c>.
    /// </summary>
    public sealed class IgnatiaAttackBodyProbeTests
    {
        [Test]
        public void TheSevenAttacksComposeToBodiesTheBuildCanLoad()
        {
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                IgnatiaAttackTargetingContent.Handlers()
                    .Concat(IgnatiaComponentContent.DeferredHandlers()));

            foreach (var cardId in new[]
            {
                IgnatiaCardContent.ChaosBolt,
                IgnatiaCardContent.PlayingWithFire,
                IgnatiaCardContent.ErraticFireblast,
                IgnatiaCardContent.CrackOfDoom,
                IgnatiaCardContent.LooselyAimedFirebolts,
                IgnatiaCardContent.ImminentEruption,
                IgnatiaCardContent.UnreliableJolt
            })
            {
                var body = IgnatiaCardContent.Definition(cardId).ActionDefinitions
                    .Single(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .EffectRoot;

                Assert.That(body, Is.Not.Null, $"'{cardId.Value}' composes a body.");
                Assert.That(body.Root.Kind, Is.EqualTo(EffectNodeKind.Conditional),
                    $"'{cardId.Value}' root is the coin Conditional.");
                Assert.That(body.Root.Capability, Is.EqualTo(IgnatiaComponentContent.CoinSunV1),
                    $"'{cardId.Value}' is branched on the coin predicate.");
                Assert.That(
                    EffectDefinitionValidator.Validate(body, catalog),
                    Is.Empty,
                    $"'{cardId.Value}' must compose to a definition this build can load.");
            }
        }
    }
}
