using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;

namespace GuardsDigital.Core.Rules.Displacement
{
    /// <summary>
    /// Milestone 6.5ad: how many spaces a declared displacement distance admits, stated once for every
    /// capability that moves something a stated number of spaces.
    ///
    /// Two rulebook sentences are the whole of it. Page 10: "Steps which include 'you may', 'up to' and
    /// 'if able' are non-mandatory, all other steps are mandatory." Page 11, under Movement in the Card
    /// Text: "Unless the distance is optional ('up to 2 spaces'), or the move itself is optional ('you
    /// may move'), you must move the exact number of spaces, or stop performing the action at that
    /// step." So an optional distance is satisfied by any number of spaces from none at all up to the
    /// declared bound, and every other wording is satisfied by the declared number alone. No clause
    /// anywhere sets a floor of one space; the Movement action itself is an `up to` that page 11 says
    /// applies its card text "even if you choose to move zero spaces".
    ///
    /// It exists because the two displacement capabilities read that sentence differently and one of
    /// them was wrong. Object Push offered distances 0..X and called zero a legal choice, matching
    /// p.18's own "this can result in pushing it 0 spaces"; card-text direct Move offered only 1..X,
    /// not by decision but because its candidates come from the Movement action's reachability walk,
    /// which never emits the mover's own space. Two readings of one sentence is the defect, so the
    /// sentence is read here and each capability consults this rather than answering for itself. A
    /// conformance rule holds them to it.
    ///
    /// The optionality itself is not restated: it is read from the node's own
    /// <see cref="EffectFailurePolicy"/>, which is where milestone 6.2a already put p.10's distinction.
    /// This type never asks what is reachable, occupied, or in a straight line - which spaces a mover
    /// can actually use is the geometry question its capability asks separately.
    ///
    /// Milestone 8.8 adds the one thing the wording cannot carry: a floor the card itself prints.
    /// No clause of the rulebook sets one, which is why the reading above starts at none - but a card
    /// can, and Kinetic Blast's "3 or 4 spaces" does. It is a third argument to the same statement
    /// rather than a second one, because a card that prints a floor is usually a card whose `up to` is
    /// about something else in the same sentence, and both readings then have to be taken together.
    /// </summary>
    public readonly struct AdmittedDistances
    {
        private AdmittedDistances(int minimum, int maximum)
        {
            Minimum = minimum;
            Maximum = maximum;
        }

        /// <summary>
        /// The reading of <paramref name="declaredDistance"/> under the wording
        /// <paramref name="failurePolicy"/> carries.
        /// </summary>
        /// <param name="failurePolicy">
        /// The declaring node's own policy. Only <see cref="EffectFailurePolicy.UpTo"/> makes the
        /// distance itself the decision; `may` and `if able` make the step optional while leaving the
        /// number exact, which is p.11's own separation of "the distance is optional" from "the move
        /// itself is optional".
        /// </param>
        /// <param name="leastPrintedDistance">
        /// Milestone 8.8. The fewest spaces the card itself names, which is 3 in Kinetic Blast's
        /// "Push up to 2 enemy units adjacent to you 3 or 4 spaces" and 0 wherever a card names one
        /// number only.
        ///
        /// It is a third argument rather than a second factory because it answers a different question
        /// from the one above and the two have to be answered together. p.10 and p.11 read the node's
        /// own wording; a floor is printed content, and the two words that carry it here - "up to 2"
        /// over the units and "3 or 4" over the spaces - sit in one sentence, so a card can make the
        /// count optional while naming the distance exactly. That is Kinetic Repulse, and before this
        /// argument existed the only way to say "up to" about the units was to make the spaces optional
        /// too. A floor cannot raise what the declaration admits: it narrows the optional reading from
        /// below and leaves an exact declaration exactly where it was.
        /// </param>
        public static AdmittedDistances ForDeclared(
            EffectFailurePolicy failurePolicy, int declaredDistance, int leastPrintedDistance = 0)
        {
            if (declaredDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(declaredDistance),
                    declaredDistance,
                    "A declared displacement distance must not be negative.");
            }

            if (leastPrintedDistance < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leastPrintedDistance),
                    leastPrintedDistance,
                    "A printed least displacement distance must not be negative.");
            }

            if (leastPrintedDistance > declaredDistance)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leastPrintedDistance),
                    leastPrintedDistance,
                    "A printed least displacement distance must not exceed the declared distance.");
            }

            return failurePolicy == EffectFailurePolicy.UpTo
                ? new AdmittedDistances(leastPrintedDistance, declaredDistance)
                : new AdmittedDistances(declaredDistance, declaredDistance);
        }

        /// <summary>
        /// The fewest spaces that satisfy the declaration: none under an optional distance, and the
        /// declared number under every other wording.
        /// </summary>
        public int Minimum { get; }

        /// <summary>The most spaces it admits, which is always the declared number.</summary>
        public int Maximum { get; }

        /// <summary>
        /// Whether travelling no distance at all satisfies the declaration - the question a capability
        /// asks to decide whether the subject's own space is one of its candidates. A declaration of
        /// zero spaces reads the same way whatever its wording, because standing still is then also the
        /// exact number.
        /// </summary>
        public bool AdmitsStandingStill => Minimum == 0;

        /// <summary>Whether that many spaces satisfies the declaration.</summary>
        public bool Admits(int distance) => distance >= Minimum && distance <= Maximum;

        /// <summary>
        /// Whether the declaration leaves nothing to decide - the question a capability asks before it
        /// opens a distance prompt. Milestone 8.8 states it here rather than at the caller, because the
        /// caller used to ask it by re-reading the node's policy, and a printed floor equal to the
        /// declared distance is now a second way for one number to be the only one admitted.
        /// </summary>
        public bool NamesOneDistance => Minimum == Maximum;

        /// <summary>
        /// Every admitted number of spaces, ascending. This is the candidate list where the distance is
        /// itself the chooser's decision, as p.18's multi-target push makes it: "you choose the distance
        /// and then push all those units that many spaces at the same time".
        /// </summary>
        public IReadOnlyList<int> Enumerate() =>
            Enumerable.Range(Minimum, (Maximum - Minimum) + 1).ToArray();
    }
}
