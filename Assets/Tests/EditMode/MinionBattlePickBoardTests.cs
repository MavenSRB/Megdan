using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.29, the board half of the user's direction for the Minion Battle: "not full hex
    /// outline when minion is selectable, and full outline when a teammate or you have selected it",
    /// with the picking teammate's small token on the minion they picked and none on the viewer's own.
    /// </summary>
    public sealed class MinionBattlePickBoardTests
    {
        private static readonly HeroId Teammate = new HeroId("teammate-hero");

        private readonly List<Object> owned = new List<Object>();
        private HexCornerHighlightTemplate template;
        private BoardInteractionUnityView view;

        [TearDown]
        public void TearDown()
        {
            view?.ReleaseOwnedResources();
            view = null;
            template?.Dispose();
            template = null;
            foreach (var value in owned)
            {
                if (value != null) Object.DestroyImmediate(value);
            }

            owned.Clear();
        }

        [Test]
        public void APickedMinionKeepsTheFullOutline_APickableOneOnlyItsCorners_AndATeammatesPickWearsTheirToken()
        {
            var pickedState = Minion("blue-melee-71", new HexCoordinate(1, 0));
            var pickableState = Minion("blue-melee-72", new HexCoordinate(2, 0));
            var pickedTile = Tile(pickedState.Coordinate);
            var pickableTile = Tile(pickableState.Coordinate);
            var picked = MinionView(pickedState);
            var pickable = MinionView(pickableState);
            Build(new[] { pickedTile, pickableTile }, new[] { picked, pickable });

            view.Render(Model(
                new[] { pickedState, pickableState },
                new[]
                {
                    new MinionBattlePickViewModel(
                        BoardObjectReference.ForMinion(pickedState.MinionId), false, new[] { Teammate })
                }));

            Assert.That(pickedTile.CornerHighlight.IsFullEdgeOutline, Is.True,
                "A teammate's pick wears the full hex outline without the pointer on it.");
            Assert.That(pickableTile.CornerHighlight.IsFullEdgeOutline, Is.False,
                "A minion that may still be picked keeps only its corner marks.");
            Assert.That(pickableTile.CornerHighlight.Kind, Is.Not.EqualTo(HexCornerHighlightKind.None));
            Assert.That(picked.PickBadges, Is.Not.Null);
            Assert.That(picked.PickBadges.VisibleCount, Is.EqualTo(1),
                "One token for the one teammate who picked it.");
            Assert.That(pickable.PickBadges == null || pickable.PickBadges.VisibleCount == 0, Is.True,
                "Nobody picked this one, so it wears no token.");

            // The viewer picks the same minion too, and the teammate takes theirs back: the outline
            // stays - someone on the side still picked it - and the token goes, because the viewer's
            // own pick needs no reminder of who made it.
            view.Render(Model(
                new[] { pickedState, pickableState },
                new[]
                {
                    new MinionBattlePickViewModel(
                        BoardObjectReference.ForMinion(pickedState.MinionId), true, Array.Empty<HeroId>())
                },
                version: 2));

            Assert.That(pickedTile.CornerHighlight.IsFullEdgeOutline, Is.True);
            Assert.That(picked.PickBadges.VisibleCount, Is.Zero, "The viewer's own pick carries no token.");

            // And when nobody holds it any more, it is pickable again: corners, no outline, no token.
            view.Render(Model(
                new[] { pickedState, pickableState }, Array.Empty<MinionBattlePickViewModel>(), version: 3,
                pickedCue: BoardTargetCueState.Eligible));

            Assert.That(pickedTile.CornerHighlight.IsFullEdgeOutline, Is.False);
            Assert.That(picked.PickBadges.VisibleCount, Is.Zero);
        }

        private BoardInteractionViewModel Model(
            IReadOnlyList<MinionOccupancy> minions,
            IEnumerable<MinionBattlePickViewModel> picks,
            long version = 1,
            BoardTargetCueState pickedCue = BoardTargetCueState.Selected) =>
            new BoardInteractionViewModel(
                version,
                Array.Empty<HeroOccupancy>(),
                minions,
                Array.Empty<HexCoordinate>(),
                Array.Empty<HeroId>(),
                Array.Empty<MinionId>(),
                default,
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(minions[0].MinionId),
                        pickedCue,
                        BoardTargetConsequenceIntent.Removal),
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(minions[1].MinionId),
                        BoardTargetCueState.Eligible,
                        BoardTargetConsequenceIntent.Removal)
                },
                minionBattlePicks: picks);

        private static MinionOccupancy Minion(string id, HexCoordinate coordinate) =>
            MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId("blue-team"),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId(id + "-spawn")),
                new ConfiguredSpaceReference(
                    new MapId("test-map"), new SpaceId("q" + coordinate.Q + "_r" + coordinate.R)),
                coordinate));

        private HexTileView Tile(HexCoordinate coordinate)
        {
            var gameObject = new GameObject("Tile " + coordinate);
            owned.Add(gameObject);
            var tile = gameObject.AddComponent<HexTileView>();
            tile.Configure(coordinate, HexWorldLayout.DefaultOuterRadius);
            return tile;
        }

        private StandardMinionView MinionView(MinionOccupancy state)
        {
            var gameObject = new GameObject("Minion " + state.MinionId.Value);
            owned.Add(gameObject);
            var minion = gameObject.AddComponent<StandardMinionView>();
            minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
            return minion;
        }

        private void Build(IEnumerable<HexTileView> tiles, IEnumerable<StandardMinionView> minions)
        {
            template = new HexCornerHighlightTemplate(HexWorldLayout.DefaultOuterRadius);
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            owned.Add(material);
            view = new BoardInteractionUnityView(
                Array.Empty<PrimitiveHeroView>(), tiles, material, minions,
                sharedCornerHighlightTemplate: template);
        }
    }
}
