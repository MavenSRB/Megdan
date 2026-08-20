using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Rules.Transitions.Synthetic;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.10a's reusable system-transition continuation contract.
    ///
    /// A multi-step authoritative transition has to be able to stop in the middle of itself, on a
    /// player, and resume exactly where it stopped - across a snapshot, a replay and a retained-prefix
    /// Undo. The thing that makes that possible is that the continuation is authority-owned serializable
    /// state rather than a callback a handler is holding, because a callback survives none of those
    /// three.
    ///
    /// Everything here runs against the bounded synthetic transition this build registers, which is the
    /// contract's declared first consumer. Milestone 6.11's Lane-Push respawn and milestone 7.5's
    /// progression are the named production consumers and are deliberately absent.
    ///
    /// The closing check for this milestone is 6.2a-style acceptance evidence rather than a
    /// failing-then-passing observation, because the contract did not exist to fail: what is shown is
    /// the paused transition driven through restore, replay and Undo, and the conformance rule measured
    /// against a violator and a compliant twin.
    ///
    /// Unity-independent.
    /// </summary>
    public sealed class SystemTransitionContinuationTests
    {
        // -------------------------------------------------------------------------------------------
        // What the continuation records
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APausedStep_RecordsOwnerStepChooserContextAndResumeLocation()
        {
            var session = CreateSession();

            Begin(session);
            var pending = session.Projection.PendingSystemTransitionStep;

            Assert.That(pending, Is.Not.Null, "The transition stops on its first step that pauses.");
            Assert.That(pending.DefinitionId, Is.EqualTo(SyntheticSystemTransitions.BoundedFourStepId),
                "The owning transition names the definition it is running.");
            Assert.That(pending.InstanceId.IsDefined, Is.True,
                "And the run, so an answer meant for a previous run of the same transition cannot land.");
            Assert.That(pending.StepId, Is.EqualTo(SyntheticSystemTransitions.RedChoiceStepId),
                "The stable current step, which is a declared identity rather than a position.");
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Red));
            Assert.That(pending.Options, Is.EqualTo(new[]
            {
                SyntheticSystemTransitions.FirstOptionId, SyntheticSystemTransitions.SecondOptionId
            }), "The legal choice context is the options themselves, in the definition's order.");
            Assert.That(
                pending.CompletedStepIds,
                Is.EqualTo(new[] { SyntheticSystemTransitions.OpeningStepId }),
                "The exact resume location, stated as the steps that have actually happened: the " +
                "opening step resolved itself on the way here, and nobody answered it.");
            Assert.That(
                pending.Invocations.Select(invocation => invocation.OptionId),
                Is.EqualTo(pending.Options),
                "Each option carries the exact command that answers it, so a caller composes nothing.");
        }

        [Test]
        public void TheContinuationHoldsNoBehaviour()
        {
            // The property the whole contract rests on, asserted rather than assumed: a continuation
            // that held a delegate could not be serialized, replayed, or truncated by an Undo, and the
            // three checks below would each be testing something that only appears to work in-process.
            var delegateMembers = typeof(PendingSystemTransitionStep)
                .GetProperties()
                .Where(property => typeof(Delegate).IsAssignableFrom(property.PropertyType))
                .Select(property => property.Name)
                .Concat(typeof(PendingSystemTransitionStep)
                    .GetFields(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.NonPublic |
                        System.Reflection.BindingFlags.Public)
                    .Where(field => typeof(Delegate).IsAssignableFrom(field.FieldType))
                    .Select(field => field.Name))
                .ToArray();

            Assert.That(delegateMembers, Is.Empty,
                "A continuation is a value the authority holds, never a callback a handler is holding.");
        }

        // -------------------------------------------------------------------------------------------
        // Accepted, rejected and stale resumptions
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnAcceptedAnswer_AdvancesToTheNextPausingStep()
        {
            var session = CreateSession();
            Begin(session);

            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            var pending = session.Projection.PendingSystemTransitionStep;

            Assert.That(pending.StepId, Is.EqualTo(SyntheticSystemTransitions.BlueChoiceStepId));
            Assert.That(pending.ChoosingSide, Is.EqualTo(TeamSide.Blue),
                "The chooser is recorded per step rather than fixed for the transition.");
            Assert.That(pending.CompletedStepIds, Is.EqualTo(new[]
            {
                SyntheticSystemTransitions.OpeningStepId, SyntheticSystemTransitions.RedChoiceStepId
            }), "The answered step joins the completed record, in the order it happened.");
        }

        [Test]
        public void TheLastAnswer_FinishesTheTransitionWithoutAFurtherCommand()
        {
            var session = CreateSession();
            Begin(session);
            Answer(session, SyntheticSystemTransitions.FirstOptionId);

            Answer(session, SyntheticSystemTransitions.SecondOptionId);

            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null,
                "The closing step resolves itself, so the transition ends on the last answer rather " +
                "than needing one more command to notice it is done.");
        }

        [Test]
        public void AnIllegalOption_IsRefusedAndLeavesTheContinuationStanding()
        {
            var session = CreateSession();
            Begin(session);
            var before = session.CaptureSnapshot();
            var pending = session.Projection.PendingSystemTransitionStep;

            var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    pending.InstanceId,
                    pending.StepId,
                    pending.ItemId,
                    pending.ChoosingSide,
                    new SystemTransitionOptionId("not-offered"))));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice));
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before),
                "A refused answer is indistinguishable from one that never arrived.");
        }

        [Test]
        public void TheWrongChooser_IsRefused()
        {
            var session = CreateSession();
            Begin(session);
            var pending = session.Projection.PendingSystemTransitionStep;

            var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    pending.InstanceId,
                    pending.StepId,
                    pending.ItemId,
                    TeamSide.Blue,
                    SyntheticSystemTransitions.FirstOptionId)));

            Assert.That(result.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice),
                "Red's step is Red's, and the continuation records whose it is precisely so this can " +
                "be refused rather than resolved by whoever asks first.");
            Assert.That(
                session.Projection.PendingSystemTransitionStep.ChoosingSide, Is.EqualTo(TeamSide.Red));
        }

        [Test]
        public void AStaleAnswer_CannotRepeatOrRestartACompletedStep()
        {
            var session = CreateSession();
            Begin(session);
            var completed = session.Projection.PendingSystemTransitionStep;
            Answer(session, SyntheticSystemTransitions.FirstOptionId);

            var repeated = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    completed.InstanceId,
                    completed.StepId,
                    completed.ItemId,
                    completed.ChoosingSide,
                    SyntheticSystemTransitions.SecondOptionId)));

            Assert.That(repeated.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice),
                "The step is completed, and a completed step cannot be answered again - which is what " +
                "keeps a resend, a double click, or a replayed stale command from re-running it.");
            Assert.That(
                session.Projection.PendingSystemTransitionStep.StepId,
                Is.EqualTo(SyntheticSystemTransitions.BlueChoiceStepId),
                "And the transition is exactly where it was.");
        }

        [Test]
        public void AnAnswerCannotSkipAhead()
        {
            var session = CreateSession();
            Begin(session);
            var pending = session.Projection.PendingSystemTransitionStep;

            var skipped = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    pending.InstanceId,
                    SyntheticSystemTransitions.BlueChoiceStepId,
                    pending.ItemId,
                    TeamSide.Blue,
                    SyntheticSystemTransitions.FirstOptionId)));

            Assert.That(skipped.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice),
                "A step the transition has not reached is as much 'not the paused step' as one it has " +
                "already passed, which is why one refusal covers both directions.");
            Assert.That(
                session.Projection.PendingSystemTransitionStep.StepId,
                Is.EqualTo(SyntheticSystemTransitions.RedChoiceStepId));
        }

        [Test]
        public void AnAnswerNamingAnotherRun_IsRefused()
        {
            var session = CreateSession();
            Begin(session);
            var pending = session.Projection.PendingSystemTransitionStep;

            var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    new SystemTransitionInstanceId("some-other-run"),
                    pending.StepId,
                    pending.ItemId,
                    pending.ChoosingSide,
                    SyntheticSystemTransitions.FirstOptionId)));

            Assert.That(result.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice));
        }

        [Test]
        public void AnsweringWithNothingPaused_IsRefused()
        {
            var session = CreateSession();

            var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    new SystemTransitionInstanceId("nothing-running"),
                    SyntheticSystemTransitions.RedChoiceStepId,
                    SystemTransitionItemId.None,
                    TeamSide.Red,
                    SyntheticSystemTransitions.FirstOptionId)));

            Assert.That(result.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.NoPendingSystemTransitionStep));
        }

        [Test]
        public void BeginningTwiceOrBeginningNothing_AreBothRefused()
        {
            var running = CreateSession();
            Begin(running);

            var again = running.Execute(
                DirectorCommand.BeginSystemTransition(SyntheticSystemTransitions.BoundedFourStepId));

            Assert.That(again.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.SystemTransitionAlreadyRunning),
                "Two transitions at once would be two resume locations.");
            Assert.That(
                running.Projection.PendingSystemTransitionStep.StepId,
                Is.EqualTo(SyntheticSystemTransitions.RedChoiceStepId),
                "And the one already running is untouched.");

            // A fresh session, because the already-running refusal comes first and would mask this
            // one: a match that is mid-transition is refused whatever name the second begin carries.
            var idle = CreateSession();
            var unknown = idle.Execute(DirectorCommand.BeginSystemTransition(
                new SystemTransitionDefinitionId("no-such-transition")));

            Assert.That(unknown.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.UnknownSystemTransition));
            Assert.That(idle.Projection.PendingSystemTransitionStep, Is.Null);
        }

        [Test]
        public void BeginningAProductionTransitionByCommand_IsRefused()
        {
            // Milestone 7.9c, finding F-PROG-01. The synthetic transition above exists to be begun by a
            // command, because a bounded fixture has no rules of its own to begin it. The two production
            // transitions are the opposite: p.8's push begins from a request an action recorded, and
            // p.15's leveling from the end-of-round stage that owns its bookkeeping. Beginning either by
            // command ran a rulebook step outside the sequence that accounts for it - a level bought
            // where `RoundLeveling` never hears about it, so p.8's Pity Coin is then paid to the hero
            // that just levelled and withheld from it a round later.
            foreach (var definitionId in new[]
            {
                HeroLevelUpSystemTransition.DefinitionId,
                LanePushSystemTransition.DefinitionId
            })
            {
                var session = CreateSession();
                var before = session.Projection.HeroProgression
                    .Select(hero => (hero.HeroId, hero.Level, hero.Coins))
                    .ToArray();

                var result = session.Execute(DirectorCommand.BeginSystemTransition(definitionId));

                Assert.That(result.Accepted, Is.False,
                    $"'{definitionId.Value}' is begun by its own rules, so a command may not begin it.");
                Assert.That(result.RoundRejectionReason,
                    Is.EqualTo(RoundResolutionRejectionReason.SystemTransitionNotBegunByCommand),
                    "And the refusal says why rather than claiming the build has no such transition: " +
                    "it has one, and that is the point.");
                Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null,
                    "Nothing is paused, so no resume location was created.");
                Assert.That(
                    session.Projection.HeroProgression
                        .Select(hero => (hero.HeroId, hero.Level, hero.Coins))
                        .ToArray(),
                    Is.EqualTo(before),
                    "And no hero's Level or coin balance moved - the refusal that matters, because " +
                    "the accepted command spent coins and raised Levels outside p.8's step.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Crossing the snapshot, replay and Undo boundaries
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APausedTransition_SurvivesSnapshotAndRestoreExactly()
        {
            var session = CreateSession();
            Begin(session);
            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            var payload = session.CaptureSnapshot();
            var expected = session.Projection.PendingSystemTransitionStep;

            var restored = CreateSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);

            var actual = restored.Projection.PendingSystemTransitionStep;
            Assert.That(actual, Is.Not.Null);
            Assert.That(actual.InstanceId, Is.EqualTo(expected.InstanceId));
            Assert.That(actual.StepId, Is.EqualTo(expected.StepId));
            Assert.That(actual.ChoosingSide, Is.EqualTo(expected.ChoosingSide));
            Assert.That(actual.Options, Is.EqualTo(expected.Options));
            Assert.That(actual.CompletedStepIds, Is.EqualTo(expected.CompletedStepIds));
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "And it round-trips byte-identically, so the continuation is genuinely on the wire " +
                "rather than rebuilt from something that happens to agree.");
        }

        [Test]
        public void ARestoredTransition_ResumesFromWhereItStopped()
        {
            var session = CreateSession();
            Begin(session);
            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            var restored = CreateSession();
            Assert.That(restored.RestoreSnapshot(session.CaptureSnapshot()).Accepted, Is.True);

            var pending = restored.Projection.PendingSystemTransitionStep;
            var result = restored.Execute(DirectorCommand.AnswerSystemTransitionStep(
                pending.Invocations[0]));

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.Projection.PendingSystemTransitionStep, Is.Null,
                "The restored authority finishes the transition from the step it was stopped on, " +
                "which is the whole claim: resume exactly where it stopped.");
        }

        [Test]
        public void ARestoredTransition_StillRefusesACompletedStep()
        {
            var session = CreateSession();
            Begin(session);
            var firstStep = session.Projection.PendingSystemTransitionStep;
            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            var restored = CreateSession();
            Assert.That(restored.RestoreSnapshot(session.CaptureSnapshot()).Accepted, Is.True);

            var stale = restored.Execute(DirectorCommand.AnswerSystemTransitionStep(
                new SystemTransitionChoiceCommand(
                    firstStep.InstanceId,
                    firstStep.StepId,
                    firstStep.ItemId,
                    firstStep.ChoosingSide,
                    SyntheticSystemTransitions.FirstOptionId)));

            Assert.That(stale.RoundRejectionReason,
                Is.EqualTo(RoundResolutionRejectionReason.IllegalSystemTransitionChoice),
                "A restore carries the completed steps with it, so a step that was answered before the " +
                "snapshot cannot be answered again after it.");
        }

        [Test]
        public void AReplayedTransition_ReconstructsEquivalently()
        {
            var session = CreateSession();
            Begin(session);
            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            var expected = session.CaptureSnapshot();

            // Undo removes the last command and replays the retained prefix, so the state that comes
            // back is rebuilt from the recorded commands rather than copied - and doing it twice, out
            // and back, is a replay of the whole prefix through the same typed path.
            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(
                session.Projection.PendingSystemTransitionStep.StepId,
                Is.EqualTo(SyntheticSystemTransitions.RedChoiceStepId),
                "The undone answer is genuinely undone: the transition is back on the step that was " +
                "answered, not merely holding an equal-looking continuation.");
            Assert.That(
                session.Projection.PendingSystemTransitionStep.CompletedStepIds,
                Is.EqualTo(new[] { SyntheticSystemTransitions.OpeningStepId }),
                "And the completed record shrank with it, which is what stops an Undo leaving a step " +
                "marked done that the match no longer says happened.");

            Answer(session, SyntheticSystemTransitions.FirstOptionId);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(expected),
                "Replaying the same answer reproduces the same authoritative state byte for byte.");
        }

        [Test]
        public void AnUndoneBegin_LeavesNoTransitionRunning()
        {
            var session = CreateSession();
            Begin(session);

            Assert.That(session.Undo().Accepted, Is.True);

            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null,
                "A retained prefix that stops before the begin rebuilds a match with no transition, " +
                "rather than one holding a continuation no command in the history produced.");
        }

        [Test]
        public void Restart_LeavesNoTransitionRunning()
        {
            var session = CreateSession();
            Begin(session);
            Answer(session, SyntheticSystemTransitions.FirstOptionId);

            Assert.That(session.Restart().Accepted, Is.True);

            Assert.That(session.Projection.PendingSystemTransitionStep, Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Refusals on the way back in
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APayloadNamingATransitionThisBuildLacks_IsRefused()
        {
            var session = CreateSession();
            Begin(session);
            var payload = AuthoritativePayload(session);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    payload,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    CreateSetup(session.Projection.SelectedMap).Minions,
                    null,
                    null,
                    SystemTransitionCatalog.Empty));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload),
                "What the steps mean stays in the catalog, so a payload naming a transition this " +
                "build no longer supplies is refused rather than reinterpreted.");
        }

        [Test]
        public void APayloadWhoseCompletedStepsContradictTheDefinition_IsRefused()
        {
            var session = CreateSession();
            Begin(session);
            var payload = AuthoritativePayload(session);
            // The opening step is the one completed step this continuation should carry; renaming it to
            // the closing step describes a run that finished before it started. The rename happens in
            // the decoded bytes rather than in the base64 text, and the two identities are the same
            // length, so the surrounding stream is untouched.
            var tampered = RewriteDecodedText(
                payload,
                SyntheticSystemTransitions.OpeningStepId.Value,
                SyntheticSystemTransitions.ClosingStepId.Value);

            Assert.That(tampered, Is.Not.EqualTo(payload), "The tamper has to actually change something.");
            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    tampered,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    CreateSetup(session.Projection.SelectedMap).Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.MalformedPayload),
                "The completed steps must reproduce the definition's own order before the paused step; " +
                "a payload that disagrees describes a transition this build cannot run.");
        }

        [Test]
        public void ThePriorSchema_IsRejectedRatherThanMigrated()
        {
            var session = CreateSession();
            Begin(session);
            var prior = RewriteSchemaVersion(
                AuthoritativePayload(session),
                AuthoritativeSnapshotSerializer.CurrentSchemaVersion - 1);

            var exception = Assert.Throws<SnapshotLoadException>(() =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    prior,
                    session.Projection.Definitions,
                    new[] { session.Projection.SelectedMap },
                    CreateSetup(session.Projection.SelectedMap).Minions));

            Assert.That(exception.Reason, Is.EqualTo(SnapshotLoadRejectionReason.UnsupportedSchemaVersion),
                "A v14 payload has no field for a paused transition, and reading one as 'nothing is " +
                "running' would restore a match that had silently skipped a step a player still owes.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        private static void Begin(MatchSession session)
        {
            var result = session.Execute(
                DirectorCommand.BeginSystemTransition(SyntheticSystemTransitions.BoundedFourStepId));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static void Answer(MatchSession session, SystemTransitionOptionId optionId)
        {
            var pending = session.Projection.PendingSystemTransitionStep;
            Assert.That(pending, Is.Not.Null, "Nothing is paused to answer.");
            var invocation = pending.Invocations.Single(value => value.OptionId.Equals(optionId));
            var result = session.Execute(DirectorCommand.AnswerSystemTransitionStep(invocation));
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static string AuthoritativePayload(MatchSession session)
        {
            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(
                session.CaptureSnapshot(), out var envelope, out _), Is.True);
            return envelope.AuthoritativePayload;
        }

        /// <summary>
        /// Replaces one equal-length ASCII run in the decoded payload. Equal length on purpose: a
        /// length-prefixed string whose prefix no longer matches its content would be refused as
        /// malformed for a reason that has nothing to do with what this fixture is asserting.
        /// </summary>
        private static string RewriteDecodedText(string payload, string existing, string replacement)
        {
            Assert.That(replacement.Length, Is.EqualTo(existing.Length));
            var bytes = Convert.FromBase64String(payload);
            var needle = System.Text.Encoding.UTF8.GetBytes(existing);
            var written = System.Text.Encoding.UTF8.GetBytes(replacement);

            for (var index = 0; index + needle.Length <= bytes.Length; index++)
            {
                var matched = true;
                for (var offset = 0; offset < needle.Length && matched; offset++)
                {
                    matched = bytes[index + offset] == needle[offset];
                }

                if (!matched) continue;
                written.CopyTo(bytes, index);
                return Convert.ToBase64String(bytes);
            }

            Assert.Fail($"'{existing}' is not in this payload.");
            return payload;
        }

        private static string RewriteSchemaVersion(string payload, int version)
        {
            var bytes = Convert.FromBase64String(payload);
            var versionAt = 1 + System.Text.Encoding.UTF8.GetByteCount("GUARDS-AUTHORITATIVE-SNAPSHOT");
            BitConverter.GetBytes(version).CopyTo(bytes, versionAt);
            return Convert.ToBase64String(bytes);
        }

        private static QuickGameSetupState CreateSetup(BoardMapDefinition map) =>
            QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                PlaceholderOneVsOneParticipants.Create(),
                map,
                new TieBreakerState(TeamSide.Red));

        private static MatchSession CreateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
