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
        DirectMove = 12,

        /// <summary>
        /// Milestone 8.35z28. Rulebook p.10's <em>Repeat</em>, named as the thing a restriction
        /// can take away - Dodger the Warlock's Enfeeblement, "<b>This turn:</b> Enemy heroes in radius
        /// have -6 [Attack] Attack and <b>cannot repeat actions</b>".
        ///
        /// <para><b>It is exactly p.10's keyword and nothing wider.</b> The user's 2026-09-06 ruling on
        /// D-09 settles the scope: the clause forbids the "may repeat" offer a card's own text prints,
        /// and leaves replacement actions - Fast Travel, the Clear that substitutes for an Attack -
        /// available, because the page keeps those deliberately distinct from a repeat.</para>
        ///
        /// <para><b>It is a thirteenth activity even though a repeat is not an action.</b> p.10 makes a
        /// repeat a control-flow modifier on an action already performed, so it maps from no
        /// <see cref="ActionKind"/> and <see cref="RuleActivityQuery.ForAction"/> deliberately returns
        /// it for none - the same standing this member shares with <see cref="ObjectPush"/>,
        /// <see cref="Swap"/>, <see cref="Place"/> and <see cref="DirectMove"/>, which are also
        /// operations rather than actions. Naming it here is what lets one restriction family answer
        /// the question, so the repeat is never <em>offered</em> rather than refused after the fact.</para>
        /// </summary>
        Repeat = 13,

        /// <summary>
        /// Milestone 9.30f. An <see cref="Attack"/> performed from a card that is p.9's BASIC
        /// classification - "All actions on a basic card are basic actions" - Gydion's Shield, "<b>This
        /// round:</b> You are immune to basic attacks."
        ///
        /// <para><b>A class of <see cref="Attack"/>, not an activity beside it.</b> An Attack
        /// targeting question that knows the attacking card asks this member (or
        /// <see cref="NonBasicAttack"/>) through <see cref="RuleActivityQuery.ForAttack"/>, and a
        /// declaration naming <see cref="Attack"/> or <see cref="All"/> still covers it
        /// (<see cref="RuleModification.Covers"/>), so every Attack immunity declared before this
        /// milestone reaches exactly what it reached. A declaration naming this member covers only a
        /// question that knows the attack is basic: a caller asking <see cref="Attack"/> without the
        /// card has not said which class it is asking about.</para>
        ///
        /// <para>The classification is the attacking card DEFINITION's own (Snorri's Block categories
        /// read it the same way), so a cast spell's Attack is classified by the spell - Magic Missile
        /// and Shocking Grasp are basic - and not by the card that cast it (GY-Q3).</para>
        /// </summary>
        BasicAttack = 14,

        /// <summary>
        /// Milestone 9.30f. An <see cref="Attack"/> performed from a card that is NOT p.9's basic -
        /// a Tier I-III card or an Ultimate (Snorri's Oath of Perseverance reads non-basic the same
        /// way) - Gydion's Invulnerability, "<b>This Round:</b> You are immune to non-basic attacks."
        /// The complement of <see cref="BasicAttack"/>; see it for the coverage rule.
        /// </summary>
        NonBasicAttack = 15
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

        /// <summary>
        /// Milestone 9.30f. The activity an Attack targeting question asks when it knows the attacking
        /// card's p.9 classification: <see cref="RuleActivity.BasicAttack"/> for a basic card,
        /// <see cref="RuleActivity.NonBasicAttack"/> for a Tier or an Ultimate, and the unclassified
        /// <see cref="RuleActivity.Attack"/> for a card that states none - which only an Attack
        /// declaration (or an unqualified one) covers.
        /// </summary>
        public static RuleActivity ForAttack(CardClassification classification)
        {
            switch (classification)
            {
                case CardClassification.Basic:
                    return RuleActivity.BasicAttack;
                case CardClassification.NonBasic:
                case CardClassification.Ultimate:
                    return RuleActivity.NonBasicAttack;
                default:
                    return RuleActivity.Attack;
            }
        }
    }
}
