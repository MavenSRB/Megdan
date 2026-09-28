using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.DodgerUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.MisaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Whisper's Tier II and Tier III faces played the way a player plays them, from the engaged
    /// staging at rulebook p.15's later stages. Milestone 8.35z53.
    ///
    /// <para><b>WHICH BRANCH A JOURNEY NAMES IS CONTENT RATHER THAN CONVENIENCE.</b> Whisper's upgrade
    /// pairs are unusually clean - the two siblings of a colour are two DIFFERENT SENTENCES rather than
    /// the same sentence with a bigger number - so a fixture that let one branch stand for the other
    /// would leave half her printed set unreachable by any journey and say nothing about it.
    /// <see cref="WhisperProgressedHands"/> records the whole split: the FIRST branch is the seat that
    /// moves, the SECOND is the seat that hands decisions away and exchanges places.</para>
    ///
    /// <para><b>The single most important thing this fixture proves is that a decision can belong to
    /// somebody else.</b> Lesser Evil and Greater Good put p.10's modal to an enemy hero Whisper
    /// selected, through <see cref="GuardsDigital.Core.Effects.EffectChooserRole.BoundHero"/> - the
    /// node-level statement of a seam milestone 7.1a built at the handler level and nothing had used
    /// from a control node. Her phase-one worker traced the projection route through the code and
    /// recorded, in as many words, that it had never been exercised live. These journeys are where
    /// that stops being true: three of them assert the standing decision's own chooser, and one drives
    /// it through an Undo and a snapshot restore to prove the OWNERSHIP survives both and not just the
    /// pause.</para>
    ///
    /// <para><b>RULING 17 is proved from both sides, deliberately.</b> Milestone 8.35z35 split "cannot
    /// be performed" from "would apply nothing": the first is greyed with its reason, the second is
    /// offered, because on an opponent-chosen modal the bullet that does nothing to them is the escape
    /// hatch the decision exists to offer. Greater Good's journey proves the empty bullet is OFFERED
    /// and ungreyed; Lesser Evil's proves TAKING it resolves and the card finishes. Either alone would
    /// be half the ruling.</para>
    ///
    /// <para><b>Grim Reaper's raise means two different things on her two basic cards, and both are
    /// here.</b> On Death Seeker the bullets are two effects and both apply. On Swift Justice they are
    /// two TARGETING SENTENCES, so under RULING 19 "both" is two complete p.14 orders inside one
    /// action - each with its own targeting against the board as it then stands, its own Defense window
    /// and its own Attack Total.</para>
    /// </summary>
    public sealed class WhisperProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Crimson Trail: "Before the attack: You may move up to 1 space if there is an empty minion
        /// spawn point in radius in the battle zone. Target a unit adjacent to you."
        ///
        /// <para>The staged unit is two spaces from Whisper: nobody is adjacent at the origin, but a
        /// legal one-space answer to the satisfied conditional move leaves the unit adjacent. The
        /// primary therefore has a complete legal path and must be offered before the player chooses
        /// the destination. This is the exact shape hidden in the live packaged match.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CrimsonTrail_IsOfferedWhenItsConditionalStepCanReachAnAdjacentUnit()
        {
            yield return WhisperAuditStaging.WithEmptyBattleSpawn(
                SceneCardContent.VerifiedEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var own = WhisperSeat(runtime).HeroId;
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var victim = runtime.Projection.Minions.Single(minion =>
                minion.MinionId.Value == "audit-step-victim");
            yield return CommitEverySeat(
                root, own, NameOf(runtime, WhisperCardContent.CrimsonTrail));
            yield return YieldUntilTheNamedSeatActs(runtime, own);

            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            Assert.That(HexDistanceQuery.Calculate(origin, victim.Coordinate), Is.EqualTo(2),
                "The live defect requires no target at the origin and one after the printed step.");
            yield return PointTrayAt(root, own);
            Assert.That(runtime.RoundView.Model.Actions.Any(value =>
                    KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack),
                Is.True,
                "Crimson Trail's satisfied conditional move can leave Whisper beside the unit, so " +
                "the primary Attack must be offered before she chooses that destination. " +
                Remaining(runtime));

            yield return StartActionThroughControl(root, ActionKind.Attack);
            var destination = LitDestinations(runtime).First(space =>
                !space.Equals(origin) && HexDistanceQuery.Calculate(space, victim.Coordinate) == 1);
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(destination).transform.position),
                Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets.Select(value => value.Space),
                Does.Contain(victim.Coordinate));
            Assert.That(UiPlayer.ClickBoard(
                pointer, root.Hud, Tile(victim.Coordinate).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Projection.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(victim.MinionId));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")),
                Is.False);
        }

        /// <summary>
        /// Sanguine Path's counted form of the same printed move-then-target order.
        /// </summary>
        [UnityTest]
        public IEnumerator SanguinePath_BuysItsStepFromTheSpawnPointCountBeforeItNamesTheUnitBesideHer()
        {
            yield return WhisperAuditStaging.WithEmptyBattleSpawn(SceneCardContent.VerifiedTierTwoEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var own = WhisperSeat(runtime).HeroId;
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var victim = runtime.Projection.Minions.Single(minion => minion.MinionId.Value == "audit-step-victim");
            CommitSoTheNamedSeatActs(runtime, own, WhisperCardContent.SanguinePath);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            var steps = LitDestinations(runtime);
            Assert.That(steps, Is.Not.Empty, "The staged empty battle-zone spawn permits a positive move.");
            Assert.That(steps.All(space => HexDistanceQuery.Calculate(space, origin) <= 2), Is.True);
            var destination = steps.First(space => !space.Equals(origin) && HexDistanceQuery.Calculate(space, victim.Coordinate) == 1);
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(destination).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate, Is.EqualTo(destination));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets.All(value => HexDistanceQuery.Calculate(value.Space, destination) == 1), Is.True);
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(victim.Coordinate).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Projection.Minions.Any(minion => minion.MinionId.Equals(victim.MinionId)), Is.False);
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        [UnityTest]
        public IEnumerator SanguinePath_NoReachableTargetRemainsUnavailable()
        {
            yield return WhisperAuditStaging.WithEmptyBattleSpawn(SceneCardContent.VerifiedTierTwoEngagedStagedWhisper, hasReachableTarget: false);
            var runtime = Root().Runtime;
            var own = WhisperSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, WhisperCardContent.SanguinePath);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            Assert.That(runtime.RoundView.Model.Actions.Any(value => KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack), Is.False);
            Assert.That(runtime.RoundView.Model.UnavailableActions.Any(value => KindOf(runtime, value.ActionDefinitionId) == ActionKind.Attack), Is.True);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator SanguinePath_ZeroBudgetStaysThenTargets()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var own = WhisperSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, WhisperCardContent.SanguinePath);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate;
            Assert.That(LitDestinations(runtime), Is.Empty);
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets.Select(value => value.Space), Is.EqualTo(new[] { origin }));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(origin).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(own)).Coordinate, Is.EqualTo(origin));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets.All(value => HexDistanceQuery.Calculate(value.Space, origin) == 1), Is.True);
            // MILESTONE 8.86: the unit is NAMED and its fate read. This used to take the first
            // target and end on "nothing is pending", which says the questions were answered and
            // nothing at all about the Attack they were asked for.
            var target = runtime.RoundView.Model.EffectChoiceTargets.First();
            var attacked = runtime.BoardView.Model.Minions
                .SingleOrDefault(minion => minion.Coordinate.Equals(target.Space));
            Assert.That(attacked, Is.Not.Null,
                "\"Target a unit adjacent to you\" - the offered space has a minion standing on it. " +
                Remaining(runtime));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, Tile(target.Space).transform.position), Is.True);
            yield return null;
            yield return null;
            Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                Has.None.EqualTo(attacked.MinionId),
                "and p.13 takes the unit the Attack named off the board, which is the outcome this " +
                "journey drove and never read. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Lesser Evil: "An enemy hero in range chooses one — • That hero discards a card, if able. •
        /// You may defeat a minion adjacent to you. ( Any option can be chosen, even if it would have
        /// no effect )."
        ///
        /// <para><b>RULING 17's second half, and the escape hatch made real.</b> Whisper picks WHICH
        /// enemy hero; that hero picks the BULLET; and this journey has them pick the one that does
        /// nothing to them. Milestone 8.35z35 made that legal by splitting "cannot be performed" from
        /// "would apply nothing": greying the second deleted a legal answer from the board, and the
        /// designer's own reading of this card's parenthesis - it holds no rules text, and the card
        /// would function identically without it - is what says the split is the printed behaviour and
        /// not a new rule.</para>
        ///
        /// <para><b>The parenthesis prints on the LOWEST tier of the branch only</b>, per the same
        /// 2026-08-19 designer answer, so Greater Good behaves identically without carrying the
        /// words.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LesserEvil_PutsItsModalToTheEnemySeatWhoTakesTheBulletThatWouldDoNothing()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.LesserEvil);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Lesser Evil. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            // HER OWN QUESTION FIRST: "An enemy hero in range" is Whisper's selection at the card's
            // printed Range 3, and it is hers because the sentence names the hero it is about rather
            // than handing anybody a choice yet.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, whisperId), Is.True,
                "Whisper picks WHICH enemy hero; the modal beneath it is the only part she does not " +
                "own. " + Remaining(runtime));

            var heroes = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(heroes, Is.Not.Empty,
                "The enemy hero stands at two, inside the printed Range 3. " + Remaining(runtime));
            Assert.That(
                heroes.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 3), Is.True,
                "\"In range\" is p.9's disk at the printed three.");

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy hero is named by clicking the piece where they stand.");
            yield return null;
            yield return null;

            // THE MODAL, AND IT IS NOT HERS. This is the assertion the whole face exists for.
            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "\"chooses one\" opens once Whisper has named whom it is put to. " +
                Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "p.10 permits a non-actor chooser - \"You make all the choices in your card's text, " +
                "unless the card text instructs the other hero to make a choice\" - and this is the " +
                "first control node in the build to say so. The pending choice's chooser is what " +
                "SeatProjection.OwnerOfStandingDecision reads, so it is the field that decides which " +
                "seat is shown these bullets. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.Not.EqualTo(whisperId),
                "and a chooser that fell back to the actor would silently hand the opponent's choice " +
                "to the card's own controller - which is the defect this assertion exists to catch, " +
                "because from her side of the table it would look like a working prompt. " +
                Remaining(runtime));

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2),
                "Both printed bullets reach the enemy seat. " + Remaining(runtime));
            // MILESTONE 8.86: the arm this journey takes is NAMED. `bullets[1]` was a position in a
            // list, and a list that came back in the other order would have had this journey taking
            // the price while its own message said it was taking the escape hatch.
            Assert.That(UiPlayer.EffectOption(runtime, "modal-slaughter"), Is.Not.Null,
                "\"You may defeat a minion adjacent to you\" is the bullet that costs the enemy " +
                "nothing, and it is the one this journey has them take. " + Remaining(runtime));

            // RULING 17. Bullet 2 is "You may defeat a minion adjacent to you" - Whisper's own Defeat,
            // performed on her side of the board - so from the enemy's point of view it is the option
            // that costs them nothing, and where no minion stands beside her it applies literally
            // nothing at all. Either way it must be OFFERED rather than greyed.
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Is.Empty,
                "RULING 17: an option that can be TAKEN and would apply nothing is offered, because " +
                "on an opponent-chosen modal it is the escape hatch the decision exists to offer. " +
                "Only an option a mandatory step of whose branch cannot be taken is greyed, and " +
                "neither of these is that. Greyed: " + string.Join(", ",
                    runtime.RoundView.Model.UnavailableEffectChoices.Select(value => value.Label)) +
                ". " + Remaining(runtime));

            var handBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);

            // The enemy TAKES the harmless bullet. Their control, submitted through the projection's
            // own minted command - which is how a seat that is not the acting one answers.
            Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                "Arrange: \"chooses one\" is the exactly-one shape milestone 8.46b answers in one " +
                "press. " + Remaining(runtime));
            Assert.That(
                runtime.SubmitProjectedCommand(UiPlayer.EffectOption(runtime, "modal-slaughter").Command),
                Is.True,
                "The bullet that does nothing to them is a legal answer and the enemy seat may take " +
                "it. " + Remaining(runtime));
            yield return null;
            yield return null;

            // Milestone 8.35z53's first Play Mode run found the opposite of what this journey was
            // written blind to expect: SELECTING A BULLET WAS NOT TAKING IT, and the modal stood on
            // `Deselect`/`Confirm` until the electing seat pressed again.
            //
            // Milestone 8.46b settled it the other way, on the user's own rule for the decision
            // window: a choice admitting exactly one selection is answered in ONE press, closed by
            // the resolution session inside that same command. So the journey that was corrected to
            // press twice is corrected back to press once - and what it asserts is the stronger
            // half, that the electing seat is offered nothing further for a modal it has already
            // answered.
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm &&
                    value.Command.EffectChoice.ChoiceId.Equals(modal.ChoiceId)),
                Is.False,
                "The bullet was the whole answer, so no completion control stands behind it. " +
                Remaining(runtime));
            Assert.That(
                runtime.PendingEffectChoice == null ||
                    !runtime.PendingEffectChoice.ChoiceId.Equals(modal.ChoiceId),
                Is.True,
                "and the modal itself is answered rather than still standing half-filled. " +
                Remaining(runtime));

            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(handBefore),
                "The taken bullet is not the discard, so the enemy's hand is untouched - which is " +
                "the whole value of the escape hatch. " + Remaining(runtime));

            // Bullet 2's own `may` is WHISPER's again: having had the bullet chosen for her, she still
            // elects whether to defeat. The Tier III printing drops that "may", which is what makes
            // Greater Good's gate row carry no ConfirmOrDecline where this one does.
            if (runtime.PendingEffectChoice != null)
            {
                // Milestone 8.46e: the `may` is answered on the decision window, whose decline names
                // the consequence rather than the strip's `DECLINE`; its label is read off the model.
                var decline = UiPlayer.EffectControl(runtime, EffectChoiceCommandKind.Decline);
                Assert.That(decline, Is.Not.Null,
                    "\"You MAY defeat a minion\" is answerable both ways, and the election stayed " +
                    "hers even though the bullet was not. " + Remaining(runtime));
                Assert.That(StandingChoiceBelongsTo(runtime, whisperId), Is.True,
                    "and the decision came BACK to her, which is the half a projection could get " +
                    "wrong in the other direction. " + Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                yield return null;
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The card finishes on a bullet that applied nothing, rather than stranding the table " +
                "on an unanswerable prompt. " + Remaining(runtime));
        }

        /// <summary>
        /// Greater Good: "An enemy hero in range chooses one — • That hero discards a card, or is
        /// defeated. • You defeat up to 3 minions adjacent to you."
        ///
        /// <para><b>RULING 17's first half: the empty option is OFFERED, ungreyed.</b> Bullet 2 is
        /// Whisper's Defeat performed on her side of the board, and "up to 3" lets the count be zero -
        /// so it is an option the enemy CAN take that may apply literally nothing, which milestone
        /// 8.35z35 offers rather than greys. That is what this journey measures over the whole offer
        /// before choosing.</para>
        ///
        /// <para><b>Then it takes the PRICE instead</b>, which is the other reason this journey
        /// exists: bullet 1 is a mandatory step with two ways to complete - discard, or be defeated -
        /// the reading ledger question 13 settled on Arien's identical sentence. Lesser Evil prints "if
        /// able" and skips silently where the hero holds nothing; this one escalates. Taking it is what
        /// drives the hand-card question both opponent-chosen faces' gate rows rest on.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator GreaterGood_OffersItsEmptyDefeatBulletUngreyedAndTakesThePriceInstead()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.GreaterGood);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Greater Good. " + Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "Whisper names the enemy hero the modal is put to.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The Tier III printing hands the bullet to the enemy exactly as its Tier II sibling " +
                "does - the escalation changed what bullet 1 costs, not whose choice it is. " +
                Remaining(runtime));

            // Milestone 8.46e: the modal is asked on the decision window, so the whole offer is read
            // off its controls - a bullet drawn unavailable there carries an UnavailableReason and no
            // command - rather than off the strip, which no longer draws the family.
            var decision = runtime.DecisionWindowView.Model;
            Assert.That(decision, Is.Not.Null,
                "The modal is asked on the decision window. " + Remaining(runtime));
            var bullets = decision.Controls
                .Where(value => value.Command?.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            var greyed = decision.Controls.Where(value => value.UnavailableReason != null).ToArray();
            Assert.That(greyed, Is.Empty,
                "RULING 17's first half, asserted over the whole offer: \"You defeat up to 3 minions " +
                "adjacent to you\" may have no minion to defeat and a floor of nought, so it can " +
                "apply nothing - and an option that applies nothing is OFFERED. Before milestone " +
                "8.35z35 it was greyed, which deleted the enemy's escape hatch from the board. " +
                "Greyed: " + string.Join(", ", greyed.Select(value => value.Label)) +
                ". " + Remaining(runtime));

            var handBefore = runtime.Cards
                .Where(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.Id)
                .ToArray();
            Assert.That(handBefore, Is.Not.Empty,
                "The price bullet needs a hand to take a card out of; a hero holding nothing is " +
                "defeated instead, which is the escalation and not this arm. " + Remaining(runtime));

            // THE PRICE. Bullet 1 is mandatory with two ways to complete, and with cards in hand the
            // completion is the discard.
            var priceNode = StandingNode(runtime);
            // Milestone 8.46e: bullet 1 is taken on the window by its branch node id, not by the
            // strip's `MODE #1 MODAL-PRICE` label.
            UiPlayer.ClickEffectOption(runtime, hud, "modal-price");
            yield return null;
            yield return ConfirmIfStillStanding(root, priceNode);
            yield return null;

            // The hand-card question belongs to the hero paying it, which is the shape
            // `synthetic-discard-chosen-card@2` has carried since milestone 7.1a - the handler names
            // the chooser, where the modal above it needed a whole new role to say the same thing.
            CardInstanceId? paid = null;
            if (runtime.PendingEffectChoice != null)
            {
                Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                    "Which card is discarded is the discarding hero's own choice. " +
                    Remaining(runtime));
                var cardNode = StandingNode(runtime);
                var card = runtime.RoundView.Model.EffectChoices.First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
                // MILESTONE 8.86: the card is NAMED and read back, so "their hand fell by one" is
                // about the face that was clicked rather than about any card of theirs.
                paid = card.Command.EffectChoice.Candidate.Value.CardInstanceId;
                Assert.That(runtime.Cards.Single(value => value.Id.Equals(paid.Value)).OwnerId,
                    Is.EqualTo(enemyId),
                    "The offered face is the paying hero's own. " + Remaining(runtime));
                var face = UiPlayer.NamedElementWithText(hud, "gd-effect-card", card.Label);
                UiPlayer.EnsureVisible(face);
                yield return null;
                UiPlayer.Click(face);
                yield return null;
                yield return ConfirmIfStillStanding(root, cardNode);
                yield return null;
                yield return null;
            }

            var handAfter = runtime.Cards
                .Where(card => card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.Hand)
                .Select(card => card.Id)
                .ToArray();
            Assert.That(handAfter, Has.Length.EqualTo(handBefore.Length - 1),
                "The price was paid out of their hand. " + Remaining(runtime));
            Assert.That(
                runtime.Cards.Any(card =>
                    card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.True,
                "and the card it cost them is in their pile - which is the second of the two routes " +
                "that fill one at all, and the one Whisper can force. " + Remaining(runtime));
            if (paid.HasValue)
            {
                Assert.That(
                    runtime.Cards.Single(card => card.Id.Equals(paid.Value)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    "and it is THE CLICKED FACE that went there. " + Remaining(runtime));
            }
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemyId)), Is.True,
                "A hero who CAN discard is not defeated: \"or is defeated\" is the other way to " +
                "complete one mandatory step, not a second consequence. " + Remaining(runtime));
        }

        /// <summary>
        /// Cruel Twist: "Swap with a unit adjacent to you, or with an enemy hero in range with a card
        /// in the discard. You may then move up to 2 spaces."
        ///
        /// <para><b>The disjunction is ONE selection and not two.</b> The face offers one choice among
        /// a union of two differently reached sets, so composing it as a modal would have invented a
        /// question - "which kind of swap?" - the card never asks, and would have let one arm be greyed
        /// while the other still had subjects. That union is <c>choose-swap-subject@1</c>, and this
        /// journey asserts the union over the offer rather than over the piece clicked.</para>
        ///
        /// <para><b>The two reaches are different kinds of number and stay apart.</b> Adjacency is
        /// p.19's innermost ring, written into the sentence, which p.16's items never raise; the hero
        /// arm's reach is the card's printed Range icon, which a +1 Range item does. On a board where
        /// nobody has defended yet the hero arm is EMPTY - it wants an enemy hero with a card in the
        /// discard - so the offer is the adjacency arm alone, which is a legitimate union of a
        /// populated set and an empty one rather than a broken offer.</para>
        ///
        /// <para><b>"You may then move" is a separate step and its own decision.</b> "Then" puts it
        /// after the Swap, and "may ... up to 2" makes both whether and how far hers - measured from
        /// the space her subject was standing in, because p.19 makes a Swap not movement and nothing
        /// about the exchange spends this budget.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CruelTwist_SwapsWithAUnitDrawnFromTheUnionItsOneSelectionOffers()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.CruelTwist);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Cruel Twist. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "One selection, spanning both printed arms. " + Remaining(runtime));
            var subjects = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(subjects, Is.Not.Empty,
                "A friendly Melee minion and an enemy Ranged minion both stand adjacent to her at " +
                "this staging, and \"a unit adjacent to you\" names NEITHER team - so the adjacency " +
                "arm alone has subjects. " + Remaining(runtime));

            // THE UNION, ASSERTED AS A UNION. Every candidate is either adjacent (arm one) or within
            // the printed Range 4 (arm two); nothing outside both is offered, and no separate "which
            // arm?" control exists at all.
            Assert.That(
                subjects.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 4), Is.True,
                "Every candidate lies inside the wider of the two arms - adjacency is a subset of the " +
                "printed Range 4 disk here. Offered: " + string.Join(", ", subjects.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Count(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.LessThanOrEqualTo(subjects.Length),
                "and there is no MODE control beside the pieces - the card offers one choice among a " +
                "union, not a choice of arm followed by a choice of piece. " + Remaining(runtime));

            Assert.That(subjects.Contains(stoodAt), Is.False, "Whisper cannot swap with herself.");
            var subject = subjects[0];
            // MILESTONE 8.86: a Swap has TWO movers, and this journey only ever read one of them.
            // The piece standing on the chosen space is identified first so its own end of the
            // exchange can be asserted.
            var swappedMinion = runtime.BoardView.Model.Minions
                .SingleOrDefault(minion => minion.Coordinate.Equals(subject));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(subject).transform.position), Is.True,
                "The swap subject is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(subject),
                "The Swap put her in the space her subject was standing in. " + Remaining(runtime));
            if (swappedMinion != null)
            {
                Assert.That(
                    runtime.BoardView.Model.Minions
                        .Single(minion => minion.MinionId.Equals(swappedMinion.MinionId)).Coordinate,
                    Is.EqualTo(stoodAt),
                    "and THE OTHER HALF OF THE EXCHANGE, which this journey never read: the unit " +
                    "she swapped with is standing in the space she left. " + Remaining(runtime));
            }

            // "You may then move up to 2 spaces" - her own decision, from where the Swap left her.
            //
            // MILESTONE 8.86. The empty-offer arm used to DECLINE, and a printed "up to" proved by
            // declining it proves nothing about the move. She is standing in a space a unit occupied
            // a moment ago, so the board around her is playable and the step has somewhere to go;
            // an empty offer here is a finding rather than a branch.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You may then move\" is a separate printed step and its own question. " +
                Remaining(runtime));
            var steps = LitDestinations(runtime)
                .Where(space => !space.Equals(subject))
                .ToArray();
            Assert.That(steps, Is.Not.Empty,
                "\"You may then move up to 2 spaces\" - every space offered was the one the Swap " +
                "left her on, so taking the step would move her nowhere. " + Remaining(runtime));
            Assert.That(
                steps.All(space => HexDistanceQuery.Calculate(space, subject) <= 2), Is.True,
                "The budget is measured from where the Swap left her, which is the space the " +
                "exchange published rather than the board being asked again. Offered: " +
                string.Join(", ", steps.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, subject)}")));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(steps[0]).transform.position),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(steps[0]),
                "and the taken step really walked her off the space the Swap put her on. " +
                Remaining(runtime));

            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        /// <summary>
        /// The opponent-chosen modal, undone and then restored - Whisper's own session-operations
        /// journey, and the pause chosen for it rather than defaulted to.
        ///
        /// <para><b>Why this pause and no other.</b> Milestone 8.35z34 found that a node asking TWO
        /// questions was not restore-safe, because a restored pause re-enters through <c>Execute</c>
        /// rather than through <c>Resume</c>. Whisper's opponent-chosen modal asks ONE question, so it
        /// is not that shape - what it is instead is a pause whose OWNER is not the acting seat, and
        /// nothing in this build has ever driven an
        /// <see cref="GuardsDigital.Core.Effects.EffectChooserRole.BoundHero"/> chooser live at all.
        /// Her phase-one worker traced the routing through the code and said so in as many words.</para>
        ///
        /// <para><b>So what a restore has to bring back here is not only the pause but WHOSE it is.</b>
        /// A restore that re-opened the modal owned by Whisper would look exactly like a working
        /// restore from her side of the table, and the enemy would simply never be asked - which is why
        /// both halves of this journey assert the chooser and not merely the node.</para>
        ///
        /// <para>The Undo is driven first and the restore second, and the order is forced rather than
        /// chosen: a restore permanently disables history, so an Undo attempted after one is testing
        /// nothing. Milestone 8.35z34 measured that too.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheOpponentChosenModal_IsAnsweredAgainByTheEnemySeatAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.LesserEvil);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId), Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "Whisper names the enemy hero the modal is put to.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var node = runtime.PendingEffectChoice.NodeId;
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The modal opens owned by the enemy seat. " + Remaining(runtime));
            var commandsAtPause = runtime.AcceptedCommands.Count;

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(bullets[0].Command), Is.True,
                Remaining(runtime));
            yield return null;

            Assert.That(runtime.AcceptedCommands.Count, Is.GreaterThan(commandsAtPause),
                "The chosen bullet has to have been accepted for an Undo to have anything to drop. " +
                Remaining(runtime));

            // THE UNDO HALF: one command at a time, back to the pause the modal opened on.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            for (var press = 0; press < 4 && runtime.AcceptedCommands.Count > commandsAtPause; press++)
            {
                UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answer it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the modal is standing again, replayed rather than remembered. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                "on the same Choose node.");
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "AND IT IS STILL THE ENEMY'S. A replayed BoundHero modal has to re-resolve its bound " +
                "chooser through the ordinary input pipeline; one that came back owned by the actor " +
                "would have handed the opponent's decision to the card's own controller, silently. " +
                Remaining(runtime));
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Count(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.EqualTo(2),
                "with BOTH bullets offered again - an Undo that came back offering one would have " +
                "kept the selection it claimed to drop. " + Remaining(runtime));

            // THE RESTORE HALF, taken from the pause the Undo returned to.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            var again = runtime.RoundView.Model.EffectChoices
                .First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(runtime.SubmitProjectedCommand(again.Command), Is.True, Remaining(runtime));
            yield return null;
            yield return null;

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on - milestone 8.35i's " +
                "corrective holding for a choice paused inside an enclosing action. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "AND THE RESTORED PAUSE IS STILL THE ENEMY'S. This is the assertion the whole journey " +
                "was built around: a restored pause re-enters through Execute rather than Resume, so " +
                "the bound chooser is derived again from the selection node's published output - and " +
                "a restore that lost it would re-open a decision nobody could answer, or worse, one " +
                "the wrong player could. " + Remaining(runtime));
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Count(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select),
                Is.EqualTo(2),
                "and it is the MODAL that is standing with both bullets, rather than a child the " +
                "restore resumed into. " + Remaining(runtime));
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "A restore adopts the restored state with no accepted history, which is why the Undo " +
                "half had to run first.");
        }

        /// <summary>Six board clicks exercise the repeat bound, including retained-prefix
        /// Undo and a snapshot restore during the third instance. Milestone 9.1, ruling W2: "repeat
        /// UP TO five times" asks Whisper after each instance, and this journey keeps going.</summary>
        [UnityTest]
        public IEnumerator BloodFrenzy_PerformsFiveDistinctRepeatsAcrossUndoAndRestore()
        {
            yield return StageBloodFrenzyRing();
            var runtime = Root().Runtime;
            var whisperId = WhisperSeat(runtime).HeroId;
            yield return RunBloodFrenzy(whisperId, stopAfter: 6);
        }

        /// <summary>
        /// Milestone 9.1, ruling W2 (Rules/PRINTED_WORDING.md): "repeat up to five times" is the
        /// player's count. Whisper keeps going once and then stops, with legal targets still standing -
        /// two attacks, not six. Until 9.1 the chain was forced to run while any target remained.
        /// </summary>
        [UnityTest]
        public IEnumerator BloodFrenzy_StopsWhenWhisperDeclinesTheNextRepeat()
        {
            yield return StageBloodFrenzyRing();
            var runtime = Root().Runtime;
            var whisperId = WhisperSeat(runtime).HeroId;
            yield return RunBloodFrenzy(whisperId, stopAfter: 2);
        }

        private static IEnumerator StageBloodFrenzyRing()
        {
            const SceneCardContent content = SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWhisper;
            yield return LoadStagedScene(content);
            var initial = Root().Runtime;
            var whisperId = WhisperSeat(initial).HeroId;
            var enemyId = OtherHeroId(initial, whisperId);
            var enemySide = initial.Setup.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).TeamSide;
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable).Select(space => space.Coordinate));
            var center = playable.Where(space => space.GetNeighbors().All(playable.Contains))
                .OrderBy(space => HexDistanceQuery.Calculate(space, new HexCoordinate(0, 0))).First();
            var ring = center.GetNeighbors().ToArray();
            var enemyAt = playable.First(space => HexDistanceQuery.Calculate(center, space) == 2);
            var occupied = new HashSet<HexCoordinate>(ring.Concat(new[] { center, enemyAt }));
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(whisperId, center),
                    new SandboxHeroStaging(enemyId, enemyAt, hand: new[] { WaspCardContent.MagneticDagger })
                },
                removedMinions: initial.Setup.Minions.Where(minion => occupied.Contains(minion.Coordinate)).Select(minion => minion.MinionId),
                spawnedMinions: ring.Select((space, index) => new SandboxMinionStaging(
                    new MinionId("audit-frenzy-" + index), enemySide, StandardMinionType.Melee, space)));
            yield return LoadStagedScene(content);
        }

        private static IEnumerator RunBloodFrenzy(HeroId whisperId, int stopAfter)
        {
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var enemyId = OtherHeroId(runtime, whisperId);
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(runtime.Cards.Any(card => card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.DiscardPile), Is.True);
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.BloodFrenzy);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;
            var taken = new List<AttackTarget>();
            for (var index = 0; index < stopAfter; index++)
            {
                var pending = runtime.PendingAttackTargetChoice;
                Assert.That(pending, Is.Not.Null, "Expected attack " + (index + 1) + ". " + Remaining(runtime));
                Assert.That(pending.LegalTargets.Intersect(taken), Is.Empty);
                var target = pending.LegalTargets.First(value => value.Kind == AttackTargetKind.Minion &&
                    value.MinionId.Value.StartsWith("audit-frenzy-", System.StringComparison.Ordinal));
                var snapshot = runtime.CaptureSnapshot();
                Assert.That(UiPlayer.ClickBoard(pointer, hud, ViewOf(target).transform.position), Is.True);
                yield return null;
                yield return null;
                Assert.That(runtime.Projection.Minions.Any(minion => minion.MinionId.Equals(target.MinionId)), Is.False);
                if (index == 2)
                {
                    UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
                    yield return null;
                    UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
                    yield return null;
                    yield return null;
                    Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
                    UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR ▾"));
                    yield return null;
                    Assert.That(runtime.RestoreSnapshot(snapshot), Is.True);
                    yield return null;
                    Assert.That(runtime.PendingAttackTargetChoice.LegalTargets.Intersect(taken), Is.Empty);
                    Assert.That(UiPlayer.ClickBoard(pointer, hud, ViewOf(target).transform.position), Is.True);
                    yield return null;
                    yield return null;
                    Assert.That(runtime.Projection.Minions.Any(minion => minion.MinionId.Equals(target.MinionId)), Is.False);
                }
                taken.Add(target);

                // W2: after every instance but the fifth repeat, the chain is Whisper's to continue.
                if (index < 5)
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        "\"repeat up to five times\" asks after instance " + (index + 1) + ". " +
                        Remaining(runtime));
                    Assert.That(runtime.PendingEffectChoice.DeclineInvocation, Is.Not.Null);
                    UiPlayer.ClickEffectControl(runtime, hud, index + 1 < stopAfter
                        ? EffectChoiceCommandKind.Confirm
                        : EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                }
            }
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                stopAfter == 6 ? "There must be no seventh attack." : "Declining ends the chain.");
            Assert.That(taken.Distinct().Count(), Is.EqualTo(stopAfter));
            Assert.That(runtime.Projection.Minions.Count(minion =>
                    minion.MinionId.Value.StartsWith("audit-frenzy-", System.StringComparison.Ordinal)),
                Is.EqualTo(6 - stopAfter),
                "The ring's remaining minions are still standing, legal and untouched.");
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }

        /// <summary>
        /// Grim Reaper on Swift Justice: "When performing basic actions you may choose one, or both."
        ///
        /// <para><b>RULING 19, and the only place in the build where one action runs rulebook p.14
        /// twice.</b> The Ultimate raises the modal's ceiling from one to two, and Swift Justice's
        /// bullets are two TARGETING SENTENCES rather than two effects - so "both" is two COMPLETE p.14
        /// orders inside one action, each binding its own target against the board as it then stands,
        /// each with its own Defense window and its own Attack Total, with the whole modal being ONE
        /// action for every external purpose.</para>
        ///
        /// <para><b>Nothing constrains the two sequences to different units.</b> The face prints no
        /// "(on different targets)", so both bullets declare <c>distinctTargets:false</c> and may point
        /// at ONE unit provided it still satisfies each bullet's own sentence when that bullet
        /// resolves - the 2026-09-06 rider, in the user's own words: "if he does not move, it can be
        /// the SAME target as well". That is asserted here at the second sequence's own offer rather
        /// than assumed from the composition.</para>
        ///
        /// <para><b>The scoping is what makes the Ultimate narrow, and it is observable on this arm
        /// alone.</b> <c>RuleValueApplicability.ControllerBasicAction</c> reaches her two dealt BASIC
        /// cards and nothing else, because p.9 makes every action on a basic card a basic action - so
        /// the three Tier III upgrades this staging deals are untouched by it, and Lesser Evil and
        /// Greater Good would be untouched too even though they print modals.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator GrimReaper_RaisesSwiftJusticesCeilingSoBothBulletsRunTheirOwnFullSequence()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;

            var ultimate = runtime.Cards.Single(card =>
                card.OwnerId.Equals(whisperId) &&
                card.DefinitionReference.DefinitionId.Equals(WhisperCardContent.GrimReaper));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "p.16 puts the unlocked Ultimate faceup in the hero's own area, which is where a " +
                "player reads it. " + Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(ultimate.Id), Is.False,
                "p.16's permanent ability is an Ultimate RULE rather than an Active effect, so it " +
                "must not appear as one - that distinction is what keeps a permanent from being " +
                "cancelled at a turn boundary. " + Remaining(runtime));

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId), Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));

            // THE RAISED CEILING, and it is the whole observable difference from the printed card.
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(2),
                "Grim Reaper raises this modal's ceiling from the printed one to two. The definition " +
                "did not move: the same card offered ONE bullet at the engaged staging, which " +
                "SwiftJustice_OffersItsTwoBulletsAndTakesTheAdjacentUnitTheSecondOneNames asserts. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.MinimumSelections, Is.EqualTo(1),
                "and the floor is untouched - a modal Attack that chose nothing would be an Attack " +
                "that never targeted anything. " + Remaining(runtime));

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));

            UiPlayer.ClickEffectAnswer(runtime, hud, bullets[0].Command);
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "One selection of two does not close a modal whose ceiling is two - the second bullet " +
                "is still takeable, which is what \"or both\" means. " + Remaining(runtime));

            var remaining = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(remaining, Is.Not.Empty,
                "The unchosen bullet is still offered after the first is taken. " + Remaining(runtime));
            UiPlayer.ClickEffectAnswer(runtime, hud, remaining[0].Command);
            yield return null;
            yield return null;

            // TWO FULL p.14 ORDERS. The first sequence's own opening question is what comes next, and
            // the second's comes after the first has finished - each against the board as it then
            // stands.
            var sequences = 0;
            var targetsTaken = new List<string>();
            for (var order = 0; order < 2; order++)
            {
                var pending = runtime.PendingAttackTargetChoice;
                if (pending == null)
                {
                    break;
                }

                sequences++;
                var heroCount = pending.LegalTargetHeroIds.Count;
                var chosen = heroCount > 0
                    ? (Target: "hero:" + pending.LegalTargetHeroIds[0].Value,
                        Command: DirectorCommand.ChooseAttackTarget(
                            pending.ResolutionId, pending.ChoiceId, pending.LegalTargetHeroIds[0]))
                    : (Target: "minion:" + pending.LegalTargetMinionIds[0].Value,
                        Command: DirectorCommand.ChooseAttackTarget(
                            pending.ResolutionId, pending.ChoiceId, pending.LegalTargetMinionIds[0]));

                // RULING 19's RIDER, asserted where it is observable: the second sequence's offer is
                // NOT narrowed to exclude the first's target, because the face prints no "(on
                // different targets)". Whether the same unit is still a candidate depends on whether
                // it survived and still satisfies this bullet's own sentence - so the assertion is
                // about the ABSENCE of a distinctness filter rather than about a particular unit.
                if (order == 1 && targetsTaken.Count > 0 && heroCount > 0)
                {
                    Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                        "The second bullet binds its own target against the board as it then stands, " +
                        "and nothing about the first sequence's choice narrows it. Previously taken: " +
                        string.Join(", ", targetsTaken) + ". " + Remaining(runtime));
                }

                var minionTaken = heroCount > 0
                    ? (MinionId?)null
                    : pending.LegalTargetMinionIds[0];
                var heroTaken = heroCount > 0
                    ? (HeroId?)pending.LegalTargetHeroIds[0]
                    : null;
                Assert.That(runtime.SubmitProjectedCommand(chosen.Command), Is.True,
                    Remaining(runtime));
                targetsTaken.Add(chosen.Target);
                yield return null;

                for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
                {
                    runtime.DeclineDefense();
                    yield return null;
                    yield return null;
                }

                for (var budget = 0; budget < 6 && AStandingChoiceCanBeAnswered(runtime); budget++)
                {
                    AnswerStandingChoiceHeadlessly(runtime);
                    yield return null;
                    yield return null;
                }

                yield return null;

                // MILESTONE 8.86. "Both" is TWO COMPLETE p.14 ORDERS, and this journey counted the
                // orders without ever reading what either of them did. Each sequence's Defense was
                // declined above, which is what makes an Attack p.13's Defeat whatever the totals,
                // so the unit each order named is off the board - and that is the difference
                // between two sequences that RAN and two questions that were merely asked.
                if (minionTaken.HasValue)
                {
                    Assert.That(runtime.BoardView.Model.Minions.Select(minion => minion.MinionId),
                        Has.None.EqualTo(minionTaken.Value),
                        $"Sequence {order + 1} resolved against the minion it named. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(heroTaken.Value)),
                        Is.False,
                        $"Sequence {order + 1} resolved against the hero it named, and the declined " +
                        "Defense made it p.13's Defeat. " + Remaining(runtime));
                }
            }

            Assert.That(sequences, Is.EqualTo(2),
                "\"Both\" is TWO COMPLETE rulebook p.14 orders inside one action, each asking its own " +
                "opening question - which is the whole of what milestone 8.35z27's multi-sequence " +
                "contract is for and what this Ultimate is the first content to reach. Sequences " +
                "driven: " + sequences + ", targets: " + string.Join(", ", targetsTaken) + ". " +
                Remaining(runtime));
        }

        /// <summary>
        /// Grim Reaper on the OTHER basic card: "both" on Death Seeker is the plain reading, because
        /// its bullets are two effects rather than two targeting sentences.
        ///
        /// <para>The same Ultimate, the same raised number, and a different meaning - which is the
        /// point of scoping it by card class rather than by card. The immunity applies AND the retrieve
        /// is offered, so this is where the retrieve's own printed `may` and its hand-card question are
        /// driven in the same play as the bullet beside them.</para>
        ///
        /// <para>"You may retrieve" is the BULLET's optionality and stays inside the bullet rather than
        /// being read as the modal's: choosing that bullet and then declining is a different thing from
        /// never choosing it, and only the second is what an unchosen bullet means.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeathSeeker_AppliesBothOfItsBulletsOnceGrimReaperHasRaisedItsCeiling()
        {
            yield return WhisperAuditStaging.WithDiscards(SceneCardContent.VerifiedTierThreeEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var own = WhisperSeat(runtime).HeroId;
            CommitSoTheNamedSeatActs(runtime, own, WhisperCardContent.DeathSeeker);
            yield return YieldUntilTheNamedSeatActs(runtime, own);
            yield return PointTrayAt(root, own);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var node = StandingNode(runtime);
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(2));
            // Milestone 8.46e: both bullets are the decision window's controls, taken by their branch
            // node ids rather than the strip's `MODE #N` labels. A choose-two keeps its DONE, which
            // ConfirmIfStillStanding presses only if the second select has not already completed the
            // raised ceiling.
            Assert.That(UiPlayer.EffectOption(runtime, "seeker-immunity"), Is.Not.Null, Remaining(runtime));
            Assert.That(UiPlayer.EffectOption(runtime, "seeker-retrieve-may"), Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickEffectOption(runtime, root.Hud, "seeker-immunity");
            yield return null;
            UiPlayer.ClickEffectOption(runtime, root.Hud, "seeker-retrieve-may");
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
            var card = runtime.Cards.Single(value => value.OwnerId.Equals(own) && value.DefinitionReference.DefinitionId.Equals(WhisperCardContent.DeathSeeker));
            Assert.That(runtime.IsActiveEffectSource(card.Id), Is.True);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            // Milestone 8.46e: "you may retrieve" is accepted on the window's own control, named for
            // what accepting does rather than `ACCEPT`.
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            var handBefore = runtime.Cards.Count(value => value.OwnerId.Equals(own) && value.State.Location == CardLocation.Hand);
            node = StandingNode(runtime);
            var retrieve = runtime.RoundView.Model.EffectChoices.First(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            // MILESTONE 8.86: the retrieved card is NAMED, so the hand count below is about the face
            // that was clicked rather than about any card reaching her hand by any route.
            var retrieved = retrieve.Command.EffectChoice.Candidate.Value.CardInstanceId;
            Assert.That(runtime.Cards.Single(value => value.Id.Equals(retrieved)).State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "\"You may retrieve a discarded card\" - the face offered is standing in a pile. " +
                Remaining(runtime));
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", retrieve.Label);
            UiPlayer.EnsureVisible(face);
            yield return null;
            UiPlayer.Click(face);
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.Cards.Count(value => value.OwnerId.Equals(own) && value.State.Location == CardLocation.Hand), Is.EqualTo(handBefore + 1));
            Assert.That(runtime.Cards.Single(value => value.Id.Equals(retrieved)).State.Location,
                Is.EqualTo(CardLocation.Hand),
                "and it is THE CLICKED FACE that came back to her hand. " + Remaining(runtime));
            Assert.That(runtime.DiagnosticEvents.Any(entry => entry.EventType.Contains("EngineFault")), Is.False);
        }
    }
}
