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
using GuardsDigital.Core.Rules.Consequences;
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
    /// Milestone 8.69d. Brynn the Seeker's three tracking swaps - Tread Lightly, Cover Tracks and Hide
    /// Traces - which print one sentence between them: "Swap either with a unit adjacent to you, or
    /// with an enemy hero in radius who is adjacent to 3 or more obstacles."
    ///
    /// <para><b>What the cases are about.</b> The sentence is ONE choice over the union of two reaches
    /// (BR-Q10), so almost every case below is a difference between two boards that changes exactly one
    /// word of it: which side a unit is on, whether it is a unit at all, how far away it is, and
    /// whether the hero the second arm reaches is boxed in. p.19 makes the exchange neither movement
    /// nor placement and makes it TARGET its subject, which is why immunity narrows the offer here and
    /// narrows none of the presence predicates beside it.</para>
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Brynn's is registered - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.69g deals him - so no composed match can put one of
    /// these cards in a hand and offer its Skill. <see cref="RowennaSwapAndGauntletTests"/> records the
    /// same boundary for the same reason, and what is proved here is everything the text itself
    /// decides: who is offered, where the pieces end up, and what the trailing move may do. The
    /// enclosing action pipeline and the seat-level Undo are 8.69g's.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class BrynnTrackingSwapTests
    {
        private static readonly HeroId Brynn = new HeroId("brynn-seeker");
        private static readonly HeroId Ally = new HeroId("brynn-blue-ally");
        private static readonly HeroId Enemy = new HeroId("brynn-red-enemy");
        private static readonly HeroId SecondEnemy = new HeroId("brynn-red-second-enemy");

        private static readonly MinionId BesideMinion = new MinionId("brynn-minion-beside");
        private static readonly MinionId DistantMinion = new MinionId("brynn-minion-distant");

        private static readonly TeamId BlueTeam = new TeamId("brynn-blue-team");
        private static readonly TeamId RedTeam = new TeamId("brynn-red-team");
        private static readonly MapId Map = new MapId("brynn-tracking-swap-map");

        /// <summary>Where he starts every case.</summary>
        private static readonly HexCoordinate BrynnAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent, and his own side - "a unit adjacent to you" names neither team.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(1, -1);

        /// <summary>Adjacent, and the other side.</summary>
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        /// <summary>Adjacent, and a minion - p.19's second kind of unit.</summary>
        private static readonly HexCoordinate BesideMinionAt = new HexCoordinate(0, -1);

        /// <summary>Two spaces out: inside every Radius these faces print, and adjacent to nothing.</summary>
        private static readonly HexCoordinate DistantMinionAt = new HexCoordinate(2, -2);

        /// <summary>Two spaces out - the second arm's reach on Tread Lightly's printed Radius 2.</summary>
        private static readonly HexCoordinate SecondEnemyAt = new HexCoordinate(0, 2);

        /// <summary>Three of <see cref="SecondEnemyAt"/>'s six neighbours, and none of Brynn's.</summary>
        private static readonly HexCoordinate[] BoxingSecondEnemy =
        {
            new HexCoordinate(1, 2), new HexCoordinate(-1, 3), new HexCoordinate(0, 3)
        };

        /// <summary>Three spaces out: outside Tread Lightly's Radius 2 and inside Cover Tracks' 3.</summary>
        private static readonly HexCoordinate BeyondTheDiskAt = new HexCoordinate(0, 3);

        private static readonly HexCoordinate[] BoxingBeyondTheDisk =
        {
            new HexCoordinate(1, 3), new HexCoordinate(-1, 4), new HexCoordinate(0, 4)
        };

        /// <summary>
        /// Three of <see cref="EnemyAt"/>'s six neighbours that nothing else is standing on, so the
        /// enemy hero beside Brynn can be boxed in as well - the one unit both arms of the printed
        /// "either ... or" name.
        /// </summary>
        private static readonly HexCoordinate[] BoxingTheAdjacentEnemy =
        {
            new HexCoordinate(2, 0), new HexCoordinate(2, -1), new HexCoordinate(1, 1)
        };

        /// <summary>Adjacent to him, and not a unit at all.</summary>
        private static readonly HexCoordinate TokenAt = new HexCoordinate(-1, 0);

        // -------------------------------------------------------------------------------------------
        // The union
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE OFFER. One board, one question, and every word of the printed sentence doing
        /// its own work: "a unit adjacent to you" admits the friendly hero, the enemy hero and the
        /// friendly minion standing beside him whatever side they are on, and "an enemy hero in radius
        /// who is adjacent to 3 or more obstacles" adds the one boxed-in enemy two spaces out.
        ///
        /// <para>Four candidates in ONE list, which is what makes this a union rather than a modal: the
        /// card never asks which kind of swap, so nothing here is asked in two questions.</para>
        /// </summary>
        [Test]
        public void TreadLightly_OffersTheUnionOfBothReachesAsOneQuestion()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            var choice = world.RunToPause();

            Assert.That(Named(choice), Is.EqualTo(new[]
                {
                    Ally.Value, Enemy.Value, SecondEnemy.Value, BesideMinion.Value
                }),
                "The three units standing beside him - friendly hero, enemy hero, friendly minion - " +
                "plus the boxed-in enemy hero inside the printed Radius 2. Heroes before minions and " +
                "each group in ordinal order, because a union offered in a derived order would " +
                "replay differently than it was answered.");

            Assert.That(choice.ChooserId, Is.EqualTo(Brynn),
                "\"Swap ... with\" is his own step, so the decision is his.");
        }

        /// <summary>
        /// A MINION two spaces out is never offered, and that is the second arm's noun rather than its
        /// reach: "an enemy HERO in radius" names heroes, so a unit inside the disk that is not one is
        /// outside the sentence entirely.
        /// </summary>
        [Test]
        public void TheRadiusArm_ReachesHeroesOnly_SoAMinionInTheDiskIsNeverOffered()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            Assert.That(
                HexDistanceQuery.Calculate(BrynnAt, DistantMinionAt), Is.EqualTo(2),
                "The minion at (2,-2) is inside Tread Lightly's printed Radius 2, which is what " +
                "makes its absence about the noun rather than about the distance.");
            Assert.That(Named(world.RunToPause()), Has.No.Member(DistantMinion.Value),
                "and it is not adjacent to him either, so neither arm of the sentence reaches it.");
        }

        /// <summary>
        /// An enemy hero inside the disk who is NOT boxed in is not offered. Same hero, same space, and
        /// the only difference between the two readings is how many of his six neighbours hold
        /// something.
        /// </summary>
        [Test]
        public void TheRadiusArm_DropsAnEnemyHeroInTheDiskWhoIsNotBoxedIn()
        {
            var open = World.Swap(BrynnCardContent.TreadLightly, boxesTheSecondEnemy: false);

            Assert.That(Named(open.RunToPause()), Has.No.Member(SecondEnemy.Value),
                "Nothing stands beside him, so \"who is adjacent to 3 or more obstacles\" is the " +
                "clause unmet - and the clause narrows who may be PICKED, so he is never offered " +
                "rather than offered and then refused.");

            var boxed = World.Swap(BrynnCardContent.TreadLightly);
            Assert.That(Named(boxed.RunToPause()), Has.Member(SecondEnemy.Value),
                "Three obstacles beside the same hero on the same space is the whole difference.");
        }

        /// <summary>
        /// A boxed-in enemy hero OUTSIDE the printed Radius is not offered, and the identical board
        /// read through Cover Tracks' Radius 3 offers him - which is p.9's icon bounding the second arm
        /// and nothing about this definition inventing a number.
        /// </summary>
        [Test]
        public void TheRadiusArm_IsBoundedByThePrintedIcon_SoCoverTracksReachesWhatTreadLightlyCannot()
        {
            Assert.That(HexDistanceQuery.Calculate(BrynnAt, BeyondTheDiskAt), Is.EqualTo(3));

            var treadLightly = World.Swap(BrynnCardContent.TreadLightly, farBoxedEnemy: true);
            Assert.That(Named(treadLightly.RunToPause()), Has.No.Member(SecondEnemy.Value),
                "Three spaces out is outside Tread Lightly's printed Radius 2.");

            var coverTracks = World.Swap(BrynnCardContent.CoverTracks, farBoxedEnemy: true);
            Assert.That(Named(coverTracks.RunToPause()), Has.Member(SecondEnemy.Value),
                "and Cover Tracks prints Radius 3, which reaches him - the same sentence read " +
                "through a different icon.");
        }

        /// <summary>
        /// p.16's Radius item widens the second arm and the first arm not at all: adjacency is p.19's
        /// innermost ring, written into the sentence, and no item raises it.
        /// </summary>
        [Test]
        public void ARadiusItem_WidensTheHeroArmAndLeavesAdjacencyAlone()
        {
            var without = World.Swap(BrynnCardContent.TreadLightly, farBoxedEnemy: true);
            Assert.That(Named(without.RunToPause()), Is.EqualTo(new[]
                {
                    Ally.Value, Enemy.Value, BesideMinion.Value
                }),
                "Radius 2 reaches nobody at three spaces, so the offer is the adjacency arm alone.");

            var with = World.Swap(
                BrynnCardContent.TreadLightly, farBoxedEnemy: true, radiusItems: 1);
            Assert.That(Named(with.RunToPause()), Is.EqualTo(new[]
                {
                    Ally.Value, Enemy.Value, SecondEnemy.Value, BesideMinion.Value
                }),
                "and one p.16 item raises the printed icon by one, which reaches him. The three " +
                "adjacent units are the same three, because nothing raises \"adjacent\".");
        }

        /// <summary>
        /// A TOKEN standing beside him is never a candidate. p.19's units are heroes and minions, the
        /// sentence says "a unit", and the consuming Swap declares
        /// <c>SwapEligiblePieceKind.Unit</c> - so the exclusion is stated twice and measured once.
        /// </summary>
        [Test]
        public void ATokenBesideHim_IsNeverASwapSubject()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            Assert.That(world.HoldsATokenAt(TokenAt), Is.True,
                "The fixture must actually have put a token beside him, or this case proves " +
                "nothing.");
            Assert.That(
                world.RunToPause().Candidates
                    .Select(candidate => candidate.Value.Kind).Distinct().ToArray(),
                Is.EquivalentTo(new[] { EffectValueKind.Hero, EffectValueKind.Minion }),
                "Every candidate is a hero or a minion; the token on an adjacent space is not one " +
                "of p.19's units and so is not part of the sentence at all.");
        }

        /// <summary>
        /// An enemy hero who is adjacent to him AND boxed in inside the disk satisfies both arms and is
        /// offered ONCE, which is what "or" means about a set (BR-Q19d).
        /// </summary>
        [Test]
        public void AHeroSatisfyingBothArms_IsOneCandidateAndNotTwo()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly, boxesTheAdjacentEnemy: true);

            var named = Named(world.RunToPause());

            Assert.That(named.Count(value => value == Enemy.Value), Is.EqualTo(1),
                "He stands beside Brynn and is adjacent to three obstacles inside the printed " +
                "Radius, so both halves of the printed \"either ... or\" name him - and a union is " +
                "a set.");
        }

        /// <summary>
        /// He is never among his own candidates. A radius query drawn on a hero contains that hero, and
        /// "swap WITH a unit" means an exchange with somebody else - an exchange with yourself is
        /// nothing.
        /// </summary>
        [Test]
        public void HeIsNeverHisOwnSwapSubject()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            Assert.That(Named(world.RunToPause()), Has.No.Member(Brynn.Value));
        }

        // -------------------------------------------------------------------------------------------
        // p.18's immunity, and what it does not touch
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.19 makes a Swap TARGET its subject, so p.18's immunity keeps an immune hero out of the
        /// offer - and the SAME immune hero goes on counting as an obstacle beside somebody else,
        /// because p.19 says in so many words that presence checks count immune units.
        ///
        /// <para>Both halves are measured on one board at one instant: the immune enemy standing at
        /// (0,1) is the third obstacle that qualifies the enemy at (0,2), and he is himself not
        /// offered.</para>
        /// </summary>
        [Test]
        public void AnImmuneEnemyHero_IsNoSwapSubjectAndStillCountsAsAnObstacle()
        {
            var world = World.Swap(
                BrynnCardContent.TreadLightly,
                adjacentEnemyAt: new HexCoordinate(0, 1),
                boxingObstacles: 2,
                immuneHero: Enemy);

            var named = Named(world.RunToPause());

            Assert.That(named, Has.No.Member(Enemy.Value),
                "An immune unit cannot be targeted at all, and p.19 makes a Swap a targeting of the " +
                "unit it exchanges with.");
            Assert.That(named, Has.Member(SecondEnemy.Value),
                "and two placed obstacles plus the immune hero standing on the third neighbouring " +
                "space is three - p.19: an effect that checks for presence counts an immune unit " +
                "exactly as it counts any other.");
        }

        /// <summary>
        /// A board on which the only subjects are immune leaves the whole sentence with nothing to
        /// take, which is the impossible-step case reached through p.18 rather than through geometry.
        /// </summary>
        [Test]
        public void TreadLightly_PerformsNothingWhenEverySubjectIsImmune()
        {
            var world = World.Swap(
                BrynnCardContent.TreadLightly,
                onlyTheAdjacentEnemy: true,
                immuneHero: Enemy);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True);
            Assert.That(world.Where(Brynn), Is.EqualTo(BrynnAt));
        }

        // -------------------------------------------------------------------------------------------
        // p.19's exchange
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The Swap exchanges the two spaces and nothing else: each piece ends up standing where the
        /// other was, and p.19 states outright that this "does not count as movement or placement".
        /// </summary>
        [Test]
        public void TheSwap_ExchangesBothPieces()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            var choice = world.RunToPause();
            world.AnswerToCompletion(choice, Pick(choice, BesideMinion));

            Assert.That(world.Where(Brynn), Is.EqualTo(BesideMinionAt));
            Assert.That(world.Where(BesideMinion), Is.EqualTo(BrynnAt),
                "Both pieces move, which is what makes a Swap different from a Place of one of them.");
        }

        /// <summary>
        /// THE "NOT MOVEMENT" MEASUREMENT. Hide Traces swaps him two spaces and then still offers the
        /// full "up to 2", measured from where the exchange left him - so the exchange spent no part of
        /// the printed budget and started no walk of its own (p.19).
        /// </summary>
        [Test]
        public void TheSwap_SpendsNoneOfTheTrailingMoveAndTheMoveStartsWhereItLeftHim()
        {
            var world = World.Swap(BrynnCardContent.HideTraces);

            var subject = world.RunToPause();
            var move = world.Answer(subject, Pick(subject, SecondEnemy));

            Assert.That(world.Where(Brynn), Is.EqualTo(SecondEnemyAt),
                "Two spaces, by exchange rather than by movement.");
            Assert.That(
                Spaces(move).Select(space => HexDistanceQuery.Calculate(SecondEnemyAt, space))
                    .Distinct().OrderBy(value => value).ToArray(),
                Is.EqualTo(new[] { 0, 1, 2 }),
                "and the move that follows is measured from the space the exchange PUBLISHED, with " +
                "its whole printed distance intact - which is both halves of p.19's sentence at " +
                "once (BR-Q20d).");
        }

        // -------------------------------------------------------------------------------------------
        // The trailing moves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Tread Lightly prints no trailing clause, so the text is over the moment the exchange has
        /// happened - measured as the walk COMPLETING on the swap's own answer rather than as the
        /// absence of a node.
        /// </summary>
        [Test]
        public void TreadLightly_EndsWithTheSwapAndAsksNothingAfterIt()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly);

            var choice = world.RunToPause();
            var result = world.AnswerToCompletion(choice, Pick(choice, Ally));

            Assert.That(result.IsComplete, Is.True);
            Assert.That(world.Where(Brynn), Is.EqualTo(AllyAt));
        }

        /// <summary>
        /// "You may move 1 space" - p.11 leaves the number exact and p.10 makes the step his to
        /// decline, so the question offers spaces exactly one away and a decline of its own.
        /// </summary>
        [Test]
        public void CoverTracks_OffersAnOptionalMoveOfExactlyOneSpace()
        {
            var world = World.Swap(BrynnCardContent.CoverTracks);

            var subject = world.RunToPause();
            var move = world.Answer(subject, Pick(subject, Ally));

            Assert.That(move.DeclineInvocation, Is.Not.Null,
                "\"You may\" is his to decline, which p.10 makes a decision about whether the step " +
                "runs at all.");
            Assert.That(
                Spaces(move).Select(space => HexDistanceQuery.Calculate(AllyAt, space)),
                Is.All.EqualTo(1),
                "and every space it offers is exactly one away: \"1 space\" prints no \"up to\", so " +
                "standing still is the decline rather than a candidate.");

            world.AnswerToCompletion(move, move.DeclineInvocation);
            Assert.That(world.Where(Brynn), Is.EqualTo(AllyAt),
                "A declined second clause says nothing about the first - the swap stands.");
        }

        /// <summary>
        /// "Move up to 2 spaces" - no "may", so the step is mandatory and the DISTANCE is the decision.
        /// p.11's "up to" admits zero, which is the GA-Q1 reading, so his own space is one of the
        /// candidates and there is no decline beside them.
        /// </summary>
        [Test]
        public void HideTraces_MakesTheDistanceTheDecisionAndAdmitsZero()
        {
            var world = World.Swap(BrynnCardContent.HideTraces);

            var subject = world.RunToPause();
            var move = world.Answer(subject, Pick(subject, Ally));

            Assert.That(Spaces(move), Has.Member(AllyAt),
                "\"up to 2\" admits zero, so the space the swap left him in is one of the answers.");
            Assert.That(
                Spaces(move).Max(space => HexDistanceQuery.Calculate(AllyAt, space)), Is.EqualTo(2),
                "and two is the ceiling the card prints.");

            world.AnswerToCompletion(move, Pick(move, AllyAt));
            Assert.That(world.Where(Brynn), Is.EqualTo(AllyAt),
                "Taking zero spaces leaves him where the exchange put him, which is the step being " +
                "performed rather than skipped.");
        }

        /// <summary>
        /// The move actually moves him, and it moves nobody else - the trailing clause names himself
        /// and the card prints no second subject.
        /// </summary>
        [Test]
        public void HideTraces_MovesHimTheDistanceHeChooses()
        {
            var world = World.Swap(BrynnCardContent.HideTraces);

            var subject = world.RunToPause();
            var move = world.Answer(subject, Pick(subject, Ally));
            var destination = Spaces(move)
                .First(space => HexDistanceQuery.Calculate(AllyAt, space) == 2);

            world.AnswerToCompletion(move, Pick(move, destination));

            Assert.That(world.Where(Brynn), Is.EqualTo(destination));
            Assert.That(world.Where(Ally), Is.EqualTo(BrynnAt),
                "and his subject is still standing where the exchange put him, because the move is " +
                "his own.");
        }

        // -------------------------------------------------------------------------------------------
        // The impossible swap
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A board with no unit beside him and no qualifying enemy hero in the disk is p.10's
        /// impossible mandatory step: the action performs nothing at all. Measured on the face that
        /// prints no trailing clause, so that nothing else could have been what stopped.
        /// </summary>
        [Test]
        public void TreadLightly_PerformsNothingWhenNeitherReachHoldsASubject()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly, empty: true);

            var result = world.Run();

            Assert.That(result.IsComplete, Is.True,
                "Nothing pauses: the step that could have asked cannot be taken.");
            Assert.That(world.Where(Brynn), Is.EqualTo(BrynnAt));
        }

        /// <summary>
        /// THE READING THE SHARD ASKS FOR. Cover Tracks' "You may move 1 space" after an impossible
        /// swap is NOT reached - because what p.10 stops is the ACTION rather than the one step, and
        /// the move is a later sibling of a mandatory step that could not be taken.
        ///
        /// <para>Measured against the same board answering differently when the swap CAN be taken, so
        /// that "no pause" is the stop rather than a face that never asks.</para>
        /// </summary>
        [Test]
        public void CoverTracks_NeverOffersItsOptionalMoveAfterAnImpossibleSwap()
        {
            var stopped = World.Swap(BrynnCardContent.CoverTracks, empty: true);

            var result = stopped.Run();

            Assert.That(result.IsComplete, Is.True,
                "The action is over: p.10 stops it at the impossible mandatory step, and \"You may " +
                "move 1 space\" is a step after that one rather than an independent sentence.");
            Assert.That(stopped.Where(Brynn), Is.EqualTo(BrynnAt),
                "and he has not moved a space, which is the whole consequence of the reading.");

            var performed = World.Swap(BrynnCardContent.CoverTracks);
            var subject = performed.RunToPause();
            Assert.That(performed.Answer(subject, Pick(subject, Ally)).NodeId.Value,
                Is.EqualTo(BrynnEffectContent.TrackingSwapMoveNode),
                "while the identical face on a board that holds a subject does reach the move - so " +
                "the silence above is p.10 rather than a missing node.");
        }

        /// <summary>
        /// Hide Traces' mandatory "Move up to 2 spaces" is unreached for the same reason, which is the
        /// same reading stated where the trailing clause is not optional at all.
        /// </summary>
        [Test]
        public void HideTraces_NeverReachesItsMandatoryMoveAfterAnImpossibleSwap()
        {
            var world = World.Swap(BrynnCardContent.HideTraces, empty: true);

            Assert.That(world.Run().IsComplete, Is.True);
            Assert.That(world.Where(Brynn), Is.EqualTo(BrynnAt));
        }

        // -------------------------------------------------------------------------------------------
        // Over the Top
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// His Ultimate widens the second arm without the swap knowing an Ultimate exists: on a board
        /// where nothing at all stands beside the enemy hero in the disk, he is offered while Brynn is
        /// the acting hero.
        ///
        /// <para>That falls out of <see cref="ObstacleAdjacencyQuery"/> owning the predicate - the
        /// selection asks the one query, and the counts-as overlay lives inside it.</para>
        /// </summary>
        [Test]
        public void OverTheTop_WidensTheRadiusArmToEveryEnemyHeroInTheDisk()
        {
            var without = World.Swap(BrynnCardContent.TreadLightly, boxesTheSecondEnemy: false);
            Assert.That(Named(without.RunToPause()), Has.No.Member(SecondEnemy.Value),
                "Nothing stands beside him on this board.");

            var with = World.Swap(
                BrynnCardContent.TreadLightly, boxesTheSecondEnemy: false, overTheTop: true);
            Assert.That(Named(with.RunToPause()), Has.Member(SecondEnemy.Value),
                "\"While you are performing actions all enemy heroes in play count as adjacent to 3 " +
                "or more obstacles\" - so the arm reaches every enemy hero the disk holds, and the " +
                "selection names no Ultimate to get it.");
        }

        /// <summary>
        /// The Ultimate widens the SECOND arm and not the first: it says nothing about reach, so a
        /// boxed-in enemy hero outside the printed Radius stays outside it.
        /// </summary>
        [Test]
        public void OverTheTop_WidensThePredicateAndNotThePrintedReach()
        {
            var world = World.Swap(
                BrynnCardContent.TreadLightly, farBoxedEnemy: true, overTheTop: true);

            Assert.That(Named(world.RunToPause()), Has.No.Member(SecondEnemy.Value),
                "Three spaces is still outside Radius 2. p.17's Counts As answers a predicate; it " +
                "is not a p.16 item.");
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
        public void HideTraces_ResumesThroughARestoreWhileTheTrailingMoveIsPending()
        {
            var world = World.Swap(BrynnCardContent.HideTraces);

            var subject = world.RunToPause();
            var move = world.Answer(subject, Pick(subject, Ally));

            world.RebindTrustedContent();

            var destination = Spaces(move)
                .First(space => HexDistanceQuery.Calculate(AllyAt, space) == 2);
            world.AnswerToCompletion(move, Pick(move, destination));

            Assert.That(world.Where(Brynn), Is.EqualTo(destination));
            Assert.That(world.Where(Ally), Is.EqualTo(BrynnAt),
                "and the exchange the frame had already performed is still on the board, because " +
                "what a restore rebuilds is the trusted content rather than the state.");
        }

        /// <summary>
        /// THE REWIND PROPERTY. A retained-prefix Undo replays the same commands against the same
        /// staged state, so two independent walks answered identically must end identically - which is
        /// what makes the union safe to rewind through.
        /// </summary>
        [Test]
        public void TheWholeSentence_ReplaysIdenticallyFromTheSameStagedState()
        {
            var first = Walk();
            var second = Walk();

            Assert.That(second.Offer, Is.EqualTo(first.Offer),
                "The same board offers the same union in the same order.");
            Assert.That(second.Destinations, Is.EqualTo(first.Destinations),
                "the same answer reaches the same trailing move,");
            Assert.That(second.BrynnAt, Is.EqualTo(first.BrynnAt));
            Assert.That(second.SubjectAt, Is.EqualTo(first.SubjectAt),
                "and the two pieces end in the same two spaces.");
        }

        /// <summary>
        /// The union is re-derived when the answer arrives rather than trusted from the moment the
        /// question opened, so a space that was never offered has no invocation to forge.
        /// </summary>
        [Test]
        public void TheOffer_CarriesNoInvocationForAUnitItNeverNamed()
        {
            var world = World.Swap(BrynnCardContent.TreadLightly, boxesTheSecondEnemy: false);

            var choice = world.RunToPause();

            Assert.That(
                choice.SelectInvocations.Any(invocation =>
                    invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                    invocation.Candidate.Value.HeroId.Equals(SecondEnemy)),
                Is.False,
                "The enemy hero standing in the open is not among the candidates, so there is no " +
                "invocation naming him - which is what makes a forged answer unanswerable rather " +
                "than merely wrong.");
        }

        // -------------------------------------------------------------------------------------------
        // The contract, and what it deliberately is not
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// BR-Q17d, MEASURED. Whisper's <c>choose-swap-subject@1</c> is the shipped union selection and
        /// its grammar is this sentence's - but its second arm is the card's printed RANGE, and all
        /// three of these faces print a RADIUS. Hide Traces is where that stops being a distinction: it
        /// prints Radius 3 and hands out a RANGE item, so a composition on the Range arm would have
        /// measured the swap with an icon the card does not print.
        /// </summary>
        [Test]
        public void TheUnionContract_IsHisOwnBecauseTheSecondArmIsARadiusAndNotARange()
        {
            var hideTraces = Face(BrynnCardContent.HideTraces);

            Assert.That(Icon(hideTraces, ActionKind.Skill, PrintedValueChannel.Radius),
                Is.EqualTo(3),
                "The face prints a Radius icon,");
            Assert.That(Icon(hideTraces, ActionKind.Skill, PrintedValueChannel.Range), Is.Zero,
                "prints no Range icon at all,");
            Assert.That(hideTraces.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Range),
                "and hands out a RANGE item - so a swap arm measured by Range would have been " +
                "widened by an item this card's own reach has nothing to do with.");

            var whisperUnion = SyntheticRosterCapabilities.Handlers()
                .Single(handler =>
                    handler.Reference.Equals(SyntheticRosterCapabilities.ChooseSwapSubjectV1));
            Assert.That(
                whisperUnion.Schema.Parameters.Select(parameter => parameter.Name).ToArray(),
                Is.EqualTo(new[]
                {
                    SyntheticRosterCapabilities.AdjacentUnitKindParameter,
                    SyntheticRosterCapabilities.RangedHeroRelationParameter,
                    SyntheticRosterCapabilities.RangedHeroRequiresDiscardParameter
                }),
                "and the shipped union is untouched by this milestone: its hero arm is still the " +
                "printed RANGE one Cruel Twist and Sealed Fate print, with no Radius toggle bolted " +
                "onto it. This build keeps Range and Radius as two capabilities rather than one " +
                "parameter, and BR-Q17d applies that doctrine inside one sentence.");
        }

        /// <summary>
        /// The selection declares no parameters at all, which is the sentence rather than a shortcut:
        /// all three faces print the identical clause, both reaches are written into it, and an unbound
        /// optional parameter is exactly how a narrowing goes silently missing.
        /// </summary>
        [Test]
        public void TheSelection_DeclaresNoParametersAndPublishesTheThreeConventionalOutputs()
        {
            var handler = BrynnComponentContent.SwapDeferredHandlers().Single();

            Assert.That(handler.Reference, Is.EqualTo(BrynnComponentContent.TrackingSwapSubjectV1));
            Assert.That(handler.Schema.Parameters, Is.Empty);
            Assert.That(
                handler.Schema.Outputs.Select(output => output.OutputName).ToArray(),
                Is.EqualTo(new[]
                {
                    BrynnComponentContent.ChosenSpaceOutputName,
                    BrynnComponentContent.ChosenHeroOutputName,
                    BrynnComponentContent.ChosenMinionOutputName
                }),
                "The three spellings the shipped selections already use, so a consumer written " +
                "against either reads the same names - and the space is the one p.19's exchange " +
                "takes.");
        }

        // -------------------------------------------------------------------------------------------
        // The three faces
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The three faces carry the printed values his shard's inventory table records. Literal rather
        /// than derived, for the reason every verified set's table is literal.
        /// </summary>
        [Test]
        public void TheThreeFaces_CarryThePrintedValuesTheShardRecords()
        {
            var treadLightly = Face(BrynnCardContent.TreadLightly);
            Assert.That(treadLightly.Initiative.Value, Is.EqualTo(10));
            Assert.That(treadLightly.Progression.Color, Is.EqualTo(CardColor.Blue));
            Assert.That(treadLightly.Progression.Tier, Is.EqualTo(CardTier.I));
            Assert.That(treadLightly.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None));
            Assert.That(Icon(treadLightly, ActionKind.Skill, PrintedValueChannel.Radius),
                Is.EqualTo(2));
            Assert.That(Icon(treadLightly, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(5));
            Assert.That(Icon(treadLightly, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(3));

            var coverTracks = Face(BrynnCardContent.CoverTracks);
            Assert.That(coverTracks.Initiative.Value, Is.EqualTo(10));
            Assert.That(coverTracks.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(coverTracks.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Defense));
            Assert.That(Icon(coverTracks, ActionKind.Skill, PrintedValueChannel.Radius),
                Is.EqualTo(3));
            Assert.That(Icon(coverTracks, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(5));

            var hideTraces = Face(BrynnCardContent.HideTraces);
            Assert.That(hideTraces.Initiative.Value, Is.EqualTo(11));
            Assert.That(hideTraces.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(hideTraces.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Range));
            Assert.That(Icon(hideTraces, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(6));
            Assert.That(Icon(hideTraces, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(3));

            foreach (var id in new[]
            {
                BrynnCardContent.TreadLightly, BrynnCardContent.CoverTracks,
                BrynnCardContent.HideTraces
            })
            {
                var face = Face(id);
                Assert.That(face.ContentRevision,
                    Is.EqualTo(BrynnComponentContent.SwapPrepRevision),
                    "Every face this block composes carries its own placeholder revision, which " +
                    "8.69f replaces with the one real string his whole set is stated under.");
                Assert.That(
                    face.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill).Id,
                    Is.EqualTo(BrynnCardContent.PrimarySkill));
            }
        }

        /// <summary>
        /// Each face names only the half of p.15's Blue fork this block composes - Mountain Guide and
        /// Expedition Leader are 8.69c's ids, and naming an id a sibling branch owns is what Min's
        /// series refused four times over. 8.69f states both ladders whole.
        /// </summary>
        [Test]
        public void TheBlueLadder_NamesOnlyTheHalfThisBlockComposes()
        {
            Assert.That(
                Face(BrynnCardContent.TreadLightly).Progression.NextTierCandidateIds
                    .Select(id => id.Value).ToArray(),
                Is.EqualTo(new[] { BrynnCardContent.CoverTracks.Value }));
            Assert.That(
                Face(BrynnCardContent.CoverTracks).Progression.NextTierCandidateIds
                    .Select(id => id.Value).ToArray(),
                Is.EqualTo(new[] { BrynnCardContent.HideTraces.Value }));
            Assert.That(
                Face(BrynnCardContent.HideTraces).Progression.NextTierCandidateIds, Is.Empty,
                "p.15's Blue line ends at Tier III.");
        }

        /// <summary>
        /// The three bodies differ by exactly the trailing clause each prints and by nothing else -
        /// which is what makes one factory the honest statement of three cards.
        /// </summary>
        [Test]
        public void TheThreeBodies_DifferByExactlyTheTrailingClause()
        {
            var treadLightly = NodeIds(BrynnCardContent.TreadLightly);
            Assert.That(treadLightly, Is.EqualTo(new[]
                {
                    BrynnEffectContent.TrackingSwapExchangeNode,
                    BrynnEffectContent.TrackingSwapRootNode,
                    BrynnEffectContent.TrackingSwapSubjectNode
                }),
                "The sentence is a selection and an exchange, in that order, under one sequence.");

            foreach (var id in new[] { BrynnCardContent.CoverTracks, BrynnCardContent.HideTraces })
            {
                Assert.That(NodeIds(id).Except(treadLightly).ToArray(),
                    Is.EqualTo(new[] { BrynnEffectContent.TrackingSwapMoveNode }),
                    $"and {id.Value} adds exactly the clause after its full stop.");
            }

            Assert.That(MovePolicy(BrynnCardContent.CoverTracks), Is.EqualTo(EffectFailurePolicy.May),
                "\"You may move 1 space\" - p.10's optional STEP over p.11's exact number.");
            Assert.That(MovePolicy(BrynnCardContent.HideTraces), Is.EqualTo(EffectFailurePolicy.UpTo),
                "\"Move up to 2 spaces\" - a mandatory step whose DISTANCE is the decision, which is " +
                "not a wording that can also be declined.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 7.9's gate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The selection is declared and withheld, asserted from BOTH sides: it is in his own deferred
        /// list, and it is not registered in the catalog a configured match loads.
        /// </summary>
        [Test]
        public void TheSelection_IsDeferredAndUnreachableFromAConfiguredMatch()
        {
            Assert.That(BrynnComponentContent.SwapRegisteredHandlers(), Is.Empty,
                "Nothing of his is registered, because 8.69g is the block that deals him.");
            Assert.That(
                BrynnComponentContent.SwapDeferredHandlers()
                    .Select(handler => handler.Reference.ToString()).ToArray(),
                Is.EqualTo(new[] { BrynnComponentContent.TrackingSwapSubjectV1.ToString() }));

            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities
                    .IsRegistered(BrynnComponentContent.TrackingSwapSubjectV1),
                Is.False,
                "'brynn-tracking-swap-subject@1' must not be reachable from a configured match " +
                "before a dealt card of his can start it: the registered content list is serialized " +
                "and pinned, so registering early moves pinned bytes while no content moved.");

            Assert.That(BrynnCardContent.DealtDefinitions, Is.Empty,
                "and he deals nothing, which is what keeps the dealt-corpus validator green while " +
                "his set is being composed.");
            foreach (var definition in BrynnCardContent.SwapDefinitions)
            {
                Assert.That(
                    BrynnCardContent.AllDefinitions.Select(face => face.Id.Value).ToArray(),
                    Has.Member(definition.Id.Value),
                    "while the three faces this block composes are visible to every content sweep " +
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
            internal HexCoordinate BrynnAt;
            internal HexCoordinate SubjectAt;
        }

        /// <summary>One whole walk of Hide Traces, answered the same way every time.</summary>
        private static Replay Walk()
        {
            var world = World.Swap(BrynnCardContent.HideTraces);
            var subject = world.RunToPause();
            var offer = Named(subject);
            var move = world.Answer(subject, Pick(subject, Ally));
            var destinations = Spaces(move).OrderBy(space => space.Q).ThenBy(space => space.R)
                .ToArray();
            var destination = destinations
                .First(space => HexDistanceQuery.Calculate(AllyAt, space) == 2);
            world.AnswerToCompletion(move, Pick(move, destination));

            return new Replay
            {
                Offer = offer,
                Destinations = destinations,
                BrynnAt = world.Where(Brynn),
                SubjectAt = world.Where(Ally)
            };
        }

        private static string[] NodeIds(CardDefinitionId cardId) =>
            Body(cardId).Nodes.Select(node => node.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static EffectFailurePolicy MovePolicy(CardDefinitionId cardId) =>
            Body(cardId).Nodes
                .Single(node => node.Id.Value == BrynnEffectContent.TrackingSwapMoveNode)
                .FailurePolicy;

        private static EffectDefinition Body(CardDefinitionId cardId) =>
            Face(cardId).ActionDefinitions
                .Single(action => action.Kind == ActionKind.Skill)
                .EffectRoot;

        private static CardDefinition Face(CardDefinitionId cardId) =>
            BrynnCardContent.AllDefinitions.Single(definition => definition.Id.Equals(cardId));

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
        /// Four seats: two Blue, so "a unit adjacent to you" can be a friendly hero, and two Red, so
        /// one enemy can stand beside him and one inside the printed disk.
        /// </summary>
        private static MatchParticipants Participants()
        {
            var brynnPlayer = new PlayerId("brynn-player");
            var allyPlayer = new PlayerId("brynn-ally-player");
            var enemyPlayer = new PlayerId("brynn-enemy-player");
            var secondEnemyPlayer = new PlayerId("brynn-second-enemy-player");

            return new MatchParticipants(
                new[]
                {
                    new Team(BlueTeam, TeamSide.Blue),
                    new Team(RedTeam, TeamSide.Red)
                },
                new[]
                {
                    new Player(brynnPlayer, BlueTeam),
                    new Player(allyPlayer, BlueTeam),
                    new Player(enemyPlayer, RedTeam),
                    new Player(secondEnemyPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Brynn, brynnPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Enemy, enemyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(SecondEnemy, secondEnemyPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// One staged board and the frame walking one of his Skill bodies over it.
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
            private readonly AdjacentMoveAuthority board;
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
                AdjacentMoveAuthority board,
                RuleModificationSet modifications,
                HeroItemState items)
            {
                this.cardId = cardId;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.components = components;
                this.board = board;
                this.modifications = modifications;
                this.items = items;
                context = BuildContext();
            }

            /// <summary>
            /// The swap board: one friendly hero, one enemy hero and one friendly minion standing
            /// beside him, one friendly minion two spaces out, one enemy hero two spaces out who can
            /// be boxed in or left in the open, and a token on a fourth adjacent space.
            /// </summary>
            internal static World Swap(
                CardDefinitionId cardId,
                bool boxesTheSecondEnemy = true,
                bool boxesTheAdjacentEnemy = false,
                bool farBoxedEnemy = false,
                bool empty = false,
                bool onlyTheAdjacentEnemy = false,
                bool overTheTop = false,
                int radiusItems = 0,
                int boxingObstacles = 3,
                HexCoordinate? adjacentEnemyAt = null,
                HeroId immuneHero = default)
            {
                var participants = Participants();

                var secondEnemyAt = farBoxedEnemy ? BeyondTheDiskAt : SecondEnemyAt;
                var boxing = farBoxedEnemy ? BoxingBeyondTheDisk : BoxingSecondEnemy;

                var heroes = new List<HeroOccupancy> { new HeroOccupancy(Brynn, BrynnAt) };
                var minions = new List<(MinionId, HexCoordinate)>();
                var obstacles = new List<HexCoordinate>();

                if (empty)
                {
                    // Both reaches empty: everybody else is parked far enough out that neither the
                    // ring nor the widest disk these faces print can see them.
                    heroes.Add(new HeroOccupancy(Ally, new HexCoordinate(6, 0)));
                    heroes.Add(new HeroOccupancy(Enemy, new HexCoordinate(0, 6)));
                    heroes.Add(new HeroOccupancy(SecondEnemy, new HexCoordinate(-6, 0)));
                }
                else if (onlyTheAdjacentEnemy)
                {
                    heroes.Add(new HeroOccupancy(Enemy, EnemyAt));
                    heroes.Add(new HeroOccupancy(Ally, new HexCoordinate(6, 0)));
                    heroes.Add(new HeroOccupancy(SecondEnemy, new HexCoordinate(-6, 0)));
                }
                else
                {
                    heroes.Add(new HeroOccupancy(Ally, AllyAt));
                    heroes.Add(new HeroOccupancy(Enemy, adjacentEnemyAt ?? EnemyAt));
                    heroes.Add(new HeroOccupancy(SecondEnemy, secondEnemyAt));
                    minions.Add((BesideMinion, BesideMinionAt));
                    minions.Add((DistantMinion, DistantMinionAt));

                    if (boxesTheSecondEnemy)
                    {
                        obstacles.AddRange(boxing.Take(boxingObstacles));
                    }

                    if (boxesTheAdjacentEnemy)
                    {
                        obstacles.AddRange(BoxingTheAdjacentEnemy);
                    }
                }

                var board = Board(heroes, minions, obstacles);
                if (!empty && !onlyTheAdjacentEnemy)
                {
                    PlaceToken(board);
                }

                var rules = overTheTop
                    ? OverTheTopRules(board, participants)
                    : immuneHero.IsDefined
                        ? ImmunityFor(participants, immuneHero, RuleActivity.Skill)
                        : RuleModificationSet.Empty;

                return new World(
                    cardId,
                    BrynnCardContent.AllDefinitions.ToArray(),
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
                    board,
                    rules,
                    RadiusItems(participants, radiusItems));
            }

            private static AdjacentMoveAuthority Board(
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At)> minions,
                IReadOnlyList<HexCoordinate> obstacles)
            {
                var zone = new ZoneId("brynn-tracking-swap-zone");
                var spaces = new List<BoardMapSpace>();
                for (var q = -8; q <= 8; q++)
                {
                    for (var r = -8; r <= 8; r++)
                    {
                        var coordinate = new HexCoordinate(q, r);
                        if (HexDistanceQuery.Calculate(coordinate, BrynnAt) > 8)
                        {
                            continue;
                        }

                        spaces.Add(new BoardMapSpace(
                            SpaceIdFor(coordinate), coordinate, BoardMapSpaceKind.Playable, zone));
                    }
                }

                return new AdjacentMoveAuthority(
                    new BoardMapDefinition(
                        Map, 1, "Brynn Tracking Swap Map",
                        spaces,
                        new[] { new BoardMapZone(zone, "Battle", "lane", null, false, true) },
                        Array.Empty<BoardMapSpawnPoint>(),
                        Array.Empty<ZoneId>()),
                    heroes,
                    obstacles
                        .Select((at, index) => new ObstacleOccupancy(
                            new ObstacleId($"brynn-obstacle-{index:00}"), at))
                        .ToArray(),
                    minions
                        .Select(minion => new QuickGameMinionSetupState(
                            minion.Id,
                            BlueTeam,
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(Map, SpaceIdFor(minion.At)),
                            minion.At))
                        .ToArray());
            }

            private static void PlaceToken(AdjacentMoveAuthority board)
            {
                var catalog = SyntheticComponentDefinitions.CreateCatalog();
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("brynn-tracking-swap-token"),
                    catalog.LoadToken(SyntheticComponentDefinitions.HazardV4),
                    Brynn,
                    Brynn,
                    new CardInstanceId("brynn-tracking-swap-token-source"),
                    TokenAt));
            }

            private static HeroItemState RadiusItems(MatchParticipants participants, int count) =>
                count <= 0
                    ? new HeroItemState(participants)
                    : HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                    {
                        new HeroItem(
                            Brynn,
                            new CardInstanceId("brynn-radius-item"),
                            HeroDashboard.RulebookLayout.Slots[3].Position,
                            PrintedValueChannel.Radius,
                            count)
                    }));

            /// <summary>
            /// The live set his unlocked Ultimate supplies while he is the acting hero - derived from
            /// the card and the board rather than hand-assembled, on milestone 8.39b's terms.
            /// </summary>
            private static RuleModificationSet OverTheTopRules(
                AdjacentMoveAuthority board, MatchParticipants participants)
            {
                var ultimate = BrynnCardContent.AllDefinitions
                    .Single(definition => definition.Id.Equals(BrynnCardContent.OverTheTop));
                return RuleModificationQuery.Build(
                    null,
                    board,
                    participants,
                    TieBreakerState.Unrevealed,
                    UltimateUnlockQuery.Derive(
                        new[] { ultimate },
                        new[]
                        {
                            new CardInstance(
                                new CardInstanceId("brynn-over-the-top-instance"),
                                ultimate.Reference,
                                Brynn,
                                new CardStateFacts(
                                    CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                    CardFaceOrientation.FaceUp))
                        }),
                    RuleObject.ForHero(Brynn),
                    null,
                    null);
            }

            /// <summary>
            /// A live set carrying one immunity, built the way a match builds it - from an Active
            /// effect in its active phase - rather than hand-assembled.
            /// </summary>
            private static RuleModificationSet ImmunityFor(
                MatchParticipants participants, HeroId heroId, RuleActivity activity)
            {
                var definition = new ActiveEffectDefinition(
                    $"brynn-{activity}-immunity-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.CardsPlayedAndRevealed,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    true,
                    $"Brynn{activity}Immunity",
                    $"brynn-{activity}-immunity",
                    Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForImmunity(activity) });

                var timing = new ActiveEffectLifecycle();
                timing.Create(
                    new ActiveEffectId($"brynn-immunity-{heroId.Value}-{activity}"),
                    definition,
                    heroId,
                    TeamSide.Red,
                    default,
                    new CardInstanceId("brynn-immunity-source"),
                    new ActionDefinitionId("brynn-immunity-action"),
                    1,
                    1);
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
                        RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Brynn)),
                    Is.True,
                    "The fixture must actually hold the immunity, or the case above proves nothing.");
                return set;
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
            /// This build's whole synthetic vocabulary plus the ONE selection this block defers.
            /// Naming the deferred list rather than the whole one is milestone 8.40j's BA-18: a
            /// fixture that hands back already-registered handlers is refused for a duplicate
            /// reference.
            /// </summary>
            private static EffectCapabilityCatalog Catalog() =>
                SyntheticEffectCapabilities.CreateCatalog(
                    BrynnComponentContent.SwapDeferredHandlers());

            internal EffectResolutionStepResult Run()
            {
                var definition = Face(cardId);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("brynn-tracking-swap-resolution"),
                    new CardInstanceId("brynn-tracking-swap-source"),
                    definition.Reference,
                    BrynnCardContent.PrimarySkill,
                    Brynn,
                    Brynn,
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

            internal EffectResolutionStepResult Submit(EffectChoiceCommand command)
            {
                var result = EffectResolutionSession.Answer(frame, command, context);
                if (result.Accepted)
                {
                    frame = result.Frame;
                }

                return result;
            }

            internal bool HoldsATokenAt(HexCoordinate at) =>
                board.TryGetObjectAt(at, out var found) && found.Kind == BoardObjectKind.Token;

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
