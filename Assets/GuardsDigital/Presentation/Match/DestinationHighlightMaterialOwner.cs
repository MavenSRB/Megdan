using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class DestinationHighlightMaterialOwner : IDisposable
    {
        public const string GeneratedMaterialName = "Adjacent Destination Highlight";

        private Material material;
        private bool generated;

        public DestinationHighlightMaterialOwner(Material assignedMaterial = null)
        {
            material = assignedMaterial;
        }

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
                        name = GeneratedMaterialName,
                        color = new Color(1f, 0.78f, 0.18f, 1f)
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
