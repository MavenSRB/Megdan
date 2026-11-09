using System;
using System.Linq;
using System.Reflection;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class StandardMinionViewTests
    {
        [Test]
        public void ConfiguredIdentity_SurvivesLossOfTransientSetupState()
        {
            var minionObject = new GameObject("Reloaded Minion");
            try
            {
                var state = CreateState("reload-safe-minion");
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);

                ReflectedMember.RequiredInstanceField(typeof(StandardMinionView), "occupancy")
                    .SetValue(minion, null);

                Assert.That(minion.Occupancy, Is.Null,
                    "The regression must exercise the transient setup-state loss seen during editor backup restoration.");
                Assert.That(minion.BoundMinionId, Is.EqualTo(state.MinionId),
                    "Serialized binding identity must survive independently of transient setup state.");
            }
            finally
            {
                Object.DestroyImmediate(minionObject);
            }
        }

        [Test]
        public void PassiveBoardView_RebindsReplacementBeforeRefreshingProjectedMinions()
        {
            var originalObject = new GameObject("Original Minion");
            var replacementObject = new GameObject("Replacement Minion");
            var tileObject = new GameObject("Replacement Tile");
            var material = new Material(Shader.Find("Standard"));
            HexCornerHighlightTemplate template = null;
            try
            {
                var state = CreateState("replaceable-minion");
                var original = originalObject.AddComponent<StandardMinionView>();
                original.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(state.Coordinate, HexWorldLayout.DefaultOuterRadius);
                template = new HexCornerHighlightTemplate(HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    new[] { original },
                    sharedCornerHighlightTemplate: template);
                var model = new BoardInteractionViewModel(
                    1,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    new[] { state.MinionId },
                    default,
                    pieceTargetCues: new[]
                    {
                        new PieceTargetCueViewModel(
                            BoardObjectReference.ForMinion(state.MinionId),
                            BoardTargetCueState.Eligible,
                            BoardTargetConsequenceIntent.Attack)
                    });
                view.Render(model);

                Object.DestroyImmediate(originalObject);
                var replacement = replacementObject.AddComponent<StandardMinionView>();
                replacement.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var selector = replacementObject.AddComponent<StandardMinionPointerSelector>();
                MinionId submitted = default;
                view.BindIntentHandlers(_ => false, id => { submitted = id; return true; }, _ => false);

                Assert.DoesNotThrow(() => view.ReplaceMinionViews(new[] { replacement }),
                    "Replacing stale cached components must reconcile the current projection atomically.");
                selector.Configure(view);

                Assert.That(view.MinionViews, Is.EquivalentTo(new[] { replacement }));
                Assert.That(replacementObject.activeSelf, Is.True);
                // Milestone 8.62, at the user's direction: the piece carries the projection's own cue
                // again ("they are almost impossible to see like this"), and a REPLACEMENT view is
                // reconciled into the standing projection, cue included.
                Assert.That(replacement.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
                Assert.That(replacement.TargetRing.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece),
                    "A rebound view draws the standing cue rather than waiting for the next change.");
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack),
                    "A replacement view is rebound before its identity-keyed blood-red corners redraw.");
                Assert.That(selector.TrySelectFromPointerRay(
                    new Ray(replacement.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True);
                Assert.That(submitted, Is.EqualTo(state.MinionId));
            }
            finally
            {
                template?.Dispose();
                Object.DestroyImmediate(material);
                if (originalObject != null) Object.DestroyImmediate(originalObject);
                Object.DestroyImmediate(replacementObject);
                Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void PassiveBoardView_UsesProjectedMinionIdentityForCuePointerAndRemoval()
        {
            var minionObject = new GameObject("Projected Minion");
            var tileObject = new GameObject("Projected Minion Tile");
            var material = new Material(Shader.Find("Standard"));
            var priorScreenUp = BoardChoiceCue.ScreenUpInWorld;
            HexCornerHighlightTemplate template = null;
            try
            {
                BoardChoiceCue.SetScreenUp(new Vector3(1f, 0f, 1f));
                var state = MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                    new MinionId("blue-target"),
                    new TeamId("blue-team"),
                    TeamSide.Blue,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId("blue-target-spawn")),
                    new ConfiguredSpaceReference(new MapId("test-map"), new SpaceId("q1_r0")),
                    new HexCoordinate(1, 0)));
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var spriteRenderer = minion.GetComponent<SpriteRenderer>();
                var sourceSprite = spriteRenderer.sprite;
                var sourceMaterial = spriteRenderer.sharedMaterial;
                var sourceColor = spriteRenderer.color;
                var selector = minionObject.AddComponent<StandardMinionPointerSelector>();
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(state.Coordinate, HexWorldLayout.DefaultOuterRadius);
                template = new HexCornerHighlightTemplate(HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    new[] { minion },
                    sharedCornerHighlightTemplate: template);
                MinionId submitted = default;
                view.BindIntentHandlers(_ => false, id => { submitted = id; return true; }, _ => false);
                selector.Configure(view);

                view.Render(new BoardInteractionViewModel(
                    1,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    new[] { state.MinionId },
                    default,
                    pieceTargetCues: new[]
                    {
                        new PieceTargetCueViewModel(
                            BoardObjectReference.ForMinion(state.MinionId),
                            BoardTargetCueState.Eligible,
                            BoardTargetConsequenceIntent.Attack)
                    }));

                var ring = minion.TargetRing;
                // Milestone 8.62, at the user's direction, reversing 8.30h for this one cue: the
                // targeted PIECE is ringed and washed in the operation's own colour, and the hex's
                // six corner marks below are exactly what they were.
                Assert.That(ring.State,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
                Assert.That(ring.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Attack)));
                Assert.That(ring.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece),
                    "The piece a player has to find is named on its own space now: " +
                    "the six marks below carry the cue, in the target's own side colour.");
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack));
                Assert.That(tile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
                Assert.That(spriteRenderer.sprite, Is.SameAs(sourceSprite));
                Assert.That(spriteRenderer.sharedMaterial, Is.SameAs(sourceMaterial));
                Assert.That(spriteRenderer.color, Is.EqualTo(sourceColor));
                Assert.That(spriteRenderer.enabled, Is.True,
                    "Corner marks never replace, tint, or hide the actual minion sprite.");
                Assert.That(ring.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(minion.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                    "An Attack target builds no legacy triangle TextMesh or backing.");
                Assert.That(tile.IsHighlighted, Is.False,
                    "A piece target uses corners without a destination fill.");
                Assert.That(selector.TrySelectFromPointerRay(
                    new Ray(minion.transform.position + (Vector3.up * 10f), Vector3.down)), Is.True);
                Assert.That(submitted, Is.EqualTo(state.MinionId));

                view.Render(new BoardInteractionViewModel(
                    2,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default,
                    pieceTargetCues: new[]
                    {
                        new PieceTargetCueViewModel(
                            BoardObjectReference.ForMinion(state.MinionId),
                            BoardTargetCueState.Selected,
                            BoardTargetConsequenceIntent.Removal)
                    }));

                Assert.That(minion.TargetRing, Is.SameAs(ring));
                Assert.That(ring.State,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Selected)));
                Assert.That(ring.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Removal)));
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.Attack),
                    "Selected removal retains the shared blood-red corner language.");

                view.Render(new BoardInteractionViewModel(
                    3,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    Array.Empty<MinionOccupancy>(),
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                Assert.That(minionObject.activeSelf, Is.False);
                Assert.That(minion.TargetRing, Is.SameAs(ring));
                Assert.That(ring.State,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.None)));
                Assert.That(ring.IsVisible, Is.False,
                    "Removal clears the retained cue before the piece view is deactivated.");
                Assert.That(tile.IsHighlighted, Is.False);
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));

                view.Render(new BoardInteractionViewModel(
                    4,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default));

                Assert.That(minionObject.activeSelf, Is.True,
                    "A later authoritative projection such as Undo/restore must re-enable the retained view.");
                Assert.That(tile.IsHighlighted, Is.False);
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
                Assert.That(minion.TargetRing, Is.SameAs(ring));
                Assert.That(ring.State, Is.EqualTo(BoardTargetCueState.None),
                    "Restore redraws the piece without reviving a cue absent from that projection.");
            }
            finally
            {
                BoardChoiceCue.SetScreenUp(priorScreenUp);
                template?.Dispose();
                UnityEngine.Object.DestroyImmediate(material);
                UnityEngine.Object.DestroyImmediate(minionObject);
                UnityEngine.Object.DestroyImmediate(tileObject);
            }
        }

        [Test]
        public void PassiveBoardView_RendersDefenseContributionOnTheHexAndRetiresTheFloatingBadge()
        {
            var minionObject = new GameObject("Defense Cue Minion");
            var tileObject = new GameObject("Defense Cue Tile");
            var material = new Material(Shader.Find("Standard"));
            HexCornerHighlightTemplate template = null;
            try
            {
                var state = CreateState("defense-cue-minion");
                var minion = minionObject.AddComponent<StandardMinionView>();
                minion.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var tile = tileObject.AddComponent<HexTileView>();
                tile.Configure(state.Coordinate, HexWorldLayout.DefaultOuterRadius);
                template = new HexCornerHighlightTemplate(HexWorldLayout.DefaultOuterRadius);
                var view = new BoardInteractionUnityView(
                    Array.Empty<PrimitiveHeroView>(),
                    new[] { tile },
                    material,
                    new[] { minion },
                    sharedCornerHighlightTemplate: template);

                view.Render(new BoardInteractionViewModel(
                    1,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default,
                    minionDefenseCues: new[] { new MinionDefenseCueViewModel(state.MinionId, -1) }));

                Assert.That(minion.GetComponentsInChildren<TextMesh>(true), Is.Empty,
                    "The floating signed badge and oval glyph backing are retired.");
                Assert.That(tile.CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.DefenseNegative));
                Assert.That(tile.CornerHighlight.VisibleMarkCount, Is.EqualTo(6));
                Assert.That(minion.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                    "A Defense modifier source is not a selectable target cue.");
                Assert.That(minion.TargetRing.IsVisible, Is.False);
                Assert.That(tile.IsHighlighted, Is.False);

                view.Render(new BoardInteractionViewModel(
                    2,
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroOccupancy>(),
                    new[] { state },
                    Array.Empty<HexCoordinate>(),
                    Array.Empty<GuardsDigital.Core.Units.Heroes.HeroId>(),
                    Array.Empty<MinionId>(),
                    default));
                Assert.That(tile.CornerHighlight.Kind, Is.EqualTo(HexCornerHighlightKind.None));
                Assert.That(minion.GetComponentsInChildren<TextMesh>(true), Is.Empty);
            }
            finally
            {
                template?.Dispose();
                Object.DestroyImmediate(material);
                Object.DestroyImmediate(minionObject);
                Object.DestroyImmediate(tileObject);
            }
        }

        [TestCase(TeamSide.Red, StandardMinionType.Heavy, "Minions/red-heavy")]
        [TestCase(TeamSide.Red, StandardMinionType.Melee, "Minions/red-melee")]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, "Minions/red-ranged")]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy, "Minions/blue-heavy")]
        [TestCase(TeamSide.Blue, StandardMinionType.Melee, "Minions/blue-melee")]
        [TestCase(TeamSide.Blue, StandardMinionType.Ranged, "Minions/blue-ranged")]
        public void ArtCatalog_SelectsReviewedCircularArtFromAuthoritativeTeamAndType(
            TeamSide teamSide,
            StandardMinionType minionType,
            string expectedResourcePath)
        {
            Assert.That(StandardMinionArtCatalog.GetResourcePath(teamSide, minionType), Is.EqualTo(expectedResourcePath));
            Assert.That(StandardMinionArtCatalog.Load(teamSide, minionType), Is.Not.Null);
        }

        [TestCase(TeamSide.Red, StandardMinionType.Heavy, "Private/Megdan/MinionStandees/red-heavy")]
        [TestCase(TeamSide.Red, StandardMinionType.Melee, "Private/Megdan/MinionStandees/red-melee")]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged, "Private/Megdan/MinionStandees/red-ranged")]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy, "Private/Megdan/MinionStandees/blue-heavy")]
        [TestCase(TeamSide.Blue, StandardMinionType.Melee, "Private/Megdan/MinionStandees/blue-melee")]
        [TestCase(TeamSide.Blue, StandardMinionType.Ranged, "Private/Megdan/MinionStandees/blue-ranged")]
        public void StandeeCatalog_MapsEveryTeamAndTypeWithoutReplacingTheFlatIcon(
            TeamSide teamSide,
            StandardMinionType minionType,
            string expectedStandeePath)
        {
            Assert.That(StandardMinionArtCatalog.GetStandeeResourcePath(teamSide, minionType),
                Is.EqualTo(expectedStandeePath));
            Assert.That(Resources.Load<Texture2D>(expectedStandeePath), Is.Not.Null);
            Assert.That(Resources.Load<TextAsset>(expectedStandeePath), Is.Not.Null);
            Assert.That(StandardMinionArtCatalog.Load(teamSide, minionType), Is.Not.Null,
                "The old 2D icon remains independently available.");
        }

        [TestCase(TeamSide.Red, StandardMinionType.Heavy)]
        [TestCase(TeamSide.Red, StandardMinionType.Melee)]
        [TestCase(TeamSide.Red, StandardMinionType.Ranged)]
        [TestCase(TeamSide.Blue, StandardMinionType.Heavy)]
        [TestCase(TeamSide.Blue, StandardMinionType.Melee)]
        [TestCase(TeamSide.Blue, StandardMinionType.Ranged)]
        public void AngledViewUsesVeljkoStandeeAndTopDownRestoresTheSameFlatIcon(
            TeamSide teamSide,
            StandardMinionType minionType)
        {
            var minionObject = new GameObject("standee minion");
            var cameraObject = new GameObject("standee camera", typeof(Camera));
            try
            {
                var state = CreateState($"{teamSide}-{minionType}", teamSide, minionType);
                var view = minionObject.AddComponent<StandardMinionView>();
                view.Configure(state, HexWorldLayout.DefaultOuterRadius);
                var renderer = minionObject.GetComponent<SpriteRenderer>();
                var flatIcon = renderer.sprite;
                var camera = cameraObject.GetComponent<Camera>();

                Assert.That(view.Standee.HasArtwork, Is.True);
                camera.transform.rotation = Quaternion.Euler(55f, 135f, 0f);
                view.Standee.Refresh(camera);
                Assert.That(view.Standee.IsStanding, Is.True);
                Assert.That(renderer.enabled, Is.False,
                    "The old flat icon is hidden, not overwritten, while the panel stands.");
                Assert.That(view.TokenBodyRenderer.enabled, Is.True,
                    "The authoritative team-colour base remains under the standee.");

                camera.transform.rotation = Quaternion.Euler(70.1f, 0f, 0f);
                view.Standee.Refresh(camera);
                Assert.That(view.Standee.IsStanding, Is.False);
                Assert.That(renderer.enabled, Is.True);
                Assert.That(renderer.sprite, Is.SameAs(flatIcon),
                    "Top-down returns to the exact pre-existing 2D icon.");
            }
            finally
            {
                Object.DestroyImmediate(minionObject);
                Object.DestroyImmediate(cameraObject);
            }
        }

        [Test]
        public void Configure_ProjectsStableAuthoritativeIdentityAndPlacementWithoutReadingArtMetadata()
        {
            var gameObject = new GameObject("Minion View Test");
            try
            {
                var view = gameObject.AddComponent<StandardMinionView>();
                var state = MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                    new MinionId("blue-melee-01"),
                    new TeamId("blue-team"),
                    TeamSide.Blue,
                    StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(new MapId("official-single-lane"), new SpawnPointId("blue-melee-minion-03")),
                    new ConfiguredSpaceReference(new MapId("official-single-lane"), new SpaceId("q-3_r2")),
                    new HexCoordinate(-3, 2)));

                view.Configure(state, HexWorldLayout.DefaultOuterRadius);

                Assert.That(view.Occupancy, Is.SameAs(state));
                Assert.That(view.Occupancy.TeamSide, Is.EqualTo(TeamSide.Blue));
                Assert.That(view.Occupancy.MinionType, Is.EqualTo(StandardMinionType.Melee));
                Assert.That(view.GetComponent<SpriteRenderer>().sprite.name, Is.EqualTo("blue-melee"));
                Assert.That(view.transform.localPosition,
                    Is.EqualTo(HexWorldLayout.ToWorld(state.Coordinate) + (Vector3.up * StandardMinionView.DefaultVerticalOffset)));
                // Milestone 8.11a: the token takes the shared board-facing rotation rather than a
                // copy of it, so this asks the shared construction what up is. It is world +Z while
                // nothing is underneath the board, and the board's own up once the artwork is.
                Assert.That(
                    Vector3.Angle(
                        view.transform.localRotation * Vector3.up,
                        BoardChoiceCue.ScreenUpInWorld),
                    Is.LessThan(0.0001f));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private static MinionOccupancy CreateState(string id) =>
            CreateState(id, TeamSide.Blue, StandardMinionType.Melee);

        private static MinionOccupancy CreateState(
            string id,
            TeamSide teamSide,
            StandardMinionType minionType) =>
            MinionOccupancy.FromSetup(new QuickGameMinionSetupState(
                new MinionId(id),
                new TeamId(teamSide == TeamSide.Blue ? "blue-team" : "red-team"),
                teamSide,
                minionType,
                new ConfiguredSpawnPointReference(new MapId("test-map"), new SpawnPointId($"{id}-spawn")),
                new ConfiguredSpaceReference(new MapId("test-map"), new SpaceId("q1_r0")),
                new HexCoordinate(1, 0)));

        private static void AssertColor(Color actual, Color expected)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1f / 255f));
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1f / 255f));
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1f / 255f));
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(1f / 255f));
        }
    }
}
