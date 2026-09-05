using GuardsDigital.Application;
using GuardsDigital.Core.Setup;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. The private-online command line: a role flag selects host or client, a
    /// malformed line is a typed refusal, and the host binds loopback unless an operator says
    /// otherwise.
    /// </summary>
    public sealed class PrivateSessionLaunchOptionsTests
    {
        [Test]
        public void NoFlag_MeansNoRole()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[] { "-guardsSmokeTest" });

            Assert.That(options.IsValid, Is.True);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.None));
        }

        [Test]
        public void AHost_DefaultsToLoopbackAndTheProtocolPort()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[] { "-guardsPrivateHost" });

            Assert.That(options.IsValid, Is.True);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.Host));
            Assert.That(options.Endpoint.IsLoopback, Is.True,
                "A public listener is an explicit operator decision, never a default.");
            Assert.That(options.Endpoint.Port, Is.EqualTo(PrivateSessionProtocol.DefaultPort));
            Assert.That(options.TimeoutSeconds,
                Is.EqualTo(PrivateSessionLaunchOptions.DefaultTimeoutSeconds));
        }

        [Test]
        public void AHost_TakesAnExplicitBindPortKeyAndTimeout()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsBind", "0.0.0.0", "-guardsPort", "50123",
                "-guardsSessionKey", "attic", "-guardsOnlineTimeoutSeconds", "45"
            });

            Assert.That(options.IsValid, Is.True);
            Assert.That(options.Endpoint.IsLoopback, Is.False);
            Assert.That(options.Endpoint.Port, Is.EqualTo(50123));
            Assert.That(options.SessionKey, Is.EqualTo("attic"));
            Assert.That(options.TimeoutSeconds, Is.EqualTo(45));
        }

        [Test]
        public void AClient_RequiresAddressAndSeat()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsAddress", "127.0.0.1:50123",
                "-guardsSeat", "red-player-01"
            });

            Assert.That(options.IsValid, Is.True);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.Client));
            Assert.That(options.Endpoint.Port, Is.EqualTo(50123));
            Assert.That(options.Seat.Value, Is.EqualTo("red-player-01"));

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsSeat", "red-player-01"
            }).Error, Does.Contain("-guardsAddress"));

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsAddress", "127.0.0.1:50123"
            }).Error, Does.Contain("-guardsSeat"));
        }

        [Test]
        public void TheRoles_AreMutuallyExclusive()
        {
            var options = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsPrivateClient"
            });

            Assert.That(options.IsValid, Is.False);
            Assert.That(options.Role, Is.EqualTo(PrivateSessionRole.None));
        }

        [Test]
        public void ARoleRefusesTheOtherRolesFlags()
        {
            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsAddress", "127.0.0.1:50123"
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsAddress", "127.0.0.1:50123",
                "-guardsSeat", "red-player-01", "-guardsPort", "50123"
            }).IsValid, Is.False);
        }

        [Test]
        public void MalformedValues_AreTypedRefusals()
        {
            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsPort", "0"
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsPort", "70000"
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsBind", "not-an-ip"
            }).IsValid, Is.False);

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsAddress", "example.test:50123",
                "-guardsSeat", "red-player-01"
            }).IsValid, Is.False, "A hostname is refused; what is dialed is what was typed.");

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", "-guardsOnlineTimeoutSeconds", "-guardsSessionKey"
            }).IsValid, Is.False, "A flag is not a value for the flag before it.");
        }

        [Test]
        public void EndpointParsing_HandlesTheIpPortShapes()
        {
            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:1", out var loopback, out _), Is.True);
            Assert.That(loopback.IsLoopback, Is.True);

            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1", out _, out var noPort), Is.False);
            Assert.That(noPort, Is.Not.Empty);

            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:0", out _, out _), Is.False);
            Assert.That(PrivateSessionEndpoint.TryParseAddress(":50123", out _, out _), Is.False);
            Assert.That(PrivateSessionEndpoint.TryParseAddress("127.0.0.1:", out _, out _), Is.False);
        }

        [Test]
        public void NoFormatFlag_ComposesTheSixPlayerQuickGameEveryExistingLaunchLineComposed()
        {
            foreach (var line in new[]
            {
                new[] { "-guardsSmokeTest" },
                new[] { "-guardsPrivateHost" },
                new[] { "-guardsPrivateLobby" },
                new[] { "-guardsPrivateClient", "-guardsAddress", "127.0.0.1:45677",
                    "-guardsSeat", "red-player-01" }
            })
            {
                var options = PrivateSessionLaunchOptions.Parse(line);
                Assert.That(options.IsValid, Is.True, string.Join(" ", line));
                Assert.That(options.MatchFormat, Is.SameAs(MatchFormat.Default),
                    "An absent flag reads as the format milestone 8.25 made the default.");
            }
        }

        [Test]
        public void TheFormatFlag_IsAcceptedBesideEveryRoleIncludingTheLobby()
        {
            var host = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", MatchFormatFlagName, "2v2-long"
            });
            Assert.That(host.IsValid, Is.True);
            Assert.That(host.MatchFormat, Is.SameAs(MatchFormat.TwoVersusTwoLong));

            // The lobby refuses an address, bind, port, seat or key because those are typed into
            // it; the format is not an invitation but the match this process composes, and until
            // the lobby grows a control of its own this flag is the only way to ask for one.
            var lobby = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateLobby", MatchFormatFlagName, "2v2-quick"
            });
            Assert.That(lobby.IsValid, Is.True);
            Assert.That(lobby.Role, Is.EqualTo(PrivateSessionRole.Lobby));
            Assert.That(lobby.MatchFormat, Is.SameAs(MatchFormat.TwoVersusTwoQuick));

            var client = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateClient", "-guardsAddress", "127.0.0.1:45677",
                "-guardsSeat", "red-player-01", MatchFormatFlagName, "2v2-quick"
            });
            Assert.That(client.IsValid, Is.True);
            Assert.That(client.MatchFormat, Is.SameAs(MatchFormat.TwoVersusTwoQuick));
        }

        [Test]
        public void AnUnprintedFormat_IsRefusedAndTheRefusalListsWhatIsPrinted()
        {
            var refused = PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", MatchFormatFlagName, "1v1-quick"
            });

            Assert.That(refused.IsValid, Is.False);
            Assert.That(refused.Error, Does.Contain("1v1-quick"));
            foreach (var format in MatchFormat.All)
            {
                Assert.That(refused.Error, Does.Contain(format.Id));
            }
        }

        [Test]
        public void TheFormatFlag_RequiresAValueAndDoesNotSwallowTheNextFlag()
        {
            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", MatchFormatFlagName
            }).IsValid, Is.False, "A trailing flag has no value.");

            Assert.That(PrivateSessionLaunchOptions.Parse(new[]
            {
                "-guardsPrivateHost", MatchFormatFlagName, "-guardsSessionKey"
            }).IsValid, Is.False, "A flag is not a value for the flag before it.");
        }

        private const string MatchFormatFlagName = PrivateSessionLaunchOptions.MatchFormatFlag;
    }
}
