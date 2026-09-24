using System;
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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class TrinketsBasicSkillTests
    {
        private static readonly HeroId Trinkets = new HeroId("trinkets-basic");

        [Test]
        public void RapidRedeployment_MoveThenPlace_RepositionsOnlyOnePersistentTurret()
        {
            var (board, context, card, action) = Setup(
                TrinketsCardContent.RapidRedeployment);
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            Assert.That(board.TryPlaceOrRepositionObstacle(turret,
                new HexCoordinate(1, 0)), Is.True);
            var step = Start(context, card, action);
            step = SelectOption(step, context, "move-place");
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(step.Frame.PendingChoice, Is.Not.Null);
            var move = step.Frame.PendingChoice.SelectInvocations.First(command =>
                command.Candidate.Value.Equals(EffectValue.Space(new HexCoordinate(0, 1))));
            step = Answer(step, move, context);
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var moved), Is.True);
            Assert.That(moved, Is.EqualTo(new HexCoordinate(0, 1)));
            var destination = new HexCoordinate(0, 2);
            var place = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Space(destination)));
            step = Answer(step, place, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(board.TryGetObstacleCoordinate(turret, out var at), Is.True);
            Assert.That(at, Is.EqualTo(destination));
            Assert.That(board.ObstacleOccupancies.Count, Is.EqualTo(1));
        }

        [Test]
        public void SalvageParts_PlaceOrRemoveMove_UsesLiveTurret()
        {
            var (board, context, card, action) = Setup(TrinketsCardContent.SalvageParts);
            var turret = OwnedObstacleIdentity.For(TrinketsComponentContent.TurretKind, Trinkets);
            var step = Start(context, card, action);
            step = SelectOption(step, context, "place-arm");
            var at = new HexCoordinate(1, 0);
            var place = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Space(at)));
            step = Answer(step, place, context);
            Assert.That(board.TryGetObstacleCoordinate(turret, out var placed), Is.True);
            Assert.That(placed, Is.EqualTo(at));

            step = Start(context, card, action);
            step = SelectOption(step, context, "move-arm");
            Assert.That(board.TryGetObstacleCoordinate(turret, out _), Is.False);
            var move = step.Frame.PendingChoice.SelectInvocations.First(command =>
                command.Candidate.Value.Equals(EffectValue.Space(new HexCoordinate(0, 1))));
            step = Answer(step, move, context);
            Assert.That(step.Accepted, Is.True, step.RejectionReason.ToString());
            Assert.That(board.TryGetHeroCoordinate(Trinkets, out var moved), Is.True);
            Assert.That(moved, Is.EqualTo(new HexCoordinate(0, 1)));
        }

        private static (AdjacentMoveAuthority Board, EffectResolutionContext Context,
            CardDefinition Card, ActionDefinition Action) Setup(CardDefinitionId id)
        {
            var spaces = Enumerable.Range(-4, 9).SelectMany(q =>
                Enumerable.Range(-4, 9).Select(r => new HexCoordinate(q, r))).ToArray();
            var board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(
                new MapId("trinkets-basic"), 1, spaces),
                new[] { new HeroOccupancy(Trinkets, new HexCoordinate(0, 0)) },
                Array.Empty<ObstacleOccupancy>());
            var participants = new MatchParticipants(
                new[] { new Team(new TeamId("red"), TeamSide.Red) },
                new[] { new Player(new PlayerId("owner"), new TeamId("red")) },
                new[] { new Hero(Trinkets, new PlayerId("owner"),
                    HeroDashboard.RulebookLayout) });
            var card = TrinketsCardContent.Definition(id);
            var action = card.ActionDefinitions.Single(value => value.Kind == ActionKind.Skill);
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(TrinketsComponentContent.Tokens(),
                    Array.Empty<MarkerDefinition>()), board,
                new MarkerAttachmentRegister(), board, board, board, obstacles: board);
            var catalog = SyntheticEffectCapabilities.CreateCatalog(
                TrinketsComponentContent.ObstacleHandlers());
            var context = new EffectResolutionContext(action.EffectRoot, catalog,
                participants, null, 1, 1, RuleModificationSet.Empty,
                EffectCardState.Of(new[] { card }, Array.Empty<CardInstance>()),
                ActionKind.Skill, components: components);
            return (board, context, card, action);
        }

        private static EffectResolutionStepResult Start(EffectResolutionContext context,
            CardDefinition card, ActionDefinition action) =>
            EffectResolutionSession.Start(new EffectResolutionId("basic-skill"),
                new CardInstanceId("basic-source"), card.Reference, action.Id,
                Trinkets, Trinkets, ActionInvocationContext.PrintedPrimary, context);

        private static EffectResolutionStepResult SelectOption(EffectResolutionStepResult step,
            EffectResolutionContext context, string arm)
        {
            var command = step.Frame.PendingChoice.SelectInvocations.Single(value =>
                value.Candidate.OptionNodeId.Equals(new EffectNodeId(arm)));
            return Answer(step, command, context);
        }

        private static EffectResolutionStepResult Answer(EffectResolutionStepResult step,
            EffectChoiceCommand command, EffectResolutionContext context)
        {
            step = EffectResolutionSession.Answer(step.Frame, command, context);
            if (step.Frame?.PendingChoice?.ConfirmInvocation != null)
                step = EffectResolutionSession.Answer(step.Frame,
                    step.Frame.PendingChoice.ConfirmInvocation, context);
            return step;
        }
    }
}
