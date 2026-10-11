using BoneLib;

using HarmonyLib;

using Il2CppSLZ.Marrow;

namespace SuperSlowMo
{
    /// <summary>
    /// TimeInput is what the slow-mo button calls. For the local player we swallow it completely so the game's own
    /// slow-mo (which changes Time.timeScale and would slow the player too) never kicks in, and toggle ours instead.
    /// </summary>
    [HarmonyPatch(typeof(OpenControllerRig))]
    public static class OpenControllerRigPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(OpenControllerRig.TimeInput))]
        public static bool TimeInputPrefix(OpenControllerRig __instance, bool down, bool up, bool touch)
        {
            if (!SlowMoSettings.ReplaceSlowMo || !IsLocalPlayer(__instance))
                return true;

            if (down)
                WorldSlowMo.Toggle();

            __instance._timeInput = false;
            return false;
        }

        private static bool IsLocalPlayer(OpenControllerRig rig)
        {
            try
            {
                return Player.ControllerRig != null && rig == Player.ControllerRig;
            }
            catch
            {
                return false;
            }
        }
    }
}
