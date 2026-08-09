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

        private readonly InputAction pointerPress = new InputAction(
            "Select Hero",
            InputActionType.Button,
            "<Pointer>/press");

        public Camera EventCamera => eventCamera;

        public IReadOnlyList<PrimitiveHeroPointerSelector> Selectors => selectors;

        public void Configure(Camera camera, IEnumerable<PrimitiveHeroPointerSelector> pointerSelectors)
        {
            eventCamera = camera;
            selectors = pointerSelectors?.Where(selector => selector != null).ToArray();
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

            var selectionController = FindFirstObjectByType<HeroSelectionController>();
            if (selectionController != null && selectionController.TryCommitDestinationFromPointerRay(pointerRay))
            {
                return true;
            }

            return false;
        }

        private void EnsureBindings()
        {
            if (eventCamera == null)
            {
                eventCamera = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            }

            if (selectors == null || selectors.Length == 0)
            {
                selectors = FindObjectsByType<PrimitiveHeroPointerSelector>(FindObjectsSortMode.None);
            }
        }
    }
}
