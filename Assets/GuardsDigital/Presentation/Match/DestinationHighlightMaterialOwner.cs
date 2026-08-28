using System;
using UnityEngine;
using GuardsDigital.Presentation.Board;

namespace GuardsDigital.Presentation.Match
{
    public sealed class DestinationHighlightMaterialOwner : IDisposable
    {
        public const string GeneratedMaterialName = "Adjacent Destination Highlight";

        /// <summary>
        /// Milestone 6.5c: the second board highlight. It marks where a projected Push would leave its
        /// target, which is a prediction rather than something the player may click, so it must not be
        /// the color every selectable destination has meant since milestone 1.5. Dark red reads as
        /// consequence beside the destination gold, and the two never share a space.
        /// </summary>
        public const string GeneratedResultingSpaceMaterialName = "Push Resulting Space Highlight";

        /// <summary>
        /// Milestone 8.11a, at the user's request, and settled after two attempts that were worse.
        ///
        /// <para>A highlight is now a rim and a fill rather than a block of colour, and the two carry
        /// different jobs. <b>The rim carries identity and shape</b>: it is opaque, it is the colour the
        /// highlight <i>means</i>, and it is what makes the space unmistakable. <b>The fill only makes
        /// the space look lit</b>, so it is a warm near-white at
        /// <see cref="BoardOverlayMaterial.OverlayAlpha"/> - light falling on the board rather than
        /// paint applied to it, which is what keeps a hand-painted board looking hand-painted.</para>
        ///
        /// <para>That split is what let the destination keep the gold it has meant since milestone 1.5.
        /// The first attempt kept gold as a blended fill and it failed, because most of this board is
        /// painted sand and gold-over-sand reads as slightly yellower sand. The second tried violet,
        /// which read clearly and looked alien - no part of the artwork is that colour, which was the
        /// point and also the problem. Gold in the rim solves both: the convention survives, and nothing
        /// is painted over the picture.</para>
        ///
        /// <para>Milestone 6.5c's rule survives too, and now lives in the rim. Both fills are the same
        /// warm light, because both mean "this space"; the red rim is what says a resulting space is a
        /// prediction rather than something to click.</para>
        /// </summary>
        private static readonly Color DestinationColor =
            BoardOverlayMaterial.AtOverlayAlpha(new Color(1f, 0.97f, 0.88f));

        private static readonly Color DestinationRimColor = new Color(1f, 0.80f, 0.25f);

        private static readonly Color ResultingSpaceColor =
            BoardOverlayMaterial.AtOverlayAlpha(new Color(1f, 0.94f, 0.92f));

        private static readonly Color ResultingSpaceRimColor = new Color(0.88f, 0.20f, 0.22f);

        private readonly string generatedName;
        private readonly Color generatedColor;
        private readonly Color generatedRimColor;
        private Material material;
        private Material rimMaterial;
        private bool generated;

        public DestinationHighlightMaterialOwner(Material assignedMaterial = null)
            : this(assignedMaterial, GeneratedMaterialName, DestinationColor, DestinationRimColor)
        {
        }

        private DestinationHighlightMaterialOwner(
            Material assignedMaterial,
            string generatedName,
            Color generatedColor,
            Color generatedRimColor)
        {
            material = assignedMaterial;
            this.generatedName = generatedName;
            this.generatedColor = generatedColor;
            this.generatedRimColor = generatedRimColor;
        }

        /// <summary>The owner of the resulting-space highlight, generated in its own distinct color.</summary>
        public static DestinationHighlightMaterialOwner ForResultingSpace(
            Material assignedMaterial = null) =>
            new DestinationHighlightMaterialOwner(
                assignedMaterial,
                GeneratedResultingSpaceMaterialName,
                ResultingSpaceColor,
                ResultingSpaceRimColor);

        public Material Material
        {
            get
            {
                if (material == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (shader == null)
                    {
                        throw new InvalidOperationException("The destination highlight requires the URP Lit shader.");
                    }

                    material = new Material(shader)
                    {
                        name = generatedName,
                        color = generatedColor
                    };
                    BoardOverlayMaterial.ApplyBlending(material);
                    generated = true;
                }

                return material;
            }
        }

        /// <summary>
        /// Milestone 8.11a. The opaque perimeter drawn around a highlighted space, in the same colour as
        /// the fill. It is generated beside the fill and disposed with it, so the pair cannot be owned
        /// half-way; an owner handed an authored fill generates its rim from that material's own colour,
        /// so a project that assigns its own highlight still gets a matching rim.
        /// </summary>
        public Material RimMaterial
        {
            get
            {
                if (rimMaterial == null)
                {
                    rimMaterial = BoardOverlayMaterial.CreateRim(
                        $"{generatedName} Rim",
                        generated || material == null ? generatedRimColor : material.color);
                }

                return rimMaterial;
            }
        }

        public bool IsGenerated => generated;

        public int OwnedNativeResourceCount =>
            ((generated && material != null) ? 1 : 0) + (rimMaterial != null ? 1 : 0);

        /// <remarks>
        /// Both teardown calls are written out here rather than factored into a helper, and
        /// deliberately so: `HighlightOwner_ReleasesOwnedMaterialThroughBothUnityTeardownPaths` scans
        /// this method's IL for them, because the play-mode branch is one an Edit Mode fixture can
        /// never take. Milestone 8.11a moved them into a helper and that check caught it.
        ///
        /// An assigned material belongs to whoever assigned it and is never destroyed; the rim is
        /// always generated here, so it always is.
        /// </remarks>
        public void Dispose()
        {
            var ownedRim = rimMaterial;
            var ownedMaterial = generated ? material : null;
            rimMaterial = null;
            material = null;
            generated = false;

            if (UnityEngine.Application.isPlaying)
            {
                if (ownedRim != null) UnityEngine.Object.Destroy(ownedRim);
                if (ownedMaterial != null) UnityEngine.Object.Destroy(ownedMaterial);
                return;
            }

            if (ownedRim != null) UnityEngine.Object.DestroyImmediate(ownedRim);
            if (ownedMaterial != null) UnityEngine.Object.DestroyImmediate(ownedMaterial);
        }
    }
}
