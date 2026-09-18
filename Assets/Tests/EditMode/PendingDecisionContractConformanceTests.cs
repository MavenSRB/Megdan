using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25e4a's exact closing check. The family inventory remains the independent oracle;
    /// this fixture proves that the new application contract, the actual production surfaces, and
    /// authority all agree over the bounded current family/answer algebra.
    /// </summary>
    public sealed class PendingDecisionContractConformanceTests
    {
        [Test]
        public void EveryStandingDecisionPublishesExactlyTheCommandsItsVisibleSurfaceCanSubmitAndAuthorityAccepts()
        {
            AssertContractStructure();
            AssertNonPendingOffersStayOutsideTheContract();

            var cases = CreatePauseCases();
            var expectedRows = DecisionFamilyInventory.Families
                .SelectMany(family => family.Answers.Select(answer =>
                    Row(family.Family, answer.Context, answer.Answer)))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var coveredRows = cases.SelectMany(value => value.CoveredRows)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(coveredRows, Is.EqualTo(expectedRows),
                "Every one of 8.25e4's thirty declared answer rows needs real configured evidence.");

            ContractObservation firstObservation = null;
            var lifecycleFamilies = new HashSet<PendingDecisionFamilyId>();
            foreach (var pauseCase in cases)
            {
                var session = pauseCase.Create();
                if (pauseCase.VerifiesLifecycle && lifecycleFamilies.Add(pauseCase.FamilyId))
                {
                    VerifyLifecycle(pauseCase, session);
                }

                var observation = VerifyPause(pauseCase, session);
                firstObservation = firstObservation ?? observation;
            }

            Assert.That(lifecycleFamilies, Is.EquivalentTo(new[]
            {
                PendingDecisionFamilies.Initiative,
                PendingDecisionFamilies.Movement,
                PendingDecisionFamilies.AttackTarget,
                PendingDecisionFamilies.Defense,
                PendingDecisionFamilies.Effect,
                PendingDecisionFamilies.Correction,
                PendingDecisionFamilies.MinionBattle,
                PendingDecisionFamilies.HeroRespawn,
                PendingDecisionFamilies.SystemTransition,
                PendingDecisionFamilies.RemovalPrevention,
                // Milestone 8.35g. The eleventh, and the one every ordinary turn reaches - so of all
                // eleven it is the family whose lifecycle across replay, Undo, restore, restart and
                // rebuild the rest of the suite leans on most heavily.
                PendingDecisionFamilies.TurnRelease
            }), "Replay, Undo, restore, restart, and rebuild are witnessed for every family.");

            AssertDeliberateViolationsAreObserved(firstObservation);
        }

        private static ContractObservation VerifyPause(PauseCase pauseCase, MatchSession session)
        {
            var projection = session.Projection;
            var contract = projection.DecisionContract;
            Assert.That(contract, Is.Not.Null, pauseCase.Name + " has no application contract.");
            Assert.That(contract.FamilyId, Is.EqualTo(pauseCase.FamilyId), pauseCase.Name);
            Assert.That(DecisionFamilyInventory.CurrentFamily(projection),
                Is.EqualTo(pauseCase.InventoryFamily), pauseCase.Name);

            var oracle = DecisionFamilyInventory.ProjectedCommands(projection, includeDeselect: true);
            Assert.That(contract.Commands, Is.EqualTo(oracle),
                pauseCase.Name + " changed the typed-pending oracle's exact ordered answer set.");
            Assert.That(contract.Commands, Is.Not.Empty, pauseCase.Name);
            Assert.That(contract.Commands.Distinct().Count(), Is.EqualTo(contract.Commands.Count),
                pauseCase.Name + " published a duplicate command.");

            var surface = SurfaceCommands(projection);
            AssertSurfaceEqualsContract(pauseCase.Name, contract, surface);
            AssertSurfaceEqualsContract(
                pauseCase.Name + " after presentation rebuild #1",
                contract,
                SurfaceCommands(projection));
            AssertSurfaceEqualsContract(
                pauseCase.Name + " after presentation rebuild #2",
                contract,
                SurfaceCommands(projection));

            var progressing = contract.Commands.Where(command =>
                    command.Kind != DirectorCommandKind.DeselectMinionBattleRemoval &&
                    !(command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                        command.EffectChoice.Kind == EffectChoiceCommandKind.Deselect))
                .ToArray();
            Assert.That(DeterministicMatchLoop.EnumerateLegalCommands(projection),
                Is.EqualTo(progressing),
                pauseCase.Name + " is not enumerated from the same contract by the configured driver.");

            var accepted = VerifyAuthorityFromRestoredPause(pauseCase, session);
            var observation = new ContractObservation(
                pauseCase.Name,
                isPending: true,
                contract,
                oracle,
                surface,
                accepted);
            Assert.That(ContractAudit.Errors(observation), Is.Empty,
                string.Join(Environment.NewLine, ContractAudit.Errors(observation)));
            return observation;
        }

        private static IReadOnlyList<DirectorCommand> VerifyAuthorityFromRestoredPause(
            PauseCase pauseCase,
            MatchSession session)
        {
            var pausedPayload = session.CaptureSnapshot();
            var answerCount = session.Projection.DecisionContract.Commands.Count;
            var acceptedEvidence = new List<DirectorCommand>();

            for (var index = 0; index < answerCount; index++)
            {
                var restored = session.RestoreSnapshot(pausedPayload);
                Assert.That(restored.Accepted, Is.True, pauseCase.Name + ": " + restored.Detail);
                Assert.That(session.CaptureSnapshot(), Is.EqualTo(pausedPayload),
                    pauseCase.Name + " did not restore byte-identically.");

                var before = session.Projection;
                var beforePayload = session.CaptureSnapshot();
                var command = before.DecisionContract.Commands[index];
                if (index == 0)
                {
                    AssertRejectedAtomically(
                        pauseCase.Name + " unrelated command",
                        session,
                        DirectorCommand.Pass(new HeroId("unrelated-pending-answer")));
                    AssertRejectedAtomically(
                        pauseCase.Name + " forged family command",
                        session,
                        ForgedCommand(before));
                }

                var result = session.Execute(command);
                Assert.That(result.Accepted, Is.True,
                    $"{pauseCase.Name} answer {index + 1} ({command.Kind}) was refused: {result.Detail}");
                Assert.That(result.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.None));
                Assert.That(result.RejectionReason, Is.Not.EqualTo(MatchSessionRejectionReason.EngineFault));
                Assert.That(result.Projection, Is.SameAs(session.Projection));
                Assert.That(result.Projection, Is.Not.SameAs(before));
                Assert.That(result.Projection.Version, Is.EqualTo(before.Version + 1));
                Assert.That(result.Command, Is.SameAs(command));
                Assert.That(result.Projection.AuthoritativeSnapshot,
                    Is.Not.EqualTo(before.AuthoritativeSnapshot));
                AssertSemanticWitness(pauseCase, before, command, result.Projection);
                acceptedEvidence.Add(command);

                var after = session.Projection;
                var afterPayload = session.CaptureSnapshot();
                var stale = session.Execute(command);
                Assert.That(stale.Accepted, Is.False,
                    pauseCase.Name + " accepted one invocation twice.");
                Assert.That(stale.RejectionReason, Is.Not.EqualTo(MatchSessionRejectionReason.EngineFault));
                Assert.That(stale.Projection, Is.SameAs(after));
                Assert.That(session.Projection, Is.SameAs(after));
                Assert.That(session.CaptureSnapshot(), Is.EqualTo(afterPayload),
                    pauseCase.Name + " mutated after a consumed invocation was reused.");
                Assert.That(beforePayload, Is.EqualTo(pausedPayload));
            }

            return acceptedEvidence;
        }

        private static void VerifyLifecycle(PauseCase pauseCase, MatchSession session)
        {
            var pausedPayload = session.CaptureSnapshot();
            var expectedFamily = session.Projection.DecisionContract.FamilyId;
            var expectedCommands = session.Projection.DecisionContract.Commands.ToArray();
            var prefix = session.Projection.AcceptedCommands.ToArray();
            Assert.That(prefix, Is.Not.Empty, pauseCase.Name + " needs a replayable real prefix.");

            Assert.That(session.Undo().Accepted, Is.True, pauseCase.Name + " Undo was refused.");
            Assert.That(session.Execute(prefix.Last()).Accepted, Is.True,
                pauseCase.Name + " could not reapply the retained-prefix command.");
            AssertContractRecreated(pauseCase.Name + " after Undo/reapply", session,
                expectedFamily, expectedCommands);

            Assert.That(session.Restart().Accepted, Is.True, pauseCase.Name + " restart was refused.");
            Assert.That(session.Projection.DecisionContract, Is.Null,
                pauseCase.Name + " initial state incorrectly carries a pending contract.");
            foreach (var command in prefix)
            {
                var replayed = session.Execute(command);
                Assert.That(replayed.Accepted, Is.True,
                    $"{pauseCase.Name} replay refused {command.Kind}: {replayed.Detail}");
            }

            AssertContractRecreated(pauseCase.Name + " after fresh replay", session,
                expectedFamily, expectedCommands);

            Assert.That(session.RestoreSnapshot(pausedPayload).Accepted, Is.True,
                pauseCase.Name + " snapshot restore was refused.");
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(pausedPayload));
            AssertContractRecreated(pauseCase.Name + " after snapshot restore", session,
                expectedFamily, expectedCommands);
            AssertSurfaceEqualsContract(
                pauseCase.Name + " surface after lifecycle reconstruction",
                session.Projection.DecisionContract,
                SurfaceCommands(session.Projection));
        }

        private static void AssertContractRecreated(
            string context,
            MatchSession session,
            PendingDecisionFamilyId family,
            IReadOnlyList<DirectorCommand> commands)
        {
            Assert.That(session.Projection.DecisionContract, Is.Not.Null, context);
            Assert.That(session.Projection.DecisionContract.FamilyId, Is.EqualTo(family), context);
            Assert.That(session.Projection.DecisionContract.Commands, Is.EqualTo(commands), context);
        }

        private static void AssertRejectedAtomically(
            string context,
            MatchSession session,
            DirectorCommand command)
        {
            var before = session.Projection;
            var payload = session.CaptureSnapshot();
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.False, context);
            Assert.That(result.RejectionReason, Is.Not.EqualTo(MatchSessionRejectionReason.EngineFault), context);
            Assert.That(result.Projection, Is.SameAs(before), context);
            Assert.That(session.Projection, Is.SameAs(before), context);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(payload), context);
        }

        private static DirectorCommand ForgedCommand(MatchSessionProjection projection)
        {
            if (projection.PendingInitiativeChoice != null)
                return DirectorCommand.ChooseTiedActor(projection.PendingInitiativeChoice.OwningSide,
                    new CardInstanceId("forged-initiative-card"));
            if (projection.PendingMovementChoice != null)
            {
                var choice = projection.PendingMovementChoice;
                return DirectorCommand.ChooseMovementDestination(
                    choice.ResolutionId, choice.ChoiceId, new HexCoordinate(99, 99));
            }
            if (projection.PendingAttackTargetChoice != null)
            {
                var choice = projection.PendingAttackTargetChoice;
                return DirectorCommand.ChooseAttackTarget(
                    choice.ResolutionId, choice.ChoiceId, new HeroId("forged-attack-target"));
            }
            if (projection.PendingDefenseChoice != null)
            {
                var choice = projection.PendingDefenseChoice;
                return DirectorCommand.ChooseDefenseCard(
                    choice.ResolutionId, choice.ChoiceId, new CardInstanceId("forged-defense-card"));
            }
            if (projection.PendingEffectChoice != null)
            {
                var choice = projection.PendingEffectChoice;
                return DirectorCommand.AnswerEffectChoice(new EffectChoiceCommand(
                    choice.ResolutionId, new EffectChoiceId("forged-effect-choice"),
                    EffectChoiceCommandKind.Stop, default, choice.ChooserId));
            }
            if (projection.PendingDisplacedMinionCorrection != null)
            {
                var invocation = projection.PendingDisplacedMinionCorrection.SelectInvocations[0];
                return invocation.Kind == DisplacedMinionCorrectionCommandKind.SelectSubject
                    ? DirectorCommand.ChooseCorrectionSubject(new DisplacedMinionCorrectionCommand(
                        invocation.Kind, invocation.ResolutionId,
                        new ActionResolutionStepId("forged-correction-choice"),
                        invocation.ChoosingSide, invocation.MinionId, invocation.Destination))
                    : DirectorCommand.ChooseCorrectionDestination(new DisplacedMinionCorrectionCommand(
                        invocation.Kind, invocation.ResolutionId,
                        new ActionResolutionStepId("forged-correction-choice"),
                        invocation.ChoosingSide, invocation.MinionId, invocation.Destination));
            }
            if (projection.PendingMinionBattleChoice != null)
            {
                var choice = projection.PendingMinionBattleChoice;
                return DirectorCommand.SelectMinionBattleRemoval(new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Select, choice.ResolutionId,
                    new EndOfRoundChoiceId("forged-minion-battle-choice"),
                    choice.ChoosingSide, choice.Candidates[0].Unit));
            }
            if (projection.PendingHeroRespawnChoice != null)
            {
                var choice = projection.PendingHeroRespawnChoice;
                return DirectorCommand.RespawnHero(new HeroRespawnCommand(
                    HeroRespawnCommandKind.Respawn, choice.HeroId, choice.CardInstanceId,
                    new HexCoordinate(99, 99)));
            }
            if (projection.PendingSystemTransitionStep != null)
            {
                var choice = projection.PendingSystemTransitionStep;
                return DirectorCommand.AnswerSystemTransitionStep(new SystemTransitionChoiceCommand(
                    choice.InstanceId, choice.StepId, choice.ItemId, choice.ChoosingSide,
                    new SystemTransitionOptionId("forged-system-option")));
            }

            if (projection.PendingTurnRelease != null)
            {
                // Milestone 8.35g. The release carries no identity at all - its whole content is a
                // consent, so there is no WRONG release to forge and this family cannot be asked
                // the question the others are asked in their own terms. What can be forged is a
                // command from the turn the pause is holding, which the pause must refuse without
                // moving anything: the same atomicity claim, asked the only way this family admits.
                return DirectorCommand.PerformHold(
                    new CardInstanceId("forged-turn-release-card"));
            }

            var removal = projection.PendingRemovalPrevention;
            Assert.That(removal, Is.Not.Null, "The projection has no pending family to forge.");
            return DirectorCommand.AnswerRemovalPrevention(RemovalPreventionCommand.Decline(
                "forged-removal-request", removal.DecidingHeroId));
        }

        private static void AssertSemanticWitness(
            PauseCase pauseCase,
            MatchSessionProjection before,
            DirectorCommand command,
            MatchSessionProjection after)
        {
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.Initiative))
            {
                Assert.That(after.SelectedActor.CardInstanceId, Is.EqualTo(command.CardInstanceId));
                Assert.That(after.PendingInitiativeChoice, Is.Null);
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.Movement))
            {
                if (command.Kind == DirectorCommandKind.StopMovement)
                    Assert.That(after.PendingMovementChoice, Is.Null);
                else
                    Assert.That(after.Heroes.Single(hero => hero.HeroId.Equals(
                            before.PendingMovementChoice.MovingHeroId)).Coordinate,
                        Is.EqualTo(command.Coordinate));
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.AttackTarget))
            {
                Assert.That(after.PendingAttackTargetChoice, Is.Null);
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.Defense))
            {
                Assert.That(after.PendingDefenseChoice, Is.Null);
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.Effect))
            {
                if (command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    before.PendingEffectChoice.MinimumSelections == 1 &&
                    before.PendingEffectChoice.MaximumSelections == 1 &&
                    before.PendingEffectChoice.Kind != EffectChoiceKind.ConfirmOrDecline)
                {
                    // Milestone 8.46b: an exactly-one choice resolves on its selection, so the witness
                    // is the pause closing - gone, replaced, or re-asked empty - never a selection
                    // left standing on it.
                    var standing = after.PendingEffectChoice;
                    Assert.That(
                        standing == null || standing.SelectedCandidates.Count == 0 ||
                        !standing.ChoiceId.Equals(before.PendingEffectChoice.ChoiceId) ||
                        !standing.ResolutionId.Equals(before.PendingEffectChoice.ResolutionId),
                        Is.True, pauseCase.Name + ": an exactly-one selection must close its own pause.");
                    Assert.That(EffectFingerprint(after), Is.Not.EqualTo(EffectFingerprint(before)));
                }
                else if (command.EffectChoice.Kind == EffectChoiceCommandKind.Select ||
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Deselect)
                {
                    Assert.That(after.PendingEffectChoice, Is.Not.Null);
                    Assert.That(after.PendingEffectChoice.SelectedCandidates,
                        Is.Not.EqualTo(before.PendingEffectChoice.SelectedCandidates));
                }
                else
                    Assert.That(EffectFingerprint(after), Is.Not.EqualTo(EffectFingerprint(before)));
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.Correction))
            {
                Assert.That(CorrectionFingerprint(after), Is.Not.EqualTo(CorrectionFingerprint(before)));
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))
            {
                if (command.Kind == DirectorCommandKind.SelectMinionBattleRemoval ||
                    command.Kind == DirectorCommandKind.DeselectMinionBattleRemoval)
                {
                    Assert.That(after.PendingMinionBattleChoice, Is.Not.Null);
                    Assert.That(after.PendingMinionBattleChoice.SelectedUnits,
                        Is.Not.EqualTo(before.PendingMinionBattleChoice.SelectedUnits));
                }
                else
                    Assert.That(MinionBattleFingerprint(after),
                        Is.Not.EqualTo(MinionBattleFingerprint(before)));
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.HeroRespawn))
            {
                var choice = before.PendingHeroRespawnChoice;
                Assert.That(after.PendingHeroRespawnChoice, Is.Null);
                if (command.Kind == DirectorCommandKind.RespawnHero)
                    Assert.That(after.Heroes.Single(hero => hero.HeroId.Equals(choice.HeroId)).Coordinate,
                        Is.EqualTo(command.Coordinate));
                else
                {
                    Assert.That(after.Heroes.Any(hero => hero.HeroId.Equals(choice.HeroId)), Is.False);
                    Assert.That(after.Cards.Single(card => card.Id.Equals(choice.CardInstanceId))
                        .State.ResolutionStatus, Is.Not.EqualTo(CardResolutionStatus.Unresolved));
                }
                return;
            }
            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.SystemTransition))
            {
                Assert.That(SystemTransitionFingerprint(after),
                    Is.Not.EqualTo(SystemTransitionFingerprint(before)));
                return;
            }

            if (pauseCase.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
            {
                // Milestone 8.35g. The release's whole visible effect is that the turn is handed on:
                // the pause is gone, and the match is no longer sitting on the card it named.
                // Naming WHICH of the three places it went - the next actor, end of round, or the
                // next turn's commitments - would tie this case to one fixture's board rather than
                // to what the release does.
                Assert.That(before.PendingTurnRelease, Is.Not.Null);
                Assert.That(after.PendingTurnRelease, Is.Null);
                Assert.That(
                    after.SelectedActor.CardInstanceId.Equals(
                        before.PendingTurnRelease.CardInstanceId),
                    Is.False,
                    "The turn it released is not the turn it is still sitting on.");
                return;
            }

            var removal = before.PendingRemovalPrevention;
            Assert.That(after.PendingRemovalPrevention, Is.Null);
            if (command.RemovalPrevention.Accepted)
            {
                Assert.That(after.Cards.Single(card => card.Id.Equals(
                        command.RemovalPrevention.PaymentCardInstanceId)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile));
                Assert.That(after.Minions.Any(minion => minion.MinionId.Equals(removal.SubjectMinionId)),
                    Is.True);
            }
            else
                Assert.That(after.Minions.Any(minion => minion.MinionId.Equals(removal.SubjectMinionId)),
                    Is.False);
        }

        private static string EffectFingerprint(MatchSessionProjection projection)
        {
            var choice = projection.PendingEffectChoice;
            return choice == null ? "none" : choice.ResolutionId + "/" + choice.ChoiceId + "/" +
                choice.NodeId + "/" + string.Join(",", choice.SelectedCandidates.Select(v => v.ToString()));
        }

        private static string CorrectionFingerprint(MatchSessionProjection projection)
        {
            var choice = projection.PendingDisplacedMinionCorrection;
            return choice == null ? "none" : choice.ResolutionId + "/" + choice.ChoiceId + "/" +
                choice.Stage + "/" + choice.SubjectMinionId;
        }

        private static string MinionBattleFingerprint(MatchSessionProjection projection)
        {
            var choice = projection.PendingMinionBattleChoice;
            return choice == null ? "none" : choice.ResolutionId + "/" + choice.ChoiceId + "/" +
                string.Join(",", choice.SelectedUnits.Select(v => v.ToString()));
        }

        private static string SystemTransitionFingerprint(MatchSessionProjection projection)
        {
            var choice = projection.PendingSystemTransitionStep;
            return choice == null ? "none" : choice.InstanceId + "/" + choice.StepId + "/" +
                choice.ItemId + "/" + string.Join(",", choice.CompletedItemIds.Select(v => v.ToString()));
        }

        private static IReadOnlyList<DirectorCommand> SurfaceCommands(MatchSessionProjection projection)
        {
            var contract = projection.DecisionContract;
            Assert.That(contract, Is.Not.Null);
            if (contract.FamilyId.Equals(PendingDecisionFamilies.Movement))
            {
                var recorder = new RecordingSession(projection);
                var presenter = new BoardInteractionPresenter(recorder, new CapturingBoardView());
                foreach (var destination in projection.PendingMovementChoice.LegalDestinations)
                    Assert.That(presenter.SubmitDestination(destination.Destination), Is.True);
                var commands = recorder.Commands.ToList();
                var stop = RoundActionPresenter.CreateViewModel(projection).EndTurnInvocation;
                if (stop != null) commands.Add(stop.Command);
                return commands;
            }
            if (contract.FamilyId.Equals(PendingDecisionFamilies.AttackTarget))
            {
                var recorder = new RecordingSession(projection);
                var presenter = new BoardInteractionPresenter(recorder, new CapturingBoardView());
                foreach (var hero in projection.PendingAttackTargetChoice.LegalTargetHeroIds)
                    Assert.That(presenter.SubmitHero(hero), Is.True);
                foreach (var minion in projection.PendingAttackTargetChoice.LegalTargetMinionIds)
                    Assert.That(presenter.SubmitMinion(minion), Is.True);
                return recorder.Commands;
            }
            if (contract.FamilyId.Equals(PendingDecisionFamilies.Correction) ||
                contract.FamilyId.Equals(PendingDecisionFamilies.MinionBattle))
            {
                var model = MinionChoicePresenter.CreateViewModel(projection);
                return model.Candidates.Select(value => value.Command)
                    .Concat(model.ConfirmInvocation == null
                        ? Enumerable.Empty<DirectorCommand>()
                        : new[] { model.ConfirmInvocation.Command })
                    .ToArray();
            }
            if (contract.FamilyId.Equals(PendingDecisionFamilies.SystemTransition) &&
                projection.PendingSystemTransitionDescription?.Kind ==
                    SystemTransitionPromptKind.LanePushRespawnPlace)
                return BoardInteractionPresenter.CreateSystemTransitionDestinations(projection)
                    .Select(value => value.Command).ToArray();

            var round = RoundActionPresenter.CreateViewModel(projection);
            if (contract.FamilyId.Equals(PendingDecisionFamilies.Initiative))
                return round.InitiativeChoices.Select(value => value.Command).ToArray();
            if (contract.FamilyId.Equals(PendingDecisionFamilies.Defense))
                return round.DefenseChoices.Select(value => value.Command).ToArray();
            if (contract.FamilyId.Equals(PendingDecisionFamilies.Effect))
            {
                var ordinary = round.EffectChoices.Select(value => value.Command)
                    .Concat(round.EffectChoiceTargets.Select(value => value.Command))
                    .Concat(round.EffectChoiceDestinations.Select(value => value.Command))
                    .ToArray();
                // Milestone 8.35v: an undeclined optional branch's own Decline is now ALSO
                // reachable through END TURN, which reads the identical pre-built invocation
                // (`PendingEffectChoice.DeclineInvocation`) rather than composing a second one -
                // so it names no additional capability and folds in only when it is not already
                // one of the ordinary controls above. The Stop shape (every candidate refused,
                // `EffectChoices` empty) is unaffected and still adds its own command as before.
                return round.EndTurnInvocation == null ||
                        ordinary.Any(command => ReferenceEquals(command, round.EndTurnInvocation.Command))
                    ? ordinary
                    : ordinary.Concat(new[] { round.EndTurnInvocation.Command }).ToArray();
            }
            if (contract.FamilyId.Equals(PendingDecisionFamilies.HeroRespawn))
                return round.HeroRespawnOptions.Select(value => value.Command)
                    .Concat(new[] { round.ResolveWithoutRespawn.Command }).ToArray();
            // Milestone 8.35g. The eleventh family's whole visible surface is the one control
            // that finished the resolution, coming back to carry the release - so it maps to END
            // TURN rather than falling through to the removal-prevention arm below.
            if (contract.FamilyId.Equals(PendingDecisionFamilies.TurnRelease))
                return new[] { round.EndTurnInvocation.Command };
            if (contract.FamilyId.Equals(PendingDecisionFamilies.SystemTransition))
                return round.SystemTransitionPrompt.Options.Select(value => value.Command).ToArray();
            return round.RemovalPreventionPrompt.Payments.Select(value => value.Command)
                .Concat(new[] { round.RemovalPreventionPrompt.Decline.Command }).ToArray();
        }

        private static void AssertSurfaceEqualsContract(
            string context, PendingDecisionContract contract, IReadOnlyList<DirectorCommand> surface)
        {
            Assert.That(surface.Count, Is.EqualTo(contract.Commands.Count), context);
            Assert.That(surface.Distinct().Count(), Is.EqualTo(surface.Count),
                context + " duplicated a submittable command.");
            Assert.That(surface, Is.EquivalentTo(contract.Commands),
                context + " has a missing, disabled, decorative, or unrelated answer.");
            foreach (var command in contract.Commands)
                Assert.That(surface.Count(candidate => ReferenceEquals(candidate, command)), Is.EqualTo(1),
                    context + " reconstructed an equal payload instead of forwarding the exact command.");
        }

        private static void AssertContractStructure()
        {
            var applicationAssembly = typeof(MatchSessionProjection).Assembly;
            var contractType = applicationAssembly.GetType(
                "GuardsDigital.Application.PendingDecisionContract", throwOnError: false);
            var contractProperty = typeof(MatchSessionProjection).GetProperty(
                nameof(MatchSessionProjection.DecisionContract), BindingFlags.Instance | BindingFlags.Public);
            Assert.That(contractType, Is.Not.Null,
                "The immutable production PendingDecisionContract is absent.");
            Assert.That(contractType.IsSealed, Is.True);
            Assert.That(contractType.GetConstructors(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(contractType.GetProperties().Select(property => property.Name),
                Is.EquivalentTo(new[] { nameof(PendingDecisionContract.FamilyId),
                    nameof(PendingDecisionContract.Commands) }));
            Assert.That(contractType.GetProperties().All(property => property.SetMethod == null), Is.True);
            Assert.That(contractProperty, Is.Not.Null);
            Assert.That(contractProperty.SetMethod, Is.Null);

            var familyIds = typeof(PendingDecisionFamilies)
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(field => field.FieldType == typeof(PendingDecisionFamilyId))
                .Select(field => ((PendingDecisionFamilyId)field.GetValue(null)).Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(familyIds, Is.EqualTo(new[]
            {
                "attack-target", "correction", "defense", "effect", "hero-respawn", "initiative",
                "minion-battle", "movement", "removal-prevention", "system-transition", "turn-release"
            }));
            Assert.That(DecisionFamilyInventory.AuditProductionInventory(), Is.Empty);
            // Milestone 8.35u adds the tenth: the per-open-item transition descriptions p.15's
            // simultaneous levelling needs, one per hero still choosing. It is a companion and not a
            // twelfth family for the same reason the singular description is one - it describes the
            // pause the system-transition family already stands on and adds no decision of its own.
            Assert.That(DecisionFamilyInventory.CompanionProjectionMembers, Has.Length.EqualTo(10));
            Assert.That(DecisionFamilyInventory.Families, Has.Length.EqualTo(11));
            // Milestone 8.35g: thirty until the turn release, which adds exactly one answer row
            // because pressing the release is the whole of what that family admits.
            Assert.That(DecisionFamilyInventory.Families.Sum(value => value.Answers.Count), Is.EqualTo(31));
        }

        private static void AssertNonPendingOffersStayOutsideTheContract()
        {
            var initial = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            Assert.That(initial.Projection.DecisionContract, Is.Null);
            var red = HandCard(initial, TeamSide.Red, WaspCardContent.LiftUp);
            var blue = HandCard(initial, TeamSide.Blue, WaspCardContent.StopProjectiles);
            Accept(initial, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Assert.That(initial.Projection.DecisionContract, Is.Null);
            Accept(initial, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(initial.Projection.DecisionContract, Is.Null);
            Assert.That(initial.Projection.AvailableTurnActions, Is.Not.Empty);
            Assert.That(initial.Projection.AvailableHoldOffer.HasValue, Is.True);
            Assert.That(DecisionFamilyInventory.NonPendingOffers.Select(value => value.Offer),
                Is.EqualTo(new[] { "Commitments", "Ordinary action selection", "Hold", "Fast Travel",
                    "Clear", "End-of-round advance" }));
            Assert.That(DecisionFamilyInventory.NonPendingOffers.Any(value =>
                value.ProjectionSource.StartsWith("Pending", StringComparison.Ordinal)), Is.False);
        }

        private static void AssertDeliberateViolationsAreObserved(ContractObservation real)
        {
            var probeCommand = DirectorCommand.Pass(new HeroId("throwaway-probe-hero"));
            var missingContract = new ContractObservation("PendingThrowawayChoice", true, null,
                new[] { probeCommand }, Array.Empty<DirectorCommand>(), Array.Empty<DirectorCommand>());
            AssertErrorsContain(missingContract, "missing application contract");
            var probeContract = new PendingDecisionContract(
                new PendingDecisionFamilyId("throwaway-probe"), new[] { probeCommand });
            var missingSurface = new ContractObservation("PendingThrowawayChoice", true, probeContract,
                new[] { probeCommand }, Array.Empty<DirectorCommand>(), Array.Empty<DirectorCommand>());
            AssertErrorsContain(missingSurface, "missing interactive surface");
            var missingEvidence = new ContractObservation("PendingThrowawayChoice", true, probeContract,
                new[] { probeCommand }, new[] { probeCommand }, Array.Empty<DirectorCommand>());
            AssertErrorsContain(missingEvidence, "missing answer/result evidence");
            var complete = new ContractObservation("PendingThrowawayChoice", true, probeContract,
                new[] { probeCommand }, new[] { probeCommand }, new[] { probeCommand });
            Assert.That(ContractAudit.Errors(complete), Is.Empty);

            AssertErrorsContain(real.WithSurface(real.Surface.Skip(1)), "surface command multiset differs");
            AssertErrorsContain(real.WithSurface(real.Surface.Concat(new[] { real.Surface[0] })),
                "duplicate surface command");
            var alteredCommands = real.Contract.Commands
                .Concat(new[] { DirectorCommand.Pass(new HeroId("altered-allowed-set")) }).ToArray();
            var altered = new ContractObservation(real.Name, true,
                new PendingDecisionContract(real.Contract.FamilyId, alteredCommands),
                real.Oracle, alteredCommands, alteredCommands);
            AssertErrorsContain(altered, "allowed command set differs from typed oracle");
        }

        private static void AssertErrorsContain(ContractObservation observation, string fragment)
        {
            var errors = ContractAudit.Errors(observation);
            Assert.That(errors.Any(error => error.Contains(fragment)), Is.True,
                $"Expected '{fragment}', got: {string.Join(" | ", errors)}");
        }

        private static PauseCase[] CreatePauseCases() => new[]
        {
            Case("same-team Initiative tie", PendingDecisionFamilies.Initiative, "Initiative",
                CreateInitiativePause, true, Row("Initiative", "Initiative", "Choose tied actor")),
            Case("Movement destinations and Stop", PendingDecisionFamilies.Movement, "Movement",
                CreateMovementPause, true, Row("Movement", "Movement", "Continue"),
                Row("Movement", "Movement", "Stop")),
            Case("mixed hero/minion Attack targets", PendingDecisionFamilies.AttackTarget, "Attack target",
                CreateAttackTargetPause, true, Row("Attack target", "Attack target", "Hero"),
                Row("Attack target", "Attack target", "Minion")),
            Case("numeric Defense and decline", PendingDecisionFamilies.Defense, "Defense",
                ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse, true,
                Row("Defense", "Defense", "Numeric Defense"), Row("Defense", "Defense", "Decline")),
            Case("fulfilled Block", PendingDecisionFamilies.Defense, "Defense",
                CreateFulfilledBlockPause, false, Row("Defense", "Defense", "Fulfilled Block")),
            Case("unfulfilled Block", PendingDecisionFamilies.Defense, "Defense",
                CreateUnfulfilledBlockPause, false, Row("Defense", "Defense", "Unfulfilled Block")),
            Case("Skill effect Select", PendingDecisionFamilies.Effect, "Effect",
                CreateSkillEffectPause, true,
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Select.ToString())),
            Case("Skill effect Deselect and Confirm", PendingDecisionFamilies.Effect, "Effect",
                CreateSelectedSkillEffectPause, false,
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Deselect.ToString()),
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Confirm.ToString())),
            Case("Skill repeat Confirm and Decline", PendingDecisionFamilies.Effect, "Effect",
                MatchSessionRepeatDispatchTests.CreateRepeatDecisionPause, false,
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Confirm.ToString()),
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Decline.ToString())),
            Case("Skill effect Stop", PendingDecisionFamilies.Effect, "Effect",
                MatchSessionRepeatDispatchTests.CreateStopDecisionPause, false,
                Row("Effect", "Effect/Skill", EffectChoiceCommandKind.Stop.ToString())),
            Case("Block rider effect Select", PendingDecisionFamilies.Effect, "Effect",
                CreateAttackEffectPause, false,
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Select.ToString())),
            Case("Block rider effect Deselect and Confirm", PendingDecisionFamilies.Effect, "Effect",
                CreateSelectedAttackEffectPause, false,
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Deselect.ToString()),
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Confirm.ToString())),
            Case("Attack repeat Confirm and Decline", PendingDecisionFamilies.Effect, "Effect",
                CreateAttackRepeatPause, false,
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Confirm.ToString()),
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Decline.ToString())),
            Case("Attack-text late refusal Stop", PendingDecisionFamilies.Effect, "Effect",
                CreateAttackEffectStopPause, false,
                Row("Effect", "Effect/Attack or Block", EffectChoiceCommandKind.Stop.ToString())),
            Case("correction subject", PendingDecisionFamilies.Correction, "Correction",
                DisplacedMinionCorrectionTests.CreateSubjectDecisionPause, true,
                Row("Correction", "Displaced minion correction",
                    DisplacedMinionCorrectionCommandKind.SelectSubject.ToString())),
            Case("correction destination", PendingDecisionFamilies.Correction, "Correction",
                DisplacedMinionCorrectionTests.CreateDestinationDecisionPause, false,
                Row("Correction", "Displaced minion correction",
                    DisplacedMinionCorrectionCommandKind.SelectDestination.ToString())),
            Case("Minion Battle Select", PendingDecisionFamilies.MinionBattle, "Minion Battle",
                MinionChoicePresentationTests.CreatePendingRemovalSession, true,
                Row("Minion Battle", "Minion Battle", MinionBattleChoiceCommandKind.Select.ToString())),
            Case("Minion Battle Deselect and Confirm", PendingDecisionFamilies.MinionBattle, "Minion Battle",
                CreateSelectedMinionBattlePause, false,
                Row("Minion Battle", "Minion Battle", MinionBattleChoiceCommandKind.Deselect.ToString()),
                Row("Minion Battle", "Minion Battle", MinionBattleChoiceCommandKind.Confirm.ToString())),
            Case("Hero respawn and decline", PendingDecisionFamilies.HeroRespawn, "Hero respawn",
                HeroRespawnTests.CreatePendingRespawnSession, true,
                Row("Hero respawn", "Hero respawn", HeroRespawnCommandKind.Respawn.ToString()),
                Row("Hero respawn", "Hero respawn", HeroRespawnCommandKind.ResolveWithoutAction.ToString())),
            Case("Level-Up system transition", PendingDecisionFamilies.SystemTransition, "System transition",
                () => ConfiguredMatchFixtures.CreateSessionPausedOnLevelUpUpgrade(out _), true,
                Row("System transition", "System transition", "Level-Up upgrade")),
            Case("Lane-Push Place system transition", PendingDecisionFamilies.SystemTransition, "System transition",
                CreateLanePushPlacePause, false,
                Row("System transition", "System transition", "Lane-Push respawn/Place")),
            Case("configured removal prevention", PendingDecisionFamilies.RemovalPrevention,
                "Removal prevention", ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause,
                true, Row("Removal prevention", "Removal prevention", "Projected payment"),
                Row("Removal prevention", "Removal prevention", "Decline")),
            // Milestone 8.35g. The eleventh family, and the only one whose whole content is a
            // consent: one command, no candidates, and the same control that finished the
            // resolution. It carries the lifecycle flag like the other owned families, because a
            // release that did not survive replay, Undo, restore and rebuild would strand the turn.
            Case("turn release", PendingDecisionFamilies.TurnRelease, "Turn release",
                CreateTurnReleasePause, true,
                Row("Turn release", "Turn release", "Release the finished turn"))
        };

        private static PauseCase Case(string name, PendingDecisionFamilyId familyId,
            string inventoryFamily, Func<MatchSession> create, bool lifecycle, params string[] rows) =>
            new PauseCase(name, familyId, inventoryFamily, create, lifecycle, rows);

        private static string Row(string family, string context, string answer) =>
            family + "/" + context + "/" + answer;

        /// <summary>
        /// Milestone 8.35g. A turn whose card resolution has finished and which is waiting for its
        /// own player. The Hold is the shortest completion there is, so the pause is reached by the
        /// rulebook's own simplest turn rather than by staging one.
        /// </summary>
        private static MatchSession CreateTurnReleasePause()
        {
            var session = ConfiguredMatchFixtures.CreateConfiguredSession();
            ConfiguredMatchFixtures.CommitBothHands(session);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                session.Projection.SelectedActor.CardInstanceId));
            Assert.That(session.Projection.PendingTurnRelease, Is.Not.Null,
                "p.12's Hold completes the card, and the completed turn waits for its own player.");
            return session;
        }

        private static MatchSession CreateInitiativePause()
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants, map, configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(configuration.Participants,
                configuration.CardDefinitions, configuration.CardInstances, configuration.TieBreakerState,
                configuration.SetupSeed, map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
            var hand = session.Projection.Cards.Where(card => card.State.Location == CardLocation.Hand)
                .Select(card => new { Card = card,
                    Side = session.Projection.Participants.GetTeamForHero(card.OwnerId).Side,
                    Initiative = DefinitionOf(session, card).Initiative.Value }).ToArray();
            var heroes = session.Projection.Setup.Heroes.Select(value => value.HeroId).ToArray();
            var tie = hand.Where(value => value.Side == TeamSide.Red)
                .GroupBy(value => value.Initiative)
                .Where(group => group.Select(value => value.Card.OwnerId).Distinct().Count() >= 2)
                .OrderByDescending(group => group.Key)
                .Select(group => new { Initiative = group.Key,
                    Cards = group.GroupBy(value => value.Card.OwnerId)
                        .Select(owner => owner.OrderBy(value => value.Card.Id.Value, StringComparer.Ordinal)
                            .First().Card).Take(2).ToArray() })
                .First(candidate => heroes.Where(hero => candidate.Cards.All(card => !card.OwnerId.Equals(hero)))
                    .All(hero => hand.Where(value => value.Card.OwnerId.Equals(hero))
                        .Min(value => value.Initiative) < candidate.Initiative));
            var tiedOwners = new HashSet<HeroId>(tie.Cards.Select(card => card.OwnerId));
            foreach (var hero in heroes)
            {
                var card = tiedOwners.Contains(hero) ? tie.Cards.Single(value => value.OwnerId.Equals(hero))
                    : hand.Where(value => value.Card.OwnerId.Equals(hero)).OrderBy(value => value.Initiative)
                        .ThenBy(value => value.Card.Id.Value, StringComparer.Ordinal).First().Card;
                Accept(session, DirectorCommand.CommitCard(hero, card.Id));
            }
            Assert.That(session.Projection.PendingInitiativeChoice, Is.Not.Null);
            return session;
        }

        private static MatchSession CreateMovementPause()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            Commit(session, WaspCardContent.LiftUp, WaspCardContent.StopProjectiles);
            StartAction(session, ActionKind.Movement);
            Assert.That(session.Projection.PendingMovementChoice?.CanStop, Is.True);
            Assert.That(session.Projection.PendingMovementChoice.LegalDestinations, Is.Not.Empty);
            return session;
        }

        private static MatchSession CreateAttackTargetPause()
        {
            var session = ConfiguredMatchFixtures.CreateAdjacentHeroAndMinionTargetSession(out _);
            Commit(session, WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles);
            StartAction(session, ActionKind.Attack);
            Assert.That(session.Projection.PendingAttackTargetChoice.LegalTargetHeroIds, Is.Not.Empty);
            Assert.That(session.Projection.PendingAttackTargetChoice.LegalTargetMinionIds, Is.Not.Empty);
            return session;
        }

        private static MatchSession CreateFulfilledBlockPause()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(WaspCardContent.ReflectProjectiles);
            ConfiguredDefenseCardTests.CommitRedAgainst(session, WaspCardContent.ThunderBoomerang,
                WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session,
                ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue));
            AssertBlockPreview(session, WaspCardContent.ReflectProjectiles, DefenseBlockResult.Blocked);
            return session;
        }

        private static MatchSession CreateUnfulfilledBlockPause()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            ConfiguredDefenseCardTests.CommitRedAgainst(session, WaspCardContent.MagneticDagger,
                WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session,
                ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Blue));
            AssertBlockPreview(session, WaspCardContent.StopProjectiles,
                DefenseBlockResult.ConditionUnfulfilled);
            return session;
        }

        private static void AssertBlockPreview(MatchSession session, CardDefinitionId cardId,
            DefenseBlockResult expected)
        {
            var offer = session.Projection.PendingDefenseChoice.LegalDefenseOffers.Single(value =>
                value.SourceDefinitionReference.DefinitionId.Equals(cardId));
            var preview = session.Projection.PendingDefensePreview.Choices.Single(value =>
                value.CardInstanceId.HasValue && value.CardInstanceId.Value.Equals(offer.SourceCardInstanceId));
            Assert.That(preview.BlockResult, Is.EqualTo(expected));
        }

        private static MatchSession CreateSkillEffectPause()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedVerifiedMirrorSession();
            Commit(session, WaspCardContent.LiftUp, WaspCardContent.StopProjectiles);
            StartAction(session, ActionKind.Skill);
            Assert.That(session.Projection.PendingSkillResolution, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.SelectInvocations, Is.Not.Empty);
            return session;
        }

        private static MatchSession CreateSelectedSkillEffectPause()
        {
            var session = CreateSkillEffectPause();
            Accept(session, session.Projection.DecisionContract.Commands.Single(command =>
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select));
            Assert.That(session.Projection.PendingEffectChoice.DeselectInvocations, Is.Not.Empty);
            Assert.That(session.Projection.PendingEffectChoice.ConfirmInvocation, Is.Not.Null);
            return session;
        }

        private static MatchSession CreateAttackEffectPause()
        {
            var session = ConfiguredMatchFixtures.CreateBlockRiderSession(WaspCardContent.ReflectProjectiles);
            ConfiguredDefenseCardTests.BlockTheRangedAttack(session, WaspCardContent.ReflectProjectiles);
            Assert.That(session.Projection.PendingAttackResolution, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.SelectInvocations, Is.Not.Empty);
            return session;
        }

        private static MatchSession CreateSelectedAttackEffectPause()
        {
            var session = CreateAttackEffectPause();
            Accept(session, session.Projection.DecisionContract.Commands.Single(command =>
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select));
            Assert.That(session.Projection.PendingEffectChoice.DeselectInvocations, Is.Not.Empty);
            Assert.That(session.Projection.PendingEffectChoice.ConfirmInvocation, Is.Not.Null);
            return session;
        }

        private static MatchSession CreateAttackRepeatPause()
        {
            var session = ConfiguredMatchFixtures.CreateThunderBoomerangSession(out _);
            Commit(session, WaspCardContent.ThunderBoomerang, WaspCardContent.StopProjectiles);
            StartAction(session, ActionKind.Attack);
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(target.ResolutionId, target.ChoiceId,
                target.LegalTargetHeroIds.Single()));
            var defense = session.Projection.PendingDefenseChoice;
            Accept(session, DirectorCommand.DeclineDefense(defense.ResolutionId, defense.ChoiceId));
            Assert.That(session.Projection.PendingAttackResolution, Is.Null,
                "The after-action repeat is an Effect decision after the Attack backend completed; " +
                "its companion resolution envelope must not be promoted into the pending algebra.");
            Assert.That(session.Projection.PendingEffectChoice.ConfirmInvocation, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.DeclineInvocation, Is.Not.Null);
            return session;
        }

        private static MatchSession CreateAttackEffectStopPause()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = BoardMapDefinition.CreateSynthetic(new MapId("pending-contract-attack-stop"), 1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var red = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blue = configuration.Participants.Heroes.Single(hero =>
                configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var redTeam = configuration.Participants.GetTeamForHero(red);
            var blueTeam = configuration.Participants.GetTeamForHero(blue);
            var action = new ActionDefinition(new ActionDefinitionId("pending-contract-stop-attack"),
                ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 1) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged, default, LateRefusalAttackEffect(), default,
                PrintedDefenseResolution.None, CardTextTiming.BeforeTheAttack);
            var definition = new CardDefinition(new CardDefinitionId("pending-contract-stop-card"),
                "pending-contract-stop-rev-1", "TEST - Pending Contract Stop", new InitiativeValue(13),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
                new[] { action });
            var instance = CardInstance.CreateInHand(new CardInstanceId("pending-contract-stop-card-instance"),
                definition.Reference, red);
            var setup = new QuickGameSetupState(map.Reference, 3, configuration.TieBreakerState,
                new[] { new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone")) },
                new[] { HeroSetup(red, redTeam.Id, TeamSide.Red, map, new HexCoordinate(0, 0)),
                    HeroSetup(blue, blueTeam.Id, TeamSide.Blue, map, new HexCoordinate(1, 0)) },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(configuration.Participants,
                configuration.CardDefinitions.Concat(new[] { definition }),
                configuration.CardInstances.Concat(new[] { instance }), configuration.TieBreakerState,
                map.Reference, setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            var session = new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
            var blueCard = session.Projection.Cards
                .Where(card => card.OwnerId.Equals(blue) && card.State.Location == CardLocation.Hand)
                .OrderBy(card => DefinitionOf(session, card).Initiative.Value).First();
            Accept(session, DirectorCommand.CommitCard(red, instance.Id));
            Accept(session, DirectorCommand.CommitCard(blue, blueCard.Id));
            StartAction(session, ActionKind.Attack);
            var target = session.Projection.PendingAttackTargetChoice;
            Accept(session, DirectorCommand.ChooseAttackTarget(target.ResolutionId, target.ChoiceId, blue));
            Assert.That(session.Projection.PendingAttackResolution, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.StopInvocation, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.SelectInvocations, Is.Empty);
            return session;
        }

        private static EffectDefinition LateRefusalAttackEffect()
        {
            var root = new EffectNodeId("late-stop-root");
            var note = new EffectNodeId("late-stop-note");
            var choose = new EffectNodeId("late-stop-choose");
            var first = new EffectNodeId("late-stop-first");
            var second = new EffectNodeId("late-stop-second");
            return new EffectDefinition(new EffectDefinitionId("pending-contract-late-stop-effect"),
                "pending-contract-stop-rev-1", root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { note, choose }),
                    new EffectNodeDefinition(note, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.NoteActorV1,
                        outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) }),
                    new EffectNodeDefinition(choose, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { first, second }, chooser: EffectChooserRole.Actor,
                        minimumSelections: 1, maximumSelections: 1),
                    new EffectNodeDefinition(first, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.ImpossibleV1),
                    new EffectNodeDefinition(second, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.ImpossibleV1)
                });
        }

        private static QuickGameHeroSetupState HeroSetup(HeroId hero, TeamId team, TeamSide side,
            BoardMapDefinition map, HexCoordinate coordinate) => new QuickGameHeroSetupState(hero,
                new HeroContentId("pending-contract-" + side.ToString().ToLowerInvariant()),
                side + " Contract Hero", team, side,
                new ConfiguredSpawnPointReference(map.Id, new SpawnPointId(side + "-spawn")),
                new ConfiguredSpaceReference(map.Id, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        private static MatchSession CreateSelectedMinionBattlePause()
        {
            var session = MinionChoicePresentationTests.CreatePendingRemovalSession();
            Accept(session, session.Projection.DecisionContract.Commands.First(value =>
                value.Kind == DirectorCommandKind.SelectMinionBattleRemoval));
            Assert.That(session.Projection.PendingMinionBattleChoice.DeselectInvocations, Is.Not.Empty);
            Assert.That(session.Projection.PendingMinionBattleChoice.CanConfirm, Is.True);
            return session;
        }

        private static MatchSession CreateLanePushPlacePause()
        {
            var session = LanePushTransitionTests.CreatePausedLanePushPlaceSession();
            Assert.That(session.Projection.PendingSystemTransitionDescription?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LanePushRespawnPlace));
            return session;
        }

        private static void Commit(MatchSession session, CardDefinitionId redDefinition,
            CardDefinitionId blueDefinition)
        {
            var red = HandCard(session, TeamSide.Red, redDefinition);
            var blue = HandCard(session, TeamSide.Blue, blueDefinition);
            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var tie = session.Projection.PendingInitiativeChoice;
            if (tie != null)
                Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide,
                    tie.CandidateCardInstanceIds.Contains(red.Id) ? red.Id : tie.CandidateCardInstanceIds[0]));
        }

        private static CardInstance HandCard(MatchSession session, TeamSide side,
            CardDefinitionId definitionId) => session.Projection.Cards.Single(card =>
                card.State.Location == CardLocation.Hand &&
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.DefinitionReference.DefinitionId.Equals(definitionId));

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(card.DefinitionReference));

        private static void StartAction(MatchSession session, ActionKind kind)
        {
            var offer = session.Projection.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable && ActionOf(session, candidate).Kind == kind);
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
        }

        private static ActionDefinition ActionOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private sealed class PauseCase
        {
            public PauseCase(string name, PendingDecisionFamilyId familyId, string inventoryFamily,
                Func<MatchSession> create, bool verifiesLifecycle, IEnumerable<string> coveredRows)
            {
                Name = name; FamilyId = familyId; InventoryFamily = inventoryFamily; Create = create;
                VerifiesLifecycle = verifiesLifecycle;
                CoveredRows = Array.AsReadOnly(coveredRows.ToArray());
            }
            public string Name { get; }
            public PendingDecisionFamilyId FamilyId { get; }
            public string InventoryFamily { get; }
            public Func<MatchSession> Create { get; }
            public bool VerifiesLifecycle { get; }
            public IReadOnlyList<string> CoveredRows { get; }
        }

        private sealed class ContractObservation
        {
            public ContractObservation(string name, bool isPending, PendingDecisionContract contract,
                IEnumerable<DirectorCommand> oracle, IEnumerable<DirectorCommand> surface,
                IEnumerable<DirectorCommand> acceptedEvidence)
            {
                Name = name; IsPending = isPending; Contract = contract;
                Oracle = Array.AsReadOnly((oracle ?? Enumerable.Empty<DirectorCommand>()).ToArray());
                Surface = Array.AsReadOnly((surface ?? Enumerable.Empty<DirectorCommand>()).ToArray());
                AcceptedEvidence = Array.AsReadOnly((acceptedEvidence ??
                    Enumerable.Empty<DirectorCommand>()).ToArray());
            }
            public string Name { get; }
            public bool IsPending { get; }
            public PendingDecisionContract Contract { get; }
            public IReadOnlyList<DirectorCommand> Oracle { get; }
            public IReadOnlyList<DirectorCommand> Surface { get; }
            public IReadOnlyList<DirectorCommand> AcceptedEvidence { get; }
            public ContractObservation WithSurface(IEnumerable<DirectorCommand> surface) =>
                new ContractObservation(Name, IsPending, Contract, Oracle, surface, AcceptedEvidence);
        }

        private static class ContractAudit
        {
            public static IReadOnlyList<string> Errors(ContractObservation observation)
            {
                var errors = new List<string>();
                if (observation.IsPending && observation.Contract == null)
                {
                    errors.Add(observation.Name + " is missing application contract");
                    return errors;
                }
                if (observation.Contract == null) return errors;
                if (!SameMultiset(observation.Contract.Commands, observation.Oracle))
                    errors.Add(observation.Name + " allowed command set differs from typed oracle");
                if (observation.Surface.Count == 0)
                    errors.Add(observation.Name + " is missing interactive surface");
                else if (!SameMultiset(observation.Contract.Commands, observation.Surface))
                    errors.Add(observation.Name + " surface command multiset differs from contract");
                if (observation.Surface.Distinct().Count() != observation.Surface.Count)
                    errors.Add(observation.Name + " has a duplicate surface command");
                if (observation.AcceptedEvidence.Count == 0)
                    errors.Add(observation.Name + " is missing answer/result evidence");
                else if (!SameMultiset(observation.Contract.Commands, observation.AcceptedEvidence))
                    errors.Add(observation.Name + " answer/result evidence differs from contract");
                return errors;
            }
            private static bool SameMultiset(IReadOnlyCollection<DirectorCommand> left,
                IReadOnlyCollection<DirectorCommand> right) => left.Count == right.Count && left.All(command =>
                    left.Count(candidate => candidate.Equals(command)) ==
                    right.Count(candidate => candidate.Equals(command)));
        }

        private sealed class RecordingSession : IMatchSession
        {
            private readonly List<DirectorCommand> commands = new List<DirectorCommand>();
            public RecordingSession(MatchSessionProjection projection) { Projection = projection; }
            public MatchSessionProjection Projection { get; }
            public IReadOnlyList<DirectorCommand> Commands => commands;
            public MatchCommandResult Execute(DirectorCommand command)
            {
                commands.Add(command);
                return new MatchCommandResult(true, command, MatchSessionRejectionReason.None,
                    string.Empty, default, default, default, default,
                    Array.Empty<RoundResolutionEvent>(), Projection);
            }
            public MatchSessionOperationResult Undo() => throw new NotSupportedException();
            public MatchSessionOperationResult Restart() => throw new NotSupportedException();
            public MatchSessionOperationResult RestoreSnapshot(string payload) =>
                throw new NotSupportedException();
            public string CaptureSnapshot() => throw new NotSupportedException();
        }

        private sealed class CapturingBoardView : IBoardInteractionView
        {
            public void Render(BoardInteractionViewModel model) { }
        }
    }
}
