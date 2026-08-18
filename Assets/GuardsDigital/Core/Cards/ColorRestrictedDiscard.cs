using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Cards
{
    /// <summary>Why a colour-restricted discard refused, or <see cref="None"/> when it did not.</summary>
    public enum ColorRestrictedDiscardRejection
    {
        None = 0,

        /// <summary>The criteria selected no card, so there is nothing to offer and nothing to discard.</summary>
        NoCandidate = 1,

        /// <summary>The answer named a card this criteria does not currently select.</summary>
        ChoiceNotOffered = 2,

        /// <summary>The named card is not in the lifecycle shape a discard from hand starts in.</summary>
        CardNotDiscardable = 3
    }

    public sealed class ColorRestrictedDiscardResult
    {
        private ColorRestrictedDiscardResult(
            bool accepted,
            ColorRestrictedDiscardRejection rejection,
            CardInstanceId discardedCardId)
        {
            Accepted = accepted;
            Rejection = rejection;
            DiscardedCardId = discardedCardId;
        }

        public bool Accepted { get; }
        public ColorRestrictedDiscardRejection Rejection { get; }
        public CardInstanceId DiscardedCardId { get; }

        public static ColorRestrictedDiscardResult Accept(CardInstanceId discardedCardId) =>
            new ColorRestrictedDiscardResult(
                true, ColorRestrictedDiscardRejection.None, discardedCardId);

        public static ColorRestrictedDiscardResult Reject(ColorRestrictedDiscardRejection rejection)
        {
            if (rejection == ColorRestrictedDiscardRejection.None)
            {
                throw new ArgumentException(
                    "A rejected discard requires a rejection reason.", nameof(rejection));
            }

            return new ColorRestrictedDiscardResult(false, rejection, default);
        }
    }

    /// <summary>
    /// The worked example the card-system's state-capability contract describes, built out of the three
    /// pieces it names: derive the legal candidates in the source-allowed state, record the correct
    /// chooser and pending choice, then invoke <c>DiscardCard</c> on the accepted candidate.
    ///
    /// It exists as one type because the acceptance check is about how the three compose, and a
    /// composition that only ever existed inside a test would prove nothing about the seam a verified
    /// card will use. Everything it does is delegation: <see cref="CardCharacteristicQuery"/> owns the
    /// candidates, <see cref="EffectChoiceRequest"/> is the effect frame's own typed pending-choice
    /// shape rather than a stand-in for one, and <see cref="CardLifecycleAuthority.TryDiscardFromHand"/>
    /// owns the mutation.
    ///
    /// The division of labour is the point, and it runs one way. This type decides colour eligibility,
    /// ownership and chooser policy; the mutation decides none of them and is handed the card that was
    /// chosen. <see cref="Resolve"/> re-derives the candidate set before committing - an answer is
    /// checked against the state that exists when it arrives, not against the state that existed when
    /// the choice opened, which is the same rule every target selection in this build follows.
    ///
    /// What it is not: it is not a card, an action, or a capability. No configured card prints a
    /// colour-restricted discard, milestone 7.1's non-goals exclude inventing one, and no verified card
    /// content exists to bind it to yet. Reaching it from an effect definition additionally needs the
    /// effect frame to carry card instances, which today it does not: a Skill's resolution receives
    /// definitions alone once it has started, so a paused choice could not re-derive card candidates
    /// when its answer arrived. Threading that through the resolver, its repeat and feasibility paths,
    /// and snapshot restore is the work the first verified colour effect owes, and doing it here would
    /// have changed the effect-resolution seam in the same milestone that moves the snapshot schema.
    /// </summary>
    public static class ColorRestrictedDiscard
    {
        /// <summary>
        /// The pending choice this discard owes, or <c>null</c> when the criteria selects nothing.
        ///
        /// One selection, mandatory, in the query's deterministic order. A single candidate still opens
        /// the choice rather than being taken automatically, which is this codebase's uniform
        /// precedent for a chooser-owned selection: the chooser owns the decision even when it has one
        /// option, and a rule that silently discarded their last legal card would be deciding for them.
        /// </summary>
        public static EffectChoiceRequest Offer(
            CardCharacteristicCriteria criteria,
            EffectChooserRole chooser,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (chooser == EffectChooserRole.None)
            {
                throw new ArgumentException("A discard choice requires a chooser.", nameof(chooser));
            }

            var candidates = Candidates(criteria, definitions, instances);
            if (candidates.Count == 0)
            {
                return null;
            }

            return new EffectChoiceRequest(
                chooser,
                candidates.Select(EffectValue.Card).ToArray(),
                1,
                1);
        }

        /// <summary>
        /// Commits the accepted answer: check it is still one of the cards this criteria selects, then
        /// discard exactly that card.
        ///
        /// The two refusals are kept apart on purpose. <see cref="ColorRestrictedDiscardRejection.ChoiceNotOffered"/>
        /// means the answer named a card the criteria does not select - a stale answer, a card that has
        /// moved, or one whose colour is no longer readable. <see cref="ColorRestrictedDiscardRejection.CardNotDiscardable"/>
        /// means the criteria selects it but the lifecycle mutation refuses it, which is the honest
        /// report when a criteria selects cards outside the hand: the query answers about
        /// characteristics and location, and only the mutation knows what a discard from hand requires.
        /// Neither refusal mutates anything.
        /// </summary>
        public static ColorRestrictedDiscardResult Resolve(
            CardCharacteristicCriteria criteria,
            CardInstanceId chosenCardId,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances)
        {
            if (!chosenCardId.IsDefined)
            {
                throw new ArgumentException(
                    "A discard answer requires a defined card instance ID.", nameof(chosenCardId));
            }

            var materializedInstances = (instances ?? throw new ArgumentNullException(nameof(instances)))
                .Where(instance => instance != null)
                .ToArray();
            var candidates = Candidates(criteria, definitions, materializedInstances);
            if (candidates.Count == 0)
            {
                return ColorRestrictedDiscardResult.Reject(ColorRestrictedDiscardRejection.NoCandidate);
            }

            if (!candidates.Contains(chosenCardId))
            {
                return ColorRestrictedDiscardResult.Reject(
                    ColorRestrictedDiscardRejection.ChoiceNotOffered);
            }

            var chosen = materializedInstances.First(instance => instance.Id.Equals(chosenCardId));
            if (!CardLifecycleAuthority.TryDiscardFromHand(chosen))
            {
                return ColorRestrictedDiscardResult.Reject(
                    ColorRestrictedDiscardRejection.CardNotDiscardable);
            }

            return ColorRestrictedDiscardResult.Accept(chosenCardId);
        }

        private static IReadOnlyList<CardInstanceId> Candidates(
            CardCharacteristicCriteria criteria,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances) =>
            CardCharacteristicQuery.FindCandidates(criteria, definitions, instances);
    }
}
