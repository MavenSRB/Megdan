using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
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
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71c. Hanu the Trickster's five Reds and his Gold basic Fight and Flight.
    ///
    /// <para><b>What this fixture measures and why each case is about one printed word.</b> The four
    /// graded Reds print one modal whose second bullet says "Target a hero (minion) in range, adjacent
    /// to your friendly hero[ and not adjacent to you]". Every case below moves exactly one thing about
    /// that sentence - the noun, the teammate, the ring, the immunity, the cardinality - and reads the
    /// candidate set off <see cref="AttackSequenceBulletSubjectQuery"/>, which is the ONE owner the
    /// modal's offer, the modal's greying and the chosen bullet's own p.14 sequence all go through.
    /// Asserting that owner is therefore asserting all three.</para>
    ///
    /// <para><b>THE BOARD IS ONE BOARD, and that is what makes each assertion load-bearing.</b> Four
    /// enemy heroes and two enemy minions stand on it at once, each in a position that satisfies a
    /// DIFFERENT subset of the printed clauses, so no case can pass by a board that happens to have
    /// nobody in it. The teammate is adjacent to Hanu himself, which is the position HA-Q5 turns on:
    /// "your friendly hero" is p.17's Friendly and never Hanu, so a candidate beside Hanu alone
    /// satisfies bullet 1 and not bullet 2.</para>
    ///
    /// <para><b>Nothing here is registered.</b> The block mints no capability at all - see
    /// <see cref="HanuComponentContent"/> - and the two cases at the end assert that from both sides,
    /// which is milestone 7.9's gate.</para>
    /// </summary>
    public sealed class HanuRedsTests
    {
        // -------------------------------------------------------------------------------------------
        // The printed set
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The six faces' values, icons and items, read against the shard's inventory row by row. A
        /// missing icon is absent and never zero (HA-06), which is why Fight and Flight declares no
        /// Range at all and is a non-Ranged Attack rather than a Ranged one of reach 1.
        /// </summary>
        [Test]
        public void TheSixFaces_CarryTheValuesIconsAndItemsTheShardInventories()
        {
            AssertFace(
                HanuCardContent.FightAndFlight, "Fight and Flight", 13, defense: 1, movement: 2,
                attack: 2, range: null, item: PrintedValueChannel.None,
                color: CardColor.Gold, tier: CardTier.None,
                primary: HanuCardContent.PrimaryBasicAttack, ranged: AttackRangeKind.NonRanged,
                timing: CardTextTiming.AfterTheAttack);
            AssertFace(
                HanuCardContent.HelpingHand, "Helping Hand", 9, defense: 4, movement: 5,
                attack: 3, range: 3, item: PrintedValueChannel.None,
                color: CardColor.Red, tier: CardTier.I,
                primary: HanuCardContent.PrimaryRangedAttack, ranged: AttackRangeKind.Ranged,
                timing: CardTextTiming.BeforeTheAttack);
            AssertFace(
                HanuCardContent.EvenTheOdds, "Even the Odds", 9, defense: 4, movement: 5,
                attack: 4, range: 4, item: PrintedValueChannel.Initiative,
                color: CardColor.Red, tier: CardTier.II,
                primary: HanuCardContent.PrimaryRangedAttack, ranged: AttackRangeKind.Ranged,
                timing: CardTextTiming.BeforeTheAttack);
            AssertFace(
                HanuCardContent.Outnumber, "Outnumber", 9, defense: 5, movement: 5,
                attack: 5, range: 4, item: PrintedValueChannel.Defense,
                color: CardColor.Red, tier: CardTier.II,
                primary: HanuCardContent.PrimaryRangedAttack, ranged: AttackRangeKind.Ranged,
                timing: CardTextTiming.BeforeTheAttack);
            AssertFace(
                HanuCardContent.TrustedSidekick, "Trusted Sidekick", 10, defense: 5, movement: 5,
                attack: 4, range: 4, item: PrintedValueChannel.Movement,
                color: CardColor.Red, tier: CardTier.III,
                primary: HanuCardContent.PrimaryRangedAttack, ranged: AttackRangeKind.Ranged,
                timing: CardTextTiming.BeforeTheAttack);
            AssertFace(
                HanuCardContent.PileOn, "Pile On", 10, defense: 6, movement: 5,
                attack: 5, range: 4, item: PrintedValueChannel.Radius,
                color: CardColor.Red, tier: CardTier.III,
                primary: HanuCardContent.PrimaryRangedAttack, ranged: AttackRangeKind.Ranged,
                timing: CardTextTiming.BeforeTheAttack);
        }

        /// <summary>
        /// p.15 forks Red twice, and this block states only the rows whose successors it composes:
        /// Helping Hand climbs to Even the Odds or Outnumber, either of those to Trusted Sidekick or
        /// Pile On, and the two Tier IIIs climb nowhere.
        /// </summary>
        [Test]
        public void TheRedLadder_ForksTwiceAndNamesOnlyThisBlocksFaces()
        {
            Assert.That(Next(HanuCardContent.HelpingHand), Is.EqualTo(new[]
            {
                HanuCardContent.EvenTheOdds.Value, HanuCardContent.Outnumber.Value
            }));
            Assert.That(Next(HanuCardContent.EvenTheOdds), Is.EqualTo(new[]
            {
                HanuCardContent.PileOn.Value, HanuCardContent.TrustedSidekick.Value
            }));
            Assert.That(Next(HanuCardContent.Outnumber), Is.EqualTo(new[]
            {
                HanuCardContent.PileOn.Value, HanuCardContent.TrustedSidekick.Value
            }));
            Assert.That(Next(HanuCardContent.TrustedSidekick), Is.Empty);
            Assert.That(Next(HanuCardContent.PileOn), Is.Empty);
        }

        /// <summary>
        /// The four graded Reds declare the two bullets their sentences print, and the four things
        /// those sentences differ in are the four things the declarations differ in: the modal's
        /// cardinality, the second bullet's noun, its predicate and nothing else.
        /// </summary>
        [Test]
        public void TheFourGradedReds_DeclareTheBulletsTheirPrintedSentencesName()
        {
            AssertModal(
                HanuCardContent.HelpingHand, maximumSelections: 1,
                secondUnits: AttackTargetableUnits.HeroesOnly,
                secondNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);
            AssertModal(
                HanuCardContent.EvenTheOdds, maximumSelections: 1,
                secondUnits: AttackTargetableUnits.HeroesOnly,
                secondNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);
            AssertModal(
                HanuCardContent.Outnumber, maximumSelections: 1,
                secondUnits: AttackTargetableUnits.MinionsOnly,
                secondNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);
            AssertModal(
                HanuCardContent.TrustedSidekick, maximumSelections: 2,
                secondUnits: AttackTargetableUnits.HeroesOnly,
                secondNarrowing: AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor);
            AssertModal(
                HanuCardContent.PileOn, maximumSelections: 2,
                secondUnits: AttackTargetableUnits.MinionsOnly,
                secondNarrowing: AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor);
        }

        /// <summary>
        /// RULING 19's rider, asserted rather than commented: none of the four faces prints
        /// "(on different targets)", so none of their eight bullets declares it - and Pile On, whose
        /// two bullets a single minion may satisfy in turn, is the reason the rider matters.
        /// </summary>
        [Test]
        public void NoBulletOfHis_DeclaresTheDifferentTargetsParenthetical()
        {
            foreach (var face in GradedReds())
            {
                var body = AttackTextOf(face);
                Assert.That(
                    body.Nodes
                        .Where(node => node.Capability.Equals(
                            SyntheticSequenceCapabilities.AttackSequenceV1))
                        .Select(node => node.Inputs
                            .Single(input => input.ParameterName ==
                                SyntheticSequenceCapabilities.DistinctTargetsParameter)
                            .Literal.BooleanValue)
                        .ToArray(),
                    Is.EqualTo(new[] { false, false }),
                    face.Value + ": with no parenthetical printed, both bullets declare the " +
                    "constraint absent - which is what lets Pile On point two bullets at one minion " +
                    "and is the whole of the card's name.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // The two narrowing members and the noun they are read against
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The two members take the next free values and the five before them keep theirs, so no
        /// payload written before this milestone decodes as a different predicate - and the schema
        /// does not move, because the field the bullet writes has been an integer validated against
        /// this enum since milestone 8.35z45.
        /// </summary>
        [Test]
        public void TheNewMembers_TakeTheNextFreeValuesAndMoveNoSchema()
        {
            Assert.That((int)AttackSequenceSubjectNarrowing.None, Is.Zero);
            Assert.That((int)AttackSequenceSubjectNarrowing.HasOneOrMoreCardsInTheDiscard,
                Is.EqualTo(1));
            Assert.That((int)AttackSequenceSubjectNarrowing.HasAnEmptyDiscard, Is.EqualTo(2));
            Assert.That((int)AttackSequenceSubjectNarrowing.HoldsTheActorsMarker, Is.EqualTo(3));
            Assert.That((int)AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles,
                Is.EqualTo(4));
            Assert.That((int)AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor,
                Is.EqualTo(5));
            Assert.That(
                (int)AttackSequenceSubjectNarrowing
                    .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor,
                Is.EqualTo(6));

            Assert.That((int)AttackTargetableUnits.HeroesAndMinions, Is.Zero);
            Assert.That((int)AttackTargetableUnits.HeroesOnly, Is.EqualTo(1));
            Assert.That((int)AttackTargetableUnits.IsolatedUnitsOrUnitsAdjacentToYou, Is.EqualTo(2));
            Assert.That((int)AttackTargetableUnits.MinionsOnly, Is.EqualTo(3),
                "The minion noun is the next free value, so every shipped action definition and every " +
                "persisted bullet still decodes as the policy it was written as.");

            // MERGE NOTE (8.71 wave): v44 is 8.71a's bump (the card-instance Initiative override),
            // landed before this block; 8.71c itself moved nothing.
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(49),
                "Milestone 8.71c moves no schema (v44 is 8.71a's). A persisted bullet already writes both of these as " +
                "plain integers read back through SnapshotEnumReader, so a member added at the end of " +
                "either vocabulary costs no byte and no version - which is the property " +
                "AttackSequenceSubjectNarrowing was minted as a closed enum to have.");
        }

        /// <summary>
        /// A bullet carrying either new member is constructible, equatable and round-trips through the
        /// value equality a restored chain compares by - the shape a snapshot reads back.
        /// </summary>
        [Test]
        public void ABulletCarryingANewMember_IsWellFormedAndCompsEqualToItsCopy()
        {
            foreach (var narrowing in new[]
                {
                    AttackSequenceSubjectNarrowing.AdjacentToAFriendlyHeroOfTheActor,
                    AttackSequenceSubjectNarrowing
                        .AdjacentToAFriendlyHeroOfTheActorAndNotAdjacentToTheActor
                })
            {
                var policy = new PrintedAttackTargetPolicy(
                    AttackTargetableUnits.MinionsOnly, AttackTargetReach.PrintedRange,
                    AttackTargetLine.AnyLine);
                var bullet = new AttackSequenceBullet("probe", policy, narrowing);
                Assert.That(bullet.NarrowsItsSubject, Is.True, narrowing.ToString());
                Assert.That(bullet, Is.EqualTo(new AttackSequenceBullet("probe", policy, narrowing)));
                Assert.That(bullet, Is.Not.EqualTo(
                    new AttackSequenceBullet("probe", policy, AttackSequenceSubjectNarrowing.None)));
            }
        }

        // -------------------------------------------------------------------------------------------
        // The candidate sets, on ONE board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE MEASUREMENT. On one board at one instant, the two bullets of Even the Odds
        /// name different units, and the only thing that differs between them is the printed clause.
        ///
        /// <para>The adjacent bullet names p.19's units in Hanu's own ring; the second names the enemy
        /// heroes in his printed Range who stand beside his teammate. The hero who is BOTH is in both
        /// sets, the hero beside Hanu alone is only in the first, and the hero beside a friendly MINION
        /// is in neither - because "your friendly hero" is the noun the card prints.</para>
        /// </summary>
        [Test]
        public void EvenTheOdds_NamesTheRingWithOneBulletAndTheTeammatesNeighboursWithTheOther()
        {
            var board = RedBoard.Staged();

            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: true),
                Is.EqualTo(new[]
                {
                    EnemyBesideBoth.Value,
                    EnemyAdjacentToHanuOnly.Value,
                    MinionBesideBoth.Value
                }),
                "\"Target a unit adjacent to you\" is p.19's units at p.19's innermost ring - two " +
                "enemy heroes and one enemy minion, and NOT the teammate, who is nobody's enemy.");

            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false),
                Is.EqualTo(new[]
                {
                    EnemyBesideBoth.Value,
                    EnemyBesideFarFriend.Value,
                    EnemyBesideFriend.Value
                }),
                "\"Target a hero in range, adjacent to your friendly hero\" names the three enemy " +
                "heroes standing beside one of his two teammates. The hero beside HANU alone is " +
                "dropped, which is HA-Q5 - p.17's Friendly never includes you - and the hero beside " +
                "a friendly MINION is dropped, because the card prints \"hero\".");
            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false),
                Has.No.Member(EnemyBesideFriendlyMinion.Value),
                "and that last hero stands four spaces off, inside the printed Range of four - so he " +
                "is dropped by the CLAUSE rather than by p.13's reach, which is what makes the " +
                "violator a violator.");
        }

        /// <summary>
        /// Trusted Sidekick prints the same bullet with "and not adjacent to you", and that half of
        /// the sentence is the ONLY difference between its candidate set and Even the Odds' - measured
        /// on the same board, so the exclusion is what removes the hero rather than anything else.
        /// </summary>
        [Test]
        public void TrustedSidekick_RefusesTheTeammatesNeighbourWhoIsAlsoInHanusOwnRing()
        {
            var board = RedBoard.Staged();

            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false),
                Contains.Item(EnemyBesideBoth.Value),
                "Fixture precondition: the hero who stands beside both is in the wider clause's set.");
            Assert.That(board.Subjects(HanuCardContent.TrustedSidekick, adjacentBullet: false),
                Is.EqualTo(new[] { EnemyBesideFarFriend.Value, EnemyBesideFriend.Value }),
                "and the narrower clause drops exactly him, keeping the two teammates' other " +
                "neighbours. That is why the face prints no \"(on " +
                "different targets)\": its two bullets are disjoint by their own words, so \"or " +
                "both\" can never point them at one unit (HA-Q6).");
        }

        /// <summary>
        /// Outnumber and Pile On print "a MINION in range", and the noun does real work: on the same
        /// board the two enemy heroes standing beside the teammate are not candidates at all, while
        /// both enemy minions beside him are.
        /// </summary>
        [Test]
        public void TheMinionFaces_NameOnlyMinions_AndNeverTheHeroesInTheSamePosition()
        {
            var board = RedBoard.Staged();

            foreach (var face in new[] { HanuCardContent.Outnumber, HanuCardContent.PileOn })
            {
                Assert.That(board.Subjects(face, adjacentBullet: false),
                    Is.EqualTo(new[] { MinionBesideBoth.Value, MinionBesideFriend.Value }),
                    face.Value + ": \"Target a minion in range, adjacent to your friendly hero\" " +
                    "names the two enemy minions beside the teammate and neither of the two enemy " +
                    "heroes in exactly that position.");
                Assert.That(board.Subjects(face, adjacentBullet: true),
                    Contains.Item(EnemyAdjacentToHanuOnly.Value),
                    face.Value + ": and the face's OTHER bullet still prints \"a unit\", so the " +
                    "noun narrows one sentence rather than the card.");
            }
        }

        /// <summary>
        /// Helping Hand prints Range 3 and Even the Odds Range 4, and the hero standing four spaces
        /// off is reached by exactly one of them - so the bullet's reach is the card's own printed
        /// icon rather than a number written into the bullet.
        /// </summary>
        [Test]
        public void TheSecondBulletsReach_IsTheCardsOwnPrintedRange()
        {
            var board = RedBoard.Staged();

            Assert.That(board.Subjects(HanuCardContent.HelpingHand, adjacentBullet: false),
                Has.No.Member(EnemyBesideFarFriend.Value),
                "Helping Hand prints Range 3 and the hero beside the far teammate stands four " +
                "spaces off, so the clause does not reach him.");
            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false),
                Contains.Item(EnemyBesideFarFriend.Value),
                "Even the Odds prints Range 4 and does - the bullet's reach is the card's own " +
                "printed icon, never a number written into the bullet.");
            Assert.That(
                board.Subjects(HanuCardContent.HelpingHand, adjacentBullet: false, withRangeItem: true),
                Contains.Item(EnemyBesideFarFriend.Value),
                "and p.16's Range item raises Helping Hand's printed 3 to 4, which reaches him too - " +
                "the reason BA-15 gives for the reach being PrintedRange rather than a literal.");
        }

        /// <summary>p.18: an immune unit cannot be targeted, so it is not a candidate of either bullet.</summary>
        [Test]
        public void AnImmuneNeighbourOfTheTeammate_IsNotACandidateAtAll()
        {
            var board = RedBoard.Staged(immuneHero: EnemyBesideFriend);

            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false),
                Is.EqualTo(new[] { EnemyBesideBoth.Value, EnemyBesideFarFriend.Value }),
                "The immune hero is gone from the set and the other neighbours of a teammate are " +
                "not - p.18 is asked by the target query this narrowing narrows, so the clause and " +
                "the immunity compose rather than one of them replacing the other.");
        }

        /// <summary>
        /// The clause names a HERO on the actor's team, so a board where the teammate has walked away
        /// leaves the second bullet naming nobody however many friendly minions stand beside the
        /// candidates - the violator case for HA-Q5's noun.
        /// </summary>
        [Test]
        public void WithTheTeammateElsewhere_TheSecondBulletNamesNobodyOnEitherNoun()
        {
            var board = RedBoard.Staged(standTheFriendBeside: false);

            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: false), Is.Empty);
            Assert.That(board.Subjects(HanuCardContent.PileOn, adjacentBullet: false), Is.Empty);
            Assert.That(board.Subjects(HanuCardContent.EvenTheOdds, adjacentBullet: true),
                Is.Not.Empty,
                "and the ring bullet is untouched, so the emptiness is about the teammate rather " +
                "than about the board being empty.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.70's greying, at the modal
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A bullet whose own sentence names nobody is greyed at the modal choice rather than offered,
        /// taken and spent on nothing - milestone 8.70, reaching Hanu's new members because the
        /// greying asks the one owner rather than holding a derivation of its own.
        /// </summary>
        [Test]
        public void WithTheTeammateElsewhere_TheSecondBulletIsGreyedAtTheChoice()
        {
            var board = RedBoard.Staged(standTheFriendBeside: false);
            var choice = board.Modal(HanuCardContent.EvenTheOdds);

            Assert.That(
                choice.UnavailableCandidates.Select(value => value.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[] { HanuEffectContent.RedFriendlyBulletNode }),
                "The clause names nobody, so the player cannot choose it.");
            Assert.That(
                choice.SelectInvocations
                    .Select(value => value.Candidate.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[] { HanuEffectContent.RedAdjacentBulletNode }),
                "and the bullet that does name somebody is still the answer they give.");
        }

        /// <summary>Both bullets live on the staged board, so nothing is greyed that the board supports.</summary>
        [Test]
        public void OnTheStagedBoard_NeitherBulletIsGreyed()
        {
            foreach (var face in GradedReds())
            {
                var choice = RedBoard.Staged().Modal(face);
                Assert.That(choice.UnavailableCandidates, Is.Empty, face.Value);
                Assert.That(choice.SelectInvocations, Has.Count.EqualTo(2), face.Value);
            }
        }

        // -------------------------------------------------------------------------------------------
        // "Choose one, or both, in any order"
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HA-Q6, MEASURED RATHER THAN ASSUMED. "In any order" needed no delta: milestone 8.35z29
        /// already pushes a <c>Choose</c>'s selected options in reverse of the order the Select
        /// commands named them, so the plan is the ACTOR's order. The same two bullets answered in the
        /// two orders produce the two plans - which is what proves it.
        ///
        /// <para>What 8.71c had to do about the printed phrase was correct
        /// <see cref="RequestedAttackSequenceQuery"/>'s own remark, which still said the opposite four
        /// milestones after it stopped being true.</para>
        /// </summary>
        [Test]
        public void ChooseBothInAnyOrder_RunsTheSequencesInTheOrderTheActorNamedThem()
        {
            var adjacentFirst = RedBoard.Staged().PlanFromBothBullets(
                HanuCardContent.PileOn, friendlyBulletFirst: false);
            var friendlyFirst = RedBoard.Staged().PlanFromBothBullets(
                HanuCardContent.PileOn, friendlyBulletFirst: true);

            Assert.That(
                adjacentFirst.Bullets.Select(bullet => bullet.BulletId).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.RedAdjacentBulletNode,
                    HanuEffectContent.RedFriendlyBulletNode
                }));
            Assert.That(
                friendlyFirst.Bullets.Select(bullet => bullet.BulletId).ToArray(),
                Is.EqualTo(new[]
                {
                    HanuEffectContent.RedFriendlyBulletNode,
                    HanuEffectContent.RedAdjacentBulletNode
                }),
                "Two answers to one modal, differing only in which bullet was pressed first, give " +
                "two plans - so p.10's \"you can choose the bullet points in any order\" is a rule " +
                "this build gives, and the printed \"in any order\" needed nothing added for it.");
        }

        /// <summary>
        /// A 1..2 face may still take ONE bullet, which is the printed "Choose one, or both" read
        /// literally, and a 1..1 face may not take two.
        /// </summary>
        [Test]
        public void TheCardinalities_AreTheOneWordThatSeparatesTheTwoGrades()
        {
            var oneBullet = RedBoard.Staged().PlanFromOneBullet(
                HanuCardContent.Outnumber, friendlyBullet: true);
            Assert.That(oneBullet.Bullets, Has.Count.EqualTo(1));
            Assert.That(oneBullet.Bullets[0].BulletId,
                Is.EqualTo(HanuEffectContent.RedFriendlyBulletNode));

            var stillOne = RedBoard.Staged().PlanFromOneBullet(
                HanuCardContent.PileOn, friendlyBullet: false);
            Assert.That(stillOne.Bullets, Has.Count.EqualTo(1),
                "\"Choose one, OR both\" is a floor of one and a ceiling of two, so one bullet is a " +
                "complete answer to Pile On as well.");

            Assert.That(RedBoard.Staged().Modal(HanuCardContent.Outnumber).ResolvesOnSelection,
                Is.True,
                "A \"Choose one\" admits exactly one selection, so naming a bullet is the whole " +
                "answer - milestone 8.46b's one press.");
            Assert.That(RedBoard.Staged().Modal(HanuCardContent.PileOn).ResolvesOnSelection,
                Is.False,
                "and \"or both\" holds the choice open for a second press.");
        }

        /// <summary>
        /// RULING 19 on Pile On, stated as the chain states it: with no "(on different targets)"
        /// printed, the unit the first sequence bound is not excluded from the second - so a minion
        /// standing beside Hanu AND beside his teammate may take both bullets.
        ///
        /// <para>Trusted Sidekick reaches the same answer from the other direction and needs no
        /// exclusion: its bullets are disjoint by their own words, so the chain's empty exclusion list
        /// can never let one unit take both.</para>
        /// </summary>
        [Test]
        public void PileOn_LetsOneMinionTakeBothBullets_AndTrustedSidekickCannotByItsOwnWords()
        {
            var board = RedBoard.Staged();

            Assert.That(board.Subjects(HanuCardContent.PileOn, adjacentBullet: true),
                Contains.Item(MinionBesideBoth.Value));
            Assert.That(board.Subjects(HanuCardContent.PileOn, adjacentBullet: false),
                Contains.Item(MinionBesideBoth.Value),
                "One minion satisfies each of Pile On's two sentences, and the card prints nothing " +
                "that keeps them apart - which is RULING 19's rider and the card's own name.");

            var chain = board.ChainAfterBinding(
                HanuCardContent.PileOn, AttackTarget.Minion(MinionBesideBoth));
            Assert.That(chain.ExcludedTargets, Is.Empty,
                "so the second sequence excludes nothing the first one took.");

            var adjacent = board.Subjects(HanuCardContent.TrustedSidekick, adjacentBullet: true);
            var friendly = board.Subjects(HanuCardContent.TrustedSidekick, adjacentBullet: false);
            Assert.That(adjacent.Intersect(friendly, StringComparer.Ordinal), Is.Empty,
                "Trusted Sidekick's two sets are disjoint on a board built to overlap them, because " +
                "its second bullet prints \"and not adjacent to you\" - which is why it needs no " +
                "parenthetical of its own.");
        }

        /// <summary>
        /// Each bullet binds afresh against the board its own p.14 reaches, so a unit the first
        /// sequence removed is not a candidate for the second - HA-Q6's "targets afresh", measured by
        /// taking the minion off the board between the two derivations rather than by reasoning.
        /// </summary>
        [Test]
        public void TheSecondBullet_RetargetsAgainstTheBoardItsOwnSequenceFinds()
        {
            var board = RedBoard.Staged();
            Assert.That(board.Subjects(HanuCardContent.PileOn, adjacentBullet: false),
                Contains.Item(MinionBesideBoth.Value),
                "Fixture precondition: the minion is a candidate of the second bullet.");

            board.Remove(BoardObjectReference.ForMinion(MinionBesideBoth));

            Assert.That(board.Subjects(HanuCardContent.PileOn, adjacentBullet: false),
                Is.EqualTo(new[] { MinionBesideFriend.Value }),
                "Once the first sequence's p.14 removed him he is not a candidate for the second, " +
                "because the second derives its set when it starts rather than at the modal.");
        }

        /// <summary>
        /// The whole answer replayed: the same commands against the same board produce the same plan
        /// and the same recorded choices, which is exactly what a snapshot restore, a replay and a
        /// retained-prefix Undo each re-run through the modal's pause.
        /// </summary>
        [Test]
        public void TheModalsAnswer_ReplaysToTheSamePlanAndTheSameRecord()
        {
            var first = RedBoard.Staged().AnswerBothBullets(
                HanuCardContent.TrustedSidekick, friendlyBulletFirst: true);
            var second = RedBoard.Staged().AnswerBothBullets(
                HanuCardContent.TrustedSidekick, friendlyBulletFirst: true);

            Assert.That(
                second.RequestedAttackSequences.Bullets.Select(b => b.BulletId).ToArray(),
                Is.EqualTo(first.RequestedAttackSequences.Bullets.Select(b => b.BulletId).ToArray()));
            Assert.That(
                second.Events.Select(value => value.NodeId.Value + ":" + value.ResultKind).ToArray(),
                Is.EqualTo(
                    first.Events.Select(value => value.NodeId.Value + ":" + value.ResultKind)
                        .ToArray()),
                "A modal answered twice the same way produces the same ordered outcomes, so nothing " +
                "about the plan depends on anything but the commands and the board.");
        }

        // -------------------------------------------------------------------------------------------
        // Fight and Flight
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The rider's shape, read off the composed body: the survival question, the shared flag gate
        /// over it, and a file whose two bounds are both three because "move 3 spaces" is a COUNT.
        /// Neither the offer nor the Move declares the obstacle clause, because the card prints none.
        /// </summary>
        [Test]
        public void FightAndFlight_ComposesAThreeSpaceFileBehindTheSurvivalGate()
        {
            var body = AttackTextOf(HanuCardContent.FightAndFlight);
            var survived = Node(body, HanuEffectContent.FlightSurvivedNode);
            var gate = Node(body, HanuEffectContent.FlightGateNode);
            var line = Node(body, HanuEffectContent.FlightLineNode);
            var move = Node(body, HanuEffectContent.FlightMoveNode);

            Assert.That(survived.Capability,
                Is.EqualTo(SyntheticAttackTargetSurvivalCapabilities.AttackTargetSurvivedV1));
            Assert.That(gate.Kind, Is.EqualTo(EffectNodeKind.Conditional));
            Assert.That(gate.Capability, Is.EqualTo(SyntheticEffectCapabilities.IsFlaggedV1));
            Assert.That(gate.ChildNodeIds.Select(id => id.Value).ToArray(),
                Is.EqualTo(new[] { HanuEffectContent.FlightArmNode }),
                "and nothing runs when the gate reads false, because the Conditional declares no " +
                "second branch.");

            Assert.That(Literal(line, "maximumDistance"), Is.EqualTo(3));
            Assert.That(Literal(line, "minimumDistance"), Is.EqualTo(3),
                "Equal bounds: \"move 3 spaces in a straight line\" is a count and not a ceiling, " +
                "and milestone 8.59's correction on Bounce is why the floor is said to the OFFER.");
            Assert.That(
                line.Inputs.Any(input =>
                    input.ParameterName == SyntheticRosterCapabilities.IgnoresObstaclesParameter),
                Is.False,
                "Bounce prints \"ignoring obstacles\" and this card prints nothing, so nothing is " +
                "declared - p.11 is the default and an absent clause is absent.");
            Assert.That(
                move.Inputs.Any(input =>
                    input.ParameterName == SyntheticDisplacementCapabilities
                        .MovesThroughObstaclesParameterName),
                Is.False);
            Assert.That(Literal(move, "distance"), Is.EqualTo(3));
            Assert.That(
                Literal(move, SyntheticDisplacementCapabilities.LeastDistanceParameterName),
                Is.EqualTo(3));

            Assert.That(line.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble));
            Assert.That(move.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble),
                "\"If able\" makes an empty offer a step not taken rather than an impossible one " +
                "that stops the action (HA-Q7), and the consuming node carries the policy too - the " +
                "showcase's own line-travel shape.");
        }

        /// <summary>
        /// On a board with one file blocked by the target, one by a teammate and one whose end is
        /// occupied, the offer is exactly the three files that are legal - p.11 whole, measured rather
        /// than reasoned.
        /// </summary>
        [Test]
        public void FightAndFlight_OffersExactlyTheLegalThreeSpaceFiles()
        {
            var rider = RiderBoard.Staged(targetRemoved: false);

            Assert.That(rider.Destinations(), Is.EqualTo(new[]
            {
                new HexCoordinate(-3, 0),
                new HexCoordinate(0, 3),
                new HexCoordinate(3, -3)
            }),
                "Three of the six files. The file the target stands on is blocked by him, the file " +
                "the teammate stands on is blocked by her, and the eastern file's third space is " +
                "occupied - so no unit transits an obstacle and none ends on one. The other three " +
                "are open and are offered at exactly three spaces, never at one or two.");
        }

        /// <summary>
        /// "If the target is not defeated" - the rider does not fire at all when p.14's step 6 removed
        /// the target, and the same board with the same file open is what proves the gate is what
        /// stopped it.
        /// </summary>
        [Test]
        public void FightAndFlight_DoesNotMoveHimAtAllWhenTheTargetWasRemoved()
        {
            Assert.That(RiderBoard.Staged(targetRemoved: false).Destinations(), Is.Not.Empty,
                "Fixture precondition: the file is open on this board.");

            var defeated = RiderBoard.Staged(targetRemoved: true);
            Assert.That(defeated.Step.Frame?.PendingChoice, Is.Null,
                "A defeated target closes the gate, so nothing asks him where to run.");
            Assert.That(defeated.Outcome(HanuEffectContent.FlightLineNode),
                Is.EqualTo(EffectExecutionResultKind.None),
                "and the file node is never reached at all - the branch is not entered rather than " +
                "entered and refused.");
            Assert.That(defeated.Step.IsComplete, Is.True);
        }

        /// <summary>
        /// "If able": a board on which no file of exactly three spaces is legal offers nothing, and
        /// the action finishes rather than stopping on an impossible step.
        /// </summary>
        [Test]
        public void FightAndFlight_IsNothingWhereNoThreeSpaceFileIsLegal()
        {
            var boxed = RiderBoard.Boxed();

            Assert.That(boxed.Step.Frame?.PendingChoice, Is.Null);
            Assert.That(boxed.Outcome(HanuEffectContent.FlightLineNode),
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "The file node was reached, found nothing, and `if able` recorded it as a step not " +
                "taken rather than as the impossible one that stops a mandatory sequence.");
            Assert.That(boxed.Step.IsComplete, Is.True,
                "and the action completes, which is what separates \"If able\" from p.11's stop.");
        }

        /// <summary>
        /// Answering the offer moves him exactly three spaces along the file he named, and the same
        /// answer replayed against the same board moves him to the same space - which is what a
        /// restore, a replay and an Undo each re-run.
        /// </summary>
        [Test]
        public void FightAndFlight_MovesHimTheWholeThreeSpaces_AndReplaysToTheSameSpace()
        {
            var first = RiderBoard.Staged(targetRemoved: false);
            var destination = first.Destinations()[0];
            first.Answer(destination);

            Assert.That(first.HeroSpace(Hanu), Is.EqualTo(destination),
                "The move is the whole file - p.11 has no shortened walk to a named space.");

            var second = RiderBoard.Staged(targetRemoved: false);
            second.Answer(second.Destinations()[0]);
            Assert.That(second.HeroSpace(Hanu), Is.EqualTo(first.HeroSpace(Hanu)));
        }

        // -------------------------------------------------------------------------------------------
        // The gate, the labels and the claim
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate from both sides: this block mints no capability, so its deferral list
        /// is empty AND nothing of Hanu's is in the build's registered catalog. The second half is the
        /// one that matters - an empty deferral list would otherwise be indistinguishable from a block
        /// that registered everything it made.
        /// </summary>
        [Test]
        public void TheBlockMintsNothing_AndNothingOfHisIsRegistered()
        {
            Assert.That(HanuComponentContent.RedDeferredHandlers(), Is.Empty,
                "Every clause of these six faces composes on a contract a dealt hero already starts.");
            Assert.That(HanuComponentContent.RedRegisteredHandlers(), Is.Empty);

            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered
                .Select(reference => reference.CapabilityId.Value)
                .ToArray();
            Assert.That(
                registered.Where(value => value.StartsWith("hanu", StringComparison.Ordinal))
                    .OrderBy(value => value, StringComparer.Ordinal),
                Is.EqualTo(new[]
                {
                    "hanu-friendly-unit-pair",
                    "hanu-hurry-up-initiative",
                    "hanu-journey-partner",
                    "hanu-safe-travels-effect",
                    "hanu-there-and-back-again-effect",
                    "hanu-unexpected-journey-effect"
                }),
                "and the SIX contracts the build has named for him since milestone 8.71h dealt him " +
                "are every other block's: not one of them belongs to these six faces, which is this " +
                "block's headline result read from the build's own catalog rather than from its own " +
                "empty lists.");
        }

        /// <summary>
        /// The decision labels are DERIVED from the bodies rather than guessed - the check milestone
        /// 8.69f's ten missing labels bought. Every control these six faces raise is covered, nothing
        /// is declared for a control they do not raise, and every word fits the window.
        /// </summary>
        [Test]
        public void TheDecisionLabels_CoverExactlyTheControlsTheseFacesRaise()
        {
            var required = ThisBlocksFaces()
                .SelectMany(card => DecisionLabelRequirementQuery.For(
                    card, MatchContentCatalogs.ForThisBuild()))
                .Select(requirement => requirement.CardId.Value + " / " + requirement.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var declared = HanuEffectContent.RedDecisionLabels()
                .Select(label => label.CardId.Value + " / " + label.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.EqualTo(required),
                "Eight controls, two per graded Red. Fight and Flight raises none: its straight-line " +
                "prompt is a declinable SELECTION whose controls name the spaces they would move to, " +
                "and the nodes around it are `if able`, which never asks.");
            Assert.That(
                HanuEffectContent.RedDecisionLabels().All(label => label.Label.Chosen.Length <= 60),
                Is.True,
                "and every word fits the sixty-character bound the decision window draws in, which " +
                "is the cut milestone 8.69g measured on Brynn's traps.");
        }

        // -------------------------------------------------------------------------------------------
        // Identities the fixture names
        // -------------------------------------------------------------------------------------------

        private static HeroId Hanu { get; } = new HeroId("hanu-reds-actor");
        private static HeroId Friend { get; } = new HeroId("hanu-reds-friend");

        /// <summary>
        /// A second teammate, three spaces off, so the clause's reach can be measured: the enemy
        /// standing beside HIM is four spaces from Hanu, which Even the Odds' printed Range reaches and
        /// Helping Hand's does not.
        /// </summary>
        private static HeroId FarFriend { get; } = new HeroId("hanu-reds-far-friend");

        /// <summary>Beside the second teammate, four spaces off: in one card's Range and not the other's.</summary>
        private static HeroId EnemyBesideFarFriend { get; } = new HeroId("hanu-reds-enemy-distant");

        /// <summary>Beside Hanu and beside the teammate: bullet 1, the wide clause, not the narrow one.</summary>
        private static HeroId EnemyBesideBoth { get; } = new HeroId("hanu-reds-enemy-both");

        /// <summary>Beside the teammate only: the narrow clause's one candidate.</summary>
        private static HeroId EnemyBesideFriend { get; } = new HeroId("hanu-reds-enemy-friend");

        /// <summary>Beside Hanu only: bullet 1 and neither form of bullet 2 (HA-Q5).</summary>
        private static HeroId EnemyAdjacentToHanuOnly { get; } = new HeroId("hanu-reds-enemy-ring");

        /// <summary>Beside a friendly MINION only: "your friendly hero" is a hero clause.</summary>
        private static HeroId EnemyBesideFriendlyMinion { get; } = new HeroId("hanu-reds-enemy-far");

        private static MinionId MinionBesideBoth { get; } = new MinionId("hanu-reds-minion-both");
        private static MinionId MinionBesideFriend { get; } = new MinionId("hanu-reds-minion-friend");
        private static MinionId FriendlyMinion { get; } = new MinionId("hanu-reds-minion-ally");

        private static readonly TeamId BlueTeam = new TeamId("hanu-reds-blue");
        private static readonly TeamId RedTeam = new TeamId("hanu-reds-red");
        private static readonly MapId ProbeMap = new MapId("hanu-reds-map");

        // -------------------------------------------------------------------------------------------
        // Helpers over the composed content
        // -------------------------------------------------------------------------------------------

        private static IEnumerable<CardDefinitionId> GradedReds() => new[]
        {
            HanuCardContent.HelpingHand,
            HanuCardContent.EvenTheOdds,
            HanuCardContent.Outnumber,
            HanuCardContent.TrustedSidekick,
            HanuCardContent.PileOn
        };

        // MERGE NOTE (8.71 wave): this block's own list, not the integrated aggregate.
        private static IEnumerable<CardDefinition> ThisBlocksFaces() => HanuCardContent.RedDefinitions();

        private static CardDefinition Face(CardDefinitionId id) =>
            HanuCardContent.AllDefinitions.Single(card => card.Id.Equals(id));

        private static ActionDefinition AttackActionOf(CardDefinitionId id) =>
            Face(id).ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

        private static EffectDefinition AttackTextOf(CardDefinitionId id)
        {
            var action = AttackActionOf(id);
            var body = action.CardTextAt(action.CardTextTiming);
            Assert.That(body, Is.Not.Null, id.Value + " composes its printed text.");
            return body;
        }

        private static EffectNodeDefinition Node(EffectDefinition body, string nodeId) =>
            body.Nodes.Single(node => node.Id.Value == nodeId);

        private static int Literal(EffectNodeDefinition node, string parameterName) =>
            node.Inputs
                .Single(input => input.ParameterName == parameterName)
                .Literal.IntegerValue;

        private static string[] Next(CardDefinitionId id) =>
            Face(id).Progression.NextTierCandidateIds
                .Select(candidate => candidate.Value)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        private static void AssertModal(
            CardDefinitionId id,
            int maximumSelections,
            AttackTargetableUnits secondUnits,
            AttackSequenceSubjectNarrowing secondNarrowing)
        {
            var body = AttackTextOf(id);
            var modal = Node(body, HanuEffectContent.RedModalNode);

            Assert.That(modal.Kind, Is.EqualTo(EffectNodeKind.Choose), id.Value);
            Assert.That(modal.Chooser, Is.EqualTo(EffectChooserRole.Actor), id.Value);
            Assert.That(modal.MinimumSelections, Is.EqualTo(1), id.Value);
            Assert.That(modal.MaximumSelections, Is.EqualTo(maximumSelections), id.Value);

            var declared = RequestedAttackSequenceQuery.Declared(body);
            Assert.That(declared, Has.Count.EqualTo(2), id.Value);

            var adjacent = declared.Single(
                bullet => bullet.BulletId == HanuEffectContent.RedAdjacentBulletNode);
            Assert.That(adjacent.TargetPolicy.TargetableUnits,
                Is.EqualTo(AttackTargetableUnits.HeroesAndMinions), id.Value);
            Assert.That(adjacent.TargetPolicy.Reach,
                Is.EqualTo(AttackTargetReach.AdjacentOnly), id.Value);
            Assert.That(adjacent.SubjectNarrowing,
                Is.EqualTo(AttackSequenceSubjectNarrowing.None), id.Value);

            var friendly = declared.Single(
                bullet => bullet.BulletId == HanuEffectContent.RedFriendlyBulletNode);
            Assert.That(friendly.TargetPolicy.TargetableUnits, Is.EqualTo(secondUnits), id.Value);
            Assert.That(friendly.TargetPolicy.Reach,
                Is.EqualTo(AttackTargetReach.PrintedRange), id.Value);
            Assert.That(friendly.SubjectNarrowing, Is.EqualTo(secondNarrowing), id.Value);
            Assert.That(friendly.TargetPolicy.Line, Is.EqualTo(AttackTargetLine.AnyLine), id.Value);
        }

        private static void AssertFace(
            CardDefinitionId id,
            string displayName,
            int initiative,
            int defense,
            int movement,
            int attack,
            int? range,
            PrintedValueChannel item,
            CardColor color,
            CardTier tier,
            ActionDefinitionId primary,
            AttackRangeKind ranged,
            CardTextTiming timing)
        {
            var card = Face(id);
            Assert.That(card.DisplayName, Is.EqualTo(displayName));
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative), id.Value);
            Assert.That(card.Progression.Color, Is.EqualTo(color), id.Value);
            Assert.That(card.Progression.Tier, Is.EqualTo(tier), id.Value);
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item), id.Value);

            var action = AttackActionOf(id);
            Assert.That(action.Id, Is.EqualTo(primary), id.Value);
            Assert.That(action.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedPrimary), id.Value);
            Assert.That(action.AttackRange, Is.EqualTo(ranged), id.Value);
            Assert.That(action.CardTextTiming, Is.EqualTo(timing), id.Value);
            Assert.That(Printed(action, PrintedValueChannel.Attack), Is.EqualTo(attack), id.Value);
            Assert.That(Printed(action, PrintedValueChannel.Range), Is.EqualTo(range),
                id.Value + ": HA-06 - a missing Range icon is absent, never zero.");

            var movementAction = card.ActionDefinitions
                .Single(value => value.Kind == ActionKind.Movement);
            Assert.That(Printed(movementAction, PrintedValueChannel.Movement),
                Is.EqualTo(movement), id.Value);
            var defenseAction = card.ActionDefinitions
                .Single(value => value.Kind == ActionKind.Defense);
            Assert.That(Printed(defenseAction, PrintedValueChannel.Defense),
                Is.EqualTo(defense), id.Value);
        }

        private static int? Printed(ActionDefinition action, PrintedValueChannel channel)
        {
            var declared = action.BaseValues
                .Where(value => value.Channel == channel)
                .ToArray();
            return declared.Length == 0 ? (int?)null : declared[0].Amount;
        }

        // -------------------------------------------------------------------------------------------
        // The staged board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// ONE board carrying every position the four printed clauses can distinguish, so each case
        /// moves one thing and nothing else.
        ///
        /// <para>Hanu stands at the origin with his TEAMMATE beside him - the position HA-Q5 turns on -
        /// and around them: an enemy hero beside both of them, an enemy hero beside the teammate only,
        /// an enemy hero beside Hanu only, an enemy hero four spaces off beside a friendly MINION, and
        /// two enemy minions in the same two positions as the first two heroes.</para>
        /// </summary>
        private sealed class RedBoard
        {
            private static readonly CardInstanceId FaceInstance =
                new CardInstanceId("hanu-reds-face");

            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MarkerAttachmentRegister markers = new MarkerAttachmentRegister();
            private readonly MatchComponents components;
            private readonly RuleModificationSet modifications;
            private readonly IReadOnlyList<CardDefinition> definitions;
            private readonly IReadOnlyList<CardInstance> instances;
            private readonly HeroItemState heroItems;
            private readonly HeroItemState heroItemsWithRange;

            private RedBoard(bool standTheFriendBeside, HeroId immuneHero)
            {
                participants = Participants();
                modifications = immuneHero.IsDefined
                    ? Immune(immuneHero)
                    : RuleModificationSet.Empty;

                var friendAt = standTheFriendBeside
                    ? new HexCoordinate(1, 0)
                    : new HexCoordinate(-4, 0);
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(ProbeMap, 1, Disk(6)),
                    new[]
                    {
                        new HeroOccupancy(Hanu, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Friend, friendAt),
                        new HeroOccupancy(FarFriend, standTheFriendBeside
                            ? new HexCoordinate(0, 3)
                            : new HexCoordinate(-5, 0)),
                        new HeroOccupancy(EnemyBesideBoth, new HexCoordinate(0, 1)),
                        new HeroOccupancy(EnemyBesideFriend, new HexCoordinate(2, 0)),
                        new HeroOccupancy(EnemyAdjacentToHanuOnly, new HexCoordinate(-1, 0)),
                        new HeroOccupancy(EnemyBesideFriendlyMinion, new HexCoordinate(4, 0)),
                        new HeroOccupancy(EnemyBesideFarFriend, new HexCoordinate(0, 4))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    new[]
                    {
                        Minion(MinionBesideBoth, RedTeam, TeamSide.Red, new HexCoordinate(1, -1)),
                        Minion(MinionBesideFriend, RedTeam, TeamSide.Red, new HexCoordinate(2, -1)),
                        Minion(FriendlyMinion, BlueTeam, TeamSide.Blue, new HexCoordinate(4, -1))
                    });

                components = new MatchComponents(
                    MatchContentCatalogs.ForThisBuild().Components,
                    board, markers, board, board, board, board);
                definitions = HanuCardContent.AllDefinitions;
                instances = new[]
                {
                    CardInstance.CreateInHand(FaceInstance, Face(HanuCardContent.EvenTheOdds).Reference, Hanu)
                };
                heroItems = new HeroItemState(participants);
                heroItemsWithRange = new HeroItemState(participants);
                Assert.That(
                    heroItemsWithRange
                        .TryGrantItemFromTheBox(Hanu, PrintedValueChannel.Range).IsPlaced,
                    Is.True,
                    "Fixture precondition: p.16's Range item can be granted to the actor.");
            }

            public static RedBoard Staged(
                bool standTheFriendBeside = true, HeroId immuneHero = default) =>
                new RedBoard(standTheFriendBeside, immuneHero);

            /// <summary>Every unit one bullet of one face admits right now, as sorted identity strings.</summary>
            public string[] Subjects(
                CardDefinitionId face, bool adjacentBullet, bool withRangeItem = false)
            {
                var action = AttackActionOf(face);
                var bullet = RequestedAttackSequenceQuery.Declared(AttackTextOf(face))
                    .Single(value => value.BulletId == (adjacentBullet
                        ? HanuEffectContent.RedAdjacentBulletNode
                        : HanuEffectContent.RedFriendlyBulletNode));
                return AttackSequenceBulletSubjectQuery.FindLegalSubjects(
                        Hanu,
                        Offer(face, action),
                        bullet,
                        definitions,
                        instances,
                        participants,
                        board,
                        board,
                        markers,
                        withRangeItem ? heroItemsWithRange : heroItems,
                        EffectRosterState.Absent,
                        modifications)
                    .Select(target => target.Kind == AttackTargetKind.Hero
                        ? target.HeroId.Value
                        : target.MinionId.Value)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray();
            }

            public void Remove(BoardObjectReference subject) =>
                Assert.That(board.CommitValidatedRemoval(subject).Accepted, Is.True);

            public PendingAttackSequenceChain ChainAfterBinding(
                CardDefinitionId face, AttackTarget bound)
            {
                var plan = new AttackSequencePlan(
                    RequestedAttackSequenceQuery.Declared(AttackTextOf(face)),
                    requireDistinctTargets: false);
                return new PendingAttackSequenceChain(
                    new PendingAttackResolution(
                        new ActionResolutionId("hanu-reds-chain"),
                        FaceInstance,
                        Face(face).Reference,
                        AttackActionOf(face).Id,
                        Hanu,
                        ActionInvocationContext.PrintedPrimary,
                        new ActionResolutionStepId("hanu-reds-chain-step"),
                        AttackResolutionPhase.ChooseTarget,
                        default(AttackTarget),
                        null),
                    plan,
                    1,
                    new[] { bound });
            }

            /// <summary>The pause the modal opens, which is what the decision window draws.</summary>
            public PendingEffectChoice Modal(CardDefinitionId face)
            {
                var step = Start(face);
                Assert.That(step.Frame.PendingChoice, Is.Not.Null,
                    face.Value + " pauses on its modal.");
                return step.Frame.PendingChoice;
            }

            public AttackSequencePlan PlanFromOneBullet(CardDefinitionId face, bool friendlyBullet)
            {
                var step = Select(Start(face), face, friendlyBullet);
                if (step.Frame?.PendingChoice?.ConfirmInvocation != null)
                {
                    step = Confirm(step, face);
                }

                Assert.That(step.RequestedAttackSequences, Is.Not.Null, face.Value);
                return step.RequestedAttackSequences;
            }

            public AttackSequencePlan PlanFromBothBullets(
                CardDefinitionId face, bool friendlyBulletFirst) =>
                AnswerBothBullets(face, friendlyBulletFirst).RequestedAttackSequences;

            public EffectResolutionStepResult AnswerBothBullets(
                CardDefinitionId face, bool friendlyBulletFirst)
            {
                var step = Select(Start(face), face, friendlyBulletFirst);
                step = Select(step, face, !friendlyBulletFirst);
                step = Confirm(step, face);
                Assert.That(step.RequestedAttackSequences, Is.Not.Null, face.Value);
                Assert.That(step.RequestedAttackSequences.Bullets, Has.Count.EqualTo(2), face.Value);
                return step;
            }

            private EffectResolutionStepResult Start(CardDefinitionId face)
            {
                var step = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-reds-" + face.Value),
                    FaceInstance,
                    Face(face).Reference,
                    AttackActionOf(face).Id,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    Context(face));
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
                return step;
            }

            private EffectResolutionStepResult Select(
                EffectResolutionStepResult step, CardDefinitionId face, bool friendlyBullet)
            {
                var node = friendlyBullet
                    ? HanuEffectContent.RedFriendlyBulletNode
                    : HanuEffectContent.RedAdjacentBulletNode;
                var invocation = step.Frame.PendingChoice.SelectInvocations
                    .Single(value => value.Candidate.OptionNodeId.Value == node);
                var answered = EffectResolutionSession.Answer(step.Frame, invocation, Context(face));
                Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
                return answered;
            }

            private EffectResolutionStepResult Confirm(
                EffectResolutionStepResult step, CardDefinitionId face)
            {
                var answered = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context(face));
                Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
                return answered;
            }

            private EffectResolutionContext Context(CardDefinitionId face) =>
                new EffectResolutionContext(
                    AttackTextOf(face),
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    new ActiveEffectLifecycle(),
                    1,
                    1,
                    modifications,
                    EffectCardState.Of(definitions, instances),
                    ActionKind.Attack,
                    components: components,
                    heroItems: heroItems,
                    rosterState: EffectRosterState.Absent);

            private ActionOffer Offer(CardDefinitionId face, ActionDefinition action) =>
                new ActionOffer(
                    FaceInstance,
                    Face(face).Reference,
                    action.Id,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    isAvailable: true,
                    attackRange: action.AttackRange,
                    sourceCardStateTransition: default);
        }

        // -------------------------------------------------------------------------------------------
        // Fight and Flight's own board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hanu at the origin after an Attack, with three of the six files blocked in three different
        /// ways - by the target, by a teammate standing in the file, and by a unit occupying the file's
        /// third space.
        /// </summary>
        private sealed class RiderBoard
        {
            private static readonly CardInstanceId FaceInstance =
                new CardInstanceId("hanu-flight-face");

            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MatchComponents components;
            private readonly EffectResolutionContext context;
            private readonly List<EffectResolutionEvent> events = new List<EffectResolutionEvent>();

            private RiderBoard(bool targetRemoved, int radius)
            {
                participants = Participants();
                var heroes = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Hanu, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Friend, new HexCoordinate(-1, 1)),
                    new HeroOccupancy(EnemyBesideFriend, new HexCoordinate(3, 0))
                };
                if (!targetRemoved)
                {
                    heroes.Add(new HeroOccupancy(EnemyBesideBoth, new HexCoordinate(0, -1)));
                }

                if (radius < 3)
                {
                    heroes.RemoveAll(value => !value.HeroId.Equals(Hanu));
                    if (!targetRemoved)
                    {
                        heroes.Add(new HeroOccupancy(EnemyBesideBoth, new HexCoordinate(0, -1)));
                    }
                }

                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(ProbeMap, 1, Disk(radius)), heroes,
                    Array.Empty<ObstacleOccupancy>());
                components = new MatchComponents(
                    MatchContentCatalogs.ForThisBuild().Components,
                    board, new MarkerAttachmentRegister(), board, board, board, board);

                var card = Face(HanuCardContent.FightAndFlight);
                var instances = new[]
                {
                    CardInstance.CreateInHand(FaceInstance, card.Reference, Hanu)
                };
                context = new EffectResolutionContext(
                    AttackTextOf(HanuCardContent.FightAndFlight),
                    MatchContentCatalogs.ForThisBuild().Capabilities,
                    participants,
                    new ActiveEffectLifecycle(),
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(HanuCardContent.AllDefinitions, instances),
                    ActionKind.Attack,
                    components: components,
                    heroItems: new HeroItemState(participants),
                    enclosingAttack: new EnclosingAttackFacts(
                        Hanu, EnemyBesideBoth, default, AttackRangeKind.NonRanged,
                        new HexCoordinate(0, -1), new HexCoordinate(0, 0), default, targetRemoved),
                    rosterState: EffectRosterState.Absent);

                Step = EffectResolutionSession.Start(
                    new EffectResolutionId("hanu-flight-resolution"),
                    FaceInstance,
                    card.Reference,
                    HanuCardContent.PrimaryBasicAttack,
                    Hanu,
                    Hanu,
                    ActionInvocationContext.PrintedPrimary,
                    context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
                events.AddRange(Step.Events ?? Array.Empty<EffectResolutionEvent>());
            }

            public EffectResolutionStepResult Step { get; private set; }

            public static RiderBoard Staged(bool targetRemoved) =>
                new RiderBoard(targetRemoved, radius: 6);

            /// <summary>A disk too small to hold any file of three spaces at all.</summary>
            public static RiderBoard Boxed() => new RiderBoard(targetRemoved: false, radius: 2);

            public HexCoordinate[] Destinations() =>
                Step.Frame?.PendingChoice == null
                    ? Array.Empty<HexCoordinate>()
                    : Step.Frame.PendingChoice.SelectInvocations
                        .Select(invocation => invocation.Candidate.Value.Coordinate)
                        .OrderBy(value => value.Q)
                        .ThenBy(value => value.R)
                        .ToArray();

            public void Answer(HexCoordinate destination)
            {
                var invocation = Step.Frame.PendingChoice.SelectInvocations
                    .Single(value => value.Candidate.Value.Coordinate.Equals(destination));
                Step = EffectResolutionSession.Answer(Step.Frame, invocation, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
                events.AddRange(Step.Events ?? Array.Empty<EffectResolutionEvent>());
            }

            public HexCoordinate HeroSpace(HeroId hero) =>
                board.Occupancies.Single(value => value.HeroId.Equals(hero)).Coordinate;

            /// <summary>What a named node reported, read off the events every step emits.</summary>
            public EffectExecutionResultKind Outcome(string nodeId)
            {
                var recorded = events
                    .Where(value => value.NodeId.Value == nodeId)
                    .ToArray();
                return recorded.Length == 0
                    ? EffectExecutionResultKind.None
                    : recorded[recorded.Length - 1].ResultKind;
            }
        }

        // -------------------------------------------------------------------------------------------
        // Shared staging
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Two teams with TWO heroes on Hanu's side, because "your friendly hero" cannot be true of
        /// anybody on a board where his team fields one - the same reason milestone 8.21 added a second
        /// Blue seat for Brogan's "A friendly hero in range".
        /// </summary>
        private static MatchParticipants Participants()
        {
            var hanuPlayer = new PlayerId("hanu-reds-player");
            var friendPlayer = new PlayerId("hanu-reds-friend-player");
            var farFriendPlayer = new PlayerId("hanu-reds-far-friend-player");
            var enemyPlayers = new[]
            {
                new PlayerId("hanu-reds-enemy-1"),
                new PlayerId("hanu-reds-enemy-2"),
                new PlayerId("hanu-reds-enemy-3"),
                new PlayerId("hanu-reds-enemy-4"),
                new PlayerId("hanu-reds-enemy-5")
            };

            return new MatchParticipants(
                new[] { new Team(BlueTeam, TeamSide.Blue), new Team(RedTeam, TeamSide.Red) },
                new[]
                {
                    new Player(hanuPlayer, BlueTeam),
                    new Player(friendPlayer, BlueTeam),
                    new Player(farFriendPlayer, BlueTeam),
                    new Player(enemyPlayers[0], RedTeam),
                    new Player(enemyPlayers[1], RedTeam),
                    new Player(enemyPlayers[2], RedTeam),
                    new Player(enemyPlayers[3], RedTeam),
                    new Player(enemyPlayers[4], RedTeam)
                },
                new[]
                {
                    new Hero(Hanu, hanuPlayer, HeroDashboard.RulebookLayout),
                    new Hero(Friend, friendPlayer, HeroDashboard.RulebookLayout),
                    new Hero(FarFriend, farFriendPlayer, HeroDashboard.RulebookLayout),
                    new Hero(EnemyBesideBoth, enemyPlayers[0], HeroDashboard.RulebookLayout),
                    new Hero(EnemyBesideFriend, enemyPlayers[1], HeroDashboard.RulebookLayout),
                    new Hero(EnemyAdjacentToHanuOnly, enemyPlayers[2], HeroDashboard.RulebookLayout),
                    new Hero(EnemyBesideFriendlyMinion, enemyPlayers[3], HeroDashboard.RulebookLayout),
                    new Hero(EnemyBesideFarFriend, enemyPlayers[4], HeroDashboard.RulebookLayout)
                });
        }

        private static QuickGameMinionSetupState Minion(
            MinionId id, TeamId team, TeamSide side, HexCoordinate at) =>
            new QuickGameMinionSetupState(
                id,
                team,
                side,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(ProbeMap, new SpawnPointId("hanu-reds-spawn")),
                new ConfiguredSpaceReference(ProbeMap, new SpaceId($"q{at.Q}_r{at.R}")),
                at);

        private static RuleModificationSet Immune(HeroId hero) =>
            new RuleModificationSet(
                new[]
                {
                    AppliedRuleModification.FromContinuousSource(
                        RuleModification.ForImmunity(RuleActivity.All),
                        RuleObject.ForHero(hero),
                        new ContinuousRuleSource(
                            "hanu-reds-immunity", ContinuousRuleSourceKind.Ultimate, hero, "probe",
                            UltimateRuleDefinition.ApplicabilityKind,
                            new CardInstanceId("hanu-reds-immunity-card")),
                        TeamSide.Red,
                        0)
                },
                Array.Empty<AppliedObstacleOverlay>(),
                TieBreakerState.Unrevealed);

        private static HexCoordinate[] Disk(int radius)
        {
            var coordinates = new List<HexCoordinate>();
            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    if (Math.Abs(q + r) <= radius)
                    {
                        coordinates.Add(new HexCoordinate(q, r));
                    }
                }
            }

            return coordinates.ToArray();
        }
    }
}
