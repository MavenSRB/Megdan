using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Rules.Progression;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.75a. Chaos Incarnate, Ignatia the Mad's p.16 permanent ability: "The first time
    /// each turn after you perform a primary action you may flip the Tie Breaker coin; if you do, you
    /// may perform that action again, choosing different targets."
    ///
    /// <para>Every case below fails for a reason the printed sentence names. Nothing of hers is
    /// registered - milestone 7.9's gate - so each frame is driven over a catalog built from
    /// <see cref="IgnatiaComponentContent.DeferredHandlers"/>, which is how her content is exercised
    /// before the dealing block moves it across.</para>
    /// </summary>
    public sealed class IgnatiaChaosIncarnateTests
    {
        private static string Revision => IgnatiaCardContent.ContentRevision;

        // -------------------------------------------------------------------------------------
        // The declaration
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheUltimateIsATriggeredAbilityOnCuttersOwnConditionAndCheckpoint()
        {
            var ultimate = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
            var rule = ultimate.UltimateRule;

            Assert.That(rule, Is.Not.Null);
            Assert.That(rule.RuleId, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRuleId));
            Assert.That(rule.RuleKind, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRuleKind));
            Assert.That(rule.TriggeredEffect.HasValue, Is.True,
                "\"after you perform a primary action ... you may\" is an ability that RUNS, so the " +
                "Ultimate names the body it runs.");
            Assert.That(
                rule.TriggeredEffect.Value,
                Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateTriggeredEffect(Revision).Reference));
            Assert.That(rule.TriggerCondition,
                Is.EqualTo(UltimateTriggerCondition.AfterControllerPerformsPrimaryAction),
                "\"after you perform a primary action\" is the condition milestone 8.39f1 minted " +
                "because the five before it are matched against a fact undefined unless the card is " +
                "p.9-Basic - and most of what she plays at Tier IV is not.");
            Assert.That(rule.TriggerCheckpoints.Count, Is.EqualTo(1));
            Assert.That(rule.TriggerCheckpoints[0], Is.EqualTo(TimingCheckpoint.ActionEnded));
            Assert.That(rule.PrintedValues.Count, Is.EqualTo(0),
                "the face prints no Initiative, no action and neither Range nor Radius.");
            Assert.That(ultimate.SourceContent.PrintedText,
                Does.StartWith("The first time each turn after you perform a primary action"));
        }

        [Test]
        public void TheBodyIsAnOfferWhoseRepeatLivesInsideIt()
        {
            var body = IgnatiaEffectContent.ChaosIncarnateTriggeredEffect(Revision);
            var flip = body.GetNode(new EffectNodeId(IgnatiaEffectContent.ChaosIncarnateFlipNode));
            var then = body.GetNode(new EffectNodeId(IgnatiaEffectContent.ChaosIncarnateThenNode));
            var coin = body.GetNode(new EffectNodeId(IgnatiaEffectContent.ChaosIncarnateCoinNode));
            var repeat = body.GetNode(new EffectNodeId(IgnatiaEffectContent.ChaosIncarnateRepeatNode));

            Assert.That(body.RootNodeId, Is.EqualTo(flip.Id),
                "\"you may flip the Tie Breaker coin\" is the whole ability's one offer, so it is " +
                "the root - the repeat after the semicolon is conditional on it.");
            Assert.That(flip.Kind, Is.EqualTo(EffectNodeKind.Optional));
            Assert.That(flip.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
            Assert.That(flip.Chooser, Is.EqualTo(EffectChooserRole.Actor),
                "p.10 gives an unassigned decision to the card's controller, and this one is " +
                "unassigned.");
            Assert.That(flip.ChildNodeIds.Count, Is.EqualTo(1));
            Assert.That(flip.ChildNodeIds[0], Is.EqualTo(then.Id));

            Assert.That(then.Kind, Is.EqualTo(EffectNodeKind.Sequence));
            Assert.That(then.ChildNodeIds.ToArray(), Is.EqualTo(new[] { coin.Id, repeat.Id }),
                "the flip runs before the repeat, because the repeat runs the OTHER of her two " +
                "printed lines.");

            Assert.That(coin.Kind, Is.EqualTo(EffectNodeKind.Capability));
            Assert.That(coin.Capability, Is.EqualTo(IgnatiaComponentContent.FlipTieBreakerV1));
            Assert.That(coin.FailurePolicy, Is.EqualTo(EffectFailurePolicy.Mandatory),
                "taking the offer flips the coin; there is no second question, because p.5's coin " +
                "always has a face to turn over.");

            Assert.That(repeat.Kind, Is.EqualTo(EffectNodeKind.RepeatAction));
            Assert.That(repeat.FailurePolicy, Is.EqualTo(EffectFailurePolicy.May));
            Assert.That(repeat.Chooser, Is.EqualTo(EffectChooserRole.Actor));
            Assert.That(repeat.MaximumRepeats, Is.EqualTo(1), "\"perform that action AGAIN\" - once.");
            Assert.That(repeat.RepeatTargetBinding, Is.EqualTo(RepeatTargetBinding.DifferentTarget),
                "\"choosing different targets\" is the member milestone 8.10 minted for Thunder " +
                "Boomerang; ID-Q6a keeps it rather than DifferentSelectionSubject, which excludes a " +
                "selection subject captured onto a frame an after-action repeat does not carry.");
        }

        [Test]
        public void TheBodyIsValidAgainstHerOwnDeferredHandlers()
        {
            var body = IgnatiaEffectContent.ChaosIncarnateTriggeredEffect(Revision);
            var rejections = EffectDefinitionValidator.Validate(body, Catalog()).ToArray();
            Assert.That(rejections.Length, Is.EqualTo(0), string.Join(" | ", rejections));

            Assert.That(
                IgnatiaEffectContent.TriggeredEffects(Revision).Count, Is.EqualTo(1),
                "one face of hers prints a permanent ability.");
            Assert.That(
                () => new TriggeredEffectCatalog(IgnatiaEffectContent.TriggeredEffects(Revision)),
                Throws.Nothing,
                "a triggered catalog admits a RepeatAction inside a triggered body since 8.23.");
        }

        [Test]
        public void BothConfirmOrDeclineControlsAreGivenWords()
        {
            var body = IgnatiaEffectContent.ChaosIncarnateTriggeredEffect(Revision);
            var owed = DecisionLabelRequirementQuery.For(new[] { body })
                .Where(requirement => requirement.OpensConfirmOrDecline)
                .Select(requirement => requirement.NodeId.Value)
                .ToArray();

            Assert.That(owed, Has.Member(IgnatiaEffectContent.ChaosIncarnateFlipNode));
            Assert.That(owed, Has.Member(IgnatiaEffectContent.ChaosIncarnateRepeatNode));

            var labelled = IgnatiaEffectContent.UltimateDecisionLabels().ToArray();
            foreach (var nodeId in owed)
            {
                var declaration = labelled.SingleOrDefault(value =>
                    value.NodeId.Value == nodeId &&
                    value.CardId.Equals(IgnatiaCardContent.ChaosIncarnate));
                Assert.That(declaration.NodeId.IsDefined, Is.True,
                    "a control with nothing declared falls back to a node id a player cannot read: " +
                    nodeId);
                Assert.That(string.IsNullOrWhiteSpace(declaration.Label.Chosen), Is.False);
                Assert.That(string.IsNullOrWhiteSpace(declaration.Label.Declined), Is.False);
            }
        }

        [Test]
        public void TheFlipCapabilityIsDeferredAndNotRegistered()
        {
            Assert.That(
                IgnatiaComponentContent.DeferredHandlers()
                    .Any(handler => handler.Reference.Equals(
                        IgnatiaComponentContent.FlipTieBreakerV1)),
                Is.True);
            Assert.That(IgnatiaComponentContent.RegisteredHandlers().Any(), Is.False,
                "milestone 7.9's gate: the registered-capability list is serialized into every " +
                "snapshot, so nothing of hers is registered before she is dealt.");
            Assert.That(
                MatchContentCatalogs.ForThisBuild().Capabilities.IsRegistered(
                    IgnatiaComponentContent.FlipTieBreakerV1),
                Is.False);
        }

        // -------------------------------------------------------------------------------------
        // The frame: flip, then repeat
        // -------------------------------------------------------------------------------------

        [Test]
        public void DecliningTheFlipLeavesTheCoinAloneAndAsksForNoRepeat()
        {
            var coin = new StubCoin(TeamSide.Red);
            var run = new UltimateFrame(coin);

            Assert.That(run.PausedNode, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateFlipNode),
                "\"you MAY flip\" is the first thing the ability asks.");
            var finished = run.Answer(EffectChoiceCommandKind.Decline);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(finished.RepeatRequested.IsRequested, Is.False,
                "\"IF YOU DO, you may perform that action again\" - a declined flip reaches no " +
                "repeat at all.");
            Assert.That(coin.Flips, Is.EqualTo(0));
            Assert.That(coin.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red));
        }

        [Test]
        public void TakingTheFlipTurnsTheCoinOverBeforeTheRepeatIsOffered()
        {
            var coin = new StubCoin(TeamSide.Red);
            var run = new UltimateFrame(coin);

            var afterFlip = run.Answer(EffectChoiceCommandKind.Confirm);

            Assert.That(coin.Flips, Is.EqualTo(1),
                "the coin is turned over the moment the offer is taken, not when the repeat is.");
            Assert.That(coin.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(afterFlip.IsComplete, Is.False);
            Assert.That(run.PausedNode, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRepeatNode),
                "the node after the flip is the repeat, and it is reached with the new face " +
                "already showing - which is what decides what her repeat may target.");
            Assert.That(coin.SidesSeenByLaterNodes, Has.Member(TeamSide.Blue),
                "every context built after the flip carries the face the coin is now showing.");
        }

        [Test]
        public void TakingTheFlipAndTheRepeatRequestsOneRepeatOnADifferentTarget()
        {
            var coin = new StubCoin(TeamSide.Red);
            var run = new UltimateFrame(coin);
            run.Answer(EffectChoiceCommandKind.Confirm);
            var finished = run.Answer(EffectChoiceCommandKind.Confirm);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(finished.RepeatRequested.IsRequested, Is.True);
            Assert.That(finished.RepeatRequested.Binding,
                Is.EqualTo(RepeatTargetBinding.DifferentTarget));
            Assert.That(coin.Flips, Is.EqualTo(1), "the sentence flips once.");
        }

        [Test]
        public void FlippingWithoutRepeatingIsLegalAndTheCoinStaysFlipped()
        {
            var coin = new StubCoin(TeamSide.Blue);
            var run = new UltimateFrame(coin);
            run.Answer(EffectChoiceCommandKind.Confirm);
            var finished = run.Answer(EffectChoiceCommandKind.Decline);

            Assert.That(finished.IsComplete, Is.True);
            Assert.That(finished.RepeatRequested.IsRequested, Is.False);
            Assert.That(coin.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Red),
                "the text offers the flip on its own terms and never takes one back.");
        }

        [Test]
        public void TheRepeatIsNotOfferedWhenNothingDifferentCanBePerformed()
        {
            var coin = new StubCoin(TeamSide.Red);
            var run = new UltimateFrame(coin, repeatCanBePerformed: _ => false);
            var finished = run.Answer(EffectChoiceCommandKind.Confirm);

            Assert.That(coin.Flips, Is.EqualTo(1),
                "the flip is its own sentence and happens whether or not a repeat follows it.");
            Assert.That(finished.IsComplete, Is.True,
                "milestone 8.35z15: a repeat with no legal instance to offer is skipped in silence " +
                "rather than offered and dropped.");
            Assert.That(finished.RepeatRequested.IsRequested, Is.False);
        }

        // -------------------------------------------------------------------------------------
        // Through the triggered authority, which is what a live turn drives
        // -------------------------------------------------------------------------------------

        [Test]
        public void TheTriggeredAuthorityFlipsTheLiveCoinBetweenTheTwoAnswers()
        {
            var coin = new StubCoin(TeamSide.Red);
            var run = new UltimateTrigger(coin);

            Assert.That(run.PausedNode, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateFlipNode));
            run.Answer(EffectChoiceCommandKind.Confirm);
            Assert.That(coin.TieBreakerState.RevealedSide, Is.EqualTo(TeamSide.Blue));
            Assert.That(run.PausedNode, Is.EqualTo(IgnatiaEffectContent.ChaosIncarnateRepeatNode));

            run.Answer(EffectChoiceCommandKind.Confirm);
            Assert.That(run.Authority.PendingResolution, Is.Null);
            Assert.That(run.Authority.LastRepeatRequested.HasValue, Is.True);
            Assert.That(run.Authority.LastRepeatRequested.Value.IsRequested, Is.True);
        }

        // -------------------------------------------------------------------------------------
        // What the sentence may repeat, and how often
        // -------------------------------------------------------------------------------------

        [Test]
        public void EveryKindOfPrimaryActionSheCanPerformHasASubjectToRepeat()
        {
            var actor = new HeroId("ignatia");
            var card = new CardInstanceId("ignatia-blue-two");
            var action = new ActionDefinitionId("primary-skill");

            foreach (var kind in new[]
                { ActionKind.Attack, ActionKind.Skill, ActionKind.Movement, ActionKind.Hold })
            {
                var performed = PrimaryActionQuery.ForCompletedAction(
                    actor, card, action, kind, ActionInvocationContext.PrintedPrimary);
                Assert.That(performed.CanBeRepeated, Is.True, kind.ToString());
                var subject = PendingAfterActionRepeat.For(
                    performed, new ActionResolutionId("after-action-repeat:x:y"));
                Assert.That(subject, Is.Not.Null,
                    "a coloured card's primary is invisible to p.9's basic fact and, unless it is an " +
                    "Attack, to 8.61h's - and most of what she plays at Tier IV is exactly that: " +
                    kind);
                Assert.That(subject.Kind, Is.EqualTo(kind));
                Assert.That(subject.ActorId, Is.EqualTo(actor));
            }
        }

        [Test]
        public void NeitherASecondaryNorAReplacementNorARepeatIsAPrimaryActionToRepeat()
        {
            var actor = new HeroId("ignatia");
            var card = new CardInstanceId("ignatia-gold");
            var action = new ActionDefinitionId("secondary-movement");

            foreach (var context in new[]
            {
                ActionInvocationContext.PrintedSecondary,
                ActionInvocationContext.GeneralRuleSecondary,
                ActionInvocationContext.Replacement,
                ActionInvocationContext.Reaction,
                ActionInvocationContext.Repeated
            })
            {
                var performed = PrimaryActionQuery.ForCompletedAction(
                    actor, card, action, ActionKind.Movement, context);
                Assert.That(performed.IsDefined, Is.False, context.ToString());
                Assert.That(performed.CanBeRepeated, Is.False, context.ToString());
                Assert.That(
                    PendingAfterActionRepeat.For(
                        performed, new ActionResolutionId("after-action-repeat:x:y")),
                    Is.Null,
                    "p.10 forbids repeating a repeat, p.11's Fast Travel and p.13's Clear are " +
                    "replacements, and a secondary is not the primary band: " + context);
            }
        }

        [Test]
        public void TheAbilityIsArmedOnceAndTheSecondPrimaryActionOfTheTurnFindsItSpent()
        {
            var participants = StagedParticipants();
            var actor = participants.Heroes.First().Id;
            var ultimate = CardInstance.CreateInDeck(
                new CardInstanceId("ignatia-chaos-incarnate"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate).Reference,
                actor);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);

            var unlocked = UltimateUnlockQuery.Derive(
                new[] { IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate) },
                new[] { ultimate });
            var source = unlocked.Ultimates.Single().TriggeredSource(
                participants, 0, default, default,
                PrimaryActionQuery.ForCompletedAction(
                    actor, ultimate.Id, new ActionDefinitionId("primary"), ActionKind.Skill,
                    ActionInvocationContext.PrintedPrimary));
            var spent = new HashSet<string>(StringComparer.Ordinal);

            Assert.That(
                UltimateTriggeredEffectSourceQuery.ArmUnspent(unlocked, new[] { source }, spent).Count,
                Is.EqualTo(1),
                "\"the FIRST time each turn\" - the first primary action arms it.");
            Assert.That(
                UltimateTriggeredEffectSourceQuery.ArmUnspent(unlocked, new[] { source }, spent).Count,
                Is.EqualTo(0),
                "a second primary action the same turn - a granted one included - finds the key " +
                "already in the turn's consumed set.");
            Assert.That(
                spent.Single(),
                Does.StartWith(UltimateTriggeredEffectSourceQuery.OncePerTurnKeyPrefix));
        }

        [Test]
        public void TheSourceCarriesThePrimaryActionItFiredFor()
        {
            var participants = StagedParticipants();
            var actor = participants.Heroes.First().Id;
            var ultimate = CardInstance.CreateInDeck(
                new CardInstanceId("ignatia-chaos-incarnate-2"),
                IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate).Reference,
                actor);
            Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
            var unlocked = UltimateUnlockQuery.Derive(
                new[] { IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate) },
                new[] { ultimate });

            var primary = PrimaryActionQuery.ForCompletedAction(
                actor, new CardInstanceId("ignatia-blue-two"),
                new ActionDefinitionId("primary-skill"), ActionKind.Skill,
                ActionInvocationContext.PrintedPrimary);
            var derived = UltimateTriggeredEffectSourceQuery.Derive(
                unlocked, StagedBoard(participants), participants, default, primary).Single();

            Assert.That(derived.TriggeringPrimaryAction, Is.EqualTo(primary),
                "before milestone 8.75a the occurrence carried no record of the primary action its " +
                "own condition had just read, so an ability firing on it had nothing to offer to " +
                "repeat.");
            Assert.That(derived.TriggeringBasicAction.IsDefined, Is.False,
                "her Tier II and III cards are not p.9-Basic.");
        }

        // -------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------

        private static EffectCapabilityCatalog Catalog() =>
            SyntheticEffectCapabilities.CreateCatalog(IgnatiaComponentContent.DeferredHandlers());

        private static MatchParticipants StagedParticipants() =>
            MatchConfiguration.CreateStagedBroganWithAllyForInternalTest(TeamSide.Red).Participants;

        private static GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority StagedBoard(
            MatchParticipants participants)
        {
            var coordinates = new List<GuardsDigital.Core.Board.HexCoordinate>();
            for (var q = -3; q <= 3; q++)
            for (var r = -3; r <= 3; r++)
            {
                if (Math.Abs(q + r) <= 3)
                    coordinates.Add(new GuardsDigital.Core.Board.HexCoordinate(q, r));
            }

            return new GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority(
                coordinates,
                participants.Heroes.Select((hero, index) =>
                    new HeroOccupancy(
                        hero.Id, coordinates[index])),
                Enumerable.Empty<GuardsDigital.Core.Obstacles.ObstacleOccupancy>(),
                Enumerable.Empty<QuickGameMinionSetupState>());
        }

        /// <summary>
        /// A Tie Breaker coin with nothing behind it but the two faces p.5 gives one, and a record of
        /// what it was asked. It is the whole of what the resolver needs to be told about a coin.
        /// </summary>
        private sealed class StubCoin : IMatchTieBreakerCoin
        {
            public StubCoin(TeamSide showing)
            {
                TieBreakerState = new TieBreakerState(showing);
            }

            public TieBreakerState TieBreakerState { get; private set; }

            public int Flips { get; private set; }

            public List<TeamSide> SidesSeenByLaterNodes { get; } = new List<TeamSide>();

            public TeamSide FlipTieBreaker()
            {
                Flips++;
                TieBreakerState = new TieBreakerState(
                    TieBreakerState.RevealedSide == TeamSide.Red ? TeamSide.Blue : TeamSide.Red);
                return TieBreakerState.RevealedSide;
            }
        }

        /// <summary>
        /// Chaos Incarnate's body driven as an ordinary effect frame, with the flip settled between
        /// steps the way <c>TriggeredEffectResolutionAuthority</c> settles it in a live turn.
        /// </summary>
        private sealed class UltimateFrame
        {
            private readonly StubCoin coin;
            private readonly Func<RepeatTargetBinding, bool> repeatCanBePerformed;
            private readonly EffectDefinition body;
            private readonly MatchParticipants participants;
            private PendingResolution frame;

            public UltimateFrame(
                StubCoin coin, Func<RepeatTargetBinding, bool> repeatCanBePerformed = null)
            {
                this.coin = coin;
                this.repeatCanBePerformed = repeatCanBePerformed;
                body = IgnatiaEffectContent.ChaosIncarnateTriggeredEffect(Revision);
                participants = StagedParticipants();
                var actor = participants.Heroes.First().Id;
                var step = Settle(EffectResolutionSession.Start(
                    new EffectResolutionId("chaos-incarnate-frame"),
                    new CardInstanceId("ignatia-chaos-incarnate"),
                    IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate).Reference,
                    new ActionDefinitionId(IgnatiaEffectContent.ChaosIncarnateRuleId),
                    actor, actor, ActionInvocationContext.PrintedPrimary, Context()));
                Assert.That(step.Accepted, Is.True);
                frame = step.Frame;
            }

            public string PausedNode => frame?.PendingChoice?.NodeId.Value;

            public EffectResolutionStepResult Answer(EffectChoiceCommandKind kind)
            {
                var choice = frame.PendingChoice;
                Assert.That(choice, Is.Not.Null, "there is a question standing to answer.");
                var step = Settle(EffectResolutionSession.Answer(
                    frame,
                    new EffectChoiceCommand(
                        frame.ResolutionId, choice.ChoiceId, kind,
                        answeringHeroId: choice.ChooserId),
                    Context()));
                Assert.That(step.Accepted, Is.True);
                frame = step.Frame;
                return step;
            }

            /// <summary>The settle loop the triggered authority runs, written out once here.</summary>
            private EffectResolutionStepResult Settle(EffectResolutionStepResult step)
            {
                while (step.Accepted && step.TieBreakerFlipRequested && step.Frame != null)
                {
                    coin.FlipTieBreaker();
                    step = EffectResolutionSession.Continue(step.Frame, Context());
                }

                return step;
            }

            private EffectResolutionContext Context()
            {
                coin.SidesSeenByLaterNodes.Add(coin.TieBreakerState.RevealedSide);
                return new EffectResolutionContext(
                    body,
                    Catalog(),
                    participants,
                    new ActiveEffectLifecycle(),
                    1,
                    1,
                    RuleModificationSet.Empty,
                    EffectCardState.Absent,
                    ActionKind.Skill,
                    repeatCanBePerformed: repeatCanBePerformed,
                    tieBreakerRevealedSide: coin.TieBreakerState.RevealedSide,
                    watchesTieBreakerFlips: true);
            }
        }

        /// <summary>
        /// The same ability run through <see cref="TriggeredEffectResolutionAuthority"/>, which is the
        /// path a live turn takes: the checkpoint starts it and the seat's answers finish it.
        /// </summary>
        private sealed class UltimateTrigger
        {
            private readonly StubCoin coin;
            private readonly MatchParticipants participants;
            private readonly CardDefinition[] definitions;
            private readonly CardInstance[] instances;
            private readonly ActiveEffectLifecycle timing = new ActiveEffectLifecycle();

            public UltimateTrigger(StubCoin coin)
            {
                this.coin = coin;
                participants = StagedParticipants();
                var actor = participants.Heroes.First().Id;
                var definition = IgnatiaCardContent.Definition(IgnatiaCardContent.ChaosIncarnate);
                var ultimate = CardInstance.CreateInDeck(
                    new CardInstanceId("ignatia-chaos-incarnate-trigger"), definition.Reference, actor);
                Assert.That(CardLifecycleAuthority.TryUnlockUltimateFromDeck(ultimate), Is.True);
                definitions = new[] { definition };
                instances = new[] { ultimate };

                Authority = new TriggeredEffectResolutionAuthority(
                    new TriggeredEffectCatalog(IgnatiaEffectContent.TriggeredEffects(Revision)),
                    Catalog());

                var source = new ContinuousTriggeredEffectSource(
                    "ultimate:" + ultimate.Id.Value + ":" +
                        IgnatiaEffectContent.ChaosIncarnateRuleId,
                    actor,
                    participants.GetTeamForHero(actor).Side,
                    ultimate.Id,
                    definition.Reference,
                    definition.UltimateRule.EffectSourceId,
                    ActionKind.Skill,
                    definition.UltimateRule.TriggeredEffect.Value,
                    new[] { TimingCheckpoint.ActionEnded },
                    0,
                    default,
                    default,
                    PrimaryActionQuery.ForCompletedAction(
                        actor, new CardInstanceId("ignatia-blue-two"),
                        new ActionDefinitionId("primary-skill"), ActionKind.Skill,
                        ActionInvocationContext.PrintedPrimary));

                Authority.ApplyCheckpoint(
                    timing,
                    new ActiveEffectCheckpointContext(TimingCheckpoint.ActionEnded, 1, 1),
                    coin.TieBreakerState,
                    definitions,
                    instances,
                    participants,
                    RuleModificationSet.Empty,
                    null,
                    new[] { source },
                    null,
                    coin);
            }

            public TriggeredEffectResolutionAuthority Authority { get; }

            public string PausedNode => Authority.PendingResolution?.PendingChoice?.NodeId.Value;

            public void Answer(EffectChoiceCommandKind kind)
            {
                var pending = Authority.PendingResolution;
                Assert.That(pending, Is.Not.Null);
                var choice = pending.PendingChoice;
                Assert.That(choice, Is.Not.Null);
                var step = Authority.Answer(
                    new EffectChoiceCommand(
                        pending.ResolutionId, choice.ChoiceId, kind,
                        answeringHeroId: choice.ChooserId),
                    timing,
                    definitions,
                    instances,
                    participants,
                    RuleModificationSet.Empty,
                    1,
                    1,
                    null,
                    null,
                    coin);
                Assert.That(step.Accepted, Is.True);
            }
        }
    }
}
