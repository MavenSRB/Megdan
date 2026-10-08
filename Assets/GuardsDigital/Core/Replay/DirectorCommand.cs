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
        PerformClear = 16,

        /// <summary>
        /// Milestone 6.4a. The after-action displaced-minion correction is a new authority-level
        /// operation - its own pending family with its own resume path, exactly the Minion-Battle
        /// precedent - so its two decisions get command kinds rather than riding another family's.
        /// Each is one-shot: selecting is the advance, so there is no deselect or confirm.
        /// </summary>
        ChooseCorrectionSubject = 17,
        ChooseCorrectionDestination = 18,
        RespawnHero = 19,
        ResolveCardWithoutRespawn = 20,

        /// <summary>
        /// Milestone 6.10a. A multi-step system transition is a new authority-level operation with its
        /// own pending family and its own resume path, which is the Minion-Battle and correction
        /// precedent for giving one a command kind rather than riding another family's.
        ///
        /// Beginning one is a command because the contract's first consumer is a bounded synthetic
        /// transition, which has no rules of its own to be begun by, and because a transition begun
        /// outside the recorded command stream would simply not exist in a replayed or Undone match -
        /// which would make the acceptance check about replay pass vacuously. Milestone 6.11's
        /// Lane-Push respawn and milestone 7.5's progression begin theirs from their own rules.
        /// </summary>
        BeginSystemTransition = 21,
        AnswerSystemTransitionStep = 22,

        /// <summary>
        /// Milestone 8.19c1. A Defeat standing at its removal stage is a new authority-level operation
        /// with its own pending family and its own resume path, which is the Minion-Battle, correction
        /// and system-transition precedent for giving one a command kind rather than riding another
        /// family's. It is not an effect choice: no effect frame is paused, the answer names a source
        /// and the card that pays for it, and what it resumes is rulebook p.13's removal stage.
        /// </summary>
        AnswerRemovalPrevention = 23,

        /// <summary>
        /// Milestone 8.35g. Releasing a finished turn is a new authority-level operation with its own
        /// pending family and its own resume path, which is the Minion-Battle, correction,
        /// system-transition and removal-prevention precedent for giving one a command kind rather
        /// than riding another family's.
        /// </summary>
        /// <remarks>
        /// It has to be a recorded command rather than a host policy for the reason
        /// <see cref="BeginSystemTransition"/> gives: a release taken outside the recorded stream
        /// would simply not exist in a replayed or Undone match, and the turn would then advance by
        /// itself during replay - which is exactly the auto-advance this milestone removed.
        /// </remarks>
        ReleaseTurn = 24,

        /// <summary>
        /// Milestone 9.21b. "Which of you acts" (RZ-Q4): a hero standing on several spaces names the
        /// body that performs the action his card is about to choose. A new authority-level operation
        /// with its own pending family and its own resume path - the respawn precedent, which is the
        /// other question p.7 asks as a card resolves - so it gets a command kind rather than riding
        /// another family's.
        /// </summary>
        ChooseActingBody = 25,
        CommitTwoCards = 26,
        RetrieveOneAfterReveal = 27,
        RetractCommitment = 28
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
            RemovalPreventionCommand removalPrevention = null,
            IEnumerable<TokenInstanceId> tokenInstanceIds = null,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId systemTransitionDefinitionId = default,
            GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand systemTransitionChoice = default,
            int heroBody = 0,
            CardInstanceId secondCardInstanceId = default)
        {
            if (heroBody < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heroBody), heroBody, "A hero body ordinal is 1 or more, or 0 for none named.");
            }

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
            SecondCardInstanceId = secondCardInstanceId;
            TeamSide = teamSide;
            ActionDefinitionId = actionDefinitionId;
            Coordinate = coordinate;
            ResolutionId = resolutionId;
            ChoiceId = choiceId;
            MinionId = minionId;
            EndOfRoundResolutionId = endOfRoundResolutionId;
            EndOfRoundChoiceId = endOfRoundChoiceId;
            EffectChoice = effectChoice;
            RemovalPrevention = removalPrevention;
            SystemTransitionDefinitionId = systemTransitionDefinitionId;
            SystemTransitionChoice = systemTransitionChoice;
            HeroBody = heroBody;
            TokenInstanceIds = Array.AsReadOnly(materializedTokenIds
                .OrderBy(id => id.Value, StringComparer.Ordinal)
                .ToArray());
        }

        public DirectorCommandKind Kind { get; }
        public HeroId HeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public CardInstanceId SecondCardInstanceId { get; }
        public TeamSide TeamSide { get; }
        public ActionDefinitionId ActionDefinitionId { get; }
        public HexCoordinate Coordinate { get; }
        public ActionResolutionId ResolutionId { get; }
        public ActionResolutionStepId ChoiceId { get; }
        public MinionId MinionId { get; }

        /// <summary>
        /// Milestone 9.21a (RZ-Q3). Which body of <see cref="HeroId"/> an Attack-target answer names, or
        /// 0 when it names the hero alone - which is every answer about a hero only one of whose bodies
        /// is a legal target, and so every answer about every hero the build deals today. Milestone
        /// 9.21b: the body a <see cref="DirectorCommandKind.ChooseActingBody"/> answer picks (RZ-Q4).
        /// </summary>
        public int HeroBody { get; }
        public EndOfRoundResolutionId EndOfRoundResolutionId { get; }
        public EndOfRoundChoiceId EndOfRoundChoiceId { get; }
        public IReadOnlyList<TokenInstanceId> TokenInstanceIds { get; }

        /// <summary>The projected typed invocation an effect choice is answered with.</summary>
        public EffectChoiceCommand EffectChoice { get; }

        /// <summary>
        /// Milestone 8.19c1: the typed answer a standing removal-prevention decision is closed with -
        /// the source whose price is paid and the card that pays it, or a decline.
        /// </summary>
        public RemovalPreventionCommand RemovalPrevention { get; }

        /// <summary>Milestone 6.10a: which declared transition to begin.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId SystemTransitionDefinitionId { get; }

        /// <summary>Milestone 6.10a: the projected invocation a paused transition step is answered with.</summary>
        public GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand SystemTransitionChoice { get; }

        public static DirectorCommand BeginSystemTransition(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionDefinitionId definitionId)
        {
            if (!definitionId.IsDefined)
            {
                throw new ArgumentException(
                    "Beginning a transition requires a defined definition ID.", nameof(definitionId));
            }

            return new DirectorCommand(
                DirectorCommandKind.BeginSystemTransition,
                systemTransitionDefinitionId: definitionId);
        }

        /// <summary>
        /// Answers a paused transition step with an invocation the continuation itself projected.
        /// Composing one by hand is refused here rather than at the authority, the same guard the
        /// Minion-Battle and effect-choice factories apply.
        /// </summary>
        public static DirectorCommand AnswerSystemTransitionStep(
            GuardsDigital.Core.Rules.Transitions.SystemTransitionChoiceCommand invocation)
        {
            if (!invocation.IsDefined)
            {
                throw new ArgumentException(
                    "Answering a transition step requires the exact projected invocation.", nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.AnswerSystemTransitionStep,
                teamSide: invocation.ChoosingSide,
                systemTransitionChoice: invocation);
        }

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

        /// <summary>
        /// Milestone 8.19c1. Closes the standing removal-prevention decision, paying one source's
        /// price or declining. The invocation is refused here rather than at the authority for the
        /// reason every other typed invocation is: a command with no decision named could only ever be
        /// rejected further in, with less to say about why.
        /// </summary>
        public static DirectorCommand AnswerRemovalPrevention(RemovalPreventionCommand invocation)
        {
            if (invocation == null)
            {
                throw new ArgumentNullException(nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.AnswerRemovalPrevention, removalPrevention: invocation);
        }

        public static DirectorCommand CommitCard(HeroId heroId, CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.CommitCard, heroId, cardInstanceId);

        public static DirectorCommand CommitTwoCards(
            HeroId heroId, CardInstanceId firstCardId, CardInstanceId secondCardId)
        {
            // The hand may be clicked in either order; one unordered pair is one authorized offer.
            if (StringComparer.Ordinal.Compare(firstCardId.Value, secondCardId.Value) > 0)
            {
                var swap = firstCardId;
                firstCardId = secondCardId;
                secondCardId = swap;
            }

            return new DirectorCommand(DirectorCommandKind.CommitTwoCards, heroId, firstCardId,
                secondCardInstanceId: secondCardId);
        }

        public static DirectorCommand RetrieveOneAfterReveal(HeroId heroId, CardInstanceId cardId) =>
            new DirectorCommand(DirectorCommandKind.RetrieveOneAfterReveal, heroId, cardId);

        public static DirectorCommand RetractCommitment(HeroId heroId) =>
            new DirectorCommand(DirectorCommandKind.RetractCommitment, heroId);

        public static DirectorCommand Pass(HeroId heroId) =>
            new DirectorCommand(DirectorCommandKind.Pass, heroId);

        public static DirectorCommand ChooseTiedActor(TeamSide side, CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.ChooseTiedActor, cardInstanceId: cardInstanceId, teamSide: side);

        public static DirectorCommand PerformHold(CardInstanceId cardInstanceId) =>
            new DirectorCommand(DirectorCommandKind.PerformHold, cardInstanceId: cardInstanceId);

        public static DirectorCommand StartAction(ActionDefinitionId actionDefinitionId) =>
            new DirectorCommand(DirectorCommandKind.StartAction, actionDefinitionId: actionDefinitionId);

        /// <summary>
        /// Milestone 8.29f. The same start, naming the card the offer was drawn on.
        ///
        /// <para>An action definition id alone is not an identity: rulebook p.14 prints the same
        /// <c>secondary-movement</c> on every card in the game, so a start command holding one binds to
        /// whoever Initiative has selected when it arrives rather than to the card a player pressed. A
        /// start submitted after its own action finished - the offer captured before an Attack, sent
        /// after it - was therefore accepted as the *next* hero's Movement. This is the identity guard
        /// every answer command in this session already carries, on the one command that had none.</para>
        /// </summary>
        public static DirectorCommand StartAction(
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId) =>
            new DirectorCommand(
                DirectorCommandKind.StartAction,
                actionDefinitionId: actionDefinitionId,
                cardInstanceId: sourceCardInstanceId);

        public static DirectorCommand RespawnHero(HeroRespawnCommand invocation)
        {
            if (invocation == null || invocation.Kind != HeroRespawnCommandKind.Respawn)
            {
                throw new ArgumentException(
                    "A hero respawn command requires the exact projected destination invocation.",
                    nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.RespawnHero,
                heroId: invocation.HeroId,
                cardInstanceId: invocation.CardInstanceId,
                coordinate: invocation.Destination);
        }

        /// <summary>Milestone 9.21b. The exact projected answer to "which of you acts" (RZ-Q4).</summary>
        public static DirectorCommand ChooseActingBody(ActingBodyCommand invocation)
        {
            if (invocation == null)
            {
                throw new ArgumentException(
                    "An acting-body answer requires the exact projected invocation.", nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.ChooseActingBody,
                heroId: invocation.HeroId,
                cardInstanceId: invocation.CardInstanceId,
                heroBody: invocation.Body);
        }

        public static DirectorCommand ResolveCardWithoutRespawn(HeroRespawnCommand invocation)
        {
            if (invocation == null || invocation.Kind != HeroRespawnCommandKind.ResolveWithoutAction)
            {
                throw new ArgumentException(
                    "Resolving without respawn requires the exact projected decline invocation.",
                    nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.ResolveCardWithoutRespawn,
                heroId: invocation.HeroId,
                cardInstanceId: invocation.CardInstanceId);
        }

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

        /// <summary>
        /// Milestone 9.21a. An Attack-target answer naming one body of a hero - required when several
        /// of that hero's bodies are legal targets of the same choice.
        /// </summary>
        public static DirectorCommand ChooseAttackTarget(
            ActionResolutionId resolutionId,
            ActionResolutionStepId choiceId,
            HeroId heroId,
            int heroBody)
        {
            if (heroBody < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(heroBody), heroBody, "A named hero body ordinal starts at 1.");
            }

            return new DirectorCommand(
                DirectorCommandKind.ChooseAttackTarget,
                heroId,
                resolutionId: resolutionId,
                choiceId: choiceId,
                heroBody: heroBody);
        }

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

        /// <summary>
        /// Milestone 8.35g. Hands a finished turn on to the next actor. It carries no payload: the
        /// only turn it can release is the one standing in the release pause, and the authority
        /// knows which that is.
        /// </summary>
        public static DirectorCommand ReleaseTurn() =>
            new DirectorCommand(DirectorCommandKind.ReleaseTurn);

        public static DirectorCommand ChooseCorrectionSubject(
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand invocation)
        {
            if (invocation == null ||
                invocation.Kind != GuardsDigital.Core.Rules.Displacement
                    .DisplacedMinionCorrectionCommandKind.SelectSubject)
            {
                throw new ArgumentException(
                    "A correction subject command requires the exact projected subject invocation.",
                    nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.ChooseCorrectionSubject,
                teamSide: invocation.ChoosingSide,
                minionId: invocation.MinionId,
                resolutionId: invocation.ResolutionId,
                choiceId: invocation.ChoiceId);
        }

        public static DirectorCommand ChooseCorrectionDestination(
            GuardsDigital.Core.Rules.Displacement.DisplacedMinionCorrectionCommand invocation)
        {
            if (invocation == null ||
                invocation.Kind != GuardsDigital.Core.Rules.Displacement
                    .DisplacedMinionCorrectionCommandKind.SelectDestination)
            {
                throw new ArgumentException(
                    "A correction destination command requires the exact projected destination invocation.",
                    nameof(invocation));
            }

            return new DirectorCommand(
                DirectorCommandKind.ChooseCorrectionDestination,
                teamSide: invocation.ChoosingSide,
                minionId: invocation.MinionId,
                coordinate: invocation.Destination,
                resolutionId: invocation.ResolutionId,
                choiceId: invocation.ChoiceId);
        }

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
                heroId: invocation.HeroId,
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
                heroId: invocation.HeroId,
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
                CardInstanceId.Equals(other.CardInstanceId) &&
                SecondCardInstanceId.Equals(other.SecondCardInstanceId) && TeamSide == other.TeamSide &&
                ActionDefinitionId.Equals(other.ActionDefinitionId) && Coordinate.Equals(other.Coordinate) &&
                ResolutionId.Equals(other.ResolutionId) && ChoiceId.Equals(other.ChoiceId) &&
                MinionId.Equals(other.MinionId) && HeroBody == other.HeroBody &&
                EndOfRoundResolutionId.Equals(other.EndOfRoundResolutionId) &&
                EndOfRoundChoiceId.Equals(other.EndOfRoundChoiceId) &&
                Equals(EffectChoice, other.EffectChoice) &&
                Equals(RemovalPrevention, other.RemovalPrevention) &&
                SystemTransitionDefinitionId.Equals(other.SystemTransitionDefinitionId) &&
                SystemTransitionChoice.Equals(other.SystemTransitionChoice) &&
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
                hash = (hash * 397) ^ SecondCardInstanceId.GetHashCode();
                hash = (hash * 397) ^ (int)TeamSide;
                hash = (hash * 397) ^ ActionDefinitionId.GetHashCode();
                hash = (hash * 397) ^ Coordinate.GetHashCode();
                hash = (hash * 397) ^ ResolutionId.GetHashCode();
                hash = (hash * 397) ^ ChoiceId.GetHashCode();
                hash = (hash * 397) ^ MinionId.GetHashCode();
                hash = (hash * 397) ^ HeroBody;
                hash = (hash * 397) ^ EndOfRoundResolutionId.GetHashCode();
                hash = (hash * 397) ^ EndOfRoundChoiceId.GetHashCode();
                hash = (hash * 397) ^ (EffectChoice?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ (RemovalPrevention?.GetHashCode() ?? 0);
                hash = (hash * 397) ^ SystemTransitionDefinitionId.GetHashCode();
                hash = (hash * 397) ^ SystemTransitionChoice.GetHashCode();
                foreach (var tokenId in TokenInstanceIds)
                    hash = (hash * 397) ^ tokenId.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => Kind.ToString();
    }
}
