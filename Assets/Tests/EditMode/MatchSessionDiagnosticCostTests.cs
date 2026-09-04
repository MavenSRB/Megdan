using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1i's measured evidence. Findings `F613-08`, `F613-09`, and `F613-10` each name a
    /// cost or a report that was previously left to inspection; these fixtures assert them.
    /// </summary>
    public sealed class MatchSessionDiagnosticCostTests
    {
        /// <summary>
        /// `F613-08`. The count is the finding's subject, so it is asserted exactly rather than
        /// bounded: an exact assertion fails when the number moves in either direction, and a
        /// silent increase is precisely what went unnoticed until the audit.
        /// </summary>
        [Test]
        public void AcceptedCommand_PerformsTheRecordedNumberOfFullSerializations()
        {
            var session = CreateSession();
            var card = HandCards(session, TeamSide.Red)[0];

            var measured = MeasureSerializations(() =>
                Assert.That(session.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True));

            Assert.That(measured.Authoritative, Is.EqualTo(1),
                "An accepted command must perform exactly one full authoritative serialization. " +
                "Before 6.1i it performed two, and the pre-command capture re-serialized state the " +
                "previous command's projection had already encoded unchanged.");
            Assert.That(measured.Envelope, Is.EqualTo(0),
                "An accepted command must not encode the rule-event envelope at all. That encode " +
                "re-encodes the whole accumulated log, so any per-command occurrence grows with " +
                "session length.");
            Assert.That(measured.EncodedCharacters, Is.EqualTo(0),
                "Since milestone 7.9b the one serialization is a capture, and nothing on the " +
                "accepted path assembles its payload string: the projection encodes it on first " +
                "read, so a reader pays for it and a command does not.");
        }

        /// <summary>
        /// `F613-08`, rejected path, as corrected after 6.1i. The count is one and not two, and it
        /// is one in every build configuration.
        /// </summary>
        /// <remarks>
        /// This assertion replaces a pair that measured the two `MatchDiagnosticsMode` values
        /// separately and recorded zero serializations for `Off`. That zero was the defect written
        /// down as an expectation: the mode it described skipped the comparison, and skipping the
        /// comparison skips the rollback, so the configuration a release player runs captured a
        /// pre-image it could never act on. The comparison is now unconditional and the mode is
        /// gone, so there is one number to assert rather than two.
        /// </remarks>
        [Test]
        public void RejectedCommand_PerformsExactlyOneFullSerialization()
        {
            var session = CreateSession();

            var measured = MeasureSerializations(() =>
                Assert.That(session.Execute(DirectorCommand.StopMovement()).Accepted, Is.False));

            Assert.That(measured.Authoritative, Is.EqualTo(1),
                "The one serialization is the atomicity comparison's own fresh re-read. The " +
                "pre-image costs nothing here because the memo still holds the state the previous " +
                "command's projection encoded. Before 6.1i this path performed six.");
            Assert.That(measured.Envelope, Is.EqualTo(0));
            Assert.That(measured.EncodedCharacters, Is.EqualTo(0),
                "Since milestone 7.9b the comparison itself answers from the captures' bytes, so " +
                "a refusal encodes no payload string either.");
        }

        /// <summary>
        /// `F613-08`, the growth half. Two halves of one session are compared rather than an
        /// absolute figure, because an absolute threshold pins the machine rather than the property
        /// the finding names: that per-command cost stops depending on how long the session is.
        /// </summary>
        /// <remarks>
        /// This measures encoded characters, not managed bytes. The acceptance check asks for a
        /// managed-allocation fixture, and that was attempted first:
        /// `GC.GetTotalAllocatedBytes` does not exist under this Unity profile, and
        /// `GC.GetAllocatedBytesForCurrentThread` compiles but returns a constant, so the delta
        /// measured zero for a batch that certainly allocated. A probe that cannot discriminate is
        /// an inconclusive result rather than a pass.
        ///
        /// The substitution is sound for the encode half of the finding and unsound for the other
        /// half, which is the correction this fixture carries: 6.1i read it as covering both, and
        /// declared `RISK-PERF-001` retired on that reading, while two array copies of the whole
        /// accumulated log survived where encoded characters cannot see them. Those copies are the
        /// subject of `CapturePreImage_CopiesNothingHoweverLongTheSessionIs` below; this test keeps
        /// its original scope.
        /// </remarks>
        [Test]
        public void LongSession_PerCommandSerializedBytesDoNotGrowWithSessionLength()
        {
            var session = CreateSession();
            var commands = BuildHoldOnlyCommandSequence(session, 24);
            Assert.That(commands, Has.Count.GreaterThanOrEqualTo(16),
                "The measurement needs enough accepted commands for two comparable halves.");

            var half = commands.Count / 2;
            var firstHalf = MeasureSerializedWork(session, commands.Take(half));
            var secondHalf = MeasureSerializedWork(session, commands.Skip(half).Take(half));

            Assert.That(firstHalf, Is.GreaterThan(0),
                "The measurement must observe real serialization work, or it proves nothing.");

            // Regression evidence, not a production target. Before 6.1i every command re-encoded the
            // whole accumulated log, so the later half cost strictly more than the earlier one by
            // construction and grew without bound. Since milestone 7.9b what a command writes is a
            // capture - the current state's bytes, plus each completed round's fragment once, when
            // it freezes - so the allowance absorbs current state moving as cards move, which is
            // state size and not session length; re-encoding the growing completed-round history
            // every command is exactly what this line would catch coming back.
            Assert.That(secondHalf, Is.LessThan(firstHalf * 2.0),
                $"Per-command serialized size grew with session length: {firstHalf} bytes for " +
                $"the first {half} commands against {secondHalf} for the next {half}.");
        }

        /// <summary>
        /// `F613-08`, the growth half, at the site the encoded-character measurement above cannot
        /// reach. Every command captures a rollback pre-image, so a pre-image that copies the
        /// accumulated rule-event log costs more with every command in the session.
        /// </summary>
        /// <remarks>
        /// Asserted as identity rather than as a size or a duration: the pre-image must hold the
        /// aggregate's own snapshot instances, which is true only if the capture copied nothing.
        /// A `ToArray()` anywhere on this path fails it immediately, whatever the machine.
        /// </remarks>
        [Test]
        public void CapturePreImage_CopiesNothingHoweverLongTheSessionIs()
        {
            var session = CreateSession();
            var aggregate = session.aggregate;

            AssertPreImageSharesTheAggregatesStreams(aggregate, "at the start of the session");

            foreach (var command in BuildHoldOnlyCommandSequence(session, 16))
            {
                Assert.That(session.Execute(command).Accepted, Is.True);
            }

            AssertPreImageSharesTheAggregatesStreams(aggregate, "after sixteen accepted commands");

            Assert.That(session.Projection.DiagnosticEvents, Is.SameAs(aggregate.EventLogEntries),
                "The projection copied the whole rule-event log on refresh. It is handed a snapshot " +
                "that cannot change, so the defensive copy defends against nothing and costs more " +
                "with every command in the session.");
        }

        private static void AssertPreImageSharesTheAggregatesStreams(MatchAggregate aggregate, string position)
        {
            var preImage = aggregate.CapturePreImage();

            Assert.That(preImage.EventLogEntries, Is.SameAs(aggregate.EventLogEntries),
                $"The pre-image copied the whole rule-event log {position}.");
            Assert.That(preImage.RoundEvents, Is.SameAs(aggregate.RoundEvents),
                $"The pre-image copied the current round's events {position}.");
        }

        /// <summary>
        /// `F613-09`. A defect thrown inside a command path must not be reported as a rule
        /// rejection. This drives a real command through the public surface with an undefined
        /// target identity, which reaches an argument guard deep in the Attack path rather than a
        /// rule refusal - the same class the finding names.
        /// </summary>
        [Test]
        public void ProgrammingDefect_IsReportedAsEngineFaultRatherThanDomainRejected()
        {
            var session = CreateAdjacentSession();
            StartTiedAttackTurn(session);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                ActionKindOf(session.Projection, offer) == ActionKind.Attack);
            Assert.That(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)).Accepted, Is.True);
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "The scenario must actually reach the Attack target choice it exists to fault.");
            var before = session.Projection;

            var result = session.Execute(DirectorCommand.ChooseAttackTarget(default(HeroId)));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.EngineFault),
                "A programming defect must be distinguishable from a rule rejection. Before 6.1i " +
                "every exception in a command path was reported as DomainRejected.");
            Assert.That(result.Detail, Does.Contain(nameof(ArgumentException)),
                "The detail must name the defect's type, so the log distinguishes them too and not " +
                "only the caller.");
            Assert.That(session.Projection.AuthoritativeSnapshot, Is.EqualTo(before.AuthoritativeSnapshot),
                "Reporting the defect honestly must not cost the rollback that 6.1f established.");
        }

        /// <summary>
        /// `F613-10`. Loading a snapshot permanently disables command history. The Undo control did
        /// grey out, so the original no-signal claim was partly refuted, but the state was
        /// indistinguishable from having nothing to undo - which is what this pins apart.
        /// </summary>
        [Test]
        public void SnapshotLoad_MakesHistoryUnavailabilityDistinguishableFromNothingToUndo()
        {
            var fresh = CreateSession();
            Assert.That(fresh.Projection.AcceptedCommands, Is.Empty);
            Assert.That(fresh.Projection.IsHistoryAvailable, Is.True,
                "A fresh session has nothing to undo, but its history is available.");

            var source = CreateSession();
            var card = HandCards(source, TeamSide.Red)[0];
            Assert.That(source.Execute(DirectorCommand.CommitCard(card.OwnerId, card.Id)).Accepted, Is.True);

            var restored = CreateSession();
            Assert.That(restored.RestoreSnapshot(source.CaptureSnapshot()).Accepted, Is.True);

            Assert.That(restored.Projection.AcceptedCommands, Is.Empty);
            Assert.That(restored.Projection.IsHistoryAvailable, Is.False,
                "After loading a snapshot the session can never record history again, and the " +
                "projection must say so rather than look identical to a fresh session.");

            // The distinction has to survive as far as the control the player actually sees.
            var freshModel = RenderUndoRestart(fresh);
            var restoredModel = RenderUndoRestart(restored);
            Assert.That(freshModel.CanUndo, Is.False);
            Assert.That(restoredModel.CanUndo, Is.False);
            Assert.That(freshModel.IsHistoryAvailable, Is.True);
            Assert.That(restoredModel.IsHistoryAvailable, Is.False,
                "Both states disable the control; only one of them means the history is gone.");
        }

        private static UndoRestartViewModel RenderUndoRestart(MatchSession session)
        {
            var view = new CapturingUndoRestartView();
            new UndoRestartPresenter(session, view).Refresh();
            Assert.That(view.Model, Is.Not.Null);
            return view.Model;
        }

        private static SerializationCount MeasureSerializations(Action act)
        {
            AuthoritativeSnapshotSerializer.ResetSerializeCallCount();
            RuleEventLogSnapshotEnvelope.ResetSerializeCallCount();
            act();
            return new SerializationCount(
                AuthoritativeSnapshotSerializer.SerializeCallCount,
                RuleEventLogSnapshotEnvelope.SerializeCallCount,
                AuthoritativeSnapshotSerializer.SerializedCharacterCount +
                RuleEventLogSnapshotEnvelope.SerializedCharacterCount);
        }

        /// <summary>
        /// Everything the batch serialized or encoded: the bytes captures wrote, plus any payload or
        /// envelope characters anything materialized. On the accepted path the encoded terms are
        /// zero, and including them is what keeps this measurement honest if that changes.
        /// </summary>
        private static long MeasureSerializedWork(
            MatchSession session,
            IEnumerable<DirectorCommand> commands)
        {
            var batch = commands.ToArray();
            AuthoritativeSnapshotSerializer.ResetSerializeCallCount();
            RuleEventLogSnapshotEnvelope.ResetSerializeCallCount();
            foreach (var command in batch)
            {
                Assert.That(session.Execute(command).Accepted, Is.True);
            }

            return AuthoritativeSnapshotSerializer.CapturedByteCount +
                AuthoritativeSnapshotSerializer.SerializedCharacterCount +
                RuleEventLogSnapshotEnvelope.SerializedCharacterCount;
        }

        /// <summary>
        /// Builds a command prefix without executing it against the measured session, by driving a
        /// throwaway twin. Measuring a session that the builder already advanced would measure the
        /// wrong commands.
        /// </summary>
        private static IReadOnlyList<DirectorCommand> BuildHoldOnlyCommandSequence(
            MatchSession measured,
            int minimumCommands)
        {
            var scratch = CreateSession();
            var commands = new List<DirectorCommand>();
            while (commands.Count < minimumCommands)
            {
                var red = EligibleHandCards(scratch, TeamSide.Red);
                var blue = EligibleHandCards(scratch, TeamSide.Blue);
                if (red.Count == 0 || blue.Count == 0)
                {
                    break;
                }

                Record(scratch, commands, DirectorCommand.CommitCard(red[0].OwnerId, red[0].Id));
                Record(scratch, commands, DirectorCommand.CommitCard(blue[0].OwnerId, blue[0].Id));
                var tie = scratch.Projection.PendingInitiativeChoice;
                if (tie != null)
                {
                    Record(scratch, commands, DirectorCommand.ChooseTiedActor(
                        tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                }

                while (scratch.Projection.Phase == RoundResolutionPhase.ResolvingTurn)
                {
                    // Milestone 8.35g. ABOVE the selection read below, which the acknowledgement
                    // behind the pause has already emptied. The release is a recorded command like
                    // any other, so it belongs in this measured stream rather than beside it - what
                    // this fixture measures is per-command cost, and a command left out of the
                    // stream would be a command left out of the measurement.
                    if (scratch.Projection.PendingTurnRelease != null)
                    {
                        Record(scratch, commands, DirectorCommand.ReleaseTurn());
                        continue;
                    }

                    Record(scratch, commands, DirectorCommand.PerformHold(
                        scratch.Projection.SelectedActor.CardInstanceId));
                }

                while (scratch.Projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Record(scratch, commands, DirectorCommand.AdvanceEndOfRound());
                }
            }

            Assert.That(measured.Projection.AcceptedCommands, Is.Empty,
                "The measured session must still be at its initial state.");
            return commands;
        }

        private static void Record(
            MatchSession scratch,
            List<DirectorCommand> commands,
            DirectorCommand command)
        {
            Assert.That(scratch.Execute(command).Accepted, Is.True);
            commands.Add(command);
        }

        private static ActionKind ActionKindOf(MatchSessionProjection projection, ActionOffer offer)
        {
            return projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;
        }

        private static void StartTiedAttackTurn(MatchSession session)
        {
            var red = HandCards(session, TeamSide.Red)
                .Where(card => DefinitionOf(session, card).ActionDefinitions.Any(action =>
                    action.Kind == ActionKind.Attack))
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
                .First();
            // Blue's lowest Initiative, so the Red Attack card is the acting one whatever the showcase
            // conversions have done to the top of either band; Red's highest remaining Attack is Red
            // Lancer at 9 since milestone 6.5a converted Red Needle.
            var blue = HandCards(session, TeamSide.Blue)
                .OrderBy(card => DefinitionOf(session, card).Initiative.Value)
                .First();

            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);

            var choice = session.Projection.PendingInitiativeChoice;
            if (choice != null)
            {
                Assert.That(session.Execute(DirectorCommand.ChooseTiedActor(choice.OwningSide, red.Id)).Accepted, Is.True);
            }

            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card)
        {
            return session.Projection.Definitions.Single(value =>
                value.Reference.Equals(card.DefinitionReference));
        }

        private static IReadOnlyList<CardInstance> HandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static IReadOnlyList<CardInstance> EligibleHandCards(MatchSession session, TeamSide side)
        {
            return session.Projection.Cards
                .Where(card => card.State.IsInitialInHand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();
        }

        private static MatchSession CreateSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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

        /// <summary>
        /// Two heroes adjacent on a two-space synthetic board, so an Attack has a legal target.
        /// Mirrors the adjacent fixture in `CardAggregateInvarianceTests`; the project keeps session
        /// builders fixture-local, and consolidating them is 6.1k's subject.
        /// </summary>
        private static MatchSession CreateAdjacentSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("diagnostic-cost-adjacent"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(1, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("red-diagnostic"),
                        "Red Diagnostic",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q0_r0")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("blue-diagnostic"),
                        "Blue Diagnostic",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                        blueCoordinate)
                },
                Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private sealed class SerializationCount
        {
            public SerializationCount(long authoritative, long envelope, long encodedCharacters)
            {
                Authoritative = authoritative;
                Envelope = envelope;
                EncodedCharacters = encodedCharacters;
            }

            public long Authoritative { get; }
            public long Envelope { get; }
            public long EncodedCharacters { get; }
        }

        private sealed class CapturingUndoRestartView : IUndoRestartView
        {
            public UndoRestartViewModel Model { get; private set; }

            public void Render(UndoRestartViewModel model)
            {
                Model = model;
            }
        }
    }
}
