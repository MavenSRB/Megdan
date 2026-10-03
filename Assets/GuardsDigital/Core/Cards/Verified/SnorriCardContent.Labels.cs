using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.40. The words on every control Snorri's composed faces raise: the Runic attacks'
    /// "you may move 1 space" and "you may retrieve a discarded card", Runic Battleaxe's "May repeat
    /// once on an enemy minion", and the three "a friendly hero in radius may" steps of Ancestral
    /// Boon and Grace - which another seat answers, so each names what THAT hero does. Derived from
    /// <see cref="DecisionLabelRequirementQuery"/> over his dealt faces (the coverage check proves
    /// the two sets equal) and joined to <see cref="DecisionLabelCatalog"/> here; until then his
    /// controls read "DO IT" and "STOP HERE".
    /// </summary>
    public static partial class SnorriCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            foreach (var face in new[]
                     {
                         SnorriAttackFaces.RunicDagger, SnorriAttackFaces.RunicHammer, SnorriAttackFaces.RunicBattleaxe
                     })
            {
                yield return DecisionStep(face, "optional-retrieve",
                    "Retrieve a discarded card", "Retrieve nothing");
            }

            foreach (var face in new[] { SnorriAttackFaces.RunicHammer, SnorriAttackFaces.RunicBattleaxe })
            {
                yield return DecisionStep(face, "optional-move", "Move 1 space", "Stay where you are");
            }

            yield return DecisionStep(SnorriAttackFaces.RunicBattleaxe, "repeat",
                "Attack an enemy minion again", "Stop here");

            foreach (var face in new[]
                     {
                         SnorriBlueMovementFaces.AncestralBoon, SnorriBlueMovementFaces.AncestralGrace
                     })
            {
                yield return DecisionStep(face, "friend-may-swap",
                    "Swap a resolved card with a card in hand", "Keep the cards as they are");
                yield return DecisionStep(face, "friend-may-retrieve",
                    "Retrieve all their discarded cards", "Retrieve nothing");
            }

            yield return DecisionStep(SnorriBlueMovementFaces.AncestralGrace, "friend-may-swap-item",
                "Swap the item with the card of its tier and colour", "Keep the item");
        }
    }
}
