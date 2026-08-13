using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Director-hand presentation against the configured stack. Before milestone 6.1c these
    /// assertions ran through the legacy <c>HeroSelectionController</c> drill, so the header,
    /// card-button, and commitment label strings the configured <see cref="DirectorHandsPresenter"/>
    /// actually produces had no direct coverage. The underlying state transitions - commit
    /// eligibility, joint reveal ordering, and pass eligibility - remain owned by
    /// <see cref="TurnCardCommitmentAuthorityTests"/>; this fixture owns the projection and its
    /// exact rendered labels.
    /// </summary>
    public sealed class DirectorHandPresentationTests
    {
        [Test]
        public void ConfiguredDirectorHands_ProjectEveryAuthoritativeHandWithTeamHeaderLabels()
        {
            var session = CreateSession();
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(session, view);

            presenter.Refresh();

            var hands = view.Model.Hands;
            var setupHeroes = session.Projection.Setup.Heroes
                .OrderBy(hero => hero.TeamSide)
                .ThenBy(hero => hero.HeroId.Value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(hands.Select(hand => hand.TeamSide),
                Is.EqualTo(new[] { TeamSide.Red, TeamSide.Blue }),
                "Hands project in deterministic team order, Red before Blue.");
            Assert.That(hands.Select(hand => hand.HeroId),
                Is.EqualTo(setupHeroes.Select(hero => hero.HeroId)));

            // The header format is the contract; the display name is whatever the authoritative
            // setup says. The mirrored 1v1 configuration names both heroes "Wasp".
            Assert.That(hands.Select(hand => hand.HeaderLabel), Is.EqualTo(setupHeroes
                .Select(hero => hero.TeamSide == TeamSide.Red
                    ? $"[R] RED {hero.DisplayName.ToUpperInvariant()}"
                    : $"[B] BLUE {hero.DisplayName.ToUpperInvariant()}")));
            Assert.That(hands.Select(hand => hand.HeaderLabel),
                Is.EqualTo(new[] { "[R] RED WASP", "[B] BLUE WASP" }),
                "Pinned so a change to the header format fails here rather than only in the HUD.");

            Assert.That(hands, Has.All.Matches<DirectorHandViewModel>(hand => hand.Cards.Count == 5));
            Assert.That(hands.SelectMany(hand => hand.Cards), Has.All.Matches<CardInstance>(card =>
                card.State.Location == CardLocation.Hand &&
                !card.State.HasBeenPlayed &&
                card.State.ResolutionStatus == CardResolutionStatus.None));
            Assert.That(hands.SelectMany(hand => hand.Cards).Select(card => card.Id),
                Is.EquivalentTo(session.Projection.Cards
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.Id)));

            foreach (var hand in hands)
            {
                foreach (var card in hand.Cards)
                {
                    var definition = hand.Definitions.Single(value =>
                        value.Reference.Equals(card.DefinitionReference));
                    Assert.That(hand.CardButtonLabels[card.Id],
                        Is.EqualTo($"{definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}"));
                }
            }

            var redHand = hands.Single(hand => hand.TeamSide == TeamSide.Red);
            var needle = redHand.Cards.Single(card => redHand.Definitions
                .Single(value => value.Reference.Equals(card.DefinitionReference))
                .DisplayName.EndsWith("Red Needle", StringComparison.Ordinal));
            Assert.That(redHand.CardButtonLabels[needle.Id], Is.EqualTo("TEST - Red Needle\nINITIATIVE 13"),
                "One concrete label is pinned so definition-to-label drift cannot pass silently.");
        }

        [Test]
        public void ConfiguredDirectorHands_StayFacedownUntilBothSidesRespondThenRevealTogether()
        {
            var session = CreateSession();
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(session, view);
            // Selected by name, not by index: GetHandCards orders by card ID, which is not the
            // definition order, so an index would silently pin whichever card happens to sort first.
            var redCard = CardNamed(session, TeamSide.Red, "Red Needle");
            var blueCard = CardNamed(session, TeamSide.Blue, "Blue Sentinel");

            presenter.Refresh();
            Assert.That(RedHand(view).CommitmentLabel, Is.EqualTo("AWAITING CARD"));

            Assert.That(presenter.SelectCommitmentCard(redCard.Id), Is.True);
            Assert.That(presenter.ConfirmSelectedCard(), Is.True);
            presenter.Refresh();

            Assert.That(session.Projection.GetCommitment(redCard.OwnerId).Status,
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(RedHand(view).CommitmentLabel, Is.EqualTo("FACEDOWN COMMITTED"));
            Assert.That(RedHand(view).CommitmentLabel, Does.Not.Contain("Red Needle"),
                "A facedown commitment must not leak the committed card's identity to the other side.");
            Assert.That(RedHand(view).RevealedCardInstanceId, Is.Null);
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(redCard.Id)).State.FaceOrientation,
                Is.EqualTo(CardFaceOrientation.FaceDown));

            Assert.That(presenter.SelectCommitmentCard(blueCard.Id), Is.True);
            Assert.That(presenter.ConfirmSelectedCard(), Is.True);
            presenter.Refresh();

            Assert.That(session.Projection.Commitments.Select(commitment => commitment.Status),
                Has.All.EqualTo(TurnCardCommitmentStatus.Revealed),
                "Both sides reveal together, not as each one commits.");
            Assert.That(
                new[] { redCard.Id, blueCard.Id }.Select(id =>
                    session.Projection.Cards.Single(card => card.Id.Equals(id)).State.FaceOrientation),
                Has.All.EqualTo(CardFaceOrientation.FaceUp));

            foreach (var hand in view.Model.Hands)
            {
                var revealed = hand.RevealedCard;
                var definition = hand.Definitions.Single(value =>
                    value.Reference.Equals(revealed.DefinitionReference));
                Assert.That(hand.CommitmentLabel, Is.EqualTo(
                    $"REVEALED: {definition.DisplayName}\nINITIATIVE {definition.Initiative.Value}"));
            }

            Assert.That(RedHand(view).CommitmentLabel, Is.EqualTo("REVEALED: TEST - Red Needle\nINITIATIVE 13"));
        }

        [Test]
        public void DirectorHandViewModel_FormatsEveryCommitmentStatusExplicitly()
        {
            var session = CreateSession();
            var projection = session.Projection;
            var hero = projection.Setup.Heroes.First(value => value.TeamSide == TeamSide.Red);
            var cards = projection.Cards
                .Where(card => card.OwnerId.Equals(hero.HeroId) && card.State.Location == CardLocation.Hand)
                .ToArray();
            var revealed = cards[0];
            var revealedDefinition = projection.Definitions.Single(value =>
                value.Reference.Equals(revealed.DefinitionReference));

            Assert.That(Label(TurnCardCommitmentStatus.AwaitingResponse), Is.EqualTo("AWAITING CARD"));
            Assert.That(Label(TurnCardCommitmentStatus.CardCommittedFacedown), Is.EqualTo("FACEDOWN COMMITTED"));
            Assert.That(Label(TurnCardCommitmentStatus.Passed), Is.EqualTo("PASS - NO CARD"));
            Assert.That(Label(TurnCardCommitmentStatus.Revealed, revealed), Is.EqualTo(
                $"REVEALED: {revealedDefinition.DisplayName}\nINITIATIVE {revealedDefinition.Initiative.Value}"));
            Assert.That(Label(TurnCardCommitmentStatus.Revealed), Is.EqualTo("REVEALED"),
                "A Revealed commitment with no resolvable card still renders an explicit label.");

            string Label(TurnCardCommitmentStatus status, CardInstance revealedCard = null) =>
                new DirectorHandViewModel(
                    hero.TeamSide,
                    hero.HeroId,
                    hero.DisplayName,
                    status,
                    status == TurnCardCommitmentStatus.Passed,
                    revealedCard?.Id,
                    revealedCard,
                    cards,
                    projection.Definitions,
                    null).CommitmentLabel;
        }

        [Test]
        public void ConfiguredDirectorHands_RefusePassForAHeroThatStillHoldsAnEligibleCard()
        {
            var session = CreateSession();
            var view = new CapturingHandsView();
            var presenter = new DirectorHandsPresenter(session, view);

            presenter.Refresh();

            Assert.That(view.Model.Hands, Has.All.Matches<DirectorHandViewModel>(hand => !hand.CanPass),
                "Every hero starts with a full eligible hand, so no side may pass.");
            foreach (var hand in view.Model.Hands)
            {
                Assert.That(presenter.Pass(hand.HeroId), Is.False);
            }

            Assert.That(session.Projection.Commitments.Select(commitment => commitment.Status),
                Has.All.EqualTo(TurnCardCommitmentStatus.AwaitingResponse),
                "A refused pass must not move any commitment state.");
        }

        private static DirectorHandViewModel RedHand(CapturingHandsView view) =>
            view.Model.Hands.Single(hand => hand.TeamSide == TeamSide.Red);

        private static CardInstance CardNamed(MatchSession session, TeamSide side, string displayNameSuffix)
        {
            return GetHandCards(session, side).Single(card => session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .DisplayName.EndsWith(displayNameSuffix, StringComparison.Ordinal));
        }

        private static IReadOnlyList<CardInstance> GetHandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards.Where(card =>
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class CapturingHandsView : IDirectorHandsView
        {
            public DirectorHandsViewModel Model { get; private set; }

            public void Render(DirectorHandsViewModel model) => Model = model;
        }
    }
}
