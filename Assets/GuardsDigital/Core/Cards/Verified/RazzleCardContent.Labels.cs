using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.21j. The words on every control Razzle's dealt faces raise (8.46a): each is a p.10
    /// confirm-or-decline, derived from <see cref="DecisionLabelRequirementQuery"/> over his dealt faces
    /// and Twin Strike's triggered body (the test asserts the two sets are equal). Rummage's and
    /// Ransack's "you may retrieve", and the two repeats by a different body - Spectacle's "May repeat
    /// once by a different one of you" and Twin Strike's "a different you may repeat it once". His
    /// spawns, removals and moves of another body raise no confirm-or-decline: their "may" and "up to"
    /// sit on capability steps whose questions are selections. Like Wuk's, the table is
    /// declared beside his content and not yet joined to <see cref="DecisionLabelCatalog"/>, whose
    /// coverage fixture still counts the twenty-two heroes it was built over.
    /// </summary>
    public static partial class RazzleCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionStep(Rummage, RazzleEffectContent.MayRetrieveNode(Rummage, 1),
                "Retrieve a discarded card", "Retrieve nothing");
            for (var index = 1; index <= RazzleEffectContent.RansackMostOtherBodies; index++)
            {
                yield return DecisionStep(Ransack, RazzleEffectContent.MayRetrieveNode(Ransack, index),
                    index == 1 ? "Retrieve a discarded card" : "Retrieve another discarded card",
                    index == 1 ? "Retrieve nothing" : "Stop here");
            }

            yield return DecisionStep(Spectacle, RazzleEffectContent.RepeatNode(Spectacle),
                "Swap again from another of you", "Stop here");
            yield return DecisionStep(TwinStrike, RazzleEffectContent.TwinStrikeRepeatNode,
                "Repeat the attack from another of you", "Stop here");
        }
    }
}
