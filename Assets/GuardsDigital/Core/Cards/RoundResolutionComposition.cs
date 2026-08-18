using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// The collaborators a <see cref="RoundResolutionAuthority"/> resolves a round with.
    /// </summary>
    /// <remarks>
    /// Milestone 6.1h introduced this to end six optional parameters on the authority's constructor.
    /// It is the same contract milestone 6.1g applied to the restore entry points: every member is
    /// required, so an absent collaborator is stated rather than omitted, and adding one breaks
    /// every caller instead of silently defaulting at a call site nobody revisits.
    ///
    /// This is a parameter object, not a new indirection layer. It holds no behaviour and makes no
    /// decisions - the authority still owns how an absent collaborator is substituted, so moving
    /// arguments here changed no defaulting rule.
    /// </remarks>
    public sealed class RoundResolutionComposition
    {
        public RoundResolutionComposition(
            CardActionResolutionAuthority cardActions,
            AdjacentMoveAuthority board,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            SystemTransitionAuthority systemTransitions,
            EndOfRoundFixtureCapabilities endOfRoundCapabilities,
            IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers)
        {
            CardActions = cardActions;
            Board = board;
            Timing = timing;
            Markers = markers;
            SystemTransitions = systemTransitions;
            EndOfRoundCapabilities = endOfRoundCapabilities;
            EndOfRoundHandlers = endOfRoundHandlers;
        }

        public CardActionResolutionAuthority CardActions { get; }
        public AdjacentMoveAuthority Board { get; }
        public ActiveEffectLifecycle Timing { get; }

        /// <summary>
        /// Milestone 6.2's marker attachments. It travels beside <see cref="Timing"/> because it is the
        /// same kind of collaborator: match-scoped state the round authority holds and the snapshot's
        /// round section persists, rather than anything the round itself owns rules for. Markers are not
        /// board state, so the board authority is deliberately not where this lives.
        /// </summary>
        public MarkerAttachmentRegister Markers { get; }

        /// <summary>
        /// Milestone 6.10a's system-transition continuation. It travels here for the same reason
        /// <see cref="Markers"/> does, and the note above gives it: match-scoped state the round
        /// authority holds and the snapshot's round section persists, rather than anything the round
        /// owns rules for. Both of the contract's named production consumers - milestone 6.11's
        /// Lane-Push respawn and milestone 7.5's progression - route through the round, so this is
        /// where a paused transition is reachable from without giving the aggregate a fifth graph.
        /// </summary>
        public SystemTransitionAuthority SystemTransitions { get; }

        public EndOfRoundFixtureCapabilities EndOfRoundCapabilities { get; }
        public IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> EndOfRoundHandlers { get; }

        /// <summary>
        /// A configured match: card actions, a real board, a shared timing lifecycle, and the shared
        /// marker register.
        /// </summary>
        public static RoundResolutionComposition ForMatch(
            CardActionResolutionAuthority cardActions,
            AdjacentMoveAuthority board,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            SystemTransitionAuthority systemTransitions)
        {
            return new RoundResolutionComposition(
                cardActions, board, timing, markers, systemTransitions, null, null);
        }

        /// <summary>
        /// Card actions alone, with the authority substituting its own board-free defaults. The
        /// shape most focused fixtures need, named so that the four absent collaborators are a
        /// deliberate choice rather than four omitted arguments.
        /// </summary>
        public static RoundResolutionComposition ForCardActionsOnly(
            CardActionResolutionAuthority cardActions)
        {
            return new RoundResolutionComposition(cardActions, null, null, null, null, null, null);
        }
    }
}
