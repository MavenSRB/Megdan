using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GuardsDigital.Application;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.30. The structural half of the transport-neutral boundary: not what the host
    /// does, but what nothing else is allowed to do around it.
    /// </summary>
    /// <remarks>
    /// The private-online decision record keeps every UGS and UTP type in Unity adapters above this
    /// line and rules authority below it. These checks are what make that a fact about the code
    /// rather than an intention recorded in a document.
    /// </remarks>
    public sealed class OnlineArchitectureConformanceTests
    {
        /// <summary>
        /// The namespaces the port is allowed to name. An allow-list rather than a list of banned
        /// prefixes: a transport type added at 8.31 fails this whether or not anyone predicted its
        /// namespace.
        /// </summary>
        private static readonly string[] AllowedNamespaces =
        {
            "System",
            "System.Collections.Generic",
            "GuardsDigital.Application"
        };

        private static readonly Type[] PortTypes =
        {
            typeof(IMatchHost),
            typeof(MatchHost),
            typeof(MatchSubmission),
            typeof(MatchSubmissionKind),
            typeof(MatchHostRejectionReason),
            typeof(MatchHostResult),
            typeof(MatchHostResultRecord),
            typeof(MatchHostResultStreamRead),
            typeof(LocalDirectorMatchClient),
            // Milestone 8.31: the transport-neutral session layer. Every socket type lives in the
            // GuardsDigital.Network adapter beneath these, so any of them naming one fails here.
            typeof(PrivateSessionProtocol),
            typeof(PrivateSessionIdentity),
            typeof(PrivateSessionContentDigest),
            typeof(PrivateSessionKeyProof),
            typeof(PrivateSessionHost),
            typeof(PrivateSessionClient),
            typeof(PrivateSessionClientState),
            typeof(PrivateSessionEventKind),
            typeof(PrivateSessionEventRecord),
            typeof(PrivateSessionFrameCodec),
            typeof(PrivateSessionFrameAssembler),
            typeof(SessionFrame),
            typeof(SessionMessageKind),
            typeof(SessionRejectionReason),
            typeof(SeatOccupancyState),
            typeof(SeatRosterEntry),
            typeof(SessionChallengeMessage),
            typeof(SessionHelloMessage),
            typeof(SessionWelcomeMessage),
            typeof(SessionRejectMessage),
            typeof(SeatClaimMessage),
            typeof(SeatGrantedMessage),
            typeof(SeatRosterMessage),
            typeof(IPrivateSessionOutbox),
            typeof(IPrivateSessionClientOutbox),
            typeof(IPrivateSessionNonceSource),
            // Milestone 8.32: the gameplay slice - the secure channel, the envelope and its three
            // messages, the command DTO codec, and the seat-projection wire record. All of it
            // stays beneath the transport-neutral port; a socket type in any of these fails here.
            typeof(PrivateSessionSecureChannel),
            typeof(NetworkEnvelope),
            typeof(NetworkSubmissionMessage),
            typeof(NetworkSubmissionResultMessage),
            typeof(NetworkSeatProjectionMessage),
            typeof(NetworkMessageCodec),
            typeof(NetworkCommandCodec),
            typeof(NetworkSeatProjectionCodec),
            typeof(NetworkSeatProjectionRecord),
            typeof(NetworkSelectedActorRecord),
            typeof(NetworkOutcomeRecord),
            typeof(NetworkDecisionOwnerRecord),
            typeof(NetworkLifeCounterRow),
            typeof(NetworkHeroRow),
            typeof(NetworkMinionRow),
            typeof(NetworkObstacleRow),
            typeof(NetworkTokenRow),
            typeof(NetworkMarkerRow),
            typeof(NetworkTurnSlotRow),
            typeof(NetworkTurnAssignmentRow),
            typeof(NetworkCommitmentRow),
            typeof(NetworkHandSummaryRow),
            typeof(NetworkCardRow),
            typeof(NetworkProgressionRow)
        };

        [Test]
        public void EveryAuthorityMutatingSessionMember_HasExactlyOneSubmissionKindOnThePort()
        {
            var mutating = new Dictionary<string, MatchSubmissionKind>(StringComparer.Ordinal)
            {
                { nameof(IMatchSession.Execute), MatchSubmissionKind.Command },
                { nameof(IMatchSession.Undo), MatchSubmissionKind.Undo },
                { nameof(IMatchSession.Restart), MatchSubmissionKind.Restart },
                { nameof(IMatchSession.RestoreSnapshot), MatchSubmissionKind.RestoreSnapshot }
            };
            var reading = new[] { nameof(IMatchSession.CaptureSnapshot) };

            var declared = typeof(IMatchSession)
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => !method.IsSpecialName)
                .Select(method => method.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.EqualTo(mutating.Keys.Concat(reading)
                    .OrderBy(name => name, StringComparer.Ordinal).ToArray()),
                "A session member with no door on the port is a second way into match authority, " +
                "which is exactly what this milestone removed. Give it a " +
                nameof(MatchSubmissionKind) + " member, or a reading member on " +
                nameof(IMatchHost) + ", before adding it here.");

            var kinds = Enum.GetValues(typeof(MatchSubmissionKind))
                .Cast<MatchSubmissionKind>()
                .Where(kind => kind != MatchSubmissionKind.None)
                .ToArray();
            Assert.That(kinds, Is.EquivalentTo(mutating.Values),
                "A submission kind naming no session member would be a door onto nothing.");

            foreach (var name in reading.Concat(new[] { nameof(IMatchSession.Projection) }))
            {
                Assert.That(typeof(IMatchHost).GetMember(name), Is.Not.Empty,
                    "The port must expose the session's reading member " + name + ", or a consumer " +
                    "needs the session as well as the port.");
            }
        }

        [Test]
        public void ThePortSurface_NamesNoUnityAndNoTransportType()
        {
            foreach (var type in PortTypes)
            {
                foreach (var referenced in ReferencedTypes(type))
                {
                    var space = referenced.Namespace ?? string.Empty;
                    var allowed = AllowedNamespaces.Contains(space, StringComparer.Ordinal) ||
                        space.StartsWith("GuardsDigital.Core", StringComparison.Ordinal);
                    Assert.That(allowed, Is.True,
                        type.Name + " names " + referenced.FullName + ". The port is transport- and " +
                        "engine-neutral: a UGS, Unity Transport, socket or UnityEngine type here " +
                        "would make the authority depend on the adapter that is supposed to depend " +
                        "on it.");
                }
            }
        }

        [Test]
        public void TheSubmissionPayload_CarriesNoCallerIdentity()
        {
            var declared = typeof(MatchSubmission)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(declared, Is.EqualTo(new[]
            {
                nameof(MatchSubmission.Command),
                nameof(MatchSubmission.Kind),
                nameof(MatchSubmission.SnapshotPayload)
            }), "A submission says what is being asked and nothing about who asked it. Milestone " +
                "8.30a binds a seat outside the payload, because the decision record refuses a " +
                "payload-claimed identity; a seat, player or connection property added here would " +
                "be that claim.");
        }

        [Test]
        public void TheLocalDirectorClient_HoldsThePortAndNoAuthority()
        {
            var fields = typeof(LocalDirectorMatchClient)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            Assert.That(fields.Select(field => field.FieldType), Is.EqualTo(new[] { typeof(IMatchHost) }),
                "The director's client is an adapter onto the port. A session or rules reference " +
                "here would be the reach-around this milestone exists to remove.");
            Assert.That(typeof(IMatchSession).IsAssignableFrom(typeof(LocalDirectorMatchClient)), Is.True);
        }

        /// <summary>
        /// Milestone 8.30's closing check. Before it, the configured scene runtime constructed the
        /// authority itself, so the local director adapter and a future network host would have been
        /// looking at two different objects.
        /// </summary>
        [Test]
        public void MatchAuthority_IsConstructedOnlyInsideTheApplicationBoundary()
        {
            var production = ProductionSourceRoot();
            var applicationBoundary = Path.Combine(production, "Application");
            var offenders = Directory
                .GetFiles(production, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("new MatchSession("))
                .Where(path => !path.StartsWith(applicationBoundary, StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Substring(production.Length).TrimStart('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "Production code outside the Application boundary constructs a match session " +
                "directly. Authority is composed by " + nameof(MatchHost) + "." +
                nameof(MatchHost.Compose) + " and reached through " + nameof(IMatchHost) + ", so " +
                "that the local director adapter and a network host submit to one executor.");
        }

        /// <summary>
        /// `RISK-ONLINE-001`'s standing obligation on this milestone: the presentation-only scene
        /// content override must be unreachable from the transport-neutral host composition. It is,
        /// structurally - the composition takes authoritative inputs only and the Application
        /// boundary cannot name a presentation type at all.
        /// </summary>
        [Test]
        public void TheHostComposition_CannotNameThePresentationOnlyContentOverride()
        {
            Assert.That(typeof(MatchHost).GetMethod(nameof(MatchHost.Compose))?.GetParameters()
                    .Select(parameter => parameter.ParameterType),
                Is.EqualTo(new[] { typeof(MatchSessionInitialState) }),
                "The host is composed from one authoritative initial state and nothing else.");

            var applicationBoundary = Path.Combine(ProductionSourceRoot(), "Application");
            var offenders = Directory
                .GetFiles(applicationBoundary, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var text = File.ReadAllText(path);
                    return text.Contains("SceneCardContent") ||
                        text.Contains("NextComposedCardContent");
                })
                .Select(path => Path.GetFileName(path))
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "The Application boundary names the Play Mode scene-content override. That override " +
                "is a presentation-only development affordance and must stay outside anything a " +
                "host composes.");
        }

        private static IEnumerable<Type> ReferencedTypes(Type type)
        {
            var found = new List<Type>();

            foreach (var constructor in type.GetConstructors(
                BindingFlags.Public | BindingFlags.Instance))
            {
                found.AddRange(constructor.GetParameters().Select(parameter => parameter.ParameterType));
            }

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                found.Add(method.ReturnType);
                found.AddRange(method.GetParameters().Select(parameter => parameter.ParameterType));
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                found.Add(property.PropertyType);
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                found.Add(field.FieldType);
            }

            return found
                .SelectMany(Unwrap)
                .Where(candidate => candidate != typeof(void) && !candidate.IsPrimitive)
                .Distinct();
        }

        private static IEnumerable<Type> Unwrap(Type type)
        {
            yield return type;
            if (!type.IsGenericType)
            {
                yield break;
            }

            foreach (var argument in type.GetGenericArguments())
            {
                foreach (var nested in Unwrap(argument))
                {
                    yield return nested;
                }
            }
        }

        /// <summary>
        /// The production source root, located by walking up from wherever the runner started. Both
        /// tiers run this fixture - Unity from the project directory, the cross-platform harness
        /// from its build output - so neither may assume the other's working directory.
        /// </summary>
        private static string ProductionSourceRoot()
        {
            var roots = new[]
            {
                Directory.GetCurrentDirectory(),
                TestContext.CurrentContext.TestDirectory
            };

            foreach (var root in roots)
            {
                var directory = new DirectoryInfo(root);
                while (directory != null)
                {
                    var candidate = Path.Combine(directory.FullName, "Assets", "GuardsDigital");
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }

                    directory = directory.Parent;
                }
            }

            Assert.Fail("Could not locate Assets/GuardsDigital from the runner's directory.");
            return null;
        }
    }
}
