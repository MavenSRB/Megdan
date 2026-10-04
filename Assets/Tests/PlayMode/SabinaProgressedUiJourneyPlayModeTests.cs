using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.SabinaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Sabina's upgraded cards played through the real surface, from rulebook p.15's staged Tier II and
    /// Tier III positions. Milestone 8.22, on the `DEV-004` terms milestone 8.15 recorded: coins are
    /// earned only through p.13's Defeat, so the rounds a live progression needs are outside a
    /// journey's budget and these fixtures start from the position the page's own procedure produces.
    ///
    /// <para>The branch each staging takes is p.15's choice, not a variant: a seat holds three of a
    /// Tier's six cards and the sibling it did not take becomes p.16's item. The second-sibling arms
    /// are the half Shootout, Steady Advance, Close Support, Bullet Hell, Unwavering Resolve and
    /// Covering Fire are on.</para>
    ///
    /// <para><b>Each journey pays for its own board.</b> Her Red kit reads what was adjacent when the
    /// Attack landed and her Blue kit counts what is in the disk, so three of these journeys spend a
    /// turn walking or swapping into the position the printed sentence is about - and every one of
    /// those setup turns rides the headless command surface, because the walk is
    /// <see cref="SabinaUiJourneyPlayModeTests.Sabina_CommitsFromHerOwnTrayAndWalksHerSecondaryMovementToAClickedSpace"/>'s
    /// subject rather than theirs.</para>
    /// </summary>
    public sealed class SabinaProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Shootout: "Target a unit in range. After the attack: If the target <em>was</em> adjacent to
        /// you remove up to one enemy minion adjacent to you."
        ///
        /// <para>The past tense is the whole point, and it is visible here: the minion this Attack
        /// names is defeated and removed at rulebook p.14's step 6, and the removal clause still runs -
        /// where Point Blank Shot's present-tense rider finds nobody to push in the same situation. The
        /// removal is a printed "up to", so a player sees a refusal beside the candidates and answers
        /// with a board click and the selection's own confirm.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Shootout_RemovesTheAdjacentEnemyMinionItsBoardClickNames()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;

            yield return StepBesideTheEnemyMelee(root, sabinaId, SabinaCardContent.SteadyAdvance);
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.Shootout);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var adjacentEnemies = AdjacentEnemyMinions(runtime, sabinaId, sabinaAt);
            Assert.That(adjacentEnemies.Length, Is.GreaterThanOrEqualTo(2),
                "The removal needs a second enemy miniature beside her once its own target is gone; " +
                "the walked turn is what puts her where two of them stand. Beside her: " +
                string.Join(", ", adjacentEnemies.Select(view => view.Occupancy.MinionId.Value)));

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));

            var shot = adjacentEnemies[0];
            var shotId = shot.Occupancy.MinionId;
            var survivorId = adjacentEnemies[1].Occupancy.MinionId;
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(shotId),
                "The miniature beside her is inside the printed Range 2.");
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, shot.transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(shotId),
                "p.14's step 6 defeated the targeted enemy minion and took it off the board.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"If the target was adjacent to you\" is true of a miniature this Attack has " +
                "already removed, so the removal clause is reached. " + Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, hud);
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed \"up to\" must be refusable by name before anybody is named. " +
                Remaining(runtime));

            var candidates = MinionsAt(
                runtime.RoundView.Model.EffectChoiceTargets.Select(candidate => candidate.Space));
            Assert.That(candidates.Select(view => view.Occupancy.MinionId), Has.Member(survivorId),
                "The removable miniatures are named from the board as it now stands. " +
                Remaining(runtime));
            Assert.That(candidates.All(view => view.Occupancy.TeamSide != TeamSideOf(runtime, sabinaId)),
                Is.True,
                "\"enemy minion\" - none of her own is offered. Offered: " + string.Join(", ",
                    candidates.Select(view =>
                        view.Occupancy.MinionId.Value + "/" + view.Occupancy.TeamSide)));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(survivorId).transform.position),
                Is.True,
                "The removed miniature is named by clicking it where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The confirmed removal ends the card's text. " + Remaining(runtime));
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(survivorId),
                "and the named miniature was removed as well as the one the Attack defeated.");

            yield return CaptureGameView("shootout-removes-the-minion-beside-her");
        }

        /// <summary>
        /// Bullet Hell: the Tier III half of the same sentence at a wider count - "remove up to two
        /// enemy minions adjacent to you" - refused on its own control.
        ///
        /// <para>Driven as its own instance rather than left to its sibling's journey for the reason
        /// milestone 8.15 set for Electrocute beside Shock, and taking the refusal rather than the
        /// removal because "up to" deliberately treats declining as its own answer: milestone 8.20a
        /// recorded that <c>Applied</c> with nothing named and <c>DeclinedOptional</c> are different
        /// recorded outcomes, and this is the printed face where a player can choose the second.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BulletHell_RefusesItsPrintedRemovalOnItsOwnControl()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;

            yield return StepBesideTheEnemyMelee(root, sabinaId, SabinaCardContent.CoveringFire);
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.BulletHell);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var adjacentEnemies = AdjacentEnemyMinions(runtime, sabinaId, sabinaAt);
            Assert.That(adjacentEnemies.Length, Is.GreaterThanOrEqualTo(2),
                "The refusal is only worth taking while somebody is standing there to refuse.");

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var shot = adjacentEnemies[0];
            var survivorId = adjacentEnemies[1].Occupancy.MinionId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, shot.transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The past-tense condition holds, so the removal clause is reached. " +
                Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, hud);
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "\"Remove up to two\" must be refusable by name. " + Remaining(runtime));
            var commandsBefore = runtime.AcceptedCommands.Count;
            UiPlayer.ClickDecisionControl(runtime, hud, decline);
            yield return null;
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsBefore + 1),
                "One click on the refusal is one accepted command.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The refused removal ends the card's text. " + Remaining(runtime));
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.Member(survivorId),
                "and the miniature she refused to remove is still standing beside her.");
        }

        /// <summary>
        /// Steady Advance: "If there are two or more friendly minions in radius you may retrieve a
        /// discarded card, then move up to 1 space."
        ///
        /// <para>Three printed things in one sentence, and a player meets all three: a condition that
        /// decides whether the card does anything at all, a "may" answered on its own control, and a
        /// card taken back on its whole printed face under milestone 8.15a's contract. The board this
        /// runs on is built by the card before it - Back to Back puts her on the one space in reach
        /// where two friendly minions are inside her printed radius - and the discarded card it
        /// retrieves is one she spent answering the enemy's Attack, which is the only way a card of
        /// hers reaches the discard this early in a round.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SteadyAdvance_TakesBackADiscardedCardOnItsFaceAndThenStepsInsideHerRadius()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);

            yield return SwapIntoTheDiskThenSpendACardDefending(root, sabinaId, enemyId);
            var discarded = DiscardOf(runtime, sabinaId);
            Assert.That(discarded, Is.Not.Empty,
                "The retrieve needs a discarded card of hers to offer.");

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.SteadyAdvance);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);
            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var friendly = runtime.BoardView.Model.Minions
                .Where(minion => minion.TeamSide == TeamSideOf(runtime, sabinaId) &&
                    HexDistanceQuery.Calculate(minion.Coordinate, sabinaAt) <= 2)
                .ToArray();
            Assert.That(friendly.Length, Is.GreaterThanOrEqualTo(2),
                "\"two or more friendly minions in radius\" is the printed gate, and the swap is " +
                "what makes it true. In radius: " +
                string.Join(", ", friendly.Select(minion => minion.MinionId.Value)));

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed condition holds, so the card stops and offers its retrieve. " +
                Remaining(runtime));
            yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed \"may\" must be refusable by name. " + Remaining(runtime));
            var faceName = NameOf(runtime, discarded[0]);
            var cardControl = runtime.DecisionWindowView.Model.Controls.Single(control =>
                control.Card != null && control.Card.DefinitionReference.DefinitionId.Equals(discarded[0]) &&
                control.Command != null);
            Assert.That(cardControl.Label, Is.EqualTo(faceName));
            UiPlayer.EnsureVisible(runtime.DecisionWindowView.ElementFor(cardControl));
            yield return null;
            UiPlayer.ClickDecisionControl(runtime, hud, cardControl);
            yield return null;

            Assert.That(runtime.Cards.Any(card =>
                    card.OwnerId.Equals(sabinaId) &&
                    card.State.Location == CardLocation.Hand &&
                    card.DefinitionReference.DefinitionId.Equals(discarded[0])),
                Is.True,
                "The card she named on its own face is back in her hand. " + Remaining(runtime));

            // "then move up to 1 space" - the step, clicked on the board. Her own space is offered
            // too, because "up to" includes staying, so the journey takes the one that is a move.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The retrieve is followed by the printed step. " + Remaining(runtime));
            var destinations = LitDestinations(runtime)
                .Where(space => !space.Equals(sabinaAt))
                .ToArray();
            Assert.That(destinations, Is.Not.Empty,
                "\"move up to 1 space\" must offer somewhere other than where she is standing. " +
                Remaining(runtime));
            foreach (var destination in destinations)
            {
                Assert.That(HexDistanceQuery.Calculate(destination, sabinaAt), Is.EqualTo(1),
                    "and every offered space is the one step the face prints.");
            }

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var moveNode = StandingNode(runtime);
            var chosen = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(chosen).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, moveNode);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both halves of the sentence are answered. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate,
                Is.EqualTo(chosen),
                "and she is standing on the space she clicked.");
            Assert.That(HeroView(sabinaId).Coordinate, Is.EqualTo(chosen),
                "with her token drawn there rather than where she started.");

            yield return CaptureGameView("steady-advance-takes-back-the-card-it-offered");
        }

        /// <summary>
        /// Close Support: "An enemy hero in radius adjacent to your friendly minion discards a card,
        /// if able."
        ///
        /// <para>Two bounds measured from two different places - the disk from Sabina, the adjacency
        /// from each of her own minions - and a player answers with one click on the hero standing in
        /// both. The card the enemy then loses is chosen by <em>them</em>, on their own printed face:
        /// rulebook p.10 gives a discard to the hero who pays it, which is why the prompt that follows
        /// belongs to the other seat.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CloseSupport_NamesTheEnemyHeroBesideHerMinionAndTakesACardFromTheirHand()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.CloseSupport);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(enemyAt, sabinaAt), Is.LessThanOrEqualTo(3),
                "The enemy hero has to be inside the printed Radius 3 for the card to name them.");
            Assert.That(runtime.BoardView.Model.Minions.Any(minion =>
                    minion.TeamSide == TeamSideOf(runtime, sabinaId) &&
                    HexDistanceQuery.Calculate(minion.Coordinate, enemyAt) == 1),
                Is.True,
                "and one of her own minions has to be standing beside them, which is the second of " +
                "the two bounds and the one the staging supplies.");

            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed Skill must stop and ask which enemy hero. " + Remaining(runtime));
            var candidates = runtime.RoundView.Model.EffectChoiceTargets;
            Assert.That(candidates, Has.Count.EqualTo(1),
                "One hero stands in both bounds on this board. Offered: " +
                string.Join(", ", candidates.Select(value => value.Space.ToString())));
            Assert.That(candidates[0].Space, Is.EqualTo(enemyAt),
                "and the offered candidate is the space the enemy hero is standing on.");
            var namedNode = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy hero is chosen by clicking their piece where it stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root, namedNode);

            // The discard belongs to the hero who pays it, and it is answered on a whole card face.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named hero must be asked which card they lose. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "p.10 gives a discard to the hero who pays it, not to the hero who played the card.");
            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            var discardNode = StandingNode(runtime);
            // MILESTONE 8.86. The face pressed is taken from the offer's FIRST invocation, so what
            // the offer admits is asserted rather than assumed: "discards a card" is a card out of
            // the hero's own hand, and every candidate has to be one.
            Assert.That(
                runtime.PendingEffectChoice.SelectInvocations.All(invocation =>
                    runtime.Cards.Single(card =>
                        card.Id.Equals(invocation.Candidate.Value.CardInstanceId)).OwnerId
                            .Equals(enemyId) &&
                    runtime.Cards.Single(card =>
                        card.Id.Equals(invocation.Candidate.Value.CardInstanceId)).State.Location ==
                            CardLocation.Hand),
                Is.True,
                "Every card the named hero is offered is a card in THEIR OWN HAND - which is what " +
                "p.10's discard is, and what makes the count below attributable. " +
                Remaining(runtime));
            var paidWith = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(
                    runtime.PendingEffectChoice.SelectInvocations[0].Candidate.Value.CardInstanceId))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", paidWith));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", paidWith));
            yield return null;
            yield return ConfirmIfStillStanding(root, discardNode);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card the enemy named answers the whole sentence. " + Remaining(runtime));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore - 1),
                "The card the hero named left their hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "\"if able\" is the arm this face prints: a hero who paid is still standing.");
        }

        /// <summary>
        /// Big Sodding Gun, rulebook p.16's Ultimate: "Your basic attack has +2 Range and +2 Attack. If
        /// you push an enemy hero, that hero discards a card, or is defeated."
        ///
        /// <para>Two printed sentences and three things a player meets: the card drawn faceup where
        /// they can see it, the standing rules raising the numbers of the basic Attack they play, and
        /// a trigger that stops the game once the push has moved somebody. The pushed hero is named by
        /// the push rather than selected - milestone 8.22's <c>OpposingHero</c> input role - so the
        /// question that arrives belongs to the other seat, on their own printed faces.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator BigSoddingGun_IsDrawnOnSabinasRailAndItsPushMakesThePushedHeroDiscard()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedSabina);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var sabinaId = SabinaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, sabinaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(
                runtime.Cards.Single(card =>
                        card.OwnerId.Equals(sabinaId) &&
                        card.DefinitionReference.DefinitionId.Equals(SabinaCardContent.BigSoddingGun))
                    .State.Location,
                Is.EqualTo(CardLocation.UltimateArea),
                "p.16 turns the Ultimate faceup in the hero's own area, which is where the staging " +
                "put it through the one transition the rules use.");

            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(sabinaId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "Sabina's rail must draw her unlocked Ultimate.");
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("Big Sodding Gun"));
            Assert.That(rail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));

            yield return StepIntoContactForTheUltimate(root, sabinaId, enemyId);
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.PointBlankShot);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var sabinaAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(sabinaId)).Coordinate;
            var before = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            yield return PointTrayAt(root, sabinaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy hero is chosen by clicking their piece where it stands.");
            yield return null;

            // The enemy's own Defense is setup. What the standing rules did to the Attack Total is
            // read off the same preview a player is shown, rather than recomputed here.
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "A hero target opens p.14's Defense response. " + Remaining(runtime));
            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "No offered face answers this Attack Total, so the push this journey is about would " +
                "have nobody to move. " + Remaining(runtime));
            Assert.That(survives.DefenseBreakdown.Text, Does.Contain("ATTACK TOTAL: 4"),
                "\"+2 Attack\" reaches the live total of her printed Attack 2 basic, and the " +
                "breakdown the defender reads says so. Shown: " + survives.DefenseBreakdown.Text);
            Assert.That(runtime.RoundPresenter.Submit(survives), Is.True);
            yield return null;
            yield return null;

            var after = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            Assert.That(after, Is.Not.EqualTo(before),
                "The basic Attack's own rider pushed the surviving adjacent hero one space. " +
                Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(after, sabinaAt), Is.EqualTo(2),
                "directly away from her, which is p.19's push.");

            // and the push is what sets the Ultimate off.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"If you push an enemy hero\" - the applied push must stop the game on the " +
                "Ultimate's own question. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "and the question belongs to the pushed hero, who was named by the push rather " +
                "than selected by anybody.");
            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            var discardNode = StandingNode(runtime);
            // MILESTONE 8.86, the same reading as Close Support's: the face pressed is the offer's
            // first invocation, so what the offer admits is asserted rather than assumed.
            Assert.That(
                runtime.PendingEffectChoice.SelectInvocations.All(invocation =>
                    runtime.Cards.Single(card =>
                        card.Id.Equals(invocation.Candidate.Value.CardInstanceId)).OwnerId
                            .Equals(enemyId) &&
                    runtime.Cards.Single(card =>
                        card.Id.Equals(invocation.Candidate.Value.CardInstanceId)).State.Location ==
                            CardLocation.Hand),
                Is.True,
                "Every card the PUSHED hero is offered is a card in their own hand. " +
                Remaining(runtime));
            var paidWith = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(
                    runtime.PendingEffectChoice.SelectInvocations[0].Candidate.Value.CardInstanceId))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", paidWith));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", paidWith));
            yield return null;
            yield return ConfirmIfStillStanding(root, discardNode);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card the pushed hero named answers the trigger. " + Remaining(runtime));
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore - 1),
                "and it left their hand.");
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "\"or is defeated\" is the escalation for a hero who could not pay, and this one " +
                "paid.");

            yield return CaptureGameView("big-sodding-gun-asks-the-pushed-hero-for-a-card");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Turn one: Sabina spends the named card's secondary Movement to stand beside the pair of
        /// enemy melee minions the staging leaves two spaces from her, which is the board her two
        /// removal faces need - one to shoot and one still standing to remove.
        /// </summary>
        private static IEnumerator StepBesideTheEnemyMelee(
            MatchSceneCompositionRoot root, HeroId sabinaId, CardDefinitionId walkOn)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, sabinaId, walkOn);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var side = TeamSideOf(runtime, sabinaId);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1)
                .OrderByDescending(value => runtime.BoardView.Model.Minions.Count(minion =>
                    minion.TeamSide != side &&
                    HexDistanceQuery.Calculate(minion.Coordinate, value.Destination) == 1))
                .ToArray();
            Assert.That(step, Is.Not.Empty, "The opened movement session offers no one-space step.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(step[0].Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Turn one: Sabina spends Path to Victory's secondary Movement to stand beside the enemy hero,
        /// which is the board the Ultimate's push needs. Its printed Initiative 3 is above the lowest
        /// card the Tier III enemy hand holds, so the walk is hers to take.
        /// </summary>
        private static IEnumerator StepIntoContactForTheUltimate(
            MatchSceneCompositionRoot root, HeroId sabinaId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.PathToVictory);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);

            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, enemyAt) == 1)
                .ToArray();
            Assert.That(step, Is.Not.Empty,
                "No one-space step of hers stands beside the enemy hero, so this staging cannot " +
                "reach the board the push needs.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(step[0].Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Turn one: Back to Back exchanges her with the friendly minion beside her, which is the one
        /// space in reach where two friendly minions stand inside her printed Radius 2 - and which
        /// leaves her adjacent to the enemy hero. Turn two: that hero's Attack, answered with a face
        /// out of her hand, which is what puts a card of hers in the discard for the retrieve to find.
        /// Both turns are setup and ride the headless surface.
        /// </summary>
        private static IEnumerator SwapIntoTheDiskThenSpendACardDefending(
            MatchSceneCompositionRoot root, HeroId sabinaId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.BackToBack);
            yield return YieldUntilTheNamedSeatActs(runtime, sabinaId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            for (var budget = 0; runtime.PendingEffectChoice != null && budget < 6; budget++)
            {
                var choice = runtime.PendingEffectChoice;
                var invocation = choice.SelectInvocations.Count > 0
                    ? choice.SelectInvocations[0]
                    : choice.ConfirmInvocation;
                Assert.That(invocation, Is.Not.Null,
                    "The staged swap must be answerable. " + Remaining(runtime));
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(invocation)),
                    Is.True);
                yield return null;
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);

            Commit(runtime, enemyId, WaspCardContent.Electrocute);
            Commit(runtime, sabinaId, SabinaCardContent.CloseSupport);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "Electrocute's 9 acts before Close Support's 3.");
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(sabinaId),
                "The swap left her beside the enemy hero, so she is a legal target.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(sabinaId)), Is.True);
            yield return null;

            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "She must be able to answer this Attack, or the journey proves a Defeat instead. " +
                Remaining(runtime));
            Assert.That(runtime.RoundPresenter.Submit(survives), Is.True);
            yield return null;
            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>The enemy miniatures standing beside the given space, in board order.</summary>
        private static StandardMinionView[] AdjacentEnemyMinions(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, HexCoordinate space)
        {
            var side = TeamSideOf(runtime, heroId);
            return Object.FindObjectsByType<StandardMinionView>()
                .Where(view => view.Occupancy != null &&
                    view.Occupancy.TeamSide != side &&
                    HexDistanceQuery.Calculate(view.Occupancy.Coordinate, space) == 1)
                .OrderBy(view => view.Occupancy.MinionId.Value, System.StringComparer.Ordinal)
                .ToArray();
        }

        private static GuardsDigital.Core.Participants.TeamSide TeamSideOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(heroId)).TeamSide;
    }
}
