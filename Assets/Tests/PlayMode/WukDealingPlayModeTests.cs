using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.114f. Wuk dealt into a loaded scene: his prepared starting hand, Mystic Saplings'
    /// three Trees drawn with their prepared face toward the camera, and his Tier III hand with Angry
    /// Stampede's Movement primary offered as a real control. Each case also writes a captured frame.
    /// </summary>
    public sealed class WukDealingPlayModeTests
    {
        private static HeroId Wuk(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "wuk").HeroId;

        [UnityTest]
        public IEnumerator MysticSaplingsTrees_StandOnTheBoardWithTheirPreparedFaceTowardTheCamera()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWuk);
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);

            yield return PointTrayAt(root, wuk);
            yield return UiPlayer.CaptureScreen("tmp/8114f-wuk-starting-hand-1920x1080.png");

            yield return CommitEverySeat(root, wuk, NameOf(runtime, WukCardContent.MysticSaplings));
            yield return WaitForActor(root, wuk);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Three optional placements in his printed Radius 3; with the full supply no reclaim is
            // owed. Take each placement on the first clickable space it offers.
            for (var budget = 0; budget < 8 && runtime.PendingEffectChoice != null; budget++)
            {
                if (runtime.PendingEffectChoice.Candidates.Any(value =>
                        value.Value.Kind == EffectValueKind.Coordinate))
                {
                    yield return Space(root);
                }
                else
                {
                    yield return May(root, false);
                }
            }

            yield return null;
            var trees = Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Where(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(WukComponentContent.TreeId))
                .ToArray();
            Assert.That(trees, Has.Length.EqualTo(3), "Mystic Saplings may place three Trees. " +
                Remaining(runtime));
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            foreach (var tree in trees)
            {
                Assert.That(tree.NameText, Is.Empty, "the prepared Tree needs no caption");
                Assert.That(tree.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                    "the Tree draws its prepared face, not the fallback disc");
                var mesh = tree.GetComponent<MeshFilter>().sharedMesh;
                var face = mesh.normals
                    .Select(normal => tree.transform.TransformDirection(normal).normalized)
                    .OrderByDescending(normal => normal.y)
                    .First();
                var towardCamera = (camera.transform.position - tree.transform.position).normalized;
                Assert.That(Vector3.Dot(face, towardCamera), Is.GreaterThan(0f),
                    "the Tree's printed face must point at the camera (8.102's Turret lesson)");
            }

            yield return UiPlayer.CaptureScreen("tmp/8114f-wuk-trees-1920x1080.png");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AngryStampedesMovementPrimary_IsAReachableControlFromTheTierThreeHand()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedWuk);
            var root = Root();
            var runtime = root.Runtime;
            var wuk = Wuk(runtime);

            yield return PointTrayAt(root, wuk);
            yield return UiPlayer.CaptureScreen("tmp/8114f-wuk-tier3-hand-1920x1080.png");

            yield return CommitEverySeat(root, wuk, NameOf(runtime, WukRedFaces.AngryStampede));
            yield return WaitForActor(root, wuk);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            yield return null;

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "the Movement primary started from the real control, its text applied before the " +
                "walk. " + Remaining(runtime));
            yield return UiPlayer.CaptureScreen("tmp/8114f-wuk-stampede-walk-1920x1080.png");
            Healthy(runtime);
        }
    }
}
