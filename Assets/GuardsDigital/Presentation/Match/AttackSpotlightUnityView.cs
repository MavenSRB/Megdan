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
    /// Milestone 8.35z12. The NINETEENTH AMENDMENT's spotlight on the real surface: the HUD
    /// goes, the board camera moves in and tilts, both card faces stand at the screen edges beside
    /// their own pieces, and one CONTINUE button gives it back.
    ///
    /// <para><b>Nothing here is authoritative and nothing here waits.</b> The camera, the hidden panels
    /// and the two faces are all viewer-local presentation; no method on this class submits a command,
    /// blocks a prompt or delays a projection. That is what lets one client hold a spotlight for six
    /// seconds while another dismissed it instantly and both stay projection-true.</para>
    ///
    /// <para><b>It rewrites the camera every frame rather than owning it.</b> The board camera has
    /// several legitimate writers - <c>PlaceholderTestGridView.FrameWholeMap</c> reframes on a map
    /// rebuild and whenever the HUD republishes its safe layout, which HIDING THE HUD can itself
    /// provoke. So this never tries to stop anyone writing the camera; it simply writes the framing it
    /// wants afterwards, every frame, from the captured rest pose and the current weight. An
    /// interrupted spotlight therefore needs no unwind step: it restores the captured pose once and
    /// asks the board to reassert its own framing, and the next frame is the board's again.</para>
    ///
    /// <para><b>It degrades rather than fails.</b> A missing camera, a HUD with no root, a piece with
    /// no view, a panel too narrow for two cards - each costs the flourish and nothing else, and
    /// <see cref="AttackSpotlightDriver"/> additionally isolates the whole per-frame pass.</para>
    /// </summary>
    public sealed class AttackSpotlightUnityView : IAttackSpotlightView
    {
        /// <summary>The name of the one layer this view owns on the HUD's root.</summary>
        public const string LayerName = "gd-attack-spotlight";

        private const string AttackerCardName = "gd-spotlight-attacker-card";
        private const string DefenderCardName = "gd-spotlight-defender-card";
        private const string CaptionName = "gd-spotlight-caption";
        private const string ContinueName = "gd-spotlight-continue";
        private const string TracerName = "gd-spotlight-tracer";
        private const string ImpactName = "gd-spotlight-impact";

        private readonly BoardInteractionUnityView board;
        private readonly ConfiguredMatchHudRenderer hud;
        private readonly List<VisualElement> hiddenChrome = new List<VisualElement>();
        // The INLINE style each panel had, not its resolved one, so a restore writes back exactly
        // what was there - including "nothing was set", which a resolved value would replace with a
        // hard-coded Flex and quietly override whatever USS had been deciding.
        private readonly List<StyleEnum<DisplayStyle>> hiddenChromeStyles =
            new List<StyleEnum<DisplayStyle>>();

        private VisualElement layer;
        private VisualElement attackerCardHost;
        private VisualElement defenderCardHost;
        private Label caption;
        private Button continueControl;
        private VisualElement tracer;
        private VisualElement impact;
        private float smackElapsed;
        private bool smackRunning;
        private AttackSpotlightViewModel model = AttackSpotlightViewModel.Empty;
        private int builtSequence;
        private bool isShowing;
        private bool hasCapturedRest;
        private Vector3 restPosition;
        private Quaternion restRotation;
        private float restOrthographicSize;
        private bool restOrthographic;

        public AttackSpotlightUnityView(
            BoardInteractionUnityView board,
            ConfiguredMatchHudRenderer hud = null,
            Func<Camera> cameraSource = null,
            Action boardFramingRestored = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.hud = hud;
            CameraSource = cameraSource ?? (() => Camera.main);
            BoardFramingRestored = boardFramingRestored;
        }

        /// <summary>
        /// Where the board camera comes from. Settable so the composition root can hand over the
        /// camera it already resolved rather than this view guessing at <c>Camera.main</c>.
        /// </summary>
        public Func<Camera> CameraSource { get; set; }

        /// <summary>
        /// Asked once after the captured rest pose is written back, so the board view can reassert its
        /// own framing. Optional: with none, the captured pose is the whole restore, which is exact
        /// unless the HUD's safe layout changed while the spotlight was up.
        /// </summary>
        public Action BoardFramingRestored { get; set; }

        /// <summary>Raised when the viewer presses CONTINUE.</summary>
        public event Action ContinueRequested;

        /// <summary>The model most recently rendered.</summary>
        public AttackSpotlightViewModel Model => model;

        /// <summary>Whether the HUD's own panels are currently hidden by this view.</summary>
        public bool IsChromeHidden => hiddenChrome.Count > 0;

        /// <summary>Whether this view is currently driving the camera.</summary>
        public bool IsDrivingCamera => hasCapturedRest;

        public void Render(AttackSpotlightViewModel viewModel)
        {
            model = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            if (model.SnapToAuthoritative || !model.IsActive)
            {
                if (isShowing || model.SnapToAuthoritative)
                {
                    Retire();
                }

                return;
            }

            EnsureLayer();
            var scene = model.Scene;
            if (scene != null && scene.Sequence != builtSequence)
            {
                BuildScene(scene);
                builtSequence = scene.Sequence;
            }

            ApplyFrame();
        }

        /// <summary>
        /// One frame of the spotlight on the real surface: the camera, the chrome, and where the two
        /// faces stand. Separate from <see cref="Render"/> because the camera has other writers - see
        /// this class's own remarks - so the framing is reasserted every frame rather than once.
        /// </summary>
        public void ApplyFrame(float deltaSeconds = 0f)
        {
            if (!model.IsActive || model.Scene == null)
            {
                smackRunning = false;
                // Nothing is up, so this frame must cost NOTHING - no style write, no camera write,
                // no allocation. This is per-frame work in a scene that had none before milestone
                // 8.35y, and the Phase 4 frame gate is the thing it must not move: a `style.display`
                // written every frame marks its element dirty every frame, whatever value it holds.
                if (isShowing)
                {
                    Retire();
                }

                return;
            }

            isShowing = true;
            var camera = CameraSource?.Invoke();
            if (camera == null)
            {
                return;
            }

            if (!hasCapturedRest)
            {
                restPosition = camera.transform.position;
                restRotation = camera.transform.rotation;
                restOrthographic = camera.orthographic;
                restOrthographicSize = camera.orthographicSize;
                hasCapturedRest = true;
            }

            var weight = Mathf.Clamp01(model.FramingWeight);
            if (!TryResolvePieces(model.Scene, out var attackerWorld, out var defenderWorld))
            {
                // A piece the board cannot draw - it left, or its view has not been bound yet. The
                // camera keeps the rest pose; the cards and the button still stand, so a viewer is
                // never trapped behind a spotlight that failed to frame anything.
                ApplyChrome(weight);
                LayoutCards(camera, weight, attackerWorld, defenderWorld, framed: false);
                return;
            }

            // The board's own screen axes, read from the camera's REST pose rather than from the map
            // view. The resting board camera looks straight down with its up along the board's
            // screen-up, so its right and up at rest are exactly the basis FrameWholeMap framed in -
            // and reading them here keeps this view from having to know the map exists.
            var screenRight = restRotation * Vector3.right;
            var screenUp = restRotation * Vector3.up;
            var frame = AttackSpotlightFraming.Frame(
                attackerWorld,
                defenderWorld,
                screenRight,
                screenUp,
                camera.aspect > 0.01f ? camera.aspect : 16f / 9f,
                PieceRadius(),
                restOrthographic ? restOrthographicSize : 0f);

            var targetRotation = Quaternion.LookRotation(frame.ViewDirection, frame.ViewUp);
            var targetPosition = AttackSpotlightFraming.PositionFor(frame);
            camera.orthographic = restOrthographic;
            if (restOrthographic)
            {
                camera.orthographicSize =
                    Mathf.Lerp(restOrthographicSize, frame.OrthographicSize, weight);
            }

            camera.transform.position = Vector3.Lerp(restPosition, targetPosition, weight);
            camera.transform.rotation = Quaternion.Slerp(restRotation, targetRotation, weight);

            ApplyChrome(weight);
            LayoutCards(camera, weight, attackerWorld, defenderWorld, framed: true);
            ApplyRangedSmack(camera, attackerWorld, defenderWorld, deltaSeconds);
        }

        /// <summary>
        /// The ranged smack: a dash crossing the ground between the two pieces, and the mark its
        /// arrival leaves.
        ///
        /// <para>It runs on the HELD phase only, which is where the deferred cues are released - so
        /// the attacker's jab, the tracer and the impact all happen with the camera already in place
        /// and both faces up, in the order the user asked for. A melee pair gets none of this: at
        /// that separation nothing crosses any ground, and the jab and flash already say
        /// everything.</para>
        /// </summary>
        private void ApplyRangedSmack(
            Camera camera, Vector3 attackerWorld, Vector3 defenderWorld, float deltaSeconds)
        {
            if (tracer == null || impact == null)
            {
                return;
            }

            var held = model.Phase == AttackSpotlightPhase.Held;
            if (!held)
            {
                smackRunning = false;
                Hide(tracer);
                Hide(impact);
                return;
            }

            if (!smackRunning)
            {
                smackRunning = true;
                smackElapsed = 0f;
            }
            else if (deltaSeconds > 0f)
            {
                smackElapsed += Mathf.Min(deltaSeconds, 0.25f);
            }

            var separation = Vector3.Distance(attackerWorld, defenderWorld);
            if (!RangedSmackProfile.IsRangedSeparation(separation))
            {
                Hide(tracer);
                Hide(impact);
                return;
            }

            var from = ToPanelPoint(camera, attackerWorld);
            var to = ToPanelPoint(camera, defenderWorld);
            var path = to - from;
            var pathLength = path.magnitude;
            if (pathLength < 1f)
            {
                Hide(tracer);
                Hide(impact);
                return;
            }

            var progress = RangedSmackProfile.TracerProgress(smackElapsed, separation);
            if (progress < 1f)
            {
                var dashLength = Mathf.Max(24f, pathLength * RangedSmackProfile.TracerLengthFraction);
                var head = Vector2.Lerp(from, to, progress);
                var centre = head - (path.normalized * (dashLength * 0.5f));
                const float thickness = 6f;
                tracer.style.display = DisplayStyle.Flex;
                tracer.style.left = centre.x - (dashLength * 0.5f);
                tracer.style.top = centre.y - (thickness * 0.5f);
                tracer.style.width = dashLength;
                tracer.style.height = thickness;
                tracer.style.opacity = 0.92f;
                tracer.style.rotate = new Rotate(
                    new Angle(Mathf.Atan2(path.y, path.x) * Mathf.Rad2Deg, AngleUnit.Degree));
            }
            else
            {
                Hide(tracer);
            }

            var strength = RangedSmackProfile.ImpactStrength(smackElapsed, separation);
            if (strength <= 0f)
            {
                Hide(impact);
                return;
            }

            // The mark the arrival leaves: a ring on the defender that opens and fades, so the moment
            // the shot lands is a moment rather than a state change nobody saw.
            var side = Mathf.Lerp(96f, 30f, strength);
            impact.style.display = DisplayStyle.Flex;
            impact.style.left = to.x - (side * 0.5f);
            impact.style.top = to.y - (side * 0.5f);
            impact.style.width = side;
            impact.style.height = side;
            impact.style.opacity = strength;
            HudElements.Border(impact, MatchHudTheme.ShineRim, 4f, side * 0.5f);
        }

        private static void Hide(VisualElement element)
        {
            if (element != null && element.style.display != DisplayStyle.None)
            {
                element.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// Puts everything back: the camera to its captured rest pose, the HUD's panels to the display
        /// they had, and this view's own layer out of the tree. Idempotent, and safe to call on a view
        /// that never opened.
        /// </summary>
        public void Retire()
        {
            builtSequence = 0;
            isShowing = false;
            RestoreChrome();
            if (layer != null)
            {
                layer.style.display = DisplayStyle.None;
            }

            if (!hasCapturedRest)
            {
                return;
            }

            var camera = CameraSource?.Invoke();
            if (camera != null)
            {
                camera.orthographic = restOrthographic;
                if (restOrthographic)
                {
                    camera.orthographicSize = restOrthographicSize;
                }

                camera.transform.position = restPosition;
                camera.transform.rotation = restRotation;
            }

            hasCapturedRest = false;
            BoardFramingRestored?.Invoke();
        }

        // ---- The board pieces.

        private bool TryResolvePieces(
            AttackSpotlightScene scene, out Vector3 attacker, out Vector3 defender)
        {
            attacker = default;
            defender = default;
            return TryResolve(scene.Attacker, out attacker) && TryResolve(scene.Defender, out defender);
        }

        private bool TryResolve(KineticSubject subject, out Vector3 world)
        {
            world = default;
            if (subject.Kind == KineticSubjectKind.Hero)
            {
                var heroes = board.HeroViews;
                for (var index = 0; index < heroes.Count; index++)
                {
                    var hero = heroes[index];
                    if (hero == null || !hero.Occupancy.HeroId.Equals(subject.HeroId))
                    {
                        continue;
                    }

                    world = ToWorld(
                        hero.transform,
                        hero.Coordinate,
                        hero.OuterRadius,
                        hero.VerticalOffset);
                    return true;
                }

                return false;
            }

            if (subject.Kind == KineticSubjectKind.Minion)
            {
                var minions = board.MinionViews;
                for (var index = 0; index < minions.Count; index++)
                {
                    var minion = minions[index];
                    if (minion == null || minion.Occupancy == null ||
                        !minion.Occupancy.MinionId.Equals(subject.MinionId))
                    {
                        continue;
                    }

                    world = ToWorld(
                        minion.transform,
                        minion.Occupancy.Coordinate,
                        minion.OuterRadius,
                        StandardMinionView.DefaultVerticalOffset);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Where a piece AUTHORITATIVELY stands, recomputed rather than read off its transform.
        ///
        /// <para>Milestone 8.35y's kinetics displace a piece's transform mid-lunge, so reading
        /// <c>transform.position</c> would make the camera chase the smack it is supposed to be
        /// framing. This is the same arithmetic <c>PrimitiveHeroView.ApplyLayout</c> and
        /// <c>StandardMinionView.Configure</c> use, which is also why an undo or a resync resolves the
        /// framing on the very next frame with nothing to unwind.</para>
        /// </summary>
        private static Vector3 ToWorld(
            Transform pieceTransform, HexCoordinate coordinate, float outerRadius, float verticalOffset)
        {
            var local = HexWorldLayout.ToWorld(coordinate, outerRadius) + (Vector3.up * verticalOffset);
            var parent = pieceTransform == null ? null : pieceTransform.parent;
            return parent == null ? local : parent.TransformPoint(local);
        }

        private float PieceRadius()
        {
            var heroes = board.HeroViews;
            for (var index = 0; index < heroes.Count; index++)
            {
                if (heroes[index] != null)
                {
                    return Mathf.Max(0.1f, heroes[index].OuterRadius);
                }
            }

            return HexWorldLayout.DefaultOuterRadius;
        }

        // ---- The HUD's own panels.

        /// <summary>
        /// The amendment's "lose the UI". Every child of the HUD root except this view's own layer is
        /// hidden, and the display each one had is remembered so a panel that was already hidden -
        /// the Director drawer, a closed log - comes back hidden rather than being switched on by a
        /// restore that assumed everything was visible.
        /// </summary>
        private void ApplyChrome(float weight)
        {
            var root = hud?.RootElement;
            if (root == null)
            {
                return;
            }

            if (weight < 0.5f && hiddenChrome.Count == 0)
            {
                // Still travelling out. The chrome goes at the halfway point so it does not vanish
                // before the camera has begun to move.
                return;
            }

            if (weight < 0.5f)
            {
                RestoreChrome();
                return;
            }

            if (hiddenChrome.Count > 0)
            {
                return;
            }

            for (var index = 0; index < root.childCount; index++)
            {
                var child = root[index];
                if (child == null || child.name == LayerName)
                {
                    continue;
                }

                hiddenChrome.Add(child);
                hiddenChromeStyles.Add(child.style.display);
                child.style.display = DisplayStyle.None;
            }
        }

        private void RestoreChrome()
        {
            for (var index = 0; index < hiddenChrome.Count; index++)
            {
                var element = hiddenChrome[index];
                if (element == null)
                {
                    continue;
                }

                element.style.display = hiddenChromeStyles[index];
            }

            hiddenChrome.Clear();
            hiddenChromeStyles.Clear();
        }

        // ---- The spotlight's own layer.

        private VisualElement EnsureLayer()
        {
            var root = hud?.RootElement;
            if (root == null)
            {
                return null;
            }

            if (layer != null && layer.parent == root)
            {
                layer.style.display = DisplayStyle.Flex;
                layer.BringToFront();
                return layer;
            }

            layer = new VisualElement
            {
                name = LayerName,
                // The layer itself never intercepts a click; the CONTINUE button inside it still does,
                // because PickingMode.Ignore excludes only the element it is set on.
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.top = 0f;
            layer.style.right = 0f;
            layer.style.bottom = 0f;

            attackerCardHost = NewCardHost(AttackerCardName);
            defenderCardHost = NewCardHost(DefenderCardName);
            layer.Add(attackerCardHost);
            layer.Add(defenderCardHost);

            // The ranged smack's two marks. They are drawn on the spotlight's own layer in PANEL
            // space rather than as world objects, and under an orthographic camera that is not an
            // approximation: the projection is linear, so a straight line between two pieces'
            // projected points is exactly where a world-space line between them would appear.
            tracer = new VisualElement
            {
                name = TracerName, pickingMode = PickingMode.Ignore, focusable = false
            };
            tracer.style.position = Position.Absolute;
            tracer.style.backgroundColor = MatchHudTheme.ShineRim;
            tracer.style.display = DisplayStyle.None;
            layer.Add(tracer);

            impact = new VisualElement
            {
                name = ImpactName, pickingMode = PickingMode.Ignore, focusable = false
            };
            impact.style.position = Position.Absolute;
            impact.style.backgroundColor = Color.clear;
            impact.style.display = DisplayStyle.None;
            layer.Add(impact);

            caption = new Label
            {
                name = CaptionName,
                pickingMode = PickingMode.Ignore
            };
            caption.style.position = Position.Absolute;
            caption.style.left = 0f;
            caption.style.right = 0f;
            caption.style.unityTextAlign = TextAnchor.MiddleCenter;
            caption.style.color = MatchHudTheme.PrimaryText;
            caption.style.fontSize = 26f;
            caption.style.unityFontStyleAndWeight = FontStyle.Bold;
            layer.Add(caption);

            continueControl = HudElements.Control("CONTINUE", () => ContinueRequested?.Invoke());
            continueControl.name = ContinueName;
            continueControl.style.position = Position.Absolute;
            continueControl.style.width = 260f;
            layer.Add(continueControl);

            root.Add(layer);
            layer.BringToFront();
            return layer;
        }

        private static VisualElement NewCardHost(string name)
        {
            var host = new VisualElement
            {
                name = name,
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            host.style.position = Position.Absolute;
            return host;
        }

        private void BuildScene(AttackSpotlightScene scene)
        {
            if (layer == null)
            {
                return;
            }

            BuildFace(attackerCardHost, scene.AttackCard);
            BuildFace(defenderCardHost, scene.DefenseCard);
            if (caption != null)
            {
                caption.text = scene.WasDefended
                    ? scene.DefenderName.ToUpperInvariant() + " DEFENDED"
                    : scene.AttackerName.ToUpperInvariant() + " HIT " +
                      scene.DefenderName.ToUpperInvariant();
            }
        }

        private static void BuildFace(VisualElement host, TrayCardViewModel card)
        {
            if (host == null)
            {
                return;
            }

            host.Clear();
            if (card == null)
            {
                host.style.display = DisplayStyle.None;
                return;
            }

            host.style.display = DisplayStyle.Flex;
            var face = new VisualElement
            {
                pickingMode = PickingMode.Ignore,
                focusable = false
            };
            // The one shared complete-card builder, so a face in a spotlight and the same face in the
            // tray cannot drift into two different cards.
            ConfiguredCardFaceView.Build(face, card);
            host.Add(face);
        }

        private void LayoutCards(
            Camera camera, float weight, Vector3 attackerWorld, Vector3 defenderWorld, bool framed)
        {
            if (layer == null)
            {
                return;
            }

            layer.style.opacity = weight;
            var viewport = MatchHudPanelScaling.CurrentReferenceViewport();
            if (continueControl != null)
            {
                continueControl.style.left = (viewport.x - 260f) * 0.5f;
                continueControl.style.top = viewport.y - 120f;
                continueControl.SetEnabled(model.IsContinueArmed);
            }

            if (caption != null)
            {
                caption.style.top = 64f;
            }

            if (!framed)
            {
                return;
            }

            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);
            var attackerPoint = ToPanelPoint(camera, attackerWorld);
            var defenderPoint = ToPanelPoint(camera, defenderWorld);
            if (!AttackSpotlightCardLayout.TryPlace(
                    attackerPoint, defenderPoint, viewport, cardSize,
                    out var attackerRect, out var defenderRect))
            {
                attackerCardHost.style.display = DisplayStyle.None;
                defenderCardHost.style.display = DisplayStyle.None;
                return;
            }

            Place(attackerCardHost, attackerRect, model.Scene?.AttackCard != null);
            Place(defenderCardHost, defenderRect, model.Scene?.DefenseCard != null);
        }

        private static void Place(VisualElement host, Rect rect, bool present)
        {
            if (host == null)
            {
                return;
            }

            if (!present)
            {
                host.style.display = DisplayStyle.None;
                return;
            }

            host.style.display = DisplayStyle.Flex;
            host.style.left = rect.x;
            host.style.top = rect.y;
            host.style.width = rect.width;
            host.style.height = rect.height;
        }

        private static Vector2 ToPanelPoint(Camera camera, Vector3 world)
        {
            var screen = camera.WorldToScreenPoint(world);
            return MatchHudPanelScaling.ScreenToPanelPoint(
                new Vector2(screen.x, screen.y), Screen.width, Screen.height);
        }
    }

    /// <summary>
    /// Milestone 8.35z12. The spotlight's own frame loop, on the same terms as
    /// <see cref="KineticCueDriver"/>: it isolates its tick and disables itself after a small budget
    /// of failures, so a defect here costs the spotlight and leaves the board exactly the
    /// projection-change redraw it did before this milestone.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AttackSpotlightDriver : MonoBehaviour
    {
        public const string DriverObjectName = "Attack Spotlight Driver";

        /// <summary>How many ticks may throw before the driver stops trying.</summary>
        public const int FailureBudget = 3;

        private AttackSpotlightPresenter presenter;
        private AttackSpotlightUnityView view;
        private int failures;

        public static AttackSpotlightDriver Attach(
            GameObject host, AttackSpotlightPresenter presenter, AttackSpotlightUnityView view)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (presenter == null) throw new ArgumentNullException(nameof(presenter));
            if (view == null) throw new ArgumentNullException(nameof(view));
            var driver = host.GetComponent<AttackSpotlightDriver>();
            if (driver == null)
            {
                driver = host.AddComponent<AttackSpotlightDriver>();
            }

            driver.presenter = presenter;
            driver.view = view;
            driver.failures = 0;
            driver.enabled = true;
            return driver;
        }

        public int FailureCount => failures;

        private void Update()
        {
            if (presenter == null || view == null)
            {
                return;
            }

            try
            {
                presenter.Tick(Time.deltaTime);
                view.ApplyFrame(Time.deltaTime);
            }
            catch (Exception error)
            {
                failures++;
                Debug.LogError(
                    "GUARDS_HUD: SPOTLIGHT_TICK_FAILED failures=" + failures);
                Debug.LogException(error);
                if (failures < FailureBudget)
                {
                    return;
                }

                // Give the surface back before going quiet: a disabled driver must not leave the
                // camera framed on two pieces with the HUD hidden.
                try
                {
                    view.Retire();
                }
                catch (Exception restoreError)
                {
                    Debug.LogException(restoreError);
                }

                enabled = false;
            }
        }

        private void OnDisable()
        {
            if (view == null)
            {
                return;
            }

            try
            {
                view.Retire();
            }
            catch (Exception error)
            {
                Debug.LogException(error);
            }
        }
    }
}
