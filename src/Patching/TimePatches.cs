using HarmonyLib;

using UnityEngine;

namespace Quicksilver.Patching
{
    [HarmonyPatch(typeof(Time))]
    public static class TimePatches
    {
        public static bool ReturnScaled = false;
        public static bool ForceDefaultTimescale = false;
        public static bool ReturnInversed = false;

        [HarmonyPostfix]
        [HarmonyPatch("deltaTime", MethodType.Getter)]
        public static void GetDeltaTime(ref float __result)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            float timeScale = GetRealTimeScale();

            if (ReturnScaled)
            {
                __result /= timeScale;
            }
            else if (ReturnInversed)
            {
                __result *= timeScale;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("fixedDeltaTime", MethodType.Getter)]
        public static void GetFixedDeltaTime(ref float __result)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            float timeScale = GetRealTimeScale();

            if (ReturnScaled)
            {
                __result /= timeScale;
            }
            else if (ReturnInversed)
            {
                __result *= timeScale;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("timeScale", MethodType.Getter)]
        public static void GetTimeScale(ref float __result)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (ForceDefaultTimescale)
                __result = 1f;
        }

        public static float GetRealTimeScale()
        {
            bool previous = ForceDefaultTimescale;
            ForceDefaultTimescale = false;
            float timeScale = Time.timeScale;
            ForceDefaultTimescale = previous;
            return timeScale;
        }
    }
}
