using System;
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
    public sealed class HeroCardInspectionTests
    {
        internal static MatchHost Create(bool allies, bool enemies)
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            RegisteredHero Hero(string id) => roster.Single(h => h.ContentId.Value == id);
            var lineup = new HeroLineup(new[] { Hero("gydion"), Hero("takahide") }, new[] { Hero("arien"), Hero("sabina") });
            var format = MatchFormat.TwoVersusTwoQuick;
            var config = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(HeroLineupComposition.Compose(format, lineup),
                config.Participants, map, config.TieBreakerState);
            var scenario = new InitialScenarioDescription(config.Participants, config.CardDefinitions,
                config.CardInstances, config.TieBreakerState, config.SetupSeed, map.Reference,
                setup.Heroes.Select(h => new HeroOccupancy(h.HeroId, h.Coordinate)), Array.Empty<ObstacleOccupancy>());
            return MatchHost.Compose(new MatchSessionInitialState(scenario, setup, new[] { map }, config,
                experiments: new ExperimentalMatchOptions(teammateCoordination: allies, enemyCardInformation: enemies)));
        }
        [TestCase(false, false)] [TestCase(true, false)] [TestCase(false, true)] [TestCase(true, true)]
        public void IndependentOptionsScopeFacesAcrossLocalAndRealWireWithoutGrantingActions(bool allies, bool enemies)
        {
            var host = Create(allies, enemies);
            var wire = PrivateOnlineWireHarness.Compose(host, "inspection"); wire.SeatAll(); wire.ReadyAndPickEverySeat();
            for (int i = 0; i < host.Seats.Seats.Count; i++)
            {
                var seat = host.Seats.Seats[i]; var local = host.ProjectionForSeat(seat.Id);
                var expected = host.Seats.Seats.Where(s => s.Id.Equals(seat.Id) ||
                    (s.TeamSide == seat.TeamSide ? allies : enemies)).Select(s => s.HeroId).ToArray();
                Assert.That(local.CardInspections.Select(r => r.Hero), Is.EquivalentTo(expected));
                var remote = wire.Client(i + 1).LatestProjection;
                Assert.That(remote.CardInspections.Select(r => r.Hero), Is.EquivalentTo(expected));
                Assert.That(remote.Experiments.EnemyCardInformation, Is.EqualTo(enemies));
                Assert.That(remote.Experiments.TeammateCoordination, Is.EqualTo(allies));
                foreach (var row in local.CardInspections)
                {
                    var copy = remote.CardInspections.Single(r => r.Hero.Equals(row.Hero));
                    Assert.That(copy.HandCards, Is.EqualTo(row.HandCards));
                    Assert.That(copy.OtherCards, Is.EqualTo(row.OtherCards));
                    Assert.That(copy.ConcealedHandCount, Is.EqualTo(row.ConcealedHandCount));
                    Assert.That(copy.ConcealedOtherCount, Is.EqualTo(row.ConcealedOtherCount));
                    Assert.That(copy.Spellbook, Is.EqualTo(row.Spellbook));
                }
                Assert.That(local.AuthorizedCommands.Where(c => c.CardInstanceId.IsDefined)
                    .All(c => host.Projection.Cards.Single(card => card.Id.Equals(c.CardInstanceId)).OwnerId.Equals(seat.HeroId)), Is.True);
                Assert.That(local.Cards.Where(c => c.State.Location == CardLocation.Hand)
                    .All(c => c.OwnerId.Equals(seat.HeroId)), Is.True, "Inspection faces must not join playable cards.");
            }
        }
        [Test] public void CurrentSetIncludesAllGoldsAndBookButNotFutureUpgradesOrItems()
        {
            var host = Create(true, true); var full = host.Projection;
            var takahide = full.Setup.Heroes.Single(h => h.ContentId.Value == "takahide").HeroId;
            var gydion = full.Setup.Heroes.Single(h => h.ContentId.Value == "gydion").HeroId;
            var view = host.ProjectionForSeat(host.Seats.Seats.Last().Id);
            var row = view.CardInspections.Single(r => r.Hero.Equals(takahide));
            Assert.That(row.HandCards.Count, Is.EqualTo(5));
            Assert.That(row.OtherCards.Count, Is.EqualTo(2));
            Assert.That(row.Cards.Count, Is.EqualTo(7), "Five dealt faces plus the other two basic golds.");
            Assert.That(row.Cards.Count(r => full.Definitions.Single(d => d.Reference.Equals(r)).Progression.Color == CardColor.Gold), Is.EqualTo(3));
            Assert.That(row.Cards.All(r => full.Definitions.Single(d => d.Reference.Equals(r)).Progression.Tier <= CardTier.I), Is.True);
            var book = view.CardInspections.Single(r => r.Hero.Equals(gydion));
            Assert.That(book.Spellbook.Count, Is.EqualTo(22));
            Assert.That(book.Cards.Count, Is.EqualTo(5));
            Assert.That(book.Cards.Intersect(book.Spellbook), Is.Empty);
            Assert.That(typeof(HeroCardInspection).GetProperties().Select(p => p.Name),
                Is.EquivalentTo(new[] { "Hero", "Team", "HandCards", "OtherCards", "ConcealedHandCount", "ConcealedOtherCount", "Cards", "Spellbook" }),
                "Only visibility-safe section membership, never instance identity, belongs in this view.");
        }
        [Test] public void CommittingDoesNotIdentifyTheHiddenCardInTheInspection()
        {
            var host = Create(true, true); var owner = host.Seats.Seats[0]; var viewer = host.Seats.Seats.Last();
            var own = host.ProjectionForSeat(owner.Id);
            var command = own.AuthorizedCommands.First(c => c.Kind == DirectorCommandKind.CommitCard);
            Assert.That(host.SubmitForSeat(owner.Id, new SeatSubmission(new SubmissionId("inspection-commit"), own.Version, command)).Accepted, Is.True);
            var after = host.ProjectionForSeat(viewer.Id);
            var row = after.CardInspections.Single(r => r.Hero.Equals(owner.HeroId));
            Assert.That(row.HandCards, Is.Empty, "Remaining identities would identify the missing commitment by elimination.");
            Assert.That(row.ConcealedHandCount, Is.EqualTo(4));
            Assert.That(row.ConcealedOtherCount, Is.EqualTo(1));
            Assert.That(row.Cards, Has.No.Member(own.Cards.Single(c => c.Id.Equals(command.CardInstanceId)).DefinitionReference));
            Assert.That(after.Cards.Any(c => c.Id.Equals(command.CardInstanceId)), Is.False);
        }
        [Test] public void CurrentFacesFollowPlayDiscardUpgradeItemsAndUnlockedUltimates()
        {
            var state = OwnerScopedProjectionTests.CreateOfficialInitialState();
            var host = MatchHost.Compose(new MatchSessionInitialState(state.Scenario, state.Setup, state.AvailableMaps,
                state.InitialConfiguration, experiments: new ExperimentalMatchOptions(enemyCardInformation: true, teammateCoordination: true)));
            var seen = new System.Collections.Generic.HashSet<CardLocation>();
            bool resolved = false;
            OwnerScopedProjectionTests.DriveThroughSeats(host, 700, projection =>
            {
                foreach (var row in host.ProjectionForSeat(host.Seats.Seats[0].Id).CardInspections)
                {
                    var trusted = HeroCardInspection.ForHero(projection, row.Hero);
                    foreach (var card in projection.Cards.Where(c => c.OwnerId.Equals(row.Hero)))
                    {
                        var definition = projection.Definitions.Single(d => d.Reference.Equals(card.DefinitionReference));
                        if (definition.Progression.IsSpell) continue;
                        seen.Add(card.State.Location);
                        resolved |= card.State.ResolutionStatus == CardResolutionStatus.Resolved;
                        if (card.State.Location == CardLocation.Hand || card.State.Location == CardLocation.TurnArea ||
                            card.State.Location == CardLocation.DiscardPile || card.State.Location == CardLocation.UltimateArea)
                            Assert.That(trusted.Cards, Does.Contain(card.DefinitionReference), "Every current face survives its lifecycle state.");
                        if (card.State.Location == CardLocation.ItemSlot || card.State.Location == CardLocation.SetAside)
                            Assert.That(trusted.Cards, Has.No.Member(card.DefinitionReference), "Items and future choices are excluded.");
                    }
                }
                return false;
            }, 6);
            Assert.That(seen, Does.Contain(CardLocation.Hand));
            Assert.That(seen, Does.Contain(CardLocation.TurnArea));
            Assert.That(seen, Does.Contain(CardLocation.ItemSlot));
            Assert.That(seen, Does.Contain(CardLocation.SetAside));
            Assert.That(resolved, Is.True);
            foreach (var fixture in new[] {
                ConfiguredMatchFixtures.CreateSessionWithADiscardedCard(out _),
                ConfiguredMatchFixtures.CreateContinuationSessionWithUnlockedUltimate() })
            {
                var projection = fixture.Projection;
                var cards = projection.Cards.Where(c => c.State.Location == CardLocation.DiscardPile ||
                    c.State.Location == CardLocation.UltimateArea).ToArray();
                Assert.That(cards, Is.Not.Empty, "The explicit scenario must contain the state it claims.");
                foreach (var card in cards)
                    Assert.That(HeroCardInspection.ForHero(projection, card.OwnerId).Cards, Does.Contain(card.DefinitionReference));
            }
        }
        [TestCase(-1)] [TestCase(11)]
        public void WireRejectsOutOfBoundInspectionCounts(int count)
        {
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                writer.Write(count); writer.Flush(); stream.Position = 0;
                using (var reader = new System.IO.BinaryReader(stream))
                    Assert.Throws<ArgumentException>(() => ExperimentalWireCodec.ReadInspections(reader));
            }
        }
        [Test] public void WireRejectsDuplicateOwnersAndInvalidFaces()
        {
            var host = Create(true, true); var row = host.ProjectionForSeat(host.Seats.Seats[0].Id).CardInspections[0];
            using (var stream = new System.IO.MemoryStream())
            using (var writer = new System.IO.BinaryWriter(stream))
            {
                ExperimentalWireCodec.WriteInspections(writer, new[] { row, row }); writer.Flush(); stream.Position = 0;
                using (var reader = new System.IO.BinaryReader(stream))
                    Assert.Throws<ArgumentException>(() => ExperimentalWireCodec.ReadInspections(reader));
            }
            Assert.Throws<ArgumentException>(() => new HeroCardInspection(row.Hero, row.Team,
                new[] { row.Cards[0], row.Cards[0] }, Array.Empty<CardDefinitionReference>(), 0, 0, row.Spellbook));
            Assert.Throws<ArgumentException>(() => new HeroCardInspection(row.Hero, row.Team,
                new[] { default(CardDefinitionReference) }, Array.Empty<CardDefinitionReference>(), 0, 0, row.Spellbook));
        }
        [Test] public void EnemyOnlyOptionPersistsAndRejectsMismatchedRestoreAtomically()
        {
            var host = Create(false, true); var snapshot = host.CaptureSnapshot();
            Assert.That(snapshot.StartsWith("guards-experimental/5:"), Is.True);
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.True);
            var disabled = Create(false, false); var before = disabled.CaptureSnapshot();
            Assert.That(disabled.Submit(MatchSubmission.ForSnapshotRestore(snapshot)).Accepted, Is.False);
            Assert.That(disabled.CaptureSnapshot(), Is.EqualTo(before));
            Assert.That(host.Submit(MatchSubmission.ForSnapshotRestore(snapshot.Replace("guards-experimental/5:", "guards-experimental/4:"))).Accepted, Is.False);
            Assert.That(host.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(new ExperimentalMatchOptions().EnemyCardInformation, Is.False);
            Assert.That(host.Experiments, Is.Not.EqualTo(disabled.Experiments));
        }
    }
}
