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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsBarrierSkillTests
    {
        private static readonly HeroId Trinkets = new HeroId("barrier-trinkets");
        private static readonly HeroId Ally = new HeroId("barrier-ally");
        private static readonly HeroId Enemy = new HeroId("barrier-enemy");
        private static readonly MapId Map = new MapId("trinkets-barrier-skill");

        [Test]
        public void GridWithEmptyEnemyHand_RequestsDefeatWhereJoltDoesNot()
        {
            var board = Board();
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            var catalog = SyntheticEffectCapabilities.CreateCatalog();
            foreach (var index in new[] { 0, 2 })
            {
                var card = TrinketsCardContent.DisruptorDefinitions[index];
                var effect = TrinketsEffectContent.DisruptorTrigger(
                    card.Id, TrinketsCardContent.ContentRevision, index == 2);
                var context = new EffectResolutionContext(
                    effect, catalog, Participants(), null, 1, 1,
                    RuleModificationSet.Empty,
                    EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                    ActionKind.Skill, opposingHeroId: Enemy,
                    components: components);
                var result = EffectResolutionSession.Start(
                    new EffectResolutionId("empty-hand-" + index),
                    new CardInstanceId("blue-source-" + index), card.Reference,
                    TrinketsCardContent.PrimarySkill, Trinkets, Trinkets,
                    ActionInvocationContext.PrintedPrimary, context);
                Assert.That(result.Accepted, Is.True);
                Assert.That(result.IsComplete, Is.True);
                Assert.That(result.RequestedHeroDefeats.Select(value => value.HeroId),
                    index == 2 ? Is.EqualTo(new[] { Enemy }) : Is.Empty);
            }
        }

        [Test]
        public void DisruptorFaces_ComposeTheirPrintedValuesAndTriggeredEffects()
        {
            var cards = TrinketsCardContent.DisruptorDefinitions;
            Assert.That(cards.Select(card => card.DisplayName), Is.EqualTo(new[]
                { "Disruptor Jolt", "Disruptor Pulse", "Disruptor Grid" }));
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsActiveEffectContent.Handlers());
            var radii = new[] { 3, 4, 4 };
            var defenses = new[] { 3, 4, 4 };
            var movements = new[] { 3, 3, 3 };
            var initiatives = new[] { 10, 10, 11 };
            var items = new[]
            {
                PrintedValueChannel.None,
                PrintedValueChannel.Attack,
                PrintedValueChannel.Movement
            };
            for (var index = 0; index < cards.Count; index++)
            {
                var card = cards[index];
                var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(radii[index]));
                Assert.That(card.Initiative.Value, Is.EqualTo(initiatives[index]));
                Assert.That(card.Progression.ItemIcon, Is.EqualTo(items[index]));
                Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Defense)
                    .BaseValues.Single().Amount, Is.EqualTo(defenses[index]));
                Assert.That(card.ActionDefinitions.Single(action => action.Kind == ActionKind.Movement)
                    .BaseValues.Single().Amount, Is.EqualTo(movements[index]));
                Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot, catalog), Is.Empty);
                var triggered = TrinketsEffectContent.DisruptorTrigger(
                    card.Id, TrinketsCardContent.ContentRevision, index == 2);
                Assert.That(EffectDefinitionValidator.Validate(triggered, catalog), Is.Empty);
            }
        }

        [Test]
        public void DisruptorTrigger_UsesLiveTurretDiskAndEnemyPrimaryOnly()
        {
            var board = Board();
            var participants = Participants();
            var timing = new ActiveEffectLifecycle();
            var definition = TrinketsActiveEffectContent.Jolt;
            var sourceCard = new CardInstanceId("jolt-source");
            var effectId = new ActiveEffectId("jolt-active");
            timing.Create(effectId, definition, Trinkets, TeamSide.Red, default,
                sourceCard, TrinketsCardContent.PrimarySkill, 1, 1);
            var offer = new ActionOffer(
                new CardInstanceId("enemy-action"),
                TrinketsCardContent.DisruptorDefinitions[0].Reference,
                TrinketsCardContent.PrimarySkill, Enemy,
                ActionInvocationContext.PrintedPrimary, true);

            IReadOnlyList<BeforeActionTriggerSource> Ask(
                ActionOffer candidate, HeroItemState items = null) =>
                BeforeActionTriggerQuery.Derive(
                    UnlockedUltimateSet.None, board, participants, candidate,
                    ActionKind.Skill, Array.Empty<string>(), timing, items);

            Assert.That(Ask(offer), Is.Empty, "the absent Turret has no disk");
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(-2, 0)), Is.True);
            Assert.That(Ask(offer).Count, Is.EqualTo(1));
            Assert.That(Ask(offer)[0].ControllerId, Is.EqualTo(Trinkets));
            Assert.That(Ask(offer)[0].Trigger.Frequency,
                Is.EqualTo(BeforeActionTriggerFrequency.EveryAction));
            var secondary = new ActionOffer(offer.SourceCardInstanceId,
                offer.SourceDefinitionReference, offer.ActionDefinitionId, Enemy,
                ActionInvocationContext.PrintedSecondary, true);
            Assert.That(Ask(secondary), Is.Empty);
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(3, 0)), Is.True);
            Assert.That(Ask(offer), Is.Empty, "the old Turret coordinate is not retained");
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(1, 0)), Is.True);
            Assert.That(Ask(offer), Is.Empty, "the printed Radius three ends here");
            var radiusItem = HeroItemState.Restore(participants,
                new HeroItemRestoreState(new[]
                {
                    new HeroItem(Trinkets, new CardInstanceId("radius-item"),
                        HeroDashboard.RulebookLayout.Slots[3].Position,
                        PrintedValueChannel.Radius, 1)
                }));
            Assert.That(Ask(offer, radiusItem).Count, Is.EqualTo(1),
                "the live p.16 Radius item expands the Turret-centred disk");
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(-2, 0)), Is.True);
            timing.CancelOne(effectId, 1, 1);
            Assert.That(Ask(offer), Is.Empty, "deactivated effects do not trigger");
        }

        [Test]
        public void BothBarrierFaces_KeepTheirPrintedValuesAndValidateTheirBodies()
        {
            var cards = TrinketsCardContent.BarrierDefinitions;
            Assert.That(cards, Has.Count.EqualTo(2));
            Assert.That(cards.Select(card => card.DisplayName),
                Is.EqualTo(new[] { "Deployable Barrier", "Deployable Bastion" }));
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.BarrierHandlers());
            foreach (var card in cards)
            {
                var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                Assert.That(skill.BaseValues.Single().Channel, Is.EqualTo(PrintedValueChannel.Radius));
                Assert.That(skill.BaseValues.Single().Amount, Is.EqualTo(3));
                Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot, catalog), Is.Empty,
                    card.DisplayName);
            }
            Assert.That(cards[0].Initiative.Value, Is.EqualTo(10));
            Assert.That(cards[1].Initiative.Value, Is.EqualTo(11));
            Assert.That(cards[0].Progression.Tier, Is.EqualTo(CardTier.II));
            Assert.That(cards[1].Progression.Tier, Is.EqualTo(CardTier.III));
            Assert.That(cards.All(card =>
                card.Progression.ItemIcon == PrintedValueChannel.Initiative), Is.True);
        }

        /// <summary>
        /// Milestone 9.39, ruling W10: "Place up to 3 barrier tokens" is one count. After the mandatory
        /// first Barrier, declining the second "up to" asks no third - before it the third was still
        /// opened - and the Barrier already placed stands.
        /// </summary>
        [Test]
        public void DecliningTheSecondBarrier_AsksNoThird_AndTheFirstStands()
        {
            var board = Board();
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            var card = TrinketsCardContent.BarrierDefinitions[1];
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.BarrierHandlers());
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            EffectResolutionContext Context() => new EffectResolutionContext(
                skill.EffectRoot, catalog, Participants(), null, 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);

            var step = EffectResolutionSession.Start(
                new EffectResolutionId("barrier-w10"), new CardInstanceId("barrier-source"),
                card.Reference, skill.Id, Trinkets, Trinkets,
                ActionInvocationContext.PrintedPrimary, Context());
            step = EffectResolutionSession.Answer(
                step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context());
            step = Select(step, new HexCoordinate(1, 1), Context());
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(1));
            Assert.That(step.Frame.PendingChoice.NodeId.Value, Is.EqualTo("trinkets-barrier-optional-1"));

            step = EffectResolutionSession.Answer(
                step.Frame, step.Frame.PendingChoice.DeclineInvocation, Context());

            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(step.IsComplete, Is.True, "W10: the decline closes the count; no third Barrier is asked.");
            Assert.That(step.Frame?.PendingChoice, Is.Null);
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(1), "the first Barrier stands.");
        }

        [TestCase(0, 2)]
        [TestCase(1, 3)]
        public void FirstBarrierMustAdjoinTurret_ThenUpToPrintedMaximumMayBePlaced(
            int cardIndex, int maximum)
        {
            var board = Board();
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            var card = TrinketsCardContent.BarrierDefinitions[cardIndex];
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.BarrierHandlers());
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            EffectResolutionContext Context() => new EffectResolutionContext(
                skill.EffectRoot, catalog, Participants(), null, 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            EffectResolutionStepResult Start() => EffectResolutionSession.Start(
                new EffectResolutionId("barrier-skill"), new CardInstanceId("barrier-source"),
                card.Reference, skill.Id, Trinkets, Trinkets,
                ActionInvocationContext.PrintedPrimary, Context());

            var withoutTurret = Start();
            Assert.That(withoutTurret.Frame?.PendingChoice, Is.Null,
                "no Turret leaves no mandatory first Barrier destination");
            Assert.That(board.PlacedTokens, Is.Empty);
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(0, 1)), Is.True);
            var zero = Start();
            Assert.That(zero.Frame?.PendingChoice?.DeclineInvocation, Is.Not.Null);
            var declined = EffectResolutionSession.Answer(
                zero.Frame, zero.Frame.PendingChoice.DeclineInvocation, Context());
            Assert.That(declined.Accepted, Is.True);
            Assert.That(board.PlacedTokens, Is.Empty);
            var step = Start();
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            step = EffectResolutionSession.Answer(
                step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context());
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            var first = step.Frame.PendingChoice;
            var adjacent = new HexCoordinate(1, 1);
            Assert.That(first.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Coordinate &&
                candidate.Value.Coordinate.Equals(adjacent)), Is.True);
            Assert.That(first.Candidates.Any(candidate =>
                candidate.Value.Kind == EffectValueKind.Coordinate &&
                candidate.Value.Coordinate.Equals(new HexCoordinate(2, -1))), Is.False);
            step = Select(step, adjacent, Context());
            Assert.That(board.PlacedTokens, Has.Count.EqualTo(1));
            for (var count = 2; count <= maximum; count++)
            {
                Assert.That(step.Frame.PendingChoice, Is.Not.Null);
                step = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, Context());
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
                var placement = count == 2
                    ? new HexCoordinate(2, -1)
                    : new HexCoordinate(1, -1);
                step = Select(step, placement, Context());
                Assert.That(board.PlacedTokens, Has.Count.EqualTo(count));
            }
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(maximum));
            Assert.That(board.PlacedTokens.Any(token =>
                HexDistanceQuery.Calculate(token.Coordinate, new HexCoordinate(0, 1)) == 1), Is.True);
            if (maximum == 3)
            {
                var reclaim = EffectResolutionSession.Start(
                    new EffectResolutionId("barrier-short-supply"),
                    new CardInstanceId("barrier-source"), card.Reference,
                    skill.Id, Trinkets, Trinkets,
                    ActionInvocationContext.PrintedPrimary, Context());
                reclaim = EffectResolutionSession.Answer(
                    reclaim.Frame, reclaim.Frame.PendingChoice.ConfirmInvocation, Context());
                Assert.That(reclaim.Frame.PendingChoice.Candidates.Select(candidate =>
                    candidate.Value.Coordinate), Does.Contain(adjacent));
                reclaim = Select(reclaim, adjacent, Context());
                Assert.That(board.PlacedTokens, Has.Count.EqualTo(2));
                reclaim = Select(reclaim, new HexCoordinate(1, 0), Context());
                Assert.That(board.PlacedTokens, Has.Count.EqualTo(3));
                Assert.That(board.PlacedTokens.Any(token =>
                    token.Coordinate.Equals(new HexCoordinate(1, 0))), Is.True);
            }
        }

        private static EffectResolutionStepResult Select(
            EffectResolutionStepResult current, HexCoordinate at,
            EffectResolutionContext context)
        {
            var pending = current.Frame.PendingChoice;
            var select = pending.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Space(at)));
            var selected = EffectResolutionSession.Answer(current.Frame, select, context);
            Assert.That(selected.Accepted, Is.True, selected.RejectionReason.ToString());
            if (selected.Frame?.PendingChoice != null &&
                selected.Frame.PendingChoice.ChoiceId.Equals(pending.ChoiceId))
            {
                selected = EffectResolutionSession.Answer(
                    selected.Frame, selected.Frame.PendingChoice.ConfirmInvocation, context);
                Assert.That(selected.Accepted, Is.True, selected.RejectionReason.ToString());
            }
            return selected;
        }

        private static AdjacentMoveAuthority Board()
        {
            var spaces = Enumerable.Range(-4, 9)
                .SelectMany(q => Enumerable.Range(-4, 9)
                    .Select(r => new HexCoordinate(q, r))).ToArray();
            return new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(Map, 1, spaces),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Ally, new HexCoordinate(2, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(-3, 0))
                }, Array.Empty<ObstacleOccupancy>());
        }

        private static MatchParticipants Participants() => new MatchParticipants(
            new[]
            {
                new Team(new TeamId("red"), TeamSide.Red),
                new Team(new TeamId("blue"), TeamSide.Blue)
            },
            new[]
            {
                new Player(new PlayerId("owner"), new TeamId("red")),
                new Player(new PlayerId("ally"), new TeamId("red")),
                new Player(new PlayerId("enemy"), new TeamId("blue"))
            },
            new[]
            {
                new Hero(Trinkets, new PlayerId("owner"), HeroDashboard.RulebookLayout),
                new Hero(Ally, new PlayerId("ally"), HeroDashboard.RulebookLayout),
                new Hero(Enemy, new PlayerId("enemy"), HeroDashboard.RulebookLayout)
            });
    }
}
