using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// Milestone 8.35g. A turn whose card resolution has finished and which is waiting for its own
    /// player to hand it on.
    /// </summary>
    /// <remarks>
    /// <para>Before this, a completed resolution and the hand-off were one accepted command: the
    /// authority acknowledged the resolved card and selected the next actor without stopping. That
    /// made the last action of a turn unreachable by milestone 8.35f's rewind, whose floor and walls
    /// only exist while the turn is still being resolved, and the user reported exactly that - a
    /// misplayed final movement ended the turn before the undo could reach it.</para>
    /// <para>It is a digital affordance rather than a rules change. Rulebook p.12's turn structure is
    /// untouched and the release adds no step to it; what the release makes explicit is the consent a
    /// tabletop hand-off carries implicitly, where the next player does not simply begin while
    /// somebody is still moving a piece.</para>
    /// <para>It names the hero and the card whose resolution finished, because the acknowledgement
    /// that produced it clears the selection that would otherwise say. The side is carried too, so a
    /// projection can answer who owns the pause without walking the participant graph.</para>
    /// </remarks>
    public sealed class PendingTurnRelease
    {
        public PendingTurnRelease(HeroId actorId, CardInstanceId cardInstanceId, TeamSide teamSide)
        {
            ActorId = actorId;
            CardInstanceId = cardInstanceId;
            TeamSide = teamSide;
        }

        /// <summary>The hero whose card resolution finished, and whose seat alone may release it.</summary>
        public HeroId ActorId { get; }

        /// <summary>The card that resolution belonged to.</summary>
        public CardInstanceId CardInstanceId { get; }

        /// <summary>That hero's side.</summary>
        public TeamSide TeamSide { get; }
    }
}
