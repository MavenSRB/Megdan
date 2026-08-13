using System;
using System.Collections;
using System.Collections.Generic;

namespace GuardsDigital.Core.Diagnostics
{
    /// <summary>
    /// Marks a list that cannot change after it is handed over, so a holder can skip the defensive
    /// copy it would otherwise have to make. Only <see cref="AppendOnlyEventLog{T}"/>'s snapshots
    /// carry it; anything else stays subject to the copy.
    /// </summary>
    public interface IImmutableEventSnapshot
    {
    }

    /// <summary>
    /// An ordered log whose contents can be snapshotted in constant time, however long it has grown.
    /// </summary>
    /// <remarks>
    /// This exists for the rollback pre-image. Every command captures one, so anything the capture
    /// copies is a per-command cost that grows with session length - the second half of finding
    /// `F613-08`. Milestone 6.1i removed the expensive form of that copy, an encode of the whole
    /// accumulated log, but left an `ToArray()` of it behind and measured encoded characters rather
    /// than allocations, so the remaining copy was invisible to the regression fixture.
    ///
    /// The structure is an append-only buffer plus a count. A snapshot is that pair, so taking one
    /// costs nothing and the value stays correct forever:
    /// <list type="bullet">
    /// <item>An append writes at index <c>count</c>, which is at or past the end of every
    /// outstanding snapshot, so no snapshot's visible range is ever overwritten.</item>
    /// <item>Growth allocates a new buffer and leaves the old one untouched, so snapshots taken
    /// before it keep reading the array they were given.</item>
    /// <item>A reset does the same rather than clearing in place, which is what makes this safe for
    /// a log that <c>Restore</c> can replace wholesale in the middle of a command.</item>
    /// </list>
    /// The snapshot is also memoized and invalidated on mutation, so an unchanged log hands out the
    /// same instance. That is what lets a test state "the capture copied nothing" as a reference
    /// comparison rather than as an allocation measurement, which this Unity profile cannot take:
    /// `GC.GetTotalAllocatedBytes` is absent and `GC.GetAllocatedBytesForCurrentThread` returns a
    /// constant.
    /// </remarks>
    public sealed class AppendOnlyEventLog<T>
    {
        private T[] buffer = Array.Empty<T>();
        private int count;
        private Snapshot memoizedSnapshot;

        public int Count => count;

        /// <summary>
        /// The current contents as an immutable value. Constant time, and the same instance until
        /// the log next changes.
        /// </summary>
        public IReadOnlyList<T> Entries => memoizedSnapshot ?? (memoizedSnapshot = new Snapshot(buffer, count));

        public void Add(T item)
        {
            if (count == buffer.Length)
            {
                // A new array rather than an in-place resize, so every outstanding snapshot keeps
                // the buffer it was handed.
                var grown = new T[buffer.Length == 0 ? 4 : buffer.Length * 2];
                Array.Copy(buffer, grown, count);
                buffer = grown;
            }

            buffer[count] = item;
            count++;
            memoizedSnapshot = null;
        }

        /// <summary>Discards the contents, leaving any snapshot of them intact and readable.</summary>
        public void Reset()
        {
            buffer = Array.Empty<T>();
            count = 0;
            memoizedSnapshot = null;
        }

        /// <summary>Replaces the contents wholesale, on the same terms as <see cref="Reset"/>.</summary>
        public void Reset(IReadOnlyList<T> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            buffer = new T[items.Count];
            for (var index = 0; index < items.Count; index++)
            {
                buffer[index] = items[index];
            }

            count = items.Count;
            memoizedSnapshot = null;
        }

        private sealed class Snapshot : IReadOnlyList<T>, IImmutableEventSnapshot
        {
            private readonly T[] buffer;

            public Snapshot(T[] buffer, int count)
            {
                this.buffer = buffer;
                Count = count;
            }

            public int Count { get; }

            public T this[int index] => index >= 0 && index < Count
                ? buffer[index]
                : throw new ArgumentOutOfRangeException(nameof(index));

            public IEnumerator<T> GetEnumerator()
            {
                for (var index = 0; index < Count; index++)
                {
                    yield return buffer[index];
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
