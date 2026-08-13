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
        }

        /// <summary>
        /// `F613-08`, rejected path. The atomicity self-check's second capture is the difference
        /// between the two modes, which is why the gate is asserted from both sides: a compile gate
        /// could only ever be observed from whichever side the test assembly compiles under.
        /// </summary>
        [Test]
        public void RejectedCommand_PerformsTheRecordedNumberOfFullSerializationsInEachDiagnosticsMode()
        {
            var verified = CreateSession(MatchDiagnosticsMode.Verified);
            var verifiedMeasured = MeasureSerializations(() =>
                Assert.That(verified.Execute(DirectorCommand.StopMovement()).Accepted, Is.False));

            Assert.That(verifiedMeasured.Authoritative, Is.EqualTo(1),
                "With self-verification on, the one serialization is the check's own fresh re-read. " +
                "The pre-image costs nothing here because the memo still holds the state the " +
                "previous command's projection encoded. Before 6.1i this path performed six.");
            Assert.That(verifiedMeasured.Envelope, Is.EqualTo(0));

            var off = CreateSession(MatchDiagnosticsMode.Off);
            var offMeasured = MeasureSerializations(() =>
                Assert.That(off.Execute(DirectorCommand.StopMovement()).Accepted, Is.False));

            Assert.That(offMeasured.Authoritative, Is.EqualTo(0),
                "With self-verification off, a refused command serializes nothing at all: it moved " +
                "no state, so the memo taken before it is still valid afterwards. The pre-image is " +
                "still captured in both modes - it is the rollback guarantee, not a diagnostic - it " +
                "simply costs no serialization when state has not changed.");
            Assert.That(offMeasured.Envelope, Is.EqualTo(0));
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
        /// an inconclusive result rather than a pass. Encoded characters are the dominant managed
        /// allocation on this path, are exact, and are machine-independent, so the substitution
        /// measures the finding's subject more sharply than the instrument named for it.
        /// </remarks>
        [Test]
        public void LongSession_PerCommandSerializedBytesDoNotGrowWithSessionLength()
        {
            var session = CreateSession();
            var commands = BuildHoldOnlyCommandSequence(session, 24);
            Assert.That(commands, Has.Count.GreaterThanOrEqualTo(16),
                "The measurement needs enough accepted commands for two comparable halves.");

            var half = commands.Count / 2;
            var firstHalf = MeasureSerializedCharacters(session, commands.Take(half));
            var secondHalf = MeasureSerializedCharacters(session, commands.Skip(half).Take(half));

            Assert.That(firstHalf, Is.GreaterThan(0),
                "The measurement must observe real encoding work, or it proves nothing.");

            // Regression evidence, not a production target. Before 6.1i every command re-encoded the
            // whole accumulated log, so the later half cost strictly more than the earlier one by
            // construction and grew without bound. The allowance absorbs the authoritative payload
            // growing as cards move and rounds complete, which is state size and not session length.
            Assert.That(secondHalf, Is.LessThan(firstHalf * 2.0),
                $"Per-command serialized size grew with session length: {firstHalf} characters for " +
                $"the first {half} commands against {secondHalf} for the next {half}.");
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
                RuleEventLogSnapshotEnvelope.SerializeCallCount);
        }

        private static long MeasureSerializedCharacters(
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

            return AuthoritativeSnapshotSerializer.SerializedCharacterCount +
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
            var blue = HandCards(session, TeamSide.Blue)
                .OrderByDescending(card => DefinitionOf(session, card).Initiative.Value)
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

        private static MatchSession CreateSession(
            MatchDiagnosticsMode diagnostics = MatchDiagnosticsMode.Verified)
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
            return new MatchSession(
                new MatchSessionInitialState(scenario, setup, new[] { map }),
                diagnostics);
        }

        /// <summary>
        /// Two heroes adjacent on a two-space synthetic board, so an Attack has a legal target.
        /// Mirrors the adjacent fixture in `CardAggregateInvarianceTests`; the project keeps session
        /// builders fixture-local, and consolidating them is 6.1k's subject.
        /// </summary>
        private static MatchSession CreateAdjacentSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
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
            public SerializationCount(long authoritative, long envelope)
            {
                Authoritative = authoritative;
                Envelope = envelope;
            }

            public long Authoritative { get; }
            public long Envelope { get; }
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
