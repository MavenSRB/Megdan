using GuardsDigital.Presentation.Board;
using GuardsDigital.Presentation.Match;
using NUnit.Framework;
using UnityEngine;

namespace GuardsDigital.Tests.EditMode
{
    /// <summary>
    /// Milestone 8.35z12. The two pieces of geometry the spotlight rests on: where the
    /// camera stands to hold two combatants at a tilt, and where the two card faces stand beside them.
    ///
    /// <para>Both are closed forms because the board camera is orthographic - see
    /// <see cref="AttackSpotlightFraming"/> - so they are proven here rather than looked at, and the
    /// Unity tier is left to answer only what a live camera and a live panel can.</para>
    /// </summary>
    public sealed class AttackSpotlightGeometryTests
    {
        private const float Tolerance = 0.0005f;

        [TestCase(40f)]
        [TestCase(180f)]
        [TestCase(260f)]
        public void TheCards_ClearTheZoomedTokenDiscsNotOnlyTheirCenters(float radius)
        {
            var attacker = new Vector2(750f, 420f);
            var defender = new Vector2(1150f, 640f);
            Assert.That(AttackSpotlightCardLayout.TryPlace(attacker, defender,
                new Vector2(1920f, 1080f), new Vector2(300f, 420f),
                out var attackCard, out var defenseCard, pieceRadius: radius), Is.True);
            foreach (var card in new[] { attackCard, defenseCard })
            foreach (var point in new[] { attacker, defender })
            {
                var closest = new Vector2(Mathf.Clamp(point.x, card.xMin, card.xMax),
                    Mathf.Clamp(point.y, card.yMin, card.yMax));
                Assert.That(Vector2.Distance(point, closest), Is.GreaterThanOrEqualTo(radius));
            }
        }
        private static readonly Vector3 ScreenRight = Vector3.right;
        private static readonly Vector3 ScreenUp = Vector3.forward;

        // ---- Framing.

        [Test]
        public void TheFraming_LooksAtThePointBetweenTheTwoCombatants()
        {
            var attacker = new Vector3(2f, 0.03f, -1f);
            var defender = new Vector3(5f, 0.03f, 3f);

            var frame = Frame(attacker, defender);

            Assert.That(frame.Focus.x, Is.EqualTo(3.5f).Within(Tolerance));
            Assert.That(frame.Focus.z, Is.EqualTo(1f).Within(Tolerance));
        }

        /// <summary>
        /// The official single-lane map's widest playable separation, in board units. Computed from
        /// `Assets/Resources/Maps/official-single-lane.json` (210 playable spaces; extent 27.71 x
        /// 27.00). No attack can reach this far - it is here to prove the clamp holds well past
        /// anything the game can produce.
        /// </summary>
        private const float BoardMaximumSeparation = 32.909f;

        /// <summary>A plausible resting board frame; the real one comes from the notched HUD fit.</summary>
        private const float RestingSize = 15f;

        [Test]
        public void TheFraming_ZoomsInAndNeverOut()
        {
            var close = Frame(Vector3.zero, new Vector3(AttackSpotlightTiming.HexStepWorld, 0f, 0f));

            Assert.That(close.OrthographicSize, Is.LessThan(RestingSize),
                "Two adjacent pieces are framed far closer than the whole board.");
            Assert.That(close.OrthographicSize,
                Is.GreaterThanOrEqualTo(AttackSpotlightTiming.MinimumOrthographicSize),
                "and never so close that the pieces have no ground around them.");
            Assert.That(close.OrthographicSize,
                Is.LessThan(AttackSpotlightTiming.MaximumOrthographicSize * 0.5f),
                "An adjacent pair stays tight and cinematic - it must not drift toward the wide clamp.");
        }

        /// <summary>
        /// The ranged case the first version of this framing got wrong. Its only ceiling was the
        /// resting board frame, so a Ranged Attack across the board opened the shot out until it WAS
        /// the board view, with the HUD hidden and nothing gained.
        /// </summary>
        [Test]
        public void AWidePair_ClampsToAUsefulShotRatherThanBecomingTheBoardView()
        {
            var wide = Frame(Vector3.zero, ScreenUp * BoardMaximumSeparation);

            Assert.That(wide.OrthographicSize,
                Is.EqualTo(AttackSpotlightTiming.MaximumOrthographicSize).Within(Tolerance),
                "Past the framed maximum the shot CROPS rather than widening further.");
            Assert.That(wide.OrthographicSize, Is.LessThan(RestingSize),
                "and it stays strictly tighter than the board's own frame, or it is not a spotlight.");
        }

