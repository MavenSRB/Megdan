using System.Collections;
using System.Linq;
using GuardsDigital.Core.Board;
using GuardsDigital.Core.Cards;
using GuardsDigital.Core.Cards.Verified;
using GuardsDigital.Core.Components;
using GuardsDigital.Core.Participants;
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
using static GuardsDigital.Tests.PlayMode.SabinaEngagedUiJourneyPlayModeTests;
using static GuardsDigital.Tests.PlayMode.SabinaUiJourneyPlayModeTests;
using Object = UnityEngine.Object;

namespace GuardsDigital.Tests.PlayMode
{
    /// <summary>
    /// Milestone 8.62. The permanent photograph of what a player sees when a piece is a legal target.
    ///
    /// <para><b>Why this fixture exists.</b> On 2026-09-19 the user looked at an attack's targets in
    /// the packaged build and said: "can you make targeting highlights more visible, they are almost
    /// impossible to see like this.. Blood red light instead of this pink i think." No assertion over a
    /// colour constant could have caught that - the constants were the reviewed blood red already, and
    /// what reached the screen was a near-white filament over a dim skirt - so this fixture drives
    /// three REAL targets onto the board and writes the frame to <c>tmp/862-captures/</c> for a person
    /// to look at, at both of the viewports the user plays on.</para>
    ///
    /// <para><b>The three subjects</b> are the three ways a piece is named: a HERO inside an attack's
    /// printed range (Sabina's Point Blank Shot at the enemy she walked up to), a MINION as the only
    /// legal target of an adjacent attack (the engaged mirror's Shock), and a HERO as a card-text
    /// EFFECT target (Shock's own rider, which stops the attack and marks the one enemy hero in its
    /// printed radius). The hovered frame is taken too, because hover is the one state that changes
    /// the mark's strength.</para>
    ///
    /// <para><b>Naming.</b> <c>GUARDS_CAPTURE_PHASE</c> names the run in the file name, so the same
    /// fixture takes the before and the after photographs of a tuning change without being edited
    /// between them; with nothing set it writes <c>current</c>.</para>
    /// </summary>
    public sealed class TargetingCuePhotographPlayModeTests
    {
        private const string CaptureRoot = "tmp/862-captures";

