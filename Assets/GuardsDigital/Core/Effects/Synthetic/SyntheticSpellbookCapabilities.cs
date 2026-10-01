using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 9.30c. Casting a spell from a spellbook, and refilling the book - Prepare Spells:
    /// "Add all spell cards to the spellbook. Only Gydion can look at the cards in the spellbook. As
    /// the spell is cast, remove it from the spellbook faceup. The action on the spell card is
    /// performed by the hero casting the spell."
    ///
    /// <para><b>Three contracts.</b></para>
    /// <list type="bullet">
    /// <item><see cref="SpellInBookHandler"/> - one declaration per spell, its definition bound when
    /// the content registers it (as <c>SyntheticOwnedTokenCapabilities</c> binds a token definition):
    /// it publishes the book owner's one instance of that spell while it lies in the book, and is
    /// impossible otherwise. Card text names a spell, never a card, so no facedown identity is a
    /// candidate of any decision (GY-05: "choose the spells by name, without looking at them").</item>
    /// <item><see cref="CastSpellV1"/> - the cast. It moves the named spell book -> removed, "as the
    /// spell is cast", as its own committed transition, and publishes the spell as the card whose
    /// printed primary the caster now performs. <c>BorrowedActionQuery.Requested</c> reads that, and
    /// the card-action dispatcher starts the primary through the borrow path (8.35z50): the caster is
    /// the actor, the values, Range, text and supertype are the spell's, and the items are the
    /// caster's. The borrow contract's own "the borrowed card does not change state" still holds,
    /// because the move is finished before the borrow begins.</item>
    /// <item><see cref="RefillSpellbookV1"/> - "Add all spell cards to the spellbook": every spell the
    /// actor has removed returns facedown, and each returned spell's Active effects are cancelled by
    /// p.17's state change ("Card changes state - becomes facedown", designer, 2026-08-22).</item>
    /// </list>
    ///
    /// <para><b>The book's owner and the caster are separate.</b> The owner is an input of the naming
    /// step; the caster is the frame's actor, and the cast asks
    /// <see cref="BorrowedPrimaryActionQuery.EligibleCards"/> with the caster as the borrower, so the
    /// borrow's ownership filter decides who may cast from whose book. NebKher (GY-05) changes that
    /// filter for <see cref="BorrowedActionPool.OwnedSpellbookSpells"/> and nothing here.</para>
    ///
    /// <para><b>Deferred, not registered</b>, as every contract is until the hero whose dealt faces
    /// start it: the registered set is serialized into every snapshot, and no dealt card casts yet.
    /// Gydion's dealing (9.30i) registers it.</para>
    /// </summary>
    public static class SyntheticSpellbookCapabilities
    {
        public static EffectCapabilityReference CastSpellV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("cast-spell"), 1);

        public static EffectCapabilityReference RefillSpellbookV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("refill-spellbook"), 1);

        /// <summary>The hero whose book the named spell lies in.</summary>
        public const string OwnerParameter = "owner";

        /// <summary>The spell the naming step found, published for the cast to bind.</summary>
        public const string SpellOutput = "spell";

        /// <summary>The cast's input: the spell instance the naming step published.</summary>
        public const string SpellParameter = "spell";

        /// <summary>How many spells a refill returned to the book.</summary>
        public const string ReturnedOutput = "returned";

        /// <summary>
        /// Milestone 9.30e. "for every N other spell cards removed from the spellbook" - the count of
        /// other spells the acting spell's book has removed, divided by N and rounded down: Midas'
        /// Touch's "Gain 1 coin for every two" binds N = 2. See <see cref="CountOtherRemovedSpellsV1"/>.
        /// </summary>
        public static EffectCapabilityReference CountOtherRemovedSpellsV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("count-other-removed-spells"), 1);

        /// <summary>
        /// Milestone 9.30e. Find Familiar's "You may remove up to three other spell cards from the spell
        /// book faceup": the named spell moves book -> removed faceup by 9.30b's transition, and nothing
        /// is cast - no action is performed off it.
        /// </summary>
        public static EffectCapabilityReference RemoveSpellFaceupV1 =>
            new EffectCapabilityReference(new EffectCapabilityId("remove-spell-faceup"), 1);

        /// <summary>The count's divisor: "for every <c>N</c>"; 1 is "for each".</summary>
        public const string EveryParameter = "every";

        /// <summary>The rounded-down count the counting step publishes.</summary>
        public const string CountOutput = "count";

        /// <summary>
        /// The cast, the refill, the count and the removal; each spell's naming step is declared by its
        /// content.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new CastSpellHandler(), new RefillSpellbookHandler(),
                new CountOtherRemovedSpellsHandler(), new RemoveSpellFaceupHandler()
            };

        /// <summary>Nothing until a dealt face casts (9.30i).</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// Milestone 9.30h. THE naming step of <paramref name="spell"/>: the spell's own id and
        /// <c>-in-book</c>, version 1. One rule, stated once, so a reader of a cast or removal arm -
        /// the decision window drawing the spell's face - can tell which spell an arm names from the
        /// public effect graph without loading a handler. Gydion's spells (9.30e,
        /// <c>GydionCardContent.InBookV1</c>) and the synthetic spells name theirs by it.
        /// </summary>
        public static EffectCapabilityReference InBookV1(CardDefinitionId spell)
        {
            if (!spell.IsDefined) throw new ArgumentException("A naming step names its spell.", nameof(spell));
            return new EffectCapabilityReference(new EffectCapabilityId(spell.Value + "-in-book"), 1);
        }

        /// <summary>
        /// Milestone 9.30h. What a cast or removal arm does with the spell it names - see
        /// <see cref="TryReadSpellArm"/>.
        /// </summary>
        public enum SpellArmKind
        {
            None = 0,

            /// <summary>A <see cref="CastArm"/>: the spell is cast.</summary>
            Cast = 1,

            /// <summary>A <see cref="RemoveArm"/>: the spell is removed faceup without being cast.</summary>
            Remove = 2
        }

        /// <summary>
        /// Milestone 9.30h. The spell a <c>Choose</c> option names, read off the public effect graph
        /// that declares it: <paramref name="arm"/> is a <see cref="CastArm"/> or <see cref="RemoveArm"/>
        /// - a <c>Sequence</c> whose first step is a spell's naming step (<see cref="InBookV1"/>) and
        /// whose second casts or removes - and <paramref name="spell"/> is the spell definition among
        /// <paramref name="definitions"/> that step names.
        ///
        /// <para>It reads content, never state: whether the spell is in the book is the choice's own
        /// answer (an unavailable option), and no instance - facedown or not - is consulted, so the same
        /// reading is safe on every seat that is shown the choice, Hanu's delegated seat included.</para>
        /// </summary>
        public static bool TryReadSpellArm(
            IEnumerable<EffectDefinition> effects,
            EffectNodeId arm,
            IEnumerable<CardDefinition> definitions,
            out CardDefinition spell,
            out SpellArmKind kind)
        {
            spell = null;
            kind = SpellArmKind.None;
            if (effects == null || definitions == null || !arm.IsDefined)
            {
                return false;
            }

            foreach (var effect in effects.Where(value => value != null))
            {
                var nodes = effect.Nodes.ToDictionary(node => node.Id);
                if (!nodes.TryGetValue(arm, out var sequence) ||
                    sequence.Kind != EffectNodeKind.Sequence ||
                    sequence.ChildNodeIds.Count != 2 ||
                    !nodes.TryGetValue(sequence.ChildNodeIds[0], out var naming) ||
                    !nodes.TryGetValue(sequence.ChildNodeIds[1], out var act))
                {
                    continue;
                }

                var armKind = act.Capability.Equals(CastSpellV1)
                    ? SpellArmKind.Cast
                    : act.Capability.Equals(RemoveSpellFaceupV1)
                        ? SpellArmKind.Remove
                        : SpellArmKind.None;
                if (armKind == SpellArmKind.None || !naming.Capability.IsDefined)
                {
                    continue;
                }

                var named = definitions.FirstOrDefault(definition =>
                    definition != null &&
                    definition.Progression.IsSpell &&
                    InBookV1(definition.Id).Equals(naming.Capability));
                if (named == null)
                {
                    continue;
                }

                spell = named;
                kind = armKind;
                return true;
            }

            return false;
        }

        /// <summary>
        /// The naming step for one spell: <paramref name="spell"/>'s definition is the declaration's
        /// fixed parameter, and the hero whose book it reads is the node's <see cref="OwnerParameter"/>.
        /// </summary>
        public static IEffectCapabilityHandler SpellInBookHandler(
            EffectCapabilityReference reference, CardDefinitionId spell) =>
            new SpellInBook(reference, spell);

        /// <summary>Milestone 9.30g. Whether the named spell lies in the book, as a predicate publishes it.</summary>
        public const string PresentOutput = "present";

        /// <summary>
        /// Milestone 9.30g. "... cast the "Wish" spell in the spellbook instead" asks whether that one
        /// spell is in the book before it offers the replacement: a <c>Conditional</c>'s predicate,
        /// bound to <paramref name="spell"/> at registration as <see cref="SpellInBookHandler"/> is,
        /// publishing <see cref="PresentOutput"/> for the book of the node's <see cref="OwnerParameter"/>.
        /// It reads the same <see cref="SpellbookQuery.InBook"/> the naming step reads, so the offer
        /// and the cast it leads to cannot disagree. It changes nothing.
        /// </summary>
        public static IEffectCapabilityHandler SpellInBookPresentHandler(
            EffectCapabilityReference reference, CardDefinitionId spell) =>
            new SpellInBookPresent(reference, spell);

        /// <summary>
        /// One "cast" bullet: a mandatory <c>Sequence</c> <paramref name="armId"/> of the spell's naming
        /// step (<paramref name="spellInBook"/>, reading the actor's own book) and the cast. A "Choose and
        /// cast one" is a <c>Choose</c> over several of these; "Cast the X spell" is one of them as the
        /// root. Node ids are derived from the arm, so several arms share one definition.
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> CastArm(
            EffectNodeId armId, EffectCapabilityReference spellInBook)
        {
            if (!armId.IsDefined) throw new ArgumentException("A cast arm names its node.", nameof(armId));
            var name = new EffectNodeId(armId.Value + "-in-book");
            var cast = new EffectNodeId(armId.Value + "-cast");
            return Array.AsReadOnly(new[]
            {
                new EffectNodeDefinition(armId, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { name, cast }),
                new EffectNodeDefinition(name, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    spellInBook,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(OwnerParameter, EffectRoleKind.Actor, EffectValueKind.Hero)
                    },
                    outputs: new[] { new EffectOutputBinding(SpellOutput, EffectValueKind.CardInstance) }),
                new EffectNodeDefinition(cast, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    CastSpellV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SpellParameter, name, SpellOutput, EffectValueKind.CardInstance)
                    },
                    outputs: new[]
                    {
                        new EffectOutputBinding(
                            SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValueKind.CardInstance)
                    })
            });
        }

        /// <summary>
        /// Milestone 9.30e. One "remove this spell faceup" option: a mandatory <c>Sequence</c>
        /// <paramref name="armId"/> of the spell's naming step (<paramref name="spellInBook"/>, reading
        /// the actor's own book) and <see cref="RemoveSpellFaceupV1"/>. Find Familiar's "remove up to
        /// three other spell cards" is a <c>Choose</c> over one of these per other spell, so the decision
        /// names spells, never book cards (GY-05), and an option whose spell is already removed is greyed
        /// by its naming step exactly as a cast bullet is.
        /// </summary>
        public static IReadOnlyList<EffectNodeDefinition> RemoveArm(
            EffectNodeId armId, EffectCapabilityReference spellInBook)
        {
            if (!armId.IsDefined) throw new ArgumentException("A removal arm names its node.", nameof(armId));
            var name = new EffectNodeId(armId.Value + "-in-book");
            var remove = new EffectNodeId(armId.Value + "-remove");
            return Array.AsReadOnly(new[]
            {
                new EffectNodeDefinition(armId, EffectNodeKind.Sequence, EffectFailurePolicy.Mandatory,
                    childNodeIds: new[] { name, remove }),
                new EffectNodeDefinition(name, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    spellInBook,
                    inputs: new[]
                    {
                        EffectInputBinding.FromRole(OwnerParameter, EffectRoleKind.Actor, EffectValueKind.Hero)
                    },
                    outputs: new[] { new EffectOutputBinding(SpellOutput, EffectValueKind.CardInstance) }),
                new EffectNodeDefinition(remove, EffectNodeKind.Capability, EffectFailurePolicy.Mandatory,
                    RemoveSpellFaceupV1,
                    inputs: new[]
                    {
                        EffectInputBinding.FromNodeOutput(
                            SpellParameter, name, SpellOutput, EffectValueKind.CardInstance)
                    })
            });
        }

        /// <summary>
        /// Milestone 9.30e. "for every N other spell cards removed from the spellbook": the count is
        /// <see cref="SpellbookQuery.OtherRemoved"/>'s - the acting card is the cast spell (9.30c stamps
        /// a cast's frame with the spell) - read when this step is asked, divided by
        /// <see cref="EveryParameter"/> and rounded down.
        ///
        /// <para><b>It decides nothing and applies nothing</b>: it publishes a number the next step
        /// binds, so the feasibility walk carries it forward (8.35l) and the gain it feeds is judged on
        /// the very amount the resolution would gain (lesson 27). A count of 0 is a count, not a
        /// failure: whether gaining 0 does anything is the consuming step's question.</para>
        /// </summary>
        private sealed class CountOtherRemovedSpellsHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => CountOtherRemovedSpellsV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(EveryParameter, EffectValueKind.Integer) },
                new[] { new EffectOutputBinding(CountOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public bool PublishesWithoutApplying => true;

            /// <summary>The one derivation every member asks, or null where it cannot be asked.</summary>
            private static IReadOnlyList<EffectOutcomeFact> Counted(EffectCapabilityContext context)
            {
                var every = context.Input(EveryParameter).IntegerValue;
                if (!context.CardState.IsAvailable || every < 1)
                {
                    return null;
                }

                var others = SpellbookQuery.OtherRemoved(
                    context.CardState.Instances, context.SourceCardInstanceId);
                // Milestone 9.30h. The one rounding the spell choice's caption reads too.
                return new[]
                {
                    new EffectOutcomeFact(CountOutput, EffectValue.Integer(SpellCountReading.PerEvery(others, every)))
                };
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var counted = Counted(context);
                return counted == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(counted);
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Counted(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Counted(context) ?? Array.Empty<EffectOutcomeFact>();
        }

        /// <summary>
        /// Milestone 9.30e. Find Familiar's removal: the named spell, while it lies in the ACTOR's book,
        /// moves book -> removed faceup through 9.30b's
        /// <see cref="CardLifecycleAuthority.TryRemoveFromSpellbookFaceup"/>. It is not a cast: no
        /// borrowed card is published, so no action is performed off the spell.
        /// </summary>
        private sealed class RemoveSpellFaceupHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RemoveSpellFaceupV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(SpellParameter, EffectValueKind.CardInstance) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>The named spell, if it lies in the actor's own book; null otherwise.</summary>
            private static CardInstance Removable(EffectCapabilityContext context)
            {
                var named = context.Input(SpellParameter);
                if (!context.CardState.IsAvailable || named.Kind != EffectValueKind.CardInstance ||
                    !context.ActorId.IsDefined)
                {
                    return null;
                }

                return context.CardState.Instances.FirstOrDefault(card =>
                    card != null &&
                    card.Id.Equals(named.CardInstanceId) &&
                    card.OwnerId.Equals(context.ActorId) &&
                    card.State.Location == CardLocation.Spellbook);
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var spell = Removable(context);
                return spell == null || !CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(spell)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(
                        Array.Empty<EffectOutcomeFact>(),
                        // Milestone 9.30h. Reported so the seat-public log can name the removal.
                        spellbookChanges: new[] { SpellbookChange.RemovedFaceup(spell.OwnerId, spell.Id) });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Removable(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        private sealed class SpellInBook : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly CardDefinitionId spell;

            public SpellInBook(EffectCapabilityReference reference, CardDefinitionId spell)
            {
                if (!reference.IsDefined)
                    throw new ArgumentException("A spell's naming step needs a reference.", nameof(reference));
                if (!spell.IsDefined)
                    throw new ArgumentException("A spell's naming step names its spell.", nameof(spell));
                Reference = reference;
                this.spell = spell;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(OwnerParameter, EffectValueKind.Hero) },
                new[] { new EffectOutputBinding(SpellOutput, EffectValueKind.CardInstance) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Naming a spell changes nothing: it is the cast after it that does, so the feasibility
            /// walk carries the spell forward and judges the cast (8.35l).
            /// </summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>
            /// The one derivation every member asks: the owner's instance of this spell, while it lies in
            /// the book. Absent - removed, never owned, no owner bound - is an impossible mandatory step,
            /// so the bullet that names it is greyed (8.35z35's refused tier).
            /// </summary>
            private CardInstance Found(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameter);
                return !context.CardState.IsAvailable || owner.Kind != EffectValueKind.Hero
                    ? null
                    : SpellbookQuery.InBook(context.CardState.Instances, owner.HeroId, spell);
            }

            private static IReadOnlyList<EffectOutcomeFact> Published(CardInstance found) =>
                new[] { new EffectOutcomeFact(SpellOutput, EffectValue.Card(found.Id)) };

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var found = Found(context);
                return found == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(Published(found));
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Found(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var found = Found(context);
                return found == null ? Array.Empty<EffectOutcomeFact>() : Published(found);
            }
        }

        /// <summary>Milestone 9.30g. See <see cref="SpellInBookPresentHandler"/>.</summary>
        private sealed class SpellInBookPresent
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly CardDefinitionId spell;

            public SpellInBookPresent(EffectCapabilityReference reference, CardDefinitionId spell)
            {
                if (!reference.IsDefined)
                    throw new ArgumentException("A spell's presence predicate needs a reference.", nameof(reference));
                if (!spell.IsDefined)
                    throw new ArgumentException("A spell's presence predicate names its spell.", nameof(spell));
                Reference = reference;
                this.spell = spell;
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(OwnerParameter, EffectValueKind.Hero) },
                new[] { new EffectOutputBinding(PresentOutput, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>A predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            private IReadOnlyList<EffectOutcomeFact> Read(EffectCapabilityContext context)
            {
                var owner = context.Input(OwnerParameter);
                if (!context.CardState.IsAvailable || owner.Kind != EffectValueKind.Hero)
                {
                    return null;
                }

                var present = SpellbookQuery.InBook(context.CardState.Instances, owner.HeroId, spell) != null;
                return new[] { new EffectOutcomeFact(PresentOutput, EffectValue.Boolean(present)) };
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var read = Read(context);
                return read == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(read);
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Read(context) ?? Array.Empty<EffectOutcomeFact>();
        }

        private sealed class CastSpellHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => CastSpellV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter(SpellParameter, EffectValueKind.CardInstance) },
                new[]
                {
                    new EffectOutputBinding(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValueKind.CardInstance)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// The named spell, if the caster may cast it now: in a book, owned by the caster (the
            /// borrow's ownership filter, with the caster as the borrower), and printing a primary the
            /// borrow contract can perform (<see cref="BorrowedPrimaryActionDeclaration.Refuses"/>).
            /// </summary>
            private static CardInstance Castable(EffectCapabilityContext context)
            {
                var named = context.Input(SpellParameter);
                if (!context.CardState.IsAvailable || named.Kind != EffectValueKind.CardInstance ||
                    !context.ActorId.IsDefined)
                {
                    return null;
                }

                return BorrowedPrimaryActionQuery.EligibleCards(
                        BorrowedPrimaryActionDeclaration.FromSpellbook(),
                        context.ActorId,
                        context.CardState.Definitions,
                        context.CardState.Instances,
                        context.Timing?.Effects,
                        context.Capabilities)
                    .SingleOrDefault(card => card.Id.Equals(named.CardInstanceId));
            }

            /// <summary>
            /// "As the spell is cast, remove it from the spellbook faceup": the move is this step's own,
            /// finished before the borrow begins, so the borrowed card never moves inside the borrow.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var spell = Castable(context);
                if (spell == null || !CardLifecycleAuthority.TryRemoveFromSpellbookFaceup(spell))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(
                    new[]
                    {
                        new EffectOutcomeFact(
                            SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValue.Card(spell.Id))
                    },
                    // Milestone 9.30h. Reported so the seat-public log can say "casts Fireball".
                    spellbookChanges: new[] { SpellbookChange.Cast(spell.OwnerId, spell.Id) });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            /// <summary>
            /// A spell that cannot be cast is a mandatory step that cannot be taken (greyed). A castable
            /// spell is always worth casting, whether or not its primary could then act: GY-Q12 (user,
            /// 2026-10-01, "Yes, castable") - the faceup removal itself changes the game, because
            /// Sunburst, Dimension Door and Midas' Touch count removed spells. So W7's grey for a borrowed
            /// primary that can do nothing (9.6) does not reach a cast, and the probe does not ask
            /// whether the spell's primary could act.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Castable(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        private sealed class RefillSpellbookHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RefillSpellbookV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding(ReturnedOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// Every spell the actor has removed, or null where the cards cannot be read. Prepare Spells'
            /// "all spell cards" is the actor's own book: a spell is only ever owned by the hero whose
            /// book it lies in (9.30b).
            /// </summary>
            private static IReadOnlyList<CardInstance> Removed(EffectCapabilityContext context) =>
                context.CardState.IsAvailable && context.ActorId.IsDefined
                    ? SpellbookQuery.Removed(context.CardState.Instances, context.ActorId)
                    : null;

            /// <summary>
            /// Each removed spell returns facedown, and p.17 then cancels the Active effects it is the
            /// source of - its own state change, one spell at a time, so an effect another card
            /// created is untouched.
            /// </summary>
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var removed = Removed(context);
                if (removed == null)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var returned = 0;
                foreach (var spell in removed)
                {
                    if (!CardLifecycleAuthority.TryReturnToSpellbook(spell))
                    {
                        continue;
                    }

                    returned++;
                    if (context.Timing != null && context.RoundIndex >= 1 && context.TurnIndex >= 1)
                    {
                        context.Timing.CancelForSourceCardStateChange(
                            spell.Id, context.RoundIndex, context.TurnIndex);
                    }
                }

                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact(ReturnedOutput, EffectValue.Integer(returned)) },
                    // Milestone 9.30h. How many returned, never which: they are facedown again. An
                    // empty refill (GY-Q7) returned nothing and reports nothing.
                    spellbookChanges: returned == 0
                        ? null
                        : new[] { SpellbookChange.Refilled(context.ActorId, returned) });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Removed(context) == null
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }
    }
}
