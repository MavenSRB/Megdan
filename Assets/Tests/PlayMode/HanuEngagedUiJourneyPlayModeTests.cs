using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.HanuUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Hanu the Trickster at rulebook p.15's Tier II stage - the two graded Reds, the second journey,
    /// the first co-move, the wider friendly pair and the plain enemy swap - plus the FRIENDLY half of
    /// milestone 8.71a's Undo pair. Milestone 8.71i.
    ///
    /// <para>Milestone 8.71h's two Tier II arms carry the two ladders: the FIRST sibling of every colour
    /// is Even the Odds, There and Back Again and Monkey Twist, and the SECOND is Outnumber, This Way!
    /// and Hear Nothing. A staged hand may take a card out of its owner's own DECK, so a journey names
    /// the face it drives and the arm decides only what is in hand without being asked.</para>
    ///
    /// <para><b>THE CO-MOVE IS A TWO-SEAT JOURNEY, and that is the whole of what This Way! adds.</b>
    /// "A friendly hero in radius chooses a distance of 1 or 2; move both of you that number of spaces
    /// in the same direction of your choice." HA-Q9 splits the sentence between two players: Hanu
    /// targets the friend, the FRIEND answers the distance on their own seat (p.10's "unless the card
    /// text instructs the other hero to make a choice"), and Hanu then answers only the direction. The
    /// distances are a MODAL rather than a list of integers - HA-Q13e - so a distance no direction
    /// admits is greyed by milestone 6.5ac's own channel rather than dropped in silence.</para>
    /// </summary>
    public sealed class HanuEngagedUiJourneyPlayModeTests
    {
        /// <summary>
        /// The minions this fixture moves to leave the co-move exactly ONE legal distance. MEASURED
        /// from Hanu at (-3,1) with the ally beside him at (-2,1): the only direction the pair can
        /// both enter is r-1, which reaches (-3,0)/(-2,0) at one space and (-3,-1)/(-2,-1) at two, so
        /// blocking the ALLY's two-space square is what leaves "1" legal and "2" impossible. The other
        /// two open directions are the two the pair would swap through, and their two-space squares -
        /// (-1,1) for q+1 and (-5,1) for q-1 - are filled by the same staging. Every remaining
        /// direction is already shut by the engagement's own terrain at (-3,3) and (-4,3) and by the
        /// minions it leaves at (-3,2) and (-4,2).
        /// </summary>
        private static KeyValuePair<MinionId, HexCoordinate>[] BoxInTheSecondSpace() =>
            new[]
            {
                new KeyValuePair<MinionId, HexCoordinate>(
                    new MinionId("blue-heavy-01"), new HexCoordinate(-1, 1)),
                new KeyValuePair<MinionId, HexCoordinate>(
                    new MinionId("blue-ranged-01"), new HexCoordinate(-5, 1)),
                new KeyValuePair<MinionId, HexCoordinate>(
                    new MinionId("red-melee-01"), new HexCoordinate(-2, -1))
            };

        /// <summary>The bullet node one printed co-move distance is drawn as.</summary>
        private static string DistanceNode(int distance) =>
            distance == 1 ? "hanu-co-move-one-space"
            : distance == 2 ? "hanu-co-move-two-spaces"
            : "hanu-co-move-three-spaces";

        /// <summary>
        /// Drives one whole co-move at the named printed distance: the friend is targeted on the
        /// board, the DISTANCE is answered on the friend's own seat, and the DIRECTION is answered on
        /// Hanu's - and both pieces travel exactly that far or neither moves.
        /// </summary>
        internal static IEnumerator DriveTheCoMove(
            MatchSceneCompositionRoot root,
            HeroId his,
            HeroId ally,
            int distance,
            int[] printedDistances)
        {
            var runtime = root.Runtime;
            var friend = runtime.PendingEffectChoice;
            Assert.That(friend, Is.Not.Null, Remaining(runtime));
            Assert.That(friend.NodeId.Value, Is.EqualTo("hanu-co-move-friend"));
            Assert.That(friend.ChooserId, Is.EqualTo(his),
                "p.19's Target is HANU's choice - the sentence hands over the distance and nothing " +
                "else. " + Remaining(runtime));
            Assert.That(
                friend.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { ally }).AsCollection,
                "\"A friendly hero in radius\" - his own side, never himself (p.17, composed with " +
                "includeSelf false). " + Remaining(runtime));
            yield return Pick(root, friend.Candidates.Single());

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "\"chooses a distance\" opens its own control. " + Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("hanu-co-move-distance"));
            Assert.That(modal.ChooserId, Is.EqualTo(ally),
                "and it is asked of the FRIENDLY HERO on their own seat - p.10's \"unless the card " +
                "text instructs the other hero to make a choice\", which this text does. " +
                Remaining(runtime));

            var drawn = OfferedBullets(runtime).Concat(GreyedBullets(runtime))
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(
                drawn,
                Is.EqualTo(printedDistances.Select(DistanceNode)
                    .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection,
                "Every printed distance is DRAWN, offered or greyed - a distance no direction " +
                "admits is greyed rather than dropped (HA-Q13e). " + Remaining(runtime));

            // NOT pointed at the friend's own piece first, and that is a measured constraint rather
            // than a shortcut: the distance modal's window covers the middle of the board, so a
            // camera ray at a piece underneath it is refused by `UiPlayer.ClickBoard` exactly as a
            // player's own click would be. WHO is being asked is asserted on the pause's own
            // `ChooserId` and on the seat projections instead, and the answer is pressed on the
            // window the owning seat is shown.
            yield return Bullet(root, DistanceNode(distance));

            var hisWas = Position(runtime, his);
            var allyWas = Position(runtime, ally);
            var direction = runtime.PendingEffectChoice;
            Assert.That(direction, Is.Not.Null,
                "\"in the same direction of your choice\" is the actor's own question. " +
                Remaining(runtime));
            Assert.That(direction.NodeId.Value, Is.EqualTo(DistanceNode(distance)));
            Assert.That(direction.ChooserId, Is.EqualTo(his),
                "and it is HANU's, on his own seat. " + Remaining(runtime));
            Assert.That(OfferedSpaces(runtime), Is.Not.Empty, Remaining(runtime));
            Assert.That(
                OfferedSpaces(runtime).All(space => Distance(space, hisWas) == distance),
                Is.True,
                "Every direction the board admits is offered at exactly the distance the friend " +
                "named. Offered: " +
                string.Join(", ", OfferedSpaces(runtime).Select(space => space.ToString())) + ". " +
                Remaining(runtime));

            yield return Space(root);

            Assert.That(Distance(Position(runtime, his), hisWas), Is.EqualTo(distance),
                Remaining(runtime));
            Assert.That(Distance(Position(runtime, ally), allyWas), Is.EqualTo(distance),
                "\"Both must be moved the full distance, or neither one moves.\" " +
                Remaining(runtime));
            Assert.That(
                new HexCoordinate(
                    Position(runtime, ally).Q - allyWas.Q, Position(runtime, ally).R - allyWas.R),
                Is.EqualTo(new HexCoordinate(
                    Position(runtime, his).Q - hisWas.Q, Position(runtime, his).R - hisWas.R)),
                "and both travel the SAME direction, which is what coupled-direct-move@1 means by " +
                "one displacement. " + Remaining(runtime));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(Position(runtime, his)));
            Assert.That(HeroView(ally).Coordinate, Is.EqualTo(Position(runtime, ally)));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier II
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Even the Odds: Helping Hand's sentence at Attack 4 and Range 4, with an Initiative item.
        ///
        /// <para>The Tier I journey drives both bullets at Range 3; this one measures the WIDER reach
        /// on the one board the narrowed bullet has a subject on, and takes the bullet that needs it -
        /// the enemy hero standing beside his friendly hero and four spaces from him, which Helping
        /// Hand's printed 3 could not have reached.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator EvenTheOddsReachesTheHeroBesideHisFriendlyHeroAtItsPrintedRangeFour()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo,
                hand: new[] { HanuCardContent.EvenTheOdds, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(0, 1),
                enemySpace: new HexCoordinate(1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var enemy = Enemies(runtime)[0];
            Assert.That(Distance(Position(runtime, ally), Position(runtime, enemy)), Is.EqualTo(1),
                "Arrange: the enemy hero stands beside his friendly HERO. " + Remaining(runtime));
            Assert.That(Distance(Position(runtime, his), Position(runtime, enemy)), Is.EqualTo(4),
                "Arrange: FOUR spaces off, which only the Tier II printed Range reaches. " +
                Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.EvenTheOdds, ActionKind.Attack,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            Assert.That(OfferedBullets(runtime), Contains.Item("hanu-red-friendly-bullet"),
                Remaining(runtime));
            yield return Bullet(root, "hanu-red-friendly-bullet");
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "and the hero four spaces off is inside the printed Range 4 the icon carries. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True);
            yield return null;
            yield return ClickDefenseFace(root, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            // THE ATTACK'S OWN OUTCOME (milestone 8.86). The journey proved the REACH and stopped
            // there; an Attack that offered the hero four away and then applied nothing at all would
            // have passed it. The printed outcome here is p.14's Defense, answered and spent.
            Assert.That(
                runtime.Cards.Single(card =>
                    card.OwnerId.Equals(enemy) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The hero at Range 4 really answered the Attack, and the face they answered with is " +
                "spent into their own discard pile. " + Remaining(runtime));
            Assert.That(runtime.PendingDefenseChoice, Is.Null, Remaining(runtime));
            SeatsAgree(runtime, "Even the Odds at Range 4");
            Healthy(runtime);
        }

        /// <summary>
        /// Outnumber: "Choose one - • Target a unit adjacent to you. • Target a MINION in range,
        /// adjacent to your friendly hero."
        ///
        /// <para><b>The noun is the whole of what separates it from Even the Odds</b> (HA-Q5), and the
        /// journey measures exactly that: the same predicate on the same board offers the enemy minion
        /// standing beside his friendly hero and does NOT offer the enemy hero standing beside the same
        /// hero. <c>AttackTargetableUnits.MinionsOnly</c> is what says so.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OutnumberTakesTheMinionBesideHisFriendlyHeroAndNeverTheHero()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo,
                hand: new[] { HanuCardContent.Outnumber, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(-1, 1),
                enemySpace: new HexCoordinate(1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginHanu(
                root, HanuCardContent.Outnumber, ActionKind.Attack,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            Assert.That(OfferedBullets(runtime), Contains.Item("hanu-red-friendly-bullet"),
                "The minion beside his friendly hero is what gives the narrowed bullet a subject. " +
                Remaining(runtime));
            yield return Bullet(root, "hanu-red-friendly-bullet");

            var target = runtime.PendingAttackTargetChoice;
            Assert.That(target, Is.Not.Null, Remaining(runtime));
            Assert.That(target.LegalTargetHeroIds, Is.Empty,
                "\"Target a MINION in range\" - the enemy HERO standing beside the same friendly " +
                "hero is not offered, which is the one thing the noun decides (HA-Q5). " +
                Remaining(runtime));
            Assert.That(target.LegalTargetMinionIds, Is.Not.Empty, Remaining(runtime));
            Assert.That(
                target.LegalTargetMinionIds.All(minion =>
                    Distance(
                        runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(minion))
                            .Coordinate,
                        Position(runtime, ally)) == 1),
                Is.True,
                "and every candidate stands beside his friendly hero. " + Remaining(runtime));
            Assert.That(
                target.LegalTargetMinionIds.All(minion =>
                    Distance(
                        runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(minion))
                            .Coordinate,
                        Position(runtime, his)) <= 4),
                Is.True,
                "inside the printed Range 4. " + Remaining(runtime));

            // MILESTONE 8.86: named by its own id in ordinal order rather than taken first off the
            // offer, so the defeat asserted below is about a piece this journey can state.
            var chosen = target.LegalTargetMinionIds
                .OrderBy(value => value.Value, StringComparer.Ordinal).First();
            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    MinionView(chosen).transform.position),
                Is.True,
                Remaining(runtime));
            yield return null;
            yield return null;
            // THE ATTACK'S OWN OUTCOME (milestone 8.86). What stood here was
            // `Position(enemy) != Position(his)`, which is true of every board two pieces can stand
            // on and therefore proved nothing about this card. p.14 defeats an attacked minion
            // without a Defense step, and that is the printed outcome.
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(chosen)),
                Is.False,
                "\"Target a MINION in range, adjacent to your friendly hero\" - the piece the " +
                "narrowed bullet named is defeated and leaves the board. " + Remaining(runtime));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                "and the enemy HERO the bullet could not name is untouched by it. " +
                Remaining(runtime));
            SeatsAgree(runtime, "Outnumber's minion bullet");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier II: the second journey and the first co-move
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// There and Back Again: Unexpected Journey's three sentences at the wider printed Radius 3.
        ///
        /// <para>The journey stands the enemy hero THREE spaces off, where Unexpected Journey's printed
        /// Radius 2 could not have named him at all, and drives the whole clause: the exchange, the
        /// Active effect the two keywords share, and the mandatory swap back at the turn's own end.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThereAndBackAgainSwapsAtItsWiderRadiusAndSwapsBackAtTheTurnsEnd()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedTierTwoEngagedStagedHanu,
                hand: new[] { HanuCardContent.ThereAndBackAgain, HanuCardContent.MonkeyTwist },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                enemySpace: new HexCoordinate(0, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];
            var hisStood = Position(runtime, his);
            var theirStood = Position(runtime, enemy);
            Assert.That(Distance(hisStood, theirStood), Is.EqualTo(3),
                "Arrange: three spaces off, which only the Tier II printed Radius reaches. " +
                Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.ThereAndBackAgain, ActionKind.Skill, WaspCardContent.Shock);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("journey-subject"));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection, Remaining(runtime));
            yield return Pick(root, subject.Candidates.Single());

            Assert.That(Position(runtime, his), Is.EqualTo(theirStood), Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(hisStood));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, his, HanuCardContent.ThereAndBackAgain)),
                Is.True,
                "and the same one effect carries both printed keywords. " + Remaining(runtime));
            SeatsAgree(runtime, "There and Back Again out");

            yield return EndTheWholeTurn(root);
            yield return null;
            yield return null;
            Assert.That(Position(runtime, his), Is.EqualTo(hisStood),
                "\"End of turn: Swap with that hero regardless of radius and immunity.\" " +
                Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(theirStood));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, his, HanuCardContent.ThereAndBackAgain)),
                Is.False,
                "and it expires at the checkpoint it fires on. " + Remaining(runtime));
            SeatsAgree(runtime, "There and Back Again home");
            Healthy(runtime);
        }

        /// <summary>
        /// This Way!: "A friendly hero in radius chooses a distance of 1 or 2; move both of you that
        /// number of spaces in the same direction of your choice. ( Both must be moved the full
        /// distance, or neither one moves )."
        ///
        /// <para><b>Both printed distances are driven, each in its own run</b>, and each one is answered
        /// on the FRIEND's seat while the direction is answered on Hanu's. Then a third run stands the
        /// pair on a MEASURED board where only one space of travel is legal in any direction at all -
        /// see <see cref="BoxInTheSecondSpace"/> - and the two-space bullet is drawn GREYED rather than
        /// silently dropped, which is HA-Q13e's whole reason for composing the distances as a modal.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ThisWayLetsTheFriendChooseTheDistanceAndHanuTheDirection()
        {
            foreach (var distance in new[] { 1, 2 })
            {
                yield return StageHanu(
                    SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo,
                    hand: new[] { HanuCardContent.ThisWay, HanuCardContent.MonkeyTrick },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                    allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                    allySpace: new HexCoordinate(-2, 1));
                var root = Root();
                var runtime = root.Runtime;
                var his = Hanu(runtime);
                var ally = Ally(runtime);
                Assert.That(Distance(Position(runtime, his), Position(runtime, ally)),
                    Is.LessThanOrEqualTo(3),
                    "Arrange: the friendly hero is inside the printed Radius 3. " +
                    Remaining(runtime));

                yield return BeginHanu(
                    root, HanuCardContent.ThisWay, ActionKind.Skill,
                    WaspCardContent.Shock, ArienCardContent.NobleBlade);
                yield return DriveTheCoMove(root, his, ally, distance, new[] { 1, 2 });
                SeatsAgree(runtime, "This Way! at " + distance);
                Healthy(runtime);
            }

            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo,
                hand: new[] { HanuCardContent.ThisWay, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(-2, 1),
                movedMinions: BoxInTheSecondSpace());
            var boxedRoot = Root();
            var boxedRuntime = boxedRoot.Runtime;
            var boxedHanu = Hanu(boxedRuntime);
            var boxedAlly = Ally(boxedRuntime);

            yield return BeginHanu(
                boxedRoot, HanuCardContent.ThisWay, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            var friend = boxedRuntime.PendingEffectChoice;
            Assert.That(friend.NodeId.Value, Is.EqualTo("hanu-co-move-friend"),
                Remaining(boxedRuntime));
            yield return Pick(boxedRoot, friend.Candidates.Single());

            Assert.That(boxedRuntime.PendingEffectChoice.ChooserId, Is.EqualTo(boxedAlly),
                Remaining(boxedRuntime));
            Assert.That(GreyedBullets(boxedRuntime),
                Is.EqualTo(new[] { DistanceNode(2) }).AsCollection,
                "Milestone 6.5ac: a distance no direction admits is drawn with its label and no " +
                "command behind it. Offered: " +
                string.Join(", ", OfferedBullets(boxedRuntime)) + ". " + Remaining(boxedRuntime));
            Assert.That(OfferedBullets(boxedRuntime),
                Is.EqualTo(new[] { DistanceNode(1) }).AsCollection,
                "and the distance the board does admit is the answer left to give. " +
                Remaining(boxedRuntime));
            Assert.That(
                boxedRuntime.RoundView.Model.UnavailableEffectChoices, Has.Count.EqualTo(1),
                "THE RENDERED TIER AGREES WITH THE AUTHORITY. " + Remaining(boxedRuntime));

            yield return Bullet(boxedRoot, DistanceNode(1));
            Assert.That(OfferedSpaces(boxedRuntime), Is.Not.Empty, Remaining(boxedRuntime));
            // MILESTONE 8.86: the boxed arm used to press the one legal distance and then assert
            // only that the pause had closed, so a co-move that greyed the two correctly and then
            // moved nobody would have passed it. Both pieces are read before and after.
            var boxedHisWas = Position(boxedRuntime, boxedHanu);
            var boxedAllyWas = Position(boxedRuntime, boxedAlly);
            var boxedTo = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), OfferedSpaces(boxedRuntime));
            yield return Space(boxedRoot, boxedTo);
            Assert.That(Position(boxedRuntime, boxedHanu), Is.EqualTo(boxedTo),
                "The one distance the board admits is walked, and he is standing where it put " +
                "him. " + Remaining(boxedRuntime));
            Assert.That(Distance(Position(boxedRuntime, boxedHanu), boxedHisWas), Is.EqualTo(1),
                Remaining(boxedRuntime));
            Assert.That(Distance(Position(boxedRuntime, boxedAlly), boxedAllyWas), Is.EqualTo(1),
                "\"Both must be moved the full distance, or neither one moves.\" " +
                Remaining(boxedRuntime));
            Assert.That(boxedRuntime.PendingEffectChoice, Is.Null, Remaining(boxedRuntime));
            SeatsAgree(boxedRuntime, "the greyed distance");
            Healthy(boxedRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier II
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Monkey Twist: "Swap two friendly units in radius", at the printed Radius 2.
        ///
        /// <para>Monkey Trick's own journey swaps two minions standing in its Radius 1; this one stands
        /// the second member TWO spaces off, where the Tier I face could not have named it, and swaps a
        /// friendly HERO with a friendly minion - which is the other half of "units": p.19 makes the
        /// pair heroes and minions alike, and the only unit p.17 keeps out of it is Hanu.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator MonkeyTwistSwapsAFriendlyHeroAndAMinionAtItsWiderRadius()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAllyAtTierTwo,
                hand: new[] { HanuCardContent.MonkeyTwist, HanuCardContent.HurryUp },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak },
                allySpace: new HexCoordinate(-1, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            Assert.That(Distance(Position(runtime, his), Position(runtime, ally)), Is.EqualTo(2),
                "Arrange: his friendly hero is two spaces off, inside the printed Radius 2 and " +
                "outside Monkey Trick's printed 1. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.MonkeyTwist, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            var pair = runtime.PendingEffectChoice;
            Assert.That(pair, Is.Not.Null, Remaining(runtime));
            Assert.That(pair.NodeId.Value, Is.EqualTo("green-friendly-swap-pair"));
            Assert.That(
                pair.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(ally)),
                Is.True,
                "\"two friendly UNITS\" - p.19 makes a hero one of them. " + Remaining(runtime));
            Assert.That(
                pair.Candidates.Any(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(his)),
                Is.False,
                "and p.17 keeps Hanu himself out of his own pair. " + Remaining(runtime));

            var allyWas = Position(runtime, ally);
            yield return Pick(
                root,
                pair.Candidates.Single(candidate =>
                    candidate.Value.Kind == EffectValueKind.Hero &&
                    candidate.Value.HeroId.Equals(ally)));
            var second = runtime.PendingEffectChoice;
            Assert.That(second, Is.Not.Null, Remaining(runtime));
            // MILESTONE 8.86: every minion still offered as the pair's second member is a FRIENDLY
            // one inside the printed Radius 2 - which is what "two friendly units in radius" admits -
            // and the one taken is named by its own id rather than by the offer's order.
            var hisSide = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(his)).TeamSide;
            Assert.That(
                second.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                    .All(candidate =>
                        runtime.ReadModel.Minions.Single(value =>
                            value.MinionId.Equals(candidate.Value.MinionId)).TeamSide == hisSide &&
                        Distance(
                            runtime.ReadModel.Minions.Single(value =>
                                value.MinionId.Equals(candidate.Value.MinionId)).Coordinate,
                            Position(runtime, his)) <= 2),
                Is.True,
                "p.17's Friendly, inside the printed Radius 2. " + Remaining(runtime));
            var minion = second.Candidates
                .Where(candidate => candidate.Value.Kind == EffectValueKind.Minion)
                .OrderBy(candidate => candidate.Value.MinionId.Value, StringComparer.Ordinal)
                .First();
            var minionId = minion.Value.MinionId;
            var minionWas = MinionView(minionId).Occupancy.Coordinate;
            yield return Pick(root, minion);

            Assert.That(Position(runtime, ally), Is.EqualTo(minionWas),
                "p.19's Swap exchanges the two friendly units it named. " + Remaining(runtime));
            Assert.That(MinionView(minionId).Occupancy.Coordinate, Is.EqualTo(allyWas));
            Assert.That(HeroView(ally).Coordinate, Is.EqualTo(minionWas));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "Monkey Twist prints no second sentence. " + Remaining(runtime));
            SeatsAgree(runtime, "Monkey Twist");
            Healthy(runtime);
        }

        /// <summary>
        /// Hear Nothing: "Swap with an enemy hero in radius." - the whole face, at the printed Radius 3.
        ///
        /// <para>It is the three journeys' first sentence with neither keyword after it, and that
        /// absence is what this journey measures: the pieces exchange, and NO Active effect stands
        /// afterwards - nobody is made immune and nothing is owed at the turn's end.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HearNothingSwapsWithTheEnemyAndLeavesNothingStanding()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedHanu,
                hand: new[] { HanuCardContent.HearNothing, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                enemySpace: new HexCoordinate(0, 1));
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var enemy = Enemies(runtime)[0];
            var hisStood = Position(runtime, his);
            var theirStood = Position(runtime, enemy);
            Assert.That(Distance(hisStood, theirStood), Is.EqualTo(3),
                "Arrange: three spaces off, inside the printed Radius 3. " + Remaining(runtime));

            yield return BeginHanu(
                root, HanuCardContent.HearNothing, ActionKind.Skill, WaspCardContent.Shock);
            var subject = runtime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(runtime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("green-enemy-swap-subject"));
            Assert.That(
                subject.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection, Remaining(runtime));
            yield return Pick(root, subject.Candidates.Single());

            Assert.That(Position(runtime, his), Is.EqualTo(theirStood), Remaining(runtime));
            Assert.That(Position(runtime, enemy), Is.EqualTo(hisStood));
            Assert.That(HeroView(his).Coordinate, Is.EqualTo(theirStood));
            Assert.That(
                runtime.IsActiveEffectSource(
                    CardIdOf(runtime, his, HanuCardContent.HearNothing)),
                Is.False,
                "Hear Nothing prints no keyword at all, so nothing stands after it - which is the " +
                "one thing that separates it from the three journeys. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and it prints no trailing move either. " + Remaining(runtime));

            yield return EndTheWholeTurn(root);
            yield return null;
            Assert.That(Position(runtime, his), Is.EqualTo(theirStood),
                "and the turn's end swaps nobody back. " + Remaining(runtime));
            SeatsAgree(runtime, "Hear Nothing");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // The friendly half of milestone 8.71a's Undo pair
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hurry up! on a FRIENDLY hero, and the rewind that is still his to take.
        ///
        /// <para><b>The other half of 8.71a's pair</b>, whose enemy side the Tier I fixture drives.
        /// Milestone 8.42e's interaction wall is OPPONENT-SCOPED by the user's own decision - "a
        /// friendly buff or an ally's move stays rewindable" - so rewriting an ALLY's printed Initiative
        /// changes nothing in the fingerprint and the seat's undo floor does not move. The Director's
        /// own UNDO then steps the rewrite back, and the ally's rail badge reads the number their card
        /// prints again, which is the reading a player would use to check.</para>
        ///
        /// <para>The seat-scoped square is a SEAT projection's fact and a Director sandbox publishes no
        /// seat of its own, so the availability is read on the host's seat projection - the value the
        /// square is drawn from - and the rewind is pressed on the control this scene really has.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HurryUpOnAFriendLeavesHisOwnRewindOpenAndItRestoresTheOrder()
        {
            yield return StageHanu(
                SceneCardContent.VerifiedStagedHanuWithAlly,
                hand: new[] { HanuCardContent.HurryUp, HanuCardContent.MonkeyTrick },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.ExpertDuelist, ArienCardContent.NobleBlade });
            var root = Root();
            var runtime = root.Runtime;
            var his = Hanu(runtime);
            var ally = Ally(runtime);
            var hisSeat = SeatOf(runtime, his);

            yield return BeginHanu(
                root, HanuCardContent.HurryUp, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.ExpertDuelist);
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("10"),
                "Arrange: Expert Duelist prints Initiative 10. " + Remaining(runtime));

            var target = runtime.PendingEffectChoice;
            Assert.That(target.NodeId.Value, Is.EqualTo("hanu-hurry-up-target"), Remaining(runtime));
            yield return Pick(
                root,
                target.Candidates.Single(candidate => candidate.Value.HeroId.Equals(ally)));
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("11"),
                "\"a hero in range\" names no side, so his own teammate is a legal subject. " +
                Remaining(runtime));
            SeatsAgree(runtime, "the friendly rewrite");

            Assert.That(runtime.Host.ProjectionForSeat(hisSeat).IsSeatUndoAvailable, Is.True,
                "8.42e's wall is opponent-scoped by the user's own decision, so a rewrite on his own " +
                "side leaves the rewind where it was. " + Remaining(runtime));

            var accepted = runtime.AcceptedCommands.Count;
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands.Count, Is.LessThan(accepted),
                "The press reaches the host's retained-prefix rewind. " + Remaining(runtime));
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("10"),
                "and the rewind restores the order the rewrite changed - p.10's own state, back " +
                "where it was. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "and the selection it undid is standing again, unanswered. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("hanu-hurry-up-target"), Remaining(runtime));
            yield return Pick(
                root,
                runtime.PendingEffectChoice.Candidates.Single(candidate =>
                    candidate.Value.HeroId.Equals(ally)));
            Assert.That(InitiativeBadgeOf(runtime, ally), Is.EqualTo("11"),
                "and answering it again writes the same eleven. " + Remaining(runtime));
            SeatsAgree(runtime, "the replayed rewrite");
            Healthy(runtime);
        }
    }
}
