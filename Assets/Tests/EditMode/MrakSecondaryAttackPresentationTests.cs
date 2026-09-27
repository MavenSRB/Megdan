using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112a. Where a printed secondary Attack's hit surface lies on a prepared front,
    /// read from the typed region alone and checked against the ink measured on Mrak's own fronts
    /// (value digits on the shared 372x520 source): an ordinary two-badge rail's boot at
    /// y=210..238; a three-badge rail (Defense, Movement, Attack) with its boot at y=179..207 and
    /// swords at y=241..269; and a Defense-and-Attack rail over a Movement primary with its swords
    /// at y=206..223.
    /// </summary>
    public sealed class MrakSecondaryAttackPresentationTests
    {
        private static readonly HeroId Owner = new HeroId("mrak-presentation-owner");

        [Test]
        public void AThreeBadgeRailSplitsBootAndSwordsIntoTheirOwnInk()
        {
            var regions = Regions(ThreeBadge);
            var movement = DirectorHandsUnityView.PreparedActionSourceBounds(
                regions.Single(region => region.ActionKind == ActionKind.Movement));
            var attack = DirectorHandsUnityView.PreparedActionSourceBounds(
                regions.Single(region => region.ActionKind == ActionKind.Attack));

            AssertCoversInk(movement, 179f, 207f, "the boot's value on a three-badge rail");
            AssertCoversInk(attack, 241f, 269f, "the swords' value under it");
            Assert.That(movement.yMax, Is.LessThanOrEqualTo(attack.yMin),
                "The two hit surfaces never overlap, so a press answers exactly one action.");
            Assert.That(attack.xMax, Is.LessThan(372f * 0.25f), "Both stay on the left rail.");
        }

        [Test]
        public void ADefenseAndAttackRailPutsTheSwordsWhereTheirInkIs()
        {
            var attack = DirectorHandsUnityView.PreparedActionSourceBounds(
                Regions(DefenseAndAttack).Single(region =>
                    region.InvocationContext == ActionInvocationContext.PrintedSecondary));

            AssertCoversInk(attack, 206f, 223f, "the swords directly under the shield");
        }

        [Test]
        public void AnOrdinaryBootKeepsItsEstablishedCircle()
        {
            var movement = DirectorHandsUnityView.PreparedActionSourceBounds(
                Regions(OrdinaryBoot).Single(region =>
                    region.InvocationContext == ActionInvocationContext.PrintedSecondary));

            Assert.That(movement, Is.EqualTo(new Rect(5f, 184f, 80f, 82f)),
                "Every card printed before Mrak offers one secondary action, so none of them moves.");
            AssertCoversInk(movement, 210f, 238f, "an ordinary boot's value");
        }

        [Test]
        public void TheSecondaryAttackShinesItsOwnBadge()
        {
            var attack = Regions(ThreeBadge).Single(region => region.ActionKind == ActionKind.Attack);
            var movement = Regions(ThreeBadge).Single(region =>
                region.ActionKind == ActionKind.Movement &&
                region.InvocationContext == ActionInvocationContext.PrintedSecondary);

            Assert.That(DirectorHandsUnityView.ShineSlotFor(attack),
                Is.EqualTo(CardShineSlot.SecondaryAttack));
            Assert.That(DirectorHandsUnityView.ShineSlotFor(movement),
                Is.EqualTo(CardShineSlot.SecondaryMovement));
        }

        // -------------------------------------------------------------------------------------------

        private static void AssertCoversInk(Rect bounds, float inkTop, float inkBottom, string what)
        {
            Assert.That(bounds.yMin, Is.LessThanOrEqualTo(inkTop), what + " starts inside the box");
            Assert.That(bounds.yMax, Is.GreaterThanOrEqualTo(inkBottom), what + " ends inside the box");
            Assert.That(bounds.xMin, Is.LessThanOrEqualTo(21f));
            Assert.That(bounds.xMax, Is.GreaterThanOrEqualTo(69f));
        }

        /// <summary>The regions the round presenter builds, in the shape it builds them.</summary>
        private static IReadOnlyList<PrintedActionRegionViewModel> Regions(CardDefinition definition)
        {
            var card = CardInstance.CreateInHand(
                new CardInstanceId("mrak-presentation-" + definition.Id.Value),
                definition.Reference, Owner);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                new[] { definition }, new[] { card }, Owner, card.Id).ToArray();
            var secondaries = offers
                .Where(offer => offer.InvocationContext != ActionInvocationContext.PrintedPrimary)
                .ToArray();
            return offers.Select(offer =>
            {
                var action = definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId));
                var index = offer.InvocationContext == ActionInvocationContext.PrintedPrimary
                    ? -1
                    : System.Array.IndexOf(secondaries, offer);
                return new PrintedActionRegionViewModel(
                    offer, action.Kind, action.Kind.ToString().ToUpperInvariant(), index,
                    secondaries.Length, action.Kind + " - available", null);
            }).ToArray();
        }

        private static ActionDefinition Defense(int value) => new ActionDefinition(
            new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
            ActionDefinitionOrigin.Reaction,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
            AttackRangeKind.None,
            new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));

        private static ActionDefinition SecondaryMovement(int value) => new ActionDefinition(
            new ActionDefinitionId("secondary-movement"), ActionKind.Movement,
            ActionDefinitionOrigin.PrintedSecondary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));

        private static ActionDefinition MovementPrimary(int value) => new ActionDefinition(
            new ActionDefinitionId("primary-movement"), ActionKind.Movement,
            ActionDefinitionOrigin.PrintedPrimary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true));

        private static ActionDefinition AwaitedSkill() => new ActionDefinition(
            new ActionDefinitionId("primary-skill"), ActionKind.Skill,
            ActionDefinitionOrigin.PrintedPrimary,
            new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
            AttackRangeKind.None, default,
            textComposition: PrintedTextComposition.AwaitedAt("8.112c"));

        private static CardDefinition Card(string id, params ActionDefinition[] actions) =>
            new CardDefinition(
                new CardDefinitionId("mrak-presentation-" + id), "rev-1", "TEST - " + id,
                new InitiativeValue(7), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated, actions);

        /// <summary>Seismic Slam's rail: Defense 7, Movement 3, Attack 6.</summary>
        private static readonly CardDefinition ThreeBadge = Card("three-badge",
            AwaitedSkill(), SecondaryMovement(3), MrakCardContent.SecondaryAttack(6), Defense(7));

        /// <summary>Stone Carapace's rail: Defense 6, Attack 7, over a Movement 4 primary.</summary>
        private static readonly CardDefinition DefenseAndAttack = Card("defense-and-attack",
            MovementPrimary(4), MrakCardContent.SecondaryAttack(7), Defense(6));

        /// <summary>Every earlier hero's rail: Defense and Movement.</summary>
        private static readonly CardDefinition OrdinaryBoot = Card("ordinary-boot",
            AwaitedSkill(), SecondaryMovement(2), Defense(6));
    }
}
