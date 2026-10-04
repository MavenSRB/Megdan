using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    public static partial class EmmittCardContent
    {
        /// <summary>All nineteen verified fronts in the rules shard's printed inventory order.</summary>
        public static IReadOnlyList<CardDefinitionId> InventoryOrder { get; } = Array.AsReadOnly(new[]
        {
            ReverseTime, UnstableTimeline,
            TemporalPunch, TemporalSlam, Flashback, TemporalJudgement, DejaVu,
            TimeSnare, TimeTrap, TimeLoop, TimeBomb, TimeWarp,
            TimeWalk, FastForward, TimeCapsule, BackToTheFuture, FutureProof,
            AlternativeTimelines, TearInTime
        });

        /// <summary>Every verified front, including the p.20 Handicap that setup boxes.</summary>
        public static IReadOnlyList<CardDefinition> ComposedDefinitions => Array.AsReadOnly(new[]
        {
            ReverseTimeDefinition, UnstableTimelineDefinition,
            TemporalPunchDefinition, TemporalSlamDefinition, FlashbackDefinition,
            TemporalJudgementDefinition, DejaVuDefinition,
            TimeSnareDefinition, TimeTrapDefinition,
            TimeLoopDefinition, TimeBombDefinition, TimeWarpDefinition,
            TimeWalkDefinition, FastForwardDefinition, TimeCapsuleDefinition,
            BackToTheFutureDefinition, FutureProofDefinition,
            AlternativeTimelinesDefinition, TearInTimeDefinition
        });

        public static IReadOnlyList<CardDefinitionId> NotYetComposed => Array.AsReadOnly(
            InventoryOrder.Where(id => ComposedDefinitions.All(card => !card.Id.Equals(id))).ToArray());

        public static IReadOnlyList<CardDefinition> AllDefinitions => Array.AsReadOnly(
            InventoryOrder.Select(Definition).ToArray());

        public static IReadOnlyList<CardDefinition> DealtDefinitions => Array.AsReadOnly(
            AllDefinitions.Where(card => !ReturnedToBoxAtSetup.Contains(card.Id)).ToArray());

        public static IReadOnlyList<CardDefinitionId> StartingHand { get; } = Array.AsReadOnly(new[]
        {
            ReverseTime, UnstableTimeline, TemporalPunch, TimeSnare, TimeWalk
        });

        public static IReadOnlyList<CardDefinitionId> ReturnedToBoxAtSetup { get; } =
            Array.AsReadOnly(new[] { TearInTime });

        public static IReadOnlyList<CardDefinitionId> Deck => Array.AsReadOnly(
            InventoryOrder.Where(id => !StartingHand.Contains(id) &&
                !ReturnedToBoxAtSetup.Contains(id)).ToArray());

        public static CardDefinition Definition(CardDefinitionId id) =>
            ComposedDefinitions.SingleOrDefault(card => card.Id.Equals(id)) ??
            throw new ArgumentException($"Emmitt prints no card '{id}'.", nameof(id));

        public static IReadOnlyList<CardInstance> CreateDealtCards(HeroId ownerId, string prefix)
        {
            if (!ownerId.IsDefined)
                throw new ArgumentException("Emmitt's cards require an owner.", nameof(ownerId));
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
