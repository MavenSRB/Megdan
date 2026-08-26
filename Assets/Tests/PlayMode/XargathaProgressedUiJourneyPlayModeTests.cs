using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Xargatha's upgraded cards played through the real surface, from rulebook p.15's staged Tier II
    /// and Tier III positions. Milestone 8.24, on the `DEV-004` terms milestone 8.15 recorded: coins
    /// are earned only through p.13's Defeat, so the rounds a live progression needs are outside a
    /// journey's budget and these fixtures start from the position the page's own procedure produces.
    ///
    /// <para><b>Her two branches are the widest apart in the roster, and that is why both are driven
    /// here.</b> The first sibling takes the petrification line and the remaining Movement primaries;
    /// the second takes the Ranged Attacks, the retrievals and the two end-of-round Defeats. Which
    /// branch a staged hand took decides which of her mechanics exist in that match at all, so a
    /// journey on one branch cannot stand for the other the way a second Attack of the same shape
    /// can.</para>
    ///
    /// <para><b>Every value these cards ask about is counted from the pieces beside her.</b> The
    /// engagement leaves one enemy Ranged minion adjacent and two enemy Melee minions two spaces out,
    /// and that arrangement is the content of three of these four journeys: it is what widens Long
    /// Thrust's Range, what makes Fresh Converts' printed condition true, and what separates Control's
    /// candidate set from Charm's.</para>
    /// </summary>
    public sealed class XargathaProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Long Thrust: "Target a unit in range; +1 [Range] Range for each enemy unit adjacent to you."
        ///
        /// <para>Her printed Range is 1, and the enemy hero is two spaces away - so the shot this
        /// journey takes is one the card cannot take on its own. The enemy Ranged minion standing
        /// beside her is the whole difference, which is why the offered set rather than the hit is
        /// what the assertions are about: every piece at distance 2 in that list is there because of
        /// a piece at distance 1.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LongThrust_ReachesTheHeroOnlyBecauseAnEnemyStandsBesideHer()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            var printedRange = PrintedValue(
                runtime, XargathaCardContent.LongThrust, PrintedValueChannel.Range);
            Assert.That(printedRange, Is.EqualTo(1),
                "The face prints Range 1; everything past that is counted from the board.");

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.LongThrust);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            var herSpace = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;
            var hisSpace = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var adjacentEnemies = runtime.BoardView.Model.Minions
                .Count(minion => minion.TeamSide != TeamSideOf(runtime, xargathaId) &&
                    HexDistanceQuery.Calculate(minion.Coordinate, herSpace) == 1);
            Assert.That(adjacentEnemies, Is.EqualTo(1),
                "One enemy minion stands beside her on this staging, so the Range is 1 + 1.");
            Assert.That(HexDistanceQuery.Calculate(herSpace, hisSpace), Is.EqualTo(2),
                "and the enemy hero is exactly one space further than the card prints.");

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "The clicked Attack control must open p.9's ranged target question. " +
                Remaining(runtime));
            Assert.That(pending.LegalTargetHeroIds, Has.Member(enemyId),
                "The enemy hero two spaces away is offered, and only the widening puts him there. " +
                Remaining(runtime));
            foreach (var minionId in pending.LegalTargetMinionIds)
            {
                var at = MinionView(minionId).Occupancy.Coordinate;
                Assert.That(HexDistanceQuery.Calculate(at, herSpace),
                    Is.LessThanOrEqualTo(printedRange + adjacentEnemies),
                    "and nothing outside the widened Range is offered.");
            }

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position),
                Is.True,
                "The ranged target is named by clicking the piece where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "The clicked piece answers the target question. " + Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "and the shot reaches p.14's Defense response on the hero it named. " +
                Remaining(runtime));

            // The enemy's own answer is setup rather than this journey's subject, so it rides the
            // headless surface: he spends a face that survives, and the shot is proved by having
            // reached him at all.
            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "The enemy holds something that answers this total. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitDefenseCard(survives.DefenseBreakdown.CardInstanceId.Value), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "He answered it, so he is still standing - two spaces from a Range 1 card.");

            yield return CaptureGameView("long-thrust-reaches-the-hero-two-spaces-away");
        }

        /// <summary>
        /// Fresh Converts: "If you are adjacent to an enemy minion, you may retrieve a discarded card."
        ///
        /// <para>Three printed things and a player meets all three: a condition the board answers, a
        /// "may" pressed on its own control, and a card taken back on its whole printed face under
        /// milestone 8.15a's contract. The discard it retrieves has to have been made, and the only
        /// way a card of hers reaches the discard this early in a round is p.14 - so the two turns
        /// before this one are the enemy walking into contact and attacking her, and the card she
        /// takes back is the one she spent surviving it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FreshConverts_TakesBackTheCardSheSpentDefendingOnItsOwnFace()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);

            yield return TheEnemyWalksInAndAttacksHer(root, xargathaId, enemyId);
            var discarded = DiscardOf(runtime, xargathaId);
            Assert.That(discarded, Has.Length.EqualTo(1),
                "She spent exactly one face answering the Attack, and that is what the retrieve has " +
                "to offer. Discarded: " + string.Join(", ", discarded.Select(id => id.Value)));

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.FreshConverts);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            var herSpace = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;
            Assert.That(runtime.BoardView.Model.Minions
                    .Any(minion => minion.TeamSide != TeamSideOf(runtime, xargathaId) &&
                        HexDistanceQuery.Calculate(minion.Coordinate, herSpace) == 1),
                Is.True,
                "\"If you are adjacent to an enemy minion\" is the printed gate, and the staging is " +
                "what makes it true - she never moved.");

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The condition holds, so the card stops and offers its retrieve. " +
                Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "A printed \"may\" must be refusable by name. " + Remaining(runtime));
            var confirm = ConfirmControl(runtime);
            Assert.That(confirm, Is.Not.Null,
                "and it must be takeable on its own control beside the refusal. " +
                Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Taking the \"may\" opens the card question behind it. " + Remaining(runtime));
            var faceName = NameOf(runtime, discarded[0]);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            var retrieval = StandingNode(runtime);
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-effect-card", faceName));
            yield return null;
            yield return ConfirmIfStillStanding(root, retrieval);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both printed questions are answered. " + Remaining(runtime));
            Assert.That(runtime.Cards.Any(card =>
                    card.OwnerId.Equals(xargathaId) &&
                    card.State.Location == CardLocation.Hand &&
                    card.DefinitionReference.DefinitionId.Equals(discarded[0])),
                Is.True,
                "The card she named on its own face is back in her hand.");
            Assert.That(DiscardOf(runtime, xargathaId), Is.Empty,
                "and it left the discard pile it came from rather than being copied out of it.");

            yield return CaptureGameView("fresh-converts-takes-back-the-defense-she-spent");
        }

        /// <summary>
        /// Control: "You may move an enemy ranged <b>or melee</b> minion in radius up to 2 spaces
        /// before or after movement."
        ///
        /// <para>One adjective apart from Charm, and the adjective is the whole card: the same disk
        /// over the same board offers three pieces here and one there. That narrowing is a thing no
        /// earlier hero's content asks for - a radius selection in this build carried a friend/enemy
        /// relation and no minion class at all before this milestone - so the candidate set is what
        /// this journey is about, and the walk afterwards is the proof that the piece it named is the
        /// piece that moved.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Control_ReachesTheMeleeMinionsCharmRefuses()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Control);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);
            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Movement);

            // The placement side first - the question Charm's own journey is about - then the "may",
            // then the target. Three questions in order, and reading the candidates after two of them
            // reads the "may" instead of the selection.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"before or after movement\" asks before anything else. " + Remaining(runtime));
            var placement = StandingNode(runtime);
            var side = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            UiPlayer.Click(UiPlayer.Button(hud, side.Label));
            yield return null;
            yield return ConfirmIfStillStanding(root, placement);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The printed \"may\" stands behind the placement. " + Remaining(runtime));
            var take = ConfirmControl(runtime);
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "and it can be refused by name. " + Remaining(runtime));
            Assert.That(take, Is.Not.Null, "and taken by name. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, take.Label));
            yield return null;

            var offered = MinionsAt(runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty,
                "The disk covers enemy minions of both classes the card names. " +
                Remaining(runtime));
            Assert.That(
                offered.Any(view => view.Occupancy.MinionType == StandardMinionType.Melee), Is.True,
                "\"ranged OR MELEE\" is what separates this card from Charm, and Charm's own journey " +
                "proves the melee pieces standing here are not offered to it.");
            foreach (var view in offered)
            {
                Assert.That(view.Occupancy.MinionType,
                    Is.EqualTo(StandardMinionType.Ranged).Or.EqualTo(StandardMinionType.Melee),
                    "and no third class is admitted by a card naming two.");
            }

            var chosen = offered.First(view =>
                view.Occupancy.MinionType == StandardMinionType.Melee);
            var chosenId = chosen.Occupancy.MinionId;
            var stoodAt = chosen.Occupancy.Coordinate;
            var selection = StandingNode(runtime);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, chosen.transform.position), Is.True,
                "The ordered minion is named by clicking the miniature where it stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root, selection);
            yield return null;

            var destinations = LitDestinations(runtime);
            Assert.That(destinations, Is.Not.Empty,
                "\"up to 2 spaces\" must offer somewhere to put it. " + Remaining(runtime));
            foreach (var space in destinations)
            {
                Assert.That(HexDistanceQuery.Calculate(space, stoodAt), Is.LessThanOrEqualTo(2),
                    "and every offered space is inside the printed distance.");
            }

            var move = StandingNode(runtime);
            var landing = FirstClickableSpace(pointerInput, destinations);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(landing).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, move);
            yield return null;

            Assert.That(MinionView(chosenId).Occupancy.Coordinate, Is.EqualTo(landing),
                "and the melee minion she ordered is standing on the space she clicked.");

            // and then her own Movement primary, which the card's text ran in front of. p.11's
            // default is that a Movement card's text comes into effect before the walk; this face is
            // one of the three that says otherwise, and the side she picked above is what put the
            // order this way round.
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card's text is finished before her own walk begins. " + Remaining(runtime));
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The Movement primary she started is still hers to spend. " + Remaining(runtime));
            var herStart = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;
            var step = FirstClickableSpace(pointerInput, runtime.PendingMovementChoice
                .LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray());
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(step).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate,
                Is.EqualTo(step),
                "and the destination she clicked is where the printed Movement put her.");
            Assert.That(HexDistanceQuery.Calculate(step, herStart), Is.EqualTo(1));
            Assert.That(HeroView(xargathaId).Coordinate, Is.EqualTo(step),
                "with her token drawn there.");

            yield return CaptureGameView("control-orders-the-melee-minion-charm-refuses");
        }

        /// <summary>
        /// p.16's unlocked Ultimate. Metamorphosis: "Gain +1 [Movement] Movement and +1 [Initiative]
        /// Initiative for each enemy unit adjacent to you. You may move through obstacles."
        ///
        /// <para><b>Hers is the first unlocked Ultimate in this build that asks nothing and waits for
        /// nothing.</b> Wasp's, Arien's, Sabina's and Tigerclaw's all fire and stop the game; this one
        /// is simply true from the moment it is faceup. So the rail has to show it, and the proof that
        /// it is doing anything has to be read off an ordinary turn: her Gold basic prints one space
        /// of secondary Movement, and with one enemy minion standing beside her she walks two.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Metamorphosis_IsDrawnOnHerRailAndWalksHerFurtherThanHerCardPrints()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(
                runtime.Cards.Single(card =>
                        card.OwnerId.Equals(xargathaId) &&
                        card.DefinitionReference.DefinitionId.Equals(
                            XargathaCardContent.Metamorphosis))
                    .State.Location,
                Is.EqualTo(CardLocation.UltimateArea),
                "p.16 turns the Ultimate faceup in the hero's own area, which is where the staging " +
                "put it through the one transition the rules use.");

            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(xargathaId));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "Xargatha's rail must draw her unlocked Ultimate.");
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("Metamorphosis"));
            Assert.That(rail.UltimateCard.HistoryLabel, Is.EqualTo("UNLOCKED"));
            var drawn = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-ultimate-card" && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(drawn, Has.Length.EqualTo(2),
                "Both seats are staged at Tier III, so both rails show an unlocked Ultimate.");

            // What it is doing, read off an ordinary walk. Cleave prints one space of secondary
            // Movement; a card printing one space closes its own session with that step.
            var printedMovement = PrintedValue(
                runtime, XargathaCardContent.Cleave, PrintedValueChannel.Movement);
            Assert.That(printedMovement, Is.EqualTo(1));

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Cleave);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);
            var startedAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;
            var adjacentEnemies = runtime.BoardView.Model.Minions
                .Count(minion => minion.TeamSide != TeamSideOf(runtime, xargathaId) &&
                    HexDistanceQuery.Calculate(minion.Coordinate, startedAt) == 1);
            Assert.That(adjacentEnemies, Is.EqualTo(1),
                "One enemy minion stands beside her, so the Ultimate is worth +1 of each channel.");

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "The clicked Movement control must open p.10's session. " + Remaining(runtime));
            Assert.That(
                runtime.PendingMovementChoice.LegalDestinations.Any(value => value.Cost == 2),
                Is.True,
                "A card printing one space offers nowhere at cost 2; the Ultimate is what puts " +
                "those spaces in the session.");

            var first = FirstClickableSpace(pointerInput, runtime.PendingMovementChoice
                .LegalDestinations
                .Where(value => value.Cost == 1)
                .Select(value => value.Destination)
                .ToArray());
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(first).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                "and the session is still open after the step the card pays for, which is the " +
                "whole of what the permanent ability changes about this turn. " + Remaining(runtime));
            var second = FirstClickableSpace(pointerInput, runtime.PendingMovementChoice
                .LegalDestinations
                .Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, startedAt) == 2)
                .Select(value => value.Destination)
                .ToArray());
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, Tile(second).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate,
                Is.EqualTo(second));
            Assert.That(HexDistanceQuery.Calculate(second, startedAt), Is.EqualTo(2),
                "She is two spaces from where she started on a card that prints one.");
            Assert.That(HeroView(xargathaId).Coordinate, Is.EqualTo(second),
                "with her token drawn there rather than where she began.");

            yield return CaptureGameView("metamorphosis-on-her-rail-and-in-her-walk");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// Two setup turns on the headless surface: the enemy walks into contact, then attacks her,
        /// and she answers with the one face of hers that survives it. What this leaves behind is the
        /// discard the retrieve is about - the only way a card of hers reaches the pile in round one.
        /// </summary>
        private static IEnumerator TheEnemyWalksInAndAttacksHer(
            MatchSceneCompositionRoot root,
            GuardsDigital.Core.Units.Heroes.HeroId xargathaId,
            GuardsDigital.Core.Units.Heroes.HeroId enemyId)
        {
            var runtime = root.Runtime;
            var herSpace = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.Electrocute);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            var movement = runtime.AvailableTurnActions.First(candidate =>
                candidate.IsAvailable && KindOf(runtime, candidate.ActionDefinitionId) ==
                    ActionKind.Movement);
            Assert.That(runtime.SubmitAction(movement.ActionDefinitionId), Is.True,
                "The enemy's walk into contact is setup and must be available. " +
                Remaining(runtime));
            yield return null;
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, herSpace) == 1)
                .ToArray();
            Assert.That(step, Is.Not.Empty,
                "No one-space step of his stands beside her, so this staging cannot reach the " +
                "board her Defense needs.");
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseMovementDestination(
                runtime.PendingMovementChoice.ResolutionId,
                runtime.PendingMovementChoice.ChoiceId,
                step[0].Destination)), Is.True);
            yield return null;
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;
            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null);
            Assert.That(pending.LegalTargetHeroIds, Has.Member(xargathaId));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                pending.ResolutionId, pending.ChoiceId, xargathaId)), Is.True);
            yield return null;

            var survives = SurvivingDefense(runtime);
            Assert.That(survives, Is.Not.Null,
                "She must hold a face that answers this total, or the setup ends in a Defeat " +
                "instead of a discard. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitDefenseCard(survives.DefenseBreakdown.CardInstanceId.Value), Is.True);
            yield return null;
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>The standing pause's own confirm control, or null when it offers none.</summary>
        internal static TypedInvocationViewModel ConfirmControl(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);

        /// <summary>One printed value off a named face, read from the definition the match dealt.</summary>
        private static int PrintedValue(
            ConfiguredMatchSceneRuntime runtime, CardDefinitionId cardId, PrintedValueChannel channel)
        {
            var values = runtime.Definitions
                .Single(definition => definition.Id.Equals(cardId))
                .ActionDefinitions
                .SelectMany(action => action.BaseValues)
                .Where(value => value.Channel == channel)
                .ToArray();
            Assert.That(values, Has.Length.EqualTo(1),
                cardId.Value + " prints " + values.Length + " " + channel + " values.");
            return values[0].Amount;
        }

        internal static GuardsDigital.Core.Participants.TeamSide TeamSideOf(
            ConfiguredMatchSceneRuntime runtime, GuardsDigital.Core.Units.Heroes.HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(heroId)).TeamSide;

        /// <summary>
        /// One targeted Game View capture of what is on screen right now, for the non-batch pass the
        /// gate requires. A batch run skips it, so the same journeys serve both tiers.
        /// </summary>
        internal static IEnumerator CaptureGameView(string name)
        {
            if (Application.isBatchMode)
            {
                yield break;
            }

            var path = System.IO.Path.GetFullPath($"tmp/8-24-{name}.png");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            Assert.That(System.IO.File.Exists(path), Is.True,
                $"The targeted Game View evidence must be captured under tmp/ as {name}.png.");
        }
    }
}
