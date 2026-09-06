using System;

namespace GuardsDigital.Core.Setup
{
    /// <summary>
    /// Milestone 8.35z37. Everything a match is composed FROM, as one value: which of
    /// rulebook pp.5-6's printed setups is being played, and which heroes stand on it.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this type exists at all.</b> Before it, the two decisions lived in different
    /// places and were taken at different moments - the format was a scene field read in
    /// <c>Awake</c>, and the line-up was not a decision at all because it was the printed constant.
    /// Neither could come from a lobby, because the lobby opens after the scene has composed. Naming
    /// the pair once, as a value with no Unity type and no session type in it, is what lets the same
    /// decision be taken by a fixture, by a launch flag, by a sandbox or by six people in a lobby and
    /// reach the composition through one door.</para>
    ///
    /// <para><b>The line-up is never null here.</b> A caller that has made no selection passes none
    /// and gets <see cref="RegisteredHeroRoster.PrintedLineup"/> - setup step 7's own arrangement,
    /// which is what this build has always dealt. That is the backward-compatibility guarantee
    /// stated as a default rather than as a branch: every existing caller composes the shipped board
    /// because the shipped board is the printed line-up, and the fixture behind that claim compares
    /// the two compositions byte for byte rather than arguing it.</para>
    ///
    /// <para><b>What is NOT here, and deliberately.</b> No seat, no connection, no player: who chose
    /// a hero is session vocabulary and lives in the Application layer on milestone 8.35a's law. No
    /// Tie Breaker coin and no setup seed either - those are drawn per composition rather than
    /// decided in a lobby, and putting them here would let a caller state a match's randomness
    /// alongside its rules. What crosses this boundary is what the rules need to build a board.</para>
    /// </remarks>
    public sealed class MatchCompositionPlan
    {
        /// <param name="format">One of pp.5-6's four printed setups.</param>
        /// <param name="lineup">
        /// The heroes each side fields, or <c>null</c> for setup step 7's printed line-up at that
        /// format's team size.
        /// </param>
        public MatchCompositionPlan(MatchFormat format, HeroLineup lineup = null)
        {
            Format = format ?? throw new ArgumentNullException(nameof(format));
            IsPrintedLineup = lineup == null;
            Lineup = lineup ?? RegisteredHeroRoster.PrintedLineup(format.HeroesPerTeam);

            if (Lineup.HeroesPerTeam != format.HeroesPerTeam)
            {
                throw new ArgumentException(
                    $"A {format.Id} match fields {format.HeroesPerTeam} heroes a side and this " +
                    $"line-up names {Lineup.HeroesPerTeam}.",
                    nameof(lineup));
            }
        }

        /// <summary>Setup step 4 and step 5's printed setup: the Wave count and the Life count.</summary>
        public MatchFormat Format { get; }

        /// <summary>The heroes on the board, never null - see the type's own remarks.</summary>
        public HeroLineup Lineup { get; }

        /// <summary>
        /// Whether the line-up was defaulted rather than chosen. Diagnostic only: nothing composes
        /// differently on it, because a defaulted line-up IS the printed one and composing from
        /// either produces the same match.
        /// </summary>
        public bool IsPrintedLineup { get; }

        /// <summary>The printed setup with nobody having chosen anything - what this build ships.</summary>
        public static MatchCompositionPlan Printed(MatchFormat format) =>
            new MatchCompositionPlan(format);

        /// <summary>
        /// The same setup with a different line-up standing on it. The format is what a session's
        /// seats are counted from and is therefore never changed this way - see
        /// <c>PrivateSessionHost</c>'s remarks on why the format is settled before anyone is seated.
        /// </summary>
        public MatchCompositionPlan WithLineup(HeroLineup lineup) =>
            new MatchCompositionPlan(Format, lineup);

        public override string ToString() =>
            Format.Id + " (" + (IsPrintedLineup ? "printed line-up" : "chosen line-up") + ")";
    }
}
