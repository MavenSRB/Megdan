using System.Collections;
using System.Linq;
using GuardsDigital.Application;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using static GuardsDigital.Tests.PlayMode.GarrusUiJourneyTests;
using static GuardsDigital.Tests.PlayMode.GarrusLifecycleJourneyTests;
using static GuardsDigital.Tests.PlayMode.BainUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.TaliUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.XargathaEngagedUiJourneyPlayModeTests;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class HeroTrayRefinementTests
    {
        [UnityTest]
        public IEnumerator BakedAndNeutralBoardHeroesShareOneRimWithoutTheBakedSourceEdge()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            var registered = RegisteredHeroRoster.ForThisBuild;
            RegisteredHero Hero(string id) => registered.Single(candidate => candidate.ContentId.Value == id);
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { Hero("nebkher"), Hero("wasp"), Hero("mrak") },
                new[] { Hero("hanu"), Hero("trinkets"), Hero("mortimer") });
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var runtime = Root().Runtime;
            foreach (var pair in new[]
            {
                (Content: "mrak", Kind: TokenFaceKind.TeamBaked),
                (Content: "nebkher", Kind: TokenFaceKind.TeamNeutral)
            })
            {
                var heroId = runtime.Host.Projection.Setup.Heroes.Single(candidate =>
                    candidate.ContentId.Value == pair.Content).HeroId;
                var token = runtime.BoardView.HeroViews.Single(candidate =>
                    candidate.Occupancy.HeroId.Equals(heroId));
                Assert.That(token.HasTokenVisual, Is.True, pair.Content + " prepared face is required here");
                var mesh = token.GetComponent<MeshFilter>().sharedMesh;
                var face = new Vector2(mesh.vertices[1].x, mesh.vertices[1].z).magnitude;
                var outside = new Vector2(mesh.vertices[1 + HeroTokenArtwork.Segments].x,
                    mesh.vertices[1 + HeroTokenArtwork.Segments].z).magnitude;
                Assert.That(outside - face, Is.EqualTo(0.15f).Within(0.0001f));
                Assert.That(mesh.uv[1].x,
                    Is.EqualTo(0.5f + 0.5f * HeroTokenArtwork.SourceInsetFor(pair.Kind)).Within(0.0001f));
                yield return BoardPieceCapturePlayModeTests.Capture(runtime.BoardView,
                    "tmp/999-ring-" + pair.Content, new[] { token.Coordinate });
                if (pair.Content == "mrak")
                {
                    var pixels = new Texture2D(2, 2);
                    try
                    {
                        Assert.That(ImageConversion.LoadImage(pixels,
                            System.IO.File.ReadAllBytes("tmp/999-ring-mrak-close.png")), Is.True);
                        // The close camera centers Mrak at 960,540 in its 1920x1080 image.
                        // Along the unobscured lower arc, every sample across the rim must read
                        // player yellow, not the source token's orange.
                        for (var radius = 150; radius <= 178; radius += 2)
                        {
                            var pixel = pixels.GetPixel(960, 540 - radius);
                            Assert.That(pixel.r > 0.86f && pixel.g > 0.75f && pixel.b < 0.4f,
                                Is.True, "yellow ring at lower radius " + radius + " was " + pixel);
                        }
                    }
                    finally
                    {
                        Object.Destroy(pixels);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator ShuffledLobbySeatsKeepFixedTrayOrderColorsAndPlayerNames()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            var registered = RegisteredHeroRoster.ForThisBuild;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { "wasp", "xargatha", "arien" }
                    .Select(id => registered.Single(hero => hero.ContentId.Value == id)),
                new[] { "trinkets", "mortimer", "hanu" }
                    .Select(id => registered.Single(hero => hero.ContentId.Value == id)));
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            UiPlayer.SetPlayerViewport(2560u, 1440u, "fixed hero tray positions");
            var root = Root();
            var runtime = root.Runtime;
            var seats = runtime.Host.Seats.Seats;
            var roster = seats.Select((seat, index) => new SeatRosterEntry(
                seat.Id, seat.HeroId, seat.TeamSide, SeatOccupancyState.Claimed,
                playerName: index == 0 ? "WWWMMMMMMMMMMMMM" : index == 1 ? "Maven" : "Player " + (index + 1))).ToArray();
            runtime.HeroOverviewsView.SetPlayerNames(roster);
            root.Hud.Layout();
            yield return null;
            yield return null;

            foreach (var width in new[] { 1280u, 1920u, 2560u })
            {
            UiPlayer.SetPlayerViewport(width, width * 9 / 16, "readable player nameplates");
            root.Hud.Layout(); yield return null; yield return null;
            foreach (var team in new[] { TeamSide.Red, TeamSide.Blue })
            {
                var column = root.Hud.RootElement.Q(team == TeamSide.Red
                    ? "gd-hero-rail-red" : "gd-hero-rail-blue");
                var expected = seats.Where(seat => seat.TeamSide == team)
                    .OrderBy(seat => seat.Id.Value).ToArray();
                var rails = column.Children().Where(element => element.name == "gd-hero-rail").ToArray();
                Assert.That(rails.Select(rail => (HeroId)rail.userData),
                    Is.EqualTo(expected.Select(seat => seat.HeroId)),
                    team + " 1/2/3 must stay top/middle/bottom regardless of selected hero names");
                for (var index = 0; index < rails.Length; index++)
                {
                    var rail = rails[index];
                    var seat = expected[index];
                    var accent = PlayerAccent.For(runtime.ReadModel.Participants, seat.HeroId);
                    Assert.That(team == TeamSide.Red ? rail.style.borderLeftColor.value :
                        rail.style.borderRightColor.value, Is.EqualTo(accent));
                    var plate = rail.Q<Label>("gd-hero-player-name");
                    var portrait = rail.Q("gd-hero-medallion");
                    var name = roster.Single(row => row.Seat.Equals(seat.Id)).PlayerName;
                    Assert.That(plate.text, Is.EqualTo(name));
                    Assert.That(plate.tooltip, Is.EqualTo(name));
                    Assert.That(rail.resolvedStyle.paddingTop, Is.EqualTo(12f),
                        "Names must not enlarge the original slim tray.");
                    Assert.That(plate.worldBound.yMin, Is.LessThan(rail.worldBound.yMin),
                        "The small banner straddles the top border.");
                    Assert.That(plate.worldBound.height, Is.EqualTo(16f).Within(0.1f));
                    Assert.That(plate.worldBound.yMax + 0.5f,
                        Is.LessThanOrEqualTo(rail.Q("gd-hero-identity-plate").worldBound.yMin),
                        "The banner stays clear of the identity panel, including scaled-pixel rounding.");
                    Assert.That(plate.worldBound.yMax, Is.LessThanOrEqualTo(portrait.worldBound.yMin + 1f));
                    Assert.That(team == TeamSide.Red
                        ? plate.worldBound.xMin <= portrait.worldBound.xMin
                        : plate.worldBound.xMax >= portrait.worldBound.xMax, Is.True,
                        "the label sits over the portrait toward its outer edge");
                }
            }
            yield return UiPlayer.CaptureScreen("tmp/w004-name-banner-" + width + ".png");
            }
            runtime.HeroOverviewsView.SetPlayerNames(System.Array.Empty<SeatRosterEntry>());
            Assert.That(root.Hud.RootElement.Query<Label>("gd-hero-player-name").ToList()
                .All(label => label.style.display.value == DisplayStyle.None), Is.True);
        }

        [UnityTest]
        public IEnumerator PlayerTokenAndPlaqueClicksKeepOwnCardsPlayable()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var seat = Root().Runtime.Host.Seats.Seats[1];
            MatchSceneCompositionRoot.NextComposedSeat = seat.Id;
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = Root();
            var runtime = root.Runtime;
            Assert.That(runtime.Seat.Id, Is.EqualTo(seat.Id));
            var pointer = UnityEngine.Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var version = runtime.ReadModel.Version;
            foreach (var other in runtime.Setup.Heroes.Where(hero => !hero.HeroId.Equals(seat.HeroId)))
            {
                UiPlayer.ClickBoard(pointer, root.Hud, HeroView(other.HeroId).transform.position);
                yield return null;
                UiPlayer.Click(Rail(root, other.HeroId).Q("gd-hero-identity-plate"));
                yield return null;
                Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));
                Assert.That(runtime.HandsView.Model.ViewedHand.HeroId, Is.EqualTo(seat.HeroId));
                Assert.That(runtime.HandsView.Model.Hands.Select(hand => hand.HeroId),
                    Is.EqualTo(new[] { seat.HeroId }));
            }
            UiPlayer.Click(Rail(root, seat.HeroId).Q("gd-hero-identity-plate"));
            yield return null;
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            var ownCard = UiPlayer.Descendants(runtime.HandsView.Root).OfType<Button>()
                .Last(button => button.name == "gd-card-choice" && button.enabledInHierarchy && UiPlayer.IsShown(button));
            UiPlayer.Click(ownCard);
            yield return null;
            Assert.That(runtime.HandsView.Model.ViewedHand.SelectedCardInstanceId, Is.Not.Null);
            UiPlayer.Click(UiPlayer.Button(root.Hud, "CONFIRM FACEDOWN"));
            yield return null;
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version + 1));
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(seat.HeroId));
        }

        [UnityTest]
        public IEnumerator DirectorPlaqueClickSelectsHeroWithoutChangingTheMatch()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            yield return Stage(GarrusCardContent.AngryStrike);
            var root = Root();
            var runtime = root.Runtime;
            var other = runtime.Setup.Heroes.First(hero => !hero.HeroId.Equals(runtime.ViewedHeroId));
            var version = runtime.ReadModel.Version;
            UiPlayer.PointAt(Rail(root, other.HeroId).Q("gd-hero-medallion"));
            yield return null;
            Assert.That(UiPlayer.IsShown(runtime.HeroOverviewsView.HoverPreviewRoot), Is.True);
            UiPlayer.Click(Rail(root, other.HeroId).Q("gd-hero-identity-plate"));
            yield return null;
            Assert.That(runtime.ViewedHeroId, Is.EqualTo(other.HeroId));
            Assert.That(runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(other.HeroId)).IsViewed,
                Is.True);
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            Assert.That(UiPlayer.IsShown(runtime.HeroOverviewsView.HoverPreviewRoot), Is.False,
                "Rebuilding the viewed-hero model drops its old preview.");
        }

        [UnityTest]
        public IEnumerator PortraitHoverShowsMatchingPickCardInsideBothViewportsAndDismisses()
        {
            MatchSceneCompositionRoot.NextComposedSeat = null;
            // A normal match, not staging: the sandbox-only banner covers Red's portrait at 540px.
            var roster = RegisteredHeroRoster.ForThisBuild;
            MatchSceneCompositionRoot.NextComposedMatchFormat = MatchFormat.ThreeVersusThreeQuick;
            MatchSceneCompositionRoot.NextComposedLineup = new HeroLineup(
                new[] { "wasp", "xargatha", "arien" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)),
                new[] { "garrus", "sabina", "tigerclaw" }.Select(id => roster.Single(hero => hero.ContentId.Value == id)));
            yield return LoadStagedScene(SceneCardContent.ConfiguredMatch);
            var root = Root();
            var runtime = root.Runtime;
            var version = runtime.ReadModel.Version;
            foreach (var height in new uint[] { 1080, 540 })
            {
                UiPlayer.SetPlayerViewport(height == 540 ? 2560u : 1920u, height, "hero pick-card hover");
                yield return null; yield return null;
                root.Hud.Layout();
                yield return null;
                foreach (var hero in runtime.HeroOverviewsView.Model.Heroes)
                {
                    var portrait = Rail(root, hero.HeroId).Q("gd-hero-medallion");
                    UiPlayer.PointAt(portrait);
                    yield return null;
                    var preview = runtime.HeroOverviewsView.HoverPreviewRoot;
                    var face = preview.Q<Image>("gd-hero-profile-preview");
                    Assert.That(face, Is.Not.Null);
                    var registered = RegisteredHeroRoster.ForThisBuild.Single(value => value.ContentId.Equals(hero.ContentId));
                    Assert.That(face.image, Is.SameAs(Resources.Load<Texture2D>("Private/HeroSelection/" + registered.HeroIdValue)));
                    Assert.That(face.image, Is.Not.Null);
                    Assert.That(UiPlayer.IsShown(face), Is.True);
                    Assert.That(preview.childCount, Is.EqualTo(1));
                    Assert.That(face.pickingMode, Is.EqualTo(PickingMode.Ignore));
                    var bounds = root.Hud.RootElement.worldBound;
                    Assert.That(face.worldBound.xMin, Is.GreaterThanOrEqualTo(bounds.xMin - 1));
                    Assert.That(face.worldBound.xMax, Is.LessThanOrEqualTo(bounds.xMax + 1));
                    Assert.That(face.worldBound.yMin, Is.GreaterThanOrEqualTo(bounds.yMin - 1));
                    Assert.That(face.worldBound.yMax, Is.LessThanOrEqualTo(bounds.yMax + 1));
                    if (hero.ContentId.Value == "garrus" || hero.ContentId.Value == "wasp")
                        yield return UiPlayer.CaptureGameView("8-42b", "profile-" + registered.HeroIdValue + "-" + height);
                    UiPlayer.PointAway(portrait);
                    yield return null;
                    Assert.That(UiPlayer.IsShown(preview), Is.False);
                    Assert.That(preview.childCount, Is.Zero);
                }
            }
            Assert.That(runtime.ReadModel.Version, Is.EqualTo(version));
            UiPlayer.SetPlayerViewport(1920, 1080, "restore profile viewport");
        }

        private static VisualElement Rail(MatchSceneCompositionRoot root, HeroId hero) =>
            UiPlayer.Descendants(root.Hud.RootElement).Single(element => element.name == "gd-hero-rail" &&
                element.userData is HeroId id && id.Equals(hero));

        /// <summary>
        /// This hero's current-card marker, looked up afresh every time: a rail that rebuilds between
        /// frames leaves an earlier reference detached, and a detached element's rectangle is a
        /// measurement of nothing. Milestone 8.55.
        /// </summary>
        private static VisualElement Badge(MatchSceneCompositionRoot root, HeroId hero) =>
            Rail(root, hero).Q<VisualElement>("gd-current-card");

        [UnityTest]
        public IEnumerator InitiativeBadgeShowsEarnedBonusAndKeepsItsFootprint()
        {
            yield return Stage(GarrusCardContent.AngryStrike, coins: 0);
            var root = Root();
            var runtime = root.Runtime;
            var owner = Garrus(runtime);
            for (var step = 0; step < 180 &&
                !(runtime.SystemTransitionPrompt?.Kind == SystemTransitionPromptKind.LevelUpUpgrade &&
                  runtime.Projection.PendingSystemTransitionDescription.HeroId.Equals(owner)); step++)
                yield return NextRoundDecision(root);
            Assert.That(runtime.SystemTransitionPrompt?.Kind, Is.EqualTo(SystemTransitionPromptKind.LevelUpUpgrade));

            // -----------------------------------------------------------------------------------
            // Milestone 8.58, the user on the two states of p.15's window, in two notes.
            //
            // DOCKED, on their own capture of the slim bar ("UPGRADE A CARD - PAID 2 COINS - 5
            // REMAINING - EXPAND"): "hand should not disappear on these choices, it makes this bar
            // float strangely in the air." The tray stays, blocked from taking a command exactly as
            // 8.35z58's optional step already blocks it, but never taken away.
            //
            // EXPANDED, on the live capture of the offer: "hand can be removed from bottom left and
            // shown below the offer window, like it was before." The panel draws the hand itself,
            // under the six candidates, so the tray underneath would be the same five cards twice.
            // -----------------------------------------------------------------------------------
            var tray = runtime.HandsView.Root;
            Assert.That(runtime.DecisionWindowView.Model, Is.Not.Null,
                "The window owns p.15's upgrade.");
            Assert.That(runtime.DecisionWindowView.Model.HandPreview, Is.Not.Empty,
                "and it carries the LEVELLING hero's hand, whoever is looking at it.");
            var preview = UiPlayer.Descendants(root.Hud.RootElement)
                .Where(element => element.name == "gd-levelup-hand-preview-card" &&
                    UiPlayer.IsShown(element))
                .ToArray();
            Assert.That(preview, Is.Not.Empty,
                "The expanded panel draws that hand inside itself.");
            var windowRoot = UiPlayer.Descendants(root.Hud.RootElement)
                .Single(element => element.name == "gd-decision-window");
            var band = UiPlayer.Descendants(windowRoot)
                .Single(element => element.name == "gd-upgrade-comparison-band");
            Assert.That(preview.Min(card => card.worldBound.yMin),
                Is.GreaterThanOrEqualTo(band.worldBound.yMax - UiPlayer.PixelSnapping(2)),
                "BELOW the offer, which is where the user asked for it: \"hand can be removed from " +
                "bottom left and shown below the offer window, like it was before.\"");
            Assert.That(UiPlayer.IsShown(tray), Is.False,
                "and the tray at the bottom left is gone while that row is on screen, so the hand " +
                "is drawn once.");

            runtime.DecisionWindowView.ForwardStateToggle();
            yield return null;
            yield return null;
            Assert.That(runtime.DecisionWindowView.Model.State,
                Is.EqualTo(DecisionWindowState.Docked));
            Assert.That(UiPlayer.IsShown(tray), Is.True,
                "It comes back the moment the window minimises, which is the capture the user sent.");
            Assert.That(runtime.HandsView.IsContextOnly, Is.True,
                "and is blocked from answering, which is what makes keeping it safe.");
            Assert.That(tray.worldBound.height, Is.GreaterThan(0f),
                "drawn rather than merely displayed: " + tray.worldBound);
            Assert.That(tray.worldBound.Overlaps(
                    UiPlayer.Descendants(root.Hud.RootElement)
                        .Single(element => element.name == "gd-decision-window").worldBound),
                Is.False,
                "and the docked bar sits above it rather than over it.");

            runtime.DecisionWindowView.ForwardStateToggle();
            yield return null;
            yield return null;
            Assert.That(runtime.DecisionWindowView.Model.State,
                Is.EqualTo(DecisionWindowState.Expanded),
                "The toggle comes back so the rest of this journey presses the expanded offer.");

            // -----------------------------------------------------------------------------------
            // Milestone 8.58. The level-up window photographed on a REAL pause, whose candidates are
            // the Tier II and Tier III faces p.15 actually offers.
            //
            // The user withdrew a complaint about p.16's `GAINS:` box covering printed text once they
            // saw which faces the first capture held: "Might be problem cuz you shown me tier one
            // cards. I think it might be fine on T2 and higher and that is ok then." A staged frame
            // built from a mirrored Tier I hand cannot answer that; this can, because these six faces
            // are the authority's own offer.
            // -----------------------------------------------------------------------------------
            var captureWidth = (uint)Screen.width;
            var captureHeight = (uint)Screen.height;
            foreach (var viewport in new[] { (1280u, 720u), (1920u, 1080u), (2560u, 1440u) })
            {
                UiPlayer.SetPlayerViewport(viewport.Item1, viewport.Item2, "8.58 level-up captures");
                yield return null;
                yield return null;
                root.Hud.Layout();
                yield return null;
                Assert.That(
                    UiPlayer.Descendants(root.Hud.RootElement)
                        .Count(element => element.name == "gd-upgrade-option" &&
                            UiPlayer.IsShown(element)),
                    Is.EqualTo(6),
                    "p.15 offers six real candidates at " + viewport.Item1 + "x" + viewport.Item2 + ".");

                // Milestone 8.58, the user's standing rule for every family in this window, applied to
                // the one that used to be exempt: "No reason for this bigger box when we have smaller
                // one already." p.15 held the WHOLE rectangle and painted its lower half empty; it is
                // content-sized now - offer, hand, chrome - and centred in the bound like the rest.
                var bound = UiPlayer.Descendants(root.Hud.RootElement)
                    .Single(element => element.name == "gd-decision-window").worldBound;
                var panel = UiPlayer.Descendants(root.Hud.RootElement)
                    .Single(element => element.name == "gd-decision-window-expanded");
                var drawn = UiPlayer.Descendants(panel)
                    .Where(element => element.name == "gd-levelup-hand-preview-card" &&
                        UiPlayer.IsShown(element))
                    .Max(element => element.worldBound.yMax);
                Assert.That(panel.worldBound.yMax - drawn,
                    // Padding, border and at most one row gap: what this measures is that there is no
                    // BAND of empty panel under the offer.
                    Is.LessThanOrEqualTo(
                        DecisionWindowUnityView.WindowPadding +
                            DecisionWindowUnityView.PanelBorderWidth +
                            DecisionWindowUnityView.LevelUpRowGap +
                            UiPlayer.PixelSnapping(4)),
                    "The panel ends where its content ends at " + viewport.Item1 + "x" +
                    viewport.Item2 + " - no empty half under the offer: " + panel.worldBound);
                Assert.That(
                    Mathf.Abs(panel.worldBound.center.x - bound.center.x),
                    Is.LessThanOrEqualTo(UiPlayer.PixelSnapping(2)),
                    "and it is centred across the bound at " + viewport.Item1 + "x" + viewport.Item2 +
                    ": " + panel.worldBound + " in " + bound);
                Assert.That(
                    Mathf.Abs(panel.worldBound.center.y - bound.center.y),
                    Is.LessThanOrEqualTo(UiPlayer.PixelSnapping(2)),
                    "and down it.");
                yield return UiPlayer.CaptureScreen(
                    "tmp/858-captures/level-up-expanded-live-" +
                    viewport.Item1 + "x" + viewport.Item2 + ".png");
            }

            UiPlayer.SetPlayerViewport(captureWidth, captureHeight, "8.58 level-up restore");
            yield return null;
            yield return null;
            root.Hud.Layout();
            yield return null;

            var option = runtime.RoundView.Model.SystemTransitionPrompt.Options.First(value =>
                value.Card != null && value.ItemIcon == PrintedValueChannel.Initiative);
            var button = UiPlayer.Descendants(root.Hud.RootElement).OfType<Button>().Single(element =>
                element.name == "gd-upgrade-option" && UiPlayer.IsShown(element) &&
                element.tooltip == option.Label + " - " + option.Detail);
            UiPlayer.EnsureVisible(button);
            yield return null;
            UiPlayer.Click(button);
            yield return null;
            for (var step = 0; step < 50 && runtime.Phase == RoundResolutionPhase.EndOfRound; step++)
                yield return NextRoundDecision(root);
            var bonus = runtime.Projection.HeroProgression.Single(hero => hero.HeroId.Equals(owner))
                .ItemBonuses.Single(value => value.Key == PrintedValueChannel.Initiative).Value;
            Assert.That(bonus, Is.GreaterThan(0));
            var held = runtime.Cards.First(card => card.OwnerId.Equals(owner) && card.State.Location == CardLocation.Hand);
            yield return CommitEverySeat(root, owner, NameOf(runtime, held.DefinitionReference.DefinitionId));
            yield return new UnityEngine.WaitForSeconds(0.5f);

            // -----------------------------------------------------------------------------------
            // Milestone 8.55, the user mid-playtest on their own capture of Swift's plaque while
            // Reload! resolved with one Initiative item, the badge reading `7`: "this should be
            // noted as 6+1." The live rail is where that is settled - the split is composed in
            // presentation, but whether a wider reading still FITS the marker is a question only a
            // laid-out panel answers, and 8.42a's acceptance keeps the badge's geometry.
            //
            // The first three viewports are the ones this build is measured at; the squat fourth is
            // 8.42a's own, kept. Text lays out in POINTS at a stated size, so a scaled panel should
            // not change the answer - and the squat one does change it, snapping the room inside the
            // marker from 42 units to 40, which is exactly why all four are measured rather than one.
            // -----------------------------------------------------------------------------------
            foreach (var viewport in new[] { (2560u, 1440u), (1920u, 1080u), (1280u, 720u), (2560u, 540u) })
            {
                UiPlayer.SetPlayerViewport(viewport.Item1, viewport.Item2, "Initiative badge");
                yield return null; yield return null;
                root.Hud.Layout();
                yield return null;
                var at = viewport.Item1 + "x" + viewport.Item2;

                // A resized panel re-enters the rail, and 8.35z11's motion eases what re-enters, so
                // three frames after the resize the marker is still on its way to the footprint it
                // reserves - measured at 31 and at 6 units wide before this settle existed, and a
                // capture taken there photographs a squeezed badge the build never draws. Wait for the
                // marker to BE its reserved footprint, then measure what is inside it.
                for (var frame = 0; frame < 600 &&
                    !AtItsFootprint(Badge(root, owner).worldBound); frame++)
                {
                    yield return null;
                }

                var model = runtime.HeroOverviewsView.Model.Heroes.Single(hero => hero.HeroId.Equals(owner));
                Assert.That(model.CurrentCard, Is.Not.Null);
                var printed = model.CurrentCard.Card.Face.Initiative.Amount;
                var expected = printed + "+" + bonus;
                Assert.That(model.CurrentCard.CardInitiative, Is.EqualTo(printed));
                Assert.That(model.CurrentCard.ItemInitiativeBonus, Is.EqualTo(bonus));
                var marker = Badge(root, owner);
                Assert.That(marker.worldBound.width,
                    Is.EqualTo(HeroOverviewsUnityView.CurrentCardWidth)
                        .Within(UiPlayer.PixelSnapping(2)),
                    "The marker is drawn at the footprint it reserves at " + at +
                    " - 8.55 changes the reading inside the badge and nothing about the badge: " +
                    marker.worldBound);
                Assert.That(marker.worldBound.height,
                    Is.EqualTo(HeroOverviewsUnityView.CurrentCardHeight)
                        .Within(UiPlayer.PixelSnapping(2)),
                    "and down it: " + marker.worldBound);
                var label = UiPlayer.Descendants(marker).OfType<Label>().First();
                Assert.That(label.text, Is.EqualTo(expected),
                    "The live badge reads the printed number and the item bonus apart at " + at);
                Assert.That(UiPlayer.IsShown(label), Is.True);
                Assert.That(label.worldBound.xMin, Is.GreaterThanOrEqualTo(marker.worldBound.xMin - 1));
                Assert.That(label.worldBound.xMax, Is.LessThanOrEqualTo(marker.worldBound.xMax + 1));

                // The label's own box is the marker's, absolutely, so its rectangle can never report
                // an overflow: what has to be measured is the TEXT, against the room inside the
                // marker's border. This is the check 8.55 exists to pass.
                var inner = marker.contentRect.width;
                var drawn = label.MeasureTextSize(label.text, 0f, VisualElement.MeasureMode.Undefined,
                    0f, VisualElement.MeasureMode.Undefined);
                TestContext.WriteLine($"8.55 badge at {at}: \"{label.text}\" at " +
                    $"{label.resolvedStyle.fontSize} draws {drawn.x:0.##}x{drawn.y:0.##} in " +
                    $"{inner:0.##}x{marker.contentRect.height:0.##} (marker {marker.worldBound}).");
                Assert.That(drawn.x, Is.LessThanOrEqualTo(inner),
                    $"`{label.text}` must fit the marker's {inner} units of room at {at} without the " +
                    "marker moving or growing: it draws " + drawn.x);
                Assert.That(drawn.y, Is.LessThanOrEqualTo(marker.contentRect.height),
                    "and stay on one line inside it at " + at);
                Assert.That(label.resolvedStyle.fontSize,
                    Is.EqualTo(HeroOverviewsUnityView.InitiativeBadgeFontSizeFor(label.text)),
                    "at the one type size 8.55's rule gives that reading.");

                // Every other reading the badge can take, measured in the font the rail is actually
                // drawing in - including the widest one, `13+2`, a two-digit printed Initiative under
                // two items, which no short fixture reaches but p.7's Initiatives make real. Each is
                // measured at the label's own size and carried to the size 8.55's rule gives it: a
                // face's advances are proportional to its point size, so this is the one arithmetic
                // step a live measurement may take, and it takes it rather than disturb the rail the
                // captures below are photographed from.
                foreach (var reading in new[] { "13+2", "13", "6+1", "6" })
                {
                    var ruleSize = HeroOverviewsUnityView.InitiativeBadgeFontSizeFor(reading);
                    var wide = label.MeasureTextSize(reading, 0f, VisualElement.MeasureMode.Undefined,
                        0f, VisualElement.MeasureMode.Undefined).x *
                        ruleSize / label.resolvedStyle.fontSize;
                    TestContext.WriteLine($"8.55 badge at {at}: \"{reading}\" at {ruleSize} would " +
                        $"draw {wide:0.##} in {inner:0.##}.");
                    Assert.That(wide, Is.LessThanOrEqualTo(inner),
                        $"`{reading}` must fit the marker's {inner} units at {at}: it draws " + wide);
                }

                // Every other hero committed too, and none of them earned an Initiative item: their
                // badges read exactly the printed number they always did, so the split form is the
                // item's doing rather than a new format for every plaque.
                foreach (var other in runtime.HeroOverviewsView.Model.Heroes
                    .Where(hero => !hero.HeroId.Equals(owner) && hero.CurrentCard?.Card != null))
                {
                    var plain = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                            element.name == "gd-hero-rail" && element.userData is HeroId id &&
                            id.Equals(other.HeroId))
                        .Q<VisualElement>("gd-current-card");
                    var plainLabel = UiPlayer.Descendants(plain).OfType<Label>().First();
                    Assert.That(other.CurrentCard.ItemInitiativeBonus, Is.Zero);
                    Assert.That(plainLabel.text,
                        Is.EqualTo(other.CurrentCard.Card.Face.Initiative.Amount.ToString()),
                        "A card with no item reads `6`, not `6+0`, at " + at);
                    Assert.That(plainLabel.resolvedStyle.fontSize,
                        Is.EqualTo(HeroOverviewsUnityView.InitiativeBadgeFontSize),
                        "at the size it drew at before 8.55, which is the geometry 8.42a keeps.");
                    Assert.That(
                        plainLabel.MeasureTextSize(plainLabel.text, 0f,
                            VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined).x,
                        Is.LessThanOrEqualTo(plain.contentRect.width));
                }

                if (viewport.Item1 == 1920u && viewport.Item2 == 1080u)
                {
                    yield return CaptureBadge(
                        root, owner,
                        "tmp/855-captures/rail-initiative-badge.png",
                        "tmp/855-captures/rail-initiative-badge-zoom.png");
                }

                yield return UiPlayer.CaptureGameView("8-55", "initiative-" + at);
            }

            // The reading survives a presentation rebuilt from the same projection - the refresh a
            // round boundary, a resync and every panel-level repaint all go through.
            runtime.RefreshAll();
            yield return null;
            root.Hud.Layout();
            yield return null;
            var rebuilt = UiPlayer.Descendants(root.Hud.RootElement).Single(element =>
                    element.name == "gd-hero-rail" && element.userData is HeroId id && id.Equals(owner))
                .Q<VisualElement>("gd-current-card");
            Assert.That(UiPlayer.Descendants(rebuilt).OfType<Label>().First().text,
                Is.EqualTo(runtime.HeroOverviewsView.Model.Heroes
                    .Single(hero => hero.HeroId.Equals(owner)).CurrentCard.Card.Face.Initiative.Amount +
                    "+" + bonus),
                "A rebuilt rail draws the same split rather than falling back to the printed number " +
                "or to 8.42a's sum.");

            UiPlayer.SetPlayerViewport(1920, 1080, "restore Initiative viewport");
            Healthy(runtime);
        }

        /// <summary>Whether the marker is drawn at the footprint it reserves, rather than easing in.</summary>
        private static bool AtItsFootprint(Rect box) =>
            Mathf.Abs(box.width - HeroOverviewsUnityView.CurrentCardWidth) <= UiPlayer.PixelSnapping(2);

        /// <summary>
        /// Milestone 8.55. The rail as the user will look at it, and a magnified crop of the badge
        /// alone beside it - what they are judging is four characters about twenty units tall in a
        /// 1920-wide photograph. Both files are written from ONE photograph, so the crop cannot show a
        /// different moment than the picture it came from, and it is magnified by whole pixels so
        /// nothing is resampled into legibility it does not have.
        ///
        /// <para>The retry is the whole point of the method. 8.35z11's motion eases the rail in
        /// whenever it re-enters the panel, and it re-enters oftener than a test can predict: the
        /// complete suite photographed the marker at 13 units of its 46 on a run where the focused one
        /// had caught it settled. So this waits for the marker to BE its footprint, reads the box in
        /// the same frame it photographs, and starts over if it was mid-flight after all.</para>
        /// </summary>
        private static IEnumerator CaptureBadge(
            MatchSceneCompositionRoot root, HeroId hero, string railPath, string zoomPath)
        {
            var railFull = System.IO.Path.GetFullPath(railPath);
            var zoomFull = System.IO.Path.GetFullPath(zoomPath);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(railFull));
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(zoomFull));
            var photographed = false;
            var lastSeen = new Rect();

            for (var attempt = 0; attempt < 20 && !photographed; attempt++)
            {
                for (var frame = 0; frame < 600 && !AtItsFootprint(Badge(root, hero).worldBound); frame++)
                {
                    yield return null;
                }

                yield return new UnityEngine.WaitForEndOfFrame();

                // Read in the photographed frame itself, with nothing yielded in between: this box is
                // what the back buffer below actually holds.
                var box = Badge(root, hero).worldBound;
                lastSeen = box;
                if (!AtItsFootprint(box))
                {
                    continue;
                }

                Texture2D frameTexture = null;
                Texture2D crop = null;
                try
                {
                    frameTexture = ScreenCapture.CaptureScreenshotAsTexture();
                    Assert.That(frameTexture, Is.Not.Null, "No frame could be photographed.");
                    System.IO.File.WriteAllBytes(railFull, frameTexture.EncodeToPNG());

                    // Panel units to device pixels, read off the panel itself rather than recomputed
                    // from the scaling formula: the root's laid-out box IS the whole screen.
                    var panel = root.Hud.RootElement.panel.visualTree.worldBound;
                    var scale = frameTexture.width / panel.width;
                    var pad = box.width * 1.6f;
                    var x = Mathf.RoundToInt((box.xMin - pad) * scale);
                    var width = Mathf.RoundToInt((box.width + pad + pad) * scale);
                    // The photograph's origin is bottom left and the panel's is top left.
                    var y = Mathf.RoundToInt(frameTexture.height - (box.yMax + pad) * scale);
                    var height = Mathf.RoundToInt((box.height + pad + pad) * scale);
                    x = Mathf.Clamp(x, 0, frameTexture.width - 1);
                    y = Mathf.Clamp(y, 0, frameTexture.height - 1);
                    width = Mathf.Clamp(width, 1, frameTexture.width - x);
                    height = Mathf.Clamp(height, 1, frameTexture.height - y);

                    const int magnification = 6;
                    var pixels = frameTexture.GetPixels(x, y, width, height);
                    crop = new Texture2D(
                        width * magnification, height * magnification, TextureFormat.RGBA32, false);
                    var magnified = new Color[width * magnification * height * magnification];
                    for (var row = 0; row < height * magnification; row++)
                    {
                        for (var column = 0; column < width * magnification; column++)
                        {
                            magnified[row * width * magnification + column] =
                                pixels[row / magnification * width + column / magnification];
                        }
                    }

                    crop.SetPixels(magnified);
                    crop.Apply();
                    System.IO.File.WriteAllBytes(zoomFull, crop.EncodeToPNG());
                    photographed = true;
                    TestContext.WriteLine($"8.55 badge capture (attempt {attempt + 1}): marker {box} " +
                        $"at scale {scale:0.###} -> crop ({x},{y},{width},{height}) magnified " +
                        $"{magnification}x. {railFull} and {zoomFull}");
                }
                finally
                {
                    if (frameTexture != null) UnityEngine.Object.DestroyImmediate(frameTexture);
                    if (crop != null) UnityEngine.Object.DestroyImmediate(crop);
                }
            }

            Assert.That(photographed, Is.True,
                "The evidence the user judges by eye must photograph the marker at its own footprint " +
                "rather than mid-transition; the last box seen was " + lastSeen);
            foreach (var written in new[] { railFull, zoomFull })
            {
                Assert.That(System.IO.File.Exists(written), Is.True, "The capture must land at " + written);
                Assert.That(new System.IO.FileInfo(written).Length, Is.GreaterThan(1024L),
                    "and hold a real image rather than an empty frame: " + written);
            }
        }
    }
}
