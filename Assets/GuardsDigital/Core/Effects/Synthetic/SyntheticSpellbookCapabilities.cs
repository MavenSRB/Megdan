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

        /// <summary>The cast and the refill; each spell's naming step is declared by its content.</summary>
        public static IEnumerable<IEffectCapabilityHandler> DeferredHandlers() =>
            new IEffectCapabilityHandler[] { new CastSpellHandler(), new RefillSpellbookHandler() };

        /// <summary>Nothing until a dealt face casts (9.30i).</summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The naming step for one spell: <paramref name="spell"/>'s definition is the declaration's
        /// fixed parameter, and the hero whose book it reads is the node's <see cref="OwnerParameter"/>.
        /// </summary>
        public static IEffectCapabilityHandler SpellInBookHandler(
            EffectCapabilityReference reference, CardDefinitionId spell) =>
            new SpellInBook(reference, spell);

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

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        SyntheticBorrowedActionCapabilities.BorrowedCardOutput, EffectValue.Card(spell.Id))
                });
            }

            public EffectExecutionResult Resume(EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            /// <summary>
            /// A spell that cannot be cast is a mandatory step that cannot be taken (greyed). A castable
            /// spell whose primary could do nothing now - by the rules the turn applies to its own offers
            /// (9.6) - is a cast that performs nothing: answered as declined, so its bullet stays
            /// selectable beside one that can act (8.35z35) and a card it is the only hope of is greyed
            /// (W7).
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var spell = Castable(context);
                if (spell == null)
                {
                    return EffectExecutionResultKind.ImpossibleMandatory;
                }

                return context.BorrowedPrimaryCanAct == null ||
                    context.BorrowedPrimaryCanAct(spell.Id, context.ActorId)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.DeclinedOptional;
            }
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

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(ReturnedOutput, EffectValue.Integer(returned))
                });
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
