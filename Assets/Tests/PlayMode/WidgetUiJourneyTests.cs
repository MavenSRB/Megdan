using System.Collections;
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
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>Staging only supplies the initial hand/positions. Even Pyro is placed by real input.</summary>
    public sealed class WidgetUiJourneyTests
    {
        internal static HeroId Widget(ConfiguredMatchSceneRuntime runtime) =>
            runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "widget").HeroId;
        internal static HexCoordinate Pyro(ConfiguredMatchSceneRuntime runtime) =>
            runtime.BoardView.Model.PlacedTokens.Single().Coordinate;

        internal static IEnumerator Stage(CardDefinitionId face, bool ultimate = false,
            CardDefinitionId[] ownHand = null, CardDefinitionId[] enemyHand = null,
            bool friendly = false, bool minion = false)
        {
            var content = ultimate ? SceneCardContent.VerifiedTierThreeEngagedStagedWidget
                : SceneCardContent.VerifiedEngagedStagedWidget;
            yield return LoadStagedScene(content);
            var runtime = Root().Runtime; var actor = Widget(runtime);
            var hand = ownHand ?? new[] { face, WidgetCardContent.DragonBond, WidgetCardContent.FightAsOne,
                WidgetCardContent.AllAboard, WidgetCardContent.FieryBreath, WidgetCardContent.DiversionaryStrike }
                .Distinct().Take(5).ToArray();
            if (friendly)
            {
                var roster = RegisteredHeroRoster.ForThisBuild;
                var widget = roster.Single(hero => hero.ContentId.Value == "widget");
                var ally = roster.Single(hero => hero.ContentId.Value == "sabina");
                var enemy = roster.Single(hero => hero.ContentId.Value == "wasp");
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                    new[] { enemy,roster.Single(hero => hero.ContentId.Value == "arien") },new[] { widget,ally });
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes:new[] {
                    new SandboxHeroStaging(new HeroId(widget.HeroIdValue),new HexCoordinate(-3,1),hand:hand),
                    new SandboxHeroStaging(new HeroId(ally.HeroIdValue),new HexCoordinate(-4,1)),
                    new SandboxHeroStaging(new HeroId(enemy.HeroIdValue),new HexCoordinate(-2,1)) });
                yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
                yield break;
            }
            MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes: new[] {
                new SandboxHeroStaging(actor,new HexCoordinate(-3,1),hand:hand),
                new SandboxHeroStaging(OtherHeroId(runtime,actor),new HexCoordinate(-2,1),hand:enemyHand) },
                spawnedMinions:minion ? new[] { new SandboxMinionStaging(new MinionId("widget-gate-minion"),
                    TeamSide.Red,StandardMinionType.Melee,new HexCoordinate(-1,0)) } : null,
                removedMinions:minion ? runtime.ReadModel.Minions.Where(value => value.Coordinate.Equals(new HexCoordinate(-1,0)))
                    .Select(value => value.MinionId) : null);
            yield return LoadStagedScene(content);
        }

        internal static IEnumerator Begin(MatchSceneCompositionRoot root, CardDefinitionId face, ActionKind kind)
        {
            var actor = Widget(root.Runtime);
            yield return CommitEverySeat(root,actor,NameOf(root.Runtime,face));
            yield return WaitForActor(root,actor);
            yield return PointTrayAt(root,actor);
            yield return StartActionThroughControl(root,kind);
            Healthy(root.Runtime);
        }

        internal static IEnumerator Option(MatchSceneCompositionRoot root, string node)
        {
            var old = root.Runtime.PendingEffectChoice.NodeId;
            yield return UiPlayer.WaitForDecisionWindow(root.Runtime, root.Hud);
            // Milestone 8.46e: the bullet is the decision window's own control - the strip no longer
            // draws a family the window has taken - so it is found there by the branch node it stands
            // for, and its label is read off the model rather than typed.
            var control = UiPlayer.EffectOption(root.Runtime, node);
            Assert.That(control, Is.Not.Null,
                $"The decision window offers no bullet for '{node}'. The shown controls are: " +
                UiPlayer.Describe(root.Hud));
            var version = root.Runtime.Projection.Version;
            var commands = root.Runtime.AcceptedCommands.Count;
            var stale = control.Command;
            UiPlayer.Click(UiPlayer.Button(root.Hud,control.Label));
            yield return null;
            Assert.That(root.Runtime.Projection.Version-version, Is.InRange(1L,2L),
                "Selection plus its projected confirmation: " + old.Value);
            if (root.Runtime.Projection.IsHistoryAvailable)
                Assert.That(root.Runtime.AcceptedCommands.Count-commands, Is.EqualTo(root.Runtime.Projection.Version-version));
            var snapshot = root.Runtime.CaptureSnapshot();
            Assert.That(root.Runtime.SubmitProjectedCommand(stale), Is.False);
            Assert.That(root.Runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
            Healthy(root.Runtime);
        }

        internal static IEnumerator NextCommit(MatchSceneCompositionRoot root)
        {
            for (var budget=0; budget<30 && !root.Runtime.HandsView.Model.Hands.Any(hand =>
                hand.CommitmentStatus == TurnCardCommitmentStatus.AwaitingResponse && hand.Cards.Count>0); budget++)
                yield return GarrusLifecycleJourneyTests.NextRoundDecision(root);
            Assert.That(root.Runtime.GetCommitmentStatus(Widget(root.Runtime)),
                Is.EqualTo(TurnCardCommitmentStatus.AwaitingResponse));
        }

        internal static IEnumerator PlacePyro(MatchSceneCompositionRoot root)
        {
            yield return Begin(root,WidgetCardContent.DragonBond,ActionKind.Skill);
            yield return Option(root,"place-pyro");
            yield return Space(root,new HexCoordinate(-2,0));
            Assert.That(Pyro(root.Runtime), Is.EqualTo(new HexCoordinate(-2,0)));
            yield return NextCommit(root);
        }

        /// <summary>
        /// One offered space of the standing move that is NOT the one the piece already stands on,
        /// chosen so a camera ray reaches it. Milestone 8.86: an "up to" move offers the mover's own
        /// space, and a journey that clicks it has proved a move by declining it.
        /// </summary>
        internal static HexCoordinate TravellingSpace(
            ConfiguredMatchSceneRuntime runtime, HexCoordinate from)
        {
            var offered = runtime.PendingEffectChoice.Candidates
                .Where(value => value.Value.Kind == EffectValueKind.Coordinate)
                .Select(value => value.Value.Coordinate)
                .Where(value => !value.Equals(from))
                .ToArray();
            Assert.That(offered, Is.Not.Empty,
                "\"up to 2 spaces\" - every space offered was the one the piece already stood on, " +
                "so taking the step would move nothing. " + Remaining(runtime));
            return FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(), offered);
        }

        internal static IEnumerator NextRound(MatchSceneCompositionRoot root)
        {
            var round = root.Runtime.RoundIndex;
            var token = root.Runtime.BoardView.Model.PlacedTokens.Single();
            for (var budget=0; budget<180 && root.Runtime.RoundIndex == round; budget++)
                yield return GarrusLifecycleJourneyTests.NextRoundDecision(root);
            Assert.That(root.Runtime.RoundIndex, Is.EqualTo(round+1));
            Assert.That(root.Runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(Pyro(root.Runtime), Is.EqualTo(token.Coordinate));
        }

        [UnityTest]
        public IEnumerator DragonBondMovesBothPiecesInEitherOrderAndRelocatesTheSamePyro()
        {
            foreach (var order in new[] { "hero-first","pyro-first" })
            {
                yield return Stage(WidgetCardContent.DragonBond);
                var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                yield return PlacePyro(root);
                var token = runtime.BoardView.Model.PlacedTokens.Single().Id;
                yield return NextRound(root);
                // Round cleanup retrieves Dragon Bond but retains the same Pyro.
                yield return Begin(root,WidgetCardContent.DragonBond,ActionKind.Skill);
                yield return Option(root,"move-both");
                yield return Option(root,order);
                var heroStart = Position(runtime,actor); var pyroStart = Pyro(runtime);
                // MILESTONE 8.86. Both moves used to take whatever space came first and be judged
                // `InRange(0,2)` - a bound a hero and a token that never left their spaces satisfy,
                // so "moves both pieces" was provable by moving neither. "Move both yourself and
                // Pyro up to 2 spaces" is an `up to`, and rule 1 of this milestone is that an `up
                // to` is proved by CHOOSING a non-zero option: each of the two steps is answered
                // with a space its own piece is not standing on, and each is read back.
                yield return Space(root,TravellingSpace(runtime,order == "hero-first" ? heroStart : pyroStart));
                yield return Space(root,TravellingSpace(runtime,order == "hero-first" ? pyroStart : heroStart));
                Assert.That(Position(runtime,actor), Is.Not.EqualTo(heroStart),
                    "\"move both yourself and Pyro up to 2 spaces\" - Widget really left her space.");
                Assert.That(Pyro(runtime), Is.Not.EqualTo(pyroStart),
                    "and Pyro really left its own, which is the half \"both\" is about.");
                Assert.That(Distance(heroStart,Position(runtime,actor)), Is.InRange(1,2));
                Assert.That(Distance(pyroStart,Pyro(runtime)), Is.InRange(1,2));
                Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token));
                yield return NextRound(root);
                yield return Begin(root,WidgetCardContent.DragonBond,ActionKind.Skill);
                yield return Option(root,"place-pyro");
                var before = Pyro(runtime);
                yield return Space(root);
                Assert.That(Pyro(runtime), Is.Not.EqualTo(before));
                Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator RedFacesResolveTheirPreAndPostAttackPyroOperations()
        {
            foreach (var face in new[] { WidgetCardContent.DiversionaryStrike,WidgetCardContent.DiversionaryAttack,
                WidgetCardContent.DiversionaryAssault,WidgetCardContent.AirborneAttack,WidgetCardContent.AirborneAssault })
            {
                yield return Stage(face);
                var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                yield return PlacePyro(root);
                var before = Position(runtime,actor); var pyroBefore = Pyro(runtime);
                var airborne = face.Equals(WidgetCardContent.AirborneAttack) || face.Equals(WidgetCardContent.AirborneAssault);
                yield return Begin(root,face,ActionKind.Attack);
                if (airborne)
                {
                    yield return May(root);
                    Assert.That(Position(runtime,actor), Is.EqualTo(pyroBefore));
                    Assert.That(Pyro(runtime), Is.EqualTo(before));
                    yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value =>
                        value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(OtherHeroId(runtime,actor))));
                }
                else
                {
                    Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,
                        HeroView(OtherHeroId(runtime,actor)).transform.position), Is.True);
                    yield return null;
                }
                var spent = runtime.Cards.Single(card => card.OwnerId.Equals(OtherHeroId(runtime,actor)) &&
                    card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier));
                yield return ClickDefenseFace(root,WaspCardContent.StaticBarrier);
                // MILESTONE 8.86: p.14's response has an outcome and this journey never read one.
                // What it asserts is the transition the page names for a spent Defense - the face
                // leaves the defender's hand for their pile - rather than survival, which these
                // faces do NOT buy: the audit measured the Static Barrier answer against
                // Diversionary Strike and the defender is DEFEATED by the comparison, so a journey
                // asserting they stood would have been asserting a board this staging never had.
                Assert.That(runtime.PendingDefenseChoice, Is.Null,
                    face.Value + ": the clicked face answered p.14's response.");
                Assert.That(runtime.Cards.Single(card => card.Id.Equals(spent.Id)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile),
                    face.Value + ": p.14 pays a Defense by discarding the face that answered it.");
                if (!airborne)
                {
                    Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("move-pyro"));
                    yield return Space(root);
                    Assert.That(Pyro(runtime), Is.Not.EqualTo(pyroBefore));
                }
                else if (face.Equals(WidgetCardContent.AirborneAssault))
                {
                    yield return May(root);
                    Assert.That(Position(runtime,actor), Is.EqualTo(before));
                    Assert.That(Pyro(runtime), Is.EqualTo(pyroBefore));
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null,face.Value);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BlueSwapsAndPairedMovesUseBoardDestinations()
        {
            foreach (var face in new[] { WidgetCardContent.AllAboard,WidgetCardContent.TakeOff,
                WidgetCardContent.SafeLanding,WidgetCardContent.DragOff,WidgetCardContent.CarryAway })
            {
                yield return Stage(face);
                var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                yield return PlacePyro(root);
                var heroBefore = Position(runtime,actor); var pyroBefore = Pyro(runtime);
                yield return Begin(root,face,ActionKind.Skill);
                if (face.Equals(WidgetCardContent.DragOff) || face.Equals(WidgetCardContent.CarryAway))
                {
                    var enemy = OtherHeroId(runtime,actor); var enemyBefore = Position(runtime,enemy);
                    yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value =>
                        value.Value.Kind == EffectValueKind.Hero && value.Value.HeroId.Equals(enemy)));
                    yield return Space(root);
                    Assert.That(Distance(pyroBefore,Pyro(runtime)), Is.InRange(2,3));
                    Assert.That(Position(runtime,enemy).Q-enemyBefore.Q, Is.EqualTo(Pyro(runtime).Q-pyroBefore.Q));
                    Assert.That(Position(runtime,enemy).R-enemyBefore.R, Is.EqualTo(Pyro(runtime).R-pyroBefore.R));
                }
                else
                {
                    if (face.Equals(WidgetCardContent.SafeLanding))
                    {
                        yield return May(root);
                        yield return Space(root);
                        Assert.That(Distance(pyroBefore,Pyro(runtime)), Is.EqualTo(1));
                        pyroBefore = Pyro(runtime);
                    }
                    yield return Option(root,"self");
                    Assert.That(Position(runtime,actor), Is.EqualTo(pyroBefore));
                    Assert.That(Pyro(runtime), Is.EqualTo(heroBefore));
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator BreathFacesAskTheOtherHeroToDiscardAWholeCard()
        {
            foreach (var face in new[] { WidgetCardContent.FieryBreath,WidgetCardContent.FlamingBreath,WidgetCardContent.ScorchingBreath })
            {
                yield return Stage(face);
                var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                yield return PlacePyro(root);
                yield return Begin(root,face,ActionKind.Skill);
                var enemy = OtherHeroId(runtime,actor);
                yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.HeroId.Equals(enemy)));
                Assert.That(runtime.PendingEffectChoice.NodeId.Value, Is.EqualTo("discard"));
                var card = runtime.PendingEffectChoice.Candidates.First(value => value.Value.Kind == EffectValueKind.CardInstance);
                Assert.That(runtime.Cards.Single(value => value.Id.Equals(card.Value.CardInstanceId)).OwnerId, Is.EqualTo(enemy));
                yield return Pick(root,card);
                Assert.That(runtime.Cards.Single(value => value.Id.Equals(card.Value.CardInstanceId)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Assert.That(runtime.BoardView.Model.PlacedTokens, Has.Count.EqualTo(1));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator EveryFriendlySwapChoosesTheAllyOnTheBoard()
        {
            foreach (var face in new[] { WidgetCardContent.AllAboard,WidgetCardContent.TakeOff,WidgetCardContent.SafeLanding })
            {
                yield return Stage(face,friendly:true);
                var root = Root(); var runtime = root.Runtime;
                var ally = runtime.Setup.Heroes.Single(hero => hero.ContentId.Value == "sabina").HeroId;
                yield return PlacePyro(root);
                var before = Position(runtime,ally); var tokenBefore = Pyro(runtime);
                yield return Begin(root,face,ActionKind.Skill);
                if (face.Equals(WidgetCardContent.SafeLanding)) yield return May(root,false);
                yield return Option(root,"friendly");
                yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.HeroId.Equals(ally)));
                Assert.That(Position(runtime,ally), Is.EqualTo(tokenBefore));
                Assert.That(Pyro(runtime), Is.EqualTo(before));
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator NibbleAndGnawRemoveTheirChosenMinionThenPyro()
        {
            foreach (var face in new[] { WidgetCardContent.Nibble,WidgetCardContent.Gnaw })
            {
                yield return Stage(face,minion:true);
                var root = Root(); var runtime = root.Runtime;
                yield return PlacePyro(root);
                yield return Begin(root,face,ActionKind.Skill);
                var target = runtime.PendingEffectChoice.Candidates.Single(value =>
                    value.Value.Kind == EffectValueKind.Minion && value.Value.MinionId.Value == "widget-gate-minion");
                yield return Pick(root,target);
                Assert.That(runtime.ReadModel.Minions.Any(value => value.MinionId.Equals(target.Value.MinionId)), Is.False);
                Assert.That(runtime.BoardView.Model.PlacedTokens, Is.Empty);
                Assert.That(runtime.BoardView.TokenViews.All(view => !view.gameObject.activeSelf), Is.True);
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator ScorchingBreathDefeatsAnEmptyHandWithoutRemovingPyro()
        {
            yield return Stage(WidgetCardContent.ScorchingBreath,enemyHand:new[] { WaspCardContent.Shock,WaspCardContent.LiftUp });
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
            yield return PlacePyro(root);
            yield return Begin(root,WidgetCardContent.ScorchingBreath,ActionKind.Skill);
            var enemy = OtherHeroId(runtime,actor);
            Assert.That(runtime.Cards.Any(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand), Is.False);
            yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.HeroId.Equals(enemy)));
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.False);
            Assert.That(runtime.BoardView.Model.PlacedTokens, Has.Count.EqualTo(1));
            Healthy(runtime);
        }
    }
}