        /// <summary>
        /// The clamp and the separation it was picked for must not drift apart: every pair up to
        /// `MaximumFramedSeparation` has to FIT, on either axis, at the tilt that separation gets.
        /// </summary>
        [Test]
        public void AWidePairAtTheFramedMaximumStillFitsTheShot()
        {
            const float separation = AttackSpotlightTiming.MaximumFramedSeparation;
            foreach (var axis in new[] { ScreenUp, ScreenRight })
            {
                var frame = Frame(Vector3.zero, axis * separation);
                var halfUp = frame.OrthographicSize * Mathf.Cos(frame.TiltDegrees * Mathf.Deg2Rad);
                var halfRight = frame.OrthographicSize * (16f / 9f);
                var needed = (separation * 0.5f) + 1f;
                var available = axis == ScreenUp ? halfUp : halfRight;

                Assert.That(available, Is.GreaterThanOrEqualTo(needed),
                    "A pair at the stated framed maximum must fit whole on the axis it is spread " +
                    "along, or MaximumOrthographicSize and MaximumFramedSeparation disagree.");
                Assert.That(frame.OrthographicSize,
                    Is.LessThanOrEqualTo(AttackSpotlightTiming.MaximumOrthographicSize + Tolerance));
            }
        }

        [Test]
        public void TheTilt_TapersAsThePairSeparatesAndNeverLeavesTheBand()
        {
            var previous = float.MaxValue;
            for (var hexes = 1f; hexes <= 10f; hexes += 0.5f)
            {
                var tilt = AttackSpotlightFraming.TiltForSeparation(
                    hexes * AttackSpotlightTiming.HexStepWorld);
                Assert.That(tilt, Is.LessThanOrEqualTo(previous + Tolerance),
                    "The tilt only ever flattens as the pair separates.");
                Assert.That(tilt, Is.InRange(
                    AttackSpotlightTiming.MinimumTiltDegrees,
                    AttackSpotlightTiming.MaximumTiltDegrees));
                previous = tilt;
            }

            Assert.That(
                AttackSpotlightFraming.TiltForSeparation(AttackSpotlightTiming.HexStepWorld),
                Is.EqualTo(AttackSpotlightTiming.TiltDegrees).Within(Tolerance),
                "An adjacent pair keeps the full tilt - it is the case the tilt was designed for.");
            Assert.That(
                AttackSpotlightFraming.TiltForSeparation(BoardMaximumSeparation),
                Is.EqualTo(AttackSpotlightTiming.MinimumTiltDegrees).Within(Tolerance),
                "and a cross-board pair sits at the amendment's floor, never flat.");
        }

        [Test]
        public void TheFraming_CentresTheMidpointAtEverySeparation()
        {
            foreach (var hexes in new[] { 1f, 4f, 8f, 19f })
            {
                var separation = hexes * AttackSpotlightTiming.HexStepWorld;
                var attacker = new Vector3(3f, 0.03f, -2f);
                var defender = attacker + (ScreenUp * separation);
                var frame = Frame(attacker, defender);

                Assert.That(
                    Vector3.Distance(frame.Focus, (attacker + defender) * 0.5f),
                    Is.LessThan(Tolerance),
                    "The camera anchors to BOTH combatants at any separation, not to either one.");
            }
        }

        [Test]
        public void TheFraming_LeavesRoomForTheTiltsOwnForeshortening()
        {
            // Two pieces separated along screen-up, which is the axis the tilt compresses.
            var attacker = Vector3.zero;
            var defender = ScreenUp * 4f;
            var frame = Frame(attacker, defender);

            var halfSpan = (4f + (2f * 1f)) * 0.5f;
            var foreshortened = halfSpan / Mathf.Cos(frame.TiltDegrees * Mathf.Deg2Rad);
            Assert.That(frame.OrthographicSize,
                Is.GreaterThanOrEqualTo(foreshortened),
                "A tilted framing that ignores cos(tilt) clips the far piece off the top of the " +
                "screen, and does it without throwing anything.");
        }

        [Test]
        public void TheFraming_TiltsByTheStatedAngleAndStillLooksDownward()
        {
            var frame = Frame(Vector3.zero, new Vector3(2f, 0f, 0f));

            Assert.That(frame.TiltDegrees, Is.EqualTo(AttackSpotlightTiming.TiltDegrees));
            Assert.That(
                Vector3.Angle(frame.ViewDirection, Vector3.down),
                Is.EqualTo(AttackSpotlightTiming.TiltDegrees).Within(0.01f),
                "The tilt is measured off straight-down, which is where the board camera rests.");
            Assert.That(frame.ViewDirection.y, Is.LessThan(0f), "It still looks at the board.");
            Assert.That(
                Vector3.Dot(frame.ViewDirection.normalized, frame.ViewUp.normalized),
                Is.EqualTo(0f).Within(0.001f),
                "The camera basis stays orthogonal, or the board reads sheared.");
        }

