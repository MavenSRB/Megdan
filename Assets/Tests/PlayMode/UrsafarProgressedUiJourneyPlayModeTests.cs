using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.UrsafarEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.UrsafarUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ursafar the Savage's printed set at rulebook p.15's Tier II and Tier III stages. Milestones
    /// 8.35z50 and 8.36f3, including the complete borrowed-primary and restore paths.
    /// </summary>
    public sealed class UrsafarProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// RULING 35: Tear's extra Life counter is computed ONCE as Tier + 1 and spent as ONE spend at
        /// the defeat - including the arm that ends the game, on a team down to its last counter.
        ///
        /// <para>Ursafar's second-sibling Tier III hand carries Tear. The enemy is staged with their
        /// team's Life counters reduced to exactly Tear's Tier + 1 (a Tier III Attack's own lowest-Tier
        /// number plus one) before the attack, so a successful, enraged Tear must end the match in one
        /// spend rather than two.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Tear_SpendsTierPlusOneOnce_AndEndsTheGameOnTheLastCounter()
        {

            // The enemy still holds Tier I cards: p.13 costs the lowest card Tier, not hero Level.
            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedUrsafar, enemyDistance: 1, enemyLife: 2);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            Assert.That(runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(enemy)).Level, Is.EqualTo(7));
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.Tear);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(false);
            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded), Remaining(runtime) + "\n" +
                string.Join("\n", runtime.DiagnosticEvents.Select(entry => entry.DisplayText)));
            Assert.That(runtime.Projection.TeamLifeCounters.Any(value => value.Value == 0), Is.True);
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Unbound Fury: always enraged with no grant played, and the pool widening RULING 43 settled
        /// - Angry Roar reaching a resolved card whose own Active effect has already expired.
        /// </summary>
        [UnityTest]
        public IEnumerator UnboundFury_IsAlwaysEnraged_AndWidensAngryRoarsPoolToExpiredCards()
        {
            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            // Hold resolves the card without granting its own enrage effect. It is therefore only
            // Unbound Fury that makes this resolved, non-active card available to Angry Roar.
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.UnstoppableForce);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return null;
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(own) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.UnstoppableForce));
            Assert.That(source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.IsActiveEffectSource(source.Id), Is.False);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "UNSTOPPABLE FORCE");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            yield return UrsafarAuditStaging.FinishMovement();
            var decline = runtime.RoundView.Model.EffectChoices.Single(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            UiPlayer.ClickEffectAnswer(runtime, root.Hud, decline.Command); yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.PendingMovementChoice, Is.Null);
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Instinctive Reaction's "choose one" over the modal control, reaching the SAME borrow
        /// card-instance pause Angry Roar opens, then surviving an Undo and a snapshot restore -
        /// the z34 two-question class this milestone's borrow is the second instance of (the first
        /// being Warning Shot's chooser/discarder pair, on a different hero's dealing).
        /// </summary>
        [UnityTest]
        public IEnumerator TheBorrowsCardChoice_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar,
                discardOwn: UrsafarCardContent.ColdIre);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.InstinctiveReaction);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            // Milestone 8.46e: the borrow bullet is the decision window's, taken by its branch node id.
            yield return TakeTheBorrowBullet(root, runtime);
            var before = runtime.CaptureSnapshot();
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(own) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.ColdIre));
            var sourceState = source.State;
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "COLD IRE");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            var during = runtime.CaptureSnapshot();
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR")); yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO")); yield return null;
            // A whole-card click records selection then confirmation. Undo retracts the
            // confirmation and restores the card picker with its selection still present.
            Assert.That(runtime.PendingMovementChoice, Is.Null);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("modal-borrow"));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾")); yield return null;
            Assert.That(runtime.RestoreSnapshot(before), Is.True);
            yield return null;
            face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "COLD IRE");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.RestoreSnapshot(during), Is.True);
            yield return null;
            yield return UrsafarAuditStaging.FinishMovement();
            Assert.That(runtime.PendingMovementChoice, Is.Null);
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State, Is.EqualTo(sourceState));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// The borrowed dispatcher is shared by all three printed primary kinds. This is its Attack
        /// arm through the real modal, card picker, target, Defense and parent-Skill continuation.
        /// </summary>
        [UnityTest]
        public IEnumerator InstinctiveReaction_PerformsADiscardedAttackWithoutMovingItsCard()
        {
            yield return UrsafarAuditStaging.Load(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar,
                enemyDistance: 1,
                discardOwn: UrsafarCardContent.Rip);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            yield return EnrageThenStartInstinctiveReaction(root, runtime, own);

            var source = runtime.Cards.Single(card => card.OwnerId.Equals(own) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.Rip));
            var sourceState = source.State;
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "RIP");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
            var target = runtime.Projection.DecisionContract.Commands.Single(value =>
                value.Kind == DirectorCommandKind.ChooseAttackTarget && value.HeroId.Equals(enemy));
            Assert.That(runtime.SubmitProjectedCommand(target), Is.True,
                "The owner-scoped player command must submit the borrowed Attack target. " +
                runtime.LastOperationDetail + " " + Remaining(runtime));
            yield return null; yield return null;
            if (runtime.PendingDefenseChoice == null)
            {
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(runtime.PendingAttackTargetChoice.LegalTargets,
                    Does.Contain(AttackTarget.Hero(enemy)), Remaining(runtime));
                Assert.That(runtime.SubmitProjectedCommand(runtime.Projection.DecisionContract.Commands.Single(
                    value => value.Kind == DirectorCommandKind.ChooseAttackTarget &&
                        value.HeroId.Equals(enemy))), Is.True,
                    runtime.LastOperationDetail + " " + Remaining(runtime));
                yield return null; yield return null;
            }
            yield return UrsafarAuditStaging.AnswerDefense(false);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State,
                Is.EqualTo(sourceState));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")),
                Is.False);
        }

        /// <summary>
        /// The borrowed dispatcher's Skill arm, including the borrowed Skill's own target and
        /// opponent card choice before control returns to Instinctive Reaction.
        /// </summary>
        [UnityTest]
        public IEnumerator InstinctiveReaction_PerformsADiscardedSkillAndReturnsThroughNestedParents()
        {
            yield return UrsafarAuditStaging.Load(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar,
                enemyDistance: 1,
                discardOwn: UrsafarCardContent.AngryRoar);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ColdIre);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            yield return UrsafarAuditStaging.FinishMovement();
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.InstinctiveReaction);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            // Milestone 8.46e: the borrow bullet is the decision window's, taken by its branch node id.
            yield return TakeTheBorrowBullet(root, runtime);

            var source = runtime.Cards.Single(card => card.OwnerId.Equals(own) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.AngryRoar));
            var sourceState = source.State;
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "ANGRY ROAR");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Borrowed Angry Roar must open its own active-card choice. " + Remaining(runtime));
            face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "COLD IRE");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            yield return UrsafarAuditStaging.FinishMovement();

            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.PendingMovementChoice, Is.Null);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State,
                Is.EqualTo(sourceState));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")),
                Is.False);
        }

        private static IEnumerator EnrageThenStartInstinctiveReaction(
            MatchSceneCompositionRoot root,
            ConfiguredMatchSceneRuntime runtime,
            HeroId own)
        {
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            Assert.That(runtime.RoundView.Model.Actions.Any(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Skill),
                Is.True, "Angry Roar must expose its Skill control. " + Remaining(runtime));
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.InstinctiveReaction);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            Assert.That(runtime.RoundView.Model.Actions.Any(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Skill),
                Is.True, "Instinctive Reaction must expose its Skill control. " + Remaining(runtime));
            yield return StartActionThroughControl(root, ActionKind.Skill);
            // Milestone 8.46e: the borrow bullet is the decision window's, taken by its branch node id.
            yield return TakeTheBorrowBullet(root, runtime);
        }

        /// <summary>
        /// Milestone 8.46e. Takes Instinctive Reaction's borrow bullet where a player now takes it:
        /// on the decision window milestone 8.46 moved every `Choose one` to. The control is found by
        /// its branch node id rather than by the strip's `MODE #1 MODAL-BORROW` label, because the
        /// window names the consequence in the card's own words and the strip no longer draws the
        /// family at all.
        ///
        /// <para>Re-derived per milestone 8.46b: a choice admitting exactly one selection resolves on
        /// the select itself, so the window draws no DONE behind the bullets and there is nothing to
        /// press after the one press - the borrow's own card chooser is what opens.</para>
        /// </summary>
        private static IEnumerator TakeTheBorrowBullet(
            MatchSceneCompositionRoot root, ConfiguredMatchSceneRuntime runtime)
        {
            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            Assert.That(modal.ResolvesOnSelection, Is.True,
                "\"Choose one\" is the exactly-one shape milestone 8.46b answers in one press. " +
                Remaining(runtime));
            Assert.That(UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm), Is.Null,
                "so the window draws no DONE behind the bullets. " + Remaining(runtime));
            UiPlayer.ClickEffectOption(runtime, root.Hud, "modal-borrow");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The borrow's card chooser opens on that one press. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChoiceId, Is.Not.EqualTo(modal.ChoiceId),
                "and the modal itself is answered rather than still standing half-filled. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Apex Predator's ledger-question-13 escalation: "an enemy hero in range discards a card,
        /// or is defeated" - the fifth verified printing of the construction, on an enemy staged with
        /// an empty hand so the mandatory discard cannot be paid and the escalation to Defeat is what
        /// this journey actually observes.
        /// </summary>
        [UnityTest]
        public IEnumerator ApexPredator_DefeatsTheEnemyInRange_WhenTheirHandIsAlreadyEmpty()
        {

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar, enemyDistance: 1, emptyEnemyHand: true);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            Assert.That(runtime.Cards.Any(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand), Is.False);
            Commit(runtime, own, UrsafarCardContent.ApexPredator);
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.Pass(enemy)), Is.True);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        // ---------------------------------------------------------------------------------------
        // Steps local to this file
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Commits and resolves the named seat's hand down to nothing, headlessly, across as many
        /// turns as it takes - setup for a journey whose subject is what happens when a mandatory
        /// discard has no card left to pay with.
        /// </summary>
        private static IEnumerator EmptyHandHeadlessly(
            ConfiguredMatchSceneRuntime runtime,
            GuardsDigital.Core.Units.Heroes.HeroId handId,
            GuardsDigital.Core.Units.Heroes.HeroId otherId)
        {
            for (var attempt = 0;
                attempt < 12 &&
                runtime.Cards.Any(card =>
                    card.OwnerId.Equals(handId) && card.State.Location == CardLocation.Hand);
                attempt++)
            {
                var lowest = LowestInitiativeIdOf(runtime, handId);
                if (!runtime.IsTurnActive || runtime.SelectedActor.ActorId.Equals(otherId))
                {
                    Commit(runtime, otherId, LowestInitiativeIdOf(runtime, otherId));
                    Commit(runtime, handId, lowest);
                }
                else
                {
                    Commit(runtime, handId, lowest);
                    Commit(runtime, otherId, LowestInitiativeIdOf(runtime, otherId));
                }

                if (runtime.SubmitAction(PrimaryActionOf(runtime)))
                {
                    var target = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
                    if (target != null)
                    {
                        runtime.SubmitProjectedCommand(ChooseTarget(target.Value));
                    }
                }

                yield return DrainEffectPausesHeadlessly(runtime, 8);
                if (runtime.IsTurnActive)
                {
                    runtime.SubmitEndTurn();
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Repeats a headless Claws That Catch Attack against the enemy for <paramref name="count"/>
        /// of Ursafar's own turns, stopping early if the game ends first. Setup for a journey whose
        /// subject is the LAST Attack rather than any of these - each one's own coin/Defense variance
        /// is not this journey's concern, only that the team's Life counters end up low.
        /// </summary>
        private static IEnumerator RepeatedHeadlessAttacks(
            ConfiguredMatchSceneRuntime runtime,
            GuardsDigital.Core.Units.Heroes.HeroId attackerId,
            GuardsDigital.Core.Units.Heroes.HeroId enemyId,
            int count)
        {
            for (var attempt = 0;
                attempt < count && runtime.Phase != RoundResolutionPhase.GameEnded;
                attempt++)
            {
                CommitSoTheNamedSeatActs(runtime, attackerId, UrsafarCardContent.ClawsThatCatch);

                // Milestone 8.35z50's first Play Mode run. This used to press End Turn ONCE and
                // `continue`, which burned a whole iteration without attacking: since milestone
                // 8.35g every turn costs TWO presses of the same control - the stop that finishes
                // the resolution, then the release that hands the turn on - so one press left the
                // enemy still holding and this loop never reached its own Attack. The shared helper
                // drives the hand-off to completion instead, and there is nothing to `continue` past.
                yield return YieldUntilTheNamedSeatActs(runtime, attackerId);

                if (runtime.SubmitAction(PrimaryActionOf(runtime)))
                {
                    var target = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
                    if (target != null)
                    {
                        runtime.SubmitProjectedCommand(ChooseTarget(target.Value));
                    }
                }

                yield return DrainEffectPausesHeadlessly(runtime, 8);
                if (runtime.IsTurnActive)
                {
                    runtime.SubmitEndTurn();
                    yield return null;
                }
            }
        }
    }
}
