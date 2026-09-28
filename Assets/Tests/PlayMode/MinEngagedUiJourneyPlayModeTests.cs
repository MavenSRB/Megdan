using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.MinUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Min the Dragonmonk at rulebook p.15's Tier II stage: the two Red siblings, the two Blue
    /// siblings and the two Green ones, driven through real pointer events and camera rays. Milestone
    /// 8.61k.
    ///
    /// <para>The staging is milestone 8.61j's two Tier II arms, and which arm a journey loads is
    /// decided by the face it drives: the FIRST sibling of every colour is Tiger Stance, Cluster Mine
    /// and Vanish, and the SECOND is Viper Stance, Death Grenade and Inner Strength. Both keep her Gold
    /// and Silver basics in hand, which is what keeps a bomb reachable at all.</para>
    ///
    /// <para><b>MI-Q13k, and it is this milestone's finding rather than a staging inconvenience.</b>
    /// Rulebook p.8 removes every token at the end of a round and her Smoke Bomb prints no override, so
    /// the only round in which a bomb of hers stands is the round her Silver resolved in - and she
    /// takes exactly one turn per round. Every bomb-consuming face of hers that is a DEFENCE therefore
    /// works (Poof!, Vanish and Ruse are discarded from hand on somebody else's turn, later in the same
    /// round), and both bomb-consuming ATTACKS - Viper Stance's and Cobra Stance's "you may swap with a
    /// Smoke bomb in radius" - cannot be reached by any sequence of legal play:
    /// placing the bomb costs the same turn the Stance would be played on.
    /// <see cref="ViperStanceSwapsWithTheSmokeBombInRadiusOrStaysWhereSheIs"/> MEASURES that rather
    /// than asserting round it, so the day the reading changes this journey reds and is rewritten to
    /// drive the swap.</para>
    /// </summary>
    public sealed class MinEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// Tiger Stance: "Target a unit adjacent to you. After the attack: You may move 1 space to a
        /// space adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces."
        ///
        /// <para><b>The optional step is measured as an exact set, and it is a set of one.</b> MI-Q11c
        /// reads "a space adjacent to the target" off the space the Attack BOUND, so the candidates are
        /// the free spaces that are both one step from (-3,1) and adjacent to (-2,1): her free
        /// neighbours are (-4,1), (-3,0) and (-2,0), and only (-2,0) touches the target. A step offer
        /// that had drifted to "anywhere one space away" would hold three.</para>
        ///
        /// <para>The Ultimate's absence is asserted here rather than left implied: p.16 turns Flurry of
        /// Blows faceup only at Tier IV, so at Tier II her rail draws no Ultimate card at all - which is
        /// the negative that makes <c>MinProgressedUiJourneyPlayModeTests</c>'s own reading mean
        /// something.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TigerStanceStepsBesideItsTargetThenPushesWhileFlurryIsStillLocked()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierTwoEngagedStagedMin,
                hand: new[] { MinCardContent.TigerStance, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(her)).UltimateCard,
                Is.Null,
                "p.16 unlocks the Ultimate at Tier IV, so her Tier II rail draws none. " +
                Remaining(runtime));

            yield return BeginMin(
                root, MinCardContent.TigerStance, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True,
                "\"a unit adjacent to you\". " + Remaining(runtime));
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                "Arrange: his printed Defense 6 survives her printed Attack 4, so the rider has a " +
                "target space to read. " + Remaining(runtime));

            var step = runtime.PendingEffectChoice;
            Assert.That(step, Is.Not.Null,
                "\"You may move 1 space\" stops the page. " + Remaining(runtime));
            Assert.That(step.NodeId.Value, Is.EqualTo("tiger-move-step"));
            Assert.That(step.DeclineInvocation, Is.Not.Null,
                "and a printed `may` can always simply be declined.");
            Assert.That(
                OfferedSpaces(runtime),
                Is.EqualTo(new[] { new HexCoordinate(-2, 0) }).AsCollection,
                "MEASURED: the one free space that is both one step from (-3,1) and adjacent to " +
                "(-2,1). " + Remaining(runtime));
            yield return Space(root, new HexCoordinate(-2, 0));
            Assert.That(Position(runtime, her), Is.EqualTo(new HexCoordinate(-2, 0)));
            Assert.That(Distance(Position(runtime, her), Position(runtime, enemy)), Is.EqualTo(1),
                "and she ends the step beside the target, which is what the sentence names.");

            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("tiger-push-subject"),
                "The push follows the step, in the printed order.");
            yield return Pick(
                root,
                subject.Candidates.Single(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(enemy)));

            var distance = runtime.PendingEffectChoice;
            Assert.That(distance.NodeId.Value, Is.EqualTo("tiger-push-push"));
            Assert.That(
                distance.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Integer)
                    .Select(candidate => candidate.Value.IntegerValue)
                    .OrderBy(value => value)
                    .ToArray(),
                Is.EqualTo(new[] { 0, 1, 2, 3 }).AsCollection);
            var before = Position(runtime, enemy);
            yield return PressAmount(root, 3);
            Assert.That(Distance(Position(runtime, enemy), before), Is.EqualTo(3),
                "and the whole printed distance is taken when three is what she presses. " +
                Remaining(runtime));
            SeatsAgree(runtime, "Tiger's step and push");
            Healthy(runtime);
        }

        /// <summary>
        /// Viper Stance: "Target a unit adjacent to you. After the attack: You may swap with a Smoke
        /// bomb in radius."
        ///
        /// <para><b>MI-Q13k, measured end to end.</b> Her Silver is committed in one round and places
        /// the bomb; the round's own p.8 cleanup then takes it; and in the next round - the first round
        /// her Viper Stance can be the card she committed - there is nothing in radius to swap with, so
        /// the printed `may` is never offered. That is not a staging accident: her supply holds one
        /// bomb, only her Silver places one, and one turn per round cannot both place it and use
        /// it.</para>
        ///
        /// <para><b>What this journey therefore proves, and what it does not.</b> It proves the whole
        /// reachable half - that the Silver really does put a bomb on the board, that p.8 really does
        /// take it at the round boundary, and that the Stance's rider is then correctly silent rather
        /// than offering a question with no answer (8.35z15's rule). It does NOT press the swap's
        /// confirm or its decline, because no configured match can open them; the gate's own row for
        /// this face declares <c>ConfirmOrDecline</c> from the printed `may`, and until MI-Q13k is
        /// settled that declaration rests on this measurement rather than on a press. Milestone 8.61k's
        /// report escalates it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ViperStanceSwapsWithTheSmokeBombInRadiusOrStaysWhereSheIs()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMin,
                hand: new[] { MinCardContent.SmokeBomb, MinCardContent.ViperStance },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];
            var round = runtime.RoundIndex;

            yield return BeginMin(
                root, MinCardContent.SmokeBomb, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-4, 1));
            Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(new HexCoordinate(-4, 1)),
                "Her bomb is on the board, one space from her and well inside Viper's printed " +
                "Radius 4. " + Remaining(runtime));

            yield return DriveUntil(
                root, () => runtime.RoundIndex > round, "the next round");
            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1),
                "The round turned over. " + Remaining(runtime));
            Assert.That(Tokens(runtime), Is.Empty,
                "MI-Q13k: rulebook p.8's \"Remove all tokens from the board\" takes it, because " +
                "her Smoke Bomb's card text says nothing otherwise - which is the whole reason the " +
                "swap below is never offered. " + Remaining(runtime));

            yield return BeginMin(
                root, MinCardContent.ViperStance, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True,
                "\"a unit adjacent to you\". " + Remaining(runtime));
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;

            // MILESTONE 8.86, THE STEP THAT MUST NOT PASS BY ABSENCE. The reading below is that
            // NOTHING is offered, so the Attack it hangs off has to be shown to have happened: the
            // defender answered p.14 with the face they spent, and her own bomb supply is empty for
            // the printed reason rather than because the card never ran.
            Assert.That(
                runtime.Cards.Single(card =>
                    card.OwnerId.Equals(enemy) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "Viper Stance's own Attack reached p.14 and the defender spent the face they " +
                "answered with. " + Remaining(runtime));
            Assert.That(Tokens(runtime), Is.Empty,
                "and the reason the swap has nothing to do is the board: p.8 took her one bomb at " +
                "the round boundary, which the assertion above this one measured. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "With nothing in radius to swap with the branch has nothing to do, so the printed " +
                "`may` is not drawn at all - which is milestone 8.35z15's rule that an offer with " +
                "no legal answer is not an offer. If a later reading keeps her bomb across the " +
                "round boundary this assertion is the one that must fail first. " +
                Remaining(runtime));
            SeatsAgree(runtime, "Viper with no bomb standing");
            Healthy(runtime);
        }

        /// <summary>
        /// Cluster Mine: "Place 3 Mine tokens, 1 blast and 2 duds, facedown in radius; units may move
        /// through them; remove mine tokens after an enemy hero moves through them; for every blast
        /// removed, that hero discards a card, if able."
        ///
        /// <para><b>The blast and the dud are driven on the SAME crossing space</b>, one run each, so
        /// the difference asserted is the kind of mine rather than the route. Her three Blue faces print
        /// their blasts first (MI-Q10d), so the first Place is the blast on one run and a dud stands
        /// there on the other - and only the blast costs the crossing hero a card. Both are removed and
        /// both reveal, which is the half that proves the dud is a real piece rather than a decoration.
        /// </para>
        /// </summary>
        [UnityTest]
        public IEnumerator ClusterMinePlacesOneBlastAndTwoDudsAndOnlyTheBlastCostsACard()
        {
            foreach (var blastOnTheRoute in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedTierTwoEngagedStagedMin,
                    hand: new[] { MinCardContent.ClusterMine, MinCardContent.SmokeBomb },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];
                var crossing = new HexCoordinate(-2, 0);

                yield return BeginMin(
                    root, MinCardContent.ClusterMine, ActionKind.Skill, WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-0"),
                    "The printed order puts her one blast first. " + Remaining(runtime));
                yield return Space(
                    root, blastOnTheRoute ? crossing : new HexCoordinate(-4, 1));
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-1"));
                yield return Space(
                    root, blastOnTheRoute ? new HexCoordinate(-4, 1) : crossing);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-2"));
                yield return Space(root, new HexCoordinate(-3, 0));

                Assert.That(Tokens(runtime), Has.Length.EqualTo(3),
                    "Three copies, which is what the face prints. " + Remaining(runtime));
                Assert.That(
                    TokenKindsInSeat(runtime, her, hers: false)
                        .Distinct(StringComparer.Ordinal).ToArray(),
                    Is.EqualTo(new[] { MinComponentContent.MineFacedown.Value }).AsCollection,
                    "and every one of them is facedown to every seat but hers.");

                var handBefore = HandCountOf(runtime, enemy);
                yield return YieldUntilTheNamedSeatActs(runtime, enemy);
                yield return PointTrayAt(root, enemy);
                yield return StartActionThroughControl(root, ActionKind.Movement);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        Tile(new HexCoordinate(-2, -1)).transform.position),
                    Is.True,
                    "The one two-cost route from (-2,1) to (-2,-1) crosses (-2,0). " +
                    Remaining(runtime));
                yield return null;
                yield return null;
                UiPlayer.Click(
                    UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
                yield return null;

                Assert.That(
                    Tokens(runtime).Any(token => token.Coordinate.Equals(crossing)), Is.False,
                    "\"remove mine tokens after an enemy hero moves through them\" - whichever " +
                    "kind it was. " + Remaining(runtime));
                Assert.That(Tokens(runtime), Has.Length.EqualTo(2),
                    "and the two he never crossed are still standing.");

                if (blastOnTheRoute)
                {
                    var discard = runtime.PendingEffectChoice;
                    Assert.That(discard, Is.Not.Null,
                        "\"for every blast removed, that hero discards a card\". " +
                        Remaining(runtime));
                    Assert.That(discard.ChooserId, Is.EqualTo(enemy));
                    yield return Pick(root, Named(discard.Candidates, "discard"));
                    Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(handBefore - 1));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "A dud removes itself and costs nothing at all, which is the whole of what " +
                        "makes the pile worth hiding. " + Remaining(runtime));
                    Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(handBefore));
                }

                SeatsAgree(runtime, blastOnTheRoute ? "the blast sprung" : "the dud crossed");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Death Grenade: "Place a Grenade token into a space in radius. End of turn: Up to 1 enemy
        /// hero adjacent to that token discards a card, or is defeated. Remove the Grenade token."
        ///
        /// <para><b>Her second cross-seat prompt, and the first that fires at a moment nobody
        /// pressed.</b> The placement is an ordinary board click inside the printed Radius 2; the blast
        /// arrives when her turn ENDS, which is p.17's End-of-turn window rather than a step of the
        /// Skill. "Up to 1" is her own `may`; the hero is her own board click; and the discard is that
        /// hero's own decision on that hero's own seat (MI-Q4).</para>
        ///
        /// <para>Both outcomes of the printed "or is defeated" are driven: a hero holding cards gives
        /// one up, and a hero whose hand the commitment emptied is defeated instead - which is ledger
        /// question 13's reading and Widget's Scorching Breath precedent.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeathGrenadeAsksTheChosenEnemyForACardAtTheEndOfHerTurnOrDefeatsAnEmptyHand()
        {
            foreach (var withCardsInHand in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMin,
                    hand: new[] { MinCardContent.DeathGrenade, MinCardContent.SmokeBomb },
                    enemyHand: withCardsInHand
                        ? new[] { WaspCardContent.Shock, WaspCardContent.Electrocute }
                        : new[] { WaspCardContent.Shock });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginMin(
                    root, MinCardContent.DeathGrenade, ActionKind.Skill, WaspCardContent.Shock);
                var place = runtime.PendingEffectChoice;
                Assert.That(place, Is.Not.Null, Remaining(runtime));
                Assert.That(place.NodeId.Value, Is.EqualTo("from-supply"));
                Assert.That(
                    OfferedSpaces(runtime).All(space =>
                        Distance(space, Position(runtime, her)) <= 2),
                    Is.True,
                    "Every candidate is inside the printed Radius 2. " + Remaining(runtime));
                yield return Space(root, new HexCoordinate(-2, 0));
                Assert.That(
                    Tokens(runtime).Single().Definition.Id, Is.EqualTo(MinComponentContent.GrenadeDefinition.Id));
                Assert.That(
                    runtime.IsActiveEffectSource(
                        CardIdOf(runtime, her, MinCardContent.DeathGrenade)),
                    Is.True,
                    "and the armed End-of-turn effect is readable on the card that raised it. " +
                    Remaining(runtime));

                // Her turn ENDS, which is what the printed sentence waits for. Milestone 8.35g
                // split that into two presses of the same control - the one that finishes the
                // resolution and the one that hands the turn on - so the window is looked for after
                // both, and the second is the release the round strip draws.
                for (var press = 0; press < 3 && runtime.PendingEffectChoice == null; press++)
                {
                    if (runtime.RoundView.Model.EndTurnInvocation != null)
                    {
                        UiPlayer.Click(
                            UiPlayer.Button(
                                root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                    }
                    else if (!UiPlayer.ReleaseFinishedTurn(runtime))
                    {
                        break;
                    }

                    yield return null;
                    yield return null;
                }

                // Milestone 9.1, ruling W3: "Up to 1 enemy hero ... discards" hits the hero there, so
                // the first question is WHICH hero - Min is not asked whether.
                var target = runtime.PendingEffectChoice;
                Assert.That(target, Is.Not.Null,
                    "\"End of turn:\" stops the page before the turn is handed on. " +
                    Remaining(runtime));
                Assert.That(target.NodeId.Value, Is.EqualTo("grenade-first-target"));
                Assert.That(target.ChooserId, Is.EqualTo(her),
                    "p.10 gives Min the choice the text hands nobody else: which hero.");
                Assert.That(target.DeclineInvocation, Is.Null,
                    "W3: a hero beside the Grenade is hit; there is nothing to decline.");
                Assert.That(
                    target.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                    Is.EqualTo(new[] { enemy }).AsCollection,
                    "\"an enemy hero adjacent to that token\" - the one standing beside (-2,0). " +
                    Remaining(runtime));
                yield return Pick(root, target.Candidates.Single());

                if (withCardsInHand)
                {
                    var discard = runtime.PendingEffectChoice;
                    Assert.That(discard, Is.Not.Null, Remaining(runtime));
                    Assert.That(discard.NodeId.Value, Is.EqualTo("grenade-first-discard"));
                    Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                        "\"discards a card\" is that hero's own decision, on their own seat. " +
                        Remaining(runtime));
                    SeatsAgree(runtime, "the grenade's own discard");
                    var heldWhenAsked = HandCountOf(runtime, enemy);
                    yield return Pick(root, Named(discard.Candidates, "discard"));
                    Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(heldWhenAsked - 1),
                        "and it costs exactly one card out of the hand it was asked of. " +
                        Remaining(runtime));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                        "and a hero who could pay is not defeated.");
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "An empty hand has nothing to offer, so no question is asked at all. " +
                        Remaining(runtime));
                    Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False,
                        "\"or is defeated\" - the printed escalation. " + Remaining(runtime));
                }

                Assert.That(Tokens(runtime), Is.Empty,
                    "\"Remove the Grenade token\" - its own text takes it, without waiting for " +
                    "p.8. " + Remaining(runtime));
                SeatsAgree(runtime, "the grenade resolved");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Vanish: "Swap with a Smoke bomb token in range; if you do, block the attack." The same
        /// printed sentence Poof! carries, at Range 4 instead of 3 - and the journey stands the bomb
        /// four spaces off, where Poof!'s own range could not reach it, so the wider number is what is
        /// being measured rather than a second copy of the same reach.
        /// </summary>
        [UnityTest]
        public IEnumerator VanishBlocksTheAttackFromItsPrintedRangeFour()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierTwoEngagedStagedMin,
                hand: new[] { MinCardContent.SmokeBomb, MinCardContent.Vanish },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.SmokeBomb, ActionKind.Skill, WaspCardContent.Shock);
            var far = OfferedSpaces(runtime)
                .Where(space => Distance(space, Position(runtime, her)) == 3)
                .OrderBy(space => space.Q)
                .ThenBy(space => space.R)
                .First();
            yield return Space(root, far);
            Assert.That(Distance(far, Position(runtime, her)), Is.EqualTo(3),
                "Her Silver's printed Radius 3 is the furthest she can throw it, and Poof!'s " +
                "printed Range 3 would still reach that - what Vanish's 4 adds is measured by the " +
                "swap succeeding from her NEW space after p.19 exchanges the two. " +
                Remaining(runtime));

            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(her).transform.position),
                Is.True);
            yield return null;
            Assert.That(
                runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(MinCardContent.Vanish)),
                Is.True,
                "Her Tier II Block is offered as a whole face. " + Remaining(runtime));

            var herSpaceBefore = Position(runtime, her);
            yield return ClickDefenseFace(root, MinCardContent.Vanish);
            yield return null;
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(her)), Is.True,
                "\"you successfully defend, regardless of the attack value\". " +
                Remaining(runtime));
            Assert.That(Position(runtime, her), Is.EqualTo(far));
            Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(herSpaceBefore),
                "and p.19's Swap exchanged the two positions. " + Remaining(runtime));
            SeatsAgree(runtime, "Vanish");
            Healthy(runtime);
        }

        /// <summary>
        /// Inner Strength: "This round: Double your item bonuses."
        ///
        /// <para><b>What a player actually meets is a READING rather than an input</b>, which is what
        /// the derived row says: the face asks nothing and leaves an Active effect standing on her own
        /// rail for the rest of the round. The journey drives a real p.15 Level Up first, because an
        /// item bonus is the only thing this sentence can double and no staging deals one; it then
        /// commits Inner Strength, reads the doubled channel on her own dashboard, and reads it again on
        /// a LATER turn of the same round, which is what makes it the round-long reading and not the
        /// turn-long one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator InnerStrengthDoublesHerItemBonusesForTheWholeRound()
        {
            yield return StageMin(SceneCardContent.VerifiedTierTwoAlternateEngagedStagedMin);
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);

            yield return DriveUntil(
                root,
                () => runtime.SystemTransitionPrompt?.Kind ==
                        SystemTransitionPromptKind.LevelUpUpgrade &&
                    runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(her),
                "her own p.15 Level Up");

            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade),
                "The rounds reached her own p.15 Level Up, which is the only way an item exists. " +
                Remaining(runtime));
            yield return NextDecision(root);
            for (var budget = 0; budget < 80 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextDecision(root);
            }

            var items = runtime.Projection.HeroProgression
                .Single(hero => hero.HeroId.Equals(her))
                .ItemBonuses
                .ToArray();
            Assert.That(items, Is.Not.Empty,
                "p.16's item comes from the sibling the Level Up did not take. " +
                Remaining(runtime));

            var before = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(her))
                .ItemBonuses
                .Select(bonus => bonus.Channel + "=" + bonus.Amount)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            Assert.That(before, Is.Not.Empty,
                "and a player reads it on her own dashboard rail before anything doubles it.");

            yield return BeginMin(root, MinCardContent.InnerStrength, ActionKind.Skill);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"This round: Double your item bonuses\" asks nobody anything. " +
                Remaining(runtime));

            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, her, MinCardContent.InnerStrength)),
                Is.True,
                "and what it leaves behind is a thing a player can see on the card that raised " +
                "it. " + Remaining(runtime));

            // MEASURED at 8.61k: the rail's ItemBonuses row is a COUNT OF ITEMS per channel, and
            // doubling multiplies the BONUS those items are worth rather than the number of them -
            // so the row a player reads is unchanged by construction and the doubling is read as the
            // Active effect above. What the multiplier is worth is owned at the authoritative tier
            // by MinItemAndFlurryTests' ItemBonusMultiplierQuery cases.
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(her))
                    .ItemBonuses
                    .Select(bonus => bonus.Channel + "=" + bonus.Amount)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray(),
                Is.EqualTo(before).AsCollection,
                "and her item row is the same row it was, because what doubled is what an item is " +
                "worth. " + Remaining(runtime));

            var round = runtime.RoundIndex;
            for (var budget = 0; budget < 40 &&
                runtime.RoundIndex == round &&
                runtime.Phase == RoundResolutionPhase.ResolvingTurn; budget++)
            {
                yield return NextDecision(root);
            }

            Assert.That(runtime.RoundIndex, Is.EqualTo(round),
                "Still inside the round her card named. " + Remaining(runtime));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, her, MinCardContent.InnerStrength)),
                Is.True,
                "and the doubling is still standing on a turn that did not raise it, which is what " +
                "makes it p.17's round-long reading rather than p.17's turn-long one. " +
                Remaining(runtime));
            SeatsAgree(runtime, "the doubled items");
            Healthy(runtime);
        }
    }
}
