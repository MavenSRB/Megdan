using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;

namespace GuardsDigital.Core.Rules.CardResolution
{
    /// <summary>Which effective unit class a reusable roster query names.</summary>
    public enum RosterUnitKind
    {
        None = 0,
        Hero = 1,
        Minion = 2,
        Unit = 3
    }

    /// <summary>
    /// Milestone 8.24. Which of rulebook p.22's minion classes a printed sentence admits, where the
    /// sentence names one.
    ///
    /// <para><b>Why the undeclared value admits every minion rather than none.</b> The other roster
    /// enums here spell <c>None = 0</c> and reject it, because each of them answers a question the card
    /// must have answered - which unit kind, which space, which retrieval. This one answers a
    /// narrowing, and a card that prints no narrowing has said something rather than nothing: "an enemy
    /// minion" admits all three classes. So the zero value is the absence of the adjective, and a
    /// selection that never declares this parameter behaves exactly as every minion selection built
    /// before Xargatha did.</para>
    ///
    /// <para>Xargatha's Green line is what prints it, and prints all four readings: Charm reaches "an
    /// enemy ranged minion", Control and Final Embrace "ranged or melee", Constrict "an enemy melee
    /// minion", and Dominate "an enemy minion" with the Heavy immunity expressly overridden. The
    /// classes themselves are <see cref="GuardsDigital.Core.Setup.StandardMinionType"/>'s; this names
    /// which of them one printed clause offers.</para>
    /// </summary>
    public enum RosterMinionClass
    {
        /// <summary>The sentence prints no class adjective, so every minion is admitted.</summary>
        AnyClass = 0,

        /// <summary>"a ranged minion".</summary>
        Ranged = 1,

        /// <summary>"a ranged or melee minion" - which is every class but Heavy.</summary>
        RangedOrMelee = 2,

        /// <summary>"a melee minion".</summary>
        Melee = 3
    }

    /// <summary>Which already-authoritative space one derived query is centered on.</summary>
    public enum RosterQueryOrigin
    {
        None = 0,
        Actor = 1,
        EnclosingTargetBeforeConsequences = 2
    }

    /// <summary>The two card-text retrieval shapes in the verified roster.</summary>
    public enum RosterCardRetrievalKind
    {
        None = 0,
        OwnedDiscardedCard = 1,
        OwnedResolvedOrDiscardedBasicSkill = 2
    }

    /// <summary>The derived board/history predicate that scales one printed value.</summary>
    public enum RosterValueQueryKind
    {
        None = 0,
        TargetPlayedAttackCardThisTurn = 1,
        FriendlyUnitAdjacentToTargetExcludingActor = 2,
        EnemyUnitsAdjacentToActorExcludingTarget = 3,
        EnemyUnitsAdjacentToActor = 4
    }

    /// <summary>
    /// One typed card-text contribution. It names a value channel, a reusable pure query and the
    /// amount supplied by each matching fact; it never names a card or hero.
    /// </summary>
    public readonly struct RosterValueContributionDefinition :
        IEquatable<RosterValueContributionDefinition>
    {
        public RosterValueContributionDefinition(
            PrintedValueChannel channel,
            RosterValueQueryKind query,
            int amountPerMatch)
        {
            if (channel != PrintedValueChannel.Attack && channel != PrintedValueChannel.Range)
            {
                throw new ArgumentException(
                    "Roster card-text contributions in this milestone modify Attack or Range.",
                    nameof(channel));
            }

            if (query == RosterValueQueryKind.None ||
                !Enum.IsDefined(typeof(RosterValueQueryKind), query))
            {
                throw new ArgumentException(
                    "A roster value contribution requires a supported derived query.", nameof(query));
            }

            if (amountPerMatch <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amountPerMatch), amountPerMatch,
                    "A roster card-text contribution adds a positive amount per match.");
            }

            if (channel == PrintedValueChannel.Range &&
                query != RosterValueQueryKind.EnemyUnitsAdjacentToActor)
            {
                throw new ArgumentException(
                    "Dynamic Range is derived before an Attack target exists, so it uses the " +
                    "enemy-unit count adjacent to the actor.", nameof(query));
            }

