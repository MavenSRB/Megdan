using System.Collections.Generic;
using System.Linq;
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

        private readonly InputAction pointerPress = new InputAction(
            "Select Hero",
            InputActionType.Button,
            "<Pointer>/press");

        public Camera EventCamera => eventCamera;

        public IReadOnlyList<PrimitiveHeroPointerSelector> Selectors => selectors;

        public void Configure(
            Camera camera,
            IEnumerable<PrimitiveHeroPointerSelector> pointerSelectors,
            HeroSelectionController controller = null)
        {
            eventCamera = camera;
            selectors = pointerSelectors?.Where(selector => selector != null).ToArray();
            selectionController = controller ?? selectors?.Select(value => value.SelectionController)
                .FirstOrDefault(value => value != null);
        }

        private void OnEnable()
        {
            pointerPress.Enable();
        }

        private void OnDisable()
        {
            pointerPress.Disable();
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
            if (selectionController != null && selectionController.BlocksPointerAt(screenPosition))
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

            if (selectionController != null && selectionController.TryCommitDestinationFromPointerRay(pointerRay))
            {
                return true;
            }

            return false;
        }

        private void EnsureBindings()
        {
            selectors = selectors?.Where(selector => selector != null).ToArray();
            selectionController ??= selectors?.Select(value => value.SelectionController)
                .FirstOrDefault(value => value != null);
        }
    }
}
