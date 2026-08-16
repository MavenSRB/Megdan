using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

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
            : this(
                kind,
                resolutionId,
                choiceId,
                choosingSide,
                minionId.IsDefined ? RuleObject.ForMinion(minionId) : RuleObject.None)
        {
        }

        public MinionBattleChoiceCommand(
            MinionBattleChoiceCommandKind kind,
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamSide choosingSide,
            RuleObject unit)
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

            if ((kind == MinionBattleChoiceCommandKind.Confirm) == unit.IsDefined)
            {
                throw new ArgumentException(
                    "Select and deselect require an effective-minion unit; confirm must not carry one.",
                    nameof(unit));
            }

            Kind = kind;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            ChoosingSide = choosingSide;
            Unit = unit;
        }

        public MinionBattleChoiceCommandKind Kind { get; }
        public EndOfRoundResolutionId ResolutionId { get; }
        public EndOfRoundChoiceId ChoiceId { get; }
        public TeamSide ChoosingSide { get; }
        public RuleObject Unit { get; }
        public MinionId MinionId => Unit.Kind == RuleObjectKind.Minion ? Unit.MinionId : default;
        public HeroId HeroId => Unit.Kind == RuleObjectKind.Hero ? Unit.HeroId : default;

        public bool Equals(MinionBattleChoiceCommand other) =>
            other != null && Kind == other.Kind && ResolutionId.Equals(other.ResolutionId) &&
            ChoiceId.Equals(other.ChoiceId) && ChoosingSide == other.ChoosingSide &&
            Unit.Equals(other.Unit);

        public override bool Equals(object obj) => Equals(obj as MinionBattleChoiceCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ ResolutionId.GetHashCode();
                hashCode = (hashCode * 397) ^ ChoiceId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ChoosingSide;
                return (hashCode * 397) ^ Unit.GetHashCode();
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

            Unit = RuleObject.ForMinion(minionId);
            MinionType = minionType;
        }

        public MinionBattleCandidate(HeroId heroId)
        {
            if (!heroId.IsDefined)
            {
                throw new ArgumentException(
                    "A Minion Battle candidate requires a stable hero ID.", nameof(heroId));
            }

            Unit = RuleObject.ForHero(heroId);
            MinionType = StandardMinionType.Melee;
        }

        public RuleObject Unit { get; }
        public MinionId MinionId => Unit.Kind == RuleObjectKind.Minion ? Unit.MinionId : default;
        public HeroId HeroId => Unit.Kind == RuleObjectKind.Hero ? Unit.HeroId : default;
        public StandardMinionType MinionType { get; }
        public bool IsHeavy => Unit.Kind == RuleObjectKind.Minion &&
            MinionType == StandardMinionType.Heavy;

        public bool Equals(MinionBattleCandidate other) =>
            Unit.Equals(other.Unit) && MinionType == other.MinionType;

        public override bool Equals(object obj) =>
            obj is MinionBattleCandidate other && Equals(other);

        public override int GetHashCode() => (Unit.GetHashCode() * 397) ^ (int)MinionType;
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
            : this(
                resolutionId,
                choiceId,
                losingTeamId,
                choosingSide,
                battleZoneId,
                requiredRemovalCount,
                candidates,
                (selectedMinionIds ?? throw new ArgumentNullException(nameof(selectedMinionIds)))
                    .Select(RuleObject.ForMinion))
        {
        }

        public PendingMinionBattleChoice(
            EndOfRoundResolutionId resolutionId,
            EndOfRoundChoiceId choiceId,
            TeamId losingTeamId,
            TeamSide choosingSide,
            ZoneId battleZoneId,
            int requiredRemovalCount,
            IEnumerable<MinionBattleCandidate> candidates,
            IEnumerable<RuleObject> selectedUnits)
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
                .OrderBy(value => IntrinsicKindQuery.Of(value.Unit))
                .ThenBy(value => UnitIdentity(value.Unit), StringComparer.Ordinal)
                .ToArray();
            if (candidateArray.Length <= requiredRemovalCount ||
                candidateArray.Select(value => value.Unit).Distinct().Count() != candidateArray.Length)
            {
                throw new ArgumentException(
                    "A pending Minion Battle choice requires more unique candidates than removals.",
                    nameof(candidates));
            }

            var selected = (selectedUnits ?? throw new ArgumentNullException(nameof(selectedUnits)))
                .OrderBy(IntrinsicKindQuery.Of)
                .ThenBy(UnitIdentity, StringComparer.Ordinal)
                .ToArray();
            if (selected.Distinct().Count() != selected.Length ||
                selected.Length > requiredRemovalCount ||
                selected.Any(unit => candidateArray.All(candidate => !candidate.Unit.Equals(unit))))
            {
                throw new ArgumentException(
                    "Selected Minion Battle identities must be a unique bounded subset of the candidates.",
                    nameof(selectedUnits));
            }

            var selectedSet = new HashSet<RuleObject>(selected);
            var selectedHeavy = candidateArray.Any(value =>
                selectedSet.Contains(value.Unit) && value.IsHeavy);
            var unselectedNonHeavy = candidateArray.Any(value =>
                !selectedSet.Contains(value.Unit) && !value.IsHeavy);
            if (selectedHeavy && unselectedNonHeavy)
            {
                throw new ArgumentException(
                    "Heavy minions may be selected only after every non-Heavy candidate is selected.",
                    nameof(selectedUnits));
            }

            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            LosingTeamId = losingTeamId;
            ChoosingSide = choosingSide;
            BattleZoneId = battleZoneId;
            RequiredRemovalCount = requiredRemovalCount;
            Candidates = Array.AsReadOnly(candidateArray);
            SelectedUnits = Array.AsReadOnly(selected);
            SelectedMinionIds = Array.AsReadOnly(selected
                .Where(IntrinsicKindQuery.IsMinionMiniature)
                .Select(unit => unit.MinionId)
                .ToArray());

            var canSelect = selected.Length < requiredRemovalCount;
            var unselected = candidateArray.Where(value => !selectedSet.Contains(value.Unit)).ToArray();
            var unselectedNonHeavyCandidates = unselected
                .Where(value => !value.IsHeavy)
                .ToArray();
            LegalSelectUnits = Array.AsReadOnly((canSelect
                    ? unselectedNonHeavyCandidates.Length > 0
                        ? unselectedNonHeavyCandidates
                        : unselected
                    : Array.Empty<MinionBattleCandidate>())
                .Select(value => value.Unit)
                .OrderBy(IntrinsicKindQuery.Of)
                .ThenBy(UnitIdentity, StringComparer.Ordinal)
                .ToArray());
            LegalSelectMinionIds = Array.AsReadOnly(LegalSelectUnits
                .Where(IntrinsicKindQuery.IsMinionMiniature)
                .Select(unit => unit.MinionId)
                .ToArray());

            var selectedCandidates = candidateArray.Where(value => selectedSet.Contains(value.Unit)).ToArray();
            var selectedHeavies = selectedCandidates
                .Where(value => value.IsHeavy)
                .ToArray();
            LegalDeselectUnits = Array.AsReadOnly((selectedHeavies.Length > 0
                    ? selectedHeavies
                    : selectedCandidates)
                .Select(value => value.Unit)
                .OrderBy(IntrinsicKindQuery.Of)
                .ThenBy(UnitIdentity, StringComparer.Ordinal)
                .ToArray());
            LegalDeselectMinionIds = Array.AsReadOnly(LegalDeselectUnits
                .Where(IntrinsicKindQuery.IsMinionMiniature)
                .Select(unit => unit.MinionId)
                .ToArray());

            SelectInvocations = Array.AsReadOnly(LegalSelectUnits.Select(unit =>
                new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Select,
                    ResolutionId,
                    ChoiceId,
                    ChoosingSide,
                    unit)).ToArray());
            DeselectInvocations = Array.AsReadOnly(LegalDeselectUnits.Select(unit =>
                new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Deselect,
                    ResolutionId,
                    ChoiceId,
                    ChoosingSide,
                    unit)).ToArray());
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
        public IReadOnlyList<RuleObject> LegalSelectUnits { get; }
        public IReadOnlyList<RuleObject> LegalDeselectUnits { get; }
        public IReadOnlyList<RuleObject> SelectedUnits { get; }
        public IReadOnlyList<MinionBattleChoiceCommand> SelectInvocations { get; }
        public IReadOnlyList<MinionBattleChoiceCommand> DeselectInvocations { get; }
        public MinionBattleChoiceCommand ConfirmInvocation { get; }
        public bool CanConfirm => SelectedUnits.Count == RequiredRemovalCount;

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

        public PendingMinionBattleChoice WithSelected(IEnumerable<RuleObject> selectedUnits) =>
            new PendingMinionBattleChoice(
                ResolutionId,
                ChoiceId,
                LosingTeamId,
                ChoosingSide,
                BattleZoneId,
                RequiredRemovalCount,
                Candidates,
                selectedUnits);

        private static string UnitIdentity(RuleObject unit) =>
            IntrinsicKindQuery.Of(unit) == RuleObjectKind.Hero
                ? unit.HeroId.Value
                : unit.MinionId.Value;
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
            IEnumerable<RuleObject> automaticRemovalUnits,
            PendingMinionBattleChoice pendingChoice)
        {
            ResolutionId = resolutionId;
            BattleZoneId = battleZoneId;
            WinningTeamId = winningTeamId;
            WinningSide = winningSide;
            LosingTeamId = losingTeamId;
            LosingSide = losingSide;
            AutomaticRemovalUnits = Array.AsReadOnly((automaticRemovalUnits ??
                throw new ArgumentNullException(nameof(automaticRemovalUnits))).ToArray());
            PendingChoice = pendingChoice;
        }

        public EndOfRoundResolutionId ResolutionId { get; }
        public ZoneId BattleZoneId { get; }
        public TeamId WinningTeamId { get; }
        public TeamSide WinningSide { get; }
        public TeamId LosingTeamId { get; }
        public TeamSide LosingSide { get; }
        public IReadOnlyList<RuleObject> AutomaticRemovalUnits { get; }
        public PendingMinionBattleChoice PendingChoice { get; }
    }

    internal static class MinionBattleRules
    {
        /// <summary>
        /// Counts the effective minion type at the Minion Battle checkpoint. A hero whose current
        /// rules make it a minion is therefore counted and is a non-Heavy removal candidate; removing
        /// it is a Minion Battle removal, not a combat defeat.
        /// </summary>
        public static MinionBattlePlan Plan(
            int roundIndex,
            AdjacentMoveAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications)
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
            var battleMinions = BattleZoneQuery.FindMinionsInside(board)
                .Select(value => new EffectiveBattleMinion(
                    RuleObject.ForMinion(value.MinionId), value.TeamSide,
                    value.MinionType == StandardMinionType.Heavy));
            var battleHeroes = board.Occupancies
                .Select(value => RuleObject.ForHero(value.HeroId))
                .Where(unit => EffectiveUnitKindQuery.Is(unit, EffectiveUnitKind.Minion, modifications))
                .Where(unit =>
                {
                    board.TryGetHeroCoordinate(unit.HeroId, out var coordinate);
                    var space = board.MapDefinition.Spaces.Single(candidate =>
                        candidate.Kind == BoardMapSpaceKind.Playable &&
                        candidate.Coordinate.Equals(coordinate));
                    return BattleZoneQuery.IsInStartingBattleZone(board.MapDefinition, space.Id);
                })
                .Select(unit => new EffectiveBattleMinion(
                    unit, participants.GetTeamForHero(unit.HeroId).Side, false));
            var effective = battleMinions.Concat(battleHeroes).ToArray();
            var red = effective.Where(value => value.TeamSide == TeamSide.Red).ToArray();
            var blue = effective.Where(value => value.TeamSide == TeamSide.Blue).ToArray();
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
                    Array.Empty<RuleObject>(),
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
                    Array.Empty<RuleObject>(),
                    null);
            }

            var nonHeavy = losingMinions
                .Where(value => !value.IsHeavy)
                .ToArray();
            var heavy = losingMinions
                .Where(value => value.IsHeavy)
                .ToArray();
            var candidates = losingMinions.Select(value =>
                IntrinsicKindQuery.Of(value.Unit) == RuleObjectKind.Hero
                ? new MinionBattleCandidate(value.Unit.HeroId)
                : new MinionBattleCandidate(
                    value.Unit.MinionId,
                    value.IsHeavy ? StandardMinionType.Heavy : board.Minions
                        .Single(minion => minion.MinionId.Equals(value.Unit.MinionId)).MinionType))
                .ToArray();

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
                        .Select(value => value.Unit),
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
                Array.Empty<RuleObject>());
            return new MinionBattlePlan(
                resolutionId,
                battleZone.Id,
                winning.Id,
                winningSide,
                losing.Id,
                losingSide,
                Array.Empty<RuleObject>(),
                choice);
        }

        private readonly struct EffectiveBattleMinion
        {
            public EffectiveBattleMinion(RuleObject unit, TeamSide teamSide, bool isHeavy)
            {
                Unit = unit;
                TeamSide = teamSide;
                IsHeavy = isHeavy;
            }

            public RuleObject Unit { get; }
            public TeamSide TeamSide { get; }
            public bool IsHeavy { get; }
        }
    }
}
