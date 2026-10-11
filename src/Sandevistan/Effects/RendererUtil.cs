using System;
using System.Runtime.CompilerServices;

using MelonLoader;

using UnityEngine;
using UnityEngine.Rendering;

namespace Quicksilver.Sandevistan
{
    internal static class RendererUtil
    {
        private static bool _shadowsBroken;

        public static void DisableShadows(Renderer renderer)
        {
            if (_shadowsBroken || renderer == null)
                return;

            try
            {
                SetShadowsOff(renderer);
            }
            catch (Exception e)
            {
                _shadowsBroken = true;
                MelonLogger.Warning("Couldn't turn off effect shadows (harmless): " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetShadowsOff(Renderer renderer)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        public static Material[] Repeat(Material material, int count)
        {
            var array = new Material[Math.Max(1, count)];
            for (int i = 0; i < array.Length; i++)
                array[i] = material;

            return array;
        }
    }
}
