using System;
using GuardsDigital.Core.Board;

namespace GuardsDigital.Core.Components
{
    /// <summary>Why a validated Swap could not be committed after all.</summary>
    public enum BoardObjectSwapRejectionReason
    {
        None = 0,

        /// <summary>One of the two objects is no longer on the board.</summary>
        UnknownObject = 1,

        /// <summary>
        /// The two named objects are the same one. Rulebook p.19 requires "two objects", and an object
        /// exchanging places with itself is the absence of a Swap rather than a zero-space one - unlike
        /// p.18's zero-space push, which the same page states outright as a legal result.
        /// </summary>
        SameObject = 2
    }

    /// <summary>Why a validated Remove could not be committed after all.</summary>
    public enum BoardObjectRemovalRejectionReason
    {
        None = 0,

        /// <summary>The named object is no longer on the board.</summary>
        UnknownObject = 1,

        /// <summary>
        /// The object is a hero. p.13 takes a hero off the board through Defeat and its consequences -
        /// unresolved-card resolution, Life, respawn - so a hero leaving the board is that staged
        /// outcome rather than p.19's reward-free Remove, and this port refuses to be the back door.
        /// </summary>
        HeroRequiresDefeat = 2
    }

    /// <summary>Why a validated Replace could not be committed after all.</summary>
    public enum BoardObjectReplacementRejectionReason
    {
        None = 0,

        /// <summary>The outgoing object is no longer on the board.</summary>
        OutgoingNotOnBoard = 1,

        /// <summary>The incoming instance identity is already on the board.</summary>
        IncomingAlreadyOnBoard = 2,

        /// <summary>
        /// The incoming object names a different space from the one the outgoing object vacates.
        /// p.19's Replace substitutes one object for another where it stands; moving the substitute
        /// somewhere else would be a Remove and a Place, which the catalog forbids decomposing it into.
        /// </summary>
        DestinationMismatch = 3
    }

    /// <summary>
    /// The outcome of one committed or refused Swap. It names both objects and both spaces and carries
    /// no movement or placement vocabulary at all, because p.19 says outright that a Swap "does not
    /// count as movement or placement".
    /// </summary>
    public readonly struct BoardObjectSwapCommitResult
    {
        private BoardObjectSwapCommitResult(
            bool accepted,
            BoardObjectSwapRejectionReason rejectionReason,
            BoardObjectReference first,
            BoardObjectReference second,
            HexCoordinate firstOrigin,
            HexCoordinate secondOrigin)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            First = first;
            Second = second;
            FirstOrigin = firstOrigin;
            SecondOrigin = secondOrigin;
        }

        public bool Accepted { get; }
        public BoardObjectSwapRejectionReason RejectionReason { get; }
        public BoardObjectReference First { get; }
        public BoardObjectReference Second { get; }

        /// <summary>Where the first object stood before the exchange, and where the second stands after it.</summary>
        public HexCoordinate FirstOrigin { get; }

        /// <summary>Where the second object stood before the exchange, and where the first stands after it.</summary>
        public HexCoordinate SecondOrigin { get; }

        public static BoardObjectSwapCommitResult Accept(
            BoardObjectReference first,
            BoardObjectReference second,
            HexCoordinate firstOrigin,
            HexCoordinate secondOrigin) =>
            new BoardObjectSwapCommitResult(
                true, BoardObjectSwapRejectionReason.None, first, second, firstOrigin, secondOrigin);

