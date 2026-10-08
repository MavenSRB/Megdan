using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class HeroMinionDefenseQueryTests
    {
        [TestCase(TeamSide.Blue, StandardMinionType.Melee, 1, 1)]
        [TestCase(TeamSide.Blue, StandardMinionType.Melee, 2, 0)]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy, 1, 1)]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy, 2, 0)]
        [TestCase(TeamSide.Blue, StandardMinionType.Ranged, 1, 0)]
        [TestCase(TeamSide.Red, StandardMinionType.Melee, 1, -1)]
        [TestCase(TeamSide.Red, StandardMinionType.Melee, 2, 0)]
        [TestCase(TeamSide.Red, StandardMinionType.Heavy, 1, -1)]
        [TestCase(TeamSide.Red, StandardMinionType.Heavy, 2, 0)]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, 1, -1)]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, 2, -1)]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, 3, 0)]
        public void PublicBoardQueryUsesRulebookMinionBoundaries(
            TeamSide side, StandardMinionType type, int distance, int expected)
        {
            var map = new MapId("defense-preview");
            var id = new MinionId("contributor");
            var minion = MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                id, new TeamId("team"), side, type,
                new ConfiguredSpawnPointReference(map, new SpawnPointId("spawn")),
                new ConfiguredSpaceReference(map, new SpaceId("space")),
                new HexCoordinate(distance, 0)));
            var result = DefenseValueQuery.CalculateMinionContributions(
                TeamSide.Blue, new HexCoordinate(0, 0), new[] { minion });
            Assert.That(result.Sum(value => value.Amount), Is.EqualTo(expected));
            Assert.That(result.Select(value => value.SourceMinionId),
                Is.EqualTo(expected == 0 ? Array.Empty<MinionId>() : new[] { id }));
        }

        [Test]
        public void EveryStandingBodyHasAnIdleReadingWithoutChangingMatchState()
        {
            var session = HeroBodiesBoard.CreateSession(out _, out var blue, out var support);
            var before = session.CaptureSnapshot();
            Assert.That(session.Projection.PendingAttackTargetDefensePreviews, Is.Empty);
            var result = HeroMinionDefenseQuery.Create(session.Projection);
            Assert.That(result.Count, Is.EqualTo(session.Projection.Heroes.Count));
            Assert.That(session.Projection.HeroDefenseModifiers.Count,
                Is.EqualTo(session.Projection.Heroes.Count));
            Assert.That(result.Single(value => value.HeroId.Equals(blue) && value.HeroBody == 1)
                .MinionContributions.Any(value => value.SourceMinionId.Equals(support)), Is.False);
            Assert.That(result.Single(value => value.HeroId.Equals(blue) && value.HeroBody == 2)
                .MinionContributions.Single(value => value.SourceMinionId.Equals(support)).Amount,
                Is.EqualTo(1), "Support belongs to the nearby body, not every body of the hero.");
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EverySeatAndDecodedNetworkBoardAgreeWithoutPrivateCardAccess(bool inputEnabled)
        {
            var session = HeroBodiesBoard.CreateSession(out _, out _, out _);
            var host = new MatchHost(session);
            var expected = Describe(host.Projection);
            var expectedModifiers = DescribeModifiers(host.Projection);
            foreach (var seat in host.Seats.Seats)
            {
                var local = host.ProjectionForSeat(seat.Id);
                Assert.That(Describe(local), Is.EqualTo(expected));
                Assert.That(DescribeModifiers(local), Is.EqualTo(expectedModifiers));
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(local), out var record), Is.True);
                var remote = new NetworkMatchReadModel(
                    host.Projection, MatchContentCatalogs.ForThisBuild(), record, inputEnabled);
                Assert.That(Describe(remote), Is.EqualTo(expected));
                Assert.That(DescribeModifiers(remote), Is.EqualTo(expectedModifiers),
                    "protocol /49 carries the public item-and-creep modifier rows");
                Assert.That(remote.PendingAttackTargetDefensePreviews, Is.Empty);
            }
        }

        [Test]
        public void AttackSelectionDefeatUndoAndRestoreRebuildTheSameBodyReadings()
        {
            var session = HeroBodiesBoard.CreateSessionAtTheAttackTargetChoice(
                out _, out var blue, out _);
            foreach (var target in session.Projection.PendingAttackTargetDefensePreviews)
            {
                var ordinary = HeroMinionDefenseQuery.Create(session.Projection).Single(value =>
                    value.HeroId.Equals(target.HeroId) && value.HeroBody == target.HeroBody);
                Assert.That(ordinary.MinionContributions.Select(value => (value.SourceMinionId, value.Amount)),
                    Is.EqualTo(target.MinionContributions.Select(value => (value.SourceMinionId, value.Amount))));
            }
            var choice = session.Projection.PendingAttackTargetChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.ChooseAttackTarget(
                choice.ResolutionId, choice.ChoiceId, blue, 2));
            var snapshot = session.CaptureSnapshot();
            var before = Describe(session.Projection);
            var defense = session.Projection.PendingDefenseChoice;
            HeroBodiesBoard.Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            Assert.That(HeroMinionDefenseQuery.Create(session.Projection).Any(value =>
                value.HeroId.Equals(blue)), Is.False, "A defeated hero must leave no standing shield.");
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(Describe(session.Projection), Is.EqualTo(before));
            var restored = HeroBodiesBoard.CreateSession(out _, out _, out _);
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True);
            Assert.That(Describe(restored.Projection), Is.EqualTo(before));
        }

        private static string Describe(IMatchReadModel projection) => string.Join("|",
            HeroMinionDefenseQuery.Create(projection).Select(hero =>
                hero.HeroId.Value + "/" + hero.HeroBody + ":" + string.Join(",",
                    hero.MinionContributions.Select(value => value.SourceMinionId.Value + "=" + value.Amount))));

        private static string DescribeModifiers(IMatchReadModel projection) => string.Join("|",
            projection.HeroDefenseModifiers.Select(hero =>
                hero.HeroId.Value + "/" + hero.HeroBody + ":" + string.Join(",",
                    hero.Contributions.Select(value => value.Kind + "/" + value.SourceLabel + "=" + value.Amount))));
    }
}
