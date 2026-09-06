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
        EnemyUnitsAdjacentToActor = 4,

        /// <summary>
        /// Milestone 8.35z28. Whether two or more empty minion Spawn points of the current Battle Zone
        /// stand inside the acting card's own printed Radius disk - Dodger's Shield of Decay, Vampiric
        /// Shield and Aegis of Doom, "+N [Defense] Defense if there are 2 or more empty spawn points in
        /// radius in the battle zone".
        ///
        /// <para><b>It is a predicate and not a count, which is why the number is in its name.</b> The
        /// sentence is a threshold - the bonus is +2, +2 and +4 whether two points qualify or five -
        /// and this enum has carried exactly that shape since milestone 8.19b:
        /// <see cref="TargetPlayedAttackCardThisTurn"/> answers one or nothing for Sabina's "+3 Attack
        /// if the target played an attack card this turn". So this member answers one or nothing too,
        /// and the printed amount beside it is the whole bonus. Every verified face that names it prints
        /// "2 or more"; a face printing a different number would be a different member here, by the same
        /// rule that made the two adjacency counts two members.</para>
        ///
        /// <para>The quantity behind the predicate is
        /// <see cref="GuardsDigital.Core.Rules.Modifications.EmptyMinionSpawnPointQuery"/>'s, asked
        /// rather than answered a second way, because eight other faces of hers read the identical
        /// printed phrase through the effect frame instead of through the value pipeline. The disk is
        /// p.9's printed Radius with p.16's matching item composed onto it, measured from the defender's
        /// own space - which is what printed p.14's worked example means by "within a radius of 2 spaces
        /// from her space".</para>
        ///
        /// <para>It is the first query here that reads the board's <em>map</em> rather than its
        /// occupants, and the first whose channel is Defense.</para>
        /// </summary>
        TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius = 5
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
            if (channel != PrintedValueChannel.Attack &&
                channel != PrintedValueChannel.Range &&
                channel != PrintedValueChannel.Defense)
            {
                throw new ArgumentException(
                    "Roster card-text contributions modify Attack, Range or Defense.",
                    nameof(channel));
            }

            // Milestone 8.35z28. p.14 illustrates the printed `+` on a Defense value using Dodger's
            // Vampiric Shield and states the notation outright - "Primary Defense Value 4 (The '+' sign
            // signifies that the value can be increased by the card text.)" - so the Defense channel
            // joins the two the Attack Total already had. The pairing runs both ways, because the one
            // verified sentence that raises a Defense and the one query that answers it are the same
            // sentence: nothing else may claim the channel, and this query answers about nothing else.
            if ((channel == PrintedValueChannel.Defense) !=
                (query == RosterValueQueryKind
                    .TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius))
            {
                throw new ArgumentException(
                    "The only verified Defense contribution is Dodger's empty-minion-spawn-point " +
                    "threshold, and that threshold raises nothing else.",
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

        /// <summary>
        /// Milestone 8.35z28. The Defense channel's contributions, which are asked with different
        /// collaborators than the Attack and Range ones above and therefore have their own entry point.
        ///
        /// <para><b>Why a second method rather than a widened first.</b> The Attack and Range arms are
        /// asked inside an Attack the actor is performing, so they take the bound target, this turn's
        /// history and the played-card state. A Defense is a reaction, has no target of its own, and
        /// what its declaration reads instead is the map and the defender's own printed Radius - which
        /// needs p.16's items and the board's emptiness port, neither of which the Attack arm holds.
        /// Passing five arguments nothing uses to reach two nothing else has is not one method.</para>
        ///
        /// <para>The declaration type is the same one and the arithmetic is the same multiplication,
        /// because the Defense query is a predicate answering one or nothing - the shape
        /// <c>TargetPlayedAttackCardThisTurn</c> has had since milestone 8.19b. So a threshold sentence
        /// needs no second arithmetic: "2 or more" is inside the query, and the printed amount beside it
        /// is the whole bonus.</para>
        ///
        /// <para><b>Card text applies only where p.9 says it does.</b> The action's own policy is the
        /// discriminator, which is printed p.14's worked example on this very hero: Dodger defends with
        /// Vampiric Shield and "since Defense is the Primary action of 'Vampiric Shield', the card text
        /// is applied", contrasted on the same page against Wasp's Lift Up, whose Defense is secondary
        /// and "its card text is ignored".</para>
        /// </summary>
        public static IReadOnlyList<ValueContribution> DefenseContributions(
            ActionDefinition action,
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            MatchParticipants participants,
            // Required rather than defaulted, by milestone 6.8f's rule: this seam's outcome can be
            // changed by a modification - Dodger's Tide of Darkness widens the count it measures - so a
            // caller that forgot the live set would silently resolve against none. Passing nothing is
            // spelled `RuleModificationSet.Empty`, which is a decision rather than an omission.
            RuleModificationSet modifications)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (board == null) throw new ArgumentNullException(nameof(board));

            var declarations = action.RosterValueContributions
                .Where(value => value.Channel == PrintedValueChannel.Defense)
                .ToArray();
            if (declarations.Length == 0 ||
                action.DefaultPolicy.CardTextApplication != CardTextApplicationPolicy.Applies)
            {
                return Array.Empty<ValueContribution>();
            }

            var result = new List<ValueContribution>();
            foreach (var declaration in declarations)
            {
                var matches = DefenseMatchCount(
                    declaration.Query, defenseOffer, definitions, board, heroItems,
                    // The defender is the one performing the Defense action, so the overlay is read
                    // from her. That is the user's 2026-09-06 D-02 ruling in composed form: p.14 says
                    // "perform a Defense action", Tide of Darkness says "an action" and narrows
                    // nothing, so the Ultimate is up while she defends. Milestone 8.35z28.
                    SpawnPointOverlay.ForActor(
                        modifications, participants, defenseOffer.ActorId));
                if (matches == 0) continue;
                result.Add(new ValueContribution(
                    ValueContributionKind.CardTextModifier,
                    defenseOffer.SourceCardInstanceId,
                    defenseOffer.ActionDefinitionId,
                    PrintedValueChannel.Defense,
                    checked(matches * declaration.AmountPerMatch)));
            }

            return Array.AsReadOnly(result.ToArray());
        }

        /// <summary>
        /// One or nothing, which is what a threshold sentence answers. The two the printed clause asks
        /// for is written here rather than on the declaration, because the query kind is what names it -
        /// see <c>TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius</c>.
        /// </summary>
        private const int PrintedSpawnPointThreshold = 2;

        private static int DefenseMatchCount(
            RosterValueQueryKind query,
            ActionOffer defenseOffer,
            IReadOnlyList<CardDefinition> definitions,
            GuardsDigital.Core.Rules.AdjacentMove.AdjacentMoveAuthority board,
            GuardsDigital.Core.Rules.Progression.HeroItemState heroItems,
            SpawnPointOverlay overlay)
        {
            if (query != RosterValueQueryKind
                    .TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(query), query, "Unknown Defense-channel roster value query.");
            }

            if (!board.TryGetObjectCoordinate(
                    BoardObjectReference.ForHero(defenseOffer.ActorId), out var defenderAt))
            {
                // A defender who is not standing anywhere measures no disk. It cannot happen for a hero
                // answering an Attack and is stated rather than assumed, because the same derivation is
                // re-run by the resolution's own checks.
                return 0;
            }

            var radius = RadiusValueQuery.Calculate(defenseOffer, definitions, heroItems);
            if (radius.Total <= 0)
            {
                // p.16 raises a printed icon and never invents one, so a card that prints no Radius has
                // no disk at all - which is the same reading the printed-icon selections take.
                return 0;
            }

            return EmptyMinionSpawnPointQuery.CountWithin(
                board, board, defenderAt, radius.Total, overlay) >= PrintedSpawnPointThreshold
                ? 1
                : 0;
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
