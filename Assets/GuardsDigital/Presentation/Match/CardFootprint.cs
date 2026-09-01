using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The declared sizes for a complete hero-card face.
    ///
    /// <para>Milestone 8.20c. Before it, a card's width was shared and its height was not: the tray
    /// gave every face a floor and let the row stretch all five to the tallest one's <em>content</em>,
    /// a Defense offer and a card-valued effect choice grew their face to the frame around it, and the
    /// hero rail's hover preview drew the same card at whatever its own words needed. So one printed
    /// face was four different heights depending on which surface asked for it, and a hand of five was
    /// as tall as its longest sentence - which made the card a function of the hero holding it. Two
    /// heroes are configured now and a third is next, so "how tall is a card" had to stop being
    /// answered by the content.</para>
    ///
    /// <para><b>Milestones 8.25f1-8.25f2.</b> A prepared front is already the complete printed card.
    /// It stands alone and keeps its verified 372-by-520 proportions, now at the established readable
    /// <see cref="Height"/> rather than at the old narrow <see cref="Width"/>. The width is derived from
    /// that height, so enlarging the card cannot distort it. The generated fallback takes the same
    /// outer box: resource availability changes the face, never its size.</para>
    ///
    /// <para><b>Why a stated constant rather than a measurement.</b> A height derived from the tallest
    /// verified face would be exactly the content-driven size this milestone removes: the next hero's
    /// longest sentence would silently resize every card in the game, including the two heroes already
    /// shipped. So the number is declared here, and the permanent hero player-path completion gate
    /// measures every verified face of every newly configured hero against it - a face that will not
    /// fit blocks that hero's completion rather than growing the card.</para>
    /// </summary>
    public static class CardFootprint
    {
        /// <summary>
        /// The solid black perimeter reserved outside every complete card face. It is presentation
        /// geometry rather than part of the printed content rectangle, so the 372:520 source crop and
        /// the generated fallback keep the dimensions below.
        /// </summary>
        public const float ExteriorOutlineWidth = 4f;

        /// <summary>The verified private front's native pixel width.</summary>
        public const float ArtworkSourceWidth = 372f;

        /// <summary>The verified private front's native pixel height.</summary>
        public const float ArtworkSourceHeight = 520f;

        /// <summary>
        /// Every complete card face is this tall - an exact height, not a floor.
        ///
        /// <para><b>Where the number comes from.</b> Measured, once, on the live panel at the validated
        /// 1920x1080 viewport: the thirty-eight verified faces the two configured heroes print settled
        /// between 344 and 401 pixels under the old floor-plus-stretch sizing, and the tallest of them
        /// is Wasp's Static Barrier at 401. This is that ceiling with room above it, so the sentences
        /// this build already prints fit without the card having to grow for any of them, and a
        /// slightly longer one still does. It is a ceiling and not a target: a face is drawn at this
        /// height whether its words need 344 or 401, and the rules panel takes the difference.</para>
        ///
        /// <para>It stays portrait at these proportions for milestone 8.5a's reason. The ordinary
        /// physical hand never shrinks or scrolls: it overlaps five faces inside its fixed bottom-left
        /// footprint. A mandatory multi-card decision may instead use the tray's bounded tall context
        /// and vertical scroller, still without resizing a face, so a card is the same size on every
        /// screen this project supports.</para>
        ///
        /// <para><b>Milestone 8.25c: 420 is a reference-unit height.</b> UI Toolkit lays text out in
        /// points at a stated font size, so the 344-to-401 range those thirty-eight generated faces
        /// settled into is the same whatever the panel scale. Milestone 8.25f2 retains that readable
        /// height for the real fronts instead of reducing them to fit the old 186-unit width.</para>
        /// </summary>
        public const float StandardHeight = 420f;

        /// <summary>Compatibility name for the standard height.</summary>
        public const float Height = StandardHeight;

        /// <summary>
        /// The shared complete-card width, derived from the verified front's source ratio at the
        /// established readable height.
        /// </summary>
        public const float StandardWidth =
            StandardHeight * ArtworkSourceWidth / ArtworkSourceHeight;

        /// <summary>Compatibility name for the standard width.</summary>
        public const float Width = StandardWidth;

        /// <summary>The prepared front uses the shared source-ratio width.</summary>
        public const float ArtworkWidth = StandardWidth;

        /// <summary>The prepared front uses the shared readable height.</summary>
        public const float ArtworkHeight = StandardHeight;

        /// <summary>
        /// Applies the generated fallback footprint.
        ///
        /// <para>The minimum and the maximum are set as well as the size, because a size alone is only
        /// a preference in a flex layout - a parent that stretches its children, or a row too narrow
        /// for its contents, would still resize this face, and both had happened. Pinning both ends is
        /// what makes "the card does not change size" a property of the card rather than a promise
        /// about every container it may ever be put in.</para>
        /// </summary>
        public static void Apply(VisualElement face)
        {
            ApplyPinned(face, Width, Height);
        }

        /// <summary>
        /// Applies the prepared-front footprint. Its height is derived from the verified source
        /// dimensions, so scaling changes size without changing the card's proportions.
        /// </summary>
        public static void ApplyArtwork(VisualElement face)
        {
            ApplyPinned(face, ArtworkWidth, ArtworkHeight);
        }

        private static void ApplyPinned(VisualElement face, float width, float height)
        {
            if (face == null)
            {
                return;
            }

            face.style.width = width;
            face.style.minWidth = width;
            face.style.maxWidth = width;
            face.style.height = height;
            face.style.minHeight = height;
            face.style.maxHeight = height;
            face.style.flexGrow = 0f;
            face.style.flexShrink = 0f;

            // Never stretched by the row or column it sits in. Every surface that draws a card lays it
            // out beside something - four Defense offers, six upgrade candidates, five tray cards - and
            // an `align-items: stretch` above any of them would reach past the size set here.
            face.style.alignSelf = Align.FlexStart;
        }
    }
}
