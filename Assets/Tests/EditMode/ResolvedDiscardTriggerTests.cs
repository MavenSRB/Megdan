using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Diagnostics;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
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
using GuardsDigital.Core.Snapshots;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    public sealed class ResolvedDiscardTriggerTests
    {
        private static readonly HeroId Red = new HeroId(StagedBoardParticipants.RedHeroIdValue);
        private static readonly HeroId Blue = new HeroId(StagedBoardParticipants.BlueHeroIdValue);
        private static readonly ActionDefinitionId Primary = new ActionDefinitionId("discard-trigger-primary");
        private static EffectNodeId Id(string value) => new EffectNodeId(value);

        [TestCase(true)]
        [TestCase(false)]
        public void DiscardInterrupt_DeclineOrAccept_ThenResumesTheNextClause(bool accept)
        {
            var f = new Fixture();
            f.Discard(f.Borrowed);
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("may-perform-primary"));
            Assert.That(f.Choice.ChooserId, Is.EqualTo(Red));
            f.Answer(accept ? f.Choice.ConfirmInvocation : f.Choice.DeclineInvocation);
            if (accept)
            {
                Assert.That(f.Choice.NodeId.Value, Is.EqualTo("borrowed-target"));
                Assert.That(f.Authority.HasBorrowedSkillParent, Is.True);
                f.Pick(EffectValue.Hero(Blue));
            }
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.DiscardPile));
            Assert.That(f.Borrowed.State.HasBeenPlayed, Is.True);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(f.Authority.HasBorrowedSkillParent, Is.False);
        }

        [TestCase(false, false)]
        [TestCase(true, true)]
        public void LockedOrForeignUltimate_DoesNotTrigger(bool unlocked, bool foreign)
        {
            var f = new Fixture(unlocked, foreign);
            f.Discard(f.Borrowed);
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
        }

        [Test]
        public void TriggerAndBorrowedPause_RoundTripAndContinueIdentically()
        {
            var f = new Fixture();
            f.Discard(f.Borrowed);
            f.RoundTrip();
            f.Answer(f.Choice.ConfirmInvocation);
            f.RoundTrip();
            f.Pick(EffectValue.Hero(Blue));
            f.RoundTrip();
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Borrowed.State.Location, Is.EqualTo(CardLocation.DiscardPile));
        }

        [Test]
        public void NestedDiscardGrants_UnwindInPrintedOrder()
        {
            var f = new Fixture(nested: true);
            f.Discard(f.Nested);
            f.Answer(f.Choice.ConfirmInvocation);
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("discard"));
            f.Discard(f.Borrowed);
            f.RoundTrip();
            f.Answer(f.Choice.ConfirmInvocation);
            f.RoundTrip();
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("borrowed-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Authority.HasBorrowedSkillParent, Is.False);
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void ActualDiscardFact_IsRequired_HandDiscardAndStateCopiesDoNotPublishIt()
        {
            var f = new Fixture();
            var root = f.Definitions[0].ActionDefinitions.Single().EffectRoot;
            Assert.That(ResolvedCardDiscardQuery.Requested(root,
                new EffectNodeOutcome(Id("parent-target"), EffectExecutionResultKind.Applied,
                    new[] { new EffectOutcomeFact("discarded", EffectValue.Card(f.Borrowed.Id)) })).IsDefined, Is.False);
            Assert.That(ResolvedCardDiscardQuery.Requested(root,
                new EffectNodeOutcome(Id("discard"), EffectExecutionResultKind.ImpossibleMandatory,
                    Array.Empty<EffectOutcomeFact>())).IsDefined, Is.False);
            Assert.That(ResolvedCardDiscardTriggerQuery.Derive(UltimateUnlockQuery.Derive(f.Definitions, f.Cards),
                f.Board, f.Borrowed.Id, f.Cards), Is.Null, "A resolved turn-area card was not discarded.");
            var handCard = CardInstance.CreateInHand(new CardInstanceId("hand-discard"), f.Borrowed.DefinitionReference, Red);
            Assert.That(CardLifecycleAuthority.TryDiscardFromHand(handCard), Is.True);
            Assert.That(ResolvedCardDiscardTriggerQuery.Derive(UltimateUnlockQuery.Derive(f.Definitions, f.Cards),
                f.Board, handCard.Id, f.Cards.Concat(new[] { handCard })), Is.Null,
                "Hand/Defense discard does not carry resolved played identity.");
        }

        [Test]
        public void DiscardCheckpoint_StopsAfterSourceCancellation_BeforeTheNextNode()
        {
            var f = new Fixture();
            var timing = new ActiveEffectLifecycle();
            var aura = new ActiveEffectDefinition("discard-aura", ActiveEffectAffectedScopeKind.ControllerHero,
                ActiveEffectDurationKind.ThisRound, TimingCheckpoint.ActionStarted, TimingCheckpoint.ActionStarted,
                TimingCheckpoint.RoundEnd, new[] { ActiveEffectCancellationCondition.SourceCardStateChanged },
                false, "discard-aura", "discard-aura", Array.Empty<TimingCheckpoint>(),
                new[] { RuleModification.ForRestriction(RuleActivity.FastTravel) });
            timing.Create(new ActiveEffectId("discard-aura-instance"), aura, Red, TeamSide.Red, default,
                f.Borrowed.Id, Primary, 1, 1);
            var root = f.Definitions[0].ActionDefinitions.Single().EffectRoot;
            var context = new EffectResolutionContext(root, f.Catalogs.Capabilities, StagedBoardParticipants.Create(),
                timing, 1, 1, RuleModificationSet.Empty, EffectCardState.Of(f.Definitions, f.Cards), ActionKind.Skill,
                watchesResolvedDiscards: true);
            var step = EffectResolutionSession.Start(new EffectResolutionId("cancellation-probe"), f.Source.Id,
                f.Source.DefinitionReference, Primary, Red, Red, ActionInvocationContext.PrintedPrimary, context);
            step = EffectResolutionSession.Answer(step.Frame, step.Frame.PendingChoice.SelectInvocations.Single(), context);
            if (!step.ResolvedCardDiscarded.IsDefined)
                step = EffectResolutionSession.Answer(step.Frame, step.Frame.PendingChoice.ConfirmInvocation, context);
            Assert.That(step.ResolvedCardDiscarded, Is.EqualTo(f.Borrowed.Id));
            Assert.That(timing.Effects.Single().EndedAt, Is.EqualTo(TimingCheckpoint.SourceCardStateChanged));
            Assert.That(step.Frame.CompletedOutcomes.Select(value => value.NodeId), Has.None.EqualTo(Id("parent-target")));
        }

        [Test]
        public void GrantedAttackWithNoTarget_StillResumesItsParent()
        {
            var f = new Fixture(attack: true);
            f.Discard(f.Borrowed);
            f.Answer(f.Choice.ConfirmInvocation);
            Assert.That(f.Choice, Is.Not.Null, "An impossible granted action must not strand its suspended parent.");
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Source.State.ResolutionStatus, Is.EqualTo(CardResolutionStatus.Resolved));
        }

        [Test]
        public void GrantedPrimary_PreservesItsOrdinaryRepeatBeforeResumingParent()
        {
            var f = new Fixture(repeat: true);
            f.Discard(f.Borrowed);
            f.Answer(f.Choice.ConfirmInvocation);
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("repeat"));
            f.RoundTrip();
            f.Answer(f.Choice.ConfirmInvocation);
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("borrowed-target"));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
            f.Pick(EffectValue.Hero(Blue));
        }

        [Test]
        public void OnBoardApplicability_AndNamedCardIdentity_AreLive()
        {
            var f = new Fixture();
            f.Discard(f.Borrowed);
            var ultimates = UltimateUnlockQuery.Derive(f.Definitions, f.Cards);
            Assert.That(ResolvedCardDiscardTriggerQuery.Derive(ultimates, f.Board, f.Borrowed.Id, f.Cards), Is.Not.Null);
            var offBoard = new AdjacentMoveAuthority(f.Board.MapDefinition,
                new[] { new HeroOccupancy(Blue, new HexCoordinate(1, 0)) }, Array.Empty<ObstacleOccupancy>());
            Assert.That(ResolvedCardDiscardTriggerQuery.Derive(ultimates, offBoard, f.Borrowed.Id, f.Cards), Is.Null);
            Assert.That(CardLifecycleAuthority.TryRetrieveEligibleCard(f.Borrowed), Is.True);
            // The trigger cannot substitute some other card if its named card is no longer discarded.
            f.Answer(f.Choice.ConfirmInvocation);
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
        }

        [Test]
        public void CommandsReplayAndRetainedPrefixes_ReproduceEveryPauseAndOrderedEvent()
        {
            var live = new Fixture(nested: true);
            live.Discard(live.Nested);
            live.Answer(live.Choice.ConfirmInvocation);
            live.Discard(live.Borrowed);
            live.Answer(live.Choice.ConfirmInvocation);
            live.Pick(EffectValue.Hero(Blue));
            live.Pick(EffectValue.Hero(Blue));
            live.Pick(EffectValue.Hero(Blue));
            for (var prefix = 0; prefix <= live.Commands.Count; prefix++)
            {
                var replay = new Fixture(nested: true);
                for (var index = 0; index < live.Commands.Count; index++)
                {
                    if (index == prefix) replay.RoundTrip();
                    replay.Answer(live.Commands[index]);
                    Assert.That(replay.Snapshots[index], Is.EqualTo(live.Snapshots[index]), "prefix " + prefix);
                    Assert.That(replay.EventPrefixes[index], Is.EqualTo(live.EventPrefixes[index]), "ordered facts");
                }
            }
        }

        [Test]
        public void WrongChooserAndStaleAcceptance_AreAtomic_AndCannotGrantTwice()
        {
            var f = new Fixture();
            f.Discard(f.Borrowed);
            var command = f.Choice.ConfirmInvocation;
            var bytes = AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board);
            Assert.That(f.Round.TryAnswerEffectChoice(new EffectChoiceCommand(command.ResolutionId,
                command.ChoiceId, command.Kind, answeringHeroId: Blue)).Accepted, Is.False);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board), Is.EqualTo(bytes));
            f.Answer(command);
            bytes = AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board);
            Assert.That(f.Round.TryAnswerEffectChoice(command).Accepted, Is.False);
            Assert.That(AuthoritativeSnapshotSerializer.Serialize(f.Round, f.Board), Is.EqualTo(bytes));
            f.Pick(EffectValue.Hero(Blue));
            Assert.That(f.Choice.NodeId.Value, Is.EqualTo("parent-target"));
        }

        private sealed class Fixture
        {
            internal CardDefinition[] Definitions;
            internal CardInstance[] Cards;
            internal AdjacentMoveAuthority Board;
            internal RoundResolutionAuthority Round;
            internal MatchContentCatalogs Catalogs;
            internal CardActionResolutionAuthority Authority;
            internal CardInstance Source => Cards.Single(card => card.Id.Value == "source");
            internal CardInstance Borrowed => Cards.Single(card => card.Id.Value == "borrowed");
            internal CardInstance Nested => Cards.Single(card => card.Id.Value == "nested");
            internal PendingEffectChoice Choice => Round.CurrentTurnAuthority.PendingEffectChoice;
            internal readonly List<EffectChoiceCommand> Commands = new List<EffectChoiceCommand>();
            internal readonly List<string> Snapshots = new List<string>();
            internal readonly List<RuleEventLogEntry[]> EventPrefixes = new List<RuleEventLogEntry[]>();
            private readonly RuleEventLogProjection log = new RuleEventLogProjection();
            internal Fixture(bool unlocked = true, bool foreign = false, bool nested = false,
                bool attack = false, bool repeat = false)
            {
                var parentEffect = new EffectDefinition(new EffectDefinitionId("discard-parent"), "rev-1", Id("sequence"),
                    new[]
                    {
                        new EffectNodeDefinition(Id("sequence"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { Id("discard"), Id("parent-target") }),
                        new EffectNodeDefinition(Id("discard"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                            SyntheticResolvedDiscardCapabilities.DiscardResolvedCardV1,
                            outputs: new[] { new EffectOutputBinding("discarded", EffectValueKind.CardInstance) }),
                        Target("parent-target")
                    });
                var borrowedEffect = new EffectDefinition(new EffectDefinitionId("borrowed-text"), "rev-1",
                    Id("borrowed-target"), new[] { Target("borrowed-target") });
                if (repeat) borrowedEffect = new EffectDefinition(new EffectDefinitionId("borrowed-text"), "rev-1",
                    Id("borrowed-sequence"), new[]
                    {
                        new EffectNodeDefinition(Id("borrowed-sequence"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                            childNodeIds: new[] { Id("borrowed-target"), Id("repeat") }),
                        Target("borrowed-target"),
                        new EffectNodeDefinition(Id("repeat"), EffectNodeKind.RepeatAction, EffectFailurePolicy.May,
                            chooser: EffectChooserRole.Actor, maximumRepeats: 1)
                    });
                var parent = Definition("parent", parentEffect, 7);
                var borrowed = Definition("borrowed-definition", borrowedEffect, 6);
                if (attack) borrowed = new CardDefinition(new CardDefinitionId("borrowed-attack"), "rev-1", "TEST - attack",
                    new InitiativeValue(6), CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Gold),
                    new[] { new ActionDefinition(Primary, ActionKind.Attack, ActionDefinitionOrigin.PrintedPrimary,
                        new[] { new PrintedValueDefinition(PrintedValueChannel.Attack, 3) },
                        new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true), AttackRangeKind.NonRanged, default) });
                var ultimateId = new CardDefinitionId("discard-ultimate");
                var trigger = GarrusEffectContent.ResolvedDiscardPrimary(ultimateId, "rev-1");
                var ultimate = new CardDefinition(ultimateId, "rev-1", "TEST - discard trigger", default,
                    CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.UltimateCard(),
                    Array.Empty<ActionDefinition>(), UltimateRuleDefinition.TriggeredAbility("discard-trigger-rule",
                        "DiscardTrigger", trigger.Reference, UltimateTriggerCondition.AfterOwnedResolvedCardDiscarded,
                        new[] { TimingCheckpoint.ResolvedCardDiscarded }, Array.Empty<PrintedValueDefinition>()));
                Definitions = new[] { parent, borrowed, ultimate };
                var ultimateCard = CardInstance.CreateInDeck(new CardInstanceId("ultimate"), ultimate.Reference, foreign ? Blue : Red);
                if (unlocked) Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimateCard), Is.True);
                Cards = new[]
                {
                    CardInstance.CreateInHand(new CardInstanceId("source"), parent.Reference, Red),
                    CardInstance.CreateInHand(new CardInstanceId("blue"), borrowed.Reference, Blue),
                    new CardInstance(new CardInstanceId("borrowed"), borrowed.Reference, Red, Resolved()),
                    ultimateCard
                }.Concat(nested ? new[] { new CardInstance(new CardInstanceId("nested"), parent.Reference, Red, Resolved()) }
                    : Array.Empty<CardInstance>()).ToArray();
                var defaults = MatchContentCatalogs.ForThisBuild();
                var catalog = new EffectCapabilityCatalog(defaults.Capabilities.Registered.Select(defaults.Capabilities.Load)
                    .Concat(SyntheticResolvedDiscardCapabilities.DeferredHandlers())
                    .Concat(SyntheticNamedBorrowCapabilities.DeferredHandlers()));
                Catalogs = new MatchContentCatalogs(catalog, defaults.Components,
                    new TriggeredEffectCatalog(defaults.TriggeredEffects.Definitions.Concat(new[] { trigger })));
                foreach (var effect in new[] { parentEffect, borrowedEffect, trigger })
                    Assert.That(EffectDefinitionValidator.Validate(effect, catalog), Is.Empty);
                var participants = StagedBoardParticipants.Create();
                Board = new AdjacentMoveAuthority(BoardMapDefinition.CreateSynthetic(new MapId("discard-trigger-map"), 1,
                        Enumerable.Range(-3, 7).SelectMany(q => Enumerable.Range(-3, 7).Select(r => new HexCoordinate(q, r)))),
                    new[] { new HeroOccupancy(Red, new HexCoordinate(0, 0)), new HeroOccupancy(Blue, new HexCoordinate(attack ? 3 : 1, 0)) },
                    Array.Empty<ObstacleOccupancy>());
                var authority = new CardActionResolutionAuthority(Catalogs, Board);
                Authority = authority;
                Round = new RoundResolutionAuthority(Definitions, Cards, participants, new TieBreakerState(TeamSide.Red),
                    1, RoundResolutionComposition.ForCardActionsOnly(authority));
                Assert.That(Round.TryCommitCard(Red, Source.Id).Accepted, Is.True);
                Assert.That(Round.TryCommitCard(Blue, Cards[1].Id).Accepted, Is.True);
                var offer = ActionOfferQuery.GetAvailableTurnActionOffers(Definitions, Cards, Red, Source.Id)
                    .Single(value => value.ActionDefinitionId.Equals(Primary));
                var started = Round.TryStartSkill(new ActionResolutionId("discard-root"), offer);
                Assert.That(started.Accepted, Is.True, started.RejectionReason.ToString());
            }
            internal void Discard(CardInstance card) => Pick(EffectValue.Card(card.Id));
            internal void Pick(EffectValue value)
            {
                var choice = Choice;
                Answer(choice.SelectInvocations.Single(command => command.Candidate.Value.Equals(value)));
                if (Choice != null && Choice.ChoiceId.Equals(choice.ChoiceId)) Answer(Choice.ConfirmInvocation);
            }
            internal void Answer(EffectChoiceCommand command)
            {
                var result = Round.TryAnswerEffectChoice(command);
                Assert.That(result.Accepted, Is.True, result.RejectionReason + "/" + result.TurnRejectionReason + "/" + result.CardActionRejectionReason);
                Commands.Add(command);
                Snapshots.Add(AuthoritativeSnapshotSerializer.Serialize(Round, Board));
                log.Append(result.Events);
                EventPrefixes.Add(log.Entries.ToArray());
            }
            internal void RoundTrip()
            {
                var bytes = AuthoritativeSnapshotSerializer.Serialize(Round, Board);
                var restored = AuthoritativeSnapshotSerializer.Deserialize(Catalogs, bytes, Definitions,
                    new[] { Board.MapDefinition }, Array.Empty<QuickGameMinionSetupState>());
                Assert.That(AuthoritativeSnapshotSerializer.Serialize(restored.Round, restored.Board), Is.EqualTo(bytes));
                Round = restored.Round;
                Board = restored.Board;
                Cards = restored.Cards.ToArray();
                Authority = restored.CardActions;
            }
        }

        private static CardStateFacts Resolved() => new CardStateFacts(CardLocation.TurnArea, true,
            CardResolutionStatus.Resolved, CardFaceOrientation.FaceUp);
        private static CardDefinition Definition(string id, EffectDefinition effect, int initiative) =>
            new CardDefinition(new CardDefinitionId(id), "rev-1", "TEST - " + id, new InitiativeValue(initiative),
                CardContentAuthority.SyntheticTestFixture, CardProgressionProfile.BasicCard(CardColor.Silver), new[]
                {
                    new ActionDefinition(Primary, ActionKind.Skill, ActionDefinitionOrigin.PrintedPrimary,
                        Array.Empty<PrintedValueDefinition>(), new ActionDefinitionPolicy(CardTextApplicationPolicy.Applies, false, true),
                        AttackRangeKind.None, default, effect)
                });
        private static EffectNodeDefinition Target(string id) =>
            new EffectNodeDefinition(Id(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticRosterCapabilities.ChooseTargetV2, inputs: new[]
                {
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.OriginParameter, RosterQueryOrigin.Actor),
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.UnitKindParameter, RosterUnitKind.Hero),
                    EffectInputBinding.FromClosedVocabulary(SyntheticRosterCapabilities.RelationParameter, SourceTeamRelation.OpposingSource),
                    EffectInputBinding.FromLiteral(SyntheticRosterCapabilities.DistanceParameter, EffectValue.Integer(3)),
                    EffectInputBinding.FromClosedVocabulary("activity", RuleActivity.Skill)
                }, outputs: new[] { new EffectOutputBinding("chosenHero", EffectValueKind.Hero) });
    }
}
