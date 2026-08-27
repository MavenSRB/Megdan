using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.25a. Rulebook p.10's repeat, reached from outside the action it repeats, stays
    /// inside the turn that owns it.
    ///
    /// <para><b>What was wrong.</b> Milestone 8.23 made Tigerclaw's Cloak and Daggers - "After you
    /// perform a basic action you may repeat it once" - open its decision at the p.8 ActionEnded
    /// checkpoint. The action that reaches that checkpoint has finished, so the turn authority
    /// resolved its card and acknowledged Initiative before the decision was answered, and withheld
    /// only the advance. The answer then settled an *owed* repeat that nothing started: the branch
    /// that finishes a trigger never asked for one. The owed instance survived the command that
    /// settled it - through the rest of the turn, the end of the round, and into the next - and was
    /// finally started by whichever action happened to complete next, by which time Initiative had
    /// selected a different hero, or the same hero on a different card. Every command the projection
    /// then offered for it was refused, because the Movement answer and its Skill equivalent both
    /// require the pending resolution to be the selected actor's. A live match no player could
    /// answer.</para>
    ///
    /// <para><b>What closes it.</b> The action is not over while an ability that may repeat it is
    /// standing, so the turn holds its card and its Initiative acknowledgement across that decision -
    /// exactly as it already held them for an ability that asked nothing and repeated inside the same
    /// command. The answer starts the second instance there and then, with the actor still selected
    /// and the card still unresolved, which is the position rulebook p.10 describes: each instance of
    /// a repeat is a separate action performed on the same card by the same hero.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class AfterActionRepeatContinuityTests
    {
        /// <summary>
        /// Large enough to reach the demonstrated refusals, which stand at commands 1047 and 2675, and
        /// to let the runs that pass them play on to an ending or to the budget.
        /// </summary>
        private const int SweepBudget = 4000;

        /// <summary>The command the staged run's refusal stood at, before this milestone.</summary>
        private const int StagedRefusalCommand = 1047;

        /// <summary>
        /// The seeds of the sweep, which are the first six rather than a chosen set: the property
        /// below is about every run, so choosing which runs to make it about would be choosing the
        /// answer.
        /// </summary>
        /// <remarks>
        /// Where the count comes from is the budget rather than the coverage. Measured at the pre-fix
        /// revision over the first twelve, seeds 1, 3, 5, 6, 9 and 11 of the staged board and 3, 4, 6,
        /// 7, 8, 9 and 12 of the six-player board reached a refusal - thirteen runs of twenty-four -
        /// and the twenty-four cost 199 s in one Unity test case, which is past that tier's 180 s
        /// per-case timeout and about as long again as the whole Edit Mode suite was before it. The
        /// first six of each board keep seven failing runs of twelve at half the price, split into one
        /// case per board so neither approaches the timeout.
        /// </remarks>
        private static readonly int[] SweepSeeds = { 1, 2, 3, 4, 5, 6 };

        /// <summary>
        /// How large a rule-event log the director Undo can still roll a probe answer back through.
        /// RuleEventLogSnapshotEnvelope bounds a captured log at 10,000 entries and Undo replays
        /// through a capture, so a sweep long enough to pass that bound cannot be forked any more.
        /// Milestone 8.25e.
        /// </summary>
        private const int RollbackReadableEventCount = 9000;

        // -----------------------------------------------------------------------------------------
        // The two demonstrated cases
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The case milestone 8.25's independent review reported, as it reported it. At the pre-fix
        /// revision this run reached command 1047, where the projection offered AnswerEffectChoice for
        /// after-action-repeat:blue-tigerclaw-blend-into-shadows:primary-skill - a repeat owed by
        /// Tigerclaw two turns and one round earlier - while Initiative had Wasp's Electroblast
        /// selected, and the rules refused it as TurnCommandRejected (SkillCommandRejected).
        /// </summary>
        [Test]
        public void TheDemonstratedStagedRun_AnswersTheRepeatItUsedToRefuse()
        {
            var record = DeterministicMatchLoop.Play(
                CreateStagedWaspVersusTigerclawSession(), 1, SweepBudget);

            Assert.That(record.Stalled, Is.False,
                "The staged Wasp-versus-Tigerclaw board reached a live match the projection offered " +
                "no legal command for.");
            Assert.That(record.Commands.Count, Is.GreaterThan(StagedRefusalCommand),
                "and it played past the command the refusal stood at rather than stopping short of it.");
        }

        /// <summary>
        /// The same shape on the board a player actually loads, and as a Movement rather than an effect
        /// choice: at the pre-fix revision seed 3 reached command 2675, where the projection offered
        /// ChooseMovementDestination for
        /// after-action-repeat:red-03-tigerclaw-blend-into-shadows:secondary-movement while Initiative
        /// had Brogan selected, and the rules refused it as TurnCommandRejected
        /// (MovementCommandRejected).
        /// </summary>
        [Test]
        public void TheDemonstratedSixPlayerRun_AnswersTheRepeatItUsedToRefuse()
        {
            var record = DeterministicMatchLoop.Play(CreateOfficialSixPlayerSession(), 3, SweepBudget);

            Assert.That(record.Stalled, Is.False,
                "The official six-player board reached a live match the projection offered no legal " +
                "command for.");
        }

        // -----------------------------------------------------------------------------------------
        // The property, over a sweep
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Every command the projection offers for a pending resolution is accepted by the rules, or
        /// the projection does not offer it.
        /// </summary>
        /// <remarks>
        /// The two cases above are the deterministic ones this milestone closes; this is the statement
        /// that they are not the only two. It is deliberately not "one seed does not stall": the defect
        /// was reachable on more than half of the first twelve seeds of both boards and on none of the
        /// rest, so a single run is a coin toss about whether the property holds.
        ///
        /// <para><see cref="Sweep"/> also asserts the structural fact underneath the refusals at every
        /// step, rather than only about the one command each step happens to draw - see its own
        /// remarks. A property that could pass because a seed reached for a different option would be
        /// no property.</para>
        /// </remarks>
        [Test]
        public void EverySweptSeedOfTheStagedBoard_PlaysWithoutStallingOrRefusal()
        {
            var indistinguishable = new List<string>();
            foreach (var seed in SweepSeeds)
            {
                Sweep(
                    CreateStagedWaspVersusTigerclawSession(), seed, "staged Wasp versus Tigerclaw",
                    indistinguishable);
            }

            AssertOnlyTheRecordedOffersHaveOneAnswer(indistinguishable);
        }

        /// <summary>The same property on the board a player loads. See the case above.</summary>
        [Test]
        public void EverySweptSeedOfTheSixPlayerBoard_PlaysWithoutStallingOrRefusal()
        {
            var indistinguishable = new List<string>();
            foreach (var seed in SweepSeeds)
            {
                Sweep(
                    CreateOfficialSixPlayerSession(), seed, "official six-player", indistinguishable);
            }

            AssertOnlyTheRecordedOffersHaveOneAnswer(indistinguishable);
        }

        // -----------------------------------------------------------------------------------------
        // The reason a refused card-action command now carries
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// A refused card-action command reports the reason its backend gave, rather than only which
        /// command was refused.
        /// </summary>
        /// <remarks>
        /// This is not decoration. The stall above was diagnosed by retrying all sixteen destinations
        /// the projection offered, one at a time, to establish that the refusal was about the
        /// resolution rather than about any destination - which the movement backend had already said,
        /// in a value the turn authority discarded on its way out. Every refused card-action command
        /// arrived as <c>MovementCommandRejected</c> or <c>SkillCommandRejected</c> and nothing else,
        /// which names the command and says nothing at all about the rules.
        ///
        /// <para>The refusal used here is a destination the Movement never offered, because that is a
        /// refusal the backend owns entirely: the command names the standing resolution and its own
        /// choice, so the session's identity guard passes it through and what says no is the Movement
        /// session itself, as <see cref="CardActionResolutionRejectionReason.InvalidChoice"/>.</para>
        /// </remarks>
        [Test]
        public void ARefusedCardActionCommand_ReportsTheReasonItsBackendGave()
        {
            var session = CreateStagedWaspVersusTigerclawSession();
            var movement = PlayUntilAMovementChoiceStands(session);

            var refused = session.Execute(DirectorCommand.ChooseMovementDestination(
                movement.ResolutionId,
                movement.ChoiceId,
                new GuardsDigital.Core.Board.HexCoordinate(99, -99)));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.TurnRejectionReason,
                Is.EqualTo(TurnResolutionRejectionReason.MovementCommandRejected),
                "which command the rules refused, as before");
            Assert.That(refused.CardActionRejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.InvalidChoice),
                "and what the backend said about it, which is what used to be dropped here.");
            Assert.That(refused.Detail, Does.Contain("InvalidChoice"),
                "The reported detail carries it too, because that string is what a driver prints when " +
                "an offered command comes back refused: " + refused.Detail);
        }

        // -----------------------------------------------------------------------------------------
        // The corrected sequence under snapshot, replay and Undo
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheCorrectedStagedRun_ReplaysRestoresAndUndoesIntoTheSameState()
        {
            var live = CreateStagedWaspVersusTigerclawSession();
            var record = DeterministicMatchLoop.Play(live, 1, SweepBudget);
            Assert.That(record.Commands.Count, Is.GreaterThan(StagedRefusalCommand),
                "The run has to reach past the command the refusal used to stand at for this to be " +
                "about the corrected sequence.");

            var replayed = CreateStagedWaspVersusTigerclawSession();
            DeterministicMatchLoop.Replay(replayed, record.Commands);
            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot));
            Assert.That(
                replayed.Projection.DiagnosticEvents.Select(entry => entry.DisplayText),
                Is.EqualTo(live.Projection.DiagnosticEvents.Select(entry => entry.DisplayText)),
                "The whole-match rule-event log is a function of the command prefix, repeats and all.");

            // Restored from a snapshot taken one command before the refusal used to stand, so what
            // crosses the wire is the corrected state around it rather than a quiet stretch of match.
            var prefix = CreateStagedWaspVersusTigerclawSession();
            DeterministicMatchLoop.Replay(
                prefix, record.Commands.Take(StagedRefusalCommand - 1).ToArray());
            var midpoint = prefix.CaptureSnapshot();

            var restored = CreateStagedWaspVersusTigerclawSession();
            var restoration = restored.RestoreSnapshot(midpoint);
            Assert.That(restoration.Accepted, Is.True, restoration.Detail);
            DeterministicMatchLoop.Replay(
                restored, record.Commands.Skip(StagedRefusalCommand - 1).ToArray());
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                "A match restored around the corrected repeat continues into the same state, byte for " +
                "byte.");

            var undone = live.Undo();
            Assert.That(undone.Accepted, Is.True, undone.Detail);
            Assert.That(live.Projection.AcceptedCommands,
                Is.EqualTo(record.Commands.Take(record.Commands.Count - 1)));
            var again = live.Execute(record.Commands[record.Commands.Count - 1]);
            Assert.That(again.Accepted, Is.True, again.Detail);
            Assert.That(live.Projection.AuthoritativeSnapshot, Is.EqualTo(record.FinalSnapshot),
                "A retained-prefix Undo followed by the same command is the same state, byte for byte.");
        }

        // -----------------------------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Plays one seed the way <see cref="DeterministicMatchLoop.Play"/> does, and asserts at every
        /// step the two facts the refusals were made of.
        /// </summary>
        /// <remarks>
        /// The first is that driver's own: a command the projection offered is accepted. The second is
        /// structural and is checked whether or not the step draws the command that would fail - a
        /// standing Movement, and a standing repeated instance, belong to the hero Initiative has
        /// selected and to the card it selected them on. That is the exact pair the Movement and Skill
        /// answers require before they will accept anything, so a decision that fails it is a decision
        /// nothing can answer, whichever of its options a seed happens to reach for.
        /// </remarks>
        private static void Sweep(
            MatchSession session, int seed, string board, List<string> indistinguishable)
        {
            var random = new MatchLoopRandom(seed);
            var probed = new HashSet<string>(StringComparer.Ordinal);
            var count = 0;
            while (session.Projection.Phase != RoundResolutionPhase.GameEnded && count < SweepBudget)
            {
                AssertEveryStandingActionDecisionBelongsToTheSelectedActor(
                    session.Projection, board, seed, count);
                AssertBothAnswersToAStandingYesOrNoDoSomethingDifferent(
                    session, board, seed, count, indistinguishable, probed);

                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                Assert.That(candidates.Count, Is.GreaterThan(0),
                    $"The {board} board on seed {seed} reached a live match the projection offered no " +
                    $"legal command for, after {count} commands.");

                var chosen = candidates[random.Next(candidates.Count)];
                var result = session.Execute(chosen);
                Assert.That(result.Accepted, Is.True,
                    $"The {board} board on seed {seed} offered command {count + 1} ({chosen.Kind}) " +
                    $"and the rules refused it: {result.Detail}");
                count++;
            }
        }

        /// <summary>
        /// Plays the staged board with seed 1 until a Movement choice stands, and returns it. The
        /// seed is the demonstrated run's, so this reaches an ordinary Movement early rather than
        /// building a second staging to hold one.
        /// </summary>
        private static PendingMovementChoice PlayUntilAMovementChoiceStands(MatchSession session)
        {
            var random = new MatchLoopRandom(1);
            for (var step = 0; step < SweepBudget; step++)
            {
                var standing = session.Projection.PendingMovementChoice;
                if (standing != null && standing.LegalDestinations.Count > 0)
                {
                    return standing;
                }

                var candidates = DeterministicMatchLoop.EnumerateLegalCommands(session.Projection);
                Assert.That(candidates.Count, Is.GreaterThan(0));
                var result = session.Execute(candidates[random.Next(candidates.Count)]);
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.Fail("No Movement stood inside the budget, so there is nothing to refuse.");
            return null;
        }

        /// <summary>
        /// Milestone 8.25e's own property, and the one shape the family this milestone closes escaped
        /// through: where the projection stands on a yes-or-no, the two answers do not leave the match
        /// in the same state.
        ///
        /// <para><b>Why this and not "an accepted command changes something".</b> Milestone 8.25a's
        /// property above is that every offered command is accepted and no decision stalls, and it is
        /// blind by construction to a command that is accepted and then does nothing: taking rulebook
        /// p.10's repeat after a Hold was accepted, produced no instance at all, and the turn moved on
        /// exactly as a decline would have - so the whole match state still changed, and a check on
        /// "did anything change" would have passed. What separates the two is each other. An offer
        /// whose answers are indistinguishable is not a decision; it is a question with one
        /// answer.</para>
        ///
        /// <para>Both answers are given here, each rolled back by the director Undo that milestone 3.7
        /// built, and the sweep then goes on with whichever one it drew - so the property is checked
        /// over the same runs rather than over runs of its own. It is stated once, for every
        /// confirm-or-decline the projection can publish, rather than once per action kind: the defect
        /// was one predicate answering for four arms, and a case per arm would have been four chances
        /// to miss the fifth.</para>
        ///
        /// <para><b>It is written as an allowed set rather than as a flat assertion because it
        /// immediately found a second one, in a different mechanism.</b> See
        /// <see cref="IndistinguishableAnswersThisMilestoneDoesNotOwn"/>.</para>
        /// </summary>
        private static void AssertBothAnswersToAStandingYesOrNoDoSomethingDifferent(
            MatchSession session, string board, int seed, int commandCount,
            List<string> indistinguishable, HashSet<string> probed)
        {
            var choice = session.Projection.PendingEffectChoice;
            if (choice == null || choice.ConfirmInvocation == null || choice.DeclineInvocation == null ||
                // Once per node per run. The rollback is the director Undo, which replays the whole
                // accepted prefix, so a node a seed reaches sixty times would cost sixty replays to
                // learn the one thing it can teach - Wasp's `high-voltage-may` does exactly that.
                !probed.Add(choice.NodeId.Value) ||
                // The rollback is the director Undo, and Undo replays through a snapshot whose
                // rule-event log is bounded at 10,000 entries. A four-thousand-command sweep passes
                // that bound part-way through, and the bound is the envelope's rather than this
                // check's, so the check stops where the instrument does instead of asking the
                // milestone to widen a wire limit it has no business touching. Every decision either
                // board reaches in its first several rounds is well inside it.
                session.Projection.DiagnosticEvents.Count > RollbackReadableEventCount)
            {
                return;
            }

            var taken = AnswerAndRollBack(session, DirectorCommand.AnswerEffectChoice(
                choice.ConfirmInvocation));
            var declined = AnswerAndRollBack(session, DirectorCommand.AnswerEffectChoice(
                choice.DeclineInvocation));
            if (taken != declined)
            {
                return;
            }

            indistinguishable.Add(
                $"{choice.NodeId.Value} ({choice.ChooserId.Value}, {board} seed {seed}, after " +
                $"{commandCount} commands)");
        }

        /// <summary>
        /// The whole sweep's inventory at once, rather than the first one it happened to reach.
        /// Milestone 8.25e: this check found four on its first runs and each failure would otherwise
        /// have hidden the next, which is the same shape of blindness it exists to remove.
        /// </summary>
        private static void AssertOnlyTheRecordedOffersHaveOneAnswer(
            IReadOnlyList<string> indistinguishable)
        {
            var unrecorded = indistinguishable
                .Where(entry => !IndistinguishableAnswersThisMilestoneDoesNotOwn.Any(
                    known => entry.StartsWith(known + " ", StringComparison.Ordinal)))
                .Distinct()
                .ToArray();
            Assert.That(unrecorded, Is.Empty,
                "The projection published a yes-or-no whose answers leave the match in exactly the " +
                "same state, so the player was asked a question with one answer: " +
                string.Join("; ", unrecorded) +
                ". Either the offer is wrong, or the node belongs beside the recorded ones with its " +
                "own reason.");
        }

        /// <summary>
        /// The nodes whose two answers this sweep has observed to be the same state, and which this
        /// milestone deliberately leaves as they are.
        ///
        /// <para><b>Eight nodes across five heroes, all one mechanism, and none of them p.10's
        /// repeat.</b> The check found them on its first runs. Every one is an optional card-text step
        /// - a printed "you may" - whose body begins with a mandatory selection: Wasp's
        /// <c>high-voltage-may</c> and <c>kinetic-push</c>, Brogan's <c>shove-move</c> and
        /// <c>throw-discard</c>, Arien's <c>wave-push</c>, Tigerclaw's <c>theft-opening-move</c>, and
        /// Xargatha's <c>green-may</c> and <c>converts-may</c>. Where the board leaves that selection
        /// nothing to name, taking the offer reaches the step, cannot complete it, and rulebook p.10's
        /// Mandatory and Optional Steps stops the action exactly where declining leaves it. Observed on
        /// both boards, on six seeds each.</para>
        ///
        /// <para><b>Why they are recorded rather than fixed here.</b> The repeat family this milestone
        /// owns is closed by asking one authority whether the instance it would start can start, and
        /// the authority already knows. These would need an optional node to look ahead into a nested
        /// selection's candidates before offering itself, which is a different seam, in card text,
        /// across five heroes' content. The project already holds the ruling such a fix would be built
        /// against - <c>RULES_LEDGER.md</c>'s question 11, where an option the actor cannot perform is
        /// refused rather than offered - so what is missing is the mechanism rather than the decision.
        /// Milestone 8.25e1's independent offer-and-answer reachability inventory is where it belongs,
        /// and this constant is what hands it over: a ninth node fails the sweep until somebody says
        /// why it is here.</para>
        /// </summary>
        private static readonly string[] IndistinguishableAnswersThisMilestoneDoesNotOwn =
        {
            "high-voltage-may", "kinetic-push", "shove-move", "throw-discard", "wave-push",
            "theft-opening-move", "green-may", "converts-may"
        };

        /// <summary>
        /// Gives one answer, reads the state and the history it produced, and puts the match back.
        ///
        /// <para>The rule events the answer raised are read beside the authoritative payload, because
        /// rulebook p.12's Hold is an action whose whole content is that nothing happens: a second one
        /// changes no piece, no card and no counter, and what makes it a performed action rather than a
        /// declined offer is that it was performed - the <c>ActionStarted</c> and <c>ActionEnded</c> it
        /// raises, and the basic action p.9 records. Reading the payload alone would call a real second
        /// Hold indistinguishable from declining one; the captured snapshot carries both but refuses to
        /// serialize once a long sweep has filled its log, so the two are read separately here.</para>
        /// </summary>
        private static string AnswerAndRollBack(MatchSession session, DirectorCommand command)
        {
            var beforeCommands = session.Projection.AcceptedCommands.Count;
            var beforeEvents = session.Projection.DiagnosticEvents.Count;
            var answered = session.Execute(command);
            Assert.That(answered.Accepted, Is.True, answered.Detail);
            var produced = session.Projection.AuthoritativeSnapshot + "\n" + string.Join(
                "\n",
                session.Projection.DiagnosticEvents.Skip(beforeEvents)
                    .Select(entry => entry.DisplayText));
            while (session.Projection.AcceptedCommands.Count > beforeCommands)
            {
                var undone = session.Undo();
                Assert.That(undone.Accepted, Is.True, undone.Detail);
            }

            return produced;
        }

        private static void AssertEveryStandingActionDecisionBelongsToTheSelectedActor(
            MatchSessionProjection projection,
            string board,
            int seed,
            int commandCount)
        {
            if (projection.PendingMovementChoice != null && projection.PendingMovementResolution != null)
            {
                var movement = projection.PendingMovementResolution;
                AssertBelongsToTheSelectedActor(
                    projection, movement.ActorId, movement.SourceCardInstanceId,
                    movement.ResolutionId.Value, board, seed, commandCount);
            }

            // A repeated instance is always a card action - no trigger runs under
            // ActionInvocationContext.Repeated - which is what makes this readable off the merged
            // pending-Skill projection without asking it to say which of the two it is holding.
            var skill = projection.PendingSkillResolution;
            if (projection.PendingEffectChoice != null && skill != null &&
                skill.InvocationContext == ActionInvocationContext.Repeated)
            {
                AssertBelongsToTheSelectedActor(
                    projection, skill.ActorId, skill.SourceCardInstanceId,
                    skill.ResolutionId.Value, board, seed, commandCount);
            }
        }

        private static void AssertBelongsToTheSelectedActor(
            MatchSessionProjection projection,
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            string resolutionId,
            string board,
            int seed,
            int commandCount)
        {
            var selected = projection.SelectedActor;
            var shown = selected.IsDefined
                ? $"{selected.ActorId.Value} on {selected.CardInstanceId.Value}"
                : "nobody";
            var stated =
                $"The {board} board on seed {seed} stands, after {commandCount} commands, on " +
                $"{resolutionId} - {actorId.Value} on {sourceCardInstanceId.Value} - while Initiative " +
                $"has selected {shown}.";
            Assert.That(selected.IsDefined, Is.True, stated);
            Assert.That(selected.ActorId.Equals(actorId), Is.True, stated);
            Assert.That(selected.CardInstanceId.Equals(sourceCardInstanceId), Is.True, stated);
        }

        /// <summary>
        /// The staged one-versus-one board milestone 8.23 shipped Tigerclaw on, composed exactly as
        /// <see cref="StagedHeroBoard"/> composes it.
        /// </summary>
        private static MatchSession CreateStagedWaspVersusTigerclawSession()
        {
            var configuration = MatchConfiguration
                .CreateStagedTigerclawForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateStagedWaspVersusTigerclawOneVsOne(),
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
        /// Rulebook p.5's six-player Quick Game, composed exactly as
        /// <see cref="OfficialSixPlayerSetupTests"/> composes it.
        /// </summary>
        private static MatchSession CreateOfficialSixPlayerSession()
        {
            var configuration = MatchConfiguration
                .CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
