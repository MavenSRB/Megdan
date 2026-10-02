using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.21j: Razzle the Ringmaster dealt into both formats with his six handlers and Twin
    /// Strike's body registered, the printed four-star, two-role picker row, his four-body supply
    /// (RZ-Q1) declared beside his roster row and carried onto every board that seats him, both seat
    /// projections reading his bodies and his settled acting body over the wire, and his prepared
    /// private art.
    /// </summary>
    public sealed class RazzleDealingTests
    {
        [TestCase(2)]
        [TestCase(3)]
        public void SelectedRazzle_DealsTheEighteenPlayableFacesInEitherFormat_AndDeclaresFourBodies(int perTeam)
        {
            var razzle = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "razzle");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(30));
            Assert.That(razzle.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.RazzleHeroIdValue));
            Assert.That(razzle.DisplayName, Is.EqualTo("Razzle the Ringmaster"), "RZ-02: the dashboard's title.");
            Assert.That(razzle.Complexity, Is.EqualTo(HeroComplexity.Wizard), "RZ-01: four stars.");
            Assert.That(razzle.Roles, Is.EqualTo(HeroRole.Tactician | HeroRole.Melee), "RZ-01.");

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { razzle }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card =>
                card.OwnerId.Value == razzle.HeroIdValue).ToArray();
            Assert.That(cards.Length, Is.EqualTo(18));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RazzleCardContent.DealtDefinitions.Select(card => card.Id)));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(RazzleCardContent.StuntedDoubles),
                "Stunted Doubles is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(RazzleCardContent.StartingHand));
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(13));

            // The supply is declared beside his roster row (RZ-Q1 (a)) and reaches the board from the
            // selected match's setup; no other seat declares one.
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup), configuration.Participants, map,
                configuration.TieBreakerState);
            var initial = new MatchSessionInitialState(Scenario(configuration, map, setup.Heroes
                .Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))), setup, new[] { map });
            Assert.That(initial.Scenario.HeroBodySupplies, Is.EqualTo(new[]
            {
                new HeroBodySupply(new HeroId(QuickGameSetupConfiguration.RazzleHeroIdValue), 4)
            }));
            Assert.That(QuickGameSetupConfiguration.HeroBodiesOf(new HeroContentId("wasp")), Is.EqualTo(1));
            Assert.That(new MatchSession(initial).Projection.Heroes
                    .Count(hero => hero.HeroId.Value == razzle.HeroIdValue), Is.EqualTo(1),
                "He begins as one body on his spawn point (p.6); the other three are his supply.");
        }

        [Test]
        public void TheSetIsTheShardsNineteenRows_AllComposed_UnderHisDealtRevision()
        {
            Assert.That(RazzleCardContent.InventoryOrder.Count, Is.EqualTo(19));
            Assert.That(RazzleCardContent.NotYetComposed, Is.Empty, "9.21d-9.21h composed every face.");
            Assert.That(RazzleCardContent.AllDefinitions.Select(definition => definition.Id),
                Is.EqualTo(RazzleCardContent.InventoryOrder), "every face, in the shard's order.");
            Assert.That(RazzleCardContent.AllDefinitions.Select(definition => definition.ContentRevision),
                Is.All.EqualTo("razzle-9-21j-rev-1"));
            Assert.That(RazzleCardContent.StartingHand, Is.EqualTo(new[]
            {
                RazzleCardContent.StuntDoubles, RazzleCardContent.CrowdControl, RazzleCardContent.PhantomStrike,
                RazzleCardContent.Alleyoop, RazzleCardContent.Tightrope
            }), "p.6: Gold, Silver and each colour's Tier I.");
            Assert.That(RazzleCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { RazzleCardContent.StuntedDoubles }));
            Assert.That(RazzleCardContent.Deck.Count, Is.EqualTo(13), "nineteen less five in hand less the Handicap.");
            Assert.That(RazzleCardContent.Definition(RazzleCardContent.TwinStrike),
                Is.SameAs(RazzleCardContent.TwinStrikeDefinition));
            Assert.That(() => RazzleCardContent.Definition(new CardDefinitionId("razzle-nothing")),
                Throws.ArgumentException);

            // The upgrade lines (shard, "by name family", validated against p.15 here).
            void Upgrades(CardDefinition face, params CardDefinitionId[] next) =>
                Assert.That(face.Progression.NextTierCandidateIds, Is.EquivalentTo(next), face.DisplayName);
            Upgrades(RazzleCardContent.PhantomStrikeDefinition, RazzleCardContent.HitAndGone, RazzleCardContent.Rummage);
            Upgrades(RazzleCardContent.HitAndGoneDefinition, RazzleCardContent.IntoThinAir, RazzleCardContent.Ransack);
            Upgrades(RazzleCardContent.RummageDefinition, RazzleCardContent.IntoThinAir, RazzleCardContent.Ransack);
            Upgrades(RazzleCardContent.AlleyoopDefinition, RazzleCardContent.GroupPerformance, RazzleCardContent.MagicTrick);
            Upgrades(RazzleCardContent.GroupPerformanceDefinition, RazzleCardContent.TeamSpirit, RazzleCardContent.AaaandItsGone);
            Upgrades(RazzleCardContent.MagicTrickDefinition, RazzleCardContent.TeamSpirit, RazzleCardContent.AaaandItsGone);
            Upgrades(RazzleCardContent.TightropeDefinition, RazzleCardContent.HighWire, RazzleCardContent.Theatrics);
            Upgrades(RazzleCardContent.HighWireDefinition, RazzleCardContent.WireDancers, RazzleCardContent.Spectacle);
            Upgrades(RazzleCardContent.TheatricsDefinition, RazzleCardContent.WireDancers, RazzleCardContent.Spectacle);
        }

        [Test]
        public void TheTrustedCatalog_RegistersHisSixHandlersAndTwinStrikesBody()
        {
            var staged = MatchConfiguration.CreateStagedRazzleForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("razzle-", StringComparison.Ordinal)), Is.EqualTo(18));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            var handlers = RazzleEffectContent.Handlers().ToArray();
            Assert.That(handlers, Has.Length.EqualTo(6));
            foreach (var handler in handlers)
            {
                Assert.That(catalogs.Capabilities.Registered, Has.Member(handler.Reference),
                    handler.Reference.ToString());
            }

            var twinStrike = RazzleCardContent.TwinStrikeDefinition.UltimateRule.TriggeredEffect;
            Assert.That(twinStrike, Is.Not.Null);
            Assert.That(catalogs.TriggeredEffects.TryGet(twinStrike.Value, out var body), Is.True,
                "p.16: the permanent ability's body is in the one catalog a snapshot compares.");
            Assert.That(body.ContentRevision, Is.EqualTo(RazzleCardContent.ContentRevision));
        }

        [Test]
        public void TheTierThreeStaging_UnlocksTwinStrike()
        {
            var staged = MatchConfiguration.CreateStagedRazzleTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(RazzleCardContent.TwinStrike));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            Assert.That(staged.CardInstances.Where(card =>
                    card.OwnerId.Equals(ultimate.OwnerId) && card.State.Location == CardLocation.Hand)
                .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(new[]
            {
                RazzleCardContent.StuntDoubles, RazzleCardContent.CrowdControl, RazzleCardContent.Ransack,
                RazzleCardContent.AaaandItsGone, RazzleCardContent.Spectacle
            }));
        }

        /// <summary>8.46a: the controls his dealt faces raise are exactly the ones he declares words for.</summary>
        [Test]
        public void EveryControlHisDealtFacesRaise_HasItsWords_AndNoOtherIsDeclared()
        {
            var required = DecisionLabelRequirementQuery
                .For(RazzleCardContent.DealtDefinitions, MatchContentCatalogs.ForThisBuild())
                .Select(requirement => requirement.CardId.Value + " / " + requirement.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            var declared = RazzleCardContent.DecisionLabels()
                .Select(declaration => declaration.CardId.Value + " / " + declaration.NodeId.Value)
                .OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Assert.That(declared, Is.EqualTo(required));
            Assert.That(required, Is.EqualTo(new[]
            {
                "razzle-ransack / razzle-ransack-may-retrieve-1",
                "razzle-ransack / razzle-ransack-may-retrieve-2",
                "razzle-ransack / razzle-ransack-may-retrieve-3",
                "razzle-rummage / razzle-rummage-may-retrieve-1",
                "razzle-spectacle / razzle-spectacle-repeat",
                "razzle-twin-strike / twin-strike-repeat"
            }), "the two repeats by a different body and the three faces' retrievals.");
        }

        /// <summary>
        /// RULING 53's "If" pattern (OfferOutcomeTests.OpensWithAnIf) over his faces: Phantom Strike's
        /// and Rummage's printed gates, and the two Doubles' ". If defeated, remove all of you", which is
        /// RZ-Q3's Defeat rather than a gate on the play.
        /// </summary>
        [Test]
        public void TheIfPattern_MatchesHisTwoGates_AndTheDoublesDefeatSentence()
        {
            Assert.That(RazzleCardContent.AllDefinitions.Where(OfferOutcomeTests.OpensWithAnIf)
                .Select(definition => definition.Id), Is.EquivalentTo(new[]
            {
                RazzleCardContent.StuntDoubles, RazzleCardContent.PhantomStrike, RazzleCardContent.Rummage,
                RazzleCardContent.StuntedDoubles
            }));
        }

        /// <summary>
        /// Both seats' projections, over the wire (9.21i's owed row, protocol /28): his two bodies on the
        /// hero rows, no acting body before the pick, the pick authorized on his seat alone, and after
        /// it the settled body read identically on the host and on every network seat.
        /// </summary>
        [Test]
        public void BothSeats_ReadHisBodiesAndHisSettledActingBody_OverTheWire()
        {
            var session = TwoBodiedStagedSession(out var wasp, out var razzle);
            HeroBodiesBoard.CommitBlueFirst(session, wasp, razzle);
            var choice = session.Projection.PendingActingBodyChoice;
            Assert.That(choice, Is.Not.Null, "Two bodies stand as Stunt Doubles resolves (RZ-Q4).");

            var host = new MatchHost(session);
            var harness = PrivateOnlineWireHarness.Compose(host);
            harness.SeatAll();
            NetworkMatchClient SeatOf(HeroId hero)
            {
                var seat = host.Seats.Seats.Single(value => value.HeroId.Equals(hero));
                return new NetworkMatchClient(harness.Client(harness.OrdinalOf(seat.Id.Value)),
                    host.Projection, MatchContentCatalogs.ForThisBuild());
            }

            void AssertEverySeatAgrees(string moment)
            {
                harness.Pump();
                harness.Pump();
                foreach (var hero in new[] { wasp, razzle })
                {
                    var view = SeatOf(hero).Projection;
                    Assert.That(view.Heroes, Is.EquivalentTo(host.Projection.Heroes), moment + " " + hero);
                    Assert.That(view.ActingBodies, Is.EqualTo(host.Projection.ActingBodies), moment + " " + hero);
                }
            }

            Assert.That(host.Projection.ActingBodies, Is.Empty, "None is settled before the pick.");
            AssertEverySeatAgrees("At the pick");
            Assert.That(SeatOf(wasp).Projection.DecisionContract?.Commands ?? Array.Empty<DirectorCommand>(),
                Is.Empty, "Wasp's seat draws no control for his pick.");

            var pickBodyTwo = SeatOf(razzle).Projection.DecisionContract.Commands
                .Single(command => command.HeroBody == 2);
            Assert.That(SeatOf(razzle).Submit(pickBodyTwo).Accepted, Is.True);
            harness.Pump();
            Assert.That(host.Projection.ActingBodies, Is.EqualTo(new[] { new HeroActingBody(razzle, 2) }));
            AssertEverySeatAgrees("After the pick");
        }

        /// <summary>
        /// The bot's evaluation reads a many-bodied hero as one hero (RZ-Q2): board presence counts him
        /// once from either side, and his own proximity is measured from his nearest body.
        /// </summary>
        [Test]
        public void TheBotsEvaluation_CountsHimOnce_AndMeasuresFromHisNearestBody()
        {
            var session = TwoBodiedStagedSession(out var wasp, out var razzle);
            var projection = session.Projection;
            Assert.That(projection.Heroes.Count(hero => hero.HeroId.Equals(razzle)), Is.EqualTo(2));
            Assert.That(BotEvaluation.Explain(projection, razzle).BoardPresence, Is.EqualTo(0),
                "one hero a side, though he stands on two spaces");
            Assert.That(BotEvaluation.Explain(projection, wasp).BoardPresence, Is.EqualTo(0));

            var waspAt = projection.Heroes.Single(hero => hero.HeroId.Equals(wasp)).Coordinate;
            var nearest = projection.Heroes.Where(hero => hero.HeroId.Equals(razzle))
                .Min(hero => HexDistanceQuery.Calculate(hero.Coordinate, waspAt));
            var farthest = projection.Heroes.Where(hero => hero.HeroId.Equals(razzle))
                .Max(hero => HexDistanceQuery.Calculate(hero.Coordinate, waspAt));
            Assert.That(nearest, Is.LessThan(farthest), "Fixture precondition: his bodies differ in reach.");
            Assert.That(BotEvaluation.Explain(projection, razzle).AdjacentThreats,
                Is.GreaterThanOrEqualTo(nearest <= 1 ? 1 : 0));
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheStandeesTheSelectionFrontAndEveryDealtFace()
        {
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/razzle-red"), Is.Not.Null,
                "run Tools/Prepare-HeroTokens.ps1 -Hero razzle in this checkout");
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/razzle-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-razzle"), Is.Not.Null);
            foreach (var definition in RazzleCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif

        // -------------------------------------------------------------------------------------------
        // The staged board
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Wasp against Razzle on the engaged staged board his scenes load, with a second body of his
        /// staged two spaces from the first - and NO supply stated by the scenario: the only way body 2
        /// can stand is the four-body supply his roster declaration carries in from the setup.
        /// </summary>
        internal static MatchSession TwoBodiedStagedSession(out HeroId wasp, out HeroId razzle)
        {
            var configuration = MatchConfiguration.CreateStagedRazzleForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusRazzleOneVsOne(),
                    configuration.Participants, map, configuration.TieBreakerState),
                out _);
            var razzleSetup = setup.Heroes.Single(hero => hero.ContentId.Value == RazzleCardContent.ContentSetId);
            var waspSetup = setup.Heroes.Single(hero => !hero.HeroId.Equals(razzleSetup.HeroId));
            wasp = waspSetup.HeroId;
            razzle = razzleSetup.HeroId;

            var occupied = new HashSet<HexCoordinate>(setup.Heroes.Select(hero => hero.Coordinate)
                .Concat(setup.Minions.Select(minion => minion.Coordinate)));
            var waspAt = waspSetup.Coordinate;
            var bodyOne = razzleSetup.Coordinate;
            var bodyTwo = map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable && !occupied.Contains(space.Coordinate))
                .Select(space => space.Coordinate)
                .Where(at => HexDistanceQuery.Calculate(at, bodyOne) == 2 &&
                    HexDistanceQuery.Calculate(at, waspAt) != HexDistanceQuery.Calculate(bodyOne, waspAt))
                .OrderBy(at => at.Q).ThenBy(at => at.R)
                .First();
            var scenario = Scenario(configuration, map, new[]
            {
                new HeroOccupancy(wasp, waspAt),
                new HeroOccupancy(razzle, 1, bodyOne),
                new HeroOccupancy(razzle, 2, bodyTwo)
            });
            Assert.That(scenario.HeroBodySupplies, Is.Empty, "The scenario states no supply of its own.");
            var initial = new MatchSessionInitialState(scenario, setup, new[] { map });
            Assert.That(initial.Scenario.HeroBodySupplies, Is.EqualTo(new[] { new HeroBodySupply(razzle, 4) }),
                "The staged seat deals his content, so his roster declaration arrives by content identity.");
            return new MatchSession(initial);
        }

        private static InitialScenarioDescription Scenario(
            MatchConfiguration configuration, BoardMapDefinition map, IEnumerable<HeroOccupancy> heroes) =>
            new InitialScenarioDescription(
                configuration.Participants,
                configuration.CardDefinitions,
                configuration.CardInstances,
                configuration.TieBreakerState,
                configuration.SetupSeed,
                map.Reference,
                heroes,
                Array.Empty<ObstacleOccupancy>());
    }
}
