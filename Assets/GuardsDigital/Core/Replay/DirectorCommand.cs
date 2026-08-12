using System;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Replay
{
    public enum DirectorCommandKind
    {
        CommitCard = 0,
        Pass = 1,
        ChooseTiedActor = 2,
        PerformHold = 3,
        StartAction = 4,
        ChooseMovementDestination = 5,
        StopMovement = 6,
        ChooseAttackTarget = 7,
        ChooseDefenseCard = 8,
        DeclineDefense = 9,
        AdvanceEndOfRound = 10,
        PerformFastTravel = 11,
        SelectMinionBattleRemoval = 12,
        DeselectMinionBattleRemoval = 13,
        ConfirmMinionBattleRemovals = 14
    }

    public sealed class DirectorCommand : IEquatable<DirectorCommand>
    {
        private DirectorCommand(
            DirectorCommandKind kind,
            HeroId heroId = default,
            CardInstanceId cardInstanceId = default,
            TeamSide teamSide = default,
            ActionDefinitionId actionDefinitionId = default,
            HexCoordinate coordinate = default,
            ActionResolutionId resolutionId = default,
            ActionResolutionStepId choiceId = default,
            MinionId minionId = default,
            EndOfRoundResolutionId endOfRoundResolutionId = default,
            EndOfRoundChoiceId endOfRoundChoiceId = default)
        {
            Kind = kind;
            HeroId = heroId;
            CardInstanceId = cardInstanceId;
            TeamSide = teamSide;
            ActionDefinitionId = actionDefinitionId;
            Coordinate = coordinate;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            MinionId = minionId;
            EndOfRoundResolutionId = endOfRoundResolutionId;
            EndOfRoundChoiceId = endOfRoundChoiceId;
        }

        public DirectorCommandKind Kind { get; }
        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public TeamSide TeamSide { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public HexCoordinate Coordinate { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public MinionId MinionId { get; }
        public EndOfRoundResolutionId EndOfRoundResolutionId { get; }
        public EndOfRoundChoiceId EndOfRoundChoiceId { get; }

        public static DirectorCommand CommitCard(HeroId heroId, CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.CommitCard, heroId, cardInstanceId);

        public static DirectorCommand Pass(HeroId heroId) =>
            new DirectorCommand(DirectorCommandKind.Pass, heroId);

        public static DirectorCommand ChooseTiedActor(TeamSide side, CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.ChooseTiedActor, cardInstanceId: cardInstanceId, teamSide: side);

        public static DirectorCommand PerformHold(CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.PerformHold, cardInstanceId: cardInstanceId);

        public static DirectorCommand StartAction(ActionDefinitionId actionDefinitionId) =>
            new DirectorCommand(DirectorCommandKind.StartAction, actionDefinitionId: actionDefinitionId);

        public static DirectorCommand PerformFastTravel(
            ActionDefinitionId replacedMovementActionDefinitionId,
            HexCoordinate destination) =>
            new DirectorCommand(
                DirectorCommandKind.PerformFastTravel,
                actionDefinitionId: replacedMovementActionDefinitionId,
                coordinate: destination);

        public static DirectorCommand ChooseMovementDestination(HexCoordinate destination) =>
            new DirectorCommand(DirectorCommandKind.ChooseMovementDestination, coordinate: destination);

        public static DirectorCommand ChooseMovementDestination(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HexCoordinate destination) =>
            new DirectorCommand(
                DirectorCommandKind.ChooseMovementDestination,
                coordinate: destination,
                resolutionId: resolutionId,
                choiceId: choiceId);

        public static DirectorCommand StopMovement() =>
            new DirectorCommand(DirectorCommandKind.StopMovement);

        public static DirectorCommand StopMovement(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId) =>
            new DirectorCommand(
                DirectorCommandKind.StopMovement,
                resolutionId: resolutionId,
                choiceId: choiceId);

        public static DirectorCommand ChooseAttackTarget(HeroId heroId) =>
            new DirectorCommand(DirectorCommandKind.ChooseAttackTarget, heroId);

        public static DirectorCommand ChooseAttackTarget(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId heroId) =>
            new DirectorCommand(
                DirectorCommandKind.ChooseAttackTarget,
                heroId,
                resolutionId: resolutionId,
                choiceId: choiceId);

        public static DirectorCommand ChooseAttackTarget(MinionId minionId) =>
            new DirectorCommand(DirectorCommandKind.ChooseAttackTarget, minionId: minionId);

        public static DirectorCommand ChooseAttackTarget(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            MinionId minionId) =>
            new DirectorCommand(
                DirectorCommandKind.ChooseAttackTarget,
                resolutionId: resolutionId,
                choiceId: choiceId,
                minionId: minionId);

        public static DirectorCommand ChooseDefenseCard(CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.ChooseDefenseCard, cardInstanceId: cardInstanceId);

        public static DirectorCommand ChooseDefenseCard(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            CardInstanceId cardInstanceId) =>
            new DirectorCommand(
                DirectorCommandKind.ChooseDefenseCard,
                cardInstanceId: cardInstanceId,
                resolutionId: resolutionId,
                choiceId: choiceId);

        public static DirectorCommand DeclineDefense() =>
            new DirectorCommand(DirectorCommandKind.DeclineDefense);

        public static DirectorCommand DeclineDefense(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId) =>
            new DirectorCommand(
                DirectorCommandKind.DeclineDefense,
                resolutionId: resolutionId,
                choiceId: choiceId);

        public static DirectorCommand AdvanceEndOfRound() =>
            new DirectorCommand(DirectorCommandKind.AdvanceEndOfRound);

        public static DirectorCommand SelectMinionBattleRemoval(MinionBattleChoiceCommand invocation)
        {
            if (invocation == null || invocation.Kind != MinionBattleChoiceCommandKind.Select)
            {
                throw new ArgumentException("A select command requires the exact projected select invocation.", nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.SelectMinionBattleRemoval,
                teamSide: invocation.ChoosingSide,
                minionId: invocation.MinionId,
                endOfRoundResolutionId: invocation.ResolutionId,
                endOfRoundChoiceId: invocation.ChoiceId);
        }

        public static DirectorCommand DeselectMinionBattleRemoval(MinionBattleChoiceCommand invocation)
        {
            if (invocation == null || invocation.Kind != MinionBattleChoiceCommandKind.Deselect)
            {
                throw new ArgumentException("A deselect command requires the exact projected deselect invocation.", nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.DeselectMinionBattleRemoval,
                teamSide: invocation.ChoosingSide,
                minionId: invocation.MinionId,
                endOfRoundResolutionId: invocation.ResolutionId,
                endOfRoundChoiceId: invocation.ChoiceId);
        }

        public static DirectorCommand ConfirmMinionBattleRemovals(MinionBattleChoiceCommand invocation)
        {
            if (invocation == null || invocation.Kind != MinionBattleChoiceCommandKind.Confirm)
            {
                throw new ArgumentException("A confirm command requires the exact projected confirm invocation.", nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.ConfirmMinionBattleRemovals,
                teamSide: invocation.ChoosingSide,
                endOfRoundResolutionId: invocation.ResolutionId,
                endOfRoundChoiceId: invocation.ChoiceId);
        }

        public bool Equals(DirectorCommand other)
        {
            return other != null && Kind == other.Kind && HeroId.Equals(other.HeroId) &&
                CardInstanceId.Equals(other.CardInstanceId) && TeamSide == other.TeamSide &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) && Coordinate.Equals(other.Coordinate) &&
                ResolutionId.Equals(other.ResolutionId) && ChoiceId.Equals(other.ChoiceId) &&
                MinionId.Equals(other.MinionId) &&
                EndOfRoundResolutionId.Equals(other.EndOfRoundResolutionId) &&
                EndOfRoundChoiceId.Equals(other.EndOfRoundChoiceId);
        }

        public override bool Equals(object obj) => Equals(obj as DirectorCommand);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ HeroId.GetHashCode();
                hash = (hash * 397) ^ CardInstanceId.GetHashCode();
                hash = (hash * 397) ^ (int)TeamSide;
                hash = (hash * 397) ^ ActionDefinitionId.GetHashCode();
                hash = (hash * 397) ^ Coordinate.GetHashCode();
                hash = (hash * 397) ^ ResolutionId.GetHashCode();
                hash = (hash * 397) ^ ChoiceId.GetHashCode();
                hash = (hash * 397) ^ MinionId.GetHashCode();
                hash = (hash * 397) ^ EndOfRoundResolutionId.GetHashCode();
                return (hash * 397) ^ EndOfRoundChoiceId.GetHashCode();
            }
        }

        public override string ToString() => Kind.ToString();
    }
}
