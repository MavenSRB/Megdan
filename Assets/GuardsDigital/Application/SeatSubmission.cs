using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Replay;

namespace GuardsDigital.Application
{
    /// <summary>
    /// Milestone 8.30a. One submission's unique identity, so a repeat can be recognized as a repeat.
    /// </summary>
    /// <remarks>
    /// Chosen by the client and meaningful only to the host that saw it. It carries no ownership
    /// claim: two seats could pick the same string and neither would gain anything by it, because
    /// who is asking comes from the seat the submission arrived on.
    /// </remarks>
    public readonly struct SubmissionId : IEquatable<SubmissionId>
    {
        public SubmissionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("A submission id requires a value.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(SubmissionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is SubmissionId other && Equals(other);

        public override int GetHashCode() =>
            Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? string.Empty;
    }

    /// <summary>
    /// Milestone 8.30a. One player submission: what is being asked, which view it was decided
    /// against, and which attempt this is.
    /// </summary>
    /// <remarks>
    /// <para>Who is asking is deliberately not here. The seat arrives with the submission - as an
    /// argument locally, as the connection it was received on from 8.31 - and the host resolves the
    /// player, hero and team from that binding and the authoritative participant graph. A
    /// <c>HeroId</c> or <c>TeamSide</c> inside <see cref="Command"/> names what the answer is about;
    /// it is never read as who sent it.</para>
    /// <para>There is no Undo, restart or snapshot here, and none reachable from the client that
    /// carries it: those are the Director's, and a player surface must not be able to name them.</para>
    /// </remarks>
    public sealed class SeatSubmission
    {
        public SeatSubmission(SubmissionId id, long expectedVersion, DirectorCommand command)
        {
            if (!id.IsDefined)
            {
                throw new ArgumentException("A submission requires an identity.", nameof(id));
            }

            if (expectedVersion < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(expectedVersion), expectedVersion, "A projection version is not negative.");
            }

            Id = id;
            ExpectedVersion = expectedVersion;
            Command = command ?? throw new ArgumentNullException(nameof(command));
        }

        public SubmissionId Id { get; }

        /// <summary>
        /// The <see cref="SeatProjection.Version"/> this answer was decided against. The host
        /// refuses anything else, so an answer to a board that has moved on is never applied to the
        /// board that replaced it.
        /// </summary>
        public long ExpectedVersion { get; }

        public DirectorCommand Command { get; }
    }

    /// <summary>
    /// Milestone 8.30a. Why a seat's submission was refused.
    /// </summary>
    /// <remarks>
    /// Distinct from both <see cref="MatchHostRejectionReason"/> and
    /// <see cref="MatchSessionRejectionReason"/>, and for the same reason those two are distinct
    /// from each other: a client must be able to tell "you are not allowed to ask that" from "the
    /// rules said no", and neither from "the host was busy".
    /// </remarks>
    public enum SeatSubmissionRejectionReason
    {
        None = 0,

        /// <summary>No seat of that id is bound in this match.</summary>
        UnknownSeat = 1,

        /// <summary>
        /// The submission names a version that is not the current one - older, because the board
        /// moved, or newer, because the client is out of step. Both are answered the same way: the
        /// seat re-reads its projection and decides again.
        /// </summary>
        StaleProjection = 2,

        /// <summary>That submission identity has already reached the authority. It is never run twice.</summary>
        DuplicateSubmission = 3,

        /// <summary>
        /// The payload claims an identity this seat does not own - another hero's commitment, another
        /// team's tied-Initiative answer, another hero's respawn.
        /// </summary>
        OwnershipMismatch = 4,

        /// <summary>
        /// The command is not in the set this seat's own projection published, whatever the rules
        /// would have said about it.
        /// </summary>
        UnauthorizedCommand = 5,

        /// <summary>The authority itself refused; its own reason is carried beside this one.</summary>
        SessionRefused = 6,

        /// <summary>The host refused before the authority was reached.</summary>
        HostRefused = 7
    }

    /// <summary>
    /// Milestone 8.30a. What the host told one seat about its own submission.
    /// </summary>
    public sealed class SeatSubmissionResult
    {
        internal SeatSubmissionResult(
            SeatSubmission submission,
            bool accepted,
            SeatSubmissionRejectionReason rejectionReason,
            MatchSessionRejectionReason sessionRejectionReason,
            string detail,
            long version,
            long sequence,
            SeatProjection projection)
        {
            Submission = submission;
            Accepted = accepted;
            RejectionReason = rejectionReason;
            SessionRejectionReason = sessionRejectionReason;
            Detail = detail;
            Version = version;
            Sequence = sequence;
            Projection = projection;
        }

        public SeatSubmission Submission { get; }
        public bool Accepted { get; }
        public SeatSubmissionRejectionReason RejectionReason { get; }

        /// <summary>The authority's reason, and <c>None</c> when the host refused before reaching it.</summary>
        public MatchSessionRejectionReason SessionRejectionReason { get; }

        public string Detail { get; }

        /// <summary>The authoritative version after this submission; unmoved by a refusal.</summary>
        public long Version { get; }

