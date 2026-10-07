using System.Collections.Generic;

using Il2CppInterop.Runtime;

using MelonLoader;

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Global URP volume that multiplies the whole screen by a green color filter.
    /// A color filter is a plain per-channel multiply, so <see cref="PlayerColorKeeper"/> can cancel it out
    /// exactly on the player's own materials.
    /// </summary>
    public static class SandevistanColorCorrection
    {
        private struct CameraState
        {
            public UniversalAdditionalCameraData data;
            public bool renderPostProcessing;
            public int volumeLayerMask;
        }

        private static GameObject _volumeObject;
        private static VolumeProfile _profile;
        private static ColorAdjustments _colorAdjustments;

        private static readonly List<CameraState> _cameraStates = new List<CameraState>();

        public static bool IsEnabled => _volumeObject != null;

        public static void Enable(Color filter)
        {
            Disable();

            try
            {
                int layer = PrepareCameras();

                _profile = ScriptableObject.CreateInstance(Il2CppType.Of<VolumeProfile>()).Cast<VolumeProfile>();
                _profile.name = "Sandevistan Profile";

                _colorAdjustments = ScriptableObject.CreateInstance(Il2CppType.Of<ColorAdjustments>()).Cast<ColorAdjustments>();
                _colorAdjustments.name = "Sandevistan Color Adjustments";
                _colorAdjustments.active = true;
                _colorAdjustments.colorFilter.overrideState = true;
                _colorAdjustments.colorFilter.value = filter;

                _profile.components.Add(_colorAdjustments);
                _profile.isDirty = true;

                // The layer has to be set before the Volume is added, since the Volume registers itself by layer on enable
                _volumeObject = new GameObject("Sandevistan Color Correction");
                _volumeObject.layer = layer;

                var volume = _volumeObject.AddComponent<Volume>();
                volume.isGlobal = true;
                volume.priority = 10000f;
                volume.weight = 1f;
                volume.sharedProfile = _profile;
            }
            catch (System.Exception e)
            {
                MelonLogger.Error($"Sandevistan: failed to create the color correction volume: {e}");
                Disable();
            }
        }

        public static void SetFilter(Color filter)
        {
            if (_colorAdjustments != null)
                _colorAdjustments.colorFilter.value = filter;
        }

        public static void Disable()
        {
            if (_volumeObject != null)
                Object.Destroy(_volumeObject);

            if (_colorAdjustments != null)
                Object.Destroy(_colorAdjustments);

            if (_profile != null)
                Object.Destroy(_profile);

            _volumeObject = null;
            _colorAdjustments = null;
            _profile = null;

            RestoreCameras();
        }

        /// <summary>
        /// Forces post-processing on for every base camera and makes sure each one can see our volume's layer.
        /// Returns the layer the volume should live on.
        /// </summary>
        private static int PrepareCameras()
        {
            _cameraStates.Clear();

            int layer = -1;

            var main = Camera.main;
            if (main != null)
            {
                var mainData = main.GetComponent<UniversalAdditionalCameraData>();
                if (mainData != null)
                    layer = LowestLayer(mainData.volumeLayerMask);
            }

            if (layer < 0)
                layer = 0;

            foreach (var camera in Camera.allCameras)
            {
                if (camera == null)
                    continue;

                var data = camera.GetComponent<UniversalAdditionalCameraData>();
                if (data == null || data.renderType != CameraRenderType.Base)
                    continue;

                int mask = data.volumeLayerMask;

                _cameraStates.Add(new CameraState
                {
                    data = data,
                    renderPostProcessing = data.renderPostProcessing,
                    volumeLayerMask = mask,
                });

                data.renderPostProcessing = true;

                if ((mask & (1 << layer)) == 0)
                    data.volumeLayerMask = mask | (1 << layer);
            }

            return layer;
        }

        private static void RestoreCameras()
        {
            foreach (var state in _cameraStates)
            {
                if (state.data == null || state.data.WasCollected)
                    continue;

                state.data.renderPostProcessing = state.renderPostProcessing;
                state.data.volumeLayerMask = state.volumeLayerMask;
            }

            _cameraStates.Clear();
        }

        private static int LowestLayer(int mask)
        {
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return -1;
        }
    }
}
