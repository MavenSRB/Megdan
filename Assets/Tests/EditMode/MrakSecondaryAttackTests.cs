using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.112a. The printed secondary Attack - the crossed-swords value on Mrak's six Red
    /// faces, the first secondary Attack in the roster - performed through a real configured turn.
    ///
    /// <para>The card under test is Seismic Slam's printed SHAPE with its text left for 8.112c: a
    /// Skill primary with a Radius 3 icon (its composition declared awaited), Defense 7, a secondary
    /// Movement 3 and the secondary Attack 6. The shape is what this milestone owns; the Radius icon
    /// beside it is what makes p.12's "even if the card has a radius icon" a real test.</para>
    /// </summary>
    public sealed class MrakSecondaryAttackTests
    {
        private static readonly HeroId Mrak = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Adjacent = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId TwoAway =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MinionId EnemyMinion = new MinionId("mrak-test-blue-melee");
        private static readonly MapId Map = new MapId("mrak-secondary-attack-test");

        private static readonly HexCoordinate MrakAt = new HexCoordinate(0, 0);
        private static readonly HexCoordinate AdjacentAt = new HexCoordinate(0, 1);
        private static readonly HexCoordinate TwoAwayAt = new HexCoordinate(1, 1);
        private static readonly HexCoordinate MinionAt = new HexCoordinate(-1, 0);

        [Test]
        public void TheSecondaryAttackIsANonRangedTextFreeReplaceableAttack()
        {
            var action = MrakCardContent.SecondaryAttack(6);

            Assert.That(action.Kind, Is.EqualTo(ActionKind.Attack));
            Assert.That(action.Origin, Is.EqualTo(ActionDefinitionOrigin.PrintedSecondary));
            Assert.That(action.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged),
                "It prints no Range, so p.12 makes it non-Ranged - Seismic Slam's own note says so.");
            Assert.That(action.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Attack));
            Assert.That(action.BaseValues.Single().Amount, Is.EqualTo(6));
            Assert.That(action.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Suppressed),
                "p.9: the card text applies only when performing the Primary action.");
            Assert.That(action.DefaultPolicy.CanBeReplaced, Is.True,
                "p.13's Clear replaces \"your entire Attack action\".");
            Assert.That(action.EffectRoot, Is.Null);
        }

        [Test]
        public void TheCardOffersItsSecondaryAttackBesideItsOtherActions()
        {
            var turn = new Turn();
            var offers = ActionOfferQuery.GetAvailableTurnActionOffers(
                turn.Definitions, turn.Instances, Mrak, turn.Played.Id);

            var attack = offers.Single(offer =>
                offer.ActionDefinitionId.Equals(MrakCardContent.SecondaryAttackId));
            Assert.That(attack.InvocationContext, Is.EqualTo(ActionInvocationContext.PrintedSecondary));
            Assert.That(attack.AttackRange, Is.EqualTo(AttackRangeKind.NonRanged));
            Assert.That(offers.Select(offer => offer.ActionDefinitionId.Value),
                Does.Contain("secondary-movement"),
                "the secondary Movement is still offered beside it");
        }

        [Test]
        public void ItTargetsOnlyAdjacentUnits_EvenBesideARadiusIcon()
        {
            var turn = new Turn();
            var choice = turn.StartSecondaryAttack();

            Assert.That(choice.LegalTargetHeroIds, Is.EqualTo(new[] { Adjacent }),
                "The hero two spaces away is inside the card's Radius 3 and is still not a target.");
            Assert.That(choice.LegalTargetMinionIds, Is.EqualTo(new[] { EnemyMinion }),
                "an adjacent enemy minion is a unit it may attack.");
        }

        [Test]
        public void AHeroTargetDefendsAgainstThePrintedValue_AndNoCardTextRuns()
        {
            var turn = new Turn();
            var choice = turn.StartSecondaryAttack();
            var answered = turn.Authority.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, Adjacent));
            Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());

            var defense = turn.Authority.PendingDefenseChoice;
            Assert.That(defense, Is.Not.Null, "p.14: the attacked hero gets the Defense window.");
            Assert.That(defense.AttackTotal, Is.EqualTo(6),
                "the printed secondary value, with no item and no text");
            Assert.That(turn.Authority.PendingSkillResolution, Is.Null);
            Assert.That(turn.Authority.PendingEffectChoice, Is.Null,
                "the Skill primary's text is never reached by a secondary action");

            var wall = defense.LegalDefenseOffers.First(offer =>
                offer.SourceDefinitionReference.Equals(Wall.Reference));
            Assert.That(turn.Authority.TryAnswerDefenseResponse(
                DefenseResponseChoiceCommand.SelectCards(
                    defense.ResolutionId, defense.ChoiceId, wall.SourceCardInstanceId)).Accepted,
                Is.True);
            Assert.That(turn.Authority.PendingDefenseChoice, Is.Null);
        }

        [Test]
        public void AMinionTargetIsDefeated()
        {
            var turn = new Turn();
            var choice = turn.StartSecondaryAttack();
            var answered = turn.Authority.TryAnswerAttackTarget(
                new AttackTargetChoiceCommand(choice.ResolutionId, choice.ChoiceId, EnemyMinion));

            Assert.That(answered.Accepted, Is.True, answered.RejectionReason.ToString());
            Assert.That(turn.Board.TryGetObjectCoordinate(
                    BoardObjectReference.ForMinion(EnemyMinion), out _), Is.False,
                "p.13: \"When you target an enemy minion with an attack, it is simply defeated.\"");
        }

        [Test]
        public void ClearMayReplaceTheSecondaryAttack()
        {
            var turn = new Turn();
            var clear = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                MrakCardContent.SecondaryAttackId,
                turn.Played.Id,
                Mrak,
                turn.Definitions,
                turn.Instances,
                RuleModificationSet.Empty);

            Assert.That(clear, Is.Not.Null,
                "p.13: \"You may perform one Clear secondary action instead of your entire Attack " +
                "action\" - the secondary Attack is an Attack action.");
        }

        [Test]
        public void AttackItemsAddToThePrintedSecondaryValue()
        {
            var turn = new Turn();
            var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                    turn.Definitions, turn.Instances, Mrak, turn.Played.Id)
                .Single(candidate =>
                    candidate.ActionDefinitionId.Equals(MrakCardContent.SecondaryAttackId));
            var items = new HeroItemState(turn.Participants);
            Assert.That(items.TryGrantItemFromTheBox(Mrak, PrintedValueChannel.Attack).IsPlaced, Is.True);

            // The same two public queries the Attack total composes (printed value, then items).
            var printed = PrintedValueQuery.CalculatePrintedValue(
                offer, turn.Definitions, PrintedValueChannel.Attack);
            var total = ItemBonusQuery.Apply(printed, items, Mrak, RuleModificationSet.Empty);

            Assert.That(printed.Total, Is.EqualTo(6));
            Assert.That(total.Total, Is.EqualTo(7),
                "p.12: \"+1 for every Attack item you have\" - on any Attack action.");
        }

        // -------------------------------------------------------------------------------------------

        private static readonly CardDefinition SeismicShape = new CardDefinition(
            new CardDefinitionId("mrak-test-seismic-shape"), "rev-1",
            "TEST - Seismic Slam's printed shape", new InitiativeValue(7),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("primary-skill"), ActionKind.Skill,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Radius, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default,
                    textComposition: PrintedTextComposition.AwaitedAt("8.112c")),
                new ActionDefinition(
                    new ActionDefinitionId("secondary-movement"), ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true)),
                MrakCardContent.SecondaryAttack(6),
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 7) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        private static readonly CardDefinition Wall = new CardDefinition(
            new CardDefinitionId("mrak-test-defense-wall"), "rev-1",
            "TEST - Mrak defense wall", new InitiativeValue(20),
            CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"), ActionKind.Defense,
                    ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(CardLocation.Hand, CardLocation.DiscardPile))
            });

        /// <summary>One configured turn with the Seismic shape played and Mrak selected to act.</summary>
        private sealed class Turn
        {
            internal readonly CardInstance Played;
            internal readonly CardDefinition[] Definitions;
            internal readonly CardInstance[] Instances;
            internal readonly MatchParticipants Participants;
            internal readonly AdjacentMoveAuthority Board;
            internal readonly TurnResolutionAuthority Authority;

            internal Turn()
            {
                var configuration = MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                    TeamSide.Red);
                Participants = configuration.Participants;
                Played = CardInstance.CreateInHand(
                    new CardInstanceId("mrak-seismic-played"), SeismicShape.Reference, Mrak);
                var walls = new[] { Adjacent, TwoAway }.Select(hero => CardInstance.CreateInHand(
                    new CardInstanceId("mrak-wall-" + hero.Value), Wall.Reference, hero)).ToArray();
                Definitions = configuration.CardDefinitions
                    .Concat(new[] { SeismicShape, Wall }).ToArray();
                Instances = configuration.CardInstances
                    .Concat(new[] { Played }).Concat(walls).ToArray();

                var spaces = from q in Enumerable.Range(-5, 11)
                             from r in Enumerable.Range(-5, 11)
                             where Math.Abs(q + r) <= 5
                             select new HexCoordinate(q, r);
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(Map, 1, spaces.ToArray()),
                    new[]
                    {
                        new HeroOccupancy(Mrak, MrakAt),
                        new HeroOccupancy(Adjacent, AdjacentAt),
                        new HeroOccupancy(TwoAway, TwoAwayAt)
                    },
                    Array.Empty<ObstacleOccupancy>(),
                    new[]
                    {
                        new QuickGameMinionSetupState(
                            EnemyMinion,
                            new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                            TeamSide.Blue,
                            StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(Map, new SpawnPointId("mrak-spawn")),
                            new ConfiguredSpaceReference(Map, new SpaceId("q-1_r0")),
                            MinionAt)
                    });
                var catalogs = new MatchContentCatalogs(
                    SyntheticEffectCapabilities.CreateCatalog(Array.Empty<IEffectCapabilityHandler>()),
                    SyntheticComponentDefinitions.CreateCatalog(),
                    new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
                var cardActions = new CardActionResolutionAuthority(catalogs, Board);
                foreach (var committed in new[]
                {
                    Played,
                    configuration.CardInstances.First(card =>
                        card.OwnerId.Equals(Adjacent) && card.State.Location == CardLocation.Hand),
                    configuration.CardInstances.First(card =>
                        card.OwnerId.Equals(TwoAway) && card.State.Location == CardLocation.Hand)
                })
                {
                    Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                    Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
                }

                Authority = new TurnResolutionAuthority(
                    Definitions, Instances, Participants, new TieBreakerState(TeamSide.Red),
                    cardActions, new ActiveEffectLifecycle(), 1, 1);
                Assert.That(Authority.TryStart().Accepted, Is.True);
                AdvanceToMrak();
            }

            /// <summary>Holds every other actor until Mrak's revealed card is the one to act.</summary>
            private void AdvanceToMrak()
            {
                for (var step = 0; step < 20; step++)
                {
                    if (Authority.PendingChoice != null)
                    {
                        var tie = Authority.PendingChoice;
                        Assert.That(Authority.TryChooseTiedActor(tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(Played.Id)
                                ? Played.Id : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }

                    if (Authority.PendingTurnRelease != null)
                    {
                        Assert.That(Authority.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }

                    if (Authority.SelectedActor.CardInstanceId.Equals(Played.Id))
                    {
                        return;
                    }

                    Assert.That(Authority.SelectedActor.CardInstanceId.IsDefined, Is.True,
                        "Mrak's card never came up to act.");
                    Assert.That(Authority.TryPerformHold(Authority.SelectedActor.CardInstanceId)
                        .Accepted, Is.True);
                }

                Assert.Fail("Mrak's card never came up to act.");
            }

            internal PendingAttackTargetChoice StartSecondaryAttack()
            {
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                        Definitions, Instances, Mrak, Played.Id)
                    .Single(candidate =>
                        candidate.ActionDefinitionId.Equals(MrakCardContent.SecondaryAttackId));
                var started = Authority.TryStartAttack(
                    new ActionResolutionId("mrak-secondary-attack"), offer);
                Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());
                var choice = Authority.PendingAttackTargetChoice;
                Assert.That(choice, Is.Not.Null, "a non-Ranged Attack asks for its adjacent target.");
                return choice;
            }
        }
    }
}
