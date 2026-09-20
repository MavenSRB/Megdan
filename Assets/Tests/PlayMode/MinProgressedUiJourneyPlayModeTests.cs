using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
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
    /// Min the Dragonmonk at rulebook p.15's Tier III stage with p.16's Ultimate unlocked, plus the
    /// session operations every hero owes. Milestone 8.61k.
    ///
    /// <para>Milestone 8.61j's two Tier III arms carry the two ladders: the FIRST sibling of every
    /// colour is Dragon Stance, Mine Field and Ruse, and the SECOND is Cobra Stance, Holy Death Grenade
    /// and Perfect Self. Flurry of Blows is faceup beside her dashboard on both.</para>
    /// </summary>
    public sealed class MinProgressedUiJourneyPlayModeTests
    {
        /// <summary>The p.15 upgrade candidate's own control, addressed the way the HUD names it.</summary>
        private static Button UpgradeControl(
            MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip ==
                    (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        // -------------------------------------------------------------------------------------------
        // p.15 and p.16: the whole upgrade face, and the item the untaken sibling becomes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.15's Level Up is offered as whole printed FACES out of her own deck, and p.16's
        /// item comes from the sibling the Level Up did not take - read on her own dashboard rail
        /// afterwards, which is the only route by which an item of hers ever exists.
        ///
        /// <para>Her three ladders each print an item on one side of every pair (Initiative, Defense,
        /// Attack, Movement, Radius and Range across the twelve upgrade faces), so the candidate this
        /// journey takes is deliberately one carrying an icon: what is being proved is that the CHANNEL
        /// the untaken sibling prints is the channel that rises.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem()
        {
            yield return StageMin(SceneCardContent.VerifiedEngagedStagedMin);
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
                "The rounds reached her own p.15 Level Up. " + Remaining(runtime));
            Assert.That(runtime.Projection.PendingSystemTransitionDescription.HeroId,
                Is.EqualTo(her));

            var options = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .Where(option => option.Card != null)
                .ToArray();
            Assert.That(options, Is.Not.Empty,
                "p.15 offers whole faces rather than anonymous ordinals. " +
                UiPlayer.Describe(root.Hud));
            Assert.That(
                options.All(option => MinCardContent.Deck.Any(id =>
                    runtime.Cards.Single(card => card.Id.Equals(option.Card.CardInstanceId))
                        .DefinitionReference.DefinitionId.Equals(id))),
                Is.True,
                "and every candidate is one of her own thirteen deck cards.");

            var taken = options.First(option => option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(her))
                .ItemBonuses
                .Where(value => value.Key == taken.ItemIcon)
                .Select(value => value.Value)
                .SingleOrDefault();

            UiPlayer.EnsureVisible(UpgradeControl(root, taken));
            yield return null;
            UiPlayer.Click(UpgradeControl(root, taken));
            yield return null;
            yield return null;
            Assert.That(
                runtime.Cards.Single(card => card.Id.Equals(taken.Card.CardInstanceId))
                    .State.Location,
                Is.EqualTo(CardLocation.Hand),
                "The chosen face is the face that reaches her hand. " + Remaining(runtime));

            for (var budget = 0; budget < 80 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextDecision(root);
            }

            var after = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(her))
                .ItemBonuses.Single(value => value.Key == taken.ItemIcon).Value;
            Assert.That(after, Is.EqualTo(before + 1),
                "p.16's item comes from the sibling the Level Up did not take.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(her))
                    .ItemBonuses,
                Is.Not.Empty,
                "and a player reads it on her own dashboard rail.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dragon Stance: "Target a unit adjacent to you. After the attack: You may move 1 or 2 spaces
        /// to a space adjacent to the target. Push an enemy unit adjacent to you up to 3 spaces."
        ///
        /// <para><b>The difference from Tiger Stance is the printed range of the step</b>, and it is the
        /// whole reason this face has a journey of its own: milestone 8.61c composed it as an Optional
        /// wrapper over an UpTo node with a printed floor of ONE, so the player meets a decline FIRST
        /// and a wider set of destinations second. Both are asserted - the declined arm leaves her
        /// exactly where she stood and the push still follows, and the accepted arm reaches a space two
        /// steps away that Tiger's single space could not.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DragonStanceStepsOneOrTwoSpacesBesideTheTargetOrHoldsHerGround()
        {
            foreach (var steps in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                    hand: new[] { MinCardContent.DragonStance, MinCardContent.SmokeBomb },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginMin(
                    root, MinCardContent.DragonStance, ActionKind.Attack, WaspCardContent.Shock);
                Assert.That(
                    UiPlayer.ClickBoard(
                        Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        root.Hud,
                        HeroView(enemy).transform.position),
                    Is.True);
                yield return null;
                yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                yield return null;
                yield return AnswerAnyUltimateRepeat(root, false);

                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null,
                    "\"You may move 1 or 2 spaces\" stops the page. " + Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("dragon-move-may"));
                Assert.That(may.DeclineInvocation, Is.Not.Null);

                var stood = Position(runtime, her);
                if (!steps)
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                    Assert.That(Position(runtime, her), Is.EqualTo(stood),
                        "A declined `may` moves nobody. " + Remaining(runtime));
                }
                else
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
                    yield return null;
                    yield return null;
                    var step = runtime.PendingEffectChoice;
                    Assert.That(step.NodeId.Value, Is.EqualTo("dragon-move-step"));
                    var candidates = OfferedSpaces(runtime);
                    Assert.That(candidates, Is.Not.Empty, Remaining(runtime));
                    Assert.That(
                        candidates.All(space =>
                            Distance(space, Position(runtime, enemy)) == 1 &&
                            Distance(space, stood) >= 1 && Distance(space, stood) <= 2),
                        Is.True,
                        "MI-Q11c: every candidate is adjacent to the space the Attack BOUND and " +
                        "within the printed one-or-two. " + Remaining(runtime));
                    // MEASURED at milestone 8.61k: on this staging the printed "1 or 2" admits
                    // exactly (-2,0), because a destination has to be BOTH inside the printed
                    // distance AND adjacent to the space the Attack bound, and the target's other
                    // free neighbours are two steps away through spaces the board does not leave
                    // open. The widening itself is owned at the authoritative tier by
                    // MinStanceTests; what this journey proves is that the offer is real, that it
                    // is measured from the bound space, and that it can be taken.
                    var taken = candidates.First();
                    yield return Space(root, taken);
                    Assert.That(Position(runtime, her), Is.EqualTo(taken));
                    Assert.That(Distance(Position(runtime, her), Position(runtime, enemy)),
                        Is.EqualTo(1));
                }

                var subject = runtime.PendingEffectChoice;
                Assert.That(subject, Is.Not.Null,
                    "and the push follows either answer - MI-Q12c prints it last and " +
                    "unconditionally. " + Remaining(runtime));
                Assert.That(subject.NodeId.Value, Is.EqualTo("dragon-push-subject"));
                yield return Pick(root, subject.Candidates.First());
                Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo("dragon-push-push"));
                yield return PressAmount(root, 1);
                SeatsAgree(runtime, steps ? "Dragon's two-space step" : "Dragon's declined step");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Cobra Stance: "Target a unit adjacent to you. After the attack: You may swap with a Smoke
        /// bomb in radius; if you do, you may place the Smoke bomb into a space in radius."
        ///
        /// <para><b>MI-Q13k again, and for the same measured reason Viper Stance carries it.</b> See
        /// <c>MinEngagedUiJourneyPlayModeTests.ViperStanceSwapsWithTheSmokeBombInRadiusOrStaysWhereSheIs</c>:
        /// her one bomb is removed by p.8 at the end of the round her Silver placed it in, and she takes
        /// one turn per round, so no legal sequence stands a bomb on the board on a turn a Stance of
        /// hers is the card she committed. This journey measures exactly that and proves the reachable
        /// half.</para>
        ///
        /// <para><b>Ruse's own re-place proves the sentence Cobra shares with it</b>, from the Defence
        /// side where a bomb genuinely is standing - see
        /// <see cref="RuseThrowsTheSmokeBombAgainAfterTheBlockOrLeavesItStanding"/>, whose
        /// <c>vanishing-replace-may</c> is the same printed "you may place the Smoke bomb" with the same
        /// board click behind it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CobraStanceSwapsWithTheSmokeBombThenThrowsItAgainOrLeavesIt()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMin,
                hand: new[] { MinCardContent.SmokeBomb, MinCardContent.CobraStance },
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
                "Her bomb is on the board, inside Cobra's printed Radius 4. " + Remaining(runtime));

            yield return DriveUntil(
                root, () => runtime.RoundIndex > round, "the next round");
            Assert.That(Tokens(runtime), Is.Empty,
                "MI-Q13k: p.8 takes it at the round boundary. " + Remaining(runtime));

            yield return BeginMin(
                root, MinCardContent.CobraStance, ActionKind.Attack, WaspCardContent.Shock);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return AnswerAnyUltimateRepeat(root, false);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "With nothing in radius the swap has nothing to do and the printed `may` is not " +
                "drawn - and MI-Q10c's nesting means the re-place beside it is not either. If a " +
                "later reading keeps her bomb across the round boundary this assertion fails " +
                "first. " + Remaining(runtime));
            SeatsAgree(runtime, "Cobra with no bomb standing");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Mine Field: "Place 3 Mine tokens, 2 blasts and 1 dud, facedown in radius ... for every blast
        /// removed, that hero discards a card, if able."
        ///
        /// <para><b>It is the only face of hers that can spring TWO blasts on one crossing</b>, which is
        /// what this journey drives: both blasts stand on the one forced two-cost route between (-2,1)
        /// and (-2,-1)... except that route has only one intermediate space, so the second blast is put
        /// on the destination's far side and the walk continues over it. "For EVERY blast removed" then
        /// asks twice, and the second question is asked only after the first is answered.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MineFieldPlacesTwoBlastsAndOneDudAndEveryBlastCostsACard()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.MineField, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.MineField, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-0"));
            yield return Space(root, new HexCoordinate(-2, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-1"),
                "Two blasts, asked for one at a time and both before the dud. " + Remaining(runtime));
            yield return Space(root, new HexCoordinate(-3, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-2"));
            yield return Space(root, new HexCoordinate(-4, 1));

            Assert.That(Tokens(runtime), Has.Length.EqualTo(3));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: true)
                    .Count(value => value == MinComponentContent.MineBlast.Value),
                Is.EqualTo(2),
                "Two blasts and one dud, as the face prints them - and only she can tell. " +
                Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: false)
                    .Distinct(StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { MinComponentContent.MineFacedown.Value }).AsCollection);

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
                Remaining(runtime));
            yield return null;
            yield return null;
            UiPlayer.Click(
                UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return null;

            var first = runtime.PendingEffectChoice;
            Assert.That(first, Is.Not.Null,
                "The blast he crossed asks him for a card. " + Remaining(runtime));
            Assert.That(first.ChooserId, Is.EqualTo(enemy));
            yield return Pick(root, first.Candidates.First());
            Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(handBefore - 1));
            Assert.That(
                Tokens(runtime).Any(token => token.Coordinate.Equals(new HexCoordinate(-2, 0))),
                Is.False,
                "and the blast is gone. " + Remaining(runtime));
            SeatsAgree(runtime, "Mine Field's blast");
            Healthy(runtime);
        }

        /// <summary>
        /// Holy Death Grenade: "Place a Grenade token into a space in radius. End of turn: Up to 2
        /// enemy heroes adjacent to that token discard a card, or are defeated. Remove the Grenade
        /// token."
        ///
        /// <para><b>The only face of hers that asks the same question twice</b>, and the second pass
        /// excludes the hero the first one named. Her staged match is one seat a side, so the printed
        /// "up to 2" meets exactly one eligible hero: the first pass takes him and the second is
        /// offered and STOPPED - which is the printed decline, on the only board a configured match of
        /// hers has. The two-hero case is owned at the authoritative tier by <c>MinGrenadeTests</c>,
        /// and milestone 8.61k's report records that a second adjacent enemy hero needs a line-up this
        /// staging does not deal.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HolyDeathGrenadeTakesTwoEnemyHeroesOrStopsAtOne()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMin,
                hand: new[] { MinCardContent.HolyDeathGrenade, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.HolyDeathGrenade, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("from-supply"),
                Remaining(runtime));
            yield return Space(root, new HexCoordinate(-2, 0));
            Assert.That(
                Tokens(runtime).Single().Definition.Id,
                Is.EqualTo(MinComponentContent.GrenadeDefinition.Id));

            for (var press = 0; press < 3 && runtime.PendingEffectChoice == null; press++)
            {
                if (runtime.RoundView.Model.EndTurnInvocation != null)
                {
                    UiPlayer.Click(
                        UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }
                else if (!UiPlayer.ReleaseFinishedTurn(runtime))
                {
                    break;
                }

                yield return null;
                yield return null;
            }

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "\"End of turn:\" stops the page before the turn is handed on. " +
                Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("holy-grenade-first-may"), Remaining(runtime));
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return null;

            var firstTarget = runtime.PendingEffectChoice;
            Assert.That(firstTarget.NodeId.Value, Is.EqualTo("holy-grenade-first-target"));
            Assert.That(
                firstTarget.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "\"an enemy hero adjacent to that token\" - the one standing beside (-2,0). " +
                Remaining(runtime));
            yield return Pick(root, firstTarget.Candidates.Single());

            var firstDiscard = runtime.PendingEffectChoice;
            Assert.That(firstDiscard.NodeId.Value, Is.EqualTo("holy-grenade-first-discard"));
            Assert.That(firstDiscard.ChooserId, Is.EqualTo(enemy),
                "The chosen hero's own card, on the chosen hero's own seat. " + Remaining(runtime));
            SeatsAgree(runtime, "the first grenade discard");
            var heldWhenAsked = HandCountOf(runtime, enemy);
            yield return Pick(root, firstDiscard.Candidates.First());
            Assert.That(HandCountOf(runtime, enemy), Is.EqualTo(heldWhenAsked - 1),
                "and it costs exactly one card out of the hand it was asked of. " +
                Remaining(runtime));

            var second = runtime.PendingEffectChoice;
            if (second != null)
            {
                Assert.That(second.NodeId.Value, Is.EqualTo("holy-grenade-second-may"),
                    "\"Up to 2\" asks again. " + Remaining(runtime));
                Assert.That(second.DeclineInvocation, Is.Not.Null,
                    "and the second pass can be stopped at one.");
                UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                yield return null;
                yield return null;
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            }

            Assert.That(Tokens(runtime), Is.Empty,
                "\"Remove the Grenade token\", either way. " + Remaining(runtime));
            SeatsAgree(runtime, "the holy grenade resolved");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Ruse: "Swap with a Smoke bomb token in range; if you do, block the attack. You may place the
        /// Smoke bomb into a space in range."
        ///
        /// <para><b>The one re-place a configured match can reach</b>, and the reason is MI-Q13k: a
        /// Block is discarded from hand on somebody else's turn, so the bomb her Silver placed earlier
        /// in the SAME round is still standing when the attack arrives. MI-Q11b's reading is driven on
        /// both answers - the re-place accepted throws the bomb again from her NEW space, and declined
        /// leaves it exactly where the swap put it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator RuseThrowsTheSmokeBombAgainAfterTheBlockOrLeavesItStanding()
        {
            foreach (var rethrows in new[] { true, false })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                    hand: new[] { MinCardContent.SmokeBomb, MinCardContent.Ruse },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginMin(
                    root, MinCardContent.SmokeBomb, ActionKind.Skill, WaspCardContent.Shock);
                var bombAt = new HexCoordinate(-4, 1);
                yield return Space(root, bombAt);
                var herSpaceBefore = Position(runtime, her);

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
                yield return ClickDefenseFace(root, MinCardContent.Ruse);
                yield return null;
                yield return null;

                Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(her)), Is.True,
                    "The swap succeeded, so the attack is blocked. " + Remaining(runtime));
                Assert.That(Position(runtime, her), Is.EqualTo(bombAt));

                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null,
                    "\"You may place the Smoke bomb into a space in range\" - MI-Q11b's gated " +
                    "third sentence. " + Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("vanishing-replace-may"));
                Assert.That(may.DeclineInvocation, Is.Not.Null);

                if (!rethrows)
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                    Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(herSpaceBefore),
                        "A declined re-place leaves the bomb where the swap put it. " +
                        Remaining(runtime));
                }
                else
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
                    yield return null;
                    yield return null;
                    var place = runtime.PendingEffectChoice;
                    Assert.That(place.NodeId.Value, Is.EqualTo("vanishing-replace-place"));
                    Assert.That(
                        OfferedSpaces(runtime).All(space =>
                            Distance(space, Position(runtime, her)) <= 4),
                        Is.True,
                        "and the printed Range 4 is measured from where she stands NOW. " +
                        Remaining(runtime));
                    var thrown = OfferedSpaces(runtime).First(space =>
                        !space.Equals(herSpaceBefore));
                    yield return Space(root, thrown);
                    Assert.That(Tokens(runtime).Single().Coordinate, Is.EqualTo(thrown),
                        "One physical bomb, moved - not a second one drawn from a supply of one. " +
                        Remaining(runtime));
                }

                SeatsAgree(runtime, rethrows ? "Ruse re-placed" : "Ruse declined the re-place");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Perfect Self: "Choose one or both - (1) This round: Double your item bonuses. (2) Take a
        /// Tier II card from your deck and add it to your dashboard as a permanent item."
        ///
        /// <para><b>p.10's modal form with a ceiling of two rather than one</b>, which is the shape
        /// milestone 8.61h composed and this journey presses: the bullet control is pressed once and the
        /// chosen sentence runs, and on the third run BOTH are pressed and both run. The deck-card
        /// bullet is the first card selection in the build whose candidates live in a DECK, and the
        /// privacy of that is asserted from a seat that must not see them (milestone 8.40k's rule).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PerfectSelfTakesEitherBulletOrBothAndTheDeckCardBecomesAPermanentItem()
        {
            foreach (var mode in new[] { "double", "take", "both" })
            {
                yield return StageMin(
                    SceneCardContent.VerifiedTierThreeAlternateEngagedStagedMin,
                    hand: new[] { MinCardContent.PerfectSelf, MinCardContent.SmokeBomb },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var her = Min(runtime);

                yield return BeginMin(
                    root, MinCardContent.PerfectSelf, ActionKind.Skill, WaspCardContent.Shock);
                var choose = runtime.PendingEffectChoice;
                Assert.That(choose, Is.Not.Null,
                    "\"Choose one or both\" opens p.10's modal. " + Remaining(runtime));
                Assert.That(choose.NodeId.Value, Is.EqualTo("perfect-self-mode"));
                Assert.That(choose.MinimumSelections, Is.EqualTo(1));
                Assert.That(choose.MaximumSelections, Is.EqualTo(2),
                    "\"one or both\" is a ceiling of two, not two separate cards. " +
                    Remaining(runtime));

                if (mode != "take")
                {
                    UiPlayer.ClickEffectOption(runtime, root.Hud, "perfect-self-double");
                    yield return null;
                    yield return null;
                }

                if (mode != "double")
                {
                    UiPlayer.ClickEffectOption(runtime, root.Hud, "perfect-self-take");
                    yield return null;
                    yield return null;
                }

                // Picking a bullet leaves p.10's modal standing with its own confirm, which is
                // Xargatha's measured shape: "one or both" cannot close itself at one selection
                // because its ceiling is two, so the chosen sentences do not run until the DONE is
                // pressed. That press is what turns a selection into an answer here.
                yield return ConfirmIfStillStanding(root, new EffectNodeId("perfect-self-mode"));
                yield return null;
                yield return null;

                if (mode == "double")
                {
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "Bullet 1 asks nothing of its own. " + Remaining(runtime));
                    Assert.That(
                        runtime.IsActiveEffectSource(
                            CardIdOf(runtime, her, MinCardContent.PerfectSelf)),
                        Is.True,
                        "Bullet 1 alone leaves the round-long doubling standing on the card that " +
                        "raised it. " + Remaining(runtime));
                    Healthy(runtime);
                    continue;
                }

                var deckCard = runtime.PendingEffectChoice;
                Assert.That(deckCard, Is.Not.Null,
                    "Bullet 2 asks which Tier II card. " + Remaining(runtime));
                Assert.That(deckCard.NodeId.Value, Is.EqualTo("perfect-self-take"));
                Assert.That(
                    deckCard.Candidates.All(candidate => runtime.Cards
                        .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                        .State.Location == CardLocation.Deck),
                    Is.True,
                    "MI-Q5: every candidate is a card in her own DECK zone. " + Remaining(runtime));
                Assert.That(
                    runtime.Host.Seats.Seats
                        .Where(seat => !seat.Owns(her))
                        .SelectMany(seat => runtime.Host.ProjectionForSeat(seat.Id).Cards)
                        .Any(card => deckCard.Candidates.Any(candidate =>
                            candidate.Value.CardInstanceId.Equals(card.Id) &&
                            card.DefinitionReference.DefinitionId.IsDefined)),
                    Is.False,
                    "and no other seat can name what she is choosing between - the privacy of a " +
                    "card choice is a projection property, which is milestone 8.40k's rule. " +
                    Remaining(runtime));

                var taken = deckCard.Candidates.First();
                var takenId = taken.Value.CardInstanceId;
                yield return Pick(root, taken);
                Assert.That(
                    runtime.Cards.Single(card => card.Id.Equals(takenId)).State.Location,
                    Is.Not.EqualTo(CardLocation.Deck),
                    "\"add it to your dashboard as a permanent item\" takes it out of the deck. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.HeroOverviewsView.Model.Heroes
                        .Single(hero => hero.HeroId.Equals(her)).ItemBonuses,
                    Is.Not.Empty,
                    "and a player reads the item it became on her own dashboard rail. " +
                    Remaining(runtime));
                SeatsAgree(runtime, "Perfect Self, " + mode);
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Flurry of Blows: "Each time after you perform an attack action you may repeat it once on a
        /// different target."
        ///
        /// <para><b>MI-Q9, driven on the surface.</b> The Ultimate is faceup on her own rail at Tier
        /// III, its trigger stops the game after her Attack resolves, the repeat takes a target the
        /// first instance did not, the Stance's own after-attack rider runs on BOTH instances, and the
        /// repeated instance never triggers a third - p.10: "You cannot repeat a repeated action".</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FlurryOfBlowsRepeatsHerAttackOnceOnADifferentTargetAndNeverAThird()
        {
            // The enemy ranged minion is stood at (-2,0) rather than at the engagement's own
            // (-4,2): it has to be ADJACENT to her for the repeat to have a legal different target,
            // and it has to be drawn where a pointer can reach it - the HUD covers the band the
            // engagement's own space is painted in at the validated 1920x1080 viewport, which the
            // first Unity run of this journey measured.
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.DragonStance, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(
                        new MinionId("red-ranged-01"), new HexCoordinate(-2, 0))
                });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(her)).UltimateCard,
                Is.Not.Null,
                "p.16 turns the unlocked Ultimate faceup where a player can read it. " +
                Remaining(runtime));

            yield return BeginMin(
                root, MinCardContent.DragonStance, ActionKind.Attack, WaspCardContent.Shock);
            var firstTargets = runtime.PendingAttackTargetChoice;
            Assert.That(firstTargets.LegalTargetMinionIds, Is.Not.Empty,
                "Arrange: an adjacent enemy minion stands beside her as well as the hero, so the " +
                "repeat has a DIFFERENT target to take. " + Remaining(runtime));
            Assert.That(firstTargets.LegalTargetHeroIds, Contains.Item(enemy));

            // The MINION is taken first and the hero is left for the repeat, which is the order the
            // viewport allows: the pieces standing in the engagement's own lower band are drawn
            // under the HUD at the validated 1920x1080 viewport, and a target a player could not
            // click is not one this journey may pretend to click either.
            var minion = firstTargets.LegalTargetMinionIds.First();
            // Its space is read BEFORE the click, because an assertion's message is evaluated
            // eagerly and a minion her printed Attack 4 defeats has no view left to ask.
            var minionSpace = MinionView(minion).Occupancy.Coordinate;
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(minion).transform.position),
                Is.True,
                "Her first instance takes the adjacent enemy minion at " + minionSpace + ". " +
                Remaining(runtime));
            yield return null;
            yield return null;

            // Dragon's own rider is answered out of the way - which of it and the Ultimate's trigger
            // a player meets first is the resolver's ordering rather than this journey's subject.
            for (var budget = 0; budget < 8; budget++)
            {
                var standing = runtime.PendingEffectChoice;
                if (standing == null || standing.NodeId.Value == "flurry-of-blows-repeat")
                {
                    break;
                }

                if (standing.DeclineInvocation != null)
                {
                    UiPlayer.ClickEffectControl(
                        runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                }
                else if (standing.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Integer))
                {
                    yield return PressAmount(root, 0);
                }
                else
                {
                    var preferred = standing.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero)
                        ? standing.Candidates.First(candidate =>
                            candidate.Value.Kind == EffectValueKind.Hero)
                        : standing.Candidates.First();
                    yield return Pick(root, preferred);
                }
            }

            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "Her Ultimate's own trigger stops the game. " + Remaining(runtime));
            Assert.That(repeat.NodeId.Value, Is.EqualTo("flurry-of-blows-repeat"));
            Assert.That(repeat.DeclineInvocation, Is.Not.Null,
                "\"you MAY repeat it\". " + Remaining(runtime));
            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return null;

            var second = runtime.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "and the repeated attack asks for its own target. " + Remaining(runtime));
            Assert.That(second.LegalTargetMinionIds.Contains(minion), Is.False,
                "\"on a DIFFERENT target\" - the piece the first instance took is not offered " +
                "again. " + Remaining(runtime));
            Assert.That(second.LegalTargetHeroIds, Contains.Item(enemy),
                "and the adjacent enemy hero is still a target it may take. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True,
                "and the repeat's own target is answered on the board. " + Remaining(runtime));
            yield return null;
            yield return null;

            for (var budget = 0; budget < 12 && runtime.PendingEffectChoice != null; budget++)
            {
                var standing = runtime.PendingEffectChoice;
                Assert.That(standing.NodeId.Value, Is.Not.EqualTo("flurry-of-blows-repeat"),
                    "p.10: \"You cannot repeat a repeated action\" - the second instance never " +
                    "triggers a third. " + Remaining(runtime));
                if (standing.DeclineInvocation != null)
                {
                    UiPlayer.ClickEffectControl(
                        runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                }
                else if (standing.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Integer))
                {
                    yield return PressAmount(root, 0);
                }
                else
                {
                    var preferred = standing.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero)
                        ? standing.Candidates.First(candidate =>
                            candidate.Value.Kind == EffectValueKind.Hero)
                        : standing.Candidates.First();
                    yield return Pick(root, preferred);
                }
            }

            SeatsAgree(runtime, "the repeated attack");
            Healthy(runtime);
        }

        /// <summary>Answers her Ultimate's repeat where a Tier III staging has already unlocked it.</summary>
        private static IEnumerator AnswerAnyUltimateRepeat(
            MatchSceneCompositionRoot root, bool accept)
        {
            var runtime = root.Runtime;
            if (runtime.PendingEffectChoice == null ||
                runtime.PendingEffectChoice.NodeId.Value != "flurry-of-blows-repeat")
            {
                yield break;
            }

            UiPlayer.ClickEffectControl(
                runtime, root.Hud,
                accept ? EffectChoiceCommandKind.Confirm : EffectChoiceCommandKind.Decline);
            yield return null;
            yield return null;
        }

        // -------------------------------------------------------------------------------------------
        // The session half every hero owes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.13's Defeat takes her piece off the board and p.19's next sentence leaves her
        /// tokens exactly where they stand - "Tokens are not removed when the hero who placed them is
        /// defeated" - p.13's respawn puts the piece back on a space a player clicks, and the Director's
        /// RESET rebuilds the whole session from its initial state.
        /// </summary>
        [UnityTest]
        public IEnumerator HerDefeatedPieceRespawnsWithHerMinesStandingAndRestartRebuildsTheSession()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.MineField, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.MineField, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-5, 1));
            yield return Space(root, new HexCoordinate(-5, 2));
            yield return Space(root, new HexCoordinate(-5, 3));
            var placed = Tokens(runtime).Select(token => token.Id.Value).OrderBy(
                value => value, StringComparer.Ordinal).ToArray();
            Assert.That(placed, Has.Length.EqualTo(3), Remaining(runtime));

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
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(her)), Is.False,
                "An undefended Attack defeats her and the piece leaves the board. " +
                Remaining(runtime));
            Assert.That(
                Tokens(runtime).Select(token => token.Id.Value)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(placed).AsCollection,
                "p.19: \"Tokens are not removed when the hero who placed them is defeated\". " +
                Remaining(runtime));
            SeatsAgree(runtime, "her own defeat");

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, her, MinCardContent.SmokeBomb);
            yield return YieldUntilTheNamedSeatActs(runtime, her);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(destination).transform.position),
                Is.True,
                "p.13's respawn is answered by clicking the space. " + Remaining(runtime));
            yield return null;
            Assert.That(Position(runtime, her), Is.EqualTo(destination));
            Assert.That(HeroView(her).Coordinate, Is.EqualTo(destination),
                "and the rebuilt piece is drawn where the authority put it.");
            Healthy(runtime);

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "RESET returns the initial state with no accepted history. " + Remaining(runtime));
            Assert.That(Tokens(runtime), Is.Empty,
                "and a rebuilt board holds no token anybody placed.");
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                    .Count(view => view.Occupancy.HeroId.Equals(her)),
                Is.EqualTo(1),
                "One hero, one piece - a rebuild that leaves a stale view behind fails here.");
            Assert.That(
                runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(her))
                    .TrayCards,
                Is.Not.Empty,
                "and her tray is reconstructed rather than left blank. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// Her own pause and the OPPONENT's own pause both survive the Director's retained-prefix Undo,
        /// a replay of that prefix, and a snapshot restore - and are answered afterwards on the same
        /// surface.
        ///
        /// <para><b>They are the hard case on purpose.</b> Mine Field's second placement is hers and the
        /// mine trap's discard belongs to the hero who walked over the blast, so what has to come back
        /// is not only the question but whose question it is; a restore that re-derived the chooser from
        /// the acting seat would look identical from her side of the table. The Undo is taken on the
        /// LIVE session before any capture, because <c>MatchSession.RestoreSnapshot</c> clears the
        /// accepted history.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerOwnPlacementUndoesAndTheOpponentsPauseSurvivesTheReplay()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.MineField, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginMin(
                root, MinCardContent.MineField, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-2, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-1"));
            var offered = OfferedSpaces(runtime);

            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-0"),
                "One press gives the first placement back unanswered - 8.46b made a pick of " +
                "exactly one ONE accepted command. " + Remaining(runtime));
            Assert.That(Tokens(runtime), Is.Empty,
                "and the mine it had already put on the board is off it again.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            yield return Space(root, new HexCoordinate(-2, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-1"),
                "Replaying the retained prefix reproduces the same pause. " + Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there.");

            yield return Space(root, new HexCoordinate(-3, 0));
            yield return Space(root, new HexCoordinate(-4, 1));
            Assert.That(Tokens(runtime), Has.Length.EqualTo(3));

            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(new HexCoordinate(-2, -1)).transform.position),
                Is.True,
                "His own walk crosses the blast at (-2,0). " + Remaining(runtime));
            yield return null;
            yield return null;
            UiPlayer.Click(
                UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
            yield return null;
            yield return null;

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null, Remaining(runtime));
            Assert.That(discard.ChooserId, Is.EqualTo(enemy),
                "The cross-seat half of her content survives the replayed prefix too: the hero who " +
                "walked over the blast is still the one being asked. " + Remaining(runtime));
            SeatsAgree(runtime, "the opponent's own mine discard after the replay");
            yield return Pick(root, discard.Candidates.First());
            Healthy(runtime);
        }

        /// <summary>
        /// A pause of hers survives a snapshot restore and is answered afterwards on the same
        /// surface, with the authority's own facedown kinds intact on the other side of it.
        ///
        /// <para>The pause is her SECOND mine placement, which is the one her content makes hardest
        /// to bring back: a restore that rebuilt the board from the masked projection rather than
        /// from the authority would hand her a blast she can no longer tell from a dud, and nothing
        /// on her own screen would say so.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerOwnMinePauseSurvivesASnapshotRestore()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.MineField, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);

            yield return BeginMin(
                root, MinCardContent.MineField, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-2, 0));
            var standing = runtime.PendingEffectChoice;
            Assert.That(standing.NodeId.Value, Is.EqualTo("place-mine-1"), Remaining(runtime));
            var offered = OfferedSpaces(runtime);
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            yield return Space(root, new HexCoordinate(-3, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-2"));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The captured pause restores. " + Remaining(runtime));
            yield return null;
            yield return null;
            var restored = runtime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(restored.NodeId.Value, Is.EqualTo("place-mine-1"));
            Assert.That(restored.ChooserId, Is.EqualTo(her));
            Assert.That(OfferedSpaces(runtime), Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there. " + Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: true),
                Is.EqualTo(new[] { MinComponentContent.MineBlast.Value }).AsCollection,
                "and the ONE mine already down is still a blast to her - a restore that rebuilt " +
                "the board from her opponents' masked view would hand her a piece she could no " +
                "longer tell from a dud. " + Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: false),
                Is.EqualTo(new[] { MinComponentContent.MineFacedown.Value }).AsCollection,
                "and it is still facedown to everybody else.");
            SeatsAgree(runtime, "the restored placement");
            yield return Space(root, new HexCoordinate(-3, 0));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("place-mine-2"),
                "and answering it on the same surface carries on. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// Rulebook p.8's "Remove all tokens from the board", read on the board a player is looking at:
        /// her three mines stand through the rest of the round they were placed in and are gone at the
        /// next commitment. It is the other end of the lifetime MI-Q13k names, and the reason every
        /// bomb-consuming face of hers has to be reached inside one round.
        /// </summary>
        [UnityTest]
        public IEnumerator EveryTokenOfHersIsRemovedAtTheEndOfTheRound()
        {
            yield return StageMin(
                SceneCardContent.VerifiedTierThreeEngagedStagedMin,
                hand: new[] { MinCardContent.MineField, MinCardContent.SmokeBomb },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var root = Root();
            var runtime = root.Runtime;
            var her = Min(runtime);
            var round = runtime.RoundIndex;

            yield return BeginMin(
                root, MinCardContent.MineField, ActionKind.Skill, WaspCardContent.Shock);
            yield return Space(root, new HexCoordinate(-5, 1));
            yield return Space(root, new HexCoordinate(-5, 2));
            yield return Space(root, new HexCoordinate(-5, 3));
            Assert.That(Tokens(runtime), Has.Length.EqualTo(3), Remaining(runtime));
            Assert.That(
                TokenKindsInSeat(runtime, her, hers: false)
                    .Distinct(StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { MinComponentContent.MineFacedown.Value }).AsCollection,
                "Three pieces every other seat can see and none of them can identify. " +
                Remaining(runtime));

            yield return DriveUntil(
                root, () => runtime.RoundIndex > round, "the next round");
            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1), Remaining(runtime));
            Assert.That(Tokens(runtime), Is.Empty,
                "p.8's \"Remove all tokens from the board\", and none of her card texts says " +
                "otherwise. " + Remaining(runtime));
            Assert.That(
                runtime.BoardView.TokenViews.Count(view => view.gameObject.activeSelf), Is.Zero,
                "and the board stops drawing them, which is what a player reads.");
            SeatsAgree(runtime, "the round boundary");
            Healthy(runtime);
        }
    }
}
