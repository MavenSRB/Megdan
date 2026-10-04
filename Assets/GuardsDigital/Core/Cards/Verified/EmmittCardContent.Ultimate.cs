using System;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string AlternativeTimelinesText =
            "You may play two cards each turn. If you do, after the cards are revealed, " +
            "retrieve one of your unresolved cards.";

        public static CardDefinition AlternativeTimelinesDefinition { get; } = new CardDefinition(
            AlternativeTimelines, ContentRevision, "Alternative Timelines", default,
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.UltimateCard(), Array.Empty<ActionDefinition>(),
            UltimateRuleDefinition.PermanentDeclaration(
                "emmitt-alternative-timelines", TwoCardTurnRule.RuleKind),
            CardSourceContent.Transcribed(AlternativeTimelinesText));
    }
}
