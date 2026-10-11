using System.Diagnostics;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Real time, untouched by the time scale (and by Quicksilver's time patches).
    /// </summary>
    internal static class FxClock
    {
        private static readonly Stopwatch Watch = Stopwatch.StartNew();

        public static float Now => (float)Watch.Elapsed.TotalSeconds;
    }
}
