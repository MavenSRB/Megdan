using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Which cards the scene deals when it composes. Milestone 8.4.
    /// </summary>
    public enum SceneCardContent
    {
        /// <summary>
        /// The configured match: two mirrored Wasps dealt their verified printed set. This is what a
        /// player - and the built player the smoke gate runs - always gets.
        /// </summary>
        ConfiguredMatch = 0,

        /// <summary>
        /// The mechanics-showcase hands, for the Play Mode fixtures whose subject is what a real scene
        /// draws for a composed Skill: greyed controls, board cues, effect-choice destinations,
        /// generated token views. Verified content cannot stand in for them, because Wasp's own text
        /// lands at the 8.6-8.13 slices and until then every printed primary of hers is refused.
        /// </summary>
        MechanicsShowcaseFixture = 1,

        /// <summary>
        /// The verified starting hands with both Wasps restaged already engaged - each adjacent to one
        /// Blue melee Battle-Zone minion, two spaces from each other around it - for the milestone
        /// 8.15 player-pipeline journeys whose subject is a played card and not the approach march.
        /// Cards, values and rules are exactly <see cref="ConfiguredMatch"/>'s; only the two heroes'
        /// staged spaces differ, and the Tie Breaker side is fixed under `DEV-005` so a journey is
        /// deterministic.
        /// </summary>
        VerifiedEngagedMirror = 2,

        /// <summary>
        /// The engaged mirror at rulebook p.15's Tier II stage: complementary Tier II hands between
        /// the seats, returned Tier I cards in the deck. Milestone 8.15, on `DEV-004` terms - a live
        /// match cannot be seeded to this position, so the journeys that verify the upgraded cards
        /// in play start from the position p.15's own procedure produces.
        /// </summary>
        VerifiedTierTwoEngagedMirror = 3,

        /// <summary>
        /// The engaged mirror at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats. Milestone 8.15, on the same terms.
        /// </summary>
        VerifiedTierThreeEngagedMirror = 4
    }

    [ExecuteAlways]
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class MatchSceneCompositionRoot : MonoBehaviour
    {
        [SerializeField] private PlaceholderTestGridView mapView;
        [SerializeField] private PrimitiveHeroPointerInput pointerInput;
        [SerializeField] private Camera eventCamera;
        [SerializeField] private PrimitiveHeroView[] heroViews;
        [SerializeField] private PrimitiveHeroPointerSelector[] pointerSelectors;
        [SerializeField] private Material assignedHighlightMaterial;

        [Tooltip(
            "Milestone 6.5c: the second board highlight, marking where a projected Push would leave " +
            "its target. Left unassigned it is generated in its own dark color, so it never reads " +
            "as a space the player may click.")]
        [SerializeField] private Material assignedResultingSpaceMaterial;

        [Tooltip(
            "Compose the board while the editor is not playing. Off by default: edit-mode composition " +
            "regenerates the full board on every domain reload and can serialize generated objects " +
            "into the saved scene. Enable only to preview, and do not save the scene while enabled.")]
        [SerializeField] private bool composeInEditMode;

        private DestinationHighlightMaterialOwner highlightOwner;
        private DestinationHighlightMaterialOwner resultingSpaceHighlightOwner;
        private ConfiguredMatchHudRenderer hud;
        private ConfiguredMatchSceneRuntime runtime;

        /// <summary>
        /// What the next composition deals, and nothing after it. Milestone 8.4.
        ///
        /// <para>The scene deals the configured match, and until milestone 8.25 replaces this
        /// composition with the official six-player setup there is no authored place to say otherwise.
        /// A Play Mode fixture that needs the mechanics-showcase Skills in a real scene states it here
        /// before loading the scene, and the composition <b>consumes and resets it</b>, so a fixture
        /// that forgets to clear it cannot change what any later scene load deals. That one-shot
        /// property is the whole reason this is a value rather than a mode: nothing can be left on.</para>
        ///
        /// <para>A launched player never writes it, exactly as a launched player never constructs
        /// <see cref="PlayerSmokeTestDriver"/>; it is the same internal development affordance
        /// `DEV-004` covers.</para>
        /// </summary>
        public static SceneCardContent NextComposedCardContent { get; set; }

        public ConfiguredMatchSceneRuntime Runtime => runtime;
        public IMatchSession Session => runtime?.Session;
        public ConfiguredMatchHudRenderer Hud => hud;
        public bool OwnsGeneratedHighlightMaterial => highlightOwner?.IsGenerated == true;

        /// <summary>
        /// Both generated highlights are owned here and disposed together, so the second one cannot
        /// become the leak the first one is tested against.
        /// </summary>
        public int OwnedHighlightNativeResourceCount =>
            (highlightOwner?.OwnedNativeResourceCount ?? 0) +
            (resultingSpaceHighlightOwner?.OwnedNativeResourceCount ?? 0);

        public void Compose()
        {
            if (runtime != null)
            {
                return;
            }

            mapView ??= FindAnyObjectByType<PlaceholderTestGridView>();
            pointerInput ??= FindAnyObjectByType<PrimitiveHeroPointerInput>();
            eventCamera ??= Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
            heroViews = FindObjectsByType<PrimitiveHeroView>();
            pointerSelectors = FindObjectsByType<PrimitiveHeroPointerSelector>();

            if (mapView == null || eventCamera == null || pointerInput == null || heroViews.Length == 0)
            {
                throw new InvalidOperationException(
                    "The configured match composition root requires a map, camera, pointer input, and hero views.");
            }

            var catalog = OfficialBoardMapCatalog.Load();
            var map = catalog.Load(OfficialBoardMapCatalog.SelectedMap);
            // Five cards per hero, which is the shape every hand in this project has been specified at
            // since milestone 3.7b. Milestone 6.2d had dealt a sixth here - milestone 6.2's Cache card -
            // because nothing else could put a token on the board, and without one 6.3's Clear and two
            // of 6.6a's three modes were controls with nothing to act on. Blue Sentinel and Blue Feint
            // carry those capabilities now, so the scene keeps them at five.
            var content = TakeRequestedCardContent();
            var configuration = content switch
            {
                SceneCardContent.MechanicsShowcaseFixture =>
                    PlaceholderOneVsOneTestConfiguration.CreateForShowcaseFixture(),
                SceneCardContent.VerifiedEngagedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierTwoMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                        TeamSide.Red),
                _ => PlaceholderOneVsOneTestConfiguration.Create()
            };
            var setup = QuickGameSetupAuthority.Create(
                QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            if (content == SceneCardContent.VerifiedEngagedMirror ||
                content == SceneCardContent.VerifiedTierTwoEngagedMirror ||
                content == SceneCardContent.VerifiedTierThreeEngagedMirror)
            {
                setup = EngagedMirrorStaging.Restage(map, setup, out _);
            }
            mapView.ConfiguredMinionViewsChanged -= RebindConfiguredMinionViews;
            mapView.ConfiguredMinionViewsChanged += RebindConfiguredMinionViews;
            mapView.ConfigureSceneBindings(eventCamera, heroViews);
            mapView.Rebuild(map, setup);

            // Use the sets the rebuild published rather than re-discovering children. In Play Mode a
            // retired view is destroyed with the deferred Destroy and remains reachable through a
            // child search until the end of the frame, so re-discovery admits doomed objects into
            // authoritative board state. See milestone 6.1e / finding F613-05.
            var tiles = mapView.CurrentTiles.ToArray();
            var minions = mapView.CurrentMinionViews.ToArray();
            var obstacles = mapView.CurrentObstacles.ToArray();
            highlightOwner = new DestinationHighlightMaterialOwner(assignedHighlightMaterial);
            resultingSpaceHighlightOwner = DestinationHighlightMaterialOwner.ForResultingSpace(
                assignedResultingSpaceMaterial);
            hud = GetComponent<ConfiguredMatchHudRenderer>();
            if (hud == null)
            {
                hud = gameObject.AddComponent<ConfiguredMatchHudRenderer>();
            }

            runtime = new ConfiguredMatchSceneRuntime(
                map,
                setup,
                configuration,
                catalog.Definitions,
                heroViews,
                minions,
                tiles,
                obstacles,
                highlightOwner.Material,
                hud,
                resultingSpaceHighlightOwner.Material,
                // Milestone 6.2d: where a view for a newly placed token comes from. The map view owns
                // the outer radius and keeps every generated board object, so it builds them for the
                // same reason it builds tiles and minions rather than the board view doing it.
                mapView.CreateTokenView,
                // Milestone 8.11a: each highlight's perimeter, in its own colour. The owners generate
                // and dispose them beside their fills, so the pair is counted and released together.
                highlightOwner.RimMaterial,
                resultingSpaceHighlightOwner.RimMaterial);

            foreach (var selector in pointerSelectors.Where(value => value != null))
            {
                selector.Configure(runtime.BoardView);
            }
            var minionSelectors = ConfigureMinionSelectors(minions);

            pointerInput.Configure(
                eventCamera,
                pointerSelectors,
                minionSelectors,
                runtime.BoardView,
                tiles,
                hud.BlocksPointerAt);
        }

        private void RebindConfiguredMinionViews(IReadOnlyList<StandardMinionView> minionViews)
        {
            if (runtime == null)
            {
                return;
            }

            var currentViews = (minionViews ?? Array.Empty<StandardMinionView>())
                .Where(value => value != null)
                .Distinct()
                .ToArray();
            runtime.BoardView.ReplaceMinionViews(currentViews);
            pointerInput.ReplaceMinionSelectors(ConfigureMinionSelectors(currentViews));
        }

        private StandardMinionPointerSelector[] ConfigureMinionSelectors(
            IEnumerable<StandardMinionView> minionViews)
        {
            var currentSelectors = (minionViews ?? Array.Empty<StandardMinionView>())
                .Where(value => value != null)
                .Select(view => view.GetComponent<StandardMinionPointerSelector>() ??
                    view.gameObject.AddComponent<StandardMinionPointerSelector>())
                .ToArray();
            foreach (var selector in currentSelectors)
            {
                selector.Configure(runtime.BoardView);
            }

            return currentSelectors;
        }

        /// <summary>
        /// Reads <see cref="NextComposedCardContent"/> and puts it back to the configured match, so the
        /// request applies to exactly the composition that consumed it.
        /// </summary>
        private static SceneCardContent TakeRequestedCardContent()
        {
            var requested = NextComposedCardContent;
            NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            return requested;
        }

        private void Awake()
        {
            // Edit-mode composition is opt-in. Running it on every domain reload regenerates the
            // whole board and lets generated objects reach the saved scene, which is what left the
            // seven authored tiles behind. See milestone 6.1e / finding F613-04.
            if (!UnityEngine.Application.isPlaying && !composeInEditMode)
            {
                return;
            }

            Compose();
            AttachSmokeDriverWhenRequested();
        }

        /// <summary>
        /// Only a player launched with the smoke flag ever constructs the driver. A normally
        /// launched build has no test hook in it at all.
        /// </summary>
        private void AttachSmokeDriverWhenRequested()
        {
            if (!UnityEngine.Application.isPlaying || !PlayerSmokeTestDriver.IsRequested())
            {
                return;
            }

            var driver = gameObject.AddComponent<PlayerSmokeTestDriver>();
            driver.Configure(this);
        }

        private void OnDestroy()
        {
            if (mapView != null)
            {
                mapView.ConfiguredMinionViewsChanged -= RebindConfiguredMinionViews;
            }
            runtime = null;
            highlightOwner?.Dispose();
            highlightOwner = null;
            resultingSpaceHighlightOwner?.Dispose();
            resultingSpaceHighlightOwner = null;
        }
    }
}
