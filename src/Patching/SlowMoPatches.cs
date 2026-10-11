using HarmonyLib;

using Il2CppSLZ.Marrow;

using Quicksilver.Sandevistan;

namespace Quicksilver.Patching
{
    /// <summary>
    /// Replaces BONELAB's slow-mo ability with the Sandevistan. The slow-mo button calls one of these two methods;
    /// instead of stepping the game's own slow-motion, it toggles the Sandevistan.
    /// </summary>
    [HarmonyPatch(typeof(TimeManager))]
    public static class SlowMoPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(TimeManager.DECREASE_TIMESCALE))]
        public static bool DECREASE_TIMESCALE()
        {
            return HandleSlowMoButton();
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(TimeManager.TOGGLE_TIMESCALE))]
        public static bool TOGGLE_TIMESCALE()
        {
            return HandleSlowMoButton();
        }

        private static bool HandleSlowMoButton()
        {
            if (!SandevistanSettings.ReplaceSlowMo.Value)
                return true;

            SandevistanTime.OnSlowMoButton();

            // Skip the game's own slow-mo
            return false;
        }
    }
}
