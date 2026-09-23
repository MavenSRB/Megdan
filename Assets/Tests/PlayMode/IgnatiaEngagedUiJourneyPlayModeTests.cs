using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.IgnatiaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Ignatia the Mad's six Tier II faces, and the two halves of milestone 8.42e's rewind wall her set
    /// puts on either side of it. Milestone 8.75c.
    ///
    /// <para><b>The Tier II faces are reached by a STAGED HAND on the Tier I boards</b>, for the reason
    /// the sibling fixture's remarks give: a staged hand takes a card out of its owner's own deck, and
    /// the two starting-hand configurations are the only pair of hers that exists under BOTH coin
    /// faces. Every journey below therefore runs once on
    /// <see cref="SceneCardContent.VerifiedEngagedStagedIgnatia"/> and once on
    /// <see cref="SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon"/> - the same board, the same
    /// pieces, the coin turned over - which is what makes each pair of offers below a statement about
    /// the CARD rather than about the board it was measured on.</para>
    ///
    /// <para><b>Except the two sun pushes, which need an ally.</b> "Move a FRIENDLY hero in radius 2
    /// spaces in a straight line" has no candidate on a one-versus-one board, because rulebook p.17's
    /// Friendly never includes you - so Searing Heat's sun arm stands on
    /// <see cref="SceneCardContent.VerifiedStagedIgnatiaWithAlly"/>, which is the one configuration of
    /// hers that seats a second hero on her side. That board's coin shows the SUN, which is exactly the
    /// face the friendly arm is on.</para>
    /// </summary>
    public sealed class IgnatiaEngagedUiJourneyPlayModeTests
    {
        // -------------------------------------------------------------------------------------------
        // The Red ladder's Tier II: the straight line at a wider Range, and the two ends of the disk
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Erratic Fireblast: "☀: Target a unit in range not in a straight line. ☾: Target a unit in
        /// range in a straight line."
        ///
        /// <para>Playing with Fire's two printed lines at the Tier II Range 3, which is a WIDER disk and
        /// therefore a different pair of sets - the enemy melee three away at (-4,4) joins the sun arm
        /// and nothing joins the moon arm, because the third ring of her six files is empty on this
        /// board. That is the measurement the journey rests on and the reason both offers are asserted
        /// as exact sets.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator ErraticFireblastTakesTheUnitOffEveryFileAtItsWiderRange()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.ErraticFireblast, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.ErraticFireblast, ActionKind.Attack,
                    WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(
                    runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo(onTheMoon
                        ? "ignatia-attack-moon-select"
                        : "ignatia-attack-sun-select"),
                    Remaining(runtime));
                Assert.That(
                    OfferedUnits(runtime),
                    onTheMoon
                        ? Is.EqualTo(new[] { RedMeleeOnAFile.Value, RedRanged.Value }
                            .OrderBy(value => value, StringComparer.Ordinal).ToArray()).AsCollection
                        : Is.EqualTo(new[]
                            {
                                enemy.Value, RedMeleeOffEveryFile.Value, RedMeleeAtThree.Value
                            }.OrderBy(value => value, StringComparer.Ordinal).ToArray())
                            .AsCollection,
                    "The printed Range 3 widens the disk and the straight-line rule splits it: the " +
                    "enemy melee at (-4,4) is three away and off every file, and nothing at all " +
                    "stands three away ON one. " + Remaining(runtime));

                var struck = onTheMoon ? RedRanged : RedMeleeAtThree;
                yield return Pick(root, CandidateFor(runtime, struck));
                yield return null;
                yield return null;
                // THE ATTACK'S OWN OUTCOME (milestone 8.86). The journey proved the two OFFERS at the
                // wider Range and stopped there, so a card that lit the right sets and then applied
                // nothing would have passed it. p.14 defeats an attacked minion.
                Assert.That(
                    runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(struck)),
                    Is.False,
                    "The piece the wider disk admitted is defeated by the Attack that named it. " +
                    Remaining(runtime));
                SeatsAgree(runtime, onTheMoon ? "the wider straight line" : "its wider complement");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Crack of Doom: "☀: Target a unit adjacent to you. ☾: Target a unit at maximum range."
        ///
        /// <para><b>The two ends of the same disk, and neither is a ceiling.</b> RULING 1 reads "at
        /// maximum range" as an EQUALITY against the printed Range Total, so the moon arm offers the
        /// enemy pieces standing at exactly five and not one of the four enemy pieces closer than that.
        /// The sun arm is p.19's ring, where the one enemy piece is the ranged minion the engagement
        /// leaves beside her.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator CrackOfDoomTakesTheAdjacentUnitAndTheUnitAtMaximumRange()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[] { IgnatiaCardContent.CrackOfDoom, IgnatiaCardContent.Equilibrium },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var hers = Ignatia(runtime);

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.CrackOfDoom, ActionKind.Attack, WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
                Assert.That(
                    runtime.PendingEffectChoice.NodeId.Value,
                    Is.EqualTo(onTheMoon
                        ? "ignatia-attack-moon-select"
                        : "ignatia-attack-sun-select"),
                    Remaining(runtime));
                if (onTheMoon)
                {
                    // THE OFFER IS COMPARED AGAINST THE BOARD RATHER THAN AGAINST A LIST, because
                    // "at maximum range" is an equality and the honest way to assert an equality is
                    // to compute it: every enemy unit standing at exactly the printed Range Total.
                    var herSide = runtime.Setup.Heroes
                        .Single(hero => hero.HeroId.Equals(hers)).TeamSide;
                    var standing = Position(runtime, hers);
                    var atMaximumRange = runtime.ReadModel.Minions
                        .Where(minion => minion.TeamSide != herSide &&
                            Distance(minion.Coordinate, standing) == 5 &&
                            minion.MinionType != StandardMinionType.Heavy)
                        .Select(minion => minion.MinionId.Value)
                        .Concat(Enemies(runtime)
                            .Where(hero => Distance(Position(runtime, hero), standing) == 5)
                            .Select(hero => hero.Value))
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray();
                    Assert.That(atMaximumRange, Is.Not.Empty,
                        "Arrange: somebody stands at exactly the printed Range 5. " +
                        Remaining(runtime));
                    Assert.That(OfferedUnits(runtime), Is.EqualTo(atMaximumRange).AsCollection,
                        "and the offer is exactly that set, computed off the live board. " +
                        Remaining(runtime));
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion => minion.TeamSide != herSide &&
                            Distance(minion.Coordinate, standing) < 5),
                        Is.True,
                        "with enemy pieces standing CLOSER than five that a ceiling would have " +
                        "admitted and this equality does not. " + Remaining(runtime));

                    // THE SECOND PIECE AT EXACTLY FIVE, and why the set above leaves it out.
                    // MEASURED at this milestone's second Unity run: the enemy HEAVY stands at
                    // (-7,6), the same five spaces away and on the board, and rulebook p.22 shields
                    // a Heavy from being targeted at all while its own side's minions stand with it
                    // inside the current Battle Zone. So the equality is not the only thing
                    // narrowing this offer, and saying so is what keeps the exact set honest rather
                    // than fitted - it is also why the friendly heavy is no candidate for her swaps.
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion =>
                            minion.MinionId.Equals(RedHeavyAtFive) &&
                            Distance(minion.Coordinate, standing) == 5),
                        Is.True,
                        "Arrange: the enemy heavy stands at exactly the printed Range too. " +
                        Remaining(runtime));
                    Assert.That(OfferedUnits(runtime), Has.No.Member(RedHeavyAtFive.Value),
                        "and p.22's Heavy immunity keeps it out of the offer rather than letting a " +
                        "player spend the card on a target the rules would refuse. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(OfferedUnits(runtime),
                        Is.EqualTo(new[] { RedRanged.Value }).AsCollection,
                        "☀ \"a unit adjacent to you\" - p.19's ring, enemies only. " +
                        Remaining(runtime));
                }

                var taken = onTheMoon ? RedMeleeAtFive : RedRanged;
                yield return Pick(root, CandidateFor(runtime, taken));
                yield return null;
                yield return null;
                // THE ATTACK'S OWN OUTCOME (milestone 8.86): the ring and the maximum-range equality
                // were asserted over the offer; the piece at each end is now asserted to have been
                // defeated by the Attack that named it.
                Assert.That(
                    runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(taken)),
                    Is.False,
                    (onTheMoon ? "☾ at exactly the printed Range" : "☀ in p.19's ring") +
                    " - p.14 defeats an attacked minion. " + Remaining(runtime));
                SeatsAgree(runtime, onTheMoon ? "the maximum range" : "the ring");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Blue ladder's Tier II: the wider discard, and the push
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Spontaneous Immolation: "☀: An enemy hero in radius adjacent to a token or to a minion
        /// discards a card, if able. ☾: Remove an enemy minion in radius adjacent to an enemy hero."
        ///
        /// <para>Abrupt Combustion's two printed lines at the Tier II Radius 4. The board is restaged
        /// the same way its Tier I is and for the same measured reason - at setup no enemy minion
        /// stands in the enemy hero's ring - and the wider disk changes neither offer on this board,
        /// which is itself worth asserting: a Radius that admitted a second candidate would mean the
        /// Tier I journey's exact sets were an accident of the smaller disk.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SpontaneousImmolationCarriesTheDiscardLineAtItsWiderRadius()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.SpontaneousImmolation, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[]
                    {
                        WaspCardContent.Shock, WaspCardContent.Electrocute,
                        WaspCardContent.KineticBlast
                    },
                    enemySpace: EnemyOnHerFile);
                var root = Root();
                var runtime = root.Runtime;
                var enemy = Enemies(runtime)[0];

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.SpontaneousImmolation, ActionKind.Skill,
                    WaspCardContent.Shock);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));

                if (onTheMoon)
                {
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                        Is.EqualTo("ignatia-discard-moon-select"));
                    Assert.That(OfferedUnits(runtime),
                        Is.EqualTo(new[] { RedMeleeOffEveryFile.Value }).AsCollection,
                        "The wider Radius 4 admits no second enemy minion standing beside an enemy " +
                        "hero on this board. " + Remaining(runtime));
                    yield return Pick(root, CandidateFor(runtime, RedMeleeOffEveryFile));
                    yield return null;
                    Assert.That(
                        runtime.ReadModel.Minions.Any(minion =>
                            minion.MinionId.Equals(RedMeleeOffEveryFile)),
                        Is.False,
                        "and the Tier II line still REMOVES rather than defeats. " +
                        Remaining(runtime));
                }
                else
                {
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value,
                        Is.EqualTo("ignatia-discard-sun-select"));
                    Assert.That(OfferedUnits(runtime),
                        Is.EqualTo(new[] { enemy.Value }).AsCollection, Remaining(runtime));
                    var held = HeldBy(runtime, enemy).Length;
                    yield return Pick(root, CandidateFor(runtime, enemy));
                    Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemy),
                        Remaining(runtime));
                    yield return ClickACardFace(root);
                    Assert.That(HeldBy(runtime, enemy), Has.Length.EqualTo(held - 1),
                        Remaining(runtime));
                }

                yield return null;
                SeatsAgree(runtime, "the Tier II discard line");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Searing Heat: "☀: Move a friendly hero in radius 2 spaces in a straight line. ☾: Move an
        /// enemy hero in radius 2 spaces in a straight line."
        ///
        /// <para><b>The straight line is the OFFER's, and milestone 8.75b is what made that true.</b>
        /// A direct Move is rulebook p.11's reachability, which turns corners; composed that way this
        /// card offered the whole disk at the printed distance - ten spaces where the six files reach
        /// four - and the printed three words did nothing. The correction anchors
        /// <c>choose-line-destination@1</c> at the CHOSEN hero's space, so every candidate below is the
        /// end of one of that hero's own six files and nothing else is lit.</para>
        ///
        /// <para><b>And an obstacle is what closes a file.</b> The ally stands at (-2,0), where exactly
        /// ONE of p.19's six directions leaves two free playable spaces - the map's own terrain, the
        /// water ring and the pieces already standing there close the other five - so the sun offer is
        /// a single space, measured. The enemy hero at (-2,2) has three.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator SearingHeatPushesAFriendUnderTheSunAndAnEnemyUnderTheMoon()
        {
            // ☀ a FRIENDLY hero - the ally board, whose coin shows the sun.
            yield return StageIgnatia(
                SceneCardContent.VerifiedStagedIgnatiaWithAlly,
                hand: new[] { IgnatiaCardContent.SearingHeat, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak });
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            var ally = Ally(sunRuntime);
            Assert.That(ally.IsDefined, Is.True, "Arrange: the ally board seats three heroes.");
            Assert.That(
                Distance(Position(sunRuntime, ally), Position(sunRuntime, Ignatia(sunRuntime))),
                Is.EqualTo(1),
                "Arrange: the friendly hero stands inside the printed Radius 3. " +
                Remaining(sunRuntime));

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.SearingHeat, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            var subject = sunRuntime.PendingEffectChoice;
            Assert.That(subject, Is.Not.Null, Remaining(sunRuntime));
            Assert.That(subject.NodeId.Value, Is.EqualTo("ignatia-push-sun-select"));
            Assert.That(OfferedUnits(sunRuntime), Is.EqualTo(new[] { ally.Value }).AsCollection,
                "p.17: \"A friendly unit means another unit on your team. It never includes you.\" " +
                Remaining(sunRuntime));
            var stood = Position(sunRuntime, ally);
            yield return Pick(sunRoot, CandidateFor(sunRuntime, ally));

            Assert.That(sunRuntime.PendingEffectChoice, Is.Not.Null, Remaining(sunRuntime));
            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-sun-line"));
            Assert.That(
                OfferedSpaces(sunRuntime),
                Is.EqualTo(new[] { new HexCoordinate(-4, 0) }).AsCollection,
                "\"2 spaces in a straight line\", walked from HIS square rather than from hers: " +
                "five of his six directions are closed by terrain or by a piece, so the one open " +
                "file is the whole offer. " + Remaining(sunRuntime));
            yield return Space(sunRoot, new HexCoordinate(-4, 0));
            yield return null;
            Assert.That(Position(sunRuntime, ally), Is.EqualTo(new HexCoordinate(-4, 0)),
                "and the friendly hero is the piece that moved. " + Remaining(sunRuntime));
            Assert.That(HeroView(ally).Coordinate, Is.EqualTo(new HexCoordinate(-4, 0)));
            Assert.That(Distance(Position(sunRuntime, ally), stood), Is.EqualTo(2),
                "exactly the printed two spaces. " + Remaining(sunRuntime));
            Assert.That(
                Position(sunRuntime, Ignatia(sunRuntime)), Is.EqualTo(HerSpace),
                "while she stands where she stood - it is his move, not hers. " +
                Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "the friendly push");
            Healthy(sunRuntime);

            // ☾ an ENEMY hero.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.SearingHeat, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var enemy = Enemies(moonRuntime)[0];

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.SearingHeat, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-moon-select"));
            Assert.That(OfferedUnits(moonRuntime), Is.EqualTo(new[] { enemy.Value }).AsCollection,
                Remaining(moonRuntime));
            var enemyStood = Position(moonRuntime, enemy);
            yield return Pick(moonRoot, CandidateFor(moonRuntime, enemy));

            Assert.That(moonRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-moon-line"));
            Assert.That(
                OfferedSpaces(moonRuntime)
                    .OrderBy(value => value.Q).ThenBy(value => value.R).ToArray(),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-2, 0), new HexCoordinate(-2, 4), new HexCoordinate(0, 2)
                    }).AsCollection,
                "Three of the enemy hero's six files hold two free spaces, and every candidate is " +
                "the end of one - never a space two steps of ordinary walking away. " +
                Remaining(moonRuntime));
            Assert.That(
                OfferedSpaces(moonRuntime).All(space => Distance(space, enemyStood) == 2),
                Is.True,
                "\"2 spaces\" is a count rather than a ceiling. " + Remaining(moonRuntime));
            yield return Space(moonRoot, new HexCoordinate(-2, 4));
            yield return null;
            Assert.That(Position(moonRuntime, enemy), Is.EqualTo(new HexCoordinate(-2, 4)),
                Remaining(moonRuntime));
            Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(new HexCoordinate(-2, 4)));
            SeatsAgree(moonRuntime, "the enemy push");
            Healthy(moonRuntime);
        }

        // -------------------------------------------------------------------------------------------
        // The Green ladder's Tier II: the longer wake, and the swap
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Path of Cinders: "☀: Move up to 3 spaces in a straight line. Place a magma token in each
        /// empty space you moved through, or out of. ☾: Place up to 3 magma tokens in radius."
        ///
        /// <para>Path of Ashes at the Tier II count, which is exactly the size of her physical supply -
        /// USER-SETTLED 2026-09-22, "Magma token count is 3". A three-space walk lays three tokens and
        /// empties the pile; the moon line's three placements are three separate decisions, and this
        /// journey refuses the last of them, which is the printed "up to" a player presses. Neither
        /// face needs p.18's short supply step, which is Path of Flames' business.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PathOfCindersLaysThreeTokensInItsWakeAndRefusesItsThirdPlacement()
        {
            // ☀ the wake of a three-space file.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PathOfCinders, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var sunRoot = Root();
            var sunRuntime = sunRoot.Runtime;
            var hers = Ignatia(sunRuntime);

            yield return BeginIgnatia(
                sunRoot, IgnatiaCardContent.PathOfCinders, ActionKind.Skill, WaspCardContent.Shock);
            Assert.That(sunRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-magma-sun-line"));
            Assert.That(OfferedSpaces(sunRuntime), Has.Member(new HexCoordinate(0, 1)),
                "\"up to 3 spaces in a straight line\" reaches the end of her longest free file. " +
                Remaining(sunRuntime));
            Assert.That(
                OfferedSpaces(sunRuntime).All(space =>
                    Distance(space, Position(sunRuntime, hers)) <= 3),
                Is.True,
                Remaining(sunRuntime));

            yield return Space(sunRoot, new HexCoordinate(0, 1));
            yield return null;
            Assert.That(Position(sunRuntime, hers), Is.EqualTo(new HexCoordinate(0, 1)));
            Assert.That(
                MagmaSpaces(sunRuntime),
                Is.EqualTo(new[]
                    {
                        new HexCoordinate(-3, 1), new HexCoordinate(-2, 1),
                        new HexCoordinate(-1, 1)
                    }).AsCollection,
                "The space she moved out of and the two she moved through - three tokens, which is " +
                "the whole supply, and never the space she now stands on. " + Remaining(sunRuntime));
            SeatsAgree(sunRuntime, "the longer wake");
            Healthy(sunRuntime);

            // ☾ three scattered placements.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon,
                hand: new[] { IgnatiaCardContent.PathOfCinders, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var moonRoot = Root();
            var moonRuntime = moonRoot.Runtime;
            var moonHer = Ignatia(moonRuntime);

            yield return BeginIgnatia(
                moonRoot, IgnatiaCardContent.PathOfCinders, ActionKind.Skill, WaspCardContent.Shock);
            for (var placement = 1; placement <= 2; placement++)
            {
                var choice = moonRuntime.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null,
                    "\"up to 3\" is three decisions. " + Remaining(moonRuntime));
                Assert.That(choice.NodeId.Value,
                    Is.EqualTo("ignatia-magma-moon-place-" + placement));
                Assert.That(
                    OfferedSpaces(moonRuntime).All(space =>
                        Distance(space, Position(moonRuntime, moonHer)) <= 4),
                    Is.True,
                    "and every space is inside the printed Radius 4. " + Remaining(moonRuntime));
                yield return Space(moonRoot);
            }

            var last = moonRuntime.PendingEffectChoice;
            Assert.That(last, Is.Not.Null, Remaining(moonRuntime));
            Assert.That(last.NodeId.Value, Is.EqualTo("ignatia-magma-moon-place-3"));
            Assert.That(last.DeclineInvocation, Is.Not.Null,
                "\"UP TO 3\" - the third placement is refusable on its own. " +
                Remaining(moonRuntime));
            yield return May(moonRoot, accept: false);
            Assert.That(MagmaSpaces(moonRuntime), Has.Length.EqualTo(2),
                "Two tokens placed and one kept, which is what \"up to\" means when a player says " +
                "so. " + Remaining(moonRuntime));
            Assert.That(moonRuntime.PendingEffectChoice, Is.Null, Remaining(moonRuntime));
            SeatsAgree(moonRuntime, "the refused third placement");
            Healthy(moonRuntime);
        }

        /// <summary>
        /// Unstable Portal: "☀: Swap with a friendly unit in radius. ☾: Swap with an enemy unit in
        /// radius."
        ///
        /// <para><b>p.18's immunity is enforced by the candidate set rather than by a greyed control</b>
        /// - milestone 8.20b's line for Wasp's Magnetic Dagger - so a unit this hero cannot exchange
        /// with is left out rather than offered and then refused, which is what the Swap named as the
        /// selection's consumer buys. What the offer DOES carry is asserted as a property - every
        /// candidate stands on the side the printed line names - rather than as a list, because which
        /// minions a composed match fields is a fact about the staging.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator UnstablePortalSwapsWithAFriendUnderTheSunAndAnEnemyUnderTheMoon()
        {
            foreach (var onTheMoon in new[] { false, true })
            {
                yield return StageIgnatia(
                    onTheMoon
                        ? SceneCardContent.VerifiedEngagedStagedIgnatiaOnTheMoon
                        : SceneCardContent.VerifiedEngagedStagedIgnatia,
                    hand: new[]
                    {
                        IgnatiaCardContent.UnstablePortal, IgnatiaCardContent.Equilibrium
                    },
                    enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
                var root = Root();
                var runtime = root.Runtime;
                var hers = Ignatia(runtime);
                var enemy = Enemies(runtime)[0];
                var herStood = Position(runtime, hers);

                yield return BeginIgnatia(
                    root, IgnatiaCardContent.UnstablePortal, ActionKind.Skill,
                    WaspCardContent.Shock);
                var choice = runtime.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, Remaining(runtime));
                Assert.That(
                    choice.NodeId.Value,
                    Is.EqualTo(onTheMoon ? "ignatia-swap-moon-select" : "ignatia-swap-sun-select"),
                    Remaining(runtime));
                Assert.That(
                    choice.Candidates.Any(candidate =>
                        candidate.Value.Kind == EffectValueKind.Hero &&
                        candidate.Value.HeroId.Equals(hers)),
                    Is.False,
                    "p.17's Friendly never includes her, and neither does p.17's Enemy. " +
                    Remaining(runtime));

                if (onTheMoon)
                {
                    Assert.That(OfferedUnits(runtime), Has.Member(enemy.Value),
                        "☾ \"an enemy unit in radius\" - p.19's units, so the enemy HERO is one. " +
                        Remaining(runtime));
                    var theirStood = Position(runtime, enemy);
                    yield return Pick(root, CandidateFor(runtime, enemy));
                    Assert.That(Position(runtime, hers), Is.EqualTo(theirStood),
                        "p.19's Swap exchanges the two positions. " + Remaining(runtime));
                    Assert.That(Position(runtime, enemy), Is.EqualTo(herStood));
                    Assert.That(HeroView(hers).Coordinate, Is.EqualTo(theirStood),
                        "and both pieces follow the authority.");
                    Assert.That(HeroView(enemy).Coordinate, Is.EqualTo(herStood));
                }
                else
                {
                    Assert.That(OfferedUnits(runtime), Has.Member(BlueMelee.Value));
                    Assert.That(OfferedUnits(runtime), Has.Member(BlueRanged.Value),
                        "☀ \"a friendly unit in radius\" - both friendly minions inside the printed " +
                        "Radius 4. " + Remaining(runtime));
                    Assert.That(
                        OfferedUnits(runtime).All(value =>
                            runtime.ReadModel.Minions.Any(minion =>
                                minion.MinionId.Value == value &&
                                minion.TeamSide == runtime.Setup.Heroes
                                    .Single(hero => hero.HeroId.Equals(hers)).TeamSide)),
                        Is.True,
                        "and every candidate is a unit on her OWN side, which is the half of p.17's " +
                        "Friendly the moon line inverts. " + Remaining(runtime));
                    var minionStood = MinionView(BlueMelee).Occupancy.Coordinate;
                    yield return Pick(root, CandidateFor(runtime, BlueMelee));
                    Assert.That(Position(runtime, hers), Is.EqualTo(minionStood),
                        Remaining(runtime));
                    Assert.That(MinionView(BlueMelee).Occupancy.Coordinate, Is.EqualTo(herStood));
                }

                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    "Unstable Portal prints no second clause on either line. " + Remaining(runtime));
                SeatsAgree(runtime, onTheMoon ? "the enemy swap" : "the friendly swap");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.42e's wall, and the rewind it leaves open
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.42e: a seat's own rewind stops at ANY interaction with an opponent, and at
        /// nothing else.
        ///
        /// <para><b>Both halves, on two faces of two cards of hers.</b> Locking an Attack's target on an
        /// ENEMY hero is the canonical interaction, and her attacks reach it through card text rather
        /// than through p.13 - which is the thing worth proving, because the wall is raised by what the
        /// answer DID rather than by which control asked. Moving a FRIENDLY hero touches nothing an
        /// opponent owns, so the same seat's rewind is still open there and one press gives the
        /// question back with the offer that was on it.</para>
        ///
        /// <para>The square itself is a SEAT projection's fact and a Director sandbox publishes no seat
        /// of its own (<c>MatchSessionProjection.IsSeatUndoAvailable</c> is always false there), so it
        /// is read on the host's own seat projection - the value the square is drawn from.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerAttackTargetWallsHerOwnRewindAndAFriendlyPushLeavesItOpen()
        {
            // The wall: an Attack target locked on an enemy hero.
            yield return StageIgnatia(
                SceneCardContent.VerifiedEngagedStagedIgnatia,
                hand: new[] { IgnatiaCardContent.PlayingWithFire, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute });
            var wallRoot = Root();
            var wallRuntime = wallRoot.Runtime;
            var wallHer = Ignatia(wallRuntime);
            var wallEnemy = Enemies(wallRuntime)[0];
            var wallSeat = SeatOf(wallRuntime, wallHer);

            yield return BeginIgnatia(
                wallRoot, IgnatiaCardContent.PlayingWithFire, ActionKind.Attack,
                WaspCardContent.Shock);
            Assert.That(
                wallRuntime.Host.ProjectionForSeat(wallSeat).IsSeatUndoAvailable, Is.True,
                "Arrange: before she names a target her own seat may still rewind. " +
                Remaining(wallRuntime));
            yield return Pick(wallRoot, CandidateFor(wallRuntime, wallEnemy));
            Assert.That(
                wallRuntime.Host.ProjectionForSeat(wallSeat).IsSeatUndoAvailable, Is.False,
                "8.42e's interaction wall: the card text locked an Attack on a hero an opponent " +
                "owns, so this seat's own rewind may not reach back through it. " +
                Remaining(wallRuntime));
            yield return null;
            yield return ClickDefenseFace(wallRoot, WaspCardContent.Electrocute);
            yield return null;
            yield return null;
            // MILESTONE 8.86: the wall is what this journey is about, but the Attack it is raised by
            // still has to have happened - a rewind wall over an Attack that applied nothing would be
            // a wall over nothing.
            Assert.That(
                wallRuntime.Cards.Single(card =>
                    card.OwnerId.Equals(wallEnemy) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.Electrocute))
                    .State.Location,
                Is.EqualTo(CardLocation.DiscardPile),
                "The locked Attack really reached p.14 and the defender spent the face they " +
                "answered with. " + Remaining(wallRuntime));
            Assert.That(wallRuntime.PendingDefenseChoice, Is.Null, Remaining(wallRuntime));
            Healthy(wallRuntime);

            // The plain rewind: a friendly hero moved, and one press gives the question back.
            yield return StageIgnatia(
                SceneCardContent.VerifiedStagedIgnatiaWithAlly,
                hand: new[] { IgnatiaCardContent.SearingHeat, IgnatiaCardContent.Equilibrium },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.Electrocute },
                allyHand: new[] { ArienCardContent.NobleBlade, ArienCardContent.SpellBreak });
            var openRoot = Root();
            var openRuntime = openRoot.Runtime;
            var openHer = Ignatia(openRuntime);
            var openAlly = Ally(openRuntime);
            var openSeat = SeatOf(openRuntime, openHer);

            yield return BeginIgnatia(
                openRoot, IgnatiaCardContent.SearingHeat, ActionKind.Skill,
                WaspCardContent.Shock, ArienCardContent.NobleBlade);
            var node = openRuntime.PendingEffectChoice.NodeId;
            var offered = OfferedUnits(openRuntime);
            yield return Pick(openRoot, CandidateFor(openRuntime, openAlly));
            Assert.That(openRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-sun-line"), Remaining(openRuntime));
            Assert.That(
                openRuntime.Host.ProjectionForSeat(openSeat).IsSeatUndoAvailable, Is.True,
                "Naming a hero on her OWN side touches nothing an opponent owns, so the wall is " +
                "not raised. " + Remaining(openRuntime));

            OpenDirectorDrawer(openRoot.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(openRoot.Hud, "UNDO"));
            yield return null;
            yield return null;
            UiPlayer.Click(UiPlayer.Button(openRoot.Hud, "DIRECTOR ▾"));
            yield return null;

            var restored = openRuntime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "One press gives the question back unanswered - milestone 8.46b made a pick of " +
                "exactly one ONE accepted command. " + Remaining(openRuntime));
            Assert.That(restored.NodeId, Is.EqualTo(node), Remaining(openRuntime));
            Assert.That(OfferedUnits(openRuntime), Is.EqualTo(offered).AsCollection,
                "and the offer that comes back is the offer that was there. " +
                Remaining(openRuntime));
            yield return Pick(openRoot, CandidateFor(openRuntime, openAlly));
            Assert.That(openRuntime.PendingEffectChoice.NodeId.Value,
                Is.EqualTo("ignatia-push-sun-line"),
                "Replaying the retained prefix reaches the same next question. " +
                Remaining(openRuntime));
            SeatsAgree(openRuntime, "the replayed push");
            Healthy(openRuntime);
        }
    }
}
