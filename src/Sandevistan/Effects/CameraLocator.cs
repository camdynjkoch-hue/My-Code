using BoneLib;

using Il2CppSLZ.Marrow;

using UnityEngine;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Finds the player's headset camera (the one closest to the head that renders to the screen).
    /// </summary>
    internal static class CameraLocator
    {
        private const float MaxDistanceFromHeadSq = 1f;

        private static Camera _camera;

        private static float _nextLookup;

        public static void Reset()
        {
            _camera = null;
            _nextLookup = 0f;
        }

        public static Camera Get()
        {
            float now = FxClock.Now;
            if (now < _nextLookup)
            {
                if (!FxMath.Alive(_camera))
                    return null;

                if (_camera.isActiveAndEnabled)
                    return _camera;
            }

            _nextLookup = now + 1f;

            Transform head = Player.Head;
            bool hasHead = FxMath.Alive(head);
            Vector3 headPosition = hasHead ? head.position : new Vector3(0f, 0f, 0f);

            Camera found = null;
            Camera main = Camera.main;

            if (Usable(main) && (!hasHead || FxMath.DistanceSq(main.transform.position, headPosition) <= MaxDistanceFromHeadSq))
            {
                found = main;
            }
            else
            {
                float best = float.MaxValue;
                RigManager rigManager = Player.RigManager;

                if (FxMath.Alive(rigManager))
                {
                    foreach (Camera camera in rigManager.GetComponentsInChildren<Camera>(false))
                    {
                        if (!Usable(camera))
                            continue;

                        float distance = hasHead ? FxMath.DistanceSq(camera.transform.position, headPosition) : 0f;
                        if (distance < best)
                        {
                            best = distance;
                            found = camera;
                        }
                    }
                }

                if (found == null && Usable(main))
                    found = main;
            }

            _camera = found;
            return found;
        }

        private static bool Usable(Camera camera)
        {
            return FxMath.Alive(camera) && camera.isActiveAndEnabled && camera.targetTexture == null;
        }
    }
}
