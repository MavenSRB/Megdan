using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.8l at the level the rule is written: a target selection that declares what it
    /// selects for offers only what that step can act on, and a branch behind one is refused before it
    /// is chosen when nothing it could name would do anything.
    ///
    /// The capabilities here are the fixture's own rather than a card's or the shipped selection's, for
    /// the reason milestone 6.5ac's own cases use the synthetic vocabulary: nothing in this rule reads
    /// a card, a hero, a board, or a node's name, and a fixture that needed one of those to show the
    /// rule would be describing something narrower than the rule. The configured-card cases in
    /// <c>MatchSessionExchangeDispatchTests</c> prove it where a player meets it.
    /// </summary>
    public sealed class EffectSelectionEligibilityTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly HeroId Other = new HeroId("blue-hero-1");
        private static readonly EffectNodeId Root = new EffectNodeId("root");
        private static readonly EffectNodeId Branch = new EffectNodeId("branch");
        private static readonly EffectNodeId Alternative = new EffectNodeId("alternative");
        private static readonly EffectNodeId Select = new EffectNodeId("select");
        private static readonly EffectNodeId Consume = new EffectNodeId("consume");
        private static readonly EffectNodeId Later = new EffectNodeId("later");

        [Test]
        public void ASelectionDeclaringItsConsumer_OffersOnlyTheCandidatesThatConsumerAccepts()
        {
            // Acceptance check 2. The consumer answers through the same Anticipate contract the
            // feasibility walk uses, and nothing is performed to find out: a candidate it would refuse
            // is one the player is never shown, because offering it would misinform the prompt as
            // surely as offering the mode above it would.
            var paused = StartSelectionOnly(consumer: Consume, accepted: Other);

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.Candidates.Select(candidate => candidate.Value.HeroId),
                Is.EqualTo(new[] { Other }));
            Assert.That(choice.UnavailableCandidates, Is.Empty,
                "A value candidate is either offered or not offered. Only an option is greyed, and " +
                "narrowing a candidate list does not change which of the two this is.");
            Assert.That(choice.MaximumSelections, Is.EqualTo(1),
                "The bound follows the narrowed set rather than the one the handler asked against.");
        }

        [Test]
        public void ASelectionWhoseConsumerAcceptsNothing_RefusesTheBranchBehindIt()
        {
            // Acceptance check 3, and the shape milestone 6.6a would otherwise have printed onto a
            // card: every target is legal and none of them is any use, so the mode is offered, chosen,
            // and spends the action and the card on nothing.
            var paused = StartChoose(consumer: Consume, accepted: default);

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { Branch, Alternative }),
                "Refusing a mode is not hiding it: the player still reads that it exists.");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { Branch }));
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { Alternative }),
                "And carries no way to select it.");
        }

        [Test]
        public void ASelectionThatNamesNoConsumer_KeepsTheConservativeReading()
        {
            // Acceptance check 4, first half. The consumer parameter is optional, so a definition that
            // says nothing about what its choice is for is judged exactly as it was before this
            // milestone - offered, because the walk cannot read it.
            var paused = StartChoose(consumer: default, accepted: default);

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Is.Empty,
                "This milestone narrows what can be read; it does not start disabling what merely " +
                "might fail.");
        }

        [Test]
        public void AConsumerWaitingOnALaterStepsOutput_KeepsTheConservativeReading()
        {
            // Acceptance check 4, second half, and the position the synthetic Skill's Swap branch is
            // in: the consumer also reads a node that runs after the selection, so asking it now would
            // put a question nobody has posed and refuse candidates for a reason that is not about
            // them.
            var paused = StartChoose(consumer: Consume, accepted: default, consumerReadsLaterStep: true);

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Is.Empty);
        }

        [Test]
        public void AnAnswerNamingACandidateTheConsumerWouldNowRefuse_IsRejectedAtomically()
        {
            // Acceptance check 5. Eligibility is read again when the answer arrives rather than
            // trusted from the offer, so a selection made while the consumer accepted it does not
            // survive the consumer ceasing to. The fixture's consumer accepts only in round 1, which
            // is how a state change between the pause and the answer is made deterministic.
            var definition = SelectionOnly(consumer: Consume);
            var paused = Start(definition, Context(definition, accepted: Other, roundIndex: 1));
            var choice = paused.Frame.PendingChoice;
            var selected = EffectResolutionSession.Answer(
                paused.Frame,
                choice.SelectInvocations.Single(),
                Context(definition, accepted: Other, roundIndex: 1));
            Assert.That(selected.Accepted, Is.True, "Fixture precondition: the answer was legal.");

            var stale = Context(definition, accepted: Other, roundIndex: 2);
            var refused = EffectResolutionSession.Answer(
                selected.Frame, selected.Frame.PendingChoice.ConfirmInvocation, stale);

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.InvalidChoice));
            Assert.That(refused.Frame, Is.SameAs(selected.Frame),
                "The pending choice survives untouched, so the decision is still there to be made.");
            Assert.That(refused.Events, Is.Empty);
        }

        [Test]
        public void NarrowingTheCandidates_PerformsNothing()
        {
            // The rule the probe contract exists for, restated where this milestone newly relies on
            // it: the consumer is asked once per candidate, and being asked is not being run.
            var definition = SelectionOnly(consumer: Consume);
            var context = Context(definition, accepted: Other, roundIndex: 1);

            Start(definition, context);

            Assert.That(ConsumerSample.Performed, Is.Zero,
                "Asking whether a target can be acted on must never act on it.");
        }

        // -------------------------------------------------------------------------------------------
        // Fixtures
        // -------------------------------------------------------------------------------------------

        private static EffectResolutionStepResult StartSelectionOnly(
            EffectNodeId consumer, HeroId accepted)
        {
            var definition = SelectionOnly(consumer);
            return Start(definition, Context(definition, accepted, roundIndex: 1));
        }

        private static EffectResolutionStepResult StartChoose(
            EffectNodeId consumer, HeroId accepted, bool consumerReadsLaterStep = false)
        {
            var definition = ChooseOverSelection(consumer, consumerReadsLaterStep);
            return Start(definition, Context(definition, accepted, roundIndex: 1));
        }

        private static EffectResolutionStepResult Start(
            EffectDefinition definition, EffectResolutionContext context)
        {
            ConsumerSample.Performed = 0;
            return EffectResolutionSession.Start(
                new EffectResolutionId("selection-eligibility-fixture"),
                new CardInstanceId("selection-eligibility-card"),
                default,
                new ActionDefinitionId("selection-eligibility-action"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                context);
        }

        private static EffectResolutionContext Context(
            EffectDefinition definition, HeroId accepted, int roundIndex) =>
            new EffectResolutionContext(
                definition,
                new EffectCapabilityCatalog(new IEffectCapabilityHandler[]
                {
                    new SelectionSample(),
                    new ConsumerSample(accepted),
                    new PairedConsumerSample(),
                    new AlwaysAppliesSample()
                }),
                null,
                null,
                roundIndex,
                1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        /// <summary>A Sequence of the selection and the step that consumes what it publishes.</summary>
        private static EffectDefinition SelectionOnly(EffectNodeId consumer) =>
            new EffectDefinition(
                new EffectDefinitionId("selection-only-fixture"), "rev-1", Branch,
                Nodes(consumer, consumerReadsLaterStep: false));

        /// <summary>The same Sequence as one option of a mandatory `Choose`, beside one that works.</summary>
        private static EffectDefinition ChooseOverSelection(
            EffectNodeId consumer, bool consumerReadsLaterStep) =>
            new EffectDefinition(
                new EffectDefinitionId("selection-choose-fixture"), "rev-1", Root,
                new[]
                {
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Branch, Alternative },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    new EffectNodeDefinition(
                        Alternative, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                        AlwaysAppliesSample.Reference_)
                }
                .Concat(Nodes(consumer, consumerReadsLaterStep)));

        private static IEnumerable<EffectNodeDefinition> Nodes(
            EffectNodeId consumer, bool consumerReadsLaterStep)
        {
            var selectionInputs = new List<EffectInputBinding>();
            if (consumer.IsDefined)
            {
                selectionInputs.Add(EffectInputBinding.FromLiteral(
                    EffectSelectionDeclaration.ConsumerParameterName, EffectValue.Node(consumer)));
            }

            var consumerInputs = new List<EffectInputBinding>
            {
                EffectInputBinding.FromNodeOutput("subject", Select, "chosen", EffectValueKind.Hero)
            };
            if (consumerReadsLaterStep)
            {
                consumerInputs.Add(EffectInputBinding.FromNodeOutput(
                    "beside", Later, "hero", EffectValueKind.Hero));
            }

            var branchChildren = consumerReadsLaterStep
                ? new[] { Select, Later, Consume }
                : new[] { Select, Consume };

            var nodes = new List<EffectNodeDefinition>
            {
                new EffectNodeDefinition(
                    Branch, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: branchChildren),
                new EffectNodeDefinition(
                    Select, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SelectionSample.Reference_,
                    inputs: selectionInputs,
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) }),
                new EffectNodeDefinition(
                    Consume, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    consumerReadsLaterStep
                        ? PairedConsumerSample.Reference_
                        : ConsumerSample.Reference_,
                    inputs: consumerInputs)
            };

            if (consumerReadsLaterStep)
            {
                nodes.Add(new EffectNodeDefinition(
                    Later, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    AlwaysAppliesSample.Reference_,
                    outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) }));
            }

            return nodes;
        }

        // -------------------------------------------------------------------------------------------
        // Sample capabilities. None is registered in the shipped catalog.
        // -------------------------------------------------------------------------------------------

        /// <summary>Offers two heroes and accepts a consumer, which is the whole shape under test.</summary>
        private sealed class SelectionSample
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public static EffectCapabilityReference Reference_ =>
                new EffectCapabilityReference(new EffectCapabilityId("sample-selection"), 1);

            public EffectCapabilityReference Reference => Reference_;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node,
                            isRequired: false)
                    },
                    new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, Candidates(), 1, 1));

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("chosen", selected[0]) });

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates().Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates();

            /// <summary>Milestone 8.35l: one hero, the shape this sample has always requested.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            /// <summary>Milestone 8.35l: selecting mutates nothing here either.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>
            /// Milestone 8.35l: the same fact <see cref="Resume"/> publishes. This sample's candidate
            /// kind and output kind coincide, which is exactly the narrow shape that was declarable
            /// before 8.35l - so stating it here keeps the sample honest without changing what it
            /// proves.
            /// </summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1
                    ? new[] { new EffectOutcomeFact("chosen", chosen[0]) }
                    : Array.Empty<EffectOutcomeFact>();

            private static IReadOnlyList<EffectValue> Candidates() =>
                new[] { EffectValue.Hero(Actor), EffectValue.Hero(Other) };
        }

        /// <summary>
        /// Accepts one named hero, and only in round 1, so that "the consumer stopped accepting this"
        /// is a state change a fixture can make happen between a pause and its answer.
        /// </summary>
        private sealed class ConsumerSample : IEffectCapabilityHandler
        {
            private readonly HeroId accepted;

            public ConsumerSample(HeroId accepted)
            {
                this.accepted = accepted;
            }

            /// <summary>How many times the execution ran. A probe must leave this at zero.</summary>
            public static int Performed { get; set; }

            public static EffectCapabilityReference Reference_ =>
                new EffectCapabilityReference(new EffectCapabilityId("sample-consumer"), 1);

            public EffectCapabilityReference Reference => Reference_;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(new[]
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Hero)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                Performed++;
                return Accepts(context)
                    ? EffectExecutionResult.Applied()
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This sample never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Accepts(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private bool Accepts(EffectCapabilityContext context) =>
                accepted.IsDefined &&
                context.RoundIndex == 1 &&
                context.Input("subject").Kind == EffectValueKind.Hero &&
                context.Input("subject").HeroId.Equals(accepted);
        }

        /// <summary>
        /// A consumer that needs a second subject as well, published by a step the selection has not
        /// reached yet. It refuses everything, so a fixture using it that offers the branch anyway is
        /// showing the conservative reading rather than a lucky answer.
        /// </summary>
        private sealed class PairedConsumerSample : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference Reference_ =>
                new EffectCapabilityReference(new EffectCapabilityId("sample-paired-consumer"), 1);

            public EffectCapabilityReference Reference => Reference_;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(new[]
                {
                    new EffectCapabilityParameter("subject", EffectValueKind.Hero),
                    new EffectCapabilityParameter("beside", EffectValueKind.Hero)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This sample never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>The option beside the one under test, so a refused branch has something to be refused against.</summary>
        private sealed class AlwaysAppliesSample : IEffectCapabilityHandler
        {
            public static EffectCapabilityReference Reference_ =>
                new EffectCapabilityReference(new EffectCapabilityId("sample-applies"), 1);

            public EffectCapabilityReference Reference => Reference_;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    null, new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("hero", EffectValue.Hero(context.ActorId)) });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This sample never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }
    }
}
