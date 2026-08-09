using System;
using System.Collections.Generic;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    public sealed class PlaceholderMovementHand
    {
        private PlaceholderMovementHand(
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances)
        {
            Definitions = definitions;
            Instances = instances;
        }

        public IReadOnlyList<CardDefinition> Definitions { get; }

        public IReadOnlyList<CardInstance> Instances { get; }

        public static PlaceholderMovementHand Create(HeroId ownerId)
        {
            return Create(
                ownerId,
                new CardInstanceId("route-drill-card"),
                new CardInstanceId("obstacle-course-card"));
        }

        public static PlaceholderMovementHand Create(
            HeroId ownerId,
            CardInstanceId routeDrillInstanceId,
            CardInstanceId obstacleCourseInstanceId)
        {
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder movement hand requires a defined owner.", nameof(ownerId));
            }

            if (!routeDrillInstanceId.IsDefined || !obstacleCourseInstanceId.IsDefined)
            {
                throw new ArgumentException("A placeholder movement hand requires defined card instance IDs.");
            }

            if (routeDrillInstanceId.Equals(obstacleCourseInstanceId))
            {
                throw new ArgumentException("A placeholder movement hand requires unique card instance IDs.");
            }

            var routeDrill = CreateDefinition(
                "route-drill",
                "Route Drill",
                AttackRangeKind.NonRanged,
                3,
                0,
                2);
            var obstacleCourse = CreateDefinition(
                "obstacle-course",
                "Obstacle Course",
                AttackRangeKind.Ranged,
                2,
                2,
                3);
            var definitions = Array.AsReadOnly(new[] { routeDrill, obstacleCourse });
            var instances = Array.AsReadOnly(new[]
            {
                CardInstance.CreateInHand(
                    routeDrillInstanceId,
                    routeDrill.Reference,
                    ownerId),
                CardInstance.CreateInHand(
                    obstacleCourseInstanceId,
                    obstacleCourse.Reference,
                    ownerId)
            });

            return new PlaceholderMovementHand(definitions, instances);
        }

        private static CardDefinition CreateDefinition(
            string id,
            string displayName,
            AttackRangeKind attackRange,
            int attackValue,
            int rangeValue,
            int defenseValue)
        {
            var actions = new List<ActionDefinition>
            {
                CreateMovementAction(
                    "primary-movement",
                    ActionDefinitionOrigin.PrintedPrimary,
                    3),
                CreateMovementAction(
                    "secondary-movement",
                    ActionDefinitionOrigin.PrintedSecondary,
                    1)
            };

            if (attackRange != AttackRangeKind.None)
            {
                actions.Add(CreateAttackAction(attackRange, attackValue, rangeValue));
            }

            actions.Add(CreateDefenseAction(defenseValue));

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{id}"),
                "phase-2-rev-2",
                $"TEST - {displayName}",
                actions);
        }

        private static ActionDefinition CreateMovementAction(
            string id,
            ActionDefinitionOrigin origin,
            int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId(id),
                ActionKind.Movement,
                origin,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, value) },
                origin == ActionDefinitionOrigin.PrintedPrimary
                    ? new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)
                    : new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
        }

        private static ActionDefinition CreateAttackAction(
            AttackRangeKind attackRange,
            int attackValue,
            int rangeValue)
        {
            var values = attackRange == AttackRangeKind.Ranged
                ? new[]
                {
                    new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue),
                    new PrintedValueDefinition(PrintedValueChannel.Range, rangeValue)
                }
                : new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, attackValue) };
            return new ActionDefinition(
                new ActionDefinitionId(
                    attackRange == AttackRangeKind.Ranged
                        ? "primary-ranged-attack"
                        : "primary-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                values,
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                attackRange,
                default);
        }

        private static ActionDefinition CreateDefenseAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("defense-reaction"),
                ActionKind.Defense,
                ActionDefinitionOrigin.Reaction,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                AttackRangeKind.None,
                new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile));
        }
    }
}
