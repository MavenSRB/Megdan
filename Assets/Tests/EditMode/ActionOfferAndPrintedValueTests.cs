using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ActionOfferAndPrintedValueTests
    {
        [Test]
        public void CardDefinition_OwnsOrderedImmutableActionDefinitions()
        {
            var primary = CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3);
            var secondary = CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1);

            var definition = CreateDefinition(primary, secondary);

            Assert.That(definition.ActionDefinitions.Select(action => action.Id), Is.EqualTo(new[]
            {
                primary.Id,
                secondary.Id
            }));
            Assert.That(definition.ActionDefinitions[0].Kind, Is.EqualTo(ActionKind.Movement));
            Assert.That(definition.ActionDefinitions[0].Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary));
            Assert.That(definition.ActionDefinitions[0].DefaultPolicy.CardTextApplication, Is.EqualTo(CardTextApplicationPolicy.Applies));
            Assert.That(definition.ActionDefinitions[0].DefaultPolicy.CanRepeat, Is.False);
            Assert.That(definition.ActionDefinitions[0].DefaultPolicy.CanBeReplaced, Is.True);
        }

        [Test]
        public void ActionOfferQuery_DerivesPrimaryAndSecondaryMovementOffersFromInHandCard()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);

            var offers = ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { instance },
                actorId);

            Assert.That(offers.Count, Is.EqualTo(2));
            Assert.That(offers.Select(offer => offer.SourceCardInstanceId), Is.All.EqualTo(instance.Id));
            Assert.That(offers.Select(offer => offer.SourceDefinitionReference), Is.All.EqualTo(definition.Reference));
            Assert.That(offers.Select(offer => offer.ActorId), Is.All.EqualTo(actorId));
            Assert.That(offers.Select(offer => offer.IsAvailable), Is.All.True);
            Assert.That(offers.Select(offer => offer.ActionDefinitionId), Is.EqualTo(new[]
            {
                new ActionDefinitionId("primary-move"),
                new ActionDefinitionId("secondary-move")
            }));
            Assert.That(offers.Select(offer => offer.InvocationContext), Is.EqualTo(new[]
            {
                ActionInvocationContext.PrintedPrimary,
                ActionInvocationContext.PrintedSecondary
            }));
        }

        [Test]
        public void ActionOfferQuery_DoesNotOfferOtherOwnersCards()
        {
            var actorId = new HeroId("primitive-hero");
            var otherHeroId = new HeroId("other-hero");
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var otherInstance = CardInstance.CreateInHand(
                new CardInstanceId("other-card"),
                definition.Reference,
                otherHeroId);

            var offers = ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { otherInstance },
                actorId);

            Assert.That(offers, Is.Empty);
        }

        [Test]
        public void PrintedValueQuery_ReturnsPrintedContributionAndTotalForChosenOffer()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);
            var offers = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId);
            var secondaryOffer = offers.Single(offer => offer.InvocationContext == ActionInvocationContext.PrintedSecondary);

            var value = PrintedValueQuery.CalculatePrintedValue(
                secondaryOffer,
                new[] { definition },
                PrintedValueChannel.Movement);

            Assert.That(value.Channel, Is.EqualTo(PrintedValueChannel.Movement));
            Assert.That(value.Total, Is.EqualTo(1));
            Assert.That(value.Contributions.Count, Is.EqualTo(1));
            Assert.That(value.Contributions[0].Kind, Is.EqualTo(PrintedValueContributionKind.PrintedIcon));
            Assert.That(value.Contributions[0].SourceCardInstanceId, Is.EqualTo(instance.Id));
            Assert.That(value.Contributions[0].SourceActionDefinitionId, Is.EqualTo(new ActionDefinitionId("secondary-move")));
            Assert.That(value.Contributions[0].Amount, Is.EqualTo(1));
        }

        [Test]
        public void PrintedValueQuery_UsesChosenOfferInsteadOfCardWideValue()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3),
                CreateMovementAction("secondary-move", ActionDefinitionOrigin.PrintedSecondary, 1));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);
            var offers = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId);
            var primaryOffer = offers.Single(offer => offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            var secondaryOffer = offers.Single(offer => offer.InvocationContext == ActionInvocationContext.PrintedSecondary);

            var primaryValue = PrintedValueQuery.CalculatePrintedValue(primaryOffer, new[] { definition }, PrintedValueChannel.Movement);
            var secondaryValue = PrintedValueQuery.CalculatePrintedValue(secondaryOffer, new[] { definition }, PrintedValueChannel.Movement);

            Assert.That(primaryValue.Total, Is.EqualTo(3));
            Assert.That(secondaryValue.Total, Is.EqualTo(1));
        }

        [Test]
        public void InvocationContextVocabulary_CoversFutureOfferContextsWithoutResolvingThem()
        {
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.PrintedPrimary), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.PrintedSecondary), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.GeneralRuleSecondary), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.Replacement), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.Reaction), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.Repeated), Is.True);
            Assert.That(Enum.IsDefined(typeof(ActionInvocationContext), ActionInvocationContext.TextGranted), Is.True);
        }

        [Test]
        public void CardDefinition_WithDuplicateActionDefinitionIds_IsRejected()
        {
            var first = CreateMovementAction("move", ActionDefinitionOrigin.PrintedPrimary, 3);
            var duplicate = CreateMovementAction("move", ActionDefinitionOrigin.PrintedSecondary, 1);

            Assert.Throws<ArgumentException>(() => CreateDefinition(first, duplicate));
        }

        [Test]
        public void ActionDefinition_WithUnsupportedMovementValue_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("bad-move"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                PrimaryMovementPolicy()));
        }

        [Test]
        public void ActionDefinition_WithInvalidOriginPolicyCombination_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("bad-primary"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)));

            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("bad-repeat"),
                ActionKind.Movement,
                ActionDefinitionOrigin.Repeated,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, true, false)));
        }

        [Test]
        public void ActionOfferQuery_WithDuplicateDefinitionIds_IsRejected()
        {
            var actorId = new HeroId("primitive-hero");
            var firstDefinition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var secondDefinition = new CardDefinition(
                firstDefinition.Id,
                "rev-2",
                "Another Placeholder Move",
                new[] { CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3) });
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                firstDefinition.Reference,
                actorId);

            Assert.Throws<ArgumentException>(() => ActionOfferQuery.GetAvailableOffers(
                new[] { firstDefinition, secondDefinition },
                new[] { instance },
                actorId));
        }

        [Test]
        public void PrintedValueQuery_WithUnresolvedActionOfferReference_IsRejected()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(CreateMovementAction("primary-move", ActionDefinitionOrigin.PrintedPrimary, 3));
            var offer = new ActionOffer(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                new ActionDefinitionId("missing-action"),
                actorId,
                ActionInvocationContext.PrintedPrimary,
                true);

            Assert.Throws<ArgumentException>(() => PrintedValueQuery.CalculatePrintedValue(
                offer,
                new[] { definition },
                PrintedValueChannel.Movement));
        }

        private static CardDefinition CreateDefinition(params ActionDefinition[] actionDefinitions)
        {
            return new CardDefinition(
                new CardDefinitionId("placeholder-movement-card"),
                "rev-1",
                "Placeholder Movement Card",
                actionDefinitions);
        }

        private static ActionDefinition CreateMovementAction(
            string id,
            ActionDefinitionOrigin origin,
            int movementValue)
        {
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Movement,
                origin,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movementValue) },
                origin == ActionDefinitionOrigin.PrintedPrimary
                    ? PrimaryMovementPolicy()
                    : SecondaryMovementPolicy());
        }

        private static ActionDefinitionPolicy PrimaryMovementPolicy()
        {
            return new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true);
        }

        private static ActionDefinitionPolicy SecondaryMovementPolicy()
        {
            return new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true);
        }
    }
}
