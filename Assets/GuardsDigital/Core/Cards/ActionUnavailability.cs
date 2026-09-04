using System;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>Why an offer the card prints cannot be taken from the state as it stands.</summary>
    public enum ActionUnavailabilityKind
    {
        /// <summary>
        /// Nothing is being said. Either the offer is available, or it is unavailable for a reason this
        /// vocabulary does not yet name - which a surface must draw as an ordinary greyed control
        /// rather than inventing an explanation for.
        /// </summary>
        None = 0,

        /// <summary>
        /// A standing rule modification bars the activity this action would perform, and the source is
        /// named. Milestone 8.35l's first and only member.
        /// </summary>
        BlockedByRuleModification = 1
    }

    /// <summary>
    /// Milestone 8.35l: the reason an offer is drawn but cannot be taken, carried on the offer itself.
    ///
    /// <para><b>Why this exists.</b> <see cref="ActionOffer"/> has carried a bare
    /// <see cref="ActionOffer.IsAvailable"/> since milestone 6.5af, so an action the rules had proven
    /// empty could be greyed and could say nothing about why. That was tolerable while the only cause
    /// was "this card's text can bind no target", which a player can usually see on the board. It stops
    /// being tolerable once the cause is another hero's standing card text: a player who cannot swap
    /// because Wasp played Magnetic Dagger three spaces away has no way to discover that from the
    /// board, and the live playtest is where that was reported - "it didn't work because of Wasp's
    /// gold ... I had it canceled without knowing why".</para>
    ///
    /// <para><b>What it is not.</b> It is not a message and carries no words. Presentation composes
    /// "BLOCKED BY WASP - MAGNETIC DAGGER" from the hero and card named here, using the display names
    /// it already draws everywhere else. The authority states which source refused and never how to
    /// phrase it, and presentation states the phrasing and never re-derives the rules - which is the
    /// division the whole feedback amendment turns on.</para>
    ///
    /// <para><b>The card, not just the effect.</b> The blocking source's card instance travels here
    /// because that is what a player recognises. The Active-effect instance is carried beside it for a
    /// surface that wants to point at the standing effect rather than at the card that created it.</para>
    /// </summary>
    public readonly struct ActionUnavailability : IEquatable<ActionUnavailability>
    {
        private ActionUnavailability(
            ActionUnavailabilityKind kind,
            RuleActivity blockedActivity,
            HeroId blockingHeroId,
            CardInstanceId blockingCardInstanceId,
            ActiveEffectId blockingActiveEffectId)
        {
            Kind = kind;
            BlockedActivity = blockedActivity;
            BlockingHeroId = blockingHeroId;
            BlockingCardInstanceId = blockingCardInstanceId;
            BlockingActiveEffectId = blockingActiveEffectId;
        }

        /// <summary>The named absence: this offer is saying nothing about why.</summary>
        public static ActionUnavailability None => default;

        /// <summary>
        /// The reason a restriction produced, read straight off the applied modification the rules
        /// query already returned. Nothing is re-derived and nothing is looked up: the modification
        /// that refused is the modification named.
        /// </summary>
        public static ActionUnavailability FromRestriction(AppliedRuleModification restriction)
        {
            if (restriction == null)
            {
                throw new ArgumentNullException(nameof(restriction));
            }

            return new ActionUnavailability(
                ActionUnavailabilityKind.BlockedByRuleModification,
                restriction.Modification.Activity,
                restriction.ControllerHeroId,
                restriction.SourceCardInstanceId,
                restriction.SourceActiveEffectId);
        }

        /// <summary>
        /// The same reason rebuilt from a decoded wire payload. It exists for exactly one caller - the
        /// seat client turning a projection record back into offers - and is deliberately separate from
        /// <see cref="FromRestriction"/> so that nothing inside the rules can compose a reason without
        /// a modification behind it. The decoder validates the vocabulary before calling this.
        /// </summary>
        public static ActionUnavailability BlockedBy(
            RuleActivity blockedActivity,
            HeroId blockingHeroId,
            CardInstanceId blockingCardInstanceId,
            ActiveEffectId blockingActiveEffectId) =>
            new ActionUnavailability(
                ActionUnavailabilityKind.BlockedByRuleModification,
                blockedActivity,
                blockingHeroId,
                blockingCardInstanceId,
                blockingActiveEffectId);

        public ActionUnavailabilityKind Kind { get; }

        /// <summary>The activity the standing modification bars - p.19's Swap or p.18's Place.</summary>
        public RuleActivity BlockedActivity { get; }

        /// <summary>The hero controlling the blocking source.</summary>
        public HeroId BlockingHeroId { get; }

        /// <summary>The card the blocking source came off, where the source knows it.</summary>
        public CardInstanceId BlockingCardInstanceId { get; }

        /// <summary>The standing Active effect, where an Active effect is the source.</summary>
        public ActiveEffectId BlockingActiveEffectId { get; }

        public bool IsStated => Kind != ActionUnavailabilityKind.None;

        public bool Equals(ActionUnavailability other) =>
            Kind == other.Kind &&
            BlockedActivity == other.BlockedActivity &&
            BlockingHeroId.Equals(other.BlockingHeroId) &&
            BlockingCardInstanceId.Equals(other.BlockingCardInstanceId) &&
            BlockingActiveEffectId.Equals(other.BlockingActiveEffectId);

        public override bool Equals(object obj) =>
            obj is ActionUnavailability other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)Kind;
                hashCode = (hashCode * 397) ^ (int)BlockedActivity;
                hashCode = (hashCode * 397) ^ BlockingHeroId.GetHashCode();
                hashCode = (hashCode * 397) ^ BlockingCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ BlockingActiveEffectId.GetHashCode();
                return hashCode;
            }
        }
    }
}
