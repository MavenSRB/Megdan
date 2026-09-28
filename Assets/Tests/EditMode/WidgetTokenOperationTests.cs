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
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>WI-Q3 / Dragon Bond and the printed ordinary Pyro moves and swaps. No dealt content.</summary>
    public sealed class WidgetTokenOperationTests
    {
        private static readonly HeroId Actor = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Enemy = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly HeroId Friend = new HeroId(StagedBoardParticipants.BlueAllyHeroIdValue);
        private static readonly CardDefinitionId Card = new CardDefinitionId("fixture-widget-clause");
        private static readonly ActionDefinitionId Action = new ActionDefinitionId("fixture-primary");
        private static HexCoordinate At(int q, int r = 0) => new HexCoordinate(q, r);

        [Test]
        public void PyroIsOnePersistentOrdinaryToken_AndIsRegisteredWithWidget()
        {
            var definition = WidgetComponentContent.PyroDefinition;
            Assert.That(definition.SupplyCount, Is.EqualTo(1));
            Assert.That(definition.RemovalCondition, Is.EqualTo(TokenRemovalCondition.RetainedAtEndOfRound));
            Assert.That(definition.Properties, Is.Empty);
            Assert.That(WidgetComponentContent.DeferredHandlers(), Is.Empty);
            Assert.That(WidgetComponentContent.RegisteredHandlers().Count(), Is.EqualTo(9));
            Assert.That(MatchContentCatalogs.ForThisBuild().Components.RegisteredTokens,
                Has.Member(definition.Reference));
        }

        [Test]
        public void DragonBondPlacesFromSupply_ThenRelocatesTheSameInstance()
        {
            var effect = WidgetEffectContent.DragonBond(Card, "rev-1");
            var probe = new Probe(effect);
            probe.Option("place-pyro");
            Assert.That(probe.Choices, Has.No.Member(EffectValue.Space(At(0))));
            Assert.That(probe.Choices, Has.No.Member(EffectValue.Space(At(3))));
            probe.Pick(EffectValue.Space(At(2)));
            Assert.That(probe.Step.IsComplete, Is.True);
            var original = probe.Board.PlacedTokens.Single();
            probe.Start();
            probe.Option("place-pyro");
            Assert.That(probe.Choice.NodeId.Value, Is.EqualTo("place-existing"));
            Assert.That(probe.Choices, Has.No.Member(EffectValue.Space(At(2))), "Place requires a different empty space.");
            probe.Pick(EffectValue.Space(At(-2)));
            Assert.That(probe.Board.PlacedTokens.Single().Id, Is.EqualTo(original.Id));
            Assert.That(probe.PyroAt, Is.EqualTo(At(-2)));
            Assert.That(probe.Board.PlacedTokens.Single().SourceCardInstanceId, Is.EqualTo(original.SourceCardInstanceId));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DragonBondBothOrders_RequeryTheVacatedSpaceForTheSecondMove(bool heroFirst)
        {
            var probe = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1));
            probe.Option("move-both");
            probe.Option(heroFirst ? "hero-first" : "pyro-first");
            probe.Pick(EffectValue.Space(heroFirst ? At(-1) : At(2)));
            Assert.That(probe.Choices, Has.Member(EffectValue.Space(heroFirst ? At(0) : At(1))));
            probe.Pick(EffectValue.Space(heroFirst ? At(0) : At(1)));
            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.HeroAt, Is.EqualTo(heroFirst ? At(-1) : At(1)));
            Assert.That(probe.PyroAt, Is.EqualTo(heroFirst ? At(0) : At(2)));
        }

        [Test]
        public void DragonBondMissingPyroRefusesMoveBoth_AndZeroMovesRemainLegal()
        {
            var empty = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"));
            Assert.That(empty.Choice.SelectInvocations.Select(c => c.Candidate.OptionNodeId.Value), Has.No.Member("move-both"));
            var probe = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1));
            probe.Option("move-both"); probe.Option("hero-first");
            probe.Pick(EffectValue.Space(At(0))); probe.Pick(EffectValue.Space(At(1)));
            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.HeroAt, Is.EqualTo(At(0)));
            Assert.That(probe.PyroAt, Is.EqualTo(At(1)));
        }

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void DiversionaryRiderMovesOnlyPyro_UpToPrintedDistance(int distance)
        {
            var probe = new Probe(WidgetEffectContent.MovePyroAfterAttack(Card, "rev-1", distance), At(1));
            Assert.That(probe.Choices, Has.Member(EffectValue.Space(At(1))), "Zero is a choice, not a failed mandatory step.");
            Assert.That(probe.Choices, Has.No.Member(EffectValue.Space(At(distance + 2))));
            probe.Pick(EffectValue.Space(At(distance + 1)));
            Assert.That(probe.HeroAt, Is.EqualTo(At(0)));
            Assert.That(probe.PyroAt, Is.EqualTo(At(distance + 1)));
        }

        [Test]
        public void AirborneSwapUsesRadius_NotRange_AndCanBeDeclined()
        {
            var effect = WidgetEffectContent.SwapWithPyro(Card, "rev-1", "before-attack");
            var outside = new Probe(effect, At(3), range: 5, radius: 2);
            Assert.That(outside.Step.IsComplete, Is.True);
            Assert.That(outside.PyroAt, Is.EqualTo(At(3)));
            var inside = new Probe(effect, At(3), radius: 3);
            inside.Answer(inside.Choice.ConfirmInvocation);
            Assert.That(inside.HeroAt, Is.EqualTo(At(3)));
            Assert.That(inside.PyroAt, Is.EqualTo(At(0)));
            var decline = new Probe(effect, At(1));
            decline.Answer(decline.Choice.DeclineInvocation);
            Assert.That(decline.HeroAt, Is.EqualTo(At(0)));
        }

        [Test]
        public void AllAboardChecksBothRanges_AndSelectsOnlyAnotherFriendlyHero()
        {
            // Actor is Blue so the existing three-seat fixture supplies an allied hero.
            var probe = new Probe(WidgetEffectContent.AllAboard(Card, "rev-1", false), At(1), actor: Enemy, range: 2);
            probe.Option("friendly");
            Assert.That(probe.Choices, Is.EqualTo(new[] { EffectValue.Hero(Friend) }));
            probe.Pick(EffectValue.Hero(Friend));
            Assert.That(probe.PyroAt, Is.EqualTo(At(0, 2)));
            Assert.That(probe.Board.Occupancies.Single(h => h.HeroId.Equals(Friend)).Coordinate, Is.EqualTo(At(1)));
            Assert.That(probe.HeroAt, Is.EqualTo(At(0)));
            var outside = new Probe(WidgetEffectContent.AllAboard(Card, "rev-1", false), At(3), range: 2, radius: 5);
            Assert.That(outside.Step.IsComplete, Is.True);
            Assert.That(outside.HeroAt, Is.EqualTo(At(0)));
        }

        [Test]
        public void SafeLandingRechecksRangeAfterItsExactOneSpaceMove()
        {
            var probe = new Probe(WidgetEffectContent.AllAboard(Card, "rev-1", true), At(3), range: 3);
            probe.Answer(probe.Choice.ConfirmInvocation);
            Assert.That(probe.Choices, Has.No.Member(EffectValue.Space(At(3))), "Optional exact one is not up-to one.");
            probe.Pick(EffectValue.Space(At(4)));
            Assert.That(probe.Step.IsComplete, Is.True, "The mandatory swap cannot reach Pyro after moving out of range.");
            Assert.That(probe.HeroAt, Is.EqualTo(At(0)));
            Assert.That(probe.PyroAt, Is.EqualTo(At(4)));
        }

        [Test]
        public void DestinationBecomingOccupiedCannotMovePyro_AndOldChoiceIdentityIsRejected()
        {
            var probe = new Probe(WidgetEffectContent.MovePyroAfterAttack(Card, "rev-1", 2), At(1));
            var command = probe.Choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(EffectValue.Space(At(2))));
            probe.Board.CommitValidatedDisplacement(BoardObjectReference.ForHero(Enemy), At(2));
            var result = EffectResolutionSession.Answer(probe.Step.Frame, command, probe.Context);
            // Milestone 8.46b: a choice of exactly one is closed by the select, so the completion is
            // asked for rather than assumed - and a pause holding nothing is the next question.
            if (result.Accepted && result.Frame?.PendingChoice?.ConfirmInvocation != null &&
                result.Frame.PendingChoice.SelectedCandidates.Count > 0)
                result = EffectResolutionSession.Answer(result.Frame, result.Frame.PendingChoice.ConfirmInvocation, probe.Context);
            // Out-of-band board changes are refused by the move handler as an impossible step;
            // the enclosing UpTo policy may finish the effect, but must never move into the occupant.
            Assert.That(result.Events.Single(o => o.NodeId.Value == "move-pyro").ResultKind,
                Is.Not.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(probe.PyroAt, Is.EqualTo(At(1)));
            var other = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1));
            var stale = other.Choice.SelectInvocations.Single(c => c.Candidate.OptionNodeId.Value == "move-both");
            other.Option("move-both");
            Assert.That(EffectResolutionSession.Answer(other.Step.Frame, stale, other.Context).Accepted, Is.False);
        }

        [TestCase(RuleActivity.Skill)]
        [TestCase(RuleActivity.Swap)]
        public void AllAboardDoesNotOfferAnImmuneFriendlyPartner(RuleActivity immunity)
        {
            var probe = new Probe(WidgetEffectContent.AllAboard(Card, "rev-1", false), At(1),
                range: 2, actor: Enemy, immune: Friend, immunity: immunity);
            Assert.That(probe.Choice.SelectInvocations.Select(c => c.Candidate.OptionNodeId.Value), Has.No.Member("friendly"));
            probe.Option("self");
            Assert.That(probe.HeroAt, Is.EqualTo(At(1)));
        }

        [Test]
        public void SafeLandingCanDeclineMoveAndSwapWithSelf()
        {
            var probe = new Probe(WidgetEffectContent.AllAboard(Card, "rev-1", true), At(1), range: 3);
            probe.Answer(probe.Choice.DeclineInvocation);
            probe.Option("self");
            Assert.That(probe.HeroAt, Is.EqualTo(At(1)));
            Assert.That(probe.PyroAt, Is.EqualTo(At(0)));
        }

        [Test]
        public void PyroPersistsWithoutItsOwnerAndAtRoundEnd_ButRemainsAnObstacleAndRemovable()
        {
            var probe = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1));
            var state = probe.Board.CaptureRestoreState();
            var withoutOwner = AdjacentMoveAuthority.Restore(new BoardRestoreState(
                state.HeroOccupancies.Where(h => !h.HeroId.Equals(Actor)), state.ObstacleOccupancies,
                state.RemainingMinions, state.PlacedTokens, state.CurrentBattleZoneId,
                state.MinionBattleExclusions, state.LastCompletedMovementWalk),
                probe.Board.MapDefinition, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(withoutOwner.PlacedTokens.Single().Id, Is.EqualTo(new TokenInstanceId("pyro")));
            Assert.That(ComponentCleanupRules.TokensRemovedAtEndOfRound(withoutOwner.PlacedTokens), Is.Empty);
            Assert.That(withoutOwner.IsEmptyPlayableSpace(At(1)), Is.False);
            withoutOwner.CommitValidatedTokenReclamation(new TokenInstanceId("pyro"));
            Assert.That(withoutOwner.IsEmptyPlayableSpace(At(1)), Is.True);
            Assert.That(probe.Catalogs.Components.CountAvailableTokens(WidgetComponentContent.PyroDefinition.Reference,
                withoutOwner.PlacedTokens), Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void DragonBondPausedSecondMove_SnapshotRoundTripsAndContinuesIdentically(bool heroFirst)
        {
            var probe = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1));
            probe.Option("move-both"); probe.Option(heroFirst ? "hero-first" : "pyro-first");
            probe.Pick(EffectValue.Space(heroFirst ? At(-1) : At(2)));
            var round = probe.AsRound();
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, probe.Board);
            var restored = AuthoritativeSnapshotSerializer.Deserialize(probe.Catalogs, payload, probe.Definitions,
                new[] { probe.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(payload));
            foreach (var branch in new[] { round, restored.Round })
            {
                var choice = branch.CurrentTurnAuthority.PendingEffectChoice;
                var select = choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(EffectValue.Space(heroFirst ? At(0) : At(1))));
                Assert.That(branch.TryAnswerEffectChoice(select).Accepted, Is.True);
                var next = branch.CurrentTurnAuthority.PendingEffectChoice;
                if (next != null && next.ChoiceId.Equals(choice.ChoiceId))
                    Assert.That(branch.TryAnswerEffectChoice(next.ConfirmInvocation).Accepted, Is.True);
            }
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board),
                Is.EqualTo(AuthoritativeSnapshotSerializer.Serialize(round, probe.Board)));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void DragonBondPlacePause_RestoresAndReplaysWithTheSameTokenIdentity(bool alreadyPlaced)
        {
            var effect = WidgetEffectContent.DragonBond(Card, "rev-1");
            var probe = new Probe(effect, alreadyPlaced ? At(1) : (HexCoordinate?)null);
            var replay = new Probe(effect, alreadyPlaced ? At(1) : (HexCoordinate?)null);
            probe.Option("place-pyro"); replay.Option("place-pyro");
            var round = probe.AsRound();
            var replayRound = replay.AsRound();
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, probe.Board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replayRound, replay.Board), Is.EqualTo(payload));
            var restored = AuthoritativeSnapshotSerializer.Deserialize(probe.Catalogs, payload, probe.Definitions,
                new[] { probe.Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            foreach (var branch in new[] { round, replayRound, restored.Round })
            {
                var choice = branch.CurrentTurnAuthority.PendingEffectChoice;
                Assert.That(branch.TryAnswerEffectChoice(choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(EffectValue.Space(At(2))))).Accepted, Is.True);
                var pending = branch.CurrentTurnAuthority.PendingEffectChoice;
                if (pending != null && pending.ChoiceId.Equals(choice.ChoiceId))
                    Assert.That(branch.TryAnswerEffectChoice(pending.ConfirmInvocation).Accepted, Is.True);
            }
            var final = AuthoritativeSnapshotSerializer.Serialize(round, probe.Board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replayRound, replay.Board), Is.EqualTo(final));
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(final));
        }

        [Test]
        public void DragonBondCannotPlaceOnAFullBoard_ButCanChooseZeroMoves()
        {
            var probe = new Probe(WidgetEffectContent.DragonBond(Card, "rev-1"), At(1),
                spaces: new[] { At(0), At(1), At(-4) });
            // The existing feasibility contract conservatively offers a conditional branch.
            // Its mandatory Place still has to refuse a full board without reclaiming Pyro.
            probe.Option("place-pyro");
            Assert.That(probe.Step.IsComplete, Is.True);
            Assert.That(probe.PyroAt, Is.EqualTo(At(1)));
            probe.Start();
            probe.Option("move-both"); probe.Option("hero-first");
            // Single zero-distance candidates may be applied automatically by the shared Move.
            while (probe.Choice != null) probe.Pick(probe.Choices.Single());
            Assert.That(probe.HeroAt, Is.EqualTo(At(0)));
            Assert.That(probe.PyroAt, Is.EqualTo(At(1)));
        }

        internal sealed class Probe
        {
            internal readonly AdjacentMoveAuthority Board;
            internal readonly MatchContentCatalogs Catalogs;
            internal readonly CardDefinition[] Definitions;
            internal readonly CardInstance[] Cards;
            internal readonly MatchParticipants Participants;
            internal readonly EffectResolutionContext Context;
            private readonly HeroId actor;
            internal EffectResolutionStepResult Step;
            internal PendingEffectChoice Choice => Step.Frame?.PendingChoice;
            internal EffectValue[] Choices => Choice.Candidates.Select(c => c.Value).ToArray();
            internal HexCoordinate HeroAt => Board.Occupancies.Single(h => h.HeroId.Equals(actor)).Coordinate;
            internal HexCoordinate PyroAt => Board.PlacedTokens.Single().Coordinate;

            internal Probe(EffectDefinition effect, HexCoordinate? pyro = null, int? range = null, int radius = 2, HeroId actor = default,
                HeroId immune = default, RuleActivity immunity = RuleActivity.None, IEnumerable<HexCoordinate> spaces = null,
                TokenDefinition tokenDefinition = null)
            {
                this.actor = actor.IsDefined ? actor : Actor;
                Participants = actor.Equals(Enemy) ? StagedBoardParticipants.CreateWithBlueAlly() : StagedBoardParticipants.Create();
                var defaults = MatchContentCatalogs.ForThisBuild();
                var capabilities = new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(WidgetComponentContent.DeferredHandlers()));
                tokenDefinition = tokenDefinition ?? WidgetComponentContent.PyroDefinition;
                var components = new ComponentDefinitionCatalog(new[] { tokenDefinition }, Array.Empty<MarkerDefinition>());
                Catalogs = new MatchContentCatalogs(capabilities, components);
                Assert.That(EffectDefinitionValidator.Validate(effect, capabilities), Is.Empty);
                var action = new ActionDefinition(Action, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    new[] { new PrintedValueDefinition(range.HasValue ? PrintedValueChannel.Range : PrintedValueChannel.Radius, range ?? radius) },
                    new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.None, default, effect);
                var definition = new CardDefinition(Card, "rev-1", "TEST - Widget clause", new InitiativeValue(5),
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[] { action });
                Definitions = new[] { definition };
                Cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("red"), definition.Reference, Actor),
                    CardInstance.CreateInHand(new CardInstanceId("blue"), definition.Reference, Enemy)
                };
                var coordinates = spaces ?? Enumerable.Range(-5, 12).SelectMany(q => Enumerable.Range(-5, 11).Select(r => At(q, r)));
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("widget-clause-map"), 1, coordinates),
                    new[] { new HeroOccupancy(this.actor, At(0)), new HeroOccupancy(this.actor.Equals(Actor) ? Enemy : Actor, At(-4)) }
                        .Concat(actor.Equals(Enemy) ? new[] { new HeroOccupancy(Friend, At(0, 2)) } : Array.Empty<HeroOccupancy>()),
                    Array.Empty<ObstacleOccupancy>(), Array.Empty<QuickGameMinionSetupState>());
                if (pyro.HasValue) Board.CommitValidatedTokenPlacement(new PlacedToken(new TokenInstanceId("pyro"),
                    tokenDefinition, this.actor, this.actor, Cards.Single(c => c.OwnerId.Equals(this.actor)).Id, pyro.Value));
                var rules = immunity == RuleActivity.None ? RuleModificationSet.Empty : new RuleModificationSet(new[]
                {
                    AppliedRuleModification.FromActiveEffect(RuleModification.ForImmunity(immunity), RuleObject.ForHero(immune),
                        new ActiveEffectId("fixture-immunity"), immune, TeamSide.Blue, 1)
                }, Array.Empty<AppliedObstacleOverlay>(), new TieBreakerState(TeamSide.Red));
                Context = new EffectResolutionContext(effect, capabilities, Participants, null, 1, 1, rules,
                    EffectCardState.Of(Definitions, Cards), ActionKind.Skill, components: new MatchComponents(components,
                        Board, new MarkerAttachmentRegister(), Board, Board, Board, Board));
                Start();
            }

            internal void Start()
            {
                Step = EffectResolutionSession.Start(new EffectResolutionId("widget-clause-resolution"),
                    Cards.Single(c => c.OwnerId.Equals(actor)).Id, Definitions[0].Reference, Action, actor, actor,
                    ActionInvocationContext.PrintedPrimary, Context);
                Assert.That(Step.Accepted, Is.True);
            }
            internal void Option(string id) => Select(Choice.SelectInvocations.Single(c => c.Candidate.OptionNodeId.Value == id));
            internal void Pick(EffectValue value) => Select(Choice.SelectInvocations.Single(c => c.Candidate.Value.Equals(value)));
            private void Select(EffectChoiceCommand command)
            {
                var id = Choice.ChoiceId;
                Answer(command);
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
                foreach (var card in Cards) Assert.That(round.TryCommitCard(card.OwnerId, card.Id).Accepted, Is.True);
                return round;
            }
        }
    }
}
