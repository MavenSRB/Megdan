using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's ATTACK-line targeting narrowings - milestone 8.75c. Her seven Attacks name
    /// their own target in card text and let the Tie Breaker coin pick which; the action reaches p.14's
    /// step 1 with no target (<c>AttackTargetBinding.ByCardText</c>) and each coin arm SELECTS a subject
    /// and binds it through <c>set-attack-target@1</c>, exactly as Dodger's modal Attacks do.
    ///
    /// <para>Four of the subjects her Attacks name cannot be expressed by any selection this build
    /// already registers, so all four are minted here as her own capabilities rather than by widening a
    /// registered one - the file that owns the printed-icon family is not this hero's to edit:
    ///
    /// <list type="bullet">
    /// <item><description>"a unit in range IN a straight line" - the printed-Range unit selection with
    /// p.19's straight-line rule read as a REQUIREMENT rather than the complement
    /// <c>synthetic-range-target@1</c>'s <c>excludeStraightLine</c> applies (and which the registered
    /// Range UNIT arm does not carry at all);</description></item>
    /// <item><description>"a unit in range NOT in a straight line" - the same selection with that rule
    /// read as the complement, on the unit arm the registered filter is absent from;</description></item>
    /// <item><description>"a unit at maximum range" - the printed-Range unit selection narrowed to
    /// candidates standing at distance EQUAL to the printed Range Total (RULING 1's reading of "at
    /// maximum range", the same equality Silverarrow's Rain of Arrows carries as an
    /// <c>AttackTargetReach</c> on the action - which a ByCardText Attack cannot use, because it has no
    /// action-side reach to inherit);</description></item>
    /// <item><description>"a hero in range, not adjacent to you" - the printed-Range HERO selection
    /// narrowed by p.9's disk without its innermost ring, which the registered Range hero arm does not
    /// declare (only the Radius hero arm carries <c>excludeAdjacent</c>).</description></item>
    /// </list></para>
    ///
    /// <para><b>What is reused rather than minted.</b> "a hero in range" is
    /// <see cref="SyntheticTargetingCapabilities.RangeHeroTargetV1"/> and every "adjacent to you"
    /// subject is <see cref="SyntheticRosterCapabilities.ChooseTargetV2"/> at a written distance of one
    /// - Dodger's own adjacent-unit reach - so neither is minted here.</para>
    ///
    /// <para><b>Shared disk.</b> All four handlers read p.9's Range icon through
    /// <see cref="RangeValueQuery"/> (items and all) and p.18's immunity through
    /// <see cref="TargetQuery"/>, exactly as <c>PrintedIconTargetHandler</c>'s Range arm does, so this
    /// narrowing cannot come to disagree with the registered family about who is inside a card's icon.
    /// None of it ignores immunity: an Attack targets what it may target.</para>
    ///
    /// <para><b>Milestone 8.88.</b> The three clauses above are now parameters of the printed-icon family
    /// itself (<c>requireStraightLine</c>, <c>excludeStraightLine</c>, <c>atMaximumReach</c> and
    /// <c>excludeAdjacent</c>, on every arm), so a later face printing them binds the family instead of
    /// minting. These four identities stay registered exactly as dealt - a snapshot names them - and
    /// their candidates now come from the family's one derivation with their clauses fixed.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Like every Ignatia declaration, these wait in
    /// <see cref="Handlers"/> until her dealing block moves them into the registered-capability list a
    /// snapshot pins - milestone 7.9's gate.</para>
    /// </summary>
    public static class IgnatiaAttackTargetingContent
    {
        // -----------------------------------------------------------------------------------------
        // Capability identities.
        // -----------------------------------------------------------------------------------------

        /// <summary>"A unit in range in a straight line."</summary>
        public static EffectCapabilityId UnitInStraightLine { get; } =
            new EffectCapabilityId("ignatia-range-unit-in-straight-line");

        public static EffectCapabilityReference UnitInStraightLineV1 =>
            new EffectCapabilityReference(UnitInStraightLine, 1);

        /// <summary>"A unit in range not in a straight line."</summary>
        public static EffectCapabilityId UnitNotInStraightLine { get; } =
            new EffectCapabilityId("ignatia-range-unit-not-in-straight-line");

        public static EffectCapabilityReference UnitNotInStraightLineV1 =>
            new EffectCapabilityReference(UnitNotInStraightLine, 1);

        /// <summary>"A unit at maximum range."</summary>
        public static EffectCapabilityId UnitAtMaximumRange { get; } =
            new EffectCapabilityId("ignatia-range-unit-at-maximum-range");

        public static EffectCapabilityReference UnitAtMaximumRangeV1 =>
            new EffectCapabilityReference(UnitAtMaximumRange, 1);

        /// <summary>"A hero in range, not adjacent to you."</summary>
        public static EffectCapabilityId HeroInRangeNotAdjacent { get; } =
            new EffectCapabilityId("ignatia-range-hero-not-adjacent");

        public static EffectCapabilityReference HeroInRangeNotAdjacentV1 =>
            new EffectCapabilityReference(HeroInRangeNotAdjacent, 1);

        /// <summary>The name a selecting node reads its chosen hero on - the shared family's own spelling.</summary>
        public const string ChosenHeroOutputName = SyntheticTargetingCapabilities.ChosenHeroOutputName;

        /// <summary>The name a selecting node reads its chosen minion on - the shared family's own spelling.</summary>
        public const string ChosenMinionOutputName = SyntheticTargetingCapabilities.ChosenMinionOutputName;

        /// <summary>The side-of-the-board parameter, carried under the shared family's name.</summary>
        public const string TargetRelationParameterName =
            SyntheticTargetingCapabilities.TargetRelationParameterName;

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new IgnatiaRangeAttackTargetHandler(
                    UnitInStraightLineV1, TargetSubject.Unit, LineMode.InStraightLine,
                    atMaximumRange: false, excludeAdjacent: false),
                new IgnatiaRangeAttackTargetHandler(
                    UnitNotInStraightLineV1, TargetSubject.Unit, LineMode.NotInStraightLine,
                    atMaximumRange: false, excludeAdjacent: false),
                new IgnatiaRangeAttackTargetHandler(
                    UnitAtMaximumRangeV1, TargetSubject.Unit, LineMode.Any,
                    atMaximumRange: true, excludeAdjacent: false),
                new IgnatiaRangeAttackTargetHandler(
                    HeroInRangeNotAdjacentV1, TargetSubject.Hero, LineMode.Any,
                    atMaximumRange: false, excludeAdjacent: true)
            };

        /// <summary>Which of rulebook p.19's kinds a selection offers, and therefore what it publishes.</summary>
        private enum TargetSubject
        {
            /// <summary>p.19's units - a hero or a minion - published as whichever one was named.</summary>
            Unit = 0,

            /// <summary>Heroes alone, published as the hero.</summary>
            Hero = 1
        }

        /// <summary>How p.19's straight-line rule narrows a selection's candidates.</summary>
        private enum LineMode
        {
            /// <summary>The rule says nothing about this selection.</summary>
            Any = 0,

            /// <summary>"in a straight line" - only candidates sharing a file with the actor.</summary>
            InStraightLine = 1,

            /// <summary>"not in a straight line" - the complement.</summary>
            NotInStraightLine = 2
        }

        // -----------------------------------------------------------------------------------------
        // One handler, four declarations. It offers p.19's units (or heroes alone) inside p.9's Range
        // disk, narrowed by the straight-line rule, the maximum-range equality and the adjacency
        // exclusion its constructor bakes in. Milestone 8.88 made those three clauses parameters of the
        // printed-icon family itself, so the candidates now come from that family's one derivation and
        // this handler only fixes which clauses its own identity prints.
        // -----------------------------------------------------------------------------------------

        private sealed class IgnatiaRangeAttackTargetHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            private readonly TargetSubject subject;
            private readonly LineMode line;
            private readonly bool atMaximumRange;
            private readonly bool excludeAdjacent;

            public IgnatiaRangeAttackTargetHandler(
                EffectCapabilityReference reference,
                TargetSubject subject,
                LineMode line,
                bool atMaximumRange,
                bool excludeAdjacent)
            {
                Reference = reference;
                this.subject = subject;
                this.line = line;
                this.atMaximumRange = atMaximumRange;
                this.excludeAdjacent = excludeAdjacent;

                var outputs = new List<EffectOutputBinding>
                {
                    new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero)
                };
                if (subject == TargetSubject.Unit)
                {
                    // A unit selection publishes both identities and fills in whichever was named -
                    // the same pair set-attack-target@1 takes either of.
                    outputs.Add(new EffectOutputBinding(ChosenMinionOutputName, EffectValueKind.Minion));
                }

                Schema = new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(
                            "activity",
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None)),
                        new EffectCapabilityParameter(
                            TargetRelationParameterName,
                            EffectValueKind.Integer,
                            integerDomain: EffectIntegerDomain.AllExcept(SourceTeamRelation.None)),
                        new EffectCapabilityParameter(
                            EffectSelectionDeclaration.ConsumerParameterName,
                            EffectValueKind.Node, isRequired: false)
                    },
                    outputs);
            }

            public EffectCapabilityReference Reference { get; }

            public EffectCapabilityParameterSchema Schema { get; }

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = AnticipateCandidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates, 1, 1));
            }

            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                var published = Publish(context, selected);
                return published == null
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.Applied(published);
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                AnticipateCandidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>Selecting mutates nothing; the set-attack-target node does whatever the effect does.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One piece: the selection names the one subject its sentence names.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context)
            {
                var filtered = FilteredTargets(context);
                if (subject == TargetSubject.Hero)
                {
                    return filtered
                        .Where(target => target.Kind == BoardObjectKind.Hero)
                        .Select(target => EffectValue.Hero(target.HeroId))
                        .ToArray();
                }

                // p.19's two unit kinds in one offer, heroes first, because a card that says "a unit"
                // says both and says nothing about which it prefers.
                return filtered
                    .Where(target => target.Kind == BoardObjectKind.Hero)
                    .Select(target => EffectValue.Hero(target.HeroId))
                    .Concat(filtered
                        .Where(target => target.Kind == BoardObjectKind.Minion)
                        .Select(target => EffectValue.Minion(target.MinionId)))
                    .ToArray();
            }

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();

            /// <summary>
            /// What answering with <paramref name="selected"/> publishes, or nothing where that answer
            /// is not one this selection would take. A unit selection publishes the kind that was named
            /// alone; a node consuming it binds both outputs and one of them resolves.
            /// </summary>
            private IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return null;
                }

                var filtered = FilteredTargets(context);

                if (selected[0].Kind == EffectValueKind.Hero &&
                    filtered.Any(target =>
                        target.Kind == BoardObjectKind.Hero &&
                        target.HeroId.Equals(selected[0].HeroId)))
                {
                    return new[]
                    {
                        new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                    };
                }

                if (subject == TargetSubject.Unit &&
                    selected[0].Kind == EffectValueKind.Minion &&
                    filtered.Any(target =>
                        target.Kind == BoardObjectKind.Minion &&
                        target.MinionId.Equals(selected[0].MinionId)))
                {
                    return new[]
                    {
                        new EffectOutcomeFact(
                            ChosenMinionOutputName, EffectValue.Minion(selected[0].MinionId))
                    };
                }

                return null;
            }

            /// <summary>
            /// The board objects this selection admits, in the disk's own order. Milestone 8.88: the
            /// printed-icon family's one derivation, asked with the clauses this identity prints - so
            /// p.9's disk, p.16's items, p.18's immunity and the side of the board are read in exactly
            /// the place every other printed-icon selection reads them. Derived on every ask, because a
            /// unit that has moved since the question opened has changed which file it is on and how
            /// far away it stands.
            /// </summary>
            private IReadOnlyList<BoardObjectReference> FilteredTargets(EffectCapabilityContext context)
            {
                var clauses = new Dictionary<string, EffectValue>(StringComparer.Ordinal);
                if (excludeAdjacent)
                {
                    clauses[SyntheticTargetingCapabilities.ExcludeAdjacentParameterName] =
                        EffectValue.Boolean(true);
                }

                if (atMaximumRange)
                {
                    clauses[SyntheticTargetingCapabilities.AtMaximumReachParameterName] =
                        EffectValue.Boolean(true);
                }

                if (line == LineMode.InStraightLine)
                {
                    clauses[SyntheticTargetingCapabilities.RequireStraightLineParameterName] =
                        EffectValue.Boolean(true);
                }
                else if (line == LineMode.NotInStraightLine)
                {
                    clauses[SyntheticTargetingCapabilities.ExcludeStraightLineParameterName] =
                        EffectValue.Boolean(true);
                }

                return SyntheticTargetingCapabilities.PrintedIconSelection(
                    context.WithAdditionalInputs(clauses),
                    PrintedValueChannel.Range,
                    subject == TargetSubject.Hero
                        ? SyntheticTargetingCapabilities.PrintedIconKinds.Heroes
                        : SyntheticTargetingCapabilities.PrintedIconKinds.Units,
                    relationRequired: true);
            }
        }
    }
}
