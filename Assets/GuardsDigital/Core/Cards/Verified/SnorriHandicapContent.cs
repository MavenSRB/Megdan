using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Snorri's nineteenth front, p.20's boxed Handicap.</summary>
    public static class SnorriHandicapContent
    {
        public const string ContentRevision = SnorriCardContent.ContentRevision;
        public static CardDefinitionId RuneShards { get; } =
            new CardDefinitionId("snorri-rune-shards");

        public static CardDefinition Definition { get; } = new CardDefinition(
            RuneShards, ContentRevision, "Rune Shards", new InitiativeValue(10),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(
                    SnorriAttackFaces.PrimaryRangedAttack,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[]
                    {
                        new PrintedValueDefinition(PrintedValueChannel.Attack, 1),
                        new PrintedValueDefinition(PrintedValueChannel.Range, 2)
                    },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.Ranged,
                    default,
                    null,
                    PrintedTextComposition.DeferredUntil(
                        "first odd-player-count configuration"),
                    PrintedDefenseResolution.None,
                    CardTextTiming.None),
                new ActionDefinition(
                    SnorriAttackFaces.SecondaryMovement,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(
                        CardTextApplicationPolicy.Suppressed, false, true)),
                new ActionDefinition(
                    SnorriAttackFaces.SecondaryDefense,
                    ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(
                        CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(
                        CardLocation.Hand, CardLocation.DiscardPile))
            },
            null,
            CardSourceContent.Transcribed(
                "Target a hero adjacent to you. If a rune is active: " +
                "Rune 2 — +3 Attack. Rune 3 — If you target a hero, gain 2 coins. " +
                "Rune 4 — Repeat once on a different hero in range."));
    }
}
