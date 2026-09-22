using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    /// <summary>
    /// Milestone 8.75 coin-text seam probe. Exercises <c>ignatia-coin-sun@1</c> - the predicate a
    /// <c>Conditional</c> branches on to pick her sun or moon text - directly, over a context carrying
    /// each revealed Tie Breaker face. Behaviour inside a live match is Unity-gated; this pins the
    /// decision and that the coin threads through <see cref="EffectCapabilityContext"/>.
    /// </summary>
    public sealed class IgnatiaCoinPredicateProbeTests
    {
        [Test]
        public void TheSunMoonMappingIsTotalAndOpposite()
        {
            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(IgnatiaComponentContent.SunFace), Is.True);
            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(IgnatiaComponentContent.MoonFace), Is.False);
            Assert.That(IgnatiaComponentContent.RevealedFaceIsSun(TeamSide.None), Is.False);
            Assert.That(IgnatiaComponentContent.IsRevealed(IgnatiaComponentContent.SunFace), Is.True);
            Assert.That(IgnatiaComponentContent.IsRevealed(IgnatiaComponentContent.MoonFace), Is.True);
            Assert.That(IgnatiaComponentContent.IsRevealed(TeamSide.None), Is.False);
            Assert.That(IgnatiaComponentContent.SunFace, Is.Not.EqualTo(IgnatiaComponentContent.MoonFace));
        }

        [Test]
        public void TheHandlerReadsTheLiveCoinFaceAndPublishesSunOrMoon()
        {
            var handler = IgnatiaComponentContent.DeferredHandlers()
                .Single(h => h.Reference.Equals(IgnatiaComponentContent.CoinSunV1));
            Assert.That(handler.Reference, Is.EqualTo(IgnatiaComponentContent.CoinSunV1));

            var sun = handler.Execute(ContextWithCoin(IgnatiaComponentContent.SunFace));
            Assert.That(sun.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(TheSunFact(sun), Is.True, "the sun face branches to the sun text.");

            var moon = handler.Execute(ContextWithCoin(IgnatiaComponentContent.MoonFace));
            Assert.That(moon.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(TheSunFact(moon), Is.False, "the moon face branches to the moon text.");

            var unrevealed = handler.Execute(ContextWithCoin(TeamSide.None));
            Assert.That(unrevealed.Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "her cards are never resolved with the coin unrevealed; the predicate refuses rather " +
                "than guessing a face.");
        }

        private static bool TheSunFact(EffectExecutionResult result) =>
            result.Outcomes.Single(o => o.OutputName == IgnatiaComponentContent.SunOutput)
                .Value.BooleanValue;

        private static EffectCapabilityContext ContextWithCoin(TeamSide revealed) =>
            new EffectCapabilityContext(
                new EffectNodeId("coin"),
                EffectFailurePolicy.Mandatory,
                new HeroId("ignatia"),
                new HeroId("ignatia"),
                default,
                default,
                default,
                null,
                0,
                0,
                null,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Absent,
                ActionKind.Skill,
                tieBreakerRevealedSide: revealed);
    }
}
