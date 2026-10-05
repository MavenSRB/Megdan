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
                foreach(var label in statusLabels)
                    Assert.That(label.worldBound.yMax,Is.LessThanOrEqualTo(bar.worldBound.yMax),"Both status lines must fit inside the existing bar: "+label.text);
                Assert.That(statusLabels[0].worldBound.height,Is.LessThanOrEqualTo(30),"Round/turn must occupy exactly one line.");
                Assert.That(bar.Query<Label>("gd-life-counter-heart").ToList().All(h=>h.text=="\u2665"),Is.True,"Life counters must contain the actual heart glyph.");
                var narration=bar.Q("gd-narration-lane");
                foreach(var heart in bar.Query<VisualElement>("gd-life-counter").ToList())
                    Assert.That(heart.worldBound.Overlaps(narration.worldBound),Is.False,"Hearts must stay beside the narration.");
                yield return UiPlayer.CaptureScreen("tmp/956-normal-"+viewport.Item1+".png");
            }
            host.AdvanceClock(TimeSpan.FromSeconds(3).Ticks);root.Runtime.RefreshAll();yield return null;yield return null;
            var top=root.Hud.RootElement.Q("gd-topbar");Assert.That(top.Query<VisualElement>("gd-main-clock").ToList().Count,Is.EqualTo(2));
            Assert.That(top.Q("gd-red-bank-preview"),Is.Null);Assert.That(top.Q("gd-blue-bank-preview"),Is.Null);
            yield return UiPlayer.CaptureScreen("tmp/956-banks-1920.png");
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
            yield return UiPlayer.CaptureScreen("tmp/956-mixed-1920.png");
            root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
            var menu=new PrivateLobbyUnityView();menu.ShowPage(PrivateLobbyEntryPage.Host);
            root.Hud.RootElement.Add(menu.Root);
            menu.Root.style.position=UnityEngine.UIElements.Position.Absolute;menu.Root.style.left=0;menu.Root.style.top=0;
            menu.Root.style.width=Length.Percent(100);menu.Root.style.height=Length.Percent(100);
            yield return null;yield return null;
            UiPlayer.Click(menu.Root.Q<Button>("experimental-button"));
            menu.Root.Q<Toggle>("experimental-timer").value=true;
            menu.Root.Q<Toggle>("experimental-coordination").value=true;
            yield return null;yield return null;
            Assert.That(menu.Root.Query<IntegerField>().ToList().Where(f=>f.name.StartsWith("experimental-duration-")).Count(),Is.EqualTo(5));
            yield return UiPlayer.CaptureScreen("tmp/956-host-menu-1920.png");
            menu.Root.RemoveFromHierarchy();
        }
    }
}
