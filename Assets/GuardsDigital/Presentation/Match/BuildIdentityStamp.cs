using System.Runtime.InteropServices;
using UnityEngine;

namespace GuardsDigital.Presentation.Match
{
    /// <summary>
    /// The build/package stamp a player can glance at to answer "which build am I running" -
    /// coordinator-directed small QoL item (2026-09-05, PLAN.md's "SMALL QoL ITEM"), after the user
    /// lost time to a stale package folder. The identity tuple already reads this exact formula for
    /// its own <c>BuildIdentity</c> element (<see cref="ConfiguredMatchSceneRuntime"/>), which is
    /// what <c>Tools/</c> packaging writes into a build's <c>BUILD-IDENTITY.txt</c>; this class is
    /// the ONE place that formula lives now, so the runtime glance and the packaged record can
    /// never drift apart by a second copy of the same derivation.
    /// </summary>
    public static class BuildIdentityStamp
    {
        /// <summary>
        /// The stamp string: the Player Settings version (which packaging bakes the
        /// timestamp+commit into, see any <c>BUILD-IDENTITY.txt</c>) plus the Unity Editor version
        /// that built it - exactly <see cref="PrivateSessionIdentity.BuildIdentity"/>'s own value.
        /// </summary>
        public static string Current =>
            UnityEngine.Application.version + "+" + UnityEngine.Application.unityVersion;

        /// <summary>
        /// Runs once at launch, before any scene loads, so the title bar carries the stamp from the
        /// first frame - including at the lobby, which composes no <see cref="ConfiguredMatchSceneRuntime"/>.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyAtLaunch() => ApplyToWindowTitle();

        /// <summary>
        /// Stamps the OS window title with <see cref="Current"/>, so a glance at the title bar
        /// settles which package is running without opening the lobby. Windows standalone players
        /// only (the platform this project ships to); a no-op everywhere else, including the
        /// Editor, where the Editor's own window already carries this information. Defensive by
        /// design - a title-bar cosmetic must never be the thing that takes a build down.
        /// </summary>
        public static void ApplyToWindowTitle()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                var handle = GetActiveWindow();
                if (handle != System.IntPtr.Zero)
                {
                    SetWindowText(handle, UnityEngine.Application.productName + " - " + Current);
                }
            }
            catch
            {
                // A cosmetic best-effort: never let a title-bar stamp fault the game.
            }
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern System.IntPtr GetActiveWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowText(System.IntPtr hwnd, string text);
#endif
    }
}
