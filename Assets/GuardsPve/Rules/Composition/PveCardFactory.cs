using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Pve.Content;

namespace GuardsDigital.Pve.Composition
{
    /// <summary>
    /// PVE-D07/D09. Enemy cards as ordinary card definitions, so every rule and hero card reads them
    /// as it reads any hero's: an orange card is one printed Attack (primary) and one printed
    /// Movement (secondary), an idle card prints no action, and a purple card prints a Defense.
    /// </summary>
    /// <remarks>
    /// The content is placeholder test data (PVE-D15), so it carries the synthetic-fixture authority
    /// and no source face. Neither action may be replaced by Fast Travel or Clear: the enemy's
    /// options name only moving and attacking.
    /// </remarks>
    public static class PveCardFactory
    {
        public const string ContentRevision = "pve-content-1";
        public static readonly ActionDefinitionId AttackActionId = new ActionDefinitionId("pve-attack");
        public static readonly ActionDefinitionId MovementActionId = new ActionDefinitionId("pve-movement");
        public static readonly ActionDefinitionId DefenseActionId = new ActionDefinitionId("pve-defense");

        public static CardDefinitionId OrangeId(PveEnemyType type, PveOrangeCard card) =>
            new CardDefinitionId("pve-" + type.Id + "-" + card.Key.ToLowerInvariant());

        public static CardDefinitionId PurpleId(PveEnemyType type, PvePurpleCard card) =>
            new CardDefinitionId("pve-" + type.Id + "-" + card.Key.ToLowerInvariant());

        public static IReadOnlyList<CardDefinition> CreateDefinitions(PveEnemyType type) =>
            type.OrangeCards.Select(card => CreateOrange(type, card))
                .Concat(type.PurpleCards.Select(card => CreatePurple(type, card)))
                .ToArray();

        /// <summary>
        /// Every card one enemy of the type holds, all in hand: its four orange cards (copies
        /// included) and its purple cards.
        /// </summary>
        public static ConfiguredHand CreateHand(PveEnemyType type, HeroId owner, string instancePrefix)
        {
            var definitions = CreateDefinitions(type);
            var instances = new List<CardInstance>();
            foreach (var card in type.OrangeCards)
            {
                for (var copy = 1; copy <= card.Copies; copy++)
                {
                    instances.Add(CardInstance.CreateInHand(
                        new CardInstanceId(instancePrefix + "-" + card.Key.ToLowerInvariant() + "-" +
                            copy.ToString(CultureInfo.InvariantCulture)),
                        new CardDefinitionReference(OrangeId(type, card), ContentRevision),
                        owner));
                }
            }

            foreach (var card in type.PurpleCards)
            {
                instances.Add(CardInstance.CreateInHand(
                    new CardInstanceId(instancePrefix + "-" + card.Key.ToLowerInvariant()),
                    new CardDefinitionReference(PurpleId(type, card), ContentRevision),
                    owner));
            }

            return new ConfiguredHand(definitions, instances, new CardInstance[0]);
        }

        private static CardDefinition CreateOrange(PveEnemyType type, PveOrangeCard card)
        {
            var actions = new List<ActionDefinition>();
            if (card.AttackValue.HasValue)
            {
                var values = new List<PrintedValueDefinition>
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, card.AttackValue.Value)
                };
                if (card.AttackRange > 0)
                {
                    values.Add(new PrintedValueDefinition(PrintedValueChannel.Range, card.AttackRange));
                }

                actions.Add(new ActionDefinition(
                    AttackActionId,
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    values,
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, false),
                    card.AttackRange > 0 ? AttackRangeKind.Ranged : AttackRangeKind.NonRanged,
                    default));
            }

            if (card.MoveValue.HasValue)
            {
                actions.Add(new ActionDefinition(
                    MovementActionId,
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, card.MoveValue.Value) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false)));
            }

            return new CardDefinition(
                OrangeId(type, card),
                ContentRevision,
                card.Name,
                new InitiativeValue(card.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                actions,
                null);
        }

        private static CardDefinition CreatePurple(PveEnemyType type, PvePurpleCard card) =>
            new CardDefinition(
                PurpleId(type, card),
                ContentRevision,
                card.Name,
                new InitiativeValue(card.Initiative),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        DefenseActionId,
                        ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, card.Defense) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                },
                null);
    }
}
