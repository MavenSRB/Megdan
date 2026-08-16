using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 6.2d in the saved configured scene: a token a player places is drawn on the board.
    ///
    /// Three layers were empty before this milestone and closing any one of them alone changed nothing
    /// a player could see, so this fixture crosses all three in one run. The scene could not hold a
    /// token at all, because the only capability that places one lives on milestone 6.2's Cache card
    /// and the scene dealt the ordinary five; the projection carried no token, by 6.2's own decision;
    /// and the whole Presentation assembly named a token nowhere. Here the Blue hero spends a turn on
    /// the Cache Skill exactly as an ordinary match would, and the board draws what it placed.
    ///
    /// The view is asserted through the configured camera rather than by its local offset, which is
    /// the lesson milestone 6.5c recorded when the hero choice cue shipped invisible: the board is the
    /// XZ plane under a top-down camera, so a piece at the wrong orientation renders as a sliver while
    /// every local-offset assertion about it still passes.
    /// </summary>
    public sealed class PlacedTokenPlayModeTests
    {
        private static readonly ActionDefinitionId CacheSkill =
            new ActionDefinitionId("primary-component-skill");

        private static readonly EffectNodeId PlaceBranch =
            new EffectNodeId("component-place-branch");

        [UnityTest]
        public IEnumerator Scene_DrawsATokenAPlayerPlacedAndTakesItBackOnUndo()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            Assert.That(runtime.Cards, Has.Count.EqualTo(12),
                "The scene deals milestone 6.2's Cache card beside the ordinary five, which is the " +
                "only way a player can put a token on this board.");
            Assert.That(runtime.BoardView.TokenViews, Is.Empty,
                "Nothing has been placed, so the board has built no token view at all.");

            var placed = PlaceAHazard(runtime);
            yield return null;

            Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Coordinate, Is.EqualTo(placed));
            var view = runtime.BoardView.TokenViews.Single();
            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(view.Coordinate, Is.EqualTo(placed));
            Assert.That(view.NameText, Is.EqualTo("TEST - HAZARD"),
                "The piece says what it is, from the definition the projection carries with it.");

            var camera = Object.FindAnyObjectByType<Camera>();
            var tile = runtime.BoardView.TileViews.Single(value => value.Coordinate.Equals(placed));
            var tokenOnScreen = camera.WorldToViewportPoint(view.transform.position);
            var tileOnScreen = camera.WorldToViewportPoint(tile.transform.position);
            Assert.That(tokenOnScreen.z, Is.GreaterThan(0f),
                "The piece is in front of the configured camera rather than behind it.");
            Assert.That(tokenOnScreen.x, Is.EqualTo(tileOnScreen.x).Within(0.01f));
            Assert.That(tokenOnScreen.y, Is.EqualTo(tileOnScreen.y).Within(0.01f),
                "It projects onto the space it stands on, which is what a player reads it as.");
            Assert.That(tokenOnScreen.x, Is.InRange(0f, 1f));
            Assert.That(tokenOnScreen.y, Is.InRange(0f, 1f),
                "And it is inside the framed board rather than off the edge of it.");

            var bounds = view.GetComponent<Renderer>().bounds;
            var lowLeft = camera.WorldToScreenPoint(bounds.min);
            var topRight = camera.WorldToScreenPoint(bounds.max);
            Assert.That(Mathf.Abs(topRight.x - lowLeft.x), Is.GreaterThan(4f));
            Assert.That(Mathf.Abs(topRight.y - lowLeft.y), Is.GreaterThan(4f),
                "It covers real screen area in both directions - an edge-on piece is a sliver in one " +
                "of them while every local-offset assertion still passes.");

            Assert.That(runtime.Undo(), Is.True);
            runtime.RefreshAll();
            yield return null;

            Assert.That(runtime.BoardView.Model.PlacedTokens, Is.Empty);
            Assert.That(runtime.BoardView.TokenViews.Single(), Is.SameAs(view),
                "The view is retained across the removal, exactly as a minion view is.");
            Assert.That(view.gameObject.activeSelf, Is.False,
                "Projection presence owns visibility, so the piece stops being drawn.");
        }

        /// <summary>
        /// The same placement, one turn on: with a Hazard standing beside it, Blue Sentinel's Skill is
        /// offered rather than greyed, and the control that names the token reads as the token. Both
        /// were milestone 6.6a's handoff finding - two of its three modes named and changed a board
        /// object that the board drew nothing for, and the prompt called it `TOKEN #1`.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_OffersTheExchangeSkillOnceATokenStandsAndNamesItByWhatItIs()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var placed = PlaceAHazard(runtime);
            yield return null;

            CommitSentinelTurn(runtime);
            var sentinelSkill = new ActionDefinitionId("primary-exchange-skill");
            Assert.That(runtime.RoundView.Model.UnavailableActions
                    .Any(value => value.ActionDefinitionId.Equals(sentinelSkill)),
                Is.False,
                "A mode now has a piece to act on, so milestone 6.5af no longer refuses the action.");
            Assert.That(runtime.SubmitAction(sentinelSkill), Is.True);
            yield return null;

            // Take whichever token-naming mode the scene's geography leaves performable, and read the
            // control it offers for the piece.
            var mode = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(runtime.RoundPresenter.Submit(mode), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            var target = runtime.RoundView.Model.EffectChoiceTargets
                .Single(value => value.Space.Equals(placed));
            Assert.That(target.Label, Is.EqualTo("TEST - HAZARD"),
                "Milestone 6.5c settled for TOKEN #1 because there was no projection to read a name " +
                "from; there is one now, and one Hazard needs no ordinal to be told apart.");
        }

        /// <summary>
        /// Spends the opening round on the Cache Skill and returns the space its Hazard took. Red
        /// commits its lowest card, so the Blue hero acts second on that round and the placement is the
        /// last thing to happen in it.
        /// </summary>
        private static HexCoordinate PlaceAHazard(ConfiguredMatchSceneRuntime runtime)
        {
            CommitTurn(runtime, "blue-cache-card");
            if (!runtime.SelectedActor.CardInstanceId.Value.StartsWith("blue-cache"))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The Red actor completes by Hold first; the Cache card sits below its band.");
            }

            Assert.That(runtime.SubmitAction(CacheSkill), Is.True);
            Assert.That(runtime.SubmitEffectChoiceOption(PlaceBranch), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True,
                "A mode keeps the explicit confirm milestone 6.8a gave it; only a value candidate " +
                "is answered in one click.");

            // The supply is untouched, so p.18's `if able` reclamation step skips and the placement
            // is the next decision.
            var space = runtime.PendingEffectChoice.Candidates[0].Value.Coordinate;
            Assert.That(runtime.SubmitEffectChoiceValue(space), Is.True);
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "An empty destination is answered by the click that takes it.");
            return space;
        }

        /// <summary>Commits Blue Sentinel against the Red hand's lowest remaining card.</summary>
        private static void CommitSentinelTurn(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            while (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
            {
                Assert.That(
                    runtime.RoundPresenter.Submit(runtime.RoundView.Model.ContinueEndOfRoundInvocation),
                    Is.True);
            }

            CommitTurn(runtime, "blue-sentinel-card");
            Assert.That(runtime.SelectedActor.CardInstanceId.Value, Is.EqualTo("blue-sentinel-card"),
                "Sentinel at Initiative 11 outranks every Red card left in the hand.");
        }

        /// <summary>
        /// Commits the named Blue card against the Red hand's lowest, which is how every showcase
        /// fixture reaches its card whatever the conversions have done to the top of either band.
        /// </summary>
        private static void CommitTurn(ConfiguredMatchSceneRuntime runtime, string blueCardId)
        {
            var blue = runtime.Cards.Single(card => card.Id.Value == blueCardId);
            var red = runtime.Cards
                .Where(card => runtime.Setup.Heroes
                    .Single(hero => hero.TeamSide == TeamSide.Red).HeroId.Equals(card.OwnerId) &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();

            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.PendingInitiativeChoice, Is.Null,
                "No Red card ties either of the Blue cards this fixture commits.");
        }

        private static IEnumerator LoadScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }
    }
}
