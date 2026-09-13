using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.Victory
{
    /// <summary>
    /// The round-owned fact a printed card claims when its text says one hero wins the match outright -
    /// milestone 8.39g1, and Cutter's A Fistful of Coins is the only sentence in the corpus that claims
    /// it.
    ///
    /// <para><b>Why this exists at all, when milestone 8.39g already built the ending.</b> That
    /// milestone built <c>MatchVictoryCause.CoinHoardClaimed</c>, <c>MatchOutcome.WonAlone</c> and
    /// <c>RoundResolutionAuthority.TryRecordSoloVictory</c>, and its non-goal read "the face that
    /// reaches this is composed at 8.39h" - as though composing the face would connect the two. It does
    /// not. A capability handler is handed <see cref="Effects.EffectRosterState"/>, the board, the
    /// participants and its own frame, and no path from any of them reaches the round authority. This
    /// interface is that path, narrowed to the one question a card may ask.
    ///
    /// <para><b>It records and does not end.</b> The claim writes the winner and returns; the match is
    /// stopped afterwards by <c>ExecuteTurnCommand</c>'s post-command check, which milestone 8.16
    /// widened from p.13's counters to "every ending the state can already describe" for exactly this
    /// shape of problem. That is what keeps <c>EndGame</c> - which stops the turn's in-flight
    /// resolution - from being called from inside the resolution that is still running. The window
    /// between the claim and the boundary is one turn command wide and never spans two, which is the
    /// same window p.8's mid-turn Lane Push already lives in.</para>
    ///
    /// <para>It is an interface rather than a reference to the round authority because the effect layer
    /// does not know about rounds and must not learn: one question, one answer, and a fixture can stand
    /// in for the authority without composing a match.</para>
    /// </summary>
    public interface ISoloVictoryClaim
    {
        /// <summary>Whether this match has already been claimed by somebody.</summary>
        bool HasBeenClaimed { get; }

        /// <summary>
        /// Records <paramref name="winningHeroId"/> as the hero who won the match alone, and reports
        /// whether the claim was taken.
        ///
        /// <para>It is refused rather than layered when the match has already ended or already been
        /// claimed - two endings cannot both be the ending - and when the hero is not one this match is
        /// playing, which is the same refusal <c>MatchOutcomeQuery</c> makes of a payload claiming one.
        /// A refusal is an ordinary answer here and not a fault: a card whose branch is reached twice
        /// has still only won once.</para>
        /// </summary>
        bool TryClaim(HeroId winningHeroId);
    }
}
