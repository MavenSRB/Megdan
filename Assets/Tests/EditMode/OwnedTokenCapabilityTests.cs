using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class OwnedTokenCapabilityTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Other = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly TokenDefinition Token = Definition("named-token");
        private static readonly EffectCapabilityReference Reference =
            new EffectCapabilityReference(new EffectCapabilityId("fixture-owned-token"), 1);

        [Test]
        public void PresenceAndPosition_AreOwnerAndRevisionScopedReadOnlyFacts()
        {
            var board = Board();
            var otherDefinition = Definition("another-token");
            var foreign = Place(board, Token, Other, 1);
            Place(board, otherDefinition, Actor, 2);
            var context = Context(board);
            var presence = SyntheticOwnedTokenCapabilities.PresenceHandler(Reference, Token);
            var position = SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Token);
            Assert.That(presence.Execute(context).Outcomes.Single().Value.BooleanValue, Is.False);
            Assert.That(position.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            board.CommitValidatedTokenReclamation(foreign.Id);
            Place(board, Token, Actor, 3);
            Assert.That(presence.Execute(context).Outcomes.Single().Value.BooleanValue, Is.True);
            var result = position.Execute(context);
            Assert.That(result.Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(result.Outcomes.Single().Value.Coordinate, Is.EqualTo(new HexCoordinate(3, 0)));
            var anticipated = (IEffectAnticipatedOutcomeCapabilityHandler)position;
            Assert.That(anticipated.PublishesWithoutApplying, Is.True);
            Assert.That(anticipated.AnticipateOutcomes(context, Array.Empty<EffectValue>()), Is.EqualTo(result.Outcomes));
            Assert.That(board.PlacedTokens.Count, Is.EqualTo(2));
            Assert.That(SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Definition("named-token", 2))
                .Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void MissingBoardIsNotAFalsePresence_AndSupplyPreventsDuplicateNamedPieces()
        {
            var presence = SyntheticOwnedTokenCapabilities.PresenceHandler(Reference, Token);
            Assert.That(presence.Execute(Context(null)).Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            var board = Board();
            Place(board, Token, Actor, 1);
            Assert.That(() => Place(board, Token, Actor, 2), Throws.InvalidOperationException);
            Assert.That(presence.Execute(Context(board)).Outcomes.Single().Value.BooleanValue, Is.True);
            Assert.That(SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Token)
                .Anticipate(Context(board)), Is.EqualTo(EffectExecutionResultKind.Applied));
        }

        [TestCase(PrintedValueChannel.Range, 2)]
        [TestCase(PrintedValueChannel.Radius, 3)]
        public void ReachUsesActorOriginAndOnlyItsDeclaredPrintedIcon(PrintedValueChannel channel, int printed)
        {
            var board = Board();
            var card = channel == PrintedValueChannel.Range ? TaliCardContent.AncestralTotem : TaliCardContent.GlacialBarrier;
            var handler = SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Token, channel);
            var token = Place(board, Token, Actor, printed);
            Assert.That(handler.Execute(Context(board, card)).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            board.CommitValidatedDisplacement(BoardObjectReference.ForToken(token.Id), new HexCoordinate(printed + 1, 0));
            Assert.That(handler.Anticipate(Context(board, card)), Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(handler.Execute(Context(board, card, Items(channel))).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(handler.Execute(Context(board, card, Items(PrintedValueChannel.Movement))).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(handler.Execute(Context(board, TaliCardContent.ColdSnap, Items(channel))).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory), "Items cannot invent the other printed channel.");
        }

        [Test]
        public void PositionRequeriesLiveBoardAfterRelocationAndRemoval()
        {
            var board = Board();
            var token = Place(board, Token, Actor, 1);
            var handler = SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Token);
            var context = Context(board);
            Assert.That(handler.Execute(context).Outcomes.Single().Value.Coordinate, Is.EqualTo(new HexCoordinate(1, 0)));
            board.CommitValidatedDisplacement(BoardObjectReference.ForToken(token.Id), new HexCoordinate(2, 0));
            Assert.That(handler.Execute(context).Outcomes.Single().Value.Coordinate, Is.EqualTo(new HexCoordinate(2, 0)));
            board.CommitValidatedTokenReclamation(token.Id);
            Assert.That(handler.Execute(context).Kind, Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            Assert.That(((IEffectAnticipatedOutcomeCapabilityHandler)handler)
                .AnticipateOutcomes(context, Array.Empty<EffectValue>()), Is.Empty);
        }

        [Test]
        public void RetainedRoundToken_IsNotClearedByRoundCleanup_ButCanStillBeRemoved()
        {
            var board = Board();
            var persistent = Place(board, Token, Actor, 1);
            var ordinary = Place(board, Definition("ordinary", removal: TokenRemovalCondition.EndOfRoundCleanup), Actor, 2);
            var cleanup = ComponentCleanupRules.TokensRemovedAtEndOfRound(board.PlacedTokens);
            Assert.That(cleanup.Select(t => t.Id), Is.EqualTo(new[] { ordinary.Id }));
            foreach (var token in cleanup) board.CommitValidatedTokenReclamation(token.Id);
            Assert.That(board.PlacedTokens.Single(), Is.SameAs(persistent));
            board.CommitValidatedTokenReclamation(persistent.Id);
            Assert.That(board.PlacedTokens, Is.Empty);
        }

        [TestCase(TokenRemovalCondition.None)]
        [TestCase((TokenRemovalCondition)99)]
        public void LifetimeRequiresAnExplicitSupportedCondition(TokenRemovalCondition condition) =>
            Assert.That(() => Definition("invalid", removal: condition), Throws.ArgumentException);

        [Test]
        public void DeclarationRefusesMultiPieceSuppliesAndNonReachChannels()
        {
            Assert.That(() => SyntheticOwnedTokenCapabilities.SpaceHandler(Reference,
                new TokenDefinition(new TokenDefinitionId("multiple"), 1, "Multiple", 2,
                    Array.Empty<TokenPropertyDefinition>(), TokenRemovalCondition.EndOfRoundCleanup)), Throws.ArgumentException);
            Assert.That(() => SyntheticOwnedTokenCapabilities.SpaceHandler(Reference, Token, PrintedValueChannel.Movement),
                Throws.ArgumentException);
        }

        private static TokenDefinition Definition(string id, int revision = 1,
            TokenRemovalCondition removal = TokenRemovalCondition.RetainedAtEndOfRound) =>
            new TokenDefinition(new TokenDefinitionId(id), revision, id, 1, Array.Empty<TokenPropertyDefinition>(), removal);

        private static AdjacentMoveAuthority Board() => new AdjacentMoveAuthority(
            Enumerable.Range(-1, 8).Select(q => new HexCoordinate(q, 0)).ToArray(),
            new[] { new HeroOccupancy(Actor, new HexCoordinate(0, 0)), new HeroOccupancy(Other, new HexCoordinate(-1, 0)) });

        private static PlacedToken Place(AdjacentMoveAuthority board, TokenDefinition definition, HeroId owner, int q)
        {
            var token = new PlacedToken(new TokenInstanceId("token-" + q), definition, owner, owner,
                new CardInstanceId("source"), new HexCoordinate(q, 0));
            board.CommitValidatedTokenPlacement(token);
            Assert.That(board.PlacedTokens.Any(placed => placed.Id.Equals(token.Id)), Is.True);
            return token;
        }

        private static HeroItemState Items(PrintedValueChannel channel) => HeroItemState.Restore(
            StagedBoardParticipants.Create(), new HeroItemRestoreState(new[]
            {
                new HeroItem(Actor, new CardInstanceId("item"), HeroDashboard.RulebookLayout.Slots[3].Position, channel, 1)
            }));

        private static EffectCapabilityContext Context(AdjacentMoveAuthority board,
            CardDefinitionId card = default, HeroItemState items = null)
        {
            var definition = TaliCardContent.Definition(card.IsDefined ? card : TaliCardContent.GlacialBarrier);
            return new EffectCapabilityContext(new EffectNodeId("token-fact"), EffectFailurePolicy.Mandatory,
                Actor, Actor, new CardInstanceId("source"), definition.Reference, TaliCardContent.PrimarySkill,
                new Dictionary<string, EffectValue>(), 1, 1, StagedBoardParticipants.Create(), null,
                RuleModificationSet.Empty, EffectCardState.Of(TaliCardContent.AllDefinitions, Array.Empty<CardInstance>()),
                ActionKind.Skill, new MatchComponents(new ComponentDefinitionCatalog(new[] { Token },
                    Array.Empty<MarkerDefinition>()), board, new MarkerAttachmentRegister(), board, board, board),
                heroItems: items);
        }
    }
}
