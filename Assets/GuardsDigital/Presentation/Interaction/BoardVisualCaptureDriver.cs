using System;
using System.Collections;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>
    /// Milestone 8.31b. Captures the running player's own frame so a gate can look at the pixels a
    /// built player actually draws.
    /// </summary>
    /// <remarks>
    /// <para>Every existing visual check in this project is taken in the Editor, where every shader
    /// variant and every asset is present. That is exactly the blind spot this driver exists to
    /// close: the board's blended overlays were correct in the Editor and opaque white in a built
    /// player for as long as the artwork has existed, and nothing looked.</para>
    /// <para>It follows <see cref="PlayerSmokeTestDriver"/>'s pattern - inert unless its flag is
    /// present, attached only then, and reporting through log markers because a player has no test
    /// runner. It captures rather than asserts: what counts as a correct board is decided by the
    /// gate script that reads the file, not here.</para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class BoardVisualCaptureDriver : MonoBehaviour
    {
        public const string CommandLineFlag = "-guardsBoardCapture";
        public const string MarkerPrefix = "GUARDS_VISUAL:";

        /// <summary>
        /// Frames to let the scene compose, the camera frame the map, and the HUD lay out before the
        /// picture is taken. Generous: a capture of a half-built board would be a false failure, and
        /// the gate's cost is dominated by the build rather than by this wait.
        /// </summary>
        private const int SettleFrames = 120;

        private MatchSceneCompositionRoot compositionRoot;
        private string outputPath;
        private Camera boardCamera;
        private Board.PlaceholderTestGridView gridView;

        /// <summary>The capture path this player was launched with, or null when it was not.</summary>
        public static string RequestedPath()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], CommandLineFlag, StringComparison.Ordinal))
                {
                    var candidate = arguments[index + 1];
                    return string.IsNullOrWhiteSpace(candidate) ? null : candidate;
                }
            }

            return null;
        }

        /// <summary>
        /// Handed the camera and the grid it photographs, rather than finding them: global
        /// discovery belongs to the composition root, which has already resolved both by the time
        /// this driver is attached.
        /// </summary>
        public void Configure(
            MatchSceneCompositionRoot root,
            string path,
            Camera camera,
            Board.PlaceholderTestGridView grid)
        {
            compositionRoot = root;
            outputPath = path;
            boardCamera = camera;
            gridView = grid;
            // A hidden or unfocused window must keep rendering long enough to be photographed, for
            // the reason corrective 8.31a gave: the checked-in player pauses its whole loop on focus
            // loss, and a gate cannot hold focus for it.
            UnityEngine.Application.runInBackground = true;
        }

        private IEnumerator Start()
        {
            Log("BEGIN");
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                Fail("no capture path was given");
                UnityEngine.Application.Quit(2);
                yield break;
            }

            if (compositionRoot == null || compositionRoot.Runtime == null)
            {
                Fail("the scene never composed, so there is no board to capture");
                UnityEngine.Application.Quit(1);
                yield break;
            }

            for (var frame = 0; frame < SettleFrames; frame++)
            {
                yield return null;
            }

            yield return new WaitForEndOfFrame();

            Texture2D captured = null;
            try
            {
                // Rendered through the board camera into a texture rather than grabbed off the
                // screen: a hidden window presents no swapchain, so ScreenCapture returns nothing,
                // and a gate cannot hold a window in the foreground. This also frames exactly what
                // the camera draws - the board - with none of the overlay HUD in the way.
                captured = RenderBoardCamera();
                if (captured == null)
                {
                    Fail("the scene exposed no camera to render");
                    UnityEngine.Application.Quit(1);
                    yield break;
                }

                var png = captured.EncodeToPNG();
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(outputPath, png);
                Log("CAPTURED width=" + captured.width.ToString(CultureInfo.InvariantCulture) +
                    " height=" + captured.height.ToString(CultureInfo.InvariantCulture) +
                    " bytes=" + png.Length.ToString(CultureInfo.InvariantCulture));
                Log("BOARD artwork=" + (BoardHasArtwork() ? "true" : "false"));
            }
            catch (Exception exception)
            {
                Fail("the capture failed: " + exception.GetType().Name + " " + exception.Message);
                UnityEngine.Application.Quit(1);
                yield break;
            }
            finally
            {
                if (captured != null)
                {
                    Destroy(captured);
                }
            }

            Log("EXIT 0");
            UnityEngine.Application.Quit(0);
        }

        /// <summary>
        /// Draws the board camera into an owned texture at a fixed size, so one build always
        /// produces one comparable picture regardless of the window it was launched with.
        /// </summary>
        private Texture2D RenderBoardCamera()
        {
            var camera = boardCamera;
            if (camera == null)
            {
                return null;
            }

            const int width = 1920;
            const int height = 1080;
            var target = new RenderTexture(width, height, 24) { name = "Owned Board Capture" };
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            Texture2D texture = null;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                target.Release();
                Destroy(target);
            }

            return texture;
        }

        /// <summary>
        /// Whether the official artwork is underneath, recorded beside the capture so a gate can
        /// tell "the board is one flat colour because the overlays are opaque" from "there is no
        /// artwork in this checkout at all".
        /// </summary>
        private bool BoardHasArtwork() => gridView != null && gridView.IsUnderlayActive;

        private static void Log(string message) => Debug.Log(MarkerPrefix + " " + message);

        private static void Fail(string reason) => Debug.LogError(MarkerPrefix + " FAIL " + reason);
    }
}
