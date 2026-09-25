using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Obstacles;
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

    /// <summary>The card-text retrieval shapes in the verified roster.</summary>
    public enum RosterCardRetrievalKind
    {
        None = 0,
        OwnedDiscardedCard = 1,
        OwnedResolvedOrDiscardedBasicSkill = 2,

        /// <summary>
        /// Milestone 8.69b. The card whose own text is running - Brynn the Seeker's Peak Precision,
        /// "After the attack: You may retrieve <b>this card</b>", which rulebook p.14 applies at step 7
        /// while the action that played it is still resolving.
        ///
        /// <para><b>It is a third pool rather than a flag on the first two</b>, because the card it
        /// admits is in a state neither of them can reach: p.8's Retrieve Cards is about cards at rest,
        /// and this one is about a card taken out from under its own action. A Boolean widening either
        /// pool would have let a face that says "a discarded card" quietly pick up the card in play
        /// whenever a binding failed to arrive, which is the silent widening this project refuses -
        /// milestone 8.37's own reasoning for splitting <c>restrictToCard</c> out rather than defaulting
        /// it.</para>
        ///
        /// <para>The pool is exactly one card and the face names it, so the composition binds
        /// <c>restrictToCard</c> from <c>EffectRoleKind.SourceCard</c> beside this and nobody is asked
        /// which card - milestone 6.5a's reading of "yourself" applied to a card.</para>
        /// </summary>
        OwnedResolvingSourceCard = 3
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
        TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius = 5,

        /// <summary>
        /// Milestone 8.37. Whether the acting card is NOT currently standing in the
        /// discard pile - Tali's Winter Dagger, Winter Spear and Winter Scepter, "+3 Attack if this card
        /// is not in the discard".
        ///
        /// <para><b>It is the first query here that reads the card performing the action rather than
        /// the target, the actor, or the board</b>, which is what T-11's "+" cue on an Attack value
        /// turned out to need: p.14's worked reading (Attack Total = printed value + items + card text)
        /// is exactly the pipeline every other member here already answers through, so a self-referential
        /// condition is one more predicate in it rather than a new arithmetic path.</para>
        ///
        /// <para><b>Answers one or nothing, matching every predicate member here</b> - true when the
        /// source card's own location is anything but the discard pile (the ordinary case: a card played
        /// from hand is standing in the turn area while its Attack resolves), false when Commune with
        /// Spirits is performing this very card's primary FROM the discard, per RULING 22's "the card
        /// stays in the discard". The two Winter Attacks composed here are the roster's first to answer
        /// differently depending on who is performing them rather than on the board.</para>
        /// </summary>
        SourceCardNotInDiscard = 6,

        /// <summary>
        /// Milestone 8.39g2. Whether the acting hero has left the space this Attack started from -
        /// Cutter's Daring Strike, Bold Thrust and Fearless Lunge, whose first bullet reads "Before the
        /// attack: Move 1[/2/3] space[s] in a straight line. Target a hero adjacent to you in the
        /// direction of the move; <b>+2 Attack</b>."
        ///
        /// <para><b>It exists because a printed <c>+N</c> has exactly one home in this build and that
        /// home cannot be arm-conditional.</b> Every one of them is a
        /// <see cref="RosterValueContributionDefinition"/> declared on the ACTION, and these three
        /// print the bonus inside one bullet of a modal. The alternative was an Active effect created
        /// by the bullet, which needs no new vocabulary and is wrong: <see cref="RuleValueApplicability"/>
        /// has no member scoped to one action, so a turn-scoped effect would also raise a repeat of
        /// this Attack or an Attack granted later in the same turn - a rule the card does not
        /// print.</para>
        ///
        /// <para><b>The question is <c>actor-moved-this-action@1</c>'s, asked in a second place rather
        /// than re-decided.</b> Milestone 8.23 minted that capability for Tigerclaw's Combat Reflexes
        /// and answers it by comparing the Attack's recorded
        /// <c>AttackActionResolutionState.ActorSpaceAtActionStart</c> against where the hero stands
        /// now; this member is that comparison reached from the value pipeline. Two readings of "did
        /// you move" would be exactly the divergence a shared label must never hide.</para>
        ///
        /// <para><b>It is equivalent to "the moving bullet was taken" only because that bullet's Move
        /// is mandatory</b>, which is what makes the composition faithful rather than approximate: an
        /// arm that could decline its Move and still take its target would need a different fact.</para>
        ///
        /// <para>Answers one or nothing, matching every predicate member here. An Attack whose
        /// resolution recorded no action-start space - which is every Attack whose card never asks -
        /// answers nothing, on the same reading the capability takes of the same absence.</para>
        /// </summary>
        ActorMovedThisAction = 7,

        /// <summary>Every card currently in the acting hero's discard, regardless of face or played state.</summary>
        OwnedDiscardCount = 8,

        /// <summary>
        /// Milestone 8.69a. Whether the unit this Attack bound is a HERO standing adjacent to three or
        /// more of rulebook p.18's obstacles - Brynn the Seeker's High Ground, Elevated Ambush and Peak
        /// Precision, "Target a unit adjacent to you. <b>If you target a hero adjacent to 3 or more
        /// obstacles, +2 [Attack] attack.</b>"
        ///
        /// <para><b>Answers one or nothing, which is what a threshold sentence answers</b>, and the
        /// number is in <see cref="GuardsDigital.Core.Rules.Modifications.ObstacleAdjacencyQuery"/>'s
        /// name rather than beside the declaration - the shape
        /// <see cref="TwoOrMoreEmptyMinionSpawnPointsInBattleZoneInPrintedRadius"/> records at length.
        /// The +2 beside it is the whole bonus whether three neighbouring spaces qualify or six.</para>
        ///
        /// <para><b>A minion target answers nothing, by the sentence rather than by convenience.</b>
        /// The clause prints "a hero", and all three faces reach p.13's own "Target a unit adjacent to
        /// you" - so a minion IS a legal target of the Attack and simply never raises it. That is the
        /// same division <c>AttackSequenceSubjectNarrowing</c> draws for a bullet that narrows by a
        /// hero fact.</para>
        ///
        /// <para><b>It is read at the Attack total, which is what makes it Tigerclaw's Backstab
        /// shape</b>: p.14 composes step 5 after step 4's Defense window, so a Defense text that pushes
        /// or swaps the defender between the two changes the answer. The predicate itself is asked of
        /// the one owner of the printed phrase, so it reaches p.17's counts-as overlay and his Ultimate
        /// widens this +2 without either card naming the other.</para>
        /// </summary>
        AttackTargetHeroAdjacentToThreeOrMoreObstacles = 9,

        /// <summary>
        /// One when the bound Attack target is in straight lines independently from the actor and
        /// the actor's singular obstacle of the trusted kind. Trinkets' three cannon Reds print the
        /// same condition with different +Attack amounts.
        /// </summary>
        AttackTargetInStraightLineFromActorAndOwnedObstacle = 10,

        /// <summary>
        /// A named marker is below the current numbered turn slot. The query reads the live
        /// physical-marker register; it does not infer activation from a card's zone.
        /// </summary>
        ActiveTurnSlotMarker = 11
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
            int amountPerMatch,
            string obstacleKind = null,
            MarkerDefinitionReference markerDefinition = default)
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

            if (query == RosterValueQueryKind.AttackTargetInStraightLineFromActorAndOwnedObstacle
                ? string.IsNullOrWhiteSpace(obstacleKind)
                : obstacleKind != null)
            {
                throw new ArgumentException(
                    "Only a dual-line obstacle contribution names a trusted obstacle kind.",
                    nameof(obstacleKind));
            }

            if (query == RosterValueQueryKind.AttackTargetInStraightLineFromActorAndOwnedObstacle &&
                channel != PrintedValueChannel.Attack)
                throw new ArgumentException(
                    "A dual-line obstacle contribution modifies Attack only.", nameof(channel));

            if ((query == RosterValueQueryKind.ActiveTurnSlotMarker) != markerDefinition.IsDefined ||
                (markerDefinition.IsDefined && channel != PrintedValueChannel.Attack))
                throw new ArgumentException(
                    "Only an active-turn-slot-marker Attack contribution names a marker definition.",
                    nameof(markerDefinition));

            Channel = channel;
            Query = query;
            AmountPerMatch = amountPerMatch;
            ObstacleKind = obstacleKind;
            MarkerDefinition = markerDefinition;
        }

        public PrintedValueChannel Channel { get; }
        public RosterValueQueryKind Query { get; }
        public int AmountPerMatch { get; }
        public string ObstacleKind { get; }
        public MarkerDefinitionReference MarkerDefinition { get; }

        public bool Equals(RosterValueContributionDefinition other) =>
            Channel == other.Channel && Query == other.Query && AmountPerMatch == other.AmountPerMatch &&
            string.Equals(ObstacleKind, other.ObstacleKind, StringComparison.Ordinal) &&
            MarkerDefinition.Equals(other.MarkerDefinition);

        public override bool Equals(object obj) =>
            obj is RosterValueContributionDefinition other && Equals(other);

        public override int GetHashCode()
        {
            var oldHash = (((int)Channel * 397) ^ (int)Query) * 397 ^ AmountPerMatch;
            var obstacleHash = ObstacleKind == null
                ? oldHash
                : oldHash * 397 ^ StringComparer.Ordinal.GetHashCode(ObstacleKind);
            return MarkerDefinition.IsDefined
                ? obstacleHash * 397 ^ MarkerDefinition.GetHashCode()
                : obstacleHash;
        }
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
            int turnIndex,
            // Milestone 8.39g2. The space the enclosing Attack recorded when it started, which is the
            // one fact `ActorMovedThisAction` needs and the only one this method was never handed.
            // Optional, so the Range call site and every caller written before that milestone keep
            // their exact contributions - and absent it, that query answers nothing rather than
            // guessing from a current position with nothing to compare against.
            HexCoordinate? actorSpaceAtActionStart = null,

            // Milestone 8.69a. p.18's obstacle classification port, which
            // `AttackTargetHeroAdjacentToThreeOrMoreObstacles` needs and which
            // `IBoardDisplacementAuthority` deliberately does not expose - see ObstacleQuery's own
            // overload for why the question goes to the port rather than the port's answer being
            // re-derived here. Optional, so every caller written before this milestone composes its
            // exact contributions; a declaration that NAMES the query and is handed none is refused
            // loudly rather than answered with a silent zero.
            ITokenPlacementAuthority spaces = null,
            IObstaclePlacementAuthority obstacles = null,
            MarkerAttachmentRegister markers = null)
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
                    declaration, offer.ActorId, offer.SourceCardInstanceId, target, definitions,
                    instances, participants, board, modifications, rosterState, roundIndex, turnIndex,
                    actorSpaceAtActionStart, spaces, obstacles, markers);
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
                    declaration.Query, defenseOffer, definitions, board, heroItems, modifications,
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
            RuleModificationSet modifications,
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

            var radius = RadiusValueQuery.Calculate(
                defenseOffer, definitions, heroItems, modifications ?? RuleModificationSet.Empty);
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
            RosterValueContributionDefinition declaration,
            HeroId actorId,
            CardInstanceId sourceCardInstanceId,
            AttackTarget target,
            IReadOnlyList<CardDefinition> definitions,
            IReadOnlyList<CardInstance> instances,
            MatchParticipants participants,
            IBoardDisplacementAuthority board,
            RuleModificationSet modifications,
            EffectRosterState rosterState,
            int roundIndex,
            int turnIndex,
            HexCoordinate? actorSpaceAtActionStart,
            ITokenPlacementAuthority spaces,
            IObstaclePlacementAuthority obstacles,
            MarkerAttachmentRegister markers)
        {
            var query = declaration.Query;
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

                case RosterValueQueryKind.SourceCardNotInDiscard:
                {
                    if (!sourceCardInstanceId.IsDefined)
                    {
                        return 0;
                    }

                    var sourceCard = instances.FirstOrDefault(
                        instance => instance != null && instance.Id.Equals(sourceCardInstanceId));
                    return sourceCard != null && sourceCard.State.Location != CardLocation.DiscardPile
                        ? 1
                        : 0;
                }

                case RosterValueQueryKind.OwnedDiscardCount:
                    return instances.Count(instance => instance != null &&
                        instance.OwnerId.Equals(actorId) &&
                        instance.State.Location == CardLocation.DiscardPile);

                // Milestone 8.39g2. The same comparison `actor-moved-this-action@1` makes, reached from
                // the value pipeline: the space the Attack recorded at its start against the space the
                // hero occupies now. An Attack that recorded none never asked, and answers nothing.
                case RosterValueQueryKind.ActorMovedThisAction:
                    return actorSpaceAtActionStart.HasValue &&
                        !actorAt.Equals(actorSpaceAtActionStart.Value)
                        ? 1
                        : 0;

                // Milestone 8.69a. "If you target a hero adjacent to 3 or more obstacles, +2 Attack" -
                // asked of the one owner of that printed phrase, so this and the attack-target bullet
                // that narrows by it cannot disagree, and so his Ultimate's counts-as overlay reaches
                // the Attack total. A minion target answers nothing: the clause prints "a hero".
                case RosterValueQueryKind.AttackTargetHeroAdjacentToThreeOrMoreObstacles:
                    if (spaces == null)
                    {
                        throw new InvalidOperationException(
                            "A card-text contribution declaring " +
                            nameof(RosterValueQueryKind
                                .AttackTargetHeroAdjacentToThreeOrMoreObstacles) +
                            " reads rulebook p.18's obstacle classification around the bound target, " +
                            "and this composition was handed no board port to ask it with. Supply " +
                            "the match's board at the call site rather than letting the clause " +
                            "silently contribute nothing.");
                    }

                    return target.Kind == AttackTargetKind.Hero &&
                        ObstacleAdjacencyQuery.HeroIsAdjacentToThreeOrMoreObstacles(
                            spaces, target.HeroId, modifications)
                        ? 1
                        : 0;

                case RosterValueQueryKind.AttackTargetInStraightLineFromActorAndOwnedObstacle:
                    if (obstacles == null)
                        throw new InvalidOperationException(
                            "A dual-line Attack contribution requires the board's obstacle port.");
                    return targetObject.IsDefined &&
                        board.TryGetObjectCoordinate(targetObject, out var dualLineTargetAt) &&
                        obstacles.TryGetObstacleCoordinate(
                            OwnedObstacleIdentity.For(declaration.ObstacleKind, actorId),
                            out var obstacleAt) &&
                        StraightLineQuery.AreInStraightLine(actorAt, dualLineTargetAt) &&
                        StraightLineQuery.AreInStraightLine(obstacleAt, dualLineTargetAt)
                        ? 1
                        : 0;

                case RosterValueQueryKind.ActiveTurnSlotMarker:
                    if (markers == null)
                        throw new InvalidOperationException(
                            "An active-turn-slot-marker Attack contribution requires the live marker register.");
                    return turnIndex >= 1 && turnIndex <= RoundResolutionAuthority.TurnsPerRound &&
                        markers.Attachments.Any(marker =>
                            marker.TurnSlotIndex == turnIndex &&
                            marker.Definition.Reference.Equals(declaration.MarkerDefinition))
                        ? 1
                        : 0;

                default:
                    throw new ArgumentOutOfRangeException(nameof(query), query, "Unknown roster value query.");
            }
        }
    }
}
