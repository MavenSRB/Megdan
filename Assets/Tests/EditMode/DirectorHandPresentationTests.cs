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
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