        private static string Phase =>
            string.IsNullOrWhiteSpace(
                System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE"))
                ? "current"
                : System.Environment.GetEnvironmentVariable("GUARDS_CAPTURE_PHASE").Trim();

        /// <summary>
        /// Milestone 8.62. The capture phase can also choose the TREATMENT, so one run photographs
        /// one treatment end to end and states its own claims about what it drew. Every run draws
        /// the shipped hex outline - the user, from the photographs of both: "I like this. Keep it."
        /// - and a run named <c>ring</c> draws the retired fitted ring instead, which is how the
        /// switched-off treatment stays photographable rather than becoming a branch nobody runs.
        /// The setting is restored whatever the test does, so no later fixture in the same batch
        /// inherits a reviewer's choice.
        /// </summary>
        private BoardTargetCueTreatment restoreTreatment;

        [SetUp]
        public void ChooseTheTreatmentThisRunPhotographs()
        {
            restoreTreatment = BoardTargetCuePresentation.Treatment;
            BoardTargetCuePresentation.Treatment = Phase == "ring"
                ? BoardTargetCueTreatment.RingOnThePiece
                : BoardTargetCuePresentation.ShippedTreatment;
        }

        [TearDown]
        public void RestoreTheShippedTreatment() =>
            BoardTargetCuePresentation.Treatment = restoreTreatment;

        private static (uint Width, uint Height)[] Viewports() =>
            new[] { (1920u, 1080u), (2560u, 1440u) };

        /// <summary>
        /// A hero standing inside an attack's printed range, photographed as a target and then as a
        /// hovered target. Point Blank Shot prints Range 1, so the walk that puts Sabina beside the
        /// enemy hero is what makes the enemy piece a legal target at all.
        /// </summary>
        [UnityTest]
        public IEnumerator AHeroWhoIsAnAttackTarget_IsPhotographedAtBothViewports()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
                var root = Root();
                var runtime = root.Runtime;
                var sabinaId = SabinaSeat(runtime).HeroId;
                var enemyId = OtherHeroId(runtime, sabinaId);

                yield return WalkIntoContact(root, sabinaId, enemyId);
                CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.PointBlankShot);
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId));
                yield return PointTrayAt(root, sabinaId);
                yield return StartAttackThroughItsControl(root);

                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null,
                    "Arrange: the clicked Attack control opens p.14's target question.");
                Assert.That(runtime.PendingAttackTargetChoice.LegalTargetHeroIds, Does.Contain(enemyId),
                    "Arrange: the enemy hero beside her is inside the printed Range 1. " +
                    Remaining(runtime));

                var targetView = HeroView(enemyId);
                var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                foreach (var viewport in Viewports())
                {
                    yield return AtViewport(viewport);
                    yield return DismissTheTransientBanner(root);
                    yield return UiPlayer.CaptureScreen(Frame("hero-attack-target", viewport));

                    // The one state that strengthens a mark: the pointer standing on the target's own
                    // space. It is driven through the pointer input's own hover entry point because a
                    // batch run has no hardware pointer to move, and it is refreshed in the same frame
                    // the photograph is taken in - `Update` reads the real pointer again next frame.
                    pointerInput.RefreshHoverFromPointerRay(
                        RayAt(pointerInput, targetView.transform.position));
                    // A batch run has no hardware pointer, and a ray that lands on a STANDEE marks
                    // the piece rather than the space, so the board's own pointer-strength entry
                    // point is called too - otherwise the "hovered" frame is the resting frame,
                    // which is exactly what the first hovered captures turned out to be. A frame is
                    // then let through, because the hover reaches the board on the NEXT render.
                    yield return null;
                    // ...and the board pointer-strength entry point is set LAST, with no frame between
                    // it and the shutter: the pointer input clears the hovered space on its own Update
                    // when no hardware pointer is over a tile, which is why the first hovered captures
                    // came out identical to the resting ones.
                    runtime.BoardView.SetHoveredCoordinate(targetView.Occupancy.Coordinate);
                    yield return UiPlayer.CaptureScreen(
                        Frame("hero-attack-target-hovered", viewport));
                    AssertThePointedAtTileIsEdgedWhole(targetView.Occupancy.Coordinate);
                    runtime.BoardView.SetHoveredCoordinate(null);
                    yield return null;
                }

                yield return AtViewport((1920u, 1080u));

                // What the frames above are showing, stated: the projection names the piece, the
                // PIECE wears the blood-red ring and wash (8.62, at the user's direction), and the
                // space it stands on keeps the blood-red corner language it already had.
                var cue = runtime.BoardView.Model.PieceTargetCues.Single(value =>
                    value.Target.Equals(BoardObjectReference.ForHero(enemyId)));
                Assert.That(cue.State, Is.EqualTo(BoardTargetCueState.Eligible));
                Assert.That(cue.ConsequenceIntent, Is.EqualTo(BoardTargetConsequenceIntent.Attack));
                Assert.That(targetView.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)),
                    "The targeted hero wears the cue the projection named them with.");
                Assert.That(targetView.TargetRing.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece));
                Assert.That(targetView.TargetRing.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Attack)));
                // Milestone 8.62, the user's settled word: "just keep the previous shape, and use the
                // new color for the outline. Don't color on the inside." The ring carries the cue and
                // the piece's own artwork is left alone. Under the hex-outline treatment there is no
                // ring geometry at all to ask - the piece is left entirely alone - so the claim is
                // stated where the ring exists.
                Assert.That(BoardTargetRing.EligibleWashAlpha, Is.Zero);
                if (BoardTargetCuePresentation.DrawsTheRingOnThePiece)
                {
                    Assert.That(targetView.TargetRing.VisibleWashColor.a,
                        Is.EqualTo(BoardTargetRing.EligibleWashAlpha).Within(0.0001f));
                    Assert.That(targetView.TargetRing.GeneratedMesh.GetTriangles(2), Is.Empty,
                        "Nothing is drawn over the portrait inside the ring.");
                }
                else
                {
                    Assert.That(targetView.TargetRing.IsVisible, Is.False,
                        "The hex-outline treatment draws nothing at all on the piece.");
                }
                Assert.That(TileUnder(targetView.Occupancy.Coordinate).CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Attack),
                    "A hero who can be attacked is marked in the board's blood-red corner language.");
                Assert.That(TileUnder(targetView.Occupancy.Coordinate).CornerHighlight.VisibleMarkCount,
                    Is.EqualTo(HexCornerHighlightTemplate.CornerCount));
                // The hex-outline variant. The user: "Let's try how it looks when we remove the
                // outline of the token, just outline the hex", and "Keep the colors, blood red on
                // enemy, green on ally". An enemy hero an Attack can take is BOTH, so the side rule
                // and the intent palette agree here and the tile is outlined blood red.
                AssertTheTileOutline(
                    targetView.Occupancy.Coordinate, BoardTargetOutlineSide.Enemy);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.62 restore");
            }
        }

        /// <summary>
        /// The engaged mirror's Shock: a MINION as the only legal target of the attack, then the one
        /// enemy HERO the card's own rider marks as an effect target. Both are photographed where the
        /// journey already proves them - <see cref="WaspEngagedUiJourneyPlayModeTests"/> - so the
        /// frames come from the same pause a player answers.
        /// </summary>
        [UnityTest]
        public IEnumerator AMinionTarget_AndAHeroEffectTarget_ArePhotographedAtBothViewports()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadEngagedMirror();
                var root = Root();
                var runtime = root.Runtime;
                var hud = root.Hud;
                var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();
                var blueHeroId = runtime.Setup.Heroes
                    .Single(hero => hero.TeamSide == TeamSide.Blue).HeroId;

                yield return CommitThroughTray(root, "SHOCK");
                yield return CommitThroughTray(root, "STOP PROJECTILES");
                yield return StartAttackThroughItsControl(root);

                Assert.That(runtime.PendingAttackTargetChoice, Is.Not.Null);
                var targetId = runtime.PendingAttackTargetChoice.LegalTargetMinionIds.Single();
                var minionView = Object.FindObjectsByType<StandardMinionView>()
                    .Single(view => view.Occupancy != null &&
                        view.Occupancy.MinionId.Equals(targetId));

                foreach (var viewport in Viewports())
                {
                    yield return AtViewport(viewport);
                    yield return DismissTheTransientBanner(root);
                    yield return UiPlayer.CaptureScreen(Frame("minion-attack-target", viewport));
                    pointerInput.RefreshHoverFromPointerRay(
                        RayAt(pointerInput, minionView.transform.position));
                    // A batch run has no hardware pointer, and a ray that lands on a STANDEE marks
                    // the piece rather than the space, so the board's own pointer-strength entry
                    // point is called too - otherwise the "hovered" frame is the resting frame,
                    // which is exactly what the first hovered captures turned out to be. A frame is
                    // then let through, because the hover reaches the board on the NEXT render.
                    yield return null;
                    // ...and the board pointer-strength entry point is set LAST, with no frame between
                    // it and the shutter: the pointer input clears the hovered space on its own Update
                    // when no hardware pointer is over a tile, which is why the first hovered captures
                    // came out identical to the resting ones.
                    runtime.BoardView.SetHoveredCoordinate(minionView.Occupancy.Coordinate);
                    yield return UiPlayer.CaptureScreen(
                        Frame("minion-attack-target-hovered", viewport));
                    AssertThePointedAtTileIsEdgedWhole(minionView.Occupancy.Coordinate);
                    // Put the pointer down again, so the next viewport's resting frame is resting.
                    runtime.BoardView.SetHoveredCoordinate(null);
                    yield return null;
                }

                yield return AtViewport((1920u, 1080u));
                Assert.That(minionView.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)),
                    "The targeted minion wears the cue too - 8.62, at the user's direction.");
                Assert.That(minionView.TargetRing.IsVisible,
                    Is.EqualTo(BoardTargetCuePresentation.DrawsTheRingOnThePiece));
                Assert.That(TileUnder(minionView.Occupancy.Coordinate).CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Attack),
                    "A minion that can be attacked is marked in the same blood-red corner language.");
                AssertTheTileOutline(
                    minionView.Occupancy.Coordinate, BoardTargetOutlineSide.Enemy);

                Assert.That(
                    UiPlayer.ClickBoard(pointerInput, hud, minionView.transform.position), Is.True);
                yield return null;

                // p.14 step 7: Shock's rider stops the attack and names the one enemy hero in its
                // printed radius who is not adjacent - a card-text EFFECT target, which is the third
                // way a piece is cued and carries its own consequence colour.
                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "Arrange: Shock's rider stops the Attack on the actor's hero question.");
                var blueHeroView = Object.FindObjectsByType<PrimitiveHeroView>()
                    .Single(view => view.Occupancy.HeroId.Equals(blueHeroId));
                Assert.That(runtime.RoundView.Model.EffectChoiceTargets, Is.Not.Empty,
                    "Arrange: the hero candidate is a named board target.");

                foreach (var viewport in Viewports())
                {
                    yield return AtViewport(viewport);
                    yield return DismissTheTransientBanner(root);
                    yield return UiPlayer.CaptureScreen(Frame("hero-effect-target", viewport));
                    pointerInput.RefreshHoverFromPointerRay(
                        RayAt(pointerInput, blueHeroView.transform.position));
                    // A batch run has no hardware pointer, and a ray that lands on a STANDEE marks
                    // the piece rather than the space, so the board's own pointer-strength entry
                    // point is called too - otherwise the "hovered" frame is the resting frame,
                    // which is exactly what the first hovered captures turned out to be. A frame is
                    // then let through, because the hover reaches the board on the NEXT render.
                    yield return null;
                    // ...and the board pointer-strength entry point is set LAST, with no frame between
                    // it and the shutter: the pointer input clears the hovered space on its own Update
                    // when no hardware pointer is over a tile, which is why the first hovered captures
                    // came out identical to the resting ones.
                    runtime.BoardView.SetHoveredCoordinate(blueHeroView.Occupancy.Coordinate);
                    yield return UiPlayer.CaptureScreen(Frame("hero-effect-target-hovered", viewport));
                    AssertThePointedAtTileIsEdgedWhole(blueHeroView.Occupancy.Coordinate);
                    // Put the pointer down again, so the next viewport's resting frame is resting.
                    runtime.BoardView.SetHoveredCoordinate(null);
                    yield return null;
                }

                yield return AtViewport((1920u, 1080u));
                Assert.That(blueHeroView.TargetCueState,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(BoardTargetCueState.Eligible)));
                Assert.That(blueHeroView.TargetRing.ConsequenceIntent,
                    Is.EqualTo(BoardTargetCuePresentation.AsDrawn(
                        BoardTargetConsequenceIntent.Affect)),
                    "A card-text choice is not harmful by construction, so the piece wears the " +
                    "effect colour rather than blood red.");
                Assert.That(TileUnder(blueHeroView.Occupancy.Coordinate).CornerHighlight.Kind,
                    Is.EqualTo(HexCornerHighlightKind.Effect),
                    "A card-text target keeps its own neutral corner language, not the attack's.");
                // THE ONE CUE THE TWO RULES DISAGREE ABOUT. Shock's rider names an ENEMY hero, and
                // the shipped INTENT palette paints a card-text choice green. Under the user's side
                // rule - "blood red on enemy, green on ally" - this tile is outlined blood red.
                AssertTheTileOutline(
                    blueHeroView.Occupancy.Coordinate,
                    BoardTargetCuePresentation.ColourByTargetSide
                        ? BoardTargetOutlineSide.Enemy
                        : BoardTargetOutlineSide.Friendly);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.62 restore");
            }
        }

        /// <summary>
        /// Milestone 8.62's hex-outline variant, the GREEN half. The user: "Keep the colors, blood
        /// red on enemy, green on ally."
        ///
        /// <para>Back to Back's printed Skill asks which FRIENDLY minion Sabina swaps with, so the
        /// pieces it lights are on the choosing seat's own side - the one staged board that offers a
        /// friendly target without a walk. The journey that proves the swap itself is
        /// <see cref="SabinaEngagedUiJourneyPlayModeTests"/>; this photographs the same pause.</para>
        /// </summary>
        [UnityTest]
        public IEnumerator AFriendlyMinionTarget_IsPhotographedAtBothViewports()
        {
            var restoreWidth = (uint)Screen.width;
            var restoreHeight = (uint)Screen.height;
            try
            {
                yield return LoadStagedScene(SceneCardContent.VerifiedEngagedStagedSabina);
                var root = Root();
                var runtime = root.Runtime;
                var sabinaId = SabinaSeat(runtime).HeroId;

                CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.BackToBack);
                Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId));
                yield return PointTrayAt(root, sabinaId);
                yield return StartActionThroughControl(root, ActionKind.Skill);

                Assert.That(runtime.PendingEffectChoice, Is.Not.Null,
                    "Arrange: the printed Skill stops and asks which friendly minion she swaps with.");
                var candidates = runtime.RoundView.Model.EffectChoiceTargets;
                Assert.That(candidates, Is.Not.Empty);
                var partner = MinionsAt(candidates.Select(candidate => candidate.Space))[0];
                var pointerInput = Object.FindAnyObjectByType<PrimitiveHeroPointerInput>();

                foreach (var viewport in Viewports())
                {
                    yield return AtViewport(viewport);
                    yield return DismissTheTransientBanner(root);
                    yield return UiPlayer.CaptureScreen(Frame("friendly-swap-target", viewport));
                    pointerInput.RefreshHoverFromPointerRay(
                        RayAt(pointerInput, partner.transform.position));
                    // A batch run has no hardware pointer, and a ray that lands on a STANDEE marks
                    // the piece rather than the space, so the board's own pointer-strength entry
                    // point is called too - otherwise the "hovered" frame is the resting frame,
                    // which is exactly what the first hovered captures turned out to be. A frame is
                    // then let through, because the hover reaches the board on the NEXT render.
                    yield return null;
                    // ...and the board pointer-strength entry point is set LAST, with no frame between
                    // it and the shutter: the pointer input clears the hovered space on its own Update
                    // when no hardware pointer is over a tile, which is why the first hovered captures
                    // came out identical to the resting ones.
                    runtime.BoardView.SetHoveredCoordinate(partner.Occupancy.Coordinate);
                    yield return UiPlayer.CaptureScreen(
                        Frame("friendly-swap-target-hovered", viewport));
                    AssertThePointedAtTileIsEdgedWhole(partner.Occupancy.Coordinate);
                    // Put the pointer down again, so the next viewport's resting frame is resting.
                    runtime.BoardView.SetHoveredCoordinate(null);
                    yield return null;
                }

                yield return AtViewport((1920u, 1080u));
                Assert.That(partner.Occupancy.TeamSide,
                    Is.EqualTo(runtime.Setup.Heroes.Single(hero => hero.HeroId.Equals(sabinaId))
                        .TeamSide),
                    "Arrange: the photographed piece is on the choosing seat's own side.");
                AssertTheTileOutline(
                    partner.Occupancy.Coordinate, BoardTargetOutlineSide.Friendly);
            }
            finally
            {
                UiPlayer.SetPlayerViewport(restoreWidth, restoreHeight, "8.62 restore");
            }
        }

        // -----------------------------------------------------------------------------------------
        // The frames
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Milestone 8.62, stated where it is drawn. The shipped cue paints a target's space by the
        /// target's SIDE and leaves its six corner marks the shapes they were; the retired ring
        /// treatment leaves that space its own kind-coloured bars instead.
        /// </summary>
        private static void AssertTheTileOutline(
            HexCoordinate coordinate, BoardTargetOutlineSide expected)
        {
            var highlight = TileUnder(coordinate).CornerHighlight;
            if (!BoardTargetCuePresentation.MarksTheTargetTileBySide)
            {
                Assert.That(highlight.OutlineSide, Is.EqualTo(BoardTargetOutlineSide.None),
                    "The ring treatment leaves the space its own kind-coloured corner bars.");
                return;
            }

            Assert.That(highlight.OutlineSide, Is.EqualTo(expected),
                "The space is painted by the target's SIDE: blood red on an enemy, green on an ally.");
            Assert.That(highlight.IsFullEdgeOutline,
                Is.EqualTo(BoardTargetCuePresentation.OutlinesTheWholeHex),
                "At rest the shipped cue is the six corner marks, not the closed hexagon.");
        }

        /// <summary>
        /// The user, on the first hovered frames: "I do not see the difference on hover though. So
        /// make it a bit thicker on hover. Go x2, or even more." Stated as geometry rather than as
        /// a look: the pointed-at edge's own mesh is at least twice the resting edge's.
        /// </summary>
        private static void AssertThePointedAtTileIsEdgedWhole(HexCoordinate coordinate)
        {
            if (!BoardTargetCuePresentation.MarksTheTargetTileBySide) return;
            var highlight = TileUnder(coordinate).CornerHighlight;
            Assert.That(highlight.IsHovered, Is.True,
                "Arrange: the pointer is on the target, so the frame above is a hovered frame.");
            Assert.That(highlight.IsFullEdgeOutline, Is.True,
                "Pointing at a target closes its six marks into the whole hex.");
        }

        private static string Frame(string subject, (uint Width, uint Height) viewport) =>
            CaptureRoot + "/" + subject + "-" + Phase + "-" +
            viewport.Width + "x" + viewport.Height + ".png";

        private static IEnumerator AtViewport((uint Width, uint Height) viewport)
        {
            UiPlayer.SetPlayerViewport(viewport.Width, viewport.Height, "8.62 cue captures");
            yield return null;
            yield return null;
            yield return null;
        }

        /// <summary>
        /// Takes the transient phase banner down before a photograph, so a picture of the BOARD is a
        /// picture of the board.
        ///
        /// <para>The banner is a shaded box across the playfield for about two seconds (8.45a), and a
        /// frame caught inside its fade dims every colour under it - which is exactly what a
        /// before-and-after pair of CUE COLOURS cannot afford. Its own <c>Hide</c> is used rather than
        /// a wait because a batch run's frames are far shorter than the banner's real-time schedule,
        /// so waiting a bounded number of frames is not waiting at all.</para>
        /// </summary>
        private static IEnumerator DismissTheTransientBanner(MatchSceneCompositionRoot root)
        {
            root.Runtime.PhaseBannerView?.Hide();
            yield return null;
            yield return null;
        }

        private static Ray RayAt(PrimitiveHeroPointerInput pointerInput, Vector3 worldPosition)
        {
            var camera = pointerInput.EventCamera;
            Assert.That(camera, Is.Not.Null);
            var screen = camera.WorldToScreenPoint(worldPosition);
            return camera.ScreenPointToRay(new Vector2(screen.x, screen.y));
        }

        private static HexTileView TileUnder(HexCoordinate coordinate) =>
            Object.FindObjectsByType<HexTileView>()
                .Single(value => value.Coordinate.Equals(coordinate));

        // -----------------------------------------------------------------------------------------
        // Arrangement
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Turn one: Sabina spends a secondary Movement to stand beside the enemy hero, which is the
        /// only board a Range 1 basic can shoot one from. The walk is setup and rides the headless
        /// surface, exactly as <see cref="SabinaEngagedUiJourneyPlayModeTests"/>'s own does.
        /// </summary>
        internal static IEnumerator WalkIntoContact(
            MatchSceneCompositionRoot root, HeroId sabinaId, HeroId enemyId)
        {
            var runtime = root.Runtime;
            CommitSoTheNamedSeatActs(runtime, sabinaId, SabinaCardContent.TroopMovement);
            Assert.That(runtime.SelectedActor.ActorId, Is.EqualTo(sabinaId));

            var enemyAt = runtime.Heroes.Single(hero => hero.HeroId.Equals(enemyId)).Coordinate;
            var movement = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Movement);
            Assert.That(runtime.RoundPresenter.Submit(movement), Is.True);
            var step = runtime.PendingMovementChoice.LegalDestinations
                .Where(value => value.Cost == 1 &&
                    HexDistanceQuery.Calculate(value.Destination, enemyAt) == 1)
                .ToArray();
            Assert.That(step, Is.Not.Empty,
                "Arrange: a one-space step of hers must stand beside the enemy hero.");
            Assert.That(runtime.BoardPresenter.SubmitDestination(step[0].Destination), Is.True);
            if (runtime.PendingMovementChoice != null)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            if (runtime.IsTurnActive)
            {
                Assert.That(runtime.SubmitEndTurn(), Is.True);
            }

            yield return AdvanceToNextTurn(runtime);
        }

        private static IEnumerator StartAttackThroughItsControl(MatchSceneCompositionRoot root)
        {
            yield return null;
            var runtime = root.Runtime;
            var action = runtime.RoundView.Model.Actions.Single(value =>
                KindOf(runtime, value.Command.ActionDefinitionId) == ActionKind.Attack);
            UiPlayer.Click(UiPlayer.Button(root.Hud, action.Label));
            yield return null;
        }

        private static IEnumerator CommitThroughTray(
            MatchSceneCompositionRoot root, string printedName)
        {
            var hud = root.Hud;
            UiPlayer.EnsureVisible(
                UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.NamedElementWithText(hud, "gd-card-choice", printedName));
            yield return null;
            UiPlayer.Click(UiPlayer.Button(hud, "CONFIRM FACEDOWN"));
            yield return null;
        }

        internal static IEnumerator LoadEngagedMirror()
        {
            UiPlayer.EnsurePlayerViewport();
            MatchSceneCompositionRoot.NextComposedCardContent =
                SceneCardContent.VerifiedEngagedMirror;
            var operation = SceneManager.LoadSceneAsync("Milestone1_2_HexGrid", LoadSceneMode.Single);
            Assert.That(operation, Is.Not.Null);
            while (!operation.isDone)
            {
                yield return null;
            }

            yield return null;
            yield return null;
        }
    }
}
