using System;
using System.Runtime.CompilerServices;

using MelonLoader;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Builds the unlit transparent materials used by the grade and the afterimages. Prefers URP/Unlit, whose blend
    /// modes can be set freely; falls back to a built-in UI shader (alpha blend only).
    /// </summary>
    internal static class MaterialFactory
    {
        private const string UrpUnlitName = "Universal Render Pipeline/Unlit";

        private static readonly string[] FallbackNames = { "UI/Default", "Sprites/Default" };

        // UnityEngine.Rendering.BlendMode / CullMode / CompareFunction values
        private const float BlendZero = 0f;
        private const float BlendOne = 1f;
        private const float BlendDstColor = 2f;
        private const float BlendSrcAlpha = 5f;
        private const float BlendOneMinusSrcAlpha = 10f;
        private const float CullOff = 0f;
        private const float CullBack = 2f;
        private const float ZTestLessEqual = 4f;
        private const float ZTestAlways = 8f;

        private static Shader _urpUnlit;
        private static Shader _fallback;

        private static float _nextSearch;
        private static bool _reported;
        private static bool _keywordsBroken;

        /// <summary>Bumped whenever material settings change, so cached materials get rebuilt.</summary>
        public static int Generation { get; private set; } = 1;

        public static bool UsesFreeBlend
        {
            get
            {
                bool hasUrp = FxMath.Alive(_urpUnlit);
                bool hasFallback = FxMath.Alive(_fallback);

                if (SandevistanSettings.UseFallbackShader.Value && hasFallback)
                    return false;

                return hasUrp;
            }
        }

        public static void BumpGeneration()
        {
            Generation++;
        }

        public static bool Ready()
        {
            if (FxMath.Alive(_urpUnlit) || FxMath.Alive(_fallback))
                return true;

            float now = FxClock.Now;
            if (now < _nextSearch)
                return false;

            _nextSearch = now + 3f;

            _urpUnlit = FindShader(UrpUnlitName);
            _fallback = null;
            foreach (string name in FallbackNames)
            {
                _fallback = FindShader(name);
                if (_fallback != null)
                    break;
            }

            bool found = _urpUnlit != null || _fallback != null;

            if (!_reported)
            {
                _reported = true;

                if (found)
                    MelonLogger.Msg("Using shader: " + (_urpUnlit != null ? UrpUnlitName : _fallback.name) + (_fallback != null ? " (fallback available: " + _fallback.name + ")" : ""));
                else
                    MelonLogger.Warning("No usable unlit shader found yet; will keep looking.");
            }

            return found;
        }

        public static Material Create(string name, BlendKind kind, Color color, int queue, bool cullOff, bool alwaysOnTop)
        {
            if (!Ready())
                return null;

            bool freeBlend = UsesFreeBlend;
            Shader shader = freeBlend ? _urpUnlit : (FxMath.Alive(_fallback) ? _fallback : _urpUnlit);
            if (!FxMath.Alive(shader))
                return null;

            var material = new Material(shader)
            {
                name = "[Sandevistan] " + name,
                hideFlags = HideFlags.HideAndDontSave
            };

            if (freeBlend)
            {
                float src;
                float dst;
                switch (kind)
                {
                    case BlendKind.Multiply:
                        src = BlendDstColor;
                        dst = BlendZero;
                        break;
                    case BlendKind.Additive:
                        src = BlendSrcAlpha;
                        dst = BlendOne;
                        break;
                    default:
                        src = BlendSrcAlpha;
                        dst = BlendOneMinusSrcAlpha;
                        break;
                }

                SetFloat(material, "_Surface", 1f);
                SetFloat(material, "_AlphaClip", 0f);
                SetFloat(material, "_ZWrite", 0f);
                SetFloat(material, "_Cull", cullOff ? CullOff : CullBack);
                SetFloat(material, "_SrcBlend", src);
                SetFloat(material, "_DstBlend", dst);
                SetFloat(material, "_SrcBlendAlpha", BlendOne);
                SetFloat(material, "_DstBlendAlpha", BlendOneMinusSrcAlpha);

                if (alwaysOnTop)
                    SetFloat(material, "_ZTest", ZTestAlways);

                ApplyTransparentKeywords(material);
            }
            else
            {
                SetFloat(material, "unity_GUIZTestMode", alwaysOnTop ? ZTestAlways : ZTestLessEqual);
            }

            material.renderQueue = queue;
            SetColor(material, color);
            return material;
        }

        public static void SetColor(Material material, Color color)
        {
            if (material == null)
                return;

            material.SetColor("_BaseColor", color);
            material.SetColor("_Color", color);
        }

        public static void Destroy(Material material)
        {
            if (FxMath.Alive(material))
                Object.Destroy(material);
        }

        private static void SetFloat(Material material, string property, float value)
        {
            material.SetFloat(Shader.PropertyToID(property), value);
        }

        private static void ApplyTransparentKeywords(Material material)
        {
            if (_keywordsBroken)
                return;

            try
            {
                SetKeywords(material);
            }
            catch (Exception e)
            {
                _keywordsBroken = true;
                MelonLogger.Warning("Couldn't set shader keywords (continuing without them): " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SetKeywords(Material material)
        {
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHAMODULATE_ON");
        }

        private static Shader FindShader(string name)
        {
            Shader shader = Shader.Find(name);
            if (FxMath.Alive(shader))
                return shader;

            foreach (Shader candidate in Resources.FindObjectsOfTypeAll<Shader>())
            {
                if (candidate != null && candidate.name == name)
                    return candidate;
            }

            return null;
        }
    }
}
