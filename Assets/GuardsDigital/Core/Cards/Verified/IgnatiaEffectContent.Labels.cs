using System.Collections.Generic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's decision labels - milestone 8.75's completion block. The words a player reads
    /// on every control her dealt faces can raise, so the decision window says what pressing it does
    /// instead of printing a node id.
    ///
    /// <para><b>The set is DERIVED, not guessed.</b> Milestone 8.69f found ten of Brynn's nineteen
    /// controls missing because the table had been written from a reading of the cards rather than from
    /// the resolver's own rule. <see cref="DecisionLabelRequirementQuery"/> is that rule, and
    /// <c>IgnatiaPrintedSetTests</c> asks it of all eighteen dealt faces and compares the answer to this
    /// table row for row - so a face that starts asking, or stops, moves this list rather than passing
    /// unnoticed.</para>
    ///
    /// <para><b>Her whole set raises exactly TWO controls, and both are repeats.</b> Every one of her
    /// eighteen dealt faces is a <see cref="EffectNodeKind.Conditional"/> on the Tie Breaker coin rather
    /// than a p.10 <c>Choose</c>, so no face of hers draws a modal arm at all: the coin picks the line,
    /// and the player is never asked which. What is left is the two printed "may repeat once" clauses -
    /// Loosely-Aimed Firebolts' on its MOON face and Imminent Eruption's on its SUN face - which are
    /// <see cref="EffectNodeKind.RepeatAction"/> at <see cref="EffectFailurePolicy.May"/> and therefore
    /// p.10's confirm-or-decline. Each owes words for both answers.</para>
    ///
    /// <para><b>The coin pause is NOT here.</b> Equilibrium's round-long free choice makes
    /// <c>ignatia-coin-sun@1</c> stop reading the coin and ask the actor which face to apply, and that
    /// question is raised by a CAPABILITY rather than by a node kind
    /// <see cref="GuardsDigital.Core.Rules.EffectResolution.OptionalStepQuery"/> names - a capability's
    /// own candidates carry their own words - so the requirement query names no row for it and none is
    /// declared. Its vocabulary belongs to the Equilibrium block; see ID-Q7c in that milestone's
    /// record.</para>
    /// </summary>
    public static partial class IgnatiaEffectContent
    {
        /// <summary>
        /// The two controls her dealt set raises, keyed by the card that raises them and the node the
        /// pause names. The catalog joins this table at the milestone that DEALS her, exactly as every
        /// hero's table joins it at theirs.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            // ☾ "Target a unit in range in a straight line. May repeat once on a different hero."
            // The repeat is bound to DifferentEnemyHero, so what accepting buys is a second attack on
            // a hero other than the one this instance took.
            yield return DecisionLabelDeclaration.DecisionStep(
                IgnatiaCardContent.LooselyAimedFirebolts,
                "ignatia-attack-moon-repeat",
                "Attack a different hero again",
                "Stop after this attack");

            // ☀ "Target a unit adjacent to you. May repeat once on a minion."
            yield return DecisionLabelDeclaration.DecisionStep(
                IgnatiaCardContent.ImminentEruption,
                "ignatia-attack-sun-repeat",
                "Attack a minion again",
                "Stop after this attack");
        }
    }
}
