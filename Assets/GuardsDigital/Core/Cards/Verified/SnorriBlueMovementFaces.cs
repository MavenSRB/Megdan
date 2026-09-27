using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// The three source-verified Blue Passage Movement faces, staged until Snorri is dealt.
    /// The two Ancestral Skill identities name their sibling branches but are composed
    /// separately inside the same approved Blue milestone.
    /// </summary>
    public static class SnorriBlueMovementFaces
    {
        public const string ContentRevision = SnorriCardContent.ContentRevision;

        public static CardDefinitionId SafePassage { get; } =
            new CardDefinitionId("snorri-safe-passage");
        public static CardDefinitionId HiddenPassage { get; } =
            new CardDefinitionId("snorri-hidden-passage");
        public static CardDefinitionId DeepPassage { get; } =
            new CardDefinitionId("snorri-deep-passage");
        public static CardDefinitionId AncestralBoon { get; } =
            new CardDefinitionId("snorri-ancestral-boon");
        public static CardDefinitionId AncestralGrace { get; } =
            new CardDefinitionId("snorri-ancestral-grace");

        public static ActionDefinitionId PrimaryMovement { get; } =
            new ActionDefinitionId("primary-movement");
        public static ActionDefinitionId SecondaryDefense { get; } =
            new ActionDefinitionId("secondary-defense");

        public static IReadOnlyList<CardDefinition> Definitions => LazyDefinitions.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDefinitions =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Build(SafePassage, "Safe Passage", CardTier.I, 9, 5,
                    PrintedValueChannel.None,
                    new[] { HiddenPassage, AncestralBoon },
                    "If the Rune 1 rune is active, This turn: You may ignore obstacles."),
                Build(HiddenPassage, "Hidden Passage", CardTier.II, 9, 6,
                    PrintedValueChannel.Attack,
                    new[] { DeepPassage, AncestralGrace },
                    "If a rune is active: Rune 1 — This turn: You may ignore obstacles. " +
                    "Rune 3 — This turn: You are immune to enemy actions."),
                Build(DeepPassage, "Deep Passage", CardTier.III, 10, 6,
                    PrintedValueChannel.Range, Array.Empty<CardDefinitionId>(),
                    "If a rune is active: Rune 1 — This turn: You may ignore obstacles. " +
                    "Rune 3 — This turn: You are immune to enemy actions. " +
                    "Rune 4 — Gain +2 Movement.")
            }));

        private static CardDefinition Build(
            CardDefinitionId card, string title, CardTier tier,
            int initiative, int defense, PrintedValueChannel item,
            IReadOnlyList<CardDefinitionId> next, string text) =>
            new CardDefinition(
                card, ContentRevision, title, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, item, next),
                new[]
                {
                    new ActionDefinition(
                        PrimaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Movement, 3)
                        },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        SnorriBlueMovementEffectContent.For(card, ContentRevision),
                        PrintedTextComposition.None,
                        PrintedDefenseResolution.None,
                        CardTextTiming.BeforeTheMovement),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[]
                        {
                            new PrintedValueDefinition(
                                PrintedValueChannel.Defense, defense)
                        },
                        new ActionDefinitionPolicy(
                            CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                },
                null, CardSourceContent.Transcribed(text));
    }
}
