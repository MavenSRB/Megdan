using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>The source-verified Ancestral Blue Skill upgrade branch.</summary>
    public static class SnorriBlueSupportFaces
    {
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static CardDefinition AncestralBoon { get; } =
            new CardDefinition(
                SnorriBlueMovementFaces.AncestralBoon,
                SnorriBlueMovementFaces.ContentRevision,
                "Ancestral Boon", new InitiativeValue(9),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.II, PrintedValueChannel.Defense,
                    new[] { SnorriBlueMovementFaces.AncestralGrace }),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SnorriBlueSupportEffectContent.ForBoon(
                            SnorriBlueMovementFaces.ContentRevision)),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 6) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(
                    "If a rune is active, a friendly hero in radius may: " +
                    "Rune 2 — Swap a resolved card with a card in hand. " +
                    "Rune 3 — Retrieve all their discarded cards."));

        public static CardDefinition AncestralGrace { get; } =
            new CardDefinition(
                SnorriBlueMovementFaces.AncestralGrace,
                SnorriBlueMovementFaces.ContentRevision,
                "Ancestral Grace", new InitiativeValue(10),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, CardTier.III, PrintedValueChannel.Defense,
                    Array.Empty<CardDefinitionId>()),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 4) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SnorriBlueSupportEffectContent.For(
                            SnorriBlueMovementFaces.AncestralGrace,
                            SnorriBlueMovementFaces.ContentRevision)),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 6) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(
                    "If a rune is active, a friendly hero in radius may: " +
                    "Rune 2 — Swap a resolved card with a card in hand. " +
                    "Rune 3 — Retrieve all their discarded cards. " +
                    "Rune 1 — Swap their item with their card of the same tier and color."));

        public static IReadOnlyList<CardDefinition> Definitions =>
            Array.AsReadOnly(new[] { AncestralBoon, AncestralGrace });
    }
}