        /// <summary>The host stream sequence this submission appended, and <c>0</c> when refused.</summary>
        public long Sequence { get; }

        /// <summary>This seat's view after the submission, refused or not.</summary>
        public SeatProjection Projection { get; }
    }

    /// <summary>
    /// Milestone 8.30a. One seat's whole view of, and access to, the match.
    /// </summary>
    /// <remarks>
    /// <para>The contract a network client will be given at 8.32 and the seat-scoped HUD will be
    /// given at 8.30b. Its seat is fixed at construction and cannot be named by a caller, so this
    /// object cannot be asked about another seat, cannot submit as one, and carries no Director
    /// authority at all.</para>
    /// </remarks>
    public interface ISeatClient
    {
        MatchSeat Seat { get; }

        /// <summary>This seat's current authorized view.</summary>
        SeatProjection Projection { get; }

        SeatSubmissionResult Submit(SeatSubmission submission);
    }

    /// <summary>
    /// Milestone 8.30a. The identity a command's payload claims to be acting <em>as</em>, which is
    /// almost never the identity it names.
    /// </summary>
    public readonly struct SeatIdentityClaim
    {
        private SeatIdentityClaim(HeroId heroId, TeamSide teamSide, bool isDefined)
        {
            HeroId = heroId;
            TeamSide = teamSide;
            IsDefined = isDefined;
        }

        public static readonly SeatIdentityClaim None =
            new SeatIdentityClaim(default, TeamSide.None, false);

        internal static SeatIdentityClaim ForHero(HeroId heroId) =>
            new SeatIdentityClaim(heroId, TeamSide.None, true);

        internal static SeatIdentityClaim ForTeam(TeamSide side) =>
            new SeatIdentityClaim(default, side, true);

        public HeroId HeroId { get; }
        public TeamSide TeamSide { get; }

        /// <summary>Whether the payload claims a caller identity at all. Most commands do not.</summary>
        public bool IsDefined { get; }

        public bool IsHeldBy(MatchSeat seat)
        {
            if (!IsDefined)
            {
                return true;
            }

            if (seat == null)
            {
                return false;
            }

            return TeamSide == TeamSide.None ? seat.Owns(HeroId) : seat.PlaysFor(TeamSide);
        }

        public override string ToString()
        {
            if (!IsDefined)
            {
                return "no caller identity";
            }

            return TeamSide == TeamSide.None ? "hero " + HeroId.Value : "side " + TeamSide;
        }
    }

    /// <summary>
    /// Milestone 8.30a. Which commands carry a caller identity in their payload, and which merely
    /// name something.
    /// </summary>
    /// <remarks>
    /// <para>This distinction is the whole of the acceptance check that a stable identity inside a
    /// <see cref="DirectorCommand"/> "identifies the requested action or choice and is never
    /// accepted as caller identity". Four commands are answers a hero gives <em>about itself</em> -
    /// its own commitment, its own Pass, its own respawn - and one is an answer a team gives; every
    /// other <c>HeroId</c> in the vocabulary is a target, a subject or a candidate.</para>
    /// <para><c>ChooseAttackTarget</c> is the case worth stating out loud: its <c>HeroId</c> is who
    /// is being attacked. Reading that as the caller would let a seat attack on someone else's turn
    /// by naming itself, and would refuse the ordinary case of attacking anyone at all.</para>
    /// </remarks>
    public static class SeatCommandOwnership
    {
        /// <summary>Commands whose <c>HeroId</c> is the hero making the choice.</summary>
        public static readonly IReadOnlyList<DirectorCommandKind> HeroClaimingKinds =
            Array.AsReadOnly(new[]
            {
                DirectorCommandKind.CommitCard,
                DirectorCommandKind.Pass,
                DirectorCommandKind.RespawnHero,
                DirectorCommandKind.ResolveCardWithoutRespawn
            });

        /// <summary>Commands whose <c>TeamSide</c> is the side making the choice.</summary>
        public static readonly IReadOnlyList<DirectorCommandKind> TeamClaimingKinds =
            Array.AsReadOnly(new[] { DirectorCommandKind.ChooseTiedActor });

        public static SeatIdentityClaim Claim(DirectorCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (HeroClaimingKinds.Contains(command.Kind))
            {
                return SeatIdentityClaim.ForHero(command.HeroId);
            }

            if (TeamClaimingKinds.Contains(command.Kind))
            {
                return SeatIdentityClaim.ForTeam(command.TeamSide);
            }

            return SeatIdentityClaim.None;
        }
    }

    internal sealed class SeatClient : ISeatClient
    {
        private readonly IMatchHost host;

        public SeatClient(IMatchHost host, MatchSeat seat)
        {
            this.host = host ?? throw new ArgumentNullException(nameof(host));
            Seat = seat ?? throw new ArgumentNullException(nameof(seat));
        }

        public MatchSeat Seat { get; }

        public SeatProjection Projection => host.ProjectionForSeat(Seat.Id);

        public SeatSubmissionResult Submit(SeatSubmission submission) =>
            host.SubmitForSeat(Seat.Id, submission);
    }
}
