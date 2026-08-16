using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
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
    /// Milestone 6.5b in the saved configured scene, corrected by milestones 6.5ac and 6.5ae.
    ///
    /// The scene's own geography decides what is reachable, and here it decides both branches are
    /// empty-handed: the two heroes start on their configured spawn points far from everything, so
    /// nothing is adjacent for the direct Move to name, and every declared miniature is standing, so
    /// p.18's Spawn has no supply to draw from.
    ///
    /// 6.5b read that as the case worth rendering and pinned it: a player could choose either mode and
    /// what they saw was p.10's stop. That was the defect, not the fixture's limitation - choosing a
    /// mode that was never going to do anything spent the card, ended the turn, changed nothing on the
    /// board, and explained nothing. 6.5ac refused both modes but had to stop the action before any
    /// prompt opened, so the player still saw nothing at all. 6.5ae gave that pause a way out.
    ///
    /// 6.5af is what stops the player entering it: the Skill is judged before it is offered, so in
    /// this position it is drawn greyed and the card's Secondary Movement - which the old behaviour
    /// silently spent the turn instead of - is still there to take. p.12 is the source: a player is
    /// never forced to perform an action, because any card can Hold.
    ///
    /// The player-facing paths where each branch has something to work with run through the
    /// application boundary in `ConfiguredDirectMoveSpawnSkillTests`, and the mixed surface - one mode
    /// refused beside one that can be taken - is reached in this scene by Red Needle, in
    /// `PushPlaceSkillPlayModeTests`.
    /// </summary>
    public sealed class DirectMoveSpawnSkillPlayModeTests
    {
        private static readonly ActionDefinitionId BombardSkill =
            new ActionDefinitionId("primary-direct-move-spawn-skill");

        [UnityTest]
        public IEnumerator Scene_GreysTheSkillItCannotStartAndLeavesTheTurnStillSpendable()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            var cardId = runtime.SelectedActor.CardInstanceId;

            var greyed = runtime.RoundView.Model.UnavailableActions.Single();
            Assert.That(greyed.ActionDefinitionId, Is.EqualTo(BombardSkill),
                "Neither mode can be performed from the scene's starting position, so the Skill is " +
                "drawn greyed rather than offered.");
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty,
                "And the card's other actions are still controls - the Secondary Movement above all.");
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(BombardSkill)), Is.False);

            Assert.That(runtime.SubmitAction(BombardSkill), Is.False,
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
            CommitBombardTurn(runtime);
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
        /// Commits Red Bombard at Initiative 5 against the Blue hand's lowest card, Blue Longbow at 4,
        /// which is the only Blue Initiative below it, so the Red hero acts first.
        /// </summary>
        private static void CommitBombardTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = runtime.Cards.Single(card => card.Id.Value == "red-bombard-card");
            var blue = runtime.Cards
                .Where(card => runtime.Setup.Heroes
                    .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId.Equals(card.OwnerId))
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();

            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.PendingInitiativeChoice, Is.Null);
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
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
