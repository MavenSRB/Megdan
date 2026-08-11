using System;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MatchSceneCompositionRoot : MonoBehaviour
    {
        [SerializeField] private PlaceholderTestGridView mapView;
        [SerializeField] private PrimitiveHeroPointerInput pointerInput;
        [SerializeField] private Camera eventCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;
        [SerializeField] private PrimitiveHeroPointerSelector[] pointerSelectors;
        [SerializeField] private Material assignedHighlightMaterial;

        private DestinationHighlightMaterialOwner highlightOwner;
        private ConfiguredMatchHudRenderer hud;
        private ConfiguredMatchSceneRuntime runtime;

        public ConfiguredMatchSceneRuntime Runtime => runtime;
        public IMatchSession Session => runtime?.Session;
        public ConfiguredMatchHudRenderer Hud => hud;
        public bool OwnsGeneratedHighlightMaterial => highlightOwner?.IsGenerated == true;
        public int OwnedHighlightNativeResourceCount => highlightOwner?.OwnedNativeResourceCount ?? 0;

        public void Compose()
        {
            if (runtime != null)
            {
                return;
            }

            mapView ??= FindAnyObjectByType<PlaceholderTestGridView>();
            pointerInput ??= FindAnyObjectByType<PrimitiveHeroPointerInput>();
            eventCamera ??= Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            heroViews = FindObjectsByType<PrimitiveHeroView>();
            pointerSelectors = FindObjectsByType<PrimitiveHeroPointerSelector>();

            if (mapView == null || eventCamera == null || pointerInput == null || heroViews.Length == 0)
            {
                throw new InvalidOperationException(
                    "The configured match composition root requires a map, camera, pointer input, and hero views.");
            }

            if (FindAnyObjectByType<HeroSelectionController>() != null)
            {
                throw new InvalidOperationException(
                    "The configured saved scene cannot contain the legacy unconfigured HeroSelectionController drill.");
            }

            var catalog = OfficialBoardMapCatalog.Load();
            var map = catalog.Load(OfficialBoardMapCatalog.SelectedMap);
            var configuration = PlaceholderOneVsOneTestConfiguration.Create();
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            mapView.ConfigureSceneBindings(eventCamera, heroViews);
            mapView.Rebuild(map, setup);

            var tiles = mapView.GetComponentsInChildren<HexTileView>(true);
            var obstacles = mapView.GetComponentsInChildren<PlaceholderObstacleView>(true);
            highlightOwner = new DestinationHighlightMaterialOwner(assignedHighlightMaterial);
            hud = GetComponent<ConfiguredMatchHudRenderer>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<ConfiguredMatchHudRenderer>();
            }

            runtime = new ConfiguredMatchSceneRuntime(
                map,
                setup,
                configuration,
                catalog.Definitions,
                heroViews,
                tiles,
                obstacles,
                highlightOwner.Material,
                hud);

            foreach (var selector in pointerSelectors.Where(value => value != null))
            {
                selector.Configure(runtime.BoardView);
            }

            pointerInput.Configure(
                eventCamera,
                pointerSelectors,
                runtime.BoardView,
                tiles,
                hud.BlocksPointerAt);
        }

        private void Awake()
        {
            Compose();
        }

        private void OnDestroy()
        {
            runtime = null;
            highlightOwner?.Dispose();
            highlightOwner = null;
        }
    }
}
