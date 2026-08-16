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
    /// Milestone 6.6a in the saved configured scene.
    ///
    /// The scene's own geography decides what is reachable, and on the opening round it decides all
    /// three modes are empty-handed: the two heroes start on their configured spawn points far from
    /// everything, so no unit stands beside the acting hero for the Swap to exchange with, and nobody
    /// has placed a token yet, so neither the Replace nor the Remove has a piece to work on.
    ///
    /// Milestone 6.2d changed what happens after that round rather than during it. The scene now deals
    /// milestone 6.2's Cache card, so a player can place a Hazard and give the other two modes
    /// something to act on; what this fixture pins is the position before anyone has, which is where
    /// milestone 6.5af's judgement reaches a player - the Skill is drawn greyed rather than offered,
    /// and the card's Secondary Movement is still there to take. That a placed token is drawn at all
    /// is `PlacedTokenPlayModeTests`, and the branch-by-branch behaviour runs through the application
    /// boundary in `ConfiguredExchangeSkillTests`.
    /// </summary>
    public sealed class ExchangeSkillPlayModeTests
    {
        private static readonly ActionDefinitionId SentinelSkill =
            new ActionDefinitionId("primary-exchange-skill");

        [UnityTest]
        public IEnumerator Scene_GreysTheSkillItCannotStartAndLeavesTheTurnStillSpendable()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitSentinelTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            var cardId = runtime.SelectedActor.CardInstanceId;

            var greyed = runtime.RoundView.Model.UnavailableActions.Single();
            Assert.That(greyed.ActionDefinitionId, Is.EqualTo(SentinelSkill),
                "No mode can be performed from the scene's starting position, so the Skill is drawn " +
                "greyed rather than offered.");
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty,
                "And the card's other actions are still controls - the Secondary Movement above all.");
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(SentinelSkill)), Is.False);

            Assert.That(runtime.SubmitAction(SentinelSkill), Is.False,
                "It cannot be started, so the turn is not spent on it.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "The card is unspent and the decision is still the player's, which is the whole point.");
            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,
                "p.12's Hold is offered throughout, for the player who wants it.");
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore),
                "Nothing was invented, and nothing was quietly taken away either.");
        }

        [UnityTest]
        public IEnumerator Scene_LightsNoBoardCueForTheSkillItRefused()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitSentinelTurn(runtime);
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty,
                "No frame is paused, so no unit is drawn as a target.");
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Empty);
            Assert.That(runtime.BoardView.Model.EffectChoiceDestinations, Is.Empty,
                "And the board lights nothing, because no destination was ever offered.");
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Is.Empty,
                "A refused mode is greyed inside a decision the player is being asked; there is no " +
                "decision here, because the action it belongs to was never started.");
        }

        /// <summary>
        /// Commits Blue Sentinel at Initiative 11 against the Red hand's lowest card, so the Blue hero
        /// acts first whatever the showcase conversions have done to the top of either band.
        /// </summary>
        private static void CommitSentinelTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var blue = runtime.Cards.Single(card => card.Id.Value == "blue-sentinel-card");
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
