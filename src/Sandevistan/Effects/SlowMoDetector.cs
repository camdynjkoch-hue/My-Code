using System;
using System.Runtime.CompilerServices;

using Il2CppSLZ.Marrow;

using MelonLoader;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Decides when the visual effects are on: always while the Sandevistan is active, and also during BONELAB's own
    /// slow-mo when the slow-mo button isn't replaced.
    /// </summary>
    internal static class SlowMoDetector
    {
        private static bool _timeManagerUnavailable;

        public static bool IsSlowMoActive()
        {
            if (SandevistanTime.IsActive)
                return true;

            float timeScale = Time.timeScale;
            if (!(timeScale > 0.001f) || !(timeScale < 0.995f))
                return false;

            if (SandevistanSettings.TriggerOnAnyTimeSlowdown.Value || _timeManagerUnavailable)
                return true;

            try
            {
                return ReadSlowMoIntensity() > 1.01f;
            }
            catch (Exception e)
            {
                _timeManagerUnavailable = true;
                MelonLogger.Warning("Couldn't read BONELAB's TimeManager, falling back to watching Time.timeScale: " + e.Message);
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float ReadSlowMoIntensity()
        {
            return TimeManager.cur_intensity;
        }
    }
}
