using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Which cards a defender may answer rulebook p.14's step 3 with.
    ///
    /// <para>Milestone 8.11 gave the question its own home for two reasons. The first is that the
    /// answer stopped being "every Defense action in hand": p.14's Block is printed as a card's primary
    /// and performed only in reaction, so the reaction window now admits two shapes rather than one.
    /// The second is milestone 8.4's honesty rule. A printed action whose card text has no typed
    /// composition is drawn greyed and refused everywhere a turn offers one, because performing it
    /// would carry out the printed values and silently drop the sentence beside them - and until this
    /// milestone the Defense path asked that question nowhere at all, because no Defense had ever
    /// carried text. Wasp's two Block riders are the first that do.</para>
    ///
    /// <para>It is public rather than internal so that both test tiers can name it. That is not a
    /// preference: <c>RISK-VALIDATION-001</c> is the standing risk that an internal member reads fine
    /// in the single-assembly cross-platform harness and does not compile in Unity's separate test
    /// assembly, and this rule is one both tiers should be able to state.</para>
    /// </summary>
    public static class DefenseOfferQuery
    {
        /// <summary>
        /// The defender's legal answers to an incoming attack: every Defense action they may perform
        /// from hand, minus any whose printed text this build cannot yet carry out.
        /// </summary>
        public static IReadOnlyList<ActionOffer> Offers(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            HeroId defenderId,
            RuleModificationSet modifications)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));

            // p.11's restriction, asked of the whole action kind before any card is looked at.
            if (ActionRestrictionQuery.IsRestrictedFrom(defenderId, ActionKind.Defense, modifications))
            {
                return Array.Empty<ActionOffer>();
            }

            return Array.AsReadOnly(ActionOfferQuery
                .GetAvailableOffers(definitions, instances, defenderId, ActionOfferWindow.Reaction)
                .Where(candidate => IsPerformable(candidate, definitions))
                .ToArray());
        }

        /// <summary>
        /// Whether one offered reaction is a Defense action this build can carry out whole.
        ///
        /// <para>Milestone 8.4's rule, asked here for the first time: a card that says out loud its
        /// printed text has no composition is not offered as a defence, exactly as it is not offered as
        /// a turn action. p.14 makes that costless in the same way p.12 does for a turn - a defender
        /// may always decline to defend - so refusing one card is never refusing the defence.</para>
        /// </summary>
        public static bool IsPerformable(
            ActionOffer offer,
            IReadOnlyList<CardDefinition> definitions)
        {
            if (!ActionOfferQuery.TryFindActionDefinition(offer, definitions, out var action))
            {
                return false;
            }

            return action.Kind == ActionKind.Defense && !action.TextComposition.IsAwaited;
        }
    }
}
