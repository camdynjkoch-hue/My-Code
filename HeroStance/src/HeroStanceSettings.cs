using BoneLib.BoneMenu;

using MelonLoader;

using UnityEngine;

namespace HeroStance
{
    public static class HeroStanceSettings
    {
        public static readonly Color MenuColor = new Color(1f, 0.8f, 0.2f);

        public static MelonPreferences_Category Category { get; private set; }

        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<float> _legSpread;
        private static MelonPreferences_Entry<float> _kneeBend;
        private static MelonPreferences_Entry<float> _footTurnOut;
        private static MelonPreferences_Entry<bool> _keepFeetPlanted;
        private static MelonPreferences_Entry<float> _idleSpeed;
        private static MelonPreferences_Entry<float> _idleDelay;
        private static MelonPreferences_Entry<float> _blendTime;

        public static bool Enabled => _enabled.Value;

        /// <summary>Degrees each leg swings out sideways from the hip.</summary>
        public static float LegSpread => Mathf.Clamp(_legSpread.Value, 0f, 45f);

        /// <summary>Degrees of knee bend. The shin bends back twice this so the foot stays under the knee.</summary>
        public static float KneeBend => Mathf.Clamp(_kneeBend.Value, 0f, 30f);

        /// <summary>Degrees each foot's toes turn outward.</summary>
        public static float FootTurnOut => Mathf.Clamp(_footTurnOut.Value, 0f, 45f);

        /// <summary>Lower the hips so the spread feet stay on the floor instead of lifting up.</summary>
        public static bool KeepFeetPlanted => _keepFeetPlanted.Value;

        /// <summary>Below this hip speed (m/s) you count as standing still.</summary>
        public static float IdleSpeed => Mathf.Max(0.01f, _idleSpeed.Value);

        /// <summary>Seconds you have to stand still before the stance kicks in.</summary>
        public static float IdleDelay => Mathf.Max(0f, _idleDelay.Value);

        /// <summary>Seconds to blend into (and out of) the stance.</summary>
        public static float BlendTime => Mathf.Max(0.01f, _blendTime.Value);

        public static void Setup()
        {
            Category = MelonPreferences.CreateCategory("HeroStance");

            _enabled = Category.CreateEntry("Enabled", true, "Enabled", "Pose your legs in a hero stance while idle.");
            _legSpread = Category.CreateEntry("LegSpread", 12f, "Leg Spread", "Degrees each leg swings out from the hip.");
            _kneeBend = Category.CreateEntry("KneeBend", 6f, "Knee Bend", "Degrees of knee bend.");
            _footTurnOut = Category.CreateEntry("FootTurnOut", 12f, "Foot Turn Out", "Degrees each foot's toes point outward.");
            _keepFeetPlanted = Category.CreateEntry("KeepFeetPlanted", true, "Keep Feet Planted", "Lower the hips so the feet stay on the floor.");
            _idleSpeed = Category.CreateEntry("IdleSpeed", 0.2f, "Idle Speed", "Below this speed (m/s) you count as standing still.");
            _idleDelay = Category.CreateEntry("IdleDelay", 0.4f, "Idle Delay", "Seconds of standing still before the stance starts.");
            _blendTime = Category.CreateEntry("BlendTime", 0.35f, "Blend Time", "Seconds to blend in and out of the stance.");
        }

        public static void SetupBoneMenu()
        {
            var page = Page.Root.CreatePage("Hero Stance", MenuColor, 0, true);

            page.CreateBool("Enabled", MenuColor, _enabled.Value, value => Save(_enabled, value));
            page.CreateFloat("Leg Spread", Color.white, _legSpread.Value, 1f, 0f, 45f, value => Save(_legSpread, value));
            page.CreateFloat("Knee Bend", Color.white, _kneeBend.Value, 1f, 0f, 30f, value => Save(_kneeBend, value));
            page.CreateFloat("Foot Turn Out", Color.white, _footTurnOut.Value, 1f, 0f, 45f, value => Save(_footTurnOut, value));
            page.CreateBool("Keep Feet Planted", Color.white, _keepFeetPlanted.Value, value => Save(_keepFeetPlanted, value));
            page.CreateFloat("Idle Speed", Color.white, _idleSpeed.Value, 0.05f, 0.05f, 1f, value => Save(_idleSpeed, value));
            page.CreateFloat("Idle Delay", Color.white, _idleDelay.Value, 0.1f, 0f, 3f, value => Save(_idleDelay, value));
            page.CreateFloat("Blend Time", Color.white, _blendTime.Value, 0.05f, 0.05f, 2f, value => Save(_blendTime, value));
        }

        private static void Save<T>(MelonPreferences_Entry<T> entry, T value)
        {
            entry.Value = value;
            Category.SaveToFile(false);
        }
    }
}
