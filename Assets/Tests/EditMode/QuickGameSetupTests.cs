using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class QuickGameSetupTests
    {
        [Test]
        public void MirroredWaspQuickGame_ResolvesExactOfficialMapSetupFromConfiguration()
        {
            var definition = LoadOfficialMap();
            var participants = StagedBoardParticipants.Create();
            var configuration = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var tieBreaker = TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Blue);

            var setup = QuickGameSetupAuthority.Create(configuration, participants, definition, tieBreaker);

            Assert.That(configuration.RequiredMap, Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(setup.SelectedMap, Is.EqualTo(configuration.RequiredMap));
            Assert.That(setup.WaveCount, Is.EqualTo(3));
            Assert.That(setup.Teams, Has.Count.EqualTo(2));
            Assert.That(setup.Teams, Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 4));
            Assert.That(setup.TieBreakerState, Is.EqualTo(tieBreaker));
            Assert.That(setup.Heroes, Has.Count.EqualTo(2));
            Assert.That(setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(hero =>
                hero.ContentId.Equals(new HeroContentId("wasp")) && hero.DisplayName == "Wasp"));
            Assert.That(setup.Heroes.Select(hero => hero.HeroId),
                Is.EquivalentTo(participants.Heroes.Select(hero => hero.Id)));
            Assert.That(setup.Heroes.Select(hero => hero.TeamSide),
                Is.EquivalentTo(new[] { TeamSide.Red, TeamSide.Blue }));
            Assert.That(setup.Heroes.All(hero =>
                definition.GetZoneForSpace(hero.OccupiedSpace.SpaceId).IsThrone), Is.True);

            Assert.That(setup.Minions, Has.Count.EqualTo(12));
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var teamMinions = setup.Minions.Where(minion => minion.TeamSide == side).ToArray();
                Assert.That(teamMinions, Has.Length.EqualTo(6));
                Assert.That(teamMinions.Count(minion => minion.MinionType == StandardMinionType.Melee), Is.EqualTo(4));
                Assert.That(teamMinions.Count(minion => minion.MinionType == StandardMinionType.Ranged), Is.EqualTo(1));
                Assert.That(teamMinions.Count(minion => minion.MinionType == StandardMinionType.Heavy), Is.EqualTo(1));
            }

            Assert.That(setup.Minions.Select(minion => minion.MinionId).Distinct().Count(), Is.EqualTo(12));
            Assert.That(setup.Minions.Select(minion => minion.SpawnPoint).Distinct().Count(), Is.EqualTo(12));
            Assert.That(setup.Heroes.Select(hero => hero.OccupiedSpace)
                .Concat(setup.Minions.Select(minion => minion.OccupiedSpace)).Distinct().Count(), Is.EqualTo(14));
            Assert.That(setup.Minions.All(minion =>
                definition.GetZoneForSpace(minion.OccupiedSpace.SpaceId).IsStartingBattleZone), Is.True);
        }

        [Test]
        public void Create_RejectsMissingWrongIdentityAndWrongRevisionSelectedMapsDistinctly()
        {
            var official = LoadOfficialMap();
            var configuration = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var participants = StagedBoardParticipants.Create();
            var tieBreaker = TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red);

            AssertReason(
                QuickGameSetupRejectionReason.MissingSelectedMap,
                () => QuickGameSetupAuthority.Create(configuration, participants, null, tieBreaker));

            var wrongIdentity = new BoardMapDefinition(
                new MapId("other-map"), official.ContentRevision, official.DisplayName,
                official.Spaces, official.Zones, official.SpawnPoints, official.OrderedLaneZoneIds);
            AssertReason(
                QuickGameSetupRejectionReason.MapIdentityMismatch,
                () => QuickGameSetupAuthority.Create(configuration, participants, wrongIdentity, tieBreaker));

            var wrongRevision = new BoardMapDefinition(
                official.Id, official.ContentRevision + 1, official.DisplayName,
                official.Spaces, official.Zones, official.SpawnPoints, official.OrderedLaneZoneIds);
            AssertReason(
                QuickGameSetupRejectionReason.ContentRevisionMismatch,
                () => QuickGameSetupAuthority.Create(configuration, participants, wrongRevision, tieBreaker));
        }

        [Test]
        public void Create_RejectsMapLocalChildReferenceFromAnotherMapBeforeResolvingItsBareId()
        {
            var official = LoadOfficialMap();
            var source = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var firstHero = source.Heroes[0];
            var crossMapHero = new QuickGameHeroPlacementDefinition(
                firstHero.HeroId,
                firstHero.ContentId,
                firstHero.DisplayName,
                new ConfiguredSpawnPointReference(new MapId("other-map"), firstHero.SpawnPoint.SpawnPointId),
                firstHero.OccupiedSpace);
            var configuration = new QuickGameSetupConfiguration(
                source.RequiredMap,
                source.WaveCount,
                source.LifeCountPerTeam,
                new[] { crossMapHero, source.Heroes[1] },
                source.Minions);

            AssertReason(
                QuickGameSetupRejectionReason.CrossMapReference,
                () => QuickGameSetupAuthority.Create(
                    configuration,
                    StagedBoardParticipants.Create(),
                    official,
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Red)));
        }

        [Test]
        public void Create_RejectsSpawnSpaceMismatchWithoutPartialSetup()
        {
            var official = LoadOfficialMap();
            var source = QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne();
            var first = source.Minions[0];
            var invalid = new QuickGameMinionPlacementDefinition(
                first.MinionId,
                first.TeamSide,
                first.MinionType,
                first.SpawnPoint,
                source.Minions[1].OccupiedSpace);
            var configuration = new QuickGameSetupConfiguration(
                source.RequiredMap,
                source.WaveCount,
                source.LifeCountPerTeam,
                source.Heroes,
                new[] { invalid }.Concat(source.Minions.Skip(1)));

            AssertReason(
                QuickGameSetupRejectionReason.SpawnSpaceMismatch,
                () => QuickGameSetupAuthority.Create(
                    configuration,
                    StagedBoardParticipants.Create(),
                    official,
                    TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Blue)));
        }

        private static BoardMapDefinition LoadOfficialMap() =>
            OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);

        private static void AssertReason(QuickGameSetupRejectionReason expected, TestDelegate action)
        {
            var exception = Assert.Throws<QuickGameSetupException>(action);
            Assert.That(exception.Reason, Is.EqualTo(expected));
        }
    }
}
