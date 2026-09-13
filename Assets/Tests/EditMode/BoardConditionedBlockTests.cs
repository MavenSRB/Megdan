using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.40g. Rulebook p.14's Block resolved by a fact about the BOARD - Bain the Bounty
    /// Hunter's Close Call, Narrow Escape and Perfect Getaway, "If a hero in play has a [bounty] bounty
    /// marker, block the attack" - proved as vocabulary before any of his content exists.
    ///
    /// <para><b>THE CONDITION TYPE WAS THE WRONG SHAPE AND SO WAS THE QUERY THAT ASKED IT.</b>
    /// <see cref="PrintedBlockCondition"/> carried two members, both read off the attacking card's own
    /// p.13 classification, and <c>DefenseBlockQuery.Evaluate</c> was handed the defence offer, the
    /// definitions and the attack's reference - no board, no components, no participants. A board fact
    /// could not be answered there at all. The widening is one required parameter: the marker register,
    /// which the Attack backend has held since milestone 6.12 and which the projection the preview is
    /// built from already publishes, so neither call site needed a new collaborator to find one.</para>
    ///
    /// <para><b>A THIRD MEMBER RATHER THAN A SECOND AXIS, AND THE CHOICE WAS MEASURED.</b> His three
    /// Blocks print no ranged clause, so the condition is unconditional in the attack and conditional in
    /// the board. A second axis has only one place to live - a new public property on
    /// <see cref="ActionDefinition"/> - and milestone 8.38d measured what that costs: the first change
    /// ever to move all twelve corpus digests at once. A member on an enum an existing property already
    /// carries costs nothing, because no card written before this milestone names it, and the cross
    /// product it would buy is a sentence no printed face asks for. The pairing guard is untouched in
    /// both directions, which the first cases below hold.</para>
    ///
    /// <para><b>THE PREVIEW IS NOT OPTIONAL AND MILESTONE 8.11a IS WHY.</b> The query has exactly two
    /// callers - the Attack resolver and the defence preview a defender chooses from - and that file's
    /// own remarks say a preview that disagreed with the resolution "would be worse than no preview at
    /// all: it is the thing a player decides from". So the agreement is asserted by driving both of them
    /// through a live match rather than by observing that they call one function, and the register they
    /// read is asserted to be one object rather than two copies.</para>
    ///
    /// <para><b>WHERE THE CONSEQUENCE RUNS, AND WHAT MAKES IT CONDITIONAL.</b> p.14 step 4 is "Defender
    /// applies Defense action text, if any", and it already runs for a Block: Tigerclaw's Sidestep,
    /// Parry, Evade and Riposte all carry one, and every one of them runs whether or not their condition
    /// was fulfilled, because the page does not make step 4 conditional on step 6. That behaviour is
    /// held byte-identical here. What makes Close Call's "block the attack <b>and</b> that hero gives the
    /// marker to you" conditional is therefore the card's own composition - milestone 8.40c's predicate
    /// standing first, the branch reading its flag - which is the shape the consequence is forced into
    /// anyway, because the transfer needs the hero the predicate published.</para>
    ///
    /// <para>No Bain card content here. The live cases carry a probe Block built in this file; the
    /// composed-consequence cases run his marker capabilities as nodes of one resolution, which is where
    /// milestones 8.40b and 8.40c left them. Milestone 8.40i composes his faces.</para>
    /// </summary>
    [TestFixture]
    public sealed class BoardConditionedBlockTests
    {
        // -------------------------------------------------------------------------------------------
        // (a) The declaration, and the pairing guard that did not move
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ABlockNamingTheBoardCondition_Constructs_AndCarriesIt()
        {
            var block = ProbeBlockAction(null);

            Assert.That(block.DefenseResolution, Is.EqualTo(PrintedDefenseResolution.Block));
            Assert.That(block.BlockCondition,
                Is.EqualTo(PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker),
                "p.14 resolves a Block by the condition its card prints, and this is a third thing a " +
                "card may print rather than a third way of saying one of the first two.");
        }

        [Test]
        public void ABlockThatNamesNoCondition_IsStillRefused()
        {
            var refusal = Assert.Throws<ArgumentException>(
                () => ProbeBlockAction(null, PrintedBlockCondition.None));

            Assert.That(refusal.ParamName, Is.EqualTo("blockCondition"),
                "Milestone 8.23's requirement is unchanged by a third member: a Block that names no " +
                "condition has no p.14 resolution at all.");
        }

        [Test]
        public void ANumericDefense_NamingTheBoardCondition_IsStillRefused()
        {
            var refusal = Assert.Throws<ArgumentException>(() => new ActionDefinition(
                new ActionDefinitionId("board-conditioned-numeric-defense"),
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedSecondary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                blockCondition: PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker));

            Assert.That(refusal.ParamName, Is.EqualTo("blockCondition"),
                "and the requirement still runs the other way: only a printed Block states a Block " +
                "condition, whichever of the three it is.");
        }

        /// <summary>
        /// The corpus, swept rather than remembered: every Block this build dealt before Bain still
        /// prints one of the two arms about the attacking card, so the third member changed no
        /// declaration.
        ///
        /// <para><b>Milestone 8.40i added the first three faces that print the third member</b>, which
        /// is what 8.40g built it for, so the count moved from eight to eleven and the classification
        /// assertion is asked of the eight that print a classification clause. A twelfth Block arriving
        /// without a milestone knowing about it is still what the count exists to notice.</para>
        /// </summary>
        [Test]
        public void EveryBlockTheBuildAlreadyDeals_StillDeclaresAnAttackClassificationArm()
        {
            var blocks = DealtBlocks().ToArray();

            Assert.That(blocks.Length, Is.EqualTo(11),
                "Wasp's three Green Blocks, Tigerclaw's five Blue ones and Bain's three Green ones " +
                "are the whole of p.14's Block content in this build.");
            foreach (var (cardId, action) in blocks.Where(block =>
                block.Action.BlockCondition != PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker))
            {
                Assert.That(
                    new[]
                    {
                        PrintedBlockCondition.AttackIsRanged,
                        PrintedBlockCondition.AttackIsNotRanged
                    },
                    Has.Member(action.BlockCondition),
                    $"{cardId.Value} prints a classification clause and still declares it.");
            }

            Assert.That(
                blocks.Count(block =>
                    block.Action.BlockCondition ==
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker),
                Is.EqualTo(3),
                "and the three that print no classification clause at all are Bain's - Close Call, " +
                "Narrow Escape and Perfect Getaway, which is the face count BA-08 records.");
        }

        // -------------------------------------------------------------------------------------------
        // (b) The verdict, asked of the one query both call sites use
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// "Regardless of the attack's classification" is the whole point of the third member, so both
        /// of p.13's subtypes are asserted on the same board rather than one of them being assumed.
        /// </summary>
        [Test]
        public void TheBoardCondition_BlocksARangedAndANonRangedAttackAlike()
        {
            var board = new BlockConditionBoard();
            board.GiveBountyTo(board.Attacker, owner: board.Defender);

            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.Ranged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "p.14: the condition is fulfilled, so the attack is defended regardless of its value - " +
                "and this condition never asked what kind of attack it was.");
            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "The same board answers the same way for the other subtype, which neither existing " +
                "member could ever do.");
        }

        [Test]
        public void TheBoardCondition_IsUnfulfilledAgainstBothSubtypesWhenNoMarkerIsOut()
        {
            var board = new BlockConditionBoard();

            Assert.That(board.Markers.Attachments, Is.Empty);
            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.Ranged),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "p.14's other half: it is not fulfilled, and the defending hero is defeated.");
            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled));
        }

        /// <summary>
        /// `BA-D5`: "a hero in play" is any hero on the board, the defender included - which is what
        /// "(The marker's effect is applied to you)" tells the reader of Close Call to expect.
        /// </summary>
        [Test]
        public void TheBoardCondition_CountsAMarkerTheDefendingHeroHimselfWears()
        {
            var board = new BlockConditionBoard();
            board.GiveBountyTo(board.Defender, owner: board.Defender);

            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "A marker Close Call has just pulled onto him still satisfies the clause on a later " +
                "turn of the same round.");
        }

        /// <summary>
        /// Whose marker, asserted rather than assumed. Milestone 8.40c reads "his own" off the
        /// attachment's owner - the hero whose card put the piece out - so a board can carry markers of
        /// two definitions and two owners and this Block still reads exactly one of them.
        /// </summary>
        [Test]
        public void TheBoardCondition_ReadsOnlyWhatTheDefendersOwnContentPutOut()
        {
            var board = new BlockConditionBoard();
            board.GivePoisonTo(board.Attacker, owner: board.Bystander);
            var anotherHerosBounty = board.GiveBountyTo(board.Defender, owner: board.Attacker);

            Assert.That(board.Markers.Attachments, Has.Count.EqualTo(2),
                "Both markers are genuinely standing, so the negative below is not an empty board " +
                "answering it - and one of them is a bounty the defending hero is himself wearing.");
            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "A poison another hero's card put out is not a bounty, and a bounty ANOTHER hero's " +
                "content put out is not the one this face's own sentence is about - a Block that " +
                "stopped an attack for either would be stating a rule no printed face carries.");

            // `BA-D1`: the bounty supply is one, so the copy another hero's content put out is the copy
            // this board has. Returning it is what frees the piece the defender's own card then gives.
            board.Markers.Return(anotherHerosBounty.Id);
            board.GiveBountyTo(board.Attacker, owner: board.Defender);

            Assert.That(board.Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker, AttackRangeKind.NonRanged),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "and on the very same board, with the same one bounty now put out by the defender's " +
                "own content instead, the clause is true - so the two answers above are the ownership " +
                "reading rather than an empty one.");
        }

        /// <summary>
        /// Milestone 8.40c's rule, asked of the third reader of the same clause: an absent register is a
        /// composition that was never given one, not a board with no marker on it, and the two must not
        /// look alike. A caller that could omit it is a caller that could disagree with the other one.
        /// </summary>
        [Test]
        public void TheBoardCondition_WithNoRegisterAtAll_RefusesLoudlyRatherThanAnswering()
        {
            var board = new BlockConditionBoard();

            var refusal = Assert.Throws<InvalidOperationException>(() => board.Verdict(
                PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker,
                AttackRangeKind.Ranged,
                withRegister: false));

            Assert.That(refusal.Message, Does.Contain("marker register"));
            Assert.That(refusal.Message, Does.Contain("8.11a"),
                "The refusal names the disagreement it exists to prevent, because the silent answer " +
                "would have been a defence greyed for a board nobody looked at.");
        }

        [Test]
        public void TheTwoAttackArms_AreAnsweredWithNoRegisterExactlyAsBefore()
        {
            var board = new BlockConditionBoard();

            Assert.That(
                board.Verdict(
                    PrintedBlockCondition.AttackIsRanged, AttackRangeKind.Ranged,
                    withRegister: false),
                Is.EqualTo(DefenseBlockResult.Blocked),
                "Neither arm about the attacking card reads the board, so neither is refused for " +
                "lacking one: the widening is scoped to the condition that needs it.");
            Assert.That(
                board.Verdict(
                    PrintedBlockCondition.AttackIsNotRanged, AttackRangeKind.Ranged,
                    withRegister: false),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled));
        }

        /// <summary>
        /// The acceptance check this milestone owes the eight Blocks already in the build, asserted over
        /// the corpus rather than over a remembered list: every one of them reaches exactly the verdict
        /// its printed classification clause reaches, on three boards - none, a bounty the defender's own
        /// content put out, and a poison - and with no register at all.
        ///
        /// <para><b>Milestone 8.40i composed the three faces that print the OTHER member</b>, and they
        /// are excluded here by their own declaration rather than by name: this case is about the eight
        /// that ask a question about the attacking card, and a Block asking the board is what the cases
        /// above and below it prove. Reading them through this one's arithmetic would assert that a
        /// board-conditioned Block answers a classification question, which is the confusion 8.40g's
        /// second axis was refused to avoid.</para>
        /// </summary>
        [Test]
        public void WaspsThreeBlocksAndTigerclawsFive_AreByteIdenticalOnEveryBoardThisMilestoneBuilds()
        {
            foreach (var (cardId, action) in DealtBlocks().Where(block =>
                block.Action.BlockCondition != PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker))
            {
                foreach (var attackRange in new[] { AttackRangeKind.Ranged, AttackRangeKind.NonRanged })
                {
                    var expected =
                        (action.BlockCondition == PrintedBlockCondition.AttackIsRanged) ==
                        (attackRange == AttackRangeKind.Ranged)
                            ? DefenseBlockResult.Blocked
                            : DefenseBlockResult.ConditionUnfulfilled;

                    var empty = new BlockConditionBoard();
                    var withBounty = new BlockConditionBoard();
                    withBounty.GiveBountyTo(withBounty.Attacker, owner: withBounty.Defender);
                    var withPoison = new BlockConditionBoard();
                    withPoison.GivePoisonTo(withPoison.Defender, owner: withPoison.Defender);

                    foreach (var board in new[] { empty, withBounty, withPoison })
                    {
                        Assert.That(
                            board.Verdict(action.BlockCondition, attackRange),
                            Is.EqualTo(expected),
                            $"{cardId.Value} answers p.13's {attackRange} attack the way its own " +
                            "printed clause always did, and the markers standing on the board are " +
                            "nothing it has ever been able to see.");
                    }

                    Assert.That(
                        empty.Verdict(action.BlockCondition, attackRange, withRegister: false),
                        Is.EqualTo(expected),
                        $"{cardId.Value} is also unchanged where there is no register at all, which " +
                        "is every composition outside a live match.");
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // (c) Both call sites, driven through a live match
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The preview a defender decides from and the resolution that answers them, on a board where the
        /// clause holds. The attack is Magnetic Dagger, which is p.13's non-Ranged subtype: every Block
        /// that stops one today stops it because its own printed clause names that subtype, and this one
        /// stops it without asking what the attack was at all.
        /// </summary>
        [Test]
        public void ThePreviewAndTheResolution_BothBlockWhenTheBoardFactHolds()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: true, out var defender, out var blockCard);
            PutAMarkerTheDefendersContentOwnsOnTheBoard(session, defender);
            DriveToTheDefenceResponse(session, defender);

            Assert.That(PreviewedVerdict(session, blockCard), Is.EqualTo(DefenseBlockResult.Blocked),
                "The defence preview answers the condition from the board the match is standing on.");

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(blockCard));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "and the resolution reaches the same verdict: \"If the Block condition is fulfilled, " +
                "you successfully defend, regardless of the attack value.\"");
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(blockCard)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The card was discarded to perform it, exactly as every other Defense action is.");
        }

        [Test]
        public void ThePreviewAndTheResolution_BothFailWhenTheBoardFactDoesNot()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: true, out var defender, out var blockCard);
            DriveToTheDefenceResponse(session, defender);

            Assert.That(RegisterOf(session).Attachments, Is.Empty,
                "Nothing is wearing anything, which is the only difference from the case above.");
            Assert.That(
                PreviewedVerdict(session, blockCard),
                Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "so the preview predicts the defence failing rather than describing a Block that holds.");

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(blockCard));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.False,
                "p.14: \"If not, you are defeated.\" - the resolution agreed with the preview the " +
                "defender decided from.");
        }

        /// <summary>
        /// Why the two cases above cannot come apart: they are not two derivations that happen to match,
        /// they are one register read twice. The projection the preview is built from and the Attack
        /// backend that resolves the Block hold the same object.
        /// </summary>
        [Test]
        public void ThePreviewAndTheAttackBackend_ReadOneRegisterRatherThanTwoCopies()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: false, out var defender, out _);
            PutAMarkerTheDefendersContentOwnsOnTheBoard(session, defender);

            var round = RegisterOf(session);
            var backend = BackendRegisterOf(session);

            Assert.That(backend, Is.Not.Null,
                "The Attack backend holds a register in a live match - milestone 6.12 binds it so " +
                "p.13's \"any markers they had are returned\" happens where the Defeat is recorded.");
            Assert.That(ReferenceEquals(round, backend), Is.True,
                "and it is the round's own, which is the object the projection publishes to the " +
                "preview. One board, one reading, no way for milestone 8.11a's disagreement to " +
                "reappear through a copy.");
        }

        // -------------------------------------------------------------------------------------------
        // (d) The consequence: where it runs, and what makes it conditional
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// p.14 step 4 - "Defender applies Defense action text, if any" - is where a Block's consequence
        /// runs, between step 3's discard and step 5's total. The probe Block's text puts a marker on the
        /// attacker, which is a consequence only a live frame carrying the component port and the
        /// opposing-hero role can carry out: both are what Close Call and Narrow Escape will need.
        /// </summary>
        [Test]
        public void ABlocksDefenseText_RunsInThePagesStep4WhenTheBlockHolds()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: true, out var defender, out var blockCard);
            PutAMarkerTheDefendersContentOwnsOnTheBoard(session, defender);
            DriveToTheDefenceResponse(session, defender);
            var attacker = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Red);

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(blockCard));

            Assert.That(
                RegisterOf(session).Attachments
                    .Where(attachment => attachment.Definition.Reference.Equals(
                        GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions
                            .FocusV1))
                    .Select(attachment => attachment.HolderHeroId),
                Has.Member(attacker),
                "The Defense text applied inside the defender's own response command, which is where " +
                "p.14 numbers it - and it reached the component port and named the attacker through " +
                "the opposing-hero role, so a marker consequence is carryable in this window.");
            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "and the Block it is printed on still held.");
        }

        /// <summary>
        /// The half of p.14 that decides where the conditionality has to live. Step 4 is not conditional
        /// on step 6: Tigerclaw's four texted Blocks apply their text whether or not their condition was
        /// fulfilled, and this build has behaved that way since milestone 8.23. So a consequence that
        /// must run only when the Block holds is the CARD's composition and not a change to this window,
        /// which is what the two cases after this one prove.
        /// </summary>
        [Test]
        public void ABlocksDefenseText_StillRunsWhenTheConditionFails()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: true, out var defender, out var blockCard);
            DriveToTheDefenceResponse(session, defender);
            var attacker = ConfiguredDefenseCardTests.HeroOn(session, TeamSide.Red);

            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(blockCard));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.False,
                "The condition was unfulfilled and the defender was defeated at step 6.");
            Assert.That(
                RegisterOf(session).Attachments
                    .Where(attachment => attachment.Definition.Reference.Equals(
                        GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions
                            .FocusV1))
                    .Select(attachment => attachment.HolderHeroId),
                Has.Member(attacker),
                "and step 4 still ran, unchanged: the page applies the Defense card's text where it " +
                "numbers it and asks nothing about the verdict. A Block printing 'block the attack AND " +
                "...' therefore gates its own consequence, which is the shape milestone 8.40c proved.");
        }

        /// <summary>
        /// The shape a conditional consequence takes is shipped content's rather than this milestone's
        /// invention, and it is asserted rather than remembered: Wasp's Deflect and Reflect print "Block
        /// a ranged attack; if you do, an enemy hero in range discards a card, if able", and milestone
        /// 8.11 composed "if you do" as the Block condition READ AGAIN - a Conditional over
        /// <c>attack-is-ranged@1</c> wrapping the whole rider.
        ///
        /// <para>Bain's two marker faces differ in one node and nothing else: their condition is about
        /// the board, so the predicate at its root is milestone 8.40c's rather than the attack's own
        /// subtype. That is why this milestone changes nothing about p.14's step 4 - the page applies
        /// the defender's text there whatever the verdict, and the card that must not act on a failed
        /// condition says so itself.</para>
        /// </summary>
        [Test]
        public void TheGateOnAConsequence_IsTheShapeWaspsTwoShippedRidersAlreadyUse()
        {
            foreach (var cardId in new[]
                     {
                         WaspCardContent.DeflectProjectiles,
                         WaspCardContent.ReflectProjectiles
                     })
            {
                var block = WaspCardContent.Definition(cardId).ActionDefinitions
                    .Single(action => action.DefenseResolution == PrintedDefenseResolution.Block);

                Assert.That(block.CardTextTiming, Is.EqualTo(CardTextTiming.DefenseAction),
                    $"{cardId.Value} applies its rider at p.14 step 4, which is the only moment a " +
                    "Defense action has.");
                var root = block.EffectRoot.Root;
                Assert.That(root.Kind, Is.EqualTo(EffectNodeKind.Conditional),
                    "and \"if you do\" is a branch rather than an unconditional sentence.");
                Assert.That(
                    root.Capability, Is.EqualTo(SyntheticConsequenceCapabilities.AttackIsRangedV1),
                    "whose predicate is this card's own Block condition asked again - which is what a " +
                    "board-conditioned face does with 8.40c's predicate in its place.");
            }
        }

        /// <summary>
        /// Close Call's shape, composed and run: the predicate stands first, the branch reads its flag,
        /// and the transfer is reached only on the arm the same board fact opened. The gate is not
        /// decoration - the consequence needs the hero the predicate published, so the composition could
        /// not have been written ungated even if the timing had allowed it.
        /// </summary>
        [Test]
        public void TheTransferShapedConsequence_RunsExactlyWhenTheSameBoardFactBlocksTheAttack()
        {
            var held = new BlockConditionBoard();
            held.GiveBountyTo(held.Attacker, owner: held.Defender);

            var run = held.RunTheGatedConsequence(BainComponentContent.TransferBountyV1);

            Assert.That(run.IsComplete, Is.True);
            Assert.That(
                run.Events.Select(recorded => recorded.ResultKind),
                Has.No.Member(EffectExecutionResultKind.ImpossibleMandatory));
            var consequence = run.Events.Single(recorded =>
                recorded.NodeId.Equals(BlockConditionBoard.ConsequenceNodeId));
            Assert.That(consequence.ResultKind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(
                held.Markers.Attachments.Single().HolderHeroId, Is.EqualTo(held.Defender),
                "\"that hero gives the marker to you\" - one attachment, moved rather than minted.");
            var predicate = run.Events.Single(recorded =>
                recorded.NodeId.Equals(BlockConditionBoard.PredicateNodeId));
            Assert.That(
                consequence.Outputs.Single(fact => fact.OutputName ==
                    SyntheticComponentCapabilities.MarkerTransferHandler.PreviousHolderOutputName)
                    .Value.HeroId,
                Is.EqualTo(
                    predicate.Outputs.Single(fact => fact.OutputName ==
                        SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName)
                        .Value.HeroId),
                "and the hero it moved FROM is the hero the predicate published, asserted against that " +
                "published fact rather than against a hero this fixture happens to know: the " +
                "sentence's \"that hero\" and the Block's own condition are one reading of one board.");
            Assert.That(
                held.VerdictOnThisBoard(), Is.EqualTo(DefenseBlockResult.Blocked),
                "which is the same board the Block was resolved on.");
        }

        [Test]
        public void TheTransferShapedConsequence_RunsNothingWhenTheClauseIsFalse()
        {
            var empty = new BlockConditionBoard();

            var run = empty.RunTheGatedConsequence(BainComponentContent.TransferBountyV1);

            Assert.That(run.IsComplete, Is.True);
            Assert.That(
                run.Events.Select(recorded => recorded.NodeId),
                Has.No.Member(BlockConditionBoard.ConsequenceNodeId),
                "The consequence was not reached at all - not reached and reported impossible, which " +
                "is what an ungated mandatory transfer on an empty board would have been.");
            Assert.That(empty.Markers.Attachments, Is.Empty);
            Assert.That(
                empty.VerdictOnThisBoard(), Is.EqualTo(DefenseBlockResult.ConditionUnfulfilled),
                "and the Block printed on the same card failed on the same board, which is the " +
                "agreement this milestone owes: the consequence runs when and only when the Block holds.");
        }

        /// <summary>
        /// Narrow Escape's shape, which differs from Close Call's in one node: "retrieve the marker" is
        /// milestone 8.40b's definition-bound return, and the gate is what keeps a mandatory return off
        /// an empty board.
        /// </summary>
        [Test]
        public void TheReturnShapedConsequence_RetrievesTheMarkerOnlyOnTheArmTheClauseOpens()
        {
            var held = new BlockConditionBoard();
            held.GiveBountyTo(held.Attacker, owner: held.Defender);

            var run = held.RunTheGatedConsequence(BainComponentContent.ReturnBountyV1);

            Assert.That(run.IsComplete, Is.True);
            Assert.That(
                run.Events.Single(recorded =>
                        recorded.NodeId.Equals(BlockConditionBoard.ConsequenceNodeId))
                    .ResultKind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(held.Markers.Attachments, Is.Empty,
                "\"retrieve the marker\" is the rulebook's word for returning one to the supply.");

            var empty = new BlockConditionBoard();
            var emptyRun = empty.RunTheGatedConsequence(BainComponentContent.ReturnBountyV1);

            Assert.That(emptyRun.IsComplete, Is.True);
            Assert.That(
                emptyRun.Events.Select(recorded => recorded.NodeId),
                Has.No.Member(BlockConditionBoard.ConsequenceNodeId),
                "and on the board where the Block fails the return is never asked, so the text ends " +
                "having done nothing rather than reporting a step it could not take.");
        }

        /// <summary>
        /// Perfect Getaway is the control and the reason for having three faces: it prints the condition
        /// and no consequence at all, so a Block carrying no card text whatever still resolves by the
        /// board - which is what says the condition is the Block's resolution rather than a rider on it.
        /// </summary>
        [Test]
        public void TheConditionAlone_ResolvesABlockCarryingNoCardTextAtAll()
        {
            var session = CreateBoardConditionedBlockSession(
                carriesDefenseText: false, out var defender, out var blockCard);
            PutAMarkerTheDefendersContentOwnsOnTheBoard(session, defender);
            DriveToTheDefenceResponse(session, defender);

            Assert.That(PreviewedVerdict(session, blockCard), Is.EqualTo(DefenseBlockResult.Blocked));

            var markersBefore = RegisterOf(session).Attachments.Count;
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.ChooseDefenseCard(blockCard));

            Assert.That(
                session.Projection.Heroes.Any(occupancy => occupancy.HeroId.Equals(defender)),
                Is.True,
                "The condition alone defended the attack.");
            Assert.That(RegisterOf(session).Attachments, Has.Count.EqualTo(markersBefore),
                "and nothing else happened, because the face prints nothing else.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers: the corpus sweep
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Every printed Block in every verified content class this build deals, found by reflecting the
        /// assembly the way the corpus guards do rather than by naming two heroes - so a ninth Block
        /// arriving with a later hero is swept by this milestone's cases without being added to a list.
        /// </summary>
        private static IEnumerable<(CardDefinitionId CardId, ActionDefinition Action)> DealtBlocks()
        {
            var contentTypes = typeof(WaspCardContent).Assembly.GetTypes()
                .Where(type =>
                    type.IsClass && type.IsAbstract && type.IsSealed &&
                    type.Namespace == typeof(WaspCardContent).Namespace &&
                    type.Name.EndsWith("CardContent", StringComparison.Ordinal))
                .OrderBy(type => type.Name, StringComparer.Ordinal);

            foreach (var type in contentTypes)
            {
                var property = type.GetProperty(
                    "AllDefinitions", BindingFlags.Public | BindingFlags.Static);
                var field = property == null
                    ? type.GetField("AllDefinitions", BindingFlags.Public | BindingFlags.Static)
                    : null;
                var cards = (IEnumerable<CardDefinition>)(property != null
                    ? property.GetValue(null)
                    : field?.GetValue(null));
                Assert.That(cards, Is.Not.Null,
                    $"{type.Name} exposes AllDefinitions, or this sweep cannot see its faces.");

                foreach (var card in cards)
                {
                    foreach (var action in card.ActionDefinitions
                        .Where(action => action.DefenseResolution == PrintedDefenseResolution.Block))
                    {
                        yield return (card.Reference.DefinitionId, action);
                    }
                }
            }
        }

        // -------------------------------------------------------------------------------------------
        // Helpers: the probe Block
        // -------------------------------------------------------------------------------------------

        private static readonly CardDefinitionId ProbeBlockCardId =
            new CardDefinitionId("board-conditioned-block");

        private static readonly ActionDefinitionId ProbeBlockActionId =
            new ActionDefinitionId("board-conditioned-block-primary");

        private static readonly CardInstanceId ProbeBlockInstanceId =
            new CardInstanceId("board-conditioned-block-card");

        /// <summary>
        /// One Block printing the board condition, with or without the consequence the two marker faces
        /// print. The consequence is a marker operation on the attacker rather than an invented one,
        /// because what it has to prove is that p.14's step 4 frame carries the component port and names
        /// the hero on the other side of the page - the two things Close Call and Narrow Escape need.
        /// </summary>
        private static ActionDefinition ProbeBlockAction(
            EffectDefinition defenseText,
            PrintedBlockCondition condition =
                PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker) =>
            new ActionDefinition(
                ProbeBlockActionId,
                ActionKind.Defense,
                ActionDefinitionOrigin.PrintedPrimary,
                Array.Empty<PrintedValueDefinition>(),
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile),
                defenseText,
                PrintedTextComposition.None,
                defenseResolution: PrintedDefenseResolution.Block,
                cardTextTiming: defenseText == null
                    ? CardTextTiming.None
                    : CardTextTiming.DefenseAction,
                blockCondition: condition);

        /// <summary>
        /// "The attacker is given a marker" - one node, mandatory, naming its subject through
        /// <see cref="EffectRoleKind.OpposingHero"/>, which is the role milestone 8.23 bound at p.14's
        /// step 4 for Tigerclaw's Parry. A registered give is used rather than Bain's deferred one,
        /// because a live match runs the registered catalog alone.
        ///
        /// <para><b>The marker it gives modifies no printed value, and that is a measurement rather
        /// than a preference.</b> The first shape of this probe gave Tigerclaw's Poison - which is
        /// -1 Initiative, -1 Attack and -1 Defense to whoever wears it - to the attacking hero, and
        /// p.14's outcome re-validation then refused the whole resolution: <c>ResolveOutcome</c>
        /// re-derives the Attack value it prevalidated at step 2 and compares it, so a Defense text
        /// that lowers the ATTACKER's Attack between step 4 and step 6 makes the page unable to resume
        /// its own comparison. Nothing in the build prints such a sentence and Bain's two marker faces
        /// cannot: the bounty carries one property, p.13's additional Life cost, and no printed value
        /// channel at all.</para>
        /// </summary>
        private static EffectDefinition ProbeDefenseText()
        {
            var root = new EffectNodeId("board-conditioned-block-text");
            return new EffectDefinition(
                new EffectDefinitionId("board-conditioned-block-text"),
                "board-conditioned-block-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticComponentCapabilities.GiveMarkerV1,
                        inputs: new[]
                        {
                            EffectInputBinding.FromRole(
                                SyntheticComponentCapabilities.MarkerGivingHandler
                                    .HolderParameterName,
                                EffectRoleKind.OpposingHero,
                                EffectValueKind.Hero)
                        })
                });
        }

        // -------------------------------------------------------------------------------------------
        // Helpers: the live match
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The engaged verified mirror with the probe Block dealt into the defending hero's hand in
        /// place of the Green card p.6 deals there, so the hand keeps its five and the only Block on
        /// offer is this milestone's.
        ///
        /// <para>The definitions and instances are handed to the scenario directly, which is the seam
        /// <c>ConfiguredDefenseCardTests</c> already composes a fixture Defense card through; the
        /// restore path a session rebuilds its configuration from routes cards by ownership and by the
        /// content's own upgrade relationships, so a probe card lands in the hand its instance says it
        /// is in.</para>
        /// </summary>
        private static MatchSession CreateBoardConditionedBlockSession(
            bool carriesDefenseText, out HeroId defender, out CardInstanceId blockCard)
        {
            var source = MatchConfiguration.CreateVerifiedMirrorForInternalTest(TeamSide.Red);
            var state = source.CaptureRestoreState();
            var blueHero = state.Participants.Heroes
                .Single(hero => state.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var probe = new CardDefinition(
                ProbeBlockCardId,
                "board-conditioned-block-rev-1",
                "TEST - Board Conditioned Block",
                new InitiativeValue(5),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[] { ProbeBlockAction(carriesDefenseText ? ProbeDefenseText() : null) });

            var configuration = MatchConfiguration.Restore(
                new MatchConfigurationRestoreState(
                    state.Participants,
                    state.Definitions.Concat(new[] { probe }),
                    state.Instances
                        .Select(instance =>
                            instance.OwnerId.Equals(blueHero) &&
                            instance.DefinitionReference.DefinitionId.Equals(
                                WaspCardContent.StopProjectiles)
                                ? CardInstance.CreateInDeck(
                                    instance.Id, instance.DefinitionReference, instance.OwnerId)
                                : instance)
                        .Concat(new[]
                        {
                            CardInstance.CreateInHand(
                                ProbeBlockInstanceId, probe.Reference, blueHero)
                        }),
                    state.TieBreakerState,
                    state.SetupSeed));

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var engaged = new QuickGameSetupState(
                setup.SelectedMap,
                setup.WaveCount,
                setup.TieBreakerState,
                setup.Teams,
                WithBlueHeroBesideTheRedOne(map, setup),
                setup.Minions);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                engaged.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());

            var session = new MatchSession(new MatchSessionInitialState(
                scenario, engaged, new[] { map }));

            defender = blueHero;
            blockCard = ProbeBlockInstanceId;
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(ProbeBlockInstanceId))
                    .State.Location,
                Is.EqualTo(CardLocation.Hand),
                "The probe Block has to be in the defender's hand, or p.14's step 3 never offers it.");
            return session;
        }

        /// <summary>The Blue hero standing beside the Red one, so a non-Ranged Attack has a target.</summary>
        private static IReadOnlyList<QuickGameHeroSetupState> WithBlueHeroBesideTheRedOne(
            BoardMapDefinition map, QuickGameSetupState setup)
        {
            var redHero = setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red);
            var occupied = new HashSet<HexCoordinate>(
                setup.Heroes.Select(hero => hero.Coordinate)
                    .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var beside = redHero.Coordinate.GetNeighbors()
                .First(coordinate => !occupied.Contains(coordinate) &&
                    map.Spaces.Any(space =>
                        space.Kind == BoardMapSpaceKind.Playable &&
                        space.Coordinate.Equals(coordinate)));
            var besideSpaceId = map.Spaces.Single(space =>
                space.Kind == BoardMapSpaceKind.Playable &&
                space.Coordinate.Equals(beside)).Id;

            return setup.Heroes
                .Select(hero => hero.TeamSide == TeamSide.Blue
                    ? new QuickGameHeroSetupState(
                        hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                        hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, besideSpaceId), beside)
                    : hero)
                .ToArray();
        }

        /// <summary>
        /// Magnetic Dagger's non-Ranged Attack on the defending hero, stopped at p.14's step 3 with the
        /// defender's own hand offered. The drive is the shipped fixture's, because what is under test
        /// is the condition rather than the page.
        /// </summary>
        private static void DriveToTheDefenceResponse(MatchSession session, HeroId defender)
        {
            ConfiguredDefenseCardTests.CommitRedAgainst(
                session, WaspCardContent.MagneticDagger, WaspCardContent.Shock);
            ConfiguredDefenseCardTests.AttackTheHero(session, defender);
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "p.14 step 3 offers the defender their hand.");
            Assert.That(
                session.Projection.PendingDefenseChoice.LegalDefenseOffers
                    .Select(offer => offer.SourceCardInstanceId),
                Has.Member(ProbeBlockInstanceId),
                "and the probe Block is one of the offers, or nothing below is reachable.");
        }

        /// <summary>The verdict the defender is shown for one offer, read off the published preview.</summary>
        private static DefenseBlockResult PreviewedVerdict(
            MatchSession session, CardInstanceId blockCard)
        {
            var preview = session.Projection.PendingDefensePreview;
            Assert.That(preview, Is.Not.Null,
                "The prompt is drawn from the preview, so no preview is no prompt.");
            var choice = preview.Choices.Single(candidate =>
                candidate.CardInstanceId.HasValue &&
                candidate.CardInstanceId.Value.Equals(blockCard));
            Assert.That(choice.DefenseTotal, Is.Null,
                "A Block prints no Defense value, so it previews none.");
            return choice.BlockResult;
        }

        /// <summary>
        /// A marker the DEFENDER's own content put out, worn by the defending hero himself - `BA-D5`'s
        /// reading, and the one shape a two-hero board can carry without the marker's own properties
        /// reaching the attacker mid-page.
        ///
        /// <para>It is attached to the round's own register rather than given by a card, because no card
        /// in this build's registered content gives one to anybody here - Bain's give is deferred until
        /// he is dealt - and what is under test is the Block that READS the board rather than the
        /// operation that changed it. Tigerclaw's Poison is the definition because it is registered, so
        /// the snapshot codec can load it back the way a live match's marker always can.</para>
        /// </summary>
        private static void PutAMarkerTheDefendersContentOwnsOnTheBoard(
            MatchSession session, HeroId defender)
        {
            RegisterOf(session).Give(new MarkerAttachment(
                new MarkerInstanceId("board-conditioned-block-marker"),
                TigerclawComponentContent.PoisonOneDefinition,
                defender,
                defender,
                new CardInstanceId("board-conditioned-block-marker-source"),
                ProbeBlockActionId));
        }

        /// <summary>
        /// The round's marker register, reached by reflection because the aggregate and the round are
        /// internal to their assemblies and no InternalsVisibleTo reaches this one - the same way
        /// <c>BainMarkerDefeatCostTests</c> reaches milestone 8.22's effect-invoked Defeat.
        /// </summary>
        private static MarkerAttachmentRegister RegisterOf(MatchSession session)
        {
            var round = RoundOf(session);
            var markers = round.GetType()
                .GetProperty("Markers", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(round);
            Assert.That(markers, Is.Not.Null,
                "The round authority still publishes the register p.8's cleanup returns markers to.");
            return (MarkerAttachmentRegister)markers;
        }

        /// <summary>The register the Attack/Defense backend answers the Block's condition from.</summary>
        private static MarkerAttachmentRegister BackendRegisterOf(MatchSession session)
        {
            var round = RoundOf(session);
            var cardActions = round.GetType()
                .GetProperty(
                    "CardActions",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(round);
            Assert.That(cardActions, Is.Not.Null,
                "The round still holds one card-action authority.");
            var backend = cardActions.GetType()
                .GetField("attackDefenseBackend", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(cardActions);
            Assert.That(backend, Is.Not.Null,
                "and that authority still holds one Attack/Defense backend.");
            return (MarkerAttachmentRegister)backend.GetType()
                .GetField("markers", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(backend);
        }

        private static object RoundOf(MatchSession session)
        {
            var aggregate = typeof(MatchSession)
                .GetField("aggregate", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(session);
            Assert.That(aggregate, Is.Not.Null, "A session still holds one aggregate.");
            var round = aggregate.GetType()
                .GetProperty(
                    "Round", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(aggregate);
            Assert.That(round, Is.Not.Null, "and that aggregate still holds one round authority.");
            return round;
        }

        // -------------------------------------------------------------------------------------------
        // Fixture: one featureless board, one register, three heroes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The board the query cases are asked on: a featureless radius-3 disk carrying the defender, the
        /// attacker and one bystander, and one live marker register - <c>BainMarkerPredicateTests</c>'
        /// board, so the four readers of "a hero in play has a bounty marker" are proved on the same
        /// shape.
        /// </summary>
        private sealed class BlockConditionBoard
        {
            internal static readonly EffectNodeId ConsequenceNodeId =
                new EffectNodeId("board-conditioned-consequence");

            private static readonly EffectNodeId RootNodeId =
                new EffectNodeId("board-conditioned-root");

            internal static readonly EffectNodeId PredicateNodeId =
                new EffectNodeId("board-conditioned-predicate");

            private static readonly EffectNodeId BranchNodeId =
                new EffectNodeId("board-conditioned-branch");

            private static readonly CardDefinitionId AttackCardId =
                new CardDefinitionId("board-conditioned-attacker");

            private static readonly ActionDefinitionId AttackActionId =
                new ActionDefinitionId("board-conditioned-attack");

            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private int minted;

            public BlockConditionBoard()
            {
                participants = StagedBoardParticipants.CreateWithBlueAlly();
                Attacker = new HeroId(StagedBoardParticipants.RedHeroIdValue);
                Defender = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
                Bystander = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
                board = new AdjacentMoveAuthority(
                    FeaturelessDisk(3),
                    new[]
                    {
                        new HeroOccupancy(Attacker, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Defender, new HexCoordinate(1, 0)),
                        new HeroOccupancy(Bystander, new HexCoordinate(0, 1))
                    });
            }

            public HeroId Attacker { get; }
            public HeroId Defender { get; }
            public HeroId Bystander { get; }
            public MarkerAttachmentRegister Markers { get; } = new MarkerAttachmentRegister();

            public MarkerAttachment GiveBountyTo(HeroId holder, HeroId owner) =>
                Attach(BainComponentContent.BountyDefinition, holder, owner);

            public MarkerAttachment GivePoisonTo(HeroId holder, HeroId owner) =>
                Attach(TigerclawComponentContent.PoisonOneDefinition, holder, owner);

            /// <summary>
            /// The verdict for answering an attack of the named classification with a Block printing the
            /// named condition, asked through the one query the resolver and the preview both use.
            /// </summary>
            public DefenseBlockResult Verdict(
                PrintedBlockCondition condition,
                AttackRangeKind attackRange,
                bool withRegister = true)
            {
                var definitions = new[]
                {
                    AttackCard(attackRange),
                    BlockCard(condition)
                };

                return DefenseBlockQuery.Evaluate(
                    BlockOffer(definitions[1]),
                    definitions,
                    definitions[0].Reference,
                    AttackActionId,
                    withRegister ? Markers : null);
            }

            /// <summary>The board condition asked about this board, against a non-Ranged attack.</summary>
            public DefenseBlockResult VerdictOnThisBoard() =>
                Verdict(
                    PrintedBlockCondition.AHeroInPlayHoldsTheActorsMarker,
                    AttackRangeKind.NonRanged);

            /// <summary>
            /// The composition every board-conditioned face with a consequence takes: the predicate as an
            /// ordinary Capability node standing first in a Sequence, and the branch reading its
            /// published flag through <c>synthetic-is-flagged@1</c> - milestone 8.40c's measured shape,
            /// because a Conditional's own branch cannot bind that Conditional's output.
            /// </summary>
            public EffectResolutionStepResult RunTheGatedConsequence(
                EffectCapabilityReference consequence)
            {
                var nodes = new List<EffectNodeDefinition>
                {
                    new EffectNodeDefinition(
                        RootNodeId, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { PredicateNodeId, BranchNodeId }),
                    new EffectNodeDefinition(
                        PredicateNodeId, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        BainComponentContent.BountyHeldV1,
                        outputs: new[]
                        {
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean),
                            new EffectOutputBinding(
                                SyntheticComponentCapabilities.MarkerHolderHandler.HolderOutputName,
                                EffectValueKind.Hero)
                        }),
                    new EffectNodeDefinition(
                        BranchNodeId, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { ConsequenceNodeId },
                        inputs: new[]
                        {
                            EffectInputBinding.FromNodeOutput(
                                "flag",
                                PredicateNodeId,
                                SyntheticComponentCapabilities.MarkerHolderHandler.PresentOutputName,
                                EffectValueKind.Boolean)
                        },
                        outputs: new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) }),
                    consequence.Equals(BainComponentContent.TransferBountyV1)
                        ? new EffectNodeDefinition(
                            ConsequenceNodeId, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory,
                            consequence,
                            inputs: new[]
                            {
                                EffectInputBinding.FromRole(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .HolderParameterName,
                                    EffectRoleKind.Actor,
                                    EffectValueKind.Hero)
                            },
                            outputs: new[]
                            {
                                new EffectOutputBinding("holder", EffectValueKind.Hero),
                                new EffectOutputBinding(
                                    SyntheticComponentCapabilities.MarkerTransferHandler
                                        .PreviousHolderOutputName,
                                    EffectValueKind.Hero)
                            })
                        : new EffectNodeDefinition(
                            ConsequenceNodeId, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory,
                            consequence)
                };

                return EffectResolutionSession.Start(
                    new EffectResolutionId("board-conditioned-consequence"),
                    ProbeBlockInstanceId,
                    default,
                    ProbeBlockActionId,
                    Defender,
                    Defender,
                    ActionInvocationContext.PrintedPrimary,
                    new EffectResolutionContext(
                        new EffectDefinition(
                            new EffectDefinitionId("board-conditioned-consequence"),
                            "board-conditioned-block-rev-1",
                            RootNodeId,
                            nodes),
                        SyntheticEffectCapabilities.CreateCatalog(
                            BainComponentContent.Handlers()),
                        participants,
                        null,
                        1,
                        1,
                        RuleModificationSet.Empty,
                        EffectCardState.Absent,
                        ActionKind.Defense,
                        components: new MatchComponents(
                            GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions
                                .CreateCatalog(),
                            board,
                            Markers,
                            board,
                            board,
                            board,
                            board)));
            }

            private MarkerAttachment Attach(
                MarkerDefinition definition, HeroId holder, HeroId owner)
            {
                var attachment = new MarkerAttachment(
                    new MarkerInstanceId(
                        $"{definition.Reference.DefinitionId.Value}-{++minted}"),
                    definition,
                    owner,
                    holder,
                    ProbeBlockInstanceId,
                    ProbeBlockActionId);
                Markers.Give(attachment);
                return attachment;
            }

            private ActionOffer BlockOffer(CardDefinition blockCard) =>
                new ActionOffer(
                    ProbeBlockInstanceId,
                    blockCard.Reference,
                    ProbeBlockActionId,
                    Defender,
                    ActionInvocationContext.PrintedPrimary,
                    true);

            private static CardDefinition BlockCard(PrintedBlockCondition condition) =>
                new CardDefinition(
                    ProbeBlockCardId,
                    "board-conditioned-block-rev-1",
                    "TEST - Board Conditioned Block",
                    new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[] { ProbeBlockAction(null, condition) });

            private static CardDefinition AttackCard(AttackRangeKind attackRange) =>
                new CardDefinition(
                    AttackCardId,
                    "board-conditioned-block-rev-1",
                    "TEST - Board Conditioned Attacker",
                    new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture,
                    CardProgressionProfile.NotStated,
                    new[]
                    {
                        new ActionDefinition(
                            AttackActionId,
                            ActionKind.Attack,
                            ActionDefinitionOrigin.PrintedPrimary,
                            attackRange == AttackRangeKind.Ranged
                                ? new[]
                                {
                                    new PrintedValueDefinition(PrintedValueChannel.Attack, 3),
                                    new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                                }
                                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                            attackRange,
                            new ActionCardStateTransition(
                                CardLocation.Hand, CardLocation.DiscardPile))
                    });

            private static HexCoordinate[] FeaturelessDisk(int radius)
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
}
