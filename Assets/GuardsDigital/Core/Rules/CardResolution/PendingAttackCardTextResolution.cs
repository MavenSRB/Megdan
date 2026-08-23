using System;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Rules.EffectResolution;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>
    /// Milestone 8.10: an Attack stopped inside one of rulebook p.14's card-text moments, because the
    /// text applied there asked the player a question.
    ///
    /// <para>Before this, card text inside an Attack could not pause at all. p.14 produces an attack's
    /// remaining steps in the command that began them, so a question standing in the middle had nothing
    /// to hold the rest of the page in - and rather than fault halfway through an announced attack, a
    /// composition that could ask one was refused at content load. Wasp's three radius riders ask two
    /// questions each ("an enemy hero in radius and not adjacent to you discards a card"), so this
    /// milestone owes the pause and the schema move it costs.</para>
    ///
    /// <para><b>What it carries and what it does not.</b> It is authority-owned serializable data with
    /// no handler, delegate or callback in it, like every other continuation in this engine. The frame
    /// is the same <see cref="PendingResolution"/> a paused Skill persists, resumed through the same
    /// <c>EffectResolutionSession.Answer</c>. Beside it are the moment the page stopped at and the
    /// attack it stopped inside - and those two, not a list of steps, are what the rest of the page is
    /// rebuilt from when the question is answered. The steps themselves are deliberately absent: they
    /// are facts about an attack rather than identity, and
    /// <see cref="AttackDefenseCardActionStrategyBackend.StepsAfterCardText"/> is the one generator
    /// that produces them whether or not anything paused.</para>
    /// </summary>
    public sealed class PendingAttackCardTextResolution
    {
        public PendingAttackCardTextResolution(
            PendingResolution frame,
            CardTextTiming moment,
            PendingAttackResolution attack,
            AttackResolutionOutcome outcome)
        {
            if (frame == null)
            {
                throw new ArgumentNullException(
                    nameof(frame), "A paused card-text continuation is the frame that paused.");
            }

            if (attack == null)
            {
                throw new ArgumentNullException(
                    nameof(attack),
                    "A paused card-text continuation names the attack whose remaining steps it owes.");
            }

            if (moment != CardTextTiming.AfterTheAttack &&
                moment != CardTextTiming.DefenseAction &&
                moment != CardTextTiming.BeforeTheAttack)
            {
                throw new ArgumentException(
                    "Only rulebook p.14's three numbered card-text moments leave a remainder that can " +
                    "be rebuilt from the attack's identity, so only they can hold a question.",
                    nameof(moment));
            }

            // Milestone 8.19a. Step 1 stands before the page announces anything, so a pause there has
            // no outcome to report yet; the two later moments do. Stating it here keeps a payload from
            // claiming an attack succeeded while its very first step is still being applied.
            if (moment == CardTextTiming.BeforeTheAttack && outcome != AttackResolutionOutcome.None)
            {
                throw new ArgumentException(
                    "Rulebook p.14's step 1 precedes every outcome the page reaches, so a question " +
                    "standing in it reports none.",
                    nameof(outcome));
            }

            if (!Enum.IsDefined(typeof(AttackResolutionOutcome), outcome))
            {
                throw new ArgumentException(
                    "A paused card-text continuation states the outcome its remainder reports.",
                    nameof(outcome));
            }

            Frame = frame;
            Moment = moment;
            Attack = attack;
            Outcome = outcome;
        }

        /// <summary>The paused effect frame, identical in kind to a paused Skill's.</summary>
        public PendingResolution Frame { get; }

        /// <summary>Which of p.14's moments the page stopped at.</summary>
        public CardTextTiming Moment { get; }

        /// <summary>The attack whose remaining steps are owed when the question is answered.</summary>
        public PendingAttackResolution Attack { get; }

        /// <summary>The outcome that remainder reports; <c>None</c> at step 4, where it has none.</summary>
        public AttackResolutionOutcome Outcome { get; }
    }
}
