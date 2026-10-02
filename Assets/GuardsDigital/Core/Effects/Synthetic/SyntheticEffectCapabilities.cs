using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// The bounded synthetic capability set milestone 6.2a executes its frame through.
    ///
    /// Deliberately none of these mutates the board. Milestone 6.2a's non-goals bar Repeat, Clear,
    /// direct Move, Object Push, Place, Spawn, Swap, Replace, and Remove, which is every board mutation
    /// the roadmap has defined so far, so a board-mutating fixture here would either violate a non-goal
    /// or pre-empt milestone 6.2. These exercise the execution frame - the four handler results, typed
    /// output binding, chooser ownership, and declared checkpoints - not new game rules.
    ///
    /// It lives in production Core rather than a test assembly for the same reason
    /// <c>SyntheticShowcaseHand</c> and <c>CardRetrievalHandler</c> do: a configured session composes
    /// it, so it must be reachable from the composition root.
    /// </summary>
    public static class SyntheticEffectCapabilities
    {
        public static EffectCapabilityId NoteActor { get; } = new EffectCapabilityId("synthetic-note-actor");
        public static EffectCapabilityId ChooseHero { get; } = new EffectCapabilityId("synthetic-choose-hero");
        public static EffectCapabilityId IsFlagged { get; } = new EffectCapabilityId("synthetic-is-flagged");
        public static EffectCapabilityId Impossible { get; } = new EffectCapabilityId("synthetic-impossible");
        public static EffectCapabilityId Declined { get; } = new EffectCapabilityId("synthetic-declined");
        public static EffectCapabilityId AcceptHero { get; } = new EffectCapabilityId("synthetic-accept-hero");
        public static EffectCapabilityId MarkActor { get; } = new EffectCapabilityId("synthetic-mark-actor");

        /// <summary>
        /// The Active effect <see cref="MarkActor"/> creates. Catalog-owned semantics: an instance
        /// records only which hero controls it and when, and a restore rebinds to this object rather
        /// than rebuilding an equivalent one from the payload.
        /// </summary>
        public static ActiveEffectDefinition MarkDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-mark-actor-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.ThisTurn,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.ActionStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.SourceCardStateChanged },
            false,
            "SyntheticMark",
            "synthetic-mark-actor",
            new[] { TimingCheckpoint.ActionEnded });

        /// <summary>
        /// A durable Next Turn effect, declared so fixtures that need an effect surviving a turn or round
        /// boundary have one the catalog actually owns. Registered on the note-actor capability because
        /// every Active effect crossing a snapshot must be declared by some registered capability.
        /// </summary>
        public static ActiveEffectDefinition DurableDefinition { get; } = new ActiveEffectDefinition(
            "synthetic-durable-effect",
            ActiveEffectAffectedScopeKind.ControllerHero,
            ActiveEffectDurationKind.NextTurn,
            TimingCheckpoint.ActionEnded,
            TimingCheckpoint.TurnStarted,
            TimingCheckpoint.TurnOnlyCancellationBeforeTurnEnded,
            new[] { ActiveEffectCancellationCondition.Explicit },
            true,
            "SyntheticDurable",
            "synthetic-durable",
            Array.Empty<TimingCheckpoint>());

        public static EffectCapabilityReference NoteActorV1 => new EffectCapabilityReference(NoteActor, 1);
        public static EffectCapabilityReference ChooseHeroV1 => new EffectCapabilityReference(ChooseHero, 1);
        public static EffectCapabilityReference IsFlaggedV1 => new EffectCapabilityReference(IsFlagged, 1);
        public static EffectCapabilityReference ImpossibleV1 => new EffectCapabilityReference(Impossible, 1);
        public static EffectCapabilityReference DeclinedV1 => new EffectCapabilityReference(Declined, 1);
        public static EffectCapabilityReference AcceptHeroV1 => new EffectCapabilityReference(AcceptHero, 1);
        public static EffectCapabilityReference MarkActorV1 => new EffectCapabilityReference(MarkActor, 1);

        /// <summary>
        /// The build's one capability contract set. Milestone 6.2's component handlers join here rather
        /// than forming a second catalog: a match has one registered set, and the snapshot records it as
        /// one list to compare a restore against.
        ///
        /// Milestone 8.25e7 keeps the implementation set generic: the build composition supplies any
        /// content-owned declarations after these handlers. That preserves one catalog and one
        /// registration list without making Core.Effects depend on a verified hero.
        /// </summary>
        public static EffectCapabilityCatalog CreateCatalog(
            IEnumerable<IEffectCapabilityHandler> contentHandlers = null) =>
            new EffectCapabilityCatalog(new IEffectCapabilityHandler[]
                {
                    new NoteActorHandler(),
                    new ChooseHeroHandler(),
                    new IsFlaggedHandler(),
                    new ImpossibleHandler(),
                    new DeclinedHandler(),
                    new AcceptHeroHandler(),
                    new MarkActorHandler()
                }
                .Concat(SyntheticComponentCapabilities.Handlers())
                .Concat(SyntheticModificationCapabilities.Handlers())
                .Concat(SyntheticDisplacementCapabilities.Handlers())
                // Milestone 8.35z33 splits the targeting list the way the roster one has been split
                // since 8.20, and the placement one has been split since milestone 8.35z28: what a
                // hero composes but no dealt card starts waits in that file's `DeferredHandlers`
                // rather than entering the registration list a snapshot compares. EVERY line here is
                // therefore `RegisteredHandlers`, and a `Handlers()` on any of them would register the
                // whole six-hero wave by accident.
                .Concat(SyntheticTargetingCapabilities.RegisteredHandlers())
                .Concat(SyntheticPlacementCapabilities.RegisteredHandlers())
                .Concat(SyntheticExchangeCapabilities.Handlers())
                .Concat(SyntheticCardStateCapabilities.Handlers())
                .Concat(SyntheticConsequenceCapabilities.Handlers())
                .Concat(SyntheticContinuationCapabilities.Handlers())
                // Milestone 8.35z27. `attack-sequence@1` joins the list on the same half
                // of the rule the comment below states: the showcase hand's two-bullet Attack names
                // it, so it is reachable from a printed card the moment it is registered, and no
                // hero's slice is the first place its handler arrives.
                .Concat(SyntheticSequenceCapabilities.Handlers())
                .Concat(SyntheticValueCapabilities.Handlers())
                .Concat(SyntheticPersistentSourceCapabilities.Handlers())
                .Concat(SyntheticPausingSourceCapabilities.Handlers())
                // Milestone 8.19b built this vocabulary and deliberately left it out of the build's
                // catalog: no verified definition could reach it, and the reachability gate refuses a
                // registration no card can start. Milestone 8.20 is the milestone that changes that -
                // Arien's Noble Blade names `choose-target@2`, his three Currents name
                // `behind-target-line@1`, and Ebb and Flow names `spaces-adjacent@1` - so those three
                // join the one registration list a snapshot compares, and the rest keep waiting for
                // the hero whose face names them.
                .Concat(SyntheticRosterCapabilities.RegisteredHandlers())
                // Milestone 8.35z50, the milestone that deals Ursafar the Savage. Three files whose
                // whole contents waited for him, joining on the same half of the rule the lines above
                // state: `hero-condition@1` is the enraged predicate twelve of his faces read,
                // `borrow-primary-action@1` is the contract Angry Roar and his two borrowing Greens
                // start, and `attack-target-survived@1` is the reading his three removal Reds gate on.
                // Each is `RegisteredHandlers` rather than a bare `Handlers()`, which is what keeps the
                // first two from registering Tali's half of their shared vocabulary before she is
                // dealt - the distinction milestone 8.35z33's comment above exists to protect.
                .Concat(SyntheticConditionCapabilities.RegisteredHandlers())
                .Concat(SyntheticBorrowedActionCapabilities.RegisteredHandlers())
                .Concat(SyntheticAttackTargetSurvivalCapabilities.RegisteredHandlers())
                // Milestone 8.37. The two showcase instances of Tali's own token handler TYPES, so
                // neither arrives for the first time inside her slice. Her six real ones register
                // from her own content file, beside the definitions they are bound to.
                .Concat(SyntheticTokenCapabilities.RegisteredHandlers())
                // Milestone 8.40j, the milestone that deals Bain the Bounty Hunter. Milestone 8.40f
                // built this file's two contracts and deliberately left them out of the catalog on
                // the same half of the rule every line above states: no dealt card printed either
                // sentence, and the reachability gate refuses a registration no card can start. His
                // three guessing Blue faces are the cards that change that, and each handler TYPE
                // took a showcase consumer in the same change.
                .Concat(SyntheticHiddenChoiceCapabilities.RegisteredHandlers())
                .Concat(SyntheticResolvedDiscardCapabilities.RegisteredHandlers())
                .Concat(SyntheticNamedBorrowCapabilities.RegisteredHandlers())
                .Concat(SyntheticOwnedTokenCapabilities.RegisteredHandlers())
                // Milestone 8.61j, the milestone that deals Min the Dragonmonk. Milestone 8.61g built
                // this file's one contract and deliberately left it out of the catalog on the same
                // half of the rule every line above states: no dealt card printed the sentence, and
                // the reachability gate refuses a registration no card can start. Her Fast as
                // Lightning is the card that changes that, and the handler TYPE took a showcase
                // consumer in the same change.
                .Concat(SyntheticBorrowedCardTextCapabilities.RegisteredHandlers())
                // Milestone 9.30i, the milestone that deals Gydion the Archwizard. Three files whose
                // contracts 9.30c-9.30g built and deliberately left out of the catalog on the same half
                // of the rule every line above states: the spellbook's cast, refill, count and removal
                // (his every front casts), the team-victory count and claim (Wish, reached through
                // Prepare Spells once The Archwizard is unlocked) and the regained Life counter (Energy
                // Drain). Each handler TYPE was proved on synthetic spells and staged rounds by those
                // blocks, the precedent Razzle's and Mortimer's hero-only types set.
                .Concat(SyntheticSpellbookCapabilities.RegisteredHandlers())
                .Concat(SyntheticTeamVictoryCapabilities.RegisteredHandlers())
                .Concat(SyntheticTeamLifeCapabilities.RegisteredHandlers())
                // Milestone 9.32h, the milestone that deals Takahide the Warlord. The three families
                // 9.32a, 9.32b and 9.32e built and left out of the catalog on the same half of the rule
                // every line above states: the swap with a deck card (his gold faces and Bushido), the
                // swap of a teammate's unresolved card (Set an Example, Lead from the Front, Hold my
                // Sake) and the choice of a card in a teammate's discard (Proven Warrior, Chosen
                // Champion, The Right Hand). Each handler TYPE was proved on synthetic hands and staged
                // rounds by those blocks.
                .Concat(SyntheticDeckSwapCapabilities.Handlers())
                .Concat(SyntheticUnresolvedSwapCapabilities.Handlers())
                .Concat(SyntheticDiscardedCardChoiceCapabilities.Handlers())
                .Concat(contentHandlers ?? Enumerable.Empty<IEffectCapabilityHandler>()));

        /// <summary>
        /// Creates one Active effect on the actor. This is the capability that gives acceptance check 5
        /// something to prove: the instance crosses the snapshot boundary carrying only runtime state,
        /// and restore rebinds its semantics to <see cref="MarkDefinition"/> from this catalog.
        /// </summary>
        private sealed class MarkActorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => MarkActorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("marked", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                new[] { MarkDefinition };

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (context.Timing == null || context.Participants == null)
                {
                    // Outside a live match there is no lifecycle to create into; the node is a legal
                    // no-op rather than a failure, which is what Applied means.
                    return EffectExecutionResult.Applied(
                        new[] { new EffectOutcomeFact("marked", EffectValue.Hero(context.ActorId)) });
                }

                var effectId = ActiveEffectInstanceIdentity.Mint(context, MarkDefinition);
                if (context.Timing.Effects.All(effect => !effect.Id.Equals(effectId)))
                {
                    context.Timing.Create(
                        effectId,
                        MarkDefinition,
                        context.ActorId,
                        context.Participants.GetTeamForHero(context.ActorId).Side,
                        default,
                        context.SourceCardInstanceId,
                        context.ActionDefinitionId,
                        Math.Max(context.RoundIndex, 1),
                        Math.Max(context.TurnIndex, 1));
                }

                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("marked", EffectValue.Hero(context.ActorId)) });
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Always applies: creating a mark that already exists is the lifecycle's own no-op, and a
            /// context without a lifecycle is a legal no-op too, so there is no state this could read
            /// that would change the answer.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Applies unconditionally and publishes the actor, so a later node can bind it.
        ///
        /// <para><b>It declares NO checkpoint, and milestone 8.39j is why that had to be said.</b> It
        /// used to ask for <c>ActionStarted</c> before itself and <c>ActionEnded</c> after, and the
        /// second of those is a step the resolution refuses by construction:
        /// <c>ResolutionStep.AtCheckpoint</c> throws on the completion checkpoint because "the
        /// completion checkpoint names the action it completes", and the Skill backend turns a node's
        /// declared checkpoints into exactly that factory. So ANY live resolution reaching this node
        /// threw an <c>ArgumentException</c> and the command was refused - which nothing noticed while
        /// the only content naming it was the showcase hand, and which Cutter's A Fistful of Coins
        /// made reachable by printing "You gain N coins" with a named recipient. Measured in Play Mode
        /// at milestone 8.39j: confirming the coin bullet was refused with that exact message and the
        /// printed victory sentence could not be reached at all.</para>
        ///
        /// <para>Both were wrong on their own terms as well as on the factory's. An action's start and
        /// end are the ACTION's moments and the backend already emits them around the whole
        /// resolution; a node that publishes who is acting is a fact, not a moment, and re-raising the
        /// action's own boundaries from inside it would dispatch every timing subscriber twice.</para>
        /// </summary>
        private sealed class NoteActorHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => NoteActorV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("hero", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                new[] { DurableDefinition };

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("hero", EffectValue.Hero(context.ActorId)) });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>Requests a chooser-owned selection between the actor and the controller.</summary>
        private sealed class ChooseHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ChooseHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                outputs: new[] { new EffectOutputBinding("chosen", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);

                return candidates.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(
                        new EffectChoiceRequest(EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(
                    new[] { new EffectOutcomeFact("chosen", selected[0]) });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Length == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The heroes this selection offers. Extracted so the probe reads the same derivation the
            /// execution does rather than restating the condition beside it.
            /// </summary>
            private static EffectValue[] Candidates(EffectCapabilityContext context) =>
                new[] { context.ActorId, context.ControllerId }
                    .Where(hero => hero.IsDefined)
                    .Distinct()
                    .OrderBy(hero => hero.Value, StringComparer.Ordinal)
                    .Select(EffectValue.Hero)
                    .ToArray();
        }

        /// <summary>A pure predicate over one Boolean input, for Conditional branching.</summary>
        private sealed class IsFlaggedHandler : IEffectCapabilityHandler,
            IEffectAnticipatedOutcomeCapabilityHandler
        {
            /// <summary>Milestone 9.6: a predicate reads the state and changes none of it.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>Milestone 9.6: what the predicate would publish, so the offer reads its branch.</summary>
            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var result = Execute(context);
                return result.Kind == EffectExecutionResultKind.Applied
                    ? result.Outcomes
                    : Array.Empty<EffectOutcomeFact>();
            }

            public EffectCapabilityReference Reference => IsFlaggedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("flag", EffectValueKind.Boolean) },
                new[] { new EffectOutputBinding("value", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        "value", EffectValue.Boolean(context.Input("flag").BooleanValue))
                });

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>A pure predicate always applies; what it publishes is the branch's business.</summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.Applied;
        }

        /// <summary>
        /// Consumes a Hero the frame supplies, so typed result binding has something to bind into: an
        /// earlier node's published output flows here without a closure or an expression language.
        /// </summary>
        private sealed class AcceptHeroHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AcceptHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[] { new EffectCapabilityParameter("hero", EffectValueKind.Hero) },
                new[] { new EffectOutputBinding("received", EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var hero = context.Input("hero");
                return hero.IsDefined
                    ? EffectExecutionResult.Applied(
                        new[] { new EffectOutcomeFact("received", hero) })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Input("hero").IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>Never possible, so the four failure policies can each be observed distinctly.</summary>
        private sealed class ImpossibleHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => ImpossibleV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Declines of its own accord. Distinct from <see cref="ImpossibleHandler"/>: this one could
        /// have applied and chose not to, which is not the same rules fact as being unable to.
        /// </summary>
        private sealed class DeclinedHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => DeclinedV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema();

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                EffectExecutionResult.DeclinedOptional();

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            /// <summary>
            /// Declining applies nothing, and unlike an impossible step it stops nothing either, so an
            /// enclosing option is carried by whatever stands after this node rather than by it.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                EffectExecutionResultKind.DeclinedOptional;
        }
    }
}
