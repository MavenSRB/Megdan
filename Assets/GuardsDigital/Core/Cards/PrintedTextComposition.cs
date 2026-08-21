using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Whether a printed action's card text has its typed composition attached yet, and - when it does
    /// not - what would produce one.
    ///
    /// Milestone 8.3. Verified Wasp definitions ship their transcribed text as
    /// <see cref="CardSourceContent"/> and attach no <c>EffectRoot</c>: each card's composition lands at
    /// its own 8.5-8.13 slice under a content-revision advance, so no half-implemented text can run in
    /// between. That leaves a definition whose face prints a sentence and whose action performs none of
    /// it, and the whole point of this type is that such a definition has to say so out loud.
    ///
    /// <para><b>Why silence was not an option.</b> A missing <c>EffectRoot</c> already means two
    /// unrelated things. On a Movement or Clear action it means the action is an algorithm rather than
    /// effect content and always was. On an Attack or Defense action it means the same thing today only
    /// because the general text seam does not exist yet (the 7.9a review's F-CARD-01, opening at
    /// milestone 8.5) - so a verified Attack with an unimplemented After-the-attack rider is
    /// indistinguishable from one that prints nothing at all. Milestone 8.4's honesty rule needs that
    /// distinction to grey and refuse exactly the primaries whose text cannot run yet, and refusing is
    /// only defensible if the definition itself states the debt.
    ///
    /// <para><b>The owner is part of the statement.</b> A bare "not composed yet" flag would age into a
    /// permanent excuse. The two shapes below carry the two honest answers this project already uses
    /// elsewhere: a scheduled milestone that owes the work, or a deferral with the named trigger that
    /// would end it - the same vocabulary the roadmap's deferred keywords and the temporary-structure
    /// register's retirement owners use. Neither is a free-floating "later".</para>
    /// </summary>
    public readonly struct PrintedTextComposition : IEquatable<PrintedTextComposition>
    {
        private PrintedTextComposition(string owningMilestoneId, string namedTrigger)
        {
            OwningMilestoneId = owningMilestoneId;
            NamedTrigger = namedTrigger;
        }

        /// <summary>
        /// The default: this action declares no outstanding composition. It is what every action built
        /// before milestone 8.3 carries and what an action whose effect content is attached carries -
        /// the two are told apart by the <c>EffectRoot</c> itself, which is present in the second case
        /// and absent in the first.
        /// </summary>
        public static PrintedTextComposition None => default;

        /// <summary>
        /// This action's face prints text whose typed composition is owed by
        /// <paramref name="owningMilestoneId"/>. <see cref="ActionDefinition"/> refuses the statement on
        /// an action whose card text does not apply, and refuses it together with an attached effect
        /// root, so it can never describe a debt that is already paid or one that could never be owed.
        /// </summary>
        public static PrintedTextComposition AwaitedAt(string owningMilestoneId)
        {
            if (string.IsNullOrWhiteSpace(owningMilestoneId))
            {
                throw new ArgumentException(
                    "An outstanding card-text composition names the milestone that owes it.",
                    nameof(owningMilestoneId));
            }

            return new PrintedTextComposition(owningMilestoneId, null);
        }

        /// <summary>
        /// This action's text is deliberately not scheduled, and <paramref name="namedTrigger"/> is the
        /// condition that would schedule it.
        ///
        /// Wasp's Crackling Dagger is the case this exists for: p.5 returns the Handicap card to the box
        /// at setup and the pitch is a six-player match, so the card's behavior is unreachable and the
        /// 2026-08-21 user decision deferred it under the trigger "first odd-player-count
        /// configuration". Its printed values and words still ship as inventory - what is deferred is
        /// the composition, not the card.
        /// </summary>
        public static PrintedTextComposition DeferredUntil(string namedTrigger)
        {
            if (string.IsNullOrWhiteSpace(namedTrigger))
            {
                throw new ArgumentException(
                    "A deferred card-text composition names the trigger that would schedule it.",
                    nameof(namedTrigger));
            }

            return new PrintedTextComposition(null, namedTrigger);
        }

        /// <summary>The milestone that owes this action's composition, or null.</summary>
        public string OwningMilestoneId { get; }

        /// <summary>The condition that would schedule this action's composition, or null.</summary>
        public string NamedTrigger { get; }

        /// <summary>Whether this action's printed text has a composition still owed to it.</summary>
        public bool IsAwaited =>
            !string.IsNullOrWhiteSpace(OwningMilestoneId) || !string.IsNullOrWhiteSpace(NamedTrigger);

        public bool Equals(PrintedTextComposition other) =>
            string.Equals(OwningMilestoneId, other.OwningMilestoneId, StringComparison.Ordinal) &&
            string.Equals(NamedTrigger, other.NamedTrigger, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is PrintedTextComposition other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OwningMilestoneId == null
                    ? 0
                    : StringComparer.Ordinal.GetHashCode(OwningMilestoneId);
                return (hashCode * 397) ^
                    (NamedTrigger == null ? 0 : StringComparer.Ordinal.GetHashCode(NamedTrigger));
            }
        }

        public override string ToString()
        {
            if (!string.IsNullOrWhiteSpace(OwningMilestoneId))
            {
                return $"awaited at milestone {OwningMilestoneId}";
            }

            return string.IsNullOrWhiteSpace(NamedTrigger)
                ? "none"
                : $"deferred until {NamedTrigger}";
        }
    }
}
