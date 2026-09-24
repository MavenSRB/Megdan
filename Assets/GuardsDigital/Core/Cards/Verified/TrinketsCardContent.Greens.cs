using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class TrinketsCardContent
    {
        public static CardDefinitionId EarlyPrototype { get; } =
            new CardDefinitionId("trinkets-early-prototype");
        public static CardDefinitionId UpdatedDesign { get; } =
            new CardDefinitionId("trinkets-updated-design");
        public static CardDefinitionId SelfDestruct { get; } =
            new CardDefinitionId("trinkets-self-destruct");
        public static CardDefinitionId PerfectedDesign { get; } =
            new CardDefinitionId("trinkets-perfected-design");
        public static CardDefinitionId EmergencyProtocol { get; } =
            new CardDefinitionId("trinkets-emergency-protocol");

        public static IReadOnlyList<CardDefinition> GreenDefinitions => LazyGreens.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyGreens =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                Green(EarlyPrototype, "Early Prototype", CardTier.I, 3, 2, 2, 2,
                    PrintedValueChannel.None,
                    "If you are in radius of the Turret, swap with a unit or a token in radius " +
                    "of the Turret, then remove the Turret.",
                    TrinketsEffectContent.PrototypeSkill(EarlyPrototype, ContentRevision, true),
                    new[] { UpdatedDesign, SelfDestruct }),
                Green(UpdatedDesign, "Updated Design", CardTier.II, 3, 2, 2, 2,
                    PrintedValueChannel.Attack,
                    "If you are in radius of the Turret, swap with a unit or a token in radius " +
                    "of the Turret.",
                    TrinketsEffectContent.PrototypeSkill(UpdatedDesign, ContentRevision, false),
                    new[] { PerfectedDesign, EmergencyProtocol }),
                Green(SelfDestruct, "Self-Destruct", CardTier.II, 3, 2, 2, 3,
                    PrintedValueChannel.Defense,
                    "Up to two enemy heroes in radius of the Turret discard a card, if able. " +
                    "Remove the Turret.",
                    TrinketsEffectContent.DetonationSkill(SelfDestruct, ContentRevision, false),
                    new[] { PerfectedDesign, EmergencyProtocol }),
                Green(PerfectedDesign, "Perfected Design", CardTier.III, 2, 3, 2, 2,
                    PrintedValueChannel.Attack,
                    "If you are in radius of the Turret, choose one: Swap with a unit or a token " +
                    "in radius of the Turret; or place yourself into a space in radius of the Turret.",
                    TrinketsEffectContent.PerfectedSkill(PerfectedDesign, ContentRevision),
                    Array.Empty<CardDefinitionId>()),
                Green(EmergencyProtocol, "Emergency Protocol", CardTier.III, 2, 3, 2, 3,
                    PrintedValueChannel.Range,
                    "Up to two enemy heroes in radius of the Turret discard a card, or are " +
                    "defeated. Remove the Turret.",
                    TrinketsEffectContent.DetonationSkill(
                        EmergencyProtocol, ContentRevision, true),
                    Array.Empty<CardDefinitionId>())
            }));

        private static CardDefinition Green(
            CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int movement, int radius,
            PrintedValueChannel itemIcon, string text,
            GuardsDigital.Core.Effects.EffectDefinition effect,
            IReadOnlyList<CardDefinitionId> next) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Green, tier, itemIcon, next),
                new[]
                {
                    new ActionDefinition(PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect),
                    new ActionDefinition(SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(
                            PrintedValueChannel.Movement, movement) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                            false, true)),
                    new ActionDefinition(SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(
                            PrintedValueChannel.Defense, defense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                            false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(
                            CardLocation.Hand, CardLocation.DiscardPile))
                }, null, CardSourceContent.Transcribed(text));
    }
}
