using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ActionOfferAndPrintedValueTests
    {
        [Test]
        public void ValueContributions_KeepPrintedMinionItemCardTextAndActiveSourcesDistinct()
        {
            var cardId = new CardInstanceId("synthetic-value-card");
            var actionId = new ActionDefinitionId("synthetic-value-action");
            var contributions = new[]
            {
                new ValueContribution(ValueContributionKind.PrintedIcon, cardId, actionId,
                    PrintedValueChannel.Defense, 3),
                new ValueContribution(ValueContributionKind.MinionDefenseModifier,
                    new MinionId("synthetic-value-minion"), PrintedValueChannel.Defense, 1),
                new ValueContribution(ValueContributionKind.ItemModifier, cardId,
                    PrintedValueChannel.Defense, 1),
                new ValueContribution(ValueContributionKind.CardTextModifier, cardId, actionId,
                    PrintedValueChannel.Defense, 2),
                new ValueContribution(ValueContributionKind.ActiveEffectModifier,
                    new ActiveEffectId("synthetic-value-effect"), PrintedValueChannel.Defense, -1)
            };

            var result = new ValueResult(PrintedValueChannel.Defense, contributions);

            Assert.That(result.Contributions.Select(value => value.Kind), Is.EqualTo(new[]
            {
                ValueContributionKind.PrintedIcon,
                ValueContributionKind.MinionDefenseModifier,
                ValueContributionKind.ItemModifier,
                ValueContributionKind.CardTextModifier,
                ValueContributionKind.ActiveEffectModifier
            }));
            Assert.That(result.Contributions[1].SourceMinionId.Value, Is.EqualTo("synthetic-value-minion"));
            Assert.That(result.Contributions[2].SourceCardInstanceId, Is.EqualTo(cardId));
            Assert.That(result.Contributions[3].SourceActionDefinitionId, Is.EqualTo(actionId));
            Assert.That(result.Contributions[4].SourceActiveEffectId.Value, Is.EqualTo("synthetic-value-effect"));
            Assert.That(result.Total, Is.EqualTo(6));
        }

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
            Assert.That(value.Contributions[0].Kind, Is.EqualTo(ValueContributionKind.PrintedIcon));
            Assert.That(value.Contributions[0].SourceCardInstanceId, Is.EqualTo(instance.Id));
            Assert.That(value.Contributions[0].SourceActionDefinitionId, Is.EqualTo(new ActionDefinitionId("secondary-move")));
            Assert.That(value.Contributions[0].Amount, Is.EqualTo(1));
            Assert.That(value.Contributions, Is.Not.InstanceOf<ValueContribution[]>());
            Assert.Throws<NotSupportedException>(() =>
                ((IList<ValueContribution>)value.Contributions)[0] = value.Contributions[0]);
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

        // Relocated verbatim from CardDrivenMovementPresentationTests at milestone 6.1c. It was the
        // one test in that fixture that never touched the legacy HeroSelectionController drill, so
        // deleting the file wholesale would have dropped live Core coverage.
        [Test]
        public void PlaceholderHand_ContainsFiveVisibleTestCardsWithOnePrintedPrimaryAndSecondaryMovement()
        {
            var ownerId = new HeroId("hero");
            var hand = SyntheticShowcaseHand.Create(ownerId);
            var primaryKinds = new List<ActionKind>();

            Assert.That(hand.DealtDefinitions, Has.Count.EqualTo(5));
            Assert.That(hand.Instances, Has.Count.EqualTo(5));
            Assert.That(hand.DealtDefinitions, Has.All.Matches<CardDefinition>(definition =>
                definition.DisplayName.StartsWith("TEST - ")));
            Assert.That(hand.Instances.Select(instance => instance.OwnerId), Has.All.EqualTo(ownerId));

            foreach (var instance in hand.Instances)
            {
                var offers = ActionOfferQuery.GetAvailableOffers(
                        hand.Definitions,
                        hand.Instances,
                        ownerId,
                        ActionOfferWindow.NormalPlay)
                    .Where(offer => offer.SourceCardInstanceId.Equals(instance.Id))
                    .ToArray();
                var primary = offers.Single(offer =>
                    offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
                var secondary = offers.Single(offer =>
                    offer.InvocationContext == ActionInvocationContext.PrintedSecondary &&
                    hand.Definitions.Single(candidate =>
                            candidate.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action =>
                            action.Id.Equals(offer.ActionDefinitionId)).Kind == ActionKind.Movement);
                var definition = hand.Definitions.Single(candidate =>
                    candidate.Reference.Equals(instance.DefinitionReference));

                primaryKinds.Add(definition.ActionDefinitions.Single(action =>
                    action.Id.Equals(primary.ActionDefinitionId)).Kind);
                Assert.That(PrintedValueQuery.CalculatePrintedValue(
                    secondary,
                    hand.Definitions,
                    PrintedValueChannel.Movement).Total, Is.InRange(2, 3));
                Assert.That(definition.ActionDefinitions.Single(action =>
                        action.Id.Equals(primary.ActionDefinitionId)).DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Applies));
                Assert.That(definition.ActionDefinitions.Single(action =>
                        action.Id.Equals(secondary.ActionDefinitionId)).DefaultPolicy.CardTextApplication,
                    Is.EqualTo(CardTextApplicationPolicy.Suppressed));
            }

            // The showcase conversions replace one card's printed Primary Attack each - 6.8a Red
            // Bulwark, 6.5a Red Needle, 6.5b Red Bombard - so the Red hand no longer offers five
            // Attacks. Every card still offers exactly one printed Primary, which is what this
            // fixture pins.
            Assert.That(primaryKinds.Count(kind => kind == ActionKind.Skill), Is.EqualTo(3));
            Assert.That(primaryKinds.Count(kind => kind == ActionKind.Attack), Is.EqualTo(2));
        }

        [Test]
        public void PlaceholderDefinitions_DeriveOwnedNonRangedAndRangedAttackAndDefenseReactionOffers()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var redHeroId = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(StagedBoardParticipants.BlueHeroIdValue);

            var redNormalOffers = ActionOfferQuery.GetAvailableOffers(
                configuration.CardDefinitions,
                configuration.CardInstances,
                redHeroId,
                ActionOfferWindow.NormalPlay);
            var redAttack = redNormalOffers.Single(offer =>
                offer.SourceCardInstanceId.Value == "red-pursuit-card" &&
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
                Is.EqualTo(4));

            var redRangedAttack = redNormalOffers.Single(offer =>
                offer.SourceCardInstanceId.Value == "red-lancer-card" &&
                offer.ActionDefinitionId.Equals(new ActionDefinitionId("primary-ranged-attack")));
            Assert.That(redRangedAttack.ActorId, Is.EqualTo(redHeroId));
            Assert.That(redRangedAttack.SourceCardInstanceId.Value, Is.EqualTo("red-lancer-card"));
            Assert.That(redRangedAttack.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedPrimary));
            Assert.That(redRangedAttack.AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(
                PrintedValueQuery.CalculatePrintedValue(
                    redRangedAttack,
                    configuration.CardDefinitions,
                    PrintedValueChannel.Attack).Total,
                Is.EqualTo(2));
            Assert.That(
                PrintedValueQuery.CalculatePrintedValue(
                    redRangedAttack,
                    configuration.CardDefinitions,
                    PrintedValueChannel.Range).Total,
                Is.EqualTo(3));

            var blueDefenseOffers = ActionOfferQuery.GetAvailableOffers(
                configuration.CardDefinitions,
                configuration.CardInstances,
                blueHeroId,
                ActionOfferWindow.Reaction);

            Assert.That(blueDefenseOffers.Count, Is.EqualTo(5));
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
                Is.EquivalentTo(new[] { 2, 4, 1, 1, 2 }));
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
                new ActionDefinitionId("ranged-attack-without-range-value"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                PrimaryMovementPolicy(),
                AttackRangeKind.Ranged,
                default));
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("ranged-attack-with-zero-range"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                    new PrintedValueDefinition(PrintedValueChannel.Range, 0)
                },
                PrimaryMovementPolicy(),
                AttackRangeKind.Ranged,
                default));
            Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("non-ranged-attack-with-range-value"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                    new PrintedValueDefinition(PrintedValueChannel.Range, 1)
                },
                PrimaryMovementPolicy(),
                AttackRangeKind.NonRanged,
                default));
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
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
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
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
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
