using System.Collections.Generic;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
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
            EndOfRoundFixtureCapabilities endOfRoundCapabilities,
            IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers)
        {
            CardActions = cardActions;
            Board = board;
            Timing = timing;
            EndOfRoundCapabilities = endOfRoundCapabilities;
            EndOfRoundHandlers = endOfRoundHandlers;
        }

        public CardActionResolutionAuthority CardActions { get; }
        public AdjacentMoveAuthority Board { get; }
        public ActiveEffectLifecycle Timing { get; }
        public EndOfRoundFixtureCapabilities EndOfRoundCapabilities { get; }
        public IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> EndOfRoundHandlers { get; }

        /// <summary>A configured match: card actions, a real board, and a shared timing lifecycle.</summary>
        public static RoundResolutionComposition ForMatch(
            CardActionResolutionAuthority cardActions,
            AdjacentMoveAuthority board,
            ActiveEffectLifecycle timing)
        {
            return new RoundResolutionComposition(cardActions, board, timing, null, null);
        }

        /// <summary>
        /// Card actions alone, with the authority substituting its own board-free defaults. The
        /// shape most focused fixtures need, named so that the four absent collaborators are a
        /// deliberate choice rather than four omitted arguments.
        /// </summary>
        public static RoundResolutionComposition ForCardActionsOnly(
            CardActionResolutionAuthority cardActions)
        {
            return new RoundResolutionComposition(cardActions, null, null, null, null);
        }
    }
}