        [Test]
        public void TheFramingRefusesToTiltOutsideTheAmendmentsBand()
        {
            var shallow = AttackSpotlightFraming.Frame(
                Vector3.zero, Vector3.right, ScreenRight, ScreenUp, 1.777f, 1f, 9f, 2f);
            var steep = AttackSpotlightFraming.Frame(
                Vector3.zero, Vector3.right, ScreenRight, ScreenUp, 1.777f, 1f, 9f, 65f);

            Assert.That(shallow.TiltDegrees,
                Is.EqualTo(AttackSpotlightTiming.MinimumTiltDegrees));
            Assert.That(steep.TiltDegrees, Is.EqualTo(AttackSpotlightTiming.MaximumTiltDegrees),
                "A steep tilt turns a top-down board game into a diorama; the amendment said modest.");
        }

        /// <summary>
        /// The depth constant and the tilt constant are one decision, so they are asserted together:
        /// under an orthographic camera the side a tilt shows is exactly depth * sin(tilt).
        /// </summary>
        [Test]
        public void TheTiltAndThePieceDepth_ShowAVisibleSideTogether()
        {
            var visible = AttackSpotlightFraming.VisibleSideHeight(
                BoardPieceDepth.Standard, AttackSpotlightTiming.TiltDegrees);

            Assert.That(visible, Is.GreaterThan(0.02f),
                "A depth this tilt cannot reveal is a depth that changed nothing.");
            Assert.That(visible, Is.LessThan(0.12f),
                "and one it reveals too much of turns a board counter into a tower.");
            Assert.That(
                AttackSpotlightFraming.VisibleSideHeight(BoardPieceDepth.Standard, 0f),
                Is.EqualTo(0f).Within(Tolerance),
                "Straight down shows no side at all - which is why the standing board is unchanged.");
        }

        // ---- The two card faces.

        /// <summary>
        /// The three properties, over the whole matrix the ranged pass has to survive: adjacency,
        /// mid-range and the maximum framed separation, in BOTH orientations. The panel separations
        /// are the ones the framing actually produces - an adjacent pair fills the tight shot and
        /// lands about 480 reference units apart, and a pair at the framed maximum lands about 890
        /// apart in the wide one, because the zoom grows with the separation it is framing.
        /// </summary>
        [Test]
        public void EachFace_StandsBesideItsOwnCombatant_AtEverySeparationAndOrientation()
        {
            var viewport = new Vector2(2560f, 1440f);
            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);
            var centre = viewport * 0.5f;

            foreach (var panelSeparation in new[] { 480f, 700f, 890f })
            {
                foreach (var sign in new[] { 1f, -1f })
                {
                    var attacker = new Vector2(centre.x - (sign * panelSeparation * 0.5f), 700f);
                    var defender = new Vector2(centre.x + (sign * panelSeparation * 0.5f), 760f);

                    Assert.That(
                        AttackSpotlightCardLayout.TryPlace(
                            attacker, defender, viewport, cardSize,
                            out var attackerCard, out var defenderCard),
                        Is.True);

                    var label = $"separation={panelSeparation} sign={sign}";
                    AssertInside(attackerCard, viewport, label);
                    AssertInside(defenderCard, viewport, label);
                    Assert.That(attackerCard.Overlaps(defenderCard), Is.False,
                        "The two faces must never cover each other. " + label);
                    AssertClearOfPieces(attackerCard, attacker, defender, label);
                    AssertClearOfPieces(defenderCard, attacker, defender, label);

                    // Each face is nearer to its OWN combatant than to the other one. This is the
                    // whole point of the ranged rewrite: the screen-edge rule it replaced put a
                    // face at the far edge of the panel from the piece it describes.
                    Assert.That(
                        Mathf.Abs(attackerCard.center.x - attacker.x),
                        Is.LessThan(Mathf.Abs(attackerCard.center.x - defender.x)),
                        "The attacker's face is beside the attacker. " + label);
                    Assert.That(
                        Mathf.Abs(defenderCard.center.x - defender.x),
                        Is.LessThan(Mathf.Abs(defenderCard.center.x - attacker.x)),
                        "The defender's face is beside the defender. " + label);
                }
            }
        }

        [Test]
        public void EachFace_TakesTheOutwardSideOfItsOwnPiece()
        {
            var viewport = new Vector2(2560f, 1440f);
            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);

            AttackSpotlightCardLayout.TryPlace(
                new Vector2(1000f, 700f), new Vector2(1560f, 700f), viewport, cardSize,
                out var attackerCard, out var defenderCard);

