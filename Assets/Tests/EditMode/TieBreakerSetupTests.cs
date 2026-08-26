using System;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TieBreakerSetupTests
    {
        [Test]
        public void NormalSetup_StoresOneLegalRevealedSideInAuthoritativeState()
        {
            var configuration = MatchConfiguration.Create();

            Assert.That(configuration.TieBreakerState.IsDefined, Is.True);
            Assert.That(
                configuration.TieBreakerState.RevealedSide,
                Is.EqualTo(TeamSide.Red).Or.EqualTo(TeamSide.Blue));
        }

        [TestCase(TeamSide.Red)]
        [TestCase(TeamSide.Blue)]
        public void InternalTestSetup_StoresTheExplicitSide(TeamSide explicitSide)
        {
            var configuration =
                MatchConfiguration.CreateForInternalTest(explicitSide);

            Assert.That(configuration.TieBreakerState.RevealedSide, Is.EqualTo(explicitSide));
        }

        [Test]
        public void InternalTestSetup_RejectsAnUndefinedSide()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MatchConfiguration.CreateForInternalTest(TeamSide.None));
        }

        [Test]
        public void TieBreakerState_IsImmutableValueDataForLaterSnapshotHistory()
        {
            var original = TieBreakerSetupAuthority.CreateInternalTestSetupState(TeamSide.Blue);
            var reconstructed = new TieBreakerState(original.RevealedSide);

            Assert.That(reconstructed, Is.EqualTo(original));
            Assert.That(reconstructed.GetHashCode(), Is.EqualTo(original.GetHashCode()));
            Assert.That(reconstructed.ToString(), Is.EqualTo("Blue"));
        }

        [Test]
        public void DefaultTieBreakerState_IsNotValidAuthoritativeSetupState()
        {
            Assert.That(default(TieBreakerState).IsDefined, Is.False);
        }
    }
}
