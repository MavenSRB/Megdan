using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 7.2c: Red Needle's Place branch is bounded by the Radius icon the card prints, not by
    /// a number written into its effect.
    ///
    /// The definition-level half of that claim lives with the card's own fixture, beside everything
    /// else milestone 6.5a pinned. What lives here is the half a definition cannot show: that the bound
    /// is a composed total, so rulebook p.16's matching item widens it, and that the capability
    /// publishing it refuses a card printing no icon rather than publishing a zero.
    /// </summary>
    public sealed class PrintedRadiusPlaceBoundTests
    {
        private static readonly HeroId RedHeroId =
            new HeroId(PlaceholderOneVsOneParticipants.RedHeroIdValue);

        private static readonly HeroId BlueHeroId =
            new HeroId(PlaceholderOneVsOneParticipants.BlueHeroIdValue);

        private static readonly ActionDefinitionId NeedleSkill =
            new ActionDefinitionId("primary-push-place-skill");

        private static readonly EffectNodeId PlaceBranch = new EffectNodeId("needle-place-branch");

        private const int PrintedRadius = 4;

        [Test]
        public void ThePlaceBranch_OffersThePrintedRadiusWithNoItem()
        {
            var fixture = new NeedleFixture();

            var offered = fixture.PlaceDestinations();

            Assert.That(offered, Is.Not.Empty);
            Assert.That(
                offered.Max(space => HexDistanceQuery.Calculate(fixture.RedAt, space)),
                Is.EqualTo(PrintedRadius),
                "The printed icon is the bound, and p.9 reaches targets up to its value.");
            Assert.That(offered, Has.No.Member(fixture.RedAt), "p.18 requires a different space.");
        }

        [Test]
        public void ARadiusItem_WidensThePlaceBranchByExactlyOneRing()
        {
            var fixture = new NeedleFixture();
            fixture.PlaceRadiusItem(RedHeroId);

            var offered = fixture.PlaceDestinations();

            Assert.That(
                offered.Max(space => HexDistanceQuery.Calculate(fixture.RedAt, space)),
                Is.EqualTo(PrintedRadius + 1),
                "p.16 raises the printed icon by one, and the Place reads that composed total.");
            Assert.That(
                offered.Any(space =>
                    HexDistanceQuery.Calculate(fixture.RedAt, space) == PrintedRadius + 1),
                Is.True);

            // One ring and no more: the destinations inside the printed disk are the same ones, so the
            // item added a ring rather than changing the shape.
            var withoutItem = new NeedleFixture().PlaceDestinations();
            Assert.That(
                offered.Where(space =>
                    HexDistanceQuery.Calculate(fixture.RedAt, space) <= PrintedRadius),
                Is.EquivalentTo(withoutItem));
        }

        [Test]
        public void ANonMatchingItem_WidensNothing()
        {
            var fixture = new NeedleFixture();
            fixture.PlaceItem(RedHeroId, PrintedValueChannel.Movement);

            Assert.That(
                fixture.PlaceDestinations().Max(space =>
                    HexDistanceQuery.Calculate(fixture.RedAt, space)),
                Is.EqualTo(PrintedRadius),
                "An item of another icon leaves this card's printed Radius exactly as printed.");
        }

        [Test]
        public void ThePublishedRadius_RefusesACardThatPrintsNoIcon()
        {
            var configuration = PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture();
            var handler = SyntheticEffectCapabilities.CreateCatalog()
                .Load(SyntheticValueCapabilities.CardRadiusV1);

            var needle = configuration.CardInstances.Single(instance =>
                instance.DefinitionReference.DefinitionId.Value == "placeholder-red-needle");
            Assert.That(
                handler.Anticipate(Context(configuration, needle, NeedleSkill)),
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "The card prints a Radius, so its value is publishable.");

            // A card that prints no Radius icon has no Radius, and p.16 does not invent one. Publishing
            // zero would silently bound a consumer to the actor's own space, which is a different
            // answer rather than an absent one.
            var bombard = configuration.CardInstances.Single(instance =>
                instance.DefinitionReference.DefinitionId.Value == "placeholder-red-bombard");
            Assert.That(
                handler.Anticipate(Context(
                    configuration, bombard, new ActionDefinitionId("primary-direct-move-spawn-skill"))),
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        // ----- Helpers -----

        private static EffectCapabilityContext Context(
            PlaceholderOneVsOneTestConfiguration configuration,
            CardInstance card,
            ActionDefinitionId action) =>
            new EffectCapabilityContext(
                new EffectNodeId("radius-probe"),
                EffectFailurePolicy.Mandatory,
                card.OwnerId,
                card.OwnerId,
                card.Id,
                card.DefinitionReference,
                action,
                new Dictionary<string, EffectValue>(),
                1,
                1,
                configuration.Participants,
                null,
                RuleModificationSet.Empty,
                EffectCardState.Of(configuration.CardDefinitions, configuration.CardInstances));

        /// <summary>
        /// The configured Red hand on a hand-built board, with the round that owns the items and the
        /// dispatcher bound to it, driven as far as Red Needle's Place branch.
        ///
        /// The card's own fixture drives the same branch through a real match session on official-map
        /// geography; this one exists because items have no public command path until milestone 7.5, so
        /// the only way to place one is through the round's own state.
        /// </summary>
        private sealed class NeedleFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;
            private readonly List<CardInstance> instances;
            private readonly List<CardDefinition> definitions;
            private readonly CardActionResolutionAuthority cardActions;
            private readonly RoundResolutionAuthority round;

            public NeedleFixture()
            {
                configuration = PlaceholderOneVsOneTestConfiguration.CreateForInternalTest(TeamSide.Red);
                definitions = configuration.CardDefinitions.ToList();
                instances = configuration.CardInstances.ToList();
                var board = new AdjacentMoveAuthority(
                    Coordinates(8),
                    new[]
                    {
                        new HeroOccupancy(RedHeroId, RedAt),
                        // Far enough away that the defender never occupies a destination either disk
                        // would otherwise offer, so the two rings are compared like for like.
                        new HeroOccupancy(BlueHeroId, new HexCoordinate(8, 0))
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    Array.Empty<QuickGameMinionSetupState>());
                cardActions = new CardActionResolutionAuthority(MatchContentCatalogs.ForThisBuild(), board);
                round = new RoundResolutionAuthority(
                    definitions,
                    instances,
                    configuration.Participants,
                    configuration.TieBreakerState,
                    1,
                    RoundResolutionComposition.ForCardActionsOnly(cardActions));
            }

            public HexCoordinate RedAt { get; } = new HexCoordinate(0, 0);

            /// <summary>A Tier III fixture card printing p.16's Radius Item icon.</summary>
            private static CardDefinition RadiusItemDefinition { get; } = new CardDefinition(
                new CardDefinitionId("fixture-needle-radius-item"),
                "phase-7-2c-fixture-rev-1",
                "TEST - Radius Item",
                new InitiativeValue(1),
                CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(
                    CardColor.Red, CardTier.III, PrintedValueChannel.Radius));

            public void PlaceRadiusItem(HeroId owner) => Place(RadiusItemDefinition, owner);

            public void PlaceItem(HeroId owner, PrintedValueChannel icon) =>
                Place(
                    definitions.First(definition =>
                        definition.Progression.ItemIcon == icon &&
                        definition.Id.Value.Contains("-red-")),
                    owner);

            /// <summary>
            /// Commits Red Needle, starts its Skill, chooses the Place branch, and returns the spaces
            /// the branch offers.
            /// </summary>
            public IReadOnlyList<HexCoordinate> PlaceDestinations()
            {
                var needle = instances.Single(instance =>
                    instance.OwnerId.Equals(RedHeroId) &&
                    instance.DefinitionReference.DefinitionId.Value == "placeholder-red-needle");
                var blueCard = instances.First(instance =>
                    instance.OwnerId.Equals(BlueHeroId) && instance.State.Location == CardLocation.Hand);
                Assert.That(round.TryCommitCard(RedHeroId, needle.Id).Accepted, Is.True);
                Assert.That(round.TryCommitCard(BlueHeroId, blueCard.Id).Accepted, Is.True);
                Assert.That(
                    round.CurrentTurnAuthority.SelectedActor.CardInstanceId, Is.EqualTo(needle.Id),
                    "Red Needle's Initiative 13 is the highest dealt, so its hero acts first.");

                var offer = ActionOfferQuery
                    .GetAvailableTurnActionOffers(definitions, instances, RedHeroId, needle.Id)
                    .Single(candidate => candidate.ActionDefinitionId.Equals(NeedleSkill));
                Assert.That(
                    round.TryStartSkill(new ActionResolutionId("needle-radius"), offer).Accepted, Is.True);

                var mode = cardActions.PendingEffectChoice;
                Assert.That(mode, Is.Not.Null);
                var branch = mode.SelectInvocations.Single(candidate =>
                    candidate.Candidate.OptionNodeId.Equals(PlaceBranch));
                Assert.That(round.TryAnswerEffectChoice(branch).Accepted, Is.True);
                var confirm = cardActions.PendingEffectChoice?.ConfirmInvocation;
                if (confirm != null && cardActions.PendingEffectChoice.NodeId.Equals(mode.NodeId))
                {
                    Assert.That(round.TryAnswerEffectChoice(confirm).Accepted, Is.True);
                }

                var destinations = cardActions.PendingEffectChoice;
                Assert.That(destinations, Is.Not.Null);
                return destinations.Candidates
                    .Select(candidate => candidate.Value.Coordinate)
                    .ToArray();
            }

            private void Place(CardDefinition definition, HeroId owner)
            {
                if (!definitions.Contains(definition))
                {
                    definitions.Add(definition);
                }

                var card = new CardInstance(
                    new CardInstanceId($"{definition.Id.Value}-set-aside"),
                    definition.Reference,
                    owner,
                    new CardStateFacts(
                        CardLocation.SetAside,
                        false,
                        CardResolutionStatus.None,
                        CardFaceOrientation.FaceDown));
                instances.Add(card);
                var placed = round.HeroItems.TryPlaceSetAsideCard(card, definitions);
                Assert.That(placed.IsPlaced, Is.True, placed.RejectionReason.ToString());
            }

            private static IReadOnlyList<HexCoordinate> Coordinates(int radius)
            {
                var coordinates = new List<HexCoordinate>();
                for (var q = -radius; q <= radius; q++)
                {
                    for (var r = -radius; r <= radius; r++)
                    {
                        if (Math.Abs(q + r) <= radius)
                        {
                            coordinates.Add(new HexCoordinate(q, r));
                        }
                    }
                }

                return coordinates;
            }
        }
    }
}
