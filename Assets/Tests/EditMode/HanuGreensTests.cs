using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71f. Hanu the Trickster's five Greens - Monkey Trick, Monkey Twist, Hear Nothing,
    /// Monkey Business and See Nothing - which print two sentences between them: "Swap two friendly
    /// units in radius" and "Swap with an enemy hero in radius", each optionally followed by "Move up
    /// to 1 space".
    ///
    /// <para><b>What the cases are about.</b> Almost every one below is a difference between two boards
    /// that changes exactly one word of a printed sentence: which side a unit is on, whether it is a
    /// hero or a minion, how far away it is, whether an item has widened the disk, and whether p.18
    /// lets Hanu reach it. p.19 makes a Swap neither movement nor placement and makes it TARGET each
    /// object, which is why immunity narrows both offers.</para>
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Hanu's is registered - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.71h deals him - so no composed match can put one of
    /// these cards in a hand and offer its Skill. <see cref="BrynnTrackingSwapTests"/> records the same
    /// boundary for the same reason, and what is proved here is everything the text itself decides:
    /// who is offered, where the pieces end up, and what the trailing move may do. The enclosing action
    /// pipeline and the seat-level Undo are 8.71h's and 8.71i's.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class HanuGreensTests
    {
        private static readonly HeroId Hanu = new HeroId("hanu-trickster");
        private static readonly HeroId Ally = new HeroId("hanu-blue-ally");
        private static readonly HeroId FarAlly = new HeroId("hanu-blue-far-ally");
        private static readonly HeroId Enemy = new HeroId("hanu-red-enemy");
        private static readonly HeroId FarEnemy = new HeroId("hanu-red-far-enemy");

        private static readonly MinionId FriendlyMinion = new MinionId("hanu-blue-minion-beside");
        private static readonly MinionId DistantFriendlyMinion = new MinionId("hanu-blue-minion-two");
        private static readonly MinionId EnemyMinion = new MinionId("hanu-red-minion-beside");

        private static readonly TeamId BlueTeam = new TeamId("hanu-blue-team");
        private static readonly TeamId RedTeam = new TeamId("hanu-red-team");
        private static readonly MapId Map = new MapId("hanu-greens-map");

        /// <summary>Where he starts every case.</summary>
        private static readonly HexCoordinate HanuAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent, his own side, a hero - the pair's first candidate on every face.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(1, -1);

        /// <summary>Adjacent, his own side, a minion - p.19's second kind of unit.</summary>
        private static readonly HexCoordinate FriendlyMinionAt = new HexCoordinate(0, -1);

        /// <summary>Two spaces out: outside Monkey Trick's Radius 1 and inside Monkey Twist's 2.</summary>
        private static readonly HexCoordinate DistantFriendlyMinionAt = new HexCoordinate(2, -2);

        /// <summary>Adjacent, and the other side.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        /// <summary>Three spaces out: inside the enemy faces' printed Radius 3 and outside every other.</summary>
        private static readonly HexCoordinate FarEnemyAt = new HexCoordinate(0, 3);

        /// <summary>Adjacent, the other side, and a minion - which "an enemy HERO" does not name.</summary>
        private static readonly HexCoordinate EnemyMinionAt = new HexCoordinate(0, 1);

        /// <summary>Four spaces out: outside every Radius any face of his prints.</summary>
        private static readonly HexCoordinate FarAllyAt = new HexCoordinate(0, 4);

        /// <summary>
        /// The five faces THIS block composes, named rather than read off
        /// <see cref="HanuCardContent.AllDefinitions"/> - that aggregate holds only these five while
        /// the wave is running and will hold all nineteen once 8.71g folds the blocks, so a case that
        /// enumerated it would silently start asserting over faces this fixture owns nothing of.
        /// </summary>
        private static readonly CardDefinitionId[] GreenFaces =
        {
            HanuCardContent.MonkeyTrick, HanuCardContent.MonkeyTwist, HanuCardContent.HearNothing,
            HanuCardContent.MonkeyBusiness, HanuCardContent.SeeNothing
        };

        // -------------------------------------------------------------------------------------------
        // "Swap two friendly units in radius"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE OFFER. One board, one question naming two pieces, and every word of the printed
        /// sentence doing its own work: "friendly" takes his own side and leaves the enemy hero and the
        /// enemy minion out, "units" admits the hero and the minion alike, and "in radius" is Monkey
        /// Trick's printed 1, which the friendly minion two spaces out is outside of.
        /// </summary>
        [Test]
        public void MonkeyTrick_AsksOneQuestionOverTheFriendlyUnitsInsideItsPrintedRadius()
        {
            var world = World.Green(HanuCardContent.MonkeyTrick);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[] { Ally.Value, FriendlyMinion.Value }),
                "The friendly hero and the friendly minion standing beside him, heroes before " +
                "minions - an offer made in a derived order would replay differently than it was " +
                "answered.");
            Assert.That(choice.MinimumSelections, Is.EqualTo(2));
            Assert.That(choice.MaximumSelections, Is.EqualTo(2),
                "\"two friendly units\" and p.19's exchange moves both, so an answer naming one is " +
                "no answer at all rather than a smaller one.");
            Assert.That(choice.ChooserId, Is.EqualTo(Hanu),
                "\"Swap ...\" is his own step, so the decision is his (p.10).");
        }

        /// <summary>
        /// p.17's Friendly, stated as the candidate that is missing: "another unit on your team. It
        /// never includes you". Measured on the face whose Radius 1 disk he is the centre of, so his
        /// absence cannot be about distance.
        /// </summary>
        [Test]
        public void ThePair_NeverNamesHanuHimself()
        {
            var world = World.Green(HanuCardContent.MonkeyTwist);

            Assert.That(Named(world.RunToPause()), Has.No.Member(Hanu.Value),
                "p.17's \"Friendly\" excludes the acting hero, and p.10 affects him only where the " +
                "text says so - HA-Q10.");
        }

        /// <summary>
        /// "Friendly" is a side as well as an exclusion: the enemy hero and the enemy minion standing
        /// as close as the two candidates are never offered.
        /// </summary>
        [Test]
        public void ThePair_ReachesHisOwnSideOnly()
        {
            var named = Named(World.Green(HanuCardContent.MonkeyTwist).RunToPause());

            Assert.That(named, Has.No.Member(Enemy.Value));
            Assert.That(named, Has.No.Member(EnemyMinion.Value),
                "Both stand adjacent to him, so their absence is the word \"friendly\" rather than " +
                "the disk.");
        }

        /// <summary>
        /// The disk is p.9's printed icon: the same board offers a third candidate through Monkey
        /// Twist's Radius 2 and does not through Monkey Trick's 1.
        /// </summary>
        [Test]
        public void ThePair_IsBoundedByThePrintedIcon_SoMonkeyTwistReachesWhatMonkeyTrickCannot()
        {
            Assert.That(
                HexDistanceQuery.Calculate(HanuAt, DistantFriendlyMinionAt), Is.EqualTo(2));

            Assert.That(
                Named(World.Green(HanuCardContent.MonkeyTrick).RunToPause()),
                Has.No.Member(DistantFriendlyMinion.Value),
                "Two spaces out is outside Monkey Trick's printed Radius 1.");
            Assert.That(
                Named(World.Green(HanuCardContent.MonkeyTwist).RunToPause()),
                Has.Member(DistantFriendlyMinion.Value),
                "and Monkey Twist prints Radius 2, which reaches it - the same sentence read " +
                "through a different icon.");
        }

        /// <summary>
        /// HA-Q13f's SECOND MEASUREMENT, at the offer. p.16's Radius item raises the icon the card
        /// prints, so the identical Monkey Trick reaches a unit two spaces away once the item is
        /// earned - which is the composition a literal distance could not have expressed.
        /// </summary>
        [Test]
        public void ThePair_ReadsThePrintedRadiusWithItsItem()
        {
            var earned = World.Green(HanuCardContent.MonkeyTrick, radiusItems: 1);

            Assert.That(Named(earned.RunToPause()), Has.Member(DistantFriendlyMinion.Value),
                "Monkey Trick prints Radius 1 and the item makes the disk 2, which is the whole " +
                "difference from the case above - p.16 raises the printed value and the selection " +
                "reads the raised one.");
        }

        /// <summary>
        /// p.18's immunity narrows the pair, because p.19 makes a Swap a TARGETING of each object. The
        /// same board with the friendly hero immune offers the two minions instead - so the sentence
        /// still has an answer and the immune unit is simply not in it.
        /// </summary>
        [Test]
        public void AnImmuneFriendlyUnit_IsNeverAmongThePairsCandidates()
        {
            var world = World.Green(HanuCardContent.MonkeyTwist, immuneHero: Ally);

            var named = Named(world.RunToPause());

            Assert.That(named, Has.No.Member(Ally.Value),
                "\"you cannot target that unit\" (p.18), and the narrowing is at the offer rather " +
                "than at the exchange - 8.35l's rule.");
            Assert.That(named,
                Is.EqualTo(new[] { FriendlyMinion.Value, DistantFriendlyMinion.Value }),
                "while the two friendly minions in the disk are still a pair he may name.");
        }

        /// <summary>
        /// ONE admissible friendly unit is no pair at all: p.10's impossible mandatory step, so the
        /// action performs nothing and nothing pauses. This is what greys the primary at 8.70's channel
        /// rather than offering an answer that cannot be completed.
        /// </summary>
        [Test]
        public void OneFriendlyUnitInRadius_RefusesTheWholeSkill()
        {
            var world = World.Green(HanuCardContent.MonkeyTrick, onlyOneFriendlyUnit: true);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: the step that could have asked cannot be taken.");
            Assert.That(world.Where(Hanu), Is.EqualTo(HanuAt));
            Assert.That(world.Where(Ally), Is.EqualTo(AllyAt),
                "and the one unit the disk does hold has not been moved, because a Swap moves two.");
        }

        /// <summary>
        /// The exchange itself: the two units named end on each other's spaces and Hanu does not move,
        /// because he is not one of the two pieces the text names.
        /// </summary>
        [Test]
        public void MonkeyTwist_ExchangesTheTwoUnitsNamed_AndLeavesHanuWhereHeStood()
        {
            var world = World.Green(HanuCardContent.MonkeyTwist);

            world.RunToPause();
            var result = world.SelectAndConfirmToCompletion(Ally, FriendlyMinion);

            Assert.That(result.IsComplete, Is.True);
            Assert.That(world.Where(Ally), Is.EqualTo(FriendlyMinionAt));
            Assert.That(world.Where(FriendlyMinion), Is.EqualTo(AllyAt));
            Assert.That(world.Where(Hanu), Is.EqualTo(HanuAt),
                "p.19's Swap exchanges the two objects it names and nobody else.");
        }

        /// <summary>
        /// The pair is UNORDERED (HA-Q10): naming the minion first and the hero first are one act, so
        /// the two boards end identically. Sabina's Listen Up reading, measured rather than asserted.
        /// </summary>
        [Test]
        public void ThePair_IsUnordered_SoEitherOrderOfTheSameTwoUnitsIsOneAnswer()
        {
            var heroFirst = World.Green(HanuCardContent.MonkeyTwist);
            heroFirst.RunToPause();
            heroFirst.SelectAndConfirmToCompletion(Ally, FriendlyMinion);

            var minionFirst = World.Green(HanuCardContent.MonkeyTwist);
            minionFirst.RunToPause();
            minionFirst.SelectAndConfirmToCompletion(FriendlyMinion, Ally);

            Assert.That(minionFirst.Where(Ally), Is.EqualTo(heroFirst.Where(Ally)));
            Assert.That(
                minionFirst.Where(FriendlyMinion), Is.EqualTo(heroFirst.Where(FriendlyMinion)),
                "Swapping A with B and B with A are the same act, so the answer is canonicalised " +
                "before the exchange reads it.");
        }

        /// <summary>
        /// Monkey Business's second sentence: "Move up to 1 space", offered from where HE stands - the
        /// pair moved two other units and never moved him - with his own space among the answers,
        /// which is what "up to" admits at its floor (p.11, Garrus's GA-Q1).
        /// </summary>
        [Test]
        public void MonkeyBusiness_OffersItsTrailingMoveFromHisOwnSpace_WithZeroAllowed()
        {
            var world = World.Green(HanuCardContent.MonkeyBusiness);

            world.RunToPause();
            var move = world.SelectAndConfirmToPause(Ally, FriendlyMinion);

            Assert.That(move.NodeId.Value, Is.EqualTo(HanuEffectContent.FriendlySwapMoveNode));
            Assert.That(Spaces(move), Has.Member(HanuAt),
                "Standing still is one of the answers \"up to 1\" admits, and his own space is " +
                "where the sentence starts because the swap moved two OTHER units.");
            Assert.That(
                Spaces(move).All(space => HexDistanceQuery.Calculate(HanuAt, space) <= 1), Is.True,
                "and nothing further than one space is offered.");

            var destination = Spaces(move)
                .First(space => HexDistanceQuery.Calculate(HanuAt, space) == 1);
            world.AnswerToCompletion(move, Pick(move, destination));
            Assert.That(world.Where(Hanu), Is.EqualTo(destination));
        }

        /// <summary>
        /// The two faces that print no second sentence compose no move node at all - so the clause is
        /// content rather than a policy the other three share and decline.
        /// </summary>
        [Test]
        public void TheFacesPrintingNoSecondSentence_ComposeNoTrailingMove()
        {
            foreach (var id in new[] { HanuCardContent.MonkeyTrick, HanuCardContent.MonkeyTwist })
            {
                Assert.That(NodeIds(id), Has.No.Member(HanuEffectContent.FriendlySwapMoveNode),
                    id.Value + " prints one sentence.");
            }

            Assert.That(
                NodeIds(HanuCardContent.MonkeyBusiness),
                Has.Member(HanuEffectContent.FriendlySwapMoveNode));
            Assert.That(
                NodeIds(HanuCardContent.HearNothing),
                Has.No.Member(HanuEffectContent.EnemySwapMoveNode));
            Assert.That(
                NodeIds(HanuCardContent.SeeNothing),
                Has.Member(HanuEffectContent.EnemySwapMoveNode));
        }

        /// <summary>
        /// Monkey Business's trailing move is never reached after an impossible pair - because what
        /// p.10 stops is the ACTION rather than the one step, and the move is a later sibling of a
        /// mandatory step that could not be taken.
        /// </summary>
        [Test]
        public void MonkeyBusiness_NeverReachesItsMoveAfterAnImpossiblePair()
        {
            var stopped = World.Green(
                HanuCardContent.MonkeyBusiness, onlyOneFriendlyUnit: true);

            Assert.That(stopped.Run().IsComplete, Is.True);
            Assert.That(stopped.Where(Hanu), Is.EqualTo(HanuAt),
                "and he has not moved a space, which is the whole consequence of the reading.");

            var performed = World.Green(HanuCardContent.MonkeyBusiness);
            performed.RunToPause();
            Assert.That(
                performed.SelectAndConfirmToPause(Ally, FriendlyMinion).NodeId.Value,
                Is.EqualTo(HanuEffectContent.FriendlySwapMoveNode),
                "while the identical face on a board that holds a pair does reach the move - so " +
                "the silence above is p.10 rather than a missing node.");
        }

        // -------------------------------------------------------------------------------------------
        // "Swap with an enemy hero in radius"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The second sentence's offer: the enemy HEROES inside the printed Radius 3 and nothing else -
        /// not his own side, and not the enemy minion standing beside him, because the noun is "hero".
        /// </summary>
        [Test]
        public void HearNothing_OffersTheEnemyHeroesInsideItsPrintedRadius()
        {
            var world = World.Green(HanuCardContent.HearNothing);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EquivalentTo(new[] { Enemy.Value, FarEnemy.Value }));
            Assert.That(Named(choice), Has.No.Member(EnemyMinion.Value),
                "An enemy MINION adjacent to him is inside the disk and outside the sentence.");
            Assert.That(Named(choice), Has.No.Member(Ally.Value),
                "and his own side is the other half of \"enemy\".");
            Assert.That(choice.ChooserId, Is.EqualTo(Hanu));
        }

        /// <summary>
        /// p.18's immunity narrows this offer too, and a board where every enemy hero in the disk is
        /// immune performs nothing at all rather than offering a swap the exchange would refuse.
        /// </summary>
        [Test]
        public void AnImmuneEnemyHero_IsNeverOffered_AndAllOfThemImmuneRefusesTheSkill()
        {
            var one = World.Green(HanuCardContent.HearNothing, immuneHero: FarEnemy);

            Assert.That(Named(one.RunToPause()), Is.EqualTo(new[] { Enemy.Value }),
                "The immune hero is out of the offer and the other is still in it.");

            var all = World.Green(
                HanuCardContent.HearNothing, immuneHero: FarEnemy, secondImmuneHero: Enemy);

            Assert.That(all.Run().IsComplete, Is.True,
                "With both immune the sentence names nobody, which is p.10's impossible mandatory " +
                "step rather than an empty prompt.");
            Assert.That(all.Where(Hanu), Is.EqualTo(HanuAt));
        }

        /// <summary>
        /// The exchange: Hanu and the chosen enemy hero end on each other's spaces, and nobody else
        /// moves.
        /// </summary>
        [Test]
        public void HearNothing_ExchangesHimWithTheEnemyHeroHeNamed()
        {
            var world = World.Green(HanuCardContent.HearNothing);

            var choice = world.RunToPause();
            world.AnswerToCompletion(choice, Pick(choice, FarEnemy));

            Assert.That(world.Where(Hanu), Is.EqualTo(FarEnemyAt),
                "A Swap is an exchange rather than a move: he ends where the hero stood, three " +
                "spaces away, and p.19 makes nothing about the route matter.");
            Assert.That(world.Where(FarEnemy), Is.EqualTo(HanuAt));
            Assert.That(world.Where(Enemy), Is.EqualTo(EnemyAt),
                "and the enemy hero he did not name has not moved.");
        }

        /// <summary>
        /// THE BINDING THAT MATTERS. See Nothing's "Move up to 1 space" starts from the space the
        /// EXCHANGE left him on, three spaces from where he began - Brynn's BR-Q20d reading, and the
        /// difference between binding the Swap's own output and asking the board about a hero who has
        /// since been moved.
        /// </summary>
        [Test]
        public void SeeNothing_OffersItsTrailingMoveFromThePostSwapSpace()
        {
            var world = World.Green(HanuCardContent.SeeNothing);

            var choice = world.RunToPause();
            var move = world.Answer(choice, Pick(choice, FarEnemy));

            Assert.That(move.NodeId.Value, Is.EqualTo(HanuEffectContent.EnemySwapMoveNode));
            Assert.That(Spaces(move), Has.Member(FarEnemyAt),
                "Standing still is admitted, and the space he is standing still ON is the one the " +
                "swap gave him.");
            Assert.That(
                Spaces(move).All(space => HexDistanceQuery.Calculate(FarEnemyAt, space) <= 1),
                Is.True,
                "Every destination is within one space of where the exchange left him, and none is " +
                "within one space of where he started.");

            var destination = Spaces(move)
                .First(space => HexDistanceQuery.Calculate(FarEnemyAt, space) == 1);
            world.AnswerToCompletion(move, Pick(move, destination));
            Assert.That(world.Where(Hanu), Is.EqualTo(destination));
            Assert.That(world.Where(FarEnemy), Is.EqualTo(HanuAt),
                "and the hero he swapped with is still on the space the exchange gave HIM - the " +
                "move is his own and moves nobody else.");
        }

        // -------------------------------------------------------------------------------------------
        // Restore, replay and a stale answer
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A restore taken while the trailing move is pending resumes against freshly rebuilt content
        /// and finishes the same sentence - which is what a snapshot carries and what the resolver has
        /// to be able to resume from (milestone 8.35i).
        /// </summary>
        [Test]
        public void MonkeyBusiness_ResumesThroughARestoreWhileTheTrailingMoveIsPending()
        {
            var world = World.Green(HanuCardContent.MonkeyBusiness);

            world.RunToPause();
            var move = world.SelectAndConfirmToPause(Ally, FriendlyMinion);

            world.RebindTrustedContent();

            var destination = Spaces(move)
                .First(space => HexDistanceQuery.Calculate(HanuAt, space) == 1);
            world.AnswerToCompletion(move, Pick(move, destination));

            Assert.That(world.Where(Hanu), Is.EqualTo(destination));
            Assert.That(world.Where(Ally), Is.EqualTo(FriendlyMinionAt),
                "and the exchange the frame had already performed is still on the board, because " +
                "what a restore rebuilds is the trusted content rather than the state.");
        }

        /// <summary>
        /// THE REWIND PROPERTY. A retained-prefix Undo replays the same commands against the same
        /// staged state, so two independent walks answered identically must end identically - which is
        /// what makes both sentences safe to rewind through.
        /// </summary>
        [Test]
        public void BothSentences_ReplayIdenticallyFromTheSameStagedState()
        {
            AssertReplays(WalkMonkeyBusiness);
            AssertReplays(WalkSeeNothing);
        }

        /// <summary>
        /// The candidates are re-derived when the answer arrives rather than trusted from the moment
        /// the question opened, so a unit that was never offered has no invocation to forge.
        /// </summary>
        [Test]
        public void TheOffers_CarryNoInvocationForAUnitTheyNeverNamed()
        {
            var pair = World.Green(HanuCardContent.MonkeyTrick).RunToPause();
            Assert.That(
                pair.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                    invocation.Candidate.Value.HeroId.Equals(Enemy)),
                Is.False,
                "The enemy hero standing beside him is not a friendly unit, so there is no " +
                "invocation naming him - which is what makes a forged answer unanswerable rather " +
                "than merely wrong.");

            var subject = World.Green(HanuCardContent.HearNothing).RunToPause();
            Assert.That(
                subject.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                    invocation.Candidate.Value.MinionId.Equals(EnemyMinion)),
                Is.False,
                "and the enemy minion is not an enemy hero.");
        }

        // -------------------------------------------------------------------------------------------
        // The contract, and what it deliberately is not
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HA-Q13f, MEASURED. <c>choose-target-pair@1</c> is the shipped pair selection and its grammar
        /// is this sentence's - so reuse-first points straight at it. Two words of the printed sentence
        /// are why it is still the wrong contract, and both are read off the shipped handler's own
        /// declaration rather than asserted: its candidates are MINIONS (no unit-kind parameter exists
        /// to widen, because Sabina's three faces all print "two minions"), and its reach is a required
        /// <c>distance</c> INTEGER a definition writes down rather than the card's printed icon.
        /// </summary>
        [Test]
        public void ThePairContract_IsHisOwnBecauseTheSentenceNamesUnitsAtThePrintedRadius()
        {
            var shipped = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticRosterCapabilities.ChooseTargetPairV1);
            var parameters = shipped.Schema.Parameters.Select(parameter => parameter.Name).ToArray();

            Assert.That(parameters, Has.No.Member(SyntheticRosterCapabilities.UnitKindParameter),
                "The shipped pair declares no unit kind at all - it derives minions, written into " +
                "the handler - so \"two friendly UNITS\" could never name a teammate's hero on it.");
            Assert.That(parameters, Has.Member(SyntheticRosterCapabilities.DistanceParameter),
                "and its reach is a number the definition carries,");
            Assert.That(
                shipped.Schema.Parameters
                    .Single(parameter =>
                        parameter.Name == SyntheticRosterCapabilities.DistanceParameter)
                    .IsRequired,
                Is.True,
                "required, which is the icon his Greens print and his Radius item raises.");

            Assert.That(
                PairNode(HanuCardContent.MonkeyTrick).Capability,
                Is.EqualTo(HanuComponentContent.FriendlyUnitPairV1),
                "so the sentence composes on his own selection, which reads the printed Radius " +
                "itself through the same query every printed-icon consumer reads.");
            Assert.That(PairNode(HanuCardContent.MonkeyTrick).Inputs, Is.Empty,
                "and it declares no parameters, because all three faces print the identical words.");
        }

        /// <summary>
        /// The enemy sentence reuses the shipped contracts whole - the printed-Radius hero selection
        /// and the actor-fixed Swap - which is the reuse-first half of the same judgement.
        /// </summary>
        [Test]
        public void TheEnemySentence_ComposesOnTheShippedRadiusSelectionAndActorSwap()
        {
            var body = Body(HanuCardContent.HearNothing);

            Assert.That(
                body.Nodes.Single(node =>
                    node.Id.Value == HanuEffectContent.EnemySwapSubjectNode).Capability,
                Is.EqualTo(SyntheticTargetingCapabilities.RadiusHeroTargetV1));
            Assert.That(
                body.Nodes.Single(node =>
                    node.Id.Value == HanuEffectContent.EnemySwapExchangeNode).Capability,
                Is.EqualTo(SyntheticExchangeCapabilities.SwapWithActorV1),
                "The actor is in the frame, so the walk that judges the option asks the same " +
                "question the choice will ask - which is why a hero is never offered whose Swap " +
                "would then be refused (8.35l).");
        }

        /// <summary>
        /// Every move either sentence composes is p.11's card-text move at
        /// <see cref="EffectFailurePolicy.UpTo"/> - a mandatory step whose DISTANCE is the decision,
        /// with zero admitted, which is not a wording that can also be declined.
        /// </summary>
        [Test]
        public void BothTrailingMoves_AreUpToRatherThanOptionalSteps()
        {
            Assert.That(
                MoveNode(HanuCardContent.MonkeyBusiness, HanuEffectContent.FriendlySwapMoveNode)
                    .FailurePolicy,
                Is.EqualTo(EffectFailurePolicy.UpTo));
            Assert.That(
                MoveNode(HanuCardContent.SeeNothing, HanuEffectContent.EnemySwapMoveNode)
                    .FailurePolicy,
                Is.EqualTo(EffectFailurePolicy.UpTo));
        }

        /// <summary>
        /// The five faces as his shard prints them - values, icons, items and both forks of p.15's
        /// Green ladder. Transcription rather than derivation, so the table and these rows are meant to
        /// be read side by side.
        /// </summary>
        [Test]
        public void TheFiveFaces_CarryTheValuesIconsAndLadderTheShardRecords()
        {
            AssertFace(HanuCardContent.MonkeyTrick, "Monkey Trick", CardTier.I, 2, 1, 2, 1,
                PrintedValueChannel.None,
                new[] { HanuCardContent.MonkeyTwist, HanuCardContent.HearNothing });
            AssertFace(HanuCardContent.MonkeyTwist, "Monkey Twist", CardTier.II, 2, 2, 2, 2,
                PrintedValueChannel.Defense,
                new[] { HanuCardContent.MonkeyBusiness, HanuCardContent.SeeNothing });
            AssertFace(HanuCardContent.HearNothing, "Hear Nothing", CardTier.II, 2, 2, 2, 3,
                PrintedValueChannel.Attack,
                new[] { HanuCardContent.MonkeyBusiness, HanuCardContent.SeeNothing });
            AssertFace(HanuCardContent.MonkeyBusiness, "Monkey Business", CardTier.III, 1, 2, 2, 2,
                PrintedValueChannel.Defense, Array.Empty<CardDefinitionId>());
            AssertFace(HanuCardContent.SeeNothing, "See Nothing", CardTier.III, 1, 2, 2, 3,
                PrintedValueChannel.Range, Array.Empty<CardDefinitionId>());

            Assert.That(Face(HanuCardContent.MonkeyTrick).SourceContent.PrintedText,
                Is.EqualTo("Swap two friendly units in radius."));
            Assert.That(Face(HanuCardContent.SeeNothing).SourceContent.PrintedText,
                Is.EqualTo("Swap with an enemy hero in radius. Move up to 1 space."),
                "and the second sentence is printed after the first on the two Tier III faces.");
        }

        /// <summary>
        /// The five Greens declare NO decision label, and the set is DERIVED rather than asserted:
        /// <see cref="DecisionLabelRequirementQuery"/> is the resolver's own rule about which controls
        /// owe words - the arms of a <c>Choose</c> and the nodes that open p.10's confirm-or-decline -
        /// and none of these five bodies contains either. Both trailing moves pause to name a
        /// DESTINATION, which is a selection rather than a confirm-or-decline.
        /// </summary>
        [Test]
        public void TheGreens_OweNoDecisionLabels_AndDeclareNone()
        {
            var required = GreenFaces
                .Select(Face)
                .SelectMany(face => DecisionLabelRequirementQuery.For(face))
                .ToArray();

            Assert.That(required, Is.Empty,
                "No Green of his raises a control that owes words - a face that starts asking " +
                "something would appear here and the declaration below would have to grow.");
            Assert.That(HanuEffectContent.GreenDecisionLabels(), Is.Empty,
                "so the block declares none, which is Brynn's swaps' and Silverarrow's whole set's " +
                "reading of the same shape.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 7.9's gate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The one selection this block mints is DEFERRED, asserted from BOTH sides: it is in his
        /// block's deferral list, his block's registered list is empty, and the catalog a configured
        /// match loads cannot start it. Registering a contract before dealt content can reach it moves
        /// the pinned capability list, every snapshot's bytes and every corpus digest while the content
        /// never moved - which is why 8.71h is the block that flips it.
        /// </summary>
        [Test]
        public void TheFriendlyPairSelection_IsDeferredAndUnreachableFromAConfiguredMatch()
        {
            Assert.That(HanuComponentContent.GreenRegisteredHandlers(), Is.Empty,
                "Nothing of his is registered until the dealing block.");
            Assert.That(
                HanuComponentContent.GreenDeferredHandlers()
                    .Select(handler => handler.Reference.ToString()).ToArray(),
                Is.EqualTo(new[] { HanuComponentContent.FriendlyUnitPairV1.ToString() }));

            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities
                    .IsRegistered(HanuComponentContent.FriendlyUnitPairV1),
                Is.False,
                "'hanu-friendly-unit-pair@1' is reachable from no dealt card in this build.");

            // MILESTONE 8.71g STATED HIS SET: eighteen dealt faces, and still nothing registered.
            Assert.That(HanuCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "his set is stated; what 8.71h still owes is the registration above.");
            foreach (var id in GreenFaces)
            {
                Assert.That(
                    HanuCardContent.AllDefinitions.Select(face => face.Id.Value).ToArray(),
                    Has.Member(id.Value),
                    "while the five faces this block composes are visible to every content sweep " +
                    "that reads the aggregate.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private sealed class Replay
        {
            internal string[] Offer;
            internal HexCoordinate[] Destinations;
            internal HexCoordinate HanuAt;
            internal HexCoordinate PartnerAt;
        }

        private static void AssertReplays(Func<Replay> walk)
        {
            var first = walk();
            var second = walk();

            Assert.That(second.Offer, Is.EqualTo(first.Offer),
                "The same board offers the same candidates in the same order.");
            Assert.That(second.Destinations, Is.EqualTo(first.Destinations),
                "the same answer reaches the same trailing move,");
            Assert.That(second.HanuAt, Is.EqualTo(first.HanuAt));
            Assert.That(second.PartnerAt, Is.EqualTo(first.PartnerAt),
                "and the pieces end in the same spaces.");
        }

        /// <summary>One whole walk of Monkey Business, answered the same way every time.</summary>
        private static Replay WalkMonkeyBusiness()
        {
            var world = World.Green(HanuCardContent.MonkeyBusiness);
            var offer = Named(world.RunToPause());
            var move = world.SelectAndConfirmToPause(Ally, FriendlyMinion);
            var destinations = Ordered(Spaces(move));
            var destination = destinations
                .First(space => HexDistanceQuery.Calculate(HanuAt, space) == 1);
            world.AnswerToCompletion(move, Pick(move, destination));

            return new Replay
            {
                Offer = offer,
                Destinations = destinations,
                HanuAt = world.Where(Hanu),
                PartnerAt = world.Where(Ally)
            };
        }

        /// <summary>One whole walk of See Nothing, answered the same way every time.</summary>
        private static Replay WalkSeeNothing()
        {
            var world = World.Green(HanuCardContent.SeeNothing);
            var choice = world.RunToPause();
            var offer = Named(choice);
            var move = world.Answer(choice, Pick(choice, FarEnemy));
            var destinations = Ordered(Spaces(move));
            var destination = destinations
                .First(space => HexDistanceQuery.Calculate(FarEnemyAt, space) == 1);
            world.AnswerToCompletion(move, Pick(move, destination));

            return new Replay
            {
                Offer = offer,
                Destinations = destinations,
                HanuAt = world.Where(Hanu),
                PartnerAt = world.Where(FarEnemy)
            };
        }

        private static HexCoordinate[] Ordered(IEnumerable<HexCoordinate> spaces) =>
            spaces.OrderBy(space => space.Q).ThenBy(space => space.R).ToArray();

        private static void AssertFace(
            CardDefinitionId id,
            string displayName,
            CardTier tier,
            int initiative,
            int defense,
            int movement,
            int radius,
            PrintedValueChannel itemIcon,
            IReadOnlyList<CardDefinitionId> nextTier)
        {
            var face = Face(id);

            Assert.That(face.DisplayName, Is.EqualTo(displayName));
            Assert.That(face.Initiative.Value, Is.EqualTo(initiative));
            Assert.That(face.Progression.Color, Is.EqualTo(CardColor.Green));
            Assert.That(face.Progression.Tier, Is.EqualTo(tier));
            Assert.That(face.Progression.ItemIcon, Is.EqualTo(itemIcon));
            Assert.That(
                face.Progression.NextTierCandidateIds.Select(candidate => candidate.Value).ToArray(),
                Is.EqualTo(nextTier.Select(candidate => candidate.Value).ToArray()));
            Assert.That(Icon(face, ActionKind.Skill, PrintedValueChannel.Radius), Is.EqualTo(radius));
            Assert.That(Icon(face, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(defense));
            Assert.That(Icon(face, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(movement));
            Assert.That(Icon(face, ActionKind.Skill, PrintedValueChannel.Range), Is.Zero,
                "A missing icon is absent, never zero - the Greens print no Range (HA-06).");
        }

        private static string[] NodeIds(CardDefinitionId cardId) =>
            Body(cardId).Nodes.Select(node => node.Id.Value).ToArray();

        private static EffectNodeDefinition PairNode(CardDefinitionId cardId) =>
            Body(cardId).Nodes.Single(node =>
                node.Id.Value == HanuEffectContent.FriendlySwapPairNode);

        private static EffectNodeDefinition MoveNode(CardDefinitionId cardId, string nodeId) =>
            Body(cardId).Nodes.Single(node => node.Id.Value == nodeId);

        private static EffectDefinition Body(CardDefinitionId cardId) =>
            Face(cardId).ActionDefinitions
                .Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot;

        private static CardDefinition Face(CardDefinitionId cardId) =>
            HanuCardContent.AllDefinitions.Single(definition => definition.Id.Equals(cardId));

        private static int Icon(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Where(action => action.Kind == kind)
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .Select(value => value.Amount)
                .SingleOrDefault();

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates
                .Select(candidate => candidate.Value.Kind == EffectValueKind.Hero
                    ? candidate.Value.HeroId.Value
                    : candidate.Value.MinionId.Value)
                .ToArray();

        private static HexCoordinate[] Spaces(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Coordinate)
                .Select(candidate => candidate.Value.Coordinate)
                .ToArray();

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HeroId hero) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                invocation.Candidate.Value.HeroId.Equals(hero));

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, MinionId minion) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Minion &&
                invocation.Candidate.Value.MinionId.Equals(minion));

        private static EffectChoiceCommand Pick(PendingEffectChoice choice, HexCoordinate space) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                invocation.Candidate.Value.Coordinate.Equals(space));

        /// <summary>
        /// Five seats: three Blue, so "two friendly units" can be two heroes or a hero and a minion,
        /// and two Red, so one enemy hero can stand beside him and one at the edge of the printed disk.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var hanuPlayer = new PlayerId("hanu-player");
            var allyPlayer = new PlayerId("hanu-ally-player");
            var farAllyPlayer = new PlayerId("hanu-far-ally-player");
            var enemyPlayer = new PlayerId("hanu-enemy-player");
            var farEnemyPlayer = new PlayerId("hanu-far-enemy-player");

            return new MatchParticipants(
                new[]
                {
                    new Team(BlueTeam, TeamSide.Blue),
                    new Team(RedTeam, TeamSide.Red)
                },
                new[]
                {
                    new Player(hanuPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(farAllyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(farEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Hanu, hanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(FarAlly, farAllyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(FarEnemy, farEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// One staged board and the frame walking one of his Green Skill bodies over it.
        ///
        /// <para>Nothing here is a configured match: see the fixture's own remarks for why, and for
        /// who owns the part this cannot reach.</para>
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private readonly HeroItemState items;
            private EffectResolutionContext context;
            private PendingResolution frame;

            private World(
                CardDefinitionId cardId,
                IReadOnlyList<CardDefinition> definitions,
                IReadOnlyList<CardInstance> instances,
                MatchParticipants participants,
                MatchComponents components,
                RuleModificationSet modifications,
                HeroItemState items)
            {
                this.cardId = cardId;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.components = components;
                this.modifications = modifications;
                this.items = items;
                context = BuildContext();
            }

            /// <summary>
            /// The Greens board: a friendly hero and a friendly minion beside him, a second friendly
            /// minion two spaces out, an enemy hero and an enemy minion beside him, a second enemy hero
            /// three spaces out, and a friendly hero four spaces out that no Radius of his reaches.
            /// </summary>
            internal static World Green(
                CardDefinitionId cardId,
                bool onlyOneFriendlyUnit = false,
                int radiusItems = 0,
                HeroId immuneHero = default,
                HeroId secondImmuneHero = default)
            {
                var participants = Participants();

                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Hanu, HanuAt),
                    new HeroOccupancy(Ally, AllyAt),
                    new HeroOccupancy(FarAlly, FarAllyAt),
                    new HeroOccupancy(Enemy, EnemyAt),
                    new HeroOccupancy(FarEnemy, FarEnemyAt)
                };

                var minions = new List<(MinionId Id, HexCoordinate At, TeamId Team, TeamSide Side)>
                {
                    (EnemyMinion, EnemyMinionAt, RedTeam, TeamSide.Red)
                };

                if (!onlyOneFriendlyUnit)
                {
                    // The friendly minions are what make the pair answerable; taking them off the
                    // board leaves exactly one friendly unit inside every Green disk.
                    minions.Add((FriendlyMinion, FriendlyMinionAt, BlueTeam, TeamSide.Blue));
                    minions.Add((DistantFriendlyMinion, DistantFriendlyMinionAt, BlueTeam,
                        TeamSide.Blue));
                }

                var board = Board(heroes, minions);
                var rules = Rules(participants, immuneHero, secondImmuneHero);

                return new World(
                    cardId,
                    HanuCardContent.AllDefinitions.ToArray(),
                    Array.Empty<CardInstance>(),
                    participants,
                    new MatchComponents(
                        SyntheticComponentDefinitions.CreateCatalog(),
                        board,
                        new MarkerAttachmentRegister(),
                        board,
                        board,
                        board,
                        board),
                    rules,
                    RadiusItems(participants, radiusItems));
            }

            private static AdjacentMoveAuthority Board(
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At, TeamId Team, TeamSide Side)> minions)
            {
                var zone = new ZoneId("hanu-greens-zone");
                var spaces = new List<BoardMapSpace>();
                for (var q = -8; q <= 8; q++)
                {
                    for (var r = -8; r <= 8; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, HanuAt) > 8)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, zone));
                    }
                }

                return new AdjacentMoveAuthority(
                    new BoardMapDefinition(
                        Map, 1, "Hanu Greens Map",
                        spaces,
                        new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                        Array.Empty<BoardMapSpawnPoint>(),
                        Array.Empty<ZoneId>()),
                    heroes,
                    Array.Empty<ObstacleOccupancy>(),
                    minions
                        .Select(minion => new QuickGameMinionSetupState(
                            minion.Id,
                            minion.Team,
                            minion.Side,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(minion.At)),
                            minion.At))
                        .ToArray());
            }

            private static HeroItemState RadiusItems(MatchParticipants participants, int count) =>
                count <= 0
                    ? new HeroItemState(participants)
                    : HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                    {
                        new HeroItem(
                            Hanu,
                            new CardInstanceId("hanu-radius-item"),
                            HeroDashboard.RulebookLayout.Slots[3].Position,
                            PrintedValueChannel.Radius,
                            count)
                    }));

            private static RuleModificationSet Rules(
                MatchParticipants participants, HeroId first, HeroId second)
            {
                if (!first.IsDefined)
                {
                    return RuleModificationSet.Empty;
                }

                var timing = new ActiveEffectLifecycle();
                Arm(timing, first);
                if (second.IsDefined)
                {
                    Arm(timing, second);
                }

                timing.ApplyCheckpoint(
                    new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                    new TieBreakerState(TeamSide.Blue));

                var set = RuleModificationQuery.Build(
                    timing,
                    null,
                    participants,
                    new TieBreakerState(TeamSide.Blue),
                    UnlockedUltimateSet.None, RuleObject.None, null, null);

                Assert.That(
                    set.IsImmuneTo(
                        RuleObject.ForHero(first), RuleActivity.Skill, RuleObject.ForHero(Hanu)),
                    Is.True,
                    "The fixture must actually hold the immunity, or the case above proves nothing.");
                return set;
            }

            /// <summary>
            /// One immunity, built the way a match builds it - from an Active effect in its active
            /// phase - rather than hand-assembled. Brynn's fixture's shape exactly.
            /// </summary>
            private static void Arm(ActiveEffectLifecycle timing, HeroId heroId)
            {
                var definition = new ActiveEffectDefinition(
                    "hanu-skill-immunity-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.CardsPlayedAndRevealed,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    true,
                    "HanuSkillImmunity",
                    "hanu-skill-immunity",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForImmunity(RuleActivity.Skill) });

                timing.Create(
                    new ActiveEffectId($"hanu-immunity-{heroId.Value}"),
                    definition,
                    heroId,
                    TeamSide.Red,
                    default,
                    new CardInstanceId("hanu-immunity-source"),
                    new ActionDefinitionId("hanu-immunity-action"),
                    1,
                    1);
            }

            private static SpaceId SpaceIdFor(HexCoordinate at) =>
                new SpaceId($"q{at.Q}_r{at.R}");

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            internal void RebindTrustedContent() => context = BuildContext();

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    Body(cardId),
                    Catalog(),
                    participants,
                    null,
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    components: components,
                    heroItems: items);

            /// <summary>
            /// This build's whole synthetic vocabulary plus the ONE selection this block declares,
            /// taken from the DEFERRED list because nothing of his is registered. Naming a block list
            /// rather than the build's whole catalog is milestone 8.40j's BA-18: this is a bounded
            /// synthetic catalog and handing back a handler the synthetic vocabulary already holds is
            /// refused for a duplicate reference.
            /// </summary>
            private static EffectCapabilityCatalog Catalog() =>
                SyntheticEffectCapabilities.CreateCatalog(
                    HanuComponentContent.GreenDeferredHandlers());

            internal EffectResolutionStepResult Run()
            {
                var definition = Face(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-greens-resolution"),
                    new CardInstanceId("hanu-greens-source"),
                    definition.Reference,
                    HanuCardContent.PrimarySkill,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    context,
                    RepeatContinuation.FirstInstance);

                Assert.That(result.Accepted, Is.True, "The walk must start.");
                frame = result.Frame;
                return result;
            }

            internal PendingEffectChoice RunToPause()
            {
                var result = Run();
                Assert.That(result.IsComplete, Is.False, "This case expects a question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal PendingEffectChoice Answer(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.False, "This case expects another question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult AnswerToCompletion(
                PendingEffectChoice choice, EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True, "The answer to " + choice.NodeId + " stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            /// <summary>
            /// The pair's whole answer: name both units, then confirm - which is how a selection with a
            /// floor of two is answered.
            /// </summary>
            private EffectResolutionStepResult SelectAndConfirm(HeroId hero, MinionId minion)
            {
                var pending = frame.PendingChoice;
                Assert.That(Submit(Pick(pending, hero)).Accepted, Is.True);
                Assert.That(Submit(Pick(frame.PendingChoice, minion)).Accepted, Is.True);
                return Submit(frame.PendingChoice.ConfirmInvocation);
            }

            private EffectResolutionStepResult SelectAndConfirm(MinionId minion, HeroId hero)
            {
                var pending = frame.PendingChoice;
                Assert.That(Submit(Pick(pending, minion)).Accepted, Is.True);
                Assert.That(Submit(Pick(frame.PendingChoice, hero)).Accepted, Is.True);
                return Submit(frame.PendingChoice.ConfirmInvocation);
            }

            internal EffectResolutionStepResult SelectAndConfirmToCompletion(
                HeroId hero, MinionId minion)
            {
                var result = SelectAndConfirm(hero, minion);
                Assert.That(result.Accepted, Is.True, "The pair stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            internal EffectResolutionStepResult SelectAndConfirmToCompletion(
                MinionId minion, HeroId hero)
            {
                var result = SelectAndConfirm(minion, hero);
                Assert.That(result.Accepted, Is.True, "The pair stood.");
                Assert.That(result.IsComplete, Is.True, "This case expects the text to finish.");
                return result;
            }

            internal PendingEffectChoice SelectAndConfirmToPause(HeroId hero, MinionId minion)
            {
                var result = SelectAndConfirm(hero, minion);
                Assert.That(result.Accepted, Is.True, "The pair stood.");
                Assert.That(result.IsComplete, Is.False, "This case expects another question.");
                Assert.That(result.Frame.PendingChoice, Is.Not.Null);
                return result.Frame.PendingChoice;
            }

            internal EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            internal HexCoordinate Where(HeroId hero) =>
                components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(hero), out var at)
                    ? at
                    : throw new InvalidOperationException($"{hero.Value} stands nowhere.");

            internal HexCoordinate Where(MinionId minion) =>
                components.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForMinion(minion), out var at)
                    ? at
                    : throw new InvalidOperationException($"{minion.Value} stands nowhere.");
        }
    }
}
