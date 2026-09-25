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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Units.Minions;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriAttackTurnTests
    {
        private static readonly HeroId Snorri =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId AdjacentEnemy =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId FarEnemy =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly MapId Map = new MapId("snorri-attack-turn");
        private static readonly MinionId EnemyMinion =
            new MinionId("snorri-battleaxe-repeat-minion");

        [TestCase(0, 2)]
        [TestCase(1, 5)]
        [TestCase(2, 5)]
        [TestCase(3, 4)]
        [TestCase(4, 6)]
        [TestCase(5, 5)]
        public void EveryGoldOrRedFace_ResolvesItsPrintedAttackWithoutARune(
            int faceIndex, int printedAttack)
        {
            var result = Run(faceIndex);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { printedAttack }));
        }

        [Test]
        public void RuneSigilsSecondRuneRaisesTheRealAttackTotalByThree()
        {
            var result = Run(0, runeOrdinal: 2);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 5 }));
        }

        [Test]
        public void RuneSigilsFourthRuneRepeatsOnADifferentHeroInPrintedRange()
        {
            var result = Run(0, runeOrdinal: 4, farDistance: 2);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 2, 2 }),
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void RuneSigilsThirdRuneGrantsThreeCoinsWhenItTargetsAHero()
        {
            var result = Run(0, runeOrdinal: 3);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 2 }));
            Assert.That(result.SnorriCoins, Is.EqualTo(3),
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void RuneblasterSecondRuneDiscardsFromAHeroAdjacentToTheTargetSpace()
        {
            var result = Run(5, runeOrdinal: 2,
                targetAtMaximumRangeWithAdjacentHero: true);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 5 }));
            Assert.That(result.FarEnemyHandCount, Is.EqualTo(
                result.FarEnemyHandCountBeforeAttack - 1),
                string.Join(" | ", result.EffectChoices));
            Assert.That(result.FarEnemyOnBoard, Is.True);
        }

        [Test]
        public void RunecasterSecondRuneUsesTheSameAdjacentSpaceClauseAtMaximumRange()
        {
            var result = Run(3, runeOrdinal: 2,
                targetAtMaximumRangeWithAdjacentHero: true);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 4 }));
            Assert.That(result.FarEnemyHandCount, Is.EqualTo(
                result.FarEnemyHandCountBeforeAttack - 1),
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void RuneblasterSecondRuneDefeatsAnAdjacentEnemyWhoCannotDiscard()
        {
            var result = Run(5, runeOrdinal: 2,
                targetAtMaximumRangeWithAdjacentHero: true,
                emptyAdjacentHeroHand: true);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 5 }));
            Assert.That(result.FarEnemyHandCountBeforeAttack, Is.Zero);
            Assert.That(result.FarEnemyOnBoard, Is.False,
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void RuneblasterSecondRuneHasNoAdjacentHeroToPunish()
        {
            var result = Run(5, runeOrdinal: 2);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 5 }));
            Assert.That(result.FarEnemyOnBoard, Is.True);
        }

        [TestCase(3, 4)]
        [TestCase(5, 5)]
        public void FourthRuneMovesSnorriAfterEitherRangedAttack(
            int faceIndex, int printedAttack)
        {
            var result = Run(faceIndex, runeOrdinal: 4);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { printedAttack }));
            Assert.That(result.SnorriFinalCoordinate,
                Is.Not.EqualTo(new HexCoordinate(0, 0)),
                string.Join(" | ", result.EffectChoices));
        }

        [TestCase(2)]
        [TestCase(4)]
        public void FourthRuneMayMoveOneBeforeHammerOrBattleaxeAttack(int faceIndex)
        {
            var result = Run(faceIndex, runeOrdinal: 4);
            Assert.That(result.AttackTotals, Has.Count.EqualTo(1),
                string.Join(" | ", result.EffectChoices));
            Assert.That(result.SnorriFinalCoordinate,
                Is.EqualTo(new HexCoordinate(0, 1)),
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void BattleaxeSecondRuneRepeatsItsAttackOnAnEnemyMinion()
        {
            var result = Run(4, runeOrdinal: 2, withEnemyMinion: true);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 6 }));
            Assert.That(result.EnemyMinionOnBoard, Is.False,
                string.Join(" | ", result.EffectChoices));
        }

        [Test]
        public void SigilsFourthRuneAttackTargetPauseRestoresAndStillRepeats()
        {
            var ordinary = Run(0, runeOrdinal: 4, farDistance: 2);
            var result = Run(0, runeOrdinal: 4, farDistance: 2,
                restoreAtFirstEffectChoice: true);
            Assert.That(result.RestoredAtEffectChoice, Is.True);
            Assert.That(result.AttackTotals, Is.EqualTo(new[] { 2, 2 }),
                string.Join(" | ", result.EffectChoices));
            Assert.That(result.FinalSnapshot, Is.EqualTo(ordinary.FinalSnapshot),
                "A restored attack pause and a replay from the same pre-answer state " +
                "must produce the same authoritative end state.");
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void ThirdRuneRetrievesAnOwnedDiscardAfterEachApplicableRedAttack(
            int faceIndex)
        {
            var result = Run(faceIndex, runeOrdinal: 3, withDiscard: true);
            Assert.That(result.AttackTotals, Has.Count.EqualTo(1));
            Assert.That(result.RetrievedCardLocation, Is.EqualTo(CardLocation.Hand),
                string.Join(" | ", result.EffectChoices));
        }

        private sealed class TurnRun
        {
            public readonly List<int> AttackTotals = new List<int>();
            public readonly List<string> EffectChoices = new List<string>();
            public CardLocation RetrievedCardLocation;
            public int SnorriCoins;
            public int FarEnemyHandCount;
            public int FarEnemyHandCountBeforeAttack;
            public bool FarEnemyOnBoard;
            public HexCoordinate SnorriFinalCoordinate;
            public bool EnemyMinionOnBoard;
            public bool RestoredAtEffectChoice;
            public string FinalSnapshot;
        }

        private static TurnRun Run(
            int faceIndex, int runeOrdinal = 0, int farDistance = 3,
            bool withDiscard = false,
            bool targetAtMaximumRangeWithAdjacentHero = false,
            bool emptyAdjacentHeroHand = false,
            bool withEnemyMinion = false,
            bool restoreAtFirstEffectChoice = false)
        {
            var configuration = MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(
                TeamSide.Red);
            var face = SnorriAttackFaces.Definitions[faceIndex];
            var attack = face.ActionDefinitions.Single(action => action.Kind == ActionKind.Attack);
            var played = CardInstance.CreateInHand(
                new CardInstanceId("snorri-attack-played"), face.Reference, Snorri);
            var walls = new[] { AdjacentEnemy, FarEnemy }.Select(hero => CardInstance.CreateInHand(
                new CardInstanceId("snorri-defense-wall-" + hero.Value),
                DefenseWall.Reference, hero)).ToArray();
            var definitions = configuration.CardDefinitions.Concat(
                new[] { face, DefenseWall }).ToArray();
            var instances = configuration.CardInstances.Concat(
                new[] { played }).Concat(walls).ToArray();
            var retrievable = withDiscard
                ? configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(Snorri) &&
                    card.State.Location == CardLocation.Hand)
                : null;
            if (retrievable != null)
                Assert.That(CardLifecycleAuthority.TryDiscardFromHand(retrievable), Is.True);
            var farCommitted = configuration.CardInstances.First(card =>
                card.OwnerId.Equals(FarEnemy) &&
                card.State.Location == CardLocation.Hand);
            if (emptyAdjacentHeroHand)
            {
                foreach (var card in instances.Where(card =>
                    card.OwnerId.Equals(FarEnemy) &&
                    card.State.Location == CardLocation.Hand &&
                    !card.Id.Equals(farCommitted.Id)).ToArray())
                {
                    Assert.That(CardLifecycleAuthority.TryDiscardFromHand(card), Is.True);
                }
            }
            var spaces = from q in Enumerable.Range(-4, 9)
                         from r in Enumerable.Range(-4, 9)
                         where Math.Abs(q + r) <= 4
                         select new HexCoordinate(q, r);
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces),
                new[]
                {
                    new HeroOccupancy(Snorri, new HexCoordinate(0, 0)),
                    new HeroOccupancy(AdjacentEnemy,
                        targetAtMaximumRangeWithAdjacentHero
                            ? new HexCoordinate(3, 0) : new HexCoordinate(1, 0)),
                    new HeroOccupancy(FarEnemy,
                        targetAtMaximumRangeWithAdjacentHero
                            ? new HexCoordinate(3, -1)
                            : new HexCoordinate(0, farDistance))
                }, Array.Empty<ObstacleOccupancy>(),
                withEnemyMinion
                    ? new[]
                    {
                        new QuickGameMinionSetupState(
                            EnemyMinion,
                            new TeamId(StagedBoardParticipants.BlueTeamIdValue),
                            TeamSide.Blue, StandardMinionType.Melee,
                            new ConfiguredSpawnPointReference(
                                Map, new SpawnPointId("snorri-repeat-minion-spawn")),
                            new ConfiguredSpaceReference(
                                Map, new SpaceId("q0_r1")),
                            new HexCoordinate(0, 1))
                    }
                    : Array.Empty<QuickGameMinionSetupState>());
            var markerCatalog = new ComponentDefinitionCatalog(
                Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers());
            var catalogs = new MatchContentCatalogs(
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers().Concat(
                        SnorriAttackTargetContent.Handlers())),
                markerCatalog, new TriggeredEffectCatalog(Array.Empty<EffectDefinition>()));
            var markers = new MarkerAttachmentRegister();
            if (runeOrdinal > 0)
                markers.Give(new MarkerAttachment(
                    new MarkerInstanceId("snorri-turn-rune"),
                    SnorriComponentContent.Runes[runeOrdinal - 1],
                    Snorri, Snorri, new CardInstanceId("snorri-inscribe"),
                    SnorriSilverFace.PrimarySkill, 1));
            var cardActions = new CardActionResolutionAuthority(catalogs, board, markers);
            var round = new RoundResolutionAuthority(
                definitions, instances, configuration.Participants,
                new TieBreakerState(TeamSide.Red), 1,
                RoundResolutionComposition.ForMatch(
                    cardActions, board, new ActiveEffectLifecycle(), markers, null, null));
            foreach (var committed in new[]
            {
                played,
                configuration.CardInstances.First(card =>
                    card.OwnerId.Equals(AdjacentEnemy) &&
                    card.State.Location == CardLocation.Hand),
                farCommitted
            })
            {
                Assert.That(round.TryCommitCard(committed.OwnerId, committed.Id).Accepted,
                    Is.True);
            }

            var turn = round.CurrentTurnAuthority;
            Assert.That(turn, Is.Not.Null);
            var run = new TurnRun();
            run.FarEnemyHandCountBeforeAttack = instances.Count(card =>
                card.OwnerId.Equals(FarEnemy) &&
                card.State.Location == CardLocation.Hand);
            var started = false;
            for (var step = 0; step < 100 && !turn.IsEnded; step++)
            {
                if (!started)
                {
                    if (turn.PendingChoice != null)
                    {
                        var tie = turn.PendingChoice;
                        Assert.That(round.TryChooseTiedActor(
                            tie.OwningSide,
                            tie.CandidateCardInstanceIds.Contains(played.Id)
                                ? played.Id : tie.CandidateCardInstanceIds[0]).Accepted, Is.True);
                        continue;
                    }
                    if (turn.PendingTurnRelease != null)
                    {
                        Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                        continue;
                    }
                    if (!turn.SelectedActor.CardInstanceId.IsDefined)
                        break;
                    if (!turn.SelectedActor.CardInstanceId.Equals(played.Id))
                    {
                        Assert.That(round.TryPerformHold(
                            turn.SelectedActor.CardInstanceId).Accepted, Is.True);
                        continue;
                    }

                    var offer = ActionOfferQuery.GetAvailableTurnActionOffers(
                            definitions, instances, Snorri, played.Id)
                        .Single(candidate => candidate.ActionDefinitionId.Equals(attack.Id));
                    Assert.That(round.TryStartAttack(
                        new ActionResolutionId("snorri-attack"), offer).Accepted, Is.True,
                        face.DisplayName);
                    started = true;
                    continue;
                }

                if (turn.PendingAttackTargetChoice != null)
                {
                    var choice = turn.PendingAttackTargetChoice;
                    var target = choice.LegalTargets.FirstOrDefault(value =>
                        value.Kind == AttackTargetKind.Hero &&
                        value.HeroId.Equals(AdjacentEnemy));
                    if (!target.IsDefined)
                        target = choice.LegalTargets.First();
                    Assert.That(round.TryAnswerAttackTarget(
                        new AttackTargetChoiceCommand(
                            choice.ResolutionId, choice.ChoiceId, target)).Accepted,
                        Is.True, face.DisplayName);
                    continue;
                }

                if (turn.PendingEffectChoice != null)
                {
                    if (restoreAtFirstEffectChoice && !run.RestoredAtEffectChoice)
                    {
                        var before = turn.PendingEffectChoice;
                        var snapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
                        var restored = AuthoritativeSnapshotSerializer.Deserialize(
                            catalogs, snapshot, definitions,
                            new[] { board.MapDefinition },
                            Array.Empty<QuickGameMinionSetupState>());
                        Assert.That(AuthoritativeSnapshotSerializer.Serialize(
                            restored.Round, restored.Board), Is.EqualTo(snapshot));
                        round = restored.Round;
                        board = restored.Board;
                        turn = round.CurrentTurnAuthority;
                        Assert.That(turn.PendingEffectChoice.Candidates,
                            Is.EqualTo(before.Candidates));
                        run.RestoredAtEffectChoice = true;
                    }
                    var choice = turn.PendingEffectChoice;
                    run.EffectChoices.Add(choice.NodeId.Value + ":" +
                        string.Join(",", choice.Candidates.Select(
                            candidate => candidate.ToString())));
                    if (choice.SelectedCandidates.Count > 0)
                    {
                        Assert.That(round.TryAnswerEffectChoice(
                            choice.ConfirmInvocation).Accepted, Is.True,
                            face.DisplayName);
                        continue;
                    }
                    var selected = choice.SelectInvocations.FirstOrDefault(invocation =>
                        choice.NodeId.Value == "optional-move-direct" &&
                        invocation.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                        invocation.Candidate.Value.Kind == EffectValueKind.Coordinate &&
                        invocation.Candidate.Value.Coordinate.Equals(
                            new HexCoordinate(0, 1)));
                    if (selected == null)
                        selected = choice.SelectInvocations.FirstOrDefault(invocation =>
                        invocation.Candidate.Kind == EffectChoiceCandidateKind.Value &&
                        invocation.Candidate.Value.Kind == EffectValueKind.Hero &&
                        invocation.Candidate.Value.HeroId.Equals(AdjacentEnemy));
                    if (selected == null)
                        selected = choice.SelectInvocations.FirstOrDefault();
                    Assert.That(round.TryAnswerEffectChoice(
                        selected ?? choice.ConfirmInvocation).Accepted, Is.True,
                        face.DisplayName);
                    continue;
                }

                if (turn.PendingDefenseChoice != null)
                {
                    var defense = turn.PendingDefenseChoice;
                    run.AttackTotals.Add(defense.AttackTotal);
                    var wall = defense.LegalDefenseOffers.Single(offer =>
                        offer.SourceDefinitionReference.Equals(DefenseWall.Reference));
                    var answered = round.TryAnswerDefenseResponse(
                        DefenseResponseChoiceCommand.SelectCards(
                            defense.ResolutionId, defense.ChoiceId,
                            wall.SourceCardInstanceId));
                    run.EffectChoices.Add("defense:" + string.Join(",",
                        answered.Events.Select(value =>
                            value.Kind + "/" + value.TurnEvent.Kind + "/" +
                            value.TurnEvent.CardActionEvent.Kind + "/" +
                            value.TurnEvent.CardActionEvent.StepId.Value)));
                    Assert.That(answered.Accepted, Is.True, face.DisplayName);
                    continue;
                }

                if (turn.PendingTurnRelease != null)
                {
                    Assert.That(round.TryReleaseTurn().Accepted, Is.True);
                    continue;
                }
                break;
            }

            Assert.That(started, Is.True, face.DisplayName);
            Assert.That(turn.PendingAttackResolution, Is.Null,
                face.DisplayName + " left a pending Attack after the scripted decisions.");
            Assert.That(turn.PendingEffectChoice, Is.Null,
                face.DisplayName + " left an unanswered effect choice.");
            run.RetrievedCardLocation = retrievable?.State.Location ?? CardLocation.None;
            run.SnorriCoins = round.CoinBalances.Single(balance =>
                balance.Key.Equals(Snorri)).Value;
            run.FarEnemyHandCount = instances.Count(card =>
                card.OwnerId.Equals(FarEnemy) &&
                card.State.Location == CardLocation.Hand);
            run.FarEnemyOnBoard = board.Occupancies.Any(hero =>
                hero.HeroId.Equals(FarEnemy));
            run.EnemyMinionOnBoard = board.Minions.Any(minion =>
                minion.MinionId.Equals(EnemyMinion));
            Assert.That(board.TryGetObjectCoordinate(
                BoardObjectReference.ForHero(Snorri), out run.SnorriFinalCoordinate),
                Is.True);
            run.FinalSnapshot = AuthoritativeSnapshotSerializer.Serialize(round, board);
            return run;
        }

        private static readonly CardDefinition DefenseWall = new CardDefinition(
            new CardDefinitionId("snorri-defense-wall"), "rev-1",
            "TEST - Snorri defense wall", new InitiativeValue(20),
            CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.NotStated,
            new[]
            {
                new ActionDefinition(
                    new ActionDefinitionId("secondary-defense"),
                    ActionKind.Defense, ActionDefinitionOrigin.Reaction,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Defense, 9) },
                    new ActionDefinitionPolicy(
                        CardTextApplicationPolicy.Suppressed, false, false),
                    AttackRangeKind.None,
                    new ActionCardStateTransition(
                        CardLocation.Hand, CardLocation.DiscardPile))
            });
    }
}
