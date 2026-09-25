using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>Source-verified Green upgrade branch, staged until Snorri is dealt.</summary>
    public static class SnorriGreenFaces
    {
        public const string ContentRevision = "snorri-8-93d-rev-1";
        public static CardDefinitionId OathOfEndurance { get; } =
            new CardDefinitionId("snorri-oath-of-endurance");
        public static CardDefinitionId OathOfFortitude { get; } =
            new CardDefinitionId("snorri-oath-of-fortitude");
        public static CardDefinitionId ChargedRunestone { get; } =
            new CardDefinitionId("snorri-charged-runestone");
        public static CardDefinitionId OathOfPerseverance { get; } =
            new CardDefinitionId("snorri-oath-of-perseverance");
        public static CardDefinitionId InscribedRunebomb { get; } =
            new CardDefinitionId("snorri-inscribed-runebomb");

        public static ActionDefinitionId PrimaryDefense { get; } =
            new ActionDefinitionId("primary-defense");
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");
        public static ActionDefinitionId SecondaryMovement { get; } =
            new ActionDefinitionId("secondary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static IReadOnlyList<CardDefinitionId> OathCards { get; } =
            Array.AsReadOnly(new[]
            {
                OathOfEndurance, OathOfFortitude, OathOfPerseverance
            });

        public static bool IsOath(CardDefinitionId card) =>
            card.Equals(OathOfEndurance) || card.Equals(OathOfFortitude) ||
            card.Equals(OathOfPerseverance);

        public static IReadOnlyList<CardDefinition> Definitions { get; } =
            Array.AsReadOnly(new[]
            {
                Oath(
                    OathOfEndurance, "Oath of Endurance", CardTier.I, 5,
                    PrintedValueChannel.None,
                    new[] { OathOfFortitude, ChargedRunestone },
                    "If a rune is active: Rune 4 — Block a basic attack. " +
                    "Rune 2 — Block a non-ranged attack. This Turn: " +
                    "You are immune to enemy actions."),
                Oath(
                    OathOfFortitude, "Oath of Fortitude", CardTier.II, 5,
                    PrintedValueChannel.Initiative,
                    new[] { OathOfPerseverance, InscribedRunebomb },
                    "If a rune is active: Rune 4 — Block a basic attack. " +
                    "Rune 2 — Block a non-ranged attack. Rune 1 — Block a ranged attack. " +
                    "This Turn: You are immune to enemy actions."),
                Skill(
                    ChargedRunestone, "Charged Runestone", CardTier.II, 5, 3,
                    PrintedValueChannel.Attack,
                    new[] { OathOfPerseverance, InscribedRunebomb },
                    "If a rune is active, an enemy hero in radius: " +
                    "Rune 4 — Discards a green card, if able. " +
                    "Rune 2 — Discards a silver card, if able. " +
                    "Rune 1 — Discards a blue card, if able.",
                    SnorriGreenEffectContent.ChargedRunestone(ContentRevision)),
                Oath(
                    OathOfPerseverance, "Oath of Perseverance", CardTier.III, 4,
                    PrintedValueChannel.Initiative,
                    Array.Empty<CardDefinitionId>(),
                    "If a rune is active: Rune 4 — Block a basic attack. " +
                    "Rune 2 — Block a non-ranged attack. Rune 1 — Block a ranged attack. " +
                    "Rune 3 — Block a non-basic attack. This Turn: " +
                    "You are immune to enemy actions."),
                Skill(
                    InscribedRunebomb, "Inscribed Runebomb", CardTier.III, 4, 4,
                    PrintedValueChannel.Attack,
                    Array.Empty<CardDefinitionId>(),
                    "Choose one active rune. An enemy hero in radius: " +
                    "Rune 4 — Discards a green card, if able. " +
                    "Rune 2 — Discards a silver card, if able. " +
                    "Rune 1 — Discards a blue card, if able. " +
                    "Rune 3 — Discards a gold card, if able.",
                    SnorriGreenEffectContent.InscribedRunebomb(ContentRevision))
            });

        private static CardDefinition Oath(
            CardDefinitionId id, string name, CardTier tier, int initiative,
            PrintedValueChannel item, IReadOnlyList<CardDefinitionId> next,
            string text) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(),
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile),
                        SnorriGreenEffectContent.Oath(id, ContentRevision),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.Block,
                        CardTextTiming.DefenseAction,
                        blockCondition:
                            PrintedBlockCondition.DeclaredByDefenseTextCapability),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 2) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                CardSourceContent.Transcribed(text));

        private static CardDefinition Skill(
            CardDefinitionId id, string name, CardTier tier, int initiative,
            int defense, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> next, string text,
            GuardsDigital.Core.Effects.EffectDefinition effect) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(PrintedValueChannel.Defense, defense)
                        },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                CardSourceContent.Transcribed(text));
    }
}
