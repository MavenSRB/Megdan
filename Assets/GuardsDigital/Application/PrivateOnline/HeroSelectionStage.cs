using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Rulebook p.22's "Advanced Hero Selection" methods, as a declared menu.
    /// </summary>
    /// <remarks>
    /// <para>The page prints six and this build implements one. The other five are members here
    /// rather than absences so that the shipped method is a CHOICE the session records rather than
    /// an assumption nothing states, and so a later milestone adds a behaviour to an id that
    /// already exists instead of introducing the concept and the behaviour at once. Every member's
    /// printed procedure is transcribed on it; nothing here is invented.</para>
    /// <para>One thing p.22 asks for that this build cannot do at all, in any method: filtering the
    /// Profile deck "of appropriate complexity". Complexity ratings are printed on the hero Profile
    /// cards, which have never been modelled. Recorded as an absence rather than approximated.</para>
    /// </remarks>
    public enum HeroSelectionMethod
    {
        /// <summary>
        /// IMPLEMENTED. p.22: "Players pick a hero to play out of all available heroes (starting
        /// with the team showing on the Tie Breaker coin and alternating between teams)."
        /// </summary>
        AllPick = 0,

        /// <summary>
        /// PRINTED, NOT IMPLEMENTED. p.22: "Shuffle all hero profile cards (of appropriate
        /// complexity) and deal one randomly to each player."
        /// </summary>
        AllRandom = 1,

        /// <summary>
        /// PRINTED, NOT IMPLEMENTED. p.22: "Shuffle the hero profile cards and deal 3 randomly to
        /// each player. Players take turns to pick a hero to play out of those dealt to them..."
        /// </summary>
        SingleDraft = 2,

        /// <summary>
        /// PRINTED, NOT IMPLEMENTED. p.22: "...randomly select a number of cards equal to the total
        /// number of players plus two. Players pick a hero to play out of selected heroes..."
        /// </summary>
        RandomDraft = 3,

        /// <summary>
        /// PRINTED, NOT IMPLEMENTED. p.22: two Captains draft PLAYERS into teams after heroes are
        /// selected by any other method. It reorders the seats themselves, which this build fixes
        /// for the life of a session, so it needs more than a selection stage.
        /// </summary>
        PlayerDraft = 4,

        /// <summary>
        /// PRINTED, NOT IMPLEMENTED. p.22: each team collectively picks or bans, in the printed
        /// order for the player count. Needs a team-collective decision and a ban list.
        /// </summary>
        PickAndBan = 5
    }

    /// <summary>Milestone 8.35z36. Why a hero selection was refused, in one bounded set.</summary>
    /// <remarks>
    /// Exhaustively mapped to words by <see cref="HeroSelectionLabels"/>, on the same law
    /// <c>PrivateLobbyLabels</c> follows: a refusal a player can receive without words is a refusal
    /// that reaches them as a blank line.
    /// </remarks>
    public enum HeroSelectionRefusal
    {
        None = 0,

        /// <summary>There is no seat of that name in this selection.</summary>
        UnknownSeat = 1,

        /// <summary>This build cannot deal a hero of that identity.</summary>
        UnregisteredHero = 2,

        /// <summary>
        /// Another seat already holds that hero. One copy of each exists - see
        /// <see cref="HeroSelectionStage.TryChoose"/>.
        /// </summary>
        HeroAlreadyTaken = 3,

        /// <summary>It is not this seat's turn under the printed pick order.</summary>
        NotThisSeatsTurn = 4,

        /// <summary>The host has confirmed the selection; nothing may move it now.</summary>
        SelectionConfirmed = 5,

        /// <summary>A seat has not picked yet, so there is nothing complete to confirm.</summary>
        SelectionIncomplete = 6,

        /// <summary>Only the host confirms.</summary>
        NotTheHost = 7
    }

    /// <summary>Milestone 8.35z36. One seat's row on the selection screen.</summary>
    public sealed class HeroSelectionRow
    {
        public HeroSelectionRow(
            SeatId seat,
            TeamSide teamSide,
            string seatLabel,
            string teamLabel,
            string heroLabel,
            bool hasChosen,
            bool isPicking,
            bool isOwnSeat)
        {
            Seat = seat;
            TeamSide = teamSide;
            SeatLabel = seatLabel ?? string.Empty;
            TeamLabel = teamLabel ?? string.Empty;
            HeroLabel = heroLabel ?? string.Empty;
            HasChosen = hasChosen;
            IsPicking = isPicking;
            IsOwnSeat = isOwnSeat;
        }

        public SeatId Seat { get; }
        public TeamSide TeamSide { get; }
        public string SeatLabel { get; }
        public string TeamLabel { get; }

        /// <summary>The chosen hero's epithet, or the waiting line - never blank.</summary>
        public string HeroLabel { get; }

        public bool HasChosen { get; }

        /// <summary>Whether the printed pick order is on this seat right now.</summary>
        public bool IsPicking { get; }

        public bool IsOwnSeat { get; }
    }

    /// <summary>Milestone 8.35z36. The words a selection screen says, in one place.</summary>
    public static class HeroSelectionLabels
    {
        public const string Waiting = "CHOOSING...";
        public const string NotYetPicking = "WAITING TO PICK";

        public static string Method(HeroSelectionMethod method)
        {
            switch (method)
            {
                case HeroSelectionMethod.AllPick: return "ALL PICK";
                case HeroSelectionMethod.AllRandom: return "ALL RANDOM";
                case HeroSelectionMethod.SingleDraft: return "SINGLE DRAFT";
                case HeroSelectionMethod.RandomDraft: return "RANDOM DRAFT";
                case HeroSelectionMethod.PlayerDraft: return "PLAYER DRAFT";
                case HeroSelectionMethod.PickAndBan: return "PICK AND BAN";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(method), method, "Every selection method must have words on it.");
            }
        }

        public static string Refusal(HeroSelectionRefusal refusal)
        {
            switch (refusal)
            {
                case HeroSelectionRefusal.None: return string.Empty;
                case HeroSelectionRefusal.UnknownSeat:
                    return "There is no seat of that name in this match.";
                case HeroSelectionRefusal.UnregisteredHero:
                    return "This build cannot deal that hero.";
                case HeroSelectionRefusal.HeroAlreadyTaken:
                    return "Another player has already taken that hero.";
                case HeroSelectionRefusal.NotThisSeatsTurn:
                    return "It is not your turn to pick yet.";
                case HeroSelectionRefusal.SelectionConfirmed:
                    return "The host has confirmed the line-up.";
                case HeroSelectionRefusal.SelectionIncomplete:
                    return "Every seat has to pick a hero first.";
                case HeroSelectionRefusal.NotTheHost:
                    return "Only the host confirms the line-up.";
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(refusal), refusal,
                        "Every refusal a player can receive must have words of its own.");
            }
        }
    }

    /// <summary>
    /// Milestone 8.35z36. The stage between lobby readiness and match start: who is
    /// playing which hero, decided by the players and carried into the match by the host's confirm.
    /// </summary>
    /// <remarks>
    /// <para><b>This is SESSION vocabulary, on milestone 8.35a's law.</b> It lives beside the
    /// session contracts rather than in a view for the same reason <c>PrivateLobbyModel</c> does:
    /// what a surface may see and what a seat may do are decided where the state is. It names no
    /// command, no submission and no match type - there is nothing here through which a selection
    /// screen could move a match. Its whole output is a <see cref="HeroLineup"/>, which is a Core
    /// fact naming no seat at all, and the one-way transition to the match carries that.</para>
    ///
    /// <para><b>The pick order is p.22's, not the lobby's.</b> All Pick is printed as a SEQUENCE,
    /// not a free-for-all: "starting with the team showing on the Tie Breaker coin and alternating
    /// between teams". So the order alternates Red/Blue (or Blue/Red) from the coin, and only the
    /// order WITHIN a team is unprinted - which this build answers the way it has answered every
    /// other unprinted interchangeable since milestone 8.25, by stating it once: lobby seat order.
    /// A caller that wants every seat to pick at once does not get it from All Pick; that is
    /// <see cref="HeroSelectionMethod.AllRandom"/>'s shape and it is not implemented.</para>
    ///
    /// <para><b>The stage is a projection, and every answer is derived.</b> Nothing here caches a
    /// row or a turn: the pick order is recomputed from the choices, so a rebuilt stage after a
    /// reconnect or a resync is the same stage. That is what lets it cross the wire as the choices
    /// alone.</para>
    /// </remarks>
    public sealed class HeroSelectionStage
    {
        private readonly IReadOnlyList<HeroSelectionSeat> seats;
        private readonly Dictionary<SeatId, RegisteredHero> choices =
            new Dictionary<SeatId, RegisteredHero>();
        private readonly IReadOnlyList<RegisteredHero> registered;
        private readonly TeamSide firstPickingTeam;
        private bool confirmed;

        /// <param name="seats">
        /// The session's seats with the side each plays for, in lobby order. Team side is the
        /// SEAT's fact, which is the whole reason a hero can be picked onto either team.
        /// </param>
        /// <param name="firstPickingTeam">
        /// p.22's "the team showing on the Tie Breaker coin". The host reads it from the composed
        /// match's own Tie Breaker rather than drawing a second one, so the coin that orders the
        /// picking is the coin the match plays with.
        /// </param>
        /// <param name="registeredHeroes">
        /// What this build can deal. Defaults to the live registration, so a hero dealt tomorrow is
        /// offered tomorrow with nothing here changed; a fixture may state its own.
        /// </param>
        public HeroSelectionStage(
            IEnumerable<HeroSelectionSeat> seats,
            TeamSide firstPickingTeam,
            HeroSelectionMethod method = HeroSelectionMethod.AllPick,
            IReadOnlyList<RegisteredHero> registeredHeroes = null)
        {
            var seatArray = seats?.ToArray() ?? throw new ArgumentNullException(nameof(seats));
            if (seatArray.Length == 0)
            {
                throw new ArgumentException("A selection needs seats.", nameof(seats));
            }

            if (seatArray.Select(seat => seat.Seat).Distinct().Count() != seatArray.Length)
            {
                throw new ArgumentException("A seat appears twice in the selection.", nameof(seats));
            }

            if (firstPickingTeam != TeamSide.Red && firstPickingTeam != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(firstPickingTeam), firstPickingTeam,
                    "The Tie Breaker coin shows Red or Blue, and p.22's pick order starts there.");
            }

            if (method != HeroSelectionMethod.AllPick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(method), method,
                    "All Pick is the only selection method this build implements; the other five " +
                    "on rulebook p.22 are declared and not built.");
            }

            this.seats = Array.AsReadOnly(seatArray);
            this.firstPickingTeam = firstPickingTeam;
            Method = method;
            registered = registeredHeroes ?? MegdanSelectionRoster.ForSeats(seatArray.Select(s => s.TeamSide));
        }

        public HeroSelectionMethod Method { get; }

        /// <summary>p.22's coin, which decides who picks first.</summary>
        public TeamSide FirstPickingTeam => firstPickingTeam;

        /// <summary>Whether the host has confirmed. One-way, exactly as the match start is.</summary>
        public bool IsConfirmed => confirmed;

        /// <summary>Every seat has a hero, so the host may confirm.</summary>
        public bool IsComplete => choices.Count == seats.Count;

        /// <summary>
        /// The heroes still available to pick: the registration minus what other seats hold.
        /// </summary>
        /// <remarks>
        /// Derived from the registration on every call rather than copied at construction, so this
        /// never counts the roster and a hero dealt in a later milestone appears here with no edit.
        /// </remarks>
        public IReadOnlyList<RegisteredHero> AvailableHeroes
        {
            get
            {
                var taken = new HashSet<string>(
                    choices.Values.Select(hero => hero.HeroIdValue), StringComparer.Ordinal);
                return registered.Where(hero => !taken.Contains(hero.HeroIdValue)).ToArray();
            }
        }

        /// <summary>
        /// The seat p.22's pick order is on, or none once every seat has picked.
        /// </summary>
        /// <remarks>
        /// <para>Derived from <see cref="PickOrder"/>: the first seat in the printed order that has
        /// no hero yet. Reading it that way rather than counting picks is what makes the order
        /// survive a seat being filled out of sequence - a spawned test driver assigned by the host
        /// - because the answer is always "the earliest seat still owing a pick" rather than "the
        /// Nth seat".</para>
        /// </remarks>
        public SeatId PickingSeat
        {
            get
            {
                foreach (var seat in PickOrder)
                {
                    if (!choices.ContainsKey(seat.Seat))
                    {
                        return seat.Seat;
                    }
                }

                return default;
            }
        }

        /// <summary>
        /// 8.35z54. The seat whose pick would be taken back: the LAST seat in the printed
        /// order that has chosen. Undefined when nobody has picked.
        /// </summary>
        /// <remarks>
        /// Read out of <see cref="PickOrder"/> for the same reason <see cref="PickingSeat"/> is -
        /// so the answer stays right when a seat was filled out of sequence - and it is the exact
        /// mirror of that property: the picking seat is the earliest still owing a pick, and this is
        /// the latest that has made one.
        /// </remarks>
        public SeatId LastPickedSeat
        {
            get
            {
                var last = default(SeatId);
                foreach (var seat in PickOrder)
                {
                    if (choices.ContainsKey(seat.Seat))
                    {
                        last = seat.Seat;
                    }
                }

                return last;
            }
        }

        /// <summary>
        /// 8.35z54. Takes back the most recent pick, returning the hero to the available
        /// list and the order to the seat that made it.
        /// </summary>
        /// <remarks>
        /// <para>The stage half of the host's lobby undo - the user's "if we get stuck we can get
        /// out". It removes exactly one choice and nothing else: the seats, the coin and the order
        /// are untouched, so the seat that just lost its pick is once again
        /// <see cref="PickingSeat"/> and everybody's screen says so from the roster alone.</para>
        /// <para>Refused once the stage is confirmed. A confirmed selection has already become a
        /// composed match, and this is a lobby control rather than a way back into one.</para>
        /// </remarks>
        /// <returns>Whether a pick was taken back.</returns>
        public bool TryUndoLastPick(out SeatId seat)
        {
            seat = default;
            if (confirmed)
            {
                return false;
            }

            var last = LastPickedSeat;
            if (!last.IsDefined || !choices.Remove(last))
            {
                return false;
            }

            seat = last;
            return true;
        }

        /// <summary>
        /// p.22's All Pick order in full: the coin's team, then the other, then the coin's again -
        /// each team's seats in lobby order.
        /// </summary>
        /// <remarks>
        /// <para>The alternation is the printed rule and is built by INTERLEAVING the two teams'
        /// seat lists rather than by scanning for an unpicked seat per team. The distinction is not
        /// cosmetic: a scan returns the coin team's second seat while the other team still owes its
        /// first, which would let one team pick its whole side before the other picked at all -
        /// the opposite of what p.22 prints. A fixture drove exactly that and is why this is an
        /// interleave.</para>
        /// <para>A team with more seats than the other keeps its surplus at the end, which cannot
        /// arise in either printed format (setup step 2's teams are equal) but is defined rather
        /// than left to an index.</para>
        /// </remarks>
        public IReadOnlyList<HeroSelectionSeat> PickOrder => PickOrderFor(seats, firstPickingTeam);

        /// <summary>
        /// p.22's All Pick order over any seats, as a function of the seats and the coin alone.
        /// </summary>
        /// <remarks>
        /// Milestone 8.35z37 lifted this out of the instance property so a CLIENT can draw
        /// the same order the host enforces. A client holds no stage - the stage is the host's, and
        /// the rule stays there - but it does hold the roster and the published coin, which is
        /// exactly this function's two arguments. One implementation of the printed order, read by
        /// both sides: a surface that derived its own would be a second reading of p.22, free to
        /// show a turn the host would refuse.
        /// </remarks>
        public static IReadOnlyList<HeroSelectionSeat> PickOrderFor(
            IReadOnlyList<HeroSelectionSeat> seats, TeamSide firstPickingTeam)
        {
            if (seats == null) throw new ArgumentNullException(nameof(seats));

            var first = seats.Where(seat => seat.TeamSide == firstPickingTeam).ToArray();
            var second = seats.Where(seat => seat.TeamSide != firstPickingTeam).ToArray();

            var order = new List<HeroSelectionSeat>(seats.Count);
            var longest = Math.Max(first.Length, second.Length);
            for (var index = 0; index < longest; index++)
            {
                if (index < first.Length) order.Add(first[index]);
                if (index < second.Length) order.Add(second[index]);
            }

            return order;
        }

        /// <summary>The screen, one row a seat, in lobby order.</summary>
        public IReadOnlyList<HeroSelectionRow> RowsFor(SeatId viewingSeat)
        {
            var picking = PickingSeat;
            var rows = new List<HeroSelectionRow>();
            foreach (var seat in seats)
            {
                var hasChosen = choices.TryGetValue(seat.Seat, out var hero);
                rows.Add(new HeroSelectionRow(
                    seat.Seat,
                    seat.TeamSide,
                    PrivateLobbyLabels.Identifier(seat.Seat.Value),
                    PrivateLobbyLabels.Team(seat.TeamSide),
                    hasChosen
                        ? hero.DisplayName
                        : seat.Seat.Equals(picking)
                            ? HeroSelectionLabels.Waiting
                            : HeroSelectionLabels.NotYetPicking,
                    hasChosen,
                    seat.Seat.Equals(picking),
                    seat.Seat.Equals(viewingSeat)));
            }

            return rows;
        }

        /// <summary>The hero this seat holds, or false when it has not picked.</summary>
        public bool TryGetChoice(SeatId seat, out RegisteredHero hero) =>
            choices.TryGetValue(seat, out hero);

        /// <summary>
        /// One seat picks one hero.
        /// </summary>
        /// <remarks>
        /// <para><b>Duplicates are refused across ALL seats, not per team.</b> The physical game
        /// holds one Profile card and one dashboard per hero, and p.22's All Pick has players pick
        /// "out of all available heroes" - a hero somebody has taken is not available to anybody.
        /// So the same hero can never end up on both teams, and never on two seats of one team.
        /// The refusal is here, at the only door a choice enters through, rather than at the
        /// composition, so a player is told at pick time instead of at match start; and
        /// <see cref="HeroLineup"/> refuses it a second time so that no OTHER path into a composed
        /// board - a bot filler, a launch flag, a fixture - can produce what this refuses.</para>
        /// <para>Re-picking is allowed while the stage is unconfirmed and the order is on this
        /// seat: the seat's previous hero returns to the available list. Picking the hero you
        /// already hold is accepted and changes nothing.</para>
        /// </remarks>
        public bool TryChoose(SeatId seat, string heroIdValue, out HeroSelectionRefusal refusal)
        {
            if (confirmed)
            {
                refusal = HeroSelectionRefusal.SelectionConfirmed;
                return false;
            }

            var row = seats.FirstOrDefault(candidate => candidate.Seat.Equals(seat));
            if (row == null)
            {
                refusal = HeroSelectionRefusal.UnknownSeat;
                return false;
            }

            if (!TryFindRegistered(heroIdValue, out var hero))
            {
                refusal = HeroSelectionRefusal.UnregisteredHero;
                return false;
            }

            if (ReferenceEquals(registered, MegdanSelectionRoster.DemoHeroes) &&
                !MegdanSelectionRoster.AllowsDemoTeam(hero, row.TeamSide))
            {
                refusal = HeroSelectionRefusal.UnregisteredHero;
                return false;
            }

            if (!seat.Equals(PickingSeat))
            {
                refusal = HeroSelectionRefusal.NotThisSeatsTurn;
                return false;
            }

            foreach (var taken in choices)
            {
                if (!taken.Key.Equals(seat) &&
                    string.Equals(
                        taken.Value.HeroIdValue, hero.HeroIdValue, StringComparison.Ordinal))
                {
                    refusal = HeroSelectionRefusal.HeroAlreadyTaken;
                    return false;
                }
            }

            choices[seat] = hero;
            refusal = HeroSelectionRefusal.None;
            return true;
        }

        /// <summary>
        /// Give a hero to a seat that is not going to pick for itself - a spawned test driver.
        /// </summary>
        /// <remarks>
        /// <para>The THIRTEENTH amendment's practice drivers and the local seat-less compositions
        /// fill seats nobody is sitting in, and a seat nobody sits in cannot take its turn in
        /// p.22's order. So this bypasses the ORDER and nothing else: the registration check and
        /// the one-copy rule both still apply, which is what stops a bot from being handed a hero a
        /// player already picked. It is host-side by construction - there is no wire message for
        /// it - so a seat cannot use it to jump the queue.</para>
        /// <para><paramref name="preferred"/> lets the host name a hero; passing none takes the
        /// first still available, which is deterministic and therefore replayable. Random
        /// assignment is deliberately NOT here: an unseeded draw at composition would be the one
        /// thing in a composed match that a recorded setup cannot reproduce, and milestone 8.28b
        /// settled that every setup draw comes from the recorded seed. A random filler belongs with
        /// that seed, and is recorded as owed rather than improvised.</para>
        /// </remarks>
        public bool TryAssign(SeatId seat, string preferred, out HeroSelectionRefusal refusal)
        {
            if (confirmed)
            {
                refusal = HeroSelectionRefusal.SelectionConfirmed;
                return false;
            }

            if (!seats.Any(candidate => candidate.Seat.Equals(seat)))
            {
                refusal = HeroSelectionRefusal.UnknownSeat;
                return false;
            }

            var side = seats.First(s => s.Seat.Equals(seat)).TeamSide;
            var available = AvailableHeroes.Where(h => !ReferenceEquals(registered, MegdanSelectionRoster.DemoHeroes) ||
                MegdanSelectionRoster.AllowsDemoTeam(h, side)).ToArray();
            RegisteredHero hero;
            if (string.IsNullOrWhiteSpace(preferred))
            {
                if (available.Length == 0)
                {
                    refusal = HeroSelectionRefusal.HeroAlreadyTaken;
                    return false;
                }

                hero = available[0];
            }
            else
            {
                if (!TryFindRegistered(preferred, out hero))
                {
                    refusal = HeroSelectionRefusal.UnregisteredHero;
                    return false;
                }
                if (ReferenceEquals(registered, MegdanSelectionRoster.DemoHeroes) &&
                    !MegdanSelectionRoster.AllowsDemoTeam(hero, side))
                {
                    refusal = HeroSelectionRefusal.UnregisteredHero;
                    return false;
                }

                if (!available.Any(candidate => string.Equals(
                        candidate.HeroIdValue, hero.HeroIdValue, StringComparison.Ordinal)) &&
                    !(choices.TryGetValue(seat, out var held) && string.Equals(
                        held.HeroIdValue, hero.HeroIdValue, StringComparison.Ordinal)))
                {
                    refusal = HeroSelectionRefusal.HeroAlreadyTaken;
                    return false;
                }
            }

            choices[seat] = hero;
            refusal = HeroSelectionRefusal.None;
            return true;
        }

        /// <summary>
        /// The host confirms, and the stage becomes the line-up the match composes from.
        /// </summary>
        /// <remarks>
        /// One-way, exactly as the transition to the match is: the flag is set once and nothing
        /// clears it, so a message arriving one moment late is refused by name rather than moving a
        /// line-up the board is already built from. The line-up carries no seat, so what crosses
        /// into the match is what the rules need and nothing about who chose it.
        /// </remarks>
        public bool TryConfirm(bool isHost, out HeroLineup lineup, out HeroSelectionRefusal refusal)
        {
            lineup = null;

            if (!isHost)
            {
                refusal = HeroSelectionRefusal.NotTheHost;
                return false;
            }

            if (confirmed)
            {
                refusal = HeroSelectionRefusal.SelectionConfirmed;
                return false;
            }

            if (!IsComplete)
            {
                refusal = HeroSelectionRefusal.SelectionIncomplete;
                return false;
            }

            lineup = BuildLineup();
            confirmed = true;
            refusal = HeroSelectionRefusal.None;
            return true;
        }

        /// <summary>
        /// The line-up as it stands, for a surface that wants to show the board that would be
        /// composed. Throws while incomplete - a partial line-up is not a line-up.
        /// </summary>
        /// <summary>
        /// The registered hero of that identity, looked up in THIS stage's registration.
        /// </summary>
        /// <remarks>
        /// One lookup against one list. An earlier draft asked both this stage's registration and
        /// the build's global one and required membership in both, which is a rule nobody could
        /// state: a stage given its own roster would have refused a hero on that roster. The
        /// registration a stage was built with is the only registration it has.
        /// </remarks>
        private bool TryFindRegistered(string heroIdValue, out RegisteredHero hero)
        {
            foreach (var candidate in registered)
            {
                if (string.Equals(candidate.HeroIdValue, heroIdValue, StringComparison.Ordinal))
                {
                    hero = candidate;
                    return true;
                }
            }

            hero = default;
            return false;
        }

        public HeroLineup BuildLineup()
        {
            if (!IsComplete)
            {
                throw new InvalidOperationException(
                    "Every seat has to hold a hero before there is a line-up to compose.");
            }

            var red = new List<RegisteredHero>();
            var blue = new List<RegisteredHero>();
            foreach (var seat in seats)
            {
                var hero = choices[seat.Seat];
                if (seat.TeamSide == TeamSide.Red)
                {
                    red.Add(hero);
                }
                else
                {
                    blue.Add(hero);
                }
            }

            return new HeroLineup(red, blue);
        }
    }

    /// <summary>
    /// Milestone 8.35z36. One seat in a selection: which seat, and which side it plays
    /// for. Both are the session's facts and neither is the hero's.
    /// </summary>
    public sealed class HeroSelectionSeat
    {
        public HeroSelectionSeat(SeatId seat, TeamSide teamSide)
        {
            if (!seat.IsDefined)
            {
                throw new ArgumentException("A selection seat requires a seat.", nameof(seat));
            }

            if (teamSide != TeamSide.Red && teamSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(teamSide), teamSide, "A seat plays for Red or for Blue.");
            }

            Seat = seat;
            TeamSide = teamSide;
        }

        public SeatId Seat { get; }
        public TeamSide TeamSide { get; }
    }
}
