using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
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
using static GuardsDigital.Tests.PlayMode.HanuEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.HanuUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Hanu the Trickster at rulebook p.15's Tier III stage with p.16's The Ultimate Trick unlocked,
    /// plus the session operations every hero owes. Milestone 8.71i.
    ///
    /// <para>Milestone 8.71h's two Tier III arms carry the two ladders: the FIRST sibling of every
    /// colour is Trusted Sidekick, Safe Travels and Monkey Business, and the SECOND is Pile On, That
    /// Way! and See Nothing. The Ultimate is faceup beside his dashboard on both, and - since milestone
    /// 8.71i - on the Tier III ALLY arm too, which is the only board a hero he may Hurry up! on his own
    /// SIDE stands on.</para>
    ///
    /// <para><b>THE ULTIMATE IS THE ONE FACE OF HIS THAT CHANGES WHO ANSWERS.</b> "You choose the next
    /// action, and how it is performed for a hero you target with the 'Hurry Up!'." Nothing is stored:
    /// <c>DelegatedActionQuery</c> reads the rewrite in the company of the unlocked Ultimate, so the
    /// delegation is visible to every seat and lapses exactly where the rewrite does. What a journey
    /// therefore has to show is not a flag but a SEAT: the targeted player's own tray is dark and holds
    /// no start command at all, Hanu's is lit and holds theirs, and every choice inside that action is
    /// answered from his side of the table.</para>
    /// </summary>
    public sealed class HanuProgressedUiJourneyPlayModeTests
    {
        /// <summary>The p.15 upgrade candidate's own control, addressed the way the HUD names it.</summary>
        private static Button UpgradeControl(
            MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip ==
                    (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        /// <summary>Every start command a seat may actually submit right now.</summary>
        private static DirectorCommand[] StartCommandsOf(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Host.ProjectionForSeat(SeatOf(runtime, heroId)).AuthorizedCommands
                .Where(command => command.Kind == DirectorCommandKind.StartAction)
                .ToArray();

        /// <summary>Whether this seat may answer the standing effect choice.</summary>
        private static bool MayAnswerTheStandingChoice(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Host.ProjectionForSeat(SeatOf(runtime, heroId)).AuthorizedCommands
                .Any(command => command.Kind == DirectorCommandKind.AnswerEffectChoice);

        /// <summary>
        /// Whether the card whose printed Initiative Hurry up! rewrote still carries the rewrite - the
        /// one fact the delegation is derived from, so its absence is the delegation's own lapse.
        /// </summary>
        private static bool RewriteStandsOn(
            ConfiguredMatchSceneRuntime runtime, HeroId heroId, CardDefinitionId face) =>
            runtime.Cards
                .Single(card => card.OwnerId.Equals(heroId) &&
                    card.DefinitionReference.DefinitionId.Equals(face))
                .InitiativeOverride.IsDefined;

        // -------------------------------------------------------------------------------------------
        // p.15 and p.16: the whole upgrade face, and the item the untaken sibling becomes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.15's Level Up is offered as whole printed FACES out of his own deck, and p.16's
        /// item comes from the sibling the Level Up did not take - read on his own dashboard rail
        /// afterwards, which is the only route by which an item of his ever exists.
        ///
        /// <para>His twelve upgrade faces print six channels between them (Initiative, Defense and
        /// Attack three times each, and Radius, Range and Movement once), so the candidate this journey
        /// takes is deliberately one whose SIBLING carries an icon: what is being proved is that the
        /// channel the untaken sibling prints is the channel that rises.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem()
        {
            yield return StageHanu(SceneCardContent.VerifiedEngagedStagedHanu);
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);

            yield return DriveUntil(
                root,
                () => runtime.SystemTransitionPrompt?.Kind ==
                        SystemTransitionPromptKind.LevelUpUpgrade &&
                    runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(his),
                "his own p.15 Level Up");

            var options = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .Where(option => option.Card != null)
                .ToArray();
            Assert.That(options, Is.Not.Empty,
                "p.15 offers whole faces rather than anonymous ordinals. " +
                UiPlayer.Describe(root.Hud));
            Assert.That(
                options.All(option => HanuCardContent.Deck.Any(id =>
                    runtime.Cards.Single(card => card.Id.Equals(option.Card.CardInstanceId))
                        .DefinitionReference.DefinitionId.Equals(id))),
                Is.True,
                "and every candidate is one of his own thirteen deck cards.");

            var taken = options.First(option => option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(his))
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
                "The chosen face is the face that reaches his hand. " + Remaining(runtime));

            for (var budget = 0; budget < 60 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextDecision(root);
            }

            var after = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(his))
                .ItemBonuses.Single(value => value.Key == taken.ItemIcon).Value;
            Assert.That(after, Is.EqualTo(before + 1),
                "p.16's item comes from the sibling the Level Up did not take.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(his))
                    .ItemBonuses,
                Is.Not.Empty,
                "and a player reads it on his own dashboard rail.");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier III: the modal at one OR BOTH bullets
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Trusted Sidekick: "Choose one, or both, in any order - • Target a unit adjacent to you. •
        /// Target a hero in range, adjacent to your friendly hero and not adjacent to you."
        ///
        /// <para><b>Both bullets, and both orders.</b> The modal's ceiling is two and a
        /// <c>Choose</c> whose second Select fills its ceiling closes with no confirm (Bain's 8.40k
        /// lesson), so the two presses ARE the answer; the order they are pressed in is the order the
        /// two complete p.14 sequences run in, which is what "in any order" prints. The journey runs it
        /// twice and asserts which unit the FIRST sequence takes each time.</para>
        ///
        /// <para><b>Its bullets are disjoint by their own words</b> (HA-Q6), and the board is measured
        /// so that the exclusion is doing real work: the enemy hero stands beside his friendly hero and
        /// FOUR spaces from Hanu, so the second bullet names him and the first - "adjacent to you" -
        /// cannot. A unit in his own ring is what the first bullet has instead.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TrustedSidekickTakesBothPrintedBulletsInEitherOrder()
        {
            foreach (var friendlyFirst in new[] { true, false })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                    hand: new[] { HanuCardContent.TrustedSidekick, HanuCardContent.MonkeyTrick },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                    allySpace: new HexCoordinate(-1, 1),
                    enemySpace: new HexCoordinate(0, 1));
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var ally = Ally(runtime);
                var enemy = Enemies(runtime)[0];
                Assert.That(
                    Distance(Position(runtime, ally), Position(runtime, enemy)), Is.EqualTo(1),
                    "Arrange: the enemy hero stands beside his friendly hero. " + Remaining(runtime));
                Assert.That(
                    Distance(Position(runtime, his), Position(runtime, enemy)), Is.EqualTo(3),
                    "Arrange: and NOT beside Hanu, which is the exclusion the second bullet " +
                    "prints - three away, inside the printed Range 4 and outside p.19's ring. " +
                    Remaining(runtime));

                yield return BeginHanu(
                    root, HanuCardContent.TrustedSidekick, ActionKind.Attack,
                    WaspCardContent.Shock, ArienCardContent.NobleBlade);
                Assert.That(
                    OfferedBullets(runtime),
                    Is.EqualTo(new[] { "hanu-red-adjacent-bullet", "hanu-red-friendly-bullet" })
                        .AsCollection,
                    "Both printed bullets name somebody on this board. " + Remaining(runtime));

                var first = friendlyFirst ? "hanu-red-friendly-bullet" : "hanu-red-adjacent-bullet";
                var second = friendlyFirst ? "hanu-red-adjacent-bullet" : "hanu-red-friendly-bullet";
                yield return Bullet(root, first);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "\"one, OR BOTH\" - the modal is still standing after one press. " +
                    Remaining(runtime));
                Assert.That(OfferedBullets(runtime), Is.EqualTo(new[] { second }).AsCollection,
                    "and the other bullet is what is left to take. " + Remaining(runtime));
                yield return Bullet(root, second);
                Assert.That(runtime.PendingEffectChoice, Is.Null.Or.Property("NodeId")
                        .Not.EqualTo(new EffectNodeId("hanu-red-modal")),
                    "A Choose whose second Select fills its ceiling closes with no confirm. " +
                    Remaining(runtime));

                var target = runtime.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null,
                    "and the first chosen bullet's own p.14 opens. " + Remaining(runtime));
                if (friendlyFirst)
                {
                    Assert.That(target.LegalTargetHeroIds.ToArray(),
                        Is.EqualTo(new[] { enemy }).AsCollection,
                        "The friendly bullet was chosen first, so the hero beside his teammate is " +
                        "the first sequence's subject - \"in any order\" read where a player sets " +
                        "it. " + Remaining(runtime));
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            HeroView(enemy).transform.position),
                        Is.True);
                    yield return null;
                    if (runtime.PendingDefenseChoice != null)
                    {
                        yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                    }

                    yield return null;
                    yield return null;
                    // THE FIRST SEQUENCE'S OWN OUTCOME (milestone 8.86): the hero it named either
                    // answered p.14 with a face they have now spent, or was defeated by it. Both are
                    // the printed outcome; neither is "nothing happened", which is what this journey
                    // used to admit.
                    Assert.That(
                        !runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)) ||
                            runtime.Cards.Single(card =>
                                card.OwnerId.Equals(enemy) &&
                                card.DefinitionReference.DefinitionId.Equals(
                                    WaspCardContent.Electrocute)).State.Location ==
                                CardLocation.DiscardPile,
                        Is.True,
                        "The first sequence really ran p.14 on the hero it named. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(target.LegalTargetHeroIds, Is.Empty,
                        "The adjacent bullet was chosen first, and no hero stands in his ring. " +
                        Remaining(runtime));
                    Assert.That(target.LegalTargetMinionIds, Is.Not.Empty, Remaining(runtime));
                    // MILESTONE 8.86: named by its own id rather than taken first off the offer.
                    var minion = target.LegalTargetMinionIds
                        .OrderBy(value => value.Value, StringComparer.Ordinal).First();
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            MinionView(minion).transform.position),
                        Is.True);
                    yield return null;
                    yield return null;
                    Assert.That(
                        runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion)),
                        Is.False,
                        "THE FIRST SEQUENCE'S OWN OUTCOME: p.14 defeats an attacked minion without " +
                        "a Defense step. " + Remaining(runtime));
                }

                yield return null;
                yield return null;
                var next = runtime.PendingAttackTargetChoice;
                Assert.That(next, Is.Not.Null,
                    "and the SECOND chosen bullet's own p.14 follows it, which is what makes the " +
                    "two sequences one action. " + Remaining(runtime));
                if (friendlyFirst)
                {
                    Assert.That(next.LegalTargetMinionIds, Is.Not.Empty,
                        "\"a unit adjacent to you\" binds afresh against the board the first " +
                        "sequence left. " + Remaining(runtime));
                    var minion = next.LegalTargetMinionIds
                        .OrderBy(value => value.Value, StringComparer.Ordinal).First();
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            MinionView(minion).transform.position),
                        Is.True);
                    yield return null;
                    yield return null;
                    Assert.That(
                        runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(minion)),
                        Is.False,
                        "THE SECOND SEQUENCE'S OWN OUTCOME (milestone 8.86): the second bullet's " +
                        "p.14 really ran on the piece it named. " + Remaining(runtime));
                }
                else
                {
                    Assert.That(next.LegalTargetHeroIds.ToArray(),
                        Is.EqualTo(new[] { enemy }).AsCollection, Remaining(runtime));
                    Assert.That(
                        UiPlayer.ClickBoard(
                            Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                            root.Hud,
                            HeroView(enemy).transform.position),
                        Is.True);
                    yield return null;
                    if (runtime.PendingDefenseChoice != null)
                    {
                        yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
                    }

                    yield return null;
                    yield return null;
                    Assert.That(
                        !runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)) ||
                            runtime.Cards.Single(card =>
                                card.OwnerId.Equals(enemy) &&
                                card.DefinitionReference.DefinitionId.Equals(
                                    WaspCardContent.Electrocute)).State.Location ==
                                CardLocation.DiscardPile,
                        Is.True,
                        "THE SECOND SEQUENCE'S OWN OUTCOME (milestone 8.86). " + Remaining(runtime));
                }

                yield return null;
                yield return null;
                SeatsAgree(
                    runtime, friendlyFirst ? "the friendly bullet first" : "the adjacent bullet first");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Pile On: "Choose one, or both, in any order - • Target a unit adjacent to you. • Target a
        /// minion in range, adjacent to your friendly hero."
        ///
        /// <para><b>RULING 19, read where a player meets it.</b> The face prints no "(on different
        /// targets)", so neither bullet declares <c>distinctTargets</c> and ONE MINION satisfies both
        /// printed sentences at once - the board is measured so that the enemy ranged minion standing
        /// in Hanu's own ring is also the one minion standing beside his friendly hero. Both bullets
        /// are offered because of that single piece, which is the positive half of the ruling.</para>
        ///
        /// <para><b>And each bullet binds AFRESH</b> (HA-Q6), which is the half p.14 decides rather
        /// than the card: a minion targeted by an Attack is defeated without a Defense step, so the
        /// second sequence's own target question is asked against the board the first one left and
        /// offers the other minion instead. A row claiming the same piece could be hit twice would be
        /// claiming p.14 left it standing.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PileOnOffersOneMinionToBothBulletsAndBindsEachAfresh()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                hand: new[] { HanuCardContent.PileOn, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(-5, 2),
                enemySpace: new HexCoordinate(0, 1),
                movedMinions: new[]
                {
                    new KeyValuePair<MinionId, HexCoordinate>(
                        new MinionId("red-melee-01"), new HexCoordinate(-2, 1))
                });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var shared = new MinionId("red-ranged-01");
            var other = new MinionId("red-melee-01");

            Assert.That(
                Distance(MinionView(shared).Occupancy.Coordinate, Position(runtime, his)),
                Is.EqualTo(1),
                "Arrange: the shared minion stands in his own ring. " + Remaining(runtime));
            Assert.That(
                Distance(MinionView(shared).Occupancy.Coordinate, Position(runtime, ally)),
                Is.EqualTo(1),
                "Arrange: and beside his friendly HERO, which is what makes it the subject of both " +
                "printed sentences at once. " + Remaining(runtime));
            Assert.That(
                Distance(MinionView(other).Occupancy.Coordinate, Position(runtime, his)),
                Is.EqualTo(1),
                "Arrange: a second minion stands in his ring and nowhere near the ally. " +
                Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.PileOn, ActionKind.Attack,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            Assert.That(
                OfferedBullets(runtime),
                Is.EqualTo(new[] { "hanu-red-adjacent-bullet", "hanu-red-friendly-bullet" })
                    .AsCollection,
                "Both bullets are offered, and the narrowed one is offered because of the same " +
                "piece the other one is. " + Remaining(runtime));
            yield return Bullet(root, "hanu-red-friendly-bullet");
            yield return Bullet(root, "hanu-red-adjacent-bullet");

            var first = runtime.PendingAttackTargetChoice;
            Assert.That(first, Is.Not.Null, Remaining(runtime));
            Assert.That(
                first.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                Is.EqualTo(new[] { shared.Value }).AsCollection,
                "\"a minion in range, adjacent to your friendly hero\" - the one piece the " +
                "predicate admits. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(shared).transform.position),
                Is.True);
            yield return null;
            yield return null;
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(shared)),
                Is.False,
                "p.14 defeats an attacked minion without a Defense step. " + Remaining(runtime));

            var second = runtime.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "and the second chosen bullet's own p.14 follows. " + Remaining(runtime));
            Assert.That(
                second.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                Is.EqualTo(new[] { other.Value }).AsCollection,
                "HA-Q6: each bullet binds afresh against the board its own sequence reaches, so the " +
                "piece the first one removed is not a candidate for the second - and the exclusion " +
                "is p.14's removal rather than a printed \"(on different targets)\". " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(other).transform.position),
                Is.True);
            yield return null;
            yield return null;
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(other)), Is.False,
                Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "and two bullets is the printed ceiling. " + Remaining(runtime));
            SeatsAgree(runtime, "Pile On's two bullets");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier III
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Safe Travels: "Swap with an enemy hero in radius. This turn: That hero is immune. End of
        /// turn: Swap with that hero regardless of radius and immunity. Move up to 1 space."
        ///
        /// <para><b>HA-Q8, settled by the user on 2026-09-21, read where a player meets it.</b> The
        /// trailing sentence is printed AFTER the "End of turn:" keyword, so it belongs to that keyword
        /// and runs at the turn's close - after the swap back, from wherever the swap back has just put
        /// him, and not at resolution after the first swap. The journey therefore asserts that NOTHING
        /// is asked when the Skill resolves and that the move is asked at the end of the turn, with
        /// its offer centred on the space he has just been returned to.</para>
        ///
        /// <para>Both answers of the printed "up to" are driven: one space, and NOUGHT - which is his
        /// own square inside the offer rather than a decline beside it (Garrus's GA-Q1, milestone
        /// 8.39j's rule for every direct Move in this build).</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SafeTravelsMovesHimUpToOneSpaceAfterTheSwapBack()
        {
            foreach (var moves in new[] { true, false })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                    hand: new[] { HanuCardContent.SafeTravels, HanuCardContent.MonkeyBusiness },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    enemySpace: new HexCoordinate(-3, -1));
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var enemy = Enemies(runtime)[0];
                var hisStood = Position(runtime, his);
                var theirStood = Position(runtime, enemy);

                yield return BeginHanu(
                    root, HanuCardContent.SafeTravels, ActionKind.Skill, WaspCardContent.Shock);
                var subject = runtime.PendingEffectChoice;
                Assert.That(subject.NodeId.Value, Is.EqualTo("journey-subject"), Remaining(runtime));
                yield return Pick(root, subject.Candidates.Single());
                Assert.That(Position(runtime, his), Is.EqualTo(theirStood), Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "HA-Q8: the trailing move is the END-OF-TURN sentence's, so nothing is asked " +
                    "here - which is the whole of what separates this face from Monkey Business and " +
                    "See Nothing. " + Remaining(runtime));
                SeatsAgree(runtime, "Safe Travels out");

                yield return EndTheWholeTurn(root);
                yield return null;
                yield return null;
                Assert.That(Position(runtime, his), Is.EqualTo(hisStood),
                    "The swap back runs first. " + Remaining(runtime));
                var walk = runtime.PendingEffectChoice;
                Assert.That(walk, Is.Not.Null,
                    "and THEN \"Move up to 1 space\" is asked. " + Remaining(runtime));
                Assert.That(walk.NodeId.Value, Is.EqualTo("journey-back-move"));
                Assert.That(walk.DeclineInvocation, Is.Null,
                    "An \"up to\" direct move carries its own nought inside the offer rather than " +
                    "as a decline beside it. " + Remaining(runtime));
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, hisStood) <= 1),
                    Is.True,
                    "and every candidate is within one of the space the swap back returned him to. " +
                    "Offered: " +
                    string.Join(", ", OfferedSpaces(runtime).Select(space => space.ToString())) +
                    ". " + Remaining(runtime));
                Assert.That(OfferedSpaces(runtime), Contains.Item(hisStood),
                    "including the space he is standing on, which is how \"up to\" says nought. " +
                    Remaining(runtime));

                var destination = moves
                    ? OfferedSpaces(runtime).First(space => Distance(space, hisStood) == 1)
                    : hisStood;
                yield return Space(root, destination);
                Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
                Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, moves ? "Safe Travels' step" : "Safe Travels' nought");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// That Way!: This Way!'s sentence with a THIRD printed distance - "a distance of 1, 2 or 3".
        ///
        /// <para>Every printed distance is DRAWN on the friend's own seat, and each one is driven where
        /// the board admits it. A distance the board does not admit is asserted GREYED rather than
        /// skipped - milestone 6.5ac's channel, which is the whole reason HA-Q13e composed the
        /// distances as a modal instead of as a handler's integers - and the journey requires that at
        /// least two of the three were really performed, so it cannot pass by greying everything.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThatWayOffersTheFriendEachOfItsThreePrintedDistances()
        {
            var driven = 0;
            foreach (var distance in new[] { 1, 2, 3 })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                    hand: new[] { HanuCardContent.ThatWay, HanuCardContent.MonkeyTrick },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                    allySpace: new HexCoordinate(-2, 1));
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var ally = Ally(runtime);

                yield return BeginHanu(
                    root, HanuCardContent.ThatWay, ActionKind.Skill,
                    WaspCardContent.Shock, ArienCardContent.NobleBlade);
                var friend = runtime.PendingEffectChoice;
                Assert.That(friend.NodeId.Value, Is.EqualTo("hanu-co-move-friend"),
                    Remaining(runtime));
                yield return Pick(root, friend.Candidates.Single());

                var modal = runtime.PendingEffectChoice;
                Assert.That(modal.NodeId.Value, Is.EqualTo("hanu-co-move-distance"),
                    Remaining(runtime));
                Assert.That(modal.ChooserId, Is.EqualTo(ally),
                    "The friendly hero answers on their own seat. " + Remaining(runtime));
                Assert.That(
                    OfferedBullets(runtime).Concat(GreyedBullets(runtime))
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    Is.EqualTo(new[]
                        {
                            "hanu-co-move-one-space", "hanu-co-move-three-spaces",
                            "hanu-co-move-two-spaces"
                        }).AsCollection,
                    "All THREE printed distances are drawn - offered or greyed. " +
                    Remaining(runtime));

                var node = distance == 1 ? "hanu-co-move-one-space"
                    : distance == 2 ? "hanu-co-move-two-spaces"
                    : "hanu-co-move-three-spaces";
                if (!OfferedBullets(runtime).Contains(node))
                {
                    Assert.That(GreyedBullets(runtime), Contains.Item(node),
                        "A distance the board does not admit is drawn greyed rather than dropped. " +
                        Remaining(runtime));
                    Assert.That(
                        runtime.RoundView.Model.UnavailableEffectChoices, Is.Not.Empty,
                        "and the rendered window draws it too. " + Remaining(runtime));
                    Healthy(runtime);
                    continue;
                }

                var hisWas = Position(runtime, his);
                var allyWas = Position(runtime, ally);
                yield return Bullet(root, node);
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo(node),
                    Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(his),
                    "and the DIRECTION is Hanu's own. " + Remaining(runtime));
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, hisWas) == distance),
                    Is.True,
                    "at exactly the distance the friend named. " + Remaining(runtime));
                yield return Space(root);
                Assert.That(Distance(Position(runtime, his), hisWas), Is.EqualTo(distance),
                    Remaining(runtime));
                Assert.That(Distance(Position(runtime, ally), allyWas), Is.EqualTo(distance),
                    "\"move both of you that number of spaces\". " + Remaining(runtime));
                driven++;
                SeatsAgree(runtime, "That Way! at " + distance);
                Healthy(runtime);
            }

            Assert.That(driven, Is.GreaterThanOrEqualTo(2),
                "At least two of the three printed distances must really have been performed, or " +
                "this journey would be passing on greyings alone.");
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier III: the two trailing moves
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Monkey Business: "Swap two friendly units in radius. Move up to 1 space."
        ///
        /// <para><b>The move starts where he has been standing all along</b>, and that is the
        /// structural half of the sentence: the swap he just performed moved two OTHER units, so
        /// <c>synthetic-actor-space@1</c> publishes his own square after the exchange rather than a
        /// space the exchange left him in. Both answers of the printed "up to" are driven, nought
        /// included.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MonkeyBusinessSwapsThePairAndThenMovesHimOrLeavesHim()
        {
            foreach (var moves in new[] { true, false })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                    hand: new[] { HanuCardContent.MonkeyBusiness, HanuCardContent.HurryUp },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    movedMinions: new[]
                    {
                        new KeyValuePair<MinionId, HexCoordinate>(
                            new MinionId("blue-ranged-01"), new HexCoordinate(-2, 1))
                    });
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var heavy = new MinionId("blue-ranged-01");
                var melee = new MinionId("blue-melee-01");
                var hisStood = Position(runtime, his);

                yield return BeginHanu(
                    root, HanuCardContent.MonkeyBusiness, ActionKind.Skill, WaspCardContent.Shock);
                var pair = runtime.PendingEffectChoice;
                Assert.That(pair.NodeId.Value, Is.EqualTo("green-friendly-swap-pair"),
                    Remaining(runtime));
                var heavyWas = MinionView(heavy).Occupancy.Coordinate;
                var meleeWas = MinionView(melee).Occupancy.Coordinate;
                yield return Pick(
                    root,
                    pair.Candidates.Single(candidate => candidate.Value.MinionId.Equals(heavy)));
                yield return Pick(
                    root,
                    runtime.PendingEffectChoice.Candidates.Single(candidate =>
                        candidate.Value.MinionId.Equals(melee)));
                Assert.That(MinionView(heavy).Occupancy.Coordinate, Is.EqualTo(meleeWas),
                    Remaining(runtime));
                Assert.That(MinionView(melee).Occupancy.Coordinate, Is.EqualTo(heavyWas));
                Assert.That(Position(runtime, his), Is.EqualTo(hisStood),
                    "and p.19's Swap moved neither of HIM. " + Remaining(runtime));

                var walk = runtime.PendingEffectChoice;
                Assert.That(walk, Is.Not.Null,
                    "\"Move up to 1 space\" follows the swap. " + Remaining(runtime));
                Assert.That(walk.NodeId.Value, Is.EqualTo("green-friendly-swap-move"));
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, hisStood) <= 1),
                    Is.True,
                    "from the square he has been standing on all along. " + Remaining(runtime));
                Assert.That(OfferedSpaces(runtime), Contains.Item(hisStood), Remaining(runtime));

                var destination = moves
                    ? OfferedSpaces(runtime).First(space => Distance(space, hisStood) == 1)
                    : hisStood;
                yield return Space(root, destination);
                Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
                Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
                Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
                SeatsAgree(runtime, moves ? "Monkey Business' step" : "Monkey Business' nought");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// See Nothing: "Swap with an enemy hero in radius. Move up to 1 space."
        ///
        /// <para><b>The move starts where the exchange left HIM</b> - BR-Q20d's binding - which is the
        /// one structural difference between this face and Monkey Business, and it is measured by
        /// standing the enemy hero three spaces off: the offer is centred on the enemy's old square and
        /// not on his own.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SeeNothingSwapsWithTheEnemyAndMovesFromTheExchangedSpace()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedHanu,
                hand: new[] { HanuCardContent.SeeNothing, HanuCardContent.HurryUp },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                enemySpace: new HexCoordinate(0, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];
            var hisStood = Position(runtime, his);
            var theirStood = Position(runtime, enemy);
            Assert.That(Distance(hisStood, theirStood), Is.EqualTo(3),
                "Arrange: three spaces off, inside the printed Radius 3 - and far enough that an " +
                "offer centred on his OLD square would be a different set. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.SeeNothing, ActionKind.Skill, WaspCardContent.Shock);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject.NodeId.Value, Is.EqualTo("green-enemy-swap-subject"),
                Remaining(runtime));
            yield return Pick(root, subject.Candidates.Single());
            Assert.That(Position(runtime, his), Is.EqualTo(theirStood), Remaining(runtime));

            var walk = runtime.PendingEffectChoice;
            Assert.That(walk, Is.Not.Null, Remaining(runtime));
            Assert.That(walk.NodeId.Value, Is.EqualTo("green-enemy-swap-move"));
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, theirStood) <= 1),
                Is.True,
                "BR-Q20d: the trailing move starts from the space the exchange left him in. " +
                "Offered: " +
                string.Join(", ", OfferedSpaces(runtime).Select(space => space.ToString())) + ". " +
                Remaining(runtime));
            var destination = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                OfferedSpaces(runtime).Where(space => Distance(space, theirStood) == 1).ToArray());
            yield return Space(root, destination);
            Assert.That(Position(runtime, his), Is.EqualTo(destination), Remaining(runtime));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination));
            SeatsAgree(runtime, "See Nothing");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Ultimate: the seat that answers
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The Ultimate Trick: "You choose the next action, and how it is performed for a hero you
        /// target with the 'Hurry Up!'."
        ///
        /// <para><b>Three delegated turns, and each one shows a different half of HA-Q3 and
        /// HA-Q13c.</b></para>
        /// <list type="number">
        /// <item><description><b>An ENEMY's action.</b> Hanu rewrites the enemy's unresolved card, the
        /// turn passes to that hero, and the controls are on HANU's seat: the targeted seat holds no
        /// start command at all and its own tray draws none, while Hanu's holds theirs and his tray
        /// draws them. He starts their Attack, answers its target on the board - and the delegation is
        /// GONE the moment that action completes, because the turn authority resolves the card the
        /// rewrite was written on.</description></item>
        /// <item><description><b>A FRIENDLY hero's action, taken as a HOLD.</b> "It applies to any hero
        /// Hanu targets, friend or enemy" (HA-Q3), and p.12's Hold is one of the offers that hero's own
        /// seat would have had - drawn on Hanu's seat as the END TURN control whose command is
        /// <c>PerformHold</c>, pressed from his side of the table, and spending the delegated hero's
        /// turn.</description></item>
        /// <item><description><b>HANU DEFEATED MID-WAY.</b> p.16 gives an Ultimate its ability "as long
        /// as your hero is on the board" and p.10 ends the rewrite only at the rewritten card's own
        /// state change, so a Hanu defeated inside the delegated action stops deciding while the
        /// eleven stands - and the targeted player takes their own action back, which is HA-Q13c. The
        /// journey reaches it the only way a live board can: Hanu's seat aims the delegated hero's own
        /// ranged Attack at HANU, Hanu's own seat answers p.14's Defense (a Defense response is never
        /// delegated), and the printed after-attack repeat that follows is then the targeted seat's to
        /// answer.</description></item>
        /// </list>
        ///
        /// <para>The retained-prefix Undo of a DELEGATED prompt is driven inside (1): one press gives
        /// the target question back unanswered, and it comes back owned by the same seat that was
        /// asked - which is the deciding seat and not the acting one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheUltimateTrickTakesTheNextActionOfAnEnemyAndOfAFriendFromHisOwnSeat()
        {
            // ---------------------------------------------------------------------------------
            // (1) An enemy's whole action, decided from his seat
            // ---------------------------------------------------------------------------------
            yield return StageHanu(
                SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyBusiness },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                enemySpace: AdjacentEnemy);
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];

            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(his)).UltimateCard,
                Is.Not.Null,
                "p.16 turns the unlocked Ultimate faceup beside his dashboard, where a player reads " +
                "it. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.HurryUp, ActionKind.Skill, WaspCardContent.Electrocute);
            yield return Pick(
                root,
                runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.HeroId.Equals(enemy)));
            Assert.That(RewriteStandsOn(runtime, enemy, WaspCardContent.Electrocute), Is.True,
                "The rewrite is the only fact the delegation is derived from. " + Remaining(runtime));

            yield return EndHisTurn(root, his);
            for (var frame = 0; frame < 8 && !runtime.SelectedActor.IsDefined; frame++)
            {
                yield return null;
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemy),
                "The hurried hero acts next, at the eleven. " + Remaining(runtime));
            Assert.That(StartCommandsOf(runtime, enemy), Is.Empty,
                "and their OWN seat may start nothing: the host withholds every start command " +
                "while the delegation stands. " + Remaining(runtime));
            Assert.That(StartCommandsOf(runtime, his), Is.Not.Empty,
                "Hanu's seat holds them instead - \"You choose the next action\". " +
                Remaining(runtime));

            // THE RENDERED HALF, read where milestone 8.71h had to move it. A Director sandbox
            // publishes one seatless projection, so the ACTION LIST is the acting hero's whatever
            // seat is being looked at - what is viewer-scoped is the turn-ending control, which
            // `RoundActionPresenter.IsViewerTheActingHero` gates through
            // `DelegatedActionQuery.DecidingHeroFor`. So the targeted player is shown no way to end
            // the turn that is nominally theirs, and Hanu is.
            // THE RENDERED HALF, asked of each SEAT's own published projection. A Director sandbox
            // composes one seatless view that answers for everybody - `AwaitedInputQuery` says so in
            // as many words, "the undefined hero for the Director, who answers for everyone" - so
            // the lit/dark reading is taken where a real player's HUD takes it: off the projection
            // that seat is sent, which is also what a networked client receives.
            yield return PointTrayAt(root, enemy);
            Assert.That(
                AwaitedInputQuery.IsWaitingOnViewer(
                    runtime.Host.ProjectionForSeat(SeatOf(runtime, enemy)), enemy),
                Is.False,
                "and the targeted player's own tray is DARK - the seat whose PIECE is acting is not " +
                "the seat the HUD shines. " + Remaining(runtime));
            yield return PointTrayAt(root, his);
            Assert.That(
                AwaitedInputQuery.IsWaitingOnViewer(
                    runtime.Host.ProjectionForSeat(SeatOf(runtime, his)), his),
                Is.True,
                "while his is lit, on the same frame - which is the whole of the Presentation " +
                "change milestone 8.71h had to make. " + Remaining(runtime));
            SeatsAgree(runtime, "the delegated enemy turn");

            // HA-Q15i, MEASURED BY THIS JOURNEY AT 8.71i AND FIXED AT 8.72. The printed-action
            // controls are drawn as an overlay on the RESOLVING CARD inside the viewed hero's own
            // tray (`DirectorHandsUnityView`), and a delegated hero's card is not in Hanu's hand -
            // so until 8.72 the seat the host authorizes was shown no control of its own and this
            // journey had to press where the HUD happened to draw it, on the TARGETED hero's tray.
            // It now presses where the host would accept it: the delegated hero's revealed card is
            // drawn in a "deciding for X" slot beside HIS hand, and the targeted tray draws nothing
            // at all. Both halves are asserted below and on their own seats by
            // `TheDelegatedActionIsDrawnAndPressedOnHisOwnSeatAtBothDesktopViewports`.
            yield return PointTrayAt(root, enemy);
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                    element.name == "gd-deciding-for-slot" && UiPlayer.IsShown(element)),
                Is.False,
                "Pointed at the targeted hero, nothing claims to be deciding for anybody. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                    element.name == "gd-printed-action-region" && UiPlayer.IsShown(element)),
                Is.False,
                "and the targeted hero's own tray draws no control - milestone 8.72's dark half, " +
                "read on the tray that used to carry them. " + Remaining(runtime));

            yield return PointTrayAt(root, his);
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement).Count(element =>
                    element.name == "gd-deciding-for-slot" && UiPlayer.IsShown(element)),
                Is.EqualTo(1),
                "One slot, on the seat the host is waiting on, naming whose card it holds. " +
                Remaining(runtime));
            yield return StartActionThroughControl(root, ActionKind.Attack);
            var target = runtime.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null,
                "Hanu's seat starts the delegated hero's own printed Attack. " + Remaining(runtime));
            Assert.That(
                target.LegalTargetMinionIds.Select(value => value.Value).ToArray(),
                Contains.Item("blue-melee-01"),
                "whose candidates are that hero's - the friendly minion standing beside THEM. " +
                Remaining(runtime));

            // The delegated prompt through milestone 8.35f's retained-prefix rewind: it comes back
            // unanswered, and it comes back on the DECIDING seat.
            var accepted = runtime.AcceptedCommands.Count;
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            Assert.That(runtime.AcceptedCommands.Count, Is.LessThan(accepted),
                "The press reaches the host's rewind. " + Remaining(runtime));
            Assert.That(StartCommandsOf(runtime, his), Is.Not.Empty,
                "and the delegation is still Hanu's after the rewind - it is derived from the " +
                "rewrite, which the rewind did not touch. " + Remaining(runtime));
            Assert.That(StartCommandsOf(runtime, enemy), Is.Empty, Remaining(runtime));

            // Milestone 8.72: the slot comes back with the delegation, because it IS the delegation
            // drawn - the rewind put the offer back and the derivation follows it.
            yield return PointTrayAt(root, his);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                "Replaying the retained prefix reproduces the same question. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(new MinionId("blue-melee-01")).transform.position),
                Is.True,
                "and Hanu answers it on the board. " + Remaining(runtime));
            yield return null;
            yield return null;
            Assert.That(
                runtime.ReadModel.Minions.Any(minion =>
                    minion.MinionId.Equals(new MinionId("blue-melee-01"))),
                Is.False,
                "The delegated Attack is performed whole - and it was Hanu who chose whose minion " +
                "paid for it. " + Remaining(runtime));
            Assert.That(
                RewriteStandsOn(runtime, enemy, WaspCardContent.Electrocute), Is.False,
                "HA-Q4: the delegation is consumed when the action completes, because the turn " +
                "authority resolves the rewritten card after the whole action. " + Remaining(runtime));
            Assert.That(StartCommandsOf(runtime, his), Is.Empty,
                "and Hanu's seat holds nothing of theirs any more. " + Remaining(runtime));
            SeatsAgree(runtime, "the delegated action completed");
            Healthy(runtime);

            // ---------------------------------------------------------------------------------
            // (2) A friendly hero's action, taken as p.12's Hold
            // ---------------------------------------------------------------------------------
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[]
                {
                    ArienCardContent.DangerousCurrent, ArienCardContent.NobleBlade
                });
            var allyRoot = Root();
            var allyRuntime = allyRoot.Runtime;
            var allyHanu = Hanu(allyRuntime);
            var ally = Ally(allyRuntime);
            Assert.That(
                allyRuntime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(allyHanu)).UltimateCard,
                Is.Not.Null,
                "Arrange: milestone 8.71i unlocks the Ultimate on the Tier III ally arm, which is " +
                "the only board a FRIENDLY hero he may Hurry up! stands on. " +
                Remaining(allyRuntime));

            yield return BeginHanu(
                allyRoot, HanuCardContent.HurryUp, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.DangerousCurrent);
            yield return Pick(
                allyRoot,
                allyRuntime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.HeroId.Equals(ally)));
            Assert.That(
                RewriteStandsOn(allyRuntime, ally, ArienCardContent.DangerousCurrent), Is.True,
                Remaining(allyRuntime));

            yield return EndHisTurn(allyRoot, allyHanu);
            for (var frame = 0; frame < 8 && !allyRuntime.SelectedActor.IsDefined; frame++)
            {
                yield return null;
            }

            Assert.That(allyRuntime.SelectedActor.ActorId, Is.EqualTo(ally),
                "His own teammate acts next, at the eleven. " + Remaining(allyRuntime));
            Assert.That(StartCommandsOf(allyRuntime, ally), Is.Empty,
                "HA-Q3: it applies to any hero he targets, friend or enemy - so his teammate's own " +
                "seat may start nothing either. " + Remaining(allyRuntime));
            Assert.That(StartCommandsOf(allyRuntime, allyHanu), Is.Not.Empty, Remaining(allyRuntime));

            yield return PointTrayAt(allyRoot, allyHanu);
            var hold = allyRuntime.RoundView.Model.EndTurnInvocation;
            Assert.That(hold, Is.Not.Null,
                "p.12's Hold is one of the offers that hero's seat would have had, drawn on the " +
                "deciding seat. " + Remaining(allyRuntime));
            Assert.That(hold.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold),
                "and the control really is the Hold rather than a release. " + Remaining(allyRuntime));
            UiPlayer.Click(UiPlayer.Button(allyRoot.Hud, hold.Label));
            yield return null;
            yield return null;
            Assert.That(
                RewriteStandsOn(allyRuntime, ally, ArienCardContent.DangerousCurrent), Is.False,
                "HA-Q4: p.12's Hold resolves the card it is taken on, so it spends the delegation " +
                "exactly as a printed action does. " + Remaining(allyRuntime));
            SeatsAgree(allyRuntime, "the delegated Hold");
            Healthy(allyRuntime);

            // ---------------------------------------------------------------------------------
            // (3) HA-Q13c: Hanu defeated mid-way, and the targeted player takes their action back
            // ---------------------------------------------------------------------------------
            yield return StageHanu(
                SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyBusiness },
                enemyHand: new[]
                {
                    WaspCardContent.ThunderBoomerang, WaspCardContent.KineticRepulse
                });
            var lastRoot = Root();
            var lastRuntime = lastRoot.Runtime;
            var lastHanu = Hanu(lastRuntime);
            var lastEnemy = Enemies(lastRuntime)[0];
            Assert.That(
                Distance(Position(lastRuntime, lastHanu), Position(lastRuntime, lastEnemy)),
                Is.EqualTo(2),
                "Arrange: two spaces off on a BENT line, which is what Thunder Boomerang's \"in " +
                "range and not in a straight line\" needs of its target. " + Remaining(lastRuntime));

            yield return BeginHanu(
                lastRoot, HanuCardContent.HurryUp, ActionKind.Skill,
                WaspCardContent.ThunderBoomerang);
            yield return Pick(
                lastRoot,
                lastRuntime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.HeroId.Equals(lastEnemy)));
            yield return EndHisTurn(lastRoot, lastHanu);
            for (var frame = 0; frame < 8 && !lastRuntime.SelectedActor.IsDefined; frame++)
            {
                yield return null;
            }

            Assert.That(lastRuntime.SelectedActor.ActorId, Is.EqualTo(lastEnemy),
                Remaining(lastRuntime));
            Assert.That(StartCommandsOf(lastRuntime, lastHanu), Is.Not.Empty, Remaining(lastRuntime));

            // Milestone 8.72: started from HIS tray's "deciding for X" slot, which is where the host
            // accepts it.
            yield return PointTrayAt(lastRoot, lastHanu);
            yield return StartActionThroughControl(lastRoot, ActionKind.Attack);
            Assert.That(
                lastRuntime.PendingAttackTargetChoice.LegalTargetHeroIds, Contains.Item(lastHanu),
                "Hanu's seat may aim the delegated hero's Attack anywhere that hero could, himself " +
                "included - p.18's \"you are never immune to your own actions\" is about the ACTOR, " +
                "and the actor here is the enemy. " + Remaining(lastRuntime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    lastRoot.Hud,
                    HeroView(lastHanu).transform.position),
                Is.True,
                Remaining(lastRuntime));
            yield return null;
            Assert.That(lastRuntime.PendingDefenseChoice, Is.Not.Null,
                "p.14's Defense is the defender's own and is never delegated. " +
                Remaining(lastRuntime));
            UiPlayer.ClickDefenseDecline(lastRuntime, lastRoot.Hud);
            yield return null;
            yield return null;
            Assert.That(lastRuntime.Heroes.Any(hero => hero.HeroId.Equals(lastHanu)), Is.False,
                "An undefended Attack defeats him and the piece leaves the board. " +
                Remaining(lastRuntime));

            var repeat = lastRuntime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "\"After the attack: If you targeted a hero may repeat once on a different target\" " +
                "stops the page INSIDE the delegated action. " + Remaining(lastRuntime));
            Assert.That(MayAnswerTheStandingChoice(lastRuntime, lastEnemy), Is.True,
                "HA-Q13c: p.16 gives the ability its lifetime \"as long as your hero is on the " +
                "board\", so a defeated Hanu decides nothing and the targeted player takes their " +
                "own action back. " + Remaining(lastRuntime));
            Assert.That(
                RewriteStandsOn(lastRuntime, lastEnemy, WaspCardContent.ThunderBoomerang), Is.True,
                "while the eleven stands, because p.10 ends it only at the rewritten card's own " +
                "state change. " + Remaining(lastRuntime));
            SeatsAgree(lastRuntime, "the delegation lapsed by defeat");

            UiPlayer.ClickEffectControl(
                lastRuntime, lastRoot.Hud, EffectChoiceCommandKind.Decline);
            yield return null;
            yield return null;
            Healthy(lastRuntime);
        }

        /// <summary>
        /// Milestone 8.72, the user's correction: "Q15i means that currently that card wont work."
        ///
        /// <para><b>Two seats, two HUDs, one frame.</b> The journey above drives the delegation on the
        /// Director's sandbox, which composes ONE seatless view that answers for everybody - so it can
        /// prove the authority and the narration but not what two real players are looking at. This one
        /// composes a live HUD PER SEAT off each seat's own published projection, at both desktop
        /// viewports, and reads the answer off the drawn elements: the deciding player's tray carries
        /// the delegated hero's card in a slot that names whose it is, with exactly the controls the
        /// host authorizes that seat, and the targeted player's tray carries the same card with no
        /// control on it and no way to end the turn. The press is then made on that drawn control and
        /// travels through Hanu's own seat client, which is the whole of what HA-Q15i said could not be
        /// done.</para>
        ///
        /// <para>An ENEMY's action and a FRIENDLY hero's action, because HA-Q3 says "friend or enemy";
        /// the friendly half is taken as p.12's Hold, which is the other shape of offer a delegation
        /// carries. The drawn state is put through the seat's own Undo and a snapshot restore on the
        /// way, because a slot that only exists on the way forward is a slot that disappears the first
        /// time a real table steps back.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheDelegatedActionIsDrawnAndPressedOnHisOwnSeatAtBothDesktopViewports()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                // -----------------------------------------------------------------------------
                // (1) A delegated ENEMY action, drawn on both seats and pressed on his
                // -----------------------------------------------------------------------------
                yield return StageHanu(
                    SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                    hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyBusiness },
                    enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                    enemySpace: AdjacentEnemy);
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var enemy = Enemies(runtime)[0];

                yield return BeginHanu(
                    root, HanuCardContent.HurryUp, ActionKind.Skill, WaspCardContent.Electrocute);
                yield return Pick(
                    root,
                    runtime.PendingEffectChoice.Candidates.Single(candidate =>
                        candidate.Value.HeroId.Equals(enemy)));
                yield return EndHisTurn(root, his);
                for (var frame = 0; frame < 8 && !runtime.SelectedActor.IsDefined; frame++)
                {
                    yield return null;
                }

                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemy),
                    "Arrange: the hurried enemy acts next, at the eleven. " + Remaining(runtime));

                // The seat's own bounded rewind, taken while the delegation stands, and a snapshot
                // captured across it - the drawn state has to survive both.
                var captured = runtime.CaptureSnapshot();
                Assert.That(captured, Is.Not.Null.And.Not.Empty);

                foreach (var viewport in new[]
                         {
                             (Width: 2560u, Height: 1440u, Label: "reference"),
                             (Width: 2048u, Height: 1152u, Label: "validated desktop")
                         })
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.72 delegated enemy " + viewport.Label);
                    yield return null;
                    yield return null;

                    var deciding = SeatHud.Compose(runtime, his, "deciding-" + viewport.Label);
                    var targeted = SeatHud.Compose(runtime, enemy, "targeted-" + viewport.Label);
                    yield return null;
                    yield return null;
                    deciding.Refresh();
                    targeted.Refresh();
                    yield return null;

                    AssertTheDecidingSeatDrawsTheDelegatedCard(
                        deciding, runtime, enemy, viewport.Label);
                    AssertTheTargetedSeatDrawsNothing(targeted, runtime, viewport.Label);

                    deciding.Dispose();
                    targeted.Dispose();
                    yield return null;
                }

                // THE PRESS, at the validated desktop viewport, on the control his own HUD drew.
                UiPlayer.SetPlayerViewport(2048u, 1152u, "8.72 delegated enemy press");
                yield return null;
                yield return null;
                var press = SeatHud.Compose(runtime, his, "press");
                yield return null;
                yield return null;
                press.Refresh();
                yield return null;
                var attack = press.Round.Model.PrintedActionRegions.Single(region =>
                    region.ActionKind == ActionKind.Attack && region.Invocation != null);
                UiPlayer.Click(UiPlayer.Button(press.Hud, attack.ActionLabel));
                yield return null;
                yield return null;
                Assert.That(runtime.Host.Projection.PendingAttackTargetChoice, Is.Not.Null,
                    "HA-Q15i CLOSED: a press on the control HIS HUD drew starts the delegated " +
                    "hero's own printed Attack, through his own seat client. " + Remaining(runtime));
                Assert.That(
                    runtime.Host.ProjectionForSeat(SeatOf(runtime, his)).DecisionContract,
                    Is.Not.Null,
                    "and the question it opened is his to answer. " + Remaining(runtime));
                Assert.That(
                    runtime.Host.ProjectionForSeat(SeatOf(runtime, enemy)).DecisionContract,
                    Is.Null,
                    "while the hero whose Attack it is is handed nothing. " + Remaining(runtime));
                press.Dispose();
                yield return null;

                // BOTH SEATS' UNDO, ACROSS THE DRAWN STATE. Milestone 8.71b put the watermark on the
                // DECIDING seat, so the rewind belongs to the player who made the choice and not to
                // the piece that made it - and the square is drawn on exactly that seat.
                var undoDeciding = SeatHud.Compose(runtime, his, "undo-deciding");
                var undoTargeted = SeatHud.Compose(runtime, enemy, "undo-targeted");
                yield return null;
                yield return null;
                undoDeciding.Refresh();
                undoTargeted.Refresh();
                yield return null;
                Assert.That(undoDeciding.Round.Model.SeatUndoAvailable, Is.True,
                    "His seat may step back out of the choice his own press opened. " +
                    Remaining(runtime));
                Assert.That(
                    runtime.Host.ProjectionForSeat(SeatOf(runtime, enemy)).AuthorizedCommands,
                    Is.Empty,
                    "while the targeted player may submit nothing at all inside the delegated " +
                    "action - so there is no press of theirs to rewind, and their HUD offers " +
                    "none. " + Remaining(runtime));
                Assert.That(Regions(undoTargeted), Is.Empty, Remaining(runtime));
                var square = UiPlayer.Descendants(undoDeciding.Hud.RootElement).OfType<Button>()
                    .Where(element => element.name == "gd-seat-undo-control" &&
                        UiPlayer.IsShown(element))
                    .ToArray();
                Assert.That(square, Has.Length.EqualTo(1),
                    "and the square is drawn on his HUD. " + Remaining(runtime));
                var accepted = runtime.Host.Projection.AcceptedCommands.Count;
                UiPlayer.Click(square[0]);
                yield return null;
                yield return null;
                Assert.That(runtime.Host.Projection.AcceptedCommands.Count, Is.LessThan(accepted),
                    "The press reaches the host's rewind. " + Remaining(runtime));
                undoDeciding.Dispose();
                undoTargeted.Dispose();
                yield return null;

                var rewound = SeatHud.Compose(runtime, his, "rewound");
                var rewoundTarget = SeatHud.Compose(runtime, enemy, "rewound-target");
                yield return null;
                yield return null;
                rewound.Refresh();
                rewoundTarget.Refresh();
                yield return null;
                AssertTheDecidingSeatDrawsTheDelegatedCard(rewound, runtime, enemy, "after the undo");
                AssertTheTargetedSeatDrawsNothing(rewoundTarget, runtime, "after the undo");
                rewound.Dispose();
                rewoundTarget.Dispose();
                yield return null;

                // The drawn state across a snapshot restore: the slot comes back because the
                // delegation does, both derived from the same rewrite.
                Assert.That(runtime.RestoreSnapshot(captured), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
                var restored = SeatHud.Compose(runtime, his, "restored");
                var restoredTarget = SeatHud.Compose(runtime, enemy, "restored-target");
                yield return null;
                yield return null;
                restored.Refresh();
                restoredTarget.Refresh();
                yield return null;
                AssertTheDecidingSeatDrawsTheDelegatedCard(restored, runtime, enemy, "restored");
                AssertTheTargetedSeatDrawsNothing(restoredTarget, runtime, "restored");
                restored.Dispose();
                restoredTarget.Dispose();
                yield return null;
                SeatsAgree(runtime, "the restored delegated action");

                // -----------------------------------------------------------------------------
                // (2) A delegated FRIENDLY action, taken as p.12's Hold from his own seat's HUD
                // -----------------------------------------------------------------------------
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                    hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyTrick },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[]
                    {
                        ArienCardContent.DangerousCurrent, ArienCardContent.NobleBlade
                    });
                var allyRoot = Root();
                var allyRuntime = allyRoot.Runtime;
                var allyHanu = Hanu(allyRuntime);
                var ally = Ally(allyRuntime);

                yield return BeginHanu(
                    allyRoot, HanuCardContent.HurryUp, ActionKind.Skill,
                    WaspCardContent.Shock, ArienCardContent.DangerousCurrent);
                yield return Pick(
                    allyRoot,
                    allyRuntime.PendingEffectChoice.Candidates.Single(candidate =>
                        candidate.Value.HeroId.Equals(ally)));
                yield return EndHisTurn(allyRoot, allyHanu);
                for (var frame = 0; frame < 8 && !allyRuntime.SelectedActor.IsDefined; frame++)
                {
                    yield return null;
                }

                Assert.That(allyRuntime.SelectedActor.ActorId, Is.EqualTo(ally),
                    "Arrange: HA-Q3 - friend or enemy. " + Remaining(allyRuntime));

                foreach (var viewport in new[]
                         {
                             (Width: 2560u, Height: 1440u, Label: "reference"),
                             (Width: 2048u, Height: 1152u, Label: "validated desktop")
                         })
                {
                    UiPlayer.SetPlayerViewport(
                        viewport.Width, viewport.Height, "8.72 delegated friend " + viewport.Label);
                    yield return null;
                    yield return null;

                    var deciding = SeatHud.Compose(allyRuntime, allyHanu, "friend-" + viewport.Label);
                    var targeted = SeatHud.Compose(allyRuntime, ally, "friend-target-" + viewport.Label);
                    yield return null;
                    yield return null;
                    deciding.Refresh();
                    targeted.Refresh();
                    yield return null;

                    AssertTheDecidingSeatDrawsTheDelegatedCard(
                        deciding, allyRuntime, ally, viewport.Label);
                    AssertTheTargetedSeatDrawsNothing(targeted, allyRuntime, viewport.Label);

                    var hold = deciding.Round.Model.EndTurnInvocation;
                    Assert.That(hold, Is.Not.Null, Remaining(allyRuntime));
                    Assert.That(hold.Command.Kind, Is.EqualTo(DirectorCommandKind.PerformHold),
                        "p.12's Hold is one of the offers that hero's own seat would have had, " +
                        "drawn on his. " + Remaining(allyRuntime));

                    if (viewport.Width == 2048u)
                    {
                        UiPlayer.Click(UiPlayer.Button(deciding.Hud, hold.Label));
                        yield return null;
                        yield return null;
                        Assert.That(
                            RewriteStandsOn(
                                allyRuntime, ally, ArienCardContent.DangerousCurrent),
                            Is.False,
                            "HA-Q4: the Hold he pressed on his own HUD resolved the delegated " +
                            "hero's card, so the delegation is spent. " + Remaining(allyRuntime));
                    }

                    deciding.Dispose();
                    targeted.Dispose();
                    yield return null;
                }

                // -----------------------------------------------------------------------------
                // (3) The card leaves his tray, and the targeted seat is a seat again
                // -----------------------------------------------------------------------------
                var afterwards = SeatHud.Compose(allyRuntime, allyHanu, "afterwards");
                var released = SeatHud.Compose(allyRuntime, ally, "released");
                yield return null;
                yield return null;
                afterwards.Refresh();
                released.Refresh();
                yield return null;
                Assert.That(afterwards.Hands.Model.DecidedForHand, Is.Null,
                    "The borrowed card leaves his tray the moment the action is over. " +
                    Remaining(allyRuntime));
                Assert.That(Slots(afterwards), Is.Empty,
                    "and so does the slot that named it. " + Remaining(allyRuntime));
                Assert.That(released.Hands.Model.DecidedForHand, Is.Null,
                    "Nobody is deciding for the targeted player any more. " +
                    Remaining(allyRuntime));
                Assert.That(Slots(released), Is.Empty,
                    "and no slot is left behind on either side of the table. " +
                    Remaining(allyRuntime));
                afterwards.Dispose();
                released.Dispose();
                yield return null;
                SeatsAgree(allyRuntime, "the delegation spent");
                Healthy(allyRuntime);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(
                    restoreWidth, restoreHeight, "8.72 restore prior viewport");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.72: reading one seat's own HUD
        // -------------------------------------------------------------------------------------------

        /// <summary>Every "deciding for X" slot actually drawn on this seat's HUD.</summary>
        private static VisualElement[] Slots(SeatHud seat) =>
            UiPlayer.Descendants(seat.Hud.RootElement)
                .Where(element => element.name == "gd-deciding-for-slot" && UiPlayer.IsShown(element))
                .ToArray();

        /// <summary>Every printed-action control actually drawn on this seat's HUD.</summary>
        private static Button[] Regions(SeatHud seat) =>
            UiPlayer.Descendants(seat.Hud.RootElement).OfType<Button>()
                .Where(element => element.name == "gd-printed-action-region" &&
                    UiPlayer.IsShown(element))
                .ToArray();

        /// <summary>
        /// The deciding seat: one slot, naming the delegated hero, carrying that hero's revealed card
        /// and exactly the start commands the host authorizes this seat.
        /// </summary>
        private static void AssertTheDecidingSeatDrawsTheDelegatedCard(
            SeatHud seat,
            ConfiguredMatchSceneRuntime runtime,
            HeroId delegatedHeroId,
            string viewport)
        {
            var where = " at the " + viewport + " viewport. " + Remaining(runtime);
            var hero = runtime.Setup.Heroes
                .Single(candidate => candidate.HeroId.Equals(delegatedHeroId));
            var delegatedCardId = runtime.Host.Projection.SelectedActor.CardInstanceId;

            Assert.That(seat.Hands.Model.DecidedForHand, Is.Not.Null,
                "The deciding seat is given the delegated hero's card to press" + where);
            Assert.That(seat.Hands.Model.DecidedForHand.HeroId, Is.EqualTo(delegatedHeroId));
            Assert.That(
                seat.Hands.Model.DecidedForHand.TrayCards.Select(card => card.CardInstanceId),
                Is.EqualTo(new[] { delegatedCardId }).AsCollection,
                "and exactly that hero's one revealed resolving card" + where);

            var slots = Slots(seat);
            Assert.That(slots, Has.Length.EqualTo(1),
                "One slot is drawn, and it is on screen" + where);
            var ribbon = slots[0].Q<Label>("gd-deciding-for-label");
            Assert.That(ribbon, Is.Not.Null, "The slot names whose card it is" + where);
            Assert.That(ribbon.text,
                Is.EqualTo(DelegatedActionPresentation.DecidingForLabel(hero.DisplayName)),
                "so the card can never read as one of his own" + where);

            // The slot stands BESIDE the hand, past the compact hand's own stated footprint, so the
            // HUD has to block the board pointer across the box it was actually laid out in - or the
            // click that starts the delegated action also reaches the hex beneath it.
            var box = slots[0].worldBound;
            Assert.That(box.width, Is.GreaterThan(0f),
                "The slot is laid out before its footprint can be asked about" + where);
            Assert.That(
                seat.Hud.BlocksPointerAt(MatchHudPanelScaling.PanelPointToScreen(
                    box.center, Screen.width, Screen.height)),
                Is.True,
                "and the HUD consumes the pointer over it, so no press is counted twice" + where);

            var authorized = runtime.Host.ProjectionForSeat(seat.SeatId).AuthorizedCommands
                .Where(command => command.Kind == DirectorCommandKind.StartAction)
                .Select(command =>
                    command.CardInstanceId.Value + "/" + command.ActionDefinitionId.Value)
                .ToArray();
            var drawn = Regions(seat)
                .Select(button => (PrintedActionRegionViewModel)button.userData)
                .Where(region => region.Invocation != null)
                .Select(region => region.Invocation.Command.CardInstanceId.Value + "/" +
                    region.Invocation.Command.ActionDefinitionId.Value)
                .ToArray();
            Assert.That(drawn, Is.EquivalentTo(authorized).And.Not.Empty,
                "THE EQUALITY: what he can press is what the host accepts from him" + where);
            Assert.That(
                drawn.All(value => value.StartsWith(
                    delegatedCardId.Value + "/", StringComparison.Ordinal)),
                Is.True,
                "and every one of them is taken off the DELEGATED hero's own card" + where);

            Assert.That(
                seat.Round.Model.Status,
                Does.Contain(DelegatedActionPresentation.DecidingActivity),
                "His own narration tells him what to press" + where);
            Assert.That(
                seat.Round.Model.Status, Does.Contain(hero.DisplayName.ToUpperInvariant()),
                "and names whose action it is - in the narration lane's own upper-case voice" +
                where);
        }

        /// <summary>
        /// The targeted seat: its own card, drawn dark, with no control on it and no way to end the
        /// turn its piece is taking.
        /// </summary>
        private static void AssertTheTargetedSeatDrawsNothing(
            SeatHud seat, ConfiguredMatchSceneRuntime runtime, string viewport)
        {
            var where = " at the " + viewport + " viewport. " + Remaining(runtime);
            Assert.That(seat.Hands.Model.DecidedForHand, Is.Null,
                "The targeted seat decides nothing for anybody" + where);
            Assert.That(Slots(seat), Is.Empty, "and draws no slot" + where);
            Assert.That(Regions(seat), Is.Empty,
                "THE DARK HALF: not one control on the tray that holds the card" + where);
            Assert.That(seat.Round.Model.EndTurnInvocation, Is.Null,
                "and no way to end the turn its own piece is taking" + where);
            Assert.That(seat.Hands.Model.ViewedHandDrawsActionControls, Is.False);

            var own = seat.Hands.Model.ViewedHand.TrayCards
                .SingleOrDefault(card => card.IsResolving);
            Assert.That(own, Is.Not.Null,
                "Arrange: the card is still in play on its owner's own tray" + where);
            Assert.That(own.IsAwaitedAnswer, Is.False,
                "and it wears no gold, because the gold means \"this is yours to answer\"" + where);
        }

        /// <summary>
        /// Milestone 8.72. One seat's own live HUD, composed beside the scene's Director surface off
        /// that seat's own published projection - the same composition
        /// <c>SeatScopedMatchHudPlayModeTests</c> uses, and the only way two players' screens can be
        /// read on ONE frame of one match.
        /// </summary>
        private sealed class SeatHud : IDisposable
        {
            private readonly GameObject host;
            private readonly DirectorHandsPresenter handsPresenter;
            private readonly RoundActionPresenter roundPresenter;

            private SeatHud(
                GameObject host,
                SeatId seatId,
                ConfiguredMatchHudRenderer hud,
                DirectorHandsUnityView hands,
                RoundActionUnityView round,
                DirectorHandsPresenter handsPresenter,
                RoundActionPresenter roundPresenter)
            {
                this.host = host;
                this.handsPresenter = handsPresenter;
                this.roundPresenter = roundPresenter;
                SeatId = seatId;
                Hud = hud;
                Hands = hands;
                Round = round;
            }

            internal SeatId SeatId { get; }
            internal ConfiguredMatchHudRenderer Hud { get; }
            internal DirectorHandsUnityView Hands { get; }
            internal RoundActionUnityView Round { get; }

            internal static SeatHud Compose(
                ConfiguredMatchSceneRuntime runtime, HeroId heroId, string label)
            {
                var seatId = SeatOf(runtime, heroId);
                var client = new SeatMatchClient(runtime.Host.ClientForSeat(seatId));
                var hands = new DirectorHandsUnityView();
                var round = new RoundActionUnityView();
                var host = new GameObject("8.72-seat-" + label);
                var hud = host.AddComponent<ConfiguredMatchHudRenderer>();
                hud.Configure(
                    hands,
                    round,
                    new UndoRestartUnityView(),
                    new DiagnosticLogUnityView(),
                    new MinionChoiceUnityView(),
                    new HeroOverviewsUnityView());
                var roundPresenter = new RoundActionPresenter(client, round, heroId);
                var handsPresenter = new DirectorHandsPresenter(client, hands, heroId);

                // The same two wires the configured runtime makes, so a press on a drawn control
                // travels this seat's own client and nobody else's.
                hands.PrintedActionSubmitted += invocation => roundPresenter.Submit(invocation);
                hands.FastTravelSelected += selection =>
                    roundPresenter.SelectFastTravel(selection);
                round.InvocationSubmitted += invocation => roundPresenter.Submit(invocation);
                round.EndTurnRequested += () => roundPresenter.SubmitEndTurn();
                round.SeatUndoRequested += () => roundPresenter.SubmitSeatUndo();
                var composed = new SeatHud(
                    host, seatId, hud, hands, round, handsPresenter, roundPresenter);
                composed.Refresh();
                return composed;
            }

            /// <summary>Redraws this seat's whole HUD from its own current projection.</summary>
            internal void Refresh()
            {
                roundPresenter.Refresh();
                handsPresenter.Refresh();
                Hud.ApplyExclusiveContext();
            }

            public void Dispose()
            {
                Object.Destroy(host);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The session half every hero owes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.13's Defeat takes his piece off the board, p.13's respawn puts it back on a space
        /// a player clicks, and the Director's RESET rebuilds the whole session from its initial state.
        /// </summary>
        [UnityTest]
        public IEnumerator HisDefeatedPieceRespawnsAndRestartRebuildsTheSession()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedTierThreeEngagedStagedHanu,
                hand: new[] { HanuCardContent.MonkeyBusiness, HanuCardContent.SafeTravels },
                enemyHand: new[] { WaspCardContent.Electrocute, WaspCardContent.KineticRepulse },
                enemySpace: AdjacentEnemy);
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];

            yield return CommitEverySeatOfHis(
                root, HanuCardContent.MonkeyBusiness, WaspCardContent.Electrocute);
            yield return YieldUntilTheNamedSeatActs(runtime, enemy);
            yield return PointTrayAt(root, enemy);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(his).transform.position),
                Is.True,
                "The enemy's printed Attack reaches him where he stands. " + Remaining(runtime));
            yield return null;
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(his)), Is.False,
                "An undefended Attack defeats him and the piece leaves the board. " +
                Remaining(runtime));
            SeatsAgree(runtime, "his own defeat");

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, his, HanuCardContent.SafeTravels);
            yield return YieldUntilTheNamedSeatActs(runtime, his);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    Tile(destination).transform.position),
                Is.True,
                "p.13's respawn is answered by clicking the space. " + Remaining(runtime));
            yield return null;
            Assert.That(Position(runtime, his), Is.EqualTo(destination));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(destination),
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
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None)
                    .Count(view => view.Occupancy.HeroId.Equals(his)),
                Is.EqualTo(1),
                "One hero, one piece - a rebuild that leaves a stale view behind fails here.");
            Assert.That(
                runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(his)).TrayCards,
                Is.Not.Empty,
                "and his tray is reconstructed rather than left blank. " + Remaining(runtime));
            Healthy(runtime);
        }

        /// <summary>
        /// Three pauses of his survive the Director's retained-prefix Undo, and replaying the retained
        /// prefix reproduces exactly the offer that was there.
        ///
        /// <para>The three are chosen because each one is rebuilt from a different thing: the RED
        /// MODAL's bullet choice is rebuilt from a live board reading (which bullets name anybody), the
        /// JOURNEY SUBJECT from a printed Radius with its consumer's own feasibility walk, and the
        /// FRIENDLY PAIR from a half-answered multi-select whose first click has to come back as an
        /// unanswered question rather than as a pair of one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisOwnPausesUndoAndTheReplayReproducesTheirOffers()
        {
            foreach (var face in new[]
            {
                HanuCardContent.TrustedSidekick,
                HanuCardContent.SafeTravels,
                HanuCardContent.MonkeyBusiness
            })
            {
                var isModal = face.Equals(HanuCardContent.TrustedSidekick);
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                    hand: new[] { face, HanuCardContent.HurryUp },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                    allySpace: new HexCoordinate(-1, 1),
                    enemySpace: isModal ? new HexCoordinate(0, 1) : new HexCoordinate(-3, -1),
                    movedMinions: face.Equals(HanuCardContent.MonkeyBusiness)
                        ? new[]
                        {
                            new KeyValuePair<MinionId, HexCoordinate>(
                                new MinionId("blue-ranged-01"), new HexCoordinate(-2, 1))
                        }
                        : null);
                var root = Root();
                var runtime = root.Runtime;

                yield return BeginHanu(
                    root, face, isModal ? ActionKind.Attack : ActionKind.Skill,
                    WaspCardContent.Shock, ArienCardContent.NobleBlade);
                var pause = runtime.PendingEffectChoice;
                Assert.That(pause, Is.Not.Null, face.Value + ". " + Remaining(runtime));
                var node = pause.NodeId;
                var offered = isModal
                    ? OfferedBullets(runtime)
                    : pause.Candidates.Select(candidate => candidate.Value.ToString())
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray();

                if (isModal)
                {
                    yield return Bullet(root, "hanu-red-friendly-bullet");
                }
                else
                {
                    // MILESTONE 8.86: the answer is NAMED rather than taken first off the offer -
                    // every candidate is a unit the printed sentence admits, and the one driven is
                    // picked by its own identity so the replay below answers the same question the
                    // same way.
                    Assert.That(
                        pause.Candidates.All(candidate =>
                            candidate.Value.Kind == EffectValueKind.Hero ||
                            candidate.Value.Kind == EffectValueKind.Minion),
                        Is.True,
                        face.Value + ": the pause names a piece on the board. " + Remaining(runtime));
                    yield return Pick(
                        root,
                        pause.Candidates
                            .OrderBy(candidate => UnitName(candidate.Value), StringComparer.Ordinal)
                            .First());
                }

                OpenDirectorDrawer(root.Hud);
                yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
                yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
                yield return null;

                var restored = runtime.PendingEffectChoice;
                Assert.That(restored, Is.Not.Null,
                    "One press gives the question back unanswered - milestone 8.46b made a pick of " +
                    "exactly one ONE accepted command. " + face.Value + ". " + Remaining(runtime));
                Assert.That(restored.NodeId, Is.EqualTo(node), face.Value + ". " + Remaining(runtime));
                Assert.That(
                    isModal
                        ? OfferedBullets(runtime)
                        : restored.Candidates.Select(candidate => candidate.Value.ToString())
                            .OrderBy(value => value, StringComparer.Ordinal).ToArray(),
                    Is.EqualTo(offered).AsCollection,
                    "and the offer that comes back is the offer that was there. " + face.Value +
                    ". " + Remaining(runtime));

                if (isModal)
                {
                    yield return Bullet(root, "hanu-red-friendly-bullet");
                    Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                        "Replaying the retained prefix reaches the same next question. " +
                        Remaining(runtime));
                }
                else
                {
                    // MILESTONE 8.86. What stood here was `Is.Not.Null.Or.Null`, which is true of
                    // every state a match can be in and asserted nothing at all. The replay answers
                    // the SAME named candidate and the board is read afterwards: a swap moved two
                    // pieces, so the unit the answer named is not standing where it stood.
                    var replayed = runtime.PendingEffectChoice.Candidates
                        .OrderBy(candidate => UnitName(candidate.Value), StringComparer.Ordinal)
                        .First();
                    var replayedUnit = replayed.Value;
                    var replayedFrom = SpaceOf(runtime, replayedUnit);
                    yield return Pick(root, replayed);
                    if (face.Equals(HanuCardContent.SafeTravels))
                    {
                        // "Swap with an enemy hero in radius" is answered whole by one pick, so the
                        // replayed answer has to have exchanged the two pieces.
                        Assert.That(SpaceOf(runtime, replayedUnit), Is.Not.EqualTo(replayedFrom),
                            face.Value + ": replaying the retained prefix performs the swap the " +
                            "pause was about, so the piece it named really left its space. " +
                            Remaining(runtime));
                    }
                    else
                    {
                        // "Swap two friendly units in radius" is a PAIR, so one pick is half an
                        // answer: nothing has moved yet and the same control is standing for the
                        // second member. A journey that asserted a displacement here would be
                        // asserting the engine swapped a pair of one.
                        Assert.That(SpaceOf(runtime, replayedUnit), Is.EqualTo(replayedFrom),
                            face.Value + ": the first half of an unordered pair moves nobody. " +
                            Remaining(runtime));
                        Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                        Assert.That(runtime.PendingEffectChoice.NodeId, Is.EqualTo(node),
                            face.Value + ": and the pair is still asking, on its own control. " +
                            Remaining(runtime));
                    }
                }

                SeatsAgree(runtime, face.Value + " replayed");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// The one pause of his that belongs to SOMEBODY ELSE survives a snapshot restore and is
        /// answered afterwards on the same surface.
        ///
        /// <para>It is the co-move's DISTANCE - "A friendly hero in radius chooses a distance of 1, 2
        /// or 3" - and it is the hardest of his to bring back for the reason Brynn's trap discard was:
        /// the chooser is the hero the card named rather than the acting seat, so a restore that
        /// re-derived the chooser from the actor would look identical from Hanu's side of the table and
        /// be wrong on the friend's. The GREYING has to come back too, because a distance no direction
        /// admits is a live board reading rather than a printed fact.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HisOwnCrossSeatDistanceSurvivesASnapshotRestore()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierThree,
                hand: new[] { HanuCardContent.ThatWay, HanuCardContent.HurryUp },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(-2, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);

            yield return BeginHanu(
                root, HanuCardContent.ThatWay, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("hanu-co-move-distance"));
            Assert.That(modal.ChooserId, Is.EqualTo(ally), Remaining(runtime));
            var offered = OfferedBullets(runtime);
            var greyed = GreyedBullets(runtime);
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            yield return Bullet(root, offered.First());
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(his),
                "Arrange: the direction is Hanu's, so the board has moved past the restored " +
                "moment. " + Remaining(runtime));

            Assert.That(runtime.RestoreSnapshot(captured), Is.True,
                "The captured pause restores. " + Remaining(runtime));
            yield return null;
            yield return null;
            var restored = runtime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(restored.NodeId.Value, Is.EqualTo("hanu-co-move-distance"));
            Assert.That(restored.ChooserId, Is.EqualTo(ally),
                "and it is still the FRIEND's decision rather than the acting seat's. " +
                Remaining(runtime));
            Assert.That(OfferedBullets(runtime), Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there. " + Remaining(runtime));
            Assert.That(GreyedBullets(runtime), Is.EqualTo(greyed).AsCollection,
                "including what was greyed, which is a live board reading rather than a printed " +
                "fact. " + Remaining(runtime));
            SeatsAgree(runtime, "the restored distance");

            yield return Bullet(root, offered.First());
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and answering it on the same surface carries on. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(his), Remaining(runtime));
            Healthy(runtime);
        }
    }
}
