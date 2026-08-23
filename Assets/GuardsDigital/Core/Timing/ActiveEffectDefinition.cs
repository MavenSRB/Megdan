using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
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
        Global = 4,

        /// <summary>
        /// Milestone 8.9. Every unit standing in a declared relation to the source, inside the disk the
        /// definition's <see cref="ActiveEffectDefinition.Radius"/> draws around its controller - Wasp's
        /// Magnetic Dagger, "Enemy units in radius".
        ///
        /// It is the first scope whose membership is decided by the board rather than by the match
        /// roster, and it is therefore the first that changes without the effect changing: p.17 moves
        /// the disk with its hero and every unit inside it walks in and out on its own. Nothing is
        /// stored, so the set is exactly whoever is inside at the moment a rule is asked for, which is
        /// what "in radius" means on a card that does not say "when this resolves".
        ///
        /// It names units rather than heroes because p.19 does: a pinned minion is as much an enemy
        /// unit as a pinned hero, and a scope that expanded to heroes alone would drop half the card.
        /// </summary>
        UnitsInRadius = 5
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
            IEnumerable<RuleModification> modifications = null,
            IEnumerable<ConditionalObstacleOverlay> obstacleOverlays = null,
            DeclaredRadius radius = default,
            SourceTeamRelation affectedUnitRelation = SourceTeamRelation.None,
            EffectDefinitionReference? triggeredEffect = null)
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
            if (triggeredEffect.HasValue && !triggeredEffect.Value.IsDefined)
                throw new ArgumentException("A triggered effect reference must be defined.", nameof(triggeredEffect));
            if (triggeredEffect.HasValue && triggers.Length == 0)
                throw new ArgumentException("A declared triggered effect requires at least one trigger checkpoint.", nameof(triggerCheckpoints));
            if (triggeredEffect.HasValue && triggers.Any(value => value != TimingCheckpoint.ActionEnded))
                throw new ArgumentException(
                    "Milestone 8.12 implements only the Action Ended trigger window required by its synthetic proof.",
                    nameof(triggerCheckpoints));
            var declaredModifications = (modifications ?? Enumerable.Empty<RuleModification>()).ToArray();
            if (declaredModifications.Any(modification => modification == null))
                throw new ArgumentException("Declared modifications must be non-null.", nameof(modifications));
            var declaredOverlays =
                (obstacleOverlays ?? Enumerable.Empty<ConditionalObstacleOverlay>()).ToArray();
            if (declaredOverlays.Any(overlay => overlay == null))
                throw new ArgumentException("Declared obstacle overlays must be non-null.", nameof(obstacleOverlays));

            ValidateDuration(durationKind, creationCheckpoint, activationCheckpoint, expirationCheckpoint, carriesAcrossRoundBoundary);
            ValidateDisk(affectedScope, radius, affectedUnitRelation, declaredOverlays.Length);
            Radius = radius;
            AffectedUnitRelation = affectedUnitRelation;
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
            TriggeredEffect = triggeredEffect;
            Modifications = new ReadOnlyCollection<RuleModification>(declaredModifications);
            ObstacleOverlays = new ReadOnlyCollection<ConditionalObstacleOverlay>(declaredOverlays);
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
        /// Milestone 8.12. The trusted effect definition executed when one of
        /// <see cref="TriggerCheckpoints"/> is visited. The reference, rather than a copied definition,
        /// keeps trigger meaning in the configured content catalog across restore and replay.
        /// </summary>
        public EffectDefinitionReference? TriggeredEffect { get; }

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

        /// <summary>
        /// Milestone 8.7. The conditional obstacle overlays this effect supplies while it is active,
        /// as definition content, beside <see cref="Modifications"/> and for the same reasons.
        ///
        /// They are a separate collection because they are a different kind of rule: a modification
        /// names an object and changes something about it, while an overlay changes what p.18 counts
        /// as an obstacle and names no object at all. An effect that declares neither is a timing
        /// marker; one that declares both is Wasp's Static Barrier's shape without her card's numbers.
        /// </summary>
        public IReadOnlyList<ConditionalObstacleOverlay> ObstacleOverlays { get; }

        /// <summary>
        /// Milestone 8.9. The one disk this effect draws around its controller, or
        /// <see cref="DeclaredRadius.None"/> when it draws none.
        ///
        /// It lives on the definition rather than on either consumer because p.9 gives a card one
        /// Range/Radius icon and card text "refers to this value": an effect whose affected units and
        /// whose obstacle overlay measured different disks would be describing two cards. Both
        /// consumers read it, and it is resolved - items and all - at derivation, never stored.
        /// </summary>
        public DeclaredRadius Radius { get; }

        /// <summary>
        /// Which side of the source the units inside that disk stand on, for
        /// <see cref="ActiveEffectAffectedScopeKind.UnitsInRadius"/>; <c>None</c> for every other
        /// scope, because none of them selects by position at all.
        /// </summary>
        public SourceTeamRelation AffectedUnitRelation { get; }

        /// <summary>
        /// The disk's own consistency, stated once. A scope that selects by position needs a radius and
        /// a side to select by, and a declared overlay needs a radius to draw itself at; a definition
        /// that needs neither may not carry one, because a disk nothing measures is a number a reader
        /// would have to decide the meaning of.
        /// </summary>
        private static void ValidateDisk(
            ActiveEffectAffectedScopeKind affectedScope,
            DeclaredRadius radius,
            SourceTeamRelation affectedUnitRelation,
            int declaredOverlayCount)
        {
            var scopeDrawsADisk = affectedScope == ActiveEffectAffectedScopeKind.UnitsInRadius;
            if (scopeDrawsADisk && affectedUnitRelation == SourceTeamRelation.None)
            {
                throw new ArgumentException(
                    "A radius-scoped Active effect states which side of its source the units it " +
                    "affects stand on; state SourceTeamRelation.AnyTeam when it affects every unit.",
                    nameof(affectedUnitRelation));
            }

            if (!scopeDrawsADisk && affectedUnitRelation != SourceTeamRelation.None)
            {
                throw new ArgumentException(
                    "Only a radius-scoped Active effect selects the units it affects by side.",
                    nameof(affectedUnitRelation));
            }

            if (!Enum.IsDefined(typeof(SourceTeamRelation), affectedUnitRelation))
            {
                throw new ArgumentException(
                    "A radius-scoped Active effect states a defined team relation.",
                    nameof(affectedUnitRelation));
            }

            var needsADisk = scopeDrawsADisk || declaredOverlayCount > 0;
            if (needsADisk && !radius.IsDefined)
            {
                throw new ArgumentException(
                    "An Active effect whose scope or overlay is measured from a disk requires the " +
                    "radius that disk is drawn at.",
                    nameof(radius));
            }

            if (!needsADisk && radius.IsDefined)
            {
                throw new ArgumentException(
                    "An Active effect that measures nothing from a disk declares no radius.",
                    nameof(radius));
            }
        }

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
