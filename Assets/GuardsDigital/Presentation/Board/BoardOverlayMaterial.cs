using System;
using UnityEngine;

namespace GuardsDigital.Presentation.Board
{
    /// <summary>
    /// Milestone 8.11a. The one place that turns a Standard material into a blended one, and the one
    /// statement of how solid a board overlay is allowed to be.
    ///
    /// <para>Every cue this project draws on the board plane used to be opaque, which was harmless while
    /// the board underneath was flat generated colour. With the official face underneath it is not
    /// harmless: an opaque highlight covers the artwork exactly where a player is being asked to look,
    /// so the space they are choosing is the one space they cannot see. Overlays are drawn blended
    /// instead, at <see cref="OverlayAlpha"/>.</para>
    ///
    /// <para>The Standard shader is opaque until its blend state is set, so a colour with a low alpha is
    /// not enough on its own - that is the trap this type exists to stop each caller repeating.</para>
    /// </summary>
    public static class BoardOverlayMaterial
    {
        /// <summary>
        /// How solid a board overlay is drawn. Low, and deliberately so: the rim carries the shape, so
        /// the fill only has to make the space look lit rather than painted. The board's own artwork,
        /// including whatever icon it prints on that space, reads straight through it.
        /// </summary>
        public const float OverlayAlpha = 0.28f;

        /// <summary>A fully transparent fill: present, pickable, and invisible.</summary>
        public const float SuppressedAlpha = 0f;

        /// <summary>
        /// Generates a blended Standard material in the given colour, taking the colour's own alpha.
        /// </summary>
        public static Material Create(string name, Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"The generated board overlay material '{name}' requires the Standard shader.");
            }

            var material = new Material(shader) { name = name, color = color };
            material.hideFlags = HideFlags.DontSave;
            ApplyBlending(material);
            return material;
        }

        /// <summary>
        /// Puts an existing Standard material into alpha-blended mode. Separate from
        /// <see cref="Create"/> because a caller may be handed an authored material to blend rather than
        /// generating its own.
        /// </summary>
        public static void ApplyBlending(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        /// <summary>The same colour at the overlay's own alpha.</summary>
        public static Color AtOverlayAlpha(Color color) =>
            new Color(color.r, color.g, color.b, OverlayAlpha);

        /// <summary>
        /// Generates the opaque rim material that draws a highlighted space's perimeter.
        ///
        /// <para>Milestone 8.11a, at the user's request. A blended fill alone is weak over painted sand,
        /// which is most of this board: the tint reads as slightly different sand rather than as a
        /// chosen space. The rim carries the shape at full strength while the fill stays see-through, so
        /// the space is unmistakable and the artwork under it is still legible.</para>
        ///
        /// <para>It is a Standard material, like every other generated one here, rather than an unlit
        /// shader that would read at exactly its colour. `RuntimeShaderResolutionTests` requires every
        /// shader this project resolves by name to be listed in Graphics settings' always-included
        /// shaders, so reaching for `Unlit/Color` would mean adding it there - a build-settings change
        /// for a line that is already legible. It is emissive instead, which is what keeps the rim at
        /// full strength wherever the board's lighting happens to fall.</para>
        /// </summary>
        public static Material CreateRim(string name, Color color)
        {
            var opaque = new Color(color.r, color.g, color.b, 1f);
            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                throw new InvalidOperationException(
                    $"The generated board rim material '{name}' requires the Standard shader.");
            }

            // Purely emissive, for the reason the board underlay is: a lit albedo on top of the
            // emission would add the scene's lighting to a line whose whole job is to be exactly one
            // colour. Black albedo, colour in the emission.
            var material = new Material(shader) { name = name, color = Color.black };
            material.hideFlags = HideFlags.DontSave;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", opaque);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            SuppressEnvironmentResponse(material);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 1;
            return material;
        }

        /// <summary>
        /// Stops a Standard material responding to the scene's environment at all, so that what it emits
        /// is the whole of what it shows.
        ///
        /// <para>Milestone 8.11a, and it is the second half of a fix whose first half was not enough.
        /// Blacking the albedo kills the diffuse term, but the Standard shader still takes a broad
        /// specular and reflection term from the environment - and this scene's ambient is a flat
        /// light blue-grey at full intensity, which laid a wash over the whole board and left the
        /// artwork looking bleached. Smoothness zero narrows that term; switching the two paths off
        /// removes it.</para>
        /// </summary>
        public static void SuppressEnvironmentResponse(Material material)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_GlossyReflections", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        }
    }
}
