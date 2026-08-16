using System;
using UnityEngine;

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

        private static readonly Color DestinationColor = new Color(1f, 0.78f, 0.18f, 1f);
        private static readonly Color ResultingSpaceColor = new Color(0.42f, 0.04f, 0.06f, 1f);

        private readonly string generatedName;
        private readonly Color generatedColor;
        private Material material;
        private bool generated;

        public DestinationHighlightMaterialOwner(Material assignedMaterial = null)
            : this(assignedMaterial, GeneratedMaterialName, DestinationColor)
        {
        }

        private DestinationHighlightMaterialOwner(
            Material assignedMaterial,
            string generatedName,
            Color generatedColor)
        {
            material = assignedMaterial;
            this.generatedName = generatedName;
            this.generatedColor = generatedColor;
        }

        /// <summary>The owner of the resulting-space highlight, generated in its own distinct color.</summary>
        public static DestinationHighlightMaterialOwner ForResultingSpace(
            Material assignedMaterial = null) =>
            new DestinationHighlightMaterialOwner(
                assignedMaterial, GeneratedResultingSpaceMaterialName, ResultingSpaceColor);

        public Material Material
        {
            get
            {
                if (material == null)
                {
                    var shader = Shader.Find("Standard");
                    if (shader == null)
                    {
                        throw new InvalidOperationException("The destination highlight requires the Standard shader.");
                    }

                    material = new Material(shader)
                    {
                        name = generatedName,
                        color = generatedColor
                    };
                    generated = true;
                }

                return material;
            }
        }

        public bool IsGenerated => generated;

        public int OwnedNativeResourceCount => generated && material != null ? 1 : 0;

        public void Dispose()
        {
            if (!generated || material == null)
            {
                generated = false;
                material = null;
                return;
            }

            var ownedMaterial = material;
            material = null;
            generated = false;
            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(ownedMaterial);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(ownedMaterial);
            }
        }
    }
}
