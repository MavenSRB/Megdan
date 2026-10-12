using System;
using System.IO;
using System.Reflection;
using GuardsDigital.Application;
using GuardsDigital.Presentation.Interaction;
using NUnit.Framework;

namespace GuardsDigital.Tests.Pve
{
    public sealed class PveLoggingTests
    {
        private sealed class FailedDisk : StringWriter
        {
            public int Attempts;
            public bool Closed;
            public override void WriteLine(string value) { Attempts++; throw new IOException("private failure detail"); }
            protected override void Dispose(bool disposing) { Closed = true; base.Dispose(disposing); }
        }

        [Test]
        public void DiskFailureDisablesOnlyCaptureAndClosesTheWriter()
        {
            var disk = new FailedDisk();
            var wrapperType = typeof(GameplayNarrationFile).GetNestedType("FailureSafeWriter", BindingFlags.NonPublic);
            var writer = (TextWriter)Activator.CreateInstance(wrapperType, new object[] { disk });
            using (var recorder = new GameplayNarrationRecorder(writer))
            {
                Assert.DoesNotThrow(() => recorder.WriteSessionEvent(1, "TEST"));
                Assert.DoesNotThrow(() => recorder.WriteSessionEvent(2, "TEST"));
            }
            Assert.That(disk.Attempts, Is.EqualTo(1));
            Assert.That(disk.Closed, Is.True);
        }
    }
}
