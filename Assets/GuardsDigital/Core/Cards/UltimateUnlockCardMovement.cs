using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 9.32f. An Ultimate whose printed text is an instruction performed once, when p.16
    /// unlocks it, rather than a permanent ability - Takahide the Warlord's Ready for War: "Return your
    /// silver card to your deck and take two gold cards from your deck into your hand. (You now have a
    /// total hand size of 6 cards.)"
    ///
    /// <para><b>Once, at the unlock, whether or not the hero is on the board (TK-Q5).</b> The sentence
    /// is an instruction, not a condition, and nothing returns the cards afterwards: p.8's Retrieve
    /// Cards brings every card back to the hand, so from then on the hero plays from the larger hand.
    /// The unlock is performed by p.15's level-up after Retrieve Cards, so the hand card named here is
    /// in the hand and the deck cards in the deck.</para>
    ///
    /// <para><b>Typed, never a card name.</b> The movement names colours and a count, read by the
    /// level-up that performs the unlock; no reader names a hero or a face.</para>
    /// </summary>
    public sealed class UltimateUnlockCardMovement
    {
        public UltimateUnlockCardMovement(
            CardColor returnedToDeckColor, CardColor takenFromDeckColor, int takenFromDeckCount)
        {
            if (!IsHandColor(returnedToDeckColor) || !IsHandColor(takenFromDeckColor))
            {
                throw new ArgumentException(
                    "An unlock movement names printed hand colours (rulebook p.15); None and Purple are " +
                    "never on a card in a hand or a deck.");
            }

            if (takenFromDeckCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(takenFromDeckCount), takenFromDeckCount, "A movement takes at least one card.");
            }

            ReturnedToDeckColor = returnedToDeckColor;
            TakenFromDeckColor = takenFromDeckColor;
            TakenFromDeckCount = takenFromDeckCount;
        }

        /// <summary>"Return your silver card to your deck": the owner's hand card of this colour.</summary>
        public CardColor ReturnedToDeckColor { get; }

        /// <summary>"take two gold cards from your deck into your hand": the colour taken.</summary>
        public CardColor TakenFromDeckColor { get; }

        /// <summary>The printed count taken, up to as many as the deck holds.</summary>
        public int TakenFromDeckCount { get; }

        private static bool IsHandColor(CardColor color) =>
            Enum.IsDefined(typeof(CardColor), color) && color != CardColor.None && color != CardColor.Purple;
    }
}
