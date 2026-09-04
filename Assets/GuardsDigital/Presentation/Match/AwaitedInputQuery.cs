using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Whose input the match is waiting on, asked of the projection rather than of what a surface
    /// happens to be drawing.
    ///
    /// <para>The user's requirement is one predicate at three scopes - "is it waiting on ME" for the
    /// top bar, "who is it waiting on" for the hero trays, and "what do I answer with" for the cards -
    /// so it is derived once here and read by each surface rather than restated per surface. This
    /// milestone owns the viewer scope; the other two read the same derivation.</para>
    ///
    /// <para><b>Why it is not the reading it replaces.</b> The narration lane's warm voice was a
    /// fifteen-way disjunction over the controls the round view model happened to expose, which is a
    /// good approximation and has one real hole: a commitment is not one of the declared pending
    /// decision families and exposes no round-action control, because the hand owns it. So the most
    /// common wait in the game - every player choosing a card at the top of a round - reached the
    /// player as the calm voice. Reading the projection answers that arm directly.</para>
    ///
    /// <para><b>Why the offer set is a scoped fact and not control presence.</b>
    /// <see cref="IMatchReadModel.AvailableTurnActions"/> and
    /// <see cref="IMatchReadModel.AvailableHoldOffer"/> are published to a seat only while
    /// <c>isActor &amp;&amp; !standing</c> - the Application layer's own scoping - so a non-empty
    /// offer set already means "this seat's turn, with no decision standing anywhere". That is what
    /// keeps an attacker's bar from lighting while the defender is the one being asked: the attacker
    /// is still the selected actor, but their offers are withheld for exactly that reason, and
    /// <see cref="IMatchReadModel.SelectedActor"/> alone could not tell the two apart.</para>
    /// </summary>
    public static class AwaitedInputQuery
    {
        /// <summary>
        /// Whether the match is waiting on input from the viewer this projection was built for.
        ///
        /// <para><paramref name="viewerHeroId"/> is the seat's own hero, or the undefined hero for
        /// the Director, who answers for everyone. Every other fact read here is already scoped to
        /// the viewer by the projection itself, which is why the hero is needed for the commitment
        /// arm alone: commitments are public, so the viewer's own has to be named.</para>
        /// </summary>
        public static bool IsWaitingOnViewer(IMatchReadModel projection, HeroId viewerHeroId)
        {
            if (projection == null || projection.Phase == RoundResolutionPhase.GameEnded)
            {
                // An ended match is a terminal state rather than another wait, the reading milestone
                // 8.30c gave the status line for the same reason.
                return false;
            }

            return OwnsAStandingDecision(projection) ||
                OwesACommitment(projection, viewerHeroId) ||
                MayTakeATurnAction(projection);
        }

        /// <summary>
        /// A standing decision this viewer may answer.
        ///
        /// <para>A seat projection publishes the contract only to the seat that owns it - "null unless
        /// this seat owns it; a seat waiting on another seat's decision sees the owner and no
        /// answers" - and the Director may answer every family, so one read is honest at both
        /// compositions. It covers every declared pending family at once, which is what keeps a
        /// twelfth from having to be added here by hand.</para>
        /// </summary>
        private static bool OwnsAStandingDecision(IMatchReadModel projection) =>
            projection.DecisionContract != null;

        /// <summary>
        /// Rulebook p.7's commitment, still owed by the viewer. This is the arm the control-presence
        /// reading could not see.
        /// </summary>
        private static bool OwesACommitment(IMatchReadModel projection, HeroId viewerHeroId)
        {
            var commitments = projection.Commitments;
            if (commitments == null || commitments.Count == 0)
            {
                return false;
            }

            if (!viewerHeroId.IsDefined)
            {
                // The Director commits for every seat, so any outstanding commitment is one it owes.
                return commitments.Any(
                    commitment => commitment.Status == TurnCardCommitmentStatus.AwaitingResponse);
            }

            foreach (var commitment in commitments)
            {
                if (commitment.HeroId.Equals(viewerHeroId))
                {
                    return commitment.Status == TurnCardCommitmentStatus.AwaitingResponse;
                }
            }

            return false;
        }

        /// <summary>
        /// The viewer's own turn, with the match accepting an action from them. Both readings are the
        /// projection's own scoped offer set rather than a re-derivation of whose turn it is; an
        /// offer that exists but is unavailable is not a wait, which is why availability is asked.
        /// </summary>
        private static bool MayTakeATurnAction(IMatchReadModel projection)
        {
            var hold = projection.AvailableHoldOffer;
            if (hold.HasValue && hold.Value.IsAvailable)
            {
                return true;
            }

            var actions = projection.AvailableTurnActions;
            return actions != null && actions.Any(action => action.IsAvailable);
        }
    }
}
