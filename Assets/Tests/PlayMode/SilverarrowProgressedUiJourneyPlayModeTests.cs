using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.SilverarrowEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SilverarrowUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Silverarrow's upgraded hands, driven through the surface a player uses. Milestone
    /// 8.35z46, and the tier where four of her six rulings first become reachable.
    ///
    /// <para><b>Which branch a staged hand took decides which half of her surface exists.</b> The
    /// SECOND sibling of each colour is the only branch holding an "End of turn:" face (Warning Shot
    /// at II, Treetop Sentinel at III), the only one holding a retrieval (Fae Healing, Nature's
    /// Blessing) and the only one holding Rain of Arrows. The FIRST is the only one holding a
    /// realised-distance self-move (Divert Attention, Lead Astray) or the Tier III modal (Clear Shot).
    /// So no single arm reaches her whole set and the four between them do - which is why both
    /// branches carry journeys here rather than one standing for the other.</para>
    ///
    /// <para><b>What Tier III adds beyond a bigger hand.</b> Wild Hunt is unlocked, and it is the
    /// first Ultimate in this build that fires before EVERY action rather than before one kind or
    /// after a basic one. So a Tier III Silverarrow opens a decision in front of every action she
    /// performs, and that free two-space straight line is also what lets her reach RULING 1's band
    /// from where she is standing - her whole play pattern in one card.</para>
    /// </summary>
    public sealed class SilverarrowProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// RULING 37, and it is the milestone's subtlest binding: "Move an enemy unit adjacent to you
        /// up to 2 spaces; if you do, move up to that number of spaces in a straight line."
        ///
        /// <para><b>"That number" is the spaces the enemy was ACTUALLY moved</b>, not the printed
        /// ceiling. The first Move publishes its realised distance and the straight-line chooser binds
        /// that output as its own bound, so a push of one leaves her a line of one - which is what this
        /// drives and what a fixture reading the composition cannot show, because both readings compose
        /// the same two nodes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DivertAttention_OffersHerALineNoLongerThanTheSpacesItActuallyMovedTheEnemy()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            // Both seats committed explicitly rather than through the single-named helper: the
            // shared helper auto-fills the OTHER seat's lowest-Initiative card, and Divert Attention
            // is hers - her Green cards carry the lowest printed Initiative of anything she holds -
            // so an auto-filled round 1 would spend the very card this journey names by hand on
            // round 2.
            CommitBothSeats(
                runtime,
                silverarrowId, SilverarrowCardContent.EntanglingVines,
                enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(
                runtime, silverarrowId, SilverarrowCardContent.DivertAttention);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True,
                "The enemy adjacent to her is named by clicking the piece.");
            yield return null;

            // The push is "up to 2", so the board offers destinations at one and at two; this journey
            // takes the ONE-space answer on purpose, because that is the cell where the two readings
            // of "that number" disagree.
            var enemyBefore = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var oneAway = LitDestinations(runtime)
                .Where(space => HexDistanceQuery.Calculate(space, enemyBefore) == 1)
                .ToArray();
            Assert.That(oneAway, Is.Not.Empty,
                "An \"up to 2\" push must offer the one-space answer as well as the two-space one. " +
                Remaining(runtime));
            var pushTo = FirstClickableSpace(pointerInput, oneAway);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, Tile(pushTo).transform.position),
                Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(pushTo),
                "The enemy moved exactly one space, which is the number the second clause binds.");

            // Her own straight-line move is now bounded at ONE, not at the printed two.
            var herLine = LitDestinations(runtime);
            Assert.That(herLine, Is.Not.Empty,
                "The second Move must open with the spaces its bound admits. " + Remaining(runtime));
            Assert.That(
                herLine.All(space =>
                    HexDistanceQuery.Calculate(space, silverarrowAt) <= 1),
                Is.True,
                "RULING 37: \"that number\" is the spaces the enemy was ACTUALLY moved. A two-space " +
                "destination offered here would be the printed-ceiling reading the user overruled, " +
                "and it is the one thing this journey exists to refuse.");

            // MILESTONE 8.86. The second Move is TAKEN. This journey read the bound off the offer and
            // stopped there, so it proved which spaces were lit and never that pressing one of them
            // moved her - and "move up to that number of spaces" is a printed move, which 8.86 says a
            // journey answers by making it rather than by inspecting the question.
            var travelling = herLine.Where(space => !space.Equals(silverarrowAt)).ToArray();
            Assert.That(travelling, Is.Not.Empty,
                "A bound of one admits a step of one, so the only space offered cannot be the one " +
                "she is already standing on. " + Remaining(runtime));
            var herStep = FirstClickableSpace(pointerInput, travelling);
            var stepNode = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, Tile(herStep).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, stepNode);
            yield return null;

            var herNow = runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            Assert.That(herNow, Is.EqualTo(herStep),
                "THE ASSERTION THIS JOURNEY WAS MISSING: she really walked the line the bound left " +
                "her. " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(herNow, silverarrowAt), Is.EqualTo(1),
                "one space - the number the enemy was actually moved, and not the printed two.");
            Assert.That(HeroView(silverarrowId).Coordinate, Is.EqualTo(herNow),
                "with her token drawn there rather than where she started.");
        }

        /// <summary>
        /// RULING 37's other arm, and the one that needed no condition of its own: an enemy moved
        /// NOUGHT spaces leaves her a straight line bounded at nought, which offers nothing.
        ///
        /// <para>The first Move is "up to 2", so nought is a legal answer - the player declines the
        /// push - and a second Move bounded at nought is a step that cannot be taken. That is why the
        /// printed "if you do" needed no predicate: the two readings agree in every board position
        /// because the bound does the work.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DivertAttention_OffersHerNothing_WhenTheEnemyWasMovedNoSpacesAtAll()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            // Both seats committed explicitly - see the sibling journey above for why Divert
            // Attention cannot be left to the shared helper's auto-fill.
            CommitBothSeats(
                runtime,
                silverarrowId, SilverarrowCardContent.EntanglingVines,
                enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            CommitSoTheNamedSeatActs(
                runtime, silverarrowId, SilverarrowCardContent.DivertAttention);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;

            var enemyBefore = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            // The zero-distance answer is the occupied origin on the board, not a decline button.
            Assert.That(runtime.RoundView.Model.EffectChoiceTargets.Any(value => value.Space.Equals(enemyBefore)), Is.True);
            Assert.That(UiPlayer.ClickBoard(pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate,
                Is.EqualTo(enemyBefore),
                "Nought spaces moved, which is a legal answer to an \"up to\".");
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate,
                Is.EqualTo(silverarrowAt),
                "And a second Move bounded at nought is a step not taken - she is where she was. " +
                "That is the whole of why \"if you do\" needed no condition.");

            // MILESTONE 8.86, rule three: a step that says nothing happens has to assert THAT rather
            // than stop in silence. "Move up to that number of spaces in a straight line" over a
            // number of nought is a move with nowhere to go, so the board offers her no space she is
            // not already standing on - which is a stronger claim than "she did not move", because
            // she could not have moved if she was never asked either.
            Assert.That(
                LitDestinations(runtime).Where(space => !space.Equals(silverarrowAt)),
                Is.Empty,
                "A second Move bounded at nought offers her no travelling destination at all. " +
                Remaining(runtime));
        }

        /// <summary>
        /// RULING 8's first arm: "A hero in radius may retrieve a discarded card" admits the ENEMY, and
        /// the user's ruling is the literal one - the printed "a hero" beats the glossary's friendly
        /// reading.
        ///
        /// <para>The enemy has to have discarded something first, which is what the earlier turn is
        /// for: a Defense answer puts a card in their pile, and Fae Healing then hands it back. The
        /// coin follows only "if they do", and that binding is the same undefined-output shape RULING
        /// 37's bound is - no separate did-it-happen predicate anywhere.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FaeHealing_RetrievesACardForTheEnemyHeroItNamed()
        {
            yield return LoadHealingStaging();
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            Assert.That(DiscardOf(runtime, enemyId), Is.Not.Empty,
                "The retrieval needs something to retrieve, and an answered Attack is what puts it " +
                "there.");

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.FaeHealing);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var candidates = runtime.PendingEffectChoice.Candidates.ToArray();
            Assert.That(candidates, Has.Length.EqualTo(2),
                "RULING 8 admits any hero in the disk - the enemy AND herself - so a Radius 4 disk " +
                "on this staging offers exactly two. One candidate would be the glossary reading " +
                "the user overruled; three would mean a hero arrived from somewhere.");

            var discardedBefore = DiscardOf(runtime, enemyId).Length;
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True,
                "She names the enemy by clicking their piece - the board-first contract.");
            yield return null;

            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);
            yield return AnswerTheRetrievalWithItsFirstCard(root);
            yield return null;

            Assert.That(DiscardOf(runtime, enemyId), Has.Length.EqualTo(discardedBefore - 1),
                "A card left the enemy's discard pile, which is what \"retrieve\" means.");
            // MILESTONE 8.86. A card leaving a discard pile is half the sentence; "retrieve" names
            // where it goes, and a step that removed it from play would satisfy the count above.
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore + 1),
                "and it arrived in the enemy's HAND - retrieved, not merely gone. " +
                Remaining(runtime));
        }

        /// <summary>
        /// RULING 8's second arm, which is the half the user settled last and the half a glossary
        /// reading would have refused outright: "We include self."
        ///
        /// <para>She stands at radius 0 of her own anchor, so she is always a candidate of her own
        /// disk. The structural argument the user's decision rests on is on record: p.17 needed a
        /// SPECIAL rule to exclude self from "friendly", and an unqualified "a hero" has no such
        /// exclusion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FaeHealing_RetrievesACardForSilverarrowHerself()
        {
            yield return LoadHealingStaging();
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            // Her own discard is filled by answering an attack, which is the one route a hero has to
            // a discard pile before p.15 has upgraded anybody.
            Assert.That(DiscardOf(runtime, silverarrowId), Is.Not.Empty);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.FaeHealing);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var discardedBefore = DiscardOf(runtime, silverarrowId).Length;

            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(silverarrowId).transform.position), Is.True,
                "She names HERSELF by clicking her own piece, which is the arm RULING 8 settled " +
                "last and the one no friendly-hero reading would offer.");
            yield return null;

            var herHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(silverarrowId) && card.State.Location == CardLocation.Hand);
            yield return AnswerTheRetrievalWithItsFirstCard(root);
            yield return null;

            Assert.That(DiscardOf(runtime, silverarrowId), Has.Length.EqualTo(discardedBefore - 1),
                "Her own card came back to her own hand.");
            // MILESTONE 8.86: "came back to her own hand" is what the sentence says, so it is read
            // in her hand rather than inferred from a pile that got one shorter.
            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(silverarrowId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(herHandBefore + 1),
                "and her hand is one card larger for it. " + Remaining(runtime));
        }

        /// <summary>
        /// SL-06, driven through a whole turn: "End of turn: An enemy hero in radius discards a card,
        /// if able."
        ///
        /// <para><b>This is the journey milestone 8.35z30 named as its largest unverified claim.</b>
        /// That milestone built the keyword, gave the turn's close a producer and inverted the
        /// lifecycle's trigger/expiry order for the coincident case - and could prove none of the part
        /// a player experiences: that the turn WITHHOLDS its close while the trigger's questions stand,
        /// and completes when they are answered. <c>SilverarrowEndOfTurnDriveTests</c> drives that
        /// handshake at the authority; this drives it on the surface, where the two questions belong to
        /// two different players.</para>
        ///
        /// <para>Both halves of SL-06 are here. Whose turn: the effect expires at the same close it
        /// fires at, so it can only ever fire on hers. When the radius is measured: at FIRING time,
        /// against wherever she is standing then - which is why the discard is asked of an enemy who
        /// is inside the disk at the close rather than at the moment the Skill resolved.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator WarningShot_FiresAtTheCloseOfHerOwnTurnAndTakesACardFromTheEnemyInRadius()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.WarningShot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var enemyHandBefore = runtime.Cards.Count(card =>
                card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand);

            yield return StartActionThroughControl(root, ActionKind.Skill);

            // The Skill itself asks nothing: its whole printed content is that a piece of work happens
            // later. What stands after it is the effect, on her own tray.
            var warningShotInstance = runtime.Cards.Single(card =>
                card.OwnerId.Equals(silverarrowId) &&
                card.DefinitionReference.DefinitionId.Equals(SilverarrowCardContent.WarningShot));
            Assert.That(runtime.IsActiveEffectSource(warningShotInstance.Id), Is.True,
                "The scheduled work must be readable from the card that scheduled it, or a player " +
                "has no way to know their turn is going to end differently.");

            // The turn's close is what fires it, so the turn is handed on - and the close does not
            // complete while the trigger's questions stand.
            yield return YieldUntilNobodyIsActing(runtime);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "SL-06: the work is printed as happening at the end of THIS turn, so the turn is " +
                "not over while its question stands. A turn that closed here would be the silent " +
                "keyword milestone 8.35z30 built three pieces to avoid. " + Remaining(runtime));

            // Question one is hers: which enemy hero inside the disk.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));

            // Question two is THEIRS: which card leaves their hand. The chooser changing sides
            // mid-effect is what makes this face a negotiation rather than a number.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The named enemy is asked which card to discard. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "It is their hand, so it is their choice.");
            yield return AnswerTheHandChoiceWithItsFirstCard(root);

            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.EqualTo(enemyHandBefore - 1),
                "A card left the enemy's hand at the close of HER turn.");
            Assert.That(runtime.IsActiveEffectSource(warningShotInstance.Id), Is.False,
                "The effect expires at the very checkpoint it fires at, so it is gone once the work " +
                "is done - which is what makes it fire exactly once.");
        }

        /// <summary>
        /// RULING 38 on the board a player actually gets: "Target a unit at maximum range. If you
        /// target a hero, repeat once on a different hero; if you do, repeat once on a minion."
        ///
        /// <para><b>What this journey CANNOT do, measured before it was written, and why it is not a
        /// gap.</b> The chain's second instance admits "a different hero", narrowed to ENEMY heroes.
        /// Every staged scene in this build is one hero per team, so once the printed instance has
        /// taken the one enemy hero there is nobody different left - the chain is structurally bounded
        /// at ONE instance on every staged scene, whatever the Tier. That is arithmetic about the
        /// staging rather than a fact about the card, and it is exactly the trap milestone 8.35z37
        /// recorded when it found three shipped journeys that had only ever DECLINED the prompt their
        /// names claimed to drive. So the three-instance chain is proved where a board can be built
        /// for it - <c>SilverarrowRepeatChainTests
        /// .RainOfArrows_ChainsThreeInstances_EachNarrowedToWhatItsOwnClausePrints</c>, which stands
        /// two enemy heroes and a minion at exactly Range 3 - and this journey proves the half a
        /// player can reach.</para>
        ///
        /// <para><b>The half a player can reach is the one the gate's repeat shape is about.</b> The
        /// face prints no "may", so nobody is asked whether to repeat: an "if able" step with no
        /// subject is skipped in SILENCE. What this drives is therefore the absence of a control -
        /// the printed instance resolves, the repeat node is reached, and no confirm-or-decline ever
        /// appears. Wasp's Thunder Boomerang and Xargatha's Cleave both draw one in the same position,
        /// which is what makes the absence evidence rather than an empty assertion.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator RainOfArrows_TakesItsHeroAndOpensNoRepeatControl_TheChainHavingNowhereToGo()
        {
            yield return LoadRangeStaging(SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSilverarrow, 3);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.RainOfArrows);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Attack);

            // This staging unlocks Wild Hunt, so "each time before you perform an action" opens its
            // own decision in FRONT of the Attack's target question - proved standing alone in
            // WildHunt_IsDrawnFaceupOnHerRailAndOffersALineBeforeTheActionSheStarts above. Declining it
            // is what this journey is actually about, so it is declined rather than driven here.
            var wildHunt = DeclineControl(runtime);
            if (wildHunt != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, wildHunt.Label));
                yield return null;
            }

            var choice = runtime.PendingAttackTargetChoice;
            Assert.That(choice, Is.Not.Null, Remaining(runtime));
            var hero = choice.LegalTargets.FirstOrDefault(target =>
                target.Kind == AttackTargetKind.Hero);
            Assert.That(hero.IsDefined, Is.True,
                "This staging deals one enemy hero, and the chain's condition is that the printed " +
                "instance took a hero.");
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), root.Hud,
                HeroView(hero.HeroId).transform.position), Is.True);
            yield return null;

            if (runtime.PendingDefenseChoice != null)
            {
                // Milestone 8.46e: the Defense is declined on the decision window's own corner
                // (`TAKE THE HIT`), read off its model - the strip no longer draws `DECLINE DEFENSE`.
                UiPlayer.ClickDefenseDecline(runtime, root.Hud);
                yield return null;
            }

            // MILESTONE 8.86. The PRINTED instance's own outcome, read before the chain's absence is
            // read: "one enemy hero on the board and the printed instance took them" is the premise
            // every assertion below rests on, and this journey used to assume it from the click. A
            // declined Defense is a Defense Total of nought, which is p.14's step 6.
            Assert.That(runtime.Heroes.Any(candidate => candidate.HeroId.Equals(enemyId)), Is.False,
                "The hero the printed instance targeted declined the Defense and was defeated, " +
                "which is why \"a different hero\" then has nobody. " + Remaining(runtime));

            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "One enemy hero on the board and the printed instance took them, so \"a different " +
                "hero\" has nobody - and a step that cannot happen is skipped rather than offered " +
                "or refused. " + Remaining(runtime));
            Assert.That(
                runtime.RoundView.Model.EffectChoices.Any(value =>
                    value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm),
                Is.False,
                "Nothing was asked, which is the whole of \"skipped in silence\".");
        }

        /// <summary>
        /// The end-of-turn discard's TWO questions, answered again after an Undo and after a restore -
        /// and this journey is aimed at a defect class rather than at a face.
        ///
        /// <para><b>Why this pause and not another.</b> Milestone 8.35z34 found that a two-question
        /// pause was NOT restore-safe: a snapshot captured on question two came back asking question
        /// one, because a restored pause re-enters a node through Execute rather than through Resume,
        /// so Execute must be idempotent with respect to the answers already recorded against it. It
        /// fixed that for its own node and recorded p.18's batch Push as an UNTESTED sibling. Warning
        /// Shot is a third two-question shape - and unlike either of those two, its questions are
        /// asked of DIFFERENT PLAYERS and stand at a moment when Initiative has no selected actor at
        /// all, which is the one thing 8.35z30 could not drive.</para>
        ///
        /// <para><b>The order of the two operations is forced rather than chosen.</b> A snapshot
        /// restore permanently disables history - accepted commands come back empty and UNDO has
        /// nothing to drop - so an Undo attempted after a restore is testing nothing. Undo first.</para>
        ///
        /// <para>What tells the two questions apart here is not the node id but the CHOOSER: question
        /// one belongs to Silverarrow and question two to the enemy whose hand the card comes out of.
        /// That is a cheaper discriminator than the one Dodger's journey needed, and it is worth
        /// noting for the next two-question shape.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheEndOfTurnDiscardsTwoQuestions_AreAnsweredAgainAfterAnUndoAndAfterARestore()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.WarningShot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return YieldUntilNobodyIsActing(runtime);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The turn's close must be standing on the trigger's first question. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(silverarrowId),
                "Question one is hers: which enemy hero in radius.");
            var node = runtime.PendingEffectChoice.NodeId;
            var commandsAtPause = runtime.AcceptedCommands.Count;

            // Answer question one, which takes a selection and then its confirm - two accepted
            // commands, not one, which is the surface fact the authority-level drive measured.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));

            Assert.That(runtime.AcceptedCommands.Count, Is.GreaterThan(commandsAtPause),
                "The answer has to have been accepted for an Undo to have anything to drop.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and question TWO is now standing. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "It is their hand, so it is their choice - and that is what tells the two questions " +
                "apart without reading a node id.");

            // THE UNDO HALF.
            UiPlayer.Click(UiPlayer.Button(hud, "DIRECTOR"));
            yield return null;
            for (var press = 0; press < 4 && runtime.AcceptedCommands.Count > commandsAtPause; press++)
            {
                UiPlayer.Click(UiPlayer.Button(hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandsAtPause),
                "The Undo drops exactly the answers it retained the prefix of.");
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the turn is standing on its own close again, replayed rather than remembered - " +
                "which is the part of this that no earlier fixture reaches, because the close is " +
                "withheld with no selected actor. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(silverarrowId),
                "and it is question ONE that came back.");
            Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node));

            // THE RESTORE HALF, taken from the pause the Undo returned to.
            var payload = runtime.CaptureSnapshot();
            Assert.That(payload, Is.Not.Null.And.Not.Empty);

            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, StandingNode(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemyId),
                "Question two again, so the state the snapshot was taken from has moved on.");

            Assert.That(runtime.RestoreSnapshot(payload), Is.True, "The payload must restore.");
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "The restored match is stopped on the decision it was captured on, with the turn's " +
                "close still withheld. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(silverarrowId),
                "and it is question ONE - the question the capture was standing on - rather than the " +
                "one the answers had moved past. A restored pause re-entering through Execute is " +
                "what milestone 8.35z34 had to make idempotent, and this is a third shape of it.");
        }

        /// <summary>
        /// Wild Hunt: "Each time before you perform an action you may move 2 spaces in a straight
        /// line." The first Ultimate in this build that fires before EVERY action rather than before
        /// one kind, and the first whose decision a player meets more than once in a turn.
        /// </summary>
        [UnityTest]
        public IEnumerator WildHunt_IsDrawnFaceupOnHerRailAndOffersALineBeforeTheActionSheStarts()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierThreeEngagedStagedSilverarrow);
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;

            var ultimate = runtime.Cards.Single(card =>
                card.OwnerId.Equals(silverarrowId) &&
                card.DefinitionReference.DefinitionId.Equals(SilverarrowCardContent.WildHunt));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea),
                "p.16 puts an unlocked Ultimate faceup in its own area, which is where a player " +
                "reads that the permanent ability is live.");

            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.ClearShot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var startedAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            yield return StartActionThroughControl(root, ActionKind.Attack);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"Each time before you perform an action\" opens its decision in FRONT of the " +
                "action, so the Attack's own target question has not been reached yet. " +
                Remaining(runtime));

            var line = LitDestinations(runtime);
            Assert.That(line, Is.Not.Empty,
                "The free move offers the spaces a straight line of exactly two admits.");

            // Milestone 8.59. EXACTLY two, not at most two. This read `<= 2` until the Bounce sweep
            // measured the same defect here: "you may move 2 spaces" leaves whether to move to the
            // hero - which is the node's own `May` policy - and leaves the distance to nobody.
            Assert.That(
                line.All(space => HexDistanceQuery.Calculate(space, startedAt) == 2),
                Is.True,
                "Two spaces is the printed count, and nothing on this card raises or lowers it - hers " +
                "is the first Ultimate in the roster that prints neither Range nor Radius, so p.16's " +
                "item has nothing here to reach.");

            // MILESTONE 8.86. THE MOVE IS MADE. This journey measured the shape of the offer and
            // stopped, so everything it said was about spaces being LIT - and a free move nobody ever
            // takes is indistinguishable from an offer the engine draws and cannot honour. "You may
            // move 2 spaces in a straight line" is a printed move, and 8.86 answers a printed move by
            // making it and reading the board afterwards.
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var landing = FirstClickableSpace(pointerInput, line);
            var lineNode = StandingNode(runtime);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, Tile(landing).transform.position),
                Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, lineNode);
            yield return null;

            var herNow = runtime.Heroes.Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            Assert.That(herNow, Is.EqualTo(landing),
                "THE ASSERTION THIS JOURNEY WAS MISSING: the free line really moved her, before the " +
                "action she started. " + Remaining(runtime));
            Assert.That(HexDistanceQuery.Calculate(herNow, startedAt), Is.EqualTo(2),
                "two spaces, which is the printed count and the reach that lets her play her own " +
                "band from where she was standing.");
            Assert.That(HeroView(silverarrowId).Coordinate, Is.EqualTo(herNow),
                "with her token drawn there rather than where she started.");
        }

        /// <summary>
        /// Milestone 8.66. Treetop Sentinel's second half, on the surface a player uses: "<b>End of
        /// turn:</b> An enemy hero in radius discards a card, <b>or is defeated</b>."
        ///
        /// <para><b>The defect this journey would have failed on: the face could never defeat
        /// anybody.</b> Its selection declared the discard as its consumer, and a consumer declaration
        /// narrows the offer to candidates the consumer can act on - so the empty-handed hero the
        /// second half of the sentence exists for was narrowed out of the first half, the mandatory
        /// step had no legal subject, and the turn closed in silence. Warning Shot, whose step ends
        /// "if able", keeps that narrowing and is driven one journey above.</para>
        ///
        /// <para>The enemy's hand is staged to exactly one card, which p.7's commitment then spends -
        /// which is how Min's Death Grenade journey empties a hand, and the only way to reach the
        /// escalation from a seat rather than from a fixture. Two loads rather than one because a
        /// staging description names heroes by id and the ids only exist once a configuration has been
        /// composed.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TreetopSentinel_DefeatsTheEnemyInRadiusWhoseHandTheCommitmentEmptied()
        {
            yield return LoadEmptyHandedEnemyStaging();
            var root = Root();
            var runtime = root.Runtime;
            var silverarrowId = SilverarrowSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, silverarrowId);
            var coinsBefore = CoinsOf(runtime, silverarrowId);

            CommitSoTheNamedSeatActs(
                runtime, silverarrowId, SilverarrowCardContent.TreetopSentinel);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            Assert.That(
                runtime.Cards.Count(card =>
                    card.OwnerId.Equals(enemyId) && card.State.Location == CardLocation.Hand),
                Is.Zero,
                "The commitment spent their only card, which is what puts the escalation in reach. " +
                Remaining(runtime));

            yield return StartActionThroughControl(root, ActionKind.Skill);

            // This staging unlocks Wild Hunt, so "each time before you perform an action" opens its
            // own decision in front of the Skill. It is declined: the free line is not what this
            // journey is about, and moving her would change the disk the trigger reads.
            var wildHunt = DeclineControl(runtime);
            if (wildHunt != null)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, wildHunt.Label));
                yield return null;
            }

            yield return YieldUntilNobodyIsActing(runtime);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "THE assertion of this milestone: an empty-handed hero is the candidate the second " +
                "half of the sentence exists for, so the turn's close must still ask which enemy " +
                "hero. A silent close here is the shipped defect. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(silverarrowId),
                "p.10 gives the unassigned decision to the card's controller.");
            Assert.That(
                runtime.PendingEffectChoice.Candidates.Select(
                    candidate => candidate.Value.HeroId),
                Does.Contain(enemyId),
                "and the empty-handed hero is IN the offer. " + Remaining(runtime));

            // The node is read BEFORE the click, because naming the only candidate is what closes an
            // exactly-one question: this one answer finishes the selection, the effect and the turn,
            // and reading a standing node afterwards would be reading one that is no longer there.
            var selection = StandingNode(runtime);
            var killsBefore = KillsOf(runtime, silverarrowId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(
                UiPlayer.ClickBoard(
                    pointerInput, root.Hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return ConfirmIfStillStanding(root, selection);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "An empty hand has nothing to offer, so their seat is asked no discard question at " +
                "all. " + Remaining(runtime));
            Assert.That(KillsOf(runtime, silverarrowId), Is.EqualTo(killsBefore + 1),
                "\"or is defeated\" - p.13's Defeat, recorded as her Kill on the 8.53 scoreboard. " +
                "The scoreboard rather than the board, because the close carries straight on into " +
                "the next round, where p.13's respawn has already put the miniature back. " +
                Remaining(runtime));
            Assert.That(CoinsOf(runtime, silverarrowId), Is.GreaterThan(coinsBefore),
                "and the Defeat is priced: p.13's coins go to the hero whose card did it.");
        }

        private static int KillsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Projection.Scoreboard
                .Where(entry => entry.HeroId.Equals(heroId))
                .Select(entry => entry.Kills)
                .DefaultIfEmpty(0)
                .Single();

        /// <summary>
        /// Her Tier III alternate branch with the enemy holding exactly one card, so p.7's commitment
        /// leaves their hand empty before her turn closes.
        /// </summary>
        private static IEnumerator LoadEmptyHandedEnemyStaging()
        {
            const SceneCardContent content =
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedSilverarrow;
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var own = SilverarrowSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(enemy, hand: runtime.Cards
                        .Where(card => card.OwnerId.Equals(enemy) &&
                            card.State.Location == CardLocation.Hand)
                        .Select(card => card.DefinitionReference.DefinitionId)
                        .Take(1))
                });
            yield return LoadStagedScene(content);
        }

        private static int CoinsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId))
                .Coins;

        // ---------------------------------------------------------------------------------------
        // Steps
        // ---------------------------------------------------------------------------------------

        /// <summary>
        /// One enemy turn spent attacking her, answered with a card, so the ENEMY's discard pile holds
        /// something a retrieval can name. A discard is the one route to a discard pile this build has
        /// before p.15 has upgraded anybody.
        /// </summary>
        private static IEnumerator SpendOneOwnCardIntoHerDiscard(
            MatchSceneCompositionRoot root, HeroId silverarrowId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            var silverarrowAt = runtime.Heroes
                .Single(hero => hero.HeroId.Equals(silverarrowId)).Coordinate;
            // Both seats committed explicitly rather than through the single-named helper: the shared
            // helper auto-fills the OTHER seat's lowest-Initiative card, and Fae Healing is hers on
            // this branch - her Green cards carry the lowest printed Initiative of anything she holds
            // - so an auto-filled round 1 would spend the very card the outer journey names by hand on
            // a later turn.
            CommitBothSeats(
                runtime,
                silverarrowId, SilverarrowCardContent.LongShot,
                enemyId, WaspCardContent.MagneticDagger);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            yield return WalkHeadlesslyTo(
                runtime, enemyId, Contact(runtime, silverarrowAt, enemyId));
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);

            // A different card from the enemy's hand: MagneticDagger was already committed and
            // resolved above, on the earlier turn, so it is no longer in their hand to name here.
            // Shock is their other printed Attack against an adjacent unit, which is exactly the
            // contact this step already walked them into.
            CommitSoTheNamedSeatActs(runtime, enemyId, WaspCardContent.Shock);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);

            var attack = runtime.AvailableTurnActions.First(candidate =>
                candidate.IsAvailable &&
                KindOf(runtime, candidate.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.SubmitAction(attack.ActionDefinitionId), Is.True);
            yield return null;
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                runtime.PendingAttackTargetChoice.ResolutionId,
                runtime.PendingAttackTargetChoice.ChoiceId,
                silverarrowId)), Is.True);
            yield return null;

            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(defense.Command), Is.True);
            yield return null;
            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// The mirror of the step above, with the seats the other way round: SHE attacks, the enemy
        /// answers with a card, and it is their pile the retrieval names.
        /// </summary>
        private static IEnumerator SpendOneEnemyCardIntoTheirDiscard(
            MatchSceneCompositionRoot root, HeroId silverarrowId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, silverarrowId, SilverarrowCardContent.ShootAndScoot);
            yield return null;
            yield return YieldUntilTheNamedSeatActs(runtime, silverarrowId);

            var attack = runtime.AvailableTurnActions.First(candidate =>
                candidate.IsAvailable &&
                KindOf(runtime, candidate.ActionDefinitionId) == ActionKind.Attack);
            Assert.That(runtime.SubmitAction(attack.ActionDefinitionId), Is.True);
            yield return null;
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(
                runtime.PendingAttackTargetChoice.ResolutionId,
                runtime.PendingAttackTargetChoice.ChoiceId,
                enemyId)), Is.True);
            yield return null;

            var defense = SurvivingDefense(runtime);
            Assert.That(defense, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.SubmitProjectedCommand(defense.Command), Is.True);
            yield return null;

            // Shoot and Scoot's own after-attack rider: "if able, you may fast travel to an adjacent
            // zone." Declined rather than driven here, so the step this journey is actually about -
            // whether the enemy is still inside her printed Radius - reads the position she attacked
            // from rather than wherever a free move happened to leave her.
            var fastTravel = DeclineControl(runtime);
            if (fastTravel != null)
            {
                Assert.That(runtime.RoundPresenter.Submit(fastTravel), Is.True);
                yield return null;
            }

            yield return YieldUntilNobodyIsActing(runtime);
            yield return AdvanceToNextTurn(runtime);
        }

        /// <summary>
        /// Answers the standing hand-card question with the first card it offers, on the whole-face
        /// contract milestone 8.15a set.
        /// </summary>
        private static IEnumerator AnswerTheHandChoiceWithItsFirstCard(
            MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            var select = runtime.RoundView.Model.EffectChoices.FirstOrDefault(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select);
            Assert.That(select, Is.Not.Null,
                "A hand-card question must offer its faces as selectable controls. " +
                Remaining(runtime));
            // A hand-card face is a card ELEMENT and not a labelled button - the same submit-directly
            // fix milestone 8.35z38's dealing found for a defense face.
            var node = StandingNode(runtime);
            var face = UiPlayer.NamedElementWithText(root.Hud, "gd-effect-card", select.Label);
            UiPlayer.EnsureVisible(face);
            yield return null;
            UiPlayer.Click(face);
            yield return null;
            yield return ConfirmIfStillStanding(root, node);
        }

        /// <summary>The retrieval's own card question, which is the same whole-face contract.</summary>
        private static IEnumerator LoadHealingStaging()
        {
            const SceneCardContent content = SceneCardContent.VerifiedTierTwoAlternateEngagedStagedSilverarrow;
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime;
            var own = SilverarrowSeat(runtime).HeroId;
            var enemy = OtherHeroId(runtime, own);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[]
            {
                new SandboxHeroStaging(own, hand: runtime.Cards.Where(card => card.OwnerId.Equals(own) &&
                    card.State.Location == CardLocation.Hand && !card.DefinitionReference.DefinitionId.Equals(SilverarrowCardContent.ShootAndScoot))
                    .Select(card => card.DefinitionReference.DefinitionId)),
                new SandboxHeroStaging(enemy, hand: runtime.Cards.Where(card => card.OwnerId.Equals(enemy) &&
                    card.State.Location == CardLocation.Hand && !card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier))
                    .Select(card => card.DefinitionReference.DefinitionId))
            });
            yield return LoadStagedScene(content);
        }

        private static IEnumerator AnswerTheRetrievalWithItsFirstCard(
            MatchSceneCompositionRoot root)
        {
            yield return AnswerTheHandChoiceWithItsFirstCard(root);
        }

        /// <summary>
        /// The command that names one attack target, dispatched on the kind. p.19's two unit classes
        /// are two commands rather than one taking a target, which is what makes a chain narrowed to
        /// heroes and then to minions answerable at all.
        /// </summary>
        private static DirectorCommand TargetCommand(
            PendingAttackTargetChoice choice, AttackTarget target) =>
            target.Kind == AttackTargetKind.Hero
                ? DirectorCommand.ChooseAttackTarget(
                    choice.ResolutionId, choice.ChoiceId, target.HeroId)
                : DirectorCommand.ChooseAttackTarget(
                    choice.ResolutionId, choice.ChoiceId, target.MinionId);
    }
}
