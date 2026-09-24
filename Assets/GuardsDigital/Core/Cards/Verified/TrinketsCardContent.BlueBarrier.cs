using System;
using System.Collections.Generic;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class TrinketsCardContent
    {
        public static ActionDefinitionId PrimarySkill { get; } =
            new ActionDefinitionId("primary-skill");

        public static CardDefinitionId DisruptorJolt { get; } =
            new CardDefinitionId("trinkets-disruptor-jolt");
        public static CardDefinitionId DisruptorPulse { get; } =
            new CardDefinitionId("trinkets-disruptor-pulse");
        public static CardDefinitionId DeployableBarrier { get; } =
            new CardDefinitionId("trinkets-deployable-barrier");
        public static CardDefinitionId DisruptorGrid { get; } =
            new CardDefinitionId("trinkets-disruptor-grid");
        public static CardDefinitionId DeployableBastion { get; } =
            new CardDefinitionId("trinkets-deployable-bastion");

        public const string BarrierText =
            "Place up to 2 barrier tokens in radius, with at least one of them adjacent to the " +
            "turret; you and friendly heroes gain +1 Defense for each barrier token they are adjacent to.";
        public const string BastionText =
            "Place up to 3 barrier tokens in radius, with at least one of them adjacent to the " +
            "turret; you and friendly heroes gain +1 Defense for each barrier token they are adjacent to.";

        public static IReadOnlyList<CardDefinition> BarrierDefinitions => LazyBarriers.Value;

        public static IReadOnlyList<CardDefinition> DisruptorDefinitions => LazyDisruptors.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDisruptors =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                DisruptorCard(DisruptorJolt, "Disruptor Jolt", CardTier.I,
                    10, 3, 3, 3, PrintedValueChannel.None,
                    TrinketsActiveEffectContent.JoltV1, false,
                    new[] { DisruptorPulse, DeployableBarrier }),
                DisruptorCard(DisruptorPulse, "Disruptor Pulse", CardTier.II,
                    10, 4, 3, 4, PrintedValueChannel.Attack,
                    TrinketsActiveEffectContent.PulseV1, false,
                    new[] { DisruptorGrid, DeployableBastion }),
                DisruptorCard(DisruptorGrid, "Disruptor Grid", CardTier.III,
                    11, 4, 3, 4, PrintedValueChannel.Movement,
                    TrinketsActiveEffectContent.GridV1, true,
                    Array.Empty<CardDefinitionId>())
            }));

        private static CardDefinition DisruptorCard(
            CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int movement, int radius,
            PrintedValueChannel itemIcon,
            GuardsDigital.Core.Effects.EffectCapabilityReference createEffect,
            bool defeatIfUnable, IReadOnlyList<CardDefinitionId> next) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(CardColor.Blue, tier, itemIcon, next),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        TrinketsEffectContent.DisruptorSkill(id, ContentRevision, createEffect)),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null,
                CardSourceContent.Transcribed(
                    "This turn: Before any enemy hero in radius of the turret performs a primary " +
                    "action, that hero discards a card" +
                    (defeatIfUnable ? ", or is defeated; " : ", if able; ") +
                    "if they discard a card, deactivate this effect."));

        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyBarriers =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(new[]
            {
                BarrierSkillCard(DeployableBarrier, "Deployable Barrier", CardTier.II,
                    10, 4, 3, 3, 2, BarrierText,
                    new[] { DisruptorGrid, DeployableBastion }),
                BarrierSkillCard(DeployableBastion, "Deployable Bastion", CardTier.III,
                    11, 4, 3, 3, 3, BastionText,
                    Array.Empty<CardDefinitionId>())
            }));

        private static CardDefinition BarrierSkillCard(
            CardDefinitionId id, string name, CardTier tier,
            int initiative, int defense, int movement, int radius, int count,
            string text, IReadOnlyList<CardDefinitionId> next) =>
            new CardDefinition(
                id, ContentRevision, name, new InitiativeValue(initiative),
                CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Blue, tier, PrintedValueChannel.Initiative, next),
                new[]
                {
                    new ActionDefinition(
                        PrimarySkill, ActionKind.Skill,
                        ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, radius) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default,
                        TrinketsEffectContent.BarrierSkill(id, ContentRevision, count)),
                    new ActionDefinition(
                        SecondaryMovement, ActionKind.Movement,
                        ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, movement) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                    new ActionDefinition(
                        SecondaryDefense, ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null, CardSourceContent.Transcribed(text));
    }
}
