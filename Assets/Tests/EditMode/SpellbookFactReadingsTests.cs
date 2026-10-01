using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.30h. The typed readings behind the spellbook's table facts and its presentation:
    /// the back a spell shows, the spellbook fact a handler reports, the counted values a spell choice
    /// is captioned with, and the one rule by which an arm names its spell. Unity-free. Their
    /// regressions, which read the same members by name, are in <see cref="SpellbookTableFactsTests"/>.
    /// </summary>
    public sealed class SpellbookFactReadingsTests
    {
        private static readonly HeroId Owner = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

        [Test]
        public void ABacksRow_CountsTheSameSpellsTheBooksSizeDoes()
        {
            var host = new MatchHost(SpellbookZoneTests.CreateSession(true));
            var summary = host.Projection.HandSummaries.Single(value => value.HeroId.Equals(Owner));
            Assert.That(summary.SpellbookCount, Is.EqualTo(2), "the Tier I spell is removed faceup");
            Assert.That(summary.SpellbookBacks.Sum(back => back.Count), Is.EqualTo(summary.SpellbookCount));
            Assert.That(summary.SpellbookBacks.Select(back => back.ToString()), Is.EqualTo(new[] { "Ix1", "IVx1" }),
                "the removed spell leaves its back's count; the basic spell still shows Tier I");
            Assert.That(SpellbookBackCount.BackOf(CardProgressionProfile.SpellCard(CardColor.Gold, CardTier.None)),
                Is.EqualTo(CardTier.I), "a basic spell wears the Tier I back");
            Assert.That(() => new SpellbookBackCount(CardTier.None, 1), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        // -------------------------------------------------------------------------------------------
        // The facts themselves
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ASpellbookFact_PutsTheOwnerOnTheTarget_TheSpellOnItsOwnField_AndARefillsCountInItsDetail()
        {
            var spell = new CardInstanceId("red-gydion-fireball");
            var node = new EffectResolutionEvent(
                new EffectNodeId("cast-node"),
                EffectExecutionResultKind.Applied,
                spellbookChanges: new[]
                {
                    SpellbookChange.Cast(Owner, spell), SpellbookChange.Refilled(Owner, 4)
                });

            var facts = CardActionResolutionEvent.CardTextNodeFacts(
                new ActionResolutionId("resolution"), new CardInstanceId("source"),
                new ActionDefinitionId("action"), Other, node).ToArray();

            Assert.That(facts.Select(fact => fact.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.CardTextCastSpell, CardActionResolutionEventKind.CardTextRefilledSpellbook
            }));
            Assert.That(facts[0].SpellCardInstanceId, Is.EqualTo(spell));
            Assert.That(facts[0].TargetHeroId, Is.EqualTo(Owner));
            Assert.That(facts[0].ActorId, Is.EqualTo(Other), "the acting hero stays the actor");
            Assert.That(facts[1].SpellCardInstanceId.IsDefined, Is.False);
            Assert.That(facts[1].SpellCount, Is.EqualTo(4));
            Assert.That(facts.Select(fact => fact.StepId.Value).Distinct().Count(), Is.EqualTo(2));

            Assert.That(EffectExecutionResult.DeclinedOptional().SpellbookChanges, Is.Empty);
            Assert.That(() => SpellbookChange.Refilled(Owner, 0), Throws.InstanceOf<ArgumentOutOfRangeException>(),
                "a refill reports what it returned; an empty one reports nothing");
            Assert.That(() => SpellbookChange.Cast(default, spell), Throws.ArgumentException);
        }

        // -------------------------------------------------------------------------------------------
        // The readings the spell choice's faces and captions are drawn from
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ACountingSpellsValues_AreReadFromItsOwnDeclarations_OverTheOtherSpellsRemoved()
        {
            var sunburst = GydionCardContent.Definition(GydionCardContent.Sunburst);
            Assert.That(SpellCountReading.Counts(sunburst), Is.True);
            Assert.That(SpellCountReading.CountedValues(sunburst, 5).Select(value => value.Key + " " + value.Value),
                Is.EqualTo(new[] { "Attack 5", "Range 5" }), "mockup A: 5 out, Attack 5, Range 5");
            Assert.That(SpellCountReading.CountedCoins(sunburst, 5), Is.Null);

            var door = GydionCardContent.Definition(GydionCardContent.DimensionDoor);
            Assert.That(SpellCountReading.CountedValues(door, 3).Select(value => value.Key + " " + value.Value),
                Is.EqualTo(new[] { "Radius 3" }));

            var midas = GydionCardContent.Definition(GydionCardContent.MidasTouch);
            Assert.That(SpellCountReading.Counts(midas), Is.True);
            Assert.That(SpellCountReading.CountedValues(midas, 5), Is.Empty);
            Assert.That(SpellCountReading.CountedCoins(midas, 5), Is.EqualTo(2), "one coin for every two, rounded down");

            var fireball = GydionCardContent.Definition(GydionCardContent.Fireball);
            Assert.That(SpellCountReading.Counts(fireball), Is.False);

            // The count is the faceup pile without the spell itself (lesson 27), read from public facts.
            var round = new GydionCountingSpellTests.GydionRound(
                GydionCountingSpellTests.GydionRound.CastingCard(GydionCardContent.Sunburst),
                removed: new[] { GydionCardContent.MagicMissile, GydionCardContent.Shield, GydionCardContent.Sunburst });
            Assert.That(SpellCountReading.OtherRemoved(
                round.Cards, GydionCountingSpellTests.GydionRound.Caster, GydionCardContent.Sunburst), Is.EqualTo(2));
            Assert.That(SpellCountReading.OtherRemoved(
                round.Cards, GydionCountingSpellTests.GydionRound.Caster, GydionCardContent.Fireball), Is.EqualTo(3));
        }

        [Test]
        public void ACastOrRemovalArm_NamesItsSpell_ByTheOneNamingStepRule()
        {
            foreach (var spell in GydionCardContent.SpellIds)
            {
                Assert.That(GydionCardContent.InBookV1(spell), Is.EqualTo(SyntheticSpellbookCapabilities.InBookV1(spell)),
                    spell.Value + ": Gydion's spells are named by the one rule");
            }

            var card = GydionCountingSpellTests.GydionRound.ChooseAndCastCard(
                GydionCardContent.BurningHands, GydionCardContent.Fireball, GydionCardContent.Sunburst);
            var effects = card.ActionDefinitions.Select(action => action.EffectRoot).ToArray();
            Assert.That(SyntheticSpellbookCapabilities.TryReadSpellArm(
                    effects, GydionCountingSpellTests.GydionRound.CastArm(GydionCardContent.Fireball),
                    GydionCardContent.SpellDefinitions, out var named, out var kind), Is.True);
            Assert.That(named.Id, Is.EqualTo(GydionCardContent.Fireball));
            Assert.That(kind, Is.EqualTo(SyntheticSpellbookCapabilities.SpellArmKind.Cast));

            var familiar = GydionCardContent.Definition(GydionCardContent.FindFamiliar);
            Assert.That(SyntheticSpellbookCapabilities.TryReadSpellArm(
                    familiar.ActionDefinitions.Select(action => action.EffectRoot),
                    GydionCardContent.FindFamiliarRemovalArm(GydionCardContent.Wish),
                    GydionCardContent.SpellDefinitions, out named, out kind), Is.True);
            Assert.That(named.Id, Is.EqualTo(GydionCardContent.Wish));
            Assert.That(kind, Is.EqualTo(SyntheticSpellbookCapabilities.SpellArmKind.Remove));

            Assert.That(SyntheticSpellbookCapabilities.TryReadSpellArm(
                    effects, new EffectNodeId("choose-and-cast-one"),
                    GydionCardContent.SpellDefinitions, out _, out _), Is.False,
                "the Choose itself names no spell");
        }
    }
}
