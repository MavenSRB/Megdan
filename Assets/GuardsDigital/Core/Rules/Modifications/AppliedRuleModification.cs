using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Which kind of source supplies a modification, and therefore whose lifetime rules it follows.
    ///
    /// The distinction is the rulebook's, not a bookkeeping convenience: an Active effect is cancelled by
    /// the p.17 conditions - it stops applying, its hero is defeated, or its card changes state - while a
    /// token's properties are retained "until that token is removed (this does not include Active
    /// effects)" (p.19). Nothing here re-implements either lifetime; the source object still owns it.
    /// </summary>
    public enum RuleModificationSourceKind
    {
        None = 0,
        ActiveEffect = 1,
        ContinuousSource = 2
    }

    /// <summary>
    /// One <see cref="RuleModification"/> bound to the object it affects and the live source supplying
    /// it. This is the unit of provenance: every query answer can name the exact source that produced it,
    /// which is what lets a value total, a refused target, or an applied permission be explained rather
    /// than merely asserted.
    /// </summary>
    public sealed class AppliedRuleModification
    {
        private AppliedRuleModification(
            RuleModification modification,
            RuleObject affectedObject,
            RuleModificationSourceKind sourceKind,
            string sourceId,
            ActiveEffectId sourceActiveEffectId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            long orderingSequence,
            HeroId boundActorHeroId,
            CardInstanceId sourceCardInstanceId)
        {
            if (!affectedObject.IsDefined)
            {
                throw new ArgumentException(
                    "An applied modification requires the object it affects.", nameof(affectedObject));
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException(
                    "An applied modification requires its source identity.", nameof(sourceId));
            }

            if (!controllerHeroId.IsDefined)
            {
                throw new ArgumentException(
                    "An applied modification requires the hero controlling its source.", nameof(controllerHeroId));
            }

            if (orderingSequence < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(orderingSequence), orderingSequence, "Ordering sequences are non-negative.");
            }

            Modification = modification ?? throw new ArgumentNullException(nameof(modification));
            AffectedObject = affectedObject;
            SourceKind = sourceKind;
            SourceId = sourceId;
            SourceActiveEffectId = sourceActiveEffectId;
            ControllerHeroId = controllerHeroId;
            ControllerTeamSide = controllerTeamSide;
            OrderingSequence = orderingSequence;
            BoundActorHeroId = boundActorHeroId;
            SourceCardInstanceId = sourceCardInstanceId;
        }

        /// <param name="boundActorHeroId">
        /// Milestone 8.20. The hero the supplying occurrence bound, carried through so that a
        /// declaration excepting "this attacker" can be answered where the question is asked. It
        /// travels with the modification rather than being looked up from the effect id, for the reason
        /// every other provenance field here travels with it: the query holds this value and not the
        /// lifecycle, and a second lookup would be a second chance for the two to disagree.
        /// </param>
        public static AppliedRuleModification FromActiveEffect(
            RuleModification modification,
            RuleObject affectedObject,
            ActiveEffectId sourceActiveEffectId,
            HeroId controllerHeroId,
            TeamSide controllerTeamSide,
            long orderingSequence,
            HeroId boundActorHeroId = default,
            CardInstanceId sourceCardInstanceId = default)
        {
            if (!sourceActiveEffectId.IsDefined)
            {
                throw new ArgumentException(
                    "An Active-effect modification requires its instance ID.", nameof(sourceActiveEffectId));
            }

            return new AppliedRuleModification(
                modification,
                affectedObject,
                RuleModificationSourceKind.ActiveEffect,
                sourceActiveEffectId.Value,
                sourceActiveEffectId,
                controllerHeroId,
                controllerTeamSide,
                orderingSequence,
                boundActorHeroId,
                sourceCardInstanceId);
        }

        public static AppliedRuleModification FromContinuousSource(
            RuleModification modification,
            RuleObject affectedObject,
            ContinuousRuleSource source,
            TeamSide controllerTeamSide,
            long orderingSequence)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            return new AppliedRuleModification(
                modification,
                affectedObject,
                RuleModificationSourceKind.ContinuousSource,
                source.SourceId,
                default,
                source.ControllerHeroId,
                controllerTeamSide,
                orderingSequence,
                // A continuous source is p.16's permanent Ultimate ability, which is not created inside
                // anything and therefore points at nobody with "this".
                default,
                source.SourceCardInstanceId);
        }

        public RuleModification Modification { get; }

        public RuleObject AffectedObject { get; }

        public RuleModificationSourceKind SourceKind { get; }

        /// <summary>The source's stable identity, which is also this modification's provenance key.</summary>
        public string SourceId { get; }

        /// <summary>The Active-effect instance, when one supplies this modification.</summary>
        public ActiveEffectId SourceActiveEffectId { get; }

        /// <summary>
        /// Milestone 8.35l. The card whose text this modification came off, where the source knows it.
        ///
        /// <para>It is provenance in the sense this type already exists for - "every query answer can
        /// name the exact source that produced it" - and it is here because a refusal a player is
        /// shown has to name a card rather than an internal identity. "BLOCKED BY WASP - MAGNETIC
        /// DAGGER" is one hero and one card, and both now travel with the modification that did the
        /// blocking rather than being looked up afterwards from an effect id, which is the same
        /// argument every other field here was added under: a second lookup is a second chance for the
        /// two to disagree.</para>
        /// </summary>
        public CardInstanceId SourceCardInstanceId { get; }

        public HeroId ControllerHeroId { get; }

        public TeamSide ControllerTeamSide { get; }

        /// <summary>
        /// Milestone 8.20. The hero the supplying occurrence bound with "this", or the undefined hero
        /// where it bound nobody - which is every modification declared before this milestone, and
        /// every one from a continuous source.
        /// </summary>
        public HeroId BoundActorHeroId { get; }

        /// <summary>
        /// The source's creation order, used as the second ordering key. Rulebook p.17 resolves
        /// simultaneous Active effects "using the same method as when resolving tied initiative", so the
        /// controlling team's revealed Tie-Breaker side orders first and this sequence breaks the rest.
        /// </summary>
        public long OrderingSequence { get; }

        /// <summary>
        /// This modification as a typed value contribution. The contribution kind records which source
        /// family produced it, so an Active-effect bonus and a token-owned one never merge into one
        /// anonymous number.
        /// </summary>
        public ValueContribution ToValueContribution()
        {
            if (Modification.Kind != RuleModificationKind.ValueModifier)
            {
                throw new InvalidOperationException(
                    "Only a value modifier produces a value contribution.");
            }

            return SourceKind == RuleModificationSourceKind.ActiveEffect
                ? new ValueContribution(
                    ValueContributionKind.ActiveEffectModifier,
                    SourceActiveEffectId,
                    Modification.Channel,
                    Modification.Amount)
                : ValueContribution.FromContinuousSource(
                    SourceId,
                    Modification.Channel,
                    Modification.Amount);
        }

        public override string ToString() => $"{Modification} on {AffectedObject} from {SourceId}";
    }
}
