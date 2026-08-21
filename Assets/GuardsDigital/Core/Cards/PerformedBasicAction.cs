using System;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards
{
    /// <summary>
    /// One completed action that rulebook p.9 counts as basic, recorded at the checkpoint that
    /// completes it.
    ///
    /// <para><b>Milestone 8.9, and why the fact is the card's rather than the action's.</b> p.9 states
    /// the supertype and its consequence in one breath: only the Gold and Silver cards are basic, and
    /// every action performed on a basic card is a basic action. So nothing about the action decides
    /// this - not its kind, not its origin, not whether the card printed an icon for it - and a Hold or
    /// a Fast Travel spent from a basic card is as basic as its printed primary. The classification is
    /// milestone 7.1's <see cref="CardClassification"/>, read from the source card, which is why this
    /// carries the card as well as the kind.</para>
    ///
    /// <para><b>Why the kind travels with it.</b> Wasp's High Voltage triggers "each time after you
    /// perform a basic <em>skill</em>" (milestone 8.13), so the reader needs both halves of the
    /// sentence and neither can be re-derived later: the card can leave the turn area, and a
    /// replacement action - p.7's Fast Travel, p.13's Clear - has no definition on the card to look its
    /// kind up on. Each resolver names its own kind at the one point it finishes an action, which it
    /// knows statically.</para>
    ///
    /// <para><b>It is a fact, not state.</b> Nothing persists it and no snapshot field holds it: it is
    /// produced by the same accepted command that ends the action, so a replay of that command produces
    /// it again in the same position, and an Undo that removes the command removes it with everything
    /// else the command did.</para>
    /// </summary>
    public readonly struct PerformedBasicAction : IEquatable<PerformedBasicAction>
    {
        private PerformedBasicAction(
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
        /// The action that just completed was not performed on a basic card, so p.9's supertype records
        /// nothing. A value rather than a null, so a reader asks <see cref="IsDefined"/> instead of
        /// discovering the absence by dereferencing it.
        /// </summary>
        public static PerformedBasicAction None => default;

        public static PerformedBasicAction Of(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind)
        {
            if (!actorId.IsDefined || !sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined)
            {
                throw new ArgumentException(
                    "A performed basic action names the hero who performed it, the card it was " +
                    "performed on, and the action definition it ran.",
                    nameof(actorId));
            }

            if (kind == ActionKind.None || !Enum.IsDefined(typeof(ActionKind), kind))
            {
                throw new ArgumentException(
                    "A performed basic action names its kind; p.9's supertype makes every action on a " +
                    "basic card basic, and which one it was is the half a trigger reads.",
                    nameof(kind));
            }

            return new PerformedBasicAction(actorId, sourceCardInstanceId, actionDefinitionId, kind);
        }

        public HeroId ActorId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        /// <summary>Which action was performed - the half "a basic skill" narrows.</summary>
        public ActionKind Kind { get; }

        public bool IsDefined => Kind != ActionKind.None;

        public bool Equals(PerformedBasicAction other) =>
            ActorId.Equals(other.ActorId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            ActionDefinitionId.Equals(other.ActionDefinitionId) &&
            Kind == other.Kind;

        public override bool Equals(object obj) => obj is PerformedBasicAction other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ActorId.GetHashCode();
                hashCode = (hashCode * 397) ^ SourceCardInstanceId.GetHashCode();
                hashCode = (hashCode * 397) ^ ActionDefinitionId.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Kind;
                return hashCode;
            }
        }

        public override string ToString() =>
            IsDefined
                ? $"basic {Kind} by {ActorId.Value} on {SourceCardInstanceId.Value}"
                : "no basic action";
    }

    /// <summary>
    /// Rulebook p.9's supertype, asked of one completed action. The only place the question is decided,
    /// so a second reader cannot answer it from the action instead of from the card.
    /// </summary>
    public static class BasicActionQuery
    {
        /// <summary>
        /// The fact this completion records, or <see cref="PerformedBasicAction.None"/> when the card
        /// the action was performed on is not one of p.15's Gold-and-Silver pair.
        ///
        /// An action whose source card the match does not hold records nothing rather than faulting:
        /// this is a fact about a card, and a completion that names none is a completion this page has
        /// nothing to say about.
        /// </summary>
        public static PerformedBasicAction ForCompletedAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind,
            System.Collections.Generic.IEnumerable<CardDefinition> definitions,
            System.Collections.Generic.IEnumerable<CardInstance> instances)
        {
            if (!actorId.IsDefined || !sourceCardInstanceId.IsDefined || !actionDefinitionId.IsDefined ||
                kind == ActionKind.None || definitions == null || instances == null)
            {
                return PerformedBasicAction.None;
            }

            CardInstance card = null;
            foreach (var candidate in instances)
            {
                if (candidate.Id.Equals(sourceCardInstanceId))
                {
                    card = candidate;
                    break;
                }
            }

            if (card == null)
            {
                return PerformedBasicAction.None;
            }

            foreach (var definition in definitions)
            {
                if (definition.Reference.Equals(card.DefinitionReference))
                {
                    return definition.Progression.Classification == CardClassification.Basic
                        ? PerformedBasicAction.Of(
                            actorId, sourceCardInstanceId, actionDefinitionId, kind)
                        : PerformedBasicAction.None;
                }
            }

            return PerformedBasicAction.None;
        }
    }
}
