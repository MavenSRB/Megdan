using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
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
    public sealed class ResolvedCardDiscardTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly ActionDefinitionId Action = new ActionDefinitionId("discard-resolved-primary");
        private static readonly EffectNodeId Node = new EffectNodeId("discard-resolved-node");
        private static IEffectCapabilityHandler Handler =>
            SyntheticResolvedDiscardCapabilities.RegisteredHandlers().Single();

        [TestCase(CardFaceOrientation.FaceUp)]
        [TestCase(CardFaceOrientation.FaceDown)]
        public void ResolvedDiscard_PreservesPlayedHistoryAndOrientation_AndRetrieves(CardFaceOrientation orientation)
        {
            var card = Card("resolved", Red, new CardStateFacts(
                CardLocation.TurnArea, true, CardResolutionStatus.Resolved, orientation));
            Assert.That(CardLifecycleAuthority.TryDiscardResolvedCard(card), Is.True);
            Assert.That(card.State, Is.EqualTo(new CardStateFacts(
                CardLocation.DiscardPile, true, CardResolutionStatus.Resolved, orientation)));
            var before = card.State;
            Assert.That(CardLifecycleAuthority.TryDiscardResolvedCard(card), Is.False);
            Assert.That(card.State, Is.EqualTo(before));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(card), Is.True);
            Assert.That(card.State, Is.EqualTo(CardStateFacts.InitialInHand()));
        }

        [Test]
        public void DiscardChoice_IsOwnedCanonicalAndRejectsStaleForeignOrUnresolvedCards()
        {
            var first = Resolved("a", Red);
            var second = Resolved("z", Red);
            var foreign = Resolved("b", Blue);
            var hand = Card("hand", Red, CardStateFacts.InitialInHand());
            var unresolved = Card("unresolved", Red, new CardStateFacts(
                CardLocation.TurnArea, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp));
            var cards = new[] { second, foreign, hand, unresolved, first };
            var context = Context(cards);
            var offered = Handler.Execute(context);
            Assert.That(offered.Kind, Is.EqualTo(EffectExecutionResultKind.NeedsChoice));
            Assert.That(offered.Choice.ChooserHeroId, Is.EqualTo(Red));
            Assert.That(offered.Choice.Candidates.Select(value => value.CardInstanceId.Value),
                Is.EqualTo(new[] { "a", "z" }));
            foreach (var invalid in new[] { foreign, hand, unresolved })
            {
                var before = invalid.State;
                Assert.That(Handler.Resume(context, new[] { EffectValue.Card(invalid.Id) }).Kind,
                    Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
                Assert.That(invalid.State, Is.EqualTo(before));
                if (invalid != foreign)
                    Assert.That(CardLifecycleAuthority.TryDiscardResolvedCard(invalid), Is.False);
            }
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(first), Is.True);
            Assert.That(Handler.Resume(context, new[] { EffectValue.Card(first.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
            var result = Handler.Resume(context, new[] { EffectValue.Card(second.Id) });
            Assert.That(result.Outcomes.Single().Value.CardInstanceId, Is.EqualTo(second.Id));
            Assert.That(Handler.Anticipate(context), Is.EqualTo(EffectExecutionResultKind.ImpossibleMandatory));
        }

        [Test]
        public void AppliedDiscard_CancelsOnlyItsSource_AndRestoresTheCancellation()
        {
            var card = Resolved("chosen", Red);
            var other = Resolved("other", Red);
            var timing = new ActiveEffectLifecycle();
            foreach (var source in new[] { card, other })
            {
                var definition = new ActiveEffectDefinition("discard-test-aura-" + source.Id.Value,
                    ActiveEffectAffectedScopeKind.ControllerHero, ActiveEffectDurationKind.ThisRound,
                    TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted, TimingCheckpoint.RoundEnd,
                    new[] { ActiveEffectCancellationCondition.SourceCardStateChanged }, false,
                    "discard-test", "discard-test-" + source.Id.Value, Array.Empty<TimingCheckpoint>(),
                    new[] { RuleModification.ForRestriction(RuleActivity.FastTravel) });
                timing.Create(new ActiveEffectId(source.Id.Value), definition, Red, TeamSide.Red,
                    default, source.Id, Action, 1, 1);
            }
            Assert.That(Handler.Resume(Context(new[] { card, other }, timing),
                new[] { EffectValue.Card(card.Id) }).Kind, Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(timing.Effects.Single(value => value.SourceCardInstanceId.Equals(card.Id)).EndedAt,
                Is.EqualTo(TimingCheckpoint.SourceCardStateChanged));
            Assert.That(timing.Effects.Single(value => value.SourceCardInstanceId.Equals(other.Id)).Phase,
                Is.EqualTo(ActiveEffectPhase.Active));
            Assert.That(timing.CancelForSourceCardStateChange(card.Id, 1, 1), Is.Empty);
            var restored = ActiveEffectLifecycle.Restore(timing.CaptureRestoreState());
            Assert.That(restored.Effects.Select(value => value.Phase),
                Is.EqualTo(timing.Effects.Select(value => value.Phase)));
        }

        [TestCase(RosterCardRetrievalKind.OwnedDiscardedCard)]
        [TestCase(RosterCardRetrievalKind.OwnedResolvedOrDiscardedBasicSkill)]
        public void ExplicitRetrieval_AdmitsPlayedDiscard_ButRejectsMalformedAndOtherOwners(
            RosterCardRetrievalKind kind)
        {
            var card = Resolved("played-discard", Red);
            var other = Resolved("foreign", Blue);
            Assert.That(CardLifecycleAuthority.TryDiscardResolvedCard(card), Is.True);
            Assert.That(CardLifecycleAuthority.TryDiscardResolvedCard(other), Is.True);
            var malformed = Card("malformed", Red, new CardStateFacts(
                CardLocation.DiscardPile, true, CardResolutionStatus.Unresolved, CardFaceOrientation.FaceUp));
            var inputs = new Dictionary<string, EffectValue>
            {
                ["owner"] = EffectValue.Hero(Red), ["chooser"] = EffectValue.Hero(Red),
                ["kind"] = EffectValue.Integer((int)kind)
            };
            var context = Context(new[] { malformed, other, card }, inputs: inputs);
            var retrieve = SyntheticRosterCapabilities.Handlers().Single(value =>
                value.Reference.Equals(SyntheticRosterCapabilities.RetrieveCardV1));
            Assert.That(retrieve.Execute(context).Choice.Candidates.Select(value => value.CardInstanceId),
                Is.EqualTo(new[] { card.Id }));
            Assert.That(retrieve.Resume(context, new[] { EffectValue.Card(card.Id) }).Kind,
                Is.EqualTo(EffectExecutionResultKind.Applied));
            Assert.That(card.State, Is.EqualTo(CardStateFacts.InitialInHand()));
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(malformed), Is.False);
        }

        [Test]
        public void NewContract_IsRegisteredWithItsConfiguredConsumer()
        {
            Assert.That(SyntheticResolvedDiscardCapabilities.DeferredHandlers(), Is.Empty);
            Assert.That(MatchContentCatalogs.ForThisBuild().Capabilities.Registered,
                Has.Some.EqualTo(SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1));
        }

        [Test]
        public void PendingDiscard_RoundTripsAndContinuesIdentically_WithPlayedDiscardInSnapshot()
        {
            var participants = StagedBoardParticipants.Create();
            var definition = Definition();
            var red = Card("source", Red, CardStateFacts.InitialInHand());
            var blue = Card("blue", Blue, CardStateFacts.InitialInHand());
            var resolved = Resolved("prior-resolved", Red);
            var cards = new[] { red, blue, resolved };
            var definitions = new[] { definition };
            var defaults = MatchContentCatalogs.ForThisBuild();
            var catalog = new EffectCapabilityCatalog(defaults.Capabilities.Registered
                .Select(defaults.Capabilities.Load).Concat(SyntheticResolvedDiscardCapabilities.DeferredHandlers()));
            var catalogs = new MatchContentCatalogs(catalog, defaults.Components);
            var frame = EffectResolutionSession.Start(new EffectResolutionId("discard-resolution"),
                red.Id, definition.Reference, Action, Red, Red, ActionInvocationContext.PrintedPrimary,
                new EffectResolutionContext(definition.ActionDefinitions.Single().EffectRoot,
                    catalog, participants, null, 1, 1, RuleModificationSet.Empty,
                    EffectCardState.Of(definitions, cards), ActionKind.Skill));
            var board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(
                new MapId("resolved-discard-map"), 1, new[] { new HexCoordinate(0, 0), new HexCoordinate(1, 0) }),
                new[] { new HeroOccupancy(Red, new HexCoordinate(0, 0)),
                    new HeroOccupancy(Blue, new HexCoordinate(1, 0)) }, Array.Empty<ObstacleOccupancy>());
            var ledger = new HeroCoinLedger(participants);
            var actions = CardActionResolutionAuthority.Restore(catalogs, board,
                CardActionResolutionRestoreState.WithPendingSkill(frame.Frame,
                    HeroCoinLedgerRestoreStateHolder.Present(ledger.CaptureRestoreState())),
                null, null, null, definitions, cards, participants, new MarkerAttachmentRegister(),
                RuleModificationSet.Empty);
            var round = new RoundResolutionAuthority(definitions, cards, participants,
                new TieBreakerState(TeamSide.Red), 1, RoundResolutionComposition.ForCardActionsOnly(actions));
            Assert.That(round.TryCommitCard(Red, red.Id).Accepted, Is.True);
            Assert.That(round.TryCommitCard(Blue, blue.Id).Accepted, Is.True);
            var payload = AuthoritativeSnapshotSerializer.Serialize(round, board);
            var replay = AuthoritativeSnapshotSerializer.Deserialize(catalogs, payload, definitions,
                new[] { board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board), Is.EqualTo(payload));
            foreach (var branch in new[] { round, replay.Round })
            {
                // Milestone 8.46b: the discard names exactly one card, so the select is the whole
                // answer and re-sending it is a stale command against a question nobody is asking.
                var choice = branch.CurrentTurnAuthority.PendingEffectChoice;
                var select = choice.SelectInvocations.Single();
                Assert.That(branch.TryAnswerEffectChoice(select).Accepted, Is.True);
                Assert.That(branch.TryAnswerEffectChoice(select).Accepted, Is.False);
            }
            var after = AuthoritativeSnapshotSerializer.Serialize(round, board);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(replay.Round, replay.Board), Is.EqualTo(after));
            var afterRestore = AuthoritativeSnapshotSerializer.Deserialize(catalogs, after, definitions,
                new[] { board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(afterRestore.Round, afterRestore.Board),
                Is.EqualTo(after));
            Assert.That(resolved.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(resolved.State.HasBeenPlayed, Is.True);
        }

        private static EffectCapabilityContext Context(CardInstance[] cards,
            ActiveEffectLifecycle timing = null, IReadOnlyDictionary<string, EffectValue> inputs = null) =>
            new EffectCapabilityContext(Node, EffectFailurePolicy.Mandatory, Red, Red,
                new CardInstanceId("source"), Definition().Reference, Action,
                inputs ?? new Dictionary<string, EffectValue>(), 1, 1, StagedBoardParticipants.Create(),
                timing, RuleModificationSet.Empty, EffectCardState.Of(new[] { Definition() }, cards), ActionKind.Skill);

        private static CardInstance Resolved(string id, HeroId owner) => Card(id, owner,
            new CardStateFacts(CardLocation.TurnArea, true, CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp));
        private static CardInstance Card(string id, HeroId owner, CardStateFacts state) =>
            new CardInstance(new CardInstanceId(id), Definition().Reference, owner, state);
        private static CardDefinition Definition() => new CardDefinition(
            new CardDefinitionId("resolved-discard-probe"), "rev-1", "TEST - resolved discard",
            new InitiativeValue(5), CardContentAuthority.SyntheticTestFixture,
            CardProgressionProfile.BasicCard(CardColor.Silver), new[]
            {
                new ActionDefinition(Action, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                    Array.Empty<PrintedValueDefinition>(), new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                    AttackRangeKind.None, default, new EffectDefinition(new EffectDefinitionId("resolved-discard-effect"),
                        "rev-1", Node, new[] { new EffectNodeDefinition(Node, EffectNodeKind.Capability,
                            EffectFailurePolicy.Mandatory, SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1,
                            outputs: new[] { new EffectOutputBinding("discarded", EffectValueKind.CardInstance) }) }))
            });
    }
}
