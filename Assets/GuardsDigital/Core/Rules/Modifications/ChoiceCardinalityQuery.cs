using System;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Milestone 8.35z33. How many options one modal admits, once every standing
    /// <see cref="RuleModificationKind.ChoiceCardinality"/> declaration has been read - Whisper's Grim
    /// Reaper, "When performing basic actions you may choose one, or both."
    ///
    /// <para><b>Why this is a query rather than a field on the node.</b> A modal's printed cardinality
    /// is content and never moves; what a permanent ability does to it is a fact about the board, true
    /// only while its hero is standing and only for the hero it belongs to. Asking here means the same
    /// definition offers one bullet before the Ultimate is unlocked and two after it, with nothing
    /// about the card having changed - which is exactly what rulebook p.16's permanent abilities do to
    /// everything else they touch.</para>
    ///
    /// <para><b>The two conditions the sentence forces, and why both are asked.</b> "When performing"
    /// names the hero doing it, so the widening applies only where the hero the modification affects
    /// is <em>both</em> the controller of the card being resolved and the chooser being asked. The two
    /// are the same hero on Death Seeker and on Swift Justice, but they are not the same question:
    /// Whisper's own Lesser Evil and Greater Good hand their modal to an <em>opponent</em>, and her
    /// Ultimate must not widen a decision she is not making. Neither is a modal she is choosing on her
    /// own card, and neither is widened.</para>
    ///
    /// <para><b>And a third condition, which is Grim Reaper's alone.</b> The face says "basic actions",
    /// which p.9 defines by the card rather than by the action - "All actions on a basic card are
    /// basic actions" - so a declaration carrying
    /// <see cref="RuleValueApplicability.ControllerBasicAction"/> is read only while the card being
    /// resolved is one. An unqualified declaration reaches every modal of its hero's, which is what a
    /// source that names no class of action means.</para>
    ///
    /// <para><b>It never lowers anything.</b> The answer is the larger of the printed ceiling and the
    /// declared one, bounded by the options the node actually has: "or both" cannot be satisfied from
    /// a one-bullet list, and a declaration cannot invent a bullet the card does not print. The floor
    /// is untouched, because the card says "you may" - taking one bullet stays a complete answer.</para>
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
        /// <param name="actionClassification">
        /// p.9's supertype of the card being resolved, which a declaration scoped to basic actions is
        /// read against. <see cref="CardClassification.None"/> means the caller could not say, and a
        /// scoped declaration is then not applied - an unknown card is not evidence of a basic one.
        /// </param>
        public static int MaximumOptions(
            int printedMaximum,
            int optionCount,
            HeroId controllerId,
            HeroId chooserId,
            RuleModificationSet modifications,
            CardClassification actionClassification = CardClassification.None)
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
                    applied.Modification.Kind == RuleModificationKind.ChoiceCardinality &&
                    Reaches(applied.Modification.ValueApplicability, actionClassification))
                .Select(applied => applied.Modification.Amount)
                .DefaultIfEmpty(printedMaximum)
                .Max();

            return Math.Min(Math.Max(printedMaximum, declared), optionCount);
        }

        /// <summary>
        /// Whether a declaration's own scope admits the card being resolved. An unqualified
        /// declaration admits every card; a basic-action one admits p.9's Basic supertype and nothing
        /// else, including the case where the caller could not name a supertype at all.
        /// </summary>
        private static bool Reaches(
            RuleValueApplicability applicability, CardClassification actionClassification)
        {
            switch (applicability)
            {
                case RuleValueApplicability.Always:
                    return true;

                case RuleValueApplicability.ControllerBasicAction:
                    return actionClassification == CardClassification.Basic;

                // Milestone 8.69a. "While you are performing actions", with no class of action named.
                // The two conditions above have already established that the hero this declaration
                // affects is both the controller of the card being resolved and the chooser, which is
                // the whole of what this scope asks - so every modal of that hero's is reached. No
                // declaration in the build carries it on this family today; the arm is stated rather
                // than left to the default below, because "any action" and "says nothing here" are
                // different answers and a reader must not have to guess which one silence meant.
                case RuleValueApplicability.ControllerAction:
                    return true;

                default:
                    // ControllerBasicAttackAction is a value declaration's scope and names an Attack's
                    // printed number; a modal's cardinality is not a value on a channel, so a
                    // declaration carrying it says nothing here rather than being read as "always".
                    return false;
            }
        }
    }
}
