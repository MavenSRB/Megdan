using System;
using System.IO;
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
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class WidgetPrintedSetTests
    {
        internal static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        internal static HexCoordinate At(int q, int r = 0) => new HexCoordinate(q, r);
        internal static MatchContentCatalogs Catalogs()
        {
            return MatchContentCatalogs.ForThisBuild();
        }

        [Test]
        public void NineteenLiteralRowsMatchEveryIconAndItem_AbsentMovementIsNotZero()
        {
            var expected = new[]
            {
                "Fight as One:11:2:1:4:0:0:None", "Dragon Bond:6:6:0:0:0:2:None",
                "Diversionary Strike:8:6:4:5:0:0:None", "Diversionary Attack:8:6:4:5:0:0:Initiative",
                "Airborne Attack:8:4:4:3:0:3:Defense", "Diversionary Assault:9:7:4:6:0:0:Initiative", "Airborne Assault:9:5:4:4:0:3:Range",
                "All Aboard:9:3:3:0:2:0:None", "Take-Off:10:3:3:0:3:0:Defense", "Drag Off:10:3:3:0:0:0:Attack",
                "Safe Landing:10:4:3:0:3:0:Defense", "Carry Away:10:4:3:0:0:0:Radius",
                "Fiery Breath:4:1:2:0:2:0:None", "Flaming Breath:4:2:2:0:3:0:Attack", "Nibble:4:2:2:0:3:0:Initiative",
                "Scorching Breath:3:2:2:0:3:0:Attack", "Gnaw:3:2:2:0:3:0:Movement", "Follow Along:10:1:2:3:0:0:None"
            };
            Assert.That(WidgetCardContent.AllDefinitions.Where(c => !c.Progression.IsUltimate).Select(c =>
                c.DisplayName + ":" + c.Initiative.Value + ":" + string.Join(":", new[] { PrintedValueChannel.Defense,
                    PrintedValueChannel.Movement, PrintedValueChannel.Attack, PrintedValueChannel.Range, PrintedValueChannel.Radius }
                    .Select(channel => c.ActionDefinitions.SelectMany(a => a.BaseValues).Where(v => v.Channel == channel)
                        .Select(v => v.Amount).DefaultIfEmpty().Single())) + ":" + c.Progression.ItemIcon), Is.EqualTo(expected));
            Assert.That(WidgetCardContent.Definition(WidgetCardContent.DragonBond).ActionDefinitions.Any(a => a.Kind == ActionKind.Movement), Is.False);
            Assert.That(WidgetCardContent.Definition(WidgetCardContent.DragonKnight).ActionDefinitions, Is.Empty);
        }

        [Test]
        public void EveryPrintedRuleMatchesTheSourceFirstLedger_AndEveryTimedRootValidates()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_WIDGET.md"))) directory = directory.Parent;
            Assert.That(directory, Is.Not.Null);
            var lines = File.ReadAllLines(Path.Combine(directory.FullName, "Docs/AI/Rules/HERO_WIDGET.md"));
            var catalogs = Catalogs();
            foreach (var card in WidgetCardContent.AllDefinitions)
            {
                var prefix = "- **" + card.DisplayName + ":** ";
                Assert.That(card.SourceContent.PrintedText, Is.EqualTo(lines.Single(l => l.StartsWith(prefix, StringComparison.Ordinal)).Substring(prefix.Length)), card.Id.Value);
                foreach (var action in card.ActionDefinitions)
                {
                    if (action.EffectRoot != null) Assert.That(EffectDefinitionValidator.Validate(action.EffectRoot, catalogs.Capabilities), Is.Empty, card.Id.Value);
                    if (action.SecondCardTextRoot != null) Assert.That(EffectDefinitionValidator.ValidateCardText(action.SecondCardTextRoot,
                        catalogs.Capabilities, action.SecondCardTextTiming), Is.Empty, card.Id.Value);
                }
            }
            Assert.That(EffectDefinitionValidator.Validate(WidgetEffectContent.DragonKnight(WidgetCardContent.DragonKnight,
                WidgetCardContent.ContentRevision), catalogs.Capabilities), Is.Empty);
        }

        [Test]
        public void SetupUpgradeGraphAndDeferralsMatchThePrintedSet()
        {
            Assert.That(WidgetCardContent.AllDefinitions, Has.Count.EqualTo(19));
            Assert.That(WidgetCardContent.DealtDefinitions, Has.Count.EqualTo(18));
            Assert.That(WidgetCardContent.StartingHand, Is.EqualTo(new[] { WidgetCardContent.FightAsOne, WidgetCardContent.DragonBond,
                WidgetCardContent.DiversionaryStrike, WidgetCardContent.AllAboard, WidgetCardContent.FieryBreath }));
            Assert.That(WidgetCardContent.Deck, Has.Count.EqualTo(13));
            Assert.That(WidgetCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { WidgetCardContent.FollowAlong }));
            Assert.That(WidgetCardContent.CreateHand(Actor, "widget-test").AllInstances, Has.Count.EqualTo(18));
            foreach (var color in new[] { CardColor.Red, CardColor.Blue, CardColor.Green })
            {
                var cards = WidgetCardContent.AllDefinitions.Where(c => c.Progression.Color == color).ToArray();
                Assert.That(cards, Has.Length.EqualTo(5));
                foreach (var card in cards)
                {
                    var next = card.Progression.Tier == CardTier.I ? CardTier.II : CardTier.III;
                    Assert.That(card.Progression.NextTierCandidateIds, Is.EquivalentTo(card.Progression.Tier == CardTier.III
                        ? Array.Empty<CardDefinitionId>() : cards.Where(c => c.Progression.Tier == next).Select(c => c.Id)));
                }
            }
            Assert.That(WidgetCardContent.DealtDefinitions.SelectMany(c => c.ActionDefinitions).All(a => a.TextComposition.Equals(PrintedTextComposition.None)), Is.True);
            Assert.That(WidgetCardContent.Definition(WidgetCardContent.FollowAlong).ActionDefinitions.First().TextComposition.Equals(PrintedTextComposition.None), Is.False);
        }

        [TestCase("airborne-attack", false)]
        [TestCase("airborne-assault", true)]
        public void AirborneSwapsBeforeSelectingTheAttackTarget_ThenOptionallySwapsBack(string face, bool after)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), enemy: At(4), pyro: At(3));
            f.RoundTrip();
            f.Answer(f.Choice.ConfirmInvocation);
            Assert.That(f.Board.TryGetHeroCoordinate(Actor, out var actorAt), Is.True);
            Assert.That(actorAt, Is.EqualTo(At(3)));
            Assert.That(f.Choice.Candidates.Single().Value, Is.EqualTo(EffectValue.Hero(Enemy)));
            f.RoundTrip(); f.Pick(EffectValue.Hero(Enemy)); f.Defend();
            if (after) { f.RoundTrip(); f.Answer(f.Choice.ConfirmInvocation); }
            Assert.That(f.Board.TryGetHeroCoordinate(Actor, out actorAt), Is.True);
            Assert.That(actorAt, Is.EqualTo(after ? At(0) : At(3)));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AirborneMayDeclineOrLackPyro_AndTargetsFromItsUnchangedSpace(bool missing)
        {
            var f = new RoundFixture(WidgetCardContent.AirborneAssault, enemy: At(1), pyro: missing ? (HexCoordinate?)null : At(3));
            if (!missing) f.Answer(f.Choice.DeclineInvocation);
            f.Pick(EffectValue.Hero(Enemy)); f.Defend();
            if (!missing) f.Answer(f.Choice.DeclineInvocation);
            Assert.That(f.Board.TryGetHeroCoordinate(Actor, out var actorAt), Is.True);
            Assert.That(actorAt, Is.EqualTo(At(0)));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void AirborneDoesNotOfferAnAttackWhenNeitherOriginCanReachATarget(int invalidShape)
        {
            new RoundFixture(WidgetCardContent.AirborneAttack, enemy: At(5),
                pyro: invalidShape == 0 ? (HexCoordinate?)null : invalidShape == 1 ? At(4) : At(2), expectStart: false);
        }

        [TestCase("all-aboard")]
        [TestCase("take-off")]
        [TestCase("safe-landing")]
        public void PrintedBlueSwapCardsUseTheActualPrimaryAndKeepTokenIdentity(string face)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), At(-4), At(2));
            if (face == "safe-landing") f.Answer(f.Choice.DeclineInvocation);
            f.Option("self");
            Assert.That(f.Board.TryGetHeroCoordinate(Actor, out var actorAt), Is.True);
            Assert.That(actorAt, Is.EqualTo(At(2)));
            Assert.That(f.Board.PlacedTokens.Single().Id, Is.EqualTo(new TokenInstanceId("pyro")));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(0)));
        }

        [TestCase("drag-off")]
        [TestCase("carry-away")]
        public void PrintedPairMovesCompleteBothSubjectsAndPreserveTheirIdentities(string face)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), At(2), At(1));
            f.Pick(EffectValue.Hero(Enemy)); f.RoundTrip(); f.Pick(EffectValue.Space(At(3)));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(3)));
            Assert.That(f.Board.TryGetHeroCoordinate(Enemy, out var enemyAt), Is.True);
            Assert.That(enemyAt, Is.EqualTo(At(4)));
        }

        [TestCase("fiery-breath")]
        [TestCase("flaming-breath")]
        [TestCase("scorching-breath")]
        public void PrintedBreathsMakeTheSelectedEnemyChooseTheirOwnDiscard(string face)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), At(2), At(3));
            f.Pick(EffectValue.Hero(Enemy));
            Assert.That(f.Choice.ChooserId, Is.EqualTo(Enemy));
            f.RoundTrip(); f.Pick(EffectValue.Card(new CardInstanceId("enemy-defense")));
            Assert.That(f.Cards.Single(c => c.Id.Value == "enemy-defense").State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PrintedDragonBondPlacesOnePyroWithoutInventingAMovementSecondary()
        {
            var f = new RoundFixture(WidgetCardContent.DragonBond, At(-4), null);
            f.Option("place-pyro"); f.RoundTrip(); f.Pick(EffectValue.Space(At(1)));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(1)));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [TestCase("nibble")]
        [TestCase("gnaw")]
        public void PrintedMinionSkillsRemoveTheirSubjectAndThenPyro(string face)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), At(-4), At(1), minion: true);
            f.RoundTrip(); f.Pick(EffectValue.Minion(new MinionId("widget-test-minion")));
            Assert.That(f.Board.Minions, Is.Empty);
            Assert.That(f.Board.PlacedTokens, Is.Empty);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PrintedFightAsOneBorrowsResolvedDragonBondWithoutReplayingItsSource()
        {
            var f = new RoundFixture(WidgetCardContent.FightAsOne, At(1), null, resolved: WidgetCardContent.DragonBond);
            var target = f.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(f.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)).Accepted, Is.True);
            f.Defend(); f.RoundTrip(); f.Pick(EffectValue.Card(new CardInstanceId("resolved")));
            f.Option("place-pyro"); f.Pick(EffectValue.Space(At(-1)));
            Assert.That(f.Cards.Single(c => c.Id.Value == "resolved").State.Location, Is.EqualTo(CardLocation.TurnArea));
            Assert.That(f.Board.PlacedTokens, Has.Count.EqualTo(1));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void PrintedDragonKnightGrantsTheCurrentFaceUpSkillAfterZeroSpaceMovement()
        {
            var f = new RoundFixture(WidgetCardContent.FieryBreath, At(2), At(3), movement: true, knight: true);
            var move = f.Round.CurrentTurnAuthority.PendingMovementChoice;
            Assert.That(f.Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(move.ResolutionId, move.ChoiceId)).Accepted, Is.True);
            f.RoundTrip(); f.Pick(EffectValue.Card(f.Source.Id)); f.Pick(EffectValue.Hero(Enemy));
            f.Pick(EffectValue.Card(new CardInstanceId("enemy-defense")));
            Assert.That(f.Choice, Is.Null);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void DecliningDragonKnightAfterNonBasicMovementReleasesTheHeldTurn_AfterRestore()
        {
            var f = new RoundFixture(WidgetCardContent.FieryBreath, At(2), At(3), movement: true, knight: true);
            var move = f.Round.CurrentTurnAuthority.PendingMovementChoice;
            Assert.That(f.Round.TryAnswerMovementChoice(MovementChoiceCommand.Stop(move.ResolutionId, move.ChoiceId)).Accepted, Is.True);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Unresolved));
            f.RoundTrip();
            var stale = f.Choice.DeclineInvocation;
            f.Answer(stale);
            Assert.That(f.Choice, Is.Null);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            var bytes = AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board);
            Assert.That(f.Round.TryAnswerEffectChoice(stale).Accepted, Is.False);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board), Is.EqualTo(bytes));
        }

        [TestCase("diversionary-strike", 2)]
        [TestCase("diversionary-attack", 3)]
        [TestCase("diversionary-assault", 4)]
        public void DiversionaryRidersUseEachPrintedDistance_AfterDefense(string face, int distance)
        {
            var f = new RoundFixture(new CardDefinitionId("widget-" + face), enemy: At(1), pyro: At(0, 2));
            Assert.That(f.Choice, Is.Null);
            var target = f.Round.CurrentTurnAuthority.PendingAttackTargetChoice;
            Assert.That(f.Round.TryAnswerAttackTarget(new AttackTargetChoiceCommand(target.ResolutionId, target.ChoiceId, Enemy)).Accepted, Is.True);
            f.Defend(); f.RoundTrip();
            Assert.That(f.Choice.Candidates.Any(c => c.Value.Equals(EffectValue.Space(At(0, 2 + distance)))), Is.True);
            Assert.That(f.Choice.Candidates.Any(c => c.Value.Equals(EffectValue.Space(At(0, 3 + distance)))), Is.False);
            f.Pick(EffectValue.Space(At(0, 2 + distance)));
            Assert.That(f.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(0, 2 + distance)));
        }

        internal sealed class RoundFixture
        {
            private static readonly CardDefinition Opponent = new CardDefinition(new CardDefinitionId("widget-opponent-fixture"), "rev-1",
                "TEST - Low initiative", new InitiativeValue(1), CardContentAuthority.SyntheticTestFixture,
                CardProgressionProfile.BasicCard(CardColor.Silver), new[] { new ActionDefinition(new ActionDefinitionId("fixture-move"),
                    ActionKind.Movement, ActionDefinitionOrigin.PrintedPrimary, new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 1) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true)) });
            internal readonly CardDefinition[] Definitions = WidgetCardContent.AllDefinitions.Concat(new[]
                { GarrusCardContent.Definition(GarrusCardContent.SendFlying), Opponent }).ToArray();
            internal readonly MatchContentCatalogs Catalog = Catalogs();
            internal CardInstance[] Cards;
            internal readonly QuickGameMinionSetupState[] Minions;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal CardInstance Source => Cards.Single(c => c.Id.Value == "source");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority?.PendingEffectChoice;
            internal RoundFixture(CardDefinitionId id, HexCoordinate enemy, HexCoordinate? pyro, bool expectStart = true,
                bool minion = false, CardDefinitionId resolved = default, bool movement = false, bool knight = false)
            {
                var card = WidgetCardContent.Definition(id);
                Cards = new[] { CardInstance.CreateInHand(new CardInstanceId("source"), card.Reference, Actor),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-source"), Opponent.Reference, Enemy),
                    CardInstance.CreateInHand(new CardInstanceId("enemy-defense"), GarrusCardContent.Definition(GarrusCardContent.SendFlying).Reference, Enemy) };
                if (resolved.IsDefined) Cards = Cards.Concat(new[] { new CardInstance(new CardInstanceId("resolved"),
                    WidgetCardContent.Definition(resolved).Reference, Actor, new CardStateFacts(CardLocation.TurnArea, true,
                        CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp)) }).ToArray();
                if (knight)
                {
                    var ultimate = CardInstance.CreateInDeck(new CardInstanceId("knight"), WidgetCardContent.Definition(WidgetCardContent.DragonKnight).Reference, Actor);
                    Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                    Cards = Cards.Concat(new[] { ultimate }).ToArray();
                }
                var map = BoardMapDefinition.CreateSynthetic(new MapId("widget-printed-map"), 1,
                    Enumerable.Range(-7, 15).SelectMany(q => Enumerable.Range(-7, 15).Select(r => At(q, r))));
                Minions = minion ? new[] { new QuickGameMinionSetupState(new MinionId("widget-test-minion"),
                    new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-spawn")),
                    new ConfiguredSpaceReference(map.Id, map.Spaces.Single(s => s.Coordinate.Equals(At(2))).Id), At(2)) } : Array.Empty<QuickGameMinionSetupState>();
                Board = new AdjacentMoveAuthority(map, new[] { new HeroOccupancy(Actor, At(0)), new HeroOccupancy(Enemy, enemy) },
                    Array.Empty<ObstacleOccupancy>(), Minions);
                if (pyro.HasValue) Board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("pyro"), WidgetComponentContent.PyroDefinition,
                    Actor, Actor, Source.Id, pyro.Value));
                var participants = StagedBoardParticipants.Create();
                var markers = new MarkerAttachmentRegister();
                Round = new RoundResolutionAuthority(Definitions, Cards, participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForMatch(new CardActionResolutionAuthority(Catalog, Board, markers), Board,
                        new ActiveEffectLifecycle(), markers, null, null, startingCoins: new HeroCoinLedger(participants).CaptureRestoreState()));
                Assert.That(Round.TryCommitCard(Actor, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Enemy, Cards[1].Id).Accepted, Is.True);
                var primary = card.ActionDefinitions.Single(a => movement ? a.Kind == ActionKind.Movement : a.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(Definitions, Cards, Actor, Source.Id).Single(o => o.ActionDefinitionId.Equals(primary.Id));
                var result = movement ? Round.TryStartMovement(new ActionResolutionId("widget-printed"), offer)
                    : primary.Kind == ActionKind.Attack ? Round.TryStartAttack(new ActionResolutionId("widget-printed"), offer)
                    : Round.TryStartSkill(new ActionResolutionId("widget-printed"), offer);
                Assert.That(result.Accepted, Is.EqualTo(expectStart), result.RejectionReason + "/" + result.CardActionRejectionReason);
            }
            internal void Defend()
            {
                var choice = Round.CurrentTurnAuthority.PendingDefenseChoice;
                Assert.That(Round.TryAnswerDefenseResponse(DefenseResponseChoiceCommand.SelectCards(choice.ResolutionId, choice.ChoiceId,
                    new[] { Cards.Single(c => c.Id.Value == "enemy-defense").Id })).Accepted, Is.True);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
            }
            internal void Pick(EffectValue value)
            {
                var choice = Choice;
                Answer(choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(choice.ChoiceId)) Answer(Choice.ConfirmInvocation);
            }
            internal void Option(string id)
            {
                var choice = Choice;
                Answer(choice.SelectInvocations.Single(c => c.Candidate.OptionNodeId.Value == id));
                if (Choice != null && Choice.ChoiceId.Equals(choice.ChoiceId)) Answer(Choice.ConfirmInvocation);
            }
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(Catalog, bytes, Definitions,
                    new[] { Board.MapDefinition }, Minions);
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round; Board = restored.Board; Cards = restored.Cards.ToArray();
            }
        }
    }
}
