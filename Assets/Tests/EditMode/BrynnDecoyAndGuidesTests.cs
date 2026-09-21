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
    /// Milestone 8.69c. Brynn the Seeker's three faces that move somebody else: Decoy, his Silver
    /// basic, and the two mountain guides.
    ///
    /// <para><b>Why these cases sit at the effect FRAME rather than on a configured match.</b> Nothing
    /// of Brynn's is registered - milestone 7.9's gate holds his content out of
    /// <see cref="MatchContentCatalogs"/> until 8.69g deals him - so no composed match can put one of
    /// these cards in a hand and perform its primary. What is proved here is everything the text
    /// itself decides: who is offered, how far each piece may go, which pieces the second pass may not
    /// name again, whether the conditional sentence runs at all, and what a restore resumes to. The
    /// enclosing action pipeline and real pointer input are 8.69g's and 8.69h's.</para>
    ///
    /// <para><b>The catalog these cases load is this build's plus Brynn's two deferred predicates.</b>
    /// The guides' "if" reaches <c>brynn-enemy-hero-in-radius-adjacent-to-obstacles@1</c>, which
    /// milestone 8.69a composed and this build does not register - a fixture that could not load it
    /// could not prove the faces at all. That the build itself still registers nothing of his is
    /// asserted below rather than assumed.</para>
    ///
    /// <para><b>The board is four seats wide and says why.</b> Two Blue - Brynn and one ally, so
    /// "a friendly unit" is not a synonym for "a minion" - and two Red, so a case can stand one enemy
    /// hero beside three obstacles and one in the open without moving anybody between cases.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class BrynnDecoyAndGuidesTests
    {
        private static readonly HeroId Brynn = new HeroId("brynn-seeker");
        private static readonly HeroId Ally = new HeroId("brynn-blue-ally");
        private static readonly HeroId Boxed = new HeroId("brynn-red-boxed");
        private static readonly HeroId Open = new HeroId("brynn-red-open");

        private static readonly TeamId BlueTeam = new TeamId("brynn-blue-team");
        private static readonly TeamId RedTeam = new TeamId("brynn-red-team");

        private static readonly MapId Map = new MapId("brynn-guides-map");

        private static readonly HexCoordinate BrynnAt = new HexCoordinate(0, 0);

        /// <summary>Adjacent to Brynn: the friendly HERO his first guide sentence may move.</summary>
        private static readonly HexCoordinate AllyAt = new HexCoordinate(1, 0);

        /// <summary>Four spaces out - the edge of the printed Radius 4 both guides and Decoy carry.</summary>
        private static readonly HexCoordinate BoxedAt = new HexCoordinate(4, 0);

        /// <summary>Five spaces out: outside that disk, and inside the one a Radius item makes of it.</summary>
        private static readonly HexCoordinate BoxedOutOfRadiusAt = new HexCoordinate(5, 0);

        /// <summary>Three spaces out, with nothing beside it.</summary>
        private static readonly HexCoordinate OpenAt = new HexCoordinate(0, 3);

        /// <summary>Well outside every disk, for a case that wants an enemy hero on the board and nowhere near.</summary>
        private static readonly HexCoordinate FarAway = new HexCoordinate(0, 7);

        private static readonly MinionId FriendlyBeside = new MinionId("brynn-friendly-beside");
        private static readonly MinionId FriendlyInRadius = new MinionId("brynn-friendly-in-radius");
        private static readonly MinionId EnemyNear = new MinionId("brynn-enemy-near");
        private static readonly MinionId EnemyOther = new MinionId("brynn-enemy-other");
        private static readonly MinionId EnemyFar = new MinionId("brynn-enemy-far");

        private static readonly HexCoordinate FriendlyBesideAt = new HexCoordinate(0, 1);
        private static readonly HexCoordinate FriendlyInRadiusAt = new HexCoordinate(0, -3);
        private static readonly HexCoordinate EnemyNearAt = new HexCoordinate(2, 0);
        private static readonly HexCoordinate EnemyOtherAt = new HexCoordinate(-2, 0);
        private static readonly HexCoordinate EnemyFarAt = new HexCoordinate(0, 6);

        /// <summary>The three obstacles that make <see cref="Boxed"/> satisfy his printed clause.</summary>
        private static HexCoordinate[] BoxesIn(HexCoordinate at) =>
            at.GetNeighbors().Take(3).ToArray();

        // -------------------------------------------------------------------------------------------
        // Decoy - the count
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// BR-Q4's floor. "Choose UP TO two times" makes zero performances a legal and complete answer,
        /// and the card then does nothing at all.
        /// </summary>
        [Test]
        public void Decoy_TakenNoTimesAtAll_CompletesWithNobodyMoved()
        {
            var world = World.Decoy();
            var before = world.Positions();

            var first = world.RunToPause();
            Assert.That(first.NodeId.Value, Is.EqualTo(BrynnEffectContent.DecoyFirstPassNode));
            Assert.That(first.DeclineInvocation, Is.Not.Null,
                "\"Up to two times\" is p.10's own word for a count whose floor is zero, so the very " +
                "first performance is refusable by name.");

            var finished = world.SubmitToCompletion(first.DeclineInvocation);
            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Positions(), Is.EqualTo(before),
                "Nobody moved, and the card still completed - zero performances is the sentence being " +
                "obeyed rather than a step that failed.");
        }

        /// <summary>
        /// The count's ceiling: the second performance is offered after the first and may itself be
        /// refused, so one is as legal an answer as two.
        /// </summary>
        [Test]
        public void Decoy_TakenOnce_OffersTheSecondPerformanceAndLetsItBeRefused()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            var second = world.Answer(spaces, Pick(spaces, EmptyNeighboursOf(EnemyNearAt, world)[0]));

            Assert.That(second.NodeId.Value,
                Is.EqualTo(BrynnEffectContent.DecoySecondPassAfterMinionNode),
                "The second performance is the last step of the bullet the first took - BR-Q17c.");

            var finished = world.SubmitToCompletion(second.DeclineInvocation);
            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(EnemyOther), Is.EqualTo(EnemyOtherAt),
                "and the enemy minion the second performance would have reached stayed where it was.");
        }

        // -------------------------------------------------------------------------------------------
        // Decoy - the two bullets
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Move an enemy minion in radius 1 space" - every enemy minion inside the printed disk is
        /// offered, the one outside it is not, and no friendly minion ever is.
        /// </summary>
        [Test]
        public void Decoy_FirstBullet_OffersTheEnemyMinionsInsideThePrintedRadiusAndNoOthers()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();

            Assert.That(
                Named(minions).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { EnemyNear.Value, EnemyOther.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "\"an enemy minion IN RADIUS\" - the minion five spaces out is outside the printed " +
                "Radius 4, and the two friendly minions are on the wrong side of the board.");
        }

        /// <summary>
        /// "Move an enemy hero in radius WHO IS ADJACENT TO 3 OR MORE OBSTACLES 1 space" - two enemy
        /// heroes stand inside one disk and only the boxed-in one is offered.
        /// </summary>
        [Test]
        public void Decoy_SecondBullet_OffersOnlyTheEnemyHeroBesideThreeObstacles()
        {
            var world = World.Decoy();

            var heroes = world.ToDecoyHeroBullet();

            Assert.That(Named(heroes), Is.EqualTo(new[] { Boxed.Value }),
                "Both enemy heroes stand inside the printed Radius 4; the clause is the only thing " +
                "separating the one that is offered from the one that is not, and it narrows who may " +
                "be PICKED rather than refusing a pick after the fact.");

            var spaces = world.Answer(heroes, Pick(heroes, Boxed));

            Assert.That(
                Spaces(spaces).OrderBy(space => space.Q).ThenBy(space => space.R).ToArray(),
                Is.EqualTo(EmptyNeighboursOf(BoxedAt, world)),
                "and the hero boxed in by three obstacles may be moved onto the three spaces that " +
                "are NOT obstacles and onto no others - p.11's own rule that a move never ends on " +
                "one, asked of the very spaces that made him a legal subject.");
            Assert.That(Spaces(spaces).Intersect(BoxesIn(BoxedAt)), Is.Empty,
                "measured against the three the fixture staged, rather than inferred.");
        }

        /// <summary>
        /// THE EXACT DISTANCE. "1 space" prints no "up to", so the destinations offered are exactly the
        /// empty spaces one step away - never the piece's own space, never a space holding anything,
        /// and never one across an obstacle.
        /// </summary>
        [Test]
        public void Decoy_MovesItsSubjectExactlyOneSpace_OntoAnEmptySpaceAndNeverOntoAnObstacle()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));

            Assert.That(
                Spaces(spaces).OrderBy(space => space.Q).ThenBy(space => space.R).ToArray(),
                Is.EqualTo(EmptyNeighboursOf(EnemyNearAt, world)
                    .OrderBy(space => space.Q).ThenBy(space => space.R).ToArray()),
                "An exact distance of one admits the six neighbouring spaces and nothing else, and " +
                "p.11's reachability keeps every occupied or terrain space out of them - which is why " +
                "the mover's own space is not among them either.");

            var destination = EmptyNeighboursOf(EnemyNearAt, world)[0];
            var second = world.Answer(spaces, Pick(spaces, destination));

            Assert.That(world.Where(EnemyNear), Is.EqualTo(destination));
            Assert.That(HexDistanceQuery.Calculate(EnemyNearAt, destination), Is.EqualTo(1),
                "One space, measured.");
            Assert.That(second.NodeId.Value,
                Is.EqualTo(BrynnEffectContent.DecoySecondPassAfterMinionNode));
        }

        /// <summary>
        /// BR-Q4's own sentence: "a unit with no empty neighbour cannot be moved and is not offered".
        /// The Move is named as the selection's consumer, so milestone 8.35l's narrowing drops it from
        /// the offer rather than letting it be picked and then refused.
        /// </summary>
        [Test]
        public void Decoy_NeverOffersAMinionWithNoEmptySpaceBesideIt()
        {
            var world = World.Decoy(extraTerrain: EnemyOtherAt.GetNeighbors().ToArray());

            Assert.That(EmptyNeighboursOf(EnemyOtherAt, world), Is.Empty,
                "The fixture must actually box the minion in, or the case below proves nothing.");

            var minions = world.ToDecoyMinionBullet();

            Assert.That(Named(minions), Is.EqualTo(new[] { EnemyNear.Value }),
                "The hemmed-in minion is a legal subject of the printed sentence and an impossible " +
                "one of its Move, and BR-Q4 settles that as not being offered at all.");
        }

        // -------------------------------------------------------------------------------------------
        // Decoy - "on different targets"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "ON DIFFERENT TARGETS", measured on the bullet that can repeat itself: the second
        /// performance's minion bullet offers every enemy minion except the one the first moved.
        /// </summary>
        [Test]
        public void Decoy_SecondPerformance_NeverOffersTheMinionTheFirstOneNamed()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            var second = world.Answer(spaces, Pick(spaces, EmptyNeighboursOf(EnemyNearAt, world)[0]));

            var again = world.Answer(second, second.ConfirmInvocation);
            var secondMinions = world.Answer(again,
                PickArm(again, BrynnEffectContent.DecoySecondMinionBulletAfterMinionNode));

            Assert.That(Named(secondMinions), Is.EqualTo(new[] { EnemyOther.Value }),
                "The minion the first performance moved is still standing inside the disk and is " +
                "still an enemy minion; \"on different targets\" is the only thing that takes it out " +
                "of the second offer.");
        }

        /// <summary>
        /// The same clause on the hero bullet: with two boxed-in enemy heroes, the second performance
        /// offers the one the first did not name.
        /// </summary>
        [Test]
        public void Decoy_SecondPerformance_NeverOffersTheHeroTheFirstOneNamed()
        {
            var world = World.Decoy(extraTerrain: BoxesIn(OpenAt));

            var heroes = world.ToDecoyHeroBullet();
            Assert.That(
                Named(heroes).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Boxed.Value, Open.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "Both enemy heroes now stand beside three obstacles, which is what makes the case " +
                "below about the exclusion and about nothing else.");

            var spaces = world.Answer(heroes, Pick(heroes, Boxed));
            var second = world.Answer(spaces, Pick(spaces, EmptyNeighboursOf(BoxedAt, world)[0]));

            var again = world.Answer(second, second.ConfirmInvocation);
            var secondHeroes = world.Answer(again,
                PickArm(again, BrynnEffectContent.DecoySecondHeroBulletAfterHeroNode));

            Assert.That(Named(secondHeroes), Is.EqualTo(new[] { Open.Value }),
                "and the hero the first performance moved is gone from the second offer.");
        }

        /// <summary>
        /// The same bullet may be taken twice and the two bullets may be taken one each - both are
        /// ordinary rather than special, and a performance that named a MINION excludes no hero.
        /// </summary>
        [Test]
        public void Decoy_MayTakeTheMinionBulletThenTheHeroBullet_WhichExcludesNobody()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            var second = world.Answer(spaces, Pick(spaces, EmptyNeighboursOf(EnemyNearAt, world)[0]));

            var again = world.Answer(second, second.ConfirmInvocation);
            var heroes = world.Answer(again,
                PickArm(again, BrynnEffectContent.DecoySecondHeroBulletAfterMinionNode));

            Assert.That(Named(heroes), Is.EqualTo(new[] { Boxed.Value }),
                "A first performance that moved a minion says nothing about which heroes remain, so " +
                "the boxed-in hero is offered in full - which is what makes the exclusion one clause " +
                "about one named unit rather than a blanket ban.");

            var heroSpaces = world.Answer(heroes, Pick(heroes, Boxed));
            var destination = EmptyNeighboursOf(BoxedAt, world)[0];
            var finished = world.AnswerToCompletion(heroSpaces, Pick(heroSpaces, destination));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(Boxed), Is.EqualTo(destination));
            Assert.That(world.Where(EnemyNear), Is.Not.EqualTo(EnemyNearAt),
                "and both performances stood: two different units, one space each.");
        }

        /// <summary>
        /// A Decoy with no legal unit at all does nothing and completes. The board holds two enemy
        /// heroes in the open and every enemy minion out of the disk, so neither bullet has a subject.
        /// </summary>
        [Test]
        public void Decoy_WithNothingItCouldMove_CompletesHavingDoneNothing()
        {
            var world = World.Decoy(
                boxedAt: FarAway,
                enemyNearAt: new HexCoordinate(7, 0),
                enemyOtherAt: new HexCoordinate(-7, 0),
                terrain: Array.Empty<HexCoordinate>());
            var before = world.Positions();

            var finished = world.Run();

            Assert.That(finished.IsComplete, Is.True,
                "Neither bullet has a subject, so the optional performance is never asked and the " +
                "Skill finishes - a card that does nothing is not a card that failed.");
            Assert.That(world.Positions(), Is.EqualTo(before));
        }

        /// <summary>
        /// OVER THE TOP, on Decoy's second bullet: with his Ultimate unlocked and Brynn acting, the
        /// enemy hero standing in the open satisfies the clause and is offered - on a board where
        /// nothing at all has moved.
        /// </summary>
        [Test]
        public void OverTheTop_WidensDecoysSecondBulletToEveryEnemyHeroInRadius()
        {
            var world = World.Decoy(terrain: Array.Empty<HexCoordinate>(), withOverTheTop: true);

            var heroes = world.ToDecoyHeroBullet();

            Assert.That(
                Named(heroes).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Boxed.Value, Open.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "\"all enemy heroes in play count as adjacent to 3 or more obstacles\" - neither hero " +
                "has an obstacle beside them and both are offered, because the bullet asks the one " +
                "owner of the printed phrase and the overlay lives inside it.");

            var withoutIt = World.Decoy(terrain: Array.Empty<HexCoordinate>());
            Assert.That(Named(withoutIt.ToDecoyHeroBullet()), Is.Empty,
                "and the identical board without the Ultimate offers nobody at all, which isolates " +
                "the widening to one declaration.");
        }

        // -------------------------------------------------------------------------------------------
        // Decoy - restore and stale answers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A restore taken between the two performances resumes against freshly rebuilt content and
        /// answers the same question, exclusion included - which is what a snapshot carries and what
        /// the resolver has to be able to resume from.
        /// </summary>
        [Test]
        public void Decoy_ResumesThroughARestoreBetweenItsTwoPerformances()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));
            var second = world.Answer(spaces, Pick(spaces, EmptyNeighboursOf(EnemyNearAt, world)[0]));

            world.RebindTrustedContent();

            var again = world.Answer(second, second.ConfirmInvocation);
            var secondMinions = world.Answer(again,
                PickArm(again, BrynnEffectContent.DecoySecondMinionBulletAfterMinionNode));

            Assert.That(Named(secondMinions), Is.EqualTo(new[] { EnemyOther.Value }),
                "The rebuilt definition binds the same exclusion to the same published minion, so the " +
                "second offer after a restore is the offer that stood before it.");
        }

        /// <summary>
        /// A stale answer to a destination that is no longer offered is refused: the candidates are
        /// re-derived when the answer arrives rather than trusted from the moment the question opened.
        /// </summary>
        [Test]
        public void Decoy_RefusesAnAnswerNamingASpaceItsMoveNeverOffered()
        {
            var world = World.Decoy();

            var minions = world.ToDecoyMinionBullet();
            var spaces = world.Answer(minions, Pick(minions, EnemyNear));

            var stale = new EffectChoiceCommand(
                spaces.ResolutionId,
                spaces.ChoiceId,
                EffectChoiceCommandKind.Select,
                EffectChoiceCandidate.Value_(EffectValue.Space(FarAway)),
                Brynn);

            Assert.That(world.Submit(stale).Accepted, Is.False,
                "Seven spaces away is not one space away, and an answer the question never offered is " +
                "refused rather than applied.");
        }

        // -------------------------------------------------------------------------------------------
        // The guides - the first sentence
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "You may move A FRIENDLY UNIT ADJACENT TO YOU" - p.19's units on his own side at p.17's one
        /// ring, and p.17's "Friendly ... never includes you" keeps Brynn himself out of it.
        /// </summary>
        [Test]
        public void MountainGuide_FirstSentence_OffersTheFriendlyUnitsBesideHimAndNeverHimself()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();

            Assert.That(units.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideFirstUnitNode));
            Assert.That(
                Named(units).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Ally.Value, FriendlyBeside.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "A friendly hero and a friendly minion, both one space away: \"unit\" is p.19's word " +
                "for both, and the friendly minion three spaces out is not adjacent.");
            Assert.That(Named(units), Has.No.Member(Brynn.Value),
                "p.17: \"Friendly ... never includes you\".");
            Assert.That(units.DeclineInvocation, Is.Not.Null,
                "\"YOU MAY move\" - the whole clause is refusable, which is the `may` p.11 separates " +
                "from the \"up to\" on its distance.");
        }

        /// <summary>
        /// "... UP TO 2 SPACES" - the distance is the decision, so every space within two is offered
        /// AND so is the unit's own, which is p.11's optional distance including none at all (BR-Q9,
        /// on Garrus's GA-Q1).
        /// </summary>
        [Test]
        public void MountainGuide_FirstSentence_OffersEverySpaceWithinTwoAndTheUnitsOwn()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));

            Assert.That(Spaces(spaces), Contains.Item(FriendlyBesideAt),
                "\"up to 2 spaces\" admits standing still, and the way this vocabulary says so is by " +
                "offering the mover's own space - which a card printing an exact distance never does.");
            Assert.That(
                Spaces(spaces).All(space =>
                    HexDistanceQuery.Calculate(FriendlyBesideAt, space) <= 2),
                Is.True,
                "and nothing further than the printed two.");
            Assert.That(
                Spaces(spaces).Any(space =>
                    HexDistanceQuery.Calculate(FriendlyBesideAt, space) == 2),
                Is.True,
                "with the printed distance itself reachable, so the bound is the card's rather than " +
                "an accident of the board.");
        }

        /// <summary>
        /// Expedition Leader prints the same sentence one space further, which is the whole of what
        /// separates the two faces.
        /// </summary>
        [Test]
        public void ExpeditionLeader_PrintsTheSameSentenceAtThreeSpaces()
        {
            var world = World.Guide(BrynnCardContent.ExpeditionLeader);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));

            Assert.That(
                Spaces(spaces).Any(space =>
                    HexDistanceQuery.Calculate(FriendlyBesideAt, space) == 3),
                Is.True,
                "\"up to 3 spaces\" on the Tier III face.");
            Assert.That(
                Spaces(spaces).All(space =>
                    HexDistanceQuery.Calculate(FriendlyBesideAt, space) <= 3),
                Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // The guides - the conditional second sentence
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "IF an enemy hero in radius is adjacent to 3 or more obstacles" - with nobody in the disk
        /// satisfying it, the second sentence never runs and the card completes.
        /// </summary>
        [Test]
        public void MountainGuide_SecondSentence_DoesNotRunWhenNoEnemyHeroInRadiusQualifies()
        {
            var world = World.Guide(
                BrynnCardContent.MountainGuide, terrain: Array.Empty<HexCoordinate>());

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));
            var finished = world.AnswerToCompletion(spaces, Pick(spaces, FriendlyBesideAt));

            Assert.That(finished.IsComplete, Is.True,
                "The printed \"if\" failed, so the sentence it governs was never reached and the " +
                "Skill finished on the first sentence alone.");
            Assert.That(world.Where(FriendlyInRadius), Is.EqualTo(FriendlyInRadiusAt));
        }

        /// <summary>
        /// The same board with the clause satisfied: the second sentence is MANDATORY - it prints no
        /// "may" - and names a unit whether or not the first sentence did anything.
        /// </summary>
        [Test]
        public void MountainGuide_SecondSentence_IsMandatoryOnceAnEnemyHeroQualifies()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));
            var second = world.Answer(spaces, Pick(spaces, FriendlyBesideAt));

            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "The first unit was named and left standing, and the second sentence opens all the " +
                "same - it carries no \"may\" of its own.");
            Assert.That(second.DeclineInvocation, Is.Null,
                "and it cannot be refused: what is optional about it is the DISTANCE, which p.11 " +
                "separates from the step.");
        }

        /// <summary>
        /// "A DIFFERENT friendly unit" excludes the unit the first sentence named WHETHER OR NOT it
        /// moved - the exclusion binds the selection rather than the walk.
        /// </summary>
        [Test]
        public void MountainGuide_SecondSentence_ExcludesTheFirstUnitEvenWhereItMovedNoSpaces()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));
            var second = world.Answer(spaces, Pick(spaces, FriendlyBesideAt));

            Assert.That(world.Where(FriendlyBeside), Is.EqualTo(FriendlyBesideAt),
                "The first unit spent its optional distance on nothing at all,");
            Assert.That(
                Named(second).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Ally.Value, FriendlyInRadius.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "and it is still not one of the units the second sentence may name - \"different\" is " +
                "about the naming rather than about the walking (BR-Q9).");
        }

        /// <summary>
        /// The first sentence declined names nobody, so the second sentence excludes nobody. An unbound
        /// exclusion is the absence of the clause rather than a narrowing nobody asked for.
        /// </summary>
        [Test]
        public void MountainGuide_SecondSentence_ExcludesNobodyWhenTheFirstSentenceWasDeclined()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var second = world.Answer(units, units.DeclineInvocation);

            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "Declining the first sentence does not decline the second: they are two sentences and " +
                "only one of them prints \"You may\".");
            Assert.That(
                Named(second).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Ally.Value, FriendlyBeside.Value, FriendlyInRadius.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "Every friendly unit in the printed Radius is offered, because no first unit was ever " +
                "named to be different from.");
        }

        /// <summary>
        /// The second sentence's reach is the card's printed RADIUS rather than the first sentence's
        /// adjacency, and its "up to 2" includes none at all.
        /// </summary>
        [Test]
        public void MountainGuide_SecondSentence_ReachesTheWholePrintedRadiusAndMayCostNothing()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var second = world.Answer(units, units.DeclineInvocation);

            Assert.That(Named(second), Contains.Item(FriendlyInRadius.Value),
                "\"a different friendly unit IN RADIUS\" - three spaces out is inside the printed " +
                "Radius 4 and nowhere near the ring the first sentence reads.");

            var spaces = world.Answer(second, Pick(second, FriendlyInRadius));
            var finished = world.AnswerToCompletion(spaces, Pick(spaces, FriendlyInRadiusAt));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyInRadius), Is.EqualTo(FriendlyInRadiusAt),
                "and \"up to 2 spaces\" was satisfied by none of them, which is p.11's optional " +
                "distance and the reading BR-Q9 takes from Garrus's GA-Q1.");
        }

        /// <summary>
        /// Both sentences, end to end: a friendly minion walks two spaces and a different friendly hero
        /// walks after it.
        /// </summary>
        [Test]
        public void MountainGuide_MovesTwoDifferentFriendlyUnits_TwoSpacesEach()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, FriendlyBeside));
            var minionDestination = Spaces(spaces).First(space =>
                HexDistanceQuery.Calculate(FriendlyBesideAt, space) == 2);
            var second = world.Answer(spaces, Pick(spaces, minionDestination));

            var allySpaces = world.Answer(second, Pick(second, Ally));
            var allyDestination = Spaces(allySpaces).First(space =>
                HexDistanceQuery.Calculate(AllyAt, space) == 2);
            var finished = world.AnswerToCompletion(allySpaces, Pick(allySpaces, allyDestination));

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(world.Where(FriendlyBeside), Is.EqualTo(minionDestination));
            Assert.That(world.Where(Ally), Is.EqualTo(allyDestination));
            Assert.That(world.Where(Brynn), Is.EqualTo(BrynnAt),
                "and Brynn never moved: both sentences move somebody else.");
        }

        /// <summary>
        /// The second sentence is MANDATORY, so a board where the first sentence took the only
        /// friendly unit in radius is a step that cannot be performed - and p.10 stops the action
        /// there rather than skipping it, with everything already applied left applied.
        /// </summary>
        [Test]
        public void MountainGuide_StopsAtItsSecondSentence_WhenNoDifferentFriendlyUnitIsInRadius()
        {
            var world = World.Guide(
                BrynnCardContent.MountainGuide,
                friendlyBesideAt: new HexCoordinate(0, -7),
                friendlyInRadiusAt: new HexCoordinate(0, -6));

            var units = world.RunToPause();
            Assert.That(Named(units), Is.EqualTo(new[] { Ally.Value }),
                "One friendly unit stands beside him and the two friendly minions are seven and six " +
                "spaces out, so the first sentence names the only one the second could have.");

            var spaces = world.Answer(units, Pick(units, Ally));
            var destination = Spaces(spaces).First(space =>
                HexDistanceQuery.Calculate(BrynnAt, space) <= 4 && !space.Equals(AllyAt));
            var finished = world.AnswerToCompletion(spaces, Pick(spaces, destination));

            Assert.That(finished.IsComplete, Is.True,
                "\"move a DIFFERENT friendly unit in radius\" has nobody left to name, which is a " +
                "mandatory step that cannot be performed - p.10 stops the action at that step.");
            Assert.That(world.Where(Ally), Is.EqualTo(destination),
                "and the first sentence stays done: what was applied before the stop stays applied.");
        }

        /// <summary>
        /// p.19: "Effects that check for unit presence ... do not target", and p.18 counts immune units
        /// in a presence check - so an enemy hero immune to everything still satisfies the guides' "if".
        /// </summary>
        [Test]
        public void MountainGuide_CountsAnImmuneEnemyHero_BecauseThePresenceTargetsNobody()
        {
            var world = World.Guide(
                BrynnCardContent.MountainGuide, modifications: ImmunityFor(Boxed, RuleActivity.All));

            var units = world.RunToPause();
            var second = world.Answer(units, units.DeclineInvocation);

            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "The only hero satisfying the clause is immune to every activity there is, and the " +
                "sentence runs anyway - asking whether somebody is boxed in touches nobody.");
        }

        /// <summary>
        /// "in radius" is p.9's printed icon with p.16's items composed onto it, so his Radius item
        /// reaches a boxed-in enemy hero the printed 4 does not.
        /// </summary>
        [Test]
        public void MountainGuide_ReadsAPrintedRadiusItemWhenAskingWhoIsBoxedIn()
        {
            var withoutItem = World.Guide(
                BrynnCardContent.MountainGuide,
                boxedAt: BoxedOutOfRadiusAt,
                terrain: BoxesIn(BoxedOutOfRadiusAt));

            var units = withoutItem.RunToPause();
            var finished = withoutItem.SubmitToCompletion(units.DeclineInvocation);
            Assert.That(finished.IsComplete, Is.True,
                "Five spaces out is outside the printed Radius 4, so the clause finds nobody and the " +
                "sentence it governs never runs.");

            var withItem = World.Guide(
                BrynnCardContent.MountainGuide,
                boxedAt: BoxedOutOfRadiusAt,
                terrain: BoxesIn(BoxedOutOfRadiusAt),
                withRadiusItem: true);

            var second = withItem.Answer(withItem.RunToPause(), withItem.RunToPause().DeclineInvocation);
            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "and p.16's item raises that icon by one, which reaches him - the same composition " +
                "every printed-icon consumer performs.");
        }

        /// <summary>
        /// OVER THE TOP satisfies the guides' presence on a board with no obstacle on it at all, and
        /// only while he is the one acting.
        /// </summary>
        [Test]
        public void OverTheTop_SatisfiesTheGuidesPresenceOnABoardWithNoObstacles()
        {
            var world = World.Guide(
                BrynnCardContent.MountainGuide,
                terrain: Array.Empty<HexCoordinate>(),
                withOverTheTop: true);

            var units = world.RunToPause();
            var second = world.Answer(units, units.DeclineInvocation);

            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "\"all enemy heroes in play count as adjacent to 3 or more obstacles\" while he acts, " +
                "and the guides' predicate asks the one owner of that phrase.");

            var withoutIt = World.Guide(
                BrynnCardContent.MountainGuide, terrain: Array.Empty<HexCoordinate>());
            var plain = withoutIt.RunToPause();
            Assert.That(withoutIt.SubmitToCompletion(plain.DeclineInvocation).IsComplete, Is.True,
                "and the identical board without the Ultimate never reaches the second sentence.");
        }

        /// <summary>
        /// THE PRINTED ORDER MATTERS AND IS MEASURABLE: the "if" is asked after the first sentence, so
        /// a friendly unit the first sentence walks next to an enemy hero can be the third obstacle
        /// that makes the second sentence true.
        /// </summary>
        [Test]
        public void MountainGuide_AsksItsConditionAfterTheFirstSentence_NotBefore()
        {
            // TWO obstacles beside the enemy hero - one short of the printed three - and the lane
            // between Brynn's friendly hero and the third neighbouring space left clear, so the first
            // sentence can supply exactly what the clause is missing.
            var third = new HexCoordinate(3, 0);
            HexCoordinate[] Board() => new[] { new HexCoordinate(5, 0), new HexCoordinate(5, -1) };

            var control = World.Guide(
                BrynnCardContent.MountainGuide,
                terrain: Board(),
                enemyNearAt: new HexCoordinate(0, 2));
            var declined = control.RunToPause();
            Assert.That(control.SubmitToCompletion(declined.DeclineInvocation).IsComplete, Is.True,
                "Two obstacles is the clause unmet, so a guide whose first sentence did nothing never " +
                "reaches its second.");

            var world = World.Guide(
                BrynnCardContent.MountainGuide,
                terrain: Board(),
                enemyNearAt: new HexCoordinate(0, 2));

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, Ally));

            Assert.That(Spaces(spaces), Contains.Item(third),
                "The friendly hero can reach the third space beside the enemy hero,");

            var second = world.Answer(spaces, Pick(spaces, third));

            Assert.That(second.NodeId.Value, Is.EqualTo(BrynnEffectContent.GuideSecondUnitNode),
                "and having walked there he IS the third obstacle, which makes the printed \"if\" " +
                "true - read against the board as the first sentence left it rather than as the card " +
                "found it.");
            Assert.That(Named(second), Has.No.Member(Ally.Value),
                "The unit that supplied the obstacle is also the unit the second sentence may not " +
                "name, which is the two clauses of this card meeting on one piece.");
        }

        /// <summary>
        /// A restore taken while the second sentence's selection is pending resumes to the same offer,
        /// exclusion included.
        /// </summary>
        [Test]
        public void MountainGuide_ResumesThroughARestoreWithItsExclusionIntact()
        {
            var world = World.Guide(BrynnCardContent.MountainGuide);

            var units = world.RunToPause();
            var spaces = world.Answer(units, Pick(units, Ally));
            var second = world.Answer(spaces, Pick(spaces, AllyAt));

            world.RebindTrustedContent();

            Assert.That(
                Named(second).OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { FriendlyBeside.Value, FriendlyInRadius.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()));

            var spacesAfter = world.Answer(second, Pick(second, FriendlyInRadius));
            var finished = world.AnswerToCompletion(
                spacesAfter, Pick(spacesAfter, FriendlyInRadiusAt));

            Assert.That(finished.IsComplete, Is.True,
                "and the sentence the restore resumed into still finishes.");
        }

        // -------------------------------------------------------------------------------------------
        // The composition, the deferral and the foundations
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The three faces this block composes, as the shard's printed-inventory table states them.
        /// Literal rather than derived, for the reason every verified set's table is literal.
        /// </summary>
        [Test]
        public void TheThreeFaces_CarryThePrintedValuesTheShardRecords()
        {
            var decoy = Face(BrynnCardContent.Decoy);
            Assert.That(decoy.Initiative.Value, Is.EqualTo(1));
            Assert.That(decoy.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(decoy.Progression.Classification, Is.EqualTo(CardClassification.Basic));
            Assert.That(PrintedValue(decoy, ActionKind.Skill, PrintedValueChannel.Radius),
                Is.EqualTo(4));
            Assert.That(PrintedValue(decoy, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(2));
            Assert.That(
                decoy.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement), Is.False,
                "BR-06: Decoy prints no Movement icon, and a missing icon is absent rather than zero - " +
                "so there is no Movement action here at all.");

            var mountainGuide = Face(BrynnCardContent.MountainGuide);
            Assert.That(mountainGuide.Initiative.Value, Is.EqualTo(10));
            Assert.That(mountainGuide.Progression.Color, Is.EqualTo(CardColor.Blue));
            Assert.That(mountainGuide.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(mountainGuide.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(PrintedValue(mountainGuide, ActionKind.Skill, PrintedValueChannel.Radius),
                Is.EqualTo(4));
            Assert.That(PrintedValue(mountainGuide, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(5));
            Assert.That(PrintedValue(mountainGuide, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(3));
            Assert.That(
                mountainGuide.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[]
                {
                    BrynnCardContent.HideTraces, BrynnCardContent.ExpeditionLeader
                }),
                "p.15 forks the Blue ladder; while 8.69d was being written on another branch this " +
                "row named only Expedition Leader, and milestone 8.69f completed it.");

            var expeditionLeader = Face(BrynnCardContent.ExpeditionLeader);
            Assert.That(expeditionLeader.Initiative.Value, Is.EqualTo(11));
            Assert.That(expeditionLeader.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(expeditionLeader.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(
                PrintedValue(expeditionLeader, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(6));
            Assert.That(expeditionLeader.Progression.NextTierCandidateIds, Is.Empty);
        }

        /// <summary>
        /// Every face this block composes passes the definition validator against the catalog these
        /// cases load - which is where a binding whose producer is not guaranteed to have run first is
        /// refused, and therefore what BR-Q17c's whole composition is arranged around.
        /// </summary>
        [Test]
        public void EveryBodyThisBlockComposes_PassesTheDefinitionValidator()
        {
            var catalog = World.Catalog();
            foreach (var card in new[]
            {
                BrynnCardContent.Decoy,
                BrynnCardContent.MountainGuide,
                BrynnCardContent.ExpeditionLeader
            })
            {
                var body = Face(card).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .EffectRoot;
                Assert.That(body, Is.Not.Null, $"'{card.Value}' composes a body.");
                Assert.That(
                    EffectDefinitionValidator.Validate(body, catalog),
                    Is.Empty,
                    $"'{card.Value}' must compose to a definition this build can load.");
            }
        }

        /// <summary>
        /// Milestone 7.9's gate, asserted from both sides for THIS block: it declares no capability at
        /// all, so there is nothing of its own to withhold, and the faces it composes are visible to
        /// every content sweep while dealing nothing.
        /// </summary>
        [Test]
        public void ThisBlockDeclaresNoCapabilityOfItsOwn_AndStillDealsNothing()
        {
            Assert.That(BrynnComponentContent.GuidesDeferredHandlers(), Is.Empty,
                "Decoy and the guides name only contracts this build already has, plus 8.69a's own " +
                "deferred predicate - so this block mints nothing and defers nothing.");
            Assert.That(BrynnComponentContent.GuidesRegisteredHandlers(), Is.Empty);

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;
            Assert.That(
                registered.IsRegistered(
                    BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1),
                Is.False,
                "and the predicate the guides DO name is still withheld from every configured match, " +
                "because 8.69g is the block that deals him.");

            Assert.That(BrynnCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "milestone 8.69f states his set, and only the boxed Handicap stays out of it.");
            Assert.That(BrynnCardContent.GuidesDefinitions, Has.Count.EqualTo(3));
            Assert.That(
                BrynnCardContent.AllDefinitions, Is.SupersetOf(BrynnCardContent.GuidesDefinitions),
                "while the three faces this block composes reach the aggregate every content sweep " +
                "reads.");
        }

        /// <summary>
        /// The two exclusion parameters are OPTIONAL and declared on the Radius arms his faces print
        /// and on no others - the rule the printed-icon family has followed since milestone 8.11.
        /// </summary>
        [Test]
        public void TheExclusions_AreOptionalAndDeclaredOnTheRadiusArmsAlone()
        {
            var hero = Handler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var heroExclusion = Parameter(
                hero, SyntheticTargetingCapabilities.ExcludedHeroParameterName);
            Assert.That(heroExclusion.ValueKind, Is.EqualTo(EffectValueKind.Hero));
            Assert.That(heroExclusion.IsRequired, Is.False,
                "Optional, because a first performance that named nobody leaves it unbound - and an " +
                "unbound binding excludes nobody, which is exactly what \"up to two times\" means " +
                "when the count stops at one.");

            var minion = Handler(SyntheticTargetingCapabilities.RadiusMinionTargetV1);
            Assert.That(
                Parameter(minion, SyntheticTargetingCapabilities.ExcludedMinionParameterName)
                    .ValueKind,
                Is.EqualTo(EffectValueKind.Minion));

            var unit = Handler(SyntheticTargetingCapabilities.RadiusUnitTargetV1);
            Assert.That(
                unit.Schema.Parameters.Select(value => value.Name),
                Has.Member(SyntheticTargetingCapabilities.ExcludedHeroParameterName)
                    .And.Member(SyntheticTargetingCapabilities.ExcludedMinionParameterName),
                "The guides' second sentence names \"a different friendly UNIT\", which may be either " +
                "kind - so the unit arm declares both and exactly one of them ever resolves.");

            foreach (var range in new[]
            {
                SyntheticTargetingCapabilities.RangeHeroTargetV1,
                SyntheticTargetingCapabilities.RangeMinionTargetV1,
                SyntheticTargetingCapabilities.RangeUnitTargetV1
            })
            {
                var names = Handler(range).Schema.Parameters.Select(value => value.Name).ToArray();
                Assert.That(names,
                    Has.No.Member(SyntheticTargetingCapabilities.ExcludedHeroParameterName));
                Assert.That(names,
                    Has.No.Member(SyntheticTargetingCapabilities.ExcludedMinionParameterName),
                    $"'{range}' pairs with no face of his, and this file declares a narrowing only " +
                    "where a verified card prints it.");
            }

            Assert.That(
                SyntheticTargetingCapabilities.ExcludedHeroParameterName,
                Is.EqualTo(SyntheticAnchoredTargetCapabilities.ExcludedHeroParameterName),
                "One printed clause has one name in this catalog - the rule milestone 8.39h stated " +
                "for \"not adjacent to you\" and 8.57c restated for \"with no cards in hand\".");
        }

        /// <summary>
        /// Every pause this block's content opens with a confirm-or-decline has authored words, and
        /// every one of them names a consequence rather than the machinery underneath it.
        /// </summary>
        [Test]
        public void EveryConfirmOrDeclinePauseThisBlockOpens_HasAuthoredWords()
        {
            var declared = BrynnEffectContent.GuidesDecisionLabels().ToArray();

            foreach (var card in new[]
            {
                BrynnCardContent.Decoy,
                BrynnCardContent.MountainGuide,
                BrynnCardContent.ExpeditionLeader
            })
            {
                var body = Face(card).ActionDefinitions
                    .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .EffectRoot;

                foreach (var node in body.Nodes.Where(OptionalStepQuery.OpensConfirmOrDecline))
                {
                    var row = declared.SingleOrDefault(candidate =>
                        candidate.CardId.Equals(card) && candidate.NodeId.Equals(node.Id));
                    Assert.That(row.Label.IsDefined, Is.True,
                        $"'{card.Value}' pauses on '{node.Id.Value}' and owes words for both answers.");
                    Assert.That(row.Label.Declined, Is.Not.Empty);
                }

                foreach (var modal in body.Nodes.Where(node => node.Kind == EffectNodeKind.Choose))
                {
                    foreach (var arm in modal.ChildNodeIds)
                    {
                        Assert.That(
                            declared.Any(candidate =>
                                candidate.CardId.Equals(card) && candidate.NodeId.Equals(arm)),
                            Is.True,
                            $"'{card.Value}' prints a bullet at '{arm.Value}' and owes it words.");
                    }
                }
            }

            Assert.That(declared.All(row => row.Label.Chosen.Length <= 60), Is.True,
                "A label is a consequence rather than a paraphrase of the whole card.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Face(CardDefinitionId id) =>
            BrynnCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private static int PrintedValue(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Single(action => action.Kind == kind)
                .BaseValues
                .Single(value => value.Channel == channel)
                .Amount;

        private static IEffectCapabilityHandler Handler(EffectCapabilityReference reference) =>
            SyntheticTargetingCapabilities.Handlers()
                .Single(handler => handler.Reference.Equals(reference));

        private static EffectCapabilityParameter Parameter(
            IEffectCapabilityHandler handler, string name) =>
            handler.Schema.Parameters.Single(value => value.Name == name);

        private static string[] Named(PendingEffectChoice choice) =>
            choice.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero ||
                    candidate.Value.Kind == EffectValueKind.Minion)
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

        private static EffectChoiceCommand PickArm(PendingEffectChoice choice, string armNodeId) =>
            choice.SelectInvocations.Single(invocation =>
                invocation.Candidate.OptionNodeId.IsDefined &&
                invocation.Candidate.OptionNodeId.Value == armNodeId);

        /// <summary>
        /// The empty playable neighbours of a space, derived from the live board rather than written
        /// down - a case that listed them would be asserting its own arithmetic.
        /// </summary>
        private static HexCoordinate[] EmptyNeighboursOf(HexCoordinate at, World world) =>
            at.GetNeighbors().Where(world.IsEmpty)
                .OrderBy(space => space.Q).ThenBy(space => space.R).ToArray();

        /// <summary>
        /// A live modification set carrying one immunity, built the way a match builds it - from an
        /// Active effect in its active phase - rather than hand-assembled.
        /// </summary>
        private static RuleModificationSet ImmunityFor(HeroId heroId, RuleActivity activity)
        {
            var definition = new ActiveEffectDefinition(
                $"brynn-guides-{activity}-immunity-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                true,
                $"BrynnGuides{activity}Immunity",
                $"brynn-guides-{activity}-immunity",
                Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForImmunity(activity) });

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                new ActiveEffectId($"brynn-guides-immunity-{heroId.Value}-{activity}"),
                definition,
                heroId,
                TeamSide.Red,
                default,
                new CardInstanceId("brynn-guides-immunity-source"),
                new ActionDefinitionId("brynn-guides-immunity-action"),
                1,
                1);
            timing.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(TimingCheckpoint.CardsPlayedAndRevealed, 2, 1),
                new TieBreakerState(TeamSide.Red));

            var set = RuleModificationQuery.Build(
                timing,
                null,
                Participants(),
                new TieBreakerState(TeamSide.Red),
                UnlockedUltimateSet.None,
                RuleObject.None,
                null,
                null);
            Assert.That(
                set.IsImmuneTo(RuleObject.ForHero(heroId), activity, RuleObject.ForHero(Brynn)),
                Is.True,
                "The fixture must actually hold the immunity, or the case above proves nothing.");
            return set;
        }

        /// <summary>Four seats: two Blue, so "friendly" is not a synonym for "minion", and two Red.</summary>
        private static MatchParticipants Participants()
        {
            var brynnPlayer = new PlayerId("brynn-player");
            var allyPlayer = new PlayerId("brynn-ally-player");
            var boxedPlayer = new PlayerId("brynn-boxed-player");
            var openPlayer = new PlayerId("brynn-open-player");

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
                    new Player(boxedPlayer, RedTeam),
                    new Player(openPlayer, RedTeam)
                },
                new[]
                {
                    new Hero(Brynn, brynnPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Ally, allyPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Boxed, boxedPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Open, openPlayer, HeroDashboard.RulebookLayout)
                });
        }

        /// <summary>
        /// One staged position and the frame walking one of this block's bodies over it. Nothing here
        /// is a configured match - see the fixture's own remarks for why, and for who owns the part
        /// this cannot reach.
        /// </summary>
        private sealed class World
        {
            private readonly CardDefinitionId cardId;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
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
                AdjacentMoveAuthority board,
                RuleModificationSet modifications,
                HeroItemState items)
            {
                this.cardId = cardId;
                this.definitions = definitions;
                this.instances = instances;
                this.participants = participants;
                this.board = board;
                this.modifications = modifications;
                this.items = items;
                components = new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(), board,
                    new MarkerAttachmentRegister(), board, board, board, board);
                context = BuildContext();
            }

            /// <summary>
            /// Decoy's board: two enemy heroes inside the printed Radius 4 - one boxed in by three
            /// obstacles and one standing in the open - two enemy minions inside it, one outside, and
            /// two friendly minions the sentence may never reach.
            /// </summary>
            internal static World Decoy(
                HexCoordinate? boxedAt = null,
                HexCoordinate? enemyNearAt = null,
                HexCoordinate? enemyOtherAt = null,
                HexCoordinate[] terrain = null,
                HexCoordinate[] extraTerrain = null,
                bool withOverTheTop = false) =>
                Create(
                    BrynnCardContent.Decoy,
                    boxedAt ?? BoxedAt,
                    FriendlyBesideAt,
                    FriendlyInRadiusAt,
                    enemyNearAt ?? EnemyNearAt,
                    enemyOtherAt ?? EnemyOtherAt,
                    (terrain ?? BoxesIn(boxedAt ?? BoxedAt))
                        .Concat(extraTerrain ?? Array.Empty<HexCoordinate>())
                        .Distinct()
                        .ToArray(),
                    null,
                    false,
                    withOverTheTop);

            /// <summary>
            /// A guide's board: the same pieces, read by a face whose two sentences move friendly
            /// units instead of enemy ones.
            /// </summary>
            internal static World Guide(
                CardDefinitionId cardId,
                HexCoordinate? boxedAt = null,
                HexCoordinate? friendlyBesideAt = null,
                HexCoordinate? friendlyInRadiusAt = null,
                HexCoordinate? enemyNearAt = null,
                HexCoordinate[] terrain = null,
                RuleModificationSet modifications = null,
                bool withRadiusItem = false,
                bool withOverTheTop = false) =>
                Create(
                    cardId,
                    boxedAt ?? BoxedAt,
                    friendlyBesideAt ?? FriendlyBesideAt,
                    friendlyInRadiusAt ?? FriendlyInRadiusAt,
                    enemyNearAt ?? EnemyNearAt,
                    EnemyOtherAt,
                    terrain ?? BoxesIn(boxedAt ?? BoxedAt),
                    modifications,
                    withRadiusItem,
                    withOverTheTop);

            private static World Create(
                CardDefinitionId cardId,
                HexCoordinate boxedAt,
                HexCoordinate friendlyBesideAt,
                HexCoordinate friendlyInRadiusAt,
                HexCoordinate enemyNearAt,
                HexCoordinate enemyOtherAt,
                IReadOnlyList<HexCoordinate> terrain,
                RuleModificationSet modifications,
                bool withRadiusItem,
                bool withOverTheTop)
            {
                var participants = Participants();
                var heroes = new[]
                {
                    new HeroOccupancy(Brynn, BrynnAt),
                    new HeroOccupancy(Ally, AllyAt),
                    new HeroOccupancy(Boxed, boxedAt),
                    new HeroOccupancy(Open, OpenAt)
                };
                var minions = new[]
                {
                    (FriendlyBeside, friendlyBesideAt, TeamSide.Blue),
                    (FriendlyInRadius, friendlyInRadiusAt, TeamSide.Blue),
                    (EnemyNear, enemyNearAt, TeamSide.Red),
                    (EnemyOther, enemyOtherAt, TeamSide.Red),
                    (EnemyFar, EnemyFarAt, TeamSide.Red)
                };

                var occupied = heroes.Select(hero => hero.Coordinate)
                    .Concat(minions.Select(minion => minion.Item2))
                    .ToArray();
                var terrainSpaces = terrain.Where(space => !occupied.Contains(space)).ToArray();
                var board = Board(heroes, minions, terrainSpaces);

                var definitions = BrynnCardContent.AllDefinitions.ToArray();
                var instances = definitions
                    .Where(definition => definition.ActionDefinitions.Count > 0)
                    .Select(definition => CardInstance.CreateInHand(
                        SourceInstanceId(definition.Id), definition.Reference, Brynn))
                    .ToArray();

                var items = withRadiusItem
                    ? HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                    {
                        new HeroItem(
                            Brynn,
                            new CardInstanceId("brynn-guides-radius-item"),
                            HeroDashboard.RulebookLayout.Slots[3].Position,
                            PrintedValueChannel.Radius,
                            1)
                    }))
                    : new HeroItemState(participants);

                return new World(
                    cardId,
                    definitions,
                    instances,
                    participants,
                    board,
                    withOverTheTop
                        ? Ultimate(board, participants, definitions)
                        : modifications ?? RuleModificationSet.Empty,
                    items);
            }

            /// <summary>The live set his unlocked Ultimate supplies while he is the acting hero.</summary>
            private static RuleModificationSet Ultimate(
                AdjacentMoveAuthority board,
                MatchParticipants participants,
                IReadOnlyList<CardDefinition> definitions)
            {
                var overTheTop = definitions.Single(definition =>
                    definition.Id.Equals(BrynnCardContent.OverTheTop));
                return RuleModificationQuery.Build(
                    null,
                    board,
                    participants,
                    TieBreakerState.Unrevealed,
                    UltimateUnlockQuery.Derive(
                        new[] { overTheTop },
                        new[]
                        {
                            new CardInstance(
                                new CardInstanceId("brynn-guides-over-the-top"),
                                overTheTop.Reference,
                                Brynn,
                                new CardStateFacts(
                                    CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                    CardFaceOrientation.FaceUp))
                        }),
                    RuleObject.ForHero(Brynn),
                    null,
                    null);
            }

            private static CardInstanceId SourceInstanceId(CardDefinitionId definitionId) =>
                new CardInstanceId(definitionId.Value + "-source");

            private static AdjacentMoveAuthority Board(
                IReadOnlyList<HeroOccupancy> heroes,
                IReadOnlyList<(MinionId Id, HexCoordinate At, TeamSide Side)> minions,
                IReadOnlyList<HexCoordinate> terrain)
            {
                var playable = Disk(8).Where(space => !terrain.Contains(space)).ToArray();
                return new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrain),
                    heroes,
                    Array.Empty<ObstacleOccupancy>(),
                    minions
                        .Select(minion => new QuickGameMinionSetupState(
                            minion.Id,
                            minion.Side == TeamSide.Blue ? BlueTeam : RedTeam,
                            minion.Side,
                            StandardMinionType.Melee,
                            default,
                            new ConfiguredSpaceReference(
                                Map, new SpaceId($"q{minion.At.Q}_r{minion.At.R}")),
                            minion.At))
                        .ToArray());
            }

            private static IEnumerable<HexCoordinate> Disk(int radius)
            {
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            yield return new HexCoordinate(q, r);
                        }
                    }
                }
            }

            /// <summary>
            /// Rebuilds everything the catalog and the content own, keeping only the frame - which is
            /// what a snapshot carries and what a restore has to be able to resume from.
            /// </summary>
            internal void RebindTrustedContent() => context = BuildContext();

            private EffectResolutionContext BuildContext() =>
                new EffectResolutionContext(
                    cardId.Equals(BrynnCardContent.Decoy)
                        ? BrynnEffectContent.Decoy(cardId, BrynnCardContent.ContentRevision)
                        : BrynnEffectContent.Guide(
                            cardId,
                            BrynnCardContent.ContentRevision,
                            cardId.Equals(BrynnCardContent.MountainGuide) ? 2 : 3),
                    Catalog(),
                    participants,
                    null,
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Skill,
                    components: components,
                    heroItems: items,
                    rosterState: EffectRosterState.Of(
                        Array.Empty<RoundTurnSlot>(),
                        new RoundHeroDefeatHistory(participants),
                        new HeroCoinLedger(participants)));

            /// <summary>
            /// This build's whole synthetic vocabulary plus the two predicates milestone 8.69a still
            /// defers - the guides' "if" names one of them, and a fixture that could not load it could
            /// not prove the faces at all. Naming the DEFERRED list rather than the whole one is
            /// milestone 8.40j's BA-18: a fixture handing back already-registered handlers is refused
            /// for a duplicate reference.
            /// </summary>
            internal static EffectCapabilityCatalog Catalog() =>
                SyntheticEffectCapabilities.CreateCatalog(
                    BrynnComponentContent.ObstacleDeferredHandlers());

            internal EffectResolutionStepResult Run()
            {
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("brynn-guides-resolution"),
                    SourceInstanceId(cardId),
                    definitions.Single(definition => definition.Id.Equals(cardId)).Reference,
                    cardId.Equals(BrynnCardContent.Decoy)
                        ? BrynnCardContent.PrimaryBasicSkill
                        : BrynnCardContent.PrimarySkill,
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

            /// <summary>Through the first performance's optional step and modal into bullet 1.</summary>
            internal PendingEffectChoice ToDecoyMinionBullet()
            {
                var pass = RunToPause();
                var modal = Answer(pass, pass.ConfirmInvocation);
                return Answer(modal, PickArm(modal, BrynnEffectContent.DecoyFirstMinionBulletNode));
            }

            /// <summary>Through the same two steps into bullet 2, or to the modal where it is empty.</summary>
            internal PendingEffectChoice ToDecoyHeroBullet()
            {
                var pass = Run();
                if (pass.IsComplete)
                {
                    return EmptyChoice();
                }

                var modal = Answer(pass.Frame.PendingChoice, pass.Frame.PendingChoice.ConfirmInvocation);
                var arm = modal.SelectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.OptionNodeId.IsDefined &&
                    invocation.Candidate.OptionNodeId.Value ==
                        BrynnEffectContent.DecoyFirstHeroBulletNode);
                return arm == null ? EmptyChoice() : Answer(modal, arm);
            }

            /// <summary>
            /// A pause naming nobody, for the cases that assert a bullet offers nothing at all - a
            /// modal that never offered the arm and an arm that offered no candidate are the same
            /// printed fact, which is that there is nobody to move.
            /// </summary>
            private static PendingEffectChoice EmptyChoice() =>
                new PendingEffectChoice(
                    new EffectResolutionId("brynn-guides-empty"),
                    new EffectChoiceId("brynn-guides-empty-choice"),
                    new EffectNodeId(BrynnEffectContent.DecoyFirstHeroBulletNode + "-target"),
                    Brynn,
                    EffectChoiceKind.SelectTarget,
                    Array.Empty<EffectChoiceCandidate>(),
                    0,
                    0);

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
                return result;
            }

            internal EffectResolutionStepResult SubmitToCompletion(EffectChoiceCommand command)
            {
                var result = Submit(command);
                Assert.That(result.Accepted, Is.True);
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

            internal HexCoordinate Where(HeroId hero) =>
                board.TryGetObjectCoordinate(BoardObjectReference.ForHero(hero), out var at)
                    ? at
                    : throw new InvalidOperationException($"{hero.Value} stands nowhere.");

            internal HexCoordinate Where(MinionId minion) =>
                board.TryGetObjectCoordinate(BoardObjectReference.ForMinion(minion), out var at)
                    ? at
                    : throw new InvalidOperationException($"{minion.Value} stands nowhere.");

            internal bool IsEmpty(HexCoordinate at) => board.IsEmptyPlayableSpace(at);

            /// <summary>Where everything stands, for a case that asserts nothing moved.</summary>
            internal string[] Positions() =>
                new[] { Brynn, Ally, Boxed, Open }
                    .Select(hero => $"{hero.Value}:{Where(hero)}")
                    .Concat(new[]
                        {
                            FriendlyBeside, FriendlyInRadius, EnemyNear, EnemyOther, EnemyFar
                        }
                        .Select(minion => $"{minion.Value}:{Where(minion)}"))
                    .ToArray();
        }
    }
}
