using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Rulebook p.8's "Retrieve Cards" stage, seen through a live configured match.
    ///
    /// Milestone 3.8c wrote this behaviour and the configured scene has depended on it to play repeated
    /// rounds ever since. Milestone 6.13 neither rewrites nor widens it: what this fixture adds is the
    /// regression evidence its acceptance checks ask for, at the stage rather than at the card - the
    /// deterministic order, the exact owner attribution, the cards the stage must leave alone, and the
    /// next round finding its hands where the stage put them.
    ///
    /// The per-card contract is <see cref="CardLifecycleAuthorityTests"/>'s: two admitted shapes, fresh
    /// in-hand state, and face orientation orthogonal to both. The stage ordering against cleanup and an
    /// owed Lane Push is <see cref="EndOfRoundComponentCleanupTests"/>'s, and what a pause or a
    /// game-ending stage before it may do is <see cref="RoundResolutionAuthorityTests"/>'s.
    /// </summary>
    public sealed class EndOfRoundCardRetrievalTests
    {
        [Test]
        public void TheStage_ReturnsEveryPlayedCardToItsCanonicalOwnerInFreshStateAndDeterministicOrder()
        {
            var session = CreateConfiguredSession();
            var playedBefore = PlayOneOrdinaryRound(session);
            var untouchedBefore = HandCardIds(session);

            var retrieval = AdvanceToRetrievalStage(session);

            var retrieved = retrieval.Events
                .Where(value => value.Kind == RoundResolutionEventKind.CardRetrieved)
                .ToArray();
            Assert.That(retrieved.Select(value => value.CardInstanceId.Value.Value), Is.EqualTo(playedBefore),
                "Every played card comes back, once each, in stable card-instance order.");
            Assert.That(
                retrieved.All(value => value.CardOwnerId.Value.Equals(
                    session.Projection.Cards.Single(card =>
                        card.Id.Equals(value.CardInstanceId.Value)).OwnerId)), Is.True,
                "A retrieval names the card's own canonical owner; no card is attributed to the other hero.");
            Assert.That(
                retrieved.Any(value => untouchedBefore.Contains(value.CardInstanceId.Value.Value)), Is.False,
                "A card that stayed in hand all round is not retrieved, because it never left.");

            Assert.That(
                session.Projection.Cards.All(card => card.State.Equals(CardStateFacts.InitialInHand())),
                Is.True,
                "p.8 returns them to their owners' hands, and fresh state is what a hand card is.");
            foreach (var hero in session.Projection.Setup.Heroes)
            {
                Assert.That(
                    session.Projection.Cards.Count(card => card.OwnerId.Equals(hero.HeroId)),
                    Is.EqualTo(session.Projection.Cards.Count(card =>
                        card.OwnerId.Equals(hero.HeroId) &&
                        card.State.Location == CardLocation.Hand)),
                    "Every card a hero owns is in that hero's hand once the stage has run.");
            }
        }

        [Test]
        public void TheStage_IsDeterministicAndOffersTheReturnedCardsToTheSameHeroNextRound()
        {
            var session = CreateConfiguredSession();
            PlayOneOrdinaryRound(session);

            var beforeStage = session.Projection;
            Assert.That(beforeStage.PendingEffectChoice, Is.Null);
            Assert.That(beforeStage.PendingMinionBattleChoice, Is.Null);
            Assert.That(beforeStage.PendingInitiativeChoice, Is.Null);

            AdvanceToRetrievalStage(session);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.PendingMinionBattleChoice, Is.Null,
                "The stage is deterministic, so it introduces no prompt and no new player command.");

            var expected = session.Projection.Cards
                .GroupBy(card => card.OwnerId.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(card => card.Id.Value).OrderBy(value => value, StringComparer.Ordinal).ToArray());

            FinishEndOfRound(session);

            Assert.That(session.Projection.RoundIndex, Is.EqualTo(2));
            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            foreach (var pair in expected)
            {
                var offered = session.Projection.Cards
                    .Where(card => card.OwnerId.Value == pair.Key && card.State.Location == CardLocation.Hand)
                    .Select(card => card.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
                Assert.That(offered, Is.EqualTo(pair.Value),
                    "The next round offers each hero exactly the cards the stage returned to that hero.");
            }

            // The offer is real rather than a reading of state: the next round accepts one of them.
            var candidate = session.Projection.Cards.First(card =>
                card.State.Location == CardLocation.Hand);
            var committed = session.Execute(DirectorCommand.CommitCard(candidate.OwnerId, candidate.Id));
            Assert.That(committed.Accepted, Is.True, committed.Detail);
        }

        [Test]
        public void TheRetrievalBoundary_SurvivesRestoreReplayAndUndoWithoutRetrievingAnythingTwice()
        {
            var session = CreateConfiguredSession();
            PlayOneOrdinaryRound(session);
            AdvanceToStage(session, EndOfRoundStage.RetrieveCardsAndCancelActiveEffects);

            var before = session.Projection.AuthoritativeSnapshot;
            var inTurnArea = session.Projection.Cards
                .Count(card => card.State.Location == CardLocation.TurnArea);
            Assert.That(inTurnArea, Is.GreaterThan(0),
                "The cards must still be in the turn area for this boundary to be about anything.");

            var retrieval = session.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(retrieval.Accepted, Is.True, retrieval.Detail);
            var after = session.Projection.AuthoritativeSnapshot;
            var retrievedCount = retrieval.Events
                .Count(value => value.Kind == RoundResolutionEventKind.CardRetrieved);
            Assert.That(retrievedCount, Is.EqualTo(inTurnArea));

            var restored = CreateConfiguredSession();
            Assert.That(restored.RestoreSnapshot(after).Accepted, Is.True);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(after));
            Assert.That(
                restored.Projection.Cards.All(card => card.State.Equals(CardStateFacts.InitialInHand())),
                Is.True, "A restored match past retrieval holds the hands the stage produced.");

            var replayed = CreateConfiguredSession();
            foreach (var command in session.Projection.AcceptedCommands)
            {
                var result = replayed.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(after),
                "A full replay reaches the same bytes, so nothing was retrieved a second time.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before),
                "An undone retrieval puts the played cards back where the stage found them.");
            Assert.That(
                session.Projection.Cards.Count(card => card.State.Location == CardLocation.TurnArea),
                Is.EqualTo(inTurnArea));

            var again = session.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(again.Accepted, Is.True, again.Detail);
            Assert.That(
                again.Events.Count(value => value.Kind == RoundResolutionEventKind.CardRetrieved),
                Is.EqualTo(retrievedCount),
                "and re-running it retrieves exactly the same cards exactly once each.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(after));
        }

        // ---------------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Plays four ordinary turns with Hold, and answers the returned card ids in the order the
        /// stage is expected to report them.
        /// </summary>
        private static string[] PlayOneOrdinaryRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var card = session.Projection.Cards.FirstOrDefault(candidate =>
                        session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                        candidate.State.Location == CardLocation.Hand);
                    if (card == null)
                    {
                        var hero = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
                        Accept(session, DirectorCommand.Pass(hero));
                        continue;
                    }

                    Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
                }

                var tie = session.Projection.PendingInitiativeChoice;
                if (tie != null)
                {
                    Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                }

                var turnIndex = session.Projection.TurnIndex;
                while (session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                    session.Projection.TurnIndex == turnIndex)
                {
                    Accept(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));
                }
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
            return session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.TurnArea)
                .Select(card => card.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
        }

        private static string[] HandCardIds(MatchSession session) =>
            session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => card.Id.Value)
                .ToArray();

        private static MatchCommandResult AdvanceToRetrievalStage(MatchSession session)
        {
            AdvanceToStage(session, EndOfRoundStage.RetrieveCardsAndCancelActiveEffects);
            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());
            Assert.That(result.Accepted, Is.True, result.Detail);
            return result;
        }

        private static void AdvanceToStage(MatchSession session, EndOfRoundStage stage)
        {
            while (session.Projection.CurrentEndOfRoundStage != stage)
            {
                Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }
        }

        private static void FinishEndOfRound(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.EndOfRound)
            {
                Accept(session, DirectorCommand.AdvanceEndOfRound());
            }
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        /// <summary>The ordinary configured match on the official map.</summary>
        private static MatchSession CreateConfiguredSession()
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
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new BoardMapDefinition[] { map }));
        }
    }
}
