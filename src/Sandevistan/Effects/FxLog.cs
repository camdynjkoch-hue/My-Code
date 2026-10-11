using System;
using System.Collections.Generic;

using MelonLoader;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Error logging that stops repeating itself, since most effect code runs every frame.
    /// </summary>
    internal static class FxLog
    {
        private const int MaxPerSource = 3;

        private static readonly Dictionary<string, int> Counts = new Dictionary<string, int>();

        public static void Error(string where, Exception e)
        {
            Counts.TryGetValue(where, out var count);
            count++;
            Counts[where] = count;

            if (count <= MaxPerSource)
                MelonLogger.Error(where + " failed: " + e);
            else if (count == MaxPerSource + 1)
                MelonLogger.Error(where + " keeps failing; further errors from it are hidden.");
        }
    }
}
