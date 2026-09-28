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
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.SnorriUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.112g. Mrak dealt into a loaded scene: his prepared hand with the three-badge and
    /// Defense-and-Attack rails, his printed secondary Attack reachable from a real hand with its CLEAR
    /// chip beside the swords, and Stone Grip's rocks drawn with their prepared face toward the camera.
    /// Each case also writes a captured frame for inspection.
    /// </summary>
    public sealed class MrakDealingPlayModeTests
    {
        private static HeroId Mrak(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "mrak").HeroId;

        private static HeroId Wasp(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "wasp").HeroId;

        [UnityTest]
        public IEnumerator StoneGripsRocks_StandOnTheBoardWithTheirPreparedFaceTowardTheCamera()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMrak);
            var root = Root();
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);
            var wasp = Wasp(runtime);

            yield return PointTrayAt(root, mrak);
            yield return UiPlayer.CaptureScreen("tmp/8112g-mrak-starting-hand-1920x1080.png");

            yield return CommitEverySeat(root, mrak, NameOf(runtime, MrakCardContent.StoneGrip));
            yield return WaitForActor(root, mrak);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return Target(root, wasp);

            // A tie among the farthest spaces is Mrak's question (MR-Q3); answer it with the first
            // offered space until the placement is done.
            for (var budget = 0; budget < 4 && runtime.PendingEffectChoice != null; budget++)
            {
                var space = runtime.PendingEffectChoice.Candidates.First(value =>
                    value.Value.Kind == EffectValueKind.Coordinate);
                yield return Pick(root, space);
            }

            yield return null;
            var rocks = Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Where(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(MrakComponentContent.RockId))
                .ToArray();
            Assert.That(rocks, Has.Length.EqualTo(3), "Stone Grip places exactly three rocks. " +
                Remaining(runtime));
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            foreach (var rock in rocks)
            {
                Assert.That(rock.NameText, Is.Empty, "the prepared rock needs no caption");
                Assert.That(rock.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                    "the rock draws its prepared face, not the fallback disc");
                var mesh = rock.GetComponent<MeshFilter>().sharedMesh;
                var face = mesh.normals
                    .Select(normal => rock.transform.TransformDirection(normal).normalized)
                    .OrderByDescending(normal => normal.y)
                    .First();
                var towardCamera = (camera.transform.position - rock.transform.position).normalized;
                Assert.That(Vector3.Dot(face, towardCamera), Is.GreaterThan(0f),
                    "the rock's printed face must point at the camera (8.102's Turret lesson)");
            }

            yield return UiPlayer.CaptureScreen("tmp/8112g-mrak-rocks-1920x1080.png");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EpicentresSecondaryAttack_IsAReachableRailControl_WithItsClearBesideTheSwords()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedMrak);
            var root = Root();
            var runtime = root.Runtime;
            var mrak = Mrak(runtime);

            yield return PointTrayAt(root, mrak);
            yield return UiPlayer.CaptureScreen("tmp/8112g-mrak-tier3-hand-1920x1080.png");

            yield return CommitEverySeat(root, mrak, NameOf(runtime, MrakRedFaces.Epicentre));
            yield return WaitForActor(root, mrak);
            yield return null;

            var attack = runtime.RoundView.Model.Actions.SingleOrDefault(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(attack, Is.Not.Null,
                "Epicentre's primary is a Skill, so its one Attack control is the printed secondary. " +
                UiPlayer.Describe(root.Hud));
            Assert.That(attack.Command.ActionDefinitionId, Is.EqualTo(MrakCardContent.SecondaryAttackId));
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>()
                    .Any(button => button.text == "CLEAR"), Is.True,
                "the secondary Attack's CLEAR chip is drawn beside its badge. " +
                UiPlayer.Describe(root.Hud));
            yield return UiPlayer.CaptureScreen("tmp/8112g-mrak-secondary-attack-1920x1080.png");

            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice != null || runtime.PendingDefenseChoice != null,
                Is.True, "the printed secondary Attack started from the real control. " +
                Remaining(runtime));
            Healthy(runtime);
        }
    }
}
