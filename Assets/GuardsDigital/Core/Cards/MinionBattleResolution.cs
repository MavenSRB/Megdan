using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Setup;

namespace GuardsDigital.Core.Cards
{
    public readonly struct EndOfRoundResolutionId : IEquatable<EndOfRoundResolutionId>
    {
        public EndOfRoundResolutionId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("End-of-round resolution ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EndOfRoundResolutionId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is EndOfRoundResolutionId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    public readonly struct EndOfRoundChoiceId : IEquatable<EndOfRoundChoiceId>
    {
        public EndOfRoundChoiceId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("End-of-round choice ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }

        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);

        public bool Equals(EndOfRoundChoiceId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is EndOfRoundChoiceId other && Equals(other);

        public override int GetHashCode() =>
            StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);

        public override string ToString() => Value ?? string.Empty;
    }

    public enum MinionBattleChoiceCommandKind
    {
        None = 0,
        Select = 1,
        Deselect = 2,
        Confirm = 3
    }

    public sealed class MinionBattleChoiceCommand : IEquatable<MinionBattleChoiceCommand>
    {
        public MinionBattleChoiceCommand(
            MinionBattleChoiceCommandKind kind,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamSide choosingSide,
            MinionId minionId = default)
        {
            if (kind != MinionBattleChoiceCommandKind.Select &&
                kind != MinionBattleChoiceCommandKind.Deselect &&
                kind != MinionBattleChoiceCommandKind.Confirm)
            {
                throw new ArgumentOutOfRangeException(nameof(kind));
            }

            if (!resolutionId.IsDefined || !choiceId.IsDefined)
            {
                throw new ArgumentException("A Minion Battle command requires stable resolution and choice IDs.");
            }

            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(choosingSide));
            }

            if ((kind == MinionBattleChoiceCommandKind.Confirm) == minionId.IsDefined)
            {
                throw new ArgumentException(
                    "Select and deselect require a minion ID; confirm must not carry one.",
                    nameof(minionId));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoosingSide = choosingSide;
            MinionId = minionId;
        }

        public MinionBattleChoiceCommandKind Kind { get; }
        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }
        public TeamSide ChoosingSide { get; }
        public MinionId MinionId { get; }

        public bool Equals(MinionBattleChoiceCommand other) =>
            other != null && Kind == other.Kind && ResolutionId.Equals(other.ResolutionId) &&
            ChoiceId.Equals(other.ChoiceId) && ChoosingSide == other.ChoosingSide &&
            MinionId.Equals(other.MinionId);

        public override bool Equals(object obj) => Equals(obj as MinionBattleChoiceCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ ResolutionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ChoiceId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChoosingSide;
                return (hashCode * 397) ^ MinionId.GetHashCode();
            }
        }
    }

    public readonly struct MinionBattleCandidate : IEquatable<MinionBattleCandidate>
    {
        public MinionBattleCandidate(MinionId minionId, StandardMinionType minionType)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A Minion Battle candidate requires a stable minion ID.", nameof(minionId));
            }

            if (minionType != StandardMinionType.Melee &&
                minionType != StandardMinionType.Ranged &&
                minionType != StandardMinionType.Heavy)
            {
                throw new ArgumentOutOfRangeException(nameof(minionType));
            }

            MinionId = minionId;
            MinionType = minionType;
        }

        public MinionId MinionId { get; }
        public StandardMinionType MinionType { get; }

        public bool Equals(MinionBattleCandidate other) =>
            MinionId.Equals(other.MinionId) && MinionType == other.MinionType;

        public override bool Equals(object obj) =>
            obj is MinionBattleCandidate other && Equals(other);

        public override int GetHashCode() => (MinionId.GetHashCode() * 397) ^ (int)MinionType;
    }

    public sealed class PendingMinionBattleChoice
    {
        public PendingMinionBattleChoice(
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamId losingTeamId,
            TeamSide choosingSide,
            ZoneId battleZoneId,
            int requiredRemovalCount,
            IEnumerable<MinionBattleCandidate> candidates,
            IEnumerable<MinionId> selectedMinionIds)
        {
            if (!resolutionId.IsDefined || !choiceId.IsDefined ||
                !losingTeamId.IsDefined || string.IsNullOrWhiteSpace(battleZoneId.Value))
            {
                throw new ArgumentException(
                    "A pending Minion Battle choice requires stable resolution, choice, team, and Battle-Zone identity.");
            }

            if (choosingSide != TeamSide.Red && choosingSide != TeamSide.Blue)
            {
                throw new ArgumentOutOfRangeException(nameof(choosingSide));
            }

            if (requiredRemovalCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(requiredRemovalCount));
            }

            var candidateArray = (candidates ?? throw new ArgumentNullException(nameof(candidates)))
                .OrderBy(value => value.MinionId.Value, StringComparer.Ordinal)
                .ToArray();
            if (candidateArray.Length <= requiredRemovalCount ||
                candidateArray.Select(value => value.MinionId).Distinct().Count() != candidateArray.Length)
            {
                throw new ArgumentException(
                    "A pending Minion Battle choice requires more unique candidates than removals.",
                    nameof(candidates));
            }

            var selected = (selectedMinionIds ?? throw new ArgumentNullException(nameof(selectedMinionIds)))
                .OrderBy(value => value.Value, StringComparer.Ordinal)
                .ToArray();
            if (selected.Distinct().Count() != selected.Length ||
                selected.Length > requiredRemovalCount ||
                selected.Any(id => candidateArray.All(candidate => !candidate.MinionId.Equals(id))))
            {
                throw new ArgumentException(
                    "Selected Minion Battle identities must be a unique bounded subset of the candidates.",
                    nameof(selectedMinionIds));
            }

            var selectedSet = new HashSet<MinionId>(selected);
            var selectedHeavy = candidateArray.Any(value =>
                selectedSet.Contains(value.MinionId) && value.MinionType == StandardMinionType.Heavy);
            var unselectedNonHeavy = candidateArray.Any(value =>
                !selectedSet.Contains(value.MinionId) && value.MinionType != StandardMinionType.Heavy);
            if (selectedHeavy && unselectedNonHeavy)
            {
                throw new ArgumentException(
                    "Heavy minions may be selected only after every non-Heavy candidate is selected.",
                    nameof(selectedMinionIds));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            LosingTeamId = losingTeamId;
            ChoosingSide = choosingSide;
            BattleZoneId = battleZoneId;
            RequiredRemovalCount = requiredRemovalCount;
            Candidates = Array.AsReadOnly(candidateArray);
            SelectedMinionIds = Array.AsReadOnly(selected);

            var canSelect = selected.Length < requiredRemovalCount;
            var unselected = candidateArray.Where(value => !selectedSet.Contains(value.MinionId)).ToArray();
            var unselectedNonHeavyCandidates = unselected
                .Where(value => value.MinionType != StandardMinionType.Heavy)
                .ToArray();
            LegalSelectMinionIds = Array.AsReadOnly((canSelect
                    ? unselectedNonHeavyCandidates.Length > 0
                        ? unselectedNonHeavyCandidates
                        : unselected
                    : Array.Empty<MinionBattleCandidate>())
                .Select(value => value.MinionId)
                .OrderBy(value => value.Value, StringComparer.Ordinal)
                .ToArray());

            var selectedCandidates = candidateArray.Where(value => selectedSet.Contains(value.MinionId)).ToArray();
            var selectedHeavies = selectedCandidates
                .Where(value => value.MinionType == StandardMinionType.Heavy)
                .ToArray();
            LegalDeselectMinionIds = Array.AsReadOnly((selectedHeavies.Length > 0
                    ? selectedHeavies
                    : selectedCandidates)
                .Select(value => value.MinionId)
                .OrderBy(value => value.Value, StringComparer.Ordinal)
                .ToArray());

            SelectInvocations = Array.AsReadOnly(LegalSelectMinionIds.Select(id =>
                new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Select,
                    ResolutionId,
                    ChoiceId,
                    ChoosingSide,
                    id)).ToArray());
            DeselectInvocations = Array.AsReadOnly(LegalDeselectMinionIds.Select(id =>
                new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Deselect,
                    ResolutionId,
                    ChoiceId,
                    ChoosingSide,
                    id)).ToArray());
            ConfirmInvocation = new MinionBattleChoiceCommand(
                MinionBattleChoiceCommandKind.Confirm,
                ResolutionId,
                ChoiceId,
                ChoosingSide);
        }

        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }
        public TeamId LosingTeamId { get; }
        public TeamSide ChoosingSide { get; }
        public ZoneId BattleZoneId { get; }
        public int RequiredRemovalCount { get; }
        public IReadOnlyList<MinionBattleCandidate> Candidates { get; }
        public IReadOnlyList<MinionId> LegalMinionIds => LegalSelectMinionIds;
        public IReadOnlyList<MinionId> LegalSelectMinionIds { get; }
        public IReadOnlyList<MinionId> LegalDeselectMinionIds { get; }
        public IReadOnlyList<MinionId> SelectedMinionIds { get; }
        public IReadOnlyList<MinionBattleChoiceCommand> SelectInvocations { get; }
        public IReadOnlyList<MinionBattleChoiceCommand> DeselectInvocations { get; }
        public MinionBattleChoiceCommand ConfirmInvocation { get; }
        public bool CanConfirm => SelectedMinionIds.Count == RequiredRemovalCount;

        public PendingMinionBattleChoice WithSelected(IEnumerable<MinionId> selectedMinionIds) =>
            new PendingMinionBattleChoice(
                ResolutionId,
                ChoiceId,
                LosingTeamId,
                ChoosingSide,
                BattleZoneId,
                RequiredRemovalCount,
                Candidates,
                selectedMinionIds);
    }

    public readonly struct LanePushRequestId : IEquatable<LanePushRequestId>
    {
        public LanePushRequestId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Lane-Push request ID must not be blank.", nameof(value));
            }

            Value = value;
        }

        public string Value { get; }
        public bool IsDefined => !string.IsNullOrWhiteSpace(Value);
        public bool Equals(LanePushRequestId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is LanePushRequestId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value ?? string.Empty);
        public override string ToString() => Value ?? string.Empty;
    }

    public sealed class PendingLanePushRequest
    {
        public PendingLanePushRequest(
            LanePushRequestId requestId,
            int roundIndex,
            ZoneId battleZoneId,
            TeamId winningTeamId,
            TeamSide winningSide,
            TeamId losingTeamId,
            TeamSide losingSide,
            EndOfRoundResolutionId causeResolutionId)
        {
            if (!requestId.IsDefined || string.IsNullOrWhiteSpace(battleZoneId.Value) ||
                !winningTeamId.IsDefined || !losingTeamId.IsDefined || !causeResolutionId.IsDefined)
            {
                throw new ArgumentException("An end-of-round Lane-Push request requires stable typed identities.");
            }

            if (roundIndex < 1 || winningTeamId.Equals(losingTeamId) || winningSide == losingSide ||
                (winningSide != TeamSide.Red && winningSide != TeamSide.Blue) ||
                (losingSide != TeamSide.Red && losingSide != TeamSide.Blue))
            {
                throw new ArgumentException("An end-of-round Lane-Push request requires distinct valid teams and round context.");
            }

            RequestId = requestId;
            RoundIndex = roundIndex;
            BattleZoneId = battleZoneId;
            WinningTeamId = winningTeamId;
            WinningSide = winningSide;
            LosingTeamId = losingTeamId;
            LosingSide = losingSide;
            CauseResolutionId = causeResolutionId;
        }

        public LanePushRequestId RequestId { get; }
        public int RoundIndex { get; }
        public ZoneId BattleZoneId { get; }
        public TeamId WinningTeamId { get; }
        public TeamSide WinningSide { get; }
        public TeamId LosingTeamId { get; }
        public TeamSide LosingSide { get; }
        public EndOfRoundResolutionId CauseResolutionId { get; }
        public EndOfRoundStage ConsumptionCheckpoint => EndOfRoundStage.ResolveRequestedLanePush;
    }

    internal sealed class MinionBattlePlan
    {
        public MinionBattlePlan(
            EndOfRoundResolutionId resolutionId,
            ZoneId battleZoneId,
            TeamId winningTeamId,
            TeamSide winningSide,
            TeamId losingTeamId,
            TeamSide losingSide,
            IEnumerable<MinionId> automaticRemovalIds,
            PendingMinionBattleChoice pendingChoice)
        {
            ResolutionId = resolutionId;
            BattleZoneId = battleZoneId;
            WinningTeamId = winningTeamId;
            WinningSide = winningSide;
            LosingTeamId = losingTeamId;
            LosingSide = losingSide;
            AutomaticRemovalIds = Array.AsReadOnly((automaticRemovalIds ??
                throw new ArgumentNullException(nameof(automaticRemovalIds))).ToArray());
            PendingChoice = pendingChoice;
        }

        public EndOfRoundResolutionId ResolutionId { get; }
        public ZoneId BattleZoneId { get; }
        public TeamId WinningTeamId { get; }
        public TeamSide WinningSide { get; }
        public TeamId LosingTeamId { get; }
        public TeamSide LosingSide { get; }
        public IReadOnlyList<MinionId> AutomaticRemovalIds { get; }
        public PendingMinionBattleChoice PendingChoice { get; }
    }

    internal static class MinionBattleRules
    {
        /// <summary>
        /// Unit-type question (6.8g ledger): intrinsic, recorded as open rules question 7 in
        /// RULES_LEDGER.md. p.8 counts minions, has the losing team remove theirs, and respawns
        /// pushed-zone minions; none of those clauses defines removal, respawn, or Life semantics
        /// for a non-minion object counting as a minion, so the battle keeps counting intrinsic
        /// miniatures until the question is settled by source or official ruling.
        /// </summary>
        public static MinionBattlePlan Plan(
            int roundIndex,
            AdjacentMoveAuthority board,
            MatchParticipants participants)
        {
            if (roundIndex < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(roundIndex));
            }

            if (board == null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (participants == null)
            {
                throw new ArgumentNullException(nameof(participants));
            }

            // Milestone 6.4a: one authority-owned membership query, shared with Heavy immunity,
            // Lane Push, setup, and the displaced-minion correction.
            var battleZone = GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                .GetStartingBattleZone(board.MapDefinition);
            var battleMinions = GuardsDigital.Core.Rules.Modifications.BattleZoneQuery
                .FindMinionsInside(board);
            var red = battleMinions.Where(value => value.TeamSide == TeamSide.Red).ToArray();
            var blue = battleMinions.Where(value => value.TeamSide == TeamSide.Blue).ToArray();
            var resolutionId = new EndOfRoundResolutionId($"round-{roundIndex}-minion-battle");
            if (red.Length == blue.Length)
            {
                return new MinionBattlePlan(
                    resolutionId,
                    battleZone.Id,
                    default,
                    TeamSide.None,
                    default,
                    TeamSide.None,
                    Array.Empty<MinionId>(),
                    null);
            }

            var winningSide = red.Length > blue.Length ? TeamSide.Red : TeamSide.Blue;
            var losingSide = winningSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red;
            var winning = participants.Teams.Single(value => value.Side == winningSide);
            var losing = participants.Teams.Single(value => value.Side == losingSide);
            var losingMinions = losingSide == TeamSide.Red ? red : blue;
            var required = Math.Min(Math.Abs(red.Length - blue.Length), losingMinions.Length);
            if (required == 0)
            {
                return new MinionBattlePlan(
                    resolutionId,
                    battleZone.Id,
                    winning.Id,
                    winningSide,
                    losing.Id,
                    losingSide,
                    Array.Empty<MinionId>(),
                    null);
            }

            var nonHeavy = losingMinions
                .Where(value => value.MinionType != StandardMinionType.Heavy)
                .ToArray();
            var heavy = losingMinions
                .Where(value => value.MinionType == StandardMinionType.Heavy)
                .ToArray();
            var candidates = losingMinions.Select(value =>
                new MinionBattleCandidate(value.MinionId, value.MinionType)).ToArray();

            if (required == losingMinions.Length ||
                (required <= nonHeavy.Length && required == nonHeavy.Length))
            {
                return new MinionBattlePlan(
                    resolutionId,
                    battleZone.Id,
                    winning.Id,
                    winningSide,
                    losing.Id,
                    losingSide,
                    (required == losingMinions.Length ? losingMinions : nonHeavy)
                        .Select(value => value.MinionId),
                    null);
            }

            var choice = new PendingMinionBattleChoice(
                resolutionId,
                new EndOfRoundChoiceId($"{resolutionId.Value}-removals"),
                losing.Id,
                losingSide,
                battleZone.Id,
                required,
                candidates,
                Array.Empty<MinionId>());
            return new MinionBattlePlan(
                resolutionId,
                battleZone.Id,
                winning.Id,
                winningSide,
                losing.Id,
                losingSide,
                Array.Empty<MinionId>(),
                choice);
        }
    }
}
