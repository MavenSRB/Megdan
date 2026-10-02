using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.EffectResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects
{
    /// <summary>
    /// The four results a capability handler may return, verbatim from
    /// <c>CARD_EFFECT_CAPABILITIES.md</c>. They stay distinct because they mean different things to the
    /// enclosing control node: a legal rules no-op is <see cref="Applied"/>, not a failure, and a
    /// declined option is not an impossible one.
    /// </summary>
    public enum EffectExecutionResultKind
    {
        None = 0,

        /// <summary>Done, including a legal rules no-op.</summary>
        Applied = 1,

        /// <summary>Paused on an authoritative pending choice.</summary>
        NeedsChoice = 2,

        /// <summary>Cannot be done. Stops the enclosing sequence; prior applied nodes stay committed.</summary>
        ImpossibleMandatory = 3,

        /// <summary>Declined or skipped under its optionality policy; execution continues.</summary>
        DeclinedOptional = 4
    }

    /// <summary>
    /// What a capability accepts and produces. The definition validator type-checks every node binding
    /// against this before a match starts, so an incompatible definition is refused at content load
    /// rather than mid-resolution.
    /// </summary>
    public sealed class EffectCapabilityParameterSchema
    {
        public EffectCapabilityParameterSchema(
            IEnumerable<EffectCapabilityParameter> parameters = null,
            IEnumerable<EffectOutputBinding> outputs = null)
        {
            var declaredParameters = (parameters ?? Enumerable.Empty<EffectCapabilityParameter>()).ToArray();
            if (declaredParameters.Select(parameter => parameter.Name)
                .Distinct(StringComparer.Ordinal).Count() != declaredParameters.Length)
            {
                throw new ArgumentException("Capability parameter names must be unique.", nameof(parameters));
            }

            var declaredOutputs = (outputs ?? Enumerable.Empty<EffectOutputBinding>()).ToArray();
            if (declaredOutputs.Select(output => output.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != declaredOutputs.Length)
            {
                throw new ArgumentException("Capability output names must be unique.", nameof(outputs));
            }

            Parameters = new ReadOnlyCollection<EffectCapabilityParameter>(declaredParameters);
            Outputs = new ReadOnlyCollection<EffectOutputBinding>(declaredOutputs);
        }

        public IReadOnlyList<EffectCapabilityParameter> Parameters { get; }
        public IReadOnlyList<EffectOutputBinding> Outputs { get; }
    }

    /// <summary>
    /// The exact legal members of one enum-backed integer vocabulary.
    ///
    /// <para>Effect values remain the small, snapshot-stable integer union the resolver already
    /// carries. This definition-time contract supplies the information that union deliberately does
    /// not: which enum the integer belongs to, and which of that enum's members this parameter admits.
    /// It is immutable so a catalog cannot validate one definition against a domain that later
    /// changes underneath it.</para>
    /// </summary>
    public readonly struct EffectIntegerDomain : IEquatable<EffectIntegerDomain>
    {
        private readonly Type vocabularyType;
        private readonly IReadOnlyList<int> legalValues;

        private EffectIntegerDomain(Type vocabularyType, IEnumerable<int> legalValues)
        {
            if (vocabularyType == null || !vocabularyType.IsEnum ||
                Enum.GetUnderlyingType(vocabularyType) != typeof(int))
            {
                throw new ArgumentException(
                    "An effect integer domain requires an enum whose underlying type is Int32.",
                    nameof(vocabularyType));
            }

            var materialized = (legalValues ?? throw new ArgumentNullException(nameof(legalValues)))
                .Distinct()
                .OrderBy(value => value)
                .ToArray();
            if (materialized.Length == 0 ||
                materialized.Any(value => !Enum.IsDefined(vocabularyType, value)))
            {
                throw new ArgumentException(
                    "An effect integer domain requires at least one defined enum member and may " +
                    "contain only defined members.", nameof(legalValues));
            }

            this.vocabularyType = vocabularyType;
            this.legalValues = new ReadOnlyCollection<int>(materialized);
        }

        public bool IsDefined => vocabularyType != null;

        public Type VocabularyType => vocabularyType;

        public IReadOnlyList<int> LegalValues => legalValues ?? Array.Empty<int>();

        public static EffectIntegerDomain All<TEnum>() where TEnum : struct =>
            Create<TEnum>(Enum.GetValues(typeof(TEnum)).Cast<TEnum>());

        public static EffectIntegerDomain AllExcept<TEnum>(params TEnum[] excluded)
            where TEnum : struct
        {
            RequireEnum<TEnum>();
            var omitted = new HashSet<TEnum>(excluded ?? Array.Empty<TEnum>());
            return Create<TEnum>(Enum.GetValues(typeof(TEnum)).Cast<TEnum>()
                .Where(value => !omitted.Contains(value)));
        }

        public static EffectIntegerDomain Only<TEnum>(params TEnum[] legalValues)
            where TEnum : struct =>
            Create<TEnum>(legalValues ?? Array.Empty<TEnum>());

        public bool Contains(int value) => IsDefined && LegalValues.Contains(value);

        public bool HasSameVocabulary(EffectIntegerDomain other) =>
            IsDefined && other.IsDefined && vocabularyType == other.vocabularyType;

        public bool Equals(EffectIntegerDomain other) =>
            vocabularyType == other.vocabularyType && LegalValues.SequenceEqual(other.LegalValues);

        public override bool Equals(object obj) =>
            obj is EffectIntegerDomain other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = vocabularyType?.GetHashCode() ?? 0;
                foreach (var value in LegalValues)
                {
                    hash = (hash * 397) ^ value;
                }

                return hash;
            }
        }

        public override string ToString() => IsDefined
            ? $"{vocabularyType.Name}[{string.Join(",", LegalValues)}]"
            : "undeclared";

        private static EffectIntegerDomain Create<TEnum>(IEnumerable<TEnum> legalValues)
            where TEnum : struct
        {
            RequireEnum<TEnum>();
            return new EffectIntegerDomain(
                typeof(TEnum), legalValues.Select(value => Convert.ToInt32(value)));
        }

        private static void RequireEnum<TEnum>() where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum || Enum.GetUnderlyingType(typeof(TEnum)) != typeof(int))
            {
                throw new ArgumentException(
                    $"'{typeof(TEnum).FullName}' is not an Int32-backed enum vocabulary.");
            }
        }
    }

    public readonly struct EffectCapabilityParameter
    {
        public EffectCapabilityParameter(
            string name,
            EffectValueKind valueKind,
            bool isRequired = true,
            EffectIntegerDomain integerDomain = default)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("A capability parameter requires a name.", nameof(name));
            }

            if (valueKind == EffectValueKind.None)
            {
                throw new ArgumentException(
                    "A capability parameter requires a typed value kind.", nameof(valueKind));
            }

            if (integerDomain.IsDefined && valueKind != EffectValueKind.Integer)
            {
                throw new ArgumentException(
                    "Only an Integer capability parameter may declare an enum-backed domain.",
                    nameof(integerDomain));
            }

            Name = name;
            ValueKind = valueKind;
            IsRequired = isRequired;
            IntegerDomain = integerDomain;
        }

        public string Name { get; }
        public EffectValueKind ValueKind { get; }
        public bool IsRequired { get; }
        public EffectIntegerDomain IntegerDomain { get; }
    }

    /// <summary>
    /// Milestone 9.6, widened at 9.38: whether <paramref name="cardId"/>'s printed primary, performed
    /// by <paramref name="actorId"/> as a borrowed instance, could do anything now - judged by the
    /// rules the turn applies to its own offers (6.5af's Skill, 7.8a1's Attack) - once
    /// <paramref name="excludedUnit"/> is barred from it. The exclusion is the unit a borrow's own
    /// clause names ("targeting a different unit": the unit its Attack took, WI-07) and
    /// <see cref="RuleObject.None"/> where the clause names none.
    /// </summary>
    public delegate bool BorrowedPrimaryJudgement(CardInstanceId cardId, HeroId actorId, RuleObject excludedUnit);

    /// <summary>
    /// Everything a handler is allowed to see. Immutable, and carries no Unity object, delegate, or
    /// continuation: the frame owns where execution is, the handler only decides what happens here.
    /// </summary>
    public sealed class EffectCapabilityContext
    {
        public EffectCapabilityContext(
            EffectNodeId nodeId,
            EffectFailurePolicy failurePolicy,
            HeroId actorId,
            HeroId controllerId,
            CardInstanceId sourceCardInstanceId,
            CardDefinitionReference sourceDefinitionReference,
            ActionDefinitionId actionDefinitionId,
            IReadOnlyDictionary<string, EffectValue> inputs,
            int roundIndex,
            int turnIndex,
            MatchParticipants participants,
            ActiveEffectLifecycle timing,
            RuleModificationSet modifications,
            EffectCardState cardState,
            ActionKind enclosingActionKind,
            MatchComponents components = null,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems = null,
            IReadOnlyList<IReadOnlyList<EffectValue>> answersAlreadyGiven = null,
            EnclosingAttackFacts enclosingAttack = default,
            EffectRosterState rosterState = null,
            IReadOnlyList<ImmunityOverrideDeclaration> immunityOverrides = null,
            IReadOnlyList<EffectOutputBinding> declaredOutputs = null,
            EffectCapabilityCatalog capabilities = null,
            HexCoordinate? prospectiveActorSpace = null,
            ActionInvocationContext invocationContext = ActionInvocationContext.None,
            TeamSide tieBreakerRevealedSide = TeamSide.None,
            BorrowedPrimaryJudgement borrowedPrimaryCanAct = null,
            IReadOnlyList<IReadOnlyList<EffectValue>> answersGivenAcrossTheGroup = null)
        {
            BorrowedPrimaryCanAct = borrowedPrimaryCanAct;
            AnswersGivenAcrossTheGroup =
                answersGivenAcrossTheGroup ?? Array.Empty<IReadOnlyList<EffectValue>>();
            TieBreakerRevealedSide = tieBreakerRevealedSide;
            InvocationContext = invocationContext;
            ProspectiveActorSpace = prospectiveActorSpace;
            Capabilities = capabilities;
            EnclosingAttack = enclosingAttack;
            if (!Enum.IsDefined(typeof(ActionKind), enclosingActionKind) ||
                enclosingActionKind == ActionKind.None)
            {
                throw new ArgumentException(
                    "An effect capability context names the action its effect is running inside.",
                    nameof(enclosingActionKind));
            }

            EnclosingActionKind = enclosingActionKind;
            if (!nodeId.IsDefined)
            {
                throw new ArgumentException("An effect capability context requires a node.", nameof(nodeId));
            }

            if (!actorId.IsDefined)
            {
                throw new ArgumentException("An effect capability context requires an actor.", nameof(actorId));
            }

            NodeId = nodeId;
            FailurePolicy = failurePolicy;
            ActorId = actorId;
            ControllerId = controllerId;
            SourceCardInstanceId = sourceCardInstanceId;
            SourceDefinitionReference = sourceDefinitionReference;
            ActionDefinitionId = actionDefinitionId;
            Inputs = inputs ?? new Dictionary<string, EffectValue>();
            RoundIndex = roundIndex;
            TurnIndex = turnIndex;
            Participants = participants;
            Timing = timing;
            Modifications = modifications ?? throw new ArgumentNullException(nameof(modifications));
            CardState = cardState ?? throw new ArgumentNullException(nameof(cardState));
            Components = components;
            HeroItems = heroItems;
            AnswersAlreadyGiven = answersAlreadyGiven ?? Array.Empty<IReadOnlyList<EffectValue>>();
            RosterState = rosterState ?? EffectRosterState.Absent;
            ImmunityOverrides = new ReadOnlyCollection<ImmunityOverrideDeclaration>(
                (immunityOverrides ?? Enumerable.Empty<ImmunityOverrideDeclaration>()).ToArray());
            DeclaredOutputs = new ReadOnlyCollection<EffectOutputBinding>(
                (declaredOutputs ?? Enumerable.Empty<EffectOutputBinding>()).ToArray());
        }

        /// <summary>
        /// The registered capability set this frame is running against, or <c>null</c> where the caller
        /// has none to give.
        ///
        /// <para>Milestone 8.35z31. Every handler before Ursafar's borrow answered its question
        /// from the board, the cards, the roster or the timing state; his Silver asks one that is about
        /// <em>content</em> - "one of your active cards <b>with an active effect</b>", where whether a
        /// card's text prints an active effect is a fact about what that card composes. The catalog is
        /// where composition meaning lives, so the reading is taken from it rather than from a flag
        /// declared beside each card, which could be set wrongly and would be one more thing to keep in
        /// step with the text.</para>
        ///
        /// <para>Optional because two of this type's callers are outside a content walk and neither
        /// needs it; a handler that requires it says so by refusing when it is absent.</para>
        /// </summary>
        /// <summary>
        /// Milestone 8.75. The revealed face of the Tie Breaker coin
        /// (<see cref="GuardsDigital.Core.Setup.TieBreakerState.RevealedSide"/>), or
        /// <see cref="TeamSide.None"/> where it is unrevealed or the caller had none to give. Ignatia
        /// the Mad's whole set reads it: her cards print a sun (☀) and a moon (☾) text, and the coin's
        /// live face selects which applies. No other hero consults it, so it defaults to the named
        /// absence and every frame before her keeps the walk it had.
        /// </summary>
        public TeamSide TieBreakerRevealedSide { get; }

        public EffectCapabilityCatalog Capabilities { get; }

        /// <summary>
        /// The outputs this node asked for, which is the exact set a handler may publish.
        ///
        /// <para>Milestone 8.20. A restored payload's recorded outcomes are checked against the node's
        /// declaration, so a handler publishing a fact the node did not ask for makes a snapshot the
        /// build refuses to read back. That is not a new rule - it is why every capability's outputs
        /// have always been declared per node - and until now no handler had an output some of its
        /// nodes wanted and others did not.</para>
        ///
        /// <para>Two of milestone 8.20's do: the roster selection publishes the chosen piece's space
        /// beside its identity, and the printed-icon minion selection publishes the chosen minion's.
        /// Both are for cards whose next step repositions the piece, and neither is wanted by the
        /// cards already using those capabilities - so asking the node is what lets the vocabulary grow
        /// without every earlier definition growing with it.</para>
        ///
        /// <para>Use it through <see cref="Publishes"/> rather than by searching it, so the one reading
        /// of "did this node ask for that" stays one reading.</para>
        /// </summary>
        public IReadOnlyList<EffectOutputBinding> DeclaredOutputs { get; }

        /// <summary>Whether this node asked for the named output. Milestone 8.20.</summary>
        public bool Publishes(string outputName) =>
            DeclaredOutputs.Any(output =>
                string.Equals(output.OutputName, outputName, StringComparison.Ordinal));

        public EffectNodeId NodeId { get; }
        public EffectFailurePolicy FailurePolicy { get; }
        public HeroId ActorId { get; }
        public HeroId ControllerId { get; }
        public CardInstanceId SourceCardInstanceId { get; }

        /// <summary>
        /// The definition revision this resolution's source card was played at, recorded by the frame
        /// at start. Milestone 7.2b added it because a capability that reads its own card's printed
        /// icon has to name the content it is reading, and the frame's own reference is what the
        /// resolution began from - re-deriving one from the instance list would let a capability read a
        /// revision the resolution never started under.
        /// </summary>
        public CardDefinitionReference SourceDefinitionReference { get; }

        public ActionDefinitionId ActionDefinitionId { get; }

        /// <summary>
        /// The kind of action this effect is running inside, and therefore the activity its operations
        /// are performed as. Milestone 8.5 (F-BOARD-03). Ask
        /// <see cref="EnclosingActivity"/> rather than mapping this yourself: the two enums are
        /// different vocabularies and only one bridge between them exists.
        /// </summary>
        public ActionKind EnclosingActionKind { get; }

        /// <summary>
        /// What this effect may ask about the Attack it is running inside - the attacker, the hero it
        /// took, and p.13.s printed subtype - or the named absence outside one. Milestone 8.10 for
        /// Thunder Boomerang.s "If you targeted a hero"; widened at 8.11 for Wasp.s three Block cards.
        /// </summary>
        public EnclosingAttackFacts EnclosingAttack { get; }

        /// <summary>
        /// p.18's activity this effect's operations count as - the enclosing action's, because card
        /// text has none of its own. Milestone 6.5b settled that reading for card-text direct Move and
        /// milestone 8.5 made it true for every action an effect can run inside rather than for the
        /// Skill alone.
        /// </summary>
        public RuleActivity EnclosingActivity => RuleActivityQuery.ForAction(EnclosingActionKind);

        public IReadOnlyDictionary<string, EffectValue> Inputs { get; }
        public int RoundIndex { get; }
        public int TurnIndex { get; }
        public MatchParticipants Participants { get; }
        public ActiveEffectLifecycle Timing { get; }

        /// <summary>
        /// The modifications in effect. Required since milestone 6.4: a handler that displaces
        /// something consults p.18's immunity and the Move-Through permission, so a capability is a
        /// rules-consuming seam like any other and takes the live set rather than an optional one.
        /// <see cref="RuleModificationSet.Empty"/> is the named absence.
        /// </summary>
        public RuleModificationSet Modifications { get; }

        /// <summary>
        /// The card state this handler may read. Required since milestone 7.1a: a capability that
        /// derives candidates from a hand needs the instances, and needs them identically at every
        /// point it can be asked - Skill start, repeated-Skill start, the option-feasibility walk, the
        /// choice-answer path, and snapshot restore's candidate re-derivation.
        ///
        /// <see cref="EffectCardState.Absent"/> is the named absence and throws when read, so a
        /// capability handed no card state fails loudly instead of reporting the empty candidate set
        /// that an ordinary, legal "the chooser holds nothing eligible" also produces. Those two mean
        /// opposite things to the enclosing node, and nothing may quietly turn the first into the
        /// second.
        /// </summary>
        public EffectCardState CardState { get; }

        // These queries concern the hero performing this action, whose on-board eligibility is
        // already validated by action authority. Unlocked Ultimate declarations are derived from
        // current card state, not stored in the Active-effect lifecycle.
        public IEnumerable<ContinuousRuleSource> ActorContinuousSources =>
            (Timing?.ContinuousSources ?? Array.Empty<ContinuousRuleSource>()).Concat(
                CardState.IsAvailable
                    ? GuardsDigital.Core.Rules.Progression.UltimateUnlockQuery
                        .Derive(CardState.Definitions, CardState.Instances).Ultimates
                        .Where(ultimate => ultimate.OwnerId.Equals(ActorId)).Select(ultimate => ultimate.Source)
                    : Enumerable.Empty<ContinuousRuleSource>());

        /// <summary>
        /// Milestone 6.2's token and marker collaborators, or <c>null</c> outside a live match. Like
        /// <see cref="Timing"/>, this is authoritative state the handler may mutate through a named
        /// operation; unlike a Unity object or a delegate, none of it is a continuation, so the frame
        /// still owns where execution is.
        /// </summary>
        public MatchComponents Components { get; }

        /// <summary>
        /// Milestone 7.2b: the match's items, or <c>null</c> in a composition that holds none - the
        /// same absence <see cref="Components"/> expresses. A capability that reads a printed value
        /// composes rulebook p.16's Item Bonus onto it through the one query every other total uses, so
        /// the same state the Defense, Attack, Movement, Range and Initiative totals read reaches the
        /// effect frame too. It is the state the round owns rather than a copy: a Radius derived from
        /// items other than the ones the match records would be a disk no restore or replay could
        /// reproduce, which is the same reason the marker register and the item state are bound rather
        /// than constructed at every seam that needs them.
        /// </summary>
        public GuardsDigital.Core.Rules.Progression.HeroItemState HeroItems { get; }

        /// <summary>
        /// The answers <em>this node</em> has already been given inside this resolution, oldest first,
        /// each one the values that closed a single choice. Empty for a node that has asked nothing yet,
        /// which is every node in this build until milestone 8.8.
        ///
        /// <para>It exists because p.18's multi-target Push asks two questions: which of the units the
        /// card admits are being pushed, and - where the card names more than one distance, as Kinetic
        /// Blast's "3 or 4 spaces" does - how far. One capability asking twice is the honest shape,
        /// because the rule is one keyword and both answers feed the same single batch; the alternative
        /// was a second node deciding a distance for a push it does not perform, which would have left
        /// the same sentence stated in two places.</para>
        ///
        /// <para>It is not memory the handler owns. These are the resolution's own accepted choice
        /// records, which the frame already carries and a snapshot already persists, so a restored pause
        /// re-derives with exactly the answers it had. A handler still re-derives everything it can:
        /// what an earlier answer names is checked against live state at the moment it is used, for the
        /// reason milestone 6.8l gives - what was true when a choice opened is not what its answer
        /// applies to.</para>
        /// </summary>
        public IReadOnlyList<IReadOnlyList<EffectValue>> AnswersAlreadyGiven { get; }

        /// <summary>
        /// Milestone 9.8c. The answers the OTHER members of this node's selection group
        /// (<see cref="EffectSelectionDeclaration.DistinctAcrossParameterName"/>) have already been given
        /// in this resolution, oldest first. Empty for a node in no group. Read off the frame's accepted
        /// choices exactly as <see cref="AnswersAlreadyGiven"/> is, so it persists and replays with them.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<EffectValue>> AnswersGivenAcrossTheGroup { get; }

        /// <summary>The round-owned roster state, or its named absence.</summary>
        public EffectRosterState RosterState { get; }

        /// <summary>
        /// Exact trusted immunity exceptions on this node. A handler must advertise support before the
        /// definition validator admits any.
        /// </summary>
        public IReadOnlyList<ImmunityOverrideDeclaration> ImmunityOverrides { get; }

        /// <summary>
        /// Milestone 8.35z42: the space the acting hero would be standing on, for a question
        /// asked about a board he has not reached yet - and the named absence everywhere else, which is
        /// every live resolution.
        ///
        /// <para><b>Why it exists.</b> A card that prints "You may move 1 space. Target a unit adjacent
        /// to you." composes the move and the selection as two ordered steps, and the offer question
        /// stands before either. Judging "adjacent to you" from the square he is about to leave answers
        /// about the wrong space, so milestone 8.35p suppressed the reading entirely and the Attack was
        /// offered wherever it could not be read. Suppression is the right default and the wrong
        /// answer when the destinations are enumerable: the walk can ask the same question once per
        /// space he could reach, and refuse only when every one of them is empty. This is the one seam
        /// that carries the hypothetical space, so a selection declaring
        /// <c>origin = Actor</c> reads it in exactly the place it reads the real one.</para>
        ///
        /// <para><b>It never reaches a live resolution.</b> Only the feasibility walk sets it, and only
        /// around <see cref="IEffectCapabilityHandler.Anticipate"/>, which may mutate nothing;
        /// <see cref="IEffectCapabilityHandler.Execute"/> and
        /// <see cref="IEffectCapabilityHandler.Resume"/> are never called on a context carrying one.
        /// The resolver builds its contexts without it and nothing on the answering path passes it
        /// on.</para>
        /// </summary>
        public HexCoordinate? ProspectiveActorSpace { get; }

        /// <summary>
        /// Rulebook p.10's question about the performance this effect is running as: the card's own
        /// resolution, a repeat, a borrowed or ability-granted instance, or a p.14 Defense. Milestone
        /// 8.54.
        ///
        /// <para><b>Why a handler may need it.</b> The frame has always carried it - it is what
        /// <c>PendingResolution.InvocationContext</c> records and what every snapshot of a paused
        /// resolution already persists - but no capability had a question that turned on it. Bounce's
        /// "if this card is already resolved as you perform this action" is the first: the engine
        /// commits a card's resolution status BELOW the <c>ActionEnded</c> checkpoint (milestone
        /// 8.25e, so a held action still has an unresolved card and a selected actor to run on), so an
        /// instance Bullet Time grants at that checkpoint reads a card the rulebook has already
        /// resolved as mid-resolution. The performance's own invocation is what tells the two apart,
        /// and <c>BasicActionQuery.IsPerformedOffACardAlreadyResolved</c> is the one place that
        /// reading is decided.</para>
        ///
        /// <para><see cref="ActionInvocationContext.None"/> is the named absence, which is every
        /// context built outside a resolution frame - the offer-feasibility walk's probes and the
        /// fixtures that ask a handler directly. It classifies as "not performed off a resolved card",
        /// so a context that names no performance changes no answer.</para>
        /// </summary>
        public ActionInvocationContext InvocationContext { get; }

        /// <summary>
        /// Milestone 9.6: whether a card's printed primary, performed by a hero as a borrowed
        /// instance, could do anything now - the same answer the turn gives its own offers (6.5af,
        /// 7.8a1). The turn's offer walk supplies it, so Reload! is greyed when the card it would
        /// borrow can do nothing and nothing else on it can either (user ruling 2026-09-28); since
        /// milestone 9.38 an Attack's own card text supplies it too, with the unit the Attack took
        /// barred, so Fight as One offers only a resolved Skill that can act on a different unit.
        /// <c>null</c> everywhere else, where a borrow is judged as able.
        /// </summary>
        public BorrowedPrimaryJudgement BorrowedPrimaryCanAct { get; }

        /// <summary>
        /// This same context, asked as if the acting hero already stood on <paramref name="space"/>.
        ///
        /// <para>Everything else is carried across unchanged, because nothing else about the question
        /// moves with him: the node, the inputs, the answers already given, the modifications and the
        /// board are all the ones this walk is judging. Only where he is standing is hypothetical, and
        /// only a reader that asks for the actor's own origin can tell.</para>
        /// </summary>
        internal EffectCapabilityContext AsIfTheActorStoodAt(HexCoordinate space) =>
            new EffectCapabilityContext(
                NodeId,
                FailurePolicy,
                ActorId,
                ControllerId,
                SourceCardInstanceId,
                SourceDefinitionReference,
                ActionDefinitionId,
                Inputs,
                RoundIndex,
                TurnIndex,
                Participants,
                Timing,
                Modifications,
                CardState,
                EnclosingActionKind,
                Components,
                HeroItems,
                AnswersAlreadyGiven,
                EnclosingAttack,
                RosterState,
                ImmunityOverrides,
                DeclaredOutputs,
                Capabilities,
                space,
                InvocationContext,
                borrowedPrimaryCanAct: BorrowedPrimaryCanAct,
                answersGivenAcrossTheGroup: AnswersGivenAcrossTheGroup);

        /// <summary>
        /// Milestone 8.88. This same context with <paramref name="clauses"/> added to its inputs, each
        /// replacing an input of the same name.
        ///
        /// <para>It exists so a hero-local selection whose narrowings are fixed by its own identity -
        /// Ignatia's "a unit in range in a straight line" bakes its clause into the capability rather
        /// than reading it from a parameter - can ask the printed-icon family's one candidate
        /// derivation the same question a definition binding those clauses would ask. Nothing else about
        /// the question moves: the node, the answers already given, the board, the timing and the
        /// hypothetical actor space are all carried across unchanged.</para>
        /// </summary>
        internal EffectCapabilityContext WithAdditionalInputs(
            IReadOnlyDictionary<string, EffectValue> clauses)
        {
            var merged = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
            foreach (var input in Inputs)
            {
                merged[input.Key] = input.Value;
            }

            foreach (var clause in clauses ?? throw new ArgumentNullException(nameof(clauses)))
            {
                merged[clause.Key] = clause.Value;
            }

            return new EffectCapabilityContext(
                NodeId,
                FailurePolicy,
                ActorId,
                ControllerId,
                SourceCardInstanceId,
                SourceDefinitionReference,
                ActionDefinitionId,
                merged,
                RoundIndex,
                TurnIndex,
                Participants,
                Timing,
                Modifications,
                CardState,
                EnclosingActionKind,
                Components,
                HeroItems,
                AnswersAlreadyGiven,
                EnclosingAttack,
                RosterState,
                ImmunityOverrides,
                DeclaredOutputs,
                Capabilities,
                ProspectiveActorSpace,
                InvocationContext,
                TieBreakerRevealedSide,
                BorrowedPrimaryCanAct,
                AnswersGivenAcrossTheGroup);
        }

        public EffectValue Input(string parameterName) =>
            Inputs.TryGetValue(parameterName, out var value) ? value : default;
    }

    /// <summary>
    /// Milestone 8.40f. One card a handler turned face up for the whole table, and the hero whose
    /// card it is.
    /// </summary>
    /// <remarks>
    /// <para>It is a typed report rather than an <see cref="EffectOutcomeFact"/>, for the reason
    /// <see cref="EffectExecutionResult.AppliedPushes"/> is one: nothing binds it. What reads it is
    /// the backend that turns a node's facts into the ordered event stream, so that the seat-public
    /// match log carries one row naming hero and card - and that row is the whole publication
    /// mechanism, because <c>SeatProjection.IsPublic</c> already admits a HAND card to every seat
    /// exactly when the bounded log names it.</para>
    /// <para>The hero is the card's OWNER and never the actor. A reveal that named the acting hero
    /// would print somebody else's name beside the card they turned over, and the owner is also what
    /// the surface needs to draw it beside the right dashboard.</para>
    /// </remarks>
    public readonly struct RevealedCard : IEquatable<RevealedCard>
    {
        public RevealedCard(HeroId ownerHeroId, CardInstanceId cardInstanceId)
        {
            if (!ownerHeroId.IsDefined || !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A reveal names an owning hero and a card instance.");
            }

            OwnerHeroId = ownerHeroId;
            CardInstanceId = cardInstanceId;
        }

        public HeroId OwnerHeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public bool IsDefined => OwnerHeroId.IsDefined && CardInstanceId.IsDefined;

        public bool Equals(RevealedCard other) =>
            OwnerHeroId.Equals(other.OwnerHeroId) && CardInstanceId.Equals(other.CardInstanceId);

        public override bool Equals(object obj) => obj is RevealedCard other && Equals(other);

        public override int GetHashCode() =>
            (OwnerHeroId.GetHashCode() * 397) ^ CardInstanceId.GetHashCode();

        public override string ToString() => OwnerHeroId + ":" + CardInstanceId;
    }

    /// <summary>
    /// One handler outcome: its kind, the typed facts it published, and the timing checkpoints it
    /// declares around itself. The checkpoints become resolver steps, so an effect raises timing the
    /// same way milestone 6.1ap made every other action raise it.
    /// </summary>
    /// <summary>
    /// Milestone 9.19. A card that card text sent from a hand to its owner's discard pile, and whose
    /// owner. Reported by the discarding handlers on the terms <see cref="RevealedCard"/> set at
    /// 8.40f, so the seat-public log can say that it happened - until this milestone a card-text
    /// discard (Charged Runestone, the Seismic faces, Trample, Mortimer's Dead faces) left no row
    /// and no narration, and a discard nobody at the table could see read as a discard that had not
    /// happened.
    /// </summary>
    public readonly struct DiscardedCard : IEquatable<DiscardedCard>
    {
        public DiscardedCard(HeroId ownerHeroId, CardInstanceId cardInstanceId)
        {
            if (!ownerHeroId.IsDefined || !cardInstanceId.IsDefined)
            {
                throw new ArgumentException("A discard names an owning hero and a card instance.");
            }

            OwnerHeroId = ownerHeroId;
            CardInstanceId = cardInstanceId;
        }

        public HeroId OwnerHeroId { get; }
        public CardInstanceId CardInstanceId { get; }
        public bool IsDefined => OwnerHeroId.IsDefined && CardInstanceId.IsDefined;

        public bool Equals(DiscardedCard other) =>
            OwnerHeroId.Equals(other.OwnerHeroId) && CardInstanceId.Equals(other.CardInstanceId);

        public override bool Equals(object obj) => obj is DiscardedCard other && Equals(other);

        public override int GetHashCode() =>
            (OwnerHeroId.GetHashCode() * 397) ^ CardInstanceId.GetHashCode();

        public override string ToString() => OwnerHeroId + ":" + CardInstanceId;
    }

    /// <summary>Milestone 9.30h. What card text did to a hero's spellbook - see <see cref="SpellbookChange"/>.</summary>
    public enum SpellbookChangeKind
    {
        None = 0,

        /// <summary>A spell was cast: removed from the book faceup as it is cast (Prepare Spells).</summary>
        Cast = 1,

        /// <summary>A spell was removed from the book faceup WITHOUT being cast (Find Familiar).</summary>
        RemovedFaceup = 2,

        /// <summary>Removed spells were added back to the book, facedown (Prepare Spells' refill).</summary>
        Refilled = 3
    }

    /// <summary>
    /// Milestone 9.30h. A change card text made to a hero's spellbook, reported by the spellbook
    /// handlers on the terms <see cref="DiscardedCard"/> set at 9.19, so the seat-public log can say
    /// it happened: a cast, a faceup removal, a refill's count.
    ///
    /// <para><b>Only a spell that is FACEUP when the change is made is named.</b> A cast and a
    /// removal leave the spell faceup in the open removed pile, so naming it publishes nothing a seat
    /// cannot already read. A refill turns spells FACEDOWN, so it names none - only how many returned
    /// (lesson 25: a facedown spell reaches its owner's seat alone).</para>
    /// </summary>
    public readonly struct SpellbookChange : IEquatable<SpellbookChange>
    {
        private SpellbookChange(
            SpellbookChangeKind kind, HeroId ownerHeroId, CardInstanceId spellCardInstanceId, int count)
        {
            if (!ownerHeroId.IsDefined)
            {
                throw new ArgumentException("A spellbook change names the hero whose book it is.");
            }

            Kind = kind;
            OwnerHeroId = ownerHeroId;
            SpellCardInstanceId = spellCardInstanceId;
            Count = count;
        }

        /// <summary>The spell was cast and now lies faceup in the removed pile.</summary>
        public static SpellbookChange Cast(HeroId ownerHeroId, CardInstanceId spell) =>
            !spell.IsDefined
                ? throw new ArgumentException("A cast names the spell it cast.", nameof(spell))
                : new SpellbookChange(SpellbookChangeKind.Cast, ownerHeroId, spell, 1);

        /// <summary>The spell was removed faceup without being cast.</summary>
        public static SpellbookChange RemovedFaceup(HeroId ownerHeroId, CardInstanceId spell) =>
            !spell.IsDefined
                ? throw new ArgumentException("A removal names the spell it removed.", nameof(spell))
                : new SpellbookChange(SpellbookChangeKind.RemovedFaceup, ownerHeroId, spell, 1);

        /// <summary>How many spells returned facedown; none is named.</summary>
        public static SpellbookChange Refilled(HeroId ownerHeroId, int count) =>
            count < 1
                ? throw new ArgumentOutOfRangeException(nameof(count), count, "A refill reports what it returned.")
                : new SpellbookChange(SpellbookChangeKind.Refilled, ownerHeroId, default, count);

        public SpellbookChangeKind Kind { get; }
        public HeroId OwnerHeroId { get; }

        /// <summary>The faceup spell a cast or a removal names; undefined on a refill.</summary>
        public CardInstanceId SpellCardInstanceId { get; }

        /// <summary>How many spells moved: one for a cast or a removal, the returned count for a refill.</summary>
        public int Count { get; }

        public bool IsDefined => Kind != SpellbookChangeKind.None && OwnerHeroId.IsDefined;

        public bool Equals(SpellbookChange other) =>
            Kind == other.Kind && OwnerHeroId.Equals(other.OwnerHeroId) &&
            SpellCardInstanceId.Equals(other.SpellCardInstanceId) && Count == other.Count;

        public override bool Equals(object obj) => obj is SpellbookChange other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Kind;
                hash = (hash * 397) ^ OwnerHeroId.GetHashCode();
                hash = (hash * 397) ^ SpellCardInstanceId.GetHashCode();
                return (hash * 397) ^ Count;
            }
        }

        public override string ToString() => Kind + ":" + OwnerHeroId + ":" + SpellCardInstanceId + "x" + Count;
    }

    /// <summary>Milestone 9.32g. Where a card stood before or after card text moved it.</summary>
    public enum CardMoveZone
    {
        None = 0,
        Hand = 1,
        Deck = 2,
        TurnArea = 3,
        DiscardPile = 4
    }

    /// <summary>
    /// Milestone 9.32g. One card a swap or an Ultimate's unlock moved (or a swap left where it was),
    /// as the seat-public log needs it: which card, by its definition; from where to where; whether
    /// it lay facedown before or arrives facedown after; and the colour the moving text itself fixes
    /// ("your gold card", "return your silver card") - <see cref="CardColor.None"/> where it fixes
    /// none.
    ///
    /// <para><b>Who may be told which card it is.</b> <see cref="IsSeenByTheTable"/>: the card lay
    /// faceup in an open zone before the move, or lies faceup in one after it. A hand or deck card
    /// that stays out of sight, and a card that arrives facedown, are named to their owner's seat
    /// alone (approved mockup F) - the seat projection clears <see cref="Definition"/> for every other
    /// seat, and the known colour is all that seat reads.</para>
    /// </summary>
    public readonly struct CardMoveFact : IEquatable<CardMoveFact>
    {
        public CardMoveFact(
            CardDefinitionId definition,
            CardMoveZone from,
            CardMoveZone to,
            bool departsFacedown = false,
            bool arrivesFacedown = false,
            CardColor knownColor = CardColor.None)
        {
            if (from == CardMoveZone.None || to == CardMoveZone.None)
            {
                throw new ArgumentException("A card move names where the card was and where it went.");
            }

            if (knownColor == CardColor.Purple)
            {
                throw new ArgumentException(
                    "Text fixes a card's colour only as one of the four card colours.", nameof(knownColor));
            }

            Definition = definition;
            From = from;
            To = to;
            DepartsFacedown = departsFacedown;
            ArrivesFacedown = arrivesFacedown;
            KnownColor = knownColor;
        }

        /// <summary>The moved card's definition; undefined on a row another seat may not name.</summary>
        public CardDefinitionId Definition { get; }

        public CardMoveZone From { get; }

        public CardMoveZone To { get; }

        public bool DepartsFacedown { get; }

        public bool ArrivesFacedown { get; }

        public CardColor KnownColor { get; }

        /// <summary>Whether every seat saw this card's face at one end of the move.</summary>
        public bool IsSeenByTheTable() =>
            (IsOpen(From) && !DepartsFacedown) || (IsOpen(To) && !ArrivesFacedown);

        /// <summary>The same move with its card left unnamed - the form another seat reads.</summary>
        public CardMoveFact Unnamed() =>
            new CardMoveFact(default, From, To, DepartsFacedown, ArrivesFacedown, KnownColor);

        /// <summary>The zone a card in this state stands in, or <see cref="CardMoveZone.None"/>.</summary>
        public static CardMoveZone ZoneOf(CardInstance card)
        {
            switch (card?.State.Location ?? CardLocation.None)
            {
                case CardLocation.Hand: return CardMoveZone.Hand;
                case CardLocation.Deck: return CardMoveZone.Deck;
                case CardLocation.TurnArea: return CardMoveZone.TurnArea;
                case CardLocation.DiscardPile: return CardMoveZone.DiscardPile;
                default: return CardMoveZone.None;
            }
        }

        private static bool IsOpen(CardMoveZone zone) =>
            zone == CardMoveZone.TurnArea || zone == CardMoveZone.DiscardPile;

        public bool Equals(CardMoveFact other) =>
            Definition.Equals(other.Definition) && From == other.From && To == other.To &&
            DepartsFacedown == other.DepartsFacedown && ArrivesFacedown == other.ArrivesFacedown &&
            KnownColor == other.KnownColor;

        public override bool Equals(object obj) => obj is CardMoveFact other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Definition.GetHashCode();
                hash = (hash * 397) ^ (int)From;
                hash = (hash * 397) ^ (int)To;
                hash = (hash * 397) ^ (DepartsFacedown ? 1 : 0);
                hash = (hash * 397) ^ (ArrivesFacedown ? 2 : 0);
                return (hash * 397) ^ (int)KnownColor;
            }
        }

        public override string ToString() => Definition + ":" + From + ">" + To;
    }

    /// <summary>Milestone 9.32g. Whether a swap's card text exchanged two cards or its chooser kept theirs.</summary>
    public enum CardSwapKind
    {
        None = 0,
        Swapped = 1,
        Kept = 2
    }

    /// <summary>
    /// Milestone 9.32g. A swap card text made - or declined - reported by the swap handlers on the
    /// terms <see cref="DiscardedCard"/> set at 9.19, so the seat-public log can say it happened
    /// (approved mockup F). The owner is the hero whose cards moved, never necessarily the actor: Set
    /// an Example's swap is the teammate's.
    /// </summary>
    public sealed class CardSwapFact : IEquatable<CardSwapFact>
    {
        private CardSwapFact(CardSwapKind kind, HeroId ownerHeroId, IEnumerable<CardMoveFact> moves)
        {
            if (!ownerHeroId.IsDefined)
            {
                throw new ArgumentException("A swap names the hero whose cards it moved.", nameof(ownerHeroId));
            }

            Kind = kind;
            OwnerHeroId = ownerHeroId;
            Moves = Array.AsReadOnly(moves.ToArray());
        }

        /// <summary>The outgoing card's move first, then the incoming card's.</summary>
        public static CardSwapFact Swapped(HeroId ownerHeroId, CardMoveFact outgoing, CardMoveFact incoming) =>
            new CardSwapFact(CardSwapKind.Swapped, ownerHeroId, new[] { outgoing, incoming });

        /// <summary>The chooser declined: the card stays where it was.</summary>
        public static CardSwapFact Kept(HeroId ownerHeroId, CardMoveFact kept) =>
            new CardSwapFact(CardSwapKind.Kept, ownerHeroId, new[] { kept });

        public CardSwapKind Kind { get; }

        public HeroId OwnerHeroId { get; }

        public IReadOnlyList<CardMoveFact> Moves { get; }

        public bool Equals(CardSwapFact other) =>
            other != null && Kind == other.Kind && OwnerHeroId.Equals(other.OwnerHeroId) &&
            Moves.SequenceEqual(other.Moves);

        public override bool Equals(object obj) => Equals(obj as CardSwapFact);

        public override int GetHashCode() => ((int)Kind * 397) ^ OwnerHeroId.GetHashCode();

        public override string ToString() => Kind + ":" + OwnerHeroId + ":" + string.Join(",", Moves);
    }

    /// <summary>
    /// Milestone 9.32g. A capability whose chooser may decline it (a May node) and whose decline is
    /// itself something the table sees - a teammate keeping the unresolved card Set an Example
    /// offered to swap. The resolver asks it once, as the decline is accepted, and publishes what it
    /// returns beside the node's declined outcome; the answer changes no state and no outcome.
    /// </summary>
    public interface IEffectDeclineReport
    {
        /// <summary>The fact the decline publishes, or null where it publishes none.</summary>
        CardSwapFact Declined(EffectCapabilityContext context);
    }

    /// <summary>
    /// Milestone 9.15. A minion that card text removed from the board (rulebook p.19's Remove, no
    /// coins), reported by the removing handler on the terms <see cref="DiscardedCard"/> set, so the
    /// seat-public log can say it left - until this milestone a card-text Removal (Ignatia's Abrupt
    /// Combustion) wrote no row at all, and the expected-outcome executor had to infer it from the
    /// scoreboard.
    /// </summary>
    public readonly struct RemovedMinion : IEquatable<RemovedMinion>
    {
        public RemovedMinion(MinionId minionId)
        {
            if (!minionId.IsDefined)
            {
                throw new ArgumentException("A removal names the minion it removed.");
            }

            MinionId = minionId;
        }

        public MinionId MinionId { get; }
        public bool IsDefined => MinionId.IsDefined;
        public bool Equals(RemovedMinion other) => MinionId.Equals(other.MinionId);
        public override bool Equals(object obj) => obj is RemovedMinion other && Equals(other);
        public override int GetHashCode() => MinionId.GetHashCode();
        public override string ToString() => MinionId.ToString();
    }

    public sealed class EffectExecutionResult
    {
        private EffectExecutionResult(
            EffectExecutionResultKind kind,
            IEnumerable<EffectOutcomeFact> outcomes,
            IEnumerable<TimingCheckpoint> checkpointsBefore,
            IEnumerable<TimingCheckpoint> checkpointsAfter,
            EffectChoiceRequest choice,
            IEnumerable<RuleObject> appliedPushes = null,
            IEnumerable<RevealedCard> revealedCards = null,
            HeroId markerGivenTo = default,
            IEnumerable<PlacedToken> placedTokens = null,
            IEnumerable<DiscardedCard> discardedCards = null,
            IEnumerable<RemovedMinion> removedMinions = null,
            IEnumerable<SpellbookChange> spellbookChanges = null,
            IEnumerable<CardSwapFact> cardSwaps = null)
        {
            // Milestone 9.32g. A swap is reported by the handler that made it; a KEPT card by the
            // decline that left it - the one fact a declined result may carry.
            var swaps = (cardSwaps ?? Enumerable.Empty<CardSwapFact>()).ToArray();
            if (swaps.Any(swap => swap == null) ||
                swaps.Any(swap => kind == EffectExecutionResultKind.Applied
                    ? swap.Kind != CardSwapKind.Swapped
                    : kind != EffectExecutionResultKind.DeclinedOptional || swap.Kind != CardSwapKind.Kept))
            {
                throw new ArgumentException(
                    "An applied result reports the swaps it made, and only a declined one the card it kept.",
                    nameof(cardSwaps));
            }

            CardSwaps = new ReadOnlyCollection<CardSwapFact>(swaps);
            var spellbook = (spellbookChanges ?? Enumerable.Empty<SpellbookChange>()).ToArray();
            if (spellbook.Any(change => !change.IsDefined))
            {
                throw new ArgumentException(
                    "A reported spellbook change names its kind and its book.", nameof(spellbookChanges));
            }

            if (spellbook.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a change it made to a spellbook.",
                    nameof(spellbookChanges));
            }

            SpellbookChanges = new ReadOnlyCollection<SpellbookChange>(spellbook);
            var placed = (placedTokens ?? Enumerable.Empty<PlacedToken>()).ToArray();
            if (placed.Any(token => token == null))
            {
                throw new ArgumentException(
                    "A reported placement names the token it placed.", nameof(placedTokens));
            }

            if (placed.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a token it placed.", nameof(placedTokens));
            }

            PlacedTokens = new ReadOnlyCollection<PlacedToken>(placed);
            var revealed = (revealedCards ?? Enumerable.Empty<RevealedCard>()).ToArray();
            if (revealed.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a card it revealed.",
                    nameof(revealedCards));
            }

            RevealedCards = new ReadOnlyCollection<RevealedCard>(revealed);
            var discarded = (discardedCards ?? Enumerable.Empty<DiscardedCard>()).ToArray();
            if (discarded.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a card it discarded.",
                    nameof(discardedCards));
            }

            DiscardedCards = new ReadOnlyCollection<DiscardedCard>(discarded);
            var removed = (removedMinions ?? Enumerable.Empty<RemovedMinion>()).ToArray();
            if (removed.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a minion it removed.",
                    nameof(removedMinions));
            }

            RemovedMinions = new ReadOnlyCollection<RemovedMinion>(removed);
            if (markerGivenTo.IsDefined && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a marker it gave.", nameof(markerGivenTo));
            }

            MarkerGivenTo = markerGivenTo;
            var pushed = (appliedPushes ?? Enumerable.Empty<RuleObject>()).ToArray();
            if (pushed.Any(subject => !subject.IsDefined))
            {
                throw new ArgumentException(
                    "An applied Push names the object it displaced.", nameof(appliedPushes));
            }

            if (pushed.Length > 0 && kind != EffectExecutionResultKind.Applied)
            {
                throw new ArgumentException(
                    "Only a handler that applied can report a displacement it performed.",
                    nameof(appliedPushes));
            }

            AppliedPushes = new ReadOnlyCollection<RuleObject>(pushed);
            if (kind == EffectExecutionResultKind.None)
            {
                throw new ArgumentException("An effect execution result requires a kind.", nameof(kind));
            }

            var facts = (outcomes ?? Enumerable.Empty<EffectOutcomeFact>()).ToArray();
            if (facts.Select(fact => fact.OutputName)
                .Distinct(StringComparer.Ordinal).Count() != facts.Length)
            {
                throw new ArgumentException("Effect outcome names must be unique.", nameof(outcomes));
            }

            var before = (checkpointsBefore ?? Enumerable.Empty<TimingCheckpoint>()).ToArray();
            var after = (checkpointsAfter ?? Enumerable.Empty<TimingCheckpoint>()).ToArray();
            if (before.Concat(after).Any(checkpoint => checkpoint == TimingCheckpoint.None))
            {
                throw new ArgumentException("A declared timing checkpoint must be defined.");
            }

            if ((kind == EffectExecutionResultKind.NeedsChoice) != (choice != null))
            {
                throw new ArgumentException(
                    "A NeedsChoice result carries exactly one choice request, and no other kind carries one.");
            }

            Kind = kind;
            Outcomes = new ReadOnlyCollection<EffectOutcomeFact>(facts);
            CheckpointsBefore = new ReadOnlyCollection<TimingCheckpoint>(before);
            CheckpointsAfter = new ReadOnlyCollection<TimingCheckpoint>(after);
            Choice = choice;
        }

        public EffectExecutionResultKind Kind { get; }
        public IReadOnlyList<EffectOutcomeFact> Outcomes { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsBefore { get; }
        public IReadOnlyList<TimingCheckpoint> CheckpointsAfter { get; }
        public EffectChoiceRequest Choice { get; }

        /// <summary>
        /// Milestone 8.19c1. The objects this handler actually displaced by one of rulebook p.18's
        /// Pushes, in the order it committed them; empty for every handler that pushed nothing.
        ///
        /// <para>It is a typed report rather than an <see cref="EffectOutcomeFact"/> because it is not
        /// something a later node binds to. p.18's own facts - how far, how many, which hero an obstacle
        /// stopped - are published as outcomes because card text reads them; this list is read by the
        /// resolver so it can hand the moment to whatever declared an <c>after-push-trigger@1</c>, and
        /// it is uncapped, whereas an outcome name can only ever hold one value. It crosses no
        /// snapshot: the pause a trigger creates names the one object it is running for.</para>
        /// </summary>
        public IReadOnlyList<RuleObject> AppliedPushes { get; }

        /// <summary>
        /// Milestone 8.40f. The cards this handler turned face up for the whole table, and empty for
        /// every handler that revealed nothing. See <see cref="RevealedCard"/>.
        /// </summary>
        public IReadOnlyList<RevealedCard> RevealedCards { get; }

        /// <summary>
        /// Milestone 9.19. The cards this handler sent from a hand to a discard pile by card text,
        /// and empty for every handler that discarded nothing. See <see cref="DiscardedCard"/>.
        /// </summary>
        public IReadOnlyList<DiscardedCard> DiscardedCards { get; }

        /// <summary>
        /// Milestone 9.15. The minions this handler removed from the board by card text, and empty
        /// for every handler that removed none. See <see cref="RemovedMinion"/>.
        /// </summary>
        public IReadOnlyList<RemovedMinion> RemovedMinions { get; }

        /// <summary>
        /// Milestone 9.30h. The changes this handler made to a spellbook - a cast, a faceup removal, a
        /// refill - and empty for every handler that changed none. See <see cref="SpellbookChange"/>.
        /// </summary>
        public IReadOnlyList<SpellbookChange> SpellbookChanges { get; }

        /// <summary>
        /// Milestone 9.32g. The swaps this handler made (or, on a decline, the card its chooser kept),
        /// and empty for every handler that swapped nothing. See <see cref="CardSwapFact"/>.
        /// </summary>
        public IReadOnlyList<CardSwapFact> CardSwaps { get; }

        /// <summary>
        /// Milestone 8.40h. The hero this handler GAVE a marker to, or the undefined id for every
        /// handler that gave none - which is every handler in this build but the giving one.
        ///
        /// <para>It is a typed report rather than an <see cref="EffectOutcomeFact"/> for the reason
        /// <see cref="AppliedPushes"/> is: card text already reads the recipient through the give's
        /// published <c>holder</c> output, and this is read by the resolver so it can hand the moment
        /// to whatever declared <see cref="TimingCheckpoint.MarkerGivenToHero"/>. The two are not the
        /// same fact, and keeping them apart is what makes the exclusions structural - a TRANSFER
        /// publishes <c>holder</c> too and reports nothing here, so a trigger keyed on this cannot see
        /// it however a face composes it.</para>
        ///
        /// <para>One hero rather than a list, because one give gives one marker to one hero. p.18's
        /// batch push has no counterpart here: nothing in the rules gives several markers at once, and
        /// a node re-executed inside its own resolution reports nothing at all, which is what bounds
        /// the moment to once per give.</para>
        /// </summary>
        public HeroId MarkerGivenTo { get; }

        /// <summary>
        /// Milestone 8.112f. The tokens this handler PLACED (rulebook p.18's Place), in the order it
        /// committed them, and empty for every handler that placed none - the moment Mrak's Rock and a
        /// Hard Place watches, "after you place one or more Rock tokens".
        ///
        /// <para>A typed report rather than an outcome fact for the reason <see cref="AppliedPushes"/>
        /// is: nothing binds it, and the resolver reads it so it can hand the moment to whatever
        /// declared an after-tokens-placed trigger. A push or a move of a standing token is not a
        /// Place and never reports here; p.18's short-supply reclaim is a removal, and the placement
        /// after it is what reports.</para>
        /// </summary>
        public IReadOnlyList<PlacedToken> PlacedTokens { get; }

        public static EffectExecutionResult Applied(
            IEnumerable<EffectOutcomeFact> outcomes = null,
            IEnumerable<TimingCheckpoint> checkpointsBefore = null,
            IEnumerable<TimingCheckpoint> checkpointsAfter = null,
            IEnumerable<RuleObject> appliedPushes = null,
            IEnumerable<RevealedCard> revealedCards = null,
            HeroId markerGivenTo = default,
            IEnumerable<PlacedToken> placedTokens = null,
            IEnumerable<DiscardedCard> discardedCards = null,
            IEnumerable<RemovedMinion> removedMinions = null,
            IEnumerable<SpellbookChange> spellbookChanges = null,
            IEnumerable<CardSwapFact> cardSwaps = null) =>
            new EffectExecutionResult(
                EffectExecutionResultKind.Applied, outcomes, checkpointsBefore, checkpointsAfter, null,
                appliedPushes, revealedCards, markerGivenTo, placedTokens, discardedCards,
                removedMinions, spellbookChanges, cardSwaps);

        public static EffectExecutionResult NeedsChoice(EffectChoiceRequest choice) =>
            new EffectExecutionResult(
                EffectExecutionResultKind.NeedsChoice, null, null, null,
                choice ?? throw new ArgumentNullException(nameof(choice)));

        public static EffectExecutionResult ImpossibleMandatory() =>
            new EffectExecutionResult(
                EffectExecutionResultKind.ImpossibleMandatory, null, null, null, null);

        public static EffectExecutionResult DeclinedOptional() =>
            new EffectExecutionResult(
                EffectExecutionResultKind.DeclinedOptional, null, null, null, null);

        /// <summary>Milestone 9.32g. A decline that publishes the card its chooser kept.</summary>
        public static EffectExecutionResult DeclinedOptional(IEnumerable<CardSwapFact> keptCards) =>
            new EffectExecutionResult(
                EffectExecutionResultKind.DeclinedOptional, null, null, null, null,
                cardSwaps: keptCards);
    }

    /// <summary>
    /// A handler's request for input: who decides, and the ordered candidate values they choose among.
    /// The frame turns this into the authoritative pending choice; the handler never holds it.
    /// </summary>
    public sealed class EffectChoiceRequest
    {
        public EffectChoiceRequest(
            EffectChooserRole chooser,
            IEnumerable<EffectValue> candidates,
            int minimumSelections,
            int maximumSelections,
            HeroId chooserHeroId = default,
            Rules.EffectResolution.EffectChoiceValueVocabulary valueVocabulary =
                Rules.EffectResolution.EffectChoiceValueVocabulary.None)
        {
            if (chooser == EffectChooserRole.None && !chooserHeroId.IsDefined)
            {
                throw new ArgumentException("An effect choice requires a chooser.", nameof(chooser));
            }

            var options = (candidates ?? throw new ArgumentNullException(nameof(candidates))).ToArray();
            if (options.Any(candidate => !candidate.IsDefined))
            {
                throw new ArgumentException("Effect choice candidates must be defined.", nameof(candidates));
            }

            if (options.Distinct().Count() != options.Length)
            {
                throw new ArgumentException("Effect choice candidates must be unique.", nameof(candidates));
            }

            if (minimumSelections < 0 || maximumSelections < minimumSelections)
            {
                throw new ArgumentException("Effect choice bounds must be a non-negative range.");
            }

            if (maximumSelections > options.Length)
            {
                throw new ArgumentException("An effect choice cannot allow more selections than candidates.");
            }

            Chooser = chooser;
            ChooserHeroId = chooserHeroId;
            ValueVocabulary = valueVocabulary;
            Candidates = new ReadOnlyCollection<EffectValue>(options);
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
        }

        public EffectChooserRole Chooser { get; }

        /// <summary>
        /// The exact hero whose decision this is, when the handler already knows. Milestone 7.1a: a
        /// capability told which hero must discard has that hero in a typed input, so naming them is
        /// reporting a fact rather than re-deriving one. It takes precedence over
        /// <see cref="Chooser"/>, which stays the definition-level way to say the same thing when the
        /// node rather than the handler decides.
        ///
        /// This is what keeps "another hero discards" out of the resolution-scoped opposing-hero seam,
        /// which cannot express it: with three opponents on a side there is no single opposing hero,
        /// and the handler's own input is the only place the answer actually exists.
        /// </summary>
        public HeroId ChooserHeroId { get; }
        public IReadOnlyList<EffectValue> Candidates { get; }
        public int MinimumSelections { get; }
        public int MaximumSelections { get; }

        /// <summary>
        /// Milestone 8.37. What this request's integer candidates mean, when they mean something
        /// other than a count - see <see cref="Rules.EffectResolution.EffectChoiceValueVocabulary"/>.
        /// The handler is the only thing that knows, which is why it is declared here and not
        /// inferred downstream.
        /// </summary>
        public Rules.EffectResolution.EffectChoiceValueVocabulary ValueVocabulary { get; }
    }

    /// <summary>
    /// A pure rules executor for one capability revision. Stateless: everything it needs arrives in the
    /// context, and everything it decides leaves in the result. <see cref="Resume"/> is separate from
    /// <see cref="Execute"/> so the frame, not the handler, owns whether this is a resumption.
    /// </summary>
    public interface IEffectCapabilityHandler
    {
        EffectCapabilityReference Reference { get; }

        EffectCapabilityParameterSchema Schema { get; }

        /// <summary>
        /// Every Active-effect definition this capability can create. These are the trusted semantics a
        /// restore rebinds an instance to, so a snapshot never has to carry them.
        /// </summary>
        IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; }

        EffectExecutionResult Execute(EffectCapabilityContext context);

        EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected);

        /// <summary>
        /// Which of the four results <see cref="Execute"/> would report in this context, answered
        /// without performing anything.
        ///
        /// Milestone 6.5ac asks this so a control node can refuse an option the actor cannot perform
        /// instead of spending a card on it. Probing by calling <see cref="Execute"/> is exactly what
        /// this member exists to avoid: a handler may apply rather than pause - marking an actor,
        /// creating an Active effect, returning a marker, pushing every adjacent object at a fixed
        /// distance - so asking that way would perform the effect it was asked about.
        ///
        /// The answer is the candidate derivation the handler already owns, without the commit that
        /// follows it. It reports the kind alone rather than an <see cref="EffectExecutionResult"/>,
        /// because a result carries published facts and declared checkpoints, and a probe has neither:
        /// it is a reading of the state, not a step in the resolution. Nothing here may mutate board,
        /// card, effect, component, supply, timing, or identity state, and nothing may mint.
        /// </summary>
        EffectExecutionResultKind Anticipate(EffectCapabilityContext context);
    }

    /// <summary>
    /// Milestone 6.8l: what a capability must add to accept a consumer.
    ///
    /// A selection that declares <see cref="EffectSelectionDeclaration.ConsumerParameterName"/> is asked
    /// to offer only the objects that consumer can act on, and both the feasibility walk and the live
    /// pause therefore need the set it <em>would</em> offer. <see cref="IEffectCapabilityHandler.Anticipate"/>
    /// cannot supply it: it answers with a kind, deliberately, because a result carries facts and
    /// checkpoints a probe has neither of. Running <see cref="IEffectCapabilityHandler.Execute"/> to
    /// find out is what milestone 6.5ac's whole contract exists to avoid.
    ///
    /// So the candidate derivation the handler already owns is declared separately, under the same rule
    /// the probe obeys: nothing here may mutate board, card, effect, component, supply, timing, or
    /// identity state, and nothing may mint. The catalog refuses to register a handler that declares the
    /// parameter without implementing this, so a selection can never accept a consumer it has no way to
    /// answer for.
    /// </summary>
    public interface IPlacedTokenCapabilityHandler
    {
        /// <summary>
        /// The token definition this capability puts on the board. Milestone 8.61j.
        ///
        /// <para><b>Why it is asked at all.</b> A hero's triggered bodies are named BY REFERENCE and
        /// carry no card id, so every walk that has to attribute one to a card follows the references
        /// a card can reach: <c>UltimateRule.TriggeredEffect</c> for a p.16 permanent ability, and an
        /// Active effect's own <c>TriggeredEffect</c> for an "End of turn:" clause. Min the
        /// Dragonmonk's mine trap is a THIRD route and the first of its kind - the price a blast
        /// charges is declared by the TOKEN DEFINITION
        /// (<see cref="Components.TokenDefinition.MoveThroughTrapConsequences"/>), and the card that
        /// can reach it is the card that PLACES that token. Without this declaration the four
        /// consequence bodies are owned by a hero and reachable from none of her cards, which is
        /// exactly the state <c>DecisionLabelCoverageTests</c> exists to refuse.</para>
        ///
        /// <para>It is a separate interface rather than a member on
        /// <see cref="IEffectCapabilityHandler"/>, on the same terms
        /// <see cref="IEffectSelectionCapabilityHandler"/> is: one handler in the build answers it and
        /// every other one has nothing to say, so asking all of them would be asking a question that
        /// is not about them. It is READ-ONLY and mutates nothing.</para>
        /// </summary>
        Components.TokenDefinition PlacedToken { get; }
    }

    public interface IEffectSelectionCapabilityHandler
    {
        /// <summary>
        /// The candidate values <see cref="IEffectCapabilityHandler.Execute"/> would offer here, derived
        /// without opening the choice. Empty means the selection has nothing to name, which is the same
        /// state its probe reports as <see cref="EffectExecutionResultKind.ImpossibleMandatory"/>.
        /// </summary>
        IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context);

        /// <summary>
        /// Milestone 8.35l: how many of those candidates a complete answer names - the same floor
        /// <see cref="EffectChoiceRequest.MinimumSelections"/> would carry.
        ///
        /// It is asked because narrowing a candidate list is only half of judging a selection. A
        /// selection that keeps one eligible candidate out of two has narrowed successfully and is
        /// still impossible when its floor is two, and reporting it as answerable there is what let
        /// Sabina's Blue swap trio be offered on a board holding exactly one legal minion - the offer
        /// stood, the player took it, and the live pause refused at the floor with the card already
        /// spent. The floor is the handler's own, for the same reason the candidates are: only the
        /// handler knows what shape of answer it would ask for.
        /// </summary>
        int AnticipateMinimumSelections(EffectCapabilityContext context);
    }

    /// <summary>
    /// Milestone 8.35l: what a node would publish, answered without performing it.
    ///
    /// Two questions in this build need it, and they are the same question. A selection declaring a
    /// consumer has to put a candidate to that consumer under the parameter the consumer actually
    /// binds - Sabina's Back to Back selects a minion and swaps with its <em>space</em>, so handing the
    /// consumer the bare minion asked it about a value of the wrong kind and every candidate came back
    /// refused, which is why the declaration had to be left off and the narrowing was lost. And the
    /// feasibility walk cannot judge a node whose subject arrives from an earlier one, so a Place
    /// standing behind <c>actor-space@1</c> was never judged at all and Tigerclaw's Blend Into Shadows
    /// was offered, clicked, and silently consumed with no prompt ever drawn.
    ///
    /// Implementing this is a claim about the handler, not a convenience: deriving these facts must
    /// mutate no board, card, effect, component, supply, timing, or identity state and must mint
    /// nothing - the same rule <see cref="IEffectCapabilityHandler.Anticipate"/> obeys - because both
    /// callers ask it while merely reading what an offer would lead to. A handler that cannot answer
    /// under that rule simply does not implement this, and the two callers keep milestone 6.5ac's
    /// conservative position: unreadable is offered.
    /// </summary>
    public interface IEffectAnticipatedOutcomeCapabilityHandler
    {
        /// <summary>
        /// Whether executing this node changes nothing a player could see - it publishes its outputs
        /// and does no more.
        ///
        /// <para>The feasibility walk needs this said out loud, because it short-circuits at the first
        /// node that can apply and a node that only publishes a fact was answering "yes" to a question
        /// about whether the action <em>does</em> anything. <c>actor-space@1</c> is the case that
        /// mattered: Tigerclaw's Blend Into Shadows reads the actor's own coordinate and then places
        /// him, and reading the coordinate was enough to keep the card offered when the Place standing
        /// behind it was barred - so the card was clicked, no prompt was ever drawn, and it resolved
        /// having done nothing. A selection answers <see cref="EffectExecutionResultKind.NeedsChoice"/>
        /// rather than <see cref="EffectExecutionResultKind.Applied"/>, so declaring this true there
        /// says the honest thing about selecting and changes nothing about whether a prompt is worth
        /// offering.</para>
        /// </summary>
        bool PublishesWithoutApplying { get; }

        /// <summary>
        /// The outputs <see cref="IEffectCapabilityHandler.Execute"/> (or, for a selection,
        /// <see cref="IEffectCapabilityHandler.Resume"/>) would publish for this answer, or nothing
        /// where this answer would publish nothing.
        ///
        /// <paramref name="chosen"/> is empty for a node that decides nothing, and carries a complete
        /// prospective answer - <see cref="IEffectSelectionCapabilityHandler.AnticipateMinimumSelections"/>
        /// values of it - for a selection.
        /// </summary>
        IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
            EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen);
    }

    /// <summary>
    /// Milestone 8.35l: a capability that can say which standing rule refused it.
    ///
    /// <para>The refusal itself is already the handler's - <c>Anticipate</c> reports it - but a kind is
    /// not an explanation, and a player told only that an action is unavailable learns nothing about
    /// another hero's card text standing three spaces away. This is the seam that carries the answer
    /// out: the handler re-asks the same restriction query it refused on and hands back the applied
    /// modification, which already names its controlling hero and its source card.</para>
    ///
    /// <para>It is deliberately the handler's job and not the offer's. A surface that worked out why an
    /// action is unavailable would be a second reading of the rules, free to disagree with the first;
    /// this way the greyed control, the reason on it, and the refusal a start would have produced are
    /// all one answer. Nothing here may mutate or mint - it is asked while merely reading an offer.</para>
    /// </summary>
    public interface IEffectRefusalExplainingCapabilityHandler
    {
        /// <summary>
        /// The standing modification that bars this node in this context, or nothing where the node is
        /// refused for some other reason - which is not a fault, and leaves the offer greyed with
        /// nothing said rather than with something invented.
        /// </summary>
        bool TryExplainRefusal(
            EffectCapabilityContext context, out AppliedRuleModification blocking);
    }

    /// <summary>
    /// Milestone 8.35z42: a capability that puts the ACTING HERO somewhere, and can say where
    /// he could end up without moving him.
    ///
    /// <para>It exists for one shape, and the shape is roster-wide: a card that prints a bounded move
    /// and then asks a question measured from the space he is in. "You may move 1 space. Target a unit
    /// adjacent to you." is two ordered steps and one offer question, and the offer question stands
    /// before either of them - so the second step reads a board the first has not produced. Milestone
    /// 8.35p answered that by refusing to read it at all, which is right whenever the destinations are
    /// unknowable and wrong whenever they are not: this board is hexes, every printed budget is one or
    /// two spaces, and the set he could be standing on is small enough to enumerate and ask the same
    /// question of, once each.</para>
    ///
    /// <para>Implementing it is a claim, exactly as
    /// <see cref="IEffectAnticipatedOutcomeCapabilityHandler"/> is: deriving these spaces must move
    /// nothing and mint nothing, and the list must be the destinations this node would really offer,
    /// because a walk that trusted a wider list would grey an action that has an answer. A handler that
    /// cannot answer under that rule does not implement this, and the walk keeps milestone 8.35p's
    /// position - unreadable is offered.</para>
    /// </summary>
    public interface IEffectActorRelocationCapabilityHandler
    {
        /// <summary>
        /// Every space the acting hero could be standing on once this node has been answered - his
        /// current space included wherever the step can be declined, because declining is an answer.
        ///
        /// <para><c>false</c> where this node does not move the actor at all, or where the walk cannot
        /// yet tell: an unresolved subject, a destination bound to a decision nobody has made, a
        /// board this context cannot read. Both are the same instruction to the caller - do not judge
        /// what stands after this - and neither is a fault.</para>
        /// </summary>
        bool TryAnticipateActorSpaces(
            EffectCapabilityContext context, out IReadOnlyList<HexCoordinate> spaces);
    }

    /// <summary>
    /// A capability that consumes exact immunity-override declarations. Content load rejects a node
    /// declaring an activity this handler does not ask.
    /// </summary>
    public interface IEffectImmunityOverrideCapabilityHandler
    {
        IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; }
    }
}
