using System;
using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
namespace GuardsDigital.Tests.PlayMode
{
    public sealed class ExperimentalTimerPlayModeTests
    {
        [UnityTest]
        public IEnumerator BlueSeat_PhaseBannerAndFinalSecondsUseDarkBlue()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = ArienUiJourneyPlayModeTests.Root();
            var blueSeat = root.Runtime.Host.Seats.Seats.First(seat => seat.TeamSide == TeamSide.Blue);

            MatchSceneCompositionRoot.NextComposedSeat = blueSeat.Id;
            root.ConfigureExperiments(new ExperimentalMatchOptions(turnTimer: true));
            yield return null;
            yield return null;

            Assert.That(root.Runtime.Seat?.TeamSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(root.Runtime.PhaseBannerView.TeamSide, Is.EqualTo(TeamSide.Blue));
            root.Runtime.PhaseBannerView.Show(PhaseBannerLabels.Commit(1, 1));
            yield return new WaitForSecondsRealtime(PhaseBannerUnityView.FadeInMs / 1000f + 0.1f);

            var phaseLine = root.Runtime.PhaseBannerView.Root.Q<Label>(PhaseBannerUnityView.LineName);
            Assert.That(phaseLine.style.color.value, Is.EqualTo(PhaseBannerUnityView.DarkBlue));
            if (!UnityEngine.Application.isBatchMode)
            {
                Assert.That(root.Runtime.PhaseBannerView.IsShowing, Is.True);
                yield return UiPlayer.CaptureScreen("tmp/w055-blue-phase-banner.png");
            }

            root.Runtime.PhaseBannerView.Hide();
            root.Hud.RenderClock(new MatchClockState(
                true, 1, 1,
                TimeSpan.FromSeconds(20).Ticks,
                TimeSpan.FromSeconds(20).Ticks,
                new[]
                {
                    new MatchClockEntry(
                        "blue-final-ten", MatchClockKind.Commitment, blueSeat.HeroId, TeamSide.Blue,
                        TimeSpan.FromSeconds(10).Ticks, running: true)
                }));
            yield return null;
            yield return null;

            var countdown = root.Hud.RootElement.Q<Label>(FinalSecondsCountdownUnityView.ValueName);
            Assert.That(countdown.style.color.value, Is.EqualTo(PhaseBannerUnityView.DarkBlue));
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return UiPlayer.CaptureScreen("tmp/w055-blue-final-ten.png");
            }
        }

        [UnityTest]
        public IEnumerator FinalTenSecondCountdown_CoversTheMapWithoutTakingCardCommands()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = ArienUiJourneyPlayModeTests.Root();
            var runtime = root.Runtime;
            var owner = runtime.Host.Seats.Seats[0].HeroId;
            var clock = new MatchClockState(
                true, 1, 1,
                TimeSpan.FromSeconds(20).Ticks,
                TimeSpan.FromSeconds(20).Ticks,
                new[]
                {
                    new MatchClockEntry(
                        "final-ten", MatchClockKind.Commitment, owner, TeamSide.Red,
                        TimeSpan.FromSeconds(10).Ticks, running: true)
                });

            root.Hud.RenderClock(clock);
            yield return null;
            yield return null;

            var overlay = root.Hud.RootElement.Q("gd-final-seconds-countdown");
            Assert.That(overlay, Is.Not.Null);
            Assert.That(UiPlayer.IsShown(overlay), Is.True);
            Assert.That(overlay.Q<Label>("gd-final-seconds-countdown-value")?.text, Is.EqualTo("10"));
            Assert.That(overlay.pickingMode, Is.EqualTo(PickingMode.Ignore));
            Assert.That(overlay.Query<VisualElement>().ToList()
                .All(element => element.pickingMode == PickingMode.Ignore), Is.True);

            if (!UnityEngine.Application.isBatchMode)
            {
                var oldWidth = (uint)Screen.width;
                var oldHeight = (uint)Screen.height;
                UiPlayer.SetPlayerViewport(2560, 1440, "W-011 countdown capture");
                root.Hud.Layout();
                yield return null;
                yield return null;
                yield return UiPlayer.CaptureScreen("tmp/w011-final-ten-countdown-2560x1440.png");
                UiPlayer.SetPlayerViewport(oldWidth, oldHeight, "W-011 restore viewport");
                root.Hud.Layout();
                yield return null;
                yield return null;
            }

