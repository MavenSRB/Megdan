using System.Linq;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MatchSceneCompositionRoot : MonoBehaviour
    {
        [SerializeField] private HeroSelectionController controller;
        [SerializeField] private PlaceholderTestGridView mapView;
        [SerializeField] private PrimitiveHeroPointerInput pointerInput;
        [SerializeField] private Camera eventCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;
        [SerializeField] private PrimitiveHeroPointerSelector[] pointerSelectors;

        public void Compose()
        {
            controller ??= GetComponent<HeroSelectionController>();
            mapView ??= FindAnyObjectByType<PlaceholderTestGridView>();
            pointerInput ??= FindAnyObjectByType<PrimitiveHeroPointerInput>();
            eventCamera ??= Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            heroViews = FindObjectsByType<PrimitiveHeroView>();
            pointerSelectors = FindObjectsByType<PrimitiveHeroPointerSelector>();

            if (controller == null || mapView == null || eventCamera == null || heroViews.Length == 0)
            {
                throw new System.InvalidOperationException(
                    "The match scene composition root requires a controller, map, camera, and hero views.");
            }

            mapView.ConfigureSceneBindings(eventCamera, heroViews);
            controller.ConfigureSceneBindings(mapView, heroViews);
            foreach (var selector in pointerSelectors.Where(value => value != null))
            {
                selector.Configure(controller);
            }

            pointerInput?.Configure(eventCamera, pointerSelectors, controller);
        }

        private void Awake()
        {
            Compose();
        }
    }
}
