using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ClearActionTests
    {
        [Test]
        public void ClearOffer_IsAValueFreeTextSuppressingReplacementForAttack()
        {
            var fixture = new Fixture();

            Assert.That(fixture.ClearOffer.InvocationContext, Is.EqualTo(ActionInvocationContext.Replacement));
            Assert.That(fixture.ClearOffer.ActionDefinitionId,
                Is.EqualTo(GeneralRuleActionOfferQuery.ClearDefinition.Id));
            Assert.That(GeneralRuleActionOfferQuery.ClearDefinition.Kind, Is.EqualTo(ActionKind.Clear));
            Assert.That(GeneralRuleActionOfferQuery.ClearDefinition.Origin,
                Is.EqualTo(ActionDefinitionOrigin.Replacement));
            Assert.That(GeneralRuleActionOfferQuery.ClearDefinition.BaseValues, Is.Empty);
            Assert.That(GeneralRuleActionOfferQuery.ClearDefinition.DefaultPolicy.CardTextApplication,
                Is.EqualTo(CardTextApplicationPolicy.Suppressed),
                "Clear replaces the entire Attack, including the source card's Primary text.");

            Assert.That(GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                fixture.MovementAction.Id,
                fixture.Card.Id,
                fixture.ActorId,
                fixture.Definitions,
                fixture.Instances), Is.Null,
                "Clear replaces Attack, not another replaceable action kind.");
        }

        [Test]
        public void ClearQuery_ProjectsEveryAndOnlyAdjacentTokenInCanonicalOrder()
        {
            var fixture = new Fixture();

            var legal = ClearActionQuery.FindLegalTokenIds(
                fixture.ClearOffer,
                fixture.AttackAction.Id,
                fixture.Definitions,
                fixture.Instances,
                fixture.Board);

            Assert.That(legal, Is.EqualTo(new[] { fixture.AdjacentB.Id, fixture.AdjacentA.Id }));
            Assert.That(legal, Has.None.EqualTo(fixture.NonAdjacent.Id));
        }

        [Test]
        public void Clear_RemovesExactlyTheChosenAdjacentSubsetWithoutAttackResolution()
        {
            var fixture = new Fixture();

            var result = fixture.Perform(new[] { fixture.AdjacentA.Id });

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.IsComplete, Is.True);
            Assert.That(fixture.Board.PlacedTokens.Select(token => token.Id),
                Is.EquivalentTo(new[] { fixture.AdjacentB.Id, fixture.NonAdjacent.Id }));
            Assert.That(result.Events.Select(ruleEvent => ruleEvent.Kind), Is.EqualTo(new[]
            {
                CardActionResolutionEventKind.ClearActionStarted,
                CardActionResolutionEventKind.TokenCleared,
                CardActionResolutionEventKind.ClearActionCompleted
            }));
            Assert.That(result.Events, Has.All.Matches<CardActionResolutionEvent>(ruleEvent =>
                ruleEvent.ActionDefinitionId.Equals(GeneralRuleActionOfferQuery.ClearDefinition.Id) &&
                ruleEvent.ReplacedActionDefinitionId.Equals(fixture.AttackAction.Id)));
            Assert.That(result.Events, Has.None.Matches<CardActionResolutionEvent>(ruleEvent =>
                ruleEvent.Kind == CardActionResolutionEventKind.AttackStarted ||
                ruleEvent.Kind == CardActionResolutionEventKind.AttackTotalCalculated ||
                ruleEvent.Kind == CardActionResolutionEventKind.AttackResolutionCompleted));
            Assert.That(result.Events.Single(ruleEvent =>
                ruleEvent.Kind == CardActionResolutionEventKind.TokenCleared).TargetTokenInstanceId,
                Is.EqualTo(fixture.AdjacentA.Id));
            Assert.That(result.Steps.First().Checkpoint, Is.EqualTo(TimingCheckpoint.ActionStarted));
            Assert.That(result.Steps.Last().Checkpoint, Is.EqualTo(TimingCheckpoint.ActionEnded));
        }

        [Test]
        public void Clear_MayChooseNoAdjacentTokens()
        {
            var fixture = new Fixture();
            var before = fixture.Board.PlacedTokens.Select(token => token.Id).ToArray();

            var result = fixture.Perform(Array.Empty<TokenInstanceId>());

            Assert.That(result.Accepted, Is.True,
                "Rulebook p.13 permits removing any number of adjacent tokens, including zero.");
            Assert.That(result.IsComplete, Is.True);
            Assert.That(result.Events, Has.None.Matches<CardActionResolutionEvent>(ruleEvent =>
                ruleEvent.Kind == CardActionResolutionEventKind.TokenCleared));
            Assert.That(fixture.Board.PlacedTokens.Select(token => token.Id), Is.EqualTo(before));
        }

        [Test]
        public void Clear_MultipleChosenTokensCommitInCanonicalBoardOrder()
        {
            var fixture = new Fixture();

            var result = fixture.Perform(new[] { fixture.AdjacentA.Id, fixture.AdjacentB.Id });

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.Events
                .Where(ruleEvent => ruleEvent.Kind == CardActionResolutionEventKind.TokenCleared)
                .Select(ruleEvent => ruleEvent.TargetTokenInstanceId),
                Is.EqualTo(new[] { fixture.AdjacentB.Id, fixture.AdjacentA.Id }),
                "Removal fact order is board-canonical and independent of command enumeration order.");
            Assert.That(fixture.Board.PlacedTokens.Single().Id, Is.EqualTo(fixture.NonAdjacent.Id));
        }

        [Test]
        public void IllegalOrStaleClearSelection_IsRejectedBeforeAnyMutation()
        {
            AssertRejected(
                fixture => new[] { fixture.NonAdjacent.Id },
                CardActionResolutionRejectionReason.ClearTokenSelectionUnavailable);
            AssertRejected(
                fixture => new[] { fixture.AdjacentA.Id, fixture.AdjacentA.Id },
                CardActionResolutionRejectionReason.ClearTokenSelectionUnavailable);

            var stale = new Fixture();
            var before = stale.Board.PlacedTokens.Select(token => token.Id).ToArray();
            Assert.That(CardLifecycleAuthority.TryResolveAfterAction(stale.Card), Is.True);

            var staleResult = stale.Perform(new[] { stale.AdjacentA.Id });

            Assert.That(staleResult.Accepted, Is.False);
            Assert.That(staleResult.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(staleResult.Events, Is.Empty);
            Assert.That(stale.Board.PlacedTokens.Select(token => token.Id), Is.EqualTo(before));

            var forged = new Fixture();
            var forgedBefore = forged.Board.PlacedTokens.Select(token => token.Id).ToArray();
            var unavailableOffer = new ActionOffer(
                forged.ClearOffer.SourceCardInstanceId,
                forged.ClearOffer.SourceDefinitionReference,
                forged.ClearOffer.ActionDefinitionId,
                forged.ClearOffer.ActorId,
                ActionInvocationContext.Replacement,
                false);

            var forgedResult = forged.PerformWithOffer(
                unavailableOffer, new[] { forged.AdjacentA.Id });

            Assert.That(forgedResult.Accepted, Is.False);
            Assert.That(forgedResult.RejectionReason,
                Is.EqualTo(CardActionResolutionRejectionReason.UnavailableOffer));
            Assert.That(forgedResult.Events, Is.Empty);
            Assert.That(forged.Board.PlacedTokens.Select(token => token.Id), Is.EqualTo(forgedBefore));
        }

        private static void AssertRejected(
            Func<Fixture, IEnumerable<TokenInstanceId>> selection,
            CardActionResolutionRejectionReason expected)
        {
            var fixture = new Fixture();
            var before = fixture.Board.PlacedTokens.Select(token => token.Id).ToArray();

            var result = fixture.Perform(selection(fixture));

            Assert.That(result.Accepted, Is.False);
            Assert.That(result.RejectionReason, Is.EqualTo(expected));
            Assert.That(result.Events, Is.Empty);
            Assert.That(fixture.Board.PlacedTokens.Select(token => token.Id), Is.EqualTo(before));
        }

        private sealed class Fixture
        {
            private readonly TokenDefinition tokenDefinition = new TokenDefinition(
                new TokenDefinitionId("clear-test-token"),
                1,
                "Clear Test Token",
                4,
                new[]
                {
                    new TokenPropertyDefinition(
                        "clear-test-property",
                        "clear-test-rule",
                        "while-token-remains-placed")
                });

            public Fixture()
            {
                ActorId = new HeroId("clear-actor");
                AttackAction = new ActionDefinition(
                    new ActionDefinitionId("clear-source-attack"),
                    ActionKind.Attack,
                    ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.NonRanged,
                    default);
                MovementAction = new ActionDefinition(
                    new ActionDefinitionId("clear-source-movement"),
                    ActionKind.Movement,
                    ActionDefinitionOrigin.PrintedSecondary,
                    new[] { new PrintedValueDefinition(PrintedValueChannel.Movement, 2) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Suppressed, false, true));
                var definition = new CardDefinition(
                    new CardDefinitionId("clear-source-card-definition"),
                    "clear-source-card-r1",
                    "TEST - Clear Source",
                    new InitiativeValue(10),
                    CardContentAuthority.SyntheticTestFixture,
                    new[] { AttackAction, MovementAction });
                Card = CardInstance.CreateInHand(
                    new CardInstanceId("clear-source-card"), definition.Reference, ActorId);
                Assert.That(CardLifecycleAuthority.TryPlayFacedown(Card), Is.True);
                Assert.That(CardLifecycleAuthority.TryReveal(Card), Is.True);
                Definitions = new[] { definition };
                Instances = new[] { Card };

                var coordinates = new[]
                {
                    new HexCoordinate(0, 0),
                    new HexCoordinate(1, 0),
                    new HexCoordinate(0, 1),
                    new HexCoordinate(2, 0)
                };
                Board = new AdjacentMoveAuthority(
                    BoardMapDefinition.CreateSynthetic(new MapId("clear-board"), 1, coordinates),
                    new[] { new HeroOccupancy(ActorId, new HexCoordinate(0, 0)) },
                    Array.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>());
                AdjacentA = CreateToken("clear-adjacent-a", new HexCoordinate(1, 0));
                AdjacentB = CreateToken("clear-adjacent-b", new HexCoordinate(0, 1));
                NonAdjacent = CreateToken("clear-non-adjacent", new HexCoordinate(2, 0));
                Board.CommitValidatedTokenPlacement(AdjacentA);
                Board.CommitValidatedTokenPlacement(AdjacentB);
                Board.CommitValidatedTokenPlacement(NonAdjacent);

                ClearOffer = GeneralRuleActionOfferQuery.GetAvailableClearOffer(
                    AttackAction.Id, Card.Id, ActorId, Definitions, Instances).Value;
            }

            public HeroId ActorId { get; }
            public ActionDefinition AttackAction { get; }
            public ActionDefinition MovementAction { get; }
            public CardInstance Card { get; }
            public CardDefinition[] Definitions { get; }
            public CardInstance[] Instances { get; }
            public AdjacentMoveAuthority Board { get; }
            public PlacedToken AdjacentA { get; }
            public PlacedToken AdjacentB { get; }
            public PlacedToken NonAdjacent { get; }
            public ActionOffer ClearOffer { get; }

            public CardActionResolutionResult Perform(IEnumerable<TokenInstanceId> selectedTokenIds) =>
                PerformWithOffer(ClearOffer, selectedTokenIds);

            public CardActionResolutionResult PerformWithOffer(
                ActionOffer offer,
                IEnumerable<TokenInstanceId> selectedTokenIds) =>
                new CardActionResolutionAuthority(Board).PerformTurnClear(
                    new ActionResolutionId("clear-test-resolution"),
                    offer,
                    AttackAction.Id,
                    selectedTokenIds,
                    Definitions,
                    Instances);

            private PlacedToken CreateToken(string id, HexCoordinate coordinate) =>
                new PlacedToken(
                    new TokenInstanceId(id),
                    tokenDefinition,
                    ActorId,
                    ActorId,
                    Card.Id,
                    coordinate);
        }
    }
}
