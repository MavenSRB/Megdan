using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32d: Takahide's gold cards, Bushido and Red faces composed on 9.32a-9.32c, one
    /// staged outcome per face, and every printed value read against the fronts ([his
    /// shard](Docs/AI/Rules/HERO_TAKAHIDE.md)'s inventory). Nothing is registered or dealt (9.32h).
    /// </summary>
    public sealed class TakahideBasicAndRedFacesTests
    {
        private static readonly CardStateFacts Resolved = new CardStateFacts(
            CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp);

        // -------------------------------------------------------------------------------------------
        // The print
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryFace_PrintsWhatTheFrontPrints()
        {
            //                                        init def mov  attack range radius item                         tier
            AssertPrint(TakahideCardContent.StingLikeABeeDefinition, 7, 1, 1, 5, 3, 0, PrintedValueChannel.None, CardTier.None);
            AssertPrint(TakahideCardContent.FloatLikeAButterflyDefinition, 8, 8, 0, 0, 0, 0, PrintedValueChannel.None, CardTier.None);
            AssertPrint(TakahideCardContent.StrikeLikeATigerDefinition, 9, 3, 3, 7, 0, 0, PrintedValueChannel.None, CardTier.None);
            AssertPrint(TakahideCardContent.BushidoDefinition, 5, 6, 0, 0, 0, 0, PrintedValueChannel.None, CardTier.None);
            AssertPrint(TakahideCardContent.SetAnExampleDefinition, 11, 1, 1, 2, 0, 3, PrintedValueChannel.None, CardTier.I);
            AssertPrint(TakahideCardContent.LeadFromTheFrontDefinition, 11, 2, 1, 3, 0, 4, PrintedValueChannel.Initiative, CardTier.II);
            AssertPrint(TakahideCardContent.SpinningBladeDefinition, 11, 2, 1, 3, 0, 4, PrintedValueChannel.Defense, CardTier.II);
            AssertPrint(TakahideCardContent.HoldMySakeDefinition, 12, 2, 1, 3, 0, 4, PrintedValueChannel.Movement, CardTier.III);
            AssertPrint(TakahideCardContent.BladeHelixDefinition, 12, 2, 1, 3, 0, 1, PrintedValueChannel.Range, CardTier.III);

            Assert.That(TakahideCardContent.FloatLikeAButterflyDefinition.ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).Kind, Is.EqualTo(ActionKind.Movement));
            Assert.That(TakahideCardContent.FloatLikeAButterflyDefinition.ActionDefinitions
                .Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary).BaseValues
                .Single(value => value.Channel == PrintedValueChannel.Movement).Amount, Is.EqualTo(5), "Basic Movement 5");
            Assert.That(TakahideCardContent.BushidoDefinition.Progression.Color, Is.EqualTo(CardColor.Silver));
            Assert.That(new[] { TakahideCardContent.StingLikeABeeDefinition, TakahideCardContent.FloatLikeAButterflyDefinition,
                    TakahideCardContent.StrikeLikeATigerDefinition }.Select(definition => definition.Progression.Color),
                Is.All.EqualTo(CardColor.Gold), "three gold cards");
            Assert.That(TakahideCardContent.SetAnExampleDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { TakahideCardContent.LeadFromTheFront, TakahideCardContent.SpinningBlade }));
            Assert.That(TakahideCardContent.LeadFromTheFrontDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { TakahideCardContent.HoldMySake, TakahideCardContent.BladeHelix }),
                "a Tier II names both Tier III faces of its colour (the Snorri lesson)");
            Assert.That(TakahideCardContent.SpinningBladeDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { TakahideCardContent.HoldMySake, TakahideCardContent.BladeHelix }));
        }

        private static void AssertPrint(
            CardDefinition card, int initiative, int defense, int movement, int attack, int range, int radius,
            PrintedValueChannel item, CardTier tier)
        {
            var primary = card.ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            int Value(IEnumerable<PrintedValueDefinition> values, PrintedValueChannel channel) =>
                values.Where(value => value.Channel == channel).Select(value => value.Amount).DefaultIfEmpty(0).Single();
            int Secondary(ActionKind kind, PrintedValueChannel channel) =>
                card.ActionDefinitions.Where(action => action.Origin != ActionDefinitionOrigin.PrintedPrimary && action.Kind == kind)
                    .Select(action => Value(action.BaseValues, channel)).DefaultIfEmpty(0).Single();
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative), card.DisplayName + " Initiative");
            Assert.That(Secondary(ActionKind.Defense, PrintedValueChannel.Defense), Is.EqualTo(defense), card.DisplayName + " Defense");
            Assert.That(Secondary(ActionKind.Movement, PrintedValueChannel.Movement), Is.EqualTo(movement), card.DisplayName + " Movement");
            Assert.That(Value(primary.BaseValues, PrintedValueChannel.Attack), Is.EqualTo(attack), card.DisplayName + " Attack");
            Assert.That(Value(primary.BaseValues, PrintedValueChannel.Range), Is.EqualTo(range), card.DisplayName + " Range");
            Assert.That(Value(primary.BaseValues, PrintedValueChannel.Radius), Is.EqualTo(radius), card.DisplayName + " Radius");
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item), card.DisplayName + " item");
            Assert.That(card.Progression.Tier, Is.EqualTo(tier), card.DisplayName + " tier");
        }

        // -------------------------------------------------------------------------------------------
        // The gold cards and Bushido
        // -------------------------------------------------------------------------------------------

        [Test]
        public void StingLikeABee_TargetsOnlyAtMaximumRange_ThenSwapsWithADeckGold()
        {
            var near = new Stage(TakahideCardContent.StingLikeABee, enemyAt: new HexCoordinate(2, 0),
                deck: new[] { TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.StrikeLikeATiger });
            Assert.That(near.IsOffered, Is.False, "\"at maximum range\": an enemy two spaces away is no target (RULING 1)");

            var stage = new Stage(TakahideCardContent.StingLikeABee, enemyAt: new HexCoordinate(3, 0),
                deck: new[] { TakahideCardContent.FloatLikeAButterfly, TakahideCardContent.StrikeLikeATiger });
            stage.AttackTheEnemyAndDefend();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EquivalentTo(new[] { stage.Id(TakahideCardContent.FloatLikeAButterfly), stage.Id(TakahideCardContent.StrikeLikeATiger) }));
            stage.ChooseCard(TakahideCardContent.StrikeLikeATiger);
            Assert.That(stage.Card(TakahideCardContent.StingLikeABee).State.IsInitialInDeck, Is.True);
            Assert.That(stage.Card(TakahideCardContent.StrikeLikeATiger).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void FloatLikeAButterfly_MovesFiveThenSwaps_TkQ1()
        {
            var stage = new Stage(TakahideCardContent.FloatLikeAButterfly,
                deck: new[] { TakahideCardContent.StingLikeABee, TakahideCardContent.StrikeLikeATiger });
            stage.StartMoving();
            Assert.That(stage.Turn.PendingMovementChoice.RemainingAllowance, Is.EqualTo(5), "Basic Movement 5");
            Assert.That(stage.Choice, Is.Null);
            stage.StopMovingIfAsked();
            stage.ChooseCard(TakahideCardContent.StingLikeABee);
            Assert.That(stage.Card(TakahideCardContent.FloatLikeAButterfly).State.IsInitialInDeck, Is.True);
            Assert.That(stage.Card(TakahideCardContent.StingLikeABee).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void StrikeLikeATiger_AttacksForSevenAdjacent_ThenSwaps()
        {
            var stage = new Stage(TakahideCardContent.StrikeLikeATiger,
                deck: new[] { TakahideCardContent.StingLikeABee, TakahideCardContent.FloatLikeAButterfly });
            stage.AttackTheEnemy();
            Assert.That(stage.Turn.PendingDefenseChoice.AttackTotal, Is.EqualTo(7), "Basic Attack 7");
            stage.DefendWithTheWall();
            stage.ChooseCard(TakahideCardContent.FloatLikeAButterfly);
            Assert.That(stage.Card(TakahideCardContent.FloatLikeAButterfly).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void Bushido_SwapsTheGoldInHand_FaceupIntoTheHand()
        {
            var stage = new Stage(TakahideCardContent.Bushido,
                hand: new[] { TakahideCardContent.FloatLikeAButterfly },
                deck: new[] { TakahideCardContent.StingLikeABee, TakahideCardContent.StrikeLikeATiger });
            stage.StartSkill();
            stage.ChooseCard(TakahideCardContent.StrikeLikeATiger);
            Assert.That(stage.Card(TakahideCardContent.StrikeLikeATiger).State.IsInitialInHand, Is.True);
            Assert.That(stage.Card(TakahideCardContent.FloatLikeAButterfly).State.IsInitialInDeck, Is.True);
        }

        [Test]
        public void Bushido_SwapsADiscardedGold_Facedown()
        {
            var stage = new Stage(TakahideCardContent.Bushido,
                discarded: new[] { TakahideCardContent.FloatLikeAButterfly },
                deck: new[] { TakahideCardContent.StingLikeABee, TakahideCardContent.StrikeLikeATiger });
            stage.StartSkill();
            stage.ChooseCard(TakahideCardContent.StingLikeABee);
            Assert.That(CardLifecycleAuthority.IsFacedownRestingCard(stage.Card(TakahideCardContent.StingLikeABee)), Is.True,
                "\"the new card is facedown\"");
        }

        // -------------------------------------------------------------------------------------------
        // The Red faces
        // -------------------------------------------------------------------------------------------

        [Test]
        public void SetAnExample_ATeammateInRadiusThreeSwapsTheirUnresolvedCard()
        {
            var stage = new Stage(TakahideCardContent.SetAnExample, allyAt: new HexCoordinate(-3, 0));
            stage.AttackTheEnemyAndDefend();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.HeroId), Is.EqualTo(new[] { Stage.Ally }),
                "the teammate three spaces away is in radius 3");
            stage.ChooseHero(Stage.Ally);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally),
                "9.32g (mockup D): the teammate answers one choice on their own seat");
            Assert.That(stage.Choice.DeclineInvocation, Is.Not.Null, "\"may\": KEEP MY CARD is on the same choice");
            stage.ChooseCard(stage.AllyHandCard);
            Assert.That(stage.Card(stage.AllyHandCard).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            stage.ReleaseIfAsked();
            Assert.That(stage.Turn.SelectedActor.ActorId, Is.EqualTo(Stage.Ally), "Initiative 9 now acts before the enemy's 5");

            var far = new Stage(TakahideCardContent.SetAnExample, allyAt: new HexCoordinate(-4, 0));
            far.AttackTheEnemyAndDefend();
            Assert.That(far.Choice, Is.Null, "four spaces away is outside radius 3: the Attack resolves alone");
            Assert.That(far.Card(TakahideCardContent.SetAnExample).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void SetAnExample_ATeammateWhoKeepsTheirCard_SwapsNothing()
        {
            var stage = new Stage(TakahideCardContent.SetAnExample, allyAt: new HexCoordinate(-3, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Card(stage.AllyHandCard).State.Location, Is.EqualTo(CardLocation.Hand),
                "KEEP MY CARD: the hand card stays where it was");
            Assert.That(stage.Card(TakahideCardContent.SetAnExample).State, Is.EqualTo(Resolved));
        }

        [Test]
        public void LeadFromTheFront_ReachesRadiusFour()
        {
            var stage = new Stage(TakahideCardContent.LeadFromTheFront, allyAt: new HexCoordinate(-4, 0));
            stage.AttackTheEnemyAndDefend();
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.HeroId), Is.EqualTo(new[] { Stage.Ally }));
        }

        [Test]
        public void HoldMySake_ReachesTheTeammatesDiscard()
        {
            var stage = new Stage(TakahideCardContent.HoldMySake, allyAt: new HexCoordinate(-3, 0), allyHandEmpty: true);
            stage.AttackTheEnemyAndDefend();
            stage.ChooseHero(Stage.Ally);
            Assert.That(stage.Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId),
                Is.EqualTo(new[] { stage.AllyDiscardedCard }), "\"or in their discard\"");
            stage.ChooseCard(stage.AllyDiscardedCard);
            Assert.That(stage.Card(stage.AllyDiscardedCard).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
        }

        [Test]
        public void SpinningBlade_BarsTheEnemyFromTheSpacesAdjacentToTakahide_ButNotTheTeammate()
        {
            var stage = new Stage(TakahideCardContent.SpinningBlade, allyAt: new HexCoordinate(-2, 0));
            stage.AttackTheEnemyAndDefend();
            stage.ReleaseIfAsked();
            var mods = stage.Turn.CurrentModifications();
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, new HexCoordinate(0, 1), mods, RuleObject.ForHero(Stage.Enemy)),
                Is.True, "adjacent to him, for an enemy unit");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, new HexCoordinate(2, -1), mods, RuleObject.ForHero(Stage.Enemy)),
                Is.False, "\"adjacent to you\": the printed Radius 4 is read by nothing");
            Assert.That(ObstacleQuery.BlocksEndOfMovement(stage.Board, new HexCoordinate(-1, 0), mods, RuleObject.ForHero(Stage.Ally)),
                Is.False, "for the enemy units only");
        }

        [Test]
        public void BladeHelix_BarsItsPrintedRadiusOfOne_AndARadiusItemWidensIt()
        {
            var two = new HexCoordinate(2, -1);
            var plain = new Stage(TakahideCardContent.BladeHelix);
            plain.AttackTheEnemyAndDefend();
            plain.ReleaseIfAsked();
            Assert.That(ObstacleQuery.BlocksEndOfMovement(plain.Board, new HexCoordinate(0, 1),
                plain.Turn.CurrentModifications(), RuleObject.ForHero(Stage.Enemy)), Is.True);
            Assert.That(ObstacleQuery.BlocksEndOfMovement(plain.Board, two,
                plain.Turn.CurrentModifications(), RuleObject.ForHero(Stage.Enemy)), Is.False, "Radius 1");

            var item = new Stage(TakahideCardContent.BladeHelix, radiusItem: true);
            item.AttackTheEnemyAndDefend();
            item.ReleaseIfAsked();
            Assert.That(ObstacleQuery.BlocksEndOfMovement(item.Board, two,
                item.Turn.CurrentModifications(), RuleObject.ForHero(Stage.Enemy)), Is.True, "\"in radius\" with a Radius item (R1)");
        }

        // -------------------------------------------------------------------------------------------
        // A staged round: Takahide (Blue), his teammate and one enemy (Red)
        // -------------------------------------------------------------------------------------------

        internal sealed class Stage
        {
            internal static readonly HeroId Taka = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            private static readonly CardInstanceId EnemyPlayed = new CardInstanceId("enemy-played");
            private static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");
            private static readonly CardInstanceId AllyPlayed = new CardInstanceId("ally-played");
            internal readonly CardInstanceId AllyHandCard = new CardInstanceId("ally-hand-9");
            internal readonly CardInstanceId AllyDiscardedCard = new CardInstanceId("ally-discarded-9");

            internal Stage(
                CardDefinitionId played,
                IEnumerable<CardDefinitionId> hand = null,
                IEnumerable<CardDefinitionId> deck = null,
                IEnumerable<CardDefinitionId> discarded = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? allyAt = null,
                bool allyHandEmpty = false,
                bool radiusItem = false)
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                var catalogs = new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)),
                    defaults.Components,
                    defaults.TriggeredEffects);
                Definitions = TakahideCardContent.BasicAndRedDefinitions
                    .Concat(new[] { Plain(5), Plain(2), Plain(9), Wall }).ToArray();
                CardDefinition Def(CardDefinitionId id) => Definitions.Single(value => value.Id.Equals(id));
                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Id(played), Def(played).Reference, Taka),
                    CardInstance.CreateInHand(EnemyPlayed, Plain(5).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, Wall.Reference, Enemy),
                    CardInstance.CreateInHand(AllyPlayed, Plain(2).Reference, Ally)
                };
                if (!allyHandEmpty)
                    cards.Add(CardInstance.CreateInHand(AllyHandCard, Plain(9).Reference, Ally));
                var allyDiscard = CardInstance.CreateInHand(AllyDiscardedCard, Plain(9).Reference, Ally);
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(allyDiscard), Is.True);
                cards.Add(allyDiscard);
                foreach (var id in hand ?? Enumerable.Empty<CardDefinitionId>())
                    cards.Add(CardInstance.CreateInHand(Id(id), Def(id).Reference, Taka));
                foreach (var id in deck ?? Enumerable.Empty<CardDefinitionId>())
                    cards.Add(CardInstance.CreateInDeck(Id(id), Def(id).Reference, Taka));
                foreach (var id in discarded ?? Enumerable.Empty<CardDefinitionId>())
                {
                    var card = CardInstance.CreateInHand(Id(id), Def(id).Reference, Taka);
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                    cards.Add(card);
                }

                Cards = cards.ToArray();
                var spaces = (from q in Enumerable.Range(-6, 13)
                              from r in Enumerable.Range(-6, 13)
                              where Math.Abs(q + r) <= 6
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("takahide-faces"), 1, spaces),
                    new[]
                    {
                        new HeroOccupancy(Taka, new HexCoordinate(0, 0)),
                        new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(1, 0)),
                        new HeroOccupancy(Ally, allyAt ?? new HexCoordinate(-5, 0))
                    },
                    Array.Empty<ObstacleOccupancy>());
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, StagedBoardParticipants.CreateWithBlueAlly(), new TieBreakerState(TeamSide.Blue), 1,
                    new RoundResolutionComposition(new CardActionResolutionAuthority(catalogs, Board, markers),
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                if (radiusItem)
                    Assert.That(Round.HeroItems.TryGrantItemFromTheBox(Taka, PrintedValueChannel.Radius).IsPlaced, Is.True);
                Accept(Round.TryCommitCard(Taka, Id(played)));
                Accept(Round.TryCommitCard(Enemy, EnemyPlayed));
                Accept(Round.TryCommitCard(Ally, AllyPlayed));
                Played = Id(played);
            }

            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;
            private CardInstanceId Played { get; }

            internal CardInstanceId Id(CardDefinitionId definition) => new CardInstanceId("taka-" + definition.Value);
            internal CardInstance Card(CardDefinitionId definition) => Card(Id(definition));
            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal bool IsOffered => Offers(ActionKind.Attack).Any(offer => offer.IsAvailable);

            internal void AttackTheEnemy()
            {
                Accept(Round.TryStartAttack(new ActionResolutionId("attack"), Offers(ActionKind.Attack).Single()));
                var target = Turn.PendingAttackTargetChoice;
                Assert.That(target, Is.Not.Null);
                Accept(Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)));
            }

            internal void DefendWithTheWall()
            {
                var defense = Turn.PendingDefenseChoice;
                Accept(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, EnemyWall)));
            }

            internal void AttackTheEnemyAndDefend()
            {
                AttackTheEnemy();
                DefendWithTheWall();
            }

            internal void StartMoving() =>
                Accept(Round.TryStartMovement(new ActionResolutionId("move"), Offers(ActionKind.Movement).Single()));

            internal void StartSkill() =>
                Accept(Round.TryStartSkill(new ActionResolutionId("skill"), Offers(ActionKind.Skill).Single()));

            internal void StopMovingIfAsked()
            {
                var move = Turn.PendingMovementChoice;
                if (move != null)
                    Accept(Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(move.ResolutionId, move.ChoiceId)));
            }

            internal void ChooseCard(CardDefinitionId definition) => ChooseCard(Id(definition));

            internal void ChooseCard(CardInstanceId card) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.CardInstanceId.Equals(card))));

            internal void ChooseHero(HeroId hero) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.HeroId.Equals(hero))));

            internal void Confirm() => Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));

            internal void Decline() => Accept(Round.TryAnswerEffectChoice(Choice.DeclineInvocation));

            internal void ReleaseIfAsked()
            {
                if (Turn != null && Turn.PendingTurnRelease != null)
                    Accept(Round.TryReleaseTurn());
            }

            private IEnumerable<ActionOffer> Offers(ActionKind kind) =>
                Turn.GetTurnActionOffers().Where(offer =>
                    offer.SourceCardInstanceId.Equals(Played) &&
                    Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Kind == kind &&
                    Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)).Origin ==
                        ActionDefinitionOrigin.PrintedPrimary);

            private readonly List<RoundResolutionEvent> events = new List<RoundResolutionEvent>();

            /// <summary>Milestone 9.32g. Every round event this stage's accepted commands produced.</summary>
            internal IReadOnlyList<RoundResolutionEvent> Events => events;

            private void Accept(RoundResolutionResult result)
            {
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                events.AddRange(result.Events);
            }

            private static CardDefinition Plain(int initiative) => new CardDefinition(
                new CardDefinitionId("takahide-faces-plain-" + initiative), "rev-1", "TEST - Initiative " + initiative,
                new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("takahide-faces-wall"), "rev-1", "TEST - Wall",
                new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(
                        new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                        ActionDefinitionOrigin.Reaction,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 12) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                        AttackRangeKind.None,
                        new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
                });
        }
    }
}
