using System.Linq;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class DestinationHighlightMaterialOwnershipTests
    {
        [Test]
        public void GeneratedOwner_ReusesOneMaterialAndReturnsExactCountToBaseline()
        {
            var baseline = CountGeneratedMaterials();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                using (var owner = new DestinationHighlightMaterialOwner())
                {
                    var first = owner.Material;
                    Assert.That(owner.Material, Is.SameAs(first));
                    Assert.That(owner.IsGenerated, Is.True);
                    Assert.That(owner.OwnedNativeResourceCount, Is.EqualTo(1));
                    Assert.That(CountGeneratedMaterials(), Is.EqualTo(baseline + 1));
                }

                Assert.That(CountGeneratedMaterials(), Is.EqualTo(baseline));
            }
        }

        [Test]
        public void AssignedOwner_DisposesWithoutDestroyingSavedOrInjectedMaterial()
        {
            var assigned = new Material(Shader.Find("Standard"))
            {
                name = "Assigned Destination Highlight"
            };
            try
            {
                var owner = new DestinationHighlightMaterialOwner(assigned);
                Assert.That(owner.Material, Is.SameAs(assigned));
                Assert.That(owner.IsGenerated, Is.False);
                owner.Dispose();

                Assert.That(assigned, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(assigned);
            }
        }

        // Milestone 6.1c removed LegacyUnconfiguredDrill_DestroysOnlyItsGeneratedHighlightAtTeardown
        // with the drill that hosted it. Its subject - a generated highlight material being destroyed
        // exactly once at teardown and leaving an assigned one alone - is covered by the two tests
        // above, which construct DestinationHighlightMaterialOwner directly, and by
        // ConfiguredMatchCutoverArchitectureTests.HighlightOwnerSource_RequiresGeneratedFlagReuseAndOwnedOnlyTeardown.

        private static int CountGeneratedMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);
    }
}
