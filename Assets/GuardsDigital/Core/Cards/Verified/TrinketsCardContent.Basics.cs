using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Rules.CardResolution;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class TrinketsCardContent
    {
        public static CardDefinitionId RapidRedeployment { get; } =
            new CardDefinitionId("trinkets-rapid-redeployment");
        public static CardDefinitionId SalvageParts { get; } =
            new CardDefinitionId("trinkets-salvage-parts");
        public static CardDefinitionId UnlimitedFirepower { get; } =
            new CardDefinitionId("trinkets-unlimited-firepower");
        public static CardDefinitionId RamshackleTurret { get; } =
            new CardDefinitionId("trinkets-ramshackle-turret");

        public static IReadOnlyList<CardDefinitionId> InventoryOrder => LazyInventoryOrder.Value;
        public static IReadOnlyList<CardDefinitionId> StartingHand => LazyStartingHand.Value;
        public static IReadOnlyList<CardDefinitionId> Deck => LazyDeck.Value;
        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup => LazyBoxed.Value;
        public static IReadOnlyList<CardDefinition> AllDefinitions => LazyAll.Value;
        public static IReadOnlyList<CardDefinition> DealtDefinitions => LazyDealt.Value;

        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyInventoryOrder =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                RapidRedeployment, SalvageParts,
                MakeshiftMinigun, GatlingGun, SteamDischarge, SuperchargedCannon, FlameBelcher,
                DisruptorJolt, DisruptorPulse, DeployableBarrier, DisruptorGrid,
                DeployableBastion, EarlyPrototype, UpdatedDesign, SelfDestruct,
                PerfectedDesign, EmergencyProtocol, UnlimitedFirepower, RamshackleTurret
            }));
        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyStartingHand =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                RapidRedeployment, SalvageParts, MakeshiftMinigun, DisruptorJolt, EarlyPrototype
            }));
        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyDeck =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(
                InventoryOrder.Where(id => !StartingHand.Contains(id) &&
                    !ReturnedToBoxAtSetup.Contains(id)).ToArray()));
        private static readonly Lazy<IReadOnlyList<CardDefinitionId>> LazyBoxed =
            new Lazy<IReadOnlyList<CardDefinitionId>>(() => Array.AsReadOnly(new[]
            {
                RamshackleTurret
            }));
        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyAll =
            new Lazy<IReadOnlyList<CardDefinition>>(() =>
            {
                var composed = new[]
                {
                    BuildRapidRedeployment(), BuildSalvageParts()
                }.Concat(RedDefinitions).Concat(DisruptorDefinitions)
                    .Concat(BarrierDefinitions).Concat(GreenDefinitions)
                    .Concat(new[] { BuildUnlimitedFirepower(), BuildRamshackleTurret() })
                    .ToArray();
                var duplicates = composed.GroupBy(card => card.Id)
                    .Where(group => group.Count() != 1).Select(group => group.Key).ToArray();
                var missing = InventoryOrder.Where(id => !composed.Any(card => card.Id.Equals(id)))
                    .ToArray();
                var extra = composed.Where(card => !InventoryOrder.Contains(card.Id))
                    .Select(card => card.Id).ToArray();
                if (duplicates.Length != 0 || missing.Length != 0 || extra.Length != 0)
                    throw new InvalidOperationException("Trinkets' nineteen-face inventory differs " +
                        "from composition.");
                return Array.AsReadOnly(InventoryOrder.Select(id =>
                    composed.Single(card => card.Id.Equals(id))).ToArray());
            });
        private static readonly Lazy<IReadOnlyList<CardDefinition>> LazyDealt =
            new Lazy<IReadOnlyList<CardDefinition>>(() => Array.AsReadOnly(AllDefinitions
                .Where(card => !ReturnedToBoxAtSetup.Contains(card.Id)).ToArray()));

        public static CardDefinition Definition(CardDefinitionId id) =>
            AllDefinitions.Single(card => card.Id.Equals(id));

        private static CardDefinition BuildRapidRedeployment() =>
            Basic(RapidRedeployment, "Rapid Redeployment", CardColor.Gold, 12, 1, 4,
                "Choose one —\n• Move up to 3 spaces and place the Turret into a space " +
                "adjacent to you; it counts as an obstacle.\n" +
                "• Defeat a minion adjacent to you.",
                TrinketsEffectContent.RapidRedeployment(
                    RapidRedeployment, ContentRevision));

        private static CardDefinition BuildSalvageParts() =>
            Basic(SalvageParts, "Salvage Parts", CardColor.Silver, 5, 2, null,
                "Choose one —\n• Place the Turret into a space adjacent to you; it counts " +
                "as an obstacle.\n• Remove the Turret; move up to 3 spaces.\n" +
                "• Remove the Turret; you may retrieve a discarded card.",
                TrinketsEffectContent.SalvageParts(SalvageParts, ContentRevision));

        private static CardDefinition Basic(CardDefinitionId id, string name, CardColor color,
            int initiative, int defense, int? movement, string text,
            GuardsDigital.Core.Effects.EffectDefinition effect)
        {
            var actions = new List<ActionDefinition>
            {
                new ActionDefinition(PrimarySkill, ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default, effect)
            };
            if (movement.HasValue)
                actions.Add(new ActionDefinition(SecondaryMovement, ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(
                        PrintedValueChannel.Movement, movement.Value) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                        false, true)));
            actions.Add(new ActionDefinition(SecondaryDefense, ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, defense) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                    false, false), AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile)));
            return new CardDefinition(id, ContentRevision, name,
                new InitiativeValue(initiative), CardContentAuthority.VerifiedOfficial,
                CardProgressionProfile.BasicCard(color), actions, null,
                CardSourceContent.Transcribed(text));
        }

        private static CardDefinition BuildUnlimitedFirepower() => new CardDefinition(
            UnlimitedFirepower, ContentRevision, "Unlimited Firepower", default,
            CardContentAuthority.VerifiedOfficial, CardProgressionProfile.UltimateCard(),
            Array.Empty<ActionDefinition>(), UltimateRuleDefinition.PermanentAbility(
                TrinketsEffectContent.UnlimitedFirepowerRuleId,
                TrinketsEffectContent.UnlimitedFirepowerRuleKind,
                TrinketsEffectContent.UnlimitedFirepowerModifications),
            CardSourceContent.Transcribed("Gain +1 Range and +1 Radius."));

        private static CardDefinition BuildRamshackleTurret() => new CardDefinition(
            RamshackleTurret, ContentRevision, "Ramshackle Turret", new InitiativeValue(12),
            CardContentAuthority.VerifiedOfficial,
            CardProgressionProfile.BasicCard(CardColor.Gold),
            new[]
            {
                new ActionDefinition(PrimarySkill, ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(),
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default, null,
                    PrintedTextComposition.DeferredUntil(
                        "first odd-player-count configuration")),
                new ActionDefinition(SecondaryMovement, ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 4) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                        false, true)),
                new ActionDefinition(SecondaryDefense, ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed,
                        false, false), AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            }, null, CardSourceContent.Transcribed(
                "Move up to 3 spaces and place the Turret into a space adjacent to you; " +
                "it counts as an obstacle."));
    }
}
