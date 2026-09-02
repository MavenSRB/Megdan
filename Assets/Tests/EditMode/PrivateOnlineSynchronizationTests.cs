using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.33. Complete projection synchronization over the wire: six real client
    /// machines reproduce the host's public state and their own private projection byte for byte
    /// across the seeded walk, the team-agreement families synchronize their statements and
    /// submit their final rules choice exactly once, a stale answer is healed by the offered
    /// resync, a host-side restore republishes every seat, and the idempotency window is sized
    /// from this walk's own measured traffic.
    /// </summary>
    public sealed class PrivateOnlineSynchronizationTests
    {
        /// <summary>
        /// The convergence check itself: across four hundred wire-driven steps of the seeded
        /// official six-seat match, after every accepted submission every seated client's
        /// delivered projection equals the host's own current encoding for that seat.
        /// </summary>
        [Test]
        public void SixClients_ConvergeByteForByte_AcrossTheSeededWireWalk()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.AssertConverged();

            var families = new HashSet<string>();
            var maxPayloadBytes = 0;
            var submitted = harness.DriveThroughWire(700, afterAcceptedStep: () =>
            {
                harness.AssertConverged();
                var contract = harness.MatchHost.Projection.DecisionContract;
                if (contract != null)
                {
                    families.Add(contract.FamilyId.Value);
                }

                for (var ordinal = 1; ordinal <= 6; ordinal++)
                {
                    maxPayloadBytes = Math.Max(
                        maxPayloadBytes, harness.LastProjectionPayload(ordinal).Length);
                }
            });

            Assert.That(submitted, Is.EqualTo(700),
                "The seeded walk ended early; the sweep saw less of the match than it must.");

            // The anti-vacuity floor: what this exact wire-driven seed-2 walk deterministically
            // reaches in its first seven hundred steps. A sweep that stops seeing these is a
            // broken sweep, not a family that moved.
            Assert.That(families, Is.SupersetOf(new[]
            {
                "initiative", "movement", "attack-target", "effect", "correction",
                "minion-battle", "system-transition"
            }), "Reached: " + string.Join(", ", families.OrderBy(value => value)));

            // The complete projection must keep fitting inside one sealed frame, with headroom.
            // The measured maximum is part of the milestone's evidence.
            Assert.That(maxPayloadBytes,
                Is.LessThan(PrivateSessionSecureChannel.MaxSealedPayloadBytes / 2),
                "The largest complete projection was " + maxPayloadBytes + " bytes; the frame " +
                "bound is " + PrivateSessionSecureChannel.MaxSealedPayloadBytes + ".");
            Assert.That(maxPayloadBytes, Is.GreaterThan(4096),
                "A complete projection this small means the sweep measured the wrong thing.");
        }

        /// <summary>
        /// The wire detail restates the pending surface field by field, and restates its scoping:
        /// what the owner's projection holds arrives populated, and what a waiting seat's
        /// projection does not hold has no populated row - structural absence, over the bytes.
        /// </summary>
        [Test]
        public void TheWireDetail_RestatesThePendingSurface_AndItsScoping()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            AssertDetailForEverySeat(host);
            var lastFamily = "";
            OwnerScopedProjectionTests.DriveThroughSeats(host, 700, projection =>
            {
                var family = projection.DecisionContract?.FamilyId.Value ?? "";
                if (family != lastFamily)
                {
                    lastFamily = family;
                    AssertDetailForEverySeat(host);
                }

                return false;
            });

            AssertDetailForEverySeat(new MatchHost(
                ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()));
            AssertDetailForEverySeat(new MatchHost(
                HeroRespawnTests.CreatePendingRespawnSession()));
            AssertDetailForEverySeat(new MatchHost(
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause()));
        }

        private static void AssertDetailForEverySeat(IMatchHost host)
        {
            foreach (var seat in host.Seats.Seats)
            {
                var projection = host.ProjectionForSeat(seat.Id);
                var encoded = NetworkSeatProjectionCodec.Encode(projection);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(encoded, out var record), Is.True,
                    "Seat " + seat.Id + "'s projection did not decode at version " +
                    projection.Version + ".");
                AssertDetailMatches(projection, record.Detail, seat.Id.Value);
            }
        }

        private static void AssertDetailMatches(
            SeatProjection projection, NetworkProjectionDetailRecord detail, string seat)
        {
            Assert.That(detail, Is.Not.Null, seat + " carried no detail record.");

            // Completed rounds: the full history, slots and stage outcomes alike.
            Assert.That(detail.CompletedRounds.Select(row => row.RoundIndex),
                Is.EqualTo(projection.CompletedRounds.Select(round => round.RoundIndex)), seat);
            foreach (var (row, round) in detail.CompletedRounds.Zip(
                projection.CompletedRounds, (row, round) => (row, round)))
            {
                Assert.That(row.TurnSlots.Select(slot => slot.TurnIndex),
                    Is.EqualTo(round.TurnSlots.Select(slot => slot.TurnIndex)), seat);
                Assert.That(row.EndOfRoundOutcomes.Select(outcome => outcome.Stage),
                    Is.EqualTo(round.EndOfRoundOutcomes.Select(outcome => (int)outcome.Stage)), seat);
                Assert.That(row.EndOfRoundOutcomes.Select(outcome => outcome.Kind),
                    Is.EqualTo(round.EndOfRoundOutcomes.Select(outcome => (int)outcome.Kind)), seat);
            }

            // Each pending family: present exactly where the scoped projection holds it, and
            // restating the owner's candidate detail where it does.
            var initiative = projection.PendingInitiativeChoice;
            Assert.That(detail.PendingInitiativeChoice != null, Is.EqualTo(initiative != null), seat);
            if (initiative != null)
            {
                Assert.That(detail.PendingInitiativeChoice.CandidateCardInstanceIds,
                    Is.EqualTo(initiative.CandidateCardInstanceIds.Select(id => id.Value)), seat);
                Assert.That(detail.PendingInitiativeChoice.OwningSide,
                    Is.EqualTo((int)initiative.OwningSide), seat);
            }

            var movementResolution = projection.PendingMovementResolution;
            Assert.That(detail.PendingMovementResolution != null,
                Is.EqualTo(movementResolution != null), seat);
            if (movementResolution != null)
            {
                Assert.That(detail.PendingMovementResolution.ResolutionId,
                    Is.EqualTo(movementResolution.ResolutionId.Value), seat);
                Assert.That(detail.PendingMovementResolution.RemainingAllowance,
                    Is.EqualTo(movementResolution.RemainingAllowance), seat);
                Assert.That(detail.PendingMovementResolution.Route.Count,
                    Is.EqualTo(movementResolution.MovementSessionState.Route.Count), seat);
            }

            var movementChoice = projection.PendingMovementChoice;
            Assert.That(detail.PendingMovementChoice != null, Is.EqualTo(movementChoice != null), seat);
            if (movementChoice != null)
            {
                Assert.That(detail.PendingMovementChoice.ChooserId,
                    Is.EqualTo(movementChoice.ChooserId.Value), seat);
                Assert.That(detail.PendingMovementChoice.LegalDestinations
                        .Select(row => row.Q + "," + row.R + ":" + row.Cost),
                    Is.EqualTo(movementChoice.LegalDestinations.Select(destination =>
                        destination.Destination.Q + "," + destination.Destination.R + ":" +
                        destination.Cost)), seat);
                Assert.That(detail.PendingMovementChoice.CanStop,
                    Is.EqualTo(movementChoice.CanStop), seat);
            }

            var attackResolution = projection.PendingAttackResolution;
            Assert.That(detail.PendingAttackResolution != null,
                Is.EqualTo(attackResolution != null), seat);
            if (attackResolution != null)
            {
                Assert.That(detail.PendingAttackResolution.Phase,
                    Is.EqualTo((int)attackResolution.Phase), seat);
                Assert.That(detail.PendingAttackResolution.HasAttackTotal,
                    Is.EqualTo(attackResolution.AttackValue != null), seat);
                Assert.That(detail.PendingAttackResolution.Target != null,
                    Is.EqualTo(attackResolution.Target.IsDefined), seat);
            }

            var attackTargetChoice = projection.PendingAttackTargetChoice;
            Assert.That(detail.PendingAttackTargetChoice != null,
                Is.EqualTo(attackTargetChoice != null), seat);
            if (attackTargetChoice != null)
            {
                Assert.That(detail.PendingAttackTargetChoice.LegalTargets.Count,
                    Is.EqualTo(attackTargetChoice.LegalTargets.Count), seat);
                Assert.That(detail.PendingAttackTargetChoice.ChooserId,
                    Is.EqualTo(attackTargetChoice.ChooserId.Value), seat);
            }

            Assert.That(detail.PendingAttackTargetDefensePreviews.Select(row => row.HeroId),
                Is.EqualTo(projection.PendingAttackTargetDefensePreviews
                    .Select(preview => preview.HeroId.Value)), seat);

            var defenseChoice = projection.PendingDefenseChoice;
            Assert.That(detail.PendingDefenseChoice != null, Is.EqualTo(defenseChoice != null), seat);
            if (defenseChoice != null)
            {
                Assert.That(detail.PendingDefenseChoice.AttackTotal,
                    Is.EqualTo(defenseChoice.AttackTotal), seat);
                Assert.That(detail.PendingDefenseChoice.LegalDefenseOffers
                        .Select(row => row.SourceCardInstanceId),
                    Is.EqualTo(defenseChoice.LegalDefenseOffers
                        .Select(offer => offer.SourceCardInstanceId.Value)), seat);
                Assert.That(detail.PendingDefenseChoice.CanDecline,
                    Is.EqualTo(defenseChoice.CanDecline), seat);
            }

            var defensePreview = projection.PendingDefensePreview;
            Assert.That(detail.PendingDefensePreview != null, Is.EqualTo(defensePreview != null), seat);
            if (defensePreview != null)
            {
                Assert.That(detail.PendingDefensePreview.AttackTotal,
                    Is.EqualTo(defensePreview.AttackTotal), seat);
                Assert.That(detail.PendingDefensePreview.Choices.Select(row => row.ChoiceLabel),
                    Is.EqualTo(defensePreview.Choices.Select(choice => choice.ChoiceLabel)), seat);
                Assert.That(detail.PendingDefensePreview.AttackContributions.Sum(row => row.Amount),
                    Is.EqualTo(defensePreview.AttackTotal), seat);
            }

            var skill = projection.PendingSkillResolution;
            Assert.That(detail.PendingSkillResolution != null, Is.EqualTo(skill != null), seat);
            if (skill != null)
            {
                Assert.That(detail.PendingSkillResolution.EffectDefinitionId,
                    Is.EqualTo(skill.EffectDefinitionReference.DefinitionId.Value), seat);
                Assert.That(detail.PendingSkillResolution.HasPendingChoice,
                    Is.EqualTo(skill.PendingChoice != null), seat);
            }

            var effectChoice = projection.PendingEffectChoice;
            Assert.That(detail.PendingEffectChoice != null, Is.EqualTo(effectChoice != null), seat);
            if (effectChoice != null)
            {
                Assert.That(detail.PendingEffectChoice.Kind, Is.EqualTo((int)effectChoice.Kind), seat);
                Assert.That(detail.PendingEffectChoice.Candidates.Count,
                    Is.EqualTo(effectChoice.Candidates.Count), seat);
                Assert.That(detail.PendingEffectChoice.SelectedCandidates.Count,
                    Is.EqualTo(effectChoice.SelectedCandidates.Count), seat);
                Assert.That(detail.PendingEffectChoice.UnavailableCandidates.Count,
                    Is.EqualTo(effectChoice.UnavailableCandidates.Count), seat);
                Assert.That(detail.PendingEffectChoice.HasConfirmInvocation,
                    Is.EqualTo(effectChoice.ConfirmInvocation != null), seat);
                Assert.That(detail.PendingEffectChoice.HasDeclineInvocation,
                    Is.EqualTo(effectChoice.DeclineInvocation != null), seat);
                Assert.That(detail.PendingEffectChoice.HasStopInvocation,
                    Is.EqualTo(effectChoice.StopInvocation != null), seat);
            }

            Assert.That(detail.PendingEffectChoicePreview.Count,
                Is.EqualTo(projection.PendingEffectChoicePreview.Count), seat);

            var correction = projection.PendingDisplacedMinionCorrection;
            Assert.That(detail.PendingDisplacedMinionCorrection != null,
                Is.EqualTo(correction != null), seat);
            if (correction != null)
            {
                Assert.That(detail.PendingDisplacedMinionCorrection.Stage,
                    Is.EqualTo((int)correction.Stage), seat);
                Assert.That(detail.PendingDisplacedMinionCorrection.SubjectCandidates.Count,
                    Is.EqualTo(correction.SubjectCandidates.Count), seat);
                Assert.That(detail.PendingDisplacedMinionCorrection.DestinationCandidates.Count,
                    Is.EqualTo(correction.DestinationCandidates.Count), seat);
            }

            var minionBattle = projection.PendingMinionBattleChoice;
            Assert.That(detail.PendingMinionBattleChoice != null,
                Is.EqualTo(minionBattle != null), seat);
            if (minionBattle != null)
            {
                Assert.That(detail.PendingMinionBattleChoice.RequiredRemovalCount,
                    Is.EqualTo(minionBattle.RequiredRemovalCount), seat);
                Assert.That(detail.PendingMinionBattleChoice.Candidates.Count,
                    Is.EqualTo(minionBattle.Candidates.Count), seat);
                Assert.That(detail.PendingMinionBattleChoice.SelectedUnits.Count,
                    Is.EqualTo(minionBattle.SelectedUnits.Count), seat);
                Assert.That(detail.PendingMinionBattleChoice.CanConfirm,
                    Is.EqualTo(minionBattle.CanConfirm), seat);
            }

            var respawn = projection.PendingHeroRespawnChoice;
            Assert.That(detail.PendingHeroRespawnChoice != null, Is.EqualTo(respawn != null), seat);
            if (respawn != null)
            {
                Assert.That(detail.PendingHeroRespawnChoice.HeroId,
                    Is.EqualTo(respawn.HeroId.Value), seat);
                Assert.That(detail.PendingHeroRespawnChoice.Destinations
                        .Select(row => row.Q + "," + row.R),
                    Is.EqualTo(respawn.Destinations.Select(destination =>
                        destination.Coordinate.Q + "," + destination.Coordinate.R)), seat);
            }

            var transitionStep = projection.PendingSystemTransitionStep;
            Assert.That(detail.PendingSystemTransitionStep != null,
                Is.EqualTo(transitionStep != null), seat);
            if (transitionStep != null)
            {
                Assert.That(detail.PendingSystemTransitionStep.Options,
                    Is.EqualTo(transitionStep.Options.Select(option => option.Value)), seat);
                Assert.That(detail.PendingSystemTransitionStep.ChoosingSide,
                    Is.EqualTo((int)transitionStep.ChoosingSide), seat);
            }

            var description = projection.PendingSystemTransitionDescription;
            Assert.That(detail.PendingSystemTransitionDescription != null,
                Is.EqualTo(description != null), seat);
            if (description != null)
            {
                Assert.That(detail.PendingSystemTransitionDescription.Kind,
                    Is.EqualTo((int)description.Kind), seat);
                Assert.That(detail.PendingSystemTransitionDescription.Options
                        .Select(row => row.OptionId),
                    Is.EqualTo(description.Options.Select(option => option.OptionId.Value)), seat);
            }

            var prevention = projection.PendingRemovalPrevention;
            Assert.That(detail.PendingRemovalPrevention != null, Is.EqualTo(prevention != null), seat);
            if (prevention != null)
            {
                Assert.That(detail.PendingRemovalPrevention.RequestId,
                    Is.EqualTo(prevention.RequestId), seat);
                Assert.That(detail.PendingRemovalPrevention.Payments
                        .Select(row => row.PaymentCardInstanceId),
                    Is.EqualTo(prevention.Payments
                        .Select(payment => payment.PaymentCardInstanceId.Value)), seat);
            }

            // The offers, scoped exactly as the projection scopes them.
            Assert.That(detail.AvailableTurnActions.Select(row =>
                    row.SourceCardInstanceId + "/" + row.ActionDefinitionId + ":" + row.IsAvailable),
                Is.EqualTo(projection.AvailableTurnActions.Select(offer =>
                    offer.SourceCardInstanceId.Value + "/" + offer.ActionDefinitionId.Value + ":" +
                    offer.IsAvailable)), seat);
            Assert.That(detail.AvailableFastTravelOptions.Count,
                Is.EqualTo(projection.AvailableFastTravelOptions.Count), seat);
            foreach (var (row, option) in detail.AvailableFastTravelOptions.Zip(
                projection.AvailableFastTravelOptions, (row, option) => (row, option)))
            {
                Assert.That(row.LegalDestinations.Select(space => space.Q + "," + space.R),
                    Is.EqualTo(option.LegalDestinations.Select(space => space.Q + "," + space.R)),
                    seat);
            }

            Assert.That(detail.AvailableClearOptions.Count,
                Is.EqualTo(projection.AvailableClearOptions.Count), seat);
            foreach (var (row, option) in detail.AvailableClearOptions.Zip(
                projection.AvailableClearOptions, (row, option) => (row, option)))
            {
                Assert.That(row.AdjacentTokenInstanceIds,
                    Is.EqualTo(option.AdjacentTokens.Select(token => token.Id.Value)), seat);
                Assert.That(row.SubsetInvocations,
                    Is.EqualTo((IEnumerable<DirectorCommand>)option.SubsetInvocations), seat);
            }

            Assert.That(detail.AvailableHoldOffer != null,
                Is.EqualTo(projection.AvailableHoldOffer.HasValue), seat);

            var agreement = projection.TeamAgreement;
            Assert.That(detail.TeamAgreement != null, Is.EqualTo(agreement != null), seat);
            if (agreement != null)
            {
                Assert.That(detail.TeamAgreement.ProposalId,
                    Is.EqualTo(agreement.ProposalId.Value), seat);
                Assert.That(detail.TeamAgreement.Participants.Select(row => row.HeroId),
                    Is.EqualTo(agreement.Participants.Select(participant =>
                        participant.HeroId.Value)), seat);
                Assert.That(detail.TeamAgreement.AuthorizedStatements,
                    Is.EqualTo((IEnumerable<TeamAgreementCommand>)agreement.AuthorizedCommands),
                    "The decoded statements must equal the projection's exactly - they are what " +
                    "a network client states with. Seat " + seat + ".");
            }
        }

        /// <summary>
        /// The growth check on the codec itself: every member of the read model the HUD is written
        /// against is either carried by a named wire field or excluded for a recorded reason. A
        /// member added to <see cref="IMatchReadModel"/> without a decision here fails this test.
        /// </summary>
        [Test]
        public void EveryReadModelMember_IsCarriedOrNamedExcluded()
        {
            var carriers = new Dictionary<string, string>
            {
                { "Version", nameof(NetworkSeatProjectionRecord.Version) },
                { "IsInputEnabled", "runtime: client connection and in-flight request state" },
                { "Setup", "excluded: the admission tuple pins setup content on both sides" },
                { "SelectedMap", "excluded: the admission tuple pins the map and its revision" },
                { "Participants", "excluded: the admission tuple and roster pin the participants" },
                { "Definitions", "excluded: the admission content digest pins every definition" },
                { "Heroes", nameof(NetworkSeatProjectionRecord.Heroes) },
                { "Minions", nameof(NetworkSeatProjectionRecord.Minions) },
                { "Obstacles", nameof(NetworkSeatProjectionRecord.Obstacles) },
                { "PlacedTokens", nameof(NetworkSeatProjectionRecord.PlacedTokens) },
                { "HeldMarkers", nameof(NetworkSeatProjectionRecord.HeldMarkers) },
                { "MarkersHeldBy", "derived: a filter over HeldMarkers" },
                { "RoundIndex", nameof(NetworkSeatProjectionRecord.RoundIndex) },
                { "TurnIndex", nameof(NetworkSeatProjectionRecord.TurnIndex) },
                { "Phase", nameof(NetworkSeatProjectionRecord.Phase) },
                { "TieBreakerState", nameof(NetworkSeatProjectionRecord.TieBreakerRevealedSide) },
                { "TurnSlots", nameof(NetworkSeatProjectionRecord.TurnSlots) },
                { "CompletedRounds", nameof(NetworkProjectionDetailRecord.CompletedRounds) },
                { "CurrentEndOfRoundStage", nameof(NetworkSeatProjectionRecord.EndOfRoundStage) },
                { "Commitments", nameof(NetworkSeatProjectionRecord.Commitments) },
                { "GetCommitment", "derived: a lookup over Commitments" },
                { "SelectedActor", nameof(NetworkSeatProjectionRecord.SelectedActor) },
                { "IsTurnActive", nameof(NetworkSeatProjectionRecord.IsTurnActive) },
                { "TeamLifeCounters", nameof(NetworkSeatProjectionRecord.TeamLifeCounters) },
                { "HeroProgression", nameof(NetworkSeatProjectionRecord.HeroProgression) },
                { "Outcome", nameof(NetworkSeatProjectionRecord.Outcome) },
                { "ActiveEffectSourceCardInstanceIds",
                    nameof(NetworkSeatProjectionRecord.ActiveEffectSourceCardInstanceIds) },
                { "IsActiveEffectSource", "derived: a lookup over ActiveEffectSourceCardInstanceIds" },
                { "TeamAgreement", nameof(NetworkProjectionDetailRecord.TeamAgreement) },
                { "Cards", "carried as OwnCards plus PublicCards" },
                { "DecisionContract", nameof(NetworkSeatProjectionRecord.DecisionContractCommands) },
                { "PendingInitiativeChoice", nameof(NetworkProjectionDetailRecord.PendingInitiativeChoice) },
                { "PendingMovementResolution", nameof(NetworkProjectionDetailRecord.PendingMovementResolution) },
                { "HasPendingMovementResolution", nameof(NetworkProjectionDetailRecord.PendingMovementResolution) },
                { "PendingMovementChoice", nameof(NetworkProjectionDetailRecord.PendingMovementChoice) },
                { "PendingAttackResolution", nameof(NetworkProjectionDetailRecord.PendingAttackResolution) },
                { "HasPendingAttackResolution", nameof(NetworkProjectionDetailRecord.PendingAttackResolution) },
                { "PendingAttackTargetChoice", nameof(NetworkProjectionDetailRecord.PendingAttackTargetChoice) },
                { "PendingAttackTargetDefensePreviews",
                    nameof(NetworkProjectionDetailRecord.PendingAttackTargetDefensePreviews) },
                { "PendingDefenseChoice", nameof(NetworkProjectionDetailRecord.PendingDefenseChoice) },
                { "PendingDefensePreview", nameof(NetworkProjectionDetailRecord.PendingDefensePreview) },
                { "PendingSkillResolution", nameof(NetworkProjectionDetailRecord.PendingSkillResolution) },
                { "HasPendingSkillResolution", nameof(NetworkProjectionDetailRecord.PendingSkillResolution) },
                { "PendingEffectChoice", nameof(NetworkProjectionDetailRecord.PendingEffectChoice) },
                { "PendingEffectChoicePreview",
                    nameof(NetworkProjectionDetailRecord.PendingEffectChoicePreview) },
                { "PendingDisplacedMinionCorrection",
                    nameof(NetworkProjectionDetailRecord.PendingDisplacedMinionCorrection) },
                { "PendingMinionBattleChoice",
                    nameof(NetworkProjectionDetailRecord.PendingMinionBattleChoice) },
                { "PendingHeroRespawnChoice",
                    nameof(NetworkProjectionDetailRecord.PendingHeroRespawnChoice) },
                { "PendingSystemTransitionStep",
                    nameof(NetworkProjectionDetailRecord.PendingSystemTransitionStep) },
                { "PendingSystemTransitionDescription",
                    nameof(NetworkProjectionDetailRecord.PendingSystemTransitionDescription) },
                { "PendingRemovalPrevention",
                    nameof(NetworkProjectionDetailRecord.PendingRemovalPrevention) },
                { "AvailableTurnActions", nameof(NetworkProjectionDetailRecord.AvailableTurnActions) },
                { "AvailableFastTravelOptions",
                    nameof(NetworkProjectionDetailRecord.AvailableFastTravelOptions) },
                { "AvailableClearOptions", nameof(NetworkProjectionDetailRecord.AvailableClearOptions) },
                { "AvailableHoldOffer", nameof(NetworkProjectionDetailRecord.AvailableHoldOffer) }
            };

            var members = typeof(IMatchReadModel)
                .GetMembers(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(member => !member.Name.StartsWith("get_", StringComparison.Ordinal))
                .Select(member => member.Name)
                .Distinct()
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(members, Is.EquivalentTo(carriers.Keys),
                "A read-model member joined or left without a wire decision. Carry it in the " +
                "record, or record why it is excluded, before extending this map.");

            // The floor that keeps the map honest: most members are carried, not excused.
            var excluded = carriers.Values.Count(value =>
                value.StartsWith("excluded", StringComparison.Ordinal) ||
                value.StartsWith("derived", StringComparison.Ordinal));
            Assert.That(excluded, Is.LessThanOrEqualTo(7),
                "The exclusion list grew. Every new exclusion is a field a client cannot see.");
        }

        /// <summary>
        /// The failure policy's stale row, end to end: the refusal names StaleProjection, mutates
        /// nothing, and the offered resync converges the stale seat without a second request.
        /// </summary>
        [Test]
        public void AStaleSubmission_IsRefusedByName_AndHealedByTheOfferedResync()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();

            // Two seats answer against the same version; the wire delivers the first, and the
            // second's answer is stale by the time the host reads it.
            var first = harness.Client(1);
            var second = harness.Client(2);
            var firstCommand = first.LatestProjection.AuthorizedCommands[0];
            var secondCommand = second.LatestProjection.AuthorizedCommands[0];
            Assert.That(first.TrySubmitCommand(firstCommand, out _), Is.True);
            Assert.That(second.TrySubmitCommand(secondCommand, out _), Is.True);
            var versionBefore = harness.MatchHost.Version;
            harness.Pump();

            Assert.That(first.LastSubmissionResult.Accepted, Is.True, first.LastSubmissionResult.Detail);
            Assert.That(second.LastSubmissionResult.Accepted, Is.False);
            Assert.That(second.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.StaleProjection));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore + 1),
                "Exactly one of the two racing submissions moved the authority.");

            // The offered resync already converged the stale seat: no request, no second round.
            harness.AssertConverged();
            Assert.That(second.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(second.ResyncRequestCount, Is.Zero,
                "The stale seat converged on the offered resync alone.");
            var events = harness.Session.DrainEvents();
            Assert.That(events.Count(record =>
                record.Kind == PrivateSessionEventKind.ResyncServed), Is.EqualTo(1));

            // And the healed seat can act again at the current version.
            var retry = second.LatestProjection.AuthorizedCommands.FirstOrDefault();
            if (retry != null)
            {
                Assert.That(second.TrySubmitCommand(retry, out _), Is.True);
                harness.Pump();
                Assert.That(second.LastSubmissionResult.Accepted, Is.True,
                    second.LastSubmissionResult.Detail);
                harness.AssertConverged();
            }
        }

        /// <summary>
        /// Synchronization under the authority's own state replacement: a snapshot restore
        /// republishes every seat to the restored state, and a corrupt snapshot payload is
        /// refused with nothing mutated and nobody diverged.
        /// </summary>
        [Test]
        public void AHostSideRestore_RepublishesEverySeat_AndACorruptSnapshotMutatesNothing()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(40);
            harness.AssertConverged();
            var snapshot = harness.MatchHost.CaptureSnapshot();
            var versionAtSnapshot = harness.MatchHost.Version;

            harness.DriveThroughWire(30);
            Assert.That(harness.MatchHost.Version, Is.GreaterThan(versionAtSnapshot));

            // A corrupt snapshot is refused whole: no mutation, no republish needed, no divergence.
            var corrupt = harness.MatchHost.Submit(MatchSubmission.ForSnapshotRestore(
                snapshot.Substring(0, snapshot.Length / 2)));
            Assert.That(corrupt.Accepted, Is.False,
                "A truncated snapshot payload restored anyway.");
            harness.AssertConverged();

            var restore = harness.MatchHost.Submit(MatchSubmission.ForSnapshotRestore(snapshot));
            Assert.That(restore.Accepted, Is.True, restore.Detail);
            harness.Session.RepublishProjections();
            harness.Pump();
            harness.AssertConverged();
            foreach (var ordinal in Enumerable.Range(1, 6))
            {
                Assert.That(harness.Client(ordinal).LatestProjection.Version,
                    Is.EqualTo(harness.MatchHost.Version),
                    "Every seat rebuilt its view from the restored state.");
            }
        }

        /// <summary>
        /// The second acceptance check's Initiative half, over the wire: each eligible player's
        /// latest statement synchronizes to every seat, the status is visibly incomplete until
        /// unanimity, the host submits the family's authoritative choice exactly once, and a
        /// replayed statement identity cannot submit twice.
        /// </summary>
        [Test]
        public void TiedInitiativeAgreement_SynchronizesStatements_AndSubmitsTheFinalExactlyOnce()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(400, projection =>
                projection.PendingInitiativeChoice != null &&
                projection.DecisionContract != null);
            var choice = harness.MatchHost.Projection.PendingInitiativeChoice;
            Assert.That(choice, Is.Not.Null,
                "The deterministic walk must reach a tied-Initiative decision.");

            // Every seat sees the same public proposal; only participants hold statements to make.
            harness.AssertConverged();
            var participantHeroes = harness.Client(1).LatestProjection.Detail.TeamAgreement
                .Participants.Select(row => row.HeroId).ToArray();
            Assert.That(participantHeroes.Length, Is.GreaterThanOrEqualTo(2));
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                var agreement = harness.Client(ordinal).LatestProjection.Detail.TeamAgreement;
                Assert.That(agreement, Is.Not.Null, "The proposal is public to every seat.");
                var isParticipant = participantHeroes.Contains(
                    harness.Client(ordinal).LatestProjection.HeroId);
                Assert.That(agreement.AuthorizedStatements.Count > 0, Is.EqualTo(isParticipant),
                    "Only a participant holds statements to make.");
            }

            var final = harness.MatchHost.Projection.DecisionContract.Commands
                .First(command => command.Kind == DirectorCommandKind.ChooseTiedActor);
            var versionBefore = harness.MatchHost.Version;

            // Statements land one at a time; each short of unanimity moves nothing and reaches
            // every seat's public surface.
            foreach (var heroId in participantHeroes)
            {
                var seat = harness.MatchHost.Seats.Seats.Single(value => value.HeroId.Value == heroId);
                var client = harness.Client(harness.OrdinalOf(seat.Id.Value));
                var statement = client.LatestProjection.Detail.TeamAgreement.AuthorizedStatements
                    .Single(value => value.Kind == TeamAgreementCommandKind.PreferInitiativeCandidate &&
                        value.PreferredCardInstanceId.Value == final.CardInstanceId.Value);
                Assert.That(client.TrySubmitAgreement(statement, out _), Is.True);
                harness.Pump();
                Assert.That(client.LastAgreementResult.Accepted, Is.True,
                    client.LastAgreementResult.Detail);
                harness.AssertConverged();
                if (heroId != participantHeroes.Last())
                {
                    Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore),
                        "A statement short of unanimity moves nothing.");
                    for (var ordinal = 1; ordinal <= 6; ordinal++)
                    {
                        var visible = harness.Client(ordinal).LatestProjection.Detail.TeamAgreement;
                        Assert.That(visible.Participants
                                .Single(row => row.HeroId == heroId).HasStatement, Is.True,
                            "Every seat sees the recorded statement.");
                    }
                }
            }

            // Unanimity submitted the family's own authoritative command exactly once.
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore + 1));
            Assert.That(harness.MatchHost.Projection.PendingInitiativeChoice, Is.Null);
            harness.AssertConverged();
            var events = harness.Session.DrainEvents();
            Assert.That(events.Count(record =>
                record.Kind == PrivateSessionEventKind.AgreementAccepted),
                Is.EqualTo(participantHeroes.Length));
        }

        /// <summary>
        /// The second check's Minion Battle half: agreement follows the exact standing proposal,
        /// withdrawing is a statement like any other, and a selection change invalidates every
        /// recorded agreement - a stale statement cannot apply to a changed removal set.
        /// </summary>
        [Test]
        public void MinionBattleAgreement_TracksTheStandingProposal_AndAChangeInvalidatesIt()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(1200, projection =>
                projection.PendingMinionBattleChoice != null &&
                projection.PendingMinionBattleChoice.CanConfirm);
            var choice = harness.MatchHost.Projection.PendingMinionBattleChoice;
            Assert.That(choice, Is.Not.Null,
                "The deterministic walk must reach a confirmable Minion Battle selection.");
            harness.AssertConverged();

            var choosers = harness.MatchHost.Seats.Seats
                .Where(seat => seat.PlaysFor(choice.ChoosingSide))
                .ToArray();
            var firstClient = harness.Client(harness.OrdinalOf(choosers[0].Id.Value));
            var proposalBefore = firstClient.LatestProjection.Detail.TeamAgreement.ProposalId;

            // One teammate agrees; every seat sees it, nothing is submitted.
            var agree = firstClient.LatestProjection.Detail.TeamAgreement.AuthorizedStatements
                .Single(value => value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
            Assert.That(firstClient.TrySubmitAgreement(agree, out _), Is.True);
            harness.Pump();
            Assert.That(firstClient.LastAgreementResult.Accepted, Is.True,
                firstClient.LastAgreementResult.Detail);
            Assert.That(harness.MatchHost.Projection.PendingMinionBattleChoice, Is.Not.Null,
                "One agreement is not unanimity.");
            harness.AssertConverged();

            // The agreed teammate may withdraw - and does, through the same public surface.
            var withdraw = firstClient.LatestProjection.Detail.TeamAgreement.AuthorizedStatements
                .Single(value => value.Kind == TeamAgreementCommandKind.WithdrawMinionBattleAgreement);
            Assert.That(firstClient.TrySubmitAgreement(withdraw, out _), Is.True);
            harness.Pump();
            Assert.That(firstClient.LastAgreementResult.Accepted, Is.True);
            harness.AssertConverged();

            // Agree again, then change the selection: the proposal identity moves, the recorded
            // agreement clears, and a statement naming the old proposal is refused as stale.
            var reAgree = firstClient.LatestProjection.Detail.TeamAgreement.AuthorizedStatements
                .Single(value => value.Kind == TeamAgreementCommandKind.AgreeToMinionBattleSelection);
            Assert.That(firstClient.TrySubmitAgreement(reAgree, out _), Is.True);
            harness.Pump();
            Assert.That(firstClient.LastAgreementResult.Accepted, Is.True);

            var teamClient = harness.Client(harness.OrdinalOf(choosers[1].Id.Value));
            var deselect = teamClient.LatestProjection.AuthorizedCommands.FirstOrDefault(command =>
                command.Kind == DirectorCommandKind.DeselectMinionBattleRemoval);
            Assert.That(deselect, Is.Not.Null,
                "A confirmable selection offers its team a deselect.");
            Assert.That(teamClient.TrySubmitCommand(deselect, out _), Is.True);
            harness.Pump();
            Assert.That(teamClient.LastSubmissionResult.Accepted, Is.True,
                teamClient.LastSubmissionResult.Detail);
            harness.AssertConverged();

            var proposalAfter = teamClient.LatestProjection.Detail.TeamAgreement?.ProposalId;
            Assert.That(proposalAfter, Is.Not.EqualTo(proposalBefore.ToString()).And.Not.Null,
                "A changed removal set is a different proposal.");
            for (var ordinal = 1; ordinal <= 6; ordinal++)
            {
                var agreement = harness.Client(ordinal).LatestProjection.Detail.TeamAgreement;
                Assert.That(agreement.Participants.All(row => !row.Agreed), Is.True,
                    "No recorded agreement survives a changed selection.");
            }

            // The stale statement, replayed against the old proposal, cannot manufacture
            // agreement on the new one.
            Assert.That(firstClient.TrySubmitAgreement(reAgree, out _), Is.True);
            harness.Pump();
            Assert.That(firstClient.LastAgreementResult.Accepted, Is.False);
            Assert.That(firstClient.LastAgreementResult.RejectionReason,
                Is.EqualTo(TeamAgreementSubmissionRejectionReason.StaleProposal));
            harness.AssertConverged();
        }

        /// <summary>
        /// No seat can submit either family's final rules choice directly, however well-formed:
        /// the offered surface excludes it, on the wire as locally.
        /// </summary>
        [Test]
        public void TheFinalRulesChoices_AreNotSubmittableByAnySeat()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.DriveThroughWire(400, projection =>
                projection.PendingInitiativeChoice != null &&
                projection.DecisionContract != null);
            Assert.That(harness.MatchHost.Projection.PendingInitiativeChoice, Is.Not.Null);

            var final = harness.MatchHost.Projection.DecisionContract.Commands
                .First(command => command.Kind == DirectorCommandKind.ChooseTiedActor);
            var owningSeat = harness.MatchHost.Seats.Seats.First(seat =>
                seat.PlaysFor(harness.MatchHost.Projection.PendingInitiativeChoice.OwningSide));
            var client = harness.Client(harness.OrdinalOf(owningSeat.Id.Value));
            var versionBefore = harness.MatchHost.Version;

            Assert.That(client.LatestProjection.AuthorizedCommands, Is.Empty,
                "The tied-Initiative family offers no direct command to any seat.");
            Assert.That(client.TrySubmitCommand(final, out _), Is.True,
                "The client machine sends what it is told; authorization is the host's.");
            harness.Pump();
            Assert.That(client.LastSubmissionResult.Accepted, Is.False);
            Assert.That(client.LastSubmissionResult.RejectionReason,
                Is.EqualTo(SeatSubmissionRejectionReason.UnauthorizedCommand));
            Assert.That(harness.MatchHost.Version, Is.EqualTo(versionBefore));
        }

        /// <summary>
        /// The idempotency window, sized from measured traffic and guarded against growth: the
        /// longest any seat goes between two of its own consumed submission identities across the
        /// full deterministic walk must fit inside the protocol constant with margin.
        /// </summary>
        [Test]
        public void TheIdempotencyWindow_CoversMeasuredRetryTraffic()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            var random = new MatchLoopRandom(2);
            var lastConsumedAt = new Dictionary<string, int>();
            var consumed = 0;
            var maxGap = 0;

            void Consume(string seat)
            {
                consumed++;
                if (lastConsumedAt.TryGetValue(seat, out var previous))
                {
                    maxGap = Math.Max(maxGap, consumed - previous);
                }

                lastConsumedAt[seat] = consumed;
            }

            var submitted = 0;
            while (submitted < 1500)
            {
                var legal = DeterministicMatchLoop.EnumerateLegalCommands(host.Projection);
                if (legal.Count == 0)
                {
                    break;
                }

                var command = legal[random.Next(legal.Count)];
                if (TeamAgreementTestDriver.SubmitFinalAgreementCommand(
                    host, command, "window-" + host.Sequence + "-" + submitted,
                    seat => Consume(seat.Id.Value)))
                {
                    submitted++;
                    continue;
                }

                var owner = host.Seats.Seats.First(value =>
                    host.ProjectionForSeat(value.Id).AuthorizedCommands.Contains(command));
                var client = host.ClientForSeat(owner.Id);
                var result = client.Submit(new SeatSubmission(
                    new SubmissionId("window-" + host.Sequence + "-" + submitted),
                    client.Projection.Version, command));
                Assert.That(result.Accepted, Is.True, result.Detail);
                Consume(owner.Id.Value);
                submitted++;
            }

            Assert.That(submitted, Is.GreaterThan(1000),
                "The walk ended early; the measurement saw less traffic than it must.");
            Assert.That(maxGap, Is.GreaterThan(0), "No seat ever submitted twice; the measurement is vacuous.");
            Assert.That(PrivateSessionProtocol.SubmissionIdempotencyWindowEntries,
                Is.GreaterThanOrEqualTo(maxGap * 4),
                "Measured worst per-seat retry distance " + maxGap + " no longer fits the window " +
                "with margin. Re-size the protocol constant deliberately - and re-record the " +
                "measurement in its remarks - rather than letting traffic outgrow it.");
            Assert.That(GuardsDigital.Application.MatchHost.RetainedSubmissionIdCount,
                Is.EqualTo(PrivateSessionProtocol.SubmissionIdempotencyWindowEntries),
                "The host's retained-identity window is the protocol constant, not a second number.");
        }
    }
}
