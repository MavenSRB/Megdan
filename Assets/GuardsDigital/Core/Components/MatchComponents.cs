using System;

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
            IBoardDisplacementAuthority board)
        {
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Tokens = tokens;
            Markers = markers;
            Board = board;
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
        /// Whether the token and marker authorities are present, so a component operation can run.
        /// Deliberately unchanged by the board port: the token and marker capabilities that predate
        /// milestone 6.4 must not start reporting themselves unable to act because a port they never
        /// use is absent. A displacement capability asks <see cref="CanDisplace"/> instead.
        /// </summary>
        public bool IsLive => Tokens != null && Markers != null;

        /// <summary>Whether the board displacement port is present, so a displacement can be committed.</summary>
        public bool CanDisplace => Board != null;
    }
}
