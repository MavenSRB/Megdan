using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.2a acceptance check 2: the stable-node `Sequence`, `Optional`, `Conditional`, and
    /// `Choose` structures preserve typed node identity, chooser ownership, inputs, outcomes, and the
    /// exact continuation needed to resume.
    ///
    /// The load-bearing assertion in this fixture is
    /// <see cref="ResumingFromAReconstructedFrame_ProducesTheSameResult"/>: a resolution rebuilt from
    /// nothing but its serializable fields continues identically. That is what makes the frame a real
    /// continuation rather than a handle on live objects, and it is the property the snapshot boundary
    /// depends on.
    /// </summary>
    public sealed class EffectResolutionSessionTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly HeroId Controller = new HeroId("blue-hero-1");

        [Test]
        public void Sequence_ExecutesItsChildrenInDeclaredOrder()
        {
            var definition = Definition(
                Sequence("root", "one", "two", "three"),
                Leaf("one"), Leaf("two"), Leaf("three"));

            var result = Start(definition);

            Assert.That(result.IsComplete, Is.True);
            Assert.That(Nodes(result), Is.EqualTo(new[] { "one", "two", "three", "root" }),
                "Children resolve in printed order, and the Sequence itself completes after them.");
        }

        [Test]
        public void Conditional_TakesTheBranchItsPredicateSelects()
        {
            Assert.That(Nodes(Start(Conditional(true))), Does.Contain("then"));
            Assert.That(Nodes(Start(Conditional(true))), Has.None.EqualTo("else"));
            Assert.That(Nodes(Start(Conditional(false))), Does.Contain("else"));
            Assert.That(Nodes(Start(Conditional(false))), Has.None.EqualTo("then"));
        }

        [Test]
        public void Choose_PausesOnAnAuthoritativeChoiceOwnedByItsDeclaredChooser()
        {
            var result = Start(ChooseDefinition(EffectChooserRole.Controller));

            Assert.That(result.IsComplete, Is.False);
            var choice = result.Frame.PendingChoice;
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectOption));
            Assert.That(choice.NodeId, Is.EqualTo(new EffectNodeId("root")),
                "The pending choice names the exact node it belongs to.");
            Assert.That(choice.ChooserId, Is.EqualTo(Controller),
                "Chooser ownership comes from the definition, not from whoever is acting.");
            Assert.That(
                choice.Candidates.Select(candidate => candidate.OptionNodeId.Value),
                Is.EqualTo(new[] { "a", "b" }));
        }

        [Test]
        public void Choose_ExecutesTheSelectedOptionAndNotTheOthers()
        {
            var definition = ChooseDefinition(EffectChooserRole.Actor);
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            // Milestone 8.46b: the Choose admits exactly one option, so naming it is the whole
            // answer and no Confirm follows.
            var selected = EffectResolutionSession.Answer(
                paused.Frame,
                choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(new EffectNodeId("b"))),
                Context(definition));

            Assert.That(selected.IsComplete, Is.True);
            Assert.That(Nodes(selected), Does.Contain("b"));
            Assert.That(Nodes(selected), Has.None.EqualTo("a"));
        }

        /// <summary>
        /// Milestone 8.46b rewrote this from a one-of-two Choose to a two-of-three one. The property
        /// it proves is unchanged and is the reason the two-step shape survives at all: while a
        /// selection can still be unfinished, withdrawing one takes the decision back to where it
        /// stood and makes it unconfirmable again. A choice admitting exactly one selection no
        /// longer stands long enough to deselect anything, which is a different rule proved beside
        /// this one in <c>ExactlyOneEffectChoiceTests</c>.
        /// </summary>
        [Test]
        public void SelectThenDeselect_ReturnsToTheEarlierSelectionState()
        {
            var definition = ChooseDefinition(EffectChooserRole.Actor, 2, 2, "a", "b", "c");
            var paused = Start(definition);

            var selected = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.SelectInvocations[0], Context(definition));
            Assert.That(selected.Frame.PendingChoice.SelectedCandidates, Has.Count.EqualTo(1));
            Assert.That(selected.Frame.PendingChoice.ConfirmInvocation, Is.Null,
                "One of the two it needs is not an answer yet.");

            var deselected = EffectResolutionSession.Answer(
                selected.Frame, selected.Frame.PendingChoice.DeselectInvocations[0], Context(definition));

            Assert.That(deselected.Frame.PendingChoice.SelectedCandidates, Is.Empty);
            Assert.That(deselected.Frame.PendingChoice.ConfirmInvocation, Is.Null,
                "A mandatory Choose is unconfirmable again once the selection is withdrawn.");
        }

        [Test]
        public void StaleChoiceIdentity_IsRejectedAndChangesNothing()
        {
            var definition = ChooseDefinition(EffectChooserRole.Actor);
            var paused = Start(definition);
            var stale = new EffectChoiceCommand(
                paused.Frame.ResolutionId,
                new EffectChoiceId("stale-choice"),
                EffectChoiceCommandKind.Confirm);

            var rejected = EffectResolutionSession.Answer(paused.Frame, stale, Context(definition));

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason, Is.EqualTo(EffectResolutionRejectionReason.StaleChoice));
            Assert.That(rejected.Frame, Is.SameAs(paused.Frame),
                "A refused command leaves the resolution exactly as it was.");
        }

        [Test]
        public void ConfirmingBelowTheMinimum_IsRefusedAsIncomplete()
        {
            var definition = ChooseDefinition(EffectChooserRole.Actor);
            var paused = Start(definition);
            var forged = new EffectChoiceCommand(
                paused.Frame.ResolutionId,
                paused.Frame.PendingChoice.ChoiceId,
                EffectChoiceCommandKind.Confirm,
                default,
                paused.Frame.PendingChoice.ChooserId);

            var rejected = EffectResolutionSession.Answer(paused.Frame, forged, Context(definition));

            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.IncompleteSelection));
        }

        [Test]
        public void ResumingFromAReconstructedFrame_ProducesTheSameResult()
        {
            var definition = ChooseDefinition(EffectChooserRole.Actor);
            var paused = Start(definition);
            var live = paused.Frame;

            // Rebuilt from the frame's own serializable fields alone - no shared object graph.
            var rebuilt = new PendingResolution(
                live.ResolutionId,
                live.SourceCardInstanceId,
                live.SourceDefinitionReference,
                live.ActionDefinitionId,
                live.EffectDefinitionReference,
                live.ActorId,
                live.ControllerId,
                live.InvocationContext,
                live.Continuation.Select(frame =>
                    new EffectContinuationFrame(frame.NodeId, frame.ChildIndex)).ToArray(),
                live.CompletedOutcomes.Select(outcome =>
                    new EffectNodeOutcome(outcome.NodeId, outcome.ResultKind, outcome.Outputs)).ToArray(),
                live.AcceptedChoices.ToArray(),
                new PendingEffectChoice(
                    live.PendingChoice.ResolutionId,
                    live.PendingChoice.ChoiceId,
                    live.PendingChoice.NodeId,
                    live.PendingChoice.ChooserId,
                    live.PendingChoice.Kind,
                    live.PendingChoice.Candidates,
                    live.PendingChoice.MinimumSelections,
                    live.PendingChoice.MaximumSelections,
                    live.PendingChoice.SelectedCandidates));

            // Milestone 8.46b: the Choose admits exactly one, so each Select closes its own frame
            // and the two answers are compared where they land rather than after a second command.
            var select = rebuilt.PendingChoice.SelectInvocations[0];
            var fromLive = EffectResolutionSession.Answer(live, select, Context(definition));
            var fromRebuilt = EffectResolutionSession.Answer(rebuilt, select, Context(definition));

            Assert.That(Nodes(fromRebuilt), Is.EqualTo(Nodes(fromLive)));
            Assert.That(fromRebuilt.IsComplete, Is.EqualTo(fromLive.IsComplete));
            Assert.That(fromRebuilt.IsComplete, Is.True,
                "A frame rebuilt from its serializable fields alone carries the same one-press " +
                "answer to its end, which is what replay and restore reproduce the execution from.");
        }

        [Test]
        public void TypedOutput_FlowsFromAnEarlierNodeIntoALaterOne()
        {
            var definition = Definition(
                Sequence("root", "producer", "reader"),
                new EffectNodeDefinition(
                    new EffectNodeId("producer"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.NoteActorV1,
                    outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) }),
                new EffectNodeDefinition(
                    new EffectNodeId("reader"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.AcceptHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "hero", new EffectNodeId("producer"), "hero", EffectValueKind.Hero)
                    },
                    outputs: new[] { new EffectOutputBinding("received", EffectValueKind.Hero) }));

            var result = Start(definition);

            var received = result.Events
                .Single(value => value.NodeId.Equals(new EffectNodeId("reader")))
                .Outputs.Single(output => output.OutputName == "received");
            Assert.That(received.Value.HeroId, Is.EqualTo(Actor),
                "A later node reads an earlier node's published fact without a closure or callback.");
        }

        [Test]
        public void CapabilityRequestedChoice_PausesAndResumesThroughTheHandler()
        {
            var definition = Definition(
                Sequence("root", "pick"),
                new EffectNodeDefinition(
                    new EffectNodeId("pick"), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.ChooseHeroV1,
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) }));

            var paused = Start(definition);
            Assert.That(paused.Frame.PendingChoice.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));

            // Milestone 8.46b: `choose-hero@1` asks for exactly one, so the selection resumes the
            // handler inside its own command.
            var selected = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.SelectInvocations[0], Context(definition));

            Assert.That(selected.IsComplete, Is.True);
            Assert.That(selected.Events.Single(value => value.NodeId.Equals(new EffectNodeId("pick")))
                .Outputs.Single().OutputName, Is.EqualTo("chosen"));
        }

        /// <summary>
        /// Milestone 8.35z5, the queued corrective the 8.35l worker found: Arien's Living Tsunami "you
        /// may move 1 space" projected BOTH a Confirm carrying an empty selection and a Decline for
        /// the same "no" answer, and the rules accepted the Decline while refusing the Confirm as
        /// incomplete. The shape is general rather than Living Tsunami's alone - ANY
        /// <see cref="EffectNodeKind.Capability"/> node marked `may` whose handler needs more than
        /// zero once a choice actually opens (Living Tsunami's and Tigerclaw's "you may move 1 space"
        /// over <c>direct-move@3</c>; every `may`-wrapped Retrieve and owned-card Discard) hits it, so
        /// this proves the class on a synthetic handler rather than one hero's card: `choose-hero@1`
        /// needs exactly one candidate, same as those.
        /// </summary>
        [Test]
        public void MayNodeWhoseHandlerNeedsOne_OffersOnlyDeclineAsTheEmptyAnswer()
        {
            var definition = Definition(
                Sequence("root", "pick"),
                new EffectNodeDefinition(
                    new EffectNodeId("pick"), EffectNodeKind.Capability, EffectFailurePolicy.May,
                    SyntheticEffectCapabilities.ChooseHeroV1,
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) }));

            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "A `may` node always offers an honest way to take nothing, from its own failure " +
                "policy rather than from the handler's floor.");
            Assert.That(choice.ConfirmInvocation, Is.Null,
                "The handler needs exactly one candidate, so an empty Confirm must not stand beside " +
                "Decline as a second way to say the same thing - before this milestone it did, and " +
                "the rules refused it as incomplete only after the card was already spent choosing it.");

            var declined = EffectResolutionSession.Answer(
                paused.Frame, choice.DeclineInvocation, Context(definition));
            Assert.That(declined.Accepted, Is.True);
            Assert.That(declined.IsComplete, Is.True,
                "Declining the whole step completes the node having taken nothing, exactly as it " +
                "always did.");

            // A hand-forged Confirm with nothing selected is refused defensively even though the
            // offer above already withholds it - the answering side and the offering side agree.
            var forged = new EffectChoiceCommand(
                paused.Frame.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Confirm,
                default, choice.ChooserId);
            var forgedResult = EffectResolutionSession.Answer(paused.Frame, forged, Context(definition));
            Assert.That(forgedResult.Accepted, Is.False);
            Assert.That(forgedResult.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.IncompleteSelection));

            // Selecting still runs the handler exactly as an ordinary mandatory single-target
            // selection would - the fix narrows what an empty answer may do, not what a complete
            // one can. Milestone 8.46b: the handler needs exactly one, so that is one press and one
            // command; the Decline above stays beside it because `may` is a real second answer.
            var selected = EffectResolutionSession.Answer(
                paused.Frame, choice.SelectInvocations[0], Context(definition));
            Assert.That(selected.Accepted, Is.True);
            Assert.That(selected.IsComplete, Is.True);
        }

        [Test]
        public void TwoIdenticalRuns_ProduceIdenticalOrderedFacts()
        {
            var definition = Definition(
                Sequence("root", "one", "two"), Leaf("one"), Leaf("two"));

            Assert.That(Nodes(Start(definition)), Is.EqualTo(Nodes(Start(definition))));
        }

        private static EffectResolutionStepResult Start(EffectDefinition definition) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("session-fixture"),
                new CardInstanceId("session-card"),
                default,
                new ActionDefinitionId("session-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                Context(definition));

        private static EffectResolutionContext Context(EffectDefinition definition) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1, RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static IReadOnlyList<string> Nodes(EffectResolutionStepResult result) =>
            result.Events.Select(value => value.NodeId.Value).ToArray();

        private static EffectDefinition Definition(params EffectNodeDefinition[] nodes) =>
            new EffectDefinition(
                new EffectDefinitionId("session-fixture-definition"), "rev-1", nodes[0].Id, nodes);

        private static EffectNodeDefinition Sequence(string id, params string[] children) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                childNodeIds: children.Select(child => new EffectNodeId(child)));

        private static EffectNodeDefinition Leaf(string id) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1);

        private static EffectDefinition Conditional(bool flag) =>
            Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.IsFlaggedV1,
                    childNodeIds: new[] { new EffectNodeId("then"), new EffectNodeId("else") },
                    inputs: new[]
                    {
                        EffectInputBinding.FromLiteral("flag", EffectValue.Boolean(flag))
                    }),
                Leaf("then"),
                Leaf("else"));

        private static EffectDefinition ChooseDefinition(EffectChooserRole chooser) =>
            ChooseDefinition(chooser, 1, 1, "a", "b");

        private static EffectDefinition ChooseDefinition(
            EffectChooserRole chooser, int minimum, int maximum, params string[] options) =>
            Definition(
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("root"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: options.Select(option => new EffectNodeId(option)),
                        chooser: chooser,
                        minimumSelections: minimum,
                        maximumSelections: maximum)
                }.Concat(options.Select(Leaf)).ToArray());
    }
}
