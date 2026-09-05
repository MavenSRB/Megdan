using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Synthetic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.19c1. The roster's three pausing rule sources, proven on synthetic shapes rather
    /// than on any hero's card: a removal prevention bought with a card, an Active effect that runs at
    /// rulebook p.8's moment before the Minion Battle, and a trigger that runs the moment one of its
    /// controller's Pushes has applied.
    ///
    /// <para>Each family is asked through the production query or authority that would answer it in a
    /// match, never through the value it reads: the modification set answers every declaration by
    /// construction, so a case that asked the set would prove only that a lookup works. Each also
    /// carries its negative arm, because what these three are mostly about is when they do
    /// <em>not</em> fire - the minion that walked out of the disk, the round that has already run its
    /// pre-battle moment, the Push that moved nothing.</para>
    /// </summary>
    public sealed class RosterPausingSourceTests
    {
        // -----------------------------------------------------------------------------------------
        // before-removal-prevention@1
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AShieldedMinion_IsOfferedItsControllersPricedPrevention()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition, fixture.Red);

            var offered = RemovalPreventionQuery.Offered(
                RuleObject.ForMinion(fixture.AllyMinionId), modifications, fixture.CardDefinitions,
                fixture.CardInstances);

            Assert.That(offered.Count, Is.EqualTo(1),
                "Brogan's Shield shape covers friendly minions in radius, and the guarded ally is " +
                "standing in the disk.");
            Assert.That(offered[0].DecidingHeroId, Is.EqualTo(fixture.Red),
                "p.10 gives the decision to the hero whose source is offering.");
            Assert.That(offered[0].Cost.Kind, Is.EqualTo(RemovalPreventionCostKind.DiscardOwnedCard),
                "The price is content on the declaration, not something the pause invents.");
            Assert.That(offered[0].PayableCards, Is.Not.Empty,
                "The candidates are re-derived from the hand at the moment the decision opens.");
            Assert.That(
                offered[0].PayableCards.All(card =>
                    fixture.CardInstances.Single(value => value.Id.Equals(card))
                        .OwnerId.Equals(fixture.Red)),
                Is.True,
                "A hero pays with its own cards. A candidate list reaching another hand would let " +
                "one player spend another player's turn.");
        }

        [Test]
        public void AMinionOutsideTheDisk_IsOfferedNothing()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition, fixture.Red);
            fixture.MoveAllyAway();
            var afterTheWalk = fixture.WithEffect(
                SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition, fixture.Red);

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AllyMinionId), modifications, fixture.CardDefinitions,
                    fixture.CardInstances),
                Is.Not.Empty,
                "The set built while the ally stood in the disk covered it.");
            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AllyMinionId), afterTheWalk, fixture.CardDefinitions,
                    fixture.CardInstances),
                Is.Empty,
                "p.17 moves the disk with its hero and every unit inside it walks in and out on its " +
                "own. A minion that left is no longer protected, and nothing about the effect changed.");
        }

        [Test]
        public void ACancelledSource_OffersNothing()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(
                lifecycle, SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition,
                fixture.Red, 1, 1);
            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AllyMinionId), fixture.Build(lifecycle),
                    fixture.CardDefinitions, fixture.CardInstances),
                Is.Not.Empty,
                "The source is standing before the cancellation.");

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.ControllerHeroDefeated, 1, 1, fixture.Red),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AllyMinionId), fixture.Build(lifecycle),
                    fixture.CardDefinitions, fixture.CardInstances),
                Is.Empty,
                "p.17 ends an Active effect on your card when you are defeated. A prevention that " +
                "outlived its source would keep a miniature on the board for a hero that is gone.");
        }

        [Test]
        public void APriceNobodyCanPay_IsNotOfferedAtAll()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition, fixture.Red);

            var offered = RemovalPreventionQuery.Offered(
                RuleObject.ForMinion(fixture.AllyMinionId),
                modifications,
                fixture.CardDefinitions,
                // Every card this hero owns has left the hand the declaration charges from.
                fixture.CardInstances.Where(card => !card.OwnerId.Equals(fixture.Red)).ToArray());

            Assert.That(offered, Is.Empty,
                "Opening a decision with no legal answer would stop the game on a question whose " +
                "only outcome was the removal that was going to happen anyway.");
        }

        [Test]
        public void AHeroSubject_IsOfferedNoPrevention()
        {
            var fixture = SourceFixture.Create();
            var modifications = fixture.WithEffect(
                SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition, fixture.Red);

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForHero(fixture.Red), modifications, fixture.CardDefinitions,
                    fixture.CardInstances),
                Is.Empty,
                "p.13 removes a defeated hero on its first line and pays the coins on its fourth, so " +
                "a hero has no 'after the reward and before the removal' moment for this family to " +
                "stand in. p.13's own example of it - Bolster - is about a minion, and this build " +
                "declines to invent the hero reading ahead of text that prints one.");
        }

        [Test]
        public void AFreePrevention_IsRefusedAsADeclaration()
        {
            Assert.That(
                () => RuleModification.ForRemovalPrevention(RemovalPreventionCost.Free),
                Throws.ArgumentException,
                "A source that stops a removal for nothing is p.18's immunity, and declaring it here " +
                "would give one rule two spellings with different answers to whether the unit may be " +
                "targeted at all.");
        }

        [Test]
        public void TwoSimultaneousShields_EachAnswerForTheirOwnSideAndNeitherSuppressesTheOther()
        {
            var fixture = SourceFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Create(
                lifecycle, SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition,
                fixture.Red, 1, 1, "red");
            fixture.Create(
                lifecycle, SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition,
                fixture.Blue, 1, 1, "blue");
            var modifications = fixture.Build(lifecycle);

            var offered = RemovalPreventionQuery.Offered(
                RuleObject.ForMinion(fixture.AllyMinionId), modifications, fixture.CardDefinitions,
                fixture.CardInstances);

            Assert.That(offered.Count, Is.EqualTo(1),
                "Two heroes shielding at once draw two disks, and the ally standing in Red's is " +
                "covered by Red's alone. Milestone 8.9 recorded the same reading for the radius " +
                "family: a shared uniqueness key must not let the first hero to resolve a card " +
                "suppress the opposing hero's.");
            Assert.That(offered[0].DecidingHeroId, Is.EqualTo(fixture.Red),
                "The decision belongs to the hero whose source covers this miniature, not to " +
                "whichever source was created first.");

            // p.17's uniqueness, from the other side: one radius-scoped occurrence per controller.
            var doubled = new ActiveEffectLifecycle();
            fixture.Create(
                doubled, SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition,
                fixture.Red, 1, 1, "first");
            fixture.Create(
                doubled, SyntheticPausingSourceCapabilities.ShieldFriendlyMinionsDefinition,
                fixture.Red, 1, 1, "second");

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AllyMinionId), fixture.Build(doubled),
                    fixture.CardDefinitions, fixture.CardInstances).Count,
                Is.EqualTo(1),
                "One hero playing the same shield twice draws one disk, so the decision offers one " +
                "price. A second option here would let a player pay twice to stop one removal.");
        }

        [Test]
        public void BothMinionDefeatPaths_AskTheOneSharedRemovalStage()
        {
            var backend = typeof(RemovalStageDecision).Assembly.GetType(
                "GuardsDigital.Core.Rules.CardResolution.AttackDefenseCardActionStrategyBackend");
            Assert.That(backend, Is.Not.Null,
                "The backend both minion Defeats run through must be locatable, or this rule proves " +
                "nothing. It is internal, so the Edit Mode assembly finds it by name.");

            var offered = ILConformanceScanner.Method(
                typeof(RemovalStageDecision), nameof(RemovalStageDecision.OfferedPreventions));
            var scope = ILConformanceScanner.WithNestedTypes(backend).ToArray();

            foreach (var name in new[] { "ResolveMinionTarget", "ResolveEffectInvokedMinionDefeat" })
            {
                var reached = ILConformanceScanner.ReachableFrom(backend, name, scope);
                Assert.That(
                    reached.Any(method => method.Instructions.Any(instruction =>
                        instruction.Member is System.Reflection.MethodBase called &&
                        called.DeclaringType == typeof(RemovalStageDecision) &&
                        string.Equals(
                            called.Name, offered.Name, StringComparison.Ordinal))),
                    Is.True,
                    $"{name} must reach the shared removal stage's prevention question. Milestone " +
                    "6.6b closed the case where one Defeat path honoured an interception and the " +
                    "other did not; a family that asked its own question at one call site would " +
                    "reopen it.");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The end-of-round moment before the Minion Battle
        // -----------------------------------------------------------------------------------------

        [Test]
        public void TheConstrictShape_DeclaresTheWindowAndTheEffectItRuns()
        {
            var definition = SyntheticPausingSourceCapabilities.ConstrictDefinition;

            Assert.That(
                definition.TriggerCheckpoints,
                Is.EqualTo(new[] { TimingCheckpoint.EndOfRoundBeforeMinionBattle }),
                "Xargatha's Constrict prints the position in its own reminder text: '( Before the end " +
                "of round minion battle. )'.");
            Assert.That(definition.TriggeredEffect.HasValue, Is.True,
                "A declared window with no effect behind it would be a card that silently never fires.");
            Assert.That(definition.ExpirationCheckpoint, Is.EqualTo(TimingCheckpoint.RoundEnd),
                "p.8 places Round End after the battle, so a This Round effect is still standing when " +
                "its own window opens.");
        }

        [Test]
        public void AWindowThisBuildDoesNotExecute_IsRefusedAtContentLoad()
        {
            Assert.That(
                () => new ActiveEffectDefinition(
                    "unsupported-window-effect",
                    ActiveEffectAffectedScopeKind.ControllerHero,
                    ActiveEffectDurationKind.ThisRound,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.ActionStarted,
                    TimingCheckpoint.RoundEnd,
                    new[] { ActiveEffectCancellationCondition.CardsRetrieved },
                    false,
                    "UnsupportedWindow",
                    "unsupported-window",
                    new[] { TimingCheckpoint.ObjectPushApplied },
                    triggeredEffect: SyntheticPausingSourceCapabilities.PausingTriggeredEffect
                        .Reference),
                Throws.ArgumentException,
                "The supported list is of windows something visits, not of windows a card could say. " +
                "The applied-Push moment is reached by an Ultimate's own declaration rather than by " +
                "the Active-effect lifecycle, so an Active effect naming it would never fire.");
        }

        [Test]
        public void ARoundRunsThePreBattleMomentBeforeItPlansTheBattle()
        {
            var session = CreatePausingSourceSession();
            var created = CreateConstrictSource(session);
            Assert.That(created, Is.True,
                "The fixture must actually create the pre-battle source, or the round below would be " +
                "asserting about a window nothing declared.");

            AdvanceToEndOfRound(session);
            var result = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(result.Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null,
                "The source's effect asks two questions, and the first of them stands before p.8's " +
                "battle is planned - which is the whole of what the printed reminder text says.");
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.MinionBattle),
                "The stage cursor is unchanged by the pause. That is exactly why the visit needs a " +
                "record of its own: the cursor cannot tell a round that has run the moment from one " +
                "that has not.");
        }

        [Test]
        public void AnAnsweredPreBattleTrigger_ResumesIntoTheSameBattle()
        {
            var session = CreatePausingSourceSession();
            Assert.That(CreateConstrictSource(session), Is.True);
            AdvanceToEndOfRound(session);
            Assert.That(session.Execute(DirectorCommand.AdvanceEndOfRound()).Accepted, Is.True);
            Assert.That(session.Projection.PendingEffectChoice, Is.Not.Null);

            var choice = session.Projection.PendingEffectChoice;
            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "The window's effect is a `may`, so p.10 gives its chooser a decline.");
            Assert.That(
                session.Execute(
                    DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation)).Accepted,
                Is.True,
                "Declining the `may` is a legal answer to the question the trigger asked.");
            Assert.That(session.Projection.PendingEffectChoice, Is.Null);
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.EqualTo(EndOfRoundStage.MinionBattle),
                "Nothing about the round moved while the question stood.");

            var resumed = session.Execute(DirectorCommand.AdvanceEndOfRound());

            Assert.That(resumed.Accepted, Is.True);
            Assert.That(
                resumed.Events.Any(value =>
                    value.Kind == RoundResolutionEventKind.EndOfRoundStageResolved),
                Is.True,
                "The advance that follows the answer resumes into the same battle rather than " +
                "visiting the moment again.");
            Assert.That(session.Projection.CurrentEndOfRoundStage,
                Is.Not.EqualTo(EndOfRoundStage.MinionBattle),
                "And the battle finished, so the round moved on.");
        }

        // -----------------------------------------------------------------------------------------
        // after-push-trigger@1
        // -----------------------------------------------------------------------------------------

        [Test]
        public void APushThatMovedNothing_ReachesNoTrigger()
        {
            var fixture = SourceFixture.Create();
            var ultimates = fixture.AfterPushUltimates();

            Assert.That(
                AfterPushTriggerQuery.Derive(
                    ultimates, fixture.Board, fixture.Participants, fixture.Red,
                    Array.Empty<RuleObject>()),
                Is.Empty,
                "p.18's 'if you push' is the applied displacement rather than the attempt. A push " +
                "whose target had nowhere to go moved nothing, so there is nothing for the rider to " +
                "be about.");
        }

        [Test]
        public void APushedEnemyHero_ReachesTheTriggerAndNothingElseDoes()
        {
            var fixture = SourceFixture.Create();
            var ultimates = fixture.AfterPushUltimates();

            var forEnemyHero = AfterPushTriggerQuery.Derive(
                ultimates, fixture.Board, fixture.Participants, fixture.Red,
                new[] { RuleObject.ForHero(fixture.Blue) });
            Assert.That(forEnemyHero.Count, Is.EqualTo(1),
                "Sabina's Big Sodding Gun prints 'if you push an enemy hero'.");
            Assert.That(forEnemyHero[0].PushedObject, Is.EqualTo(RuleObject.ForHero(fixture.Blue)),
                "The source names the object the push applied to, because the rider is about 'that " +
                "hero' and a source naming only the moment would leave the effect guessing.");

            Assert.That(
                AfterPushTriggerQuery.Derive(
                    ultimates, fixture.Board, fixture.Participants, fixture.Red,
                    new[] { RuleObject.ForMinion(fixture.AllyMinionId) }),
                Is.Empty,
                "p.19 keeps heroes and minions apart, and the card names a hero.");

            Assert.That(
                AfterPushTriggerQuery.Derive(
                    ultimates, fixture.Board, fixture.Participants, fixture.Red,
                    new[] { RuleObject.ForHero(fixture.Red) }),
                Is.Empty,
                "The pusher is not an enemy hero, and p.18 lets a push reach any object.");
        }

        [Test]
        public void APushByAnotherHero_ReachesNoTrigger()
        {
            var fixture = SourceFixture.Create();

            Assert.That(
                AfterPushTriggerQuery.Derive(
                    fixture.AfterPushUltimates(), fixture.Board, fixture.Participants, fixture.Blue,
                    new[] { RuleObject.ForHero(fixture.Red) }),
                Is.Empty,
                "The card says 'if YOU push'. Somebody else's push that displaced the same hero is " +
                "somebody else's push.");
        }

        [Test]
        public void APushHandler_ReportsExactlyWhatItDisplaced()
        {
            Assert.That(
                () => EffectExecutionResult.Applied(
                    appliedPushes: new[] { RuleObject.None }),
                Throws.ArgumentException,
                "An applied Push names the object it displaced; an undefined one would be a report " +
                "about nobody.");
            Assert.That(
                () => EffectExecutionResult.DeclinedOptional(),
                Throws.Nothing,
                "A handler that declined reports no displacement, which is the ordinary case.");
        }

        // -----------------------------------------------------------------------------------------
        // The two frames at once, and the schema that holds them
        // -----------------------------------------------------------------------------------------

        [Test]
        public void AnInterrupt_RefusesEveryShapeTheEngineCannotBeIn()
        {
            var frame = ProbeFrame("trigger-frame");

            Assert.That(
                () => new PendingEffectInterrupt(
                    frame, EffectInterruptKind.AfterPushTrigger, RuleObject.None,
                    new EffectResolutionId("interrupted")),
                Throws.ArgumentException,
                "An interrupt names the object the moment concerned.");

            Assert.That(
                () => new PendingEffectInterrupt(
                    frame, EffectInterruptKind.AfterPushTrigger, RuleObject.ForHero(new HeroId("h")),
                    frame.ResolutionId),
                Throws.ArgumentException,
                "A trigger does not interrupt itself; a payload pairing one frame with its own " +
                "identity describes nothing.");

            Assert.That(
                () => new PendingEffectInterrupt(
                    frame, EffectInterruptKind.AfterPushTrigger, RuleObject.ForHero(new HeroId("h")),
                    new EffectResolutionId("interrupted"),
                    new[] { RuleObject.ForHero(new HeroId("h")) }),
                Throws.ArgumentException,
                "The object this interrupt is running for is not also still owed a run, or the same " +
                "hero would be asked twice about one push.");

            var interrupt = new PendingEffectInterrupt(
                frame, EffectInterruptKind.AfterPushTrigger, RuleObject.ForHero(new HeroId("h")),
                new EffectResolutionId("interrupted"),
                new[] { RuleObject.ForHero(new HeroId("other")) });
            Assert.That(interrupt.RemainingSubjects.Count, Is.EqualTo(1),
                "p.18 pushes a batch at the same time, so the objects still owed a run travel with " +
                "the pause rather than being re-derived from a board that has since moved.");
        }

        [Test]
        public void ARemovalPreventionDecision_RefusesEveryShapeTheEngineCannotBeIn()
        {
            var resolution = ProbeAttackResolution();
            var outcome = new DefeatOutcome(
                new DefeatOutcomeId("probe:minion-defeat"),
                new DefeatTargetId("probe-minion"),
                new TeamId("probe-team"),
                new HeroId("probe-hero"),
                2,
                0);

            Assert.That(
                () => new PendingRemovalPreventionDecision(
                    RemovalPreventionOrigin.None, resolution, new MinionId("probe-minion"), outcome,
                    Array.Empty<CoinResourceOperation>(), new HeroId("probe-hero"),
                    RuleActivity.Attack),
                Throws.ArgumentException,
                "The two origins owe different remainders, so a decision that named neither could " +
                "not be finished.");

            Assert.That(
                () => new PendingRemovalPreventionDecision(
                    RemovalPreventionOrigin.AttackMinionDefeat, resolution,
                    new MinionId("probe-minion"), outcome,
                    new[] { ProbeCoinOperation(new DefeatOutcomeId("some-other-defeat")) },
                    new HeroId("probe-hero"), RuleActivity.Attack),
                Throws.ArgumentException,
                "Every carried coin operation belongs to the Defeat this decision is standing " +
                "inside; a payload pairing one with another Defeat's reward would restore a hero's " +
                "balance from somebody else's kill.");

            var decision = new PendingRemovalPreventionDecision(
                RemovalPreventionOrigin.AttackMinionDefeat, resolution, new MinionId("probe-minion"),
                outcome, new[] { ProbeCoinOperation(outcome.Id) }, new HeroId("probe-hero"),
                RuleActivity.Attack);
            Assert.That(decision.Reward.Accepted, Is.True,
                "The reward is rebuilt from the operations the ledger performed, because p.13 has " +
                "already paid it and re-running the award would be refused as a second payment.");
            Assert.That(decision.RequestId, Does.Contain("probe-minion"),
                "One Defeat has one removal stage, so the decision's identity is composed from the " +
                "resolution and the subject rather than stored.");
        }

        [Test]
        public void TheSchemaVersionMovesForThreeDecisionsV26CannotHold()
        {
            Assert.That(AuthoritativeSnapshotSerializer.CurrentSchemaVersion, Is.EqualTo(36),
                "Milestone 8.19c1 adds a trigger frame standing inside the effect it interrupted, a " +
                "Defeat waiting at its removal stage with the coin operations that paid it, and a " +
                "round-scoped record that p.8's pre-battle moment has already run. None of the three " +
                "has a reading available for its absence in a v26 payload, which is why v27 exists. " +
                "The version has since moved once more, at milestone 8.20, for the hero an " +
                "Active-effect occurrence bound with the word `this` - Arien's Expert and Master " +
                "Duelist, whose immunity excepts the attacker they were discarded against - and again at " +
                "milestone 8.21, whose Brogan faces reach eleven capability contracts no v28 payload " +
                "pins, so every one of them is refused rather than reinterpreted - and once more at " +
                "milestone 8.22, whose Sabina faces reach five more that no v29 payload pins, which " +
                "makes the pinned list the second cause to have moved this version twice - and once more " +
                "again at 8.22, for the rest of p.14 owed while a trigger a card-text Push set off " +
                "is standing, which is a fourth stopped decision of exactly this kind - and once " +
                "more at milestone 8.23, for the completed basic action Tigerclaw's Cloak and " +
                "Daggers may still be asked to repeat, which is a fifth, and the first that stands " +
                "outside the action it is about rather than inside it - and once more again at 8.23, for " +
                "where the attacker stood when an Attack started, which Combat Reflexes asks about " +
                "six numbered steps later and is a sixth - and once more at milestone 8.24, which is " +
                "the pinned list again and the THIRD cause to have moved this version more than once: " +
                "Xargatha's faces reach five contracts no v33 payload pins, and two of those five are " +
                "this family's own, because Constrict and Final Embrace are the first verified content " +
                "to stop the game at the pre-Minion-Battle moment 8.19c1 built with nothing visiting " +
                "it - and once more at milestone 8.35g, which is not a decision at all but the " +
                "first pause that exists because a turn is COMPLETE rather than interrupted: the " +
                "finished turn waiting for its own player's release, unre-derivable because " +
                "acknowledging the resolved card is what clears the selection that would say " +
                "whose it was - and once more at milestone 8.35u, which is the first move that adds " +
                "no new decision at all and instead changes the ARITY of one: rulebook p.15 levels " +
                "every hero simultaneously, so the paused step writes one item, chooser and option " +
                "list PER HERO still choosing where v35 wrote exactly one of each. No v35 payload " +
                "can be reinterpreted, because the count this version writes sits where that one " +
                "wrote its item-present boolean - the old bytes do not fail to mean something, they " +
                "mean something else. This " +
                "case pins the current version rather than its own, so a later move has to be " +
                "accounted for here rather than passing unread.");
        }

        // -----------------------------------------------------------------------------------------
        // Fixtures
        // -----------------------------------------------------------------------------------------

        private static PendingResolution ProbeFrame(string resolutionId) =>
            new PendingResolution(
                new EffectResolutionId(resolutionId),
                new CardInstanceId("probe-card"),
                new CardDefinitionReference(new CardDefinitionId("probe-definition"), "probe-rev"),
                new ActionDefinitionId("probe-action"),
                SyntheticPausingSourceCapabilities.PausingTriggeredEffect.Reference,
                new HeroId("probe-hero"),
                new HeroId("probe-hero"),
                ActionInvocationContext.PrintedPrimary,
                Array.Empty<EffectContinuationFrame>());

        private static PendingAttackResolution ProbeAttackResolution() =>
            new PendingAttackResolution(
                new ActionResolutionId("probe-resolution"),
                new CardInstanceId("probe-card"),
                new CardDefinitionReference(new CardDefinitionId("probe-definition"), "probe-rev"),
                new ActionDefinitionId("probe-action"),
                new HeroId("probe-hero"),
                ActionInvocationContext.PrintedPrimary,
                new ActionResolutionStepId("minion-defeat"),
                AttackResolutionPhase.ResolveMinionDefeat,
                AttackTarget.Minion(new MinionId("probe-minion")),
                null);

        private static CoinResourceOperation ProbeCoinOperation(DefeatOutcomeId outcomeId) =>
            new CoinResourceOperation(
                outcomeId, new DefeatTargetId("probe-minion"), new HeroId("probe-hero"),
                new TeamId("probe-team"), CoinAwardKind.Defeat, 2, 0, 2);

        private static MatchSession CreatePausingSourceSession()
        {
            var configuration =
                MatchConfiguration.CreateForPausingSourceFixture(TeamSide.Red);
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
                setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                Array.Empty<ObstacleOccupancy>());
            return new MatchSession(new MatchSessionInitialState(scenario, setup, new[] { map }));
        }

        /// <summary>
        /// Plays the pausing-source Skill and takes its pre-Minion-Battle mode, which is what puts the
        /// source on the board. Reports whether it got there, so a fixture that could not play the card
        /// fails loudly rather than asserting about a window nothing declared.
        /// </summary>
        private static bool CreateConstrictSource(MatchSession session)
        {
            while (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
            {
                var skill = session.Projection.Cards.FirstOrDefault(card =>
                    card.State.Location == CardLocation.Hand &&
                    DefinitionOf(session, card).ActionDefinitions.Any(action =>
                        action.Id.Value == "primary-pausing-source-skill"));
                if (skill == null)
                {
                    return false;
                }

                foreach (var hero in session.Projection.Participants.Heroes)
                {
                    var card = hero.Id.Equals(skill.OwnerId)
                        ? skill
                        : session.Projection.Cards.First(value =>
                            value.OwnerId.Equals(hero.Id) &&
                            value.State.Location == CardLocation.Hand);
                    if (!session.Execute(DirectorCommand.CommitCard(hero.Id, card.Id)).Accepted)
                    {
                        return false;
                    }
                }

                var guard = 0;
                while (session.Projection.IsTurnActive && guard++ < 32)
                {
                    if (!AnswerOrAct(session, skill))
                    {
                        return false;
                    }
                }

                if (session.Projection.ActiveEffectSourceCardInstanceIds.Contains(skill.Id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// One step of the fixture's turn: close whatever question is standing, or perform the pausing
        /// Skill when its own card is the selected actor, or hold. Reports whether it could.
        /// </summary>
        private static bool AnswerOrAct(MatchSession session, CardInstance skill)
        {
            var choice = session.Projection.PendingEffectChoice;
            if (choice != null)
            {
                var constrict = choice.SelectInvocations.FirstOrDefault(invocation =>
                    invocation.Candidate.OptionNodeId.IsDefined &&
                    invocation.Candidate.OptionNodeId.Value == "pausing-constrict");
                if (constrict != null)
                {
                    if (!session.Execute(DirectorCommand.AnswerEffectChoice(constrict)).Accepted)
                    {
                        return false;
                    }

                    var confirm = session.Projection.PendingEffectChoice?.ConfirmInvocation;
                    return confirm != null &&
                        session.Execute(DirectorCommand.AnswerEffectChoice(confirm)).Accepted;
                }

                var close = choice.ConfirmInvocation ?? choice.DeclineInvocation ??
                    choice.StopInvocation;
                return close != null &&
                    session.Execute(DirectorCommand.AnswerEffectChoice(close)).Accepted;
            }

            // Milestone 8.35g. ABOVE the selection read below: a finished resolution empties the
            // selection and waits for its own player, so without this the step would read that
            // emptiness as a stall and give up on a turn that is merely waiting to be handed on.
            if (session.Projection.PendingTurnRelease != null)
            {
                return session.Execute(DirectorCommand.ReleaseTurn()).Accepted;
            }

            var actor = session.Projection.SelectedActor;
            if (!actor.IsDefined)
            {
                return false;
            }

            if (actor.CardInstanceId.Equals(skill.Id))
            {
                var offer = session.Projection.AvailableTurnActions.FirstOrDefault(candidate =>
                    candidate.SourceCardInstanceId.Equals(skill.Id));
                if (offer.IsAvailable &&
                    session.Execute(DirectorCommand.StartAction(offer.ActionDefinitionId)).Accepted)
                {
                    return true;
                }
            }

            return session.Execute(DirectorCommand.PerformHold(actor.CardInstanceId)).Accepted;
        }

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(definition =>
                definition.Reference.Equals(card.DefinitionReference));

        private static void AdvanceToEndOfRound(MatchSession session)
        {
            var guard = 0;
            while (session.Projection.Phase != RoundResolutionPhase.EndOfRound && guard++ < 64)
            {
                if (session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    foreach (var hero in session.Projection.Participants.Heroes)
                    {
                        var card = session.Projection.Cards.First(value =>
                            value.OwnerId.Equals(hero.Id) &&
                            value.State.Location == CardLocation.Hand);
                        Assert.That(
                            session.Execute(DirectorCommand.CommitCard(hero.Id, card.Id)).Accepted,
                            Is.True);
                    }

                    continue;
                }

                if (session.Projection.PendingEffectChoice != null)
                {
                    var choice = session.Projection.PendingEffectChoice;
                    var close = choice.DeclineInvocation ?? choice.ConfirmInvocation ??
                        choice.StopInvocation;
                    Assert.That(close, Is.Not.Null,
                        "A question standing between the fixture and the end of the round has to be " +
                        "closable, or the fixture is stuck rather than the rule wrong.");
                    Assert.That(
                        session.Execute(DirectorCommand.AnswerEffectChoice(close)).Accepted, Is.True);
                    continue;
                }

                // Milestone 8.35g. ABOVE the active-turn assertion below, not merely above the Hold:
                // a turn held for its own player's release reads as inactive, so a guard placed one
                // line lower would still fail on the assertion it was meant to protect.
                if (session.Projection.PendingTurnRelease != null)
                {
                    Assert.That(
                        session.Execute(DirectorCommand.ReleaseTurn()).Accepted, Is.True);
                    continue;
                }

                Assert.That(session.Projection.IsTurnActive, Is.True);
                Assert.That(
                    session.Execute(DirectorCommand.PerformHold(
                        session.Projection.SelectedActor.CardInstanceId)).Accepted,
                    Is.True);
            }

            Assert.That(session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                "The fixture has to reach the end of the round for the pre-battle moment to exist.");
        }

        private sealed class SourceFixture
        {
            private readonly MatchConfiguration configuration;

            private SourceFixture(
                MatchConfiguration configuration,
                AdjacentMoveAuthority board,
                HeroId red,
                HeroId blue,
                HexCoordinate distantSpace)
            {
                this.configuration = configuration;
                Board = board;
                Red = red;
                Blue = blue;
                DistantSpace = distantSpace;
            }

            public AdjacentMoveAuthority Board { get; }
            public MatchParticipants Participants => configuration.Participants;
            public IReadOnlyList<CardDefinition> CardDefinitions => configuration.CardDefinitions;
            public IReadOnlyList<CardInstance> CardInstances => configuration.CardInstances;
            public HeroId Red { get; }
            public HeroId Blue { get; }
            public HexCoordinate DistantSpace { get; }
            public MinionId AllyMinionId { get; private set; }

            public static SourceFixture Create()
            {
                var configuration =
                    MatchConfiguration.CreateForInternalTest(TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);
                var board = new AdjacentMoveAuthority(
                    map,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate)).ToArray(),
                    Array.Empty<ObstacleOccupancy>(),
                    setup.Minions);

                var red = configuration.Participants.Heroes
                    .Single(hero =>
                        configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                    .Id;
                var blue = configuration.Participants.Heroes
                    .Single(hero =>
                        configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                    .Id;

                var pair = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .Where(candidate => !AnyUnitWithin(board, candidate, 2))
                    .SelectMany(first => first.GetNeighbors()
                        .Where(second => board.IsEmptyPlayableSpace(second) &&
                            !AnyUnitWithin(board, second, 2))
                        .Select(second => new { First = first, Second = second }))
                    .FirstOrDefault();
                Assert.That(pair, Is.Not.Null,
                    "The fixture needs two adjacent empty spaces well clear of the official setup, or " +
                    "the disk below would be measuring somebody else's minions.");

                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(red), pair.First).Accepted, Is.True);
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(blue), pair.Second).Accepted, Is.True);

                var distant = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .OrderByDescending(candidate => HexDistanceQuery.Calculate(candidate, pair.First))
                    .First();

                var ally = board.Minions
                    .Where(minion => minion.TeamSide == TeamSide.Red)
                    .OrderBy(minion => minion.MinionId.Value, StringComparer.Ordinal)
                    .First();
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForMinion(ally.MinionId),
                        pair.First.GetNeighbors().First(board.IsEmptyPlayableSpace)).Accepted,
                    Is.True,
                    "The shielded ally has to stand inside the disk the declaration draws.");

                return new SourceFixture(configuration, board, red, blue, distant)
                {
                    AllyMinionId = ally.MinionId
                };
            }

            private static bool AnyUnitWithin(
                AdjacentMoveAuthority board, HexCoordinate space, int distance) =>
                board.Occupancies.Any(hero =>
                    HexDistanceQuery.Calculate(hero.Coordinate, space) <= distance) ||
                board.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(minion.Coordinate, space) <= distance);

            public void Create(
                ActiveEffectLifecycle lifecycle,
                ActiveEffectDefinition definition,
                HeroId controller,
                int roundIndex,
                int turnIndex,
                string suffix = null) =>
                lifecycle.Create(
                    new ActiveEffectId(
                        $"{definition.DefinitionId}:{controller.Value}:{suffix ?? "one"}"),
                    definition,
                    controller,
                    Participants.GetTeamForHero(controller).Side,
                    default,
                    new CardInstanceId($"pausing-source-probe-{controller.Value}-{suffix ?? "one"}"),
                    new ActionDefinitionId("pausing-source-probe-action"),
                    roundIndex,
                    turnIndex);

            public RuleModificationSet WithEffect(
                ActiveEffectDefinition definition, HeroId controller)
            {
                var lifecycle = new ActiveEffectLifecycle();
                Create(lifecycle, definition, controller, 1, 1);
                return Build(lifecycle);
            }

            public RuleModificationSet Build(ActiveEffectLifecycle lifecycle) =>
                RuleModificationQuery.Build(
                    lifecycle,
                    Board,
                    Participants,
                    new TieBreakerState(TeamSide.Red),
                    UnlockedUltimateSet.None,
                    RuleObject.None,
                    null,
                    null);

            /// <summary>Moves the shielded ally out of the disk this fixture measures.</summary>
            public void MoveAllyAway() =>
                Assert.That(
                    Board.CommitValidatedDisplacement(
                        BoardObjectReference.ForMinion(AllyMinionId), DistantSpace).Accepted,
                    Is.True);

            /// <summary>The Ultimates a hand carrying milestone 8.19c1's after-Push shape unlocks.</summary>
            public UnlockedUltimateSet AfterPushUltimates()
            {
                var hand = SyntheticShowcaseHand.CreateRedWithAfterPushUltimate(Red);
                var ultimate = hand.AllInstances.Single(card =>
                    hand.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .UltimateRule != null);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True,
                    "p.16 unlocks the Ultimate by moving the card beside the dashboard.");
                return UltimateUnlockQuery.Derive(hand.Definitions, hand.AllInstances);
            }
        }
    }
}
