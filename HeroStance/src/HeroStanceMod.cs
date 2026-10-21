using System;

using MelonLoader;

using UnityEngine;

using static HeroStance.MathUtil;

namespace HeroStance
{
    public class HeroStanceMod : MelonMod
    {
        public const string Version = "1.0.0";

        private const float StickDeadzone = 0.15f;
        private const int MaxLoggedErrors = 5;

        private bool _ready;

        private Component _avatar;
        private LegPoser _poser;

        private Vector3 _lastHipsPosition;
        private bool _hasLastHipsPosition;
        private float _smoothedSpeed;
        private float _idleTime;
        private float _blend;

        private int _errorCount;

        public override void OnInitializeMelon()
        {
            HeroStanceSettings.Setup();
        }

        public override void OnLateInitializeMelon()
        {
            // BoneLib is a separate mod, so look it up once every mod has loaded
            _ready = BoneLibBridge.Initialize();
            HeroStanceSettings.SetupBoneMenu(BoneLibBridge.BoneLib);

            if (_ready)
                MelonLogger.Msg("Hero Stance ready. Stand still to strike a pose.");
        }

        public override void OnLateUpdate()
        {
            if (!_ready)
                return;

            // LateUpdate runs after the game has posed the avatar for this frame, so we pose on top of it
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                if (_errorCount++ < MaxLoggedErrors)
                    MelonLogger.Error($"Hero Stance: {e}");

                ResetPoser();
            }
        }

        private void Tick()
        {
            if (!RefreshPoser(BoneLibBridge.GetPlayerAvatar()))
                return;

            _poser.BeginFrame();

            float dt = Time.deltaTime;
            if (dt <= 0f)
                return;

            bool idle = UpdateIdle(dt);
            float target = HeroStanceSettings.Enabled && idle ? 1f : 0f;
            _blend = MoveTowards(_blend, target, dt / HeroStanceSettings.BlendTime);

            if (_blend <= 0f)
                return;

            float weight = SmoothStep(_blend);
            _poser.Apply(
                HeroStanceSettings.LegSpread * weight,
                HeroStanceSettings.KneeBend * weight,
                HeroStanceSettings.FootTurnOut * weight,
                HeroStanceSettings.KeepFeetPlanted);
        }

        /// <summary>Rebuilds the poser when the avatar changes. Returns false if there's nothing to pose.</summary>
        private bool RefreshPoser(Component avatar)
        {
            if (avatar == null)
            {
                ResetPoser();
                return false;
            }

            if (_avatar != null && _avatar == avatar)
                return _poser != null && _poser.IsValid;

            ResetPoser();
            _avatar = avatar;

            var animator = avatar.GetComponentInChildren<Animator>();
            _poser = LegPoser.TryCreate(animator);

            if (_poser == null)
                MelonLogger.Warning($"Hero Stance: avatar '{avatar.name}' has no humanoid leg bones, so it can't be posed.");
            else
                MelonLogger.Msg($"Hero Stance: posing avatar '{avatar.name}'.");

            return _poser != null;
        }

        private void ResetPoser()
        {
            _avatar = null;
            _poser = null;
            _hasLastHipsPosition = false;
            _smoothedSpeed = 0f;
            _idleTime = 0f;
            _blend = 0f;
        }

        /// <summary>
        /// You're idle when your body isn't moving and you aren't pushing either thumbstick.
        /// Uses the hips as the game placed them this frame, before our own offset.
        /// </summary>
        private bool UpdateIdle(float dt)
        {
            Vector3 hipsPosition = _poser.Hips.position;

            if (_hasLastHipsPosition)
            {
                float speed = Magnitude(Sub(hipsPosition, _lastHipsPosition)) / dt;
                _smoothedSpeed += (speed - _smoothedSpeed) * (1f - MathF.Exp(-10f * dt));
            }

            _lastHipsPosition = hipsPosition;
            _hasLastHipsPosition = true;

            bool still = _smoothedSpeed < HeroStanceSettings.IdleSpeed && !BoneLibBridge.IsAnyThumbstickHeld(StickDeadzone);

            _idleTime = still ? _idleTime + dt : 0f;
            return _idleTime >= HeroStanceSettings.IdleDelay;
        }
    }
}
