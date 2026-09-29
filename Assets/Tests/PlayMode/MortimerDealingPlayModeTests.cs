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
    /// Milestone 9.8f. Mortimer dealt into a loaded scene: his prepared starting hand, Awaken!'s four
    /// Zombies drawn with their prepared face toward the camera, and his Tier III hand with Master of
    /// Puppets faceup, Awaken! started from the real control. Each case also writes a captured frame.
    /// </summary>
    public sealed class MortimerDealingPlayModeTests
    {
        private static HeroId Mortimer(GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "mortimer").HeroId;

        [UnityTest]
        public IEnumerator AwakensZombies_StandOnTheBoardWithTheirPreparedFaceTowardTheCamera()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedMortimer);
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);

            yield return PointTrayAt(root, mortimer);
            yield return UiPlayer.CaptureScreen("tmp/98f-mortimer-starting-hand-1920x1080.png");

            yield return CommitEverySeat(root, mortimer, NameOf(runtime, MortimerCardContent.Awaken));
            yield return WaitForActor(root, mortimer);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // Four optional placements into spawn points in Radius 4 or spaces beside him; with the
            // full supply no reclaim is owed. Take each placement on the first clickable space.
            for (var budget = 0; budget < 10 && runtime.PendingEffectChoice != null; budget++)
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
            var zombies = Object.FindObjectsByType<PlacedTokenView>(FindObjectsSortMode.None)
                .Where(view => view.Token != null &&
                    view.Token.Definition.Id.Equals(MortimerComponentContent.ZombieId))
                .ToArray();
            Assert.That(zombies, Has.Length.EqualTo(4), "Awaken! may place four Zombies. " +
                Remaining(runtime));
            var camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            foreach (var zombie in zombies)
            {
                Assert.That(zombie.NameText, Is.Empty, "the prepared Zombie needs no caption");
                Assert.That(zombie.GetComponent<MeshRenderer>().sharedMaterial.mainTexture, Is.Not.Null,
                    "the Zombie draws its prepared face, not the fallback disc");
                var mesh = zombie.GetComponent<MeshFilter>().sharedMesh;
                var face = mesh.normals
                    .Select(normal => zombie.transform.TransformDirection(normal).normalized)
                    .OrderByDescending(normal => normal.y)
                    .First();
                var towardCamera = (camera.transform.position - zombie.transform.position).normalized;
                Assert.That(Vector3.Dot(face, towardCamera), Is.GreaterThan(0f),
                    "the Zombie's printed face must point at the camera (8.102's Turret lesson)");
            }

            yield return UiPlayer.CaptureScreen("tmp/98f-mortimer-zombies-1920x1080.png");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator TheTierThreeHand_WithMasterOfPuppetsFaceup_StartsAwakenFromItsControl()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedMortimer);
            var root = Root();
            var runtime = root.Runtime;
            var mortimer = Mortimer(runtime);

            yield return PointTrayAt(root, mortimer);
            yield return UiPlayer.CaptureScreen("tmp/98f-mortimer-tier3-hand-1920x1080.png");

            yield return CommitEverySeat(root, mortimer, NameOf(runtime, MortimerCardContent.Awaken));
            yield return WaitForActor(root, mortimer);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Awaken! started from the real control and asks where the first Zombie goes. " +
                Remaining(runtime));
            yield return UiPlayer.CaptureScreen("tmp/98f-mortimer-tier3-awaken-1920x1080.png");
            Healthy(runtime);
        }
    }
}
