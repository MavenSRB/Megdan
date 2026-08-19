using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.1a end to end: a Skill requires another hero to discard one card of their own
    /// choosing, and the card state that decision needs reaches every point the capability can be
    /// asked from.
    ///
    /// Milestone 7.1 built the composition and could not reach it. What this fixture proves is the
    /// reaching: that the frame carries card instances at Skill start, at repeated-Skill start, on the
    /// option-feasibility walk, on the choice-answer path, and through snapshot restore's candidate
    /// re-derivation, and that a capability handed no card state fails loudly instead of reporting the
    /// empty hand that means something else entirely.
    /// </summary>
    public sealed class CardStateFromEffectTests
    {
        private static readonly ActionDefinitionId CardStateSkill =
            new ActionDefinitionId("primary-card-state-skill");

        private static readonly EffectNodeId DiscardNode = new EffectNodeId("demand-discard");
        private static readonly HexCoordinate ActorAt = new HexCoordinate(0, -1);
        private static readonly HexCoordinate OpponentAt = new HexCoordinate(-1, -5);

        // -------------------------------------------------------------------------------------------
        // The absence is named, and reading it fails loudly
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AbsentCardState_IsNotAnEmptyHand()
        {
            Assert.That(EffectCardState.Absent.IsAvailable, Is.False);
            Assert.That(
                () => EffectCardState.Absent.Instances,
                Throws.InstanceOf<InvalidOperationException>(),
                "Reading cards that were never supplied must fail rather than answer 'none'. The two " +
                "mean opposite things: one is a caller bug, the other is `if able` declining.");

            var supplied = EffectCardState.Of(
                Array.Empty<CardDefinition>(), Array.Empty<CardInstance>());
            Assert.That(supplied.IsAvailable, Is.True);
            Assert.That(supplied.Instances, Is.Empty,
                "An empty view is a legitimate answer and is not the absence.");
        }

        [Test]
        public void AResolutionGivenNoCardState_FailsRatherThanOfferingNothing()
        {
            // The deliberate violation this rule is proved against: a resolution that reaches the
            // discard without card state. Before milestone 7.1a every path was in exactly this
            // position - `StartTurnSkill` already took the instances and dropped them - and the
            // damage would not have been an exception but a silent one: the capability would have
            // reported an empty candidate set, which the node reads as the chooser having nothing to
            // give. A hero would have kept their whole hand and the rules would have looked satisfied.
            Assert.That(
                () => StartResolution(EffectCardState.Absent, out _),
                Throws.InstanceOf<InvalidOperationException>(),
                "Acceptance check 1: no path may substitute an empty set for cards it was not given.");
        }

        // -------------------------------------------------------------------------------------------
        // Reach point 1: Skill start
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AtSkillStart_TheChoiceOffersTheOtherHerosHandAndRecordsThemAsChooser()
        {
            var session = StartCardStateSkill();
            var choice = session.Projection.PendingEffectChoice;

            Assert.That(choice, Is.Not.Null, "The discard pauses on a chooser-owned selection.");
            Assert.That(choice.NodeId, Is.EqualTo(DiscardNode));
            Assert.That(choice.ChooserId, Is.EqualTo(BlueHeroId(session)),
                "Acceptance check 3: the pending choice records whose decision it is, and it is not " +
                "the acting hero's.");
            Assert.That(choice.ChooserId, Is.Not.EqualTo(ActingHeroId(session)));

            var offered = choice.Candidates.Select(candidate => candidate.Value.CardInstanceId).ToArray();
            Assert.That(offered, Is.Not.Empty, "The frame reached the chooser's hand.");
            Assert.That(
                offered.All(id => CardOf(session, id).OwnerId.Equals(BlueHeroId(session))),
                Is.True,
                "Candidates are derived in the chooser's own context, not the actor's.");
            Assert.That(
                offered.All(id => CardOf(session, id).State.Location == CardLocation.Hand),
                Is.True);
        }

        [Test]
        public void ConfirmingTheChoice_DiscardsExactlyThatCardFromTheChoosersHand()
        {
            var session = StartCardStateSkill();
            var chosen = FirstCandidate(session);
            var otherHand = HandOf(session, BlueHeroId(session))
                .Where(card => !card.Id.Equals(chosen)).Select(card => card.Id).ToArray();

            AnswerWithCard(session, chosen);

            Assert.That(CardOf(session, chosen).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(
                otherHand.All(id => CardOf(session, id).State.Location == CardLocation.Hand),
                Is.True,
                "Exactly the chosen card moves; the mutation is handed a card and decides nothing.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null,
                "The action completes once the discard commits.");
        }

        // -------------------------------------------------------------------------------------------
        // Reach point 2: the option-feasibility walk
        // -------------------------------------------------------------------------------------------

        [Test]
        public void TheFeasibilityWalk_ReadsTheSameHandTheExecutionWould()
        {
            var session = CreateCardStateSession();
            CommitCardStateCards(session);

            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(CardStateSkill));

            Assert.That(offer.ActionDefinitionId, Is.EqualTo(CardStateSkill));
            Assert.That(offer.IsAvailable, Is.True,
                "Acceptance check 1: the walk is asked with the same card state the start would get. " +
                "Given none it would answer with an empty hand and grey a Skill that can be performed.");
        }

        // -------------------------------------------------------------------------------------------
        // Reach point 3: the choice-answer path, and whose answer it is
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnAnswerSubmittedForAnotherHero_IsRefusedAndTheChoiceStillStands()
        {
            var session = StartCardStateSkill();
            var choice = session.Projection.PendingEffectChoice;
            var legitimate = choice.SelectInvocations[0];
            var handBefore = HandOf(session, BlueHeroId(session)).Select(card => card.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            var forged = new EffectChoiceCommand(
                legitimate.ResolutionId,
                legitimate.ChoiceId,
                legitimate.Kind,
                legitimate.Candidate,
                ActingHeroId(session));

            var rejected = session.Execute(DirectorCommand.AnswerEffectChoice(forged));

            Assert.That(rejected.Accepted, Is.False,
                "The acting hero does not get to make the other hero's decision for them.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "Acceptance check 3: refused atomically with the choice still standing.");
            Assert.That(
                session.Projection.PendingEffectChoice.SelectedCandidates, Is.Empty,
                "Nothing was selected on the way to being refused.");
            Assert.That(
                HandOf(session, BlueHeroId(session)).Select(card => card.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(handBefore),
                "No card moved: the same hand stands, card for card.");

            // and the hero whose decision it is can still make it.
            AnswerWithCard(session, legitimate.Candidate.Value.CardInstanceId);
            Assert.That(
                CardOf(session, legitimate.Candidate.Value.CardInstanceId).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The refusal cost the chooser nothing; their own answer still commits.");
        }

        [Test]
        public void TheResolverReportsAWrongChooserDistinctlyFromAnInvalidChoice()
        {
            var frame = PausedFrame(out var context, out var chooserId, out var actorId);
            var choice = frame.PendingChoice;

            var wrongHero = EffectResolutionSession.Answer(
                frame,
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                    choice.Candidates[0], actorId),
                context);

            Assert.That(wrongHero.Accepted, Is.False);
            Assert.That(
                wrongHero.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.WrongChooser),
                "Who sent it is a different complaint from what it said, and a player needs told which.");
            Assert.That(wrongHero.Frame, Is.SameAs(frame),
                "A refused command leaves the resolution exactly as it was.");

            var rightHeroWrongCard = EffectResolutionSession.Answer(
                frame,
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Confirm,
                    default, chooserId),
                context);

            Assert.That(rightHeroWrongCard.Accepted, Is.False);
            Assert.That(
                rightHeroWrongCard.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.IncompleteSelection),
                "The chooser's own malformed answer still reports what was wrong with the answer.");
        }

        // -------------------------------------------------------------------------------------------
        // `if able`, and the stale answer
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AChooserWithNoCardLeft_DeclinesWithoutFailingTheAction()
        {
            var withCards = StartResolution(BuildCardState(chooserHasCards: true), out _);
            var withNone = StartResolution(BuildCardState(chooserHasCards: false), out _);

            Assert.That(withCards.Frame, Is.Not.Null, "A chooser with cards is asked.");
            Assert.That(withNone.Frame, Is.Null, "A chooser with none is not asked.");
            Assert.That(withNone.IsComplete, Is.True,
                "Acceptance check 4: the enclosing action ends normally rather than stopping.");
            Assert.That(
                withNone.Events.Any(value =>
                    value.ResultKind == EffectExecutionResultKind.ImpossibleMandatory),
                Is.False,
                "`if able` is not failure: a hero with nothing to give does not stop the action.");
            Assert.That(
                withNone.Events.Any(value =>
                    value.ResultKind == EffectExecutionResultKind.DeclinedOptional),
                Is.True,
                "It is recorded as a decline.");
        }

        [Test]
        public void TheEmptyHandDecline_IsDistinguishableFromADeclinedOffer()
        {
            var withCards = StartResolution(BuildCardState(chooserHasCards: true), out _);
            var withNone = StartResolution(BuildCardState(chooserHasCards: false), out _);

            // The distinction without a fifth result kind: a chooser who was never asked leaves no
            // choice behind, while one who was asked and declined leaves the record of being asked.
            Assert.That(
                withNone.Events.Any(value =>
                    value.ResultKind == EffectExecutionResultKind.NeedsChoice),
                Is.False,
                "Nothing was offered, so nothing was declined by anyone.");
            Assert.That(withCards.Frame.PendingChoice, Is.Not.Null,
                "The other outcome pauses on a decision that exists and is someone's to make.");
            Assert.That(
                withCards.Events.Any(value =>
                    value.ResultKind == EffectExecutionResultKind.DeclinedOptional),
                Is.False,
                "and it has not declined anything at the point it pauses.");
        }

        [Test]
        public void ACardThatLeftTheHandBetweenOfferAndAnswer_IsRefused()
        {
            var session = StartCardStateSkill();
            var chosen = FirstCandidate(session);
            var choice = session.Projection.PendingEffectChoice;
            var select = choice.SelectInvocations
                .Single(invocation => invocation.Candidate.Value.CardInstanceId.Equals(chosen));

            // The offer named it; the hand no longer does. The answer is checked against the state
            // that exists when it arrives, not the state that existed when the choice opened.
            var moved = CardOf(session, chosen);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(moved), Is.True,
                "Arrange: the card leaves the hand behind the choice's back.");
            var survivingHand = HandOf(session, BlueHeroId(session)).Select(card => card.Id.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();

            var accepted = session.Execute(DirectorCommand.AnswerEffectChoice(select));
            if (accepted.Accepted)
            {
                session.Execute(DirectorCommand.AnswerEffectChoice(
                    session.Projection.PendingEffectChoice.ConfirmInvocation));
            }

            Assert.That(
                HandOf(session, BlueHeroId(session)).Select(card => card.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(survivingHand),
                "Acceptance check 5: the answer is re-read against the hand as it now stands, so the " +
                "stale card is refused and no other card is taken in its place.");
            Assert.That(
                session.Projection.Cards.Count(card =>
                    card.Id.Equals(chosen) && card.State.Location == CardLocation.DiscardPile),
                Is.EqualTo(1),
                "The card that had already gone is not discarded a second time.");
        }

        // -------------------------------------------------------------------------------------------
        // Reach point 4: snapshot restore re-derives the paused choice's candidates
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ThePausedChoice_SurvivesSnapshotRoundTripWithItsChooserAndCandidates()
        {
            var session = StartCardStateSkill();
            var before = session.Projection.PendingEffectChoice;

            var restored = Deserialize(session);
            var after = restored.Round.CurrentTurnAuthority.PendingEffectChoice;

            Assert.That(after, Is.Not.Null,
                "Acceptance check 1: restore re-derives the candidates by asking the handler again, " +
                "and it can only do that if the payload's cards reached the resolution context.");
            Assert.That(after.ChooserId, Is.EqualTo(before.ChooserId));
            Assert.That(after.NodeId, Is.EqualTo(before.NodeId));
            Assert.That(
                after.Candidates.Select(candidate => candidate.Value.CardInstanceId.Value).OrderBy(v => v),
                Is.EqualTo(before.Candidates.Select(candidate => candidate.Value.CardInstanceId.Value)
                    .OrderBy(v => v)),
                "The re-derived set is the offered set, because both come from the same hand.");
        }

        [Test]
        public void RestoreAndContinue_CommitsTheDiscardThroughThePublicCommandPath()
        {
            var session = StartCardStateSkill();
            var snapshot = session.Projection.AuthoritativeSnapshot;
            var chosen = FirstCandidate(session);

            var resumed = CreateCardStateSession();
            var restore = resumed.RestoreSnapshot(snapshot);
            Assert.That(restore.Accepted, Is.True, restore.Detail);

            AnswerWithCard(resumed, chosen);

            Assert.That(CardOf(resumed, chosen).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "A restored pause is answered by the same commands the live one takes.");
            Assert.That(resumed.Projection.PendingEffectChoice, Is.Null);
        }

        [Test]
        public void TheDiscard_ReplaysDeterministicallyAndUndoesOnEitherSideOfIt()
        {
            var session = StartCardStateSkill();
            var chosen = FirstCandidate(session);
            AnswerWithCard(session, chosen);

            var history = session.Projection.AcceptedCommands;
            var replayed = CreateCardStateSession();
            foreach (var command in history)
            {
                var result = replayed.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
            }

            Assert.That(
                replayed.Projection.AuthoritativeSnapshot,
                Is.EqualTo(session.Projection.AuthoritativeSnapshot),
                "A fresh deterministic replay of the same commands reaches the same authoritative state.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(CardOf(session, chosen).State.Location, Is.EqualTo(CardLocation.Hand),
                "Undo truncated after the discard puts the card back in the hand it came from.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "and leaves the choice standing again, which is the state the prefix ended in.");
        }

        // -------------------------------------------------------------------------------------------
        // Reach point 5: the repeated-Skill start
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ARepeatedSkillStart_IsGivenCardStateToo()
        {
            // The repeated start builds its own resolution context from the action boundary rather
            // than inheriting the first instance's, so it is a separate reach point even though the
            // card-state fixture itself never repeats. Asking the boundary directly is what proves it
            // carries the instances, without inventing a second repeat fixture to carry a discard.
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var boundary = new ActionBoundaryContext(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                new GuardsDigital.Core.Timing.ActiveEffectLifecycle(),
                configuration.TieBreakerState,
                1,
                1);

            Assert.That(boundary.Instances, Is.Not.Empty,
                "Acceptance check 1: a repeated instance reads the hands as they stand when it starts.");
            Assert.That(
                boundary.Instances.Select(instance => instance.Id.Value),
                Is.EquivalentTo(configuration.CardInstances.Select(instance => instance.Id.Value)));
            Assert.That(
                () => new ActionBoundaryContext(
                    configuration.CardDefinitions, null, configuration.Participants,
                    new GuardsDigital.Core.Timing.ActiveEffectLifecycle(),
                    configuration.TieBreakerState, 1, 1),
                Throws.InstanceOf<ArgumentNullException>(),
                "There is no boundary without them, so no path can quietly pass none.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static EffectResolutionStepResult StartResolution(
            EffectCardState cardState, out HeroId chooserId)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var redHeroId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            chooserId = blueHeroId;

            var card = configuration.CardInstances.First(instance =>
                instance.Id.Value.Contains("red-demand"));
            var definition = configuration.CardDefinitions.Single(candidate =>
                candidate.Reference.Equals(card.DefinitionReference));
            var action = definition.ActionDefinitions.Single(candidate =>
                candidate.Id.Equals(CardStateSkill));

            var context = new EffectResolutionContext(
                action.EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(),
                configuration.Participants,
                null,
                1,
                1,
                RuleModificationSet.Empty,
                cardState);

            return EffectResolutionSession.Start(
                new EffectResolutionId("card-state-probe"),
                card.Id,
                card.DefinitionReference,
                CardStateSkill,
                redHeroId,
                redHeroId,
                ActionInvocationContext.PrintedPrimary,
                context);
        }

        /// <summary>The frame a live pause produces, with the context that opened it.</summary>
        private static PendingResolution PausedFrame(
            out EffectResolutionContext context, out HeroId chooserId, out HeroId actorId)
        {
            var cardState = BuildCardState(chooserHasCards: true);
            var step = StartResolution(cardState, out chooserId);
            actorId = new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var card = configuration.CardInstances.First(instance =>
                instance.Id.Value.Contains("red-demand"));
            var definition = configuration.CardDefinitions.Single(candidate =>
                candidate.Reference.Equals(card.DefinitionReference));

            context = new EffectResolutionContext(
                definition.ActionDefinitions.Single(candidate => candidate.Id.Equals(CardStateSkill))
                    .EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(),
                configuration.Participants,
                null,
                1,
                1,
                RuleModificationSet.Empty,
                cardState);

            Assert.That(step.Frame, Is.Not.Null, "This helper is about a frame that paused.");
            return step.Frame;
        }

        /// <summary>
        /// The fixture's own definitions and instances, optionally with the chooser's hand emptied so
        /// the `if able` path can be reached without steering a whole match into it.
        /// </summary>
        private static EffectCardState BuildCardState(bool chooserHasCards)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var blueHeroId = new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);
            var instances = chooserHasCards
                ? configuration.CardInstances
                : configuration.CardInstances.Where(instance => !instance.OwnerId.Equals(blueHeroId))
                    .ToArray();

            return EffectCardState.Of(configuration.CardDefinitions, instances);
        }

        private static MatchSession CreateCardStateSession()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForCardStateFixture(TeamSide.Blue);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var redHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;
            var blueHeroId = configuration.Participants.Heroes
                .Single(hero => configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, ActorAt),
                    new HeroOccupancy(blueHeroId, OpponentAt)
                },
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        private static void CommitCardStateCards(MatchSession session)
        {
            foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var card = session.Projection.Cards.Single(candidate =>
                    session.Projection.Participants.GetTeamForHero(candidate.OwnerId).Side == side &&
                    candidate.Id.Value.Contains("demand") &&
                    candidate.State.Location == CardLocation.Hand);
                Accept(session, DirectorCommand.CommitCard(card.OwnerId, card.Id));
            }
        }

        private static MatchSession StartCardStateSkill()
        {
            var session = CreateCardStateSession();
            CommitCardStateCards(session);

            var offer = session.Projection.AvailableTurnActions
                .SingleOrDefault(candidate => candidate.ActionDefinitionId.Equals(CardStateSkill));
            Assert.That(offer.ActionDefinitionId, Is.EqualTo(CardStateSkill),
                "The card-state Skill must be the current actor's offer.");
            Accept(session, DirectorCommand.StartAction(offer.ActionDefinitionId));
            return session;
        }

        private static void AnswerWithCard(MatchSession session, CardInstanceId cardId)
        {
            var choice = session.Projection.PendingEffectChoice;
            Accept(session, DirectorCommand.AnswerEffectChoice(
                choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.CardInstanceId.Equals(cardId))));
            Accept(session, DirectorCommand.AnswerEffectChoice(
                session.Projection.PendingEffectChoice.ConfirmInvocation));
        }

        private static CardInstanceId FirstCandidate(MatchSession session) =>
            session.Projection.PendingEffectChoice.Candidates[0].Value.CardInstanceId;

        private static CardInstance CardOf(MatchSession session, CardInstanceId cardId) =>
            session.Projection.Cards.Single(card => card.Id.Equals(cardId));

        private static IReadOnlyList<CardInstance> HandOf(MatchSession session, HeroId heroId) =>
            session.Projection.Cards
                .Where(card => card.OwnerId.Equals(heroId) && card.State.Location == CardLocation.Hand)
                .ToArray();

        private static HeroId ActingHeroId(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero =>
                    session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red).Id;

        private static HeroId BlueHeroId(MatchSession session) =>
            session.Projection.Participants.Heroes
                .Single(hero =>
                    session.Projection.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue).Id;

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static RestoredAuthoritativeMatch Deserialize(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
    }
}
