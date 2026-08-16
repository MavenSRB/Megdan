using System;
using System.Linq;
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

            var definition = new CardDefinition(
                definitionId,
                "rev-1",
                "Placeholder Move",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture);

            Assert.That(definition.Id, Is.EqualTo(definitionId));
            Assert.That(definition.ContentRevision, Is.EqualTo("rev-1"));
            Assert.That(definition.DisplayName, Is.EqualTo("Placeholder Move"));
            Assert.That(definition.Initiative, Is.EqualTo(new InitiativeValue(5)));
            Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(definition.Reference, Is.EqualTo(new CardDefinitionReference(definitionId, "rev-1")));
        }

        [Test]
        public void CardDefinition_WithBlankIdentityRevisionOrName_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => new CardDefinitionId(" "));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("card"),
                " ",
                "Name",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("card"),
                "rev-1",
                " ",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("card"),
                "rev-1",
                "Name",
                default,
                CardContentAuthority.SyntheticTestFixture));
            Assert.Throws<ArgumentException>(() => new CardDefinition(
                new CardDefinitionId("card"),
                "rev-1",
                "Name",
                new InitiativeValue(5),
                CardContentAuthority.None));
        }

        [Test]
        public void InitiativeValue_MustBePositiveAndSupportsOrderAndTies()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new InitiativeValue(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InitiativeValue(-1));

            var lower = new InitiativeValue(4);
            var higher = new InitiativeValue(7);
            var tied = new InitiativeValue(7);

            Assert.That(higher.CompareTo(lower), Is.GreaterThan(0));
            Assert.That(higher, Is.EqualTo(tied));
        }

        [Test]
        public void SyntheticHands_ProvideUnequalInitiativesAndCrossHeroTieFixtures()
        {
            var redHand = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"));
            var blueHand = PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"));

            Assert.That(redHand.Definitions.Select(definition => definition.Initiative.Value),
                Is.EqualTo(new[] { 13, 11, 9, 7, 5 }));
            Assert.That(blueHand.Definitions.Select(definition => definition.Initiative.Value),
                Is.EqualTo(new[] { 12, 11, 8, 6, 4 }));
            Assert.That(redHand.Definitions[1].Initiative, Is.EqualTo(blueHand.Definitions[1].Initiative));
            Assert.That(redHand.Definitions, Has.All.Property("ContentAuthority")
                .EqualTo(CardContentAuthority.SyntheticTestFixture));
            Assert.That(blueHand.Definitions, Has.All.Property("ContentAuthority")
                .EqualTo(CardContentAuthority.SyntheticTestFixture));
            // Each showcase conversion advances exactly one card - 6.8a Red Bulwark, 6.8b Blue Feint,
            // 6.5b Red Bombard - and the seven still at the 5.1a revision is what proves nothing else
            // moved with it. Milestone 6.8l is the first non-conversion to advance a card revision,
            // and it advances exactly the two whose Skill opens a target selection: what changed is
            // the declaration those selections carry, not what either card does.
            Assert.That(
                redHand.Definitions.Select(definition => definition.ContentRevision),
                Is.EqualTo(new[]
                {
                    "phase-6-8l-rev-1",
                    "phase-6-8a-rev-1",
                    "phase-5-1a-rev-3",
                    "phase-5-1a-rev-3",
                    "phase-6-8l-rev-1"
                }));
            Assert.That(
                blueHand.Definitions.Select(definition => definition.ContentRevision),
                Is.EqualTo(new[]
                {
                    "phase-6-8b-rev-1",
                    "phase-5-1a-rev-3",
                    "phase-5-1a-rev-3",
                    "phase-5-1a-rev-3",
                    "phase-5-1a-rev-3"
                }));
            Assert.That(redHand.Definitions, Has.All.Property("DisplayName").StartsWith("TEST - "));
            Assert.That(blueHand.Definitions, Has.All.Property("DisplayName").StartsWith("TEST - "));
        }

        [Test]
        public void SyntheticHands_MatchApprovedFiveCardValueAndActionMatrix()
        {
            var redHand = PlaceholderMovementHand.CreateRed(new HeroId("red-synthetic-hero"));
            var blueHand = PlaceholderMovementHand.CreateBlue(new HeroId("blue-synthetic-hero"));
            AssertSyntheticMatrix(
                redHand,
                new[] { "Red Needle", "Red Bulwark", "Red Lancer", "Red Pursuit", "Red Bombard" },
                new[] { 13, 11, 9, 7, 5 },
                // Red Needle's, Red Bulwark's and Red Bombard's printed Primary Attacks became
                // milestones 6.5a's, 6.8a's and 6.5b's showcase Skills; the attack and range columns
                // say nothing for a Skill, which prints no value channel. Red Pursuit remains the
                // team's one printed non-Ranged Attack and Red Lancer its one Ranged Attack, which
                // the showcase schedule requires.
                new[]
                {
                    ActionKind.Skill, ActionKind.Skill, ActionKind.Attack,
                    ActionKind.Attack, ActionKind.Skill
                },
                new[] { 0, 0, 2, 4, 0 },
                new[] { 0, 0, 3, 0, 0 },
                new[] { 2, 2, 3, 3, 2 },
                new[] { 1, 3, 1, 2, 2 });
            AssertSyntheticMatrix(
                blueHand,
                new[] { "Blue Feint", "Blue Sentinel", "Blue Scout Shot", "Blue Hammer", "Blue Longbow" },
                new[] { 12, 11, 8, 6, 4 },
                // Blue Feint's printed Primary Attack became milestone 6.8b's showcase Skill; the
                // attack and range columns say nothing for a Skill, which prints no value channel.
                new[]
                {
                    ActionKind.Skill, ActionKind.Attack, ActionKind.Attack,
                    ActionKind.Attack, ActionKind.Attack
                },
                new[] { 0, 3, 2, 5, 3 },
                new[] { 0, 0, 2, 0, 3 },
                new[] { 3, 2, 4, 2, 3 },
                new[] { 2, 4, 1, 1, 2 });
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
            var secondDefinition = new CardDefinition(
                firstDefinition.Id,
                "rev-2",
                "Another Placeholder",
                new InitiativeValue(6),
                CardContentAuthority.SyntheticTestFixture);

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
                "Placeholder Move",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture);
        }

        private static void AssertSyntheticMatrix(
            PlaceholderMovementHand hand,
            string[] names,
            int[] initiatives,
            ActionKind[] primaryKinds,
            int[] attacks,
            int[] ranges,
            int[] movements,
            int[] defenses)
        {
            Assert.That(hand.Definitions, Has.Count.EqualTo(5));
            Assert.That(hand.Instances, Has.Count.EqualTo(5));
            for (var index = 0; index < hand.Definitions.Count; index++)
            {
                var definition = hand.Definitions[index];
                Assert.That(definition.DisplayName, Is.EqualTo($"TEST - {names[index]}"));
                Assert.That(definition.Initiative.Value, Is.EqualTo(initiatives[index]));
                Assert.That(definition.ContentAuthority, Is.EqualTo(CardContentAuthority.SyntheticTestFixture));
                Assert.That(definition.ActionDefinitions, Has.Count.EqualTo(3));
                Assert.That(definition.ActionDefinitions.Count(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == primaryKinds[index]), Is.EqualTo(1));
                Assert.That(definition.ActionDefinitions.Count(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary && action.Kind == ActionKind.Movement), Is.EqualTo(1));
                Assert.That(definition.ActionDefinitions.Count(action =>
                    action.Origin == ActionDefinitionOrigin.Reaction && action.Kind == ActionKind.Defense), Is.EqualTo(1));
                Assert.That(definition.ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == ActionKind.Movement), Is.False);

                var movement = definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement);
                var defense = definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense);
                if (primaryKinds[index] == ActionKind.Attack)
                {
                    var attack = definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
                    Assert.That(attack.BaseValues.Single(value => value.Channel == PrintedValueChannel.Attack).Amount,
                        Is.EqualTo(attacks[index]));
                    Assert.That(attack.AttackRange,
                        Is.EqualTo(ranges[index] > 0 ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged));
                    Assert.That(attack.BaseValues.Where(value => value.Channel == PrintedValueChannel.Range)
                        .Select(value => value.Amount).SingleOrDefault(), Is.EqualTo(ranges[index]));
                }
                else
                {
                    var converted = definition.ActionDefinitions.Single(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                    Assert.That(definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Attack), Is.False,
                        "A converted card's printed Primary Attack is replaced, not kept alongside its Skill.");
                    Assert.That(converted.BaseValues, Is.Empty,
                        "A Skill prints no value channel of its own.");
                    Assert.That(converted.AttackRange, Is.EqualTo(AttackRangeKind.None));
                }

                Assert.That(movement.BaseValues.Single().Amount, Is.EqualTo(movements[index]));
                Assert.That(defense.BaseValues.Single().Amount, Is.EqualTo(defenses[index]));
            }
        }
    }
}
