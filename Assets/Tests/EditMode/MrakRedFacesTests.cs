using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112c. Mrak's Red faces through configured rounds: the Seismic line's selection,
    /// discard-or-defeat and Epicentre's different-target repeat; and Stone Carapace's This round
    /// substitution as the user settled it - MR-Q4 (an empty hand is saved), MR-Q5 (any demanded
    /// discard, whatever colour), MR-Q6 (an ordinary Defense still discards the card used; the
    /// Carapace defends with its own printed Defense).
    /// </summary>
    public sealed class MrakRedFacesTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId EnemyAlly = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);

        private static HexCoordinate H(int q, int r) => new HexCoordinate(q, r);

        // --- The Seismic line ----------------------------------------------------------------------

        [Test]
        public void SeismicSlam_TargetsAnEnemyBesideARock_WhoDiscards()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) });
            var slam = match.Give(Mrak, MrakRedFaces.SeismicSlam);
            var hold = match.Give(Enemy, Hold);
            var wall = match.Give(Enemy, Wall);
            match.CommitTurn((Mrak, slam), (Enemy, hold));
            match.ActWith(slam);
            match.StartSkill(slam, MrakCardContent.PrimarySkill);

            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { Enemy }));
            match.PickHero(Enemy);
            Assert.That(match.OfferedCards(), Is.EqualTo(new[] { wall.Id }),
                "The enemy chooses the card they discard.");
            match.PickCard(wall.Id);

            Assert.That(wall.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(match.OnBoard(Enemy), Is.True, "A discard answers the step; no defeat.");
        }

        [Test]
        public void SeismicSlam_TargetsAnEnemyBesideTerrain()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                terrainAt: new[] { H(1, 3) });
            var slam = match.Give(Mrak, MrakRedFaces.SeismicSlam);
            var hold = match.Give(Enemy, Hold);
            match.CommitTurn((Mrak, slam), (Enemy, hold));
            match.ActWith(slam);
            match.StartSkill(slam, MrakCardContent.PrimarySkill);

            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { Enemy }),
                "\"adjacent to terrain, or to a rock token\" - terrain alone qualifies.");
        }

        [Test]
        public void SeismicSlam_IsNotOfferedWhenNoEnemyStandsBesideTerrainOrARock()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) });
            var slam = match.Give(Mrak, MrakRedFaces.SeismicSlam);
            var hold = match.Give(Enemy, Hold);
            match.CommitTurn((Mrak, slam), (Enemy, hold));
            match.ActWith(slam);

            Assert.That(match.IsOffered(MrakCardContent.PrimarySkill), Is.False,
                "no legal subject, so the printed Skill is not offered to do nothing");
            Assert.That(match.IsOffered(MrakCardContent.SecondaryAttackId), Is.False,
                "and the secondary Attack has nobody adjacent");
            Assert.That(match.IsOffered(MrakCardContent.SecondaryMovement), Is.True);
        }

        [Test]
        public void SeismicSlam_AnEmptyHandIsDefeated()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)) },
                rocksAt: new[] { H(0, 4) });
            var slam = match.Give(Mrak, MrakRedFaces.SeismicSlam);
            var hold = match.Give(Enemy, Hold);
            match.CommitTurn((Mrak, slam), (Enemy, hold));
            match.ActWith(slam);
            match.StartSkill(slam, MrakCardContent.PrimarySkill);
            match.PickHero(Enemy);

            Assert.That(match.OnBoard(Enemy), Is.False,
                "\"discards a card, or is defeated\" - nothing in hand, so the second arm.");
        }

        [Test]
        public void Epicentre_RepeatsOnADifferentTarget()
        {
            var match = new Match(
                heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 3)), (EnemyAlly, H(3, 0)) },
                rocksAt: new[] { H(0, 4), H(4, 0) }, withAlly: true);
            var epicentre = match.Give(Mrak, MrakRedFaces.Epicentre);
            var hold = match.Give(Enemy, Hold);
            var allyHold = match.Give(EnemyAlly, Hold);
            var wall = match.Give(Enemy, Wall);
            var allyWall = match.Give(EnemyAlly, Wall);
            match.CommitTurn((Mrak, epicentre), (Enemy, hold), (EnemyAlly, allyHold));
            match.ActWith(epicentre);
            match.StartSkill(epicentre, MrakCardContent.PrimarySkill);

            Assert.That(match.OfferedHeroes(), Is.EquivalentTo(new[] { Enemy, EnemyAlly }));
            match.PickHero(Enemy);
            match.PickCard(wall.Id);

            // Milestone 9.1, ruling W1: no "may" is printed, so the repeat follows without a question.
            Assert.That(match.OfferedHeroes(), Is.EqualTo(new[] { EnemyAlly }),
                "\"Repeat once on a different target\" - automatic, and the first hero is taken out.");
            match.PickHero(EnemyAlly);
            match.PickCard(allyWall.Id);
            Assert.That(allyWall.State.Location, Is.EqualTo(CardLocation.DiscardPile));
        }

        // --- Stone Carapace ------------------------------------------------------------------------

        [Test]
        public void Carapace_MayBeDiscardedInsteadOfAHandCard()
        {
            var match = CarapaceThenSeismic(out var carapace, out var spare);
            Assert.That(match.OfferedCards(), Is.EquivalentTo(new[] { spare.Id, carapace.Id }),
                "Seismic Slam demands a discard from Mrak's hand; the resolved Carapace may answer it.");
            match.PickCard(carapace.Id);

            Assert.That(carapace.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(spare.State.Location, Is.EqualTo(CardLocation.Hand), "the hand card is kept");
            Assert.That(match.CarapaceGrantIsLive(carapace), Is.False,
                "p.17: its card changed state, so its This round effect is cancelled.");
            Assert.That(match.OnBoard(Mrak), Is.True);
        }

        [Test]
        public void Carapace_SavesAnEmptyHandedMrakFromDefeat()
        {
            var match = CarapaceThenSeismic(out var carapace, out _, withSpare: false);
            Assert.That(match.OfferedCards(), Is.EqualTo(new[] { carapace.Id }),
                "MR-Q4, user-settled: \"Yes, it saves him\".");
            match.PickCard(carapace.Id);

            Assert.That(match.OnBoard(Mrak), Is.True);
            Assert.That(carapace.State.Location, Is.EqualTo(CardLocation.DiscardPile));
        }

        [Test]
        public void Carapace_AnswersAColourDemandItDoesNotMatch()
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) });
            var carapace = match.Give(Mrak, MrakRedFaces.StoneCarapace);
            var mrakHold = match.Give(Mrak, Hold);
            var spare = match.Give(Mrak, Wall);
            var hold = match.Give(Enemy, Hold);
            var greenDemand = match.Give(Enemy, GreenDemand);
            match.PlayCarapaceTurn(carapace, hold);

            match.CommitTurn((Mrak, mrakHold), (Enemy, greenDemand));
            match.ActWith(greenDemand);
            match.StartSkill(greenDemand, GreenDemandSkill);
            match.PickHero(Mrak);

            Assert.That(match.OfferedCards(), Is.EqualTo(new[] { carapace.Id }),
                "MR-Q5, user-settled: \"Yes, always\" - no Green card in hand, and the Red Carapace " +
                "still answers the demand.");
            match.PickCard(carapace.Id);
            Assert.That(spare.State.Location, Is.EqualTo(CardLocation.Hand));
        }

        [Test]
        public void Carapace_DefendsWithItsOwnDefense_AndIsSpent()
        {
            var match = CarapaceThenAttack(out var carapace, out var spare);
            var offers = match.DefenseOffers();
            Assert.That(offers.Select(offer => offer.SourceCardInstanceId),
                Is.EquivalentTo(new[] { spare.Id, carapace.Id }),
                "\"you may discard this card to perform its defense action, as if it was in your hand\"");
            match.Defend(carapace.Id);

            Assert.That(match.OnBoard(Mrak), Is.True, "Defense 6 against the Attack of 5.");
            Assert.That(carapace.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(spare.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(match.CarapaceGrantIsLive(carapace), Is.False);
        }

        [Test]
        public void Carapace_DoesNotReplaceTheCardOfAnOrdinaryDefense()
        {
            var match = CarapaceThenAttack(out var carapace, out var spare);
            match.Defend(spare.Id);

            Assert.That(spare.State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "MR-Q6, user-settled (A): the card defended with is the card discarded.");
            Assert.That(carapace.State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(match.CarapaceGrantIsLive(carapace), Is.True,
                "and the Carapace's This round grant is still standing.");
        }

        [Test]
        public void CarapacesDiscardChoiceRestoresWithTheSubstituteAndReplaysToTheSameBytes()
        {
            var match = CarapaceThenSeismic(out var carapace, out _);
            match.AssertRestoreReplays(carapace.Id);
        }

        // --- Fixtures ------------------------------------------------------------------------------

        /// <summary>Turn 1: Mrak plays Stone Carapace. Turn 2: the enemy's Seismic Slam names Mrak.</summary>
        private static Match CarapaceThenSeismic(
            out CardInstance carapace, out CardInstance spare, bool withSpare = true) =>
            CarapaceThenSeismicCore(out carapace, out spare, withSpare);

        private static Match CarapaceThenSeismicCore(
            out CardInstance carapace, out CardInstance spare, bool withSpare)
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 2)) },
                rocksAt: new[] { H(-1, 0) });
            carapace = match.Give(Mrak, MrakRedFaces.StoneCarapace);
            var mrakHold = match.Give(Mrak, Hold);
            spare = withSpare ? match.Give(Mrak, Wall) : null;
            var hold = match.Give(Enemy, Hold);
            var slam = match.Give(Enemy, MrakRedFaces.SeismicSlam);
            match.PlayCarapaceTurn(carapace, hold);

            match.CommitTurn((Mrak, mrakHold), (Enemy, slam));
            match.ActWith(slam);
            match.StartSkill(slam, MrakCardContent.PrimarySkill);
            match.PickHero(Mrak);
            return match;
        }

        /// <summary>Turn 1: Mrak plays Stone Carapace. Turn 2: the enemy attacks him for 5.</summary>
        private static Match CarapaceThenAttack(out CardInstance carapace, out CardInstance spare)
        {
            var match = new Match(heroes: new[] { (Mrak, H(0, 0)), (Enemy, H(0, 1)) });
            carapace = match.Give(Mrak, MrakRedFaces.StoneCarapace);
            var mrakHold = match.Give(Mrak, Hold);
            spare = match.Give(Mrak, Wall);
            var hold = match.Give(Enemy, Hold);
            var strike = match.Give(Enemy, Strike);
            match.PlayCarapaceTurn(carapace, hold);

            match.CommitTurn((Mrak, mrakHold), (Enemy, strike));
            match.ActWith(strike);
            match.StartAttack(strike, StrikeAttack);
            match.TargetHero(Mrak);
            return match;
        }

        private static readonly ActionDefinitionId StrikeAttack = new ActionDefinitionId("primary-attack");
        private static readonly ActionDefinitionId GreenDemandSkill = new ActionDefinitionId("primary-skill");

        private static readonly CardDefinition Hold = new CardDefinition(
            new CardDefinitionId("mrak-red-test-hold"), "rev-1", "TEST - Hold", new InitiativeValue(1),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
            });

        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("mrak-red-test-wall"), "rev-1", "TEST - Wall", new InitiativeValue(2),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NonBasicCard(CardColor.Blue, CardTier.I, PrintedValueChannel.None),
            new[]
            {
                new ActionDefinition(new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static readonly CardDefinition Strike = new CardDefinition(
            new CardDefinitionId("mrak-red-test-strike"), "rev-1", "TEST - Strike", new InitiativeValue(9),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(StrikeAttack, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 5) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged, default)
            });

        /// <summary>"An enemy hero in radius discards a Green card, if able" - a colour demand.</summary>
        private static readonly CardDefinition GreenDemand = new CardDefinition(
            new CardDefinitionId("mrak-red-test-green-demand"), "rev-1", "TEST - Green demand",
            new InitiativeValue(9), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(GreenDemandSkill, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    new EffectDefinition(
                        new EffectDefinitionId("mrak-red-test-green-demand-skill"), "rev-1",
                        new EffectNodeId("root"),
                        new[]
                        {
                            new EffectNodeDefinition(new EffectNodeId("root"), EffectNodeKind.Sequence,
                                EffectFailurePolicy.Mandatory,
                                childNodeIds: new[] { new EffectNodeId("select"), new EffectNodeId("discard") }),
                            new EffectNodeDefinition(new EffectNodeId("select"), EffectNodeKind.Capability,
                                EffectFailurePolicy.Mandatory, SyntheticTargetingCapabilities.RadiusHeroTargetV1,
                                inputs: new[]
                                {
                                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill),
                                    EffectInputBinding.FromClosedVocabulary(
                                        SyntheticTargetingCapabilities.TargetRelationParameterName,
                                        SourceTeamRelation.OpposingSource),
                                    EffectInputBinding.FromLiteral(
                                        SyntheticTargetingCapabilities.ExcludeAdjacentParameterName,
                                        EffectValue.Boolean(false))
                                },
                                outputs: new[]
                                {
                                    new EffectOutputBinding(SyntheticRosterCapabilities.ChosenHeroOutput,
                                        EffectValueKind.Hero)
                                }),
                            new EffectNodeDefinition(new EffectNodeId("discard"), EffectNodeKind.Capability,
                                EffectFailurePolicy.IfAble, SyntheticCardStateCapabilities.DiscardChosenCardV1,
                                inputs: new[]
                                {
                                    EffectInputBinding.FromNodeOutput(SyntheticCardStateCapabilities.HeroParameter,
                                        new EffectNodeId("select"), SyntheticRosterCapabilities.ChosenHeroOutput,
                                        EffectValueKind.Hero),
                                    EffectInputBinding.FromLiteral(SyntheticCardStateCapabilities.ColorParameter,
                                        EffectValue.Integer((int)CardColor.Green))
                                },
                                outputs: new[]
                                {
                                    new EffectOutputBinding(SyntheticCardStateCapabilities.DiscardedOutput,
                                        EffectValueKind.CardInstance)
                                })
                        }))
            });

        /// <summary>A configured round over Mrak's faces and the fixture cards above.</summary>
        private sealed class Match
        {
            private readonly List<CardDefinition> definitions = new List<CardDefinition>
            {
                Hold, Wall, Strike, GreenDemand
            };
            private readonly List<CardInstance> instances = new List<CardInstance>();
            private readonly (HeroId Hero, HexCoordinate At)[] heroes;
            private readonly HexCoordinate[] rocks;
            private readonly HexCoordinate[] terrain;
            private readonly bool withAlly;
            private MatchContentCatalogs catalogs;
            private AdjacentMoveAuthority board;
            private RoundResolutionAuthority round;
            private int serial;

            internal Match((HeroId, HexCoordinate)[] heroes, HexCoordinate[] rocksAt = null,
                bool withAlly = false, HexCoordinate[] terrainAt = null)
            {
                terrain = terrainAt ?? Array.Empty<HexCoordinate>();
                this.heroes = heroes;
                rocks = rocksAt ?? Array.Empty<HexCoordinate>();
                this.withAlly = withAlly;
                definitions.AddRange(MrakRedFaces.Definitions);
            }

            private TurnResolutionAuthority Turn => round.CurrentTurnAuthority;

            internal CardInstance Give(HeroId owner, CardDefinitionId face) =>
                Give(owner, definitions.Single(value => value.Id.Equals(face)));

            internal CardInstance Give(HeroId owner, CardDefinition definition)
            {
                var card = CardInstance.CreateInHand(
                    new CardInstanceId($"mrak-red-{owner.Value}-{definition.Id.Value}-{serial++}"),
                    definition.Reference, owner);
                instances.Add(card);
                return card;
            }

            private void EnsureStarted()
            {
                if (round != null) return;
                var spaces = (from q in Enumerable.Range(-5, 11)
                              from r in Enumerable.Range(-5, 11)
                              where Math.Abs(q + r) <= 5
                              select new HexCoordinate(q, r))
                    .Where(space => !terrain.Contains(space)).ToArray();
                board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("mrak-red-faces"), 1, spaces, terrain),
                    heroes.Select(value => new HeroOccupancy(value.Hero, value.At)),
                    Array.Empty<ObstacleOccupancy>());
                foreach (var at in rocks)
                {
                    board.CommitValidatedTokenPlacement(new PlacedToken(
                        new TokenInstanceId($"mrak-red-rock-{at.Q}-{at.R}"), MrakComponentContent.Rock,
                        Mrak, Mrak, new CardInstanceId("mrak-red-earlier"), at));
                }

                catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(MrakComponentContent.Handlers()
                        .Concat(MrakRockPlacementContent.Handlers())
                        .Concat(MrakRedFaces.Handlers())),
                    new ComponentDefinitionCatalog(MrakComponentContent.Tokens(), Array.Empty<MarkerDefinition>()),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var markers = new MarkerAttachmentRegister();
                round = new RoundResolutionAuthority(
                    definitions, instances,
                    withAlly ? StagedBoardParticipants.CreateWithBlueAlly() : StagedBoardParticipants.Create(),
                    new TieBreakerState(TeamSide.Red), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, board, markers),
                        board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
            }

            internal void CommitTurn(params (HeroId Hero, CardInstance Card)[] commits)
            {
                EnsureStarted();
                foreach (var (hero, card) in commits)
                {
                    var result = round.TryCommitCard(hero, card.Id);
                    Assert.That(result.Accepted, Is.True, $"{hero.Value} commits {card.Id.Value}");
                }
            }

            /// <summary>Clears ties, releases and other actors until <paramref name="card"/> acts.</summary>
            internal void ActWith(CardInstance card)
            {
                for (var step = 0; step < 30; step++)
                {
                    if (Turn.PendingChoice != null)
                    {
                        var tie = Turn.PendingChoice;
                        Assert.That(round.TryChooseTiedActor(tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(card.Id)
                                ? card.Id : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (Turn.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (Turn.SelectedActor.CardInstanceId.Equals(card.Id)) return;
                    Assert.That(Turn.SelectedActor.CardInstanceId.IsDefined, Is.True,
                        card.Id.Value + " never came up to act.");
                    Assert.That(round.TryPerformHold(Turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                }

                Assert.Fail(card.Id.Value + " never came up to act.");
            }

            /// <summary>Holds and releases until the round asks for the next turn's cards.</summary>
            private void FinishTurn()
            {
                for (var step = 0; step < 30; step++)
                {
                    if (round.CurrentTurnAuthority == null || round.CurrentTurnAuthority.IsEnded) return;
                    if (Turn.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (!Turn.SelectedActor.CardInstanceId.IsDefined) return;
                    Assert.That(round.TryPerformHold(Turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                }
            }

            /// <summary>Turn 1: Mrak plays the Carapace face, moves nowhere, and the enemy holds.</summary>
            internal void PlayCarapaceTurn(CardInstance carapace, CardInstance enemyHold)
            {
                CommitTurn((Mrak, carapace), (Enemy, enemyHold));
                ActWith(carapace);
                Assert.That(round.TryStartMovement(new ActionResolutionId("mrak-carapace"),
                    Offer(carapace, MrakRedFaces.PrimaryMovement)).Accepted, Is.True);
                var choice = Turn.PendingMovementChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerMovementChoice(
                    MovementChoiceCommand.Stop(choice.ResolutionId, choice.ChoiceId)).Accepted, Is.True);
                Assert.That(CarapaceGrantIsLive(carapace), Is.True,
                    "the This round text came into effect before the movement (p.11)");
                FinishTurn();
            }

            internal bool CarapaceGrantIsLive(CardInstance carapace) =>
                SourceCardSubstitutionQuery.Substitutes(
                    Turn.CurrentModifications(), Mrak,
                    RulePermissionKind.DiscardSourceCardInsteadOfHandCard, instances)
                .Any(card => card.Id.Equals(carapace.Id));

            private ActionOffer Offer(CardInstance card, ActionDefinitionId action) =>
                ActionOfferQuery.GetAvailableTurnActionOffers(definitions, instances, card.OwnerId, card.Id)
                    .Single(offer => offer.ActionDefinitionId.Equals(action));

            internal bool IsOffered(ActionDefinitionId action) =>
                Turn.GetTurnActionOffers().Any(offer =>
                    offer.ActionDefinitionId.Equals(action) && offer.IsAvailable);

            internal void StartSkill(CardInstance card, ActionDefinitionId action) =>
                Assert.That(round.TryStartSkill(new ActionResolutionId("mrak-red-skill-" + serial++),
                    Offer(card, action)).Accepted, Is.True);

            internal void StartAttack(CardInstance card, ActionDefinitionId action) =>
                Assert.That(round.TryStartAttack(new ActionResolutionId("mrak-red-attack-" + serial++),
                    Offer(card, action)).Accepted, Is.True);

            internal void TargetHero(HeroId hero)
            {
                var choice = Turn.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, hero)).Accepted, Is.True);
            }

            internal IReadOnlyList<ActionOffer> DefenseOffers() => Turn.PendingDefenseChoice.LegalDefenseOffers;

            internal void Defend(CardInstanceId card)
            {
                var defense = Turn.PendingDefenseChoice;
                Assert.That(defense, Is.Not.Null);
                var result = round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, card));
                Assert.That(result.Accepted, Is.True, result.ToString());
            }

            internal HeroId[] OfferedHeroes() =>
                Turn.PendingEffectChoice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.Hero)
                    .Select(candidate => candidate.Value.HeroId).ToArray();

            internal CardInstanceId[] OfferedCards()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "a discard question is open");
                return choice.Candidates
                    .Where(candidate => candidate.Value.Kind == EffectValueKind.CardInstance)
                    .Select(candidate => candidate.Value.CardInstanceId).ToArray();
            }

            internal void PickHero(HeroId hero) =>
                Pick(round, value => value.Kind == EffectValueKind.Hero && value.HeroId.Equals(hero));

            internal void PickCard(CardInstanceId card) =>
                Pick(round, value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(card));

            internal void AcceptRepeat()
            {
                var choice = Turn.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null, "the repeat is offered");
                Assert.That(round.TryAnswerEffectChoice(choice.ConfirmInvocation).Accepted, Is.True);
            }

            private static void Pick(RoundResolutionAuthority target, Func<EffectValue, bool> which)
            {
                var choice = target.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(choice, Is.Not.Null);
                Assert.That(target.TryAnswerEffectChoice(choice.SelectInvocations
                    .Single(invocation => which(invocation.Candidate.Value))).Accepted, Is.True);
                var still = target.CurrentTurnAuthority.PendingEffectChoice;
                if (still != null && still.ChoiceId.Equals(choice.ChoiceId) && still.ConfirmInvocation != null)
                {
                    Assert.That(target.TryAnswerEffectChoice(still.ConfirmInvocation).Accepted, Is.True);
                }
            }

            internal bool OnBoard(HeroId hero) => board.TryGetHeroCoordinate(hero, out _);

            internal void AssertRestoreReplays(CardInstanceId answer)
            {
                var pending = Turn.PendingEffectChoice;
                Assert.That(pending, Is.Not.Null);
                var bytes = AuthoritativeSnapshotSerializer.Serialize(round, board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(
                    catalogs, bytes, definitions, new[] { board.MapDefinition },
                    Array.Empty<GuardsDigital.Core.Setup.QuickGameMinionSetupState>());
                var resumed = restored.Round.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(resumed?.NodeId, Is.EqualTo(pending.NodeId));
                Assert.That(resumed.Candidates.Select(candidate => candidate.ToString()),
                    Is.EqualTo(pending.Candidates.Select(candidate => candidate.ToString())),
                    "the restored question still offers the resolved Carapace");

                PickCard(answer);
                Pick(restored.Round, value => value.Kind == EffectValueKind.CardInstance &&
                    value.CardInstanceId.Equals(answer));
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                    Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, board)));
            }
        }
    }
}
