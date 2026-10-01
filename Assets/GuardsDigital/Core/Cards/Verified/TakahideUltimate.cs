using System;
using System.Collections.Generic;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Milestone 9.32f. Takahide's Ultimate, Ready for War: "Return your silver card to your deck and
    /// take two gold cards from your deck into your hand. (You now have a total hand size of 6
    /// cards.)"
    ///
    /// <para>TK-Q5 (A): an instruction performed once, at the level-up that unlocks it, whether or not
    /// he is on the board. p.15 performs the unlock after p.8's Retrieve Cards, so Bushido and one
    /// gold card are in his hand and the other two golds in his deck; from then on he plays from six
    /// cards (three golds and his three colours), Bushido stays in the deck, and a gold's "swap with a
    /// different gold card in your deck" finds none and does nothing. The reminder text composes to
    /// nothing: the hand size follows from the movement.</para>
    /// </summary>
    public static partial class TakahideCardContent
    {
        public const string ReadyForWarText =
            "Return your silver card to your deck and take two gold cards from your deck into your hand. " +
            "(You now have a total hand size of 6 cards.)";

        public const string ReadyForWarRuleId = "takahide-ready-for-war";
        public const string ReadyForWarRuleKind = "TakahideReadyForWar";

        /// <summary>Milestone 9.32f's face: the Ultimate.</summary>
        public static IReadOnlyList<CardDefinition> UltimateDefinitions => LazyUltimate.Value;

        /// <summary>Purple, Tier IV: Ultimate, no Initiative and no icon.</summary>
        public static CardDefinition ReadyForWarDefinition => LazyUltimate.Value[0];

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyUltimate =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                new CardDefinition(
                    ReadyForWar, ContentRevision, "Ready for War", default,
                    CardContentAuthority.VerifiedOfficial,
                    CardProgressionProfile.UltimateCard(),
                    Array.Empty<ActionDefinition>(),
                    UltimateRuleDefinition.UnlockCardMovementAbility(
                        ReadyForWarRuleId, ReadyForWarRuleKind,
                        new UltimateUnlockCardMovement(CardColor.Silver, CardColor.Gold, takenFromDeckCount: 2)),
                    CardSourceContent.Transcribed(ReadyForWarText))
            }));
    }
}
