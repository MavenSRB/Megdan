using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Effects;
using GuardsDigital.Core.Participants;
using GuardsDigital.Core.Replay;
using GuardsDigital.Core.Rules.CardResolution;
using GuardsDigital.Core.Rules.Modifications;
using GuardsDigital.Core.Setup;
using GuardsDigital.Core.Units.Heroes;
using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Interaction;
using GuardsDigital.Presentation.Match;
using GuardsDigital.Presentation.Units.Heroes;
using GuardsDigital.Presentation.Units.Minions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GuardsDigital.Tests.PlayMode
{
    public sealed class MinimalHexGridPlayModeTests
    {
        [UnityTest]
        public IEnumerator Scene_LoadsCompleteSelectedMapPresentationUnderFramedFixedCamera()
        {
            yield return LoadScene();

            var tiles = Object.FindObjectsByType<HexTileView>();
            var markers = Object.FindObjectsByType<BoardMapSpawnMarkerView>();
            var cameras = Object.FindObjectsByType<Camera>();
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var minions = Object.FindObjectsByType<StandardMinionView>();
            var root = Root();

            Assert.That(root.Runtime.Setup.WaveCount, Is.EqualTo(3));
            Assert.That(root.Runtime.Setup.Teams,
                Has.All.Matches<QuickGameTeamSetupState>(team => team.LifeCount == 4));
            Assert.That(mapView.RenderedDefinition.Reference, Is.EqualTo(OfficialBoardMapCatalog.SelectedMap));
            Assert.That(tiles, Has.Length.EqualTo(292));
            Assert.That(tiles.Count(tile => tile.IsPlayable), Is.EqualTo(210));
            Assert.That(tiles.Count(tile => tile.SpaceKind == BoardMapSpaceKind.Terrain), Is.EqualTo(82));
            Assert.That(markers, Has.Length.EqualTo(40));
            Assert.That(minions, Has.Length.EqualTo(12));
            Assert.That(minions, Has.All.Matches<StandardMinionView>(view =>
                view.GetComponent<SpriteRenderer>().sprite != null));
            Assert.That(cameras, Has.Length.EqualTo(1));
            Assert.That(cameras[0].orthographic, Is.True);
            Assert.That(tiles.All(tile =>
            {
                var viewport = cameras[0].WorldToViewportPoint(tile.transform.position);
                return viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            }), Is.True);
        }

        /// <summary>
        /// Milestone 8.4. The scene a player loads deals verified content, which is the whole point of
        /// the milestone and which only a loaded scene can show: every Editor tier composes its own
        /// configuration, so none of them can say what the saved scene's composition root builds.
        ///
        /// It also holds the one-shot property the showcase request depends on. The fixtures above ask
        /// for the mechanics-showcase hands and never clear the request, because the composition clears
        /// it; if it were a mode that stayed on, this case would find showcase cards here.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DealsTheVerifiedWaspMirrorAndKeepsDealingItAfterAShowcaseRequest()
        {
            yield return LoadShowcaseScene();
            Assert.That(
                Root().Runtime.Cards.Any(card =>
                    card.DefinitionReference.DefinitionId.Value == "placeholder-red-pursuit"),
                Is.True,
                "The requested composition is the showcase one, or the reset below proves nothing.");

            yield return LoadScene();
            var runtime = Root().Runtime;

            Assert.That(runtime.Cards, Has.Count.EqualTo(36),
                "Two eighteen-card decks, which is the smoke gate's pinned cards=36 marker.");
            Assert.That(
                runtime.Definitions.All(definition =>
                    definition.ContentAuthority == CardContentAuthority.VerifiedOfficial &&
                    definition.ContentRevision == WaspCardContent.ContentRevision),
                Is.True,
                "and every one of them is source-verified Wasp content at its stated revision.");
            Assert.That(
                runtime.Cards
                    .Where(card => card.State.Location == CardLocation.Hand)
                    .Select(card => card.DefinitionReference.DefinitionId)
                    .Distinct()
                    .ToArray(),
                Is.EquivalentTo(WaspCardContent.StartingHand),
                "Both seats hold rulebook p.5's starting five.");
        }

        [UnityTest]
        public IEnumerator Scene_RendersHeaderAndBlocksBoardPointerBehindConfiguredHud()
        {
            yield return LoadScene();
            var root = Root();
            var header = root.Hud.Header;
            var side = root.Runtime.TieBreakerState.RevealedSide;
            var indicator = root.Hud.TieBreakerIndicatorRect;

            Assert.That(header.TieBreaker, Is.EqualTo(root.Runtime.Setup.TieBreakerState));
            Assert.That(header.Text, Does.Contain(side == TeamSide.Red ? "[R] RED" : "[B] BLUE"));
            Assert.That(header.Text, Does.Contain("WAVES 3"));
            Assert.That(header.Text, Does.Contain("LIVES  [R] 4  [B] 4"));
            Assert.That(indicator.xMax, Is.EqualTo(Screen.width - 12f).Within(0.01f));
            Assert.That(indicator.yMin, Is.EqualTo(12f).Within(0.01f));
            var guiPoint = indicator.center;
            Assert.That(root.Hud.BlocksPointerAt(new Vector2(guiPoint.x, Screen.height - guiPoint.y)), Is.True);

        }

        [UnityTest]
        public IEnumerator Scene_KeepsConfiguredCardPanelInsideRuntimeViewport()
        {
            yield return LoadScene();

            var panel = Root().Hud.CardPanelRect;
            Assert.That(panel.xMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.yMin, Is.GreaterThanOrEqualTo(0f));
            Assert.That(panel.xMax, Is.LessThanOrEqualTo(Screen.width));
            Assert.That(panel.yMax, Is.LessThanOrEqualTo(Screen.height));
            Assert.That(panel.width,
                Is.EqualTo(Mathf.Min(1280f, Mathf.Max(0f, Screen.width - 24f))).Within(0.01f));
            // Milestone 7.8a: the tray also clears the standing status strip, so the two never claim
            // the same pixels and neither guaranteed surface can hide behind the other. Its height
            // bound is 360 or a share of a tall viewport, whichever is larger, and never more than the
            // space left below the strip. Milestone 8.5a raised that share to 0.46, because a tray card
            // now carries its face's whole printed text.
            Assert.That(panel.height,
                Is.EqualTo(Mathf.Min(
                    Mathf.Max(360f, Screen.height * 0.46f),
                    Mathf.Max(0f, Screen.height - 24f - ConfiguredMatchHudRenderer.StatusBarHeight)))
                    .Within(0.01f));
            Assert.That(panel.yMin,
                Is.GreaterThanOrEqualTo(ConfiguredMatchHudRenderer.StatusBarHeight));
        }

        /// <summary>
        /// Milestone 7.8a: the HUD is one live UI Toolkit panel in the running scene, the two hero rails
        /// and the tray are on it, and the pointer is blocked over each of them.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_BuildsTheRuntimeHudPanelWithBothHeroRailsAndBlocksThePointerOverThem()
        {
            yield return LoadScene();
            var root = Root();
            var hud = root.Hud;

            Assert.That(hud.RootElement, Is.Not.Null, "The configured HUD builds a runtime panel.");
            Assert.That(hud.RootElement.panel, Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-topbar"), Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-tray-region"), Is.Not.Null);
            Assert.That(Named(hud.RootElement, "gd-director-toggle"), Is.Not.Null);

            var rails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(rails, Has.Length.EqualTo(root.Runtime.Setup.Heroes.Count),
                "One overview per projected hero, drawn as a repeated component.");

            foreach (var rect in new[] { hud.CardPanelRect, hud.TieBreakerIndicatorRect })
            {
                var guiPoint = rect.center;
                Assert.That(
                    hud.BlocksPointerAt(new Vector2(guiPoint.x, Screen.height - guiPoint.y)),
                    Is.True,
                    $"The HUD consumes the pointer over {rect}.");
            }

            Assert.That(hud.IsDirectorDrawerOpen, Is.False);
            var acceptedBefore = root.Runtime.AcceptedCommands.Count;
            var roundBefore = root.Runtime.RoundIndex;
            root.Runtime.ToggleDirectorDrawer();
            Assert.That(hud.IsDirectorDrawerOpen, Is.True);
            Assert.That(root.Runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore),
                "Opening the drawer submits no command.");
            Assert.That(root.Runtime.RoundIndex, Is.EqualTo(roundBefore));
            root.Runtime.ToggleDirectorDrawer();
            Assert.That(hud.IsDirectorDrawerOpen, Is.False);
        }

        /// <summary>
        /// Milestone 7.8a's visible Game View sweep: the standing HUD, the HUD with the `DIRECTOR`
        /// drawer open, and the contextual region drawing a paused leveling prompt.
        ///
        /// The prompt is rendered into the scene's own view from a presentation-shaped model, which is
        /// how milestone 6.1a's removal prompt and milestone 5.8b's Defense preview are captured here
        /// too: the configured symmetric setup reaches neither state in one round, and what a capture is
        /// evidence for is contrast, overlap, and clipping rather than authority. Whether the options are
        /// the authority's own is proved in <c>MatchHudPresentationTests</c> against a live pause.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_CapturesTheStandingHudAndOneContextualStateForGameViewInspection()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            yield return null;

            yield return Capture("tmp/milestone-7-8a-standing-hud.png");

            runtime.ToggleDirectorDrawer();
            yield return null;
            yield return Capture("tmp/milestone-7-8a-director-drawer.png");
            runtime.ToggleDirectorDrawer();
            yield return null;

            var current = runtime.RoundView.Model;
            var upgrade = new SystemTransitionPromptViewModel(
                GuardsDigital.Core.Rules.Transitions.SystemTransitionPromptKind.LevelUpUpgrade,
                "LEVEL UP",
                "RED WASP REACHED LEVEL 2 - UPGRADE A CARD",
                "PAID 1 COIN - 1 REMAINING",
                // Three upgradable cards with p.15's two candidates each, which is the shape the
                // configured hand actually produces: the grid breaks a row per card, and each row
                // comes out one colour because the page finds both candidates at the chosen card's
                // own colour.
                UpgradeOptions(CardColor.Red, "RED BOMBARD", "red-bombard-card")
                    .Concat(UpgradeOptions(CardColor.Blue, "RED BULWARK", "red-bulwark-card"))
                    .Concat(UpgradeOptions(CardColor.Green, "RED PURSUIT", "red-pursuit-card")));
            runtime.RoundView.Render(new RoundActionViewModel(
                current.Version + 1,
                current.RoundIndex,
                current.TurnIndex,
                current.Phase,
                "End of round - rulebook p.15 is waiting on an upgrade.",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                upgrade));
            root.Hud.ApplyExclusiveContext();
            yield return null;
            Assert.That(runtime.RoundView.HasExclusiveDecision, Is.True,
                "A paused transition is a decision the tray steps aside for.");
            yield return Capture("tmp/milestone-7-8a-leveling-prompt.png");
        }

        /// <summary>
        /// Milestone 8.5a's targeted Game View inspection, and the running-scene proof behind it: the
        /// tray draws Wasp's verified starting five with each face's own words, in the five regions the
        /// approved mockup gives them, and every card lands inside the tray it is drawn in.
        ///
        /// It runs against the saved scene rather than a fixture because the layout is what is being
        /// checked, and only a live panel resolves one. The Editor tier proves the grouping is the
        /// definition's; this proves the cards fit.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_DrawsTheVerifiedCardFacesInTheTrayAndCapturesThemForInspection()
        {
            yield return LoadScene();
            var root = Root();
            var hud = root.Hud;
            // Two frames, so the panel has laid out and every card has a resolved rectangle.
            yield return null;
            yield return null;

            var cards = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-choice")
                .ToArray();
            Assert.That(cards, Has.Length.EqualTo(5),
                "The viewed hero's whole current round is rulebook p.5's starting five.");

            foreach (var card in cards)
            {
                foreach (var region in new[]
                    {
                        "gd-card-initiative", "gd-card-name-band", "gd-card-secondary-values",
                        "gd-card-primary-action", "gd-card-rules", "gd-card-history"
                    })
                {
                    Assert.That(
                        Descendants(card).Count(element => element.name == region), Is.EqualTo(1),
                        $"Each tray card draws exactly one {region}.");
                }
            }

            var printed = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-printed-text")
                .OfType<UnityEngine.UIElements.Label>()
                .Select(label => label.text)
                .ToArray();
            var transcriptions = root.Runtime.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(root.Runtime.ViewedHeroId))
                .Select(card => root.Runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .SourceContent.PrintedText)
                .ToArray();
            Assert.That(printed, Is.EquivalentTo(transcriptions),
                "and the words on screen are the verified transcriptions, whole and unabbreviated.");

            var tray = Named(hud.RootElement, "gd-tray-region");
            // The vertical bound is only claimed where the viewport is tall enough to hold a card plus
            // the tray's own chrome. A 480-line Game View cannot, and the tray scrolls there rather than
            // clipping; milestone 8.5a's acceptance is stated at the supported Windows validation
            // resolution, which is where the targeted non-batch inspection runs.
            var trayHoldsACard =
                hud.CardPanelRect.height >= DirectorHandsUnityView.CardMinimumHeight + 140f;
            foreach (var card in cards)
            {
                var bounds = card.worldBound;
                Assert.That(bounds.width, Is.GreaterThan(0f), "Every card resolved a real rectangle.");
                Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(tray.worldBound.xMin - 0.5f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(tray.worldBound.xMax + 0.5f),
                    "and no card runs out through the side of the tray.");
                if (trayHoldsACard)
                {
                    Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(tray.worldBound.yMin - 0.5f));
                    Assert.That(bounds.yMax, Is.LessThanOrEqualTo(tray.worldBound.yMax + 0.5f),
                        "and none is cut off at its foot or pushed behind a scrollbar - the tray's " +
                        "height bound has to hold the tallest verified face's wrapped rules text.");
                }
            }

            // One row: the cards share a top edge. Checked only where the viewport is actually wide
            // enough for five of them, so a cramped Game View reports nothing rather than a false
            // failure - the fixed-resolution arithmetic is asserted in MatchHudPresentationTests.
            var required = (5f * (DirectorHandsUnityView.CardWidth + DirectorHandsUnityView.CardGap +
                (2f * (DirectorHandsUnityView.ActiveOutlineGap +
                    DirectorHandsUnityView.ActiveOutlineWidth)))) + 30f;
            if (hud.CardPanelRect.width >= required)
            {
                Assert.That(cards.Select(card => Mathf.Round(card.worldBound.yMin)).Distinct().Count(),
                    Is.EqualTo(1),
                    "Five cards of one hand are one readable row, never a wrapped second line.");
            }

            // Milestone 8.5a, by the user's 2026-08-21 decision: the board framing reserves a share of
            // the viewport's width along the left edge, where the tray stands, instead of centring the
            // map in the whole viewport.
            //
            // Stated as the asymmetry it produces rather than as a pixel count, so it holds at any
            // resolution or aspect and cannot pass by luck: under the centred framing this replaced the
            // map's margins were equal by construction, so `min == 1 - max` exactly and this comparison
            // is false. The reserve settled where the user wanted the board rather than at the value
            // that would clear the tray completely, so a little of the map is behind the cards and the
            // gain is what the reserve buys - the left band is the tray's.
            var camera = Object.FindAnyObjectByType<Camera>();
            var viewportX = Object.FindObjectsByType<HexTileView>(FindObjectsSortMode.None)
                .Select(tile => camera.WorldToViewportPoint(tile.transform.position).x)
                .ToArray();
            Assert.That(viewportX.Min(), Is.GreaterThan(1f - viewportX.Max()),
                "The map sits right of centre, leaving the viewport's left band to the card tray.");
            Assert.That(viewportX.Min(), Is.GreaterThanOrEqualTo(0f));
            Assert.That(viewportX.Max(), Is.LessThanOrEqualTo(1f),
                "and it is still whole - the reserve never pushes a space out of the view.");

            yield return Capture("tmp/milestone-8-5a-card-tray.png");

            var chosen = root.Runtime.Cards.First(card =>
                card.State.Location == CardLocation.Hand &&
                card.OwnerId.Equals(root.Runtime.ViewedHeroId));
            Assert.That(root.Runtime.SelectCommitmentCard(chosen.Id), Is.True);
            yield return null;
            yield return Capture("tmp/milestone-8-5a-card-tray-selected.png");
        }

        /// <summary>
        /// Milestone 8.5b in the running scene: the card that sourced a live Active effect is outlined
        /// and labelled, a card can be selected and active at once, and both go when the effect ends.
        ///
        /// It runs on the showcase composition because that is the only content in this build that
        /// creates an Active effect - every Wasp printed primary is still greyed under `DEV-006` until
        /// its own slice composes it - and on the round-scoped mode of the Bulwark Skill, so the effect
        /// outlives the turn that made it and is still live while the next turn's commitment phase can
        /// select a card. The three states the milestone asks to see are captured in order.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_MarksTheSourceCardWhileItsActiveEffectIsLiveAndCapturesEachState()
        {
            yield return LoadShowcaseScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            yield return null;

            var bulwark = runtime.Cards.Single(card => card.Id.Value == "red-bulwark-card");
            var blueHeroId = runtime.Setup.Heroes.Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;
            var blue = runtime.Cards
                .Where(card => card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(blueHeroId))
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            foreach (var card in new[] { bulwark, blue })
            {
                Assert.That(runtime.SelectCommitmentCard(card.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            }

            Assert.That(runtime.SubmitAction(new ActionDefinitionId("primary-modification-skill")),
                Is.True);
            Assert.That(
                runtime.SubmitEffectChoiceOption(new EffectNodeId("bulwark-fast-travel-restriction")),
                Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);
            yield return null;

            Assert.That(runtime.ActiveEffectSourceCardInstanceIds,
                Is.EqualTo(new[] { bulwark.Id }),
                "The authority reports exactly the card that sourced the live effect.");
            // The turn may already have moved on to the other hero, and the tray follows whoever the
            // match is waiting on; the indicator is a fact about a card, so point the tray at the hand
            // that holds it before reading it.
            Assert.That(runtime.SelectViewedHero(bulwark.OwnerId), Is.True);
            yield return null;
            AssertOneCardIsMarkedActive(hud, "while the effect is live");
            yield return Capture("tmp/milestone-8-5b-active.png");

            // Play the turn out, so the next turn's commitment phase can select a card while the
            // round-scoped effect is still live - the two states standing together. The Skill may have
            // closed its own turn already, so the advance is offered rather than assumed.
            AdvancePastTheCurrentTurn(runtime);
            yield return null;
            if (runtime.Phase == RoundResolutionPhase.AwaitingCommitments &&
                runtime.IsActiveEffectSource(bulwark.Id))
            {
                var selectable = runtime.Cards.First(card =>
                    card.State.Location == CardLocation.Hand &&
                    card.OwnerId.Equals(bulwark.OwnerId));
                Assert.That(runtime.SelectCommitmentCard(selectable.Id), Is.True);
                Assert.That(runtime.SelectViewedHero(bulwark.OwnerId), Is.True);
                yield return null;
                AssertOneCardIsMarkedActive(hud, "with another card selected beside it");
                yield return Capture("tmp/milestone-8-5b-active-and-selected.png");
            }

            for (var turn = 0; turn < 8 && runtime.Phase != RoundResolutionPhase.EndOfRound; turn++)
            {
                if (runtime.Phase == RoundResolutionPhase.AwaitingCommitments)
                {
                    CommitNextTurn(runtime);
                }

                AdvancePastTheCurrentTurn(runtime);
            }

            Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound),
                "The round has to reach its end for a round-scoped effect to expire.");

            while (runtime.Phase == RoundResolutionPhase.EndOfRound)
            {
                var options = runtime.PausedSystemTransitionOptions;
                if (options.Count > 0)
                {
                    Assert.That(runtime.AnswerPausedSystemTransition(options[0]), Is.True);
                    continue;
                }

                Assert.That(runtime.ContinueEndOfRound(), Is.True);
            }

            yield return null;
            Assert.That(runtime.ActiveEffectSourceCardInstanceIds, Is.Empty,
                "Round End expires a round-scoped effect, so nothing is live any more.");
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-card-active"),
                Is.EqualTo(0),
                "and the indicator is gone with it rather than left behind by presentation.");
            yield return Capture("tmp/milestone-8-5b-ended.png");
        }

        /// <summary>
        /// Ends the current turn, however many projected stops that takes and whether or not the action
        /// already closed it. Nothing here assumes a turn shape - only that the round moves on.
        /// </summary>
        /// <summary>
        /// Milestone 8.7a in the running scene: the hero rail keeps the card being played in its own
        /// header marker and only numbers a turn once that card has resolved.
        ///
        /// It runs against the saved scene rather than a fixture because the three things the milestone
        /// asks to see are all layout - the marker fits the compact rail beside the hero's name, the
        /// mirrored rail puts it on the opposite side, and the whole rail still stands inside the region
        /// it reserved. The Editor tier proves the states themselves against live authority. The three
        /// captures are the targeted Game View inspection against the approved mockup.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_SeparatesTheCardInPlayFromResolvedTurnHistoryOnBothRails()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            var hud = root.Hud;
            yield return null;
            yield return null;

            var rails = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-hero-rail")
                .ToArray();
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-current-card-slot"),
                Is.EqualTo(rails.Length),
                "Every rail reserves the marker's footprint, so the header never moves.");
            Assert.That(
                Descendants(hud.RootElement).Any(element => element.name == "gd-current-card"),
                Is.False,
                "Nobody has committed, so no rail draws a marker inside its slot.");

            // One hero committed: a facedown back, and nothing about the card.
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            yield return null;
            var facedown = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-current-card")
                .ToArray();
            Assert.That(facedown, Has.Length.EqualTo(1));
            Assert.That(
                Descendants(facedown[0]).Any(element => element.name == "gd-current-card-back"),
                Is.True,
                "A commitment nobody may read yet is a neutral back.");
            Assert.That(
                Descendants(hud.RootElement).OfType<UnityEngine.UIElements.Label>()
                    .Where(label => label.name == "gd-current-card-state")
                    .Select(label => label.text),
                Is.EquivalentTo(new[] { "FACEDOWN" }),
                "A back with nothing on it says nothing, so the facedown marker keeps its word.");
            yield return Capture("tmp/milestone-8-7a-facedown.png");

            // Both committed: p.7's joint reveal, both cards in play, no turn numbered yet.
            var blue = Hand(runtime, TeamSide.Blue).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            yield return null;
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-current-card"),
                Is.EqualTo(2),
                "Joint reveal puts both heroes' cards in play, one marker each.");
            Assert.That(
                Descendants(hud.RootElement).Any(element => element.name == "gd-current-card-state"),
                Is.False,
                "and a revealed marker carries its colour and Initiative rather than a caption.");
            Assert.That(
                runtime.HeroOverviewsView.Model.Heroes
                    .SelectMany(hero => hero.TurnSlots).Any(slot => slot.IsOccupied),
                Is.False,
                "The round's history names both cards already; neither turn is over.");

            // Every rail stays inside its own reserved region with the marker in it, and the two rails
            // are mirrored - the marker on the inner corner of each.
            var playfield = ConfiguredMatchHudRenderer.CalculatePlayfieldRect(Screen.width, Screen.height);
            foreach (var rail in Descendants(hud.RootElement).Where(element =>
                element.name == "gd-hero-rail"))
            {
                var bounds = rail.worldBound;
                Assert.That(bounds.width, Is.GreaterThan(0f), "Every rail resolved a real rectangle.");
                Assert.That(bounds.width,
                    Is.LessThanOrEqualTo(ConfiguredMatchHudRenderer.HeroRailWidth + 0.5f),
                    "and the marker did not widen the rail past its reserved band.");
                var marker = Descendants(rail).First(element => element.name == "gd-current-card-slot");
                var onLeftEdge = bounds.center.x < playfield.center.x;
                Assert.That(
                    marker.worldBound.center.x > bounds.center.x,
                    Is.EqualTo(onLeftEdge),
                    "The marker sits on each rail's inner corner: right on the left rail, left on the " +
                    "mirrored one.");
            }

            yield return Capture("tmp/milestone-8-7a-resolving.png");

            // The acting hero's card resolves: its marker clears and its turn is numbered history.
            var acting = runtime.SelectedActor.ActorId;
            AdvancePastTheCurrentTurn(runtime);
            yield return null;
            var resolved = runtime.HeroOverviewsView.Model.Heroes
                .Single(hero => hero.HeroId.Equals(acting));
            Assert.That(resolved.CurrentCard, Is.Null);
            Assert.That(resolved.TurnSlots.Count(slot => slot.IsOccupied), Is.EqualTo(1),
                "and exactly one numbered turn is now history.");
            Assert.That(
                Descendants(hud.RootElement).OfType<UnityEngine.UIElements.Label>()
                    .Count(label => label.name == "gd-turn-slots-caption" && label.text == "RESOLVED TURNS"),
                Is.EqualTo(rails.Length),
                "and both rails caption the numbered row as what it is.");
            yield return Capture("tmp/milestone-8-7a-resolved.png");
        }

        private static void AdvancePastTheCurrentTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var turnIndex = runtime.TurnIndex;
            for (var guard = 0; guard < 6; guard++)
            {
                if (runtime.Phase != RoundResolutionPhase.ResolvingTurn ||
                    runtime.TurnIndex != turnIndex)
                {
                    return;
                }

                if (!runtime.SubmitEndTurn())
                {
                    return;
                }
            }
        }

        private static void AssertOneCardIsMarkedActive(
            ConfiguredMatchHudRenderer hud, string because)
        {
            var slots = Descendants(hud.RootElement)
                .Where(element => element.name == "gd-card-slot")
                .ToArray();
            Assert.That(
                slots.Count(slot =>
                    slot.style.borderTopColor.value == MatchHudTheme.ActiveEffectAccent),
                Is.EqualTo(1),
                $"Exactly one card wears the Active-effect outline {because}.");
            Assert.That(
                Descendants(hud.RootElement).Count(element => element.name == "gd-card-active"),
                Is.EqualTo(1),
                $"and it carries the word ACTIVE {because}, so the state is never colour alone.");
        }

        /// <summary>p.15's two answers for one card, both at that card's own colour.</summary>
        private static IEnumerable<SystemTransitionOptionViewModel> UpgradeOptions(
            CardColor color, string name, string instanceId)
        {
            foreach (var variant in new[] { "A", "B" })
            {
                var sibling = variant == "A" ? "B" : "A";
                yield return new SystemTransitionOptionViewModel(
                    $"UPGRADE TEST - {name} INTO TEST - {name} II-{variant}",
                    $"SETS TEST - {name} II-{sibling} ASIDE AS A ATK +1 ITEM",
                    DirectorCommand.AdvanceEndOfRound(),
                    color,
                    new CardInstanceId(instanceId));
            }
        }

        private static IEnumerator Capture(string relativePath)
        {
            if (UnityEngine.Application.isBatchMode)
            {
                yield break;
            }

            var path = Path.GetFullPath(relativePath);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return null;
            yield return null;
            Assert.That(File.Exists(path), Is.True,
                $"The targeted Game View visual evidence must be captured at {relativePath}.");
        }

        private static UnityEngine.UIElements.VisualElement Named(
            UnityEngine.UIElements.VisualElement root, string name) =>
            Descendants(root).FirstOrDefault(element => element.name == name);

        private static IEnumerable<UnityEngine.UIElements.VisualElement> Descendants(
            UnityEngine.UIElements.VisualElement element)
        {
            yield return element;
            foreach (var child in element.Children())
            {
                foreach (var nested in Descendants(child))
                {
                    yield return nested;
                }
            }
        }

        [UnityTest]
        public IEnumerator Scene_LoadsVisibleConfiguredHeroesAndPassiveBoardCallbacks()
        {
            yield return LoadScene();
            var heroes = Object.FindObjectsByType<PrimitiveHeroView>();
            var selectors = Object.FindObjectsByType<PrimitiveHeroPointerSelector>();

            Assert.That(heroes, Has.Length.EqualTo(2));
            Assert.That(heroes.Select(hero => hero.Occupancy.HeroId.Value), Is.EquivalentTo(new[]
            {
                PlaceholderOneVsOneParticipants.RedHeroIdValue,
                PlaceholderOneVsOneParticipants.BlueHeroIdValue
            }));
            Assert.That(selectors, Has.Length.EqualTo(2));
            Assert.That(selectors, Has.All.Matches<PrimitiveHeroPointerSelector>(selector =>
                selector.GetComponent<PrimitiveHeroView>() != null &&
                selector.GetComponent<CapsuleCollider>() != null));
            Assert.That(heroes, Has.All.Matches<PrimitiveHeroView>(hero =>
                hero.GetComponent<MeshRenderer>().enabled &&
                hero.GetComponent<MeshFilter>().sharedMesh != null));
        }

        /// <summary>
        /// Milestone 6.5c, corrected after review. The hero choice cue, checked against the scene's
        /// own camera rather than against an assumption about which axis is up.
        ///
        /// This is the check the milestone should have carried. It asserted where the marker sat in
        /// the hero's local space, which stayed true while the marker was edge-on to a camera that
        /// looks straight down the board and therefore invisible - the defect a player saw
        /// immediately and no automated check did. Milestone 6.1a already had the honest form for a
        /// minion, projecting through the configured camera, and this is that form for the hero,
        /// which the two views now share one cue construction for.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ShowsTheHeroChoiceCueFacingTheConfiguredCamera()
        {
            yield return LoadScene();
            var hero = Object.FindObjectsByType<PrimitiveHeroView>()
                .Single(value => value.Occupancy.HeroId.Value ==
                    PlaceholderOneVsOneParticipants.RedHeroIdValue);
            var camera = Object.FindAnyObjectByType<Camera>();

            hero.SetChoiceCue(MinionChoiceCueKind.Highlighted);
            yield return null;

            var cue = hero.GetComponentsInChildren<TextMesh>(true)
                .Single(value => value.gameObject.name == "Hero Choice Cue");
            Assert.That(cue.text, Is.EqualTo(BoardChoiceCue.Marker));
            Assert.That(cue.gameObject.activeInHierarchy, Is.True);

            var toCamera = (camera.transform.position - cue.transform.position).normalized;
            Assert.That(Vector3.Dot(cue.transform.forward, toCamera), Is.GreaterThan(0.9f),
                "The marker faces the camera instead of standing edge-on to it.");
            Assert.That(camera.WorldToViewportPoint(cue.transform.position).y,
                Is.GreaterThan(camera.WorldToViewportPoint(hero.transform.position).y),
                "And it reads above its hero, exactly as the minion cue reads above its minion.");

            hero.SetChoiceCue(MinionChoiceCueKind.None);
            yield return null;
            Assert.That(cue.gameObject.activeInHierarchy, Is.False);
        }

        [UnityTest]
        public IEnumerator Scene_RoutesConfiguredMinionAttackCuePointerAndRemovalThroughFeatureBoundary()
        {
            yield return LoadShowcaseScene();
            var root = Root();
            var runtime = root.Runtime;
            var mapView = Object.FindAnyObjectByType<PlaceholderTestGridView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var transientSetupField = ReflectedMember.RequiredInstanceField(
                typeof(StandardMinionView),
                "occupancy");
            foreach (var configuredMinion in Object.FindObjectsByType<StandardMinionView>())
            {
                transientSetupField.SetValue(configuredMinion, null);
            }

            mapView.Rebuild(mapView.RenderedDefinition, runtime.Setup);
            yield return null;

            var reboundViews = Object.FindObjectsByType<StandardMinionView>();
            Assert.That(reboundViews, Has.Length.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews, Is.EquivalentTo(reboundViews));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(reboundViews));
            var redHeroId = new GuardsDigital.Core.Units.Heroes.HeroId(
                PlaceholderOneVsOneParticipants.RedHeroIdValue);

            // Reserved before the approach turns rather than looked for after them. Those turns spend
            // Red's highest cards, and since milestone 6.5b converted Red Bombard the team prints
            // exactly one Ranged Attack - so the card this case exists to play would otherwise be the
            // third card the approach burns.
            var attackCard = Hand(runtime, TeamSide.Red).Cards
                .Where(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack &&
                        action.AttackRange == AttackRangeKind.Ranged))
                .OrderByDescending(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();

            for (var approachTurn = 0; approachTurn < 3; approachTurn++)
            {
                // The approach card is named by the printed action it has to perform rather than by
                // being the top of the band. Milestone 6.2d needed that, because the scene was dealt
                // milestone 6.2's Cache card as a sixth - its only printed action a Skill, and its
                // Initiative above every ordinary Red card - so "Red's highest" picked a card with no
                // Movement to approach with. The five-card scene deals no such card any more, and the
                // filter stays because naming the action a fixture needs is the right way round: it
                // survives whatever a later conversion does to the top of the band.
                var red = Hand(runtime, TeamSide.Red).Cards
                    .Where(card => !card.Id.Equals(attackCard.Id) &&
                        runtime.Definitions.Single(definition =>
                            definition.Reference.Equals(card.DefinitionReference))
                        .ActionDefinitions.Any(action =>
                            action.Kind == GuardsDigital.Core.Cards.ActionKind.Movement))
                    .OrderByDescending(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                var blue = Hand(runtime, TeamSide.Blue).Cards
                    .OrderBy(card => runtime.Definitions.Single(definition =>
                        definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                    .First();
                Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
                Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
                if (runtime.PendingInitiativeChoice != null)
                {
                    Assert.That(runtime.SubmitInitiativeChoice(red.Id), Is.True);
                }

                if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
                {
                    Assert.That(runtime.SubmitEndTurn(), Is.True,
                        "The earlier opposing actor should complete by Hold before the Red approach action.");
                }

                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
                var movement = runtime.RoundView.Model.Actions.Single(value =>
                    ActionKind(runtime, value.Command.ActionDefinitionId) ==
                    GuardsDigital.Core.Cards.ActionKind.Movement);
                Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
                while (runtime.PendingMovementChoice != null)
                {
                    var blueMinions = runtime.BoardView.Model.Minions
                        .Where(value => value.TeamSide == TeamSide.Blue &&
                            value.MinionType != StandardMinionType.Heavy)
                        .ToArray();
                    var destination = runtime.PendingMovementChoice.LegalDestinations
                        .OrderBy(value => blueMinions.Min(minion =>
                            HexDistance(value.Destination, minion.Coordinate)))
                        .ThenByDescending(value => value.Cost)
                        .First().Destination;
                    Assert.That(runtime.BoardPresenter.SubmitDestination(destination), Is.True);
                }

                if (runtime.IsTurnActive)
                {
                    Assert.That(runtime.SelectedActor.ActorId, Is.Not.EqualTo(redHeroId));
                    Assert.That(runtime.SubmitEndTurn(), Is.True,
                        "The later opposing actor should complete by Hold after the approach action.");
                }
            }

            var finalBlueCard = Hand(runtime, TeamSide.Blue).Cards
                .OrderBy(card => runtime.Definitions.Single(definition =>
                    definition.Reference.Equals(card.DefinitionReference)).Initiative.Value)
                .First();
            Assert.That(runtime.SelectCommitmentCard(attackCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(finalBlueCard.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(attackCard.Id), Is.True);
            }

            if (!runtime.SelectedActor.ActorId.Equals(redHeroId))
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True,
                    "The earlier opposing actor should complete by Hold before the Red Attack.");
            }

            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(redHeroId));
            var attack = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            Assert.That(runtime.RoundPresenter.Submit(attack), Is.True);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Is.Not.Empty);
            var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.First();
            var minion = Object.FindObjectsByType<StandardMinionView>()
                .Single(value => value.Occupancy.MinionId.Equals(targetId));
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(minion.Occupancy.Coordinate));
            Assert.That(runtime.BoardView.Model.MinionAttackTargets, Does.Contain(targetId));
            Assert.That(tile.IsHighlighted, Is.True,
                "The exact projected minion occupancy must receive the configured target cue.");
            Assert.That(minion.GetComponent<StandardMinionPointerSelector>(), Is.Not.Null);
            var pendingTargetSnapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseAttackTarget));
            Assert.That(runtime.AcceptedCommands.Last().MinionId, Is.EqualTo(targetId));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(attackCard.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
            Assert.That(tile.IsHighlighted, Is.False);
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionDefeatRecorded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRewardAwarded"));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.EventType), Does.Contain(
                "Action.MinionRemovalCompleted"));

            Assert.That(runtime.Undo(), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(tile.IsHighlighted, Is.True);

            Assert.That(runtime.Restart(), Is.True);
            yield return null;
            Assert.That(runtime.BoardView.Model.Minions, Has.Count.EqualTo(12));
            Assert.That(runtime.BoardView.MinionViews.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));

            Assert.That(runtime.RestoreSnapshot(pendingTargetSnapshot), Is.True, runtime.LastOperationDetail);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice.LegalTargetMinionIds, Does.Contain(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.True);
            Assert.That(tile.IsHighlighted, Is.True);
            Assert.That(pointerInput.TryHandlePointerRay(DownRay(minion.transform.position)), Is.True);
            yield return null;
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.BoardView.Model.Minions.Select(value => value.MinionId),
                Has.None.EqualTo(targetId));
            Assert.That(minion.gameObject.activeSelf, Is.False);
        }

        [UnityTest]
        public IEnumerator Scene_CommitsBothHandsAndResolvesHoldThroughFeaturePresenters()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];

            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.GetCommitmentStatus(red.OwnerId),
                Is.EqualTo(TurnCardCommitmentStatus.CardCommittedFacedown));
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            yield return null;

            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.Setup.Heroes, Has.All.Matches<QuickGameHeroSetupState>(value =>
                runtime.GetCommitmentStatus(value.HeroId) == TurnCardCommitmentStatus.Revealed));
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.PerformHold));
            var firstActor = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.SubmitEndTurn(), Is.True);
            yield return null;
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(firstActor)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.Not.EqualTo(firstActor));
        }

        [UnityTest]
        public IEnumerator Scene_RoutesMovementDestinationAndStopThroughBoardViewPresenter()
        {
            yield return LoadScene();
            var root = Root();
            var runtime = root.Runtime;
            CommitNextTurn(runtime);
            var actorId = runtime.SelectedActor.ActorId;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            Assert.That(runtime.PendingMovementChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.EndTurnInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.StopMovement));

            var destination = runtime.PendingMovementChoice.LegalDestinations
                .First(value => value.Cost == 1).Destination;
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;
            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.ChooseMovementDestination));
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
                Assert.That(runtime.AcceptedCommands.Last().Kind,
                    Is.EqualTo(DirectorCommandKind.StopMovement));
            }

            yield return null;
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.BoardView.Model.MovementDestinations, Is.Empty);
            var fastTravelDestinations = runtime.BoardView.Model.FastTravelDestinations;
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value =>
                    value.IsHighlighted == fastTravelDestinations.Contains(value.Coordinate)));
        }

        [UnityTest]
        public IEnumerator Scene_OffersAndRoutesFastTravelThroughConfiguredPassiveViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextTurn(runtime);
            yield return null;

            var actorId = runtime.SelectedActor.ActorId;
            var sourceCardId = runtime.SelectedActor.CardInstanceId;
            Assert.That(runtime.RoundView.Model.FastTravelOffers, Has.Count.EqualTo(1));
            Assert.That(runtime.RoundView.Model.FastTravelOffers[0].Label,
                Is.EqualTo("FAST TRAVEL"));
            Assert.That(runtime.RoundView.Model.Actions, Is.Not.Empty);
            Assert.That(runtime.BoardView.Model.FastTravelDestinations, Is.Empty,
                "Fast Travel must not be the configured board's default interaction state.");
            Assert.That(Object.FindObjectsByType<HexTileView>(),
                Has.All.Matches<HexTileView>(value => !value.IsHighlighted));

            runtime.RoundView.ForwardFastTravelSelection(
                runtime.RoundView.Model.FastTravelOffers.Single());
            yield return null;

            Assert.That(runtime.BoardView.Model.FastTravelDestinations,
                Has.Count.EqualTo(runtime.RoundView.Model.FastTravelOffers[0].LegalDestinationCount));

            var destination = runtime.BoardView.Model.FastTravelDestinations[0];
            var tile = Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(destination));
            Assert.That(tile.IsHighlighted, Is.True);
            var pointer = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            var commandCount = runtime.AcceptedCommands.Count;

            Assert.That(pointer.TryHandlePointerRay(DownRay(tile.transform.position)), Is.True);
            yield return null;

            Assert.That(runtime.AcceptedCommands.Count, Is.EqualTo(commandCount + 1));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.PerformFastTravel));
            Assert.That(runtime.Heroes.Single(hero => hero.HeroId.Equals(actorId)).Coordinate,
                Is.EqualTo(destination));
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(sourceCardId)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
        }

        /// <summary>
        /// Milestone 6.8a. Red Bulwark's converted Skill is the first Skill in the configured hand, so
        /// this is the scene actually reaching a paused effect frame and finishing it through the
        /// passive round view. The view forwards the projected invocations and nothing else.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_ResolvesTheConvertedSkillThroughThePassiveEffectChoicePrompt()
        {
            yield return LoadShowcaseScene();
            var runtime = Root().Runtime;
            var red = Hand(runtime, TeamSide.Red).Cards
                .Single(card => card.Id.Value == "red-bulwark-card");
            var blue = Hand(runtime, TeamSide.Blue).Cards
                .Single(card => card.Id.Value == "blue-hammer-card");
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));

            var skill = runtime.RoundView.Model.Actions.Single(value =>
                ActionKind(runtime, value.Command.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Skill);
            Assert.That(runtime.RoundPresenter.Submit(skill), Is.True);

            Assert.That(runtime.PendingEffectChoice, Is.Not.Null);
            Assert.That(runtime.RoundView.Model.Actions, Is.Empty,
                "The paused frame withholds the offers the authority would refuse.");
            Assert.That(runtime.RoundView.Model.EffectChoices, Has.Count.EqualTo(3),
                "Every mode of the converted Skill reaches the passive view.");

            var mode = new EffectNodeId("bulwark-defense-bonus");
            Assert.That(runtime.SubmitEffectChoiceOption(mode), Is.True);
            Assert.That(runtime.ConfirmEffectChoice(), Is.True);

            Assert.That(runtime.PendingEffectChoice, Is.Null);
            Assert.That(runtime.RoundView.Model.EffectChoices, Is.Empty);
            Assert.That(runtime.Cards.Single(card => card.Id.Equals(red.Id)).State.ResolutionStatus,
                Is.EqualTo(CardResolutionStatus.Resolved));
            Assert.That(runtime.AcceptedCommands.Last().Kind,
                Is.EqualTo(DirectorCommandKind.AnswerEffectChoice));
        }

        [UnityTest]
        public IEnumerator Scene_RejectsUnavailableAttackWithoutMutationThroughRoundPresenter()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            CommitNextAttackTurn(runtime);

            // Milestone 7.8a1 changed where this Attack is drawn without changing what it does. The
            // configured heroes start far apart, so the authority now judges the Attack unstartable and
            // it keeps its label among the greyed controls instead of sitting in the row of actions that
            // can be taken. What this case has always protected is unchanged and asserted below: a start
            // naming it mutates nothing.
            var unavailable = runtime.RoundView.Model.UnavailableActions.Single(value =>
                ActionKind(runtime, value.ActionDefinitionId) ==
                GuardsDigital.Core.Cards.ActionKind.Attack);
            Assert.That(
                runtime.RoundView.Model.Actions.Any(value =>
                    value.Command.ActionDefinitionId.Equals(unavailable.ActionDefinitionId)),
                Is.False,
                "An action the authority judged unstartable carries no command at all.");

            var snapshot = runtime.CaptureSnapshot();
            var commandCount = runtime.AcceptedCommands.Count;

            // The stale invocation a view could still be holding from an older model.
            var stale = new TypedInvocationViewModel(
                unavailable.Label, DirectorCommand.StartAction(unavailable.ActionDefinitionId));
            Assert.That(runtime.RoundPresenter.Submit(stale), Is.False);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshot));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(commandCount));
            Assert.That(runtime.PendingAttackTargetChoice, Is.Null);
            Assert.That(runtime.PendingDefenseChoice, Is.Null);
        }

        [UnityTest]
        public IEnumerator Scene_PassiveRoundViewPreservesFourPrintedDefenseInvocationsAndDeclineCommands()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var defenseCards = Hand(runtime, TeamSide.Blue).Cards.Take(4).ToArray();
            var invocations = new[]
            {
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE FEINT\nPRINTED DEFENSE 2",
                    DirectorCommand.ChooseDefenseCard(defenseCards[0].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE SENTINEL\nPRINTED DEFENSE 4",
                    DirectorCommand.ChooseDefenseCard(defenseCards[1].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1",
                    DirectorCommand.ChooseDefenseCard(defenseCards[2].Id)),
                new TypedInvocationViewModel(
                    "DISCARD TEST - BLUE HAMMER\nPRINTED DEFENSE 1",
                    DirectorCommand.ChooseDefenseCard(defenseCards[3].Id))
            };
            var declineCommand = DirectorCommand.DeclineDefense();
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                declineCommand);
            var choices = invocations.Concat(new[] { declineInvocation }).ToArray();
            var model = runtime.RoundView.Model;
            TypedInvocationViewModel submitted = null;
            runtime.RoundView.InvocationSubmitted += value => submitted = value;
            runtime.RoundView.Render(new RoundActionViewModel(
                model.Version,
                model.RoundIndex,
                model.TurnIndex,
                model.Phase,
                model.Status,
                model.Actions,
                model.EndTurnInvocation,
                model.ContinueEndOfRoundInvocation,
                model.InitiativeChoices,
                choices));

            yield return null;

            Assert.That(runtime.RoundView.Model.DefenseChoices, Has.Count.EqualTo(5));
            Assert.That(runtime.RoundView.Model.DefenseChoices[2].Label,
                Is.EqualTo("DISCARD TEST - BLUE SCOUT SHOT\nPRINTED DEFENSE 1"));
            Assert.That(runtime.RoundView.Model.DefenseChoices[4].Label, Is.EqualTo("DECLINE DEFENSE"));
            foreach (var invocation in invocations)
            {
                runtime.RoundView.ForwardInvocation(invocation);
                Assert.That(submitted, Is.SameAs(invocation));
                Assert.That(submitted.Command.Kind, Is.EqualTo(DirectorCommandKind.ChooseDefenseCard));
                Assert.That(submitted.Command.CardInstanceId, Is.EqualTo(invocation.Command.CardInstanceId));
            }

            runtime.RoundView.ForwardInvocation(declineInvocation);
            Assert.That(submitted, Is.SameAs(declineInvocation));
            Assert.That(submitted.Command, Is.EqualTo(declineCommand));
        }

        [UnityTest]
        public IEnumerator Scene_PassiveDefensePreviewAndSignedMinionCuesUseConfiguredViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var supporting = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Blue);
            var hindering = runtime.BoardView.Model.Minions.First(value => value.TeamSide == TeamSide.Red);
            var baseBoard = runtime.BoardView.Model;
            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: new[]
                {
                    new MinionDefenseCueViewModel(supporting.MinionId, 1),
                    new MinionDefenseCueViewModel(hindering.MinionId, -1)
                }));

            var supportingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(supporting.MinionId));
            var hinderingView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(hindering.MinionId));
            Assert.That(supportingView.DefenseCueAmount, Is.EqualTo(1));
            var supportingCue = supportingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue");
            var supportingBacking = supportingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue Background");
            Assert.That(supportingCue.text, Is.EqualTo("+1"));
            Assert.That(supportingCue.characterSize, Is.EqualTo(StandardMinionView.DefenseCueCharacterSize));
            Assert.That(supportingCue.transform.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)));
            Assert.That(supportingBacking.transform.localScale.y,
                Is.EqualTo(StandardMinionView.DefenseCueBackgroundVerticalScale));
            Assert.That(supportingBacking.transform.localScale.y,
                Is.GreaterThan(supportingBacking.transform.localScale.x));
            var supportingCenterDelta = supportingView.transform.InverseTransformVector(
                supportingCue.GetComponent<MeshRenderer>().bounds.center -
                supportingBacking.GetComponent<MeshRenderer>().bounds.center);
            Assert.That(supportingCenterDelta.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(supportingCenterDelta.y,
                Is.EqualTo(StandardMinionView.DefenseCueOpticalVerticalOffset).Within(0.0001f));
            Assert.That(hinderingView.DefenseCueAmount, Is.EqualTo(-1));
            var hinderingCue = hinderingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue");
            var hinderingBacking = hinderingView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Defense Contribution Cue Background");
            Assert.That(hinderingCue.text, Is.EqualTo("-1"));
            Assert.That(hinderingCue.characterSize, Is.EqualTo(StandardMinionView.DefenseCueCharacterSize));
            Assert.That(hinderingCue.transform.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)));
            Assert.That(hinderingBacking.transform.localScale.y,
                Is.EqualTo(StandardMinionView.DefenseCueBackgroundVerticalScale));
            Assert.That(hinderingBacking.transform.localScale.y,
                Is.GreaterThan(hinderingBacking.transform.localScale.x));
            var hinderingCenterDelta = hinderingView.transform.InverseTransformVector(
                hinderingCue.GetComponent<MeshRenderer>().bounds.center -
                hinderingBacking.GetComponent<MeshRenderer>().bounds.center);
            Assert.That(hinderingCenterDelta.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(hinderingCenterDelta.y,
                Is.EqualTo(StandardMinionView.DefenseCueOpticalVerticalOffset).Within(0.0001f));

            var resolutionId = new ActionResolutionId("configured-preview-resolution");
            var choiceId = new ActionResolutionStepId("configured-preview-choice");
            var cardBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                Hand(runtime, TeamSide.Blue).Cards[0].Id,
                "TEST DEFENSE CARD DEFENSE",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  CARD: TEST DEFENSE CARD\n  +3 — Test Defense Card\n" +
                "DEFENSE TOTAL: 3\nRESULT: DEFENDED",
                AttackResolutionOutcome.SuccessfullyDefended);
            var declineBreakdown = new DefenseBreakdownViewModel(
                12,
                resolutionId,
                choiceId,
                null,
                "DECLINE PREVIEW",
                "ATTACK:\n  +3 — Test Attack Card\nATTACK TOTAL: 3\n\n" +
                "DEFENSE:\n  NO DEFENSE TOTAL\nRESULT: NO DEFENSE TOTAL - ATTACK SUCCEEDS",
                AttackResolutionOutcome.AttackSuccessful);
            var currentRound = runtime.RoundView.Model;
            var cardInvocation = new TypedInvocationViewModel(
                "DISCARD TEST DEFENSE CARD\nPRINTED DEFENSE 3",
                DirectorCommand.ChooseDefenseCard(Hand(runtime, TeamSide.Blue).Cards[0].Id),
                cardBreakdown);
            var declineInvocation = new TypedInvocationViewModel(
                "DECLINE DEFENSE",
                DirectorCommand.DeclineDefense(),
                declineBreakdown);
            runtime.RoundView.Render(new RoundActionViewModel(
                12,
                currentRound.RoundIndex,
                currentRound.TurnIndex,
                currentRound.Phase,
                "Defense response pending against Attack 3.",
                currentRound.Actions,
                currentRound.EndTurnInvocation,
                currentRound.ContinueEndOfRoundInvocation,
                currentRound.InitiativeChoices,
                new[] { cardInvocation, declineInvocation },
                currentRound.FastTravelOffers));

            runtime.RoundView.ShowDefensePreview(cardBreakdown, new Rect(120f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text, Does.Contain("DEFENSE TOTAL: 3"));
            runtime.RoundView.ShowDefensePreview(declineBreakdown, new Rect(320f, 220f, 190f, 46f));
            yield return null;
            Assert.That(runtime.RoundView.ActiveDefensePreview.Text,
                Does.Contain("NO DEFENSE TOTAL - ATTACK SUCCEEDS"));
            Assert.That(supportingView.DefenseCueAmount, Is.EqualTo(1),
                "Moving between Defense choices must not rebuild attack-scoped cues.");
            Assert.That(hinderingView.DefenseCueAmount, Is.EqualTo(-1));

            if (!UnityEngine.Application.isBatchMode)
            {
                var screenshotPath = Path.GetFullPath("tmp/milestone-6-1b-defense-preview.png");
                if (File.Exists(screenshotPath))
                {
                    File.Delete(screenshotPath);
                }
                ScreenCapture.CaptureScreenshot(screenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(screenshotPath), Is.True,
                    "The targeted Game View visual evidence must be captured under tmp/.");
            }
        }

        /// <summary>
        /// Milestone 6.1a. The configured setup is symmetric, so no round of it ever produces a
        /// Minion Battle removal choice; the prompt and its board cues are proven here the same way
        /// the Defense preview is - by rendering an authority-shaped model into the scene's own
        /// views and checking what the configured scene does with it, including that a passive view
        /// forwards controls without reaching authoritative state itself.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_MinionRemovalPromptAndBoardCuesUseConfiguredPassiveViews()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            Assert.That(runtime.PendingMinionBattleChoice, Is.Null);
            Assert.That(runtime.MinionChoiceView.Model, Is.Null,
                "With no pending choice the configured prompt exposes no controls.");

            // Non-Heavy candidates only. The authority withholds a control from a Heavy while any
            // non-Heavy is unselected, so a hand-built model offering one would render a prompt the
            // real projection can never produce.
            var blue = runtime.BoardView.Model.Minions
                .Where(value => value.TeamSide == TeamSide.Blue &&
                    value.MinionType != StandardMinionType.Heavy)
                .OrderBy(value => value.MinionId.Value, System.StringComparer.Ordinal)
                .Take(2)
                .ToArray();
            var blueTeamId = runtime.Setup.Teams.Single(value => value.TeamSide == TeamSide.Blue).TeamId;
            var resolutionId = new EndOfRoundResolutionId("configured-minion-battle");
            var choiceId = new EndOfRoundChoiceId("configured-minion-battle-removals");
            var baseBoard = runtime.BoardView.Model;
            var acceptedBefore = runtime.AcceptedCommands.Count;
            var snapshotBefore = runtime.CaptureSnapshot();

            // Nothing selected yet: both candidates carry a removal control and there is nothing to
            // confirm. The board marks only the candidate the pointer is on.
            var unselected = blue.Select((minion, index) => new MinionChoiceCandidateViewModel(
                    minion.MinionId,
                    $"REMOVE {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, minion.MinionType, index + 1)}",
                    index + 1,
                    false,
                    DirectorCommand.SelectMinionBattleRemoval(new MinionBattleChoiceCommand(
                        MinionBattleChoiceCommandKind.Select, resolutionId, choiceId,
                        TeamSide.Blue, minion.MinionId))))
                .ToArray();
            runtime.MinionChoiceView.Render(new MinionChoiceViewModel(
                baseBoard.Version,
                resolutionId,
                choiceId,
                blueTeamId,
                TeamSide.Blue,
                1,
                0,
                unselected,
                null));
            RenderChoiceCues(runtime, baseBoard, baseBoard.Version + 1, new[]
            {
                new MinionChoiceCueViewModel(blue[1].MinionId, MinionChoiceCueKind.Highlighted)
            });
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.PromptLabel,
                Is.EqualTo("MINION BATTLE - [B] BLUE REMOVES 1 MINION"));
            Assert.That(runtime.MinionChoiceView.Model.StatusLabel, Is.EqualTo("SELECTED 0 OF 1"));
            Assert.That(runtime.MinionChoiceView.Model.Candidates, Has.Count.EqualTo(2));
            Assert.That(runtime.MinionChoiceView.Model.ConfirmInvocation, Is.Null,
                "Below the required count the configured prompt offers no confirmation control.");

            var firstView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(blue[0].MinionId));
            var secondView = runtime.BoardView.MinionViews.Single(value =>
                value.BoundMinionId.Equals(blue[1].MinionId));
            Assert.That(firstView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
            Assert.That(secondView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted));
            Assert.That(secondView.GetComponentsInChildren<TextMesh>(true)
                    .Single(value => value.gameObject.name == "Minion Choice Cue").text,
                Is.EqualTo(StandardMinionView.ChoiceCueMarker),
                "The cue above the minion is the one marker every visible kind uses.");

            // One selected at the required count: the chosen minion keeps its board cue, its control
            // becomes the projected deselect, and the confirmation appears.
            var selectedCandidate = new MinionChoiceCandidateViewModel(
                blue[0].MinionId,
                $"KEEP {MinionChoiceLabels.CandidateLabel(TeamSide.Blue, blue[0].MinionType, 1)}",
                1,
                true,
                DirectorCommand.DeselectMinionBattleRemoval(new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Deselect, resolutionId, choiceId,
                    TeamSide.Blue, blue[0].MinionId)));
            var confirm = new TypedInvocationViewModel(
                "CONFIRM REMOVAL",
                DirectorCommand.ConfirmMinionBattleRemovals(new MinionBattleChoiceCommand(
                    MinionBattleChoiceCommandKind.Confirm, resolutionId, choiceId, TeamSide.Blue)));
            runtime.MinionChoiceView.Render(new MinionChoiceViewModel(
                baseBoard.Version,
                resolutionId,
                choiceId,
                blueTeamId,
                TeamSide.Blue,
                1,
                1,
                new[] { selectedCandidate },
                confirm));
            RenderChoiceCues(runtime, baseBoard, baseBoard.Version + 2, new[]
            {
                new MinionChoiceCueViewModel(blue[0].MinionId, MinionChoiceCueKind.Selected),
                new MinionChoiceCueViewModel(blue[1].MinionId, MinionChoiceCueKind.Highlighted)
            });
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model.StatusLabel, Is.EqualTo("SELECTED 1 OF 1"));
            Assert.That(runtime.MinionChoiceView.Model.Candidates.Single().IsSelected, Is.True);
            Assert.That(runtime.MinionChoiceView.Model.ConfirmInvocation.Command.Kind,
                Is.EqualTo(DirectorCommandKind.ConfirmMinionBattleRemovals));
            Assert.That(firstView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Selected));
            Assert.That(secondView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.Highlighted),
                "A selected minion keeps its cue while the player points at another.");

            var selectedCue = firstView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Minion Choice Cue");
            var selectedBacking = firstView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Minion Choice Cue Background");
            var highlightedCue = secondView.GetComponentsInChildren<TextMesh>(true).Single(value =>
                value.gameObject.name == "Minion Choice Cue");
            Assert.That(selectedCue.text, Is.EqualTo(StandardMinionView.ChoiceCueMarker));
            Assert.That(highlightedCue.text, Is.EqualTo(selectedCue.text),
                "Both kinds mean the same thing about the minion, so both draw the same marker.");
            Assert.That(highlightedCue.color, Is.Not.EqualTo(selectedCue.color),
                "Selection and the pointed-at control stay apart by color.");
            Assert.That(selectedCue.characterSize, Is.EqualTo(StandardMinionView.ChoiceCueCharacterSize));
            Assert.That(selectedCue.transform.localScale, Is.EqualTo(new Vector3(-1f, 1f, 1f)),
                "The cue reuses the token-orientation correction of the Defense contribution cue.");
            Assert.That(selectedBacking.transform.localScale.y,
                Is.GreaterThan(selectedBacking.transform.localScale.x));
            var camera = Object.FindAnyObjectByType<Camera>();
            Assert.That(camera.WorldToViewportPoint(selectedCue.transform.position).y,
                Is.GreaterThan(camera.WorldToViewportPoint(firstView.transform.position).y),
                "The cue reads above its minion under the configured top-down camera.");

            if (!UnityEngine.Application.isBatchMode)
            {
                var promptScreenshotPath = Path.GetFullPath("tmp/milestone-6-1a-minion-removal-prompt.png");
                if (File.Exists(promptScreenshotPath))
                {
                    File.Delete(promptScreenshotPath);
                }
                ScreenCapture.CaptureScreenshot(promptScreenshotPath);
                yield return new WaitForEndOfFrame();
                yield return null;
                Assert.That(File.Exists(promptScreenshotPath), Is.True,
                    "The targeted Game View visual evidence must be captured under tmp/.");
            }

            MinionChoiceCandidateViewModel submitted = null;
            var confirmed = 0;
            var highlights = new List<MinionChoiceCandidateViewModel>();
            runtime.MinionChoiceView.CandidateSubmitted += value => submitted = value;
            runtime.MinionChoiceView.ConfirmRequested += () => confirmed++;
            runtime.MinionChoiceView.CandidateHighlighted += value => highlights.Add(value);

            runtime.MinionChoiceView.ForwardCandidate(selectedCandidate);
            Assert.That(submitted, Is.SameAs(selectedCandidate));
            runtime.MinionChoiceView.ForwardConfirm();
            Assert.That(confirmed, Is.EqualTo(1));

            // Milestone 6.2d: the prompt reports the whole candidate rather than its unit alone, so a
            // control that shares its unit with every other one is still distinguishable. The unit is
            // still readable off it, which is what the board's minion cue goes on reading.
            runtime.MinionChoiceView.ForwardHighlight(null);
            runtime.MinionChoiceView.ForwardHighlight(selectedCandidate);
            Assert.That(highlights.Last(), Is.SameAs(selectedCandidate));
            Assert.That(runtime.MinionChoiceView.HighlightedCandidate, Is.SameAs(selectedCandidate));
            Assert.That(runtime.MinionChoiceView.HighlightedUnit,
                Is.EqualTo(RuleObject.ForMinion(blue[0].MinionId)));
            Assert.That(runtime.AcceptedCommands, Has.Count.EqualTo(acceptedBefore),
                "The passive prompt reports intent; it never advances authoritative state itself.");
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(snapshotBefore));

            // Back to the ordinary no-choice state the configured scene actually sits in.
            runtime.MinionChoicePresenter.Refresh();
            RenderChoiceCues(runtime, baseBoard, baseBoard.Version + 3,
                System.Array.Empty<MinionChoiceCueViewModel>());
            yield return null;

            Assert.That(runtime.MinionChoiceView.Model, Is.Null);
            Assert.That(firstView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
            Assert.That(secondView.ChoiceCueKind, Is.EqualTo(MinionChoiceCueKind.None));
            Assert.That(selectedCue.gameObject.activeSelf, Is.False);
        }

        private static void RenderChoiceCues(
            ConfiguredMatchSceneRuntime runtime,
            BoardInteractionViewModel baseBoard,
            long version,
            IEnumerable<MinionChoiceCueViewModel> cues)
        {
            runtime.BoardView.Render(new BoardInteractionViewModel(
                version,
                baseBoard.Heroes,
                baseBoard.Minions,
                baseBoard.MovementDestinations,
                baseBoard.AttackTargets,
                System.Array.Empty<MinionId>(),
                baseBoard.FocusedHeroId,
                fastTravelDestinations: baseBoard.FastTravelDestinations,
                minionDefenseCues: baseBoard.MinionDefenseCues,
                minionChoiceCues: cues));
        }

        [UnityTest]
        public IEnumerator Scene_UndoRestartRestoreAndDiagnosticsStayEquivalent()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var initial = runtime.CaptureSnapshot();
            Assert.That(runtime.DiagnosticEvents.Single().EventType,
                Is.EqualTo("Setup.MatchInitialized"));
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[1];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            var afterRed = runtime.CaptureSnapshot();
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            var revealed = runtime.CaptureSnapshot();
            var revealedEvents = runtime.DiagnosticEvents.Select(value => value.DisplayText).ToArray();

            runtime.DiagnosticPresenter.Toggle();
            Assert.That(runtime.DiagnosticView.Model.IsExpanded, Is.True);
            Assert.That(runtime.Undo(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(afterRed));
            Assert.That(runtime.Restart(), Is.True);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(initial));
            Assert.That(runtime.RestoreSnapshot(revealed), Is.True, runtime.LastOperationDetail);
            Assert.That(runtime.CaptureSnapshot(), Is.EqualTo(revealed));
            Assert.That(runtime.DiagnosticEvents.Select(value => value.DisplayText),
                Is.EqualTo(revealedEvents));
            Assert.That(runtime.AcceptedCommands, Is.Empty);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Scene_PlaysTwoCompleteRoundsThroughConfiguredFeatureBoundary()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;

            for (var round = 1; round <= 2; round++)
            {
                for (var turn = 1; turn <= 4; turn++)
                {
                    CommitNextTurn(runtime);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                    Assert.That(runtime.SubmitEndTurn(), Is.True);
                }

                Assert.That(runtime.Phase, Is.EqualTo(RoundResolutionPhase.EndOfRound));
                while (runtime.Phase == RoundResolutionPhase.EndOfRound)
                {
                    // Milestone 7.6. p.8's Pity Coin leaves every hero able to afford a level, so from
                    // the second round on this loop meets p.15's Upgrade a Card choice. No presenter
                    // draws a paused system transition yet - the same gap milestone 6.11's Lane-Push
                    // respawn has - so the scene answers it through the runtime's director command
                    // surface, naming the option rather than letting presentation pick one.
                    var options = runtime.PausedSystemTransitionOptions;
                    if (options.Count > 0)
                    {
                        Assert.That(runtime.AnswerPausedSystemTransition(options[0]), Is.True);
                        continue;
                    }

                    Assert.That(runtime.ContinueEndOfRound(), Is.True);
                }

                Assert.That(runtime.RoundIndex, Is.EqualTo(round + 1));
            }

            Assert.That(runtime.CompletedRounds.Select(value => value.RoundIndex),
                Is.EqualTo(new[] { 1, 2 }));
        }

        /// <summary>
        /// Milestone 6.5c added the second board highlight, so the composition root now generates two
        /// materials. What this fixture guards is unchanged and is the point: both are owned by the
        /// root, both are released on reload, and neither accumulates a copy per scene load.
        /// </summary>
        [UnityTest]
        public IEnumerator Scene_HidesAnAuthoritativelyAbsentHeroAndShowsItAgainWhenReturned()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var baseBoard = runtime.BoardView.Model;
            var heroViews = Object.FindObjectsByType<PrimitiveHeroView>();
            var blueHeroId = runtime.Setup.Heroes.Single(value => value.TeamSide == TeamSide.Blue).HeroId;
            var blueView = heroViews.Single(value => value.Occupancy.HeroId.Equals(blueHeroId));
            var remainingHeroes = baseBoard.Heroes
                .Where(value => !value.HeroId.Equals(blueHeroId))
                .ToArray();

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                remainingHeroes,
                baseBoard.Minions,
                System.Array.Empty<HexCoordinate>(),
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                default));

            Assert.That(blueView.gameObject.activeSelf, Is.False,
                "A Minion-Battle-removed hero must disappear when its occupancy leaves projection.");

            var returnedCoordinate = Object.FindObjectsByType<HexTileView>()
                .First(tile => tile.IsPlayable && remainingHeroes.All(hero =>
                    !hero.Coordinate.Equals(tile.Coordinate)))
                .Coordinate;
            var returnedHeroes = remainingHeroes.Concat(new[]
            {
                new HeroOccupancy(blueHeroId, returnedCoordinate)
            });
            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 2,
                returnedHeroes,
                baseBoard.Minions,
                System.Array.Empty<HexCoordinate>(),
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                default));

            Assert.That(blueView.gameObject.activeSelf, Is.True);
            Assert.That(blueView.Occupancy.Coordinate, Is.EqualTo(returnedCoordinate),
                "Respawn, Undo, or restore reuses and repositions the configured hero view.");
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneReload_ReleasesAndReusesExactlyOneOwnedHighlightMaterialOfEachKind()
        {
            yield return LoadScene();
            var first = Root();
            Assert.That(first.OwnsGeneratedHighlightMaterial, Is.True);
            Assert.That(first.OwnedHighlightNativeResourceCount, Is.EqualTo(2));
            Assert.That(CountHighlightMaterials(), Is.EqualTo(1));
            Assert.That(CountResultingSpaceMaterials(), Is.EqualTo(1));
            AssertCurrentMinionBindings(first);

            yield return LoadScene();
            yield return null;
            var second = Root();
            Assert.That(second.OwnedHighlightNativeResourceCount, Is.EqualTo(2));
            Assert.That(CountHighlightMaterials(), Is.EqualTo(1));
            Assert.That(CountResultingSpaceMaterials(), Is.EqualTo(1));
            AssertCurrentMinionBindings(second);
        }

        [UnityTest]
        public IEnumerator Scene_RespawnButtonHoverUsesTheMovementDestinationHighlight()
        {
            yield return LoadScene();
            var runtime = Root().Runtime;
            var baseBoard = runtime.BoardView.Model;
            var tile = Object.FindObjectsByType<HexTileView>()
                .First(candidate => candidate.IsPlayable);
            var destination = tile.Coordinate;

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 1,
                baseBoard.Heroes,
                baseBoard.Minions,
                new[] { destination },
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                baseBoard.FocusedHeroId));
            Assert.That(tile.IsHighlighted, Is.True);
            var movementHighlightMaterial = tile.GetComponent<MeshRenderer>().sharedMaterial;

            runtime.BoardView.Render(new BoardInteractionViewModel(
                baseBoard.Version + 2,
                baseBoard.Heroes,
                baseBoard.Minions,
                System.Array.Empty<HexCoordinate>(),
                baseBoard.AttackTargets,
                baseBoard.MinionAttackTargets,
                baseBoard.FocusedHeroId,
                heroRespawnHighlight: destination));
            Assert.That(tile.IsHighlighted, Is.True);
            Assert.That(tile.GetComponent<MeshRenderer>().sharedMaterial,
                Is.SameAs(movementHighlightMaterial),
                "Respawn hover uses the exact movement destination highlight, not a second visual language.");

            var hero = runtime.Setup.Heroes.Single(value => value.TeamSide == TeamSide.Blue).HeroId;
            var card = runtime.Cards.First(value => value.OwnerId.Equals(hero) &&
                value.State.Location == CardLocation.Hand);
            var option = new HeroRespawnOptionViewModel(
                "RESPAWN AT SPAWN #1",
                destination,
                DirectorCommand.RespawnHero(new HeroRespawnCommand(
                    HeroRespawnCommandKind.Respawn, hero, card.Id, destination)));
            HeroRespawnOptionViewModel highlighted = null;
            runtime.RoundView.RespawnHighlighted += value => highlighted = value;
            runtime.RoundView.Render(new RoundActionViewModel(
                runtime.RoundView.Model.Version + 1,
                runtime.RoundIndex,
                runtime.RoundView.Model.TurnIndex,
                RoundResolutionPhase.ResolvingTurn,
                "Choose respawn",
                System.Array.Empty<TypedInvocationViewModel>(),
                null,
                null,
                System.Array.Empty<TypedInvocationViewModel>(),
                System.Array.Empty<TypedInvocationViewModel>(),
                heroRespawnOptions: new[] { option }));
            runtime.RoundView.ForwardRespawnHighlight(option);
            Assert.That(highlighted, Is.SameAs(option));
            Assert.That(highlighted.Destination, Is.EqualTo(destination));
            yield return null;
        }


        /// <summary>
        /// Milestone 8.4. The scene deals Wasp's verified cards, whose printed primaries are all
        /// refused until their 8.6-8.13 slices compose their text, so the two cases below - which need
        /// a Skill that resolves and an Attack that reaches a minion - ask for the mechanics-showcase
        /// hands instead. The request is consumed and reset by the composition it applies to, so every
        /// other case in this fixture still loads the configured match.
        /// </summary>
        private static IEnumerator LoadShowcaseScene()
        {
            MatchSceneCompositionRoot.NextComposedCardContent = SceneCardContent.MechanicsShowcaseFixture;
            return LoadScene();
        }
        private static IEnumerator LoadScene()
        {
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
        }

        private static MatchSceneCompositionRoot Root()
        {
            var root = Object.FindAnyObjectByType<MatchSceneCompositionRoot>();
            Assert.That(root, Is.Not.Null);
            Assert.That(root.Runtime, Is.Not.Null);
            return root;
        }

        private static DirectorHandViewModel Hand(ConfiguredMatchSceneRuntime runtime, TeamSide side) =>
            runtime.HandsView.Model.Hands.Single(value => value.TeamSide == side);

        private static void CommitNextTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = Hand(runtime, TeamSide.Red).Cards[0];
            var blue = Hand(runtime, TeamSide.Blue).Cards[0];
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.IsTurnActive, Is.True);
        }

        /// <summary>
        /// Commits the Red hand's highest-Initiative card that still prints an Attack, against the Blue
        /// hand's lowest, so the acting card offers an Attack. The showcase conversions took the printed
        /// Attack off both hands' first card - Red Bulwark at 6.8a, Blue Feint at 6.8b, Red Needle at
        /// 6.5a - so the ordinary <see cref="CommitNextTurn"/> pair no longer offers one.
        /// </summary>
        private static void CommitNextAttackTurn(ConfiguredMatchSceneRuntime runtime)
        {
            var red = Hand(runtime, TeamSide.Red).Cards
                .Where(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .ActionDefinitions.Any(action =>
                        action.Kind == GuardsDigital.Core.Cards.ActionKind.Attack))
                .OrderByDescending(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            var blue = Hand(runtime, TeamSide.Blue).Cards
                .OrderBy(card => runtime.Definitions
                    .Single(definition => definition.Reference.Equals(card.DefinitionReference))
                    .Initiative.Value)
                .First();
            Assert.That(runtime.SelectCommitmentCard(red.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            Assert.That(runtime.SelectCommitmentCard(blue.Id), Is.True);
            Assert.That(runtime.ConfirmSelectedCommitment(), Is.True);
            ResolveInitiativeChoice(runtime);
            Assert.That(runtime.IsTurnActive, Is.True);
            Assert.That(runtime.SelectedActor.CardInstanceId, Is.EqualTo(red.Id));
        }

        private static void ResolveInitiativeChoice(ConfiguredMatchSceneRuntime runtime)
        {
            if (runtime.PendingInitiativeChoice != null)
            {
                Assert.That(runtime.SubmitInitiativeChoice(
                    runtime.PendingInitiativeChoice.CandidateCardInstanceIds[0]), Is.True);
            }
        }

        private static GuardsDigital.Core.Cards.ActionKind ActionKind(
            ConfiguredMatchSceneRuntime runtime,
            ActionDefinitionId actionId)
        {
            var offer = runtime.AvailableTurnActions.Single(value =>
                value.ActionDefinitionId.Equals(actionId));
            return runtime.Definitions.Single(value =>
                    value.Reference.Equals(offer.SourceDefinitionReference))
                .ActionDefinitions.Single(value => value.Id.Equals(actionId)).Kind;
        }

        private static int HexDistance(HexCoordinate first, HexCoordinate second)
        {
            var deltaQ = first.Q - second.Q;
            var deltaR = first.R - second.R;
            return (System.Math.Abs(deltaQ) + System.Math.Abs(deltaR) +
                System.Math.Abs(deltaQ + deltaR)) / 2;
        }

        private static Ray DownRay(Vector3 position) =>
            new Ray(position + (Vector3.up * 2f), Vector3.down);

        private static int CountHighlightMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null && value.name == DestinationHighlightMaterialOwner.GeneratedMaterialName);

        private static int CountResultingSpaceMaterials() =>
            Resources.FindObjectsOfTypeAll<Material>().Count(value =>
                value != null &&
                value.name == DestinationHighlightMaterialOwner.GeneratedResultingSpaceMaterialName);

        private static void AssertCurrentMinionBindings(MatchSceneCompositionRoot root)
        {
            var views = Object.FindObjectsByType<StandardMinionView>();
            var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
            Assert.That(views, Has.Length.EqualTo(12));
            Assert.That(root.Runtime.BoardView.MinionViews, Is.EquivalentTo(views));
            Assert.That(pointerInput.MinionSelectors.Count, Is.EqualTo(12));
            Assert.That(pointerInput.MinionSelectors.Select(value =>
                value.GetComponent<StandardMinionView>()), Is.EquivalentTo(views));
        }
    }
}
