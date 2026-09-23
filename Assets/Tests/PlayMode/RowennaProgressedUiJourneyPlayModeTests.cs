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
using static GuardsDigital.Tests.PlayMode.RowennaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Rowenna's Green ladder, her Tier III faces, rulebook p.16's Ultimate and the session operations
    /// every hero owes the permanent gate. Milestone 8.57h.
    ///
    /// <para><b>One journey here composes a real two-versus-two match rather than a staged engagement</b>,
    /// and the reason is printed: Opening Volley's "May repeat once" is offered only while another
    /// eligible hero exists, and eligibility here means an enemy hero in radius whose discard pile is
    /// still empty - which the first instance has just made false of the hero it named. A one-enemy
    /// board can therefore never show the repeat, so the journey deals two enemy heroes through the
    /// line-up seam milestone 8.35z37 built, exactly as Garrus's own two-enemy journey does.</para>
    /// </summary>
    public sealed class RowennaProgressedUiJourneyPlayModeTests
    {
        /// <summary>
        /// A real two-versus-two match with Rowenna and Arien against Wasp and Xargatha, all four
        /// stood by the sandbox. Her own hand is rulebook p.6's five PLUS the named face drawn from
        /// her deck, which is how the staging leaves her discard pile EMPTY: a hand that names fewer
        /// cards than she was dealt discards the difference, and Opening Volley's both-parties clause
        /// reads exactly that pile.
        /// </summary>
        private static IEnumerator StageTwoVersusTwo(CardDefinitionId face)
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            RegisteredHero Find(string id) =>
                roster.Single(hero => hero.ContentId.Value == id);
            var her = Find(RowennaCardContent.ContentSetId);
            var ally = Find(ArienCardContent.ContentSetId);
            var first = Find(WaspCardContent.ContentSetId);
            var second = Find(XargathaCardContent.ContentSetId);

            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { first, second }, new[] { her, ally });
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(
                        new HeroId(her.HeroIdValue), HerSpace, hand: DealtHandPlus(face)),
                    new SandboxHeroStaging(new HeroId(ally.HeroIdValue), AllySpace),
                    new SandboxHeroStaging(new HeroId(first.HeroIdValue), EnemySpace),
                    new SandboxHeroStaging(
                        new HeroId(second.HeroIdValue), new HexCoordinate(-4, 0))
                });
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
        }

        /// <summary>The p.15 upgrade candidate's own control, addressed the way the HUD names it.</summary>
        private static Button UpgradeControl(
            MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip ==
                    (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        // -------------------------------------------------------------------------------------------
        // The Green ladder
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Close Quarters and Melee: "After movement, if you are adjacent to an enemy hero, you may
        /// Choose one - (1) Place a friendly minion in radius into a space adjacent to that enemy hero.
        /// (2) Place an enemy minion in radius into a space adjacent to you."
        ///
        /// <para><b>The text arrives AFTER the walk</b>, which is milestone 8.35z31's timing and
        /// Ursafar's seam: a zero-space walk still applies it, and this journey takes exactly that -
        /// she is already adjacent to the enemy hero, so the movement is ended where it began and the
        /// modal opens anyway.</para>
        ///
        /// <para>Both printed bullets are driven and so is the printed decline, and every offer is
        /// asserted as an exact set: bullet 1 offers the friendly minions inside her own printed Radius
        /// and lands one on a FREE neighbour of the enemy hero she named, bullet 2 offers the enemy
        /// minions inside that Radius and lands one beside HER. Melee is the same sentence at Radius 3
        /// and its offer is measurably wider, which is what makes the two faces one journey rather than
        /// one row copied twice.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheGreenModalsPlaceEitherMinionAfterTheMovementOrDecline()
        {
            foreach (var bullet in new[] { "friendly", "enemy", "decline" })
            {
                var face = bullet == "enemy"
                    ? RowennaCardContent.Melee
                    : RowennaCardContent.CloseQuarters;
                var content = bullet == "enemy"
                    ? SceneCardContent.VerifiedTierTwoEngagedStagedRowenna
                    : SceneCardContent.VerifiedEngagedStagedRowenna;
                yield return StageRowenna(content);
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                var enemy = Enemies(runtime)[0];
                var herSpace = Position(runtime, her);

                yield return BeginRowenna(root, face, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null,
                    face.Value + " prints a Movement primary. " + Remaining(runtime));

                // Milestone 8.43f's independent control, and it is the ONE that performs a
                // zero-space walk: END TURN with nothing done cancels the open action instead, which
                // is what the first Unity run of this journey measured - the card text never ran and
                // the strip went back to "choose an action".
                Assert.That(runtime.RoundView.Model.FinishMovementInvocation, Is.Not.Null,
                    "An open Movement session draws its own finish. " + Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(root.Hud, "FINISH MOVEMENT"));
                yield return null;
                yield return null;
                Assert.That(Position(runtime, her), Is.EqualTo(herSpace),
                    "A zero-space walk is still a performed Movement action - 8.35z31's seam.");

                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null,
                    "\"After movement, if you are adjacent to an enemy hero\" is true, so the " +
                    "printed `may` opens. " + Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("melee-first-may"));
                Assert.That(may.DeclineInvocation, Is.Not.Null);
                if (bullet == "decline")
                {
                    UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
                    yield return null;
                    yield return null;
                    Assert.That(runtime.PendingEffectChoice, Is.Null,
                        "A declined `may` runs neither bullet. " + Remaining(runtime));
                    Healthy(runtime);
                    continue;
                }

                yield return May(root);
                var modal = runtime.PendingEffectChoice;
                Assert.That(modal, Is.Not.Null, Remaining(runtime));
                Assert.That(modal.NodeId.Value, Is.EqualTo("melee-first-modal"));
                Assert.That(
                    modal.Candidates
                        .Select(candidate => candidate.OptionNodeId.Value)
                        .OrderBy(value => value, StringComparer.Ordinal)
                        .ToArray(),
                    Is.EqualTo(new[] { "melee-first-enemy", "melee-first-friendly" }).AsCollection,
                    "p.10's modal form draws both printed bullets. " + Remaining(runtime));

                var branch = bullet == "friendly" ? "melee-first-friendly" : "melee-first-enemy";
                UiPlayer.ClickEffectOption(runtime, root.Hud, branch);
                yield return null;
                yield return null;

                var radius = runtime.Definitions
                    .Single(definition => definition.Id.Equals(face))
                    .ActionDefinitions.Single(action =>
                        action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                    .BaseValues.Single(value => value.Channel == PrintedValueChannel.Radius)
                    .Amount;
                var minion = runtime.PendingEffectChoice;
                Assert.That(minion.NodeId.Value, Does.EndWith("-minion"), Remaining(runtime));
                var offered = minion.Candidates
                    .Select(candidate => candidate.Value.MinionId)
                    .ToArray();
                Assert.That(offered, Is.Not.Empty);
                foreach (var candidate in offered)
                {
                    var standing = runtime.ReadModel.Minions
                        .Single(value => value.MinionId.Equals(candidate));
                    Assert.That(Distance(standing.Coordinate, herSpace),
                        Is.LessThanOrEqualTo(radius),
                        "Every offered minion is inside " + face.Value + "'s printed Radius " +
                        radius + ". " + Remaining(runtime));
                    Assert.That(
                        runtime.ReadModel.Minions.Single(value =>
                            value.MinionId.Equals(candidate)).TeamSide ==
                            runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(her)).TeamSide,
                        Is.EqualTo(bullet == "friendly"),
                        "and the printed bullet decides whose minions are offered.");
                }

                var moved = offered.First();
                yield return Pick(
                    root,
                    minion.Candidates.Single(candidate =>
                        candidate.Value.MinionId.Equals(moved)));

                if (bullet == "friendly")
                {
                    var named = runtime.PendingEffectChoice;
                    Assert.That(named.NodeId.Value, Is.EqualTo("melee-first-hero"),
                        "\"that enemy hero\" is named as a landmark before the space is picked, " +
                        "which is why 8.35l's first-step walk greys this bullet correctly. " +
                        Remaining(runtime));
                    Assert.That(
                        named.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                        Is.EqualTo(new[] { enemy }).AsCollection);
                    yield return Pick(root, named.Candidates.Single());
                }

                var place = runtime.PendingEffectChoice;
                Assert.That(place.NodeId.Value, Does.EndWith("-place"), Remaining(runtime));
                var anchor = bullet == "friendly" ? Position(runtime, enemy) : herSpace;
                Assert.That(
                    OfferedSpaces(runtime).All(space => Distance(space, anchor) == 1),
                    Is.True,
                    "Every destination is adjacent to " +
                    (bullet == "friendly" ? "the enemy hero she named" : "her") + ". " +
                    Remaining(runtime));
                var destination = OfferedSpaces(runtime).First();
                yield return Space(root, destination);
                Assert.That(
                    runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(moved))
                        .Coordinate,
                    Is.EqualTo(destination),
                    "and the miniature that was named is the miniature that moved.");
                Assert.That(runtime.PendingEffectChoice, Is.Null,
                    face.Value + " prints one performance. " + Remaining(runtime));
                SeatsAgree(runtime, face.Value + " resolved");
                Healthy(runtime);
            }
        }

        /// <summary>
        /// Grand Melee: "After movement, if you are adjacent to an enemy hero, Choose up to two times -
        /// (1) ... (2) ..."
        ///
        /// <para><b>Two performances of the same printed modal in one turn</b>, composed at milestone
        /// 8.57d as two sibling subtrees rather than a loop - there is no loop node and a
        /// <c>RepeatAction</c> would repeat the Movement - and each performance is carried out before
        /// the next is offered, which this journey reads by taking a DIFFERENT bullet each time and
        /// watching the first placement change what the second one offers.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator GrandMeleeTakesBothPerformancesInOneTurn()
        {
            yield return StageRowenna(SceneCardContent.VerifiedTierThreeEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];
            var herSpace = Position(runtime, her);

            yield return BeginRowenna(root, RowennaCardContent.GrandMelee, ActionKind.Movement);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "FINISH MOVEMENT"));
            yield return null;
            yield return null;
            Assert.That(Position(runtime, her), Is.EqualTo(herSpace));

            var placed = new List<HexCoordinate>();
            foreach (var index in new[] { "first", "second" })
            {
                var may = runtime.PendingEffectChoice;
                Assert.That(may, Is.Not.Null,
                    "\"Choose up to two times\" offers performance " + index + ". " +
                    Remaining(runtime));
                Assert.That(may.NodeId.Value, Is.EqualTo("melee-" + index + "-may"));
                yield return May(root);

                var branch = "melee-" + index + (index == "first" ? "-friendly" : "-enemy");
                UiPlayer.ClickEffectOption(runtime, root.Hud, branch);
                yield return null;
                yield return null;

                var minion = runtime.PendingEffectChoice;
                Assert.That(minion.NodeId.Value, Is.EqualTo(branch + "-minion"), Remaining(runtime));
                var moved = Named(minion.Candidates, "minion").Value.MinionId;
                yield return Pick(root, Named(minion.Candidates, "minion"));

                if (index == "first")
                {
                    yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());
                }

                var anchor = index == "first" ? Position(runtime, enemy) : herSpace;
                var destination = OfferedSpaces(runtime)
                    .First(space => !placed.Contains(space) && Distance(space, anchor) == 1);
                yield return Space(root, destination);
                placed.Add(destination);
                Assert.That(
                    runtime.ReadModel.Minions.Single(value => value.MinionId.Equals(moved))
                        .Coordinate,
                    Is.EqualTo(destination));
            }

            Assert.That(placed, Has.Count.EqualTo(2));
            Assert.That(placed[0], Is.Not.EqualTo(placed[1]),
                "Each performance is carried out before the next is offered, so the second one " +
                "cannot be offered the space the first just filled.");
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "and \"up to two times\" is two, not three. " + Remaining(runtime));
            SeatsAgree(runtime, "both performances");
            Healthy(runtime);
        }

        /// <summary>
        /// Opening Volley: "If both you and an enemy hero in radius have no cards in the discard, that
        /// hero discards a card, if able. May repeat once."
        ///
        /// <para>The repeat needs a SECOND eligible hero and nothing else will do, because the first
        /// instance has just put a card in the first hero's pile. Both instances are the enemy's own
        /// decision on their own seat, and the second instance's offer is asserted as the exact set
        /// that is left.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator OpeningVolleyRepeatsAgainstASecondEligibleHero()
        {
            yield return StageTwoVersusTwo(RowennaCardContent.OpeningVolley);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemies = Enemies(runtime);
            Assert.That(enemies, Has.Length.EqualTo(2),
                "Arrange: two enemy heroes, which is what the printed repeat needs.");
            Assert.That(DiscardsOf(runtime, her), Is.Empty,
                "Arrange: her own pile is empty, so her half of the both-parties clause holds.");

            yield return BeginRowenna(root, RowennaCardContent.OpeningVolley, ActionKind.Skill);
            var first = runtime.PendingEffectChoice;
            Assert.That(first.NodeId.Value, Is.EqualTo("volley-target"), Remaining(runtime));
            Assert.That(
                first.Candidates.Select(candidate => candidate.Value.HeroId)
                    .OrderBy(value => value.Value, StringComparer.Ordinal).ToArray(),
                Is.EqualTo(enemies.OrderBy(value => value.Value, StringComparer.Ordinal).ToArray())
                    .AsCollection,
                "Both enemy heroes stand in radius with empty piles. " + Remaining(runtime));
            var named = Named(first.Candidates, "first").Value.HeroId;
            yield return Pick(root, Named(first.Candidates, "first"));

            var discard = runtime.PendingEffectChoice;
            Assert.That(discard.NodeId.Value, Is.EqualTo("volley-discard"));
            Assert.That(discard.ChooserId, Is.EqualTo(named),
                "The named hero spends one of their OWN cards. " + Remaining(runtime));
            SeatsAgree(runtime, "the first opponent's discard");
            yield return Pick(root, Named(discard.Candidates, "discard"));
            Assert.That(DiscardsOf(runtime, named), Has.Length.EqualTo(1));

            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null,
                "\"May repeat once\" is offered while another hero is still eligible. " +
                Remaining(runtime));
            Assert.That(repeat.NodeId.Value, Is.EqualTo("volley-repeat"));
            Assert.That(repeat.ChooserId, Is.EqualTo(her));
            yield return May(root);

            var second = runtime.PendingEffectChoice;
            Assert.That(second.NodeId.Value, Is.EqualTo("volley-target"), Remaining(runtime));
            Assert.That(
                second.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(enemies.Where(hero => !hero.Equals(named)).ToArray()).AsCollection,
                "and the hero whose pile the first instance filled is no longer eligible - the " +
                "both-parties clause removes a candidate by their OWN pile. " + Remaining(runtime));
            var other = second.Candidates.Single().Value.HeroId;
            yield return Pick(root, second.Candidates.Single());
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(other));
            yield return Pick(root, Named(runtime.PendingEffectChoice.Candidates, "runtime.PendingEffectChoice"));
            Assert.That(DiscardsOf(runtime, other), Has.Length.EqualTo(1));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "The printed repeat is once, so there is no third instance. " + Remaining(runtime));
            SeatsAgree(runtime, "the repeat resolved");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Fabled Lance: "All your attack actions gain the &quot;Ranged&quot; subtype, target a unit in
        /// range, and count as having a printed Range value of 2."
        ///
        /// <para><b>Three player-visible consequences, and the same board without the Ultimate is what
        /// makes each of them a measurement rather than an assertion.</b> With Fabled Lance faceup
        /// beside her dashboard the enemy hero standing two spaces away is a legal target and the
        /// Attack's own control reads <c>RANGE 2</c>; on the identical engagement with the card still
        /// in her deck the same hero is offered to nothing at all and the control names no range. And
        /// the defender's answer changes with it: Wasp's Reflect Projectiles is a p.14 BLOCK whose
        /// printed condition is a Ranged attack, and it is offered here because her Attack now is
        /// one.</para>
        ///
        /// <para>The Ultimate's own p.16 visibility is read where a player reads it - the unlocked
        /// card on her side rail, drawn as <c>gd-ultimate-card</c>.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator FabledLanceMakesHerAttacksRangedAtTwoAndARangedBlockAnswersThem()
        {
            var twoAway = new HexCoordinate(-1, 1);

            // Without it: the same hero, the same distance, offered to nothing.
            yield return StageRowenna(
                SceneCardContent.VerifiedEngagedStagedRowenna, enemySpace: twoAway);
            var plainRoot = Root();
            var plainRuntime = plainRoot.Runtime;
            var plainHer = Rowenna(plainRuntime);
            var plainEnemy = Enemies(plainRuntime)[0];
            Assert.That(
                Distance(Position(plainRuntime, plainHer), Position(plainRuntime, plainEnemy)),
                Is.EqualTo(2),
                "Arrange: the sandbox stands the enemy hero exactly two spaces away.");
            Assert.That(
                plainRuntime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(plainHer)).UltimateCard,
                Is.Null,
                "and her Ultimate is still in the deck, so the rail draws none.");
            yield return BeginRowenna(
                plainRoot,
                RowennaCardContent.CodeOfChivalry,
                ActionKind.Attack,
                WaspCardContent.MagneticDagger);
            Assert.That(plainRuntime.PendingAttackTargetChoice.LegalTargetHeroIds, Is.Empty,
                "Her printed Attack reaches adjacency only. " + Remaining(plainRuntime));
            Healthy(plainRuntime);

            // With it.
            yield return StageRowenna(
                SceneCardContent.VerifiedTierThreeEngagedStagedRowenna, enemySpace: twoAway);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];

            var rail = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(her));
            Assert.That(rail.UltimateCard, Is.Not.Null,
                "p.16 turns the unlocked Ultimate faceup where a player can see it.");
            Assert.That(rail.UltimateCard.DisplayName, Is.EqualTo("Fabled Lance"));
            Assert.That(
                UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                    element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)),
                Is.True,
                "and it is drawn on her rail rather than only held in a model. Shown: " +
                UiPlayer.Describe(root.Hud));

            yield return BeginRowenna(
                root,
                RowennaCardContent.CodeOfChivalry,
                ActionKind.Attack,
                WaspCardContent.MagneticDagger);
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetHeroIds.ToArray(),
                Is.EqualTo(new[] { enemy }).AsCollection,
                "\"target a unit in range\" now reaches two spaces. " + Remaining(runtime));
            Assert.That(
                runtime.PendingAttackTargetChoice.LegalTargetMinionIds
                    .Select(minion => runtime.ReadModel.Minions
                        .Single(value => value.MinionId.Equals(minion)).Coordinate)
                    .All(space => Distance(space, Position(runtime, her)) <= 2),
                Is.True,
                "and nothing beyond the printed Range 2 is offered. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud,
                    HeroView(enemy).transform.position),
                Is.True,
                "and a player answers it with a board click on the piece two spaces away.");
            yield return null;
            yield return null;

            var defense = runtime.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, Remaining(runtime));
            Assert.That(
                defense.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(
                        WaspCardContent.ReflectProjectiles)),
                Is.True,
                "Reflect Projectiles is a Block whose printed condition is a RANGED attack, and " +
                "hers now is one. Offers: " +
                string.Join(", ", defense.LegalDefenseOffers.Select(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Value)));
            yield return ClickDefenseFace(root, WaspCardContent.ReflectProjectiles);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True,
                "\"a Block succeeds regardless of the Attack Value\" when its condition is met, so " +
                "the defender is still standing. " + Remaining(runtime));
            SeatsAgree(runtime, "the ranged attack answered by a Block");
            Healthy(runtime);
        }

        /// <summary>
        /// The Attack control a player actually presses names the Ultimate's own printed Range, which
        /// is where <c>RangedAttackOverlayQuery</c> is read on the round strip.
        ///
        /// <para>It is asserted on its own rather than inside the journey above because the label is
        /// read at a different moment - before the action is started, while the strip still draws
        /// every offer - and because the negative belongs beside it: the same face on the same board
        /// without the Ultimate names no range at all.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HerAttackControlNamesTheUltimatesPrintedRangeOnlyWhileItIsUnlocked()
        {
            foreach (var unlocked in new[] { false, true })
            {
                yield return StageRowenna(
                    unlocked
                        ? SceneCardContent.VerifiedTierThreeEngagedStagedRowenna
                        : SceneCardContent.VerifiedEngagedStagedRowenna);
                var root = Root();
                var runtime = root.Runtime;
                var her = Rowenna(runtime);
                yield return PointTrayAt(root, her);
                yield return CommitThroughTray(
                    root, NameOf(runtime, RowennaCardContent.CodeOfChivalry));
                foreach (var seat in Enemies(runtime))
                {
                    yield return PointTrayAt(root, seat);
                    yield return CommitThroughTray(root, LowestInitiativeCardOf(runtime, seat));
                }

                yield return WaitForActor(root, her);
                yield return PointTrayAt(root, her);
                var attack = runtime.RoundView.Model.Actions.Single(action =>
                    KindOf(runtime, action.Command.ActionDefinitionId) == ActionKind.Attack);
                Assert.That(attack.Label.Contains("RANGE 2"), Is.EqualTo(unlocked),
                    "The control reads \"RANGE 2\" exactly while Fabled Lance is unlocked. It " +
                    "read: " + attack.Label.Replace("\n", " / "));
                UiPlayer.Click(UiPlayer.Button(root.Hud, attack.Label));
                yield return null;
                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    "and the control a player presses is the one that opens p.13's targeting. " +
                    Remaining(runtime));
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The session half every hero owes
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Rulebook p.15's Level Up, taken on the real window: her upgrade candidates are her own deck
        /// faces, each paired with the item icon its sibling carries, and the item she is left with
        /// raises a real Attack Total afterwards.
        ///
        /// <para>p.16's item is the one piece of hero content no card can hand a player directly - it
        /// is gained from the sibling the Level Up did NOT take - so this is the only route to
        /// <c>ItemVisibilityAndUse</c>, and it is driven through the window's own control rather than
        /// through a projected command.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator LevelUpTakesTheWholeUpgradeFaceAndItsSiblingItem()
        {
            yield return StageRowenna(SceneCardContent.VerifiedEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);

            for (var budget = 0; budget < 200 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                    runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(her));
                budget++)
            {
                yield return NextRoundDecision(root);
            }

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
                options.All(option => RowennaCardContent.Deck.Any(id =>
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

            for (var budget = 0; budget < 60 &&
                runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
            {
                yield return NextRoundDecision(root);
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

        /// <summary>
        /// Rulebook p.13's Defeat takes her piece off the board, p.13's respawn puts it back on a space
        /// a player clicks, and the Director's RESET rebuilds the whole session from its initial state
        /// with exactly one piece for her and a tray that matches her hand.
        /// </summary>
        [UnityTest]
        public IEnumerator HerDefeatedPieceRespawnsAndRestartRebuildsTheSession()
        {
            yield return StageRowenna(SceneCardContent.VerifiedEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];

            Commit(runtime, her, RowennaCardContent.StandGuard);
            Commit(runtime, enemy, WaspCardContent.MagneticDagger);
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
            SeatsAgree(runtime, "her own defeat");

            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime, her, RowennaCardContent.CodeOfChivalry);
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
            Assert.That(runtime.AcceptedCommands, Is.Empty,
                "RESET returns the initial state with no accepted history. " + Remaining(runtime));
            Assert.That(
                Object.FindObjectsByType<PrimitiveHeroView>()
                    .Count(view => view.Occupancy.HeroId.Equals(her)),
                Is.EqualTo(1),
                "One hero, one piece - a rebuild that leaves a stale view behind fails here.");
            Assert.That(
                runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(her))
                    .TrayCards.Count,
                Is.EqualTo(runtime.Cards.Count(card =>
                    card.OwnerId.Equals(her) && card.State.Location == CardLocation.Hand)),
                "and her tray is reconstructed from the projection rather than from a stale view.");
            Healthy(runtime);
        }

        /// <summary>
        /// The two pauses of hers that belong to somebody else survive the Director's retained-prefix
        /// Undo, a replay of that prefix, and a snapshot restore - and are answered afterwards on the
        /// same surface.
        ///
        /// <para><b>They are the hard case on purpose.</b> Throw the Gauntlet's move and Opening Shots'
        /// discard are both standing on the OPPONENT's seat when the capture is taken, so what has to
        /// come back is not only the question but whose question it is; a restore that re-derived the
        /// chooser from the acting seat would look identical from her side of the table and would hand
        /// the enemy's decision to her.</para>
        ///
        /// <para>The Undo is taken on the LIVE session before any capture, because
        /// <c>MatchSession.RestoreSnapshot</c> clears the accepted history - there is nothing to undo
        /// after a restore, and an assertion about history would not survive one.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator TheOpponentsOwnPausesSurviveUndoReplayAndSnapshotRestore()
        {
            yield return StageRowenna(SceneCardContent.VerifiedEngagedStagedRowenna);
            var root = Root();
            var runtime = root.Runtime;
            var her = Rowenna(runtime);
            var enemy = Enemies(runtime)[0];

            yield return BeginRowenna(root, RowennaCardContent.ThrowTheGauntlet, ActionKind.Skill);
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single());
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("gauntlet-place"));
            var destinations = OfferedSpaces(runtime);
            yield return Space(root, destinations.First());

            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("gauntlet-move"));
            Assert.That(runtime.PendingEffectChoice.ChooserId, Is.EqualTo(enemy));

            // The retained-prefix Undo, on the live session.
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("gauntlet-place"),
                "One press gives the space question back unanswered - 8.46b made a pick of exactly " +
                "one ONE accepted command. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Is.Empty);
            Assert.That(
                OfferedSpaces(runtime), Is.EqualTo(destinations).AsCollection,
                "and the offer that comes back is the offer that was there.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            yield return Space(root, destinations.First());
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("gauntlet-move"),
                "Replaying the retained prefix reproduces the same pause. " + Remaining(runtime));
            var captured = runtime.CaptureSnapshot();
            Assert.That(captured, Is.Not.Null.And.Not.Empty);

            UiPlayer.ClickEffectControl(runtime, root.Hud, EffectChoiceCommandKind.Decline);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(CoinsOf(runtime, her), Is.Zero);

            Assert.That(runtime.RestoreSnapshot(captured), Is.True);
            yield return null;
            yield return null;
            var restored = runtime.PendingEffectChoice;
            Assert.That(restored, Is.Not.Null,
                "A pause survives a snapshot restore. " + Remaining(runtime));
            Assert.That(restored.NodeId.Value, Is.EqualTo("gauntlet-move"));
            Assert.That(restored.ChooserId, Is.EqualTo(enemy),
                "and it is still the OPPONENT's decision after the restore, which is the half a " +
                "journey driven from her seat alone could not tell apart. " + Remaining(runtime));
            SeatsAgree(runtime, "the restored opponent move");
            // MILESTONE 8.86: the restored move is ANSWERED with a space the enemy really travels
            // to, and both halves of "that hero may move 1 space; if they do, gain 2 coins" are
            // read - the displacement first, then the coin it is the condition of.
            var enemyStood = Position(runtime, enemy);
            var restoredTo = FirstClickableSpace(
                Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), OfferedSpaces(runtime));
            yield return Space(root, restoredTo);
            Assert.That(Position(runtime, enemy), Is.EqualTo(restoredTo),
                "The hero whose decision this was really moved to the space the answer named. " +
                Remaining(runtime));
            Assert.That(Distance(Position(runtime, enemy), enemyStood), Is.EqualTo(1),
                "one space, which is what the sentence prints. " + Remaining(runtime));
            Assert.That(CoinsOf(runtime, her), Is.EqualTo(2),
                "and answering it on the same surface pays the printed 2 coins.");
            Healthy(runtime);

            // The other cross-seat pause, restored on the seat that owns it.
            yield return StageRowenna(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedRowenna);
            var shotsRoot = Root();
            var shotsRuntime = shotsRoot.Runtime;
            var shotsEnemy = Enemies(shotsRuntime)[0];
            yield return BeginRowenna(
                shotsRoot, RowennaCardContent.OpeningShots, ActionKind.Skill);
            yield return Pick(shotsRoot, shotsRuntime.PendingEffectChoice.Candidates.Single());
            Assert.That(shotsRuntime.PendingEffectChoice.NodeId.Value, Is.EqualTo("volley-discard"));
            var before = shotsRuntime.PendingEffectChoice.Candidates.Count;
            var pause = shotsRuntime.CaptureSnapshot();

            yield return Pick(shotsRoot, Named(shotsRuntime.PendingEffectChoice.Candidates, "shotsRuntime.PendingEffectChoice"));
            Assert.That(DiscardsOf(shotsRuntime, shotsEnemy), Has.Length.EqualTo(1));

            Assert.That(shotsRuntime.RestoreSnapshot(pause), Is.True);
            yield return null;
            yield return null;
            Assert.That(shotsRuntime.PendingEffectChoice, Is.Not.Null, Remaining(shotsRuntime));
            Assert.That(shotsRuntime.PendingEffectChoice.NodeId.Value, Is.EqualTo("volley-discard"));
            Assert.That(shotsRuntime.PendingEffectChoice.ChooserId, Is.EqualTo(shotsEnemy));
            Assert.That(shotsRuntime.PendingEffectChoice.Candidates.Count, Is.EqualTo(before),
                "The same hand is offered again, which means the restore brought the opponent's own " +
                "position back rather than re-deriving it. " + Remaining(shotsRuntime));
            Assert.That(DiscardsOf(shotsRuntime, shotsEnemy), Is.Empty);
            SeatsAgree(shotsRuntime, "the restored opponent discard");
            yield return Pick(shotsRoot, Named(shotsRuntime.PendingEffectChoice.Candidates, "shotsRuntime.PendingEffectChoice"));
            Assert.That(DiscardsOf(shotsRuntime, shotsEnemy), Has.Length.EqualTo(1));
            Healthy(shotsRuntime);
        }
    }
}
