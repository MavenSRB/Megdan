using System;

namespace GuardsDigital.Application
{
    /// <summary>Immutable per-game options. A new host starts with both experiments disabled.</summary>
    public sealed class ExperimentalMatchOptions : IEquatable<ExperimentalMatchOptions>
    {
        public static readonly ExperimentalMatchOptions Disabled = new ExperimentalMatchOptions();
        public ExperimentalMatchOptions(bool teammateCoordination = false, bool turnTimer = false,
            int cardCommitmentSeconds = 120, int cardResolutionSeconds = 30,
            int responseSeconds = 30, int cardUpgradeSeconds = 120, int teamBankSeconds = 300)
        {
            Validate(cardCommitmentSeconds, nameof(cardCommitmentSeconds));
            Validate(cardResolutionSeconds, nameof(cardResolutionSeconds));
            Validate(responseSeconds, nameof(responseSeconds));
            Validate(cardUpgradeSeconds, nameof(cardUpgradeSeconds));
            Validate(teamBankSeconds, nameof(teamBankSeconds), true);
            TeammateCoordination = teammateCoordination;
            TurnTimer = turnTimer;
            CardCommitmentSeconds = cardCommitmentSeconds;
            CardResolutionSeconds = cardResolutionSeconds;
            ResponseSeconds = responseSeconds;
            CardUpgradeSeconds = cardUpgradeSeconds;
            TeamBankSeconds = teamBankSeconds;
        }
        private static void Validate(int value, string name, bool allowZero = false)
        {
            if (value < (allowZero ? 0 : 1) || value > 86400)
                throw new ArgumentOutOfRangeException(name, "Use whole seconds, up to 86400 (24 hours).");
        }
        public bool TeammateCoordination { get; }
        public bool TurnTimer { get; }
        public int CardCommitmentSeconds { get; }
        public int CardResolutionSeconds { get; }
        public int ResponseSeconds { get; }
        public int CardUpgradeSeconds { get; }
        public int TeamBankSeconds { get; }
        public bool Equals(ExperimentalMatchOptions other) => other != null &&
            TeammateCoordination == other.TeammateCoordination && TurnTimer == other.TurnTimer &&
            CardCommitmentSeconds == other.CardCommitmentSeconds && CardResolutionSeconds == other.CardResolutionSeconds &&
            ResponseSeconds == other.ResponseSeconds && CardUpgradeSeconds == other.CardUpgradeSeconds &&
            TeamBankSeconds == other.TeamBankSeconds;
        public override bool Equals(object obj) => Equals(obj as ExperimentalMatchOptions);
        public override int GetHashCode() => (TeammateCoordination ? 1 : 0) ^ (TurnTimer ? 2 : 0) ^
            CardCommitmentSeconds ^ CardResolutionSeconds ^ ResponseSeconds ^ CardUpgradeSeconds ^ TeamBankSeconds;
    }
}
