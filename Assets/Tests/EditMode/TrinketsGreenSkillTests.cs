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
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsGreenSkillTests
    {
        private static readonly HeroId Trinkets = new HeroId("trinkets-green");
        private static readonly HeroId Enemy = new HeroId("enemy-green");
        private static readonly HeroId EnemyTwo = new HeroId("enemy-two-green");

        [Test]
        public void AllGreenFaces_KeepPrintedValuesAndValidateTheirBodies()
        {
            var cards = TrinketsCardContent.GreenDefinitions;
            Assert.That(cards.Select(card => card.DisplayName), Is.EqualTo(new[]
            {
                "Early Prototype", "Updated Design", "Self-Destruct",
                "Perfected Design", "Emergency Protocol"
            }));
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            foreach (var card in cards)
            {
                var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
                Assert.That(EffectDefinitionValidator.Validate(skill.EffectRoot, catalog), Is.Empty,
                    card.DisplayName);
            }
            Assert.That(cards.Select(card => card.Initiative.Value),
                Is.EqualTo(new[] { 3, 3, 3, 2, 2 }));
            Assert.That(cards.Select(card => card.Progression.ItemIcon),
                Is.EqualTo(new[]
                {
                    PrintedValueChannel.None, PrintedValueChannel.Attack,
                    PrintedValueChannel.Defense, PrintedValueChannel.Attack,
                    PrintedValueChannel.Range
                }));
        }

        [TestCase(0, false, false)]
        [TestCase(1, true, false)]
        [TestCase(3, true, false)]
        [TestCase(0, false, true)]
        public void PrototypeSwap_UsesTurretRadiusAndPrintedRemoval(
            int index, bool turretRemains, bool selectToken)
        {
            var spaces = Enumerable.Range(-4, 9).SelectMany(q =>
                Enumerable.Range(-4, 9).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("trinkets-green"), 1, spaces),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(0, 2)),
                    new HeroOccupancy(EnemyTwo, new HexCoordinate(1, 2))
                }, Array.Empty<ObstacleOccupancy>());
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            var targetAt = selectToken
                ? new HexCoordinate(1, 1) : new HexCoordinate(0, 2);
            if (selectToken)
                board.CommitValidatedTokenPlacement(new PlacedToken(
                    new TokenInstanceId("green-swap-barrier"),
                    TrinketsComponentContent.Barrier,
                    Trinkets, Trinkets, new CardInstanceId("green-token-source"), targetAt));
            var participants = new MatchParticipants(
                new[]
                {
                    new Team(new TeamId("red"), TeamSide.Red),
                    new Team(new TeamId("blue"), TeamSide.Blue)
                },
                new[]
                {
                    new Player(new PlayerId("owner"), new TeamId("red")),
                    new Player(new PlayerId("enemy"), new TeamId("blue")),
                    new Player(new PlayerId("enemy-two"), new TeamId("blue"))
                },
                new[]
                {
                    new Hero(Trinkets, new PlayerId("owner"), HeroDashboard.RulebookLayout),
                    new Hero(Enemy, new PlayerId("enemy"), HeroDashboard.RulebookLayout),
                    new Hero(EnemyTwo, new PlayerId("enemy-two"), HeroDashboard.RulebookLayout)
                });
            var card = TrinketsCardContent.GreenDefinitions[index];
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            var context = new EffectResolutionContext(
                skill.EffectRoot, catalog, participants, null, 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            if (index == 0)
            {
                var immune = new RuleModificationSet(
                    new[]
                    {
                        AppliedRuleModification.FromActiveEffect(
                            RuleModification.ForImmunity(RuleActivity.Skill),
                            RuleObject.ForHero(Enemy),
                            new ActiveEffectId("green-immunity"), Enemy,
                            TeamSide.Blue, 0)
                    }, Array.Empty<AppliedObstacleOverlay>(), TieBreakerState.Unrevealed);
                var immuneContext = new EffectResolutionContext(
                    skill.EffectRoot, catalog, participants, null, 1, 1,
                    immune,
                    EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                    ActionKind.Skill, components: components);
                var immuneStart = EffectResolutionSession.Start(
                    new EffectResolutionId("green-immune"),
                    new CardInstanceId("green-source"), card.Reference, skill.Id,
                    Trinkets, Trinkets, ActionInvocationContext.PrintedPrimary,
                    immuneContext);
                Assert.That(immuneStart.Frame.PendingChoice.Candidates.Any(candidate =>
                    candidate.Value.Equals(EffectValue.Space(new HexCoordinate(0, 2)))),
                    Is.False, "the enemy's Skill immunity excludes it from the swap offer");
            }
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("green-swap"), new CardInstanceId("green-source"),
                card.Reference, skill.Id, Trinkets, Trinkets,
                ActionInvocationContext.PrintedPrimary, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(step.Frame?.PendingChoice, Is.Not.Null);
            if (index == 3)
            {
                var mode = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                    command.Candidate.OptionNodeId.Equals(new EffectNodeId("swap-arm")));
                step = EffectResolutionSession.Answer(step.Frame, mode, context);
                Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
                if (step.Frame?.PendingChoice?.ConfirmInvocation != null)
                {
                    step = EffectResolutionSession.Answer(
                        step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
                    Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
                }
            }
            var selected = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Space(targetAt)));
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(3, 3)), Is.True);
            var stale = EffectResolutionSession.Answer(step.Frame, selected, context);
            if (stale.Frame?.PendingChoice?.ConfirmInvocation != null)
                stale = EffectResolutionSession.Answer(
                    stale.Frame, stale.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var unchanged), Is.True);
            Assert.That(unchanged, Is.EqualTo(new HexCoordinate(0, 0)),
                "a Turret moved after the offer must not apply the old Swap answer");
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(0, 1)), Is.True);
            step = EffectResolutionSession.Answer(step.Frame, selected, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            if (step.Frame?.PendingChoice != null)
                step = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var at), Is.True);
            Assert.That(at, Is.EqualTo(targetAt));
            if (selectToken)
                Assert.That(board.PlacedTokens.Single().Coordinate,
                    Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(board.TryGetObstacleCoordinate(turret, out _), Is.EqualTo(turretRemains));
        }

        [TestCase(2, false)]
        [TestCase(4, true)]
        public void Detonation_EmptyHandDiscardsIfAbleOrRequestsDefeatAndRemovesTurret(
            int index, bool defeats)
        {
            var spaces = Enumerable.Range(-4, 9).SelectMany(q =>
                Enumerable.Range(-4, 9).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("trinkets-detonation"), 1, spaces),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(0, 2)),
                    new HeroOccupancy(EnemyTwo, new HexCoordinate(1, 2))
                }, Array.Empty<ObstacleOccupancy>());
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            var participants = new MatchParticipants(
                new[]
                {
                    new Team(new TeamId("red"), TeamSide.Red),
                    new Team(new TeamId("blue"), TeamSide.Blue)
                },
                new[]
                {
                    new Player(new PlayerId("owner"), new TeamId("red")),
                    new Player(new PlayerId("enemy"), new TeamId("blue")),
                    new Player(new PlayerId("enemy-two"), new TeamId("blue"))
                },
                new[]
                {
                    new Hero(Trinkets, new PlayerId("owner"), HeroDashboard.RulebookLayout),
                    new Hero(Enemy, new PlayerId("enemy"), HeroDashboard.RulebookLayout),
                    new Hero(EnemyTwo, new PlayerId("enemy-two"), HeroDashboard.RulebookLayout)
                });
            var card = TrinketsCardContent.GreenDefinitions[index];
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(Array.Empty<TokenDefinition>(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            var context = new EffectResolutionContext(
                skill.EffectRoot, catalog, participants, null, 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(4, 4)), Is.True);
            var noTarget = EffectResolutionSession.Start(
                new EffectResolutionId("detonation-no-target"),
                new CardInstanceId("green-source"), card.Reference, skill.Id,
                Trinkets, Trinkets, ActionInvocationContext.PrintedPrimary, context);
            if (noTarget.Frame?.PendingChoice?.DeclineInvocation != null)
                noTarget = EffectResolutionSession.Answer(
                    noTarget.Frame, noTarget.Frame.PendingChoice.DeclineInvocation, context);
            Assert.That(noTarget.Accepted, Is.True, noTarget.RejectionReason.ToString());
            Assert.That(board.TryGetObstacleCoordinate(turret, out _), Is.False,
                "a zero-candidate detonation still removes its Turret");
            Assert.That(board.TryPlaceOrRepositionObstacle(
                turret, new HexCoordinate(0, 1)), Is.True);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("detonation"), new CardInstanceId("green-source"),
                card.Reference, skill.Id, Trinkets, Trinkets,
                ActionInvocationContext.PrintedPrimary, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            // Milestone 9.1, ruling W3: "Up to two enemy heroes ... discard" hits the heroes present,
            // so the first question is WHICH hero, and it cannot be declined.
            Assert.That(step.Frame?.PendingChoice, Is.Not.Null);
            Assert.That(step.Frame.PendingChoice.DeclineInvocation, Is.Null,
                "W3: a hero in the Turret's radius is hit; the step is not Trinkets' to skip.");
            var select = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Hero(Enemy)));
            var requested = new List<HeroId>();
            step = EffectResolutionSession.Answer(step.Frame, select, context);
            requested.AddRange(step.RequestedHeroDefeats.Select(value => value.HeroId));
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            if (step.Frame?.PendingChoice?.ConfirmInvocation != null &&
                step.Frame.PendingChoice.SelectInvocations.Any(command =>
                    command.Candidate.Value.Equals(EffectValue.Hero(Enemy))))
                step = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            requested.AddRange(step.RequestedHeroDefeats.Select(value => value.HeroId));
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(step.Frame?.PendingChoice, Is.Not.Null,
                "W3: the second enemy hero present is hit too.");
            Assert.That(step.Frame.PendingChoice.DeclineInvocation, Is.Null);
            Assert.That(step.Frame.PendingChoice.SelectInvocations.Any(command =>
                command.Candidate.Value.Equals(EffectValue.Hero(Enemy))), Is.False);
            var second = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Hero(EnemyTwo)));
            step = EffectResolutionSession.Answer(step.Frame, second, context);
            requested.AddRange(step.RequestedHeroDefeats.Select(value => value.HeroId));
            if (step.Frame?.PendingChoice?.ConfirmInvocation != null &&
                step.Frame.PendingChoice.SelectInvocations.Any(command =>
                    command.Candidate.Value.Equals(EffectValue.Hero(EnemyTwo))))
                step = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            requested.AddRange(step.RequestedHeroDefeats.Select(value => value.HeroId));
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(requested,
                defeats ? Does.Contain(Enemy) : Is.Empty);
            if (defeats) Assert.That(requested, Does.Contain(EnemyTwo));
            Assert.That(board.TryGetObstacleCoordinate(turret, out _), Is.False);
        }

        [Test]
        public void PerfectedDesign_PlacesSelfInLiveTurretRadiusWithoutRemovingIt()
        {
            var spaces = Enumerable.Range(-4, 9).SelectMany(q =>
                Enumerable.Range(-4, 9).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(
                BoardMapDefinition.CreateSynthetic(new MapId("trinkets-perfect-place"), 1,
                    spaces),
                new[]
                {
                    new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(0, 2))
                }, Array.Empty<ObstacleOccupancy>());
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret, new HexCoordinate(0, 1)), Is.True);
            var participants = new MatchParticipants(
                new[]
                {
                    new Team(new TeamId("red"), TeamSide.Red),
                    new Team(new TeamId("blue"), TeamSide.Blue)
                },
                new[]
                {
                    new Player(new PlayerId("owner"), new TeamId("red")),
                    new Player(new PlayerId("enemy"), new TeamId("blue"))
                },
                new[]
                {
                    new Hero(Trinkets, new PlayerId("owner"), HeroDashboard.RulebookLayout),
                    new Hero(Enemy, new PlayerId("enemy"), HeroDashboard.RulebookLayout)
                });
            var card = TrinketsCardContent.GreenDefinitions[3];
            var skill = card.ActionDefinitions.Single(action => action.Kind == ActionKind.Skill);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(Array.Empty<TokenDefinition>(),
                    Array.Empty<MarkerDefinition>()),
                board, new MarkerAttachmentRegister(), board, board, board,
                obstacles: board);
            var context = new EffectResolutionContext(
                skill.EffectRoot, catalog, participants, null, 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("perfect-place"), new CardInstanceId("green-source"),
                card.Reference, skill.Id, Trinkets, Trinkets,
                ActionInvocationContext.PrintedPrimary, context);
            var mode = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.OptionNodeId.Equals(new EffectNodeId("place-arm")));
            step = EffectResolutionSession.Answer(step.Frame, mode, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            var at = new HexCoordinate(1, 1);
            var choice = step.Frame.PendingChoice;
            Assert.That(choice.SelectInvocations.Select(value => value.Candidate.Value),
                Does.Contain(EffectValue.Space(at)));
            var command = choice.SelectInvocations.Single(value =>
                value.Candidate.Value.Equals(EffectValue.Space(at)));
            Assert.That(board.TryPlaceOrRepositionObstacle(turret,
                new HexCoordinate(3, 3)), Is.True);
            var stale = EffectResolutionSession.Answer(step.Frame, command, context);
            if (stale.Frame?.PendingChoice?.ConfirmInvocation != null)
                stale = EffectResolutionSession.Answer(
                    stale.Frame, stale.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var unchanged), Is.True);
            Assert.That(unchanged, Is.EqualTo(new HexCoordinate(0, 0)));
            Assert.That(board.TryPlaceOrRepositionObstacle(turret,
                new HexCoordinate(0, 1)), Is.True);
            step = EffectResolutionSession.Answer(step.Frame, command, context);
            if (step.Frame?.PendingChoice?.ConfirmInvocation != null)
                step = EffectResolutionSession.Answer(
                    step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var placed), Is.True);
            Assert.That(placed, Is.EqualTo(at));
            Assert.That(board.TryGetObstacleCoordinate(turret, out var turretAt), Is.True);
            Assert.That(turretAt, Is.EqualTo(new HexCoordinate(0, 1)));
        }
    }
}
