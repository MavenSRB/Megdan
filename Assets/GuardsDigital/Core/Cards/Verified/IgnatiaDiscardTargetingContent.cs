using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Effects.Synthetic;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Cards.Verified
{
    /// <summary>
    /// Ignatia the Mad's DISCARD-line targeting narrowings - milestone 8.75d. Her three discard cards
    /// (Abrupt Combustion, Spontaneous Immolation, Violent Conflagration) name two subjects no earlier
    /// hero's shared selection can express, so both are minted here as her own capabilities rather than
    /// by widening a registered one:
    ///
    /// <list type="bullet">
    /// <item><description>the SUN arm's subject, "an enemy hero in radius, adjacent to a token, or a
    /// minion" - a printed-Radius enemy-hero selection narrowed to candidates standing beside at least
    /// one TOKEN OR at least one minion (either side);</description></item>
    /// <item><description>the MOON arm's subject, "an enemy minion in radius adjacent to an enemy hero"
    /// - a printed-Radius enemy-minion selection narrowed to candidates standing beside at least one
    /// enemy hero.</description></item>
    /// </list>
    ///
    /// <para><b>Why "a token, or a minion" is not "any obstacle" - and not "her magma" either.</b>
    /// ID-Q8c, SETTLED at milestone 8.75c against the printed front (<c>ignatia8__7641669</c>, whose
    /// sun line reads "An enemy hero in radius, adjacent to a token, or a minion, discards a card, If
    /// able"). A minion is itself one of rulebook p.18's obstacles, so a sentence that names a token
    /// AND a minion separately is narrower than p.18's list: it never reaches terrain and it never
    /// reaches another hero. It is not narrower than that. The card prints "a token" with no
    /// qualifier - not "your token", not "a magma token" - and p.19's word covers every placed token
    /// whoever put it there, so a hero standing beside Min's mine or Tali's totem satisfies the clause
    /// exactly as one standing beside her own magma does.
    ///
    /// <para>The inventory pass read it as HER magma alone, which is the reading this block removed:
    /// the argument for it established only that "a token, or a minion" is narrower than "any
    /// obstacle", and a narrowing the text does not carry is one "Cards are the definition" forbids.
    /// Her Green line still makes the clause hers in practice, because on most boards the token beside
    /// an enemy hero is one she dropped.</para>
    ///
    /// <para>The two halves are one printed clause joined by "or", so a candidate satisfying EITHER
    /// qualifies - the same reading
    /// <see cref="SyntheticTargetingCapabilities.AdjacentToAnEnemyHeroOrHoldingADiscardParameterName"/>
    /// records for a disjunction carried as one narrowing.</para>
    ///
    /// <para><b>Both are their own capability ids rather than parameters on the shared printed-icon
    /// handler</b>, because the file that owns that handler is not this hero's to edit and because the
    /// magma-token half reads a token identity no shared narrowing knows. They share nothing with the
    /// registered family except the public disk queries every printed-icon selection uses -
    /// <see cref="RadiusValueQuery"/> for p.9's icon with p.16's items, and <see cref="RadiusQuery"/>
    /// for the geometry - so the disk, immunity and item composition are still read in one place.</para>
    ///
    /// <para><b>NO TOKEN-DEFINITION DEPENDENCY.</b> Because the printed word is the unqualified "a
    /// token", the hero narrowing asks the token authority whether ANY token stands beside the
    /// candidate rather than matching a definition id. That is one fewer thing that can drift: the
    /// magma token her Green line drops is a token, so it satisfies the clause without this file
    /// naming it, and so does every other token p.19 admits.</para>
    ///
    /// <para><b>Nothing here is registered.</b> Like every Ignatia declaration, these wait in
    /// <see cref="Handlers"/> until her dealing block moves them into the registered-capability list a
    /// snapshot pins - milestone 7.9's gate. The probe test builds a catalog from
    /// <see cref="Handlers"/> plus the coin predicate to prove the three bodies compose to definitions
    /// the build can load, without registering anything a dealt card cannot yet start.</para>
    /// </summary>
    public static class IgnatiaDiscardTargetingContent
    {
        // -----------------------------------------------------------------------------------------
        // Capability identities.
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// "An enemy hero in radius, adjacent to a token, or a minion."
        ///
        /// <para>The word "magma" in the identity is the inventory pass's spelling, kept because a
        /// capability id is content identity and renaming one is a change to what a snapshot records
        /// rather than a correction to a rule. ID-Q8c widened what the handler ADMITS - any token, not
        /// only hers - and the id is the same contract either way.</para>
        /// </summary>
        public static EffectCapabilityId EnemyHeroAdjacentToMagmaOrMinion { get; } =
            new EffectCapabilityId("ignatia-enemy-hero-adjacent-to-magma-or-minion");

        public static EffectCapabilityReference EnemyHeroAdjacentToMagmaOrMinionV1 =>
            new EffectCapabilityReference(EnemyHeroAdjacentToMagmaOrMinion, 1);

        /// <summary>"An enemy minion in radius adjacent to an enemy hero."</summary>
        public static EffectCapabilityId EnemyMinionAdjacentToEnemyHero { get; } =
            new EffectCapabilityId("ignatia-enemy-minion-adjacent-to-enemy-hero");

        public static EffectCapabilityReference EnemyMinionAdjacentToEnemyHeroV1 =>
            new EffectCapabilityReference(EnemyMinionAdjacentToEnemyHero, 1);

        /// <summary>The name a selecting node reads its chosen hero on - the shared family's own spelling.</summary>
        public const string ChosenHeroOutputName = SyntheticTargetingCapabilities.ChosenHeroOutputName;

        /// <summary>The name a selecting node reads its chosen minion on - the shared family's own spelling.</summary>
        public const string ChosenMinionOutputName = SyntheticTargetingCapabilities.ChosenMinionOutputName;

        /// <summary>The chosen minion's own space, for a consumer that takes a coordinate (p.19's Remove).</summary>
        public const string ChosenMinionSpaceOutputName =
            SyntheticTargetingCapabilities.ChosenMinionSpaceOutputName;

        /// <summary>The side-of-the-board parameter, carried under the shared family's name.</summary>
        public const string TargetRelationParameterName =
            SyntheticTargetingCapabilities.TargetRelationParameterName;

        public static IEnumerable<IEffectCapabilityHandler> Handlers() =>
            new IEffectCapabilityHandler[]
            {
                new EnemyHeroAdjacentToMagmaOrMinionHandler(),
                new EnemyMinionAdjacentToEnemyHeroHandler()
            };

        // -----------------------------------------------------------------------------------------
        // Shared disk. Mirrors SyntheticTargetingCapabilities.PrintedIconTargets (Radius arm): p.9's
        // disk measured from the actor at the acting card's own printed Radius with p.16's items
        // composed on, and p.18's immunity filtered through the same TargetQuery every printed-icon
        // selection uses - so this narrowing cannot come to disagree with the registered family about
        // who is inside a card's icon.
        // -----------------------------------------------------------------------------------------

        private static IReadOnlyList<BoardObjectReference> RadiusTargets(
            EffectCapabilityContext context, out HexCoordinate origin)
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

            var printedTotal = RadiusValueQuery.Calculate(
                context.SourceCardInstanceId,
                context.SourceDefinitionReference,
                context.ActionDefinitionId,
                context.CardState.Definitions,
                context.ActorId,
                context.HeroItems,
                context.Modifications);
            if (printedTotal.Total <= 0)
            {
                // A card printing no Radius icon has no such bound at all, and p.16 does not give it
                // one - the reading the printed-icon family already records.
                return Array.Empty<BoardObjectReference>();
            }

            return RadiusQuery.Targets(
                origin,
                printedTotal,
                includeSelf: false,
                selector.AsRuleObject(),
                activity,
                board,
                context.Participants,
                context.Modifications,
                context.ImmunityOverrides,
                activity);
        }

        private static SourceTeamRelation Relation(EffectCapabilityContext context)
        {
            var relation = (SourceTeamRelation)context.Input(TargetRelationParameterName).IntegerValue;
            return Enum.IsDefined(typeof(SourceTeamRelation), relation) ? relation : SourceTeamRelation.None;
        }

        // -----------------------------------------------------------------------------------------
        // (a) "An enemy hero in radius, adjacent to a [magma] token, or a minion."
        // -----------------------------------------------------------------------------------------

        private sealed class EnemyHeroAdjacentToMagmaOrMinionHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => EnemyHeroAdjacentToMagmaOrMinionV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
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
                    new EffectOutcomeFact(ChosenHeroOutputName, EffectValue.Hero(selected[0].HeroId))
                });
            }

            public EffectExecutionResultKind Anticipate(EffectCapabilityContext context) =>
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(EffectValue.Hero).ToArray();

            /// <summary>Selecting mutates nothing; the discard node does whatever the effect does.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One hero: the selection names the one subject its sentence names.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                chosen != null && chosen.Count == 1 && chosen[0].Kind == EffectValueKind.Hero &&
                Candidates(context).Contains(chosen[0].HeroId)
                    ? new[]
                    {
                        new EffectOutcomeFact(ChosenHeroOutputName, EffectValue.Hero(chosen[0].HeroId))
                    }
                    : Array.Empty<EffectOutcomeFact>();

            private static IReadOnlyList<HeroId> Candidates(EffectCapabilityContext context)
            {
                var relation = Relation(context);
                if (relation == SourceTeamRelation.None)
                {
                    return Array.Empty<HeroId>();
                }

                var targets = RadiusTargets(context, out _);
                var board = context.Components?.Board;
                var tokens = context.Components?.Tokens;
                if (board == null || tokens == null)
                {
                    return Array.Empty<HeroId>();
                }

                var heroes = new List<HeroId>(targets.Count);
                foreach (var target in targets)
                {
                    if (target.Kind != BoardObjectKind.Hero ||
                        !TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board,
                            context.Modifications))
                    {
                        continue;
                    }

                    // Milestone 9.21c: each body is tested from its own space, and the hero is
                    // named once (RZ-Q2).
                    if (board.TryGetObjectCoordinate(target, out var at) &&
                        IsAdjacentToATokenOrAMinion(board, tokens, at) &&
                        !heroes.Contains(target.HeroId))
                    {
                        heroes.Add(target.HeroId);
                    }
                }

                return heroes;
            }
        }

        /// <summary>
        /// Whether one of the six spaces neighbouring <paramref name="coordinate"/> holds a TOKEN of
        /// any definition and any owner, or a minion of either side - ID-Q8c's reading of "adjacent to
        /// a token, or a minion", settled at 8.75c against the printed front.
        ///
        /// <para>The minion half asks the board occupancy directly - a minion is one obstacle per space,
        /// so its space names it - and the token half asks the token authority for any placed token at
        /// that space. Neither half counts terrain or another hero, which is what makes "a token, or a
        /// minion" narrower than p.18's obstacle list; nothing narrows it further, because the card
        /// prints "a token" with no qualifier.</para>
        /// </summary>
        private static bool IsAdjacentToATokenOrAMinion(
            IBoardDisplacementAuthority board,
            ITokenPlacementAuthority tokens,
            HexCoordinate coordinate)
        {
            foreach (var neighbour in coordinate.GetNeighbors())
            {
                if (board.TryGetObjectAt(neighbour, out var occupant) &&
                    occupant.Kind == BoardObjectKind.Minion)
                {
                    return true;
                }

                if (tokens.PlacedTokens.Any(token => token.Coordinate.Equals(neighbour)))
                {
                    return true;
                }
            }

            return false;
        }

        // -----------------------------------------------------------------------------------------
        // (b) "An enemy minion in radius adjacent to an enemy hero."
        // -----------------------------------------------------------------------------------------

        private sealed class EnemyMinionAdjacentToEnemyHeroHandler
            : IEffectCapabilityHandler, IEffectSelectionCapabilityHandler,
                IEffectAnticipatedOutcomeCapabilityHandler
        {
            public EffectCapabilityReference Reference => EnemyMinionAdjacentToEnemyHeroV1;

            public EffectCapabilityParameterSchema Schema { get; } =
                new EffectCapabilityParameterSchema(
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
                    new[]
                    {
                        new EffectOutputBinding(ChosenMinionOutputName, EffectValueKind.Minion),

                        // Published only for a node that declared it - p.19's Remove takes the space,
                        // p.13's Defeat takes the identity. See AnticipateOutcomes / Publish.
                        new EffectOutputBinding(
                            ChosenMinionSpaceOutputName, EffectValueKind.Coordinate)
                    });

            public IReadOnlyList<ActiveEffectDefinition> ActiveEffectDefinitions { get; } =
                Array.Empty<ActiveEffectDefinition>();

            public EffectExecutionResult Execute(EffectCapabilityContext context)
            {
                var candidates = Candidates(context);
                return candidates.Count == 0
                    ? EffectExecutionResult.ImpossibleMandatory()
                    : EffectExecutionResult.NeedsChoice(new EffectChoiceRequest(
                        EffectChooserRole.Actor, candidates.Select(EffectValue.Minion).ToArray(), 1, 1));
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
                Candidates(context).Count == 0
                    ? EffectExecutionResultKind.ImpossibleMandatory
                    : EffectExecutionResultKind.NeedsChoice;

            public IReadOnlyList<EffectValue> AnticipateCandidates(EffectCapabilityContext context) =>
                Candidates(context).Select(EffectValue.Minion).ToArray();

            /// <summary>Selecting mutates nothing; the remove/defeat node does whatever the effect does.</summary>
            public bool PublishesWithoutApplying => true;

            /// <summary>One minion: the selection names the one subject its sentence names.</summary>
            public int AnticipateMinimumSelections(EffectCapabilityContext context) => 1;

            public IReadOnlyList<EffectOutcomeFact> AnticipateOutcomes(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> chosen) =>
                Publish(context, chosen) ?? Array.Empty<EffectOutcomeFact>();

            /// <summary>
            /// What answering with <paramref name="selected"/> publishes, or nothing where that answer
            /// is not one this selection would take. The space rides beside the identity only for a node
            /// that declared it, because a fact a node did not declare is a snapshot the build refuses to
            /// read back (see <see cref="EffectCapabilityContext.Publishes"/>).
            /// </summary>
            private static IReadOnlyList<EffectOutcomeFact> Publish(
                EffectCapabilityContext context, IReadOnlyList<EffectValue> selected)
            {
                if (selected == null || selected.Count != 1 ||
                    selected[0].Kind != EffectValueKind.Minion ||
                    !Candidates(context).Contains(selected[0].MinionId))
                {
                    return null;
                }

                var outcomes = new List<EffectOutcomeFact>
                {
                    new EffectOutcomeFact(
                        ChosenMinionOutputName, EffectValue.Minion(selected[0].MinionId))
                };

                if (context.Publishes(ChosenMinionSpaceOutputName) &&
                    context.Components?.Board != null &&
                    context.Components.Board.TryGetObjectCoordinate(
                        BoardObjectReference.ForMinion(selected[0].MinionId), out var at))
                {
                    outcomes.Add(new EffectOutcomeFact(
                        ChosenMinionSpaceOutputName, EffectValue.Space(at)));
                }

                return outcomes;
            }

            private static IReadOnlyList<MinionId> Candidates(EffectCapabilityContext context)
            {
                var relation = Relation(context);
                if (relation == SourceTeamRelation.None)
                {
                    return Array.Empty<MinionId>();
                }

                var targets = RadiusTargets(context, out _);
                var board = context.Components?.Board;
                if (board == null)
                {
                    return Array.Empty<MinionId>();
                }

                var minions = new List<MinionId>(targets.Count);
                foreach (var target in targets)
                {
                    if (target.Kind != BoardObjectKind.Minion ||
                        !TargetQuery.StandsInRelationTo(
                            relation, target, context.ActorId, context.Participants, board,
                            context.Modifications))
                    {
                        continue;
                    }

                    if (board.TryGetObjectCoordinate(target, out var at) &&
                        IsAdjacentToAnEnemyHero(board, context, at))
                    {
                        minions.Add(target.MinionId);
                    }
                }

                return minions;
            }

            /// <summary>
            /// Whether one of the six spaces neighbouring <paramref name="coordinate"/> holds a hero
            /// standing on the OpposingSource side of the acting hero - "an enemy hero", read from the
            /// actor's own perspective, which is how every relation in this catalog reads it.
            /// </summary>
            private static bool IsAdjacentToAnEnemyHero(
                IBoardDisplacementAuthority board,
                EffectCapabilityContext context,
                HexCoordinate coordinate)
            {
                var neighbours = coordinate.GetNeighbors();
                return board.Occupancies.Any(occupancy =>
                    neighbours.Contains(occupancy.Coordinate) &&
                    TargetQuery.StandsInRelationTo(
                        SourceTeamRelation.OpposingSource,
                        BoardObjectReference.ForHero(occupancy.HeroId),
                        context.ActorId,
                        context.Participants,
                        board,
                        context.Modifications));
            }
        }
    }
}
