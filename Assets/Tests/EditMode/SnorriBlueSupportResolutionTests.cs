using System;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class SnorriBlueSupportResolutionTests
    {
        private static readonly HeroId Actor =
            new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly HeroId Friend =
            new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Enemy =
            new HeroId(StagedBoardParticipants.RedHeroIdValue);

        [Test]
        public void RuneThreeRetrievalIsChosenByFriendAndReturnsAllTheirDiscards()
        {
            var face = SnorriBlueSupportFaces.AncestralBoon;
            var skill = face.ActionDefinitions.Single(action =>
                action.Kind == ActionKind.Skill);
            var board = new AdjacentMoveAuthority(
                new[]
                {
                    new HexCoordinate(0, 0), new HexCoordinate(1, 0),
                    new HexCoordinate(-1, 0)
                },
                new[]
                {
                    new HeroOccupancy(Actor, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Friend, new HexCoordinate(1, 0)),
                    new HeroOccupancy(Enemy, new HexCoordinate(-1, 0))
                });
            var markers = new MarkerAttachmentRegister();
            markers.Give(new MarkerAttachment(
                new MarkerInstanceId("rune-three"),
                SnorriComponentContent.Runes[2], Actor, Actor,
                new CardInstanceId("inscribe"), SnorriSilverFace.PrimarySkill, 1));
            var components = new MatchComponents(
                new ComponentDefinitionCatalog(
                    Array.Empty<TokenDefinition>(), SnorriComponentContent.Markers()),
                board, markers, board, board, board);
            var source = CardInstance.CreateInHand(
                new CardInstanceId("boon-source"), face.Reference, Actor);
            var first = new CardInstance(
                new CardInstanceId("friend-discard-one"),
                SnorriBlueMovementFaces.Definitions[0].Reference, Friend,
                CardStateFacts.DiscardedUnplayed());
            var second = new CardInstance(
                new CardInstanceId("friend-discard-two"),
                SnorriBlueMovementFaces.Definitions[1].Reference, Friend,
                CardStateFacts.DiscardedUnplayed());
            var context = new EffectResolutionContext(
                skill.EffectRoot,
                SyntheticEffectCapabilities.CreateCatalog(
                    SnorriComponentContent.Handlers()
                        .Concat(SnorriBlueSupportCapabilities.Handlers())),
                StagedBoardParticipants.CreateWithBlueAlly(),
                new ActiveEffectLifecycle(), 1, 1,
                RuleModificationSet.Empty,
                EffectCardState.Of(
                    SnorriBlueMovementFaces.Definitions
                        .Concat(SnorriBlueSupportFaces.Definitions),
                    new[] { source, first, second }),
                ActionKind.Skill, components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId("ancestral-retrieve"),
                source.Id, face.Reference, skill.Id, Actor, Actor,
                ActionInvocationContext.PrintedPrimary, context);

            Assert.That(step.Accepted, Is.True);
            Assert.That(step.Frame.PendingChoice, Is.Not.Null);
            var chooseFriend = step.Frame.PendingChoice.SelectInvocations.Single(command =>
                command.Candidate.Value.Equals(EffectValue.Hero(Friend)));
            step = EffectResolutionSession.Answer(step.Frame, chooseFriend, context);
            Assert.That(step.Accepted, Is.True);
            Assert.That(step.Frame.PendingChoice.ChooserId, Is.EqualTo(Friend));
            step = EffectResolutionSession.Answer(
                step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(step.Accepted, Is.True);
            Assert.That(first.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(second.State.Location, Is.EqualTo(CardLocation.Hand));
            Assert.That(source.State.Location, Is.EqualTo(CardLocation.Hand));
        }
    }
}
