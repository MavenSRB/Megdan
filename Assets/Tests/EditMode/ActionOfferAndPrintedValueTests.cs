using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
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
            Assert.That(definition.ActionDefinitions, Is.Not.InstanceOf<ActionDefinition[]>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ActionDefinition>)definition.ActionDefinitions)[0] = secondary);
            Assert.That(primary.BaseValues, Is.Not.InstanceOf<PrintedValueDefinition[]>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<PrintedValueDefinition>)primary.BaseValues)[0] =
                    new PrintedValueDefinition(PrintedValueChannel.Movement, 99));
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
        public void ActionOfferQuery_OnlyEmitsDefinitionsForTheRequestedTimingWindow()
        {
            var actorId = new HeroId("primitive-hero");
            var definition = CreateDefinition(
                CreateAction("primary", ActionDefinitionOrigin.PrintedPrimary, CardTextApplicationPolicy.Applies, true),
                CreateAction("secondary", ActionDefinitionOrigin.PrintedSecondary, CardTextApplicationPolicy.Suppressed, true),
                CreateAction("general", ActionDefinitionOrigin.GeneralRuleSecondary, CardTextApplicationPolicy.Suppressed, true),
                CreateAction("replacement", ActionDefinitionOrigin.Replacement, CardTextApplicationPolicy.Suppressed, false),
                CreateAction("reaction", ActionDefinitionOrigin.Reaction, CardTextApplicationPolicy.Applies, false),
                CreateAction("text-granted", ActionDefinitionOrigin.TextGranted, CardTextApplicationPolicy.Applies, false));
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("primitive-card-1"),
                definition.Reference,
                actorId);

            var normal = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId);
            var general = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId, ActionOfferWindow.GeneralRule);
            var replacement = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId, ActionOfferWindow.Replacement);
            var reaction = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId, ActionOfferWindow.Reaction);
            var textGranted = ActionOfferQuery.GetAvailableOffers(new[] { definition }, new[] { instance }, actorId, ActionOfferWindow.TextGranted);

            Assert.That(normal.Select(offer => offer.InvocationContext), Is.EqualTo(new[]
            {
                ActionInvocationContext.PrintedPrimary,
                ActionInvocationContext.PrintedSecondary
            }));
            Assert.That(general.Single().InvocationContext, Is.EqualTo(ActionInvocationContext.GeneralRuleSecondary));
            Assert.That(replacement.Single().InvocationContext, Is.EqualTo(ActionInvocationContext.Replacement));
            Assert.That(reaction.Single().InvocationContext, Is.EqualTo(ActionInvocationContext.Reaction));
            Assert.That(textGranted.Single().InvocationContext, Is.EqualTo(ActionInvocationContext.TextGranted));
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
            Assert.That(value.Contributions, Is.Not.InstanceOf<PrintedValueContribution[]>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<PrintedValueContribution>)value.Contributions)[0] = value.Contributions[0]);
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
        public void PlaceholderDefinitions_DeriveOwnedNonRangedAttackAndDefenseReactionOffers()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

            var redNormalOffers = ActionOfferQuery.GetAvailableOffers(
                configuration.CardDefinitions,
                configuration.CardInstances,
                redHeroId,
                ActionOfferWindow.NormalPlay);
            var redAttack = redNormalOffers.Single(offer =>
                offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-attack")));

            Assert.That(redAttack.ActorId, Is.EqualTo(redHeroId));
            Assert.That(redAttack.SourceCardInstanceId.Value, Does.StartWith("red-"));
            Assert.That(redAttack.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedPrimary));
            Assert.That(redAttack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged));
            Assert.That(redAttack.SourceCardStateTransition.IsDefined, Is.False);
            Assert.That(
                PrintedValueQuery.CalculatePrintedValue(
                    redAttack,
                    configuration.CardDefinitions,
                    PrintedValueChannel.Attack).Total,
                Is.EqualTo(3));

            var blueDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                configuration.CardDefinitions,
                configuration.CardInstances,
                blueHeroId,
                ActionOfferWindow.Reaction);

            Assert.That(blueDefenseOffers.Count, Is.EqualTo(2));
            Assert.That(blueDefenseOffers.Select(offer => offer.ActorId), Is.All.EqualTo(blueHeroId));
            Assert.That(blueDefenseOffers.Select(offer => offer.SourceCardInstanceId.Value),
                Is.All.StartsWith("blue-"));
            Assert.That(blueDefenseOffers.Select(offer => offer.InvocationContext),
                Is.All.EqualTo(ActionInvocationContext.Reaction));
            Assert.That(blueDefenseOffers.Select(offer => offer.SourceCardStateTransition.RequiredLocation),
                Is.All.EqualTo(CardLocation.Hand));
            Assert.That(blueDefenseOffers.Select(offer => offer.SourceCardStateTransition.DestinationLocation),
                Is.All.EqualTo(CardLocation.DiscardPile));
            Assert.That(blueDefenseOffers.Select(offer =>
                    PrintedValueQuery.CalculatePrintedValue(
                        offer,
                        configuration.CardDefinitions,
                        PrintedValueChannel.Defense).Total),
                Is.EquivalentTo(new[] { 2, 3 }));
        }

        [Test]
        public void DefenseReaction_IsNotANormalActionAndRejectsDiscardedOrCrossOwnerCards()
        {
            var actorId = new HeroId("defending-hero");
            var otherHeroId = new HeroId("other-hero");
            var definition = CreateDefinition(CreateDefenseAction("defense", 2));
            var actorCard = CardInstance.CreateInHand(
                new CardInstanceId("defender-card"),
                definition.Reference,
                actorId);
            var otherCard = CardInstance.CreateInHand(
                new CardInstanceId("other-card"),
                definition.Reference,
                otherHeroId);
            var discardedActorCard = new CardInstance(
                new CardInstanceId("discarded-defender-card"),
                definition.Reference,
                actorId,
                CardStateFacts.DiscardedUnplayed());

            Assert.That(ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { actorCard, otherCard, discardedActorCard },
                actorId,
                ActionOfferWindow.NormalPlay), Is.Empty);

            var reactions = ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { actorCard, otherCard, discardedActorCard },
                actorId,
                ActionOfferWindow.Reaction);

            Assert.That(reactions.Select(offer => offer.SourceCardInstanceId),
                Is.EqualTo(new[] { actorCard.Id }));
            Assert.That(discardedActorCard.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(discardedActorCard.State.HasBeenPlayed, Is.False);
        }

        [Test]
        public void AttackAndDefenseDefinitions_RejectInvalidValuesRangeOriginAndTransition()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrintedValueDefinition(PrintedValueChannel.Attack, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new PrintedValueDefinition(PrintedValueChannel.Defense, -1));

            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("attack-with-defense-value"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                PrimaryMovementPolicy(),
                AttackRangeKind.NonRanged,
                default));
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("attack-without-range"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                PrimaryMovementPolicy()));
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("normal-defense"),
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                SecondaryMovementPolicy(),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("defense-without-discard"),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                default));
        }

        [Test]
        public void ActionOfferQuery_RejectsMissingOwnerAndReturnsNoCrossOwnerOffer()
        {
            var definition = CreateDefinition(CreateDefenseAction("defense", 2));
            var ownerId = new HeroId("owner");
            var card = CardInstance.CreateInHand(new CardInstanceId("card"), definition.Reference, ownerId);

            Assert.Throws<ArgumentException>(() => ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { card },
                default,
                ActionOfferWindow.Reaction));
            Assert.Throws<ArgumentException>(() => new CardInstance(
                new CardInstanceId("missing-owner-card"),
                definition.Reference,
                default,
                CardStateFacts.InitialInHand()));
            Assert.That(ActionOfferQuery.GetAvailableOffers(
                new[] { definition },
                new[] { card },
                new HeroId("different-owner"),
                ActionOfferWindow.Reaction), Is.Empty);
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
                new ActionDefinitionId("bad-secondary"),
                ActionKind.Movement,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)));

            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("missing-policy"),
                ActionKind.Hold,
                ActionDefinitionOrigin.TextGranted,
                Array.Empty<PrintedValueDefinition>(),
                default));

            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("invalid-kind"),
                (ActionKind)999,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                PrimaryMovementPolicy()));

            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("invalid-origin"),
                ActionKind.Hold,
                (ActionDefinitionOrigin)999,
                Array.Empty<PrintedValueDefinition>(),
                PrimaryMovementPolicy()));

            Assert.That(Enum.IsDefined(typeof(ActionDefinitionOrigin), 6), Is.False,
                "Repeated actions are invocations of an existing definition, not static definition origins.");
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

        private static ActionDefinition CreateAction(
            string id,
            ActionDefinitionOrigin origin,
            CardTextApplicationPolicy cardTextApplication,
            bool canBeReplaced)
        {
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Hold,
                origin,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(cardTextApplication, false, canBeReplaced));
        }

        private static ActionDefinition CreateDefenseAction(string id, int defenseValue)
        {
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defenseValue) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
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
