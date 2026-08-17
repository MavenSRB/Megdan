using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 6.7a in the saved configured scene.
    ///
    /// What the scene decides here is the same thing it decided for milestone 6.6a's card, and for the
    /// same reason: both heroes start on their configured spawn points far from everything, so nothing
    /// stands beside the acting hero and the one step this card names has nothing to name. Milestone
    /// 6.5af therefore judges the Skill before it is offered, and what reaches a player is a greyed
    /// control and a turn still worth spending.
    ///
    /// That is a real acceptance case rather than a consolation one - it is the position a player
    /// actually meets on the opening round - but it is deliberately not the whole of the milestone.
    /// Reaching the repeat prompt needs a unit beside the acting hero, which the scene's geography
    /// cannot supply until somebody walks there, so the ordinary, declined and repeated paths are
    /// proved through the application boundary in `ConfiguredPushRepeatSkillTests` and their controls
    /// through `MatchFeaturePresenterTests`. What these cases add is that the conversion reached the
    /// scene's own hand at all, which no Edit Mode fixture can say.
    /// </summary>
    public sealed class PushRepeatSkillPlayModeTests
    {
        private static readonly ActionDefinitionId LongbowSkill =
            new ActionDefinitionId("primary-push-repeat-skill");

        [UnityTest]
        public IEnumerator Scene_DealsTheConvertedCardAndGreysTheSkillItCannotStart()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var longbow = runtime.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            var definition = runtime.Definitions
                .Single(value => value.Reference.Equals(longbow.DefinitionReference));

            Assert.That(definition.ContentRevision, Is.EqualTo("phase-6-4c-rev-1"),
                "The scene deals the converted card rather than a fixture-only copy of it. Milestone " +
                "6.4c advanced the revision again when the card declared which kind of piece its " +
                "Push may take.");
            Assert.That(
                definition.ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind,
                Is.EqualTo(ActionKind.Skill));

            CommitLongbowTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;

            var greyed = runtime.RoundView.Model.UnavailableActions.Single();
            Assert.That(greyed.ActionDefinitionId, Is.EqualTo(LongbowSkill),
                "Nothing stands beside the acting hero, so the one thing this card can do can do " +
                "nothing and the Skill is drawn greyed rather than offered.");
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty,
                "And the card's other actions are still controls - the Secondary Movement above all.");
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(LongbowSkill)), Is.False);

            Assert.That(runtime.SubmitAction(LongbowSkill), Is.False,
                "It cannot be started, so the turn is not spent on it.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(longbow.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "The card is unspent and the decision is still the player's.");
            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,
                "p.12's Hold is offered throughout, for the player who wants it.");
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore),
                "Nothing was invented, and nothing was quietly taken away either.");
        }

        [UnityTest]
        public IEnumerator Scene_LightsNoBoardCueAndOffersNoRepeatForTheSkillItRefused()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitLongbowTurn(runtime);
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty,
                "No frame is paused, so no unit is drawn as a target.");
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Empty);
            Assert.That(runtime.BoardView.Model.EffectChoiceDestinations, Is.Empty,
                "And the board lights nothing, because no destination was ever offered.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty,
                "A repeat is a decision inside a resolving action; the action never started, so " +
                "neither the confirm nor the decline exists to be pressed.");
        }

        /// <summary>
        /// Commits Blue Longbow at Initiative 4 against the Red hand's lowest card and spends the Red
        /// turn on p.12's Hold. Blue Longbow sits below every Red card, so it always acts second - the
        /// mirror of the position every other converted Blue card is in, and one an ordinary match
        /// reaches by a player declining to spend a turn.
        /// </summary>
        private static void CommitLongbowTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var blue = runtime.Cards.Single(card => card.Id.Value == "blue-longbow-card");
            var red = runtime.Cards
                .Where(card => runtime.Setup.Heroes
                    .Single(hero => hero.TeamSide == TeamSide.Red).HeroId.Equals(card.OwnerId))
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();

            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.PendingInitiativeChoice, Is.Null);
            Assert.That(runtime.SubmitEndTurn(), Is.True,
                "The Red hero holds, which is what any Action card may do with a turn nobody wants.");
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(blue.Id));
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
