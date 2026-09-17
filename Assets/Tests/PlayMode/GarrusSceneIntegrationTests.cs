using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class GarrusSceneIntegrationTests
    {
        [UnityTest]
        public IEnumerator EveryStagedBranchSpawnsGarrusWithHisOwnArtworkAndPointer()
        {
            foreach (var content in new[] { SceneCardContent.VerifiedEngagedStagedGarrus,
                SceneCardContent.VerifiedTierTwoEngagedStagedGarrus,
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedGarrus,
                SceneCardContent.VerifiedTierThreeEngagedStagedGarrus,
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedGarrus })
            {
                yield return LoadStagedScene(content);
                var root = Root();
                var setup = root.Runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "garrus");
                var views = Object.FindObjectsByType<PrimitiveHeroView>();
                Assert.That(views, Has.Length.EqualTo(2));
                var view = views.Single(value => value.Occupancy.HeroId.Equals(setup.HeroId));
                Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("garrus"));
                Assert.That(view.HasTokenVisual, Is.True);
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, view.transform.position), Is.True);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryGarrusFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedGarrus);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Garrus", GarrusCardContent.AllDefinitions, Root().Hud);
        }
    }
}
