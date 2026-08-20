using System.Linq;
using GuardsDigital.Core.Diagnostics;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// The structure the rollback pre-image relies on: a snapshot costs nothing to take and stays
    /// correct forever, including across the buffer growth and wholesale replacement that a restore
    /// performs in the middle of a command.
    /// </summary>
    public sealed class AppendOnlyEventLogTests
    {
        /// <summary>
        /// The property finding `F613-08` actually names. An unchanged log hands back the same
        /// instance, so "the capture copied nothing" is observable as a reference comparison - which
        /// this Unity profile's absent managed-allocation counters cannot express any other way.
        /// </summary>
        [Test]
        public void Entries_ReturnsTheSameInstanceUntilTheLogChanges()
        {
            var log = new AppendOnlyEventLog<string>();
            log.Add("one");

            var first = log.Entries;
            Assert.That(log.Entries, Is.SameAs(first),
                "Reading twice without a mutation must not build a second snapshot.");

            log.Add("two");
            Assert.That(log.Entries, Is.Not.SameAs(first));
            Assert.That(first, Is.EqualTo(new[] { "one" }),
                "The older snapshot keeps the contents it was taken from.");
        }

        /// <summary>
        /// Growth reallocates. A snapshot taken before it must keep reading the array it was handed
        /// rather than following the log forward, which is what makes the capture safe to keep.
        /// </summary>
        [Test]
        public void Snapshot_SurvivesTheBufferGrowthThatFollowsIt()
        {
            var log = new AppendOnlyEventLog<int>();
            for (var value = 0; value < 5; value++)
            {
                log.Add(value);
            }

            var taken = log.Entries;
            for (var value = 5; value < 200; value++)
            {
                log.Add(value);
            }

            Assert.That(taken.Count, Is.EqualTo(5));
            Assert.That(taken.ToArray(), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }));
            Assert.That(log.Entries.Count, Is.EqualTo(200));
        }

        /// <summary>
        /// Milestone 7.9b's prefix view, on the terms the full snapshot states: taken for free,
        /// correct forever. A retained-prefix Undo reads the history without its latest entry, and
        /// materializing that prefix as a copy would be a per-command cost on the path that exists
        /// to remove one.
        /// </summary>
        [Test]
        public void PrefixEntries_IsAnImmutableViewOnTheSnapshotTerms()
        {
            var log = new AppendOnlyEventLog<string>();
            log.Add("one");
            log.Add("two");
            log.Add("three");

            var prefix = log.PrefixEntries(2);
            Assert.That(prefix, Is.EqualTo(new[] { "one", "two" }));
            Assert.That(log.PrefixEntries(3), Is.SameAs(log.Entries),
                "The whole log as a prefix is the snapshot itself, not a second instance.");

            log.Add("four");
            log.Reset(new[] { "replaced" });
            Assert.That(prefix, Is.EqualTo(new[] { "one", "two" }),
                "A prefix view keeps reading the buffer it was handed across appends and resets.");

            Assert.That(() => log.PrefixEntries(2), Throws.InstanceOf<System.ArgumentOutOfRangeException>(),
                "A prefix longer than the log describes nothing and must be refused.");
            Assert.That(() => log.PrefixEntries(-1), Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }

        /// <summary>
        /// The case that rules out the cheaper "remember the length and truncate" pre-image. A
        /// restore replaces the log wholesale, and it can do so inside a command that is later
        /// rolled back, so a snapshot must be readable after its contents have been replaced rather
        /// than merely extended.
        /// </summary>
        [Test]
        public void Snapshot_SurvivesAResetThatReplacesTheLogWholesale()
        {
            var log = new AppendOnlyEventLog<string>();
            log.Add("before");
            var taken = log.Entries;

            log.Reset(new[] { "restored" });

            Assert.That(taken, Is.EqualTo(new[] { "before" }),
                "A length alone would not have been enough to reconstruct this.");
            Assert.That(log.Entries, Is.EqualTo(new[] { "restored" }));

            log.Reset();
            Assert.That(taken, Is.EqualTo(new[] { "before" }));
            Assert.That(log.Entries, Is.Empty);
        }
    }
}
