using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.71b. Hanu the Trickster's The Ultimate Trick - "You choose the next action, and
    /// how it is performed for a hero you target with the 'Hurry Up!'."
    ///
    /// <para><b>What the cases are about.</b> Four separable things. WHAT the face prints is the
    /// card definition's. WHEN a delegation stands is <see cref="DelegatedActionQuery"/>'s, and
    /// every case there is a board that changes exactly one word of the printed clause - whether
    /// the Ultimate is unlocked, whose card text wrote the rewrite, whether the rewrite still
    /// stands, whether its author is still on the board. WHO MAY ACT is the seat projection's, and
    /// those cases are driven on a real six-seat match rather than asserted on a function. And
    /// whose rewind it is afterwards is the host's.</para>
    ///
    /// <para><b>Why a synthetic unlock rather than a dealt one.</b> Nothing of Hanu's is registered
    /// - milestone 7.9's gate holds his content out of <see cref="MatchContentCatalogs"/> until
    /// 8.71h deals him - so no composed match can put Hurry up! in a hand and offer its Skill. What
    /// the live cases stage instead is the STATE that face leaves behind: his real Ultimate card
    /// beside a dashboard, and milestone 8.71a's own transition
    /// (<c>CardLifecycleAuthority.TrySetPrintedInitiativeOverride</c>) applied to another hero's
    /// unresolved card with his own played card named as its source. That is exactly what the
    /// resolved Skill writes and nothing else, so what the seats then see is what they will see at
    /// 8.71i with no scaffolding at all.</para>
    ///
    /// <para>Unity-independent, so it runs in the cross-platform harness as well as the Windows
    /// tier.</para>
    /// </summary>
    public sealed class HanuUltimateTrickTests
    {
        private const string TestRevision = "hanu-ultimate-trick-test-revision";

        private static readonly HeroId Hanu = new HeroId("hanu-trickster");
        private static readonly HeroId Ally = new HeroId("hanu-blue-ally");
        private static readonly HeroId Enemy = new HeroId("hanu-red-enemy");
        private static readonly HeroId Bystander = new HeroId("hanu-red-bystander");

        private static readonly CardInstanceId HanuCard = new CardInstanceId("hanu-played-card");
        private static readonly CardInstanceId HanuSecondCard =
            new CardInstanceId("hanu-second-played-card");
        private static readonly CardInstanceId AllyCard = new CardInstanceId("ally-played-card");
        private static readonly CardInstanceId EnemyCard = new CardInstanceId("enemy-played-card");
        private static readonly CardInstanceId BystanderCard =
            new CardInstanceId("bystander-played-card");
        private static readonly CardInstanceId UltimateCard = new CardInstanceId("hanu-ultimate-card");

        // -------------------------------------------------------------------------------------------
        // (a) The printed face
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The face, as his shard prints it: Purple Tier IV, NO Initiative, NO Defense, NO Movement,
        /// no Range and no Radius - the one face of his that prints no value at all - and no action,
        /// because p.16's Ultimate is never played.
        /// </summary>
        [Test]
        public void TheFace_PrintsNoValueAndNoActionAtAll()
        {
            var face = TheUltimateTrick();

            Assert.That(face.Id.Value, Is.EqualTo("hanu-the-ultimate-trick"));
            Assert.That(face.DisplayName, Is.EqualTo("The Ultimate Trick"));
            Assert.That(face.ContentAuthority, Is.EqualTo(CardContentAuthority.VerifiedOfficial));
            Assert.That(face.Initiative.IsDefined, Is.False,
                "HA-06: a missing icon is absent, never zero, and this face prints none at all.");
            Assert.That(face.ActionDefinitions, Is.Empty,
                "It is never played, so it prints no primary, no secondary and no Defense.");
            Assert.That(face.Progression.IsUltimate, Is.True);
            Assert.That(face.Progression.NextTierCandidateIds, Is.Empty,
                "p.16 UNLOCKS an Ultimate rather than upgrading into one, so it is on no ladder.");
            Assert.That(face.UltimateRule, Is.Not.Null);
            Assert.That(face.UltimateRule.PrintedValues, Is.Empty);
        }

        /// <summary>
        /// The printed sentence, transcribed - including the card's own spelling of the Silver's
        /// title, which the shard records as the same card.
        /// </summary>
        [Test]
        public void ThePrintedText_IsTranscribedWordForWord()
        {
            Assert.That(
                TheUltimateTrick().SourceContent.PrintedText,
                Is.EqualTo(
                    "You choose the next action, and how it is performed for a hero you target " +
                    "with the \"Hurry Up!\"."));
        }

        /// <summary>
        /// The whole of its content is a standing declaration: no rule modification, no triggered
        /// effect, no checkpoint, no Minion-Battle rule and no before-action or after-push trigger.
        /// It changes WHO CHOOSES, which none of <c>RuleModification</c>'s five families can say.
        /// </summary>
        [Test]
        public void TheAbility_IsAStandingDeclarationAndNothingElse()
        {
            var rule = TheUltimateTrick().UltimateRule;

            Assert.That(rule.RuleId, Is.EqualTo("hanu-the-ultimate-trick"));
            Assert.That(rule.Modifications, Is.Empty,
                "Ursafar's Unbound Fury shape: a declaration other rules read by name.");
            Assert.That(rule.TriggeredEffect.HasValue, Is.False);
            Assert.That(rule.TriggerCheckpoints, Is.Empty);
            Assert.That(rule.TriggerCondition, Is.EqualTo(UltimateTriggerCondition.None));
            Assert.That(rule.BeforeAction.IsDeclared, Is.False);
            Assert.That(rule.AfterPush.IsDeclared, Is.False);
            Assert.That(rule.MinionBattleRule, Is.Null);
            Assert.That(rule.TextComposition.IsAwaited, Is.False,
                "It owes no later milestone a composition: this block composes it whole.");
        }

        /// <summary>
        /// The rule kind is spelled on the content and in the engine rule that reads it, and the two
        /// are the same string - the discipline <c>BorrowedPrimaryAction</c> already follows for the
        /// other standing declaration a rule reads by name.
        /// </summary>
        [Test]
        public void TheRuleKind_IsTheOneTheDelegationMatchesOn()
        {
            Assert.That(
                HanuEffectContent.TheUltimateTrickRuleKind,
                Is.EqualTo(DelegatedActionQuery.DelegatingRuleKind));
            Assert.That(
                TheUltimateTrick().UltimateRule.RuleKind,
                Is.EqualTo(DelegatedActionQuery.DelegatingRuleKind),
                "A rule kind nothing matched on would be an Ultimate that does nothing.");
        }

        // -------------------------------------------------------------------------------------------
        // (b) When a delegation stands - the rule itself
        // -------------------------------------------------------------------------------------------

        [Test]
        public void WhileTheUltimateIsLocked_HurryUpDelegatesNothing()
        {
            var board = Board.Staged(unlocked: false);
            board.Rewrite(EnemyCard, HanuCard);

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False,
                "Before p.16 unlocks it, Hurry up! rewrites the Initiative and nothing more.");
            Assert.That(
                board.Card(EnemyCard).InitiativeOverride.IsDefined, Is.True,
                "and the rewrite itself is untouched by the Ultimate's absence.");
        }

        [Test]
        public void ARewrittenEnemy_HasTheirNextActionDelegatedToTheRewriter()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);

            var delegation = board.DelegationFor(Enemy);

            Assert.That(delegation.IsDefined, Is.True);
            Assert.That(delegation.DelegatedHeroId, Is.EqualTo(Enemy));
            Assert.That(delegation.DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(delegation.DelegatedCardInstanceId, Is.EqualTo(EnemyCard));
            Assert.That(delegation.DecidingCardInstanceId, Is.EqualTo(HanuCard));
            Assert.That(delegation.Delegates(Enemy), Is.True);
            Assert.That(delegation.Delegates(Ally), Is.False);
        }

        [Test]
        public void ARewrittenFRIEND_IsDelegatedOnExactlyTheSameTerms()
        {
            var board = Board.Staged();
            board.Rewrite(AllyCard, HanuCard);

            var delegation = board.DelegationFor(Ally);

            Assert.That(delegation.IsDefined, Is.True,
                "HA-Q3: it applies to any hero Hanu targets with Hurry up!, friend or enemy - the " +
                "printed sentence names no side, and neither does the Silver it rides.");
            Assert.That(delegation.DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False,
                "and a hero nobody rewrote is delegated to nobody.");
        }

        [Test]
        public void TwoRewrittenHeroes_AreTwoSeparateDelegationsAndNeverOneStack()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);
            board.Rewrite(AllyCard, HanuCard);

            Assert.That(board.DelegationFor(Enemy).DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(board.DelegationFor(Ally).DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(board.DelegationFor(Bystander).IsDefined, Is.False);
        }

        /// <summary>
        /// HA-Q4's no-stacking rule, which the rewrite already states: the printed sentence SETS
        /// rather than adds, so a second Hurry up! on the same hero before they act records the
        /// newest source and leaves one delegation behind it.
        /// </summary>
        [Test]
        public void ASecondHurryUpOnTheSameHero_ReArmsTheDelegationRatherThanDoublingIt()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);
            board.Rewrite(EnemyCard, HanuSecondCard);

            var delegation = board.DelegationFor(Enemy);

            Assert.That(delegation.IsDefined, Is.True);
            Assert.That(delegation.DecidingHeroId, Is.EqualTo(Hanu));
            Assert.That(delegation.DecidingCardInstanceId, Is.EqualTo(HanuSecondCard),
                "One rewrite per card, so one delegation per card, and the newest source is the " +
                "one it names.");
        }

        /// <summary>
        /// The four exits HA-Q4 names, each reached the way the match reaches it: the action
        /// completing, p.13's discard of a defeated hero's unresolved card, a retrieval, and a turn
        /// to face down. They are one case because they are one rule - the rewrite ends at the first
        /// state change of the card it was written on, and the delegation is that rewrite.
        /// </summary>
        [Test]
        public void TheDelegationEndsWithTheRewriteAtEveryOneOfItsExits()
        {
            AssertLapses(
                "the delegated action completes - the turn authority resolves the card after the " +
                "whole action, so the delegation covers exactly 'the next action'",
                card => CardLifecycleAuthority.TryResolveAfterAction(card));
            AssertLapses(
                "the card is retrieved to its owner's hand while its own action runs",
                card => CardLifecycleAuthority.TryRetrieveResolvingCard(card));
            AssertLapses(
                "the hero is defeated and p.13 puts their card in the discard pile",
                card => CardLifecycleAuthority.TryResolveAfterAction(card) &&
                    CardLifecycleAuthority.TryDiscardResolvedCard(card));
            AssertLapses(
                "the round ends and p.8 retrieves the card",
                card => CardLifecycleAuthority.TryResolveAfterAction(card) &&
                    CardLifecycleAuthority.TryRetrieveEligibleCard(card));
        }

        /// <summary>
        /// HA-Q13c. p.16 gives an Ultimate its ability "as long as your hero is on the board", so a
        /// defeated Hanu decides nothing - and the rewrite he already wrote still stands, because
        /// p.10 ends that at the rewritten CARD's own state change and at nothing else. The targeted
        /// player takes their own action back, at Initiative 11.
        /// </summary>
        [Test]
        public void ADefeatedRewriter_DecidesNothingWhileTheRewriteStands()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);
            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.True);

            board.RemoveFromBoard(Hanu);

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False);
            Assert.That(board.Card(EnemyCard).InitiativeOverride.Value, Is.EqualTo(11),
                "The rewrite is a fact about the targeted hero's CARD and outlives its author.");
        }

        [Test]
        public void AnUltimateDeclaringSomeOtherRule_DelegatesNothing()
        {
            var board = Board.Staged(ruleKind: "SomeOtherUltimatesDeclaration");
            board.Rewrite(EnemyCard, HanuCard);

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False,
                "The reader matches one declared rule kind, so no other Ultimate in the build can " +
                "acquire this ability by standing beside a dashboard.");
        }

        /// <summary>
        /// HA-Q13b. What the delegation matches is a rewrite whose SOURCE CARD belongs to a hero
        /// whose unlocked Ultimate declares the rule kind - never the rewriting card's definition.
        /// A rewrite by a hero with no such Ultimate is the plain Silver and nothing more.
        /// </summary>
        [Test]
        public void ARewriteByAHeroWithNoSuchUltimate_IsTheSilverAndNothingMore()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, AllyCard);

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False);
            Assert.That(board.Card(EnemyCard).InitiativeOverride.IsDefined, Is.True);
        }

        [Test]
        public void HanusOwnCard_IsNeverDelegatedToHimself()
        {
            var board = Board.Staged();
            board.Rewrite(HanuSecondCard, HanuCard);

            Assert.That(board.DelegationFor(Hanu).IsDefined, Is.False,
                "p.10 keeps Hanu out of his own selection, and a delegation of his own action to " +
                "his own seat would be a sentence that said nothing.");
        }

        /// <summary>
        /// Two derivations of one collection agree, whatever order the collection arrives in. Every
        /// seat, the host, a restore, a replay and a rewind each derive this separately, so an
        /// answer that depended on list order would be six answers.
        /// </summary>
        [Test]
        public void TheDerivation_IsIndependentOfTheOrderTheCardsArriveIn()
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);

            var forward = board.DelegationFor(Enemy);
            var reversed = DelegatedActionQuery.ForActor(
                board.Definitions.Reverse().ToArray(),
                board.Instances.Reverse().ToArray(),
                board.Occupancies.Reverse().ToArray(),
                Enemy);

            Assert.That(reversed, Is.EqualTo(forward));
        }

        [Test]
        public void AnUndefinedActorAndABoardWithNoRewriteAtAll_AreAnsweredWithNone()
        {
            var board = Board.Staged();

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False);
            Assert.That(
                DelegatedActionQuery.ForActor(
                    board.Definitions, board.Instances, board.Occupancies, default).IsDefined,
                Is.False);
            Assert.That(
                DelegatedActionQuery.DecidingHeroFor(
                    board.Definitions, board.Instances, board.Occupancies, Enemy),
                Is.EqualTo(Enemy),
                "With nothing delegated, the hero who acts is the hero whose action it is.");
        }

        // -------------------------------------------------------------------------------------------
        // (c) Who may act - the seat projection, on a driven six-seat match
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheDecidingSeatIsOfferedTheWholeActionAndTheDelegatedSeatIsOfferedNothing()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);

            var deciding = match.ProjectionFor(match.Decider);
            var delegated = match.ProjectionFor(match.Delegated);

            Assert.That(delegated.AuthorizedCommands, Is.Empty,
                "THE WHOLE SENTENCE AT THIS BOUNDARY. The hero acting may submit nothing: " +
                "'You choose the next action, and how it is performed'.");
            Assert.That(delegated.Pending.TurnActions, Is.Empty,
                "and the offers are absent rather than present and refused, so no surface can " +
                "draw a control that the host would reject.");
            Assert.That(delegated.Pending.HoldOffer, Is.Null);

            Assert.That(deciding.AuthorizedCommands, Is.Not.Empty);
            Assert.That(
                deciding.AuthorizedCommands.All(command =>
                    command.Kind == DirectorCommandKind.StartAction ||
                    command.Kind == DirectorCommandKind.PerformHold ||
                    command.Kind == DirectorCommandKind.PerformFastTravel ||
                    command.Kind == DirectorCommandKind.PerformClear),
                Is.True,
                "HA-Q3: exactly the offers that hero's own seat would have had - the card's " +
                "primary and secondary, p.12's Hold, and p.11's Fast Travel and p.13's Clear " +
                "replacements.");
            Assert.That(
                deciding.AuthorizedCommands.Any(command =>
                    command.Kind == DirectorCommandKind.StartAction &&
                    command.CardInstanceId.Equals(match.DelegatedCardId)),
                Is.True,
                "and they are started off the DELEGATED hero's own card.");
            Assert.That(deciding.Pending.TurnActions, Is.Not.Empty);
        }

        [Test]
        public void NoOtherSeatAtTheTableIsOfferedTheDelegatedAction()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);

            foreach (var seat in match.Host.Seats.Seats)
            {
                var offered = match.Host.ProjectionForSeat(seat.Id).AuthorizedCommands;
                Assert.That(offered.Count > 0, Is.EqualTo(seat.Owns(match.Decider)),
                    "Seat " + seat.Id + ": exactly one seat holds a delegated action, and it is " +
                    "the deciding one.");
            }
        }

        /// <summary>
        /// A friendly delegation is the same rule with one word changed, and it is asserted
        /// separately because a reading that only worked against an enemy would be a different card.
        /// </summary>
        [Test]
        public void AFriendlyHerosAction_IsDelegatedOnTheSameTerms()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: false);

            Assert.That(
                match.Host.Seats.FindByHero(match.Delegated).TeamSide,
                Is.EqualTo(match.Host.Seats.FindByHero(match.Decider).TeamSide),
                "This case measures nothing unless the two are on one side.");
            Assert.That(match.ProjectionFor(match.Delegated).AuthorizedCommands, Is.Empty);
            Assert.That(match.ProjectionFor(match.Decider).AuthorizedCommands, Is.Not.Empty);
        }

        /// <summary>
        /// Every choice inside the action is the deciding seat's: the pause that opens when the
        /// delegated action starts is owned by Hanu, carries its answers to his seat alone, and the
        /// hero whose action it is is handed the contract no more than any other spectator.
        /// </summary>
        [Test]
        public void EveryChoiceInsideTheDelegatedAction_IsOwnedAndAnsweredByTheDecidingSeat()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            var opened = match.StartTheDelegatedAction();

            Assert.That(opened, Is.True,
                "The delegated action must open a pause for this case to measure one.");

            var owner = SeatProjectionFactory.OwnerOfStandingDecision(match.Host.Projection);
            Assert.That(owner.IsDefined, Is.True);
            Assert.That(owner.HeroIds, Is.EqualTo(new[] { match.Decider }),
                "The authority is waiting on Hanu for a choice inside another hero's action.");
            Assert.That(
                SeatProjectionFactory.DelegatedDecisionFamilies, Has.Member(owner.FamilyId),
                "and the family it re-homed is one the block classified rather than one it " +
                "happened to catch.");

            Assert.That(match.ProjectionFor(match.Decider).DecisionContract, Is.Not.Null);
            Assert.That(match.ProjectionFor(match.Delegated).DecisionContract, Is.Null);
            Assert.That(match.ProjectionFor(match.Delegated).AuthorizedCommands, Is.Empty);
            Assert.That(match.ProjectionFor(match.Delegated).IsWaitingForAnotherSeat, Is.True,
                "and the delegated player's own screen says so, which is what keeps a table that " +
                "has stopped from looking broken.");
        }

        /// <summary>
        /// The whole table still learns who is being waited on. It is the same publication milestone
        /// 8.30a makes of every other decision - and under a delegation the answer is a hero whose
        /// turn it is not, which is exactly the fact 8.71h's narration has to say out loud.
        /// </summary>
        [Test]
        public void EverySeatLearnsThatHanuIsDecidingAndWhoseActionItIs()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            match.StartTheDelegatedAction();

            foreach (var seat in match.Host.Seats.Seats)
            {
                var view = match.Host.ProjectionForSeat(seat.Id);
                Assert.That(view.DecisionOwner.HeroIds, Has.Member(match.Decider),
                    "Seat " + seat.Id + " must be able to say who the table is waiting for.");
                Assert.That(view.SelectedActor.ActorId, Is.EqualTo(match.Delegated),
                    "and whose action it is stays public on the selected actor.");
                Assert.That(
                    DelegatedActionQuery.ForActor(
                        view.Definitions, view.Cards, view.Heroes, view.SelectedActor.ActorId),
                    Is.EqualTo(SeatProjectionFactory.DelegationOf(match.Host.Projection)),
                    "THE FACT 8.71h's PRESENTATION READS. Every seat derives the host's own " +
                    "delegation from its own projection - the rewritten card, the rewriting card " +
                    "and the unlocked Ultimate are all in one of p.4's open zones - so the HUD can " +
                    "name Hanu as the decider of this hero's action without one new wire field.");
            }
        }

        /// <summary>
        /// A hero's Defense against somebody else's attack is never the action they are performing,
        /// so it is not delegated - the user's own exception in HA-Q3, asserted as the classification
        /// that implements it rather than waited for on a board.
        /// </summary>
        [Test]
        public void EveryPendingFamilyIsClassified_AndTheDefensiveOnesAreNeverDelegated()
        {
            var declared = typeof(PendingDecisionFamilies)
                .GetFields(System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Static)
                .Where(field => field.FieldType == typeof(PendingDecisionFamilyId))
                .Select(field => (PendingDecisionFamilyId)field.GetValue(null))
                .ToArray();

            Assert.That(declared, Is.Not.Empty);
            Assert.That(
                SeatProjectionFactory.DelegatedDecisionFamilies
                    .Concat(SeatProjectionFactory.UndelegatedDecisionFamilies)
                    .ToArray(),
                Is.EquivalentTo(declared),
                "A family nobody classified would join the delegation by accident. Every member " +
                "of PendingDecisionFamilies is named on one of the two lists and on one only.");
            Assert.That(
                SeatProjectionFactory.DelegatedDecisionFamilies
                    .Intersect(SeatProjectionFactory.UndelegatedDecisionFamilies),
                Is.Empty);

            foreach (var family in new[]
            {
                PendingDecisionFamilies.Defense,
                PendingDecisionFamilies.HeroRespawn,
                PendingDecisionFamilies.RemovalPrevention,
                PendingDecisionFamilies.TurnRelease,
                PendingDecisionFamilies.SystemTransition
            })
            {
                Assert.That(
                    SeatProjectionFactory.UndelegatedDecisionFamilies, Has.Member(family),
                    "HA-Q3: a hero's Defense responses, their respawn choice and their " +
                    "end-of-round choices are not 'the action' and stay theirs - and " +
                    family.Value + " is one of them.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // (d) Bots
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A lobby bot holding Hanu answers the delegated action, and a bot holding the delegated
        /// hero does not act during it. Both follow from the authorization change alone, because the
        /// seat conductor plays out of <c>SeatProjection.AuthorizedCommands</c> and nothing else -
        /// which is the property this case is really asserting.
        /// </summary>
        [Test]
        public void ABotOnTheDecidingSeatPlaysTheDelegatedActionAndABotOnTheDelegatedSeatDoesNot()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            var delegatedSeat = match.Host.Seats.FindByHero(match.Delegated).Id;
            var decidingSeat = match.Host.Seats.FindByHero(match.Decider).Id;

            var idle = new BotSeatConductor(match.Host, new[] { delegatedSeat }, 4);
            Assert.That(idle.TryPlayOne(out _), Is.False,
                "The hero whose action it is has nothing to answer, so its bot waits - exactly as " +
                "it waits on any other seat's turn.");

            var playing = new BotSeatConductor(match.Host, new[] { decidingSeat }, 4);
            Assert.That(playing.TryPlayOne(out var action), Is.True);
            Assert.That(action.Accepted, Is.True, "A bot holding Hanu answers for the other hero.");
            Assert.That(match.Host.Projection.AcceptedCommands.Count,
                Is.GreaterThan(match.CommandsBeforeTheDelegatedTurn));
        }

        // -------------------------------------------------------------------------------------------
        // (e) The rewind
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// HA-Q4's Undo rule: the delegated decisions are HANU's seat's decisions. They rewind under
        /// his watermark, and the targeted seat cannot rewind them.
        /// </summary>
        [Test]
        public void TheDelegatedTurnsRewindBelongsToTheDecidingSeatAndNeverToTheDelegatedOne()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: false);
            match.StartTheDelegatedAction();

            var refused = match.Host.SubmitUndoForSeat(
                match.Host.Seats.FindByHero(match.Delegated).Id,
                new SeatUndoSubmission(
                    new SubmissionId("delegated-seat-undo"),
                    match.ProjectionFor(match.Delegated).Version));
            Assert.That(refused.Accepted, Is.False,
                "A seat that submitted nothing may not step back through what another player " +
                "sent on its behalf.");
            Assert.That(match.ProjectionFor(match.Delegated).IsSeatUndoAvailable, Is.False,
                "and its own projection never offered the control.");

            Assert.That(match.ProjectionFor(match.Decider).IsSeatUndoAvailable, Is.True,
                "A FRIENDLY delegation touches nothing an opponent owns, so 8.42e's interaction " +
                "wall stays down and the deciding seat may take its own press back.");
            Assert.That(
                match.Host.SubmitUndoForSeat(
                    match.Host.Seats.FindByHero(match.Decider).Id,
                    new SeatUndoSubmission(
                        new SubmissionId("deciding-seat-undo"),
                        match.ProjectionFor(match.Decider).Version)).Accepted,
                Is.True);
        }

        /// <summary>
        /// And the wall it does NOT relax: a delegated ENEMY hero moving is a change to an
        /// opponent's own piece, watched by the player it belongs to, so the rewind stops at it
        /// exactly as it stops at every other interaction (8.42e; HA-Q4's last clause).
        /// </summary>
        [Test]
        public void ADelegatedENEMYAction_WallsTheRewindAsAnyOtherInteractionDoes()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            match.DriveTheDelegatedActionToItsEnd();

            Assert.That(match.ProjectionFor(match.Decider).IsSeatUndoAvailable, Is.False,
                "The delegated hero's own player has already watched their hero act.");
            Assert.That(match.ProjectionFor(match.Delegated).IsSeatUndoAvailable, Is.False);
        }

        // -------------------------------------------------------------------------------------------
        // (f) Snapshot, the deferral, the labels and the revision
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The delegation survives capture and restore because there is nothing of it to survive:
        /// what is persisted is schema v44's rewrite, and the ability is re-read off the card beside
        /// the dashboard. Asserted as an equality between the live answer and the restored one.
        /// </summary>
        [Test]
        public void TheDelegationIsTheSameAfterASnapshotRoundTrip()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            var before = SeatProjectionFactory.DelegationOf(match.Host.Projection);
            Assert.That(before.IsDefined, Is.True);

            var payload = match.Host.Projection.AuthoritativeSnapshot;
            var restored = AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                payload,
                match.Definitions,
                new[] { match.Map },
                match.Host.Projection.Setup.Minions);

            Assert.That(
                DelegatedActionQuery.ForActor(
                    match.Definitions,
                    restored.Cards,
                    restored.Board.Occupancies,
                    before.DelegatedHeroId),
                Is.EqualTo(before),
                "Nothing of the delegation is stored, so a restore supplies it again from the " +
                "rewrite it persisted and the Ultimate still beside the dashboard - which is why " +
                "neither schema v44 nor wire guards-private-online/20 moved for this milestone.");
        }

        /// <summary>
        /// THE SHIPPED DEFECT this block found, and it was milestone 8.71a's: the read model's copy
        /// of a card dropped its rewritten printed Initiative, so the rewrite reached the authority,
        /// the turn order and the snapshot and reached nothing that reads a projection.
        /// </summary>
        /// <remarks>
        /// Three readers were answering about a board that did not exist - the seat projection and
        /// the wire block 8.71a wrote for it, the hero rail's printed-Initiative badge, and
        /// <c>MatchHost.OpposingFingerprint</c>'s own 8.71a term, which is the whole of 8.42e's
        /// interaction wall for a Hurry up! on an enemy. This milestone needed the first of them
        /// anyway, because a delegation is derived from exactly that field.
        /// </remarks>
        [Test]
        public void TheProjectionsCopyOfARewrittenCard_CarriesTheRewriteToEverySeatAndTheWire()
        {
            var match = DelegatedMatch.Stage(againstAnEnemy: true);
            var rewrittenId = SeatProjectionFactory.DelegationOf(match.Host.Projection)
                .DelegatedCardInstanceId;

            var director = match.Host.Projection.Cards.Single(card => card.Id.Equals(rewrittenId));
            Assert.That(director.InitiativeOverride.IsDefined, Is.True,
                "A COPY of a card that is not a copy of that card is the defect, and every card " +
                "fact this read model ever gains belongs on the same line.");
            Assert.That(director.InitiativeOverride.Value, Is.EqualTo(11));

            foreach (var seat in match.Host.Seats.Seats)
            {
                var view = match.Host.ProjectionForSeat(seat.Id);
                var card = view.Cards.SingleOrDefault(value => value.Id.Equals(rewrittenId));
                Assert.That(card, Is.Not.Null,
                    "Seat " + seat.Id + ": a played, revealed card is in p.4's open turn area.");
                Assert.That(card.InitiativeOverride.Value, Is.EqualTo(11));

                Assert.That(
                    NetworkSeatProjectionCodec.TryDecode(
                        NetworkSeatProjectionCodec.Encode(view), out var record),
                    Is.True);
                var row = record.OwnCards.Concat(record.PublicCards)
                    .Single(value => value.InstanceId == rewrittenId.Value);
                Assert.That(row.RewrittenInitiative, Is.EqualTo(11),
                    "and wire guards-private-online/20's own rewritten-Initiative block, which " +
                    "was written at 8.71a and was empty in every frame of every match until now.");
            }
        }

        /// <summary>
        /// Milestone 7.9's gate, asserted from BOTH sides. This block declares no capability at all,
        /// so both lists are empty and no catalog this build composes registers anything of its own.
        /// </summary>
        [Test]
        public void ThisBlockDeclaresNoCapabilityOnEitherSide()
        {
            Assert.That(HanuComponentContent.UltimateDeferredHandlers(), Is.Empty,
                "The Ultimate composes no effect body, so there is no handler to defer.");
            Assert.That(HanuComponentContent.UltimateRegisteredHandlers(), Is.Empty);
            Assert.That(
                HanuCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "8.71g stated his set; nothing of Hanu's is REGISTERED until 8.71h deals him.");
            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities, Is.Not.Null,
                "and the build's own catalog is composed without this block contributing to it.");
        }

        /// <summary>
        /// The decision labels this block owes are DERIVED from the composed face rather than
        /// declared by guess, which is the measurement 8.69f's ten-of-nineteen miss cost.
        /// </summary>
        [Test]
        public void TheDecisionLabels_AreDerivedAndThisFaceRaisesNone()
        {
            // MERGE NOTE (8.71 wave): derived over THIS block's face, not the integrated aggregate.
            var required = DecisionLabelRequirementQuery
                .For(HanuCardContent.UltimateDefinitions().ToArray())
                .Select(requirement => requirement.NodeId.Value)
                .ToArray();

            Assert.That(required, Is.Empty,
                "The face runs no effect, so it raises no control of its own: every control a " +
                "delegated action draws belongs to the delegated CARD.");
            Assert.That(HanuEffectContent.UltimateDecisionLabels(), Is.Empty);
        }

        /// <summary>
        /// The one face this block composes is one of his nineteen, under the set's one real revision
        /// since milestone 8.71g replaced this block's placeholder "hanu-8-71b-prep".
        /// </summary>
        [Test]
        public void TheComposedSet_IsOneFaceUnderTheSetsRevision()
        {
            // MERGE NOTE (8.71 wave): the integrated aggregate holds every landed block's faces, so
            // this block's one face is asserted on its own list and by containment in the aggregate.
            Assert.That(HanuCardContent.UltimateDefinitions().Count(), Is.EqualTo(1));
            Assert.That(HanuCardContent.AllDefinitions.Select(face => face.Id.Value),
                Does.Contain("hanu-the-ultimate-trick"));
            Assert.That(HanuCardContent.DealtDefinitions, Has.Count.EqualTo(18),
                "and the Ultimate is one of the eighteen: p.16 puts it in his DECK.");
            Assert.That(
                HanuCardContent.UltimateDefinitions()
                    .Select(face => face.Reference.ContentRevision).Distinct().ToArray(),
                Is.EqualTo(new[] { HanuCardContent.ContentRevision }),
                "one string across his whole set, which `HanuPrintedSetTests` asserts from both ends.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static CardDefinition TheUltimateTrick() =>
            HanuCardContent.AllDefinitions.Single(
                definition => definition.Id.Equals(HanuCardContent.TheUltimateTrick));

        private static void AssertLapses(string what, Func<CardInstance, bool> exit)
        {
            var board = Board.Staged();
            board.Rewrite(EnemyCard, HanuCard);
            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.True, what);

            Assert.That(exit(board.Card(EnemyCard)), Is.True, what);

            Assert.That(board.DelegationFor(Enemy).IsDefined, Is.False,
                "The delegation ends when the rewrite does, and for the same reason: " + what);
        }

        /// <summary>
        /// A staged board of cards and heroes, with no match around it: the smallest thing
        /// <see cref="DelegatedActionQuery"/> is a function of.
        /// </summary>
        private sealed class Board
        {
            private readonly List<CardDefinition> definitions = new List<CardDefinition>();
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly List<HeroOccupancy> occupancies = new List<HeroOccupancy>();

            private Board(bool unlocked, string ruleKind)
            {
                var plain = Face("hanu-ultimate-test-plain");
                var ultimate = Ultimate(ruleKind);
                definitions.Add(plain);
                definitions.Add(ultimate);

                instances.Add(Played(HanuCard, Hanu, plain));
                instances.Add(Played(HanuSecondCard, Hanu, plain));
                instances.Add(Played(AllyCard, Ally, plain));
                instances.Add(Played(EnemyCard, Enemy, plain));
                instances.Add(Played(BystanderCard, Bystander, plain));
                instances.Add(new CardInstance(
                    UltimateCard,
                    ultimate.Reference,
                    Hanu,
                    unlocked
                        ? new CardStateFacts(
                            CardLocation.UltimateArea, false, CardResolutionStatus.None,
                            CardFaceOrientation.FaceUp)
                        : CardStateFacts.InitialInDeck()));

                occupancies.Add(new HeroOccupancy(Hanu, new HexCoordinate(0, 0)));
                occupancies.Add(new HeroOccupancy(Ally, new HexCoordinate(1, 0)));
                occupancies.Add(new HeroOccupancy(Enemy, new HexCoordinate(2, 0)));
                occupancies.Add(new HeroOccupancy(Bystander, new HexCoordinate(3, 0)));
            }

            internal static Board Staged(
                bool unlocked = true, string ruleKind = HanuEffectContent.TheUltimateTrickRuleKind) =>
                new Board(unlocked, ruleKind);

            internal IReadOnlyList<CardDefinition> Definitions => definitions;
            internal IReadOnlyList<CardInstance> Instances => instances;
            internal IReadOnlyList<HeroOccupancy> Occupancies => occupancies;

            internal CardInstance Card(CardInstanceId id) =>
                instances.Single(card => card.Id.Equals(id));

            internal void Rewrite(CardInstanceId rewritten, CardInstanceId source) =>
                Assert.That(
                    CardLifecycleAuthority.TrySetPrintedInitiativeOverride(Card(rewritten), 11, source),
                    Is.True,
                    "The staged rewrite is milestone 8.71a's own transition, not a field poke.");

            internal void RemoveFromBoard(HeroId heroId) =>
                occupancies.RemoveAll(occupancy => occupancy.HeroId.Equals(heroId));

            internal DelegatedAction DelegationFor(HeroId actorId) =>
                DelegatedActionQuery.ForActor(definitions, instances, occupancies, actorId);

            private static CardInstance Played(
                CardInstanceId id, HeroId owner, CardDefinition definition) =>
                new CardInstance(
                    id,
                    definition.Reference,
                    owner,
                    new CardStateFacts(
                        CardLocation.TurnArea, true, CardResolutionStatus.Unresolved,
                        CardFaceOrientation.FaceUp));

            private static CardDefinition Face(string id) => new CardDefinition(
                new CardDefinitionId(id),
                TestRevision,
                id,
                new InitiativeValue(9),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Gold),
                new List<ActionDefinition>
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-movement"),
                        ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                },
                null,
                CardSourceContent.NotSourced);

            private static CardDefinition Ultimate(string ruleKind) =>
                string.Equals(ruleKind, HanuEffectContent.TheUltimateTrickRuleKind,
                    StringComparison.Ordinal)
                    ? TheUltimateTrick()
                    : new CardDefinition(
                        new CardDefinitionId("hanu-ultimate-test-other"),
                        TestRevision,
                        "another Ultimate",
                        default,
                        CardContentAuthority.SyntheticTestFixture,
                        CardProgressionProfile.UltimateCard(),
                        Array.Empty<ActionDefinition>(),
                        UltimateRuleDefinition.PermanentDeclaration(
                            "hanu-ultimate-test-other", ruleKind),
                        CardSourceContent.NotSourced);
        }

        /// <summary>
        /// A real six-seat match driven to the moment a rewritten hero is the selected actor, with
        /// The Ultimate Trick itself beside its controller's dashboard.
        /// </summary>
        private sealed class DelegatedMatch
        {
            private readonly MatchSession session;

            private DelegatedMatch(
                MatchSession session,
                IMatchHost host,
                IReadOnlyList<CardDefinition> definitions,
                BoardMapDefinition map)
            {
                this.session = session;
                Host = host;
                Definitions = definitions;
                Map = map;
            }

            internal IMatchHost Host { get; }
            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal BoardMapDefinition Map { get; }
            internal HeroId Decider { get; private set; }
            internal HeroId Delegated { get; private set; }
            internal CardInstanceId DelegatedCardId { get; private set; }
            internal int CommandsBeforeTheDelegatedTurn { get; private set; }

            internal SeatProjection ProjectionFor(HeroId heroId) =>
                Host.ProjectionForSeat(Host.Seats.FindByHero(heroId).Id);

            internal static DelegatedMatch Stage(bool againstAnEnemy)
            {
                var configuration =
                    MatchConfiguration.CreateOfficialSixPlayerForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateOfficialSixPlayer(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);

                var decider = configuration.Participants.Heroes.First().Id;
                var ultimate = TheUltimateTrick();
                var definitions = configuration.CardDefinitions.Concat(new[] { ultimate }).ToArray();
                var instances = configuration.CardInstances
                    .Concat(new[]
                    {
                        new CardInstance(
                            new CardInstanceId("hanu-ultimate-staged-card"),
                            ultimate.Reference,
                            decider,
                            CardStateFacts.InitialInDeck())
                    })
                    .ToArray();

                var scenario = new InitialScenarioDescription(
                    configuration.Participants,
                    definitions,
                    instances,
                    configuration.TieBreakerState,
                    configuration.SetupSeed,
                    map.Reference,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                    Array.Empty<ObstacleOccupancy>());

                var session = new MatchSession(
                    new MatchSessionInitialState(scenario, setup, new[] { map }));
                var host = new MatchHost(session);
                var staged = new DelegatedMatch(session, host, definitions, map) { Decider = decider };
                staged.DriveToADelegatedTurn(againstAnEnemy);
                return staged;
            }

            /// <summary>
            /// Plays the opening of a real match, then writes exactly what a resolved Hurry up!
            /// writes - p.16's unlock and 8.71a's rewrite - and plays on until the rewritten hero is
            /// the selected actor.
            /// </summary>
            private void DriveToADelegatedTurn(bool againstAnEnemy)
            {
                OwnerScopedProjectionTests.DriveThroughSeats(
                    Host, 200,
                    projection => projection.IsTurnActive && projection.SelectedActor.IsDefined);
                Assert.That(Host.Projection.SelectedActor.IsDefined, Is.True,
                    "The opening did not reach a turn, so nothing below measures anything.");

                var live = session.aggregate.Configuration.CardInstances;
                var ultimateCard = live.Single(card =>
                    card.Id.Value == "hanu-ultimate-staged-card");
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimateCard), Is.True,
                    "p.16 puts the card faceup beside its owner's dashboard, from the deck.");

                var deciderSide = Host.Seats.FindByHero(Decider).TeamSide;
                var source = live.Single(card =>
                    card.OwnerId.Equals(Decider) &&
                    CardLifecycleAuthority.IsResolvingCardInTurnArea(card));
                var target = live.First(card =>
                    !card.OwnerId.Equals(Decider) &&
                    !card.OwnerId.Equals(Host.Projection.SelectedActor.ActorId) &&
                    CardLifecycleAuthority.IsResolvingCardInTurnArea(card) &&
                    (Host.Seats.FindByHero(card.OwnerId).TeamSide != deciderSide) == againstAnEnemy);

                Delegated = target.OwnerId;
                DelegatedCardId = target.Id;
                Assert.That(
                    CardLifecycleAuthority.TrySetPrintedInitiativeOverride(target, 11, source.Id),
                    Is.True);

                OwnerScopedProjectionTests.DriveThroughSeats(
                    Host, 400,
                    projection => projection.IsTurnActive &&
                        projection.SelectedActor.IsDefined &&
                        projection.SelectedActor.ActorId.Equals(Delegated) &&
                        projection.DecisionContract == null);

                Assert.That(Host.Projection.SelectedActor.ActorId, Is.EqualTo(Delegated),
                    "The rewritten hero never came up as the actor, so nothing below measures a " +
                    "delegated action. A walk that stops reaching one is a coverage regression.");
                Assert.That(
                    SeatProjectionFactory.DelegationOf(Host.Projection).DecidingHeroId,
                    Is.EqualTo(Decider),
                    "and the delegation the whole fixture is about actually stands here.");
                CommandsBeforeTheDelegatedTurn = Host.Projection.AcceptedCommands.Count;
            }

            /// <summary>Starts the delegated action from the deciding seat, and says whether it paused.</summary>
            internal bool StartTheDelegatedAction()
            {
                var seat = Host.Seats.FindByHero(Decider);
                var client = Host.ClientForSeat(seat.Id);
                var start = client.Projection.AuthorizedCommands
                    .FirstOrDefault(command => command.Kind == DirectorCommandKind.StartAction);
                if (start == null)
                {
                    return false;
                }

                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("delegated-start-" + Host.Sequence),
                    client.Projection.Version,
                    start));
                Assert.That(result.Accepted, Is.True, result.Detail);
                return Host.Projection.DecisionContract != null;
            }

            /// <summary>Plays the delegated action out from whichever seat holds its answers.</summary>
            internal void DriveTheDelegatedActionToItsEnd()
            {
                StartTheDelegatedAction();
                OwnerScopedProjectionTests.DriveThroughSeats(
                    Host, 40,
                    projection => !projection.SelectedActor.ActorId.Equals(Delegated));
            }
        }
    }
}
