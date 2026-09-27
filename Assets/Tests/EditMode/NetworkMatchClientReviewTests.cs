using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35b review closures for the network adapter boundary: a new immutable view for
    /// every received/actionability state, retained public history while controls are stripped,
    /// canonical typed prompt invocations, and the asynchronous half of exact-choice completion.
    /// </summary>
    public sealed class NetworkMatchClientReviewTests
    {
        [Test]
        public void AgreementUpdate_ChangesProjectionIdentity_AndWaitingViewKeepsPublicReadOnlyFacts()
        {
            var host = CreateHostAtTeamAgreement();
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var seat = host.Seats.Seats.First(value =>
                host.ProjectionForSeat(value.Id).TeamAgreement?.AuthorizedCommands.Count > 0);
            var client = harness.Client(harness.OrdinalOf(seat.Id.Value));
            var adapter = new NetworkMatchClient(
                client, host.Projection, MatchContentCatalogs.ForThisBuild());

            var before = adapter.Projection;
            var statement = before.TeamAgreement.AuthorizedCommands[0];
            var result = adapter.SubmitAgreement(statement);
            var waiting = result.Projection;

            Assert.That(result.Accepted, Is.True);
            Assert.That(waiting, Is.Not.SameAs(before),
                "Disabling actionability is itself a newly renderable projection state.");
            Assert.That(waiting.IsInputEnabled, Is.False);
            Assert.That(waiting.TeamAgreement, Is.Not.Null,
                "The public proposal and statements remain readable while an input is in flight.");
            Assert.That(waiting.TeamAgreement.ProposalId,
                Is.EqualTo(before.TeamAgreement.ProposalId));
            Assert.That(waiting.TeamAgreement.Participants.Select(value => value.HeroId),
                Is.EqualTo(before.TeamAgreement.Participants.Select(value => value.HeroId)));
            Assert.That(waiting.TeamAgreement.AuthorizedCommands, Is.Empty,
                "Retaining public agreement state must not retain a submit capability.");

            harness.Pump();
            var afterStatement = adapter.Projection;
            Assert.That(afterStatement, Is.Not.SameAs(waiting));
            Assert.That(afterStatement, Is.Not.SameAs(before),
                "A same-match-version agreement push is still a distinct received projection.");
            Assert.That(afterStatement.Version, Is.EqualTo(before.Version),
                "A statement updates public session state without executing the final rules command.");
            Assert.That(afterStatement.IsInputEnabled, Is.True);
            Assert.That(afterStatement.TeamAgreement.Participants.Any(value => value.HasStatement),
                Is.True);
            Assert.That(adapter.Projection, Is.SameAs(afterStatement),
                "Polling an unchanged record/actionability pair must retain stable identity.");
        }

        [Test]
        public void InputDisabledView_KeepsCompletedRounds()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host,
                700,
                projection => projection.CompletedRounds.Count > 0);
            Assert.That(host.Projection.CompletedRounds, Is.Not.Empty,
                "The closing fixture must reach public round history.");

            var seatProjection = host.ProjectionForSeat(host.Seats.Seats[0].Id);
            var online = Rehydrate(seatProjection, host.Projection, true);
            var expected = online.CompletedRounds.Select(value => value.RoundIndex).ToArray();

            online.Replace(Decode(seatProjection), false);

            Assert.That(online.IsInputEnabled, Is.False);
            Assert.That(online.CompletedRounds.Select(value => value.RoundIndex),
                Is.EqualTo(expected),
                "Input suppression removes commands, not already-public match history.");
        }

        [Test]
        public void RehydratedEffectAndRemovalPrompts_UseTheDecisionContractsCanonicalInvocations()
        {
            var effectHost = CreateHostAtExactOneEffectChoice();
            var effectSeat = effectHost.Seats.Seats.Single(value =>
                effectHost.ProjectionForSeat(value.Id).PendingEffectChoice != null);
            var effect = Rehydrate(
                effectHost.ProjectionForSeat(effectSeat.Id), effectHost.Projection, true);
            AssertCanonicalEffectBindings(effect);

            var removalHost = new MatchHost(
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause());
            var removalSeat = removalHost.Seats.Seats.Single(value =>
                removalHost.ProjectionForSeat(value.Id).PendingRemovalPrevention != null);
            var removal = Rehydrate(
                removalHost.ProjectionForSeat(removalSeat.Id), removalHost.Projection, true);
            AssertCanonicalRemovalBindings(removal);
        }

        [Test]
        public void ExactOneEffectSelection_AutoConfirmsAfterHostProjection_ExactlyOnce()
        {
            var host = CreateHostAtExactOneEffectChoice();
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var seat = host.Seats.Seats.Single(value =>
                host.ProjectionForSeat(value.Id).PendingEffectChoice != null);
            var client = harness.Client(harness.OrdinalOf(seat.Id.Value));
            var adapter = new NetworkMatchClient(
                client, host.Projection, MatchContentCatalogs.ForThisBuild());
            var before = adapter.Projection;
            var choice = before.PendingEffectChoice;
            var selection = before.DecisionContract.Commands.Single(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Equals(choice.SelectInvocations[0]));
            var sequence = host.Sequence;

            Assert.That(adapter.Submit(selection).Accepted, Is.True);
            harness.Pump();
            Assert.That(host.Sequence, Is.EqualTo(sequence + 1),
                "The first pump delivers only the selected candidate.");

            var autoConfirmWaiting = adapter.Projection;
            Assert.That(client.IsWaitingForHost, Is.True,
                "Reading the accepted selection projection queues its authoritative confirm.");
            Assert.That(autoConfirmWaiting.IsInputEnabled, Is.False);
            Assert.That(host.Sequence, Is.EqualTo(sequence + 1),
                "The confirmation is asynchronous and has not crossed the wire until the pump.");
            Assert.That(adapter.Projection, Is.SameAs(autoConfirmWaiting));

            harness.Pump();
            Assert.That(host.Sequence, Is.EqualTo(sequence + 2));
            var completed = adapter.Projection;
            Assert.That(
                completed.PendingEffectChoice == null ||
                !completed.PendingEffectChoice.ChoiceId.Equals(choice.ChoiceId),
                Is.True,
                "The select-plus-confirm continuation closes the exact choice.");

            _ = adapter.Projection;
            harness.Pump();
            Assert.That(host.Sequence, Is.EqualTo(sequence + 2),
                "Repeated rendering cannot submit the automatic confirmation twice.");
        }

        /// <summary>
        /// Milestone 8.93f. Snorri's rune permutation is reviewed as a whole: the fourth placement
        /// fills the choice but a network seat sends no automatic confirm, and only DONE commits the
        /// four physical runes.
        /// </summary>
        [Test]
        public void RuneSlotPermutation_WaitsForItsOwnDoneOverTheWire()
        {
            var session = ConfiguredMatchFixtures.CreateEngagedStagedSnorriSession();
            var snorri = session.Projection.Participants.Heroes.Select(hero => hero.Id).Single(hero =>
                session.Projection.Cards.Any(card => card.OwnerId.Equals(hero) &&
                    card.DefinitionReference.DefinitionId.Equals(
                        SnorriSilverFace.InscribeTheRunes)));
            var silver = session.Projection.Cards.Single(card =>
                card.OwnerId.Equals(snorri) &&
                card.DefinitionReference.DefinitionId.Equals(SnorriSilverFace.InscribeTheRunes));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(snorri, silver.Id));
            foreach (var other in session.Projection.Participants.Heroes.Select(hero => hero.Id)
                .Where(hero => !hero.Equals(snorri)).ToArray())
            {
                var card = session.Projection.Cards.First(value =>
                    value.OwnerId.Equals(other) && value.State.Location == CardLocation.Hand);
                ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(other, card.Id));
            }

            for (var step = 0; step < 20 &&
                !session.Projection.SelectedActor.CardInstanceId.Equals(silver.Id); step++)
            {
                var projection = session.Projection;
                if (projection.PendingInitiativeChoice != null)
                    ConfiguredMatchFixtures.Accept(session, DirectorCommand.ChooseTiedActor(
                        projection.PendingInitiativeChoice.OwningSide,
                        projection.PendingInitiativeChoice.CandidateCardInstanceIds
                            .Contains(silver.Id)
                            ? silver.Id
                            : projection.PendingInitiativeChoice.CandidateCardInstanceIds[0]));
                else if (projection.PendingTurnRelease != null)
                    ConfiguredMatchFixtures.Accept(session, DirectorCommand.ReleaseTurn());
                else
                    ConfiguredMatchFixtures.Accept(session, DirectorCommand.PerformHold(
                        projection.SelectedActor.CardInstanceId));
            }

            var skill = session.Projection.AvailableTurnActions.Single(candidate =>
                Action(session, candidate).Kind == ActionKind.Skill &&
                Action(session, candidate).Origin == ActionDefinitionOrigin.PrintedPrimary);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.StartAction(skill.ActionDefinitionId));
            Assert.That(session.Projection.PendingEffectChoice.ValueVocabulary,
                Is.EqualTo(EffectChoiceValueVocabulary.RuneTurnSlot));

            var host = new MatchHost(session);
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var seat = host.Seats.Seats.Single(value =>
                host.ProjectionForSeat(value.Id).PendingEffectChoice != null);
            var client = harness.Client(harness.OrdinalOf(seat.Id.Value));
            var adapter = new NetworkMatchClient(
                client, host.Projection, MatchContentCatalogs.ForThisBuild());

            // Rune N to slot (N mod 4) + 1: the encoded pair is (rune - 1) * 4 + slot.
            foreach (var pair in new[] { 2, 7, 12, 13 })
            {
                var choice = adapter.Projection.PendingEffectChoice;
                var select = adapter.Projection.DecisionContract.Commands.Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    command.EffectChoice.Candidate.Value.Equals(EffectValue.Integer(pair)));
                Assert.That(adapter.Submit(select).Accepted, Is.True);
                harness.Pump();
                _ = adapter.Projection;
                harness.Pump();
                Assert.That(adapter.Projection.PendingEffectChoice, Is.Not.Null,
                    "Placing pair " + pair + " must not close the choice.");
            }

            var filled = adapter.Projection.PendingEffectChoice;
            Assert.That(filled.SelectedCandidates, Has.Count.EqualTo(4));
            Assert.That(host.Projection.HeldMarkers, Is.Empty,
                "No automatic confirm crossed the wire: the four runes are still unassigned.");

            var done = adapter.Projection.DecisionContract.Commands.Single(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                ReferenceEquals(command.EffectChoice, filled.ConfirmInvocation));
            Assert.That(adapter.Submit(done).Accepted, Is.True);
            harness.Pump();
            Assert.That(host.Projection.MarkersHeldBy(snorri).Select(marker => marker.TurnSlotIndex),
                Is.EquivalentTo(new int?[] { 2, 3, 4, 1 }));
        }

        [Test]
        public void EffectAutoConfirmContinuation_IsAbandonedAcrossReconnect()
        {
            var host = CreateHostAtExactOneEffectChoice();
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            var seat = host.Seats.Seats.Single(value =>
                host.ProjectionForSeat(value.Id).PendingEffectChoice != null);
            var ordinal = harness.OrdinalOf(seat.Id.Value);
            var client = harness.Client(ordinal);
            var adapter = new NetworkMatchClient(
                client, host.Projection, MatchContentCatalogs.ForThisBuild());
            var initial = adapter.Projection;
            var initialSelect = initial.DecisionContract.Commands.First(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select);

            Assert.That(adapter.Submit(initialSelect).Accepted, Is.True);
            harness.DisconnectClient(ordinal);
            _ = adapter.Projection;
            ordinal = harness.ReconnectClient(ordinal);

            var reconnected = adapter.Projection;
            var directSelect = reconnected.DecisionContract.Commands.First(command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(client.TrySubmitCommand(directSelect, out _), Is.True);
            harness.Pump();
            var afterDirectSelect = host.Sequence;

            var selected = adapter.Projection;
            harness.Pump();
            Assert.That(host.Sequence, Is.EqualTo(afterDirectSelect),
                "A continuation armed on the abandoned connection cannot confirm later state.");
            Assert.That(selected.PendingEffectChoice, Is.Not.Null);
            Assert.That(selected.PendingEffectChoice.SelectedCandidates, Has.Count.EqualTo(1));
            Assert.That(selected.PendingEffectChoice.ConfirmInvocation, Is.Not.Null);
        }

        private static void AssertCanonicalEffectBindings(NetworkMatchReadModel projection)
        {
            var prompt = projection.PendingEffectChoice;
            Assert.That(prompt, Is.Not.Null);
            var invocations = prompt.SelectInvocations
                .Concat(prompt.DeselectInvocations)
                .Concat(Optional(prompt.ConfirmInvocation))
                .Concat(Optional(prompt.DeclineInvocation))
                .Concat(Optional(prompt.StopInvocation))
                .ToArray();

            Assert.That(invocations, Is.Not.Empty);
            foreach (var invocation in invocations)
            {
                var command = projection.DecisionContract.Commands.Single(value =>
                    value.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.EffectChoice.Equals(invocation));
                Assert.That(command.EffectChoice, Is.SameAs(invocation),
                    "The prompt and DecisionContract must share one authoritative invocation object.");
            }

            Assert.That(projection.PendingEffectChoice, Is.SameAs(prompt),
                "One immutable projection materializes one prompt identity.");
        }

        private static void AssertCanonicalRemovalBindings(NetworkMatchReadModel projection)
        {
            var prompt = projection.PendingRemovalPrevention;
            Assert.That(prompt, Is.Not.Null);
            var invocations = prompt.Payments.Select(value => value.Invocation)
                .Concat(new[] { prompt.DeclineInvocation });
            foreach (var invocation in invocations)
            {
                var command = projection.DecisionContract.Commands.Single(value =>
                    value.Kind == DirectorCommandKind.AnswerRemovalPrevention &&
                    value.RemovalPrevention.Equals(invocation));
                Assert.That(command.RemovalPrevention, Is.SameAs(invocation),
                    "The prompt and DecisionContract must share one authoritative invocation object.");
            }

            Assert.That(projection.PendingRemovalPrevention, Is.SameAs(prompt),
                "One immutable projection materializes one prompt identity.");
        }

        private static IEnumerable<EffectChoiceCommand> Optional(EffectChoiceCommand invocation) =>
            invocation == null
                ? Enumerable.Empty<EffectChoiceCommand>()
                : new[] { invocation };

        private static NetworkMatchReadModel Rehydrate(
            SeatProjection projection,
            IMatchReadModel trusted,
            bool actionable)
        {
            var result = new NetworkMatchReadModel(
                trusted, MatchContentCatalogs.ForThisBuild());
            result.Replace(Decode(projection), actionable);
            return result;
        }

        private static NetworkSeatProjectionRecord Decode(SeatProjection projection)
        {
            Assert.That(NetworkSeatProjectionCodec.TryDecode(
                NetworkSeatProjectionCodec.Encode(projection), out var record), Is.True);
            return record;
        }

        private static IMatchHost CreateHostAtTeamAgreement()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            OwnerScopedProjectionTests.DriveThroughSeats(
                host,
                120,
                _ => host.Seats.Seats.Any(value =>
                    host.ProjectionForSeat(value.Id).TeamAgreement != null));
            Assert.That(host.Seats.Seats.Any(value =>
                    host.ProjectionForSeat(value.Id).TeamAgreement != null), Is.True,
                "The closing fixture must reach a public team agreement.");
            return host;
        }

        private static IMatchHost CreateHostAtExactOneEffectChoice()
        {
            var session = ConfiguredMatchFixtures.CreateKineticSession(
                WaspCardContent.KineticRepulse, stoppedShort: false);
            var red = HandCard(session, TeamSide.Red, WaspCardContent.KineticRepulse);
            var blue = HandCard(session, TeamSide.Blue, WaspCardContent.StopProjectiles);
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            ConfiguredMatchFixtures.Accept(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));

            var skill = session.Projection.AvailableTurnActions.Single(candidate =>
            {
                var action = Action(session, candidate);
                return action.Origin == ActionDefinitionOrigin.PrintedPrimary &&
                    action.Kind == ActionKind.Skill;
            });
            ConfiguredMatchFixtures.Accept(
                session, DirectorCommand.StartAction(skill.ActionDefinitionId));
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null);
            Assert.That(session.Projection.PendingEffectChoice.MaximumSelections, Is.EqualTo(1),
                "Only one enemy unit is adjacent in the configured Kinetic Repulse fixture.");
            Assert.That(session.Projection.PendingEffectChoice.SelectedCandidates, Is.Empty);
            Assert.That(session.Projection.PendingEffectChoice.SelectInvocations, Has.Count.EqualTo(1));
            return new MatchHost(session);
        }

        private static ActionDefinition Action(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId));

        private static CardInstance HandCard(
            MatchSession session,
            TeamSide side,
            CardDefinitionId definitionId) =>
            session.Projection.Cards.Single(card =>
                session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                card.State.Location == CardLocation.Hand &&
                card.DefinitionReference.DefinitionId.Equals(definitionId));
    }
}
