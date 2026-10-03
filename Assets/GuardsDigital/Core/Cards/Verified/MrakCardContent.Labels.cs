using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.40. The words on every control Mrak's composed faces raise: the three shifting
    /// Greens' "You may move a unit in range 1 space ...", Avalanche's and Ground Shaker's "May repeat
    /// once", and Rock Solid's "You may retrieve a discarded card". Derived from
    /// <see cref="DecisionLabelRequirementQuery"/> over his dealt faces (the coverage check proves
    /// the two sets equal) and joined to <see cref="DecisionLabelCatalog"/> here; until then his
    /// controls read "DO IT" and "STOP HERE".
    /// </summary>
    public static partial class MrakCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            foreach (var face in new[]
                     {
                         MrakGreenFaces.TreacherousGround, MrakGreenFaces.Rockslide, MrakGreenFaces.Avalanche
                     })
            {
                yield return DecisionStep(face, "shift-root",
                    "Move a unit 1 space beside terrain or a rock token", "Move nobody");
            }

            yield return DecisionStep(MrakGreenFaces.Avalanche, "shift-repeat",
                "Move a unit beside terrain or a rock token again", "Stop here");

            // "Move a unit in radius which is adjacent to terrain, or to a rock token, 1 space. Place a
            // rock token in the space it occupied. May repeat once on a different target."
            yield return DecisionStep(MrakBlueFaces.GroundShaker, "stomp-repeat",
                "Move a different unit and leave a rock token behind", "Stop here");

            yield return DecisionStep(MrakRedFaces.RockSolid, "carapace-may-retrieve",
                "Retrieve a discarded card", "Retrieve nothing");
        }
    }
}
