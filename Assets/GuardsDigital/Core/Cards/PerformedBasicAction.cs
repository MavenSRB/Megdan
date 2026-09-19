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
            ActionKind kind,
            bool wasRepeatedInstance,
            bool placedSelf,
            bool defeatedMinionWithBasicAttack,
            bool wasCardResolution)
        {
            ActorId = actorId;
            SourceCardInstanceId = sourceCardInstanceId;
            ActionDefinitionId = actionDefinitionId;
            Kind = kind;
            WasRepeatedInstance = wasRepeatedInstance;
            PlacedSelf = placedSelf;
            DefeatedMinionWithBasicAttack = defeatedMinionWithBasicAttack;
            WasCardResolution = wasCardResolution;
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
            ActionKind kind,
            bool wasRepeatedInstance = false,
            bool placedSelf = false,
            bool defeatedMinionWithBasicAttack = false,
            bool wasCardResolution = false)
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

            return new PerformedBasicAction(
                actorId, sourceCardInstanceId, actionDefinitionId, kind, wasRepeatedInstance,
                placedSelf, defeatedMinionWithBasicAttack, wasCardResolution);
        }

        public HeroId ActorId { get; }

        public CardInstanceId SourceCardInstanceId { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        /// <summary>Which action was performed - the half "a basic skill" narrows.</summary>
        public ActionKind Kind { get; }

        /// <summary>
        /// Whether the instance that ended was itself produced by a repeat. Milestone 8.23.
        ///
        /// <para>Rulebook p.10: "You cannot repeat a repeated action!" It is carried on the fact
        /// rather than asked of the resolution because by the time this is recorded the resolution has
        /// finished and been cleared, and it is carried <em>beside</em> the trigger rather than
        /// suppressing it: a repeated basic Skill is still a basic Skill being performed, so a card
        /// that fires on one - Wasp's High Voltage - still fires. What the page forbids is the second
        /// repeat, not the second trigger.</para>
        /// </summary>
        public bool WasRepeatedInstance { get; }

        /// <summary>
        /// Milestone 8.35z29. Whether this action's own card text placed the hero performing it -
        /// Misa's Watch How I Soar, Swoop In and Leaf on the Wind, and the whole of what her Power
        /// Overwhelming's second paragraph watches: "Each time after you place yourself, an enemy hero
        /// adjacent to you discards a card, if able."
        ///
        /// <para>It travels beside the kind rather than being re-derived, for the reason
        /// <see cref="Kind"/> travels: by the time a trigger reads this fact the resolution whose
        /// outcomes prove it has been cleared, and the card may have left the turn area.</para>
        ///
        /// <para><b>The two bounds this build's shape puts on it, stated rather than left to be
        /// discovered.</b> It is recorded once per completed action rather than once per placement -
        /// no verified face places its own actor twice in one action, so "each time" and "at the end of
        /// an action in which it happened" name the same moments today. And it reaches a trigger only
        /// through p.9's basic-action supertype, which is where every self-placing face Misa prints
        /// lives: all three are Basic Skills. A face that self-placed from a non-basic card, or twice,
        /// would need the immediate interrupt <c>after-push-trigger@1</c> already has.</para>
        /// </summary>
        public bool PlacedSelf { get; }

        /// <summary>
        /// Milestone 8.37. Whether this basic Attack's own resolution actually REMOVED a
        /// minion - Tali's Reign of Winter, "Each time after you defeat a minion with your basic attack,
        /// an enemy hero in radius discards a card of the color of your choice, if able."
        ///
        /// <para>It travels beside <see cref="PlacedSelf"/> for the identical reason that one does: by
        /// the time a trigger reads a completed action's supertype, the resolution whose own steps prove
        /// the fact has already finished and been cleared, so the fact must be carried rather than
        /// re-derived. It answers about REMOVAL specifically, not about a Defeat merely being staged or
        /// attempted - a minion whose removal a totem's save or p.18's immunity stopped was not
        /// "defeated with" the Attack in the sense this sentence reads, because the piece is still on
        /// the board.</para>
        /// </summary>
        public bool DefeatedMinionWithBasicAttack { get; }

        /// <summary>
        /// Whether this completion was the CARD's own resolution - rulebook p.10's "resolve your card"
        /// - rather than a second performance off a card already resolved. Milestone 8.38c2.
        ///
        /// <para><b>Swift's Bullet Time is what needs the distinction, and it is not the one
        /// <see cref="WasRepeatedInstance"/> draws.</b> Her Ultimate reads "After you resolve a basic
        /// CARD you may perform the primary action on that card", where Tigerclaw's Cloak and Daggers
        /// reads "After you perform a basic ACTION you may repeat it once". A card is resolved once per
        /// turn; actions are performed as often as text grants them. So a repeat, a borrowed primary
        /// and a granted one are all basic actions and none of them is a card being resolved.</para>
        ///
        /// <para><b>It is true for a replacement action too</b>, and deliberately: p.7's Hold and Fast
        /// Travel are taken INSTEAD of the card's printed action, and the card is resolved all the
        /// same. What Bullet Time then grants is the card's printed primary, which is a different
        /// action from the one performed - which is the second reason this could not be folded into the
        /// repeat's condition, because a repeat has nothing to be different from.</para>
        ///
        /// <para>A p.14 Defense is a <see cref="ActionInvocationContext.Reaction"/> spent on somebody
        /// else's turn and is not this hero resolving their own card, so it reads false.</para>
        /// </summary>
        public bool WasCardResolution { get; }

        public bool IsDefined => Kind != ActionKind.None;

        /// <summary>
        /// Whether rulebook p.10's repeat has anything to make a second instance of here. Milestone
        /// 8.25e; the whole of "is there something to repeat", asked in one place so the ability that
        /// offers and the authority that holds the subject cannot answer it differently.
        ///
        /// <para>Three sentences, and each removes a different case. There is no action at all when
        /// the completion recorded none. p.10's "You cannot repeat a repeated action!" removes the
        /// second instance. And <see cref="KindCanBeRepeated"/> removes the two kinds the page will
        /// not make a second instance of at all.</para>
        /// </summary>
        public bool CanBeRepeated =>
            IsDefined && !WasRepeatedInstance && KindCanBeRepeated(Kind);

        /// <summary>
        /// Whether rulebook p.10's repeat can produce a second instance of an action of this kind.
        ///
        /// <para>p.9 makes every action on a basic card basic, so this set is wider than the printed
        /// icons: an Attack, a Skill, a Movement and p.12's Hold are all repeatable, and repeating a
        /// Hold performs a Hold again - "you simply do nothing" is what that instance does, and it is
        /// still an action performed, so it raises p.8's checkpoints of its own.</para>
        ///
        /// <para><b>p.11's Fast Travel and p.13's Clear are not, and the page says so three times.</b>
        /// Both exist only as replacements - "instead of your entire Movement action", "instead of
        /// your entire Attack action" - and p.10's own bullet reads "A repeated instance of an action
        /// cannot be replaced with a different action (Such as Clear or Fast Travel)". p.11 adds "You
        /// can only perform one Fast Travel action, regardless of the movement value on the card!" and
        /// p.13 offers "one Clear secondary action". So the only instance such a repeat could produce
        /// is a second replacement, which all three sentences refuse; there is nothing else it could
        /// be, because performing the un-replaced action instead would be the replacement running
        /// backwards. The offer is therefore not made rather than made and dropped.</para>
        /// </summary>
        public static bool KindCanBeRepeated(ActionKind kind) =>
            kind == ActionKind.Attack ||
            kind == ActionKind.Skill ||
            kind == ActionKind.Movement ||
            kind == ActionKind.Hold;

        public bool Equals(PerformedBasicAction other) =>
            ActorId.Equals(other.ActorId) &&
            SourceCardInstanceId.Equals(other.SourceCardInstanceId) &&
            ActionDefinitionId.Equals(other.ActionDefinitionId) &&
            Kind == other.Kind &&
            WasRepeatedInstance == other.WasRepeatedInstance;

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
        /// <summary>
        /// Whether an action performed under <paramref name="invocationContext"/> was rulebook p.10's
        /// "resolve your card" rather than a second performance off a card already resolved.
        /// Milestone 8.38c2.
        ///
        /// <para>The four true arms are the card's own resolution: its printed primary, either
        /// secondary, and p.7's replacement actions, which are taken INSTEAD of the printed action and
        /// resolve the card all the same. <see cref="ActionInvocationContext.Repeated"/> and
        /// <see cref="ActionInvocationContext.TextGranted"/> are extra performances off a card already
        /// resolved, and <see cref="ActionInvocationContext.Reaction"/> is a p.14 Defense spent on
        /// somebody else's turn.</para>
        ///
        /// <para>It is a named rule rather than an expression at its one call site so that every
        /// context is decided in one place and a context added later has to be classified here rather
        /// than falling silently into "not a resolution".</para>
        /// </summary>
        public static bool IsCardResolution(ActionInvocationContext invocationContext)
        {
            switch (invocationContext)
            {
                case ActionInvocationContext.PrintedPrimary:
                case ActionInvocationContext.PrintedSecondary:
                case ActionInvocationContext.GeneralRuleSecondary:
                case ActionInvocationContext.Replacement:
                    return true;
                case ActionInvocationContext.Repeated:
                case ActionInvocationContext.TextGranted:
                case ActionInvocationContext.Reaction:
                case ActionInvocationContext.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(invocationContext), invocationContext,
                        "Every invocation context states whether it resolves the card it is performed " +
                        "on; a new one is classified here rather than defaulting.");
            }
        }

        /// <summary>
        /// Whether an action performed under <paramref name="invocationContext"/> is one TAKEN FROM a
        /// card whose own resolution is already behind it - the other side of
        /// <see cref="IsCardResolution"/>, and not its negation. Milestone 8.54.
        ///
        /// <para><b>Why it is not simply <c>!IsCardResolution</c>.</b> That question is about whether
        /// THIS performance resolves the card. This one is about whether THE CARD is resolved while
        /// this performance runs, and two of the false arms above answer it differently.
        /// <see cref="ActionInvocationContext.Repeated"/> is p.10's second instance of an action the
        /// turn is still holding open: milestones 8.25a and 8.25e put it deliberately above the card's
        /// own resolution, "while the hero it belongs to is still the one Initiative selected and their
        /// card is still unresolved", so a repeat runs on a card that is not resolved yet.
        /// <see cref="ActionInvocationContext.Reaction"/> is a p.14 Defense spent on somebody else's
        /// turn, where the card is not being resolved at all.</para>
        ///
        /// <para><b><see cref="ActionInvocationContext.TextGranted"/> is the one true arm</b>, and it
        /// is true by the printed sentences that produce it. Reload! borrows "your rightmost resolved
        /// card"; Ursafar's Silver borrows from cards already active; Bullet Time reads "After you
        /// RESOLVE a basic card you may perform the primary action on that card". Every one of them
        /// names a card whose resolution has happened before the instance they grant begins - which is
        /// what milestone 8.54 needed said out loud, because the engine commits the card's status flip
        /// BELOW the <c>ActionEnded</c> checkpoint the grant fires at, so the state alone reads a
        /// Bullet Time grant as running on a card mid-resolution.</para>
        ///
        /// <para>A named rule beside <see cref="IsCardResolution"/> rather than a comparison at its
        /// call site, for the reason that one is: a context added later is classified here or the
        /// switch refuses it.</para>
        /// </summary>
        public static bool IsPerformedOffACardAlreadyResolved(
            ActionInvocationContext invocationContext)
        {
            switch (invocationContext)
            {
                case ActionInvocationContext.TextGranted:
                    return true;
                case ActionInvocationContext.PrintedPrimary:
                case ActionInvocationContext.PrintedSecondary:
                case ActionInvocationContext.GeneralRuleSecondary:
                case ActionInvocationContext.Replacement:
                case ActionInvocationContext.Repeated:
                case ActionInvocationContext.Reaction:
                case ActionInvocationContext.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(invocationContext), invocationContext,
                        "Every invocation context states whether the card it is performed on has " +
                        "already resolved; a new one is classified here rather than defaulting.");
            }
        }

        public static PerformedBasicAction ForCompletedAction(
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            ActionDefinitionId actionDefinitionId,
            ActionKind kind,
            System.Collections.Generic.IEnumerable<CardDefinition> definitions,
            System.Collections.Generic.IEnumerable<CardInstance> instances,
            bool wasRepeatedInstance = false,
            bool placedSelf = false,
            bool defeatedMinionWithBasicAttack = false,
            bool wasCardResolution = false)
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
                            actorId, sourceCardInstanceId, actionDefinitionId, kind,
                            wasRepeatedInstance, placedSelf, defeatedMinionWithBasicAttack,
                            wasCardResolution)
                        : PerformedBasicAction.None;
                }
            }

            return PerformedBasicAction.None;
        }
    }
}
