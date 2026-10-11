using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// W-026. The one side a Director plays when an automatic controller plays the other - the
    /// separate PvE executable's player directs Blue while its enemies act on their own. Every
    /// existing composition names no side, and every reader of this class is skipped for it, so the
    /// Director keeps answering for everybody exactly as before.
    /// </summary>
    public static class DirectorPlayerSide
    {
        /// <summary>Whether the hero belongs to the side, by the match's own setup.</summary>
        public static bool IsOnSide(IMatchReadModel projection, HeroId hero, TeamSide side) =>
            hero.IsDefined &&
            projection.Setup.Heroes.Any(value => value.HeroId.Equals(hero) && value.TeamSide == side);

        /// <summary>
        /// Whether the side owns the standing moment: the decision that stands, when one does, and
        /// otherwise the turn being resolved - judged by the hero deciding it, as
        /// <see cref="DelegatedActionPresentation.DecidingHeroFor"/> reads it. Commitments and the
        /// gaps between decisions belong to nobody in particular, so they read as the side's own.
        /// </summary>
        public static bool OwnsTheMoment(IMatchReadModel projection, TeamSide side)
        {
            var owner = projection.DecisionOwner;
            if (owner.IsDefined)
            {
                return owner.IsTeamOwned
                    ? owner.TeamSide == side
                    : owner.HeroIds.Any(hero => IsOnSide(projection, hero, side));
            }

            if (projection.Phase == RoundResolutionPhase.AwaitingCommitments ||
                !projection.SelectedActor.IsDefined)
            {
                return true;
            }

            return IsOnSide(projection, DelegatedActionPresentation.DecidingHeroFor(projection), side);
        }
    }
}
