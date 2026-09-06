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
        AfterTheAttack = 3,

        /// <summary>
        /// Milestone 8.19a. Text the actor applies either side of the primary action it is printed on,
        /// choosing which when the action starts.
        ///
        /// <para>p.14's numbered order is the Attack's alone, and the three moments above are its.
        /// Xargatha's Charm prints a Movement card whose text moves an enemy minion "before or after"
        /// that Movement, so the moment is neither of p.14's and is not fixed by the page at all - it
        /// is a decision the acting player makes, once, at the start of the action. That is why this
        /// timing names the action rather than a numbered step: what it declares is that the text is
        /// applied <em>around</em> the action, with the side chosen through the
        /// `around-primary-action@1` capability.</para>
        ///
        /// <para>It is declared on a Movement action alone in this build, which is the one printed
        /// shape the roster inventories contain. A later slice transcribing the same wording on
        /// another action kind widens <see cref="CardTextTimingQuery.OwningActionKind"/> rather than
        /// this reading being guessed at now.</para>
        /// </summary>
        AroundTheAction = 4,

        /// <summary>
        /// Milestone 8.20. Rulebook p.11's own default for a Movement card's primary action: "When you
        /// perform the Primary action on a Movement card, apply the card text in addition to moving up
        /// to the number of spaces on the icon ... <b>Unless it says otherwise, the card text comes
        /// into effect before movement.</b>"
        ///
        /// <para>This is the "unless" that <see cref="AroundTheAction"/> is the exception to.
        /// Milestone 8.19a read Xargatha's Charm, which does say otherwise - "before or after" - and
        /// built the placement question for it; Arien's Slippery Ground and Deluge say nothing, which
        /// the page answers for them. So the two are one sentence read twice, and the reason they are
        /// separate members is that one is decided by the content and the other by the player.</para>
        ///
        /// <para>The page's second sentence is why this is a moment rather than a step inside the
        /// Movement: "The card text is applied even if you choose to move zero spaces." A text that
        /// ran as part of the walk would not run for a hero who never took one.</para>
        /// </summary>
        BeforeTheMovement = 5,

        /// <summary>
        /// Milestone 8.35z31. The other half of p.11's sentence - the "otherwise" a card is
        /// allowed to say: "<b>Unless it says otherwise</b>, the card text comes into effect before
        /// movement."
        ///
        /// <para>Three of Ursafar's five Blue faces say otherwise in so many words. Prowling Brute
        /// prints "If enraged, <b>after movement</b>, you may swap with a unit or a token adjacent to
        /// you"; Rampaging Beast and Unstoppable Force print the same clause and add a further
        /// displacement to it. The swap is about where the walk left him, so a text that ran first
        /// would be asking about a space he has not reached.</para>
        ///
        /// <para>It is a sixth member rather than a reuse of <see cref="AroundTheAction"/> for the
        /// reason <see cref="BeforeTheMovement"/> is a fifth: that one is a question the actor answers
        /// and this is a side the card states. Composing these three as an ordering question would
        /// offer a choice the face does not print, and - since one of the two answers would be wrong
        /// about the board - a choice the player could get wrong on the card's behalf.</para>
        ///
        /// <para>Everything downstream of the declaration is shared rather than duplicated: the held
        /// offer, the parked continuation and the entry after the Movement completes are milestone
        /// 8.19a's <c>PreActionPlacement.AfterTheAction</c> path exactly, which is why this member
        /// moves no snapshot field - the continuation it parks is one the codec has written since
        /// 8.19a.</para>
        /// </summary>
        AfterTheMovement = 6
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
                        nameof(timing), timing,
                        "Only rulebook p.14's three numbered moments correspond to an Attack " +
                        "checkpoint; text applied around a primary action is placed by the actor, " +
                        "and text applied before a Movement runs at that action's own start.");
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
                case CardTextTiming.AroundTheAction:
                case CardTextTiming.BeforeTheMovement:
                case CardTextTiming.AfterTheMovement:
                    return ActionKind.Movement;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(timing), timing, "No action kind owns this card-text timing.");
            }
        }
    }
}
