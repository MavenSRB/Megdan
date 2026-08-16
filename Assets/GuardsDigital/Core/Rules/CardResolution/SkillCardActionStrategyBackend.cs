using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// The fourth concrete card-action backend. It owns Skill's pending state and its translation to
    /// and from the layers around it; the rules of what a Skill actually does live in the effect
    /// definition and its registered capabilities, not here.
    ///
    /// That division is what acceptance check 4 asks for. A later verified capability adds a catalog
    /// entry and a handler; it does not touch this file, the dispatcher, or the turn authority. The
    /// only switch on effect-node kind in the whole engine is inside
    /// <see cref="EffectResolutionSession"/>, which is the component whose job that is.
    /// </summary>
    internal sealed class SkillCardActionStrategyBackend
    {
        private readonly EffectCapabilityCatalog catalog;
        private PendingResolution pendingResolution;

        internal SkillCardActionStrategyBackend(EffectCapabilityCatalog catalog)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        private SkillCardActionStrategyBackend(
            EffectCapabilityCatalog catalog,
            PendingResolution pendingResolution)
            : this(catalog)
        {
            this.pendingResolution = pendingResolution;
        }

        public PendingResolution PendingResolution => pendingResolution;

        public PendingEffectChoice PendingChoice => pendingResolution?.PendingChoice;

        public bool HasPendingResolution => pendingResolution != null;

        internal SkillBackendRestoreState CaptureRestoreState() =>
            pendingResolution == null ? null : new SkillBackendRestoreState(pendingResolution);

        internal static SkillCardActionStrategyBackend Restore(
            EffectCapabilityCatalog catalog,
            SkillBackendRestoreState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            state.Deconstruct(out var pending);
            return new SkillCardActionStrategyBackend(catalog, pending);
        }

        public CardActionResolutionResult StartTurnSkill(
            ActionResolutionId resolutionId,
            ActionOffer offer,
            IEnumerable<CardDefinition> definitions,
            IEnumerable<CardInstance> instances,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex)
        {
            if (!resolutionId.IsDefined)
            {
                throw new ArgumentException("A Skill requires a resolution ID.", nameof(resolutionId));
            }

            if (pendingResolution != null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var action = ActionOfferQuery.FindActionDefinition(offer, materializedDefinitions);
            if (action == null || action.Kind != ActionKind.Skill || action.EffectRoot == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                action.EffectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                components: components);
            var step = EffectResolutionSession.Start(
                new EffectResolutionId(resolutionId.Value),
                offer.SourceCardInstanceId,
                offer.SourceDefinitionReference,
                offer.ActionDefinitionId,
                offer.ActorId,
                offer.ActorId,
                offer.InvocationContext,
                context);

            return Complete(step, resolutionId, offer, true);
        }

        public CardActionResolutionResult AnswerEffectChoice(
            EffectChoiceCommand command,
            IEnumerable<CardDefinition> definitions,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            MatchComponents components,
            int roundIndex,
            int turnIndex)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (pendingResolution == null)
            {
                return Reject(CardActionResolutionRejectionReason.NoPendingResolution);
            }

            var materializedDefinitions = definitions?.ToArray() ?? throw new ArgumentNullException(nameof(definitions));
            var effectRoot = FindEffectRoot(materializedDefinitions, pendingResolution);
            if (effectRoot == null)
            {
                return Reject(CardActionResolutionRejectionReason.UnavailableOffer);
            }

            var context = new EffectResolutionContext(
                effectRoot, catalog, participants, timing, roundIndex, turnIndex, modifications,
                components: components);
            var step = EffectResolutionSession.Answer(pendingResolution, command, context);
            if (!step.Accepted)
            {
                return Reject(step.RejectionReason == EffectResolutionRejectionReason.StaleChoice
                    ? CardActionResolutionRejectionReason.StaleChoice
                    : CardActionResolutionRejectionReason.InvalidChoice);
            }

            return Complete(step, default, default, false);
        }

        /// <summary>
        /// Resolves the effect definition a pending Skill is executing from the trusted card
        /// definitions rather than from anything the frame carries. The frame holds a reference; the
        /// semantics always come from the definition the match was configured with.
        /// </summary>
        internal static EffectDefinition FindEffectRoot(
            IReadOnlyList<CardDefinition> definitions,
            PendingResolution pending)
        {
            foreach (var definition in definitions)
            {
                if (!definition.Reference.Equals(pending.SourceDefinitionReference))
                {
                    continue;
                }

                foreach (var action in definition.ActionDefinitions)
                {
                    if (action.Id.Equals(pending.ActionDefinitionId) &&
                        action.EffectRoot != null &&
                        action.EffectRoot.Reference.Equals(pending.EffectDefinitionReference))
                    {
                        return action.EffectRoot;
                    }
                }
            }

            return null;
        }

        private CardActionResolutionResult Complete(
            EffectResolutionStepResult step,
            ActionResolutionId startedResolutionId,
            ActionOffer startedOffer,
            bool isStart)
        {
            // Read the identity before overwriting the field: on the final step the frame is gone, so
            // the completing facts would otherwise have nothing to name.
            //
            // At a start the identity is the offer's, not the frame's. The frame is built from that
            // offer, so the two agree whenever a frame exists - but a Skill whose root resolves to
            // completion during the start itself leaves no frame, and there is no earlier pending
            // resolution either, because a start is refused while one is live. Reading the frame there
            // read null and faulted, which made choice-free Skills unwritable.
            ActionResolutionId resolutionId;
            CardInstanceId card;
            ActionDefinitionId action;
            HeroId actor;
            if (isStart)
            {
                resolutionId = startedResolutionId;
                card = startedOffer.SourceCardInstanceId;
                action = startedOffer.ActionDefinitionId;
                actor = startedOffer.ActorId;
            }
            else
            {
                var source = step.Frame ?? pendingResolution;
                resolutionId = new ActionResolutionId(source.ResolutionId.Value);
                card = source.SourceCardInstanceId;
                action = source.ActionDefinitionId;
                actor = source.ActorId;
            }

            pendingResolution = step.Frame;

            var steps = new List<ResolutionStep>();
            if (isStart)
            {
                steps.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionStarted));
                steps.Add(ResolutionStep.FromFact(
                    CardActionResolutionEvent.SkillActionStarted(resolutionId, startedOffer)));
            }

            foreach (var effectEvent in step.Events)
            {
                foreach (var checkpoint in effectEvent.CheckpointsBefore)
                {
                    steps.Add(ResolutionStep.AtCheckpoint(checkpoint));
                }

                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillEffectNode(
                    FactKindOf(effectEvent.ResultKind),
                    resolutionId, card, action, actor, effectEvent.NodeId.Value)));

                foreach (var checkpoint in effectEvent.CheckpointsAfter)
                {
                    steps.Add(ResolutionStep.AtCheckpoint(checkpoint));
                }
            }

            if (step.IsComplete)
            {
                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillActionCompleted(
                    resolutionId, card, action, actor)));
                steps.Add(ResolutionStep.AtCheckpoint(TimingCheckpoint.ActionEnded));
            }
            else
            {
                steps.Add(ResolutionStep.FromFact(CardActionResolutionEvent.SkillChoiceRequested(
                    resolutionId, card, action, actor, step.Frame.PendingChoice.ChoiceId.Value)));
            }

            return CardActionResolutionResult.Accept(step.IsComplete, null, null, steps);
        }

        /// <summary>
        /// The one place a node outcome becomes a fact kind. It is a translation between two closed
        /// vocabularies, not a rules decision, and it lives here rather than in any dispatcher.
        /// </summary>
        private static CardActionResolutionEventKind FactKindOf(EffectExecutionResultKind resultKind)
        {
            switch (resultKind)
            {
                case EffectExecutionResultKind.DeclinedOptional:
                    return CardActionResolutionEventKind.SkillEffectNodeDeclined;
                case EffectExecutionResultKind.ImpossibleMandatory:
                    return CardActionResolutionEventKind.SkillEffectNodeStopped;
                default:
                    return CardActionResolutionEventKind.SkillEffectNodeApplied;
            }
        }

        private static CardActionResolutionResult Reject(CardActionResolutionRejectionReason reason) =>
            CardActionResolutionResult.Reject(reason, null, null);
    }
}
