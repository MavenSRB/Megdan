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
    /// The members mirror <see cref="ActionKind"/> plus <see cref="ObjectPush"/>, which is an effect
    /// family rather than an action kind (p.18, Push). <see cref="RuleActivityQuery.ForAction"/> is the
    /// only bridge between the two enums; neither is cast into the other.
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
        /// Official straight-line forced displacement (p.18). Declared here because milestone 6.8b's
        /// immunity mode names it as its originating effect family and must serialize before its
        /// consumer exists; milestone 6.4 implements the effect itself.
        /// </summary>
        ObjectPush = 9
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
