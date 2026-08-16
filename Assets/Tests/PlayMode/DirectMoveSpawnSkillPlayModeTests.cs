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
    /// Milestone 6.5b in the saved configured scene, corrected by milestone 6.5ac.
    ///
    /// The scene's own geography decides what is reachable, and here it decides both branches are
    /// empty-handed: the two heroes start on their configured spawn points far from everything, so
    /// nothing is adjacent for the direct Move to name, and every declared miniature is standing, so
    /// p.18's Spawn has no supply to draw from.
    ///
    /// 6.5b read that as the case worth rendering and pinned it: a player could choose either mode and
    /// what they saw was p.10's stop. That was the defect, not the fixture's limitation - choosing a
    /// mode that was never going to do anything spent the card, ended the turn, changed nothing on the
    /// board, and explained nothing. Neither mode is offered now. The node reports itself impossible
    /// before any prompt opens, and its mandatory policy is what stops the action.
    ///
    /// The player-facing paths where each branch has something to work with run through the
    /// application boundary in `ConfiguredDirectMoveSpawnSkillTests`, and the greyed surface - one
    /// mode refused beside one that can be taken - is reached in this scene by Red Needle, in
    /// `PushPlaceSkillPlayModeTests`.
    /// </summary>
    public sealed class DirectMoveSpawnSkillPlayModeTests
    {
        private static readonly ActionDefinitionId BombardSkill =
            new ActionDefinitionId("primary-direct-move-spawn-skill");

        private static readonly EffectNodeId MoveBranch = new EffectNodeId("bombard-move-branch");
        private static readonly EffectNodeId Spawn = new EffectNodeId("bombard-spawn");

        [UnityTest]
        public IEnumerator Scene_OffersNeitherBranchWhenNeitherCanBePerformed()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            var cardId = runtime.SelectedActor.CardInstanceId;

            Assert.That(runtime.SubmitAction(BombardSkill), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "A choice nobody could answer is never opened: a mandatory Choose offers no decline, " +
                "and every other control is withheld while an effect choice stands.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty);
            Assert.That(runtime.SubmitEffectChoiceOption(MoveBranch), Is.False,
                "Neither mode can be chosen, so neither can be spent on.");
            Assert.That(runtime.SubmitEffectChoiceOption(Spawn), Is.False);
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "p.10: a mandatory step that cannot be performed stops the action there, and the " +
                "action closes rather than being left pending.");
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
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Is.Empty,
                "A refused option is greyed inside a decision the player is being asked; there is no " +
                "decision here to grey anything in.");
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
