using System;
using System.Collections.Generic;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// The green world grade: two quads right in front of the headset camera. One multiplies the screen toward the
    /// grade color, the other adds glow and the activation flash. The player's own materials are drawn after these
    /// (see <see cref="PlayerColorShield"/>), so the player keeps their normal colors.
    /// </summary>
    internal sealed class ScreenGrade
    {
        private GameObject _root;
        private Camera _camera;
        private MeshRenderer _tint;
        private MeshRenderer _glow;
        private Material _tintMaterial;
        private Material _glowMaterial;
        private bool _freeBlend;
        private int _generation;

        private readonly HashSet<IntPtr> _ownRenderers = new HashSet<IntPtr>();

        public bool Owns(Renderer renderer)
        {
            return renderer != null && _ownRenderers.Contains(renderer.Pointer);
        }

        public void Update(Camera camera, float weight, float flash)
        {
            if (!(weight > 0.0001f) && !(flash > 0.0001f))
            {
                Hide();
                return;
            }

            if (!FxMath.Alive(camera) || !EnsureBuilt(camera))
            {
                Hide();
                return;
            }

            if (!_root.activeSelf)
                _root.SetActive(true);

            float distance = Math.Max(_camera.nearClipPlane * 2f, 0.025f);
            _root.transform.localPosition = new Vector3(0f, 0f, distance);
            _root.transform.localRotation = Quaternion.identity;

            Color gradeColor = SandevistanSettings.GradeColorValue;
            float strength = FxMath.Clamp01(SandevistanSettings.GradeStrength.Value) * weight;
            float flashAmount = flash * FxMath.Clamp01(SandevistanSettings.ActivationFlash.Value);

            if (_freeBlend)
            {
                Color tint = FxMath.Lerp(new Color(1f, 1f, 1f, 1f), gradeColor, strength);
                tint.a = 1f;
                MaterialFactory.SetColor(_tintMaterial, tint);

                float glow = FxMath.Clamp01(SandevistanSettings.GlowStrength.Value) * weight + flashAmount * 0.6f;
                bool showGlow = glow > 0.001f;
                _glow.enabled = showGlow;

                if (showGlow)
                    MaterialFactory.SetColor(_glowMaterial, new Color(gradeColor.r, gradeColor.g, gradeColor.b, FxMath.Clamp01(glow)));
            }
            else
            {
                // Fallback shader can only alpha blend: a translucent green wash
                float alpha = FxMath.Clamp01(strength * 0.45f + flashAmount * 0.35f);
                MaterialFactory.SetColor(_tintMaterial, new Color(gradeColor.r, gradeColor.g, gradeColor.b, alpha));
                _glow.enabled = false;
            }
        }

        public void Destroy()
        {
            if (FxMath.Alive(_root))
                Object.Destroy(_root);

            MaterialFactory.Destroy(_tintMaterial);
            MaterialFactory.Destroy(_glowMaterial);

            _root = null;
            _camera = null;
            _tint = null;
            _glow = null;
            _tintMaterial = null;
            _glowMaterial = null;
            _ownRenderers.Clear();
        }

        private void Hide()
        {
            if (FxMath.Alive(_root) && _root.activeSelf)
                _root.SetActive(false);
        }

        private bool EnsureBuilt(Camera camera)
        {
            if (FxMath.Alive(_root) && FxMath.Alive(_camera) && _camera.Pointer == camera.Pointer
                && FxMath.Alive(_tintMaterial) && FxMath.Alive(_glowMaterial) && _generation == MaterialFactory.Generation)
            {
                return true;
            }

            Destroy();

            if (!MaterialFactory.Ready())
                return false;

            _freeBlend = MaterialFactory.UsesFreeBlend;
            _tintMaterial = MaterialFactory.Create("Grade", _freeBlend ? BlendKind.Multiply : BlendKind.Alpha, new Color(1f, 1f, 1f, 0f), RenderOrder.GradeTint, cullOff: true, alwaysOnTop: true);
            _glowMaterial = MaterialFactory.Create("Glow", BlendKind.Additive, new Color(0f, 0f, 0f, 0f), RenderOrder.GradeGlow, cullOff: true, alwaysOnTop: true);

            if (_tintMaterial == null || _glowMaterial == null)
            {
                Destroy();
                return false;
            }

            _camera = camera;
            _generation = MaterialFactory.Generation;

            _root = new GameObject("[Sandevistan] Screen Grade");
            _root.layer = PickLayer(camera);
            FxMath.AttachAtOrigin(_root.transform, camera.transform);

            _tint = CreateQuad("Tint", _tintMaterial);
            _glow = CreateQuad("Glow", _glowMaterial);
            return true;
        }

        private MeshRenderer CreateQuad(string name, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

            Collider collider = quad.GetComponent<Collider>();
            if (collider != null)
                Object.DestroyImmediate(collider);

            quad.name = "[Sandevistan] " + name;
            quad.layer = _root.layer;
            FxMath.AttachAtOrigin(quad.transform, _root.transform);
            quad.transform.localScale = new Vector3(6f, 6f, 1f);

            MeshRenderer renderer = quad.GetComponent<MeshRenderer>();
            renderer.sharedMaterials = RendererUtil.Repeat(material, 1);
            RendererUtil.DisableShadows(renderer);

            _ownRenderers.Add(renderer.Pointer);
            return renderer;
        }

        private static int PickLayer(Camera camera)
        {
            int mask = camera.cullingMask;
            if ((mask & 1) != 0)
                return 0;

            for (int i = 1; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0)
                    return i;
            }

            return 0;
        }
    }
}
