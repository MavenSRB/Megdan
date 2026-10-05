using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Timing;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Effects.Synthetic
{
    /// <summary>
    /// Milestone 8.88. What a printed-icon OBJECT selection offers, beyond its relation: every board
    /// object in the disk, the units alone, or the tokens alone.
    ///
    /// <para>It is an Integer-backed vocabulary whose zero member is the absence of the clause, so an
    /// unbound parameter narrows nothing and every object selection written before 8.88 keeps its
    /// exact candidate set - the shape <see cref="RosterMinionClass.AnyClass"/> already takes.</para>
    /// </summary>
    public enum PrintedIconObjectKind
    {
        /// <summary>The sentence names no kind, so every object in the disk is offered.</summary>
        AnyObject = 0,

        /// <summary>p.19's units - heroes and minions - and no token.</summary>
        UnitsOnly = 1,

        /// <summary>Tokens, and no unit: "a Zombie token in range".</summary>
        TokensOnly = 2,

        /// <summary>A unit or token, excluding an obstacle or other board object.</summary>
        UnitsAndTokens = 3,

        /// <summary>
        /// Milestone 9.30d. A minion or a token, and no hero - Gydion's Polymorph, "Swap an enemy hero
        /// in radius with <b>a token in radius or with an enemy minion in radius</b>". One selection
        /// over the union, for the reason <c>choose-swap-subject@1</c> records: the face offers one
        /// choice, not a choice of which kind of choice to make. The side ("enemy" minion, any token) is
        /// the selection's relation, <c>OpposingSourceOrUnaligned</c>.
        /// </summary>
        MinionsAndTokens = 4
    }

    public static partial class SyntheticTargetingCapabilities
    {
        /// <summary>
        /// Milestone 8.88. "in a straight line" as a REQUIREMENT - the complement of
        /// <see cref="ExcludeStraightLineParameterName"/>, measured from the disk's origin. Ignatia's
        /// "a unit in range in a straight line" is the first face that prints it.
        /// </summary>
        public const string RequireStraightLineParameterName = "requireStraightLine";

        /// <summary>
        /// Milestone 8.88. "at maximum range" / "at maximum radius": the candidate stands at EXACTLY
        /// the printed total from the disk's origin, items composed on - RULING 1's equality, which
        /// Ignatia's and Silverarrow's faces already read.
        /// </summary>
        public const string AtMaximumReachParameterName = "atMaximumReach";

        /// <summary>
        /// Milestone 8.88. The space the printed disk is measured FROM, where it is not the acting
        /// hero's own - Trinkets' "an enemy hero in radius of the Turret". Bound to an earlier node's
        /// published space; unbound, the disk is measured from the actor, as every selection before
        /// 8.88 measures it.
        ///
        /// <para>The acting card still owns the total: p.9 puts the value on the card, and the card
        /// is the actor's. Only the centre moves. "Not adjacent", the straight-line rules and the
        /// maximum-reach equality are all measured from the same centre, because they are statements
        /// about the disk.</para>
        /// </summary>
        public const string MeasuredFromParameterName = "measuredFrom";

        /// <summary>
        /// Milestone 8.88. A SECOND centre the candidate must also lie within the printed total of -
        /// Trinkets' "a unit in range of both you and the Turret". Bound to an earlier node's published
        /// space; unbound, it narrows nothing.
        /// </summary>
        public const string AlsoWithinReachOfParameterName = "alsoWithinReachOf";

        /// <summary>
        /// Milestone 8.88. Whether a neighbouring space that is terrain counts toward the candidate's
        /// adjacency clause - Mrak's "adjacent to terrain, or to a rock token". Terrain is
        /// <see cref="TerrainPresenceQuery.IsTerrain"/>'s answer, so p.19's water ring and a unit
        /// counting as terrain are both terrain here, exactly as everywhere else.
        /// </summary>
        public const string AdjacentToTerrainParameterName = "adjacentToTerrain";

        /// <summary>
        /// Milestone 8.88. Whether a neighbouring space holding a token the ACTING HERO owns counts
        /// toward the candidate's adjacency clause - Mortimer's "adjacent to a Zombie token".
        ///
        /// <para><b>Ownership names the kind.</b> The effect vocabulary has no token-definition value,
        /// and adding one would reach the network codec. Every undealt hero whose clause names a token
        /// kind owns exactly one kind of token, so "a token you own" and "a Zombie token" select the
        /// same pieces for Mortimer; a dealing that composes such a clause records that equivalence
        /// for its hero. Trinkets' Turret is not a token at all: its named obstacle position is
        /// published as a space for <see cref="AdjacentToSpaceParameterName"/> instead.</para>
        /// </summary>
        public const string AdjacentToOwnTokenParameterName = "adjacentToOwnToken";

        /// <summary>
        /// Milestone 8.88. One named space whose neighbours qualify a candidate - Trinkets' "adjacent
        /// to the Turret". Bound to an earlier node's published space.
        /// </summary>
        public const string AdjacentToSpaceParameterName = "adjacentToSpace";

        /// <summary>
        /// Milestone 8.91. Whether a neighbouring space holding ANY token - of any kind and any owner -
        /// counts toward the adjacency clause: Ignatia's "adjacent to a token, or a minion".
        /// </summary>
        public const string AdjacentToAnyTokenParameterName = "adjacentToAnyToken";

        /// <summary>
        /// Milestone 8.91. A token KIND whose pieces count toward the adjacency clause, as a
        /// <see cref="EffectValueKind.TokenKind"/> literal - "adjacent to a Zombie token" named by the
        /// token's definition rather than by its owner, which is what a hero owning two token kinds
        /// (Min's Smoke bomb and Mines) needs. Trinkets' Turret is an obstacle, not a token; Barrier
        /// is his token kind. A token that hides its kind
        /// (a facedown Mine) shows its facedown face to everyone but its owner, so another hero's card
        /// can never find out which facedown piece is which.
        /// </summary>
        public const string AdjacentToTokenKindParameterName = "adjacentToTokenKind";

        /// <summary>
        /// Milestone 8.91. Which of p.19's unit kinds on a neighbouring space counts toward the
        /// adjacency clause, as <see cref="RosterUnitKind"/>: Ignatia's "a minion adjacent to an enemy
        /// hero". Unbound (<see cref="RosterUnitKind.None"/>) is the absence of the clause. Standing
        /// beside somebody is not something done to them, so immunity is not asked (p.19).
        /// </summary>
        public const string AdjacentToUnitKindParameterName = "adjacentToUnitKind";

        /// <summary>
        /// Milestone 8.91. The side of the board those neighbouring units must be on, read from the
        /// acting hero like every relation here. Unbound is any side.
        /// </summary>
        public const string AdjacentToUnitRelationParameterName = "adjacentToUnitRelation";

        /// <summary>
        /// Milestone 8.91. An OBJECT selection's tokens narrowed to one kind - "a Smoke bomb in
        /// radius" - as a <see cref="EffectValueKind.TokenKind"/> literal. Units are untouched by it.
        /// A token hiding its kind is judged by its facedown face except by its owner.
        /// </summary>
        public const string TokenKindParameterName = "tokenKind";

        /// <summary>
        /// Milestone 8.88. How many of the candidate's six neighbours must satisfy the adjacency
        /// clause - Mortimer's "adjacent to two or more Zombie tokens".
        ///
        /// <para>An unbound optional Integer reads as zero, and that is why zero is read as ONE here
        /// rather than as a threshold: "adjacent to" means at least one, and the count form only ever
        /// raises it. It narrows nothing on its own - with no adjacency kind declared there is nothing
        /// to count.</para>
        /// </summary>
        public const string AdjacentAtLeastParameterName = "adjacentAtLeast";

        /// <summary>
        /// Milestone 9.6. The adjacency clause read as its complement - Swift's "an enemy hero in
        /// radius, who is NOT adjacent to terrain". A candidate qualifies when fewer of its neighbours
        /// satisfy the declared adjacency kinds than <see cref="AdjacentAtLeastParameterName"/> asks.
        /// It narrows nothing on its own, like the count: with no adjacency kind declared there is
        /// nothing to negate.
        /// </summary>
        public const string NotAdjacentParameterName = "notAdjacent";

        /// <summary>
        /// Milestone 9.8a. A placement clause: the space is a spawn point (any of p.19's, minion or
        /// hero, in any zone) or a space adjacent to the acting hero - Mortimer's Awaken!, "into spawn
        /// points in radius, counting spaces adjacent to you as spaces with spawn points" (MO-Q2). The
        /// printed reach still bounds the space; this clause narrows which spaces inside it qualify.
        /// Unbound, it narrows nothing.
        /// </summary>
        public const string SpawnPointOrAdjacentParameterName = "spawnPointOrAdjacent";

        /// <summary>
        /// Milestone 8.88. Which kinds an OBJECT selection offers, as <see cref="PrintedIconObjectKind"/>.
        /// Only the object arms declare it: every other arm's kind is its identity.
        /// </summary>
        public const string AdmittedObjectKindParameterName = "admittedObjectKind";

        /// <summary>
        /// Milestone 8.88. Whether an OBJECT selection offers only the tokens the acting hero owns -
        /// "Swap with a Zombie token in range". Units are untouched by it; pair it with
        /// <see cref="PrintedIconObjectKind.TokensOnly"/> to offer tokens alone. Ownership names the
        /// kind for the reason <see cref="AdjacentToOwnTokenParameterName"/> records.
        /// </summary>
        public const string OwnTokensOnlyParameterName = "ownTokensOnly";

        /// <summary>
        /// Milestone 8.88. Every filter the printed-icon family composes, in declaration order. Each
        /// is declared on every arm it can mean something on, and refused by name on the rest - see
        /// <see cref="PrintedIconFilterRefusal"/>.
        /// </summary>
        public static IReadOnlyList<string> PrintedIconFilterParameterNames { get; } =
            Array.AsReadOnly(new[]
            {
                IncludeSelfParameterName,
                TargetRelationParameterName,
                ExcludeAdjacentParameterName,
                ExcludeStraightLineParameterName,
                RequireStraightLineParameterName,
                AtMaximumReachParameterName,
                MeasuredFromParameterName,
                AlsoWithinReachOfParameterName,
                ExcludeTheAttackerParameterName,
                ExcludeTheAttackTargetParameterName,
                EffectSelectionDeclaration.DistinctAcrossParameterName,
                OnceAcrossTheGroupParameterName,
                ExcludedHeroParameterName,
                ExcludedMinionParameterName,
                AdmittedMinionClassParameterName,
                HasCardInDiscardParameterName,
                ResolvedCardThisTurnParameterName,
                DiscardedCardThisTurnParameterName,
                RemainedSincePreviousTurnParameterName,
                HoldsAnUnresolvedCardParameterName,
                MinimumCardsInHandParameterName,
                RequiresEmptyHandParameterName,
                BothDiscardPilesEmptyParameterName,
                AdjacentToThreeOrMoreObstaclesParameterName,
                AdjacentToEmptyMinionSpawnPointParameterName,
                OccupiesSpawnPointParameterName,
                AdjacentToAnEnemyHeroOrHoldingADiscardParameterName,
                AdjacentToTerrainParameterName,
                AdjacentToOwnTokenParameterName,
                AdjacentToSpaceParameterName,
                AdjacentToAnyTokenParameterName,
                AdjacentToTokenKindParameterName,
                AdjacentToUnitKindParameterName,
                AdjacentToUnitRelationParameterName,
                AdjacentAtLeastParameterName,
                NotAdjacentParameterName,
                AdmittedObjectKindParameterName,
                OwnTokensOnlyParameterName,
                TokenKindParameterName
            });

        /// <summary>
        /// Milestone 8.88. Why <paramref name="arm"/> refuses <paramref name="parameterName"/>, or
        /// <c>null</c> where it declares it (or it is not one of
        /// <see cref="PrintedIconFilterParameterNames"/>, or the reference is not a printed-icon arm).
        ///
        /// <para>A refusal is never "no face prints it": that was the rule before 8.88, and it is what
        /// made a new phrase mint a capability. It is always a statement that the filter cannot be true
        /// of anything the arm offers - a minion owns no cards and is never the acting hero, and a
        /// hero, minion or unit arm's kind is its own identity.</para>
        /// </summary>
        public static string PrintedIconFilterRefusal(EffectCapabilityReference arm, string parameterName)
        {
            var handler = RegisteredHandlers().OfType<PrintedIconTargetHandler>()
                .FirstOrDefault(candidate => candidate.Reference.Equals(arm));
            if (handler == null || !PrintedIconFilterParameterNames.Contains(parameterName))
            {
                return null;
            }

            return RefusalFor(handler.Subject, parameterName);
        }

        private static string RefusalFor(PrintedIconSelectionSubject subject, string parameterName)
        {
            if (subject != PrintedIconSelectionSubject.BoardObject &&
                (parameterName == AdmittedObjectKindParameterName ||
                    parameterName == OwnTokensOnlyParameterName ||
                    parameterName == TokenKindParameterName))
            {
                return "this arm's kind is its identity; only an object selection chooses among kinds";
            }

            if (subject == PrintedIconSelectionSubject.Hero &&
                (parameterName == ExcludedMinionParameterName ||
                    parameterName == AdmittedMinionClassParameterName))
            {
                return "a hero selection offers no minion to exclude or classify";
            }

            if (subject == PrintedIconSelectionSubject.Minion)
            {
                if (parameterName == IncludeSelfParameterName ||
                    parameterName == ExcludeTheAttackerParameterName ||
                    parameterName == ExcludedHeroParameterName)
                {
                    return "a minion selection offers no hero: the actor and the attacker are heroes";
                }

                if (HeroCardPredicates.Contains(parameterName))
                {
                    return "a minion owns no cards and no hand, so the clause is true of no candidate";
                }
            }

            return null;
        }

        /// <summary>
        /// The clauses only a hero can satisfy, because they read a hero's own cards or a hero-scoped
        /// rule. On an arm that offers minions as well, a minion fails them.
        /// </summary>
        private static readonly string[] HeroCardPredicates =
        {
            HasCardInDiscardParameterName,
            ResolvedCardThisTurnParameterName,
            DiscardedCardThisTurnParameterName,
            RemainedSincePreviousTurnParameterName,
            HoldsAnUnresolvedCardParameterName,
            MinimumCardsInHandParameterName,
            RequiresEmptyHandParameterName,
            BothDiscardPilesEmptyParameterName,
            AdjacentToThreeOrMoreObstaclesParameterName
        };

        /// <summary>
        /// Milestone 8.88. One arm's parameter schema: every filter it does not refuse, each optional
        /// except the ones that arm has always required.
        /// </summary>
        private static IReadOnlyList<EffectCapabilityParameter> PrintedIconParameters(
            PrintedIconSelectionSubject subject, PrintedValueChannel channel)
        {
            var parameters = new List<EffectCapabilityParameter>
            {
                new EffectCapabilityParameter(
                    "activity",
                    EffectValueKind.Integer,
                    integerDomain: EffectIntegerDomain.AllExcept(RuleActivity.None))
            };

            foreach (var name in PrintedIconFilterParameterNames)
            {
                if (RefusalFor(subject, name) != null)
                {
                    continue;
                }

                parameters.Add(Declare(name, IsRequiredOn(subject, channel, name)));
            }

            parameters.Add(new EffectCapabilityParameter(
                EffectSelectionDeclaration.ConsumerParameterName,
                EffectValueKind.Node,
                isRequired: false));
            return parameters;
        }

        /// <summary>
        /// The parameters each arm required before 8.88, which it still requires: every definition
        /// written against it binds them, and loosening one would let a definition forget a clause its
        /// face prints. Everything 8.88 adds is optional, and unbound is the absence of the clause.
        /// </summary>
        private static bool IsRequiredOn(
            PrintedIconSelectionSubject subject, PrintedValueChannel channel, string name)
        {
            switch (subject)
            {
                case PrintedIconSelectionSubject.BoardObject:
                    return name == IncludeSelfParameterName ||
                        (channel == PrintedValueChannel.Range && name == ExcludeStraightLineParameterName);
                case PrintedIconSelectionSubject.Hero:
                    return name == TargetRelationParameterName ||
                        (channel == PrintedValueChannel.Radius && name == ExcludeAdjacentParameterName) ||
                        (channel == PrintedValueChannel.Range && name == ExcludeTheAttackerParameterName);
                default:
                    return name == TargetRelationParameterName;
            }
        }

        private static EffectCapabilityParameter Declare(string name, bool isRequired)
        {
            switch (name)
            {
                case TargetRelationParameterName:
                    return new EffectCapabilityParameter(
                        name,
                        EffectValueKind.Integer,
                        isRequired,
                        EffectIntegerDomain.AllExcept(SourceTeamRelation.None));
                case AdmittedMinionClassParameterName:
                    return new EffectCapabilityParameter(
                        name, EffectValueKind.Integer, isRequired, EffectIntegerDomain.All<RosterMinionClass>());
                case AdmittedObjectKindParameterName:
                    return new EffectCapabilityParameter(
                        name, EffectValueKind.Integer, isRequired, EffectIntegerDomain.All<PrintedIconObjectKind>());
                case AdjacentToUnitKindParameterName:
                    return new EffectCapabilityParameter(
                        name, EffectValueKind.Integer, isRequired, EffectIntegerDomain.All<RosterUnitKind>());
                case AdjacentToUnitRelationParameterName:
                    return new EffectCapabilityParameter(
                        name, EffectValueKind.Integer, isRequired, EffectIntegerDomain.All<SourceTeamRelation>());
                case AdjacentToTokenKindParameterName:
                case TokenKindParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.TokenKind, isRequired);
                case MinimumCardsInHandParameterName:
                case AdjacentAtLeastParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.Integer, isRequired);
                case ExcludedHeroParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.Hero, isRequired);
                case EffectSelectionDeclaration.DistinctAcrossParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.Node, isRequired);
                case ExcludedMinionParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.Minion, isRequired);
                case MeasuredFromParameterName:
                case AlsoWithinReachOfParameterName:
                case AdjacentToSpaceParameterName:
                    return new EffectCapabilityParameter(name, EffectValueKind.Coordinate, isRequired);
                default:
                    return new EffectCapabilityParameter(name, EffectValueKind.Boolean, isRequired);
            }
        }

        /// <summary>
        /// Milestone 8.88. The kinds a pipeline call may offer, before any clause narrows them.
        /// </summary>
        [Flags]
        internal enum PrintedIconKinds
        {
            None = 0,
            Heroes = 1,
            Minions = 2,
            Tokens = 4,
            Units = Heroes | Minions,
            All = Heroes | Minions | Tokens
        }

        /// <summary>
        /// Milestone 8.88. The one candidate derivation every printed-icon selection asks, and every
        /// hero-local selection that mirrored it now asks too: p.9's disk read off the acting card's
        /// Radius or Range total with p.16's items composed on, p.18's immunity filtered through
        /// <see cref="TargetQuery"/>, and then every declared clause, each conjunctive, in the disk's
        /// own canonical order.
        ///
        /// <para>Every clause reads its input unconditionally. An arm that does not declare a
        /// parameter cannot have it bound - the definition validator refuses the binding by name - so
        /// an undeclared clause reads as its absence, and one reading serves every arm.</para>
        ///
        /// <para>Derived on every ask - the offer, the answer and a restored pause - for the reason
        /// every selection here re-derives: what was true when a choice opened is not what its answer
        /// applies to.</para>
        /// </summary>
        internal static IReadOnlyList<BoardObjectReference> PrintedIconSelection(
            EffectCapabilityContext context,
            PrintedValueChannel channel,
            PrintedIconKinds kinds,
            bool relationRequired,
            HexCoordinate? originOverride = null)
        {
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
            var actor = BoardObjectReference.ForHero(context.ActorId);
            if (board == null || !board.TryGetObjectCoordinate(actor, out var actorAt))
            {
                return Array.Empty<BoardObjectReference>();
            }

            // The side of the board. A hero, minion or unit arm requires it and names nobody without
            // it; an object arm may leave it unbound, which is the whole board as it has always been.
            var relation = (SourceTeamRelation)context.Input(TargetRelationParameterName).IntegerValue;
            if (!Enum.IsDefined(typeof(SourceTeamRelation), relation) ||
                (relationRequired && relation == SourceTeamRelation.None))
            {
                return Array.Empty<BoardObjectReference>();
            }

            // A class or kind the vocabulary cannot answer is a claim this selection refuses rather than
            // quietly widening to everybody - milestone 8.24's reading, applied to both adjectives.
            var admittedClass = (RosterMinionClass)context.Input(AdmittedMinionClassParameterName).IntegerValue;
            var admittedKind = (PrintedIconObjectKind)context.Input(AdmittedObjectKindParameterName).IntegerValue;
            if (!Enum.IsDefined(typeof(RosterMinionClass), admittedClass) ||
                !Enum.IsDefined(typeof(PrintedIconObjectKind), admittedKind))
            {
                return Array.Empty<BoardObjectReference>();
            }

            if (admittedKind == PrintedIconObjectKind.UnitsOnly)
            {
                kinds &= PrintedIconKinds.Units;
            }
            else if (admittedKind == PrintedIconObjectKind.TokensOnly)
            {
                kinds &= PrintedIconKinds.Tokens;
            }
            else if (admittedKind == PrintedIconObjectKind.UnitsAndTokens)
            {
                kinds &= PrintedIconKinds.Units | PrintedIconKinds.Tokens;
            }
            else if (admittedKind == PrintedIconObjectKind.MinionsAndTokens)
            {
                kinds &= PrintedIconKinds.Minions | PrintedIconKinds.Tokens;
            }

            var printedTotal = channel == PrintedValueChannel.Radius
                ? RadiusValueQuery.Calculate(
                    context.SourceCardInstanceId,
                    context.SourceDefinitionReference,
                    context.ActionDefinitionId,
                    context.CardState.Definitions,
                    context.ActorId,
                    context.HeroItems,
                    context.Modifications)
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
                // A card printing no icon of this channel has no such bound at all, and p.16 does not
                // give it one: an item raises a printed icon and never invents one.
                return Array.Empty<BoardObjectReference>();
            }

            var total = printedTotal.Total;
            var includeSelf = context.Input(IncludeSelfParameterName).BooleanValue;
            var measuredFrom = context.Input(MeasuredFromParameterName);
            var moved = originOverride.HasValue ||
                measuredFrom.Kind == EffectValueKind.Coordinate;
            var origin = originOverride ??
                (measuredFrom.Kind == EffectValueKind.Coordinate
                    ? measuredFrom.Coordinate : actorAt);

            // p.9 states one geometry for both icons, counted through obstacles because there are no
            // line-of-sight rules. Where the centre has moved, the object standing on it is not the
            // actor, so the disk is asked with its centre admitted and the actor alone is then left out
            // unless the sentence includes them.
            var disk = channel == PrintedValueChannel.Radius
                ? RadiusQuery.Targets(
                    origin,
                    printedTotal,
                    includeSelf || moved,
                    actor.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity)
                : TargetQuery.FindTargetsWithin(
                    origin,
                    total,
                    includeSelf || moved,
                    actor.AsRuleObject(),
                    activity,
                    board,
                    context.Participants,
                    context.Modifications,
                    context.ImmunityOverrides,
                    activity);

            var clauses = new PrintedIconClauses(context, board, origin, total);
            if (clauses.EmptiesTheOffer)
            {
                return Array.Empty<BoardObjectReference>();
            }

            var found = new List<BoardObjectReference>(disk.Count);
            foreach (var target in disk)
            {
                if (!Offers(kinds, target, context.Modifications))
                {
                    continue;
                }

                // Milestone 9.21f (RZ-Q5). Unmoved, the disk has already left the acting body's own
                // space out; references compare at hero grain, so this also leaves out every OTHER
                // body of a many-bodied actor - none is "a unit" or "a friendly hero" for his own text
                // (p.17 "Friendly ... never includes you"). A one-bodied actor stands nowhere else, so
                // every selection composed before it keeps its exact candidate set.
                if (!includeSelf && target.Equals(actor))
                {
                    continue;
                }

                if (relation != SourceTeamRelation.None &&
                    !TargetQuery.StandsInRelationTo(
                        relation, target, context.ActorId, context.Participants, board,
                        context.Modifications))
                {
                    continue;
                }

                if (target.Kind == BoardObjectKind.Minion && !AdmitsClass(admittedClass, target.MinionId, board))
                {
                    continue;
                }
                if (target.Kind == BoardObjectKind.Token &&
                    EffectiveUnitKindQuery.Is(
                        RuleObject.ForToken(target.TokenInstanceId),
                        EffectiveUnitKind.Minion, context.Modifications) &&
                    admittedClass != RosterMinionClass.AnyClass &&
                    !SyntheticRosterCapabilities.AdmittedClassIncludes(
                        admittedClass,
                        EffectiveUnitKindQuery.TokenMinionType(
                            RuleObject.ForToken(target.TokenInstanceId),
                            context.Modifications)))
                    continue;

                if (!board.TryGetObjectCoordinate(target, out var at) || !clauses.Admit(target, at))
                {
                    continue;
                }

                found.Add(target);
            }

            return found;
        }

        private static bool Offers(
            PrintedIconKinds kinds, BoardObjectReference target,
            RuleModificationSet modifications)
        {
            switch (target.Kind)
            {
                case BoardObjectKind.Hero:
                    return (kinds & PrintedIconKinds.Heroes) != 0;
                case BoardObjectKind.Minion:
                    return (kinds & PrintedIconKinds.Minions) != 0;
                case BoardObjectKind.Token:
                    return (kinds & PrintedIconKinds.Tokens) != 0 ||
                        ((kinds & PrintedIconKinds.Minions) != 0 &&
                         EffectiveUnitKindQuery.Is(
                             RuleObject.ForToken(target.TokenInstanceId),
                             EffectiveUnitKind.Minion, modifications));
                default:
                    return false;
            }
        }

        /// <summary>
        /// Whether one minion on the board is of a class the printed sentence admits. It reads the
        /// minion's own standard type off the board, because p.22 prints the three
        /// classes as identities; a minion the board does not know is admitted by nothing.
        /// </summary>
        private static bool AdmitsClass(
            RosterMinionClass admittedClass, MinionId minionId, IBoardDisplacementAuthority board)
        {
            if (admittedClass == RosterMinionClass.AnyClass)
            {
                return true;
            }

            var minion = board.Minions.FirstOrDefault(value => value.MinionId.Equals(minionId));
            return minion != null &&
                SyntheticRosterCapabilities.AdmittedClassIncludes(admittedClass, minion.MinionType);
        }

        /// <summary>
        /// Milestone 8.88. Every per-candidate clause of the printed-icon family, read once per offer.
        /// Each one was a separate narrowing on the one arm whose face printed it; here each is one
        /// test any arm applies, so a clause learned for one hero is available to every sentence.
        /// </summary>
        private sealed class PrintedIconClauses
        {
            private readonly EffectCapabilityContext context;
            private readonly IBoardDisplacementAuthority board;
            private readonly HexCoordinate origin;
            private readonly int total;

            private readonly HeroId excludedAttacker;
            private readonly bool excludesTheAttackTarget;
            private readonly IReadOnlyList<EffectValue> chosenAcrossTheGroup;
            private readonly EffectValue excludedHero;
            private readonly EffectValue excludedMinion;
            private readonly PrintedIconSpaceClauses space;

            private readonly bool requiresACardInDiscard;
            private readonly bool requiresResolvedCardThisTurn;
            private readonly bool requiresDiscardedCardThisTurn;
            private readonly bool requiresRemainedSincePreviousTurn;
            private readonly bool requiresAnUnresolvedCard;
            private readonly int minimumCardsInHand;
            private readonly bool requiresEmptyHand;
            private readonly bool requiresBothDiscardsEmpty;
            private readonly bool requiresThreeObstaclesBeside;
            private readonly bool requiresAnEmptySpawnPointBeside;
            private readonly IReadOnlyList<HexCoordinate> qualifyingSpawnPoints;
            private readonly bool requiresASpawnPoint;
            private readonly bool requiresTheSwapQualifier;
            private readonly bool ownTokensOnly;
            private readonly EffectValue tokenKind;

            public PrintedIconClauses(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                HexCoordinate origin,
                int total)
            {
                this.context = context;
                this.board = board;
                this.origin = origin;
                this.total = total;

                // Milestone 8.11. "other than the attacker": the enclosing Attack's own actor, a fact
                // about the action this text is answering.
                excludedAttacker = context.Input(ExcludeTheAttackerParameterName).BooleanValue
                    ? context.EnclosingAttack.ActorId
                    : default;

                // Milestone 9.8c. "ANOTHER enemy hero" in an Attack's text, and "each enemy hero can
                // only be targeted once" across a modal's passes - every answer the selection's group
                // has already been given in this resolution.
                excludesTheAttackTarget = context.Input(ExcludeTheAttackTargetParameterName).BooleanValue;
                chosenAcrossTheGroup = context.AnswersGivenAcrossTheGroup.SelectMany(answer => answer).ToArray();

                // Milestone 8.69c. "on different targets" / "a DIFFERENT friendly unit". An unbound
                // binding carries nobody, which is a pass that named nobody and excludes nobody.
                excludedHero = context.Input(ExcludedHeroParameterName);
                excludedMinion = context.Input(ExcludedMinionParameterName);

                space = new PrintedIconSpaceClauses(context, board, origin, total);

                requiresACardInDiscard = context.Input(HasCardInDiscardParameterName).BooleanValue;
                requiresResolvedCardThisTurn = context.Input(ResolvedCardThisTurnParameterName).BooleanValue;
                requiresDiscardedCardThisTurn = context.Input(DiscardedCardThisTurnParameterName).BooleanValue;
                requiresRemainedSincePreviousTurn = context.Input(RemainedSincePreviousTurnParameterName).BooleanValue;
                requiresAnUnresolvedCard = context.Input(HoldsAnUnresolvedCardParameterName).BooleanValue;
                minimumCardsInHand = context.Input(MinimumCardsInHandParameterName).IntegerValue;
                requiresEmptyHand = context.Input(RequiresEmptyHandParameterName).BooleanValue;
                requiresBothDiscardsEmpty = context.Input(BothDiscardPilesEmptyParameterName).BooleanValue;
                requiresThreeObstaclesBeside =
                    context.Input(AdjacentToThreeOrMoreObstaclesParameterName).BooleanValue;
                requiresAnEmptySpawnPointBeside =
                    context.Input(AdjacentToEmptyMinionSpawnPointParameterName).BooleanValue;
                requiresASpawnPoint = context.Input(OccupiesSpawnPointParameterName).BooleanValue;
                requiresTheSwapQualifier =
                    context.Input(AdjacentToAnEnemyHeroOrHoldingADiscardParameterName).BooleanValue;

                ownTokensOnly = context.Input(OwnTokensOnlyParameterName).BooleanValue;
                tokenKind = context.Input(TokenKindParameterName);

                // Milestone 8.57d. Rowenna's own half of "both you and an enemy hero ... have no cards
                // in the discard" is a fact about her and not about any candidate, so it is asked once
                // and a card in her pile empties the whole offer.
                EmptiesTheOffer = (context.Input(OnceAcrossTheGroupParameterName).BooleanValue &&
                        chosenAcrossTheGroup.Count > 0) ||
                    requiresBothDiscardsEmpty &&
                    HeroDiscardPileQuery.HasAnyCard(context.CardState.Instances, context.ActorId);

                // Milestone 8.35z28. The qualifying points are a property of the board and the map,
                // derived once for the whole offer. The overlay is read from the ACTOR: Tide of
                // Darkness is Dodger's own Ultimate, and Death Trap then asks whether the candidate
                // stands beside a qualifying point.
                qualifyingSpawnPoints =
                    requiresAnEmptySpawnPointBeside && context.Components?.Tokens != null
                        ? EmptyMinionSpawnPointQuery.FindInCurrentBattleZone(
                            board,
                            context.Components.Tokens,
                            SpawnPointOverlay.ForActor(
                                context.Modifications, context.Participants, context.ActorId))
                        : Array.Empty<HexCoordinate>();
            }

            public bool EmptiesTheOffer { get; }

            private bool IsTheAttackTarget(BoardObjectReference target) =>
                target.Kind == BoardObjectKind.Hero
                    ? context.EnclosingAttack.HeroTargetId.IsDefined &&
                        target.HeroId.Equals(context.EnclosingAttack.HeroTargetId)
                    : target.Kind == BoardObjectKind.Minion &&
                        context.EnclosingAttack.MinionTargetId.IsDefined &&
                        target.MinionId.Equals(context.EnclosingAttack.MinionTargetId);

            /// <summary>Whether an earlier answer of the group named this candidate.</summary>
            private static bool Names(EffectValue chosen, BoardObjectReference target, HexCoordinate at)
            {
                switch (chosen.Kind)
                {
                    case EffectValueKind.Hero:
                        return target.Kind == BoardObjectKind.Hero && target.HeroId.Equals(chosen.HeroId);
                    case EffectValueKind.Minion:
                        return target.Kind == BoardObjectKind.Minion && target.MinionId.Equals(chosen.MinionId);
                    case EffectValueKind.Coordinate:
                        return target.Kind == BoardObjectKind.Token && at.Equals(chosen.Coordinate);
                    default:
                        return false;
                }
            }

            public bool Admit(BoardObjectReference target, HexCoordinate at)
            {
                if (excludedAttacker.IsDefined && target.Kind == BoardObjectKind.Hero &&
                    target.HeroId.Equals(excludedAttacker))
                {
                    return false;
                }

                if (excludedHero.Kind == EffectValueKind.Hero && target.Kind == BoardObjectKind.Hero &&
                    target.HeroId.Equals(excludedHero.HeroId))
                {
                    return false;
                }

                if (excludesTheAttackTarget && IsTheAttackTarget(target))
                {
                    return false;
                }

                if (chosenAcrossTheGroup.Any(chosen => Names(chosen, target, at)))
                {
                    return false;
                }

                if (excludedMinion.Kind == EffectValueKind.Minion && target.Kind == BoardObjectKind.Minion &&
                    target.MinionId.Equals(excludedMinion.MinionId))
                {
                    return false;
                }

                // Geometry and adjacency: the clauses a bare space can satisfy, shared with placement.
                if (!space.Admit(at))
                {
                    return false;
                }

                if (!AdmitsByTheCandidatesCards(target))
                {
                    return false;
                }

                // "who is adjacent to an empty spawn point in the battle zone" - a second bound
                // measured from the candidate's own space.
                if (requiresAnEmptySpawnPointBeside &&
                    !qualifyingSpawnPoints.Any(point => HexDistanceQuery.Calculate(at, point) == 1))
                {
                    return false;
                }

                // "occupying a spawn point" - p.19's own definition, any Spawn point of either type.
                if (requiresASpawnPoint && !StandsOnASpawnPoint(board, at))
                {
                    return false;
                }

                // Rowenna's disjunction, one clause rather than two narrowings.
                if (requiresTheSwapQualifier && !SatisfiesTheSwapQualifier(context, board, target, at))
                {
                    return false;
                }

                if (ownTokensOnly && target.Kind == BoardObjectKind.Token &&
                    !PrintedIconSpaceClauses.IsOwnToken(context, at))
                {
                    return false;
                }

                // Milestone 8.91. "a Smoke bomb in radius": a token of the named kind, as the acting
                // hero may know it.
                if (tokenKind.Kind == EffectValueKind.TokenKind && target.Kind == BoardObjectKind.Token &&
                    !PrintedIconSpaceClauses.HoldsTokenOfKind(context, at, tokenKind.TokenKindId))
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// The clauses that read a hero's own cards or a hero-scoped rule. Asked of a hero through
            /// the one owner of each phrase; a minion or a token satisfies none of them.
            /// </summary>
            private bool AdmitsByTheCandidatesCards(BoardObjectReference target)
            {
                var anyRequired = requiresACardInDiscard || requiresAnUnresolvedCard ||
                    requiresResolvedCardThisTurn || requiresDiscardedCardThisTurn ||
                    requiresRemainedSincePreviousTurn ||
                    minimumCardsInHand > 0 || requiresEmptyHand || requiresBothDiscardsEmpty ||
                    requiresThreeObstaclesBeside;
                if (!anyRequired)
                {
                    return true;
                }

                if (target.Kind != BoardObjectKind.Hero)
                {
                    return false;
                }

                var instances = context.CardState.Instances;
                if (requiresACardInDiscard && !HeroDiscardPileQuery.HasAnyCard(instances, target.HeroId))
                {
                    return false;
                }

                if (requiresResolvedCardThisTurn &&
                    context.RosterState.CurrentTurnCardHistory?.HasResolvedCardThisTurn(target.HeroId) != true)
                {
                    return false;
                }

                if (requiresDiscardedCardThisTurn &&
                    !instances.Any(card => card.OwnerId.Equals(target.HeroId) &&
                        CardLifecycleAuthority.IsDiscardedRestingCard(card) &&
                        context.RosterState.CurrentTurnCardHistory?.WasDiscardedThisTurn(card.Id) == true))
                {
                    return false;
                }

                if (requiresRemainedSincePreviousTurn &&
                    (!(board is IUnitTurnHistory history) || !history.RemainedSincePreviousTurn(target)))
                {
                    return false;
                }

                if (requiresAnUnresolvedCard && !HeroUnresolvedCardQuery.Holds(instances, target.HeroId))
                {
                    return false;
                }

                if (minimumCardsInHand > 0 &&
                    !HeroHandQuery.HasAtLeast(instances, target.HeroId, minimumCardsInHand))
                {
                    return false;
                }

                if (requiresEmptyHand && !HeroHandQuery.IsEmpty(instances, target.HeroId))
                {
                    return false;
                }

                if (requiresBothDiscardsEmpty && HeroDiscardPileQuery.HasAnyCard(instances, target.HeroId))
                {
                    return false;
                }

                // Milestone 8.69a. Through the one owner of the phrase, so his Ultimate's counts-as
                // overlay reaches this clause as it reaches every other reading of it.
                if (requiresThreeObstaclesBeside &&
                    (context.Components?.Tokens == null ||
                        !ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                            context.Components.Tokens, target.HeroId, context.Modifications,
                            // Milestone 9.21c: the body this candidate is (RZ-Q2).
                            target.HeroBody)))
                {
                    return false;
                }

                return true;
            }
        }

        /// <summary>
        /// Milestone 8.88a. The clauses a bare SPACE can satisfy - the disk's geometry and the space's
        /// neighbours - which is everything a placement destination can be asked. The printed-icon
        /// family and both placement families read them here, so "in a straight line", "at maximum
        /// radius", "adjacent to terrain" and the rest mean one thing whether a card chooses a unit or
        /// a space.
        /// </summary>
        public static IReadOnlyList<string> PlacementClauseParameterNames { get; } =
            Array.AsReadOnly(new[]
            {
                ExcludeAdjacentParameterName,
                ExcludeStraightLineParameterName,
                RequireStraightLineParameterName,
                AtMaximumReachParameterName,
                MeasuredFromParameterName,
                AlsoWithinReachOfParameterName,
                AdjacentToTerrainParameterName,
                AdjacentToOwnTokenParameterName,
                AdjacentToSpaceParameterName,
                AdjacentToAnyTokenParameterName,
                AdjacentToTokenKindParameterName,
                AdjacentToUnitKindParameterName,
                AdjacentToUnitRelationParameterName,
                AdjacentAtLeastParameterName,
                NotAdjacentParameterName,
                SpawnPointOrAdjacentParameterName
            });

        /// <summary>
        /// Milestone 8.88a. The placement clauses as optional parameters, for a placement declaration
        /// to append to its own. Unbound, each is the absence of its clause.
        /// </summary>
        internal static IEnumerable<EffectCapabilityParameter> PlacementClauseParameters() =>
            PlacementClauseParameterNames.Select(name => Declare(name, isRequired: false));

        /// <summary>
        /// Milestone 8.88a. The space a reach is measured from where the declaration moved it
        /// (<see cref="MeasuredFromParameterName"/>), or <paramref name="otherwise"/>.
        /// </summary>
        internal static HexCoordinate MeasuredFromOr(EffectCapabilityContext context, HexCoordinate otherwise)
        {
            var moved = context.Input(MeasuredFromParameterName);
            return moved.Kind == EffectValueKind.Coordinate ? moved.Coordinate : otherwise;
        }

        /// <summary>
        /// Milestone 8.88a. The space clauses read once for one offer, measured from one centre at one
        /// total. See <see cref="PlacementClauseParameterNames"/>.
        /// </summary>
        internal sealed class PrintedIconSpaceClauses
        {
            private readonly EffectCapabilityContext context;
            private readonly IBoardDisplacementAuthority board;
            private readonly HexCoordinate origin;
            private readonly int total;
            private readonly bool excludeAdjacent;
            private readonly bool excludeStraightLine;
            private readonly bool requireStraightLine;
            private readonly bool atMaximumReach;
            private readonly HexCoordinate? alsoWithinReachOf;
            private readonly bool adjacentToTerrain;
            private readonly bool adjacentToOwnToken;
            private readonly HexCoordinate? adjacentToSpace;
            private readonly bool adjacentToAnyToken;
            private readonly EffectValue adjacentToTokenKind;
            private readonly RosterUnitKind adjacentToUnitKind;
            private readonly SourceTeamRelation adjacentToUnitRelation;
            private readonly int adjacentAtLeast;
            private readonly bool notAdjacent;
            private readonly bool spawnPointOrAdjacent;

            public PrintedIconSpaceClauses(
                EffectCapabilityContext context,
                IBoardDisplacementAuthority board,
                HexCoordinate origin,
                int total)
            {
                this.context = context;
                this.board = board;
                this.origin = origin;
                this.total = total;
                excludeAdjacent = context.Input(ExcludeAdjacentParameterName).BooleanValue;
                excludeStraightLine = context.Input(ExcludeStraightLineParameterName).BooleanValue;
                requireStraightLine = context.Input(RequireStraightLineParameterName).BooleanValue;
                atMaximumReach = context.Input(AtMaximumReachParameterName).BooleanValue;
                alsoWithinReachOf = CoordinateOrNothing(context.Input(AlsoWithinReachOfParameterName));
                adjacentToTerrain = context.Input(AdjacentToTerrainParameterName).BooleanValue;
                adjacentToOwnToken = context.Input(AdjacentToOwnTokenParameterName).BooleanValue;
                adjacentToSpace = CoordinateOrNothing(context.Input(AdjacentToSpaceParameterName));
                adjacentToAnyToken = context.Input(AdjacentToAnyTokenParameterName).BooleanValue;
                adjacentToTokenKind = context.Input(AdjacentToTokenKindParameterName);
                adjacentToUnitKind = (RosterUnitKind)context.Input(AdjacentToUnitKindParameterName).IntegerValue;
                adjacentToUnitRelation =
                    (SourceTeamRelation)context.Input(AdjacentToUnitRelationParameterName).IntegerValue;
                adjacentAtLeast = Math.Max(1, context.Input(AdjacentAtLeastParameterName).IntegerValue);
                notAdjacent = context.Input(NotAdjacentParameterName).BooleanValue;
                spawnPointOrAdjacent = context.Input(SpawnPointOrAdjacentParameterName).BooleanValue;
            }

            public bool Admit(HexCoordinate at)
            {
                var distance = HexDistanceQuery.Calculate(origin, at);

                // p.9's disk without its innermost ring, stated as the exact distance the word means.
                if (excludeAdjacent && distance == 1)
                {
                    return false;
                }

                // RULING 1's equality: exactly the printed total, items composed on.
                if (atMaximumReach && distance != total)
                {
                    return false;
                }

                // p.19's straight-line rule, membership or its complement, anchored at the centre.
                var inLine = StraightLineQuery.AreInStraightLine(origin, at);
                if ((excludeStraightLine && inLine) || (requireStraightLine && !inLine))
                {
                    return false;
                }

                if (alsoWithinReachOf.HasValue &&
                    HexDistanceQuery.Calculate(alsoWithinReachOf.Value, at) > total)
                {
                    return false;
                }

                if (spawnPointOrAdjacent && !IsSpawnPointOrBesideTheActor(at))
                {
                    return false;
                }

                return !DeclaresAdjacency || (QualifyingNeighbours(at) >= adjacentAtLeast) != notAdjacent;
            }

            private bool IsSpawnPointOrBesideTheActor(HexCoordinate at) =>
                board != null &&
                (StandsOnASpawnPoint(board, at) ||
                    board.TryGetObjectCoordinate(
                        BoardObjectReference.ForHero(context.ActorId), out var actorAt) &&
                    HexDistanceQuery.Calculate(actorAt, at) == 1);

            private bool DeclaresAdjacency =>
                adjacentToTerrain || adjacentToOwnToken || adjacentToSpace.HasValue || adjacentToAnyToken ||
                adjacentToTokenKind.Kind == EffectValueKind.TokenKind ||
                adjacentToUnitKind != RosterUnitKind.None;

            /// <summary>
            /// How many of the six spaces around <paramref name="at"/> satisfy the declared adjacency
            /// kinds. A neighbour is counted once however many kinds it satisfies, because p.17's
            /// Adjacent is a question about spaces.
            /// </summary>
            private int QualifyingNeighbours(HexCoordinate at)
            {
                var count = 0;
                foreach (var neighbour in at.GetNeighbors())
                {
                    if ((adjacentToSpace.HasValue && neighbour.Equals(adjacentToSpace.Value)) ||
                        (adjacentToTerrain && board != null &&
                            TerrainPresenceQuery.IsTerrain(board, neighbour, context.Modifications)) ||
                        (adjacentToOwnToken && IsOwnToken(context, neighbour)) ||
                        (adjacentToAnyToken && HoldsAnyToken(context, neighbour)) ||
                        (adjacentToTokenKind.Kind == EffectValueKind.TokenKind &&
                            HoldsTokenOfKind(context, neighbour, adjacentToTokenKind.TokenKindId)) ||
                        (adjacentToUnitKind != RosterUnitKind.None && HoldsQualifyingUnit(neighbour)))
                    {
                        count++;
                    }
                }

                return count;
            }

            /// <summary>
            /// Milestone 8.91. Whether a unit of the declared kind, on the declared side (any side when
            /// unbound), stands on <paramref name="at"/>. A kind the vocabulary cannot answer qualifies
            /// nothing rather than everything.
            /// </summary>
            private bool HoldsQualifyingUnit(HexCoordinate at)
            {
                if (board == null || !Enum.IsDefined(typeof(RosterUnitKind), adjacentToUnitKind) ||
                    !Enum.IsDefined(typeof(SourceTeamRelation), adjacentToUnitRelation) ||
                    !board.TryGetObjectAt(at, out var occupant))
                {
                    return false;
                }

                var kindMatches =
                    (occupant.Kind == BoardObjectKind.Hero &&
                        (adjacentToUnitKind == RosterUnitKind.Hero || adjacentToUnitKind == RosterUnitKind.Unit)) ||
                    (occupant.Kind == BoardObjectKind.Minion &&
                        (adjacentToUnitKind == RosterUnitKind.Minion || adjacentToUnitKind == RosterUnitKind.Unit));
                return kindMatches &&
                    (adjacentToUnitRelation == SourceTeamRelation.None ||
                        (context.Participants != null && TargetQuery.StandsInRelationTo(
                            adjacentToUnitRelation, occupant, context.ActorId, context.Participants,
                            board, context.Modifications)));
            }

            /// <summary>Milestone 8.91. Whether any token, of any kind and owner, stands on <paramref name="at"/>.</summary>
            public static bool HoldsAnyToken(EffectCapabilityContext context, HexCoordinate at) =>
                context.Components?.Tokens != null &&
                context.Components.Tokens.PlacedTokens.Any(token => token.Coordinate.Equals(at));

            /// <summary>
            /// Milestone 8.91. Whether a token of <paramref name="kind"/> stands on
            /// <paramref name="at"/>, as the acting hero may know it: a token hiding its kind shows its
            /// facedown face to everyone but its owner, so no card of another hero can tell a facedown
            /// piece's kind by what it selects.
            /// </summary>
            public static bool HoldsTokenOfKind(
                EffectCapabilityContext context, HexCoordinate at, TokenDefinitionId kind) =>
                context.Components?.Tokens != null &&
                context.Components.Tokens.PlacedTokens.Any(token =>
                    token.Coordinate.Equals(at) &&
                    (token.Definition.HidesItsKind && !token.OwnerHeroId.Equals(context.ActorId)
                        ? token.Definition.FacedownFace.Id
                        : token.Definition.Id).Equals(kind));

            /// <summary>Whether a token the acting hero owns stands on <paramref name="at"/>.</summary>
            public static bool IsOwnToken(EffectCapabilityContext context, HexCoordinate at) =>
                context.Components?.Tokens != null &&
                context.Components.Tokens.PlacedTokens.Any(token =>
                    token.Coordinate.Equals(at) && token.OwnerHeroId.Equals(context.ActorId));

            private static HexCoordinate? CoordinateOrNothing(EffectValue value) =>
                value.Kind == EffectValueKind.Coordinate ? value.Coordinate : (HexCoordinate?)null;
        }

        /// <summary>
        /// Whether the space one object stands on carries a Spawn point - p.19's own definition, "Spaces
        /// on the board used to spawn minions and heroes", of either type and either team.
        /// </summary>
        private static bool StandsOnASpawnPoint(IBoardDisplacementAuthority board, HexCoordinate at) =>
            board.MapDefinition.SpawnPoints.Any(point =>
                board.MapDefinition.Spaces.Any(space =>
                    space.Id.Equals(point.SpaceId) && space.Coordinate.Equals(at)));

        /// <summary>
        /// Milestone 8.57b. "which is adjacent to an enemy hero, or has a card in the discard" - one
        /// candidate measured against one printed clause, in the printed order. "Enemy" is read from
        /// the actor, as every relation in this catalog is.
        /// </summary>
        private static bool SatisfiesTheSwapQualifier(
            EffectCapabilityContext context,
            IBoardDisplacementAuthority board,
            BoardObjectReference target,
            HexCoordinate at)
        {
            if (target.Kind == BoardObjectKind.Hero && context.CardState.IsAvailable &&
                HeroDiscardPileQuery.HasAnyCard(context.CardState.Instances, target.HeroId))
            {
                return true;
            }

            if (context.Participants == null)
            {
                return false;
            }

            return RosterPresenceQuery.FindUnitsWithin(
                at,
                1,
                RosterUnitKind.Hero,
                SourceTeamRelation.OpposingSource,
                context.ActorId,
                board,
                context.Participants,
                context.Modifications).Count > 0;
        }
    }
}
