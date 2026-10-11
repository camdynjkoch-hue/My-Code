using System;

using BoneLib;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Runs the Sandevistan visuals (green grade, activation flash, rainbow afterimages, player color shield)
    /// whenever slow-motion is active. All timing is real time, so the effects run at normal speed in slow-mo.
    /// </summary>
    public sealed class EffectsController
    {
        private const float ScanInterval = 0.2f;
        private const float FlashDuration = 0.3f;

        private readonly PlayerScanner _scanner = new PlayerScanner();
        private readonly PlayerColorShield _shield = new PlayerColorShield();
        private readonly ScreenGrade _grade = new ScreenGrade();
        private readonly AfterimageSystem _afterimages = new AfterimageSystem();

        private readonly Func<UnityEngine.Renderer, bool> _isOwnRenderer;

        private bool _active;
        private float _weight;
        private float _flash;
        private float _nextScan;
        private bool _rescanRequested;
        private int _frame;
        private float _lastUpdate = -1f;

        /// <summary>Forces the effects on without slowing time (BoneMenu preview).</summary>
        public bool Preview { get; set; }

        public EffectsController()
        {
            _isOwnRenderer = _grade.Owns;
        }

        public void Update()
        {
            float now = FxClock.Now;
            float deltaTime = _lastUpdate < 0f ? 0f : now - _lastUpdate;
            _lastUpdate = now;

            if (deltaTime > 0.1f)
                deltaTime = 0.1f;

            bool enabled = SandevistanSettings.Enabled.Value;
            bool hasRig = FxMath.Alive(Player.RigManager);
            bool shouldBeActive = enabled && hasRig && (Preview || SlowMoDetector.IsSlowMoActive());

            if (shouldBeActive && !_active)
                Activate();
            else if (!shouldBeActive && _active)
                Deactivate();

            float target = _active ? 1f : 0f;
            float fadeTime = _active ? SandevistanSettings.FadeInTime.Value : SandevistanSettings.FadeOutTime.Value;
            _weight = fadeTime <= 0.0001f ? target : FxMath.MoveTowards(_weight, target, deltaTime / fadeTime);
            _flash = FxMath.MoveTowards(_flash, 0f, deltaTime / FlashDuration);

            if (!enabled || !hasRig)
            {
                _weight = 0f;
                _flash = 0f;
            }

            bool visible = _weight > 0.0001f || _flash > 0.0001f;

            try
            {
                if (visible)
                {
                    if (_rescanRequested || now >= _nextScan)
                    {
                        _rescanRequested = false;
                        _nextScan = now + ScanInterval;
                        _scanner.Scan(_isOwnRenderer, CameraLocator.Get());

                        if (SandevistanSettings.KeepPlayerColors.Value)
                            _shield.Apply(_scanner.Renderers);
                        else if (_shield.IsActive)
                            _shield.RestoreAll(destroyClones: true);
                    }
                }
                else if (_shield.IsActive)
                {
                    _shield.RestoreAll(destroyClones: true);
                }
            }
            catch (Exception e)
            {
                FxLog.Error("Keeping player colors", e);
            }

            try
            {
                _grade.Update(CameraLocator.Get(), _weight, _flash);
            }
            catch (Exception e)
            {
                FxLog.Error("Color grade", e);
            }

            // Build afterimage meshes ahead of time, a few frames apart, so activating doesn't hitch
            _frame++;
            if (!_active && enabled && hasRig && _frame % 8 == 0)
            {
                try
                {
                    _afterimages.Prewarm();
                }
                catch (Exception e)
                {
                    FxLog.Error("Preparing afterimages", e);
                }
            }
        }

        public void LateUpdate()
        {
            if (!_active)
                return;

            try
            {
                if (!SandevistanSettings.AfterimagesEnabled.Value)
                    _afterimages.HideAll();
                else
                    _afterimages.Tick(_scanner, CameraLocator.Get());
            }
            catch (Exception e)
            {
                FxLog.Error("Afterimages", e);
                SafeHideAfterimages();
            }
        }

        public void RequestRescan()
        {
            _rescanRequested = true;
        }

        public void OnAvatarChanged()
        {
            try
            {
                _afterimages.OnAvatarChanged();
            }
            catch (Exception e)
            {
                FxLog.Error("Avatar change", e);
            }

            _rescanRequested = true;
        }

        public void OnMaterialSettingsChanged()
        {
            MaterialFactory.BumpGeneration();

            try
            {
                _afterimages.OnMaterialSettingsChanged();
            }
            catch (Exception e)
            {
                FxLog.Error("Applying settings", e);
            }
        }

        public void OnLevelUnloaded()
        {
            _active = false;
            _weight = 0f;
            _flash = 0f;

            try
            {
                _shield.RestoreAll(destroyClones: true);
            }
            catch (Exception e)
            {
                FxLog.Error("Level unload (colors)", e);
            }

            try
            {
                _afterimages.DestroyAll();
            }
            catch (Exception e)
            {
                FxLog.Error("Level unload (afterimages)", e);
            }

            try
            {
                _grade.Destroy();
            }
            catch (Exception e)
            {
                FxLog.Error("Level unload (grade)", e);
            }

            _scanner.Reset();
            CameraLocator.Reset();
        }

        public void OnLevelLoaded()
        {
            _scanner.Reset();
            CameraLocator.Reset();
            _rescanRequested = true;
        }

        private void Activate()
        {
            _active = true;
            _rescanRequested = true;
            _flash = 1f;
            SafeHideAfterimages();
        }

        private void Deactivate()
        {
            _active = false;
            _flash = 0f;
            SafeHideAfterimages();
        }

        private void SafeHideAfterimages()
        {
            try
            {
                _afterimages.HideAll();
            }
            catch (Exception e)
            {
                FxLog.Error("Hiding afterimages", e);
            }
        }
    }
}
