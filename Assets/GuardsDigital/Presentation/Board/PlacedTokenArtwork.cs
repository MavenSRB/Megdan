using GuardsDigital.Core.Components;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.37. Where a placed token's printed face lives, and how a view asks for it.
    ///
    /// <para><b>Why this exists now and not before.</b> <see cref="PlacedTokenView"/> has drawn every
    /// token as one flat coloured disc under its display name since milestone 6.2d, and its own
    /// remarks said why that was right at the time: "There is no catalog lookup and no per-definition
    /// art mapping, which is what makes a Replace read correctly for a token kind this view has never
    /// seen." Until Tali the Spiritcaller no verified hero placed a token at all, so the only tokens
    /// on any board were milestone 6.2's synthetic hazard and ward. Her Green line puts Ice and two
    /// Totems on the board, and three identical discs is a board a player cannot read.</para>
    ///
    /// <para><b>The absence-tolerant contract is kept exactly.</b> This is a LOOKUP, not a
    /// requirement: the art lives under <c>Resources/Private/</c>, which Git ignores, so a clean
    /// checkout resolves nothing and every token keeps the generated disc it has always had. That is
    /// the same contract <see cref="Units.Heroes.HeroTokenArtwork"/> and the board face carry, and it
    /// is what keeps the tracked project free of the private corpus. A token kind this catalog has
    /// never seen still draws - which is the property the view's own remarks were protecting, now
    /// stated here rather than achieved by having no lookup at all.</para>
    ///
    /// <para><b>Addressed by definition id and nothing else.</b> A token's identity is its definition,
    /// so the resource name is that id verbatim. Two definitions that share a physical piece share a
    /// file rather than a code path: Tali's two Totems are one double-sided token whose totem side
    /// both wear, and <c>Tools/Prepare-TokenArtwork.ps1</c> writes that one face under both ids. The
    /// runtime therefore never has to know which definitions happen to look alike.</para>
    /// </summary>
    public static class PlacedTokenArtwork
    {
        /// <summary>
        /// The ignored resource folder <c>Tools/Prepare-TokenArtwork.ps1</c> writes into, under
        /// <c>Private/</c> for the reason the hero standees and the board face are.
        /// </summary>
        public const string ResourceRoot = "Private/Tokens";

        /// <summary>
        /// How much of the source face the disc samples. The prepared face is a hexagon on a
        /// transparent ground and the mesh is drawn with an opaque shader, so the sample stays inside
        /// the printed edge rather than reaching it - the same reason
        /// <see cref="Units.Heroes.HeroTokenArtwork.FaceInset"/> gives for a hero's own circle.
        /// </summary>
        public const float FaceInset = 0.985f;

        /// <summary>The resource this token definition's prepared face would be at.</summary>
        public static string ResourcePath(TokenDefinitionId definitionId) =>
            !definitionId.IsDefined ? null : ResourceRoot + "/" + definitionId.Value;

        /// <summary>
        /// Loads a token definition's prepared face, answering false when there is none. Absence is
        /// the tracked project's ordinary state, not an error: see the type remarks.
        /// </summary>
        public static bool TryLoadTexture(TokenDefinitionId definitionId, out Texture2D texture)
        {
            var path = ResourcePath(definitionId);
            texture = string.IsNullOrEmpty(path) ? null : Resources.Load<Texture2D>(path);
            return texture != null;
        }
    }
}
