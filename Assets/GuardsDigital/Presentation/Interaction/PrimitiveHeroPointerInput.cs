using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GuardsDigital.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class PrimitiveHeroPointerInput : MonoBehaviour
    {
        [SerializeField]
        private Camera eventCamera;

        [SerializeField]
        private PrimitiveHeroPointerSelector[] selectors;

        private BoardInteractionUnityView boardView;
        private StandardMinionPointerSelector[] minionSelectors;
        private HexTileView[] tiles;
        private System.Func<Vector2, bool> blocksPointer;
        public System.Func<GuardsDigital.Core.Board.HexCoordinate, bool> SuggestHex { get; set; }

        private readonly InputAction pointerPress = new InputAction(
            "Select Hero",
            InputActionType.Button,
            "<Pointer>/press");

        public Camera EventCamera => eventCamera;

        public IReadOnlyList<PrimitiveHeroPointerSelector> Selectors => selectors;
        public IReadOnlyList<StandardMinionPointerSelector> MinionSelectors =>
            minionSelectors ?? System.Array.Empty<StandardMinionPointerSelector>();

        public void Configure(
            Camera camera,
            IEnumerable<PrimitiveHeroPointerSelector> pointerSelectors,
            IEnumerable<StandardMinionPointerSelector> pointerMinionSelectors,
            BoardInteractionUnityView passiveBoardView,
            IEnumerable<HexTileView> boardTiles,
            System.Func<Vector2, bool> pointerBlocker)
        {
            eventCamera = camera ?? throw new System.ArgumentNullException(nameof(camera));
            selectors = (pointerSelectors ?? throw new System.ArgumentNullException(nameof(pointerSelectors)))
                .Where(selector => selector != null)
                .ToArray();
            minionSelectors = (pointerMinionSelectors ??
                throw new System.ArgumentNullException(nameof(pointerMinionSelectors)))
                .Where(selector => selector != null)
                .ToArray();
            boardView = passiveBoardView ?? throw new System.ArgumentNullException(nameof(passiveBoardView));
            tiles = (boardTiles ?? throw new System.ArgumentNullException(nameof(boardTiles)))
                .Where(tile => tile != null)
                .Distinct()
                .ToArray();
            blocksPointer = pointerBlocker;
        }

        /// <summary>
        /// Milestone 9.21i. Offers the pointer a standee the board drew after composition - a
        /// many-bodied hero's second, third or fourth body.
        /// </summary>
        public void AddHeroSelectors(IEnumerable<PrimitiveHeroPointerSelector> added)
        {
            EnsureBindings();
            selectors = selectors
                .Concat((added ?? System.Array.Empty<PrimitiveHeroPointerSelector>())
                    .Where(selector => selector != null))
                .Distinct()
                .ToArray();
        }

        public void ReplaceMinionSelectors(
            IEnumerable<StandardMinionPointerSelector> pointerMinionSelectors)
        {
            minionSelectors = (pointerMinionSelectors ??
                    throw new System.ArgumentNullException(nameof(pointerMinionSelectors)))
                .Where(selector => selector != null)
                .Distinct()
                .ToArray();
        }

        private void OnEnable()
        {
            pointerPress.Enable();
        }

        private void OnDisable()
        {
            pointerPress.Disable();
        }

        private void OnDestroy()
        {
            pointerPress.Dispose();
        }

        private void Update()
        {
            RefreshHoverAtCurrentPointer();
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame && eventCamera != null)
            {
                TrySuggestAtScreen(Mouse.current.position.ReadValue());
            }
            if (!pointerPress.WasPressedThisFrame())
            {
                return;
            }

            TrySelectAtCurrentPointer();
        }

        public bool TrySuggestAtScreen(Vector2 screenPosition)
        {
            if (eventCamera == null || (blocksPointer != null && blocksPointer(screenPosition))) return false;
            var tile = FindTileUnderPointer(eventCamera.ScreenPointToRay(screenPosition));
            return tile != null && SuggestHex?.Invoke(tile.Coordinate) == true;
        }

        private void RefreshHoverAtCurrentPointer()
        {
            if (boardView == null || eventCamera == null || Pointer.current == null)
            {
                boardView?.SetHoveredCoordinate(null);
                boardView?.SetPointerHoveredHero(default);
                return;
            }

            var screenPosition = Pointer.current.position.ReadValue();
            if (blocksPointer != null && blocksPointer(screenPosition))
            {
                boardView.SetHoveredCoordinate(null);
                boardView.SetPointerHoveredHero(default);
                return;
            }

            RefreshHoverFromPointerRay(eventCamera.ScreenPointToRay(screenPosition));
        }

        /// <summary>Updates only hover strength from the board plane; it never submits an intent.</summary>
        public bool RefreshHoverFromPointerRay(Ray pointerRay)
        {
            EnsureBindings();
            if (boardView == null)
            {
                return false;
            }

            var tile = FindTileUnderPointer(pointerRay);
            boardView.SetHoveredCoordinate(tile?.Coordinate);
            var hoveredHero = default(GuardsDigital.Core.Units.Heroes.HeroId);
            var hoveredBody = 0;
            foreach (var selector in selectors ?? System.Array.Empty<PrimitiveHeroPointerSelector>())
            {
                if (selector != null &&
                    selector.TryGetHeroFromPointerRay(pointerRay, out hoveredHero, out hoveredBody))
                    break;
            }
            boardView.SetPointerHoveredHero(hoveredHero, hoveredBody);
            return tile != null;
        }

        private bool TrySelectAtCurrentPointer()
        {
            EnsureBindings();

            if (eventCamera == null || Pointer.current == null || selectors == null)
            {
                return false;
            }

            var screenPosition = Pointer.current.position.ReadValue();
            if (blocksPointer != null && blocksPointer(screenPosition))
            {
                return false;
            }

            var pointerRay = eventCamera.ScreenPointToRay(screenPosition);

            return TryHandlePointerRay(pointerRay);
        }

        /// <summary>
        /// Resolves one click against everything the ray touches, by WHAT THE CLICK CAN LEGALLY DO.
        ///
        /// <para><b>Two passes, and the split is the whole milestone (8.35z48).</b> The first pass
        /// asks every authority-backed question on the ray in turn - is this hero an answer, is this
        /// minion an answer, is this space an answer - and the first that says yes wins. Only if the
        /// entire pass declines does the second pass ask the one question that answers nothing and
        /// merely points the tray at a hero.</para>
        ///
        /// <para><b>What it was, and the defect that composed.</b> Milestone 8.11a stated the intent
        /// exactly right at the binding site: <i>"Each click is offered to the authority-backed
        /// handlers in turn and the first that recognises it wins; only a click nothing claims falls
        /// through to pointing the tray at a hero."</i> The fallback was then built INSIDE the hero
        /// arm of a first-match-wins chain, so it ran before the board was ever consulted rather than
        /// after. Because pointing the tray succeeds for any hero that is not already the viewed one,
        /// a hero's pick volume became an ABSORBING SURFACE: a ray aimed at a space but grazing a
        /// piece was consumed, the destination question was never asked, and the click still reported
        /// success. The two hit models make that easy to graze - a piece is picked by a capsule whose
        /// radius (<c>HeroTokenArtwork.TokenOuterRadius</c>, 0.81) exceeds half its height, so it is
        /// effectively a sphere standing above the board nearly as wide as a hex, while a tile is
        /// picked by intersecting the flat y=0 plane.</para>
        ///
        /// <para>It even flipped on WHICH hero was grazed, because pointing the tray returns false for
        /// the hero already being viewed: the same click at the same coordinate moved the acting hero
        /// on a staging where she was the viewed piece and did nothing on one where a different piece
        /// stood in the ray. That is the anomaly milestone 8.35z38 recorded across eight Play Mode
        /// runs and correctly could not pin on reach, offer or driver - all three are model-side, and
        /// the click never reached the model.</para>
        ///
        /// <para><b>What is deliberately NOT done: depth.</b> Comparing the capsule hit against the
        /// tile hit and preferring the nearer surface is the geometrically stronger rule, and it is
        /// not taken here. Under this project's tilted board camera the y=0 intersection of a ray
        /// through the TOP of a token lands systematically beyond the token's own tile, so a
        /// containment or nearest-surface test would reject honest clicks on a piece. Legality is
        /// both the safer and the more honest arbiter: the match is asked what the click could mean
        /// before geometry is asked what it touched.</para>
        /// </summary>
        public bool TryHandlePointerRay(Ray pointerRay)
        {
            EnsureBindings();

            foreach (var selector in selectors)
            {
                if (selector != null && selector.TrySelectFromPointerRay(pointerRay))
                {
                    return true;
                }
            }

            foreach (var selector in minionSelectors ?? System.Array.Empty<StandardMinionPointerSelector>())
            {
                if (selector != null && selector.TrySelectFromPointerRay(pointerRay))
                {
                    return true;
                }
            }

            if (boardView != null)
            {
                var tile = FindTileUnderPointer(pointerRay);
                if (tile != null && boardView.TryForwardDestinationIntent(tile.Coordinate))
                {
                    return true;
                }
            }

            // Nothing on this ray answers anything the match is asking. NOW the click may mean the
            // one thing it can always mean - show me that hero's tray - which is the feature 7.8a
            // added and this milestone keeps: a player who just wants to look at a hero still clicks
            // the hero. Pointing the tray at the hero already being viewed changes nothing visible
            // and reports false, so a click that does nothing says so instead of claiming success.
            foreach (var selector in selectors)
            {
                if (selector != null && selector.TryViewFromPointerRay(pointerRay))
                {
                    return true;
                }
            }

            return false;
        }

        private void EnsureBindings()
        {
            if (selectors == null)
            {
                selectors = System.Array.Empty<PrimitiveHeroPointerSelector>();
            }
        }

        private HexTileView FindTileUnderPointer(Ray pointerRay)
        {
            if (tiles == null || tiles.Length == 0)
            {
                return null;
            }

            var boardPlane = new Plane(Vector3.up, Vector3.zero);
            if (!boardPlane.Raycast(pointerRay, out var enter))
            {
                return null;
            }

            // Milestone 8.39j. EVERY DRAWN TILE, not the playable ones alone.
            //
            // <para><b>The defect this closes, found by driving a card rather than by reading one.</b>
            // Cutter's Grappling Bolt is the first content in the build whose offer can name a space
            // that is not playable: p.18 makes terrain an obstacle, and "target an obstacle in range
            // and in a straight line" therefore lights terrain hexes as real candidates. This search
            // filtered them out before the match was ever asked, so the ray found no tile, the
            // destination intent was never forwarded, and a lit candidate answered nothing at all -
            // an offer the input could not reach, which is the inverse of the offer-admits-more-than-
            // the-answer-can-perform class this project already tracks. Measured in Play Mode at this
            // milestone before it was changed: the click reported false with the ray passing through
            // no piece.</para>
            //
            // <para><b>Legality is the arbiter, exactly as this method's caller already documents.</b>
            // A tile that is not a legal answer is refused by
            // <c>TryForwardDestinationIntent</c> - which asks the match what the click could mean -
            // and the chain then falls through to the tray-pointing pass precisely as it did when this
            // search returned null. So the only behaviour that changes is the one that was broken:
            // a click on a non-playable space the match is actually offering now reaches it.</para>
            var hitPoint = pointerRay.GetPoint(enter);
            return tiles
                .Where(tile => tile != null)
                .OrderBy(tile => Vector3.Distance(tile.transform.position, hitPoint))
                .FirstOrDefault(tile =>
                {
                    var flatDistance = Vector2.Distance(
                        new Vector2(tile.transform.position.x, tile.transform.position.z),
                        new Vector2(hitPoint.x, hitPoint.z));
                    return flatDistance <= tile.OuterRadius;
                });
        }
    }
}
