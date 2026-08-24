using System;
using GuardsDigital.Core.Cards;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// The named action or effect family a restriction or an immunity is declared against.
    ///
    /// Rulebook p.18 makes the distinction load-bearing: a unit that "is immune" is not affected by your
    /// actions at all, while a unit immune "to a specific action type" is unaffected by that type "but is
    /// affected by other effects and actions". <see cref="All"/> is the first form; every other member is
    /// the second, so a declaration cannot be silently widened by omission.
    ///
    /// The members mirror <see cref="ActionKind"/> plus the four effect families that are not action
    /// kinds - <see cref="ObjectPush"/> (p.18, Push), <see cref="Swap"/> (p.19), <see cref="Place"/>
    /// (p.18), and <see cref="DirectMove"/> (p.11 card-text movement). <see cref="RuleActivityQuery.ForAction"/>
    /// is the only bridge between the two enums;
    /// neither is cast into the other, and it deliberately maps no action kind to the three families.
    /// </summary>
    public enum RuleActivity
    {
        None = 0,

        /// <summary>Every action and effect, which is p.18's unqualified "if a unit is immune".</summary>
        All = 1,
        Movement = 2,
        Attack = 3,
        Skill = 4,
        Defense = 5,
        Hold = 6,
        FastTravel = 7,
        Clear = 8,

        /// <summary>
        /// Official straight-line forced displacement (p.18). Milestone 6.4 implements the effect and is
        /// the consumer that reads this member; milestone 6.8b's immunity mode then names it as its
        /// originating effect family, which is why 6.8b is scheduled after 6.4 rather than before it.
        /// </summary>
        ObjectPush = 9,

        /// <summary>
        /// Rulebook p.19's Swap: "Exchange the position of two objects on the board." Milestone 8.7
        /// names it for the reason milestone 6.4 named <see cref="ObjectPush"/> - it is a thing the
        /// rules do to an object, which a source can be immune to or restricted from, and which no
        /// action kind is. Wasp's Magnetic Dagger is the first verified text to name it.
        /// </summary>
        Swap = 10,

        /// <summary>
        /// Rulebook p.18's Place: "Take an object and put it into a different space (without moving)."
        /// Named beside <see cref="Swap"/> and by the same card.
        /// </summary>
        Place = 11,

        /// <summary>
        /// Card text that directly moves a named unit without performing a Movement action. It is
        /// separate from Movement for the same reason Swap and Place are: the operation targets the
        /// moved unit and can be named by an exact immunity or Heavy-immunity exception.
        /// </summary>
        DirectMove = 12
    }

    public static class RuleActivityQuery
    {
        /// <summary>
        /// The activity one performed action belongs to. An explicit switch rather than a cast: the two
        /// enums are different vocabularies that happen to overlap, and a numeric cast between them would
        /// silently invent a mapping the moment either gains a member.
        /// </summary>
        public static RuleActivity ForAction(ActionKind actionKind)
        {
            switch (actionKind)
            {
                case ActionKind.Movement:
                    return RuleActivity.Movement;
                case ActionKind.Attack:
                    return RuleActivity.Attack;
                case ActionKind.Skill:
                    return RuleActivity.Skill;
                case ActionKind.Defense:
                    return RuleActivity.Defense;
                case ActionKind.Hold:
                    return RuleActivity.Hold;
                case ActionKind.FastTravel:
                    return RuleActivity.FastTravel;
                case ActionKind.Clear:
                    return RuleActivity.Clear;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(actionKind), actionKind, "No rules activity is declared for this action kind.");
            }
        }
    }
}