            Assert.That(attackerCard.xMax, Is.LessThanOrEqualTo(1000f),
                "The attacker is the left of the pair, so its face goes to its left - outward, " +
                "away from the ground the two pieces are fighting over.");
            Assert.That(defenderCard.xMin, Is.GreaterThanOrEqualTo(1560f),
                "and the defender's goes to its right.");
        }

        /// <summary>
        /// The flip. A piece pushed against its own edge of the panel - which the wide shot does,
        /// because the framing spreads a ranged pair toward the frame - has no room outward, and the
        /// face must move to the piece's other side rather than slide away from the piece.
        /// </summary>
        [Test]
        public void AFaceFlipsInward_WhenItsOwnPieceIsAgainstTheEdge()
        {
            var viewport = new Vector2(2560f, 1440f);
            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);

            AttackSpotlightCardLayout.TryPlace(
                new Vector2(80f, 700f), new Vector2(2480f, 700f), viewport, cardSize,
                out var attackerCard, out var defenderCard);

            AssertInside(attackerCard, viewport, "flipped attacker");
            AssertInside(defenderCard, viewport, "flipped defender");
            Assert.That(attackerCard.xMin, Is.GreaterThanOrEqualTo(80f),
                "There is no panel to the left of a piece at x=80, so its face flipped to the right.");
            Assert.That(defenderCard.xMax, Is.LessThanOrEqualTo(2480f),
                "and the far piece's face flipped to the left.");
            Assert.That(attackerCard.Overlaps(defenderCard), Is.False);
            AssertClearOfPieces(attackerCard, new Vector2(80f, 700f), new Vector2(2480f, 700f), "flip");
            AssertClearOfPieces(defenderCard, new Vector2(80f, 700f), new Vector2(2480f, 700f), "flip");
        }

        [Test]
        public void EachFace_FollowsItsOwnPieceVerticallyAndStaysOnScreen()
        {
            var viewport = new Vector2(2560f, 1440f);
            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);

            AttackSpotlightCardLayout.TryPlace(
                new Vector2(900f, 400f), new Vector2(1700f, 1000f), viewport, cardSize,
                out var high, out var low);
            Assert.That(high.y, Is.LessThan(low.y), "The face tracks the piece it belongs to.");

            AttackSpotlightCardLayout.TryPlace(
                new Vector2(900f, -400f), new Vector2(1700f, 3000f), viewport, cardSize,
                out var above, out var below);
            AssertInside(above, viewport);
            AssertInside(below, viewport);
        }

        [Test]
        public void APanelTooNarrowForTwoFaces_ShowsNoneRatherThanOverlappingOnes()
        {
            var cardSize = new Vector2(CardFootprint.Width, CardFootprint.Height);

            Assert.That(
                AttackSpotlightCardLayout.TryPlace(
                    new Vector2(100f, 100f), new Vector2(200f, 100f),
                    new Vector2(400f, 1440f), cardSize, out _, out _),
                Is.False);
        }

        [Test]
        public void TwoPiecesOnTheSamePoint_StillGetADeterministicSide()
        {
            var point = new Vector2(1280f, 720f);

            Assert.That(
                AttackSpotlightCardLayout.TakesLeftEdge(point, point), Is.True,
                "Two clients that disagreed here would draw the same attack mirrored.");
            Assert.That(
                AttackSpotlightCardLayout.TakesLeftEdge(point, new Vector2(1280f, 900f)), Is.True,
                "A dead heat on x is broken by y rather than by whichever compare ran first.");
        }

        private static AttackSpotlightCameraFrame Frame(Vector3 attacker, Vector3 defender) =>
            AttackSpotlightFraming.Frame(
                attacker, defender, ScreenRight, ScreenUp, 16f / 9f, 1f, RestingSize);

        private static void AssertInside(Rect rect, Vector2 viewport, string label = "")
        {
            Assert.That(rect.xMin, Is.GreaterThanOrEqualTo(0f), label);
            Assert.That(rect.yMin, Is.GreaterThanOrEqualTo(0f), label);
            Assert.That(rect.xMax, Is.LessThanOrEqualTo(viewport.x), label);
            Assert.That(rect.yMax, Is.LessThanOrEqualTo(viewport.y), label);
        }

        /// <summary>
        /// The property that actually ruins a spotlight: a face covering a PIECE. The camera has just
        /// travelled to show those two pieces, so a card sitting on one of them undoes the whole move.
        /// </summary>
        private static void AssertClearOfPieces(
            Rect card, Vector2 attacker, Vector2 defender, string label)
        {
            Assert.That(card.Contains(attacker), Is.False,
                "A face is covering the attacking piece. " + label);
            Assert.That(card.Contains(defender), Is.False,
                "A face is covering the defending piece. " + label);
        }
    }
}
