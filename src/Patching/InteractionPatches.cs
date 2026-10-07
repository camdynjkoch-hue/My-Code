using System;

using HarmonyLib;

using Il2CppSLZ.Marrow;

using UnityEngine;

namespace Quicksilver.Patching
{
    [HarmonyPatch(typeof(ForcePullGrip), "OnFarHandHoverUpdate")]
    public static class ForcePullPatches
    {
        public static void Prefix(ForcePullGrip __instance, Hand hand)
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.TargetTimeScale > 0f)
                __instance.maxForce = 250f / QuicksilverMod.TargetTimeScale;
        }
    }

    [HarmonyPatch(typeof(AlignPlug))]
    public static class AlignPlugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void UpdatePrefix()
        {
            if (!QuicksilverMod.IsEnabled)
                return;

            if (QuicksilverMod.TargetTimeScale > 0f)
                TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePostfix()
        {
            TimePatches.ReturnScaled = false;
        }
    }

    [HarmonyPatch(typeof(Gun))]
    public static class GunPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        public static void UpdatePrefix(Gun __instance)
        {
            if (QuicksilverMod.TargetTimeScale > 0f
                && __instance.triggerGrip
                && __instance.triggerGrip.GetHand()
                && __instance.triggerGrip.GetHand().manager == QuicksilverMod.Instance.rigManager)
            {
                if (QuicksilverMod.IsEnabled)
                    __instance.fireDuration = 60f / __instance.roundsPerMinute * QuicksilverMod.TargetTimeScale;
                else
                    __instance.fireDuration = 60f / __instance.roundsPerMinute;
            }

            TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Update")]
        public static void UpdatePostfix()
        {
            TimePatches.ReturnScaled = false;
        }
    }

    [HarmonyPatch(typeof(HandJointConfiguration))]
    public static class HandJointConfigurationPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch("LockConfiguration")]
        public static void LockConfiguration(ConfigurableJoint joint)
        {
            float timeScale = QuicksilverMod.TargetTimeScale;

            if (QuicksilverMod.IsEnabled && timeScale > 0f)
            {
                float sqrTimeScale = timeScale * timeScale;
                joint.breakForce /= sqrTimeScale;
                joint.breakTorque /= sqrTimeScale;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ApplyConfiguration", new Type[] { typeof(ConfigurableJoint) })]
        public static void ApplyConfiguration(ConfigurableJoint joint)
        {
            if (QuicksilverMod.IsEnabled && QuicksilverMod.TargetTimeScale > 0f)
            {
                joint.breakForce /= QuicksilverMod.TargetTimeScale * QuicksilverMod.TargetTimeScale;
                joint.breakTorque /= QuicksilverMod.TargetTimeScale * QuicksilverMod.TargetTimeScale;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("ApplyConfiguration", new Type[] { typeof(Quaternion), typeof(ConfigurableJoint) })]
        public static void ApplyConfiguration(Quaternion localRotation, ConfigurableJoint joint)
        {
            if (QuicksilverMod.IsEnabled && QuicksilverMod.TargetTimeScale > 0f)
            {
                joint.breakForce /= QuicksilverMod.TargetTimeScale * QuicksilverMod.TargetTimeScale;
                joint.breakTorque /= QuicksilverMod.TargetTimeScale * QuicksilverMod.TargetTimeScale;
            }
        }
    }

    [HarmonyPatch(typeof(HandgunVirtualController))]
    public static class HandgunVirtualControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnVirtualControllerSolve")]
        public static void OnVirtualControllerSolvePrefix(VirtualControlerPayload p)
        {
            TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnVirtualControllerSolve")]
        public static void OnVirtualControllerSolvePostfix(VirtualControlerPayload p)
        {
            TimePatches.ReturnScaled = false;
        }
    }

    [HarmonyPatch(typeof(RifleVirtualController))]
    public static class RifleVirtualControllerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnVirtualControllerSolve")]
        public static void OnVirtualControllerSolvePrefix(VirtualControlerPayload payload)
        {
            TimePatches.ReturnScaled = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("OnVirtualControllerSolve")]
        public static void OnVirtualControllerSolvePostfix(VirtualControlerPayload payload)
        {
            TimePatches.ReturnScaled = false;
        }
    }
}
