using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.LanePush;
using GuardsDigital.Core.Rules.Progression;
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
            LanePushConsumption lanePush,
            HeroLevelUpStepRules levelUp,
            TeamLifeState teamLives,
            EndOfRoundStageAvailability endOfRoundStageAvailability,
            IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> endOfRoundHandlers)
        {
            CardActions = cardActions;
            Board = board;
            Timing = timing;
            Markers = markers;
            SystemTransitions = systemTransitions;
            LanePush = lanePush;
            LevelUp = levelUp;
            TeamLives = teamLives;
            EndOfRoundStageAvailability = endOfRoundStageAvailability;
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

        /// <summary>
        /// Milestone 6.11's Lane-Push authority: rulebook p.8's shared Wave counters and the stages
        /// that spend them. It travels beside <see cref="SystemTransitions"/> because the two are one
        /// mechanism seen from two sides - this owns what p.8's steps do, that owns where the
        /// transition is - and because the round's snapshot section is where both are persisted.
        ///
        /// Milestone 6.10 deliberately left it out of the match, since a push that could run only its
        /// first two steps would have moved the Battle Zone away from minions nothing removed.
        /// </summary>
        public LanePushConsumption LanePush { get; }

        /// <summary>
        /// Milestone 7.5: what p.15's and p.16's leveling steps do. It travels beside
        /// <see cref="LanePush"/> because it is the same arrangement seen twice - these rules own what
        /// a step does and <see cref="SystemTransitions"/> owns where the transition is - and because
        /// the round authority is what binds them the Levels, items and coin ledger they move.
        ///
        /// Absent outside a match with a coin ledger to spend from, in which case the leveling stage
        /// finds no rules and refuses rather than substituting a purchase nobody could pay for.
        /// </summary>
        public HeroLevelUpStepRules LevelUp { get; }

        /// <summary>
        /// Milestone 7.4: rulebook p.13's Life counters, which a hero Defeat spends. It travels here
        /// rather than being created by the authority because the starting count is a setup decision -
        /// the prototype baseline records five per team, the staged 1v1 fixture composes four - and the
        /// composed setup is the one record of it. An absent one is substituted by the authority with
        /// the baseline count, in the shape <see cref="Markers"/> is substituted, so a board-free
        /// fixture still has counters to spend rather than a null to check.
        /// </summary>
        public TeamLifeState TeamLives { get; }

        public EndOfRoundStageAvailability EndOfRoundStageAvailability { get; }
        public IReadOnlyDictionary<EndOfRoundStage, IEndOfRoundStageHandler> EndOfRoundHandlers { get; }

        /// <summary>
        /// A configured match: card actions, a real board, a shared timing lifecycle, the shared
        /// marker register, and the two halves of the Lane-Push transition.
        /// </summary>
        public static RoundResolutionComposition ForMatch(
            CardActionResolutionAuthority cardActions,
            AdjacentMoveAuthority board,
            ActiveEffectLifecycle timing,
            MarkerAttachmentRegister markers,
            SystemTransitionAuthority systemTransitions,
            LanePushConsumption lanePush,
            HeroLevelUpStepRules levelUp = null,
            TeamLifeState teamLives = null)
        {
            return new RoundResolutionComposition(
                cardActions, board, timing, markers, systemTransitions, lanePush, levelUp, teamLives,
                null, null);
        }

        /// <summary>
        /// Card actions alone, with the authority substituting its own board-free defaults. The
        /// shape most focused fixtures need, named so that the five absent collaborators are a
        /// deliberate choice rather than five omitted arguments.
        /// </summary>
        public static RoundResolutionComposition ForCardActionsOnly(
            CardActionResolutionAuthority cardActions)
        {
            return new RoundResolutionComposition(
                cardActions, null, null, null, null, null, null, null, null, null);
        }
    }
}
