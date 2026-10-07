using HarmonyLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.Audio;

namespace Quicksilver.Patching
{
    // Keeps the player's own sound effects at normal pitch while in slow-motion.

    [HarmonyPatch(typeof(FootstepSFX))]
    public static class FootstepSFXPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("PlayStep")]
        public static void PlayStepPrefix(float velocitySqr) => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("PlayStep")]
        public static void PlayStepPostfix(float velocitySqr) => TimePatches.ForceDefaultTimescale = false;
    }

    [HarmonyPatch(typeof(HandSFX))]
    public static class HandSFXPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("Grab")]
        public static void GrabPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("Grab")]
        public static void GrabPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("BodySlot")]
        public static void BodySlotPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("BodySlot")]
        public static void BodySlotPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("Drop")]
        public static void DropPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("Drop")]
        public static void DropPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("ForcePull")]
        public static void ForcePullPrefix(float massDistance) => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("ForcePull")]
        public static void ForcePullPostfix(float massDistance) => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("OnSignificantCollisionEnter")]
        public static void OnSignificantCollisionEnterPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("OnSignificantCollisionEnter")]
        public static void OnSignificantCollisionEnterPostfix() => TimePatches.ForceDefaultTimescale = false;
    }

    [HarmonyPatch(typeof(HeadSFX))]
    public static class HeadSFXPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("SmallDamageVocal")]
        public static void SmallDamageVocalPrefix(float damage) => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("SmallDamageVocal")]
        public static void SmallDamageVocalPostfix(float damage) => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("BigDamageVocal")]
        public static void BigDamageVocalPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("BigDamageVocal")]
        public static void BigDamageVocalPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("DyingVocal")]
        public static void DyingVocalPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("DyingVocal")]
        public static void DyingVocalPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("DeathVocal")]
        public static void DeathVocalPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("DeathVocal")]
        public static void DeathVocalPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("RecoveryVocal")]
        public static void RecoveryVocalPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("RecoveryVocal")]
        public static void RecoveryVocalPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("JumpEffort")]
        public static void JumpEffortPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("JumpEffort")]
        public static void JumpEffortPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("DoubleJump")]
        public static void DoubleJumpPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("DoubleJump")]
        public static void DoubleJumpPostfix() => TimePatches.ForceDefaultTimescale = false;

        [HarmonyPrefix]
        [HarmonyPatch("OnSignificantCollisionEnter")]
        public static void OnSignificantCollisionEnterPrefix() => TimePatches.ForceDefaultTimescale = true;

        [HarmonyPostfix]
        [HarmonyPatch("OnSignificantCollisionEnter")]
        public static void OnSignificantCollisionEnterPostfix() => TimePatches.ForceDefaultTimescale = false;
    }

    [HarmonyPatch(typeof(WindBuffetSFX))]
    public static class WindBuffetPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch("UpdateBuffet")]
        public static void UpdateBuffetPrefix(WindBuffetSFX __instance)
        {
            if (QuicksilverMod.IsEnabled)
            {
                if (QuicksilverMod.TargetTimeScale > 0f)
                {
                    __instance.minSpeed = 5f / QuicksilverMod.TargetTimeScale;
                    __instance.maxSpeed = 40f / QuicksilverMod.TargetTimeScale;
                }

                TimePatches.ForceDefaultTimescale = true;
            }
            else
            {
                __instance.minSpeed = 5f;
                __instance.maxSpeed = 40f;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateBuffet")]
        public static void UpdateBuffetPostfix(WindBuffetSFX __instance)
        {
            TimePatches.ForceDefaultTimescale = false;
        }
    }
}
