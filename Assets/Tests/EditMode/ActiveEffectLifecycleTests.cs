using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ActiveEffectLifecycleTests
    {
        private static readonly HeroId RedHero = new HeroId("red-hero-1");
        private static readonly HeroId BlueHero = new HeroId("blue-hero-1");
        private static readonly CardInstanceId SourceCard = new CardInstanceId("synthetic-timing-card");
        private static readonly ActionDefinitionId SourceAction = new ActionDefinitionId("synthetic-timing-action");

        [Test]
        public void Definition_RequiresCompleteNonContradictoryLifecycleMetadata()
        {
            Assert.Throws<ArgumentException>(() => new ActiveEffectDefinition(
                "bad", ActiveEffectAffectedScopeKind.None,
                ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Conditions(), false, "SyntheticModifier", "bad-unique", Array.Empty<TimingCheckpoint>()));
            Assert.Throws<ArgumentException>(() => new ActiveEffectDefinition(
                "bad", ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd,
                Conditions(), false, "SyntheticModifier", "bad-unique", Array.Empty<TimingCheckpoint>()));
            Assert.Throws<ArgumentException>(() => new ActiveEffectDefinition(
                "bad", ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.NextTurnAfterPlayingCards, TimingCheckpoint.ActionEnded,
                TimingCheckpoint.TurnStarted, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Conditions(), true, "SyntheticModifier", "bad-unique", Array.Empty<TimingCheckpoint>()));
        }

        [Test]
        public void ThisTurn_CreatedPartwayThroughTurn_ExpiresExactlyOnceAtTurnCheckpoint()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("this-turn"), Definition(
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false), SourceCard, SourceAction, 1, 2);

            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            var first = Apply(lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 1, 2, RedHero);
            var duplicate = Apply(lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 1, 2, RedHero);

            Assert.That(first.Select(value => value.Kind), Is.EqualTo(new[] { ActiveEffectLifecycleEventKind.Expired }));
            Assert.That(duplicate, Is.Empty);
            Assert.That(lifecycle.Effects.Single().EndedAt,
                Is.EqualTo(TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded));
        }

        [Test]
        public void ThisRound_SurvivesTurnsAndExpiresAtRoundEnd()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("this-round"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.RoundEnd,
                false), SourceCard, SourceAction, 3, 1);

            Assert.That(Apply(lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 3, 1, RedHero), Is.Empty);
            Assert.That(Apply(lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 3, 4, RedHero), Is.Empty);
            Assert.That(Apply(lifecycle, TimingCheckpoint.RoundEnd, 3, 4, RedHero).Single().Kind,
                Is.EqualTo(ActiveEffectLifecycleEventKind.Expired));
        }

        [Test]
        public void NextTurn_TargetsExactHeroAndHonorsCrossRoundPolicy()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("next-turn-carry"), Definition(
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                true), SourceCard, SourceAction, 1, 4);
            lifecycle.Create(new ActiveEffectId("next-turn-cancel"), Definition(
                ActiveEffectDurationKind.NextTurn,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.TurnStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false, "next-turn-cancel"), SourceCard, SourceAction, 1, 4);

            Assert.That(Apply(lifecycle, TimingCheckpoint.TurnStarted, 2, 1, BlueHero), Is.Empty,
                "Initiative order or another hero's turn must not retarget a dormant effect.");
            var roundEnd = Apply(lifecycle, TimingCheckpoint.RoundEnd, 1, 4, RedHero);
            Assert.That(roundEnd.Select(value => value.EffectId.Value), Is.EqualTo(new[] { "next-turn-cancel" }));
            Assert.That(roundEnd.Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Cancelled));

            var activation = Apply(lifecycle, TimingCheckpoint.TurnStarted, 2, 1, RedHero);
            Assert.That(activation.Single().EffectId.Value, Is.EqualTo("next-turn-carry"));
            Assert.That(activation.Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Activated));
            Assert.That(Apply(lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 2, 1, RedHero)
                .Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Expired));
        }

        [Test]
        public void NextTurnAfterPlayingCards_DoesNotActivateAtOrdinaryTurnStart()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("after-playing"), Definition(
                ActiveEffectDurationKind.NextTurnAfterPlayingCards,
                TimingCheckpoint.ActionEnded,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                true), SourceCard, SourceAction, 1, 1);

            Assert.That(Apply(lifecycle, TimingCheckpoint.TurnStarted, 1, 2, RedHero), Is.Empty);
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(Apply(lifecycle, TimingCheckpoint.CardsPlayedAndRevealed, 1, 2, RedHero)
                .Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Activated));
        }

        [Test]
        public void CancellationAndNormalExpiry_AreIdempotent()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("cancel-me"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                false), SourceCard, SourceAction, 1, 1);

            var cancellation = lifecycle.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.SourceCardStateChanged, 1, 1, sourceCardInstanceId: SourceCard),
                new TieBreakerState(TeamSide.Red));
            Assert.That(cancellation.Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Cancelled));
            Assert.That(Apply(lifecycle, TimingCheckpoint.AffectedHeroDefeated, 1, 1, RedHero), Is.Empty);
            Assert.That(Apply(lifecycle, TimingCheckpoint.RoundEnd, 1, 4, RedHero), Is.Empty);
        }

        [Test]
        public void SimultaneousOrdering_UsesTieBreakerSideWithoutFlippingIt()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("red"), Definition(
                ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd, false),
                SourceCard, SourceAction, 1, 1);
            lifecycle.Create(new ActiveEffectId("blue"), Definition(
                ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd, false,
                id: "blue-definition", controller: BlueHero, side: TeamSide.Blue),
                SourceCard, SourceAction, 1, 1);
            var tie = new TieBreakerState(TeamSide.Blue);

            var events = lifecycle.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.RoundEnd, 1, 4), tie);

            Assert.That(events.Select(value => value.EffectId.Value), Is.EqualTo(new[] { "blue", "red" }));
            Assert.That(tie.RevealedSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void ContinuousSources_RemainDistinctFromCardActiveEffectCancellation()
        {
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.AddContinuousSource(new ContinuousRuleSource(
                "ultimate-source", ContinuousRuleSourceKind.Ultimate, RedHero,
                "SyntheticPermission", "WhileControllerHeroOnBoard", SourceCard));
            lifecycle.Create(new ActiveEffectId("ordinary"), Definition(
                ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd, false),
                SourceCard, SourceAction, 1, 1);

            lifecycle.ApplyCheckpoint(new ActiveEffectCheckpointContext(
                TimingCheckpoint.SourceCardStateChanged, 1, 1, sourceCardInstanceId: SourceCard),
                new TieBreakerState(TeamSide.Red));

            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(lifecycle.ContinuousSources, Has.Count.EqualTo(1));
        }

        [Test]
        public void UniquenessKey_PreventsRepeatFromDuplicatingAnActiveEffect()
        {
            var lifecycle = new ActiveEffectLifecycle();
            var definition = Definition(ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd, false);
            lifecycle.Create(new ActiveEffectId("first"), definition, SourceCard, SourceAction, 1, 1);

            var duplicate = lifecycle.Create(new ActiveEffectId("repeat"), definition,
                SourceCard, SourceAction, 1, 1);

            Assert.That(duplicate.Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.DuplicateSuppressed));
            Assert.That(duplicate.EffectId.Value, Is.EqualTo("first"));
            Assert.That(lifecycle.Effects, Has.Count.EqualTo(1));
        }

        [Test]
        public void SnapshotRestoreReplayAndRetainedPrefixUndo_PreserveDormantAndActiveState()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redCard = configuration.RedHand.Instances[0];
            var redAction = configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(redCard.DefinitionReference))
                .ActionDefinitions[0].Id;
            // Milestone 6.2a: an Active effect that crosses the snapshot boundary must be one a
            // registered capability declares, because restore rebinds its semantics from the catalog
            // rather than rebuilding them from the payload. A fixture-local definition is exactly what
            // that boundary now refuses, so this case uses the catalog-declared synthetic effect.
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(
                new ActiveEffectId("durable-mark"),
                SyntheticEffectCapabilities.DurableDefinition,
                RedHero,
                TeamSide.Red,
                default,
                redCard.Id,
                redAction,
                1,
                1);
            lifecycle.AddContinuousSource(new ContinuousRuleSource(
                "durable-ultimate", ContinuousRuleSourceKind.Ultimate, RedHero,
                "SyntheticPermission", "WhileControllerHeroOnBoard", redCard.Id));
            var session = CreateSession(configuration, lifecycle);

            var dormantSnapshot = session.CaptureSnapshot();
            var restored = CreateSession(configuration, lifecycle);
            Assert.That(restored.RestoreSnapshot(dormantSnapshot).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(dormantSnapshot));

            CompleteCurrentTurnWithHolds(session);
            var redSecond = EligibleCard(session, TeamSide.Red);
            var blueSecond = EligibleCard(session, TeamSide.Blue);
            Assert.That(session.Execute(DirectorCommand.CommitCard(redSecond.OwnerId, redSecond.Id)).Accepted, Is.True);
            var beforeReveal = session.CaptureSnapshot();
            Assert.That(session.Execute(DirectorCommand.CommitCard(blueSecond.OwnerId, blueSecond.Id)).Accepted, Is.True);
            var activeSnapshot = session.CaptureSnapshot();
            Assert.That(activeSnapshot, Is.Not.EqualTo(beforeReveal));

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(beforeReveal));
            Assert.That(session.Execute(DirectorCommand.CommitCard(blueSecond.OwnerId, blueSecond.Id)).Accepted, Is.True);
            Assert.That(session.CaptureSnapshot(), Is.EqualTo(activeSnapshot),
                "Retained-prefix replay plus the same accepted command must reconstruct identical timing state.");

            Assert.That(RuleEventLogSnapshotEnvelope.TryDeserialize(activeSnapshot, out var envelope), Is.True);
            var decoded = AuthoritativeSnapshotSerializer.Deserialize(
                envelope.AuthoritativePayload,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);
            Assert.That(decoded.Round.Timing.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(decoded.Round.Timing.ContinuousSources.Single().SourceId, Is.EqualTo("durable-ultimate"));
        }

        [Test]
        public void RoundAuthority_EmitsLifecycleFactsBeforeTurnEnded()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redCard = configuration.RedHand.Instances.OrderBy(card =>
                configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value).First();
            var redAction = configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(redCard.DefinitionReference)).ActionDefinitions[0].Id;
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("turn-event"), Definition(
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false), new CardInstanceId("unrelated-timing-source"), redAction, 1, 1);
            lifecycle.Create(new ActiveEffectId("turn-end-event"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                false,
                "turn-end-definition",
                triggers: new[] { TimingCheckpoint.TurnEnded }),
                new CardInstanceId("unrelated-turn-end-source"), redAction, 1, 1);
            var session = CreateSession(configuration, lifecycle);

            CompleteCurrentTurnWithHolds(session);

            var turnEvents = session.Projection.RoundEvents.Where(value =>
                value.Kind == RoundResolutionEventKind.TurnResolutionProgressed).ToArray();
            var expired = Array.FindIndex(turnEvents, value =>
                value.TurnEvent.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.TurnEvent.ActiveEffectEvent.Kind == ActiveEffectLifecycleEventKind.Expired);
            var ended = Array.FindIndex(turnEvents, value =>
                value.TurnEvent.Kind == TurnResolutionEventKind.TurnEnded);
            var turnEndTrigger = Array.FindIndex(turnEvents, value =>
                value.TurnEvent.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.TurnEvent.ActiveEffectEvent.Kind == ActiveEffectLifecycleEventKind.Triggered &&
                value.TurnEvent.ActiveEffectEvent.Checkpoint == TimingCheckpoint.TurnEnded);
            Assert.That(expired, Is.GreaterThanOrEqualTo(0));
            Assert.That(turnEndTrigger, Is.GreaterThan(expired));
            Assert.That(ended, Is.GreaterThan(expired));
            Assert.That(ended, Is.GreaterThan(turnEndTrigger));
            Assert.That(session.Projection.DiagnosticEvents.Any(value =>
                value.EventType == "Timing.Expired" && value.Detail.Contains("TurnOnlyCancellationBeforeTurnEnded")),
                Is.True);
        }

        [Test]
        public void ResolvingSourceCard_CancelsItsActiveEffectThroughTurnAuthority()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
            var redCard = configuration.RedHand.Instances.OrderBy(card =>
                configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value).First();
            var redAction = configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(redCard.DefinitionReference)).ActionDefinitions[0].Id;
            var definition = Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.CardsPlayedAndRevealed,
                TimingCheckpoint.RoundEnd,
                false,
                "source-card-definition");
            var activeEffect = new ActiveEffectInstance(
                new ActiveEffectId("source-card-event"),
                definition.Definition,
                definition.ControllerHeroId,
                definition.ControllerTeamSide,
                definition.AffectedHeroId,
                redCard.Id,
                redAction,
                1,
                1,
                0,
                ActiveEffectPhase.Active,
                1,
                1);
            var lifecycle = ActiveEffectLifecycle.Restore(new ActiveEffectLifecycleRestoreState(
                new[] { activeEffect }, Array.Empty<ContinuousRuleSource>()));
            var definitions = configuration.CardDefinitions;
            var instances = configuration.CardInstances;
            var authority = new TurnResolutionAuthority(
                definitions,
                instances,
                configuration.Participants,
                configuration.TieBreakerState,
                null,
                lifecycle,
                1,
                1);
            Assert.That(authority.Timing, Is.SameAs(lifecycle));
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(
                instances.Single(card => card.Id.Equals(redCard.Id))), Is.True);
            var blueCard = configuration.BlueHand.Instances.OrderBy(card =>
                definitions.Single(candidate =>
                    candidate.Reference.Equals(card.DefinitionReference)).Initiative.Value).First();
            Assert.That(CardLifecycleAuthority.TryPlayFacedown(
                instances.Single(card => card.Id.Equals(blueCard.Id))), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(
                instances.Single(card => card.Id.Equals(redCard.Id))), Is.True);
            Assert.That(CardLifecycleAuthority.TryReveal(
                instances.Single(card => card.Id.Equals(blueCard.Id))), Is.True);
            Assert.That(authority.TryStart().Accepted, Is.True);

            while (!authority.SelectedActor.CardInstanceId.Equals(redCard.Id))
            {
                var otherHold = authority.TryPerformHold(authority.SelectedActor.CardInstanceId);
                Assert.That(otherHold.Accepted, Is.True);
                Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            }
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            var hold = authority.TryPerformHold(redCard.Id);

            Assert.That(hold.Accepted, Is.True);
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
            Assert.That(hold.Events.Any(value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Kind == ActiveEffectLifecycleEventKind.Cancelled &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.SourceCardStateChanged),
                Is.True);
        }

        [Test]
        public void HeroScopedEffect_ExpiresAtItsWindowWhileAnotherHeroActs()
        {
            // ER2 closing check for F-ER2-03 (expiration half). The current duration vocabulary
            // permits expiration only at the turn-only and round checkpoints (ValidateDuration),
            // so the roadmap's ActionEnded example is realized at the valid turn-only checkpoint;
            // the correction removes the hero filter from every expiration checkpoint, which
            // covers the 6.2a action-window durations when they arrive.
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("blue-window"), Definition(
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false,
                id: "blue-window",
                scope: ActiveEffectAffectedScopeKind.Hero,
                affectedHero: BlueHero), SourceCard, SourceAction, 1, 2);
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));

            var visitedWhileRedActs = Apply(
                lifecycle, TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded, 1, 2, RedHero);

            Assert.That(visitedWhileRedActs.Select(value => value.Kind),
                Is.EqualTo(new[] { ActiveEffectLifecycleEventKind.Expired }),
                "A Hero-scoped effect must reach its own expiration checkpoint regardless of which hero the checkpoint context names.");
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
        }

        [Test]
        public void SameUniquenessKey_DifferentControllers_Coexist()
        {
            // ER2 closing check for F-ER2-03 (uniqueness half): suppression compares the identity
            // scope the definition declares, not one global key namespace.
            var lifecycle = new ActiveEffectLifecycle();
            var red = lifecycle.Create(new ActiveEffectId("red-copy"), Definition(
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false,
                id: "shared-card-effect",
                controller: RedHero,
                side: TeamSide.Red), SourceCard, SourceAction, 1, 1);
            var blue = lifecycle.Create(new ActiveEffectId("blue-copy"), Definition(
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                false,
                id: "shared-card-effect",
                controller: BlueHero,
                side: TeamSide.Blue), SourceCard, SourceAction, 1, 1);

            Assert.That(red.Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Created));
            Assert.That(blue.Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Created),
                "Two controllers activating the same definition must coexist; uniqueness is scoped to the declared identity.");
            Assert.That(lifecycle.Effects.Count, Is.EqualTo(2));
        }

        [Test]
        public void AffectedHeroDefeated_CancellationStillRequiresTheMatchingHero()
        {
            // Guard: unfiltering expiration must not widen identity-targeted cancellation.
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("blue-anchored"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.RoundEnd,
                false,
                id: "blue-anchored",
                scope: ActiveEffectAffectedScopeKind.Hero,
                affectedHero: BlueHero), SourceCard, SourceAction, 1, 1);

            var wrongHero = Apply(lifecycle, TimingCheckpoint.AffectedHeroDefeated, 1, 2, RedHero);
            Assert.That(wrongHero, Is.Empty,
                "Another hero's defeat must not cancel a Hero-scoped effect anchored to a different hero.");
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));

            var matchingHero = Apply(lifecycle, TimingCheckpoint.AffectedHeroDefeated, 1, 2, BlueHero);
            Assert.That(matchingHero.Select(value => value.Kind),
                Is.EqualTo(new[] { ActiveEffectLifecycleEventKind.Cancelled }));
        }

        private static EffectSpec Definition(
            ActiveEffectDurationKind duration,
            TimingCheckpoint creation,
            TimingCheckpoint activation,
            TimingCheckpoint expiration,
            bool carries,
            string id = "synthetic-definition",
            HeroId controller = default,
            TeamSide side = TeamSide.Red,
            TimingCheckpoint[] triggers = null,
            ActiveEffectAffectedScopeKind scope = ActiveEffectAffectedScopeKind.ControllerHero,
            HeroId affectedHero = default)
        {
            if (!controller.IsDefined) controller = RedHero;
            return new EffectSpec(
                new ActiveEffectDefinition(id, scope, duration, creation, activation,
                    expiration, Conditions(), carries, "SyntheticModifier", id + "-unique",
                    triggers ?? Array.Empty<TimingCheckpoint>()),
                controller,
                side,
                affectedHero);
        }

        private static ActiveEffectCancellationCondition[] Conditions() => new[]
        {
            ActiveEffectCancellationCondition.SourceCardStateChanged,
            ActiveEffectCancellationCondition.AffectedHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved,
            ActiveEffectCancellationCondition.Explicit
        };

        private static System.Collections.Generic.IReadOnlyList<ActiveEffectLifecycleEvent> Apply(
            ActiveEffectLifecycle lifecycle, TimingCheckpoint checkpoint, int round, int turn, HeroId hero) =>
            lifecycle.ApplyCheckpoint(new ActiveEffectCheckpointContext(checkpoint, round, turn, hero),
                new TieBreakerState(TeamSide.Red));

        private static MatchSession CreateSession(
            PlaceholderOneVsOneTestConfiguration configuration,
            ActiveEffectLifecycle timing)
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
            return new MatchSession(new MatchSessionInitialState(
                scenario, setup, new[] { map }, initialTiming: timing));
        }

        private static void CompleteCurrentTurnWithHolds(MatchSession session)
        {
            var red = EligibleCard(session, TeamSide.Red);
            var blue = EligibleCard(session, TeamSide.Blue);
            Assert.That(session.Execute(DirectorCommand.CommitCard(red.OwnerId, red.Id)).Accepted, Is.True);
            Assert.That(session.Execute(DirectorCommand.CommitCard(blue.OwnerId, blue.Id)).Accepted, Is.True);
            while (session.Projection.IsTurnActive)
            {
                Assert.That(session.Execute(
                    DirectorCommand.PerformHold(session.Projection.SelectedActor.CardInstanceId)).Accepted, Is.True);
            }
        }

        private static CardInstance EligibleCard(MatchSession session, TeamSide side)
        {
            var hero = session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
            return session.Projection.Cards.Where(card => card.OwnerId.Equals(hero) &&
                    card.State.Location == CardLocation.Hand)
                .OrderBy(card => session.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
        }
    }
}
