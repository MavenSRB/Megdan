using System;
using System.IO;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Pve.Composition;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    /// <summary>
    /// W-021, PVE-D01. The PvE rules assembly stays engine-free, and no existing assembly references
    /// any PvE assembly: PvE depends on the existing code, never the other way round.
    /// </summary>
    public sealed class PveAssemblyBoundaryTests
    {
        [Test]
        public void ThePveRulesAssemblyReferencesNoUnityAssembly()
        {
            var references = typeof(PveEncounterComposer).Assembly.GetReferencedAssemblies()
                .Select(reference => reference.Name);

            Assert.That(ApplicationAssemblyDependencyPolicy.FindForbiddenReferences(references), Is.Empty);
        }

        [Test]
        public void NoExistingAssemblyDefinitionReferencesAPveAssembly()
        {
            var assets = Path.Combine(RepositoryRoot(), "Assets");
            var existing = Directory.GetFiles(assets, "*.asmdef", SearchOption.AllDirectories)
                .Where(path => !path.Replace('\\', '/').Contains("/GuardsPve/") &&
                    !path.Replace('\\', '/').Contains("/Tests/Pve"))
                .ToArray();

            Assert.That(existing, Is.Not.Empty);
            foreach (var path in existing)
            {
                Assert.That(File.ReadAllText(path), Does.Not.Contain("GuardsDigital.Pve"),
                    Path.GetFileName(path) + " must not reference a PvE assembly.");
            }
        }

        [Test]
        public void EveryPveAssemblyCompilesOnlyForTheEditorOrThePveBuild()
        {
            var pve = Directory.GetFiles(Path.Combine(RepositoryRoot(), "Assets"), "*.asmdef", SearchOption.AllDirectories)
                .Where(path => File.ReadAllText(path).Contains("\"name\": \"GuardsDigital.Pve"))
                .ToArray();

            Assert.That(pve.Length, Is.GreaterThanOrEqualTo(4));
            foreach (var path in pve)
            {
                var text = File.ReadAllText(path);
                var editorOnly = text.Contains("\"Editor\"");
                Assert.That(editorOnly || text.Contains("UNITY_EDITOR || GUARDS_PVE"), Is.True,
                    Path.GetFileName(path) + " would otherwise compile into the existing player.");
                Assert.That(text, Does.Contain("\"autoReferenced\": false"), Path.GetFileName(path));
            }
        }

        private static string RepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Assets", "GuardsPve")))
            {
                directory = directory.Parent;
            }

            if (directory == null)
            {
                directory = new DirectoryInfo(Directory.GetCurrentDirectory());
                while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, "Assets", "GuardsPve")))
                {
                    directory = directory.Parent;
                }
            }

            Assert.That(directory, Is.Not.Null, "The repository root holds Assets/GuardsPve.");
            return directory.FullName;
        }
    }
}
