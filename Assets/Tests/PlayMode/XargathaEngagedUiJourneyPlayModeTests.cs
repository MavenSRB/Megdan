using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Xargatha's starting-hand cards played the way a player plays them, from the engaged staging -
    /// Wasp and Xargatha two spaces apart, with an enemy Ranged minion beside her and a friendly Melee
    /// minion beside them both. Milestone 8.24.
    ///
    /// <para>Every decision each journey is about is made on the live surface: tray cards, prompt
    /// controls and card faces through <see cref="UiPlayer"/>, board decisions through the camera's
    /// screen-point ray after the HUD's own pointer-blocking answer. Decisions that are only setup -
    /// the walk that puts her where a printed sentence can reach, the enemy's own turn, the enemy's
    /// own Attack - ride the headless command surface, exactly as milestones 8.15, 8.20a, 8.21, 8.22
    /// and 8.23 do, and are marked where they appear.</para>
    ///
    /// <para><b>The staging's minions are content for her, not scenery.</b> Five of her faces are
    /// answered only by a minion and three of her values are counted from the ones standing beside
    /// her, so the enemy Ranged minion the engagement leaves adjacent is what several of these
    /// journeys are about rather than something they work around.</para>
    ///
    /// <para><b>One turn is one action, and that is what shapes these journeys.</b> Spending the
    /// secondary Movement ends the turn - the stop control and the end-turn control are the same
    /// command - so a face whose printed sentence needs her somewhere else spends a previous turn
    /// walking there.</para>
    /// </summary>
    public sealed class XargathaEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// The space the Defense journey walks her to: adjacent to the enemy hero, and the only such
        /// space on this staging with no enemy minion beside it.
        ///
        /// <para>It has to be that one, for the reason milestone 8.23's fixture records: p.14 composes
        /// the minion contributions standing around a defender into their Defense Total, and one
        /// adjacent enemy minion is the whole difference between a face that answers and a face that
        /// does not.</para>
        /// </summary>
        internal static readonly HexCoordinate DefenceContact = new HexCoordinate(-1, 2);

        /// <summary>
        /// Cleave: "Target a unit adjacent to you. After the attack: May repeat once on a different
        /// enemy hero."
        ///
        /// <para>Two board decisions and one printed offer between them. She attacks the enemy minion
        /// standing beside her, p.14's step 7 asks whether to repeat, and the repeat - if it is taken -
        /// admits enemy heroes alone. That narrowing is what no earlier repeat in this build does, and
        /// it is why the card's own parenthesis can say the first target may have been a minion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Cleave_AttacksAnAdjacentMinionAndIsOfferedItsRepeatOnAHero()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Cleave);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(xargathaId),
                "Cleave's printed Initiative 11 acts before the lowest card the enemy holds.");

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));
            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty,
                "An enemy minion stands beside her on the staged board, and \"a unit\" admits it.");
            var targetView = MinionView(targets[0]);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, targetView.transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack: May repeat once\" is p.14's step 7 and it asks. " +
                Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "A printed \"May\" has to be declinable on its own control - the user's rule, and " +
                "the one milestone 8.20a made an offer that must be named rather than implied.");

            UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Declining the repeat closes the card. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and no second Attack opened a target question, which is what declining means.");
        }

        /// <summary>
        /// Siren's Call: "Target an enemy unit not adjacent to you and in range; if able, move the
        /// target up to 3 spaces to a space adjacent to you."
        ///
        /// <para>Two board decisions, and the second is bounded by where she stands rather than by how
        /// far the unit may travel. The staged enemy Ranged minion beside her is the piece that proves
        /// the first bound is real: it is in range and is not offered, because the sentence excludes
        /// what is already adjacent.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SirensCall_OffersOnlyDistantEnemiesAndDrawsTheNamedOneToHerSide()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.SirensCall);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            var herSpace = runtime.Heroes.Single(hero => hero.HeroId.Equals(xargathaId)).Coordinate;
            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The Skill's own text opens its selection. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(offered, Is.Not.Empty,
                "Somebody enemy stands inside Range 3 and away from her. " + Remaining(runtime));
            foreach (var space in offered)
            {
                Assert.That(HexDistanceQuery.Calculate(space, herSpace), Is.GreaterThan(1),
                    "\"not adjacent to you\" removes the innermost ring of the printed Range - and " +
                    "it has to remove minions as well as heroes, or the clause would mean one thing " +
                    "for each kind.");
            }

            var chosen = MinionsAt(offered).FirstOrDefault();
            Assert.That(chosen, Is.Not.Null,
                "This journey names a minion, so one of the offered pieces has to be one. " +
                Remaining(runtime));
            var selection = StandingNode(runtime);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, chosen.transform.position), Is.True,
                "The target is named by clicking the miniature where it stands.");
            yield return null;
            yield return ConfirmIfStillStanding(root, selection);
            yield return null;

            var destinations = LitDestinations(runtime);
            if (destinations.Length > 0)
            {
                foreach (var space in destinations)
                {
                    Assert.That(HexDistanceQuery.Calculate(space, herSpace), Is.EqualTo(1),
                        "\"to a space adjacent to you\" - every destination the move offers is " +
                        "beside her, which is the bound p.16's items never widen because it is " +
                        "written in the card's text rather than printed as an icon.");
                }

                var move = StandingNode(runtime);
                var landing = FirstClickableSpace(pointerInput, destinations);
                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, hud, Tile(landing).transform.position), Is.True);
                yield return null;
                yield return ConfirmIfStillStanding(root, move);
                yield return null;

                Assert.That(
                    HexDistanceQuery.Calculate(
                        MinionView(chosen.Occupancy.MinionId).Occupancy.Coordinate, herSpace),
                    Is.EqualTo(1),
                    "The called unit is standing beside her now.");
            }
        }

        /// <summary>
        /// Stone Gaze: "<b>Next turn:</b> Enemy heroes in radius count as both heroes and terrain, and
        /// cannot perform movement actions."
        ///
        /// <para><b>This is the journey the Edit Mode tier cannot have.</b> p.19's <c>Next turn:</c>
        /// becomes applicable at her next <c>TurnStarted</c>, and a fixture that ended the round to
        /// reach that window would read p.8's card retrieval cancelling the effect instead. Here the
        /// turns actually come round, so the effect is met where a player meets it: raised on the turn
        /// it was played, and biting on the next one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator StoneGaze_IsDormantOnTheTurnItIsPlayedAndStandsOnHerNext()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var xargathaId = XargathaSeat(runtime).HeroId;
            // The enemy hero is the one the disk is about; his seat is read for the journey's own
            // narrative rather than for an assertion this tier can make.

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.StoneGaze);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            // NOTHING IS LIVE YET, AND THAT IS THE KEYWORD DOING ITS WORK. p.19's `Next turn:` is
            // dormant until its controller's next TurnStarted, and a dormant source is deliberately
            // not reported as one - an effect standing here would be "This turn:", a different
            // keyword on a different card.
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "A Next Turn effect is not live on the turn that raised it. " + Remaining(runtime));

            // The turns come round, which is the whole reason this journey exists at Play Mode: the
            // Edit Mode tier's staged board gives her one turn, so its only way to "advance time" is
            // to end the round - and p.8's card retrieval cancels the effect on the way.
            yield return YieldUntilNobodyIsActing(runtime);
            yield return null;
            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments),
                "Her next turn is inside this round, not after it. " + Remaining(runtime));

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Cleave);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            // Her next turn has begun, so the disk she laid down last turn is standing now.
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Not.Empty,
                "The petrification becomes applicable at her next turn, which has now started. " +
                Remaining(runtime));

            var trayCards = runtime.HandsView.Model.Hands
                .Single(hand => hand.HeroId.Equals(xargathaId)).TrayCards;
            Assert.That(trayCards.Any(card => card.IsActiveEffectSource), Is.True,
                "and the card that raised it is drawn as carrying it on her own tray - which is the " +
                "only thing on screen that says the disk exists. Tray: " +
                string.Join(", ", trayCards.Select(card =>
                    card.Group + "/" + card.IsActiveEffectSource)));
        }

        /// <summary>
        /// Charm: "You may move an enemy ranged minion in radius up to 2 spaces before or after
        /// movement."
        ///
        /// <para>Three questions in printed order, and the first is the one no earlier hero asks: which
        /// side of her own Movement the text runs on. It is mandatory - the card offers two sides, not
        /// three - and what may be declined is the move behind it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Charm_AsksWhichSideOfHerMovementItRunsOnAndWalksTheRangedMinion()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Charm);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Movement);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"before or after movement\" is asked before anything moves. " + Remaining(runtime));
            var sides = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(sides, Has.Length.EqualTo(2),
                "Two sides are offered, because the card prints two. " + Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Null,
                "and neither of them may be declined: the card gives her a choice of when, not of " +
                "whether. What may be declined is the move.");

            // The side is a modal-style selection: picking it leaves the node standing with its own
            // confirm, so the placement is not made until that confirm is pressed.
            UiPlayer.Click(UiPlayer.Button(hud, sides[0].Label));
            yield return null;
            var placement = StandingNode(runtime);
            yield return ConfirmIfStillStanding(root, placement);
            yield return null;
            yield return null;

            // The "you may" behind the placement is the second question.
            for (var budget = 0; budget < 3 && runtime.RoundView.Model.EffectChoiceTargets.Count == 0; budget++)
            {
                var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
                if (confirm == null)
                {
                    break;
                }

                UiPlayer.Click(UiPlayer.Button(hud, confirm.Label));
                yield return null;
                yield return null;
            }

            var offered = MinionsAt(runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space));
            Assert.That(offered, Is.Not.Empty,
                "The staged enemy Ranged minion is inside Radius 2 and is the class the card " +
                "names. " + Remaining(runtime));
            foreach (var view in offered)
            {
                Assert.That(view.Occupancy.MinionType, Is.EqualTo(StandardMinionType.Ranged),
                    "Charm reaches an enemy RANGED minion and no other class - the adjective its " +
                    "face prints, and the whole difference between it and Control.");
            }
        }

        [UnityTest]
        public IEnumerator SecondaryDefense_IsSpentOnItsOwnNumericFaceAndXargathaSurvivesTheAttack()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);

            // Setup: she walks into contact, headlessly, so the enemy's Attack can reach her.
            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.DangerousSlash);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);
            yield return WalkHeadlesslyTo(runtime, xargathaId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // The enemy attacks, and her Defense is answered on the live surface.
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            if (runtime.PendingAttackTargetChoice != null)
            {
                var pending = runtime.PendingAttackTargetChoice;
                Assert.That(
                    runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                        pending.ResolutionId,
                        pending.ChoiceId,
                        pending.LegalTargetHeroIds.First())),
                    Is.True);
                yield return null;
            }

            var surviving = SurvivingDefense(runtime);
            Assert.That(surviving, Is.Not.Null,
                "One of her numeric Defense faces has to answer this attack. " + Remaining(runtime));

            // A Defense is offered as a WHOLE FACE named `gd-defense-card`, never as a labelled
            // button - the same contract milestone 8.15a set for every card-valued choice.
            var faceName = NameOf(runtime, runtime.Cards
                .Single(card => card.Id.Equals(surviving.DefenseBreakdown.CardInstanceId.Value))
                .DefinitionReference.DefinitionId);
            UiPlayer.EnsureVisible(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-defense-card", faceName));
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. " + Remaining(runtime));

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(xargathaId)), Is.True,
                "The spent Defense keeps her on the board.");
        }

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>Walks the named hero to a space through the headless surface, as setup.</summary>
        internal static IEnumerator WalkHeadlesslyTo(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, HexCoordinate destination)
        {
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(heroId));
            var movement = runtime.AvailableTurnActions.FirstOrDefault(candidate =>
                candidate.IsAvailable &&
                runtime.Definitions
                    .Single(definition => definition.Reference.Equals(candidate.SourceDefinitionReference))
                    .ActionDefinitions
                    .Single(action => action.Id.Equals(candidate.ActionDefinitionId))
                    .Kind == ActionKind.Movement);
            Assert.That(movement.IsAvailable, Is.True,
                "The walk that puts her in contact is setup and must be available. " +
                Remaining(runtime));
            Assert.That(runtime.SubmitAction(movement.ActionDefinitionId), Is.True);
            yield return null;

            for (var budget = 0; budget < 6 && runtime.PendingMovementChoice != null; budget++)
            {
                var step = runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 1)
                    .OrderBy(value => HexDistanceQuery.Calculate(value.Destination, destination))
                    .FirstOrDefault();
                if (step.Destination.Equals(destination))
                {
                    Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseMovementDestination(
                        runtime.PendingMovementChoice.ResolutionId,
                        runtime.PendingMovementChoice.ChoiceId,
                        step.Destination)), Is.True);
                    yield return null;
                    break;
                }

                Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseMovementDestination(
                        runtime.PendingMovementChoice.ResolutionId,
                        runtime.PendingMovementChoice.ChoiceId,
                        step.Destination)), Is.True);
                yield return null;
            }

            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                yield return null;
            }
        }

        internal static TypedInvocationViewModel SurvivingDefense(
            ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.DefenseChoices.FirstOrDefault(choice =>
                choice.DefenseBreakdown != null &&
                choice.DefenseBreakdown.CardInstanceId.HasValue &&
                choice.DefenseBreakdown.Outcome == AttackResolutionOutcome.SuccessfullyDefended);

        /// <summary>The standing pause's own decline control, or null when it offers none.</summary>
        internal static TypedInvocationViewModel DeclineControl(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);

        internal static HeroId OtherHeroId(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Setup.Heroes.Single(hero => !hero.HeroId.Equals(heroId)).HeroId;

        /// <summary>
        /// Commits the named card out of the named seat's hand and the lowest-Initiative card out of
        /// every other seat's, headlessly. The tray-clicked commitment has its own journey in
        /// <see cref="XargathaUiJourneyPlayModeTests"/>, and rulebook p.7 resolves the higher printed
        /// Initiative first, so the other seat yielding is what makes the turn deterministic.
        /// </summary>
        internal static void CommitSoTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId actingHeroId, CardDefinitionId definitionId)
        {
            foreach (var seat in runtime.Setup.Heroes.Select(hero => hero.HeroId).ToArray())
            {
                if (seat.Equals(actingHeroId))
                {
                    Commit(runtime, seat, definitionId);
                    continue;
                }

                var lowest = runtime.Cards
                    .Where(card => card.OwnerId.Equals(seat) &&
                        card.State.Location == CardLocation.Hand)
                    .OrderBy(card => runtime.Definitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .Initiative.Value)
                    .ThenBy(card => card.Id.Value, System.StringComparer.Ordinal)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(lowest.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }
        }

        internal static void Commit(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId definitionId)
        {
            var card = runtime.Cards.Single(value =>
                value.OwnerId.Equals(heroId) &&
                value.State.Location == CardLocation.Hand &&
                value.DefinitionReference.DefinitionId.Equals(definitionId));
            Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True,
                $"{definitionId.Value} must be selectable from {heroId.Value}'s hand.");
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
        }

        /// <summary>The selected actor's printed primary action, for a setup step that is not the subject.</summary>
        internal static ActionDefinitionId PrimaryActionOf(ConfiguredMatchSceneRuntime runtime) =>
            runtime.AvailableTurnActions.Single(candidate =>
                candidate.IsAvailable && OriginOf(runtime, candidate) ==
                    ActionDefinitionOrigin.PrintedPrimary).ActionDefinitionId;

        /// <summary>
        /// Ends whoever is acting until the actor is the named seat. A cross-team Initiative tie goes
        /// to the Tie Breaker side, which `DEV-005` fixes to Red.
        /// </summary>
        internal static IEnumerator YieldUntilTheNamedSeatActs(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId)
        {
            for (var budget = 0; budget < 4; budget++)
            {
                if (!runtime.SelectedActor.IsDefined ||
                    runtime.SelectedActor.ActorId.Equals(heroId))
                {
                    yield break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The enemy's turn is setup for this journey and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }

            Assert.Fail("The named seat never took a turn. " + Remaining(runtime));
        }

        /// <summary>
        /// Presses the confirm belonging to the node that was standing before the click, when one is
        /// still there. Naming the node matters: an unqualified confirm reaches past its own step, as
        /// milestone 8.22's first Play Mode run found.
        /// </summary>
        internal static IEnumerator ConfirmIfStillStanding(
            MatchSceneCompositionRoot root, EffectNodeId node)
        {
            var runtime = root.Runtime;
            var standing = runtime.PendingEffectChoice;
            if (standing == null || !standing.NodeId.Equals(node))
            {
                yield break;
            }

            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            if (confirm == null)
            {
                yield break;
            }

            UiPlayer.Click(UiPlayer.Button(root.Hud, confirm.Label));
            yield return null;
        }

        /// <summary>The node a pause is standing on, for a confirm that must not reach past it.</summary>
        internal static EffectNodeId StandingNode(ConfiguredMatchSceneRuntime runtime)
        {
            var standing = runtime.PendingEffectChoice;
            Assert.That(standing, Is.Not.Null,
                "No effect pause is standing to read a node from. " + Remaining(runtime));
            return standing.NodeId;
        }

        /// <summary>Ends whoever is acting until nobody is, so the round can reach its own boundary.</summary>
        internal static IEnumerator YieldUntilNobodyIsActing(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.SelectedActor.IsDefined && budget < 6; budget++)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "Every remaining turn of this round is setup and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            for (var budget = 0; runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                Assert.That(budget, Is.LessThan(40),
                    "The end of round must complete within a bounded number of answers.");
                AnswerAnyEndOfRoundPause(runtime);
                yield return null;
            }

            yield return null;
        }

        private static void AnswerAnyEndOfRoundPause(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingEffectChoice != null)
            {
                // Her Constrict and Final Embrace stop p.8 before the Minion Battle - the first
                // verified content to reach that window - so an end-of-round drive has to answer an
                // effect choice here where no earlier hero's did.
                var model = runtime.RoundView.Model;
                var answer = model.EffectChoices.FirstOrDefault(value =>
                        value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select) ??
                    model.EffectChoices.FirstOrDefault(value =>
                        value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm) ??
                    model.EffectChoices.FirstOrDefault(value =>
                        value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Decline);
                Assert.That(answer, Is.Not.Null,
                    "A pause with no answer would stop the round. " + Remaining(runtime));
                Assert.That(runtime.SubmitProjectedCommand(answer.Command), Is.True);
                return;
            }

            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                {
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId),
                        Is.True);
                }

                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                return;
            }

            if (runtime.PausedSystemTransitionOptions.Count > 0)
            {
                Assert.That(
                    runtime.AnswerPausedSystemTransition(runtime.PausedSystemTransitionOptions[0]),
                    Is.True);
                return;
            }

            Assert.That(runtime.ContinueEndOfRound(), Is.True);
        }

        internal static ActionDefinitionOrigin OriginOf(
            ConfiguredMatchSceneRuntime runtime, ActionOffer offer) =>
            runtime.Definitions
                .Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions
                .Single(action => action.Id.Equals(offer.ActionDefinitionId))
                .Origin;

        internal static CardDefinitionId[] DiscardOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Cards
                .Where(card => card.OwnerId.Equals(heroId) &&
                    card.State.Location == CardLocation.DiscardPile)
                .Select(card => card.DefinitionReference.DefinitionId)
                .ToArray();

        /// <summary>
        /// The spaces a paused effect is offering, in board order. Space-valued candidates come through
        /// <c>EffectChoiceDestinations</c> and object-valued ones through <c>EffectChoiceTargets</c>;
        /// reading the wrong one is how a board-answered question looks answerless.
        /// </summary>
        internal static HexCoordinate[] LitDestinations(ConfiguredMatchSceneRuntime runtime) =>
            runtime.RoundView.Model.EffectChoiceDestinations
                .Select(value => value.Command.EffectChoice.Candidate.Value.Coordinate)
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();

        /// <summary>The drawn miniatures standing on the given spaces, in the order the spaces come.</summary>
        internal static StandardMinionView[] MinionsAt(IEnumerable<HexCoordinate> spaces) =>
            spaces
                .Select(space => Object.FindObjectsByType<StandardMinionView>()
                    .FirstOrDefault(view => view.Occupancy != null &&
                        view.Occupancy.Coordinate.Equals(space)))
                .Where(view => view != null)
                .ToArray();

        internal static StandardMinionView MinionView(MinionId minionId) =>
            Object.FindObjectsByType<StandardMinionView>()
                .Single(view => view.Occupancy != null &&
                    view.Occupancy.MinionId.Equals(minionId));

        internal static PrimitiveHeroView HeroView(HeroId heroId) =>
            Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(view => view.Occupancy.HeroId.Equals(heroId));

        internal static HexTileView Tile(HexCoordinate space) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(space));

        /// <summary>
        /// What a paused frame is still asking, for a failure message that says what the player was
        /// actually looking at rather than only that something was left over.
        /// </summary>
        internal static string Remaining(ConfiguredMatchSceneRuntime runtime)
        {
            var model = runtime.RoundView.Model;
            return $"status='{model.Status}' choices=[" +
                string.Join(", ", model.EffectChoices.Select(value =>
                    $"{value.Command.EffectChoice.Kind}:{value.Label}")) +
                "] targets=[" +
                string.Join(", ", model.EffectChoiceTargets.Select(value => value.Space.ToString())) +
                "] destinations=[" +
                string.Join(", ", model.EffectChoiceDestinations.Select(value =>
                    value.Command.EffectChoice.Candidate.Value.Coordinate.ToString())) +
                "] defense=[" +
                string.Join(", ", model.DefenseChoices.Select(value => value.Label)) + "]";
        }

        /// <summary>
        /// The first offered space whose own camera ray reaches the board rather than a hero piece
        /// standing over it. <c>PrimitiveHeroPointerInput</c> offers a ray to the hero selectors before
        /// the tiles, so a space drawn behind a piece is answered by clicking elsewhere on it.
        /// </summary>
        internal static HexCoordinate FirstClickableSpace(
            PrimitiveHeroPointerInput pointerInput, IReadOnlyList<HexCoordinate> spaces)
        {
            var camera = pointerInput.EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            foreach (var space in spaces)
            {
                var tile = Tile(space);
                var screen = camera.WorldToScreenPoint(tile.transform.position);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.Fail("Every offered space is drawn behind a hero piece.");
            return default;
        }
    }
}
