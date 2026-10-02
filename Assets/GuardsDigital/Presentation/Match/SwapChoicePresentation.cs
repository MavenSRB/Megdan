using System.Globalization;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 9.32g. The words and the extra card the decision window gives the two swap choices
    /// Takahide's text raises, to the user's approved mockups:
    ///
    /// <list type="bullet">
    /// <item>C - the deck-gold choice (<c>swap-owned-color-card-with-deck-card@1</c>, Bushido): each
    /// candidate is captioned FACEDOWN when the card it brings in will arrive facedown - the swapped
    /// card was resolved or discarded (p.10).</item>
    /// <item>D - a teammate's unresolved-card swap (<c>swap-unresolved-card@1</c>): one window on the
    /// teammate's own seat, their unresolved card on the left captioned with its Initiative, their
    /// cards captioned by Initiative (the number that decides when they act, p.7), and KEEP MY CARD
    /// as the decline.</item>
    /// </list>
    ///
    /// <para>Every reading is of a typed capability reference, a node literal or a card's state - never
    /// a card or hero name.</para>
    /// </summary>
    internal static class SwapChoicePresentation
    {
        internal const string KeepMyCardLabel = "KEEP MY CARD";
        internal const string UnresolvedSwapTitle = "SWAP YOUR UNRESOLVED CARD?";

        internal static bool IsUnresolvedSwap(EffectNodeDefinition node) =>
            node != null && node.Capability.Equals(SyntheticUnresolvedSwapCapabilities.SwapUnresolvedCardV1);

        /// <summary>
        /// Whether the card a deck swap brings in arrives facedown: the node is the colour-named swap
        /// declaring Bushido's flag, and the chooser's card of that colour outside the deck is resolved or
        /// discarded - exactly the case <c>CardLifecycleAuthority.TrySwapWithDeckCard</c> turns facedown.
        /// </summary>
        internal static bool ArrivesFacedown(
            IMatchReadModel projection, PendingEffectChoice choice, EffectNodeDefinition node)
        {
            if (projection == null || choice == null || node == null ||
                !node.Capability.Equals(SyntheticDeckSwapCapabilities.SwapOwnedColorCardWithDeckCardV1) ||
                !Literal(node, SyntheticDeckSwapCapabilities.FacedownWhenRestingParameterName).BooleanValue)
            {
                return false;
            }

            var color = (CardColor)Literal(node, SyntheticDeckSwapCapabilities.ColorParameterName).IntegerValue;
            return projection.Cards.Any(card =>
                card.OwnerId.Equals(choice.ChooserId) &&
                card.State.Location != CardLocation.Deck &&
                CurrentRoundCards.ColorOf(projection, card) == color &&
                (CardLifecycleAuthority.IsResolvedCardInTurnArea(card) ||
                    CardLifecycleAuthority.IsDiscardedRestingCard(card)));
        }

        /// <summary>A candidate's caption: the Initiative for a teammate's swap, FACEDOWN for C.</summary>
        internal static string Relabel(
            IMatchReadModel projection, EffectChoiceCandidate candidate, bool unresolvedSwap, bool arrivesFacedown,
            string label)
        {
            if (candidate.Kind != EffectChoiceCandidateKind.Value ||
                candidate.Value.Kind != EffectValueKind.CardInstance)
            {
                return label;
            }

            if (arrivesFacedown)
            {
                return label + " - FACEDOWN";
            }

            if (!unresolvedSwap)
            {
                return label;
            }

            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(candidate.Value.CardInstanceId));
            if (card == null)
            {
                return label;
            }

            var initiative = InitiativeLabel(projection, card);
            return card.State.Location == CardLocation.DiscardPile
                ? "FROM YOUR DISCARD - " + initiative
                : initiative;
        }

        /// <summary>The chooser's card waiting in this turn's slot - the one the swap would replace.</summary>
        internal static CardInstance UnresolvedCardOf(IMatchReadModel projection, HeroId chooser)
        {
            var slotCard = projection.TurnSlots
                .Where(slot => slot.RoundIndex == projection.RoundIndex && slot.TurnIndex == projection.TurnIndex)
                .SelectMany(slot => slot.Assignments)
                .Where(assignment => assignment.HeroId.Equals(chooser) && assignment.CardInstanceId.HasValue)
                .Select(assignment => assignment.CardInstanceId.Value)
                .FirstOrDefault();
            return slotCard.IsDefined
                ? projection.Cards.FirstOrDefault(card => card.Id.Equals(slotCard) &&
                    CardLifecycleAuthority.IsResolvingCardInTurnArea(card))
                : null;
        }

        internal static string UnresolvedCaption(IMatchReadModel projection, CardInstance card) =>
            "YOUR UNRESOLVED CARD - " + InitiativeLabel(projection, card);

        /// <summary>The eyebrow: whose card asks, and which - "TAKAHIDE - SET AN EXAMPLE".</summary>
        internal static string SourceEyebrow(IMatchReadModel projection, CardInstanceId sourceCardId)
        {
            var card = projection.Cards.FirstOrDefault(value => value.Id.Equals(sourceCardId));
            var definition = card == null
                ? null
                : projection.Definitions.FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            var hero = card == null
                ? null
                : projection.Setup?.Heroes.FirstOrDefault(value => value.HeroId.Equals(card.OwnerId))?.DisplayName;
            return definition == null || hero == null
                ? null
                : (hero + " - " + definition.DisplayName).ToUpperInvariant();
        }

        internal static string UnresolvedSwapQuestion(EffectNodeDefinition node) =>
            Literal(node, SyntheticUnresolvedSwapCapabilities.FromDiscardParameterName).BooleanValue
                ? "You may swap it with a card in your hand or discard. The new card's Initiative decides when you act."
                : "You may swap it with a card in your hand. The new card's Initiative decides when you act.";

        private static string InitiativeLabel(IMatchReadModel projection, CardInstance card)
        {
            var definition = projection.Definitions.FirstOrDefault(value => value.Reference.Equals(card.DefinitionReference));
            return definition == null
                ? "INITIATIVE"
                : "INITIATIVE " + definition.Initiative.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static EffectValue Literal(EffectNodeDefinition node, string parameter) =>
            node?.Inputs.FirstOrDefault(input => input.ParameterName == parameter).Literal ?? default;
    }
}
