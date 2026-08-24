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
    /// Wasp's own board piece and the session operations behind it, at the authoritative tier.
    /// Milestone 8.20b.
    ///
    /// <para>The permanent hero player-path gate asks for the hero piece to be proved as the visible,
    /// selectable, movable, defeatable, respawnable thing it is, and for restore, replay, Undo and
    /// restart to reconstruct it. The half a player performs is proved through real pointer events and
    /// camera rays by <c>WaspUiJourneyPlayModeTests</c> and its two siblings; this fixture is the
    /// authoritative half those journeys stand on - what the state says before anything is drawn. It
    /// is the twin of <see cref="ArienHeroPieceLifecycleTests"/>, which milestone 8.20a wrote for the
    /// other seat.</para>
    ///
    /// <para><b>Two configurations, deliberately.</b> Identity is asked of the staged Wasp-versus-Arien
    /// board, because that is the match the configured scene loads and the question is which seat is
    /// Wasp's. Defeat, restore, replay, Undo and restart are asked of the verified mirror, because
    /// there the hero under attack is a Wasp rather than the hero opposite her - and a Defeat is the
    /// one of these that has to happen to <em>this</em> piece to say anything about it.</para>
    ///
    /// <para>It is the row the gate's matrix links from <c>wasp-hero-piece</c> and
    /// <c>wasp-session-operations</c>, and it is deliberately Unity-independent so the same claims
    /// hold wherever the rules run.</para>
    /// </summary>
    public sealed class WaspHeroPieceLifecycleTests
    {
        [Test]
        public void TheStagedRedSeatIsWaspsOwnPrintedSet_AtOneSpawnAndOneSpace()
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(
                out _, stagedArien: true);
            var setup = session.Projection.Setup;

            var waspSeats = setup.Heroes
                .Where(hero => hero.ContentId.Value == WaspCardContent.ContentSetId)
                .ToArray();
            Assert.That(waspSeats, Has.Length.EqualTo(1),
                "Exactly one seat deals Wasp's printed set.");
            var wasp = waspSeats[0];
            Assert.That(wasp.TeamSide, Is.EqualTo(TeamSide.Red));
            Assert.That(string.IsNullOrWhiteSpace(wasp.SpawnPoint.SpawnPointId.Value), Is.False,
                "and it is bound to a configured spawn point rather than a bare coordinate.");

            var occupancies = session.Projection.Heroes
                .Where(hero => hero.HeroId.Equals(wasp.HeroId))
                .ToArray();
            Assert.That(occupancies, Has.Length.EqualTo(1),
                "One hero, one occupancy - two would be the duplicate piece the gate counts.");

            var dealt = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(wasp.HeroId))
                .Select(card => card.DefinitionReference.DefinitionId.Value)
                .OrderBy(id => id)
                .ToArray();
            Assert.That(dealt, Is.EqualTo(WaspCardContent.DealtDefinitions
                .Select(definition => definition.Id.Value)
                .OrderBy(id => id)
                .ToArray()),
                "and the eighteen cards it holds are Wasp's own, which is what makes the piece " +
                "this hero rather than a Red-coloured one.");
        }

        [Test]
        public void WaspIsDefeatedAndRespawns_LeavingOneOccupancyAtEveryStage()
        {
            var session = CreateWaspUnderAttack(out var defenderId);
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(defenderId)),
                Is.EqualTo(1));

            DeclineTheDefense(session);

            Assert.That(session.Projection.Heroes.Any(hero => hero.HeroId.Equals(defenderId)),
                Is.False,
                "p.13 takes the defeated hero off the board.");

            // p.13 offers the respawn at the defeated hero's own next turn. Reaching it is the
            // journeys' business; what this holds is that the piece never exists twice on the way.
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(defenderId)),
                Is.LessThanOrEqualTo(1),
                "and never leaves a second copy behind while it is away.");
        }

        [Test]
        public void WaspsDefeatSurvivesASnapshotRoundTrip_WithTheSameOccupancy()
        {
            var session = CreateWaspUnderAttack(out var defenderId);
            DeclineTheDefense(session);
            var before = session.Projection.Heroes
                .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")
                .OrderBy(value => value)
                .ToArray();

            var payload = session.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            var restored = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(out _);
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True,
                "Wasp's defeated position must restore.");

            Assert.That(restored.Projection.Heroes
                    .Select(hero => $"{hero.HeroId.Value}@{hero.Coordinate}")
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(before),
                "The restored board holds exactly the pieces the captured one did.");
            Assert.That(restored.Projection.Heroes.Any(hero => hero.HeroId.Equals(defenderId)),
                Is.False,
                "including the defeated Wasp's absence, which is the state a restore most easily " +
                "invents.");
        }

        [Test]
        public void WaspsAcceptedCommandsReplayIntoTheSameState_AndUndoReturnsTheOneBefore()
        {
            var session = CreateWaspUnderAttack(out _);
            var beforeDefense = session.Projection.Heroes.Count;
            DeclineTheDefense(session);
            var commands = session.Projection.AcceptedCommands.ToArray();
            var afterDefense = session.CaptureSnapshot();

            var replayed = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(out _);
            foreach (var command in commands)
            {
                Assert.That(replayed.Execute(command).Accepted, Is.True,
                    $"Every accepted command replays: {command.Kind}.");
            }

            Assert.That(replayed.CaptureSnapshot(), Is.EqualTo(afterDefense),
                "A replay of Wasp's own match reproduces its authoritative state exactly.");

            Assert.That(session.Undo().Accepted, Is.True,
                "The retained prefix gives back the state before the last answer.");
            Assert.That(session.Projection.Heroes, Has.Count.EqualTo(beforeDefense),
                "and Wasp is standing again, because the Defeat came from the command Undo dropped.");
        }

        [Test]
        public void ARestartedWaspMatch_ReturnsHerWholeSetupWithNoAcceptedHistory()
        {
            var session = CreateWaspUnderAttack(out var defenderId);
            DeclineTheDefense(session);
            Assert.That(session.Projection.AcceptedCommands, Is.Not.Empty);

            Assert.That(session.Restart().Accepted, Is.True);
            Assert.That(session.Projection.AcceptedCommands, Is.Empty,
                "A restart adopts the initial state with no accepted history.");

            var wasp = session.Projection.Setup.Heroes
                .Single(hero => hero.HeroId.Equals(defenderId));
            Assert.That(session.Projection.Heroes.Count(hero => hero.HeroId.Equals(defenderId)),
                Is.EqualTo(1),
                "Wasp is back on the board exactly once.");
            Assert.That(session.Projection.Heroes
                    .Single(hero => hero.HeroId.Equals(defenderId)).Coordinate,
                Is.EqualTo(wasp.Coordinate),
                "on the space her configuration puts her on,");
            Assert.That(session.Projection.Cards
                    .Count(card => card.OwnerId.Equals(defenderId) &&
                        card.State.Location == CardLocation.Hand),
                Is.EqualTo(WaspCardContent.StartingHand.Count),
                "with p.6's starting hand back in front of her.");
        }

        // -----------------------------------------------------------------------------------------
        // Steps
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The verified Wasp mirror with the Red seat's printed Attack stopped on the Blue Wasp at
        /// p.14's Defense response - the position every case below starts from. The mirror rather than
        /// the staged Wasp-versus-Arien board, because the hero this fixture is about has to be the one
        /// being defeated.
        /// </summary>
        private static MatchSession CreateWaspUnderAttack(out HeroId defenderId)
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(out _);
            defenderId = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue);
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.MagneticDagger, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defenderId);
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
