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
        private readonly Material highlightMaterial;
        private readonly Material resultingSpaceMaterial;
        private readonly Material highlightRimMaterial;
        private readonly Material resultingSpaceRimMaterial;
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
            Material resultingSpaceRimMaterial = null)
        {
            this.heroes = Array.AsReadOnly((heroes ?? throw new ArgumentNullException(nameof(heroes)))
                .Where(value => value != null).Distinct().ToArray());
            this.minions = Array.AsReadOnly((minions ?? Enumerable.Empty<StandardMinionView>())
                .Where(value => value != null).Distinct().ToArray());
            this.tiles = Array.AsReadOnly((tiles ?? throw new ArgumentNullException(nameof(tiles)))
                .Where(value => value != null).Distinct().ToArray());
            this.highlightMaterial = highlightMaterial ?? throw new ArgumentNullException(nameof(highlightMaterial));
            // Milestone 6.5c: without its own material the resulting space cannot be drawn at all,
            // because painting a prediction in the destination color would say "click here".
            this.resultingSpaceMaterial = resultingSpaceMaterial;
            // Milestone 8.11a: the perimeter each highlight draws around its space, in the highlight's
            // own colour. Optional, because a caller that supplies no rim gets the fill-only highlight
            // this had before - which is what every fixture below the configured scene still uses.
            this.highlightRimMaterial = highlightRimMaterial;
            this.resultingSpaceRimMaterial = resultingSpaceRimMaterial;
            // Milestone 6.2d: where a view for a newly placed token comes from. A token has no
            // declared roster - its identity is minted when an effect places it - so unlike heroes and
            // minions its views cannot all exist before the match does. Left unbound, a projected
            // token faults rather than being quietly left undrawn.
            this.tokenViewFactory = tokenViewFactory;
        }

        public BoardInteractionViewModel Model { get; private set; }
        public IReadOnlyList<StandardMinionView> MinionViews => minions;

        /// <summary>Every token view this board has ever built, active or pooled.</summary>
        public IReadOnlyList<PlacedTokenView> TokenViews => tokens;

        /// <summary>The tiles this board paints, so a caller can read what is currently lit.</summary>
        public IReadOnlyList<HexTileView> TileViews => tiles;

        public event Action<HeroId> HeroSubmitted;
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

        public void Render(BoardInteractionViewModel model)
        {
            if (ReferenceEquals(Model, model))
            {
                return;
            }

            Model = model ?? throw new ArgumentNullException(nameof(model));
            var heroChoiceCues = model.HeroChoiceCues.ToDictionary(value => value.HeroId);
            foreach (var hero in heroes)
            {
                var occupancy = model.Heroes.SingleOrDefault(value =>
                    value.HeroId.Equals(hero.Occupancy.HeroId));
                if (!occupancy.HeroId.IsDefined)
                {
                    hero.SetChoiceCue(MinionChoiceCueKind.None);
                    hero.gameObject.SetActive(false);
                    continue;
                }

                // Hero views are retained across removal for Respawn, Undo, and restore, just as
                // minion views are retained across removal. Projection presence owns visibility;
                // an absent hero must not remain as a stale selectable board object.
                hero.Configure(occupancy, hero.OuterRadius);
                hero.gameObject.SetActive(true);

                // Milestone 6.5c: an effect-choice target is addressed by its space, so whichever
                // unit stands there wears the cue. The hero path exists because a Push may name one.
                hero.SetChoiceCue(
                    heroChoiceCues.TryGetValue(occupancy.HeroId, out var heroChoiceCue)
                        ? heroChoiceCue.Kind
                        : model.EffectChoiceTargetSpace.HasValue &&
                        occupancy.Coordinate.Equals(model.EffectChoiceTargetSpace.Value)
                        ? MinionChoiceCueKind.Highlighted
                        : MinionChoiceCueKind.None);
            }

            RenderMinions(model);
            RenderTokens(model);

            // Milestone 8.11a, at the user's request, and settled by what is actually being chosen
            // rather than by how many there are.
            //
            // <b>Choosing a hex lights the hex.</b> A movement or fast-travel range, a correction
            // destination, a respawn space, an empty effect-choice candidate - the answer is the space
            // itself, so the space is what changes. Lighting is also the only thing that can show a
            // region as a region.
            //
            // <b>Choosing a piece marks the piece.</b> A hero or minion an Attack may take, an effect
            // candidate that names a unit or a token - the answer is the thing standing there, not the
            // ground under it, and lighting the ground says the wrong thing. It also could not be made
            // to read: a tint has to contrast with pale sand, mid brown, jungle and near-black rock at
            // once and no colour does, while the Minion Battle's marker carries its own dark backing
            // and does not have to.
            var markedCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>();
            markedCoordinates.UnionWith(model.EffectChoiceTargetSpaces);
            foreach (var targetId in model.AttackTargets)
            {
                var target = model.Heroes.Single(value => value.HeroId.Equals(targetId));
                markedCoordinates.Add(target.Coordinate);
            }
            foreach (var targetId in model.MinionAttackTargets)
            {
                var target = model.Minions.Single(value => value.MinionId.Equals(targetId));
                markedCoordinates.Add(target.Coordinate);
            }

            var targetCoordinates = new HashSet<GuardsDigital.Core.Board.HexCoordinate>(model.MovementDestinations);
            targetCoordinates.UnionWith(model.FastTravelDestinations);
            // Milestone 6.4b highlighted every correction route endpoint and Place space at once;
            // milestone 6.2d lights the one whose control is being pointed at, because each of them
            // carries its own control and the board was saying nothing the controls did not already.
            if (model.CorrectionDestination.HasValue)
            {
                targetCoordinates.Add(model.CorrectionDestination.Value);
            }
            // Milestone 6.5c: an empty effect-choice candidate is a destination like any other, and
            // is the whole surface for that candidate - no control is drawn for it anywhere.
            targetCoordinates.UnionWith(model.EffectChoiceDestinations);
            if (model.HeroRespawnHighlight.HasValue)
            {
                targetCoordinates.Add(model.HeroRespawnHighlight.Value);
            }
            // Milestone 8.11a: and every space the Respawn offers, not only the one a control was
            // pointing at, now that the board is where the choice is made.
            targetCoordinates.UnionWith(model.HeroRespawnDestinations);
            targetCoordinates.ExceptWith(markedCoordinates);

            // The predicted landing space is drawn in its own color and is never selectable, so it is
            // resolved after the selectable set and can never be added to it.
            var resultingSpace = resultingSpaceMaterial == null
                ? null
                : model.EffectChoiceResultingSpace;
            foreach (var tile in tiles.Where(value => value.IsPlayable))
            {
                if (resultingSpace.HasValue && tile.Coordinate.Equals(resultingSpace.Value) &&
                    !targetCoordinates.Contains(tile.Coordinate))
                {
                    // The prediction is a space rather than a piece - where this Push would end - so it
                    // is lit like one, in its own colour and never selectable.
                    tile.SetHighlighted(true, resultingSpaceMaterial, resultingSpaceRimMaterial);
                    tile.SetChoiceCue(MinionChoiceCueKind.None);
                    continue;
                }

                // Milestone 8.11a, at the user's request: a marked space wears the marker and nothing
                // else. Lighting it as well said the same thing twice, and the fill was the half that
                // read worst - it is the half that has to compete with whatever the board is painted.
                var marked = markedCoordinates.Contains(tile.Coordinate);
                var highlighted = !marked && targetCoordinates.Contains(tile.Coordinate);
                tile.SetHighlighted(
                    highlighted,
                    highlighted ? highlightMaterial : null,
                    highlighted ? highlightRimMaterial : null);
                tile.SetChoiceCue(marked ? MinionChoiceCueKind.Highlighted : MinionChoiceCueKind.None);
            }
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
            var defenseCues = model.MinionDefenseCues.ToDictionary(value => value.MinionId, value => value.Amount);
            var choiceCues = model.MinionChoiceCues.ToDictionary(value => value.MinionId);
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

                    minion.SetDefenseContributionCue(
                        defenseCues.TryGetValue(boundId, out var amount) ? amount : 0);
                    minion.SetChoiceCue(choiceCues.TryGetValue(boundId, out var choiceCue)
                        ? choiceCue.Kind
                        : model.EffectChoiceTargetSpace.HasValue &&
                            state.Coordinate.Equals(model.EffectChoiceTargetSpace.Value)
                            ? MinionChoiceCueKind.Highlighted
                            : MinionChoiceCueKind.None);

                    projectedMinions.Remove(state.MinionId);
                }
                else
                {
                    minion.SetDefenseContributionCue(0);
                    minion.SetChoiceCue(MinionChoiceCueKind.None);
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
            // Milestone 6.3a: which drawn pieces a control currently names. A cue is addressed by the
            // token's own identity rather than by its space, so a released view can never keep one.
            var choiceCues = model.TokenChoiceCues.ToDictionary(value => value.TokenId);
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

                    token.SetChoiceCue(choiceCues.TryGetValue(boundId, out var choiceCue)
                        ? choiceCue.Kind
                        : MinionChoiceCueKind.None);
                    projectedTokens.Remove(boundId);
                }
                else
                {
                    token.SetChoiceCue(MinionChoiceCueKind.None);
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
                    view.Configure(state, view.OuterRadius);
                }
                else
                {
                    view = CreateTokenView(state);
                }

                view.SetChoiceCue(choiceCues.TryGetValue(state.Id, out var newCue)
                    ? newCue.Kind
                    : MinionChoiceCueKind.None);
                view.gameObject.SetActive(true);
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
