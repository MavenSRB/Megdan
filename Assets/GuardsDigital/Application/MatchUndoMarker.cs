using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.35r. The rewind currently standing: whose it is, and how many accepted commands
    /// it has taken back so far.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is a published fact and not a log line.</b> The match log is derived from
    /// the recorded rule-event stream, and an undo records NOTHING in that stream -
    /// <c>MatchSession.Undo</c> reconstructs from the retained command prefix, so the undone entries
    /// simply cease to exist and nothing says a rewind happened, by whom, or how many. A synthetic
    /// entry minted at the undo seam was the alternative and was refused: it would have to be
    /// STORED, because it is derivable from nothing, which breaks the log's own "derived, never
    /// stored" property and moves the snapshot schema - and its sequence number would collide with
    /// the real event that reuses it after the truncation.</para>
    /// <para><b>What it deliberately does not do.</b> It marks the rewind that is STANDING, not
    /// every rewind that ever happened. Once its owner plays forward again the marker retires,
    /// because the log's own content is again a true account of the match and there is nothing left
    /// to explain. A permanent, positioned "took back N" line in history would need exactly the
    /// storage the paragraph above refuses. The completing UX milestone inherits that limit stated,
    /// rather than discovering it.</para>
    /// <para><b>Seat rewinds only.</b> Like <c>IMatchReadModel.IsSeatUndoAvailable</c>, this is
    /// about the seat control. The Director's own Undo is its drawer's - unbounded, not seat-scoped,
    /// and belonging to no hero - so it mints no marker and the Director's full-trust view reads
    /// <see cref="None"/>.</para>
    /// </remarks>
    public readonly struct MatchUndoMarker : IEquatable<MatchUndoMarker>
    {
        private MatchUndoMarker(HeroId heroId, int count)
        {
            HeroId = heroId;
            Count = count;
        }

        /// <summary>No rewind is standing.</summary>
        public static MatchUndoMarker None => default;

        /// <summary>A standing rewind of <paramref name="count"/> commands by one hero.</summary>
        public static MatchUndoMarker Of(HeroId heroId, int count)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "A standing rewind belongs to a hero. A marker naming nobody is what " +
                    nameof(None) + " is for.",
                    nameof(heroId));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count), "A standing rewind has taken back at least one command.");
            }

            return new MatchUndoMarker(heroId, count);
        }

        /// <summary>The hero doing the rewinding, or undefined.</summary>
        public HeroId HeroId { get; }

        /// <summary>How many accepted commands this run has taken back. Zero when none stands.</summary>
        public int Count { get; }

        public bool IsStated => Count > 0 && HeroId.IsDefined;

        public bool Equals(MatchUndoMarker other) =>
            Count == other.Count && HeroId.Equals(other.HeroId);

        public override bool Equals(object obj) => obj is MatchUndoMarker other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Count * 397) ^ HeroId.GetHashCode();
            }
        }

        public override string ToString() =>
            IsStated ? HeroId.Value + " took back " + Count : "no standing rewind";
    }
}
