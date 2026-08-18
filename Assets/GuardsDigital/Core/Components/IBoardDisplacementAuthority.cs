using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.Displacement;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// What kind of object occupies a space. The public counterpart of the board index's internal
    /// occupant vocabulary: the index keeps its own type private because it owns the one-obstacle-per-
    /// space rule, and a capability that displaces something still has to be told what it moved.
    ///
    /// Tokens are present here and deliberately absent from <see cref="RuleObject"/>. p.18's Push names
    /// "a unit or a token" as targets, so a displacement subject can be either; p.19 states that tokens
    /// are not units, so a token can never be the affected object of a restriction, permission,
    /// immunity, or <c>Counts As</c>. The two vocabularies answer different questions and stay separate.
    /// </summary>
    public enum BoardObjectKind
    {
        None = 0,
        Hero = 1,
        Minion = 2,
        Token = 3
    }

    /// <summary>The stable identity of one displaceable object on the board.</summary>
    public readonly struct BoardObjectReference : IEquatable<BoardObjectReference>
    {
        private BoardObjectReference(
            BoardObjectKind kind,
            HeroId heroId,
            MinionId minionId,
            TokenInstanceId tokenInstanceId)
        {
            Kind = kind;
            HeroId = heroId;
            MinionId = minionId;
            TokenInstanceId = tokenInstanceId;
        }

        public BoardObjectKind Kind { get; }
        public HeroId HeroId { get; }
        public MinionId MinionId { get; }
        public TokenInstanceId TokenInstanceId { get; }

        public bool IsDefined => Kind != BoardObjectKind.None;

        public static BoardObjectReference ForHero(HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException("A hero board object requires a defined hero ID.", nameof(heroId));
            }

            return new BoardObjectReference(BoardObjectKind.Hero, heroId, default, default);
        }

        public static BoardObjectReference ForMinion(MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A minion board object requires a defined minion ID.", nameof(minionId));
            }

            return new BoardObjectReference(BoardObjectKind.Minion, default, minionId, default);
        }

        public static BoardObjectReference ForToken(TokenInstanceId tokenInstanceId)
        {
            if (!tokenInstanceId.IsDefined)
            {
                throw new ArgumentException("A token board object requires a defined instance ID.", nameof(tokenInstanceId));
            }

            return new BoardObjectReference(BoardObjectKind.Token, default, default, tokenInstanceId);
        }

        /// <summary>
        /// This object as a rules object, or <see cref="RuleObject.None"/> for a token. The named
        /// absence is the honest answer rather than a missing one: p.18's immunity and permission
        /// families are addressed to units, so asking them about a token asks a question the rulebook
        /// does not pose.
        /// </summary>
        public RuleObject AsRuleObject()
        {
            switch (Kind)
            {
                case BoardObjectKind.Hero:
                    return RuleObject.ForHero(HeroId);
                case BoardObjectKind.Minion:
                    return RuleObject.ForMinion(MinionId);
                default:
                    return RuleObject.None;
            }
        }

        public bool Equals(BoardObjectReference other) =>
            Kind == other.Kind &&
            HeroId.Equals(other.HeroId) &&
            MinionId.Equals(other.MinionId) &&
            TokenInstanceId.Equals(other.TokenInstanceId);

        public override bool Equals(object obj) => obj is BoardObjectReference other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ HeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ MinionId.GetHashCode();
                hashCode = (hashCode * 397) ^ TokenInstanceId.GetHashCode();
                return hashCode;
            }
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case BoardObjectKind.Hero:
                    return $"Hero {HeroId.Value}";
                case BoardObjectKind.Minion:
                    return $"Minion {MinionId.Value}";
                case BoardObjectKind.Token:
                    return $"Token {TokenInstanceId.Value}";
                default:
                    return "Undefined board object";
            }
        }
    }

    /// <summary>
    /// The board port a displacement capability reaches, beside the token placement port
    /// <see cref="ITokenPlacementAuthority"/> and for the same reason: the capability needs specific
    /// authoritative questions and one validated commit, not the whole board authority.
    ///
    /// It exposes the geometry rather than the map so the queries stay on the authority that owns
    /// occupancy. A handler asking "where does this push end" gets p.18's answer computed against live
    /// state; it never walks the board itself, which is milestone 6.4's fourth acceptance check.
    /// </summary>
    public interface IBoardDisplacementAuthority
    {
        BoardMapDefinition MapDefinition { get; }

        /// <summary>
        /// Rulebook p.8's Battle Zone as authoritative state (milestone 6.10), and the named absence on
        /// a map that declares none. It is on this port rather than beside the map because the map says
        /// only where the match began; which Zone the Lane has since reached is a fact about the match,
        /// and every question that asks it - the Minion Battle, the p.13 Heavy-immunity accompaniment,
        /// p.18's displaced-minion correction, p.8's own Lane-Push cause - already arrives here.
        /// </summary>
        ZoneId? CurrentBattleZoneId { get; }

        IReadOnlyList<GuardsDigital.Core.Units.Heroes.HeroOccupancy> Occupancies { get; }

        IReadOnlyList<GuardsDigital.Core.Units.Minions.MinionOccupancy> Minions { get; }

        /// <summary>
        /// The object occupying <paramref name="coordinate"/>, or <c>false</c> when the space is empty.
        ///
        /// A coordinate is the identity a displacement target arrives as, because p.18 admits no second
        /// obstacle in a space, so a space names its occupant unambiguously for every kind at once. That
        /// is the same reasoning the token reclamation capability already records for its candidates,
        /// and it keeps a token target expressible without widening the typed effect value union.
        /// </summary>
        bool TryGetObjectAt(HexCoordinate coordinate, out BoardObjectReference subject);

        /// <summary>Where a displaceable object currently stands, or <c>false</c> when it is not on the board.</summary>
        bool TryGetObjectCoordinate(BoardObjectReference subject, out HexCoordinate coordinate);

        /// <summary>
        /// Rulebook p.18's Push geometry against current occupancy. Returns where the target ends up,
        /// including the zero-space result the rule explicitly allows; it commits nothing.
        /// </summary>
        PushPathResult ResolvePush(
            HexCoordinate pusher,
            HexCoordinate target,
            int requestedDistance,
            RuleObject targetObject,
            bool pushIgnoresObstacles,
            RuleModificationSet modifications);

        /// <summary>
        /// The spaces a displaceable object may reach with a direct Move of this allowance, each with
        /// its route and cost. Rulebook p.11: movement in the card text "follows the same rules and
        /// restrictions as a Movement action", so this is the same reachability the Movement action
        /// uses rather than a second traversal written for effects.
        ///
        /// The subject is any board object since milestone 6.4a, because p.11's sentence is "move, or
        /// to move another unit or a token in the card text". A token holds no Move-Through permission
        /// of its own - p.19 says tokens are not units - so it reaches the transit predicate as the
        /// named absence rather than as a unit with nothing granted.
        /// </summary>
        IReadOnlyList<GuardsDigital.Core.Rules.Movement.ReachableMovementDestination> FindDirectMoveDestinations(
            BoardObjectReference mover,
            int allowance,
            RuleModificationSet modifications);

        /// <summary>
        /// The shortest routes of empty spaces from <paramref name="origin"/>, for milestone 6.4a's
        /// displaced-minion correction. p.18 returns a minion "by the shortest path of empty spaces",
        /// which is stricter than movement: a correcting minion passes through nothing at all.
        /// </summary>
        IReadOnlyList<GuardsDigital.Core.Rules.Movement.ReachableMovementDestination> FindEmptyPathRoutes(
            HexCoordinate origin,
            int maximumDistance);

        /// <summary>
        /// Relocates an object whose destination the caller has already validated, and reports what
        /// moved and from where.
        ///
        /// Displacement is deliberately not movement. p.18 classes Push as its own operation and p.11
        /// keeps direct Move distinct from a Movement action, so this commit emits no movement event of
        /// its own; the calling capability publishes the typed facts its own rule owns. Rejections are
        /// returned rather than thrown because a stale validated destination is a real race the effect
        /// frame must report, not an engine defect.
        /// </summary>
        DisplacementCommitResult CommitValidatedDisplacement(
            BoardObjectReference subject,
            HexCoordinate destination);
    }
}
