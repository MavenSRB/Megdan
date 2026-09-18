using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19a. The four resumable continuations the six-hero roster's printed text needs,
    /// proved on synthetic content that is nobody's card.
    ///
    /// <para><b>What is new here is where a match may stop.</b> Every pause this engine had before this
    /// milestone stood inside an action - a Movement mid-route, an Attack awaiting its Defense, a Skill
    /// mid-node, card text at two of rulebook p.14's three moments. Four printed shapes in the roster
    /// stop somewhere none of those cover: at p.14's step 1, before the Attack Total is announced; at
    /// step 1 again with no target chosen at all, which the text itself then names; at step 4 on the
    /// defender's own side; and outside an action that has not started, while the actor decides which
    /// side of it a rider belongs to.</para>
    ///
    /// <para>The cards are the smallest content that reaches each altitude, and none of them is a
    /// hero's: milestone 7.9's deferral gate requires the seam to work before a verified face names it,
    /// so 8.20-8.24 compose Arien, Brogan, Sabina, Tigerclaw and Xargatha onto machinery already under
    /// test. Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.
    /// </para>
    /// </summary>
    public sealed class ResumableContinuationTests
    {
        private const string BeforeAttackMoveCard = "placeholder-red-tidecaller";
        private const string ChargeCard = "placeholder-red-charger";
        private const string AroundMovementCard = "placeholder-red-charmer";
        private const string DefenderRiderCard = "placeholder-blue-sidestepper";

        // -------------------------------------------------------------------------------------------
        // Arien's shape: rulebook p.14's step 1 stops, and the rest of the page follows the answer
        // -------------------------------------------------------------------------------------------

        [Test]
        public void BeforeAttackText_StopsThePageBeforeTheAttackTotalAndResumesItOnTheAnswer()
        {
            var session = StartAttackWith(BeforeAttackMoveCard, out var target);
            Accept(session, DirectorCommand.ChooseAttackTarget(target));

            var paused = session.Projection.PendingEffectChoice;
            Assert.That(paused, Is.Not.Null,
                "The attacker's \"Before the attack:\" text asks whether to move, so p.14 stops at " +
                "step 1. Before this milestone such a composition was refused at content load.");
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null,
                "p.14 announces the Attack Total at step 2 and opens the Defense response at step 3, " +
                "both of which are on the far side of the question standing at step 1.");
            Assert.That(paused.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));

            Accept(session, DirectorCommand.AnswerEffectChoice(paused.DeclineInvocation));

            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "Declining the text finishes step 1, so the page continues into the Defense response " +
                "exactly as it would have with no text at all.");
        }

        [Test]
        public void BeforeAttackText_AsksItsSecondQuestionAndOnlyThenAnnouncesTheAttack()
        {
            var session = StartAttackWith(BeforeAttackMoveCard, out var target);
            Accept(session, DirectorCommand.ChooseAttackTarget(target));

            var offer = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(offer.ConfirmInvocation));

            var selection = session.Projection.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null,
                "Accepting the offer opens the selection inside it, so one attack holds two questions " +
                "at the same moment of the page.");
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null);
            Assert.That(selection.SelectInvocations, Is.Not.Empty);

            Accept(session, DirectorCommand.AnswerEffectChoice(selection.SelectInvocations[0]));
            AnswerRemainingText(session);

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "The page resumes once the text is finished, however many questions it asked.");
        }

        [Test]
        public void APageStoppedAtStepOne_RestoresByteIdenticallyAndAnswersTheSameQuestion()
        {
            var session = StartAttackWith(BeforeAttackMoveCard, out var target);
            Accept(session, DirectorCommand.ChooseAttackTarget(target));
            var paused = session.Projection.PendingEffectChoice;
            Assert.That(paused, Is.Not.Null);

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateContinuationSession();
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "Schema v25 carries the whole altitude - the frame, the attack it is standing inside, " +
                "and the space its target is standing on - so the restored capture is the same bytes.");

            var restoredChoice = restored.Projection.PendingEffectChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(restoredChoice.ChoiceId, Is.EqualTo(paused.ChoiceId));
            Assert.That(restoredChoice.NodeId, Is.EqualTo(paused.NodeId));

            Accept(restored, DirectorCommand.AnswerEffectChoice(restoredChoice.DeclineInvocation));
            Assert.That(restored.Projection.PendingDefenseChoice, Is.Not.Null,
                "A restored pause owns exactly what the live one owned, including the rest of p.14.");
        }

        [Test]
        public void AStaleAnswerAtStepOne_IsRefusedAndChangesNothing()
        {
            var session = StartAttackWith(BeforeAttackMoveCard, out var target);
            Accept(session, DirectorCommand.ChooseAttackTarget(target));
            var paused = session.Projection.PendingEffectChoice;
            var before = session.CaptureSnapshot();

            var forged = new EffectChoiceCommand(
                paused.ResolutionId,
                new EffectChoiceId("not-the-question-that-is-standing"),
                EffectChoiceCommandKind.Decline,
                default,
                paused.ChooserId);

            Assert.That(session.Execute(DirectorCommand.AnswerEffectChoice(forged)).Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before),
                "A rejected command mutates nothing, at this altitude as at every other.");
            Assert.That(session.Projection.PendingEffectChoice.ChoiceId, Is.EqualTo(paused.ChoiceId),
                "and the question that was standing is still the question standing.");
        }

        // -------------------------------------------------------------------------------------------
        // Brogan's shape: `attack-target@2` - the Attack's own text names what it attacks
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnAttackWhoseMandatoryTextMustBindItsTarget_IsUnavailableWhenNoBindingCanBeCompleted()
        {
            var blocked = ConfiguredMatchFixtures.CreateContinuationSessionWithoutChargeBinding();
            CommitForAttack(blocked, ChargeCard);
            Assert.That(AttackOffer(blocked).IsAvailable, Is.False,
                "A mandatory charge with no legal move-and-target continuation is visibly unavailable.");

            var performable = ConfiguredMatchFixtures.CreateContinuationSession();
            CommitForAttack(performable, ChargeCard);
            Assert.That(AttackOffer(performable).IsAvailable, Is.True,
                "Any complete legal move-and-target continuation keeps the same typed Attack available.");
        }

        [Test]
        public void AnAttackWhoseTextNamesItsTarget_OpensNoTargetChoiceAndBindsOneThroughItsText()
        {
            var session = StartAttackWith(ChargeCard, out _);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "The card charges and attacks what it ends beside, so the set p.14 would offer now is " +
                "the set before the charge. `attack-target@2` says the text names the target instead.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The action starts straight into p.14's step 1.");

            AnswerRemainingText(session);

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "`set-attack-target@1` named a hero, so the page announced the Attack Total against it " +
                "and opened the Defense response.");
            Assert.That(
                session.Projection.PendingDefenseChoice.ChooserId,
                Is.EqualTo(BlueHeroOf(session)),
                "and the hero the text bound is the hero being asked to defend.");
        }

        [Test]
        public void ATargetBoundByCardText_IsRecordedWithTheSpaceItStoodOnBeforeTheConsequences()
        {
            var session = StartAttackWith(ChargeCard, out _);
            AnswerRemainingText(session);

            var pending = session.Projection.PendingDefenseChoice;
            Assert.That(pending, Is.Not.Null);

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateContinuationSession();
            var restoreResult = restored.RestoreSnapshot(payload);
            Assert.That(restoreResult.Accepted, Is.True, restoreResult.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "The pre-consequence target space crosses the wire, so a restored attack knows where " +
                "its target was standing even after p.14 has taken the piece off the board.");
        }

        // -------------------------------------------------------------------------------------------
        // Tigerclaw's shape: the defender's own rider stops p.14 at step 4
        // -------------------------------------------------------------------------------------------

        [Test]
        public void DefenseActionText_StopsThePageOnTheDefendersSideAndResumesTheComparison()
        {
            var session = StartAttackWith(BeforeAttackMoveCard, out var target);
            Accept(session, DirectorCommand.ChooseAttackTarget(target));
            Accept(
                session,
                DirectorCommand.AnswerEffectChoice(
                    session.Projection.PendingEffectChoice.DeclineInvocation));

            var rider = session.Projection.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Value == DefenderRiderCard &&
                card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.ChooseDefenseCard(rider.Id));

            var paused = session.Projection.PendingEffectChoice;
            Assert.That(paused, Is.Not.Null,
                "p.14's step 4 is the defender's own text, and this one asks whether to move.");
            Assert.That(paused.ChooserId, Is.EqualTo(rider.OwnerId),
                "p.10 gives the choices inside a card's text to the hero whose card it is, which at " +
                "step 4 is the defender rather than the attacker.");

            Accept(session, DirectorCommand.AnswerEffectChoice(paused.DeclineInvocation));
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.IsTurnActive, Is.True,
                "and the page ran on to its comparison rather than stopping where the rider did.");
        }

        // -------------------------------------------------------------------------------------------
        // Xargatha's shape: `around-primary-action@1` - the actor places the text either side
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TextAppliedAroundAMovement_AsksItsSideBeforeTheMovementStarts()
        {
            var session = StartPrimaryMovementWith(AroundMovementCard);

            var ordering = session.Projection.PendingEffectChoice;
            Assert.That(ordering, Is.Not.Null,
                "Which side of the Movement the rider runs on is a question about the Movement, so it " +
                "is asked before the allowance is spent rather than during it.");
            Assert.That(ordering.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(ordering.Candidates, Has.Count.EqualTo(2));
            Assert.That(session.Projection.PendingMovementChoice, Is.Null,
                "and the Movement has not started while it stands.");
        }

        [Test]
        public void PlacingTheTextBeforeTheMovement_RunsItAndThenStartsTheMovement()
        {
            var session = StartPrimaryMovementWith(AroundMovementCard);
            var ordering = session.Projection.PendingEffectChoice;

            Accept(session, DirectorCommand.AnswerEffectChoice(ordering.SelectInvocations[0]));
            Accept(
                session,
                DirectorCommand.AnswerEffectChoice(
                    session.Projection.PendingEffectChoice.ConfirmInvocation));

            // The work the ordering question placed is entered next, and it may ask its own questions.
            AnswerRemainingText(session);

            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null,
                "The Movement starts once the work placed before it is done, from the offer the turn " +
                "validated rather than one re-derived after the rider moved a piece.");
        }

        [Test]
        public void PlacingTheTextAfterTheMovement_StartsTheMovementAndParksTheWorkBehindIt()
        {
            var session = StartPrimaryMovementWith(AroundMovementCard);
            var ordering = session.Projection.PendingEffectChoice;

            // Milestone 8.46b: the completion is asked for rather than assumed, and aimed at the choice
            // this answer went into - one admitting exactly one selection is closed by the select itself.
            ConfiguredMatchFixtures.AnswerEffectChoiceSelection(session, ordering.SelectInvocations[1]);

            Assert.That(session.Projection.PendingMovementChoice, Is.Not.Null,
                "The Movement runs first, and the work the actor placed after it waits.");

            var parked = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateContinuationSession();
            Assert.That(restored.RestoreSnapshot(parked).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(parked),
                "A parked continuation coexists with the Movement's own, which is why schema v25 " +
                "writes it beside the card-action continuation rather than as one of its kinds.");

            Accept(session, DirectorCommand.StopMovement());
            AnswerRemainingText(session);

            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "and the work runs when the Movement ends, before the card resolves.");
        }

        // -------------------------------------------------------------------------------------------
        // Arien's Ultimate shape: `before-action-trigger@1` holds the offer it interrupts
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ABeforeActionTrigger_RunsBeforeTheActionAndResumesTheExactValidatedOffer()
        {
            var session = ConfiguredMatchFixtures.CreateContinuationSessionWithUnlockedUltimate();
            CommitForAttack(session, BeforeAttackMoveCard);

            var attack = AttackOffer(session);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));

            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The Ultimate declares that it runs before an Attack action, so the trigger's own " +
                "question stands before the action has started.");
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "and nothing about the Attack has begun while it does.");

            AnswerRemainingText(session);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "The held offer starts the Attack once the trigger is finished, rather than the turn " +
                "re-deriving one against the board the trigger may have changed.");
        }

        [Test]
        public void ABeforeActionTrigger_FiresOncePerTurnAcrossASnapshot()
        {
            var session = ConfiguredMatchFixtures.CreateContinuationSessionWithUnlockedUltimate();
            CommitForAttack(session, BeforeAttackMoveCard);

            var attack = AttackOffer(session);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            var paused = session.Projection.PendingEffectChoice;
            Assert.That(paused, Is.Not.Null);

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateContinuationSession();
            var restoreResult = restored.RestoreSnapshot(payload);
            Assert.That(restoreResult.Accepted, Is.True, restoreResult.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "The frame standing outside the action, the offer it holds, and the trigger the turn " +
                "has spent are all schema v25 fields, so the payload round-trips.");

            AnswerRemainingText(restored);
            Assert.That(restored.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "and the restored turn starts the same Attack the live one would have.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static MatchSession StartAttackWith(string cardDefinitionId, out HeroId target)
        {
            var session = ConfiguredMatchFixtures.CreateContinuationSession();
            CommitForAttack(session, cardDefinitionId);
            var attack = AttackOffer(session);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            target = BlueHeroOf(session);
            return session;
        }

        private static MatchSession StartPrimaryMovementWith(string cardDefinitionId)
        {
            var session = ConfiguredMatchFixtures.CreateContinuationSession();
            CommitForAttack(session, cardDefinitionId);
            var movement = session.Projection.AvailableTurnActions.Single(offer =>
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary);
            Accept(session, DirectorCommand.StartAction(movement.ActionDefinitionId));
            return session;
        }

        /// <summary>
        /// Commits the named Red card and the Blue card with the lowest printed Initiative, so the
        /// acting hero is the one holding the shape under test.
        /// </summary>
        private static void CommitForAttack(MatchSession session, string cardDefinitionId)
        {
            var red = session.Projection.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Value == cardDefinitionId &&
                card.State.Location == CardLocation.Hand);
            var blue = session.Projection.Cards
                .Where(card =>
                    card.State.Location == CardLocation.Hand &&
                    session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == TeamSide.Blue)
                .OrderBy(card => InitiativeOf(session, card))
                .ThenBy(card => card.Id.Value, System.StringComparer.Ordinal)
                .First();

            Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(red.Id),
                "The fixture's Initiative values have to leave the Red card acting first, or these " +
                "cases would be asserting about the other hero's turn.");
        }

        private static int InitiativeOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                .Initiative.Value;

        private static ActionOffer AttackOffer(MatchSession session) =>
            session.Projection.AvailableTurnActions.Single(offer =>
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary &&
                offer.AttackRange != AttackRangeKind.None);

        private static HeroId BlueHeroOf(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero =>
                    session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        /// <summary>
        /// Answers whatever the standing text asks until it stops asking - declining a `may` and taking
        /// the first candidate of a selection. What each case is about is where the pause is, not which
        /// answer is chosen, so the answers are the least interesting ones available.
        /// </summary>
        private static void AnswerRemainingText(MatchSession session)
        {
            for (var guard = 0; guard < 8; guard++)
            {
                var choice = session.Projection.PendingEffectChoice;
                if (choice == null)
                {
                    return;
                }

                if (choice.DeclineInvocation != null)
                {
                    Accept(session, DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation));
                    continue;
                }

                if (choice.SelectInvocations.Count > 0)
                {
                    Accept(session, DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0]));
                    continue;
                }

                Assert.That(choice.ConfirmInvocation, Is.Not.Null,
                    "A standing question offers some way to answer it, or the turn cannot continue.");
                Accept(session, DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation));
            }

            Assert.Fail("The text asked more questions than any composition in this fixture declares.");
        }


        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.RejectionReason + ": " + result.Detail);
        }
    }
}
