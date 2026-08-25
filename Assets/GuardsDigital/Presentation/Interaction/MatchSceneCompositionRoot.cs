using System;
using System.Collections.Generic;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
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
        /// The configured match: Wasp on Red and Arien on Blue, each dealt their verified printed set.
        /// This is what a player - and the built player the smoke gate runs - always gets.
        ///
        /// <para>Milestone 8.20 made it two heroes. It was two mirrored Wasps for as long as one
        /// verified set was all this build had; the mirror is still composed by
        /// <see cref="VerifiedEngagedMirror"/> and its two progressed stages, which the fixtures that
        /// measure one seat against its own reflection ask for by name.</para>
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
        VerifiedTierThreeEngagedMirror = 4,

        /// <summary>
        /// Milestone 8.20. The staged Wasp-versus-Arien hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedMirror"/> stages its two - same spaces, same
        /// minion, same fixed Tie Breaker side under `DEV-005` - so an Arien journey starts from a
        /// played card rather than from the approach march.
        /// </summary>
        VerifiedEngagedStagedArien = 5,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Arien holding the first sibling
        /// of each colour. `DEV-004` terms, for the reason the mirror's own progressed stages are on
        /// them.
        /// </summary>
        VerifiedTierTwoEngagedStagedArien = 6,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats - High Voltage in Wasp's area and Living Tsunami in Arien's.
        /// </summary>
        VerifiedTierThreeEngagedStagedArien = 7,

        /// <summary>
        /// The Tier II staging with Arien holding the <em>second</em> sibling of each colour - Rogue
        /// Wave, Slippery Ground and Arcane Whirlpool - which is the other half of p.15's choice and
        /// therefore the other half of his printed set.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedArien = 8,

        /// <summary>
        /// The Tier III staging with Arien holding the second sibling of each colour: Tidal Blast,
        /// Deluge and Ebb and Flow.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedArien = 9,

        /// <summary>
        /// Milestone 8.20. The verified Wasp mirror at p.6's own spawn positions - the scene the
        /// default arm composed before this milestone put Arien on the Blue seat.
        ///
        /// <para>It is named rather than gone because one fixture's subject genuinely is the mirror:
        /// the pointer-driven Attack that ends on p.14's Block beside numeric Defense choices needs a
        /// defender holding a Block, and Arien prints none - his three Duelist cards print p.14's other
        /// Defense shape instead. That scenario also drives an eight-turn approach from the spawns, so
        /// the engaged staging cannot stand in for it.</para>
        /// </summary>
        VerifiedMirror = 10,

        /// <summary>
        /// Milestone 8.21. The staged Wasp-versus-Brogan hands with both heroes restaged already
        /// engaged, exactly as <see cref="VerifiedEngagedStagedArien"/> stages its two - same spaces,
        /// same minion, same fixed Tie Breaker side under `DEV-005`.
        ///
        /// <para>Brogan takes the Blue seat rather than joining it. Milestone 8.25 owns the official
        /// six-player configuration, so a third dealt hero is a fixture's choice of which two play
        /// rather than a wider match, and <see cref="ConfiguredMatch"/> still deals Wasp against
        /// Arien.</para>
        /// </summary>
        VerifiedEngagedStagedBrogan = 11,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier II stage, with Brogan holding the first sibling
        /// of each colour - Bullrush, Mighty Punch and Bolster. `DEV-004` terms.
        /// </summary>
        VerifiedTierTwoEngagedStagedBrogan = 12,

        /// <summary>
        /// The Tier II staging with Brogan holding the <em>second</em> sibling of each colour -
        /// Throwing Axe, Shield Bash and War Drummer - which is the other half of p.15's choice and
        /// therefore the other half of his printed set.
        /// </summary>
        VerifiedTierTwoAlternateEngagedStagedBrogan = 13,

        /// <summary>
        /// The engaged staging at rulebook p.15's Tier III stage with p.16's Ultimate unlocked faceup
        /// on both seats - High Voltage in Wasp's area and One Man Army in Brogan's - and Brogan
        /// holding the first sibling of each colour.
        /// </summary>
        VerifiedTierThreeEngagedStagedBrogan = 14,

        /// <summary>
        /// The Tier III staging with Brogan holding the second sibling of each colour: Throwing Spear,
        /// Counterattack and Master Skald.
        /// </summary>
        VerifiedTierThreeAlternateEngagedStagedBrogan = 15,

        /// <summary>
        /// Milestone 8.21. Wasp on Red against Arien <em>and</em> Brogan on Blue - the one composition
        /// in this build that deals two heroes to a team.
        ///
        /// <para>It exists for two of Brogan's nineteen faces. War Drummer and Master Skald print "A
        /// friendly hero in range gains N coins", and rulebook p.10 keeps the acting hero out of what
        /// an action affects unless the text says so; with one hero per team that selection has no
        /// candidate at all, so those two cards could not be driven through the player pipeline in any
        /// other composition this build has.</para>
        ///
        /// <para>The three heroes stand at their own spawn points rather than restaged into an
        /// engagement. The staging exists to put a hero next to an enemy minion, and neither of these
        /// cards names an enemy: what they need is an ally in range, and the second Blue spawn is two
        /// spaces from the first - well inside their printed Range 4.</para>
        ///
        /// <para>Still not the six-player setup, which milestone 8.25 owns: Red fields one hero and
        /// <see cref="ConfiguredMatch"/> is untouched.</para>
        /// </summary>
        VerifiedStagedBroganWithAlly = 16,

        /// <summary>
        /// The same three-hero staging with Brogan's own seat dealt at rulebook p.15's Tier II stage
        /// on the second-sibling branch, which is the only position War Drummer is in his hand from.
        ///
        /// <para>It is a separate arm rather than a parameter of <see cref="VerifiedStagedBroganWithAlly"/>
        /// because a scene request names one composition: p.6 leaves War Drummer in the deck, so the
        /// starting-hand arm above cannot reach the card the ally seat exists for.</para>
        /// </summary>
        VerifiedStagedBroganWithAllyAtTierTwo = 17,

        /// <summary>
        /// The three-hero staging at rulebook p.15's Tier III stage on the second-sibling branch,
        /// where Master Skald is in Brogan's hand - the Tier III half of the same pair.
        /// </summary>
        VerifiedStagedBroganWithAllyAtTierThree = 18
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
                SceneCardContent.VerifiedEngagedMirror or SceneCardContent.VerifiedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierTwoMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierThreeEngagedMirror =>
                    PlaceholderOneVsOneTestConfiguration.CreateVerifiedTierThreeMirrorForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedEngagedStagedArien =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedArienForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedArien =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedArienTierTwoForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedArienTierTwoForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedArien =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedArienTierThreeForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedArien =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedArienTierThreeForInternalTest(
                        TeamSide.Red, GuardsDigital.Core.Cards.Verified.ArienUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedEngagedStagedBrogan =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedTierTwoEngagedStagedBrogan =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganTierTwoForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedTierThreeEngagedStagedBrogan =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.FirstSibling),
                SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrogan =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganTierThreeForInternalTest(
                        TeamSide.Red,
                        GuardsDigital.Core.Cards.Verified.BroganUpgradeBranch.SecondSibling),
                SceneCardContent.VerifiedStagedBroganWithAlly =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red),
                SceneCardContent.VerifiedStagedBroganWithAllyAtTierTwo =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red, BroganAllyStage.TierTwo),
                SceneCardContent.VerifiedStagedBroganWithAllyAtTierThree =>
                    PlaceholderOneVsOneTestConfiguration.CreateStagedBroganWithAllyForInternalTest(
                        TeamSide.Red, BroganAllyStage.TierThree),
                _ => PlaceholderOneVsOneTestConfiguration.Create()
            };
            // Milestone 8.20. The board's hero identities have to agree with the hands the
            // configuration above dealt: the Blue seat's content identity is what addresses its
            // prepared token and what the projection names it by, so a mirror board under an Arien
            // hand would draw Wasp's face on a hero holding Arien's cards. The three mirror arms keep
            // the mirror board; the default arm, which is what a player plays, takes the staged one.
            var setup = QuickGameSetupAuthority.Create(
                content == SceneCardContent.VerifiedEngagedMirror ||
                content == SceneCardContent.VerifiedTierTwoEngagedMirror ||
                content == SceneCardContent.VerifiedTierThreeEngagedMirror ||
                content == SceneCardContent.VerifiedMirror ||
                content == SceneCardContent.MechanicsShowcaseFixture
                    ? QuickGameSetupConfiguration.CreateMirroredWaspOneVsOne()
                    : IsBroganWithAlly(content)
                        ? QuickGameSetupConfiguration.CreateStagedWaspVersusArienAndBroganOneVsTwo()
                        : IsStagedBrogan(content)
                            ? QuickGameSetupConfiguration.CreateStagedWaspVersusBroganOneVsOne()
                            : QuickGameSetupConfiguration.CreateStagedWaspVersusArienOneVsOne(),
                configuration.Participants,
                map,
                configuration.TieBreakerState);
            // Milestone 8.20 adds the staged Arien arms to the same restaging. The staging is about
            // where the two heroes stand and what they stand beside, and it names neither of them - so
            // one hero on each side is the same engagement whichever printed sets they are holding.
            if (content == SceneCardContent.VerifiedEngagedMirror ||
                content == SceneCardContent.VerifiedTierTwoEngagedMirror ||
                content == SceneCardContent.VerifiedTierThreeEngagedMirror ||
                content == SceneCardContent.VerifiedEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierTwoEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierThreeEngagedStagedArien ||
                content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedArien ||
                IsStagedBrogan(content))
            {
                setup = EngagedMirrorStaging.Restage(map, setup, out _);
            }
            // Milestone 8.21. A configuration may deal a seat the scene authors no view for - the
            // staged Brogan-with-an-ally match deals three - so any such seat gets a generated view
            // and a generated pointer selector before the board is rebuilt. It is the same answer the
            // board already gives a placed token: authored content where there is some, generated
            // where a configuration asks for more, and no fixture reaching into the scene to add one.
            EnsureViewsForConfiguredSeats(setup);
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
        /// <summary>
        /// Milestone 8.21. Gives every configured seat a hero view and a pointer selector, generating
        /// one for any seat the scene does not author.
        ///
        /// <para>The scene authors one view per team, which is what every configuration before this
        /// milestone dealt. The staged match whose subject is a card naming a friendly hero deals a
        /// second Blue seat, and that seat needs the same two things every other has: a piece on the
        /// board and something a pointer ray can hit, or the journeys that prove those cards would be
        /// driving a hero nobody can click.</para>
        ///
        /// <para>The generated object carries no artwork of its own. <see cref="PrimitiveHeroView"/>
        /// builds its own mesh and materials from the team and the prepared token, exactly as an
        /// authored one does, so a generated seat and an authored seat are the same piece drawn the
        /// same way.</para>
        /// </summary>
        private void EnsureViewsForConfiguredSeats(QuickGameSetupState setup)
        {
            var generated = new List<PrimitiveHeroView>();
            var selectors = new List<PrimitiveHeroPointerSelector>();
            foreach (var seat in setup.Heroes)
            {
                if (heroViews.Any(view => view != null && view.Occupancy.HeroId.Equals(seat.HeroId)))
                {
                    continue;
                }

                var host = new GameObject($"generated-seat ({seat.HeroId.Value})");
                host.transform.SetParent(mapView.transform, false);
                var view = host.AddComponent<PrimitiveHeroView>();
                // The identity has to be set before the rebuild, because that is what the board
                // matches a configured seat to its view by - and it is set at the map's own outer
                // radius rather than at zero, because configuring a view lays it out immediately and
                // `HexWorldLayout.ToWorld` refuses a radius that is not a finite positive value. The
                // rebuild below configures it again at the same number, so this is the seat's real
                // geometry rather than a placeholder the rebuild has to correct.
                view.Configure(
                    new HeroOccupancy(seat.HeroId, seat.Coordinate), mapView.OuterRadius);
                generated.Add(view);
                selectors.Add(host.AddComponent<PrimitiveHeroPointerSelector>());
            }

            if (generated.Count == 0)
            {
                return;
            }

            heroViews = heroViews.Concat(generated).ToArray();
            pointerSelectors = (pointerSelectors ?? Array.Empty<PrimitiveHeroPointerSelector>())
                .Concat(selectors)
                .ToArray();
        }

        /// <summary>
        /// Whether this content deals Brogan on the Blue seat. Milestone 8.21, and it decides two
        /// things that have to agree: which hero identity the board binds - the content identity is
        /// what addresses the prepared token and what the projection names the piece by - and that the
        /// two heroes are restaged already engaged, which every staged arm is.
        /// </summary>
        private static bool IsStagedBrogan(SceneCardContent content) =>
            content == SceneCardContent.VerifiedEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierTwoEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierTwoAlternateEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierThreeEngagedStagedBrogan ||
            content == SceneCardContent.VerifiedTierThreeAlternateEngagedStagedBrogan;

        /// <summary>
        /// Whether this content deals two heroes to the Blue team with Brogan beside Arien. Milestone
        /// 8.21's three-hero arms, which take the two-hero setup and are deliberately <em>not</em>
        /// restaged: what those cards need is an ally in range, and the two Blue spawns already are.
        /// </summary>
        private static bool IsBroganWithAlly(SceneCardContent content) =>
            content == SceneCardContent.VerifiedStagedBroganWithAlly ||
            content == SceneCardContent.VerifiedStagedBroganWithAllyAtTierTwo ||
            content == SceneCardContent.VerifiedStagedBroganWithAllyAtTierThree;

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
