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

        [SerializeField]
        private HeroSelectionController selectionController;

        private BoardInteractionUnityView boardView;
        private StandardMinionPointerSelector[] minionSelectors;
        private HexTileView[] tiles;
        private System.Func<Vector2, bool> blocksPointer;

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
            HeroSelectionController controller = null)
        {
            boardView = null;
            minionSelectors = System.Array.Empty<StandardMinionPointerSelector>();
            tiles = System.Array.Empty<HexTileView>();
            blocksPointer = null;
            eventCamera = camera;
            selectors = pointerSelectors?.Where(selector => selector != null).ToArray();
            selectionController = controller ?? selectors?.Select(value => value.SelectionController)
                .FirstOrDefault(value => value != null);
        }

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
            selectionController = null;
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
            if (!pointerPress.WasPressedThisFrame())
            {
                return;
            }

            TrySelectAtCurrentPointer();
        }

        private bool TrySelectAtCurrentPointer()
        {
            EnsureBindings();

            if (eventCamera == null || Pointer.current == null || selectors == null)
            {
                return false;
            }

            var screenPosition = Pointer.current.position.ReadValue();
            if ((blocksPointer != null && blocksPointer(screenPosition)) ||
                (selectionController != null && selectionController.BlocksPointerAt(screenPosition)))
            {
                return false;
            }

            var pointerRay = eventCamera.ScreenPointToRay(screenPosition);

            return TryHandlePointerRay(pointerRay);
        }

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
                return tile != null && boardView.TryForwardDestinationIntent(tile.Coordinate);
            }

            if (selectionController != null && selectionController.TryCommitDestinationFromPointerRay(pointerRay))
            {
                return true;
            }

            return false;
        }

        private void EnsureBindings()
        {
            if (selectors == null)
            {
                selectors = System.Array.Empty<PrimitiveHeroPointerSelector>();
            }

            if (boardView == null && selectionController == null)
            {
                for (var index = 0; index < selectors.Length; index++)
                {
                    var selector = selectors[index];
                    if (selector != null && selector.SelectionController != null)
                    {
                        selectionController = selector.SelectionController;
                        break;
                    }
                }
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

            var hitPoint = pointerRay.GetPoint(enter);
            return tiles
                .Where(tile => tile != null && tile.IsPlayable)
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
