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
    ///
    /// Milestone 6.5c changed what "rendered" means for the Place branch: its destinations are lit on
    /// the board and carry no controls, and one click answers the choice outright. Milestone 6.5ac
    /// changed what it means for the Push branch: with nothing adjacent it can do nothing, so it is
    /// drawn greyed and carries no invocation, which makes this scene position the one that shows both
    /// halves of the option surface at once.
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

            // Milestone 6.5ac: this is the scene position where both readings show at once. Nothing
            // stands adjacent, so the Push branch can do nothing and is greyed; the Place branch has
            // empty spaces within Radius 4 and stays a control.
            var modes = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .Select(value => value.Command.EffectChoice.Candidate.OptionNodeId)
                .ToArray();
            Assert.That(modes, Is.EqualTo(new[] { PlaceBranch }),
                "Only the branch the actor can perform is rendered as a control.");
            Assert.That(
                runtime.RoundView.Model.UnavailableEffectChoices
                    .Select(value => value.Candidate.OptionNodeId),
                Is.EqualTo(new[] { PushBranch }),
                "The other is still rendered, greyed, so the player sees the mode exists and that it " +
                "cannot be taken here.");
            Assert.That(runtime.SubmitEffectChoiceOption(PushBranch), Is.False,
                "A greyed control carries no command, so nothing about it can reach the session.");

            Assert.That(runtime.SubmitEffectChoiceOption(PlaceBranch), Is.True);
            yield return null;

            var destinations = runtime.PendingEffectChoice.Candidates
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations, Has.No.Member(origin), "p.18 requires a different space.");
            Assert.That(
                destinations.All(space => HexDistanceQuery.Calculate(origin, space) <= 4), Is.True,
                "Radius 4 is inclusive rulebook distance.");

            // Milestone 6.5c: the destinations carry no controls at all now - the board lights them
            // and one click answers the choice, because the select and the confirm the authority
            // offers with it are forwarded together for an exactly-one selection.
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty,
                "An empty space names no object, so no named control is drawn for one.");
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.False,
                "The anonymous destination buttons are gone.");
            Assert.That(runtime.RoundView.Model.EffectChoiceDestinations.Select(value =>
                    value.Command.EffectChoice.Candidate.Value.Coordinate),
                Is.EqualTo(destinations),
                "Each offered space keeps the projected invocation a board click forwards.");
            Assert.That(runtime.BoardView.Model.EffectChoiceDestinations, Is.EqualTo(destinations),
                "And the board lights exactly those spaces.");

            var chosen = destinations[0];
            Assert.That(runtime.SubmitEffectChoiceValue(chosen), Is.True,
                "The lit space carries the projection's own invocation.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null, "The Skill completes in one click.");
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
                    .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId.Equals(card.OwnerId) &&
                    card.State.Location == CardLocation.Hand)
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
