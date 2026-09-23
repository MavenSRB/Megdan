using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
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
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class GarrusLifecycleJourneyTests
    {
        [UnityTest]
        public IEnumerator AngryStrikeCountsHisDiscardsWhenTheHeroIsTargeted()
        {
            yield return Stage(GarrusCardContent.AngryStrike,
                ownHand: new[] { GarrusCardContent.AngryStrike,GarrusCardContent.ChillingHowl });
            var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
            var discards = runtime.Cards.Count(card => card.OwnerId.Equals(actor) && card.State.Location == CardLocation.DiscardPile);
            yield return Begin(root,GarrusCardContent.AngryStrike,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,
                HeroView(OtherHeroId(runtime,actor)).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingDefenseChoice.AttackTotal, Is.EqualTo(4 + discards));
            // Milestone 8.46e: the Defense decline is the decision window's corner, read off its model.
            UiPlayer.ClickDefenseDecline(runtime,root.Hud); yield return null;
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
            // THE ATTACK'S OWN OUTCOME (milestone 8.86). This journey read the counted TOTAL and
            // stopped there, so an Attack that computed the right number and then applied nothing
            // would have passed it. An undefended Attack is p.14's defeat.
            Assert.That(
                runtime.Heroes.Any(hero => hero.HeroId.Equals(OtherHeroId(runtime,actor))), Is.False,
                "The counted Attack was answered by no Defense at all, so p.14 defeats the hero it " +
                "was aimed at and the piece leaves the board. " + Remaining(runtime));
            Healthy(runtime);
        }

        // Walk ordinary filler turns through the same controls, including board-only system choices.
        internal static IEnumerator NextRoundDecision(MatchSceneCompositionRoot root)
        {
            var runtime = root.Runtime;
            var hand = runtime.HandsView.Model.Hands.FirstOrDefault(value =>
                value.CommitmentStatus == TurnCardCommitmentStatus.AwaitingResponse && value.Cards.Count > 0);
            if (hand != null)
            {
                yield return PointTrayAt(root,hand.HeroId);
                yield return CommitThroughTray(root,NameOf(runtime,hand.Cards[0].DefinitionReference.DefinitionId));
            }
            // Milestone 8.46e: a tied initiative is drawn on the decision window as card faces.
            else if (UiPlayer.DecisionControl(runtime, command => command.Kind == DirectorCommandKind.ChooseTiedActor) != null)
                UiPlayer.ClickDecisionControl(runtime, root.Hud, UiPlayer.DecisionControl(runtime, command => command.Kind == DirectorCommandKind.ChooseTiedActor));
            else if (runtime.BoardView.Model.SystemTransitionDestinations.Count > 0)
            {
                var at = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                    runtime.BoardView.Model.SystemTransitionDestinations.Select(value => value.Destination).ToArray());
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(at).transform.position), Is.True);
            }
            else if (runtime.RoundView.Model.SystemTransitionPrompt != null)
            {
                var option = runtime.RoundView.Model.SystemTransitionPrompt.Options[0];
                if (option.Card != null)
                {
                    UiPlayer.EnsureVisible(UpgradeButton(root,option));
                    yield return null;
                    UiPlayer.Click(UpgradeButton(root,option));
                }
                else UiPlayer.Click(UiPlayer.Button(root.Hud,option.Label));
            }
            else if (runtime.RoundView.Model.ContinueEndOfRoundInvocation != null)
                UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.RoundView.Model.ContinueEndOfRoundInvocation.Label));
            else
            {
                Assert.That(runtime.RoundView.Model.EndTurnInvocation, Is.Not.Null,Remaining(runtime));
                UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.RoundView.Model.EndTurnInvocation.Label));
            }
            yield return null;
            Healthy(runtime);
        }

        private static Button UpgradeButton(MatchSceneCompositionRoot root, SystemTransitionOptionViewModel option) =>
            UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip == (option.Detail.Length > 0 ? option.Label + " - " + option.Detail : option.Label));

        [UnityTest]
        public IEnumerator HowlRestrictsTheOpponentAndExpiresWhenTheRoundRetrievesIt()
        {
            yield return Stage(GarrusCardContent.ChillingHowl);
            var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime); var enemy = OtherHeroId(runtime,actor);
            yield return PointTrayAt(root,actor);
            yield return CommitThroughTray(root,NameOf(runtime,GarrusCardContent.ChillingHowl));
            yield return PointTrayAt(root,enemy);
            yield return CommitThroughTray(root,NameOf(runtime,WaspCardContent.Shock));
            yield return WaitForActor(root,actor);
            yield return PointTrayAt(root,actor);
            yield return StartActionThroughControl(root,ActionKind.Skill);
            var howl = runtime.Cards.Single(card => card.OwnerId.Equals(actor) && card.DefinitionReference.DefinitionId.Equals(GarrusCardContent.ChillingHowl));
            Assert.That(runtime.IsActiveEffectSource(howl.Id), Is.True);
            Assert.That(runtime.BoardView.LitAuraTiles.Count, Is.GreaterThan(0));
            yield return WaitForActor(root,enemy);
            yield return PointTrayAt(root,enemy);
            Assert.That(runtime.Projection.AvailableFastTravelOptions, Is.Empty);
            yield return UiPlayer.CaptureGameView("8-41f","howl-opponent");
            yield return StartActionThroughControl(root,ActionKind.Movement);
            Assert.That(runtime.PendingMovementChoice.RemainingAllowance, Is.EqualTo(2), "Shock prints Move 4, but the aura caps this action at 2.");
            var at = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                runtime.PendingMovementChoice.LegalDestinations.Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
            var walkedFrom = Position(runtime,enemy);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(at).transform.position), Is.True);
            yield return null;
            Assert.That(runtime.PendingMovementChoice.RemainingAllowance, Is.EqualTo(1));
            // MILESTONE 8.86: the capped walk is read on the BOARD as well as on the allowance - a
            // step that spent the allowance and moved nobody would have passed the line above.
            Assert.That(Position(runtime,enemy), Is.EqualTo(at),
                "The restricted hero really walked the step the aura still allows him. " +
                Remaining(runtime));
            Assert.That(Distance(Position(runtime,enemy),walkedFrom), Is.EqualTo(1),
                "one space, for one point of the capped allowance. " + Remaining(runtime));
            var sawLaterTurn = false;
            for (var budget = 0; budget < 100 && runtime.RoundIndex == 1; budget++)
            {
                yield return NextRoundDecision(root);
                if (runtime.RoundIndex == 1 && runtime.TurnIndex > 1 && runtime.Phase != RoundResolutionPhase.EndOfRound)
                {
                    sawLaterTurn = true;
                    Assert.That(runtime.IsActiveEffectSource(howl.Id), Is.True);
                }
            }
            Assert.That(sawLaterTurn, Is.True);
            Assert.That(runtime.RoundIndex, Is.EqualTo(2));
            Assert.That(runtime.IsActiveEffectSource(howl.Id), Is.False);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(howl.Id)).State.Location, Is.EqualTo(CardLocation.Hand));
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator LevelUpChoosesTheWholeUpgradeAndShowsItsSiblingItem()
        {
            yield return Stage(GarrusCardContent.AngryStrike,coins:0);
            var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
            for (var budget = 0; budget < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(actor)); budget++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));
            Assert.That(runtime.Projection.PendingSystemTransitionDescription.HeroId, Is.EqualTo(actor));
            var upgrade = runtime.RoundView.Model.SystemTransitionPrompt.Options.First(value => value.Card != null && value.ItemIcon == PrintedValueChannel.Attack);
            var before = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(actor));
            var oldItems = before.ItemBonuses.Where(value => value.Key == upgrade.ItemIcon).Select(value => value.Value).SingleOrDefault();
            UiPlayer.SetPlayerViewport(2560,540,"Garrus level-up short viewport");
            yield return null; yield return null; root.Hud.Layout(); yield return null;
            UiPlayer.EnsureVisible(UpgradeButton(root,upgrade));
            yield return null;
            yield return UiPlayer.CaptureGameView("8-41f","level-up-short");
            UiPlayer.Click(UpgradeButton(root,upgrade));
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(upgrade.Card.CardInstanceId)).State.Location, Is.EqualTo(CardLocation.Hand));
            UiPlayer.SetPlayerViewport(1920,1080,"restore after Garrus level-up");
            yield return null;
            for (var budget = 0; budget < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; budget++)
                yield return NextRoundDecision(root);
            var after = runtime.Projection.HeroProgression.Single(value => value.HeroId.Equals(actor));
            Assert.That(after.ItemBonuses.Single(value => value.Key == upgrade.ItemIcon).Value, Is.EqualTo(oldItems+1));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(actor)).ItemBonuses, Is.Not.Empty);
            yield return Begin(root,GarrusCardContent.AngryStrike,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,
                HeroView(OtherHeroId(runtime,actor)).transform.position), Is.True);
            yield return null;
            var discarded = runtime.Cards.Count(card => card.OwnerId.Equals(actor) && card.State.Location == CardLocation.DiscardPile);
            Assert.That(runtime.PendingDefenseChoice.AttackTotal, Is.EqualTo(4 + discarded + oldItems + 1), "The item gained from Light Pilum actually raises this Attack.");
            Healthy(runtime);
        }

        [UnityTest]
        public IEnumerator EveryPrintedSecondaryMovesOrDefendsThroughItsOwnCardFace()
        {
            foreach (var definition in GarrusCardContent.DealtDefinitions.Where(card => card.UltimateRule == null))
            {
                if (definition.ActionDefinitions.Any(action => action.Kind == ActionKind.Movement))
                {
                    yield return Stage(definition.Id);
                    var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                    yield return Begin(root,definition.Id,ActionKind.Movement);
                    var target = FirstClickableSpace(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),
                        runtime.PendingMovementChoice.LegalDestinations.Where(value => value.Cost == 1).Select(value => value.Destination).ToArray());
                    Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(target).transform.position), Is.True);
                    yield return null;
                    Assert.That(Position(runtime,actor), Is.EqualTo(target));
                    if (runtime.PendingMovementChoice != null)
                    {
                        UiPlayer.Click(UiPlayer.Button(root.Hud,runtime.RoundView.Model.EndTurnInvocation.Label));
                        yield return null;
                    }
                    Healthy(runtime);
                }
                var committed = definition.Id.Equals(GarrusCardContent.HoldGround) ? GarrusCardContent.Trace : GarrusCardContent.HoldGround;
                yield return Stage(definition.Id,ownHand:new[] { definition.Id,committed });
                var defenseRoot = Root(); var defenseRuntime = defenseRoot.Runtime; var defender = Garrus(defenseRuntime);
                var enemy = OtherHeroId(defenseRuntime,defender);
                yield return PointTrayAt(defenseRoot,defender);
                yield return CommitThroughTray(defenseRoot,NameOf(defenseRuntime,committed));
                yield return PointTrayAt(defenseRoot,enemy);
                yield return CommitThroughTray(defenseRoot,NameOf(defenseRuntime,WaspCardContent.MagneticDagger));
                yield return YieldUntilTheNamedSeatActs(defenseRuntime,enemy);
                yield return StartActionThroughControl(defenseRoot,ActionKind.Attack);
                Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),defenseRoot.Hud,HeroView(defender).transform.position), Is.True);
                yield return null;
                Assert.That(defenseRuntime.PendingDefenseChoice.LegalDefenseOffers.Any(offer => offer.SourceDefinitionReference.DefinitionId.Equals(definition.Id)), Is.True,definition.DisplayName);
                yield return ClickDefenseFace(defenseRoot,definition.Id);
                Assert.That(defenseRuntime.PendingDefenseChoice, Is.Null);
                Assert.That(defenseRuntime.Cards.Single(card => card.OwnerId.Equals(defender) && card.DefinitionReference.DefinitionId.Equals(definition.Id)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
                Healthy(defenseRuntime);
            }
        }

        [UnityTest]
        public IEnumerator AllThreeRetrievalFacesChooseDiscardsWithTwoEnemyHeroesPresent()
        {
            foreach (var face in new[] { GarrusCardContent.HoldGround,GarrusCardContent.MakeAStand,GarrusCardContent.BattleReady })
            {
                var roster = RegisteredHeroRoster.ForThisBuild;
                var own = roster.Single(hero => hero.ContentId.Value == "garrus");
                var wasp = roster.Single(hero => hero.ContentId.Value == "wasp");
                var xargatha = roster.Single(hero => hero.ContentId.Value == "xargatha");
                MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.TwoVersusTwoQuick;
                MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                    new[] { own,roster.Single(hero => hero.ContentId.Value == "sabina") },new[] { wasp,xargatha });
                MatchSceneCompositionRoot.NextComposedSandboxStaging = new SandboxStagingDescription(heroes:new[] {
                    new SandboxHeroStaging(new HeroId(own.HeroIdValue),new HexCoordinate(-3,1),hand:new[] { face,GarrusCardContent.AngryStrike,GarrusCardContent.ChillingHowl }),
                    new SandboxHeroStaging(new HeroId(wasp.HeroIdValue),new HexCoordinate(-2,1)),
                    new SandboxHeroStaging(new HeroId(xargatha.HeroIdValue),new HexCoordinate(-2,2)) });
                yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime);
                yield return Begin(root,face,ActionKind.Skill);
                var count = face.Equals(GarrusCardContent.BattleReady) ? 2 : 1;
                for (var i=0; i<count; i++)
                {
                    yield return May(root);
                    // MILESTONE 8.86: every face offered is one of HER OWN discards - which is what
                    // "retrieve a discarded card" admits and nothing else - and the one taken is
                    // named by its instance id rather than taken first off the offer.
                    Assert.That(
                        runtime.PendingEffectChoice.Candidates.All(value => runtime.Cards
                            .Single(card => card.Id.Equals(value.Value.CardInstanceId))
                            .OwnerId.Equals(actor) &&
                            runtime.Cards.Single(card => card.Id.Equals(value.Value.CardInstanceId))
                                .State.Location == CardLocation.DiscardPile),
                        Is.True,
                        face.Value + ": the offer is his own discard pile. " + Remaining(runtime));
                    var pileBefore = runtime.Cards.Count(card =>
                        card.OwnerId.Equals(actor) && card.State.Location == CardLocation.DiscardPile);
                    var choice = runtime.PendingEffectChoice.Candidates
                        .OrderBy(value => value.Value.CardInstanceId.Value, StringComparer.Ordinal)
                        .First();
                    yield return Pick(root,choice);
                    Assert.That(runtime.Cards.Single(card => card.Id.Equals(choice.Value.CardInstanceId)).State.Location, Is.EqualTo(CardLocation.Hand));
                    Assert.That(
                        runtime.Cards.Count(card => card.OwnerId.Equals(actor) &&
                            card.State.Location == CardLocation.DiscardPile),
                        Is.EqualTo(pileBefore - 1),
                        face.Value + ": and the pile is exactly one card shorter. " + Remaining(runtime));
                }
                Assert.That(runtime.PendingEffectChoice, Is.Null);
                Healthy(runtime);
            }
        }

        [UnityTest]
        public IEnumerator HisDefeatedPieceRespawnsAndHisSessionRebuildsWithoutDuplicates()
        {
            yield return Stage(GarrusCardContent.HoldGround);
            var root = Root(); var runtime = root.Runtime; var actor = Garrus(runtime); var enemy = OtherHeroId(runtime,actor);
            Commit(runtime,actor,GarrusCardContent.HoldGround);
            Commit(runtime,enemy,WaspCardContent.MagneticDagger);
            yield return YieldUntilTheNamedSeatActs(runtime,enemy);
            yield return PointTrayAt(root,enemy);
            yield return StartActionThroughControl(root,ActionKind.Attack);
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,HeroView(actor).transform.position), Is.True);
            yield return null;
            // Milestone 8.46e: the Defense decline is the decision window's corner, read off its model.
            UiPlayer.ClickDefenseDecline(runtime,root.Hud); yield return null;
            Assert.That(runtime.Heroes.Any(hero => hero.HeroId.Equals(actor)), Is.False);
            yield return AdvanceToNextTurn(runtime);
            CommitSoTheNamedSeatActs(runtime,actor,GarrusCardContent.AngryStrike);
            yield return YieldUntilTheNamedSeatActs(runtime,actor);
            var destination = runtime.RoundView.Model.HeroRespawnOptions.First().Destination;
            Assert.That(UiPlayer.ClickBoard(Object.FindAnyObjectByType<PrimitiveHeroPointerInput>(),root.Hud,Tile(destination).transform.position), Is.True);
            yield return null;
            Assert.That(Position(runtime,actor), Is.EqualTo(destination));
            Healthy(runtime);
            OpenDirectorDrawer(root.Hud); yield return null;
            UiPlayer.Click(UiPlayer.Button(root.Hud,"RESET")); yield return null; yield return null;
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            Assert.That(Object.FindObjectsByType<PrimitiveHeroView>().Count(view => view.Occupancy.HeroId.Equals(actor)), Is.EqualTo(1));
            Assert.That(runtime.HandsView.Model.Hands.Single(hand => hand.HeroId.Equals(actor)).TrayCards.Count,
                Is.EqualTo(runtime.Cards.Count(card => card.OwnerId.Equals(actor) && card.State.Location == CardLocation.Hand)));
            Healthy(runtime);
        }
    }
}
