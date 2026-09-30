using System;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Units.Heroes
{
    /// <summary>
    /// One body of a hero standing on one space.
    ///
    /// <para>Milestone 9.21a. A hero may stand on several spaces as one hero (Razzle's "each of you is
    /// the same hero"), so an occupancy names a BODY: the hero plus a body ordinal. Every hero that
    /// declares no bodies has exactly one, ordinal <see cref="FirstBody"/>, which is what the
    /// two-argument constructor builds - so every single-bodied hero's occupancy, equality, ordering
    /// and persisted row mean exactly what they meant before. The hand, level, coins, items and
    /// Ultimate stay the hero's; only the board sees bodies.</para>
    /// </summary>
    public readonly struct HeroOccupancy : IEquatable<HeroOccupancy>
    {
        /// <summary>
        /// The ordinal every hero's first body carries, and the only one a hero that declares no body
        /// supply can have. It is also the default ACTING body (RZ-Q4): until milestone 9.21b builds
        /// the pick, a read that asks where a many-bodied hero stands as the actor answers this body.
        /// </summary>
        public const int FirstBody = 1;

        public HeroOccupancy(HeroId heroId, HexCoordinate coordinate)
            : this(heroId, FirstBody, coordinate)
        {
        }

        public HeroOccupancy(HeroId heroId, int body, HexCoordinate coordinate)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("Hero occupancy requires a defined hero ID.", nameof(heroId));
            }

            if (body < FirstBody)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(body), body, "A hero body ordinal starts at 1.");
            }

            HeroId = heroId;
            Body = body;
            Coordinate = coordinate;
        }

        public HeroId HeroId { get; }

        /// <summary>Milestone 9.21a. Which of the hero's bodies stands here; 1 for every single-bodied hero.</summary>
        public int Body { get; }

        public HexCoordinate Coordinate { get; }

        /// <summary>The same body standing somewhere else.</summary>
        public HeroOccupancy MovedTo(HexCoordinate destination) =>
            new HeroOccupancy(HeroId, Body, destination);

        public bool Equals(HeroOccupancy other)
        {
            return HeroId.Equals(other.HeroId) && Body == other.Body && Coordinate.Equals(other.Coordinate);
        }

        public override bool Equals(object obj)
        {
            return obj is HeroOccupancy other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (((HeroId.GetHashCode() * 397) ^ Body) * 397) ^ Coordinate.GetHashCode();
            }
        }

        public override string ToString()
        {
            return Body == FirstBody ? $"{HeroId} at {Coordinate}" : $"{HeroId} body {Body} at {Coordinate}";
        }
    }

    /// <summary>
    /// Milestone 9.21a. How many bodies a hero has in all (RZ-Q1: Razzle's four - the hero plus three).
    ///
    /// <para>A hero that declares none has one, and the board's one-space-per-hero rule is relaxed only
    /// for a hero that declares more: a second occupancy of a hero with no declaration is still
    /// refused as it always was. p.19's Spawn takes from this supply, so the bodies standing never
    /// exceed it.</para>
    /// </summary>
    public readonly struct HeroBodySupply : IEquatable<HeroBodySupply>
    {
        public HeroBodySupply(HeroId heroId, int bodies)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A hero body supply requires a defined hero ID.", nameof(heroId));
            }

            if (bodies < HeroOccupancy.FirstBody)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(bodies), bodies, "A hero has at least one body.");
            }

            HeroId = heroId;
            Bodies = bodies;
        }

        public HeroId HeroId { get; }

        public int Bodies { get; }

        public bool Equals(HeroBodySupply other) => HeroId.Equals(other.HeroId) && Bodies == other.Bodies;

        public override bool Equals(object obj) => obj is HeroBodySupply other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (HeroId.GetHashCode() * 397) ^ Bodies;
            }
        }

        public override string ToString() => $"{HeroId} x{Bodies}";
    }
}
