using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
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
    /// token at all, because the only capability that places one lived on milestone 6.2's Cache card
    /// and the scene dealt the ordinary five; the projection carried no token, by 6.2's own decision;
    /// and the whole Presentation assembly named a token nowhere. Here the Blue hero spends a turn on
    /// the Skill that places one exactly as an ordinary match would, and the board draws what it
    /// placed.
    ///
    /// That Skill is Blue Feint's since the scene went back to five cards per hero: the Cache card is
    /// no longer dealt, and Feint carries its three modes. The fixture's shape is unchanged, because
    /// Feint at Initiative 12 and Sentinel at 11 are two cards of one hand exactly as Cache and
    /// Sentinel were - which is the whole reason the placement went to Feint rather than to Sentinel,
    /// whose family it belongs to. p.8 removes every placed token at the end of a round, so a Sentinel
    /// that supplied its own Hazard would never have one standing when it came back to hand.
    ///
    /// The view is asserted through the configured camera rather than by its local offset, which is
    /// the lesson milestone 6.5c recorded when the hero choice cue shipped invisible: the board is the
    /// XZ plane under a top-down camera, so a piece at the wrong orientation renders as a sliver while
    /// every local-offset assertion about it still passes.
    /// </summary>
    public sealed class PlacedTokenPlayModeTests
    {
        private static readonly ActionDefinitionId FeintSkill =
            new ActionDefinitionId("primary-immunity-skill");

        private static readonly EffectNodeId PlaceBranch =
            new EffectNodeId("feint-place-branch");

        [UnityTest]
        public IEnumerator Scene_DrawsATokenAPlayerPlacedAndTakesItBackOnUndo()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            Assert.That(
                runtime.Cards.Count(card => card.State.Location == CardLocation.Hand),
                Is.EqualTo(10),
                "Five cards per hero in hand. The capability that puts a token on this board rides on " +
                "one of them rather than on a sixth card dealt beside them.");
            Assert.That(runtime.Cards, Has.Count.EqualTo(36),
                "Milestone 7.5 deals rulebook p.6's remaining thirteen cards per hero into the deck.");
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

            // Milestone 6.3a: the second half of milestone 6.6a's handoff finding. The control read
            // correctly and the board said nothing at all, so a player could read which token was
            // meant only by counting spaces. Pointing at the control now marks the exact piece.
            Assert.That(target.TokenId, Is.EqualTo(runtime.BoardView.Model.PlacedTokens.Single().Id),
                "The control carries the token's own identity, so the board never has to work out " +
                "which piece stands on a space.");
            // The board is marked by the view's own hover route, which this driver goes through, so
            // what is asserted is the cue that results rather than the call's own return.
            runtime.HighlightEffectChoiceTarget(placed);
            yield return null;

            var marked = runtime.BoardView.TokenViews.Single(value =>
                value.BoundTokenId.Equals(target.TokenId));
            Assert.That(runtime.BoardView.Model.TokenChoiceCues.Single().TokenId,
                Is.EqualTo(target.TokenId));
            Assert.That(marked.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
            AssertCueIsReadableThroughTheCamera(marked);

            runtime.HighlightEffectChoiceTarget(null);
            yield return null;

            Assert.That(runtime.BoardView.Model.TokenChoiceCues, Is.Empty);
            Assert.That(marked.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None),
                "The cue is dropped when the pointer leaves, exactly as milestone 6.5c requires of " +
                "every other board cue.");
        }

        /// <summary>
        /// Milestone 6.3a in the saved configured scene: the implemented Clear reaches a player.
        ///
        /// Milestone 6.3 built the offer, the validation, the command and the resolution, and left the
        /// HUD without a way to submit any of it. With a Hazard standing beside the hero and a card
        /// that prints an Attack in play, the configured action row now offers the replacement, the
        /// selection names the piece, pointing at that control marks it, and confirming takes exactly
        /// the pieces chosen.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_OffersClearOnAnAttackTurnBesideAPlacedHazard()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var placed = PlaceAHazard(runtime);
            yield return null;

            CommitAttackTurn(runtime);
            var offer = runtime.ClearOffers.Single();
            Assert.That(offer.Label, Is.EqualTo("CLEAR (REPLACES ATTACK)"),
                "Milestone 6.3 implemented Clear and shipped no way to choose it; the configured " +
                "action row says what it costs.");
            Assert.That(offer.CandidateCount, Is.EqualTo(1));

            var acceptedBefore = runtime.AcceptedCommands.Count;
            Assert.That(runtime.SelectClear(), Is.True);
            yield return null;

            var token = runtime.BoardView.Model.PlacedTokens.Single();
            var candidate = runtime.ClearSelection.Candidates.Single();
            Assert.That(candidate.Label, Is.EqualTo("TEST - HAZARD"));
            Assert.That(candidate.TokenId, Is.EqualTo(token.Id));
            Assert.That(candidate.Space, Is.EqualTo(placed));
            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(acceptedBefore),
                "Opening the selection submits nothing and leaves the card unresolved.");

            Assert.That(runtime.HighlightClearToken(token.Id), Is.True);
            yield return null;

            var view = runtime.BoardView.TokenViews.Single(value =>
                value.BoundTokenId.Equals(token.Id));
            Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
            AssertCueIsReadableThroughTheCamera(view);

            Assert.That(runtime.ToggleClearToken(token.Id), Is.True);
            yield return null;

            Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Selected),
                "A chosen piece keeps its own treatment while the pointer moves on.");
            Assert.That(runtime.ClearSelection.Confirm.Command.TokenInstanceIds,
                Is.EqualTo(new[] { token.Id }));

            Assert.That(runtime.ConfirmClear(), Is.True);
            yield return null;

            Assert.That(runtime.BoardView.Model.PlacedTokens, Is.Empty);
            Assert.That(view.gameObject.activeSelf, Is.False,
                "The piece stops being drawn, because projection presence owns visibility.");
            Assert.That(runtime.ClearSelection, Is.Null);

            Assert.That(runtime.Undo(), Is.True);
            runtime.RefreshAll();
            yield return null;

            Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(view.gameObject.activeSelf, Is.True);
            Assert.That(view.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None),
                "Undo rebuilds the surface from the position as it now is, carrying no cue across.");
        }

        /// <summary>
        /// Asserts a cue reads at the configured camera rather than only sitting at the right local
        /// offset - the lesson milestone 6.5c recorded when the hero choice cue shipped invisible.
        /// </summary>
        private static void AssertCueIsReadableThroughTheCamera(PlacedTokenView view)
        {
            var camera = Object.FindAnyObjectByType<Camera>();
            var cue = view.ChoiceCue;
            Assert.That(cue, Is.Not.Null);
            Assert.That(cue.gameObject.activeInHierarchy, Is.True);
            var onScreen = camera.WorldToViewportPoint(cue.transform.position);
            Assert.That(onScreen.z, Is.GreaterThan(0f),
                "The cue is in front of the configured camera rather than behind it.");
            Assert.That(onScreen.x, Is.InRange(0f, 1f));
            Assert.That(onScreen.y, Is.InRange(0f, 1f),
                "And inside the framed board rather than off the edge of it.");

            var bounds = cue.GetComponent<Renderer>().bounds;
            var lowLeft = camera.WorldToScreenPoint(bounds.min);
            var topRight = camera.WorldToScreenPoint(bounds.max);
            Assert.That(Mathf.Abs(topRight.x - lowLeft.x), Is.GreaterThan(2f));
            Assert.That(Mathf.Abs(topRight.y - lowLeft.y), Is.GreaterThan(2f),
                "It covers real screen area in both directions - an edge-on glyph is a sliver in one " +
                "of them while every local-offset assertion still passes.");

            var tokenOnScreen = camera.WorldToViewportPoint(view.transform.position);
            Assert.That(onScreen.y, Is.GreaterThan(tokenOnScreen.y),
                "It sits above the piece it marks, which is what makes a downward triangle read.");
        }

        /// <summary>
        /// Commits the Blue card that prints an Attack against the Red hand's lowest, so the hero that
        /// placed the Hazard is standing beside it with a replaceable Attack in play.
        /// </summary>
        private static void CommitAttackTurn(ConfiguredMatchSceneRuntime runtime)
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

            // Named by the printed action rather than by being the top of a band: the showcase
            // conversions have left Blue exactly one non-Ranged and one Ranged printed Attack.
            CommitTurn(runtime, "blue-hammer-card");
            if (!runtime.SelectedActor.CardInstanceId.Value.StartsWith("blue-hammer"))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The Red actor completes by Hold first; Blue Hammer at 6 sits below Red's lowest.");
            }

            Assert.That(runtime.SelectedActor.CardInstanceId.Value, Is.EqualTo("blue-hammer-card"));
        }

        /// <summary>
        /// Spends the opening turn on Blue Feint's place mode and returns the space its Hazard took.
        /// Red commits its lowest card, and Feint at Initiative 12 outranks it, so the Blue hero acts
        /// first and the placement is the first thing to happen in the round.
        /// </summary>
        private static HexCoordinate PlaceAHazard(ConfiguredMatchSceneRuntime runtime)
        {
            CommitTurn(runtime, "blue-feint-card");
            if (!runtime.SelectedActor.CardInstanceId.Value.StartsWith("blue-feint"))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "Whichever actor holds the turn first completes by Hold.");
            }

            Assert.That(runtime.SubmitAction(FeintSkill), Is.True);
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
            // Milestone 8.4. The configured scene deals Wasp's verified cards now, and every printed
            // primary of hers is refused until its 8.6-8.13 slice composes its text, so a scene that
            // must draw a composed Skill asks for the mechanics-showcase hands. The request is consumed
            // and reset by the composition it applies to, so it cannot leak into another fixture.
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.MechanicsShowcaseFixture;
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
