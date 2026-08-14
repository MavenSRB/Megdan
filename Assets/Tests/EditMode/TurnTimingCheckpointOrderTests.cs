using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.1ap. Pins the ordered timing checkpoints an action raises, so moving emission from
    /// the turn dispatcher's post-hoc rewriter into the resolvers themselves is provably
    /// order-preserving rather than assumed to be.
    ///
    /// Every scenario runs on a session that carries a live Active effect. That is load-bearing: the
    /// placeholder configuration creates no Active effect, so <c>ApplyCheckpoint</c> returns an empty
    /// list at every checkpoint and a fixture built on it would observe nothing and pass either way.
    /// The effect here is Global-scoped so it is relevant to whichever hero a checkpoint names -
    /// notably the Defense-response checkpoint, which names the defender rather than the actor - and
    /// it triggers on every action-window checkpoint so each one becomes an observable event.
    /// </summary>
    public sealed class TurnTimingCheckpointOrderTests
    {
        private static readonly ActiveEffectId ObserverId = new ActiveEffectId("timing-order-observer");
        private static readonly CardInstanceId ObserverSourceCard =
            new CardInstanceId("timing-order-observer-card");
        private static readonly ActionDefinitionId ObserverSourceAction =
            new ActionDefinitionId("timing-order-observer-action");

        [Test]
        public void MovementAction_RaisesCheckpointsInOrder()
        {
            var session = CreateObservingSession();
            var observed = Observe(session, StartOfficialMapMovementFour);

            Assert.That(Describe(observed), Is.EqualTo(
                    "TurnStarted -> ActionStarted -> ActionEnded -> SourceCardStateChanged"),
                "Movement checkpoint order changed. Measured at 2e47c97 before the correction; the " +
                "correction moves where these are emitted from, not their order.");
        }

        [Test]
        public void AttackActionWithDefenseResponse_RaisesCheckpointsInOrder()
        {
            var session = CreateObservingAdjacentSession();
            var observed = Observe(session, DriveAttackWithDefenseResponse);

            Assert.That(Describe(observed), Is.EqualTo(
                    "ActionStarted -> BeforeAttack -> DefenseResponse -> AttackResultComparison -> " +
                    "AfterAttack -> ActionEnded -> SourceCardStateChanged"),
                "Attack-with-Defense checkpoint order changed. Measured at 2e47c97 before the correction.");
        }

        [Test]
        public void AttackActionAgainstAMinion_RaisesCheckpointsInOrder()
        {
            var session = CreateObservingAdjacentSession(withBlueMinionBetween: true);
            var observed = Observe(session, DriveAttackAgainstMinion);

            Assert.That(Describe(observed), Is.EqualTo(
                    "ActionStarted -> BeforeAttack -> AfterAttack -> ActionEnded -> SourceCardStateChanged"),
                "Attack-against-minion checkpoint order changed. Measured at 2e47c97 before the " +
                "correction. A minion opens no Defense reaction, so neither DefenseResponse nor " +
                "AttackResultComparison appears here - that difference is the point of this case.");
        }

        [Test]
        public void FastTravelAction_RaisesCheckpointsInOrder()
        {
            var session = CreateObservingSession();
            var observed = Observe(session, DriveFastTravel);

            Assert.That(Describe(observed), Is.EqualTo(
                    "TurnStarted -> ActionStarted -> ActionEnded -> SourceCardStateChanged"),
                "Fast Travel checkpoint order changed. Measured at 2e47c97 before the correction.");
        }

        /// <summary>
        /// Milestone 6.1ap's one deliberate behavior change, and the only scenario that can observe it.
        ///
        /// The emitted event order is identical before and after the correction, because the deleted
        /// rewriter spliced the action-end checkpoint back next to the completion fact. The order the
        /// checkpoints were actually *applied* to the Active-effect lifecycle was the inverse: source-card
        /// cancellation ran during resolution, action end ran afterwards from the Initiative advance.
        ///
        /// An effect anchored to the acting card, cancelled by that card's state change and triggering
        /// at action end, distinguishes the two. Applied in the corrected order it triggers and is then
        /// cancelled; applied in the old order it was already Ended and action end passed over it in
        /// silence. `Card-System/RESOLUTION_AND_EFFECTS.md` specifies the corrected order.
        /// </summary>
        [Test]
        public void ActionEndCheckpoint_ReachesAnEffectBeforeItsSourceCardCancellation()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var actingCardId = ActingCardId(configuration);
            var session = CreateObservingSession(configuration, sourceCardInstanceId: actingCardId);

            var raw = new List<ActiveEffectLifecycleEvent>();
            StartOfficialMapMovementFour(session, raw);
            var observed = DescribeLifecycle(raw);

            Assert.That(observed, Does.Contain("Triggered@ActionEnded"),
                "The action-end checkpoint no longer reaches an effect anchored to the acting card, " +
                "which means source-card cancellation ran first - the pre-6.1ap order.");
            Assert.That(
                IndexOf(observed, "Triggered@ActionEnded"),
                Is.LessThan(IndexOf(observed, "Cancelled@SourceCardStateChanged")),
                "Action end must reach the Active-effect lifecycle before source-card cancellation.");
        }

        /// <summary>
        /// Guards the fixture itself: if the observing effect ever stops producing events, every
        /// order assertion above would pass on an empty sequence.
        /// </summary>
        [Test]
        public void ObservingSession_ActuallyCarriesALiveActiveEffect()
        {
            var session = CreateObservingSession();
            var observed = Observe(session, StartOfficialMapMovementFour);

            Assert.That(observed, Is.Not.Empty,
                "The observing Active effect produced no checkpoint event, so every order assertion " +
                "in this fixture would be vacuous. Restore the effect rather than relaxing the check.");
        }

        private static int IndexOf(IReadOnlyList<string> observed, string value)
        {
            for (var index = 0; index < observed.Count; index++)
            {
                if (string.Equals(observed[index], value, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            Assert.Fail($"The observed lifecycle sequence never contains '{value}': " +
                string.Join(" -> ", observed));
            return -1;
        }

        private static string Describe(IEnumerable<ActiveEffectLifecycleEvent> observed) =>
            string.Join(" -> ", observed.Select(value => value.Checkpoint));

        /// <summary>Kind and checkpoint together, for the one case where which event matters.</summary>
        private static IReadOnlyList<string> DescribeLifecycle(
            IEnumerable<ActiveEffectLifecycleEvent> observed) =>
            observed.Select(value => $"{value.Kind}@{value.Checkpoint}").ToArray();

        /// <summary>
        /// Records what was measured beside the assertion, the way the invariance fixture writes its
        /// measured digests, because a changed order is far easier to diff from a file than from a
        /// truncated assertion message.
        /// </summary>
        private static IReadOnlyList<ActiveEffectLifecycleEvent> Observe(
            MatchSession session,
            Action<MatchSession, List<ActiveEffectLifecycleEvent>> drive,
            [System.Runtime.CompilerServices.CallerMemberName] string scenario = null)
        {
            var observed = new List<ActiveEffectLifecycleEvent>();
            drive(session, observed);
            TryRecordMeasured(scenario, observed);
            return observed;
        }

        private static void TryRecordMeasured(
            string scenario,
            IEnumerable<ActiveEffectLifecycleEvent> observed)
        {
            try
            {
                var directory = System.IO.Path.Combine(
                    TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "tmp");
                System.IO.Directory.CreateDirectory(directory);
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(directory, "milestone-6-1ap-measured-checkpoints.txt"),
                    $"{scenario}: {Describe(observed)}{Environment.NewLine}");
            }
            catch (System.IO.IOException)
            {
                // Diagnostic only. A read-only or unavailable path must not fail the assertion.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void Record(MatchCommandResult result, List<ActiveEffectLifecycleEvent> observed)
        {
            Assert.That(result.Accepted, Is.True, result.Detail);
            foreach (var roundEvent in result.Events)
            {
                if (roundEvent.Kind == RoundResolutionEventKind.TimingProgressed)
                {
                    observed.Add(roundEvent.ActiveEffectEvent);
                    continue;
                }

                if (roundEvent.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    roundEvent.TurnEvent.Kind == TurnResolutionEventKind.TimingProgressed)
                {
                    observed.Add(roundEvent.TurnEvent.ActiveEffectEvent);
                }
            }
        }

        private static void StartOfficialMapMovementFour(
            MatchSession session,
            List<ActiveEffectLifecycleEvent> observed)
        {
            var redCard = GetCardByInitiative(session, TeamSide.Red, 5);
            var blueCard = GetCardByInitiative(session, TeamSide.Blue, 8);
            Record(session.Execute(DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id)), observed);
            Record(session.Execute(DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id)), observed);
            var movement = GetCurrentAction(session, ActionKind.Movement);
            Record(session.Execute(DirectorCommand.StartAction(movement.ActionDefinitionId)), observed);
            Record(session.Execute(DirectorCommand.StopMovement()), observed);
        }

        /// <summary>
        /// The first turn only manoeuvres the heroes into range and is deliberately not recorded; the
        /// pinned sequence is the Attack turn alone, so an unrelated change to how the heroes close
        /// distance cannot move this expectation.
        /// </summary>
        private static void DriveAttackWithDefenseResponse(
            MatchSession session,
            List<ActiveEffectLifecycleEvent> observed)
        {
            var red = GetCards(session, TeamSide.Red);
            var blue = GetCards(session, TeamSide.Blue);

            Commit(session, red[3], blue[3]);
            StartCurrentAction(session, ActionKind.Movement);
            var stopped = session.Projection.PendingMovementChoice;
            Execute(session, DirectorCommand.StopMovement(stopped.ResolutionId, stopped.ChoiceId));
            HoldCurrentActor(session);

            Commit(session, red[2], blue[2]);

            var attack = GetCurrentAction(session, ActionKind.Attack);
            Record(session.Execute(DirectorCommand.StartAction(attack.ActionDefinitionId)), observed);

            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(targetChoice, Is.Not.Null, "The Attack did not request a target choice.");
            Record(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetHeroIds.Single())), observed);

            var defenseChoice = session.Projection.PendingDefenseChoice;
            Assert.That(defenseChoice, Is.Not.Null, "The Attack did not request a Defense response.");
            Record(session.Execute(DirectorCommand.ChooseDefenseCard(
                defenseChoice.ResolutionId,
                defenseChoice.ChoiceId,
                defenseChoice.LegalDefenseOffers.First().SourceCardInstanceId)), observed);
        }

        /// <summary>
        /// A minion never opens a Defense reaction, so this path exercises the arms the hero-versus-hero
        /// sequence does not: no Defense-response checkpoint and no result-comparison checkpoint.
        /// Only the Red hero stands adjacent to the Blue minion, so the fixture Holds until Red is the
        /// selected actor rather than assuming an Initiative outcome.
        /// </summary>
        private static void DriveAttackAgainstMinion(MatchSession session, List<ActiveEffectLifecycleEvent> observed)
        {
            var redHeroId = session.Projection.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Red).HeroId;
            Commit(session, GetCards(session, TeamSide.Red)[2], GetCards(session, TeamSide.Blue)[2]);

            while (!session.Projection.SelectedActor.ActorId.Equals(redHeroId))
            {
                Assert.That(session.Projection.IsTurnActive, Is.True,
                    "The turn ended before the Red hero could act.");
                HoldCurrentActor(session);
            }

            Assert.That(TryFindAvailableAttack(session, out var chosen), Is.True,
                "The Red hero has no available Attack against the adjacent minion.");
            Record(session.Execute(DirectorCommand.StartAction(chosen.ActionDefinitionId)), observed);

            var targetChoice = session.Projection.PendingAttackTargetChoice;
            Assert.That(targetChoice, Is.Not.Null, "The Attack did not request a target choice.");
            Assert.That(targetChoice.LegalTargetMinionIds, Is.Not.Empty,
                "No minion was a legal target, so this fixture would pin a hero Attack instead.");
            Record(session.Execute(DirectorCommand.ChooseAttackTarget(
                targetChoice.ResolutionId,
                targetChoice.ChoiceId,
                targetChoice.LegalTargetMinionIds.First())), observed);
        }

        private static bool TryFindAvailableAttack(MatchSession session, out ActionOffer attack)
        {
            foreach (var offer in session.Projection.AvailableTurnActions)
            {
                if (!offer.IsAvailable || KindOf(session, offer) != ActionKind.Attack)
                {
                    continue;
                }

                attack = offer;
                return true;
            }

            attack = default;
            return false;
        }

        private static ActionKind KindOf(MatchSession session, ActionOffer offer) =>
            session.Projection.Definitions
                .Single(value => value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(offer.ActionDefinitionId)).Kind;

        private static void Execute(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static void Commit(MatchSession session, CardInstance red, CardInstance blue)
        {
            Execute(session, DirectorCommand.CommitCard(red.OwnerId, red.Id));
            Execute(session, DirectorCommand.CommitCard(blue.OwnerId, blue.Id));
            var choice = session.Projection.PendingInitiativeChoice;
            if (choice != null)
            {
                Execute(session, DirectorCommand.ChooseTiedActor(
                    choice.OwningSide, choice.CandidateCardInstanceIds[0]));
            }
        }

        private static void StartCurrentAction(MatchSession session, ActionKind actionKind) =>
            Execute(session, DirectorCommand.StartAction(GetCurrentAction(session, actionKind).ActionDefinitionId));

        private static void HoldCurrentActor(MatchSession session) =>
            Execute(session, DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId));

        private static IReadOnlyList<CardInstance> GetCards(MatchSession session, TeamSide side) =>
            session.Projection.Cards
                .Where(card => session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                .ToArray();

        private static void DriveFastTravel(MatchSession session, List<ActiveEffectLifecycleEvent> observed)
        {
            var redCard = GetCardByInitiative(session, TeamSide.Red, 5);
            var blueCard = GetCardByInitiative(session, TeamSide.Blue, 8);
            Record(session.Execute(DirectorCommand.CommitCard(redCard.OwnerId, redCard.Id)), observed);
            Record(session.Execute(DirectorCommand.CommitCard(blueCard.OwnerId, blueCard.Id)), observed);

            var option = session.Projection.AvailableFastTravelOptions[0];
            Record(session.Execute(DirectorCommand.PerformFastTravel(
                option.ReplacedMovementActionDefinitionId, option.LegalDestinations[0])), observed);
        }

        /// <summary>
        /// The card the Movement scenario's actor plays: Blue's Initiative-8 card, which
        /// <see cref="StartOfficialMapMovementFour"/> commits and which wins the Initiative.
        /// </summary>
        private static CardInstanceId ActingCardId(PlaceholderOneVsOneTestConfiguration configuration)
        {
            var definitionsByReference = configuration.CardDefinitions
                .ToDictionary(definition => definition.Reference);
            var blueHeroIds = configuration.BlueHand.Instances
                .Select(instance => instance.OwnerId)
                .ToArray();
            return configuration.CardInstances
                .Where(instance => blueHeroIds.Contains(instance.OwnerId))
                .Single(instance =>
                    definitionsByReference[instance.DefinitionReference].Initiative.Value == 8)
                .Id;
        }

        private static MatchSession CreateObservingSession() =>
            CreateObservingSession(
                PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue),
                ObserverSourceCard);

        private static MatchSession CreateObservingSession(
            PlaceholderOneVsOneTestConfiguration configuration,
            CardInstanceId sourceCardInstanceId)
        {
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());

            var controller = setup.Heroes.First(hero => hero.TeamSide == TeamSide.Red).HeroId;
            var timing = new ActiveEffectLifecycle();
            timing.Create(
                ObserverId,
                ObservingDefinition(controller),
                sourceCardInstanceId,
                ObserverSourceAction,
                1,
                1);

            return new MatchSession(new MatchSessionInitialState(
                scenario, setup, new[] { map }, initialTiming: timing));
        }

        /// <summary>
        /// The three-space synthetic board the session command-family fixture uses, so heroes can reach
        /// Attack range in one Movement. Carries the same observing effect.
        /// </summary>
        private static MatchSession CreateObservingAdjacentSession(bool withBlueMinionBetween = false)
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Blue);
            var map = BoardMapDefinition.CreateSynthetic(
                new MapId("timing-checkpoint-order-fixture"),
                1,
                new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0), new HexCoordinate(2, 0) });
            var redHeroId = configuration.RedHand.Instances[0].OwnerId;
            var blueHeroId = configuration.BlueHand.Instances[0].OwnerId;
            var redTeam = configuration.Participants.GetTeamForHero(redHeroId);
            var blueTeam = configuration.Participants.GetTeamForHero(blueHeroId);
            var redCoordinate = new HexCoordinate(0, 0);
            var blueCoordinate = new HexCoordinate(2, 0);
            var setup = new QuickGameSetupState(
                map.Reference,
                3,
                configuration.TieBreakerState,
                new[]
                {
                    new QuickGameTeamSetupState(redTeam.Id, TeamSide.Red, 4, new ZoneId("fixture-red-zone")),
                    new QuickGameTeamSetupState(blueTeam.Id, TeamSide.Blue, 4, new ZoneId("fixture-blue-zone"))
                },
                new[]
                {
                    new QuickGameHeroSetupState(
                        redHeroId,
                        new HeroContentId("synthetic-red"),
                        "Synthetic Red",
                        redTeam.Id,
                        TeamSide.Red,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-red-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("fixture-red-space")),
                        redCoordinate),
                    new QuickGameHeroSetupState(
                        blueHeroId,
                        new HeroContentId("synthetic-blue"),
                        "Synthetic Blue",
                        blueTeam.Id,
                        TeamSide.Blue,
                        new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-blue-spawn")),
                        new ConfiguredSpaceReference(map.Id, new SpaceId("fixture-blue-space")),
                        blueCoordinate)
                },
                withBlueMinionBetween
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            new MinionId("timing-order-minion"),
                            blueTeam.Id,
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-blue-spawn")),
                            new ConfiguredSpaceReference(map.Id, new SpaceId("q1_r0")),
                            new HexCoordinate(1, 0))
                    }
                    : Array.Empty<QuickGameMinionSetupState>());
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                map.Reference,
                new[]
                {
                    new HeroOccupancy(redHeroId, redCoordinate),
                    new HeroOccupancy(blueHeroId, blueCoordinate)
                },
                Array.Empty<ObstacleOccupancy>());

            var timing = new ActiveEffectLifecycle();
            timing.Create(
                ObserverId, ObservingDefinition(redHeroId), ObserverSourceCard, ObserverSourceAction, 1, 1);

            return new MatchSession(new MatchSessionInitialState(
                scenario, setup, new[] { map }, initialTiming: timing));
        }

        /// <summary>
        /// Global scope so no checkpoint is filtered out by which hero it names, and This Round duration
        /// so it survives the whole turn.
        ///
        /// It declares source-card cancellation, but that only fires for an effect whose source card is
        /// the one whose state changed. Every scenario except
        /// <see cref="ActionEndCheckpoint_ReachesAnEffectBeforeItsSourceCardCancellation"/> anchors it to
        /// a card that is not in the match, so nothing an ordinary action does ends it early; that one
        /// scenario anchors it to the acting card precisely so the cancellation does fire.
        /// </summary>
        private static ActiveEffectDefinition ObservingDefinition(HeroId controller) =>
            new ActiveEffectDefinition(
                "timing-order-observer-definition",
                controller,
                TeamSide.Red,
                ActiveEffectAffectedScopeKind.Global,
                default,
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.RoundEnd,
                new[]
                {
                    ActiveEffectCancellationCondition.Explicit,
                    ActiveEffectCancellationCondition.SourceCardStateChanged
                },
                false,
                "SyntheticTimingObserver",
                "timing-order-observer-unique",
                new[]
                {
                    TimingCheckpoint.TurnStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.BeforeAttack,
                    TimingCheckpoint.DefenseResponse,
                    TimingCheckpoint.AttackResultComparison,
                    TimingCheckpoint.AfterAttack,
                    TimingCheckpoint.ActionEnded,
                    TimingCheckpoint.SourceCardStateChanged,
                    TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                    TimingCheckpoint.TurnEnded
                });

        private static CardInstance GetCardByInitiative(MatchSession session, TeamSide side, int initiative) =>
            session.Projection.Cards
                .Where(card => session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side)
                .Single(card => session.Projection.Definitions.Single(
                    definition => definition.Reference.Equals(card.DefinitionReference)).Initiative.Value == initiative);

        private static ActionOffer GetCurrentAction(MatchSession session, ActionKind actionKind) =>
            session.Projection.AvailableTurnActions.Single(offer =>
            {
                var definition = session.Projection.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference));
                return definition.ActionDefinitions.Single(value =>
                    value.Id.Equals(offer.ActionDefinitionId)).Kind == actionKind;
            });
    }
}
