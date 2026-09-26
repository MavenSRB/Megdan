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
    /// staging. Milestones 8.35z50 and 8.36f3. The audit drives every interaction through the
    /// configured player controls, including the borrowed primary Angry Roar starts and the parent
    /// Skill that resumes after it finishes.
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
        public IEnumerator ClawsThatCatch_FizzlesCalmWithNoTargetAndMovesThenTargetsOnceEnraged()
        {

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root(); var runtime = root.Runtime;
            var own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ClawsThatCatch);
            yield return YieldUntilTheNamedSeatActs(runtime, own);

            // MILESTONE 8.86. Arm one used to branch - "if the Attack is offered, assert nothing
            // pauses; otherwise assert SOMETHING is greyed" - and the second half is a pass by
            // absence that would have been satisfied by any greyed control on the board. The staging
            // puts the enemy two spaces away and the only minion three, so nothing is adjacent and
            // the mandatory "target a unit adjacent to you" has no subject: the outcome asserted is
            // that STARTING THE ATTACK ASKS NOTHING AND APPLIES NOTHING, on whichever of the two
            // surfaces the offer is drawn on, and each surface is named.
            var startable = runtime.RoundView.Model.Actions
                .Any(value => KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            var heroesBefore = runtime.Heroes.Count;
            if (startable)
            {
                yield return StartActionThroughControl(root, ActionKind.Attack);
                Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                    "\"Target a unit adjacent to you\" - nobody is adjacent on this staging, so p.14 " +
                    "asks nothing. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "and step 7's grant is never reached, because p.10 stops the card at the " +
                    "mandatory targeting step it cannot complete. " + Remaining(runtime));
            }
            else
            {
                Assert.That(
                    runtime.RoundView.Model.UnavailableActions.Any(value =>
                        KindOf(runtime, value.ActionDefinitionId) == ActionKind.Attack),
                    Is.True,
                    "The refusal has to be THIS card's Attack rather than any greyed control: rule " +
                    "14 refuses an action whose mandatory step can be proven to have no subject. " +
                    "Greyed: " + string.Join(", ",
                        runtime.RoundView.Model.UnavailableActions.Select(value => value.Label)) +
                    ". " + Remaining(runtime));
            }

            Assert.That(runtime.Heroes.Count, Is.EqualTo(heroesBefore),
                "and nobody was attacked either way - the card stopped before it reached anybody.");
            Assert.That(IsEnraged(runtime, own), Is.False);

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar, enemyDistance: 2);
            yield return UrsafarAuditStaging.EnrageThroughProwlingBrute();
            root = Root(); runtime = root.Runtime; own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ClawsThatCatch);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // MILESTONE 8.86, AND THIS IS THE HALF THE JOURNEY'S NAME PROMISED AND DECLINED. "Before
            // the attack: If enraged, you may move 1 space" was answered by pressing the pause's
            // DECLINE whenever one happened to be offered - so the MOVE arm of "moves then targets"
            // was never once driven, and a move that went nowhere would have read the same. The
            // optional step is TAKEN, a destination away from his own space is clicked, and the
            // board is read before the target is named from where the move left him.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"If enraged, you may move 1 space\" - he is enraged, so the printed `may` stands. " +
                Remaining(runtime));
            var takeTheMove = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Confirm);
            Assert.That(takeTheMove, Is.Not.Null,
                "and it is takeable on its own control beside its refusal. " + Remaining(runtime));
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null; yield return null;

            // The step is chosen TOWARDS the enemy rather than anywhere: the printed order is move
            // then target, and "target a unit adjacent to you" is judged from the space the move
            // leaves him in - so a step that walked him out of contact would prove the reading by
            // breaking the rest of the sentence.
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemy)).Coordinate;
            var steps = LitDestinations(runtime)
                .Where(space => !space.Equals(stoodAt))
                .ToArray();
            Assert.That(steps, Is.Not.Empty,
                "\"you may move 1 space\" - taking the option has to offer a space that is not the " +
                "one he stands on. " + Remaining(runtime));
            var contact = steps
                .Where(space =>
                    GuardsDigital.Core.Board.HexDistanceQuery.Calculate(space, enemyAt) == 1)
                .ToArray();
            Assert.That(contact, Is.Not.Empty,
                "and one of them stands beside the enemy, which is what makes the targeting step " +
                "after it reachable from where the move leaves him. Offered: " +
                string.Join(", ", steps.Select(space => space.ToString())) + ". " +
                Remaining(runtime));
            var moveNode = StandingNode(runtime);
            var landing = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), contact);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(landing).transform.position), Is.True);
            yield return null;
            yield return XargathaEngagedUiJourneyPlayModeTests.ConfirmIfStillStanding(root, moveNode);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.EqualTo(landing),
                "RULING 46/47's card-text move really moved him. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.Not.EqualTo(stoodAt),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the printed order is move THEN target, so " +
                "the space the target is judged from is not the space he started on.");

            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(false);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                "and the target named from the space the move left him on was attacked with the " +
                "Defense declined, which p.14 makes p.13's Defeat. " + Remaining(runtime));
            var card = runtime.Cards.Single(value => value.OwnerId.Equals(own) && value.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.ClawsThatCatch));
            Assert.That(runtime.IsActiveEffectSource(card.Id), Is.True);
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Angry Roar, RULING 43's pool and the complete borrowed Movement primary: selection,
        /// movement, borrowed card text, and return to the borrowing Skill.
        /// </summary>
        [UnityTest]
        public IEnumerator AngryRoar_GrantsCalmAndOpensTheBorrowChoiceOnceEnraged()
        {

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar);
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(IsEnraged(runtime, own), Is.True);
            Assert.That(runtime.PendingEffectChoice, Is.Null);

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar);
            yield return UrsafarAuditStaging.EnrageThroughProwlingBrute();
            root = Root(); runtime = root.Runtime; own = UrsafarSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("roar-borrow"));
            var pausedSnapshot = runtime.CaptureSnapshot();
            Assert.That(runtime.RestoreSnapshot(pausedSnapshot), Is.True,
                "A borrowed-card choice depends on the restored Active-effect lifecycle and must " +
                "survive the same snapshot round trip rollback uses.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pausedSnapshot));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("roar-borrow"));
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "PROWLING BRUTE");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, "Selecting the borrowed card must actually start its action. " + Remaining(runtime));
            yield return UrsafarAuditStaging.FinishMovement();
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, "The borrowed card still applies its swap text.");
            var decline = runtime.RoundView.Model.EffectChoices.Single(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            UiPlayer.ClickEffectAnswer(runtime, root.Hud, decline.Command); yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        [UnityTest]
        public IEnumerator AngryRoar_BorrowedGoldAttackMovesIntoRangeAndReturnsToSilver()
        {
            yield return UrsafarAuditStaging.Load(
                SceneCardContent.VerifiedEngagedStagedUrsafar, enemyDistance: 1);
            var root = Root(); var runtime = root.Runtime;
            var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ClawsThatCatch);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(true);
            var gold = runtime.Cards.Single(card => card.OwnerId.Equals(own) &&
                card.DefinitionReference.DefinitionId.Equals(UrsafarCardContent.ClawsThatCatch));
            Assert.That(runtime.IsActiveEffectSource(gold.Id), Is.True, Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.ProwlingBrute);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemy)).Coordinate;
            var away = runtime.PendingMovementChoice.LegalDestinations
                .Select(choice => choice.Destination)
                .First(space =>
                    GuardsDigital.Core.Board.HexDistanceQuery.Calculate(space, enemyAt) == 2);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(away).transform.position), Is.True);
            yield return null;
            if (runtime.PendingMovementChoice != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }
            var swapDecline = runtime.RoundView.Model.EffectChoices
                .FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
            if (swapDecline != null)
            {
                UiPlayer.ClickEffectAnswer(runtime, root.Hud, swapDecline.Command);
                yield return null;
            }
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.EqualTo(away), Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.AngryRoar);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice?.NodeId.Value, Is.EqualTo("roar-borrow"),
                Remaining(runtime));
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", "CLAWS THAT CATCH");
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The borrowed Gold's before-attack movement must be offered. " + Remaining(runtime));
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null; yield return null;
            var contact = LitDestinations(runtime).First(space =>
                GuardsDigital.Core.Board.HexDistanceQuery.Calculate(space, enemyAt) == 1);
            var moveNode = StandingNode(runtime);
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, Tile(contact).transform.position), Is.True);
            yield return null;
            yield return XargathaEngagedUiJourneyPlayModeTests.ConfirmIfStillStanding(root, moveNode);
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate,
                Is.EqualTo(contact), "The borrowed before-attack move must change the board.");
            Assert.That(UiPlayer.ClickBoard(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(false);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                "The borrowed Gold attack must actually reach and defeat its target. " +
                Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")),
                Is.False);
        }

        /// <summary>
        /// Prey Drive's "and the target was not removed" - milestone 8.35z50's own new
        /// engine piece, proved end to end: enraged, the target survives its own Attack (a hero
        /// target, never removed by p.13's Defeat), and the up-to-1 minion in radius is removed.
        /// Prey Abundance and Feeding Frenzy share this row's mechanism, at their own printed radius
        /// and count.
        /// </summary>
        [UnityTest]
        public IEnumerator PreyDrive_RemovesAnAdjacentEnemyMinionOnceEnragedAndTheTargetSurvived()
        {

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar);
            yield return UrsafarAuditStaging.EnrageThroughProwlingBrute();
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.PreyDrive);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            yield return UrsafarAuditStaging.AnswerDefense(true);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True);
            var minion = runtime.Projection.Minions.Single(value => value.MinionId.Value == "audit-prey");
            var node = StandingNode(runtime);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                Object.FindObjectsByType<GuardsDigital.Presentation.Board.HexTileView>().Single(tile => tile.Coordinate.Equals(minion.Coordinate)).transform.position), Is.True);
            yield return null;
            yield return XargathaEngagedUiJourneyPlayModeTests.ConfirmIfStillStanding(root, node);
            Assert.That(runtime.Projection.Minions.Any(value => value.MinionId.Equals(minion.MinionId)), Is.False);
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Sniff Out's Green family: "If enraged, an enemy hero in range discards a card, if able."
        /// Eyes on the Prey shares this row's mechanism at Range 3; Apex Predator's escalation to
        /// "or is defeated" is content this journey does not drive - see its own row.
        /// </summary>
        [UnityTest]
        public IEnumerator SniffOut_MakesTheEnemyInRangeDiscardOnceEnraged()
        {

            yield return UrsafarAuditStaging.Load(SceneCardContent.VerifiedEngagedStagedUrsafar);
            yield return UrsafarAuditStaging.EnrageThroughProwlingBrute();
            var root = Root(); var runtime = root.Runtime; var own = UrsafarSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            CommitSoTheNamedSeatActs(runtime, own, UrsafarCardContent.SniffOut);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud, HeroView(enemy).transform.position), Is.True);
            yield return null; yield return null;
            var before = runtime.Cards.Count(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand);
            var node = StandingNode(runtime);
            var choice = runtime.RoundView.Model.EffectChoices.First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            // MILESTONE 8.86: the card is NAMED, and the assertion afterwards is about that card
            // rather than about a count a card of anybody's reaching a pile would satisfy.
            var discarded = choice.Command.EffectChoice.Candidate.Value.CardInstanceId;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(discarded)).OwnerId,
                Is.EqualTo(enemy),
                "\"an enemy hero in range discards a card\" - the face offered is the enemy's own. " +
                Remaining(runtime));
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", choice.Label);
            UiPlayer.EnsureVisible(face); yield return null; UiPlayer.Click(face); yield return null;
            yield return XargathaEngagedUiJourneyPlayModeTests.ConfirmIfStillStanding(root, node);
            Assert.That(runtime.Cards.Count(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand), Is.EqualTo(before - 1));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(discarded)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "and it is THE CLICKED FACE that left their hand for their pile. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
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
                .Where(card => card.OwnerId.Equals(heroId))
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
