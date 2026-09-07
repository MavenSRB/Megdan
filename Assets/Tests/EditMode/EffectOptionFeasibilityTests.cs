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
        public void AttackTargetBindingProbe_DoesNotMistakeAnOptionalStepForTheMandatoryBinding()
        {
            var optional = new EffectNodeId("optional-flourish");
            var optionalWork = new EffectNodeId("optional-work");
            var blocker = new EffectNodeId("mandatory-blocker");
            var bind = new EffectNodeId("unreachable-bind");
            var definition = new EffectDefinition(
                new EffectDefinitionId("optional-before-binding-fixture"), "rev-1", Root,
                new[]
                {
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { optional, blocker, bind }),
                    new EffectNodeDefinition(
                        optional, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { optionalWork },
                        chooser: EffectChooserRole.Actor),
                    Leaf(optionalWork, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.May),
                    Leaf(blocker, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    Leaf(bind, SyntheticContinuationCapabilities.SetAttackTargetV1,
                        EffectFailurePolicy.Mandatory)
                });
            var context = Context(definition);

            Assert.That(
                EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                    Prospective(context, "optional-binding-probe"), context),
                Is.False,
                "Optional work can apply, but the mandatory path still stops before it binds a target.");
        }

        [Test]
        public void AttackTargetBindingProbe_AdmitsAModalDefinitionWhenAnyBranchCanBind()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticContinuationCapabilities.SetAttackTargetV1,
                    EffectFailurePolicy.Mandatory));
            var context = Context(definition);

            Assert.That(
                EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                    Prospective(context, "modal-binding-probe"), context),
                Is.True,
                "One legal modal branch that reaches the typed binding keeps the Attack available.");
        }

        /// <summary>
        /// THE CRIMSON TRAIL / CLAWS THAT CATCH DEFECT, fixed at milestone 8.35z49.
        ///
        /// <para>A <c>Conditional</c> is not a <c>Choose</c>: an unsatisfied "if" is a step that simply
        /// does not happen and the enclosing Sequence carries on to whatever stands after the gate.
        /// <see cref="EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding"/>'s walk did
        /// not honour that distinction - its <c>Choose</c>/<c>Conditional</c> arm returned
        /// <c>Blocked</c> whenever none of a mandatory node's own children reached the binding, which is
        /// the right answer for a <c>Choose</c> (nothing selectable leads anywhere) and the wrong one
        /// for a <c>Conditional</c> (the binding can legitimately stand after the gate).</para>
        ///
        /// <para>This fixture composes that shape directly rather than reaching into either hero's
        /// content, so it keeps proving the engine rule after any card is recomposed:
        /// <c>Sequence[Conditional(Mandatory) -&gt; [optional work], bind]</c>. Two shipped faces carry
        /// it - Whisper's Crimson Trail and Ursafar's Claws That Catch, enumerated corpus-wide by
        /// <c>AttackTargetBindingConditionalChooseOfferSweepTests</c> - and both had their Attack greyed
        /// on every board before the fix. It was written on the Whisper dealing-prep branch as an
        /// <c>[Explicit]</c> case asserting the answer the engine did not yet give; the fix lands in
        /// this same milestone, so it is an ordinary passing case here.</para>
        /// </summary>
        [Test]
        public void AttackTargetBindingProbe_CrimsonTrailShape_AMandatoryConditionalsOwnBranchNotBinding_StillLetsTheBindingAfterItStand()
        {
            var gate = new EffectNodeId("mandatory-gate");
            var gatedWork = new EffectNodeId("gated-work");
            var bind = new EffectNodeId("binding-after-the-gate");
            var definition = new EffectDefinition(
                new EffectDefinitionId("mandatory-conditional-before-binding-fixture"), "rev-1", Root,
                new[]
                {
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { gate, bind }),

                    // Crimson Trail's and Claws That Catch's shared shape: an optional step nested as
                    // the ONLY child of a MANDATORY Conditional. Nothing under the gate reaches the
                    // typed binding - it stands AFTER the gate, in the enclosing Sequence.
                    new EffectNodeDefinition(
                        gate, EffectNodeKind.Conditional, EffectFailurePolicy.Mandatory,
                        SyntheticEffectCapabilities.IsFlaggedV1,
                        childNodeIds: new[] { gatedWork },
                        inputs: new[]
                        {
                            EffectInputBinding.FromLiteral("flag", EffectValue.Boolean(true))
                        }),
                    Leaf(gatedWork, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.May),
                    Leaf(bind, SyntheticContinuationCapabilities.SetAttackTargetV1,
                        EffectFailurePolicy.Mandatory)
                });
            var context = Context(definition);

            Assert.That(
                EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                    Prospective(context, "mandatory-conditional-before-binding-probe"), context),
                Is.True,
                "The mandatory bind stands right after the gate in the enclosing Sequence, so a " +
                "legal path to it exists whatever the gate's own branch does - a Conditional is not " +
                "the only path to a binding the way a Choose with no performable arm is.");
        }

        /// <summary>
        /// The control that keeps the fix above honest: a mandatory <c>Choose</c> whose every arm fails
        /// to reach the binding still reports the Attack unavailable. Milestone 8.35z49 split the two
        /// node kinds apart, and this is the half that did NOT move - milestone 8.35z27's modal-Attack
        /// family depends on it. Without this case the fix could be widened to <c>Choose</c> and
        /// nothing would fail.
        /// </summary>
        [Test]
        public void AttackTargetBindingProbe_AMandatoryChooseWithNoBindingArm_StillRefusesTheAttack()
        {
            var definition = Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory));
            var context = Context(definition);

            Assert.That(
                EffectOptionFeasibilityQuery.CanCompleteMandatoryAttackTargetBinding(
                    Prospective(context, "modal-no-binding-probe"), context),
                Is.False,
                "A Choose IS the decision, so a mandatory one with no arm reaching the binding has no " +
                "path to one at all - unlike a Conditional, which an enclosing Sequence walks past.");
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
            var participants = StagedBoardParticipants.Create();
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
        // Milestone 8.35z35: the two tiers. Blocked is greyed; empty-but-takeable is offered.
        //
        // Ruling 17 splits one grey into two rules facts. Since milestone 6.5ac a `Choose` greyed every
        // option the walk did not prove `Performable`, which put "a mandatory step of this branch stops
        // the resolution" and "this branch can be taken and would apply nothing" behind the same
        // disabled control. Only the first is a refusal. The second is a move: p.12's Hold on the
        // actor's own modal - the page says a player is never forced to perform an action - and on a
        // node the card hands to an opponent ("An enemy hero in range chooses one") it is the escape
        // hatch the decision exists to offer. Greying it deleted a legal answer.
        //
        // The fixtures below are the same synthetic vocabulary the cases above use, and the one thing
        // that separates the tiers is the refusing node's own failure policy: `ImpossibleV1` under
        // `Mandatory` blocks, and the identical capability under `if able` is skipped, which is exactly
        // p.10's distinction rather than anything this milestone invented.
        // -------------------------------------------------------------------------------------------

        [Test]
        public void AnOptionThatCanBeTakenAndAppliesNothing_IsOfferedRatherThanGreyed()
        {
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble),
                Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.UnavailableCandidates, Is.Empty,
                "`if able` turns the impossible step into a skip, so nothing stops this branch: it " +
                "can be taken, and taking it applies nothing. That is a legal answer, not a refusal.");
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { First, Second }),
                "So both modes carry a way to select them.");
            Assert.That(choice.StopInvocation, Is.Null,
                "And nothing here is refused, so milestone 6.5ae's stop has nothing to close.");
        }

        [Test]
        public void AMixedNode_GreysTheBlockedArmAndStillOffersTheEmptyOne()
        {
            // The case that proves the split is a split rather than a blanket lift: one option of this
            // node genuinely cannot be taken and the other can, and they are the same capability.
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble)));

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.Candidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First, Second }),
                "Both modes are projected, as they always were.");
            Assert.That(choice.UnavailableCandidates.Select(candidate => candidate.OptionNodeId),
                Is.EqualTo(new[] { First }),
                "Only the arm a mandatory step stops is greyed.");
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { Second }),
                "And the empty arm keeps the way to select it.");
            Assert.That(choice.StopInvocation, Is.Null,
                "The node still offers something, so it is not milestone 6.5ae's position.");
        }

        [Test]
        public void AMandatoryNodeWhoseOptionsAreAllEmpty_OffersEveryOneOfThemAndNoStop()
        {
            // Every option applies nothing and every option can be taken. Before the split this greyed
            // all of them and offered p.10's stop, which ENDS THE ACTION - so a later step of the card
            // never ran. Taking an empty bullet completes the node instead, which is the difference
            // between a card that did nothing here and a card that stopped here.
            var paused = Start(Choose(
                EffectFailurePolicy.Mandatory, 1, 1,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble),
                Leaf(Second, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble)));

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice.UnavailableCandidates, Is.Empty);
            Assert.That(choice.SelectInvocations.Select(invocation => invocation.Candidate.OptionNodeId),
                Is.EqualTo(new[] { First, Second }));
            Assert.That(choice.StopInvocation, Is.Null,
                "The stop is for a node with nothing takeable in it, and this node is all takeable.");
        }

        [Test]
        public void ChoosingAnEmptyOption_AppliesNothingAndLetsTheRestOfTheActionRun()
        {
            // The no-op has to be honest in both directions: nothing applied, and nothing hung either.
            var after = new EffectNodeId("after");
            var sequence = new EffectNodeId("sequence");
            var definition = new EffectDefinition(
                new EffectDefinitionId("empty-option-fixture"), "rev-1", sequence,
                new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Root, after }),
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Choose, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { First, Second },
                        chooser: EffectChooserRole.Actor,
                        minimumSelections: 1,
                        maximumSelections: 1),
                    Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble),
                    Leaf(Second, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory),
                    Leaf(after, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)
                });

            var paused = Start(definition);
            var choice = paused.Frame.PendingChoice;

            var selection = choice.SelectInvocations
                .SingleOrDefault(invocation => invocation.Candidate.OptionNodeId.Equals(First));
            Assert.That(selection, Is.Not.Null,
                "The empty option carries a way to select it, which is the whole ruling.");

            var selected = EffectResolutionSession.Answer(
                paused.Frame, selection, Context(definition));
            Assert.That(selected.Accepted, Is.True);

            var confirmation = selected.Frame.PendingChoice.ConfirmInvocation;
            Assert.That(confirmation, Is.Not.Null,
                "One empty selection satisfies the node's minimum, so it is confirmable.");

            var confirmed = EffectResolutionSession.Answer(
                selected.Frame, confirmation, Context(definition));

            Assert.That(confirmed.Accepted, Is.True);
            Assert.That(confirmed.IsComplete, Is.True,
                "The action finishes rather than hanging on a pause nobody can answer.");
            Assert.That(confirmed.Frame, Is.Null);
            Assert.That(Kinds(confirmed), Does.Contain("root=Applied"),
                "The node completes: a mode was chosen and its text was applied, and its text " +
                "applying nothing is what the branch says.");
            Assert.That(Kinds(confirmed), Does.Contain("after=Applied"),
                "So the ordered step after the modal still runs - which the stop would have skipped.");
        }

        // -------------------------------------------------------------------------------------------
        // Milestone 8.29e: the same question, asked of an `Optional` node's one branch
        // -------------------------------------------------------------------------------------------

        /// <summary>
        /// The closing check. A `Choose` has refused an option it can prove does nothing since
        /// milestone 6.5ac; an `Optional` opened p.10's confirm-or-decline whatever stood behind it,
        /// so a player was asked a question whose two answers left the same board - and asked it at
        /// the one moment every other control is withheld.
        ///
        /// <para>Skipping is not a new rule. `may`, `up to` and `if able` all treat a step nobody can
        /// take as simply not taken, and that is what a declined optional already records; what
        /// changed is that the reading happens before the question rather than after the answer.</para>
        /// </summary>
        [Test]
        public void AnOptionalStepWhoseBranchCannotApply_CompletesWithoutOfferingAConfirmCommand()
        {
            var completed = Start(Optional(
                EffectFailurePolicy.May,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory)));

            Assert.That(completed.Frame, Is.Null,
                "There is nothing here to confirm and nothing to decline, so nothing is asked and " +
                "no frame is left standing to be answered.");
            Assert.That(completed.IsComplete, Is.True,
                "And the resolution is over rather than waiting on an answer nobody can give.");
            Assert.That(Kinds(completed), Is.EqualTo(new[] { "root=DeclinedOptional" }),
                "The node records what it always recorded for a step that was not taken, and the " +
                "branch behind it records nothing because it never ran.");
        }

        /// <summary>
        /// What the skipped node leaves for a replay: nothing. p.10's confirm and decline are the two
        /// answers an <see cref="EffectChoiceRecord"/> closes on, and a question nobody was asked has
        /// neither - the same position milestone 6.5ae's unprompted stop was in.
        ///
        /// <para>The second offer is what keeps the frame alive to be read, and is the other half of
        /// the claim: skipping one branch narrows what is asked, not what may be answered.</para>
        /// </summary>
        [Test]
        public void AnUnaskedOptionalStep_RecordsNoAnswerAndLeavesALaterOfferStanding()
        {
            var second = new EffectNodeId("second-may");
            var performable = new EffectNodeId("performable");
            var sequence = new EffectNodeId("sequence");
            var paused = Start(new EffectDefinition(
                new EffectDefinitionId("two-optionals-fixture"), "rev-1", sequence,
                new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Root, second }),
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { First },
                        chooser: EffectChooserRole.Actor),
                    Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    new EffectNodeDefinition(
                        second, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { performable },
                        chooser: EffectChooserRole.Actor),
                    Leaf(performable, SyntheticEffectCapabilities.NoteActorV1,
                        EffectFailurePolicy.Mandatory)
                }));

            Assert.That(paused.Frame.PendingChoice, Is.Not.Null);
            Assert.That(paused.Frame.PendingChoice.NodeId, Is.EqualTo(second),
                "The first offer was skipped and the second one - which can do something - stands.");
            Assert.That(paused.Frame.AcceptedChoices, Is.Empty,
                "And the skipped one produced no answer, so a replay has none to reproduce.");
            Assert.That(Kinds(paused), Is.EqualTo(new[] { "root=DeclinedOptional" }));
        }

        [Test]
        public void AnOptionalStepWhoseBranchCanApply_StillOpensP10sConfirmOrDecline()
        {
            var paused = Start(Optional(
                EffectFailurePolicy.May,
                Leaf(First, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            var choice = paused.Frame.PendingChoice;
            Assert.That(choice, Is.Not.Null,
                "A branch that can do something is a real offer, and the gate narrows what is " +
                "asked rather than what may be answered.");
            Assert.That(choice.Kind, Is.EqualTo(EffectChoiceKind.ConfirmOrDecline));
            Assert.That(choice.ConfirmInvocation, Is.Not.Null);
            Assert.That(choice.DeclineInvocation, Is.Not.Null,
                "Declining a `may` costs nothing and is still the chooser's to give.");
        }

        /// <summary>
        /// The rest of the action is what an optional step that was not taken leaves standing, and it
        /// stands whether the step was declined or never offered - which is the whole reason skipping
        /// is not a stop.
        /// </summary>
        [Test]
        public void AnUnaskedOptionalStep_StopsNothingAfterIt()
        {
            var after = new EffectNodeId("after");
            var sequence = new EffectNodeId("sequence");
            var definition = new EffectDefinition(
                new EffectDefinitionId("unasked-optional-fixture"), "rev-1", sequence,
                new[]
                {
                    new EffectNodeDefinition(
                        sequence, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                        childNodeIds: new[] { Root, after }),
                    new EffectNodeDefinition(
                        Root, EffectNodeKind.Optional, EffectFailurePolicy.May,
                        childNodeIds: new[] { First },
                        chooser: EffectChooserRole.Actor),
                    Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory),
                    Leaf(after, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)
                });

            var completed = Start(definition);

            Assert.That(completed.Frame, Is.Null, "Nothing paused.");
            Assert.That(Kinds(completed),
                Is.EqualTo(new[] { "root=DeclinedOptional", "after=Applied", "sequence=Applied" }),
                "The step was not taken and the ordered step after it ran, in the definition's own " +
                "order.");
        }

        /// <summary>
        /// p.11's `up to` and p.10's `may` are different words for different things - one is over a
        /// distance and the other over the whole step - but both make an impossible step one that is
        /// simply not taken, so both read their branch the same way here.
        /// </summary>
        [Test]
        public void AnUpToOptionalWhoseBranchCannotApply_IsSkippedForTheSameReasonAMayIs()
        {
            var completed = Start(Optional(
                EffectFailurePolicy.UpTo,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.Mandatory)));

            Assert.That(completed.Frame, Is.Null);
            Assert.That(Kinds(completed), Is.EqualTo(new[] { "root=DeclinedOptional" }));
        }

        /// <summary>
        /// `if able` is untouched: it never asked, so there was never a prompt to withhold. It still
        /// enters its branch and lets the step's own policy skip what cannot be taken, which is a
        /// different record from the one an unasked `may` writes.
        /// </summary>
        [Test]
        public void AnIfAbleOptional_StillEntersItsBranchRatherThanBeingDeclinedForIt()
        {
            var completed = Start(Optional(
                EffectFailurePolicy.IfAble,
                Leaf(First, SyntheticEffectCapabilities.ImpossibleV1, EffectFailurePolicy.IfAble)));

            Assert.That(completed.Frame, Is.Null);
            Assert.That(Kinds(completed),
                Is.EqualTo(new[] { "first-option=DeclinedOptional", "root=Applied" }),
                "The branch was entered and its own step is what reported it could not be taken, so " +
                "the Optional above it applied. An unoffered `may` writes that record on the " +
                "Optional itself and never reaches the step at all - the same word on a different " +
                "node, which is what tells a reader which of the two happened.");
        }

        /// <summary>
        /// A branch whose only step declines applies nothing, so there is nothing to offer - the same
        /// reading the option walk makes, and the reason declining and being impossible are still two
        /// different facts one node down.
        /// </summary>
        [Test]
        public void AnOptionalWhoseBranchOnlyDeclines_IsNotOffered()
        {
            var completed = Start(Optional(
                EffectFailurePolicy.May,
                Leaf(First, SyntheticEffectCapabilities.DeclinedV1, EffectFailurePolicy.Mandatory)));

            Assert.That(completed.Frame, Is.Null);
            Assert.That(Kinds(completed), Is.EqualTo(new[] { "root=DeclinedOptional" }));
        }

        /// <summary>
        /// And the mirror: a branch whose first step declines but whose second applies is worth
        /// offering, because a declined step stops nothing after it. The gate refuses a branch it can
        /// prove empty and never one it merely has not finished reading.
        /// </summary>
        [Test]
        public void AnOptionalWhoseLaterStepApplies_IsStillOffered()
        {
            var declines = new EffectNodeId("declines");
            var applies = new EffectNodeId("applies");
            var paused = Start(Optional(
                EffectFailurePolicy.May,
                new EffectNodeDefinition(
                    First, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { declines, applies }),
                Leaf(declines, SyntheticEffectCapabilities.DeclinedV1, EffectFailurePolicy.Mandatory),
                Leaf(applies, SyntheticEffectCapabilities.NoteActorV1, EffectFailurePolicy.Mandatory)));

            Assert.That(paused.Frame.PendingChoice, Is.Not.Null);
            Assert.That(paused.Frame.PendingChoice.NodeId, Is.EqualTo(Root));
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

        private static PendingResolution Prospective(
            EffectResolutionContext context, string resolutionId) =>
            EffectResolutionSession.BuildStartFrame(
                new EffectResolutionId(resolutionId),
                new CardInstanceId("feasibility-card"),
                default,
                new ActionDefinitionId("feasibility-action"),
                Actor,
                Actor,
                ActionInvocationContext.PrintedPrimary,
                context);

        private static IReadOnlyList<string> Kinds(EffectResolutionStepResult result) =>
            result.Events.Select(value => $"{value.NodeId}={value.ResultKind}").ToArray();

        private static EffectNodeDefinition Leaf(
            EffectNodeId id, EffectCapabilityReference capability, EffectFailurePolicy policy) =>
            new EffectNodeDefinition(id, EffectNodeKind.Capability, policy, capability);

        /// <summary>
        /// A definition whose root is an `Optional` over the first node given, with any further nodes
        /// carried along as the table that branch resolves through. Milestone 8.29e.
        /// </summary>
        private static EffectDefinition Optional(
            EffectFailurePolicy policy,
            params EffectNodeDefinition[] nodes)
        {
            var root = new EffectNodeDefinition(
                Root, EffectNodeKind.Optional, policy,
                childNodeIds: new[] { nodes[0].Id },
                // An `if able` node opens no decision, so the definition refuses one that names a
                // chooser - which is the declaration this fixture has to respect rather than restate.
                chooser: policy == EffectFailurePolicy.IfAble
                    ? EffectChooserRole.None
                    : EffectChooserRole.Actor);
            return new EffectDefinition(
                new EffectDefinitionId("optional-fixture-definition"), "rev-1", Root,
                new[] { root }.Concat(nodes));
        }

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
