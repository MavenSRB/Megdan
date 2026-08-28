using System;
using System.Collections.Generic;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>The two legal sides of an around-primary-action placement declaration.</summary>
    public enum AroundActionPlacement
    {
        None = 0,
        BeforeTheAction = 1,
        AfterTheAction = 2
    }

    /// <summary>
    /// Milestone 8.19a. The two registered capabilities the roster's resumable continuations need, and
    /// the stable identities of the two declarations that are content rather than handlers.
    ///
    /// <para>Four identities are named by the milestone and only two of them are executed. That is not
    /// an inconsistency, it is the catalog's own distinction between a capability that <em>does</em>
    /// something at a node and a declaration that says something about a card. `attack-target@2` is a
    /// printed Attack's statement that its own text names its target, and it is read by the resolver
    /// before any effect frame exists; `before-action-trigger@1` is a rule source's statement that it
    /// runs before an action starts, and it is read by the turn before any effect frame exists. Both
    /// are typed content on the definition, which is where this catalog puts every statement no handler
    /// performs - the same place a token's properties and an Ultimate's rule live.</para>
    ///
    /// <para>The two that are handlers are the two that run at a node: one binds the enclosing Attack's
    /// target from something the text chose, and one publishes which side of a primary action the
    /// text's remaining work belongs to. Neither performs the thing it names. Binding a target does not
    /// resolve an Attack and publishing a placement does not perform a Movement; both publish a typed
    /// fact and the authority that owns the action reads it, which is the transport milestone 6.9's
    /// Lane-Push cause and milestone 8.10's Defeat request already use.</para>
    /// </summary>
    public static class SyntheticContinuationCapabilities
    {
        /// <summary>
        /// `attack-target@2`. Not a handler: it is
        /// <see cref="GuardsDigital.Core.Cards.AttackTargetBinding.ByCardText"/> on a printed Attack's
        /// target policy, read by the Attack resolver when the action starts. Named here so the roster
        /// matrix's identity has one spelling in code.
        /// </summary>
        public const string AttackTargetDeclarationId = "attack-target";

        /// <summary>The revision of that declaration this build implements.</summary>
        public const int AttackTargetDeclarationVersion = 2;

        /// <summary>
        /// `before-action-trigger@1`. Not a handler either: it is
        /// <see cref="GuardsDigital.Core.Cards.BeforeActionTrigger"/> on a continuous rule source, read
        /// by the turn before it starts the action the trigger precedes.
        /// </summary>
        public const string BeforeActionTriggerDeclarationId = "before-action-trigger";

        public const int BeforeActionTriggerDeclarationVersion = 1;

        /// <summary>`set-attack-target@1`.</summary>
        public static EffectCapabilityId SetAttackTarget { get; } =
            new EffectCapabilityId("set-attack-target");

        public static EffectCapabilityReference SetAttackTargetV1 =>
            new EffectCapabilityReference(SetAttackTarget, 1);

        /// <summary>`around-primary-action@1`.</summary>
        public static EffectCapabilityId AroundPrimaryAction { get; } =
            new EffectCapabilityId("around-primary-action");

        public static EffectCapabilityReference AroundPrimaryActionV1 =>
            new EffectCapabilityReference(AroundPrimaryAction, 1);

        /// <summary>The hero this text names as the enclosing Attack's target, when it names one.</summary>
        public const string HeroParameter = "hero";

        /// <summary>The minion this text names as the enclosing Attack's target, when it names one.</summary>
        public const string MinionParameter = "minion";

        /// <summary>
        /// The declared output the bound hero target is published on. One spelling, shared by the
        /// capability that writes it and <c>RequestedAttackTargetQuery</c>, which reads it.
        /// </summary>
        public const string BoundHeroTargetOutput = "boundAttackTargetHero";

        /// <summary>The same, for a minion target.</summary>
        public const string BoundMinionTargetOutput = "boundAttackTargetMinion";

        /// <summary>The side of the primary action this branch stands for: 1 before it, 2 after it.</summary>
        public const string PlacementParameter = "placement";

        /// <summary>The declared output the chosen placement is published on.</summary>
        public const string PlacementOutput = "aroundActionPlacement";

        /// <summary>The placement value naming the side before the primary action.</summary>
        public const int PlacementBeforeTheAction = (int)AroundActionPlacement.BeforeTheAction;

        /// <summary>The placement value naming the side after the primary action.</summary>
        public const int PlacementAfterTheAction = (int)AroundActionPlacement.AfterTheAction;

        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            yield return new SetAttackTargetHandler();
            yield return new AroundPrimaryActionHandler();
        }

        /// <summary>
        /// `set-attack-target@1`. Names the unit the enclosing Attack takes, from a value an earlier
        /// node in the same text published.
        ///
        /// <para><b>It does not resolve anything.</b> The Attack's own resolver owns rulebook p.14: it
        /// re-derives the legal target set against the board as it stands after this text has run,
        /// refuses a unit that set does not contain, records where that unit is standing before the
        /// page's consequences, and only then announces the Attack Total. All this node does is say
        /// which unit the text chose, which is why a charge that ends beside nobody stops the action
        /// here rather than attacking something the card never named.</para>
        ///
        /// <para><b>Why two optional parameters rather than one.</b> p.13 lets an Attack take a hero or
        /// a minion, and the typed value union has a separate kind for each; a single parameter would
        /// have to name one kind and so could not express the other. Exactly one is defined at any
        /// occurrence - a text naming both would be describing two targets for one Attack.</para>
        /// </summary>
        private sealed class SetAttackTargetHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SetAttackTargetV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(HeroParameter, EffectValueKind.Hero, false),
                    new EffectCapabilityParameter(MinionParameter, EffectValueKind.Minion, false)
                },
                new[]
                {
                    new EffectOutputBinding(BoundHeroTargetOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(BoundMinionTargetOutput, EffectValueKind.Minion)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var hero = context.Input(HeroParameter);
                var minion = context.Input(MinionParameter);
                if (hero.IsDefined && minion.IsDefined)
                {
                    throw new InvalidOperationException(
                        "Rulebook p.13 gives an Attack one target, so text binding one names a hero or " +
                        "a minion and never both.");
                }

                if (hero.IsDefined)
                {
                    return EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(BoundHeroTargetOutput, hero)
                    });
                }

                if (minion.IsDefined)
                {
                    return EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(BoundMinionTargetOutput, minion)
                    });
                }

                // Nothing to name. p.10 stops the rest of a mandatory step's action and keeps what the
                // text already did, which is exactly right for a charge that reached nobody: the hero
                // has moved and the Attack does not happen.
                return EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.Input(HeroParameter).IsDefined || context.Input(MinionParameter).IsDefined
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// `around-primary-action@1`. Publishes which side of the primary action this text's remaining
        /// work belongs to.
        ///
        /// <para>It is a leaf under an ordinary <see cref="EffectNodeKind.Choose"/> rather than a node
        /// that asks a question itself, because the question is p.10's ordinary chooser-owned selection
        /// and there is no reason for a second way to ask one. Each branch of that selection is one of
        /// these leaves carrying the literal side it stands for, so the prompt names two branches the
        /// player picks between and the frame ends up holding a typed fact rather than a label.</para>
        ///
        /// <para>Executing it performs nothing at all. The authority that owns the action reads the
        /// published side and decides where the text's remaining work runs: the rest of the text now
        /// and then the action, or the action and then the rest of the text.</para>
        /// </summary>
        private sealed class AroundPrimaryActionHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AroundPrimaryActionV1;

            public EffectCapabilityParameterSchema Schema { get; } = new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(
                        PlacementParameter,
                        EffectValueKind.Integer,
                        integerDomain: EffectIntegerDomain.AllExcept(
                            AroundActionPlacement.None))
                },
                new[] { new EffectOutputBinding(PlacementOutput, EffectValueKind.Integer) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var placement = context.Input(PlacementParameter);
                return IsASide(placement)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(PlacementOutput, placement)
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                EffectExecutionResult.ImpossibleMandatory();

            /// <summary>
            /// Placing work either side of an action is always possible, which is why both branches of
            /// the selection are offered. Whether the work itself can be done is the work's own
            /// question, asked where the work runs.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                IsASide(context.Input(PlacementParameter))
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;

            private static bool IsASide(EffectValue placement) =>
                placement.Kind == EffectValueKind.Integer &&
                (placement.IntegerValue == PlacementBeforeTheAction ||
                 placement.IntegerValue == PlacementAfterTheAction);
        }
    }
}
