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
    /// build four ways, and only one of them is a capability of this block's. A modal Attack bullet
    /// narrows by it through <c>AttackSequenceSubjectNarrowing.IsAdjacentToThreeOrMoreObstacles</c>; an
    /// Attack total raises by it through
    /// <c>RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles</c>; a printed-Radius
    /// hero selection narrows by it through
    /// <c>SyntheticTargetingCapabilities.AdjacentToThreeOrMoreObstaclesParameterName</c>. None of those
    /// is a capability of his. What IS one is the PREDICATE his card text conditions on, which no
    /// shared contract can express because it names a subject the general vocabulary does not carry:
    /// "If an enemy hero in radius is adjacent to 3 or more obstacles" (Mountain Guide, Expedition
    /// Leader, True Grit, Die Hard - 8.69c and 8.69e). This block minted a SECOND predicate for "If you
    /// target a hero who is adjacent to 3 or more obstacles"; milestone 8.69b replaced it with a
    /// question about the targeting and milestone 8.69f dropped it, which is recorded where it
    /// stood.</para>
    ///
    /// <para><b>It is hero-scoped rather than shared</b>, on this project's reuse-first rule read in
    /// the direction it usually cuts the other way: <c>subject-adjacent-terrain-presence@1</c> is the
    /// SHAPE it copies - one Boolean <c>present</c> output, publishing without applying - and it is not
    /// the contract it widens, because that contract answers about p.18's terrain and this one answers
    /// about a THRESHOLD over p.18's whole obstacle list. A shared label attaching a rule the printed
    /// text does not carry is exactly what this project splits on.</para>
    ///
    /// <para><b>Nothing here is registered, and that is the point rather than an oversight.</b>
    /// <see cref="MatchContentCatalogs"/> names no Brynn content until 8.69g deals him, so no
    /// configured match can start the contract. Milestone 7.9's gate is why: the registered content
    /// list is serialized and pinned, so registering a capability before a dealt card can reach it
    /// moves the pinned list, every snapshot's bytes and every corpus digest while the content itself
    /// never moved. <see cref="ObstacleDeferredHandlers"/> is the whole list until then,
    /// <see cref="ObstacleRegisteredHandlers"/> is empty, and <c>BrynnObstacleAdjacencyTests</c>
    /// asserts the gate from both sides.</para>
    /// </summary>
    public static partial class BrynnComponentContent
    {
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

        // MILESTONE 8.69f DROPPED THIS BLOCK'S SECOND PREDICATE, `brynn-hero-adjacent-to-obstacles@1`,
        // and the `HeroAdjacentToObstaclesNode` that composed it. It was minted here for the three
        // split faces' gate, and milestone 8.69b then measured that their subject is the TARGETING
        // rather than a hero - p.14 removes a defeated target at step 6, so a predicate that reads the
        // live hero refuses Peak Precision's retrieval exactly when the attack worked - and minted
        // `brynn-attack-target-was-adjacent-to-obstacles@1` instead. No printed sentence of his asks
        // "is THIS named hero adjacent to 3 or more obstacles": the fifteen faces that print the phrase
        // ask it of a DISK (the presence predicate below), of a candidate at selection (the printed
        // Radius arms' `adjacentToThreeOrMoreObstacles` flag, and the Attack bullet's narrowing
        // member), of the Attack total (the value-query member) or of the targeting (8.69b's). A
        // contract nothing can start is one this set drops rather than one a dealing registers.

        public static EffectCapabilityReference EnemyHeroInRadiusAdjacentToObstaclesV1 =>
            new EffectCapabilityReference(EnemyHeroInRadiusAdjacentToObstacles, 1);

        /// <summary>The Boolean his predicates publish - <c>subject-adjacent-terrain-presence@1</c>'s name.</summary>
        public const string PresentOutputName = "present";

        /// <summary>
        /// The ONE predicate this block declares, REGISTERED since milestone 8.69g dealt him. Mountain
        /// Guide, Expedition Leader, True Grit and Die Hard gate their second sentence on it, and all
        /// four are cards a configured match now deals - which is the whole of what milestone 7.9's
        /// gate asks before a contract joins the build's catalog. It is a new handler TYPE, so the
        /// same change gave it a showcase consumer (<c>SyntheticShowcaseHand</c>'s Red Surveyor).
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ObstacleRegisteredHandlers() =>
            new IEffectCapabilityHandler[]
            {
                new EnemyHeroInRadiusAdjacentToObstaclesHandler()
            };

        /// <summary>
        /// Nothing, since milestone 8.69g dealt him. The member stays rather than being deleted: an
        /// empty deferral list is what a DISCHARGED gate looks like, the sweeps that read "registered
        /// or deferred" keep their shape, and a later block that withholds something of his finds the
        /// shape already here. The block's second declaration was dropped at 8.69f - see the note
        /// where it used to stand.
        /// </summary>
        public static IEnumerable<IEffectCapabilityHandler> ObstacleDeferredHandlers() =>
            Array.Empty<IEffectCapabilityHandler>();

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
    }
}
