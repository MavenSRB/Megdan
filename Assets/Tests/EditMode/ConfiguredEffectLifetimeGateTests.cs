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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.13's lifetime gate: one configured multi-turn scenario in which all four live
    /// lifetime forms happen together, rather than five isolated proofs that each form works alone.
    ///
    /// The two converted showcase cards supply every form between them. Red Bulwark is the source for
    /// `This Turn` (+1 Defense), `This Round` (no Fast Travel) and `Next Turn` (Move Through obstacles);
    /// Blue Feint is the source for `This Round` (Counts As minion) and `Next Turn after Playing Cards`
    /// (immune to Push). A card creates one effect per play and returns to its owner's hand at the end
    /// of the round, so the scenario is three rounds long and each round is chosen for what it can show:
    ///
    ///   Round 1 - both delayed forms, created together in turn 1 and activated together in turn 2 at
    ///             their two different anchors, then expiring at that turn's own checkpoint.
    ///   Round 2 - both `This Round` forms, live across every remaining turn and expiring at Round End,
    ///             which the round authority reaches at the Minion Battle stage - before the cleanup,
    ///             before an owed Lane Push, and before Retrieve Cards.
    ///   Round 3 - `This Turn` in turn 1, and a delayed form created in turn 4 with no turn left to
    ///             activate in, which survives Round End on its cross-round flag and is cancelled by
    ///             retrieval instead.
    ///
    /// That last case is the one the whole ordering exists for. Rulebook p.17 cancels an Active effect
    /// on your card when the card "changes state (including at the end of the round)", and Retrieve
    /// Cards is that state change, so a `Next Turn` effect made on the last turn of a round never gets
    /// its turn. Both cards declare the retrieval condition, and neither declares the source-card one,
    /// because p.17 has you turn the card on its side *after* performing the action precisely to signal
    /// that its effect is still running.
    ///
    /// None of the synthetic values or texts is official content.
    /// </summary>
    public sealed class ConfiguredEffectLifetimeGateTests
    {
        private static readonly CardInstanceId BulwarkCardId = new CardInstanceId("red-bulwark-card");
        private static readonly CardInstanceId FeintCardId = new CardInstanceId("blue-feint-card");

        private static readonly ActionDefinitionId BulwarkSkill =
            new ActionDefinitionId("primary-modification-skill");

        private static readonly ActionDefinitionId FeintSkill =
            new ActionDefinitionId("primary-immunity-skill");

        private static readonly EffectNodeId DefenseMode = new EffectNodeId("bulwark-defense-bonus");
        private static readonly EffectNodeId RestrictionMode =
            new EffectNodeId("bulwark-fast-travel-restriction");
        private static readonly EffectNodeId PermissionMode =
            new EffectNodeId("bulwark-move-through-permission");
        private static readonly EffectNodeId ImmunityMode = new EffectNodeId("feint-push-immunity");
        private static readonly EffectNodeId CountsAsMode = new EffectNodeId("feint-counts-as-minion");

        private const string Permission = "synthetic-move-through-permission-effect";
        private const string Immunity = "synthetic-push-immunity-effect";
        private const string Restriction = "synthetic-fast-travel-restriction-effect";
        private const string CountsAs = "synthetic-counts-as-minion-effect";
        private const string DefenseBonus = "synthetic-defense-bonus-effect";

        // ---------------------------------------------------------------------------------------
        // What the five sources declare
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheFiveConfiguredSources_CoverAllFourDurationsAndDeclareTheSameTwoCancellations()
        {
            var declared = ScenarioDefinitions();

            Assert.That(
                declared.Select(definition => definition.DurationKind).Distinct().OrderBy(kind => kind),
                Is.EqualTo(new[]
                {
                    ActiveEffectDurationKind.ThisTurn,
                    ActiveEffectDurationKind.ThisRound,
                    ActiveEffectDurationKind.NextTurn,
                    ActiveEffectDurationKind.NextTurnAfterPlayingCards
                }),
                "All four live lifetime forms must be reachable from the two configured cards.");

            foreach (var definition in declared)
            {
                Assert.That(
                    definition.CancellationConditions.OrderBy(condition => condition),
                    Is.EqualTo(new[]
                    {
                        ActiveEffectCancellationCondition.ControllerHeroDefeated,
                        ActiveEffectCancellationCondition.CardsRetrieved
                    }),
                    definition.DefinitionId + " must declare exactly p.17's two applicable cancellations.");
                Assert.That(
                    definition.CancellationConditions.Any(condition =>
                        condition == ActiveEffectCancellationCondition.SourceCardStateChanged), Is.False,
                    "Resolving the source card is deliberately not a cancellation: p.17 turns the card on " +
                    "its side after the action precisely because the effect is still running.");
                Assert.That(definition.AffectedScope,
                    Is.EqualTo(ActiveEffectAffectedScopeKind.ControllerHero),
                    "The resolved card is only the stable source; the affected rules object is the acting hero.");
            }

            Assert.That(
                declared.Where(definition =>
                        definition.DurationKind == ActiveEffectDurationKind.NextTurn ||
                        definition.DurationKind == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
                    .All(definition => definition.CarriesAcrossRoundBoundary), Is.True,
                "Only the delayed forms may outlive Round End, and only as far as retrieval.");
        }

        // ---------------------------------------------------------------------------------------
        // Round 1: the two delayed forms, together
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BothDelayedForms_StayDormantThroughTheTurnThatCreatedThemAndThroughResolvingTheirCards()
        {
            var run = new ScenarioRun();
            run.PlayRoundOneOpeningTurn();

            Assert.That(run.Phase(Immunity), Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(run.Phase(Permission), Is.EqualTo(ActiveEffectPhase.Dormant),
                "Neither keyword names the turn it was written in, so neither applies anything yet.");
            Assert.That(run.CardState(FeintCardId).ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(run.CardState(BulwarkCardId).ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved),
                "Both source cards resolved while their effects stand: a resolved card is not a cancellation.");

            run.CompleteCurrentTurn();

            Assert.That(run.Phase(Immunity), Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(run.Phase(Permission), Is.EqualTo(ActiveEffectPhase.Dormant));
            Assert.That(run.LifecycleEvents(Immunity), Is.Empty,
                "A turn that never reached the activation anchor emits nothing for it.");
            Assert.That(run.LifecycleEvents(Permission), Is.Empty);
        }

        [Test]
        public void TheTwoDelayedForms_ActivateAtTheirOwnAnchorsInTheOneTurnThatFollows()
        {
            var run = new ScenarioRun();
            run.PlayRoundOneOpeningTurn();
            run.CompleteCurrentTurn();

            run.CommitOrdinaryTurn();

            // One command carries both anchors, and the order is the round's own: cards are revealed
            // before the turn starts, so the reveal-anchored form comes into effect first.
            Assert.That(
                run.LifecycleEvents(Immunity).Select(value => value.Kind + "@" + value.Checkpoint),
                Is.EqualTo(new[] { "Activated@CardsPlayedAndRevealed" }),
                "p.17 applies this one once next turn, after all players have played and revealed.");
            Assert.That(
                run.LifecycleEvents(Permission).Select(value => value.Kind + "@" + value.Checkpoint),
                Is.EqualTo(new[] { "Activated@TurnStarted" }),
                "An ordinary Next Turn effect waits for the turn itself rather than for the reveal.");
            Assert.That(run.ActivationOrder(), Is.EqualTo(new[] { Immunity, Permission }),
                "Two anchors inside one turn, and the reveal is the earlier of them.");
            Assert.That(run.Phase(Immunity), Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(run.Phase(Permission), Is.EqualTo(ActiveEffectPhase.Active));
        }

        [Test]
        public void TheTwoDelayedForms_ExpireExactlyOnceAtTheirOwnTurnsCheckpointAndNeverAgain()
        {
            var run = new ScenarioRun();
            run.PlayRoundOneOpeningTurn();
            run.CompleteCurrentTurn();
            run.CommitOrdinaryTurn();
            run.CompleteCurrentTurn();

            foreach (var effect in new[] { Immunity, Permission })
            {
                Assert.That(
                    run.LifecycleEvents(effect).Select(value => value.Kind + "@" + value.Checkpoint).Last(),
                    Is.EqualTo("Expired@TurnOnlyCancellationBeforeTurnEnded"),
                    effect + " expires at the checkpoint its declaration names.");
                Assert.That(run.EndingEventCount(effect), Is.EqualTo(1));
                Assert.That(run.Phase(effect), Is.EqualTo(ActiveEffectPhase.Ended));
            }

            run.PlayRestOfRound();
            run.AdvanceThroughEndOfRound();

            foreach (var effect in new[] { Immunity, Permission })
            {
                Assert.That(run.EndingEventCount(effect), Is.EqualTo(1),
                    "Round End and Retrieve Cards must not end an effect that already ended.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Round 2: both `This Round` forms
        // ---------------------------------------------------------------------------------------

        [Test]
        public void BothThisRoundForms_ApplyFromTheActionAndOutliveEveryRemainingTurnOfTheirRound()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwoOpeningTurn();

            Assert.That(run.Phase(Restriction), Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(run.Phase(CountsAs), Is.EqualTo(ActiveEffectPhase.Active),
                "A This Round effect activates in the action that made it.");

            run.CompleteCurrentTurn();
            for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound; turn++)
            {
                run.CommitOrdinaryTurn();
                run.CompleteCurrentTurn();
                Assert.That(run.Phase(Restriction), Is.EqualTo(ActiveEffectPhase.Active),
                    "A round is four turns, and a This Round effect crosses all of them.");
                Assert.That(run.Phase(CountsAs), Is.EqualTo(ActiveEffectPhase.Active));
            }
        }

        [Test]
        public void BothThisRoundForms_ExpireAtRoundEndBeforeCleanupTheLanePushStageAndRetrieval()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwo(stopAtEndOfRound: true);

            var atMinionBattle = run.AdvanceOneEndOfRoundStage();

            Assert.That(atMinionBattle.Stage, Is.EqualTo(EndOfRoundStage.MinionBattle));
            foreach (var effect in new[] { Restriction, CountsAs })
            {
                Assert.That(
                    atMinionBattle.Lifecycle(effect),
                    Is.EqualTo(new[] { "Expired@RoundEnd" }),
                    effect + " ends at Round End, which the round reaches at its first stage.");
                Assert.That(run.Phase(effect), Is.EqualTo(ActiveEffectPhase.Ended));
            }

            // Everything p.8 prints after Round End must find nothing left to end.
            foreach (var later in new[]
            {
                EndOfRoundStage.RemoveTokensAndReturnMarkers,
                EndOfRoundStage.ResolveRequestedLanePush,
                EndOfRoundStage.RetrieveCardsAndCancelActiveEffects
            })
            {
                var stage = run.AdvanceOneEndOfRoundStage();
                Assert.That(stage.Stage, Is.EqualTo(later));
                Assert.That(stage.Lifecycle(Restriction), Is.Empty);
                Assert.That(stage.Lifecycle(CountsAs), Is.Empty);
            }

            foreach (var effect in new[] { Restriction, CountsAs })
            {
                Assert.That(run.EndingEventCount(effect), Is.EqualTo(1),
                    "Retrieval cannot cancel what Round End already expired.");
            }
        }

        // ---------------------------------------------------------------------------------------
        // Round 3: `This Turn`, and the delayed form retrieval ends
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheThisTurnForm_AppliesInsideItsOwnTurnAndExpiresOnceWhenThatTurnEnds()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwo();
            run.PlayRoundThreeOpeningTurn();

            Assert.That(run.Phase(DefenseBonus), Is.EqualTo(ActiveEffectPhase.Active));

            run.CompleteCurrentTurn();

            Assert.That(
                run.LifecycleEvents(DefenseBonus).Select(value => value.Kind + "@" + value.Checkpoint),
                Is.EqualTo(new[] { "Expired@TurnOnlyCancellationBeforeTurnEnded" }),
                "This Turn is the shortest form: one action window, ended by its own turn.");
            Assert.That(run.EndingEventCount(DefenseBonus), Is.EqualTo(1));
        }

        [Test]
        public void ADelayedFormMadeOnTheLastTurn_SurvivesRoundEndAndIsCancelledOnceByRetrieval()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwo();
            run.PlayRoundThree(stopAtEndOfRound: true);

            var lastTurnImmunity = run.EffectCreatedIn(Immunity, roundIndex: 3);
            Assert.That(run.PhaseOf(lastTurnImmunity), Is.EqualTo(ActiveEffectPhase.Dormant),
                "Turn four is the last turn, so the keyword's next turn never arrived inside this round.");

            var minionBattle = run.AdvanceOneEndOfRoundStage();

            Assert.That(minionBattle.Stage, Is.EqualTo(EndOfRoundStage.MinionBattle));
            Assert.That(minionBattle.LifecycleOf(lastTurnImmunity), Is.Empty,
                "The declaration carries across the round boundary, so Round End leaves it alone.");
            Assert.That(run.PhaseOf(lastTurnImmunity), Is.EqualTo(ActiveEffectPhase.Dormant));

            run.AdvanceOneEndOfRoundStage();
            run.AdvanceOneEndOfRoundStage();
            var retrieval = run.AdvanceOneEndOfRoundStage();

            Assert.That(retrieval.Stage, Is.EqualTo(EndOfRoundStage.RetrieveCardsAndCancelActiveEffects));
            Assert.That(
                retrieval.LifecycleOf(lastTurnImmunity).Select(value => value.Kind + "@" + value.Checkpoint),
                Is.EqualTo(new[] { "Cancelled@CardsRetrieved" }),
                "p.17 cancels it because its card changed state, \"including at the end of the round\".");
            Assert.That(run.PhaseOf(lastTurnImmunity), Is.EqualTo(ActiveEffectPhase.Ended));

            run.FinishEndOfRound();
            run.CommitOrdinaryTurn();

            Assert.That(run.PhaseOf(lastTurnImmunity), Is.EqualTo(ActiveEffectPhase.Ended),
                "The next round's reveal must not resurrect an effect retrieval already cancelled.");
            Assert.That(run.EndingEventCountOf(lastTurnImmunity), Is.EqualTo(1));
        }

        // ---------------------------------------------------------------------------------------
        // What retrieval may and may not do to an effect
        // ---------------------------------------------------------------------------------------

        [Test]
        public void Retrieval_EndsOnlyWhatDeclaredItsConditionAndNeverDeletesOrRewritesAnInstance()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwo();
            run.PlayRoundThree(stopAtEndOfRound: true);

            var before = run.Effects();
            run.AdvanceOneEndOfRoundStage();
            run.AdvanceOneEndOfRoundStage();
            run.AdvanceOneEndOfRoundStage();
            var retrieval = run.AdvanceOneEndOfRoundStage();
            var after = run.Effects();

            Assert.That(after.Select(effect => effect.Id.Value),
                Is.EqualTo(before.Select(effect => effect.Id.Value)),
                "Retrieval ends effects; it never deletes an instance.");
            Assert.That(
                after.Select(effect => effect.Definition.DefinitionId),
                Is.EqualTo(before.Select(effect => effect.Definition.DefinitionId)),
                "A restored instance names its definition, and retrieval may not rewrite which one.");

            var cancelled = retrieval.AllLifecycle()
                .Where(value => value.Kind == ActiveEffectLifecycleEventKind.Cancelled)
                .Select(value => value.EffectId)
                .ToArray();
            Assert.That(cancelled, Is.Not.Empty,
                "The scenario must reach retrieval with something live for this rule to be about anything.");
            foreach (var effectId in cancelled)
            {
                var instance = after.Single(effect => effect.Id.Equals(effectId));
                Assert.That(
                    instance.Definition.CancellationConditions.Any(condition =>
                        condition == ActiveEffectCancellationCondition.CardsRetrieved), Is.True,
                    "Cancellation follows the declared condition, never the source card's display name.");
            }

            Assert.That(
                after.Where(effect => !cancelled.Any(id => id.Equals(effect.Id)))
                    .All(effect => effect.EndedAt != TimingCheckpoint.CardsRetrieved), Is.True,
                "An effect the checkpoint did not name must keep the phase and ending it already had.");
        }

        // ---------------------------------------------------------------------------------------
        // The scenario across the persistence boundary
        // ---------------------------------------------------------------------------------------

        [Test]
        public void TheScenario_SurvivesRestoreReplayAndRetainedPrefixUndoOnBothSidesOfRetrieval()
        {
            var run = new ScenarioRun();
            run.PlayRoundOne();
            run.PlayRoundTwo();
            run.PlayRoundThree(stopAtEndOfRound: true);
            run.AdvanceOneEndOfRoundStage();
            run.AdvanceOneEndOfRoundStage();
            run.AdvanceOneEndOfRoundStage();

            AssertRoundTrips(run, "before retrieval");
            var beforeRetrieval = run.Session.Projection.AuthoritativeSnapshot;

            run.AdvanceOneEndOfRoundStage();

            AssertRoundTrips(run, "after retrieval");
            Assert.That(run.Session.Projection.AuthoritativeSnapshot, Is.Not.EqualTo(beforeRetrieval),
                "Retrieval must actually move the state these round trips are about.");

            // Undo takes the retrieval advance back off, and re-executing it lands on the same bytes.
            var snapshot = run.Session.Projection.AuthoritativeSnapshot;
            var commands = run.Session.Projection.AcceptedCommands.ToArray();
            Assert.That(run.Session.Undo().Accepted, Is.True);
            Assert.That(run.Session.Projection.AuthoritativeSnapshot, Is.EqualTo(beforeRetrieval),
                "An undone retrieval leaves the round exactly where the stage before it left off.");
            var reExecuted = run.Session.Execute(commands.Last());
            Assert.That(reExecuted.Accepted, Is.True, reExecuted.Detail);
            Assert.That(run.Session.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "The retained prefix replays to exactly the state it produced, retrieving nothing twice.");
        }

        private static void AssertRoundTrips(ScenarioRun run, string where)
        {
            var snapshot = run.Session.Projection.AuthoritativeSnapshot;
            var phases = run.Effects()
                .Select(effect => effect.Definition.DefinitionId + ":" + effect.Phase + ":" + effect.EndedAt)
                .ToArray();

            var restored = ScenarioRun.CreateSession();
            Assert.That(restored.RestoreSnapshot(snapshot).Accepted, Is.True, where);
            Assert.That(restored.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "Restore " + where + " must reproduce the payload it was given.");
            Assert.That(ScenarioRun.PhasesOf(restored), Is.EqualTo(phases),
                "Restore " + where + " must reproduce every effect phase and ending checkpoint.");

            var replayed = ScenarioRun.CreateSession();
            foreach (var command in run.Session.Projection.AcceptedCommands)
            {
                var result = replayed.Execute(command);
                Assert.That(result.Accepted, Is.True, where + ": " + result.Detail);
            }

            Assert.That(replayed.Projection.AuthoritativeSnapshot, Is.EqualTo(snapshot),
                "A full replay " + where + " must land on the same bytes.");
            Assert.That(ScenarioRun.PhasesOf(replayed), Is.EqualTo(phases));
        }

        private static IReadOnlyList<ActiveEffectDefinition> ScenarioDefinitions() => new[]
        {
            SyntheticModificationCapabilities.DefenseBonusDefinition,
            SyntheticModificationCapabilities.FastTravelRestrictionDefinition,
            SyntheticModificationCapabilities.MoveThroughPermissionDefinition,
            SyntheticModificationCapabilities.PushImmunityDefinition,
            SyntheticModificationCapabilities.CountsAsMinionDefinition
        };

        // ---------------------------------------------------------------------------------------
        // The scenario driver
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// One live configured match, driven through the scenario and recording every lifecycle event
        /// each command produced. Tests read the recording rather than re-deriving it, so every case
        /// below is a statement about the same run.
        /// </summary>
        private sealed class ScenarioRun
        {
            private readonly List<ActiveEffectLifecycleEvent> lifecycle =
                new List<ActiveEffectLifecycleEvent>();

            public ScenarioRun()
            {
                Session = CreateSession();
            }

            public MatchSession Session { get; }

            // -- scenario steps ------------------------------------------------------------------

            /// <summary>Round 1 turn 1: Blue Feint at 12 acts first, then Red Bulwark at 11.</summary>
            public void PlayRoundOneOpeningTurn()
            {
                CommitNamedPair(BulwarkCardId, FeintCardId);
                Assert.That(Session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(FeintCardId),
                    "Blue Feint outranks Red Bulwark, so it resolves first and both fit in one turn.");
                ResolveSkill(FeintSkill, ImmunityMode);
                ResolveSkill(BulwarkSkill, PermissionMode);
            }

            public void PlayRoundOne()
            {
                PlayRoundOneOpeningTurn();
                CompleteCurrentTurn();
                PlayRestOfRound();
                AdvanceThroughEndOfRound();
            }

            public void PlayRoundTwoOpeningTurn()
            {
                Assert.That(Session.Projection.RoundIndex, Is.EqualTo(2));
                CommitNamedPair(BulwarkCardId, FeintCardId);
                ResolveSkill(FeintSkill, CountsAsMode);
                ResolveSkill(BulwarkSkill, RestrictionMode);
            }

            public void PlayRoundTwo(bool stopAtEndOfRound = false)
            {
                PlayRoundTwoOpeningTurn();
                CompleteCurrentTurn();
                PlayRestOfRound();
                if (!stopAtEndOfRound)
                {
                    AdvanceThroughEndOfRound();
                }
            }

            /// <summary>Round 3 turn 1: only Red acts with its Skill; Blue keeps Feint back for turn 4.</summary>
            public void PlayRoundThreeOpeningTurn()
            {
                Assert.That(Session.Projection.RoundIndex, Is.EqualTo(3));
                CommitNamedPair(BulwarkCardId, LowestBlueCardExcept(FeintCardId));
                ResolveSkill(BulwarkSkill, DefenseMode);
            }

            public void PlayRoundThree(bool stopAtEndOfRound = false)
            {
                PlayRoundThreeOpeningTurn();
                CompleteCurrentTurn();

                // Turns 2 and 3 are ordinary; turn 4 is the one Blue Feint was held for.
                for (var turn = 2; turn <= RoundResolutionAuthority.TurnsPerRound - 1; turn++)
                {
                    CommitOrdinaryTurn(exceptBlue: FeintCardId);
                    CompleteCurrentTurn();
                }

                CommitNamedPair(HighestRedCard(), FeintCardId);
                ResolveSkill(FeintSkill, ImmunityMode);
                CompleteCurrentTurn();
                if (!stopAtEndOfRound)
                {
                    AdvanceThroughEndOfRound();
                }
            }

            public void PlayRestOfRound()
            {
                while (Session.Projection.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitOrdinaryTurn();
                    CompleteCurrentTurn();
                }
            }

            // -- primitives ----------------------------------------------------------------------

            public void CommitOrdinaryTurn(CardInstanceId? exceptBlue = null)
            {
                Assert.That(Session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
                foreach (var side in new[] { TeamSide.Red, TeamSide.Blue })
                {
                    var held = HandCards(side)
                        .Where(card => !exceptBlue.HasValue || !card.Id.Equals(exceptBlue.Value))
                        .ToArray();
                    if (held.Length == 0)
                    {
                        var hero = Session.Projection.Setup.Heroes.Single(value => value.TeamSide == side).HeroId;
                        Accept(DirectorCommand.Pass(hero));
                        continue;
                    }

                    Accept(DirectorCommand.CommitCard(held[0].OwnerId, held[0].Id));
                }

                ResolveInitiativeTie();
            }

            public void CompleteCurrentTurn()
            {
                var turnIndex = Session.Projection.TurnIndex;
                while (Session.Projection.Phase == RoundResolutionPhase.ResolvingTurn &&
                    Session.Projection.TurnIndex == turnIndex)
                {
                    Accept(DirectorCommand.PerformHold(Session.Projection.SelectedActor.CardInstanceId));
                }
            }

            public StageOutcome AdvanceOneEndOfRoundStage()
            {
                Assert.That(Session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                var stage = Session.Projection.CurrentEndOfRoundStage;
                Assert.That(stage.HasValue, Is.True);
                var result = Session.Execute(DirectorCommand.AdvanceEndOfRound());
                Assert.That(result.Accepted, Is.True, result.Detail);
                var events = Record(result);
                return new StageOutcome(stage.Value, events);
            }

            public void FinishEndOfRound()
            {
                while (Session.Projection.Phase == RoundResolutionPhase.EndOfRound)
                {
                    // Milestone 7.6. p.8's Pity Coin can leave a hero able to afford a level, so from
                    // the second round on this scenario's end of round can stop on p.15's Upgrade a
                    // Card. The answer deliberately avoids upgrading either card this scenario commits
                    // by name, because p.15 returns the upgraded card to the deck and this fixture
                    // plays Bulwark and Feint across three rounds; every hero here has three non-Basic
                    // cards, so one is always free to upgrade. What leveling itself does is
                    // `HeroLevelUpTests`' subject and the stage is `EndOfRoundLevelUpStageTests`'.
                    var pending = Session.Projection.PendingSystemTransitionStep;
                    if (pending != null)
                    {
                        var spare = pending.Invocations
                            .Where(candidate =>
                            {
                                var upgraded = GuardsDigital.Core.Rules.Progression.HeroLevelUpOption
                                    .Read(candidate.OptionId).UpgradedCardInstanceId;
                                return !upgraded.Equals(BulwarkCardId) && !upgraded.Equals(FeintCardId);
                            })
                            .ToArray();
                        Assert.That(spare, Is.Not.Empty,
                            "p.15 offers every tied lowest-Tier card, so one that this scenario does " +
                            "not name is always among them.");
                        var answered = Session.Execute(
                            DirectorCommand.AnswerSystemTransitionStep(spare[0]));
                        Assert.That(answered.Accepted, Is.True, answered.Detail);
                        Record(answered);
                        continue;
                    }

                    AdvanceOneEndOfRoundStage();
                }
            }

            public void AdvanceThroughEndOfRound()
            {
                Assert.That(Session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                FinishEndOfRound();
                Assert.That(Session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
            }

            // -- readings ------------------------------------------------------------------------

            public IReadOnlyList<ActiveEffectInstance> Effects() =>
                Restore(Session).Round.Timing.Effects;

            public ActiveEffectPhase Phase(string definitionId) =>
                PhaseOf(Single(definitionId).Id);

            public ActiveEffectPhase PhaseOf(ActiveEffectId id) =>
                Effects().Single(effect => effect.Id.Equals(id)).Phase;

            public CardStateFacts CardState(CardInstanceId id) =>
                Session.Projection.Cards.Single(card => card.Id.Equals(id)).State;

            public ActiveEffectId EffectCreatedIn(string definitionId, int roundIndex) =>
                Effects().Single(effect =>
                    effect.Definition.DefinitionId == definitionId &&
                    effect.CreatedRoundIndex == roundIndex).Id;

            public IReadOnlyList<ActiveEffectLifecycleEvent> LifecycleEvents(string definitionId) =>
                LifecycleOf(Single(definitionId).Id);

            public IReadOnlyList<ActiveEffectLifecycleEvent> LifecycleOf(ActiveEffectId id) =>
                lifecycle.Where(value => value.EffectId.Equals(id)).ToArray();

            public int EndingEventCount(string definitionId) => EndingEventCountOf(Single(definitionId).Id);

            public int EndingEventCountOf(ActiveEffectId id) =>
                LifecycleOf(id).Count(value =>
                    value.Kind == ActiveEffectLifecycleEventKind.Expired ||
                    value.Kind == ActiveEffectLifecycleEventKind.Cancelled);

            /// <summary>The definition ids of the effects activated so far, in the order they activated.</summary>
            public IReadOnlyList<string> ActivationOrder()
            {
                var byId = Effects().ToDictionary(effect => effect.Id.Value, effect => effect.Definition.DefinitionId);
                return lifecycle
                    .Where(value => value.Kind == ActiveEffectLifecycleEventKind.Activated)
                    .Select(value => byId[value.EffectId.Value])
                    .ToArray();
            }

            private ActiveEffectInstance Single(string definitionId)
            {
                var matches = Effects()
                    .Where(effect => effect.Definition.DefinitionId == definitionId)
                    .ToArray();
                Assert.That(matches.Length, Is.EqualTo(1),
                    "This reading names one instance of " + definitionId + "; the run holds " + matches.Length + ".");
                return matches[0];
            }

            // -- driving helpers -----------------------------------------------------------------

            private void ResolveSkill(ActionDefinitionId skill, EffectNodeId mode)
            {
                var offer = Session.Projection.AvailableTurnActions
                    .Single(candidate => candidate.ActionDefinitionId.Equals(skill));
                Accept(DirectorCommand.StartAction(offer.ActionDefinitionId));
                var choice = Session.Projection.PendingEffectChoice;
                Accept(DirectorCommand.AnswerEffectChoice(choice.SelectInvocations
                    .Single(candidate => candidate.Candidate.OptionNodeId.Equals(mode))));
                Accept(DirectorCommand.AnswerEffectChoice(
                    Session.Projection.PendingEffectChoice.ConfirmInvocation));
            }

            private void CommitNamedPair(CardInstanceId red, CardInstanceId blue)
            {
                Assert.That(Session.Projection.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments));
                foreach (var id in new[] { red, blue })
                {
                    var card = Session.Projection.Cards.Single(candidate => candidate.Id.Equals(id));
                    Assert.That(card.State.Location, Is.EqualTo(CardLocation.Hand),
                        id.Value + " must be back in hand for this round's play.");
                    Accept(DirectorCommand.CommitCard(card.OwnerId, card.Id));
                }

                ResolveInitiativeTie();
            }

            private CardInstanceId LowestBlueCardExcept(CardInstanceId excluded) =>
                HandCards(TeamSide.Blue)
                    .Where(card => !card.Id.Equals(excluded))
                    .OrderBy(card => DefinitionOf(card).Initiative.Value)
                    .First().Id;

            private CardInstanceId HighestRedCard() =>
                HandCards(TeamSide.Red)
                    .OrderByDescending(card => DefinitionOf(card).Initiative.Value)
                    .First().Id;

            private CardDefinition DefinitionOf(CardInstance card) =>
                Session.Projection.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference));

            private CardInstance[] HandCards(TeamSide side) =>
                Session.Projection.Cards
                    .Where(card =>
                        Session.Projection.Participants.GetTeamForHero(card.OwnerId).Side == side &&
                        card.State.Location == CardLocation.Hand)
                    .OrderByDescending(card => DefinitionOf(card).Initiative.Value)
                    .ToArray();

            private void ResolveInitiativeTie()
            {
                var tie = Session.Projection.PendingInitiativeChoice;
                if (tie != null)
                {
                    Accept(DirectorCommand.ChooseTiedActor(tie.OwningSide, tie.CandidateCardInstanceIds[0]));
                }
            }

            private void Accept(DirectorCommand command)
            {
                var result = Session.Execute(command);
                Assert.That(result.Accepted, Is.True, result.Detail);
                Record(result);
            }

            private IReadOnlyList<ActiveEffectLifecycleEvent> Record(MatchCommandResult result)
            {
                var recorded = result.Events
                    .Select(value => value.ActiveEffectEvent.EffectId.IsDefined
                        ? value.ActiveEffectEvent
                        : value.TurnEvent.ActiveEffectEvent)
                    .Where(value => value.EffectId.IsDefined)
                    .ToArray();
                lifecycle.AddRange(recorded);
                return recorded;
            }

            // -- shared construction --------------------------------------------------------------

            public static string[] PhasesOf(MatchSession session) =>
                Restore(session).Round.Timing.Effects
                    .Select(effect => effect.Definition.DefinitionId + ":" + effect.Phase + ":" + effect.EndedAt)
                    .ToArray();

            private static RestoredAuthoritativeMatch Restore(MatchSession session) =>
                AuthoritativeSnapshotSerializer.Deserialize(
                    MatchContentCatalogs.ForThisBuild(),
                    session.Projection.AuthoritativeSnapshot,
                    session.Projection.Definitions,
                    new BoardMapDefinition[] { session.Projection.SelectedMap },
                    session.Projection.Setup.Minions);

            /// <summary>The ordinary configured match on the official map.</summary>
            public static MatchSession CreateSession()
            {
                var configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
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
                return new MatchSession(new MatchSessionInitialState(scenario, setup, new BoardMapDefinition[] { map }));
            }
        }

        /// <summary>One end-of-round stage advance: which stage ran, and what it did to the lifecycle.</summary>
        private sealed class StageOutcome
        {
            private readonly IReadOnlyList<ActiveEffectLifecycleEvent> events;

            public StageOutcome(EndOfRoundStage stage, IReadOnlyList<ActiveEffectLifecycleEvent> events)
            {
                Stage = stage;
                this.events = events;
            }

            public EndOfRoundStage Stage { get; }

            public IReadOnlyList<ActiveEffectLifecycleEvent> AllLifecycle() => events;

            public IReadOnlyList<ActiveEffectLifecycleEvent> LifecycleOf(ActiveEffectId id) =>
                events.Where(value => value.EffectId.Equals(id)).ToArray();

            public IReadOnlyList<string> Lifecycle(string definitionId) =>
                events.Where(value => value.EffectId.Value.EndsWith(definitionId, StringComparison.Ordinal))
                    .Select(value => value.Kind + "@" + value.Checkpoint)
                    .ToArray();
        }
    }
}