            Channel = channel;
            Query = query;
            AmountPerMatch = amountPerMatch;
        }

        public PrintedValueChannel Channel { get; }
        public RosterValueQueryKind Query { get; }
        public int AmountPerMatch { get; }

        public bool Equals(RosterValueContributionDefinition other) =>
            Channel == other.Channel && Query == other.Query && AmountPerMatch == other.AmountPerMatch;

        public override bool Equals(object obj) =>
            obj is RosterValueContributionDefinition other && Equals(other);

        public override int GetHashCode() =>
            (((int)Channel * 397) ^ (int)Query) * 397 ^ AmountPerMatch;
    }

    /// <summary>How p.14's minion Defense modifiers contribute for one Defense card.</summary>
    public enum MinionDefenseContributionPolicy
    {
        IncludeAll = 0,
        SuppressAll = 1
    }

    /// <summary>
    /// A restriction on which origin of Defense action may answer an incoming Attack. The only
    /// verified shape forbids Printed Primary Defense while preserving ordinary secondary Defense.
    /// </summary>
    public readonly struct DefenseOfferOriginRestriction :
        IEquatable<DefenseOfferOriginRestriction>
    {
        private DefenseOfferOriginRestriction(ActionDefinitionOrigin forbiddenOrigin)
        {
            ForbiddenOrigin = forbiddenOrigin;
        }

        public static DefenseOfferOriginRestriction None => default;

        public static DefenseOfferOriginRestriction PrintedPrimaryOnly { get; } =
            new DefenseOfferOriginRestriction(ActionDefinitionOrigin.PrintedPrimary);

        public ActionDefinitionOrigin ForbiddenOrigin { get; }

        public bool IsDefined => ForbiddenOrigin != ActionDefinitionOrigin.None;

        public bool Forbids(ActionDefinition action) =>
            IsDefined && action != null && action.Origin == ForbiddenOrigin;

        public bool Equals(DefenseOfferOriginRestriction other) =>
            ForbiddenOrigin == other.ForbiddenOrigin;

        public override bool Equals(object obj) =>
            obj is DefenseOfferOriginRestriction other && Equals(other);

        public override int GetHashCode() => (int)ForbiddenOrigin;
    }

    /// <summary>
    /// Shared pure calculations behind presence/count and dynamic Attack/Range contributions.
    /// Targeting immunity is intentionally absent; p.18 says presence still counts immune units.
    /// </summary>
    public static class RosterPresenceQuery
    {
        public static IReadOnlyList<BoardObjectReference> FindUnitsWithin(
            HexCoordinate origin,
            int distance,
            RosterUnitKind unitKind,
            SourceTeamRelation relation,
            HeroId sourceHero,
            IBoardDisplacementAuthority board,
            MatchParticipants participants,
            RuleModificationSet modifications,
            BoardObjectReference excludedFirst = default,
            BoardObjectReference excludedSecond = default)
        {
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));
            if (distance < 0) throw new ArgumentOutOfRangeException(nameof(distance));
            if (unitKind == RosterUnitKind.None || !Enum.IsDefined(typeof(RosterUnitKind), unitKind))
                throw new ArgumentException("A presence query requires a unit kind.", nameof(unitKind));
            if (relation == SourceTeamRelation.None ||
                !Enum.IsDefined(typeof(SourceTeamRelation), relation))
                throw new ArgumentException("A presence query requires a team relation.", nameof(relation));

            var units = board.Occupancies
                .Select(value => BoardObjectReference.ForHero(value.HeroId))
                .Concat(board.Minions.Select(value => BoardObjectReference.ForMinion(value.MinionId)))
                .Where(value => board.TryGetObjectCoordinate(value, out var at) &&
                    HexDistanceQuery.Calculate(origin, at) <= distance)
                .Where(value => AdmitsKind(value, unitKind, modifications))
                .Where(value => TargetQuery.StandsInRelationTo(
                    relation, value, sourceHero, participants, board))
                .Where(value => !excludedFirst.IsDefined || !value.Equals(excludedFirst))
                .Where(value => !excludedSecond.IsDefined || !value.Equals(excludedSecond))
                .OrderBy(value => value.Kind)
                .ThenBy(Identity, StringComparer.Ordinal)
                .ToArray();
            return Array.AsReadOnly(units);
        }

        private static bool AdmitsKind(
            BoardObjectReference value,
            RosterUnitKind kind,
            RuleModificationSet modifications)
        {
            if (kind == RosterUnitKind.Unit) return value.AsRuleObject().IsDefined;
            var effective = kind == RosterUnitKind.Hero
                ? EffectiveUnitKind.Hero
                : EffectiveUnitKind.Minion;
            return EffectiveUnitKindQuery.Is(value.AsRuleObject(), effective, modifications);
        }

        internal static string Identity(BoardObjectReference value)
        {
            switch (value.Kind)
            {
                case BoardObjectKind.Hero:
                    return value.HeroId.Value;
                case BoardObjectKind.Minion:
                    return value.MinionId.Value;
                case BoardObjectKind.Token:
                    return value.TokenInstanceId.Value;
                default:
                    return string.Empty;
            }
        }
    }

    /// <summary>Builds typed card-text contributions from trusted declarations.</summary>
    public static class RosterDynamicValueQuery
    {
        public static IReadOnlyList<ValueContribution> Contributions(
            ActionDefinition action,
            ActionOffer offer,
            AttackTarget target,
            PrintedValueChannel channel,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            EffectRosterState rosterState,
            int roundIndex,
            int turnIndex)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            if (participants == null) throw new ArgumentNullException(nameof(participants));
            if (board == null) throw new ArgumentNullException(nameof(board));
            if (modifications == null) throw new ArgumentNullException(nameof(modifications));

            var result = new List<ValueContribution>();
            foreach (var declaration in action.RosterValueContributions
                .Where(value => value.Channel == channel))
            {
                var matches = MatchCount(
                    declaration.Query, offer.ActorId, target, definitions, instances,
                    participants, board, modifications, rosterState, roundIndex, turnIndex);
                if (matches == 0) continue;
                result.Add(new ValueContribution(
                    ValueContributionKind.CardTextModifier,
                    offer.SourceCardInstanceId,
                    offer.ActionDefinitionId,
                    channel,
                    checked(matches * declaration.AmountPerMatch)));
            }

            return Array.AsReadOnly(result.ToArray());
        }

        private static int MatchCount(
            RosterValueQueryKind query,
            HeroId actorId,
            AttackTarget target,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            EffectRosterState rosterState,
            int roundIndex,
            int turnIndex)
        {
            if (!board.TryGetObjectCoordinate(BoardObjectReference.ForHero(actorId), out var actorAt))
                return 0;

            var targetObject = target.Kind == AttackTargetKind.Hero
                ? BoardObjectReference.ForHero(target.HeroId)
                : target.Kind == AttackTargetKind.Minion
                    ? BoardObjectReference.ForMinion(target.MinionId)
                    : default;
            switch (query)
            {
                case RosterValueQueryKind.TargetPlayedAttackCardThisTurn:
                    return target.Kind == AttackTargetKind.Hero && rosterState != null &&
                        rosterState.IsAvailable && roundIndex >= 1 &&
                        turnIndex >= 1 && turnIndex <= RoundResolutionAuthority.TurnsPerRound &&
                        PlayedAttackCardThisTurnQuery.HasPlayed(
                            target.HeroId, roundIndex, turnIndex,
                            rosterState.TurnSlots, definitions, instances)
                        ? 1
                        : 0;

                case RosterValueQueryKind.FriendlyUnitAdjacentToTargetExcludingActor:
                    if (!targetObject.IsDefined ||
                        !board.TryGetObjectCoordinate(targetObject, out var targetAt)) return 0;
                    return RosterPresenceQuery.FindUnitsWithin(
                        targetAt, 1, RosterUnitKind.Unit, SourceTeamRelation.AlliedWithSource,
                        actorId, board, participants, modifications,
                        BoardObjectReference.ForHero(actorId)).Count > 0 ? 1 : 0;

                case RosterValueQueryKind.EnemyUnitsAdjacentToActorExcludingTarget:
                    return RosterPresenceQuery.FindUnitsWithin(
                        actorAt, 1, RosterUnitKind.Unit, SourceTeamRelation.OpposingSource,
                        actorId, board, participants, modifications, targetObject).Count;

                case RosterValueQueryKind.EnemyUnitsAdjacentToActor:
                    return RosterPresenceQuery.FindUnitsWithin(
                        actorAt, 1, RosterUnitKind.Unit, SourceTeamRelation.OpposingSource,
                        actorId, board, participants, modifications).Count;

                default:
                    throw new ArgumentOutOfRangeException(nameof(query), query, "Unknown roster value query.");
            }
        }
    }
}
