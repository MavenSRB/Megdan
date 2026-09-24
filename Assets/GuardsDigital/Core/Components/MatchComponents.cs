using System;
using GuardsDigital.Core.Obstacles;

namespace GuardsDigital.Core.Components
{
    /// <summary>
    /// The component collaborators a capability handler is allowed to reach: the trusted catalog, the
    /// board's token placement port, and the marker register.
    ///
    /// It is a parameter object for the same reason <c>RoundResolutionComposition</c> is one - three
    /// separate arguments would have joined an already long capability-context constructor, and every
    /// future component concern would add a fourth. It holds no behaviour and makes no decisions.
    ///
    /// The two authorities are nullable and the catalog is not. A resolver fixture with no board and no
    /// match around it still has trusted content - supply sizes and return conditions are content, not
    /// runtime state - so a capability can always answer "how many exist" and must decide for itself
    /// what an absent board means. The synthetic handlers treat it as being unable to act, which is the
    /// honest rules answer rather than a silent success.
    /// </summary>
    public sealed class MatchComponents
    {
        public MatchComponents(
            ComponentDefinitionCatalog catalog,
            ITokenPlacementAuthority tokens,
            MarkerAttachmentRegister markers,
            IBoardDisplacementAuthority board,
            IUnitSpawnAuthority units,
            IBoardObjectExchangeAuthority exchange,
            ILaneAdvanceAuthority lanes = null,
            IObstaclePlacementAuthority obstacles = null)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Tokens = tokens;
            Markers = markers;
            Board = board;
            Units = units;
            Exchange = exchange;
            Lanes = lanes;
            Obstacles = obstacles;
        }

        public ComponentDefinitionCatalog Catalog { get; }

        /// <summary>The board's token port, or <c>null</c> outside a live match.</summary>
        public ITokenPlacementAuthority Tokens { get; }

        /// <summary>The marker register, or <c>null</c> outside a live match.</summary>
        public MarkerAttachmentRegister Markers { get; }

        /// <summary>
        /// The board's displacement port (milestone 6.4), or <c>null</c> outside a live match. It is a
        /// third named port rather than a widening of <see cref="Tokens"/> because relocating a board
        /// object is a different authority question from placing one from a supply, and p.18 keeps
        /// Push, Place, and Movement as separate operations.
        /// </summary>
        public IBoardDisplacementAuthority Board { get; }

        /// <summary>
        /// The board's unit Spawn port (milestone 6.5), or <c>null</c> outside a live match. It is a
        /// fourth named port for the reason the third exists: p.18's Spawn takes a miniature out of
        /// the supply, which is neither a relocation of something already standing nor a token
        /// placement, and the rulebook keeps the three operations distinct.
        /// </summary>
        public IUnitSpawnAuthority Units { get; }

        /// <summary>
        /// The board's exchange port (milestone 6.6), or <c>null</c> outside a live match. It is a
        /// fifth named port for the reason the third and fourth exist: p.19's Swap, Replace, and
        /// Remove are their own operations, and none of them can reach the displacement commit, which
        /// requires an empty destination that none of the three has.
        /// </summary>
        public IBoardObjectExchangeAuthority Exchange { get; }

        /// <summary>
        /// The board's Lane-advance port (milestone 6.6a), or <c>null</c> outside a live match. It is a
        /// sixth named port for the reason the third, fourth and fifth exist: putting a supply
        /// miniature onto a <em>named</em> Spawn point is p.19's Respawn, which is neither the Spawn
        /// that puts a miniature back where it belongs nor a relocation of something standing.
        ///
        /// <para>Milestone 8.35z28 is what made it reachable from a capability. Until Dodger the
        /// Warlock's Necromancy and Necromastery, p.8's Lane Push was the only caller and it holds the
        /// authority directly; those two faces are the first card text that respawns, and they need
        /// exactly the commit p.8 already uses. Declaring it here rather than casting <see cref="Board"/>
        /// keeps that a stated dependency - a handler that silently down-cast a port would be reaching
        /// past the parameter object this type exists to be.</para>
        ///
        /// <para>It is optional on the constructor and last in the list so the fixtures that compose a
        /// board without a Lane are untouched; a capability that needs it asks
        /// <see cref="CanAdvanceLanes"/> and reports itself unable to act, which is the honest rules
        /// answer the other ports already give.</para>
        /// </summary>
        public ILaneAdvanceAuthority Lanes { get; }

        /// <summary>The board's persistent, non-token obstacle placement port.</summary>
        public IObstaclePlacementAuthority Obstacles { get; }

        /// <summary>
        /// Whether the token and marker authorities are present, so a component operation can run.
        /// Deliberately unchanged by the board port: the token and marker capabilities that predate
        /// milestone 6.4 must not start reporting themselves unable to act because a port they never
        /// use is absent. A displacement capability asks <see cref="CanDisplace"/> instead.
        /// </summary>
        public bool IsLive => Tokens != null && Markers != null;

        /// <summary>Whether the board displacement port is present, so a displacement can be committed.</summary>
        public bool CanDisplace => Board != null;

        /// <summary>Whether the unit Spawn port is present, so a supply miniature can enter the board.</summary>
        public bool CanSpawnUnits => Units != null;

        /// <summary>
        /// Whether a Swap, Replace, or Remove can be committed. It requires the displacement port too,
        /// because all three read where their subjects currently stand through it before they mutate
        /// anything, and a capability that could commit an exchange it could not first resolve would
        /// have no pre-effect state to validate against.
        /// </summary>
        public bool CanExchangeObjects => Exchange != null && Board != null;

        /// <summary>
        /// Whether p.19's Respawn can be committed onto a named Spawn point. It requires the
        /// displacement port too, because a Respawn reads where the Battle Zone and its miniatures are
        /// through it before it places anything.
        /// </summary>
        public bool CanRespawnMinions => Lanes != null && Board != null;
    }
}
