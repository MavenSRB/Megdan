using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z25. Combat Reflexes and Leaping Strike print
    /// <i>"Before the attack: You may move 1 space. Target a unit adjacent to you."</i>, and milestone
    /// 8.23 played those two sentences in the opposite order from the one they are printed in.
    ///
    /// <para><b>The ruling.</b> The user's 2026-09-06 decision is that a card's PRINTED READING ORDER
    /// governs the move/target sequence. These two faces get a corrective to move-then-target:
    /// optionally move one space first, then target a unit adjacent to the space that move left him
    /// in. The rest of the roster already matched its own print - Brogan's charges and Blink Strike
    /// move first as printed, Arien's five name their target first as printed - so this is two faces
    /// being brought onto the rule the others were already on, not a new rule.</para>
    ///
    /// <para><b>What the old order actually cost.</b> It was not a presentation detail. With the
    /// target named at the action's start and the move applied afterwards, the Attack could name a
    /// neighbour and then legally walk out of contact with him inside its own resolution - measured by
    /// the defense-offer corrective, which found p.14's step 3 opening with attacker and target two
    /// spaces apart. Naming the target last ends that class outright: there is no interval left
    /// between the naming and the defender's answer for the attacker to move in.</para>
    ///
    /// <para><b>The stranding case is the player's rope, and it is a ruling rather than an oversight.</b>
    /// The move text demands nothing of the space, so every destination is offered - including one that
    /// leaves nobody beside him. Taking it means the mandatory targeting has nothing to name and p.10
    /// stops the action there, with the card spent and the move standing.
    /// <see cref="AStrandingDestinationIsOffered_AndTakingItStopsTheActionAtTheMandatoryStep"/> is that
    /// case. It is the printed difference from Brogan's charges, whose own text demands a destination
    /// "adjacent to an enemy unit" and which therefore filter destinations and grey the whole Attack
    /// when none qualifies.</para>
    /// </summary>
    [TestFixture]
    public class TigerclawReadingOrderTests
    {
        /// <summary>The two faces the corrective moves, with the p.15 stage each is dealt at.</summary>
        private static IEnumerable<TestCaseData> TheTwoFaces()
        {
            yield return new TestCaseData(TigerclawCardContent.CombatReflexes, 2)
                .SetName("CombatReflexes");
            yield return new TestCaseData(TigerclawCardContent.LeapingStrike, 3)
                .SetName("LeapingStrike");
        }

        // -----------------------------------------------------------------------------------------
        // The order itself
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The printed order, driven: the move is the first thing asked and nothing has been targeted
        /// when it is, then the targeting follows it.
        /// </summary>
        [TestCaseSource(nameof(TheTwoFaces))]
        public void TheMoveIsAskedFirst_AndNothingIsTargetedUntilItIsAnswered(
            CardDefinitionId face, int tier)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));

            board.StartPrimary(face);

            Assert.That(board.Session.Projection.PendingAttackTargetChoice, Is.Null,
                "p.14's ordinary target question is not what opens this Attack any more - the card " +
                "names its own target, in the sentence it prints after the move.");
            Assert.That(board.Choice?.NodeId.Value, Is.EqualTo("before-move"),
                "\"You may move 1 space.\" is printed first and is asked first.");

            board.Decline();

            Assert.That(board.Choice?.NodeId.Value, Is.EqualTo("before-target"),
                "\"Target a unit adjacent to you.\" is printed second and is asked second.");
        }

        /// <summary>
        /// RULING 45, measured. The move text is unconstrained, so every space one step away is a
        /// legal destination and declining is the other answer. Nothing filters this selection and no
        /// feasibility layer quietly removes a destination that would strand him.
        ///
        /// <para><b>This is the printed difference from Brogan's charges</b>, whose text names what the
        /// destination has to be next to and whose selection therefore refuses the spaces that are
        /// not. Reusing that filtering here would be enforcing a sentence these two faces do not
        /// print.</para>
        /// </summary>
        [TestCaseSource(nameof(TheTwoFaces))]
        public void TheMoveOffersEverySpaceOneStepAway_AndDecliningIsTheOtherAnswer(
            CardDefinitionId face, int tier)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));

            board.StartPrimary(face);
            var offered = board.CandidateSpaces(board.Choice);
            var here = board.HeroSpace(board.Hero);

            Assert.That(offered, Is.Not.Empty, "\"You may move 1 space\" has somewhere to go.");
            Assert.That(offered.All(space => Distance(here, space) == 1), Is.True,
                "One space is exactly one - the distance is not \"up to\", so nothing further is " +
                "offered and standing still is the decline rather than a destination.");
            Assert.That(offered, Has.Member(StagedHeroBoard.ThirdAdjacent),
                "The destination that leaves no unit beside him is offered like any other: the " +
                "sentence demands nothing of the space, so no feasibility reading may withhold it.");
            Assert.That(board.Choice.DeclineInvocation, Is.Not.Null,
                "p.17's `you may` - staying put is an answer to this step, not the absence of one.");
        }

        /// <summary>
        /// The target is judged from the space the move produced, and the measurement is the point:
        /// a unit that is beside him only AFTER the move is offered, and the offer is read off the
        /// node rather than inferred.
        /// </summary>
        [Test]
        public void TheTargetSetIsMeasuredFromWhereTheMoveLeftHim_NotFromWhereHeStarted()
        {
            var board = TigerclawBoard.Create(
                tier: 2,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent,
                    ["red-ranged-01"] = OnlyBesideTheDestination
                });
            var besideTheStart = board.MinionAt(StagedHeroBoard.FirstAdjacent);
            var besideTheDestination = board.MinionAt(OnlyBesideTheDestination);

            board.StartPrimary(TigerclawCardContent.CombatReflexes);

            Assert.That(board.CandidateSpaces(board.Choice),
                Has.Member(StagedHeroBoard.SecondAdjacent));
            board.Select(StagedHeroBoard.SecondAdjacent);
            board.Confirm();

            var reachable = board.CandidateMinions(board.Choice);

            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(StagedHeroBoard.SecondAdjacent));
            Assert.That(reachable, Has.Member(besideTheDestination),
                "This minion is two spaces from where the action started and one from where the " +
                "move ended, so offering it is the whole of what \"adjacent to you\" now means.");
            Assert.That(reachable, Has.No.Member(besideTheStart),
                "and the one he was standing next to when the action began is NOT offered - the " +
                "set is measured from the post-move space, so it loses as well as gains. Under the " +
                "order 8.23 shipped, this was the only minion the card could have named.");
        }

        /// <summary>
        /// THE EXPLOIT, gone. Under the old order the attacker named a neighbour and his own printed
        /// body then walked him out of contact with the unit he had already named, which is the state
        /// the defense-offer corrective measured at p.14's step 3.
        ///
        /// <para>The reordering removes the interval rather than policing it: the target is named from
        /// the post-move space, so at the moment a target exists the attacker is beside it, and the
        /// action goes straight on to the defender's answer with nothing left to ask him.</para>
        /// </summary>
        [Test]
        public void TheAttackCannotOutrunItsOwnTarget_BecauseNothingMovesHimAfterTheNaming()
        {
            var board = TigerclawBoard.Create(
                tier: 2,
                heroPlacements: StagedHeroBoard.At("primitive-hero", StagedHeroBoard.ThirdAdjacent));

            board.StartPrimary(TigerclawCardContent.CombatReflexes);
            board.Decline();
            board.Select(board.Enemy);
            board.Confirm();

            Assert.That(board.Session.Projection.PendingDefensePreview, Is.Not.Null,
                "Naming the target hands p.14 straight to its step 3.");
            Assert.That(board.Choice, Is.Null,
                "and the attacker is asked nothing between naming his target and the defender " +
                "answering - which is the interval the old order gave him to step away in.");
            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.HeroSpace(board.Enemy)), Is.EqualTo(1),
                "so the two are in contact at the Defense response, by construction rather than by " +
                "a guard that checks it afterwards.");
        }

        // -----------------------------------------------------------------------------------------
        // RULING 45's stranding case
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// RULING 45: the stranding destination is offered, taking it is legal, and the action then
        /// stops at the mandatory targeting step under p.10 - the card spent, the move standing.
        ///
        /// <para><b>What is asserted is the whole shape of the stop, not just its absence of an
        /// attack.</b> A stop that silently rewound the move, or left the card unspent, or refused the
        /// destination in the first place would each be a different rule from the one the user
        /// ruled.</para>
        /// </summary>
        [TestCaseSource(nameof(TheTwoFaces))]
        public void AStrandingDestinationIsOffered_AndTakingItStopsTheActionAtTheMandatoryStep(
            CardDefinitionId face, int tier)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));
            var victim = board.MinionAt(StagedHeroBoard.FirstAdjacent);

            board.StartPrimary(face);
            board.Select(StagedHeroBoard.ThirdAdjacent);
            board.Confirm();

            Assert.That(board.HeroSpace(board.Hero), Is.EqualTo(StagedHeroBoard.ThirdAdjacent),
                "The move stands. p.10 stops what FOLLOWS a mandatory step it cannot complete; it " +
                "does not undo what the text already did.");
            Assert.That(board.MinionIds(), Has.Member(victim),
                "and nothing was attacked - the rope is that the turn is spent, not that it hits " +
                "something else.");
            Assert.That(
                Distance(board.HeroSpace(board.Hero), board.SpaceOf(victim)), Is.EqualTo(2),
                "and he has stranded himself, which is the whole case.");
            Assert.That(board.Choice, Is.Null,
                "\"Target a unit adjacent to you.\" has nothing to name, and a mandatory step with " +
                "no legal answer is not turned into a question.");
            Assert.That(board.Session.Projection.PendingDefensePreview, Is.Null,
                "No target was ever named, so p.14 never reaches its Defense response.");
        }

        // -----------------------------------------------------------------------------------------
        // "If you did not move before the attack" - the reading, in the test's own name
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// THE READING SHIPPED, recorded in the name. Combat Reflexes' step 7 asks whether he
        /// <i>did not move</i> before the attack, and the printed words are about MOVING rather than
        /// about having been offered the move.
        ///
        /// <para>The two readings cannot come apart on this card and that is why the positional one is
        /// correct and sufficient: the step is "move 1 space" at exactly one, not "up to" one, so
        /// there is no way to take the offer and end where he began. Taking it always changes his
        /// space; declining always leaves it. <c>actor-moved-this-action@1</c> compares where he
        /// stands with where this Attack recorded him standing when it began, which is that same
        /// fact.</para>
        ///
        /// <para><b>A standing observation the reorder does not change.</b> That capability answers
        /// "did his space change", so anything else that relocated him inside the same Attack would
        /// read as movement here. No dealt content can do that between these two moments; if a later
        /// face can, this clause needs the offer's own answer rather than the position.</para>
        /// </summary>
        [Test]
        public void DidNotMove_ReadsHisSpaceRatherThanTheOffer_AndTakingTheStepAlwaysChangesIt()
        {
            var board = TigerclawBoard.Create(
                tier: 2,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));

            board.StartPrimary(TigerclawCardContent.CombatReflexes);
            var startedAt = board.HeroSpace(board.Hero);
            var destinations = board.CandidateSpaces(board.Choice);

            // `Has.No.Member` rather than `Does.Not.Contain`: Unity's custom NUnit build resolves
            // the latter to the STRING constraint and refuses a HexCoordinate, which the dotnet
            // harness's own NUnit accepts. The presentation compile probe is what caught it.
            Assert.That(destinations, Has.No.Member(startedAt),
                "The step is one space and not \"up to\" one, so his own space is not among the " +
                "destinations: taking the offer always moves him, and \"did not move\" is therefore " +
                "exactly the set of players who declined.");

            board.Select(StagedHeroBoard.FourthAdjacent);
            board.Confirm();
            board.Select(board.MinionAt(StagedHeroBoard.FirstAdjacent));
            board.Confirm();

            Assert.That(board.NodeApplied("reflexes-move"), Is.False,
                "He moved, so the step-7 clause is false and its move never runs.");
        }

        // -----------------------------------------------------------------------------------------
        // RULING 47: a card-text Move inside an Attack is not a Movement action
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// RULING 47: the "Before the attack:" move is a CARD-TEXT Move inside an Attack, not a
        /// Movement action - so movement-action RESTRICTIONS do not block it and movement-action
        /// BUFFS do not widen it.
        ///
        /// <para><b>What this asserts, and why that is the whole distinction.</b> Restrictions reach
        /// an offer through <see cref="ActionRestrictionQuery"/>, which asks
        /// <see cref="RuleActivityQuery.ForAction"/> about the ACTION's kind. These moves are carried
        /// by an action whose kind is Attack, and Attack does not map to
        /// <see cref="RuleActivity.Movement"/> - so a Movement restriction has no path to this offer.
        /// The step itself is <c>synthetic-direct-move</c>, p.18's direct Move, which is the same
        /// contract every other card-text move of his uses and is a different seam from the Movement
        /// action's own backend. Both halves are read off the composed content rather than described,
        /// and the after-attack moves are held to the same statement.</para>
        ///
        /// <para><b>The driven half is integration-owed.</b> A case that pins the hero with a live
        /// Movement restriction and then reads this offer would be the stronger evidence; the staged
        /// one-versus-one deals no hand that can apply one to him, and staging a cross-hero board to
        /// get it is out of a corrective's scope. Recorded rather than skipped.</para>
        /// </summary>
        [TestCaseSource(nameof(TheTwoFaces))]
        public void TheCardTextMoveIsNotAMovementAction_SoNoMovementRestrictionHasAPathToIt(
            CardDefinitionId face, int tier)
        {
            var configuration = MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
            var attack = configuration.CardDefinitions
                .Single(definition => definition.Reference.DefinitionId.Equals(face))
                .ActionDefinitions
                .Single(action => action.Kind == ActionKind.Attack);

            Assert.That(RuleActivityQuery.ForAction(attack.Kind),
                Is.Not.EqualTo(RuleActivity.Movement),
                "The action carrying this move is an Attack, and a restriction is matched against " +
                "the action's own activity - so a bar on Movement actions never reaches this offer.");

            foreach (var moment in new[]
                     {
                         CardTextTiming.BeforeTheAttack, CardTextTiming.AfterTheAttack
                     })
            {
                var body = attack.CardTextAt(moment);
                if (body == null)
                {
                    continue;
                }

                var capabilities = body.Nodes
                    .Where(node => node.Kind == EffectNodeKind.Capability)
                    .Select(node => node.Capability.CapabilityId.Value)
                    .Distinct()
                    .ToArray();

                Assert.That(capabilities, Has.Member("synthetic-direct-move"),
                    $"The {moment} body relocates him through p.18's direct Move, which is the " +
                    "contract every other card-text move of his uses.");
                Assert.That(
                    capabilities.Where(id =>
                        id.IndexOf("movement", System.StringComparison.OrdinalIgnoreCase) >= 0),
                    Is.Empty,
                    $"and nothing in the {moment} body names the Movement-action seam - which is " +
                    "the whole of the rider's distinction, since a Movement action is what a " +
                    "movement restriction and a movement buff are both scoped to: " +
                    string.Join(", ", capabilities));
            }
        }

        // -----------------------------------------------------------------------------------------
        // The pause moved, so the snapshot seam moved with it
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// The reorder puts a pause where there was none - an Attack now stops before anything is
        /// targeted - so a capture taken there is a payload of a position the same session has to be
        /// able to read back. Both new seams are round-tripped: standing at the move offer, and
        /// standing between the move and the targeting.
        /// </summary>
        [Test]
        public void BothNewMidAttackPauses_SurviveASnapshotRoundTrip()
        {
            var atTheMove = TigerclawBoard.Create(
                tier: 2,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));
            atTheMove.StartPrimary(TigerclawCardContent.CombatReflexes);
            Assert.That(atTheMove.Choice?.NodeId.Value, Is.EqualTo("before-move"));
            atTheMove.RoundTripThroughASnapshot();

            Assert.That(atTheMove.Choice?.NodeId.Value, Is.EqualTo("before-move"),
                "The restored session is standing at the same question.");
            atTheMove.Decline();
            Assert.That(atTheMove.Choice?.NodeId.Value, Is.EqualTo("before-target"),
                "and it is answerable, not merely readable.");

            var betweenTheTwo = TigerclawBoard.Create(
                tier: 2,
                minionPlacements: StagedHeroBoard.At("red-melee-01", StagedHeroBoard.FirstAdjacent));
            betweenTheTwo.StartPrimary(TigerclawCardContent.CombatReflexes);
            betweenTheTwo.Select(StagedHeroBoard.FourthAdjacent);
            betweenTheTwo.Confirm();
            Assert.That(betweenTheTwo.Choice?.NodeId.Value, Is.EqualTo("before-target"));
            betweenTheTwo.RoundTripThroughASnapshot();

            Assert.That(betweenTheTwo.Choice?.NodeId.Value, Is.EqualTo("before-target"),
                "The move has been taken and the targeting has not, which is a state the payload " +
                "has to carry: the actor's space is no longer where the Attack began.");
            Assert.That(
                betweenTheTwo.HeroSpace(betweenTheTwo.Hero),
                Is.EqualTo(StagedHeroBoard.FourthAdjacent),
                "and the move it carried is the one that was taken.");
            betweenTheTwo.Select(betweenTheTwo.MinionAt(StagedHeroBoard.FirstAdjacent));
            betweenTheTwo.Confirm();
            Assert.That(betweenTheTwo.Session.Projection.PendingEffectChoice, Is.Null,
                "and the restored Attack resolves from there.");
        }

        // -----------------------------------------------------------------------------------------
        // The board vocabulary these cases need
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// One space from <see cref="StagedHeroBoard.SecondAdjacent"/> and two from the hero's own
        /// staged space, which is the whole geometry the post-move measurement needs: a unit the
        /// action could not have named when it started and can name once the move has been taken.
        /// </summary>
        private static readonly HexCoordinate OnlyBesideTheDestination =
            StagedHeroBoard.SecondTwoAway;

        private static int Distance(HexCoordinate first, HexCoordinate second)
        {
            var q = first.Q - second.Q;
            var r = first.R - second.R;
            return (System.Math.Abs(q) + System.Math.Abs(r) + System.Math.Abs(q + r)) / 2;
        }
    }
}
