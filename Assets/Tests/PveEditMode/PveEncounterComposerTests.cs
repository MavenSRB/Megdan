using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Composition;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-021. The static encounter composes as an ordinary match through public APIs: chosen heroes
    /// on the placement spaces, round-1 enemies on their spawns, round-2 enemies off the board,
    /// creeps as Blue melee minions, and the same seed always the same match.
    /// </summary>
    public sealed class PveEncounterComposerTests
    {
        private static readonly string[] FiveHeroes =
            { "hero-arien", "hero-brogan", "hero-wasp", "hero-tigerclaw", "hero-xargatha" };

        private static PveComposedEncounter Compose(ulong seed = 1234, string[] heroes = null) =>
            PveEncounterComposer.Compose(
                new PveEncounterSetup(heroes ?? FiveHeroes, seed),
                PveTestContent.Encounter(),
                PveTestContent.Catalog(),
                PveTestContent.OfficialMap());

        [Test]
        public void FiveChosenHeroesAreBlueAndStartOffTheBoardForTheirPlacement()
        {
            var encounter = Compose();
            var projection = MatchHost.Compose(encounter.InitialState).Projection;

            Assert.That(encounter.Heroes.Select(hero => hero.Value), Is.EqualTo(FiveHeroes));
            foreach (var hero in encounter.Heroes)
            {
                Assert.That(projection.Participants.GetTeamForHero(hero).Side, Is.EqualTo(TeamSide.Blue));
                Assert.That(projection.Heroes.Any(value => value.HeroId.Equals(hero)), Is.False,
                    "PVE-D05: heroes are placed by the player at round 1.");
            }

            var rules = encounter.InitialState.ModeRules;
            Assert.That(rules.PlayerPlacementSpaces, Is.EqualTo(PveTestContent.Encounter().HeroPlacement));
            Assert.That(rules.RoundStartPlacements.Count(value => value.Round == 1), Is.EqualTo(5));
        }

        [Test]
        public void RoundOneEnemiesStandOnTheirSpawnsAndRoundTwoEnemiesStartOffTheBoard()
        {
            var encounter = Compose();
            var projection = MatchHost.Compose(encounter.InitialState).Projection;

            Assert.That(encounter.Enemies.Count, Is.EqualTo(9));
            foreach (var enemy in encounter.Enemies)
            {
                Assert.That(projection.Participants.GetTeamForHero(enemy.HeroId).Side, Is.EqualTo(TeamSide.Red));
                var standing = projection.Heroes.SingleOrDefault(value => value.HeroId.Equals(enemy.HeroId));
                if (enemy.Spawn.Round == 1)
                {
                    Assert.That(standing.Coordinate, Is.EqualTo(enemy.Spawn.Coordinate), enemy.DisplayName);
                }
                else
                {
                    Assert.That(standing.HeroId.IsDefined, Is.False, enemy.DisplayName + " starts off the board.");
                }
            }

            Assert.That(encounter.Enemies.Where(enemy => enemy.Spawn.Round == 1).Select(enemy => enemy.DisplayName),
                Is.EqualTo(Enumerable.Range(1, 6).Select(number => "Raider " + number)));
        }

        [Test]
        public void EachEnemyHoldsItsFourOrangeAndItsPurpleCardsInHand()
        {
            var encounter = Compose();
            var projection = MatchHost.Compose(encounter.InitialState).Projection;

            foreach (var enemy in encounter.Enemies)
            {
                var cards = projection.Cards.Where(card => card.OwnerId.Equals(enemy.HeroId)).ToArray();
                Assert.That(cards.Length, Is.EqualTo(4 + enemy.Type.PurpleCards.Count), enemy.DisplayName);
                Assert.That(cards.All(card => card.State.Location == CardLocation.Hand), Is.True);
            }
        }

        [Test]
        public void TheCreepsAreBlueMeleeMinionsOnTheirSpaces()
        {
            var projection = MatchHost.Compose(Compose().InitialState).Projection;

            Assert.That(projection.Minions.Select(minion => minion.Coordinate),
                Is.EquivalentTo(PveTestContent.Encounter().Creeps.Select(creep => creep.Coordinate)));
            Assert.That(projection.Minions.All(minion => minion.TeamSide == TeamSide.Blue), Is.True);
        }

        [Test]
        public void TheSameSeedComposesTheSameMatchAndTheSnapshotRoundTrips()
        {
            var first = MatchHost.Compose(Compose(77).InitialState);
            var second = MatchHost.Compose(Compose(77).InitialState);

            Assert.That(second.CaptureSnapshot(), Is.EqualTo(first.CaptureSnapshot()));

            var session = new LocalDirectorMatchClient(second);
            var restored = session.RestoreSnapshot(first.CaptureSnapshot());
            Assert.That(restored.Accepted, Is.True, restored.Detail);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(first.CaptureSnapshot()));
        }

        [Test]
        public void TheSeedDecidesTheTieBreakerSide()
        {
            var sides = Enumerable.Range(1, 16)
                .Select(seed => Compose((ulong)seed, new[] { "hero-arien" }).InitialState.Setup.TieBreakerState.RevealedSide)
                .Distinct()
                .ToArray();

            Assert.That(sides, Is.EquivalentTo(new[] { TeamSide.Red, TeamSide.Blue }),
                "Different seeds reach both sides; the same seed always reaches the same one.");
            Assert.That(Compose(5).InitialState.Setup.TieBreakerState,
                Is.EqualTo(Compose(5).InitialState.Setup.TieBreakerState));
        }

        [Test]
        public void ACommitmentUndoesBackToTheComposedMatch()
        {
            var host = MatchHost.Compose(Compose().InitialState);
            var session = new LocalDirectorMatchClient(host);
            var before = session.CaptureSnapshot();
            var hero = new HeroId("pve-enemy-01");
            var card = session.Projection.Cards.First(value =>
                value.OwnerId.Equals(hero) && value.State.Location == CardLocation.Hand);

            var committed = session.Execute(DirectorCommand.CommitCard(hero, card.Id));
            Assert.That(committed.Accepted, Is.True, committed.Detail);
            Assert.That(session.CaptureSnapshot(), Is.Not.EqualTo(before));

            var undone = session.Undo();
            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before));
        }
    }
}
