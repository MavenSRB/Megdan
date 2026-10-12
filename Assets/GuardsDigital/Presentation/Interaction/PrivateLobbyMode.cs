using System;
using GuardsDigital.Application;

namespace GuardsDigital.Presentation.Interaction
{
    /// <summary>Optional mode adapter. The shared lobby never depends on a mode assembly.</summary>
    public static class PrivateLobbyMode
    {
        public const string PveId = "pve";
        public const string PveLabel = "PvE — Defend the creeps";

        public static Action<PrivateLobbyEntry, bool, ulong> OpenPve { get; set; }
        public static Func<SessionRejectMessage, ulong?> ReadPveSeed { get; set; }
    }
}
