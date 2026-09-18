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
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>HERO_WIDGET WI-04/05: exact Green clauses composed over deferred contracts.</summary>
    public sealed class WidgetAnchoredSkillTests
    {
        internal static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        internal static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        internal static readonly CardDefinitionId Card = new CardDefinitionId("widget-anchored-fixture");
        internal static readonly ActionDefinitionId Action = new ActionDefinitionId("fixture-skill");
        internal static readonly MinionId Minion = new MinionId("widget-minion");
        internal static HexCoordinate At(int q, int r = 0) => new HexCoordinate(q, r);

        [TestCase(2, 0, true)]
        [TestCase(0, 3, false)]
        [TestCase(1, 1, false)]
        [TestCase(-3, 0, false)]
        public void BreathIntersectsWidgetRangeWithPyroLine(int q, int r, bool eligible)
        {
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", false), At(4), At(q, r), range: 2);
            Assert.That(p.Choices.Contains(EffectValue.Hero(Enemy)), Is.EqualTo(eligible));
            Assert.That(p.Board.PlacedTokens.Single().Coordinate, Is.EqualTo(At(4)), "Pyro need not itself be in range.");
        }

        [Test]
        public void BreathUsesRangeItemsAndNoLineOfSight_AndEnemyOwnsDiscard()
        {
            var items = HeroItemState.Restore(StagedBoardParticipants.Create(), new HeroItemRestoreState(new[]
            {
                new HeroItem(Actor, new CardInstanceId("range-item"), HeroDashboard.RulebookLayout.Slots[3].Position,
                    PrintedValueChannel.Range, 1)
            }));
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", false), At(4), At(3), range: 2,
                terrain: new[] { At(1), At(2) }, items: items);
            p.Pick(EffectValue.Hero(Enemy));
            Assert.That(p.Choice.ChooserId, Is.EqualTo(Enemy));
            Assert.That(p.Choices.All(v => p.Cards.Single(c => c.Id.Equals(v.CardInstanceId)).OwnerId.Equals(Enemy)), Is.True);
            p.Pick(p.Choices.First());
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Context.CardState.Instances.Count(c => c.OwnerId.Equals(Enemy) && c.State.Location == CardLocation.DiscardPile), Is.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void EmptyHandDiffersBetweenIfAbleAndOrDefeated(bool scorching)
        {
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", scorching), At(2), At(1), enemyHand: false);
            p.Pick(EffectValue.Hero(Enemy));
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Step.Events.Any(e => e.NodeId.Value == "defeat" && e.ResultKind == EffectExecutionResultKind.Applied), Is.EqualTo(scorching));
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MissingPyroAndSkillImmuneHeroAreNotTargets(bool missing)
        {
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", true), missing ? (HexCoordinate?)null : At(2), At(1),
                rules: Immunity(RuleActivity.Skill));
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Choices, Is.Empty);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BreathOrdinaryAuthorityConsumesOnlyScorchingsEmptyHandDefeat(bool scorching)
        {
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", scorching), At(2), At(1));
            var round = p.AsRound(); // Both sole cards are committed; the target now has no hand card to discard.
            PickRound(round, EffectValue.Hero(Enemy));
            Assert.That(p.Board.Occupancies.Any(h => h.HeroId.Equals(Enemy)), Is.EqualTo(!scorching));
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
        }

        [Test]
        public void NibbleRemovesEnemyMinionThenPyro_NotDefeat()
        {
            var p = new Probe(WidgetEffectContent.Nibble(Card, "rev-1", false), At(1), At(-4), minion: At(2));
            p.Pick(EffectValue.Minion(Minion));
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Board.Minions, Is.Empty);
            Assert.That(p.Board.PlacedTokens, Is.Empty);
            Assert.That(p.Step.Events.Where(e => e.ResultKind == EffectExecutionResultKind.Applied).Select(e => e.NodeId.Value),
                Does.Contain("minion").And.Contain("remove-pyro"));
            Assert.That(p.Step.RequestedMinionDefeats, Is.Empty);
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void GnawAllowsFriendlyMinionButNibbleDoesNot(bool gnaw, bool eligible)
        {
            var p = new Probe(WidgetEffectContent.Nibble(Card, "rev-1", gnaw), At(1), At(-4), minion: At(2), friendly: true);
            Assert.That(p.Choices.Contains(EffectValue.Minion(Minion)), Is.EqualTo(eligible));
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
        }

        [TestCase(4, 0)]
        [TestCase(0, 2)]
        public void ImpossibleMandatoryMinionStepLeavesPyro(int q, int r)
        {
            var p = new Probe(WidgetEffectContent.Nibble(Card, "rev-1", false), At(3), At(-4), minion: At(q, r), range: 3);
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
            Assert.That(p.Board.Minions, Has.Count.EqualTo(1));
        }

        [Test]
        public void StaleMinionSelectionCannotRemovePyroOrTheNowDistantMinion()
        {
            var p = new Probe(WidgetEffectContent.Nibble(Card, "rev-1", false), At(1), At(-4), minion: At(2));
            var command = p.Choice.SelectInvocations.Single();
            Assert.That(p.Board.CommitValidatedDisplacement(BoardObjectReference.ForMinion(Minion), At(5)).Accepted, Is.True);
            p.Answer(command);
            if (p.Choice != null) p.Answer(p.Choice.ConfirmInvocation);
            Assert.That(p.Step.IsComplete, Is.True);
            Assert.That(p.Board.PlacedTokens, Has.Count.EqualTo(1));
            Assert.That(p.Board.Minions.Single().Coordinate, Is.EqualTo(At(5)));
        }

        [Test]
        public void EnemyDiscardPauseRestoresWithSameChooserAndResolvesOnce()
        {
            var p = new Probe(WidgetEffectContent.Breath(Card, "rev-1", true), At(2), At(1), enemySpare: true);
            var round = p.AsRound();
            PickRound(round, EffectValue.Hero(Enemy));
            Assert.That(round.CurrentTurnAuthority.PendingEffectChoice.ChooserId, Is.EqualTo(Enemy));
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, p.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(p.Catalogs, payload, p.Definitions,
                new[] { p.Board.MapDefinition }, p.Minions);
            foreach (var branch in new[] { round, restored.Round })
                PickRound(branch, EffectValue.Card(new CardInstanceId("blue-spare")));
            Assert.That(p.Board.Occupancies.Any(h => h.HeroId.Equals(Enemy)), Is.True);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, p.Board)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void GreenSelectionRestoresAndResolvesOrdinaryMinionConsequences(bool gnaw)
        {
            var p = new Probe(WidgetEffectContent.Nibble(Card, "rev-1", gnaw), At(1), At(-4), minion: At(2));
            var round = p.AsRound();
            var coinsBefore = round.CoinBalances.Single(c => c.Key.Equals(Actor)).Value;
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, p.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(p.Catalogs, payload, p.Definitions,
                new[] { p.Board.MapDefinition }, p.Minions);
            foreach (var branch in new[] { round, restored.Round }) PickRound(branch, EffectValue.Minion(Minion));
            Assert.That(p.Board.Minions, Is.Empty, "The card authority must consume Gnaw's staged Defeat before continuing.");
            Assert.That(p.Board.PlacedTokens, Is.Empty);
            Assert.That(round.CoinBalances.Single(c => c.Key.Equals(Actor)).Value > coinsBefore, Is.EqualTo(gnaw),
                "Only Defeat awards a reward; Remove does not.");
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, p.Board)));
        }

        internal static RuleModificationSet Immunity(RuleActivity activity) => new RuleModificationSet(new[]
        {
            AppliedRuleModification.FromActiveEffect(RuleModification.ForImmunity(activity), RuleObject.ForHero(Enemy),
                new ActiveEffectId("widget-test-immunity"), Enemy, TeamSide.Blue, 1)
        }, Array.Empty<AppliedObstacleOverlay>(), new TieBreakerState(TeamSide.Red));

        internal static void PickRound(RoundResolutionAuthority round, EffectValue value)
        {
            var pending = round.CurrentTurnAuthority.PendingEffectChoice;
            Assert.That(round.TryAnswerEffectChoice(pending.SelectInvocations.Single(c => c.Candidate.Value.Equals(value))).Accepted, Is.True);
            if (round.CurrentTurnAuthority?.PendingEffectChoice?.ChoiceId.Equals(pending.ChoiceId) == true)
                Assert.That(round.TryAnswerEffectChoice(round.CurrentTurnAuthority.PendingEffectChoice.ConfirmInvocation).Accepted, Is.True);
        }

        internal sealed class Probe
        {
            internal readonly AdjacentMoveAuthority Board;
            internal readonly MatchContentCatalogs Catalogs;
            internal readonly CardDefinition[] Definitions;
            internal readonly CardInstance[] Cards;
            internal readonly QuickGameMinionSetupState[] Minions;
            internal readonly MatchParticipants Participants = StagedBoardParticipants.Create();
            internal readonly EffectResolutionContext Context;
            internal EffectResolutionStepResult Step;
            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;
            internal EffectValue[] Choices => Choice?.Candidates.Select(c => c.Value).ToArray() ?? Array.Empty<EffectValue>();

            internal Probe(EffectDefinition effect, HexCoordinate? pyro, HexCoordinate enemy, int range = 3,
                HexCoordinate? minion = null, bool friendly = false, bool enemyHand = true,
                IEnumerable<HexCoordinate> terrain = null, RuleModificationSet rules = null, HeroItemState items = null,
                BoardMapDefinition map = null, IEnumerable<QuickGameMinionSetupState> minions = null, bool enemySpare = false)
            {
                var defaults = MatchContentCatalogs.ForThisBuild();
                var caps = new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(WidgetComponentContent.DeferredHandlers()).Concat(SyntheticCoupledMoveCapabilities.DeferredHandlers()));
                var components = new ComponentDefinitionCatalog(new[] { WidgetComponentContent.PyroDefinition }, Array.Empty<MarkerDefinition>());
                Catalogs = new MatchContentCatalogs(caps, components);
                Assert.That(EffectDefinitionValidator.Validate(effect, caps), Is.Empty);
                var action = new ActionDefinition(Action, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Range, range) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default, effect);
                var definition = new CardDefinition(Card, "rev-1", "TEST - Widget anchored clause", new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[] { action });
                Definitions = new[] { definition };
                Cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("red"), definition.Reference, Actor),
                    new CardInstance(new CardInstanceId("blue"), definition.Reference, Enemy, enemyHand ? CardStateFacts.InitialInHand() :
                        new CardStateFacts(CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp))
                }.Concat(enemySpare ? new[] { CardInstance.CreateInHand(new CardInstanceId("blue-spare"), definition.Reference, Enemy) }
                    : Array.Empty<CardInstance>()).ToArray();
                var terrainArray = (terrain ?? Array.Empty<HexCoordinate>()).ToArray();
                map = map ?? BoardMapDefinition.CreateSynthetic(new MapId("widget-anchor-map"), 1,
                    Enumerable.Range(-6, 13).SelectMany(q => Enumerable.Range(-6, 13).Select(r => At(q, r))).Except(terrainArray), terrainArray);
                Minions = minions?.ToArray() ?? (minion.HasValue ? new[] { new QuickGameMinionSetupState(Minion,
                    new TeamId(friendly ? StagedBoardParticipants.RedTeamIdValue : StagedBoardParticipants.BlueTeamIdValue),
                    friendly ? TeamSide.Red : TeamSide.Blue, StandardMinionType.Melee,
                    new ConfiguredSpawnPointReference(map.Id, new SpawnPointId("fixture-spawn")),
                    new ConfiguredSpaceReference(map.Id, map.Spaces.Single(s => s.Coordinate.Equals(minion.Value)).Id), minion.Value) }
                    : Array.Empty<QuickGameMinionSetupState>());
                Board = new AdjacentMoveAuthority(map, new[] { new HeroOccupancy(Actor, At(0)), new HeroOccupancy(Enemy, enemy) },
                    Array.Empty<ObstacleOccupancy>(), Minions);
                if (pyro.HasValue) Board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("pyro"),
                    WidgetComponentContent.PyroDefinition, Actor, Actor, Cards[0].Id, pyro.Value));
                Context = new EffectResolutionContext(effect, caps, Participants, null, 1, 1, rules ?? RuleModificationSet.Empty,
                    EffectCardState.Of(Definitions, Cards), ActionKind.Skill, components: new MatchComponents(components,
                        Board, new MarkerAttachmentRegister(), Board, Board, Board, Board), heroItems: items);
                Step = EffectResolutionSession.Start(new EffectResolutionId("widget-anchor-resolution"), Cards[0].Id,
                    definition.Reference, Action, Actor, Actor, ActionInvocationContext.PrintedPrimary, Context);
                Assert.That(Step.Accepted, Is.True);
            }

            internal void Pick(EffectValue value)
            {
                var id = Choice.ChoiceId;
                Answer(Choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(id)) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                Step = EffectResolutionSession.Answer(Step.Frame, command, Context);
                Assert.That(Step.Accepted, Is.True, Step.RejectionReason.ToString());
            }
            internal RoundResolutionAuthority AsRound()
            {
                var ledger = new HeroCoinLedger(Participants);
                var actions = CardActionResolutionAuthority.Restore(Catalogs, Board,
                    CardActionResolutionRestoreState.WithPendingSkill(Step.Frame,
                        HeroCoinLedgerRestoreStateHolder.Present(ledger.CaptureRestoreState())),
                    null, null, null, Definitions, Cards, Participants, new MarkerAttachmentRegister(), RuleModificationSet.Empty);
                var round = new RoundResolutionAuthority(Definitions, Cards, Participants, new TieBreakerState(TeamSide.Red), 1,
                    RoundResolutionComposition.ForCardActionsOnly(actions));
                foreach (var card in Cards.Take(2)) Assert.That(round.TryCommitCard(card.OwnerId, card.Id).Accepted, Is.True);
                return round;
            }
        }
    }
}
