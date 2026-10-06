using System;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string CrackInRealityText =
            "Split the board into two sides with a straight line of spaces drawn through your space. " +
            "This turn: Units on either side of the line cannot interact with objects and spaces on " +
            "the other side of the line as if they did not exist. (This includes movement, placement, " +
            "radius-based effects, etc.)";

        public const string ShiftRealityText =
            "Split the board into two sides with a straight line of spaces drawn through your space. " +
            "This turn: Units on either side of the line cannot interact with you, and with objects " +
            "and spaces on the other side of the line, as if they did not exist.";

        public static CardDefinition CrackInRealityDefinition { get; } = SplitFace(
            CrackInReality, "Crack in Reality", CardTier.II,
            PrintedValueChannel.Defense,
            new[] { AnIllusionOfChoice, ShiftReality },
            NebKherComponentContent.CrackRealityV1, CrackInRealityText);

        public static CardDefinition ShiftRealityDefinition { get; } = SplitFace(
            ShiftReality, "Shift Reality", CardTier.III,
            PrintedValueChannel.Range, Array.Empty<CardDefinitionId>(),
            NebKherComponentContent.ShiftRealityV1, ShiftRealityText);

        private static CardDefinition SplitFace(
            CardDefinitionId id, string name, CardTier tier, PrintedValueChannel item,
            CardDefinitionId[] upgrades, EffectCapabilityReference split, string text) =>
            new CardDefinition(id, ContentRevision, name, new InitiativeValue(11),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, SplitEffect(id, split)),
                    Movement(2), Defense(5)
                },
                null, CardSourceContent.Transcribed(text));

        private static EffectDefinition SplitEffect(
            CardDefinitionId card, EffectCapabilityReference capability)
        {
            var root = new EffectNodeId(card.Value + "-split");
            return new EffectDefinition(new EffectDefinitionId(card.Value + "-skill"),
                ContentRevision, root, new[]
                {
                    new EffectNodeDefinition(root, EffectNodeKind.Capability,
                        EffectFailurePolicy.Mandatory, capability)
                });
        }
    }
}
