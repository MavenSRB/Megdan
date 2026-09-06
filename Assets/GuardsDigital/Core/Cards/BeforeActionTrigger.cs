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
        OncePerTurn = 1,

        /// <summary>
        /// Milestone 8.35z30. Silverarrow's Wild Hunt prints "<b>Each time</b> before you
        /// perform an action you may move 2 spaces in a straight line" - no limit, and no action kind
        /// either.
        ///
        /// <para><b>The member says both, because the card says both in one sentence.</b> "Each time"
        /// is the frequency and "an action" is the scope, and Wild Hunt is the only shape in which
        /// either appears: a declaration pairs this frequency with <see cref="ActionKind.None"/> - the
        /// absence of a named kind, read as p.11's three - and pairs
        /// <see cref="OncePerTurn"/> with a named one, which is Arien's Living Tsunami. The two
        /// pairings are the only legal ones, so there is exactly one way to write each printed
        /// sentence and no combination whose meaning this file would have to invent.</para>
        ///
        /// <para>Nothing consumes a trigger declared here, which is what "each time" means: a turn
        /// records the sources it has spent so a once-per-turn ability cannot fire twice, and an
        /// ability with no limit is never written into that record.</para>
        /// </summary>
        EveryAction = 2
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
            if (frequency == BeforeActionTriggerFrequency.None ||
                !Enum.IsDefined(typeof(BeforeActionTriggerFrequency), frequency))
            {
                throw new ArgumentException(
                    "A before-action trigger states how often it may fire.", nameof(frequency));
            }

            // Milestone 8.35z30. Every-action is the one declaration that names no kind, and
            // it is the only one that may: see BeforeActionTriggerFrequency.EveryAction for why the
            // pairing is exact in both directions.
            var everyAction = frequency == BeforeActionTriggerFrequency.EveryAction;
            if (everyAction && action != ActionKind.None)
            {
                throw new ArgumentException(
                    "Wild Hunt's \"each time before you perform an action\" names no action kind, so " +
                    "a trigger declaring that frequency names none either.",
                    nameof(action));
            }

            if (!everyAction && action != ActionKind.Attack && action != ActionKind.Movement &&
                action != ActionKind.Skill)
            {
                throw new ArgumentException(
                    "A before-action trigger names one of the three action kinds a hero performs from " +
                    "a card; a Defense is a reaction and has no start of its own to precede.",
                    nameof(action));
            }

            Action = action;
            Frequency = frequency;
        }

        /// <summary>The named absence: a rule source that declares no before-action trigger.</summary>
        public static BeforeActionTrigger None => default;

        public ActionKind Action { get; }

        public BeforeActionTriggerFrequency Frequency { get; }

        public bool IsDeclared => Frequency != BeforeActionTriggerFrequency.None;

        /// <summary>
        /// Milestone 8.35z30. Whether this declaration fires before an action of the named
        /// kind - which, for Wild Hunt's kindless sentence, is every kind a hero performs from a card.
        /// </summary>
        public bool FiresBefore(ActionKind actionKind) =>
            IsDeclared &&
            (Frequency == BeforeActionTriggerFrequency.EveryAction || Action == actionKind);

        /// <summary>
        /// Whether a turn records this source once it has fired. "Each time" records nothing, which is
        /// the whole of the difference between Wild Hunt and Living Tsunami.
        ///
        /// <para><b>It is a method and not a property, deliberately.</b> This struct is reachable from
        /// every hero's Ultimate through <see cref="UltimateRuleDefinition.BeforeAction"/>, and the
        /// content-identity oracle walks every public instance property of everything it reaches - so a
        /// property here appends a token to all six shipped heroes' canonical strings and moves all six
        /// pinned digests for no change in behaviour whatsoever. Measured: adding it as a property
        /// moved arien, brogan, sabina, tigerclaw, wasp and xargatha at once. A method is not walked,
        /// and the answer is the same.</para>
        /// </summary>
        public bool IsConsumedWhenItFires() =>
            Frequency == BeforeActionTriggerFrequency.OncePerTurn;

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
