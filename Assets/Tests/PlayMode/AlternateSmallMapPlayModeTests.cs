using System;
using System.Collections;
using GuardsDigital.Network;
using Object = UnityEngine.Object;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
namespace GuardsDigital.Tests.PlayMode
{
    public sealed class AlternateSmallMapPlayModeTests
    {
        public static void ClickDropdownItem(Label item)
        {
            var point=item.worldBound.center;
            var picked=item.panel.Pick(point);
            var menu=item.parent;
            while(menu!=null && !menu.ClassListContains(GenericDropdownMenu.ussClassName))menu=menu.parent;
            Assert.That(menu,Is.Not.Null,"The row must belong to the rendered dropdown.");
            Assert.That(picked==menu || menu.Contains(picked),Is.True,"The dropdown row must be reachable through its menu's pointer surface.");
            using(var move=PointerMoveEvent.GetPooled(new Event{type=EventType.MouseMove,mousePosition=point}))
            {move.target=picked;picked.SendEvent(move);}
            using(var down=PointerDownEvent.GetPooled(new Event{type=EventType.MouseDown,button=0,mousePosition=point}))
            {down.target=picked;picked.SendEvent(down);}
            using(var up=PointerUpEvent.GetPooled(new Event{type=EventType.MouseUp,button=0,mousePosition=point}))
            {up.target=picked;picked.SendEvent(up);}
        }
        private static int AvailablePort()
        {
            var probe=new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback,0);
            probe.Start();var port=((System.Net.IPEndPoint)probe.LocalEndpoint).Port;probe.Stop();return port;
        }
        [UnityTest] public IEnumerator JoinInheritsBothHostMapAndFormatOverRealTcp()
        {
            MatchSceneCompositionRoot.NextComposedSeat=null;
            yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root=ArienUiJourneyPlayModeTests.Root();
            var configured = MatchFormat.CreateConfigured(2, 7, 9);
            root.Recompose(MatchCompositionPlan.Printed(configured,OfficialBoardMapCatalog.AlternateSmall));
            var host=root.Runtime.Host;var identity=root.Runtime.SessionIdentity;
            root.Recompose(MatchCompositionPlan.Printed(MatchFormat.ThreeVersusThreeQuick));
            PrivateLobbyDriver.FieldMemoryOverride=new InMemoryPrivateLobbyFieldMemory();PrivateLobbyDriver.SuppressRouterQuestion=true;
            var owner=new GameObject("map-inheritance-client");var driver=owner.AddComponent<PrivateLobbyDriver>();driver.Configure(root);
            PrivateSessionClient Client() => (PrivateSessionClient)typeof(PrivateLobbyDriver).GetProperty("RehearsalClient",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(driver);
            yield return null;yield return null;
            Assert.That(PrivateSessionEndpoint.TryCreate("127.0.0.1",AvailablePort(),out var endpoint,out var error),Is.True,error);
            using(var listener=new TcpSessionListener())
            {
                listener.Start(endpoint);
                var session=new PrivateSessionHost(host,identity,"map-inheritance",listener,new CryptoNonceSource());
                driver.Presenter.Press(PrivateLobbyControl.Join,new PrivateLobbyEntry("127.0.0.1",listener.BoundPort.ToString(),"map-inheritance",default));
                var deadline=Time.realtimeSinceStartup+15;
                while(Time.realtimeSinceStartup<deadline && Client()?.State!=PrivateSessionClientState.Admitted)
                {
                    foreach(var item in listener.Drain())
                    {
                        switch(item.Kind)
                        {
                            case TransportEventKind.Connected:session.OnConnected(item.Ordinal,DateTime.UtcNow.Ticks);break;
                            case TransportEventKind.Frame:session.OnFrame(item.Ordinal,item.Frame);break;
                            case TransportEventKind.Disconnected:session.OnDisconnected(item.Ordinal);break;
                            case TransportEventKind.StreamRejected:session.OnStreamRejected(item.Ordinal,item.Reason);break;
                        }
                    }
                    yield return null;
                }
                Assert.That(Client().State,Is.EqualTo(PrivateSessionClientState.Admitted),Client().RejectionDetail);
                Assert.That(root.ComposedPlan.Map,Is.EqualTo(OfficialBoardMapCatalog.AlternateSmall));
                Assert.That(root.ComposedPlan.Format,Is.EqualTo(configured));
                Assert.That(root.Runtime.ReadModel.Setup.WaveCount,Is.EqualTo(7));
                Assert.That(root.Runtime.ReadModel.Setup.Teams.All(team=>team.LifeCount==9),Is.True);
                Assert.That(Client().RejectionReason,Is.EqualTo(SessionRejectionReason.None));
                Assert.That(driver.View.Root.Q<Label>("lobby-selected-map").text,Does.Contain("Alternate Small Map"));
                driver.Presenter.Press(PrivateLobbyControl.Leave,null);yield return null;
            }
            Object.Destroy(owner);yield return null;
            PrivateLobbyDriver.FieldMemoryOverride=null;PrivateLobbyDriver.SuppressRouterQuestion=false;
        }

        [UnityTest] public IEnumerator StandardHostSelectorAndMapArtworkWorkAtBothSizes()
        {
            var oldWidth=(uint)Screen.width;var oldHeight=(uint)Screen.height;
            try
            {
                MatchSceneCompositionRoot.NextComposedSeat=null;
                yield return BroganEngagedUiJourneyPlayModeTests.LoadStagedScene(SceneCardContent.ConfiguredMatch);
                var root=ArienUiJourneyPlayModeTests.Root();
                root.ConfigureExperiments(ExperimentalMatchOptions.Disabled);
                PrivateLobbyDriver.FieldMemoryOverride=new InMemoryPrivateLobbyFieldMemory();
                PrivateLobbyDriver.SuppressRouterQuestion=true;
                var owner=new GameObject("alternate-map-lobby-test");
                var driver=owner.AddComponent<PrivateLobbyDriver>();driver.Configure(root);
                yield return null;yield return null;
                driver.View.ShowPage(PrivateLobbyEntryPage.Host);
                foreach(var width in new[]{1280u,1920u})
                {
                    UiPlayer.SetPlayerViewport(width,width*9/16,"alternate map host selector");
                    yield return null;yield return null;
                    var field=driver.View.Root.Q<DropdownField>("host-map-selector");
                    Assert.That(UiPlayer.IsShown(field),Is.True);
                    Assert.That(driver.View.Root.Q("experimental-menu").Contains(field),Is.False);
                    Assert.That(driver.View.CurrentEntry().Experiments,Is.EqualTo(ExperimentalMatchOptions.Disabled));
                    UiPlayer.Click(field);yield return null;yield return null;
                    var choice=field.panel.visualTree.Query<Label>().ToList().Last(l=>l.text=="Alternate Small Map" && UiPlayer.IsShown(l));
                    ClickDropdownItem(choice);yield return null;yield return null;
                    Assert.That(driver.View.CurrentEntry().Map,Is.EqualTo(OfficialBoardMapCatalog.AlternateSmall));
                    yield return UiPlayer.CaptureScreen("tmp/961-host-map-"+width+".png");
                }
                var entry=driver.View.CurrentEntry();
                driver.Presenter.Press(PrivateLobbyControl.Host,new PrivateLobbyEntry(
                    string.Empty,AvailablePort().ToString(),"alternate-map-test",default,
                    map:entry.Map,waveCount:7,lifeCountPerTeam:9));
                for(int frame=0;frame<120 && driver.Presenter.Model.Role==PrivateLobbyRole.None;frame++)yield return null;
                Assert.That(driver.Presenter.Model.Role,Is.EqualTo(PrivateLobbyRole.Host));
                Assert.That(root.ComposedPlan.Map,Is.EqualTo(OfficialBoardMapCatalog.AlternateSmall));
                Assert.That(root.ComposedPlan.Format.Id,Is.EqualTo("custom-6p-w7-l9"));
                Assert.That(root.Runtime.ReadModel.Setup.WaveCount,Is.EqualTo(7));
                Assert.That(root.Runtime.ReadModel.Setup.Teams.All(team=>team.LifeCount==9),Is.True);
                Assert.That(driver.View.Root.Q<DropdownField>("host-map-selector").enabledSelf,Is.False);
                Assert.That(driver.View.Root.Q<Label>("lobby-selected-map").text,Does.Contain("Alternate Small Map"));
                driver.Presenter.Press(PrivateLobbyControl.Leave,null);yield return null;yield return null;
                Object.Destroy(owner);yield return null;
                foreach(var width in new[]{1280u,1920u})
                {
                    UiPlayer.SetPlayerViewport(width,width*9/16,"alternate map artwork");
                    foreach(var format in new[]{MatchFormat.TwoVersusTwoQuick,MatchFormat.ThreeVersusThreeQuick})
                    {
                        root.Recompose(MatchCompositionPlan.Printed(format,OfficialBoardMapCatalog.AlternateSmall));
                        yield return null;yield return null;root.Hud.Layout();yield return null;yield return null;
                        var map=root.Runtime.ReadModel.SelectedMap;
                        var grid=Object.FindAnyObjectByType<PlaceholderTestGridView>();
                        Assert.That(grid.IsUnderlayActive,Is.True,"The approved private image must be imported for the visual gate.");
                        var tiles=grid.GetComponentsInChildren<HexTileView>();
                        Assert.That(tiles.Length,Is.EqualTo(258));
                        foreach (var zoneId in new[] { "top-jungle", "bottom-jungle" })
                        {
                            var sideTiles = tiles.Where(t => t.ZoneId.HasValue && t.ZoneId.Value.Value == zoneId).ToArray();
                            Assert.That(sideTiles.Length, Is.EqualTo(15), "Entire blue region must be rendered as a zone.");
                            Assert.That(sideTiles.All(t => t.IsPlayable), Is.True);
                        }
                        foreach(var spawn in map.SpawnPoints)
                        {
                            var coordinate=map.Spaces.Single(s=>s.Id.Equals(spawn.SpaceId)).Coordinate;
                            var pixel=new Vector2(453.4f+42.04f*coordinate.Q+29.49f*coordinate.R,424.4f-9.78f*coordinate.Q+31.52f*coordinate.R);
                            var tile=tiles.Single(t=>t.Coordinate.Equals(coordinate));
                            Assert.That(Vector3.Distance(tile.transform.localPosition,BoardArtworkCatalog.PixelToWorld(pixel,tile.OuterRadius)),Is.LessThan(.001f));
                        }
                        Assert.That(root.Runtime.ReadModel.Setup.Heroes.Count,Is.EqualTo(format.PlayerCount));
                        yield return UiPlayer.CaptureScreen("tmp/961-map-"+format.Id+"-"+width+".png");
                    }
                }
                root.Recompose(MatchCompositionPlan.Printed(MatchFormat.Default));yield return null;yield return null;
                Assert.That(root.Runtime.ReadModel.SelectedMap.Reference,Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
                Assert.That(Object.FindAnyObjectByType<PlaceholderTestGridView>().GetComponentsInChildren<HexTileView>().Length,Is.EqualTo(292));
            }
            finally
            {
                MatchSceneCompositionRoot.NextComposedMap=null;
                PrivateLobbyDriver.FieldMemoryOverride=null;PrivateLobbyDriver.SuppressRouterQuestion=false;
                UiPlayer.SetPlayerViewport(oldWidth,oldHeight,"restore map test viewport");
            }
        }
    }
}
