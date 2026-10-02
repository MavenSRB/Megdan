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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 9.32e: Takahide's Blue and Green faces, one staged outcome per face, every printed
    /// value read against the fronts ([his shard](Docs/AI/Rules/HERO_TAKAHIDE.md)), and TK-Q4: with no
    /// teammate the face can act on, the Skill is greyed and nothing is spent. Nothing is registered or
    /// dealt (9.32h).
    /// </summary>
    public sealed class TakahideBlueAndGreenFacesTests
    {
        // -------------------------------------------------------------------------------------------
        // The print
        // -------------------------------------------------------------------------------------------

        [Test]
        public void EveryFace_PrintsWhatTheFrontPrints()
        {
            //                                                  init def range radius item                       tier
            AssertPrint(TakahideCardContent.ProvenWarriorDefinition, 9, 4, 0, 3, PrintedValueChannel.None, CardTier.I);
            AssertPrint(TakahideCardContent.ChosenChampionDefinition, 10, 4, 0, 4, PrintedValueChannel.Attack, CardTier.II);
            AssertPrint(TakahideCardContent.CalculatedRiskDefinition, 10, 4, 0, 4, PrintedValueChannel.Initiative, CardTier.II);
            AssertPrint(TakahideCardContent.TheRightHandDefinition, 10, 5, 0, 4, PrintedValueChannel.Attack, CardTier.III);
            AssertPrint(TakahideCardContent.TacticalGambitDefinition, 10, 5, 0, 4, PrintedValueChannel.Initiative, CardTier.III);
            AssertPrint(TakahideCardContent.ComeToAidDefinition, 4, 3, 3, 0, PrintedValueChannel.None, CardTier.I);
            AssertPrint(TakahideCardContent.BringTheReliefDefinition, 3, 4, 4, 0, PrintedValueChannel.Attack, CardTier.II);
            AssertPrint(TakahideCardContent.PledgeOfAllegianceDefinition, 3, 4, 4, 0, PrintedValueChannel.Defense, CardTier.II);
            AssertPrint(TakahideCardContent.CommitReservesDefinition, 3, 4, 4, 0, PrintedValueChannel.Radius, CardTier.III);
            AssertPrint(TakahideCardContent.LoyalRetainerDefinition, 3, 4, 4, 0, PrintedValueChannel.Defense, CardTier.III);

            Assert.That(TakahideCardContent.BlueAndGreenDefinitions.Take(5).Select(card => card.Progression.Color),
                Is.All.EqualTo(CardColor.Blue));
            Assert.That(TakahideCardContent.BlueAndGreenDefinitions.Skip(5).Select(card => card.Progression.Color),
                Is.All.EqualTo(CardColor.Green));
            Assert.That(TakahideCardContent.ProvenWarriorDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { TakahideCardContent.ChosenChampion, TakahideCardContent.CalculatedRisk }));
            foreach (var tierTwo in new[] { TakahideCardContent.ChosenChampionDefinition, TakahideCardContent.CalculatedRiskDefinition })
                Assert.That(tierTwo.Progression.NextTierCandidateIds,
                    Is.EquivalentTo(new[] { TakahideCardContent.TheRightHand, TakahideCardContent.TacticalGambit }),
                    "a Tier II names both Tier III faces of its colour (the Snorri lesson)");
            Assert.That(TakahideCardContent.ComeToAidDefinition.Progression.NextTierCandidateIds,
                Is.EquivalentTo(new[] { TakahideCardContent.BringTheRelief, TakahideCardContent.PledgeOfAllegiance }));
            foreach (var tierTwo in new[] { TakahideCardContent.BringTheReliefDefinition, TakahideCardContent.PledgeOfAllegianceDefinition })
                Assert.That(tierTwo.Progression.NextTierCandidateIds,
                    Is.EquivalentTo(new[] { TakahideCardContent.CommitReserves, TakahideCardContent.LoyalRetainer }));
        }

        private static void AssertPrint(
            CardDefinition card, int initiative, int defense, int range, int radius, PrintedValueChannel item, CardTier tier)
        {
            var primary = card.ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
            int Value(IEnumerable<PrintedValueDefinition> values, PrintedValueChannel channel) =>
                values.Where(value => value.Channel == channel).Select(value => value.Amount).DefaultIfEmpty(0).Single();
            int Secondary(ActionKind kind, PrintedValueChannel channel) =>
                card.ActionDefinitions.Where(action => action.Origin != ActionDefinitionOrigin.PrintedPrimary && action.Kind == kind)
                    .Select(action => Value(action.BaseValues, channel)).DefaultIfEmpty(0).Single();
            Assert.That(primary.Kind, Is.EqualTo(ActionKind.Skill), card.DisplayName + " Skill");
            Assert.That(card.Initiative.Value, Is.EqualTo(initiative), card.DisplayName + " Initiative");
            Assert.That(Secondary(ActionKind.Defense, PrintedValueChannel.Defense), Is.EqualTo(defense), card.DisplayName + " Defense");
            Assert.That(Secondary(ActionKind.Movement, PrintedValueChannel.Movement), Is.EqualTo(2), card.DisplayName + " Movement");
            Assert.That(Value(primary.BaseValues, PrintedValueChannel.Range), Is.EqualTo(range), card.DisplayName + " Range");
            Assert.That(Value(primary.BaseValues, PrintedValueChannel.Radius), Is.EqualTo(radius), card.DisplayName + " Radius");
            Assert.That(card.Progression.ItemIcon, Is.EqualTo(item), card.DisplayName + " item");
            Assert.That(card.Progression.Tier, Is.EqualTo(tier), card.DisplayName + " tier");
        }

        // -------------------------------------------------------------------------------------------
        // The Blues' same-colour discard
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ProvenWarrior_TakahideChoosesATeammatesDiscardedCard_AndTheEnemyDiscardsItsColour()
        {
            var stage = new Stage(TakahideCardContent.ProvenWarrior, allyDiscard: new[] { CardColor.Red, CardColor.Green });
            Assert.That(stage.IsOffered, Is.True);
            stage.StartSkill();
            Assert.That(stage.HeroCandidates, Is.EqualTo(new[] { Stage.Ally }), "a friendly hero, never himself");
            stage.ChooseHero(Stage.Ally);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Taka), "Takahide chooses the card (p.10)");
            Assert.That(stage.CardCandidates, Is.EquivalentTo(new[] { Stage.AllyDiscarded(0), Stage.AllyDiscarded(1) }));
            stage.ChooseCard(Stage.AllyDiscarded(0));
            Assert.That(stage.HeroCandidates, Is.EqualTo(new[] { Stage.Enemy }));
            stage.ChooseHero(Stage.Enemy);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Enemy), "the enemy chooses which card");
            Assert.That(stage.CardCandidates, Is.EqualTo(new[] { Stage.EnemyHand(CardColor.Red) }), "of the same color");
            stage.ChooseCard(Stage.EnemyHand(CardColor.Red));
            Assert.That(stage.Card(Stage.EnemyHand(CardColor.Red)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(stage.Card(Stage.EnemyHand(CardColor.Blue)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(stage.Card(Stage.AllyDiscarded(0)).State.Location, Is.EqualTo(CardLocation.DiscardPile),
                "choosing the teammate's card moves nothing");
        }

        [Test]
        public void ProvenWarrior_AnEnemyWithNoCardOfThatColour_DiscardsNothing()
        {
            var stage = new Stage(TakahideCardContent.ProvenWarrior, allyDiscard: new[] { CardColor.Green });
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.ChooseCard(Stage.AllyDiscarded(0));
            stage.ChooseHero(Stage.Enemy);
            Assert.That(stage.Choice, Is.Null, "\"if able\": nobody is asked");
            Assert.That(stage.Card(Stage.EnemyHand(CardColor.Red)).State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(stage.Card(Stage.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void ProvenWarrior_AFacedownDiscardIsNoChoice_SoTheSkillIsGreyed_TkQ4()
        {
            var facedown = new Stage(TakahideCardContent.ProvenWarrior, allyDiscard: new[] { CardColor.Red },
                allyDiscardFacedown: true);
            Assert.That(CardLifecycleAuthority.IsFacedownRestingCard(facedown.Card(Stage.AllyDiscarded(0))), Is.True);
            Assert.That(facedown.IsOffered, Is.False, "a facedown card has no colour (p.10): nothing to choose");

            var empty = new Stage(TakahideCardContent.ProvenWarrior, allyDiscard: Array.Empty<CardColor>());
            Assert.That(empty.IsOffered, Is.False, "a teammate with an empty discard pile");

            var far = new Stage(TakahideCardContent.ProvenWarrior, allyDiscard: new[] { CardColor.Red },
                allyAt: new HexCoordinate(-4, 0));
            Assert.That(far.IsOffered, Is.False, "radius 3: the teammate four spaces away is out of reach");
            Assert.That(far.Hold(), Is.True, "Hold stays available (Rule 14)");

            var reached = new Stage(TakahideCardContent.ChosenChampion, allyDiscard: new[] { CardColor.Red },
                allyAt: new HexCoordinate(-4, 0));
            Assert.That(reached.IsOffered, Is.True, "Chosen Champion's radius 4 reaches them");
        }

        [Test]
        public void TheRightHand_HitsBothEnemiesInRadius_W3()
        {
            var stage = new Stage(TakahideCardContent.TheRightHand, allyDiscard: new[] { CardColor.Red },
                secondEnemyAt: new HexCoordinate(0, 2));
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.ChooseCard(Stage.AllyDiscarded(0));
            Assert.That(stage.HeroCandidates, Is.EquivalentTo(new[] { Stage.Enemy, Stage.SecondEnemy }));
            Assert.That(stage.Choice.DeclineInvocation, Is.Null, "W3: a hero who is there is not skipped");
            stage.ChooseHero(Stage.SecondEnemy);
            stage.ChooseCard(Stage.SecondEnemyHand(CardColor.Red));
            Assert.That(stage.HeroCandidates, Is.EqualTo(new[] { Stage.Enemy }), "the second is a different hero");
            stage.ChooseHero(Stage.Enemy);
            stage.ChooseCard(Stage.EnemyHand(CardColor.Red));
            Assert.That(stage.Card(Stage.EnemyHand(CardColor.Red)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(stage.Card(Stage.SecondEnemyHand(CardColor.Red)).State.Location, Is.EqualTo(CardLocation.DiscardPile));
        }

        [Test]
        public void TheRightHand_WithNoEnemyInRadius_StillResolves()
        {
            var stage = new Stage(TakahideCardContent.TheRightHand, allyDiscard: new[] { CardColor.Red },
                enemyAt: new HexCoordinate(6, -6));
            Assert.That(stage.IsOffered, Is.True, "\"up to two\": zero enemies is legal");
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.ChooseCard(Stage.AllyDiscarded(0));
            Assert.That(stage.Choice, Is.Null);
            Assert.That(stage.Card(Stage.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        // -------------------------------------------------------------------------------------------
        // Calculated Risk and Tactical Gambit: the teammate's discard and move
        // -------------------------------------------------------------------------------------------

        [Test]
        public void CalculatedRisk_TheTeammateDiscardsAnAttackCard_ThenMovesUpToTwo()
        {
            var stage = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: Array.Empty<CardColor>());
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally), "\"may discard\" is the teammate's");
            Assert.That(stage.CardCandidates, Is.EqualTo(new[] { Stage.AllyAttackCard }), "an attack card (p.9)");
            stage.ChooseCard(Stage.AllyAttackCard);
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally), "\"that hero may move\" (RO-Q2)");
            Assert.That(stage.SpaceCandidates, Has.Member(new HexCoordinate(-5, 0)), "two spaces");
            Assert.That(stage.SpaceCandidates, Has.No.Member(new HexCoordinate(-6, 0)), "not three");
            stage.ChooseSpace(new HexCoordinate(-5, 0));
            Assert.That(stage.Where(Stage.Ally), Is.EqualTo(new HexCoordinate(-5, 0)));
        }

        [Test]
        public void CalculatedRisk_ATeammateWhoDeclinesButHasADiscard_StillMoves()
        {
            var stage = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: new[] { CardColor.Red });
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Card(Stage.AllyAttackCard).State.Location, Is.EqualTo(CardLocation.Hand));
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Ally),
                "\"if that hero has a card in the discard\" reads the pile, not \"if they do\"");
            stage.ChooseSpace(new HexCoordinate(-4, 0));
            Assert.That(stage.Where(Stage.Ally), Is.EqualTo(new HexCoordinate(-4, 0)));
        }

        [Test]
        public void CalculatedRisk_ATeammateWhoDeclinesWithAnEmptyDiscard_DoesNotMove_Ruling53()
        {
            var stage = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: Array.Empty<CardColor>());
            Assert.That(stage.IsOffered, Is.True, "a teammate is in radius: the Skill is offered");
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Choice, Is.Null, "a false printed \"If\" does nothing");
            Assert.That(stage.Where(Stage.Ally), Is.EqualTo(new HexCoordinate(-3, 0)));
            Assert.That(stage.Card(Stage.Played).State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void CalculatedRisk_AFacedownDiscardIsStillACardInTheDiscard()
        {
            var stage = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: new[] { CardColor.Red },
                allyDiscardFacedown: true);
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.SpaceCandidates, Is.Not.Empty, "p.10: a facedown card is still a card there");
        }

        [Test]
        public void TacticalGambit_TheTeammateMovesThroughObstacles()
        {
            var walledIn = AroundTheAlly();
            var plain = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: new[] { CardColor.Red }, obstacles: walledIn);
            plain.StartSkill();
            plain.ChooseHero(Stage.Ally);
            plain.Decline();
            plain.ConfirmIfAsked(Stage.Ally);
            Assert.That(plain.SpaceCandidates, Is.EqualTo(new[] { new HexCoordinate(-3, 0) }),
                "walled in: Calculated Risk's \"up to 2\" can only stay (a move of 0, W5)");

            var stage = new Stage(TakahideCardContent.TacticalGambit, allyDiscard: new[] { CardColor.Red }, obstacles: walledIn);
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.SpaceCandidates, Has.Member(new HexCoordinate(-5, 0)), "\"ignoring obstacles\"");
            Assert.That(stage.SpaceCandidates, Has.No.Member(new HexCoordinate(-4, 0)), "never ending on one (p.18)");
            stage.ChooseSpace(new HexCoordinate(-5, 0));
            Assert.That(stage.Where(Stage.Ally), Is.EqualTo(new HexCoordinate(-5, 0)));
        }

        [Test]
        public void CalculatedRisk_WithNoTeammateInRadius_IsGreyed_TkQ4()
        {
            var stage = new Stage(TakahideCardContent.CalculatedRisk, allyDiscard: new[] { CardColor.Red },
                allyAt: new HexCoordinate(-5, 0));
            Assert.That(stage.IsOffered, Is.False);
            Assert.That(stage.Hold(), Is.True);
        }

        // -------------------------------------------------------------------------------------------
        // The Greens
        // -------------------------------------------------------------------------------------------

        [Test]
        public void ComeToAid_AfterTheTeammatesDiscard_TakahideMovesUpToThree()
        {
            var stage = new Stage(TakahideCardContent.ComeToAid, allyDiscard: Array.Empty<CardColor>());
            stage.StartSkill();
            Assert.That(stage.HeroCandidates, Is.EqualTo(new[] { Stage.Ally }), "in range 3, never himself");
            stage.ChooseHero(Stage.Ally);
            stage.ConfirmIfAsked(Stage.Ally);
            Assert.That(stage.CardCandidates, Is.EquivalentTo(new[] { Stage.AllyAttackCard, Stage.AllySkillCard }), "a card");
            stage.ChooseCard(Stage.AllySkillCard);
            stage.ConfirmIfAsked(Stage.Taka);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Taka), "\"you may move\"");
            Assert.That(stage.SpaceCandidates, Has.Member(new HexCoordinate(0, 3)));
            Assert.That(stage.SpaceCandidates, Has.No.Member(new HexCoordinate(0, 4)));
            stage.ChooseSpace(new HexCoordinate(0, 3));
            Assert.That(stage.Where(Stage.Taka), Is.EqualTo(new HexCoordinate(0, 3)));
        }

        [Test]
        public void BringTheRelief_ReachesRangeFour_AndMovesUpToFour()
        {
            var stage = new Stage(TakahideCardContent.BringTheRelief, allyDiscard: new[] { CardColor.Red },
                allyAt: new HexCoordinate(-4, 0));
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            stage.ConfirmIfAsked(Stage.Taka);
            Assert.That(stage.SpaceCandidates, Has.Member(new HexCoordinate(0, 4)));
        }

        [Test]
        public void CommitReserves_TakahideMovesThroughObstacles()
        {
            var walledIn = Around(new HexCoordinate(0, 0)).Where(space => !space.Equals(new HexCoordinate(1, 0))).ToArray();
            var stage = new Stage(TakahideCardContent.CommitReserves, allyDiscard: new[] { CardColor.Red },
                obstacles: walledIn, enemyAt: new HexCoordinate(1, 0));
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            stage.ConfirmIfAsked(Stage.Taka);
            Assert.That(stage.SpaceCandidates, Has.Member(new HexCoordinate(-2, 0)), "\"ignoring obstacles\"");
        }

        [Test]
        public void PledgeOfAllegiance_BothGainACoin_AndTakahideMayRetrieveHisOwnCard()
        {
            var stage = new Stage(TakahideCardContent.PledgeOfAllegiance, allyDiscard: new[] { CardColor.Red },
                takaDiscarded: true);
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Coins(Stage.Taka), Is.EqualTo(1));
            Assert.That(stage.Coins(Stage.Ally), Is.EqualTo(1), "both you and that hero");
            stage.ConfirmIfAsked(Stage.Taka);
            Assert.That(stage.Choice.ChooserId, Is.EqualTo(Stage.Taka));
            Assert.That(stage.CardCandidates, Is.EqualTo(new[] { Stage.TakaDiscarded }),
                "his own discarded card, never the teammate's (p.10)");
            stage.ChooseCard(Stage.TakaDiscarded);
            Assert.That(stage.Card(Stage.TakaDiscarded).State.Location, Is.EqualTo(CardLocation.Hand));
        }

        [Test]
        public void LoyalRetainer_PaysTwoCoinsEach_AndAFalseCheckPaysNothing()
        {
            var stage = new Stage(TakahideCardContent.LoyalRetainer, allyDiscard: new[] { CardColor.Red });
            stage.StartSkill();
            stage.ChooseHero(Stage.Ally);
            stage.Decline();
            Assert.That(stage.Coins(Stage.Taka), Is.EqualTo(2));
            Assert.That(stage.Coins(Stage.Ally), Is.EqualTo(2));

            var nothing = new Stage(TakahideCardContent.LoyalRetainer, allyDiscard: Array.Empty<CardColor>());
            nothing.StartSkill();
            nothing.ChooseHero(Stage.Ally);
            nothing.Decline();
            Assert.That(nothing.Choice, Is.Null);
            Assert.That(nothing.Coins(Stage.Taka), Is.Zero);
            Assert.That(nothing.Coins(Stage.Ally), Is.Zero);
        }

        [Test]
        public void TheGreens_WithNoTeammateInRange_AreGreyed_TkQ4()
        {
            var stage = new Stage(TakahideCardContent.ComeToAid, allyDiscard: new[] { CardColor.Red },
                allyAt: new HexCoordinate(-4, 0));
            Assert.That(stage.IsOffered, Is.False, "range 3");
            Assert.That(stage.Hold(), Is.True);
        }

        private static HexCoordinate[] AroundTheAlly() => Around(new HexCoordinate(-3, 0));

        private static HexCoordinate[] Around(HexCoordinate centre) =>
            new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, -1), (-1, 1) }
                .Select(offset => new HexCoordinate(centre.Q + offset.Item1, centre.R + offset.Item2)).ToArray();

        // -------------------------------------------------------------------------------------------
        // A staged round: Takahide and his teammate (Blue) against one or two enemies (Red)
        // -------------------------------------------------------------------------------------------

        private sealed class Stage
        {
            internal static readonly HeroId Taka = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
            internal static readonly HeroId Ally = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
            internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.RedHeroIdValue);
            internal static readonly HeroId SecondEnemy = new HeroId("red-second-hero");
            internal static readonly CardInstanceId AllyAttackCard = new CardInstanceId("ally-attack");
            internal static readonly CardInstanceId AllySkillCard = new CardInstanceId("ally-skill");
            internal static readonly CardInstanceId TakaDiscarded = new CardInstanceId("taka-discarded");
            private static readonly CardInstanceId EnemyWall = new CardInstanceId("enemy-wall");

            internal static CardInstanceId AllyDiscarded(int index) => new CardInstanceId("ally-discarded-" + index);
            internal static CardInstanceId EnemyHand(CardColor color) => new CardInstanceId("enemy-" + color);
            internal static CardInstanceId SecondEnemyHand(CardColor color) => new CardInstanceId("second-enemy-" + color);

            private readonly CardActionResolutionAuthority cardActions;

            internal Stage(
                CardDefinitionId played,
                IReadOnlyList<CardColor> allyDiscard,
                bool allyDiscardFacedown = false,
                HexCoordinate? allyAt = null,
                HexCoordinate? enemyAt = null,
                HexCoordinate? secondEnemyAt = null,
                IEnumerable<HexCoordinate> obstacles = null,
                bool takaDiscarded = false)
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                var catalogs = new MatchContentCatalogs(
                    new EffectCapabilityCatalog(defaults.Capabilities.Registered
                        .Select(defaults.Capabilities.Load)),
                    defaults.Components,
                    defaults.TriggeredEffects);
                Definitions = TakahideCardContent.BlueAndGreenDefinitions
                    .Concat(new[] { Plain(1), Plain(2), Plain(5), Wall })
                    .Concat(new[] { CardColor.Red, CardColor.Blue, CardColor.Green }.Select(color => Colored(color, ActionKind.Movement)))
                    .Concat(new[] { Colored(CardColor.Red, ActionKind.Attack) })
                    .ToArray();
                CardDefinition Def(CardDefinitionId id) => Definitions.Single(value => value.Id.Equals(id));
                var cards = new List<CardInstance>
                {
                    CardInstance.CreateInHand(Played, Def(played).Reference, Taka),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-played"), Plain(1).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyWall, Wall.Reference, Enemy),
                    CardInstance.CreateInHand(EnemyHand(CardColor.Red), Colored(CardColor.Red, ActionKind.Movement).Reference, Enemy),
                    CardInstance.CreateInHand(EnemyHand(CardColor.Blue), Colored(CardColor.Blue, ActionKind.Movement).Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("ally-played"), Plain(2).Reference, Ally),
                    CardInstance.CreateInHand(AllyAttackCard, Colored(CardColor.Red, ActionKind.Attack).Reference, Ally),
                    CardInstance.CreateInHand(AllySkillCard, Colored(CardColor.Blue, ActionKind.Movement).Reference, Ally)
                };
                for (var index = 0; index < allyDiscard.Count; index++)
                {
                    var definition = Colored(allyDiscard[index], ActionKind.Movement).Reference;
                    if (allyDiscardFacedown)
                    {
                        // Bushido's shape (9.32a): a discarded card swapped with a deck card, which lands facedown.
                        var deckCard = CardInstance.CreateInDeck(AllyDiscarded(index), definition, Ally);
                        var outgoing = CardInstance.CreateInHand(
                            new CardInstanceId("ally-swapped-away-" + index), definition, Ally);
                        Assert.That(CardLifecycleAuthority.TryDiscardFromHand(outgoing), Is.True);
                        Assert.That(CardLifecycleAuthority.TrySwapWithDeckCard(outgoing, deckCard, true), Is.True);
                        cards.Add(outgoing);
                        cards.Add(deckCard);
                        continue;
                    }

                    var discarded = CardInstance.CreateInHand(AllyDiscarded(index), definition, Ally);
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(discarded), Is.True);

                    cards.Add(discarded);
                }

                if (takaDiscarded)
                {
                    var own = CardInstance.CreateInHand(TakaDiscarded, Colored(CardColor.Green, ActionKind.Movement).Reference, Taka);
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(own), Is.True);
                    cards.Add(own);
                }

                var heroes = new List<Hero>
                {
                    new Hero(Enemy, new PlayerId(StagedBoardParticipants.RedPlayerIdValue), HeroDashboard.RulebookLayout),
                    new Hero(Taka, new PlayerId(StagedBoardParticipants.BluePlayerIdValue), HeroDashboard.RulebookLayout),
                    new Hero(Ally, new PlayerId(StagedBoardParticipants.BlueAllyPlayerIdValue), HeroDashboard.RulebookLayout)
                };
                var players = new List<Player>
                {
                    new Player(new PlayerId(StagedBoardParticipants.RedPlayerIdValue), new TeamId(StagedBoardParticipants.RedTeamIdValue)),
                    new Player(new PlayerId(StagedBoardParticipants.BluePlayerIdValue), new TeamId(StagedBoardParticipants.BlueTeamIdValue)),
                    new Player(new PlayerId(StagedBoardParticipants.BlueAllyPlayerIdValue), new TeamId(StagedBoardParticipants.BlueTeamIdValue))
                };
                var occupancies = new List<HeroOccupancy>
                {
                    new HeroOccupancy(Taka, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, enemyAt ?? new HexCoordinate(1, 0)),
                    new HeroOccupancy(Ally, allyAt ?? new HexCoordinate(-3, 0))
                };
                if (secondEnemyAt.HasValue)
                {
                    heroes.Add(new Hero(SecondEnemy, new PlayerId("red-second-player"), HeroDashboard.RulebookLayout));
                    players.Add(new Player(new PlayerId("red-second-player"), new TeamId(StagedBoardParticipants.RedTeamIdValue)));
                    occupancies.Add(new HeroOccupancy(SecondEnemy, secondEnemyAt.Value));
                    cards.Add(CardInstance.CreateInHand(new CardInstanceId("second-enemy-played"), Plain(5).Reference, SecondEnemy));
                    cards.Add(CardInstance.CreateInHand(
                        SecondEnemyHand(CardColor.Red), Colored(CardColor.Red, ActionKind.Movement).Reference, SecondEnemy));
                }

                Cards = cards.ToArray();
                var participants = new MatchParticipants(
                    new[]
                    {
                        new Team(new TeamId(StagedBoardParticipants.RedTeamIdValue), TeamSide.Red),
                        new Team(new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue)
                    },
                    players,
                    heroes);
                var spaces = (from q in Enumerable.Range(-6, 13)
                              from r in Enumerable.Range(-6, 13)
                              where Math.Abs(q + r) <= 6
                              select new HexCoordinate(q, r)).ToArray();
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("takahide-blue-green"), 1, spaces),
                    occupancies,
                    (obstacles ?? Enumerable.Empty<HexCoordinate>())
                        .Select((space, index) => new ObstacleOccupancy(new ObstacleId("wall-" + index), space))
                        .ToArray());
                var markers = new MarkerAttachmentRegister();
                cardActions = new CardActionResolutionAuthority(catalogs, Board, markers);
                Round = new RoundResolutionAuthority(
                    Definitions, Cards, participants, new TieBreakerState(TeamSide.Blue), 1,
                    new RoundResolutionComposition(cardActions,
                        Board, new ActiveEffectLifecycle(), markers, null, null, null, null, null, null));
                Accept(Round.TryCommitCard(Taka, Played));
                Accept(Round.TryCommitCard(Enemy, new CardInstanceId("enemy-played")));
                Accept(Round.TryCommitCard(Ally, new CardInstanceId("ally-played")));
                if (secondEnemyAt.HasValue)
                    Accept(Round.TryCommitCard(SecondEnemy, new CardInstanceId("second-enemy-played")));
                Assert.That(Turn.SelectedActor.ActorId, Is.EqualTo(Taka), "Takahide's Initiative is the highest");
            }

            internal static readonly CardInstanceId Played = new CardInstanceId("taka-played");
            internal IReadOnlyList<CardDefinition> Definitions { get; }
            internal IReadOnlyList<CardInstance> Cards { get; }
            internal RoundResolutionAuthority Round { get; }
            internal AdjacentMoveAuthority Board { get; }
            internal TurnResolutionAuthority Turn => Round.CurrentTurnAuthority;
            internal PendingEffectChoice Choice => Turn.PendingEffectChoice;

            internal CardInstance Card(CardInstanceId id) => Cards.Single(card => card.Id.Equals(id));

            internal int Coins(HeroId hero) => cardActions.CoinLedger.GetCoins(hero);

            internal HexCoordinate Where(HeroId hero)
            {
                Assert.That(Board.TryGetHeroCoordinate(hero, out var at), Is.True);
                return at;
            }

            internal IReadOnlyList<HeroId> HeroCandidates =>
                Choice.Candidates.Select(candidate => candidate.Value.HeroId).ToArray();

            internal IReadOnlyList<CardInstanceId> CardCandidates =>
                Choice.Candidates.Select(candidate => candidate.Value.CardInstanceId).ToArray();

            internal IReadOnlyList<HexCoordinate> SpaceCandidates =>
                Choice == null
                    ? Array.Empty<HexCoordinate>()
                    : Choice.Candidates.Select(candidate => candidate.Value.Coordinate).ToArray();

            internal bool IsOffered => SkillOffers().Any(offer => offer.IsAvailable);

            /// <summary>Performs Hold; true when it was accepted (Rule 14 leaves it available).</summary>
            internal bool Hold() => Round.TryPerformHold(Played).Accepted;

            internal void StartSkill() =>
                Accept(Round.TryStartSkill(new ActionResolutionId("skill"), SkillOffers().Single(offer => offer.IsAvailable)));

            internal void ChooseHero(HeroId hero) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.HeroId.Equals(hero))));

            internal void ChooseCard(CardInstanceId card) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.CardInstanceId.Equals(card))));

            internal void ChooseSpace(HexCoordinate space) =>
                Accept(Round.TryAnswerEffectChoice(Choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.Value.Coordinate.Equals(space))));

            /// <summary>A "may" may open as its own confirm-or-decline, asked of the hero it names.</summary>
            internal void ConfirmIfAsked(HeroId chooser)
            {
                if (Choice != null && Choice.Kind == EffectChoiceKind.ConfirmOrDecline)
                {
                    Assert.That(Choice.ChooserId, Is.EqualTo(chooser));
                    Accept(Round.TryAnswerEffectChoice(Choice.ConfirmInvocation));
                }
            }

            internal void Decline()
            {
                Assert.That(Choice.ChooserId, Is.EqualTo(Ally), "the teammate's \"may\"");
                Assert.That(Choice.DeclineInvocation, Is.Not.Null);
                Accept(Round.TryAnswerEffectChoice(Choice.DeclineInvocation));
            }

            private IEnumerable<ActionOffer> SkillOffers() =>
                Turn.GetTurnActionOffers().Where(offer =>
                    offer.SourceCardInstanceId.Equals(Played) &&
                    Definitions.Single(definition => definition.Reference.Equals(offer.SourceDefinitionReference))
                        .ActionDefinitions.Single(action => action.Id.Equals(offer.ActionDefinitionId)) is var action &&
                    action.Kind == ActionKind.Skill && action.Origin == ActionDefinitionOrigin.PrintedPrimary);

            private void Accept(RoundResolutionResult result) =>
                Assert.That(result.Accepted, Is.True,
                    result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);

            private static CardDefinition Plain(int initiative) => new CardDefinition(
                new CardDefinitionId("takahide-blue-green-plain-" + initiative), "rev-1", "TEST - Initiative " + initiative,
                new InitiativeValue(initiative), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NotStated,
                new[]
                {
                    new ActionDefinition(new ActionDefinitionId("movement"),
                        ActionKind.Movement, ActionDefinitionOrigin.PrintedSecondary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true))
                });

            /// <summary>A Tier I card of one colour whose printed primary is <paramref name="primary"/>.</summary>
            private static CardDefinition Colored(CardColor color, ActionKind primary) => new CardDefinition(
                new CardDefinitionId("takahide-blue-green-" + color + "-" + primary), "rev-1", "TEST - " + color + " " + primary,
                new InitiativeValue(6), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.NonBasicCard(color, CardTier.I, PrintedValueChannel.None),
                new[]
                {
                    primary == ActionKind.Attack
                        ? new ActionDefinition(new ActionDefinitionId("primary-attack"), ActionKind.Attack,
                            ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 2) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                            AttackRangeKind.NonRanged, default)
                        : new ActionDefinition(new ActionDefinitionId("primary-movement"), ActionKind.Movement,
                            ActionDefinitionOrigin.PrintedPrimary,
                            new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                            new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true))
                });

            private static readonly CardDefinition Wall = new CardDefinition(
                new CardDefinitionId("takahide-blue-green-wall"), "rev-1", "TEST - Wall",
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
