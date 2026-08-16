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
    /// Milestone 6.5b in the saved configured scene.
    ///
    /// The scene's own geography decides what is reachable, and here it decides both branches are
    /// empty-handed: the two heroes start on their configured spawn points far from everything, so
    /// nothing is adjacent for the direct Move to name, and every declared miniature is standing, so
    /// p.18's Spawn has no supply to draw from. That is the case worth rendering here rather than a
    /// limitation to work around - a player really can choose either mode in this position, and what
    /// they must see is p.10's stop rather than a fault or a silent nothing.
    ///
    /// The player-facing paths where each branch has something to work with run through the
    /// application boundary in `ConfiguredDirectMoveSpawnSkillTests`.
    /// </summary>
    public sealed class DirectMoveSpawnSkillPlayModeTests
    {
        private static readonly ActionDefinitionId BombardSkill =
            new ActionDefinitionId("primary-direct-move-spawn-skill");

        private static readonly EffectNodeId MoveBranch = new EffectNodeId("bombard-move-branch");
        private static readonly EffectNodeId Spawn = new EffectNodeId("bombard-spawn");

        [UnityTest]
        public IEnumerator Scene_RendersBothBranchesAndStopsTheActionWhenTheChosenOneHasNothingToTake()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);
            var minionsBefore = runtime.BoardView.Model.Minions.Count;
            var cardId = runtime.SelectedActor.CardInstanceId;

            Assert.That(runtime.SubmitAction(BombardSkill), Is.True);
            yield return null;

            var modes = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .Select(value => value.Command.EffectChoice.Candidate.OptionNodeId)
                .ToArray();
            Assert.That(modes, Is.EqualTo(new[] { MoveBranch, Spawn }),
                "Both branches are rendered as controls in the definition's own order.");

            Assert.That(runtime.SubmitEffectChoiceOption(Spawn), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "There is no supply to choose from, so no second decision is opened.");
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "p.10: a mandatory step that cannot be performed stops the action there, and the " +
                "action closes rather than being left pending.");
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(minionsBefore),
                "Nothing was invented to satisfy the mode the player picked.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty,
                "The stopped action leaves no decision on screen.");
        }

        [UnityTest]
        public IEnumerator Scene_TheDirectMoveBranchOffersNoTargetWhereNothingStandsAdjacent()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitBombardTurn(runtime);
            var cardId = runtime.SelectedActor.CardInstanceId;

            Assert.That(runtime.SubmitAction(BombardSkill), Is.True);
            Assert.That(runtime.SubmitEffectChoiceOption(MoveBranch), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "`another adjacent unit` names nothing here, so the selection opens no prompt.");
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty);
            Assert.That(runtime.BoardView.Model.EffectChoiceDestinations, Is.Empty,
                "And the board lights nothing, because no destination was ever offered.");
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(cardId)).State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "The action really was started and stopped rather than refused outright.");
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
