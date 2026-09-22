using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// One completed action that was the performance of a card's PRIMARY action, recorded at the
    /// checkpoint that completes it. Milestone 8.39f1.
    ///
    /// <para><b>Why this is a fact beside <see cref="PerformedBasicAction"/> rather than a widening of
    /// it.</b> That one records rulebook p.9's supertype, and p.9 makes the supertype a fact about the
    /// CARD: only the Gold and Silver cards are basic, so <c>BasicActionQuery.ForCompletedAction</c>
    /// returns the named absence for every other card in the box. Five Ultimates, p.10's repeat
    /// machinery and the authoritative snapshot all read that type today, and every one of them is
    /// written on the understanding that a defined value means a basic card. Cutter's Legend of the
    /// Skies asks a different question - "after you perform a primary action" - which every card in her
    /// deck can answer, and her Tier IV is unlocked when most of what she plays is not basic. Widening
    /// the older fact to reach her would have changed what Wasp's, Tigerclaw's, Misa's, Tali's and
    /// Swift's Ultimates see, and what p.10 repeats.</para>
    ///
    /// <para><b>It is produced at the same single site and on the same terms.</b>
    /// <c>TurnResolutionAuthority.AddCompletedAction</c> is the one place an action's completion is
    /// recorded, and it already knows the <see cref="ActionInvocationContext"/> - so this is read once,
    /// where the answer is already in hand, and never re-derived by a second reader. That is the
    /// discipline <see cref="PerformedBasicAction"/> records for itself.</para>
    ///
    /// <para><b>It is a fact, not state.</b> Nothing persists it and no snapshot field holds it: the
    /// accepted command that ends the action produces it, so a replay produces it again in the same
    /// position and an Undo removes it with everything else the command did.</para>
    /// </summary>
    public readonly struct PerformedPrimaryAction : IEquatable<PerformedPrimaryAction>
    {
        private PerformedPrimaryAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind)
        {
            ActorId = actorId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            Kind = kind;
        }

        /// <summary>
        /// The action that just completed was not the performance of a card's primary action, so
        /// nothing is recorded. A value rather than a null, so a reader asks <see cref="IsDefined"/>
        /// instead of discovering the absence by dereferencing it.
        /// </summary>
        public static PerformedPrimaryAction None => default;

        public static PerformedPrimaryAction Of(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind)
        {
            if (!actorId.IsDefined || !sourceCardInstanceId.IsDefined ||
                !actionDefinitionId.IsDefined || kind == ActionKind.None)
            {
                return None;
            }

            return new PerformedPrimaryAction(
                actorId, sourceCardInstanceId, actionDefinitionId, kind);
        }

        public bool IsDefined => ActorId.IsDefined;

        public HeroId ActorId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        /// <summary>
        /// Which action was performed. No printed sentence narrows this clause by kind today - Legend
        /// of the Skies says "a primary action" and means all of them - and it is carried because the
        /// fact is about an action and a reader that could not say which one would be reporting less
        /// than it knows.
        /// </summary>
        public ActionKind Kind { get; }

        /// <summary>
        /// Whether rulebook p.10's repeat has anything to make a second instance of here - the
        /// <see cref="PerformedBasicAction.CanBeRepeated"/> question asked of this fact. Milestone
        /// 8.75a, for Ignatia the Mad's Chaos Incarnate: "you may perform that action again".
        ///
        /// <para><b>p.10's "You cannot repeat a repeated action!" is already enforced by
        /// construction, which is why it is not repeated here.</b>
        /// <see cref="PrimaryActionQuery.IsPerformingAPrimaryAction"/> answers false for
        /// <see cref="ActionInvocationContext.Repeated"/> - and says in as many words why: a repeat
        /// REPLACES the original context, so a completed repeat cannot say whether what it repeated
        /// was the card's printed primary. So a repeated instance produces no primary fact at all,
        /// and there is no <c>WasRepeatedInstance</c> on this type to ask about.</para>
        ///
        /// <para><see cref="PerformedBasicAction.KindCanBeRepeated"/> IS asked, unlike on
        /// <see cref="PerformedAttackAction"/>, because this fact is recorded for every kind a card's
        /// primary band can print and for every kind a grant can produce. p.11's Fast Travel and
        /// p.13's Clear reach neither: both are <see cref="ActionInvocationContext.Replacement"/>,
        /// which the query above already refuses.</para>
        /// </summary>
        public bool CanBeRepeated =>
            IsDefined && PerformedBasicAction.KindCanBeRepeated(Kind);

        public bool Equals(PerformedPrimaryAction other) =>
            ActorId.Equals(other.ActorId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            ActionDefinitionId.Equals(other.ActionDefinitionId) &&
            Kind == other.Kind;

        public override bool Equals(object obj) =>
            obj is PerformedPrimaryAction other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ActorId.GetHashCode();
                hash = (hash * 397) ^ SourceCardInstanceId.GetHashCode();
                hash = (hash * 397) ^ ActionDefinitionId.GetHashCode();
                return (hash * 397) ^ (int)Kind;
            }
        }

        public override string ToString() =>
            IsDefined ? $"{ActorId} performed primary {Kind}" : "no primary action";
    }

    /// <summary>
    /// Which invocations count as a hero performing a card's primary action. Milestone 8.39f1.
    ///
    /// <para>A named rule rather than an expression at its one call site, so every context is decided
    /// in one place and a context added later has to be classified here rather than falling silently
    /// into "not a primary action" - the shape <c>BasicActionQuery.IsCardResolution</c> records.</para>
    /// </summary>
    public static class PrimaryActionQuery
    {
        /// <summary>
        /// <para><b>The printed primary is the clause's own subject</b>, so
        /// <see cref="ActionInvocationContext.PrintedPrimary"/> answers definitionally. Note that a
        /// p.14 Block does not reach it even though the card prints the Defense in its primary slot:
        /// <c>ActionOfferQuery.ToInvocationContext</c> classifies every printed-primary Defense as a
        /// <see cref="ActionInvocationContext.Reaction"/>, because it is spent on somebody else's turn
        /// in answer to their Attack. That is a rule this file reads rather than restates.</para>
        ///
        /// <para><b>A granted primary IS a primary action being performed</b>, which is what
        /// <see cref="ActionInvocationContext.TextGranted"/> carries - the borrow's own context,
        /// <c>BorrowedPrimaryAction.InvocationContext</c>. "Perform the primary action of a card" names
        /// the thing this clause watches, so a reading that excluded it would have the engine disagree
        /// with the sentence that produced it. It does not make Legend of the Skies loop, because the
        /// clause fires the FIRST time each turn and the grant can only follow a firing.</para>
        ///
        /// <para><b>The two secondary contexts and the reaction are not primary actions</b>, which is
        /// the printed band they come from.</para>
        ///
        /// <para><b>A replacement is not the printed primary</b>, and this build has already settled
        /// that in as many words: <c>BeforeActionTriggerQuery</c>'s own remark says "p.7's Fast Travel
        /// and p.13's Clear are replacements rather than the printed primary the sentence names". The
        /// same sentence shape gets the same answer at both ends of the action.</para>
        ///
        /// <para><b>A repeat does not arm it, because the fact needed to decide is not recorded.</b>
        /// <see cref="ActionInvocationContext.Repeated"/> REPLACES the original context rather than
        /// qualifying it, so a completed repeat cannot say whether what it repeated was the card's
        /// printed primary or its secondary. Answering "yes" would be inventing that; answering "no" is
        /// what this build can actually see. It is also moot on every board a match reaches, because a
        /// repeat necessarily follows the action it repeats, and that action already armed the
        /// clause.</para>
        /// </summary>
        public static bool IsPerformingAPrimaryAction(ActionInvocationContext invocationContext)
        {
            switch (invocationContext)
            {
                case ActionInvocationContext.PrintedPrimary:
                case ActionInvocationContext.TextGranted:
                    return true;
                case ActionInvocationContext.PrintedSecondary:
                case ActionInvocationContext.GeneralRuleSecondary:
                case ActionInvocationContext.Replacement:
                case ActionInvocationContext.Reaction:
                case ActionInvocationContext.Repeated:
                case ActionInvocationContext.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(invocationContext), invocationContext,
                        "Every invocation context states whether it performs a card's primary action; " +
                        "a new one is classified here rather than defaulting.");
            }
        }

        /// <summary>
        /// The fact for one completed action, or <see cref="PerformedPrimaryAction.None"/> where the
        /// invocation was not the performance of a primary action.
        ///
        /// <para>Unlike <c>BasicActionQuery.ForCompletedAction</c> this consults no card definition: p.9
        /// decides the basic supertype from the CARD, and the primary/secondary split is decided by
        /// which band of the card the action was invoked from, which the invocation context already
        /// states.</para>
        /// </summary>
        public static PerformedPrimaryAction ForCompletedAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind,
            ActionInvocationContext invocationContext) =>
            IsPerformingAPrimaryAction(invocationContext)
                ? PerformedPrimaryAction.Of(
                    actorId, sourceCardInstanceId, actionDefinitionId, kind)
                : PerformedPrimaryAction.None;
    }
}
