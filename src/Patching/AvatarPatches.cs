using HarmonyLib;

using UnityEngine;

using Avatar = Il2CppSLZ.VRMK.Avatar;

namespace Quicksilver.Patching
{
    [HarmonyPatch(typeof(Avatar))]
    public static class AvatarPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("ComputeMass")]
        public static void ComputeMass(Avatar __instance, float normalizeTo82)
        {
            if (QuicksilverMod.TargetTimeScale <= 0f || !QuicksilverMod.IsMainRig(__instance) || !QuicksilverMod.IsEnabled)
                return;

            float mult = 1f / QuicksilverMod.TargetTimeScale;
            float totalMult = 1f / Mathf.Lerp(1f, QuicksilverMod.TargetTimeScale, 0.9f);

            __instance._massChest *= mult;
            __instance._massHead *= mult;
            __instance._massPelvis *= mult;
            __instance._massArm *= mult;
            __instance._massLeg *= mult;
            __instance._massTotal *= totalMult;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ComputeBaseStats")]
        public static void ComputeBaseStats(Avatar __instance)
        {
            if (QuicksilverMod.TargetTimeScale <= 0f || !QuicksilverMod.IsMainRig(__instance) || !QuicksilverMod.IsEnabled)
                return;

            float mult = 1f / QuicksilverMod.TargetTimeScale;
            float sqrMult = mult * mult;

            __instance._strengthUpper *= sqrMult;
            __instance._strengthLower *= sqrMult * sqrMult;
            __instance._speed *= mult;
            __instance._agility *= sqrMult * sqrMult;
        }
    }
}
