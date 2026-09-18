using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35x-confirmfix. A discard whose subject was never named must be a sentence with
    /// nothing to do, and not a fault.
    ///
    /// <para><b>What was wrong, in one line:</b> <c>synthetic-discard-chosen-card</c> built its card
    /// criteria from an input that an <c>IfAble</c> selection is allowed to leave unbound, and
    /// <see cref="GuardsDigital.Core.Cards.CardCharacteristicCriteria"/> refuses an undefined owner by
    /// construction - so the capability THREW.</para>
    ///
    /// <para><b>Why that looked like an offered control that does nothing.</b> A throw is not a
    /// failure RESULT, so the node's own <c>EffectFailurePolicy.IfAble</c> could not absorb it. It
    /// escaped the resolution, faulted the command at the aggregate's commit boundary, and the match
    /// was rolled back to a state byte-identical to the one the player was already looking at: the
    /// same status line, the same single offered destination, the same Confirm still standing, and the
    /// piece still on its staged space. Milestone 8.35z38 spent eight Play Mode runs on that signature
    /// and integration batch C measured it pressing the control four times running with nothing
    /// changing. The answer had always been legal; what faulted was the trigger that ran after it.</para>
    ///
    /// <para><b>Two parked journeys, one defect.</b> Both cases below fault identically on the shipped
    /// code and pass identically with the guard: Power Overwhelming's placement trigger (RULING 30,
    /// "an enemy hero adjacent to you discards a card, if able") and Dash and Slash's line rider ("an
    /// enemy hero you moved through discards a card, if able"). They are different compositions of the
    /// one shape - a selection that may name nobody, feeding a discard that reads its output - which is
    /// why one guard closes both.</para>
    ///
    /// <para>The offer side was never wrong: <c>AnticipateCandidates</c> has stated this same rule
    /// since milestone 8.21. That asymmetry is the reusable finding - a rule wired into the
    /// anticipating caller and not into the executing one is invisible to every test that only reads
    /// what is offered.</para>
    /// </summary>
    public sealed class UnnamedDiscardSubjectTests
    {
        /// <summary>
        /// Takes both of Watch How I Soar's printed bullets, which is what RULING 28's raised ceiling
        /// admits, and leaves the Place bullet's destination ring standing.
        /// </summary>
        private static void OpenThePlacementRing(StagedHeroBoard board)
        {
            for (var step = 0; step < 4; step++)
            {
                var standing = board.Choice;
                if (standing == null ||
                    !standing.SelectInvocations.Any(invocation =>
                        invocation.Candidate.Kind == EffectChoiceCandidateKind.Option))
                {
                    break;
                }

                board.SelectFirst();
            }

            board.Confirm();
        }

        /// <summary>
        /// A space the ring is offering that no enemy hero is standing next to - which is exactly the
        /// board on which the trigger's own selection names nobody, and therefore the only place this
        /// defect is reachable. Chosen from the live offer rather than hard-coded, so a staging change
        /// reports rather than silently tests something else.
        /// </summary>
        private static HexCoordinate ASpaceWithNoEnemyHeroBesideIt(StagedHeroBoard board)
        {
            var ring = board.CandidateSpaces(board.Choice);
            Assert.That(ring, Is.Not.Empty, "The placement has to offer somewhere to stand.");

            var enemyAt = board.HeroSpace(board.Enemy);
            var landing = ring
                .Where(space => HexDistanceQuery.Calculate(space, enemyAt) > 1)
                .OrderByDescending(space => HexDistanceQuery.Calculate(space, enemyAt))
                .FirstOrDefault();
            Assert.That(
                ring.Contains(landing), Is.True,
                "The ring has to offer at least one space with no enemy hero beside it, because that " +
                "is the board on which the trigger's `if able` selection names nobody. Offered: " +
                string.Join(", ", ring.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, enemyAt)}")));
            return landing;
        }

        /// <summary>
        /// RULING 30's trigger with no subject: she places herself where no enemy hero stands beside
        /// her, and the placement resolves.
        ///
        /// <para>This is milestone 8.35z38's parked placement driven to its end. On the shipped code
        /// the answer below is refused as an engine fault and she never leaves her staged space.</para>
        ///
        /// <para>Milestone 8.46b moved where that answer is given without changing what it has to
        /// do. The ring admits exactly one space, so naming one IS the answer and the resolution
        /// session runs the placement inside that command; the refusal this case exists to catch
        /// would now come back on the select. What the case asserts is unchanged and is the thing
        /// that matters: the command is accepted and she stands where she chose.</para>
        /// </summary>
        [Test]
        public void APlacementTriggerThatNamesNobody_ResolvesTheConfirmInsteadOfFaultingIt()
        {
            var board = MisaBoard.Create(tier: 3);
            var stoodAt = board.HeroSpace(board.Hero);
            board.StartPrimary(MisaCardContent.WatchHowISoar);
            OpenThePlacementRing(board);

            var ring = board.Choice;
            Assert.That(ring.ResolvesOnSelection, Is.True,
                "The ring asks for exactly one space, so naming one is the whole answer.");

            // The whole milestone: this command is accepted. `Select` submits through `Accept`, which
            // fails the test on a refusal, so the shipped fault is caught here by name.
            var landing = ASpaceWithNoEnemyHeroBesideIt(board);
            board.Select(landing);

            Assert.That(
                board.Choice == null || !board.Choice.ChoiceId.Equals(ring.ChoiceId), Is.True,
                "and nothing of the ring is left standing to press a second time.");

            Assert.That(
                board.HeroSpace(board.Hero), Is.EqualTo(landing),
                "The confirmed placement puts her on the space she chose.");
            Assert.That(
                board.HeroSpace(board.Hero), Is.Not.EqualTo(stoodAt),
                "and she is no longer on her staged space, which is the observable change four " +
                "presses of this control failed to produce.");
        }

        /// <summary>
        /// The same defect through Dash and Slash's rider, which is the second parked journey. Her line
        /// ends where no enemy hero was crossed, so the rider's selection names nobody.
        /// </summary>
        [Test]
        public void ALineRiderThatNamesNobody_ResolvesTheConfirmInsteadOfFaultingIt()
        {
            var board = MisaBoard.Create(tier: 2, branch: MisaUpgradeBranch.SecondSibling);
            var stoodAt = board.HeroSpace(board.Hero);
            board.StartPrimary(MisaCardContent.DashAndSlash);

            var landing = ASpaceWithNoEnemyHeroBesideIt(board);
            board.Select(landing);
            board.ConfirmIfStillOwed();

            Assert.That(
                board.HeroSpace(board.Hero), Is.EqualTo(landing),
                "The line travels to the end she named.");
            Assert.That(
                board.HeroSpace(board.Hero), Is.Not.EqualTo(stoodAt),
                "which is the movement the second parked journey never saw.");
        }

        /// <summary>
        /// The standing placement prompt captured and restored, and still answerable afterwards.
        /// Isolated from the Undo case below because a restore permanently disables history, which
        /// milestone 8.35z34 measured - an Undo attempted after one is testing nothing.
        ///
        /// <para>Milestone 8.46b. The capture moved one command earlier, because the state it used
        /// to be taken in no longer exists: the ring admits exactly one space, so naming one closes
        /// it inside that command and there is no half-filled pause to snapshot. The property is
        /// the same and is asserted on the state that does exist - the ring standing with its whole
        /// offer, captured, restored, and then answered into the same resolution.</para>
        /// </summary>
        [Test]
        public void TheStandingPlacementPrompt_RoundTripsThroughASnapshotAndStillResolves()
        {
            var board = MisaBoard.Create(tier: 3);
            board.StartPrimary(MisaCardContent.WatchHowISoar);
            OpenThePlacementRing(board);

            var landing = ASpaceWithNoEnemyHeroBesideIt(board);
            var node = board.Choice.NodeId;
            var ringBefore = board.CandidateSpaces(board.Choice).Length;
            board.RoundTripThroughASnapshot();

            Assert.That(
                board.Choice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on.");
            Assert.That(board.Choice.NodeId, Is.EqualTo(node), "on the same node,");
            Assert.That(
                board.CandidateSpaces(board.Choice).Length, Is.EqualTo(ringBefore),
                "offering the same ring it offered before the capture,");
            Assert.That(
                board.CandidateSpaces(board.Choice), Does.Contain(landing),
                "including the space this case is about.");

            board.Select(landing);
            Assert.That(
                board.HeroSpace(board.Hero), Is.EqualTo(landing),
                "and the restored pause answers into the same resolution it was captured from.");
        }

        /// <summary>
        /// The named space dropped by an Undo and named again, with the answer still resolving.
        /// Isolated from the restore case above, and driven first in its own fixture for the same
        /// reason.
        ///
        /// <para>Milestone 8.46b made this stricter rather than weaker. Naming the space used to be
        /// the first of two accepted commands, so the Undo dropped half an answer and landed the
        /// player on a pause standing on Deselect and Confirm - a state they had never seen going
        /// forwards. The ring admits exactly one space, so naming one now PERFORMS the placement in
        /// one command and one press back gives the whole question again, with her where she
        /// started.</para>
        /// </summary>
        [Test]
        public void TheStandingPlacementPrompt_SurvivesAnUndoAndStillResolves()
        {
            var board = MisaBoard.Create(tier: 3);
            board.StartPrimary(MisaCardContent.WatchHowISoar);
            OpenThePlacementRing(board);

            var ringBefore = board.CandidateSpaces(board.Choice).Length;
            var stoodAt = board.HeroSpace(board.Hero);
            var landing = ASpaceWithNoEnemyHeroBesideIt(board);
            board.Select(landing);
            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(landing),
                "One press placed her, which is what the press back has to give up.");

            Assert.That(
                board.Session.Undo().Accepted, Is.True,
                "The named space is an accepted command, so there is something to drop.");
            Assert.That(
                board.Choice, Is.Not.Null,
                "and the ring is standing again, replayed rather than remembered.");
            Assert.That(
                board.Choice.SelectedCandidates, Is.Empty,
                "with the answer it claimed to drop actually dropped,");
            Assert.That(
                board.HeroSpace(board.Hero), Is.EqualTo(stoodAt),
                "her back where the question found her,");
            Assert.That(
                board.CandidateSpaces(board.Choice).Length, Is.EqualTo(ringBefore),
                "and every space offered again.");

            board.Select(landing);
            Assert.That(
                board.HeroSpace(board.Hero), Is.EqualTo(landing),
                "A replayed pause answers into the resolution the replay rebuilt.");
        }
    }
}
