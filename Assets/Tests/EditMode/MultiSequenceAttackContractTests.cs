using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z27. The MODAL ATTACK contract: "choose one, or both" runs a complete
    /// rulebook p.14 sequence per chosen bullet, and all of them are ONE action.
    ///
    /// <para><b>Why it is its own contract.</b> The only existing shape that produces a second complete
    /// p.14 order is p.10's Repeat, and the Dodger phase-2 worker measured what reusing it would cost:
    /// Enfeeblement prints "cannot repeat actions", PLAN Ruling 14 scopes that to p.10's Repeat keyword
    /// alone, and a choose-both Attack composed as an internal repeat would be shut down by a
    /// restriction its own card never prints. The lane-push timing disagrees too - the designer's
    /// 2026-06-18 ruling makes each repeat instance a separate action, so p.8's mid-turn push resolves
    /// BETWEEN instances, while Ruling 5 makes both sequences one action and puts the push at the end
    /// of all of it. The implementation is shared with p.14 wholesale; the label is not.</para>
    ///
    /// <para><b>Why the content is synthetic.</b> All three real consumers are unlanded - Middlefinger
    /// of Death is on a hero branch, Grim Reaper and Spirit Bear are future heroes - and milestone
    /// 7.9's deferral gate wants the seam proved before a printed face names it. The two fixture cards
    /// differ only in whether they print "(on different targets)", which is what makes Ruling 19's
    /// rider a measured contrast rather than an assertion.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows tier.</para>
    /// </summary>
    public sealed class MultiSequenceAttackContractTests
    {
        private const string ModalCard = "placeholder-red-twinstrike";
        private const string ModalDistinctCard = "placeholder-red-forkstrike";

        // -------------------------------------------------------------------------------------------
        // The shape: one action, N complete p.14 sequences
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ChoosingBothBullets_RunsOneCompleteAttackSequencePerBullet()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);

            var first = session.Projection.PendingAttackTargetChoice;
            Assert.That(first, Is.Not.Null,
                "The modal is answered and the first chosen bullet's own sequence opens its own " +
                "targeting. p.14 runs per bullet, so the page starts again rather than continuing.");
            var firstResolution = first.ResolutionId;

            RunOneSequenceAgainst(session, BlueHeroOf(session));

            var second = session.Projection.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "Ruling 5: the second chosen bullet contributes a second COMPLETE p.14 order, with " +
                "its own targeting rather than a rider on the first.");
            Assert.That(second.ResolutionId, Is.Not.EqualTo(firstResolution),
                "Each sequence runs under an identity of its own, because rulebook p.13's Defeat " +
                "outcome pays out once per identity and two sequences may each defeat a unit.");
        }

        [Test]
        public void EachSequence_TargetsByItsOwnBulletsPrintedSentenceRatherThanTheActionsPolicy()
        {
            var session = StartModalAttack(ModalCard, out var adjacentMinion);
            ChooseBullets(session, both: true);

            var firstBullet = session.Projection.PendingAttackTargetChoice;
            Assert.That(firstBullet.LegalTargetMinionIds, Contains.Item(adjacentMinion),
                "The first bullet prints \"Target a unit adjacent to you\", and a minion beside the " +
                "attacker is a unit.");
            Assert.That(firstBullet.LegalTargetHeroIds, Contains.Item(BlueHeroOf(session)));

            RunOneSequenceAgainst(session, BlueHeroOf(session));

            var secondBullet = session.Projection.PendingAttackTargetChoice;
            Assert.That(secondBullet.LegalTargetMinionIds, Is.Empty,
                "The second bullet prints \"Target a hero adjacent to you\", so the same minion the " +
                "first bullet could take is not a candidate here. The sentence that decides a " +
                "sequence's target set is the BULLET's, not the action's printed policy - which is " +
                "the whole reason the plan carries a policy per bullet.");
            Assert.That(secondBullet.LegalTargetHeroIds, Is.Not.Empty);
        }

        [Test]
        public void ChoosingOneBullet_RunsExactlyOneSequenceAndEndsTheAction()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: false);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null);
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "p.10's modal lets the actor take one option, and one option is one sequence.");
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // Ruling 19's rider: the constraint is the card's, not the contract's
        // -------------------------------------------------------------------------------------------

        [Test]
        public void WithNoPrintedConstraint_TheSameUnitMayBeBothSequencesTarget()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            var hero = BlueHeroOf(session);

            RunOneSequenceAgainst(session, hero);

            var second = session.Projection.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null);
            Assert.That(second.LegalTargetHeroIds, Contains.Item(hero),
                "Ruling 19's rider: a card printing no \"(on different targets)\" may point both " +
                "bullets at ONE unit, provided it still satisfies each bullet's own sentence when " +
                "that bullet resolves. The hero is still adjacent, so it still does.");
        }

        [Test]
        public void WithThePrintedConstraint_TheUnitTheFirstSequenceTookIsGoneFromTheSecondsSet()
        {
            var session = StartModalAttack(ModalDistinctCard, out var adjacentMinion);
            ChooseBullets(session, both: true);
            var hero = BlueHeroOf(session);

            RunOneSequenceAgainst(session, hero);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "\"(on different targets)\" takes the unit the first sequence bound out of the " +
                "second's set. The second bullet takes an adjacent HERO and this board holds one, so " +
                "removing it empties the set and the second sequence simply does not happen - p.10 " +
                "stopping a mandatory step that cannot be carried out.");

            // The contrast is the evidence: same board, same choices, one printed clause apart.
            var unconstrained = StartModalAttack(ModalCard, out _);
            ChooseBullets(unconstrained, both: true);
            RunOneSequenceAgainst(unconstrained, BlueHeroOf(unconstrained));
            Assert.That(unconstrained.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "and the card that does not print the clause is offered the same hero again, which " +
                "is what makes the clause the cause rather than the contract.");
            Assert.That(adjacentMinion.IsDefined, Is.True);
        }

        [Test]
        public void WithThePrintedConstraint_TwoSequencesRunAgainstTwoDifferentUnits()
        {
            var session = StartModalAttack(ModalDistinctCard, out var adjacentMinion);
            ChooseBullets(session, both: true);

            RunOneSequenceAgainst(session, adjacentMinion);

            var second = session.Projection.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "The first sequence took the minion, so the second bullet's hero is untouched by the " +
                "exclusion and its own complete p.14 runs.");
            Assert.That(second.LegalTargetHeroIds, Contains.Item(BlueHeroOf(session)));
        }

        // -------------------------------------------------------------------------------------------
        // Targets are bound per sequence, against the board as it then stands
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ASequenceThatDefeatsTheNextBulletsOnlyCandidate_LeavesTheNextSequenceNothingToTake()
        {
            var session = ConfiguredMatchFixtures.CreateModalSequenceSessionWithoutAnAdjacentMinion();
            StartModalAttackOn(session, ModalCard);
            ChooseBullets(session, both: true);
            var hero = BlueHeroOf(session);

            Accept(session, DirectorCommand.ChooseAttackTarget(hero));
            Accept(session, DirectorCommand.DeclineDefense());

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "The undefended attack succeeded and p.14's step 6 removed the hero, so the second " +
                "bullet - which takes an adjacent hero - reaches its own targeting with nothing to " +
                "take. That is the claim: a sequence binds against the board as it THEN stands, not " +
                "against the board the modal was answered on.");
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null);
        }

        // -------------------------------------------------------------------------------------------
        // One action for every external purpose
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AChooseBothAttack_IsInvisibleToEveryRepeatReaderInThisBuild()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            // This is the exact conflict the Dodger phase-2 worker predicted, stated as the property
            // every repeat reader in this build keys on. Enfeeblement's "cannot repeat actions" -
            // Ruling 14, p.10's Repeat keyword ONLY - can only reach an action that reports itself as
            // a repeat, and PerformedBasicAction.WasRepeatedInstance is where this build says so.
            // A modal Attack's sequences never say it: they run under the action's PRINTED invocation
            // context, no repeat is ever owed, and the frame requests none. So the restriction leaves
            // a choose-both attack whole - which is what its card text says, and what composing the
            // second sequence as an internal p.10 Repeat would have broken.
            var performed = PerformedAttacks(session);
            Assert.That(performed, Has.Length.EqualTo(1),
                "Ruling 5: both sequences are ONE action, so \"you performed an Attack action\" is " +
                "recorded once. Two would also give a repeat restriction a second subject to bite.");
            Assert.That(performed[0].WasRepeatedInstance, Is.False,
                "and the one action is not a repeated instance, which is the flag a repeat " +
                "restriction reads. Nothing about a choose-both attack is spelled 'repeat'.");
            Assert.That(performed[0].Kind, Is.EqualTo(ActionKind.Attack));
        }

        [Test]
        public void AChooseBothAttack_RecordsOneActionPerformedAndOneOutcomePerSequence()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            Assert.That(PerformedAttacks(session), Has.Length.EqualTo(1),
                "Ruling 5 makes the whole modal ONE action, so every after-action ability in the " +
                "roster sees one performed Attack rather than two.");
            Assert.That(CompletedSequences(session), Has.Length.EqualTo(2),
                "and each chosen bullet still reaches an outcome of its own, because each really did " +
                "run a complete p.14 - own targeting, own Defense window, own comparison.");
            Assert.That(
                CompletedSequences(session).Select(value => value.ResolutionId).Distinct().Count(),
                Is.EqualTo(2),
                "under two identities, because p.13's Defeat outcome pays out once per identity.");
        }

        [Test]
        public void APushOwedByTheFirstSequence_ResolvesOnlyOnceTheWholeActionIsOver()
        {
            var session = ConfiguredMatchFixtures
                .CreateModalSequenceSessionOnAnEmptiableBattleZone(out var lastBlueMinion);
            StartModalAttackOn(session, ModalCard);
            ChooseBullets(session, both: true);

            RunOneSequenceAgainst(session, lastBlueMinion);

            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Not.Null,
                "the second sequence is standing, so the action is not over");
            Assert.That(LanePushRequests(session), Is.Zero,
                "p.8's mid-turn push is owed by the action, and Ruling 5 makes both sequences ONE " +
                "action - so it may not fire between them. This is the exact opposite of a repeat, " +
                "where the designer's 2026-06-18 ruling makes each instance a separate action and " +
                "milestone 8.35z21 therefore drains the push in between. Composing choose-both as a " +
                "repeat would have pushed the Lane here.");

            RunOneSequenceAgainst(session, BlueHeroOf(session));

            Assert.That(LanePushRequests(session), Is.EqualTo(1),
                "and once the plan is spent the action really is over, so the push it owed resolves " +
                "at the end of all of it.");
        }

        // -------------------------------------------------------------------------------------------
        // Snapshot, restore and undo treat the chain as one in-flight action
        // -------------------------------------------------------------------------------------------

        [Test]
        public void APauseBetweenSequences_RestoresByteIdenticallyAndOwesTheSameSequence()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            var standing = session.Projection.PendingAttackTargetChoice;
            Assert.That(standing, Is.Not.Null, "the second sequence is the one standing");

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateModalSequenceSession(out _);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "Schema v38 carries the whole plan - the action that opened it, the chosen bullets " +
                "with each one's printed targeting sentence, and the units the earlier sequences " +
                "bound - so a capture taken mid-chain round-trips to the same bytes.");

            var restoredChoice = restored.Projection.PendingAttackTargetChoice;
            Assert.That(restoredChoice, Is.Not.Null);
            Assert.That(restoredChoice.ResolutionId, Is.EqualTo(standing.ResolutionId));
            Assert.That(
                restoredChoice.LegalTargetHeroIds.OrderBy(value => value.Value).ToArray(),
                Is.EqualTo(standing.LegalTargetHeroIds.OrderBy(value => value.Value).ToArray()),
                "and the restored sequence is offered its own bullet's set rather than the action's " +
                "printed one - which is exactly what a restore without the plan could not know.");
            Assert.That(restoredChoice.LegalTargetMinionIds, Is.Empty);
        }

        [Test]
        public void APauseInsideTheSecondSequencesDefenseWindow_RestoresByteIdentically()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));
            Accept(session, DirectorCommand.ChooseAttackTarget(BlueHeroOf(session)));

            Assert.That(session.Projection.PendingDefenseChoice, Is.Not.Null,
                "the second sequence has its own Defense window, which is half of what makes it a " +
                "complete p.14 rather than a rider");

            var payload = session.CaptureSnapshot();
            var restored = ConfiguredMatchFixtures.CreateModalSequenceSession(out _);
            var result = restored.RestoreSnapshot(payload);

            Assert.That(result.Accepted, Is.True, result.Detail);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload));
            Assert.That(restored.Projection.PendingDefenseChoice, Is.Not.Null);
        }

        [Test]
        public void MidChain_TheProjectionReportsOneAttackStillResolvingAndOffersOnlyItsOwnDecision()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            Assert.That(session.Projection.HasPendingAttackResolution, Is.True,
                "This is the reading milestone 8.35f's seat-undo wall is built on: the user's rule " +
                "is \"once an attack target has been selected, that is a hard wall\", and MatchHost " +
                "keeps the wall up until nothing of the p.14 action is still resolving. A modal " +
                "Attack between two of its sequences IS still resolving, so a seat cannot rewind " +
                "through the half of the action it has already committed.");

            var contract = session.Projection.DecisionContract;
            Assert.That(contract, Is.Not.Null);
            Assert.That(contract.FamilyId, Is.EqualTo(PendingDecisionFamilies.AttackTarget),
                "and the only decision the seat is offered is the running sequence's own targeting - " +
                "an ordinary attack-target contract, which is why the per-seat projection needed no " +
                "new family and the wire needed no bump.");
            Assert.That(contract.Commands, Is.Not.Empty);
        }

        [Test]
        public void ARejectedCommandMidChain_RollsBackToTheSameChain()
        {
            var session = StartModalAttack(ModalCard, out _);
            ChooseBullets(session, both: true);
            RunOneSequenceAgainst(session, BlueHeroOf(session));

            var before = session.CaptureSnapshot();
            var standing = session.Projection.PendingAttackTargetChoice;

            // A Defense answered while a target choice is standing is refused, and every refused
            // command is rolled back by deserializing the pre-image - which is why the plan has to be
            // persisted state rather than a value held between commands.
            Assert.That(session.Execute(DirectorCommand.DeclineDefense()).Accepted, Is.False);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(before),
                "A refused command mutates nothing, the modal Attack's plan included.");
            Assert.That(
                session.Projection.PendingAttackTargetChoice.ResolutionId,
                Is.EqualTo(standing.ResolutionId),
                "and the sequence that was standing is still the sequence standing.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixture
        // -------------------------------------------------------------------------------------------

        private static MatchSession StartModalAttack(string cardDefinitionId, out MinionId adjacent)
        {
            var session = ConfiguredMatchFixtures.CreateModalSequenceSession(out adjacent);
            StartModalAttackOn(session, cardDefinitionId);
            return session;
        }

        private static void StartModalAttackOn(MatchSession session, string cardDefinitionId)
        {
            CommitForAttack(session, cardDefinitionId);
            var attack = session.Projection.AvailableTurnActions.Single(offer =>
                offer.InvocationContext == ActionInvocationContext.PrintedPrimary &&
                offer.AttackRange != AttackRangeKind.None);
            Accept(session, DirectorCommand.StartAction(attack.ActionDefinitionId));
            Assert.That(session.Projection.PendingAttackTargetChoice, Is.Null,
                "A modal Attack declares `attack-target@2`: what its step-1 text names is a plan, so " +
                "the action starts straight into p.14's step 1 with nothing to choose yet.");
        }

        /// <summary>
        /// Answers "choose one, or both" - the first bullet alone, or both of them.
        /// </summary>
        private static void ChooseBullets(MatchSession session, bool both)
        {
            var modal = session.Projection.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, "the modal is p.14 step 1's own text");
            Assert.That(modal.MaximumSelections, Is.EqualTo(2),
                "\"choose one, or both\" is the multi arm of p.10's modal: one selection minimum, " +
                "both options maximum.");
            Assert.That(modal.MinimumSelections, Is.EqualTo(1));

            Accept(session, DirectorCommand.AnswerEffectChoice(modal.SelectInvocations[0]));
            if (both)
            {
                var afterFirst = session.Projection.PendingEffectChoice;
                Accept(session, DirectorCommand.AnswerEffectChoice(afterFirst.SelectInvocations[0]));
            }

            var toConfirm = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(toConfirm.ConfirmInvocation));
        }

        /// <summary>
        /// Runs the whole of one sequence's p.14 against the named unit, with the defender playing a
        /// card so the target survives it.
        ///
        /// <para>Surviving matters to the fixture rather than to the rules: both bullets of these
        /// cards can point at the same hero, so a case watching the SECOND sequence needs a first one
        /// that leaves it something to look at. The printed Attack value is 1 for exactly this
        /// reason, and the mid-chain-defeat case declines instead.</para>
        /// </summary>
        private static void RunOneSequenceAgainst(MatchSession session, HeroId target)
        {
            Accept(session, DirectorCommand.ChooseAttackTarget(target));
            AnswerTheDefenseWindow(session);
        }

        private static void RunOneSequenceAgainst(MatchSession session, MinionId target)
        {
            // p.13: "Minions cannot defend. When you target an enemy minion with an attack, it is
            // simply defeated." So there is no Defense window on this arm, and the check below is
            // what says so rather than a comment claiming it.
            Accept(session, DirectorCommand.ChooseAttackTarget(target));
            Assert.That(session.Projection.PendingDefenseChoice, Is.Null);
        }

        private static void AnswerTheDefenseWindow(MatchSession session)
        {
            var defense = session.Projection.PendingDefenseChoice;
            if (defense == null)
            {
                return;
            }

            Accept(
                session,
                defense.LegalDefenseOffers.Count > 0
                    ? DirectorCommand.ChooseDefenseCard(
                        defense.LegalDefenseOffers[0].SourceCardInstanceId)
                    : DirectorCommand.DeclineDefense());
        }

        /// <summary>
        /// Every "you performed a basic Attack action" fact the turn has recorded. This is the surface
        /// after-action abilities and repeat restrictions read, which is why the contract's headline
        /// claim is stated over it rather than over an internal field.
        /// </summary>
        private static PerformedBasicAction[] PerformedAttacks(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value =>
                    value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.BasicActionPerformed &&
                    value.TurnEvent.BasicAction.Kind == ActionKind.Attack)
                .Select(value => value.TurnEvent.BasicAction)
                .ToArray();

        /// <summary>How many mid-turn Lane Pushes rulebook p.8 has been asked for so far.</summary>
        private static int LanePushRequests(MatchSession session) =>
            session.Projection.RoundEvents.Count(value =>
                value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                value.TurnEvent.Kind == TurnResolutionEventKind.ImmediateLanePushRequested);

        /// <summary>Every p.14 order this action ran to an outcome.</summary>
        private static CardActionResolutionEvent[] CompletedSequences(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value =>
                    value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.AttackActionProgressed &&
                    value.TurnEvent.CardActionEvent.Kind ==
                        CardActionResolutionEventKind.AttackResolutionCompleted)
                .Select(value => value.TurnEvent.CardActionEvent)
                .ToArray();

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

        private static HeroId BlueHeroOf(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero =>
                    session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                .Id;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True,
                $"{command.Kind} was refused: {result.RejectionReason}");
        }
    }
}
