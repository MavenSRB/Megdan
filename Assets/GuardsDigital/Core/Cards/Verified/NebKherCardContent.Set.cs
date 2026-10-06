using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class NebKherCardContent
    {
        public static IReadOnlyList<CardDefinitionId> InventoryOrder { get; } =
            Array.AsReadOnly(new[]
            {
                MindGrip, DiabolicalLaughter,
                PhantasmalSentry, PhantasmalWarrior, TwistFate,
                PhantasmalChampion, DeviousScheme,
                ImbueDoubt, TimeToReconsider, CrackInReality,
                AnIllusionOfChoice, ShiftReality,
                FleetingImage, MultipleProjections, IllusionaryForce,
                MasterOfIllusions, IllusionaryArmy,
                WhatTheHellAreYou, MindProbe
            });

        public static IReadOnlyList<CardDefinition> ComposedDefinitions =>
            Array.AsReadOnly(new[]
            {
                MindGripDefinition, DiabolicalLaughterDefinition,
                PhantasmalSentryDefinition, PhantasmalWarriorDefinition,
                TwistFateDefinition, PhantasmalChampionDefinition,
                DeviousSchemeDefinition, ImbueDoubtDefinition,
                TimeToReconsiderDefinition, CrackInRealityDefinition,
                AnIllusionOfChoiceDefinition, ShiftRealityDefinition,
                FleetingImageDefinition, MultipleProjectionsDefinition,
                IllusionaryForceDefinition, MasterOfIllusionsDefinition,
                IllusionaryArmyDefinition, NebKherUltimateContent.Definition,
                MindProbeDefinition
            });

        public static IReadOnlyList<CardDefinitionId> NotYetComposed => Array.AsReadOnly(
            InventoryOrder.Where(id => ComposedDefinitions.All(card => !card.Id.Equals(id))).ToArray());

        public static IReadOnlyList<CardDefinition> AllDefinitions => Array.AsReadOnly(
            InventoryOrder.Select(Definition).ToArray());

        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } =
            Array.AsReadOnly(new[]
            {
                MindGrip, DiabolicalLaughter, PhantasmalSentry,
                ImbueDoubt, FleetingImage
            });

        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { MindProbe });

        public static IReadOnlyList<CardDefinition> DealtDefinitions => Array.AsReadOnly(
            AllDefinitions.Where(card => !ReturnedToBoxAtSetup.Contains(card.Id)).ToArray());

        public static IReadOnlyList<CardDefinitionId> Deck => Array.AsReadOnly(
            InventoryOrder.Where(id => !StartingHand.Contains(id) &&
                !ReturnedToBoxAtSetup.Contains(id)).ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            ComposedDefinitions.SingleOrDefault(card => card.Id.Equals(id)) ??
            throw new ArgumentException($"NebKher prints no card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string prefix)
        {
            if (!ownerId.IsDefined)
                throw new ArgumentException("NebKher's cards require an owner.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Card instance IDs require a per-seat prefix.", nameof(prefix));
            return Array.AsReadOnly(DealtDefinitions.Select(definition =>
            {
                var id = new CardInstanceId($"{prefix}-{definition.Id.Value}");
                return StartingHand.Contains(definition.Id)
                    ? CardInstance.CreateInHand(id, definition.Reference, ownerId)
                    : CardInstance.CreateInDeck(id, definition.Reference, ownerId);
            }).ToArray());
        }

        public static ConfiguredHand CreateHand(HeroId ownerId, string prefix)
        {
            var cards = CreateDealtCards(ownerId, prefix);
            return new ConfiguredHand(DealtDefinitions,
                cards.Where(card => StartingHand.Contains(card.DefinitionReference.DefinitionId)),
                cards.Where(card => !StartingHand.Contains(card.DefinitionReference.DefinitionId)));
        }
    }
}
