using System;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Milestone 8.35z29. How many options one modal admits, once every standing
    /// <see cref="RuleModificationKind.ChoiceCardinality"/> declaration has been read - Misa's Power
    /// Overwhelming, "Whenever you choose one, you may choose two different options instead".
    ///
    /// <para><b>Why this is a query rather than a field on the node.</b> A modal's printed cardinality
    /// is content and never moves; what a permanent ability does to it is a fact about the board, true
    /// only while its hero is standing and only for the hero it belongs to. Asking here means the same
    /// definition offers one bullet before the Ultimate is unlocked and two after it, with nothing
    /// about the card having changed - which is exactly what p.16's permanent abilities do to
    /// everything else they touch.</para>
    ///
    /// <para><b>The two conditions the ruling forces, and why both are asked.</b> The user's
    /// 2026-09-06 ruling narrows the clause to "her own cards' choose-one nodes", and the sentence
    /// itself says "whenever <em>you</em> choose one". So the widening applies only where the hero the
    /// modification affects is <em>both</em> the controller of the card being resolved and the chooser
    /// being asked. The two are the same hero on every modal Misa prints, but they are not the same
    /// question: a card of hers may hand a decision to an opponent - the roster already prints such
    /// nodes - and an enemy's card may hand one to her. Neither is a modal she is choosing on her own
    /// card, and neither is widened.</para>
    ///
    /// <para><b>It never lowers anything.</b> The answer is the larger of the printed ceiling and the
    /// declared one, bounded by the options the node actually has: "two different options" cannot be
    /// satisfied from a one-bullet list, and a declaration cannot invent a bullet the card does not
    /// print. The floor is untouched, because the card says "you may".</para>
    /// </summary>
    public static class ChoiceCardinalityQuery
    {
        /// <summary>
        /// The ceiling this modal admits for this chooser, or <paramref name="printedMaximum"/> where
        /// nothing standing widens it.
        /// </summary>
        /// <param name="printedMaximum">The definition's own <c>maximumSelections</c>.</param>
        /// <param name="optionCount">How many bullets the node actually offers.</param>
        /// <param name="controllerId">Whose card is being resolved.</param>
        /// <param name="chooserId">Whom the decision is being put to.</param>
        public static int MaximumOptions(
            int printedMaximum,
            int optionCount,
            HeroId controllerId,
            HeroId chooserId,
            RuleModificationSet modifications)
        {
            if (modifications == null)
            {
                throw new ArgumentNullException(nameof(modifications));
            }

            if (printedMaximum < 1 || optionCount < 2 ||
                !controllerId.IsDefined || !chooserId.IsDefined ||
                !controllerId.Equals(chooserId))
            {
                return printedMaximum;
            }

            var declared = modifications
                .For(RuleObject.ForHero(chooserId))
                .Where(applied =>
                    applied.Modification.Kind == RuleModificationKind.ChoiceCardinality)
                .Select(applied => applied.Modification.Amount)
                .DefaultIfEmpty(printedMaximum)
                .Max();

            return Math.Min(Math.Max(printedMaximum, declared), optionCount);
        }
    }
}
