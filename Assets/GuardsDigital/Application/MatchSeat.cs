using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30a. One place at the table, named independently of what sits in it.
    /// </summary>
    /// <remarks>
    /// The local stand-in for the <c>OnlineSeatId</c> milestone 8.31 binds to an authenticated
    /// service identity and a live connection. There is no connection locally, so a seat is named
    /// after the player it is bound to - but it stays a distinct type, because the whole point of
    /// the binding is that a seat and the identity it controls are two facts joined by the host and
    /// never by a payload.
    /// </remarks>
    public readonly struct SeatId : IEquatable<SeatId>
    {
        public SeatId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A seat id requires a value.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SeatId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SeatId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// Milestone 8.30a. The trusted binding of one seat to the identity it may act as.
    /// </summary>
    /// <remarks>
    /// <para>Every field here is derived from the authoritative participant graph, never from a
    /// submitted payload. That is the whole rule the decision record states: the host resolves who
    /// is asking from the seat the request arrived on, and a <c>HeroId</c> or <c>TeamSide</c> inside
    /// a <see cref="Core.Replay.DirectorCommand"/> says what is being asked, never by whom.</para>
    /// </remarks>
    public sealed class MatchSeat
    {
        internal MatchSeat(SeatId id, PlayerId playerId, HeroId heroId, TeamId teamId, TeamSide teamSide)
        {
            Id = id;
            PlayerId = playerId;
            HeroId = heroId;
            TeamId = teamId;
            TeamSide = teamSide;
        }

        public SeatId Id { get; }

        /// <summary>The player this seat is, resolved through <see cref="MatchParticipants"/>.</summary>
        public PlayerId PlayerId { get; }

        /// <summary>The one hero this seat owns.</summary>
        public HeroId HeroId { get; }

        public TeamId TeamId { get; }
        public TeamSide TeamSide { get; }

        /// <summary>Whether this seat owns that hero. The only ownership question worth asking.</summary>
        public bool Owns(HeroId heroId) => HeroId.Equals(heroId);

        /// <summary>Whether this seat plays for that side.</summary>
        public bool PlaysFor(TeamSide side) => side != TeamSide.None && TeamSide == side;

        public override string ToString() =>
            Id.Value + " (" + PlayerId.Value + "/" + HeroId.Value + "/" + TeamSide + ")";
    }

    /// <summary>
    /// Milestone 8.30a. Every seat of one match, in a stable order, derived once from the
    /// authoritative participant graph.
    /// </summary>
    /// <remarks>
    /// <para>Derived rather than configured, so a restored or replaced session rebuilds the same
    /// seats from the same authority instead of carrying a second copy of who owns what. A seat's
    /// identity is therefore as durable as the participants themselves.</para>
    /// <para>The private-online track is defined as one independently owned hero per player, so a
    /// player owning any other number of heroes is refused here rather than silently seated on the
    /// first one found.</para>
    /// </remarks>
    public sealed class MatchSeatBinding
    {
        private readonly IReadOnlyList<MatchSeat> seats;

        private MatchSeatBinding(IReadOnlyList<MatchSeat> seats)
        {
            this.seats = seats;
        }

        public static MatchSeatBinding ForParticipants(MatchParticipants participants)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));

            var bound = new List<MatchSeat>();
            foreach (var player in participants.Players.OrderBy(value => value.Id.Value, StringComparer.Ordinal))
            {
                var owned = participants.Heroes
                    .Where(hero => hero.PlayerId.Equals(player.Id))
                    .ToArray();
                if (owned.Length != 1)
                {
                    throw new ArgumentException(
                        "Seating requires exactly one owned hero per player; player " +
                        player.Id.Value + " owns " + owned.Length + ".",
                        nameof(participants));
                }

                var team = participants.GetTeam(player.TeamId);
                bound.Add(new MatchSeat(
                    new SeatId(player.Id.Value), player.Id, owned[0].Id, team.Id, team.Side));
            }

            if (bound.Count == 0)
            {
                throw new ArgumentException(
                    "Seating requires at least one player.", nameof(participants));
            }

            return new MatchSeatBinding(Array.AsReadOnly(bound.ToArray()));
        }

        public IReadOnlyList<MatchSeat> Seats => seats;

        /// <summary>The seat with that id, or <c>null</c> - an unknown seat is a refusal, not a throw.</summary>
        public MatchSeat Find(SeatId id) =>
            seats.FirstOrDefault(seat => seat.Id.Equals(id));

        public MatchSeat FindByHero(HeroId heroId) =>
            seats.FirstOrDefault(seat => seat.Owns(heroId));

        public IEnumerable<MatchSeat> ForSide(TeamSide side) =>
            side == TeamSide.None
                ? Enumerable.Empty<MatchSeat>()
                : seats.Where(seat => seat.TeamSide == side);
    }
}
