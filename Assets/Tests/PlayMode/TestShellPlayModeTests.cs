using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class TestShellPlayModeTests
    {
        [UnityTest]
        public IEnumerator TestRunner_CanEnterPlayModeAndAdvanceOneFrame()
        {
            Assert.That(Application.isPlaying, Is.True);

            yield return null;

            Assert.That(Application.isPlaying, Is.True);
        }
    }
}
