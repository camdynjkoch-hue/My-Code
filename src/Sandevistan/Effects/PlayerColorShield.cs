using System;
using System.Collections.Generic;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using UnityEngine;

using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Keeps the player (body, held and holstered items) in their normal colors while the grade is up.
    /// Their renderers get swapped onto clones of their materials with a render queue after the grade quad, so the
    /// grade is drawn underneath them. Shared materials are never modified, and everything is swapped back afterward.
    /// </summary>
    internal sealed class PlayerColorShield
    {
        private readonly Dictionary<IntPtr, Renderer> _protected = new Dictionary<IntPtr, Renderer>();
        private readonly Dictionary<IntPtr, Material> _cloneByOriginal = new Dictionary<IntPtr, Material>();
        private readonly Dictionary<IntPtr, Material> _originalByClone = new Dictionary<IntPtr, Material>();
        private readonly HashSet<IntPtr> _seen = new HashSet<IntPtr>();
        private readonly List<IntPtr> _stale = new List<IntPtr>();

        public bool IsActive => _protected.Count > 0 || _cloneByOriginal.Count > 0;

        public void Apply(List<Renderer> renderers)
        {
            _seen.Clear();

            for (int i = 0; i < renderers.Count; i++)
            {
                Renderer renderer = renderers[i];
                if (!FxMath.Alive(renderer))
                    continue;

                IntPtr key = renderer.Pointer;
                if (_seen.Add(key))
                {
                    EnsureProtected(renderer);
                    _protected[key] = renderer;
                }
            }

            // Anything dropped or put away since the last scan goes back to its own materials
            _stale.Clear();
            foreach (var pair in _protected)
            {
                if (!_seen.Contains(pair.Key))
                    _stale.Add(pair.Key);
            }

            for (int i = 0; i < _stale.Count; i++)
            {
                Restore(_protected[_stale[i]]);
                _protected.Remove(_stale[i]);
            }
        }

        public void RestoreAll(bool destroyClones)
        {
            foreach (var pair in _protected)
                Restore(pair.Value);

            _protected.Clear();

            if (!destroyClones)
                return;

            foreach (var pair in _cloneByOriginal)
            {
                if (FxMath.Alive(pair.Value))
                    Object.Destroy(pair.Value);
            }

            _cloneByOriginal.Clear();
            _originalByClone.Clear();
        }

        private void EnsureProtected(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;
            int length = materials.Length;
            if (length == 0)
                return;

            Material[] replaced = null;
            for (int i = 0; i < length; i++)
            {
                Material material = materials[i];
                if (material == null || _originalByClone.ContainsKey(material.Pointer))
                    continue;

                replaced ??= Copy(materials, length);
                replaced[i] = CloneFor(material);
            }

            if (replaced != null)
                renderer.sharedMaterials = replaced;
        }

        private void Restore(Renderer renderer)
        {
            if (!FxMath.Alive(renderer))
                return;

            var materials = renderer.sharedMaterials;
            int length = materials.Length;

            Material[] restored = null;
            for (int i = 0; i < length; i++)
            {
                Material material = materials[i];
                if (material == null || !_originalByClone.TryGetValue(material.Pointer, out var original))
                    continue;

                restored ??= Copy(materials, length);
                restored[i] = original;
            }

            if (restored != null)
                renderer.sharedMaterials = restored;
        }

        private static Material[] Copy(Il2CppReferenceArray<Material> materials, int length)
        {
            var copy = new Material[length];
            for (int i = 0; i < length; i++)
                copy[i] = materials[i];

            return copy;
        }

        private Material CloneFor(Material original)
        {
            IntPtr key = original.Pointer;
            if (_cloneByOriginal.TryGetValue(key, out var existing) && FxMath.Alive(existing))
                return existing;

            var clone = new Material(original)
            {
                name = original.name + " [Sandevistan]",
                hideFlags = HideFlags.HideAndDontSave
            };
            clone.renderQueue = RenderOrder.PlayerQueue(original.renderQueue);

            _cloneByOriginal[key] = clone;
            _originalByClone[clone.Pointer] = original;
            return clone;
        }
    }
}
