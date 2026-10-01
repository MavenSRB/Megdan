using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Victory
{
    /// <summary>
    /// Milestone 9.30g. One team's count toward a victory a printed card awards to that team - Gydion's
    /// Wish: "After you cast the "Wish" spell three times your team wins the game."
    ///
    /// <para><b>Recorded, because nothing else remembers it.</b> A Wish lies faceup only until the
    /// next Prepare Spells returns it, so the removed pile cannot say how often it was cast; the count
    /// is match-scoped, public (GY-Q10: it survives rounds and his Defeat) and written beside the
    /// other recorded victory fact, <c>RoundCursorRestoreState.SoloVictoryHeroId</c>.</para>
    ///
    /// <para><b>The threshold travels with the count.</b> It is the card's printed number, bound at
    /// the step that advances the count, so every seat reads "n/3" off the same record rather than off
    /// a constant the presentation would have to know a hero by.</para>
    /// </summary>
    public readonly struct TeamVictoryTally : IEquatable<TeamVictoryTally>
    {
        public TeamVictoryTally(TeamId teamId, int count, int threshold)
        {
            if (!teamId.IsDefined)
                throw new ArgumentException("A tally names the team it counts for.", nameof(teamId));
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count), count, "A tally exists once it has counted.");
            if (threshold < 1)
                throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "A printed threshold is positive.");
            TeamId = teamId;
            Count = count;
            Threshold = threshold;
        }

        public TeamId TeamId { get; }

        public int Count { get; }

        /// <summary>The printed number of times the count must reach for the team to win.</summary>
        public int Threshold { get; }

        public bool IsReached => Count >= Threshold;

        public bool Equals(TeamVictoryTally other) =>
            TeamId.Equals(other.TeamId) && Count == other.Count && Threshold == other.Threshold;

        public override bool Equals(object obj) => obj is TeamVictoryTally other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = TeamId.GetHashCode();
                hash = (hash * 397) ^ Count;
                return (hash * 397) ^ Threshold;
            }
        }

        /// <summary>"1/3": what the plaque prints beside the purple card.</summary>
        public override string ToString() =>
            Count.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/" +
            Threshold.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Milestone 9.30g. The round-owned count and claim a printed team victory reaches, handed to the
    /// effect frame beside <see cref="ISoloVictoryClaim"/> and for its reason: a capability reaches no
    /// round authority but through <c>EffectRosterState</c>.
    ///
    /// <para><b>It records and does not end</b>, exactly as the solo claim: the match is stopped at the
    /// end of the turn command by <c>RoundResolutionAuthority</c>'s post-command check, so
    /// <c>EndGame</c> is never called from inside the resolution still running.</para>
    /// </summary>
    public interface ITeamVictoryTally
    {
        /// <summary>Every team's tally, ordered by team id; a team that never counted has none.</summary>
        IReadOnlyList<TeamVictoryTally> TeamVictoryTallies { get; }

        /// <summary>The team a printed team victory was claimed for, or the named absence.</summary>
        TeamId TeamVictoryTeamId { get; }

        /// <summary>
        /// Adds one to <paramref name="heroId"/>'s team's tally toward <paramref name="threshold"/>.
        /// Refused when the match has ended or the hero is not one this match plays.
        /// </summary>
        bool TryAdvanceTeamVictoryTally(HeroId heroId, int threshold);

        /// <summary>
        /// Records <paramref name="heroId"/>'s team as the winner when its tally has reached its
        /// threshold. Refused, as an ordinary answer, when the match has ended, when an ending is
        /// already claimed or already describable from the state (GY-Q10: an ending the inner spell
        /// caused stands), and when the tally is short.
        /// </summary>
        bool TryClaimTeamVictory(HeroId heroId);
    }

    /// <summary>Milestone 9.30g. The tally that counts for a hero's team, read from the public list.</summary>
    public static class TeamVictoryTallyQuery
    {
        public static TeamVictoryTally? ForHero(
            MatchParticipants participants, IEnumerable<TeamVictoryTally> tallies, HeroId heroId)
        {
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (!heroId.IsDefined || !participants.Heroes.Any(hero => hero.Id.Equals(heroId)))
                return null;
            var team = participants.GetPlayerForHero(heroId).TeamId;
            foreach (var tally in tallies ?? Enumerable.Empty<TeamVictoryTally>())
            {
                if (tally.TeamId.Equals(team)) return tally;
            }

            return null;
        }
    }
}
