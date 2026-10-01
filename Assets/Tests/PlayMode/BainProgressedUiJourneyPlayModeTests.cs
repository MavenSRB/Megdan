using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.WhisperUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Bain's Tier II and Tier III faces and his Ultimate, driven through the controls a player
    /// actually reaches. Milestone 8.40k, and the content half of the FIFTEENTH hero's player-path
    /// gate - his starting hand is in <see cref="BainUiJourneyPlayModeTests"/> and the component half
    /// in <see cref="BainEngagedUiJourneyPlayModeTests"/>.
    ///
    /// <para><b>Ten of these eleven journeys need a board his printed set has to BUILD first</b>, and
    /// that is the single fact that shapes this fixture. Nine of his nineteen faces read the bounty -
    /// the two marker modals, all five Greens and both Movement primaries - and on an untouched
    /// staging no marker is out, so every one of those faces would be proved on the branch its printed
    /// condition is false on. Each such journey therefore plays Dead or Alive's whole page first,
    /// through the projected-command surface, and only then drives its own subject through real
    /// controls. The give itself is not delegated: it has its own journey in the base fixture, driven
    /// click by click.</para>
    ///
    /// <para><b>And two of them stage a HAND rather than a board.</b> His two shared retrievals need
    /// discard piles on both sides and a dealt match starts with none, so the Director's sandbox
    /// stages each hero holding a subset of what they were dealt - which puts the rest in their
    /// discard through <c>CardLifecycleAuthority</c>'s own transition, exactly as a round that had
    /// been played would have.</para>
    /// </summary>
    public sealed class BainProgressedUiJourneyPlayModeTests
    {
        /// <summary>An extra Red melee beside him, so a modal's adjacent bullet has a subject.</summary>
        private static readonly MinionId BesideHim = new MinionId("bain-gate-red-beside");

        private static readonly HexCoordinate BesideAt = new HexCoordinate(-4, 1);

        private static readonly HexCoordinate ArmingVictim = new HexCoordinate(-4, 2);

        /// <summary>
        /// Loads a staged Bain scene and stands an extra Red melee beside him, so that a journey whose
        /// setup defeats one adjacent minion still leaves one for its own subject to reach.
        /// </summary>
        private static IEnumerator StagedWithAMinionBesideHim(SceneCardContent content)
        {
            yield return LoadStagedScene(content);
            var surveyed = Root().Runtime;
            var bainId = BainSeat(surveyed).HeroId;
            var enemyId = OtherHeroId(surveyed, bainId);
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: new[]
                {
                    new SandboxHeroStaging(bainId),
                    new SandboxHeroStaging(enemyId)
                },
                spawnedMinions: new[]
                {
                    new SandboxMinionStaging(
                        BesideHim, TeamSide.Red,
                        StandardMinionType.Melee, BesideAt)
                },
                // Milestone 8.49: p.13 now charges a Tier III bounty-wearer four Life counters (the
                // Tier's three plus the marker's one), more than the staged one-versus-one deals a
                // side - so the enemy team is dealt p.6's six-player five, and a Defeat in this
                // journey's first order no longer ends the match before its second order can open.
                teamLifeCounts: new[]
                {
                    new KeyValuePair<TeamSide, int>(TeamSide.Red, 5),
                    new KeyValuePair<TeamSide, int>(TeamSide.Blue, 5)
                });
            yield return LoadStagedScene(content);
        }

        /// <summary>
        /// The spaces an ordinary walk of <paramref name="allowance"/> steps could reach from
        /// <paramref name="origin"/> - a breadth-first search that stops at every obstacle, which is
        /// what p.18 does to a hero without the permission his two Movement faces grant.
        /// </summary>
        private static HashSet<HexCoordinate> ReachableWithoutCrossing(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate origin, int allowance)
        {
            var map = runtime.ReadModel.SelectedMap;
            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable)
                .Select(space => space.Coordinate));
            var blocked = new HashSet<HexCoordinate>(
                runtime.ReadModel.Minions.Select(minion => minion.Coordinate));
            foreach (var hero in runtime.Heroes)
            {
                blocked.Add(hero.Coordinate);
            }

            var open = new HashSet<HexCoordinate> { origin };
            var frontier = new List<HexCoordinate> { origin };
            for (var step = 0; step < allowance; step++)
            {
                var next = new List<HexCoordinate>();
                foreach (var space in frontier)
                {
                    for (var direction = 0; direction < 6; direction++)
                    {
                        var neighbour = space.GetNeighbor((HexDirection)direction);
                        if (!playable.Contains(neighbour) || blocked.Contains(neighbour) ||
                            !open.Add(neighbour))
                        {
                            continue;
                        }

                        next.Add(neighbour);
                    }
                }

                frontier = next;
            }

            return open;
        }

        /// <summary>The printed Movement value of a face, before any card text raises it.</summary>
        private static int PrintedMovementOf(
            ConfiguredMatchSceneRuntime runtime, CardDefinitionId definitionId) =>
            runtime.Definitions
                .Single(definition => definition.Id.Equals(definitionId))
                .ActionDefinitions
                .Single(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .BaseValues
                .Single(value => value.Channel == PrintedValueChannel.Movement)
                .Amount;

        // -------------------------------------------------------------------------------------------
        // The Red line's two marker modals
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Hand Crossbow: "Choose one — • Target a hero in range with a bounty marker. • Target a unit
        /// adjacent to you."
        ///
        /// <para><b>The first bullet is the only way any face of his reaches the enemy HERO on this
        /// board, and that is what makes the assertion worth making.</b> The engagement stands them two
        /// apart on a bent line, so "adjacent to you" cannot reach him and the plain crossbows' straight
        /// line cannot either. With the bounty out, the marker bullet offers exactly that hero and
        /// nothing else - the printed Range 3 disk narrowed to marker holders, which is milestone
        /// 8.40c's fourth <c>AttackSequenceSubjectNarrowing</c> member doing its whole job.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HandCrossbowTakesItsMarkerBullet_AndReachesTheHeroWearingTheBounty()
        {
            yield return LoadStagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return ArmTheBounty(root, bainId, enemyId);
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            yield return HisTurnOn(root, bainId, BainCardContent.HandCrossbow);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "His Tier II Red stops the page before the attack, on p.10's modal. " +
                Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("crossbow-modal"), Remaining(runtime));
            Assert.That(modal.MaximumSelections, Is.EqualTo(1),
                "\"Choose one\" is 1..1, which is the ONLY thing separating this face from " +
                "Hunter-Seeker. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null,
                "p.14 has not asked whom yet, because each bullet publishes its own targeting " +
                "sentence. " + Remaining(runtime));

            var distance = HexDistanceQuery.Calculate(
                runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate,
                runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate);
            Assert.That(distance, Is.GreaterThan(1),
                "The enemy hero is NOT adjacent, so the second bullet cannot reach him and the " +
                "first bullet is the only thing that can. " + Remaining(runtime));
            // MILESTONE 8.70. The bullet this journey drives is the one with a subject, so the
            // greying must not have touched it - and asserting that here is what keeps the journey
            // honest if a later staging moves the bounty or the geometry.
            Assert.That(
                modal.UnavailableCandidates.Select(candidate => candidate.OptionNodeId.Value),
                Does.Not.Contain("crossbow-marker-bullet"),
                "\"a hero in range with a bounty marker\" names the hero wearing it, so this bullet " +
                "is offered rather than greyed. " + Remaining(runtime));

            yield return PressEffectControl(root, BulletLabelFor(runtime, "crossbow-marker-bullet"));

            var targets = runtime.PendingAttackTargetChoice;
            Assert.That(targets, Is.Not.Null,
                "The taken bullet opens p.14's own target question. " + Remaining(runtime));
            Assert.That(
                targets.LegalTargets.Select(target => target.Kind == AttackTargetKind.Hero
                    ? "hero " + target.HeroId.Value
                    : "minion " + target.MinionId.Value).ToArray(),
                Is.EqualTo(new[] { "hero " + enemyId.Value }).AsCollection,
                "\"a hero in range with a bounty marker\" admits the one hero wearing it and no " +
                "unit at all. " + Remaining(runtime));

            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The named hero is taken by clicking the piece where they stand.");
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "p.14 then runs the attack the bullet bound, so the defender is asked. " +
                Remaining(runtime));
            Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "\"Choose one\" takes exactly one bullet, so nothing stands afterwards. " +
                Remaining(runtime));
        }

        /// <summary>
        /// Hunter-Seeker: "Choose one, or both, on different targets — • Target a hero in range with a
        /// bounty marker. • Target a unit adjacent to you."
        ///
        /// <para><b>BA-D4 reads this as TWO COMPLETE p.14 ORDERS</b>, each defending separately and
        /// each against the board as the sequence finds it, which is Dodger's D-06 ruling word for
        /// word. This journey takes both bullets in one action and drives both pages: the hero wearing
        /// the bounty answers the first, and a unit standing beside him answers the second. The sandbox
        /// stands an extra Red melee at (-4,1) so the arming attack and the second bullet do not have
        /// to share the one adjacent minion the engagement deals.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator HunterSeekerTakesBothBullets_AsTwoCompleteAttackOrders()
        {
            yield return StagedWithAMinionBesideHim(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return ArmTheBounty(root, bainId, enemyId, ArmingVictim);
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                Remaining(runtime));
            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(BesideHim)), Is.True,
                "The arming attack spent the minion at (-4,2), so the one the sandbox stands at " +
                "(-4,1) is what the second bullet reaches. " + Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            yield return HisTurnOn(root, bainId, BainCardContent.HunterSeeker);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null, Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("crossbow-modal"), Remaining(runtime));
            Assert.That(modal.MinimumSelections, Is.EqualTo(1), Remaining(runtime));
            Assert.That(modal.MaximumSelections, Is.EqualTo(2),
                "\"Choose one, OR BOTH\" is 1..2, which is p.10's Modal Actions read literally and " +
                "the only difference between this face and Hand Crossbow. " + Remaining(runtime));

            // BOTH bullets, selected one at a time and closed by the electing seat's own confirm -
            // selecting is not taking.
            UiPlayer.Click(UiPlayer.Button(hud, BulletLabelFor(runtime, "crossbow-marker-bullet")));
            yield return null;
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, BulletLabelFor(runtime, "crossbow-adjacent-bullet")));
            yield return null;
            yield return null;

            // The second select fills the modal's printed ceiling of two, so the offer closes on its
            // own rather than waiting for a confirm - which is the difference between "choose one, or
            // both" and every unbounded Choose in this build, and is worth noting rather than
            // asserting a control the page does not draw.
            yield return ConfirmIfStillStanding(root, modal.NodeId);
            yield return null;

            // ORDER ONE: the hero wearing the bounty.
            var first = runtime.PendingAttackTargetChoice;
            Assert.That(first, Is.Not.Null,
                "The first chosen bullet opens its own p.14. " + Remaining(runtime));
            Assert.That(
                first.LegalTargets.Any(target => target.Kind == AttackTargetKind.Hero &&
                    target.HeroId.Equals(enemyId)),
                Is.True,
                "and the marker bullet admits the hero wearing the bounty. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;
            if (runtime.PendingDefenseChoice != null)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }

            // Milestone 8.49: the declined Defense is a Defeat that p.13 now prices at four Life
            // counters (the Tier III hero's three plus the bounty marker's one), which the staging
            // above dealt the team enough to pay - so the match goes on into the second order.
            Assert.That(runtime.Phase, Is.Not.EqualTo(RoundResolutionPhase.GameEnded),
                "The first order's Defeat spends four counters of five, not the last one. " +
                Remaining(runtime));

            // ORDER TWO: a separate page, with its own target question and its own Defense window.
            var second = runtime.PendingAttackTargetChoice;
            Assert.That(second, Is.Not.Null,
                "\"or both\" runs a SECOND complete p.14 rather than a second consequence inside " +
                "the first. " + Remaining(runtime));
            Assert.That(
                second.LegalTargets.All(target => target.Kind != AttackTargetKind.Hero),
                Is.True,
                "and the adjacent bullet offers units, not the hero the first bullet took - which " +
                "is what \"on different targets\" prints. " + Remaining(runtime));
            var beside = runtime.ReadModel.Minions.Single(minion => minion.MinionId.Equals(BesideHim));
            Assert.That(
                second.LegalTargets.Any(target => target.MinionId.Equals(BesideHim)), Is.True,
                "The Red melee standing beside him is what the second bullet reaches. " +
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(beside.MinionId).transform.position),
                Is.True);
            yield return null;
            yield return null;
            if (runtime.PendingDefenseChoice != null)
            {
                Assert.That(runtime.DeclineDefense(), Is.True, Remaining(runtime));
                yield return null;
                yield return null;
            }

            Assert.That(
                runtime.ReadModel.Minions.Any(minion => minion.MinionId.Equals(BesideHim)), Is.False,
                "Both orders resolved, and the second one removed what it hit. " + Remaining(runtime));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null, Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // The Blue line's other two branches
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Dead Man's Hand: the guessing clause with two coins on a miss.
        ///
        /// <para><b>The journey names a colour it KNOWS is wrong</b>, read off the hidden card's own
        /// printed colour, which is the branch A Game of Chance's journey cannot take: a wrong guess
        /// leaves the card exactly where it was and pays the printed coins instead. The reveal still
        /// happens - "then reveal the card" is before the branch, not inside it - so the card goes back
        /// into a hand every seat has now seen it in, which is BA-D3's permanence.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DeadMansHandMissesItsGuess_AndPaysItsCoinsInstead()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierTwoEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var coinsBefore = CoinsOf(runtime, bainId);

            yield return HisTurnOn(root, bainId, BainCardContent.DeadMansHand);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guess-select"),
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The pick is the enemy's, on their own hand. " + Remaining(runtime));
            var chosen = default(CardInstanceId);
            yield return ClickACardFace(root, card => chosen = card);

            var printed = runtime.Definitions
                .Single(definition => definition.Reference.Equals(
                    runtime.Cards.Single(card => card.Id.Equals(chosen)).DefinitionReference));
            var wrong = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.Value.IntegerValue !=
                    (int)printed.Progression.Color);
            yield return PressEffectControl(root, wrong.Label);

            Assert.That(RevealRows(runtime), Has.Length.EqualTo(1),
                "The reveal is BEFORE the branch, so a wrong guess still turns the card over for " +
                "the whole table. " + Remaining(runtime));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(chosen)).State.Location,
                Is.EqualTo(CardLocation.Hand),
                "\"otherwise\" takes nothing: the revealed card goes back into the hand it never " +
                "left. " + Remaining(runtime));
            Assert.That(CoinsOf(runtime, bainId), Is.EqualTo(coinsBefore + 2),
                "and the printed two coins are paid instead. " + Remaining(runtime));
        }

        /// <summary>
        /// We're Not Done Yet!: the guessing clause whose FAILURE branch is itself a modal -
        /// "otherwise may repeat once or gain 2 coins".
        ///
        /// <para><b>The repeat re-asks EVERYTHING, of anybody</b>, which is BA-Q2 settled from the
        /// printed sentence: a second run re-opens the radius selection, the enemy's own hidden pick
        /// and the colour question, and excludes neither the hero nor the card the first run revealed.
        /// This journey misses twice, so it meets the modal twice - and the second time the repeat
        /// bullet is drawn and NOT takeable, because "may repeat ONCE" has been spent. That greying is
        /// what milestone 8.40f built the feasibility arm for, and this is the only face in the build
        /// that can reach it.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator WereNotDoneYetRepeatsOnceOnAMiss_ThenCanOnlyTakeTheCoins()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var coinsBefore = CoinsOf(runtime, bainId);

            yield return HisTurnOn(root, bainId, BainCardContent.WereNotDoneYet);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            yield return MissOneGuess(root, enemyId, pointerInput);

            var modal = runtime.PendingEffectChoice;
            Assert.That(modal, Is.Not.Null,
                "A wrong guess on the Tier III face opens its own modal rather than paying. " +
                Remaining(runtime));
            Assert.That(modal.NodeId.Value, Is.EqualTo("guess-modal"), Remaining(runtime));
            Assert.That(modal.Candidates.Count, Is.EqualTo(2),
                "Both printed bullets stand on the first miss. " + Remaining(runtime));
            Assert.That(
                modal.SelectInvocations.Select(value => value.Candidate.OptionNodeId.Value)
                    .OrderBy(value => value, System.StringComparer.Ordinal).ToArray(),
                Is.EqualTo(new[] { "guess-coin-bullet", "guess-repeat" }).AsCollection,
                "and both are TAKEABLE the first time. " + Remaining(runtime));

            // The bullet is pressed WITHOUT reaching past it: this modal takes one bullet, so the
            // select resolves it on its own and the confirm standing afterwards belongs to the NEXT
            // node - the trap milestone 8.22's first Play Mode run recorded and the reason
            // ConfirmIfStillStanding names its node.
            UiPlayer.Click(UiPlayer.Button(hud, BulletLabelFor(runtime, "guess-repeat")));
            yield return null;
            yield return null;
            yield return ConfirmIfStillStanding(root, modal.NodeId);

            // The repeat's own decision, which is p.10's `may` on the printed "may repeat once".
            var repeat = runtime.PendingEffectChoice;
            Assert.That(repeat, Is.Not.Null, Remaining(runtime));
            Assert.That(repeat.NodeId.Value, Is.EqualTo("guess-repeat"), Remaining(runtime));
            Assert.That(repeat.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline), Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "\"MAY repeat once\" is answerable both ways. " + Remaining(runtime));
            // Milestone 8.46e: the accept is the decision window's control, labelled for what
            // accepting does, so it is pressed by its command kind rather than by a strip label.
            UiPlayer.ClickEffectControl(runtime, hud, EffectChoiceCommandKind.Confirm);
            yield return null;
            yield return null;

            // THE WHOLE CLAUSE AGAIN, from its own first question.
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guess-select"),
                "The repeat re-asks the SELECTION, which is what \"repeat\" means with no exclusion " +
                "printed beside it. " + Remaining(runtime));
            yield return MissOneGuess(root, enemyId, pointerInput);

            var again = runtime.PendingEffectChoice;
            Assert.That(again, Is.Not.Null,
                "The second miss opens the same modal. " + Remaining(runtime));
            Assert.That(again.NodeId.Value, Is.EqualTo("guess-modal"), Remaining(runtime));
            Assert.That(again.Candidates.Count, Is.EqualTo(2),
                "Both bullets are still DRAWN, so a player can see the option exists. " +
                Remaining(runtime));
            Assert.That(
                again.SelectInvocations.Select(value => value.Candidate.OptionNodeId.Value).ToArray(),
                Is.EqualTo(new[] { "guess-coin-bullet" }).AsCollection,
                "and only the coins can be TAKEN, because \"may repeat once\" has been spent - a " +
                "bullet whose second instance cannot run is greyed rather than hidden. " +
                Remaining(runtime));

            UiPlayer.Click(UiPlayer.Button(hud, BulletLabelFor(runtime, "guess-coin-bullet")));
            yield return null;
            yield return null;
            yield return ConfirmIfStillStanding(root, again.NodeId);
            Assert.That(CoinsOf(runtime, bainId), Is.EqualTo(coinsBefore + 2),
                "and the other arm of the same modal pays the printed two. " + Remaining(runtime));
            Assert.That(RevealRows(runtime), Has.Length.EqualTo(2),
                "Two complete instances ran, so the table saw two reveals. " + Remaining(runtime));
        }

        /// <summary>
        /// Answers one whole guessing instance with a colour the journey knows is wrong: the radius
        /// selection on the board, the enemy's own hidden pick on its face, and a colour control that
        /// is not the hidden card's.
        /// </summary>
        private static IEnumerator MissOneGuess(
            MatchSceneCompositionRoot root, HeroId enemyId, PrimitiveHeroPointerInput pointerInput)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("guess-select"),
                Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, root.Hud, HeroView(enemyId).transform.position),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "The pick belongs to the hero whose hand it is, on every instance. " +
                Remaining(runtime));
            var chosen = default(CardInstanceId);
            yield return ClickACardFace(root, card => chosen = card);

            var printed = runtime.Definitions
                .Single(definition => definition.Reference.Equals(
                    runtime.Cards.Single(card => card.Id.Equals(chosen)).DefinitionReference));
            var wrong = runtime.RoundView.Model.EffectChoices.First(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                value.Command.EffectChoice.Candidate.Value.IntegerValue !=
                    (int)printed.Progression.Color);
            yield return PressEffectControl(root, wrong.Label);
        }

        // -------------------------------------------------------------------------------------------
        // The Blue line's two shared retrievals
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Drinking Buddies: "You may have a hero in radius retrieve a discarded card. If they do, you
        /// may also retrieve a discarded card."
        ///
        /// <para><b>Two retrievals, two seats, and the second one is gated on the first.</b> BA-Q1
        /// settles "a hero in radius" as every hero on the board EXCEPT him, enemies included - the
        /// noun carries no side-word, and "you may ALSO retrieve" means nothing unless the first
        /// retrieval was somebody else's. The first prompt therefore opens on the CHOSEN hero's seat
        /// and reads their own discard pile; the second opens on his, and only because they took
        /// theirs.</para>
        ///
        /// <para><b>The discard piles are staged rather than played for.</b> A dealt match starts with
        /// empty piles, so the sandbox stages each hero holding a subset of what they were dealt, which
        /// discards the rest through the card lifecycle's own transition.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator DrinkingBuddiesLetsTheOtherHeroRetrieveFromTheirOwnSeat_ThenHimself()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBain,
                bainHand: new[] { BainCardContent.DrinkingBuddies, BainCardContent.DeadOrAlive },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.StopProjectiles });
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            Assert.That(DiscardOf(runtime, bainId), Is.Not.Empty,
                "The staged hand is what fills his own discard pile. " + Remaining(runtime));
            Assert.That(DiscardOf(runtime, enemyId), Is.Not.Empty,
                "and theirs. " + Remaining(runtime));
            var hisDiscardBefore = DiscardOf(runtime, bainId).Length;
            var theirDiscardBefore = DiscardOf(runtime, enemyId).Length;

            yield return HisTurnOn(root, bainId, BainCardContent.DrinkingBuddies);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            var selection = runtime.PendingEffectChoice;
            Assert.That(selection, Is.Not.Null, Remaining(runtime));
            Assert.That(selection.NodeId.Value, Is.EqualTo("buddies-select"), Remaining(runtime));
            Assert.That(
                selection.Candidates.Select(candidate => candidate.Value.HeroId).ToArray(),
                Is.EqualTo(new[] { enemyId }).AsCollection,
                "BA-Q1: every hero in the printed Radius 3 EXCEPT him, which on a two-seat board is " +
                "the enemy alone. " + Remaining(runtime));
            Assert.That(
                selection.Candidates.All(candidate => !candidate.Value.HeroId.Equals(bainId)),
                Is.True,
                "and he is not among them, because \"you may ALSO retrieve\" would mean nothing if " +
                "he were. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            // THEIR retrieval, from THEIR pile, answered from THEIR seat.
            var theirs = runtime.PendingEffectChoice;
            Assert.That(theirs, Is.Not.Null,
                "\"have a hero in radius retrieve\" opens on the hero he named. " + Remaining(runtime));
            Assert.That(theirs.NodeId.Value, Is.EqualTo("buddies-retrieve"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "p.10 gives a step's choice to whoever performs it, and this one is theirs. " +
                Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "\"You MAY have a hero retrieve\" is declinable. " + Remaining(runtime));
            Assert.That(
                theirs.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemyId)),
                Is.True,
                "and every offered face is one of THEIR OWN discarded cards - p.10 is explicit that " +
                "\"you can never retrieve another hero's cards\". " + Remaining(runtime));
            yield return ClickACardFace(root);

            Assert.That(DiscardOf(runtime, enemyId), Has.Length.EqualTo(theirDiscardBefore - 1),
                "Exactly the clicked face came back to their hand. " + Remaining(runtime));

            // HIS retrieval, opened only because they took theirs.
            var his = runtime.PendingEffectChoice;
            Assert.That(his, Is.Not.Null,
                "\"If they do, you may also retrieve\" opens on the first retrieval's own published " +
                "fact. " + Remaining(runtime));
            Assert.That(his.NodeId.Value, Is.EqualTo("buddies-also-retrieve"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, bainId), Is.True,
                "and the second sentence is about HIM. " + Remaining(runtime));
            Assert.That(
                his.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(bainId)),
                Is.True,
                "from his own pile and nobody else's. " + Remaining(runtime));
            yield return ClickACardFace(root);

            Assert.That(DiscardOf(runtime, bainId), Has.Length.EqualTo(hisDiscardBefore - 1),
                "and his own card came back too. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        /// <summary>
        /// Another One!: Drinking Buddies' sentence plus "End of turn: May repeat once."
        ///
        /// <para><b>The keyword sentence is the whole Tier difference</b> - BA-12 records that the two
        /// faces print byte-identical first sentences at the same Radius - and it costs Tali's four
        /// content pieces, because <c>EffectNodeKind.RepeatAction</c> re-runs an enclosing ACTION and
        /// there is none at <c>TimingCheckpoint.TurnEnded</c>. What a PLAYER meets is an Active effect
        /// standing on his own tray from the moment the Skill resolves until the turn it belongs to
        /// ends, which is the reading this journey takes.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AnotherOneArmsItsEndOfTurnRepeat_AndTheTrayReadsItWhileItStands()
        {
            yield return RestagedScene(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBain,
                bainHand: new[] { BainCardContent.AnotherOne, BainCardContent.DeadOrAlive },
                enemyHand: new[] { WaspCardContent.Shock, WaspCardContent.StopProjectiles });
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            yield return HisTurnOn(root, bainId, BainCardContent.AnotherOne);
            var sourceCardId = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.IsActiveEffectSource(sourceCardId), Is.False,
                "Nothing is standing before the Skill is performed. " + Remaining(runtime));

            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            yield return null;

            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("buddies-select"),
                "The shared clause is the same one Drinking Buddies prints. " + Remaining(runtime));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True);
            yield return null;
            yield return null;

            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True, Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null,
                "\"You MAY have a hero retrieve\" is declinable on this face too. " +
                Remaining(runtime));
            yield return ClickACardFace(root);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("buddies-also-retrieve"),
                Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, bainId), Is.True, Remaining(runtime));
            Assert.That(DeclineControl(runtime), Is.Not.Null, Remaining(runtime));
            yield return ClickACardFace(root);

            // THE READING: the keyword sentence is standing, and the player sees it on the card.
            Assert.That(runtime.IsActiveEffectSource(sourceCardId), Is.True,
                "\"End of turn: May repeat once\" is an Active effect this card is the live source " +
                "of from the moment it resolves. " + Remaining(runtime));

            yield return PointTrayAt(root, bainId);
            yield return null;
            var slot = UiPlayer.NamedElementWithText(
                hud, "gd-card-slot", NameOf(runtime, BainCardContent.AnotherOne));
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "and his own tray draws the card's slot wearing the Active-effect outline while it " +
                "lives, which is the only place a player can read that the turn still owes them a " +
                "repeat.");

            // It outlives the moment that raised it, which is what separates the two readings: the
            // resolution has finished and the effect is still standing, waiting for the turn to end.
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(sourceCardId), Is.True,
                "The clause is not spent by the Skill that raised it; it is waiting for p.8's own " +
                "end-of-turn moment. " + Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // The Green line
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Narrow Escape: "If a hero in play has a bounty marker, block the attack and retrieve the
        /// marker."
        ///
        /// <para><b>"Retrieve the marker" is the rulebook's word for returning one to the supply</b>,
        /// and Narrow Escape gave none - so it is milestone 8.40b's definition-bound reading of the
        /// return: the one attachment of THAT definition, wherever it sits. What a player reads is the
        /// row LEAVING the rail it was on, which is the same reading the give leaves taken from the
        /// other side.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator NarrowEscapeBlocksTheAttack_AndReturnsTheBountyToTheSupply()
        {
            yield return BlockJourney(
                SceneCardContent.VerifiedTierTwoEngagedStagedBain,
                BainCardContent.NarrowEscape,
                afterTheBlock: (runtime, hud, bainId, enemyId) =>
                {
                    Assert.That(BountyHolders(runtime), Is.Empty,
                        "\"retrieve the marker\" took the one attachment off the board, so nobody " +
                        "is wearing a bounty afterwards. " + Remaining(runtime));
                    HeroRailMarkerCheck.AssertReadings(
                        hud, TeamSide.Red, "the returned bounty");
                    HeroRailMarkerCheck.AssertReadings(
                        hud, TeamSide.Blue, "the returned bounty");
                });
        }

        /// <summary>
        /// Perfect Getaway: "If a hero in play has a bounty marker, block the attack."
        ///
        /// <para><b>The only Green of his that carries no consequence at all</b>: its whole printed
        /// text IS the Block's condition, which is why its matrix row names neither a Defense-text
        /// application nor a marker reading. What this journey proves is the half a value-comparing
        /// Defense cannot have - the offer reports itself as a SUCCESSFUL defence before it is taken,
        /// because the board fact it reads is already true, and the attack it answers is one the
        /// printed Defense values could not have survived.</para>
        ///
        /// <para><b>The negative - that the same face blocks nothing while no bounty is out - is owned
        /// at the authoritative tier</b> by <c>BainMarkerPredicateTests.TheBlockCondition_IsTrueOnly
        /// WhileAHeroWearsHisMarker</c>, because reaching it here would need a second attack in a
        /// second round on a board the first one had already emptied of markers.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator PerfectGetawayBlocksTheAttackOutright_WhileABountyIsOnTheBoard()
        {
            yield return BlockJourney(
                SceneCardContent.VerifiedTierThreeEngagedStagedBain,
                BainCardContent.PerfectGetaway,
                afterTheBlock: (runtime, hud, bainId, enemyId) =>
                {
                    Assert.That(BountyHolders(runtime),
                        Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                        "This face's whole text is its own condition, so the marker is exactly " +
                        "where it was. " + Remaining(runtime));
                    HeroRailMarkerCheck.AssertReadings(
                        hud, TeamSide.Red, "the answered Block", "BOUNTY");
                });
        }

        /// <summary>
        /// The shape all three of his Green Blocks share: arm the bounty, keep the Green in hand,
        /// let the enemy attack, read the rail, and answer p.14's prompt by clicking the face.
        /// </summary>
        private static IEnumerator BlockJourney(
            SceneCardContent content,
            CardDefinitionId green,
            System.Action<ConfiguredMatchSceneRuntime, ConfiguredMatchHudRenderer, HeroId, HeroId>
                afterTheBlock)
        {
            yield return RestagedScene(content, enemyAt: new HexCoordinate(-2, 1));
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);

            yield return ArmTheBounty(root, bainId, enemyId);
            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                "The Block's condition is a board fact, so the setup half has to make it true. " +
                Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            Commit(runtime, enemyId, HighestInitiativeAttackIdOf(runtime, enemyId));
            Commit(runtime, bainId, LowestInitiativeIdOf(runtime, bainId, green));
            yield return YieldUntilTheNamedSeatActs(runtime, enemyId);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(enemyId),
                "The enemy's Attack must act before his own card, or the Green is never offered as " +
                "a Defense. " + Remaining(runtime));

            // THE READING THE BLOCK IS DECIDED FROM, taken before anything is answered.
            HeroRailMarkerCheck.AssertReadings(hud, TeamSide.Red, "the armed board", "BOUNTY");

            Assert.That(runtime.SubmitAction(PrimaryActionOf(runtime)), Is.True, Remaining(runtime));
            var target = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind == AttackTargetKind.Hero &&
                    candidate.HeroId.Equals(bainId));
            Assert.That(
                runtime.SubmitProjectedCommand(DirectorCommand.ChooseAttackTarget(target.HeroId)),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null,
                "The journey needs p.14's Defense response standing. " + Remaining(runtime));
            var offered = runtime.RoundView.Model.DefenseChoices
                .Where(value => value.Card != null)
                .ToArray();
            var blockOffer = offered.FirstOrDefault(value =>
                value.Card.DisplayName.ToUpperInvariant() == NameOf(runtime, green));
            Assert.That(blockOffer, Is.Not.Null,
                NameOf(runtime, green) + " is offered as a whole face in the defender's prompt. " +
                "Offered: " + string.Join(", ", offered.Select(value => value.Card.DisplayName)) +
                ". " + Remaining(runtime));
            Assert.That(blockOffer.DefenseBreakdown, Is.Not.Null, Remaining(runtime));
            Assert.That(blockOffer.DefenseBreakdown.Outcome,
                Is.EqualTo(AttackResolutionOutcome.SuccessfullyDefended),
                "and the prompt tells the defender, before they answer, that this face BLOCKS - " +
                "which it does because a hero in play wears his bounty, not because of any printed " +
                "value. " + Remaining(runtime));

            yield return ClickDefenseFace(root, green);

            Assert.That(runtime.PendingDefenseChoice, Is.Null,
                "The clicked face answers p.14's response. Shown: " + UiPlayer.Describe(hud));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(bainId)), Is.True,
                "\"block the attack\" succeeds regardless of the Attack Value. " + Remaining(runtime));
            afterTheBlock(runtime, hud, bainId, enemyId);
        }

        // -------------------------------------------------------------------------------------------
        // The Green line's two Movement primaries
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Vantage Point: "Ignore obstacles. If a hero in play has a bounty marker, +1 movement."
        ///
        /// <para><b>Two sentences, two Active effects, and both run BEFORE the walk</b> - p.11's
        /// default, and the only reading under which either does anything. What a player meets is one
        /// Movement session whose allowance is the printed value plus one and whose offer contains
        /// spaces an ordinary walk could not reach, because the route to them crosses an obstacle.
        /// Both halves are measured against the board rather than written down: the raise against the
        /// face's own printed Movement, and the permission against a breadth-first search that stops
        /// where p.18 does.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator VantagePointIgnoresObstacles_AndRaisesHisMovementWhileABountyIsOut()
        {
            yield return StrideJourney(
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBain,
                BainCardContent.VantagePoint,
                printedRaise: 1);
        }

        /// <summary>
        /// High Ground: Vantage Point's shape with one more space of raise - "+2 movement" - which is
        /// the whole of its Tier difference.
        /// </summary>
        [UnityTest]
        public IEnumerator HighGroundRaisesHisMovementByTwo_AndCrossesWhatItIgnores()
        {
            yield return StrideJourney(
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBain,
                BainCardContent.HighGround,
                printedRaise: 2);
        }

        private static IEnumerator StrideJourney(
            SceneCardContent content, CardDefinitionId face, int printedRaise)
        {
            yield return LoadStagedScene(content);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var printed = PrintedMovementOf(runtime, face);

            yield return ArmTheBounty(root, bainId, enemyId);
            Assert.That(BountyHolders(runtime), Is.Not.Empty,
                "The raise is gated on a hero in play wearing his bounty, so the setup half has to " +
                "put one out. " + Remaining(runtime));
            yield return AdvanceToNextTurn(runtime);

            yield return HisTurnOn(root, bainId, face);
            var sourceCardId = runtime.SelectedActor.CardInstanceId;
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Movement);
            yield return null;

            var walk = runtime.PendingMovementChoice;
            Assert.That(walk, Is.Not.Null,
                "Its printed primary is a Movement, and the card's own text asks nothing of its " +
                "own. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null,
                "p.11 runs the text BEFORE the movement and neither sentence opens a control. " +
                Remaining(runtime));
            Assert.That(runtime.IsActiveEffectSource(sourceCardId), Is.True,
                "and both sentences are standing as Active effects this card is the live source of. " +
                Remaining(runtime));

            // The player reads them on his own tray, as the played card's slot wearing the Active
            // outline - which is where an effect that draws no control of its own can be read at all.
            var slot = UiPlayer.NamedElementWithText(hud, "gd-card-slot", NameOf(runtime, face));
            Assert.That(slot.resolvedStyle.borderTopColor.a, Is.GreaterThan(0.5f),
                "The card's slot wears the Active-effect outline while its two sentences live.");
            Assert.That(walk.RemainingAllowance, Is.EqualTo(printed + printedRaise),
                "The session opens on the printed " + printed + " raised by the printed " +
                printedRaise + ", which is what \"" + printed + "+\" prints and BA-09 records. " +
                Remaining(runtime));

            var origin = runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate;
            var ordinary = ReachableWithoutCrossing(runtime, origin, walk.RemainingAllowance);
            var onlyByCrossing = walk.LegalDestinations
                .Select(value => value.Destination)
                .Where(value => !ordinary.Contains(value))
                .OrderBy(value => value.Q).ThenBy(value => value.R)
                .ToArray();
            Assert.That(onlyByCrossing, Is.Not.Empty,
                "\"Ignore obstacles\" has to reach somewhere an ordinary walk of the same length " +
                "cannot, or the permission is drawing nothing. Offered " +
                walk.LegalDestinations.Count + ", ordinarily reachable " + (ordinary.Count - 1) +
                ". " + Remaining(runtime));

            var destination = FirstClickableSpace(pointerInput, onlyByCrossing);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, Tile(destination).transform.position), Is.True,
                "The destination is answered on the board. " +
                UiPlayer.DescribeBoardRay(pointerInput, Tile(destination).transform.position));
            yield return null;
            yield return null;

            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(bainId)).Coordinate,
                Is.EqualTo(destination),
                "and he is standing on a space the obstacles between would otherwise have refused. " +
                Remaining(runtime));
        }

        // -------------------------------------------------------------------------------------------
        // p.16's Ultimate
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// A Complicated Profession: "After you give a hero the bounty marker that hero discards a
        /// card, if able."
        ///
        /// <para><b>A permanent ability that fires on a moment rather than on an action</b>, which
        /// milestone 8.40h built as a twentieth <c>TimingCheckpoint</c> and a seventh
        /// <c>UltimateTriggerCondition</c>. What a player meets is: the unlocked card faceup in his own
        /// area, then - the instant his give lands - a prompt on the RECIPIENT's seat asking which of
        /// their own cards they lose. BA-D5 is why the trigger reads the giver: Close Call's transfer
        /// is another hero giving the marker to him, and it must not fire this.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AComplicatedProfessionFiresOnHisGive_AndTheRecipientDiscardsFromTheirOwnSeat()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedBain);
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            var bainId = BainSeat(runtime).HeroId;
            var enemyId = OtherHeroId(runtime, bainId);
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

            // p.16 puts the unlocked Ultimate faceup in the hero's area, which is a thing a player
            // must be able to SEE before it is a thing that can happen to them.
            yield return PointTrayAt(root, bainId);
            yield return null;
            var ultimate = UiPlayer.ItemStripDescendants(hud)
                .Where(element => element.name == "gd-ultimate-card" && UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(ultimate, Is.Not.Empty,
                "His unlocked Ultimate is drawn in his own area. Shown: " + UiPlayer.Describe(hud));
            Assert.That(
                ultimate,
                Does.Contain(UiPlayer.NamedElementWithText(
                    hud, "gd-ultimate-card",
                    NameOf(runtime, BainCardContent.AComplicatedProfession))),
                "and it is HIS Ultimate by name rather than a placeholder, in the items strip.");

            yield return HisTurnOn(root, bainId, BainCardContent.DeadOrAlive);
            yield return PointTrayAt(root, bainId);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            yield return null;

            var victim = runtime.PendingAttackTargetChoice.LegalTargets
                .First(candidate => candidate.Kind != AttackTargetKind.Hero);
            var piece = runtime.ReadModel.Minions
                .Single(minion => minion.MinionId.Equals(victim.MinionId));
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, MinionView(piece.MinionId).transform.position),
                Is.True);
            yield return null;
            yield return null;

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            var confirm = runtime.RoundView.Model.EffectChoices.Single(value =>
                value.Command.EffectChoice.Kind == EffectChoiceCommandKind.Confirm);
            UiPlayer.ClickEffectAnswer(runtime, hud, confirm.Command);
            yield return null;
            yield return null;

            var handBefore = HandCountOf(runtime, enemyId);
            Assert.That(
                UiPlayer.ClickBoard(pointerInput, hud, HeroView(enemyId).transform.position), Is.True,
                "The recipient is named by clicking the piece where they stand.");
            yield return null;
            yield return null;

            Assert.That(BountyHolders(runtime), Is.EqualTo(new[] { enemyId.Value }).AsCollection,
                Remaining(runtime));

            // THE TRIGGER, AND THE SEAT IT IS PUT TO.
            var discard = runtime.PendingEffectChoice;
            Assert.That(discard, Is.Not.Null,
                "The Ultimate fires the instant the give lands, which is the moment milestone " +
                "8.40h's twentieth checkpoint names. " + Remaining(runtime));
            Assert.That(discard.NodeId.Value, Is.EqualTo("profession-discard"), Remaining(runtime));
            Assert.That(StandingChoiceBelongsTo(runtime, enemyId), Is.True,
                "\"that hero discards a card\" is answered by the hero who received the marker, " +
                "from their own seat and out of their own hand. " + Remaining(runtime));
            Assert.That(
                discard.Candidates.All(candidate => runtime.Cards
                    .Single(card => card.Id.Equals(candidate.Value.CardInstanceId))
                    .OwnerId.Equals(enemyId)),
                Is.True,
                Remaining(runtime));

            yield return ClickACardFace(root);
            Assert.That(HandCountOf(runtime, enemyId), Is.EqualTo(handBefore - 1),
                "and exactly the clicked face left their hand. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice, Is.Null, Remaining(runtime));
        }

        private static int CoinsOf(ConfiguredMatchSceneRuntime runtime, HeroId heroId) =>
            runtime.Projection.HeroProgression
                .Single(progression => progression.HeroId.Equals(heroId))
                .Coins;
    }
}
