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
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;
using T = GuardsDigital.Core.Cards.Verified.TakahideCardContent;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 9.32i. Real pointer and control journeys for Takahide the Warlord, on boards measured
    /// headlessly before any was written (scenes 144, 145 and 146, driven with DirectorCommands):
    /// Takahide stands on (-3, 1); Wasp beside him on (-2, 1) for his adjacent Attacks, at exactly three
    /// spaces on (0, 1) for Sting like a Bee's "maximum range", or two away on (-1, 1) for the Blue
    /// radius; and, on the ally board, Arien beside him on (-3, 0) holding Aspiring Duelist (9), Liquid
    /// Leap (4) and Noble Blade (11), with Spell Break and Dangerous Current in his discard. Every other
    /// seat commits its lowest card, so Arien's Liquid Leap is still unresolved when a Red or Blue face
    /// of Takahide's acts. His own blue melee minion stands on (-3, 2) and Red's ranged minion on (-4, 2),
    /// both beside him.
    ///
    /// <para>Every journey reads what its answer changed: where a card lies and whether it lies
    /// facedown, a hand, a discard, coins, the board, the barred spaces his blades draw, the window a
    /// teammate answers on their own seat.</para>
    /// </summary>
    public sealed class TakahideUiJourneyPlayModeTests
    {
        internal static readonly HexCoordinate TakahideSpace = new HexCoordinate(-3, 1);
        internal static readonly HexCoordinate WaspSpace = new HexCoordinate(-2, 2);
        internal static readonly HexCoordinate WaspBesideHim = new HexCoordinate(-2, 1);
        internal static readonly HexCoordinate WaspAtRangeThree = new HexCoordinate(0, 1);
        internal static readonly HexCoordinate WaspTwoAway = new HexCoordinate(-1, 1);
        internal static readonly HexCoordinate ArienBesideHim = new HexCoordinate(-3, 0);

        internal static readonly CardDefinitionId[] ArienHand =
        {
            ArienCardContent.AspiringDuelist, ArienCardContent.LiquidLeap, ArienCardContent.NobleBlade
        };

        // -------------------------------------------------------------------------------------------
        // Reading the board and the cards
        // -------------------------------------------------------------------------------------------

        internal static HeroId Takahide(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == T.ContentSetId).HeroId;

        internal static HeroId Wasp(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == WaspCardContent.ContentSetId).HeroId;

        internal static HeroId Arien(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "arien").HeroId;

        internal static PrimitiveHeroPointerInput Pointer() => Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

        internal static bool OnBoard(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Any(value => value.HeroId.Equals(hero));

        internal static HexCoordinate SpaceOf(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Heroes.Single(value => value.HeroId.Equals(hero)).Coordinate;

        internal static CardInstance Card(ConfiguredMatchSceneRuntime runtime, HeroId owner, CardDefinitionId face) =>
            runtime.Cards.Single(card => card.OwnerId.Equals(owner) &&
                card.DefinitionReference.DefinitionId.Equals(face));

        internal static int HandCount(ConfiguredMatchSceneRuntime runtime, HeroId owner) =>
            runtime.Cards.Count(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);

        internal static int Coins(ConfiguredMatchSceneRuntime runtime, HeroId hero) =>
            runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(hero)).Coins;

        /// <summary>Every hero stands drawn where the projection puts it, and nothing faulted.</summary>
        internal static void Healthy(ConfiguredMatchSceneRuntime runtime)
        {
            Assert.That(runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText),
                Has.None.Contains("EngineFault"));
            foreach (var row in runtime.Heroes)
            {
                Assert.That(HeroView(row.HeroId).Coordinate, Is.EqualTo(row.Coordinate), row.ToString());
            }
        }

        // -------------------------------------------------------------------------------------------
        // Staging
        // -------------------------------------------------------------------------------------------

        private static readonly Dictionary<SceneCardContent, (HeroId Takahide, HeroId Wasp, HeroId? Arien,
            (MinionId Id, HexCoordinate At)[] Minions)> Cached =
            new Dictionary<SceneCardContent, (HeroId, HeroId, HeroId?, (MinionId, HexCoordinate)[])>();

        /// <summary>
        /// Loads <paramref name="scene"/> staged: Takahide on (-3, 1) holding <paramref name="hand"/> (the
        /// rest of his p.6 hand in his discard), Wasp where asked holding her own hand, and on the ally
        /// board Arien beside him holding <see cref="ArienHand"/>. A minion standing where a staged piece
        /// goes is cleared. The scene's ids and minion layout are read once per scene.
        /// </summary>
        internal static IEnumerator Stage(SceneCardContent scene, CardDefinitionId[] hand,
            HexCoordinate? wasp = null, int? coins = null)
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            if (!Cached.ContainsKey(scene))
            {
                yield return LoadStagedScene(scene);
                var plain = Root().Runtime;
                Cached[scene] = (Takahide(plain), Wasp(plain),
                    plain.Setup.Heroes.Any(hero => hero.ContentId.Value == "arien") ? Arien(plain) : (HeroId?)null,
                    plain.ReadModel.Minions.Select(value => (value.MinionId, value.Coordinate)).ToArray());
            }

            var cached = Cached[scene];
            var waspAt = wasp ?? WaspSpace;
            var heroes = new List<SandboxHeroStaging>
            {
                new SandboxHeroStaging(cached.Takahide, TakahideSpace, coins: coins, hand: hand),
                new SandboxHeroStaging(cached.Wasp, waspAt)
            };
            var occupied = new List<HexCoordinate> { TakahideSpace, waspAt };
            if (cached.Arien.HasValue)
            {
                heroes.Add(new SandboxHeroStaging(cached.Arien.Value, ArienBesideHim, hand: ArienHand));
                occupied.Add(ArienBesideHim);
            }

            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(
                heroes: heroes,
                removedMinions: cached.Minions.Where(value => occupied.Contains(value.At)).Select(value => value.Id));
            yield return LoadStagedScene(scene);
            Assert.That(SpaceOf(Root().Runtime, cached.Takahide), Is.EqualTo(TakahideSpace));
        }

        // -------------------------------------------------------------------------------------------
        // Steps
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Commits <paramref name="face"/> from his tray and every other seat's lowest card through
        /// theirs, waits for his turn and starts the face's action of <paramref name="kind"/> from its
        /// control.
        /// </summary>
        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId face, ActionKind kind)
        {
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            yield return CommitEverySeat(root, takahide, NameOf(runtime, face));
            yield return GarrusUiJourneyTests.WaitForActor(root, takahide);
            yield return PointTrayAt(root, takahide);
            yield return StartActionThroughControl(root, kind);
            yield return null;
        }

        /// <summary>
        /// Answers one candidate of the standing effect choice where a player does - the piece, the
        /// space, or a card face in the window - confirming it when it still stands, and proves the
        /// consumed input is stale.
        /// </summary>
        internal static IEnumerator Pick(MatchSceneCompositionRoot root, Func<EffectValue, bool> which, string why)
        {
            var runtime = root.Runtime;
            var choice = runtime.PendingEffectChoice;
            Assert.That(choice, Is.Not.Null, why + " " + Remaining(runtime));
            var stale = choice.SelectInvocations.FirstOrDefault(value => which(value.Candidate.Value));
            Assert.That(stale, Is.Not.Null, why + " " + Remaining(runtime));
            var node = choice.NodeId;
            var value = stale.Candidate.Value;
            if (value.Kind == EffectValueKind.CardInstance)
            {
                var card = runtime.RoundView.Model.EffectChoices.Single(row =>
                    row.Command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    row.Command.EffectChoice.Candidate.Equals(stale.Candidate));
                yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                var control = UiPlayer.DecisionControl(runtime, command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    ReferenceEquals(command.EffectChoice, stale));
                Assert.That(control, Is.Not.Null, why + " " + Remaining(runtime));
                Assert.That(control.Card?.DisplayName, Is.EqualTo(card.Card.DisplayName));
                UiPlayer.EnsureVisible(runtime.DecisionWindowView.ElementFor(control));
                yield return null;
                UiPlayer.ClickDecisionControl(runtime, root.Hud, control);
                yield return null;
            }
            else
            {
                var at = value.Kind == EffectValueKind.Hero ? HeroView(value.HeroId).transform.position
                    : value.Kind == EffectValueKind.Minion ? MinionView(value.MinionId).transform.position
                    : value.Kind == EffectValueKind.Coordinate ? Tile(value.Coordinate).transform.position
                    : throw new InvalidOperationException("No board answer for " + value.Kind);
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True,
                    why + " " + UiPlayer.DescribeBoardRay(Pointer(), at));
                yield return null;
            }

            yield return ConfirmIfStillStanding(root, node);
            var after = runtime.CaptureSnapshot();
            Assert.That(runtime.SubmitProjectedCommand(DirectorCommand.AnswerEffectChoice(stale)), Is.False,
                "The consumed input is stale.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(after), "Stale input is atomic.");
        }

        /// <summary>Picks the card face of <paramref name="face"/> out of the standing card question.</summary>
        internal static IEnumerator PickCard(MatchSceneCompositionRoot root, CardDefinitionId face, string why)
        {
            var runtime = root.Runtime;
            yield return Pick(root, value => value.Kind == EffectValueKind.CardInstance &&
                runtime.Cards.Single(card => card.Id.Equals(value.CardInstanceId))
                    .DefinitionReference.DefinitionId.Equals(face), why);
        }

        /// <summary>
        /// The first of <paramref name="spaces"/> a player can click: on screen and not under the HUD,
        /// preferring one no hero piece is drawn in front of.
        /// </summary>
        internal static HexCoordinate ReachableSpace(MatchSceneCompositionRoot root, IEnumerable<HexCoordinate> spaces)
        {
            var camera = Pointer().EventCamera;
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>()
                .Select(view => view.GetComponent<CapsuleCollider>())
                .Where(capsule => capsule != null)
                .ToArray();
            var onScreen = new List<HexCoordinate>();
            foreach (var space in spaces)
            {
                var screen = camera.WorldToScreenPoint(Tile(space).transform.position);
                if (screen.z <= 0f || screen.x < 4f || screen.y < 4f ||
                    screen.x > Screen.width - 4f || screen.y > Screen.height - 4f ||
                    root.Hud.BlocksPointerAt(new Vector2(screen.x, screen.y)))
                {
                    continue;
                }

                onScreen.Add(space);
                var ray = camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
                if (!heroes.Any(capsule => capsule.Raycast(ray, out _, 500f)))
                {
                    return space;
                }
            }

            Assert.That(onScreen, Is.Not.Empty, "No offered space can be clicked: " + string.Join(", ", spaces));
            return onScreen[0];
        }

        /// <summary>Picks a space the standing question offers, other than <paramref name="except"/>.</summary>
        internal static IEnumerator PickSpace(MatchSceneCompositionRoot root, HexCoordinate except, string why)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingEffectChoice, Is.Not.Null, why + " " + Remaining(runtime));
            var space = ReachableSpace(root, runtime.PendingEffectChoice.Candidates
                .Where(value => value.Value.Kind == EffectValueKind.Coordinate && !value.Value.Coordinate.Equals(except))
                .Select(value => value.Value.Coordinate));
            yield return Pick(root, value => value.Kind == EffectValueKind.Coordinate && value.Coordinate.Equals(space), why);
        }

        /// <summary>Clicks a hero's piece to name it as the standing Attack's target.</summary>
        internal static IEnumerator TargetHero(MatchSceneCompositionRoot root, HeroId hero)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.PendingAttackTargetChoice?.LegalTargetHeroIds, Has.Member(hero), Remaining(runtime));
            var at = HeroView(hero).transform.position;
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, at), Is.True, UiPlayer.DescribeBoardRay(Pointer(), at));
            yield return null;
        }

        /// <summary>His Attack on Wasp, through her Defense: she takes the hit.</summary>
        internal static IEnumerator HitWasp(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            yield return TargetHero(root, Wasp(runtime));
            Assert.That(runtime.PendingDefenseChoice, Is.Not.Null, Remaining(runtime));
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Commits both seats' named cards headlessly - the commitment is setup here, its tray journey is
        /// every face's own - so the enemy's card decides who acts first.
        /// </summary>
        internal static void CommitBoth(ConfiguredMatchSceneRuntime runtime, CardDefinitionId his, CardDefinitionId hers)
        {
            UiPlayer.EndAnyTurnInProgress(runtime);
            Commit(runtime, Takahide(runtime), his);
            Commit(runtime, Wasp(runtime), hers);
        }

        /// <summary>Wasp, acting first, starts her Attack through its control.</summary>
        internal static IEnumerator WaspStartsHerAttack(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(Wasp(runtime)), Remaining(runtime));
            yield return PointTrayAt(root, Wasp(runtime));
            yield return StartActionThroughControl(root, ActionKind.Attack);
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null, Remaining(runtime));
        }

        /// <summary>Ends the open movement on the round's own control, as a player presses it.</summary>
        internal static IEnumerator StopMoving(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var stop = runtime.RoundView.Model.EndTurnInvocation;
            Assert.That(stop?.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement), Remaining(runtime));
            UiPlayer.Click(UiPlayer.Button(root.Hud, stop.Label));
            yield return null;
            yield return null;
        }

        /// <summary>Hands every turn on until the next commitment opens, on the controls a player presses.</summary>
        internal static IEnumerator UntilCommitments(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            for (var budget = 0; budget < 24 && runtime.Phase != RoundResolutionPhase.AwaitingCommitments; budget++)
            {
                var initiative = UiPlayer.DecisionControl(runtime, command =>
                    command.Kind == DirectorCommandKind.ChooseTiedActor);
                if (initiative != null) UiPlayer.ClickDecisionControl(runtime, root.Hud, initiative);
                else
                {
                    Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null, Remaining(runtime));
                    UiPlayer.Click(UiPlayer.Button(root.Hud, runtime.RoundView.Model.EndTurnInvocation.Label));
                }

                yield return null;
            }

            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.AwaitingCommitments), Remaining(runtime));
        }

        /// <summary>The window asks his own deck-gold question, in mockup C's words.</summary>
        internal static void AssertTheGoldSwapWindow(ConfiguredMatchSceneRuntime runtime, string why)
        {
            var window = runtime.DecisionWindowView.Model;
            Assert.That(window, Is.Not.Null, why + " " + Remaining(runtime));
            Assert.That(window.Eyebrow, Is.EqualTo("GOLD SWAP"), why);
            Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(Takahide(runtime)),
                why + ": he chooses the deck gold himself.");
        }

        // -------------------------------------------------------------------------------------------
        // Gold and silver
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EachGoldAttackSwapsItselfForTheDeckGoldHePicksAfterItsAttack()
        {
            foreach (var (face, wasp, swappedIn, other) in new[]
            {
                (T.StingLikeABee, WaspAtRangeThree, T.StrikeLikeATiger, T.FloatLikeAButterfly),
                (T.StrikeLikeATiger, WaspBesideHim, T.StingLikeABee, T.Bushido)
            })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide, new[] { face, other }, wasp: wasp);
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                var waspId = Wasp(runtime);
                yield return Begin(root, face, ActionKind.Attack);
                if (face.Equals(T.StingLikeABee))
                {
                    var targets = runtime.PendingAttackTargetChoice;
                    Assert.That(targets?.LegalTargetHeroIds, Has.Member(waspId), Remaining(runtime));
                    foreach (var minion in targets.LegalTargetMinionIds)
                    {
                        Assert.That(HexDistanceQuery.Calculate(TakahideSpace, runtime.ReadModel.Minions
                            .Single(value => value.MinionId.Equals(minion)).Coordinate), Is.EqualTo(3),
                            "Sting like a Bee targets a unit at maximum range only.");
                    }
                }

                yield return HitWasp(root);
                Assert.That(OnBoard(runtime, waspId), Is.False, face.Value + " defeats Wasp. " + Remaining(runtime));
                yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                AssertTheGoldSwapWindow(runtime, face.Value);
                yield return PickCard(root, swappedIn, face.Value + " offers the gold in his deck.");
                Assert.That(Card(runtime, takahide, face).State.Location, Is.EqualTo(CardLocation.Deck), face.Value + " goes into his deck.");
                Assert.That(Card(runtime, takahide, swappedIn).State.Location, Is.EqualTo(CardLocation.TurnArea),
                    swappedIn.Value + " takes its place in his turn slot.");
                Assert.That(Card(runtime, takahide, swappedIn).State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceUp));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator FloatLikeAButterflyMovesThenSwapsForTheDeckGoldHePicks()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.FloatLikeAButterfly, T.Bushido }, wasp: WaspAtRangeThree);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            yield return Begin(root, T.FloatLikeAButterfly, ActionKind.Movement);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, Remaining(runtime));
            var step = ReachableSpace(root, runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 2).Select(value => value.Destination));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(step).transform.position), Is.True);
            yield return null;
            Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(step), "He walks two of his five spaces.");
            yield return StopMoving(root);
            AssertTheGoldSwapWindow(runtime, "Float like a Butterfly, after its movement (TK-Q1)");
            yield return PickCard(root, T.StingLikeABee, "The deck holds Sting like a Bee and Strike like a Tiger.");
            Assert.That(Card(runtime, takahide, T.FloatLikeAButterfly).State.Location, Is.EqualTo(CardLocation.Deck));
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(Card(runtime, takahide, T.StrikeLikeATiger).State.Location, Is.EqualTo(CardLocation.Deck));
            Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(step), "The swap moves no piece.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator BushidoSwapsHisResolvedGoldAndTheNewCardLiesFacedown()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.FloatLikeAButterfly, T.Bushido }, wasp: WaspAtRangeThree);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);

            // Turn 1: Float like a Butterfly walks nowhere and swaps for Sting like a Bee, which rests
            // resolved in his first slot.
            yield return Begin(root, T.FloatLikeAButterfly, ActionKind.Movement);
            yield return StopMoving(root);
            yield return PickCard(root, T.StingLikeABee, "Float like a Butterfly's swap.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.TurnArea));
            yield return UntilCommitments(root);

            // Turn 2: Bushido swaps that resolved gold, and the new card is resolved facedown.
            yield return Begin(root, T.Bushido, ActionKind.Skill);
            AssertTheGoldSwapWindow(runtime, "Bushido");
            Assert.That(runtime.PendingEffectChoice.Candidates.Select(value => runtime.Cards
                    .Single(card => card.Id.Equals(value.Value.CardInstanceId)).DefinitionReference.DefinitionId),
                Is.EquivalentTo(new[] { T.FloatLikeAButterfly, T.StrikeLikeATiger }), "His deck's two golds.");
            yield return PickCard(root, T.StrikeLikeATiger, "Bushido's swap.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.Deck),
                "The resolved gold goes into his deck.");
            var arrived = Card(runtime, takahide, T.StrikeLikeATiger);
            Assert.That(arrived.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(arrived.State.FaceOrientation, Is.EqualTo(CardFaceOrientation.FaceDown),
                "A resolved gold swapped by Bushido arrives facedown.");
            yield return null;
            Assert.That(UiPlayer.Descendants(root.Hud.RootElement)
                    .Any(element => element.name == "gd-facedown-hatch" && UiPlayer.IsShown(element.parent)), Is.True,
                "His rail draws the facedown gold hatched.");

            var nonOwner = runtime.Host.Seats.Seats.Single(seat => seat.Owns(Wasp(runtime)));
            var safe = HeroCardInspection.ForSeat(
                    ((MatchHost)runtime.Host).Projection,
                    nonOwner,
                    new ExperimentalMatchOptions(enemyCardInformation: true))
                .Single(row => row.Hero.Equals(takahide));
            Assert.That(safe.OtherGoldCards, Is.Empty,
                "Known Gold faces stay withheld together so the facedown arrival cannot be identified by elimination.");
            Assert.That(safe.ConcealedOtherGoldCount, Is.EqualTo(2),
                "Only the two basic deck Golds occupy the dedicated anonymous group.");
            Assert.That(safe.ConcealedOtherCount, Is.EqualTo(1),
                "The resolved facedown Gold remains anonymous in Unavailable, not in the deck group.");

            var oldWidth = (uint)Screen.width;
            var oldHeight = (uint)Screen.height;
            UiPlayer.SetPlayerViewport(1920, 1080, "Takahide facedown Gold inspection");
            root.Hud.Layout();
            yield return null;
            yield return null;
            var token = HeroView(takahide);
            var overview = runtime.HeroOverviewsView.Model.Heroes.Single(row => row.HeroId.Equals(takahide));
            var definitions = runtime.ReadModel.Definitions.ToDictionary(value => value.Reference);
            var knownHandPips = safe.HandCards
                .Where(definitions.ContainsKey)
                .Select(reference => new HeroCardPip(
                    definitions[reference].Progression.Color, reference))
                .OrderBy(pip => pip.Color)
                .ToArray();
            token.SetCardStateIndicator(
                overview.CurrentCard != null,
                new HeroCardPip(
                    overview.CurrentCard?.Color ?? CardColor.None,
                    overview.CurrentCard?.Card?.DefinitionReference ?? default),
                knownHandPips,
                safe.ConcealedHandCount);
            var expectedPips = safe.HandCards.Count + safe.ConcealedHandCount + (overview.CurrentCard == null ? 0 : 1);
            Assert.That(token.CardStateIndicator?.PipCount ?? 0, Is.EqualTo(expectedPips),
                "The facedown resting Gold is not a playable hand/current-card pip on his token.");
            Assert.That(expectedPips, Is.Zero,
                "At this completed Bushido moment the resolved facedown Gold must leave the token pip-free.");
            yield return BoardPieceCapturePlayModeTests.Capture(
                runtime.BoardView,
                "tmp/9-77-takahide-token-pips-facedown-1920",
                new[] { token.Coordinate });
            var template = runtime.HeroOverviewsView.CardInspection.Root;
            var inspection = new HeroCardInspectionUnityView(() => runtime.ReadModel);
            inspection.Root.style.left = template.resolvedStyle.left;
            inspection.Root.style.top = template.resolvedStyle.top;
            inspection.Root.style.width = template.resolvedStyle.width;
            inspection.Root.style.height = template.resolvedStyle.height;
            root.Hud.RootElement.Add(inspection.Root);
            Assert.That(inspection.Open(safe, "Takahide the Warlord"), Is.True);
            yield return null;
            yield return null;
            var handSection = inspection.Root.Q("gd-hero-card-hand");
            var unavailableSection = inspection.Root.Q("gd-hero-card-other");
            var goldSection = inspection.Root.Q("gd-hero-card-other-golds");
            Assert.That(safe.OtherCards, Is.Not.Empty,
                "Bushido is a visible unavailable card in the completed swap state.");
            Assert.That(unavailableSection.Query<VisualElement>("gd-inspected-card").ToList(), Is.Not.Empty);
            Assert.That(unavailableSection.worldBound.xMin, Is.GreaterThan(handSection.worldBound.xMin),
                "Unavailable cards occupy the upper-right area.");
            Assert.That(goldSection.worldBound.yMin, Is.GreaterThan(handSection.worldBound.yMin + 1f),
                "Other Golds occupy their own box below the available hand.");
            Assert.That(goldSection.Query<VisualElement>("gd-inspected-card-back").ToList(), Has.Count.EqualTo(2));
            Assert.That(goldSection.Query<VisualElement>("gd-inspected-card").ToList(), Is.Empty);
            yield return UiPlayer.CaptureScreen("tmp/9-77-takahide-facedown-gold-1920.png");
            inspection.Root.RemoveFromHierarchy();
            UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "restore after Takahide facedown Gold inspection");
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Red: the teammate's swap and the blades
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HisRedSwapsLetArienSwapOrKeepHisUnresolvedCardOnHisOwnSeat()
        {
            foreach (var (face, answer) in new[]
            {
                (T.SetAnExample, (CardDefinitionId?)ArienCardContent.AspiringDuelist),
                (T.LeadFromTheFront, (CardDefinitionId?)null),
                (T.HoldMySake, (CardDefinitionId?)ArienCardContent.DangerousCurrent)
            })
            {
                yield return Stage(SceneCardContent.VerifiedStagedTakahideWithAlly,
                    new[] { face, T.ProvenWarrior }, wasp: WaspBesideHim);
                var root = Root();
                var runtime = root.Runtime;
                var arien = Arien(runtime);
                yield return Begin(root, face, ActionKind.Attack);
                Assert.That(Card(runtime, arien, ArienCardContent.LiquidLeap).State.Location, Is.EqualTo(CardLocation.TurnArea),
                    "Arien's Liquid Leap waits unresolved behind him.");
                yield return HitWasp(root);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    face.Value + ": he names the friendly hero in radius on the board.");

                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(arien),
                    face.Value + ": the swap is asked on Arien's own seat. " + Remaining(runtime));
                yield return UiPlayer.WaitForDecisionWindow(runtime, root.Hud);
                Assert.That(runtime.DecisionWindowView.Model?.Title,
                    Does.StartWith("SWAP YOUR UNRESOLVED CARD?").And.EndWith("ARIEN"),
                    face.Value + ": the window names the seat it asks.");
                Assert.That(runtime.DecisionWindowView.Model.CornerControl?.Label, Is.EqualTo("KEEP MY CARD"),
                    face.Value + ": one window, its decline his own.");
                if (answer.HasValue)
                {
                    yield return PickCard(root, answer.Value, face.Value + " offers Arien's card.");
                    Assert.That(Card(runtime, arien, answer.Value).State.Location, Is.EqualTo(CardLocation.TurnArea),
                        face.Value + ": the picked card is now his unresolved card.");
                    Assert.That(Card(runtime, arien, ArienCardContent.LiquidLeap).State.Location,
                        Is.EqualTo(face.Equals(T.HoldMySake) ? CardLocation.DiscardPile : CardLocation.Hand),
                        face.Value + ": Liquid Leap takes the picked card's place.");
                }
                else
                {
                    yield return GarrusUiJourneyTests.May(root, false);
                    Assert.That(Card(runtime, arien, ArienCardContent.LiquidLeap).State.Location, Is.EqualTo(CardLocation.TurnArea),
                        face.Value + ": KEEP MY CARD leaves Liquid Leap where it was.");
                    Assert.That(Card(runtime, arien, ArienCardContent.AspiringDuelist).State.Location, Is.EqualTo(CardLocation.Hand));
                }

                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HisBladesBarTheEmptySpacesAroundHimForTheEnemy()
        {
            foreach (var face in new[] { T.SpinningBlade, T.BladeHelix })
            {
                yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                    new[] { face, T.ProvenWarrior }, wasp: WaspBesideHim);
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                yield return Begin(root, face, ActionKind.Attack);
                Assert.That(Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                    .Any(tile => tile.HasEnemyObstacleCue), Is.False, "Nothing is barred before the attack.");
                yield return HitWasp(root);
                yield return null;
                Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Has.Member(Card(runtime, takahide, face).Id),
                    face.Value + "'s This turn effect stands. " + Remaining(runtime));
                var occupied = runtime.Heroes.Select(hero => hero.Coordinate)
                    .Concat(runtime.ReadModel.Minions.Select(minion => minion.Coordinate)).ToArray();
                var cued = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                    .Where(tile => tile.HasEnemyObstacleCue).Select(tile => tile.Coordinate).ToArray();
                Assert.That(cued, Is.Not.Empty, face.Value + " marks the empty spaces around him.");
                Assert.That(cued, Has.Member(WaspBesideHim), face.Value + ": the space Wasp fell from is empty now.");
                foreach (var space in cued)
                {
                    Assert.That(HexDistanceQuery.Calculate(TakahideSpace, space), Is.EqualTo(1),
                        face.Value + " bars only the spaces beside him: " + space);
                    Assert.That(occupied, Has.No.Member(space), face.Value + " bars only empty spaces: " + space);
                }

                Assert.That(cued, Has.No.Member(new HexCoordinate(-3, 2)),
                    "His own minion's space is no empty space.");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Blue: the colour chosen from a teammate's discard, and the teammate's move
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HisBlueChoiceMakesWaspDiscardTheColourHeChoseFromAriensDiscard()
        {
            foreach (var face in new[] { T.ProvenWarrior, T.ChosenChampion, T.TheRightHand })
            {
                yield return Stage(SceneCardContent.VerifiedStagedTakahideWithAlly,
                    new[] { face, T.SetAnExample }, wasp: WaspTwoAway);
                var root = Root();
                var runtime = root.Runtime;
                var arien = Arien(runtime);
                var wasp = Wasp(runtime);
                yield return Begin(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    face.Value + " names the friendly hero in radius.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(Takahide(runtime)),
                    face.Value + ": he chooses the card in Arien's discard. " + Remaining(runtime));
                yield return PickCard(root, ArienCardContent.DangerousCurrent, face.Value + " offers Arien's discard.");
                Assert.That(Card(runtime, arien, ArienCardContent.DangerousCurrent).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                    "Choosing a card leaves it in the discard.");
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(wasp),
                    face.Value + " names the enemy hero in radius.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(wasp),
                    face.Value + ": Wasp chooses her own discard. " + Remaining(runtime));
                Assert.That(runtime.PendingEffectChoice.Candidates.Select(value => runtime.Cards
                        .Single(card => card.Id.Equals(value.Value.CardInstanceId)).DefinitionReference.DefinitionId),
                    Is.EquivalentTo(new[] { WaspCardContent.Shock }), "Only her red card matches Dangerous Current.");
                yield return PickCard(root, WaspCardContent.Shock, face.Value + ": Wasp's red card.");
                Assert.That(Card(runtime, wasp, WaspCardContent.Shock).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                    face.Value + ": Wasp discards a card of that colour.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator CalculatedRiskLetsArienDiscardAnAttackAndMove()
        {
            foreach (var (face, discards) in new[] { (T.CalculatedRisk, true), (T.TacticalGambit, false) })
            {
                yield return Stage(SceneCardContent.VerifiedStagedTakahideWithAlly,
                    new[] { face, T.SetAnExample }, wasp: WaspAtRangeThree);
                var root = Root();
                var runtime = root.Runtime;
                var arien = Arien(runtime);
                yield return Begin(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    face.Value + " names the friendly hero in radius.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(arien),
                    face.Value + ": Arien chooses whether to discard. " + Remaining(runtime));
                if (discards)
                {
                    yield return PickCard(root, ArienCardContent.NobleBlade, face.Value + ": his one attack card in hand.");
                    Assert.That(Card(runtime, arien, ArienCardContent.NobleBlade).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                }
                else
                {
                    yield return GarrusUiJourneyTests.May(root, false);
                    Assert.That(Card(runtime, arien, ArienCardContent.NobleBlade).State.Location, Is.EqualTo(CardLocation.Hand),
                        face.Value + ": a declined discard keeps the card.");
                }

                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(arien),
                    face.Value + ": he has a card in his discard, so Arien may move. " + Remaining(runtime));
                yield return PickSpace(root, ArienBesideHim, face.Value + "'s move for Arien.");
                Assert.That(SpaceOf(runtime, arien), Is.Not.EqualTo(ArienBesideHim), face.Value + ": Arien moves.");
                Assert.That(HexDistanceQuery.Calculate(ArienBesideHim, SpaceOf(runtime, arien)), Is.LessThanOrEqualTo(2),
                    face.Value + ": up to two spaces.");
                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // Green: the teammate's discard, then his move or the coins
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator HisGreenAidMovesHimOnceArienHasACardInHisDiscard()
        {
            foreach (var (face, reach, discards) in new[]
            {
                (T.ComeToAid, 3, true), (T.BringTheRelief, 4, false), (T.CommitReserves, 4, true)
            })
            {
                yield return Stage(SceneCardContent.VerifiedStagedTakahideWithAlly,
                    new[] { face, T.SetAnExample }, wasp: WaspAtRangeThree);
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                var arien = Arien(runtime);
                yield return Begin(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    face.Value + " names the friendly hero in range.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(arien),
                    face.Value + ": Arien chooses whether to discard. " + Remaining(runtime));
                if (discards)
                {
                    yield return PickCard(root, ArienCardContent.AspiringDuelist, face.Value + ": a card from Arien's hand.");
                    Assert.That(Card(runtime, arien, ArienCardContent.AspiringDuelist).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                }
                else
                {
                    yield return GarrusUiJourneyTests.May(root, false);
                    Assert.That(Card(runtime, arien, ArienCardContent.AspiringDuelist).State.Location, Is.EqualTo(CardLocation.Hand));
                }

                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(takahide),
                    face.Value + ": Arien has a card in his discard, so he may move. " + Remaining(runtime));
                yield return PickSpace(root, TakahideSpace, face.Value + "'s move.");
                Assert.That(SpaceOf(runtime, takahide), Is.Not.EqualTo(TakahideSpace), face.Value + ": he moves.");
                Assert.That(HexDistanceQuery.Calculate(TakahideSpace, SpaceOf(runtime, takahide)), Is.LessThanOrEqualTo(reach),
                    face.Value + ": up to " + reach + " spaces.");
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HisPledgesPayThemBothCoinsAndHeMayRetrieveACard()
        {
            foreach (var (face, coins, accepts) in new[] { (T.PledgeOfAllegiance, 1, true), (T.LoyalRetainer, 2, false) })
            {
                yield return Stage(SceneCardContent.VerifiedStagedTakahideWithAlly,
                    new[] { face, T.SetAnExample }, wasp: WaspAtRangeThree, coins: 0);
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                var arien = Arien(runtime);
                var arienCoins = Coins(runtime, arien);
                yield return Begin(root, face, ActionKind.Skill);
                yield return Pick(root, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(arien),
                    face.Value + " names the friendly hero in range.");
                if (accepts)
                {
                    yield return PickCard(root, ArienCardContent.AspiringDuelist, face.Value + ": Arien discards.");
                    Assert.That(Card(runtime, arien, ArienCardContent.AspiringDuelist).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                }
                else
                {
                    yield return GarrusUiJourneyTests.May(root, false);
                }

                Assert.That(Coins(runtime, takahide), Is.EqualTo(coins), face.Value + " pays him.");
                Assert.That(Coins(runtime, arien), Is.EqualTo(arienCoins + coins), face.Value + " pays Arien the same.");
                Assert.That(runtime.PendingEffectChoice?.ChooserId ?? default, Is.EqualTo(takahide),
                    face.Value + ": he may retrieve a discarded card. " + Remaining(runtime));
                if (accepts)
                {
                    yield return PickCard(root, T.Bushido, face.Value + ": Bushido from his discard.");
                    Assert.That(Card(runtime, takahide, T.Bushido).State.Location, Is.EqualTo(CardLocation.Hand), "Bushido is back in his hand.");
                }
                else
                {
                    yield return GarrusUiJourneyTests.May(root, false);
                    Assert.That(Card(runtime, takahide, T.Bushido).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                }

                Healthy(runtime);
            }
        }

        // -------------------------------------------------------------------------------------------
        // The Ultimate
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ReadyForWarStandsFaceupAndHisSixCardHandPlays()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedTierThreeEngagedStagedTakahide);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            Assert.That(UiPlayer.ItemStripDescendants(root.Hud).Any(element =>
                element.name == "gd-ultimate-card" && UiPlayer.IsShown(element)), Is.True,
                "Ready for War stands faceup on his rail.");
            Assert.That(HandCount(runtime, takahide), Is.EqualTo(6), "Ready for War leaves him six cards (TK-Q5).");
            Assert.That(Card(runtime, takahide, T.Bushido).State.Location, Is.EqualTo(CardLocation.Deck), "His silver card is in his deck.");
            foreach (var gold in new[] { T.StingLikeABee, T.FloatLikeAButterfly, T.StrikeLikeATiger })
            {
                Assert.That(Card(runtime, takahide, gold).State.Location, Is.EqualTo(CardLocation.Hand), gold.Value + " is in his hand.");
            }

            yield return CommitEverySeat(root, takahide, NameOf(runtime, T.StrikeLikeATiger));
            yield return GarrusUiJourneyTests.WaitForActor(root, takahide);
            Assert.That(Card(runtime, takahide, T.StrikeLikeATiger).State.Location, Is.EqualTo(CardLocation.TurnArea),
                "A gold from the six-card hand is committed and revealed.");
            Assert.That(HandCount(runtime, takahide), Is.EqualTo(5));
            Healthy(runtime);
        }

        // -------------------------------------------------------------------------------------------
        // Every face's ribbon
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovementMovesHimThroughItsCardControl()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement &&
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary)))
            {
                var other = definition.Id.Equals(T.ProvenWarrior) ? T.SetAnExample : T.ProvenWarrior;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide, new[] { definition.Id, other });
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                yield return Begin(root, definition.Id, ActionKind.Movement);
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null, definition.Id.Value + " " + Remaining(runtime));
                var step = ReachableSpace(root, runtime.PendingMovementChoice.LegalDestinations
                    .Where(value => value.Cost == 1).Select(value => value.Destination));
                Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(step).transform.position), Is.True);
                yield return null;
                Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(step), definition.Id.Value);
                Assert.That(HeroView(takahide).Coordinate, Is.EqualTo(step), definition.Id.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryDefenseAnswersAnEnemyAttackFromItsOwnFace()
        {
            foreach (var definition in T.DealtDefinitions.Where(card =>
                card.ActionDefinitions.Any(action => action.Kind == ActionKind.Defense &&
                    action.Origin == ActionDefinitionOrigin.Reaction)))
            {
                // A committed card below Magnetic Dagger's 12, so Wasp acts first.
                var committed = definition.Id.Equals(T.ComeToAid) ? T.PledgeOfAllegiance : T.ComeToAid;
                yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                    new[] { definition.Id, committed }, wasp: WaspBesideHim);
                var root = Root();
                var runtime = root.Runtime;
                var takahide = Takahide(runtime);
                CommitBoth(runtime, committed, WaspCardContent.MagneticDagger);
                yield return null;
                yield return WaspStartsHerAttack(root);
                yield return TargetHero(root, takahide);
                Assert.That(runtime.PendingDefenseChoice?.LegalDefenseOffers.Any(offer =>
                    offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True,
                    definition.Id.Value + " " + Remaining(runtime));
                yield return ClickDefenseFace(root, definition.Id);
                Assert.That(runtime.PendingDefenseChoice, Is.Null, definition.Id.Value);
                Assert.That(Card(runtime, takahide, definition.Id).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                    definition.Id.Value + " is spent from his hand to defend.");
            }
        }

        // -------------------------------------------------------------------------------------------
        // Session continuity, lifecycle and components
        // -------------------------------------------------------------------------------------------

        [UnityTest]
        public IEnumerator APausedGoldSwapRestoresAtItsQuestion()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.StrikeLikeATiger, T.Bushido }, wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            yield return Begin(root, T.StrikeLikeATiger, ActionKind.Attack);
            yield return HitWasp(root);
            var asked = runtime.PendingEffectChoice;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var pause = runtime.CaptureSnapshot();
            yield return PickCard(root, T.StingLikeABee, "The swap.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.TurnArea));

            Assert.That(runtime.RestoreSnapshot(pause), Is.True, runtime.LastOperationDetail);
            yield return null;
            yield return null;
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.NodeId), "The restored match waits at the swap.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause));
            Assert.That(Card(runtime, takahide, T.StrikeLikeATiger).State.Location, Is.EqualTo(CardLocation.TurnArea),
                "Strike like a Tiger is read back into his slot.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.Deck));
            AssertTheGoldSwapWindow(runtime, "The restored window");
            Healthy(runtime);

            yield return PickCard(root, T.StingLikeABee, "The restored swap resolves.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(Card(runtime, takahide, T.StrikeLikeATiger).State.Location, Is.EqualTo(CardLocation.Deck));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator AFloatSwapUndoesToItsRetainedPrefixAndReplaysExactly()
        {
            // Wasp stands three spaces off, so nothing in this turn touches an opponent and the rewind
            // is not walled (8.42e).
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.FloatLikeAButterfly, T.Bushido }, wasp: WaspAtRangeThree);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            yield return Begin(root, T.FloatLikeAButterfly, ActionKind.Movement);
            var step = ReachableSpace(root, runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1).Select(value => value.Destination));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(step).transform.position), Is.True);
            yield return null;
            yield return StopMoving(root);
            var asked = runtime.PendingEffectChoice?.NodeId;
            Assert.That(asked, Is.Not.Null, Remaining(runtime));
            var pause = runtime.CaptureSnapshot();

            OpenDirectorDrawer(root.Hud);
            yield return null;
            for (var press = 0; press < 3 && runtime.PendingMovementChoice == null; press++)
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "UNDO"));
                yield return null;
            }

            Assert.That(runtime.PendingMovementChoice, Is.Not.Null, "Undo gives the open movement back. " + Remaining(runtime));
            Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(step), "The retained prefix keeps his step.");
            Assert.That(runtime.PendingEffectChoice, Is.Null, "The swap is no longer asked.");
            UiPlayer.Click(UiPlayer.Button(root.Hud, "DIRECTOR ▾"));
            yield return null;

            yield return StopMoving(root);
            Assert.That(runtime.PendingEffectChoice?.NodeId, Is.EqualTo(asked.Value));
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause), "The same answer replays into the same pause.");
            yield return PickCard(root, T.StingLikeABee, "The replayed swap.");
            Assert.That(Card(runtime, takahide, T.StingLikeABee).State.Location, Is.EqualTo(CardLocation.TurnArea));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator HeLeavesOnHisDefeatRespawnsByAClickAndARestartRebuildsTheBoard()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.ComeToAid, T.SetAnExample, T.ProvenWarrior }, wasp: WaspBesideHim);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            var standee = HeroView(takahide);
            Assert.That(standee.HeroContentId.Value.Value, Is.EqualTo(T.ContentSetId));

            CommitBoth(runtime, T.ComeToAid, WaspCardContent.MagneticDagger);
            yield return null;
            yield return WaspStartsHerAttack(root);
            yield return TargetHero(root, takahide);
            UiPlayer.ClickDefenseDecline(runtime, root.Hud);
            yield return null;
            Assert.That(OnBoard(runtime, takahide), Is.False, "He is defeated. " + Remaining(runtime));
            Assert.That(standee.gameObject.activeSelf, Is.False, "His standee leaves the board.");
            Assert.That(runtime.BoardView.HeroDefeatGhostFor(standee), Is.Not.Null, "and fades.");

            // His turn-1 card resolves off the board; at his next action p.13 offers the respawn.
            yield return YieldUntilTheNamedSeatActs(runtime, takahide);
            CommitSoTheNamedSeatActs(runtime, takahide, T.SetAnExample);
            yield return YieldUntilTheNamedSeatActs(runtime, takahide);
            var respawn = ReachableSpace(root,
                runtime.RoundView.Model.HeroRespawnOptions.Select(option => option.Destination).ToArray());
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, Tile(respawn).transform.position), Is.True);
            yield return null;
            Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(respawn), "He respawns where he was clicked.");
            Assert.That(HeroView(takahide).Coordinate, Is.EqualTo(respawn));
            Healthy(runtime);

            // The Defeat's spotlight may still be entering: wait for the drawer's control as a player does.
            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            OpenDirectorDrawer(root.Hud);
            yield return null;
            UiPlayer.RestartForReconstruction(runtime);
            yield return null;
            yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(SpaceOf(runtime, takahide), Is.EqualTo(TakahideSpace), "Restart reconstructs the staged board.");
            Assert.That(HeroView(takahide).Coordinate, Is.EqualTo(TakahideSpace));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator StagedTakahideSpawnsWithHisOwnArtworkAndBoardPointer()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTakahide);
            var root = Root();
            var takahide = Takahide(root.Runtime);
            var view = HeroView(takahide);
            Assert.That(view.HeroContentId.Value.Value, Is.EqualTo(T.ContentSetId));
            Assert.That(view.HasTokenVisual, Is.True, "His prepared standee face is drawn.");
            Assert.That(view.Coordinate, Is.EqualTo(TakahideSpace));
            Assert.That(UiPlayer.ClickBoard(Pointer(), root.Hud, view.transform.position), Is.True);
            yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(takahide));
            Assert.That(root.Runtime.HeroOverviewsView.Model.Heroes.Count(hero => hero.HeroId.Equals(takahide)), Is.EqualTo(1));
            Healthy(root.Runtime);
        }

        [UnityTest]
        public IEnumerator EveryTakahideFaceKeepsTheStandardCardFootprint()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedTakahide);
            yield return StandardCardFootprintCheck.EveryVerifiedFaceKeepsTheStandardFootprint(
                "Takahide", T.AllDefinitions, Root().Hud);
        }

        [UnityTest]
        public IEnumerator TakahideLevelUpAddsTheChosenCardAndShowsItsSiblingItem()
        {
            yield return Stage(SceneCardContent.VerifiedEngagedStagedTakahide,
                new[] { T.SetAnExample, T.ProvenWarrior }, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var takahide = Takahide(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(takahide));
                budget++)
                yield return RoundStep(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options
                .First(option => option.Card != null && option.ItemIcon != PrintedValueChannel.None);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(takahide));
            var previous = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon)
                .Select(value => value.Value).SingleOrDefault();
            UiPlayer.EnsureVisible(UpgradeButton(root, upgrade));
            yield return null;
            UiPlayer.Click(UpgradeButton(root, upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location,
                Is.EqualTo(CardLocation.Hand));
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return RoundStep(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(takahide));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value, Is.EqualTo(previous + 1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(takahide)).ItemBonuses,
                Is.Not.Empty);
            Healthy(runtime);
        }

        /// <summary>One step toward the next round, answering a Minion Battle removal where one is asked.</summary>
        private static IEnumerator RoundStep(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            if (runtime.PendingMinionBattleChoice != null)
            {
                var battle = runtime.MinionChoiceView.Model;
                if (battle != null && battle.Candidates.Count > 0)
                    Assert.That(runtime.SubmitMinionChoiceCandidate(
                        battle.Candidates.First(value => value.Destination == null).MinionId), Is.True);
                Assert.That(runtime.ConfirmMinionChoice(), Is.True);
                yield return null;
                yield break;
            }

            yield return NextRoundDecision(root);
        }
    }
}
