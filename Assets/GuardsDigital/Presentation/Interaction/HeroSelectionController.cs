using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Rules.AdjacentMove;
using GuardsDigital.Core.Rules.Movement;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    [DisallowMultipleComponent]
    public sealed partial class HeroSelectionController : MonoBehaviour
    {
        public const int DefaultMovementAllowance = 3;

        [SerializeField]
        private PrimitiveHeroView heroView;

        [SerializeField]
        private PrimitiveHeroView[] otherHeroViews;

        [SerializeField]
        private HexTileView[] tileViews;

        [SerializeField]
        private PlaceholderObstacleView[] obstacleViews;

        [SerializeField]
        private Material highlightMaterial;

        [SerializeField]
        private int movementAllowance = DefaultMovementAllowance;

        private readonly List<HexCoordinate> highlightedCoordinates = new List<HexCoordinate>();
        private readonly Dictionary<HexCoordinate, ReachableMovementDestination> reachableDestinations =
            new Dictionary<HexCoordinate, ReachableMovementDestination>();

        private AdjacentMoveAuthority moveAuthority;
        private MovementSession movementSession;

        public bool IsHeroSelected { get; private set; }

        public IReadOnlyList<HexCoordinate> HighlightedCoordinates => highlightedCoordinates;

        public Material HighlightMaterial => EnsureHighlightMaterial();

        public IReadOnlyList<PlaceholderObstacleView> ObstacleViews =>
            obstacleViews != null ? obstacleViews.Where(obstacle => obstacle != null).ToArray() : System.Array.Empty<PlaceholderObstacleView>();

        public IReadOnlyList<PrimitiveHeroView> OtherHeroViews =>
            otherHeroViews != null ? otherHeroViews.Where(hero => hero != null).ToArray() : System.Array.Empty<PrimitiveHeroView>();

        public PrimitiveHeroView ActiveHeroView
        {
            get
            {
                EnsureBindings();
                return heroView;
            }
        }

        public IReadOnlyList<HeroOccupancy> HeroOccupancies
        {
            get
            {
                EnsureBindings();
                EnsureMoveAuthority();
                return moveAuthority != null
                    ? moveAuthority.Occupancies
                    : System.Array.Empty<HeroOccupancy>();
            }
        }

        public int MovementAllowance => movementAllowance;

        public int RemainingMovementAllowance => cardDrivenMovementEnabled
            ? CardRemainingMovementAllowance
            : movementSession != null ? movementSession.RemainingAllowance : 0;

        public bool IsMovementSessionActive => cardDrivenMovementEnabled
            ? IsCardMovementResolutionPending
            : movementSession != null && !movementSession.IsComplete;

        public IReadOnlyList<HexCoordinate> MovementRoute =>
            cardDrivenMovementEnabled
                ? CardMovementRoute
                : movementSession != null ? movementSession.Route : System.Array.Empty<HexCoordinate>();

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

        public void Configure(
            PrimitiveHeroView hero,
            IEnumerable<HexTileView> tiles,
            Material material,
            int allowance = DefaultMovementAllowance,
            IEnumerable<PlaceholderObstacleView> obstacles = null,
            IEnumerable<PrimitiveHeroView> otherHeroes = null)
        {
            heroView = hero;
            otherHeroViews = otherHeroes?.Where(otherHero => otherHero != null && otherHero != hero).ToArray();
            tileViews = tiles?.Where(tile => tile != null).ToArray();
            obstacleViews = obstacles?.Where(obstacle => obstacle != null).ToArray();
            highlightMaterial = material;
            movementAllowance = allowance;
            movementSession = null;
            reachableDestinations.Clear();
            RebuildMoveAuthority();
            ClearSelectionVisuals();
            ResetCardDrivenMovementPresentation();
            InitializeCardDrivenMovementIfEnabled();
        }

        public void SelectHero()
        {
            EnsureBindings();
            TrySelectHero(heroView);
        }

        public bool TrySelectHero(PrimitiveHeroView selectedHero)
        {
            EnsureBindings();
            EnsureMoveAuthority();

            if (selectedHero == null)
            {
                return false;
            }

            var boundHeroes = new[] { heroView }
                .Concat(otherHeroViews ?? System.Array.Empty<PrimitiveHeroView>())
                .Where(hero => hero != null)
                .Distinct()
                .ToArray();
            if (!boundHeroes.Contains(selectedHero))
            {
                return false;
            }

            if (cardDrivenMovementEnabled && IsAttackTargetPending)
            {
                return TrySubmitAttackTarget(selectedHero.Occupancy.HeroId);
            }

            if (cardDrivenMovementEnabled && IsDefenseResponsePending)
            {
                return false;
            }

            if (cardDrivenMovementEnabled && IsAttackOutcomePending)
            {
                return false;
            }

            if (IsMovementSessionActive)
            {
                return selectedHero == heroView && StopMovementSession();
            }

            if (cardDrivenMovementEnabled && IsVisibleTurnActive)
            {
                return selectedHero == heroView;
            }

            if (selectedHero != heroView)
            {
                ClearSelectionVisuals();
                heroView = selectedHero;
                otherHeroViews = boundHeroes.Where(hero => hero != selectedHero).ToArray();
                movementSession = null;
                RebuildMoveAuthority();

                if (cardDrivenMovementEnabled)
                {
                    ResetCardDrivenMovementPresentation();
                    InitializeCardDrivenMovementIfEnabled();
                    cardStatus = $"{heroView.TeamSide} hero selected. Choose a test card.";
                }
            }

            if (cardDrivenMovementEnabled)
            {
                return true;
            }

            if (heroView == null || tileViews == null)
            {
                IsHeroSelected = false;
                return false;
            }

            RebuildMoveAuthority();
            var selectedOccupancy = GetHeroOccupancyFromAuthority();
            movementSession = new MovementSession(
                moveAuthority,
                selectedOccupancy.HeroId,
                movementAllowance);
            RefreshSelectionHighlights(selectedOccupancy);
            return true;
        }

        public bool StopMovementSession()
        {
            if (cardDrivenMovementEnabled)
            {
                return IsCardMovementResolutionPending && TryStopCardMovement();
            }

            if (!IsHeroSelected || movementSession == null || movementSession.IsComplete)
            {
                return false;
            }

            movementSession.Complete();
            ClearSelectionVisuals();
            return true;
        }

        private void RefreshSelectionHighlights(HeroOccupancy selectedOccupancy)
        {
            ClearTileHighlights();
            highlightedCoordinates.Clear();
            reachableDestinations.Clear();

            var availableTiles = tileViews.Where(tile => tile != null).ToArray();
            var boardCoordinates = availableTiles.Select(tile => tile.Coordinate);
            var destinations = MovementReachabilityQuery.FindReachableDestinations(
                selectedOccupancy,
                boardCoordinates,
                moveAuthority.Occupancies,
                moveAuthority.ObstacleOccupancies,
                movementSession != null ? movementSession.RemainingAllowance : movementAllowance);
            var destinationSet = new HashSet<HexCoordinate>(destinations.Select(destination => destination.Destination));

            foreach (var destination in destinations)
            {
                highlightedCoordinates.Add(destination.Destination);
                reachableDestinations[destination.Destination] = destination;
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

            if (cardDrivenMovementEnabled)
            {
                return TrySubmitCardDestination(destination);
            }

            if (!IsHeroSelected || heroView == null || moveAuthority == null)
            {
                return false;
            }

            if (movementSession == null)
            {
                var currentOccupancy = GetHeroOccupancyFromAuthority();
                movementSession = new MovementSession(
                    moveAuthority,
                    currentOccupancy.HeroId,
                    movementAllowance);
            }

            if (!reachableDestinations.TryGetValue(destination, out var reachableDestination))
            {
                LastMoveRejectionReason = AdjacentMoveRejectionReason.None;
                return false;
            }

            for (var routeIndex = 1; routeIndex < reachableDestination.Route.Count; routeIndex++)
            {
                var result = movementSession.TryCommitStep(reachableDestination.Route[routeIndex]);
                LastMoveRejectionReason = result.AdjacentMoveRejectionReason;

                if (!result.Accepted)
                {
                    return false;
                }
            }

            var updatedOccupancy = GetHeroOccupancyFromAuthority();
            heroView.Configure(updatedOccupancy, heroView.OuterRadius);
            movementSession.Complete();
            ClearSelectionVisuals();

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
            if (cardDrivenMovementEnabled)
            {
                if (IsCardMovementResolutionPending)
                {
                    TryStopCardMovement();
                }
                else if (!IsCardResolutionPending)
                {
                    ClearSelectionVisuals();
                }

                return;
            }

            if (movementSession != null && !movementSession.IsComplete)
            {
                movementSession.Complete();
            }

            ClearSelectionVisuals();
        }

        private void ClearSelectionVisuals()
        {
            ClearTileHighlights();
            highlightedCoordinates.Clear();
            reachableDestinations.Clear();
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
            var discoveredHeroes = FindObjectsByType<PrimitiveHeroView>(FindObjectsSortMode.None);
            if (heroView == null)
            {
                heroView = discoveredHeroes.FirstOrDefault(
                        hero => hero.Occupancy.HeroId.Value == PrimitiveHeroView.DefaultHeroId) ??
                    discoveredHeroes.FirstOrDefault();
            }

            if (discoveredHeroes.Length > 0 &&
                (otherHeroViews == null ||
                 otherHeroViews.Any(hero => hero == null) ||
                 otherHeroViews.Length != discoveredHeroes.Count(hero => hero != heroView)))
            {
                otherHeroViews = discoveredHeroes.Where(hero => hero != heroView).ToArray();
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

            var discoveredObstacles = FindObjectsByType<PlaceholderObstacleView>(FindObjectsSortMode.None);
            if (discoveredObstacles.Length > 0 &&
                (obstacleViews == null ||
                 obstacleViews.Length == 0 ||
                 obstacleViews.Any(obstacle => obstacle == null) ||
                 discoveredObstacles.Length != obstacleViews.Length))
            {
                obstacleViews = discoveredObstacles;
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
            var obstacleOccupancies = obstacleViews != null
                ? obstacleViews
                    .Where(obstacle => obstacle != null)
                    .Select(obstacle => obstacle.Occupancy)
                    .ToArray()
                : System.Array.Empty<ObstacleOccupancy>();

            var heroOccupancies = new[] { heroView }
                .Concat(otherHeroViews ?? System.Array.Empty<PrimitiveHeroView>())
                .Where(hero => hero != null)
                .Select(hero => hero.Occupancy)
                .ToArray();

            moveAuthority = new AdjacentMoveAuthority(boardCoordinates, heroOccupancies, obstacleOccupancies);
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
            InitializeCardDrivenMovementIfEnabled();
        }
    }
}
