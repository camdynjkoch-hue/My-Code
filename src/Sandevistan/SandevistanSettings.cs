using MelonLoader;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Sandevistan preferences. Slow-motion settings live under [Sandevistan]; the visual effect settings live under
    /// [SandevistanFX] so an existing SandevistanFX config carries over.
    /// </summary>
    public static class SandevistanSettings
    {
        private const int CurrentConfigVersion = 3;

        public const float DefaultHueSpeed = 0.2f;
        public const int DefaultMaxCount = 50;
        public const float DefaultLifetime = 3f;
        public const int MaxCountLimit = 100;

        private static readonly Color DefaultGradeColor = new Color(0.24f, 1f, 0.45f, 1f);

        private static MelonPreferences_Category _timeCategory;
        private static MelonPreferences_Category _fxCategory;

        // Slow-motion
        public static MelonPreferences_Entry<bool> ReplaceSlowMo;
        public static MelonPreferences_Entry<float> TimeScaleEntry;

        // Effects
        public static MelonPreferences_Entry<int> ConfigVersion;
        public static MelonPreferences_Entry<bool> Enabled;
        public static MelonPreferences_Entry<bool> TriggerOnAnyTimeSlowdown;
        public static MelonPreferences_Entry<bool> UseFallbackShader;
        public static MelonPreferences_Entry<string> GradeColor;
        public static MelonPreferences_Entry<float> GradeStrength;
        public static MelonPreferences_Entry<float> GlowStrength;
        public static MelonPreferences_Entry<float> ActivationFlash;
        public static MelonPreferences_Entry<float> FadeInTime;
        public static MelonPreferences_Entry<float> FadeOutTime;
        public static MelonPreferences_Entry<bool> KeepPlayerColors;
        public static MelonPreferences_Entry<bool> AfterimagesEnabled;
        public static MelonPreferences_Entry<float> AfterimageInterval;
        public static MelonPreferences_Entry<float> AfterimageLifetime;
        public static MelonPreferences_Entry<float> AfterimageOpacity;
        public static MelonPreferences_Entry<float> AfterimageHueSpeed;
        public static MelonPreferences_Entry<float> AfterimageSaturation;
        public static MelonPreferences_Entry<float> AfterimageMinMove;
        public static MelonPreferences_Entry<float> AfterimageRevealDelay;
        public static MelonPreferences_Entry<float> AfterimageHeadHideRadius;
        public static MelonPreferences_Entry<float> AfterimageRevealDistance;
        public static MelonPreferences_Entry<int> AfterimageMaxCount;
        public static MelonPreferences_Entry<bool> AfterimageIncludeItems;
        public static MelonPreferences_Entry<bool> AfterimageAdditive;

        private static string _parsedColorSource;
        private static Color _parsedColor = DefaultGradeColor;

        /// <summary>World speed while the Sandevistan is active (0.25 = 4x slower).</summary>
        public static float TimeScale => Mathf.Clamp(TimeScaleEntry.Value, 0.05f, 0.95f);

        public static Color GradeColorValue
        {
            get
            {
                string value = GradeColor.Value;
                if (!ReferenceEquals(value, _parsedColorSource))
                {
                    _parsedColorSource = value;
                    if (FxMath.TryParseHexColor(value, out var color))
                    {
                        _parsedColor = color;
                    }
                    else
                    {
                        MelonLogger.Warning("GradeColor '" + value + "' isn't a valid hex color, using the default neon green.");
                        _parsedColor = DefaultGradeColor;
                    }
                }
                return _parsedColor;
            }
        }

        public static void Setup()
        {
            _timeCategory = MelonPreferences.CreateCategory("Sandevistan");
            ReplaceSlowMo = _timeCategory.CreateEntry("ReplaceSlowMo", true, "Replace Slow-Mo", "On: the slow-mo button toggles the Sandevistan. Off: BONELAB's normal slow-mo (Quicksilver and the effects still apply).");
            TimeScaleEntry = _timeCategory.CreateEntry("TimeScale", 0.25f, "Time Scale", "World speed while the Sandevistan is active (0.05-0.95). You stay at full speed.");

            _fxCategory = MelonPreferences.CreateCategory("SandevistanFX", "Sandevistan FX");
            Enabled = _fxCategory.CreateEntry("Enabled", true, description: "Master switch for the visual effects.");
            TriggerOnAnyTimeSlowdown = _fxCategory.CreateEntry("TriggerOnAnyTimeSlowdown", false, description: "Off: only the Sandevistan / BONELAB's slow-mo triggers the effects. On: any slowed time scale does (other slow-mo mods, Fusion, etc.).");
            UseFallbackShader = _fxCategory.CreateEntry("UseFallbackShader", false, description: "Troubleshooting: turn on if the green grade or afterimages never show up. Uses a simpler built-in shader (grade becomes a green wash).");
            GradeColor = _fxCategory.CreateEntry("GradeColor", "#3DFF73", description: "Hex color of the world grade.");
            GradeStrength = _fxCategory.CreateEntry("GradeStrength", 0.7f, description: "0-1. How hard the world is pushed toward the grade color.");
            GlowStrength = _fxCategory.CreateEntry("GlowStrength", 0.1f, description: "0-1. Extra neon green light added on top of the grade.");
            ActivationFlash = _fxCategory.CreateEntry("ActivationFlash", 0.5f, description: "0-1. Brightness of the green flash when slow-mo kicks in.");
            FadeInTime = _fxCategory.CreateEntry("FadeInTime", 0.12f, description: "Seconds (real time) for the grade to fade in.");
            FadeOutTime = _fxCategory.CreateEntry("FadeOutTime", 0.15f, description: "Seconds (real time) for the grade to fade out. 0 = instant.");
            KeepPlayerColors = _fxCategory.CreateEntry("KeepPlayerColors", true, description: "Keep your body, held weapons/items and holstered gear in their normal colors while the grade is up.");
            AfterimagesEnabled = _fxCategory.CreateEntry("AfterimagesEnabled", true, description: "Rainbow afterimages while slow-mo is on.");
            AfterimageInterval = _fxCategory.CreateEntry("AfterimageInterval", 0.06f, description: "Seconds (real time) between afterimages.");
            AfterimageLifetime = _fxCategory.CreateEntry("AfterimageLifetime", DefaultLifetime, description: "Seconds (real time) each afterimage stays before disappearing instantly (no fade).");
            AfterimageOpacity = _fxCategory.CreateEntry("AfterimageOpacity", 0.5f, description: "0-1. Opacity of each afterimage (constant until it disappears).");
            AfterimageHueSpeed = _fxCategory.CreateEntry("AfterimageHueSpeed", DefaultHueSpeed, description: "How fast the afterimage color moves through the rainbow (cycles per second). Lower = slower color change.");
            AfterimageSaturation = _fxCategory.CreateEntry("AfterimageSaturation", 0.85f, description: "0-1. Color saturation of the afterimages.");
            AfterimageMinMove = _fxCategory.CreateEntry("AfterimageMinMove", 0.02f, description: "Meters your head, hands or held items must move before a new afterimage spawns.");
            AfterimageRevealDelay = _fxCategory.CreateEntry("AfterimageRevealDelay", 0f, description: "Seconds an afterimage stays hidden after spawning. 0 = appears instantly.");
            AfterimageHeadHideRadius = _fxCategory.CreateEntry("AfterimageHeadHideRadius", 0.45f, description: "Afterimage heads are hidden while your eyes are within this many meters of them, so they never block your view.");
            AfterimageRevealDistance = _fxCategory.CreateEntry("AfterimageRevealDistance", 0f, description: "Meters you must move away from an afterimage before it appears. 0 = appears instantly (raise to e.g. 0.12 to keep afterimages off your own body).");
            AfterimageMaxCount = _fxCategory.CreateEntry("AfterimageMaxCount", DefaultMaxCount, description: "Maximum afterimages alive at once. When a new one spawns at the limit, the oldest disappears instantly.");
            AfterimageIncludeItems = _fxCategory.CreateEntry("AfterimageIncludeItems", true, description: "Include held and holstered weapons/items in the afterimages.");
            AfterimageAdditive = _fxCategory.CreateEntry("AfterimageAdditive", false, description: "Off: solid see-through afterimages. On: glowing additive afterimages.");
            ConfigVersion = _fxCategory.CreateEntry("ConfigVersion", 0, description: "Internal: used to update old settings files. Don't edit.");

            Migrate();
        }

        private static void Migrate()
        {
            if (ConfigVersion.Value >= CurrentConfigVersion)
                return;

            if (ConfigVersion.Value < 2)
            {
                AfterimageHueSpeed.Value = DefaultHueSpeed;
                AfterimageMaxCount.Value = DefaultMaxCount;
                AfterimageLifetime.Value = DefaultLifetime;
            }

            if (ConfigVersion.Value < 3)
            {
                AfterimageRevealDelay.Value = 0f;
                AfterimageRevealDistance.Value = 0f;
            }

            ConfigVersion.Value = CurrentConfigVersion;
            Save();
        }

        public static void Save()
        {
            _timeCategory?.SaveToFile(false);
            _fxCategory?.SaveToFile(false);
        }

        public static void Set<T>(MelonPreferences_Entry<T> entry, T value)
        {
            entry.Value = value;
            Save();
        }
    }
}
