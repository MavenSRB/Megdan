using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Rules.Modifications;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35x-moveprover: an Attack that moves before it targets is offered exactly where
    /// some space it can reach keeps a target, and greyed where none of them does.
    ///
    /// <para><b>The hole this closes was measured, not suspected.</b> Milestone 8.35z25 put Combat
    /// Reflexes and Leaping Strike into their printed reading order - move first, then "target a unit
    /// adjacent to you" - and recorded the consequence in its own delta table: <em>nothing within
    /// move-and-reach went from greyed to OFFERED</em>. That was deliberate at the time and named as
    /// debt rather than a decision, because the walk deciding the offer could not reason about the
    /// move. Milestone 8.35p had taught it to stop judging anything standing behind a Move it has not
    /// taken - the right answer for Brogan's charges, whose destination is chosen inside the card and
    /// unknowable to a walk standing before it - and that suppression is what these two faces landed
    /// in.</para>
    ///
    /// <para><b>Two standing rules decide the shape, and they point in opposite directions.</b> Rule
    /// 14, restated at milestone 8.35l: an action PROVEN empty is refused before it is offered. RULING
    /// 45: every destination of an unconstrained "you may move 1 space" stays offered while ANY of
    /// them completes the sentence after it - the player's rope covers choosing badly, not being
    /// offered the impossible. Both are satisfied by one question asked once per space he could stand
    /// on: offered where some space keeps a candidate, greyed only where every one is empty. So the
    /// merely-inconvenient board - a target reachable only if he moves the right way - is fully
    /// offered, and the hopeless board is not.</para>
    ///
    /// <para><b>What the walk still refuses to judge.</b> Brogan's charges cross a Move whose
    /// destination is bound to a choice nobody has made, so their spaces cannot be enumerated here
    /// and milestone 8.35p's suppression stands - which costs them nothing, because a charge's own
    /// text demands a destination beside an enemy and <c>choose-charge-line@1</c> stands BEFORE the
    /// Move and is judged normally. Their feasibility was already proven at the right node.</para>
    ///
    /// <para>Unity-free on purpose, so the harness and the Edit Mode assembly both run it.</para>
    /// </summary>
    public sealed class MoveThenTargetFeasibilityTests
    {
        /// <summary>
        /// Distance 3 from the staged hero: one space further than a one-space move plus a one-space
        /// reach can carry him, and a free playable space so the enemy hero really stands there.
        /// </summary>
        private static readonly HexCoordinate OutOfEveryReach = new HexCoordinate(-6, 3);

        /// <summary>The enemy seat on <see cref="TigerclawBoard"/>, by the id the staging gives it.</summary>
        private const string EnemyHero = "primitive-hero";

        /// <summary>
        /// The two enemy minions the staged position leaves two spaces out, which is inside
        /// move-and-reach and therefore has to go before a board can be called hopeless.
        /// </summary>
        private static readonly string[] TheEnemiesWithinMoveAndReach = { "red-melee-01", "red-melee-02" };

        /// <summary>
        /// THE CASE THIS MILESTONE EXISTS FOR. Every enemy three spaces away, a one-space move and a
        /// one-space reach: no destination he can take leaves anybody beside him, so the Attack is
        /// proven empty and refused before it is offered.
        ///
        /// <para>Measured at the previous revision as <c>True</c> on both faces - the offer stood, a
        /// player could take it, the move would happen and p.14's mandatory targeting would stop the
        /// action with the card spent. That is the state rule 14 forbids.</para>
        /// </summary>
        [TestCase(2, "combat-reflexes")]
        [TestCase(3, "leaping-strike")]
        public void NoDestinationKeepsATarget_SoTheAttackIsGreyed(int tier, string face)
        {
            var board = Hopeless(tier);

            Assert.That(NearestEnemy(board), Is.GreaterThan(2),
                "Every enemy has to stand further than one move plus one space of reach, or this " +
                "case is about a board the card can still act on.");
            Assert.That(board.IsPrimaryAvailable(Face(face)), Is.False,
                "No space he may move to leaves a unit adjacent to him, so the card text can bind " +
                "no target and the Attack is refused rather than offered and stopped.");
        }

        /// <summary>
        /// RULING 45's own case, and the one a prover must not break: a target he can reach only by
        /// moving the right way is a full offer, not a conditional one. Every destination stays
        /// available, including the ones that strand him.
        /// </summary>
        [TestCase(2, "combat-reflexes")]
        [TestCase(3, "leaping-strike")]
        public void SomeDestinationKeepsATarget_SoEveryDestinationIsStillOffered(int tier, string face)
        {
            var board = TigerclawBoard.Create(tier: tier);

            Assert.That(NearestEnemy(board), Is.EqualTo(2),
                "Nobody is beside him and somebody is one move plus one space away, which is the " +
                "position the correction is about.");
            Assert.That(board.IsPrimaryAvailable(Face(face)), Is.True,
                "One reachable space keeps a candidate, so the Attack is offered whole - choosing a " +
                "destination that strands him is the player's rope, not a reason to grey the card.");
        }

        /// <summary>
        /// The unmoved reading, which the prover reaches by counting standing still among the answers:
        /// declining a `you may` move is an answer, so the space he is on is one of the spaces judged.
        /// </summary>
        [TestCase(2, "combat-reflexes")]
        [TestCase(3, "leaping-strike")]
        public void SomebodyAlreadyBesideHim_IsOfferedWithoutMovingAtAll(int tier, string face)
        {
            var board = TigerclawBoard.Create(
                tier: tier,
                minionPlacements: new Dictionary<string, HexCoordinate>
                {
                    ["red-melee-01"] = StagedHeroBoard.FirstAdjacent
                });

            Assert.That(NearestEnemy(board), Is.EqualTo(1));
            Assert.That(board.IsPrimaryAvailable(Face(face)), Is.True,
                "p.10 lets him decline the step, so his own space is among the spaces the sentence " +
                "after it is judged from.");
        }

        /// <summary>
        /// Milestone 8.35l's discipline on the other kind of offer: the refusal carries a reason where
        /// a standing rule gave one and invents nothing where none did. A board with nobody in reach
        /// is geometry, not a rule anyone controls, so the greyed control says nothing new.
        /// </summary>
        [Test]
        public void AGeometricRefusal_NamesNoBlockerRatherThanInventingOne()
        {
            var offer = Hopeless(2).PrimaryOffer(TigerclawCardContent.CombatReflexes);

            Assert.That(offer.IsAvailable, Is.False);
            Assert.That(offer.Unavailability, Is.EqualTo(ActionUnavailability.None),
                "No hero's card text put those units out of reach, so there is nothing to name and " +
                "nothing is named.");
        }

        /// <summary>
        /// The bound on what this costs, measured off the shipped prompt rather than asserted.
        ///
        /// <para>The spaces the prover judges from are the Move's own destinations plus standing
        /// still, and the Move's own destinations are exactly what the player is offered when the
        /// step opens. So the live prompt IS the cost, and a hex has six neighbours: an unrestricted
        /// one-space move can offer no more than six, and the prover asks the selection's own target
        /// query at most seven times. Every printed budget in this family is one or two spaces.</para>
        /// </summary>
        [Test]
        public void TheProverJudgesFromTheMovesOwnDestinationsPlusStandingStill()
        {
            var board = TigerclawBoard.Create(tier: 2);
            board.StartPrimary(TigerclawCardContent.CombatReflexes);

            var step = board.Choice;
            Assert.That(step, Is.Not.Null, "The face opens with its own optional move.");
            Assert.That(step.NodeId.Value, Is.EqualTo("before-move"));

            var destinations = board.CandidateSpaces(step);
            Assert.That(destinations, Is.Not.Empty);
            Assert.That(destinations.Length, Is.LessThanOrEqualTo(6),
                "A hex has six neighbours, so a one-space move can offer no more than six spaces " +
                "and the prover asks the selection at most that many times plus one for standing " +
                "still.");
            Assert.That(destinations, Has.No.Member(StagedHeroBoard.HeroAt),
                "Standing still is not a destination the geometry emits - it is the answer p.10 " +
                "gives to the step itself, which is why the prover adds it rather than finding it.");
        }

        /// <summary>
        /// The family whose feasibility was already proven at the right node, and which this milestone
        /// therefore leaves exactly where it was.
        ///
        /// <para>A charge's Move names a destination the card's own <c>choose-charge-line@1</c>
        /// produced, and that node stands BEFORE the Move and is judged normally - a board with no
        /// straight destination beside an enemy still greys the Attack there. Standing before the
        /// line's answer the prover cannot enumerate where he lands, so milestone 8.35p's suppression
        /// is kept, and the charge is offered on a board an adjacency Attack cannot act on because a
        /// charge can genuinely reach it.</para>
        /// </summary>
        [Test]
        public void ACharge_KeepsTheFeasibilityItsOwnDestinationNodeAlreadyDecides()
        {
            // A fresh board per reading: taking an offer commits every seat, so one board answers
            // one question.
            Assert.That(NearestEnemy(HopelessForBrogan()), Is.GreaterThan(2));
            Assert.That(HopelessForBrogan().IsPrimaryAvailable(BroganCardContent.MadDash), Is.True,
                "Two spaces of charge plus the space it lands beside reaches an enemy three away, " +
                "so the charge is genuinely offerable and the prover must not narrow it.");
            Assert.That(HopelessForBrogan().IsPrimaryAvailable(BroganCardContent.Onslaught), Is.False,
                "The ordinary adjacency Attack on the same board is refused, which is what says the " +
                "charge's availability is about its own reach rather than about the board being easy.");
        }

        /// <summary>
        /// The other card-text-bound shape milestone 8.35p already settled: a Move with nothing between
        /// it and the typed binding. There is no actor-origin sentence to judge, so nothing here
        /// changes and Blink Strike keeps reading its own line node.
        /// </summary>
        [Test]
        public void AMoveWithNoSelectionBetweenItAndTheBinding_IsUnchanged()
        {
            Assert.That(
                TigerclawBoard.Create(
                    tier: 1,
                    minionPlacements: new Dictionary<string, HexCoordinate>
                    {
                        ["red-melee-01"] = StagedHeroBoard.FirstAdjacent
                    })
                    .IsPrimaryAvailable(TigerclawCardContent.BlinkStrike),
                Is.True);
            Assert.That(
                Hopeless(1).IsPrimaryAvailable(TigerclawCardContent.BlinkStrike),
                Is.False,
                "Its own line node refuses when no straight destination exists, exactly as before.");
        }

        // -------------------------------------------------------------------------------------------

        private static CardDefinitionId Face(string face) =>
            face == "combat-reflexes"
                ? TigerclawCardContent.CombatReflexes
                : TigerclawCardContent.LeapingStrike;

        /// <summary>Brogan on the same hopeless geometry, which his charge can still act on.</summary>
        private static StagedHeroBoard HopelessForBrogan() =>
            BroganBoard.Create(
                tier: 1,
                heroPlacements: new Dictionary<string, HexCoordinate>
                {
                    [EnemyHero] = OutOfEveryReach
                },
                banishedMinions: TheEnemiesWithinMoveAndReach);

        /// <summary>Tigerclaw with every enemy unit three or more spaces from him.</summary>
        private static StagedHeroBoard Hopeless(int tier) =>
            TigerclawBoard.Create(
                tier: tier,
                heroPlacements: new Dictionary<string, HexCoordinate>
                {
                    [EnemyHero] = OutOfEveryReach
                },
                banishedMinions: TheEnemiesWithinMoveAndReach);

        /// <summary>How far the closest enemy unit stands from the staged hero.</summary>
        private static int NearestEnemy(StagedHeroBoard board)
        {
            var standing = board.HeroSpace(board.Hero);
            var mine = board.Session.Projection.Participants.GetTeamForHero(board.Hero).Side;
            return board.Session.Projection.Minions
                .Where(minion => minion.TeamSide != mine)
                .Select(minion => HexDistanceQuery.Calculate(minion.Coordinate, standing))
                .Concat(board.Session.Projection.Heroes
                    .Where(hero => !hero.HeroId.Equals(board.Hero))
                    .Select(hero => HexDistanceQuery.Calculate(hero.Coordinate, standing)))
                .Min();
        }
    }
}
