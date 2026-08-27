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
    /// <para><b>Milestone 8.25f1.</b> A prepared front is already the complete printed card and keeps
    /// its verified 372-by-520 proportions at <see cref="Width"/> by
    /// <see cref="ArtworkHeight"/>. The taller <see cref="Height"/> remains the generated fallback's
    /// text-fit box, so an ignored private resource can be absent without making its typed face
    /// unreadable. Every consumer still receives one of these two explicitly pinned shapes from the
    /// one complete-face builder; content and interaction state never choose a third size.</para>
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
        /// Every complete card face is this wide.
        ///
        /// Milestone 8.5a's width, unchanged: five of these plus their gutters leave the tray's
        /// 1280-pixel bound comfortable, which is what p.6's five-card hand needs from a single row.
        /// </summary>
        public const float StandardWidth = 186f;

        /// <summary>Compatibility name for the standard width.</summary>
        public const float Width = StandardWidth;

        /// <summary>The verified private front's native pixel width.</summary>
        public const float ArtworkSourceWidth = 372f;

        /// <summary>The verified private front's native pixel height.</summary>
        public const float ArtworkSourceHeight = 520f;

        /// <summary>
        /// A prepared complete front at the shared width, preserving 372:520 exactly.
        /// </summary>
        public const float ArtworkHeight =
            StandardWidth * ArtworkSourceHeight / ArtworkSourceWidth;

        /// <summary>
        /// Every generated fallback face is this tall - an exact height, not a floor.
        ///
        /// <para><b>Where the number comes from.</b> Measured, once, on the live panel at the validated
        /// 1920x1080 viewport: the thirty-eight verified faces the two configured heroes print settled
        /// between 344 and 401 pixels under the old floor-plus-stretch sizing, and the tallest of them
        /// is Wasp's Static Barrier at 401. This is that ceiling with room above it, so the sentences
        /// this build already prints fit without the card having to grow for any of them, and a
        /// slightly longer one still does. It is a ceiling and not a target: a face is drawn at this
        /// height whether its words need 344 or 401, and the rules panel takes the difference.</para>
        ///
        /// <para>It stays portrait at these proportions for milestone 8.5a's reason, and one of these
        /// stands inside the tray region's own height at the validated viewport. A shorter supported
        /// viewport scrolls the region that holds the cards - the tray region has always been a
        /// <see cref="ScrollView"/> - rather than shrinking the cards inside it, so a card is the same
        /// size on every screen this project supports.</para>
        ///
        /// <para><b>Milestone 8.25c: 186 by 420 are reference units.</b> They keep the exact meaning
        /// milestone 8.20c gave them, and no verified face changes which side of the ceiling it falls
        /// on, because the measurement was in <em>panel points</em> to begin with: UI Toolkit lays text
        /// out in points at a stated font size, so the 344-to-401 range those thirty-eight faces
        /// settled into is the same range whatever the panel is scaled to. What changes is only how
        /// many device pixels a point is drawn across. "The same size on every screen" is now literally
        /// true rather than true of the pixel count. The short case the bounded scroll is for is still
        /// real and is now a matter of the window's <em>shape</em> - a panel shorter than about 913
        /// reference units cannot hold a whole face - rather than of its pixel count.</para>
        /// </summary>
        public const float StandardHeight = 420f;

        /// <summary>Compatibility name for the standard height.</summary>
        public const float Height = StandardHeight;

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
            ApplyPinned(face, Height);
        }

        /// <summary>
        /// Applies the prepared-front footprint. Its height is derived from the verified source
        /// dimensions, so scaling changes size without changing the card's proportions.
        /// </summary>
        public static void ApplyArtwork(VisualElement face)
        {
            ApplyPinned(face, ArtworkHeight);
        }

        private static void ApplyPinned(VisualElement face, float height)
        {
            if (face == null)
            {
                return;
            }

            face.style.width = Width;
            face.style.minWidth = Width;
            face.style.maxWidth = Width;
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
