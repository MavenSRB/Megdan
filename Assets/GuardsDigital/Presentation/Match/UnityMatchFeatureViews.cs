using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionUnityView : IBoardInteractionView
    {
        private readonly IReadOnlyList<PrimitiveHeroView> heroes;
        private IReadOnlyList<StandardMinionView> minions;
        private readonly List<PlacedTokenView> tokens = new List<PlacedTokenView>();
        private readonly Func<PlacedToken, PlacedTokenView> tokenViewFactory;
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly HexCornerHighlightTemplate cornerHighlightTemplate;
        private ActingHeroMarker actingHeroMarker;
        private GuardsDigital.Core.Board.HexCoordinate? hoveredCoordinate;
        private HeroId hoveredHeroId;
        private HeroId pointerHoveredHeroId;
        private HeroId interfaceHoveredHeroId;
        private Func<HeroId, bool> heroIntentHandler;
        private Func<MinionId, bool> minionIntentHandler;
        private Func<GuardsDigital.Core.Board.HexCoordinate, bool> destinationIntentHandler;

        public BoardInteractionUnityView(
            IEnumerable<PrimitiveHeroView> heroes,
            IEnumerable<HexTileView> tiles,
            Material highlightMaterial,
            IEnumerable<StandardMinionView> minions = null,
            Material resultingSpaceMaterial = null,
            Func<PlacedToken, PlacedTokenView> tokenViewFactory = null,
            Material highlightRimMaterial = null,
            Material resultingSpaceRimMaterial = null,
            HexCornerHighlightTemplate sharedCornerHighlightTemplate = null)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.minions = Array.AsReadOnly((minions ?? Enumerable.Empty<StandardMinionView>())
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            // The material arguments remain source-compatible for lower-level historical fixtures;
            // the unified configured board consumes only the shared corner template.
            cornerHighlightTemplate = sharedCornerHighlightTemplate;
            if (cornerHighlightTemplate != null)
            {
                foreach (var tile in this.tiles.Where(value => value.IsPlayable))
                {
                    tile.ConfigureCornerHighlight(cornerHighlightTemplate);
                }
            }
            // Milestone 6.2d: where a view for a newly placed token comes from. A token has no
            // declared roster - its identity is minted when an effect places it - so unlike heroes and
            // minions its views cannot all exist before the match does. Left unbound, a projected
            // token faults rather than being quietly left undrawn.
            this.tokenViewFactory = tokenViewFactory;
        }

        public BoardInteractionViewModel Model { get; private set; }
        public IReadOnlyList<StandardMinionView> MinionViews => minions;

        /// <summary>The hero pieces this board draws, so a kinetic pass can offset the ones an event
        /// names without holding a second roster of its own. Milestone 8.35y.</summary>
        public IReadOnlyList<PrimitiveHeroView> HeroViews => heroes;

        /// <summary>
        /// The one acting-hero ring this board owns, or null before one has been needed. Milestone
        /// 8.35y pulses it while the viewer is being waited on; it stays this view's to
        /// position and to hide.
        /// </summary>
        public ActingHeroMarker ActingHeroMarker => actingHeroMarker;

        /// <summary>Every token view this board has ever built, active or pooled.</summary>
        public IReadOnlyList<PlacedTokenView> TokenViews => tokens;

        /// <summary>The tiles this board paints, so a caller can read what is currently lit.</summary>
        public IReadOnlyList<HexTileView> TileViews => tiles;

        public event Action<HeroId> HeroSubmitted;
        public event Action<HeroId> HeroHighlighted;
        public event Action<MinionId> MinionSubmitted;
        public event Action<GuardsDigital.Core.Board.HexCoordinate> DestinationSubmitted;

        public void BindIntentHandlers(
            Func<HeroId, bool> submitHero,
            Func<MinionId, bool> submitMinion,
            Func<GuardsDigital.Core.Board.HexCoordinate, bool> submitDestination)
        {
            heroIntentHandler = submitHero ?? throw new ArgumentNullException(nameof(submitHero));
            minionIntentHandler = submitMinion ?? throw new ArgumentNullException(nameof(submitMinion));
            destinationIntentHandler = submitDestination ??
                throw new ArgumentNullException(nameof(submitDestination));
        }

        public void Render(BoardInteractionViewModel model) => Render(model, false);

        private void Render(BoardInteractionViewModel model, bool refreshHover)
        {
            if (!refreshHover && ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            PrimitiveHeroView actingHero = null;
            foreach (var hero in heroes)
            {
                var occupancy = model.Heroes.SingleOrDefault(value =>
                    value.HeroId.Equals(hero.Occupancy.HeroId));
                if (!occupancy.HeroId.IsDefined)
                {
                    hero.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.None);
                    hero.gameObject.SetActive(false);
                    continue;
                }

                // Hero views are retained across removal for Respawn, Undo, and restore, just as
                // minion views are retained across removal. Projection presence owns visibility;
                // an absent hero must not remain as a stale selectable board object.
                hero.Configure(occupancy, hero.OuterRadius);
                hero.gameObject.SetActive(true);

                // Every board choice is now carried by the occupied hex's six corners. The retained
                // ring remains part of the reusable piece view, but configured play never activates it.
                hero.SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);

                // Milestone 8.35n (the FOURTH amendment). `FocusedHeroId` is `SelectedActor`'s own
                // hero, unscoped and public, and unrelated to `hoveredHeroId` above - a viewer
                // pointing at some other hero's plaque must not steal the acting-hero marker.
                if (model.FocusedHeroId.IsDefined &&
                    occupancy.HeroId.Equals(model.FocusedHeroId))
                {
                    actingHero = hero;
                }
            }

            RenderActingHeroMarker(actingHero);

            RenderMinions(model);
            RenderTokens(model);

            // Milestone 8.30h's revised board language is deliberately uniform: every answer with a
            // board coordinate lights the same six fixed corner marks. Stable piece identity is still
            // projected and clicked as before; this view resolves its current occupied coordinate only
            // to place the presentation cue. It never invents eligibility.

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>();
            // Milestone 6.4b highlighted every correction route endpoint and Place space at once;
            // milestone 6.2d lights the one whose control is being pointed at, because each of them
            // carries its own control and the board was saying nothing the controls did not already.
            var movementEffectCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                model.EffectChoiceMovementDestinations);
            // Milestone 6.5c: an empty effect-choice candidate is a destination like any other, and
            // is the whole surface for that candidate - no control is drawn for it anywhere.
            targetCoordinates.UnionWith(model.EffectChoiceDestinations.Where(value =>
                !movementEffectCoordinates.Contains(value)));
            // Milestone 8.16c: a minion whose Spawn point is blocked is Placed into one of the
            // equally-nearest empty spaces. Those answers are spaces, so every one is lit before the
            // pointer moves and the HUD needs no destination control.
            if (model.EffectChoiceTargetSpace.HasValue)
            {
                targetCoordinates.Add(model.EffectChoiceTargetSpace.Value);
            }

            foreach (var cue in model.PieceTargetCues.Where(value =>
                         value.State != BoardTargetCueState.None &&
                         value.ConsequenceIntent == BoardTargetConsequenceIntent.Affect))
            {
                if (TryResolveCoordinate(model, cue.Target, out var coordinate))
                {
                    targetCoordinates.Add(coordinate);
                }
            }

            // The predicted landing space uses its own neutral corners and is never selectable, so it
            // is resolved after the selectable set and can never be added to it.
            var resultingSpace = model.EffectChoiceResultingSpace;
            var movementCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                model.MovementDestinations);
            // Fast Travel replaces a Movement action and uses the same destination language. The
            // projection still distinguishes its typed invocation; only the six-corner treatment is
            // shared here.
            movementCoordinates.UnionWith(model.FastTravelDestinations);
            movementCoordinates.UnionWith(movementEffectCoordinates);
            movementCoordinates.UnionWith(
                model.SystemTransitionDestinations.Select(value => value.Destination));
            if (model.CorrectionDestination.HasValue)
            {
                movementCoordinates.Add(model.CorrectionDestination.Value);
            }
            var respawnCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                model.HeroRespawnDestinations);
            if (model.HeroRespawnHighlight.HasValue)
            {
                respawnCoordinates.Add(model.HeroRespawnHighlight.Value);
            }
            var attackHeroIds = new HashSet<HeroId>(model.AttackTargets);
            var attackMinionIds = new HashSet<MinionId>(model.MinionAttackTargets);
            var attackCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                model.Heroes.Where(value => attackHeroIds.Contains(value.HeroId))
                    .Select(value => value.Coordinate));
            attackCoordinates.UnionWith(model.Minions
                .Where(value => attackMinionIds.Contains(value.MinionId))
                .Select(value => value.Coordinate));
            var activeDefenseCues = model.MinionDefenseCues;
            if (activeDefenseCues.Count == 0 && hoveredCoordinate.HasValue)
            {
                var hoveredHero = model.Heroes.FirstOrDefault(value =>
                    value.Coordinate.Equals(hoveredCoordinate.Value));
                var targetPreview = !hoveredHero.HeroId.IsDefined
                    ? null
                    : model.AttackTargetDefensePreviews.FirstOrDefault(value =>
                        value.HeroId.Equals(hoveredHero.HeroId));
                if (targetPreview != null)
                {
                    activeDefenseCues = Array.AsReadOnly(targetPreview.MinionContributions
                        .Select(value => new MinionDefenseCueViewModel(
                            value.SourceMinionId, value.Amount))
                        .ToArray());
                }
            }
            var minionCoordinates = model.Minions.ToDictionary(
                value => value.MinionId, value => value.Coordinate);
            var defenseCoordinates = activeDefenseCues.ToDictionary(
                value => minionCoordinates[value.MinionId],
                value => value.Amount > 0
                    ? HexCornerHighlightKind.DefensePositive
                    : HexCornerHighlightKind.DefenseNegative);
            var allyCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>();
            GuardsDigital.Core.Board.HexCoordinate? hoveredHeroCoordinate = null;
            if (hoveredHeroId.IsDefined)
            {
                var hoveredHero = model.Heroes.FirstOrDefault(value =>
                    value.HeroId.Equals(hoveredHeroId));
                if (hoveredHero.HeroId.IsDefined)
                    hoveredHeroCoordinate = hoveredHero.Coordinate;
            }
            foreach (var cue in model.PieceTargetCues.Where(value =>
                         value.State != BoardTargetCueState.None &&
                         (value.ConsequenceIntent == BoardTargetConsequenceIntent.Attack ||
                          value.ConsequenceIntent == BoardTargetConsequenceIntent.Removal)))
            {
                if (TryResolveCoordinate(model, cue.Target, out var coordinate))
                {
                    attackCoordinates.Add(coordinate);
                }
            }
            foreach (var cue in model.PieceTargetCues.Where(value =>
                         value.State != BoardTargetCueState.None &&
                         value.ConsequenceIntent == BoardTargetConsequenceIntent.Ally))
            {
                if (TryResolveCoordinate(model, cue.Target, out var coordinate))
                {
                    allyCoordinates.Add(coordinate);
                }
            }
            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                // Clear any state left by a pre-8.30h renderer. Configured play no longer activates
                // whole-hex fills or rims for a board choice or projected result.
                tile.SetHighlighted(false, null, null);

                if (cornerHighlightTemplate != null)
                {
                    var kind = hoveredHeroCoordinate.HasValue &&
                               hoveredHeroCoordinate.Value.Equals(tile.Coordinate)
                        ? HexCornerHighlightKind.Ally
                        : attackCoordinates.Contains(tile.Coordinate)
                            ? HexCornerHighlightKind.Attack
                        : respawnCoordinates.Contains(tile.Coordinate)
                            ? HexCornerHighlightKind.Respawn
                            : movementCoordinates.Contains(tile.Coordinate)
                            ? HexCornerHighlightKind.Movement
                            : allyCoordinates.Contains(tile.Coordinate)
                                ? HexCornerHighlightKind.Ally
                                : targetCoordinates.Contains(tile.Coordinate)
                                    ? HexCornerHighlightKind.Effect
                                    : resultingSpace.HasValue &&
                                      resultingSpace.Value.Equals(tile.Coordinate)
                                        ? HexCornerHighlightKind.EffectResult
                                        : HexCornerHighlightKind.None;
                    tile.SetCornerHighlighted(
                        kind,
                        hoveredCoordinate.HasValue &&
                        hoveredCoordinate.Value.Equals(tile.Coordinate),
                        defenseCoordinates.TryGetValue(tile.Coordinate, out var defenseKind)
                            ? defenseKind
                            : HexCornerHighlightKind.None);
                }
            }
        }

        private static bool TryResolveCoordinate(
            BoardInteractionViewModel model,
            BoardObjectReference target,
            out GuardsDigital.Core.Board.HexCoordinate coordinate)
        {
            if (target.Kind == BoardObjectKind.Hero)
            {
                var occupancy = model.Heroes.Single(value => value.HeroId.Equals(target.HeroId));
                coordinate = occupancy.Coordinate;
                return true;
            }

            if (target.Kind == BoardObjectKind.Minion)
            {
                var occupancy = model.Minions.Single(value => value.MinionId.Equals(target.MinionId));
                coordinate = occupancy.Coordinate;
                return true;
            }

            if (target.Kind == BoardObjectKind.Token)
            {
                var token = model.PlacedTokens.Single(value => value.Id.Equals(target.TokenInstanceId));
                coordinate = token.Coordinate;
                return true;
            }

            coordinate = default;
            return false;
        }

        /// <summary>
        /// Pointer-only visual strength. The projected model still exclusively decides which spaces
        /// are legal; this may only strengthen a corner construction that Render already made visible.
        /// </summary>
        public void SetHoveredCoordinate(GuardsDigital.Core.Board.HexCoordinate? coordinate)
        {
            if (Nullable.Equals(hoveredCoordinate, coordinate)) return;
            hoveredCoordinate = coordinate;
            if (Model != null) Render(Model, true);
        }

        /// <summary>
        /// Standing presentation-only hero linkage shared by the map, hero plaques and Initiative
        /// preference buttons. It never submits or changes selection.
        /// </summary>
        public void SetHoveredHero(HeroId heroId)
        {
            interfaceHoveredHeroId = heroId;
            ApplyHoveredHero();
        }

        public void SetPointerHoveredHero(HeroId heroId)
        {
            pointerHoveredHeroId = heroId;
            ApplyHoveredHero();
        }

        /// <summary>
        /// Milestone 8.35n (the FOURTH amendment): every viewer's board marks the acting hero's own
        /// piece with a distinct ring, and clears the moment no actor stands. One marker serves the
        /// whole board - at most one hero is ever the selected actor - created lazily on first use
        /// and repositioned here every <see cref="Render"/>, never retained per piece the way
        /// <see cref="Board.BoardTargetRing"/> is.
        /// </summary>
        private void RenderActingHeroMarker(PrimitiveHeroView actingHero)
        {
            if (actingHero == null)
            {
                actingHeroMarker?.SetHidden();
                return;
            }

            if (actingHeroMarker == null)
            {
                actingHeroMarker = ActingHeroMarker.Create();
            }

            actingHeroMarker.SetVisible(actingHero.transform.position, actingHero.SelectionRadius);
        }

        private void ApplyHoveredHero()
        {
            var next = interfaceHoveredHeroId.IsDefined
                ? interfaceHoveredHeroId
                : pointerHoveredHeroId;
            if (hoveredHeroId.Equals(next)) return;
            hoveredHeroId = next;
            HeroHighlighted?.Invoke(next);
            if (Model != null) Render(Model, true);
        }

        public void ReplaceMinionViews(IEnumerable<StandardMinionView> currentMinions)
        {
            minions = Array.AsReadOnly((currentMinions ??
                    throw new ArgumentNullException(nameof(currentMinions)))
                .Where(value => value != null)
                .Distinct()
                .ToArray());
            if (Model != null)
            {
                RenderMinions(Model);
            }
        }

        private void RenderMinions(BoardInteractionViewModel model)
        {
            var projectedMinions = model.Minions.ToDictionary(value => value.MinionId);
            foreach (var minion in minions)
            {
                var boundId = minion.BoundMinionId;
                if (boundId.IsDefined && projectedMinions.TryGetValue(boundId, out var state))
                {
                    minion.gameObject.SetActive(true);
                    if (!ReferenceEquals(minion.Occupancy, state))
                    {
                        minion.Configure(state, minion.OuterRadius);
                    }

                    minion.SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.None);
                    minion.gameObject.SetActive(false);
                }
            }

            if (projectedMinions.Count > 0)
            {
                throw new InvalidOperationException("The configured board has no view for a projected minion identity.");
            }
        }

        /// <summary>
        /// Milestone 6.2d: draws one piece per projected token and nothing else.
        ///
        /// The discipline is the minions': projection presence owns visibility, a view is never
        /// destroyed, and a projected identity with nowhere to go is a fault rather than a silently
        /// missing piece. What differs is where a view comes from. A minion's identity is declared in
        /// the setup, so every minion view exists before the match starts and stays bound to its own
        /// identity forever. A token's identity is minted the moment an effect places one, so views
        /// are pooled instead: a view is bound while its token is projected, released when it is not,
        /// and the pool grows only when more tokens stand at once than ever have before. That is what
        /// makes a Remove, a Clear and an Undo all reach the same place - the piece stops being drawn
        /// or starts again, and no identity outlives the projection that named it.
        ///
        /// A Replace needs no arm of its own. The outgoing token leaves the projection and the
        /// incoming one arrives in the same command, so the space is redrawn once, carrying the new
        /// definition's name; it is never drawn empty in between, which is the single-stage property
        /// the operation itself declares.
        /// </summary>
        private void RenderTokens(BoardInteractionViewModel model)
        {
            var projectedTokens = model.PlacedTokens.ToDictionary(value => value.Id);
            var released = new List<PlacedTokenView>();
            foreach (var token in tokens)
            {
                var boundId = token.BoundTokenId;
                if (boundId.IsDefined && projectedTokens.TryGetValue(boundId, out var state))
                {
                    token.gameObject.SetActive(true);
                    if (!ReferenceEquals(token.Token, state))
                    {
                        token.Configure(state, token.OuterRadius);
                    }

                    token.SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);
                    projectedTokens.Remove(boundId);
                }
                else
                {
                    token.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.None);
                    token.gameObject.SetActive(false);
                    released.Add(token);
                }
            }

            var freeIndex = 0;
            foreach (var state in model.PlacedTokens.Where(value => projectedTokens.ContainsKey(value.Id)))
            {
                PlacedTokenView view;
                if (freeIndex < released.Count)
                {
                    view = released[freeIndex++];
                    view.gameObject.SetActive(true);
                    view.Configure(state, view.OuterRadius);
                }
                else
                {
                    view = CreateTokenView(state);
                    view.gameObject.SetActive(true);
                }

                view.SetTargetCue(BoardTargetCueState.None, BoardTargetConsequenceIntent.None);
            }
        }

        /// <summary>
        /// Asks the board for a view of a token it has never drawn. The factory owns the board's own
        /// outer radius and hands back a view already configured for this token, which is why nothing
        /// here has to know one.
        /// </summary>
        private PlacedTokenView CreateTokenView(PlacedToken state)
        {
            var created = tokenViewFactory?.Invoke(state);
            if (created == null)
            {
                throw new InvalidOperationException(
                    "The configured board has no view for a projected token identity.");
            }

            tokens.Add(created);
            return created;
        }

        public bool TryForwardHeroIntent(HeroId heroId)
        {
            if (heroIntentHandler != null)
            {
                return heroIntentHandler(heroId);
            }

            if (HeroSubmitted == null)
            {
                return false;
            }

            HeroSubmitted.Invoke(heroId);
            return true;
        }

        public bool TryForwardMinionIntent(MinionId minionId)
        {
            if (minionIntentHandler != null)
            {
                return minionIntentHandler(minionId);
            }

            if (MinionSubmitted == null)
            {
                return false;
            }

            MinionSubmitted.Invoke(minionId);
            return true;
        }

        public bool TryForwardDestinationIntent(GuardsDigital.Core.Board.HexCoordinate destination)
        {
            if (destinationIntentHandler != null)
            {
                return destinationIntentHandler(destination);
            }

            if (DestinationSubmitted == null)
            {
                return false;
            }

            DestinationSubmitted.Invoke(destination);
            return true;
        }
    }
}
