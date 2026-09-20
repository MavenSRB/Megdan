using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Min the Dragonmonk's decision windows, as ONE table - milestone 8.61i.
    ///
    /// <para><b>What this adds to the eight partials beside it.</b> Three of her composition blocks
    /// declared the labels their own faces owe - 8.61c's Stance riders, 8.61f's two Grenade sentences
    /// and 8.61h's Perfect Self modal - each under a block-scoped member name, because eight partials
    /// of one class were being written at once. This is the one reading of them, so that the milestone
    /// that deals her concatenates a single name the way <c>DecisionLabelCatalog</c> concatenates every
    /// other hero's.</para>
    ///
    /// <para><b>It IS wired into <see cref="DecisionLabelCatalog"/> from milestone 8.61j on, and the
    /// timing is the rule rather than an accident.</b> Every contribution in that file joins at a
    /// DEALING milestone - Rowenna's fifteen declarations joined at 8.57g although 8.57f composed
    /// them - because what a table is for is a window over a card a match deals. 8.61j is the block
    /// that adds her line, and <c>DecisionLabelCoverageTests</c>' own hand-named list of dealt heroes
    /// is the other half of that same change.</para>
    ///
    /// <para><b>The four blocks that declare nothing declare nothing for a reason.</b> Her Smoke
    /// Bomb, her three mine faces, the mine trap and her Gold pair open no window the resolution
    /// session labels: a step that may simply fail, a mandatory placement whose spaces are ordinary
    /// Place choices and a borrow that names one candidate without a prompt are not decision STEPS,
    /// which is the same explicit empty contribution Silverarrow's row records. Her three Blocks
    /// declare exactly one between them - Ruse's optional re-place - which is 8.61b's own recorded
    /// debt, paid at the dealing.</para>
    /// </summary>
    public static partial class MinEffectContent
    {
        /// <summary>
        /// Every decision window her set opens, in composition order: her five Stances' riders, both
        /// Grenades' "up to N enemy heroes", Perfect Self's one-or-both modal, Flurry of Blows'
        /// repeat and Ruse's optional re-place.
        /// </summary>
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels() =>
            StanceDecisionLabels()
                .Concat(GrenadeDecisionLabels())
                .Concat(DisciplineDecisionLabels())
                // Milestone 8.61j. Ruse's optional re-place, the one window her three Block faces
                // open - a debt 8.61b wrote down and the dealing pays, because a decision label is a
                // window over a card a match deals.
                .Concat(VanishingDecisionLabels());
    }
}
