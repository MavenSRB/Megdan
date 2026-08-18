using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.4a's one authority-owned Battle-Zone membership query. Minion Battle, Heavy
    /// immunity, Lane Push, setup, and the displaced-minion correction all consult it, so its answers
    /// for the awkward coordinates - terrain, off-board - are stated here rather than discovered
    /// mid-correction.
    /// </summary>
    public sealed class BattleZoneQueryTests
    {
        private static readonly HeroId Hero = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
        private static readonly MapId Map = new MapId("synthetic-battle-zone-query-map");

        [Test]
        public void Membership_IsFalseForTerrainAndOffBoard_RatherThanThrowing()
        {
            // The correction asks about wherever a displaced minion ended up, so the query must state
            // an answer for a terrain space (no zone by the map invariant) and an off-board coordinate
            // (no space at all) instead of throwing the way GetZoneForSpace does.
            var map = CreateTwoZoneMap();

            Assert.That(BattleZoneQuery.IsInStartingBattleZone(map, new HexCoordinate(0, 0)), Is.True);
            Assert.That(BattleZoneQuery.IsInStartingBattleZone(map, new HexCoordinate(3, 0)), Is.False,
                "An outer-zone space is playable and outside.");
            Assert.That(BattleZoneQuery.IsInStartingBattleZone(map, new HexCoordinate(6, 0)), Is.False,
                "A terrain space belongs to no zone.");
            Assert.That(BattleZoneQuery.IsInStartingBattleZone(map, new HexCoordinate(99, 99)), Is.False,
                "An off-board coordinate has no space at all.");
            Assert.That(
                BattleZoneQuery.IsInStartingBattleZone(map, new SpaceId("q99_r99")), Is.False,
                "An unknown space ID is not in the zone rather than an exception.");
        }

        [Test]
        public void TheProbeForm_TellsAConfiguredMapFromASyntheticOne()
        {
            Assert.That(
                BattleZoneQuery.TryGetStartingBattleZone(CreateTwoZoneMap(), out var zone), Is.True);
            Assert.That(zone.IsStartingBattleZone, Is.True);
            Assert.That(
                BattleZoneQuery.TryGetStartingBattleZone(
                    BoardMapDefinition.CreateSynthetic(
                        Map, 1, new[] { new HexCoordinate(0, 0) }),
                    out var none),
                Is.False,
                "The single-zone synthetic board flags nothing, which is what routes the round " +
                "authority to its synthetic end-of-round capabilities.");
            Assert.That(none, Is.Null);
        }

        [Test]
        public void MinionsOutside_AreIntrinsicMiniaturesOnly_AndCanonicallyOrdered()
        {
            // The subject set is board.Minions - miniatures by construction - so a hero standing
            // outside the zone and a token placed outside it are structurally incapable of joining it,
            // which is p.18's "only relevant for minions represented by minion miniatures" made
            // architectural rather than filtered. The query takes no modification set at all, so no
            // counts-as declaration can widen it.
            var board = CreateTwoZoneBoard(
                heroAt: new HexCoordinate(3, 0),
                minions: new[]
                {
                    Minion("zone-query-minion-b", new HexCoordinate(4, 0)),
                    Minion("zone-query-minion-a", new HexCoordinate(3, 1)),
                    Minion("zone-query-minion-inside", new HexCoordinate(0, 0))
                });
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId("zone-query-token"),
                SyntheticComponentDefinitions.CreateCatalog().LoadToken(SyntheticComponentDefinitions.HazardV4),
                Hero,
                Hero,
                new CardInstanceId("zone-query-token-source"),
                new HexCoordinate(4, 1)));

            var outside = BattleZoneQuery.FindMinionsOutside(board);
            var inside = BattleZoneQuery.FindMinionsInside(board);

            Assert.That(
                outside.Select(minion => minion.MinionId.Value),
                Is.EqualTo(new[] { "zone-query-minion-a", "zone-query-minion-b" }),
                "Only the two displaced miniatures, in canonical identity order; the hero and the " +
                "token standing outside the zone never enter the set.");
            Assert.That(
                inside.Select(minion => minion.MinionId.Value),
                Is.EqualTo(new[] { "zone-query-minion-inside" }));
        }

        [Test]
        public void ZoneCoordinates_AreThePlayableZoneSetInCanonicalOrder()
        {
            var coordinates = BattleZoneQuery.FindStartingBattleZoneCoordinates(CreateTwoZoneMap());

            Assert.That(coordinates, Is.EqualTo(new[]
            {
                new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0)
            }), "The battle zone's playable spaces only, never terrain, in axial order.");
        }

        /// <summary>Battle zone (0,0)..(2,0), outer zone (3,0)..(5,0) plus (3,1),(4,1), terrain (6,0).</summary>
        private static BoardMapDefinition CreateTwoZoneMap() =>
            BoardMapDefinition.CreateSyntheticWithStartingBattleZone(
                Map,
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) },
                new[]
                {
                    new HexCoordinate(3, 0), new HexCoordinate(4, 0), new HexCoordinate(5, 0),
                    new HexCoordinate(3, 1), new HexCoordinate(4, 1)
                },
                new[] { new HexCoordinate(6, 0) });

        private static AdjacentMoveAuthority CreateTwoZoneBoard(
            HexCoordinate heroAt, QuickGameMinionSetupState[] minions) =>
            new AdjacentMoveAuthority(
                CreateTwoZoneMap(),
                new[] { new HeroOccupancy(Hero, heroAt) },
                Array.Empty<ObstacleOccupancy>(),
                minions);

        private static QuickGameMinionSetupState Minion(string id, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(PlaceholderOneVsOneParticipants.RedTeamIdValue),
                TeamSide.Red,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("zone-query-spawn")),
                new ConfiguredSpaceReference(Map, new SpaceId($"q{at.Q}_r{at.R}")),
                at);
    }
}
