using System;

using MelonLoader;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Owns the Sandevistan's slow-motion. While active the world runs at <see cref="SandevistanSettings.TimeScale"/>
    /// and Quicksilver keeps the player at full speed. The visual effects follow <see cref="IsActive"/>.
    /// </summary>
    public static class SandevistanTime
    {
        // The game can call more than one slow-mo method for a single button press; treat those as one press.
        private const float ButtonDebounce = 0.15f;

        public static bool IsActive { get; private set; }

        private static float _savedTimeScale = 1f;
        private static float _savedFixedDeltaTime = 0.02f;

        private static float _lastButtonPress = -1f;

        /// <summary>
        /// Called in place of BONELAB's own slow-mo when the slow-mo button is pressed.
        /// </summary>
        public static void OnSlowMoButton()
        {
            float now = FxClock.Now;

            if (_lastButtonPress >= 0f && now - _lastButtonPress < ButtonDebounce)
                return;

            _lastButtonPress = now;

            Toggle();
        }

        public static void Toggle()
        {
            if (IsActive)
                Deactivate();
            else
                Activate();
        }

        public static void Activate()
        {
            if (IsActive)
                return;

            var mod = QuicksilverMod.Instance;
            if (mod == null || !mod.HasRigManager())
            {
                MelonLogger.Warning("Sandevistan: no player rig, can't activate.");
                return;
            }

            float currentTimeScale = Time.timeScale;
            if (currentTimeScale <= 0f)
                return; // Game is paused/loading

            _savedTimeScale = currentTimeScale;
            _savedFixedDeltaTime = Time.fixedDeltaTime;

            IsActive = true;

            ApplyTimeScale();

            MelonLogger.Msg("Sandevistan activated.");
        }

        public static void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;

            try
            {
                if (Time.timeScale > 0f)
                    Time.timeScale = _savedTimeScale;

                Time.fixedDeltaTime = _savedFixedDeltaTime;
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Sandevistan: {e}");
            }

            MelonLogger.Msg("Sandevistan deactivated.");
        }

        public static void OnUpdate()
        {
            var mod = QuicksilverMod.Instance;

            // The rig is gone (level unloading), drop the slow-motion
            if (IsActive && (mod == null || !mod.HasRigManager()))
                Deactivate();
        }

        public static void OnLateUpdate()
        {
            if (!IsActive)
                return;

            float timeScale = Time.timeScale;

            // Don't fight a pause (time scale 0), but keep the game from pulling us out of slow-motion
            if (timeScale > 0f && Mathf.Abs(timeScale - SandevistanSettings.TimeScale) > 0.0001f)
                ApplyTimeScale();
        }

        private static void ApplyTimeScale()
        {
            float target = SandevistanSettings.TimeScale;

            Time.timeScale = target;

            // Keep the physics step rate the same in real time
            Time.fixedDeltaTime = _savedFixedDeltaTime * (target / _savedTimeScale);
        }
    }
}
