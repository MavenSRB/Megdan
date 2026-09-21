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
    /// Milestone 8.69a. Brynn the Seeker's one printed predicate - "adjacent to 3 or more obstacles" -
    /// as one query every consumer asks, and Over the Top, the Ultimate that makes every enemy hero
    /// satisfy it while he acts.
    ///
    /// <para><b>What the fifteen faces that print it have in common.</b> The phrase reaches this build
    /// four ways and the whole point of the milestone is that all four ask the same question:
    /// <see cref="ObstacleAdjacencyQuery"/>. A modal-Attack bullet narrows by it, an Attack total is
    /// raised by it, a printed-Radius hero selection narrows by it, and two deferred predicates answer
    /// it for card text. A fifth reader - his Ultimate's counts-as overlay - lives INSIDE the query, so
    /// widening it widens all four at once and none of them names an Ultimate.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class BrynnObstacleAdjacencyTests
    {
        private static readonly HeroId Brynn = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MapId Map = new MapId("brynn-obstacle-adjacency");

        /// <summary>The space every case measures from, well inside the synthetic disk.</summary>
        private static readonly HexCoordinate Subject = new HexCoordinate(0, 0);

        /// <summary>Its six neighbours, in <c>HexCoordinate.GetNeighbors</c>'s own order.</summary>
        private static readonly HexCoordinate[] Around = Subject.GetNeighbors().ToArray();

        /// <summary>Far enough from <see cref="Subject"/> to touch none of its neighbours.</summary>
        private static readonly HexCoordinate FarAway = new HexCoordinate(3, 0);

        // -------------------------------------------------------------------------------------------
        // The threshold
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE PRINTED NUMBER. Two obstacles beside a hero is not three, and the clause says three.
        ///
        /// <para>Measured as a difference on one board rather than asserted as a property: the same
        /// space is read twice and the only thing that changes between the readings is how many of its
        /// six neighbours hold something.</para>
        /// </summary>
        [Test]
        public void ThePredicate_IsAThresholdOfThreeOfTheSixNeighbours_AndTwoIsNotThree()
        {
            var two = Board(
                heroes: new[] { At(Brynn, FarAway), At(Enemy, Subject) },
                terrain: new[] { Around[0], Around[1] });

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    two, Enemy, RuleModificationSet.Empty),
                Is.False,
                "Two of the six neighbouring spaces hold an obstacle. The printed clause is \"3 or " +
                "more\", so two is the clause not being met rather than a smaller version of it.");

            var three = Board(
                heroes: new[] { At(Brynn, FarAway), At(Enemy, Subject) },
                terrain: new[] { Around[0], Around[1], Around[2] });

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    three, Enemy, RuleModificationSet.Empty),
                Is.True,
                "and one more obstacle on a third neighbouring space is the whole difference.");

            Assert.That(ObstacleAdjacencyQuery.PrintedObstacleThreshold, Is.EqualTo(3),
                "The number is stated once, beside the rule, because all fifteen of his faces print " +
                "the same three and a consumer writing its own would be a second place it could " +
                "disagree.");
        }

        /// <summary>
        /// EVERY KIND OF OBSTACLE p.18 lists counts, and each is measured on its own so that a reading
        /// which silently dropped one would fail here rather than in a Play Mode journey.
        ///
        /// <para>The Gold basic's own reminder is the list - "( You, other heroes, minions, tokens and
        /// terrain are obstacles )" - and p.19 adds the two the reminder leaves implicit: "Any board
        /// space without a full grid outline counts as terrain and is an obstacle, including the water
        /// spaces surrounding the board", plus milestone 8.7's overlay, which makes a space count as
        /// one while a qualifying hero acts.</para>
        /// </summary>
        [Test]
        public void EveryObstacleKindCounts_IncludingBrynnHimselfAndAnOverlaidSpace()
        {
            // Brynn on one neighbour, an enemy hero's ally on a second, a minion on a third: three
            // units, and one of them is the hero whose own card prints the reminder.
            var units = Board(
                heroes: new[]
                {
                    At(Brynn, Around[0]), At(Enemy, Subject), At(SecondEnemy, Around[1])
                },
                minions: new[] { Minion(Around[2]) });

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    units, Enemy, RuleModificationSet.Empty),
                Is.True,
                "\"You, other heroes, minions ... are obstacles\" - Brynn himself is one of the three, " +
                "which is what the Gold's own parenthesis says and BR-Q1 records.");

            // Printed terrain on two, a token on the third.
            var terrainAndToken = Board(
                heroes: new[] { At(Brynn, FarAway), At(Enemy, Subject) },
                terrain: new[] { Around[0], Around[1] });
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    terrainAndToken, Enemy, RuleModificationSet.Empty),
                Is.False,
                "Two printed terrain spaces are two obstacles.");
            PlaceToken(terrainAndToken, Around[2]);
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    terrainAndToken, Enemy, RuleModificationSet.Empty),
                Is.True,
                "and a token placed on a third neighbour is the third - \"tokens ... are obstacles\".");

            // Milestone 8.7's overlay: a space nothing stands on, closed while a qualifying hero acts.
            var overlaid = Board(
                heroes: new[] { At(Brynn, FarAway), At(Enemy, Subject) },
                terrain: new[] { Around[0], Around[1] });
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    overlaid, Enemy, WithOverlayOver(Around[2])),
                Is.True,
                "A space an overlay has made an obstacle is one, because this query asks " +
                "ObstacleQuery rather than reading occupancy itself - which is the whole reason p.18's " +
                "classification has one owner.");
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    overlaid, Enemy, RuleModificationSet.Empty),
                Is.False,
                "and the identical board without the overlay is two, so the overlay is what moved it.");
        }

        /// <summary>
        /// BR-Q13, stated explicitly because the user is being asked to confirm it: p.19 makes the
        /// water surrounding the board terrain and therefore an obstacle, so a hero on the map's rim
        /// often satisfies the clause from the rim alone.
        /// </summary>
        [Test]
        public void TheWaterRingCounts_SoAHeroOnTheRimNeedsOnlyTwoNeighboursOfItsOwn()
        {
            // A one-ring board: the origin and its six neighbours, and nothing else. A hero standing
            // on a rim space has three neighbours ON the map and three off it; Brynn stands on the
            // opposite rim, two spaces away, so nothing of his is beside the hero being measured.
            var rim = Around[0];
            var board = Board(
                heroes: new[] { At(Brynn, Around[3]), At(Enemy, rim) },
                radius: 1);

            Assert.That(
                rim.GetNeighbors().Count(neighbor =>
                    ObstacleQuery.IsObstacle(board, neighbor, RuleModificationSet.Empty)),
                Is.EqualTo(3),
                "Three of the rim space's six neighbours are off the map, and p.19 calls the water " +
                "surrounding the board terrain and an obstacle.");
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, Enemy, RuleModificationSet.Empty),
                Is.True,
                "so a hero on the edge satisfies the printed clause with nothing standing beside " +
                "them at all - BR-Q13, built to the printed rule.");

            var inland = Board(
                heroes: new[] { At(Brynn, new HexCoordinate(2, 0)), At(Enemy, Subject) },
                radius: 2);
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    inland, Enemy, RuleModificationSet.Empty),
                Is.False,
                "and a hero one space in, with all six neighbours on the map and empty, does not.");
        }

        /// <summary>
        /// p.18: "Effects that check for unit presence, or count units, ... still count immune units."
        /// Asking this question targets nobody (p.19), so immunity never enters it.
        /// </summary>
        [Test]
        public void ImmuneUnitsAreCounted_BecauseAskingTargetsNobody()
        {
            var board = Board(
                heroes: new[]
                {
                    At(Brynn, Around[0]), At(Enemy, Subject), At(SecondEnemy, Around[1])
                },
                minions: new[] { Minion(Around[2]) });

            var immune = new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        RuleModification.ForImmunity(RuleActivity.All),
                        RuleObject.ForHero(SecondEnemy),
                        new ContinuousRuleSource(
                            "immunity-probe", ContinuousRuleSourceKind.Ultimate, SecondEnemy,
                            "probe", UltimateRuleDefinition.ApplicabilityKind,
                            new CardInstanceId("immunity-probe-card")),
                        TeamSide.Blue,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(board, Enemy, immune),
                Is.True,
                "One of the three obstacles is a hero immune to everything, and the count is still " +
                "three: p.18 counts immune units in a presence check, and p.19 says an effect that " +
                "checks presence does not target.");
        }

        /// <summary>
        /// The subject's own space is never one of its neighbours, and a hero standing nowhere is not
        /// adjacent to anything.
        /// </summary>
        [Test]
        public void TheSubjectsOwnSpaceIsNotCounted_AndAnAbsentHeroAnswersFalse()
        {
            var board = Board(
                heroes: new[] { At(Brynn, FarAway), At(Enemy, Subject) },
                terrain: new[] { Around[0], Around[1] });

            Assert.That(
                ObstacleAdjacencyQuery.IsAdjacentToThreeOrMoreObstacles(
                    board, Subject, WithOverlayOver(Subject)),
                Is.False,
                "p.17's Adjacent is the six-space ring, so a space an overlay closed UNDER the " +
                "subject is not one of the subject's own neighbours.");

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, SecondEnemy, RuleModificationSet.Empty),
                Is.False,
                "and a hero who is not standing on this board answers false rather than throwing - " +
                "a defeated hero is not a candidate, and reporting the clause about them would make " +
                "an absent hero the most eligible target there is.");
        }

        // -------------------------------------------------------------------------------------------
        // Familiar Ground
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The Gold basic's two bullets, read out of the composed definition by the same query the
        /// offer-feasibility gate uses. Asserted as a literal table rather than derived, for the reason
        /// every verified set's printed-value table is literal: a case that read its expectations out
        /// of the definition would pass whatever the definition happened to say.
        /// </summary>
        [Test]
        public void FamiliarGround_DeclaresTwoBullets_AndOnlyTheSecondNamesTheObstacles()
        {
            var declared = RequestedAttackSequenceQuery.Declared(FamiliarGroundText());

            Assert.That(declared, Has.Count.EqualTo(2),
                "\"Choose one\" prints exactly two bullets, each one attack-sequence leaf under the " +
                "modal.");

            Assert.That(declared[0].BulletId,
                Is.EqualTo(BrynnEffectContent.FamiliarGroundAdjacentBulletNode));
            Assert.That(declared[0].TargetPolicy.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesAndMinions),
                "\"Target a UNIT adjacent to you\" - p.19's units, heroes and minions both.");
            Assert.That(declared[0].TargetPolicy.Reach, Is.EqualTo(AttackTargetReach.AdjacentOnly));
            Assert.That(declared[0].SubjectNarrowing,
                Is.EqualTo(AttackSequenceSubjectNarrowing.None),
                "The first bullet qualifies nobody and says so by declaring nothing.");

            Assert.That(declared[1].BulletId,
                Is.EqualTo(BrynnEffectContent.FamiliarGroundObstacleBulletNode));
            Assert.That(declared[1].TargetPolicy.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesOnly),
                "\"Target a HERO in range\" - heroes only is the sentence's own noun.");
            Assert.That(declared[1].TargetPolicy.Reach,
                Is.EqualTo(AttackTargetReach.PrintedRange),
                "\"in range\" is p.13's ceiling read off the card's printed Range 3 icon, which is " +
                "what makes a Range item raise the face that prints one.");
            Assert.That(declared[1].SubjectNarrowing,
                Is.EqualTo(AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles),
                "\"who is adjacent to 3 or more obstacles\" is the clause this block exists for.");

            var modal = FamiliarGroundText().Nodes.Single(node =>
                node.Id.Equals(FamiliarGroundText().RootNodeId));
            Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose));
            Assert.That(modal.MinimumSelections, Is.EqualTo(1));
            Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                "\"Choose one\" is 1..1 - p.10's modal read literally, and BR-Q3's reading: one " +
                "bullet is taken and the other ignored.");
        }

        /// <summary>
        /// The narrowing is a CLOSED VOCABULARY the persisted bullet names, and the fourth member kept
        /// every earlier value. A domain that silently grew would be a domain nobody was watching, so
        /// this is the one place it is told by hand.
        /// </summary>
        [Test]
        public void TheNarrowing_IsTheFifthMemberAndRenumbersNone()
        {
            Assert.That((int)AttackSequenceSubjectNarrowing.None, Is.EqualTo(0));
            Assert.That((int)AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard,
                Is.EqualTo(1));
            Assert.That((int)AttackSequenceSubjectNarrowing.HasAnEmptyDiscard, Is.EqualTo(2));
            Assert.That((int)AttackSequenceSubjectNarrowing.HoldsTheActorsMarker, Is.EqualTo(3));
            Assert.That((int)AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles,
                Is.EqualTo(4),
                "The enum grew by one at the end, so no payload written before this milestone " +
                "decodes as a different predicate.");

            var parameter = SyntheticSequenceCapabilities.Handlers().Single().Schema.Parameters
                .Single(value =>
                    value.Name == SyntheticSequenceCapabilities.SubjectNarrowingParameter);
            Assert.That(parameter.IsRequired, Is.False);
            Assert.That(parameter.IntegerDomain.LegalValues,
                Contains.Item((int)AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles),
                "`EffectIntegerDomain.All<T>()` reads the enum TYPE, so the parameter admitted the " +
                "new member without the schema being touched.");
        }

        // -------------------------------------------------------------------------------------------
        // The +2 at the Attack total
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE +2, measured through the production entry point that composes p.14's Attack Total, on
        /// three targets that differ in exactly one thing each.
        /// </summary>
        [Test]
        public void TheRaise_AppliesToABoxedInHero_AndToNeitherAMinionNorAHeroInTheOpen()
        {
            var board = Board(
                heroes: new[] { At(Brynn, Around[0]), At(Enemy, Subject) },
                terrain: new[] { Around[1], Around[2] },
                minions: new[] { Minion(FarAway) });

            Assert.That(AmountFor(board, AttackTarget.Hero(Enemy)), Is.EqualTo(2),
                "\"If you target a hero adjacent to 3 or more obstacles, +2 attack\" - Brynn himself " +
                "plus two terrain spaces is three, and the printed amount beside the clause is the " +
                "whole bonus.");

            Assert.That(AmountFor(board, AttackTarget.Minion(MinionId)), Is.EqualTo(0),
                "A minion standing in the same board is a legal target of \"Target a unit adjacent " +
                "to you\" and raises nothing: the conditional clause prints \"a HERO\".");

            var open = Board(
                heroes: new[] { At(Brynn, Around[0]), At(Enemy, Subject) },
                terrain: new[] { Around[1] });
            Assert.That(AmountFor(open, AttackTarget.Hero(Enemy)), Is.EqualTo(0),
                "and the same hero with two obstacles beside them instead of three raises nothing " +
                "at all, which isolates the clause to one degree of freedom.");
        }

        /// <summary>
        /// BR-Q6: the contribution is computed at the Attack total, so a Defense text that moves the
        /// defender between p.14's step 4 and step 6 changes it. Measured as the query answering
        /// differently for the same declaration on the same card once the board moves.
        /// </summary>
        [Test]
        public void TheRaise_IsReadLive_SoMovingTheDefenderBetweenStepsChangesIt()
        {
            var board = Board(
                heroes: new[] { At(Brynn, Around[0]), At(Enemy, Subject) },
                terrain: new[] { Around[1], Around[2] });

            Assert.That(AmountFor(board, AttackTarget.Hero(Enemy)), Is.EqualTo(2));

            Assert.That(
                board.CommitValidatedDisplacement(
                    BoardObjectReference.ForHero(Enemy), FarAway).Accepted,
                Is.True,
                "The defender is pushed out of the corner, which is what a Defense text may do " +
                "between p.14's steps 4 and 6.");

            Assert.That(AmountFor(board, AttackTarget.Hero(Enemy)), Is.EqualTo(0),
                "and the Attack Total composed after that move carries no raise - the clause is read " +
                "at the total rather than recorded when the target was bound (BR-Q6).");
        }

        /// <summary>
        /// The ninth query kind is appended without renumbering the eight before it, so no payload
        /// written earlier decodes as a different question.
        /// </summary>
        [Test]
        public void TheValueQuery_IsTheNinthKind_AndPairsOnlyWithTheAttackChannel()
        {
            Assert.That(
                (int)RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                Is.EqualTo(9));

            Assert.That(
                () => new RosterValueContributionDefinition(
                    PrintedValueChannel.Attack,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2),
                Throws.Nothing,
                "The Attack channel is the one his three faces print the clause on.");

            Assert.That(
                () => new RosterValueContributionDefinition(
                    PrintedValueChannel.Range,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2),
                Throws.ArgumentException,
                "Dynamic Range is derived before an Attack target exists, so a query about the bound " +
                "target cannot answer for it.");

            Assert.That(
                () => new RosterValueContributionDefinition(
                    PrintedValueChannel.Defense,
                    RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles,
                    2),
                Throws.ArgumentException,
                "and the Defense channel is paired with Dodger's threshold and nothing else.");
        }

        // -------------------------------------------------------------------------------------------
        // The radius-hero selection narrowing
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "an enemy hero in radius who is adjacent to 3 or more obstacles" as a SELECTION narrowing:
        /// two enemy heroes in the same disk, and only the boxed-in one is offered.
        /// </summary>
        [Test]
        public void TheRadiusSelection_OffersOnlyTheBoxedInEnemyHero()
        {
            var board = Board(
                heroes: new[]
                {
                    At(Brynn, Around[0]), At(Enemy, Subject), At(SecondEnemy, new HexCoordinate(2, 0))
                },
                terrain: new[] { Around[1], Around[2] });

            Assert.That(
                ChosenHeroes(board, narrowed: false),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }),
                "Both enemy heroes stand inside the printed Radius, which is what makes the case " +
                "below about the clause and about nothing else.");

            Assert.That(
                ChosenHeroes(board, narrowed: true),
                Is.EqualTo(new[] { Enemy.Value }),
                "\"who is adjacent to 3 or more obstacles\" narrows who may be PICKED, so the hero " +
                "standing in the open is never offered rather than offered and then refused.");
        }

        /// <summary>
        /// The narrowing is an optional BOOLEAN, on the Radius arm alone - the shape milestone 8.57c
        /// settled, and for the reason it settled it: an unbound optional Integer reads as zero, and a
        /// threshold of zero admits every hero on the board.
        /// </summary>
        [Test]
        public void TheRadiusNarrowing_IsAnOptionalBooleanAndIsAbsentFromTheRangeArm()
        {
            var radius = HeroSelectionHandler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var parameter = radius.Schema.Parameters.Single(value =>
                value.Name ==
                SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName);

            Assert.That(parameter.ValueKind, Is.EqualTo(EffectValueKind.Boolean),
                "A Boolean, never an Integer: an unbound optional Integer reads as zero, and a " +
                "selection that forgot to bind a threshold of zero would silently admit everybody.");
            Assert.That(parameter.IsRequired, Is.False,
                "Optional, so every hero selection composed before him keeps its exact candidate set " +
                "without naming it.");

            var range = HeroSelectionHandler(SyntheticTargetingCapabilities.RangeHeroTargetV1);
            Assert.That(
                range.Schema.Parameters.Any(value =>
                    value.Name ==
                    SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName),
                Is.False,
                "and it is declared on the Radius arm alone, by this file's standing rule: every face " +
                "of his printing the clause beside a reach prints a Radius icon, and his one Range " +
                "face states it as a modal-Attack bullet instead.");
        }

        // -------------------------------------------------------------------------------------------
        // The deferred predicate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "If an enemy hero in radius is adjacent to 3 or more obstacles" - true and false on one
        /// board, and widened by a p.16 Radius item rather than by anything the card says.
        /// </summary>
        [Test]
        public void ThePresencePredicate_AnswersTheDisk_AndAPrintedRadiusItemWidensIt()
        {
            // The only boxed-in enemy hero stands four spaces away: outside the printed Radius 3 and
            // inside the Radius 4 one item makes of it.
            var far = new HexCoordinate(4, 0);
            var board = Board(
                heroes: new[] { At(Brynn, Subject), At(Enemy, far) },
                terrain: far.GetNeighbors().Take(3).ToArray(),
                radius: 5);

            Assert.That(PresentInRadius(board, withRadiusItem: false), Is.False,
                "\"in radius\" is p.9's printed icon, and the one enemy hero who satisfies the " +
                "clause is standing outside the disk the card prints.");
            Assert.That(PresentInRadius(board, withRadiusItem: true), Is.True,
                "and p.16's item raises that icon by one, which reaches him - the same composition " +
                "every printed-icon consumer performs, rather than a number this predicate invents.");
        }

        // MILESTONE 8.69F DROPPED THIS BLOCK'S SECOND PREDICATE and the case that drove it. 8.69a
        // minted `brynn-hero-adjacent-to-obstacles@1` for the three split faces' gate, and 8.69b
        // measured that their subject is the TARGETING rather than a hero - p.14 removes a defeated
        // target at step 6, so a predicate reading the live hero refuses Peak Precision's retrieval
        // exactly when the attack worked. No printed sentence of his asks about one named hero, so
        // the composition block dropped a contract nothing could start rather than leaving the dealing
        // a handler type to register and find a showcase consumer for. `BrynnPrintedSetTests` asserts
        // the three ids that remain, as a set, from both sides.

        // -------------------------------------------------------------------------------------------
        // Over the Top
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE ULTIMATE'S HEADLINE MEASUREMENT. On one board, at one instant, the same two enemy heroes
        /// satisfy the clause while Brynn acts and satisfy nothing while anybody else does.
        /// </summary>
        [Test]
        public void OverTheTop_MakesEveryEnemyHeroQualifyWhileHeActs_AndNobodyWhileAnotherHeroDoes()
        {
            var board = OpenBoard();

            var whileHeActs = Rules(board, RuleObject.ForHero(Brynn));
            foreach (var enemy in new[] { Enemy, SecondEnemy })
            {
                Assert.That(
                    ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                        board, enemy, whileHeActs),
                    Is.True,
                    $"\"all enemy heroes in play count as adjacent to 3 or more obstacles\" - {enemy} " +
                    "is standing alone on an empty board and satisfies the clause anyway, because " +
                    "p.17's Counts As is an answer rather than an addition to a count.");
            }

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, Brynn, whileHeActs),
                Is.False,
                "and Brynn is not his own enemy, so the Ultimate says nothing about him.");

            var whileTheEnemyActs = Rules(board, RuleObject.ForHero(Enemy));
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, SecondEnemy, whileTheEnemyActs),
                Is.False,
                "\"While YOU are performing actions\" - the identical board with somebody else acting " +
                "carries none of it (BR-Q2).");

            var whileNobodyActs = Rules(board, RuleObject.None);
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    board, Enemy, whileNobodyActs),
                Is.False,
                "and a set derived at a round boundary, with nobody acting at all, carries none of " +
                "it either - which is the printed condition failing rather than an omission.");
        }

        /// <summary>
        /// MILESTONE 8.69H. "All enemy heroes in play" is the MATCH's enemy heroes rather than the
        /// board's standing pieces, and the difference is one that only rulebook p.13's Defeat can
        /// show: an enemy hero the page has removed still counts while Brynn acts.
        ///
        /// <para><b>It is a correction rather than a widening for its own sake.</b> The one consumer
        /// that can ask about a hero the board no longer holds is the split faces' targeting gate,
        /// whose whole contract is to answer about the target as the Attack bound it (BR-Q5, BR-Q14b) -
        /// and whose fallback reads the captured SPACE, which cannot carry a hero-scoped counts-as. Read
        /// off the board, Peak Precision applied its printed +2 at p.14's step 4 and refused its
        /// printed retrieval at step 7 on the same printed condition, exactly when the attack
        /// succeeded. Every other consumer derives its candidates from the board, so a hero standing
        /// nowhere is not among them to be widened.</para>
        /// </summary>
        [Test]
        public void OverTheTop_StillCountsAnEnemyHeroPage13HasTakenOffTheBoard()
        {
            var standing = OpenBoard();
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    standing, Enemy, Rules(standing, RuleObject.ForHero(Brynn))),
                Is.True,
                "Arrange: he satisfies the clause through the Ultimate and nothing else.");

            var defeated = Board(
                heroes: new[] { At(Brynn, Subject), At(SecondEnemy, new HexCoordinate(0, 2)) },
                radius: 4);
            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    defeated, Enemy, Rules(defeated, RuleObject.ForHero(Brynn))),
                Is.True,
                "and the same hero, now removed from the board, still carries the overlay while " +
                "Brynn acts - which is what lets a clause read at targeting be answered after " +
                "p.14's step 6.");

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    defeated, Enemy, Rules(defeated, RuleObject.ForHero(SecondEnemy))),
                Is.False,
                "\"While YOU are performing actions\" is untouched: with somebody else acting the " +
                "overlay is not derived at all, for a removed hero as for a standing one.");

            Assert.That(
                ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    defeated, Enemy, RuleModificationSet.Empty),
                Is.False,
                "and without the overlay a hero standing nowhere answers false exactly as before - " +
                "the geometry branch still reads the board.");
        }

        /// <summary>
        /// The overlay is addressed to heroes, so minions are untouched - p.17's Counts As changes
        /// nothing else about anybody, and the sentence names heroes.
        /// </summary>
        [Test]
        public void OverTheTop_TouchesNoMinionAndNothingElseAboutTheHeroesItReaches()
        {
            var board = OpenBoard(withMinion: true);
            var rules = Rules(board, RuleObject.ForHero(Brynn));

            Assert.That(
                rules.CountsAsAdjacentToThreeOrMoreObstacles(RuleObject.ForMinion(MinionId)),
                Is.False,
                "\"all enemy HEROES in play\" - an enemy minion standing beside them is not one.");
            Assert.That(
                rules.CountsAs(RuleObject.ForHero(Enemy), EffectiveUnitKind.Terrain),
                Is.False,
                "and p.17's replacement is untouched: the hero the clause reaches is still a hero on " +
                "still their own team, which is why this is a family of its own rather than a " +
                "counts-as of a unit TYPE.");
            Assert.That(
                rules.OverlaidEffectiveUnitKinds(RuleObject.ForHero(Enemy)), Is.Empty,
                "and nothing has been added to their type either.");
        }

        /// <summary>
        /// Every consumer of the printed phrase this block owns sees the Ultimate at once, because the
        /// overlay lives inside the one query rather than beside each of them. (8.69b's gate and
        /// 8.69d's swap selection are the other two, each measured in its own block's fixture, and
        /// <c>BrynnPrintedSetTests</c> asks three of his faces at once on one board.)
        /// </summary>
        [Test]
        public void OverTheTop_WidensTheSelection_ThePresenceAndTheRaise()
        {
            var board = OpenBoard();
            var rules = Rules(board, RuleObject.ForHero(Brynn));

            Assert.That(
                ChosenHeroes(board, narrowed: true, participants: ThreeHeroes(), modifications: rules)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { Enemy.Value, SecondEnemy.Value }
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()),
                "The radius-hero selection offers both enemy heroes on a board with no obstacle on " +
                "it at all.");

            Assert.That(PresentInRadius(board, withRadiusItem: false, modifications: rules), Is.True,
                "the presence predicate holds,");

            Assert.That(AmountFor(board, AttackTarget.Hero(Enemy), rules, ThreeHeroes()),
                Is.EqualTo(2),
                "and the Attack Total carries the +2 - three consumers widened by one declaration " +
                "none of them names.");
        }

        /// <summary>
        /// The declaration is DERIVED and stored nowhere, which is what makes it survive a snapshot, a
        /// replay and an Undo without the payload carrying a byte of it: the same board and the same
        /// unlocked card produce the same set again, and a board the hero has left produces none.
        /// </summary>
        [Test]
        public void OverTheTop_IsDerivedFromTheUnlockedCardAndTheBoard_SoNothingPersistsIt()
        {
            var board = OpenBoard();
            var first = Rules(board, RuleObject.ForHero(Brynn));
            var second = Rules(board, RuleObject.ForHero(Brynn));

            Assert.That(
                second.Modifications.Select(applied => applied.ToString()).ToArray(),
                Is.EqualTo(first.Modifications.Select(applied => applied.ToString()).ToArray()),
                "Two derivations of the same state produce the same declarations in the same order, " +
                "which is what a restore, a replay and an Undo each re-run.");
            Assert.That(first.Modifications, Has.Count.EqualTo(2),
                "one per enemy hero standing on the board, which is the expansion the derivation " +
                "performs because it is what holds the participants.");
            Assert.That(
                first.Modifications.All(applied =>
                    applied.Modification.Kind ==
                        RuleModificationKind.CountsAsAdjacentToThreeOrMoreObstacles &&
                    applied.SourceKind == RuleModificationSourceKind.ContinuousSource),
                Is.True,
                "and each is a continuous source rather than an Active effect, so nothing about it " +
                "is written into a snapshot at all - p.16's ability lives exactly as long as the card " +
                "beside the dashboard and the hero on the board.");

            var withoutHim = Board(heroes: new[] { At(Enemy, Subject), At(SecondEnemy, FarAway) });
            Assert.That(
                Rules(withoutHim, RuleObject.ForHero(Brynn)).Modifications, Is.Empty,
                "and a hero who has left the board supplies no Ultimate at all, which is p.16's own " +
                "parenthesis - \"as long as your hero is on the board\".");
        }

        /// <summary>
        /// The applicability member the Ultimate carries is a third one, and it does not widen the
        /// basic-action member Whisper's Grim Reaper reads.
        /// </summary>
        [Test]
        public void TheApplicability_IsAThirdMemberAndLeavesTheBasicActionOneAlone()
        {
            Assert.That((int)RuleValueApplicability.Always, Is.EqualTo(0));
            Assert.That((int)RuleValueApplicability.ControllerBasicAttackAction, Is.EqualTo(1));
            Assert.That((int)RuleValueApplicability.ControllerBasicAction, Is.EqualTo(2));
            Assert.That((int)RuleValueApplicability.ControllerAction, Is.EqualTo(3),
                "Appended, so no declaration written before this milestone changed what it says.");

            var declared = BrynnEffectContent.OverTheTopModifications.Single();
            Assert.That(declared.Kind,
                Is.EqualTo(RuleModificationKind.CountsAsAdjacentToThreeOrMoreObstacles));
            Assert.That(declared.ValueApplicability,
                Is.EqualTo(RuleValueApplicability.ControllerAction),
                "\"While you are performing actions\" names no class of action, and " +
                "ControllerBasicAction is p.9's Basic supertype - which fourteen of the fifteen faces " +
                "his Ultimate is about are not.");
            Assert.That(declared.Amount, Is.Zero);
            Assert.That(declared.Channel, Is.EqualTo(PrintedValueChannel.None));
            Assert.That(declared.Activities, Is.Empty,
                "and it carries no amount, channel or activity, because a public property added to " +
                "RuleModification moves the content digest of every hero whose declarations reach it.");

            Assert.That((int)RuleModificationKind.RemovedWhenAnEnemyHeroMovesThrough, Is.EqualTo(17));
            Assert.That((int)RuleModificationKind.CountsAsAdjacentToThreeOrMoreObstacles,
                Is.EqualTo(18),
                "The eighteenth family, appended without renumbering the seventeen above it.");
        }

        // -------------------------------------------------------------------------------------------
        // What the four faces print, and what this build refuses
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The four faces this block composes, as the shard's printed-inventory table states them.
        /// Literal rather than derived, for the reason every verified set's table is literal.
        /// </summary>
        [Test]
        public void TheFourFaces_CarryThePrintedValuesTheShardRecords()
        {
            var familiarGround = Face(BrynnCardContent.FamiliarGround);
            Assert.That(familiarGround.Initiative.Value, Is.EqualTo(12));
            Assert.That(familiarGround.Progression.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(familiarGround.Progression.Classification,
                Is.EqualTo(CardClassification.Basic));
            Assert.That(PrintedValue(familiarGround, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(2));
            Assert.That(PrintedValue(familiarGround, ActionKind.Attack, PrintedValueChannel.Range),
                Is.EqualTo(3));
            Assert.That(PrintedValue(familiarGround, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(2));
            Assert.That(
                PrintedValue(familiarGround, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(1));
            Assert.That(
                familiarGround.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Attack).AttackRange,
                Is.EqualTo(AttackRangeKind.Ranged),
                "\"BASIC ATTACK - RANGED\" is the printed band, which is what makes its Range icon " +
                "p.13's ceiling.");

            var highGround = Face(BrynnCardContent.HighGround);
            Assert.That(highGround.Initiative.Value, Is.EqualTo(9));
            Assert.That(highGround.Progression.Tier, Is.EqualTo(CardTier.I));
            Assert.That(highGround.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.None),
                "BR-06: his Tier I Red prints no item icon, and a missing icon is absent rather " +
                "than zero.");
            Assert.That(PrintedValue(highGround, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(4),
                "The printed \"4+\" is a 4 with a card-text raise beside it, which is what p.14's own " +
                "note on the plus sign says.");
            Assert.That(PrintedValue(highGround, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(6));
            Assert.That(PrintedValue(highGround, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(4));
            Assert.That(
                highGround.ActionDefinitions
                    .Single(action => action.Kind == ActionKind.Attack).AttackRange,
                Is.EqualTo(AttackRangeKind.NonRanged),
                "and it prints no Range or Radius at all, so p.13's adjacency is the whole reach.");

            var elevatedAmbush = Face(BrynnCardContent.ElevatedAmbush);
            Assert.That(elevatedAmbush.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(elevatedAmbush.Progression.ItemIcon,
                Is.EqualTo(PrintedValueChannel.Initiative));
            Assert.That(PrintedValue(elevatedAmbush, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(5));
            Assert.That(
                highGround.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[]
                {
                    BrynnCardContent.ElevatedAmbush, BrynnCardContent.SplitAttack
                }),
                "p.15 forks the Red ladder; while 8.69b was being written on another branch this row " +
                "named only Elevated Ambush, and milestone 8.69f completed it.");

            var overTheTop = Face(BrynnCardContent.OverTheTop);
            Assert.That(overTheTop.Progression.Classification,
                Is.EqualTo(CardClassification.Ultimate));
            Assert.That(overTheTop.ActionDefinitions, Is.Empty,
                "It prints no Initiative, no icons and no action, so it is never played and never " +
                "discarded to defend.");
            Assert.That(overTheTop.UltimateRule, Is.Not.Null);
            Assert.That(overTheTop.UltimateRule.Modifications.Count, Is.EqualTo(1),
                "and the whole of its content is the one standing declaration.");
        }

        /// <summary>
        /// Milestone 7.9's gate, asserted from BOTH sides AND FROM THE FAR END OF IT: milestone 8.69g
        /// dealt him, so the one contract this block declares is now REGISTERED, the deferral list is
        /// empty, and the two facts are one claim rather than two.
        ///
        /// <para>The case is deliberately kept rather than deleted with the deferral it used to
        /// assert. What it guards now is the other direction - a contract of his quietly falling OUT
        /// of the registration list, or a second declaration appearing in this block and reaching the
        /// build without anybody deciding to register it - and that is the failure a dealing makes
        /// possible rather than one it closes.</para>
        /// </summary>
        [Test]
        public void EveryContractOfHisIsRegisteredByThisBuild_AndTheDeferralListIsEmpty()
        {
            var registeredHere = BrynnComponentContent.ObstacleRegisteredHandlers()
                .Select(handler => handler.Reference)
                .ToArray();

            Assert.That(
                registeredHere.Select(reference => reference.CapabilityId.Value).ToArray(),
                Is.EqualTo(new[]
                {
                    BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstacles.Value
                }),
                "The ONE predicate this block still declares. It minted a second - " +
                "'brynn-hero-adjacent-to-obstacles' - which 8.69b superseded and 8.69f dropped, " +
                "because no printed sentence of his asks about a named hero and a capability nothing " +
                "can start is not a registration a dealing should make.");
            Assert.That(BrynnComponentContent.ObstacleDeferredHandlers(), Is.Empty,
                "and nothing of his is withheld any more, because 8.69g is the block that dealt him " +
                "and registration and dealing land in ONE change - Silverarrow's 8.35z46 law.");

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities;
            foreach (var reference in registeredHere)
            {
                Assert.That(registered.IsRegistered(reference), Is.True,
                    $"'{reference}' is named by four faces a configured match now deals - Mountain " +
                    "Guide, Expedition Leader, True Grit and Die Hard - so a match that could not " +
                    "start it would grey out every one of them.");
            }

            Assert.That(
                BrynnCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "and milestone 8.69f states his set: the eighteen p.6 deals, with the boxed Handicap " +
                "the nineteenth. With the registration in place the dealt-corpus validator refuses " +
                "NONE of them, which is the half of 8.57f's precedent a dealing discharges.");
            Assert.That(BrynnCardContent.ObstacleDefinitions, Has.Count.EqualTo(4),
                "while the four faces this block composes are still exactly four.");
            Assert.That(
                BrynnCardContent.AllDefinitions.Select(definition => definition.Id.Value).ToArray(),
                Is.SupersetOf(
                    BrynnCardContent.ObstacleDefinitions
                        .Select(definition => definition.Id.Value).ToArray()),
                "and every one of them is visible to the content sweeps that read the aggregate - " +
                "which is the claim rather than the aggregate's SIZE, because each wave block of his " +
                "series appends its own faces to it.");
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static readonly MinionId MinionId = new MinionId("brynn-obstacle-minion");

        private static MatchParticipants ThreeHeroes() =>
            StagedBoardParticipants.CreateWithBlueAlly();

        private static HeroOccupancy At(HeroId heroId, HexCoordinate coordinate) =>
            new HeroOccupancy(heroId, coordinate);

        private static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HexCoordinate> terrain = null,
            IEnumerable<QuickGameMinionSetupState> minions = null,
            int radius = 4)
        {
            var terrainSpaces = (terrain ?? Array.Empty<HexCoordinate>()).ToArray();
            var playable = Disk(radius)
                .Where(space => !terrainSpaces.Contains(space))
                .ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, playable, terrainSpaces),
                heroes.ToArray(),
                Array.Empty<ObstacleOccupancy>(),
                (minions ?? Array.Empty<QuickGameMinionSetupState>()).ToArray());
        }

        /// <summary>An empty board with no obstacle of any kind on it, for the Ultimate's cases.</summary>
        private static AdjacentMoveAuthority OpenBoard(bool withMinion = false) =>
            Board(
                heroes: new[]
                {
                    At(Brynn, Subject),
                    At(Enemy, new HexCoordinate(2, 0)),
                    At(SecondEnemy, new HexCoordinate(0, 2))
                },
                minions: withMinion
                    ? new[] { Minion(new HexCoordinate(2, -2)) }
                    : Array.Empty<QuickGameMinionSetupState>(),
                radius: 4);

        private static IEnumerable<HexCoordinate> Disk(int radius)
        {
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius) yield return new HexCoordinate(q, r);
                }
            }
        }

        private static QuickGameMinionSetupState Minion(HexCoordinate coordinate) =>
            new QuickGameMinionSetupState(
                MinionId,
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("brynn-obstacle-spawn")),
                new ConfiguredSpaceReference(
                    Map, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        private static void PlaceToken(AdjacentMoveAuthority board, HexCoordinate coordinate)
        {
            var catalog = SyntheticComponentDefinitions.CreateCatalog();
            board.CommitValidatedTokenPlacement(new PlacedToken(
                new TokenInstanceId($"brynn-obstacle-token-{coordinate.Q}-{coordinate.R}"),
                catalog.LoadToken(SyntheticComponentDefinitions.HazardV4),
                Brynn,
                Brynn,
                new CardInstanceId("brynn-obstacle-token-source"),
                coordinate));
        }

        /// <summary>
        /// A live set carrying milestone 8.7's overlay closing exactly one space, so the fourth source
        /// p.18's classification reads can be measured on its own.
        /// </summary>
        private static RuleModificationSet WithOverlayOver(HexCoordinate space) =>
            new RuleModificationSet(
                Array.Empty<AppliedRuleModification>(),
                new[]
                {
                    AppliedObstacleOverlay.Bind(
                        ConditionalObstacleOverlay.Declare(
                            SourceTeamRelation.AnyTeam,
                            ObstacleOverlayArm.SpacesInsideWhileActorOutside),
                        space,
                        0,
                        "brynn-obstacle-overlay",
                        Brynn,
                        TeamSide.Red,
                        TeamSide.Blue,
                        FarAway)
                },
                TieBreakerState.Unrevealed);

        /// <summary>The live set his unlocked Ultimate supplies while <paramref name="acting"/> acts.</summary>
        private static RuleModificationSet Rules(AdjacentMoveAuthority board, RuleObject acting) =>
            RuleModificationQuery.Build(
                null,
                board,
                ThreeHeroes(),
                TieBreakerState.Unrevealed,
                UltimateUnlockQuery.Derive(
                    new[] { Face(BrynnCardContent.OverTheTop) },
                    new[]
                    {
                        new CardInstance(
                            new CardInstanceId("brynn-over-the-top-instance"),
                            Face(BrynnCardContent.OverTheTop).Reference,
                            Brynn,
                            new CardStateFacts(
                                CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                CardFaceOrientation.FaceUp))
                    }),
                acting,
                null,
                null);

        // -------------------------------------------------------------------------------------------
        // The +2, through the production entry point
        // -------------------------------------------------------------------------------------------

        private static int AmountFor(
            AdjacentMoveAuthority board,
            AttackTarget target,
            RuleModificationSet modifications = null,
            MatchParticipants participants = null)
        {
            var action = RaisedAttackAction();
            var definition = ProbeDefinition(action);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-raise-probe-instance"), definition.Reference, Brynn);
            var live = participants ?? StagedBoardParticipants.Create();

            var contributions = RosterDynamicValueQuery.Contributions(
                action,
                Offer(card, action, AttackRangeKind.NonRanged),
                target,
                PrintedValueChannel.Attack,
                new[] { definition },
                new[] { card },
                live,
                board,
                modifications ?? RuleModificationSet.Empty,
                EffectRosterState.Of(
                    Array.Empty<RoundTurnSlot>(),
                    new RoundHeroDefeatHistory(live),
                    new HeroCoinLedger(live)),
                1,
                1,
                null,
                board);

            return contributions.Sum(contribution => contribution.Amount);
        }

        private static ActionDefinition RaisedAttackAction() =>
            new ActionDefinition(
                new ActionDefinitionId("brynn-raise-probe-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 4) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged,
                default,
                rosterValueContributions: BrynnCardContent.RaisedAttack());

        // -------------------------------------------------------------------------------------------
        // The radius-hero selection, through its handler
        // -------------------------------------------------------------------------------------------

        private static string[] ChosenHeroes(
            AdjacentMoveAuthority board,
            bool narrowed,
            MatchParticipants participants = null,
            RuleModificationSet modifications = null)
        {
            var handler = HeroSelectionHandler(SyntheticTargetingCapabilities.RadiusHeroTargetV1);
            var action = RadiusSkillAction(3);
            var definition = ProbeDefinition(action);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-selection-probe-instance"), definition.Reference, Brynn);

            var inputs = new Dictionary<string, EffectValue>
            {
                ["activity"] = EffectValue.Integer((int)RuleActivity.All),
                [SyntheticTargetingCapabilities.TargetRelationParameterName] =
                    EffectValue.Integer((int)SourceTeamRelation.OpposingSource),
                [SyntheticTargetingCapabilities.ExcludeAdjacentParameterName] =
                    EffectValue.Boolean(false)
            };
            if (narrowed)
            {
                inputs[SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName] =
                    EffectValue.Boolean(true);
            }

            var result = handler.Execute(Context(
                board, definition, card, action, inputs,
                participants ?? StagedBoardParticipants.CreateWithBlueAlly(),
                modifications ?? RuleModificationSet.Empty));

            return result.Kind == EffectExecutionResultKind.NeedsChoice
                ? result.Choice.Candidates.Select(candidate => candidate.HeroId.Value).ToArray()
                : Array.Empty<string>();
        }

        private static IEffectCapabilityHandler HeroSelectionHandler(
            EffectCapabilityReference reference) =>
            SyntheticTargetingCapabilities.Handlers()
                .Single(handler => handler.Reference.Equals(reference));

        // -------------------------------------------------------------------------------------------
        // The two predicates, through their handlers
        // -------------------------------------------------------------------------------------------

        private static bool PresentInRadius(
            AdjacentMoveAuthority board,
            bool withRadiusItem,
            RuleModificationSet modifications = null)
        {
            var handler = BrynnComponentContent.Handlers().Single(candidate =>
                candidate.Reference.Equals(
                    BrynnComponentContent.EnemyHeroInRadiusAdjacentToObstaclesV1));
            var action = RadiusSkillAction(3);
            var definition = ProbeDefinition(action);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-presence-probe-instance"), definition.Reference, Brynn);
            var participants = ThreeHeroes();

            var items = withRadiusItem
                ? HeroItemState.Restore(participants, new HeroItemRestoreState(new[]
                {
                    new HeroItem(
                        Brynn,
                        new CardInstanceId("brynn-radius-item"),
                        HeroDashboard.RulebookLayout.Slots[3].Position,
                        PrintedValueChannel.Radius,
                        1)
                }))
                : new HeroItemState(participants);

            var result = handler.Execute(Context(
                board, definition, card, action,
                new Dictionary<string, EffectValue>(),
                participants,
                modifications ?? RuleModificationSet.Empty,
                items));

            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The predicate can be asked on this board, so it answers rather than refusing.");
            return result.Outcomes
                .Single(fact => fact.OutputName == BrynnComponentContent.PresentOutputName)
                .Value.BooleanValue;
        }

        // -------------------------------------------------------------------------------------------
        // Shared probe content
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A probe Skill printing one Radius icon, whose body is the presence predicate itself - a
        /// Skill definition owes an effect root, and the honest one for a card that asks this question
        /// is the node that asks it.
        /// </summary>
        private static ActionDefinition RadiusSkillAction(int radius)
        {
            var root = new EffectNodeId("brynn-probe-root");
            return new ActionDefinition(
                new ActionDefinitionId("brynn-probe-skill"),
                ActionKind.Skill,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.None,
                default,
                new EffectDefinition(
                    new EffectDefinitionId("brynn-probe-effect"),
                    "rev-1",
                    root,
                    new[]
                    {
                        BrynnEffectContent.EnemyHeroInRadiusAdjacentToObstaclesNode(root)
                    }));
        }

        private static CardDefinition ProbeDefinition(ActionDefinition action) =>
            new CardDefinition(
                new CardDefinitionId("brynn-obstacle-probe"),
                "rev-1",
                "TEST - Brynn obstacle probe",
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { action });

        private static ActionOffer Offer(
            CardInstance card, ActionDefinition action, AttackRangeKind range) =>
            new ActionOffer(
                card.Id,
                card.DefinitionReference,
                action.Id,
                Brynn,
                ActionInvocationContext.PrintedPrimary,
                true,
                range,
                default);

        private static EffectCapabilityContext Context(
            AdjacentMoveAuthority board,
            CardDefinition definition,
            CardInstance card,
            ActionDefinition action,
            IReadOnlyDictionary<string, EffectValue> inputs,
            MatchParticipants participants,
            RuleModificationSet modifications,
            HeroItemState items = null) =>
            new EffectCapabilityContext(
                new EffectNodeId("brynn-obstacle-probe-node"),
                EffectFailurePolicy.Mandatory,
                Brynn,
                Brynn,
                card.Id,
                card.DefinitionReference,
                action.Id,
                inputs,
                1,
                1,
                participants,
                null,
                modifications,
                EffectCardState.Of(new[] { definition }, new[] { card }),
                ActionKind.Skill,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                items ?? new HeroItemState(participants));

        private static EffectDefinition FamiliarGroundText()
        {
            var primary = Face(BrynnCardContent.FamiliarGround).ActionDefinitions
                .Single(action => action.Kind == ActionKind.Attack);
            Assert.That(primary.EffectRoot, Is.Not.Null,
                "Familiar Ground composes its modal at this milestone; a null body means the face " +
                "has been re-parked and every case here is about a card that no longer exists.");
            Assert.That(primary.CardTextTiming, Is.EqualTo(CardTextTiming.BeforeTheAttack),
                "Its bullets ARE the targeting sentence, so they are applied at p.14's step 1.");
            return primary.EffectRoot;
        }

        private static CardDefinition Face(CardDefinitionId id) =>
            BrynnCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private static int PrintedValue(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Single(action => action.Kind == kind)
                .BaseValues
                .Single(value => value.Channel == channel)
                .Amount;
    }
}
