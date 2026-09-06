using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Minions;

namespace GuardsDigital.Core.Rules.Modifications
{
    /// <summary>
    /// Whether Dodger the Warlock's Ultimate is rewriting what the board's spaces count as while its
    /// hero acts. Milestone 8.35z28.
    ///
    /// <para>Tide of Darkness prints "While you are performing an action, all spaces count as if they
    /// were in the battle zone and had a friendly minion spawn point", and the user's 2026-09-06
    /// rulings settle both halves of what that reaches: it applies to <em>any</em> action she performs,
    /// a Defense action included, and the Spawn point it grants satisfies the word "empty" wherever the
    /// space itself is empty - p.19, "An empty Spawn point is a Spawn point in an empty space". The
    /// card supplies the Spawn point and the board supplies the emptiness.</para>
    ///
    /// <para><b>Why it is a parameter rather than a second query.</b> Nine of her nineteen faces read
    /// one printed phrase and the Ultimate is the switch that widens it. A separate overlay-aware query
    /// beside the plain one would be two derivations of that phrase, which is the defect
    /// <see cref="EmptyMinionSpawnPointQuery"/> exists to avoid; a parameter keeps the sentence in one
    /// method and lets the switch be off by default for every caller that has no Ultimate to ask
    /// about.</para>
    ///
    /// <para><b>It carries a side because one clause needs one.</b> The counting and adjacency faces
    /// say "an empty spawn point" with no side-word, so they never read it. Necromancy and Necromastery
    /// say "an empty <em>friendly</em> spawn point", and the Ultimate's grant is explicitly a
    /// <em>friendly</em> minion Spawn point - so the side the grant is friendly to is the acting hero's,
    /// and it is recorded rather than re-derived at each consumer.</para>
    /// </summary>
    public readonly struct SpawnPointOverlay
    {
        private SpawnPointOverlay(bool isActive, TeamSide friendlySide)
        {
            IsActive = isActive;
            FriendlySide = friendlySide;
        }

        /// <summary>No overlay: the board's printed Spawn points and its real Battle Zone, and nothing else.</summary>
        public static SpawnPointOverlay None => default;

        /// <summary>
        /// Tide of Darkness, up, for a hero on <paramref name="friendlySide"/>. A side of
        /// <see cref="TeamSide.None"/> leaves the overlay off rather than granting a point friendly to
        /// nobody, which is the honest reading of a grant whose whole content is the word "friendly".
        /// </summary>
        public static SpawnPointOverlay AllSpacesFriendlyTo(TeamSide friendlySide) =>
            friendlySide == TeamSide.None
                ? None
                : new SpawnPointOverlay(true, friendlySide);

        /// <summary>
        /// Whether the hero about to be measured carries the Ultimate that grants this, and if so which
        /// side its Spawn points are friendly to. Milestone 8.35z28.
        ///
        /// <para>Every consumer of <see cref="EmptyMinionSpawnPointQuery"/> asks here rather than
        /// testing the modification set itself, for the reason the query has one derivation: nine faces
        /// read one printed phrase and the switch behind it has to be one predicate. A caller with no
        /// modification set, no participants or no defined actor gets the overlay off - the honest
        /// answer for a fixture that has no match around it, and the same one the queries themselves
        /// give when the board cannot answer.</para>
        /// </summary>
        public static SpawnPointOverlay ForActor(
            RuleModificationSet modifications,
            MatchParticipants participants,
            HeroId actorId)
        {
            if (modifications == null || participants == null || !actorId.IsDefined)
            {
                return None;
            }

            if (!modifications.AllSpacesCountAsFriendlyMinionSpawnPointsInBattleZone(
                    RuleObject.ForHero(actorId)))
            {
                return None;
            }

            var team = participants.GetTeamForHero(actorId);
            return team == null ? None : AllSpacesFriendlyTo(team.Side);
        }

        public bool IsActive { get; }

        /// <summary>The side the granted Spawn points are friendly to; meaningless while inactive.</summary>
        public TeamSide FriendlySide { get; }
    }

    /// <summary>
    /// One qualifying "empty spawn point in the battle zone", with the two facts a Respawn needs and a
    /// count does not: whose side its point belongs to, and which minion type it spawns.
    /// Milestone 8.35z28.
    ///
    /// <para><b>A point granted by the Ultimate carries no type, and that is a printed absence rather
    /// than a modelling gap.</b> Tide of Darkness says every space "had a friendly minion spawn point"
    /// and names no type; p.19's Respawn eligibility is stated per type and colour, and p.8's only
    /// other respawn places "one minion of the matching type".</para>
    ///
    /// <para><b>What the absence does NOT mean is that nothing can respawn there</b> - RULING 48
    /// (2026-09-06), which overruled the reading milestone 8.35z28 shipped here. A granted point
    /// answers the nine faces that ask whether an empty Spawn point is there, and a Respawn onto one
    /// takes its type from p.19's eligibility count rather than from the point: the page names which
    /// types may come back at all, so the question the point cannot answer is one the rule already
    /// has. Nothing is filled in with a type the card does not print - the type is read from the
    /// rulebook, and where the page admits more than one, p.10 hands the choice to the actor.</para>
    /// </summary>
    public readonly struct EmptyMinionSpawnPointFact
    {
        public EmptyMinionSpawnPointFact(
            SpawnPointId spawnPointId,
            HexCoordinate at,
            TeamSide teamSide,
            StandardMinionType minionType,
            bool isPrinted)
        {
            SpawnPointId = spawnPointId;
            At = at;
            TeamSide = teamSide;
            MinionType = minionType;
            IsPrinted = isPrinted;
        }

        /// <summary>The map's Spawn point, or the default identity for one the Ultimate granted.</summary>
        public SpawnPointId SpawnPointId { get; }

        public HexCoordinate At { get; }

        public TeamSide TeamSide { get; }

        /// <summary>
        /// The minion type this point spawns, or <see cref="StandardMinionType.None"/> for a point the
        /// Ultimate granted - see the type-level remark.
        /// </summary>
        public StandardMinionType MinionType { get; }

        /// <summary>Whether the map prints this point, as against the Ultimate granting it.</summary>
        public bool IsPrinted { get; }
    }
}
