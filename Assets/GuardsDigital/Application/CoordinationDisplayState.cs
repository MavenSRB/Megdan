using System;
using System.Collections.Generic;
using System.Linq;
namespace GuardsDigital.Application
{
    /// <summary>Communication-only state; never a gameplay projection or acknowledgement.</summary>
    public sealed class CoordinationDisplayState
    {
        public CoordinationDisplayState(long version, long revision, int generation, IEnumerable<TeammateMarker> markers)
        {
            if(version<0 || revision<0 || generation<0) throw new ArgumentOutOfRangeException(nameof(version));
            Version=version; Revision=revision; Generation=generation;
            var rows=(markers ?? throw new ArgumentNullException(nameof(markers))).ToArray();
            if(rows.Length>10 || rows.Any(m=>m==null || !m.Sender.IsDefined || m.Revision<0) ||
                rows.Select(m=>m.Sender).Distinct().Count()!=rows.Length) throw new ArgumentException("Invalid teammate markers.");
            Markers=Array.AsReadOnly(rows);
        }
        public long Version { get; }
        public long Revision { get; }
        public int Generation { get; }
        public IReadOnlyList<TeammateMarker> Markers { get; }
    }
}
