using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Consequences;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.19b's reusable roster vocabulary. These are synthetic consumers over shared typed
    /// contracts; no verified hero definition is registered here.
    /// </summary>
    public static class SyntheticRosterCapabilities
    {
        public static EffectCapabilityReference ChooseTargetV2 => Reference("choose-target", 2);
        public static EffectCapabilityReference ChooseTargetSetV1 => Reference("choose-target-set", 1);
        public static EffectCapabilityReference ChooseTargetPairV1 => Reference("choose-target-pair", 1);
        public static EffectCapabilityReference BehindTargetLineV1 => Reference("behind-target-line", 1);
        public static EffectCapabilityReference ChooseChargeLineV1 => Reference("choose-charge-line", 1);
        public static EffectCapabilityReference ChooseThroughTargetLineV1 =>
            Reference("choose-through-target-line", 1);
        public static EffectCapabilityReference FormerTargetSpaceV1 =>
            Reference("attacked-target-space", 1);
        public static EffectCapabilityReference PlayedAttackThisTurnV1 =>
            Reference("target-played-attack-this-turn", 1);
        public static EffectCapabilityReference HeroDefeatedThisRoundV1 =>
            Reference("hero-defeated-this-round", 1);
        public static EffectCapabilityReference PresenceCountV1 => Reference("presence-count", 1);
        public static EffectCapabilityReference PreConsequenceAdjacencyV1 =>
            Reference("pre-consequence-adjacency", 1);

        /// <summary>
        /// Milestone 8.20's `was-adjacent@1`, which the roster matrix names beside milestone 8.19b's
        /// <see cref="PreConsequenceAdjacencyV1"/> and which is a different question.
        ///
        /// <para>That one asks about the target of an enclosing Attack before p.14's consequences ran,
        /// which is what Sabina's Shootout prints. Arien's Ebb and Flow is a Skill and has no enclosing
        /// Attack at all: "Swap with an enemy minion in range; <b>if it was adjacent to you</b>, may
        /// repeat once." Its "was" is the moment before its own Swap, and p.19's Swap exchanges exactly
        /// two spaces - so the two spaces the Swap traded <em>are</em> where the minion and the hero
        /// were standing, and asking whether they are adjacent is asking the card's question rather
        /// than a reconstruction of it.</para>
        ///
        /// <para>Stated as a question about two spaces rather than about a swap, because that is all it
        /// reads: no board, no participants, no declaration. The past tense the face prints is what
        /// makes the sentence readable at all - by the time it is asked, the swap has already moved the
        /// minion into the hero's former space - and a predicate over the two exchanged spaces is
        /// unaffected by that, which is why it can be asked afterwards.</para>
        /// </summary>
        public static EffectCapabilityReference SpacesAdjacentV1 => Reference("spaces-adjacent", 1);
        public static EffectCapabilityReference AdjacentTerrainPresenceV1 =>
            Reference("adjacent-terrain-presence", 1);
        public static EffectCapabilityReference RetrieveCardV1 => Reference("retrieve-card", 1);
        public static EffectCapabilityReference GainResourceV1 => Reference("gain-resource", 1);
        public static EffectCapabilityReference TransferResourceV1 => Reference("transfer-resource", 1);

        public const string OriginParameter = "origin";

        /// <summary>
        /// How far from its origin a selection reaches, as a number the definition writes down.
        ///
        /// <para>Milestone 8.20 renames the wire name from `distance` to `reach`, and the rename is the
        /// point. `OptionalDistanceConformanceTests` requires every capability declaring a *distance*
        /// to read how much of it is optional through the one statement of rulebook pages 10 and 11 -
        /// because a displacement that decided for itself once let one capability move nothing where
        /// another refused the same choice. A selection radius has no such question: p.9's disk is
        /// inclusive and reaches everything within it, and there is nothing about it a player could
        /// choose to do less of. Two different questions sharing one parameter name made this
        /// vocabulary look like a subject of that rule the moment the build registered it.</para>
        /// </summary>
        public const string DistanceParameter = "reach";
        public const string UnitKindParameter = "unitKind";
        public const string RelationParameter = "relation";
        public const string ChosenHeroOutput = "chosenHero";
        public const string ChosenMinionOutput = "chosenMinion";

        /// <summary>
        /// Milestone 8.20. Whether the enclosing Attack's own target is one of the candidates.
        ///
        /// <para>Arien's Noble Blade is what needs it: "You may move <b>another</b> unit that is
        /// adjacent to the target 1 space". A disk of radius one drawn on the target's space contains
        /// the target, because p.9 measures inclusively - and the target is not adjacent to itself, so
        /// offering it would let this card reposition the very unit the Attack is about to resolve
        /// against.</para>
        ///
        /// <para>It is optional and defaults to false, so every declaration written at milestone 8.19b
        /// keeps its exact candidate set. That is the same shape <c>presence-count@1</c> already gave
        /// the question, stated once more here rather than answered a second way.</para>
        /// </summary>
        public const string ExcludeTargetParameter = "excludeTarget";

        /// <summary>
        /// Milestone 8.20. The chosen piece's current space, published beside its identity.
        ///
        /// <para>It exists because the operations that reposition a piece take a coordinate - p.18's
        /// Push, Place and direct Move each name a space - while this capability names a unit, and
        /// Noble Blade needs both: the sentence chooses a unit and then moves it. Wasp's radius
        /// selections have published a coordinate since milestone 6.8, so this is the same output on a
        /// second selection rather than a new idea.</para>
        ///
        /// <para>A node that does not declare it never sees it, which is why adding it leaves every
        /// milestone 8.19b definition unchanged.</para>
        /// </summary>
        public const string ChosenSpaceOutput = "chosenSpace";

        /// <summary>
        /// Milestone 8.20. Whether an up-to-one selection named anybody at all.
        ///
        /// <para>Arien's Dangerous Current, Raging Stream and Violent Torrent each print "Up to 1 enemy
        /// hero in any of the N spaces ... discards a card, or is defeated", and the two halves of that
        /// sentence are a selection that may reach nobody followed by a step that is mandatory once it
        /// does. A conditional reads a boolean, and this is that boolean: naming nobody is a legal and
        /// complete outcome, which is exactly what a consumer must be able to tell apart from a
        /// question that was never asked.</para>
        /// </summary>
        public const string NamedAHeroOutput = "namedAHero";

        /// <summary>
        /// The whole vocabulary, for the bounded fixture catalogs that exercise each contract over
        /// hero-shaped consumers. It is deliberately not what the build registers: milestone 7.9's
        /// deferral gate refuses a registration no printed card can start, and most of this set is
        /// still waiting for the hero whose face names it.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            RegisteredHandlers().Concat(new IEffectCapabilityHandler[]
            {
                new ChooseTargetSetHandler(),
                new ChooseTargetPairHandler(),
                new ChargeLineHandler(),
                new ThroughTargetLineHandler(),
                new FormerTargetSpaceHandler(),
                new PlayedAttackThisTurnHandler(),
                new HeroDefeatedThisRoundHandler(),
                new PresenceCountHandler(),
                new PreConsequenceAdjacencyHandler(),
                new AdjacentTerrainPresenceHandler(),
                new RetrieveCardHandler(),
                new GainResourceHandler(),
                new TransferResourceHandler()
            });

        /// <summary>
        /// The part of this vocabulary a verified hero's printed set actually reaches, which is what
        /// <see cref="SyntheticEffectCapabilities.CreateCatalog"/> registers.
        ///
        /// <para>Milestone 8.20 is the first milestone with anything to put here. Arien's Noble Blade
        /// names <c>choose-target@2</c>, his three Currents name <c>behind-target-line@1</c>, and Ebb
        /// and Flow names <c>spaces-adjacent@1</c>. The remaining thirteen contracts belong to Brogan,
        /// Sabina, Tigerclaw and Xargatha, and each joins this list at the milestone whose printed card
        /// first names it - which is what keeps the registration list a statement about what this build
        /// can do rather than about what it has written down.</para>
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> RegisteredHandlers()
        {
            yield return new ChooseTargetHandler();
            yield return new BehindTargetLineHandler();
            yield return new SpacesAdjacentHandler();
        }

        private static EffectCapabilityReference Reference(string id, int revision) =>
            new EffectCapabilityReference(new EffectCapabilityId(id), revision);

        private static EffectCapabilityParameterSchema TargetSchema(
            IEnumerable<EffectCapabilityParameter> extra = null,
            IEnumerable<EffectOutputBinding> outputs = null) =>
            new EffectCapabilityParameterSchema(
                new[]
                {
                    new EffectCapabilityParameter(OriginParameter, EffectValueKind.Integer),
                    new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer),
                    new EffectCapabilityParameter(UnitKindParameter, EffectValueKind.Integer),
                    new EffectCapabilityParameter(RelationParameter, EffectValueKind.Integer),
                    new EffectCapabilityParameter(
                        "activity", EffectValueKind.Integer, isRequired: false)
                }.Concat(extra ?? Enumerable.Empty<EffectCapabilityParameter>()),
                outputs ?? new[]
                {
                    new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                });

        private abstract class TargetingHandler : IEffectImmunityOverrideCapabilityHandler
        {
            public abstract IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; }

            protected static IReadOnlyList<BoardObjectReference> Targets(
                EffectCapabilityContext context,
                RosterQueryOrigin origin,
                int distance,
                RosterUnitKind unitKind,
                SourceTeamRelation relation,
                RuleActivity activity,
                bool excludeEnclosingTarget = false)
            {
                if (!TryOrigin(context, origin, out var at) || distance < 0 ||
                    unitKind == RosterUnitKind.None ||
                    !Enum.IsDefined(typeof(RosterUnitKind), unitKind) ||
                    relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    activity == RuleActivity.None ||
                    !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var board = context.Components?.Board;
                if (board == null || context.Participants == null)
                    return Array.Empty<BoardObjectReference>();

                return RosterPresenceQuery.FindUnitsWithin(
                        at,
                        distance,
                        unitKind,
                        relation,
                        context.ActorId,
                        board,
                        context.Participants,
                        context.Modifications,
                        BoardObjectReference.ForHero(context.ActorId),
                        excludeEnclosingTarget ? EnclosingTarget(context) : default)
                    .Where(target => !ImmunityQuery.IsImmuneTo(
                        target.AsRuleObject(),
                        activity,
                        RuleObject.ForHero(context.ActorId),
                        board,
                        context.Participants,
                        context.Modifications,
                        context.ImmunityOverrides))
                    .ToArray();
            }

            protected static IReadOnlyList<BoardObjectReference> DeclaredTargets(
                EffectCapabilityContext context)
            {
                var declaredActivity = (RuleActivity)context.Input("activity").IntegerValue;
                var activity = declaredActivity == RuleActivity.None
                    ? context.EnclosingActivity
                    : declaredActivity;
                return Targets(
                    context,
                    (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                    context.Input(DistanceParameter).IntegerValue,
                    (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue,
                    (SourceTeamRelation)context.Input(RelationParameter).IntegerValue,
                    activity,
                    context.Input(ExcludeTargetParameter).BooleanValue);
            }

            protected static EffectValue AsValue(BoardObjectReference target) =>
                target.Kind == BoardObjectKind.Hero
                    ? EffectValue.Hero(target.HeroId)
                    : EffectValue.Minion(target.MinionId);

            protected static EffectOutcomeFact AsOutcome(
                BoardObjectReference target,
                string heroOutput = ChosenHeroOutput,
                string minionOutput = ChosenMinionOutput) =>
                target.Kind == BoardObjectKind.Hero
                    ? new EffectOutcomeFact(heroOutput, EffectValue.Hero(target.HeroId))
                    : new EffectOutcomeFact(minionOutput, EffectValue.Minion(target.MinionId));

            protected static BoardObjectReference FromValue(EffectValue value)
            {
                if (value.Kind == EffectValueKind.Hero)
                    return BoardObjectReference.ForHero(value.HeroId);
                if (value.Kind == EffectValueKind.Minion)
                    return BoardObjectReference.ForMinion(value.MinionId);
                return default;
            }
        }

        private sealed class ChooseTargetHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                AllActivities;
            public EffectCapabilityReference Reference => ChooseTargetV2;

            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                // Milestone 8.20. Carried by this selection alone, because it is the only one a
                // verified card asks the question of - the same rule that keeps every other narrowing
                // on the declaration that prints it. `presence-count@1` names its own, beside an
                // actor exclusion this selection makes unconditionally.
                new[]
                {
                    new EffectCapabilityParameter(
                        ExcludeTargetParameter, EffectValueKind.Boolean, isRequired: false)
                },
                new[]
                {
                    new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                    new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion),
                    new EffectOutputBinding(ChosenSpaceOutput, EffectValueKind.Coordinate)
                });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var chosen = selected?.Count == 1 ? FromValue(selected[0]) : default;
                if (!chosen.IsDefined || !DeclaredTargets(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Milestone 8.20 publishes where the chosen piece is standing beside who it is, for a
                // node that asked - the space is read now rather than derived later by whatever
                // consumes it, because that is the moment the answer is about and a later node may
                // already have moved something. A node that did not ask never sees it, which is what
                // keeps every milestone 8.19b definition publishing exactly what it declared.
                var outcomes = new List<EffectOutcomeFact> { AsOutcome(chosen) };
                if (context.Publishes(ChosenSpaceOutput) &&
                    context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(chosen, out var at))
                {
                    outcomes.Add(new EffectOutcomeFact(ChosenSpaceOutput, EffectValue.Space(at)));
                }

                return EffectExecutionResult.Applied(outcomes);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                DeclaredTargets(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private sealed class ChooseTargetSetHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => ChooseTargetSetV1;
            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                new[]
                {
                    new EffectCapabilityParameter("minimum", EffectValueKind.Integer),
                    new EffectCapabilityParameter("maximum", EffectValueKind.Integer)
                },
                new[]
                {
                    new EffectOutputBinding("selectedCount", EffectValueKind.Integer),
                    new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                    new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                    new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                    new EffectOutputBinding("secondMinion", EffectValueKind.Minion)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                    return EffectExecutionResult.ImpossibleMandatory();
                if (maximum == 0)
                    return SetOutcome(Array.Empty<BoardObjectReference>());
                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, candidates.Select(AsValue), minimum, maximum));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out var minimum, out var maximum))
                    return EffectExecutionResult.ImpossibleMandatory();
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Any(value => !value.IsDefined || !candidates.Contains(value)) ||
                    chosen.Distinct().Count() != chosen.Length ||
                    chosen.Length < minimum || chosen.Length > maximum)
                    return EffectExecutionResult.ImpossibleMandatory();
                return SetOutcome(Canonical(chosen));
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context)
            {
                var candidates = DeclaredTargets(context);
                if (!Bounds(context, candidates.Count, out _, out var maximum))
                    return EffectExecutionResultKind.ImpossibleMandatory;
                return maximum == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
            }

            private static bool Bounds(
                EffectCapabilityContext context, int candidates, out int minimum, out int maximum)
            {
                minimum = context.Input("minimum").IntegerValue;
                maximum = context.Input("maximum").IntegerValue;
                if (minimum < 0 || maximum < minimum || maximum > 2) return false;
                maximum = Math.Min(maximum, candidates);
                return minimum <= maximum;
            }

            private static EffectExecutionResult SetOutcome(
                IReadOnlyList<BoardObjectReference> selected)
            {
                var facts = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact("selectedCount", EffectValue.Integer(selected.Count))
                };
                if (selected.Count > 0)
                    facts.Add(AsOutcome(selected[0], "firstHero", "firstMinion"));
                if (selected.Count > 1)
                    facts.Add(AsOutcome(selected[1], "secondHero", "secondMinion"));
                return EffectExecutionResult.Applied(facts);
            }
        }

        private sealed class ChooseTargetPairHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                SwapActivity;
            public EffectCapabilityReference Reference => ChooseTargetPairV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(OriginParameter, EffectValueKind.Integer),
                        new EffectCapabilityParameter(DistanceParameter, EffectValueKind.Integer),
                        new EffectCapabilityParameter(RelationParameter, EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("firstHero", EffectValueKind.Hero),
                        new EffectOutputBinding("firstMinion", EffectValueKind.Minion),
                        new EffectOutputBinding("secondHero", EffectValueKind.Hero),
                        new EffectOutputBinding("secondMinion", EffectValueKind.Minion)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<BoardObjectReference> Candidates(EffectCapabilityContext context) =>
                Targets(
                    context,
                    (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                    context.Input(DistanceParameter).IntegerValue,
                    RosterUnitKind.Minion,
                    (SourceTeamRelation)context.Input(RelationParameter).IntegerValue,
                    RuleActivity.Swap);

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count < 2
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 2, 2));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var candidates = Candidates(context);
                var chosen = (selected ?? Array.Empty<EffectValue>()).Select(FromValue).ToArray();
                if (chosen.Length != 2 || chosen.Distinct().Count() != 2 ||
                    chosen.Any(value => !value.IsDefined || !candidates.Contains(value)))
                    return EffectExecutionResult.ImpossibleMandatory();
                var canonical = Canonical(chosen);
                return EffectExecutionResult.Applied(new[]
                {
                    AsOutcome(canonical[0], "firstHero", "firstMinion"),
                    AsOutcome(canonical[1], "secondHero", "secondMinion")
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count < 2
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private sealed class BehindTargetLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => BehindTargetLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("length", EffectValueKind.Integer) },
                    new[]
                    {
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(NamedAHeroOutput, EffectValueKind.Boolean)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<HeroId> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var targetAt = context.EnclosingAttack.TargetSpaceBeforeConsequences;
                var length = context.Input("length").IntegerValue;
                if (board == null || context.Participants == null || !targetAt.HasValue || length < 1 ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) ||
                    !StraightLineQuery.TryGetDirection(
                        actorAt, targetAt.Value, out var direction, out _))
                    return Array.Empty<HeroId>();

                var result = new List<HeroId>();
                for (var distance = 1; distance <= length; distance++)
                {
                    var at = StraightLineQuery.Step(targetAt.Value, direction, distance);
                    if (!board.TryGetObjectAt(at, out var target) ||
                        target.Kind != BoardObjectKind.Hero ||
                        !TargetQuery.StandsInRelationTo(
                            SourceTeamRelation.OpposingSource, target, context.ActorId,
                            context.Participants, board) ||
                        ImmunityQuery.IsImmuneTo(
                            target.AsRuleObject(), context.EnclosingActivity,
                            RuleObject.ForHero(context.ActorId), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides))
                        continue;
                    result.Add(target.HeroId);
                }

                return result;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.Applied(NamedNobody)
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero), 0, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count == 0)
                {
                    return EffectExecutionResult.Applied(NamedNobody);
                }

                return selected.Count == 1 && selected[0].Kind == EffectValueKind.Hero &&
                    Candidates(context).Contains(selected[0].HeroId)
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(ChosenHeroOutput, selected[0]),
                        new EffectOutcomeFact(NamedAHeroOutput, EffectValue.Boolean(true))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            /// <summary>
            /// The named absence, published rather than left out. Milestone 8.20: "Up to 1 enemy hero
            /// ... discards a card, or is defeated" is a step that may reach nobody, and what follows
            /// it has to be able to tell "nobody was named" from "the question has not been asked".
            /// An outcome nothing published resolves to an undefined value at the consumer, which is
            /// the same thing both cases would look like.
            /// </summary>
            private static readonly EffectOutcomeFact[] NamedNobody =
            {
                new EffectOutcomeFact(NamedAHeroOutput, EffectValue.Boolean(false))
            };

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private sealed class ChargeLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();
            public EffectCapabilityReference Reference => ChooseChargeLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("minimumDistance", EffectValueKind.Integer),
                        new EffectCapabilityParameter("maximumDistance", EffectValueKind.Integer),
                        new EffectCapabilityParameter(UnitKindParameter, EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate),
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                Ask(context);

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (context.AnswersAlreadyGiven.Count == 0)
                {
                    if (selected == null || selected.Count != 1 ||
                        selected[0].Kind != EffectValueKind.Coordinate ||
                        !Options(context).Any(value => value.Destination.Equals(selected[0].Coordinate)))
                        return EffectExecutionResult.ImpossibleMandatory();
                    return AskTargets(context, selected[0].Coordinate);
                }

                var destinationAnswer = context.AnswersAlreadyGiven[0];
                var target = selected?.Count == 1 ? FromValue(selected[0]) : default;
                if (destinationAnswer.Count != 1 ||
                    destinationAnswer[0].Kind != EffectValueKind.Coordinate || !target.IsDefined)
                    return EffectExecutionResult.ImpossibleMandatory();

                var option = Options(context).FirstOrDefault(value =>
                    value.Destination.Equals(destinationAnswer[0].Coordinate) &&
                    value.Targets.Contains(target));
                if (option == null) return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(option.Destination)),
                    AsOutcome(target)
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Options(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static EffectExecutionResult Ask(EffectCapabilityContext context)
            {
                if (context.AnswersAlreadyGiven.Count > 0)
                {
                    var answer = context.AnswersAlreadyGiven[0];
                    return answer.Count == 1 && answer[0].Kind == EffectValueKind.Coordinate
                        ? AskTargets(context, answer[0].Coordinate)
                        : EffectExecutionResult.ImpossibleMandatory();
                }

                var destinations = Options(context).Select(value => value.Destination).Distinct().ToArray();
                return destinations.Length == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, destinations.Select(EffectValue.Space), 1, 1));
            }

            private static EffectExecutionResult AskTargets(
                EffectCapabilityContext context, HexCoordinate destination)
            {
                var option = Options(context).FirstOrDefault(value =>
                    value.Destination.Equals(destination));
                return option == null || option.Targets.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, option.Targets.Select(AsValue), 1, 1));
            }

            private static IReadOnlyList<ChargeOption> Options(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var minimum = context.Input("minimumDistance").IntegerValue;
                var maximum = context.Input("maximumDistance").IntegerValue;
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                if (board == null || context.Participants == null || minimum < 1 || maximum < minimum ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<ChargeOption>();

                var actor = BoardObjectReference.ForHero(context.ActorId);
                var result = new List<ChargeOption>();
                foreach (var route in board.FindDirectMoveDestinations(
                    actor, maximum, context.Modifications))
                {
                    if (route.Cost < minimum || route.Cost > maximum ||
                        !StraightLineQuery.TryGetDirection(
                            actorAt, route.Destination, out var direction, out var distance) ||
                        distance != route.Cost || !IsStraightRoute(actorAt, direction, route.Route))
                        continue;

                    var targets = RosterPresenceQuery.FindUnitsWithin(
                            route.Destination, 1, kind, SourceTeamRelation.OpposingSource,
                            context.ActorId, board, context.Participants, context.Modifications)
                        .Where(target => !ImmunityQuery.IsImmuneTo(
                            target.AsRuleObject(), context.EnclosingActivity,
                            actor.AsRuleObject(), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides))
                        .ToArray();
                    if (targets.Length > 0)
                        result.Add(new ChargeOption(route.Destination, targets));
                }

                return result.OrderBy(value => value.Destination.Q)
                    .ThenBy(value => value.Destination.R).ToArray();
            }
        }

        private sealed class ThroughTargetLineHandler : TargetingHandler, IEffectCapabilityHandler
        {
            private static readonly HexDirection[] Directions =
            {
                HexDirection.East, HexDirection.NorthEast, HexDirection.NorthWest,
                HexDirection.West, HexDirection.SouthWest, HexDirection.SouthEast
            };

            public override IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                Array.Empty<RuleActivity>();

            public EffectCapabilityReference Reference => ChooseThroughTargetLineV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter(UnitKindParameter, EffectValueKind.Integer) },
                    new[]
                    {
                        new EffectOutputBinding("destination", EffectValueKind.Coordinate),
                        new EffectOutputBinding(ChosenHeroOutput, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutput, EffectValueKind.Minion)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var options = Options(context);
                return options.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, options.Select(value => AsValue(value.Target)), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var target = selected?.Count == 1 ? FromValue(selected[0]) : default;
                var option = target.IsDefined
                    ? Options(context).SingleOrDefault(value => value.Target.Equals(target))
                    : null;
                if (option == null) return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("destination", EffectValue.Space(option.Destination)),
                    AsOutcome(option.Target)
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Options(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static IReadOnlyList<ThroughOption> Options(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                if (board == null || context.Participants == null ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    !board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                    return Array.Empty<ThroughOption>();

                var actor = BoardObjectReference.ForHero(context.ActorId);
                var options = new List<ThroughOption>();
                foreach (var direction in Directions)
                {
                    var crossedAt = StraightLineQuery.Step(actorAt, direction, 1);
                    var destination = StraightLineQuery.Step(actorAt, direction, 2);
                    if (!board.TryGetObjectAt(crossedAt, out var target) ||
                        !Admits(target, kind, context.Modifications) ||
                        !TargetQuery.StandsInRelationTo(
                            SourceTeamRelation.OpposingSource, target, context.ActorId,
                            context.Participants, board) ||
                        ImmunityQuery.IsImmuneTo(
                            target.AsRuleObject(), context.EnclosingActivity,
                            actor.AsRuleObject(), board, context.Participants,
                            context.Modifications, context.ImmunityOverrides) ||
                        !board.MapDefinition.PlayableCoordinates.Contains(destination) ||
                        board.TryGetObjectAt(destination, out _) ||
                        context.Modifications.AnyOverlayMakesAnObstacleOf(destination))
                        continue;
                    options.Add(new ThroughOption(target, destination));
                }

                return options.OrderBy(value => value.Target.Kind)
                    .ThenBy(value => RosterPresenceQuery.Identity(value.Target), StringComparer.Ordinal)
                    .ToArray();
            }
        }

        private sealed class FormerTargetSpaceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => FormerTargetSpaceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("space", EffectValueKind.Coordinate) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("space", EffectValue.Space(
                            context.EnclosingAttack.TargetSpaceBeforeConsequences.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class PlayedAttackThisTurnHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PlayedAttackThisTurnV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[] { new EffectCapabilityParameter("hero", EffectValueKind.Hero) },
                    new[] { new EffectOutputBinding("played", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool CanRead(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable && context.CardState.IsAvailable &&
                context.Input("hero").HeroId.IsDefined;
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                !CanRead(context)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("played", EffectValue.Boolean(
                            PlayedAttackCardThisTurnQuery.HasPlayed(
                                context.Input("hero").HeroId,
                                context.RoundIndex,
                                context.TurnIndex,
                                context.RosterState.TurnSlots,
                                context.CardState.Definitions,
                                context.CardState.Instances)))
                    });
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanRead(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class HeroDefeatedThisRoundHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => HeroDefeatedThisRoundV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("defeated", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            public EffectExecutionResult Execute(EffectCapabilityContext context) =>
                !context.RosterState.IsAvailable
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("defeated", EffectValue.Boolean(
                            HeroDefeatedThisRoundQuery.IsSatisfied(context.RosterState.HeroDefeats)))
                    });
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class PresenceCountHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PresenceCountV1;
            public EffectCapabilityParameterSchema Schema { get; } = TargetSchema(
                new[]
                {
                    new EffectCapabilityParameter("excludeActor", EffectValueKind.Boolean),
                    new EffectCapabilityParameter("excludeTarget", EffectValueKind.Boolean)
                },
                new[]
                {
                    new EffectOutputBinding("count", EffectValueKind.Integer),
                    new EffectOutputBinding("present", EffectValueKind.Boolean)
                });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static int Count(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var distance = context.Input(DistanceParameter).IntegerValue;
                var kind = (RosterUnitKind)context.Input(UnitKindParameter).IntegerValue;
                var relation = (SourceTeamRelation)context.Input(RelationParameter).IntegerValue;
                if (board == null || context.Participants == null || distance < 0 ||
                    kind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), kind) ||
                    relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                    !TryOrigin(
                        context, (RosterQueryOrigin)context.Input(OriginParameter).IntegerValue,
                        out var at))
                    return -1;
                var excludedActor = context.Input("excludeActor").BooleanValue
                    ? BoardObjectReference.ForHero(context.ActorId)
                    : default;
                var excludedTarget = context.Input("excludeTarget").BooleanValue
                    ? EnclosingTarget(context)
                    : default;
                return RosterPresenceQuery.FindUnitsWithin(
                    at,
                    distance,
                    kind,
                    relation,
                    context.ActorId,
                    board,
                    context.Participants,
                    context.Modifications,
                    excludedActor,
                    excludedTarget).Count;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var count = Count(context);
                return count < 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("count", EffectValue.Integer(count)),
                        new EffectOutcomeFact("present", EffectValue.Boolean(count > 0))
                    });
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Count(context) < 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.Applied;
        }

        private sealed class PreConsequenceAdjacencyHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => PreConsequenceAdjacencyV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("wasAdjacent", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool? Read(EffectCapabilityContext context)
            {
                var targetAt = context.EnclosingAttack.TargetSpaceBeforeConsequences;
                return targetAt.HasValue && context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt)
                    ? (bool?)(HexDistanceQuery.Calculate(actorAt, targetAt.Value) == 1)
                    : null;
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("wasAdjacent", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        /// <summary>
        /// Milestone 8.20. Whether two named spaces are rulebook-adjacent, published as a boolean a
        /// conditional can read. Nothing about the board is consulted: p.19's adjacency is a property
        /// of two coordinates, and both arrive bound from earlier nodes.
        /// </summary>
        private sealed class SpacesAdjacentHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => SpacesAdjacentV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("first", EffectValueKind.Coordinate),
                        new EffectCapabilityParameter("second", EffectValueKind.Coordinate)
                    },
                    new[] { new EffectOutputBinding("adjacent", EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var first = context.Input("first");
                var second = context.Input("second");
                return first.Kind == EffectValueKind.Coordinate &&
                    second.Kind == EffectValueKind.Coordinate
                    ? (bool?)(HexDistanceQuery.Calculate(first.Coordinate, second.Coordinate) == 1)
                    : null;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("adjacent", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class AdjacentTerrainPresenceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => AdjacentTerrainPresenceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    outputs: new[] { new EffectOutputBinding("present", EffectValueKind.Boolean) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            // Milestone 8.19c routed this through the one terrain question, so an effective-type
            // overlay reaches it: a hero petrified into terrain is terrain to stand beside, exactly as
            // the printed rock is. Reading the map alone was correct while the map was the only source
            // and would silently have been half an answer once a card made a second one.
            private static bool? Read(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                if (board == null || !board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out var actorAt)) return null;
                return TerrainPresenceQuery.IsAdjacentToTerrain(board, actorAt, context.Modifications);
            }
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact("present", EffectValue.Boolean(value.Value))
                    })
                    : EffectExecutionResult.ImpossibleMandatory();
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Read(context).HasValue
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class RetrieveCardHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => RetrieveCardV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("owner", EffectValueKind.Hero),
                        new EffectCapabilityParameter("chooser", EffectValueKind.Hero),
                        new EffectCapabilityParameter("kind", EffectValueKind.Integer)
                    },
                    new[] { new EffectOutputBinding("retrieved", EffectValueKind.CardInstance) });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<CardInstance> Candidates(EffectCapabilityContext context)
            {
                var owner = context.Input("owner").HeroId;
                var kind = (RosterCardRetrievalKind)context.Input("kind").IntegerValue;
                if (!context.CardState.IsAvailable || !owner.IsDefined ||
                    kind == RosterCardRetrievalKind.None ||
                    !Enum.IsDefined(typeof(RosterCardRetrievalKind), kind))
                    return Array.Empty<CardInstance>();
                var definitions = context.CardState.Definitions;
                return context.CardState.Instances
                    .Where(card => card.OwnerId.Equals(owner) && Eligible(card, kind, definitions))
                    .OrderBy(card => card.Id.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                var chooser = context.Input("chooser").HeroId;
                return candidates.Count == 0 || !IsKnownHero(context, chooser)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None, candidates.Select(card => EffectValue.Card(card.Id)),
                        1, 1, chooser));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var card = selected?.Count == 1 && selected[0].Kind == EffectValueKind.CardInstance
                    ? Candidates(context).SingleOrDefault(value => value.Id.Equals(
                        selected[0].CardInstanceId))
                    : null;
                if (card == null || !CardLifecycleAuthority.TryRetrieveEligibleCard(card))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("retrieved", EffectValue.Card(card.Id))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ||
                !IsKnownHero(context, context.Input("chooser").HeroId)
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            private static bool Eligible(
                CardInstance card,
                RosterCardRetrievalKind kind,
                IReadOnlyList<CardDefinition> definitions)
            {
                var isRetrievableDiscard =
                    card.State.Location == CardLocation.DiscardPile &&
                    !card.State.HasBeenPlayed &&
                    card.State.ResolutionStatus == CardResolutionStatus.None;
                if (kind == RosterCardRetrievalKind.OwnedDiscardedCard)
                    return isRetrievableDiscard;

                var definition = definitions.SingleOrDefault(value =>
                    value.Reference.Equals(card.DefinitionReference));
                var primary = definition?.ActionDefinitions.SingleOrDefault(value =>
                    value.Origin == ActionDefinitionOrigin.PrintedPrimary);
                if (definition == null || !definition.Progression.IsBasic ||
                    primary?.Kind != ActionKind.Skill) return false;
                return isRetrievableDiscard ||
                    (card.State.Location == CardLocation.TurnArea && card.State.HasBeenPlayed &&
                     card.State.ResolutionStatus == CardResolutionStatus.Resolved);
            }
        }

        private sealed class GainResourceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => GainResourceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("recipient", EffectValueKind.Hero),
                        new EffectCapabilityParameter("amount", EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();
            private static bool CanApply(EffectCapabilityContext context) =>
                context.RosterState.IsAvailable && context.Input("recipient").HeroId.IsDefined &&
                context.RosterState.Coins.CanGrant(
                    context.Input("recipient").HeroId, context.Input("amount").IntegerValue);
            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                if (!CanApply(context) || !context.RosterState.Coins.TryGrant(
                    context.Input("recipient").HeroId,
                    context.Input("amount").IntegerValue,
                    CoinGrantKind.CardEffect,
                    out var operation))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("recipient", EffectValue.Hero(operation.RecipientHeroId)),
                    new EffectOutcomeFact("amount", EffectValue.Integer(operation.Amount))
                });
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected) =>
                throw new InvalidOperationException("This capability never requests a choice.");
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                CanApply(context)
                    ? EffectExecutionResultKind.Applied
                    : EffectExecutionResultKind.ImpossibleMandatory;
        }

        private sealed class TransferResourceHandler : IEffectCapabilityHandler
        {
            public EffectCapabilityReference Reference => TransferResourceV1;
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter("source", EffectValueKind.Hero),
                        new EffectCapabilityParameter("recipient", EffectValueKind.Hero),
                        new EffectCapabilityParameter("chooser", EffectValueKind.Hero),
                        new EffectCapabilityParameter("minimum", EffectValueKind.Integer),
                        new EffectCapabilityParameter("maximum", EffectValueKind.Integer)
                    },
                    new[]
                    {
                        new EffectOutputBinding("source", EffectValueKind.Hero),
                        new EffectOutputBinding("recipient", EffectValueKind.Hero),
                        new EffectOutputBinding("amount", EffectValueKind.Integer)
                    });
            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static IReadOnlyList<int> Candidates(EffectCapabilityContext context)
            {
                if (!context.RosterState.IsAvailable) return Array.Empty<int>();
                var source = context.Input("source").HeroId;
                var recipient = context.Input("recipient").HeroId;
                var minimum = context.Input("minimum").IntegerValue;
                var maximum = context.Input("maximum").IntegerValue;
                if (!source.IsDefined || !recipient.IsDefined || source.Equals(recipient) ||
                    minimum < 1 || maximum < minimum) return Array.Empty<int>();
                var ledger = context.RosterState.Coins;
                if (!ledger.TryGetCoins(source, out var sourceBalance) ||
                    !ledger.TryGetCoins(recipient, out var recipientBalance))
                    return Array.Empty<int>();
                var admittedMaximum = Math.Min(maximum, sourceBalance);
                if (admittedMaximum < minimum) return Array.Empty<int>();
                return Enumerable.Range(minimum, admittedMaximum - minimum + 1)
                    .Where(amount => (long)recipientBalance + amount <= int.MaxValue)
                    .ToArray();
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                var chooser = context.Input("chooser").HeroId;
                return candidates.Count == 0 || !IsKnownHero(context, chooser)
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.None, candidates.Select(EffectValue.Integer), 1, 1, chooser));
            }
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Integer ||
                    !Candidates(context).Contains(selected[0].IntegerValue) ||
                    !context.RosterState.Coins.TryTransfer(
                        context.Input("source").HeroId,
                        context.Input("recipient").HeroId,
                        selected[0].IntegerValue,
                        out var operation))
                    return EffectExecutionResult.ImpossibleMandatory();
                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("source", EffectValue.Hero(operation.SourceHeroId)),
                    new EffectOutcomeFact("recipient", EffectValue.Hero(operation.RecipientHeroId)),
                    new EffectOutcomeFact("amount", EffectValue.Integer(operation.Amount))
                });
            }
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0 ||
                !IsKnownHero(context, context.Input("chooser").HeroId)
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;
        }

        private static bool IsKnownHero(EffectCapabilityContext context, HeroId heroId) =>
            heroId.IsDefined && context.Participants != null &&
            context.Participants.Heroes.Any(hero => hero.Id.Equals(heroId));

        private static bool TryOrigin(
            EffectCapabilityContext context,
            RosterQueryOrigin origin,
            out HexCoordinate coordinate)
        {
            coordinate = default;
            var board = context.Components?.Board;
            if (board == null) return false;
            if (origin == RosterQueryOrigin.Actor)
                return board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(context.ActorId), out coordinate);
            if (origin == RosterQueryOrigin.EnclosingTargetBeforeConsequences &&
                context.EnclosingAttack.TargetSpaceBeforeConsequences.HasValue)
            {
                coordinate = context.EnclosingAttack.TargetSpaceBeforeConsequences.Value;
                return true;
            }
            return false;
        }

        private static BoardObjectReference EnclosingTarget(EffectCapabilityContext context)
        {
            if (context.EnclosingAttack.HeroTargetId.IsDefined)
                return BoardObjectReference.ForHero(context.EnclosingAttack.HeroTargetId);
            if (context.EnclosingAttack.MinionTargetId.IsDefined)
                return BoardObjectReference.ForMinion(context.EnclosingAttack.MinionTargetId);
            return default;
        }

        private static bool Admits(
            BoardObjectReference target, RosterUnitKind kind, RuleModificationSet modifications)
        {
            if (!target.AsRuleObject().IsDefined) return false;
            if (kind == RosterUnitKind.Unit) return true;
            return EffectiveUnitKindQuery.Is(
                target.AsRuleObject(),
                kind == RosterUnitKind.Hero ? EffectiveUnitKind.Hero : EffectiveUnitKind.Minion,
                modifications);
        }

        private static IReadOnlyList<BoardObjectReference> Canonical(
            IEnumerable<BoardObjectReference> values) =>
            values.OrderBy(value => value.Kind)
                .ThenBy(value => RosterPresenceQuery.Identity(value), StringComparer.Ordinal)
                .ToArray();

        private static bool IsStraightRoute(
            HexCoordinate origin,
            HexDirection direction,
            IReadOnlyList<HexCoordinate> route)
        {
            if (route == null || route.Count < 2 || !route[0].Equals(origin)) return false;
            for (var index = 1; index < route.Count; index++)
            {
                if (!route[index].Equals(StraightLineQuery.Step(origin, direction, index)))
                    return false;
            }
            return true;
        }

        private static IReadOnlyList<RuleActivity> AllActivities { get; } =
            Array.AsReadOnly(Enum.GetValues(typeof(RuleActivity)).Cast<RuleActivity>()
                .Where(value => value != RuleActivity.None).ToArray());

        private static IReadOnlyList<RuleActivity> SwapActivity { get; } =
            Array.AsReadOnly(new[] { RuleActivity.Swap });

        private sealed class ChargeOption
        {
            public ChargeOption(
                HexCoordinate destination,
                IReadOnlyList<BoardObjectReference> targets)
            {
                Destination = destination;
                Targets = targets;
            }
            public HexCoordinate Destination { get; }
            public IReadOnlyList<BoardObjectReference> Targets { get; }
        }

        private sealed class ThroughOption
        {
            public ThroughOption(BoardObjectReference target, HexCoordinate destination)
            {
                Target = target;
                Destination = destination;
            }
            public BoardObjectReference Target { get; }
            public HexCoordinate Destination { get; }
        }
    }
}
