using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
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
        ConfirmMinionBattleRemovals = 14,

        /// <summary>
        /// Milestone 6.2a. Every decision inside an effect frame rides this one member, carrying the
        /// projected typed invocation the pending choice built. A new player-decision family is not a
        /// reason to add a command kind; a new authority-level operation is.
        /// </summary>
        AnswerEffectChoice = 15,
        PerformClear = 16
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
            EndOfRoundChoiceId endOfRoundChoiceId = default,
            EffectChoiceCommand effectChoice = null,
            IEnumerable<TokenInstanceId> tokenInstanceIds = null)
        {
            var materializedTokenIds = tokenInstanceIds?.ToArray() ?? Array.Empty<TokenInstanceId>();
            if (materializedTokenIds.Any(id => !id.IsDefined) ||
                materializedTokenIds.Distinct().Count() != materializedTokenIds.Length)
            {
                throw new ArgumentException(
                    "Director command token IDs must be defined and unique.", nameof(tokenInstanceIds));
            }

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
            EffectChoice = effectChoice;
            TokenInstanceIds = Array.AsReadOnly(materializedTokenIds
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
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
        public IReadOnlyList<TokenInstanceId> TokenInstanceIds { get; }

        /// <summary>The projected typed invocation an effect choice is answered with.</summary>
        public EffectChoiceCommand EffectChoice { get; }

        /// <summary>
        /// Answers a pending effect choice with an invocation the pending choice itself projected.
        /// Composing one by hand is refused here rather than at the authority, the same guard the
        /// Minion-Battle factories apply.
        /// </summary>
        public static DirectorCommand AnswerEffectChoice(EffectChoiceCommand invocation)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            if (invocation.Kind == EffectChoiceCommandKind.None)
            {
                throw new ArgumentException(
                    "An effect choice invocation requires a kind.", nameof(invocation));
            }

            return new DirectorCommand(DirectorCommandKind.AnswerEffectChoice, effectChoice: invocation);
        }

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

        public static DirectorCommand PerformClear(
            ActionDefinitionId replacedAttackActionDefinitionId,
            IEnumerable<TokenInstanceId> selectedTokenIds)
        {
            if (!replacedAttackActionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "Clear requires the replaced Attack action ID.",
                    nameof(replacedAttackActionDefinitionId));
            }

            return new DirectorCommand(
                DirectorCommandKind.PerformClear,
                actionDefinitionId: replacedAttackActionDefinitionId,
                tokenInstanceIds: selectedTokenIds ?? throw new ArgumentNullException(nameof(selectedTokenIds)));
        }

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
                EndOfRoundChoiceId.Equals(other.EndOfRoundChoiceId) &&
                Equals(EffectChoice, other.EffectChoice) &&
                TokenInstanceIds.SequenceEqual(other.TokenInstanceIds);
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
                hash = (hash * 397) ^ EndOfRoundChoiceId.GetHashCode();
                hash = (hash * 397) ^ (EffectChoice?.GetHashCode() ?? 0);
                foreach (var tokenId in TokenInstanceIds)
                    hash = (hash * 397) ^ tokenId.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => Kind.ToString();
    }
}
