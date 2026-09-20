using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BroganEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BroganUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Brogan's upgraded cards played through the real surface, from rulebook p.15's staged Tier II and
    /// Tier III positions. Milestone 8.21, on the `DEV-004` terms milestone 8.15 recorded: coins are
    /// earned only through p.13's Defeat, so the rounds a live progression needs are outside a
    /// journey's budget and these fixtures start from the position the page's own procedure produces.
    ///
    /// <para>The branch each staging takes is p.15's choice, not a variant: a seat holds three of a
    /// Tier's six cards and the sibling it did not take becomes p.16's item. The second-sibling arms
    /// are the half Throwing Axe and Shield Bash are on.</para>
    /// </summary>
    public sealed class BroganProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Throwing Axe: "Choose one — • Target a unit adjacent to you. • You may discard a card; if
        /// you do, target a unit in range."
        ///
        /// <para>Rulebook p.10's modal form, and the first printed face in this build to use it. The
        /// bullets are not a confirm-or-decline and not a piece on the board: they are two printed
        /// sentences, of which the chosen one goes on to ask its own questions. This journey takes the
        /// ranged bullet, because that is the half that spends a card for reach and therefore the half
        /// that shows the modal choice deciding something.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThrowingAxe_ChoosesItsRangedBulletAndSpendsACardToTargetAtRange()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.ThrowingAxe);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Throwing Axe's printed Initiative 7 acts before the enemy's lowest card.");
            yield return PointTrayAt(root, broganId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // The modal choice itself: two printed bullets, drawn as two controls.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed \"Choose one\" must stop the Attack before it asks for a target.");
            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2),
                "p.10's modal form offers exactly the bullets the face prints. Shown: " +
                UiPlayer.Describe(hud));
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Empty,
                "A bullet is a printed sentence rather than a piece, so nothing is offered on the " +
                "board while the mode is being chosen.");
            UiPlayer.ClickEffectAnswer(runtime, hud, bullets[1].Command);
            yield return null;

            // "You may discard a card" - the printed may, answered on the whole face of the card spent.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The ranged bullet must ask for the card it spends. " + Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline), Is.True,
                "and declining must be drawn, because the bullet prints `may`.");
            var spent = runtime.RoundView.Model.EffectChoices
                .First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(spent.Card, Is.Not.Null,
                "A card-valued choice is drawn as the whole printed face, per milestone 8.15a.");
            var spentName = spent.Card.DisplayName.ToUpperInvariant();
            var discardBefore = DiscardOf(runtime, broganId).Length;
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", spentName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", spentName));
            yield return null;

            Assert.That(DiscardOf(runtime, broganId), Has.Length.EqualTo(discardBefore + 1),
                "The clicked face was spent, which is the condition the rest of the bullet reads.");

            // "target a unit in range" - the printed Range 2 selection, answered on the board.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The spent card must open the ranged target question. " + Remaining(runtime));
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Is.Not.Empty,
                "A unit selection is answered on the map. " + Remaining(runtime));
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .FirstOrDefault(view => view.Occupancy != null &&
                    candidates.Any(candidate => candidate.Space.Equals(view.Occupancy.Coordinate)));
            Assert.That(minion, Is.Not.Null,
                "This journey targets a miniature rather than the enemy hero, whose own p.14 Defense " +
                "response is another journey's subject. Offered: " +
                string.Join(", ", candidates.Select(candidate => candidate.Space.ToString())));
            var targetId = minion.Occupancy.MinionId;
            var broganAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(broganId)).Coordinate;
            Assert.That(
                HexDistanceQuery.Calculate(minion.Occupancy.Coordinate, broganAt),
                Is.LessThanOrEqualTo(2),
                "and every candidate is inside the printed Range 2 the bullet bought.");
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, minion.transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The chosen bullet's questions are all answered. " + Remaining(runtime));
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId),
                "and the Attack resolved against the unit the ranged bullet named.");

            yield return CaptureGameView("throwing-axe-chooses-its-printed-bullet");
        }

        /// <summary>
        /// Shield Bash: "An enemy hero adjacent to you, who has played an attack card this turn
        /// discards a card, if able."
        ///
        /// <para>The printed condition is part of who may be chosen rather than a gate on the
        /// consequence - "not one to offer and then refuse" - so what a player meets is a candidate set
        /// that already excludes an adjacent enemy who committed something else. The discard is then
        /// answered by the hero who pays it, on that hero's own card faces, which is the one place in
        /// this build where two players answer inside one turn.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ShieldBash_MakesTheAdjacentAttackerDiscardOnHerOwnPrintedFace()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Setup, headlessly: Shield Bash names an enemy hero adjacent to Brogan, and the staging
            // puts the two heroes two apart, so turn one closes the gap with his own secondary
            // Movement. Turn two is the enemy's committed Attack card, which is what the printed
            // condition reads - p.10 makes a card played the moment it is placed facedown.
            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Onslaught);
            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .First(value => HexDistanceQuery.Calculate(value.Destination, enemyAt) == 1);
            Assert.That(runtime.BoardPresenter.SubmitDestination(step.Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);

            Commit(runtime, enemyId, WaspCardContent.MagneticDagger);
            Commit(runtime, broganId, BroganCardContent.ShieldBash);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Magnetic Dagger's 12 acts before Shield Bash's 9, so the enemy attacks first.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    runtime.PendingAttackTargetChoice.LegalTargetMinionIds.First())),
                Is.True);
            yield return DrainEffectPausesHeadlessly(runtime, 8);
            // Milestone 8.35g. The enemy's resolution is finished and their player holds the turn;
            // the release is what hands it to Brogan inside the same turn number.
            Assert.That(UiPlayer.ReleaseFinishedTurn(runtime), Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "and Brogan's own turn follows inside the same turn number.");

            // The subject: the enemy is offered as a candidate because of what she committed.
            yield return PointTrayAt(root, broganId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill must ask which adjacent attacker discards. " + Remaining(runtime));
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Has.Count.EqualTo(1),
                "The one candidate is the enemy hero standing beside him who played an Attack card " +
                "this turn. Offered: " +
                string.Join(", ", candidates.Select(candidate => candidate.Space.ToString())));
            var enemyView = HeroView(enemyId);
            Assert.That(candidates[0].Space, Is.EqualTo(enemyView.Coordinate));
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, enemyView.transform.position), Is.True,
                "The named hero is chosen by clicking her piece where she stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root);

            // The discard is the named hero's own answer, on her own faces.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named hero must be asked which card she spends. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "p.10 gives the discard to the hero paying it, not to the hero who named her.");
            var spentName = runtime.RoundView.Model.EffectChoices
                .First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .Card.DisplayName.ToUpperInvariant();
            var enemyDiscardBefore = DiscardOf(runtime, enemyId).Length;
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", spentName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", spentName));
            yield return null;
            yield return ConfirmIfStillStanding(root);

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The Skill's two questions are answered. " + Remaining(runtime));
            Assert.That(DiscardOf(runtime, enemyId), Has.Length.EqualTo(enemyDiscardBefore + 1),
                "and the adjacent attacker paid a card out of her own hand.");
        }

        /// <summary>
        /// p.16's unlocked Ultimate, drawn where a player can see it. One Man Army carries no triggered
        /// effect at all - it is a permanent Minion-Battle ability rather than something that fires -
        /// so what the rail has to show is that it is unlocked and readable, and the ability itself is
        /// owned at the authoritative tier by <c>BroganOneManArmyTests</c>.
        /// </summary>
        [UnityTest]
        public IEnumerator OneManArmy_IsDrawnOnBrogansRailAsHisUnlockedUltimate()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;

            Assert.That(
                runtime.Cards.Single(card =>
                        card.OwnerId.Equals(broganId) &&
                        card.DefinitionReference.DefinitionId.Equals(BroganCardContent.OneManArmy))
                    .State.Location,
                Is.EqualTo(CardLocation.UltimateArea),
                "p.16 turns the Ultimate faceup in the hero's own area, which is where the staging " +
                "put it through the one transition the rules use.");

            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(broganId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "Brogan's rail must draw his unlocked Ultimate.");
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("One Man Army"));
            Assert.That(rail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));

            var markers = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-ultimate-card" && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(markers, Has.Length.EqualTo(2),
                "Both seats are staged at Tier III, so both rails show an unlocked Ultimate.");

            yield return CaptureGameView("one-man-army-on-the-hero-rail");
        }

        /// <summary>
        /// Fortify at the second altitude the removal-prevention decision can be reached from: a Defeat
        /// an effect invoked rather than one rulebook p.14's Attack produced. The enemy's own Ultimate
        /// defeats a friendly minion inside Fortify's radius, and the price Brogan is offered is the
        /// basic card his Tier III face charges where its two lower siblings charge a silver one.
        ///
        /// <para><b>What this found.</b> Before milestone 8.21, answering here was refused: the
        /// triggered effect produced its last step, Initiative advanced past the standing decision, and
        /// the answer - which ends by advancing Initiative itself - was then refused against an actor
        /// whose own card was unresolved. The decision could be opened and could not be answered.
        /// <c>TurnResolutionAuthority.AdvanceInitiative</c> now withholds the advance while it stands,
        /// on the same terms it already withheld one for a pending triggered effect.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Fortify_KeepsTheMinionAnEnemyUltimateDefeatedByPayingABasicCard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedBrogan);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var broganId = BroganSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, broganId);

            // Turn one: Fortify's printed Initiative 5 raises the guard before the enemy's Skill.
            CommitSoTheNamedSeatActs(runtime, broganId, BroganCardContent.Fortify);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Fortify's 5 acts before the lowest card the enemy holds.");
            yield return PointTrayAt(root, broganId);
            var fortifyCardId = runtime.SelectedActor.CardInstanceId;
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.IsActiveEffectSource(fortifyCardId), Is.True,
                "The performed Skill is the live source of the printed round-long guard.");
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);

            // Turn two, headlessly: the enemy performs her basic Skill and her Ultimate defeats a
            // friendly minion inside Fortify's radius. Every step here is the enemy's own.
            Commit(runtime, enemyId, WaspCardContent.StaticBarrier);
            Commit(runtime, broganId, BroganCardContent.FuriousCharge);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Static Barrier's 13 acts before Furious Charge's 8.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            yield return AnswerUntilThePreventionStands(runtime, 14);

            // The subject.
            var prompt = runtime.PendingRemovalPrevention;
            Assert.That(prompt, Is.Not.Null,
                "The enemy Ultimate's Defeat must stop at its removal stage. " + Remaining(runtime));
            Assert.That(prompt.Origin,
                Is.EqualTo(RemovalPreventionOrigin.EffectInvokedMinionDefeat),
                "This altitude is a Defeat an effect invoked, not one p.14's Attack produced.");
            Assert.That(prompt.DecidingHeroId, Is.EqualTo(broganId));
            Assert.That(prompt.Payments, Is.Not.Empty,
                "Fortify charges a basic card, and p.6 deals him two.");
            var payingCard = runtime.Cards.Single(card =>
                card.Id.Equals(prompt.Payments[0].PaymentCardInstanceId));
            var subject = prompt.SubjectMinionId;

            Assert.That(runtime.RoundView.Model.RemovalPreventionPrompt, Is.Not.Null,
                "and the region must have been given it to draw.");
            var subjectView = MinionView(subject);
            // Milestone 8.62: the threatened piece wears the removal cue itself.
            Assert.That(subjectView.TargetCueState,
                Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
            Assert.That(subjectView.TargetRing.ConsequenceIntent,
                Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetConsequenceIntent.Removal)));
            Assert.That(Tile(subjectView.Occupancy.Coordinate).CornerHighlight.Kind,
                Is.EqualTo(HexCornerHighlightKind.Attack),
                "with the threatened minion identified by blood-red removal corners.");

            var faceName = NameOf(runtime, payingCard.DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingRemovalPrevention, Is.Null,
                "The clicked face answers the decision rather than being refused. " +
                Remaining(runtime));
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.Member(subject),
                "The paid-for miniature is still standing.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(broganId)), Is.True,
                "and the match carried on afterwards rather than stopping on an unanswerable pause.");

            // Milestone 8.35g. The release is withheld while any other decision stands, so it became
            // the standing question only once the prevention above was answered - and it is the
            // release, not the answer, that hands the turn to Brogan.
            Assert.That(UiPlayer.ReleaseFinishedTurn(runtime), Is.True);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(broganId),
                "Initiative had already moved on to Brogan's own turn while the decision stood - a " +
                "trigger of this shape runs after its action has ended - and the answer left it " +
                "there rather than trying to advance a second time, which is what was refused " +
                "before this milestone.");
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty,
                "and his turn is answerable, so the match is playable rather than merely unstuck.");

            yield return CaptureGameView("fortify-keeps-the-minion-an-ultimate-defeated");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Answers the enemy's own effect questions headlessly until the removal-prevention decision
        /// stands. Everything it answers belongs to the acting hero and none of it is this journey's
        /// subject; the decision it stops at is.
        /// </summary>
        private static IEnumerator AnswerUntilThePreventionStands(
            ConfiguredMatchSceneRuntime runtime, int budget)
        {
            for (var step = 0; step < budget; step++)
            {
                if (runtime.PendingRemovalPrevention != null)
                {
                    yield break;
                }

                // Milestone 8.35g. A finished resolution holds the turn for its own player, and no
                // effect choice stands while it does - so the release is pressed and the walk goes
                // on, rather than the empty choice being read as the chain having failed.
                if (UiPlayer.ReleaseFinishedTurn(runtime))
                {
                    yield return null;
                    continue;
                }

                var choice = runtime.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null,
                    "The enemy Ultimate must reach its Defeat before the budget runs out. " +
                    Remaining(runtime));
                if (choice.SelectInvocations.Count > 0)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                            DirectorCommand.AnswerEffectChoice(choice.SelectInvocations[0])),
                        Is.True,
                        "The standing choice offered a selection its own authority refused: " +
                        runtime.LastOperationDetail + ". " + Remaining(runtime));
                }
                else if (choice.ConfirmInvocation != null)
                {
                    Assert.That(runtime.SubmitProjectedCommand(
                            DirectorCommand.AnswerEffectChoice(choice.ConfirmInvocation)),
                        Is.True,
                        "The standing choice offered a confirm its own authority refused: " +
                        runtime.LastOperationDetail + ". " + Remaining(runtime));
                }
                else
                {
                    Assert.Fail("A pause with no answer stands. " + Remaining(runtime));
                }

                yield return null;
            }

            Assert.Fail(
                "The removal-prevention decision never opened. " + Remaining(runtime));
        }
    }
}
