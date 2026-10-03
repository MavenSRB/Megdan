using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.40. The words on every control Trinkets' composed faces raise: the bullets of her
    /// three "Choose one" faces, Salvage Parts' "you may retrieve", the Red "may repeat" steps, and
    /// the Barrier faces' "up to" placements. Derived from <see cref="DecisionLabelRequirementQuery"/>
    /// over her dealt faces (the coverage check proves the two sets equal) and joined to
    /// <see cref="DecisionLabelCatalog"/> here; until then her controls read "OPTION 1".
    /// </summary>
    public static partial class TrinketsEffectContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionArm(TrinketsCardContent.RapidRedeployment, "move-place",
                "Move up to 3 spaces and place the Turret beside you");
            yield return DecisionArm(TrinketsCardContent.RapidRedeployment, "defeat-arm",
                "Defeat a minion adjacent to you");

            yield return DecisionArm(TrinketsCardContent.SalvageParts, "place-arm",
                "Place the Turret into a space adjacent to you");
            yield return DecisionArm(TrinketsCardContent.SalvageParts, "move-arm",
                "Remove the Turret and move up to 3 spaces");
            yield return DecisionArm(TrinketsCardContent.SalvageParts, "retrieve-arm",
                "Remove the Turret to retrieve a discarded card");
            yield return DecisionStep(TrinketsCardContent.SalvageParts, "may-retrieve",
                "Retrieve a discarded card", "Retrieve nothing");

            // "May repeat once on a different enemy unit." / "May repeat up to two times on different
            // enemy units." - one node per Red face, under its own card.
            yield return DecisionStep(TrinketsCardContent.SteamDischarge, "trinkets-red-repeat",
                "Attack a different enemy unit beside the Turret", "Stop here");
            yield return DecisionStep(TrinketsCardContent.FlameBelcher, "trinkets-red-repeat",
                "Attack a different enemy unit beside the Turret", "Stop here");

            // "Place up to 2 (3) barrier tokens in radius, with at least one of them adjacent to the
            // Turret": the first placement is the whole offer, each further one is its own "up to".
            foreach (var face in new[]
                     {
                         TrinketsCardContent.DeployableBarrier, TrinketsCardContent.DeployableBastion
                     })
            {
                yield return DecisionStep(face, "trinkets-barrier-root",
                    "Place a Barrier token adjacent to the Turret", "Place no Barrier tokens");
                yield return DecisionStep(face, "trinkets-barrier-optional-1",
                    "Place a second Barrier token in radius", "Place no more Barrier tokens");
            }

            yield return DecisionStep(TrinketsCardContent.DeployableBastion, "trinkets-barrier-optional-2",
                "Place a third Barrier token in radius", "Place no more Barrier tokens");

            yield return DecisionArm(TrinketsCardContent.PerfectedDesign, "swap-arm",
                "Swap with a unit or a token in radius of the Turret");
            yield return DecisionArm(TrinketsCardContent.PerfectedDesign, "place-arm",
                "Place yourself into a space in radius of the Turret");
        }
    }
}
