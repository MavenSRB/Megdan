using System;
using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public const string ImbueDoubtText =
            "Choose a color. Next turn, after playing cards: An enemy hero in radius " +
            "discards a card of that color, if able.";
        public const string TimeToReconsiderText = ImbueDoubtText;
        public const string AnIllusionOfChoiceText =
            "Choose a color. Next turn, after playing cards: Up to two enemy heroes " +
            "in radius discard a card of that color, if able.";

        public static CardDefinition ImbueDoubtDefinition { get; } = DoubtFace(
            ImbueDoubt, "Imbue Doubt", CardTier.I, 10, 4, 3,
            PrintedValueChannel.None,
            new[] { TimeToReconsider, CrackInReality }, ImbueDoubtText);

        public static CardDefinition TimeToReconsiderDefinition { get; } = DoubtFace(
            TimeToReconsider, "Time to Reconsider", CardTier.II, 11, 5, 4,
            PrintedValueChannel.Attack,
            new[] { AnIllusionOfChoice, ShiftReality }, TimeToReconsiderText);

        public static CardDefinition AnIllusionOfChoiceDefinition { get; } = DoubtFace(
            AnIllusionOfChoice, "An Illusion of Choice", CardTier.III, 11, 5, 4,
            PrintedValueChannel.Movement, Array.Empty<CardDefinitionId>(),
            AnIllusionOfChoiceText);

        private static CardDefinition DoubtFace(
            CardDefinitionId card, string title, CardTier tier, int initiative,
            int defense, int radius, PrintedValueChannel item,
            CardDefinitionId[] upgrades, string text) =>
            new CardDefinition(card, ContentRevision, title,
                new InitiativeValue(initiative), CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, upgrades),
                new[]
                {
                    new ActionDefinition(PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        NebKherDelayedDoubtContent.ChooseColor(card)),
                    Movement(2), Defense(defense)
                }, null, CardSourceContent.Transcribed(text));
    }
}
