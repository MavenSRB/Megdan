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
    public sealed class WidgetSceneIntegrationTests
    {
        [UnityTest]
        public IEnumerator EveryStagedBranchSpawnsWidgetWithHisOwnArtworkAndPointer()
        {
            foreach (var content in new[] { SceneCardContent.VerifiedEngagedStagedWidget,
                SceneCardContent.VerifiedTierTwoEngagedStagedWidget,
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWidget,
                SceneCardContent.VerifiedTierThreeEngagedStagedWidget,
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWidget })
            {
                yield return LoadStagedScene(content);
                var root = Root();
                var setup = root.Runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "widget");
                var views = Object.FindObjectsByType<PrimitiveHeroView>();
                Assert.That(views, Has.Length.EqualTo(2));
                var view = views.Single(value => value.Occupancy.HeroId.Equals(setup.HeroId));
                Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("widget"));
                Assert.That(view.HasTokenVisual, Is.True);
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, view.transform.position), Is.True);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator EveryWidgetFaceUsesTheStandardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWidget);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Widget", WidgetCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator DragonBondPlacesPyrosOwnBoardArtworkThroughPlayerInput()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedWidget);
            var root = Root();
            var actor = root.Runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "widget").HeroId;
            yield return CommitEverySeat(root, actor, "Dragon Bond");
            yield return GarrusUiJourneyTests.WaitForActor(root, actor);
            yield return PointTrayAt(root, actor);
            yield return StartActionThroughControl(root, GuardsDigital.Core.Cards.ActionKind.Skill);
            var option = root.Runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == GuardsDigital.Core.Rules.EffectResolution.EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.OptionNodeId.Value == "place-pyro");
            UiPlayer.Click(UiPlayer.Button(root.Hud, option.Label));
            yield return null;
            yield return GarrusUiJourneyTests.Space(root);
            var token = Object.FindObjectsByType<GuardsDigital.Presentation.Board.PlacedTokenView>().Single();
            Assert.That(token.NameText, Is.Empty, "Pyro's art has no overprinted caption.");
            Assert.That(token.GetComponent<MeshRenderer>().sharedMaterial.mainTexture,
                Is.SameAs(Resources.Load<Texture2D>("Private/Tokens/widget-pyro")));
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/widget"),
                Is.Not.SameAs(token.GetComponent<MeshRenderer>().sharedMaterial.mainTexture));
            yield return UiPlayer.CaptureGameView("widget-f", "widget-and-pyro");
        }
    }
}
