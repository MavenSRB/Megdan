using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 6.8k's target-selection capability: an effect definition's typed way to name another
    /// board object.
    ///
    /// It is an ordinary registered capability rather than a new node kind because the acceptance text
    /// requires the chosen space to reach a later node through the existing node-output binding - the
    /// selection pauses through the same capability-choice seam every other chooser-owned decision uses,
    /// and publishes one <see cref="EffectValueKind.Coordinate"/> output. The coordinate is the subject
    /// identity milestone 6.4 chose for displacement: one obstacle per space makes the object standing
    /// there nameable without widening the typed value union, which is how a token - unrepresentable as
    /// a rules object by p.19 - stays expressible.
    ///
    /// Its candidate set comes from <see cref="TargetQuery"/>, the reusable targeting contract, rather
    /// than effect-local geometry. Immunity therefore filters selection - p.18 makes it a targeting
    /// rule - while a presence question over the same spaces still counts the immune unit. The activity
    /// consulted is declared by revision 4. Revision 3 is bound to Skill; definitions feeding a
    /// consuming effect with its own activity use revision 4 so the offer and final operation ask the
    /// identical immunity question.
    ///
    /// Milestone 6.8l replaced revisions 1 and 2 with 3 and 4, which declare one further optional
    /// parameter: the node this selection selects for. A legal target and a target the consuming step
    /// can act on are different questions, and until the selection could name its consumer only the
    /// first was asked - so a branch behind one was offered whatever the step after it would do. The
    /// revisions moved rather than being joined because the registration list records contracts, the
    /// same reason milestone 6.8k retired direct Move revision 1 when it widened the subject.
    ///
    /// This is the seam 6.5b and 6.6a reuse: Red Needle's Push, Red Bombard's direct Move of another
    /// adjacent unit, and Blue Sentinel's Swap all name their object through this one selection rather
    /// than each inventing a binding.
    /// </summary>
    public static class SyntheticTargetingCapabilities
    {
        public static EffectCapabilityId ChooseTarget { get; } =
            new EffectCapabilityId("synthetic-choose-target");

        /// <summary>
        /// Revision 3, minted at milestone 6.8l: revision 1's declaration plus an optional consumer.
        /// The enclosing action's activity is what it asks immunity for.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV3 =>
            new EffectCapabilityReference(ChooseTarget, 3);

        /// <summary>
        /// Revision 4, minted at milestone 6.8l: revision 2's declaration plus an optional consumer.
        /// The activity it asks immunity for is the definition's, for an operation p.18 gives one.
        /// </summary>
        public static EffectCapabilityReference ChooseTargetV4 =>
            new EffectCapabilityReference(ChooseTarget, 4);

        /// <summary>
        /// Milestone 7.2b. The same selection bounded by rulebook p.9's printed Radius icon instead of
        /// a number the definition writes down.
        ///
        /// A second capability ID rather than a third revision of the one above, for the reason the
        /// named Push is a second ID beside the batch Push: these are two live declarations of
        /// different content, not two versions of one. Where a card's text says "choose a unit within
        /// 1 space" the range is card text and the definition states it; where the card prints a Radius
        /// icon, p.9 says "the value on the Range/Radius icon determines the maximum Range/Radius of
        /// your action. If the card text mentions Range/Radius, it refers to this value" - so the bound
        /// is printed content the capability reads, and p.16's items raise it. A card cannot hold both
        /// readings of one parameter, which is exactly the ambiguity the definition validator refuses.
        /// </summary>
        public static EffectCapabilityId RadiusTarget { get; } =
            new EffectCapabilityId("synthetic-radius-target");

        public static EffectCapabilityReference RadiusTargetV1 =>
            new EffectCapabilityReference(RadiusTarget, 1);

        /// <summary>
        /// Milestone 8.6. The printed-Range twin of <see cref="RadiusTarget"/>, for card text that says
        /// "in range" rather than "in radius".
        ///
        /// A second capability ID rather than a parameter on the one above, for the reason p.9 gives
        /// two icons two meanings and p.16 two items: a card prints one of them, and a definition that
        /// could name either through the same parameter would let a Radius card read a Range item. The
        /// handler beneath is nevertheless shared - the two differ only in which
        /// <see cref="PrintedValueChannel"/> they read - so no geometry, immunity or item composition
        /// is written twice.
        ///
        /// It declares one thing the Radius selection does not: p.19's straight-line complement, which
        /// Wasp's Telekinesis, Mass Telekinesis and both Boomerangs print as "in range, which is not in
        /// a straight line". No verified card pairs that filter with a Radius, so the Radius
        /// declaration does not carry the parameter rather than carrying one nothing sets.
        /// </summary>
        public static EffectCapabilityId RangeTarget { get; } =
            new EffectCapabilityId("synthetic-range-target");

        public static EffectCapabilityReference RangeTargetV1 =>
            new EffectCapabilityReference(RangeTarget, 1);

        /// <summary>
        /// Milestone 8.10. The printed-Radius selection asked about heroes rather than about board
        /// objects, for card text that names one: Wasp's Shock, Electrocute and Electroblast each print
        /// "An enemy hero in radius and not adjacent to you".
        ///
        /// <para>A second capability ID rather than a revision of the Radius selection above, by the
        /// same rule that made the Range twin one: these are two live declarations of different
        /// content. That one offers every board object p.19 admits and publishes the space it stands
        /// on, because what consumes it is a displacement and a token has no other name; this one
        /// offers heroes and publishes the hero, because what consumes it is a discard and a space
        /// cannot be asked for a card. The handler beneath is nevertheless the same one, exactly as it
        /// is for the Range twin - so p.9's disk, p.18's immunity and p.16's item composition are
        /// written once, and milestone 7.9's deferral gate keeps holding: this slice registers a hero's
        /// declaration on an implementation the showcase already proves, rather than introducing an
        /// implementation inside a hero slice.</para>
        ///
        /// <para>It declares two things the object selection does not, and both are printed words
        /// rather than defaults: the side of the board the hero must be on - "enemy", read from the
        /// acting hero's own perspective - and p.9's disk without its innermost ring, which is what
        /// "and not adjacent to you" removes.</para>
        /// </summary>
        public static EffectCapabilityId RadiusHeroTarget { get; } =
            new EffectCapabilityId("synthetic-radius-hero-target");

        public static EffectCapabilityReference RadiusHeroTargetV1 =>
            new EffectCapabilityReference(RadiusHeroTarget, 1);

        /// <summary>
        /// Milestone 8.11. The printed-Range twin of <see cref="RadiusHeroTarget"/>, for card text that
        /// names a hero "in range": Wasp's Deflect Projectiles and Reflect Projectiles.
        ///
        /// <para>A second capability ID rather than a parameter, by the rule that made the object
        /// selections two: p.9 gives the two icons two meanings and p.16 two items, so a definition
        /// that could name either through one parameter would let a Range card read a Radius item. The
        /// handler beneath is the same one all four printed-icon declarations share.</para>
        ///
        /// <para>It declares one thing the Radius twin does not - whether the attacker is excluded -
        /// and the Radius twin declares one this does not, p.9's disk without its innermost ring.
        /// Neither carries a parameter nothing sets: no verified card pairs "in range" with "not
        /// adjacent", and none pairs "in radius" with "other than the attacker".</para>
        /// </summary>
        public static EffectCapabilityId RangeHeroTarget { get; } =
            new EffectCapabilityId("synthetic-range-hero-target");

        public static EffectCapabilityReference RangeHeroTargetV1 =>
            new EffectCapabilityReference(RangeHeroTarget, 1);

        public static EffectCapabilityId RadiusMinionTarget { get; } =
            new EffectCapabilityId("synthetic-radius-minion-target");

        public static EffectCapabilityReference RadiusMinionTargetV1 =>
            new EffectCapabilityReference(RadiusMinionTarget, 1);

        /// <summary>
        /// Milestone 8.20. The printed-Range twin of <see cref="RadiusMinionTarget"/>, for card text
        /// that names a minion "in range": Arien's Arcane Whirlpool and Ebb and Flow, "Swap with an
        /// enemy minion in range".
        ///
        /// <para>A second capability ID rather than a parameter, by the rule the other three pairs
        /// here already record: p.9 gives the two icons two meanings and p.16 two items, so a
        /// definition that could name either through one parameter would let a Range card read a Radius
        /// item. The handler beneath is the same one all five printed-icon declarations share.</para>
        /// </summary>
        public static EffectCapabilityId RangeMinionTarget { get; } =
            new EffectCapabilityId("synthetic-range-minion-target");

        public static EffectCapabilityReference RangeMinionTargetV1 =>
            new EffectCapabilityReference(RangeMinionTarget, 1);

        /// <summary>
        /// Milestone 8.20. The chosen minion's current space, published beside its identity.
        ///
        /// <para>Rulebook p.19's Swap exchanges two <em>spaces</em>, and every repositioning operation
        /// in this build names its subject by the space it stands on - which p.19's one-obstacle-per-
        /// space rule makes unambiguous. So a selection whose consumer is a Swap has to publish both:
        /// the minion, because the card's sentence names one, and where it is standing, because the
        /// operation takes that. The board-object selection has published a space since milestone
        /// 7.2b; this is the same output on the minion arm rather than a second idea.</para>
        /// </summary>
        public const string ChosenMinionSpaceOutputName = "chosenMinionSpace";

        public static EffectCapabilityId HeroAdjacentToMinion { get; } =
            new EffectCapabilityId("synthetic-hero-adjacent-to-minion");

        public static EffectCapabilityReference HeroAdjacentToMinionV1 =>
            new EffectCapabilityReference(HeroAdjacentToMinion, 1);

        /// <summary>
        /// The straight-line filter parameter's name, spelled once so a definition cannot mistype it.
        /// </summary>
        public const string ExcludeStraightLineParameterName = "excludeStraightLine";

        /// <summary>
        /// The adjacency filter parameter's name, spelled once so a definition cannot mistype it.
        /// Milestone 8.10.
        /// </summary>
        public const string ExcludeAdjacentParameterName = "excludeAdjacent";

        /// <summary>
        /// The attacker-exclusion parameter's name, spelled once so a definition cannot mistype it.
        /// Milestone 8.11, for Deflect Projectiles' "other than the attacker".
        /// </summary>
        public const string ExcludeTheAttackerParameterName = "excludeTheAttacker";

        /// <summary>
        /// The side-of-the-board parameter's name, carrying a <see cref="SourceTeamRelation"/>.
        /// Milestone 8.10.
        /// </summary>
        public const string TargetRelationParameterName = "relation";

        /// <summary>The hero a printed-Radius hero selection chose.</summary>
        public const string ChosenHeroOutputName = "chosenHero";

        public const string ChosenMinionOutputName = "chosenMinion";

        public const string MinionParameterName = "minion";

        private static IReadOnlyList<RuleActivity> AllActivities { get; } =
            Array.AsReadOnly(Enum.GetValues(typeof(RuleActivity)).Cast<RuleActivity>()
                .Where(value => value != RuleActivity.None).ToArray());

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new ChooseTargetHandler(ChooseTargetV3, declaresActivity: false),
                new ChooseTargetHandler(ChooseTargetV4, declaresActivity: true),
                new PrintedIconTargetHandler(
                    RadiusTargetV1, PrintedValueChannel.Radius, declaresStraightLineFilter: false),
                new PrintedIconTargetHandler(
                    RangeTargetV1, PrintedValueChannel.Range, declaresStraightLineFilter: true),
                new PrintedIconTargetHandler(
                    RadiusHeroTargetV1, PrintedValueChannel.Radius,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RangeHeroTargetV1, PrintedValueChannel.Range,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Hero),
                new PrintedIconTargetHandler(
                    RadiusMinionTargetV1, PrintedValueChannel.Radius,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Minion),
                new PrintedIconTargetHandler(
                    RangeMinionTargetV1, PrintedValueChannel.Range,
                    declaresStraightLineFilter: false, PrintedIconSelectionSubject.Minion),
                new HeroAdjacentToMinionHandler()
            };

        /// <summary>
        /// Which kind of thing a printed-icon selection offers, and therefore what it publishes.
        /// Milestone 8.10.
        /// </summary>
        private enum PrintedIconSelectionSubject
        {
            /// <summary>Milestone 7.2b's: every board object in the disk, published as its space.</summary>
            BoardObject = 0,

            /// <summary>The heroes on a declared side of it, published as the hero.</summary>
            Hero = 1
            ,
            Minion = 2
        }

        /// <summary>
        /// Every board object the acting hero may target inside its card's printed icon, with the
        /// origin the disk was measured from.
        ///
        /// <para>Shared by both selections this class builds on p.9's icons - milestone 7.2b's, which
        /// publishes the chosen object's space, and milestone 8.10's, which publishes the chosen hero -
        /// so p.9's disk, p.18's immunity and p.16's item composition are asked in exactly one place
        /// and the two selections cannot come to disagree about who is inside a card's icon.</para>
        ///
        /// <para>The total is re-derived on every ask rather than captured, which is what makes the
        /// offered disk and the disk an answer is checked against the same one: an item gained between
        /// the two would widen both, and neither is written to the snapshot.</para>
        /// </summary>
        private static IReadOnlyList<BoardObjectReference> PrintedIconTargets(
            EffectCapabilityContext context,
            PrintedValueChannel channel,
            bool includeSelf,
            out HexCoordinate origin)
        {
            origin = default;
            var components = context.Components;
            if (components == null || !components.CanDisplace || !context.CardState.IsAvailable)
            {
                return Array.Empty<BoardObjectReference>();
            }

            var activity = (RuleActivity)context.Input("activity").IntegerValue;
            if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
            {
                return Array.Empty<BoardObjectReference>();
            }

            var board = components.Board;
            var selector = BoardObjectReference.ForHero(context.ActorId);
            if (!board.TryGetObjectCoordinate(selector, out origin))
            {
                return Array.Empty<BoardObjectReference>();
            }

            var printedTotal = channel == PrintedValueChannel.Radius
                ? RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems)
                : RangeValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications,
                    // Milestone 8.19c. Card text resolving inside an action asks for the same Range
                    // total that action's own targeting asked for, so the class is read from the card
                    // and action this node is inside rather than assumed from the node.
                    ActionValueApplicabilityQuery.For(
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions));
            if (printedTotal.Total <= 0)
            {
                // A card printing no icon of this channel has no such bound at all, and p.16 does
                // not give it one: an item raises a printed icon and never invents one. The
                // selection is therefore impossible rather than adjacent-by-default.
                return Array.Empty<BoardObjectReference>();
            }

            // p.9 states one geometry for both icons - "targets up to 3 spaces away", counted
            // through obstacles because there are no line-of-sight rules - so the Range reading
            // asks the same disk question. RadiusQuery is where that question lives and it refuses
            // a total of the wrong channel by construction, which is why the Range arm asks
            // TargetQuery, the shared contract underneath it, rather than mislabelling its total.
            return channel == PrintedValueChannel.Radius
                ? RadiusQuery.Targets(
                    origin,
                    printedTotal,
                    includeSelf,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity)
                : TargetQuery.FindTargetsWithin(
                    origin,
                    printedTotal.Total,
                    includeSelf,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity);
        }

        /// <summary>
        /// Milestone 7.2b's printed-icon selection: the same chooser-owned target selection, whose
        /// bound is the acting card's Radius or Range Total rather than an integer the definition
        /// carries.
        ///
        /// It states no range parameter at all, which is the whole point - there is nothing for a
        /// definition to write down, because p.9 puts the value on the card. What it does state is the
        /// activity it targets under, since p.18 declares immunity per activity and this selection
        /// filters immunity through the same <see cref="TargetQuery"/> the capability above uses. The
        /// disk itself comes from <see cref="RadiusQuery"/>, and the total from
        /// <see cref="RadiusValueQuery"/> or <see cref="RangeValueQuery"/>, so neither the geometry nor
        /// the item composition is repeated here.
        ///
        /// Milestone 8.6 gave it the channel and the optional straight-line complement as constructor
        /// state, exactly as <see cref="ChooseTargetHandler"/> takes its activity declaration: two
        /// capability IDs, one implementation, and no way for the two icons' selections to disagree
        /// about anything except which icon they read.
        /// </summary>
        private sealed class PrintedIconTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectImmunityOverrideCapabilityHandler
        {
            private readonly PrintedValueChannel channel;
            private readonly bool declaresStraightLineFilter;
            private readonly PrintedIconSelectionSubject subject;

            public PrintedIconTargetHandler(
                EffectCapabilityReference reference,
                PrintedValueChannel channel,
                bool declaresStraightLineFilter,
                PrintedIconSelectionSubject subject = PrintedIconSelectionSubject.BoardObject)
            {
                Reference = reference;
                this.channel = channel;
                this.declaresStraightLineFilter = declaresStraightLineFilter;
                this.subject = subject;

                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("activity", EffectValueKind.Integer)
                };
                if (subject == PrintedIconSelectionSubject.BoardObject)
                {
                    // Only an object selection can be asked about the actor's own space; a selection
                    // that names heroes on a declared side answers that question through the side.
                    parameters.Insert(0, new EffectCapabilityParameter(
                        "includeSelf", EffectValueKind.Boolean));
                }
                else if (subject == PrintedIconSelectionSubject.Hero)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        TargetRelationParameterName, EffectValueKind.Integer));

                    // Milestone 8.11. Each icon's hero selection declares the one narrowing a verified
                    // card pairs with it and no other: p.9's disk without its innermost ring for the
                    // Radius cards' "not adjacent to you", and the attacker's exclusion for the Range
                    // cards' "other than the attacker". Declaring both on both would carry a parameter
                    // nothing sets, which is the ambiguity the Range/Radius split exists to refuse.
                    parameters.Add(channel == PrintedValueChannel.Radius
                        ? new EffectCapabilityParameter(
                            ExcludeAdjacentParameterName, EffectValueKind.Boolean)
                        : new EffectCapabilityParameter(
                            ExcludeTheAttackerParameterName, EffectValueKind.Boolean));
                }
                else
                {
                    parameters.Add(new EffectCapabilityParameter(
                        TargetRelationParameterName, EffectValueKind.Integer));
                }

                if (declaresStraightLineFilter)
                {
                    parameters.Add(new EffectCapabilityParameter(
                        ExcludeStraightLineParameterName, EffectValueKind.Boolean));
                }

                parameters.Add(new EffectCapabilityParameter(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValueKind.Node,
                    isRequired: false));
                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[]
                    {
                        subject == PrintedIconSelectionSubject.BoardObject
                            ? new EffectOutputBinding("chosen", EffectValueKind.Coordinate)
                            : subject == PrintedIconSelectionSubject.Hero
                                ? new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero)
                                : new EffectOutputBinding(ChosenMinionOutputName, EffectValueKind.Minion)
                    }
                    .Concat(subject == PrintedIconSelectionSubject.Minion
                        // Milestone 8.20. The minion arm publishes where its answer is standing as
                        // well as who it is, because a Swap takes a space and the card's sentence
                        // names a minion. A node that does not declare it never sees it.
                        ? new[]
                        {
                            new EffectOutputBinding(
                                ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                        }
                        : Enumerable.Empty<EffectOutputBinding>()));
            }

            public EffectCapabilityReference Reference { get; }

            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities =>
                AllActivities;

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = AnticipateCandidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (subject == PrintedIconSelectionSubject.Hero)
                {
                    if (selected == null || selected.Count != 1 ||
                        selected[0].Kind != EffectValueKind.Hero ||
                        !HeroCandidates(context).Contains(selected[0].HeroId))
                    {
                        return EffectExecutionResult.ImpossibleMandatory();
                    }

                    return EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                    });
                }

                if (subject == PrintedIconSelectionSubject.Minion)
                {
                    if (selected == null || selected.Count != 1 ||
                        selected[0].Kind != EffectValueKind.Minion ||
                        !MinionCandidates(context).Contains(selected[0].MinionId))
                    {
                        return EffectExecutionResult.ImpossibleMandatory();
                    }

                    var chosenMinion = BoardObjectReference.ForMinion(selected[0].MinionId);
                    var minionOutcomes = new List<EffectOutcomeFact>
                    {
                        new EffectOutcomeFact(
                            ChosenMinionOutputName, EffectValue.Minion(selected[0].MinionId))
                    };
                    // Published for a node that asked, and only for one: Wasp's High Voltage names the
                    // minion it defeats and wants no space, while Arien's Swap takes a space and wants
                    // no name. See EffectCapabilityContext.DeclaredOutputs.
                    if (context.Publishes(ChosenMinionSpaceOutputName) &&
                        context.Components?.Board != null &&
                        context.Components.Board.TryGetObjectCoordinate(chosenMinion, out var minionAt))
                    {
                        minionOutcomes.Add(new EffectOutcomeFact(
                            ChosenMinionSpaceOutputName, EffectValue.Space(minionAt)));
                    }

                    return EffectExecutionResult.Applied(minionOutcomes);
                }

                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                subject == PrintedIconSelectionSubject.Hero
                    ? HeroCandidates(context).Select(EffectValue.Hero).ToArray()
                    : subject == PrintedIconSelectionSubject.Minion
                        ? MinionCandidates(context).Select(EffectValue.Minion).ToArray()
                    : Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            private IReadOnlyList<MinionId> MinionCandidates(EffectCapabilityContext context)
            {
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<MinionId>();
                }

                var targets = PrintedIconTargets(context, channel, includeSelf: false, out _);
                var board = context.Components?.Board;
                if (board == null) return Array.Empty<MinionId>();

                return targets
                    .Where(target => target.Kind == BoardObjectKind.Minion &&
                        TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board))
                    .Select(target => target.MinionId)
                    .ToArray();
            }

            /// <summary>
            /// Milestone 8.10. The heroes on the declared side of the board inside the same disk, which
            /// is what card text naming "an enemy hero in radius" chooses among.
            ///
            /// <para>The acting hero is never among them: the disk is asked without self, and the side
            /// would exclude them in any case. An empty result is
            /// <see cref="EffectExecutionResultKind.ImpossibleMandatory"/> rather than a decline,
            /// because by p.10 the sentence is one mandatory step and a step with nobody to perform it
            /// is one that cannot complete; the `if able` its lower-Tier printings add is about the
            /// card, and is declared on the discard node where the page puts it.</para>
            /// </summary>
            private IReadOnlyList<HeroId> HeroCandidates(EffectCapabilityContext context)
            {
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<HeroId>();
                }

                var targets = PrintedIconTargets(context, channel, includeSelf: false, out var origin);
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<HeroId>();
                }

                var excludeAdjacent = channel == PrintedValueChannel.Radius &&
                    context.Input(ExcludeAdjacentParameterName).BooleanValue;

                // Milestone 8.11. "other than the attacker", which only a Block's rider prints and only
                // a Range selection therefore declares. The attacker is the enclosing Attack's own
                // actor - a fact about the action this text is answering rather than about anything
                // the text did - so it is read from the same context p.13's subtype is.
                var excludedAttacker = channel == PrintedValueChannel.Range &&
                    context.Input(ExcludeTheAttackerParameterName).BooleanValue
                        ? context.EnclosingAttack.ActorId
                        : default;

                var heroes = new List<HeroId>(targets.Count);
                foreach (var target in targets)
                {
                    if (target.Kind != BoardObjectKind.Hero ||
                        !TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board))
                    {
                        continue;
                    }

                    if (excludedAttacker.IsDefined && target.HeroId.Equals(excludedAttacker))
                    {
                        continue;
                    }

                    // p.9's disk without its innermost ring, which is what "and not adjacent to you"
                    // removes. It is stated as the exact distance the word means rather than as
                    // "further than one", so it says nothing about the actor's own space and does not
                    // quietly depend on that space having been excluded above.
                    if (excludeAdjacent &&
                        board.TryGetObjectCoordinate(target, out var at) &&
                        HexDistanceQuery.Calculate(origin, at) == 1)
                    {
                        continue;
                    }

                    heroes.Add(target.HeroId);
                }

                return heroes;
            }

            /// <summary>
            /// The spaces of every object the acting hero may target inside its card's printed icon, in
            /// canonical axial order.
            ///
            /// The total is re-derived on every ask rather than captured, which is what makes the
            /// offered disk and the disk the answer is checked against the same one: an item gained
            /// between the two would widen both, and neither is written to the snapshot.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var targets = PrintedIconTargets(
                    context, channel, context.Input("includeSelf").BooleanValue, out var origin);
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                // p.19's straight-line rule read as a complement, anchored at the selecting hero. It is
                // applied after immunity rather than before it because the two narrow for different
                // reasons and neither is a special case of the other: a unit can be out of line and
                // immune at once, and both refusals are the card's.
                return declaresStraightLineFilter &&
                    context.Input(ExcludeStraightLineParameterName).BooleanValue
                    ? StraightLineQuery.FilterOutStraightLine(origin, spaces)
                    : (IReadOnlyList<HexCoordinate>)spaces;
            }
        }

        private sealed class HeroAdjacentToMinionHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            public EffectCapabilityReference Reference => HeroAdjacentToMinionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(MinionParameterName, EffectValueKind.Minion),
                        new EffectCapabilityParameter(
                            TargetRelationParameterName, EffectValueKind.Integer),
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node, isRequired: false)
                    },
                    new[] { new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Hero).ToArray(), 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Hero ||
                    !Candidates(context).Contains(selected[0].HeroId))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact(
                        ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(EffectValue.Hero).ToArray();

            private static IReadOnlyList<HeroId> Candidates(EffectCapabilityContext context)
            {
                var board = context.Components?.Board;
                var minionId = context.Input(MinionParameterName).MinionId;
                var relation = (SourceTeamRelation)context
                    .Input(TargetRelationParameterName).IntegerValue;
                if (board == null || !minionId.IsDefined || relation == SourceTeamRelation.None ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                {
                    return Array.Empty<HeroId>();
                }

                var minion = BoardObjectReference.ForMinion(minionId);
                if (!board.TryGetObjectCoordinate(minion, out var origin))
                    return Array.Empty<HeroId>();

                return board.Occupancies
                    .Where(occupancy =>
                        HexDistanceQuery.Calculate(origin, occupancy.Coordinate) == 1 &&
                        TargetQuery.StandsInRelationTo(
                            relation, BoardObjectReference.ForHero(occupancy.HeroId),
                            context.ActorId, context.Participants, board))
                    .Select(occupancy => occupancy.HeroId)
                    .OrderBy(id => id.Value, StringComparer.Ordinal)
                    .ToArray();
            }
        }

        /// <summary>
        /// Offers every targetable object within range of the acting hero as an authoritative
        /// chooser-owned selection and publishes the chosen object's space.
        ///
        /// It always pauses, even on a single candidate: that is the codebase's uniform capability
        /// precedent - direct Move pauses on one destination and an `up to 0` push pauses on one
        /// distance - and it keeps "chooser-owned" honest. Restore re-derives the candidates by
        /// re-asking this handler, exactly as every other capability choice restores. Selecting mutates
        /// nothing; the consuming node does whatever the effect does.
        /// </summary>
        private sealed class ChooseTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler
        {
            private readonly bool declaresActivity;

            public ChooseTargetHandler(
                EffectCapabilityReference reference,
                bool declaresActivity)
            {
                Reference = reference;
                this.declaresActivity = declaresActivity;
                var parameters = new List<EffectCapabilityParameter>
                {
                    new EffectCapabilityParameter("range", EffectValueKind.Integer),
                    new EffectCapabilityParameter("includeSelf", EffectValueKind.Boolean)
                };
                if (declaresActivity)
                {
                    parameters.Add(new EffectCapabilityParameter("activity", EffectValueKind.Integer));
                }

                // Milestone 6.8l. Optional, because a selection whose result no single later node
                // consumes still has a legal shape, and one that names nothing keeps exactly the
                // conservative reading milestone 6.5ac chose. Nothing here reads this parameter: the
                // handler cannot see the definition, so the resolver owns the narrowing and this
                // declaration is what lets a definition state it.
                parameters.Add(new EffectCapabilityParameter(
                    EffectSelectionDeclaration.ConsumerParameterName,
                    EffectValueKind.Node,
                    isRequired: false));

                Schema = new EffectCapabilityParameterSchema(
                    parameters,
                    new[] { new EffectOutputBinding("chosen", EffectValueKind.Coordinate) });
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                if (candidates.Count == 0)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                    EffectChooserRole.Actor,
                    candidates.Select(candidate => EffectValue.Space(candidate)).ToArray(),
                    1,
                    1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Coordinate)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                // Re-derived against current state rather than trusted from the paused choice: an
                // object targetable when the choice opened may have moved or become immune before the
                // answer arrived.
                var chosen = selected[0].Coordinate;
                if (!Candidates(context).Contains(chosen))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    new EffectOutcomeFact("chosen", EffectValue.Space(chosen))
                });
            }

            /// <summary>
            /// The same targeting question <see cref="Execute"/> asks, without opening the selection.
            /// Selecting mutates nothing even when it runs, so what this avoids is not a side effect
            /// but a pause: a probe that executed would leave a pending choice nobody asked for.
            /// </summary>
            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// Milestone 6.8l: the same spaces <see cref="Execute"/> would offer, as the same typed
            /// values, from the same derivation. The resolver asks for them so it can put each one to
            /// the node this selection selects for; what it does with the answer is not this handler's
            /// business, and could not be - a handler sees its own context, never the definition
            /// around it.
            /// </summary>
            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(candidate => EffectValue.Space(candidate)).ToArray();

            /// <summary>
            /// The spaces of every object the acting hero may target, in canonical axial order. Nothing
            /// here walks the board or invents geometry: the reusable targeting contract owns both.
            /// </summary>
            private IReadOnlyList<HexCoordinate> Candidates(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components == null || !components.CanDisplace)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var range = context.Input("range").IntegerValue;
                if (range < 0)
                {
                    return Array.Empty<HexCoordinate>();
                }

                var board = components.Board;
                var selector = BoardObjectReference.ForHero(context.ActorId);
                if (!board.TryGetObjectCoordinate(selector, out var origin))
                {
                    return Array.Empty<HexCoordinate>();
                }

                // Milestone 8.5 (F-BOARD-03). A node that names its own activity still wins - that is
                // what the declaration is for - and one that names none performs as the action it is
                // running inside rather than as the Skill it used to be the only one of.
                var activity = declaresActivity
                    ? (RuleActivity)context.Input("activity").IntegerValue
                    : context.EnclosingActivity;
                if (activity == RuleActivity.None || !Enum.IsDefined(typeof(RuleActivity), activity))
                {
                    return Array.Empty<HexCoordinate>();
                }

                var targets = TargetQuery.FindTargetsWithin(
                    origin,
                    range,
                    context.Input("includeSelf").BooleanValue,
                    selector.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications);

                var spaces = new List<HexCoordinate>(targets.Count);
                foreach (var target in targets)
                {
                    if (board.TryGetObjectCoordinate(target, out var at))
                    {
                        spaces.Add(at);
                    }
                }

                return spaces;
            }
        }
    }
}
