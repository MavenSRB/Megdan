using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using NUnit.Framework;
using static GuardsDigital.Tests.EditMode.BeforeActionStepPositions;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.9. A play reached only through its own before-the-action step is offered - by the
    /// rules and by the seat the player sits at.
    ///
    /// <para><b>The family.</b> The bug that hurt live play most was an offer that missed a viable
    /// play whose target is reached only through the card's own step ("before the attack: move 1,
    /// then attack"). 9.6 and 9.7 judge offers against outcomes, but only in the positions random
    /// play happens to reach and only at the rules offer. This sweep builds the positions that
    /// decide the family on purpose, for every face <see cref="BeforeActionStepPositions"/> derives
    /// from the content, and reads the offer at two layers on one state: the rules offer
    /// (<c>MatchSession.Projection</c>) and the owner-scoped seat projection
    /// (<c>SeatProjectionFactory</c>, through <c>MatchHost.ProjectionForSeat</c>). The third layer,
    /// the HUD read model, needs Presentation and is <c>BeforeActionStepHudTests</c>, on the same
    /// positions.</para>
    ///
    /// <para><b>What is a finding.</b> A face offered by one layer and greyed by another; or greyed
    /// by the rules where forcing the action through
    /// <c>StartActionIgnoringFeasibilityForInternalTest</c> and trying every answer reaches a target
    /// (an Attack) or ends anywhere Hold does not (a Skill). Nothing here asks what the card
    /// should do - only whether the offer agrees with what the rules then let it do.</para>
    /// </summary>
    public sealed class BeforeActionStepOfferTests
    {
        private static readonly Lazy<Sweep> Swept = new Lazy<Sweep>(Run);

        [Test]
        public void EveryDerivedFace_IsStagedInEveryPosition()
        {
            var sweep = Swept.Value;
            foreach (var line in sweep.Report)
            {
                TestContext.Out.WriteLine(line);
            }

            Assert.That(sweep.Faces, Is.GreaterThan(20),
                "The content derivation found almost no stepped faces; it is not reading the corpus.");
            Assert.That(sweep.Unstaged, Is.Empty,
                "These faces could not be staged, so the sweep says nothing about them.");
        }

        [Test]
        public void EveryDerivedFace_IsAskedBothWithoutTheStepAndWithTheStepBlocked()
        {
            foreach (var face in Faces)
            {
                var positions = PositionsFor(face);
                Assert.That(positions.Any(position => position.BeyondReach && !position.Walled), Is.True,
                    $"{face.Name} has no position where only its step can reach a target.");
                Assert.That(positions.Any(position => position.Walled), Is.True,
                    $"{face.Name} has no position where its step cannot be taken.");
            }
        }

        [Test]
        public void TheSeatProjection_OffersExactlyWhatTheRulesOffer()
        {
            Assert.That(Swept.Value.SeatDisagreements, Is.Empty,
                "The owner's seat projection disagreed with the rules offer on these stepped faces.");
        }

        [Test]
        public void NoGreyedSteppedPlay_CouldHaveBeenTaken()
        {
            Assert.That(Swept.Value.GreyedButLegal, Is.Empty,
                "These faces were greyed by the rules, yet forced past the offer some answer reached " +
                "a target (an Attack) or changed the game (a Skill): a legal play the player was denied.");
            Assert.That(Swept.Value.GreyedTried, Is.GreaterThan(20),
                "A sweep forcing few greyed offers is not asking the question it exists for.");
        }

        /// <summary>
        /// Milestone 9.9, the sweep's finding on one board: Arien's Living Tsunami ("Once per turn,
        /// before performing an Attack action, you may move 1 space") stands, and the only enemy unit
        /// is two spaces away. Violent Torrent's "a unit adjacent to you" reaches nobody from where he
        /// stands and somebody from a space the step reaches, and the step runs before the target is
        /// chosen - so the Attack is a legal play, and it was greyed.
        /// </summary>
        [Test]
        public void AnAttackWhoseOnlyTargetABeforeActionStepBringsInReach_IsOffered_AndReachesIt()
        {
            var board = ArienBoard.Create(
                tier: 3,
                minionPlacements: new Dictionary<string, GuardsDigital.Core.Board.HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.TwoAway
                },
                heroPlacements: StagedHeroBoard.At(
                    "primitive-hero", new GuardsDigital.Core.Board.HexCoordinate(2, 0)),
                banishedMinions: new[]
                {
                    "red-melee-02", "red-melee-03", "red-melee-04", "red-ranged-01", "red-heavy-01",
                    "blue-melee-01", "blue-melee-02", "blue-melee-03", "blue-melee-04",
                    "blue-ranged-01", "blue-heavy-01"
                });

            var offer = board.PrimaryOffer(GuardsDigital.Core.Cards.Verified.ArienCardContent.ViolentTorrent);

            Assert.That(offer.IsAvailable, Is.True,
                "Nobody is adjacent to Arien, but Living Tsunami's one space puts the minion beside him " +
                "before the Attack chooses its target.");
            board.StartOffer(offer);
            Assert.That(board.Choice, Is.Not.Null, "The step is asked first.");
            var beside = board.CandidateSpaces(board.Choice).First(space =>
                GuardsDigital.Core.Board.HexDistanceQuery.Calculate(space, StagedHeroBoard.TwoAway) == 1);
            board.Select(beside);
            Assert.That(board.Session.Projection.PendingAttackTargetChoice != null ||
                    board.Session.Projection.PendingAttackResolution != null ||
                    board.Session.Projection.PendingDefenseChoice != null,
                Is.True, "and from beside the minion the Attack takes it.");
        }

        [Test]
        public void TheSweep_ExploresEveryForcedPlayToTheEnd()
        {
            Assert.That(Swept.Value.Undecided, Is.Empty,
                "A forced play's answers outgrew the exploration budget, so it was neither proved " +
                "legal nor reported; raise the budget rather than let it pass unread.");
        }

        internal sealed class Sweep
        {
            public readonly List<string> Report = new List<string>();
            public readonly List<string> Unstaged = new List<string>();
            public readonly List<string> SeatDisagreements = new List<string>();
            public readonly List<string> GreyedButLegal = new List<string>();
            public readonly List<string> Undecided = new List<string>();
            public readonly List<string> OfferedWithoutATarget = new List<string>();
            public int Faces;
            public int GreyedTried;
        }

        private static Sweep Run()
        {
            var sweep = new Sweep();
            foreach (var face in Faces)
            {
                sweep.Faces++;
                sweep.Report.Add($"{face.Hero}: {face.Name} ({face.Action.Kind} {face.Action.Id.Value})");
                foreach (var position in PositionsFor(face))
                {
                    var where = $"{face.Name} - {position.Name}";
                    var staged = Stage(face, position, out var unstaged);
                    if (staged == null)
                    {
                        sweep.Unstaged.Add($"{where}: {unstaged}");
                        continue;
                    }

                    var offered = staged.Offer.IsAvailable;
                    var seat = SeatReading(staged);
                    if (seat != offered)
                    {
                        sweep.SeatDisagreements.Add(
                            $"{where}: rules {(offered ? "offer" : "grey")}, seat {(seat ? "offers" : "greys")}");
                    }

                    var verdict = offered ? "offered" : "greyed";
                    if (offered && face.Action.Kind == ActionKind.Attack)
                    {
                        // The converse, for the report: an offered Attack that no answer brings to a
                        // target is a card spent on nothing but its step (RULING 45's rope).
                        var taken = OfferOutcomeTests.Restored(staged.Initial, staged.Session.CaptureSnapshot());
                        var budget = OfferOutcomeTests.AnswerPathsPerOffer;
                        if (taken.Execute(DirectorCommand.StartAction(
                                staged.Offer.SourceCardInstanceId, staged.Offer.ActionDefinitionId)).Accepted &&
                            !OfferOutcomeTests.ReachesATarget(staged.Initial, taken, staged.Actor, 0, ref budget) &&
                            budget > 0)
                        {
                            sweep.OfferedWithoutATarget.Add(where);
                            verdict += ", but no answer reaches a target";
                        }
                    }

                    if (!offered && !staged.Offer.Unavailability.IsStated)
                    {
                        sweep.GreyedTried++;
                        var budget = OfferOutcomeTests.AnswerPathsPerOffer;
                        var legal = ForcedIsLegal(staged, face, position, ref budget);
                        if (legal)
                        {
                            sweep.GreyedButLegal.Add(where);
                            verdict += ", yet forced it is legal";
                        }
                        else if (budget <= 0)
                        {
                            sweep.Undecided.Add(where);
                        }
                    }

                    sweep.Report.Add($"    {position.Name}: {verdict}");
                }
            }

            return sweep;
        }

        /// <summary>
        /// The owner's seat: its projected offer is available, and its authorized commands carry the
        /// start exactly when it is. Two readings the seat surface is built from, so both must agree.
        /// </summary>
        internal static bool SeatReading(StagedStep staged)
        {
            var host = new MatchHost(staged.Session);
            var seat = host.Seats.FindByHero(staged.Actor);
            var projection = host.ProjectionForSeat(seat.Id);
            var offer = projection.AvailableTurnActions.SingleOrDefault(candidate =>
                candidate.SourceCardInstanceId.Equals(staged.Offer.SourceCardInstanceId) &&
                candidate.ActionDefinitionId.Equals(staged.Offer.ActionDefinitionId));
            var start = DirectorCommand.StartAction(
                staged.Offer.SourceCardInstanceId, staged.Offer.ActionDefinitionId);
            var authorized = projection.AuthorizedCommands.Contains(start);
            return offer.ActionDefinitionId.IsDefined && offer.IsAvailable && authorized;
        }

        /// <summary>
        /// Whether the greyed action, started anyway, has a legal completion: an Attack some answer
        /// brings to a target, or a Skill some answer leaves anywhere Hold does not.
        /// </summary>
        internal static bool ForcedIsLegal(
            StagedStep staged, SteppedFace face, StepPosition position, ref int budget)
        {
            var snapshot = staged.Session.CaptureSnapshot();
            var forced = OfferOutcomeTests.Restored(staged.Initial, snapshot);
            if (!forced.StartActionIgnoringFeasibilityForInternalTest(
                    staged.Offer.SourceCardInstanceId, staged.Offer.ActionDefinitionId).Accepted)
            {
                return false;
            }

            if (face.Action.Kind == ActionKind.Attack)
            {
                return OfferOutcomeTests.ReachesATarget(staged.Initial, forced, staged.Actor, 0, ref budget);
            }

            if (face.Ability != null)
            {
                // The ability's own move changes the board whatever the Skill then does, so "ends
                // anywhere Hold does not" would count the step as the Skill. What is asked instead is
                // the Skill's own question from every square the step can leave the hero on: the
                // same position, the hero standing there, no ability, and the rules' own offer.
                var reached = new HashSet<GuardsDigital.Core.Board.HexCoordinate>();
                StepEnds(staged, forced, TriggerNodes(face), reached, 0, ref budget);
                foreach (var space in reached)
                {
                    var moved = Stage(face, position, out _, space, withoutTheAbility: true);
                    if (moved != null && moved.Offer.IsAvailable)
                    {
                        return true;
                    }
                }

                return false;
            }

            var hold = OfferOutcomeTests.Restored(staged.Initial, snapshot);
            Assert.That(hold.Execute(DirectorCommand.PerformHold(staged.Offer.SourceCardInstanceId)).Accepted,
                Is.True, "p.12: any card can Hold.");
            var held = OfferOutcomeTests.Settled(hold);
            return OfferOutcomeTests.ForcedDoesSomething(
                staged.Initial, forced, staged.Actor, held, ref budget, face.Card.Id.Value);
        }

        /// <summary>The nodes of the ability's own before-action effect.</summary>
        private static HashSet<string> TriggerNodes(SteppedFace face)
        {
            var reference = face.Ability.UltimateRule.TriggeredEffect;
            Assert.That(reference.HasValue, Is.True, $"{face.Ability.Id.Value} declares a step it runs.");
            Assert.That(MatchContentCatalogs.ForThisBuild().TriggeredEffects.TryGet(
                reference.Value, out var effect), Is.True);
            return new HashSet<string>(effect.Nodes.Select(node => node.Id.Value), StringComparer.Ordinal);
        }

        /// <summary>
        /// Every square the ability's step can leave the hero on: each of its questions answered every
        /// way the rules offer, until the step is over and the held action is about to begin.
        /// </summary>
        private static void StepEnds(
            StagedStep staged, MatchSession session, HashSet<string> stepNodes,
            HashSet<GuardsDigital.Core.Board.HexCoordinate> reached, int depth, ref int budget)
        {
            var projection = session.Projection;
            var choice = projection.PendingEffectChoice;
            if (choice == null || !stepNodes.Contains(choice.NodeId.Value) ||
                projection.DecisionContract == null || depth > 4)
            {
                budget--;
                var standing = projection.Heroes.SingleOrDefault(hero => hero.HeroId.Equals(staged.Actor));
                if (standing.HeroId.IsDefined)
                {
                    reached.Add(standing.Coordinate);
                }

                return;
            }

            var here = session.CaptureSnapshot();
            foreach (var answer in projection.DecisionContract.Commands)
            {
                if (budget <= 0)
                {
                    return;
                }

                var branch = OfferOutcomeTests.Restored(staged.Initial, here);
                if (branch.Execute(answer).Accepted)
                {
                    StepEnds(staged, branch, stepNodes, reached, depth + 1, ref budget);
                }
            }
        }
    }
}
