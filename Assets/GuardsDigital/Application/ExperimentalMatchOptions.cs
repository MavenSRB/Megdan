using System;

namespace GuardsDigital.Application
{
    /// <summary>Immutable per-game options. A new host starts with all experiments disabled.</summary>
    public sealed class ExperimentalMatchOptions : IEquatable<ExperimentalMatchOptions>
    {
        public static readonly ExperimentalMatchOptions Disabled = new ExperimentalMatchOptions();
        public ExperimentalMatchOptions(bool teammateCoordination = false, bool turnTimer = false,
            int cardCommitmentSeconds = 120, int cardResolutionSeconds = 30,
            int responseSeconds = 30, int cardUpgradeSeconds = 120, int teamBankSeconds = 300,
            bool enemyCardInformation = false, bool minionBattleRemovalCapEnabled = false,
            int minionBattleRemovalCap = 0, bool heroDefeatBaseLifeCostIsOne = false,
            bool heroShieldItems = false, bool heroShieldActiveEffects = false,
            bool heroShieldCardEffects = false, bool cardModifiersItems = false,
            bool cardModifiersMinions = false, bool cardModifiersActiveEffects = false,
            bool cardModifiersCardEffects = false)
        {
            Validate(cardCommitmentSeconds, nameof(cardCommitmentSeconds));
            Validate(cardResolutionSeconds, nameof(cardResolutionSeconds));
            Validate(responseSeconds, nameof(responseSeconds));
            Validate(cardUpgradeSeconds, nameof(cardUpgradeSeconds));
            Validate(teamBankSeconds, nameof(teamBankSeconds), true);
            Validate(minionBattleRemovalCap, nameof(minionBattleRemovalCap), true);
            TeammateCoordination = teammateCoordination;
            TurnTimer = turnTimer;
            EnemyCardInformation = enemyCardInformation;
            MinionBattleRemovalCapEnabled = minionBattleRemovalCapEnabled;
            MinionBattleRemovalCap = minionBattleRemovalCapEnabled ? minionBattleRemovalCap : 0;
            HeroDefeatBaseLifeCostIsOne = heroDefeatBaseLifeCostIsOne;
            HeroShieldItems = heroShieldItems;
            HeroShieldActiveEffects = heroShieldActiveEffects;
            HeroShieldCardEffects = heroShieldCardEffects;
            CardModifiersItems = cardModifiersItems;
            CardModifiersMinions = cardModifiersMinions;
            CardModifiersActiveEffects = cardModifiersActiveEffects;
            CardModifiersCardEffects = cardModifiersCardEffects;
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
        public bool EnemyCardInformation { get; }
        public bool MinionBattleRemovalCapEnabled { get; }
        public int MinionBattleRemovalCap { get; }
        public bool HeroDefeatBaseLifeCostIsOne { get; }
        public bool HeroShieldItems { get; }
        public bool HeroShieldActiveEffects { get; }
        public bool HeroShieldCardEffects { get; }
        public bool CardModifiersItems { get; }
        public bool CardModifiersMinions { get; }
        public bool CardModifiersActiveEffects { get; }
        public bool CardModifiersCardEffects { get; }
        public int CardCommitmentSeconds { get; }
        public int CardResolutionSeconds { get; }
        public int ResponseSeconds { get; }
        public int CardUpgradeSeconds { get; }
        public int TeamBankSeconds { get; }
        public bool Equals(ExperimentalMatchOptions other) => other != null &&
            TeammateCoordination == other.TeammateCoordination && TurnTimer == other.TurnTimer &&
            EnemyCardInformation == other.EnemyCardInformation &&
            MinionBattleRemovalCapEnabled == other.MinionBattleRemovalCapEnabled &&
            MinionBattleRemovalCap == other.MinionBattleRemovalCap &&
            HeroDefeatBaseLifeCostIsOne == other.HeroDefeatBaseLifeCostIsOne &&
            HeroShieldItems == other.HeroShieldItems &&
            HeroShieldActiveEffects == other.HeroShieldActiveEffects &&
            HeroShieldCardEffects == other.HeroShieldCardEffects &&
            CardModifiersItems == other.CardModifiersItems &&
            CardModifiersMinions == other.CardModifiersMinions &&
            CardModifiersActiveEffects == other.CardModifiersActiveEffects &&
            CardModifiersCardEffects == other.CardModifiersCardEffects &&
            CardCommitmentSeconds == other.CardCommitmentSeconds && CardResolutionSeconds == other.CardResolutionSeconds &&
            ResponseSeconds == other.ResponseSeconds && CardUpgradeSeconds == other.CardUpgradeSeconds &&
            TeamBankSeconds == other.TeamBankSeconds;
        public override bool Equals(object obj) => Equals(obj as ExperimentalMatchOptions);
        public override int GetHashCode() => (TeammateCoordination ? 1 : 0) ^ (TurnTimer ? 2 : 0) ^ (EnemyCardInformation ? 4 : 0) ^
            (MinionBattleRemovalCapEnabled ? 8 : 0) ^ (HeroDefeatBaseLifeCostIsOne ? 16 : 0) ^
            (HeroShieldItems ? 32 : 0) ^ (HeroShieldActiveEffects ? 64 : 0) ^
            (HeroShieldCardEffects ? 128 : 0) ^ (CardModifiersItems ? 256 : 0) ^
            (CardModifiersMinions ? 512 : 0) ^ (CardModifiersActiveEffects ? 1024 : 0) ^
            (CardModifiersCardEffects ? 2048 : 0) ^ MinionBattleRemovalCap ^
            CardCommitmentSeconds ^ CardResolutionSeconds ^ ResponseSeconds ^ CardUpgradeSeconds ^ TeamBankSeconds;
    }
}
