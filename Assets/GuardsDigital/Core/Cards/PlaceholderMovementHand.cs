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
            if (!ownerId.IsDefined)
            {
                throw new ArgumentException("A placeholder movement hand requires a defined owner.", nameof(ownerId));
            }

            var routeDrill = CreateDefinition("route-drill", "Route Drill");
            var obstacleCourse = CreateDefinition("obstacle-course", "Obstacle Course");
            var definitions = Array.AsReadOnly(new[] { routeDrill, obstacleCourse });
            var instances = Array.AsReadOnly(new[]
            {
                CardInstance.CreateInHand(
                    new CardInstanceId("route-drill-card"),
                    routeDrill.Reference,
                    ownerId),
                CardInstance.CreateInHand(
                    new CardInstanceId("obstacle-course-card"),
                    obstacleCourse.Reference,
                    ownerId)
            });

            return new PlaceholderMovementHand(definitions, instances);
        }

        private static CardDefinition CreateDefinition(string id, string displayName)
        {
            return new CardDefinition(
                new CardDefinitionId($"placeholder-{id}"),
                "phase-1-rev-1",
                $"TEST - {displayName}",
                new[]
                {
                    CreateMovementAction(
                        "primary-movement",
                        ActionDefinitionOrigin.PrintedPrimary,
                        3),
                    CreateMovementAction(
                        "secondary-movement",
                        ActionDefinitionOrigin.PrintedSecondary,
                        1)
                });
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
    }
}
