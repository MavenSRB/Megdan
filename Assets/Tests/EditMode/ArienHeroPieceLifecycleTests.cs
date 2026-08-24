using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Arien's own board piece and the session operations behind it, at the authoritative tier.
    /// Milestone 8.20a.
    ///
    /// <para>The permanent hero player-path gate asks for the hero piece to be proved as the visible,
    /// selectable, movable, defeatable, respawnable thing it is, and for restore, replay, Undo and
    /// restart to reconstruct it. The half a player performs is proved through real pointer events and
    /// camera rays by <c>ArienUiJourneyPlayModeTests</c> and its two siblings; this fixture is the
    /// authoritative half those journeys stand on - what the state says before anything is drawn.</para>
    ///
    /// <para>It is the row the gate's matrix links from <c>arien-hero-piece</c> and
    /// <c>arien-session-operations</c>, and it is deliberately Unity-independent so the same claims
    /// hold wherever the rules run.</para>
    /// </summary>
    public sealed class ArienHeroPieceLifecycleTests
    {
        [Test]
        public void TheStagedBlueSeatIsAriensOwnPrintedSet_AtOneSpawnAndOneSpace()
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien: true);
            var setup = session.Projection.Setup;

            var arienSeats = setup.Heroes
                .Where(hero => hero.ContentId.Value == ArienCardContent.ContentSetId)
                .ToArray();
            Assert.That(arienSeats, Has.Length.EqualTo(1),
                "Exactly one seat deals Arien's printed set.");
            var arien = arienSeats[0];
            Assert.That(arien.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(string.IsNullOrWhiteSpace(arien.SpawnPoint.SpawnPointId.Value), Is.False,
                "and it is bound to a configured spawn point rather than a bare coordinate.");

            var occupancies = session.Projection.Heroes
                .Where(hero => hero.HeroId.Equals(arien.HeroId))
                .ToArray();
            Assert.That(occupancies, Has.Length.EqualTo(1),
                "One hero, one occupancy - two would be the duplicate piece the gate counts.");

            var dealt = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(arien.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(ArienCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "and the eighteen cards it holds are Arien's own, which is what makes the piece " +
                "this hero rather than a Blue-coloured one.");
        }

        [Test]
        public void ArienIsDefeatedAndRespawns_LeavingOneOccupancyAtEveryStage()
        {
            var session = CreateArienUnderAttack(out var arienId);
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(arienId)),
                Is.EqualTo(1));

            DeclineTheDefense(session);

            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(arienId)), Is.False,
                "p.13 takes the defeated hero off the board.");

            // p.13 offers the respawn at the defeated hero's own next turn. Reaching it is the
            // journeys' business; what this holds is that the piece never exists twice on the way.
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(arienId)),
                Is.LessThanOrEqualTo(1),
                "and never leaves a second copy behind while it is away.");
        }

        [Test]
        public void AriensDefeatSurvivesASnapshotRoundTrip_WithTheSameOccupancy()
        {
            var session = CreateArienUnderAttack(out var arienId);
            DeclineTheDefense(session);
            var before = session.Projection.Heroes
                .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")
                .OrderBy(value => value)
                .ToArray();

            var payload = session.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            var restored = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien: true);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True,
                "Arien's defeated position must restore.");

            Assert.That(restored.Projection.Heroes
                    .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(before),
                "The restored board holds exactly the pieces the captured one did.");
            Assert.That(restored.Projection.Heroes.Any(hero => hero.HeroId.Equals(arienId)),
                Is.False,
                "including Arien's absence, which is the state a restore most easily invents.");
        }

        [Test]
        public void AriensAcceptedCommandsReplayIntoTheSameState_AndUndoReturnsTheOneBefore()
        {
            var session = CreateArienUnderAttack(out _);
            var beforeDefense = session.Projection.Heroes.Count;
            DeclineTheDefense(session);
            var commands = session.Projection.AcceptedCommands.ToArray();
            var afterDefense = session.CaptureSnapshot();

            var replayed = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien: true);
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    $"Every accepted command replays: {command.Kind}.");
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(afterDefense),
                "A replay of Arien's own match reproduces its authoritative state exactly.");

            Assert.That(session.Undo().Accepted, Is.True,
                "The retained prefix gives back the state before the last answer.");
            Assert.That(session.Projection.Heroes, Has.Count.EqualTo(beforeDefense),
                "and Arien is standing again, because the Defeat came from the command Undo dropped.");
        }

        [Test]
        public void ARestartedArienMatch_ReturnsHisWholeSetupWithNoAcceptedHistory()
        {
            var session = CreateArienUnderAttack(out var arienId);
            DeclineTheDefense(session);
            Assert.That(session.Projection.AcceptedCommands, Is.Not.Empty);

            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.Projection.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");

            var arien = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(arienId));
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(arienId)),
                Is.EqualTo(1),
                "Arien is back on the board exactly once.");
            Assert.That(session.Projection.Heroes
                    .Single(hero => hero.HeroId.Equals(arienId)).Coordinate,
                Is.EqualTo(arien.Coordinate),
                "on the space his configuration puts him on,");
            Assert.That(session.Projection.Cards
                    .Count(card => card.OwnerId.Equals(arienId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(ArienCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of him.");
        }

        // -----------------------------------------------------------------------------------------
        // Steps
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The staged Wasp-versus-Arien board with the enemy's printed Attack stopped on Arien at
        /// p.14's Defense response - the position every case below starts from.
        /// </summary>
        private static MatchSession CreateArienUnderAttack(out HeroId arienId)
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien: true);
            arienId = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.MagneticDagger, ArienCardContent.LiquidLeap);
            ConfiguredDefenseCardTests.AttackTheHero(session, arienId);
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "The Attack must reach p.14's Defense response.");
            return session;
        }

        private static void DeclineTheDefense(MatchSession session)
        {
            var choice = session.Projection.PendingDefenseChoice;
            Assert.That(choice.CanDecline, Is.True,
                "p.14 lets a defender answer with nothing, which is what defeats them here.");
            Assert.That(
                session.Execute(DirectorCommand.DeclineDefense(
                    choice.ResolutionId, choice.ChoiceId)).Accepted,
                Is.True);
        }
    }
}