            var version = runtime.Host.Version;
            var card = runtime.HandsView.Root.Query<Button>("gd-card-choice").ToList().First();
            UiPlayer.Click(card);
            yield return null;
            var confirm = root.Hud.RootElement.Q<Button>("gd-confirm-control");
            Assert.That(UiPlayer.IsShown(confirm), Is.True,
                "The full-map countdown behaves as absent to the card pointer.");
            UiPlayer.Click(confirm);
            yield return null;
            yield return null;
            Assert.That(runtime.Host.Version, Is.GreaterThan(version),
                "The commitment command passes while the countdown is painted.");

        }

        [UnityTest] public IEnumerator NormalAndBankClocksFitTheUnchangedHeaderAtBothViewports()
        {
            MatchSceneCompositionRoot.NextComposedSeat=null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root=ArienUiJourneyPlayModeTests.Root(); var seat=root.Runtime.Host.Seats.Seats[0];
            MatchSceneCompositionRoot.NextComposedSeat=seat.Id;
            root.ConfigureExperiments(new ExperimentalMatchOptions(turnTimer:true,cardCommitmentSeconds:2,teamBankSeconds:20));
            yield return null;yield return null;
            var host=(MatchHost)root.Runtime.Host; host.SetTimerStarted(true);
            foreach(var s in host.Seats.Seats) host.SetClockReadiness(s.Id,host.Version,true);
            foreach(var viewport in new[]{(1280u,720u),(1920u,1080u)})
            {
                UiPlayer.SetPlayerViewport(viewport.Item1,viewport.Item2,"experimental clocks");
                root.Runtime.RefreshAll(); yield return null;yield return null;root.Hud.Layout();yield return null;yield return null;
                Assert.That(root.Hud.StatusBarRect.height,Is.EqualTo(96));
                var bar=root.Hud.RootElement.Q("gd-topbar");
                Assert.That(bar.Query<Label>().ToList().Any(l=>l.text=="GUARDS DIGITAL"),Is.False);
                var main=bar.Query<VisualElement>("gd-main-clock").ToList();Assert.That(main.Count,Is.EqualTo(1));
                Assert.That(bar.Q("gd-red-bank-preview"),Is.Not.Null);Assert.That(bar.Q("gd-blue-bank-preview"),Is.Not.Null);
                foreach(var clock in main.Concat(new[]{bar.Q("gd-red-bank-preview"),bar.Q("gd-blue-bank-preview")}))
                {Assert.That(bar.worldBound.Contains(clock.worldBound.min),Is.True);Assert.That(bar.worldBound.Contains(clock.worldBound.max),Is.True);}
                var statusLabels=bar.Q("gd-topbar-identity").Query<Label>().ToList();
                Assert.That(statusLabels.Count,Is.EqualTo(2));
                Assert.That((statusLabels[0].worldBound.yMin + statusLabels[1].worldBound.yMax) / 2f,
                    Is.EqualTo(bar.worldBound.center.y).Within(2f), "Status block must be vertically centred.");
                foreach(var window in main.Concat(new[]{bar.Q("gd-red-bank-preview"),bar.Q("gd-blue-bank-preview")}))
                {
                    Assert.That(window.Q<Label>("gd-clock-digits").resolvedStyle.unityTextAlign,Is.EqualTo(TextAnchor.MiddleCenter));
                    Assert.That(window.Q("gd-clock-icon").worldBound.center.y,
                        Is.EqualTo(window.Q("gd-clock-digits").worldBound.center.y).Within(2f));
                }
                foreach(var label in statusLabels)
                    Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(bar.worldBound.yMax),"Both status lines must fit inside the existing bar: "+label.text);
                Assert.That(statusLabels[0].worldBound.height,Is.LessThanOrEqualTo(30),"Round/turn must occupy exactly one line.");
                Assert.That(bar.Query<Label>("gd-life-counter-heart").ToList().All(h=>h.text=="\u2665"),Is.True,"Life counters must contain the actual heart glyph.");
                var narration=bar.Q("gd-narration-lane");
                foreach(var heart in bar.Query<VisualElement>("gd-life-counter").ToList())
                    Assert.That(heart.worldBound.Overlaps(narration.worldBound),Is.False,"Hearts must stay beside the narration.");
                var handCard=root.Runtime.HandsView.Root.Q<Button>("gd-card-choice");
                var roundText=bar.Q<Label>("gd-round-turn");var life=bar.Q("gd-life-counter");
                var narrationText=narration.Q<Label>();
                host.AdvanceClock(TimeSpan.FromMilliseconds(100).Ticks);
                root.Runtime.RefreshClockDisplay(); yield return null;
                Assert.That(root.Runtime.HandsView.Root.Q<Button>("gd-card-choice"),Is.SameAs(handCard));
                Assert.That(bar.Q<Label>("gd-round-turn"),Is.SameAs(roundText));
                Assert.That(bar.Q("gd-life-counter"),Is.SameAs(life));
                Assert.That(narration.Q<Label>(),Is.SameAs(narrationText));
                yield return UiPlayer.CaptureScreen("tmp/964a-normal-"+viewport.Item1+".png");
            }
            host.AdvanceClock(TimeSpan.FromSeconds(3).Ticks);root.Runtime.RefreshAll();yield return null;yield return null;
            var top=root.Hud.RootElement.Q("gd-topbar");Assert.That(top.Query<VisualElement>("gd-main-clock").ToList().Count,Is.EqualTo(2));
            Assert.That(top.Q("gd-red-bank-preview"),Is.Null);Assert.That(top.Q("gd-blue-bank-preview"),Is.Null);
            yield return UiPlayer.CaptureScreen("tmp/964a-banks-1920.png");
            // Mixed bank/allowance and independently started response clocks keep their ownership.
            var model=root.Runtime.ReadModel;
            var clocks=new MatchClockState(true,1,3,TimeSpan.FromSeconds(18).Ticks,TimeSpan.FromSeconds(20).Ticks,
                new[]{new MatchClockEntry("bank",MatchClockKind.Response,new HeroId("red-clock"),TeamSide.Red,0,true),
                    new MatchClockEntry("response",MatchClockKind.Response,new HeroId("blue-clock"),TeamSide.Blue,TimeSpan.FromSeconds(7).Ticks,true)});
            root.Hud.RenderHeader(new MatchHudHeaderViewModel(model.TieBreakerState,model.Setup,roundIndex:99,turnIndex:4,remainingWaveCounters:12,clock:clocks));
            yield return null;yield return null;
            Assert.That(top.Query<VisualElement>("gd-main-clock").ToList().Count,Is.EqualTo(2));
            Assert.That(top.Q("gd-red-bank-preview"),Is.Null);Assert.That(top.Q("gd-blue-bank-preview"),Is.Not.Null);
            foreach(var label in top.Query<VisualElement>("gd-main-clock").ToList().SelectMany(w=>w.Query<Label>().ToList()))
                Assert.That(label.text.All(c=>char.IsDigit(c)||c==':'),Is.True,"Clock windows contain digits only.");
            yield return UiPlayer.CaptureScreen("tmp/964a-mixed-1920.png");

            // Milestone 9.67: readiness can start simultaneous commitment allowances a few
            // seconds apart. They remain separate authority clocks, but the shared header must not
            // turn that implementation detail into four contradictory-looking countdowns.
            var staggeredCommitments=new MatchClockState(true,2,3,
                TimeSpan.FromSeconds(18).Ticks,TimeSpan.FromSeconds(20).Ticks,
                new[]{
                    new MatchClockEntry("commit-a",MatchClockKind.Commitment,new HeroId("red-a"),TeamSide.Red,TimeSpan.FromSeconds(108).Ticks,true),
                    new MatchClockEntry("commit-b",MatchClockKind.Commitment,new HeroId("blue-a"),TeamSide.Blue,TimeSpan.FromSeconds(109).Ticks,true),
                    new MatchClockEntry("commit-c",MatchClockKind.Commitment,new HeroId("blue-b"),TeamSide.Blue,TimeSpan.FromSeconds(111).Ticks,true),
                    new MatchClockEntry("commit-d",MatchClockKind.Commitment,new HeroId("red-b"),TeamSide.Red,TimeSpan.FromSeconds(110).Ticks,true)
                });
            root.Hud.RenderHeader(new MatchHudHeaderViewModel(model.TieBreakerState,model.Setup,
                roundIndex:99,turnIndex:4,remainingWaveCounters:12,clock:staggeredCommitments));
            yield return null;yield return null;
            var sharedCommitment=top.Query<VisualElement>("gd-main-clock").ToList();
            Assert.That(sharedCommitment,Has.Count.EqualTo(1),
                "Simultaneous commitment allowances are one gold header countdown, not one " +
                "badge for every slightly different per-seat value.");
            Assert.That(sharedCommitment[0].Q<Label>("gd-clock-digits").text,Is.EqualTo("01:51"),
                "The shared window lasts until its final current participant's allowance expires.");
            Assert.That(sharedCommitment[0].resolvedStyle.borderTopColor,
                Is.EqualTo(MatchHudTheme.ShineGlow));
            yield return UiPlayer.CaptureScreen("tmp/967-shared-commitment-clock-1920.png");

            root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            var menu=new PrivateLobbyUnityView();menu.ShowPage(PrivateLobbyEntryPage.Host);
            root.Hud.RootElement.Add(menu.Root);
            menu.Root.style.position=UnityEngine.UIElements.Position.Absolute;menu.Root.style.left=0;menu.Root.style.top=0;
            menu.Root.style.width=Length.Percent(100);menu.Root.style.height=Length.Percent(100);
            yield return null;yield return null;
            UiPlayer.Click(menu.Root.Q<Button>("experimental-button"));
            menu.Root.Q<Toggle>("experimental-timer").value=true;
            menu.Root.Q<Toggle>("experimental-coordination").value=true;
            menu.Root.Q<Toggle>("experimental-hero-defeat-base-one").value=true;
            foreach (var name in new[]
            {
                "experimental-shield-items", "experimental-shield-active-effects",
                "experimental-shield-card-effects", "experimental-card-modifiers-items",
                "experimental-card-modifiers-minions", "experimental-card-modifiers-active-effects",
                "experimental-card-modifiers-card-effects"
            })
                menu.Root.Q<Toggle>(name).value = true;
            yield return null;yield return null;
            var cardModifiersHeading=menu.Root.Q<Label>("experimental-card-modifiers-heading");
            var timerHeading=menu.Root.Q<Label>("experimental-timer-heading");
            var timerToggle=menu.Root.Q<Toggle>("experimental-timer");
            Assert.That(timerHeading.worldBound.yMin,Is.GreaterThan(cardModifiersHeading.worldBound.yMax),
                "Turn timer must begin under its own heading after the Card Modifiers section.");
            Assert.That(timerToggle.worldBound.yMin,Is.GreaterThanOrEqualTo(timerHeading.worldBound.yMax));
            Assert.That(menu.Root.Query<IntegerField>().ToList().Where(f=>f.name.StartsWith("experimental-duration-")).Count(),Is.EqualTo(5));
            var commitment=menu.Root.Q<IntegerField>("experimental-duration-0");
            commitment.value=125; commitment.value=0;
            Assert.That(menu.CurrentEntry().Experiments.CardCommitmentSeconds,Is.EqualTo(125));
            Assert.That(menu.CurrentEntry().Experiments.HeroDefeatBaseLifeCostIsOne,Is.True);
            Assert.That(menu.CurrentEntry().Experiments.CardModifiersMinions,Is.True);
            Assert.That(menu.CurrentEntry().Experiments.HeroShieldCardEffects,Is.True);
            Assert.That(menu.Root.Q<Label>("experimental-duration-error").text,Is.Not.Empty);
            foreach(var field in menu.Root.Query<IntegerField>().ToList().Where(f=>f.name.StartsWith("experimental-duration-")))
                Assert.That(field.resolvedStyle.color,Is.EqualTo(Color.black));
            yield return UiPlayer.CaptureScreen("tmp/w019-experimental-popup-1920.png");
            UiPlayer.Click(menu.Root.Q<Button>("experimental-close"));
            yield return null;
            Assert.That(UiPlayer.IsShown(menu.Root.Q<VisualElement>("experimental-menu-popup")), Is.False);
            menu.Root.RemoveFromHierarchy();
        }
    }
}
