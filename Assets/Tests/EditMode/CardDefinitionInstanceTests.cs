using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class CardDefinitionInstanceTests
    {
        [Test]
        public void CardDefinition_StoresStableIdentityRevisionAndDisplayName()
        {
            var definitionId = new CardDefinitionId("placeholder-move");

            var definition = new CardDefinition(definitionId, "rev-1", "Placeholder Move");

            Assert.That(definition.Id, Is.EqualTo(definitionId));
            Assert.That(definition.ContentRevision, Is.EqualTo("rev-1"));
            Assert.That(definition.DisplayName, Is.EqualTo("Placeholder Move"));
            Assert.That(definition.Reference, Is.EqualTo(new CardDefinitionReference(definitionId, "rev-1")));
        }

        [Test]
        public void CardDefinition_WithBlankIdentityRevisionOrName_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new CardDefinitionId(" "));
            Assert.Throws<ArgumentException>(() => new CardDefinition(new CardDefinitionId("card"), " ", "Name"));
            Assert.Throws<ArgumentException>(() => new CardDefinition(new CardDefinitionId("card"), "rev-1", " "));
        }

        [Test]
        public void CardInstance_ReferencesDefinitionAndOwnerWithInitialInHandState()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");
            var instanceId = new CardInstanceId("primitive-hero-card-1");

            var instance = CardInstance.CreateInHand(instanceId, definition.Reference, ownerId);

            Assert.That(instance.Id, Is.EqualTo(instanceId));
            Assert.That(instance.DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(instance.OwnerId, Is.EqualTo(ownerId));
            Assert.That(instance.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(instance.State.HasBeenPlayed, Is.False);
            Assert.That(instance.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.None));
            Assert.That(instance.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
        }

        [Test]
        public void CardInstance_WithBlankInstanceDefinitionOrOwnerIds_IsRejected()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");

            Assert.Throws<ArgumentException>(() => new CardInstanceId(" "));
            Assert.Throws<ArgumentException>(() => CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                default,
                ownerId));
            Assert.Throws<ArgumentException>(() => CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                definition.Reference,
                default));
        }

        [Test]
        public void TwoInstances_MayReferenceOneImmutableDefinition()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");

            var firstInstance = CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                definition.Reference,
                ownerId);
            var secondInstance = CardInstance.CreateInHand(
                new CardInstanceId("copy-2"),
                definition.Reference,
                ownerId);

            Assert.That(firstInstance.Id, Is.Not.EqualTo(secondInstance.Id));
            Assert.That(firstInstance.DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(secondInstance.DefinitionReference, Is.EqualTo(definition.Reference));
            Assert.That(definition.ContentRevision, Is.EqualTo("rev-1"));
            Assert.That(definition.DisplayName, Is.EqualTo("Placeholder Move"));
        }

        [Test]
        public void CardStateFacts_AreOrthogonalLifecycleFacts()
        {
            var resolvedFacedownDiscarded = new CardStateFacts(
                CardLocation.DiscardPile,
                true,
                CardResolutionStatus.Resolved,
                CardFaceOrientation.FaceDown);

            Assert.That(resolvedFacedownDiscarded.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(resolvedFacedownDiscarded.HasBeenPlayed, Is.True);
            Assert.That(resolvedFacedownDiscarded.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(resolvedFacedownDiscarded.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));

            var unplayedDiscarded = CardStateFacts.DiscardedUnplayed();
            Assert.That(unplayedDiscarded.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(unplayedDiscarded.HasBeenPlayed, Is.False);
            Assert.That(unplayedDiscarded.ResolutionStatus, Is.EqualTo(CardResolutionStatus.None));
            Assert.That(unplayedDiscarded.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
        }

        [Test]
        public void CardStateFacts_WithResolutionButNotPlayed_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new CardStateFacts(
                CardLocation.DiscardPile,
                false,
                CardResolutionStatus.Resolved,
                CardFaceOrientation.FaceUp));
        }

        [Test]
        public void InitialSetupValidator_AcceptsUniqueDefinitionsAndInHandInstances()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");
            var firstInstance = CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                definition.Reference,
                ownerId);
            var secondInstance = CardInstance.CreateInHand(
                new CardInstanceId("copy-2"),
                definition.Reference,
                ownerId);

            Assert.DoesNotThrow(() => CardInitialSetupValidator.Validate(
                new[] { definition },
                new[] { firstInstance, secondInstance }));
        }

        [Test]
        public void InitialSetupValidator_RejectsDuplicateDefinitionIds()
        {
            var firstDefinition = CreateDefinition();
            var secondDefinition = new CardDefinition(firstDefinition.Id, "rev-2", "Another Placeholder");

            Assert.Throws<ArgumentException>(() => CardInitialSetupValidator.Validate(
                new[] { firstDefinition, secondDefinition },
                Array.Empty<CardInstance>()));
        }

        [Test]
        public void InitialSetupValidator_RejectsDuplicateInstanceIds()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");
            var firstInstance = CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                definition.Reference,
                ownerId);
            var duplicateInstance = CardInstance.CreateInHand(
                firstInstance.Id,
                definition.Reference,
                ownerId);

            Assert.Throws<ArgumentException>(() => CardInitialSetupValidator.Validate(
                new[] { definition },
                new[] { firstInstance, duplicateInstance }));
        }

        [Test]
        public void InitialSetupValidator_RejectsUnresolvedDefinitionReferences()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");
            var instance = CardInstance.CreateInHand(
                new CardInstanceId("copy-1"),
                new CardDefinitionReference(definition.Id, "stale-rev"),
                ownerId);

            Assert.Throws<ArgumentException>(() => CardInitialSetupValidator.Validate(
                new[] { definition },
                new[] { instance }));
        }

        [Test]
        public void InitialSetupValidator_RejectsInvalidInitialRuntimeState()
        {
            var definition = CreateDefinition();
            var ownerId = new HeroId("primitive-hero");
            var invalidState = new CardStateFacts(
                CardLocation.TurnArea,
                true,
                CardResolutionStatus.Unresolved,
                CardFaceOrientation.FaceUp);
            var instance = new CardInstance(
                new CardInstanceId("copy-1"),
                definition.Reference,
                ownerId,
                invalidState);

            Assert.Throws<ArgumentException>(() => CardInitialSetupValidator.Validate(
                new[] { definition },
                new[] { instance }));
        }

        private static CardDefinition CreateDefinition()
        {
            return new CardDefinition(
                new CardDefinitionId("placeholder-move"),
                "rev-1",
                "Placeholder Move");
        }
    }
}
