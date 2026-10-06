using System.Collections.Generic;
using static GuardsDigital.Core.Cards.DecisionLabelDeclaration;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public static IEnumerable<DecisionLabelDeclaration> DecisionLabels()
        {
            yield return DecisionArm(MindGrip, MindGrip.Value + "-borrow-arm",
                "Perform an action on an enemy hero's previous-slot card");
            yield return DecisionArm(MindGrip, MindGrip.Value + "-minion-arm",
                "Defeat an adjacent minion");

            yield return DecisionStep(DiabolicalLaughter,
                DiabolicalLaughter.Value + "-laugh-optional",
                "Laugh diabolically and choose up to three effects",
                "Do not laugh");
            for (var pass = 1; pass <= 3; pass++)
            {
                var prefix = DiabolicalLaughter.Value + "-choice-" + pass;
                yield return DecisionStep(DiabolicalLaughter, prefix + "-optional",
                    pass == 1 ? "Choose a laughter effect" : "Choose another laughter effect",
                    "Stop choosing laughter effects");
                yield return DecisionArm(DiabolicalLaughter, prefix + "-swap-body",
                    "Swap with an Illusion token in radius");
                yield return DecisionArm(DiabolicalLaughter, prefix + "-place-body",
                    "Place an Illusion token in an adjacent space");
                yield return DecisionArm(DiabolicalLaughter, prefix + "-cards-body",
                    "Swap two resolved cards of an enemy hero in radius");
            }

            yield return DecisionArm(PhantasmalSentry, "phantasmal-sentry-image-arm",
                "Target a hero in range beside an Illusion token");
            yield return DecisionArm(PhantasmalSentry, "phantasmal-sentry-adjacent-arm",
                "Target an adjacent unit");
            foreach (var card in new[] { PhantasmalWarrior, PhantasmalChampion })
            {
                yield return DecisionArm(card, card.Value + "-image-arm",
                    "Move an Illusion token, then target a nearby enemy hero");
                yield return DecisionArm(card, card.Value + "-adjacent-arm",
                    "Target an adjacent unit");
            }
            foreach (var card in new[] { TwistFate, DeviousScheme })
                yield return DecisionStep(card, card.Value + "-may-swap",
                    "Swap an enemy unit with an Illusion token", "Do not swap");

            foreach (var card in new[] { ImbueDoubt, TimeToReconsider, AnIllusionOfChoice })
            {
                foreach (var color in NebKherDelayedDoubtContent.NameableColors)
                {
                    var name = color.ToString();
                    var prefix = card.Value + "-" + name.ToLowerInvariant();
                    yield return DecisionArm(card, prefix + "-create",
                        "Choose " + name + " for next-turn discard");
                }
            }

            foreach (var card in new[]
                { FleetingImage, MultipleProjections, MasterOfIllusions })
                yield return DecisionStep(card, card.Value + "-may-swap",
                    "Swap with an Illusion token in play", "Stay in this space");
        }
    }
}
