using System;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// When a printed non-Skill action's card text is applied, named by the keyword the card prints.
    ///
    /// <para>Milestone 8.5. A Skill needs none of this - rulebook p.12 says "When you perform a Skill
    /// action, simply apply the card text", so the effect <i>is</i> the action and runs when the action
    /// does. An Attack's text does not: p.13 puts the "Before the Attack" text "at the very beginning"
    /// and the "After the Attack" text "at the very end, after the target either defends, or is
    /// defeated and removed", and p.14's numbered Attack/Defense order fixes both against the
    /// defender's own text in between. So an action that carries an effect root and is not a Skill has
    /// to say which of those three moments is its own, and it says it here rather than through a
    /// keyword parsed out of the transcribed sentence.</para>
    ///
    /// <para>One action declares one timing. No verified face in Wasp's set prints two attack-text
    /// keywords - the sentence before a keyword is the target specification rather than a second rider
    /// - so a card printing both would widen this to a list at the slice that first transcribes one,
    /// rather than being guessed at here.</para>
    /// </summary>
    public enum CardTextTiming
    {
        /// <summary>
        /// This action's effect root is not card text applied at an Attack checkpoint. It is what a
        /// Skill carries, because a Skill's effect is the action itself, and what every action with no
        /// effect root carries.
        /// </summary>
        None = 0,

        /// <summary>p.14 step 1: "Attacker applies 'Before the attack' text, if any."</summary>
        BeforeTheAttack = 1,

        /// <summary>p.14 step 4: "Defender applies Defense action text, if any."</summary>
        DefenseAction = 2,

        /// <summary>
        /// p.14 step 7: "Apply 'After the attack:' text on the Attack card, if any" - after step 6 has
        /// checked the attack and removed the defending hero if it succeeded.
        /// </summary>
        AfterTheAttack = 3
    }

    public static class CardTextTimingQuery
    {
        /// <summary>
        /// The already-declared timing checkpoint each keyword's moment corresponds to. Milestone 5.8
        /// introduced all three and nothing produced them for card text until now; this is the one
        /// bridge between the printed keyword and the checkpoint vocabulary, so no caller maps them a
        /// second way.
        /// </summary>
        public static TimingCheckpoint Checkpoint(CardTextTiming timing)
        {
            switch (timing)
            {
                case CardTextTiming.BeforeTheAttack:
                    return TimingCheckpoint.BeforeAttack;
                case CardTextTiming.DefenseAction:
                    return TimingCheckpoint.DefenseResponse;
                case CardTextTiming.AfterTheAttack:
                    return TimingCheckpoint.AfterAttack;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(timing), timing, "No Attack checkpoint corresponds to this card-text timing.");
            }
        }

        /// <summary>The action kind whose resolution owns this moment.</summary>
        public static ActionKind OwningActionKind(CardTextTiming timing)
        {
            switch (timing)
            {
                case CardTextTiming.BeforeTheAttack:
                case CardTextTiming.AfterTheAttack:
                    return ActionKind.Attack;
                case CardTextTiming.DefenseAction:
                    return ActionKind.Defense;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(timing), timing, "No action kind owns this card-text timing.");
            }
        }
    }
}
