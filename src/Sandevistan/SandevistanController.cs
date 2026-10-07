using System;

using BoneLib;

using MelonLoader;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Cyberpunk: Edgerunners style Sandevistan.
    /// While active: time slows down (Quicksilver keeps the player at full speed), the world gets a green
    /// color correction, the player keeps their normal colors, and rainbow afterimages are left behind.
    /// Turning it off restores time and removes every afterimage.
    /// </summary>
    public static class SandevistanController
    {
        public static bool IsActive { get; private set; }

        private static float _savedTimeScale = 1f;
        private static float _savedFixedDeltaTime = 0.02f;

        private static Color _appliedFilter;

        private static bool _loggedUpdateError;

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
            _loggedUpdateError = false;

            ApplyTimeScale();

            _appliedFilter = SandevistanSettings.GetFilterColor();
            SandevistanColorCorrection.Enable(_appliedFilter);
            SafeRun(() => PlayerColorKeeper.Apply(mod.rigManager.avatar, PlayerColorKeeper.GetCompensation(_appliedFilter)));

            AfterimageSystem.Begin();

            MelonLogger.Msg("Sandevistan activated.");
        }

        public static void Deactivate()
        {
            if (!IsActive)
                return;

            IsActive = false;

            SafeRun(AfterimageSystem.Clear);
            SafeRun(SandevistanColorCorrection.Disable);
            SafeRun(PlayerColorKeeper.Restore);

            if (Time.timeScale > 0f)
                Time.timeScale = _savedTimeScale;

            Time.fixedDeltaTime = _savedFixedDeltaTime;

            MelonLogger.Msg("Sandevistan deactivated.");
        }

        public static void OnUpdate()
        {
            var mod = QuicksilverMod.Instance;
            bool hasRig = mod != null && mod.HasRigManager();

            if (!hasRig)
            {
                // The rig is gone (level unloading), drop everything
                Deactivate();
                return;
            }

            HandleInput();

            if (!IsActive)
                return;

            try
            {
                UpdateEffects(mod.rigManager);
            }
            catch (Exception e)
            {
                // The avatar can be mid-swap for a frame; only report the first failure per activation
                if (!_loggedUpdateError)
                {
                    _loggedUpdateError = true;
                    MelonLogger.Error($"Sandevistan: {e}");
                }
            }
        }

        private static void UpdateEffects(Il2CppSLZ.Marrow.RigManager rigManager)
        {
            var avatar = rigManager.avatar;

            // Live-update the tint if the intensity was changed from BoneMenu
            Color filter = SandevistanSettings.GetFilterColor();
            bool filterChanged = filter != _appliedFilter;
            if (filterChanged)
            {
                _appliedFilter = filter;
                SandevistanColorCorrection.SetFilter(filter);
            }

            Color compensation = PlayerColorKeeper.GetCompensation(filter);

            // Quicksilver re-swaps the avatar whenever the time scale changes, so keep re-applying to the current one
            if (filterChanged || PlayerColorKeeper.Avatar != avatar)
                PlayerColorKeeper.Apply(avatar, compensation);

            AfterimageSystem.Tick(avatar, GetHeadPosition(rigManager), compensation);
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

        private static void HandleInput()
        {
            if (!SandevistanSettings.ThumbstickActivation)
                return;

            var left = Player.LeftController;
            var right = Player.RightController;

            if (left == null || right == null)
                return;

            // Click both thumbsticks together
            bool leftDown = left.GetThumbStickDown();
            bool rightDown = right.GetThumbStickDown();

            if ((leftDown && right.GetThumbStick()) || (rightDown && left.GetThumbStick()))
                Toggle();
        }

        private static Vector3 GetHeadPosition(Il2CppSLZ.Marrow.RigManager rigManager)
        {
            var camera = Camera.main;
            if (camera != null)
                return camera.transform.position;

            return rigManager.physicsRig.m_head.position;
        }

        private static void SafeRun(Action action)
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                MelonLogger.Error($"Sandevistan: {e}");
            }
        }
    }
}
