using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Application.Bots;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using GuardsDigital.Presentation.Board;
#if UNITY_EDITOR
using GuardsDigital.Presentation.Match;
using UnityEngine;
#endif
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32h: Takahide the Warlord dealt into both formats - twenty fronts dealt, Shameful
    /// Display boxed (p.20), Float like a Butterfly, Bushido and the three Tier I faces in his hand and
    /// the other two golds in his deck by their own text - with every handler his faces name
    /// registered, the printed four-star, five-role picker row, the Tier III staging Ready for War
    /// leaves, and the bots answering his gold swaps on his seat and a teammate's swap on the
    /// teammate's own seat, through the wire (owed by 9.32b).
    /// </summary>
    public sealed class TakahideDealingTests
    {
        private const string Revision = "takahide-9-32h-rev-1";

        [TestCase(2)]
        [TestCase(3)]
        public void SelectedTakahide_DealsTwentyFronts_WithTwoGoldsInHisDeck(int perTeam)
        {
            var takahide = RegisteredHeroRoster.ForThisBuild.Single(hero => hero.ContentId.Value == "takahide");
            Assert.That(RegisteredHeroRoster.ForThisBuild.Count, Is.EqualTo(32));
            Assert.That(takahide.HeroIdValue, Is.EqualTo(QuickGameSetupConfiguration.TakahideHeroIdValue));
            Assert.That(takahide.DisplayName, Is.EqualTo("Takahide the Warlord"));
            Assert.That(takahide.Complexity, Is.EqualTo(HeroComplexity.Wizard), "four stars");
            Assert.That(takahide.Roles, Is.EqualTo(HeroRole.Durable | HeroRole.Sniper | HeroRole.Tactician |
                HeroRole.Damager | HeroRole.Farming));

            var printed = RegisteredHeroRoster.PrintedLineup(perTeam);
            var lineup = new HeroLineup(new[] { takahide }.Concat(printed.Red.Skip(1)), printed.Blue);
            var format = perTeam == 2 ? MatchFormat.TwoVersusTwoQuick : MatchFormat.ThreeVersusThreeQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var cards = configuration.CardInstances.Where(card => card.OwnerId.Value == takahide.HeroIdValue).ToArray();

            Assert.That(cards.Select(card => card.DefinitionReference),
                Is.EquivalentTo(TakahideCardContent.DealtDefinitions.Select(card => card.Reference)));
            Assert.That(cards.Length, Is.EqualTo(20));
            Assert.That(cards.Select(card => card.DefinitionReference.DefinitionId),
                Has.No.Member(TakahideCardContent.ShamefulDisplay),
                "Shameful Display is the p.20 Handicap and returns to the box at setup.");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Is.EquivalentTo(new[]
                {
                    TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.Bushido,
                    TakahideCardContent.SetAnExample, TakahideCardContent.ProvenWarrior,
                    TakahideCardContent.ComeToAid
                }), "\"This card starts the game in your hand\" - and p.6's basics and Tier I faces");
            Assert.That(cards.Where(card => card.State.Location == CardLocation.Deck)
                    .Select(card => card.DefinitionReference.DefinitionId),
                Has.Member(TakahideCardContent.StingLikeABee).And.Member(TakahideCardContent.StrikeLikeATiger),
                "\"This card starts the game in your deck\"");
            Assert.That(cards.Count(card => card.State.Location == CardLocation.Deck), Is.EqualTo(15));

            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup), configuration.Participants, map,
                configuration.TieBreakerState);
            var host = new MatchHost(new MatchSession(new MatchSessionInitialState(
                Scenario(configuration, map, setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))),
                setup, new[] { map })));
            foreach (var seat in host.Seats.Seats)
            {
                Assert.That(host.ProjectionForSeat(seat.Id).HandSummaries
                    .Single(value => value.HeroId.Value == takahide.HeroIdValue).HandCount, Is.EqualTo(5),
                    seat.HeroId.Value);
            }
        }

        [Test]
        public void TheSetIsTheShardsTwentyOneFaces_AllComposed_UnderHisDealtRevision()
        {
            Assert.That(TakahideCardContent.InventoryOrder.Count, Is.EqualTo(21));
            Assert.That(TakahideCardContent.NotYetComposed, Is.Empty, "9.32d-9.32f and 9.32h composed every face");
            Assert.That(TakahideCardContent.AllDefinitions.Select(definition => definition.Id),
                Is.EqualTo(TakahideCardContent.InventoryOrder));
            Assert.That(TakahideCardContent.AllDefinitions.Select(definition => definition.Reference.ContentRevision),
                Is.All.EqualTo(Revision));
            Assert.That(TakahideCardContent.DealtDefinitions.Count, Is.EqualTo(20));
            Assert.That(TakahideCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { TakahideCardContent.ShamefulDisplay }));
            var shameful = TakahideCardContent.Definition(TakahideCardContent.ShamefulDisplay);
            Assert.That(shameful.Initiative.Value, Is.EqualTo(5));
            Assert.That(shameful.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(shameful.ActionDefinitions, Is.Empty,
                "boxed for every configuration: no Block condition this build declares says \"You are defeated\"");
            Assert.That(TakahideCardContent.Deck.Count, Is.EqualTo(15));
        }

        [Test]
        public void TheTrustedCatalog_RegistersEveryHandlerHisFacesName()
        {
            var staged = MatchConfiguration.CreateStagedTakahideForInternalTest(TeamSide.Red);
            Assert.That(staged.CardDefinitions.Count(card =>
                card.Id.Value.StartsWith("takahide-", StringComparison.Ordinal)), Is.EqualTo(20));

            var catalogs = MatchContentCatalogs.ForThisBuild();
            var named = TakahideCardContent.DealtDefinitions
                .SelectMany(definition => definition.ActionDefinitions)
                .SelectMany(action => new[] { action.EffectRoot, action.SecondCardTextRoot })
                .Where(root => root != null)
                .SelectMany(root => root.Nodes)
                .Where(node => node.Capability.IsDefined)
                .Select(node => node.Capability)
                .Distinct()
                .ToArray();
            Assert.That(named, Has.Member(SyntheticDeckSwapCapabilities.SwapThisCardWithDeckCardV1)
                .And.Member(SyntheticDeckSwapCapabilities.SwapOwnedColorCardWithDeckCardV1)
                .And.Member(SyntheticUnresolvedSwapCapabilities.SwapUnresolvedCardV1)
                .And.Member(SyntheticDiscardedCardChoiceCapabilities.ChooseDiscardedCardV1)
                .And.Member(TakahideCardContent.GrantSpinningBladeV1)
                .And.Member(TakahideCardContent.GrantBladeHelixV1));
            foreach (var capability in named)
            {
                Assert.That(catalogs.Capabilities.Registered, Has.Member(capability), capability.ToString());
            }
        }

        [Test]
        public void TheTierThreeStaging_LeavesHimAsReadyForWarDoes()
        {
            var staged = MatchConfiguration.CreateStagedTakahideTierThreeForInternalTest(TeamSide.Red);
            var ultimate = staged.CardInstances.Single(card =>
                card.DefinitionReference.DefinitionId.Equals(TakahideCardContent.ReadyForWar));
            Assert.That(ultimate.State.Location, Is.EqualTo(CardLocation.UltimateArea));
            var mine = staged.CardInstances.Where(card => card.OwnerId.Equals(ultimate.OwnerId)).ToArray();
            Assert.That(mine.Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId), Is.EquivalentTo(new[]
            {
                TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.StingLikeABee,
                TakahideCardContent.StrikeLikeATiger, TakahideCardContent.HoldMySake,
                TakahideCardContent.TacticalGambit, TakahideCardContent.LoyalRetainer
            }), "six cards: the three golds and a Tier III per colour (TK-Q5)");
            Assert.That(mine.Single(card => card.DefinitionReference.DefinitionId.Equals(TakahideCardContent.Bushido))
                .State.Location, Is.EqualTo(CardLocation.Deck), "\"Return your silver card to your deck\"");
        }

        /// <summary>
        /// 8.46a: his faces raise no p.10 confirm-or-decline - every "may" and "up to" sits on a
        /// selection with its own decline (KEEP MY CARD, 9.32g) - so he declares no decision labels.
        /// </summary>
        [Test]
        public void HisFacesRaiseNoConfirmOrDecline_SoHeDeclaresNoDecisionLabels()
        {
            Assert.That(DecisionLabelRequirementQuery.For(
                TakahideCardContent.DealtDefinitions, MatchContentCatalogs.ForThisBuild()), Is.Empty);
        }

        /// <summary>
        /// RULING 53's "If" pattern (OfferOutcomeTests.OpensWithAnIf) reads the played card: his
        /// Calculated Risk, Tactical Gambit and five Greens open a sentence with their printed gate "If
        /// that hero has a card in the discard", and no other face opens one - pinned so a new "If"
        /// sentence on his faces is seen rather than absorbed.
        /// </summary>
        [Test]
        public void TheIfPattern_MatchesExactlyHisSevenDiscardGates()
        {
            Assert.That(TakahideCardContent.DealtDefinitions.Where(OfferOutcomeTests.OpensWithAnIf)
                    .Select(definition => definition.Id),
                Is.EquivalentTo(new[]
                {
                    TakahideCardContent.CalculatedRisk, TakahideCardContent.TacticalGambit,
                    TakahideCardContent.ComeToAid, TakahideCardContent.BringTheRelief,
                    TakahideCardContent.PledgeOfAllegiance, TakahideCardContent.CommitReserves,
                    TakahideCardContent.LoyalRetainer
                }));
        }

        // -------------------------------------------------------------------------------------------
        // The bots answer his decisions
        // -------------------------------------------------------------------------------------------

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerTheGoldSwap_OnHisSeat(int seed)
        {
            var session = StagedSession(tierThree: false, out var wasp, out var takahide);
            ActWith(session, wasp, takahide, TakahideCardContent.FloatLikeAButterfly);
            Accept(session, Start(session, TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.PrimaryMovement));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered.Where(entry => entry.Seat.Equals(takahide)), Is.Not.Empty);
            Assert.That(session.Projection.Cards.Single(card =>
                    card.OwnerId.Equals(takahide) &&
                    card.DefinitionReference.DefinitionId.Equals(TakahideCardContent.FloatLikeAButterfly)).State.Location,
                Is.EqualTo(CardLocation.Deck), "Float like a Butterfly swapped itself for a deck gold");
        }

        [TestCase(1)]
        [TestCase(2)]
        public void TheBots_AnswerBushido_OnHisSeat(int seed)
        {
            var session = StagedSession(tierThree: false, out var wasp, out var takahide);
            ActWith(session, wasp, takahide, TakahideCardContent.Bushido);
            Accept(session, Start(session, TakahideCardContent.Bushido, TakahideCardContent.PrimarySkill));
            var answered = BotsAnswerUntilTheActionEnds(session, seed);
            Assert.That(answered.Where(entry => entry.Seat.Equals(takahide)), Is.Not.Empty);
            Assert.That(session.Projection.Cards.Count(card =>
                    card.OwnerId.Equals(takahide) && card.State.Location == CardLocation.Hand &&
                    DefinitionOf(session, card).Progression.Color == CardColor.Gold),
                Is.EqualTo(1), "the gold in hand was swapped for a deck gold, faceup into the hand");
        }

        /// <summary>
        /// Owed by 9.32b: a teammate's seat answers their own swap. The decision is on the teammate's
        /// seat, crosses the wire to it, and the answer its bot picks from the decoded seat is accepted.
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        public void ATeammatesNetworkSeat_AnswersSetAnExamplesSwap(int seed)
        {
            var session = TeammateSession(out var takahide, out var teammate);
            var host = new MatchHost(session);
            Accept(session, Start(session, TakahideCardContent.SetAnExample, TakahideCardContent.PrimaryAttack));
            var random = new DeterministicBotRandom(seed);
            for (var step = 0; step < 40; step++)
            {
                var rules = session.Projection;
                Assert.That(rules.DecisionContract, Is.Not.Null, "Set an Example ended before the teammate was asked");
                var seat = host.Seats.Seats.First(value => rules.DecisionOwner.IsOwnedBy(value));
                if (seat.HeroId.Equals(teammate) && rules.PendingEffectChoice != null)
                {
                    var view = host.ProjectionForSeat(seat.Id);
                    Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(view), out var record),
                        Is.True);
                    var network = new NetworkMatchReadModel(session.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
                    Assert.That(network.PendingEffectChoice, Is.Not.Null, "the swap reaches the teammate's seat");
                    Assert.That(network.PendingEffectChoice.ChooserId, Is.EqualTo(teammate));
                    Assert.That(network.PendingEffectChoice.DeclineInvocation, Is.Not.Null, "KEEP MY CARD");
                    var pick = BotPolicy.SelectPrivateSessionTactics(view.AuthorizedCommands, view, teammate, random);
                    Assert.That(pick, Is.Not.Null);
                    var result = session.Execute(pick);
                    Assert.That(result.Accepted, Is.True, result.Detail);
                    return;
                }

                var own = host.ProjectionForSeat(seat.Id);
                var answer = BotPolicy.SelectPrivateSessionTactics(own.AuthorizedCommands, own, seat.HeroId, random);
                Assert.That(answer, Is.Not.Null, seat.HeroId.Value);
                Accept(session, answer);
            }

            Assert.Fail("The teammate was never asked to swap.");
        }

        // -------------------------------------------------------------------------------------------
        // The completeness walk's fixture (owed by 9.32a and 9.32g)
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// Turn 1: Float like a Butterfly moves 0 and swaps for Sting like a Bee (a swap row in the
        /// log); turn 2: Bushido swaps the resolved Sting for Strike like a Tiger, facedown in slot 1 -
        /// the withheld slot every other seat reads as a gold.
        /// </summary>
        internal static MatchSession CreateFacedownGoldSession()
        {
            var session = StagedSession(tierThree: false, out var wasp, out var takahide);
            ActWith(session, wasp, takahide, TakahideCardContent.FloatLikeAButterfly);
            Accept(session, Start(session, TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.PrimaryMovement));
            AnswerHisChoices(session, TakahideCardContent.StingLikeABee);
            FinishTheTurn(session);
            ActWith(session, wasp, takahide, TakahideCardContent.Bushido);
            Accept(session, Start(session, TakahideCardContent.Bushido, TakahideCardContent.PrimarySkill));
            AnswerHisChoices(session, TakahideCardContent.StrikeLikeATiger);
            Assert.That(session.Projection.Cards.Single(card =>
                    card.OwnerId.Equals(takahide) &&
                    card.DefinitionReference.DefinitionId.Equals(TakahideCardContent.StrikeLikeATiger)).State.FaceOrientation,
                Is.EqualTo(CardFaceOrientation.FaceDown), "Fixture: Bushido turned the new card facedown");
            return session;
        }

        [Test]
        public void TheFacedownGoldFixture_IsWithheldFromTheOtherSeat_AsAGold_AndNamedToHis()
        {
            var session = CreateFacedownGoldSession();
            var host = new MatchHost(session);
            foreach (var seat in host.Seats.Seats)
            {
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(NetworkSeatProjectionCodec.TryDecode(NetworkSeatProjectionCodec.Encode(view), out var record),
                    Is.True);
                var network = new NetworkMatchReadModel(session.Projection, MatchContentCatalogs.ForThisBuild(), record, true);
                var slot = network.TurnSlots
                    .Where(value => value.RoundIndex == network.RoundIndex)
                    .SelectMany(value => value.Assignments)
                    .Single(value => value.IsFacedown);
                if (seat.Owns(slot.HeroId))
                {
                    Assert.That(slot.IsWithheld, Is.False, "his own seat names the facedown card");
                    Assert.That(slot.CardInstanceId.HasValue, Is.True);
                }
                else
                {
                    Assert.That(slot.IsWithheld, Is.True, "another seat reads no card");
                    Assert.That(slot.KnownColor, Is.EqualTo(CardColor.Gold), "but knows it is a gold");
                }
            }
        }

        #if UNITY_EDITOR
        [Test]
        public void PreparedPrivateArt_LoadsForTheStandeesTheSelectionFrontTheBackAndEveryDealtFace()
        {
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/takahide-red"), Is.Not.Null,
                "run Tools/Prepare-HeroTokens.ps1 -Hero takahide in this checkout");
            Assert.That(Resources.Load<Texture2D>("Private/Heroes/takahide-blue"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/HeroSelection/hero-takahide"), Is.Not.Null);
            Assert.That(Resources.Load<Texture2D>("Private/CardBacks/hero-takahide"), Is.Not.Null,
                "run Tools/Prepare-CardBacks.ps1 in this checkout");
            foreach (var definition in TakahideCardContent.DealtDefinitions)
            {
                Assert.That(ConfiguredCardArtworkCatalog.Catalog.TryGet(definition.Reference, out _),
                    Is.True, definition.DisplayName);
            }
        }
        #endif

        // -------------------------------------------------------------------------------------------
        // The staged boards
        // -------------------------------------------------------------------------------------------

        /// <summary>Wasp against Takahide on the engaged staged board his scenes load.</summary>
        internal static MatchSession StagedSession(bool tierThree, out HeroId wasp, out HeroId takahide)
        {
            var configuration = tierThree
                ? MatchConfiguration.CreateStagedTakahideTierThreeForInternalTest(TeamSide.Red)
                : MatchConfiguration.CreateStagedTakahideForInternalTest(TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = EngagedMirrorStaging.Restage(
                map,
                QuickGameSetupAuthority.Create(
                    QuickGameSetupConfiguration.CreateStagedWaspVersusTakahideOneVsOne(),
                    configuration.Participants, map, configuration.TieBreakerState),
                out _);
            var takahideSetup = setup.Heroes.Single(hero => hero.ContentId.Value == TakahideCardContent.ContentSetId);
            takahide = takahideSetup.HeroId;
            var takahideId = takahide;
            wasp = setup.Heroes.Single(hero => !hero.HeroId.Equals(takahideId)).HeroId;
            return new MatchSession(new MatchSessionInitialState(
                Scenario(configuration, map, setup.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))),
                setup, new[] { map }));
        }

        /// <summary>
        /// A 2v2 with Takahide on Red beside an enemy melee minion and his teammate two spaces from
        /// him, every seat committed and the round run to Takahide's Set an Example turn - his teammate's
        /// card still unresolved.
        /// </summary>
        private static MatchSession TeammateSession(out HeroId takahide, out HeroId teammate)
        {
            var roster = RegisteredHeroRoster.ForThisBuild;
            RegisteredHero Hero(string id) => roster.Single(hero => hero.ContentId.Value == id);
            var lineup = new HeroLineup(new[] { Hero("takahide"), Hero("wasp") }, new[] { Hero("arien"), Hero("sabina") });
            var format = MatchFormat.TwoVersusTwoQuick;
            var configuration = MatchConfiguration.CreateForLineupForInternalTest(format, lineup, TeamSide.Red);
            var map = OfficialBoardMapCatalog.Load().Load(OfficialBoardMapCatalog.SelectedMap);
            var setup = QuickGameSetupAuthority.Create(
                HeroLineupComposition.Compose(format, lineup), configuration.Participants, map,
                configuration.TieBreakerState);
            var takahideSetup = setup.Heroes.Single(hero => hero.ContentId.Value == "takahide");
            var teammateSetup = setup.Heroes.Single(hero => hero.ContentId.Value == "wasp");
            takahide = takahideSetup.HeroId;
            teammate = teammateSetup.HeroId;

            var playable = new HashSet<HexCoordinate>(map.Spaces
                .Where(space => space.Kind == BoardMapSpaceKind.Playable).Select(space => space.Coordinate));
            var occupied = new HashSet<HexCoordinate>(setup.Minions.Select(minion => minion.Coordinate)
                .Concat(setup.Heroes.Select(hero => hero.Coordinate)));
            var redSpawn = takahideSetup.Coordinate;
            HexCoordinate takahideAt = default, teammateAt = default;
            var found = false;
            foreach (var minion in setup.Minions
                .Where(minion => minion.TeamSide == TeamSide.Blue && minion.MinionType == StandardMinionType.Melee)
                .OrderBy(minion => HexDistanceQuery.Calculate(minion.Coordinate, redSpawn))
                .ThenBy(minion => minion.MinionId.Value, StringComparer.Ordinal))
            {
                foreach (var at in minion.Coordinate.GetNeighbors()
                    .Where(value => playable.Contains(value) && !occupied.Contains(value)))
                {
                    var friend = playable.Where(value => !occupied.Contains(value) && !value.Equals(at) &&
                            HexDistanceQuery.Calculate(value, at) == 2 &&
                            HexDistanceQuery.Calculate(value, minion.Coordinate) > 1)
                        .OrderBy(value => value.Q).ThenBy(value => value.R)
                        .Select(value => (HexCoordinate?)value)
                        .FirstOrDefault();
                    if (!friend.HasValue)
                        continue;
                    takahideAt = at;
                    teammateAt = friend.Value;
                    found = true;
                    break;
                }

                if (found) break;
            }

            Assert.That(found, Is.True, "Fixture: no Blue melee minion has a free neighbour with a free space two away");
            var takahideId = takahide;
            var teammateId = teammate;
            var heroes = setup.Heroes.Select(hero =>
            {
                var coordinate = hero.HeroId.Equals(takahideId) ? takahideAt
                    : hero.HeroId.Equals(teammateId) ? teammateAt
                    : hero.Coordinate;
                var space = map.Spaces.Single(value => value.Coordinate.Equals(coordinate));
                return new QuickGameHeroSetupState(
                    hero.HeroId, hero.ContentId, hero.DisplayName, hero.TeamId, hero.TeamSide,
                    hero.SpawnPoint, new ConfiguredSpaceReference(map.Id, space.Id), coordinate);
            }).ToArray();
            var restaged = new QuickGameSetupState(
                setup.SelectedMap, setup.WaveCount, setup.TieBreakerState, setup.Teams, heroes, setup.Minions);
            var session = new MatchSession(new MatchSessionInitialState(
                Scenario(configuration, map, restaged.Heroes.Select(hero => new HeroOccupancy(hero.HeroId, hero.Coordinate))),
                restaged, new[] { map }));

            // Takahide commits Set an Example (Initiative 11); every other hero its lowest card, so his
            // teammate's card waits unresolved after his.
            foreach (var hero in restaged.Heroes)
            {
                var card = hero.HeroId.Equals(takahideId)
                    ? InHand(session, hero.HeroId).Single(value =>
                        value.DefinitionReference.DefinitionId.Equals(TakahideCardContent.SetAnExample))
                    : InHand(session, hero.HeroId)
                        .OrderBy(value => DefinitionOf(session, value).Initiative.Value)
                        .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                        .First();
                Accept(session, DirectorCommand.CommitCard(hero.HeroId, card.Id));
            }

            RunTo(session, takahideId);
            return session;
        }

        /// <summary>
        /// Both commit - Wasp her lowest-Initiative card, Takahide <paramref name="card"/> - and the
        /// round runs, every other actor Holding, until Takahide's turn with that card is being taken.
        /// </summary>
        private static void ActWith(MatchSession session, HeroId wasp, HeroId takahide, CardDefinitionId card)
        {
            var takahideCard = InHand(session, takahide).Single(value => value.DefinitionReference.DefinitionId.Equals(card));
            var waspCard = InHand(session, wasp)
                .OrderBy(value => DefinitionOf(session, value).Initiative.Value)
                .ThenBy(value => value.Id.Value, StringComparer.Ordinal)
                .First();
            Accept(session, DirectorCommand.CommitCard(wasp, waspCard.Id));
            Accept(session, DirectorCommand.CommitCard(takahide, takahideCard.Id));
            RunTo(session, takahide);
            Assert.That(session.Projection.SelectedActor.CardInstanceId, Is.EqualTo(takahideCard.Id));
        }

        private static void RunTo(MatchSession session, HeroId hero)
        {
            for (var step = 0; step < 12 && !(session.Projection.IsTurnActive &&
                session.Projection.SelectedActor.ActorId.Equals(hero)); step++)
            {
                var projection = session.Projection;
                if (projection.PendingInitiativeChoice != null)
                {
                    var tie = projection.PendingInitiativeChoice;
                    var mine = tie.CandidateCardInstanceIds.FirstOrDefault(id =>
                        session.Projection.Cards.Single(card => card.Id.Equals(id)).OwnerId.Equals(hero));
                    Accept(session, DirectorCommand.ChooseTiedActor(tie.OwningSide,
                        mine.IsDefined ? mine : tie.CandidateCardInstanceIds.First()));
                }
                else if (projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                }
                else if (projection.IsTurnActive)
                {
                    Accept(session, DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
                }
                else
                {
                    Assert.Fail("Fixture: the round reached no turn for " + hero.Value + ": " + projection.Phase);
                }
            }

            Assert.That(session.Projection.SelectedActor.ActorId, Is.EqualTo(hero));
        }

        /// <summary>Releases the turn and lets the round run to its next commitment.</summary>
        private static void FinishTheTurn(MatchSession session)
        {
            for (var step = 0; step < 16; step++)
            {
                var projection = session.Projection;
                if (projection.PendingTurnRelease != null)
                {
                    Accept(session, DirectorCommand.ReleaseTurn());
                }
                else if (projection.IsTurnActive)
                {
                    Accept(session, DirectorCommand.PerformHold(projection.SelectedActor.CardInstanceId));
                }
                else
                {
                    var advance = DeterministicMatchLoop.EnumerateLegalCommands(projection)
                        .FirstOrDefault(command => command.Kind == DirectorCommandKind.AdvanceEndOfRound);
                    if (advance == null) return;
                    Accept(session, advance);
                }
            }
        }

        /// <summary>Stops a movement at once and answers a swap with <paramref name="chosen"/>.</summary>
        private static void AnswerHisChoices(MatchSession session, CardDefinitionId chosen)
        {
            for (var step = 0; step < 8 && session.Projection.DecisionContract != null; step++)
            {
                var commands = session.Projection.DecisionContract.Commands;
                if (commands.All(command => command.Kind != DirectorCommandKind.StopMovement &&
                    command.Kind != DirectorCommandKind.AnswerEffectChoice))
                {
                    return;
                }

                var stop = commands.FirstOrDefault(command => command.Kind == DirectorCommandKind.StopMovement);
                if (stop != null)
                {
                    Accept(session, stop);
                    continue;
                }

                Accept(session, commands.Single(command =>
                    command.Kind == DirectorCommandKind.AnswerEffectChoice &&
                    command.EffectChoice.Kind == EffectChoiceCommandKind.Select &&
                    command.EffectChoice.Candidate.Value.Kind == GuardsDigital.Core.Effects.EffectValueKind.CardInstance &&
                    session.Projection.Cards.Single(card => card.Id.Equals(command.EffectChoice.Candidate.Value.CardInstanceId))
                        .DefinitionReference.DefinitionId.Equals(chosen)));
            }
        }

        private static DirectorCommand Start(MatchSession session, CardDefinitionId card, ActionDefinitionId action)
        {
            var offer = session.Projection.AvailableTurnActions.Single(value =>
                value.SourceDefinitionReference.DefinitionId.Equals(card) &&
                value.ActionDefinitionId.Equals(action));
            Assert.That(offer.IsAvailable, Is.True, card.Value + " is offered");
            return DirectorCommand.StartAction(offer.SourceCardInstanceId, offer.ActionDefinitionId);
        }

        /// <summary>Every decision the action raises, answered by the owning seat's bot from its own projection.</summary>
        private static List<(string Node, HeroId Seat)> BotsAnswerUntilTheActionEnds(MatchSession session, int seed)
        {
            var host = new MatchHost(session);
            var random = new DeterministicBotRandom(seed);
            var answered = new List<(string Node, HeroId Seat)>();
            for (var step = 0; step < 40; step++)
            {
                var rules = session.Projection;
                if (rules.DecisionContract == null)
                {
                    return answered;
                }

                var seat = host.Seats.Seats.First(value => rules.DecisionOwner.IsOwnedBy(value));
                var view = host.ProjectionForSeat(seat.Id);
                Assert.That(view.AuthorizedCommands, Is.Not.Empty,
                    $"{seat.HeroId.Value} owns the standing decision ({rules.DecisionOwner}) and is offered no answer");
                var pick = BotPolicy.SelectPrivateSessionTactics(view.AuthorizedCommands, view, seat.HeroId, random);
                Assert.That(pick, Is.Not.Null);
                answered.Add((rules.PendingEffectChoice?.NodeId.Value ?? rules.DecisionOwner.FamilyId.ToString(),
                    seat.HeroId));
                var result = session.Execute(pick);
                Assert.That(result.Accepted, Is.True, $"{seat.HeroId.Value}'s bot answer {pick}: {result.Detail}");
            }

            Assert.Fail("The bots did not finish the action in 40 answers: " +
                string.Join(", ", answered.Select(entry => entry.Node)));
            return answered;
        }

        private static IEnumerable<CardInstance> InHand(MatchSession session, HeroId hero) =>
            session.Projection.Cards.Where(card => card.OwnerId.Equals(hero) && card.State.Location == CardLocation.Hand);

        private static CardDefinition DefinitionOf(MatchSession session, CardInstance card) =>
            session.Projection.Definitions.Single(definition => definition.Reference.Equals(card.DefinitionReference));

        private static void Accept(MatchSession session, DirectorCommand command)
        {
            var result = session.Execute(command);
            Assert.That(result.Accepted, Is.True, $"{command}: {result.Detail}");
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
