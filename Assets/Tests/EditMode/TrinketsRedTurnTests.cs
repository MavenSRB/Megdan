using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components.Synthetic;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
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
    public sealed class TrinketsRedTurnTests
    {
        private static readonly HeroId Trinkets = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId SecondEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MapId Map = new MapId("trinkets-red-turn-test");

        [TestCase(0, 5)]
        [TestCase(1, 5)]
        [TestCase(2, 4)]
        [TestCase(3, 7)]
        [TestCase(4, 5)]
        public void EveryRedFace_BindsATurretQualifiedTargetAndResolvesItsAttack(int index, int total)
        {
            var run = Run(TrinketsCardContent.RedDefinitions[index].Id);
            Assert.That(run.TargetOffers[0], Does.Contain(Enemy));
            Assert.That(run.AttackTotals[0], Is.EqualTo(total));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SteamAndFlame_RepeatOffersExcludeTheFirstUnit(bool flame)
        {
            var run = Run(flame
                ? TrinketsCardContent.FlameBelcher
                : TrinketsCardContent.SteamDischarge);
            Assert.That(run.TargetOffers, Has.Count.EqualTo(2));
            Assert.That(run.TargetOffers[0], Does.Contain(Enemy));
            Assert.That(run.TargetOffers[0], Does.Contain(SecondEnemy));
            Assert.That(run.TargetOffers[1].Contains(Enemy), Is.False,
                "the repeat cannot choose the first Attack's unit again");
            Assert.That(run.TargetOffers[1], Does.Contain(SecondEnemy));
            Assert.That(run.AttackTotals, Has.Count.EqualTo(2),
                "a third offer cannot recycle either earlier target");
        }

        private sealed class TurnRun
        {
            public List<IReadOnlyList<HeroId>> TargetOffers = new List<IReadOnlyList<HeroId>>();
            public List<int> AttackTotals = new List<int>();
        }

        private static TurnRun Run(CardDefinitionId faceId)
        {
            var configuration = MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                TeamSide.Red);
            var face = TrinketsCardContent.RedDefinitions.Single(card => card.Id.Equals(faceId));
            var attack = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("trinkets-red-played"), face.Reference, Trinkets);
            var walls = new[] { Enemy, SecondEnemy }.Select(hero => CardInstance.CreateInHand(
                new CardInstanceId("trinkets-red-wall-" + hero.Value),
                DefenseWall.Reference, hero)).ToArray();
            var definitions = configuration.CardDefinitions.Concat(new[] { face, DefenseWall }).ToArray();
            var instances = configuration.CardInstances.Concat(new[] { played }).Concat(walls).ToArray();
            var spaces = from q in Enumerable.Range(-5, 11)
                         from r in Enumerable.Range(-5, 11)
                         where Math.Abs(q + r) <= 5
                         select new HexCoordinate(q, r);
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces.ToArray()),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(0, 2)),
                    new HeroOccupancy(SecondEnemy, new HexCoordinate(1, 1))
                }, Array.Empty<ObstacleOccupancy>());
            Assert.That(board.TryPlaceOrRepositionObstacle(
                OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets),
                new HexCoordinate(0, 1)), Is.True);
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    TrinketsComponentContent.ObstacleHandlers()),
                SyntheticComponentDefinitions.CreateCatalog(),
                new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var cardActions = new CardActionResolutionAuthority(catalogs, board);
            foreach (var committed in new[]
            {
                played,
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(Enemy) && card.State.Location == CardLocation.Hand),
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(SecondEnemy) && card.State.Location == CardLocation.Hand)
            })
            {
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(committed), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(committed), Is.True);
            }

            var turn = new TurnResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), cardActions, new ActiveEffectLifecycle(), 1, 1);
            Assert.That(turn.TryStart().Accepted, Is.True);
            var run = new TurnRun();
            var started = false;
            for (var step = 0; step < 80 && !turn.IsEnded; step++)
            {
                if (!started)
                {
                    if (turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        Assert.That(turn.TryChooseTiedActor(tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(played.Id)
                                ? played.Id : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }
                    if (turn.PendingTurnRelease != null)
                    {
                        Assert.That(turn.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }
                    if (!turn.SelectedActor.CardInstanceId.IsDefined) break;
                    if (!turn.SelectedActor.CardInstanceId.Equals(played.Id))
                    {
                        Assert.That(turn.TryPerformHold(turn.SelectedActor.CardInstanceId).Accepted,
                            Is.True);
                        continue;
                    }

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Trinkets, played.Id)
                        .Single(candidate => candidate.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(turn.TryStartAttack(
                        new ActionResolutionId("trinkets-red-attack"), offer).Accepted, Is.True);
                    started = true;
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    var effect = turn.PendingEffectChoice;
                    if (effect.SelectedCandidates.Count > 0)
                    {
                        Assert.That(turn.TryAnswerEffectChoice(effect.ConfirmInvocation).Accepted,
                            Is.True);
                        continue;
                    }
                    var heroes = effect.Candidates
                        .Where(candidate => candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Hero)
                        .Select(candidate => candidate.Value.HeroId).ToArray();
                    if (heroes.Length > 0)
                    {
                        run.TargetOffers.Add(heroes);
                        var index = effect.Candidates.ToList().FindIndex(candidate =>
                            candidate.Kind == EffectChoiceCandidateKind.Value &&
                            candidate.Value.Kind == EffectValueKind.Hero &&
                            candidate.Value.HeroId.Equals(Enemy));
                        if (index < 0) index = 0;
                        Assert.That(turn.TryAnswerEffectChoice(effect.SelectInvocations[index]).Accepted,
                            Is.True);
                    }
                    else
                    {
                        Assert.That(turn.TryAnswerEffectChoice(
                            effect.SelectInvocations.Count > 0
                                ? effect.SelectInvocations[0]
                                : effect.ConfirmInvocation).Accepted, Is.True);
                    }
                    continue;
                }

                if (turn.PendingDefenseChoice != null)
                {
                    var defense = turn.PendingDefenseChoice;
                    run.AttackTotals.Add(defense.AttackTotal);
                    var wall = defense.LegalDefenseOffers.FirstOrDefault(offer =>
                        offer.SourceDefinitionReference.Equals(DefenseWall.Reference));
                    Assert.That(wall.IsAvailable, Is.True,
                        "offers=" + string.Join(",", defense.LegalDefenseOffers.Select(offer =>
                            offer.SourceDefinitionReference.DefinitionId.Value)) +
                        " target-offers=" + string.Join("/", run.TargetOffers.Select(heroes =>
                            string.Join(",", heroes.Select(hero => hero.Value)))));
                    Assert.That(turn.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.SelectCards(
                            defense.ResolutionId, defense.ChoiceId,
                            wall.SourceCardInstanceId)).Accepted, Is.True);
                    continue;
                }

                break;
            }

            Assert.That(started, Is.True);
            return run;
        }

        private static readonly CardDefinition DefenseWall = new CardDefinition(
            new CardDefinitionId("trinkets-red-defense-wall"), "rev-1",
            "TEST - Red defense wall", new InitiativeValue(20),
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
    }
}
