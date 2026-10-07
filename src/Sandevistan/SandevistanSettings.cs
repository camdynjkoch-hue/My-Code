using BoneLib.BoneMenu;

using MelonLoader;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    public static class SandevistanSettings
    {
        public static readonly Color MenuColor = new Color(0.3f, 1f, 0.45f);

        public static MelonPreferences_Category Category { get; private set; }

        private static MelonPreferences_Entry<float> _timeScale;
        private static MelonPreferences_Entry<float> _greenIntensity;
        private static MelonPreferences_Entry<float> _afterimageInterval;
        private static MelonPreferences_Entry<float> _afterimageSpacing;
        private static MelonPreferences_Entry<float> _afterimageOpacity;
        private static MelonPreferences_Entry<float> _hueCycleSpeed;
        private static MelonPreferences_Entry<int> _maxAfterimages;
        private static MelonPreferences_Entry<bool> _thumbstickActivation;

        /// <summary>Time scale while the Sandevistan is running (0.25 = 4x slower).</summary>
        public static float TimeScale => Mathf.Clamp(_timeScale.Value, 0.05f, 0.95f);

        /// <summary>0 = no tint, 1 = strongest green color correction.</summary>
        public static float GreenIntensity => Mathf.Clamp01(_greenIntensity.Value);

        /// <summary>Minimum real-time seconds between afterimages.</summary>
        public static float AfterimageInterval => Mathf.Max(0.01f, _afterimageInterval.Value);

        /// <summary>How far (meters) you have to move before another afterimage is left behind.</summary>
        public static float AfterimageSpacing => Mathf.Max(0f, _afterimageSpacing.Value);

        public static float AfterimageOpacity => Mathf.Clamp(_afterimageOpacity.Value, 0.05f, 1f);

        /// <summary>Full hue cycles per real-time second. Low values = slow color drift.</summary>
        public static float HueCycleSpeed => Mathf.Max(0f, _hueCycleSpeed.Value);

        /// <summary>0 = unlimited. Afterimages never fade; this only exists as a safety valve for performance.</summary>
        public static int MaxAfterimages => Mathf.Max(0, _maxAfterimages.Value);

        public static bool ThumbstickActivation => _thumbstickActivation.Value;

        public static void Setup()
        {
            Category = MelonPreferences.CreateCategory("Sandevistan");

            _timeScale = Category.CreateEntry("TimeScale", 0.25f, "Time Scale", "Time scale while the Sandevistan is active.");
            _greenIntensity = Category.CreateEntry("GreenIntensity", 0.7f, "Green Intensity", "Strength of the green color correction (0-1).");
            _afterimageInterval = Category.CreateEntry("AfterimageInterval", 0.08f, "Afterimage Interval", "Minimum real-time seconds between afterimages.");
            _afterimageSpacing = Category.CreateEntry("AfterimageSpacing", 0.15f, "Afterimage Spacing", "Meters you must move before another afterimage spawns.");
            _afterimageOpacity = Category.CreateEntry("AfterimageOpacity", 0.45f, "Afterimage Opacity", "Opacity of the afterimages (0.05-1).");
            _hueCycleSpeed = Category.CreateEntry("HueCycleSpeed", 0.1f, "Hue Cycle Speed", "Full color cycles per second for the afterimages.");
            _maxAfterimages = Category.CreateEntry("MaxAfterimages", 0, "Max Afterimages", "0 = unlimited. Oldest afterimages are removed past this count.");
            _thumbstickActivation = Category.CreateEntry("ThumbstickActivation", true, "Thumbstick Activation", "Click both thumbsticks together to toggle the Sandevistan.");
        }

        public static void SetupBoneMenu(Page parent)
        {
            parent.CreateFunction("Toggle Sandevistan", MenuColor, SandevistanController.Toggle);

            var page = parent.CreatePage("Sandevistan Settings", MenuColor, 0, true);

            page.CreateBool("Thumbstick Activation", Color.white, _thumbstickActivation.Value, value => Save(_thumbstickActivation, value));
            page.CreateFloat("Time Scale", Color.white, _timeScale.Value, 0.05f, 0.05f, 0.95f, value => Save(_timeScale, value));
            page.CreateFloat("Green Intensity", MenuColor, _greenIntensity.Value, 0.05f, 0f, 1f, value => Save(_greenIntensity, value));
            page.CreateFloat("Afterimage Interval", Color.white, _afterimageInterval.Value, 0.01f, 0.01f, 0.5f, value => Save(_afterimageInterval, value));
            page.CreateFloat("Afterimage Spacing", Color.white, _afterimageSpacing.Value, 0.05f, 0f, 1f, value => Save(_afterimageSpacing, value));
            page.CreateFloat("Afterimage Opacity", Color.white, _afterimageOpacity.Value, 0.05f, 0.05f, 1f, value => Save(_afterimageOpacity, value));
            page.CreateFloat("Hue Cycle Speed", Color.white, _hueCycleSpeed.Value, 0.05f, 0f, 2f, value => Save(_hueCycleSpeed, value));
            page.CreateInt("Max Afterimages (0 = inf)", Color.white, _maxAfterimages.Value, 25, 0, 5000, value => Save(_maxAfterimages, value));
        }

        /// <summary>
        /// The multiplicative filter used by the color correction. Green stays at 1, red/blue are pulled down.
        /// </summary>
        public static Color GetFilterColor()
        {
            float intensity = GreenIntensity;
            return new Color(1f - 0.55f * intensity, 1f, 1f - 0.45f * intensity, 1f);
        }

        private static void Save<T>(MelonPreferences_Entry<T> entry, T value)
        {
            entry.Value = value;
            Category.SaveToFile(false);
        }
    }
}
