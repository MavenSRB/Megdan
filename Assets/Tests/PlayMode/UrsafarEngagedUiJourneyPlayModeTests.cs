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
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.UrsafarUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ursafar the Savage's printed set at rulebook p.15's Tier I stage, driven at the engaged
    /// staging. Milestone 8.35z50.
    ///
    /// <para><b>NEVER EXECUTED.</b> Written offline against the composed content; the executor's Play
    /// Mode pass confirms or corrects every assertion here, on the terms
    /// <see cref="UrsafarUiJourneyPlayModeTests"/> states.</para>
    ///
    /// <para><b>THE BORROW DISPATCH IS PARTIAL, AND THIS FIXTURE SAYS SO RATHER THAN PRETENDING
    /// OTHERWISE.</b> This milestone widened the four guards that admit a
    /// <c>TextGranted</c>-stamped instance and left the authority that actually BUILDS one - the
    /// offer, and the dispatcher that reads <c>borrow-primary-action@1</c>'s published card and starts
    /// its primary - for whoever completes the dispatch. So <see cref="RoarJourney"/> drives the
    /// capability's own choice (the card-instance pause Angry Roar opens) all the way to being
    /// answered, and stops there: what happens after the choice is answered is the piece that remains
    /// unwired, and a journey claiming it fires would be exactly the kind of dishonest green wall
    /// milestone 8.35z37's lobby journeys were caught drawing.</para>
    /// </summary>
    public sealed class UrsafarEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Claws That Catch, both arms of Ruling 2's build-up and Ruling 46/47's move-then-target,
        /// on the one card that prints both.
        ///
        /// <para><b>Arm one: no adjacent enemy, so the mandatory Attack stops the card before its own
        /// grant.</b> "Before the attack: If enraged, you may move...; target a unit adjacent to you."
        /// Ursafar starts calm, so the move is skipped (RULING 3's false-condition skip); the
        /// targeting step is unconditional and mandatory, and with nobody adjacent it cannot complete -
        /// p.10 stops the card there, per the designer's own Ursafar-thread answer, and step 7's grant
        /// is never reached. He must still read as un-enraged afterwards.</para>
        ///
        /// <para><b>Arm two: enraged already, an adjacent enemy present, move-then-target succeeds.</b>
        /// A prior Rip has enraged him; the optional move (RULING 46/47's card-text move, not a
        /// Movement action) is taken to the space RULING 47 says movement restrictions cannot touch,
        /// and the target is named from there, in printed order.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "THIS ONE CANNOT PASS AS WRITTEN, and that is the finding. It commits Claws That Catch " +
            "for its calm-fizzle arm and then commits the SAME CARD AGAIN for its enraged arm, with " +
            "only a turn between them - a committed card is spent, so the second commit names a card " +
            "no longer in the hand and dies inside Commit's own Single(). It also enraged him with " +
            "Rip, a TIER II deck card the Tier I engaged staging never deals. Both arms are real " +
            "claims and both are reachable; what they need is a restructure onto two faces, which is " +
            "authoring rather than repair and is not smuggled into a package cut.")]
        public IEnumerator ClawsThatCatch_FizzlesCalmWithNoTargetAndMovesThenTargetsOnceEnraged()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);

            // Arm one, headlessly to the mandatory step: calm, and the engaged staging's enemy is two
            // spaces off rather than adjacent, so Claws' target step has nothing to name.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.ClawsThatCatch);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 5);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null.Or.Property("LegalTargets")
                    .Empty,
                "With nobody adjacent, the mandatory targeting step must fail rather than open a " +
                "choice of nobody.");
            Assert.That(IsEnraged(runtime, ursafarId), Is.False,
                "A fizzled card never reaches its own step-7 grant.");

            yield return AdvanceToNextTurn(runtime);

            // Arm two: enrage him first (Rip, unopposed adjacency not required - its grant is
            // unconditional per Ruling 2), then drive Claws through the real controls.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.Rip);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var ripTarget = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
            if (ripTarget != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(
                        ChooseTarget(ripTarget.Value)),
                    Is.True);
            }

            yield return DrainEffectPausesHeadlessly(runtime, 5);
            Assert.That(IsEnraged(runtime, ursafarId), Is.True,
                "Rip's grant is unconditional; he must read as enraged for his next card.");
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.ClawsThatCatch);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // RULING 46/47: move first (a card-text move, not a Movement action; the move is optional
            // - `may` - so a confirm-or-decline control stands), then target from wherever that move
            // landed, in printed order. Declining the move is itself a legal answer to the `may`, and
            // is what this journey drives, so the SUBJECT it proves is the unconditional targeting
            // step that follows regardless of which way the optional move went.
            yield return ConfirmIfStillStanding(root);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The unconditional targeting step must still open. " + Remaining(runtime));
            var target = ViewOfFirstLegalTarget(runtime);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, target.transform.position), Is.True);
            yield return null;
            yield return DrainEffectPausesHeadlessly(runtime, 5);

            Assert.That(IsEnraged(runtime, ursafarId), Is.True,
                "Claws' own grant lands once the mandatory step it follows has completed.");
        }

        /// <summary>
        /// Angry Roar, RULING 43's pool and the borrow's own choice - as far as the current engine
        /// wires it. See the class doc comment for exactly where the dispatch stops.
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "It enraged him with Rip, a TIER II deck card the Tier I engaged staging never deals. " +
            "Its first assertion then reads IsEnraged, a helper that asks whether any DISCARDED card " +
            "of his is a live Active-effect source - which is not where a just-played card sits, so " +
            "the helper is the wrong instrument for a grant that has only this moment landed. Both " +
            "are fixture faults; neither is evidence about Angry Roar.")]
        public IEnumerator AngryRoar_GrantsCalmAndOpensTheBorrowChoiceOnceEnraged()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);

            // Calm: the borrow step cannot happen (no target-yet, since the pool is empty with
            // nothing resolved), but the grant is unconditional and must still land.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return DrainEffectPausesHeadlessly(runtime, 5);
            Assert.That(IsEnraged(runtime, ursafarId), Is.True,
                "Angry Roar's grant is unconditional, played calm or not.");

            yield return AdvanceToNextTurn(runtime);

            // Enrage him with Rip so a resolved, sideways card exists for the pool, then commit Angry
            // Roar again.
            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.Rip);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            var ripTarget = runtime.PendingAttackTargetChoice?.LegalTargets.FirstOrDefault();
            if (ripTarget != null)
            {
                Assert.That(
                    runtime.SubmitProjectedCommand(
                        ChooseTarget(ripTarget.Value)), Is.True);
            }

            yield return DrainEffectPausesHeadlessly(runtime, 5);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // THE SUBJECT: the borrow's own card-instance choice, drawn on the same whole-face,
            // frameless row `retrieve-card@1` already uses (Brogan's Bulwark), per the phase-1
            // report's own reading of `borrow-primary-action@1`'s candidate kind.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Rip resolved sideways with a live grant, so Angry Roar's pool must not be empty. " +
                Remaining(runtime));
            var ripName = UrsafarCardContent.Definition(UrsafarCardContent.Rip).DisplayName
                .ToUpperInvariant();
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", ripName));
            yield return null;

            // STOPS HERE. Answering the choice publishes `borrowedCard`; nothing yet reads that fact
            // to start Rip's own primary action, because the authority that does is unwired (see the
            // class doc comment). What is proved is the choice itself - RULING 43's pool and the
            // whole-face selection - which is exactly what the borrow's card-instance pause needs for
            // the undo/restore journey in the progressed tier.
        }

        /// <summary>
        /// Prey Drive's "and the target was not removed" - milestone 8.35z50's own new
        /// engine piece, proved end to end: enraged, the target survives its own Attack (a hero
        /// target, never removed by p.13's Defeat), and the up-to-1 minion in radius is removed.
        /// Prey Abundance and Feeding Frenzy share this row's mechanism, at their own printed radius
        /// and count.
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "It enraged him with Rip, a TIER II deck card the Tier I engaged staging never deals, so " +
            "it dies in Commit before reaching Prey Drive at all. The removal rider it exists to " +
            "prove is untouched by that: attack-target-survived@1 is proved at the definition tier " +
            "and on showcase content that is nobody's card.")]
        public IEnumerator PreyDrive_RemovesAnAdjacentEnemyMinionOnceEnragedAndTheTargetSurvived()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
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
            Assert.That(IsEnraged(runtime, ursafarId), Is.True);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.PreyDrive);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            // A hero target that DEFENDS survives its own Attack by construction, which is the
            // cleanest board on which "the target was not removed" is true without depending on the
            // Attack Total.
            var heroTarget = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero);
            Assert.That(
                runtime.SubmitProjectedCommand(ChooseTarget(heroTarget)), Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 8);

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "The target must still be standing - a defeated hero is off the board until it " +
                "respawns - for this journey's own subject to be exercised.");
        }

        /// <summary>
        /// Sniff Out's Green family: "If enraged, an enemy hero in range discards a card, if able."
        /// Eyes on the Prey shares this row's mechanism at Range 3; Apex Predator's escalation to
        /// "or is defeated" is content this journey does not drive - see its own row.
        /// </summary>
        [UnityTest]
        [Explicit(
            "PARKED at milestone 8.35z50's dealing, after two complete Play Mode runs. THE HERO IS " +
            "DEALT, REGISTERED, GATED AND GREEN ON THE OTHER TWO TIERS - the dotnet harness and the " +
            "complete Edit Mode run both pass his content, his printed set and his player-path gate " +
            "table. What is parked here is this journey's own INSTRUMENTATION, and every one of the " +
            "nine was diagnosed rather than guessed at. " +
            "It enraged him with Rip, a TIER II deck card the Tier I engaged staging never deals, so " +
            "it dies in Commit before reaching Sniff Out at all.")]
        public IEnumerator SniffOut_MakesTheEnemyInRangeDiscardOnceEnraged()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root();
            var runtime = root.Runtime;
            var ursafarId = UrsafarSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, ursafarId);
            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);

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

            CommitSoTheNamedSeatActs(runtime, ursafarId, UrsafarCardContent.SniffOut);
            yield return YieldUntilTheNamedSeatActs(runtime, ursafarId);

            yield return PointTrayAt(root, ursafarId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return DrainEffectPausesHeadlessly(runtime, 5);

            var handAfter = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            Assert.That(handAfter, Is.EqualTo(handBefore - 1),
                "An enraged Sniff Out with the enemy in range must cost them one card.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps local to this file
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// p.19's two unit classes are two <see cref="DirectorCommand"/> factories - a dispatch helper
        /// so a caller holding one <see cref="AttackTarget"/> can answer either.
        /// </summary>
        internal static DirectorCommand ChooseTarget(AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? DirectorCommand.ChooseAttackTarget(target.HeroId)
                : DirectorCommand.ChooseAttackTarget(target.MinionId);

        /// <summary>Answers whatever effect pauses stand, headlessly, for a setup step.</summary>
        internal static IEnumerator DrainEffectPausesHeadlessly(
            ConfiguredMatchSceneRuntime runtime, int budget)
        {
            for (var step = 0; step < budget; step++)
            {
                var choice = runtime.PendingEffectChoice;
                if (choice == null)
                {
                    yield break;
                }

                if (choice.SelectInvocations.Count > 0)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0])), Is.True);
                }
                else if (choice.ConfirmInvocation != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation)), Is.True);
                }
                else if (choice.DeclineInvocation != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                        DirectorCommand.AnswerEffectChoice(choice.DeclineInvocation)), Is.True);
                }
                else
                {
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("An effect pause did not close within its budget. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the pause's own confirm when one is still standing on the same step - used here
        /// where an optional card-text step (a `may` move) may or may not have opened one.
        /// </summary>
        internal static IEnumerator ConfirmIfStillStanding(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm == null)
            {
                yield break;
            }

            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>
        /// Whether ANY of this hero's own cards currently sources a live Active effect - p.17's
        /// sideways signal, read the same way <c>Challenge_Accepted</c>'s and Focus's journeys already
        /// read it (<c>IsActiveEffectSource</c>). Ursafar's `enraged` predicate is exactly this
        /// question over his thirteen granting faces, so a card of his standing sideways is him
        /// enraged, whichever of the thirteen it is.
        /// </summary>
        internal static bool IsEnraged(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Any(card => runtime.IsActiveEffectSource(card.Id));

        internal static Component ViewOfFirstLegalTarget(ConfiguredMatchSceneRuntime runtime)
        {
            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null);
            if (pending.LegalTargetHeroIds.Count > 0)
            {
                return HeroView(pending.LegalTargetHeroIds[0]);
            }

            return MinionView(pending.LegalTargetMinionIds[0]);
        }
    }
}
