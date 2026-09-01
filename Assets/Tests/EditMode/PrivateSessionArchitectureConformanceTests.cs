using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.31. The amended record's removal-path rule, made mechanical: every socket type
    /// stays inside the adapter assembly beneath the transport-neutral port, and no production
    /// source anywhere can name a connection's remote endpoint.
    /// </summary>
    /// <remarks>
    /// Source-text scans, the same technique the online conformance fixture uses for authority
    /// construction: they run identically in the Unity Editor and the cross-platform harness,
    /// where assembly identity would not (the harness compiles everything into one assembly, so
    /// the assembly-level halves of this rule live in the Unity-only boundary fixture).
    /// </remarks>
    public sealed class PrivateSessionArchitectureConformanceTests
    {
        [Test]
        public void SocketTypes_LiveOnlyInTheNetworkAdapter()
        {
            var production = ProductionSourceRoot();
            var adapterBoundary = Path.Combine(production, "Network");
            var offenders = Directory
                .GetFiles(production, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("System.Net"))
                .Where(path => !path.StartsWith(adapterBoundary, StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Substring(production.Length).TrimStart('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "A production source outside Assets/GuardsDigital/Network names System.Net. The " +
                "amended architecture record keeps every socket type in the adapter assembly " +
                "beneath the transport-neutral port, so that deleting the transport deletes one " +
                "assembly and nothing above it.");
        }

        [Test]
        public void TheAdapter_DoesUseSockets()
        {
            var adapterBoundary = Path.Combine(ProductionSourceRoot(), "Network");
            var sites = Directory
                .GetFiles(adapterBoundary, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("System.Net.Sockets"))
                .ToArray();

            Assert.That(sites, Is.Not.Empty,
                "A rule confining sockets to the adapter is satisfied vacuously by an adapter " +
                "with no sockets; the confinement must be shown to confine something.");
        }

        [Test]
        public void NoProductionSource_NamesAConnectionsRemoteEndpoint()
        {
            var production = ProductionSourceRoot();
            var offenders = Directory
                .GetFiles(production, "*.cs", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("RemoteEndPoint"))
                .Select(path => path.Substring(production.Length).TrimStart('\\', '/'))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "A production source reads a connection's remote endpoint. The amended record " +
                "names connections by opaque ordinals everywhere - an address read anywhere is one " +
                "log statement away from the privacy leak the diagnostics allow-list forbids.");
        }

        [Test]
        public void TheSessionLayer_NamesNoSocketAndNoUnityType()
        {
            var sessionBoundary = Path.Combine(ProductionSourceRoot(), "Application", "PrivateOnline");
            var offenders = Directory
                .GetFiles(sessionBoundary, "*.cs", SearchOption.AllDirectories)
                .Where(path =>
                {
                    var text = File.ReadAllText(path);
                    return text.Contains("System.Net") || text.Contains("UnityEngine");
                })
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.That(offenders, Is.Empty,
                "The transport-neutral session layer names a socket or engine type. It exists so " +
                "the adapter beneath it is the only place either can live.");
        }

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
