using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;
using NUnit.Framework;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 6.5ac: a `Choose` offers only the options the acting player can actually perform.
    ///
    /// Before this, every child of a `Choose` was offered unconditionally, so a mode that could do
    /// nothing was selectable, and choosing it ran the branch to a mandatory stop that spent the card,
    /// ended the turn, changed nothing, and explained nothing. These cases are the rule at the level it
    /// is written - the branch's own declared contract, judged by the same walk for every definition -
    /// while the configured-card fixtures prove it where a player meets it.
    ///
    /// Milestone 6.5ae then closed the one position 6.5ac left without a surface. Where no option can
    /// be performed the node used to report itself impossible before any prompt opened, because a
    /// mandatory `Choose` offers no decline and every other control is withheld while a choice stands.
    /// It now pauses with every option greyed and offers p.10's stop, which the player takes.
    ///
    /// The definitions here are the resolver's own synthetic vocabulary rather than cards, which is the
    /// point: nothing in the rule reads a card, a hero, or a node's name.
    /// </summary>
    public sealed class EffectOptionFeasibilityTests
    {
        private static readonly HeroId Actor = new HeroId("red-hero-1");
        private static readonly EffectNodeId Root = new EffectNodeId("root");
        private static readonly EffectNodeId First = new EffectNodeId("first-option");
        private static readonly EffectNodeId Second = new EffectNodeId("second-option");

        [Test]
        public void AnUnperformableOption_IsProjectedAndCarriesNoSelectInvocation()
        {
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First, Second }),
                "Refusing an option is not hiding it: a player sees the mode exists.");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First }));
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { Second }),
                "The option that can do nothing carries no way to select it.");
        }

        [Test]
        public void SelectingAnUnperformableOption_IsRejectedWithoutTouchingTheFrame()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            var refused = EffectResolutionSession.Answer(
                paused.Frame,
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                    EffectChoiceCandidate.Option(First), choice.ChooserId),
                Context(definition));

            Assert.That(refused.Accepted, Is.False);
            Assert.That(refused.RejectionReason, Is.EqualTo(EffectResolutionRejectionReason.InvalidChoice));
            Assert.That(refused.Frame, Is.SameAs(paused.Frame),
                "The refusal returns the frame untouched: no continuation, outcome, or accepted " +
                "choice moves.");
            Assert.That(refused.Events, Is.Empty);
        }

        [Test]
        public void AMandatoryChooseWithNoPerformableOption_PausesWithEveryOptionGreyedAndOnlyAStop()
        {
            // Milestone 6.5ae. This used to report itself impossible before any prompt opened, so the
            // player never saw the modes at all; the recorded reason was that pausing left the match
            // with no legal command. The stop is that command.
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory)));

            Assert.That(paused.IsComplete, Is.False);
            Assert.That(Kinds(paused), Is.Empty, "Nothing has happened yet; the action is waiting.");

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First, Second }),
                "Every mode is projected, so the player reads which ones exist.");
            Assert.That(choice.UnavailableCandidates, Is.EqualTo(choice.Candidates),
                "And that none of them can be taken.");
            Assert.That(choice.SelectInvocations, Is.Empty);
            Assert.That(choice.DeselectInvocations, Is.Empty);
            Assert.That(choice.ConfirmInvocation, Is.Null,
                "A mandatory Choose requires at least one selection, so nothing is confirmable.");
            Assert.That(choice.DeclineInvocation, Is.Null, "And a mandatory Choose offers no decline.");
            Assert.That(choice.StopInvocation, Is.Not.Null,
                "So the stop is the one legal command, exactly as a paused Movement's is.");
            Assert.That(choice.StopInvocation.Kind, Is.EqualTo(EffectChoiceCommandKind.Stop));
            Assert.That(choice.StopInvocation.ResolutionId, Is.EqualTo(choice.ResolutionId));
            Assert.That(choice.StopInvocation.ChoiceId, Is.EqualTo(choice.ChoiceId));
            Assert.That(choice.StopInvocation.Candidate.IsDefined, Is.False,
                "A stop answers no question, so it names no candidate.");
        }

        /// <summary>
        /// Milestone 6.5af judges a whole effect before its action is offered, which raises the
        /// question of whether milestone 6.5ae's pause is still reachable at all. It is, and this is
        /// the shape that reaches it: the walk short-circuits at the first thing that can apply, so a
        /// definition whose first step works is offered without its later nodes being judged, and the
        /// `Choose` after it can still turn out to refuse everything once execution gets there.
        ///
        /// That is the honest division between the two milestones. 6.5af declines to offer an action
        /// it can prove up front is empty; 6.5ae closes one that only turns out to be empty later.
        /// </summary>
        [Test]
        public void AnEffectWhoseLaterChooseRefusesEverything_IsStillOfferedAndStillReachesTheStop()
        {
            var first = new EffectNodeId("first-step");
            var choose = new EffectNodeId("later-choose");
            var sequence = new EffectNodeId("sequence");
            var definition = new EffectDefinition(
                new EffectDefinitionId("late-refusal-fixture"), "rev-1", sequence,
                new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { first, choose }),
                    Leaf(first, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                    new EffectNodeDefinition(
                        choose, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { First, Second },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory)
                });
            var context = Context(definition);

            var prospective = EffectResolutionSession.BuildStartFrame(
                new EffectResolutionId("late-refusal-probe"),
                new CardInstanceId("feasibility-card"),
                default,
                new ActionDefinitionId("feasibility-action"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                context);
            Assert.That(EffectOptionFeasibilityQuery.CanApplyAnything(prospective, context), Is.True,
                "The first step can apply, so milestone 6.5af offers the action - it refuses what it " +
                "can prove is empty and never what it merely has not judged.");

            var paused = Start(definition);

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice, Is.Not.Null);
            Assert.That(choice.UnavailableCandidates, Is.EqualTo(choice.Candidates),
                "And once execution reaches the Choose, every option really is refused.");
            Assert.That(choice.StopInvocation, Is.Not.Null,
                "So milestone 6.5ae's stop is what closes it, which is why that milestone is not " +
                "made dead by this one.");
        }

        [Test]
        public void AnEffectThatCanApplyNothing_IsProvenEmptyBeforeItStarts()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory));
            var context = Context(definition);

            var prospective = EffectResolutionSession.BuildStartFrame(
                new EffectResolutionId("empty-probe"),
                new CardInstanceId("feasibility-card"),
                default,
                new ActionDefinitionId("feasibility-action"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                context);

            Assert.That(EffectOptionFeasibilityQuery.CanApplyAnything(prospective, context), Is.False,
                "Nothing in this definition can apply, so milestone 6.5af refuses to offer it.");
            Assert.That(
                EffectOptionFeasibilityQuery.CanApplyAnything(
                    EffectResolutionSession.BuildStartFrame(
                        new EffectResolutionId("a-completely-different-identity"),
                        new CardInstanceId("feasibility-card"),
                        default,
                        new ActionDefinitionId("feasibility-action"),
                        Actor,
                        Actor,
                        ActionInvocationContext.PrintedPrimary,
                        context),
                    context),
                Is.False,
                "The probe's own resolution identity cannot change the answer, because a capability " +
                "is never given one - which is what lets the probe compose a throwaway.");
        }

        [Test]
        public void AChoiceWithOnePerformableOption_OffersNoStop()
        {
            // The control that keeps the stop measured rather than blanket: where a mode can still be
            // taken there is a decision to make, and taking it is how the action goes on.
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            Assert.That(paused.Frame.PendingChoice.StopInvocation, Is.Null);
        }

        [Test]
        public void AcceptingTheStop_EndsTheActionExactlyAsTheUnpromptedStopDid()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory));
            var paused = Start(definition);

            var stopped = EffectResolutionSession.Answer(
                paused.Frame, paused.Frame.PendingChoice.StopInvocation, Context(definition));

            Assert.That(stopped.Accepted, Is.True);
            Assert.That(stopped.IsComplete, Is.True);
            Assert.That(stopped.Frame, Is.Null, "The action closes rather than being left pending.");
            Assert.That(Kinds(stopped), Is.EqualTo(new[] { "root=ImpossibleMandatory" }),
                "The same node result and the same ordered facts the silent stop produced: this " +
                "milestone changes when the action ends, not what ending it means.");
        }

        [Test]
        public void WhileTheStopStands_AnySelectNamingAGreyedOption_IsRejectedWithoutTouchingTheFrame()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory));
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            foreach (var option in new[] { First, Second })
            {
                var refused = EffectResolutionSession.Answer(
                    paused.Frame,
                    new EffectChoiceCommand(
                        choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Select,
                        EffectChoiceCandidate.Option(option), choice.ChooserId),
                    Context(definition));

                Assert.That(refused.Accepted, Is.False);
                Assert.That(refused.RejectionReason,
                    Is.EqualTo(EffectResolutionRejectionReason.InvalidChoice));
                Assert.That(refused.Frame, Is.SameAs(paused.Frame),
                    "No continuation, outcome, or accepted choice moves.");
                Assert.That(refused.Events, Is.Empty);
            }

            var confirmed = EffectResolutionSession.Answer(
                paused.Frame,
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Confirm, default, choice.ChooserId),
                Context(definition));
            Assert.That(confirmed.Accepted, Is.False,
                "Nothing is selected and nothing may be, so there is nothing to confirm either.");
        }

        [Test]
        public void AStopIsRefusedWhereTheAuthorityNeverOfferedOne()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));
            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            var forged = EffectResolutionSession.Answer(
                paused.Frame,
                new EffectChoiceCommand(
                    choice.ResolutionId, choice.ChoiceId, EffectChoiceCommandKind.Stop, default, choice.ChooserId),
                Context(definition));

            Assert.That(forged.Accepted, Is.False,
                "A stop composed by hand against a choice that offers one option is not a way out " +
                "of taking it.");
            Assert.That(forged.RejectionReason,
                Is.EqualTo(EffectResolutionRejectionReason.InvalidChoice));
            Assert.That(forged.Frame, Is.SameAs(paused.Frame));
            Assert.That(forged.Events, Is.Empty);
        }

        [Test]
        public void AStopCannotBeBuiltForAChoiceThatStillOffersSomething()
        {
            // The invariant stated once, where it is derived, rather than trusted at each call site.
            Assert.That(
                () => new PendingEffectChoice(
                    new EffectResolutionId("stop-guard"),
                    new EffectChoiceId("node-root"),
                    Root,
                    Actor,
                    EffectChoiceKind.SelectOption,
                    new[] { EffectChoiceCandidate.Option(First), EffectChoiceCandidate.Option(Second) },
                    1,
                    1,
                    null,
                    new[] { EffectChoiceCandidate.Option(First) },
                    true),
                Throws.ArgumentException);
        }

        [Test]
        public void AnUpToChooseWithNoPerformableOption_DeclinesAndLetsTheActionContinue()
        {
            var choose = new EffectNodeDefinition(
                Root, EffectNodeKind.Choose, EffectFailurePolicy.UpTo,
                childNodeIds: new[] { First, Second },
                chooser: EffectChooserRole.Actor,
                minimumSelections: 0,
                maximumSelections: 2);
            var after = new EffectNodeId("after");
            var sequence = new EffectNodeId("sequence");
            var definition = new EffectDefinition(
                new EffectDefinitionId("up-to-choose-fixture"), "rev-1", sequence,
                new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Root, after }),
                    choose,
                    Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    Leaf(after, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)
                });

            var result = Start(definition);

            Assert.That(Kinds(result), Does.Contain("root=DeclinedOptional"),
                "An optional node that cannot be taken is not a failure.");
            Assert.That(Kinds(result), Does.Contain("after=Applied"),
                "So the rest of the action still runs, exactly as an impossible optional step does.");
            Assert.That(result.Frame, Is.Null,
                "Milestone 6.5ae leaves this case alone: where a node's own policy already declines " +
                "there is nothing for a player to close, so it never pauses and never offers a stop.");
        }

        [Test]
        public void AnOptionWhoseFirstStepOnlyDeclines_StaysOfferedForWhatFollowsIt()
        {
            // Declining and being impossible are different rules facts, and the difference decides
            // this: a declined step stops nothing, so the node after it still runs and the option is
            // worth offering.
            var inner = new EffectNodeId("declines");
            var later = new EffectNodeId("applies");
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { inner, later }),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf(inner, SyntheticEffectCapabilities.DeclinedV1, EffectFailurePolicy.Mandatory),
                Leaf(later, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Is.Empty);
        }

        [Test]
        public void AnOptionStoppedByItsFirstMandatoryStep_IsRefusedDespiteWhatFollowsIt()
        {
            // The mirror of the case above. A mandatory step that cannot be taken ends the whole
            // resolution there, so nothing later in the branch can run and the option can do nothing.
            var blocked = new EffectNodeId("blocked");
            var unreachable = new EffectNodeId("unreachable");
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { blocked, unreachable }),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf(blocked, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(unreachable, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            Assert.That(
                paused.Frame.PendingChoice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First }));
        }

        [Test]
        public void AnOptionWhoseImpossibleStepIsOptional_StaysOffered()
        {
            var skipped = new EffectNodeId("skipped");
            var later = new EffectNodeId("applies");
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { skipped, later }),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf(skipped, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble),
                Leaf(later, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Is.Empty,
                "`if able` turns an impossible step into a skip, so it stops nothing and refuses " +
                "nothing.");
        }

        [Test]
        public void AnOptionWaitingOnAnEarlierStepsOutput_IsNeverRefusedForBeingUndecidable()
        {
            // `accept-hero` reports itself impossible when its required input is missing, and at the
            // moment this walk runs the step that publishes it has not been taken. Judging it there
            // would grey a mode that works, so an undecidable node is treated as performable - the
            // control that keeps this rule from disabling whatever it cannot read.
            var declines = new EffectNodeId("declines");
            var consumes = new EffectNodeId("consumes");
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { declines, consumes }),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf(declines, SyntheticEffectCapabilities.DeclinedV1, EffectFailurePolicy.Mandatory),
                new EffectNodeDefinition(
                    consumes, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    SyntheticEffectCapabilities.AcceptHeroV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            "hero", new EffectNodeId("never-run"), "hero", EffectValueKind.Hero)
                    },
                    outputs: new[] { new EffectOutputBinding("received", EffectValueKind.Hero) })));

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates, Is.Empty);
        }

        [Test]
        public void ANestedChooseWithNoPerformableOption_RefusesTheOptionThatContainsIt()
        {
            var nested = new EffectNodeId("nested");
            var innerFirst = new EffectNodeId("inner-first");
            var innerSecond = new EffectNodeId("inner-second");
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { nested }),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                new EffectNodeDefinition(
                    nested, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { innerFirst, innerSecond },
                    chooser: EffectChooserRole.Actor,
                    minimumSelections: 1,
                    maximumSelections: 1),
                Leaf(innerFirst, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(innerSecond, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory)));

            Assert.That(
                paused.Frame.PendingChoice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First }),
                "A Choose whose every option is refused cannot be taken either, and the walk reads " +
                "that the same way the resolver does when it reaches one.");
        }

        [Test]
        public void JudgingTheOptions_CreatesNothingAndCommitsNothing()
        {
            // The reason the contract asks a separate question rather than running Execute: this
            // capability's execution creates an Active effect on the actor. A probe that executed it
            // would grant the effect merely for opening the choice.
            var timing = new ActiveEffectLifecycle();
            var participants = PlaceholderOneVsOneParticipants.Create();
            var actor = participants.Heroes.First().Id;
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.MarkActorV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory));

            var paused = EffectResolutionSession.Start(
                new EffectResolutionId("feasibility-fixture"),
                new CardInstanceId("feasibility-card"),
                default,
                new ActionDefinitionId("feasibility-action"),
                actor,
                actor,
                ActionInvocationContext.PrintedPrimary,
                new EffectResolutionContext(
                    definition, SyntheticEffectCapabilities.CreateCatalog(), participants, timing,
                    1, 1, RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill));

            Assert.That(paused.Frame.PendingChoice.UnavailableCandidates
                    .Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { Second }),
                "Fixture precondition: the walk really did judge both options.");
            Assert.That(timing.Effects, Is.Empty,
                "Asking whether an option can be taken must never take it.");
        }

        // -------------------------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------------------------

        private static EffectResolutionStepResult Start(EffectDefinition definition) =>
            EffectResolutionSession.Start(
                new EffectResolutionId("feasibility-fixture"),
                new CardInstanceId("feasibility-card"),
                default,
                new ActionDefinitionId("feasibility-action"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                Context(definition));

        private static EffectResolutionContext Context(EffectDefinition definition) =>
            new EffectResolutionContext(
                definition, SyntheticEffectCapabilities.CreateCatalog(), null, null, 1, 1,
                RuleModificationSet.Empty, EffectCardState.Absent, ActionKind.Skill);

        private static IReadOnlyList<string> Kinds(EffectResolutionStepResult result) =>
            result.Events.Select(value => $"{value.NodeId}={value.ResultKind}").ToArray();

        private static EffectNodeDefinition Leaf(
            EffectNodeId id, EffectCapabilityReference capability, EffectFailurePolicy policy) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability, policy, capability);

        /// <summary>
        /// A definition whose root is a `Choose` over the first two nodes given, with any further
        /// nodes carried along as the table the branches resolve through.
        /// </summary>
        private static EffectDefinition Choose(
            EffectFailurePolicy policy,
            int minimumSelections,
            int maximumSelections,
            params EffectNodeDefinition[] nodes)
        {
            var root = new EffectNodeDefinition(
                Root, EffectNodeKind.Choose, policy,
                childNodeIds: new[] { nodes[0].Id, nodes[1].Id },
                chooser: EffectChooserRole.Actor,
                minimumSelections: minimumSelections,
                maximumSelections: maximumSelections);
            return new EffectDefinition(
                new EffectDefinitionId("feasibility-fixture-definition"), "rev-1", Root,
                new[] { root }.Concat(nodes));
        }
    }
}
