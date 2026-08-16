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
    /// Milestone 6.5a in the saved configured scene. Red Needle's showcase Skill is the first
    /// value-shaped effect choice a player can reach from the ordinary configured hand, so this is
    /// where milestone 6.4b's coordinate-addressed submitter finally has a real consumer.
    ///
    /// The scene's own geography decides which branch is reachable: the two heroes start on their
    /// configured spawn points, far apart, so nothing is adjacent and the Place branch is the one a
    /// player can complete here. The Push branch's player-facing path runs through the application
    /// boundary in `ConfiguredPushPlaceSkillTests`; what this fixture adds is the passive-view half -
    /// that the rendered controls carry exactly the projected invocations and that a control the
    /// projection never offered forwards nothing at all.
    /// </summary>
    public sealed class PushPlaceSkillPlayModeTests
    {
        private static readonly ActionDefinitionId NeedleSkill =
            new ActionDefinitionId("primary-push-place-skill");

        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("needle-place-branch");
        private static readonly EffectNodeId PushBranch = new EffectNodeId("needle-push-branch");

        [UnityTest]
        public IEnumerator Scene_ThePlaceBranchForwardsItsProjectedDestinationAndRelocatesTheHero()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNeedleTurn(runtime);
            var actorId = runtime.SelectedActor.ActorId;
            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate;

            Assert.That(runtime.SubmitAction(NeedleSkill), Is.True);
            yield return null;

            var modes = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .Select(value => value.Command.EffectChoice.Candidate.OptionNodeId)
                .ToArray();
            Assert.That(modes, Is.EqualTo(new[] { PushBranch, PlaceBranch }),
                "Both branches are rendered as controls in the definition's own order.");

            Assert.That(runtime.SubmitEffectChoiceOption(PlaceBranch), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            var destinations = runtime.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations, Has.No.Member(origin), "p.18 requires a different space.");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(origin, space) <= 4), Is.True,
                "Radius 4 is inclusive rulebook distance.");

            var chosen = destinations[0];
            Assert.That(runtime.SubmitEffectChoiceValue(chosen), Is.True,
                "The rendered destination control carries the projection's own invocation.");
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null, "The Skill completes.");
            Assert.That(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(chosen),
                "The acting hero really is standing on the space the player picked.");
            Assert.That(runtime.PendingMovementChoice, Is.Null,
                "Placing is not movement (p.18): no Movement action was started and none is pending.");
        }

        [UnityTest]
        public IEnumerator Scene_AControlTheProjectionNeverOfferedForwardsNothing()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNeedleTurn(runtime);
            Assert.That(runtime.SubmitAction(NeedleSkill), Is.True);
            Assert.That(runtime.SubmitEffectChoiceOption(PlaceBranch), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            var acceptedBefore = runtime.AcceptedCommands.Count;
            var snapshotBefore = runtime.CaptureSnapshot();
            var unreachable = new HexCoordinate(40, 40);
            Assert.That(
                runtime.PendingEffectChoice.Candidates
                    .Select(candidate => candidate.Value.Coordinate),
                Has.No.Member(unreachable),
                "Fixture precondition: the projection really does not offer that space.");

            Assert.That(runtime.SubmitEffectChoiceValue(unreachable), Is.False,
                "The passive view forwards only what the projection rendered; it composes nothing.");
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshotBefore));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The authoritative pending choice is exactly what it was.");
        }

        /// <summary>
        /// Commits Red Needle at Initiative 13 against the Blue hand's lowest card, so the Red hero
        /// acts first and its showcase Skill is the offered action.
        /// </summary>
        private static void CommitNeedleTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = runtime.Cards.Single(card => card.Id.Value == "red-needle-card");
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
