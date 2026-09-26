using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.61g. Min the Dragonmonk's borrowed after-attack text: "Target a unit in range. After
    /// the attack: Apply the \"After the attack\" text of your resolved or discarded red card. (If it
    /// has radius, use that card's value)."
    ///
    /// <para><b>The bodies borrowed here are stand-ins, and that is deliberate.</b> Milestone 8.61c
    /// composes her five Stances on a branch of its own, so this fixture cannot see them and must not
    /// guess at them. What it composes instead is the four SHAPES the borrow has to carry whatever
    /// 8.61c writes - a push of an enemy adjacent to her, read off the board the attack just left; an
    /// optional move that ends beside the unit the attack took; a reach measured by the BORROWED card's
    /// Radius icon with rulebook p.16's items; and a second optional step after the first, so that a
    /// borrowed body may stop twice. Each stand-in is named for the Stance it stands in for. If 8.61c
    /// writes a shape none of these covers, this file is what has to grow.</para>
    ///
    /// <para><b>Nothing of Min's is registered</b>, so no configured match can deal Fast as Lightning
    /// and no player can reach it - milestone 7.9's gate, which 8.61j flips. The driven cases below
    /// therefore compose a turn of their own over a catalog that adds the deferred handler to the
    /// build's registered set, which is the shape <c>GarrusAttackVocabularyTests</c> and
    /// <c>BounceBulletTimeRepeatTests</c> already use for unreachable content.</para>
    /// </summary>
    [TestFixture]
    public sealed class MinBorrowedRiderTests
    {
        private static readonly HeroId Min = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId FarEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static readonly HexCoordinate MinAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate EnemyAt = new HexCoordinate(1, 0);

        // ------------------------------------------------------------------------------------------
        // The pool - rulebook p.10's four positions for her one Red card, as a function of state alone
        // ------------------------------------------------------------------------------------------

        /// <summary>
        /// MI-Q7 read exactly: a card that has been performed, and a card that has been discarded,
        /// supply the rider; a card still in her hand and a card she has committed but not yet
        /// performed supply nothing at all.
        /// </summary>
        [Test]
        public void ThePool_IsHerRedCard_OnlyWhileItIsResolvedOrDiscarded()
        {
            Assert.That(PoolIds(Pool.InHand), Is.Empty,
                "A card in hand has not been resolved and has not been discarded.");
            Assert.That(PoolIds(Pool.PlayedUnresolved), Is.Empty,
                "p.10's middle state is neither of the two the clause names - the card is committed " +
                "and its text has not been applied.");
            Assert.That(PoolIds(Pool.Resolved), Is.EqualTo(new[] { StandInRedCardInstanceId }));
            Assert.That(PoolIds(Pool.Discarded), Is.EqualTo(new[] { StandInRedCardInstanceId }));
        }

        [Test]
        public void ThePool_NarrowsToRed_AndNeverReachesAnotherHerosCard()
        {
            var hers = ResolvedStandIn(Min, "min-red", CardColor.Red);
            var alsoHers = ResolvedStandIn(Min, "min-blue", CardColor.Blue);
            var theirs = ResolvedStandIn(Enemy, "enemy-red", CardColor.Red);

            var eligible = BorrowedCardTextDeclaration.EligibleCards(
                BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red),
                Min,
                StandInDefinitions(hers, alsoHers, theirs),
                new[] { hers.Card, alsoHers.Card, theirs.Card });

            Assert.That(eligible.Select(card => card.Id.Value), Is.EqualTo(new[] { "min-red" }),
                "\"your RED card\": p.9's colour narrows it, and p.10 keeps another hero's cards out " +
                "of reach however they are standing.");
        }

        /// <summary>
        /// The three arms that are not about state. A card printing no primary at all is rulebook
        /// p.16's Ultimate; a card whose text is transcribed but not composed is a p.20 Handicap - her
        /// own Chop Down among them; and a card whose primary prints no "After the attack:" line
        /// supplies no subject for the sentence to name.
        /// </summary>
        [Test]
        public void ThePool_RefusesAnUltimate_AParkedHandicap_AndAFaceWithNoAfterAttackText()
        {
            var noPrimary = new CardDefinition(
                new CardDefinitionId("min-test-no-primary"), "rev-1", "TEST - no primary action",
                new InitiativeValue(5), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.I, PrintedValueChannel.None),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                });
            Assert.That(BorrowedCardTextDeclaration.Refuses(noPrimary),
                Is.EqualTo(BorrowedCardTextRefusal.NoPrintedPrimaryAction),
                "p.16's Ultimate is the printed case: a card with no primary action supplies no " +
                "\"After the attack\" text either.");

            Assert.That(
                BorrowedCardTextDeclaration.Refuses(
                    MinCardContent.BorrowedRiderDefinition(MinCardContent.ChopDown)),
                Is.EqualTo(BorrowedCardTextRefusal.PrintedTextNotComposed),
                "Her own p.20 Handicap is the standing instance: a deferred face would apply a " +
                "shortened version of itself.");

            Assert.That(
                BorrowedCardTextDeclaration.Refuses(StandInRed(CardColor.Red, body: null).Definition),
                Is.EqualTo(BorrowedCardTextRefusal.NoAfterTheAttackText),
                "\"the \\\"After the attack\\\" text of that card\" names nothing on a face that " +
                "prints none.");

            Assert.That(BorrowedCardTextDeclaration.Refuses(null),
                Is.EqualTo(BorrowedCardTextRefusal.UnresolvedDefinition));
        }

        /// <summary>
        /// The guard that keeps a rider from applying itself forever. It cannot arise from either
        /// printed face - both are Gold and the pool is narrowed to Red - which is exactly why it is
        /// stated rather than left to luck.
        /// </summary>
        [Test]
        public void ThePool_NeverNamesTheCardWhoseTextIsAsking()
        {
            var hers = ResolvedStandIn(Min, "min-red", CardColor.Red);

            Assert.That(
                BorrowedCardTextDeclaration.EligibleCards(
                    BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red),
                    Min,
                    StandInDefinitions(hers),
                    new[] { hers.Card },
                    askingCardInstanceId: hers.Card.Id),
                Is.Empty);
        }

        [Test]
        public void ADeclarationIsValueEqual_SoBothOfHerGoldFacesPrintTheSameDeclaration()
        {
            Assert.That(
                BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red),
                Is.EqualTo(BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red)));
            Assert.That(
                BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red),
                Is.Not.EqualTo(
                    BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.None)));
            Assert.That(BorrowedCardTextDeclaration.BorrowedMoment,
                Is.EqualTo(CardTextTiming.AfterTheAttack),
                "The clause names one of rulebook p.14's moments and this vocabulary names the same.");
        }

        // ------------------------------------------------------------------------------------------
        // The two composed faces
        // ------------------------------------------------------------------------------------------

        [Test]
        public void FastAsLightning_IsTheRangedGoldBasic_WhoseWholeRiderIsOneBorrowNode()
        {
            var definition =
                MinCardContent.BorrowedRiderDefinition(MinCardContent.FastAsLightning);

            Assert.That(definition.Initiative.Value, Is.EqualTo(13));
            Assert.That(definition.Progression.Color, Is.EqualTo(CardColor.Gold));
            Assert.That(definition.Progression.IsBasic, Is.True);

            var attack = definition.ActionDefinitions.Single(
                action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            Assert.That(attack.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.Ranged),
                "\"BASIC ATTACK - RANGED\": p.13's Ranged sentence reaches by the printed Range icon.");
            Assert.That(Printed(attack, PrintedValueChannel.Attack), Is.EqualTo(2));
            Assert.That(Printed(attack, PrintedValueChannel.Range), Is.EqualTo(1));
            Assert.That(
                attack.BaseValues.Any(value => value.Channel == PrintedValueChannel.Radius), Is.False,
                "She prints NO Radius icon, which is the whole reason the parenthesis names the other " +
                "card's value - a missing icon is absent, never zero.");
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.AfterTheAttack));
            Assert.That(attack.TextComposition.IsAwaited, Is.False);

            Assert.That(
                definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                    .BaseValues.Single().Amount, Is.EqualTo(1));
            Assert.That(
                definition.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount, Is.EqualTo(1));

            var body = attack.CardTextAt(CardTextTiming.AfterTheAttack);
            Assert.That(body, Is.Not.Null);
            Assert.That(body.Nodes.Count, Is.EqualTo(1),
                "The clause is one instruction, and the borrow node being the LAST thing the body " +
                "does is what lets the dispatcher read the request from the step that completed it.");
            var node = body.Nodes.Single();
            Assert.That(node.Kind, Is.EqualTo(EffectNodeKind.Capability));
            Assert.That(node.Capability,
                Is.EqualTo(SyntheticBorrowedCardTextCapabilities.BorrowAfterAttackTextV1));
            Assert.That(node.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "No \"may\", no \"up to\", no \"if able\": p.10 makes the step mandatory, so an empty " +
                "pool is its impossible step rather than a decline.");
            Assert.That(
                node.Inputs.Single(input =>
                        input.ParameterName == SyntheticBorrowedCardTextCapabilities.ColorParameter)
                    .Literal.IntegerValue,
                Is.EqualTo((int)CardColor.Red));
            Assert.That(
                node.Inputs.Single(input =>
                        input.ParameterName == SyntheticBorrowedCardTextCapabilities.PoolParameter)
                    .Literal.IntegerValue,
                Is.EqualTo((int)BorrowedCardTextPool.OwnedResolvedOrDiscardedCards));
        }

        [Test]
        public void ChopDown_ShipsItsPrintedValuesAndIsParkedUnderTheEstablishedHandicapDeferral()
        {
            var definition = MinCardContent.BorrowedRiderDefinition(MinCardContent.ChopDown);
            var attack = definition.ActionDefinitions.Single(
                action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            Assert.That(definition.Initiative.Value, Is.EqualTo(12));
            Assert.That(Printed(attack, PrintedValueChannel.Attack), Is.EqualTo(1));
            Assert.That(Printed(attack, PrintedValueChannel.Range), Is.EqualTo(1));
            Assert.That(attack.TextComposition.IsAwaited, Is.True,
                "p.6 returns the Handicap to the box and p.20 brings it back only for an odd player " +
                "count - the established deferral every roster Handicap carries.");
            Assert.That(attack.EffectRoot, Is.Null,
                "and what is deferred is the COMPOSITION, so the parked face ships no body at all.");
            Assert.That(attack.CardTextTiming, Is.EqualTo(CardTextTiming.None));
            Assert.That(MinCardContent.ReturnedToBoxAtSetup,
                Is.EqualTo(new[] { MinCardContent.ChopDown }));
            Assert.That(definition.SourceContent.PrintedText,
                Does.Contain("Target a hero adjacent to you"),
                "Its whole printed sentence ships as transcription, its hero-only clause included.");
        }

        /// <summary>
        /// The gate, from both sides, FLIPPED at milestone 8.61j. A capability the build registers is
        /// a capability a configured match can start, and Fast as Lightning is a card a configured
        /// match now puts in a hand - so the contract is registered, its handler TYPE took a showcase
        /// consumer in the same change, and her face composes against the build's own catalog.
        /// </summary>
        [Test]
        public void TheRiderBorrowIsRegisteredByThisBuild_AndHerFaceComposesAgainstIt()
        {
            var registered = MatchContentCatalogs.ForThisBuild().Capabilities.Registered;
            Assert.That(
                registered.Contains(SyntheticBorrowedCardTextCapabilities.BorrowAfterAttackTextV1),
                Is.True,
                "borrow-after-attack-text@1 is named by her dealt Gold basic, which is what " +
                "milestone 7.9's gate asks before a registration joins the serialized set.");

            Assert.That(
                SyntheticBorrowedCardTextCapabilities.DeferredHandlers(), Is.Empty,
                "and the deferred list is empty, which is the other side of the same claim.");
            Assert.That(
                SyntheticBorrowedCardTextCapabilities.RegisteredHandlers()
                    .Select(handler => handler.Reference),
                Is.EqualTo(new[]
                {
                    SyntheticBorrowedCardTextCapabilities.BorrowAfterAttackTextV1
                }));

            var rider = MinCardContent
                .BorrowedRiderDefinition(MinCardContent.FastAsLightning)
                .ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .CardTextAt(CardTextTiming.AfterTheAttack);
            Assert.That(
                EffectDefinitionValidator.Validate(
                    rider, MatchContentCatalogs.ForThisBuild().Capabilities),
                Is.Empty,
                "and her face composes against what the build registers, which is the gate read " +
                "from the content's own side.");
        }

        [Test]
        public void TheComposedRider_ValidatesAgainstTheFixturesOwnCatalog()
        {
            var body = MinCardContent
                .BorrowedRiderDefinition(MinCardContent.FastAsLightning)
                .ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .CardTextAt(CardTextTiming.AfterTheAttack);

            Assert.That(EffectDefinitionValidator.Validate(body, Catalog()), Is.Empty);
        }

        // ------------------------------------------------------------------------------------------
        // The driven Attack - rulebook p.14's step 7, with the borrowed body spliced into it
        // ------------------------------------------------------------------------------------------

        [TestCase(Pool.Resolved)]
        [TestCase(Pool.Discarded)]
        public void TheRiderApplies_WhenHerRedCardIsResolvedOrDiscarded(Pool state)
        {
            var turn = new RiderTurn(state, StanceShape.CranePush);

            turn.Drive();

            Assert.That(turn.BorrowedNodes, Is.Not.Empty,
                turn.Trace() + " | The borrowed body has to actually run.");
            Assert.That(turn.PushedEnemyTo, Is.Not.Null,
                turn.Trace() + " | Crane's shape pushes an enemy unit adjacent to her, read off the " +
                "board the attack has just left.");
            Assert.That(turn.AttacksCompleted, Is.EqualTo(1),
                turn.Trace() + " | One attack, not two: the borrowed text is a rider on this one.");
        }

        [TestCase(Pool.InHand)]
        [TestCase(Pool.PlayedUnresolved)]
        public void TheRiderAppliesNothing_AndTheAttackIsUnaffected_WhenTheRedCardIsNeither(Pool state)
        {
            var turn = new RiderTurn(state, StanceShape.CranePush);

            turn.Drive();

            Assert.That(turn.BorrowedNodes, Is.Empty,
                turn.Trace() + " | The pool is empty, so p.10's impossible step is all that happens.");
            Assert.That(turn.PushedEnemyTo, Is.Null, turn.Trace());
            Assert.That(turn.AttacksCompleted, Is.EqualTo(1),
                turn.Trace() + " | MI-Q7: an impossible rider stops the TEXT and leaves the Attack " +
                "that carried it completely alone - it still reached its outcome.");
            Assert.That(turn.AttackWasSuccessful, Is.True, turn.Trace());
        }

        /// <summary>
        /// The optional move's destinations are measured from the unit the ATTACK took, which is the
        /// reading that makes "the target" inside a borrowed body mean anything at all. Tiger Stance's
        /// shape at distance 1 and Dragon Stance's at 1 or 2.
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        public void TheBorrowedMove_EndsBesideTheUnitThisAttackTook(int distance)
        {
            var turn = new RiderTurn(
                Pool.Resolved,
                distance == 1 ? StanceShape.TigerMoveThenPush : StanceShape.DragonMoveThenPush);

            turn.Drive();

            Assert.That(turn.BorrowedNodes, Does.Contain("stand-in-move"), turn.Trace());
            Assert.That(turn.MoveDestinations, Is.Not.Empty,
                turn.Trace() + " | The move is offered destinations beside the attack's own target.");
            Assert.That(
                turn.MoveDestinations.All(space =>
                    HexDistanceQuery.Calculate(space, EnemyAt) <= 1),
                Is.True,
                turn.Trace() + " | every one of them adjacent to the unit the attack took, or her " +
                "own space when she already stands beside it.");
            Assert.That(
                turn.MoveDestinations.All(space =>
                    HexDistanceQuery.Calculate(space, MinAt) <= distance),
                Is.True,
                turn.Trace() + " | and none of them further than the printed distance.");
        }

        /// <summary>
        /// The optional step is the player's to refuse, and refusing it leaves the board exactly as the
        /// attack left it. "You may" inside a borrowed body is still a "you may".
        /// </summary>
        [Test]
        public void TheBorrowedOptionalStep_IsDeclinable_AndDecliningMovesNothing()
        {
            var turn = new RiderTurn(
                Pool.Resolved, StanceShape.TigerMoveThenPush, declineTheOptionalStep: true);

            turn.Drive();

            Assert.That(turn.OptionalStepWasDeclinable, Is.True, turn.Trace());
            Assert.That(turn.PushedEnemyTo, Is.Null, turn.Trace());
            Assert.That(turn.MinStandsAt, Is.EqualTo(MinAt),
                turn.Trace() + " | She declined, so nothing of hers moved.");
            Assert.That(turn.AttacksCompleted, Is.EqualTo(1), turn.Trace());
        }

        /// <summary>
        /// THE PARENTHESIS, measured. "(If it has radius, use that card's value)": the borrowed body's
        /// reach is the RED card's printed Radius icon, and Fast as Lightning prints none at all - so a
        /// build that read the borrowing card's Radius would offer nothing whatsoever here, not a
        /// smaller set.
        /// </summary>
        [Test]
        public void TheBorrowedReach_IsTheRedCardsPrintedRadius_AndNeverFastAsLightnings()
        {
            var near = new RiderTurn(
                Pool.Resolved, StanceShape.ViperRadiusReach, radius: 4, farEnemyAt: new HexCoordinate(0, 5));
            near.Drive();

            Assert.That(near.BorrowedNodes, Does.Contain("stand-in-radius-target"),
                near.Trace() + " | The Radius node publishes the borrowed card's own icon and the " +
                "selection it feeds is reached; on Fast as Lightning's the publisher would have been " +
                "impossible, because she prints no Radius icon at all.");
            Assert.That(near.RadiusCandidates, Is.EqualTo(new[] { Enemy }),
                near.Trace() + " | Radius 4 reaches the adjacent enemy and not the one five away.");

            var wide = new RiderTurn(
                Pool.Resolved, StanceShape.ViperRadiusReach, radius: 4,
                farEnemyAt: new HexCoordinate(0, 5), radiusItems: 1);
            wide.Drive();

            Assert.That(wide.RadiusCandidates, Is.EquivalentTo(new[] { Enemy, FarEnemy }),
                wide.Trace() + " | p.16's Radius item raises the BORROWED card's icon to 5, which is " +
                "the whole of what \"use that card's value\" buys.");
        }

        /// <summary>
        /// A borrowed body may stop more than once, and the page stays stopped until the last of them
        /// is answered - Cobra Stance's swap and its re-place are two questions, not one.
        /// </summary>
        [Test]
        public void ABorrowedBody_MayStopTwice_AndThePageResumesOnceBothAreAnswered()
        {
            var turn = new RiderTurn(
                Pool.Resolved, StanceShape.CobraRadiusReachTwice, radius: 4,
                farEnemyAt: new HexCoordinate(0, 3));

            turn.Drive();

            Assert.That(turn.BorrowedPauses, Is.GreaterThanOrEqualTo(2), turn.Trace());
            Assert.That(turn.AttacksCompleted, Is.EqualTo(1),
                turn.Trace() + " | and the attack still reaches exactly one completion.");
        }

        /// <summary>
        /// The unit is gone, so a clause naming it has no referent and the step is p.10's impossible
        /// one - exactly what the same sentence would do printed on the Red card itself.
        /// </summary>
        [Test]
        public void WhenTheAttackRemovedItsTarget_TheBorrowedStepNamingItSimplyStops()
        {
            var turn = new RiderTurn(
                Pool.Resolved, StanceShape.TigerMoveThenPush, defendTheAttack: false);

            turn.Drive();

            Assert.That(turn.TargetWasRemoved, Is.True,
                turn.Trace() + " | The fixture is only the fixture while the attack actually removes " +
                "the unit its rider would name.");
            Assert.That(turn.MoveDestinations, Is.Empty,
                turn.Trace() + " | \"a space adjacent to the target\" has no referent left.");
            Assert.That(turn.AttacksCompleted, Is.EqualTo(1),
                turn.Trace() + " | and p.14 still finished the attack that removed it.");
        }

        /// <summary>
        /// p.16's Range item on the BORROWING card, which is the other half of the same page and the
        /// one Fast as Lightning's own icon reads. Range 1 reaches the adjacent enemy alone; Range 2
        /// reaches the one two spaces away as well.
        /// </summary>
        [Test]
        public void ARangeItem_RaisesFastAsLightningsOwnReachToTwo()
        {
            var printed = new RiderTurn(
                Pool.Resolved, StanceShape.CranePush, farEnemyAt: new HexCoordinate(2, 0));
            printed.Drive();
            Assert.That(printed.AttackTargets, Is.EqualTo(new[] { Enemy }),
                printed.Trace() + " | Printed Range 1 is adjacency.");

            var raised = new RiderTurn(
                Pool.Resolved, StanceShape.CranePush, farEnemyAt: new HexCoordinate(2, 0),
                rangeItems: 1);
            raised.Drive();
            Assert.That(raised.AttackTargets, Is.EquivalentTo(new[] { Enemy, FarEnemy }),
                raised.Trace() + " | and a Range item raises the printed icon by one, which is p.16 " +
                "read on the card that prints it.");
        }

        /// <summary>
        /// Milestone 8.35i's rule, on the first content that pauses inside a body belonging to a
        /// different card than the Attack: the pause restores, it still offers what it offered, and it
        /// is answerable afterwards. Nothing new is written for it - the paused frame names the
        /// borrowed card, its action and its body, which the payload has carried since 8.10.
        /// </summary>
        [Test]
        public void APausePausedInsideABorrowedBody_RoundTripsThroughASnapshotAndStillOffersWhatItOffered()
        {
            var turn = new RiderTurn(Pool.Resolved, StanceShape.CranePush);

            turn.DriveToTheBorrowedPause();

            var before = turn.PendingChoice;
            Assert.That(before, Is.Not.Null, turn.Trace());
            Assert.That(turn.PausedInsideAnAttack, Is.True,
                turn.Trace() + " | and it is paused INSIDE the Attack, which is the whole subject.");
            Assert.That(turn.PausedFrameSourceCard, Is.EqualTo(StandInRedCardInstanceId),
                turn.Trace() + " | the paused frame belongs to the BORROWED card, which is the fact " +
                "the restore has to rebuild.");

            var payload = turn.Capture();
            var restored = turn.Restore(payload);

            Assert.That(turn.Capture(), Is.EqualTo(payload),
                "Reading the payload back has to produce the state it was written from.");
            var after = restored.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(after, Is.Not.Null);
            Assert.That(after.NodeId, Is.EqualTo(before.NodeId));
            Assert.That(after.Candidates, Is.EqualTo(before.Candidates),
                "The re-asked handler offers the same units, because it is asked the same question - " +
                "which is what carrying the enclosing Attack across the restore buys.");
            Assert.That(
                restored.TryAnswerEffectChoice(after.SelectInvocations.First()).Accepted, Is.True,
                "and the restored pause takes its own published answer.");
        }

        /// <summary>
        /// The schema is not moved by any of this, and that is stated rather than assumed: a paused
        /// borrowed body writes a card-text continuation exactly as a paused ordinary rider does.
        /// </summary>
        [Test]
        public void TheBorrowedPause_MovesNoSnapshotSchemaVersion()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(46),
                "8.61g adds no persisted field: the borrowed frame is an ordinary paused card text " +
                "whose card happens to be another of hers." +
                "Milestone 8.71a moved it again, to v44, for Hanu the Trickster's " +
                "rewritten printed Initiative - a sparse card-keyed list in the card " +
                "section, which is authoritative state no live state remembers.");
        }

        /// <summary>
        /// Every state the driven stream passes through round-trips, which is the regression shape
        /// milestone 8.35i recorded: the defect there was invisible until one intermediate state was
        /// captured.
        /// </summary>
        [Test]
        public void EveryStateOfTheDrivenStream_RoundTripsThroughASnapshot()
        {
            var turn = new RiderTurn(Pool.Resolved, StanceShape.CranePush, captureEveryStep: true);

            turn.Drive();

            Assert.That(turn.CapturedStates, Is.GreaterThan(0));
            Assert.That(turn.StatesPausedInsideABorrowedBody, Is.GreaterThan(0),
                turn.Trace() + " | The stream has to actually pass through a pause inside the " +
                "borrowed body, or this case round-trips a match that never reaches the state.");
        }

        /// <summary>
        /// Milestone 8.61h's Flurry of Blows repeats an attack action, and a repeated instance runs
        /// rulebook p.14 again - so its step 7 asks the borrow again and gets the same answer, because
        /// applying a card's text moves no card and the pool is a pure function of card states.
        ///
        /// <para>The repeat is driven here from a card-text <c>RepeatAction</c> rather than from
        /// Flurry, which is a p.16 permanent ability 8.61h owns: what both produce is a second Attack
        /// instance, and it is the second instance's own p.14 that this case is about.</para>
        /// </summary>
        [Test]
        public void ARepeatedInstanceOfTheAttack_BorrowsAgain()
        {
            // The borrowed shape here moves nobody: Crane's push would carry the target three spaces
            // out of Range 1, and the second instance would then have no legal target at all - which is
            // a true fact about the board rather than about the borrow.
            var turn = new RiderTurn(
                Pool.Resolved, StanceShape.ViperRadiusReach, repeatTheAttack: true);

            turn.Drive();

            Assert.That(turn.AttacksCompleted, Is.EqualTo(2),
                turn.Trace() + " | p.10's repeat is a second instance of the action.");
            Assert.That(turn.BorrowedBodies, Is.EqualTo(2),
                turn.Trace() + " | and each instance's own step 7 borrows: nothing is consumed, so " +
                "the pool still names the same resolved card.");
        }

        // ------------------------------------------------------------------------------------------
        // Fixture
        // ------------------------------------------------------------------------------------------

        /// <summary>Rulebook p.10's four positions a hero's card can be in.</summary>
        public enum Pool
        {
            InHand,
            PlayedUnresolved,
            Resolved,
            Discarded
        }

        /// <summary>Which Stance's SHAPE the stand-in body carries. See the type's own remarks.</summary>
        public enum StanceShape
        {
            /// <summary>Crane: "Push an enemy unit adjacent to you up to 3 spaces."</summary>
            CranePush,

            /// <summary>Tiger: "You may move 1 space to a space adjacent to the target", then the push.</summary>
            TigerMoveThenPush,

            /// <summary>Dragon: the same at 1 or 2 spaces.</summary>
            DragonMoveThenPush,

            /// <summary>Viper: an optional step reaching "in radius" - the swap's own reach.</summary>
            ViperRadiusReach,

            /// <summary>Cobra: the same, and a second optional step after it.</summary>
            CobraRadiusReachTwice
        }

        private const string StandInRedCardId = "min-stand-in-red";
        private static readonly CardInstanceId StandInRedCardInstanceId =
            new CardInstanceId(StandInRedCardId);

        private static int Printed(ActionDefinition action, PrintedValueChannel channel) =>
            action.BaseValues.Single(value => value.Channel == channel).Amount;

        private static EffectCapabilityCatalog Catalog()
        {
            var defaults = MatchContentCatalogs.ForThisBuild();
            return new EffectCapabilityCatalog(defaults.Capabilities.Registered
                .Select(defaults.Capabilities.Load)
                .Concat(SyntheticBorrowedCardTextCapabilities.DeferredHandlers())
                .Concat(SyntheticDisplacementCapabilities.DeferredHandlers())
                .Concat(SyntheticPlacementCapabilities.DeferredHandlers())
                .Concat(SyntheticRosterCapabilities.DeferredHandlers())
                .GroupBy(handler => handler.Reference)
                .Select(group => group.First()));
        }

        private static IReadOnlyList<CardInstanceId> PoolIds(Pool state)
        {
            var stand = StandIn(Min, StandInRedCardId, CardColor.Red, state);
            return BorrowedCardTextDeclaration.EligibleCards(
                    BorrowedCardTextDeclaration.FromResolvedOrDiscardedCards(CardColor.Red),
                    Min,
                    StandInDefinitions(stand),
                    new[] { stand.Card })
                .Select(card => card.Id)
                .ToArray();
        }

        private static IEnumerable<CardDefinition> StandInDefinitions(params StandInCard[] cards) =>
            cards.Select(card => card.Definition)
                .GroupBy(definition => definition.Reference)
                .Select(group => group.First());

        private static StandInCard ResolvedStandIn(HeroId owner, string id, CardColor color) =>
            StandIn(owner, id, color, Pool.Resolved);

        private static StandInCard StandIn(HeroId owner, string id, CardColor color, Pool state)
        {
            var stand = StandInRed(color, StanceBodies.For(StanceShape.CranePush));
            var card = new CardInstance(
                new CardInstanceId(id), stand.Definition.Reference, owner, StateFor(state));
            return new StandInCard(stand.Definition, card);
        }

        private static CardStateFacts StateFor(Pool state)
        {
            switch (state)
            {
                case Pool.InHand:
                    return CardStateFacts.InitialInHand();
                case Pool.PlayedUnresolved:
                    return new CardStateFacts(
                        CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                        CardFaceOrientation.FaceUp);
                case Pool.Resolved:
                    return new CardStateFacts(
                        CardLocation.TurnArea, true, CardResolutionStatus.Resolved,
                        CardFaceOrientation.FaceUp);
                default:
                    return new CardStateFacts(
                        CardLocation.DiscardPile, true, CardResolutionStatus.Resolved,
                        CardFaceOrientation.FaceUp);
            }
        }

        private sealed class StandInCard
        {
            public StandInCard(CardDefinition definition, CardInstance card)
            {
                Definition = definition;
                Card = card;
            }

            public CardDefinition Definition { get; }
            public CardInstance Card { get; }
        }

        private sealed class StandInRedCard
        {
            public StandInRedCard(CardDefinition definition)
            {
                Definition = definition;
            }

            public CardDefinition Definition { get; }
        }

        /// <summary>
        /// A Red Attack face of the shape 8.61c will compose - printed Radius, a primary Attack and one
        /// "After the attack:" body. It is the fixture's own content, which is what keeps this file
        /// independent of that block.
        /// </summary>
        private static StandInRedCard StandInRed(
            CardColor color, EffectDefinition body, int radius = 4)
        {
            // One definition per colour: a pool narrowed by colour cannot be measured against three
            // instances that share one printed face.
            var id = new CardDefinitionId(
                color == CardColor.Red
                    ? StandInRedCardId
                    : StandInRedCardId + "-" + color.ToString().ToLowerInvariant());
            var values = new List<PrintedValueDefinition>
            {
                new PrintedValueDefinition(PrintedValueChannel.Attack, 3)
            };
            if (radius > 0)
            {
                values.Add(new PrintedValueDefinition(PrintedValueChannel.Radius, radius));
            }

            return new StandInRedCard(new CardDefinition(
                id,
                "min-stand-in-rev-1",
                "TEST - Min stand-in Stance",
                new InitiativeValue(8),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    color, CardTier.I, PrintedValueChannel.None),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-attack"),
                        ActionKind.Attack,
                        ActionDefinitionOrigin.PrintedPrimary,
                        values,
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.NonRanged,
                        default,
                        body,
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        body == null ? CardTextTiming.None : CardTextTiming.AfterTheAttack),
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"),
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 4) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                }));
        }

        /// <summary>
        /// One of Min's turns: Fast as Lightning committed, her Red stand-in standing in one of
        /// rulebook p.10's four positions, and two enemy heroes - one adjacent, one further off.
        ///
        /// <para>Shaped after <see cref="BounceBulletTimeRepeatTests"/>' own driver: it answers
        /// whichever decision the round is actually waiting on and records what it was shown, so
        /// nothing here names a command the rules were not already offering.</para>
        /// </summary>
        private sealed class RiderTurn
        {
            private readonly List<CardDefinition> definitions = new List<CardDefinition>();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly MatchParticipants participants;
            private readonly AdjacentMoveAuthority board;
            private readonly MatchContentCatalogs catalogs;
            private readonly RoundResolutionAuthority round;
            private readonly CardActionResolutionAuthority cardActions;
            private readonly List<string> borrowedNodes = new List<string>();
            private readonly List<string> trace = new List<string>();
            private readonly List<HeroId> radiusCandidates = new List<HeroId>();
            private readonly List<HexCoordinate> moveDestinations = new List<HexCoordinate>();
            private readonly List<HeroId> attackTargets = new List<HeroId>();
            private readonly CardInstance attackCard;
            private readonly bool declineTheOptionalStep;
            private readonly bool defendTheAttack;
            private readonly bool captureEveryStep;
            private HexCoordinate enemyStartedAt;

            public RiderTurn(
                Pool state,
                StanceShape shape,
                bool declineTheOptionalStep = false,
                bool defendTheAttack = true,
                HexCoordinate? farEnemyAt = null,
                int radius = 4,
                int radiusItems = 0,
                int rangeItems = 0,
                bool repeatTheAttack = false,
                bool captureEveryStep = false)
            {
                this.declineTheOptionalStep = declineTheOptionalStep;
                this.defendTheAttack = defendTheAttack;
                this.captureEveryStep = captureEveryStep;
                participants = StagedBoardParticipants.CreateWithBlueAlly();

                var coordinates = new List<HexCoordinate>();
                for (var q = -6; q <= 6; q++)
                for (var r = -6; r <= 6; r++)
                {
                    if (Math.Abs(q + r) <= 6) coordinates.Add(new HexCoordinate(q, r));
                }

                var far = farEnemyAt ?? new HexCoordinate(0, 6);
                board = new AdjacentMoveAuthority(
                    coordinates,
                    new[]
                    {
                        new HeroOccupancy(Min, MinAt),
                        new HeroOccupancy(Enemy, EnemyAt),
                        new HeroOccupancy(FarEnemy, far)
                    },
                    Enumerable.Empty<ObstacleOccupancy>(),
                    Enumerable.Empty<QuickGameMinionSetupState>());

                // Her two composed faces, the Red stand-in whose body is borrowed, and the filler the
                // other seats commit and defend with.
                definitions.AddRange(MinCardContent.BorrowedRiderDefinitions);
                var standIn = StandInRed(
                    CardColor.Red, StanceBodies.For(shape, shape == StanceShape.DragonMoveThenPush ? 2 : 1),
                    radius);
                definitions.Add(standIn.Definition);
                definitions.Add(Filler);
                definitions.Add(ShieldCard);
                definitions.Add(RadiusItemCard);
                definitions.Add(RangeItemCard);
                if (repeatTheAttack)
                {
                    definitions.Add(RepeatingAttackCard);
                }

                attackCard = CardInstance.CreateInHand(
                    new CardInstanceId("min-attack-card"),
                    (repeatTheAttack
                        ? RepeatingAttackCard
                        : MinCardContent.BorrowedRiderDefinition(MinCardContent.FastAsLightning))
                    .Reference,
                    Min);
                instances.Add(attackCard);
                instances.Add(new CardInstance(
                    StandInRedCardInstanceId, standIn.Definition.Reference, Min, StateFor(state)));

                foreach (var enemy in new[] { Enemy, FarEnemy })
                {
                    instances.Add(CardInstance.CreateInHand(
                        new CardInstanceId(enemy.Value + "-commit"), Filler.Reference, enemy));
                    instances.Add(CardInstance.CreateInHand(
                        new CardInstanceId(enemy.Value + "-shield"), ShieldCard.Reference, enemy));
                }

                var items = new List<CardInstance>();
                for (var index = 0; index < radiusItems; index++)
                {
                    items.Add(SetAside("min-radius-item-" + index, RadiusItemCard));
                }

                for (var index = 0; index < rangeItems; index++)
                {
                    items.Add(SetAside("min-range-item-" + index, RangeItemCard));
                }

                instances.AddRange(items);

                var defaults = MatchContentCatalogs.ForThisBuild();
                catalogs = new MatchContentCatalogs(Catalog(), defaults.Components);
                cardActions = new CardActionResolutionAuthority(
                    catalogs,
                    board,
                    new MarkerAttachmentRegister(),
                    new HeroProgressionState(participants),
                    new TeamLifeState(participants, 8));
                round = new RoundResolutionAuthority(
                    definitions, instances, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForCardActionsOnly(cardActions));

                foreach (var item in items)
                {
                    Assert.That(
                        round.HeroItems.TryPlaceSetAsideCard(item, definitions).IsPlaced, Is.True,
                        "p.16 places the set-aside card as an item before the turn reads it.");
                }

                Accept(round.TryCommitCard(Min, attackCard.Id));
                foreach (var enemy in new[] { Enemy, FarEnemy })
                {
                    Accept(round.TryCommitCard(
                        enemy, new CardInstanceId(enemy.Value + "-commit")));
                }
            }

            public IReadOnlyList<string> BorrowedNodes => borrowedNodes;

            public IReadOnlyList<HeroId> RadiusCandidates => radiusCandidates;

            public IReadOnlyList<HexCoordinate> MoveDestinations => moveDestinations;

            public IReadOnlyList<HeroId> AttackTargets => attackTargets;

            public int AttacksCompleted { get; private set; }

            public int ActionsEnded { get; private set; }

            public int BorrowedBodies { get; private set; }

            public int BorrowedPauses { get; private set; }

            public bool OptionalStepWasDeclinable { get; private set; }

            public bool AttackWasSuccessful { get; private set; }

            public bool TargetWasRemoved =>
                !board.Occupancies.Any(occupancy => occupancy.HeroId.Equals(Enemy));

            public HexCoordinate? PushedEnemyTo
            {
                get
                {
                    var now = board.Occupancies
                        .Where(occupancy => occupancy.HeroId.Equals(Enemy))
                        .Select(occupancy => (HexCoordinate?)occupancy.Coordinate)
                        .FirstOrDefault();
                    return now.HasValue && !now.Value.Equals(enemyStartedAt) ? now : null;
                }
            }

            public HexCoordinate MinStandsAt =>
                board.Occupancies.Single(occupancy => occupancy.HeroId.Equals(Min)).Coordinate;

            public PendingEffectChoice PendingChoice => round.CurrentTurnAuthority?.PendingEffectChoice;

            public bool PausedInsideAnAttack =>
                cardActions.PendingCardTextResolution != null &&
                cardActions.PendingCardTextResolution.Attack != null;

            public CardInstanceId PausedFrameSourceCard =>
                cardActions.PendingCardTextResolution.Frame.SourceCardInstanceId;

            public int CapturedStates { get; private set; }

            public int StatesPausedInsideABorrowedBody { get; private set; }

            public string Trace() => string.Join(",", trace);

            public string Capture() => AuthoritativeSnapshotSerializer.Serialize(round, board);

            public RoundResolutionAuthority Restore(string payload)
            {
                var replay = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, payload, definitions, new[] { board.MapDefinition },
                    Array.Empty<QuickGameMinionSetupState>());
                Assert.That(
                    AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board),
                    Is.EqualTo(payload));
                return replay.Round;
            }

            public void Drive() => Drive(stopAtTheBorrowedPause: false);

            public void DriveToTheBorrowedPause() => Drive(stopAtTheBorrowedPause: true);

            private void Drive(bool stopAtTheBorrowedPause)
            {
                enemyStartedAt = EnemyAt;
                var started = false;
                for (var step = 0; step < 160; step++)
                {
                    var turn = round.CurrentTurnAuthority;
                    if (turn == null)
                    {
                        trace.Add("no-turn");
                        return;
                    }

                    if (turn.PendingChoice != null)
                    {
                        trace.Add("tie");
                        var tie = turn.PendingChoice;
                        Accept(round.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(attackCard.Id)
                                ? attackCard.Id
                                : tie.CandidateCardInstanceIds[0]));
                        continue;
                    }

                    if (turn.PendingTurnRelease != null)
                    {
                        trace.Add("release");
                        Accept(round.TryReleaseTurn());
                        continue;
                    }

                    if (turn.PendingAttackTargetChoice != null)
                    {
                        var choice = turn.PendingAttackTargetChoice;
                        trace.Add("target");
                        attackTargets.Clear();
                        attackTargets.AddRange(choice.LegalTargetHeroIds);
                        Accept(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                            choice.ResolutionId, choice.ChoiceId, Enemy)));
                        continue;
                    }

                    if (turn.PendingDefenseChoice != null)
                    {
                        var choice = turn.PendingDefenseChoice;
                        trace.Add("defense");
                        var shields = choice.LegalDefenseOffers
                            .Where(offer =>
                                offer.SourceCardInstanceId.Value.EndsWith(
                                    "-shield", StringComparison.Ordinal))
                            .ToArray();
                        Accept(round.TryAnswerDefenseResponse(
                            defendTheAttack && shields.Length > 0
                                ? DefenseResponseChoiceCommand.SelectCards(
                                    choice.ResolutionId, choice.ChoiceId,
                                    shields[0].SourceCardInstanceId)
                                : DefenseResponseChoiceCommand.Decline(
                                    choice.ResolutionId, choice.ChoiceId)));
                        continue;
                    }

                    if (turn.PendingEffectChoice != null)
                    {
                        var choice = turn.PendingEffectChoice;
                        var node = choice.NodeId.Value;
                        trace.Add("choice:" + node);
                        if (node.StartsWith("stand-in-", StringComparison.Ordinal))
                        {
                            borrowedNodes.Add(node);
                            BorrowedPauses++;
                            if (node == "stand-in-push-target" || node == "stand-in-root")
                            {
                                BorrowedBodies++;
                            }
                            if (stopAtTheBorrowedPause) return;
                        }

                        if (node == "stand-in-root" || node == "stand-in-again")
                        {
                            OptionalStepWasDeclinable = choice.DeclineInvocation != null;
                        }

                        if (node == "stand-in-move")
                        {
                            moveDestinations.Clear();
                            moveDestinations.AddRange(choice.Candidates
                                .Where(candidate =>
                                    candidate.Value.Kind == EffectValueKind.Coordinate)
                                .Select(candidate => candidate.Value.Coordinate));
                        }

                        if (node.StartsWith("stand-in-radius-target", StringComparison.Ordinal))
                        {
                            radiusCandidates.Clear();
                            radiusCandidates.AddRange(choice.Candidates
                                .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                                .Select(candidate => candidate.Value.HeroId));
                        }

                        var answer =
                            declineTheOptionalStep && node == "stand-in-root" &&
                            choice.DeclineInvocation != null
                                ? choice.DeclineInvocation
                                : choice.SelectInvocations.Count > 0
                                    ? choice.SelectInvocations[0]
                                    : choice.ConfirmInvocation;
                        Assert.That(answer, Is.Not.Null,
                            Trace() + " | " + node + " offered no answer at all.");
                        Accept(round.TryAnswerEffectChoice(answer));
                        continue;
                    }

                    if (!started && turn.SelectedActor.CardInstanceId.IsDefined)
                    {
                        if (!turn.SelectedActor.CardInstanceId.Equals(attackCard.Id))
                        {
                            trace.Add("hold");
                            Accept(round.TryPerformHold(turn.SelectedActor.CardInstanceId));
                            continue;
                        }

                        trace.Add("start");
                        var offer = ActionOfferQuery
                            .GetAvailableTurnActionOffers(
                                definitions, instances, Min, attackCard.Id)
                            .Single(value => value.ActionDefinitionId.Equals(
                                MinCardContent.PrimaryRangedAttack));
                        Accept(round.TryStartAttack(
                            new ActionResolutionId("min-borrowed-rider"), offer));
                        started = true;
                        continue;
                    }

                    trace.Add("break");
                    break;
                }
            }

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    Trace() + " | refused: " + result.RejectionReason + "/" +
                    result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                foreach (var produced in result.Events)
                {
                    var fact = produced.TurnEvent.CardActionEvent;
                    if (fact.Kind == CardActionResolutionEventKind.AttackResolutionCompleted)
                    {
                        AttacksCompleted++;
                        AttackWasSuccessful = true;
                    }

                    if (produced.TurnEvent.Kind == TurnResolutionEventKind.BasicActionPerformed)
                    {
                        ActionsEnded++;
                    }
                }

                if (!captureEveryStep) return;
                var payload = Capture();
                Restore(payload);
                CapturedStates++;
                if (PendingChoice != null &&
                    PendingChoice.NodeId.Value.StartsWith("stand-in-", StringComparison.Ordinal) &&
                    PausedInsideAnAttack)
                {
                    StatesPausedInsideABorrowedBody++;
                }
            }

            private CardInstance SetAside(string id, CardDefinition definition) =>
                new CardInstance(
                    new CardInstanceId(id), definition.Reference, Min,
                    new CardStateFacts(
                        CardLocation.SetAside, false, CardResolutionStatus.None,
                        CardFaceOrientation.FaceDown));
        }

        /// <summary>A card the other seats commit so the round has a turn for every hero.</summary>
        private static readonly CardDefinition Filler = new CardDefinition(
            new CardDefinitionId("min-rider-filler"), "min-stand-in-rev-1", "TEST - filler",
            new InitiativeValue(20), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
            });

        /// <summary>
        /// The card an attacked seat spends at rulebook p.14's step 3. Defense 4 beats Fast as
        /// Lightning's printed Attack 2, so the target survives for the rider to name.
        /// </summary>
        private static readonly CardDefinition ShieldCard = new CardDefinition(
            new CardDefinitionId("min-rider-shield"), "min-stand-in-rev-1", "TEST - shield",
            new InitiativeValue(19), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver),
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static readonly CardDefinition RadiusItemCard = ItemCard(
            "min-rider-radius-item", PrintedValueChannel.Radius);

        private static readonly CardDefinition RangeItemCard = ItemCard(
            "min-rider-range-item", PrintedValueChannel.Range);

        /// <summary>A set-aside upgrade sibling, which is what rulebook p.16 turns into an item.</summary>
        private static CardDefinition ItemCard(string id, PrintedValueChannel icon) =>
            new CardDefinition(
                new CardDefinitionId(id), "min-stand-in-rev-1", "TEST - " + icon + " item",
                new InitiativeValue(18), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(CardColor.Green, CardTier.II, icon),
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true))
                });

        /// <summary>
        /// Fast as Lightning's own shape with rulebook p.10's repeat printed into its step-7 body, as a
        /// stand-in for milestone 8.61h's Flurry of Blows: what both produce is a SECOND instance of the
        /// attack action, and what this fixture is about is that the second instance's own step 7 asks
        /// the borrow again. The repeat node stands BEFORE the borrow so that the borrow is still the
        /// last thing the body does.
        /// </summary>
        private static readonly CardDefinition RepeatingAttackCard = new CardDefinition(
            new CardDefinitionId("min-rider-repeating"), "min-stand-in-rev-1",
            "TEST - Fast as Lightning with a repeat", new InitiativeValue(13),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    MinCardContent.PrimaryRangedAttack, ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 2),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 1)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    RepeatingRider(),
                    PrintedTextComposition.None,
                    PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack),
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static EffectDefinition RepeatingRider()
        {
            var borrow = MinEffectContent
                .BorrowedAfterAttackRider(
                    new CardDefinitionId("min-rider-repeating"), "min-stand-in-rev-1")
                .Nodes.Single();
            return new EffectDefinition(
                new EffectDefinitionId("min-rider-repeating-after-attack"), "min-stand-in-rev-1",
                new EffectNodeId("repeat-root"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("repeat-root"), EffectNodeKind.Sequence,
                        EffectFailurePolicy.Mandatory,
                        childNodeIds: new[]
                        {
                            new EffectNodeId("stand-in-repeat"), borrow.Id
                        }),
                    new EffectNodeDefinition(
                        new EffectNodeId("stand-in-repeat"), EffectNodeKind.RepeatAction,
                        EffectFailurePolicy.May, chooser: EffectChooserRole.Actor,
                        maximumRepeats: 1),
                    borrow
                });
        }

        /// <summary>
        /// The four SHAPES a borrowed Stance body can carry, composed here rather than taken from
        /// milestone 8.61c - see the fixture's own remarks for why. Every node id is prefixed
        /// <c>stand-in-</c> so the driver can tell a borrowed question from the Attack's own.
        /// </summary>
        private static class StanceBodies
        {
            public static EffectDefinition For(StanceShape shape, int moveDistance = 1)
            {
                switch (shape)
                {
                    case StanceShape.CranePush:
                        return Body("stand-in-crane", new[] { PushTarget(), Push() },
                            Sequence("stand-in-root", "stand-in-push-target", "stand-in-push"));
                    case StanceShape.TigerMoveThenPush:
                    case StanceShape.DragonMoveThenPush:
                        return Body("stand-in-tiger",
                            new[]
                            {
                                Sequence("stand-in-advance", "stand-in-actor-space", "stand-in-move",
                                    "stand-in-push-target", "stand-in-push"),
                                ActorSpace(), Move(moveDistance), PushTarget(), Push()
                            },
                            May("stand-in-root", "stand-in-advance"));
                    case StanceShape.ViperRadiusReach:
                        return Body("stand-in-viper",
                            new[]
                            {
                                Sequence("stand-in-reach", "stand-in-radius", "stand-in-radius-target"),
                                CardRadius(), RadiusTarget("stand-in-radius-target")
                            },
                            May("stand-in-root", "stand-in-reach"));
                    default:
                        return Body("stand-in-cobra",
                            new[]
                            {
                                Sequence("stand-in-reach", "stand-in-radius", "stand-in-radius-target",
                                    "stand-in-again"),
                                CardRadius(), RadiusTarget("stand-in-radius-target"),
                                May("stand-in-again", "stand-in-radius-target-2"),
                                RadiusTarget("stand-in-radius-target-2")
                            },
                            May("stand-in-root", "stand-in-reach"));
                }
            }

            private static EffectDefinition Body(
                string id, IEnumerable<EffectNodeDefinition> nodes, EffectNodeDefinition root) =>
                new EffectDefinition(
                    new EffectDefinitionId(id), "min-stand-in-rev-1", new EffectNodeId("stand-in-root"),
                    new[] { root }.Concat(nodes));

            private static EffectNodeDefinition Sequence(string id, params string[] children) =>
                new EffectNodeDefinition(
                    new EffectNodeId(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: Array.ConvertAll(children, value => new EffectNodeId(value)));

            private static EffectNodeDefinition May(string id, string child) =>
                new EffectNodeDefinition(
                    new EffectNodeId(id), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { new EffectNodeId(child) },
                    chooser: EffectChooserRole.Actor);

            private static EffectNodeDefinition ActorSpace() =>
                new EffectNodeDefinition(
                    new EffectNodeId("stand-in-actor-space"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticPlacementCapabilities.ActorSpaceV1,
                    outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) });

            /// <summary>
            /// "You may move N spaces to a space adjacent to the target" - the destination policy that
            /// names the Attack's own target, which is the half of a Stance the borrow has to carry.
            /// </summary>
            private static EffectNodeDefinition Move(int distance) =>
                new EffectNodeDefinition(
                    new EffectNodeId("stand-in-move"), EffectNodeKind.Capability,
                    EffectFailurePolicy.UpTo,
                    SyntheticDisplacementCapabilities.MoveAdjacentToEnemyHeroForPushV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "mover", new EffectNodeId("stand-in-actor-space"), "space",
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(distance)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticDisplacementCapabilities.DestinationPolicyParameterName,
                            DirectMoveDestinationPolicy.Unrestricted)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding("moved", EffectValueKind.Coordinate)
                    });

            /// <summary>"an enemy unit adjacent to you", read off the board the attack has just left.</summary>
            private static EffectNodeDefinition PushTarget() =>
                new EffectNodeDefinition(
                    new EffectNodeId("stand-in-push-target"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromLiteral(
                            SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(1)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary(
                            "activity", RuleActivity.ObjectPush)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenSpaceOutput, EffectValueKind.Coordinate)
                    });

            /// <summary>"up to 3 spaces", straight line away from her.</summary>
            private static EffectNodeDefinition Push() =>
                new EffectNodeDefinition(
                    new EffectNodeId("stand-in-push"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticDisplacementCapabilities.PushTargetV3,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "target", new EffectNodeId("stand-in-push-target"),
                            SyntheticRosterCapabilities.ChosenSpaceOutput,
                            EffectValueKind.Coordinate),
                        EffectInputBinding.FromLiteral("distance", EffectValue.Integer(3)),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticExchangeCapabilities.EligiblePieceParameterName,
                            PushEligiblePieceKind.Unit)
                    },
                    outputs: new[] { new EffectOutputBinding("moved", EffectValueKind.Integer) });

            /// <summary>
            /// The borrowed card's own printed Radius icon with rulebook p.16's items - the reading the
            /// printed parenthesis is about, and the one that would be IMPOSSIBLE on Fast as Lightning,
            /// which prints no Radius icon at all.
            /// </summary>
            private static EffectNodeDefinition CardRadius() =>
                new EffectNodeDefinition(
                    new EffectNodeId("stand-in-radius"), EffectNodeKind.Capability,
                    EffectFailurePolicy.Mandatory, SyntheticValueCapabilities.CardRadiusV1,
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer)
                    });

            /// <summary>"in radius" - a selection whose whole reach is the node above.</summary>
            private static EffectNodeDefinition RadiusTarget(string id) =>
                new EffectNodeDefinition(
                    new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticRosterCapabilities.ChooseTargetV2,
                    inputs: new[]
                    {
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                        EffectInputBinding.FromNodeOutput(
                            SyntheticRosterCapabilities.DistanceParameter,
                            new EffectNodeId("stand-in-radius"),
                            SyntheticValueCapabilities.RadiusOutput, EffectValueKind.Integer),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                        EffectInputBinding.FromClosedVocabulary(
                            SyntheticRosterCapabilities.RelationParameter,
                            SourceTeamRelation.OpposingSource),
                        EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticRosterCapabilities.ChosenHeroOutput, EffectValueKind.Hero)
                    });
        }
    }
}
