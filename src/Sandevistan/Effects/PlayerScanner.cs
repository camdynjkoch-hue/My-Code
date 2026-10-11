using System;
using System.Collections.Generic;

using BoneLib;

using Il2CppSLZ.Marrow;
using Il2CppSLZ.Marrow.AI;
using Il2CppSLZ.Marrow.Interaction;

using UnityEngine;

using Avatar = Il2CppSLZ.VRMK.Avatar;

namespace Quicksilver.Sandevistan
{
    /// <summary>
    /// Collects everything that counts as "the player": the rig, the avatar, and held or holstered items.
    /// </summary>
    internal sealed class PlayerScanner
    {
        private const float MaxItemDistanceSq = 16f;

        public readonly List<Renderer> Renderers = new List<Renderer>();

        public readonly List<GameObject> ItemRoots = new List<GameObject>();

        private readonly HashSet<IntPtr> _itemKeys = new HashSet<IntPtr>();
        private readonly Dictionary<IntPtr, bool> _npcCache = new Dictionary<IntPtr, bool>();
        private readonly List<InventorySlotReceiver> _slots = new List<InventorySlotReceiver>();

        private IntPtr _slotsRig = IntPtr.Zero;
        private float _slotsRefreshAt;

        private Vector3 _eye;
        private bool _hasEye;

        public void Reset()
        {
            Renderers.Clear();
            ItemRoots.Clear();
            _itemKeys.Clear();
            _npcCache.Clear();
            _slots.Clear();
            _slotsRig = IntPtr.Zero;
            _slotsRefreshAt = 0f;
        }

        public void Scan(Func<Renderer, bool> exclude, Camera camera)
        {
            Renderers.Clear();
            ItemRoots.Clear();
            _itemKeys.Clear();

            Transform head = Player.Head;
            _hasEye = FxMath.Alive(head) || FxMath.Alive(camera);
            if (FxMath.Alive(head))
                _eye = head.position;
            else if (_hasEye)
                _eye = camera.transform.position;

            RigManager rigManager = Player.RigManager;
            if (!FxMath.Alive(rigManager))
                return;

            CollectHeld(Player.LeftHand, rigManager);
            CollectHeld(Player.RightHand, rigManager);
            CollectHolstered(rigManager);

            AddRenderers(rigManager.gameObject, exclude);

            Avatar avatar = rigManager.avatar;
            if (FxMath.Alive(avatar))
                AddRenderers(avatar.gameObject, exclude);

            for (int i = 0; i < ItemRoots.Count; i++)
                AddRenderers(ItemRoots[i], exclude);
        }

        private void CollectHeld(Hand hand, RigManager rig)
        {
            if (!FxMath.Alive(hand))
                return;

            GameObject held = Player.GetObjectInHand(hand);
            if (FxMath.Alive(held))
                AddEntityOf(held.transform, rig);
        }

        private void CollectHolstered(RigManager rig)
        {
            float now = FxClock.Now;
            if (_slotsRig != rig.Pointer || now >= _slotsRefreshAt)
            {
                _slots.Clear();
                foreach (InventorySlotReceiver slot in rig.GetComponentsInChildren<InventorySlotReceiver>(true))
                {
                    if (slot != null)
                        _slots.Add(slot);
                }

                _slotsRig = rig.Pointer;
                _slotsRefreshAt = now + 5f;
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                InventorySlotReceiver slot = _slots[i];
                if (!FxMath.Alive(slot))
                    continue;

                WeaponSlot weapon = slot._slottedWeapon;
                if (FxMath.Alive(weapon))
                    AddEntityOf(weapon.transform, rig);
            }
        }

        private void AddEntityOf(Transform part, RigManager rig)
        {
            if (_hasEye && FxMath.DistanceSq(part.position, _eye) > MaxItemDistanceSq)
                return;

            MarrowEntity entity = part.GetComponentInParent<MarrowEntity>();
            if (!FxMath.Alive(entity))
                return;

            // Skip the player's own rig entity and anything with an AI brain (grabbing an NPC doesn't make it you)
            Hand hand = Player.LeftHand;
            if (!FxMath.Alive(hand))
                hand = Player.RightHand;

            if (FxMath.Alive(hand) && FxMath.IsDescendantOf(hand.transform, entity.transform))
                return;

            if (IsNpc(entity))
                return;

            GameObject root = entity.gameObject;
            if (_itemKeys.Add(root.Pointer))
                ItemRoots.Add(root);
        }

        private bool IsNpc(MarrowEntity entity)
        {
            IntPtr key = entity.Pointer;
            if (_npcCache.TryGetValue(key, out var isNpc))
                return isNpc;

            isNpc = entity.GetComponentInChildren<AIBrain>(true) != null;

            if (_npcCache.Count > 512)
                _npcCache.Clear();

            _npcCache[key] = isNpc;
            return isNpc;
        }

        private void AddRenderers(GameObject root, Func<Renderer, bool> exclude)
        {
            if (!FxMath.Alive(root))
                return;

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null && !exclude(renderer))
                    Renderers.Add(renderer);
            }
        }
    }
}
