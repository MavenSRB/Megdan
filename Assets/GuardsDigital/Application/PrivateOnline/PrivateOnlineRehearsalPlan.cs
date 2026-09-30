using System;
using System.Collections.Generic;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Application
{
    /// <summary>The independently addressable case families in milestone 8.35c's Full rehearsal.</summary>
    public enum PrivateOnlineRehearsalCaseCategory
    {
        ProjectionDrop = 1,
        BoundedDelay = 2,
        DuplicateDelivery = 3,
        TwoMessageReorder = 4,
        CompletedSubmissionReplay = 5,
        ProjectionVersion = 6,
        UnknownMessageKind = 7,
        UnknownDiscriminator = 8,
        FrameTruncation = 9,
        MismatchedLength = 10,
        ProtocolLimitExceeded = 11,
        ForgedEnvelopeIdentity = 12,
        PendingDecisionReconnect = 13,
        ReconnectEligibilityExpiry = 14,
        HostTermination = 15
    }

    /// <summary>The three claimed projection-version relationships exercised by the Full plan.</summary>
    public enum PrivateOnlineRehearsalVersionRelation
    {
        None = 0,
        Stale = 1,
        Current = 2,
        Future = 3
    }

    /// <summary>Every identity-bearing field in <see cref="NetworkEnvelope"/>.</summary>
    public enum PrivateOnlineRehearsalEnvelopeField
    {
        None = 0,
        SessionCorrelation = 1,
        Epoch = 2,
        Sequence = 3,
        Correlation = 4
    }

    /// <summary>The five reconnect cuts required for each configured pending-decision family.</summary>
    public enum PrivateOnlineRehearsalReconnectCutPoint
    {
        None = 0,
        BeforeAnswer = 1,
        AfterSendBeforeResponse = 2,
        DuplicateAfterReconnect = 3,
        StaleAfterResync = 4,
        SuccessfulResume = 5
    }

    /// <summary>
    /// One immutable, stably named Full-plan case. The seed and smallest reproducing message-prefix
    /// length travel with the case rather than living in runner code or diagnostic prose.
    /// </summary>
    public sealed class PrivateOnlineRehearsalCase
    {
        internal PrivateOnlineRehearsalCase(
            string id,
            PrivateOnlineRehearsalCaseCategory category,
            int seed,
            int smallestMessagePrefixLength,
            PrivateOnlineRehearsalVersionRelation versionRelation =
                PrivateOnlineRehearsalVersionRelation.None,
            PrivateOnlineRehearsalEnvelopeField envelopeField =
                PrivateOnlineRehearsalEnvelopeField.None,
            PendingDecisionFamilyId pendingFamily = default(PendingDecisionFamilyId),
            PrivateOnlineRehearsalReconnectCutPoint reconnectCutPoint =
                PrivateOnlineRehearsalReconnectCutPoint.None,
            string limitId = null,
            int? declaredLimit = null,
            int? exercisedValue = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A rehearsal case requires a stable ID.", nameof(id));
            }

            if (seed < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(seed), seed,
                    "A rehearsal case seed is positive and fixed.");
            }

            if (smallestMessagePrefixLength < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(smallestMessagePrefixLength), smallestMessagePrefixLength,
                    "A rehearsal case records a positive reproducing message prefix.");
            }

            var hasPendingFamily = pendingFamily.IsDefined;
            var hasReconnectCutPoint = reconnectCutPoint !=
                PrivateOnlineRehearsalReconnectCutPoint.None;
            if (hasPendingFamily != hasReconnectCutPoint ||
                (category == PrivateOnlineRehearsalCaseCategory.PendingDecisionReconnect) !=
                hasPendingFamily)
            {
                throw new ArgumentException(
                    "A pending-decision case requires exactly one family and reconnect cut point.");
            }

            if ((category == PrivateOnlineRehearsalCaseCategory.ProjectionVersion) !=
                (versionRelation != PrivateOnlineRehearsalVersionRelation.None))
            {
                throw new ArgumentException(
                    "A projection-version case requires exactly one version relationship.");
            }

            if ((category == PrivateOnlineRehearsalCaseCategory.ForgedEnvelopeIdentity) !=
                (envelopeField != PrivateOnlineRehearsalEnvelopeField.None))
            {
                throw new ArgumentException(
                    "A forged-envelope case requires exactly one identity field.");
            }

            var hasLimitId = !string.IsNullOrWhiteSpace(limitId);
            if (hasLimitId != declaredLimit.HasValue || hasLimitId != exercisedValue.HasValue ||
                (category == PrivateOnlineRehearsalCaseCategory.ProtocolLimitExceeded) != hasLimitId)
            {
                throw new ArgumentException(
                    "A protocol-limit case requires one named declared limit and exercised value.");
            }

            if (hasLimitId &&
                (declaredLimit.Value < 0 || exercisedValue.Value != checked(declaredLimit.Value + 1)))
            {
                throw new ArgumentException(
                    "A protocol-limit case exercises exactly its declared limit plus one.");
            }

            Id = id;
            Category = category;
            Seed = seed;
            SmallestMessagePrefixLength = smallestMessagePrefixLength;
            VersionRelation = versionRelation;
            EnvelopeField = envelopeField;
            PendingFamily = pendingFamily;
            ReconnectCutPoint = reconnectCutPoint;
            LimitId = limitId;
            DeclaredLimit = declaredLimit;
            ExercisedValue = exercisedValue;
        }

        public string Id { get; }
        public PrivateOnlineRehearsalCaseCategory Category { get; }
        public int Seed { get; }
        public int SmallestMessagePrefixLength { get; }
        public PrivateOnlineRehearsalVersionRelation VersionRelation { get; }
        public PrivateOnlineRehearsalEnvelopeField EnvelopeField { get; }
        public PendingDecisionFamilyId PendingFamily { get; }
        public PrivateOnlineRehearsalReconnectCutPoint ReconnectCutPoint { get; }
        public string LimitId { get; }
        public int? DeclaredLimit { get; }
        public int? ExercisedValue { get; }
    }

    /// <summary>
    /// Milestone 8.35c's Unity-free source of truth for the exact six-client topology and Full
    /// deterministic fault/reconnect plan. A player runner executes these cases; it does not keep
    /// a second handwritten inventory.
    /// </summary>
    public static class PrivateOnlineRehearsalPlan
    {
        public const int HostProcessCount = 1;
        public const int HostSeatCount = 0;
        public const int ClientProcessCount = PrivateSessionProtocol.MaxSeats;
        public const int TotalProcessCount = HostProcessCount + ClientProcessCount;

        static PrivateOnlineRehearsalPlan()
        {
            SeatIds = Array.AsReadOnly(new[]
            {
                new SeatId(OfficialSixPlayerParticipants.RedFirstPlayerIdValue),
                new SeatId(OfficialSixPlayerParticipants.RedSecondPlayerIdValue),
                new SeatId(OfficialSixPlayerParticipants.RedThirdPlayerIdValue),
                new SeatId(OfficialSixPlayerParticipants.BlueFirstPlayerIdValue),
                new SeatId(OfficialSixPlayerParticipants.BlueSecondPlayerIdValue),
                new SeatId(OfficialSixPlayerParticipants.BlueThirdPlayerIdValue)
            });

            PendingFamilies = Array.AsReadOnly(new[]
            {
                PendingDecisionFamilies.Initiative,
                PendingDecisionFamilies.Movement,
                PendingDecisionFamilies.AttackTarget,
                PendingDecisionFamilies.Defense,
                PendingDecisionFamilies.Effect,
                PendingDecisionFamilies.Correction,
                PendingDecisionFamilies.MinionBattle,
                PendingDecisionFamilies.HeroRespawn,
                PendingDecisionFamilies.SystemTransition,
                PendingDecisionFamilies.RemovalPrevention,
                // Milestone 9.21j. "Which of you acts" (9.21b, RZ-Q4), dealt with Razzle: the
                // eleventh family a seat may be disconnected on, staged by the reconnect matrix on
                // his two-bodied board.
                PendingDecisionFamilies.ActingBody
            });

            ReconnectCutPoints = Array.AsReadOnly(new[]
            {
                PrivateOnlineRehearsalReconnectCutPoint.BeforeAnswer,
                PrivateOnlineRehearsalReconnectCutPoint.AfterSendBeforeResponse,
                PrivateOnlineRehearsalReconnectCutPoint.DuplicateAfterReconnect,
                PrivateOnlineRehearsalReconnectCutPoint.StaleAfterResync,
                PrivateOnlineRehearsalReconnectCutPoint.SuccessfulResume
            });

            FullCases = Array.AsReadOnly(CreateFullCases().ToArray());
        }

        /// <summary>The six official player identities, reused as the private session's seat IDs.</summary>
        public static IReadOnlyList<SeatId> SeatIds { get; }

        /// <summary>
        /// The exact eleven production pending-decision families a seat can be disconnected on
        /// (milestone 9.21j added the acting body); the turn release is walked by the reconnect
        /// matrix but is not a cut-point family of this plan.
        /// </summary>
        public static IReadOnlyList<PendingDecisionFamilyId> PendingFamilies { get; }

        /// <summary>The exact five required reconnect cut points.</summary>
        public static IReadOnlyList<PrivateOnlineRehearsalReconnectCutPoint> ReconnectCutPoints { get; }

        /// <summary>Every independently named case in <c>-FaultPlan Full</c>.</summary>
        public static IReadOnlyList<PrivateOnlineRehearsalCase> FullCases { get; }

        private static List<PrivateOnlineRehearsalCase> CreateFullCases()
        {
            var cases = new List<PrivateOnlineRehearsalCase>();

            Add(cases, "projection-drop", PrivateOnlineRehearsalCaseCategory.ProjectionDrop, 1);
            Add(cases, "projection-delay-bounded", PrivateOnlineRehearsalCaseCategory.BoundedDelay, 1);
            Add(cases, "projection-duplicate-first", PrivateOnlineRehearsalCaseCategory.DuplicateDelivery, 1);
            Add(cases, "projection-duplicate-second", PrivateOnlineRehearsalCaseCategory.DuplicateDelivery, 2);
            Add(cases, "message-reorder-two", PrivateOnlineRehearsalCaseCategory.TwoMessageReorder, 2);
            Add(cases, "submission-replay-completed",
                PrivateOnlineRehearsalCaseCategory.CompletedSubmissionReplay, 2);

            AddVersion(cases, PrivateOnlineRehearsalVersionRelation.Stale, "stale");
            AddVersion(cases, PrivateOnlineRehearsalVersionRelation.Current, "current");
            AddVersion(cases, PrivateOnlineRehearsalVersionRelation.Future, "future");

            Add(cases, "message-kind-unknown",
                PrivateOnlineRehearsalCaseCategory.UnknownMessageKind, 1);
            Add(cases, "command-discriminator-unknown",
                PrivateOnlineRehearsalCaseCategory.UnknownDiscriminator, 1);
            Add(cases, "frame-truncated", PrivateOnlineRehearsalCaseCategory.FrameTruncation, 1);
            Add(cases, "frame-length-mismatched",
                PrivateOnlineRehearsalCaseCategory.MismatchedLength, 1);

            AddLimit(cases, "frame-payload-bytes", PrivateSessionProtocol.MaxPayloadBytes);
            AddLimit(cases, "sealed-payload-bytes",
                PrivateSessionSecureChannel.MaxSealedPayloadBytes);
            AddLimit(cases, "challenge-nonce-bytes", PrivateSessionProtocol.MaxNonceBytes);
            AddLimit(cases, "reject-detail-characters",
                PrivateSessionProtocol.MaxRejectDetailChars);
            AddLimit(cases, "seat-count", PrivateSessionProtocol.MaxSeats);
            AddLimit(cases, "concurrent-connection-count",
                PrivateSessionProtocol.MaxConcurrentConnections);
            AddLimit(cases, "reconnect-capability-characters",
                PrivateSessionProtocol.MaxReconnectCapabilityChars);
            AddLimit(cases, "submission-identity-characters",
                NetworkMessageCodec.MaxSubmissionIdChars);
            AddLimit(cases, "command-token-selection-count", NetworkCommandCodec.MaxTokenSelections);
            AddLimit(cases, "projection-list-entry-count", NetworkSeatProjectionCodec.MaxListEntries);
            AddLimit(cases, "submission-idempotency-window-count",
                PrivateSessionProtocol.SubmissionIdempotencyWindowEntries);

            AddEnvelope(cases, PrivateOnlineRehearsalEnvelopeField.SessionCorrelation,
                "session-correlation");
            AddEnvelope(cases, PrivateOnlineRehearsalEnvelopeField.Epoch,
                "connection-epoch");
            AddEnvelope(cases, PrivateOnlineRehearsalEnvelopeField.Sequence, "sequence");
            AddEnvelope(cases, PrivateOnlineRehearsalEnvelopeField.Correlation, "correlation");

            foreach (var family in PendingFamilies)
            {
                foreach (var cutPoint in ReconnectCutPoints)
                {
                    var id = "reconnect-" + family.Value + "-" + CutPointId(cutPoint);
                    cases.Add(new PrivateOnlineRehearsalCase(
                        id,
                        PrivateOnlineRehearsalCaseCategory.PendingDecisionReconnect,
                        FixedSeedFor(id),
                        (int)cutPoint,
                        pendingFamily: family,
                        reconnectCutPoint: cutPoint));
                }
            }

            Add(cases, "reconnect-eligibility-expired",
                PrivateOnlineRehearsalCaseCategory.ReconnectEligibilityExpiry, 1);
            Add(cases, "host-terminated", PrivateOnlineRehearsalCaseCategory.HostTermination, 1);

            return cases;
        }

        private static void Add(
            ICollection<PrivateOnlineRehearsalCase> cases,
            string id,
            PrivateOnlineRehearsalCaseCategory category,
            int smallestMessagePrefixLength)
        {
            cases.Add(new PrivateOnlineRehearsalCase(
                id, category, FixedSeedFor(id), smallestMessagePrefixLength));
        }

        private static void AddVersion(
            ICollection<PrivateOnlineRehearsalCase> cases,
            PrivateOnlineRehearsalVersionRelation relation,
            string idPart)
        {
            var id = "projection-version-" + idPart;
            cases.Add(new PrivateOnlineRehearsalCase(
                id,
                PrivateOnlineRehearsalCaseCategory.ProjectionVersion,
                FixedSeedFor(id),
                1,
                versionRelation: relation));
        }

        private static void AddEnvelope(
            ICollection<PrivateOnlineRehearsalCase> cases,
            PrivateOnlineRehearsalEnvelopeField field,
            string idPart)
        {
            var id = "envelope-" + idPart + "-forged";
            cases.Add(new PrivateOnlineRehearsalCase(
                id,
                PrivateOnlineRehearsalCaseCategory.ForgedEnvelopeIdentity,
                FixedSeedFor(id),
                1,
                envelopeField: field));
        }

        private static void AddLimit(
            ICollection<PrivateOnlineRehearsalCase> cases,
            string limitId,
            int declaredLimit)
        {
            var id = "limit-" + limitId + "-plus-one";
            cases.Add(new PrivateOnlineRehearsalCase(
                id,
                PrivateOnlineRehearsalCaseCategory.ProtocolLimitExceeded,
                FixedSeedFor(id),
                1,
                limitId: limitId,
                declaredLimit: declaredLimit,
                exercisedValue: checked(declaredLimit + 1)));
        }

        private static string CutPointId(PrivateOnlineRehearsalReconnectCutPoint cutPoint)
        {
            switch (cutPoint)
            {
                case PrivateOnlineRehearsalReconnectCutPoint.BeforeAnswer:
                    return "before-answer";
                case PrivateOnlineRehearsalReconnectCutPoint.AfterSendBeforeResponse:
                    return "after-send-before-response";
                case PrivateOnlineRehearsalReconnectCutPoint.DuplicateAfterReconnect:
                    return "duplicate-after-reconnect";
                case PrivateOnlineRehearsalReconnectCutPoint.StaleAfterResync:
                    return "stale-after-resync";
                case PrivateOnlineRehearsalReconnectCutPoint.SuccessfulResume:
                    return "successful-resume";
                default:
                    throw new ArgumentOutOfRangeException(nameof(cutPoint), cutPoint,
                        "A Full-plan reconnect case requires a declared cut point.");
            }
        }

        /// <summary>Stable FNV-1a over the stable case ID; never runtime-randomized string hashing.</summary>
        private static int FixedSeedFor(string id)
        {
            unchecked
            {
                uint hash = 2166136261;
                foreach (var character in id)
                {
                    hash ^= character;
                    hash *= 16777619;
                }

                var seed = (int)(hash & 0x7fffffff);
                return seed == 0 ? 1 : seed;
            }
        }
    }
}
