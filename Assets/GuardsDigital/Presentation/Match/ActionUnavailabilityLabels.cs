using System;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35s (the SIXTH amendment's user-approved cursor-tooltip design: "NOTHING IS EVER
    /// GREYED WITHOUT SAYING WHY"). The fixed short vocabulary a greyed control's hover tooltip names
    /// its reason from.
    ///
    /// <para>Every word here is composed from a fact the authority already published on the offer or
    /// option a view model carries - never a rule re-derived here, exactly as
    /// <see cref="NarrationLabels"/> composes its own sentences from facts a projection already states.
    /// A control whose unavailability cause is not one of the named shapes below draws
    /// <see cref="Generic"/> rather than a guessed word: <c>ActionUnavailabilityKind.None</c> is itself
    /// documented ("Core/Cards/ActionUnavailability.cs") to mean "either available, or unavailable for a
    /// reason this vocabulary does not yet name," and this class is what keeps that second reading
    /// honest instead of inventing an explanation the authority never gave.</para>
    /// </summary>
    public static class ActionUnavailabilityLabels
    {
        /// <summary>
        /// The named absence: a greyed control whose authority-side cause is not one of the named
        /// shapes below. Shipped deliberately rather than guessed - see the milestone's recorded gap
        /// for the printed-action-region family, whose single unstated bit conflates several distinct
        /// authority-side causes with no published fact that tells them apart.
        /// </summary>
        public const string Generic = "UNAVAILABLE";

        /// <summary>
        /// Milestone 8.8b's Clear, and only Clear: <c>ClearOptionProjection</c> stamps its own
        /// replacement offer unavailable for exactly one reason - no token stands adjacent to the actor
        /// to Clear - so this word is safe wherever that specific offer is unavailable, with no other
        /// cause the class can produce. It is not applied to the general printed-action-region family
        /// (an Attack or Skill offer), whose own unavailable state can also mean the turn's other
        /// printed action is already under way, an uncomposed card text, or an actor restriction the
        /// authority does not yet thread through <c>ActionUnavailability</c> - see <see cref="Generic"/>.
        /// </summary>
        public const string NoTarget = "NO TARGET";

        /// <summary>
        /// Milestone 8.35l's reason, threaded end to end from <c>RestrictionQuery.TryGetRestriction</c>
        /// through <c>ActionUnavailability.FromRestriction</c>, the wire, and the read model: a standing
        /// rule modification bars every subject this action would apply to, and the authority names its
        /// source. Presentation supplies only the display names it already draws everywhere else - the
        /// hero rail's own name and the card catalog's own display name - never a card or hero identity
        /// re-read from anywhere new.
        /// </summary>
        public static string BlockedBy(string blockingHeroDisplayName, string blockingCardDisplayName)
        {
            if (string.IsNullOrWhiteSpace(blockingHeroDisplayName))
            {
                throw new ArgumentException(
                    "A BLOCKED BY reason names the blocking hero.", nameof(blockingHeroDisplayName));
            }

            if (string.IsNullOrWhiteSpace(blockingCardDisplayName))
            {
                throw new ArgumentException(
                    "A BLOCKED BY reason names the blocking card.", nameof(blockingCardDisplayName));
            }

            return $"BLOCKED BY {blockingHeroDisplayName} — {blockingCardDisplayName}";
        }
    }
}
