using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Timing
{
    public enum ActiveEffectDurationKind
    {
        None = 0,
        ThisTurn = 1,
        ThisRound = 2,
        NextTurn = 3,
        NextTurnAfterPlayingCards = 4
    }

    public enum ActiveEffectAffectedScopeKind
    {
        None = 0,
        ControllerHero = 1,
        Hero = 2,
        ControllerTeam = 3,
        Global = 4
    }

    public enum ActiveEffectCancellationCondition
    {
        None = 0,
        SourceCardStateChanged = 1,
        /// <summary>
        /// p.17: an Active effect on your card ends when "you are defeated" - the hero whose action
        /// created it, which p.13 states from the other side as "Defeated hero's actions with Active
        /// effects are canceled". Named `AffectedHeroDefeated` until milestone 7.4.
        /// </summary>
        ControllerHeroDefeated = 2,
        CardsRetrieved = 3,
        Explicit = 4
    }

    public sealed class ActiveEffectDefinition
    {
        public ActiveEffectDefinition(
            string definitionId,
            ActiveEffectAffectedScopeKind affectedScope,
            ActiveEffectDurationKind durationKind,
            TimingCheckpoint creationCheckpoint,
            TimingCheckpoint activationCheckpoint,
            TimingCheckpoint expirationCheckpoint,
            IEnumerable<ActiveEffectCancellationCondition> cancellationConditions,
            bool carriesAcrossRoundBoundary,
            string effectKind,
            string uniquenessKey,
            IEnumerable<TimingCheckpoint> triggerCheckpoints,
            IEnumerable<RuleModification> modifications = null)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("An Active-effect definition requires a stable ID.", nameof(definitionId));
            if (affectedScope == ActiveEffectAffectedScopeKind.None)
                throw new ArgumentException("An Active-effect definition requires an affected scope.", nameof(affectedScope));
            if (durationKind == ActiveEffectDurationKind.None)
                throw new ArgumentException("An Active-effect definition requires a duration kind.", nameof(durationKind));
            if (creationCheckpoint == TimingCheckpoint.None || activationCheckpoint == TimingCheckpoint.None ||
                expirationCheckpoint == TimingCheckpoint.None)
                throw new ArgumentException("Creation, activation, and expiration checkpoints must all be explicit.");
            if (string.IsNullOrWhiteSpace(effectKind))
                throw new ArgumentException("An Active-effect definition requires a typed effect kind.", nameof(effectKind));
            if (string.IsNullOrWhiteSpace(uniquenessKey))
                throw new ArgumentException("An Active-effect definition requires an explicit uniqueness key.", nameof(uniquenessKey));

            var conditions = (cancellationConditions ?? throw new ArgumentNullException(nameof(cancellationConditions)))
                .ToArray();
            if (conditions.Length == 0 || conditions.Contains(ActiveEffectCancellationCondition.None) ||
                conditions.Distinct().Count() != conditions.Length)
                throw new ArgumentException("Active-effect cancellation conditions must be nonempty, defined, and unique.", nameof(cancellationConditions));
            var triggers = (triggerCheckpoints ?? throw new ArgumentNullException(nameof(triggerCheckpoints))).ToArray();
            if (triggers.Contains(TimingCheckpoint.None) || triggers.Distinct().Count() != triggers.Length)
                throw new ArgumentException("Active-effect trigger checkpoints must be defined and unique.", nameof(triggerCheckpoints));
            var declaredModifications = (modifications ?? Enumerable.Empty<RuleModification>()).ToArray();
            if (declaredModifications.Any(modification => modification == null))
                throw new ArgumentException("Declared modifications must be non-null.", nameof(modifications));

            ValidateDuration(durationKind, creationCheckpoint, activationCheckpoint, expirationCheckpoint, carriesAcrossRoundBoundary);
            DefinitionId = definitionId;
            AffectedScope = affectedScope;
            DurationKind = durationKind;
            CreationCheckpoint = creationCheckpoint;
            ActivationCheckpoint = activationCheckpoint;
            ExpirationCheckpoint = expirationCheckpoint;
            CancellationConditions = new ReadOnlyCollection<ActiveEffectCancellationCondition>(conditions);
            CarriesAcrossRoundBoundary = carriesAcrossRoundBoundary;
            EffectKind = effectKind;
            UniquenessKey = uniquenessKey;
            TriggerCheckpoints = new ReadOnlyCollection<TimingCheckpoint>(triggers);
            Modifications = new ReadOnlyCollection<RuleModification>(declaredModifications);
        }

        public string DefinitionId { get; }
        public ActiveEffectAffectedScopeKind AffectedScope { get; }
        public ActiveEffectDurationKind DurationKind { get; }
        public TimingCheckpoint CreationCheckpoint { get; }
        public TimingCheckpoint ActivationCheckpoint { get; }
        public TimingCheckpoint ExpirationCheckpoint { get; }
        public IReadOnlyList<ActiveEffectCancellationCondition> CancellationConditions { get; }
        public bool CarriesAcrossRoundBoundary { get; }
        public string EffectKind { get; }
        public string UniquenessKey { get; }
        public IReadOnlyList<TimingCheckpoint> TriggerCheckpoints { get; }

        /// <summary>
        /// The typed rules this effect supplies while it is active, as definition content.
        ///
        /// Milestone 6.8 put them here rather than on the instance for the reason milestone 6.2a moved
        /// the rest of the semantics here: a snapshot names the definition and the trusted catalog
        /// supplies its meaning, so a restored instance produces exactly these modifications again and no
        /// payload can restate what a saved effect does. An effect that declares none is a timing marker,
        /// not a modifier.
        /// </summary>
        public IReadOnlyList<RuleModification> Modifications { get; }

        private static void ValidateDuration(
            ActiveEffectDurationKind duration,
            TimingCheckpoint creation,
            TimingCheckpoint activation,
            TimingCheckpoint expiration,
            bool carriesAcrossRound)
        {
            if (duration == ActiveEffectDurationKind.NextTurn ||
                duration == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
            {
                if (creation == TimingCheckpoint.TurnStarted || creation == TimingCheckpoint.CardsPlayedAndRevealed ||
                    creation == TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded || creation == TimingCheckpoint.TurnEnded ||
                    creation == TimingCheckpoint.RoundEnd || creation == TimingCheckpoint.CardsRetrieved)
                    throw new ArgumentException("A Next Turn effect must be created during an action/effect window before its future activation anchor.");
            }

            if (duration == ActiveEffectDurationKind.ThisTurn)
            {
                if (activation != TimingCheckpoint.ActionStarted && activation != TimingCheckpoint.BeforeAttack &&
                    activation != TimingCheckpoint.DefenseResponse &&
                    activation != TimingCheckpoint.AttackResultComparison &&
                    activation != TimingCheckpoint.AfterAttack && activation != TimingCheckpoint.ActionEnded &&
                    activation != TimingCheckpoint.TurnStarted)
                    throw new ArgumentException("A This Turn effect must activate within the current turn.");
                if (expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded || carriesAcrossRound)
                    throw new ArgumentException("A This Turn effect expires at the turn-only checkpoint and never carries across rounds.");
            }
            else if (duration == ActiveEffectDurationKind.ThisRound)
            {
                if (expiration != TimingCheckpoint.RoundEnd || carriesAcrossRound)
                    throw new ArgumentException("A This Round effect expires at Round End and never carries across rounds.");
            }
            else if (duration == ActiveEffectDurationKind.NextTurn)
            {
                if (activation != TimingCheckpoint.TurnStarted || expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded)
                    throw new ArgumentException("A Next Turn effect activates at Turn Started and expires at that turn's cancellation checkpoint.");
            }
            else if (duration == ActiveEffectDurationKind.NextTurnAfterPlayingCards)
            {
                if (activation != TimingCheckpoint.CardsPlayedAndRevealed ||
                    expiration != TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded)
                    throw new ArgumentException("Next Turn after Playing Cards has distinct reveal activation and turn expiry checkpoints.");
            }
        }
    }
}
