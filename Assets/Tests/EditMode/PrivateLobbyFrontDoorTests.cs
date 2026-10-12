using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.107. The engine-free half of the lobby's new front door: what a pasted field
    /// means, the session ID a host is handed, what is remembered between launches, the lines a
    /// host reads out to its guests, the router's three answers, and the non-playing host's screen.
    /// </summary>
    public sealed class PrivateLobbyFrontDoorTests
    {
        [TestCase(" 45677 ", "45677")]
        [TestCase("45677 ", "45677")]
        [TestCase("\t45677\r\n", "45677")]
        [TestCase("﻿45677", "45677")]
        [TestCase("​127.0.0.1​", "127.0.0.1")]
        [TestCase("192. 168 .1.20", "192.168.1.20")]
        [TestCase(" 203.0.113.7 ", "203.0.113.7")]
        [TestCase("", "")]
        [TestCase(null, "")]
        public void APastedAddressOrPort_LosesEveryBlankAndInvisibleCharacter(string typed, string read)
        {
            Assert.That(PrivateLobbyEntryText.CleanEndpointPart(typed), Is.EqualTo(read));
        }

        [TestCase("  K7QM-4TZP-9WXR \n", "K7QM-4TZP-9WXR")]
        [TestCase("​K7QM ", "K7QM")]
        [TestCase("our table key", "our table key")]
        [TestCase(" \t ", "")]
        [TestCase(null, "")]
        public void APastedSessionId_LosesOnlyItsBlankEnds(string typed, string read)
        {
            Assert.That(PrivateLobbyEntryText.CleanSessionId(typed), Is.EqualTo(read));
        }

        [Test]
        public void AGeneratedSessionId_IsThreeGroupsOfFourUnambiguousCharacters()
        {
            var fixedBytes = Enumerable.Range(0, 12).Select(value => (byte)value).ToArray();
            Assert.That(PrivateLobbyEntryText.SessionIdFrom(fixedBytes), Is.EqualTo("ABCD-EFGH-JKMN"));

            var shape = new Regex("^[" + PrivateLobbyEntryText.SessionIdAlphabet + "]{4}(-[" +
                PrivateLobbyEntryText.SessionIdAlphabet + "]{4}){2}$");
            var generated = Enumerable.Range(0, 20).Select(_ => PrivateSessionIdSource.NewSessionId()).ToArray();
            Assert.That(generated.All(value => shape.IsMatch(value)), Is.True, string.Join(", ", generated));
            Assert.That(generated.Distinct().Count(), Is.EqualTo(generated.Length),
                "Every launch gets its own session ID.");
            Assert.That(PrivateLobbyEntryText.SessionIdAlphabet, Does.Not.Contain("0")
                .And.Not.Contain("O").And.Not.Contain("1").And.Not.Contain("I").And.Not.Contain("L"));
        }

        [Test]
        public void ANewMachine_OffersLoopbackTheProtocolPortAndNoSessionId()
        {
            var fields = PrivateLobbyRememberedFields.Load(
                new InMemoryPrivateLobbyFieldMemory(), "127.0.0.1", "45677");

            Assert.That(fields.JoinAddress, Is.EqualTo("127.0.0.1"));
            Assert.That(fields.JoinPort, Is.EqualTo("45677"));
            Assert.That(fields.JoinSessionId, Is.Empty);
            Assert.That(fields.HostPort, Is.EqualTo("45677"));
        }

        [Test]
        public void AJoin_IsRememberedCleaned_AndAHost_RemembersItsPortButNeverItsSessionId()
        {
            var memory = new RecordingMemory();
            PrivateLobbyRememberedFields.RememberJoin(memory, " 81.2.69.160 ", "5000 ", " SECRET ");
            PrivateLobbyRememberedFields.RememberHost(memory, " 6000 ");

            var fields = PrivateLobbyRememberedFields.Load(memory, "127.0.0.1", "45677");
            Assert.That(fields.JoinAddress, Is.EqualTo("81.2.69.160"));
            Assert.That(fields.JoinPort, Is.EqualTo("5000"));
            Assert.That(fields.JoinSessionId, Is.EqualTo("SECRET"));
            Assert.That(fields.HostPort, Is.EqualTo("6000"));
            Assert.That(memory.Keys, Is.EquivalentTo(new[]
            {
                PrivateLobbyRememberedFields.JoinAddressKey,
                PrivateLobbyRememberedFields.JoinPortKey,
                PrivateLobbyRememberedFields.JoinSessionIdKey,
                PrivateLobbyRememberedFields.HostPortKey
            }), "The host's session ID has no key: it is fresh on every launch.");
        }

        [Test]
        public void TheHostsInvitation_NamesTheRoutersInternetIp_WhenTheRouterAnswers()
        {
            var searching = PrivateLobbyInvitation.For(
                "K7QM-4TZP-9WXR", 45677, HostAddressDiscovery.Searching, null, null);
            Assert.That(searching.SessionIdLine, Is.EqualTo("SESSION ID: K7QM-4TZP-9WXR"));
            Assert.That(searching.AddressLine, Does.StartWith("INTERNET IP: FINDING PUBLIC IP"));

            var answered = PrivateLobbyInvitation.For(
                "K7QM-4TZP-9WXR", 45677, HostAddressDiscovery.RouterAnswered, "81.2.69.160", "192.168.1.20");
            Assert.That(answered.AddressLine, Is.EqualTo("INTERNET IP: 81.2.69.160   PORT 45677"));

            var fallbackAnswered = PrivateLobbyInvitation.For(
                "K7QM-4TZP-9WXR", 45677, HostAddressDiscovery.PublicServiceAnswered,
                "81.2.69.161", "192.168.1.20");
            Assert.That(fallbackAnswered.AddressLine, Is.EqualTo("INTERNET IP: 81.2.69.161   PORT 45677"));
        }

        [Test]
        public void TheHostsInvitation_FallsBackToTheLabelledLanIp_WhenTheRouterIsSilent()
        {
            var lan = PrivateLobbyInvitation.For(
                "K7QM", 5000, HostAddressDiscovery.RouterSilent, null, "192.168.1.20");
            Assert.That(lan.AddressLine, Does.StartWith("LAN IP: 192.168.1.20   PORT 5000"));
            Assert.That(lan.AddressLine, Does.Contain("public IP lookup failed"));

            var nothing = PrivateLobbyInvitation.For(
                "K7QM", 5000, HostAddressDiscovery.RouterSilent, null, string.Empty);
            Assert.That(nothing.AddressLine, Does.StartWith("INTERNET IP: UNKNOWN"));
        }

        [Test]
        public void AnUnkeyedHost_IsToldItIsReachableFromThisComputerOnly()
        {
            var local = PrivateLobbyInvitation.For(
                string.Empty, 45677, HostAddressDiscovery.NotStarted, null, "192.168.1.20");

            Assert.That(local.SessionIdLine, Is.EqualTo("SESSION ID: NONE"));
            Assert.That(local.AddressLine, Does.StartWith("THIS COMPUTER ONLY (127.0.0.1)"));
            Assert.That(PrivateLobbyInvitation.None.IsShown, Is.False);
        }

        [Test]
        public void AnSsdpReply_NamesTheGatewaysDescription_OnlyOnAPrivateHttpHost()
        {
            const string Reply = "HTTP/1.1 200 OK\r\nCACHE-CONTROL: max-age=120\r\n" +
                "Location: http://192.168.1.1:5000/rootDesc.xml\r\nST: " +
                RouterAddressParsing.GatewaySearchTarget + "\r\n\r\n";
            Assert.That(RouterAddressParsing.TryReadSsdpLocation(Reply, out var location), Is.True);
            Assert.That(location.ToString(), Is.EqualTo("http://192.168.1.1:5000/rootDesc.xml"));

            Assert.That(RouterAddressParsing.TryReadSsdpLocation(
                "HTTP/1.1 200 OK\r\nLOCATION: http://81.2.69.160/desc.xml\r\n\r\n", out _), Is.False,
                "A reply may come from anything on the LAN; it is never followed off the private network.");
            Assert.That(RouterAddressParsing.TryReadSsdpLocation(
                "HTTP/1.1 200 OK\r\nLOCATION: file:///etc/passwd\r\n\r\n", out _), Is.False);
            Assert.That(RouterAddressParsing.TryReadSsdpLocation("HTTP/1.1 200 OK\r\n\r\n", out _), Is.False);
        }

        [Test]
        public void ADeviceDescription_NamesTheWanServicesControlUrl_ResolvedAgainstItsBase()
        {
            const string Description =
                "<?xml version=\"1.0\"?><root xmlns=\"urn:schemas-upnp-org:device-1-0\">" +
                "<URLBase>http://192.168.1.1:5000/</URLBase><device><serviceList>" +
                "<service><serviceType>urn:schemas-upnp-org:service:Layer3Forwarding:1</serviceType>" +
                "<controlURL>/ctl/L3F</controlURL></service>" +
                "<service><serviceType>urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>" +
                "<controlURL>/ctl/IPConn</controlURL></service>" +
                "</serviceList></device></root>";

            Assert.That(RouterAddressParsing.TryFindWanControlUrl(
                Description, new Uri("http://192.168.1.1:5000/rootDesc.xml"),
                out var control, out var serviceType), Is.True);
            Assert.That(control.ToString(), Is.EqualTo("http://192.168.1.1:5000/ctl/IPConn"));
            Assert.That(serviceType, Is.EqualTo("urn:schemas-upnp-org:service:WANIPConnection:1"));
        }

        [Test]
        public void ADeviceDescription_WithADtdOrAPublicControlHost_IsNotFollowed()
        {
            const string WithDtd = "<?xml version=\"1.0\"?><!DOCTYPE root [<!ENTITY x \"y\">]>" +
                "<root><service><serviceType>urn:schemas-upnp-org:service:WANIPConnection:1" +
                "</serviceType><controlURL>/ctl</controlURL></service></root>";
            Assert.That(RouterAddressParsing.TryFindWanControlUrl(
                WithDtd, new Uri("http://192.168.1.1/desc.xml"), out _, out _), Is.False);

            const string PublicControl = "<root><service><serviceType>" +
                "urn:schemas-upnp-org:service:WANIPConnection:1</serviceType>" +
                "<controlURL>http://81.2.69.160/ctl</controlURL></service></root>";
            Assert.That(RouterAddressParsing.TryFindWanControlUrl(
                PublicControl, new Uri("http://192.168.1.1/desc.xml"), out _, out _), Is.False);
        }

        [Test]
        public void TheRoutersAnswer_IsShownOnlyWhenItIsAPublicIPv4()
        {
            string Answer(string value) =>
                "<s:Envelope><s:Body><u:GetExternalIPAddressResponse>" +
                "<NewExternalIPAddress>" + value + "</NewExternalIPAddress>" +
                "</u:GetExternalIPAddressResponse></s:Body></s:Envelope>";

            Assert.That(RouterAddressParsing.TryReadExternalAddress(Answer("81.2.69.160"), out var address), Is.True);
            Assert.That(address, Is.EqualTo("81.2.69.160"));

            foreach (var inner in new[] { "192.168.0.2", "10.1.2.3", "100.64.0.1", "172.20.0.1", "0.0.0.0", "" })
            {
                Assert.That(RouterAddressParsing.TryReadExternalAddress(Answer(inner), out _), Is.False,
                    inner + " is not an address somebody on the internet can dial.");
            }

            Assert.That(RouterAddressParsing.TryReadExternalAddress(Answer("<script>"), out _), Is.False);
            Assert.That(RouterAddressParsing.TryReadExternalAddress("<html/>", out _), Is.False);
        }

        [Test]
        public void TheHttpsAnswer_IsShownOnlyWhenItIsExactlyOnePublicIPv4()
        {
            Assert.That(RouterAddressParsing.TryReadPublicAddressText(" 81.2.69.160\r\n", out var address), Is.True);
            Assert.That(address, Is.EqualTo("81.2.69.160"));

            foreach (var answer in new[]
                     {
                         "192.168.0.2", "100.64.0.1", "81.2.69.160 extra", "<html>81.2.69.160</html>", ""
                     })
            {
                Assert.That(RouterAddressParsing.TryReadPublicAddressText(answer, out _), Is.False, answer);
            }
        }

        [Test]
        public void AddressDiscovery_FallsBackToHttps_WhenTheRouterCannotNameThePublicIp()
        {
            var probe = new RouterExternalAddressProbe(
                _ => null,
                _ => "81.2.69.160");

            probe.Start(TimeSpan.FromSeconds(1));

            Assert.That(SpinWait.SpinUntil(
                () => probe.Discovery != HostAddressDiscovery.Searching,
                TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(probe.Discovery, Is.EqualTo(HostAddressDiscovery.PublicServiceAnswered));
            Assert.That(probe.InternetAddress, Is.EqualTo("81.2.69.160"));
        }

        [Test]
        public void AHostWhoTookNoSeat_HidesTheLobbyForTheLiveMatchHud()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            var hostClient = harness.ConnectSeatless(7);
            harness.Pump();
            var seats = harness.Session.Roster.Select(entry => entry.Seat.Value).ToArray();
            for (var index = 0; index < seats.Length; index++)
            {
                harness.Connect(index + 1, seats[index]);
            }

            harness.Pump();

            var beforeStart = PrivateLobbyModel.ForHostPlayer(harness.Session, hostClient, string.Empty);
            Assert.That(beforeStart.IsHostConsole, Is.False);
            Assert.That(beforeStart.Offers(PrivateLobbyControl.DirectorUndo), Is.False,
                "Before the match there is nothing for the Director to rewind.");

            StartTheMatch(harness, seats.Length);
            Assert.That(hostClient.State, Is.EqualTo(PrivateSessionClientState.Admitted),
                "The hosting client never took a seat.");
            Assert.That(hostClient.HasMatchStarted, Is.False,
                "It is never told the match began, which is why the host's own fact decides.");

            var observer = PrivateLobbyModel.ForHostPlayer(harness.Session, hostClient, string.Empty);
            Assert.That(observer.IsHostConsole, Is.False);
            Assert.That(observer.HasMatchStarted, Is.True);
            Assert.That(observer.IsShown, Is.False,
                "The host sees the retained local match HUD, not a full-screen lobby console.");
            Assert.That(observer.Controls, Is.Empty);
            Assert.That(observer.Selection.IsRunning, Is.False);
            Assert.That(observer.Seats.Count, Is.EqualTo(seats.Length));
            Assert.That(harness.Session.ClaimedSeatCount, Is.EqualTo(seats.Length));
            Assert.That(hostClient.GrantedSeat, Is.Null,
                "Watching the local authority neither claims nor transfers a player seat.");
        }

        [Test]
        public void AHostWhoIsPlaying_NeverGetsTheConsole_BecauseItsMatchHudCarriesTheDrawer()
        {
            var harness = PrivateOnlineWireHarness.Compose();
            harness.SeatAll();
            harness.ReadyAndPickEverySeat();

            var playing = PrivateLobbyModel.ForHostPlayer(harness.Session, harness.Client(1), string.Empty);

            Assert.That(playing.IsHostConsole, Is.False);
            Assert.That(playing.Offers(PrivateLobbyControl.DirectorUndo), Is.False);
            Assert.That(playing.Offers(PrivateLobbyControl.DirectorRestart), Is.False);
        }

        [Test]
        public void TheDirectorsLobbyControls_HaveTheirOwnWords()
        {
            Assert.That(PrivateLobbyLabels.Control(PrivateLobbyControl.DirectorUndo), Is.EqualTo("DIRECTOR UNDO"));
            Assert.That(PrivateLobbyLabels.Control(PrivateLobbyControl.DirectorRestart), Is.EqualTo("DIRECTOR RESTART"));
        }

        private static void StartTheMatch(PrivateOnlineWireHarness harness, int seatCount)
        {
            harness.Session.Composition = new NoComposition();
            for (var ordinal = 1; ordinal <= seatCount; ordinal++)
            {
                Assert.That(harness.Client(ordinal).TrySetReadiness(true), Is.True);
                harness.Pump();
            }

            var guard = 0;
            while (!harness.Session.HasMatchStarted && guard++ <= seatCount)
            {
                var picking = harness.Session.Selection.PickingSeat;
                var ordinal = Enumerable.Range(1, seatCount)
                    .First(candidate => harness.SeatOf(candidate) == picking.Value);
                var client = harness.Client(ordinal);
                var offered = PrivateLobbyModel.ForClient(client).Selection.Heroes
                    .First(option => option.IsAvailable);
                Assert.That(client.TryChooseHero(offered.HeroIdValue), Is.True);
                harness.Pump();
            }

            Assert.That(harness.Session.HasMatchStarted, Is.True);
        }

        private sealed class NoComposition : IDeferredMatchComposition
        {
            public IMatchHost ComposeForLineup(GuardsDigital.Core.Setup.HeroLineup lineup) => null;
        }

        private sealed class RecordingMemory : IPrivateLobbyFieldMemory
        {
            private readonly Dictionary<string, string> values = new Dictionary<string, string>();

            public IEnumerable<string> Keys => values.Keys;

            public string Read(string key) => values.TryGetValue(key, out var value) ? value : null;

            public void Write(string key, string value) => values[key] = value;
        }

        [Test]
        public void TimerDurationsRoundTripAndInvalidStoredValuesFallBack()
        {
            var memory=new InMemoryPrivateLobbyFieldMemory();
            PrivateLobbyRememberedFields.RememberTimerSettings(memory,
                new ExperimentalMatchOptions(cardCommitmentSeconds:91,cardResolutionSeconds:22,
                    responseSeconds:17,cardUpgradeSeconds:83,teamBankSeconds:0));
            var fields=PrivateLobbyRememberedFields.Load(memory,"127.0.0.1","45677");
            Assert.That(new[]{fields.CardCommitmentSeconds,fields.CardResolutionSeconds,
                fields.ResponseSeconds,fields.CardUpgradeSeconds,fields.TeamBankSeconds},
                Is.EqualTo(new[]{91,22,17,83,0}));

            memory.Write(PrivateLobbyRememberedFields.ResponseSecondsKey,"0");
            memory.Write(PrivateLobbyRememberedFields.TeamBankSecondsKey,"86401");
            fields=PrivateLobbyRememberedFields.Load(memory,"127.0.0.1","45677");
            Assert.That(fields.ResponseSeconds,
                Is.EqualTo(PrivateLobbyRememberedFields.DefaultResponseSeconds));
            Assert.That(fields.TeamBankSeconds,
                Is.EqualTo(PrivateLobbyRememberedFields.DefaultTeamBankSeconds));
        }
    }
}
