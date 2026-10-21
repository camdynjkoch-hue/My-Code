using System.Reflection;

using MelonLoader;

using UnityEngine;

namespace HeroStance
{
    public static class HeroStanceSettings
    {
        public static readonly Color MenuColor = new Color { r = 1f, g = 0.8f, b = 0.2f, a = 1f };

        private static readonly Color White = new Color { r = 1f, g = 1f, b = 1f, a = 1f };

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
        public static float LegSpread => MathUtil.Clamp(_legSpread.Value, 0f, 45f);

        /// <summary>Degrees of knee bend. The shin bends back twice this so the foot stays under the knee.</summary>
        public static float KneeBend => MathUtil.Clamp(_kneeBend.Value, 0f, 30f);

        /// <summary>Degrees each foot's toes turn outward.</summary>
        public static float FootTurnOut => MathUtil.Clamp(_footTurnOut.Value, 0f, 45f);

        /// <summary>Lower the hips so the spread feet stay on the floor instead of lifting up.</summary>
        public static bool KeepFeetPlanted => _keepFeetPlanted.Value;

        /// <summary>Below this hip speed (m/s) you count as standing still.</summary>
        public static float IdleSpeed => System.MathF.Max(0.01f, _idleSpeed.Value);

        /// <summary>Seconds you have to stand still before the stance kicks in.</summary>
        public static float IdleDelay => System.MathF.Max(0f, _idleDelay.Value);

        /// <summary>Seconds to blend into (and out of) the stance.</summary>
        public static float BlendTime => System.MathF.Max(0.01f, _blendTime.Value);

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

        public static void SetupBoneMenu(Assembly boneLib)
        {
            var page = BoneMenuBridge.CreatePage(boneLib, "Hero Stance", MenuColor);
            if (page == null)
                return;

            page.CreateBool("Enabled", MenuColor, _enabled.Value, value => Save(_enabled, value));
            page.CreateFloat("Leg Spread", White, _legSpread.Value, 1f, 0f, 45f, value => Save(_legSpread, value));
            page.CreateFloat("Knee Bend", White, _kneeBend.Value, 1f, 0f, 30f, value => Save(_kneeBend, value));
            page.CreateFloat("Foot Turn Out", White, _footTurnOut.Value, 1f, 0f, 45f, value => Save(_footTurnOut, value));
            page.CreateBool("Keep Feet Planted", White, _keepFeetPlanted.Value, value => Save(_keepFeetPlanted, value));
            page.CreateFloat("Idle Speed", White, _idleSpeed.Value, 0.05f, 0.05f, 1f, value => Save(_idleSpeed, value));
            page.CreateFloat("Idle Delay", White, _idleDelay.Value, 0.1f, 0f, 3f, value => Save(_idleDelay, value));
            page.CreateFloat("Blend Time", White, _blendTime.Value, 0.05f, 0.05f, 2f, value => Save(_blendTime, value));
        }

        private static void Save<T>(MelonPreferences_Entry<T> entry, T value)
        {
            entry.Value = value;
            Category.SaveToFile(false);
        }
    }
}
