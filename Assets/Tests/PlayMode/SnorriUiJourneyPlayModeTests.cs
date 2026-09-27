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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.93g. Real pointer and control journeys for Snorri the Runescribe. A rune-gated
    /// face is played the way a player reaches it: Inscribe the Runes resolves in turn 1 with a
    /// permutation that puts the needed rune under turn 2, and the face is played in turn 2.
    /// </summary>
    public sealed class SnorriUiJourneyPlayModeTests
    {
        private static readonly HexCoordinate SnorriSpace = new HexCoordinate(-3, 1);
        private static readonly HexCoordinate AdjacentSpace = new HexCoordinate(-2, 1);
        private static readonly HexCoordinate TwoAwaySpace = new HexCoordinate(-1, 1);
        private static readonly HexCoordinate ThreeAwaySpace = new HexCoordinate(0, 1);
        private static readonly HexCoordinate BesideThreeAwaySpace = new HexCoordinate(0, 0);
        private static readonly HexCoordinate OtherAdjacentSpace = new HexCoordinate(-3, 2);
        private static readonly MinionId StagedMinion = new MinionId("snorri-journey-minion");

        internal static HeroId Snorri(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "snorri").HeroId;

        private static HeroId HeroOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, string content) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == content).HeroId;

        private static HexCoordinate SpaceOf(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        /// <summary>
        /// Loads <paramref name="scene"/>, then reloads it staged: Snorri on <see cref="SnorriSpace"/>
        /// with <paramref name="hand"/>, Wasp (and Arien where the board deals her) where asked, and
        /// an optional enemy minion. Minions standing where a staged piece goes are cleared.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wasp = null, CardDefinitionId[] waspHand = null,
            HexCoordinate? arien = null, HexCoordinate? minion = null, int? coins = null)
        {
            yield return LoadStagedScene(scene);
            var runtime = Root().Runtime;
            var snorri = Snorri(runtime);
            var waspId = HeroOf(runtime, "wasp");
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(snorri, SnorriSpace, coins: coins, hand: hand),
                new SandboxHeroStaging(waspId, wasp ?? AdjacentSpace, hand: waspHand ?? new[]
                {
                    WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                    WaspCardContent.Shock, WaspCardContent.LiftUp
                })
            };
            var occupied = new List<HexCoordinate> { SnorriSpace, wasp ?? AdjacentSpace };
            if (runtime.Setup.Heroes.Any(hero => hero.ContentId.Value == "arien"))
            {
                var arienSpace = arien ?? OtherAdjacentSpace;
                heroes.Add(new SandboxHeroStaging(HeroOf(runtime, "arien"), arienSpace));
                occupied.Add(arienSpace);
            }
            if (minion.HasValue) occupied.Add(minion.Value);
            var enemySide = runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(waspId)).TeamSide;
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                spawnedMinions: minion.HasValue
                    ? new[]
                    {
                        new SandboxMinionStaging(StagedMinion, enemySide,
                            StandardMinionType.Melee, minion.Value)
                    }
                    : null,
                removedMinions: runtime.ReadModel.Minions
                    .Where(value => occupied.Contains(value.Coordinate))
                    .Select(value => value.MinionId));
            yield return LoadStagedScene(scene);
        }

        /// <summary>Ends turns (answering a tied Initiative by its window) until the named turn.</summary>
        internal static IEnumerator EndTurnsUntil(MatchSceneCompositionRoot root, int turn)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 30 && runtime.ReadModel.TurnIndex < turn; budget++)
            {
                var tie = UiPlayer.DecisionControl(runtime, command =>
                    command.Kind == DirectorCommandKind.ChooseTiedActor);
                if (tie != null) UiPlayer.ClickDecisionControl(runtime, root.Hud, tie);
                else if (runtime.RoundView.Model.EndTurnInvocation != null)
                    UiPlayer.Click(UiPlayer.Button(root.Hud,
                        runtime.RoundView.Model.EndTurnInvocation.Label));
                yield return null;
            }
            Assert.That(runtime.ReadModel.TurnIndex, Is.EqualTo(turn), Remaining(runtime));
        }

        /// <summary>
        /// Drags nothing and clicks each rune then its slot, then DONE - the focus path to the same
        /// typed pairs the drag submits. <paramref name="slotForRune"/> is indexed by rune 1-4.
        /// </summary>
        internal static IEnumerator PlaceRunes(MatchSceneCompositionRoot root, int[] slotForRune)
        {
            var hud = root.Hud.RootElement;
            for (var rune = 1; rune <= 4; rune++)
            {
                UiPlayer.Click(hud.Q<Button>("gd-rune-piece-" + rune));
                yield return null;
                UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slotForRune[rune]));
                yield return null;
            }
            UiPlayer.Click(hud.Q<Button>("gd-rune-slot-done"));
            yield return null;
            yield return null;
        }

        /// <summary>Rune N to a slot so that <paramref name="turnTwoRune"/> stands under turn 2.</summary>
        internal static int[] PermutationFor(int turnTwoRune)
        {
            var slots = new int[5];
            slots[turnTwoRune] = 2;
            var free = new Queue<int>(new[] { 1, 3, 4 });
            for (var rune = 1; rune <= 4; rune++)
                if (rune != turnTwoRune) slots[rune] = free.Dequeue();
            return slots;
        }

        /// <summary>
        /// Turn 1: Inscribe the Runes by real input with <paramref name="turnTwoRune"/> under turn 2.
        /// Every other seat commits its lowest card and ends its turn. Leaves the match at turn 2.
        /// </summary>
        internal static IEnumerator Inscribe(MatchSceneCompositionRoot root, int turnTwoRune)
        {
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            yield return CommitEverySeat(root, snorri,
                NameOf(runtime, SnorriSilverFace.InscribeTheRunes));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            if (runtime.PendingEffectChoice != null &&
                runtime.PendingEffectChoice.ValueVocabulary != EffectChoiceValueVocabulary.RuneTurnSlot)
                yield return AnswerRuneMastery(root, 1);
            yield return PlaceRunes(root, PermutationFor(turnTwoRune));
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri).Count(), Is.EqualTo(4),
                Remaining(runtime));
            yield return EndTurnsUntil(root, 2);
        }

        /// <summary>Commits the face this turn and starts its action through the tray control.</summary>
        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId face,
            ActionKind action)
        {
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            yield return CommitEverySeat(root, snorri, NameOf(runtime, face));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, action);
        }

        /// <summary>Rune Mastery's pre-action choice, answered through its window by rune ordinal.</summary>
        internal static IEnumerator AnswerRuneMastery(MatchSceneCompositionRoot root, int? rune)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, Remaining(runtime));
            var candidate = rune.HasValue
                ? choice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Integer &&
                    value.Value.IntegerValue == rune.Value)
                : choice.Candidates.First(value => value.Value.Kind == EffectValueKind.Integer);
            var control = UiPlayer.DecisionControl(runtime, command =>
                command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                command.EffectChoice.Candidate.Equals(candidate));
            Assert.That(control, Is.Not.Null, "No window control for rune " + candidate.Value.IntegerValue +
                ". " + UiPlayer.Describe(root.Hud));
            UiPlayer.Click(UiPlayer.Button(root.Hud, control.Label));
            yield return null;
            yield return null;
        }

        /// <summary>Chooses a hero as the Attack's target, by board click or its effect choice.</summary>
        internal static IEnumerator Target(MatchSceneCompositionRoot root, HeroId hero)
        {
            var runtime = root.Runtime;
            var effects = runtime.PendingEffectChoice?.Candidates.Where(value =>
                value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(hero))
                .ToArray() ?? Array.Empty<EffectChoiceCandidate>();
            if (effects.Length == 1)
            {
                yield return Pick(root, effects[0]);
                yield break;
            }
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(hero).transform.position), Is.True, Remaining(runtime));
            yield return null;
        }

        private static string Pause(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime)
        {
            var choice = runtime.PendingEffectChoice;
            return choice == null ? "none" : choice.NodeId.Value + " kind=" + choice.Kind +
                " min=" + choice.MinimumSelections + " max=" + choice.MaximumSelections +
                " confirm=" + (choice.ConfirmInvocation != null) + " decline=" +
                (choice.DeclineInvocation != null) + " candidates=" + string.Join(",",
                    choice.Candidates.Select(value => value.Value.Kind + ":" + value.Value));
        }

        internal static IEnumerator TakeTheHit(MatchSceneCompositionRoot root)
        {
            Assert.That(root.Runtime.PendingDefenseChoice, Is.Not.Null, Remaining(root.Runtime));
            UiPlayer.ClickDefenseDecline(root.Runtime, root.Hud);
            yield return null;
        }

        private static CardInstance Card(
            GuardsDigital.Presentation.Match.ConfiguredMatchSceneRuntime runtime, HeroId owner,
            CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        /// <summary>Wasp attacks Snorri with Magnetic Dagger this turn and waits at his Defense.</summary>
        private static IEnumerator WaspAttacksSnorri(MatchSceneCompositionRoot root,
            CardDefinitionId snorriCommits)
        {
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            var wasp = HeroOf(runtime, "wasp");
            yield return PointTrayAt(root, snorri);
            yield return CommitThroughTray(root, NameOf(runtime, snorriCommits));
            yield return PointTrayAt(root, wasp);
            yield return CommitThroughTray(root, NameOf(runtime, WaspCardContent.MagneticDagger));
            yield return YieldUntilTheNamedSeatActs(runtime, wasp);
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, HeroView(snorri).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
        }

        [UnityTest]
        public IEnumerator RuneSigilsRepeatsOnTheOtherEnemyHeroUnderRuneFour()
        {
            yield return Stage(SceneCardContent.VerifiedStagedSnorriVersusTwo, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriAttackFaces.RuneSigils,
                SnorriBlueMovementFaces.SafePassage
            }, arien: OtherAdjacentSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            var arien = HeroOf(runtime, "arien");
            yield return Inscribe(root, 4);
            yield return Begin(root, SnorriAttackFaces.RuneSigils, ActionKind.Attack);
            yield return Target(root, wasp);
            yield return TakeTheHit(root);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Rune 4 offers the printed repeat on a different hero in range. " + Remaining(runtime));
            // "Repeat once" is not a may: the repeat asks straight for the different hero.
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)) ?? false,
                Is.False, "The repeat excludes the hero already attacked.");
            yield return Target(root, arien);
            Assert.That(runtime.PendingDefenseChoice?.ChooserId ?? default, Is.EqualTo(arien),
                "The repeated Attack reaches Arien's own Defense. " + Remaining(runtime));
            yield return TakeTheHit(root);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EveryRetrievingRedFaceReturnsTheSpentDefenseCardUnderRuneThree()
        {
            foreach (var face in new[]
            {
                SnorriAttackFaces.RunicDagger, SnorriAttackFaces.RunicHammer,
                SnorriAttackFaces.RunicBattleaxe
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriBlueMovementFaces.SafePassage
                });
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                var wasp = HeroOf(runtime, "wasp");
                // Turn 1: Wasp's Magnetic Dagger acts first; Snorri spends Safe Passage on Defense.
                yield return WaspAttacksSnorri(root, SnorriSilverFace.InscribeTheRunes);
                yield return ClickDefenseFace(root, SnorriBlueMovementFaces.SafePassage);
                var spent = Card(runtime, snorri, SnorriBlueMovementFaces.SafePassage);
                Assert.That(spent.State.Location, Is.EqualTo(CardLocation.DiscardPile));
                yield return WaitForActor(root, snorri);
                yield return PointTrayAt(root, snorri);
                yield return StartActionThroughControl(root, ActionKind.Skill);
                yield return PlaceRunes(root, PermutationFor(3));
                yield return EndTurnsUntil(root, 2);
                yield return Begin(root, face, ActionKind.Attack);
                yield return Target(root, wasp);
                yield return TakeTheHit(root);
                for (var budget = 0; budget < 4 && runtime.PendingEffectChoice != null &&
                    !runtime.PendingEffectChoice.Candidates.Any(value =>
                        value.Value.Kind == EffectValueKind.CardInstance); budget++)
                    yield return May(root, true);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " offers the discard to retrieve. " + Remaining(runtime));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.CardInstance &&
                    value.Value.CardInstanceId.Equals(spent.Id)));
                Assert.That(Card(runtime, snorri, SnorriBlueMovementFaces.SafePassage).State.Location,
                    Is.EqualTo(CardLocation.Hand), face.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HammerAndBattleaxeMoveOneBeforeTheirAttackUnderRuneFour()
        {
            foreach (var face in new[]
            {
                SnorriAttackFaces.RunicHammer, SnorriAttackFaces.RunicBattleaxe
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriBlueMovementFaces.SafePassage
                }, wasp: TwoAwaySpace);
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return Inscribe(root, 4);
                yield return Begin(root, face, ActionKind.Attack);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " asks for its rune 4 move before the Attack. " + Remaining(runtime));
                yield return May(root, true);
                yield return Space(root, AdjacentSpace);
                Assert.That(SpaceOf(runtime, snorri), Is.EqualTo(AdjacentSpace),
                    face.Value + " moved Snorri one space before targeting.");
                yield return Target(root, wasp);
                yield return TakeTheHit(root);
                while (runtime.PendingEffectChoice != null) yield return May(root, false);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BattleaxeRepeatsItsAttackOnAnEnemyMinionUnderRuneTwo()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriAttackFaces.RunicBattleaxe,
                SnorriBlueMovementFaces.SafePassage
            }, minion: OtherAdjacentSpace);
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return Inscribe(root, 2);
            yield return Begin(root, SnorriAttackFaces.RunicBattleaxe, ActionKind.Attack);
            yield return Target(root, wasp);
            yield return TakeTheHit(root);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, Remaining(runtime));
            yield return May(root, true);
            var minions = runtime.PendingEffectChoice?.Candidates.Where(value =>
                value.Value.Kind == EffectValueKind.Minion &&
                value.Value.MinionId.Equals(StagedMinion)).ToArray() ?? Array.Empty<EffectChoiceCandidate>();
            if (minions.Length == 1) yield return Pick(root, minions[0]);
            else
            {
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, MinionView(StagedMinion).transform.position), Is.True);
                yield return null;
            }
            Assert.That(runtime.Projection.Minions.Any(value => value.MinionId.Equals(StagedMinion)),
                Is.False, "The repeated Attack defeats the chosen enemy minion. " + Remaining(runtime));
            while (runtime.PendingEffectChoice != null) yield return May(root, false);
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator CasterAndBlasterMakeAHeroBesideTheirTargetDiscardUnderRuneTwo()
        {
            foreach (var face in new[]
            {
                SnorriAttackFaces.Runecaster, SnorriAttackFaces.Runeblaster
            })
            {
                yield return Stage(SceneCardContent.VerifiedStagedSnorriVersusTwo, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriBlueMovementFaces.SafePassage
                }, wasp: ThreeAwaySpace, arien: BesideThreeAwaySpace);
                var root = Root();
                var runtime = root.Runtime;
                var wasp = HeroOf(runtime, "wasp");
                var arien = HeroOf(runtime, "arien");
                yield return Inscribe(root, 2);
                yield return Begin(root, face, ActionKind.Attack);
                var arienHand = runtime.Cards.Count(card => card.OwnerId.Equals(arien) &&
                    card.State.Location == CardLocation.Hand);
                yield return Target(root, wasp);
                yield return TakeTheHit(root);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " asks which enemy hero beside the target discards. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(arien)),
                    Is.True, face.Value + " names Arien, beside the target, as the hero who discards.");
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(arien)));
                Assert.That(runtime.PendingEffectChoice?.Candidates.Any(value =>
                    value.Value.Kind == EffectValueKind.CardInstance) ?? false, Is.True,
                    face.Value + " lets Arien choose the discarded card. " + Remaining(runtime));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.First(value =>
                    value.Value.Kind == EffectValueKind.CardInstance));
                Assert.That(runtime.Cards.Count(card => card.OwnerId.Equals(arien) &&
                    card.State.Location == CardLocation.Hand), Is.EqualTo(arienHand - 1), face.Value);
                while (runtime.PendingEffectChoice != null) yield return May(root, false);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator CasterAndBlasterMayMoveUpToTwoAfterTheAttackUnderRuneFour()
        {
            foreach (var face in new[]
            {
                SnorriAttackFaces.Runecaster, SnorriAttackFaces.Runeblaster
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriBlueMovementFaces.SafePassage
                }, wasp: ThreeAwaySpace);
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                var wasp = HeroOf(runtime, "wasp");
                yield return Inscribe(root, 4);
                yield return Begin(root, face, ActionKind.Attack);
                yield return Target(root, wasp);
                yield return TakeTheHit(root);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " offers its rune 4 move after the Attack. " + Remaining(runtime));
                // "Move up to 2 spaces" is asked directly, and staying put is one of its answers.
                var before = SpaceOf(runtime, snorri);
                yield return Space(root, FirstClickableSpace(
                    Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingEffectChoice.Candidates
                        .Where(value => value.Value.Kind == EffectValueKind.Coordinate &&
                            !value.Value.Coordinate.Equals(before))
                        .Select(value => value.Value.Coordinate).ToArray()));
                Assert.That(SpaceOf(runtime, snorri), Is.Not.EqualTo(before), face.Value);
                while (runtime.PendingEffectChoice != null) yield return May(root, false);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator ThreePassagesCreateTheirRuneEffectBeforeMoving()
        {
            foreach (var face in new[]
            {
                SnorriBlueMovementFaces.SafePassage, SnorriBlueMovementFaces.HiddenPassage,
                SnorriBlueMovementFaces.DeepPassage
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriAttackFaces.RunicDagger
                });
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                yield return Inscribe(root, 1);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty);
                yield return Begin(root, face, ActionKind.Movement);
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Count.EqualTo(1),
                    face.Value + " creates its rune 1 obstacle permission before the move. " +
                    Remaining(runtime));
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
                var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, snorri), Is.EqualTo(target), face.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator AncestralSkillsLetTheFriendSwapAResolvedCardUnderRuneTwo()
        {
            foreach (var face in new[]
            {
                SnorriBlueSupportFaces.AncestralBoon.Id, SnorriBlueSupportFaces.AncestralGrace.Id
            })
            {
                yield return Stage(SceneCardContent.VerifiedStagedSnorriWithAlly, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriAttackFaces.RunicDagger
                }, arien: OtherAdjacentSpace);
                var root = Root();
                var runtime = root.Runtime;
                var arien = HeroOf(runtime, "arien");
                yield return Inscribe(root, 2);
                var resolved = runtime.Cards.Single(card => card.OwnerId.Equals(arien) &&
                    card.State.Location == CardLocation.TurnArea &&
                    card.State.ResolutionStatus == CardResolutionStatus.Resolved);
                yield return Begin(root, face, ActionKind.Skill);
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    face.Value + " asks which friendly hero in radius. " + Remaining(runtime));
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(arien)));
                yield return May(root, true);
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.CardInstance &&
                    value.Value.CardInstanceId.Equals(resolved.Id)));
                var fromHand = runtime.PendingEffectChoice.Candidates.First(value =>
                    value.Value.Kind == EffectValueKind.CardInstance);
                yield return Pick(root, fromHand);
                Assert.That(runtime.Cards.Single(card => card.Id.Equals(resolved.Id)).State.Location,
                    Is.EqualTo(CardLocation.Hand), face.Value + ": the resolved card returns to hand.");
                Assert.That(runtime.Cards.Single(card =>
                        card.Id.Equals(fromHand.Value.CardInstanceId)).State.Location,
                    Is.EqualTo(CardLocation.TurnArea), face.Value + ": the hand card takes its slot.");
                while (runtime.PendingEffectChoice != null) yield return May(root, false);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator OathsBlockABasicAttackUnderRuneFourAndLeaveSnorriImmune()
        {
            foreach (var face in new[]
            {
                SnorriGreenFaces.OathOfEndurance, SnorriGreenFaces.OathOfFortitude,
                SnorriGreenFaces.OathOfPerseverance
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
                {
                    SnorriSilverFace.InscribeTheRunes, face, SnorriAttackFaces.RunicDagger
                });
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                yield return Inscribe(root, 4);
                yield return WaspAttacksSnorri(root, SnorriAttackFaces.RunicDagger);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(face)), Is.True, face.Value);
                yield return ClickDefenseFace(root, face);
                Assert.That(runtime.Projection.Heroes.Any(hero => hero.HeroId.Equals(snorri)),
                    Is.True, face.Value + " Blocks the basic Attack under rune 4.");
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds.Contains(
                    Card(runtime, snorri, face).Id), Is.True,
                    face.Value + " leaves its this-turn immunity in effect. " + Remaining(runtime));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RunestoneMakesTheEnemyDiscardTheActiveRunesColour()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriGreenFaces.ChargedRunestone,
                SnorriAttackFaces.RunicDagger
            }, waspHand: new[]
            {
                WaspCardContent.MassTelekinesis, WaspCardContent.StopProjectiles,
                WaspCardContent.Telekinesis, WaspCardContent.DeflectProjectiles,
                WaspCardContent.MagneticDagger
            });
            var root = Root();
            var runtime = root.Runtime;
            var wasp = HeroOf(runtime, "wasp");
            yield return Inscribe(root, 4);
            Assert.That(Card(runtime, wasp, WaspCardContent.Telekinesis).State.Location,
                Is.EqualTo(CardLocation.Hand), "Wasp still holds Telekinesis among his green cards.");
            yield return Begin(root, SnorriGreenFaces.ChargedRunestone, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Hero) ?? false, Is.True,
                "Runestone asks for the enemy hero in radius. " + Remaining(runtime));
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)));
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.CardInstance) ?? false, Is.True, Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates.Count(value =>
                value.Value.Kind == EffectValueKind.CardInstance), Is.GreaterThan(1),
                "Wasp chooses among his green cards.");
            var telekinesis = Card(runtime, wasp, WaspCardContent.Telekinesis).Id;
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                value.Value.Kind == EffectValueKind.CardInstance &&
                value.Value.CardInstanceId.Equals(telekinesis)));
            Assert.That(Card(runtime, wasp, WaspCardContent.Telekinesis).State.Location,
                Is.EqualTo(CardLocation.DiscardPile), "Rune 4 names green.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator RuneMasteryAddsAnActiveRuneThatRunebombThenChooses()
        {
            yield return Stage(SceneCardContent.VerifiedTierThreeEngagedStagedSnorri, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriGreenFaces.InscribedRunebomb,
                SnorriAttackFaces.RunicBattleaxe
            }, waspHand: new[]
            {
                WaspCardContent.MagneticDagger, WaspCardContent.StopProjectiles,
                WaspCardContent.Shock, WaspCardContent.LiftUp, WaspCardContent.Electrocute
            });
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            var wasp = HeroOf(runtime, "wasp");
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Rune Mastery stands face up on Snorri's rail.");
            yield return Inscribe(root, 4);
            yield return CommitEverySeat(root, snorri, NameOf(runtime, SnorriGreenFaces.InscribedRunebomb));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Rune Mastery asks for an inactive rune before the action. " + Remaining(runtime));
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Integer && value.Value.IntegerValue == 4),
                Is.False, "The live slot rune is not inactive.");
            yield return AnswerRuneMastery(root, 3);
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                "Runebomb asks which active rune. " + Remaining(runtime));
            var heroChosen = false;
            if (runtime.PendingEffectChoice.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Hero))
            {
                heroChosen = true;
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)));
            }
            Assert.That(runtime.PendingEffectChoice.Candidates.Where(value =>
                    value.Value.Kind == EffectValueKind.Integer)
                .Select(value => value.Value.IntegerValue), Is.EquivalentTo(new[] { 3, 4 }),
                "Both the slot rune and Mastery's rune are active. " + Remaining(runtime));
            yield return AnswerRuneMastery(root, 3);
            if (runtime.PendingEffectChoice != null && runtime.PendingEffectChoice.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.Hero))
            {
                heroChosen = true;
                yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(wasp)));
            }
            Assert.That(heroChosen, Is.True, "Runebomb asks for the enemy hero in radius.");
            Assert.That(runtime.PendingEffectChoice?.Candidates.Any(value =>
                value.Value.Kind == EffectValueKind.CardInstance) ?? false, Is.True, Remaining(runtime));
            yield return Pick(root, runtime.PendingEffectChoice.Candidates.Single(value =>
                value.Value.Kind == EffectValueKind.CardInstance));
            Assert.That(Card(runtime, wasp, WaspCardContent.MagneticDagger).State.Location,
                Is.Not.EqualTo(CardLocation.Hand), "Rune 3 names gold: Wasp discards Magnetic Dagger.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesSnorriThroughItsCardControl()
        {
            foreach (var definition in SnorriCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri,
                    new[] { definition.Id, SnorriSilverFace.InscribeTheRunes });
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                yield return CommitEverySeat(root, snorri, NameOf(runtime, definition.Id));
                yield return WaitForActor(root, snorri);
                yield return PointTrayAt(root, snorri);
                yield return StartActionThroughControl(root, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value);
                var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.PendingMovementChoice.LegalDestinations
                        .Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    root.Hud, Tile(target).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, snorri), Is.EqualTo(target), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in SnorriCardContent.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.DefenseResolution == PrintedDefenseResolution.Value)))
            {
                var committed = definition.Id.Equals(SnorriSilverFace.InscribeTheRunes)
                    ? SnorriAttackFaces.RunicDagger
                    : SnorriSilverFace.InscribeTheRunes;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri,
                    new[] { definition.Id, committed });
                var root = Root();
                var runtime = root.Runtime;
                var snorri = Snorri(runtime);
                yield return WaspAttacksSnorri(root, committed);
                Assert.That(runtime.PendingDefenseChoice.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)),
                    Is.True, definition.Id.Value);
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, snorri, definition.Id).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RuneSlotPauseRestoresAndRetainedPrefixUndoReplaysExactly()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriAttackFaces.RunicDagger
            });
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            var hud = root.Hud.RootElement;
            yield return CommitEverySeat(root, snorri, NameOf(runtime, SnorriSilverFace.InscribeTheRunes));
            yield return WaitForActor(root, snorri);
            yield return PointTrayAt(root, snorri);
            yield return StartActionThroughControl(root, ActionKind.Skill);
            var slots = PermutationFor(2);
            for (var rune = 1; rune <= 2; rune++)
            {
                UiPlayer.Click(hud.Q<Button>("gd-rune-piece-" + rune));
                yield return null;
                UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slots[rune]));
                yield return null;
            }
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Has.Count.EqualTo(2));
            var pause = runtime.CaptureSnapshot();
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.SelectedCandidates.Count ?? -1, Is.EqualTo(1),
                "One Undo takes back the last placement and keeps the first. " + Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;
            UiPlayer.Click(hud.Q<Button>("gd-rune-piece-2"));
            yield return null;
            UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slots[2]));
            yield return null;
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause),
                "Re-placing the retained prefix reconstructs the identical pause.");
            Assert.That(runtime.RestoreSnapshot(pause), Is.True);
            yield return null;
            Assert.That(runtime.PendingEffectChoice.SelectedCandidates, Has.Count.EqualTo(2));
            for (var rune = 3; rune <= 4; rune++)
            {
                UiPlayer.Click(hud.Q<Button>("gd-rune-piece-" + rune));
                yield return null;
                UiPlayer.Click(hud.Q<Button>("gd-rune-turn-slot-" + slots[rune]));
                yield return null;
            }
            UiPlayer.Click(hud.Q<Button>("gd-rune-slot-done"));
            yield return null;
            var held = runtime.ReadModel.MarkersHeldBy(snorri).ToArray();
            Assert.That(held, Has.Length.EqualTo(4));
            Assert.That(held.Single(marker => marker.Definition.Id.Equals(
                SnorriComponentContent.Runes[1].Id)).TurnSlotIndex, Is.EqualTo(2));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator RunesStayInTheTrayThroughSnorrisDefeatTheRoundBoundaryAndARestart()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri, new[]
            {
                SnorriSilverFace.InscribeTheRunes, SnorriAttackFaces.RunicDagger
            });
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            yield return Inscribe(root, 2);
            yield return WaspAttacksSnorri(root, SnorriAttackFaces.RunicDagger);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(runtime.Projection.Heroes.Any(hero => hero.HeroId.Equals(snorri)), Is.False,
                "Wasp defeats Snorri. " + Remaining(runtime));
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri).Count(), Is.EqualTo(4),
                "The printed exception keeps the runes through Snorri's defeat.");
            var round = runtime.RoundIndex;
            for (var budget = 0; budget < 180 && runtime.RoundIndex == round; budget++)
            {
                if (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
                {
                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                    yield return null;
                }
                else yield return NextRoundDecision(root);
            }
            Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri).Count(), Is.EqualTo(4),
                "The runes survive the round boundary.");
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement).Count(element =>
                element.name == "gd-turn-slot-marker" && UiPlayer.IsShown(element)), Is.EqualTo(4),
                "The rebuilt tray still draws the four runes.");
            CommitSoTheNamedSeatActs(runtime, snorri, SnorriAttackFaces.RunicDagger);
            yield return YieldUntilTheNamedSeatActs(runtime, snorri);
            var respawn = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination).ToArray());
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var camera = pointer.EventCamera;
            var tile = Tile(respawn);
            var depth = camera.WorldToScreenPoint(tile.transform.position).z;
            camera.transform.position += tile.transform.position -
                camera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, depth));
            Assert.That(UiPlayer.ClickBoard(pointer, root.Hud, tile.transform.position), Is.True);
            yield return null;
            Assert.That(SpaceOf(runtime, snorri), Is.EqualTo(respawn));
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud, "RESET"));
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(runtime.ReadModel.MarkersHeldBy(snorri), Is.Empty,
                "Restart reconstructs the staged session, before any rune was inscribed.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedSnorriSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSnorri);
            var root = Root();
            var snorri = Snorri(root.Runtime);
            var view = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Equals(snorri));
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo("snorri"));
            Assert.That(view.HasTokenVisual, Is.True);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(snorri));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EverySnorriFaceKeepsTheStandardCardFootprint()
        {
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSnorri);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Snorri", SnorriCardContent.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator SnorriLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedSnorri,
                new[] { SnorriAttackFaces.RunicDagger, SnorriSilverFace.InscribeTheRunes }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var snorri = Snorri(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(snorri));
                budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind,
                Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(snorri));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId))
                .State.Location, Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return NextRoundDecision(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(snorri));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value,
                Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero =>
                hero.HeroId.Equals(snorri)).ItemBonuses, Is.Not.Empty);
            Healthy(runtime);
        }
    }
}
