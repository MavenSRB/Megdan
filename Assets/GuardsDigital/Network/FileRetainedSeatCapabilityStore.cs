using System;
using System.IO;
using System.Text;
using GuardsDigital.Application;
using GuardsDigital.Core.Participants;

namespace GuardsDigital.Network
{
    /// <summary>
    /// Milestone 8.35k. The store a real player's machine uses: one small file per session and
    /// seat, under a directory the caller supplies.
    /// </summary>
    /// <remarks>
    /// <para>This lives in the adapter, beneath the transport-neutral session port, for the same
    /// reason <see cref="CryptoNonceSource"/> does: <c>GuardsDigital.Application.PrivateOnline</c>
    /// states the storage contract (<see cref="IRetainedSeatCapabilityStore"/>) only, and the
    /// conformance scan that keeps a socket or engine type out of that layer does not govern this
    /// assembly. The directory is injected rather than discovered because this assembly is itself
    /// engine-free (<c>noEngineReferences</c>) - a packaged Win64 player passes
    /// <c>UnityEngine.Application.persistentDataPath</c> from Presentation, which is the one place
    /// that knows Unity is underneath. <c>persistentDataPath</c> is per-user and outside the
    /// installation, so the build directory stays read-only and a returned playtest log bundle
    /// sweeps up nothing from here.</para>
    /// <para>Every failure is swallowed deliberately. A machine with no writable profile, a
    /// read-only disk or a locked file is a machine whose player simply does not get the automatic
    /// road back - they get the host's deliberate release instead, which is exactly what that
    /// control exists for. A store that threw would turn a convenience into a crash on the entry
    /// form, and the reconnect mechanism itself does not depend on this succeeding.</para>
    /// </remarks>
    public sealed class FileRetainedSeatCapabilityStore : IRetainedSeatCapabilityStore
    {
        private readonly string root;

        public FileRetainedSeatCapabilityStore(string root)
        {
            this.root = root ?? string.Empty;
        }

        public string Find(string sessionCorrelation, SeatId seat)
        {
            var path = PathFor(sessionCorrelation, seat);
            if (path.Length == 0)
            {
                return string.Empty;
            }

            try
            {
                return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public void Retain(string sessionCorrelation, SeatId seat, string capability)
        {
            var path = PathFor(sessionCorrelation, seat);
            if (path.Length == 0 || string.IsNullOrEmpty(capability))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, capability, Encoding.UTF8);
            }
            catch (Exception)
            {
                // See the type's own remarks: an unwritable machine loses the automatic road and
                // keeps the deliberate one, rather than failing the player's entry.
            }
        }

        public void Forget(string sessionCorrelation, SeatId seat)
        {
            var path = PathFor(sessionCorrelation, seat);
            if (path.Length == 0)
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// One file per session and seat, named from both so an entry cannot be read back into a
        /// session it does not belong to. Both parts are reduced to characters a file name admits
        /// on every platform, and the reduction is one-way padding rather than an escape: two
        /// different correlations can only ever collide into the same name, which loses a road back
        /// and never grants one, because the host still has to accept what the file holds.
        /// </summary>
        private string PathFor(string sessionCorrelation, SeatId seat)
        {
            if (root.Length == 0 ||
                string.IsNullOrWhiteSpace(sessionCorrelation) ||
                !seat.IsDefined)
            {
                return string.Empty;
            }

            return Path.Combine(
                root,
                "private-online-return",
                Sanitize(sessionCorrelation) + "." + Sanitize(seat.Value) + ".capability");
        }

        private static string Sanitize(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value)
            {
                builder.Append(
                    char.IsLetterOrDigit(character) || character == '-' || character == '_'
                        ? character
                        : '_');
            }

            return builder.ToString();
        }
    }
}
