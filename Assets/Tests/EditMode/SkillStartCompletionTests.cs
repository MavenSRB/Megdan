using System;
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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8j: a Skill whose effect root resolves to completion during its own start step.
    ///
    /// Milestone 6.8g's fixture found the seam. <c>SkillCardActionStrategyBackend.Complete</c> read
    /// the completing facts' identity from <c>step.Frame ?? pendingResolution</c>; at
    /// start-completion the step carries no frame, because the resolution finished, and there is no
    /// earlier pending resolution either, because a start is refused while one is live. Both were
    /// null and the session reported an engine fault. No configured content reaches the shape - every
    /// configured and synthetic Skill opens a choice - but the fault made choice-free Skills
    /// unwritable, and the 6.8g fixture had to adopt a trailing `may` step it did not otherwise need.
    ///
    /// The fixture is a configured official-map match holding one test-local card whose Skill is a
    /// single mandatory capability node. The capability is the already-registered
    /// <c>MarkActorV1</c>, chosen because it applies without pausing and creates an Active effect
    /// that declares an <c>ActionEnded</c> trigger - so the effect's own lifecycle reports whether
    /// the action's closing checkpoint was raised after the completing node, rather than the test
    /// asserting the stream shape from the outside.
    /// </summary>
    public sealed class SkillStartCompletionTests
    {
        private static readonly CardInstanceId StartCompletingCardId =
            new CardInstanceId("start-completing-skill-card");

        private static readonly ActionDefinitionId StartCompletingSkill =
            new ActionDefinitionId("start-completing-skill");

        /// <summary>
        /// Milestone 6.8j's failing observation: at the pre-fix revision this start reported
        /// <see cref="MatchSessionRejectionReason.EngineFault"/>, because the completing facts read
        /// their identity from a null frame.
        /// </summary>
        [Test]
        public void ASkillCompletingAtItsOwnStart_ResolvesInsteadOfFaulting()
        {
            var session = CreateStartCompletingSession();
            CommitBothSides(session);
            var actor = session.Projection.SelectedActor.ActorId;

            var started = session.Execute(DirectorCommand.StartAction(StartCompletingSkill));

            Assert.That(started.Accepted, Is.True, started.Detail);
            Assert.That(started.RejectionReason, Is.EqualTo(MatchSessionRejectionReason.None));
            Assert.That(session.Projection.PendingSkillResolution, Is.Null,
                "A Skill that never pauses leaves no pending resolution behind, exactly as one that " +
                "pauses and then finishes does.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(
                session.Projection.Cards.Single(card => card.Id.Equals(StartCompletingCardId))
                    .State.ResolutionStatus,
                Is.Not.EqualTo(CardResolutionStatus.Unresolved),
                "A completed Skill resolves its source card exactly as every other completed action " +
                "does, whether or not it opened a choice.");
            Assert.That(session.Projection.SelectedActor.ActorId, Is.Not.EqualTo(actor),
                "The turn advances normally: the next actor is selected rather than the match " +
                "stalling on a resolution that faulted.");
        }

        [Test]
        public void TheCompletingFacts_NameTheStartedResolutionAndOffer()
        {
            var session = CreateStartCompletingSession();
            CommitBothSides(session);
            var actor = session.Projection.SelectedActor.ActorId;

            Accept(session, DirectorCommand.StartAction(StartCompletingSkill));

            var skillFacts = SkillFacts(session);
            Assert.That(
                skillFacts.Select(fact => fact.Kind),
                Is.EqualTo(new[]
                {
                    CardActionResolutionEventKind.SkillActionStarted,
                    CardActionResolutionEventKind.SkillEffectNodeApplied,
                    CardActionResolutionEventKind.SkillActionCompleted
                }),
                "The ordered stream keeps its shape: the Skill started fact, the node fact, and the " +
                "Skill completed fact, with no choice-requested fact because nothing paused.");

            var completed = skillFacts.Last();
            Assert.That(completed.SourceCardInstanceId, Is.EqualTo(StartCompletingCardId),
                "The completing fact names the card the offer named. Reading it from a null frame is " +
                "what faulted.");
            Assert.That(completed.ActionDefinitionId, Is.EqualTo(StartCompletingSkill));
            Assert.That(completed.ActorId, Is.EqualTo(actor));
            Assert.That(
                skillFacts.Select(fact => fact.ResolutionId).Distinct().Count(), Is.EqualTo(1),
                "Every fact of one action names one resolution, so the completing fact cannot have " +
                "minted an identity of its own.");
        }

        /// <summary>
        /// The action's closing checkpoint is raised after the completing node, read through the
        /// effect the node creates rather than asserted from outside: the mark declares an
        /// <c>ActionEnded</c> trigger, so a triggered event can only exist if that checkpoint ran
        /// after the node executed.
        /// </summary>
        [Test]
        public void TheActionEndCheckpoint_RunsAndTheResolvedCardPreservesItsEffect()
        {
            var session = CreateStartCompletingSession();
            CommitBothSides(session);

            Accept(session, DirectorCommand.StartAction(StartCompletingSkill));

            var effect = Restore(session).Round.Timing.Effects.SingleOrDefault(candidate =>
                candidate.Definition.DefinitionId ==
                SyntheticEffectCapabilities.MarkDefinition.DefinitionId);
            Assert.That(effect, Is.Not.Null,
                "The node executed, so its Active effect exists; a start that faulted rolled the " +
                "whole command back and created nothing.");

            Assert.That(
                TimingEvents(session).Select(value => (value.Kind, value.Checkpoint)),
                Is.EqualTo(new[]
                {
                    (ActiveEffectLifecycleEventKind.Triggered, TimingCheckpoint.ActionEnded)
                }),
                "The mark declares an ActionEnded trigger, so the triggered event proves the closing " +
                "checkpoint ran after the completing node rather than being skipped with the frame. " +
                "Resolving the face-up card is not a source-invalidating transition and must not " +
                "cancel the Active effect it just created.");

            Assert.That(effect.Phase, Is.EqualTo(ActiveEffectPhase.Active));
        }

        [Test]
        public void AStartCompletingSkill_SurvivesSnapshotRestoreReplayAndUndo()
        {
            var session = CreateStartCompletingSession();
            CommitBothSides(session);
            var beforeStart = session.Projection.DiagnosticSnapshot;

            Accept(session, DirectorCommand.StartAction(StartCompletingSkill));
            var afterStart = session.Projection.DiagnosticSnapshot;
            var payload = session.CaptureSnapshot();

            Assert.That(afterStart, Is.Not.EqualTo(beforeStart),
                "The completed start must emit ordered facts, or the cases below prove nothing.");

            var restored = CreateStartCompletingSession();
            Assert.That(restored.RestoreSnapshot(payload).Accepted, Is.True);
            Assert.That(restored.CaptureSnapshot(), Is.EqualTo(payload),
                "A start-completing Skill crosses the snapshot boundary byte-identically.");

            Assert.That(session.Undo().Accepted, Is.True);
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(beforeStart),
                "Retained-prefix Undo reproduces the exact state before the completing start.");

            Accept(session, DirectorCommand.StartAction(StartCompletingSkill));
            Assert.That(session.Projection.DiagnosticSnapshot, Is.EqualTo(afterStart),
                "Replaying the same start reproduces the same ordered facts.");
        }

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, result.Detail);
        }

        private static CardActionResolutionEvent[] SkillFacts(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.SkillActionProgressed)
                .Select(value => value.TurnEvent.CardActionEvent)
                .ToArray();

        private static ActiveEffectLifecycleEvent[] TimingEvents(MatchSession session) =>
            session.Projection.RoundEvents
                .Where(value => value.Kind == RoundResolutionEventKind.TurnResolutionProgressed &&
                    value.TurnEvent.Kind == TurnResolutionEventKind.TimingProgressed)
                .Select(value => value.TurnEvent.ActiveEffectEvent)
                .ToArray();

        private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
            AuthoritativeSnapshotSerializer.Deserialize(
                MatchContentCatalogs.ForThisBuild(),
                session.Projection.AuthoritativeSnapshot,
                session.Projection.Definitions,
                new[] { session.Projection.SelectedMap },
                session.Projection.Setup.Minions);

        /// <summary>
        /// Commits the start-completing card for its owner and any eligible card for the other side.
        /// Initiative 30 outranks every placeholder card, so its owner acts first and cannot tie.
        /// </summary>
        private static void CommitBothSides(MatchSession session)
        {
            var owner = session.Projection.Cards
                .Single(card => card.Id.Equals(StartCompletingCardId)).OwnerId;
            Accept(session, DirectorCommand.CommitCard(owner, StartCompletingCardId));

            var other = session.Projection.Cards.First(card =>
                !card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            Accept(session, DirectorCommand.CommitCard(other.OwnerId, other.Id));

            Assert.That(session.Projection.PendingInitiativeChoice, Is.Null,
                "Initiative 30 against a placeholder card cannot tie.");
            Assert.That(session.Projection.SelectedActor.CardInstanceId,
                Is.EqualTo(StartCompletingCardId),
                "The start-completing card must be the one resolving, or the fixture proves nothing.");
        }

        /// <summary>
        /// The Skill under test: one mandatory capability node as the effect root, so the resolution
        /// runs to completion inside the start and never opens a decision. Test-local content; it
        /// implies no verified card text and is deliberately not added to any configured hand.
        /// </summary>
        private static CardDefinition CreateStartCompletingDefinition()
        {
            var root = new EffectNodeId("start-completing-root");
            var effect = new EffectDefinition(
                new EffectDefinitionId("start-completing-skill-effect"),
                "phase-6-8j-test-rev-1",
                root,
                new[]
                {
                    new EffectNodeDefinition(
                        root, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.MarkActorV1,
                        outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) })
                });

            return new CardDefinition(
                new CardDefinitionId("start-completing-skill-test"),
                "phase-6-8j-test-rev-1",
                "TEST - Start Completing Skill",
                new InitiativeValue(30),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        StartCompletingSkill,
                        ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None,
                        default,
                        effect)
                });
        }

        private static MatchSession CreateStartCompletingSession()
        {
            var configuration = MatchConfiguration.CreateForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);

            var definition = CreateStartCompletingDefinition();
            var instance = CardInstance.CreateInHand(
                StartCompletingCardId,
                definition.Reference,
                configuration.RedHand.Instances[0].OwnerId);
            var scenario = new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions.Concat(new[] { definition }),
                configuration.CardInstances.Concat(new[] { instance }),
                configuration.TieBreakerState,
                map.Reference,
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }
    }
}
