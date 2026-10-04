using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Replay;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static GuardsDigital.Tests.PlayMode.WidgetUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class WidgetReuseJourneyTests
    {
        [UnityTest]
        public IEnumerator FightAsOneExcludesTheAttackedHeroFromItsBorrowedSkill()
        {
            yield return WidgetUiJourneyTests.Stage(WidgetCardContent.FieryBreath,enemyHand:new[] {
                WaspCardContent.Shock,WaspCardContent.Electrocute,WaspCardContent.ChargedBoomerang,
                WaspCardContent.Electroblast,WaspCardContent.StaticBarrier });
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime); var enemy = OtherHeroId(runtime,actor);
            yield return PlacePyro(root);
            yield return WidgetUiJourneyTests.Begin(root,WidgetCardContent.FieryBreath,ActionKind.Skill);
            yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.HeroId.Equals(enemy)));
            var barrier = runtime.Cards.Single(card => card.OwnerId.Equals(enemy) && card.DefinitionReference.DefinitionId.Equals(WaspCardContent.StaticBarrier));
            yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.CardInstanceId.Equals(barrier.Id)));
            yield return NextCommit(root);
            yield return WidgetUiJourneyTests.Begin(root,WidgetCardContent.FightAsOne,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,HeroView(enemy).transform.position), Is.True);
            yield return null;
            yield return ClickDefenseFace(root,WaspCardContent.Electroblast);
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(enemy)), Is.True);
            var breath = runtime.Cards.Single(card => card.OwnerId.Equals(actor) && card.DefinitionReference.DefinitionId.Equals(WidgetCardContent.FieryBreath));
            Assert.That(breath.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(value => value.Value.Kind == EffectValueKind.CardInstance), Is.True,
                "Dragon Bond still qualifies because the exclusion does not ban tokens/spaces.");
            // Milestone 9.38. The attacked hero is the only enemy in Range and in Pyro's line, so the
            // resolved Breath can act on no different unit and the borrow does not offer it (before,
            // it was offered, picked and canceled - the 2026-10-02 playtester's report).
            Assert.That(runtime.PendingEffectChoice.Candidates.Select(value => value.Value).ToArray(),
                Has.No.Member(EffectValue.Card(breath.Id)),
                "The borrowed Breath could only target the original attack target, so it is not offered.");
            var enemyHand = runtime.Cards.Where(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand).Select(card => card.Id).ToArray();
            yield return May(root,false);
            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.Cards.Where(card => card.OwnerId.Equals(enemy) && card.State.Location == CardLocation.Hand).Select(card => card.Id),
                Is.EqualTo(enemyHand),"Nothing borrowed asked the original attack target to discard again.");
            Assert.That(breath.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator FightAsOneReusesAResolvedTokenOnlySkillWithoutMovingItsCard()
        {
            yield return WidgetUiJourneyTests.Stage(WidgetCardContent.FightAsOne);
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
            yield return PlacePyro(root);
            var source = runtime.Cards.Single(card => card.OwnerId.Equals(actor) &&
                card.DefinitionReference.DefinitionId.Equals(WidgetCardContent.DragonBond));
            var state = source.State;
            yield return WidgetUiJourneyTests.Begin(root,WidgetCardContent.FightAsOne,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,
                HeroView(OtherHeroId(runtime,actor)).transform.position), Is.True);
            yield return null;
            yield return ClickDefenseFace(root,WaspCardContent.StaticBarrier);
            Assert.That(runtime.PendingEffectChoice.Candidates.Any(value => value.Value.Kind == EffectValueKind.CardInstance &&
                value.Value.CardInstanceId.Equals(source.Id)), Is.True);
            yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.CardInstanceId.Equals(source.Id)));
            yield return Option(root,"place-pyro");
            var tokenBefore = Pyro(runtime);
            yield return UiPlayer.WaitForControl(root.Hud, "DIRECTOR");
            if (UiPlayer.HasButton(root.Hud, "MINIMISE"))
            {
                UiPlayer.Click(UiPlayer.Button(root.Hud, "MINIMISE"));
                yield return null;
            }
            yield return Space(root);
            Assert.That(Pyro(runtime), Is.Not.EqualTo(tokenBefore));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State, Is.EqualTo(state));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator DragonKnightReusesTheCurrentSkillAfterZeroMovementThroughUndoAndRestore()
        {
            foreach (var accept in new[] { true,false })
            {
                yield return WidgetUiJourneyTests.Stage(WidgetCardContent.AllAboard,ultimate:true);
                var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                yield return PlacePyro(root);
                yield return WidgetUiJourneyTests.Begin(root,WidgetCardContent.AllAboard,ActionKind.Movement);
                var stop = runtime.RoundView.Model.EndTurnInvocation;
                Assert.That(stop.Command.Kind, Is.EqualTo(DirectorCommandKind.StopMovement));
                var before = Position(runtime,actor); var tokenBefore = Pyro(runtime);
                yield return UiPlayer.CaptureGameView("widget-f","finish-movement");
                UiPlayer.Click(UiPlayer.Button(root.Hud,"FINISH MOVEMENT")); yield return null;
                Assert.That(Position(runtime,actor), Is.EqualTo(before));
                Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(actor)).UltimateCard.DisplayName,
                    Is.EqualTo("Dragon Knight"));
                var source = runtime.Cards.Single(card => card.OwnerId.Equals(actor) &&
                    card.DefinitionReference.DefinitionId.Equals(WidgetCardContent.AllAboard));
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,Remaining(runtime) + " / Ultimate " +
                    runtime.Cards.Single(card => card.OwnerId.Equals(actor) && card.DefinitionReference.DefinitionId.Equals(WidgetCardContent.DragonKnight)).State.Location +
                    " / " + string.Join(" | ",runtime.Projection.DiagnosticEvents.Select(value => value.DisplayText).TakeLast(8)));
                Assert.That(runtime.PendingEffectChoice.Candidates.Any(value => value.Value.Kind == EffectValueKind.CardInstance &&
                    value.Value.CardInstanceId.Equals(source.Id)), Is.True,"The current public face-up Skill is eligible.");
                var pause = runtime.CaptureSnapshot();
                Assert.That(runtime.RoundView.Model.Status, Does.Not.Contain("retrieve"),
                    "A borrowed Skill performs its Primary without retrieving the source card.");
                yield return UiPlayer.CaptureGameView("widget-f","dragon-knight-pause");
                OpenDirectorDrawer(root.Hud); yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud,"UNDO")); yield return null;
                Assert.That(runtime.PendingMovementChoice, Is.Not.Null);
                UiPlayer.Click(UiPlayer.Button(root.Hud,"DIRECTOR ▾")); yield return null;
                UiPlayer.Click(UiPlayer.Button(root.Hud,"FINISH MOVEMENT")); yield return null;
                Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(pause),"Retained commands replay the same suspended Movement grant.");
                Assert.That(runtime.RestoreSnapshot(pause), Is.True); yield return null;
                if (accept)
                {
                    yield return Pick(root,runtime.PendingEffectChoice.Candidates.Single(value => value.Value.CardInstanceId.Equals(source.Id)));
                    yield return Option(root,"self");
                    Assert.That(Position(runtime,actor), Is.EqualTo(tokenBefore));
                    Assert.That(Pyro(runtime), Is.EqualTo(before));
                }
                else yield return May(root,false);
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Assert.That(runtime.Cards.Single(card => card.Id.Equals(source.Id)).State.Location, Is.EqualTo(CardLocation.TurnArea));
                Healthy(runtime);
            }
        }
    }
}
