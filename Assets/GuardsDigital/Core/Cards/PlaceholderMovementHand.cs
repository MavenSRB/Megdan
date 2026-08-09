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

            var routeDrill = CreateDefinition("route-drill", "Route Drill", true, 2);
            var obstacleCourse = CreateDefinition("obstacle-course", "Obstacle Course", false, 3);
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
            bool includesAttack,
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

            if (includesAttack)
            {
                actions.Add(CreateAttackAction(3));
            }

            actions.Add(CreateDefenseAction(defenseValue));

            return new CardDefinition(
                new CardDefinitionId($"placeholder-{id}"),
                "phase-2-rev-1",
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

        private static ActionDefinition CreateAttackAction(int value)
        {
            return new ActionDefinition(
                new ActionDefinitionId("primary-attack"),
                ActionKind.Attack,
                ActionDefinitionOrigin.PrintedPrimary,
                new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, value) },
                new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                AttackRangeKind.NonRanged,
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
