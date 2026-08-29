using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30a. A development-only way to sit in any seat of a local match.
    /// </summary>
    /// <remarks>
    /// <para><b>This is not a player-facing surface and never becomes one.</b> It exists so that six
    /// seat perspectives can be exercised in one process before a transport exists - by fixtures now,
    /// by milestone 8.30b's visibly non-player-facing seat switcher and 8.30c's six-seat gate next.
    /// A private-online client is handed one <see cref="ISeatClient"/> for the seat its connection
    /// was bound to and never sees this type at all, which is the same internal development
    /// affordance boundary `DEV-004` draws around the headless smoke driver.</para>
    /// <para>It holds no authority and can create none. Every seat it hands out comes from the host's
    /// own binding, which is derived from the authoritative participant graph, so this cannot invent
    /// a seat, move one, or change what a seat owns - it can only choose which existing seat to look
    /// through.</para>
    /// </remarks>
    public sealed class LocalSeatHarness
    {
        private readonly IMatchHost host;

        public LocalSeatHarness(IMatchHost host)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
        }

        /// <summary>Every seat of the match, in the binding's own stable order.</summary>
        public IReadOnlyList<MatchSeat> Seats => host.Seats.Seats;

        /// <summary>The client for that seat.</summary>
        public ISeatClient Select(SeatId seat) => host.ClientForSeat(seat);

        /// <summary>The client for the seat that owns that hero.</summary>
        public ISeatClient SelectByHero(HeroId heroId)
        {
            var seat = host.Seats.FindByHero(heroId);
            if (seat == null)
            {
                throw new ArgumentException(
                    "No seat owns hero '" + heroId.Value + "'.", nameof(heroId));
            }

            return host.ClientForSeat(seat.Id);
        }

        /// <summary>One client per seat, which is how a fixture drives all six perspectives at once.</summary>
        public IReadOnlyList<ISeatClient> SelectAll() =>
            Array.AsReadOnly(Seats.Select(seat => host.ClientForSeat(seat.Id)).ToArray());

        /// <summary>The clients of one side, for a team-owned decision.</summary>
        public IReadOnlyList<ISeatClient> SelectSide(TeamSide side) =>
            Array.AsReadOnly(host.Seats.ForSide(side)
                .Select(seat => host.ClientForSeat(seat.Id))
                .ToArray());
    }
}
