using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Rules.AdjacentMove;
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

        private AdjacentMoveAuthority moveAuthority;

        public bool IsHeroSelected { get; private set; }

        public IReadOnlyList<HexCoordinate> HighlightedCoordinates => highlightedCoordinates;

        public Material HighlightMaterial => EnsureHighlightMaterial();

        public AdjacentMoveRejectionReason LastMoveRejectionReason { get; private set; }

        public HeroOccupancy CurrentHeroOccupancy
        {
            get
            {
                EnsureBindings();
                EnsureMoveAuthority();
                return GetHeroOccupancyFromAuthority();
            }
        }

        public void Configure(PrimitiveHeroView hero, IEnumerable<HexTileView> tiles, Material material)
        {
            heroView = hero;
            tileViews = tiles?.Where(tile => tile != null).ToArray();
            highlightMaterial = material;
            RebuildMoveAuthority();
            ClearSelection();
        }

        public void SelectHero()
        {
            EnsureBindings();
            EnsureMoveAuthority();
            ClearTileHighlights();
            highlightedCoordinates.Clear();

            if (heroView == null || tileViews == null)
            {
                IsHeroSelected = false;
                return;
            }

            var availableTiles = tileViews.Where(tile => tile != null).ToArray();
            var boardCoordinates = availableTiles.Select(tile => tile.Coordinate);
            var selectedOccupancy = GetHeroOccupancyFromAuthority();
            var destinations = AdjacentDestinationQuery.FindEmptyAdjacentDestinations(
                selectedOccupancy,
                boardCoordinates,
                moveAuthority.Occupancies);
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

        public bool TryCommitDestination(HexCoordinate destination)
        {
            EnsureBindings();
            EnsureMoveAuthority();

            if (!IsHeroSelected || heroView == null || moveAuthority == null)
            {
                return false;
            }

            var currentOccupancy = GetHeroOccupancyFromAuthority();
            var result = moveAuthority.TryCommit(new AdjacentMoveCommand(
                currentOccupancy.HeroId,
                currentOccupancy.Coordinate,
                destination));

            LastMoveRejectionReason = result.RejectionReason;

            if (!result.Accepted)
            {
                return false;
            }

            var updatedOccupancy = GetHeroOccupancyFromAuthority();
            heroView.Configure(updatedOccupancy, heroView.OuterRadius);
            ClearSelection();
            return true;
        }

        public bool TryCommitDestinationFromPointerRay(Ray pointerRay)
        {
            EnsureBindings();

            if (tileViews == null)
            {
                return false;
            }

            var hitTile = FindTileUnderPointer(pointerRay);
            return hitTile != null && TryCommitDestination(hitTile.Coordinate);
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

            var discoveredTiles = FindObjectsByType<HexTileView>(FindObjectsSortMode.None);
            if (discoveredTiles.Length > 0 &&
                (tileViews == null ||
                 tileViews.Length == 0 ||
                 tileViews.Any(tile => tile == null) ||
                 discoveredTiles.Length != tileViews.Length))
            {
                tileViews = discoveredTiles;
            }
        }

        private void RebuildMoveAuthority()
        {
            if (heroView == null || tileViews == null)
            {
                moveAuthority = null;
                return;
            }

            var boardCoordinates = tileViews
                .Where(tile => tile != null)
                .Select(tile => tile.Coordinate)
                .ToArray();

            moveAuthority = new AdjacentMoveAuthority(
                boardCoordinates,
                new[] { heroView.Occupancy });
        }

        private void EnsureMoveAuthority()
        {
            if (moveAuthority == null)
            {
                RebuildMoveAuthority();
            }
        }

        private HeroOccupancy GetHeroOccupancyFromAuthority()
        {
            return moveAuthority.Occupancies.Single(occupancy => occupancy.HeroId.Equals(heroView.Occupancy.HeroId));
        }

        private HexTileView FindTileUnderPointer(Ray pointerRay)
        {
            var boardPlane = new Plane(Vector3.up, Vector3.zero);
            if (!boardPlane.Raycast(pointerRay, out var enter))
            {
                return null;
            }

            var hitPoint = pointerRay.GetPoint(enter);
            return tileViews
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
            EnsureMoveAuthority();
        }
    }
}
