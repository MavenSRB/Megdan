using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Replay;
using GuardsDigital.Pve.Composition;
using GuardsDigital.Pve.Play;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveCoopWireTests
    {
        private sealed class Nonces : IPrivateSessionNonceSource
        {
            private long value;
            public Nonces(long start) { value = start; }
            public byte[] NextNonce() => BitConverter.GetBytes(++value).Concat(BitConverter.GetBytes(value)).ToArray();
        }
        private sealed class HostOutbox : IPrivateSessionOutbox
        {
            public Action<int, byte[]> SendBytes;
            public Action<int> CloseLink;
            public void Send(int ordinal, byte[] frame) => SendBytes(ordinal, frame);
            public void Close(int ordinal) => CloseLink(ordinal);
        }
        private sealed class Link : IPrivateSessionClientOutbox
        {
            public int Ordinal;
            public PrivateSessionClient Client;
            public PrivateSessionFrameAssembler ToHost = new PrivateSessionFrameAssembler();
            public PrivateSessionFrameAssembler ToClient = new PrivateSessionFrameAssembler();
            public Rig Rig;
            public void Send(byte[] bytes) => Rig.Queue.Enqueue(() =>
            {
                Assert.That(ToHost.Add(bytes, 0, bytes.Length), Is.True);
                while (ToHost.TryDequeue(out var frame)) Rig.Server.OnFrame(Ordinal, frame);
            });
            public void Close() { }
        }
        private sealed class Rig
        {
            public readonly Queue<Action> Queue = new Queue<Action>();
            public readonly List<Link> Links = new List<Link>();
            public PveCoopMatch Match = PveCoopMatchTests.Create();
            public PrivateSessionHost Server;
            public Rig()
            {
                var initial = Match.Encounter.InitialState;
                var identity = PrivateSessionIdentity.ForComposition(PveTestContent.OfficialMap(), initial.InitialConfiguration, initial.Setup, "pve-wire-test");
                var outbox = new HostOutbox();
                outbox.SendBytes = (ordinal, bytes) => Queue.Enqueue(() =>
                {
                    var link = Links.Single(value => value.Ordinal == ordinal);
                    Assert.That(link.ToClient.Add(bytes, 0, bytes.Length), Is.True);
                    while (link.ToClient.TryDequeue(out var frame)) link.Client.OnFrame(frame);
                });
                outbox.CloseLink = ordinal => Queue.Enqueue(() => Links.Single(value => value.Ordinal == ordinal).Client.OnDisconnected());
                Server = new PrivateSessionHost(Match.Host, identity, "test-key", outbox, new Nonces(1000));
                Server.SeatComposition = selection =>
                {
                    var choices = selection.PickOrder.Select(seat => { selection.TryGetChoice(seat.Seat, out var hero); return hero.HeroIdValue; });
                    Match = new PveCoopMatch(PveEncounterComposer.Compose(new PveEncounterSetup(choices, 42),
                        PveTestContent.Encounter(), PveTestContent.Catalog(), PveTestContent.OfficialMap()));
                    return Match.Host;
                };
                for (var i = 1; i <= 3; i++)
                {
                    var link = new Link { Ordinal = i, Rig = this };
                    link.Client = new PrivateSessionClient(identity, new SeatId("blue-player-0" + i), "test-key", link, new Nonces(i * 100));
                    Links.Add(link);
                    Server.OnConnected(i, 0);
                    link.Client.OnConnected();
                }
                Pump();
                foreach (var link in Links) Assert.That(link.Client.TrySetReadiness(true), Is.True);
                Pump();
                var chosen = new[] { "hero-wasp", "hero-arien", "hero-brogan" };
                for (var i = 0; i < 3; i++)
                { Assert.That(Links[i].Client.TryChooseHero(chosen[i]), Is.True); Pump(); }
                Assert.That(Server.HasMatchStarted, Is.True);
            }
            public void Pump()
            {
                var limit = 10000;
                while (Queue.Count > 0 && limit-- > 0) Queue.Dequeue()();
                Assert.That(Queue, Is.Empty);
            }
            public void Publish() { Server.RepublishProjections(); Pump(); }
        }

        [Test]
        public void ThreeKeyedClientsPickOwnHeroesPlayAndReconnectToTheSameSeat()
        {
            var rig = new Rig();
            foreach (var link in rig.Links)
            {
                Assert.That(link.Client.HasMatchStarted, Is.True);
                Assert.That(link.Client.Roster.Count, Is.EqualTo(3));
                Assert.That(link.Client.LatestProjection, Is.Not.Null);
                var hero = rig.Match.Host.Seats.Find(link.Client.TargetSeat).HeroId.Value;
                Assert.That(link.Client.LatestProjection.OwnCards.All(card => card.OwnerHeroId == hero), Is.True);
                Assert.That(link.Client.LatestProjection.PublicCards.All(card =>
                    rig.Match.Encounter.Enemies.Any(enemy => enemy.HeroId.Value == card.OwnerHeroId)), Is.True,
                    "The received wire projection must not carry another player's initial private hand.");
            }
            var playerMoves = 0;
            for (var step = 0; step < 180 && rig.Match.Session.Status == PveEncounterStatus.Ongoing; step++)
            {
                rig.Match.Session.RunAutomatic();
                rig.Publish();
                var answer = PvePassivePlayer.NextAnswer(rig.Match.Session);
                if (answer == null) break;
                var owner = rig.Links.FirstOrDefault(link => rig.Match.Host.ProjectionForSeat(link.Client.TargetSeat).AuthorizedCommands.Contains(answer));
                if (owner != null)
                {
                    Assert.That(owner.Client.TrySubmitCommand(answer, out _), Is.True);
                    rig.Pump();
                    Assert.That(owner.Client.LastSubmissionResult.Accepted, Is.True);
                    playerMoves++;
                }
                else
                {
                    Assert.That(rig.Match.Session.CurrentPrompt != null ||
                        PveCoopMatch.IsSharedChoice(rig.Match.Session.Projection), Is.True,
                        "A hero-owned command must never fall back to the host: " + answer.Kind);
                    var accepted = rig.Match.Session.Execute(answer);
                    Assert.That(accepted.Accepted, Is.True, accepted.Detail);
                    rig.Publish();
                }
                foreach (var link in rig.Links)
                    Assert.That(link.Client.LatestProjection.Version, Is.EqualTo(rig.Match.Host.Version));
            }
            Assert.That(playerMoves, Is.GreaterThan(6));
            var returning = rig.Links[1];
            var seat = returning.Client.TargetSeat;
            rig.Server.OnDisconnected(returning.Ordinal);
            returning.Client.OnDisconnected();
            rig.Pump();
            Assert.That(rig.Server.IsPaused, Is.True);
            Assert.That(returning.Client.BeginReconnect(), Is.True);
            returning.Ordinal = 20;
            returning.ToHost = new PrivateSessionFrameAssembler();
            returning.ToClient = new PrivateSessionFrameAssembler();
            rig.Server.OnConnected(20, 1);
            returning.Client.OnConnected();
            rig.Pump();
            Assert.That(returning.Client.TargetSeat, Is.EqualTo(seat));
            Assert.That(returning.Client.State, Is.EqualTo(PrivateSessionClientState.Seated));
            Assert.That(rig.Server.IsPaused, Is.False);
            Assert.That(returning.Client.LatestProjection.Version, Is.EqualTo(rig.Match.Host.Version));
            rig.Server.Shutdown();
            rig.Pump();
            Assert.That(rig.Server.HasEnded, Is.True);
            Assert.That(rig.Links.All(link => link.Client.State == PrivateSessionClientState.HostLost ||
                link.Client.State == PrivateSessionClientState.Disconnected), Is.True, "No client becomes a replacement host.");
            foreach (var link in rig.Links)
                Assert.That(link.Client.TrySubmitCommand(DirectorCommand.Pass(rig.Match.Encounter.Heroes[0]), out _), Is.False);
        }
    }
}
