using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed class HeroSelectionController : MonoBehaviour
    {
        [SerializeField]
        private PrimitiveHeroView heroView;

        [SerializeField]
        private HexTileView[] tileViews;

        [SerializeField]
        private Material highlightMaterial;

        private readonly List<HexCoordinate> highlightedCoordinates = new List<HexCoordinate>();

        public bool IsHeroSelected { get; private set; }

        public IReadOnlyList<HexCoordinate> HighlightedCoordinates => highlightedCoordinates;

        public Material HighlightMaterial => EnsureHighlightMaterial();

        public void Configure(PrimitiveHeroView hero, IEnumerable<HexTileView> tiles, Material material)
        {
            heroView = hero;
            tileViews = tiles?.Where(tile => tile != null).ToArray();
            highlightMaterial = material;
            ClearSelection();
        }

        public void SelectHero()
        {
            EnsureBindings();
            ClearTileHighlights();
            highlightedCoordinates.Clear();

            if (heroView == null || tileViews == null)
            {
                IsHeroSelected = false;
                return;
            }

            var availableTiles = tileViews.Where(tile => tile != null).ToArray();
            var boardCoordinates = availableTiles.Select(tile => tile.Coordinate);
            var occupancies = new[] { heroView.Occupancy };
            var destinations = AdjacentDestinationQuery.FindEmptyAdjacentDestinations(
                heroView.Occupancy,
                boardCoordinates,
                occupancies);
            var destinationSet = new HashSet<HexCoordinate>(destinations);

            foreach (var coordinate in destinations)
            {
                highlightedCoordinates.Add(coordinate);
            }

            var material = EnsureHighlightMaterial();
            foreach (var tile in availableTiles)
            {
                tile.SetHighlighted(destinationSet.Contains(tile.Coordinate), material);
            }

            IsHeroSelected = true;
        }

        public void ClearSelection()
        {
            ClearTileHighlights();
            highlightedCoordinates.Clear();
            IsHeroSelected = false;
        }

        private void ClearTileHighlights()
        {
            if (tileViews == null)
            {
                return;
            }

            foreach (var tile in tileViews)
            {
                if (tile != null)
                {
                    tile.SetHighlighted(false, null);
                }
            }
        }

        private void EnsureBindings()
        {
            if (heroView == null)
            {
                heroView = FindFirstObjectByType<PrimitiveHeroView>();
            }

            if (tileViews == null || tileViews.Length == 0)
            {
                tileViews = FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            }
        }

        private Material EnsureHighlightMaterial()
        {
            if (highlightMaterial != null)
            {
                return highlightMaterial;
            }

            var shader = Shader.Find("Standard");
            highlightMaterial = new Material(shader)
            {
                name = "Adjacent Destination Highlight",
                color = new Color(1f, 0.78f, 0.18f, 1f)
            };
            return highlightMaterial;
        }

        private void Awake()
        {
            EnsureBindings();
        }
    }
}
