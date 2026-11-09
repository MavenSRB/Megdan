using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35y. Plays the kinetic cues on the real board and HUD.
    ///
    /// <para><b>The one discipline this whole file follows.</b> Every frame it recomputes each piece's
    /// AUTHORITATIVE placement from the piece's own projected occupancy - the same arithmetic
    /// <c>PrimitiveHeroView.ApplyLayout</c> and <c>StandardMinionView.Configure</c> use - and then adds
    /// the kinetic offset to it. It never integrates, never remembers a position, and never moves a
    /// piece relative to where it currently is. So a kinetic that is interrupted, a projection that
    /// changes mid-slide, an undo, a resync and a restore all resolve to authoritative placement on the
    /// very next frame with no unwind step, and a frame with nothing playing writes exactly what the
    /// board view already wrote.</para>
    ///
    /// <para><b>It degrades rather than fails.</b> Anything it cannot find - a missing camera, a HUD
    /// with no root, a piece with no view, an overlay texture that was never prepared - costs the
    /// flourish and nothing else. <see cref="KineticCueDriver"/> additionally isolates the whole tick,
    /// because a per-frame throw during a playtest is far worse than a board that does not smack.</para>
    /// </summary>
    public sealed class KineticCueUnityView : IKineticCueView
    {
        /// <summary>The furthest a lunge displaces a piece, in board units. About half a hex.</summary>
        public const float MaxLungeWorldDistance = 0.60f;
        public const float StandeeLungeWorldDistance = 0.30f;
        public const float StandeeHitBumpWorldDistance = 0.12f;

        /// <summary>How much a struck piece swells at the peak of its flash.</summary>
        public const float FlashScalePunch = 0.14f;

        /// <summary>
        /// The most awaited outlines lit at once. W-014 removes hand cards from this animation
        /// family, leaving at most the six hero rails; two spare slots retain the bounded headroom.
        /// </summary>
        public const int MaxBreathingShineElements = 8;

        private const string OverlayName = "gd-kinetic-overlay";
        // Milestone 8.35z9 retired `ShineElementName = "gd-card-shine"` from here: that name belongs
        // to milestone 8.30d's prepared printed-artwork overlay, which this file mistook for the
        // awaited-answer shine for four milestones. The breath asks for
        // MatchHudTheme.AwaitedShineClass, which is the marker the awaited treatments actually wear.
        //
        // Milestone 8.35z39 retired `SweepName = "gd-kinetic-sweep"` with the travelling
        // light it named. Nothing is parented into a shine host any more; the breath writes the
        // host's own border colour, so there is no element to name, create, find or clear.

        private readonly BoardInteractionUnityView board;
        private readonly ConfiguredMatchHudRenderer hud;
        private readonly Func<Camera> cameraSource;
        private readonly KineticPlayback playback = new KineticPlayback();
        private readonly List<PieceRestore> restorations = new List<PieceRestore>();
        private VisualElement overlay;
        private KineticCueViewModel model = KineticCueViewModel.Empty;

        // ---- Milestone 8.35z11 (the TWENTIETH AMENDMENT). Three additions, each owning its own
        // file: the conductor paces what used to all start at once, the flight layer carries the card
        // and coin journeys the board cannot, and the ambient sweep eases panels into existence.
        private readonly KineticConductor conductor = new KineticConductor();
        private readonly KineticFlightLayer flights;
        private readonly HudAmbientLife ambient = new HudAmbientLife();

        public KineticCueUnityView(
            BoardInteractionUnityView board,
            ConfiguredMatchHudRenderer hud = null,
            Func<Camera> cameraSource = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hud = hud;
            this.cameraSource = cameraSource ?? (() => Camera.main);
            flights = new KineticFlightLayer(
                () => this.hud?.RootElement, this.cameraSource, BoardPositionOfHero);
        }

        /// <summary>The clock, exposed so a Play Mode case can assert what is playing.</summary>
        public KineticPlayback Playback => playback;

        /// <summary>Presentation-only strike staging while the spotlight has not released its hit.</summary>
        public Func<KineticSubject, HexCoordinate, HexCoordinate?> WaitingStrikeOrigin { get; set; }

        /// <summary>
        /// Raised after a conducted kinetic scene has entered playback. Presentation that must wait
        /// for motion to genuinely start uses this boundary rather than equating queue release with
        /// animation start.
        /// </summary>
        public event Action SceneStarted;

        /// <summary>
        /// Milestone 8.35z11. The queue, exposed for the same reason the clock is: a Play Mode
        /// case asserting that six reveals stage one at a time has to be able to see the queue.
        /// </summary>
        public KineticConductor Conductor => conductor;

        /// <summary>The HUD journeys currently drawing.</summary>
        public KineticFlightLayer Flights => flights;

        /// <summary>The screen-wide enter easing, exposed so a case can count what it eased.</summary>
        public HudAmbientLife Ambient => ambient;

        /// <summary>The model most recently rendered.</summary>
        public KineticCueViewModel Model => model;

        public void Render(KineticCueViewModel viewModel)
        {
            model = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            // Milestone 8.35z11. The screen-wide enter easing runs HERE rather than in the frame
            // loop, because a projection change is exactly when the HUD rebuilds and therefore the only
            // moment anything can be newly on screen. It is the same cadence the HUD already pays for.
            ambient.Sweep(hud?.RootElement);

            if (model.SnapToAuthoritative)
            {
                // A first sight of the match, or a log that went backwards. Everything stops, every
                // piece is put back, every queued moment is dropped and every flight is erased.
                conductor.CancelAll();
                flights.CancelAll();
                playback.CancelAll();
                RestoreEveryPiece();
                ambient.Reset();
                return;
            }

            // The cues are QUEUED now rather than started. See KineticConductor for why deferring them
            // is safe, and for the one case - a stale slide - that it is not, which ApplyHero checks.
            conductor.Enqueue(model.Scenes);
        }

        /// <summary>
        /// One frame of kinetics. Called by <see cref="KineticCueDriver"/>, or directly by a test that
        /// wants to advance the clock deterministically instead of waiting on frames.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            var started = conductor.Advance(deltaSeconds);
            if (started != null)
            {
                // Each family to the layer that can draw it. See KineticCueFamilies for why handing
                // the piece clock a card's journey would leave it reporting a motion it never animated.
                playback.Begin(KineticCueFamilies.BoardCuesOf(started));
                flights.Begin(started, model.ViewerHeroId);
                SceneStarted?.Invoke();
            }

            playback.Advance(deltaSeconds);
            flights.Advance(deltaSeconds);
            ApplyPieceMotions();
            // Player-coloured acting ripples own their animation and Director settings.
            ApplyShineBreath();
        }

        // ---- Pieces.

        private void ApplyPieceMotions()
        {
            var touched = false;
            var heroes = board.HeroViews;
            for (var index = 0; index < heroes.Count; index++)
            {
                touched |= ApplyHero(heroes[index]);
            }

            var minions = board.MinionViews;
            for (var index = 0; index < minions.Count; index++)
            {
                touched |= ApplyMinion(minions[index]);
            }

            if (!touched && restorations.Count > 0)
            {
                RestoreEveryPiece();
            }

            // Milestone 8.35z63. Last, because it READS what the loops above just wrote: a
            // carrier's disk is offset by the displacement its own piece is currently carrying. It
            // starts no clock and knows no duration - see BoardInteractionUnityView.GlideActiveEffectAuras
            // - so this remains the one place piece motion is decided, and a frame with nothing playing
            // writes a zero offset exactly as it writes an authoritative position.
            board.GlideActiveEffectAuras();
        }

        private bool ApplyHero(PrimitiveHeroView hero)
        {
            if (hero == null)
            {
                return false;
            }

            var subject = KineticSubject.ForHero(hero.Occupancy.HeroId);
            var motion = WithoutStaleFlash(subject, hero.Coordinate, playback.MotionFor(subject));
            var standing = hero.Standee != null && hero.Standee.IsStanding;
            var reaction = StandeeReaction(subject, motion, AuthoritativeHeroPosition(hero), hero.OuterRadius);
            hero.Standee?.SetAttackLean(WorldDirection(hero.transform, reaction), cameraSource());
            if (motion.IsStill)
            {
                var waiting = WaitingStrikeOrigin?.Invoke(subject, hero.Coordinate) ??
                    conductor.WaitingTravelOriginFor(subject, hero.Coordinate);
                if (waiting.HasValue)
                {
                    Remember(hero.transform, hero.gameObject, null);
                    hero.transform.localPosition = HexWorldLayout.ToWorld(waiting.Value, hero.OuterRadius) +
                        Vector3.up * hero.VerticalOffset;
                    return true;
                }
                Restore(hero.transform);
                return false;
            }

            Remember(hero.transform, hero.gameObject, null);
            var origin = AuthoritativeHeroPosition(hero);
            hero.transform.localPosition =
                origin + Displacement(subject, motion, origin, hero.OuterRadius, hero.Coordinate,
                    standing ? StandeeLungeWorldDistance : MaxLungeWorldDistance) +
                (standing ? HitBump(subject, motion, origin, hero.OuterRadius) : Vector3.zero);
            ApplyScaleAndVisibility(hero.transform, hero.gameObject, motion, standing);
            ApplyHeroTint(hero, motion);
            return true;
        }

        private bool ApplyMinion(StandardMinionView minion)
        {
            if (minion == null || minion.Occupancy == null)
            {
                return false;
            }

            var subject = KineticSubject.ForMinion(minion.Occupancy.MinionId);
            var motion = WithoutStaleFlash(
                subject, minion.Occupancy.Coordinate, playback.MotionFor(subject));
            var standing = minion.Standee != null && minion.Standee.IsStanding;
            var reaction = StandeeReaction(subject, motion, AuthoritativeMinionPosition(minion), minion.OuterRadius);
            minion.Standee?.SetAttackLean(WorldDirection(minion.transform, reaction), cameraSource());
            if (motion.IsStill)
            {
                Restore(minion.transform);
                return false;
            }

            var renderer = minion.GetComponent<SpriteRenderer>();
            Remember(minion.transform, minion.gameObject, renderer);
            var origin = AuthoritativeMinionPosition(minion);
            minion.transform.localPosition =
                origin + Displacement(
                    subject, motion, origin, minion.OuterRadius, minion.Occupancy.Coordinate,
                    standing ? StandeeLungeWorldDistance : MaxLungeWorldDistance) +
                (standing ? HitBump(subject, motion, origin, minion.OuterRadius) : Vector3.zero);
            ApplyScaleAndVisibility(minion.transform, minion.gameObject, motion, standing);
            if (renderer != null)
            {
                var baseColor = BaseColorOf(minion.transform, renderer);
                var lit = Color.Lerp(baseColor, MatchHudTheme.ShineRim, motion.FlashStrength);
                renderer.color = new Color(lit.r, lit.g, lit.b, baseColor.a * motion.Opacity);
            }

            return true;
        }

        /// <summary>
        /// The kinetic displacement for one piece: how far back along its route it still is, plus how
        /// far it is leaning into what it struck. Both are computed against the piece's authoritative
        /// placement, never accumulated.
        /// </summary>
        private Vector3 Displacement(
            KineticSubject subject, KineticPieceMotion motion, Vector3 authoritative, float radius,
            HexCoordinate current, float maxLunge)
        {
            var displacement = Vector3.zero;

            if (motion.HasRouteTravel)
            {
                var route = playback.RouteFor(subject);

                // Milestone 8.35z11. THE GUARD THAT MAKES A QUEUED SLIDE SAFE. The offset below
                // assumes the projection stands this piece at the route's END. Before the conductor
                // that was true by construction, because a slide started on the very projection that
                // recorded it. A queued slide can now reach the screen a moment or two later, and if
                // the piece moved again in between, "current position plus (part-way minus arrival)"
                // is a position on no path the piece ever took. So a slide whose route no longer ends
                // where the piece stands travels nothing at all, and the piece is simply drawn where
                // the projection puts it - the end state it was always going to reach.
                if (!KineticRouteFreshness.IsRouteStillCurrent(route, current))
                {
                    route = null;
                }

                if (route != null && route.Count >= 2)
                {
                    // The projection already stands the piece at the route's end, so the slide draws
                    // it at the route point for the current progress and offsets by the difference.
                    var travelled = RoutePoint(route, motion.RouteProgress, radius);
                    var arrival = RoutePoint(route, 1f, radius);
                    displacement += travelled - arrival;
                }
            }

            if (motion.LungeLean > 0f)
            {
                var recordedOrigin = playback.SubjectSpaceFor(subject, KineticCueKind.Lunge);
                var strikeOrigin = recordedOrigin.HasValue ? HexWorldLayout.ToWorld(recordedOrigin.Value, radius) : authoritative;
                strikeOrigin.y = authoritative.y;
                var target = playback.LungeTargetFor(subject);
                var recordedTarget = playback.LungeTargetSpaceFor(subject);
                var struck = recordedTarget.HasValue
                    ? HexWorldLayout.ToWorld(recordedTarget.Value, radius)
                    : Vector3.zero;
                if (recordedTarget.HasValue || TryWorldPositionOf(target, out struck))
                {
                    var toward = Vector3.ProjectOnPlane(struck - strikeOrigin, Vector3.up);
                    var distance = toward.magnitude;
                    if (distance > 0.0001f)
                    {
                        var reach = Mathf.Min(
                            distance * KineticPlayback.LungeReachFraction, maxLunge);
                        displacement += strikeOrigin - authoritative + (toward / distance) * reach * motion.LungeLean;
                    }
                }
            }

            return displacement;
        }

        private static Vector3 WorldDirection(Transform piece, Vector3 direction) =>
            piece.parent == null ? direction : piece.parent.TransformDirection(direction);

        private Vector3 StandeeReaction(KineticSubject subject, KineticPieceMotion motion, Vector3 origin, float radius)
        {
            var recordedOrigin = playback.SubjectSpaceFor(subject, KineticCueKind.Lunge);
            if (recordedOrigin.HasValue) origin = HexWorldLayout.ToWorld(recordedOrigin.Value, radius);
            var target = playback.LungeTargetSpaceFor(subject);
            var struck = Vector3.zero;
            if (motion.LungeLean > 0f &&
                (target.HasValue || TryWorldPositionOf(playback.LungeTargetFor(subject), out struck)))
            {
                if (target.HasValue) struck = HexWorldLayout.ToWorld(target.Value, radius);
                return Vector3.ProjectOnPlane(struck - origin, Vector3.up).normalized * motion.LungeLean;
            }
            return HitBump(subject, motion, origin, radius) / StandeeHitBumpWorldDistance * 0.5f;
        }

        private Vector3 HitBump(KineticSubject subject, KineticPieceMotion motion, Vector3 origin, float radius)
        {
            if (motion.FlashStrength <= 0f) return Vector3.zero;
            var incoming = playback.IncomingLungeFor(subject);
            if (incoming == null) return Vector3.zero;
            var attacker = Vector3.zero;
            if (incoming.SubjectSpace.HasValue)
                attacker = HexWorldLayout.ToWorld(incoming.SubjectSpace.Value, radius);
            else if (!TryWorldPositionOf(incoming.Subject, out attacker)) return Vector3.zero;
            return Vector3.ProjectOnPlane(origin - attacker, Vector3.up).normalized *
                (StandeeHitBumpWorldDistance * motion.FlashStrength);
        }

        private KineticPieceMotion WithoutStaleFlash(
            KineticSubject subject, HexCoordinate current, KineticPieceMotion motion)
        {
            var recorded = playback.SubjectSpaceFor(subject, KineticCueKind.Flash);
            if (!recorded.HasValue || recorded.Value.Equals(current) || motion.FlashStrength <= 0f)
            {
                return motion;
            }

            return new KineticPieceMotion(
                motion.RouteProgress,
                motion.HasRouteTravel,
                motion.LungeLean,
                motion.Scale,
                motion.Opacity,
                0f,
                motion.HoldVisible);
        }

        /// <summary>The point at <paramref name="progress"/> along a hex route, in board space.</summary>
        private static Vector3 RoutePoint(
            IReadOnlyList<HexCoordinate> route, float progress, float radius)
        {
            var clamped = KineticPlayback.Clamp01(progress);
            if (route.Count == 1)
            {
                return HexWorldLayout.ToWorld(route[0], radius);
            }

            var scaled = clamped * (route.Count - 1);
            var leg = Mathf.Min((int)scaled, route.Count - 2);
            var withinLeg = scaled - leg;
            return Vector3.Lerp(
                HexWorldLayout.ToWorld(route[leg], radius),
                HexWorldLayout.ToWorld(route[leg + 1], radius),
                withinLeg);
        }

        private static Vector3 AuthoritativeHeroPosition(PrimitiveHeroView hero) =>
            HexWorldLayout.ToWorld(hero.Coordinate, hero.OuterRadius) +
            (Vector3.up * hero.VerticalOffset);

        private static Vector3 AuthoritativeMinionPosition(StandardMinionView minion) =>
            HexWorldLayout.ToWorld(minion.Occupancy.Coordinate, minion.OuterRadius) +
            (Vector3.up * StandardMinionView.DefaultVerticalOffset);

        /// <summary>
        /// Milestone 8.35z11. Where one hero's miniature authoritatively stands, for the flight
        /// layer's coin journeys - the same arithmetic every other placement in this file uses, so a
        /// coin sets out from exactly where the player can see the piece.
        /// </summary>
        private Vector3? BoardPositionOfHero(HeroId heroId) =>
            TryWorldPositionOf(KineticSubject.ForHero(heroId), out var position)
                ? position
                : (Vector3?)null;

        private bool TryWorldPositionOf(KineticSubject subject, out Vector3 position)
        {
            position = Vector3.zero;
            if (!subject.IsDefined)
            {
                return false;
            }

            if (subject.Kind == KineticSubjectKind.Hero)
            {
                var heroes = board.HeroViews;
                for (var index = 0; index < heroes.Count; index++)
                {
                    var hero = heroes[index];
                    if (hero != null && hero.Occupancy.HeroId.Equals(subject.HeroId))
                    {
                        position = AuthoritativeHeroPosition(hero);
                        return true;
                    }
                }

                return false;
            }

            var minions = board.MinionViews;
            for (var index = 0; index < minions.Count; index++)
            {
                var minion = minions[index];
                if (minion != null && minion.Occupancy != null &&
                    minion.Occupancy.MinionId.Equals(subject.MinionId))
                {
                    position = AuthoritativeMinionPosition(minion);
                    return true;
                }
            }

            return false;
        }

        private void ApplyScaleAndVisibility(
            Transform transform, GameObject host, KineticPieceMotion motion, bool standing)
        {
            var baseScale = BaseScaleOf(transform);
            var swell = standing ? 1f : 1f + (FlashScalePunch * motion.FlashStrength);
            transform.localScale = baseScale * (motion.Scale * swell);

            // THE DEFEAT HOLD IS DELIBERATELY NOT IMPLEMENTED HERE, and the reason is the discipline
            // this whole file is built on. Holding a defeated piece drawn meant re-activating a
            // GameObject the board view had already deactivated - the one place this layer would show
            // a piece the projection does not contain, and it did it behind the owning view's back,
            // on an object that view pools and reuses. The complete unfiltered Play Mode run at
            // integration found the cost: five existing journeys assert that a defeated miniature has
            // LEFT the board, which is rulebook p.13's removal and a real acceptance check of the
            // milestones that wrote them, and the hold made all five report the piece still present.
            // Weakening five rules guards to buy a flourish is the wrong trade.
            //
            // Milestone 8.35z6 delivers the fade properly, exactly where this paragraph said it
            // had to live: BoardInteractionUnityView (Match/UnityMatchFeatureViews.cs) spawns a bare,
            // non-pooled DefeatFadeGhostView the instant its own Render/RenderMinions finds a piece
            // going from drawn to not-drawn, and BoardDefeatFadeDriver ticks it - independent of this
            // file and this cue's own (still-inert) Scale/Opacity math on the real, pooled piece above.
            // This file and the five removal guards it protected are untouched.
        }

        private void ApplyHeroTint(PrimitiveHeroView hero, KineticPieceMotion motion)
        {
            var renderer = hero.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            // The standee already reacts with a physical recoil. Keep its team base at its resting
            // colour, including a token-to-standee switch during a running legacy hit flash.
            if (hero.Standee != null && hero.Standee.IsStanding)
            {
                renderer.SetPropertyBlock(null);
                return;
            }
            if (motion.FlashStrength <= 0f) return;

            // A property block rather than a material write: it allocates no material, mutates nothing
            // shared, and is dropped whole when the flash ends. A shader without these properties
            // simply ignores them, which is why the scale punch above carries the flash on its own.
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            var lit = Color.Lerp(Color.white, MatchHudTheme.ShineRim, motion.FlashStrength);
            block.SetColor("_BaseColor", lit);
            block.SetColor("_Color", lit);
            renderer.SetPropertyBlock(block);
        }

        // ---- Putting pieces back. The half that makes every end state projection-true.

        private sealed class PieceRestore
        {
            public Transform Transform;
            public GameObject Host;
            public SpriteRenderer Renderer;
            public Vector3 Scale;
            public Color Color;
            public bool HadColor;
        }

        private void Remember(Transform transform, GameObject host, SpriteRenderer renderer)
        {
            for (var index = 0; index < restorations.Count; index++)
            {
                if (ReferenceEquals(restorations[index].Transform, transform))
                {
                    return;
                }
            }

            restorations.Add(new PieceRestore
            {
                Transform = transform,
                Host = host,
                Renderer = renderer,
                Scale = transform.localScale,
                Color = renderer == null ? Color.white : renderer.color,
                HadColor = renderer != null
            });
        }

        private Vector3 BaseScaleOf(Transform transform)
        {
            for (var index = 0; index < restorations.Count; index++)
            {
                if (ReferenceEquals(restorations[index].Transform, transform))
                {
                    return restorations[index].Scale;
                }
            }

            return transform.localScale;
        }

        private Color BaseColorOf(Transform transform, SpriteRenderer renderer)
        {
            for (var index = 0; index < restorations.Count; index++)
            {
                if (ReferenceEquals(restorations[index].Transform, transform))
                {
                    return restorations[index].HadColor ? restorations[index].Color : Color.white;
                }
            }

            return renderer == null ? Color.white : renderer.color;
        }

        private void Restore(Transform transform)
        {
            for (var index = restorations.Count - 1; index >= 0; index--)
            {
                if (!ReferenceEquals(restorations[index].Transform, transform))
                {
                    continue;
                }

                Put(restorations[index]);
                restorations.RemoveAt(index);
                return;
            }
        }

        private void RestoreEveryPiece()
        {
            for (var index = 0; index < restorations.Count; index++)
            {
                Put(restorations[index]);
            }

            restorations.Clear();
        }

        private static void Put(PieceRestore restore)
        {
            if (restore.Transform == null)
            {
                return;
            }

            restore.Transform.localScale = restore.Scale;
            var hero = restore.Transform.GetComponent<PrimitiveHeroView>();
            var minion = restore.Transform.GetComponent<StandardMinionView>();
            if (hero != null) restore.Transform.localPosition = AuthoritativeHeroPosition(hero);
            else if (minion != null && minion.Occupancy != null)
                restore.Transform.localPosition = AuthoritativeMinionPosition(minion);
            restore.Transform.GetComponent<IllustratedStandeeView>()?.SetAttackLean(Vector3.zero, Camera.main);
            restore.Transform.GetComponent<IllustratedMinionStandeeView>()?.SetAttackLean(Vector3.zero, Camera.main);
            if (restore.HadColor && restore.Renderer != null)
            {
                restore.Renderer.color = restore.Color;
            }

            var meshRenderer = restore.Transform.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.SetPropertyBlock(null);
            }
        }

        // ---- The board's own marks.


        /// <summary>
        /// The one absolutely-positioned, non-picking layer every kinetic overlay lives on. It is a
        /// sibling added last rather than an edit to any HUD panel, so it draws above the HUD, takes
        /// part in no layout, and can never intercept a click meant for a control.
        /// </summary>
        private VisualElement EnsureOverlay()
        {
            if (overlay != null && overlay.panel != null)
            {
                return overlay;
            }

            var root = hud?.RootElement;
            if (root == null)
            {
                return null;
            }

            overlay = new VisualElement
            {
                name = OverlayName,
                pickingMode = PickingMode.Ignore
            };
            overlay.style.position = Position.Absolute;
            overlay.style.left = 0f;
            overlay.style.top = 0f;
            overlay.style.right = 0f;
            overlay.style.bottom = 0f;
            root.Add(overlay);
            return overlay;
        }

        // ---- The breathing light on the gold shine.

        /// <summary>
        /// Milestone 8.35z39. Breathes the OWN OUTLINE of every element wearing the awaited
        /// gold, for each hero the match is currently waiting on.
        ///
        /// <para>The animation now applies only to the standing rail frame. W-014 removes the
        /// card-perimeter host entirely, so awaited cards never enter this traversal while the
        /// quieter rail signal keeps its established cadence.</para>
        ///
        /// <para><b>"Light, not just yellow color" is the BRIGHTNESS axis, and only that</b> - and it
        /// travels DOWNWARD from the gold, which an offline measurement settled. The first version of
        /// this ran from the resting <see cref="MatchHudTheme.ShineGlow"/> UP toward
        /// <see cref="MatchHudTheme.ShineRim"/> and read correctly in every respect except the one
        /// that mattered: measured, it moved the outline's luminance by 3.44%, because ShineGlow is
        /// already a 0.92-luminance near-white and its gap to the rim is almost all blue channel -
        /// saturation, not brightness. On a 1-3px border that is nothing, and it would have shipped
        /// looking exactly like the static gold the user asked to replace. So the light KINDLES
        /// instead: the outline dims to <see cref="KineticTiming.ShineBreatheFloor"/> of its
        /// brightness and returns to the full painted gold - a 2.2x luminance swing - with the channel
        /// ratios, and therefore the hue, held exactly constant, because the floor is applied as a
        /// multiplier rather than as a lerp toward black.</para>
        ///
        /// <para><b>It owns nothing and remembers nothing</b>, which is what lets it run against a HUD
        /// that rebuilds its elements wholesale several times a second. It writes only colours on its
        /// own breath and recognises only colours on that same breath, so a freshly rebuilt element -
        /// painted at the breath's brightest point by definition - is picked up on its first frame, an
        /// element it lit last frame is still recognised, and a border painted any other colour is
        /// never touched however often this runs. A rail carries FOUR meanings on its four edges - the
        /// awaited frame, the team accent, the reserved identity edge and the selection outline - and
        /// only the first is the light's to move. That is also why there is no ratchet: the source of
        /// every lerp is the constant, never the value this wrote last time.</para>
        /// </summary>
        private void ApplyShineBreath()
        {
            var root = hud?.RootElement;
            if (root == null)
            {
                return;
            }

            if (model.BreathingShineHeroes.Count == 0)
            {
                // Nothing is awaited. The HUD already rebuilds an unawaited element at its resting
                // colour, so there is nothing to put back - and a rebuild is not guaranteed to have
                // happened yet, which is why the hosts still on screen are returned to rest here.
                Rest(root);
                return;
            }

            var hosts = FindAwaitedShineHosts(root);
            if (hosts.Count == 0)
            {
                return;
            }

            // 0 is the dimmed amber, 1 is the resting gold the HUD paints. The breath is the whole
            // travel, so there is no amplitude factor here to get out of step with the floor.
            var lit = playback.ShineBreathIntensity;
            for (var index = 0; index < hosts.Count; index++)
            {
                Light(hosts[index], lit);
            }
        }

        /// <summary>
        /// Lights one element's own outline to a point on its breath: <c>0</c> the dimmed amber,
        /// <c>1</c> the full painted gold. Every awaited element on screen is handed the SAME point on
        /// the SAME frame, so a hand of cards and the tray above them breathe as one light rather than
        /// as several.
        /// </summary>
        private static void Light(VisualElement host, float lit)
        {
            if (host?.panel == null)
            {
                return;
            }

            if (host.name == "gd-rail-awaited-glow" && host.userData is Color accent)
            {
                var playerLight = accent * (0.35f + 0.65f * lit);
                playerLight.a = accent.a;
                host.style.borderTopColor = host.style.borderBottomColor = playerLight;
                host.style.borderLeftColor = host.style.borderRightColor = playerLight;
                return;
            }
            var toward = LitOutline(lit);
            var style = host.style;
            if (IsAwaitedGold(host.resolvedStyle.borderTopColor)) style.borderTopColor = toward;
            if (IsAwaitedGold(host.resolvedStyle.borderBottomColor)) style.borderBottomColor = toward;
            if (IsAwaitedGold(host.resolvedStyle.borderLeftColor)) style.borderLeftColor = toward;
            if (IsAwaitedGold(host.resolvedStyle.borderRightColor)) style.borderRightColor = toward;
        }

        /// <summary>
        /// Puts every lit outline still on screen back to the full resting gold - the top of the
        /// breath, which is exactly the colour the HUD paints for itself. So an element this stops
        /// touching is left indistinguishable from one it never touched.
        /// </summary>
        private static void Rest(VisualElement root)
        {
            var hosts = FindAwaitedShineHosts(root);
            for (var index = 0; index < hosts.Count; index++)
            {
                Light(hosts[index], 1f);
            }
        }

        /// <summary>
        /// The bottom of the breath: the resting awaited gold, dimmed. Multiplied rather than lerped
        /// toward black so the channel RATIOS - the hue - are untouched and only the brightness moves.
        /// </summary>
        internal static Color DimmedOutline()
        {
            var glow = MatchHudTheme.ShineGlow;
            var floor = KineticTiming.ShineBreatheFloor;
            return new Color(glow.r * floor, glow.g * floor, glow.b * floor, glow.a);
        }

        /// <summary>
        /// What an awaited outline is at a given point of its breath: <c>0</c> is the dimmed amber at
        /// the bottom, <c>1</c> is the full resting gold the HUD itself paints.
        ///
        /// <para><b>The top of the breath is the HUD's own colour, and that is load-bearing rather
        /// than tidy.</b> It means the outline the HUD paints on a rebuild is already a point on this
        /// breath - its brightest one - so a freshly rebuilt element is recognised on its first frame
        /// with no special case, and an element this treatment stops touching is left at exactly the
        /// colour the HUD would have given it. It also means the light cannot climb past the gold into
        /// a colour nobody chose: <see cref="MatchHudTheme.ShineGlow"/> already has a saturated red
        /// channel, so brightening past it would only wash the hue out toward white, which is the
        /// "just yellow color" failure from the other direction.</para>
        /// </summary>
        internal static Color LitOutline(float lit) =>
            Color.Lerp(DimmedOutline(), MatchHudTheme.ShineGlow, lit);

        /// <summary>
        /// Whether a border colour is one this breath owns: any point on the segment from the dimmed
        /// amber to full light.
        ///
        /// <para><b>Distance to the SEGMENT, not a bounding box around it.</b> A box around two colours
        /// this far apart admits a large volume of colour space that the light never actually
        /// occupies, and the HUD paints real borders inside that volume - the Active violet and the
        /// confirm orange both land in a box drawn round this segment, and both mean something a light
        /// does not get to overwrite. The segment itself is a line, and every foreign colour on this
        /// HUD is far off it.</para>
        ///
        /// <para>The tolerance has to clear one genuine source of drift: a colour written into a
        /// <c>style</c> and read back from <c>resolvedStyle</c> makes a round trip through Unity's own
        /// colour handling. It is small enough that the nearest foreign colour is still many times
        /// further away, which the offline probe measures rather than assumes.</para>
        /// </summary>
        public static bool IsAwaitedGold(Color color)
        {
            if (color.a < 0.5f)
            {
                return false;
            }

            return DistanceToBreathSegment(color) <= AwaitedGoldTolerance;
        }

        /// <summary>How far off the breath's own segment a colour may sit and still be claimed.</summary>
        public const float AwaitedGoldTolerance = 0.04f;

        /// <summary>
        /// Milestone 8.35z47. The narrowest band, in reference units, that this treatment may
        /// be written across and still be a light a player can see.
        ///
        /// <para><b>Why a width belongs beside a colour.</b> The breath writes border colours, so the
        /// whole of its visible effect is (the swing) x (the pixels the border covers). 8.35z39
        /// measured the first factor carefully - it caught and rejected a version whose swing was
        /// 3.44% - and never measured the second at all, and the second is where it went wrong: the
        /// hero tray's gold was its rail's ONE-unit border, so a correct 2.22x swing was being written
        /// across half a real pixel and read, accurately, as a solid colour.</para>
        ///
        /// <para>A reference unit is one pixel at the 2560x1440 the HUD's coordinate space is stated
        /// in, and the smallest viewport this build supports halves that. Four units is therefore two
        /// real pixels at the worst case - the floor at which a brightness change reads as a change at
        /// all rather than as edge antialiasing. The bands this HUD actually draws stand well clear of
        /// it; this is the line under them, not the target.</para>
        /// </summary>
        internal const float MinimumBreathBandWidth = 4f;

        /// <summary>
        /// The distance from a colour to the nearest point of the dimmed-to-lit segment, in RGB.
        /// Exposed for the probe that measures the margin against every other colour this HUD paints
        /// on a border.
        ///
        /// <para>Milestone 8.35z47 widened this and the three members around it from
        /// <c>internal</c> to <c>public</c>, for one reason worth stating: this assembly's
        /// <c>InternalsVisibleTo</c> deliberately stops at the EDIT MODE test assembly, and the case
        /// that samples this breath on consecutive REAL frames can only exist in PLAY MODE. Reading
        /// these four from a Play Mode fixture is what lets that case measure the SHIPPED predicate
        /// instead of a copy of it - and a copy is precisely what would let the production formula
        /// drift away from its own acceptance test. They are pure, side-effect-free queries over a
        /// colour, so publishing them widens no state and grants no writer.</para>
        /// </summary>
        public static float DistanceToBreathSegment(Color color)
        {
            var from = DimmedOutline();
            var to = MatchHudTheme.ShineGlow;
            var dr = to.r - from.r;
            var dg = to.g - from.g;
            var db = to.b - from.b;
            var lengthSquared = (dr * dr) + (dg * dg) + (db * db);

            var t = lengthSquared <= 0f
                ? 0f
                : (((color.r - from.r) * dr) + ((color.g - from.g) * dg) + ((color.b - from.b) * db)) /
                    lengthSquared;
            t = t < 0f ? 0f : t > 1f ? 1f : t;

            var nearestR = from.r + (dr * t);
            var nearestG = from.g + (dg * t);
            var nearestB = from.b + (db * t);
            return Mathf.Sqrt(
                ((color.r - nearestR) * (color.r - nearestR)) +
                ((color.g - nearestG) * (color.g - nearestG)) +
                ((color.b - nearestB) * (color.b - nearestB)));
        }

        /// <summary>
        /// Milestone 8.35z9. Every element wearing the awaited gold, found by the ONE shared marker
        /// <see cref="MatchHudTheme.AwaitedShineClass"/> - a lit hero rail and a shining card face
        /// alike, which are different element types on different surfaces and cannot share a name.
        ///
        /// <para><b>What this replaced, and why it animated nothing for four milestones.</b> It used
        /// to ask for <c>Image</c> elements NAMED <c>gd-card-shine</c>. That is the milestone 8.30d
        /// prepared printed-artwork overlay - a different family altogether, present only on cards
        /// whose art has a traced shine and unrelated to whether the match is waiting on anybody.
        /// The awaited ring is a <c>VisualElement</c> called <c>gd-card-awaited-shine</c> and the
        /// awaited rail has no child element at all, so this query could not match either one: wrong
        /// name AND wrong type. It returned an empty list, the caller returned early, and nothing
        /// ever swept - silently, because an empty HUD is a legitimate state for this to find.</para>
        ///
        /// <para>Exposed rather than kept private so the guard case can assert on THIS code rather
        /// than on a copy of it: "a composed HUD with a lit tray and a shining card yields a
        /// non-empty host set" is the claim that had no test. Milestone 8.35z47 took it from
        /// <c>internal</c> to <c>public</c> so the Play Mode assembly - which this assembly's
        /// <c>InternalsVisibleTo</c> deliberately does not name - can ask the same question on the
        /// live panel; see <see cref="DistanceToBreathSegment"/> for the full reasoning.</para>
        /// </summary>
        public static List<VisualElement> FindAwaitedShineHosts(VisualElement root)
        {
            var found = new List<VisualElement>();
            if (root == null)
            {
                return found;
            }

            var hosts = root.Query<VisualElement>(className: MatchHudTheme.AwaitedShineClass).ToList();
            for (var index = 0; index < hosts.Count && found.Count < MaxBreathingShineElements; index++)
            {
                var host = hosts[index];
                if (host == null)
                {
                    continue;
                }

                found.Add(host);
            }

            return found;
        }
    }

    /// <summary>
    /// Milestone 8.35y. The one per-frame tick the match scene needs, and the isolation around
    /// it.
    ///
    /// <para>The configured match scene has never had a frame loop - every surface redraws on a
    /// projection change - so kinetics need one, and it must be the safest component in the project.
    /// A throw inside <c>Update</c> repeats every frame and would take a playtest down over a
    /// flourish, so the tick is caught, reported once, and after a small number of failures this
    /// component disables itself and the board goes back to redrawing on projection changes exactly as
    /// it did before this milestone.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KineticCueDriver : MonoBehaviour
    {
        public const string DriverObjectName = "Kinetic Cue Driver";

        /// <summary>How many ticks may throw before the driver takes itself out of the frame loop.</summary>
        public const int FailureBudget = 3;

        private KineticCueUnityView view;
        private int failures;

        public static KineticCueDriver Attach(GameObject host, KineticCueUnityView view)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (view == null) throw new ArgumentNullException(nameof(view));
            // Same `??` trap HexTileView.SetActiveEffectAura records at 8.35z63, corrected for
            // consistency: `??` bypasses Unity's null overload, an explicit comparison does not.
            var driver = host.GetComponent<KineticCueDriver>();
            if (driver == null)
            {
                driver = host.AddComponent<KineticCueDriver>();
            }

            driver.view = view;
            driver.failures = 0;
            driver.enabled = true;
            return driver;
        }

        /// <summary>How many ticks have thrown. Zero on a healthy driver, and asserted by a fixture.</summary>
        public int FailureCount => failures;

        private void Update()
        {
            if (view == null)
            {
                return;
            }

            try
            {
                view.Tick(Time.deltaTime);
            }
            catch (Exception error)
            {
                failures++;
                Debug.LogWarning(
                    "Kinetic cue tick failed (" + failures + "/" + FailureBudget + "): " + error);
                if (failures >= FailureBudget)
                {
                    Debug.LogWarning(
                        "Kinetic cues are disabled for this session; the board keeps redrawing on " +
                        "projection changes exactly as it did before this milestone.");
                    enabled = false;
                }
            }
        }
    }
}
