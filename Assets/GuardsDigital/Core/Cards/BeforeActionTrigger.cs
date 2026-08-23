using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>How often a before-action trigger may fire.</summary>
    public enum BeforeActionTriggerFrequency
    {
        None = 0,

        /// <summary>
        /// Arien's Living Tsunami prints "Once per turn". The turn records which controllers have
        /// consumed a before-action trigger, so a hero who takes a repeated action does not get the
        /// ability a second time inside the same turn.
        /// </summary>
        OncePerTurn = 1
    }

    /// <summary>
    /// `before-action-trigger@1`. A trusted rule source's declaration that it runs before its
    /// controller performs an action of a named kind. Milestone 8.19a.
    ///
    /// <para><b>It is a declaration, not a checkpoint.</b> The lifecycle's checkpoints are moments
    /// <em>inside</em> an action - it opens at <c>ActionStarted</c> and closes at <c>ActionEnded</c> -
    /// and this fires before the first of them, while the action is still an offer the player has
    /// selected and the resolver has validated. That is exactly what makes it worth its own
    /// declaration: what it interrupts is a start, and what it must not disturb is the offer that start
    /// was validated against.</para>
    ///
    /// <para><b>Why the kind is declared rather than the card named.</b> p.16 gives an Ultimate a
    /// permanent ability and says nothing about which cards it watches; Living Tsunami's own words name
    /// an <em>action</em> - "before performing an Attack action" - so the declaration names the action
    /// kind and the frequency, and no rule anywhere reads a hero, a display name, or a card title.</para>
    /// </summary>
    public readonly struct BeforeActionTrigger : IEquatable<BeforeActionTrigger>
    {
        public BeforeActionTrigger(ActionKind action, BeforeActionTriggerFrequency frequency)
        {
            if (action != ActionKind.Attack && action != ActionKind.Movement &&
                action != ActionKind.Skill)
            {
                throw new ArgumentException(
                    "A before-action trigger names one of the three action kinds a hero performs from " +
                    "a card; a Defense is a reaction and has no start of its own to precede.",
                    nameof(action));
            }

            if (frequency == BeforeActionTriggerFrequency.None ||
                !Enum.IsDefined(typeof(BeforeActionTriggerFrequency), frequency))
            {
                throw new ArgumentException(
                    "A before-action trigger states how often it may fire.", nameof(frequency));
            }

            Action = action;
            Frequency = frequency;
        }

        /// <summary>The named absence: a rule source that declares no before-action trigger.</summary>
        public static BeforeActionTrigger None => default;

        public ActionKind Action { get; }

        public BeforeActionTriggerFrequency Frequency { get; }

        public bool IsDeclared => Frequency != BeforeActionTriggerFrequency.None;

        public bool Equals(BeforeActionTrigger other) =>
            Action == other.Action && Frequency == other.Frequency;

        public override bool Equals(object obj) => obj is BeforeActionTrigger other && Equals(other);

        public override int GetHashCode()
        {
            unchecked { return ((int)Action * 397) ^ (int)Frequency; }
        }

        public override string ToString() =>
            IsDeclared ? $"before {Action} ({Frequency})" : "no before-action trigger";
    }
}
