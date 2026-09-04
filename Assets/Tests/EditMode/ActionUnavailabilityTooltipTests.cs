using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35s (the SIXTH amendment's user-approved cursor-tooltip design): "NOTHING IS EVER
    /// GREYED WITHOUT SAYING WHY." Presentation composes a greyed control's hover reason from a fixed
    /// short vocabulary (<see cref="ActionUnavailabilityLabels"/>), mapped from the authority's own
    /// answer and never re-derived here.
    /// </summary>
    public sealed class ActionUnavailabilityTooltipTests
    {
        /// <summary>Milestone 8.35l's fixture shape, reused rather than re-invented: Sabina inside
        /// Magnetic Dagger's Radius 3 disk with both of her neighbouring minions also pinned, so every
        /// partner Back to Back could name is barred and the whole Skill is unavailable.</summary>
        [Test]
        public void BlockedByRuleModification_NamesTheBlockingHeroAndCard_UsingTheHudsOwnDisplayNames()
        {
            var board = SabinaBoard.Create(
                clearHerNeighbourhood: false,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-02"] = StagedHeroBoard.SecondAdjacent,
                    ["blue-ranged-01"] = StagedHeroBoard.ThirdAdjacent
                });

            var offer = board.PrimaryOffer(
                SabinaCardContent.BackToBack, WaspCardContent.MagneticDagger, letTheEnemyAct: true);
            Assert.That(offer.Unavailability.Kind,
                Is.EqualTo(ActionUnavailabilityKind.BlockedByRuleModification),
                "Fixture precondition, proven at Core level by " +
                "SwapAndPlaceRestrictionFeasibilityTests.BackToBackWithTheActorHerselfPinned_...");

            var projection = board.Session.Projection;
            var model = RoundActionPresenter.CreateViewModel(projection);
            var region = model.PrintedActionRegions.Single(value =>
                value.ActionDefinitionId.Equals(offer.ActionDefinitionId));

            var expectedHero = projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(offer.Unavailability.BlockingHeroId))
                .DisplayName.ToUpperInvariant();
            var blockingCard = projection.Cards.Single(card =>
                card.Id.Equals(offer.Unavailability.BlockingCardInstanceId));
            var expectedCard = projection.Definitions
                .Single(definition => definition.Reference.Equals(blockingCard.DefinitionReference))
                .DisplayName.ToUpperInvariant();

            Assert.That(region.AssistiveLabel,
                Does.Contain($"BLOCKED BY {expectedHero} — {expectedCard}"),
                "Presentation prints the authority's own answer using the exact display names the " +
                "rest of the HUD already draws - the hero rail's name and the card catalog's name - " +
                "never a card or hero identity re-derived here.");
            Assert.That(region.AssistiveLabel, Does.Not.Contain(ActionUnavailabilityLabels.Generic),
                "A stated reason replaces the generic word rather than sitting beside it.");
        }

        /// <summary>
        /// Milestone 8.35l's OTHER fixture shape: Sabina outside the disk with one partner barred and
        /// one free. The action stands (offer.IsAvailable), so presentation carries no reason to
        /// attach here at all - <c>ActionOffer.Unavailability</c> is <c>None</c> on every available
        /// offer by construction. This is the board half of the SIXTH amendment's design: the barred
        /// partner is absent from the candidate list rather than drawn with an invented ring or an
        /// invented reason, because milestone 8.35l's own narrowing is what makes it absent and no
        /// per-candidate reason exists to invent one from.
        /// </summary>
        [Test]
        public void ANarrowedEffectChoice_NamesOnlyTheFreeCandidate_AndInventsNoRingOrReasonForTheBarredOne()
        {
            var board = SabinaBoard.Create(
                clearHerNeighbourhood: false,
                heroPlacements: StagedHeroBoard.At("blue-test-hero", new HexCoordinate(-1, -3)),
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["blue-melee-02"] = new HexCoordinate(-2, -1),
                    ["blue-ranged-01"] = new HexCoordinate(-1, -5)
                });

            var offer = board.PrimaryOffer(
                SabinaCardContent.BackToBack, WaspCardContent.MagneticDagger, letTheEnemyAct: true);
            Assert.That(offer.IsAvailable, Is.True,
                "Fixture precondition: one partner is out of the disk, so a legal continuation exists.");
            Assert.That(offer.Unavailability.IsStated, Is.False,
                "An available offer never carries a reason - there is nothing here for a control-level " +
                "tooltip to say, which is exactly why the board case must be proven at the candidate " +
                "list instead.");
            board.StartOffer(offer);

            var model = RoundActionPresenter.CreateViewModel(board.Session.Projection);

            Assert.That(model.EffectChoiceTargets.Select(target => target.Space),
                Is.EqualTo(new[] { new HexCoordinate(-1, -5) }),
                "Only the free minion's space is a target the board can answer with - the pinned " +
                "partner's space is absent, exactly as milestone 8.35l's own candidate narrowing " +
                "requires, not merely drawn and then refused.");
        }

        [Test]
        public void Generic_IsTheFixedWordAWordlessGreyReads()
        {
            Assert.That(ActionUnavailabilityLabels.Generic, Is.EqualTo("UNAVAILABLE"));
        }

        [Test]
        public void NoTarget_IsTheFixedWordClearsSoleCauseReads()
        {
            Assert.That(ActionUnavailabilityLabels.NoTarget, Is.EqualTo("NO TARGET"));
        }

        [Test]
        public void BlockedBy_ComposesTheHeroAndCardIntoOneReason()
        {
            Assert.That(
                ActionUnavailabilityLabels.BlockedBy("WASP", "MAGNETIC DAGGER"),
                Is.EqualTo("BLOCKED BY WASP — MAGNETIC DAGGER"));
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void BlockedBy_RequiresABlockingHeroName(string blankName)
        {
            Assert.That(
                () => ActionUnavailabilityLabels.BlockedBy(blankName, "MAGNETIC DAGGER"),
                Throws.ArgumentException);
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void BlockedBy_RequiresABlockingCardName(string blankName)
        {
            Assert.That(
                () => ActionUnavailabilityLabels.BlockedBy("WASP", blankName),
                Throws.ArgumentException);
        }
    }
}
