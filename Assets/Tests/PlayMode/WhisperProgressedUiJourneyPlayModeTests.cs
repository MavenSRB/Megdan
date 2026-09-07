using System.Collections;
using System.Collections.Generic;
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
        /// Sanguine Path: "Before the attack: You may move up to 1 space for every empty minion spawn
        /// point in radius in the battle zone up to a maximum of 2 spaces. Target a unit adjacent to
        /// you."
        ///
        /// <para><b>The printed order decides the target, and that is the whole seam.</b> The move
        /// comes first, so "a unit adjacent to you" is judged from the space the move left her in -
        /// milestone 8.19a's shape, which 8.35z25 corrected two Tigerclaw faces onto and the
        /// 2026-09-06 printed-reading-order ruling settled. The Attack therefore declares its target
        /// bound by its own card text, and p.14's opening question, asked before any text runs, does
        /// not ask it.</para>
        ///
        /// <para><b>The board supplies the budget and the card supplies its ceiling.</b> Both halves
        /// are one sentence and yield ONE number: the count capability publishes the lesser of the two
        /// and the move binds its distance from it, rather than the move being given a printed number
        /// and a clamp applied somewhere else. So the offered step is bounded by two at this Tier and
        /// by whatever the board actually holds, and this journey asserts the smaller of them over the
        /// whole offer.</para>
        ///
        /// <para><b>This journey stands in for CRIMSON TRAIL as well, and the reason is named rather
        /// than hidden.</b> Crimson Trail prints the identical seam with a boolean gate instead of a
        /// count, and its Attack is GREYED ON EVERY BOARD IN THIS BUILD -
        /// <c>WalkToAttackTargetBinding</c>'s Conditional arm returns <c>Blocked</c>, which is a
        /// pre-existing engine defect unrelated to this hero and corrected separately. Sanguine Path
        /// and Blood Pilgrimage are unaffected because their Move is a direct Sequence child under
        /// <c>EffectFailurePolicy.UpTo</c> rather than a Conditional's, so the seam all three faces'
        /// gate rows describe is driven here.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS, and it is a genuine fork this dealing does not " +
            "settle. She is at (-3,1) and exactly one candidate is offered, at distance 0 - so the " +
            "printed step did NOT move her before the targeting sentence ran. WHAT IS ELIMINATED: the " +
            "ordering is composed correctly and is proved at the definition tier, and Crimson Trail's own " +
            "Attack now starts at all (milestone 8.35z49's Conditional fix), so this is not the binding " +
            "defect that shape used to carry. WHAT STANDS are two readings this board cannot separate: " +
            "either the empty-minion-spawn-point condition is FALSE on the staged board, in which case " +
            "the step correctly skipped and the assertion is written against a board that never promised " +
            "one - the reading ledger row 21 makes ordinary - or the step is offered and the candidates " +
            "are derived from where she started. Distinguishing them needs a staging with a KNOWN empty " +
            "Spawn point in radius, which is a fixture this milestone did not build.")]
        public IEnumerator SanguinePath_BuysItsStepFromTheSpawnPointCountBeforeItNamesTheUnitBesideHer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SanguinePath);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Sanguine Path. " + Remaining(runtime));

            var stoodAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            // THE STEP COMES FIRST AND p.14's OWN QUESTION DOES NOT. That pairing is the seam: a card
            // whose text names its target has nothing for the page's opening question to ask.
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "The target is bound by this card's own text, so p.14's opening question is not " +
                "asked - the step-1 body runs first. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Before the attack: You may move ...\" is what opens. " + Remaining(runtime));

            var steps = LitDestinations(runtime);
            if (steps.Length > 0)
            {
                Assert.That(
                    steps.All(space => HexDistanceQuery.Calculate(space, stoodAt) <= 2), Is.True,
                    "The budget is the LESSER of the board's empty minion Spawn points in radius and " +
                    "the card's printed ceiling of two, published as one number. Offered: " +
                    string.Join(", ", steps.Select(space =>
                        $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, stoodAt)}")));

                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, hud, Tile(steps[0]).transform.position),
                    Is.True,
                    "The step is answered on the board.");
                yield return null;
                yield return null;
            }
            else
            {
                // A board with no qualifying Spawn point yields a budget of zero, and an up-to move of
                // zero spaces moves nowhere without needing a condition to say so - which is exactly
                // why this face prints no "if there is" where Crimson Trail does. A legitimate outcome
                // rather than a failure, and the journey says which one it met.
                var decline = DeclineControl(runtime);
                if (decline != null)
                {
                    UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                    yield return null;
                    yield return null;
                }
            }

            // THE TARGETING SENTENCE, judged from wherever the step left her.
            var nowAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Target a unit adjacent to you\" is the card's own second sentence and its own " +
                "question. " + Remaining(runtime));
            var victims = runtime.RoundView.Model.EffectChoiceTargets
                .Select(candidate => candidate.Space)
                .ToArray();
            Assert.That(victims, Is.Not.Empty,
                "An enemy Ranged minion stands adjacent to her at this staging, so the mandatory " +
                "targeting step has a subject. " + Remaining(runtime));
            Assert.That(
                victims.All(space => HexDistanceQuery.Calculate(space, nowAt) == 1), Is.True,
                "and every candidate is adjacent to WHERE SHE NOW STANDS rather than to where she " +
                "started, which is the whole reason the move is printed first. She is at " +
                $"({nowAt.Q},{nowAt.R}); offered: " + string.Join(", ", victims.Select(space =>
                    $"({space.Q},{space.R})@{HexDistanceQuery.Calculate(space, nowAt)}")));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(victims[0]).transform.position), Is.True,
                "The target is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            for (var budget = 0; budget < 6 && runtime.PendingDefenseChoice != null; budget++)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }
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
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
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
            Assert.That(runtime.SubmitProjectedCommand(bullets[1].Command), Is.True,
                "The bullet that does nothing to them is a legal answer and the enemy seat may take " +
                "it. " + Remaining(runtime));
            yield return null;
            yield return null;

            // Milestone 8.35z53's first Play Mode run: SELECTING A BULLET IS NOT TAKING IT. The modal
            // stands on `Deselect`/`Confirm` until the electing seat commits, which is the ordinary
            // shape of every `Choose` in this build and not something the opponent-chooser changes -
            // this journey was written blind and expected the selection to resolve on the Select
            // alone. The enemy seat confirms through its own projected command, exactly as it
            // selected.
            var confirm = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            Assert.That(confirm, Is.Not.Null,
                "The electing seat closes its own selection. " + Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(confirm.Command), Is.True,
                "and the confirm is theirs to submit, on the same projection the select came " +
                "through. " + Remaining(runtime));
            yield return null;
            yield return null;

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
                var decline = DeclineControl(runtime);
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
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: FIXTURE SEQUENCING. It counts the enemy hand " +
            "expecting 3 and measures 4 while the surface still stands on \"YOUR CARD CHOICE - Choose 1 of " +
            "4 cards for Wasp to discard\" - so the count is taken BEFORE the price is actually paid, and " +
            "the four cards it sees are the four it is being asked to choose among. WHAT IS ELIMINATED: " +
            "the offer itself, which is the journey's real subject and which passed - the empty Defeat " +
            "bullet is offered ungreyed, RULING 17's escape hatch, asserted before this line. WHAT STANDS " +
            "is one missing step: answer the discard choice, then count. It is left parked rather than " +
            "patched blind because the same arc feeds two later assertions whose expected numbers move " +
            "with it.")]
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

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            Assert.That(runtime.RoundView.Model.UnavailableEffectChoices, Is.Empty,
                "RULING 17's first half, asserted over the whole offer: \"You defeat up to 3 minions " +
                "adjacent to you\" may have no minion to defeat and a floor of nought, so it can " +
                "apply nothing - and an option that applies nothing is OFFERED. Before milestone " +
                "8.35z35 it was greyed, which deleted the enemy's escape hatch from the board. " +
                "Greyed: " + string.Join(", ",
                    runtime.RoundView.Model.UnavailableEffectChoices.Select(value => value.Label)) +
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
            Assert.That(runtime.SubmitProjectedCommand(bullets[0].Command), Is.True,
                "The enemy seat takes the price. " + Remaining(runtime));
            yield return null;
            yield return null;

            // The hand-card question belongs to the hero paying it, which is the shape
            // `synthetic-discard-chosen-card@2` has carried since milestone 7.1a - the handler names
            // the chooser, where the modal above it needed a whole new role to say the same thing.
            if (runtime.PendingEffectChoice != null)
            {
                Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                    "Which card is discarded is the discarding hero's own choice. " +
                    Remaining(runtime));
                Assert.That(AnswerStandingChoiceHeadlessly(runtime), Is.True,
                    "The enemy seat answers with one of their own faces. " + Remaining(runtime));
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
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: the swap itself completes and what fails is the " +
            "step AFTER it. The surface stands at \"YOUR TURN - RELEASE TO CONTINUE\" with no pending " +
            "choice where this expects the printed \"You may then move\" to be its own question. WHAT IS " +
            "ELIMINATED: choose-swap-subject@1 is registered, reachable and exercised - its showcase " +
            "consumer and her gate row both pass, and the selection resolved here before this assertion. " +
            "WHAT STANDS is whether the optional move opens as a separate question at all on this board, " +
            "or whether it was already answered inside the same resolution: distinguishing them is a " +
            "content-and-feasibility reading of her Blue pair rather than a fixture edit, so it is left " +
            "NAMED rather than settled inside a dealing.")]
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

            var subject = subjects[0];
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(subject).transform.position), Is.True,
                "The swap subject is chosen by clicking the piece where it stands.");
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(whisperId)).Coordinate,
                Is.EqualTo(subject),
                "The Swap put her in the space her subject was standing in. " + Remaining(runtime));

            // "You may then move up to 2 spaces" - her own decision, from where the Swap left her.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You may then move\" is a separate printed step and its own question. " +
                Remaining(runtime));
            var steps = LitDestinations(runtime);
            if (steps.Length > 0)
            {
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
            }
            else
            {
                var decline = DeclineControl(runtime);
                Assert.That(decline, Is.Not.Null,
                    "A printed `you may` whose offer is empty must still be closable. " +
                    Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(hud, decline.Label));
                yield return null;
                yield return null;
            }

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

        /// <summary>
        /// Blood Frenzy: "Target a unit adjacent to you. After the attack: If an enemy hero in radius
        /// has a card in the discard, repeat up to five times on different targets."
        ///
        /// <para><b>THIS JOURNEY PROVES A SHORTFALL RATHER THAN THE PRINTED RULE, deliberately, and
        /// asserts what the engine actually does today.</b> RULING 34 reads "on different targets" as
        /// PAIRWISE distinct - no unit attacked twice anywhere in the chain.
        /// <c>CardActionResolutionAuthority</c>'s repeat exclusion is a SINGLE <c>AttackTarget</c>,
        /// reset at each instance, so it excludes only the unit the instance IMMEDIATELY BEFORE took.
        /// At Blood Fury's printed maximum of one the two readings coincide exactly and that card is
        /// correct; at Blood Frenzy's five they diverge, and the third instance may re-take the first's
        /// target.</para>
        ///
        /// <para><b>It is recorded and not fixed here, because the fix is schema-bumping.</b> Closing
        /// it means an accumulating exclusion SET where a single target is held, and that state is
        /// persisted (<c>OwedAttackRepeatExclusion</c> is in the restore state), so it is not a
        /// dealing's work. This journey is the same instrument Whisper's phase-one worker used for
        /// Sabina's analogous gap - a named test that pins the measurement so the fix has something to
        /// turn red.</para>
        ///
        /// <para><b>The condition is re-checked before each instance and that is the other half.</b>
        /// p.10 makes each repeat a separate instance of the action, so the card text runs again inside
        /// it and asks the board again - which is why a hero who discards TO DEFEND one instance arms
        /// the instances after it. Nothing arranges that; it falls out of the condition being a live
        /// query inside a body that runs per instance.</para>
        ///
        /// <para><b>The chain is bounded by the board before it is bounded by the card, and the journey
        /// says which bound it met.</b> "A unit adjacent to you" is measured from her space every
        /// instance, so the number of reachable instances is a property of this staging rather than of
        /// the printed five - and running out of targets is the ordinary end of a chain rather than a
        /// stop.</para>
        /// </summary>
        [UnityTest]
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: FIXTURE INSTRUMENTATION, and its own author " +
            "predicted the shape. It dies in CommitSoTheNamedSeatActs for the same reason the immunity " +
            "journey does - the arc must manufacture a mid-chain defense discard to arm later repeat " +
            "instances (RULING 34's only mechanism), and a card spent to arm the chain is a card a later " +
            "commit still names. WHAT IS ELIMINATED: the turn hand-off, fixed for every Whisper journey " +
            "at this milestone. WHAT STANDS is the arc's card bookkeeping, plus the prep's own recorded " +
            "uncertainty that the engaged staging may not seat enough neighbours to reach a THIRD " +
            "instance, which is the only position from which the single-slot divergence is observable at " +
            "all. The shortfall itself is pinned at the definition tier instead, which is what the " +
            "journey's own name says it records.")]
        public IEnumerator BloodFrenzy_ExcludesOnlyTheInstanceBeforeIt_RecordingTheSingleSlotShortfall()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);

            // SETUP, HEADLESSLY: the gate reads an ENEMY HERO's discard pile, which fills only when
            // they defend. So the enemy attacks her first and she answers - no, the other way round:
            // her own pile is irrelevant here, and the enemy's is what the condition reads. She attacks
            // them with her Gold basic and they spend a face.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            for (var budget = 0; budget < 3 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            var opening = runtime.PendingAttackTargetChoice;
            if (opening != null && opening.LegalTargetHeroIds.Contains(enemyId))
            {
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    opening.ResolutionId, opening.ChoiceId, enemyId));
                yield return null;

                var answer = SurvivingDefense(runtime);
                if (answer != null)
                {
                    runtime.SubmitProjectedCommand(answer.Command);
                    yield return null;
                    yield return null;
                }
            }

            for (var budget = 0; budget < 4 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.Cards.Any(card =>
                    card.OwnerId.Equals(enemyId) &&
                    card.State.Location == CardLocation.DiscardPile),
                Is.True,
                "The chain's gate reads an ENEMY hero's pile, and a spent Defense is what fills one. " +
                Remaining(runtime));

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // HER TURN, ON THE LIVE SURFACE.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.BloodFrenzy);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId),
                "The journey needs her turn open on Blood Frenzy. " + Remaining(runtime));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            // BLOOD FRENZY TAKES p.14's ORDINARY QUESTION and narrows nothing - it prints its
            // targeting sentence FIRST and its rider at step 7, so "a unit" is the page's own word and
            // the card declares no binding.
            var first = runtime.PendingAttackTargetChoice;
            Assert.That(first, Is.Not.Null,
                "The two Red repeaters take p.14's own opening question, unlike the three step-1 " +
                "movers. " + Remaining(runtime));

            var takenInOrder = new List<string>();
            var offeredAtEachInstance = new List<string[]>();

            for (var instance = 0; instance < 5; instance++)
            {
                var pending = runtime.PendingAttackTargetChoice;
                if (pending == null)
                {
                    break;
                }

                var offered = pending.LegalTargetHeroIds.Select(id => "hero:" + id.Value)
                    .Concat(pending.LegalTargetMinionIds.Select(id => "minion:" + id.Value))
                    .OrderBy(id => id, System.StringComparer.Ordinal)
                    .ToArray();
                offeredAtEachInstance.Add(offered);
                if (offered.Length == 0)
                {
                    break;
                }

                // THE EXCLUSION, OBSERVED AT THE OFFER. Instance N is not offered instance N-1's
                // target, and that single fact is the whole of what the engine enforces.
                if (takenInOrder.Count > 0)
                {
                    Assert.That(offered, Does.Not.Contain(takenInOrder[takenInOrder.Count - 1]),
                        "\"On different targets\" excludes the unit the PREVIOUS instance took, which " +
                        "the engine does enforce. Instance " + (instance + 1) + " was offered: " +
                        string.Join(", ", offered) + "; the previous instance took " +
                        takenInOrder[takenInOrder.Count - 1] + ". " + Remaining(runtime));
                }

                var chosen = pending.LegalTargetHeroIds.Count > 0
                    ? (Target: "hero:" + pending.LegalTargetHeroIds[0].Value,
                        Command: DirectorCommand.ChooseAttackTarget(
                            pending.ResolutionId, pending.ChoiceId, pending.LegalTargetHeroIds[0]))
                    : (Target: "minion:" + pending.LegalTargetMinionIds[0].Value,
                        Command: DirectorCommand.ChooseAttackTarget(
                            pending.ResolutionId, pending.ChoiceId, pending.LegalTargetMinionIds[0]));

                Assert.That(runtime.SubmitProjectedCommand(chosen.Command), Is.True,
                    Remaining(runtime));
                takenInOrder.Add(chosen.Target);
                yield return null;

                for (var budget = 0; budget < 4 && runtime.PendingDefenseChoice != null; budget++)
                {
                    runtime.DeclineDefense();
                    yield return null;
                    yield return null;
                }

                for (var budget = 0; budget < 4 && AStandingChoiceCanBeAnswered(runtime); budget++)
                {
                    AnswerStandingChoiceHeadlessly(runtime);
                    yield return null;
                    yield return null;
                }

                yield return null;
            }

            Assert.That(takenInOrder, Is.Not.Empty,
                "The printed instance has to have run at all. " + Remaining(runtime));

            // WHAT THE JOURNEY MEASURED, stated whichever bound the board turned out to impose.
            if (takenInOrder.Count < 3)
            {
                // THE BOARD BOUNDED THE CHAIN BEFORE THE CARD DID, which is an ordinary end and not a
                // failure: "a unit adjacent to you" is re-measured from her space every instance, and
                // this staging holds a small number of neighbours. The pairwise-versus-single-slot
                // divergence cannot be OBSERVED in fewer than three instances - it first appears when
                // instance 3 re-offers instance 1's target - so what this run proves is the enforced
                // half, and the shortfall stays pinned at the definition tier by
                // WhisperVerifiedContentTests
                // .BloodFrenzy_IsTheFirstAttackRepeat_WhoseDistinctTargetMaximumIsAboveOne.
                Assert.That(takenInOrder, Has.Count.GreaterThanOrEqualTo(1),
                    "The chain ran " + takenInOrder.Count + " instance(s) before the board ran out " +
                    "of adjacent subjects: " + string.Join(" -> ", takenInOrder) + ". " +
                    Remaining(runtime));
                yield break;
            }

            // THE SHORTFALL, OBSERVED. Three or more instances ran, so instance 3's offer is where the
            // two readings diverge - and under the single-slot exclusion the first instance's target is
            // back in it. This assertion is written to hold for the ENGINE AS IT IS: if the pairwise
            // fix ever lands, this is the case that turns red, which is exactly what a pinned
            // measurement is for.
            Assert.That(offeredAtEachInstance[2], Does.Contain(takenInOrder[0]),
                "MEASURED SHORTFALL, not printed behaviour. RULING 34 reads \"on different targets\" " +
                "as pairwise distinct across the whole chain, and the engine holds ONE excluded " +
                "target reset at each instance - so the third instance is offered the first's target " +
                "again. Taken in order: " + string.Join(" -> ", takenInOrder) + "; instance 3 was " +
                "offered: " + string.Join(", ", offeredAtEachInstance[2]) + ". If this assertion " +
                "fails, the accumulating-exclusion fix has landed and this journey should be inverted " +
                "rather than deleted. " + Remaining(runtime));
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

            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "One selection of two does not close a modal whose ceiling is two - the second bullet " +
                "is still takeable, which is what \"or both\" means. " + Remaining(runtime));

            var remaining = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(remaining, Is.Not.Empty,
                "The unchosen bullet is still offered after the first is taken. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, remaining[0].Label));
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
        [Explicit(
            "MEASURED AT 8.35z53's FIRST PLAY MODE RUNS: FIXTURE INSTRUMENTATION, and the message names " +
            "it exactly - \"wasp-magnetic-dagger must be selectable from primitive-hero's hand\". Wasp's " +
            "Gold basic IS dealt to her Tier III hand (WaspProgressedHands puts Magnetic Dagger and " +
            "Static Barrier there beside the upgrades), so the card is not missing from the staging: the " +
            "arc SPENDS it earlier, as the Defense that fills the enemy discard pile this journey's " +
            "subject depends on, and then names the same card for a later commit. WHAT IS ELIMINATED: the " +
            "raised ceiling, which is real and now proved - " +
            "GrimReaper_RaisesSwiftJusticesCeilingSoBothBulletsRunTheirOwnFullSequence passes at this " +
            "milestone once the cardinality query was given the card supertype it had never been passed. " +
            "WHAT STANDS is one card's bookkeeping inside this arc.")]
        public IEnumerator DeathSeeker_AppliesBothOfItsBulletsOnceGrimReaperHasRaisedItsCeiling()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedWhisper);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var whisperId = WhisperSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, whisperId);

            // SETUP, HEADLESSLY, AND IT HAS TO FILL TWO PILES. The gate reads an ENEMY hero's pile;
            // the retrieve bullet reads HER OWN. Both fill only by defending, so both heroes have to
            // have answered an Attack before this card is worth its second bullet.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.SwiftJustice);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            yield return null;

            for (var budget = 0; budget < 3 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            var opening = runtime.PendingAttackTargetChoice;
            if (opening != null && opening.LegalTargetHeroIds.Contains(enemyId))
            {
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                    opening.ResolutionId, opening.ChoiceId, enemyId));
                yield return null;
                var answer = SurvivingDefense(runtime);
                if (answer != null)
                {
                    runtime.SubmitProjectedCommand(answer.Command);
                    yield return null;
                    yield return null;
                }
            }

            for (var budget = 0; budget < 4 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // The enemy's turn: their Attack fills HER pile when she answers it.
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            if (runtime.SubmitAction(PrimaryActionOf(runtime)))
            {
                yield return null;
                var theirs = runtime.PendingAttackTargetChoice;
                if (theirs != null && theirs.LegalTargetHeroIds.Contains(whisperId))
                {
                    runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                        theirs.ResolutionId, theirs.ChoiceId, whisperId));
                    yield return null;
                    var hers = SurvivingDefense(runtime);
                    if (hers != null)
                    {
                        runtime.SubmitProjectedCommand(hers.Command);
                        yield return null;
                        yield return null;
                    }
                }
            }

            for (var budget = 0; budget < 4 && AStandingChoiceCanBeAnswered(runtime); budget++)
            {
                AnswerStandingChoiceHeadlessly(runtime);
                yield return null;
                yield return null;
            }

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // HER TURN, ON THE LIVE SURFACE.
            CommitSoTheNamedSeatActs(runtime, whisperId, WhisperCardContent.DeathSeeker);
            yield return YieldUntilTheNamedSeatActs(runtime, whisperId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(whisperId), Remaining(runtime));

            var deathSeeker = runtime.Cards.Single(card =>
                card.OwnerId.Equals(whisperId) &&
                card.DefinitionReference.DefinitionId.Equals(WhisperCardContent.DeathSeeker));

            yield return PointTrayAt(root, whisperId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The enemy's pile holds the face they defended with, so the gate is satisfied. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.MaximumSelections, Is.EqualTo(2),
                "The same Ultimate raises the same number on her other basic card - and \"both\" here " +
                "is the plain reading, because these bullets are two EFFECTS. " + Remaining(runtime));

            var bullets = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(bullets, Has.Length.EqualTo(2), Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, bullets[0].Label));
            yield return null;

            var second = runtime.RoundView.Model.EffectChoices
                .Where(value => value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select)
                .ToArray();
            Assert.That(second, Is.Not.Empty, Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(hud, second[0].Label));
            yield return null;
            yield return null;

            Assert.That(runtime.IsActiveEffectSource(deathSeeker.Id), Is.True,
                "The immunity bullet applied. " + Remaining(runtime));

            // THE RETRIEVE BULLET'S OWN `may`, which is the bullet's optionality and not the modal's.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"You MAY retrieve a discarded card\" opens its own confirm-or-decline once the " +
                "bullet has been chosen - which is a different thing from never choosing it. " +
                Remaining(runtime));
            var decline = DeclineControl(runtime);
            Assert.That(decline, Is.Not.Null,
                "A printed `may` is answerable both ways. " + Remaining(runtime));

            Assert.That(AnswerStandingChoiceHeadlessly(runtime), Is.True,
                "Confirming opens the hand-card question the retrieve is about. " + Remaining(runtime));
            yield return null;
            yield return null;

            if (runtime.PendingEffectChoice != null)
            {
                Assert.That(StandingChoiceBelongsTo(runtime, whisperId), Is.True,
                    "\"A discarded card\" is HER OWN pile, so the choice is hers - which is the " +
                    "contrast with Greater Good's discard, where the same kind of question belongs to " +
                    "the hero paying it. " + Remaining(runtime));
                Assert.That(AnswerStandingChoiceHeadlessly(runtime), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Both bullets have applied and the card is finished. " + Remaining(runtime));
        }
    }
}
