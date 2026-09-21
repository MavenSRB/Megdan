using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.BoardObjects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's one selection over a UNION of two reaches - "Swap either with a unit adjacent
    /// to you, or with an enemy hero in radius who is adjacent to 3 or more obstacles" - which Tread
    /// Lightly, Cover Tracks and Hide Traces print word for word. Milestone 8.69d.
    ///
    /// <para><b>Why this is a contract of his rather than Whisper's.</b>
    /// <c>choose-swap-subject@1</c> is the shipped union selection and its grammar is exactly this
    /// sentence's - one choice over two differently reached sets, never a modal - so reuse-first points
    /// straight at it. It is still the wrong contract here, and one printed icon is why: its second arm
    /// is the card's printed RANGE (<c>RangeValueQuery</c>, for Cruel Twist's "an enemy hero in range
    /// with a card in the discard"), and all three of these faces print a RADIUS. Hide Traces is where
    /// that stops being a distinction and becomes a defect: it prints Radius 3 and carries a RANGE item,
    /// so a composition on the Range arm would have measured the swap with an icon the card does not
    /// print, widened it with the wrong item, and ignored the Radius one his set also hands out. This
    /// build already keeps that difference as two capabilities rather than one parameter -
    /// <c>synthetic-range-hero-target@1</c> beside <c>synthetic-radius-hero-target@1</c> - and a
    /// "measure by Radius instead" toggle bolted onto the union would have collapsed exactly that
    /// doctrine inside one sentence. BR-Q17d records the reading.</para>
    ///
    /// <para><b>One capability and not two nodes.</b> The face offers one choice and not a choice of
    /// which kind of choice to make, which is the whole argument
    /// <see cref="GuardsDigital.Core.Effects.Synthetic.SyntheticRosterCapabilities.ChooseSwapSubjectV1"/>
    /// records: a <c>Choose</c> over two selections would put a question to the player the card never
    /// asks, and would let one arm grey for emptiness while the other still had subjects. The printed
    /// "either ... or" joins two REACHES, and the union of two reaches is one set.</para>
    ///
    /// <para><b>Nothing here is registered.</b> <see cref="MatchContentCatalogs"/> names no Brynn
    /// content until 8.69g deals him - see <c>BrynnComponentContent.Obstacles.cs</c> for milestone
    /// 7.9's gate, which is the reason rather than an oversight. <see cref="SwapDeferredHandlers"/> is
    /// the whole list until then and <c>BrynnTrackingSwapTests</c> asserts the absence from both
    /// sides.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// "Swap either with a unit adjacent to you, or with an enemy hero in radius who is adjacent to
        /// 3 or more obstacles" - the subject of that swap, as ONE selection over the union the
        /// sentence names.
        ///
        /// <para>It selects and never performs: p.19's exchange is
        /// <c>synthetic-swap-actor@1</c>'s, which every swapping face in this build already reaches,
        /// and this node hands it the space the chooser picked.</para>
        /// </summary>
        public static EffectCapabilityId TrackingSwapSubject { get; } =
            new EffectCapabilityId("brynn-tracking-swap-subject");

        public static EffectCapabilityReference TrackingSwapSubjectV1 =>
            new EffectCapabilityReference(TrackingSwapSubject, 1);

        /// <summary>
        /// Where the chosen unit is standing - the value <c>synthetic-swap-actor@1</c> reads, because
        /// p.19's exchange names two spaces and one-obstacle-per-space makes a space an unambiguous
        /// identity for every kind at once. The three spellings are the shipped selections' own, so a
        /// consumer written against either reads the same names.
        /// </summary>
        public const string ChosenSpaceOutputName = "chosenSpace";

        public const string ChosenHeroOutputName = "chosenHero";

        public const string ChosenMinionOutputName = "chosenMinion";

        /// <summary>
        /// The one selection this block declares, REGISTERED since milestone 8.69g dealt him: Tread
        /// Lightly, Cover Tracks and Hide Traces are cards a configured match now deals. It is a new
        /// handler TYPE, so the same change gave it a showcase consumer - the Red Pathkeeper.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SwapRegisteredHandlers() =>
            new IEffectCapabilityHandler[] { new TrackingSwapSubjectHandler() };

        /// <summary>
        /// Nothing, since milestone 8.69g dealt him. The member stays rather than being deleted, on the
        /// terms the obstacle partial's own pair states.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> SwapDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The union, whole: every unit standing in p.19's innermost ring around him, plus every enemy
        /// hero inside the performing card's printed Radius who satisfies his one printed predicate.
        /// </summary>
        private sealed class TrackingSwapSubjectHandler
            : IEffectCapabilityHandler, IEffectImmunityOverrideCapabilityHandler
        {
            /// <summary>
            /// p.19's Swap is the activity an override may be declared for, which is the same list
            /// <c>synthetic-swap-actor@1</c> and <c>choose-swap-subject@1</c> both publish.
            /// </summary>
            public IReadOnlyList<RuleActivity> SupportedImmunityOverrideActivities { get; } =
                Array.AsReadOnly(new[] { RuleActivity.Swap });

            public EffectCapabilityReference Reference => TrackingSwapSubjectV1;

            /// <summary>
            /// NO parameters, which is the sentence rather than a shortcut: all three faces print the
            /// identical clause, both reaches are written into it (p.19's ring, and the performing
            /// card's own printed Radius icon), and the predicate is the one
            /// <see cref="ObstacleAdjacencyQuery"/> owns. A parameter here would be a knob no printed
            /// face of his turns - and an unbound optional one is precisely how a narrowing goes
            /// silently missing.
            /// </summary>
            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[]
                    {
                        new EffectOutputBinding(ChosenSpaceOutputName, EffectValueKind.Coordinate),
                        new EffectOutputBinding(ChosenHeroOutputName, EffectValueKind.Hero),
                        new EffectOutputBinding(ChosenMinionOutputName, EffectValueKind.Minion)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Subjects(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(AsValue), 1, 1));
            }

            /// <summary>
            /// The answer is measured against a freshly derived union rather than against the list the
            /// question was opened with - which is what makes a restore, a replay and a rewind ask the
            /// same question, and a forged answer unanswerable.
            /// </summary>
            public EffectExecutionResult Resume(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1)
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                var chosen = FromValue(selected[0]);
                if (!chosen.IsDefined || !Subjects(context).Contains(chosen) ||
                    !context.Components.Board.TryGetObjectCoordinate(chosen, out var at))
                {
                    return EffectExecutionResult.ImpossibleMandatory();
                }

                return EffectExecutionResult.Applied(new[]
                {
                    chosen.Kind == BoardObjectKind.Hero
                        ? new EffectOutcomeFact(
                            ChosenHeroOutputName, EffectValue.Hero(chosen.HeroId))
                        : new EffectOutcomeFact(
                            ChosenMinionOutputName, EffectValue.Minion(chosen.MinionId)),
                    new EffectOutcomeFact(ChosenSpaceOutputName, EffectValue.Space(at))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Subjects(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            /// <summary>
            /// The union the sentence names, in canonical order and without duplicates.
            ///
            /// <para><b>An enemy hero standing beside him who is also boxed in satisfies both arms and
            /// is ONE candidate</b>, which is what "or" means about a set - the same de-duplication
            /// <c>choose-swap-subject@1</c> performs for the same reason (BR-Q19d).</para>
            ///
            /// <para><b>A token is never among them.</b> p.19's units are heroes and minions, the
            /// sentence says "a unit", and the roster query answers about units - so the exclusion is
            /// the noun rather than a filter. The consuming Swap declares
            /// <c>SwapEligiblePieceKind.Unit</c> and would refuse one anyway, which is the second
            /// statement of the same word.</para>
            /// </summary>
            private static IReadOnlyList<BoardObjectReference> Subjects(
                EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Board == null || components.Tokens == null ||
                    context.Participants == null ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return Array.Empty<BoardObjectReference>();
                }

                var found = new List<BoardObjectReference>();

                // "a unit adjacent to you" - p.19's innermost ring, written into the sentence, which
                // p.16's items never raise; and every side of the board, because the clause names
                // neither team so neither is excluded.
                found.AddRange(RosterPresenceQuery.FindUnitsWithin(
                    actorAt,
                    1,
                    RosterUnitKind.Unit,
                    SourceTeamRelation.AnyTeam,
                    context.ActorId,
                    components.Board,
                    context.Participants,
                    context.Modifications));

                // "an enemy hero in radius who is adjacent to 3 or more obstacles" - p.9's printed
                // Radius icon of the card being performed with p.16's matching items composed onto it,
                // read exactly as `synthetic-radius-hero-target@1` reads it. A card printing no Radius
                // has no disk and reaches nobody, which is the reading every printed-icon consumer
                // takes of the same absence.
                if (context.CardState.IsAvailable)
                {
                    var radius = RadiusValueQuery.Calculate(
                        context.SourceCardInstanceId,
                        context.SourceDefinitionReference,
                        context.ActionDefinitionId,
                        context.CardState.Definitions,
                        context.ActorId,
                        context.HeroItems,
                        context.Modifications);
                    if (radius.Total > 0)
                    {
                        found.AddRange(RosterPresenceQuery.FindUnitsWithin(
                                actorAt,
                                radius.Total,
                                RosterUnitKind.Hero,
                                SourceTeamRelation.OpposingSource,
                                context.ActorId,
                                components.Board,
                                context.Participants,
                                context.Modifications)
                            .Where(unit => unit.Kind == BoardObjectKind.Hero)
                            .Where(unit => ObstacleAdjacencyQuery
                                .HeroIsAdjacentToThreeOrMoreObstacles(
                                    components.Tokens, unit.HeroId, context.Modifications)));
                    }
                }

                // A radius-one query includes its origin and "adjacent" does not; a swap with himself
                // is nothing in either arm.
                var self = BoardObjectReference.ForHero(context.ActorId);
                return found
                    .Where(value => !value.Equals(self))
                    .Where(value => Admits(context, value))
                    .Distinct()
                    .OrderBy(value => value.Kind)
                    .ThenBy(value => value.HeroId.Value ?? value.MinionId.Value, StringComparer.Ordinal)
                    .ToArray();
            }

            /// <summary>
            /// Whether p.18's immunity and p.19's restrictions let this actor exchange places with this
            /// piece - asked here so that a unit the Swap would refuse is never offered, which is
            /// milestone 8.35l's rule.
            ///
            /// <para><b>It asks exactly what the consuming Swap asks, in the same two activities.</b>
            /// <c>synthetic-swap-actor@1</c> tests the enclosing action's activity AND p.19's Swap, so
            /// a selection asking only one of them would offer a unit immune to the other and then have
            /// the exchange refuse it - the offer-and-refuse shape milestone 8.35l exists to stop
            /// (BR-Q18d). p.19 makes a swap a TARGETING of the unit, which is why immunity enters here
            /// at all and never enters the presence predicates beside it.</para>
            /// </summary>
            private static bool Admits(
                EffectCapabilityContext context, BoardObjectReference candidate)
            {
                var piece = candidate.AsRuleObject();
                if (!piece.IsDefined)
                {
                    return false;
                }

                var actor = RuleObject.ForHero(context.ActorId);
                if (IsPrevented(context, piece, actor, context.EnclosingActivity) ||
                    IsPrevented(context, piece, actor, RuleActivity.Swap))
                {
                    return false;
                }

                return !RestrictionQuery.TryGetRestriction(
                    piece, RuleActivity.Swap, actor, context.Participants, context.Modifications,
                    out _);
            }

            private static bool IsPrevented(
                EffectCapabilityContext context,
                RuleObject piece,
                RuleObject actor,
                RuleActivity activity) =>
                activity != RuleActivity.None &&
                TargetQuery.IsPrevented(
                    piece,
                    activity,
                    actor,
                    context.Components.Board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    RuleActivity.Swap);

            private static EffectValue AsValue(BoardObjectReference candidate) =>
                candidate.Kind == BoardObjectKind.Hero
                    ? EffectValue.Hero(candidate.HeroId)
                    : EffectValue.Minion(candidate.MinionId);

            private static BoardObjectReference FromValue(EffectValue value)
            {
                if (value.Kind == EffectValueKind.Hero)
                {
                    return BoardObjectReference.ForHero(value.HeroId);
                }

                return value.Kind == EffectValueKind.Minion
                    ? BoardObjectReference.ForMinion(value.MinionId)
                    : default;
            }
        }
    }
}
