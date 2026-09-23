using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class AwaitedResponderNarrationJourneyTests
    {
        [UnityTest]
        public IEnumerator TheActualAttackerAndDefenderSeeTheRightNarrationAndReturnToTheTurn()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
            var initial = Root().Runtime;
            var defender = initial.Setup.Heroes.Single(hero => hero.ContentId.Value == "sabina").HeroId;
            var attacker = initial.Setup.Heroes.Single(hero => hero.ContentId.Value == "wasp").HeroId;
            var seats = initial.Host.Seats.Seats;
            foreach (var viewer in new[] { attacker, defender })
            {
                MatchSceneCompositionRoot.NextComposedSeat = seats.Single(seat => seat.HeroId.Equals(viewer)).Id;
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
                {
                    new SandboxHeroStaging(attacker, new HexCoordinate(-3, 1)),
                    new SandboxHeroStaging(defender, new HexCoordinate(-2, 1))
                });
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
                var root = Root();
                var runtime = root.Runtime;
                Assert.That(runtime.Seat.HeroId, Is.EqualTo(viewer));
                var ownCard = viewer.Equals(attacker) ? WaspCardContent.MagneticDagger : SabinaCardContent.Quickdraw;
                yield return CommitThroughTray(root, NameOf(runtime, ownCard));
                var other = viewer.Equals(attacker) ? defender : attacker;
                var otherCard = viewer.Equals(attacker) ? SabinaCardContent.Quickdraw : WaspCardContent.MagneticDagger;
                var remote = Client(root, other);
                var card = remote.Projection.Cards.Single(value => value.OwnerId.Equals(other) &&
                    value.State.Location == CardLocation.Hand && value.DefinitionReference.DefinitionId.Equals(otherCard));
                Assert.That(remote.Submit(remote.SeatProjection.AuthorizedCommands.Single(command =>
                    command.Kind == DirectorCommandKind.CommitCard && command.CardInstanceId.Equals(card.Id))).Accepted, Is.True);
                runtime.RefreshAll(); yield return null;
                Assert.That(runtime.ReadModel.SelectedActor.ActorId, Is.EqualTo(attacker));

                if (viewer.Equals(attacker))
                {
                    yield return StartActionThroughControl(root, ActionKind.Attack);
                    Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                        HeroView(defender).transform.position), Is.True);
                    yield return null;
                }
                else
                {
                    var actorClient = Client(root, attacker);
                    var offer = actorClient.Projection.AvailableTurnActions.Single(value => actorClient.Projection.Definitions
                        .Single(definition => definition.Reference.Equals(value.SourceDefinitionReference)).ActionDefinitions
                        .Single(action => action.Id.Equals(value.ActionDefinitionId)).Kind == ActionKind.Attack);
                    Assert.That(actorClient.Submit(actorClient.SeatProjection.AuthorizedCommands.Single(command =>
                        command.Kind == DirectorCommandKind.StartAction && command.ActionDefinitionId.Equals(offer.ActionDefinitionId))).Accepted, Is.True);
                    Assert.That(actorClient.Submit(actorClient.SeatProjection.AuthorizedCommands.Single(command =>
                        command.Kind == DirectorCommandKind.ChooseAttackTarget && command.HeroId.Equals(defender))).Accepted, Is.True);
                    runtime.RefreshAll(); yield return null;
                }

                // The narration names the hero exactly as every other status line does - the
                // configured DISPLAY NAME ("Sabina the Gunslinger"), never a short id - so this
                // expectation is derived from the same setup fact instead of being hardcoded.
                var defenderName = runtime.ReadModel.Setup.Heroes
                    .Single(hero => hero.HeroId.Equals(defender)).DisplayName.ToUpperInvariant();
                var expected = viewer.Equals(attacker)
                    ? defenderName + " — CHOOSING A DEFENSE CARD"
                    : "YOUR DEFENSE — CHOOSE A CARD, OR DECLINE";
                Assert.That(runtime.RoundView.Model.Status, Is.EqualTo(expected));
                Assert.That(root.Hud.RootElement.Q<Label>("gd-narration-text").text, Is.EqualTo(expected));
                Assert.That(runtime.HandsView.Model.Hands.All(hand => hand.HeroId.Equals(viewer)), Is.True);
                if (viewer.Equals(attacker))
                {
                    Assert.That(runtime.ReadModel.PendingDefenseChoice, Is.Null);
                    Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                        element.name == "gd-defense-card" && UiPlayer.IsShown(element)), Is.False);
                }
                yield return UiPlayer.CaptureGameView("narration-842c", viewer.Equals(attacker) ? "defense-observer" : "defense-responder");
                var defenseClient = Client(root, defender);
                var defense = defenseClient.Projection.PendingDefenseChoice.LegalDefenseOffers.First();
                if (viewer.Equals(defender))
                    yield return ClickDefenseFace(root, defense.SourceDefinitionReference.DefinitionId);
                else
                {
                    Assert.That(defenseClient.Submit(defenseClient.SeatProjection.AuthorizedCommands.First(command =>
                        command.Kind == DirectorCommandKind.ChooseDefenseCard && command.CardInstanceId.Equals(defense.SourceCardInstanceId))).Accepted, Is.True);
                    runtime.RefreshAll(); yield return null;
                }
                Assert.That(runtime.ReadModel.PendingDefenseChoice, Is.Null);
                // MILESTONE 8.86: p.14's Defense was ANSWERED rather than merely withdrawn. The
                // printed cost of answering is the face leaving the defender's hand, so the card
                // this journey pressed is read in the discard pile it was spent into - which is the
                // one board fact a narration journey can otherwise pass without ever touching.
                Assert.That(
                    runtime.ReadModel.Cards
                        .Single(value => value.Id.Equals(defense.SourceCardInstanceId))
                        .State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "The face that answered p.14 is spent into the defender's own discard pile.");
                Assert.That(runtime.RoundView.Model.Status, Does.Not.Contain("DEFENSE"));
                Assert.That(runtime.RoundView.Model.Status,
                    viewer.Equals(attacker) ? Does.StartWith("YOUR TURN") : Does.Contain("WASP"));
                Healthy(runtime);
            }
        }

        private static SeatMatchClient Client(MatchSceneCompositionRoot root, HeroId hero) =>
            new SeatMatchClient(root.Runtime.Host.ClientForSeat(root.Runtime.Host.Seats.Seats.Single(seat => seat.HeroId.Equals(hero)).Id));
    }
}
