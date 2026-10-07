using System.Collections.Generic;

using UnityEngine;

using Avatar = Il2CppSLZ.VRMK.Avatar;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Keeps the Sandevistan user in their normal colors while the green color correction is on.
    /// The color correction multiplies every pixel by a filter; here the player's material colors are
    /// pre-multiplied by the inverse of that filter (through MaterialPropertyBlocks, so the shared materials
    /// are never touched), which cancels the tint out on the player only.
    /// </summary>
    public static class PlayerColorKeeper
    {
        private static readonly int[] ColorProperties =
        {
            Shader.PropertyToID("_BaseColor"),
            Shader.PropertyToID("_Color"),
            Shader.PropertyToID("_EmissionColor"),
        };

        private sealed class Entry
        {
            public Renderer renderer;
            public int materialIndex;
            public bool hadPropertyBlock;
            public readonly List<KeyValuePair<int, Color>> originals = new List<KeyValuePair<int, Color>>();
        }

        private static readonly List<Entry> _entries = new List<Entry>();
        private static MaterialPropertyBlock _block;

        public static Avatar Avatar { get; private set; }

        public static void Apply(Avatar avatar, Color compensation)
        {
            Restore();

            if (avatar == null)
                return;

            Avatar = avatar;
            if (_block == null)
                _block = new MaterialPropertyBlock();

            foreach (var renderer in avatar.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;

                bool hadPropertyBlock = renderer.HasPropertyBlock();
                var materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                {
                    var material = materials[i];
                    if (material == null)
                        continue;

                    var entry = new Entry
                    {
                        renderer = renderer,
                        materialIndex = i,
                        hadPropertyBlock = hadPropertyBlock,
                    };

                    _block.Clear();
                    renderer.GetPropertyBlock(_block, i);

                    foreach (var property in ColorProperties)
                    {
                        if (!material.HasProperty(property))
                            continue;

                        Color original = material.GetColor(property);
                        entry.originals.Add(new KeyValuePair<int, Color>(property, original));
                        _block.SetColor(property, Multiply(original, compensation));
                    }

                    if (entry.originals.Count == 0)
                        continue;

                    renderer.SetPropertyBlock(_block, i);
                    _entries.Add(entry);
                }
            }
        }

        public static void Restore()
        {
            if (_block == null)
                _block = new MaterialPropertyBlock();

            foreach (var entry in _entries)
            {
                var renderer = entry.renderer;
                if (renderer == null || renderer.WasCollected)
                    continue;

                if (!entry.hadPropertyBlock)
                {
                    try
                    {
                        renderer.SetPropertyBlock(null, entry.materialIndex);
                        continue;
                    }
                    catch
                    {
                        // Fall through and write the original colors back instead
                    }
                }

                _block.Clear();
                renderer.GetPropertyBlock(_block, entry.materialIndex);

                foreach (var original in entry.originals)
                    _block.SetColor(original.Key, original.Value);

                renderer.SetPropertyBlock(_block, entry.materialIndex);
            }

            _entries.Clear();
            Avatar = null;
        }

        /// <summary>The inverse of the color filter, per channel.</summary>
        public static Color GetCompensation(Color filter)
        {
            return new Color(1f / Mathf.Max(filter.r, 0.01f), 1f / Mathf.Max(filter.g, 0.01f), 1f / Mathf.Max(filter.b, 0.01f), 1f);
        }

        public static Color Multiply(Color color, Color by)
        {
            return new Color(color.r * by.r, color.g * by.g, color.b * by.b, color.a);
        }
    }
}
