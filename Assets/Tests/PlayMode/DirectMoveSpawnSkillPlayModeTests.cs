using System.Collections;
using System.Linq;
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
    /// prompt opened, so the player still saw nothing at all. 6.5ae is the surface: the choice pauses
    /// with both modes greyed and offers End Turn, and this scene position is where the whole defect
    /// was found, so it is where the correction is proved.
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

        private static readonly EffectNodeId MoveBranch = new EffectNodeId("bombard-move-branch");
        private static readonly EffectNodeId Spawn = new EffectNodeId("bombard-spawn");

        [UnityTest]
        public IEnumerator Scene_GreysBothBranchesAndOffersEndTurnAsTheOnlyControl()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            var cardId = runtime.SelectedActor.CardInstanceId;

            Assert.That(runtime.SubmitAction(BombardSkill), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The action pauses on a decision the player can see, rather than ending unasked.");
            Assert.That(
                runtime.RoundView.Model.UnavailableEffectChoices
                    .Select(value => value.Candidate.OptionNodeId),
                Is.EqualTo(new[] { MoveBranch, Spawn }),
                "Both modes are drawn greyed, in the definition's own order.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty);
            Assert.That(runtime.RoundView.Model.Actions, Is.Empty);
            Assert.That(runtime.RoundView.Model.FastTravelOffers, Is.Empty);
            Assert.That(runtime.SubmitEffectChoiceOption(MoveBranch), Is.False,
                "Neither mode can be chosen, so neither can be spent on.");
            Assert.That(runtime.SubmitEffectChoiceOption(Spawn), Is.False);
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Unresolved),
                "And nothing stops the action while the player is still looking at it.");

            Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,
                "End Turn is the one legal command in this position.");
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "p.10: the stop ends the action there, and the action closes rather than being left " +
                "pending.");
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore),
                "Nothing was invented, and nothing was quietly taken away either.");
        }

        [UnityTest]
        public IEnumerator Scene_LightsNoBoardCueForTheModesItRefused()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);

            Assert.That(runtime.SubmitAction(BombardSkill), Is.True);
            yield return null;

            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty,
                "`another adjacent unit` names nothing here, so no unit is drawn as a target.");
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations, Is.Empty);
            Assert.That(runtime.BoardView.Model.EffectChoiceDestinations, Is.Empty,
                "And the board lights nothing, because no destination was ever offered.");
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
