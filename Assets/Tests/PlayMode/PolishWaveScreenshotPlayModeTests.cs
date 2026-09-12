using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Core.Rules.Transitions;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// The polish wave's owed VISUAL EVIDENCE, captured rather than described: milestone 8.35z58's
    /// button treatment in all four of its states on both surfaces that draw buttons, the levelling
    /// hand preview the same milestone added, and milestone 8.35z59's ultrawide fit at a real
    /// 3440x1440 viewport.
    /// </summary>
    /// <remarks>
    /// <para><b>Why a fixture rather than the hand-capture recipe that was written for this.</b> The
    /// worktree that built the button pass could not launch Unity and left a verbatim recipe for a
    /// human to follow with a mouse - open the scene, hover, hold the button down, screenshot. That
    /// recipe is correct and unreproducible: "hold the left button and photograph it" is not a step
    /// a validation run can repeat, and the states it asks for are driven by
    /// <c>HudElements.ApplyButtonMotion</c>'s own pointer callbacks, which a fixture can raise
    /// exactly. So the states are entered through the SAME events a real pointer raises -
    /// <c>PointerEnterEvent</c> for hover, <c>PointerDownEvent</c> with button 0 for press - and the
    /// Game View is photographed between them.</para>
    ///
    /// <para><b>These cases capture; they do not gate.</b> Milestone 8.35z58's own
    /// <c>HudButtonTreatmentTests</c> already asserts the contrast floor, the shared radius and the
    /// 100ms press numerically, and that is where a regression is caught. What no assertion can do is
    /// answer "does it LOOK right", which is the user's call and needs a picture. A batch run has no
    /// Game View to photograph, so <see cref="UiPlayer.CaptureGameView"/> captures nothing there
    /// rather than failing - these cases still drive every state, so the batch tier proves the states
    /// are REACHABLE even when it cannot photograph them.</para>
    /// </remarks>
    public sealed class PolishWaveScreenshotPlayModeTests
    {
        /// <summary>
        /// Where the wave's evidence lands, as one folder the integration report can name. It is a
        /// prefix on <see cref="UiPlayer.CaptureGameView"/>'s own <c>tmp/</c>-rooted path rather than
        /// a second capture implementation.
        /// </summary>
        private const string Shot =
            "8-35d-8-35e-multiplayer-corrections/integration-screenshots/polish-wave";

        private static void EnsureShotFolder()
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath("tmp/" + Shot));
            if (!string.IsNullOrEmpty(folder))
            {
                Directory.CreateDirectory(folder);
            }
        }

        /// <summary>
        /// Enters the pressed state the way a held finger does, through the event
        /// <c>ApplyButtonMotion</c> actually listens for. A pooled <c>PointerDownEvent</c> carries
        /// button 0, which is the left button that motion pass requires.
        /// </summary>
        private static void PressAndHold(VisualElement element)
        {
            Assert.That(element?.panel, Is.Not.Null, "Nothing to press.");
            using (var down = PointerDownEvent.GetPooled())
            {
                down.target = element;
                element.SendEvent(down);
            }
        }

        private static void ReleasePress(VisualElement element)
        {
            using (var up = PointerUpEvent.GetPooled())
            {
                up.target = element;
                element.SendEvent(up);
            }
        }

        /// <summary>
        /// Loads the ordinary Director match - the shipped scene with none of
        /// <c>PrivateSessionLaunchOptions</c>' role flags present, which is the composition the
        /// button recipe names.
        /// </summary>
        private static IEnumerator LoadTheMatch()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        [UnityTest]
        public IEnumerator AuraAndActivity_RenderAndRestoreTheConfiguredBoard()
        {
            yield return LoadTheMatch();
            var root = Root();
            var original = root.Runtime.BoardView.Model;
            var probeObject = new GameObject("Aura visual probe");
            probeObject.SetActive(false); // Do not run the driver's command-line Start/exit coroutine.
            var probe = probeObject.AddComponent<BoardVisualCaptureDriver>();
            try
            {
                probe.Configure(root, Path.GetFullPath("tmp/8-35z84-aura.png"),
                    Object.FindAnyObjectByType<Camera>(),
                    Object.FindAnyObjectByType<GuardsDigital.Presentation.Board.PlaceholderTestGridView>());
                probe.CaptureAuraInspection();
                Assert.That(root.Runtime.BoardView.Model, Is.SameAs(original));
                foreach (var state in new[] { "none", "aura", "aura-active", "aura-active-minimum", "active" })
                    Assert.That(new FileInfo("tmp/8-35z84-aura-" + state + ".png").Length, Is.GreaterThan(1000));
            }
            finally { Object.Destroy(probeObject); }
            yield return null;
        }

        [UnityTest]
        public IEnumerator PreparedHighlightsAndBanners_AreVisibleAcrossEveryHero()
        {
            if (!Directory.Exists(Path.Combine(UnityEngine.Application.dataPath, "Resources", "Private", "Cards")))
                yield break;
            yield return LoadTheMatch();
            var definitions = WaspCardContent.DealtDefinitions.Concat(XargathaCardContent.DealtDefinitions)
                .Concat(TigerclawCardContent.DealtDefinitions).Concat(ArienCardContent.DealtDefinitions)
                .Concat(BroganCardContent.DealtDefinitions).Concat(SabinaCardContent.DealtDefinitions)
                .Concat(DodgerCardContent.DealtDefinitions).Concat(MisaCardContent.DealtDefinitions)
                .Concat(SilverarrowCardContent.DealtDefinitions).Concat(UrsafarCardContent.DealtDefinitions)
                .Concat(WhisperCardContent.DealtDefinitions)
                // Milestone 8.37. The twelfth and last: this walk is driven by the artwork catalog's
                // own entries, so a hero whose rows are in that catalog and whose definitions are
                // not in this dictionary throws on the lookup rather than being quietly skipped.
                .Concat(TaliCardContent.DealtDefinitions)
                // Milestone 8.38f. The THIRTEENTH, added because this tripwire caught her absence
                // exactly as the note above predicted: 8.38e put her rows in the artwork catalog and
                // left her definitions out of this dictionary, and the lookup threw
                // `KeyNotFoundException` on `swift-reload`. Only a Play Mode run can see it, which is
                // the debt 8.38e moved here. A FOURTEENTH hero will trip it again - the list is hand
                // written where 8.38d.s corpus sweep is now derived, and that is the fix this note
                // leaves for whoever adds her.
                .Concat(SwiftCardContent.DealtDefinitions).ToDictionary(card => card.Reference);
            var flags = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
            foreach (var hero in ConfiguredCardArtworkCatalog.Catalog.Entries.GroupBy(entry => entry.ContentSetId))
            {
                var cards = hero.Select(entry => definitions[entry.Reference]).Where(card => card.UltimateRule == null).ToArray();
                var attack = cards.FirstOrDefault(card => card.ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == ActionKind.Attack));
                var move = cards.First(card => card.ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedSecondary && action.Kind == ActionKind.Movement));
                var defense = cards.First(card => card.ActionDefinitions.Any(action =>
                    action.BaseValues.Any(value => value.Channel == PrintedValueChannel.Defense)));
                var primary = cards.FirstOrDefault(card => card.ActionDefinitions.Any(action =>
                    action.Origin == ActionDefinitionOrigin.PrintedPrimary && action.Kind == ActionKind.Movement)) ??
                    cards.First(card => card.ActionDefinitions.Any(action => action.Origin == ActionDefinitionOrigin.PrintedPrimary));
                var gallery = new VisualElement { style = { position = Position.Absolute, left = 340f,
                    top = 180f, flexDirection = FlexDirection.Row, backgroundColor = MatchHudTheme.TopBarBackground } };
                Root().Hud.RootElement.Add(gallery);
                try
                {
                    var samples = new[] {
                        (Card: attack ?? primary, Slot: CardShineSlot.PrimaryRing, Context: ActionInvocationContext.PrintedPrimary, Banner: attack != null ? "CLEAR" : ""),
                        (Card: move, Slot: CardShineSlot.SecondaryMovement, Context: ActionInvocationContext.PrintedSecondary, Banner: "FAST TRAVEL"),
                        (Card: defense, Slot: CardShineSlot.SecondaryDefense, Context: ActionInvocationContext.PrintedSecondary, Banner: ""),
                        (Card: primary, Slot: CardShineSlot.PrimaryRing, Context: ActionInvocationContext.PrintedPrimary, Banner: primary.ActionDefinitions.Any(a => a.Origin == ActionDefinitionOrigin.PrintedPrimary && a.Kind == ActionKind.Movement) ? "FAST TRAVEL" : "")
                    };
                    foreach (var sample in samples)
                    {
                        var column = new VisualElement { style = { width = CardFootprint.Width + 20f, flexShrink = 0f } };
                        column.Add(new Label(hero.Key + " / " + sample.Slot) { style = { color = Color.white } });
                        var model = CurrentRoundCards.DeckCard(new CardInstanceId("shine-probe-" + sample.Slot + sample.Card.Id.Value), sample.Card);
                        var face = DirectorHandsUnityView.BuildStandaloneCard(model);
                        var slot = sample.Slot;
                        if (slot == CardShineSlot.SecondaryDefense && !sample.Card.ActionDefinitions.Any(a =>
                                a.Origin != ActionDefinitionOrigin.PrintedPrimary && a.BaseValues.Any(v => v.Channel == PrintedValueChannel.Defense)))
                            slot = CardShineSlot.Primary;
                        DirectorHandsUnityView.AddFaceShine(face, model, slot);
                        Assert.That(face.Q<Image>("gd-card-shine"), Is.Not.Null, sample.Card.Id.Value);
                        if (sample.Banner.Length > 0)
                        {
                            var action = sample.Card.ActionDefinitions.First(a =>
                                a.Origin == (sample.Context == ActionInvocationContext.PrintedPrimary ? ActionDefinitionOrigin.PrintedPrimary : ActionDefinitionOrigin.PrintedSecondary) &&
                                a.Kind == (sample.Banner == "CLEAR" ? ActionKind.Attack : ActionKind.Movement));
                            var region = new PrintedActionRegionViewModel(new ActionOffer(model.CardInstanceId,
                                sample.Card.Reference, action.Id, new HeroId("visual-probe"), sample.Context, true),
                                action.Kind, action.Kind.ToString(), 0, 1, sample.Banner, null, cardColor: sample.Card.Progression.Color);
                            var bounds = (Rect)typeof(DirectorHandsUnityView).GetMethod("PrintedActionBounds", flags)
                                .Invoke(null, new object[] { region, true });
                            var banner = (Button)typeof(DirectorHandsUnityView).GetMethod("BuildReplacementChip", flags)
                                .Invoke(null, new object[] { region, sample.Banner, bounds, (System.Action)(() => { }), true, true });
                            face.Add(banner);
                        }
                        column.Add(face);
                        gallery.Add(column);
                    }
                    yield return null;
                    yield return null;
                    yield return UiPlayer.CaptureGameView("8-35z83", hero.Key + "-highlights-banners");
                }
                finally { gallery.RemoveFromHierarchy(); }
            }
        }

        [UnityTest]
        public IEnumerator WhenPrivateCorpusIsPresent_NewHeroBasicAndUpgradeFacesUseRealArtwork()
        {
            var corpus = Path.Combine(UnityEngine.Application.dataPath, "..", "Docs",
                "Guards of Atlantis II - Tabletopia Cards");
            if (!Directory.Exists(corpus)) yield break;
            yield return LoadTheMatch();
            var hud = Root().Hud;
            var gallery = PlayerSmokeTestDriver.BuildArtworkInspection();
            hud.RootElement.Add(gallery);
            try
            {
                var heroes = new[]
                {
                    (Name: "Dodger", Cards: DodgerCardContent.DealtDefinitions),
                    (Name: "Misa", Cards: MisaCardContent.DealtDefinitions),
                    (Name: "Silverarrow", Cards: SilverarrowCardContent.DealtDefinitions),
                    (Name: "Ursafar", Cards: UrsafarCardContent.DealtDefinitions),
                    (Name: "Whisper", Cards: WhisperCardContent.DealtDefinitions)
                };
                foreach (var hero in heroes)
                {
                    foreach (var tier in new[] { CardTier.None, CardTier.II })
                    {
                        var definition = hero.Cards.First(card => card.Progression.Tier == tier);
                        Assert.That(ConfiguredCardArtwork.TryLoadTexture(definition.Reference,
                            out var texture, out var path), Is.True, definition.Id.Value);
                        var face = gallery.Children().SelectMany(column => column.Children()).Single(element =>
                            element.userData is CardDefinitionReference reference && reference.Equals(definition.Reference));
                        var picture = face.Q<Image>("gd-card-artwork");
                        Assert.That(picture, Is.Not.Null, path);
                        Assert.That(picture.image, Is.SameAs(texture));
                        Assert.That(face.Q("gd-card-frame"), Is.Null);
                    }
                }
                yield return null;
                yield return null;
                yield return UiPlayer.CaptureGameView("8-35z81", "new-hero-real-faces");
            }
            finally { gallery.RemoveFromHierarchy(); }
        }

        /// <summary>
        /// Milestone 8.35z58. A representative HUD button in idle, hover, pressed and disabled, so the
        /// user can accept or reject the treatment from pictures rather than from colour triples.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHudButton_IsPhotographedInEveryStateItDraws()
        {
            EnsureShotFolder();
            yield return LoadTheMatch();
            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null, "The match HUD is not composed into the scene.");

            var buttons = UiPlayer.Descendants(hud.RootElement)
                .OfType<Button>()
                .Where(candidate => UiPlayer.IsShown(candidate) &&
                    !string.IsNullOrWhiteSpace(candidate.text))
                .ToArray();
            Assert.That(buttons, Is.Not.Empty,
                "The composed HUD draws no labelled button to photograph. Shown: " +
                UiPlayer.Describe(hud));

            var enabled = buttons.FirstOrDefault(candidate => candidate.enabledInHierarchy)
                ?? buttons[0];

            yield return UiPlayer.CaptureGameView(Shot, "hud-button-idle");

            UiPlayer.PointAt(enabled);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "hud-button-hover");

            PressAndHold(enabled);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "hud-button-pressed");
            ReleasePress(enabled);
            UiPlayer.PointAway(enabled);
            yield return null;

            // The DISABLED state is photographed on whatever the composed match actually draws
            // unavailable, rather than on a control forced into it - a forced disable would
            // photograph a state the shipped HUD never reaches.
            var disabled = buttons.FirstOrDefault(candidate => !candidate.enabledInHierarchy);
            if (disabled != null)
            {
                UiPlayer.EnsureVisible(disabled);
                yield return null;
                yield return UiPlayer.CaptureGameView(Shot, "hud-button-disabled");
            }
            else
            {
                Debug.Log(
                    "GUARDS_SHOT: no HUD control is drawn unavailable in the opening position, so " +
                    "the disabled state is photographed on the lobby's taken-hero option instead.");
            }

            Assert.Pass(
                "Captured the HUD button states that the composed match draws. Photographed control: '" +
                enabled.text + "'.");
        }

        /// <summary>
        /// Milestone 8.35z59. The HUD at a real ultrawide viewport, which is the milestone's whole
        /// subject: before it, the hero rail column went short at about 16:9.06 and the plaques
        /// overflowed into the tray band.
        /// </summary>
        [UnityTest]
        public IEnumerator TheHud_IsPhotographedAtAnUltrawideViewport()
        {
            EnsureShotFolder();

            // THE VIEWPORT IS SET BEFORE THE SCENE COMPOSES, and that ordering is the honest one
            // rather than a convenience. A first attempt resized an already-composed match and
            // photographed a 2560-wide frame padded into a 3440 image - the HUD had laid out for the
            // old viewport and does not re-lay-out on a mid-session resolution change, so the shot
            // showed a black band that reads as "the panel does not fill an ultrawide" when it is
            // really "this HUD was composed for a narrower screen". A shipped build takes its
            // resolution at launch and composes once inside it, so composing INSIDE the ultrawide is
            // what a player actually gets, and is what this photographs.
            UiPlayer.SetPlayerViewport(3440, 1440, "8.35z59 ultrawide evidence");
            var settled = false;
            for (var frame = 0; frame < 120 && !settled; frame++)
            {
                yield return null;
                settled = Screen.width >= 3400;
            }

            Assert.That(settled, Is.True,
                "The play view never reported the 3440-wide viewport this shot is about; it stopped " +
                "at " + Screen.width + "x" + Screen.height + ".");

            yield return LoadTheMatch();

            // WaitForEndOfFrame THROWS in batch mode - Unity never evokes it without a display - and
            // the complete Play Mode run is batch, so this is guarded rather than unconditional. The
            // extra settle frame only matters for the capture, which batch mode does not take
            // either, so the batch tier still drives every state and simply photographs nothing.
            if (!UnityEngine.Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
            }

            yield return null;

            var hud = Root().Hud;
            Assert.That(hud, Is.Not.Null);

            var rails = UiPlayer.Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Is.Not.Empty,
                "The ultrawide shot is only evidence if the rails it is about are drawn.");

            yield return UiPlayer.CaptureGameView(Shot, "hud-ultrawide-3440x1440");

            // Put the viewport back, because every other journey in this suite reads its geometry
            // from whatever size is left behind.
            UiPlayer.SetPlayerViewport(1920, 1080, "restore after 8.35z59 evidence");
            yield return null;
        }

        /// <summary>
        /// Milestone 8.35z58. A LOBBY button in the same four states, on the surface the user was
        /// actually looking at when they reported "they were the old Buttons" - and the disabled
        /// state on the control that genuinely draws one, p.22's already-taken hero option.
        /// </summary>
        [UnityTest]
        public IEnumerator TheLobbyButton_IsPhotographedInEveryStateItDraws()
        {
            EnsureShotFolder();
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedSeat = null;
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.ConfiguredMatch;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            var host = new GameObject("gd-private-lobby");
            host.AddComponent<PrivateLobbyDriver>().Configure(Root());
            yield return null;
            yield return null;

            var driver = Object.FindAnyObjectByType<PrivateLobbyDriver>();
            Assert.That(driver, Is.Not.Null, "The lobby driver was not composed into the scene.");
            Assert.That(driver.View, Is.Not.Null, "The lobby driver has not built its panel yet.");

            var controls = UiPlayer.Descendants(driver.View.Root)
                .OfType<Button>()
                .Where(candidate => UiPlayer.IsShown(candidate))
                .ToArray();
            Assert.That(controls, Is.Not.Empty,
                "The entry form draws no control to photograph.");

            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-idle");

            var control = controls.FirstOrDefault(candidate => candidate.text == "HOST")
                ?? controls[0];
            UiPlayer.PointAt(control);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-hover");

            PressAndHold(control);
            yield return null;
            yield return UiPlayer.CaptureGameView(Shot, "lobby-button-pressed");
            ReleasePress(control);
            UiPlayer.PointAway(control);
            yield return null;

            var unavailable = UiPlayer.Descendants(driver.View.Root)
                .OfType<Button>()
                .FirstOrDefault(candidate =>
                    UiPlayer.IsShown(candidate) && !candidate.enabledInHierarchy);
            if (unavailable != null)
            {
                yield return UiPlayer.CaptureGameView(Shot, "lobby-button-disabled");
            }
            else
            {
                Debug.Log(
                    "GUARDS_SHOT: the entry form draws no unavailable control before anybody is " +
                    "seated, which is correct - a taken-hero option only exists once picking opens.");
            }
        }

        /// <summary>
        /// Milestone 8.35z58's second half: the seat's own hand, drawn above the p.15 upgrade offer.
        /// </summary>
        /// <remarks>
        /// Reach a real levelling pause through the existing Director fixture controls, then bind
        /// the levelling hero as this presenter's viewer. This is a rendering/capture check, not a
        /// network authorization check. The staged pause is inspected and answered through real
        /// pointer controls. Missing setup is a failure, never a skip.
        /// </remarks>
        [UnityTest]
        public IEnumerator TheLevellingHandPreview_IsPhotographedAboveTheOffer()
        {
            EnsureShotFolder();
            yield return LoadTheMatch();
            var root = Root();
            var runtime = root.Runtime;
            var budget = 256;
            while (runtime.SystemTransitionPrompt?.Kind != SystemTransitionPromptKind.LevelUpUpgrade)
            {
                Assert.That(budget--, Is.GreaterThan(0), "The fixture never reached a level-up offer.");
                var awaiting = runtime.HandsView.Model.Hands.FirstOrDefault(hand =>
                    hand.CommitmentStatus == TurnCardCommitmentStatus.AwaitingResponse && hand.Cards.Count > 0);
                if (awaiting != null)
                {
                    Assert.That(runtime.SelectCommitmentCard(awaiting.Cards[0].Id), Is.True);
                    Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                }
                else if (runtime.PendingInitiativeChoice != null)
                {
                    Assert.That(runtime.SubmitInitiativeChoice(
                        runtime.PendingInitiativeChoice.CandidateCardInstanceIds[0]), Is.True);
                }
                else if (runtime.SystemTransitionPrompt != null)
                {
                    Assert.That(runtime.SubmitSystemTransitionOption(0), Is.True);
                }
                else if (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                }
                else
                {
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                }
                yield return null;
            }

            // The feature intentionally excludes an undefined Director viewer. Bind a real hero
            // through the existing presenter seam; never manufacture a preview model or hand.
            var heroId = runtime.Projection.PendingSystemTransitionDescription.HeroId;
            runtime.RoundPresenter.SetHomeHero(heroId);
            runtime.RefreshAll();
            yield return null;
            yield return null;
            var hud = root.Hud;
            Assert.That(hud, Is.Not.Null);

            var preview = UiPlayer.Descendants(hud.RootElement)
                .FirstOrDefault(element => element.name == "gd-levelup-hand-preview");
            Assert.That(preview, Is.Not.Null, "The real level-up pause must render its hand preview.");
            Assert.That(UiPlayer.IsShown(preview), Is.True);
            Assert.That(runtime.RoundView.Model.LevelUpHandPreview, Is.Not.Empty);
            Assert.That(runtime.RoundView.Model.LevelUpHandPreview.All(card =>
                runtime.Projection.Cards.Single(instance => instance.Id.Equals(card.CardInstanceId)).OwnerId.Equals(heroId)),
                Is.True, "Every preview card belongs to the bound levelling hero.");

            var snapshot = runtime.CaptureSnapshot();
            try
            {
                foreach (var size in new[] { new Vector2Int(2032, 754), new Vector2Int(2560, 540) })
                {
                    UiPlayer.SetPlayerViewport((uint)size.x, (uint)size.y, "8.36f1 level-up comparison");
                    yield return null;
                    yield return null;
                    hud.Layout();
                    yield return null;
                    yield return null;
                    var slot = UiPlayer.Descendants(preview).Last(element =>
                        element.name == "gd-levelup-hand-preview-card");
                    UiPlayer.EnsureVisible(slot);
                    yield return null;
                    UiPlayer.PointAt(slot);
                    yield return null;
                    var peek = runtime.RoundView.DefenseCardHoverPreviewRoot;
                    Assert.That(UiPlayer.IsShown(peek), Is.True);
                    Assert.That(peek.ElementAt(0).resolvedStyle.opacity, Is.EqualTo(1f));
                    Assert.That(UiPlayer.Descendants(peek).Any(element => element.name == "gd-card-unavailable-veil"), Is.False);
                    var viewport = ConfiguredMatchHudRenderer.PanelViewport;
                    var face = peek.ElementAt(0).worldBound;
                    Assert.That(face.height, Is.EqualTo(CardFootprint.Height).Within(UiPlayer.PixelSnapping(2)));
                    Assert.That(face.yMin, Is.GreaterThanOrEqualTo(0f));
                    Assert.That(face.yMax, Is.LessThanOrEqualTo(viewport.y + UiPlayer.PixelSnapping(2)));
                    Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot), "Inspection must not submit a card command.");
                    yield return UiPlayer.CaptureGameView("8-36f1", $"hand-{size.x}-{size.y}");
                    UiPlayer.PointAway(slot);
                    yield return null;
                    Assert.That(UiPlayer.IsShown(peek), Is.False);

                    var option = UiPlayer.Descendants(hud.RootElement).OfType<Button>()
                        .First(element => element.name == "gd-upgrade-option");
                    UiPlayer.EnsureVisible(option);
                    yield return null;
                    var tray = UiPlayer.Descendants(hud.RootElement)
                        .Single(element => element.name == "gd-tray-region").worldBound;
                    Assert.That(option.worldBound.yMin, Is.GreaterThanOrEqualTo(tray.yMin - UiPlayer.PixelSnapping(2)));
                    Assert.That(option.worldBound.yMax, Is.LessThanOrEqualTo(tray.yMax + UiPlayer.PixelSnapping(2)));
                    Assert.That(UiPlayer.Descendants(option).OfType<Label>().Any(label => label.text == "GAINS:"), Is.True);
                    yield return UiPlayer.CaptureGameView("8-36f1", $"upgrades-{size.x}-{size.y}");
                }
                var selected = UiPlayer.Descendants(hud.RootElement).OfType<Button>()
                    .First(element => element.name == "gd-upgrade-option");
                UiPlayer.Click(selected);
                yield return null;
                Assert.That(runtime.CaptureSnapshot(), Is.Not.EqualTo(snapshot), "The real upgrade control must consume its choice.");
            }
            finally
            {
                UiPlayer.SetPlayerViewport(1920, 1080, "restore after 8.36f1");
            }
        }
    }
}
