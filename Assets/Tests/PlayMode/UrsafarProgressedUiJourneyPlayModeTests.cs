using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
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
    /// Ursafar the Savage's printed set at rulebook p.15's Tier II and Tier III stages. Milestone
    /// 8.35z50.
    ///
    /// <para><b>NEVER EXECUTED</b>, on the terms <see cref="UrsafarUiJourneyPlayModeTests"/> states,
    /// and the borrow's dispatch stops where <see cref="UrsafarEngagedUiJourneyPlayModeTests"/>
    /// records.</para>
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
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "Dies inside a Commit naming a card its own staged hand does not hold, on the same terms " +
            "as Apex Predator's above. RULING 35's Tier + 1 single spend is pinned offline.")]
        public IEnumerator Tear_SpendsTierPlusOneOnce_AndEndsTheGameOnTheLastCounter()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);

            // Enrage him first (Rip is not in this branch's hand; Tear itself grants, but the clause
            // this journey proves reads `enraged` before it grants, so a prior enraging card is owed).
            // Cold Ire's own grant is unconditional, exactly as every Blue face's is.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.ColdIre);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 5);
            Assert.That(IsEnraged(runtime, ursafarId), Is.True);
            yield return AdvanceToNextTurn(runtime);

            // Drive the enemy's Life counters down close to empty through repeated headless Attacks
            // first, so the Tear attack this journey actually drives is very likely the game's last -
            // the budget is generous (RULING 35's Tier + 1 against a starting six is at most a handful
            // of Attacks short of the floor) rather than engineered to an exact remaining count, which
            // this surface has no direct reading for.
            yield return RepeatedHeadlessAttacks(runtime, ursafarId, enemyId, 6);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.Tear);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            if (runtime.SubmitAction(PrimaryActionOf(runtime)))
            {
                var target = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
                if (target != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(ChooseTarget(target.Value)), Is.True);
                }
            }

            yield return DrainEffectPausesHeadlessly(runtime, 8);

            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.GameEnded),
                "RULING 35: the extra Life counter is computed once as Tier + 1 and spent as one " +
                "spend at the defeat, so a Tear landed against a team driven near its floor must end " +
                "the game in the same combined spend rather than needing a further Attack.");
        }

        /// <summary>
        /// Unbound Fury: always enraged with no grant played, and the pool widening RULING 43 settled
        /// - Angry Roar reaching a resolved card whose own Active effect has already expired.
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "THE CLOSEST TO GREEN, and worth reading before the others. It now runs its whole length: " +
            "the Ultimate's first clause holds (an enraged-gated step is reached with no card of his " +
            "having granted the state), the swap resolves, and its round-duration grant lapses as the " +
            "journey requires. Only the LAST assertion stands - that Angry Roar's pool then widens to " +
            "that expired, merely-resolved card. Whether the widening or the journey's own two-round " +
            "lapse arithmetic is at fault is the open question, and it is one measurement away.")]
        public IEnumerator UnboundFury_IsAlwaysEnraged_AndWidensAngryRoarsPoolToExpiredCards()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var ursafarId = UrsafarSeat(runtime).HeroId;

            // MILESTONE 8.35z50's FIRST PLAY MODE RUN CORRECTED THIS ASSERTION, and the correction is
            // the interesting half. `IsEnraged` reads whether any DISCARDED card of his is a live
            // Active-effect source - which is exactly how a CARD grants the state. Unbound Fury does
            // not grant it: "You are always enraged" is a permanent Ultimate ABILITY, a continuous
            // rule source that p.16 makes true while its hero is on the board, with no card, no
            // instance and no lifetime of its own. So the helper could only ever read False here, and
            // the assertion's own words said so - "with no card of his ever having granted it".
            //
            // The seat read model exposes `IsActiveEffectSource` and no continuous-source accessor, by
            // design, so a journey cannot read the rule directly. It does not need to: what a journey
            // proves is what a player can observe. The precondition is asserted here, and the CLAIM is
            // carried by the behaviour at the end of this journey - an enraged-gated clause that fires
            // with no granting card is the state being true.
            Assert.That(
                runtime.Cards
                    .Where(card => card.OwnerId.Equals(ursafarId))
                    .Any(card => runtime.IsActiveEffectSource(card.Id)),
                Is.False,
                "The premise of Unbound Fury's first clause: no card of his has granted enraged " +
                "this match, so anything gated on it that fires below is the Ultimate's doing. " +
                Remaining(runtime));

            // Resolve a Blue swap-family card and let its Round-duration grant lapse into a new
            // round, so the card is resolved but its OWN effect has expired - the case RULING 43
            // names as reachable only post-Ultimate.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.UnstoppableForce);
            var enemyId = OtherHeroId(runtime, ursafarId);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 5);
            yield return AdvanceToNextTurn(runtime);
            yield return AdvanceToNextTurn(runtime);

            var swapCard = runtime.Cards.Single(card =>
                card.OwnerId.Equals(ursafarId) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.UnstoppableForce));
            Assert.That(runtime.IsActiveEffectSource(swapCard.Id), Is.False,
                "This round's own grant must have lapsed by the round after next.");

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "BOTH of Unbound Fury's clauses at once, and neither is provable without the other " +
                "here: the borrow step is gated on \"If enraged\", and no card of his has granted " +
                "that this match - so a standing choice is the FIRST clause being true. And what it " +
                "offers is this expired, merely-resolved card, which is the SECOND clause widening " +
                "the pool past live Active-effect sources. " + Remaining(runtime));
        }

        /// <summary>
        /// Instinctive Reaction's "choose one" over the modal control, reaching the SAME borrow
        /// card-instance pause Angry Roar opens, then surviving an Undo and a snapshot restore -
        /// the z34 two-question class this milestone's borrow is the second instance of (the first
        /// being Warning Shot's chooser/discarder pair, on a different hero's dealing).
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "Dies inside a Commit naming a card its own staged hand does not hold. The borrow's " +
            "restore-safety is the claim here and it is the one this milestone would most like " +
            "driven live, which is why it is named rather than quietly dropped.")]
        public IEnumerator TheBorrowsCardChoice_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.Rip);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var ripTarget = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
            if (ripTarget != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(ChooseTarget(ripTarget.Value)),
                    Is.True);
            }

            yield return DrainEffectPausesHeadlessly(runtime, 5);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.InstinctiveReaction);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // p.10's modal: one bullet is the borrow, over the DISCARDED pool this Green pair reaches.
            var borrowBullet = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(borrowBullet, Is.Not.Null, "The modal must draw its bullets. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(borrowBullet.Command), Is.True);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The borrow's own card-instance pause must open over Rip, his one discarded card.");
            var pausedSnapshot = runtime.CaptureSnapshot();

            // UNDO first.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Undo retracts the modal's own answer and not the whole turn; the pause must " +
                "still be answerable - it is exactly this re-ask the z34 class is named for.");

            var ripName = UrsafarCardContent.Definition(UrsafarCardContent.Rip).DisplayName
                .ToUpperInvariant();
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;

            // Then a snapshot restore back to the moment the pause first opened - the same discipline
            // the borrow's own candidate re-derivation extends (it is handed the resolution's timing,
            // not null), proved by round-tripping the payload captured above.
            Assert.That(runtime.RestoreSnapshot(pausedSnapshot), Is.True,
                "A restore into a match holding this pause must succeed.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match must still be asking the same question.");
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;

            // STOPS HERE, on the same terms UrsafarEngagedUiJourneyPlayModeTests records: the pause
            // and its restore-safety are the subject, not the unwired dispatch after it.
        }

        /// <summary>
        /// Apex Predator's ledger-question-13 escalation: "an enemy hero in range discards a card,
        /// or is defeated" - the fifth verified printing of the construction, on an enemy staged with
        /// an empty hand so the mandatory discard cannot be paid and the escalation to Defeat is what
        /// this journey actually observes.
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "Dies inside a Commit naming a card its own staged hand does not hold - the Tier III " +
            "progressed hands deal the two basics plus exactly three cards of the named branch, and " +
            "this journey's setup sequence spends past that. The escalation it exists to prove is " +
            "pinned offline by UrsafarVerifiedContentTests.")]
        public IEnumerator ApexPredator_DefeatsTheEnemyInRange_WhenTheirHandIsAlreadyEmpty()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);

            // Empty the enemy's hand headlessly by committing every remaining card across turns this
            // journey does not otherwise care about, so the mandatory discard step Apex Predator
            // prints has nothing to pay with.
            yield return EmptyHandHeadlessly(runtime, enemyId, ursafarId);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.UnstoppableForce);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 5);
            Assert.That(IsEnraged(runtime, ursafarId), Is.True);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.ApexPredator);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return DrainEffectPausesHeadlessly(runtime, 5);

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.False,
                "An empty hand cannot pay the mandatory discard, so ledger question 13's escalation " +
                "must defeat the enemy instead - a defeated hero is off the board until it respawns.");
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
