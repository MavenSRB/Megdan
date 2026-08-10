using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class DirectorHandPresentationTests
    {
        [Test]
        public void VisibleDirectorHands_ProjectBothAuthoritativeHandsWithExplicitTeamLabels()
        {
            var gameObject = new GameObject("Director Hand Controller");
            try
            {
                var controller = gameObject.AddComponent<HeroSelectionController>();
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                controller.ConfigurePlaceholderTestMatch(configuration);
                controller.ConfigureCardDrivenMovement(true);

                var hands = controller.VisibleDirectorHands;

                Assert.That(hands.Select(hand => hand.Label), Is.EqualTo(new[]
                {
                    "[R] RED HERO",
                    "[B] BLUE HERO"
                }));
                Assert.That(hands.Select(hand => hand.OwnerId), Is.EqualTo(new[]
                {
                    new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue),
                    new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue)
                }));
                Assert.That(hands.SelectMany(hand => hand.Instances).Select(card => card.Id),
                    Is.EquivalentTo(configuration.CardInstances.Select(card => card.Id)));
                Assert.That(hands.SelectMany(hand => hand.Instances), Has.All.Matches<CardInstance>(card =>
                    card.State.Location == CardLocation.Hand &&
                    !card.State.HasBeenPlayed &&
                    card.State.ResolutionStatus == CardResolutionStatus.None));
                Assert.That(hands, Has.All.Matches<DirectorTestHand>(hand => hand.Instances.Count == 5));
                Assert.That(hands[0].GetCardButtonLabel(hands[0].Instances[0]),
                    Is.EqualTo("TEST - Red Needle\nINITIATIVE 13"));
                Assert.That(hands[0].GetCardButtonLabel(hands[0].Instances[4]),
                    Is.EqualTo("TEST - Red Bombard\nINITIATIVE 5"));
                Assert.That(hands[1].GetCardButtonLabel(hands[1].Instances[4]),
                    Is.EqualTo("TEST - Blue Longbow\nINITIATIVE 4"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void ConfirmedDirectorCards_StayFacedownUntilBothSidesRespondThenRevealTogether()
        {
            var gameObject = new GameObject("Turn Card Controller");
            try
            {
                var controller = gameObject.AddComponent<HeroSelectionController>();
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                controller.ConfigurePlaceholderTestMatch(configuration);
                controller.ConfigureCardDrivenMovement(true);
                var redCard = configuration.RedHand.Instances[0];
                var blueCard = configuration.BlueHand.Instances[1];

                Assert.That(controller.TrySelectDirectorCard(redCard.Id), Is.True);
                Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);

                Assert.That(redCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown));
                Assert.That(controller.GetTurnCardCommitmentStatus(TeamSide.Red),
                    Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
                Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red),
                    Is.EqualTo("FACEDOWN COMMITTED"));
                Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red),
                    Does.Not.Contain("Red Needle"));
                Assert.That(controller.TurnCardCommitmentEvents, Has.Count.EqualTo(1));
                Assert.That(controller.TurnCardCommitmentEvents[0].HasCardInstanceId, Is.False);
                Assert.That(controller.TrySelectDirectorCard(configuration.RedHand.Instances[1].Id), Is.False);

                Assert.That(controller.TrySelectDirectorCard(blueCard.Id), Is.True);
                Assert.That(controller.TryConfirmSelectedTurnCard(), Is.True);

                Assert.That(controller.IsTurnCardRevealComplete, Is.True);
                Assert.That(redCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
                Assert.That(blueCard.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
                Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red),
                    Is.EqualTo("REVEALED: TEST - Red Needle\nINITIATIVE 13"));
                Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Blue),
                    Is.EqualTo("REVEALED: TEST - Blue Sentinel\nINITIATIVE 11"));
                Assert.That(controller.CardStatus, Does.Contain("revealed together"));
                Assert.That(controller.CardResolutionEvents, Is.Empty);
                Assert.That(controller.TurnCardCommitmentEvents.Select(ruleEvent => ruleEvent.Kind),
                    Is.EqualTo(new[]
                    {
                        TurnCardCommitmentEventKind.CardCommittedFacedown,
                        TurnCardCommitmentEventKind.CardCommittedFacedown,
                        TurnCardCommitmentEventKind.CardRevealed,
                        TurnCardCommitmentEventKind.CardRevealed,
                        TurnCardCommitmentEventKind.RevealCompleted
                    }));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void DirectorPass_IsAvailableOnlyForSideWithNoEligibleCard()
        {
            var gameObject = new GameObject("Turn Pass Controller");
            try
            {
                var controller = gameObject.AddComponent<HeroSelectionController>();
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                foreach (var redCard in configuration.RedHand.Instances)
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardDefenseFromHand(redCard), Is.True);
                }

                controller.ConfigurePlaceholderTestMatch(configuration);
                controller.ConfigureCardDrivenMovement(true);

                Assert.That(controller.CanPassTurnCard(TeamSide.Red), Is.True);
                Assert.That(controller.CanPassTurnCard(TeamSide.Blue), Is.False);
                Assert.That(controller.TryPassTurnCard(TeamSide.Red), Is.True);
                Assert.That(controller.GetTurnCardCommitmentStatus(TeamSide.Red),
                    Is.EqualTo(TurnCardCommitmentStatus.Passed));
                Assert.That(controller.GetDirectorCommitmentLabel(TeamSide.Red),
                    Is.EqualTo("PASS - NO CARD"));
                Assert.That(controller.CardStatus, Does.Contain("passed with no card"));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
