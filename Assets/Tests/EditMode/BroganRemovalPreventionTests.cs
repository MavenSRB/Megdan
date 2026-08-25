using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.21. Brogan's Shield, Bolster and Fortify - the first configured content that can
    /// open milestone 8.19c1's removal-prevention decision at all.
    ///
    /// <para>That milestone built the rule against a synthetic shape and recorded, correctly, that no
    /// printed card could reach it. What these cases prove is the half that could not be proved then:
    /// that three real faces declare three real prices, that the disk each one draws is the number its
    /// face prints, and that the difference between "a silver card" and "a basic card" is a difference
    /// a player can see in what they are offered. Everything about how the decision is answered,
    /// restored and replayed is <see cref="RosterPausingSourceTests"/>'s and is not repeated here.</para>
    ///
    /// <para>The prices are asserted as the cards Brogan is actually holding rather than as the
    /// declaration's fields, because the declaration is already checked field by field in
    /// <see cref="BroganVerifiedContentTests"/>. What this adds is the consequence: at p.6's setup his
    /// hand holds one Silver card and two Basic ones, so Fortify offers a payment its two lower-Tier
    /// siblings do not - which is the whole of what the Tier III card buys under the 2026-08-23 ruling
    /// recorded as Arien's A-03.</para>
    /// </summary>
    public sealed class BroganRemovalPreventionTests
    {
        [Test]
        public void TheThreeGuards_OfferTheirOwnPrintedPriceToTheirControllerAlone()
        {
            var fixture = GuardFixture.Create();

            foreach (var guard in new[]
            {
                BroganActiveEffectContent.ShieldDefinition,
                BroganActiveEffectContent.BolsterDefinition,
                BroganActiveEffectContent.FortifyDefinition
            })
            {
                var offered = fixture.Offers(guard, fixture.AdjacentAllyMinionId);

                Assert.That(offered.Count, Is.EqualTo(1),
                    guard.DefinitionId + " covers friendly minions in its radius, and this one is " +
                    "standing beside Brogan.");
                Assert.That(offered[0].DecidingHeroId, Is.EqualTo(fixture.Brogan),
                    "p.10 gives the decision to the hero whose source is offering.");
                Assert.That(
                    offered[0].Cost.Kind, Is.EqualTo(RemovalPreventionCostKind.DiscardOwnedCard),
                    "All three faces charge a card out of hand; a free prevention would be p.18's " +
                    "immunity and the declaration refuses one.");
                Assert.That(
                    offered[0].PayableCards.All(card =>
                        fixture.CardInstances.Single(value => value.Id.Equals(card))
                            .OwnerId.Equals(fixture.Brogan)),
                    Is.True,
                    "A hero pays with their own cards - p.10 says so of retrieving and the same is " +
                    "true of spending.");
            }
        }

        /// <summary>
        /// The printed upgrade, read as the cards it actually offers. Shield and Bolster print "you may
        /// discard a silver card" and Fortify prints "a basic card", and p.15's Basic supertype spans
        /// Gold and Silver - so at p.6's setup the Tier III card accepts Onslaught as well as Bulwark
        /// where its two siblings accept Bulwark alone.
        /// </summary>
        [Test]
        public void FortifyAcceptsABasicCard_WhereItsTwoSiblingsAcceptOnlyTheSilverOne()
        {
            var fixture = GuardFixture.Create();

            Assert.That(
                fixture.PayableCardNames(
                    BroganActiveEffectContent.ShieldDefinition, fixture.AdjacentAllyMinionId),
                Is.EqualTo(new[] { "Bulwark" }),
                "Bulwark is the one Silver card p.6 deals him.");
            Assert.That(
                fixture.PayableCardNames(
                    BroganActiveEffectContent.BolsterDefinition, fixture.AdjacentAllyMinionId),
                Is.EqualTo(new[] { "Bulwark" }),
                "The Tier II card names the same colour as the Tier I one.");
            Assert.That(
                fixture.PayableCardNames(
                    BroganActiveEffectContent.FortifyDefinition, fixture.AdjacentAllyMinionId),
                Is.EqualTo(new[] { "Bulwark", "Onslaught" }),
                "\"a basic card\" is p.15's supertype, which spans Gold and Silver - so the Tier III " +
                "card is a genuine upgrade rather than a reworded sibling. Leap Forward is his other " +
                "Gold card and p.6 has already returned it to the box.");
        }

        /// <summary>
        /// Each face's printed Radius, measured as the minion it reaches. p.17 moves the disk with its
        /// hero, so this is asked of the board rather than of the declaration.
        /// </summary>
        [Test]
        public void ShieldReachesOneSpace_WhereBolsterAndFortifyReachTwo()
        {
            var fixture = GuardFixture.Create();

            Assert.That(
                fixture.Offers(
                    BroganActiveEffectContent.ShieldDefinition, fixture.DistantAllyMinionId),
                Is.Empty,
                "Shield prints Radius 1 and this ally is standing two spaces away.");
            Assert.That(
                fixture.Offers(
                    BroganActiveEffectContent.BolsterDefinition, fixture.DistantAllyMinionId),
                Is.Not.Empty,
                "Bolster prints Radius 2, which is what its Tier buys over Shield's disk.");
            Assert.That(
                fixture.Offers(
                    BroganActiveEffectContent.FortifyDefinition, fixture.DistantAllyMinionId),
                Is.Not.Empty,
                "Fortify prints Radius 2 as well; its upgrade over Bolster is the price, not the disk.");
        }

        /// <summary>
        /// "any friendly <b>minion</b> in radius" - the word the three faces print, and the reason
        /// milestone 8.21 added a minions-only scope rather than widening the units one.
        /// </summary>
        [Test]
        public void AFriendlyHeroInTheDisk_IsOfferedNoPrevention()
        {
            var fixture = GuardFixture.Create();

            Assert.That(
                fixture.Offers(BroganActiveEffectContent.BolsterDefinition, fixture.AllyHeroSubject),
                Is.Empty,
                "The faces say minion. A scope widened to units would offer a friendly hero a " +
                "purchase Brogan's cards do not print.");
        }

        /// <summary>
        /// An enemy minion standing in the same disk. The declaration names its side, and a source
        /// offering to keep the other team's minions on the board would be a different card.
        /// </summary>
        [Test]
        public void AnEnemyMinionInTheDisk_IsOfferedNoPrevention()
        {
            var fixture = GuardFixture.Create();

            Assert.That(
                fixture.Offers(
                    BroganActiveEffectContent.BolsterDefinition, fixture.AdjacentEnemyMinionId),
                Is.Empty,
                "\"friendly\" is the side the declaration names.");
        }

        /// <summary>
        /// Two of Brogan's own guards standing at once - Shield and Fortify - which is the competing
        /// sources case, and the reason a payment names the source whose price it pays.
        /// </summary>
        [Test]
        public void TwoOfHisOwnGuards_EachOfferTheirOwnPriceForTheSameMinion()
        {
            var fixture = GuardFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Raise(lifecycle, BroganActiveEffectContent.ShieldDefinition, "shield");
            fixture.Raise(lifecycle, BroganActiveEffectContent.FortifyDefinition, "fortify");

            var offered = RemovalPreventionQuery.Offered(
                RuleObject.ForMinion(fixture.AdjacentAllyMinionId),
                fixture.Build(lifecycle),
                fixture.CardDefinitions,
                fixture.CardInstances);

            Assert.That(offered, Has.Count.EqualTo(2),
                "Both sources cover this minion, so both are offering and the player chooses which " +
                "price to pay.");
            Assert.That(
                offered.Select(option => option.PayableCards.Count).OrderBy(count => count),
                Is.EqualTo(new[] { 1, 2 }),
                "Shield admits the Silver card alone and Fortify admits both Basic ones, so the two " +
                "offers are genuinely different purchases rather than one listed twice.");
        }

        /// <summary>
        /// A source whose hero has been defeated is cancelled by p.17, and a cancelled source offers
        /// nothing - which is what keeps a prevention from outliving the card that raised it.
        /// </summary>
        [Test]
        public void ACancelledGuard_OffersNothing()
        {
            var fixture = GuardFixture.Create();
            var lifecycle = new ActiveEffectLifecycle();
            fixture.Raise(lifecycle, BroganActiveEffectContent.BolsterDefinition, "bolster");

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AdjacentAllyMinionId),
                    fixture.Build(lifecycle),
                    fixture.CardDefinitions,
                    fixture.CardInstances),
                Is.Not.Empty,
                "The live source covers this minion.");

            lifecycle.ApplyCheckpoint(
                new ActiveEffectCheckpointContext(
                    TimingCheckpoint.ControllerHeroDefeated, 1, 1, fixture.Brogan),
                new TieBreakerState(TeamSide.Red));

            Assert.That(
                RemovalPreventionQuery.Offered(
                    RuleObject.ForMinion(fixture.AdjacentAllyMinionId),
                    fixture.Build(lifecycle),
                    fixture.CardDefinitions,
                    fixture.CardInstances),
                Is.Empty,
                "p.17 cancels an Active effect when its controller is defeated, and a cancelled " +
                "source is not offering anything to buy.");
        }

        /// <summary>
        /// The board and the three seats these cases measure. Brogan stands clear of the official
        /// setup with one friendly minion beside him, one two spaces away, and one enemy minion beside
        /// him - the four positions the three faces distinguish between.
        /// </summary>
        private sealed class GuardFixture
        {
            private readonly PlaceholderOneVsOneTestConfiguration configuration;

            private GuardFixture(
                PlaceholderOneVsOneTestConfiguration configuration,
                AdjacentMoveAuthority board,
                HeroId brogan,
                HeroId wasp)
            {
                this.configuration = configuration;
                Board = board;
                Brogan = brogan;
                AllyHeroSubject = RuleObject.ForHero(wasp);
            }

            public AdjacentMoveAuthority Board { get; }
            public MatchParticipants Participants => configuration.Participants;
            public IReadOnlyList<CardDefinition> CardDefinitions => configuration.CardDefinitions;
            public IReadOnlyList<CardInstance> CardInstances => configuration.CardInstances;
            public HeroId Brogan { get; }
            public MinionId AdjacentAllyMinionId { get; private set; }
            public MinionId DistantAllyMinionId { get; private set; }
            public MinionId AdjacentEnemyMinionId { get; private set; }

            /// <summary>
            /// A hero standing where a guarded minion would be. It is the Red hero moved into the
            /// disk rather than a friendly one, because the point is the <em>kind</em> of piece: a
            /// scope that admitted units would offer for either, and one that names minions offers
            /// for neither.
            /// </summary>
            public RuleObject AllyHeroSubject { get; }

            public static GuardFixture Create()
            {
                var configuration =
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganForInternalTest(
                        TeamSide.Red);
                var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
                var setup = QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne(),
                    configuration.Participants,
                    map,
                    configuration.TieBreakerState);
                var board = new AdjacentMoveAuthority(
                    map,
                    setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))
                        .ToArray(),
                    Array.Empty<ObstacleOccupancy>(),
                    setup.Minions);

                var brogan = configuration.Participants.Heroes
                    .Single(hero =>
                        configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Blue)
                    .Id;
                var wasp = configuration.Participants.Heroes
                    .Single(hero =>
                        configuration.Participants.GetTeamForHero(hero.Id).Side == TeamSide.Red)
                    .Id;

                // A space with nothing of the official setup within three, so the disks below measure
                // only the pieces this fixture puts there.
                var clear = map.PlayableCoordinates
                    .Where(board.IsEmptyPlayableSpace)
                    .FirstOrDefault(candidate =>
                        candidate.GetNeighbors().Count(board.IsEmptyPlayableSpace) >= 3 &&
                        !AnyUnitWithin(board, candidate, 3));
                Assert.That(clear, Is.Not.Null,
                    "The fixture needs a space clear of the official setup, or these disks would be " +
                    "measuring somebody else's minions.");

                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(brogan), clear).Accepted, Is.True);

                var fixture = new GuardFixture(configuration, board, brogan, wasp);
                fixture.AdjacentAllyMinionId = fixture.Place(TeamSide.Blue, clear, 1);
                fixture.DistantAllyMinionId = fixture.Place(TeamSide.Blue, clear, 2);
                fixture.AdjacentEnemyMinionId = fixture.Place(TeamSide.Red, clear, 1);

                // The Red hero stands in the same disk, so the minion-versus-unit case is about the
                // kind of piece rather than about where it is standing.
                Assert.That(
                    board.CommitValidatedDisplacement(
                        BoardObjectReference.ForHero(wasp),
                        clear.GetNeighbors().First(board.IsEmptyPlayableSpace)).Accepted,
                    Is.True);
                return fixture;
            }

            public IReadOnlyList<RemovalPreventionOption> Offers(
                ActiveEffectDefinition guard, MinionId subject) =>
                Offers(guard, RuleObject.ForMinion(subject));

            public IReadOnlyList<RemovalPreventionOption> Offers(
                ActiveEffectDefinition guard, RuleObject subject)
            {
                var lifecycle = new ActiveEffectLifecycle();
                Raise(lifecycle, guard, "one");
                return RemovalPreventionQuery.Offered(
                    subject, Build(lifecycle), CardDefinitions, CardInstances);
            }

            /// <summary>The display names of the cards one guard would accept, in a stable order.</summary>
            public string[] PayableCardNames(ActiveEffectDefinition guard, MinionId subject) =>
                Offers(guard, subject)
                    .Single()
                    .PayableCards
                    .Select(card => CardInstances.Single(value => value.Id.Equals(card)))
                    .Select(card => CardDefinitions
                        .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                        .DisplayName)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();

            public void Raise(
                ActiveEffectLifecycle lifecycle, ActiveEffectDefinition definition, string suffix) =>
                lifecycle.Create(
                    new ActiveEffectId($"{definition.DefinitionId}:{Brogan.Value}:{suffix}"),
                    definition,
                    Brogan,
                    Participants.GetTeamForHero(Brogan).Side,
                    default,
                    new CardInstanceId($"brogan-guard-probe-{suffix}"),
                    new ActionDefinitionId("brogan-guard-probe-action"),
                    1,
                    1);

            public RuleModificationSet Build(ActiveEffectLifecycle lifecycle) =>
                RuleModificationQuery.Build(
                    lifecycle,
                    Board,
                    Participants,
                    new TieBreakerState(TeamSide.Red),
                    UnlockedUltimateSet.None,
                    RuleObject.None,
                    null);

            /// <summary>Moves one of a side's minions to an empty space exactly this far from the anchor.</summary>
            private MinionId Place(TeamSide side, HexCoordinate anchor, int distance)
            {
                var destination = Board.MapDefinition.PlayableCoordinates
                    .Where(Board.IsEmptyPlayableSpace)
                    .FirstOrDefault(candidate =>
                        HexDistanceQuery.Calculate(candidate, anchor) == distance);
                Assert.That(destination, Is.Not.Null,
                    $"The fixture needs an empty space {distance} from Brogan.");

                var minion = Board.Minions
                    .Where(value => value.TeamSide == side &&
                        HexDistanceQuery.Calculate(value.Coordinate, anchor) > 3)
                    .OrderBy(value => value.MinionId.Value, StringComparer.Ordinal)
                    .First();
                Assert.That(
                    Board.CommitValidatedDisplacement(
                        BoardObjectReference.ForMinion(minion.MinionId), destination).Accepted,
                    Is.True);
                return minion.MinionId;
            }

            private static bool AnyUnitWithin(
                AdjacentMoveAuthority board, HexCoordinate space, int distance) =>
                board.Occupancies.Any(hero =>
                    HexDistanceQuery.Calculate(hero.Coordinate, space) <= distance) ||
                board.Minions.Any(minion =>
                    HexDistanceQuery.Calculate(minion.Coordinate, space) <= distance);
        }
    }
}
