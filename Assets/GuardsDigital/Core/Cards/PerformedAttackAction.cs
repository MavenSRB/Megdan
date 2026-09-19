using System;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// One completed Attack action, whatever card it came off. Milestone 8.61h.
    ///
    /// <para><b>Why this is a fact beside <see cref="PerformedBasicAction"/> rather than a widening of
    /// it, and the reason is the same one milestone 8.39f1 recorded for
    /// <see cref="PerformedPrimaryAction"/>.</b> That fact records rulebook p.9's SUPERTYPE, which the
    /// page makes a property of the CARD: only the Gold and Silver cards are basic, so
    /// <c>BasicActionQuery.ForCompletedAction</c> returns the named absence for every other card in
    /// the box. Min the Dragonmonk's Flurry of Blows says "Each time after you perform an ATTACK
    /// action", and most of the Attacks she performs come off her five Red Stances, which are Tier I
    /// to III and not basic at all. Widening the older fact to reach them would have changed what
    /// Wasp's, Tigerclaw's, Misa's, Tali's and Swift's Ultimates see, and what p.10's repeat machinery
    /// finds to repeat.</para>
    ///
    /// <para><b>It is narrower than <see cref="PerformedPrimaryAction"/> for the opposite reason.</b>
    /// Cutter's Legend of the Skies asks about the BAND the action came from - the card's primary -
    /// and never about its kind; this asks about the kind and never about the band. A secondary Attack
    /// is an attack action, and a card whose primary is a Skill performs no attack action by
    /// performing it. Neither question is the other's answer, which is why the two facts are produced
    /// side by side from the same completion rather than one being derived from the other.</para>
    ///
    /// <para><b>p.13's replacements are excluded structurally.</b> Fast Travel and Clear are their own
    /// <see cref="ActionKind"/> members, so a Clear taken instead of the Attack never reaches this
    /// fact - there is no condition to forget, which is the shape
    /// <see cref="PerformedMovementAction"/> already takes for its own kind. A Defense is
    /// <see cref="ActionKind.Defense"/> and is likewise not an attack action however much p.14 pairs
    /// them.</para>
    ///
    /// <para><b>It is a fact, not state.</b> Nothing persists it and no snapshot field holds it: the
    /// accepted command that ends the action produces it, so a replay produces it again in the same
    /// position and an Undo that removes the command removes it with everything else the command
    /// did.</para>
    /// </summary>
    public readonly struct PerformedAttackAction : IEquatable<PerformedAttackAction>
    {
        private PerformedAttackAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            bool wasRepeatedInstance)
        {
            ActorId = actorId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            WasRepeatedInstance = wasRepeatedInstance;
        }

        /// <summary>
        /// The action that just completed was not an Attack, so nothing is recorded. A value rather
        /// than a null, so a reader asks <see cref="IsDefined"/> instead of discovering the absence by
        /// dereferencing it.
        /// </summary>
        public static PerformedAttackAction None => default;

        /// <summary>
        /// The fact one completion records, or <see cref="None"/> when the action that ended was not
        /// an Attack. Shaped as a filter rather than as a constructor with a precondition, exactly as
        /// <see cref="PerformedMovementAction.ForCompletedAction"/> is, so the one site that records
        /// completions hands over what it knows and this decides whether there is anything to say.
        /// </summary>
        public static PerformedAttackAction ForCompletedAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind,
            bool wasRepeatedInstance = false) =>
            kind == ActionKind.Attack && actorId.IsDefined &&
                sourceCardInstanceId.IsDefined && actionDefinitionId.IsDefined
                ? new PerformedAttackAction(
                    actorId, sourceCardInstanceId, actionDefinitionId, wasRepeatedInstance)
                : None;

        public bool IsDefined => ActorId.IsDefined;

        public HeroId ActorId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        /// <summary>
        /// Whether the instance that ended was itself produced by a repeat.
        ///
        /// <para>Rulebook p.10: "You cannot repeat a repeated action!" It is carried BESIDE the
        /// trigger rather than suppressing it, which is the reading
        /// <see cref="PerformedBasicAction.WasRepeatedInstance"/> already settled: a repeated Attack is
        /// still an Attack being performed, so Flurry of Blows still fires on it - and finds nothing
        /// it may repeat, because <see cref="CanBeRepeated"/> is false. What the page forbids is the
        /// second repeat, not the second trigger.</para>
        /// </summary>
        public bool WasRepeatedInstance { get; }

        /// <summary>
        /// Whether rulebook p.10's repeat has anything to make a second instance of here - the
        /// <see cref="PerformedBasicAction.CanBeRepeated"/> question asked of this fact.
        ///
        /// <para>Two of that property's three sentences apply and the third cannot: there is no action
        /// at all when the completion recorded none, and p.10's "You cannot repeat a repeated action!"
        /// removes the second instance. <see cref="PerformedBasicAction.KindCanBeRepeated"/> is not
        /// asked because this fact exists only for <see cref="ActionKind.Attack"/>, which that method
        /// already admits; stating it again would be a condition that can never be false.</para>
        /// </summary>
        public bool CanBeRepeated => IsDefined && !WasRepeatedInstance;

        public bool Equals(PerformedAttackAction other) =>
            ActorId.Equals(other.ActorId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            ActionDefinitionId.Equals(other.ActionDefinitionId) &&
            WasRepeatedInstance == other.WasRepeatedInstance;

        public override bool Equals(object obj) => obj is PerformedAttackAction other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = ActorId.GetHashCode();
                hash = (hash * 397) ^ SourceCardInstanceId.GetHashCode();
                hash = (hash * 397) ^ ActionDefinitionId.GetHashCode();
                return (hash * 397) ^ (WasRepeatedInstance ? 1 : 0);
            }
        }

        public override string ToString() =>
            IsDefined
                ? $"attack by {ActorId.Value} on {SourceCardInstanceId.Value}"
                : "no attack action";
    }
}
