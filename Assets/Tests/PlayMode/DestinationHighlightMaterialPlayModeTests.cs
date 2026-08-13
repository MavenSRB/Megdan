using System.Collections;
using System.Linq;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 6.1k, finding F613-13. <see cref="DestinationHighlightMaterialOwner.Dispose"/> takes
    /// one of two branches: <c>Object.DestroyImmediate</c> in the Editor and <c>Object.Destroy</c>
    /// while playing. Every Edit Mode fixture takes the first branch by construction, so the second was
    /// covered only by an assertion that the string <c>UnityEngine.Object.Destroy(ownedMaterial)</c>
    /// appeared in the source file - which proves the call is written, not that it runs or that it
    /// releases anything.
    ///
    /// This is that branch, executed. `Object.Destroy` defers the actual destruction to the end of the
    /// frame, which is exactly why it needs a Play Mode test and a yielded frame to observe.
    /// </summary>
    public sealed class DestinationHighlightMaterialPlayModeTests
    {
        [UnityTest]
        public IEnumerator PlayingOwner_DestroysItsGeneratedMaterialAtEndOfFrame()
        {
            Assert.That(UnityEngine.Application.isPlaying, Is.True,
                "This fixture exists to take the playing branch; in the Editor it would prove nothing.");

            var baseline = CountGeneratedMaterials();
            var owner = new DestinationHighlightMaterialOwner();
            var generated = owner.Material;

            Assert.That(generated, Is.Not.Null);
            Assert.That(owner.IsGenerated, Is.True);
            Assert.That(owner.OwnedNativeResourceCount, Is.EqualTo(1));
            Assert.That(CountGeneratedMaterials(), Is.EqualTo(baseline + 1));

            owner.Dispose();
            yield return null;

            Assert.That(generated == null, Is.True,
                "The owned material must be destroyed, not merely dereferenced.");
            Assert.That(CountGeneratedMaterials(), Is.EqualTo(baseline));
            Assert.That(owner.OwnedNativeResourceCount, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator PlayingOwner_LeavesAnAssignedMaterialAlone()
        {
            var assigned = new Material(Shader.Find("Standard")) { name = "Assigned Play Mode Highlight" };
            try
            {
                var owner = new DestinationHighlightMaterialOwner(assigned);
                Assert.That(owner.Material, Is.SameAs(assigned));
                Assert.That(owner.IsGenerated, Is.False);

                owner.Dispose();
                yield return null;

                Assert.That(assigned == null, Is.False,
                    "A material the owner did not generate is not its to destroy, in either mode.");
            }
            finally
            {
                Object.Destroy(assigned);
            }
        }

        private static int CountGeneratedMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);
    }
}
