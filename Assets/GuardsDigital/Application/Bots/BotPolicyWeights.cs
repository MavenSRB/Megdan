using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace GuardsDigital.Application.Bots
{
    /// <summary>
    /// FOURTEENTH amendment, tier-2. The eleven POLICY weights as one tunable value.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this type exists, and it is the central measured finding of tier-2.</b> The nine
    /// weights in <see cref="BotEvaluationWeights"/> are what a reader would tune first, and tuning
    /// them is very nearly a no-op. The reason is structural rather than accidental: a candidate's
    /// score is <c>standing + delta</c>, and <c>standing</c> - the whole evaluation of the position
    /// as it currently is - is <b>the same number for every candidate of one decision</b>, so it
    /// cancels out of the ranking and out of the near-equal band alike. Only the DELTAS rank
    /// candidates, and of the nine evaluation weights just four appear inside any delta at all
    /// (<c>TeamLife</c> and <c>OwnCoin</c> and <c>MinionPressure</c> in the attack-target delta,
    /// <c>OwnHandSize</c> in the defense one, plus the two geometry terms). The weights below are the
    /// numbers that actually decide what a practice bot does.</para>
    /// <para>Phase A's own experience is the corroboration and nobody read it that way at the time:
    /// the one weight it successfully tuned - <c>CommitInitiativeWeight</c>, worth 31 points - is a
    /// POLICY weight, and it lives here. Its eight untouched neighbours are the rest of this vector.</para>
    /// <para><b>Content-generic, exactly as the evaluation's maintenance covenant requires.</b> Every
    /// weight below prices a STRUCTURAL fact - a card prints an Attack, a card prints Initiative, an
    /// action forfeits the turn. None names a card, a hero, an action definition or an effect, and a
    /// tuner searching this space cannot invent one, because there is no coordinate here for it.</para>
    /// </remarks>
    public sealed class BotPolicyWeights
    {
        public static readonly IReadOnlyList<string> TermNames = new[]
        {
            "HitTaken",
            "DefenseCardSpend",
            "AttackAction",
            "MovementAction",
            "EndTurn",
            "CommitInitiative",
            "CommitReachableAttack",
            "CommitMovement",
            "CommitPrintedValue",
            "Pass",
            "Respawn"
        };

        public const int TermCount = 11;

        /// <summary>
        /// The vector milestone 8.35z22 shipped, and the CONTROL every tier-2 margin is stated
        /// against. <see cref="BotPolicy.SelectTierOne"/> is pinned to it, so the brain that ships to
        /// a spawned network seat cannot drift while tier-2 is tuned.
        /// </summary>
        public static readonly BotPolicyWeights PhaseA = new BotPolicyWeights(
            hitTaken: 600,
            defenseCardSpend: 20,
            attackAction: 120,
            movementAction: 40,
            endTurn: 200,
            commitInitiative: -10,
            commitReachableAttack: 90,
            commitMovement: 45,
            commitPrintedValue: 6,
            pass: 400,
            respawn: 500);

        /// <summary>
        /// THE TUNED VECTOR. Every number is a measurement; see
        /// <c>BotTierTwoTuning.ThePolicySensitivityScreen</c> for the table each came from and
        /// <c>TheAssembledVectorConfirmation</c> for the held-out check on the assembled result.
        /// </summary>
        /// <remarks>
        /// <para><b>Two terms moved, both in the commitment family, and the screen that moved them is
        /// checked in.</b> Each row below is one weight varied against the phase-A vector, played
        /// against the shipped tier-1 brain over the twenty tuning seeds from both sides - forty
        /// matches - where <b>50.0% is the control</b>, because at the phase-A vector this selection
        /// IS tier-1:</para>
        /// <code>
        /// CommitInitiative    +10  37.5% |    0  22.5% |   -5  38.5% |  -20  50.0% |  -40  62.2%  &lt;- taken
        /// CommitPrintedValue    -6  50.0% |    0  63.2%  &lt;- taken |  3  45.0% |  12  38.5% |  24  35.0%
        /// HitTaken            -600   5.0% |    0   5.0% |  300  50.0% | 1200  50.0% | 2400  50.0%
        /// AttackAction        -120   0.0% |    0   0.0% |   60  50.0% |  240  50.0% |  480  50.0%
        /// EndTurn             -200   0.0% |    0  33.3% |  100  50.0% |  400  50.0% |  800  50.0%
        /// CommitReachableAttack -90  28.2% |    0  51.3% |   45  38.5% |  180  47.4% |  360  47.4%
        /// Pass                -400  50.0% |    0  50.0% |  200  50.0% |  800  50.0% | 1600  50.0%
        /// </code>
        /// <para><b>Read the shape, not just the winners.</b> Most of these weights are flat under
        /// scaling and catastrophic under sign flip or zeroing - <c>AttackAction</c> at 60, 240 and
        /// 480 all measure exactly 50.0%, and at 0 or -120 the bot loses every match. That is the same
        /// cancellation the evaluation suffers, one level down: within a decision the candidates are
        /// usually all of one KIND, so a weight that prices that kind shifts every candidate equally
        /// and only its SIGN, or its size relative to a competing family, can change an answer. It is
        /// why "tune the magnitudes" finds so little and why the two terms that did move are the two
        /// that trade off against each other inside a single commitment decision.</para>
        /// <para><b>ONE TERM MOVED, AND HELD-OUT SEEDS ARE WHY IT IS ONLY ONE.</b> The screen above
        /// nominated two winners. Measured on seeds no sweep ever read
        /// (<c>BotTierTwoTuning.TheCandidateVectorHeldOutScreen</c>, tuning column then held-out):</para>
        /// <code>
        /// phaseA (control)          50.0%   50.0%
        /// CommitInitiative=-40      62.2%   47.5%   &lt;- did NOT generalise; -10 stays
        /// CommitInitiative=-20      50.0%   60.0%
        /// CommitPrintedValue=0      63.2%   76.9%   &lt;- TAKEN: stronger out of sample than in
        /// CommitReachableAttack=0   51.3%   30.8%   &lt;- anti-generalised; 90 stays
        /// both winners              66.7%   45.0%   &lt;- the two together overfit outright
        /// </code>
        /// <para>So the sweep's best-looking result was a fit and its second was real. Only
        /// <c>CommitPrintedValue</c> moves.</para>
        /// <para><b>What the change MEANS, because a weight of zero deserves a reading.</b> It removes
        /// "prefer the card with the bigger printed number" from the commitment decision. What
        /// survives is <c>CommitReachableAttack</c> - whether the card's Attack can actually reach
        /// something from where this hero stands - and zeroing THAT costs 19 points, so the
        /// commitment family is not being switched off. The finding is narrower and more interesting
        /// than that: at a commitment, whether the action is USABLE decides the turn, and the size of
        /// the number printed on it does not - chasing the bigger number measurably costs matches.
        /// Recorded as a measurement, not a rule of the game.</para>
        /// <para><b>Do not hand-edit a number here.</b> The screen is re-runnable; a value changed by
        /// taste silently invalidates every win rate the notes and the gate quote. And this is a
        /// statement about THIS content and THIS match length - the roster is expanding underneath
        /// it, which is why the gate is a test rather than a note.</para>
        /// </remarks>
        public static readonly BotPolicyWeights Default = new BotPolicyWeights(
            hitTaken: 600,
            defenseCardSpend: 20,
            attackAction: 120,
            movementAction: 40,
            endTurn: 200,
            commitInitiative: -10,
            commitReachableAttack: 90,
            commitMovement: 45,
            commitPrintedValue: 0,
            pass: 400,
            respawn: 500);

        public BotPolicyWeights(
            int hitTaken,
            int defenseCardSpend,
            int attackAction,
            int movementAction,
            int endTurn,
            int commitInitiative,
            int commitReachableAttack,
            int commitMovement,
            int commitPrintedValue,
            int pass,
            int respawn)
        {
            HitTaken = hitTaken;
            DefenseCardSpend = defenseCardSpend;
            AttackAction = attackAction;
            MovementAction = movementAction;
            EndTurn = endTurn;
            CommitInitiative = commitInitiative;
            CommitReachableAttack = commitReachableAttack;
            CommitMovement = commitMovement;
            CommitPrintedValue = commitPrintedValue;
            Pass = pass;
            Respawn = respawn;
        }

        /// <summary>
        /// What a landed attack is treated as costing. Deliberately large and deliberately NOT
        /// <see cref="BotEvaluationWeights.TeamLife"/>: a successful attack is not a Defeat, and a
        /// Defeat is not always one Life counter. This is an ordering statement - a hit is worse than
        /// spending any card and better than losing the match - applied only where the authority's
        /// own preview says a hit is what would happen.
        /// </summary>
        public int HitTaken { get; }

        /// <summary>What spending a Defense card costs beyond the hand-size term: the card is gone.</summary>
        public int DefenseCardSpend { get; }

        public int AttackAction { get; }
        public int MovementAction { get; }
        public int EndTurn { get; }

        /// <summary>
        /// <b>NEGATIVE ON PURPOSE. Do not "fix" the sign.</b> A lower printed Initiative is preferred,
        /// so the bot deliberately tries to act LATER in the round.
        /// </summary>
        /// <remarks>
        /// This started at <c>+10</c>, from a fact that is true and an inference from it that is not.
        /// The fact: the Initiative authority takes the MAXIMUM total, so a higher printed Initiative
        /// really does act first. The inference - that acting first is worth having - is what phase
        /// A's self-play falsified: at <c>+10</c> the win rate against the tier-0.5 pass was 51.3%
        /// and the brain won 3 of 20 from the first-acting side; at <c>-10</c> it was 82.5% and 13 of
        /// 20. Nothing else changed, and held-out seeds gave 80.0%. Recorded as a measurement, not a
        /// rule: it is a property of the current content and match length.
        /// </remarks>
        public int CommitInitiative { get; }

        public int CommitReachableAttack { get; }
        public int CommitMovement { get; }
        public int CommitPrintedValue { get; }
        public int Pass { get; }
        public int Respawn { get; }

        /// <summary>Sweep access, in <see cref="TermNames"/> order. Not for shipped code.</summary>
        public int this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return HitTaken;
                    case 1: return DefenseCardSpend;
                    case 2: return AttackAction;
                    case 3: return MovementAction;
                    case 4: return EndTurn;
                    case 5: return CommitInitiative;
                    case 6: return CommitReachableAttack;
                    case 7: return CommitMovement;
                    case 8: return CommitPrintedValue;
                    case 9: return Pass;
                    case 10: return Respawn;
                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(index), index, "There are " + TermCount + " policy weights.");
                }
            }
        }

        /// <summary>This vector with one term replaced. The sweep's only mutation.</summary>
        public BotPolicyWeights WithTerm(int index, int value)
        {
            if (index < 0 || index >= TermCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index), index, "There are " + TermCount + " policy weights.");
            }

            var values = new int[TermCount];
            for (var term = 0; term < TermCount; term++)
            {
                values[term] = this[term];
            }

            values[index] = value;
            return new BotPolicyWeights(
                values[0], values[1], values[2], values[3], values[4], values[5],
                values[6], values[7], values[8], values[9], values[10]);
        }

        public string ToVectorString() =>
            string.Join(", ", Enumerable.Range(0, TermCount).Select(index =>
                TermNames[index] + "=" + this[index].ToString(CultureInfo.InvariantCulture)));

        public override string ToString() => ToVectorString();
    }
}
