using HarmonyLib;

using Il2CppSLZ.Marrow;

using UnityEngine;

namespace Quicksilver.Patching
{
    [HarmonyPatch(typeof(AnimationRig))]
    public static class AnimationRigPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePrefix(AnimationRig __instance)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.IsMainRig(__instance))
                TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePostfix(AnimationRig __instance)
        {
            TimePatches.ReturnScaled = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("BodyVelocity")]
        public static void BodyVelocity(AnimationRig __instance, Rig inRig, ref Vector2 vel, ref Vector2 accel, float deltaTime)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (!QuicksilverMod.IsMainRig(__instance))
                return;

            vel *= QuicksilverMod.TargetTimeScale;
            accel *= QuicksilverMod.TargetTimeScale;
        }
    }

    [HarmonyPatch(typeof(ArtRig))]
    public static class ArtRigPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("ArtOutputUpdate")]
        public static void ArtOutputUpdatePrefix(ArtRig __instance, PhysicsRig inRig)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.TargetTimeScale > 0f && QuicksilverMod.IsMainRig(inRig))
                TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("ArtOutputUpdate")]
        public static void ArtOutputUpdatePostfix()
        {
            TimePatches.ReturnScaled = false;
        }
    }

    [HarmonyPatch(typeof(OpenControllerRig))]
    public static class OpenControllerRigPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateHeptaBody")]
        public static void UpdateHeptaBody(OpenControllerRig __instance, ref float deltaTime)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.TargetTimeScale > 0f && QuicksilverMod.IsMainRig(__instance))
                deltaTime /= QuicksilverMod.TargetTimeScale;
        }

        [HarmonyPrefix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePrefix(OpenControllerRig __instance)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.IsMainRig(__instance))
            {
                TimePatches.ReturnScaled = true;
                TimePatches.ForceDefaultTimescale = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePostfix(OpenControllerRig __instance)
        {
            TimePatches.ReturnScaled = false;
            TimePatches.ForceDefaultTimescale = false;
        }
    }

    [HarmonyPatch(typeof(RemapRig))]
    public static class RemapRigPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePrefix(RemapRig __instance)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.IsMainRig(__instance))
                TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnEarlyUpdate")]
        public static void OnEarlyUpdatePostfix(RemapRig __instance)
        {
            TimePatches.ReturnScaled = false;
        }
    }

    [HarmonyPatch(typeof(SLZ_Body.Footstep))]
    public static class FootstepPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateStepping")]
        public static void UpdateStepping(Vector3 rootPosition, Quaternion rootRotation, Quaternion footRotation, Vector3 rootUp, ref Vector3 velocitySanGrav, ref Vector3 accel, float angularVel, float velocitySanGravNorm, float stepLZ, float velDot, float sacrumHeightWeight, float deltaTime)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.TargetTimeScale > 0f)
            {
                float mult = Mathf.Lerp(1f, QuicksilverMod.TargetTimeScale, 0.95f);
                velocitySanGrav *= mult;
                accel *= mult;
            }
        }
    }
}
