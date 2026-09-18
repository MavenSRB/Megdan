using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.46b. The user's rule from the decision-window design of 2026-09-18: a choice that
    /// admits exactly one selection is answered in ONE press.
    ///
    /// <para>Until this milestone every effect choice stood on Select then Confirm, and presentation
    /// papered over the second press for the player by forwarding the authority's own Confirm behind
    /// a filled selection (milestone 6.5c). That left the fact underneath: two accepted commands, a
    /// pause that really did stand on Deselect and Confirm in between, a seat rewind that stepped
    /// back half an answer, and a remote seat that had to send the second command after the host's
    /// projection came back. The rule now lives where Select is applied - in
    /// <see cref="EffectResolutionSession"/> - so the one press is one command on every surface.</para>
    ///
    /// <para>What this fixture pins is the BOUNDARY, in both directions: which choices resolve on the
    /// selection and which keep the two-step answer they have always had. The cases below are
    /// deliberately shaped around the three near neighbours that must NOT move - an `up to 1` whose
    /// floor of zero makes taking none a real answer, a choose-several that is genuinely unfinished
    /// after one pick, and a confirm-or-decline whose press IS the Confirm.</para>
    ///
    /// <para>Unity-free on purpose, so the offline harness and the Edit Mode assembly both run it.</para>
    /// </summary>
    public sealed class ExactlyOneEffectChoiceTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly HeroId Controller = new HeroId("blue-hero-1");

        /// <summary>
        /// The rule itself, on the mode shape: a mandatory <c>Choose</c> of one option out of two is
        /// finished by naming one, and the node it names runs inside that same command.
        /// </summary>
        [Test]
        public void ExactlyOneChoose_ResolvesOnTheSelectionItself()
        {
            var definition = ChooseDefinition(1, 1, "a", "b");
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            Assert.That(choice.ResolvesOnSelection, Is.True,
                "Arrange: a one-of-two mandatory Choose is the shape the rule is about.");

            var answered = EffectResolutionSession.Answer(
                paused.Frame,
                choice.SelectInvocations.Single(invocation =>
                    invocation.Candidate.OptionNodeId.Equals(new EffectNodeId("b"))),
                Context(definition));

            Assert.That(answered.Accepted, Is.True);
            Assert.That(answered.IsComplete, Is.True,
                "The Select carried the whole answer, so the effect ran to its end on one command.");
            Assert.That(answered.Frame, Is.Null,
                "and nothing is left standing to ask a second time.");
            Assert.That(Nodes(answered), Does.Contain("b"));
            Assert.That(Nodes(answered), Has.None.EqualTo("a"));
        }

        /// <summary>
        /// The same rule on the other family that reaches it: a capability that asked for exactly one
        /// typed value. <c>choose-hero@1</c> declares the bounds Living Tsunami's move, every
        /// single-card Retrieve and every named piece target declare, so this is that whole class.
        /// </summary>
        [Test]
        public void ExactlyOneCapabilityTarget_ResolvesOnTheSelectionItself()
        {
            var definition = ChooseHeroDefinition(EffectFailurePolicy.Mandatory);
            var paused = Start(definition);

            Assert.That(paused.Frame.PendingChoice.Kind, Is.EqualTo(EffectChoiceKind.SelectTarget));
            Assert.That(paused.Frame.PendingChoice.ResolvesOnSelection, Is.True);

            var answered = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.SelectInvocations[0], Context(definition));

            Assert.That(answered.IsComplete, Is.True);
            Assert.That(
                answered.Events.Single(value => value.NodeId.Equals(new EffectNodeId("pick")))
                    .Outputs.Single().OutputName,
                Is.EqualTo("chosen"),
                "The handler was resumed with the selection, exactly as the Confirm used to resume it.");
        }

        /// <summary>
        /// The pause never stands on Deselect and Confirm. This is the half of the rule a player
        /// sees: a decision window for such a choice has one control per candidate and no Done.
        /// </summary>
        [Test]
        public void ExactlyOneChoice_StandsWithNeitherDeselectNorConfirm()
        {
            foreach (var definition in new[]
                {
                    ChooseDefinition(1, 1, "a", "b"),
                    ChooseHeroDefinition(EffectFailurePolicy.Mandatory)
                })
            {
                var choice = Start(definition).Frame.PendingChoice;

                Assert.That(choice.SelectedCandidates, Is.Empty,
                    "Such a choice is only ever published empty, because the first Select closes it.");
                Assert.That(choice.DeselectInvocations, Is.Empty,
                    "so there is nothing to take back...");
                Assert.That(choice.ConfirmInvocation, Is.Null,
                    "...and no Done to press. Both follow from the bounds rather than from a " +
                    "special case: `0 >= 1` is false.");
                Assert.That(choice.SelectInvocations, Has.Count.GreaterThan(1),
                    "while every candidate is still offered.");
            }
        }

        /// <summary>
        /// A Deselect never applies to an exactly-one choice. The offering side withholds it above;
        /// this is the answering side refusing a hand-forged one, so the two agree rather than one
        /// depending on the other - the same defensive pairing milestone 8.35z5 used for the empty
        /// Confirm.
        /// </summary>
        [Test]
        public void ForgedDeselect_AgainstAnExactlyOneChoice_IsRefusedAndChangesNothing()
        {
            var definition = ChooseDefinition(1, 1, "a", "b");
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;
            var forged = new EffectChoiceCommand(
                paused.Frame.ResolutionId,
                choice.ChoiceId,
                EffectChoiceCommandKind.Deselect,
                EffectChoiceCandidate.Option(new EffectNodeId("a")),
                choice.ChooserId);

            var refused = EffectResolutionSession.Answer(paused.Frame, forged, Context(definition));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.InvalidChoice));
            Assert.That(refused.Frame, Is.SameAs(paused.Frame),
                "and the decision is still there to be made.");
        }

        /// <summary>
        /// The first neighbour that must not move: a choice admitting several is genuinely
        /// unfinished after one pick, so it keeps Select, Deselect and Confirm exactly as it had
        /// them.
        /// </summary>
        [Test]
        public void ChooseSeveral_StillNeedsItsConfirm()
        {
            var definition = ChooseDefinition(2, 2, "a", "b", "c");
            var paused = Start(definition);

            Assert.That(paused.Frame.PendingChoice.ResolvesOnSelection, Is.False);

            var first = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.SelectInvocations[0], Context(definition));
            Assert.That(first.IsComplete, Is.False,
                "One of two is not an answer, so the decision still stands.");
            Assert.That(first.Frame.PendingChoice.ConfirmInvocation, Is.Null);
            Assert.That(first.Frame.PendingChoice.DeselectInvocations, Has.Count.EqualTo(1),
                "and the pick made can still be taken back.");

            var second = EffectResolutionSession.Answer(
                first.Frame, first.Frame.PendingChoice.SelectInvocations[0], Context(definition));
            Assert.That(second.IsComplete, Is.False,
                "Filling the selection is still not the answer: the Confirm is what closes it.");
            Assert.That(second.Frame.PendingChoice.ConfirmInvocation, Is.Not.Null);

            var confirmed = EffectResolutionSession.Answer(
                second.Frame, second.Frame.PendingChoice.ConfirmInvocation, Context(definition));
            Assert.That(confirmed.IsComplete, Is.True);
        }

        /// <summary>
        /// The second neighbour: an `up to 1`. Its ceiling is one but its floor is zero, so closing
        /// it having taken none is a real answer and the completion action that gives it survives.
        /// This is the case a rule written on <c>MaximumSelections</c> alone would have broken.
        /// </summary>
        [Test]
        public void UpToOne_KeepsItsTwoStepAnswer()
        {
            var definition = ChooseDefinition(0, 1, "a", "b");
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            Assert.That(choice.ResolvesOnSelection, Is.False,
                "A floor of zero means taking none is an answer, so the press is not the whole of it.");
            Assert.That(choice.ConfirmInvocation, Is.Not.Null,
                "and closing it with nothing named is offered from the start.");

            var selected = EffectResolutionSession.Answer(
                paused.Frame, choice.SelectInvocations[0], Context(definition));

            Assert.That(selected.IsComplete, Is.False,
                "so naming one pauses exactly as it always did.");
            Assert.That(selected.Frame.PendingChoice.ConfirmInvocation, Is.Not.Null);
            Assert.That(selected.Frame.PendingChoice.DeselectInvocations, Has.Count.EqualTo(1));
        }

        /// <summary>
        /// The third neighbour: confirm-or-decline is unchanged. Its press IS the Confirm - it
        /// offers no candidates at all, so there is no Select for the rule to attach to.
        /// </summary>
        [Test]
        public void ConfirmOrDecline_IsUnchanged()
        {
            var definition = OptionalDefinition();
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.ResolvesOnSelection, Is.False);
            Assert.That(choice.Candidates, Is.Empty);
            Assert.That(choice.SelectInvocations, Is.Empty);
            Assert.That(choice.ConfirmInvocation, Is.Not.Null);
            Assert.That(choice.DeclineInvocation, Is.Not.Null);

            var taken = EffectResolutionSession.Answer(
                paused.Frame, choice.ConfirmInvocation, Context(definition));
            Assert.That(taken.IsComplete, Is.True);
            Assert.That(Nodes(taken), Does.Contain("step"));

            var declined = EffectResolutionSession.Answer(
                Start(definition).Frame, choice.DeclineInvocation, Context(definition));
            Assert.That(declined.IsComplete, Is.True);
            Assert.That(Nodes(declined), Has.None.EqualTo("step"));
        }

        /// <summary>
        /// A `may` node whose handler needs exactly one keeps its Decline and gains the one press.
        /// The two answers are different things and stay different things: declining records that
        /// the step was refused, naming a candidate takes it.
        /// </summary>
        [Test]
        public void MayNodeNeedingOne_ResolvesOnSelectionAndStillDeclines()
        {
            var definition = ChooseHeroDefinition(EffectFailurePolicy.May);
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            Assert.That(choice.ResolvesOnSelection, Is.True);
            Assert.That(choice.ConfirmInvocation, Is.Null);
            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "Milestone 8.35z5's rule is untouched: `may` is declinable from its own failure " +
                "policy rather than from the floor.");

            var selected = EffectResolutionSession.Answer(
                paused.Frame, choice.SelectInvocations[0], Context(definition));
            Assert.That(selected.IsComplete, Is.True);
            Assert.That(
                selected.Events.Single(value => value.NodeId.Equals(new EffectNodeId("pick")))
                    .ResultKind,
                Is.EqualTo(EffectExecutionResultKind.Applied),
                "Taking the step ran it.");

            var declined = EffectResolutionSession.Answer(
                Start(definition).Frame, choice.DeclineInvocation, Context(definition));
            Assert.That(declined.IsComplete, Is.True);
            Assert.That(
                declined.Events.Single(value => value.NodeId.Equals(new EffectNodeId("pick")))
                    .ResultKind,
                Is.EqualTo(EffectExecutionResultKind.DeclinedOptional),
                "and refusing it recorded that it was refused, which is a different thing.");
        }

        /// <summary>
        /// The answer the one press records is the answer the two presses recorded. This is what
        /// keeps replay, snapshot restore and retained-prefix Undo reproducing the same execution
        /// across the change, and it is why the schema does not move.
        /// </summary>
        [Test]
        public void TheRecordedAnswer_IsTheSameOneTheTwoStepShapeWrote()
        {
            var definition = TwoPicksDefinition();
            var paused = Start(definition);
            var select = paused.Frame.PendingChoice.SelectInvocations[1];

            var answered = EffectResolutionSession.Answer(paused.Frame, select, Context(definition));

            Assert.That(answered.Frame.PendingChoice, Is.Not.Null,
                "Arrange: the second selection is what keeps the frame standing to be read.");
            var record = answered.Frame.AcceptedChoices.Single();
            Assert.That(record.NodeId, Is.EqualTo(new EffectNodeId("first")));
            Assert.That(record.ChoiceId, Is.EqualTo(paused.Frame.PendingChoice.ChoiceId));
            Assert.That(record.ResolvedBy, Is.EqualTo(EffectChoiceCommandKind.Confirm),
                "Recorded as a Confirm because that is what it is: the choice was closed by taking " +
                "an option, not by refusing the step. Nothing downstream can tell how many commands " +
                "it took to say, and nothing downstream should have to.");
            Assert.That(record.Selected, Is.EqualTo(new[] { select.Candidate }));
        }

        /// <summary>
        /// The seat half of the rule: one accepted command answers such a choice, so one press of
        /// the seat's rewind puts the decision back.
        /// </summary>
        /// <remarks>
        /// <para>Driven through the seats of a real six-player match rather than asserted on a
        /// synthetic frame, because "how many accepted commands" is a question only the authority's
        /// own stream answers - the same reason <c>SeatBoundedUndoTests</c> drives rather than
        /// composes. The walk stops at the first exactly-one effect choice whose seat holds a
        /// rewind; if the seeded prefix reaches none, the case says so rather than passing
        /// vacuously.</para>
        /// <para>Before this milestone the same position took TWO accepted commands and therefore
        /// two presses, and the first press back landed the player on a pause standing on Deselect
        /// and Confirm - a state the player had never seen going forwards.</para>
        /// </remarks>
        [Test]
        public void OneAcceptedCommandAnswersIt_AndOneRewindPutsItBack()
        {
            var host = MatchHost.Compose(OwnerScopedProjectionTests.CreateOfficialInitialState());
            MatchSeat answering = null;
            EffectChoiceId standing = default;

            OwnerScopedProjectionTests.DriveThroughSeats(host, 1600, projection =>
            {
                var choice = projection.PendingEffectChoice;
                if (choice == null || !choice.ResolvesOnSelection)
                {
                    return false;
                }

                answering = host.Seats.Seats.FirstOrDefault(seat =>
                    host.ProjectionForSeat(seat.Id).IsSeatUndoAvailable &&
                    host.ProjectionForSeat(seat.Id).PendingEffectChoice != null);
                standing = choice.ChoiceId;
                return answering != null;
            });

            Assert.That(answering, Is.Not.Null,
                "The seeded seat walk reached no exactly-one effect choice standing for a seat that " +
                "also held a rewind. If content drifts so that this position is unreachable in the " +
                "budget, widen the walk rather than deleting the case.");

            var client = host.ClientForSeat(answering.Id);
            var before = host.Projection.AcceptedCommands.Count;
            var select = client.Projection.PendingEffectChoice.SelectInvocations[0];
            var accepted = client.Submit(new SeatSubmission(
                new SubmissionId("one-press-select"),
                client.Projection.Version,
                DirectorCommand.AnswerEffectChoice(select)));

            Assert.That(accepted.Accepted, Is.True, accepted.Detail);
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before + 1),
                "One press is one accepted command.");
            Assert.That(
                host.Projection.PendingEffectChoice == null ||
                    !host.Projection.PendingEffectChoice.ChoiceId.Equals(standing),
                Is.True,
                "and that command answered the choice rather than parking on it.");

            if (!client.Projection.IsSeatUndoAvailable)
            {
                // The answer raised a wall of its own - an attack target named through card text,
                // or a command that touched an opponent. That is milestone 8.35p's and 8.42e's rule
                // doing its job on the single command, which is the point rather than a miss.
                Assert.Pass(
                    "The one command raised the seat's own undo wall, which is 8.35p/8.42e holding " +
                    "on the select-and-confirm exactly as it held on the Confirm.");
            }

            var rewound = client.SubmitUndo(new SeatUndoSubmission(
                new SubmissionId("one-press-undo"), client.Projection.Version));

            Assert.That(rewound.Accepted, Is.True, rewound.Detail);
            Assert.That(host.Projection.AcceptedCommands.Count, Is.EqualTo(before),
                "One press back is the whole answer back.");
            Assert.That(host.Projection.PendingEffectChoice, Is.Not.Null);
            Assert.That(host.Projection.PendingEffectChoice.ChoiceId, Is.EqualTo(standing),
                "The choice the player answered is the choice the player is asked again, rather " +
                "than a half-answered one they never saw going forwards.");
            Assert.That(host.Projection.PendingEffectChoice.SelectedCandidates, Is.Empty);
        }

        private static EffectResolutionStepResult Start(EffectDefinition definition) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("one-press-fixture"),
                new CardInstanceId("one-press-card"),
                default,
                new ActionDefinitionId("one-press-action"),
                Actor,
                Controller,
                ActionInvocationContext.PrintedPrimary,
                Context(definition));

        private static EffectResolutionContext Context(EffectDefinition definition) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static IReadOnlyList<string> Nodes(EffectResolutionStepResult result) =>
            result.Events.Select(value => value.NodeId.Value).ToArray();

        private static EffectDefinition Definition(params EffectNodeDefinition[] nodes) =>
            new EffectDefinition(
                new EffectDefinitionId("one-press-fixture-definition"), "rev-1", nodes[0].Id, nodes);

        private static EffectNodeDefinition Leaf(string id) =>
            new EffectNodeDefinition(
                new EffectNodeId(id), EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                SyntheticEffectCapabilities.NoteActorV1);

        private static EffectDefinition ChooseDefinition(
            int minimum, int maximum, params string[] options) =>
            Definition(
                new[]
                {
                    new EffectNodeDefinition(
                        new EffectNodeId("root"), EffectNodeKind.Choose,
                        minimum == 0 ? EffectFailurePolicy.UpTo : EffectFailurePolicy.Mandatory,
                        childNodeIds: options.Select(option => new EffectNodeId(option)),
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: minimum,
                        maximumSelections: maximum)
                }.Concat(options.Select(Leaf)).ToArray());

        private static EffectDefinition ChooseHeroDefinition(EffectFailurePolicy policy) =>
            Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("pick") }),
                new EffectNodeDefinition(
                    new EffectNodeId("pick"), EffectNodeKind.Capability, policy,
                    SyntheticEffectCapabilities.ChooseHeroV1,
                    outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) }));

        private static EffectDefinition OptionalDefinition() =>
            Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Optional, EffectFailurePolicy.May,
                    childNodeIds: new[] { new EffectNodeId("step") },
                    chooser: EffectChooserRole.Actor),
                Leaf("step"));

        /// <summary>
        /// Two exactly-one selections in a row. Answering the first leaves the frame standing on the
        /// second, which is how a case can read what the first answer RECORDED - a completed step
        /// keeps no frame.
        /// </summary>
        private static EffectDefinition TwoPicksDefinition() =>
            Definition(
                new EffectNodeDefinition(
                    new EffectNodeId("root"), EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("first"), new EffectNodeId("second") }),
                new EffectNodeDefinition(
                    new EffectNodeId("first"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("a"), new EffectNodeId("b") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                new EffectNodeDefinition(
                    new EffectNodeId("second"), EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { new EffectNodeId("c"), new EffectNodeId("d") },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Leaf("a"),
                Leaf("b"),
                Leaf("c"),
                Leaf("d"));
    }
}
