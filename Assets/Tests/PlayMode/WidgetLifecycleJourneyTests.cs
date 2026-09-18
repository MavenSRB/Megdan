using System.Collections;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.WidgetUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class WidgetLifecycleJourneyTests
    {
        internal static IEnumerator Tray(MatchSceneCompositionRoot root, HeroId hero)
        {
            var rail = UiPlayer.Descendants(root.Hud.RootElement).Single(element => element.name == "gd-hero-rail" &&
                element.userData is HeroId id && id.Equals(hero));
            UiPlayer.Click(rail.Q("gd-hero-identity-plate")); yield return null;
            Assert.That(root.Runtime.ViewedHeroId, Is.EqualTo(hero));
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in WidgetCardContent.DealtDefinitions.Where(card => card.UltimateRule == null))
            {
                if (definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement))
                {
                    yield return WidgetUiJourneyTests.Stage(definition.Id);
                    var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
                    yield return WidgetUiJourneyTests.Begin(root,definition.Id,ActionKind.Movement);
                    var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        runtime.PendingMovementChoice.LegalDestinations.Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                    Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(target).transform.position), Is.True);
                    yield return null;
                    Assert.That(Position(runtime,actor), Is.EqualTo(target));
                    if (runtime.PendingMovementChoice != null)
                    {
                        UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.RoundView.Model.EndTurnInvocation.Label)); yield return null;
                    }
                    Healthy(runtime);
                }
                var committed = definition.Id.Equals(WidgetCardContent.FieryBreath) ? WidgetCardContent.AllAboard : WidgetCardContent.FieryBreath;
                yield return WidgetUiJourneyTests.Stage(definition.Id,ownHand:new[] { definition.Id,committed });
                var defenseRoot = Root(); var defenseRuntime = defenseRoot.Runtime; var defender = Widget(defenseRuntime);
                var enemy = OtherHeroId(defenseRuntime,defender);
                yield return Tray(defenseRoot,defender);
                yield return CommitThroughTray(defenseRoot,NameOf(defenseRuntime,committed));
                yield return Tray(defenseRoot,enemy);
                yield return CommitThroughTray(defenseRoot,NameOf(defenseRuntime,WaspCardContent.MagneticDagger));
                yield return WaitForActor(defenseRoot,enemy);
                yield return StartActionThroughControl(defenseRoot,ActionKind.Attack);
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),defenseRoot.Hud,HeroView(defender).transform.position), Is.True);
                yield return null;
                Assert.That(defenseRuntime.PendingDefenseChoice.LegalDefenseOffers.Any(offer => offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True,definition.DisplayName);
                yield return ClickDefenseFace(defenseRoot,definition.Id);
                Assert.That(defenseRuntime.PendingDefenseChoice, Is.Null);
                Assert.That(defenseRuntime.Cards.Single(card => card.OwnerId.Equals(defender) && card.DefinitionReference.DefinitionId.Equals(definition.Id)).State.Location,
                    Is.EqualTo(CardLocation.DiscardPile));
                Healthy(defenseRuntime);
            }
        }

        [UnityTest]
        public IEnumerator PyroSurvivesWidgetDefeatAndRespawnThenRestartRebuildsTheSession()
        {
            yield return WidgetUiJourneyTests.Stage(WidgetCardContent.AllAboard);
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime); var enemy = OtherHeroId(runtime,actor);
            yield return PlacePyro(root);
            var token = runtime.BoardView.Model.PlacedTokens.Single();
            yield return Tray(root,actor);
            yield return CommitThroughTray(root,NameOf(runtime,WidgetCardContent.AllAboard));
            yield return Tray(root,enemy);
            yield return CommitThroughTray(root,NameOf(runtime,WaspCardContent.MagneticDagger));
            yield return WaitForActor(root,enemy);
            yield return StartActionThroughControl(root,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,HeroView(actor).transform.position), Is.True);
            yield return null;
            // Milestone 8.46e: the Defense decline is the decision window's corner, read off its model.
            UiPlayer.ClickDefenseDecline(runtime,root.Hud); yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(actor)), Is.False);
            Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            yield return NextCommit(root);
            yield return Tray(root,actor);
            yield return CommitThroughTray(root,NameOf(runtime,WidgetCardContent.FightAsOne));
            yield return Tray(root,enemy);
            yield return CommitThroughTray(root,LowestInitiativeCardOf(runtime,enemy));
            yield return WaitForActor(root,actor);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(destination).transform.position), Is.True);
            yield return null;
            Assert.That(Position(runtime,actor), Is.EqualTo(destination));
            Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Healthy(runtime);
            OpenDirectorDrawer(root.Hud); yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud,"RESET")); yield return null; yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(runtime.BoardView.Model.PlacedTokens, Is.Empty);
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>().Count(view => view.Occupancy.HeroId.Equals(actor)), Is.EqualTo(1));
            Assert.That(runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(actor)).TrayCards.Count,
                Is.EqualTo(runtime.Cards.Count(card => card.OwnerId.Equals(actor) && card.State.Location == CardLocation.Hand)));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator ClearRemovesPyroThroughItsBoardPieceAndUndoRestoresIt()
        {
            yield return WidgetUiJourneyTests.Stage(WidgetCardContent.FightAsOne);
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
            yield return PlacePyro(root);
            var token = runtime.BoardView.Model.PlacedTokens.Single();
            yield return CommitEverySeat(root,actor,NameOf(runtime,WidgetCardContent.FightAsOne));
            yield return WaitForActor(root,actor);
            yield return Tray(root,actor);
            var clear = UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(control =>
                control.name == "gd-action-replacement-chip" && control.text == "CLEAR");
            UiPlayer.Click(clear); yield return null;
            var view = runtime.BoardView.TokenViews.Single(value => value.BoundTokenId.Equals(token.Id));
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,view.transform.position), Is.True);
            yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.ClearSelection.Confirm.Label)); yield return null;
            Assert.That(runtime.BoardView.Model.PlacedTokens, Is.Empty);
            Assert.That(view.gameObject.activeSelf, Is.False);
            OpenDirectorDrawer(root.Hud); yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud,"UNDO")); yield return null;
            Assert.That(runtime.BoardView.Model.PlacedTokens.Single().Id, Is.EqualTo(token.Id));
            Assert.That(runtime.BoardView.TokenViews.Count(value => value.gameObject.activeSelf), Is.EqualTo(1));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator LevelUpUsesTheChosenWholeFaceAndItsEarnedAttackItem()
        {
            yield return WidgetUiJourneyTests.Stage(WidgetCardContent.FightAsOne);
            var root = Root(); var runtime = root.Runtime; var actor = Widget(runtime);
            for (var budget=0; budget<180 && !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(actor)); budget++)
                yield return GarrusLifecycleJourneyTests.NextRoundDecision(root);
            Assert.That(runtime.Projection.PendingSystemTransitionDescription.HeroId, Is.EqualTo(actor));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options.First(value => value.Card != null && value.ItemIcon == PrintedValueChannel.Attack);
            Button Choice() => UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip == (upgrade.Detail.Length>0 ? upgrade.Label + " - " + upgrade.Detail : upgrade.Label));
            UiPlayer.SetPlayerViewport(2560,540,"Widget level-up short viewport");
            yield return null; yield return null; root.Hud.Layout(); yield return null;
            UiPlayer.EnsureVisible(Choice()); yield return null;
            yield return UiPlayer.CaptureGameView("widget-f","level-up-short");
            UiPlayer.Click(Choice()); yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location, Is.EqualTo(CardLocation.Hand));
            UiPlayer.SetPlayerViewport(1920,1080,"Widget normal viewport"); yield return null;
            for (var budget=0; budget<60 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return GarrusLifecycleJourneyTests.NextRoundDecision(root);
            var attackItem = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(actor))
                .ItemBonuses.Single(value => value.Key == PrintedValueChannel.Attack).Value;
            Assert.That(attackItem, Is.GreaterThan(0));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(actor)).ItemBonuses, Is.Not.Empty);
            yield return WidgetUiJourneyTests.Begin(root,WidgetCardContent.FightAsOne,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,HeroView(OtherHeroId(runtime,actor)).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice.AttackTotal, Is.EqualTo(4+attackItem));
            Healthy(runtime);
        }
    }
}
