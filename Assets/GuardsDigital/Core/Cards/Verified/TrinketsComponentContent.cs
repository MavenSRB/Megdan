using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Physical components of Trinkets; registration waits for his dealing milestone.</summary>
    public static class TrinketsComponentContent
    {
        public static TokenDefinitionId BarrierId { get; } =
            new TokenDefinitionId("trinkets-barrier");

        public static TokenDefinitionReference BarrierV1 =>
            new TokenDefinitionReference(BarrierId, 1);

        public static TokenDefinition Barrier { get; } = new TokenDefinition(
            BarrierId,
            1,
            "Barrier",
            3,
            new[]
            {
                new TokenPropertyDefinition(
                    "adjacent-friendly-defense",
                    "printed-defense-bonus",
                    "while-barrier-remains-placed",
                    RuleModification.ForValue(PrintedValueChannel.Defense, 1),
                    TokenPropertyAffectedObject.FriendlyHeroesAdjacentToTheToken)
            },
            TokenRemovalCondition.EndOfRoundCleanup);

        public static IEnumerable<TokenDefinition> Tokens()
        {
            yield return Barrier;
        }
    }
}
