using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75 inventory probe. Materializing <see cref="IgnatiaCardContent.AllDefinitions"/>
    /// runs every one of the nineteen <c>CardDefinition</c>/<c>ActionDefinition</c> constructors, so
    /// this test fails loudly on any malformed row rather than only at a Unity launch. It asserts the
    /// deck arithmetic her dealing depends on, nothing about composition (which is deliberately owed).
    /// </summary>
    public sealed class IgnatiaInventoryProbeTests
    {
        [Test]
        public void AllNineteenFacesMaterializeAndTheDeckMathHolds()
        {
            var all = IgnatiaCardContent.AllDefinitions;
            Assert.That(all.Count, Is.EqualTo(19), "Ignatia prints nineteen faces.");
            Assert.That(all.Select(d => d.Id).Distinct().Count(), Is.EqualTo(19),
                "every id is unique.");
            Assert.That(IgnatiaCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(IgnatiaCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(IgnatiaCardContent.ReturnedToBoxAtSetup.Count, Is.EqualTo(1));
            Assert.That(IgnatiaCardContent.DealtDefinitions.Count, Is.EqualTo(18),
                "the eighteen dealt faces are everything but the boxed Handicap.");

            // Starting hand + deck + boxed = the whole set, with no card in two lists.
            var partitioned = IgnatiaCardContent.StartingHand
                .Concat(IgnatiaCardContent.Deck)
                .Concat(IgnatiaCardContent.ReturnedToBoxAtSetup)
                .ToArray();
            Assert.That(partitioned.Distinct().Count(), Is.EqualTo(19),
                "the three lists partition the set exactly.");

            var ultimate = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
            Assert.That(ultimate.Progression.IsUltimate, Is.True);
            Assert.That(ultimate.UltimateRule, Is.Not.Null,
                "the Ultimate ships its owed rule as inventory.");
        }
    }
}
