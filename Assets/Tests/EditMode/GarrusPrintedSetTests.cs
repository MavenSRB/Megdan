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
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class GarrusPrintedSetTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        internal static MatchContentCatalogs Catalogs()
        {
            var defaults = MatchContentCatalogs.ForThisBuild();
            return new MatchContentCatalogs(new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                .Concat(SyntheticDisplacementCapabilities.DeferredHandlers())
                .Concat(SyntheticResolvedDiscardCapabilities.DeferredHandlers())
                .Concat(SyntheticNamedBorrowCapabilities.DeferredHandlers())
                .Concat(GarrusActiveEffectContent.DeferredHandlers())), defaults.Components,
                new TriggeredEffectCatalog(defaults.TriggeredEffects.Definitions.Concat(new[] {
                    GarrusEffectContent.ResolvedDiscardPrimary(GarrusCardContent.BattleFury, GarrusCardContent.ContentRevision) })));
        }

        [Test]
        public void NineteenFaces_ValidateAndPartitionIntoFiveThirteenAndBoxedHandicap()
        {
            Assert.That(GarrusCardContent.AllDefinitions.Count, Is.EqualTo(19));
            Assert.That(GarrusCardContent.DealtDefinitions.Count, Is.EqualTo(18));
            Assert.That(GarrusCardContent.StartingHand.Count, Is.EqualTo(5));
            Assert.That(GarrusCardContent.Deck.Count, Is.EqualTo(13));
            Assert.That(GarrusCardContent.StartingHand.Concat(GarrusCardContent.Deck),
                Is.EquivalentTo(GarrusCardContent.DealtDefinitions.Select(value => value.Id)));
            Assert.That(GarrusCardContent.ReturnedToBoxAtSetup, Is.EqualTo(new[] { GarrusCardContent.AngrySwipe }));
            var catalogs = Catalogs();
            foreach (var card in GarrusCardContent.AllDefinitions)
            foreach (var action in card.ActionDefinitions)
                if (action.EffectRoot != null)
                    Assert.That(EffectDefinitionValidator.Validate(action.EffectRoot, catalogs.Capabilities), Is.Empty, card.Id.Value);
            Assert.That(EffectDefinitionValidator.Validate(GarrusEffectContent.ResolvedDiscardPrimary(
                GarrusCardContent.BattleFury, GarrusCardContent.ContentRevision), catalogs.Capabilities), Is.Empty);
        }

        [Test]
        public void AllPrintedValuesAndItems_MatchTheSourceInventory()
        {
            var expected = new[]
            {
                "Angry Strike:11:3:1:4:0:0:None", "Chilling Howl:13:3:0:0:0:3:None",
                "Trace:7:5:4:3:0:0:None", "Chase:8:5:4:4:0:0:Defense", "Blunt Force:8:7:4:7:0:0:Initiative",
                "Hunt Down:8:6:4:4:0:0:Range", "Send Flying:8:8:4:7:0:0:Radius",
                "Menace:9:6:3:0:2:0:None", "Threaten:10:6:3:0:2:0:Defense", "Form Up!:10:6:3:0:3:0:Attack",
                "Terrify:10:7:3:0:2:0:Defense", "Testudo!:10:7:3:0:3:0:Attack",
                "Hold Ground:5:3:2:0:0:3:None", "Make a Stand:4:4:2:0:0:4:Initiative",
                "Light Pilum:4:4:2:0:2:0:Attack", "Battle Ready:4:4:2:0:0:4:Initiative",
                "Heavy Pilum:4:4:2:0:2:0:Movement", "Angry Swipe:10:2:1:3:0:0:None"
            };
            Assert.That(GarrusCardContent.AllDefinitions.Where(card => !card.Progression.IsUltimate).Select(card =>
                card.DisplayName + ":" + card.Initiative.Value + ":" +
                string.Join(":", new[] { PrintedValueChannel.Defense, PrintedValueChannel.Movement,
                    PrintedValueChannel.Attack, PrintedValueChannel.Range, PrintedValueChannel.Radius }.Select(channel =>
                    card.ActionDefinitions.SelectMany(action => action.BaseValues).Where(value => value.Channel == channel)
                        .Select(value => value.Amount).DefaultIfEmpty().Single())) + ":" + card.Progression.ItemIcon), Is.EqualTo(expected));
            Assert.That(GarrusCardContent.CreateHand(Red, "garrus-probe").AllInstances.Count, Is.EqualTo(18));
        }

        [Test]
        public void Upgrades_PreserveEachPrintedColorAndBothNextTierChoices()
        {
            foreach (var color in new[] { CardColor.Red, CardColor.Blue, CardColor.Green })
            {
                var cards = GarrusCardContent.AllDefinitions.Where(card => card.Progression.Color == color).ToArray();
                Assert.That(cards.Length, Is.EqualTo(5));
                foreach (var card in cards)
                {
                    var next = card.Progression.Tier == CardTier.I ? CardTier.II : CardTier.III;
                    Assert.That(card.Progression.NextTierCandidateIds, Is.EquivalentTo(card.Progression.Tier == CardTier.III
                        ? Array.Empty<CardDefinitionId>() : cards.Where(other => other.Progression.Tier == next).Select(other => other.Id)));
                }
            }
            Assert.That(GarrusCardContent.DealtDefinitions.SelectMany(card => card.ActionDefinitions)
                .All(action => action.TextComposition.Equals(PrintedTextComposition.None)), Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BlueMovement_AdmitsMinionsAsWellAsHeroes(bool friendly)
        {
            var p = new Probe(friendly ? GarrusCardContent.FormUp : GarrusCardContent.Menace, friendly: friendly, minion: true);
            p.Pick(EffectValue.Minion(new MinionId("target-minion")));
            p.Pick(EffectValue.Space(friendly ? new HexCoordinate(1, 0) : new HexCoordinate(2, 1)));
            Assert.That(p.Board.Minions.Single().Coordinate, Is.EqualTo(friendly ? new HexCoordinate(1, 0) : new HexCoordinate(2, 1)));
        }

        [TestCase(RuleActivity.Skill)]
        [TestCase(RuleActivity.DirectMove)]
        public void BlueMovement_DoesNotSelectHeroesImmuneToTheSkillOrItsMove(RuleActivity immunity)
        {
            var p = new Probe(GarrusCardContent.Menace, immunity: immunity);
            Assert.That(p.Choice.Candidates.Any(candidate => candidate.Value.Equals(EffectValue.Hero(Blue))), Is.False);
            Assert.That(p.Choice.Candidates.Any(candidate => candidate.Value.Equals(EffectValue.Hero(Other))), Is.True);
        }

        [Test]
        public void PresenceCountsSkillImmuneHeroes_ButNotMinions()
        {
            Assert.That(new Probe(GarrusCardContent.HoldGround, discards: 1, immunity: RuleActivity.Skill).Choice, Is.Not.Null);
            Assert.That(new Probe(GarrusCardContent.HoldGround, discards: 1, otherSpace: new HexCoordinate(0, 5), minion: true).Step.IsComplete, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BlueMoves_ExactlyOneSpace_InThePrintedDirection_AndNeverSelf(bool friendly)
        {
            var p = new Probe(friendly ? GarrusCardContent.FormUp : GarrusCardContent.Menace, friendly: friendly);
            Assert.That(p.Choice.SelectInvocations.Any(command => command.Candidate.Value.Equals(EffectValue.Hero(p.Actor))), Is.False);
            p.Pick(EffectValue.Hero(friendly ? Other : Blue));
            var origin = friendly ? new HexCoordinate(0, 2) : new HexCoordinate(2, 0);
            Assert.That(p.Choice.SelectInvocations.Select(command => command.Candidate.Value), Has.None.EqualTo(EffectValue.Space(origin)));
            p.Pick(EffectValue.Space(friendly ? new HexCoordinate(0, 1) : new HexCoordinate(3, 0)));
            Assert.That(p.Board.TryGetHeroCoordinate(friendly ? Other : Blue, out var moved), Is.True);
            Assert.That(moved, Is.EqualTo(friendly ? new HexCoordinate(0, 1) : new HexCoordinate(3, 0)));
        }

        [TestCase(0, 2)]
        [TestCase(1, 1)]
        [TestCase(2, 0)]
        public void BattleReady_RetrievesZeroOneOrTwoDistinctDiscards(int count, int remaining)
        {
            var p = new Probe(GarrusCardContent.BattleReady, discards: 2);
            p.Answer(count == 0 ? p.Choice.DeclineInvocation : p.Choice.ConfirmInvocation);
            if (count > 0)
            {
                p.Pick(EffectValue.Card(p.Cards.Single(card => card.Id.Value == "discard-0").Id));
                p.Answer(count == 1 ? p.Choice.DeclineInvocation : p.Choice.ConfirmInvocation);
                if (count == 2) p.Pick(EffectValue.Card(p.Cards.Single(card => card.Id.Value == "discard-1").Id));
            }
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Cards.Count(card => card.State.Location == CardLocation.DiscardPile), Is.EqualTo(remaining));
        }

        [TestCase("hold-ground", 3)]
        [TestCase("make-a-stand", 4)]
        [TestCase("battle-ready", 4)]
        public void Retrieval_RequiresTwoEnemyHeroesInRadius_NotRange(string face, int radius)
        {
            var id = new CardDefinitionId("garrus-" + face);
            var outside = new HexCoordinate(0, radius + 1);
            Assert.That(new Probe(id, discards: 1, otherSpace: outside).Step.IsComplete, Is.True);
            Assert.That(new Probe(id, discards: 1, otherSpace: outside, item: PrintedValueChannel.Range).Step.IsComplete, Is.True);
            var p = new Probe(id, discards: 1, otherSpace: outside, item: PrintedValueChannel.Radius);
            Assert.That(p.Choice, Is.Not.Null);
            p.Answer(p.Choice.ConfirmInvocation);
            p.Pick(EffectValue.Card(new CardInstanceId("discard-0")));
            Assert.That(p.Cards.Single(card => card.Id.Value == "discard-0").State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(new Probe(id, discards: 2, friendly: true).Step.IsComplete, Is.True, "An ally is not a second enemy.");
            Assert.That(new Probe(id).Step.IsComplete, Is.True, "No discard means no pointless optional prompt.");
        }

        [TestCase(false, 0)]
        [TestCase(false, 1)]
        [TestCase(true, 0)]
        [TestCase(true, 1)]
        public void Pilum_DiscardBelongsToOpponent_AndTheMoveSurvivesEitherOutcome(bool heavy, int hand)
        {
            var p = new Probe(heavy ? GarrusCardContent.HeavyPilum : GarrusCardContent.LightPilum, enemyHand: hand);
            p.Pick(EffectValue.Hero(Blue));
            if (hand > 0)
            {
                Assert.That(p.Choice.ChooserId, Is.EqualTo(Blue));
                p.Pick(EffectValue.Card(new CardInstanceId("enemy-hand-0")));
                Assert.That(p.Cards.Single(card => card.Id.Value == "enemy-hand-0").State.Location, Is.EqualTo(CardLocation.DiscardPile));
            }
            Assert.That(p.Choice.NodeId.Value, Is.EqualTo("may-move"));
            Assert.That(p.Step.Frame.CompletedOutcomes.Any(outcome => outcome.NodeId.Value == "defeat"), Is.EqualTo(heavy && hand == 0));
            p.Answer(p.Choice.ConfirmInvocation);
            Assert.That(p.Choice.Candidates.Any(candidate => candidate.Value.Equals(EffectValue.Space(new HexCoordinate(0, 0)))), Is.EqualTo(heavy));
            Assert.That(p.Choice.Candidates.Any(candidate => candidate.Value.Equals(EffectValue.Space(new HexCoordinate(-2, 0)))), Is.EqualTo(heavy));
            p.Pick(EffectValue.Space(new HexCoordinate(heavy ? -2 : -1, 0)));
            Assert.That(p.Step.IsComplete, Is.True);
        }

        [TestCase("menace", false, 2, 0)]
        [TestCase("threaten", false, 2, 1)]
        [TestCase("terrify", false, 2, 2)]
        [TestCase("form-up", true, 3, 1)]
        [TestCase("testudo", true, 3, 2)]
        public void BlueFaces_UseRangeItemsAndTheirPrintedRepeatCount(string face, bool friendly, int range, int repeats)
        {
            var id = new CardDefinitionId("garrus-" + face);
            var p = new Probe(id, friendly: friendly, enemySpace: new HexCoordinate(range + 1, 0),
                otherSpace: new HexCoordinate(0, range + 1), item: PrintedValueChannel.Range);
            var target = friendly ? Other : Blue;
            Assert.That(p.Choice.Candidates.Any(candidate => candidate.Value.Equals(EffectValue.Hero(target))), Is.True);
            p.Pick(EffectValue.Hero(target));
            p.Pick(EffectValue.Space(friendly ? new HexCoordinate(0, range) : new HexCoordinate(range + 2, 0)));
            if (repeats == 0) Assert.That(p.Step.IsComplete, Is.True);
            else
            {
                Assert.That(p.Choice.NodeId.Value, Is.EqualTo("repeat"));
                p.Answer(p.Choice.DeclineInvocation);
                Assert.That(p.Step.IsComplete, Is.True);
            }
            Assert.That(GarrusCardContent.Definition(id).ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary)
                .EffectRoot.Nodes.Where(node => node.Kind == EffectNodeKind.RepeatAction).Select(node => node.MaximumRepeats)
                .DefaultIfEmpty().Single(), Is.EqualTo(repeats));
            var wrongIcon = new Probe(id, friendly: friendly, enemySpace: new HexCoordinate(range + 1, 0),
                otherSpace: new HexCoordinate(0, range + 1), item: PrintedValueChannel.Radius);
            Assert.That(wrongIcon.Step.IsComplete, Is.True);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Howl_OptionalResolvedDiscard_DoesNotGateTheAura(bool discard)
        {
            var p = new Probe(GarrusCardContent.ChillingHowl, resolved: 1);
            p.Answer(discard ? p.Choice.ConfirmInvocation : p.Choice.DeclineInvocation);
            if (discard) p.Pick(EffectValue.Card(new CardInstanceId("resolved-0")));
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Cards.Single(card => card.Id.Value == "resolved-0").State.Location,
                Is.EqualTo(discard ? CardLocation.DiscardPile : CardLocation.TurnArea));
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.True);
            Assert.That(p.Rules.TryGetActivityLimit(RuleObject.ForHero(Blue), RuleActivity.Movement, out var cap), Is.True);
            Assert.That(cap, Is.EqualTo(2));
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Red), RuleActivity.FastTravel), Is.False);
            Assert.That(p.Rules.TryGetActivityLimit(RuleObject.ForHero(Blue), RuleActivity.DirectMove, out _), Is.False);
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.ObjectPush), Is.False);
        }

        [TestCase(PrintedValueChannel.None, false)]
        [TestCase(PrintedValueChannel.Range, false)]
        [TestCase(PrintedValueChannel.Radius, true)]
        public void Howl_LiveRadiusUsesRadiusItem_AndEndsAtRoundEnd(PrintedValueChannel item, bool affected)
        {
            var p = new Probe(GarrusCardContent.ChillingHowl, enemySpace: new HexCoordinate(4, 0), item: item);
            Assert.That(p.Step.IsComplete, Is.True, "No eligible resolved card skips the optional discard.");
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.EqualTo(affected));
            p.Timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(TimingCheckpoint.RoundEnd, 1, 1), new TieBreakerState(TeamSide.Red));
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.False);
        }

        [TestCase(TimingCheckpoint.ControllerHeroDefeated)]
        [TestCase(TimingCheckpoint.CardsRetrieved)]
        public void Howl_CancelsWhenItsControllerOrSourceLeavesPlay(TimingCheckpoint checkpoint)
        {
            var p = new Probe(GarrusCardContent.ChillingHowl);
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.True);
            p.Timing.ApplyCheckpoint(new ActiveEffectCheckpointContext(checkpoint, 1, 1,
                affectedHeroId: Red, sourceCardInstanceId: p.Cards[0].Id), new TieBreakerState(TeamSide.Red));
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.False);
        }

        [Test]
        public void Howl_CancelsImmediatelyOnResolvedSourceDiscard()
        {
            var p = new Probe(GarrusCardContent.ChillingHowl);
            p.Timing.CancelForSourceCardStateChange(p.Cards[0].Id, 1, 1);
            Assert.That(p.Rules.IsRestricted(RuleObject.ForHero(Blue), RuleActivity.FastTravel), Is.False);
        }

        private sealed class Probe
        {
            internal readonly HeroId Actor;
            internal readonly AdjacentMoveAuthority Board;
            internal readonly CardInstance[] Cards;
            internal readonly ActiveEffectLifecycle Timing = new ActiveEffectLifecycle();
            internal readonly MatchParticipants Participants;
            internal readonly HeroItemState Items;
            internal RuleModificationSet Rules => RuleModificationQuery.Build(Timing, Board, Participants,
                new TieBreakerState(TeamSide.Red), UnlockedUltimateSet.None, RuleObject.None, Items, null);
            private readonly EffectResolutionContext context;
            internal EffectResolutionStepResult Step;
            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;
            internal Probe(CardDefinitionId cardId, bool friendly = false, int discards = 0, int enemyHand = 1,
                HexCoordinate? enemySpace = null, HexCoordinate? otherSpace = null, PrintedValueChannel item = PrintedValueChannel.None,
                int resolved = 0, bool minion = false, RuleActivity immunity = RuleActivity.None)
            {
                Actor = friendly ? Blue : Red;
                var participants = StagedBoardParticipants.CreateWithBlueAlly();
                Participants = participants;
                HeroDashboard.RulebookLayout.TryFindSlotAccepting(item == PrintedValueChannel.None ? PrintedValueChannel.Range : item, out var slot);
                Items = HeroItemState.Restore(participants, new HeroItemRestoreState(item == PrintedValueChannel.None
                    ? Array.Empty<HeroItem>() : new[] { new HeroItem(Actor, new CardInstanceId("item"), slot.Position, item, 1) }));
                var card = GarrusCardContent.Definition(cardId);
                var primary = card.ActionDefinitions.Single(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary);
                var source = CardInstance.CreateInHand(new CardInstanceId("source"), card.Reference, Actor);
                CardLifecycleAuthority.TryPlayFacedown(source);
                CardLifecycleAuthority.TryReveal(source);
                var spare = GarrusCardContent.Definition(GarrusCardContent.AngryStrike);
                Cards = new[] { source }.Concat(Enumerable.Range(0, discards).Select(index =>
                    new CardInstance(new CardInstanceId("discard-" + index), spare.Reference, Actor,
                        new CardStateFacts(CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceDown))))
                    .Concat(Enumerable.Range(0, resolved).Select(index => new CardInstance(new CardInstanceId("resolved-" + index), spare.Reference, Actor,
                        new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp))))
                    .Concat(Enumerable.Range(0, enemyHand).Select(index => CardInstance.CreateInHand(new CardInstanceId("enemy-hand-" + index),
                        spare.Reference, friendly ? Red : Blue))).ToArray();
                var mapId = new MapId("garrus-set-probe");
                var minions = minion ? new[] { new QuickGameMinionSetupState(new MinionId("target-minion"),
                    new TeamId(StagedBoardParticipants.BlueTeamIdValue), TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(mapId, new SpawnPointId("spawn")),
                    new ConfiguredSpaceReference(mapId, new SpaceId("q1_r1")), new HexCoordinate(1, 1)) }
                    : Array.Empty<QuickGameMinionSetupState>();
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(mapId, 1,
                    Enumerable.Range(-5, 11).SelectMany(q => Enumerable.Range(-5, 11).Select(r => new HexCoordinate(q, r)))),
                    new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                        new HeroOccupancy(friendly ? Red : Blue, enemySpace ?? new HexCoordinate(2, 0)),
                        new HeroOccupancy(Other, otherSpace ?? new HexCoordinate(0, 2)) }, Array.Empty<ObstacleOccupancy>(), minions);
                if (immunity != RuleActivity.None)
                {
                    var immune = new ActiveEffectDefinition("test-immunity", ActiveEffectAffectedScopeKind.ControllerHero,
                        ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
                        TimingCheckpoint.RoundEnd, new[] { ActiveEffectCancellationCondition.Explicit }, false, "TestImmunity",
                        "test-immunity", Array.Empty<TimingCheckpoint>(), new[] { RuleModification.ForImmunity(immunity) });
                    Timing.Create(new ActiveEffectId("immune"), immune, Blue, TeamSide.Blue, default,
                        new CardInstanceId("immune-source"), primary.Id, 1, 1);
                }
                var catalogs = Catalogs();
                context = new EffectResolutionContext(primary.EffectRoot, catalogs.Capabilities, participants, Timing, 1, 1,
                    Rules, EffectCardState.Of(GarrusCardContent.AllDefinitions, Cards), primary.Kind,
                    components: new MatchComponents(catalogs.Components, Board, new MarkerAttachmentRegister(), Board, Board, Board, Board), heroItems: Items);
                Step = EffectResolutionSession.Start(new EffectResolutionId("garrus-set-resolution"), source.Id, card.Reference,
                    primary.Id, Actor, Actor, ActionInvocationContext.PrintedPrimary, context);
                Assert.That(Step.Accepted, Is.True);
            }
            internal void Pick(EffectValue value)
            {
                var id = Choice.ChoiceId;
                Answer(Choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }
        }
    }
}
