using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TestShellEditModeTests
    {
        [Test]
        public void TestRunner_CanExecuteEditModeTest()
        {
            Assert.That(1 + 1, Is.EqualTo(2));
        }
    }
}
