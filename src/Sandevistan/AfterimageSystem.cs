using System;
using System.Collections.Generic;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using MelonLoader;

using UnityEngine;
using UnityEngine.Rendering;

using Avatar = Il2CppSLZ.VRMK.Avatar;
using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Leaves frozen copies of the player's avatar behind while the Sandevistan runs.
    /// Afterimages never fade: each one keeps slowly cycling through the rainbow until the Sandevistan
    /// is turned off, at which point every afterimage (and every mesh/material made for it) is destroyed.
    /// </summary>
    public static class AfterimageSystem
    {
        private sealed class Afterimage
        {
            public GameObject root;
            public Material material;
            public float hueOffset;
            public Vector3 anchor;
            public bool visible = true;
            public readonly List<Mesh> ownedMeshes = new List<Mesh>();
            public readonly List<Renderer> renderers = new List<Renderer>();
        }

        // Afterimages spawned right where the player's head is would cover the player's own view
        // until they move away, so they stay hidden while the head is within this distance.
        private const float HideRadius = 0.6f;

        // Each new afterimage starts a little further around the color wheel so the trail reads as a gradient.
        private const float HueStepPerAfterimage = 0.035f;

        private static readonly string[] ShaderNames =
        {
            "Sprites/Default",
            "Universal Render Pipeline/Unlit",
            "UI/Default",
            "Hidden/Internal-Colored",
        };

        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly List<Afterimage> _afterimages = new List<Afterimage>();

        private static Shader _shader;
        private static string _shaderName;
        private static bool _searchedShader;

        private static float _lastSpawnTime;
        private static Vector3 _lastSpawnPosition;
        private static bool _hasSpawned;
        private static int _spawnIndex;

        public static int Count => _afterimages.Count;

        public static void Begin()
        {
            Clear();

            _hasSpawned = false;
            _lastSpawnTime = float.NegativeInfinity;
            _spawnIndex = 0;
        }

        public static void Tick(Avatar avatar, Vector3 headPosition, Color compensation)
        {
            float now = Time.unscaledTime;

            if (avatar != null && ShouldSpawn(now, headPosition))
            {
                try
                {
                    Spawn(avatar, headPosition);
                }
                catch (Exception e)
                {
                    MelonLogger.Error($"Sandevistan: failed to spawn afterimage: {e}");
                }

                _lastSpawnTime = now;
                _lastSpawnPosition = headPosition;
                _hasSpawned = true;
            }

            UpdateAfterimages(now, headPosition, compensation);
        }

        /// <summary>Destroys every afterimage and everything created for them.</summary>
        public static void Clear()
        {
            for (int i = _afterimages.Count - 1; i >= 0; i--)
                Destroy(_afterimages[i]);

            _afterimages.Clear();
        }

        private static bool ShouldSpawn(float now, Vector3 headPosition)
        {
            if (now - _lastSpawnTime < SandevistanSettings.AfterimageInterval)
                return false;

            if (!_hasSpawned)
                return true;

            float spacing = SandevistanSettings.AfterimageSpacing;
            return (headPosition - _lastSpawnPosition).sqrMagnitude >= spacing * spacing;
        }

        private static void Spawn(Avatar avatar, Vector3 headPosition)
        {
            var material = CreateMaterial();
            if (material == null)
                return;

            int maxAfterimages = SandevistanSettings.MaxAfterimages;
            if (maxAfterimages > 0)
            {
                while (_afterimages.Count >= maxAfterimages)
                {
                    Destroy(_afterimages[0]);
                    _afterimages.RemoveAt(0);
                }
            }

            var afterimage = new Afterimage
            {
                root = new GameObject("Sandevistan Afterimage"),
                material = material,
                hueOffset = _spawnIndex * HueStepPerAfterimage,
                anchor = headPosition,
            };

            _spawnIndex++;

            // Track it right away so Clear() still cleans up if baking throws halfway through
            _afterimages.Add(afterimage);

            // Skinned meshes get baked into a frozen snapshot of the current pose
            foreach (var skinned in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(false))
            {
                if (skinned == null || !skinned.enabled || skinned.sharedMesh == null)
                    continue;

                var mesh = new Mesh { name = "Sandevistan Afterimage Mesh" };
                skinned.BakeMesh(mesh, true);
                afterimage.ownedMeshes.Add(mesh);

                var transform = skinned.transform;
                AddPart(afterimage, mesh, transform.position, transform.rotation, Vector3.one);
            }

            // Rigid meshes (accessories, props on the avatar) can just reuse their mesh
            foreach (var meshRenderer in avatar.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (meshRenderer == null || !meshRenderer.enabled)
                    continue;

                var filter = meshRenderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                var transform = meshRenderer.transform;
                AddPart(afterimage, filter.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
            }
        }

        private static void AddPart(Afterimage afterimage, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            var part = new GameObject("Part");
            part.transform.SetParent(afterimage.root.transform, false);
            part.transform.SetPositionAndRotation(position, rotation);
            part.transform.localScale = scale;

            part.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = part.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            int subMeshCount = Mathf.Max(1, mesh.subMeshCount);
            var materials = new Il2CppReferenceArray<Material>(subMeshCount);
            for (int i = 0; i < subMeshCount; i++)
                materials[i] = afterimage.material;

            renderer.sharedMaterials = materials;

            afterimage.renderers.Add(renderer);
        }

        private static void UpdateAfterimages(float now, Vector3 headPosition, Color compensation)
        {
            float hueSpeed = SandevistanSettings.HueCycleSpeed;
            float opacity = SandevistanSettings.AfterimageOpacity;
            float hideRadiusSqr = HideRadius * HideRadius;

            for (int i = _afterimages.Count - 1; i >= 0; i--)
            {
                var afterimage = _afterimages[i];

                // Something else (a level unload) destroyed it
                if (afterimage.root == null)
                {
                    Destroy(afterimage);
                    _afterimages.RemoveAt(i);
                    continue;
                }

                // Slowly drift through the rainbow. Never fades: alpha stays constant.
                Color color = HsvToRgb(afterimage.hueOffset + now * hueSpeed, 1f, 1f);

                // Cancel out the green color correction so the afterimages show their real colors
                color = PlayerColorKeeper.Multiply(color, compensation);
                color.a = opacity;

                afterimage.material.SetColor(ColorId, color);
                afterimage.material.SetColor(BaseColorId, color);

                bool visible = (afterimage.anchor - headPosition).sqrMagnitude > hideRadiusSqr;
                if (visible != afterimage.visible)
                {
                    afterimage.visible = visible;

                    foreach (var renderer in afterimage.renderers)
                    {
                        if (renderer != null)
                            renderer.enabled = visible;
                    }
                }
            }
        }

        private static void Destroy(Afterimage afterimage)
        {
            if (afterimage.root != null)
                Object.Destroy(afterimage.root);

            foreach (var mesh in afterimage.ownedMeshes)
            {
                if (mesh != null)
                    Object.Destroy(mesh);
            }

            if (afterimage.material != null)
                Object.Destroy(afterimage.material);

            afterimage.ownedMeshes.Clear();
            afterimage.renderers.Clear();
        }

        private static Material CreateMaterial()
        {
            if (!_searchedShader)
            {
                _searchedShader = true;

                foreach (var name in ShaderNames)
                {
                    var shader = Shader.Find(name);
                    if (shader != null)
                    {
                        _shader = shader;
                        _shaderName = name;
                        MelonLogger.Msg($"Sandevistan: afterimages are using the \"{name}\" shader.");
                        break;
                    }
                }

                if (_shader == null)
                    MelonLogger.Error("Sandevistan: could not find a usable shader for afterimages.");
            }

            if (_shader == null)
                return null;

            var material = new Material(_shader) { name = "Sandevistan Afterimage" };

            if (_shaderName == "Universal Render Pipeline/Unlit")
            {
                // Switch URP Unlit to alpha-blended transparent
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_Blend", 0f);
                material.SetFloat("_Cull", 0f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetOverrideTag("RenderType", "Transparent");
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else if (_shaderName == "Hidden/Internal-Colored")
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.SetFloat("_Cull", 0f);
            }

            material.renderQueue = (int)RenderQueue.Transparent;

            return material;
        }

        private static Color HsvToRgb(float h, float s, float v)
        {
            h = (h - (float)Math.Floor(h)) * 6f;
            int sector = (int)h;
            float f = h - sector;

            float p = v * (1f - s);
            float q = v * (1f - s * f);
            float t = v * (1f - s * (1f - f));

            switch (sector)
            {
                case 0: return new Color(v, t, p);
                case 1: return new Color(q, v, p);
                case 2: return new Color(p, v, t);
                case 3: return new Color(p, q, v);
                case 4: return new Color(t, p, v);
                default: return new Color(v, p, q);
            }
        }
    }
}
