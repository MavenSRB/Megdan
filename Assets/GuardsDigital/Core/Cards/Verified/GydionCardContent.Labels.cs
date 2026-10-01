using System.Collections.Generic;
using System.Linq;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.30i. The words on every control Gydion's dealt fronts and his spells raise (8.46a),
    /// aggregated from the blocks that composed them and joined to <see cref="DecisionLabelCatalog"/>:
    /// the "Choose and cast one" bullets (9.30i), Vampiric Touch's retrieval (9.30d), Find Familiar's
    /// removals (9.30e), the five immunity, discard, Life and forced-move spells' none (9.30f), and
    /// Wish's casts with Prepare Spells' "add, or cast Wish instead" (9.30g). Each is derived from the
    /// content, and <c>GydionDealingTests</c> asserts the set equals what
    /// <see cref="DecisionLabelRequirementQuery"/> derives.
    /// </summary>
    public static partial class GydionCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels() =>
            CastingCardDecisionLabels()
                .Concat(ReuseSpellDecisionLabels())
                .Concat(CountingSpellDecisionLabels())
                .Concat(ImmunityDiscardLifeMoveDecisionLabels())
                .Concat(ArchwizardDecisionLabels());
    }
}
