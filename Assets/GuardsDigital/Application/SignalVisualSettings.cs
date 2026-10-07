using System;
namespace GuardsDigital.Application
{
    /// <summary>Session presentation preference, deliberately absent from rules, clocks and snapshots.</summary>
    public sealed class SignalVisualSettings
    {
        public static readonly SignalVisualSettings Default = new SignalVisualSettings(1400, 100);
        public SignalVisualSettings(int periodMilliseconds, int intensityPercent)
        {
            if (periodMilliseconds < 400 || periodMilliseconds > 4000) throw new ArgumentOutOfRangeException(nameof(periodMilliseconds));
            if (intensityPercent < 0 || intensityPercent > 100) throw new ArgumentOutOfRangeException(nameof(intensityPercent));
            PeriodMilliseconds = periodMilliseconds; IntensityPercent = intensityPercent;
        }
        public int PeriodMilliseconds { get; }
        public int IntensityPercent { get; }
    }
}