        public static BoardObjectSwapCommitResult Reject(BoardObjectSwapRejectionReason reason)
        {
            if (reason == BoardObjectSwapRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Swap requires a reason.");
            }

            return new BoardObjectSwapCommitResult(false, reason, default, default, default, default);
        }
    }

    /// <summary>The outcome of one committed or refused Remove, and the space the piece left.</summary>
    public readonly struct BoardObjectRemovalCommitResult
    {
        private BoardObjectRemovalCommitResult(
            bool accepted,
            BoardObjectRemovalRejectionReason rejectionReason,
            BoardObjectReference subject,
            HexCoordinate origin)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Subject = subject;
            Origin = origin;
        }

        public bool Accepted { get; }
        public BoardObjectRemovalRejectionReason RejectionReason { get; }
        public BoardObjectReference Subject { get; }
        public HexCoordinate Origin { get; }

        public static BoardObjectRemovalCommitResult Accept(
            BoardObjectReference subject, HexCoordinate origin) =>
            new BoardObjectRemovalCommitResult(
                true, BoardObjectRemovalRejectionReason.None, subject, origin);

        public static BoardObjectRemovalCommitResult Reject(BoardObjectRemovalRejectionReason reason)
        {
            if (reason == BoardObjectRemovalRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Remove requires a reason.");
            }

            return new BoardObjectRemovalCommitResult(false, reason, default, default);
        }
    }

    /// <summary>The outcome of one committed or refused Replace, naming both objects and their shared space.</summary>
    public readonly struct BoardObjectReplacementCommitResult
    {
        private BoardObjectReplacementCommitResult(
            bool accepted,
            BoardObjectReplacementRejectionReason rejectionReason,
            PlacedToken outgoing,
            PlacedToken incoming)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Outgoing = outgoing;
            Incoming = incoming;
        }

        public bool Accepted { get; }
        public BoardObjectReplacementRejectionReason RejectionReason { get; }

        /// <summary>The instance that left the board, or <c>null</c> when the Replace was refused.</summary>
        public PlacedToken Outgoing { get; }

        /// <summary>The instance now standing in its space, or <c>null</c> when the Replace was refused.</summary>
        public PlacedToken Incoming { get; }

        public static BoardObjectReplacementCommitResult Accept(PlacedToken outgoing, PlacedToken incoming) =>
            new BoardObjectReplacementCommitResult(
                true, BoardObjectReplacementRejectionReason.None, outgoing, incoming);

        public static BoardObjectReplacementCommitResult Reject(
            BoardObjectReplacementRejectionReason reason)
        {
            if (reason == BoardObjectReplacementRejectionReason.None)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(reason), reason, "A refused Replace requires a reason.");
            }

            return new BoardObjectReplacementCommitResult(false, reason, null, null);
        }
    }

    /// <summary>
    /// The board port milestone 6.6's three operations reach, beside the token placement, displacement,
    /// and unit Spawn ports and for the reason each of those was added: these are different authority
    /// questions, and p.19 keeps Swap, Replace, and Remove as separate operations that
    /// <c>CARD_EFFECT_CAPABILITIES.md</c> forbids treating as interchangeable.
    ///
    /// None of the three is expressible through
    /// <see cref="IBoardDisplacementAuthority.CommitValidatedDisplacement"/>. That commit requires an
    /// empty destination, and a Swap has none by definition - both spaces are occupied, which is what
    /// makes it a Swap. A Remove has no destination on the board at all, and a Replace's incoming piece
    /// takes a space its outgoing piece has not left yet. Each therefore commits here, as one
    /// transaction, and the calling operation publishes the typed facts its own rule owns.
    /// </summary>
    public interface IBoardObjectExchangeAuthority
    {
        /// <summary>
        /// Rulebook p.19: "Two objects on the board swap places with each other." One transaction over
        /// two occupied spaces, so the one-obstacle-per-space rule is never momentarily broken and
        /// neither object has to be lifted off the board first.
        /// </summary>
        BoardObjectSwapCommitResult CommitValidatedSwap(
            BoardObjectReference first,
            BoardObjectReference second);

        /// <summary>
        /// Rulebook p.19: "Remove a game piece (such as a miniature, or a token) from the board. Do not
        /// collect any coins." The piece returns to the supply it came from - a minion miniature to the
        /// reserve p.19's Respawn ordering requires, a token to its finite token supply - and this
        /// commit awards nothing, opens no Defeat stage, and records no reward eligibility.
        /// </summary>
        BoardObjectRemovalCommitResult CommitValidatedRemoval(BoardObjectReference subject,
            GuardsDigital.Core.Setup.MatchModeRules modeRules = null);

        /// <summary>
        /// Rulebook p.19: "Swap an object (such as a miniature or a token on the board) with a different
        /// object." One transaction in one space, deliberately not a Remove followed by a Place: the
        /// space is never empty between them, so nothing can take it and no observer sees two events.
        /// </summary>
        BoardObjectReplacementCommitResult CommitValidatedTokenReplacement(
            TokenInstanceId outgoing,
            PlacedToken incoming);

        /// <summary>
        /// Milestone 9.8e. p.19's Replace with a minion miniature going out and a token coming in -
        /// Mortimer's "Replace an enemy minion ... with a Zombie token". The same one transaction in one
        /// space: the miniature returns to the reserve p.19's Respawn requires, exactly as a Remove
        /// returns it, and the token takes its space without it ever standing empty. The result names
        /// the minion and the space it left.
        /// </summary>
        BoardObjectRemovalCommitResult CommitValidatedMinionReplacement(
            GuardsDigital.Core.Setup.MinionId outgoing,
            PlacedToken incoming);

        /// <summary>Atomic replacement of a single-body mode enemy retaining its hero identity.</summary>
        BoardObjectRemovalCommitResult CommitValidatedHeroReplacement(
            GuardsDigital.Core.Units.Heroes.HeroId outgoing, PlacedToken incoming);
    }
}
