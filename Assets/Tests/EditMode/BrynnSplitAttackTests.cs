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
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.69b. Brynn the Seeker's three Red faces whose text hangs off the Attack's own
    /// target: Split Attack, Split Throw and Peak Precision.
    ///
    /// <para><b>One printed condition, three consequences.</b> All three print "If you target a hero
    /// who is adjacent to 3 or more obstacles" and then a may-repeat narrowed to p.19's ring, an
    /// if-able repeat over the action's own reach, or a +2 and an optional retrieval of the card
    /// itself. The condition is one contract - milestone 8.69b's
    /// <c>brynn-attack-target-was-adjacent-to-obstacles@1</c> - asked of the enclosing Attack, so the
    /// three faces cannot come to disagree about what their shared sentence means.</para>
    ///
    /// <para><b>The two repeats are driven through a whole turn</b>, on Silverarrow's Rain of Arrows
    /// fixture's own driver, because what is under test is a candidate SET a repeated instance is
    /// offered and only the page can build one. The board is built rather than staged, for milestone
    /// 8.69a's reason: nothing of his is registered until 8.69g, so no configured match can deal him -
    /// which is also why the snapshot and Undo journeys across these pauses belong to that block and
    /// its gate rather than to this one. Everything this milestone adds is DERIVED from state the
    /// codec already writes, which
    /// <see cref="NothingThisBlockAddsIsPersisted_SoNoSchemaVersionMoves"/> is the containment proof
    /// for.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class BrynnSplitAttackTests
    {
        private static readonly HeroId Brynn = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId BoxedIn = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId InTheOpen =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly MinionId Minion = new MinionId("brynn-split-minion");

        private static readonly MapId Map = new MapId("brynn-split-attacks");

        private static readonly HexCoordinate Origin = new HexCoordinate(0, 0);

        // -------------------------------------------------------------------------------------------
        // The composition, read off the definitions
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The three faces carry the numbers the shard's printed inventory records, and Peak Precision
        /// declares no reach icon at all - BR-06, "a missing icon is absent, never zero" - while its
        /// ITEM slot is a Radius, which p.16 makes a fact about a card somebody tucks under a dashboard
        /// rather than about this action.
        /// </summary>
        [Test]
        public void TheThreeFaces_CarryThePrintedValuesTheShardRecords()
        {
            var split = Face(BrynnCardContent.SplitAttack);
            Assert.That(split.Initiative.Value, Is.EqualTo(9));
            Assert.That(PrintedValue(split, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(3));
            Assert.That(PrintedValue(split, ActionKind.Attack, PrintedValueChannel.Range),
                Is.EqualTo(2));
            Assert.That(PrintedValue(split, ActionKind.Movement, PrintedValueChannel.Movement),
                Is.EqualTo(4));
            Assert.That(PrintedValue(split, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(3));
            Assert.That(Attack(split).AttackRange, Is.EqualTo(AttackRangeKind.Ranged));
            Assert.That(split.Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(split.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Defense));
            Assert.That(split.Progression.NextTierCandidateIds, Is.EquivalentTo(new[]
                {
                    BrynnCardContent.PeakPrecision, BrynnCardContent.SplitThrow
                }),
                "p.15 forks Red at Tier II, and these are the two halves of the fork THIS block " +
                "composes; 8.69f states the ladders whole.");

            var throwFace = Face(BrynnCardContent.SplitThrow);
            Assert.That(throwFace.Initiative.Value, Is.EqualTo(10));
            Assert.That(PrintedValue(throwFace, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(3));
            Assert.That(PrintedValue(throwFace, ActionKind.Attack, PrintedValueChannel.Range),
                Is.EqualTo(2));
            Assert.That(PrintedValue(throwFace, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(4));
            Assert.That(throwFace.Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(throwFace.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Defense));

            var peak = Face(BrynnCardContent.PeakPrecision);
            Assert.That(peak.Initiative.Value, Is.EqualTo(10));
            Assert.That(PrintedValue(peak, ActionKind.Attack, PrintedValueChannel.Attack),
                Is.EqualTo(5),
                "The printed \"+\" is the contribution and not a second number - p.14 states the " +
                "notation outright.");
            Assert.That(PrintedValue(peak, ActionKind.Defense, PrintedValueChannel.Defense),
                Is.EqualTo(7));
            Assert.That(Attack(peak).AttackRange, Is.EqualTo(AttackRangeKind.NonRanged),
                "\"Target a unit adjacent to you\" is p.13's own reading of a non-Ranged Attack.");
            Assert.That(
                Attack(peak).BaseValues.Any(value =>
                    value.Channel == PrintedValueChannel.Range ||
                    value.Channel == PrintedValueChannel.Radius),
                Is.False,
                "BR-06: it prints no reach icon, so it declares none - a missing icon is absent, " +
                "never zero.");
            Assert.That(peak.Progression.ItemIcon, Is.EqualTo(PrintedValueChannel.Radius));
            Assert.That(peak.Progression.NextTierCandidateIds, Is.Empty);
        }

        /// <summary>
        /// The two repeat clauses, read off the definitions: the printed "may" and its absence, and the
        /// binding each face's own words name.
        ///
        /// <para>Split Throw prints no "may", which in this build is
        /// <see cref="EffectFailurePolicy.IfAble"/> and cannot be anything else -
        /// <c>EffectNodeDefinition</c> refuses a mandatory repeat outright ("a mandatory repeat is
        /// unbounded"), and BR-Q5 asks for the impossible step to leave the first instance's outcome
        /// standing, which is what a skip does and a failure does not. Rain of Arrows' two clauses are
        /// the precedent, word for word.</para>
        /// </summary>
        [Test]
        public void TheTwoRepeats_DeclareThePrintedMayAndThePrintedReach()
        {
            var split = RepeatNode(BrynnCardContent.SplitAttack);
            Assert.That(split.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
            Assert.That(split.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "p.10 gives an unassigned decision to the card's controller.");
            Assert.That(split.MaximumRepeats, Is.EqualTo(1));
            Assert.That(split.RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentUnitAdjacentToActor));

            var throwRepeat = RepeatNode(BrynnCardContent.SplitThrow);
            Assert.That(throwRepeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.IfAble));
            Assert.That(throwRepeat.Chooser, Is.EqualTo(EffectChooserRole.None),
                "Nobody is asked, because the face prints no \"may\".");
            Assert.That(throwRepeat.RepeatTargetBinding,
                Is.EqualTo(RepeatTargetBinding.DifferentTarget),
                "\"a different target unit\" keeps the action's own reach - milestone 8.10's member, " +
                "unchanged.");

            Assert.That(
                Attack(Face(BrynnCardContent.SplitAttack)).CardTextTiming,
                Is.EqualTo(CardTextTiming.BeforeTheAttack),
                "The gate is about the target this instance bound, and p.14's step 1 is where the " +
                "target is already chosen (BR-Q5) - Arien's Violent Torrent's own moment.");
            Assert.That(
                Attack(Face(BrynnCardContent.PeakPrecision)).CardTextTiming,
                Is.EqualTo(CardTextTiming.AfterTheAttack),
                "and Peak Precision prints \"After the attack:\" in so many words.");
        }

        /// <summary>
        /// The seventh binding member is appended without renumbering the six before it, so no
        /// definition written earlier reads as a different clause; and it names no selection node, for
        /// the reason every Different arm about the ACTION's own target names none.
        /// </summary>
        [Test]
        public void TheRepeatBinding_IsTheSeventhMember_AndNamesNoSelectionNode()
        {
            Assert.That((int)RepeatTargetBinding.DifferentUnitAdjacentToActor, Is.EqualTo(6));
            Assert.That((int)RepeatTargetBinding.DifferentTarget, Is.EqualTo(2));
            Assert.That((int)RepeatTargetBinding.DifferentSelectionSubject, Is.EqualTo(5));

            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("probe-repeat"), EffectNodeKind.RepeatAction,
                    EffectFailurePolicy.May,
                    chooser: EffectChooserRole.Actor,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentUnitAdjacentToActor,
                    boundSelectionNodeId: new EffectNodeId("probe-selection")),
                Throws.ArgumentException,
                "What it excludes is the Attack's own target, so naming a selection node is refused " +
                "exactly as it is for the three arms beside it.");

            Assert.That(
                () => new EffectNodeDefinition(
                    new EffectNodeId("probe-repeat"), EffectNodeKind.RepeatAction,
                    EffectFailurePolicy.Mandatory,
                    maximumRepeats: 1,
                    repeatTargetBinding: RepeatTargetBinding.DifferentTarget),
                Throws.ArgumentException,
                "and a mandatory repeat is refused outright, which is why Split Throw's clause is " +
                "`if able` rather than mandatory.");
        }

        // -------------------------------------------------------------------------------------------
        // The gate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// THE HEADLINE READING. The gate is about the TARGETING, so it still answers true about a
        /// target rulebook p.14's step 6 has already taken off the board - which is the common case for
        /// Peak Precision, whose retrieval is applied at step 7 of an Attack that may well have
        /// defeated the hero it read.
        ///
        /// <para>The space is milestone 8.19a's <c>TargetSpaceBeforeConsequences</c>, captured when the
        /// target was validated and carried for exactly this - Brogan's Onslaught and Min's Stances
        /// already read it at this step. Nothing is recorded for this milestone.</para>
        /// </summary>
        [Test]
        public void TheGate_IsAboutTheTargeting_SoARemovedTargetIsStillTheHeroItChose()
        {
            var boxed = new HexCoordinate(2, 0);
            var board = Board(
                heroes: new[] { At(Brynn, Origin), At(BoxedIn, boxed) },
                terrain: BoxSpaces(boxed));

            Assert.That(Gate(board, AttackOn(BoxedIn, boxed)), Is.True,
                "A hero standing beside three obstacles satisfies the printed clause.");

            // p.14 step 6: the attack succeeded and the defender has been removed - the same board
            // with nobody standing on that space any more.
            var removed = Board(
                heroes: new[] { At(Brynn, Origin) },
                terrain: BoxSpaces(boxed));

            Assert.That(Gate(removed, AttackOn(BoxedIn, boxed)), Is.True,
                "and the clause is still satisfied at step 7, because \"if you TARGET a hero who " +
                "is...\" is a fact about a choice that already happened. Milestone 8.69a's " +
                "subject-scoped predicate answers NOTHING here, by design - which is why this block " +
                "asks the Attack rather than the hero.");
        }

        /// <summary>
        /// The two ordinary answers and the one refusal. A minion target is a legal Attack whose
        /// condition does not hold; a hero in the open is the same; a composition with no Attack around
        /// it cannot be asked at all.
        /// </summary>
        [Test]
        public void TheGate_AnswersFalseForAMinionOrAnOpenHero_AndRefusesOutsideAnAttack()
        {
            var open = new HexCoordinate(2, 0);
            var board = Board(
                heroes: new[] { At(Brynn, Origin), At(InTheOpen, open) },
                minions: new[] { MinionAt(new HexCoordinate(1, 0)) });

            Assert.That(Gate(board, AttackOn(InTheOpen, open)), Is.False,
                "A hero with no obstacles beside them is the clause not being met, and a false " +
                "answer is a rule.");

            Assert.That(
                Gate(board, new EnclosingAttackFacts(
                    Brynn, default, Minion, AttackRangeKind.NonRanged, new HexCoordinate(1, 0))),
                Is.False,
                "A minion target answers false rather than refusing: p.13 lets an Attack take a " +
                "unit of either kind, and the conditional clause prints \"a HERO\".");

            Assert.That(
                RunGate(board, EnclosingAttackFacts.None).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory),
                "\"This cannot be asked\" and \"the answer is no\" are different facts, and only " +
                "one of them is a rule.");
        }

        /// <summary>
        /// Over the Top reaches this gate without the gate naming an Ultimate: the counts-as overlay is
        /// consulted inside <see cref="ObstacleAdjacencyQuery"/>, so a hero standing alone in the
        /// middle of an empty map satisfies all three faces' condition while Brynn acts.
        /// </summary>
        [Test]
        public void OverTheTop_MakesEveryEnemyHeroSatisfyTheGate_WhileHeActsAndNotOtherwise()
        {
            var open = new HexCoordinate(2, 0);
            var board = Board(heroes: new[] { At(Brynn, Origin), At(BoxedIn, open) });
            var participants = ThreeHeroes();

            Assert.That(Gate(board, AttackOn(BoxedIn, open)), Is.False,
                "Nothing stands beside him, so without the Ultimate the clause fails.");

            Assert.That(
                Gate(board, AttackOn(BoxedIn, open), WhileActing(Brynn, board, participants),
                    participants),
                Is.True,
                "\"While you are performing actions all enemy heroes in play count as adjacent to 3 " +
                "or more obstacles\" - and this block's predicate inherits it by asking 8.69a's " +
                "query rather than counting neighbours of its own.");

            Assert.That(
                Gate(board, AttackOn(BoxedIn, open), WhileActing(BoxedIn, board, participants),
                    participants),
                Is.False,
                "and while somebody else is the acting hero the overlay is not derived at all.");
        }

        /// <summary>
        /// MILESTONE 8.69H'S CORRECTION, and the case a Play Mode journey of his found. The overlay's
        /// answer has to survive rulebook p.14's step 6 exactly as the captured space does, or Peak
        /// Precision applies its printed +2 at step 4 and refuses its printed retrieval at step 7 - on
        /// the same printed condition, and precisely when the attack succeeded.
        ///
        /// <para>The board is the one the case above uses: nothing stands beside the target, so the
        /// clause holds for one reason only. Removing him is p.14's step 6, and the fallback this block
        /// built reads the captured SPACE - whose geometry never carried the hero-scoped counts-as and
        /// never could. So the loss was in the overlay's own derivation, which until this milestone
        /// expanded "all enemy heroes in play" over the BOARD's standing pieces; it now expands over
        /// the match's enemy heroes, and no other consumer can reach a hero standing nowhere.</para>
        /// </summary>
        [Test]
        public void OverTheTopsAnswer_SurvivesPage14RemovingTheTargetItWasReadAbout()
        {
            var open = new HexCoordinate(2, 0);
            var participants = ThreeHeroes();
            var standing = Board(heroes: new[] { At(Brynn, Origin), At(BoxedIn, open) });

            Assert.That(
                Gate(standing, AttackOn(BoxedIn, open),
                    WhileActing(Brynn, standing, participants), participants),
                Is.True,
                "Arrange: the target satisfies the clause through the Ultimate and nothing else.");

            var removed = Board(heroes: new[] { At(Brynn, Origin) });
            Assert.That(
                Gate(removed, AttackOn(BoxedIn, open),
                    WhileActing(Brynn, removed, participants), participants),
                Is.True,
                "and the same question asked at step 7, about the same targeting, still answers " +
                "true - which is what BR-Q14b requires and what this block's fallback could not " +
                "supply on its own.");

            Assert.That(
                Gate(removed, AttackOn(BoxedIn, open)),
                Is.False,
                "without the Ultimate the removed target's captured space is open ground, so the " +
                "clause fails for the reason it always did.");
        }

        // -------------------------------------------------------------------------------------------
        // Split Attack, driven
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// SPLIT ATTACK'S WHOLE SENTENCE. The printed instance reaches Range 2; the repeat reaches
        /// p.19's innermost ring alone, minus the unit the first instance took.
        ///
        /// <para>The board holds the difference in one picture: a boxed-in hero at Range 2, a hero in
        /// the open at Range 2, and a minion standing beside Brynn. The printed instance is offered all
        /// three. The repeat is offered the minion and nobody else - the hero in the open is in range
        /// and not adjacent, which is the whole of what the new binding says.</para>
        /// </summary>
        [Test]
        public void SplitAttack_RepeatsOnlyOverTheRingAroundHim_AndOnlyAfterAQualifyingHero()
        {
            var instances = DriveSplit(BrynnCardContent.SplitAttack, TakeTheBoxedInHero);

            Assert.That(instances, Is.EqualTo(new[]
            {
                "brynn-split: hero:blue-ally-hero, hero:blue-test-hero, minion:brynn-split-minion",
                "brynn-split:repeat: minion:brynn-split-minion"
            }),
                "\"may repeat once on a different unit adjacent to you\": the hero in the open is " +
                "inside the printed Range 2 and outside p.19's ring, so the card's own words " +
                "override p.10's \"the entire action again\".");
        }

        /// <summary>
        /// The two halves of "a DIFFERENT unit adjacent to you", measured apart: with the qualifying
        /// hero standing beside Brynn, the ring holds two units and the repeat is offered one.
        /// </summary>
        [Test]
        public void SplitAttack_ExcludesTheUnitItTook_EvenWhenThatUnitStandsBesideHim()
        {
            var instances = DriveSplit(
                BrynnCardContent.SplitAttack, TakeTheBoxedInHero, boxedInHeroIsAdjacent: true);

            Assert.That(instances.Count, Is.EqualTo(2));
            Assert.That(instances[1], Is.EqualTo("brynn-split:repeat: minion:brynn-split-minion"),
                "Both units stand beside him; the one the first instance took is the one the repeat " +
                "may not take.");
        }

        /// <summary>
        /// The gate refusing, from both sides p.13 makes available: a hero with only two obstacles
        /// beside them, and a minion. Neither produces a second instance, and neither stops the Attack.
        /// </summary>
        [Test]
        public void SplitAttack_OffersNoRepeatAfterAnOpenHeroOrAMinion()
        {
            Assert.That(
                DriveSplit(BrynnCardContent.SplitAttack, TakeTheOpenHero).Count, Is.EqualTo(1),
                "\"If you target a hero who is adjacent to 3 or more obstacles\" - two is not three.");

            Assert.That(
                DriveSplit(BrynnCardContent.SplitAttack, TakeTheMinion).Count, Is.EqualTo(1),
                "and a minion target is an ordinary legal Attack whose condition simply does not " +
                "hold, rather than a text that failed.");
        }

        /// <summary>
        /// Milestone 8.35z15's rule, applied to the new binding: an offer is an offer, so a repeat
        /// whose ring holds nobody is not opened for confirmation and then dropped. The board is the
        /// qualifying case with every other unit taken out of p.19's innermost ring, so the gate holds
        /// and only the reach refuses.
        /// </summary>
        [Test]
        public void SplitAttack_WithAnEmptyRing_IsNeverOfferedForConfirmation()
        {
            var run = DriveSplitRun(
                BrynnCardContent.SplitAttack, TakeTheBoxedInHero, aloneOnTheBoard: true);

            Assert.That(run.Offered.Count, Is.EqualTo(1));
            Assert.That(run.OpenedAConfirmOrDecline, Is.False,
                "p.10's confirm-or-decline is asked of a second instance that could actually be " +
                "performed; a ring with nobody in it is a repeat this node has nothing to offer.");
        }

        // -------------------------------------------------------------------------------------------
        // Split Throw, driven
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// SPLIT THROW'S WHOLE SENTENCE. Same first two sentences, same gate, and a repeat that keeps
        /// the action's own Range 2 - so the hero in the open, refused by Split Attack's ring, is
        /// offered here. Nobody is asked whether to repeat: the face prints no "may".
        /// </summary>
        [Test]
        public void SplitThrow_RepeatsOverTheActionsOwnReach_WithoutAskingAnybody()
        {
            var instances = DriveSplit(BrynnCardContent.SplitThrow, TakeTheBoxedInHero);

            Assert.That(instances, Is.EqualTo(new[]
            {
                "brynn-split: hero:blue-ally-hero, hero:blue-test-hero, minion:brynn-split-minion",
                "brynn-split:repeat: hero:blue-ally-hero, minion:brynn-split-minion"
            }),
                "\"repeat once on a different target unit\": everything the action reaches except " +
                "the unit it took - which is milestone 8.10's binding, unchanged, and is exactly " +
                "what Split Attack's clause is narrower than.");
        }

        /// <summary>
        /// BR-Q5's second half: with no different unit left in range, the mandatory-where-possible
        /// repeat is p.10's impossible step. The Attack ends with one instance and the first
        /// instance's outcome stands - it is a step that could not happen rather than an action that
        /// stopped.
        /// </summary>
        [Test]
        public void SplitThrow_WithNobodyElseInRange_IsAnImpossibleStep_AndTheFirstOutcomeStands()
        {
            var instances = DriveSplit(
                BrynnCardContent.SplitThrow, TakeTheBoxedInHero, aloneOnTheBoard: true);

            Assert.That(instances.Count, Is.EqualTo(1),
                "`if able` asks nobody and performs nothing where nothing is left, which is p.10's " +
                "silent skip - and the printed instance's attack still happened.");
        }

        /// <summary>
        /// p.10's "You cannot repeat a repeated action!". Neither face prints a chain, so the instance
        /// a repeat produced never reaches the clause again - measured as the driver reaching its end
        /// after exactly two instances on a board that would otherwise still hold a legal third.
        /// </summary>
        [Test]
        public void NeitherRepeat_IsAskedAgainByTheInstanceItProduced()
        {
            Assert.That(DriveSplit(BrynnCardContent.SplitThrow, TakeTheBoxedInHero).Count,
                Is.EqualTo(2));
            Assert.That(DriveSplit(BrynnCardContent.SplitAttack, TakeTheBoxedInHero).Count,
                Is.EqualTo(2));
        }

        // -------------------------------------------------------------------------------------------
        // Peak Precision
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The +2 is milestone 8.69a's contribution reused rather than restated, so the three faces
        /// printing "+2 [Attack] attack" read one declaration; and the two split faces print no "+" at
        /// all, so they declare none.
        /// </summary>
        [Test]
        public void ThePlusTwo_IsOnPeakPrecisionAlone_AndIsTheContributionEightySixNineAOwns()
        {
            var peak = Attack(Face(BrynnCardContent.PeakPrecision));
            var declared = peak.RosterValueContributions.Single();
            Assert.That(declared.Channel, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(declared.Query,
                Is.EqualTo(RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles));
            Assert.That(declared.AmountPerMatch, Is.EqualTo(2));

            foreach (var id in new[] { BrynnCardContent.SplitAttack, BrynnCardContent.SplitThrow })
            {
                Assert.That(Attack(Face(id)).RosterValueContributions, Is.Empty,
                    "Neither split face prints a \"+\", so neither declares a contribution - which " +
                    "is why no repeat in his set has a +2 question of its own.");
            }
        }

        /// <summary>
        /// PEAK PRECISION'S WHOLE SENTENCE, driven: a qualifying hero beside him, the Attack Total
        /// raised to 7, the optional retrieval offered at p.14's step 7, the card back in his hand -
        /// and the action completing and the turn moving on with it.
        /// </summary>
        [Test]
        public void PeakPrecision_RaisesTheTotal_ThenOffersTheRetrieval_AndTheActionStillCompletes()
        {
            var run = DrivePeakPrecision(qualifying: true, retrieve: true);

            Assert.That(run.AttackTotal, Is.EqualTo(7),
                "Printed 5 with the +2 the condition adds, read where p.14 computes the total.");
            Assert.That(run.RetrievalWasOffered, Is.True,
                "\"You may retrieve this card\" is a decision the actor is given - the Optional " +
                "wrapper, because a retrieval that already names its card has nothing to ask.");
            Assert.That(run.SourceCardLocation, Is.EqualTo(CardLocation.Hand));
            Assert.That(run.SourceCardHasBeenPlayed, Is.False,
                "It comes back as an ordinary card in hand, so it may be committed again in a later " +
                "round exactly as any other.");
            Assert.That(run.ActionCompleted, Is.True,
                "BR-Q7: the action still completes - it is already resolving, and what the text " +
                "changed is where the card is rather than whether the page finished.");
            Assert.That(run.SelectedActorCleared, Is.True,
                "and Initiative acknowledged the finished action, which is what keeps the turn from " +
                "hanging on a card that is no longer in the turn area.");
        }

        /// <summary>
        /// Declining is a decline and not a failure: the card stays where a played card stays and the
        /// action completes exactly as it does when the retrieval is taken.
        /// </summary>
        [Test]
        public void PeakPrecision_DeclinedRetrieval_LeavesTheCardWhereAPlayedCardEnds()
        {
            var run = DrivePeakPrecision(qualifying: true, retrieve: false);

            Assert.That(run.RetrievalWasOffered, Is.True);
            Assert.That(run.SourceCardLocation, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(run.SourceCardHasBeenPlayed, Is.True);
            Assert.That(run.ActionCompleted, Is.True);
        }

        /// <summary>
        /// The condition gates BOTH halves, which is what the printed "and" says (BR-Q7): against a
        /// hero with two obstacles beside them the total is the printed 5 and nobody is offered
        /// anything.
        /// </summary>
        [Test]
        public void PeakPrecision_AgainstAnOpenHero_RaisesNothingAndOffersNothing()
        {
            var run = DrivePeakPrecision(qualifying: false, retrieve: true);

            Assert.That(run.AttackTotal, Is.EqualTo(5));
            Assert.That(run.RetrievalWasOffered, Is.False);
            Assert.That(run.SourceCardLocation, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(run.ActionCompleted, Is.True);
        }

        /// <summary>
        /// THE THIRD POOL, isolated. <c>retrieve-card@1</c>'s two existing pools are rulebook p.8's
        /// cards at rest, and neither reaches a card whose own action is running; the new pool reaches
        /// that card and nothing else. Both directions are measured, because a pool that quietly
        /// widened would let "retrieve a discarded card" pick up the card in play.
        /// </summary>
        [Test]
        public void TheRetrievalPool_IsAThirdPool_AndNeitherOfTheOtherTwoReachesTheResolvingCard()
        {
            Assert.That((int)RosterCardRetrievalKind.OwnedResolvingSourceCard, Is.EqualTo(3));

            var definition = Face(BrynnCardContent.PeakPrecision);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-retrieve-probe"), definition.Reference, Brynn);

            Assert.That(CardLifecycleAuthority.IsResolvingCardInTurnArea(card), Is.False,
                "A card in hand is not a card being resolved.");
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            Assert.That(CardLifecycleAuthority.IsResolvingCardInTurnArea(card), Is.True,
                "Played, face up in the turn area and not yet resolved - the one state p.14's step 7 " +
                "finds the card it is printed on in.");

            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(card), Is.False,
                "p.8's end-of-round retrieval does not reach it, and milestone 6.13's audit forbids " +
                "widening that question - which is why this is a second transition.");

            Assert.That(CardLifecycleAuthority.TryRetrieveResolvingCard(card), Is.True);
            Assert.That(card.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(card.State.HasBeenPlayed, Is.False);
            Assert.That(card.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.None));
            Assert.That(CardLifecycleAuthority.WasRetrievedDuringItsOwnAction(card), Is.True,
                "and the shape the two turn seams recognise is the one it leaves behind.");

            Assert.That(CardLifecycleAuthority.TryRetrieveResolvingCard(card), Is.False,
                "A card already in hand is not a card being resolved, so the transition refuses it " +
                "rather than applying twice.");
        }

        // -------------------------------------------------------------------------------------------
        // The deferral and the containment
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 7.9's gate, asserted from both sides and now from the far end of it: this block's
        /// one contract is composed and REGISTERED since milestone 8.69g dealt him, the deferral list
        /// that used to name it is empty, and the registration is what makes its three faces startable.
        /// Because it is a new handler TYPE, 8.69g also owed it a showcase consumer - the Red
        /// Quarryman's "After the attack" rider, which is an Attack rather than a Skill for the same
        /// reason this predicate is: it is a fact about the Attack that is running.
        /// </summary>
        [Test]
        public void ThisBlocksContractIsRegistered_AndTheDeferralListIsEmpty()
        {
            Assert.That(BrynnComponentContent.SplitAttackDeferredHandlers(), Is.Empty);

            var registeredHere = BrynnComponentContent.SplitAttackRegisteredHandlers().ToArray();
            Assert.That(
                registeredHere.Select(handler => handler.Reference.ToString()), Is.EqualTo(new[]
            {
                BrynnComponentContent.AttackTargetWasAdjacentToObstaclesV1.ToString()
            }));

            var registered = MatchContentCatalogs.ForThisBuild();
            Assert.That(
                registered.Capabilities.IsRegistered(
                    BrynnComponentContent.AttackTargetWasAdjacentToObstaclesV1),
                Is.True,
                "Split Attack, Split Throw and Peak Precision are cards a configured match now " +
                "deals, so the build must be able to start the gate all three of them read.");

            Assert.That(BrynnCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "and milestone 8.69f states his set: the eighteen p.6 deals, with the boxed Handicap "
                + "the nineteenth. With the registration in place the dealt-corpus validator refuses "
                + "none of them.");

            foreach (var id in new[]
            {
                BrynnCardContent.SplitAttack, BrynnCardContent.SplitThrow,
                BrynnCardContent.PeakPrecision
            })
            {
                Assert.That(Face(id).ContentRevision,
                    Is.EqualTo(BrynnCardContent.ContentRevision),
                    "Every face this block composes carries the one real revision milestone 8.69f " +
                    "stated his whole set under, which replaced this block's placeholder.");
            }
        }

        /// <summary>
        /// Everything this milestone added is derived from state the codec already writes - the repeat
        /// binding is read off the trusted definition when a repeated instance builds its candidates,
        /// the gate is read off the enclosing Attack's own facts, and the retrieval's whole effect is a
        /// <c>CardStateFacts</c> the snapshot has written since milestone 6.x. So no schema version
        /// moves, which is asserted rather than asserted-about-in-prose.
        /// </summary>
        [Test]
        public void NothingThisBlockAddsIsPersisted_SoNoSchemaVersionMoves()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(45),
                "Milestone 8.71a moved it to v44 for Hanu's rewritten printed Initiative - a " +
                "sparse card-keyed list this block owns and nothing here writes.");

            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-restore-probe"),
                Face(BrynnCardContent.PeakPrecision).Reference,
                Brynn);
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryRetrieveResolvingCard(card), Is.True);

            var rebuilt = CardInstance.CreateInHand(
                new CardInstanceId("brynn-restore-probe"),
                Face(BrynnCardContent.PeakPrecision).Reference,
                Brynn);
            Assert.That(rebuilt.State, Is.EqualTo(card.State),
                "The state a retrieval leaves is the ordinary in-hand one, so a restore that " +
                "rebuilds a card from its written facts reaches the identical position and the two " +
                "turn seams reach the identical verdict on it.");
        }

        // -------------------------------------------------------------------------------------------
        // Driving the two split faces
        // -------------------------------------------------------------------------------------------

        private const int TakeTheBoxedInHero = 0;
        private const int TakeTheOpenHero = 1;
        private const int TakeTheMinion = 2;

        /// <summary>
        /// One Split Attack or Split Throw resolution driven to its end, reporting each instance's
        /// identity and the candidate set the page offered it.
        ///
        /// <para>Silverarrow's Rain of Arrows fixture's driver, with his board: Brynn at the origin, a
        /// boxed-in enemy hero and an enemy hero in the open both at Range 2, and an enemy minion
        /// standing beside him. Three terrain spaces around the boxed-in hero are the only thing
        /// separating the two heroes, so every case turns on the printed clause and nothing else.</para>
        /// </summary>
        private sealed class SplitRun
        {
            public readonly List<string> Offered = new List<string>();
            public bool OpenedAConfirmOrDecline;
        }

        private static IReadOnlyList<string> DriveSplit(
            CardDefinitionId faceId,
            int firstTarget,
            bool boxedInHeroIsAdjacent = false,
            bool aloneOnTheBoard = false) =>
            DriveSplitRun(faceId, firstTarget, boxedInHeroIsAdjacent, aloneOnTheBoard).Offered;

        private static SplitRun DriveSplitRun(
            CardDefinitionId faceId,
            int firstTarget,
            bool boxedInHeroIsAdjacent = false,
            bool aloneOnTheBoard = false)
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
            var face = Face(faceId);
            var attack = Attack(face);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("brynn-split-card"), face.Reference, Brynn);

            var walls = DefenceWalls();
            var definitions = configuration.CardDefinitions
                .Concat(new[] { face, DefenceWall }).ToArray();
            var instances = configuration.CardInstances
                .Concat(new[] { played }).Concat(walls).ToArray();

            var boxed = boxedInHeroIsAdjacent ? new HexCoordinate(1, 0) : new HexCoordinate(2, 0);
            var minionSpace = boxedInHeroIsAdjacent
                ? new HexCoordinate(0, 1)
                : new HexCoordinate(1, 0);

            var heroes = new List<HeroOccupancy>
            {
                At(Brynn, Origin),
                At(BoxedIn, boxed),

                // In range and in the open - the hero Split Attack's ring refuses and Split Throw's
                // reach admits. Parked far outside both when the case is about an empty ring.
                At(InTheOpen, aloneOnTheBoard ? new HexCoordinate(-4, 0) : new HexCoordinate(0, 2))
            };

            var board = Board(
                heroes,
                terrain: BoxSpaces(boxed),
                minions: aloneOnTheBoard
                    ? Array.Empty<QuickGameMinionSetupState>()
                    : new[] { MinionAt(minionSpace) });

            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    BrynnComponentContent.SplitAttackRegisteredHandlers()),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var cardActions = new CardActionResolutionAuthority(catalogs, board);

            CommitEveryHand(configuration, played);

            var turn = new TurnResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);

            var run = new SplitRun();
            var started = false;
            for (var step = 0; step < 80 && !turn.IsEnded; step++)
            {
                if (!started && Advance(turn, played, out var reachedTheCard))
                {
                    if (!reachedTheCard) continue;

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Brynn, played.Id)
                        .Single(value => value.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(
                        turn.TryStartAttack(new ActionResolutionId("brynn-split"), offer).Accepted,
                        Is.True);
                    started = true;
                    continue;
                }

                if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    run.Offered.Add(choice.ResolutionId.Value + ": " + string.Join(
                        ", ",
                        choice.LegalTargets.Select(Describe)
                            .OrderBy(value => value, StringComparer.Ordinal)));

                    var target = run.Offered.Count == 1
                        ? FirstTarget(choice.LegalTargets, firstTarget)
                        : choice.LegalTargets[0];
                    Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, target)).Accepted, Is.True);
                    continue;
                }

                if (AnsweredADefense(turn))
                {
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    run.OpenedAConfirmOrDecline = true;
                    AnsweredAnEffectChoice(turn);
                    continue;
                }

                break;
            }

            Assert.That(started, Is.True, "The face never reached its own action.");
            return run;
        }

        private static AttackTarget FirstTarget(
            IReadOnlyList<AttackTarget> legal, int which)
        {
            switch (which)
            {
                case TakeTheBoxedInHero:
                    return legal.Single(value =>
                        value.Kind == AttackTargetKind.Hero && value.HeroId.Equals(BoxedIn));
                case TakeTheOpenHero:
                    return legal.Single(value =>
                        value.Kind == AttackTargetKind.Hero && value.HeroId.Equals(InTheOpen));
                default:
                    return legal.Single(value => value.Kind == AttackTargetKind.Minion);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Driving Peak Precision
        // -------------------------------------------------------------------------------------------

        private sealed class PeakPrecisionRun
        {
            public int AttackTotal;
            public bool RetrievalWasOffered;
            public bool ActionCompleted;
            public bool SelectedActorCleared;
            public CardLocation SourceCardLocation;
            public bool SourceCardHasBeenPlayed;
        }

        private static PeakPrecisionRun DrivePeakPrecision(bool qualifying, bool retrieve)
        {
            var configuration =
                MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red);
            var face = Face(BrynnCardContent.PeakPrecision);
            var attack = Attack(face);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("brynn-peak-card"), face.Reference, Brynn);

            var walls = DefenceWalls();
            var definitions = configuration.CardDefinitions
                .Concat(new[] { face, DefenceWall }).ToArray();
            var instances = configuration.CardInstances
                .Concat(new[] { played }).Concat(walls).ToArray();

            // Adjacent, because Peak Precision is a non-Ranged Attack. Brynn himself is one of the
            // three obstacles when the case is the qualifying one, exactly as his Gold's reminder
            // says, so only two terrain spaces are staged for it.
            var defender = new HexCoordinate(1, 0);
            var board = Board(
                heroes: new[]
                {
                    At(Brynn, Origin),
                    At(BoxedIn, defender),
                    At(InTheOpen, new HexCoordinate(-3, 0))
                },
                terrain: qualifying
                    ? new[] { new HexCoordinate(2, 0), new HexCoordinate(2, -1) }
                    : Array.Empty<HexCoordinate>());

            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    BrynnComponentContent.SplitAttackRegisteredHandlers()),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var cardActions = new CardActionResolutionAuthority(catalogs, board);

            CommitEveryHand(configuration, played);

            var turn = new TurnResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);

            var run = new PeakPrecisionRun();
            var started = false;
            for (var step = 0; step < 80 && !turn.IsEnded; step++)
            {
                if (!started && Advance(turn, played, out var reachedTheCard))
                {
                    if (!reachedTheCard) continue;

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Brynn, played.Id)
                        .Single(value => value.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(
                        turn.TryStartAttack(new ActionResolutionId("brynn-peak"), offer).Accepted,
                        Is.True);
                    started = true;
                    continue;
                }

                if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    Assert.That(turn.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                            choice.ResolutionId, choice.ChoiceId,
                            choice.LegalTargets.Single(value =>
                                value.Kind == AttackTargetKind.Hero &&
                                value.HeroId.Equals(BoxedIn)))).Accepted,
                        Is.True);
                    continue;
                }

                if (turn.PendingDefenseChoice != null)
                {
                    run.AttackTotal = turn.PendingDefenseChoice.AttackTotal;
                    AnsweredADefense(turn);
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    var effect = turn.PendingEffectChoice;
                    run.RetrievalWasOffered = true;
                    Assert.That(
                        turn.TryAnswerEffectChoice(
                                retrieve ? effect.ConfirmInvocation : effect.DeclineInvocation)
                            .Accepted,
                        Is.True);
                    continue;
                }

                break;
            }

            Assert.That(started, Is.True, "Peak Precision never reached its own action.");
            run.ActionCompleted = turn.PendingAttackTargetChoice == null &&
                turn.PendingDefenseChoice == null && turn.PendingEffectChoice == null;
            run.SelectedActorCleared = !turn.SelectedActor.CardInstanceId.Equals(played.Id);
            run.SourceCardLocation = played.State.Location;
            run.SourceCardHasBeenPlayed = played.State.HasBeenPlayed;
            return run;
        }

        // -------------------------------------------------------------------------------------------
        // Shared driving
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// One step of the turn's own opening: a tie, a release, or holding a card that is not the one
        /// under test. Reports whether the acting card is now the selected actor.
        /// </summary>
        private static bool Advance(
            TurnResolutionAuthority turn, CardInstance played, out bool reachedTheCard)
        {
            reachedTheCard = false;
            if (turn.PendingChoice != null)
            {
                var tie = turn.PendingChoice;
                Assert.That(turn.TryChooseTiedActor(
                        tie.OwningSide,
                        tie.CandidateCardInstanceIds.Contains(played.Id)
                            ? played.Id
                            : tie.CandidateCardInstanceIds[0]).Accepted,
                    Is.True);
                return true;
            }

            if (turn.PendingTurnRelease != null)
            {
                Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                return true;
            }

            if (!turn.SelectedActor.CardInstanceId.IsDefined)
            {
                return false;
            }

            if (!turn.SelectedActor.CardInstanceId.Equals(played.Id))
            {
                Assert.That(turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                return true;
            }

            reachedTheCard = true;
            return true;
        }

        /// <summary>
        /// The defender answers with a card, so the attack is survived: a Defeat would price Levels and
        /// coins against a round authority this composition has none of, and what is under test is the
        /// clause rather than p.13's consequences.
        /// </summary>
        private static bool AnsweredADefense(TurnResolutionAuthority turn)
        {
            if (turn.PendingDefenseChoice == null) return false;
            var defense = turn.PendingDefenseChoice;
            var wall = defense.LegalDefenseOffers.FirstOrDefault(offer =>
                offer.SourceDefinitionReference.Equals(DefenceWall.Reference));
            Assert.That(turn.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId,
                    defense.ChoiceId,
                    wall.IsAvailable
                        ? wall.SourceCardInstanceId
                        : defense.LegalDefenseOffers[0].SourceCardInstanceId)).Accepted, Is.True);
            return true;
        }

        /// <summary>
        /// A probe card in each enemy hero's hand whose only action is a Defense of 9.
        ///
        /// <para>It exists so that rulebook p.14's step 6 never removes anybody: a Defeat prices Levels
        /// and coins against a round authority this composition has none of, and what is under test is
        /// the clause rather than p.13's consequences. Peak Precision's own raised total is 7, so
        /// nothing smaller would survive it - Silverarrow's chain fixture gets away with the staged
        /// hand because Rain of Arrows prints 3.</para>
        /// </summary>
        private static readonly CardDefinition DefenceWall = new CardDefinition(
            new CardDefinitionId("brynn-split-defence-wall"),
            "rev-1",
            "TEST - Brynn split defence wall",
            new InitiativeValue(20),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"),
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static IEnumerable<CardInstance> DefenceWalls() =>
            new[] { BoxedIn, InTheOpen }.Select(hero => CardInstance.CreateInHand(
                new CardInstanceId("brynn-split-wall-" + hero.Value),
                DefenceWall.Reference,
                hero));

        private static bool AnsweredAnEffectChoice(TurnResolutionAuthority turn)
        {
            if (turn.PendingEffectChoice == null) return false;
            var effect = turn.PendingEffectChoice;
            Assert.That(turn.TryAnswerEffectChoice(
                effect.SelectInvocations.Count > 0
                    ? effect.SelectInvocations[0]
                    : effect.ConfirmInvocation).Accepted, Is.True);
            return true;
        }

        private static void CommitEveryHand(MatchConfiguration configuration, CardInstance played)
        {
            foreach (var committed in new[]
            {
                played,
                configuration.CardInstances.First(value =>
                    value.OwnerId.Equals(BoxedIn) && value.State.Location == CardLocation.Hand),
                configuration.CardInstances.First(value =>
                    value.OwnerId.Equals(InTheOpen) && value.State.Location == CardLocation.Hand)
            })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }
        }

        private static string Describe(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? "hero:" + target.HeroId.Value
                : "minion:" + target.MinionId.Value;

        // -------------------------------------------------------------------------------------------
        // The gate, through its handler
        // -------------------------------------------------------------------------------------------

        private static bool Gate(
            AdjacentMoveAuthority board,
            EnclosingAttackFacts attack,
            RuleModificationSet modifications = null,
            MatchParticipants participants = null)
        {
            var result = RunGate(board, attack, modifications, participants);
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied),
                "The predicate can be asked inside an Attack on a board, so it answers.");
            return result.Outcomes
                .Single(fact => fact.OutputName == BrynnComponentContent.PresentOutputName)
                .Value.BooleanValue;
        }

        private static EffectExecutionResult RunGate(
            AdjacentMoveAuthority board,
            EnclosingAttackFacts attack,
            RuleModificationSet modifications = null,
            MatchParticipants participants = null)
        {
            var handler = BrynnComponentContent.SplitAttackRegisteredHandlers().Single();
            var definition = Face(BrynnCardContent.SplitAttack);
            var card = CardInstance.CreateInHand(
                new CardInstanceId("brynn-gate-probe"), definition.Reference, Brynn);
            var live = participants ?? ThreeHeroes();

            return handler.Execute(new EffectCapabilityContext(
                new EffectNodeId("brynn-gate-probe-node"),
                EffectFailurePolicy.Mandatory,
                Brynn,
                Brynn,
                card.Id,
                card.DefinitionReference,
                Attack(definition).Id,
                new Dictionary<string, EffectValue>(),
                1,
                1,
                live,
                null,
                modifications ?? RuleModificationSet.Empty,
                EffectCardState.Of(new[] { definition }, new[] { card }),
                ActionKind.Attack,
                new MatchComponents(
                    SyntheticComponentDefinitions.CreateCatalog(),
                    board,
                    new MarkerAttachmentRegister(),
                    board,
                    board,
                    board,
                    board),
                new HeroItemState(live),
                null,
                attack));
        }

        private static EnclosingAttackFacts AttackOn(HeroId target, HexCoordinate boundAt) =>
            new EnclosingAttackFacts(
                Brynn, target, default, AttackRangeKind.Ranged, boundAt);

        /// <summary>
        /// The live set while one hero performs an action, derived exactly as milestone 8.69a derives
        /// it - from his unlocked Ultimate, the board and the participants - so nothing here states the
        /// overlay a second way.
        /// </summary>
        private static RuleModificationSet WhileActing(
            HeroId acting, AdjacentMoveAuthority board, MatchParticipants participants) =>
            RuleModificationQuery.Build(
                null,
                board,
                participants,
                TieBreakerState.Unrevealed,
                UltimateUnlockQuery.Derive(
                    new[] { Face(BrynnCardContent.OverTheTop) },
                    new[]
                    {
                        new CardInstance(
                            new CardInstanceId("brynn-split-over-the-top"),
                            Face(BrynnCardContent.OverTheTop).Reference,
                            Brynn,
                            new CardStateFacts(
                                CardLocation.UltimateArea, false, CardResolutionStatus.None,
                                CardFaceOrientation.FaceUp))
                    }),
                RuleObject.ForHero(acting),
                null,
                null);

        // -------------------------------------------------------------------------------------------
        // Boards
        // -------------------------------------------------------------------------------------------

        private static HeroOccupancy At(HeroId heroId, HexCoordinate coordinate) =>
            new HeroOccupancy(heroId, coordinate);

        /// <summary>
        /// Three of a space's six neighbours, chosen away from the origin so that boxing one hero in
        /// never changes what any other space reaches.
        /// </summary>
        private static HexCoordinate[] BoxSpaces(HexCoordinate around) =>
            around.GetNeighbors()
                .Where(space => HexDistanceQuery.Calculate(Origin, space) >
                    HexDistanceQuery.Calculate(Origin, around))
                .Take(3)
                .ToArray();

        private static AdjacentMoveAuthority Board(
            IEnumerable<HeroOccupancy> heroes,
            IEnumerable<HexCoordinate> terrain = null,
            IEnumerable<QuickGameMinionSetupState> minions = null,
            int radius = 5)
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

        private static QuickGameMinionSetupState MinionAt(HexCoordinate coordinate) =>
            new QuickGameMinionSetupState(
                Minion,
                new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                TeamSide.Blue,
                StandardMinionType.Melee,
                new ConfiguredSpawnPointReference(Map, new SpawnPointId("brynn-split-spawn")),
                new ConfiguredSpaceReference(
                    Map, new SpaceId($"q{coordinate.Q}_r{coordinate.R}")),
                coordinate);

        private static MatchParticipants ThreeHeroes() =>
            StagedBoardParticipants.CreateWithBlueAlly();

        // -------------------------------------------------------------------------------------------
        // Reading the composed faces
        // -------------------------------------------------------------------------------------------

        private static CardDefinition Face(CardDefinitionId id) =>
            BrynnCardContent.AllDefinitions.Single(definition => definition.Id.Equals(id));

        private static ActionDefinition Attack(CardDefinition definition) =>
            definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);

        private static EffectNodeDefinition RepeatNode(CardDefinitionId id)
        {
            var root = Attack(Face(id)).EffectRoot;
            Assert.That(root, Is.Not.Null,
                "The face composes its clause at this milestone; a null body means it has been " +
                "re-parked and every case here is about a card that no longer exists.");
            return root.Nodes.Single(node => node.Kind == EffectNodeKind.RepeatAction);
        }

        private static int PrintedValue(
            CardDefinition definition, ActionKind kind, PrintedValueChannel channel) =>
            definition.ActionDefinitions
                .Single(action => action.Kind == kind)
                .BaseValues
                .Single(value => value.Channel == channel)
                .Amount;
    }
}
