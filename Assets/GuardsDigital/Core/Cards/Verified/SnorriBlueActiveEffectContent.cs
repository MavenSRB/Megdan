using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Snorri's three Passage faces create only the independent This Turn clauses
    /// whose glyphs are active. They use the ordinary rule-modification authority.
    /// </summary>
    public static class SnorriBlueActiveEffectContent
    {
        private static readonly ActiveEffectCancellationCondition[] Cancellations =
        {
            ActiveEffectCancellationCondition.ControllerHeroDefeated,
            ActiveEffectCancellationCondition.CardsRetrieved
        };

        public static EffectCapabilityReference ObstaclePermissionFor(CardDefinitionId card) =>
            Reference(card, "obstacle-permission");

        public static EffectCapabilityReference EnemyImmunityFor(CardDefinitionId card) =>
            Reference(card, "enemy-immunity");

        public static EffectCapabilityReference MovementRaiseFor(CardDefinitionId card) =>
            Reference(card, "movement-raise");

        public static ActiveEffectDefinition ObstaclePermissionDefinitionFor(
            CardDefinitionId card) =>
            Definition(card, "obstacle-permission",
                RuleModification.ForPermission(
                    RulePermissionKind.MoveThroughObstacles, RuleActivity.Movement));

        public static ActiveEffectDefinition EnemyImmunityDefinitionFor(
            CardDefinitionId card) =>
            Definition(card, "enemy-immunity",
                RuleModification.ForImmunity(
                    new[] { RuleActivity.All },
                    RestrictedActorScope.Of(
                        RestrictedActorRelation.HeroesOpposingSource)));

        public static ActiveEffectDefinition MovementRaiseDefinitionFor(
            CardDefinitionId card) =>
            Definition(card, "movement-raise",
                RuleModification.ForValue(PrintedValueChannel.Movement, 2));

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            PassageCards().Select(card =>
                (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                    ObstaclePermissionFor(card),
                    ObstaclePermissionDefinitionFor(card)))
            .Concat(PassageCards().Where(card =>
                    !card.Equals(SnorriBlueMovementFaces.SafePassage))
                .Select(card =>
                    (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                        EnemyImmunityFor(card),
                        EnemyImmunityDefinitionFor(card))))
            .Concat(new[]
            {
                (IEffectCapabilityHandler)new ActiveEffectCreationHandler(
                    MovementRaiseFor(SnorriBlueMovementFaces.DeepPassage),
                    MovementRaiseDefinitionFor(SnorriBlueMovementFaces.DeepPassage))
            });

        public static IEnumerable<ActiveEffectDefinition> Definitions() =>
            PassageCards().Select(ObstaclePermissionDefinitionFor)
                .Concat(PassageCards().Where(card =>
                        !card.Equals(SnorriBlueMovementFaces.SafePassage))
                    .Select(EnemyImmunityDefinitionFor))
                .Concat(new[]
                {
                    MovementRaiseDefinitionFor(SnorriBlueMovementFaces.DeepPassage)
                });

        private static IEnumerable<CardDefinitionId> PassageCards()
        {
            yield return SnorriBlueMovementFaces.SafePassage;
            yield return SnorriBlueMovementFaces.HiddenPassage;
            yield return SnorriBlueMovementFaces.DeepPassage;
        }

        private static EffectCapabilityReference Reference(
            CardDefinitionId card, string clause) =>
            new EffectCapabilityReference(
                new EffectCapabilityId(card.Value + "-" + clause), 1);

        private static ActiveEffectDefinition Definition(
            CardDefinitionId card, string clause, RuleModification modification)
        {
            if (!PassageCards().Contains(card))
                throw new ArgumentException(
                    "Only a Snorri Passage face creates this effect.", nameof(card));
            return new ActiveEffectDefinition(
                card.Value + "-" + clause + "-effect",
                ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisTurn,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.ActionStarted,
                TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
                Cancellations,
                false,
                "SnorriPassage",
                card.Value + "-" + clause,
                Array.Empty<TimingCheckpoint>(),
                new[] { modification });
        }
    }
}
