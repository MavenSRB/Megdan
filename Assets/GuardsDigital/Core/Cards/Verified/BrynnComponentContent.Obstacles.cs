using System;
using System.Collections.Generic;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Brynn the Seeker's contracts for the one printed phrase his whole set is built around -
    /// "adjacent to 3 or more obstacles" - as the questions a card's TEXT asks about it rather than as
    /// the selections and values that read it. Milestone 8.69a, the first block of the twentieth hero.
    ///
    /// <para><b>What this file declares and what it deliberately does not.</b> The phrase reaches this
    /// build four ways, and only two of them are capabilities. A modal Attack bullet narrows by it
    /// through <c>AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles</c>; an Attack total
    /// raises by it through <c>RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles</c>;
    /// a printed-Radius hero selection narrows by it through
    /// <c>SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName</c>. None of those
    /// is a capability of his. What IS one is the two PREDICATES his card text conditions on, which no
    /// shared contract can express because each names a subject the general vocabulary does not carry:
    /// "If an enemy hero in radius is adjacent to 3 or more obstacles" (Mountain Guide, Expedition
    /// Leader, True Grit, Die Hard - 8.69c and 8.69e) and "If you target a hero who is adjacent to 3 or
    /// more obstacles" (Split Attack, Split Throw, Peak Precision - 8.69b).</para>
    ///
    /// <para><b>Both are hero-scoped rather than shared</b>, on this project's reuse-first rule read in
    /// the direction it usually cuts the other way: <c>subject-adjacent-terrain-presence@1</c> is the
    /// SHAPE they copy - one Boolean <c>present</c> output, publishing without applying - and it is not
    /// the contract they widen, because that contract answers about p.18's terrain and this one answers
    /// about a THRESHOLD over p.18's whole obstacle list. A shared label attaching a rule the printed
    /// text does not carry is exactly what this project splits on.</para>
    ///
    /// <para><b>Nothing here is registered, and that is the point rather than an oversight.</b>
    /// <see cref="MatchContentCatalogs"/> names no Brynn content until 8.69g deals him, so no
    /// configured match can start either contract. Milestone 7.9's gate is why: the registered content
    /// list is serialized and pinned, so registering a capability before a dealt card can reach it
    /// moves the pinned list, every snapshot's bytes and every corpus digest while the content itself
    /// never moved. <see cref="ObstacleDeferredHandlers"/> is the whole list until then,
    /// <see cref="ObstacleRegisteredHandlers"/> is empty, and <c>BrynnObstacleAdjacencyTests</c>
    /// asserts the gate from both sides.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
        /// <summary>
        /// The placeholder revision every definition this block composes carries, replaced by 8.69f
        /// with the one real string his whole set is stated under.
        ///
        /// <para>It is deliberately malformed - no dealt hero's revision could ever read like this -
        /// on the project's standing rule that a placeholder must not be a value that could be mistaken
        /// for a real one.</para>
        /// </summary>
        public const string PrepRevision = "brynn-8-69a-prep";

        /// <summary>
        /// "If an enemy hero in radius is adjacent to 3 or more obstacles" - the presence predicate
        /// four of his Blue and Green faces gate their second sentence on.
        ///
        /// <para>A presence check and never a target: p.19 says outright that "Effects that check for
        /// unit presence ... do not target", so p.18's immunity never enters it and nothing on the
        /// board is touched by asking. That is the same statement <c>subject-adjacent-terrain-
        /// presence@1</c> makes about its own question.</para>
        /// </summary>
        public static EffectCapabilityId EnemyHeroInRadiusAdjacentToObstacles { get; } =
            new EffectCapabilityId("brynn-enemy-hero-in-radius-adjacent-to-obstacles");

        /// <summary>
        /// "a hero who is adjacent to 3 or more obstacles" asked about ONE named hero - the gate
        /// 8.69b's two split repeats and Peak Precision's retrieval read off the first instance's
        /// bound Attack target.
        ///
        /// <para>It is a second contract rather than a parameter on the first because the two name
        /// different subjects: the first is a question about a DISK and answers whether anybody in it
        /// qualifies, this one is a question about a hero somebody already chose. A single contract
        /// with an optional subject would have answered the disk question whenever a binding failed to
        /// arrive, which is the silent widening an unbound optional parameter always is.</para>
        /// </summary>
        public static EffectCapabilityId HeroAdjacentToObstacles { get; } =
            new EffectCapabilityId("brynn-hero-adjacent-to-obstacles");

        public static EffectCapabilityReference EnemyHeroInRadiusAdjacentToObstaclesV1 =>
            new EffectCapabilityReference(EnemyHeroInRadiusAdjacentToObstacles, 1);

        public static EffectCapabilityReference HeroAdjacentToObstaclesV1 =>
            new EffectCapabilityReference(HeroAdjacentToObstacles, 1);

        /// <summary>The hero this predicate is asked about, for <see cref="HeroAdjacentToObstacles"/>.</summary>
        public const string SubjectParameterName = "subject";

        /// <summary>The Boolean both predicates publish - <c>subject-adjacent-terrain-presence@1</c>'s name.</summary>
        public const string PresentOutputName = "present";

        /// <summary>
        /// Nothing this build registers, until 8.69g deals him - see the type's own remarks for why the
        /// gate is the point. The member exists so a "registered or deferred" sweep keeps its shape and
        /// so the fixture asserting the deferral asks a question rather than failing to compile.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ObstacleRegisteredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

        /// <summary>
        /// The two predicates this block declares and this build withholds. Both are a new handler
        /// TYPE, so 8.69g owes each a showcase consumer as well as a registration.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ObstacleDeferredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new EnemyHeroInRadiusAdjacentToObstaclesHandler(),
                new HeroAdjacentToObstaclesHandler()
            };

        /// <summary>
        /// Everything this hero's contracts amount to, registered or not, for the offline content
        /// fixtures that compose catalogs of their own - the shape <c>MinComponentContent.Handlers</c>
        /// keeps.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> Handlers()
        {
            foreach (var handler in ObstacleRegisteredHandlers())
            {
                yield return handler;
            }

            foreach (var handler in ObstacleDeferredHandlers())
            {
                yield return handler;
            }
        }

        /// <summary>
        /// "If an enemy hero in radius is adjacent to 3 or more obstacles" - whole.
        ///
        /// <para>The disk is p.9's printed Radius icon of the card being performed with p.16's matching
        /// items composed onto it, read the way <c>synthetic-radius-hero-target@1</c> reads it, because
        /// the four faces that print this clause all print a Radius icon and the item that raises it is
        /// one of his twelve. A card printing no Radius has no disk and the answer is false - the same
        /// reading every printed-icon consumer takes of the same absence, and never an adjacency
        /// default p.16 would have had to invent.</para>
        ///
        /// <para>Which heroes are in it is <c>RosterPresenceQuery</c>'s answer rather than a second
        /// walk, and whether one of them qualifies is
        /// <see cref="ObstacleAdjacencyQuery"/>'s - so his Ultimate widens this predicate to every
        /// enemy hero in the disk without this handler knowing an Ultimate exists.</para>
        /// </summary>
        private sealed class EnemyHeroInRadiusAdjacentToObstaclesHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => EnemyHeroInRadiusAdjacentToObstaclesV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    Array.Empty<EffectCapabilityParameter>(),
                    new[] { new EffectOutputBinding(PresentOutputName, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            /// <summary>
            /// The answer, or the named absence where the question cannot be asked at all - no board,
            /// no placement port, no card state to read a printed icon off, or an actor who is not
            /// standing anywhere. Refused rather than answered false, because "no enemy hero
            /// qualifies" and "this composition cannot see the board" are different facts and only one
            /// of them is a rule.
            /// </summary>
            private static bool? Read(EffectCapabilityContext context)
            {
                var components = context.Components;
                if (components?.Board == null || components.Tokens == null ||
                    !context.CardState.IsAvailable ||
                    !components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt))
                {
                    return null;
                }

                var radius = RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications);
                if (radius.Total <= 0)
                {
                    return false;
                }

                var heroes = RosterPresenceQuery.FindUnitsWithin(
                    actorAt,
                    radius.Total,
                    RosterUnitKind.Hero,
                    SourceTeamRelation.OpposingSource,
                    context.ActorId,
                    components.Board,
                    context.Participants,
                    context.Modifications);
                for (var index = 0; index < heroes.Count; index++)
                {
                    if (heroes[index].Kind == BoardObjectKind.Hero &&
                        ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                            components.Tokens, heroes[index].HeroId, context.Modifications))
                    {
                        return true;
                    }
                }

                return false;
            }

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
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

            /// <summary>
            /// Asking whether somebody is boxed in changes nothing, and answering "yes" is not the card
            /// doing anything - milestone 8.35l's defect, which the two terrain predicates record: a
            /// gate that counted as application left the step it gates unjudged, so a barred step was
            /// offered, drew no prompt and spent the card.
            /// </summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var value = Read(context);
                return value.HasValue
                    ? new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
                    }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }

        /// <summary>
        /// "a hero who is adjacent to 3 or more obstacles", asked about one bound hero - the gate
        /// 8.69b's split repeats and Peak Precision's retrieval read off the target the first instance
        /// of the Attack chose.
        ///
        /// <para>A subject that is not on the board answers NOTHING rather than false, on
        /// <c>subject-adjacent-terrain-presence@1</c>'s own terms: a hero who has left the board is not
        /// the hero the clause is about, and reporting "not adjacent to three obstacles" about them
        /// would be an answer nobody asked for. An unbound subject is the same absence.</para>
        /// </summary>
        private sealed class HeroAdjacentToObstaclesHandler
            : IEffectCapabilityHandler, IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => HeroAdjacentToObstaclesV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
                    new[]
                    {
                        new EffectCapabilityParameter(SubjectParameterName, EffectValueKind.Hero)
                    },
                    new[] { new EffectOutputBinding(PresentOutputName, EffectValueKind.Boolean) });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            private static bool? Read(EffectCapabilityContext context)
            {
                var spaces = context.Components?.Tokens;
                var subject = context.Input(SubjectParameterName);
                if (spaces == null || subject.Kind != EffectValueKind.Hero ||
                    !subject.HeroId.IsDefined ||
                    !StandsSomewhere(spaces, subject.HeroId, context.Modifications))
                {
                    return null;
                }

                return ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                    spaces, subject.HeroId, context.Modifications);
            }

            /// <summary>
            /// Whether the subject is a hero this question can be asked about at all.
            ///
            /// <para>A hero the Ultimate's counts-as overlay reaches is one even while the board lookup
            /// would fail, because the overlay is a rule about the hero rather than about where they
            /// stand; every other subject has to be standing somewhere.</para>
            /// </summary>
            private static bool StandsSomewhere(
                ITokenPlacementAuthority spaces, HeroId hero, RuleModificationSet modifications) =>
                modifications.CountsAsAdjacentToThreeOrMoreObstacles(RuleObject.ForHero(hero)) ||
                spaces.TryGetHeroCoordinate(hero, out _);

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var value = Read(context);
                return value.HasValue
                    ? EffectExecutionResult.Applied(new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
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

            /// <summary>Asking about a hero's footing changes nothing; see the twin above.</summary>
            public bool PublishesWithoutApplying => true;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen)
            {
                var value = Read(context);
                return value.HasValue
                    ? new[]
                    {
                        new EffectOutcomeFact(PresentOutputName, EffectValue.Boolean(value.Value))
                    }
                    : Array.Empty<EffectOutcomeFact>();
            }
        }
    }
}
