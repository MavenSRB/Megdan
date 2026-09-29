using System.Collections.Generic;
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
        // Presentation metadata only: the prepared face is sufficient to identify this piece.
        private static readonly IReadOnlyDictionary<TokenDefinitionId, string> PreparedCaptions =
            new Dictionary<TokenDefinitionId, string>
            {
                [new TokenDefinitionId("widget-pyro")] = string.Empty,

                // Milestone 8.61j. Min the Dragonmonk's five faces, on Pyro's terms: each prepared
                // face says what the piece is without a word over it - a blue smoke cloud, a fiery
                // skull-bomb, a skull-and-crossbones danger sign, a green landmine and a bare patch
                // of grass.
                //
                // THE FACEDOWN ONE MATTERS MOST AND IS THE LEAST INTERESTING TO LOOK AT. Every seat
                // but hers is handed `min-mine-facedown` by `FacedownTokenMask` in place of the real
                // definition, so the caption drawn over a mine on another player's screen is read
                // off the MASK and can never be the kind. The caption this entry removes would have
                // been "MINE", which leaks nothing either - the empty string is about the board
                // being readable rather than about the secret.
                [new TokenDefinitionId("min-smoke-bomb-token")] = string.Empty,
                [new TokenDefinitionId("min-grenade")] = string.Empty,
                [new TokenDefinitionId("min-mine-facedown")] = string.Empty,
                [new TokenDefinitionId("min-mine-blast")] = string.Empty,
                [new TokenDefinitionId("min-mine-dud")] = string.Empty,

                // Milestone 8.75b. Ignatia the Mad's magma, on Min's terms exactly: the prepared face
                // is a pool of molten rock on a hex, which says what the piece is without a word over
                // it. The caption this entry removes would have been "MAGMA", which is legible and
                // redundant - and redundant matters here, because her three Paths can leave three of
                // them on one file and three stacked captions is a board a player reads slower than
                // the art.
                [new TokenDefinitionId("ignatia-magma")] = string.Empty,
                [new TokenDefinitionId("trinkets-barrier")] = string.Empty,

                // Milestone 8.112g. Mrak's rock: the prepared hex prints the rock itself, so no caption.
                [new TokenDefinitionId("mrak-rock")] = string.Empty,

                // Milestone 8.114f. Wuk's Tree: the prepared hex prints the tree itself, so no caption.
                [new TokenDefinitionId("wuk-tree")] = string.Empty,

                // Milestone 9.8f. Mortimer's Zombie: the prepared hex prints the Zombie, so no caption.
                [new TokenDefinitionId("mortimer-zombie")] = string.Empty
            };

        /// <summary>Named fallback discs remain readable when private artwork is absent.</summary>
        public static string Caption(TokenDefinition definition, bool hasPreparedFace) =>
            hasPreparedFace && PreparedCaptions.TryGetValue(definition.Id, out var caption)
                ? caption
                : definition.DisplayName.ToUpperInvariant();

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
