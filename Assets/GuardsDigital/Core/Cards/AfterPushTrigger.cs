using System;

namespace GuardsDigital.Core.Cards
{
    /// <summary>Which pushed object makes an after-push trigger fire.</summary>
    public enum AfterPushTriggerSubject
    {
        None = 0,

        /// <summary>
        /// Sabina's Big Sodding Gun prints "If you push an enemy hero"; the subject is a hero on the
        /// team opposing the source's controller. A minion pushed by the same push reaches no trigger,
        /// because the card names a hero and rulebook p.19 keeps the two apart.
        /// </summary>
        EnemyHero = 1
    }

    /// <summary>
    /// `after-push-trigger@1`. A trusted rule source's declaration that it runs immediately after one
    /// of rulebook p.18's Pushes has applied to an object it names. Milestone 8.19c1.
    ///
    /// <para><b>It fires on the displacement, not the attempt.</b> p.18's "if you push" is what
    /// happened rather than what was tried, so a push whose target had nowhere to go - stopped by an
    /// obstacle standing right behind it, or refused outright - moved nothing and this declaration is
    /// never reached. The producer is the push itself: a capability reports which objects it displaced,
    /// and only a reported displacement is a push that applied.</para>
    ///
    /// <para><b>Why it is a declaration rather than a checkpoint on its own.</b>
    /// <see cref="GuardsDigital.Core.Timing.TimingCheckpoint.ObjectPushApplied"/> is the moment; this
    /// is the source's statement that it is watching that moment and whose displacement it cares about.
    /// The two are separate for the reason milestone 8.19a separated
    /// <see cref="BeforeActionTrigger"/> from the lifecycle's own windows: a checkpoint is a position in
    /// the rules, and which objects at that position concern a card is the card's own words.</para>
    ///
    /// <para>Nothing here reads a hero, a display name, or a card title, so the rule stays a rule.</para>
    /// </summary>
    public readonly struct AfterPushTrigger : IEquatable<AfterPushTrigger>
    {
        public AfterPushTrigger(AfterPushTriggerSubject subject)
        {
            if (subject == AfterPushTriggerSubject.None ||
                !Enum.IsDefined(typeof(AfterPushTriggerSubject), subject))
            {
                throw new ArgumentException(
                    "An after-push trigger names the pushed object it fires for.", nameof(subject));
            }

            Subject = subject;
        }

        /// <summary>The named absence: a rule source that declares no after-push trigger.</summary>
        public static AfterPushTrigger None => default;

        public AfterPushTriggerSubject Subject { get; }

        public bool IsDeclared => Subject != AfterPushTriggerSubject.None;

        public bool Equals(AfterPushTrigger other) => Subject == other.Subject;

        public override bool Equals(object obj) => obj is AfterPushTrigger other && Equals(other);

        public override int GetHashCode() => (int)Subject;

        public override string ToString() =>
            IsDeclared ? $"after pushing {Subject}" : "no after-push trigger";
    }
}
