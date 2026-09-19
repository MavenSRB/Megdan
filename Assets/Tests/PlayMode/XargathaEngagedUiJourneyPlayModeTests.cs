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
        /// The one square on this staging that is adjacent to the staged hero AND to an enemy minion,
        /// so a repeat narrowed to enemy heroes has somewhere to go once the printed instance has
        /// taken the minion. Milestone 8.35z15.
        ///
        /// <para><b>Why a journey has to put somebody here, and why that is the milestone's own
        /// finding.</b> <see cref="EngagedMirrorStaging"/> stages the two heroes exactly two spaces
        /// apart - it searches for the pair and skips any candidate whose distance is not 2, because
        /// the gap is what makes a Radius 2 disk and a bent ranged line meaningful. Cleave's repeat
        /// admits a different enemy <em>hero</em> and Cleave is melee, so on the untouched staging her
        /// repeat has no legal subject at all and rulebook p.10 does not offer it. Before milestone
        /// 8.35z15 it was offered anyway, and this journey answered that offer by declining it - which
        /// is how a prompt that could never produce an instance survived three journeys whose names
        /// said they drove a repeat.</para>
        /// </summary>
        internal static readonly HexCoordinate BesideHerAndTheMinion = new HexCoordinate(-2, 1);

        /// <summary>
        /// Cleave: "Target a unit adjacent to you. After the attack: May repeat once on a different
        /// enemy hero."
        ///
        /// <para>Two board decisions and one printed offer between them. She attacks the enemy minion
        /// standing beside her, p.14's step 7 asks whether to repeat, and the repeat admits enemy
        /// heroes alone. That narrowing is what no earlier repeat in this build does, and it is why the
        /// card's own parenthesis can say the first target may have been a minion.</para>
        ///
        /// <para><b>The enemy walks into reach first, and that is the whole setup.</b> Wasp's Magnetic
        /// Dagger prints Initiative 12 against Cleave's 11, so the enemy seat takes its turn first and
        /// spends its secondary Movement stepping onto
        /// <see cref="BesideHerAndTheMinion"/> - the only square adjacent to both her and the staged
        /// enemy Ranged minion. Nothing is placed by hand: it is a position the rules produce, the same
        /// shape milestone 8.29e used when it let Wasp's Dagger clear a square before Arien acted.</para>
        ///
        /// <para><b>This journey takes the repeat rather than declining it.</b> Milestone 8.35z15's own
        /// record: until then, every journey naming a repeat only ever pressed its decline, so no live
        /// coverage had once driven a second instance through the real controls.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator Cleave_AttacksAnAdjacentMinionAndIsOfferedItsRepeatOnAHero()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, enemyId, WaspCardContent.MagneticDagger);
            Commit(runtime, xargathaId, XargathaCardContent.Cleave);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(runtime, enemyId, BesideHerAndTheMinion);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);

            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "The clicked Attack control must open p.14's target question. " + Remaining(runtime));
            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty,
                "An enemy minion stands beside her on the staged board, and \"a unit\" admits it.");
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Has.Member(enemyId),
                "and the enemy hero who just stepped in is admitted too, which is what gives the " +
                "repeat somewhere to go. " + Remaining(runtime));
            var targetView = MinionView(targets[0]);
            Assert.That(UiPlayer.ClickBoard(pointerInput, hud, targetView.transform.position), Is.True,
                "The target is chosen by clicking the miniature where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"After the attack: May repeat once\" is p.14's step 7 and it asks. " +
                Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "A printed \"May\" has to be declinable on its own control - the user's rule, and " +
                "the one milestone 8.20a made an offer that must be named rather than implied.");

            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "and it has to be takeable on its own control too. " + Remaining(runtime));
            UiPlayer.ClickEffectAnswer(runtime, hud, confirm.Command);
            yield return null;
            yield return null;

            var repeat = runtime.PendingAttackTargetChoice;
            Assert.That(repeat, Is.Not.Null,
                "Confirming starts the second instance, which opens its own target question. " +
                Remaining(runtime));
            Assert.That(repeat.LegalTargetHeroIds, Is.EqualTo(new[] { enemyId }),
                "\"on a different enemy hero\": the one enemy hero in reach is the whole set.");
            Assert.That(repeat.LegalTargetMinionIds, Is.Empty,
                "and the minion still standing beside her is not in it, though the printed instance " +
                "admitted it - the narrowing milestone 8.24 derived and 8.35z15 now judges the offer by.");

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The repeated instance's target is chosen by clicking the hero where they stand.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "A hero target opens p.14's Defense question for the repeated instance exactly as it " +
                "does for the printed one - the page is the same page. " + Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.DeclineDefense()), Is.True,
                "The defender's answer is aftermath here, so it is given headlessly.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The second instance reaches its own step 7 and p.10 forbids repeating a repeat, so " +
                "nothing is asked again. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and no third instance of one played Cleave opened a target question.");
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
            var calledFrom = chosen.Occupancy.Coordinate;
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
                    Assert.That(HexDistanceQuery.Calculate(calledFrom, space),
                        Is.InRange(1, 3),
                        "'up to 3 spaces' admits only positive paths inside the printed budget.");
                }

                var shorter = destinations.Where(space =>
                    HexDistanceQuery.Calculate(calledFrom, space) < 3).ToArray();
                Assert.That(shorter, Is.Not.Empty,
                    "The live board exposes at least one legal shorter path; requiring exactly three " +
                    "spaces was the demonstrated player-facing defect.");
                var move = StandingNode(runtime);
                var landing = FirstClickableSpace(pointerInput, shorter);
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
            UiPlayer.ClickEffectAnswer(runtime, hud, sides[0].Command);
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
        public IEnumerator SirensCall_IsOfferedForItsPrintedDefenseThreeAndXargathaSurvivesTheAttack()
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

            // The enemy's Initiative 12 Attack beats Cleave's 11 while Siren's Call stays in hand
            // and can therefore answer the reaction window on the live surface.
            Commit(runtime, enemyId, WaspCardContent.MagneticDagger);
            Commit(runtime, xargathaId, XargathaCardContent.Cleave);
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

            var sirensCallInstance = runtime.Cards.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(XargathaCardContent.SirensCall));
            var surviving = runtime.RoundView.Model.DefenseChoices.SingleOrDefault(choice =>
                choice.DefenseBreakdown != null &&
                choice.DefenseBreakdown.CardInstanceId.HasValue &&
                choice.DefenseBreakdown.CardInstanceId.Value.Equals(sirensCallInstance.Id));
            Assert.That(surviving, Is.Not.Null,
                "Siren's Call has to be offered for its printed Defense 3. " + Remaining(runtime));
            var defenseOffer = runtime.PendingDefenseChoice.LegalDefenseOffers.Single(offer =>
                offer.SourceCardInstanceId.Equals(sirensCallInstance.Id));
            Assert.That(
                PrintedValueQuery.CalculatePrintedValue(
                    defenseOffer, runtime.Definitions, PrintedValueChannel.Defense).Total,
                Is.EqualTo(3),
                "The response must use the shield value, not the primary's Range 3.");

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

        /// <summary>
        /// The same printed repeat, taken back and given again: an Undo across the answered decision,
        /// then a snapshot restored onto it, then the replay both of those rest on.
        ///
        /// <para>It is Cleave's repeat rather than any earlier question of hers because a repeat is
        /// the one pause a player is asked <em>after</em> the thing it is about has already happened -
        /// p.14's step 7, with an Attack resolved between the target click and this control - so a
        /// re-derivation that came back with the wrong instance has something to be wrong about. The
        /// Undo half is proved on the live session before the capture, because a restore adopts a
        /// state rather than a history and clears the retained prefix behind it.</para>
        ///
        /// <para><b>The enemy steps into reach first for the reason milestone 8.35z15 records</b> on
        /// <see cref="BesideHerAndTheMinion"/>: the engaged staging holds the two heroes two spaces
        /// apart by construction, so without that step Cleave's repeat has no legal subject and is
        /// correctly not offered - and there would be no standing decision for this journey to undo
        /// and restore.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CleavesRepeat_IsAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Commit(runtime, enemyId, WaspCardContent.MagneticDagger);
            Commit(runtime, xargathaId, XargathaCardContent.Cleave);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(runtime, enemyId, BesideHerAndTheMinion);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);
            yield return PointTrayAt(root, xargathaId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            var targets = runtime.PendingAttackTargetChoice.LegalTargetMinionIds;
            Assert.That(targets, Is.Not.Empty);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(targets[0]).transform.position),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The journey needs the repeat standing. " + Remaining(runtime));
            var node = runtime.PendingEffectChoice.NodeId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Answered, then taken back. The answer is an ordinary recorded command, so the retained
            // prefix replays every command before it - the commitment, the Attack, the target - and
            // stops on the same question.
            UiPlayer.ClickEffectAnswer(runtime, hud, DeclineControl(runtime).Command);
            yield return null;
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause + 1),
                "One click on the repeat's control is one accepted command.");
            Assert.That(runtime.PendingEffectChoice, Is.Null);

            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the repeat is standing again, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                "about the same printed decision.");
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "and offering the refusal it offered before, which is derived from the node's own " +
                "floor rather than stored beside it.");

            // A snapshot taken on the stopped decision and restored onto the same session.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);
            UiPlayer.ClickEffectAnswer(runtime, hud, DeclineControl(runtime).Command);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The declined repeat is gone and the card is finished.");

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the same decision it was captured on. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));

            // and it is answerable on the same visible surface, by clicking the same real control.
            var restored = DeclineControl(runtime);
            Assert.That(restored, Is.Not.Null,
                "and it offers what the live pause offered, because nothing about the options is " +
                "persisted. " + Remaining(runtime));
            UiPlayer.ClickEffectAnswer(runtime, hud, restored.Command);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The click a player made after the restore answered the restored decision.");
        }

        /// <summary>
        /// p.13's Defeat and respawn, with the piece as its subject: the token leaves the board when
        /// the answer is a decline, and comes back on a clicked spawn space at her own next turn. It
        /// is also the half of the spawn claim the engaged staging leaves open - the space she returns
        /// to is the map's own Blue hero spawn area, not the space the staging moved her to.
        /// </summary>
        [UnityTest]
        public IEnumerator Xargatha_IsDefeatedByAnAttackAndRespawnsOnHerOwnSpawnByAClick()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedXargatha);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var xargathaId = XargathaSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, xargathaId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // Setup, headlessly: she walks into the one contact space with no enemy minion beside it,
            // and the enemy's Attack follows on the next turn.
            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.DangerousSlash);
            yield return YieldUntilTheNamedSeatActs(runtime, xargathaId);
            yield return WalkHeadlesslyTo(runtime, xargathaId, DefenceContact);
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True,
                "The enemy's Attack is setup for this journey. " + Remaining(runtime));
            yield return null;

            var pending = runtime.PendingAttackTargetChoice;
            Assert.That(pending, Is.Not.Null,
                "The enemy's Attack must ask which unit it takes. " + Remaining(runtime));
            Assert.That(pending.LegalTargetHeroIds, Has.Member(xargathaId),
                "She is standing beside him, so she is a legal target of it.");
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    pending.ResolutionId, pending.ChoiceId, xargathaId)),
                Is.True);
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var lifeBefore = hud.Header.BlueLifeLabel;
            Assert.That(runtime.DeclineDefense(), Is.True,
                "Declining is what makes this Attack a Defeat; spending a face is " +
                "SecondaryDefense_IsSpentOnItsOwnNumericFaceAndXargathaSurvivesTheAttack's subject.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(xargathaId)), Is.False,
                "The defeated hero is off the board until she respawns.");
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>()
                    .Any(view => view.Occupancy.HeroId.Equals(xargathaId)), Is.False,
                "and her token is gone with her rather than left standing.");
            Assert.That(hud.Header.BlueLifeLabel, Is.Not.EqualTo(lifeBefore),
                "The defeat spends a Blue Life, and the status strip says so.");

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // p.13 offers the respawn at the defeated hero's own next turn, so the card she commits
            // for it has to take that turn: Cleave's printed 11 is above every card the enemy still
            // holds, and a lower one would leave the offer standing behind theirs.
            CommitSoTheNamedSeatActs(runtime, xargathaId, XargathaCardContent.Cleave);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(xargathaId),
                "The respawn is offered at her own turn, so her card has to open it. " +
                Remaining(runtime));
            yield return PointTrayAt(root, enemyId);
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Not.Empty,
                "The defeated Xargatha must be offered her respawn at her own turn. Status: " +
                runtime.RoundView.Model.Status);
            var respawn = runtime.RoundView.Model.HeroRespawnOptions.First();
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(respawn.Destination).transform.position),
                Is.True,
                "The respawn space is answered on the board.");
            yield return null;

            Assert.That(runtime.RoundView.Model.HeroRespawnOptions, Is.Empty,
                "The answered respawn withdraws its options.");
            var respawned = Object.FindObjectsByType<PrimitiveHeroView>()
                .Where(view => view.Occupancy.HeroId.Equals(xargathaId))
                .ToArray();
            Assert.That(respawned, Has.Length.EqualTo(1),
                "Exactly one Xargatha token stands after the respawn, not a second one beside a " +
                "stale.");
            Assert.That(respawned[0].Coordinate, Is.EqualTo(respawn.Destination));
            Assert.That(respawned[0].HasTokenVisual, Is.True,
                "and it is her own official token that came back.");
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
            // Milestone 8.35g. The commitment phase this drives does not open while a finished turn
            // is still held for its own player, so the release goes above every read below.
            UiPlayer.EndAnyTurnInProgress(runtime);

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
            // Milestone 8.35g. A finished turn is held for its own player, so the commitment phase
            // this reads into does not open until that player hands it on. The release goes above
            // the read, exactly where the player presses it.
            UiPlayer.EndAnyTurnInProgress(runtime);

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
            // Milestone 8.35g. A standing release is not "nobody is acting" - it is the previous
            // player still holding their finished turn with the selection already emptied - so it
            // is pressed like any other turn ending rather than mistaken for the exit. The budget
            // doubles because every turn now costs two presses of the same control.
            for (var budget = 0; budget < 8; budget++)
            {
                if (runtime.PendingTurnRelease == null &&
                    (!runtime.SelectedActor.IsDefined ||
                        runtime.SelectedActor.ActorId.Equals(heroId)))
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

            UiPlayer.ClickEffectAnswer(runtime, root.Hud, confirm.Command);
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
            // Milestone 8.35g. "Nobody is acting" is no longer "no actor is selected". A turn whose
            // resolution has finished holds the round with an EMPTY selection until its own player
            // releases it, so stopping at the empty selection would leave the round standing in a
            // pause and report it as finished - which is exactly what the round-boundary assertion
            // below would then read as a phase that never turned over.
            // Milestone 8.35z53. A TURN HOLDING AN OPEN DECISION IS NOT ENDABLE, and until Whisper was
            // dealt no setup arc could leave one standing here. Her Gold basic's "After the attack:"
            // rider asks the hero who DEFENDED for a space (`defended-card-forced-move@1`), so an arc
            // that drives an Attack-and-Defence exchange to fill a discard pile - which four of her
            // journeys must, because no staging can deal a pile - reaches this loop with a destination
            // question open and `SubmitEndTurn` correctly refusing. Answering what stands before
            // insisting the turn is endable restores this helper's own stated contract rather than
            // widening it: for every hero dealt before her nothing stands, the predicate is false on
            // the first look, and the loop behaves exactly as it did.
            for (var budget = 0;
                (runtime.SelectedActor.IsDefined || runtime.PendingTurnRelease != null) && budget < 24;
                budget++)
            {
                if (MisaUiJourneyPlayModeTests.AStandingChoiceCanBeAnswered(runtime))
                {
                    Assert.That(
                        MisaUiJourneyPlayModeTests.AnswerStandingChoiceHeadlessly(runtime), Is.True,
                        "A decision that says it can be answered must answer. " + Remaining(runtime));
                    yield return null;
                    continue;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "Every remaining turn of this round is setup and must be endable. " +
                    Remaining(runtime));
                yield return null;
            }
        }

        /// <summary>Drains whatever end-of-round pauses stand until the next turn can be committed.</summary>
        internal static IEnumerator AdvanceToNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            // Milestone 8.35g. Every turn now costs TWO presses of the same control - the Hold or
            // stop that finishes the resolution, then the release that hands the turn on - so a
            // journey step that pressed once per turn arrives here with the round still resolving
            // and the commitment phase it wants never opening. This drives the rest of the round
            // the way a player does, pressing whatever END TURN currently means, and stops the
            // moment that control is no longer offered, so a decision this filler cannot answer is
            // left standing for the journey rather than swallowed by it.
            for (var budget = 0; budget < 12; budget++)
            {
                if (runtime.Phase != RoundResolutionPhase.ResolvingTurn ||
                    runtime.RoundView.Model.EndTurnInvocation == null)
                {
                    break;
                }

                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "A turn ending this filler was offered must be takeable.");
                yield return null;
            }

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
