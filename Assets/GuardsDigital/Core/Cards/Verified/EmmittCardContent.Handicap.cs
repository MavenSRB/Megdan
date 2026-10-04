using GuardsDigital.Core.Rules.Modifications;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        public const string TearInTimeText =
            "Target a hero adjacent to you. After the attack, Next Turn: Heroes with lower " +
            "initiative act before heroes with higher initiative. This effect ignores immunity.";

        // p.6 returns this Handicap to the box. Its transcribed definition is kept apart from the
        // eighteen dealt fronts, including the narrower printed hero-only Attack target.
        public static CardDefinition TearInTimeDefinition { get; } = new CardDefinition(
            TearInTime, ContentRevision, "Tear in Time", new InitiativeValue(10),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    PrimaryAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default,
                    GrantEffect(TearInTime, EmmittActiveEffectContent.ReverseTimeV1),
                    PrintedTextComposition.None, PrintedDefenseResolution.None,
                    CardTextTiming.AfterTheAttack,
                    PrintedAttackTargetPolicy.ForClassification(AttackRangeKind.NonRanged)
                        .NarrowedToHeroes()),
                Movement(1), Defense(1)
            },
            null, CardSourceContent.Transcribed(TearInTimeText));
    }
}
