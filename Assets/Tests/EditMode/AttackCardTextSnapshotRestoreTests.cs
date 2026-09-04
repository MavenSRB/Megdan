using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35i. A capability choice paused inside an Attack's card text has to restore, and
    /// before this corrective it did not.
    ///
    /// <para><b>What went wrong, because the shape is the reusable part.</b> Candidates are never
    /// written to a payload - a control node's options come from the trusted definition and a
    /// capability's come from asking the trusted handler again, which is what stops a payload
    /// inventing an authoritative option. Re-asking means rebuilding the question, and Arien's Noble
    /// Blade binds its selection to <c>EnclosingTargetBeforeConsequences</c>: "another unit adjacent
    /// to the target of this Attack". The restore path rebuilt that question with NO enclosing
    /// Attack, so the origin resolved to nobody, the handler offered no choice at all, and the
    /// payload was refused for naming a pause whose capability "no longer requests a choice". The
    /// content had not changed. The question had been asked with half of what it reads.</para>
    ///
    /// <para>The Attack is written to the payload AFTER the frame that pauses inside it, so the
    /// frame is now read as a function of it - the same answer milestone 8.23 gave for the opposing
    /// hero, which the payload also names only after the frame. No byte moved and no schema
    /// changed.</para>
    ///
    /// <para>Reached first by milestone 8.35f's rewind sweep as a refused Undo at a history of 1,262
    /// commands, whose text named nothing useful; the fixtures here reach the same state in a handful
    /// of commands, deterministically, because the position is what matters rather than the walk that
    /// stumbled into it.</para>
    /// </summary>
    public sealed class AttackCardTextSnapshotRestoreTests
    {
        /// <summary>
        /// The focused case: the pause restores, and what it offers afterwards is what it offered
        /// before. Asserting the restore alone would pass against a build that restored the pause and
        /// forgot its candidates, which is the same defect one layer down.
        /// </summary>
        [Test]
        public void ACapabilityChoicePausedInsideAnAttack_RestoresAndStillOffersWhatItOffered()
        {
            var board = CreateBoardWhereNobleBladeHasAUnitToMove();
            PlayNobleBladeToItsCapabilityChoice(board);

            var before = board.Session.Projection.PendingEffectChoice;
            Assert.That(before, Is.Not.Null);
            Assert.That(before.NodeId.Value, Is.EqualTo("noble-other"),
                "The fixture is only the fixture while the capability choice is the one standing.");
            Assert.That(before.Candidates, Is.Not.Empty,
                "A choice with nothing to pick between would restore vacuously.");
            Assert.That(board.Session.Projection.HasPendingAttackResolution, Is.True,
                "and it is paused INSIDE the Attack, which is the whole subject.");

            var captured = board.Session.CaptureSnapshot();
            var restored = board.Session.RestoreSnapshot(captured);

            Assert.That(restored.Accepted, Is.True,
                "The payload has to be readable by the build that wrote it. " + restored.Detail);
            Assert.That(board.Session.CaptureSnapshot(), Is.EqualTo(captured),
                "and reading it back has to produce the state it was written from.");

            var after = board.Session.Projection.PendingEffectChoice;
            Assert.That(after, Is.Not.Null);
            Assert.That(after.NodeId, Is.EqualTo(before.NodeId));
            Assert.That(after.Candidates, Is.EqualTo(before.Candidates),
                "The re-asked handler offers the same units, because it is asked the same question - " +
                "which is what carrying the enclosing Attack across the restore buys.");
            Assert.That(after.MinimumSelections, Is.EqualTo(before.MinimumSelections));
            Assert.That(after.MaximumSelections, Is.EqualTo(before.MaximumSelections));

            // And the restored pause is answerable, not merely present: a decision that reconstructs
            // but cannot be taken would strand the turn holding it.
            var selection = after.SelectInvocations.First();
            Assert.That(board.Session.Execute(DirectorCommand.AnswerEffectChoice(selection)).Accepted,
                Is.True,
                "The restored pause takes its own published answer.");
        }

        /// <summary>
        /// The same stream, checked at every step rather than at its end: every state the reproducing
        /// commands pass through round-trips through a snapshot.
        /// </summary>
        /// <remarks>
        /// <para>The original reproduction was a 1,262-command seeded seat walk pressing an undo every
        /// third offer, and it is deliberately NOT reproduced here. Milestone 8.35g's turn release
        /// moved every seeded trajectory, so that walk no longer reaches this state; more to the
        /// point, a random walk is a poor instrument for it - a scan of twenty-four seeds over 2,500
        /// commands each on the official six-player board reaches an effect choice paused inside an
        /// Attack exactly zero times, because it needs Noble Blade played with a unit beside its
        /// target and its `may` confirmed. The recorded stream is replayed instead, which is the part
        /// of that walk that actually reproduces.</para>
        /// <para>Checking after EVERY command rather than only at the pause is what makes this a
        /// regression rather than a second copy of the case above: the defect was invisible until a
        /// specific intermediate state was captured, and that is the class of thing a per-command
        /// check finds and an end-state check does not.</para>
        /// </remarks>
        [Test]
        public void EveryStateOfTheReproducingStream_RoundTripsThroughASnapshot()
        {
            var recorded = CreateBoardWhereNobleBladeHasAUnitToMove();
            PlayNobleBladeToItsCapabilityChoice(recorded);
            var stream = recorded.Session.Projection.AcceptedCommands.ToArray();
            Assert.That(stream, Is.Not.Empty);

            var replayed = CreateBoardWhereNobleBladeHasAUnitToMove();
            var pausedInsideAnAttack = 0;
            for (var index = 0; index < stream.Length; index++)
            {
                var applied = replayed.Session.Execute(stream[index]);
                Assert.That(applied.Accepted, Is.True,
                    $"Command {index + 1} ({stream[index].Kind}) was refused on replay: {applied.Detail}");

                var captured = replayed.Session.CaptureSnapshot();
                var restored = replayed.Session.RestoreSnapshot(captured);
                Assert.That(restored.Accepted, Is.True,
                    $"The state after command {index + 1} ({stream[index].Kind}) did not restore: " +
                    restored.Detail);
                Assert.That(replayed.Session.CaptureSnapshot(), Is.EqualTo(captured),
                    $"and the state after command {index + 1} did not survive its own round trip.");

                if (replayed.Session.Projection.PendingEffectChoice != null &&
                    replayed.Session.Projection.HasPendingAttackResolution)
                {
                    pausedInsideAnAttack++;
                }
            }

            Assert.That(pausedInsideAnAttack, Is.GreaterThan(0),
                "The stream has to actually pass through a choice paused inside an Attack, or this " +
                "case round-trips a match that never reaches the state it exists for.");
        }

        // -------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------

        /// <summary>
        /// Noble Blade's target with one other unit standing beside it, which is the board its
        /// printed "you may move another unit adjacent to the target" has anything to offer on.
        /// </summary>
        private static StagedHeroBoard CreateBoardWhereNobleBladeHasAUnitToMove() =>
            ArienBoard.Create(
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-melee-02"] = StagedHeroBoard.TwoAway
                },
                heroPlacements: StagedHeroBoard.At("primitive-hero", FarFromTheEngagement),
                banishedMinions: new[]
                {
                    "red-melee-03", "red-melee-04", "red-ranged-01", "red-heavy-01",
                    "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
                    "blue-ranged-01", "blue-heavy-01"
                });

        /// <summary>
        /// Plays the Attack and confirms p.10's `may`, which is what opens the capability choice.
        /// The `may` itself is a confirm-or-decline with no candidates to re-derive, so it restores
        /// on a build that has this defect - which is why the fixture has to go one step further.
        /// </summary>
        private static void PlayNobleBladeToItsCapabilityChoice(StagedHeroBoard board)
        {
            board.PlayAttackAt(
                ArienCardContent.NobleBlade, board.MinionAt(StagedHeroBoard.FirstAdjacent));
            Assert.That(board.Choice, Is.Not.Null);
            Assert.That(board.Choice.NodeId.Value, Is.EqualTo("noble-may"));
            board.Confirm();
        }

        /// <summary>A free playable space far from the engagement, for the seat kept out of it.</summary>
        private static readonly HexCoordinate FarFromTheEngagement = new HexCoordinate(2, 0);
    }
}
