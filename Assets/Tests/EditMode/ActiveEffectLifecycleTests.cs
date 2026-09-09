using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
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
            Assert.That(Apply(lifecycle, TimingCheckpoint.ControllerHeroDefeated, 1, 1, RedHero), Is.Empty);
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
        public void AnEffectDeclaringATriggerWindow_RunsItsDeclaredEffectWhenTheCheckpointArrives()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var sourceCard = configuration.RedHand.Instances[0];
            var sourceAction = configuration.CardDefinitions.Single(definition =>
                    definition.Reference.Equals(sourceCard.DefinitionReference))
                .ActionDefinitions[0];
            var triggeredEffect = new EffectDefinition(
                new EffectDefinitionId("synthetic-triggered-mark"),
                "rev-1",
                new EffectNodeId("mark"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("mark"),
                        EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.MarkActorV1,
                        outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) })
                });
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("trigger-source"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                false,
                "trigger-source-definition",
                triggers: new[] { TimingCheckpoint.ActionEnded },
                controller: sourceCard.OwnerId,
                triggeredEffect: triggeredEffect.Reference),
                sourceCard.Id, sourceAction.Id, 1, 1);
            var authority = new TriggeredEffectResolutionAuthority(
                new TriggeredEffectCatalog(new[] { triggeredEffect }),
                SyntheticEffectCapabilities.CreateCatalog());

            var events = authority.ApplyCheckpoint(
                lifecycle,
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1, sourceCard.OwnerId),
                new TieBreakerState(TeamSide.Red),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                RuleModificationSet.Empty);

            Assert.That(events.Select(value => value.Kind),
                Is.EqualTo(new[] { ActiveEffectLifecycleEventKind.Triggered }));
            Assert.That(lifecycle.Effects, Has.Count.EqualTo(2),
                "The Triggered visit must execute the effect declared by its source, not only emit a lifecycle fact.");
        }

        [Test]
        public void ActiveAndContinuousTriggers_UseTieBreakerOrderWithoutFlippingIt()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Blue);
            var redCard = configuration.RedHand.Instances[0];
            var blueCard = configuration.BlueHand.Instances[0];
            var redDefinition = configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(redCard.DefinitionReference));
            var blueDefinition = configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(blueCard.DefinitionReference));
            var redAction = redDefinition.ActionDefinitions[0];
            var blueAction = blueDefinition.ActionDefinitions[0];
            var triggeredEffect = MarkTriggeredEffect("ordered-trigger");
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("red-active-trigger"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                false,
                "red-active-trigger-definition",
                controller: redCard.OwnerId,
                side: TeamSide.Red,
                triggers: new[] { TimingCheckpoint.ActionEnded },
                triggeredEffect: triggeredEffect.Reference), redCard.Id, redAction.Id, 1, 1);
            var continuous = new ContinuousTriggeredEffectSource(
                "blue-continuous-trigger",
                blueCard.OwnerId,
                TeamSide.Blue,
                blueCard.Id,
                blueDefinition.Reference,
                blueAction.Id,
                blueAction.Kind,
                triggeredEffect.Reference,
                new[] { TimingCheckpoint.ActionEnded });
            var tie = new TieBreakerState(TeamSide.Blue);
            var authority = new TriggeredEffectResolutionAuthority(
                new TriggeredEffectCatalog(new[] { triggeredEffect }),
                SyntheticEffectCapabilities.CreateCatalog());

            authority.ApplyCheckpoint(
                lifecycle,
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                tie,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                RuleModificationSet.Empty,
                continuousSources: new[] { continuous });

            Assert.That(lifecycle.Effects.Skip(1).Select(value => value.ControllerHeroId),
                Is.EqualTo(new[] { blueCard.OwnerId, redCard.OwnerId }));
            Assert.That(tie.RevealedSide, Is.EqualTo(TeamSide.Blue));
        }

        [Test]
        public void CancelledOrExpiredActiveEffect_DoesNotExecuteItsTrigger()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var sourceCard = configuration.RedHand.Instances[0];
            var sourceDefinition = configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(sourceCard.DefinitionReference));
            var sourceAction = sourceDefinition.ActionDefinitions[0];
            var triggeredEffect = MarkTriggeredEffect("cancelled-trigger");
            var authority = new TriggeredEffectResolutionAuthority(
                new TriggeredEffectCatalog(new[] { triggeredEffect }),
                SyntheticEffectCapabilities.CreateCatalog());

            foreach (var checkpoint in new[]
                     {
                         TimingCheckpoint.SourceCardStateChanged,
                         TimingCheckpoint.ControllerHeroDefeated,
                         TimingCheckpoint.RoundEnd
                     })
            {
                var lifecycle = new ActiveEffectLifecycle();
                lifecycle.Create(new ActiveEffectId("source-" + checkpoint), Definition(
                    ActiveEffectDurationKind.ThisRound,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.RoundEnd,
                    false,
                    "source-definition-" + checkpoint,
                    controller: sourceCard.OwnerId,
                    triggers: new[] { TimingCheckpoint.ActionEnded },
                    triggeredEffect: triggeredEffect.Reference), sourceCard.Id, sourceAction.Id, 1, 1);
                var context = new ActiveEffectCheckpointContext(
                    checkpoint,
                    1,
                    1,
                    affectedHeroId: sourceCard.OwnerId,
                    sourceCardInstanceId: sourceCard.Id);

                authority.ApplyCheckpoint(
                    lifecycle, context, new TieBreakerState(TeamSide.Red),
                    configuration.CardDefinitions, configuration.CardInstances,
                    configuration.Participants, RuleModificationSet.Empty);
                authority.ApplyCheckpoint(
                    lifecycle,
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 2, 1, sourceCard.OwnerId),
                    new TieBreakerState(TeamSide.Red),
                    configuration.CardDefinitions, configuration.CardInstances,
                    configuration.Participants, RuleModificationSet.Empty);

                Assert.That(lifecycle.Effects, Has.Count.EqualTo(1), checkpoint.ToString());
                Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
            }
        }

        [Test]
        public void TriggeredEffectThatPauses_UsesTheExistingSerializableEffectFrame()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var sourceCard = configuration.RedHand.Instances[0];
            var sourceDefinition = configuration.CardDefinitions.Single(value =>
                value.Reference.Equals(sourceCard.DefinitionReference));
            var sourceAction = sourceDefinition.ActionDefinitions[0];
            var choiceEffect = new EffectDefinition(
                new EffectDefinitionId("paused-trigger"), "rev-1", new EffectNodeId("choose"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("choose"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.ChooseHeroV1,
                        outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) })
                });
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("pausing-source"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd,
                false,
                "pausing-source-definition",
                controller: sourceCard.OwnerId,
                triggers: new[] { TimingCheckpoint.ActionEnded },
                triggeredEffect: choiceEffect.Reference), sourceCard.Id, sourceAction.Id, 1, 1);
            var catalog = new TriggeredEffectCatalog(new[] { choiceEffect });
            var authority = new TriggeredEffectResolutionAuthority(
                catalog, SyntheticEffectCapabilities.CreateCatalog());
            authority.ApplyCheckpoint(
                lifecycle,
                new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                new TieBreakerState(TeamSide.Red),
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                RuleModificationSet.Empty);

            var rebuilt = Rebuild(authority.PendingResolution);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(-1, 0),
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0)
                },
                new[]
                {
                    new HeroOccupancy(configuration.Participants.Heroes[0].Id, new HexCoordinate(0, 0)),
                    new HeroOccupancy(configuration.Participants.Heroes[1].Id, new HexCoordinate(-1, 0))
                });
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(),
                GuardsDigital.Core.Components.Synthetic.SyntheticComponentDefinitions.CreateCatalog(),
                catalog);
            var restored = CardActionResolutionAuthority.Restore(
                catalogs,
                board,
                CardActionResolutionRestoreState.WithPendingSkill(
                    rebuilt,
                    HeroCoinLedgerRestoreStateHolder.Absent()),
                null,
                null,
                null,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                new MarkerAttachmentRegister(),
                RuleModificationSet.Empty);
            Assert.That(restored.IsPendingTriggeredEffect, Is.True,
                "The existing Skill continuation discriminator must restore a trigger frame without a new pending kind.");

            // AnswerTriggeredEffectChoice is internal and GuardsDigital.Core stays closed - the scope
            // Application/AssemblyInfo.cs deliberately declined to widen - so the restored frame is
            // finished through its real caller: the turn authority, whose effect answer reaches a
            // pending trigger before a pending Skill. That also puts the restored frame on the path a
            // command actually takes, which is what the milestone claims about it. Two revealed cards
            // are what p.7 needs before a turn can start at all.
            foreach (var committed in new[]
                { configuration.RedHand.Instances[1], configuration.BlueHand.Instances[0] })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }

            var turn = new TurnResolutionAuthority(
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.Participants,
                new TieBreakerState(TeamSide.Red),
                restored,
                lifecycle,
                1,
                1);
            Assert.That(turn.TryStart().Accepted, Is.True);
            Assert.That(
                turn.TryAnswerEffectChoice(rebuilt.PendingChoice.SelectInvocations[0]).Accepted,
                Is.True);
            Assert.That(
                turn.TryAnswerEffectChoice(
                    restored.PendingSkillResolution.PendingChoice.ConfirmInvocation).Accepted,
                Is.True);

            Assert.That(restored.IsPendingTriggeredEffect, Is.False);
            Assert.That(restored.PendingSkillResolution, Is.Null);
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
                MatchContentCatalogs.ForThisBuild(),
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
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
        public void ResolvingSourceCard_PreservesItsActiveEffectUntilARealSourceChange()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
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
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active),
                "p.17 leaves the resolved card face-up to indicate its continuing Active effect.");
            Assert.That(hold.Events.Any(value =>
                value.Kind == TurnResolutionEventKind.TimingProgressed &&
                value.ActiveEffectEvent.Kind == ActiveEffectLifecycleEventKind.Cancelled &&
                value.ActiveEffectEvent.Checkpoint == TimingCheckpoint.SourceCardStateChanged),
                Is.False);
            Assert.That(authority.NotifySourceCardStateChanged(redCard.Id)
                .Single().Kind, Is.EqualTo(ActiveEffectLifecycleEventKind.Cancelled));
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Ended));
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
        public void ControllerHeroDefeated_CancellationRequiresTheControllerRatherThanTheDeclaredScope()
        {
            // Guard: unfiltering expiration must not widen identity-targeted cancellation - and, since
            // milestone 7.4 gave this checkpoint its first producer, the identity it targets is the
            // controller. Rulebook p.13 cancels "Defeated hero's actions with Active effects" and p.17
            // ends an Active effect on your card when "you are defeated", so both name the hero whose
            // action made it. The effect below is Hero-scoped at the Blue hero and controlled by the
            // Red one, which is exactly the pair the two readings disagree about.
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("blue-anchored"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.RoundEnd,
                false,
                id: "blue-anchored",
                controller: RedHero,
                scope: ActiveEffectAffectedScopeKind.Hero,
                affectedHero: BlueHero), SourceCard, SourceAction, 1, 1);

            var affectedHero = Apply(lifecycle, TimingCheckpoint.ControllerHeroDefeated, 1, 2, BlueHero);
            Assert.That(affectedHero, Is.Empty,
                "The hero this effect is aimed at was defeated, but its owner is alive and its card " +
                "is untouched, so neither page ends it.");
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));

            var controllingHero = Apply(lifecycle, TimingCheckpoint.ControllerHeroDefeated, 1, 2, RedHero);
            Assert.That(controllingHero.Select(value => value.Kind),
                Is.EqualTo(new[] { ActiveEffectLifecycleEventKind.Cancelled }));
        }

        [Test]
        public void ControllerHeroDefeated_LeavesAGlobalEffectAnotherHeroCreated()
        {
            // The other half of the same correction. A Global effect is relevant to every hero, so the
            // scope-based reading cancelled one whichever hero was defeated - including the many
            // Defeats that have nothing to do with the hero who created it.
            var lifecycle = new ActiveEffectLifecycle();
            lifecycle.Create(new ActiveEffectId("red-global"), Definition(
                ActiveEffectDurationKind.ThisRound,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.AfterAttack,
                TimingCheckpoint.RoundEnd,
                false,
                id: "red-global",
                controller: RedHero,
                scope: ActiveEffectAffectedScopeKind.Global), SourceCard, SourceAction, 1, 1);

            Assert.That(Apply(lifecycle, TimingCheckpoint.ControllerHeroDefeated, 1, 2, BlueHero),
                Is.Empty);
            Assert.That(lifecycle.Effects.Single().Phase, Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(
                Apply(lifecycle, TimingCheckpoint.ControllerHeroDefeated, 1, 2, RedHero)
                    .Select(value => value.Kind),
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
            HeroId affectedHero = default,
            EffectDefinitionReference? triggeredEffect = null)
        {
            if (!controller.IsDefined) controller = RedHero;
            return new EffectSpec(
                new ActiveEffectDefinition(id, scope, duration, creation, activation,
                    expiration, Conditions(), carries, "SyntheticModifier", id + "-unique",
                    triggers ?? Array.Empty<TimingCheckpoint>(),
                    triggeredEffect: triggeredEffect),
                controller,
                side,
                affectedHero);
        }

        private static EffectDefinition MarkTriggeredEffect(string id) =>
            new EffectDefinition(
                new EffectDefinitionId(id), "rev-1", new EffectNodeId("mark"),
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("mark"), EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, SyntheticEffectCapabilities.MarkActorV1,
                        outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) })
                });

        private static PendingResolution Rebuild(PendingResolution live) =>
            new PendingResolution(
                live.ResolutionId,
                live.SourceCardInstanceId,
                live.SourceDefinitionReference,
                live.ActionDefinitionId,
                live.EffectDefinitionReference,
                live.ActorId,
                live.ControllerId,
                live.InvocationContext,
                live.Continuation.Select(value =>
                    new EffectContinuationFrame(value.NodeId, value.ChildIndex)).ToArray(),
                live.CompletedOutcomes.Select(value =>
                    new EffectNodeOutcome(value.NodeId, value.ResultKind, value.Outputs)).ToArray(),
                live.AcceptedChoices.ToArray(),
                new PendingEffectChoice(
                    live.PendingChoice.ResolutionId,
                    live.PendingChoice.ChoiceId,
                    live.PendingChoice.NodeId,
                    live.PendingChoice.ChooserId,
                    live.PendingChoice.Kind,
                    live.PendingChoice.Candidates,
                    live.PendingChoice.MinimumSelections,
                    live.PendingChoice.MaximumSelections,
                    live.PendingChoice.SelectedCandidates));

        private static ActiveEffectCancellationCondition[] Conditions() => new[]
        {
            ActiveEffectCancellationCondition.SourceCardStateChanged,
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved,
            ActiveEffectCancellationCondition.Explicit
        };

        private static System.Collections.Generic.IReadOnlyList<ActiveEffectLifecycleEvent> Apply(
            ActiveEffectLifecycle lifecycle, TimingCheckpoint checkpoint, int round, int turn, HeroId hero) =>
            lifecycle.ApplyCheckpoint(new ActiveEffectCheckpointContext(checkpoint, round, turn, hero),
                new TieBreakerState(TeamSide.Red));

        private static MatchSession CreateSession(
            MatchConfiguration configuration,
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
                // Milestone 8.35g. A finished resolution waits for its own player to hand the
                // turn on, and acknowledging it clears the selection this loop reads.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Assert.That(session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);
                    continue;
                }

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
