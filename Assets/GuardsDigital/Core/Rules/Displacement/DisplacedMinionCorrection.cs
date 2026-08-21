using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Rules.Placement;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>How one displaced minion is corrected: back along an empty path, or Placed when no path exists.</summary>
    public enum DisplacedMinionCorrectionKind
    {
        None = 0,
        Move = 1,
        Place = 2
    }

    /// <summary>
    /// Which decision the pending correction is waiting on. The subject stage exists per the
    /// 2026-08-16 amendment: within one team the owning team chooses which of its displaced minions
    /// corrects next, because one correction can free or block another's shortest path.
    /// </summary>
    public enum DisplacedMinionCorrectionStage
    {
        None = 0,
        SelectSubject = 1,
        SelectDestination = 2
    }

    public enum DisplacedMinionCorrectionCommandKind
    {
        None = 0,
        SelectSubject = 1,
        SelectDestination = 2
    }

    /// <summary>The projected typed invocation a pending correction choice is answered with.</summary>
    public sealed class DisplacedMinionCorrectionCommand : IEquatable<DisplacedMinionCorrectionCommand>
    {
        public DisplacedMinionCorrectionCommand(
            DisplacedMinionCorrectionCommandKind kind,
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            TeamSide choosingSide,
            MinionId minionId,
            HexCoordinate destination)
        {
            if (kind == DisplacedMinionCorrectionCommandKind.None)
            {
                throw new ArgumentException("A correction command requires a kind.", nameof(kind));
            }

            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("A correction command names its resolution and choice.");
            }

            if (choosingSide == TeamSide.None)
            {
                throw new ArgumentException("A correction command names its choosing side.", nameof(choosingSide));
            }

            if (kind == DisplacedMinionCorrectionCommandKind.SelectSubject && !minionId.IsDefined)
            {
                throw new ArgumentException("A subject selection names the minion.", nameof(minionId));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoosingSide = choosingSide;
            MinionId = minionId;
            Destination = destination;
        }

        public DisplacedMinionCorrectionCommandKind Kind { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public TeamSide ChoosingSide { get; }
        public MinionId MinionId { get; }
        public HexCoordinate Destination { get; }

        public bool Equals(DisplacedMinionCorrectionCommand other) =>
            other != null && Kind == other.Kind && ResolutionId.Equals(other.ResolutionId) &&
            ChoiceId.Equals(other.ChoiceId) && ChoosingSide == other.ChoosingSide &&
            MinionId.Equals(other.MinionId) && Destination.Equals(other.Destination);

        public override bool Equals(object obj) => Equals(obj as DisplacedMinionCorrectionCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ ResolutionId.GetHashCode();
                hash = (hash * 397) ^ ChoiceId.GetHashCode();
                hash = (hash * 397) ^ (int)ChoosingSide;
                hash = (hash * 397) ^ MinionId.GetHashCode();
                return (hash * 397) ^ Destination.GetHashCode();
            }
        }
    }

    /// <summary>A displaced minion offered for the subject stage: its identity and where it stands.</summary>
    public readonly struct DisplacedMinionCorrectionSubject : IEquatable<DisplacedMinionCorrectionSubject>
    {
        public DisplacedMinionCorrectionSubject(MinionId minionId, HexCoordinate coordinate)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A correction subject requires a minion.", nameof(minionId));
            }

            MinionId = minionId;
            Coordinate = coordinate;
        }

        public MinionId MinionId { get; }
        public HexCoordinate Coordinate { get; }

        public bool Equals(DisplacedMinionCorrectionSubject other) =>
            MinionId.Equals(other.MinionId) && Coordinate.Equals(other.Coordinate);

        public override bool Equals(object obj) =>
            obj is DisplacedMinionCorrectionSubject other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (MinionId.GetHashCode() * 397) ^ Coordinate.GetHashCode();
            }
        }
    }

    /// <summary>
    /// One legal correction destination. A Move candidate carries its one deterministic disclosed
    /// route, the Movement projection's own precedent; a Place candidate has no route at all, because
    /// Place is not movement (p.18). Cost is route length for a Move and geometric distance for a
    /// Place, recorded so equal-shortest and equally-near stay visible facts rather than re-derived
    /// claims.
    /// </summary>
    public readonly struct DisplacedMinionCorrectionCandidate
    {
        public DisplacedMinionCorrectionCandidate(
            HexCoordinate destination, IEnumerable<HexCoordinate> route, int cost)
        {
            Destination = destination;
            Route = (route ?? Enumerable.Empty<HexCoordinate>()).ToArray();
            Cost = cost;
        }

        public HexCoordinate Destination { get; }
        public IReadOnlyList<HexCoordinate> Route { get; }
        public int Cost { get; }
    }

    /// <summary>
    /// The paused after-action correction: which action it belongs to, which team decides, and the
    /// exact legal candidates. Immutable; answering re-derives against the live board rather than
    /// trusting anything recorded here, exactly as the Minion Battle choice validates.
    /// </summary>
    public sealed class PendingDisplacedMinionCorrection
    {
        public PendingDisplacedMinionCorrection(
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId,
            DisplacedMinionCorrectionStage stage,
            TeamId choosingTeamId,
            TeamSide choosingSide,
            ZoneId battleZoneId,
            IEnumerable<DisplacedMinionCorrectionSubject> subjectCandidates,
            MinionId subjectMinionId,
            DisplacedMinionCorrectionKind kind,
            IEnumerable<DisplacedMinionCorrectionCandidate> destinationCandidates)
        {
            if (!resolutionId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !actionDefinitionId.IsDefined || !actorId.IsDefined)
            {
                throw new ArgumentException("A pending correction names its owning action.");
            }

            if (stage == DisplacedMinionCorrectionStage.None)
            {
                throw new ArgumentException("A pending correction requires a stage.", nameof(stage));
            }

            if (!choosingTeamId.IsDefined || choosingSide == TeamSide.None)
            {
                throw new ArgumentException("A pending correction names the deciding team.");
            }

            var subjects = (subjectCandidates ?? Enumerable.Empty<DisplacedMinionCorrectionSubject>()).ToArray();
            var destinations = (destinationCandidates ?? Enumerable.Empty<DisplacedMinionCorrectionCandidate>()).ToArray();
            if (stage == DisplacedMinionCorrectionStage.SelectSubject)
            {
                if (subjects.Length < 2)
                {
                    throw new ArgumentException(
                        "A subject stage exists only when the team has a real choice between minions.",
                        nameof(subjectCandidates));
                }

                if (subjectMinionId.IsDefined || kind != DisplacedMinionCorrectionKind.None || destinations.Length > 0)
                {
                    throw new ArgumentException("A subject stage carries no destination decision yet.");
                }
            }
            else
            {
                if (!subjectMinionId.IsDefined || kind == DisplacedMinionCorrectionKind.None)
                {
                    throw new ArgumentException("A destination stage names its subject and correction kind.");
                }

                if (destinations.Length < 2)
                {
                    throw new ArgumentException(
                        "A destination stage exists only when the team has a real choice between spaces; " +
                        "a single candidate is applied without a prompt.",
                        nameof(destinationCandidates));
                }

                if (subjects.Length > 0)
                {
                    throw new ArgumentException("A destination stage carries no subject candidates.");
                }
            }

            ResolutionId = resolutionId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            ActorId = actorId;
            Stage = stage;
            ChoosingTeamId = choosingTeamId;
            ChoosingSide = choosingSide;
            BattleZoneId = battleZoneId;
            SubjectCandidates = subjects;
            SubjectMinionId = subjectMinionId;
            Kind = kind;
            DestinationCandidates = destinations;
            ChoiceId = stage == DisplacedMinionCorrectionStage.SelectSubject
                ? new ActionResolutionStepId("correction-subject")
                : new ActionResolutionStepId($"correction-{subjectMinionId.Value}");
            SelectInvocations = stage == DisplacedMinionCorrectionStage.SelectSubject
                ? subjects
                    .Select(subject => new DisplacedMinionCorrectionCommand(
                        DisplacedMinionCorrectionCommandKind.SelectSubject,
                        resolutionId, ChoiceId, choosingSide, subject.MinionId, default))
                    .ToArray()
                : (IReadOnlyList<DisplacedMinionCorrectionCommand>)destinations
                    .Select(candidate => new DisplacedMinionCorrectionCommand(
                        DisplacedMinionCorrectionCommandKind.SelectDestination,
                        resolutionId, ChoiceId, choosingSide, subjectMinionId, candidate.Destination))
                    .ToArray();
        }

        public ActionResolutionId ResolutionId { get; }
        public CardInstanceId SourceCardInstanceId { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public HeroId ActorId { get; }
        public DisplacedMinionCorrectionStage Stage { get; }
        public TeamId ChoosingTeamId { get; }
        public TeamSide ChoosingSide { get; }
        public ZoneId BattleZoneId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public IReadOnlyList<DisplacedMinionCorrectionSubject> SubjectCandidates { get; }
        public MinionId SubjectMinionId { get; }
        public DisplacedMinionCorrectionKind Kind { get; }
        public IReadOnlyList<DisplacedMinionCorrectionCandidate> DestinationCandidates { get; }

        /// <summary>The exact typed invocations that can advance this choice, one per legal candidate.</summary>
        public IReadOnlyList<DisplacedMinionCorrectionCommand> SelectInvocations { get; }
    }

    /// <summary>What the planner found next: nothing left, one automatic correction, or a real choice.</summary>
    internal enum DisplacedMinionCorrectionStepKind
    {
        Finished = 0,
        Apply = 1,
        Choice = 2
    }

    internal readonly struct DisplacedMinionCorrectionStep
    {
        private DisplacedMinionCorrectionStep(
            DisplacedMinionCorrectionStepKind kind,
            MinionOccupancy subject,
            DisplacedMinionCorrectionKind correctionKind,
            DisplacedMinionCorrectionCandidate destination,
            PendingDisplacedMinionCorrection pending,
            IReadOnlyList<MinionOccupancy> leftInPlace)
        {
            Kind = kind;
            Subject = subject;
            CorrectionKind = correctionKind;
            Destination = destination;
            Pending = pending;
            LeftInPlace = leftInPlace ?? Array.Empty<MinionOccupancy>();
        }

        internal DisplacedMinionCorrectionStepKind Kind { get; }
        internal MinionOccupancy Subject { get; }
        internal DisplacedMinionCorrectionKind CorrectionKind { get; }
        internal DisplacedMinionCorrectionCandidate Destination { get; }
        internal PendingDisplacedMinionCorrection Pending { get; }

        /// <summary>Displaced minions no correction can reach, reported when the pass finishes.</summary>
        internal IReadOnlyList<MinionOccupancy> LeftInPlace { get; }

        internal static DisplacedMinionCorrectionStep Finished(IReadOnlyList<MinionOccupancy> leftInPlace) =>
            new DisplacedMinionCorrectionStep(
                DisplacedMinionCorrectionStepKind.Finished, null,
                DisplacedMinionCorrectionKind.None, default, null, leftInPlace);

        internal static DisplacedMinionCorrectionStep Apply(
            MinionOccupancy subject,
            DisplacedMinionCorrectionKind correctionKind,
            DisplacedMinionCorrectionCandidate destination) =>
            new DisplacedMinionCorrectionStep(
                DisplacedMinionCorrectionStepKind.Apply, subject, correctionKind, destination, null, null);

        internal static DisplacedMinionCorrectionStep Choice(PendingDisplacedMinionCorrection pending) =>
            new DisplacedMinionCorrectionStep(
                DisplacedMinionCorrectionStepKind.Choice, null,
                DisplacedMinionCorrectionKind.None, default, pending, null);
    }

    /// <summary>
    /// Derives the next correction from board position at the action boundary - p.18's "ends up
    /// outside the Battle Zone" is a positional predicate, so every step is re-derived from the live
    /// board and nothing is remembered about what moved. Pure: committing is the caller's business.
    ///
    /// Order across teams is the Tie Breaker's - the revealed side's minions resolve first, without
    /// flipping the coin. An unrevealed Tie Breaker is a synthetic-fixture state, pinned to Red first
    /// so the order stays deterministic rather than accidental. Within a team the owning team chooses
    /// (2026-08-16 amendment), so more than one correctable subject pauses on a subject selection;
    /// exactly one proceeds without a prompt.
    /// </summary>
    internal static class DisplacedMinionCorrectionPlanner
    {
        /// <summary>
        /// The correction fallback's declared Place (milestone 6.5). p.18: "If there is no path to the
        /// Battle Zone, or the minion cannot move, place that minion in the nearest empty space of the
        /// Battle Zone." Every field is that sentence: the piece comes off the board, it is a minion
        /// miniature and explicitly not an object that merely counts as one, the destinations are the
        /// nearest empty spaces of the declared zone, the minion's own team decides between equally
        /// near ones, and there is no no-destination arm to take because the planner has already
        /// filtered a subject with nowhere to go out of the correctable set.
        ///
        /// It is declared here rather than inside the operation because the operation is general and
        /// this rule is not: milestone 6.5's fifth acceptance check is exactly that this fallback stops
        /// being its own minimum placement and becomes one more caller of the shared Place.
        /// </summary>
        internal static PlacePolicy FallbackPlacePolicy { get; } = new PlacePolicy(
            PlaceSourceContainer.Board,
            PlaceEligiblePieceKind.MinionMiniature,
            PlaceDestinationScope.NearestEmptySpacesInScope,
            0,
            PlaceChooserKind.OwningTeam,
            PlaceNoDestinationPolicy.Impossible,
            PlaceDestinationAnchor.PlacedPiece);

        internal static DisplacedMinionCorrectionStep DeriveNext(
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            TieBreakerState tieBreaker,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));

            // A board with no current Battle Zone has no p.18 correction to run - the single-zone
            // synthetic boards predate the battle-zone concept, and on them every minion would
            // otherwise read as displaced.
            if (!BattleZoneQuery.TryGetCurrentBattleZone(board, out var zone))
            {
                return DisplacedMinionCorrectionStep.Finished(Array.Empty<MinionOccupancy>());
            }

            var displaced = BattleZoneQuery.FindMinionsOutside(board);
            if (displaced.Count == 0)
            {
                return DisplacedMinionCorrectionStep.Finished(Array.Empty<MinionOccupancy>());
            }
            var zoneCoordinates = BattleZoneQuery.FindCurrentBattleZoneCoordinates(board);
            var firstSide = tieBreaker.IsDefined ? tieBreaker.RevealedSide : TeamSide.Red;
            var secondSide = firstSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;

            foreach (var side in new[] { firstSide, secondSide })
            {
                var subjects = displaced.Where(minion => minion.TeamSide == side).ToArray();
                var correctable = subjects
                    .Where(subject => DeriveCandidates(board, zoneCoordinates, subject, out _).Count > 0)
                    .ToArray();
                if (correctable.Length == 0)
                {
                    continue;
                }

                if (correctable.Length > 1)
                {
                    return DisplacedMinionCorrectionStep.Choice(new PendingDisplacedMinionCorrection(
                        resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                        DisplacedMinionCorrectionStage.SelectSubject,
                        participants.Teams.Single(team => team.Side == side).Id,
                        side,
                        zone.Id,
                        correctable.Select(subject =>
                            new DisplacedMinionCorrectionSubject(subject.MinionId, subject.Coordinate)),
                        default,
                        DisplacedMinionCorrectionKind.None,
                        null));
                }

                return DeriveDestinationStep(
                    board, participants, zone.Id, zoneCoordinates, correctable[0],
                    resolutionId, sourceCardInstanceId, actionDefinitionId, actorId);
            }

            // Displaced minions remain and none can be corrected: they stay where they ended up, a
            // typed outcome rather than a fault (open rules question 10).
            return DisplacedMinionCorrectionStep.Finished(displaced);
        }

        /// <summary>
        /// The destination step for one named subject: automatic when exactly one candidate exists,
        /// a real choice otherwise. Also the validation shape a restored or answered stage runs.
        /// </summary>
        internal static DisplacedMinionCorrectionStep DeriveDestinationStep(
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            ZoneId battleZoneId,
            IReadOnlyList<HexCoordinate> zoneCoordinates,
            MinionOccupancy subject,
            ActionResolutionId resolutionId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            HeroId actorId)
        {
            var candidates = DeriveCandidates(board, zoneCoordinates, subject, out var kind);
            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "A destination step requires a correctable subject; the planner filters these.");
            }

            if (candidates.Count == 1)
            {
                return DisplacedMinionCorrectionStep.Apply(subject, kind, candidates[0]);
            }

            return DisplacedMinionCorrectionStep.Choice(new PendingDisplacedMinionCorrection(
                resolutionId, sourceCardInstanceId, actionDefinitionId, actorId,
                DisplacedMinionCorrectionStage.SelectDestination,
                participants.Teams.Single(team => team.Side == subject.TeamSide).Id,
                subject.TeamSide,
                battleZoneId,
                null,
                subject.MinionId,
                kind,
                candidates));
        }

        /// <summary>
        /// One subject's legal correction candidates. A shortest empty path into the zone wins; with
        /// no path at all, the equally-nearest empty zone spaces are the Place fallback; with no empty
        /// zone space either, the subject is uncorrectable and the caller records it left in place.
        /// </summary>
        internal static IReadOnlyList<DisplacedMinionCorrectionCandidate> DeriveCandidates(
            AdjacentMoveAuthority board,
            IReadOnlyList<HexCoordinate> zoneCoordinates,
            MinionOccupancy subject,
            out DisplacedMinionCorrectionKind kind)
        {
            var zoneSet = new HashSet<HexCoordinate>(zoneCoordinates);
            var routes = board.FindEmptyPathRoutes(
                    subject.Coordinate, board.MapDefinition.PlayableCoordinates.Count)
                .Where(destination => zoneSet.Contains(destination.Destination))
                .ToArray();
            if (routes.Length > 0)
            {
                kind = DisplacedMinionCorrectionKind.Move;
                var shortest = routes.Min(destination => destination.Cost);
                return routes
                    .Where(destination => destination.Cost == shortest)
                    .OrderBy(destination => destination.Destination.Q)
                    .ThenBy(destination => destination.Destination.R)
                    .Select(destination => new DisplacedMinionCorrectionCandidate(
                        destination.Destination, destination.Route, destination.Cost))
                    .ToArray();
            }

            // The fallback's destinations are the general Place's own question, asked through the
            // shared query rather than re-derived here (milestone 6.5).
            var placements = PlaceDestinationQuery.FindDestinations(
                FallbackPlacePolicy, board, subject.Coordinate, zoneCoordinates);
            if (placements.Count == 0)
            {
                kind = DisplacedMinionCorrectionKind.None;
                return Array.Empty<DisplacedMinionCorrectionCandidate>();
            }

            kind = DisplacedMinionCorrectionKind.Place;
            return placements
                .Select(destination => new DisplacedMinionCorrectionCandidate(
                    destination,
                    Enumerable.Empty<HexCoordinate>(),
                    HexDistanceQuery.Calculate(subject.Coordinate, destination)))
                .ToArray();
        }
    }
}
