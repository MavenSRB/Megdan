using System;
using System.Collections.Generic;
using GuardsDigital.Core.Board;
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

        /// <summary>How much a struck piece swells at the peak of its flash.</summary>
        public const float FlashScalePunch = 0.14f;

        /// <summary>
        /// The most shine overlays the circling light travels at once. The amendment gates this pass on
        /// the recorded perf thresholds and asks for "few animated elements at once"; a hand of five
        /// cards plus a tray is the realistic worst case, and this is the hard stop above it.
        /// </summary>
        public const int MaxSweptShineElements = 8;

        private const string OverlayName = "gd-kinetic-overlay";
        private const string SweepName = "gd-kinetic-sweep";
        // Milestone 8.35z9 retired `ShineElementName = "gd-card-shine"` from here: that name belongs
        // to milestone 8.30d's prepared printed-artwork overlay, which this file mistook for the
        // awaited-answer shine for four milestones. The sweep now asks for
        // MatchHudTheme.AwaitedShineClass, which is the marker the awaited treatments actually wear.

        private readonly BoardInteractionUnityView board;
        private readonly ConfiguredMatchHudRenderer hud;
        private readonly Func<Camera> cameraSource;
        private readonly KineticPlayback playback = new KineticPlayback();
        private readonly List<PieceRestore> restorations = new List<PieceRestore>();
        private readonly List<VisualElement> sweeps = new List<VisualElement>();
        private VisualElement overlay;
        private KineticCueViewModel model = KineticCueViewModel.Empty;

        public KineticCueUnityView(
            BoardInteractionUnityView board,
            ConfiguredMatchHudRenderer hud = null,
            Func<Camera> cameraSource = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hud = hud;
            this.cameraSource = cameraSource ?? (() => Camera.main);
        }

        /// <summary>The clock, exposed so a Play Mode case can assert what is playing.</summary>
        public KineticPlayback Playback => playback;

        /// <summary>The model most recently rendered.</summary>
        public KineticCueViewModel Model => model;

        public void Render(KineticCueViewModel viewModel)
        {
            model = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            if (model.SnapToAuthoritative)
            {
                // A first sight of the match, or a log that went backwards. Everything stops and
                // every piece is put back.
                playback.CancelAll();
                RestoreEveryPiece();
                return;
            }

            playback.Begin(model.Cues);
        }

        /// <summary>
        /// One frame of kinetics. Called by <see cref="KineticCueDriver"/>, or directly by a test that
        /// wants to advance the clock deterministically instead of waiting on frames.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            playback.Advance(deltaSeconds);
            ApplyPieceMotions();
            ApplyBoardMarkPulse();
            ApplyShineSweep();
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
        }

        private bool ApplyHero(PrimitiveHeroView hero)
        {
            if (hero == null)
            {
                return false;
            }

            var subject = KineticSubject.ForHero(hero.Occupancy.HeroId);
            var motion = playback.MotionFor(subject);
            if (motion.IsStill)
            {
                Restore(hero.transform);
                return false;
            }

            Remember(hero.transform, hero.gameObject, null);
            var origin = AuthoritativeHeroPosition(hero);
            hero.transform.localPosition = origin + Displacement(subject, motion, origin, hero.OuterRadius);
            ApplyScaleAndVisibility(hero.transform, hero.gameObject, motion);
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
            var motion = playback.MotionFor(subject);
            if (motion.IsStill)
            {
                Restore(minion.transform);
                return false;
            }

            var renderer = minion.GetComponent<SpriteRenderer>();
            Remember(minion.transform, minion.gameObject, renderer);
            var origin = AuthoritativeMinionPosition(minion);
            minion.transform.localPosition =
                origin + Displacement(subject, motion, origin, minion.OuterRadius);
            ApplyScaleAndVisibility(minion.transform, minion.gameObject, motion);
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
            KineticSubject subject, KineticPieceMotion motion, Vector3 authoritative, float radius)
        {
            var displacement = Vector3.zero;

            if (motion.HasRouteTravel)
            {
                var route = playback.RouteFor(subject);
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
                var target = playback.LungeTargetFor(subject);
                if (TryWorldPositionOf(target, out var struck))
                {
                    var toward = struck - authoritative;
                    var distance = toward.magnitude;
                    if (distance > 0.0001f)
                    {
                        var reach = Mathf.Min(
                            distance * KineticPlayback.LungeReachFraction, MaxLungeWorldDistance);
                        displacement += (toward / distance) * reach * motion.LungeLean;
                    }
                }
            }

            return displacement;
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
            Transform transform, GameObject host, KineticPieceMotion motion)
        {
            var baseScale = BaseScaleOf(transform);
            var swell = 1f + (FlashScalePunch * motion.FlashStrength);
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
            if (motion.FlashStrength <= 0f)
            {
                return;
            }

            var renderer = hero.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

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

        private void ApplyBoardMarkPulse()
        {
            var marker = board.ActingHeroMarker;
            if (marker == null || !marker.IsVisible)
            {
                return;
            }

            // Only while the viewer has something to answer. Everything else on the board stays still,
            // which is the amendment's own rule and the reason this costs one multiply a frame.
            marker.transform.localScale = model.PulseBoardMarks
                ? Vector3.one * playback.MarkPulseScale
                : Vector3.one;
        }

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

        // ---- The circling light on the gold shine.

        /// <summary>
        /// Travels a small bright mark around the border of each shine overlay that belongs to a hero
        /// the match is currently waiting on.
        ///
        /// <para>It attaches its own marks to elements it finds by name and owns nothing else, so a HUD
        /// that rebuilt its card faces this frame simply gets fresh marks, and a HUD with no prepared
        /// shine overlays at all gets none - the clean-checkout case, where this is silently
        /// nothing.</para>
        /// </summary>
        private void ApplyShineSweep()
        {
            var root = hud?.RootElement;
            if (root == null)
            {
                return;
            }

            var wanted = model.SweepingShineHeroes.Count > 0;
            if (!wanted)
            {
                ClearSweeps();
                return;
            }

            CollectSweepHosts(root);
            if (sweeps.Count == 0)
            {
                return;
            }

            var phase = playback.ShineSweepPhase;
            for (var index = 0; index < sweeps.Count; index++)
            {
                var mark = sweeps[index];
                if (mark?.parent == null)
                {
                    continue;
                }

                // Each mark is offset around the loop so the lights do not march in lockstep, which
                // reads as a machine rather than as a shine.
                Place(mark, KineticPlayback.Repeat(phase + (index * 0.13f)));
            }
        }

        /// <summary>Places one mark at a fraction of the way around its parent's border.</summary>
        private static void Place(VisualElement mark, float phase)
        {
            var side = phase * 4f;
            float left;
            float top;
            if (side < 1f)
            {
                left = side * 100f;
                top = 0f;
            }
            else if (side < 2f)
            {
                left = 100f;
                top = (side - 1f) * 100f;
            }
            else if (side < 3f)
            {
                left = (3f - side) * 100f;
                top = 100f;
            }
            else
            {
                left = 0f;
                top = (4f - side) * 100f;
            }

            mark.style.left = Length.Percent(left);
            mark.style.top = Length.Percent(top);
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
        /// <para>Exposed <c>internal static</c> rather than kept private so the guard case can assert
        /// on THIS code rather than on a copy of it: "a composed HUD with a lit tray and a shining
        /// card yields a non-empty host set" is the claim that had no test.</para>
        /// </summary>
        internal static List<VisualElement> FindShineSweepHosts(VisualElement root)
        {
            var found = new List<VisualElement>();
            if (root == null)
            {
                return found;
            }

            var hosts = root.Query<VisualElement>(className: MatchHudTheme.AwaitedShineClass).ToList();
            for (var index = 0; index < hosts.Count && found.Count < MaxSweptShineElements; index++)
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

        private void CollectSweepHosts(VisualElement root)
        {
            sweeps.Clear();
            var hosts = FindShineSweepHosts(root);
            for (var index = 0; index < hosts.Count; index++)
            {
                // The mark rides INSIDE the element that carries the gold, so its path traces that
                // element's own border - the card's ring, the rail's frame - rather than a parent's.
                sweeps.Add(EnsureSweepMark(hosts[index]));
            }
        }

        private static VisualElement EnsureSweepMark(VisualElement host)
        {
            var existing = host.Q<VisualElement>(SweepName);
            if (existing != null)
            {
                return existing;
            }

            var mark = new VisualElement
            {
                name = SweepName,
                pickingMode = PickingMode.Ignore
            };
            mark.style.position = Position.Absolute;
            mark.style.width = 10f;
            mark.style.height = 10f;
            mark.style.backgroundColor = MatchHudTheme.ShineRim;
            mark.style.opacity = 0.75f;
            mark.style.borderTopLeftRadius = 5f;
            mark.style.borderTopRightRadius = 5f;
            mark.style.borderBottomLeftRadius = 5f;
            mark.style.borderBottomRightRadius = 5f;
            mark.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-50f));
            host.Add(mark);
            return mark;
        }

        private void ClearSweeps()
        {
            for (var index = 0; index < sweeps.Count; index++)
            {
                sweeps[index]?.RemoveFromHierarchy();
            }

            sweeps.Clear();
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
            var driver = host.GetComponent<KineticCueDriver>() ?? host.AddComponent<KineticCueDriver>();
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
