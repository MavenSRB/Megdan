using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// FOURTEENTH amendment, tier-2. The nine evaluation weights as one value a tuner can vary,
    /// rather than nine <c>const</c>s a tuner could only change by rebuilding.
    /// </summary>
    /// <remarks>
    /// <para><b>THE MAINTENANCE COVENANT of <see cref="BotEvaluation"/> governs this type too</b>, and
    /// this type is the reason it can: a weight is a number attached to a STRUCTURAL term, so a tuner
    /// searching this space can never invent a per-card judgment - there is no coordinate here for one
    /// to live in. Adding a tenth term is a deliberate act that must justify itself against that
    /// covenant; adding a tenth weight is not a way around it.</para>
    /// <para><b>Why a vector and not nine constants.</b> Phase A tuned exactly one weight by hand and
    /// it moved the win rate 31 points, which is the strongest possible argument that the other eight
    /// were never checked. A sweep needs to construct a candidate vector, play it, and throw it away;
    /// <see cref="WithTerm"/> and the indexer exist for exactly that and for nothing else in shipped
    /// code, where <see cref="Default"/> is the only vector anything reads.</para>
    /// <para><b>Integers, deliberately</b>, for the same reason <see cref="BotEvaluation"/> scores in
    /// whole points: "these two candidates tie" must be an exact question so the seeded near-equal
    /// pick that follows it is reproducible on every runtime, rather than depending on
    /// floating-point association order.</para>
    /// </remarks>
    public sealed class BotEvaluationWeights
    {
        /// <summary>
        /// The term order. It is the sweep's coordinate order and the order
        /// <see cref="ToVectorString"/> prints, so a logged vector can be read back by hand.
        /// </summary>
        public static readonly IReadOnlyList<string> TermNames = new[]
        {
            "TeamLife",
            "BoardPresence",
            "LevelAdvantage",
            "OwnLevel",
            "OwnCoin",
            "MinionPressure",
            "EngagementProximity",
            "ThreatAdjacency",
            "OwnHandSize"
        };

        public const int TermCount = 9;

        /// <summary>
        /// The vector milestone 8.35z22 shipped: eight hand-set numbers and one measured one
        /// (Initiative, which lives on <see cref="BotPolicy"/> rather than here).
        /// </summary>
        /// <remarks>
        /// Kept as a named vector rather than deleted, because it is the control every tier-2
        /// measurement is stated against. A gate that says "tier-2 beats tier-1" is only meaningful
        /// while the thing it beat is still constructible.
        /// </remarks>
        public static readonly BotEvaluationWeights PhaseA = new BotEvaluationWeights(
            teamLife: 1000,
            boardPresence: 30,
            levelAdvantage: 40,
            ownLevel: 25,
            ownCoin: 6,
            minionPressure: 12,
            engagementProximity: 4,
            threatAdjacency: 15,
            ownHandSize: 5);

        /// <summary>
        /// THE TUNED VECTOR, and every number in it is a measurement rather than a preference.
        /// </summary>
        /// <remarks>
        /// <para>Produced by <c>BotTierTwoTuning</c>'s coordinate sweep: each term in turn was played
        /// over a fixed set of TUNING seeds against the shipped tier-1 brain, from both sides, and the
        /// value with the best win rate was kept before moving to the next term. The gate that
        /// certifies the result plays HELD-OUT seeds this sweep never saw, exactly as phase A's single
        /// weight was certified, because a vector chosen on seeds is a vector that can fit them.</para>
        /// <para><b>Do not hand-edit a number here.</b> The sweep is checked in and re-runnable; a
        /// value changed by taste rather than by a re-run silently invalidates every win rate the
        /// notes and the gate quote. Re-run the sweep and move the whole vector.</para>
        /// <para><b>It is a statement about this content and this match length</b>, exactly as phase
        /// A's Initiative sign is, and the roster is expanding underneath it. That is why the gate is
        /// a test: a hero batch that moves the game moves the win rate, and a moved win rate fails
        /// loudly instead of rotting quietly.</para>
        /// </remarks>
        public static readonly BotEvaluationWeights Default = new BotEvaluationWeights(
            teamLife: 1000,
            boardPresence: 30,
            levelAdvantage: 40,
            ownLevel: 25,
            ownCoin: 6,
            minionPressure: 12,
            engagementProximity: 4,
            threatAdjacency: 15,
            ownHandSize: 5);

        public BotEvaluationWeights(
            int teamLife,
            int boardPresence,
            int levelAdvantage,
            int ownLevel,
            int ownCoin,
            int minionPressure,
            int engagementProximity,
            int threatAdjacency,
            int ownHandSize)
        {
            TeamLife = teamLife;
            BoardPresence = boardPresence;
            LevelAdvantage = levelAdvantage;
            OwnLevel = ownLevel;
            OwnCoin = ownCoin;
            MinionPressure = minionPressure;
            EngagementProximity = engagementProximity;
            ThreatAdjacency = threatAdjacency;
            OwnHandSize = ownHandSize;
        }

        /// <summary>Own team's Life counters minus the strongest enemy team's. The win condition.</summary>
        public int TeamLife { get; }

        /// <summary>Own heroes standing minus enemy heroes; one awaiting respawn is absent.</summary>
        public int BoardPresence { get; }

        public int LevelAdvantage { get; }
        public int OwnLevel { get; }
        public int OwnCoin { get; }

        /// <summary>Own minions standing minus enemy minions - the board-public lane-pressure proxy.</summary>
        public int MinionPressure { get; }

        /// <summary><see cref="BotEvaluation.ProximityCap"/> minus the clamped distance to the nearest enemy unit.</summary>
        public int EngagementProximity { get; }

        /// <summary>
        /// Applied NEGATIVELY by the evaluation and scaled by p.13's price of this hero's own Defeat,
        /// so the number here is a magnitude. A larger value means a more cautious bot.
        /// </summary>
        public int ThreatAdjacency { get; }

        public int OwnHandSize { get; }

        /// <summary>Sweep access, in <see cref="TermNames"/> order. Not for shipped code.</summary>
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return TeamLife;
                    case 1: return BoardPresence;
                    case 2: return LevelAdvantage;
                    case 3: return OwnLevel;
                    case 4: return OwnCoin;
                    case 5: return MinionPressure;
                    case 6: return EngagementProximity;
                    case 7: return ThreatAdjacency;
                    case 8: return OwnHandSize;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(index), index, "There are " + TermCount + " evaluation terms.");
                }
            }
        }

        /// <summary>This vector with one term replaced. The sweep's only mutation.</summary>
        public BotEvaluationWeights WithTerm(int index, int value)
        {
            if (index < 0 || index >= TermCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "There are " + TermCount + " evaluation terms.");
            }

            var values = new int[TermCount];
            for (var term = 0; term < TermCount; term++)
            {
                values[term] = this[term];
            }

            values[index] = value;
            return new BotEvaluationWeights(
                values[0], values[1], values[2], values[3], values[4],
                values[5], values[6], values[7], values[8]);
        }

        /// <summary>
        /// The vector as one line, in <see cref="TermNames"/> order. What a sweep logs and what a
        /// reader pastes back into <see cref="Default"/>.
        /// </summary>
        public string ToVectorString() =>
            string.Join(", ", Enumerable.Range(0, TermCount).Select(index =>
                TermNames[index] + "=" + this[index].ToString(CultureInfo.InvariantCulture)));

        public override string ToString() => ToVectorString();
    }
}
