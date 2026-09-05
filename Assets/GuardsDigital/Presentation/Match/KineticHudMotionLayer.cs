using System;
using System.Collections.Generic;
using GuardsDigital.Core.Units.Heroes;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// Milestone 8.35z11. Principle 1 on the HUD: the cards and coins travel.
    ///
    /// <para><b>Two techniques, chosen per journey rather than uniformly.</b> A journey BETWEEN two
    /// places flies a ghost - a plain primitive rectangle or disc on an overlay above the HUD - because
    /// the real element does not exist at both ends. A journey that happens IN PLACE (a reveal turning
    /// over, a Level popping) transforms the REAL element instead, because the real element is already
    /// there showing the right thing and a ghost over it would be a second copy of the same card. The
    /// in-place half borrows the element's transform and gives it back, exactly as
    /// <c>KineticCueUnityView</c> borrows a piece's.</para>
    ///
    /// <para><b>It owns nothing the HUD owns.</b> Ghosts live on its own overlay and are destroyed with
    /// the flight; borrowed elements are restored the instant a flight ends, and a HUD that rebuilt
    /// mid-flight simply leaves a restore with nothing to restore - the fresh element was built correct
    /// and is never touched. That is why a rebuild during a flight costs a flourish and can never leave
    /// a panel half-faded.</para>
    /// </summary>
    public sealed class KineticFlightLayer
    {
        public const string LayerName = "gd-kinetic-flight";
        public const string GhostName = "gd-kinetic-ghost";

        /// <summary>
        /// The most ghosts on screen at once. Six coins from one defeat plus a card in flight is the
        /// realistic worst case; this is the hard stop above it, and it is here for the same reason
        /// <c>KineticTiming.MaxConcurrentCues</c> is - the amendment gates this pass on the recorded
        /// perf thresholds.
        /// </summary>
        public const int MaxGhosts = 10;

        /// <summary>The size of a flying card ghost when no destination slot could be measured.</summary>
        public const float DefaultCardWidth = 52f;
        public const float DefaultCardHeight = 72f;
        public const float CoinDiameter = 14f;

        private readonly Func<VisualElement> rootSource;
        private readonly Func<Camera> cameraSource;
        private readonly Func<HeroId, Vector3?> boardPositionOf;
        private readonly List<Flight> flights = new List<Flight>();
        private VisualElement layer;

        /// <param name="boardPositionOf">
        /// Where a hero's miniature stands in world units, or null when it cannot be located. This is
        /// what gives <see cref="KineticSpaceBridge"/> something to convert: coins are won on the BOARD
        /// and land in a tray on the HUD, so their journey is the one that genuinely crosses the two
        /// coordinate systems.
        /// </param>
        public KineticFlightLayer(
            Func<VisualElement> rootSource,
            Func<Camera> cameraSource = null,
            Func<HeroId, Vector3?> boardPositionOf = null)
        {
            this.rootSource = rootSource ?? throw new ArgumentNullException(nameof(rootSource));
            this.cameraSource = cameraSource ?? (() => Camera.main);
            this.boardPositionOf = boardPositionOf;
        }

        /// <summary>How many journeys are drawing right now. Zero on a quiet HUD, and asserted by a case.</summary>
        public int ActiveFlightCount => flights.Count;

        private sealed class Flight
        {
            public KineticCueKind Kind;
            public Vector2 From;
            public Vector2 To;
            public float Duration;

            /// <summary>
            /// This flight's OWN clock.
            ///
            /// <para><b>It is deliberately not the conductor's scene clock, and the difference is a
            /// defect avoided rather than a preference.</b> Reading the active scene's elapsed time
            /// would tie a flight's progress to which scene is playing: on the frame the conductor
            /// releases one scene and starts the next it resets that clock to zero, and any flight not
            /// yet finished would snap back to its origin and fly a second time. Per-flight elapsed has
            /// no such coupling - every journey runs its own duration out and ends, whatever the queue
            /// does around it.</para>
            /// </summary>
            public float Elapsed;

            public VisualElement Ghost;

            /// <summary>The real element an in-place motion borrowed, or null for a ghost flight.</summary>
            public VisualElement Borrowed;
            public StyleScale BorrowedScale;
            public StyleFloat BorrowedOpacity;

            /// <summary>A destination hidden while something flies to it, so it lands rather than appears.</summary>
            public VisualElement Hidden;
            public StyleFloat HiddenOpacity;
        }

        /// <summary>
        /// Starts every HUD journey in one scene. Anything whose anchors cannot be found is skipped
        /// silently - see <see cref="KineticFlightAnchors"/> for why that is a supported outcome and
        /// not a swallowed error.
        /// </summary>
        public void Begin(KineticScene scene, HeroId viewerHeroId)
        {
            if (scene == null)
            {
                return;
            }

            var root = rootSource();
            if (root == null)
            {
                return;
            }

            for (var index = 0; index < scene.Cues.Count; index++)
            {
                if (KineticCueFamilies.IsHudJourney(scene.Cues[index].Kind))
                {
                    BeginCue(root, scene.Cues[index], viewerHeroId);
                }
            }
        }

        private void BeginCue(VisualElement root, KineticCue cue, HeroId viewerHeroId)
        {
            if (cue.Subject.Kind != KineticSubjectKind.Hero)
            {
                return;
            }

            var heroId = cue.Subject.HeroId;
            switch (cue.Kind)
            {
                case KineticCueKind.CardCommit:
                {
                    var slot = KineticFlightAnchors.CurrentCardSlot(root, heroId);
                    var hand = KineticFlightAnchors.HandOrRail(root, heroId, viewerHeroId);
                    if (slot == null || hand == null)
                    {
                        return;
                    }

                    // The slot already holds its card back - the projection changed before this cue
                    // played. Hiding it for the length of the flight is what turns "a card appeared,
                    // and then a card flew at it" into one card arriving.
                    FlyGhost(cue, KineticSpaceBridge.CentreOf(hand), slot, CardGhost(slot), hide: slot);
                    return;
                }

                case KineticCueKind.CardRetrieve:
                {
                    var slot = KineticFlightAnchors.CurrentCardSlot(root, heroId);
                    var hand = KineticFlightAnchors.HandOrRail(root, heroId, viewerHeroId);
                    if (slot == null || hand == null)
                    {
                        return;
                    }

                    FlyGhost(
                        cue,
                        KineticSpaceBridge.CentreOf(slot),
                        hand,
                        CardGhost(slot),
                        hide: null);
                    return;
                }

                case KineticCueKind.CardReveal:
                {
                    Borrow(cue, KineticFlightAnchors.CurrentCardSlot(root, heroId));
                    return;
                }

                case KineticCueKind.LevelPop:
                {
                    Borrow(cue, KineticFlightAnchors.Level(root, heroId));
                    return;
                }

                case KineticCueKind.CoinFly:
                {
                    var coins = KineticFlightAnchors.Coins(root, heroId);
                    if (coins == null)
                    {
                        return;
                    }

                    // THE BRIDGE'S OWN CASE: coins are won on the BOARD - p.13's defeats - and land in
                    // a tray on the HUD, so this journey genuinely crosses the two spaces. When the
                    // hero's piece cannot be located on screen the coins fall back to setting out from
                    // the hero's own rail, which is a shorter but never a wrong statement.
                    var origin = BoardOriginFor(root, heroId);
                    var ghosts = cue.Amount > MaxGhosts ? MaxGhosts : cue.Amount;
                    for (var index = 0; index < ghosts; index++)
                    {
                        // A small spread, so several coins read as several coins rather than as one
                        // thick line.
                        var spread = new Vector2((index % 3) * 9f - 9f, (index / 3) * 9f - 4f);
                        FlyGhost(cue, origin + spread, coins, CoinGhost(), hide: null);
                    }

                    return;
                }
            }
        }

        /// <summary>
        /// Where a hero's coins set out from: their miniature on the board, converted into panel
        /// points through <see cref="KineticSpaceBridge"/>, and their own rail when the miniature
        /// cannot be located or is off screen.
        ///
        /// <para>The fallback is a shorter statement but never a wrong one - the coins still arrive in
        /// the right tray from the right hero - which is why it degrades rather than skipping the
        /// flight.</para>
        /// </summary>
        private Vector2 BoardOriginFor(VisualElement root, HeroId heroId)
        {
            var world = boardPositionOf?.Invoke(heroId);
            if (world.HasValue &&
                KineticSpaceBridge.TryBoardToPanel(cameraSource(), world.Value, out var onPanel))
            {
                return onPanel;
            }

            return KineticSpaceBridge.CentreOf(KineticFlightAnchors.HeroRail(root, heroId));
        }

        /// <summary>
        /// Starts a ghost journey. <paramref name="hide"/> is the destination to keep invisible until
        /// the ghost lands on it.
        /// </summary>
        private void FlyGhost(
            KineticCue cue, Vector2 from, VisualElement destination, VisualElement ghost,
            VisualElement hide)
        {
            if (flights.Count >= MaxGhosts || ghost == null)
            {
                return;
            }

            var host = EnsureLayer();
            if (host == null)
            {
                return;
            }

            host.Add(ghost);
            var flight = new Flight
            {
                Kind = cue.Kind,
                From = from,
                To = KineticSpaceBridge.CentreOf(destination),
                Duration = cue.DurationSeconds,
                Ghost = ghost
            };

            if (hide != null)
            {
                flight.Hidden = hide;
                flight.HiddenOpacity = hide.style.opacity;
                hide.style.opacity = 0f;
            }

            flights.Add(flight);
            Place(flight, 0f);
        }

        /// <summary>Starts an in-place motion on a real HUD element, remembering what to give back.</summary>
        private void Borrow(KineticCue cue, VisualElement element)
        {
            if (element == null || flights.Count >= MaxGhosts)
            {
                return;
            }

            var flight = new Flight
            {
                Kind = cue.Kind,
                From = Vector2.zero,
                To = Vector2.zero,
                Duration = cue.DurationSeconds,
                Borrowed = element,
                BorrowedScale = element.style.scale,
                BorrowedOpacity = element.style.opacity
            };

            flights.Add(flight);
            Place(flight, 0f);
        }

        /// <summary>
        /// Advances every running journey by one frame and ends those that have arrived.
        /// </summary>
        public void Advance(float deltaSeconds)
        {
            if (flights.Count == 0)
            {
                return;
            }

            // The same clamp KineticPlayback.Advance and KineticConductor.Advance apply, for the same
            // reason: a hitched frame must not teleport a 380ms arc past its own midpoint.
            var delta = float.IsNaN(deltaSeconds) || deltaSeconds <= 0f
                ? 0f
                : deltaSeconds > 0.25f ? 0.25f : deltaSeconds;

            for (var index = flights.Count - 1; index >= 0; index--)
            {
                var flight = flights[index];
                flight.Elapsed += delta;
                var progress = flight.Duration <= 0f
                    ? 1f
                    : KineticEasing.Clamp01(flight.Elapsed / flight.Duration);
                Place(flight, progress);

                if (progress >= 1f)
                {
                    End(flight);
                    flights.RemoveAt(index);
                }
            }
        }

        /// <summary>
        /// Ends everything at once and puts every borrowed element back.
        ///
        /// <para>The conductor asks for this on a snap - the first sight of a match, and a log that
        /// went backwards. A flight is the one thing in this pass that draws something the projection
        /// does not contain, so an undo has to be able to erase it in a single call.</para>
        /// </summary>
        public void CancelAll()
        {
            for (var index = 0; index < flights.Count; index++)
            {
                End(flights[index]);
            }

            flights.Clear();
        }

        private static void Place(Flight flight, float progress)
        {
            var sample = KineticFlightPath.Sample(flight.From, flight.To, progress, flight.Kind);

            if (flight.Borrowed != null)
            {
                if (flight.Borrowed.panel == null)
                {
                    return;
                }

                flight.Borrowed.style.scale = new Scale(
                    new Vector3(sample.Scale * sample.FaceScaleX, sample.Scale, 1f));
                return;
            }

            if (flight.Ghost == null)
            {
                return;
            }

            var width = flight.Ghost.resolvedStyle.width;
            var height = flight.Ghost.resolvedStyle.height;
            flight.Ghost.style.left = sample.Position.x - (width / 2f);
            flight.Ghost.style.top = sample.Position.y - (height / 2f);
            flight.Ghost.style.scale = new Scale(new Vector3(sample.Scale, sample.Scale, 1f));
            flight.Ghost.style.rotate = new Rotate(new Angle(sample.RotationDegrees, AngleUnit.Degree));
        }

        private static void End(Flight flight)
        {
            flight.Ghost?.RemoveFromHierarchy();

            if (flight.Borrowed != null && flight.Borrowed.panel != null)
            {
                flight.Borrowed.style.scale = flight.BorrowedScale;
                flight.Borrowed.style.opacity = flight.BorrowedOpacity;
            }

            if (flight.Hidden != null && flight.Hidden.panel != null)
            {
                flight.Hidden.style.opacity = flight.HiddenOpacity;
            }
        }

        private VisualElement EnsureLayer()
        {
            if (layer != null && layer.panel != null)
            {
                return layer;
            }

            var root = rootSource();
            if (root == null)
            {
                return null;
            }

            layer = new VisualElement
            {
                name = LayerName,
                pickingMode = PickingMode.Ignore
            };
            layer.style.position = Position.Absolute;
            layer.style.left = 0f;
            layer.style.top = 0f;
            layer.style.right = 0f;
            layer.style.bottom = 0f;
            root.Add(layer);
            return layer;
        }

        private static VisualElement CardGhost(VisualElement modelledOn)
        {
            var ghost = new VisualElement
            {
                name = GhostName,
                pickingMode = PickingMode.Ignore
            };
            ghost.style.position = Position.Absolute;

            var width = DefaultCardWidth;
            var height = DefaultCardHeight;
            if (KineticSpaceBridge.IsPlaced(modelledOn))
            {
                width = modelledOn.worldBound.width;
                height = modelledOn.worldBound.height;
            }

            ghost.style.width = width;
            ghost.style.height = height;
            ghost.style.backgroundColor = MatchHudTheme.PanelBackground;
            ghost.style.borderTopWidth = 1f;
            ghost.style.borderBottomWidth = 1f;
            ghost.style.borderLeftWidth = 1f;
            ghost.style.borderRightWidth = 1f;
            ghost.style.borderTopColor = MatchHudTheme.ShineRim;
            ghost.style.borderBottomColor = MatchHudTheme.ShineRim;
            ghost.style.borderLeftColor = MatchHudTheme.ShineRim;
            ghost.style.borderRightColor = MatchHudTheme.ShineRim;
            ghost.style.borderTopLeftRadius = 4f;
            ghost.style.borderTopRightRadius = 4f;
            ghost.style.borderBottomLeftRadius = 4f;
            ghost.style.borderBottomRightRadius = 4f;
            return ghost;
        }

        private static VisualElement CoinGhost()
        {
            var ghost = new VisualElement
            {
                name = GhostName,
                pickingMode = PickingMode.Ignore
            };
            ghost.style.position = Position.Absolute;
            ghost.style.width = CoinDiameter;
            ghost.style.height = CoinDiameter;
            ghost.style.backgroundColor = MatchHudTheme.ShineRim;
            ghost.style.borderTopLeftRadius = CoinDiameter / 2f;
            ghost.style.borderTopRightRadius = CoinDiameter / 2f;
            ghost.style.borderBottomLeftRadius = CoinDiameter / 2f;
            ghost.style.borderBottomRightRadius = CoinDiameter / 2f;
            return ghost;
        }
    }

    /// <summary>
    /// Milestone 8.35z11. Principle 5 - AMBIENT LIFE: "nothing is fully static or instant ...
    /// every panel/prompt/control EASES into existence".
    ///
    /// <para><b>The engine does the work, not a frame loop.</b> Each element this marks is given UI
    /// Toolkit's own USS transition on <c>opacity</c> and <c>scale</c> and then has those values moved
    /// once; Unity eases them natively. So a screen-wide floor of motion costs no per-frame C# at all,
    /// which is what makes it affordable against the Phase 4 p95 gate the amendment gates this pass
    /// on.</para>
    ///
    /// <para><b>THE TRAP THIS FILE IS SHAPED AROUND: the HUD rebuilds its elements wholesale on every
    /// projection change.</b> <c>BuildOverview</c> constructs a fresh rail per render, and the same is
    /// true across the HUD. So "has this element been eased yet?" cannot be answered by marking the
    /// element - every element is new on every refresh, and marking would ease the entire HUD several
    /// times a second, which is precisely the "busy" the principle forbids. What persists across a
    /// rebuild is the element's NAME, so entry is decided by a set difference over names: a panel whose
    /// name was not on screen last time is entering, and a panel that was there is not, however many
    /// times its element has been rebuilt underneath.</para>
    ///
    /// <para><b>Exits are a recorded gap, not an oversight.</b> Easing something OUT means delaying its
    /// removal, and these elements are removed by the views that own them during a rebuild this sweep
    /// only observes afterwards. Doing it properly means those views deferring their own removal - a
    /// change across <c>UiToolkitMatchHudViews</c>, which a parallel worker holds. Enters ease; exits
    /// are instant; the seam to fix it later is a view-side deferral and nothing here.</para>
    /// </summary>
    public sealed class HudAmbientLife
    {
        /// <summary>Put this on an element that must appear instantly.</summary>
        public const string OptOutClass = "gd-no-ambient";

        /// <summary>
        /// How big an element must be before it counts as a panel or a prompt rather than a chip.
        ///
        /// <para>Easing every label and value chip into existence would be motion for its own sake and
        /// would multiply the transition count by an order of magnitude. The things a player watches
        /// APPEAR are panels, prompts, banners and cards, and they are all comfortably above this.</para>
        /// </summary>
        public const float MinimumAreaPoints = 6000f;

        /// <summary>
        /// The easing modes the motion language permits on a UI transition.
        ///
        /// <para><b><see cref="EasingMode.Linear"/> is absent, and its absence is the audited fact.</b>
        /// The amendment's "no linear motion anywhere" cannot be checked by reading source for a word,
        /// so it is expressed as a set that a case asserts does not contain it and that every transition
        /// this file writes is drawn from.</para>
        /// </summary>
        public static readonly IReadOnlyList<EasingMode> AllowedEasingModes = new[]
        {
            EasingMode.EaseInOut,
            EasingMode.EaseInOutSine,
            EasingMode.EaseInOutCubic,
            EasingMode.EaseOut,
            EasingMode.EaseOutSine,
            EasingMode.EaseOutCubic
        };

        /// <summary>The mode a panel eases into existence with.</summary>
        public const EasingMode EnterEasing = EasingMode.EaseInOutSine;

        private HashSet<string> present = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> scratch = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>How many elements the last sweep eased into existence.</summary>
        public int LastEnterCount { get; private set; }

        /// <summary>
        /// Looks over the HUD and eases in whatever is newly there. Runs on a projection change - the
        /// same cadence the HUD itself rebuilds on - and never per frame.
        /// </summary>
        public int Sweep(VisualElement root)
        {
            LastEnterCount = 0;
            if (root == null)
            {
                return 0;
            }

            scratch.Clear();
            var entered = 0;
            Walk(root, ref entered);

            // Swap rather than copy: the scratch set becomes the record, and last round's record is
            // reused as next round's scratch. No allocation on a steady HUD.
            var previous = present;
            present = new HashSet<string>(scratch, StringComparer.Ordinal);
            previous.Clear();

            LastEnterCount = entered;
            return entered;
        }

        /// <summary>Forgets what was on screen, so the next sweep treats everything as entering.</summary>
        public void Reset()
        {
            present.Clear();
        }

        private void Walk(VisualElement element, ref int entered)
        {
            for (var index = 0; index < element.childCount; index++)
            {
                var child = element[index];
                if (child == null)
                {
                    continue;
                }

                if (Qualifies(child))
                {
                    scratch.Add(child.name);
                    if (!present.Contains(child.name))
                    {
                        EaseIn(child);
                        entered++;
                    }
                }

                Walk(child, ref entered);
            }
        }

        /// <summary>
        /// Whether an element is one of the things principle 5 is about.
        ///
        /// <para>It must be named (an unnamed element cannot be recognised across a rebuild, which is
        /// this design's whole identity mechanism), big enough to read as a panel, actually laid out,
        /// not opted out, and NOT already carrying a transition of its own - the last of which keeps
        /// this off every control milestone 8.35x's button pass already animates, rather than
        /// overwriting its hover with a fade.</para>
        /// </summary>
        public static bool Qualifies(VisualElement element)
        {
            if (element == null || string.IsNullOrEmpty(element.name) ||
                element.ClassListContains(OptOutClass) ||
                element is Button ||
                element.name == KineticFlightLayer.LayerName ||
                element.name == KineticFlightLayer.GhostName)
            {
                return false;
            }

            if (!KineticSpaceBridge.IsPlaced(element))
            {
                return false;
            }

            var bounds = element.worldBound;
            if (bounds.width * bounds.height < MinimumAreaPoints)
            {
                return false;
            }

            // An element the HUD is deliberately dimming (an unavailable card, a veiled group) owns its
            // own opacity, and easing it to 1 would erase a meaning the HUD is stating.
            if (element.resolvedStyle.opacity < 0.999f)
            {
                return false;
            }

            var transitions = element.style.transitionProperty;
            return transitions.keyword != StyleKeyword.Undefined || transitions.value == null ||
                transitions.value.Count == 0;
        }

        private static void EaseIn(VisualElement element)
        {
            element.style.transitionProperty = new List<StylePropertyName>
            {
                new StylePropertyName("opacity"),
                new StylePropertyName("scale")
            };
            element.style.transitionDuration = new List<TimeValue>
            {
                new TimeValue(KineticMotionDurations.PanelEnterSeconds, TimeUnit.Second)
            };
            element.style.transitionTimingFunction = new List<EasingFunction>
            {
                new EasingFunction(EnterEasing)
            };

            var start = 1f - KineticAmbientAmplitudes.PanelEnterScaleDrop;
            element.style.opacity = 0f;
            element.style.scale = new Scale(new Vector3(start, start, 1f));

            // One scheduled write on the next panel update is what actually starts the transition:
            // set both ends in the same frame and UI Toolkit sees no change to ease.
            element.schedule.Execute(() =>
            {
                if (element.panel == null)
                {
                    return;
                }

                element.style.opacity = 1f;
                element.style.scale = new Scale(Vector3.one);
            }).StartingIn(0);
        }
    }
}
