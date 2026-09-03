using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Replay;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35b. The network projection is rehydrated behind IMatchReadModel, so every
    /// established presenter receives the same meanings it receives locally and no transport DTO.
    /// </summary>
    public sealed class OnlineMatchHudTests
    {
        [Test]
        public void EveryWirePromptFamily_ReachesTheSharedReadModel_WithEquivalentMeaning()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var seen = new System.Collections.Generic.HashSet<string>();

            AssertAllSeats(harness, seen);
            // Milestone 8.35d re-derived the seed: with the round's advance no longer drawn by the
            // walk, seed 2 reaches the Attack target and the Minion Battle far outside this
            // budget and seed 3 reaches every family inside 268 steps.
            var submitted = harness.DriveThroughWire(700, afterAcceptedStep: () =>
                AssertAllSeats(harness, seen), seed: 3);

            Assert.That(submitted, Is.EqualTo(700));
            Assert.That(seen, Is.SupersetOf(new[]
            {
                "initiative", "movement", "attack-target", "effect", "correction",
                "minion-battle", "system-transition"
            }));

            AssertHost(new MatchHost(ConfiguredMatchFixtures.CreateSessionAwaitingADefenseResponse()));
            AssertHost(new MatchHost(HeroRespawnTests.CreatePendingRespawnSession()));
            AssertHost(new MatchHost(
                ConfiguredPendingChoiceProjectionTests.CreateConfiguredRemovalPreventionPause()));
        }

        [Test]
        public void ARecoveringOrWaitingClient_ImmediatelyPublishesNoActionableControls()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var local = harness.MatchHost.ProjectionForSeat(
                harness.MatchHost.Seats.Seats[0].Id);
            var bytes = NetworkSeatProjectionCodec.Encode(local);
            Assert.That(NetworkSeatProjectionCodec.TryDecode(bytes, out var record), Is.True);
            var online = new NetworkMatchReadModel(
                harness.MatchHost.Projection, MatchContentCatalogs.ForThisBuild());

            online.Replace(record, true);
            Assert.That(online.IsInputEnabled, Is.True);
            Assert.That(online.DecisionContract != null ||
                online.AvailableTurnActions.Count > 0 ||
                online.Commitments.Any(value =>
                    value.Status == TurnCardCommitmentStatus.AwaitingResponse), Is.True,
                "The positive fixture must expose some player progression surface.");

            online.Replace(record, false);
            Assert.That(online.IsInputEnabled, Is.False);
            Assert.That(online.DecisionContract, Is.Null);
            Assert.That(online.AvailableTurnActions, Is.Empty);
            Assert.That(online.AvailableFastTravelOptions, Is.Empty);
            Assert.That(online.AvailableClearOptions, Is.Empty);
            Assert.That(online.TeamAgreement, Is.Null);
            Assert.That(online.PendingInitiativeChoice, Is.Null);
            Assert.That(online.PendingMovementChoice, Is.Null);
            Assert.That(online.PendingAttackTargetChoice, Is.Null);
            Assert.That(online.PendingDefenseChoice, Is.Null);
            Assert.That(online.PendingEffectChoice, Is.Null);
            Assert.That(online.PendingDisplacedMinionCorrection, Is.Null);
            Assert.That(online.PendingMinionBattleChoice, Is.Null);
            Assert.That(online.PendingHeroRespawnChoice, Is.Null);
            Assert.That(online.PendingSystemTransitionStep, Is.Null);
            Assert.That(online.PendingRemovalPrevention, Is.Null);
        }

        [Test]
        public void OneInFlightPress_DisablesFurtherInput_AndReconnectUsesTheFreshProjection()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(1);

            Assert.That(client.TrySubmitCommand(DirectorCommand.AdvanceEndOfRound(), out _), Is.True);
            Assert.That(client.IsWaitingForHost, Is.True);
            Assert.That(client.TrySubmitCommand(DirectorCommand.AdvanceEndOfRound(), out _), Is.False,
                "A second press must not race the host answer for the first one.");

            client.OnDisconnected();
            Assert.That(client.BeginReconnect(), Is.True);
            Assert.That(client.IsWaitingForHost, Is.False,
                "The complete reconnect projection decides whether the prompt still exists.");
        }

        [Test]
        public void SameVersionProjectionReceipts_AdvanceThePresentationRevision()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            var client = harness.Client(1);
            var version = client.LatestProjection.Version;
            var revision = client.ProjectionRevision;

            harness.Session.RepublishProjections();
            harness.Pump();

            Assert.That(client.LatestProjection.Version, Is.EqualTo(version),
                "Republishing public/session state does not invent an authoritative gameplay step.");
            Assert.That(client.ProjectionRevision, Is.GreaterThan(revision),
                "Presentation still needs a monotonic signal for a newly received same-version view.");
        }

        [Test]
        public void OnlineFeedback_IsBounded_AndNamesRecoveryAndTerminalStates()
        {
            Assert.That(NetworkMatchFeedback.MaxLength, Is.LessThanOrEqualTo(96));
            Assert.That(NetworkMatchFeedback.For(null), Does.Contain("Not connected"));
            var rows = new[]
            {
                (PrivateSessionClientState.AwaitingChallenge, false, false, false, "", "", "Joining"),
                (PrivateSessionClientState.AwaitingSeat, true, false, false, "", "", "Reconnecting"),
                (PrivateSessionClientState.Seated, false, false, false, "", "", "Synchronizing"),
                (PrivateSessionClientState.Seated, false, true, true, "", "", "Waiting"),
                (PrivateSessionClientState.Seated, false, true, false, "stale", "", "refused"),
                (PrivateSessionClientState.Resynchronizing, false, true, false, "", "", "out of date"),
                (PrivateSessionClientState.HostLost, false, true, false, "", "", "cannot continue"),
                (PrivateSessionClientState.Disconnected, false, false, false, "", "", "Disconnected"),
                (PrivateSessionClientState.Rejected, false, false, false, "", "BuildMismatch", "BuildMismatch"),
                (PrivateSessionClientState.Faulted, false, false, false, "", "", "incompatible")
            };
            foreach (var row in rows)
            {
                var text = NetworkMatchFeedback.ForState(
                    row.Item1, row.Item2, row.Item3, row.Item4, row.Item5, row.Item6);
                Assert.That(text, Does.Contain(row.Item7));
                Assert.That(text.Length, Is.LessThanOrEqualTo(NetworkMatchFeedback.MaxLength));
            }
        }

        private static void AssertAllSeats(
            PrivateOnlineWireHarness harness,
            System.Collections.Generic.ISet<string> seen)
        {
            var family = harness.MatchHost.Projection.DecisionContract?.FamilyId.Value;
            if (!string.IsNullOrEmpty(family)) seen.Add(family);

            foreach (var seat in harness.MatchHost.Seats.Seats)
            {
                var local = harness.MatchHost.ProjectionForSeat(seat.Id);
                var bytes = NetworkSeatProjectionCodec.Encode(local);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(bytes, out var wire), Is.True);
                var online = new NetworkMatchReadModel(
                    harness.MatchHost.Projection, MatchContentCatalogs.ForThisBuild());
                online.Replace(wire, true);

                Assert.That(online.Version, Is.EqualTo(local.Version));
                Assert.That(online.Phase, Is.EqualTo(local.Phase));
                Assert.That(online.Heroes, Is.EqualTo(local.Heroes));
                Assert.That(online.Minions.Select(value => value.MinionId),
                    Is.EqualTo(local.Minions.Select(value => value.MinionId)));
                Assert.That(online.Cards.Select(value => value.Id),
                    Is.EquivalentTo(local.Cards.Select(value => value.Id)));
                Assert.That(online.DecisionContract != null,
                    Is.EqualTo(local.DecisionContract != null), seat.Id.Value);
                Assert.That(online.PendingInitiativeChoice != null,
                    Is.EqualTo(local.PendingInitiativeChoice != null), seat.Id.Value);
                Assert.That(online.HasPendingMovementResolution,
                    Is.EqualTo(local.HasPendingMovementResolution), seat.Id.Value);
                Assert.That(online.PendingMovementChoice != null,
                    Is.EqualTo(local.PendingMovementChoice != null), seat.Id.Value);
                Assert.That(online.HasPendingAttackResolution,
                    Is.EqualTo(local.HasPendingAttackResolution), seat.Id.Value);
                Assert.That(online.PendingAttackTargetChoice != null,
                    Is.EqualTo(local.PendingAttackTargetChoice != null), seat.Id.Value);
                Assert.That(online.PendingDefenseChoice != null,
                    Is.EqualTo(local.PendingDefenseChoice != null), seat.Id.Value);
                Assert.That(online.PendingDefensePreview != null,
                    Is.EqualTo(local.PendingDefensePreview != null), seat.Id.Value);
                Assert.That(online.HasPendingSkillResolution,
                    Is.EqualTo(local.HasPendingSkillResolution), seat.Id.Value);
                Assert.That(online.PendingEffectChoice != null,
                    Is.EqualTo(local.PendingEffectChoice != null), seat.Id.Value);
                Assert.That(online.PendingDisplacedMinionCorrection != null,
                    Is.EqualTo(local.PendingDisplacedMinionCorrection != null), seat.Id.Value);
                Assert.That(online.PendingMinionBattleChoice != null,
                    Is.EqualTo(local.PendingMinionBattleChoice != null), seat.Id.Value);
                Assert.That(online.PendingHeroRespawnChoice != null,
                    Is.EqualTo(local.PendingHeroRespawnChoice != null), seat.Id.Value);
                Assert.That(online.PendingSystemTransitionStep != null,
                    Is.EqualTo(local.PendingSystemTransitionStep != null), seat.Id.Value);
                Assert.That(online.PendingSystemTransitionDescription != null,
                    Is.EqualTo(local.PendingSystemTransitionDescription != null), seat.Id.Value);
                Assert.That(online.PendingRemovalPrevention != null,
                    Is.EqualTo(local.PendingRemovalPrevention != null), seat.Id.Value);
                Assert.That(online.TeamAgreement != null,
                    Is.EqualTo(local.TeamAgreement != null), seat.Id.Value);
            }
        }

        private static void AssertHost(IMatchHost host)
        {
            foreach (var seat in host.Seats.Seats)
            {
                var local = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(
                    NetworkSeatProjectionCodec.Encode(local), out var wire), Is.True);
                var online = new NetworkMatchReadModel(
                    host.Projection, MatchContentCatalogs.ForThisBuild());
                online.Replace(wire, true);
                Assert.That(online.PendingDefenseChoice != null,
                    Is.EqualTo(local.PendingDefenseChoice != null), seat.Id.Value);
                Assert.That(online.PendingDefensePreview != null,
                    Is.EqualTo(local.PendingDefensePreview != null), seat.Id.Value);
                Assert.That(online.PendingHeroRespawnChoice != null,
                    Is.EqualTo(local.PendingHeroRespawnChoice != null), seat.Id.Value);
                Assert.That(online.PendingRemovalPrevention != null,
                    Is.EqualTo(local.PendingRemovalPrevention != null), seat.Id.Value);
            }
        }

    }
}
