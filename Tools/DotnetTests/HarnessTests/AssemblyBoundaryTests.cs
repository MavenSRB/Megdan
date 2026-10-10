using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Network;
using NUnit.Framework;

namespace GuardsDigital.Tests.Harness
{
    public sealed class AssemblyBoundaryTests
    {
        private static string RepositoryRoot
        {
            get
            {
                var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Assets", "GuardsDigital")))
                    directory = directory.Parent;
                return directory?.FullName ?? throw new InvalidOperationException("Cannot locate this harness's repository.");
            }
        }

        [Test]
        public void PublicConsumersUseSeparateProductionAssemblies()
        {
            Assert.That(typeof(CardInstance).Assembly.GetName().Name, Is.EqualTo("GuardsDigital.Core.CardState"));
            Assert.That(typeof(HexCoordinate).Assembly.GetName().Name, Is.EqualTo("GuardsDigital.Core"));
            Assert.That(typeof(MatchSession).Assembly.GetName().Name, Is.EqualTo("GuardsDigital.Application"));
            Assert.That(typeof(PrivateSessionEndpoint).Assembly.GetName().Name, Is.EqualTo("GuardsDigital.Network"));
            Assert.That(GetType().Assembly.GetName().Name, Is.EqualTo("GuardsDigital.EditModeTests"));
            Assert.That(typeof(HexCoordinate).Assembly, Is.Not.EqualTo(GetType().Assembly));
            Assert.That(new HexCoordinate(2, 3), Is.EqualTo(new HexCoordinate(2, 3)));
        }

        [TestCase("CardState", "Core/CardState", "GuardsDigital.Core.CardState")]
        [TestCase("Core", "Core", "GuardsDigital.Core")]
        [TestCase("Application", "Application", "GuardsDigital.Application")]
        [TestCase("Network", "Network", "GuardsDigital.Network")]
        [TestCase("Presentation", "Presentation", "GuardsDigital.Presentation")]
        [TestCase("Pve", "Rules", "GuardsDigital.Pve", "GuardsPve")]
        public void ProjectIdentityAndDependenciesMatchTheUnityBoundary(string folder, string source, string name, string root = "GuardsDigital")
        {
            var project = XDocument.Load(Path.Combine(RepositoryRoot, "Tools", "DotnetTests", "Assemblies", folder, name + ".csproj"));
            using var asmdef = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, "Assets", root, source, name + ".asmdef")));
            Assert.That(project.Descendants("AssemblyName").Single().Value, Is.EqualTo(asmdef.RootElement.GetProperty("name").GetString()));
            var actual = project.Descendants("ProjectReference").Select(element => Path.GetFileNameWithoutExtension(element.Attribute("Include").Value));
            var expected = asmdef.RootElement.GetProperty("references").EnumerateArray().Select(element => element.GetString())
                .Where(reference => reference.StartsWith("GuardsDigital.", StringComparison.Ordinal));
            Assert.That(actual, Is.EquivalentTo(expected));
            Assert.That(Assembly.Load(name).GetName().Name, Is.EqualTo(name));
        }

        [Test]
        public void UnityOnlyExclusionsAreExplicitUniqueExistingFiles()
        {
            var harness = Path.Combine(RepositoryRoot, "Tools", "DotnetTests");
            var paths = XDocument.Load(Path.Combine(harness, "UnityOnlyTests.props")).Descendants("UnityOnlyTest")
                .Select(element => element.Attribute("Include").Value).ToArray();
            Assert.That(paths, Is.Unique);
            foreach (var path in paths)
            {
                Assert.That(path, Does.Not.Contain("*"));
                Assert.That(File.Exists(Path.Combine(harness, path)), Is.True, path);
            }
            var tests = XDocument.Load(Path.Combine(harness, "GuardsDigital.DotnetTests.csproj")).Descendants("Compile")
                .Single(element => (string)element.Attribute("Include") == "../../Assets/Tests/EditMode/*.cs");
            Assert.That((string)tests.Attribute("Exclude"), Is.EqualTo("@(UnityOnlyTest)"));
        }
    }
}
