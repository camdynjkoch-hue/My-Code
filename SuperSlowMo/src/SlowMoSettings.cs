using System;

using BoneLib.BoneMenu;

using MelonLoader;

using UnityEngine;

namespace SuperSlowMo
{
    public static class SlowMoSettings
    {
        public static readonly Color MenuColor = new Color(0.45f, 0.75f, 1f);

        public static MelonPreferences_Category Category { get; private set; }

        private static MelonPreferences_Entry<bool> _replaceSlowMo;
        private static MelonPreferences_Entry<float> _worldSpeed;
        private static MelonPreferences_Entry<bool> _slowAnimators;
        private static MelonPreferences_Entry<bool> _slowNavAgents;
        private static MelonPreferences_Entry<bool> _slowParticles;
        private static MelonPreferences_Entry<float> _maxExitSpeed;

        /// <summary>When on, the game's slow-mo button toggles world slow-mo instead of the normal slow-mo.</summary>
        public static bool ReplaceSlowMo => _replaceSlowMo.Value;

        /// <summary>How fast the world runs while active (0.1 = 10x slower).</summary>
        public static float WorldSpeed => Math.Clamp(_worldSpeed.Value, 0.01f, 1f);

        public static bool SlowAnimators => _slowAnimators.Value;

        public static bool SlowNavAgents => _slowNavAgents.Value;

        public static bool SlowParticles => _slowParticles.Value;

        /// <summary>
        /// Objects you hit during slow-mo store up that speed and get it all back at once when time resumes.
        /// This caps how fast they can fly off.
        /// </summary>
        public static float MaxExitSpeed => Math.Max(1f, _maxExitSpeed.Value);

        public static void Setup()
        {
            Category = MelonPreferences.CreateCategory("SuperSlowMo");

            _replaceSlowMo = CreateEntry("ReplaceSlowMo", true, "Replace Slow-Mo Button", "The game's slow-mo button toggles world slow-mo instead of normal slow-mo.");
            _worldSpeed = CreateEntry("WorldSpeed", 0.1f, "World Speed", "How fast everything except you runs while active (0.01-1). 0.1 = 10x slower.");
            _slowAnimators = CreateEntry("SlowAnimators", true, "Slow Animators", "Slow down animations (NPCs, doors, machines).");
            _slowNavAgents = CreateEntry("SlowNavAgents", true, "Slow NavMesh Agents", "Slow down NavMesh pathing for NPCs.");
            _slowParticles = CreateEntry("SlowParticles", true, "Slow Particles", "Slow down particle effects.");
            _maxExitSpeed = CreateEntry("MaxExitSpeed", 50f, "Max Exit Speed", "Speed cap (m/s) for objects when slow-mo ends.");
        }

        public static void SetupBoneMenu()
        {
            var page = Page.Root.CreatePage("Super Slow Mo", MenuColor, 0, true);

            page.CreateFunction("Toggle World Slow-Mo", Color.yellow, WorldSlowMo.Toggle);
            page.CreateBool("Replace Slow-Mo Button", Color.white, _replaceSlowMo.Value, value => Save(_replaceSlowMo, value));
            page.CreateFloat("World Speed", MenuColor, _worldSpeed.Value, 0.01f, 0.01f, 1f, OnSetWorldSpeed);
            page.CreateFloat("Max Exit Speed", Color.white, _maxExitSpeed.Value, 5f, 5f, 200f, value => Save(_maxExitSpeed, value));
            page.CreateBool("Slow Animators", Color.white, _slowAnimators.Value, value => Save(_slowAnimators, value));
            page.CreateBool("Slow NavMesh Agents", Color.white, _slowNavAgents.Value, value => Save(_slowNavAgents, value));
            page.CreateBool("Slow Particles", Color.white, _slowParticles.Value, value => Save(_slowParticles, value));
        }

        private static void OnSetWorldSpeed(float value)
        {
            Save(_worldSpeed, value);
            WorldSlowMo.SetSpeed(WorldSpeed);
        }

        // Passing every argument binds to the overload that exists in all MelonLoader 0.6/0.7 releases
        private static MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName, string description)
        {
            return Category.CreateEntry(identifier, defaultValue, displayName, description, false, false, null);
        }

        private static void Save<T>(MelonPreferences_Entry<T> entry, T value)
        {
            entry.Value = value;
            Category.SaveToFile(false);
        }
    }
}
