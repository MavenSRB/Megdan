using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Obstacles;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    public sealed class BoardInteractionUnityView : IBoardInteractionView
    {
        // Milestone 9.21i. A list rather than a fixed roster: a many-bodied hero's second, third and
        // fourth standees are drawn the first time the projection stands one (EnsureHeroBodyViews).
        private readonly List<PrimitiveHeroView> heroes;
        private IReadOnlyList<StandardMinionView> minions;
        private readonly List<PlacedTokenView> tokens = new List<PlacedTokenView>();
        private readonly List<PlaceholderObstacleView> projectedObstacles =
            new List<PlaceholderObstacleView>();
        private readonly Func<PlacedToken, PlacedTokenView> tokenViewFactory;
        private readonly IReadOnlyList<HexTileView> tiles;
        private readonly HexCornerHighlightTemplate cornerHighlightTemplate;
        private ActingHeroMarker actingHeroMarker;
        private readonly List<ActingHeroMarker> faintActingMarkers = new List<ActingHeroMarker>();
        private GuardsDigital.Core.Board.HexCoordinate? hoveredCoordinate;
        private HeroId hoveredHeroId;
        private HeroId pointerHoveredHeroId;
        private int pointerHoveredHeroBody;
        private int hoveredHeroBody;
        private Func<HeroId, int, bool> heroBodyIntentHandler;
        private HeroId interfaceHoveredHeroId;
        private Func<HeroId, bool> heroIntentHandler;
        private Func<MinionId, bool> minionIntentHandler;
        private Func<GuardsDigital.Core.Board.HexCoordinate, bool> destinationIntentHandler;
        private Func<HeroId, bool> heroViewIntentHandler;

        // Milestone 8.35z6 (the SIXTEENTH amendment's Defeat shrink-and-fade, delivered
        // pooling-safely). Keyed by the piece's OWN view instance rather than its identity value: a
        // hero or minion view is bound to exactly one identity for the life of a match (unlike a
        // PlacedTokenView, whose identity is reassigned on release), so the instance is already the
        // stable key an identity value would only restate.
        private readonly Dictionary<PrimitiveHeroView, DefeatFadeGhostView> heroDefeatGhosts =
            new Dictionary<PrimitiveHeroView, DefeatFadeGhostView>();
        private readonly Dictionary<StandardMinionView, DefeatFadeGhostView> minionDefeatGhosts =
            new Dictionary<StandardMinionView, DefeatFadeGhostView>();
        private readonly List<DefeatFadeGhostView> freeHeroDefeatGhosts = new List<DefeatFadeGhostView>();
        private readonly List<DefeatFadeGhostView> freeMinionDefeatGhosts =
            new List<DefeatFadeGhostView>();

        // Milestone 8.35z63. The one violet tint and the one hex mesh every Active-effect
        // disk is painted with, generated on first use so a board with nothing running allocates
        // nothing at all, and shared by every tinted space.
        private Material activeEffectAuraMaterial;
        private Mesh activeEffectAuraMesh;

        // The tiles currently tinted, so the per-frame glide walks the disk rather than all 210 tiles.
        private readonly List<HexTileView> litAuraTiles = new List<HexTileView>();

        // Milestone 8.46d. The one arrow construction and the (at most six) arrows drawn from it,
        // generated on first use exactly as the Active-effect disk above is: a board that is never
        // asked a direction allocates nothing, and an answered direction hides them rather than
        // destroying them.
        private HexDirectionArrowTemplate directionArrowTemplate;
        private readonly List<HexDirectionArrow> directionArrows = new List<HexDirectionArrow>();

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
            this.heroes = (heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToList();
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
        /// Milestone 9.21i. Raised once for each standee this board draws for a body the scene never
        /// authored, after it is built and its pointer selector is bound to this board - so a
        /// composition can offer it to the pointer and retire it with the views it generated.
        /// </summary>
        public event Action<PrimitiveHeroView> HeroBodyViewCreated;

        /// <summary>
        /// The one acting-hero ring this board owns, or null before one has been needed. Milestone
        /// 8.35y pulses it while the viewer is being waited on; it stays this view's to
        /// position and to hide.
        /// </summary>
        public ActingHeroMarker ActingHeroMarker => actingHeroMarker;

        /// <summary>
        /// Milestone 9.21i. The faint gold rings on the acting hero's OTHER standing bodies, pooled;
        /// only the visible ones are drawn. Empty for every single-bodied hero.
        /// </summary>
        public IReadOnlyList<ActingHeroMarker> FaintActingMarkers => faintActingMarkers;

        /// <summary>Every token view this board has ever built, active or pooled.</summary>
        public IReadOnlyList<PlacedTokenView> TokenViews => tokens;

        /// <summary>The tiles this board paints, so a caller can read what is currently lit.</summary>
        public IReadOnlyList<HexTileView> TileViews => tiles;

        /// <summary>
        /// The generated violet tint an Active effect's disk is painted with, or null before any disk
        /// has needed one. Milestone 8.35z63.
        ///
        /// <para><b>Blended paint rather than the luminous light the six-corner marks now use, and the
        /// difference is the whole overlap rule.</b> A mark is a thin shape that says "act here", and
        /// milestone 8.35z39 made those ADD their colour so the board reads as lit. A disk is a large
        /// standing area that says "this is going on", and added light over a whole hex saturates
        /// toward white - it would wash out the artwork, and worse, it would out-shout the gold
        /// awaited-shine family whose job is to say "waiting on you". So the aura is milestone 8.11a's
        /// FILL instead, at <see cref="BoardOverlayMaterial.OverlayAlpha"/>: the board's own painting
        /// reads straight through it, every corner mark and every piece is drawn above it, and gold
        /// still wins the eye where both apply. Both are drawn; neither hides the other.</para>
        /// </summary>
        public Material ActiveEffectAuraMaterial => activeEffectAuraMaterial;

        /// <summary>
        /// The one hex mesh every tinted space shares, or null before any disk has needed one. A disk
        /// of sixty-one spaces is sixty-one renderers pointing at this, not sixty-one meshes.
        /// </summary>
        public Mesh ActiveEffectAuraMesh => activeEffectAuraMesh;

        /// <summary>The native resources this view itself generated, for the hygiene fixtures.</summary>
        public int OwnedNativeResourceCount =>
            (activeEffectAuraMaterial == null ? 0 : 1) + (activeEffectAuraMesh == null ? 0 : 1) +
            (directionArrowTemplate == null ? 0 : directionArrowTemplate.OwnedNativeResourceCount);

        /// <summary>
        /// The direction arrows this board has built, drawn or hidden. Empty until a direction choice
        /// has stood at least once. Milestone 8.46d.
        /// </summary>
        public IReadOnlyList<HexDirectionArrow> DirectionArrows => directionArrows;

        /// <summary>
        /// Releases what this view generated. The composition root calls it where it already disposes
        /// the shared corner construction, for the reason that type documents.
        /// </summary>
        public void ReleaseOwnedResources()
        {
            var ownedMaterial = activeEffectAuraMaterial;
            var ownedMesh = activeEffectAuraMesh;
            activeEffectAuraMaterial = null;
            activeEffectAuraMesh = null;
            foreach (var tile in tiles)
            {
                if (tile != null)
                {
                    tile.SetActiveEffectAura(false, null, null);
                }
            }

            DestroyOwnedResource(ownedMaterial);
            DestroyOwnedResource(ownedMesh);

            // Milestone 8.46d. The arrows are destroyed before the construction they point at, for
            // the reason every generated board child in this project follows: a renderer left
            // referencing a released mesh outlives the release by a frame.
            var ownedArrowTemplate = directionArrowTemplate;
            directionArrowTemplate = null;
            foreach (var arrow in directionArrows)
            {
                if (arrow != null)
                {
                    DestroyOwnedResource(arrow.gameObject);
                }
            }

            directionArrows.Clear();
            ownedArrowTemplate?.Dispose();
        }

        private static void DestroyOwnedResource(UnityEngine.Object resource)
        {
            if (resource == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(resource);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(resource);
            }
        }

        private Material EnsureActiveEffectAuraMaterial() =>
            activeEffectAuraMaterial != null
                ? activeEffectAuraMaterial
                // Lit's shipped template preserves specular via premultiplication; changing its
                // keywords at runtime can select a stripped variant in a player. This Unlit
                // template already ships the exact alpha-blended variant the tint uses.
                : activeEffectAuraMaterial = BoardOverlayMaterial.CreateUnlitBlended(
                    ActiveEffectAuraMaterialName,
                    BoardOverlayMaterial.AtOverlayAlpha(MatchHudTheme.ActiveEffectAccent));

        private Mesh EnsureActiveEffectAuraMesh(float outerRadius)
        {
            if (activeEffectAuraMesh != null)
            {
                return activeEffectAuraMesh;
            }

            activeEffectAuraMesh = FlatHexMeshBuilder.Create(outerRadius);
            activeEffectAuraMesh.name = ActiveEffectAuraMaterialName;
            activeEffectAuraMesh.hideFlags = HideFlags.DontSave;
            return activeEffectAuraMesh;
        }

        public const string ActiveEffectAuraMaterialName = "Active Effect Aura";

        /// <summary>The spaces currently tinted by a live disk, in board order.</summary>
        public IReadOnlyList<HexTileView> LitAuraTiles => litAuraTiles;

        /// <summary>
        /// Slides every tinted space by its own carrier's live kinetic displacement, so a disk travels
        /// with the piece it is drawn around instead of arriving a hex ahead of it.
        ///
        /// <para><b>Milestone 8.35z63, answering the user's "Does it fly with animation?" -
        /// and it adds no animation authority.</b> It starts no clock, reads no duration and evaluates
        /// no easing curve. <see cref="PrimitiveHeroView.KineticDisplacement"/> is the offset the
        /// kinetic layer has ALREADY written into the piece's transform this frame; this copies it onto
        /// that carrier's disk and stops. When nothing is playing the displacement is exactly zero, so
        /// this writes zero and the disk sits on its settled, hex-derived spaces.</para>
        ///
        /// <para><b>Why the settled set never changes while it glides.</b> The projection has already
        /// moved the hero when the slide begins, so <see cref="ActiveEffectVisibilityModel"/> already
        /// holds the DESTINATION disk; the displacement is negative for the whole slide and eases to
        /// zero. Drawing the destination disk offset backwards by one hex step is geometrically the
        /// disk at the origin - a hex lattice is translation-invariant - so the disk appears to travel
        /// from where the piece was to where it is going, and agrees exactly with the hex-derived set
        /// at the instant the slide ends. Nothing re-derives membership mid-slide: what the rules
        /// consider affected is the authority's and is untouched here. These are pixels.</para>
        /// </summary>
        public void GlideActiveEffectAuras()
        {
            if (litAuraTiles.Count == 0)
            {
                return;
            }

            var model = Model?.ActiveEffects;
            for (var index = 0; index < litAuraTiles.Count; index++)
            {
                var tile = litAuraTiles[index];
                if (tile == null)
                {
                    continue;
                }

                tile.SetActiveEffectAuraGlide(DisplacementFor(model, tile.Coordinate));
            }
        }

        private Vector3 DisplacementFor(
            ActiveEffectVisibilityModel model, GuardsDigital.Core.Board.HexCoordinate space)
        {
            var aura = model?.AuraCovering(space);
            if (aura == null || !aura.Carrier.IsDefined)
            {
                return Vector3.zero;
            }

            for (var index = 0; index < heroes.Count; index++)
            {
                var hero = heroes[index];
                if (hero != null && hero.Occupancy.HeroId.Equals(aura.Carrier))
                {
                    // A piece that is not drawn is not being slid either.
                    return hero.gameObject.activeSelf ? hero.KineticDisplacement : Vector3.zero;
                }
            }

            return Vector3.zero;
        }

        public event Action<HeroId> HeroSubmitted;
        public event Action<HeroId> HeroHighlighted;
        public event Action<MinionId> MinionSubmitted;
        public event Action<GuardsDigital.Core.Board.HexCoordinate> DestinationSubmitted;

        /// <summary>
        /// Binds what a click on each thing the board draws MEANS.
        ///
        /// <para>Milestone 8.35z48 adds <paramref name="viewHero"/>, and the split is the point of it:
        /// the first three are AUTHORITY-BACKED - each one either answers something the match is
        /// asking or declines - while the fourth answers nothing and only points the tray at a hero.
        /// Leaving the fourth unbound is legal and means "this board has no tray to point"; it is not
        /// a degraded state.</para>
        /// </summary>
        public void BindIntentHandlers(
            Func<HeroId, bool> submitHero,
            Func<MinionId, bool> submitMinion,
            Func<GuardsDigital.Core.Board.HexCoordinate, bool> submitDestination,
            Func<HeroId, bool> viewHero = null,
            Func<HeroId, int, bool> submitHeroBody = null)
        {
            heroIntentHandler = submitHero ?? throw new ArgumentNullException(nameof(submitHero));
            // Milestone 9.21i. The standee's body travels with the click where a handler takes it.
            heroBodyIntentHandler = submitHeroBody;
            minionIntentHandler = submitMinion ?? throw new ArgumentNullException(nameof(submitMinion));
            destinationIntentHandler = submitDestination ??
                throw new ArgumentNullException(nameof(submitDestination));
            heroViewIntentHandler = viewHero;
        }

        public void Render(BoardInteractionViewModel model) => Render(model, false);

        private void Render(BoardInteractionViewModel model, bool refreshHover)
        {
            if (!refreshHover && ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));

            // Milestone 8.35z63. Read once for the whole pass: which pieces an Active effect
            // is standing on, and which spaces its disk covers. Both sets are the projection's, derived
            // by `ActiveEffectVisibility`; this view decides only how they are drawn.
            var activeEffects = model.ActiveEffects ?? ActiveEffectVisibilityModel.Empty;
            var activeEffectHeroes = new HashSet<HeroId>(activeEffects.MarkedHeroes);
            var activeEffectAura = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                activeEffects.AuraSpaces);
            litAuraTiles.Clear();

            // Milestone 9.21i. One standee per standing BODY. A body the scene never authored is drawn
            // the first time it stands, from its hero's own standee; each is then matched by (hero,
            // body), so two bodies of one hero never collapse into one piece.
            EnsureHeroBodyViews(model);
            PrimitiveHeroView actingHero = null;
            var otherActingBodies = new List<PrimitiveHeroView>();
            foreach (var hero in heroes)
            {
                var occupancy = FindHeroRow(model, hero.Occupancy.HeroId, hero.Body);
                if (!occupancy.HeroId.IsDefined)
                {
                    hero.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.None);
                    // A piece that is not on the board wears no ongoing-effect ring either. p.17
                    // cancels an Active effect on a defeated controller anyway; this makes the visual
                    // agree with that in the frame the piece disappears rather than one later.
                    hero.SetActiveEffectMark(false, MatchHudTheme.ActiveEffectAccent);
                    // Milestone 9.21i. A body REMOVED without a Defeat (9.21e's faces) leaves the
                    // projection exactly as a defeated one does, so it fades the same way here; no
                    // Defeat is narrated, because no Defeat row exists for it.
                    // Milestone 8.35z6. Only a piece that WAS drawn a moment ago is a genuine
                    // removal edge worth a fade - a hero this render pass finds still absent (already
                    // inactive) fires nothing a second time.
                    if (hero.gameObject.activeSelf)
                    {
                        SpawnHeroDefeatGhost(hero);
                    }

                    hero.gameObject.SetActive(false);
                    continue;
                }

                // Hero views are retained across removal for Respawn, Undo, and restore, just as
                // minion views are retained across removal. Projection presence owns visibility;
                // an absent hero must not remain as a stale selectable board object.
                hero.Configure(occupancy, hero.OuterRadius);
                hero.gameObject.SetActive(true);
                // Milestone 8.35z6. A hero reappearing (Respawn, Undo, restore) before its own
                // fade finished on its own must never leave a stale echo of a piece that is visible
                // again; the fade ghost is purely decorative, so cancelling it changes nothing the
                // projection owns.
                ReleaseHeroDefeatGhost(hero);

                // Milestone 8.62, AT THE USER'S DIRECTION - and it reverses 8.30h's rule for the one
                // cue the user was looking at. "can you make targeting highlights more visible, they
                // are almost impossible to see like this.. Blood red light instead of this pink i
                // think." 8.30h routed every board choice into the occupied hex's six corner marks
                // and left the fitted piece ring built, projected and never drawn; what a player saw
                // of a targeted HERO was therefore six small marks on the sand around them. The
                // projection has named these pieces all along - `BoardInteractionPresenter` gives
                // every legal Attack, Removal and card-text target its typed cue - so this draws the
                // cue that was already being computed. The six corner marks are UNCHANGED and still
                // mark the space: the ring and its wash are what say the PIECE is the target.
                ApplyPieceTargetCue(
                    hero, model, BoardObjectReference.ForHero(occupancy.HeroId), occupancy.Body);

                // Milestone 9.21i. The quiet per-body mark, only where a hero stands on several spaces
                // or on a body above the first: every single-bodied piece is drawn as before.
                var severalBodies = StandsOnSeveralSpaces(model, occupancy.HeroId);
                hero.SetBodyMark(severalBodies || occupancy.Body > HeroOccupancy.FirstBody
                    ? occupancy.Body
                    : 0);

                // Milestone 8.35z63: the tray's own Active violet, on the piece the effect is
                // standing on. It is a separate, inner annulus beside the token, so a
                // hero who is both being waited on and carrying an effect wears both marks at once.
                hero.SetActiveEffectMark(
                    activeEffectHeroes.Contains(occupancy.HeroId),
                    MatchHudTheme.ActiveEffectAccent);

                // Milestone 8.35n (the FOURTH amendment). `FocusedHeroId` is `SelectedActor`'s own
                // hero, unscoped and public, and unrelated to `hoveredHeroId` above - a viewer
                // pointing at some other hero's plaque must not steal the acting-hero marker.
                // Milestone 9.21i. On a hero standing on several spaces the ring is the ACTING CUE: it
                // stands on the engine's settled acting body (the model's FocusedHeroBody) and on no
                // body while none is settled - the pick is then still open, and every body is cued as
                // eligible instead.
                if (model.FocusedHeroId.IsDefined &&
                    occupancy.HeroId.Equals(model.FocusedHeroId) &&
                    (!severalBodies || model.FocusedHeroBody == occupancy.Body))
                {
                    actingHero = hero;
                }
                // Milestone 9.21i, USER-SETTLED: while a body is settled, every OTHER standing body
                // of that hero wears the same ring faintly - one bright, the rest faint.
                else if (severalBodies && model.FocusedHeroId.IsDefined &&
                         occupancy.HeroId.Equals(model.FocusedHeroId) &&
                         model.FocusedHeroBody >= HeroOccupancy.FirstBody)
                {
                    otherActingBodies.Add(hero);
                }
            }

            RenderActingHeroMarker(actingHero);
            RenderFaintActingMarkers(actingHero == null ? null : otherActingBodies);

            RenderMinions(model);
            RenderTokens(model);
            RenderObstacles(model);

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
                targetCoordinates.UnionWith(CoordinatesOf(model, cue.Target));
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
            // Milestone 9.21i. A hero on several spaces is marked only where a body is offered - his
            // piece cues name each offered body, and are read below.
            var attackCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(
                model.Heroes.Where(value => attackHeroIds.Contains(value.HeroId) &&
                        !StandsOnSeveralSpaces(model, value.HeroId))
                    .Select(value => value.Coordinate));
            attackCoordinates.UnionWith(model.Minions
                .Where(value => attackMinionIds.Contains(value.MinionId))
                .Select(value => value.Coordinate));
            var activeDefenseCues = model.MinionDefenseCues;
            if (activeDefenseCues.Count == 0 && hoveredCoordinate.HasValue)
            {
                var hoveredHero = model.Heroes.FirstOrDefault(value =>
                    value.Coordinate.Equals(hoveredCoordinate.Value));
                // Milestone 9.21i. The preview of the BODY under the pointer (RZ-Q3).
                var targetPreview = !hoveredHero.HeroId.IsDefined
                    ? null
                    : model.AttackTargetDefensePreviews.FirstOrDefault(value =>
                        value.HeroId.Equals(hoveredHero.HeroId) && value.HeroBody == hoveredHero.Body);
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
            // Milestone 9.21i. The standee under the pointer lights its own space; a hero pointed at
            // through the HUD (no body) lights every space he stands on.
            var hoveredHeroCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>();
            if (hoveredHeroId.IsDefined)
            {
                foreach (var hoveredHero in model.Heroes)
                {
                    if (hoveredHero.HeroId.Equals(hoveredHeroId) &&
                        (hoveredHeroBody < HeroOccupancy.FirstBody || hoveredHero.Body == hoveredHeroBody))
                        hoveredHeroCoordinates.Add(hoveredHero.Coordinate);
                }
            }
            foreach (var cue in model.PieceTargetCues.Where(value =>
                         value.State != BoardTargetCueState.None &&
                         (value.ConsequenceIntent == BoardTargetConsequenceIntent.Attack ||
                          value.ConsequenceIntent == BoardTargetConsequenceIntent.Removal)))
            {
                attackCoordinates.UnionWith(CoordinatesOf(model, cue.Target));
            }
            foreach (var cue in model.PieceTargetCues.Where(value =>
                         value.State != BoardTargetCueState.None &&
                         value.ConsequenceIntent == BoardTargetConsequenceIntent.Ally))
            {
                allyCoordinates.UnionWith(CoordinatesOf(model, cue.Target));
            }
            var targetOutlineSides = TargetOutlineSides(model);
            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                // Clear any state left by a pre-8.30h renderer. Configured play no longer activates
                // whole-hex fills or rims for a board choice or projected result.
                tile.SetHighlighted(false, null, null);

                // Milestone 8.35z63: a live Active effect's disk, on its OWN layer under
                // every mark and every piece. It is not the retired choice fill and never touches
                // `IsHighlighted` - see `HexTileView.SetActiveEffectAura` for why that separation is
                // load-bearing rather than tidy.
                var inAura = activeEffectAura.Contains(tile.Coordinate);
                tile.SetActiveEffectAura(
                    inAura,
                    inAura ? EnsureActiveEffectAuraMesh(tile.OuterRadius) : null,
                    inAura ? EnsureActiveEffectAuraMaterial() : null);
                if (inAura)
                {
                    litAuraTiles.Add(tile);
                }

                if (cornerHighlightTemplate != null)
                {
                    var kind = hoveredHeroCoordinates.Contains(tile.Coordinate)
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
                    var pointedAt = hoveredCoordinate.HasValue &&
                        hoveredCoordinate.Value.Equals(tile.Coordinate);
                    var outlineSide =
                        targetOutlineSides.TryGetValue(tile.Coordinate, out var side)
                            ? side
                            : BoardTargetOutlineSide.None;
                    // Milestone 8.62. A target's outline also thickens when the pointer is on the
                    // PIECE rather than on the sand around it, which is where a player actually puts
                    // it. Only the outline reads this: the six corner marks keep the hover language
                    // every shipped journey pins them with.
                    if (outlineSide != BoardTargetOutlineSide.None &&
                        hoveredHeroCoordinates.Contains(tile.Coordinate))
                    {
                        pointedAt = true;
                    }

                    tile.SetCornerHighlighted(
                        kind,
                        pointedAt,
                        defenseCoordinates.TryGetValue(tile.Coordinate, out var defenseKind)
                            ? defenseKind
                            : HexCornerHighlightKind.None,
                        outlineSide);
                }
            }

            RenderDirectionArrows(model);

            // Milestone 8.35z63. Lighting a space resets its aura to the space itself, so the
            // live offset is re-applied before this pass returns. Without it the one render that
            // matters most - the projection change that STARTS a slide, when the displacement is at
            // its largest - would draw the disk already arrived for a frame and then jump backwards to
            // meet the piece. It reads the transforms the kinetic layer last wrote and moves nothing.
            GlideActiveEffectAuras();
        }

        /// <summary>
        /// Milestone 8.46d. Six arrows around the hero a direction choice is asking, one per offered
        /// direction, each standing on the neighbouring space a click on it names.
        ///
        /// <para><b>Placed in the TILES' own frame rather than in world space.</b> A tile positions
        /// itself at <see cref="HexWorldLayout.ToWorld"/> in its parent's local coordinates, so an
        /// arrow placed the same way under the same parent lands exactly on the lattice however the
        /// board root itself is scaled, rotated or moved - and it lands there whether or not a tile
        /// happens to be drawn on that space, which is what keeps the six arrows a set of six.</para>
        ///
        /// <para>Nothing here decides which directions are offered or what answering one does: the
        /// model carries the candidates and each carries its own command. This is where they are
        /// drawn and nothing more.</para>
        /// </summary>
        private void RenderDirectionArrows(BoardInteractionViewModel model)
        {
            var candidates = model.DirectionCandidates;
            var chooserSpace = model.DirectionChooserSpace;
            if (candidates.Count == 0 || !chooserSpace.HasValue)
            {
                HideDirectionArrows();
                return;
            }

            // The chooser's own tile where there is one, so the arrows are placed in the frame the
            // hero is standing in; any drawn tile otherwise, since every tile of a board shares one.
            // `!= null` rather than `??` throughout: see BoardDefeatFadeDriver.Attach for why an
            // explicit comparison is the only one that asks Unity's own question.
            var reference = tiles.FirstOrDefault(tile =>
                tile != null && tile.Coordinate.Equals(chooserSpace.Value));
            if (reference == null)
            {
                reference = tiles.FirstOrDefault(tile => tile != null);
            }

            if (reference == null)
            {
                HideDirectionArrows();
                return;
            }

            var radius = reference.OuterRadius;
            var parent = reference.transform.parent;
            var chooserLocal = HexWorldLayout.ToWorld(chooserSpace.Value, radius);
            for (var index = 0; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (index >= directionArrows.Count)
                {
                    directionArrows.Add(HexDirectionArrow.Create(EnsureDirectionArrowTemplate(radius)));
                }

                var local = HexWorldLayout.ToWorld(candidate.Neighbour, radius);
                directionArrows[index].SetVisible(
                    candidate.Direction,
                    candidate.Neighbour,
                    parent,
                    local,
                    local - chooserLocal,
                    hoveredCoordinate.HasValue &&
                        hoveredCoordinate.Value.Equals(candidate.Neighbour));
            }

            HideDirectionArrows(candidates.Count);
        }

        private void HideDirectionArrows(int from = 0)
        {
            for (var index = from; index < directionArrows.Count; index++)
            {
                if (directionArrows[index] != null)
                {
                    directionArrows[index].SetHidden();
                }
            }
        }

        private HexDirectionArrowTemplate EnsureDirectionArrowTemplate(float outerRadius)
        {
            if (directionArrowTemplate == null)
            {
                directionArrowTemplate = new HexDirectionArrowTemplate(outerRadius);
            }

            return directionArrowTemplate;
        }

        /// <summary>
        /// The spaces one projected piece cue stands on. Milestone 9.21i: a hero cue naming a body is
        /// that body's space; one naming no body is every space the hero stands on (before this it was
        /// a <c>Single</c> read, which threw the moment a hero stood on two spaces).
        /// </summary>
        private static IEnumerable<GuardsDigital.Core.Board.HexCoordinate> CoordinatesOf(
            BoardInteractionViewModel model,
            BoardObjectReference target)
        {
            if (target.Kind == BoardObjectKind.Hero)
            {
                return model.Heroes
                    .Where(value => value.HeroId.Equals(target.HeroId) &&
                        (target.HeroBody < HeroOccupancy.FirstBody || value.Body == target.HeroBody))
                    .Select(value => value.Coordinate)
                    .ToArray();
            }

            if (target.Kind == BoardObjectKind.Minion)
            {
                var occupancy = model.Minions.Single(value => value.MinionId.Equals(target.MinionId));
                return new[] { occupancy.Coordinate };
            }

            if (target.Kind == BoardObjectKind.Token)
            {
                var token = model.PlacedTokens.Single(value => value.Id.Equals(target.TokenInstanceId));
                return new[] { token.Coordinate };
            }

            return Array.Empty<GuardsDigital.Core.Board.HexCoordinate>();
        }

        /// <summary>Milestone 9.21i. The row of one body of a hero, or default when it does not stand.</summary>
        private static HeroOccupancy FindHeroRow(BoardInteractionViewModel model, HeroId heroId, int body)
        {
            var rows = model.Heroes;
            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index].HeroId.Equals(heroId) && rows[index].Body == body) return rows[index];
            }

            return default;
        }

        private static bool StandsOnSeveralSpaces(BoardInteractionViewModel model, HeroId heroId)
        {
            var count = 0;
            var rows = model.Heroes;
            for (var index = 0; index < rows.Count; index++)
            {
                if (rows[index].HeroId.Equals(heroId) && ++count > 1) return true;
            }

            return false;
        }

        /// <summary>
        /// Milestone 9.21i. Draws a standee for every projected body that has none, from the standee
        /// of the same hero this board already draws (its team face, identity and size), with its own
        /// pointer selector bound to this board. A hero the board has no standee for at all stays
        /// undrawn, as before. Views are never destroyed: a body that leaves the board keeps its
        /// standee hidden for a respawn, a spawn, an Undo or a restore.
        /// </summary>
        private void EnsureHeroBodyViews(BoardInteractionViewModel model)
        {
            foreach (var row in model.Heroes)
            {
                PrimitiveHeroView template = null;
                var drawn = false;
                for (var index = 0; index < heroes.Count && !drawn; index++)
                {
                    var view = heroes[index];
                    if (view == null || !view.Occupancy.HeroId.Equals(row.HeroId)) continue;
                    drawn = view.Body == row.Body;
                    if (template == null || view.Body < template.Body) template = view;
                }

                if (drawn || template == null) continue;

                var piece = new GameObject($"{template.name} body {row.Body}");
                piece.transform.SetParent(template.transform.parent, false);
                var created = piece.AddComponent<PrimitiveHeroView>();
                created.ConfigureAsBodyOf(template, row);
                piece.AddComponent<GuardsDigital.Presentation.Interaction.PrimitiveHeroPointerSelector>()
                    .Configure(this);
                heroes.Add(created);
                HeroBodyViewCreated?.Invoke(created);
            }
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

        public void SetPointerHoveredHero(HeroId heroId) => SetPointerHoveredHero(heroId, 0);

        /// <summary>Milestone 9.21i. The standee under the pointer, by hero and body (0 = hero only).</summary>
        public void SetPointerHoveredHero(HeroId heroId, int body)
        {
            pointerHoveredHeroId = heroId;
            pointerHoveredHeroBody = heroId.IsDefined ? body : 0;
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

            actingHeroMarker.SetVisible(actingHero.transform.position, actingHero.SelectionRadius,
                actingHero.HasActiveEffectMark);
        }

        /// <summary>
        /// Milestone 9.21i. One faint ring per other standing body of the acting hero, drawn only
        /// while the bright ring stands on his settled body; pooled rather than destroyed.
        /// </summary>
        private void RenderFaintActingMarkers(IReadOnlyList<PrimitiveHeroView> others)
        {
            var count = others?.Count ?? 0;
            for (var index = 0; index < count; index++)
            {
                if (index >= faintActingMarkers.Count)
                {
                    faintActingMarkers.Add(ActingHeroMarker.Create(true));
                }

                var body = others[index];
                faintActingMarkers[index].SetVisible(
                    body.transform.position, body.SelectionRadius, body.HasActiveEffectMark);
            }

            for (var index = count; index < faintActingMarkers.Count; index++)
            {
                if (faintActingMarkers[index] != null) faintActingMarkers[index].SetHidden();
            }
        }

        private void ApplyHoveredHero()
        {
            var next = interfaceHoveredHeroId.IsDefined
                ? interfaceHoveredHeroId
                : pointerHoveredHeroId;
            var nextBody = interfaceHoveredHeroId.IsDefined ? 0 : pointerHoveredHeroBody;
            if (hoveredHeroId.Equals(next) && hoveredHeroBody == nextBody) return;
            var heroChanged = !hoveredHeroId.Equals(next);
            hoveredHeroId = next;
            hoveredHeroBody = nextBody;
            if (heroChanged) HeroHighlighted?.Invoke(next);
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
                    // Milestone 8.35z6: see the hero loop's identical comment above.
                    ReleaseMinionDefeatGhost(minion);
                    if (!ReferenceEquals(minion.Occupancy, state))
                    {
                        minion.Configure(state, minion.OuterRadius);
                    }

                    // Milestone 8.62: the same cue on the same terms as the hero loop's. A minion an
                    // attack can take is the target a player most often has to find on a crowded
                    // board.
                    ApplyPieceTargetCue(
                        minion, model, BoardObjectReference.ForMinion(state.MinionId));

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.SetTargetCue(
                        BoardTargetCueState.None,
                        BoardTargetConsequenceIntent.None);
                    // Milestone 8.35z6: see the hero loop's identical comment above.
                    if (minion.gameObject.activeSelf)
                    {
                        SpawnMinionDefeatGhost(minion);
                    }

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

                    // Milestone 8.62: a placed token is a piece like any other, and a card that can
                    // remove one names it the same way.
                    ApplyPieceTargetCue(token, model, BoardObjectReference.ForToken(boundId));
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

                ApplyPieceTargetCue(view, model, BoardObjectReference.ForToken(state.Id));
            }
        }

        /// <summary>Authority-owned obstacles are projected pieces, including Trinkets' single Turret.</summary>
        private void RenderObstacles(BoardInteractionViewModel model)
        {
            var pending = model.Obstacles.ToDictionary(value => value.ObstacleId);
            var free = new List<PlaceholderObstacleView>();
            foreach (var view in projectedObstacles)
            {
                if (pending.TryGetValue(view.Occupancy.ObstacleId, out var state))
                {
                    view.gameObject.SetActive(true);
                    if (!view.Occupancy.Equals(state))
                        view.Configure(state, view.OuterRadius);
                    pending.Remove(state.ObstacleId);
                }
                else
                {
                    view.gameObject.SetActive(false);
                    free.Add(view);
                }
            }

            var radius = tiles.Count == 0 ? HexWorldLayout.DefaultOuterRadius :
                tiles[0].OuterRadius;
            foreach (var state in model.Obstacles.Where(value => pending.ContainsKey(value.ObstacleId)))
            {
                PlaceholderObstacleView view;
                if (free.Count != 0)
                {
                    view = free[free.Count - 1];
                    free.RemoveAt(free.Count - 1);
                }
                else
                {
                    var piece = new GameObject("Projected Obstacle");
                    if (tiles.Count != 0) piece.transform.SetParent(tiles[0].transform.parent, false);
                    view = piece.AddComponent<PlaceholderObstacleView>();
                    projectedObstacles.Add(view);
                }
                view.Configure(state, radius);
                view.gameObject.SetActive(true);
                pending.Remove(state.ObstacleId);
            }
        }

        /// <summary>
        /// Milestone 8.62. Draws one piece's own projected target cue - the fitted ring and the
        /// translucent wash of its exact footprint - or clears it.
        ///
        /// <para>The state and the consequence are the PROJECTION's, never inferred here: a piece
        /// with no cue in <see cref="BoardInteractionViewModel.PieceTargetCues"/> is not a target and
        /// wears nothing. The three overloads exist because a hero, a minion and a placed token each
        /// own their own fitted ring and expose the same two-argument call.</para>
        /// </summary>
        private static void ApplyPieceTargetCue(
            GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView piece,
            BoardInteractionViewModel model,
            BoardObjectReference reference,
            int body)
        {
            var cue = Drawable(piece.CanWearATargetCue, CueFor(model, reference, body));
            piece.SetTargetCue(
                cue?.State ?? BoardTargetCueState.None,
                cue?.ConsequenceIntent ?? BoardTargetConsequenceIntent.None);
        }

        private static void ApplyPieceTargetCue(
            GuardsDigital.Presentation.Units.Minions.StandardMinionView piece,
            BoardInteractionViewModel model,
            BoardObjectReference reference)
        {
            var cue = Drawable(piece.CanWearATargetCue, CueFor(model, reference));
            piece.SetTargetCue(
                cue?.State ?? BoardTargetCueState.None,
                cue?.ConsequenceIntent ?? BoardTargetConsequenceIntent.None);
        }

        private static void ApplyPieceTargetCue(
            PlacedTokenView piece,
            BoardInteractionViewModel model,
            BoardObjectReference reference)
        {
            var cue = Drawable(piece.CanWearATargetCue, CueFor(model, reference));
            piece.SetTargetCue(
                cue?.State ?? BoardTargetCueState.None,
                cue?.ConsequenceIntent ?? BoardTargetConsequenceIntent.None);
        }

        /// <summary>
        /// Milestone 8.62's hex-outline variant. For every space that carries a PIECE target cue,
        /// which side that piece stands on relative to the seat that is choosing.
        ///
        /// <para>The user: "Let's try how it looks when we remove the outline of the token, just
        /// outline the hex", and "Keep the colors, blood red on enemy, green on ally". So the colour
        /// is a fact about the TARGET, not about the operation - which is a different rule from the
        /// shipped intent palette, and differs from it exactly twice: an effect aimed at an enemy
        /// (intent green, side red) and any consequence aimed at a friendly piece.</para>
        ///
        /// <para><b>Read entirely from what the view already holds.</b> The choosing side is the
        /// focused hero's own view's <c>TeamSide</c>; a target hero's side is its view's, a target
        /// minion's is its occupancy's. The PROJECTION is untouched - no new field, no presenter
        /// change - which is what lets either treatment ship from one constant. A placed token has
        /// no side of its own, so it keeps the operation's intent.</para>
        /// </summary>
        private Dictionary<GuardsDigital.Core.Board.HexCoordinate, BoardTargetOutlineSide>
            TargetOutlineSides(BoardInteractionViewModel model)
        {
            var sides =
                new Dictionary<GuardsDigital.Core.Board.HexCoordinate, BoardTargetOutlineSide>();
            if (!BoardTargetCuePresentation.MarksTheTargetTileBySide) return sides;

            var choosingSide = ChoosingTeamSide(model);
            foreach (var cue in model.PieceTargetCues)
            {
                if (cue.State == BoardTargetCueState.None) continue;

                var side = BoardTargetCuePresentation.ColourByTargetSide
                    ? SideOf(model, cue, choosingSide)
                    : IntentSide(cue);
                if (side == BoardTargetOutlineSide.None) continue;

                // A space that is both an enemy target and a friendly one cannot happen - one piece
                // stands on it - but a later cue wins deterministically rather than by dictionary
                // order.
                foreach (var coordinate in CoordinatesOf(model, cue.Target))
                {
                    sides[coordinate] = side;
                }
            }

            return sides;
        }

        private static BoardTargetOutlineSide IntentSide(PieceTargetCueViewModel cue) =>
            cue.ConsequenceIntent == BoardTargetConsequenceIntent.Attack ||
            cue.ConsequenceIntent == BoardTargetConsequenceIntent.Removal
                ? BoardTargetOutlineSide.Enemy
                : BoardTargetOutlineSide.Friendly;

        private BoardTargetOutlineSide SideOf(
            BoardInteractionViewModel model,
            PieceTargetCueViewModel cue,
            TeamSide? choosingSide)
        {
            if (!choosingSide.HasValue) return IntentSide(cue);

            if (cue.Target.HeroId.IsDefined)
            {
                var view = HeroViewFor(cue.Target.HeroId);
                if (view == null) return IntentSide(cue);
                return view.TeamSide == choosingSide.Value
                    ? BoardTargetOutlineSide.Friendly
                    : BoardTargetOutlineSide.Enemy;
            }

            if (cue.Target.MinionId.IsDefined)
            {
                for (var index = 0; index < model.Minions.Count; index++)
                {
                    if (!model.Minions[index].MinionId.Equals(cue.Target.MinionId)) continue;
                    return model.Minions[index].TeamSide == choosingSide.Value
                        ? BoardTargetOutlineSide.Friendly
                        : BoardTargetOutlineSide.Enemy;
                }
            }

            return IntentSide(cue);
        }

        private TeamSide? ChoosingTeamSide(BoardInteractionViewModel model)
        {
            if (!model.FocusedHeroId.IsDefined) return null;
            var view = HeroViewFor(model.FocusedHeroId);
            return view == null ? (TeamSide?)null : view.TeamSide;
        }

        private GuardsDigital.Presentation.Units.Heroes.PrimitiveHeroView HeroViewFor(HeroId heroId)
        {
            for (var index = 0; index < heroes.Count; index++)
            {
                var hero = heroes[index];
                if (hero != null && hero.Occupancy.HeroId.Equals(heroId)) return hero;
            }

            return null;
        }

        /// <summary>
        /// Milestone 8.62. The cue a piece can actually be drawn with.
        ///
        /// <para>A fitted ring is measured against the piece's own visible footprint, and a piece can
        /// legitimately have none to measure - a minion whose sprite the art catalog has not supplied,
        /// a view configured outside a loaded scene. <c>BoardTargetRing.SetCue</c> refuses such a
        /// footprint loudly rather than fitting a ring to nothing, so a piece that cannot wear the cue
        /// is left to the six corner marks on its space instead of taking the whole render pass down
        /// with it. The PROJECTION is untouched: it still names the piece, and every other reader of
        /// it - the corner marks, the pointer, the log - is unaffected.</para>
        /// </summary>
        private static PieceTargetCueViewModel? Drawable(
            bool canWearACue, PieceTargetCueViewModel? cue) =>
            canWearACue && BoardTargetCuePresentation.DrawsTheRingOnThePiece ? cue : null;

        private static PieceTargetCueViewModel? CueFor(
            BoardInteractionViewModel model, BoardObjectReference reference, int body = 0)
        {
            var cues = model.PieceTargetCues;
            for (var index = 0; index < cues.Count; index++)
            {
                // Milestone 9.21i. A hero cue naming a body is that body's standee's alone; one naming
                // no body is every standee of the hero (the equality is at hero grain, RZ-Q2).
                if (cues[index].Target.Equals(reference) &&
                    (cues[index].Target.Kind != BoardObjectKind.Hero ||
                     cues[index].Target.HeroBody < HeroOccupancy.FirstBody ||
                     body < HeroOccupancy.FirstBody ||
                     cues[index].Target.HeroBody == body))
                {
                    return cues[index];
                }
            }

            return null;
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

        /// <summary>
        /// Milestone 9.21i. A click on one standee, carrying the body it draws: the body-aware handler
        /// answers where one is bound (the acting-body pick, an Attack on a body), and the hero handler
        /// otherwise, exactly as before.
        /// </summary>
        public bool TryForwardHeroIntent(HeroId heroId, int body)
        {
            if (heroBodyIntentHandler != null && body >= HeroOccupancy.FirstBody)
            {
                return heroBodyIntentHandler(heroId, body);
            }

            return TryForwardHeroIntent(heroId);
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

        /// <summary>
        /// Milestone 8.35z48. Points the tray at a hero and nothing else: it submits no command,
        /// changes no authoritative state, and is asked only after every authority-backed question on
        /// the same ray has declined. A board with no handler bound answers false, which is the
        /// honest answer to "can this click at least show me something".
        /// </summary>
        public bool TryForwardHeroViewIntent(HeroId heroId) =>
            heroViewIntentHandler != null && heroViewIntentHandler(heroId);

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

        // ---- Milestone 8.35z6: Defeat's shrink-and-fade, delivered pooling-safely. See
        // DefeatFadeGhostView's own doc comment for why this exists and why it never touches
        // `heroes`/`minions` above.

        private void SpawnHeroDefeatGhost(PrimitiveHeroView hero)
        {
            if (heroDefeatGhosts.ContainsKey(hero))
            {
                return;
            }

            var meshFilter = hero.GetComponent<MeshFilter>();
            var meshRenderer = hero.GetComponent<MeshRenderer>();
            if (meshFilter == null || meshRenderer == null || meshFilter.sharedMesh == null)
            {
                // Degrades rather than faults, matching KineticCueUnityView's own rule: a missing
                // visual costs the flourish and nothing else.
                return;
            }

            var ghost = Pop(freeHeroDefeatGhosts) ?? DefeatFadeGhostView.CreateHeroGhost();
            ghost.ConfigureHero(
                hero.transform,
                meshFilter.sharedMesh,
                meshRenderer.sharedMaterials,
                meshRenderer.shadowCastingMode,
                meshRenderer.receiveShadows);
            ghost.Begin(DefeatFadeGhostView.DefaultFadeSeconds);
            heroDefeatGhosts[hero] = ghost;
        }

        private void ReleaseHeroDefeatGhost(PrimitiveHeroView hero)
        {
            if (!heroDefeatGhosts.TryGetValue(hero, out var ghost))
            {
                return;
            }

            ghost.CancelImmediately();
            freeHeroDefeatGhosts.Add(ghost);
            heroDefeatGhosts.Remove(hero);
        }

        private void SpawnMinionDefeatGhost(StandardMinionView minion)
        {
            if (minionDefeatGhosts.ContainsKey(minion))
            {
                return;
            }

            var spriteRenderer = minion.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return;
            }

            var ghost = Pop(freeMinionDefeatGhosts) ?? DefeatFadeGhostView.CreateMinionGhost();
            ghost.ConfigureMinion(
                minion.transform, spriteRenderer.sprite, spriteRenderer.color, spriteRenderer.sortingOrder);
            ghost.Begin(DefeatFadeGhostView.DefaultFadeSeconds);
            minionDefeatGhosts[minion] = ghost;
        }

        private void ReleaseMinionDefeatGhost(StandardMinionView minion)
        {
            if (!minionDefeatGhosts.TryGetValue(minion, out var ghost))
            {
                return;
            }

            ghost.CancelImmediately();
            freeMinionDefeatGhosts.Add(ghost);
            minionDefeatGhosts.Remove(minion);
        }

        private static DefeatFadeGhostView Pop(List<DefeatFadeGhostView> free)
        {
            if (free.Count == 0)
            {
                return null;
            }

            var last = free.Count - 1;
            var ghost = free[last];
            free.RemoveAt(last);
            return ghost;
        }

        /// <summary>How many Defeat fade ghosts are currently fading, for a test with no frame loop of
        /// its own to assert against.</summary>
        public int ActiveDefeatFadeCount => heroDefeatGhosts.Count + minionDefeatGhosts.Count;

        /// <summary>Presentation-only hold while the attack camera travels to its strike.
        /// Authoritative pieces still deactivate immediately; only their non-interactive echoes wait.</summary>
        public Func<bool> DeferDefeatFades { get; set; }

        /// <summary>The in-flight ghost for a hero, or null when it is not currently fading. Exposed for
        /// tests; the driver never needs it, since <see cref="AdvanceDefeatFades"/> ticks every ghost by
        /// itself.</summary>
        public DefeatFadeGhostView HeroDefeatGhostFor(PrimitiveHeroView hero) =>
            heroDefeatGhosts.TryGetValue(hero, out var ghost) ? ghost : null;

        /// <summary>The in-flight ghost for a minion, or null when it is not currently fading.</summary>
        public DefeatFadeGhostView MinionDefeatGhostFor(StandardMinionView minion) =>
            minionDefeatGhosts.TryGetValue(minion, out var ghost) ? ghost : null;

        /// <summary>
        /// Advances every in-flight Defeat fade ghost by one tick. Called once a frame by
        /// <see cref="BoardDefeatFadeDriver"/>, entirely independent of the kinetic cue frame loop -
        /// also callable directly by a test to drive the fade deterministically instead of waiting on
        /// real frames.
        /// </summary>
        public void AdvanceDefeatFades(float deltaSeconds)
        {
            if (DeferDefeatFades?.Invoke() == true) return;
            AdvanceGhosts(heroDefeatGhosts, freeHeroDefeatGhosts, deltaSeconds);
            AdvanceGhosts(minionDefeatGhosts, freeMinionDefeatGhosts, deltaSeconds);
        }

        private static void AdvanceGhosts<TKey>(
            Dictionary<TKey, DefeatFadeGhostView> active,
            List<DefeatFadeGhostView> free,
            float deltaSeconds)
        {
            if (active.Count == 0)
            {
                return;
            }

            List<TKey> finished = null;
            foreach (var pair in active)
            {
                if (pair.Value.Advance(deltaSeconds))
                {
                    (finished ?? (finished = new List<TKey>())).Add(pair.Key);
                }
            }

            if (finished == null)
            {
                return;
            }

            for (var index = 0; index < finished.Count; index++)
            {
                free.Add(active[finished[index]]);
                active.Remove(finished[index]);
            }
        }
    }

    /// <summary>
    /// Milestone 8.35z6. The one per-frame tick the Defeat fade ghosts need, isolated exactly
    /// like <see cref="KineticCueDriver"/> and entirely independent of it: the board's own fade never
    /// reaches into the kinetic layer, and the kinetic layer never reaches into the board's pool to
    /// deliver it. A throw inside <c>Update</c> repeats every frame, so it is caught, reported once, and
    /// after a small budget of failures this component disables itself - the board falls back to exactly
    /// the synchronous hide-on-removal it always had, with no fade, which is what this milestone found
    /// integrated behind it anyway.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BoardDefeatFadeDriver : MonoBehaviour
    {
        public const string DriverObjectName = "Board Defeat Fade Driver";

        /// <summary>How many ticks may throw before the driver takes itself out of the frame loop.</summary>
        public const int FailureBudget = 3;

        private BoardInteractionUnityView board;
        private int failures;

        public static BoardDefeatFadeDriver Attach(GameObject host, BoardInteractionUnityView board)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (board == null) throw new ArgumentNullException(nameof(board));
            // Same `??` trap HexTileView.SetActiveEffectAura records, corrected for consistency:
            // `??` bypasses Unity's null overload, an explicit comparison does not.
            var driver = host.GetComponent<BoardDefeatFadeDriver>();
            if (driver == null)
            {
                driver = host.AddComponent<BoardDefeatFadeDriver>();
            }

            driver.board = board;
            driver.failures = 0;
            driver.enabled = true;
            return driver;
        }

        /// <summary>How many ticks have thrown. Zero on a healthy driver, and asserted by a fixture.</summary>
        public int FailureCount => failures;

        private void Update()
        {
            if (board == null)
            {
                return;
            }

            try
            {
                board.AdvanceDefeatFades(Time.deltaTime);
            }
            catch (Exception error)
            {
                failures++;
                Debug.LogWarning(
                    "Defeat fade ghost tick failed (" + failures + "/" + FailureBudget + "): " + error);
                if (failures >= FailureBudget)
                {
                    Debug.LogWarning(
                        "Defeat fade ghosts are disabled for this session; removal keeps the synchronous " +
                        "hide it always had, with no fade.");
                    enabled = false;
                }
            }
        }
    }
}
