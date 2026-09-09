using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Interaction;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    internal static class WhisperAuditStaging
    {
        internal static IEnumerator WithDiscards(SceneCardContent content, bool adjacent = false)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var own = WhisperSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var ownAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            var enemyAt = adjacent ? ownAt.GetNeighbors().First(space => map.Spaces.Any(tile =>
                tile.Coordinate.Equals(space) && tile.Kind == BoardMapSpaceKind.Playable)) :
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemy)).Coordinate;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(own, hand: runtime.Cards.Where(card => card.OwnerId.Equals(own) &&
                    card.State.Location == CardLocation.Hand && !card.DefinitionReference.DefinitionId.Equals(WhisperCardContent.SwiftJustice))
                    .Select(card => card.DefinitionReference.DefinitionId)),
                new SandboxHeroStaging(enemy, enemyAt, hand: runtime.Cards.Where(card => card.OwnerId.Equals(enemy) &&
                    card.State.Location == CardLocation.Hand && !card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier))
                    .Select(card => card.DefinitionReference.DefinitionId))
            }, removedMinions: runtime.Setup.Minions.Where(minion => minion.Coordinate.Equals(enemyAt)).Select(minion => minion.MinionId));
            yield return LoadStagedScene(content);
        }

        internal static IEnumerator WithEmptyBattleSpawn(SceneCardContent content, bool hasReachableTarget = true)
        {
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var own = WhisperSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var battle = map.Zones.Single(zone => zone.IsStartingBattleZone).Id;
            var spawn = map.SpawnPoints.Where(point => point.UnitType != "hero")
                .Select(point => map.Spaces.Single(space => space.Id.Equals(point.SpaceId)))
                .First(space => space.ZoneId.HasValue && space.ZoneId.Value.Equals(battle));
            var playable = map.Spaces.Where(space => space.Kind == BoardMapSpaceKind.Playable).Select(space => space.Coordinate).ToArray();
            var ownAt = spawn.Coordinate.GetNeighbors().First(playable.Contains);
            var victimAt = spawn.Coordinate.GetNeighbors().First(space => playable.Contains(space) && HexDistanceQuery.Calculate(space, ownAt) == 2);
            var enemyAt = playable.First(space => HexDistanceQuery.Calculate(space, ownAt) == (hasReachableTarget ? 3 : 4) && !space.Equals(victimAt));
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(own, ownAt), new SandboxHeroStaging(enemy, enemyAt)
            }, removedMinions: runtime.Setup.Minions.Select(minion => minion.MinionId),
                spawnedMinions: hasReachableTarget ? new[] { new SandboxMinionStaging(new MinionId("audit-step-victim"),
                    runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(enemy)).TeamSide, StandardMinionType.Melee, victimAt) } : new SandboxMinionStaging[0]);
            yield return LoadStagedScene(content);
        }
    }
}
