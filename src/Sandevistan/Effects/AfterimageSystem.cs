using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

using BoneLib;

using Il2CppInterop.Runtime.InteropTypes.Arrays;

using Il2CppSLZ.Marrow;

using MelonLoader;

using UnityEngine;

using Avatar = Il2CppSLZ.VRMK.Avatar;
using Object = UnityEngine.Object;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Rainbow afterimages: frozen, see-through copies of the player's avatar (and held/holstered items) left behind
    /// as they move. Each one keeps a single color from the rainbow cycle, stays for a fixed lifetime, then disappears.
    /// Ghost objects are pooled and prebuilt, so spawning one is just copying bone poses.
    /// </summary>
    internal sealed class AfterimageSystem
    {
        /// <summary>
        /// Detects avatar parts that don't follow the player (e.g. a mesh left at the spawn point).
        /// </summary>
        private sealed class PartMotion
        {
            private const float HeadTravelSq = 1f;
            private const float StillLimitSq = 0.010000001f;

            private Vector3 _refPart;
            private Vector3 _refHead;
            private bool _hasRef;
            private bool _reported;

            public bool Stationary;

            public bool Update(Vector3 part, Vector3 head, string name)
            {
                if (!_hasRef)
                {
                    _refPart = part;
                    _refHead = head;
                    _hasRef = true;
                    return Stationary;
                }

                // Only judge once the player has moved far enough
                if (FxMath.DistanceSq(head, _refHead) < HeadTravelSq)
                    return Stationary;

                Stationary = FxMath.DistanceSq(part, _refPart) < StillLimitSq;
                if (Stationary && !_reported)
                {
                    _reported = true;
                    MelonLogger.Msg("Leaving '" + name + "' out of afterimages: it stays put while you move, so it isn't part of your body.");
                }

                _refPart = part;
                _refHead = head;
                return Stationary;
            }
        }

        private sealed class SkinnedProfile
        {
            public SkinnedMeshRenderer Source;
            public string Name;
            public int[] BoneMap;
            public int[] UniqueBones;
            public int RootIndex;
            public int SubMeshCount;
            public int BlendShapeCount;
            public IntPtr MeshKey;
            public IntPtr RootKey;
            public IntPtr[] BoneKeys;
            public bool ReportedFar;
            public readonly PartMotion Motion = new PartMotion();
        }

        private sealed class StaticProfile
        {
            public MeshRenderer Source;
            public MeshFilter Filter;
            public string Name;
            public bool IsHead;
            public int SubMeshCount;
            public IntPtr MeshKey;
            public bool ReportedFar;
            public readonly PartMotion Motion = new PartMotion();
        }

        /// <summary>
        /// Everything about the current avatar that the ghosts need: the union of all bones and every mesh.
        /// </summary>
        private sealed class AvatarProfile
        {
            public int Id;
            public IntPtr AvatarKey;
            public Transform[] Bones;
            public int[] HeadBones;
            public float Reach;
            public int SkinnedComponentCount;
            public int StaticComponentCount;
            public readonly List<SkinnedProfile> Skinned = new List<SkinnedProfile>();
            public readonly List<StaticProfile> Statics = new List<StaticProfile>();
        }

        private sealed class AvatarGhost
        {
            public GameObject Root;
            public Transform[] Bones;
            public Vector3[] HeadScales;
            public SkinnedMeshRenderer[] Skinned;
            public bool[] SkinnedOn;
            public MeshRenderer[] Statics;
            public Transform[] StaticTransforms;
            public bool[] StaticOn;
            public Material Material;
            public int Generation;
            public int ProfileId;
        }

        private sealed class ItemPart
        {
            public GameObject Go;
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public int MaterialSlots = -1;
            public IntPtr MaterialKey = IntPtr.Zero;
            public Vector3 Position;
            public bool Shown;
        }

        private sealed class Snapshot
        {
            public float Born;
            public Color Color;
            public Vector3 Eye;
            public Vector3 LeftHand;
            public Vector3 RightHand;
            public bool HasLeftHand;
            public bool HasRightHand;
            public bool Revealed;
            public bool HeadHidden;
            public AvatarGhost Avatar;
            public readonly List<ItemPart> Items = new List<ItemPart>();
            public Material ItemMaterial;
            public int ItemMaterialGeneration;
        }

        private sealed class ItemSources
        {
            public float RefreshAt;
            public readonly List<MeshRenderer> Renderers = new List<MeshRenderer>();
            public readonly List<MeshFilter> Filters = new List<MeshFilter>();
        }

        private struct PlayerPose
        {
            public Vector3 Eye;
            public Vector3 Body;
            public Vector3 LeftHand;
            public Vector3 RightHand;
            public bool HasLeftHand;
            public bool HasRightHand;

            public static PlayerPose Read(Camera camera)
            {
                var pose = new PlayerPose { Eye = camera.transform.position };
                pose.Body = BodyReference(pose.Eye);

                Hand leftHand = Player.LeftHand;
                if (FxMath.Alive(leftHand))
                {
                    pose.LeftHand = leftHand.transform.position;
                    pose.HasLeftHand = true;
                }

                Hand rightHand = Player.RightHand;
                if (FxMath.Alive(rightHand))
                {
                    pose.RightHand = rightHand.transform.position;
                    pose.HasRightHand = true;
                }

                return pose;
            }
        }

        private enum PoseResult
        {
            Ok,
            NothingToShow,
            Stale
        }

        private const int MaxItemPartsPerSnapshot = 48;
        private const float ItemEyeHideRadiusSq = 0.09f;
        private const float AvatarSettleTime = 1f;
        private const float ValidateInterval = 0.5f;
        private const float HandRevealFactor = 1.75f;

        private GameObject _root;

        private AvatarProfile _profile;
        private int _nextProfileId;
        private IntPtr _failedAvatar = IntPtr.Zero;
        private float _profileRetryAt;
        private int _avatarGhostsCreated;
        private IntPtr _seenAvatar = IntPtr.Zero;
        private float _seenAvatarAt;
        private float _nextValidate;
        private bool _forceRebuild;
        private bool _validationOff;
        private readonly Queue<float> _rebuildTimes = new Queue<float>();

        private Vector3[] _bonePositions = new Vector3[0];

        private readonly Stack<AvatarGhost> _avatarPool = new Stack<AvatarGhost>();
        private readonly Stack<ItemPart> _itemPool = new Stack<ItemPart>();
        private readonly Stack<Snapshot> _snapshotPool = new Stack<Snapshot>();
        private readonly List<Snapshot> _live = new List<Snapshot>();
        private readonly Dictionary<IntPtr, ItemSources> _itemSources = new Dictionary<IntPtr, ItemSources>();

        private readonly List<Vector3> _points = new List<Vector3>();
        private readonly List<Vector3> _lastPoints = new List<Vector3>();
        private float _lastSpawn = -999f;

        private static bool _meshInfoBroken;
        private static bool _blendShapesBroken;

        private static BlendKind GhostBlend => SandevistanSettings.AfterimageAdditive.Value ? BlendKind.Additive : BlendKind.Alpha;

        private static Vector3 BodyReference(Vector3 fallback)
        {
            Transform head = Player.Head;
            return FxMath.Alive(head) ? head.position : fallback;
        }

        public void Tick(PlayerScanner scanner, Camera camera)
        {
            float now = FxClock.Now;

            if (!EnsureRoot())
                return;

            if (!FxMath.Alive(camera))
            {
                HideAll();
                return;
            }

            EnsureProfile(now, immediate: true);

            PlayerPose pose = PlayerPose.Read(camera);

            if (_lastPoints.Count == 0)
            {
                GatherTrackedPoints(pose);
                _lastPoints.AddRange(_points);
            }
            else if (now - _lastSpawn >= Math.Max(0.01f, SandevistanSettings.AfterimageInterval.Value))
            {
                GatherTrackedPoints(pose);

                if (HasMoved())
                {
                    Spawn(scanner, now, pose);
                    _lastSpawn = now;
                    _lastPoints.Clear();
                    _lastPoints.AddRange(_points);
                }
            }

            UpdateLive(now, pose);
        }

        public void HideAll()
        {
            for (int i = 0; i < _live.Count; i++)
                Recycle(_live[i]);

            _live.Clear();
            _lastPoints.Clear();
            _lastSpawn = -999f;
        }

        /// <summary>
        /// Builds one pooled ghost at a time while the Sandevistan is off, so the first activation doesn't hitch.
        /// </summary>
        public void Prewarm()
        {
            if (!SandevistanSettings.AfterimagesEnabled.Value || !EnsureRoot())
                return;

            EnsureProfile(FxClock.Now, immediate: false);
            if (_profile == null)
                return;

            if (_avatarGhostsCreated < MaxAlive())
            {
                AvatarGhost ghost = CreateAvatarGhost();
                if (ghost != null)
                    _avatarPool.Push(ghost);
            }
        }

        public void OnAvatarChanged()
        {
            HideAll();
            DropProfile();
            _seenAvatar = IntPtr.Zero;
        }

        public void OnMaterialSettingsChanged()
        {
            HideAll();
            DropProfile();
        }

        public void DestroyAll()
        {
            HideAll();
            DropProfile();

            while (_snapshotPool.Count > 0)
                MaterialFactory.Destroy(_snapshotPool.Pop().ItemMaterial);

            _itemPool.Clear();
            _itemSources.Clear();
            _seenAvatar = IntPtr.Zero;
            _failedAvatar = IntPtr.Zero;

            if (FxMath.Alive(_root))
                Object.Destroy(_root);

            _root = null;
        }

        private void Spawn(PlayerScanner scanner, float now, PlayerPose pose)
        {
            // At the cap, the oldest afterimage disappears
            int max = MaxAlive();
            while (_live.Count >= max)
            {
                Recycle(_live[0]);
                _live.RemoveAt(0);
            }

            Snapshot snapshot = _snapshotPool.Count > 0 ? _snapshotPool.Pop() : new Snapshot();
            snapshot.Born = now;
            snapshot.Revealed = false;
            snapshot.HeadHidden = false;
            snapshot.Eye = pose.Eye;
            snapshot.LeftHand = pose.LeftHand;
            snapshot.RightHand = pose.RightHand;
            snapshot.HasLeftHand = pose.HasLeftHand;
            snapshot.HasRightHand = pose.HasRightHand;
            snapshot.Color = FxMath.HsvToRgb(now * SandevistanSettings.AfterimageHueSpeed.Value, SandevistanSettings.AfterimageSaturation.Value, 1f);

            try
            {
                if (_profile != null)
                {
                    AvatarGhost ghost = RentAvatarGhost();
                    if (ghost != null)
                    {
                        PoseResult result = PoseAvatarGhost(ghost, pose.Body);
                        if (result == PoseResult.Ok)
                        {
                            snapshot.Avatar = ghost;
                        }
                        else
                        {
                            ReturnAvatarGhost(ghost);

                            if (result == PoseResult.Stale)
                                _forceRebuild = true;
                        }
                    }
                }

                if (SandevistanSettings.AfterimageIncludeItems.Value && scanner.ItemRoots.Count > 0)
                    SpawnItemParts(snapshot, scanner, now, pose.Body);
            }
            catch
            {
                Recycle(snapshot);
                throw;
            }

            if (snapshot.Avatar == null && snapshot.Items.Count == 0)
            {
                _snapshotPool.Push(snapshot);
                return;
            }

            // Hidden until revealed in UpdateLive
            SetSnapshotAlpha(snapshot, 0f);
            _live.Add(snapshot);
        }

        private void UpdateLive(float now, PlayerPose pose)
        {
            float lifetime = Math.Max(0.05f, SandevistanSettings.AfterimageLifetime.Value);
            float revealDelay = Math.Min(Math.Max(0f, SandevistanSettings.AfterimageRevealDelay.Value), lifetime * 0.5f);
            float alpha = FxMath.Clamp01(SandevistanSettings.AfterimageOpacity.Value);

            float headHideRadius = Math.Max(0f, SandevistanSettings.AfterimageHeadHideRadius.Value);
            float headHideSq = headHideRadius * headHideRadius;

            float revealDistance = Math.Max(0f, SandevistanSettings.AfterimageRevealDistance.Value);
            float revealSq = revealDistance * revealDistance;
            float handRevealSq = revealSq * HandRevealFactor * HandRevealFactor;

            Vector3 eye = pose.Eye;

            for (int i = _live.Count - 1; i >= 0; i--)
            {
                Snapshot snapshot = _live[i];
                float age = now - snapshot.Born;

                if (age >= lifetime)
                {
                    Recycle(snapshot);
                    _live.RemoveAt(i);
                    continue;
                }

                // Never let an afterimage's head block the player's view
                bool hideHead = FxMath.DistanceSq(eye, snapshot.Eye) < headHideSq;
                if (hideHead != snapshot.HeadHidden)
                    SetHeadHidden(snapshot, hideHead);

                if (!snapshot.Revealed)
                {
                    bool movedAway = FxMath.DistanceSq(eye, snapshot.Eye) >= revealSq
                        || (pose.HasLeftHand && snapshot.HasLeftHand && FxMath.DistanceSq(pose.LeftHand, snapshot.LeftHand) >= handRevealSq)
                        || (pose.HasRightHand && snapshot.HasRightHand && FxMath.DistanceSq(pose.RightHand, snapshot.RightHand) >= handRevealSq);

                    if (age < revealDelay || !movedAway)
                        continue;

                    SetSnapshotAlpha(snapshot, alpha);

                    if (snapshot.Avatar != null && FxMath.Alive(snapshot.Avatar.Root))
                        snapshot.Avatar.Root.SetActive(true);

                    snapshot.Revealed = true;
                }

                for (int j = 0; j < snapshot.Items.Count; j++)
                {
                    ItemPart part = snapshot.Items[j];
                    bool show = FxMath.DistanceSq(part.Position, eye) >= ItemEyeHideRadiusSq;

                    if (show != part.Shown && FxMath.Alive(part.Go))
                    {
                        part.Go.SetActive(show);
                        part.Shown = show;
                    }
                }
            }
        }

        private static int MaxAlive()
        {
            int value = SandevistanSettings.AfterimageMaxCount.Value;
            if (value < 1)
                return 1;

            return value <= SandevistanSettings.MaxCountLimit ? value : SandevistanSettings.MaxCountLimit;
        }

        private void GatherTrackedPoints(PlayerPose pose)
        {
            _points.Clear();
            _points.Add(pose.Eye);

            if (pose.HasLeftHand)
                _points.Add(pose.LeftHand);

            if (pose.HasRightHand)
                _points.Add(pose.RightHand);
        }

        private bool HasMoved()
        {
            if (_lastPoints.Count != _points.Count)
                return true;

            float minMove = Math.Max(0f, SandevistanSettings.AfterimageMinMove.Value);
            float minMoveSq = minMove * minMove;

            for (int i = 0; i < _points.Count; i++)
            {
                if (FxMath.DistanceSq(_points[i], _lastPoints[i]) >= minMoveSq)
                    return true;
            }

            return false;
        }

        private void SetSnapshotAlpha(Snapshot snapshot, float alpha)
        {
            Color color = snapshot.Color;
            color.a = alpha;

            if (snapshot.Avatar != null)
                MaterialFactory.SetColor(snapshot.Avatar.Material, color);

            if (snapshot.Items.Count > 0)
                MaterialFactory.SetColor(snapshot.ItemMaterial, color);
        }

        private void SetVisible(Snapshot snapshot, bool visible)
        {
            if (snapshot.Avatar != null && FxMath.Alive(snapshot.Avatar.Root))
                snapshot.Avatar.Root.SetActive(visible);

            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ItemPart part = snapshot.Items[i];
                if (FxMath.Alive(part.Go))
                    part.Go.SetActive(visible);

                part.Shown = visible;
            }
        }

        private void SetHeadHidden(Snapshot snapshot, bool hidden)
        {
            snapshot.HeadHidden = hidden;

            AvatarGhost ghost = snapshot.Avatar;
            if (ghost == null || _profile == null)
                return;

            int[] headBones = _profile.HeadBones;
            for (int i = 0; i < headBones.Length; i++)
            {
                Transform bone = ghost.Bones[headBones[i]];
                if (bone != null)
                    bone.localScale = hidden ? Vector3.zero : ghost.HeadScales[i];
            }

            for (int i = 0; i < ghost.Statics.Length; i++)
            {
                if (!_profile.Statics[i].IsHead)
                    continue;

                MeshRenderer renderer = ghost.Statics[i];
                if (renderer != null)
                    renderer.enabled = ghost.StaticOn[i] && !hidden;
            }
        }

        private void Recycle(Snapshot snapshot)
        {
            SetVisible(snapshot, visible: false);

            if (snapshot.Avatar != null)
            {
                AvatarGhost ghost = snapshot.Avatar;
                snapshot.Avatar = null;
                ReturnAvatarGhost(ghost);
            }

            for (int i = 0; i < snapshot.Items.Count; i++)
            {
                ItemPart part = snapshot.Items[i];
                if (FxMath.Alive(part.Go))
                    _itemPool.Push(part);
            }

            snapshot.Items.Clear();
            snapshot.Revealed = false;
            snapshot.HeadHidden = false;
            _snapshotPool.Push(snapshot);
        }

        private bool Reusable(AvatarGhost ghost)
        {
            return _profile != null
                && ghost.ProfileId == _profile.Id
                && FxMath.Alive(ghost.Root)
                && FxMath.Alive(ghost.Material)
                && ghost.Generation == MaterialFactory.Generation;
        }

        private AvatarGhost RentAvatarGhost()
        {
            while (_avatarPool.Count > 0)
            {
                AvatarGhost ghost = _avatarPool.Pop();
                if (Reusable(ghost))
                    return ghost;

                DestroyAvatarGhost(ghost);
            }

            return CreateAvatarGhost();
        }

        private void ReturnAvatarGhost(AvatarGhost ghost)
        {
            if (ghost == null)
                return;

            if (FxMath.Alive(ghost.Root))
                ghost.Root.SetActive(false);

            if (Reusable(ghost))
                _avatarPool.Push(ghost);
            else
                DestroyAvatarGhost(ghost);
        }

        private AvatarGhost CreateAvatarGhost()
        {
            AvatarProfile profile = _profile;
            if (profile == null)
                return null;

            Material material = MaterialFactory.Create("Afterimage", GhostBlend, new Color(1f, 1f, 1f, 0f), RenderOrder.Afterimage, cullOff: false, alwaysOnTop: false);
            if (material == null)
                return null;

            var root = new GameObject("[Sandevistan] Afterimage");
            root.SetActive(false);
            FxMath.AttachAtOrigin(root.transform, _root.transform);

            var ghost = new AvatarGhost
            {
                Root = root,
                Material = material,
                Generation = MaterialFactory.Generation,
                ProfileId = profile.Id,
                Bones = new Transform[profile.Bones.Length],
                HeadScales = new Vector3[profile.HeadBones.Length],
                Skinned = new SkinnedMeshRenderer[profile.Skinned.Count],
                SkinnedOn = new bool[profile.Skinned.Count],
                Statics = new MeshRenderer[profile.Statics.Count],
                StaticTransforms = new Transform[profile.Statics.Count],
                StaticOn = new bool[profile.Statics.Count]
            };

            // Flat bone list; each frame they're placed in world space, so no hierarchy is needed
            for (int i = 0; i < ghost.Bones.Length; i++)
            {
                var bone = new GameObject();
                FxMath.AttachAtOrigin(bone.transform, root.transform);
                ghost.Bones[i] = bone.transform;
            }

            for (int i = 0; i < profile.Skinned.Count; i++)
            {
                SkinnedProfile source = profile.Skinned[i];

                var go = new GameObject("Skin");
                go.layer = source.Source.gameObject.layer;
                FxMath.AttachAtOrigin(go.transform, root.transform);

                var bones = new Transform[source.BoneMap.Length];
                for (int b = 0; b < bones.Length; b++)
                    bones[b] = ghost.Bones[source.BoneMap[b]];

                SkinnedMeshRenderer renderer = go.AddComponent<SkinnedMeshRenderer>();
                renderer.sharedMesh = source.Source.sharedMesh;
                renderer.bones = bones;
                renderer.rootBone = ghost.Bones[source.RootIndex];
                renderer.localBounds = new Bounds(new Vector3(0f, 0f, 0f), new Vector3(5f, 5f, 5f));
                RendererUtil.DisableShadows(renderer);
                renderer.sharedMaterials = RendererUtil.Repeat(material, source.SubMeshCount);

                ghost.Skinned[i] = renderer;
            }

            for (int i = 0; i < profile.Statics.Count; i++)
            {
                StaticProfile source = profile.Statics[i];

                var go = new GameObject("Static");
                go.layer = source.Source.gameObject.layer;
                FxMath.AttachAtOrigin(go.transform, root.transform);

                go.AddComponent<MeshFilter>().sharedMesh = source.Filter.sharedMesh;

                MeshRenderer renderer = go.AddComponent<MeshRenderer>();
                RendererUtil.DisableShadows(renderer);
                renderer.sharedMaterials = RendererUtil.Repeat(material, source.SubMeshCount);

                ghost.Statics[i] = renderer;
                ghost.StaticTransforms[i] = go.transform;
            }

            _avatarGhostsCreated++;
            return ghost;
        }

        private PoseResult PoseAvatarGhost(AvatarGhost ghost, Vector3 body)
        {
            AvatarProfile profile = _profile;
            Transform[] sourceBones = profile.Bones;
            Transform[] ghostBones = ghost.Bones;

            if (_bonePositions.Length < sourceBones.Length)
                _bonePositions = new Vector3[sourceBones.Length];

            for (int i = 0; i < sourceBones.Length; i++)
            {
                Transform source = sourceBones[i];
                if (source == null)
                    return PoseResult.Stale;

                Vector3 position = source.position;
                _bonePositions[i] = position;

                Transform target = ghostBones[i];
                target.SetPositionAndRotation(position, source.rotation);
                target.localScale = source.lossyScale;
            }

            int[] headBones = profile.HeadBones;
            for (int i = 0; i < headBones.Length; i++)
                ghost.HeadScales[i] = ghostBones[headBones[i]].localScale;

            float reachSq = profile.Reach * profile.Reach;
            bool anyVisible = false;

            for (int i = 0; i < profile.Skinned.Count; i++)
            {
                SkinnedProfile source = profile.Skinned[i];
                SkinnedMeshRenderer renderer = ghost.Skinned[i];

                bool on = FxMath.Alive(source.Source) && source.Source.enabled && source.Source.gameObject.activeInHierarchy;
                if (on)
                {
                    Vector3 center = Center(source.UniqueBones, _bonePositions);
                    float distanceSq = FxMath.DistanceSq(center, body);

                    if (distanceSq > reachSq)
                    {
                        on = false;
                        ReportFar(ref source.ReportedFar, source.Name, distanceSq);
                    }
                    else if (source.Motion.Update(center, body, source.Name))
                    {
                        on = false;
                    }
                }

                ghost.SkinnedOn[i] = on;
                renderer.enabled = on;

                if (on)
                {
                    anyVisible = true;

                    if (source.BlendShapeCount > 0)
                        CopyBlendShapes(source.Source, renderer, source.BlendShapeCount);
                }
            }

            for (int i = 0; i < profile.Statics.Count; i++)
            {
                StaticProfile source = profile.Statics[i];

                bool on = FxMath.Alive(source.Source) && source.Source.enabled && source.Source.gameObject.activeInHierarchy;
                Transform sourceTransform = null;

                if (on)
                {
                    sourceTransform = source.Source.transform;
                    Vector3 position = sourceTransform.position;
                    float distanceSq = FxMath.DistanceSq(position, body);

                    if (distanceSq > reachSq)
                    {
                        on = false;
                        ReportFar(ref source.ReportedFar, source.Name, distanceSq);
                    }
                    else if (source.Motion.Update(position, body, source.Name))
                    {
                        on = false;
                    }
                }

                ghost.StaticOn[i] = on;
                ghost.Statics[i].enabled = on;

                if (on)
                {
                    anyVisible = true;

                    Transform target = ghost.StaticTransforms[i];
                    target.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
                    target.localScale = sourceTransform.lossyScale;
                }
            }

            return anyVisible ? PoseResult.Ok : PoseResult.NothingToShow;
        }

        private static Vector3 Center(int[] indices, Vector3[] positions)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

            for (int i = 0; i < indices.Length; i++)
            {
                Vector3 position = positions[indices[i]];
                x += position.x;
                y += position.y;
                z += position.z;
            }

            float inverse = 1f / Math.Max(1, indices.Length);
            return new Vector3(x * inverse, y * inverse, z * inverse);
        }

        private static void ReportFar(ref bool reported, string name, float distanceSq)
        {
            if (reported)
                return;

            reported = true;
            MelonLogger.Msg("Leaving '" + name + "' out of afterimages: it's " + Math.Sqrt(distanceSq).ToString("0.0") + " m from you, so it isn't following your body.");
        }

        private void DestroyAvatarGhost(AvatarGhost ghost)
        {
            if (ghost == null)
                return;

            if (FxMath.Alive(ghost.Root))
                Object.Destroy(ghost.Root);

            MaterialFactory.Destroy(ghost.Material);
        }

        private void SpawnItemParts(Snapshot snapshot, PlayerScanner scanner, float now, Vector3 body)
        {
            if (!FxMath.Alive(snapshot.ItemMaterial) || snapshot.ItemMaterialGeneration != MaterialFactory.Generation)
            {
                MaterialFactory.Destroy(snapshot.ItemMaterial);
                snapshot.ItemMaterial = MaterialFactory.Create("Afterimage Item", GhostBlend, new Color(1f, 1f, 1f, 0f), RenderOrder.Afterimage, cullOff: false, alwaysOnTop: false);
                snapshot.ItemMaterialGeneration = MaterialFactory.Generation;
            }

            if (snapshot.ItemMaterial == null)
                return;

            int budget = MaxItemPartsPerSnapshot;
            IntPtr materialKey = snapshot.ItemMaterial.Pointer;

            float reach = (_profile != null ? _profile.Reach : 2.5f) + 1.5f;
            float reachSq = reach * reach;

            for (int i = 0; i < scanner.ItemRoots.Count && budget > 0; i++)
            {
                GameObject root = scanner.ItemRoots[i];
                if (!FxMath.Alive(root))
                    continue;

                ItemSources sources = GetItemSources(root, now);

                for (int j = 0; j < sources.Renderers.Count && budget > 0; j++)
                {
                    MeshRenderer renderer = sources.Renderers[j];
                    MeshFilter filter = sources.Filters[j];

                    if (!FxMath.Alive(renderer) || !FxMath.Alive(filter) || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        continue;

                    Mesh mesh = filter.sharedMesh;
                    if (mesh == null)
                        continue;

                    Transform source = renderer.transform;
                    Vector3 position = source.position;
                    if (FxMath.DistanceSq(position, body) > reachSq)
                        continue;

                    ItemPart part = RentItemPart();
                    part.Go.layer = renderer.gameObject.layer;
                    part.Filter.sharedMesh = mesh;

                    int slots = SubMeshCount(mesh);
                    if (part.MaterialSlots != slots || part.MaterialKey != materialKey)
                    {
                        part.Renderer.sharedMaterials = RendererUtil.Repeat(snapshot.ItemMaterial, slots);
                        part.MaterialSlots = slots;
                        part.MaterialKey = materialKey;
                    }

                    part.Transform.SetPositionAndRotation(position, source.rotation);
                    part.Transform.localScale = source.lossyScale;
                    part.Position = position;
                    part.Shown = false;

                    snapshot.Items.Add(part);
                    budget--;
                }
            }
        }

        private ItemSources GetItemSources(GameObject root, float now)
        {
            IntPtr key = root.Pointer;
            if (!_itemSources.TryGetValue(key, out var sources))
            {
                if (_itemSources.Count > 128)
                    _itemSources.Clear();

                sources = new ItemSources();
                _itemSources[key] = sources;
            }

            if (now >= sources.RefreshAt)
            {
                sources.RefreshAt = now + 1f;
                sources.Renderers.Clear();
                sources.Filters.Clear();

                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (renderer == null)
                        continue;

                    MeshFilter filter = renderer.GetComponent<MeshFilter>();
                    if (filter == null)
                        continue;

                    sources.Renderers.Add(renderer);
                    sources.Filters.Add(filter);
                }
            }

            return sources;
        }

        private ItemPart RentItemPart()
        {
            while (_itemPool.Count > 0)
            {
                ItemPart pooled = _itemPool.Pop();
                if (FxMath.Alive(pooled.Go))
                    return pooled;
            }

            var go = new GameObject("[Sandevistan] Afterimage Item");
            go.SetActive(false);
            FxMath.AttachAtOrigin(go.transform, _root.transform);

            var part = new ItemPart
            {
                Go = go,
                Transform = go.transform,
                Filter = go.AddComponent<MeshFilter>(),
                Renderer = go.AddComponent<MeshRenderer>()
            };

            RendererUtil.DisableShadows(part.Renderer);
            return part;
        }

        private bool EnsureRoot()
        {
            if (FxMath.Alive(_root))
                return true;

            // The root (and everything under it) is gone, so the pools point at destroyed objects
            _live.Clear();
            _avatarPool.Clear();
            _itemPool.Clear();
            _avatarGhostsCreated = 0;

            _root = new GameObject("[Sandevistan] Afterimages");
            Object.DontDestroyOnLoad(_root);
            return true;
        }

        private void EnsureProfile(float now, bool immediate)
        {
            RigManager rigManager = Player.RigManager;
            if (!FxMath.Alive(rigManager))
                return;

            Avatar avatar = rigManager.avatar;
            if (!FxMath.Alive(avatar))
                return;

            IntPtr key = avatar.Pointer;
            if (key != _seenAvatar)
            {
                _seenAvatar = key;
                _seenAvatarAt = now;
                _rebuildTimes.Clear();
                _validationOff = false;
            }

            if (_profile != null && _profile.AvatarKey == key)
            {
                if (_forceRebuild)
                {
                    Rebuild(avatar.gameObject, key, now);
                }
                else if (now >= _nextValidate)
                {
                    _nextValidate = now + ValidateInterval;

                    if (!_validationOff && !ProfileMatches(avatar.gameObject))
                    {
                        MelonLogger.Msg("Avatar meshes or bones changed; refreshing afterimages.");
                        Rebuild(avatar.gameObject, key, now);
                    }
                    else
                    {
                        TrackPartMotion();
                    }
                }
            }
            else
            {
                // Give a freshly swapped avatar a moment to settle before prewarming
                bool settled = immediate || now - _seenAvatarAt >= AvatarSettleTime;
                bool retryAllowed = key != _failedAvatar || now >= _profileRetryAt;

                if (settled && retryAllowed)
                    Rebuild(avatar.gameObject, key, now);
            }
        }

        private void Rebuild(GameObject avatar, IntPtr key, float now)
        {
            HideAll();
            DropProfile();

            _rebuildTimes.Enqueue(now);
            while (_rebuildTimes.Count > 0 && now - _rebuildTimes.Peek() > 10f)
                _rebuildTimes.Dequeue();

            if (_rebuildTimes.Count > 4 && !_validationOff)
            {
                _validationOff = true;
                MelonLogger.Warning("This avatar keeps changing its meshes; afterimages will stop re-checking it until you switch avatars.");
            }

            _profile = BuildProfile(avatar, key, ++_nextProfileId);
            _nextValidate = now + ValidateInterval;

            if (_profile == null)
            {
                _failedAvatar = key;
                _profileRetryAt = now + 2f;
            }
        }

        private void TrackPartMotion()
        {
            Transform head = Player.Head;
            if (!FxMath.Alive(head))
                return;

            Vector3 headPosition = head.position;
            AvatarProfile profile = _profile;

            if (_bonePositions.Length < profile.Bones.Length)
                _bonePositions = new Vector3[profile.Bones.Length];

            for (int i = 0; i < profile.Bones.Length; i++)
            {
                Transform bone = profile.Bones[i];
                if (bone == null)
                    return;

                _bonePositions[i] = bone.position;
            }

            for (int i = 0; i < profile.Skinned.Count; i++)
            {
                SkinnedProfile skinned = profile.Skinned[i];
                skinned.Motion.Update(Center(skinned.UniqueBones, _bonePositions), headPosition, skinned.Name);
            }

            for (int i = 0; i < profile.Statics.Count; i++)
            {
                StaticProfile staticPart = profile.Statics[i];
                if (FxMath.Alive(staticPart.Source))
                    staticPart.Motion.Update(staticPart.Source.transform.position, headPosition, staticPart.Name);
            }
        }

        private bool ProfileMatches(GameObject avatar)
        {
            AvatarProfile profile = _profile;

            for (int i = 0; i < profile.Skinned.Count; i++)
            {
                SkinnedProfile skinned = profile.Skinned[i];
                if (!FxMath.Alive(skinned.Source))
                    return false;

                if (PointerOf(skinned.Source.sharedMesh) != skinned.MeshKey)
                    return false;

                if (PointerOf(skinned.Source.rootBone) != skinned.RootKey)
                    return false;

                Il2CppReferenceArray<Transform> bones = skinned.Source.bones;
                if (bones.Length != skinned.BoneKeys.Length)
                    return false;

                for (int j = 0; j < skinned.BoneKeys.Length; j++)
                {
                    if (PointerOf(bones[j]) != skinned.BoneKeys[j])
                        return false;
                }
            }

            for (int i = 0; i < profile.Statics.Count; i++)
            {
                StaticProfile staticPart = profile.Statics[i];
                if (!FxMath.Alive(staticPart.Source) || !FxMath.Alive(staticPart.Filter))
                    return false;

                if (PointerOf(staticPart.Filter.sharedMesh) != staticPart.MeshKey)
                    return false;
            }

            if (avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length != profile.SkinnedComponentCount)
                return false;

            if (avatar.GetComponentsInChildren<MeshRenderer>(true).Length != profile.StaticComponentCount)
                return false;

            return true;
        }

        private static IntPtr PointerOf(Object obj)
        {
            return obj == null ? IntPtr.Zero : obj.Pointer;
        }

        private void DropProfile()
        {
            while (_avatarPool.Count > 0)
                DestroyAvatarGhost(_avatarPool.Pop());

            _avatarGhostsCreated = 0;
            _profile = null;
            _forceRebuild = false;
        }

        private static AvatarProfile BuildProfile(GameObject avatar, IntPtr key, int id)
        {
            Transform head = FindHead(avatar);

            var profile = new AvatarProfile
            {
                AvatarKey = key,
                Id = id
            };

            var union = new List<Transform>();
            var unionIndex = new Dictionary<IntPtr, int>();

            int IndexOf(Transform t)
            {
                if (t == null)
                    return -1;

                if (unionIndex.TryGetValue(t.Pointer, out var existing))
                    return existing;

                int index = union.Count;
                union.Add(t);
                unionIndex[t.Pointer] = index;
                return index;
            }

            foreach (SkinnedMeshRenderer skinned in avatar.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                profile.SkinnedComponentCount++;

                if (skinned == null)
                    continue;

                Mesh mesh = skinned.sharedMesh;
                if (mesh == null)
                    continue;

                Il2CppReferenceArray<Transform> bones = skinned.bones;
                int length = bones.Length;
                if (length == 0)
                    continue;

                Transform rootBone = skinned.rootBone;
                int rootIndex = IndexOf(rootBone != null ? rootBone : skinned.transform);

                var boneMap = new int[length];
                var boneKeys = new IntPtr[length];
                var unique = new HashSet<int>();

                for (int i = 0; i < length; i++)
                {
                    Transform bone = bones[i];
                    boneKeys[i] = PointerOf(bone);

                    int index = IndexOf(bone);
                    boneMap[i] = index < 0 ? rootIndex : index;

                    if (index >= 0)
                        unique.Add(index);
                }

                if (unique.Count == 0)
                    unique.Add(rootIndex);

                var uniqueBones = new int[unique.Count];
                unique.CopyTo(uniqueBones);

                profile.Skinned.Add(new SkinnedProfile
                {
                    Source = skinned,
                    Name = skinned.name,
                    BoneMap = boneMap,
                    UniqueBones = uniqueBones,
                    RootIndex = rootIndex,
                    SubMeshCount = SubMeshCount(mesh),
                    BlendShapeCount = BlendShapeCount(mesh),
                    MeshKey = mesh.Pointer,
                    RootKey = PointerOf(rootBone),
                    BoneKeys = boneKeys
                });
            }

            foreach (MeshRenderer renderer in avatar.GetComponentsInChildren<MeshRenderer>(true))
            {
                profile.StaticComponentCount++;

                if (renderer == null)
                    continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null)
                    continue;

                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;

                profile.Statics.Add(new StaticProfile
                {
                    Source = renderer,
                    Filter = filter,
                    Name = renderer.name,
                    IsHead = head != null && FxMath.IsDescendantOf(renderer.transform, head),
                    SubMeshCount = SubMeshCount(mesh),
                    MeshKey = mesh.Pointer
                });
            }

            if (profile.Skinned.Count == 0 && profile.Statics.Count == 0)
                return null;

            profile.Bones = union.ToArray();

            var headBones = new List<int>();
            if (head != null)
            {
                for (int i = 0; i < profile.Bones.Length; i++)
                {
                    if (FxMath.IsDescendantOf(profile.Bones[i], head))
                        headBones.Add(i);
                }
            }

            profile.HeadBones = headBones.ToArray();

            float size = SkeletonSize(profile.Bones);
            profile.Reach = Math.Max(2.5f, size * 1.25f);

            var names = new StringBuilder();
            for (int i = 0; i < profile.Skinned.Count; i++)
                names.Append(i == 0 ? "" : ", ").Append(profile.Skinned[i].Name);

            MelonLogger.Msg("Afterimage profile built: " + profile.Skinned.Count + " skinned mesh(es) [" + names + "], "
                + profile.Statics.Count + " static mesh(es), " + profile.Bones.Length + " bones, size " + size.ToString("0.0") + " m"
                + (head != null ? ", head found" : ", no head bone found"));

            return profile;
        }

        private static float SkeletonSize(Transform[] bones)
        {
            bool first = true;
            var min = new Vector3(0f, 0f, 0f);
            var max = new Vector3(0f, 0f, 0f);

            for (int i = 0; i < bones.Length; i++)
            {
                if (bones[i] == null)
                    continue;

                Vector3 position = bones[i].position;
                if (first)
                {
                    min = position;
                    max = position;
                    first = false;
                    continue;
                }

                if (position.x < min.x) min.x = position.x;
                if (position.y < min.y) min.y = position.y;
                if (position.z < min.z) min.z = position.z;
                if (position.x > max.x) max.x = position.x;
                if (position.y > max.y) max.y = position.y;
                if (position.z > max.z) max.z = position.z;
            }

            return (float)Math.Sqrt(FxMath.DistanceSq(min, max));
        }

        private static Transform FindHead(GameObject avatar)
        {
            try
            {
                Transform head = FindHumanoidHead(avatar);
                if (head != null)
                    return head;
            }
            catch (Exception e)
            {
                MelonLogger.Warning("Humanoid head lookup failed, searching by name instead: " + e.Message);
            }

            foreach (Transform t in avatar.GetComponentsInChildren<Transform>(true))
            {
                if (t == null)
                    continue;

                string name = t.name;
                if (name.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0
                    && name.IndexOf("end", StringComparison.OrdinalIgnoreCase) < 0
                    && name.IndexOf("top", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return t;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Transform FindHumanoidHead(GameObject avatar)
        {
            Animator animator = avatar.GetComponentInChildren<Animator>(true);
            return animator == null ? null : animator.GetBoneTransform(HumanBodyBones.Head);
        }

        private static int SubMeshCount(Mesh mesh)
        {
            if (_meshInfoBroken || mesh == null)
                return 1;

            try
            {
                return Math.Max(1, ReadSubMeshCount(mesh));
            }
            catch (Exception e)
            {
                _meshInfoBroken = true;
                MelonLogger.Warning("Couldn't read sub-mesh counts (afterimages use one material slot): " + e.Message);
                return 1;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadSubMeshCount(Mesh mesh)
        {
            return mesh.subMeshCount;
        }

        private static int BlendShapeCount(Mesh mesh)
        {
            if (_blendShapesBroken || mesh == null)
                return 0;

            try
            {
                return ReadBlendShapeCount(mesh);
            }
            catch (Exception e)
            {
                _blendShapesBroken = true;
                MelonLogger.Warning("Couldn't read blend shapes (afterimages keep a neutral face): " + e.Message);
                return 0;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int ReadBlendShapeCount(Mesh mesh)
        {
            return mesh.blendShapeCount;
        }

        private static void CopyBlendShapes(SkinnedMeshRenderer from, SkinnedMeshRenderer to, int count)
        {
            if (_blendShapesBroken)
                return;

            try
            {
                CopyBlendShapeWeights(from, to, count);
            }
            catch (Exception e)
            {
                _blendShapesBroken = true;
                MelonLogger.Warning("Couldn't copy blend shapes (afterimages keep a neutral face): " + e.Message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CopyBlendShapeWeights(SkinnedMeshRenderer from, SkinnedMeshRenderer to, int count)
        {
            for (int i = 0; i < count; i++)
                to.SetBlendShapeWeight(i, from.GetBlendShapeWeight(i));
        }
    }
}
