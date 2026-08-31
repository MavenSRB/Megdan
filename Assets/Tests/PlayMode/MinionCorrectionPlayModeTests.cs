using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Setup;
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
    /// Milestone 6.4b: the displaced-minion correction rides milestone 6.1a's configured minion-choice
    /// prompt and board cues in the real scene.
    ///
    /// The configured scene's hand cannot reach a real correction - the displacement Skill lives only
    /// on the Skill-fixture hands - so, exactly as the 6.1a removal check does, these cases hand-build
    /// authority-shaped view models and render them into the configured passive views, then prove the
    /// scene's own effects: prompt family replacement, Move-versus-Place wording, subject progression,
    /// the no-choice silence, and the passive-view authority boundary under which every forwarded
    /// intent leaves authoritative state untouched.
    /// </summary>
    public sealed class MinionCorrectionPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_CorrectionPromptRidesTheConfiguredMinionChoiceViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            Assert.That(runtime.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(runtime.MinionChoiceView.Model, Is.Null,
                "With nothing pending the configured prompt exposes no controls.");

            var minions = runtime.BoardView.Model.Minions
                .Where(value => value.TeamSide == TeamSide.Blue)
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            var blueTeamId = runtime.Setup.Teams.Single(value => value.TeamSide == TeamSide.Blue).TeamId;
            var resolutionId = new ActionResolutionId("configured-correction");
            var subjectChoiceId = new ActionResolutionStepId("correction-subject");
            var baseBoard = runtime.BoardView.Model;
            var acceptedBefore = runtime.AcceptedCommands.Count;
            var snapshotBefore = runtime.CaptureSnapshot();

            // A removal prompt first, then the correction: the one shared prompt replaces its family
            // rather than a second framework appearing beside it.
            var removalResolutionId = new EndOfRoundResolutionId("configured-minion-battle");
            var removalChoiceId = new EndOfRoundChoiceId("configured-minion-battle-removals");
            runtime.MinionChoiceView.Render(new MinionChoiceViewModel(
                baseBoard.Version,
                removalResolutionId,
                removalChoiceId,
                blueTeamId,
                TeamSide.Blue,
                1,
                0,
                new[]
                {
                    new MinionChoiceCandidateViewModel(
                        minions[0].MinionId,
                        $"REMOVE {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, minions[0].MinionType, 1)}",
                        1,
                        false,
                        DirectorCommand.SelectMinionBattleRemoval(new MinionBattleChoiceCommand(
                            MinionBattleChoiceCommandKind.Select, removalResolutionId, removalChoiceId,
                            TeamSide.Blue, minions[0].MinionId)))
                },
                null));
            yield return null;
            Assert.That(runtime.MinionChoiceView.Model.SectionLabel, Is.EqualTo("Minion Battle"));

            // Multi-minion progression starts at the subject stage: the owning team picks which of
            // its displaced minions corrects next.
            var subjects = minions.Select((minion, index) => new MinionChoiceCandidateViewModel(
                    minion.MinionId,
                    $"RETURN {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, minion.MinionType, index + 1)}",
                    index + 1,
                    false,
                    DirectorCommand.ChooseCorrectionSubject(new DisplacedMinionCorrectionCommand(
                        DisplacedMinionCorrectionCommandKind.SelectSubject, resolutionId,
                        subjectChoiceId, TeamSide.Blue, minion.MinionId, default))))
                .ToArray();
            runtime.MinionChoiceView.Render(MinionChoiceViewModel.ForCorrection(
                baseBoard.Version + 1,
                MinionChoicePromptKind.CorrectionSubject,
                resolutionId,
                subjectChoiceId,
                default,
                blueTeamId,
                TeamSide.Blue,
                subjects));
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.SectionLabel, Is.EqualTo("Correction"),
                "The prompt family is replaced: the gutter now names the correction.");
            Assert.That(runtime.MinionChoiceView.Model.PromptLabel,
                Is.EqualTo("CORRECTION - [B] BLUE RETURNS A DISPLACED MINION"));
            Assert.That(runtime.MinionChoiceView.Model.Candidates, Has.Count.EqualTo(2),
                "Every displaced minion of the deciding team is offered.");
            Assert.That(runtime.MinionChoiceView.Model.ConfirmInvocation, Is.Null,
                "Selection is the advance for a correction; there is nothing separate to confirm.");

            // The route stage: candidates are destinations for the one named subject; the board marks
            // that subject, and since milestone 6.2d lights the one endpoint whose control is being
            // pointed at rather than every offered space at once.
            var subject = minions[0];
            var destinationChoiceId = new ActionResolutionStepId($"correction-{subject.MinionId.Value}");
            var destinations = new[]
            {
                new HexCoordinate(subject.Coordinate.Q + 1, subject.Coordinate.R),
                new HexCoordinate(subject.Coordinate.Q, subject.Coordinate.R + 1)
            };
            var routeCandidates = destinations.Select((destination, index) =>
                    new MinionChoiceCandidateViewModel(
                        subject.MinionId,
                        $"MOVE TO ({destination.Q},{destination.R})",
                        index + 1,
                        false,
                        DirectorCommand.ChooseCorrectionDestination(new DisplacedMinionCorrectionCommand(
                            DisplacedMinionCorrectionCommandKind.SelectDestination, resolutionId,
                            destinationChoiceId, TeamSide.Blue, subject.MinionId, destination)),
                        destination))
                .ToArray();
            runtime.MinionChoiceView.Render(MinionChoiceViewModel.ForCorrection(
                baseBoard.Version + 2,
                MinionChoicePromptKind.CorrectionMove,
                resolutionId,
                destinationChoiceId,
                subject.MinionId,
                blueTeamId,
                TeamSide.Blue,
                routeCandidates));
            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 2,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: System.Array.Empty<MinionDefenseCueViewModel>(),
                pieceTargetCues: new[]
                {
                    new PieceTargetCueViewModel(
                        BoardObjectReference.ForMinion(subject.MinionId),
                        BoardTargetCueState.Selected,
                        BoardTargetConsequenceIntent.Affect)
                },
                correctionDestination: destinations[1]));
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.PromptLabel,
                Is.EqualTo("CORRECTION (MOVE) - [B] BLUE CHOOSES THE RETURN SPACE"));
            Assert.That(runtime.MinionChoiceView.Model.SubjectMinionId, Is.EqualTo(subject.MinionId),
                "The prompt names the displaced stable minion identity the destinations would receive.");
            Assert.That(
                runtime.MinionChoiceView.Model.Candidates.Select(value => value.Destination.Value),
                Is.EqualTo(destinations),
                "Every legal route endpoint is a selectable control without presentation calculating anything.");
            var subjectView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(subject.MinionId));
            var subjectRing = subjectView.TargetRing;
            Assert.That(subjectView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(subjectRing.IsVisible, Is.False);
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(subject.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect),
                "The board identifies the correction subject with neutral corners.");
            Assert.That(runtime.BoardView.Model.CorrectionDestination, Is.EqualTo(destinations[1]),
                "Milestone 6.2d: the board lights the endpoint being pointed at, and only that one.");
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(destinations[1])).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(destinations[0])).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.None));

            // Place wording stays apart from Move wording inside the correction family.
            runtime.MinionChoiceView.Render(MinionChoiceViewModel.ForCorrection(
                baseBoard.Version + 3,
                MinionChoicePromptKind.CorrectionPlace,
                resolutionId,
                destinationChoiceId,
                subject.MinionId,
                blueTeamId,
                TeamSide.Blue,
                routeCandidates.Select((candidate, index) => new MinionChoiceCandidateViewModel(
                    subject.MinionId,
                    $"PLACE AT ({candidate.Destination.Value.Q},{candidate.Destination.Value.R})",
                    index + 1,
                    false,
                    candidate.Command,
                    candidate.Destination)).ToArray()));
            yield return null;
            Assert.That(runtime.MinionChoiceView.Model.PromptLabel,
                Is.EqualTo("CORRECTION (PLACE) - [B] BLUE CHOOSES THE PLACE SPACE"));

            // The passive boundary: forwarded intent raises events and changes nothing authoritative,
            // and a control with no live pending choice behind it submits nothing - the stale case.
            MinionChoiceCandidateViewModel submitted = null;
            runtime.MinionChoiceView.CandidateSubmitted += value => submitted = value;
            runtime.MinionChoiceView.ForwardCandidate(routeCandidates[0]);
            Assert.That(submitted, Is.SameAs(routeCandidates[0]));
            Assert.That(runtime.SubmitCorrectionDestination(destinations[0]), Is.False,
                "No correction is authoritatively pending, so the stale control submits nothing.");
            Assert.That(runtime.SubmitCorrectionSubject(subject.MinionId), Is.False);
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore),
                "The passive prompt reports intent; it never advances authoritative state itself.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshotBefore));

            // Deterministic no-choice corrections expose no prompt: the authoritative projection holds
            // nothing, so the presenter renders null and the scene shows no controls.
            runtime.MinionChoicePresenter.Refresh();
            yield return null;
            Assert.That(runtime.MinionChoiceView.Model, Is.Null);
            Assert.That(subjectView.TargetCueState, Is.EqualTo(BoardTargetCueState.None));
            Assert.That(runtime.BoardView.TileViews.Single(value =>
                value.Coordinate.Equals(subject.Coordinate)).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Effect));
            runtime.BoardPresenter.Refresh();
            yield return null;
            Assert.That(runtime.BoardView.Model.CorrectionDestination, Is.Null,
                "The authoritative projection carries no correction, so no destination stays lit.");
            Assert.That(subjectView.TargetRing, Is.SameAs(subjectRing));
            Assert.That(subjectView.TargetCueState, Is.EqualTo(BoardTargetCueState.None),
                "The same retained ring clears when authority rebuilds the ordinary board.");
            Assert.That(subjectRing.IsVisible, Is.False);
        }

        private static void AssertWash(
            BoardTargetRing ring,
            Color expectedColor,
            float expectedAlpha)
        {
            Assert.That(ring.VisibleWashColor.r, Is.EqualTo(expectedColor.r).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.g, Is.EqualTo(expectedColor.g).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.b, Is.EqualTo(expectedColor.b).Within(0.0001f));
            Assert.That(ring.VisibleWashColor.a, Is.EqualTo(expectedAlpha).Within(0.0001f));
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
